using CsharpHttpHelper;
using HtmlAgilityPack;
using Ywdsoft.Utility;
using Nancy.Hosting.Self;
using Owin_Nancy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using Ywdsoft.Utility.Mef;
using Ywdsoft.Utility.ConfigHandler;
using System.Net.NetworkInformation;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;

namespace Ywdsoft.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //var str = GetUrltoHtml("http://www.kuaidaili.com/free/inha");
            //var str1 = PostForm("http://www.kuaidaili.com/free/inha");

            AdminRun.Run();
            //1.MEF初始化
            MefConfig.Init();

            //2.
            ConfigInit.InitConfig();

            //3.系统参数配置初始化
            ConfigManager configManager = MefConfig.TryResolve<ConfigManager>();
            configManager.Init();


            QuartzHelper.InitScheduler();
            QuartzHelper.StartScheduler();
            try
            {
                //启动站点
                using (NancyHost host = Startup.Start(SystemConfig.WebPort))
                {
                    //调用系统默认的浏览器   
                    string url = string.Format("http://127.0.0.1:{0}", SystemConfig.WebPort);
                    Process.Start(url);
                    Console.WriteLine("系统监听站点地址:{0}", url);
                    Console.WriteLine("程序已启动,按任意键退出");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //GetProxyIpList();
            //ParseExpressCode();
            //ExpressUtil.HandleProecssInfo("");
            //MessageHelper.SendMessage(new Guid("6282AA73-2A58-E511-8D70-00155D0C740D"));
            //new Ywdsoft.Task.TaskSet.SendMessageJob().Execute(null);

            Console.Read();
        }

        /*
        public static void GetProxyIpList()
        {
            IPProxy item = null;
            HtmlNodeCollection nodes = null;
            HtmlNode node = null;
            HtmlAttribute atr = null;

            var url = string.Format("{0}/{1}", IpProxyConfig.IPUrl, 1);
            var doc = new HtmlDocument();
            doc.LoadHtml(GetHTML(url, ""));
            //获取所有数据节点tr
            var trs = doc.DocumentNode.SelectNodes(@"//table[@class='table table-bordered table-striped']/tbody/tr");
            if (trs != null && trs.Count > 1)
            {
                for (int j = 1; j < trs.Count; j++)
                {
                    nodes = trs[j].SelectNodes("td");
                    if (nodes != null)
                    {
                        var ip = nodes[0].InnerText.Trim();
                        if (IpProxyConfig.IsPingIp && !Ping(ip))
                        {
                            continue;
                        }
                        //有效的IP才添加
                        item = new IPProxy();

                        item.IP = ip;
                        item.Port = nodes[1].InnerText.Trim();
                        item.Anonymity = nodes[2].InnerText.Trim();
                        item.ProxyIp = string.Format("{0}:{1}", item.IP, item.Port);
                        item.Position = nodes[4].InnerText.Trim();
                        item.Type = nodes[3].InnerText.Trim();
                        item.Speed = nodes[5].InnerText.Trim();
                        item.VerifyTime = nodes[6].InnerText.Trim();

                    }
                }
            }
        }
        */

        /// <summary>  
        /// 是否能 Ping 通指定的主机  
        /// </summary>  
        /// <param name="ip">ip 地址或主机名或域名</param>  
        /// <returns>true 通，false 不通</returns>  
        private static bool Ping(string ip)
        {
            Ping p = new Ping();
            int timeout = 1000;
            PingReply reply = p.Send(ip, timeout);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }

        /// <summary>
        /// 支持请求启用cookie
        /// </summary>
        /// <param name="url"></param>
        /// <param name="form"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string PostForm(string url, string type = "UTF-8")
        {
            string resultPage = string.Empty;
            try
            {
                CookieContainer cookies = new CookieContainer();
                string postStr = "";
                byte[] postData = Encoding.ASCII.GetBytes(postStr);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                //禁止重定向
                request.AllowAutoRedirect = false;
                request.ContentType = "application/x-www-form-urlencoded;charset=gbk";
                request.CookieContainer = new CookieContainer();
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.95 Safari/537.11";

                var arr = "122.224.109.109:80".Split(':');
                if (arr.Length < 2)
                    return "ip格式错误";
                WebProxy myProxy = new WebProxy(arr[0].ToString(), Convert.ToInt32(arr[1]));
                request.Timeout = 5000;
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;
                //设置请求使用代理信息
                //request.Proxy = myProxy;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(postData, 0, postData.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                using (System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.GetEncoding(type)))
                {
                    resultPage = reader.ReadToEnd();
                }

                string cookie = response.Headers.Get("Set-Cookie");
                //string html = getHtml(GetCookieName(cookie), GetCookieValue(cookie));
                responseStream.Close();
                return resultPage;
            }
            catch (Exception ex)
            {
                resultPage = ex.Message;
            }
            return resultPage;
        }
        private static string GetCookieValue(string cookie)
        {
            Regex regex = new Regex("=.*?;");
            Match value = regex.Match(cookie);
            string cookieValue = value.Groups[0].Value;
            return cookieValue.Substring(1, cookieValue.Length - 2);
        }

        private static string GetCookieName(string cookie)
        {
            Regex regex = new Regex("sulcmiswebpac.*?");
            Match value = regex.Match(cookie);
            return value.Groups[0].Value;
        }

        private static string getHtml(string name, string value)
        {
            CookieCollection cookies = new CookieCollection();
            cookies.Add(new Cookie(name, value));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://222.200.98.171:81/user/bookborrowed.aspx");
            request.Method = "GET";
            request.Headers.Add("Cookie", name + "=" + value);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// 代理使用示例
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetUrltoHtml(string Url, string type = "UTF-8")
        {
            try
            {
                //CookieContainer myCookieContainer = GetCookies();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                request.Method = "Get";
                //WebProxy myProxy = new WebProxy("192.168.15.11", 8015);
                WebProxy myProxy = new WebProxy("122.224.109.109", 80);
                request.Timeout = 5000;
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;
                //建议连接（代理需要身份认证，才需要用户名密码）
                //myProxy.Credentials = new NetworkCredential("admin", "123456");

                //设置请求使用代理信息
                request.Proxy = myProxy;
                // Get the response instance.
                System.Net.WebResponse wResp = request.GetResponse();
                System.IO.Stream respStream = wResp.GetResponseStream();
                // Dim reader As StreamReader = New StreamReader(respStream)
                using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.GetEncoding(type)))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                //errorMsg = ex.Message;
            }
            return "";
        }

        //private static CookieContainer GetCookies()
        //{
        //    CookieContainer myCookieContainer = new CookieContainer();

        //    HttpCookie requestCookie;
        //    int requestCookiesCount = System.Web.HttpRequest.Cookies.Count;
        //    for (int i = 0; i < requestCookiesCount; i++)
        //    {
        //        requestCookie = System.Web.HttpContext.Current.Request.Cookies[i];
        //        Cookie clientCookie = new Cookie(requestCookie.Name, requestCookie.Value, requestCookie.Path, requestCookie.Domain == null ? System.Web.HttpContext.Current.Request.Url.Host : requestCookie.Domain);
        //        myCookieContainer.Add(clientCookie);
        //    }
        //    return myCookieContainer;
        //}

        /// <summary>
        /// 获取快递公司列表
        /// </summary>
        private static void ParseExpressCode()
        {
            string HTML = GetHTML("http://m.kuaidi100.com/all/", "61.234.249.107:8118");
            var doc = new HtmlDocument();
            doc.LoadHtml(HTML);
            var coms = doc.DocumentNode.SelectNodes(@"//dl[@id='comList']/dd/a");
            List<ExpressCom> list = new List<ExpressCom>();
            HtmlAttribute atr = null;
            foreach (var node in coms)
            {
                ExpressCom item = new ExpressCom();
                atr = node.Attributes["data-code"];
                if (atr != null)
                {
                    item.CompanyCode = atr.Value.Trim();
                }

                item.CompanyName = node.InnerText.Trim();
                list.Add(item);
            }
            if (list.Count > 0)
            {
                SQLHelper.ExecuteNonQuery("Truncate TABLE p_ExpressCompany");
                SQLHelper.BatchSaveData(list, "p_ExpressCompany");
            }
        }

        /// <summary>
        /// 获取页面html内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetHTML(string url, string ProxyIp)
        {
            try
            {
                HttpHelper http = new HttpHelper();
                //创建Httphelper参数对象
                HttpItem item = new HttpItem()
                {
                    URL = url,//URL     必需项    
                    Method = "get",//可选项 默认为Get   
                    ContentType = "text/html",//返回类型    可选项有默认值 
                    ProxyIp = ProxyIp
                };
                //请求的返回值对象
                HttpResult result = http.GetHtml(item);
                return result.Html;
            }
            catch (Exception)
            {
                return "<HTML></HTML>";
            }
        }
    }

    public class ExpressCom
    {
        public string CompanyName { get; set; }

        public string CompanyCode { get; set; }
    }
}
