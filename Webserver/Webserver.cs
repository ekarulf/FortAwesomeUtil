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
    struct WebrequestCallback
    {
        public HttpListenerContext context;
        public ManualResetEvent doneEvent;
    }

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
            if (requestCompletionEvents.Count > 0)
                WaitHandle.WaitAll(requestCompletionEvents.ToArray());
        }

        private void ProcessRequest(IAsyncResult result)
        {
            // Continue handling requests
            server.BeginGetContext(new AsyncCallback(this.ProcessRequest), null);

            WebrequestCallback callbackObj = new WebrequestCallback();
            callbackObj.context = server.EndGetContext(result);
            callbackObj.doneEvent = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessRequest), callbackObj);

            lock (requestCompletionEvents)
            {
                requestCompletionEvents.Add(callbackObj.doneEvent);

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
                    // clean_prefix = EscapedHttpWildcardRe.Replace(clean_prefix, @"$1[\d\w-\.]+$3");
                    clean_prefix = EscapedHttpWildcardRe.Replace(clean_prefix, "");
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
                sb.AppendFormat("(?<{0}>{1}(?:{2}))", WebserviceGroupName(webservice), path, webservice.RoutingRegex.ToString());
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
            return String.Format("service_{0}", service.GetHashCode());
        }

        public Webservice ResolveWebservice(string url)
        {
            Match match = routingRegex.Match(url);
            return ResolveWebservice(match);
        }

        public Webservice ResolveWebservice(Match match)
        {
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

        internal void ProcessRequest(object obj)
        {
            WebrequestCallback callbackObj = (WebrequestCallback)obj;
            HttpListenerContext context = callbackObj.context;

            try
            {
                // Resolve to method
                Match match = routingRegex.Match(callbackObj.context.Request.Url.AbsolutePath);
                Webservice service = ResolveWebservice(match);
                MethodInfo method = null;

                if (service != null)
                {
                    method = service.ResolveMethod(match);
                }

                if (service == null || method == null)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "Service not found";
                }
                else
                {
                    // TODO: Parse POST then GET params?
                    // TODO: Allow variables to be tagged with resolution order
                    // void foo(@WebVar('POST') int x)

                    object[] args = new object[method.GetParameters().Length];

                    foreach (var param in method.GetParameters())
                    {
                        if (param.ParameterType == typeof(HttpListenerContext))
                        {
                            args[param.Position] = callbackObj.context;
                        }
                        else if (param.ParameterType == typeof(HttpListenerRequest))
                        {
                            args[param.Position] = callbackObj.context.Request;
                        }
                        else if (param.ParameterType == typeof(HttpListenerResponse))
                        {
                            args[param.Position] = callbackObj.context.Response;
                        }
                        else
                        {
                            //
                            // Pull argument from URI
                            //

                            Group group = match.Groups[Webservice.WebArgumentGroupName(param.Name)];
                            // If the assertion below fails, check the routing regex.
                            Debug.Assert(group.Success &&
                                Webservice.ValidParameterTypes.Keys.Contains(param.ParameterType));
                            string argString = group.Value;
                            object argValue = null;
                            if (!group.Success)
                            {
                                throw new InvalidOperationException("Programmer Error: URL group match was not successful");
                            }
                            else if (param.ParameterType == typeof(string))
                            {
                                argValue = argString;
                            }
                            else if (Webservice.ValidParameterTypes.Keys.Contains(param.ParameterType))
                            {
                                object[] temp = { argString };
                                argValue = param.ParameterType.InvokeMember("Parse", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, temp);
                            }
                            else
                            {
                                throw new InvalidOperationException(string.Format("Programmer Error: Type {0} is not a supported WebArgument type"));
                            }
                            args[param.Position] = argValue;
                        }
                    }

                    object returnValue = method.Invoke(service, args);

                    if (context.Response.StatusCode == (int)HttpStatusCode.OK && returnValue != null)
                    {
                        Encoding encoding = context.Response.ContentEncoding;
                        if (encoding == null)
                        {
                            context.Response.ContentEncoding = encoding = Encoding.UTF8;
                        }

                        byte[] buffer = null;
                        if (method.ReturnType == typeof(string))
                        {
                            buffer = encoding.GetBytes((string)returnValue);
                        }

                        if (buffer != null)
                        {
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.Response.StatusDescription = "Invalid Return Type";
                        }
                    }
                }
                callbackObj.context.Response.Close();
            }
            finally
            {
                callbackObj.doneEvent.Set();
            }
        }
    }
}
