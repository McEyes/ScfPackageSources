﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Scf.XscfBase;
using Senparc.Xscf.DatabaseToolkit.Functions;
using System;

namespace Senparc.Xscf.DatabaseToolkit
{
    public partial class Register : IXscfDatabase
    {
        public const string DATABASE_PREFIX = "DatabaseToolkit";
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type XscfDatabaseDbContextType => typeof(DatabaseToolkitEntities);

        public void AddXscfDatabaseModule(IServiceCollection services)
        {
            //SenparcTrace.SendCustomLog("执行调试", "DatabaseToolkit.AddXscfDatabaseModule");
            services.AddScoped<DbConfig>();
            services.AddScoped<SetConfig>();
            services.AddScoped<SetConfig.SetConfig_Parameters>();

            //AutoMap映射
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<SetConfig.SetConfig_Parameters, SetConfig>();
                profile.CreateMap<SetConfig.SetConfig_Parameters, DbConfig>();
            });
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //实现 [XscfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
