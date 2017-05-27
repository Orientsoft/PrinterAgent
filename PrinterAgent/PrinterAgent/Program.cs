using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Web;
using System.Security.Cryptography;
using System.Text;

// report deps
using gregn6Lib;

namespace PrinterAgent
{
    public class Crypto
    {
        public static string keyseed = "9527AA55";
        public static string decode(string encodedString, string key)
        {
            encodedString = encodedString.Replace('-', '+');
            encodedString = encodedString.Replace('*', '/');
            encodedString = encodedString.Replace('^', '=');

            byte[] keyBytes = Encoding.Unicode.GetBytes(key);
            byte[] encodedBytes = Convert.FromBase64String(encodedString.Trim('\0'));

            for (int i = 0; i < encodedBytes.Length; i += 2)
            {
                for (int j = 0; j < keyBytes.Length; j += 2)
                {
                    encodedBytes[i] = Convert.ToByte(encodedBytes[i] ^ keyBytes[j]);
                }
            }

            string decodedString = Encoding.Unicode.GetString(encodedBytes).TrimEnd('\0');
            return decodedString;
        }

        public static string encode(string plainString, string key)
        {
            byte[] keyBytes = Encoding.Unicode.GetBytes(key);
            byte[] plainBytes = Encoding.Unicode.GetBytes(plainString);

            for (int i = 0; i < plainBytes.Length; i += 2)
            {
                for (int j = 0; j < keyBytes.Length; j += 2)
                {
                    plainBytes[i] = Convert.ToByte(plainBytes[i] ^ keyBytes[j]);
                }
            }

            string encodedString = Convert.ToBase64String(plainBytes);
            encodedString.Replace('+', '-');
            encodedString.Replace('/', '*');
            encodedString.Replace('=', '^');

            return encodedString;
        }
    }

    public class JsonpHandler
    {
        public static string handle(HttpListenerRequest req, string resp)
        {
            string callback = req.QueryString[ConfigurationManager.AppSettings["JsonpCallbackName"]];
            string script = callback + "(" + resp + ")";

            return script;
        }
    }

    public class ListenerThread
    {
        private GridppReport Report = new GridppReport();

        private bool listenFlag;
        private HttpListener listener;

        public ListenerThread()
        {
            listenFlag = false;
            listener = new HttpListener();
        }

        ~ListenerThread()
        {
            listener.Close();
        }

        public void start()
        {
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            listener.Prefixes.Add(ConfigurationManager.AppSettings["HttpEndpoint"]);

            listener.Start();
            listenFlag = true;

            while (listenFlag)
            {
                HttpListenerContext ctx = listener.GetContext(); // this is blocking

                string jsonp = null;

                // get params from ctx
                string version = ctx.Request.QueryString["ver"];
                string xmlData = ctx.Request.QueryString["data"];
                string template = ctx.Request.QueryString["tpl"];
                string templateVersion = ctx.Request.QueryString["tplver"];
                string operation = ctx.Request.QueryString["op"];
                int offsetX = 0;
                int offsetY = 0;
                int.TryParse(ctx.Request.QueryString["offx"], out offsetX);
                int.TryParse(ctx.Request.QueryString["offy"], out offsetY);

                /*
                if (ConfigurationManager.AppSettings["Version"] != version)
                {
                    jsonp = JsonpHandler.handle(ctx.Request, "{\"error\": \"API version mismatch.\"}");
                    ctx.Response.StatusCode = 200;
                    response(ctx.Response, jsonp);
                }
                */

                string templateUrl = ConfigurationManager.AppSettings["TemplateEndpoint"] +
                    ConfigurationManager.AppSettings["TemplatePath"] +
                    template + "-" + templateVersion +
                    ConfigurationManager.AppSettings["TemplateExtension"];
                
                try
                {
                    Report.Clear();
                    bool ret = Report.LoadFromURL(templateUrl);
                    // Report.LoadFromFile("D:\\project\\PrinterService\\grf\\IC卡购金额发票.grf");
                    // Report.LoadFromFile("D:\\project\\PrinterAgent\\romulan_grf\\GCSF3.grf");
                    if (!ret)
                    {
                        jsonp = JsonpHandler.handle(ctx.Request, "{\"error\": \"Load template failed.\"}");
                        ctx.Response.StatusCode = 200;
                        response(ctx.Response, jsonp);
                    }
                }
                catch (Exception e)
                {
                    jsonp = JsonpHandler.handle(ctx.Request, "{\"error\": \"" + e.Message + "\"}");
                    ctx.Response.StatusCode = 200;
                    response(ctx.Response, jsonp);
                }
                
                try
                {
                    xmlData = Crypto.decode(xmlData, Crypto.keyseed);
                    // MessageBox.Show(xmlData);
                    // xmlData = "<xml><row><流水编号>54321</流水编号><开票日期>2017-4-12</开票日期><用户类型>帅锅</用户类型><用户名称>王碧林</用户名称><用户编码>12345</用户编码><地址>通美大厦</地址></row></xml>";
                    // xmlData = "<xml><master><XMSFJL_XMID>12345</XMSFJL_XMID></master><row><SFJLMX_ShoufeiXiangmu>a</SFJLMX_ShoufeiXiangmu></row><row><SFJLMX_ShoufeiXiangmu>b</SFJLMX_ShoufeiXiangmu></row></xml>";
                    bool ret = Report.LoadDataFromXML(xmlData);
                    if (!ret)
                    {
                        jsonp = JsonpHandler.handle(ctx.Request, "{\"error\": \"Load data failed.\"}");
                        ctx.Response.StatusCode = 200;
                        response(ctx.Response, jsonp);
                    }
                }
                catch (Exception e)
                {
                    jsonp = JsonpHandler.handle(ctx.Request, "{\"error\": \"" + e.Message + "\"}");
                    ctx.Response.StatusCode = 200;
                    response(ctx.Response, jsonp);
                }

                try
                {
                    // do work
                    switch (operation)
                    {
                        case "print":
                            Report.Print(false);
                            jsonp = JsonpHandler.handle(ctx.Request, "{\"result\": \"OK\"}");
                            ctx.Response.StatusCode = 200;
                            break;

                        case "preview":
                            Report.PrintPreview(true);
                            jsonp = JsonpHandler.handle(ctx.Request, "{\"result\": \"OK\"}");
                            ctx.Response.StatusCode = 200;
                            break;

                        default:
                            jsonp = JsonpHandler.handle(ctx.Request, "{\"error\":\"Unknown operation\"}");
                            ctx.Response.StatusCode = 200;
                            break;
                    }
                }
                catch (Exception e)
                {
                    jsonp = JsonpHandler.handle(ctx.Request, "{\"error\": \"" + e.Message + "\"}");
                    ctx.Response.StatusCode = 200;
                }

                response(ctx.Response, jsonp);
            } // while (listenFlag)

            listener.Stop();
        }

        public void stop()
        {
            listenFlag = false;
        }

        private void response(HttpListenerResponse response, string jsonp)
        {
            // write response        
            using (StreamWriter writer = new StreamWriter(response.OutputStream))
            {
                if (jsonp != null)
                    writer.Write(jsonp);

                writer.Close();
            }

            // close connection
            response.Close();
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // start listener thread
            ListenerThread listenerThread;
            listenerThread = new ListenerThread();
            Thread thread = new Thread(new ThreadStart(listenerThread.start));

            thread.IsBackground = true;
            thread.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
