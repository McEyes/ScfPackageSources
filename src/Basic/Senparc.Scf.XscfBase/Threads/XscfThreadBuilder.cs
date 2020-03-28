﻿using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Scf.XscfBase.Threads
{
    /// <summary>
    /// XSCF Thread 模块，线程配置
    /// </summary>
    public class XscfThreadBuilder
    {
        private List<ThreadInfo> _threadInfoList = new List<ThreadInfo>();
        public void AddThreadInfo(ThreadInfo threadInfo)
        {
            _threadInfoList.Add(threadInfo);
        }

        internal void Build(IApplicationBuilder app, IXscfRegister register)
        {
            var threadRegister = register as IXscfThread;
            if (threadRegister == null)
            {
                return;
            }

            var i = 0;
            //遍历单个 XSCF 内所有线程配置
            foreach (var threadInfo in _threadInfoList)
            {
                if (threadInfo.Task == null)
                {
                    continue;
                }
                try
                {
                    i++;
                    //定义线程
                    Thread thread = new Thread(async () =>
                    {
                        SenparcTrace.SendCustomLog("启动线程", $"{register.Name}-{threadInfo.Name}");
                        await Task.Delay(TimeSpan.FromSeconds(i));
                        while (true)
                        {
                            try
                            {
                                await threadInfo.Task.Invoke(app, threadInfo);
                                // 建议开发者自己在内部做好线程内的异常处理
                            }
                            catch (Exception ex)
                            {
                                SenparcTrace.BaseExceptionLog(ex);
                                await threadInfo.ExceptionHandler?.Invoke(ex);
                            }
                            //进行延迟
                            await Task.Delay(threadInfo.IntervalTime);
                        }
                    });
                    thread.Name = $"{register.Uid}-{threadInfo.Name ?? Guid.NewGuid().ToString()}";
                    thread.Start();//启动
                    Register.ThreadCollection[threadInfo] = thread;
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }
            }
        }
    }
}
