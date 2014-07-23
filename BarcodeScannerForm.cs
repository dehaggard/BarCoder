/*
 * Copyright 2012 ZXing.Net authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

using WatiN.Core;


using AForge.Video;
using AForge.Vision;
using AForge.Vision.Motion;
using AForge.Imaging;
using AForge.Imaging.Filters;
using ZXing;
using System.Runtime.InteropServices;
using System.Text;

using System.Diagnostics;

using BarCoder.Pages;



namespace BarCoder
{
    public partial class BarcodeScannerForm : System.Windows.Forms.Form
    {
        private struct Device
        {
            public int Index;
            public string Name;
            public override string ToString()
            {
                return Name;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);


        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


        const uint WM_GETTEXT = 0x000D;
        const uint WM_SETTEXT = 0x000C;
        const uint BM_CLICK = 0x00F5;

        private readonly BarCoder.CameraDevices camDevices;
        private Bitmap currentBitmapForDecoding;
        private readonly Thread decodingThread;
        private Result currentResult;
        private readonly Pen resultRectPen;
        private int lastMotion;
        private MotionDetector detector = new MotionDetector(new SimpleBackgroundModelingDetector());

        public IE ieAmazon;
        public IE ieEbay;
        public IE ieCamel;


        public BarcodeScannerForm()
        {
            //this.Visible = false;
            InitializeComponent();

            camDevices = new CameraDevices();

            decodingThread = new Thread(DecodeBarcode);
            decodingThread.Start();

            //pictureBox1.Paint += pictureBox1_Paint;
            resultRectPen = new Pen(Color.Red, 10);
            lastMotion = 3;

        }

        //void pictureBox1_Paint(object sender, PaintEventArgs e)
        //{

        //}

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadDevicesToCombobox();
            if (camDevices.Devices.Count > 0)
            {
                cmbDevice.SelectedIndex = Properties.Settings.Default.iCameraSelectedIndex;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel)
            {
                decodingThread.Abort();
                if (camDevices.Current != null)
                {
                    camDevices.Current.NewFrame -= Current_NewFrame;
                    if (camDevices.Current.IsRunning)
                    {
                        camDevices.Current.SignalToStop();
                    }
                }
            }
        }
        private void LoadDevicesToCombobox()
        {
            cmbDevice.Items.Clear();
            for (var index = 0; index < camDevices.Devices.Count; index++)
            {
                cmbDevice.Items.Add(new Device { Index = index, Name = camDevices.Devices[index].Name });
            }
        }

        private void cmbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (camDevices.Current != null)
            {
                camDevices.Current.NewFrame -= Current_NewFrame;
                if (camDevices.Current.IsRunning)
                {
                    camDevices.Current.SignalToStop();
                }
            }
            camDevices.SelectCamera(((Device)(cmbDevice.SelectedItem)).Index);
            camDevices.Current.DesiredFrameSize = new Size(320, 240);
            camDevices.Current.NewFrame += Current_NewFrame;
            camDevices.Current.Start();

            Properties.Settings.Default.iCameraSelectedIndex = cmbDevice.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void Current_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (currentBitmapForDecoding == null)
                {
                    currentBitmapForDecoding = (Bitmap)eventArgs.Frame.Clone();
                }
                Invoke(new Action<Bitmap>(ShowFrame), eventArgs.Frame.Clone());
            }
            catch (ObjectDisposedException)
            {
                // not sure, why....
            }
        }

        private void ShowFrame(Bitmap frame)
        {
            //if (pictureBox1.Width < frame.Width)
            //{
            //    pictureBox1.Width = frame.Width;
            //}
            //if (pictureBox1.Height < frame.Height)
            //{
            //    pictureBox1.Height = frame.Height;
            //}

            if (currentResult != null)
            {
                if (currentResult.ResultPoints != null && currentResult.ResultPoints.Length > 0)
                {
                    var resultPoints = currentResult.ResultPoints;
                    var rect = new Rectangle((int)resultPoints[0].X, (int)resultPoints[0].Y, 1, 1);
                    foreach (var point in resultPoints)
                    {
                        if (point.X < rect.Left)
                            rect = new Rectangle((int)point.X, rect.Y, rect.Width + rect.X - (int)point.X, rect.Height);
                        if (point.X > rect.Right)
                            rect = new Rectangle(rect.X, rect.Y, rect.Width + (int)point.X - rect.X, rect.Height);
                        if (point.Y < rect.Top)
                            rect = new Rectangle(rect.X, (int)point.Y, rect.Width, rect.Height + rect.Y - (int)point.Y);
                        if (point.Y > rect.Bottom)
                            rect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height + (int)point.Y - rect.Y);
                    }
                    using (var g = Graphics.FromImage(frame))
                    {
                        g.DrawRectangle(resultRectPen, rect);
                    }
                }
            }

            if (rotateToolStripMenuItem.Checked == true & flipToolStripMenuItem.Checked == false)
            { frame.RotateFlip(RotateFlipType.Rotate180FlipNone); }

            if (rotateToolStripMenuItem.Checked == true & flipToolStripMenuItem.Checked == true)
            { frame.RotateFlip(RotateFlipType.Rotate180FlipXY); }

            if (rotateToolStripMenuItem.Checked == false & flipToolStripMenuItem.Checked == true)
            { frame.RotateFlip(RotateFlipType.RotateNoneFlipXY); }

            if (detector.ProcessFrame(frame) > 0.2)
            {
                this.Visible = true;
                lastMotion = 3;
            }
            pictureBox1.Image = frame;

            //pictureBox1.Image.Save(@"c:\test.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        private void DecodeBarcode()
        {
            var reader = new BarcodeReader();
            while (true)
            {
                if (currentBitmapForDecoding != null)
                {
                    var result = reader.Decode(currentBitmapForDecoding);
                    if (result != null)
                    {
                        //Invoke(new Action<Result>(SendResultToWindow), result);
                        Invoke(new Action<Result>(SendResultToBrowser), result);




                    }
                    currentBitmapForDecoding.Dispose();
                    currentBitmapForDecoding = null;
                }
                Thread.Sleep(200);
            }
        }

        private void SendResultToWindow(Result result)
        {
            currentResult = result;
            txtBarcodeFormat.Text = result.BarcodeFormat.ToString();
            txtContent.Text = result.Text;

            IntPtr hwndBHI = FindWindow("ThunderFormDC", "BHI");
            if (hwndBHI != IntPtr.Zero)
            {
                IntPtr hwndCombo = FindWindowEx(hwndBHI, IntPtr.Zero, "ThunderComboBox", "");
                if (hwndCombo != IntPtr.Zero)
                {
                    StringBuilder text = new StringBuilder(255);
                    SendMessage(hwndCombo, WM_GETTEXT, (IntPtr)text.Capacity, text);
                    string[] Data = result.Text.Split('|');
                    if (text.ToString() != Data[0].ToString())
                    {
                        StringBuilder output = new StringBuilder(result.Text);
                        SendMessage(hwndCombo, WM_SETTEXT, (IntPtr)0, output);
                        IntPtr hwndButton = FindWindowEx(hwndBHI, IntPtr.Zero, "ThunderCommandButton", "&Search");
                        if (hwndButton != IntPtr.Zero)
                        {
                            SendMessage(hwndButton, BM_CLICK, 0, 0);
                        }
                    }
                }
            }
        }

        private void SendResultToBrowser(Result result)
        {
            currentResult = result;
            txtBarcodeFormat.Text = result.BarcodeFormat.ToString();
            txtContent.Text = result.Text;
            if (amazonToolStripMenuItem.Checked)
            {

                //Settings.AutoStartDialogWatcher = false;
                //var thread = new Thread(() =>
                //{
                    
                //    try
                //    {
                //        IE ieAmazon;
                //        if (Browser.Exists<IE>(Find.ByTitle("Amazon")))
                //        //if (Browser.Exists<IE>(Find.ByTitle(Amazon.SearchPage)))
                //        {
                //            ieAmazon = Browser.AttachTo<IE>(Find.ByTitle("Amazon"));
                //            if (ieAmazon.Page<BarCoder.Pages.Amazon.SearchPage>().Search_TextField.Exists == false)
                //            {
                //                ieAmazon.GoTo("www.Amazon.com");
                //            }
                //        }
                //        else
                //        {
                //            ieAmazon = new IE("www.amazon.com");
                //        }
                //        ieAmazon.Page<BarCoder.Pages.Amazon.SearchPage>().Search(result.Text);
                //    }
                //    catch (Exception e)
                //    {

                //    }

                //});
                //thread.SetApartmentState(ApartmentState.STA);
                //thread.Start();


                if (ieAmazon == null)
                {
                    try
                    {
                        if (Browser.Exists<IE>(Find.ByTitle("Amazon")))
                        {
                            ieAmazon = Browser.AttachTo<IE>(Find.ByTitle("Amazon"));
                            ieAmazon.GoTo("www.Amazon.com");
                        }
                        else
                        {
                            ieAmazon = new IE("www.amazon.com");
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
                ieAmazon.Page<BarCoder.Pages.Amazon.SearchPage>().Search(result.Text);
            }

            if (ebayToolStripMenuItem.Checked)
            {
                if (ieEbay == null) { ieEbay = new IE("www.ebay.com"); }
                try
                {
                    if (ieEbay.hWnd == null) { }

                }
                catch (COMException e)
                {
                    ieEbay = new IE("www.ebay.com");
                }
                ieEbay.Page<BarCoder.Pages.Ebay.SearchPage>().Search(result.Text);
            }

            if (camelToolStripMenuItem.Checked)
            {
                if (ieCamel == null) { ieCamel = new IE("www.CamelCamelCamel.com"); }
                try
                {
                    if (ieCamel.hWnd == null) { }

                }
                catch (COMException e)
                {
                    ieCamel = new IE("www.CamelCamelCamel.com");
                }
                ieCamel.Page<BarCoder.Pages.Camel.SearchPage>().Search(result.Text);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentResult = null;
            if (lastMotion != -1)
            {
                if (lastMotion == 0)
                {
                    this.Visible = false;
                }
                else
                {
                    lastMotion -= 1;
                }
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            lastMotion = -1;
            this.Visible = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aforgeResize(object sender, EventArgs e)
        {
            if (this.Visible == true)
            {
                Rectangle workingArea = SystemInformation.VirtualScreen;
                Point newLocation = new Point(workingArea.Right, workingArea.Bottom);
                Screen screen = Screen.FromPoint(newLocation);
                this.Location = new Point(workingArea.Right - this.Size.Width, screen.Bounds.Bottom - this.Size.Height);
            }
        }

        private void BarcodeScannerForm_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.bAmazon)
            {
                amazonToolStripMenuItem.CheckState = CheckState.Checked;
            }

            if (Properties.Settings.Default.bEbay)
            {
                ebayToolStripMenuItem.CheckState = CheckState.Checked;
            }

            if (Properties.Settings.Default.bCamel)
            {
                camelToolStripMenuItem.CheckState = CheckState.Checked;
            }

            if (Properties.Settings.Default.bFlip)
            {
                flipToolStripMenuItem.CheckState = CheckState.Checked;
            }

            if (Properties.Settings.Default.bRotate)
            {
                rotateToolStripMenuItem.CheckState = CheckState.Checked;
            }

        }

        private void BarcodeScannerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.bAmazon = amazonToolStripMenuItem.Checked;
            Properties.Settings.Default.bEbay = ebayToolStripMenuItem.Checked;
            Properties.Settings.Default.bCamel = camelToolStripMenuItem.Checked;
            Properties.Settings.Default.bFlip = flipToolStripMenuItem.Checked;
            Properties.Settings.Default.bRotate = rotateToolStripMenuItem.Checked;
            Properties.Settings.Default.iCameraSelectedIndex = cmbDevice.SelectedIndex;
            Properties.Settings.Default.Save();
        }
    }
}
