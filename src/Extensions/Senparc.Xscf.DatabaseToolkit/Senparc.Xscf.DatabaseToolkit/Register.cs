﻿using Senparc.Scf.Core.Enums;
using Senparc.Scf.Core.Models;
using Senparc.Scf.XscfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xscf.DatabaseToolkit
{
    [XscfRegister]
    public partial class Register : XscfRegisterBase, IXscfRegister
    {
        public Register()
        { }

        #region IRegister 接口

        public override string Name => "Senparc.Xscf.DatabaseToolkit";

        public override string Uid => "3019CCBE-0739-43D5-9DED-027A0B26745E";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.5.2";//必须填写版本号

        public override string MenuName => "数据库工具包";
        public override string Icon => "fa fa-database";
        public override string Description => "为方便数据库操作提供的工具包。请完全了解本工具各项功能特点后再使用，所有数据库操作都有损坏数据的可能，修改数据库前务必注意数据备份！";

        /// <summary>
        /// 注册当前模块需要支持的功能模块
        /// </summary>
        public override IList<Type> Functions => new[] {
            typeof(Functions.SetConfig),
            typeof(Functions.BackupDatabase),
            typeof(Functions.ExportSQL),
            typeof(Functions.CheckUpdate),
            typeof(Functions.UpdateDatabase),
        };

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //更新数据库
            await base.MigrateDatabaseAsync<DatabaseToolkitEntities>(serviceProvider);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            DatabaseToolkitEntities mySenparcEntities = serviceProvider.GetService<DatabaseToolkitEntities>();

            //指定需要删除的数据实体

            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.XscfDatabaseDbContextType).Keys.ToArray();
            //删除数据库表
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        #endregion
    }
}