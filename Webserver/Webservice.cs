using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

namespace FortAwesomeUtil.Webserver
{
    class Webservice
    {
        internal HttpListenerContext _context;
        // internal static ?????? mapping
        protected HttpListenerRequest Request { get { return _context.Request; } }
        protected HttpListenerResponse Response { get { return _context.Response; } }

        internal sealed Webservice(HttpListenerContext context)
        {
            _context = context;
        }

        internal sealed void ProcessRequest(object doneEventObj)
        {
            ManualResetEvent doneEvent = (ManualResetEvent)doneEventObj;
            try
            {

            }
            finally
            {
                doneEvent.Set();
            }
        }

        // internal sealed static ?????? GetMappings()
    }
}
