using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace TrimbleConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            //listener.Prefixes.Add("http://+:8600/");
            // Service Fabric Environment Variable = "Fabric_Endpoint_<<ServiceEndpointName>>"
            listener.Prefixes.Add(String.Format("http://+:{0}/", Environment.GetEnvironmentVariable("Fabric_Endpoint_GEListenerTestTypeEndpoint")));
            listener.Start();
            Console.WriteLine("Listening...");
            for (; ; )
            {
                HttpListenerContext ctx = listener.GetContext();
                new Thread(new Worker(ctx).ProcessRequest).Start();
            }
        }
    }

    class Worker
    {
        private HttpListenerContext context;

        public Worker(HttpListenerContext context)
        {
            this.context = context;
        }

        public void ProcessRequest()
        {
            string msg = context.Request.HttpMethod + " " + context.Request.Url;
            Console.WriteLine(msg);

            StringBuilder sb = new StringBuilder();
            sb.Append("<html><body><h1>" + msg + "</h1>");
            DumpRequest(context.Request, sb);
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                sb.Append(String.Format("{0} = {1}", de.Key, de.Value));
            }
            sb.Append("</body></html>");

            byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
            context.Response.ContentLength64 = b.Length;
            context.Response.OutputStream.Write(b, 0, b.Length);
            context.Response.OutputStream.Close();
        }

        private void DumpRequest(HttpListenerRequest request, StringBuilder sb)
        {
            DumpObject(request, sb);
        }

        private void DumpObject(object o, StringBuilder sb)
        {
            DumpObject(o, sb, true);
        }

        private void DumpObject(object o, StringBuilder sb, bool ulli)
        {
            if (ulli)
                sb.Append("<ul>");

            if (o is string || o is int || o is long || o is double)
            {
                if (ulli)
                    sb.Append("<li>");

                sb.Append(o.ToString());

                if (ulli)
                    sb.Append("</li>");
            }
            else
            {
                Type t = o.GetType();
                foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    sb.Append("<li><b>" + p.Name + ":</b> ");
                    object val = null;

                    try
                    {
                        val = p.GetValue(o, null);
                    }
                    catch { }

                    if (val is string || val is int || val is long || val is double)
                        sb.Append(val);
                    else

                    if (val != null)
                    {
                        Array arr = val as Array;
                        if (arr == null)
                        {
                            NameValueCollection nv = val as NameValueCollection;
                            if (nv == null)
                            {
                                IEnumerable ie = val as IEnumerable;
                                if (ie == null)
                                    sb.Append(val.ToString());
                                else
                                    foreach (object oo in ie)
                                        DumpObject(oo, sb);
                            }
                            else
                            {
                                sb.Append("<ul>");
                                foreach (string key in nv.AllKeys)
                                {
                                    sb.AppendFormat("<li>{0} = ", key);
                                    DumpObject(nv[key], sb, false);
                                    sb.Append("</li>");
                                }
                                sb.Append("</ul>");
                            }
                        }
                        else
                            foreach (object oo in arr)
                                DumpObject(oo, sb);
                    }
                    else
                    {
                        sb.Append("<i>null</i>");
                    }
                    sb.Append("</li>");
                }
            }
            if (ulli)
                sb.Append("</ul>");
        }
    }
}
