using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace Yodii.Model
{
    //ToDo : removeItem, Obersable, ienumerable, 
    public class ConfigurationLayer : INotifyPropertyChanged
    {
        #region fields

        private string _configurationName;
        private ConfigurationItemCollection _configurationItemCollection;
        private ConfigurationManager _configurationManagerParent;

        #endregion fields

        #region properties

        public string ConfigurationName
        {
            get { return _configurationName; }
            set
            {
                if( value == null ) throw new NullReferenceException();
                _configurationName = value;
                NotifyPropertyChanged();
            }
        }
        public ConfigurationItemCollection Items
        {
            get { return _configurationItemCollection; }
        }

        public ConfigurationManager ConfigurationManagerParent
        {
            get { return _configurationManagerParent; }
        }

        #endregion properties

        public ConfigurationLayer()
        {
            _configurationName = string.Empty;
            _configurationItemCollection = new ConfigurationItemCollection( this );
        }

        public ConfigurationLayer( string configurationName )
            : this()
        {
            if( configurationName == null ) throw new ArgumentNullException( "configurationName" );

            _configurationName = configurationName;
        }

        internal bool OnConfigurationItemChanged( ConfigurationItem configurationItem, ConfigurationStatus newStatus )
        {
            _configurationManagerParent.OnConfigurationLayerChanged( configurationItem, newStatus );
            return true;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged( [CallerMemberName] String propertyName = "" )
        {
            if( PropertyChanged != null )
            {
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }

        #endregion INotifyPropertyChanged

        //SortedArrayList from CK.Core

        public class ConfigurationItemCollection : IEnumerable
        {
            CKObservableSortedArrayKeyList<ConfigurationItem, string> _items;
            private ConfigurationLayer _parent;

            internal CKObservableSortedArrayKeyList<ConfigurationItem, string> Items
            {
                get { return _items; }
            }

            internal ConfigurationItemCollection( ConfigurationLayer parent )
            {
                _items = new CKObservableSortedArrayKeyList<ConfigurationItem, string>( e => e.ServiceOrPluginName, ( x, y ) => StringComparer.Ordinal.Compare( x, y ) );
                _parent = parent;
            }

            public bool Add( string serviceOrPluginName, ConfigurationStatus status )
            {
                if( string.IsNullOrEmpty( serviceOrPluginName ) ) throw new ArgumentNullException( "serviceOrPluginName is null" );

                //the layer is in a manager
                if( _parent._configurationManagerParent != null )
                {
                    return AddItemWithParent( serviceOrPluginName, status );
                }
                else
                {
                    return AddItemWithoutParent( serviceOrPluginName, status );
                }
            }

            public bool Remove( string serviceOrPluginName )
            {
                return _items.Remove( serviceOrPluginName );
            }

            private bool AddItemWithParent( string serviceOrPluginName, ConfigurationStatus status )
            {
                //if the service or plugin already exist, we update his status
                if( _items.Contains( serviceOrPluginName ) )
                {
                    if( _items.GetByKey( serviceOrPluginName ).Status != status )
                    {
                        //ConfigurationItem.SetStatus check if we can change the status
                        return _items.GetByKey( serviceOrPluginName ).SetStatus( status );
                    }
                    return true;
                }
                else
                {
                    ConfigurationItem newConfigurationItem = new ConfigurationItem( serviceOrPluginName, status, _parent );
                    if( _parent.ConfigurationManagerParent.OnConfigurationLayerChanged( newConfigurationItem, status ) )
                    {
                        _items.Add( newConfigurationItem );
                        return true;
                    }
                    return false;
                }
            }

            private bool AddItemWithoutParent( string serviceOrPluginName, ConfigurationStatus status )
            {
                //if the service or plugin already exist, we update his status
                if( _items.Contains( serviceOrPluginName ) )
                {
                    if( _items.GetByKey( serviceOrPluginName ).Status != status )
                    {
                        if( _items.GetByKey( serviceOrPluginName ).CanChangeStatus( status ) )
                        {
                            _items.GetByKey( serviceOrPluginName ).Status = status;
                            return true;
                        }
                        return false;
                    }
                    return true;
                }
                else
                {
                    _items.Add( new ConfigurationItem( serviceOrPluginName, status, _parent ) );
                    return true;
                }
            }

            public ConfigurationItem this[string key]
            {
                get { return this._items.GetByKey( key ); }
            }

            public int Count
            {
                get { return this._items.Count; }
            }

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }
            #endregion
        }
    }
}
