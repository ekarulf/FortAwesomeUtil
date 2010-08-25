using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.Reflection;
using FortAwesomeUtil.Webserver.Framework;

namespace FortAwesomeUtil.Webserver
{
    abstract public class Webservice
    {
        private static readonly Dictionary<Type, string> ValidParameterTypes = new Dictionary<Type, string> {
            {typeof(byte),      @"\d{1,3}"},
            {typeof(sbyte),     @"-?\d{1,3}"},
            {typeof(int),       @"-?\d+"},
            {typeof(uint),      @"\d+"},
            {typeof(short),     @"-?\d+"},
            {typeof(ushort),    @"\d+"},
            {typeof(long),      @"-?\d+"},
            {typeof(ulong),     @"\d+"},
            {typeof(float),     @"-?\d+\.\d+"},
            {typeof(double),    @"-?\d+\.\d+"},
            {typeof(char),      @"[^/]"},
            // native type object is not supported
            {typeof(string),    @"[^/]*"},
            {typeof(decimal),   @"-?\d+\.\d+"},
        };
        public Regex RoutingRegex { get; private set; }
        private List<MethodInfo> RoutingMethods = new List<MethodInfo>();

        public Webservice()
        {
            GenerateRoutingRegex();
        }

        internal void GenerateRoutingRegex()
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            // Only select methods tagged with a PathAttribute
            foreach (var method in this.GetType().GetMethods().Where(
                         method => method.GetCustomAttributes(typeof(PathAttribute), false).Length > 0))
            {
                // Populate the map of url parameters
                Dictionary<string, string> urlParams = new Dictionary<string, string>();
                foreach (var param in method.GetParameters())
                {
                    if (param.ParameterType == typeof(HttpListenerContext) ||
                        param.ParameterType == typeof(HttpListenerRequest) || 
                        param.ParameterType == typeof(HttpListenerResponse))
                    {
                        continue;
                    }

                    try
                    {
                        urlParams[param.Name] = Webservice.ValidParameterTypes[param.ParameterType];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new InvalidOperationException(String.Format("Webservice {0} parameter {1} is not a native type.", this.GetType().ToString(), param.Name));
                    }
                }

                // Add a new regex match for each path
                foreach (string path in PathAttribute.PathsForMethod(method))
                {
                    if (first)
                        first = false;
                    else
                        sb.Append("|");

                    // Update path regular expression to reflect url parameters
                    string pathRegex = Regex.Escape(path);
                    foreach (var paramKvp in urlParams)
                    {
                        string key = String.Format(":{0}", paramKvp.Key);
                        string regex = String.Format("(?<arg_{0}>{1})", paramKvp.Key, paramKvp.Value);

                        if (!pathRegex.Contains(key))
                            throw new InvalidOperationException(String.Format("Webservice {0} parameter {1} not found in path {2}", this.GetType().Name, key, path));

                        pathRegex = path.Replace(key, regex);
                    }

                    // "Index" pages
                    if (path == "/")
                    {
                        pathRegex = "";
                    }

                    sb.AppendFormat("(?<method_{0}>{1})", method.GetHashCode(), pathRegex);
                    RoutingMethods.Add(method);
                }
            }

            // Update instance variable
            RoutingRegex = new Regex(sb.ToString());
        }

        internal void ProcessRequest(object doneEventObj)
        {
            ManualResetEvent doneEvent = (ManualResetEvent)doneEventObj;
            try
            {
                // TODO: Parse URL Parameters
                // TODO: Invoke Method Call
                // TOOD: Close Connection
            }
            finally
            {
                doneEvent.Set();
            }
        }
    }
}
