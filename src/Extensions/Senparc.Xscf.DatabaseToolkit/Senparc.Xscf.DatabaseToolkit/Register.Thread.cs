﻿using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Scf.Service;
using Senparc.Scf.XscfBase;
using Senparc.Scf.XscfBase.Threads;
using Senparc.Xscf.DatabaseToolkit.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xscf.DatabaseToolkit
{
    public partial class Register : IXscfThread
    {
        public void ThreadConfig(XscfThreadBuilder xscfThreadBuilder)
        {
            xscfThreadBuilder.AddThreadInfo(new Scf.XscfBase.Threads.ThreadInfo(
                name: "定时备份",
                intervalTime: TimeSpan.FromSeconds(10),
                task: async (app, threadInfo) =>
                {
                    try
                    {
                        //SenparcTrace.SendCustomLog("执行调试", "DatabaseToolkit.Register.ThreadConfig");
                        threadInfo.RecordStory("开始检测并备份");

                        using (var scope = app.ApplicationServices.CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;
                            //初始化数据库备份方法
                            BackupDatabase backupDatabase = new BackupDatabase(serviceProvider);
                            //初始化参数
                            var backupParam = new BackupDatabase.BackupDatabase_Parameters();
                            var dbConfigService = serviceProvider.GetService<ServiceBase<DbConfig>>();
                            var dbConfig = await dbConfigService.GetObjectAsync(z => true);
                            var stopBackup = false;
                            try
                            {
                                if (dbConfig != null && dbConfig.BackupCycleMinutes > 0 && !dbConfig.BackupPath.IsNullOrEmpty())
                                {
                                    if (!dbConfig.LastBackupTime.HasValue || SystemTime.NowDiff(dbConfig.LastBackupTime.Value) > TimeSpan.FromMinutes(dbConfig.BackupCycleMinutes))
                                    {
                                        backupParam.Path = dbConfig.BackupPath;
                                        //await backupParam.LoadData(serviceProvider);
                                        //threadInfo.RecordStory("完成备份设置数据载入");
                                    }
                                    else
                                    {
                                        stopBackup = true;
                                    }
                                }
                                else
                                {
                                    threadInfo.RecordStory("不需要备份，或没有设置备份周期/路径，已忽略本次备份计划");
                                    stopBackup = true;//不需要备份，或没有设置，返回
                                }
                            }
                            catch (Exception ex)
                            {
                                threadInfo.RecordStory(@$"遇到异常，可能未配置数据库，已忽略本次备份计划。如需启动，请更新此模块到最新版本。
异常信息：{ex.Message}
{ex.StackTrace}");
                                stopBackup = true;//可能没有配置数据库，返回
                            }

                            if (stopBackup)
                            {
                                return;
                            }


                            //执行备份方法
                            threadInfo.RecordStory("备份开始：" + backupParam.Path);
                            var result = backupDatabase.Run(backupParam);
                            if (!result.Success)
                            {
                                threadInfo.RecordStory("执行备份发生异常：" + result.Message);
                                throw new Exception("执行备份发生异常");
                            }

                            dbConfig.RecordBackupTime();
                            await dbConfigService.SaveObjectAsync(dbConfig);

                            threadInfo.RecordStory("完成数据库自动备份：" + result.Message);
                            SenparcTrace.SendCustomLog("完成数据库自动备份", backupParam.Path);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        threadInfo.RecordStory("检测并备份结束");
                    }
                },
                exceptionHandler: ex =>
                {
                    SenparcTrace.SendCustomLog("DatabaseToolkit", @$"{ex.Message}
{ex.StackTrace}
{ex.InnerException?.StackTrace}");
                    return Task.CompletedTask;
                }));
        }
    }
}
