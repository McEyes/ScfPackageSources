﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Scf.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Senparc.Scf.XscfBase.Database
{
    /// <summary>
    /// 提供给数据库 Migration 使用的 DesignTimeDbContextFactory
    /// </summary>
    /// <typeparam name="TSenparcEntities"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TSenparcEntities, TXscfDatabaseRegister>
        : IDesignTimeDbContextFactory<TSenparcEntities>
            where TSenparcEntities : XscfDatabaseDbContext
            where TXscfDatabaseRegister : class, IXscfDatabase, new()
    {
        public virtual string RootDictionaryPath => Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\");

        public virtual SenparcSetting SenparcSetting => new SenparcSetting();

        public virtual string SqlConnectionStr => SenparcDatabaseConfigs.ClientConnectionString ?? "Server=.\\;Database=SCF;Trusted_Connection=True;integrated security=True;";

        public virtual TSenparcEntities GetInstance(DbContextOptions<TSenparcEntities> dbContextOptions)
        {
            //获取 XscfDatabase 对象
            var databaseRegister = Activator.CreateInstance(typeof(TXscfDatabaseRegister)) as TXscfDatabaseRegister;
            //获取 XscfSenparcEntities 实例
            var xscfSenparcEntities = Activator.CreateInstance(databaseRegister.XscfDatabaseDbContextType, new object[] { dbContextOptions }) as TSenparcEntities;
            return xscfSenparcEntities;
        }

        public SenparcDesignTimeDbContextFactoryBase()
        {
            //if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
            //{
            //    //未执行 AddSenparcGlobalServices 注册，执行注册过程
            //    Host.CreateDefaultBuilder()
            //      .ConfigureWebHostDefaults(webBuilder =>
            //      {
            //          webBuilder.ConfigureServices((hostBuilder, services) =>
            //          {
            //              services.AddMemoryCache();//使用本地缓需要添加
            //          services.AddSenparcGlobalServices(hostBuilder.Configuration);
            //          });
            //      }).Build();
            //}

            Senparc.Scf.Core.Register.TryRegisterMiniCore();
        }

        public virtual TSenparcEntities CreateDbContext(string[] args)
        {
            //修复 https://github.com/SenparcCoreFramework/SCF/issues/13 发现的问题（在非Web环境下无法得到网站根目录路径）

            IRegisterService co2netRegister = RegisterService.Start(SenparcSetting);
            CO2NET.Config.RootDictionaryPath = RootDictionaryPath;

            var register = System.Activator.CreateInstance<TXscfDatabaseRegister>() as TXscfDatabaseRegister;

            //配置数据库
            var builder = new DbContextOptionsBuilder<TSenparcEntities>();
            builder.UseSqlServer(SqlConnectionStr, b =>
            {
                register.DbContextOptionsAction(b, null);
                b.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: new int[] { 2 });
            });

            //还可以补充更多的数据库类型

            return GetInstance(builder.Options);
        }
    }
}
