using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;



namespace BarCoder.Pages
{
    class Ebay
    {
        public class SearchPage : Page
        {

            //public Uri defaultUri
            //{
            //    get { return new Uri("www.ebay.com"); }
            //}

            //public String Tittle
            //{
            //    get { return "eBay"; }
            //}
            
            public TextField Search_TextField
            {
                get
                {
                    return Document.TextField("gh-ac");
                }
            }
            public Button Search_Button
            {
                get
                {
                    return Document.Button("gh-btn");
                }
            }
            public void Search(string search)
            {
                if (Search_TextField.Value != search)
                {
                    Search_TextField.Value = search;
                    Search_Button.Click();
                }
            }
        }
    }

}
