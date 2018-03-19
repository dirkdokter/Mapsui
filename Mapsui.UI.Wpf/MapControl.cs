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

        private readonly Rectangle _selectRectangle = CreateSelectRectangle();
        
        private bool _invalid = true;
        private Map _map;
        
        private RenderMode _renderMode;
        private double _innerRotation;

        private bool ModifierCtrlPressed => (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
        private bool ModifierShiftPressed => (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));


        public MapControl()
        {
            _scale = 1; // Scale is always 1 in WPF

            Children.Add(RenderCanvas);
            Children.Add(RenderElement);
            Children.Add(_selectRectangle);

            RenderElement.PaintSurface += SKElementOnPaintSurface;
            RenderingWeakEventManager.AddHandler(CompositionTargetRendering);

            Map = new Map();

            Loaded += MapControlLoaded;

            MouseLeftButtonDown += MapControlMouseLeftButtonDown;
            MouseLeftButtonUp += MapControlMouseLeftButtonUp;
            MouseMove += MapControlMouseMove;
            MouseWheel += MapControlMouseWheel;

            MouseLeave += MapControlMouseLeave;
            
            SizeChanged += MapControlSizeChanged;

            ManipulationStarting += MapControlManipulationStarting;
            ManipulationStarted += MapControlManipulationStarted;
            ManipulationDelta += MapControlManipulationDelta;
            ManipulationCompleted += MapControlManipulationCompleted;

            IsManipulationEnabled = true;
        }

      

        private static Rectangle CreateSelectRectangle()
        {
            return new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                RadiusX = 0.5,
                RadiusY = 0.5,
                StrokeDashArray = new DoubleCollection { 3.0 },
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed
            };
        }

        public IRenderer Renderer { get; set; } = new MapRenderer();

        private bool IsInBoxZoomMode { get; set; }

        public bool ZoomToBoxMode { get; set; }

        public Map Map
        {
            get => _map;
            set
            {
                if (_map != null)
                {
                    UnsubscribeFromMapEvents(_map);
                    _map = null;
                }

                _map = value;

                if (_map != null)
                {
                    SubscribeToMapEvents(_map);
                    _map.ViewChanged(true);
                }

                RefreshGraphics();
            }
        }

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
        public event EventHandler<FeatureInfoEventArgs> FeatureInfo;
        public event EventHandler ViewportInitialized;

        private void MapRefreshGraphics(object sender, EventArgs eventArgs)
        {
            RefreshGraphics();
        }

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

        private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > Constants.Epsilon)
            {
                OnZoomIn(e.GetPosition(this).ToMapsui());
            }
            else if (e.Delta < Constants.Epsilon)
            {
                OnZoomOut(e.GetPosition(this).ToMapsui());
            }
            e.Handled = true;
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

        private void MapControlMouseLeave(object sender, MouseEventArgs e)
        {
            _mode = TouchMode.None;
            ReleaseMouseCapture();
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

        private void MapControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CaptureMouse();

            if (e.ClickCount > 1)
            {
                OnDoubleTapped(e.GetPosition(this).ToMapsui(), e.ClickCount);
            }
            else
            {
                OnTouchStart(e.GetPosition(this).ToMapsui());
            }
        }

        private void MapControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                throw new Exception("This should not happen. See https://github.com/pauldendulk/Mapsui/issues/344.");
            }

            OnTouchEnd(e.GetPosition(this).ToMapsui());

            ReleaseMouseCapture();
        }

        private void WidgetTouched(Widgets.IWidget widget, Geometries.Point screenPosition)
        {
            if (widget is Widgets.Hyperlink)
                System.Diagnostics.Process.Start(((Widgets.Hyperlink)widget).Url);

            widget.HandleWidgetTouched(screenPosition);
        }

        private void OnFeatureInfo(IDictionary<string, IEnumerable<IFeature>> features)
        {
            FeatureInfo?.Invoke(this, new FeatureInfoEventArgs { FeatureInfo = features });
        }

        private bool MouseButtonsReleased(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Released
                   && e.MiddleButton == MouseButtonState.Released
                   && e.RightButton == MouseButtonState.Released;
        }

        private void MapControlMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                OnTouchMove(new List<Geometries.Point>() {e.GetPosition(this).ToMapsui()});
            }
            else if (MouseButtonsReleased(e))
            {
                OnHovered(e.GetPosition(this).ToMapsui());
            }
        }
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

        private void ClearBBoxDrawing()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _selectRectangle.Visibility = Visibility.Collapsed;
            }));
        }

        private void DrawBbox(Point newPos)
        {
            /*if (_mouseDown)
            {
                var from = _previousMousePosition;
                var to = newPos;

                if (from.X > to.X)
                {
                    var temp = from;
                    from.X = to.X;
                    to.X = temp.X;
                }

                if (from.Y > to.Y)
                {
                    var temp = from;
                    from.Y = to.Y;
                    to.Y = temp.Y;
                }

                _selectRectangle.Width = to.X - from.X;
                _selectRectangle.Height = to.Y - from.Y;
                _selectRectangle.Margin = new Thickness(from.X, from.Y, 0, 0);
                _selectRectangle.Visibility = Visibility.Visible;
            }*/
        }

        public void ZoomToFullEnvelope()
        {
            if (Map.Envelope == null) return;
            if (ActualWidth.IsNanOrZero()) return;
            Map.Viewport.Resolution = Math.Max(Map.Envelope.Width / ActualWidth, Map.Envelope.Height / ActualHeight);
            Map.Viewport.Center = Map.Envelope.GetCentroid();
        }

        private void MapControlManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            // Capture touch to MapControl
            e.ManipulationContainer = this; 
            e.Handled = true;
        }

        private void MapControlManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            OnTouchStart(e.Manipulators.ToMapsui(this), e.Timestamp); // Always one manipulator
        }

        private void MapControlManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.IsInertial)
            {
                Console.WriteLine(e.Manipulators.Count());
            }
            if (_mode == TouchMode.Dragging && e.Manipulators.Count() >= 2)
            {
                // Re-start the touch operation
                OnTouchStart(e.Manipulators.ToMapsui(this));
            }

            var hasBeenManipulatedSignificantly = Math.Abs(e.DeltaManipulation.Translation.X) > SystemParameters.MinimumHorizontalDragDistance
                                   || Math.Abs(e.DeltaManipulation.Translation.Y) > SystemParameters.MinimumVerticalDragDistance;

            if (hasBeenManipulatedSignificantly)
            {
                OnTouchMove(e.Manipulators.ToMapsui(this));
            }

            e.Handled = true;
        }

        private void MapControlManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            OnTouchEnd(e.Manipulators.ToMapsui(this), e.ManipulationOrigin.ToMapsui(), e.Timestamp);
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
