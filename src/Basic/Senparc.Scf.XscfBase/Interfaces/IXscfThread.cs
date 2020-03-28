﻿using Senparc.Scf.XscfBase.Threads;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Scf.XscfBase
{
    /// <summary>
    /// XSCF 线程模块接口
    /// </summary>
    public interface IXscfThread
    {
        /// <summary>
        /// 线程配置
        /// </summary>
        /// <param name="xscfThreadBuilder"></param>
        void ThreadConfig(XscfThreadBuilder xscfThreadBuilder);
    }
}
