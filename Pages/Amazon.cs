using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;

namespace BarCoder.Pages
{
    class Amazon
    {
        public class SearchPage : Page
        {
            //public Uri defaultUri 
            //{
            //    get {return new Uri("www.amazon.com");}
            //}

            //public String Tittle 
            //{
            //    get {return "Amazon";}
            //}


            public TextField Search_TextField
            {
                get
                {
                    return Document.TextField("twotabsearchtextbox");
                }
            }
            public Button Search_Button
            {
                get
                {
                    return Document.Button(Find.ByTitle("Go"));
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
