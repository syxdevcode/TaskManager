﻿using CsharpHttpHelper;
using HtmlAgilityPack;
using Ywdsoft.Task.Utils;
using Ywdsoft.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using System.Threading;
using Ywdsoft.Utility.ConfigHandler;
using System.Net;
using System.Text;
using System.IO;

namespace Ywdsoft.Task.Utils
{
    /// <summary>
    /// 从ip代理站点(http://www.kuaidaili.com/free/inha)获取所有代理ip
    /// </summary>
    public static class IpProxyGet1
    {
        /// <summary>
        /// CPU数量
        /// </summary>
        private static readonly int CPUCount = Convert.ToInt32(Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS"));

        //创建Httphelper对象
        private static HttpHelper http = new HttpHelper();

        public static List<IPProxy> ParseProxy(string ProxyIp)
        {
            if (string.IsNullOrEmpty(IpProxyConfig.IPUrl))
            {
                throw new ArgumentNullException("ParseProxy函数参数空异常");
            }

            //总页数
            int total = GetTotalPage(IpProxyConfig.IPUrl, ProxyIp);

            //返回结果
            List<IPProxy> list = new List<IPProxy>();

            //多线程进行解析获取
            List<Thread> listThread = new List<Thread>();

            //每个线程需要解析的页面数量
            int threadPqgeSize = (total / CPUCount) + 1;
            int count = 0;
            //为每个线程准备参数
            List<Hashtable> threadParams = new List<Hashtable>();
            int start, end;
            Hashtable table = null;
            for (int i = 0; i < CPUCount; i++)
            {
                start = i * threadPqgeSize + 1;
                if (i == CPUCount - 1)
                {
                    end = total;
                }
                else
                {
                    end = start + threadPqgeSize;
                }
                table = new Hashtable();
                table.Add("start", start);
                table.Add("end", end);
                table.Add("list", list);
                table.Add("param", ProxyIp);
                threadParams.Add(table);

                count += threadPqgeSize;
            }

            for (int i = 1; i < CPUCount; i++)
            {
                Thread thread = new Thread(DoWork);
                thread.IsBackground = true;
                thread.Name = "PageParse #" + i.ToString();
                listThread.Add(thread);
                thread.Start(threadParams[i]);
            }

            // 为当前线程指派生成任务。
            DoWork(threadParams[0]);

            // 等待所有的编译线程执行线束。
            foreach (Thread thread in listThread)
            {
                thread.Join();
            }
            if (list.Count == 0)
            {
                TaskLog.IpProxyLogInfo.WriteLogE("爬虫-代理ip任务", new Exception("没有获取到数据,可能当前ip(" + ProxyIp + ")已被服务器封锁"));
            }
            else
            {
                BatchSaveData(list);
            }
            return list;
        }

        /// <summary>
        /// 解析每一页数据
        /// </summary>
        /// <param name="param"></param>
        private static void DoWork(object param)
        {
            //参数还原
            Hashtable table = param as Hashtable;
            int start = Convert.ToInt32(table["start"]);
            int end = Convert.ToInt32(table["end"]);
            List<IPProxy> list = table["list"] as List<IPProxy>;
            string ProxyIp = table["param"] as string;

            //页面地址
            string url = string.Empty;
            string ip = string.Empty;
            IPProxy item = null;
            HtmlNodeCollection nodes = null;
            HtmlNode node = null;
            HtmlAttribute atr = null;
            for (int i = start; i <= end; i++)
            {
                TaskLog.IpProxyLogInfo.WriteLogE(string.Format("开始解析,页码{0}~{1},当前页码{2}", start, end, i));
                url = string.Format("{0}/{1}", IpProxyConfig.IPUrl, i);
                var doc = new HtmlDocument();
                doc.LoadHtml(GetUrltoHtml(url, ProxyIp));
                //获取所有数据节点tr
                var trs = doc.DocumentNode.SelectNodes(@"//table[@class='table table-bordered table-striped']/tbody/tr");
                if (trs != null && trs.Count > 1)
                {
                    TaskLog.IpProxyLogInfo.WriteLogE(string.Format("当前页码{0},请求地址{1},共{2}条数据", i, url, trs.Count));
                    for (int j = 0; j <= trs.Count; j++)
                    {
                        nodes = trs[j].SelectNodes("td");
                        if (nodes != null && nodes.Count > 6)
                        {
                            ip = nodes[0].InnerText.Trim();
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
                            list.Add(item);
                        }
                    }
                    TaskLog.IpProxyLogInfo.WriteLogE(string.Format("当前页码{0},共{1}条数据", i, trs.Count));
                }
                TaskLog.IpProxyLogInfo.WriteLogE(string.Format("结束解析,页码{0}~{1},当前页码{2}", start, end, i));
            }
        }

        /// <summary>
        /// 获取总页数
        /// </summary>
        /// <returns>总页数</returns>
        private static int GetTotalPage(string IPURL, string ProxyIp)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(GetUrltoHtml(IPURL, ProxyIp));
            var res = doc.DocumentNode.SelectNodes(@"//div[@id='listnav']/ul/li");
            if (res != null && res.Count > 2)
            {
                int page;
                if (int.TryParse(res[res.Count - 2].InnerText, out page))
                {
                    return page;
                }
            }
            return 1;
        }

        /// <summary>
        /// 使用代理ip获取页面html内容
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetUrltoHtml(string url, string ip,string type = "UTF-8")
        {
            string resultPage = string.Empty;
            try
            {
                string postStr = "";
                byte[] postData = Encoding.ASCII.GetBytes(postStr);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                //禁止重定向
                request.AllowAutoRedirect = false;
                request.ContentType = "application/x-www-form-urlencoded;charset=gbk";
                request.CookieContainer = new CookieContainer();
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.95 Safari/537.11";
                if (!string.IsNullOrEmpty(ip))
                {
                    var arr = ip.Split(':');
                    if (arr.Length < 2)
                        return "ip格式错误";
                    WebProxy myProxy = new WebProxy(arr[0].ToString(), Convert.ToInt32(arr[1]));
                    //设置请求使用代理信息
                    request.Proxy = myProxy;
                }
                request.Timeout = 5000;
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;
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

        /// <summary>
        /// 获取页面html内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetHTML(string url, string ProxyIp)
        {
            try
            {
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
            catch (Exception ex)
            {
                TaskLog.IpProxyLogError.WriteLogE(string.Format("url:{0},ip:{1}获取HTML内容出错", url, ProxyIp), ex);
                return "<HTML></HTML>";
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="list"></param>
        private static void BatchSaveData(List<IPProxy> list)
        {
            try
            {
                SQLHelper.ExecuteNonQuery("Truncate TABLE p_IPProxy");
                SQLHelper.BatchSaveData(list, "p_IPProxy");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

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
        /// 通过IP和端口号组成代理ip
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private static string GetIP(string ip, string port)
        {
            return string.Format("{0}:{1}", ip, port);
        }
        /// <summary>
        /// 获取正确的代理ip
        /// </summary>
        /// <param name="Param">爬取参数</param>
        /// <returns>正确的代理ip</returns>
        public static string GetCorrectIP()
        {
            string ProxyIp = string.Empty;
            string tempProxyIp = string.Empty;
            //当前页
            int currentPage = 1;
            int PageSize = 100;
            while (string.IsNullOrEmpty(ProxyIp))
            {
                //从数据库取
                string strSQL = string.Format(@"SELECT IP,Port FROM (
                                    SELECT IP,Port,ROW_NUMBER() OVER(ORDER BY Speed) AS Num FROM dbo.p_IPProxy WHERE Type='HTTP' AND ProxyIp NOT IN(SELECT ProxyIP FROM dbo.p_ProxyIPUseHistory WHERE Type='IpProxyJob' AND CreateDay=CONVERT(VARCHAR(10),GETDATE(),120))
                                 ) AS A
                                WHERE  Num BETWEEN {0} AND {1}", (currentPage - 1) * PageSize + 1, currentPage * PageSize);
                DataTable dt = SQLHelper.FillDataTable(strSQL);
                if (dt == null || dt.Rows.Count == 0)
                {
                    break;
                }
                foreach (DataRow dr in dt.Rows)
                {
                    tempProxyIp = GetIP(dr["IP"].ToString(), dr["Port"].ToString());
                    TaskLog.IpProxyLogInfo.WriteLogE("当前IP:" + tempProxyIp);
                    if (Ping(dr["IP"].ToString()) && GetTotalPage(IpProxyConfig.IPUrl, tempProxyIp) > 1)
                    {
                        ProxyIp = tempProxyIp;
                        break;
                    }
                }
                currentPage++;
            }
            if (string.IsNullOrEmpty(ProxyIp))
            {
                ProxyIp = IpProxyConfig.DefaultProxyIp;
            }
            return ProxyIp;
        }
    }
}
