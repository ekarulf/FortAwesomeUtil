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
        private Thread dispatchThread = null;
        private volatile bool dispatchThreadAlive = false;
        private HttpListener server;
        private object serverLock = new object();
        private Dictionary<Regex, Type> services = new Dictionary<Regex, Type>();

        // Compiled Regular Expressions
        private Regex routingRegex;

        public Webserver()
        {
            // Fail early for old Windows kernels that do not support http.sys
            if (!HttpListener.IsSupported)
            {
                throw new PlatformNotSupportedException("Windows XP SP2/Server 2003 or newer is required.");
            }

            server = new HttpListener();
        }

        public void RegisterWebservice(string path, Webservice webservice)
        {
            lock (serverLock)
            {
                // This limitation allows us to pre-compute the url processing regular expression
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Can not add a prefix to a running server");
                }

                // Translate path into regex friendly path
                Regex key;
                if (path.StartsWith("http://+", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("http://*", StringComparison.OrdinalIgnoreCase))
                {
                    key = new Regex("http://.+" + Regex.Escape(path.Substring(8)), RegexOptions.Compiled);
                }
                else
                {
                    key = new Regex(Regex.Escape(path), RegexOptions.Compiled);
                }

                // Register into route processing
                services.Add(key, webservice.GetType());
            }
        }

        public void Bind(string prefix)
        {
            lock (serverLock)
            {
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Can not add a prefix to a running server");
                }
                server.Prefixes.Add(prefix);
            }
        }

        public void StartServer()
        {
            lock (serverLock)
            {
                if (server.IsListening)
                {
                    throw new InvalidOperationException("Server already started");
                }

                Debug.Assert(dispatchThread == null);
                dispatchThread = new Thread(new ThreadStart(RunServer));
                dispatchThread.Start();
            }
        }

        public void StopServer()
        {
            lock (serverLock)
            {
                if (!server.IsListening)
                {
                    throw new InvalidOperationException("Server has not started");
                }

                dispatchThreadAlive = false;
                server.Stop();
                dispatchThread.Join();
            }
        }

        private void RunServer()
        {
            Debug.Assert(Thread.CurrentThread == dispatchThread, "RunServer should only run inside the dispatch thread");

            // Async completion markers
            List<ManualResetEvent> processingEvents = new List<ManualResetEvent>();

            lock (serverLock)
            {
                // Pre-compute URL processing
                GenerateRoutes();

                // Signals to other methods that the server has started
                server.Start();
            }

            while (dispatchThreadAlive)
            {
                // Wait for a incoming request and enqueue it for the threadpool
                try
                {
                    HttpListenerContext ctx = server.GetContext();
                    ManualResetEvent doneEvent = new ManualResetEvent(false);
                    Webservice webservice = ResolveWebservice(ctx);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(webservice.ProcessRequest), doneEvent);
                    processingEvents.Add(doneEvent);
                }
                catch (HttpListenerException)
                {
                    // No cleanup needed
                }

                // Prune the processingEvents list
                processingEvents.RemoveAll(processingEvent => processingEvent.WaitOne(0));

            }
            server.Stop();
            WaitHandle.WaitAll(processingEvents.ToArray());   // Blocks until all workers are complete
        }

        private void GenerateRoutes()
        {
            StringBuilder sb = new StringBuilder();

            // Group 1: Prefix
            bool firstPrefix = true;
            foreach (string prefix in server.Prefixes)
            {
                sb.Append(firstPrefix ? "(" : "|");
                sb.Append(Regex.Escape(prefix));
            }
            sb.Append(")");

            // Group 2: Webservice Path

            // Group 3: Webservice Method Path

        }

        private Webservice ResolveWebservice(HttpListenerContext context)
        {
            Regex r;
            Match m = r.Match(context.Request.Url);
            m.Groups[2].Captures
            
            // Group 1: Prefix - assumed
            // Group 2: Webservice Path
            services.First(kvp => kvp.Key.Matches(
            throw new NotImplementedException();
        }
    }
}
