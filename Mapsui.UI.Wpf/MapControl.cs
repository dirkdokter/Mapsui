using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Rendering;
using Mapsui.Rendering.Xaml;
using Mapsui.Utilities;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Point = System.Windows.Point;
using XamlVector = System.Windows.Vector;

namespace Mapsui.UI.Wpf
{
    public enum RenderMode
    {
        Wpf,
        Skia
    }

    public partial class MapControl : Grid, IMapControl
    {
        // ReSharper disable once UnusedMember.Local // This registration triggers the call to OnResolutionChanged
        private static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register(
                "Resolution", typeof(double), typeof(MapControl),
                new PropertyMetadata(OnResolutionChanged));
        
        private bool _invalid = true;
        private Map _map;
        
        private RenderMode _renderMode;
        
        public MapControl()
        {
            _scale = 1; // Scale is always 1 in WPF

            Children.Add(RenderCanvas);
            Children.Add(RenderElement);
            // TODO Children.Add(_selectRectangle);

            RenderElement.PaintSurface += SKElementOnPaintSurface;
            RenderingWeakEventManager.AddHandler(CompositionTargetRendering);

            Map = new Map();

            Loaded += MapControlLoaded;
            SizeChanged += MapControlSizeChanged;

            _addEventHandlers();
        }
      
        public IRenderer Renderer { get; set; } = new MapRenderer();

        public string ErrorMessage { get; private set; }
        
        public Canvas RenderCanvas { get; } = CreateWpfRenderCanvas();

        private SKElement RenderElement { get; } = CreateSkiaRenderElement();

        public RenderMode RenderMode
        {
            get => _renderMode;
            set
            {
                if (value == RenderMode.Skia)
                {
                    RenderCanvas.Visibility = Visibility.Collapsed;
                    RenderElement.Visibility = Visibility.Visible;
                    Renderer = new Rendering.Skia.MapRenderer();
                    _scale = GetSkiaScale();
                    Refresh();
                }
                else
                {
                    RenderElement.Visibility = Visibility.Collapsed;
                    RenderCanvas.Visibility = Visibility.Visible;
                    Renderer = new MapRenderer();
                    _scale = 1; // Scale is always 1 in WPF
                    Refresh();
                }
                _renderMode = value;
            }
        }

        private static Canvas CreateWpfRenderCanvas()
        {
            return new Canvas
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private static SKElement CreateSkiaRenderElement()
        {
            return new SKElement
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };
        }

        public event EventHandler ErrorMessageChanged;
        public event EventHandler<ViewChangedEventArgs> ViewChanged;
        public event EventHandler ViewportInitialized;


        private void MapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(new Action(() => MapPropertyChanged(sender, e)));
            else
            {
                if (e.PropertyName == nameof(Layer.Enabled))
                {
                    RefreshGraphics();
                }
                else if (e.PropertyName == nameof(Layer.Opacity))
                {
                    RefreshGraphics();
                }
            }
        }

        private void MapRefreshGraphics(object sender, EventArgs eventArgs)
        {
            RefreshGraphics();
        }

        private void OnViewChanged(bool userAction = false)
        {
            if (_map == null) return;

            ViewChanged?.Invoke(this, new ViewChangedEventArgs { Viewport = Map.Viewport, UserAction = userAction });
        }

        internal void InvalidateCanvas()
        {
            Dispatcher.BeginInvoke(new Action(InvalidateVisual));
        }


        private void OnErrorMessageChanged(EventArgs e)
        {
            ErrorMessageChanged?.Invoke(this, e);
        }

        private static void OnResolutionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var newResolution = (double)e.NewValue;
            ((MapControl)dependencyObject).ZoomToResolution(newResolution);
        }

        private void ZoomToResolution(double resolution)
        {
            var current = Map.Viewport.Center;

            Map.Viewport.Transform(current.X, current.Y, current.X, current.Y, Map.Viewport.Resolution / resolution);

            ViewportLimiter.Limit(_map.Viewport, _map.ZoomMode, _map.ZoomLimits, _map.Resolutions,
                _map.PanMode, _map.PanLimits, _map.Envelope);

            _map.ViewChanged(true);
            OnViewChanged();
            RefreshGraphics();
        }


        private void MapControlLoaded(object sender, RoutedEventArgs e)
        {
            TryInitializeViewport();
            UpdateSize();
            Focusable = true;
        }



        private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            TryInitializeViewport();
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
            UpdateSize();
            _map.ViewChanged(true);
            OnViewChanged();
            Refresh();
        }

        private void UpdateSize()
        {
            if (Map.Viewport != null)
            {
                Map.Viewport.Width = ActualWidth;
                Map.Viewport.Height = ActualHeight;

                ViewportLimiter.Limit(_map.Viewport, _map.ZoomMode, _map.ZoomLimits, _map.Resolutions,
                    _map.PanMode, _map.PanLimits, _map.Envelope);
            }
        }



        public void MapDataChanged(object sender, DataChangedEventArgs e) // todo: make private?
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new DataChangedEventHandler(MapDataChanged), sender, e);
            }
            else
            {
                if (e == null)
                {
                    ErrorMessage = "Unexpected error: DataChangedEventArgs can not be null";
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else if (e.Cancelled)
                {
                    ErrorMessage = "Cancelled";
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else if (e.Error is WebException)
                {
                    ErrorMessage = "WebException: " + e.Error.Message;
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else if (e.Error != null)
                {
                    ErrorMessage = e.Error.GetType() + ": " + e.Error.Message;
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else // no problems
                {
                    RefreshGraphics();
                }
            }
        }
        

        /*private void WidgetTouched(Widgets.IWidget widget, Geometries.Point screenPosition)
        {
            if (widget is Widgets.Hyperlink)
                System.Diagnostics.Process.Start(((Widgets.Hyperlink)widget).Url);

            widget.HandleWidgetTouched(screenPosition);
        }*/



        private void TryInitializeViewport()
        {
            if (_map.Viewport.Initialized) return;

            if (_map.Viewport.TryInitializeViewport(_map, ActualWidth, ActualHeight))
            {
                ViewportLimiter.Limit(_map.Viewport, _map.ZoomMode, _map.ZoomLimits, _map.Resolutions,
                    _map.PanMode, _map.PanLimits, _map.Envelope);

                Map.ViewChanged(true);
                OnViewportInitialized();
            }
        }

        private void OnViewportInitialized()
        {
            ViewportInitialized?.Invoke(this, EventArgs.Empty);
        }

        private void CompositionTargetRendering(object sender, EventArgs e)
        {
            if (!_invalid) return; // Don't render when nothing has changed

            if (RenderMode == RenderMode.Wpf) RenderWpf();
            else RenderElement.InvalidateVisual();
        }

        private void RenderWpf()
        {
            if (Renderer == null) return;
            if (_map == null) return;
            if (double.IsNaN(ActualWidth) || ActualWidth == 0 || double.IsNaN(ActualHeight) || ActualHeight == 0) return;

            TryInitializeViewport();

            Renderer.Render(RenderCanvas, Map.Viewport, _map.Layers, Map.Widgets, _map.BackColor);

            _invalid = false;
        }


        public void ZoomToFullEnvelope()
        {
            if (Map.Envelope == null) return;
            if (ActualWidth.IsNanOrZero()) return;
            Map.Viewport.Resolution = Math.Max(Map.Envelope.Width / ActualWidth, Map.Envelope.Height / ActualHeight);
            Map.Viewport.Center = Map.Envelope.GetCentroid();
        }

       
        private double GetDeltaScale(XamlVector scale)
        {
            if (ZoomLock) return 1;
            var deltaScale = (scale.X + scale.Y) / 2;
            if (Math.Abs(deltaScale) < Constants.Epsilon)
                return 1; // If there is no scaling the deltaScale will be 0.0 in Windows Phone (while it is 1.0 in wpf)
            if (!(Math.Abs(deltaScale - 1d) > Constants.Epsilon)) return 1;
            return deltaScale;
        }


        private float GetSkiaScale()
        {
            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource == null) throw new Exception("PresentationSource is null");
            var compositionTarget = presentationSource.CompositionTarget;
            if (compositionTarget == null) throw new Exception("CompositionTarget is null");

            var m = compositionTarget.TransformToDevice;

            var dpiX = m.M11;
            var dpiY = m.M22;

            if (dpiX != dpiY)
                throw new ArgumentException();

            return (float)dpiX;
        }

        private void SKElementOnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (!_invalid) return; // Don't render when nothing has changed
            if (double.IsNaN(ActualWidth) || ActualWidth == 0 || double.IsNaN(ActualHeight) ||
                ActualHeight == 0) return;

            e.Surface.Canvas.Scale((float) _scale, (float) _scale);

            Map.Viewport.Width = ActualWidth;
            Map.Viewport.Height = ActualHeight;

            TryInitializeViewport();
            Renderer.Render(e.Surface.Canvas, Map.Viewport, Map.Layers, Map.Widgets, Map.BackColor);

            _invalid = false;
        }


        ~MapControl()
        {
            // Because we use weak events the finalizer will be called even while the event is still registered.
            RenderingWeakEventManager.RemoveHandler(CompositionTargetRendering);
        }
    }
}
