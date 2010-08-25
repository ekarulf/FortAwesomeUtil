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
    public class Webserver
    {
        // N.B. - This class makes heavy use of Regex objects, read up on the dangers of compiled Regex
        // http://blogs.msdn.com/b/bclteam/archive/2010/06/25/optimizing-regular-expression-performance-part-i-working-with-the-regex-class-and-regex-objects.aspx
        private static readonly Regex EscapedHttpWildcardRe = new Regex(@"^(http[s]?://)(\\\+|\\\*)(\:[\d]+)?", RegexOptions.Compiled);
        private HttpListener server = null;
        private Dictionary<string, Webservice> services = new Dictionary<string, Webservice>();
        private Regex routingRegex = null;
        private List<ManualResetEvent> requestCompletionEvents = new List<ManualResetEvent>();

        public Webserver()
        {
            // Fail early for old Windows kernels that do not support http.sys
            if (!HttpListener.IsSupported)
            {
                throw new PlatformNotSupportedException("Windows XP SP2/Server 2003 or newer is required to create a webserver.");
            }

            // Constructed here to prevent errors on unsupported kernels
            server = new HttpListener();
        }

        public void RegisterWebservice(string path, Webservice webservice)
        {
            lock (server)
            {
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Can not add a prefix to a running server");
                }

                if (!path.EndsWith("/"))
                {
                    throw new ArgumentException("path should end with /");
                }

                // Register into route processing
                services.Add(path, webservice);
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

                if (!prefix.EndsWith("/"))
                {
                    throw new ArgumentException("prefix should end with /");
                }

                // Register with http.sys
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

            // Block until all workers are complete
            WaitHandle.WaitAll(requestCompletionEvents.ToArray());
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
            StringBuilder sb = new StringBuilder("^");

            // Prefix
            bool first = true;
            foreach (string prefix in server.Prefixes)
            {
                string clean_prefix = Regex.Escape(prefix);
                if (prefix == "/")
                    clean_prefix = "";
                else
                    clean_prefix = EscapedHttpWildcardRe.Replace(clean_prefix, @"$1[\d\w-\.]+$3");
                sb.Append(first ? "(?:" : "|");
                sb.Append(clean_prefix);
                first = false;
            }
            sb.Append(")");

            // Webservice
            first = true;
            foreach (KeyValuePair<string, Webservice> kvp in services)
            {
                string path = Regex.Escape(kvp.Key);
                Webservice webservice = kvp.Value;
                sb.Append(first ? "(?:" : "|");
                sb.AppendFormat("(?<{0}>{1}({2}))", WebserviceGroupName(webservice), path, webservice.RoutingRegex.ToString());
                first = false;
            }
            sb.Append(")$");

            // I create a compiled RegEx because I believe that GenerateRoutes
            // not be called often during the execution of a program.
            // The alternative is to use a regular regex and then do all
            // processing in a static function.
            routingRegex = new Regex(sb.ToString(), RegexOptions.Compiled);
        }

        internal static string WebserviceGroupName(Webservice service)
        {
            return String.Format("svc{0}", service.GetHashCode());
        }

        public Webservice ResolveWebservice(HttpListenerContext context)
        {
            return ResolveWebservice(context.Request.Url.ToString());
        }

        public Webservice ResolveWebservice(string url)
        {
            Match match = routingRegex.Match(url);
            foreach (Webservice webservice in services.Values)
            {
                var foo = match.Groups[WebserviceGroupName(webservice)];
                if (foo.Success)
                {
                    return webservice;
                }
            }
            return null;
        }
    }
}
