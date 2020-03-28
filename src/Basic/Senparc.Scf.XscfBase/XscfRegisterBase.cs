﻿using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Scf.Core.Areas;
using Senparc.Scf.Core.Enums;
using Senparc.Scf.Core.Models;
using Senparc.Scf.XscfBase.Database;
using Senparc.Scf.XscfBase.Threads;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Scf.XscfBase
{
    /// <summary>
    /// 所有 XSCF 模块注册的基类
    /// </summary>
    public abstract class XscfRegisterBase : IXscfRegister
    {
        /// <summary>
        /// 是否忽略安装（但不影响执行注册代码），默认为 false
        /// </summary>
        public virtual bool IgnoreInstall { get; }

        /// <summary>
        /// 模块名称，要求全局唯一
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// 编号，要求全局唯一
        /// </summary>
        public abstract string Uid { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        public abstract string Version { get; }
        /// <summary>
        /// 菜单名称
        /// </summary>
        public abstract string MenuName { get; }
        /// <summary>
        /// Icon图标
        /// </summary>
        public abstract string Icon { get; }
        /// <summary>
        /// 说明
        /// </summary>
        public abstract string Description { get; }
        /// <summary>
        /// 注册方法，注册的顺序决定了界面中排列的顺序
        /// </summary>
        public abstract IList<Type> Functions { get; }

        /// <summary>
        /// 添加 AutoMap 映射
        /// </summary>
        public virtual ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }
        /// <summary>
        /// 获取当前模块的已注册线程信息
        /// </summary>
        public IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo => Register.ThreadCollection.Where(z => z.Value.Name.StartsWith(Uid));


        /// <summary>
        /// 执行 Migrate 更新数据
        /// </summary>
        /// <typeparam name="TSenparcEntities"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        protected virtual async Task MigrateDatabaseAsync<TSenparcEntities>(IServiceProvider serviceProvider)
            where TSenparcEntities : XscfDatabaseDbContext
        {
            var mySenparcEntities = serviceProvider.GetService<TSenparcEntities>();
            await mySenparcEntities.Database.MigrateAsync().ConfigureAwait(false);//更新数据库

            //if (!await mySenparcEntities.Database.EnsureCreatedAsync().ConfigureAwait(false))
            //{
            //    throw new ScfModuleException($"更新数据库失败：{typeof(TSenparcEntities).Name}");
            //}
        }

        /// <summary>
        /// 安装代码
        /// </summary>
        public virtual Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// 卸载代码
        /// </summary>
        public virtual async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        /// <summary>
        /// 删除表（此方法请慎重使用！）
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="databaseDbContext"></param>
        /// <param name="entityType">需要删除的表所对应的实体类型</param>
        /// <returns></returns>
        protected virtual async Task DropTablesAsync(IServiceProvider serviceProvider, XscfDatabaseDbContext databaseDbContext, Type[] entityType)
        {
            SenparcTrace.SendCustomLog("开始删除应用表格", MenuName + ", " + Name);
            var appliedMigrations = databaseDbContext.Database.GetAppliedMigrations();
            if (appliedMigrations.Count() > 0)
            {
                using (await databaseDbContext.Database.BeginTransactionAsync())
                {
                    //mySenparcEntities.Database.GetService<>
                }
                //var databaseCreator = mySenparcEntities.Database.GetService<IRelationalDatabaseCreator>();


                foreach (var type in entityType)
                {
                    var schma = databaseDbContext.Model.FindEntityType(type).GetSchema();
                    var tableName = databaseDbContext.Model.FindEntityType(type).GetTableName();
                    SenparcTrace.SendCustomLog("开始删除表格", $"[schma].[tableName]：[{schma}].[{tableName}]");
                    //mySenparcEntities.Colors.FromSqlRaw($"DELETE FROM [{key}]");

                    string fullTableName = $"[{tableName}]";
                    if (!schma.IsNullOrEmpty())
                    {
                        fullTableName = $"[{schma}].{fullTableName}";
                    }

                    int keyExeCount = await databaseDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE {fullTableName}");
                    SenparcTrace.SendCustomLog("影响行数", keyExeCount + " 行");
                }

                //删除 Migration 记录
                var migrationHistoryTableName = GetDatabaseMigrationHistoryTableName();
                SenparcTrace.SendCustomLog("开始删除 DatabaseMigrationHistory 表格", $"[{migrationHistoryTableName}]");
                int historyExeCount = await databaseDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE [{migrationHistoryTableName}]");
                SenparcTrace.SendCustomLog("影响行数", historyExeCount + " 行");
            }
        }

        /// <summary>
        /// 获取首页Url
        /// <para>仅限实现了 IAreaRegister 接口之后的 Register，否则将返回 null</para>
        /// </summary>
        /// <returns></returns>
        public virtual string GetAreaHomeUrl()
        {
            if (this is IAreaRegister)
            {
                var homeUrl = (this as IAreaRegister).HomeUrl;
                return GetAreaUrl(homeUrl);
            }
            return null;
        }
        /// <summary>
        /// 获取指定网页的Url
        /// <para>仅限实现了 IAreaRegister 接口之后的 Register，否则将返回 null</para>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual string GetAreaUrl(string path)
        {
            if (this is IAreaRegister)
            {
                if (path == null)
                {
                    return "/";
                }

                path += path.Contains("?") ? "&" : "?";
                path += $"uid={Uid}";
                return path;
            }
            return null;
        }

        /// <summary>
        /// 添加模块
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public virtual IServiceCollection AddXscfModule(IServiceCollection services, IConfiguration configuration)
        {
            if (this is IXscfDatabase databaseRegister)
            {
                //定义 XscfSenparcEntities 实例生成
                Func<IServiceProvider, object> implementationFactory = s =>
                {
                    //准备创建 DbContextOptionsBuilder 实例，定义类型
                    var dbOptionBuilderType = typeof(DbContextOptionsBuilder<>);
                    //获取泛型对象类型，如：DbContextOptionsBuilder<SenparcEntity>
                    dbOptionBuilderType = dbOptionBuilderType.MakeGenericType(databaseRegister.XscfDatabaseDbContextType);
                    //创建 DbContextOptionsBuilder 实例
                    DbContextOptionsBuilder dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder;
                    //继续定义配置
                    dbOptionBuilder = SqlServerDbContextOptionsExtensions.UseSqlServer(dbOptionBuilder, Scf.Core.Config.SenparcDatabaseConfigs.ClientConnectionString,
                        b =>
                        {
                            databaseRegister.DbContextOptionsAction(b, null);
                            b.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(5),
                                errorNumbersToAdd: new int[] { 2 });
                        });
                    //创建 SenparcEntities 实例
                    var xscfSenparcEntities = Activator.CreateInstance(databaseRegister.XscfDatabaseDbContextType, new object[] { dbOptionBuilder.Options });
                    return xscfSenparcEntities;
                };
                //添加 XscfSenparcEntities 依赖注入配置
                services.AddScoped(databaseRegister.XscfDatabaseDbContextType, implementationFactory);
                //注册当前数据库的对象（必须）
                EntitySetKeys.TryLoadSetInfo(databaseRegister.XscfDatabaseDbContextType);

                //添加数据库相关注册过程
                databaseRegister.AddXscfDatabaseModule(services);
            }
            return services;
        }

        /// <summary>
        /// 在 startup.cs 的 Configure() 方法中执行配置
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService"></param>
        /// <returns></returns>
        public virtual IApplicationBuilder UseXscfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            return app;
        }

        public static object AddAutoMapMappingLock = new object();
        public virtual void AddAutoMapMapping(Action<Profile> mapping)
        {
            if (AutoMapMappingConfigs == null)
            {
                lock (AddAutoMapMappingLock)
                {
                    if (AutoMapMappingConfigs == null)
                    {
                        AutoMapMappingConfigs = new ConcurrentBag<Action<Profile>>();
                    }
                }
            }
            AutoMapMappingConfigs.Add(mapping);
        }

        /// <summary>
        /// 获取 EF Code First MigrationHistory 数据库表名
        /// </summary>
        /// <returns></returns>
        public virtual string GetDatabaseMigrationHistoryTableName()
        {
            if (this is IXscfDatabase databaseRegiser)
            {
                return "__" + databaseRegiser.DatabaseUniquePrefix + "_EFMigrationsHistory";
            }
            return null;
        }


        /// <summary>
        /// 数据库 DbContext 选项配置
        /// </summary>
        /// <param name="dbContextOptionsAction"></param>
        /// <param name="assemblyName">MigrationsAssembly 的程序集名称，如果为 null，为默认使用当前 XscfDatabaseDbContextType 所在的程序集</param>
        public virtual void DbContextOptionsAction(IRelationalDbContextOptionsBuilderInfrastructure dbContextOptionsAction,
                                                   string assemblyName = null)
        {
            if (this is IXscfDatabase databaseRegiser)
            {
                if (dbContextOptionsAction is SqlServerDbContextOptionsBuilder sqlServerOptionsAction)
                {
                    var senparcEntitiesAssemblyName = assemblyName ?? databaseRegiser.XscfDatabaseDbContextType.Assembly.FullName;
                    var databaseMigrationHistoryTableName = GetDatabaseMigrationHistoryTableName();

                    sqlServerOptionsAction
                        .MigrationsAssembly(senparcEntitiesAssemblyName)
                        .MigrationsHistoryTable(databaseMigrationHistoryTableName);
                }

                //可以支持其他更多数据库
            }
        }
    }
}
