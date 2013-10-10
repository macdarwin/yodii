using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Yodii.Model
{
    //ToDo : removeItem, Obersable, ienumerable, 
    public class ConfigurationLayer
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
        }

        public ConfigurationItemCollection Items
        {
            get { return _configurationItemCollection; }
        }

        internal ConfigurationManager ConfigurationManagerParent
        {
            get { return _configurationManagerParent; }
            set { _configurationManagerParent = value; }
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

        internal ConfigurationResult OnConfigurationItemChanging( FinalConfigurationChange change, ConfigurationItem configurationItem, ConfigurationStatus newStatus )
        {
            return _configurationManagerParent.OnConfigurationLayerChanging( change, configurationItem, newStatus );
        }

        internal void OnConfigurationItemChanged( FinalConfigurationChange change, ConfigurationItem configurationItem, ConfigurationStatus newStatus )
        {
            _configurationManagerParent.OnConfigurationManagerChanged( change, configurationItem, newStatus );
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

        #endregion

        public class ConfigurationItemCollection : IEnumerable, INotifyCollectionChanged, INotifyPropertyChanged
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
                _items.PropertyChanged += RetrievePropertyEvent;
                _items.CollectionChanged += RetrieveCollectionEvent;
            }

            private void RetrieveCollectionEvent( object sender, NotifyCollectionChangedEventArgs e )
            {
                FireCollectionChanged( e );
            }

            private void RetrievePropertyEvent( object sender, PropertyChangedEventArgs e )
            {
                FirePropertyChanged( e );
            }

            public ConfigurationResult Add( string serviceOrPluginName, ConfigurationStatus status, string statusReason = "" )
            {
                if( string.IsNullOrEmpty( serviceOrPluginName ) ) throw new ArgumentNullException( "serviceOrPluginName is null" );
                if( statusReason == null ) throw new NullReferenceException();

                //the layer is in a manager
                if( _parent.ConfigurationManagerParent != null )
                {
                    return AddItemWithParent( serviceOrPluginName, status, statusReason );
                }
                else
                {
                    return AddItemWithoutParent( serviceOrPluginName, status, statusReason );
                }
            }

            public ConfigurationResult Remove( string serviceOrPluginName )
            {
                if( string.IsNullOrEmpty( serviceOrPluginName ) ) throw new ArgumentException( "serviceOrPluginID is null or empty" );
                if( _parent.ConfigurationManagerParent != null )
                {
                    return RemoveWithParent( serviceOrPluginName );
                }
                else
                {
                    return RemoveWithoutParent( serviceOrPluginName );
                }
            }

            private ConfigurationResult AddItemWithParent( string serviceOrPluginName, ConfigurationStatus status, string statusReason )
            {
                bool itemExist;
                ConfigurationItem item = _items.GetByKey( serviceOrPluginName, out itemExist );
                //if the service or plugin already exist, we update his status
                if( itemExist )
                {
                    if( item.Status != status )
                    {
                        //ConfigurationItem.SetStatus check if we can change the status
                        return item.SetStatus( status, statusReason );
                    }
                    return new ConfigurationResult();
                }
                else
                {
                    ConfigurationItem newConfigurationItem = new ConfigurationItem( serviceOrPluginName, status, _parent, statusReason );
                    ConfigurationResult result = _parent.OnConfigurationItemChanging( FinalConfigurationChange.ItemAdded, newConfigurationItem, status );
                    if( result )
                    {
                        if( _items.Add( newConfigurationItem ) )
                        {
                            return result;
                        }
                        else
                        {
                            return new ConfigurationResult( "A problem has been encountered while adding the ConfigurationItem in the collection" );
                        }
                    }
                    return result;
                }
            }

            private ConfigurationResult AddItemWithoutParent( string serviceOrPluginName, ConfigurationStatus status, string statusReason )
            {
                bool itemExist;
                ConfigurationItem item = _items.GetByKey( serviceOrPluginName, out itemExist );
                //if the service or plugin already exist, we update his status
                if( itemExist )
                {
                    if( item.Status != status )
                    {
                        if( item.CanChangeStatus( status ) )
                        {
                            item.Status = status;
                            item.StatusReason = statusReason;
                            return new ConfigurationResult();
                        }
                        return new ConfigurationResult( string.Format( "{0} already exists and cannot switch from {1} to {2}", item.ServiceOrPluginName, item.Status, status ) );
                    }
                    return new ConfigurationResult();
                }
                else
                {
                    if( _items.Add( new ConfigurationItem( serviceOrPluginName, status, _parent, statusReason ) ) )
                    {
                        return new ConfigurationResult();
                    }
                    else
                    {
                        return new ConfigurationResult( "A problem has been encountered while adding the ConfigurationItem in the collection" );
                    }
                }
            }

            private ConfigurationResult RemoveWithParent( string serviceOrPluginName )
            {
                bool itemExist;
                ConfigurationItem item = _items.GetByKey( serviceOrPluginName, out itemExist );
                //if the service or plugin already exist, we update his status
                if( itemExist )
                {
                    ConfigurationResult result = _parent.ConfigurationManagerParent.OnConfigurationLayerChanging( FinalConfigurationChange.ItemRemoved, item, ConfigurationStatus.Optional );
                    if( result )
                    {
                        if( _items.Remove( serviceOrPluginName ) )
                        {
                            return result;
                        }
                        else
                        {
                            return new ConfigurationResult( "A problem has been encountered while adding the ConfigurationItem in the collection" );
                        }
                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    return new ConfigurationResult( string.Format("{1} doesn't exist in the collection", serviceOrPluginName ) );
                }
            }

            private ConfigurationResult RemoveWithoutParent( string serviceOrPluginName )
            {
                if( _items.Remove( serviceOrPluginName ) )
                {
                    return new ConfigurationResult();
                }
                else
                {
                    return new ConfigurationResult( "A problem has been encountered while adding the ConfigurationItem in the collection" );
                }
            }

            public ConfigurationItem this[string key]
            {
                get { return this._items.GetByKey( key ); }
            }
            public ConfigurationItem this[int index]
            {
                get { return this._items[index]; }
            }

            public int Count
            {
                get { return this._items.Count; }
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            private void FirePropertyChanged( PropertyChangedEventArgs e )
            {
                if( this.PropertyChanged != null )
                {
                    this.PropertyChanged( this, e );
                }
            }
            private void FireCollectionChanged( NotifyCollectionChangedEventArgs e )
            {
                if( this.CollectionChanged != null )
                {
                    this.CollectionChanged( this, e );
                }
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
