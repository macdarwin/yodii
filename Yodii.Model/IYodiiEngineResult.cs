﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace Yodii.Model
{
    public interface IYodiiEngineResult
    {
        bool Success { get; }

        IConfigurationFailureResult ConfigurationFailureResult { get; }

        IStaticFailureResult StaticFailureResult { get; }

        IDynamicFailureResult HostFailureResult { get; }

        IReadOnlyList<IPluginInfo> PluginCulprits { get; }
        
        IReadOnlyList<IServiceInfo> ServiceCulprits { get; }
    }
}