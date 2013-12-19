﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using GraphX;
using GraphX.GraphSharp.Algorithms.Layout;

namespace Yodii.Lab
{
    class YodiiLayout : IExternalLayout<YodiiGraphVertex>
    {
        readonly YodiiGraphArea _graphArea;
        readonly Dictionary<YodiiGraphVertex, Point> _vertexPositions = new Dictionary<YodiiGraphVertex, Point>();
        IDictionary<YodiiGraphVertex, Size> _vertexSizes;

        public YodiiLayout( YodiiGraphArea graphArea )
        {
            _graphArea = graphArea;
        }
        public void Compute()
        {
            Dictionary<YodiiGraphVertex,Point> d = null;
            Application.Current.Dispatcher.Invoke( DispatcherPriority.Normal, new Action(() => {
                d = _graphArea.GetVertexPositions();
            }));


            _vertexPositions.Clear();

            foreach( var v in _vertexSizes )
            {

                Point currentPoint;
                if( d.TryGetValue( v.Key, out currentPoint ) )
                { // Update by index: TODO
                    if( Double.IsNaN(currentPoint.X) || Double.IsNaN(currentPoint.Y))
                    {
                        _vertexPositions.Add( v.Key, new Point( 0, 0 ) );


                    }
                    else
                    {
                        _vertexPositions.Add( v.Key, currentPoint );
                    }
                } else {
                    _vertexPositions.Add( v.Key, new Point(0,0));
                }
            }
        }

        public bool NeedVertexSizes
        {
            get { return true; }
        }

        public System.Collections.Generic.IDictionary<YodiiGraphVertex, Point> VertexPositions
        {
            get { return _vertexPositions; }
        }

        public System.Collections.Generic.IDictionary<YodiiGraphVertex, Size> VertexSizes
        {
            get
            {
                return _vertexSizes;
            }
            set
            {
                _vertexSizes = value;
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow
    {
        readonly MainWindowViewModel _vm;

        /// <summary>
        /// Creates the main window.
        /// </summary>
        public MainWindow()
        {
            _vm = new MainWindowViewModel( false );
            this.DataContext = _vm;
            _vm.NewNotification += _vm_NewNotification;
            InitializeComponent();

            GraphArea.DefaultEdgeRoutingAlgorithm = GraphX.EdgeRoutingAlgorithmTypeEnum.SimpleER;
            GraphArea.DefaultOverlapRemovalAlgorithm = GraphX.OverlapRemovalAlgorithmTypeEnum.FSA;
            //GraphArea.SetVerticesDrag( true, true );
            GraphArea.EnableParallelEdges = true;
            GraphArea.EdgeShowSelfLooped = true;
            GraphArea.EdgeCurvingEnabled = true;
            GraphArea.UseNativeObjectArrange = false;
            GraphArea.SideExpansionSize = new Size( 100, 100 );

            GraphArea.GenerateGraphFinished += GraphArea_GenerateGraphFinished;
            GraphArea.RelayoutFinished += GraphArea_RelayoutFinished;

            ZoomBox.IsAnimationDisabled = false;
            ZoomBox.MaxZoomDelta = 2;

            _vm.Graph.GraphUpdateRequested += Graph_GraphUpdateRequested;


            //GraphArea.DefaultLayoutAlgorithm = _vm.GraphLayoutAlgorithmType;
            //GraphArea.DefaultLayoutAlgorithmParams = _vm.GraphLayoutParameters;
            GraphArea.ExternalLayoutAlgorithm = new YodiiLayout( GraphArea );

            this.Loaded += MainWindow_Loaded;
        }

        void _vm_NewNotification( object sender, NotificationEventArgs e )
        {
            if( this.NotificationControl != null )
            {
                this.NotificationControl.AddNotification( e.Notification );
            }
        }

        void MainWindow_Loaded( object sender, RoutedEventArgs e )
        {
            GraphArea.GenerateGraph( _vm.Graph );
        }

        void GraphArea_RelayoutFinished( object sender, EventArgs e )
        {
            ZoomBox.ZoomToFill();
            ZoomBox.Mode = GraphX.Controls.ZoomControlModes.Custom;
            foreach( var item in GraphArea.VertexList )
            {
                DragBehaviour.SetIsDragEnabled( item.Value, true );
                item.Value.EventOptions.PositionChangeNotification = true;
            }
            GraphArea.ShowAllEdgesLabels();
            GraphArea.InvalidateVisual();

        }

        void GraphArea_GenerateGraphFinished( object sender, EventArgs e )
        {
            GraphArea.GenerateAllEdges();
            ZoomBox.ZoomToFill();
            ZoomBox.Mode = GraphX.Controls.ZoomControlModes.Custom;
            foreach( var item in GraphArea.VertexList )
            {
                DragBehaviour.SetIsDragEnabled( item.Value, true );
                item.Value.EventOptions.PositionChangeNotification = true;
            }
            GraphArea.ShowAllEdgesLabels();
            GraphArea.InvalidateVisual();
        }

        void Graph_GraphUpdateRequested( object sender, GraphUpdateRequestEventArgs e )
        {
            if( e.NewLayout != null )
            {
                GraphArea.DefaultLayoutAlgorithm = (GraphX.LayoutAlgorithmTypeEnum)e.NewLayout;
            }
            if( e.AlgorithmParameters != null )
            {
                GraphArea.DefaultLayoutAlgorithmParams = e.AlgorithmParameters;
            }

            if( e.RequestType == GraphGenerationRequestType.RelayoutGraph )
            {
                try
                {
                    this.GraphArea.RelayoutGraph();
                }
                catch( Exception ex )
                {
                    MessageBox.Show( String.Format( "An error was encountered while updating the layout:\n\n- {0}\n\nStack trace:\n{1}", ex.Message, ex.StackTrace ),
                        "Error while updating layout",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error, MessageBoxResult.OK );

                    ResetGraphToDefaultState();
                }
            }
            else if( e.RequestType == GraphGenerationRequestType.RegenerateGraph )
            {
                try
                {
                    this.GraphArea.GenerateGraph( _vm.Graph, true, true, true );
                }
                catch( Exception ex )
                {
                    MessageBox.Show( String.Format( "An error was encountered while generating the graph:\n\n- {0}\n\nStack trace:\n{1}", ex.Message, ex.StackTrace ),
                        "Error while generating graph",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error, MessageBoxResult.OK );

                    ResetGraphToDefaultState();
                }
            }

        }

        private void StackPanel_MouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            FrameworkElement vertexPanel = sender as FrameworkElement;

            YodiiGraphVertex vertex = vertexPanel.DataContext as YodiiGraphVertex;

            _vm.SelectedVertex = null;
            _vm.SelectedVertex = vertex;
        }

        private void graphLayout_MouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            _vm.SelectedVertex = null;
        }

        private void ResetGraphToDefaultState()
        {
            this.GraphArea.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.CompoundFDP;
            this.GraphArea.DefaultLayoutAlgorithmParams = null;

            this.GraphArea.GenerateGraph( _vm.Graph, true, true, true );
        }

        private void ExportToPngButton_Click( object sender, RoutedEventArgs e )
        {

            this.GraphArea.ExportAsPNG( false );

            //Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            //dlg.DefaultExt = ".png";
            //dlg.Filter = "PNG images (*.png)|*.png";
            //dlg.CheckPathExists = true;
            //dlg.OverwritePrompt = true;
            //dlg.AddExtension = true;

            //Nullable<bool> result = dlg.ShowDialog();

            //if( result == true )
            //{
            //    string filePath = dlg.FileName;
            //    this.GraphArea.ExportAsImage( ImageType.PNG, false, 96, 100 );
            //}
        }

    }
}
