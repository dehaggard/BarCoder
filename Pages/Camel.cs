using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;

namespace BarCoder.Pages
{
    class Camel
    {
        public class SearchPage : Page
        {
            //public Uri defaultUri
            //{
            //    get { return new Uri("www.camelcamelcamel.com"); }
            //}

            //public String Tittle
            //{
            //    get { return "camel"; }
            //}
            
            public TextField Search_TextField
            {
                get
                {
                    return Document.TextField("sq");
                }
            }
            public Button Search_Button
            {
                get
                {
                    return Document.Button("searchbutton");
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
