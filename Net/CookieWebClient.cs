using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace FortAwesomeUtil.Net
{
    public class CookieWebClient : WebClient
    {
        private CookieContainer cookies = null;

        public CookieWebClient(CookieContainer container)
        {
            cookies = container;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = cookies;
            }
            return request;
        }
    }
}
