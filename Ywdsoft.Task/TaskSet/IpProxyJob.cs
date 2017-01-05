﻿using Ywdsoft.Task.Utils;
using Ywdsoft.Utility;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using Ywdsoft.Utility.ConfigHandler;

namespace Ywdsoft.Task.TaskSet
{
    /// <summary>
    /// 获取代理IP任务
    /// </summary>
    ///<remarks>DisallowConcurrentExecution属性标记任务不可并行，要是上一任务没运行完即使到了运行时间也不会运行</remarks>
    [DisallowConcurrentExecution]
    public class IpProxyJob : IJob
    {
        /// <summary>
        /// 任务总共执行次数
        /// </summary>
        private static int ExecuteCount = 0;

        /// <summary>
        /// 是否需要切换代理ip
        /// </summary>
        private static bool NeedChangeIP = false;

        /// <summary>
        /// 代理IP
        /// </summary>
        private static string ProxyIp;

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                DateTime start = DateTime.Now;
                TaskLog.IpProxyLogInfo.WriteLogE("\r\n\r\n\r\n\r\n------------------爬虫开始执行获取代理ip任务 " + start.ToString("yyyy-MM-dd HH:mm:ss") + " BEGIN-----------------------------\r\n\r\n");


                //每执行10次任务,换一个代理IP
                if (NeedChangeIP || ExecuteCount % IpProxyConfig.Speed == 0)
                {
                    if (NeedChangeIP)
                    {
                        ExecuteCount = (ExecuteCount / IpProxyConfig.Speed + 1) * IpProxyConfig.Speed;
                    }
                    TaskLog.IpProxyLogInfo.WriteLogE("\r\n\r\n\r\n\r\n------------------开始解析使用的代理ip " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " BEGIN-----------------------------\r\n\r\n");
                    ProxyIp = IpProxyGet.GetCorrectIP();
                    TaskLog.IpProxyLogInfo.WriteLogE("------------------保存使用的代理ip：" + ProxyIp + " -----------------------------");
                    SQLHelper.ExecuteNonQuery("INSERT INTO dbo.p_ProxyIPUseHistory(ProxyIP,Type) VALUES (@ProxyIP,'IpProxyJob')", new { ProxyIP = ProxyIp });
                    NeedChangeIP = false;
                }
                TaskLog.IpProxyLogInfo.WriteLogE("\r\n\r\n\r\n\r\n------------------任务使用的代理ip:" + ProxyIp + "----------------------------\r\n\r\n");

                List<IPProxy> list = IpProxyGet.ParseProxy(ProxyIp);
                if (list.Count == 0)
                {
                    //没有返回数据.表示当前IP已经被锁定需要更换
                    NeedChangeIP = true;
                }

                DateTime end = DateTime.Now;
                ExecuteCount++;
                TaskLog.IpProxyLogInfo.WriteLogE("\r\n\r\n------------------爬虫完成获取代理ip任务:" + end.ToString("yyyy-MM-dd HH:mm:ss") + ",本次共耗时(分):" + (end - start).TotalMinutes + " END------------------------\r\n\r\n\r\n\r\n");
            }
            catch (Exception ex)
            {
                JobExecutionException e2 = new JobExecutionException(ex);
                TaskLog.IpProxyLogError.WriteLogE("爬虫获取代理ip任务异常", ex);
                ExecuteCount++;
                //1.立即重新执行任务 
                e2.RefireImmediately = true;
                //2 立即停止所有相关这个任务的触发器
                //e2.UnscheduleAllTriggers=true; 
            }
        }
    }
}
