﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using Yodii.Model;

namespace Yodii.Engine
{
    internal class ConfigurationManager : IConfigurationManager
    {
        readonly YodiiEngine _engine;
        readonly ConfigurationLayerCollection _configurationLayerCollection;
        FinalConfiguration _finalConfiguration;

        ConfigurationChangingEventArgs _currentEventArgs;

        public event EventHandler<ConfigurationChangingEventArgs> ConfigurationChanging;
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        IConfigurationLayerCollection IConfigurationManager.Layers
        {
            get { return _configurationLayerCollection; }
        }

        public ConfigurationLayerCollection Layers
        {
            get { return _configurationLayerCollection; }
        }

        public FinalConfiguration FinalConfiguration
        {
            get { return _finalConfiguration; }
            private set
            {
                _finalConfiguration = value;
                NotifyPropertyChanged();
            }
        }

        internal ConfigurationManager( YodiiEngine engine )
        {
            _engine = engine;
            _configurationLayerCollection = new ConfigurationLayerCollection( this );
        }

        internal void OnLayerNameChanged( IConfigurationLayer layer )
        {
            _configurationLayerCollection.CheckPosition( layer );
        }

        ConfigurationFailureResult FillFromConfiguration( Dictionary<string, ConfigurationStatus> final, Func<ConfigurationItem, bool> filter = null )
        {
            foreach( ConfigurationLayer layer in _configurationLayerCollection )
            {
                ConfigurationStatus status;
                foreach( ConfigurationItem item in layer.Items )
                {
                    if( filter == null || filter( item ) )
                    {
                        if( final.TryGetValue( item.ServiceOrPluginId, out status ) )
                        {
                            if( status == ConfigurationStatus.Optional || (status == ConfigurationStatus.Runnable && item.Status == ConfigurationStatus.Running) )
                            {
                                final[item.ServiceOrPluginId] = item.Status;
                            }
                            else if( status != item.Status )
                            {
                                return new ConfigurationFailureResult( String.Format( "Conflict for {0} between statuses {1} and {2}", item.ServiceOrPluginId, item.Status, status ) );
                            }
                        }
                        else
                        {
                            final.Add( item.ServiceOrPluginId, item.Status );
                        }
                    }
                }
            }
            return new ConfigurationFailureResult();
        }

        internal IYodiiEngineResult OnConfigurationItemChanging( ConfigurationItem item, ConfigurationStatus newStatus )
        {
            Debug.Assert( item != null && _finalConfiguration != null && _configurationLayerCollection.Count != 0 );
            if( _currentEventArgs != null ) throw new InvalidOperationException( "Another change is in progress" );

            Dictionary<string,ConfigurationStatus> final = new Dictionary<string, ConfigurationStatus>();
            final.Add( item.ServiceOrPluginId, newStatus );

            ConfigurationFailureResult internalResult = FillFromConfiguration( final, c => c != item );
            if( internalResult.Success )
            {
                FinalConfiguration finalConfiguration = new FinalConfiguration( final );
                IYodiiEngineResult result = _engine.StaticResolution( finalConfiguration );
                if( result.Success )
                {
                    _currentEventArgs = new ConfigurationChangingEventArgs( finalConfiguration, FinalConfigurationChange.StatusChanged, item );
                    RaiseConfigurationChanging( _currentEventArgs );

                    if( _currentEventArgs.IsCanceled )
                    {
                        return new YodiiEngineResult( new ConfigurationFailureResult( _currentEventArgs.FailureExternalReasons ) );
                    }
                    return _engine.DynamicResolution();
                }
                return result;
            }
            internalResult.addFailureReason( String.Format( "The status of {0} cannot be changed", item.ServiceOrPluginId ) );
            return new YodiiEngineResult( internalResult );
        }

        internal IYodiiEngineResult OnConfigurationItemAdding( ConfigurationItem newItem )
        {
            Dictionary<string,ConfigurationStatus> final = new Dictionary<string, ConfigurationStatus>();
            final.Add( newItem.ServiceOrPluginId, newItem.Status );

            ConfigurationFailureResult internalResult = FillFromConfiguration( final );
            if( internalResult.Success )
            {
                FinalConfiguration finalConfiguration = new FinalConfiguration( final );
                IYodiiEngineResult result = _engine.StaticResolution( finalConfiguration );
                if( result.Success )
                {
                    _currentEventArgs = new ConfigurationChangingEventArgs( finalConfiguration, FinalConfigurationChange.ItemAdded, newItem );
                    RaiseConfigurationChanging( _currentEventArgs );

                    if( _currentEventArgs.IsCanceled )
                    {
                        return new YodiiEngineResult( new ConfigurationFailureResult( _currentEventArgs.FailureExternalReasons ) );
                    }
                    return _engine.DynamicResolution();
                }
                return result;
            }
            internalResult.addFailureReason( String.Format( "{0} cannot be added", newItem.ServiceOrPluginId ) );
            return new YodiiEngineResult( internalResult );
        }

        internal IYodiiEngineResult OnConfigurationItemRemoving( ConfigurationItem item )
        {
            Dictionary<string,ConfigurationStatus> final = new Dictionary<string, ConfigurationStatus>();

            ConfigurationFailureResult internalResult = FillFromConfiguration( final, c => c != item );
            if( internalResult.Success )
            {
                FinalConfiguration finalConfiguration = new FinalConfiguration( final );
                IYodiiEngineResult result = _engine.StaticResolution( finalConfiguration );
                if( result.Success )
                {
                    _currentEventArgs = new ConfigurationChangingEventArgs( finalConfiguration, FinalConfigurationChange.ItemRemoved, item );
                    RaiseConfigurationChanging( _currentEventArgs );

                    if( _currentEventArgs.IsCanceled )
                    {
                        return new YodiiEngineResult( new ConfigurationFailureResult( _currentEventArgs.FailureExternalReasons ) );
                    }
                    return _engine.DynamicResolution();
                }
                return result;
            }
            Debug.Fail( "Removing a configuration item can not lead to an impossibility." );
            internalResult.addFailureReason( "Removing a configuration item can not lead to an impossibility." );
            return new YodiiEngineResult( internalResult );
        }

        internal IYodiiEngineResult OnConfigurationLayerRemoving( ConfigurationLayer layer )
        {
            Dictionary<string,ConfigurationStatus> final = new Dictionary<string, ConfigurationStatus>();

            ConfigurationFailureResult internalResult = FillFromConfiguration( final, c => c.Layer != layer );

            if( internalResult.Success )
            {
                FinalConfiguration finalConfiguration = new FinalConfiguration( final );
                IYodiiEngineResult result = _engine.StaticResolution( finalConfiguration );
                if( result.Success )
                {
                    _currentEventArgs = new ConfigurationChangingEventArgs( new FinalConfiguration( final ), FinalConfigurationChange.LayerRemoved, layer );
                    RaiseConfigurationChanging( _currentEventArgs );


                    if( _currentEventArgs.IsCanceled )
                    {
                        return new YodiiEngineResult( new ConfigurationFailureResult( _currentEventArgs.FailureExternalReasons ) );
                    }
                    return _engine.DynamicResolution();
                }
                return new SuccessYodiiEngineResult();
            }
            Debug.Fail( "Removing a configuration layer can not lead to an impossibility." );
            internalResult.addFailureReason( "Removing a configuration layer can not lead to an impossibility." );
            return new YodiiEngineResult( internalResult );
        }

        internal void OnConfigurationChanged()
        {
            Debug.Assert( _currentEventArgs != null );

            FinalConfiguration = _currentEventArgs.FinalConfiguration;
            if( _currentEventArgs.FinalConfigurationChange == FinalConfigurationChange.StatusChanged
                || _currentEventArgs.FinalConfigurationChange == FinalConfigurationChange.ItemAdded
                || _currentEventArgs.FinalConfigurationChange == FinalConfigurationChange.ItemRemoved )
            {
                RaiseConfigurationChanged( new ConfigurationChangedEventArgs( FinalConfiguration, _currentEventArgs.FinalConfigurationChange, _currentEventArgs.ConfigurationItemChanged ) );
            }
            else
            {
                RaiseConfigurationChanged( new ConfigurationChangedEventArgs( FinalConfiguration, _currentEventArgs.FinalConfigurationChange, _currentEventArgs.ConfigurationLayerChanged ) );
            }
            _currentEventArgs = null;
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

        private void RaiseConfigurationChanging( ConfigurationChangingEventArgs e )
        {
            var h = ConfigurationChanging;
            if( h != null ) h( this, e );
        }

        private void RaiseConfigurationChanged( ConfigurationChangedEventArgs e )
        {
            var h = ConfigurationChanged;
            if( h != null ) h( this, e );
        }

        public class ConfigurationLayerCollection : IConfigurationLayerCollection
        {
            private CKObservableSortedArrayKeyList<ConfigurationLayer,string> _layers;
            private ConfigurationManager _parent;

            internal ConfigurationLayerCollection( ConfigurationManager parent )
            {
                _parent = parent;
                _layers = new CKObservableSortedArrayKeyList<ConfigurationLayer, string>( e => e.LayerName, ( x, y ) => StringComparer.Ordinal.Compare( x, y ), allowDuplicates: true );

                _layers.PropertyChanged += RetrievePropertyEvent;
                _layers.CollectionChanged += RetrieveCollectionEvent;
            }

            private void RetrieveCollectionEvent( object sender, NotifyCollectionChangedEventArgs e )
            {
                FireCollectionChanged( e );
            }

            private void RetrievePropertyEvent( object sender, PropertyChangedEventArgs e )
            {
                FirePropertyChanged( e );
            }

            public IConfigurationLayer Create( string layerName = null )
            {
                var layer = new ConfigurationLayer( _parent, layerName );
                _layers.Add( layer );
                return layer;
            }

            public IYodiiEngineResult Remove( IConfigurationLayer layer )
            {
                if( layer == null ) throw new ArgumentNullException( "layer" );
                if( layer.ConfigurationManager != _parent ) return new SuccessYodiiEngineResult();
                // When called by a hacker.
                ConfigurationLayer l = layer as ConfigurationLayer;
                if ( l == null ) throw new ArgumentException( "Invalid layer.", "layer" );

                Debug.Assert( _layers.Contains( layer ), "Since layer.ConfigurationManager == _parent, then it necessarily belongs to us." );

                IYodiiEngineResult result = _parent.OnConfigurationLayerRemoving( l );
                if( result.Success )
                {
                    _layers.Remove( l );
                    l.SetConfigurationManager( null );
                    _parent.OnConfigurationChanged();
                }
                return result;
            }

            internal void CheckPosition( IConfigurationLayer layer )
            {
                _layers.CheckPosition( _layers.IndexOf( layer ) );
            }

            public IConfigurationLayer this[string key]
            {
                get { return this._layers.GetByKey( key ); }
            }

            private void FirePropertyChanged( PropertyChangedEventArgs e )
            {
                var h = PropertyChanged;
                if( h != null ) h( this, e );
            }

            private void FireCollectionChanged( NotifyCollectionChangedEventArgs e )
            {
                var h = CollectionChanged;
                if( h != null ) h( this, e );
            }

            #region ICKReadOnlyCollection<ConfigurationLayer> Members

            public bool Contains( object item )
            {
                return _layers.Contains( item );
            }

            #endregion

            #region IReadOnlyCollection<ConfigurationLayer> Members

            public int Count
            {
                get { return _layers.Count; }
            }

            #endregion

            #region IEnumerable<ConfigurationLayer> Members

            public IEnumerator<IConfigurationLayer> GetEnumerator()
            {
                return _layers.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _layers.GetEnumerator();
            }

            #endregion

            #region INotifyCollectionChanged Members

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            #endregion

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region ICKReadOnlyList<ConfigurationLayer> Members

            public int IndexOf( object item )
            {
                return _layers.IndexOf( item );
            }

            #endregion

            #region IReadOnlyList<ConfigurationLayer> Members

            public IConfigurationLayer this[int index]
            {
                get { return _layers[index]; }
            }

            #endregion           
        

            IReadOnlyCollection<IConfigurationLayer> IConfigurationLayerCollection.this[string layerName]
            {
                get { return _layers.GetAllByKey( String.IsNullOrWhiteSpace( layerName ) ? String.Empty : layerName ); }
            }
        }

    }

}