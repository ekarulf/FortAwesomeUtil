using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;


namespace FortAwesomeUtil.Webserver
{
    class Webserver
    {
        // N.B. - This class makes heavy use of Regex objects, read up on the dangers of compiled Regex
        private static Regex EscapedHttpWildcardRe = new Regex(@"^http[s]?://(\\\+|\\\*)(?:\:[\d]+)?/$", RegexOptions.Compiled);
        private HttpListener server = null;
        private Dictionary<string, Type> services = new Dictionary<string, Type>();
        private Regex routingRegex = null;
        private List<ManualResetEvent> requestCompletionEvents = new List<ManualResetEvent>();

        public Webserver()
        {
            // Fail early for old Windows kernels that do not support http.sys
            if (!HttpListener.IsSupported)
            {
                throw new PlatformNotSupportedException("Windows XP SP2/Server 2003 or newer is required.");
            }

            // Constructed here to prevent errors on unsupported kernels
            server = new HttpListener();
        }

        public void RegisterWebservice(string path, Webservice webservice)
        {
            lock (server)
            {
                // This limitation allows us to pre-compute the url processing regular expression
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Can not add a prefix to a running server");
                }

                // Register into route processing
                services.Add(path, webservice.GetType());
            }
        }

        public void Bind(string prefix)
        {
            lock (server)
            {
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Can not add a prefix to a running server");
                }
                server.Prefixes.Add(prefix);
            }
        }

        public void Start()
        {
            lock (server)
            {
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Server already started");
                }

                // Pre-compute URL processing
                GenerateRoutes();

                // Signals to other methods that the server has started
                server.Start();

                // Register Async Request Handler
                server.BeginGetContext(new AsyncCallback(this.ProcessRequest), null);
            }
        }

        public void Stop()
        {
            lock (server)
            {
                if (!server.IsListening)
                {
                    throw new InvalidOperationException("Server has not started");
                }

                // Stop accepting new connections and finish existing queue
                server.Close();
            }
        }

        public void Join()
        {
            lock (server)
            {
                if (!server.IsListening)
                {
                    throw new InvalidOperationException("Server has not started");
                }
            }

            lock (requestCompletionEvents)
            {
                // Block until all workers are complete
                WaitHandle.WaitAll(requestCompletionEvents.ToArray());
            }

        }

        private void ProcessRequest(IAsyncResult result)
        {
            HttpListenerContext ctx = server.EndGetContext(result);
            ManualResetEvent doneEvent = new ManualResetEvent(false);
            Webservice webservice = ResolveWebservice(ctx);
            ThreadPool.QueueUserWorkItem(new WaitCallback(webservice.ProcessRequest), doneEvent);

            lock (requestCompletionEvents)
            {
                requestCompletionEvents.Add(doneEvent);

                // Prune the processingEvents list
                requestCompletionEvents.RemoveAll(processingEvent => processingEvent.WaitOne(0));
            }
        }

        /// <summary>
        /// This function generates a giant regular expression used for URL routing
        /// 
        /// The regular expression is designed to be used with CaptureCollections, where the
        /// captured group index can be used to lookup the appropriate Webservice
        /// </summary>
        private void GenerateRoutes()
        {
            StringBuilder sb = new StringBuilder();

            // Prefix
            bool first = true;
            foreach (string prefix in server.Prefixes)
            {
                string clean_prefix = Regex.Escape(prefix);
                clean_prefix = EscapedHttpWildcardRe.Replace(clean_prefix, ".+");
                sb.Append(first ? "(?:" : "|");
                sb.Append(clean_prefix);
                first = false;
            }
            sb.Append(")");

            // Webservice
            first = true;
            foreach (KeyValuePair<string, Type> kvp in services)
            {
                string path = Regex.Escape(kvp.Key);
                Type serviceType = kvp.Value;
                sb.Append(first ? "(?:" : "|");
                // Ideally I would be passing: Class<? extends Webservice>, which makes static invocation much prettier :)
                Regex webserviceMap = (Regex)serviceType.InvokeMember("GetURLMapping", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, null, null);
                sb.AppendFormat("({0}({1}))", path, webserviceMap.ToString());
                first = false;
            }
            sb.Append(")");
        }

        private Webservice ResolveWebservice(HttpListenerContext context)
        {
            Regex r;
            Match m = r.Match(context.Request.Url);
            m.Groups[2].Captures
            
            // Group 1: Prefix - assumed
            // Group 2: Webservice Path
            services.First(kvp => kvp.Key.Matches()));
            
            throw new NotImplementedException();
        }
    }
}
