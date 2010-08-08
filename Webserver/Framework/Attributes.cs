using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

// Attributes.cs is inspired by Kayak.Framework
// Portions Copyright 2010 by Benjamin van der Veen (MIT License)

namespace FortAwesomeUtil.Webserver.Framework
{
    /// <summary>
    /// Decorate a method with this attribute to indicate that it should be invoked to handle
    /// requests for a given path.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class PathAttribute : Attribute
    {
        public string Path { get; private set; }
        public PathAttribute(string path) { Path = path; }

        public static string[] PathsForMethod(MethodInfo method)
        {
            return (from pathAttr in method.GetCustomAttributes(typeof(PathAttribute), false)
                    select (pathAttr as PathAttribute).Path).ToArray();
        }
    }

    /// <summary>
    /// This attribute is used in conjunction with the [Path] attribute to indicate that the method should be
    /// invoked in response to requests for the path with a particular HTTP verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class VerbAttribute : Attribute
    {
        public string Verb { get; private set; }
        public VerbAttribute(string verb) { Verb = verb; }

        public static string[] VerbsForMethod(MethodInfo method)
        {
            var result = (from verbAttr in method.GetCustomAttributes(typeof(VerbAttribute), false)
                          select (verbAttr as VerbAttribute).Verb).ToArray();

            // if there are no explicit verb attributes, GET is implied.
            if (result.Length == 0) return new string[] { "GET" };

            return result;
        }
    }
}
