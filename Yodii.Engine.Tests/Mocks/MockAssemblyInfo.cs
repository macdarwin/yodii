﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yodii.Model;

namespace Yodii.Engine.Tests.Mocks
{
    public class MockAssemblyInfo : IAssemblyInfo
    {
        readonly string _assemblyFileName;
        readonly List<IPluginInfo> _plugins;
        readonly List<IServiceInfo> _services;

        internal MockAssemblyInfo( string assemblyFileName )
        {
            Debug.Assert( !String.IsNullOrEmpty( assemblyFileName ) );

            _assemblyFileName = assemblyFileName;
            _plugins = new List<IPluginInfo>();
            _services = new List<IServiceInfo>();
        }

        #region IAssemblyInfo Members

        public string AssemblyFileName
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasPluginsOrServices
        {
            get { return _plugins.Count > 0 || _services.Count > 0; }
        }

        public IReadOnlyList<IPluginInfo> Plugins
        {
            get { return _plugins.AsReadOnly(); }
        }

        public IReadOnlyList<IServiceInfo> Services
        {
            get { return _services.AsReadOnly(); }
        }

        #endregion
    }
}