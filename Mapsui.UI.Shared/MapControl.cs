using Mapsui.Geometries.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Mapsui.Utilities;
using Mapsui.Layers;
using Mapsui.Providers;
using Point = Mapsui.Geometries.Point;

#if __ANDROID__
namespace Mapsui.UI.Android
#elif __IOS__
namespace Mapsui.UI.iOS
#elif __UWP__
namespace Mapsui.UI.Uwp
#elif __FORMS__
namespace Mapsui.UI
#else
namespace Mapsui.UI.Wpf
#endif
{
    public partial class MapControl
    {
        /// <summary>
        /// Private global variables
        /// </summary>
        
        /// <summary>
        /// Display scale for converting screen position to real position
        /// </summary>
        private float _scale;

        /// <summary>
        /// Mode of a touched move event
        /// </summary>
        private TouchMode _mode = TouchMode.None;

        /// <summary>
        /// Saver for center before last pinch movement, or the previous touch start (left mouse down/finger/pen) position 
        /// </summary>
        private Geometries.Point _previousCenter = new Geometries.Point();

        /// <summary>
        /// Saver for angle before last pinch movement
        /// </summary>
        private double _previousAngle;

        /// <summary>
        /// Saver for radius before last pinch movement
        /// </summary>
        private double _previousRadius = 1f;

        /// <summary>
        /// Saver for the previous touch start (left mouse down/finger/pen) position 
        /// </summary>
        private Geometries.Point _previousTouchStart = new Geometries.Point();

        /// <summary>
        /// Saver for the previous single tap position 
        /// </summary>
        private Geometries.Point _previousTap = new Geometries.Point();

        /// <summary>
        /// Saver for the previous single tap time 
        /// </summary>
        private int? _previousTapTime;

        /// <summary>
        /// Saver for the previous hover object
        /// </summary>
        private IUiEventReceiver _previousHoverObject = null;

        /// <summary>
        /// Events
        /// </summary>

        /// <summary>
        /// TouchStart is called, when user press a mouse button or touch the display
        /// </summary>
        public event EventHandler<TouchedEventArgs> TouchStarted;

        /// <summary>
        /// TouchEnd is called, when user release a mouse button or doesn't touch display anymore
        /// </summary>
        public event EventHandler<TouchedEventArgs> TouchEnded;

        /// <summary>
        /// TouchMove is called, when user move mouse over map (independent from mouse button state) or move finger on display
        /// </summary>
#if __WPF__
        public new event EventHandler<TouchedEventArgs> TouchMove;
#else
        public event EventHandler<TouchedEventArgs> TouchMove;
#endif

        /// <summary>
        /// Hover is called, when user move mouse over map without pressing mouse button
        /// </summary>
#if __ANDROID__
        public new event EventHandler<HoveredEventArgs> Hovered;
#else
        public event EventHandler<HoveredEventArgs> Hovered;
#endif

        /// <summary>
        /// Swipe is called, when user release mouse button or lift finger while moving with a certain speed 
        /// </summary>
        public event EventHandler<SwipedEventArgs> Swipe;

        /// <summary>
        /// Fling is called, when user release mouse button or lift finger while moving with a certain speed, higher than speed of swipe 
        /// </summary>
        public event EventHandler<SwipedEventArgs> Fling;

        /// <summary>
        /// SingleTap is called, when user clicks with a mouse button or tap with a finger on map 
        /// </summary>
        public event EventHandler<TappedEventArgs> SingleTap;

        /// <summary>
        /// LongTap is called, when user clicks with a mouse button or tap with a finger on map for 500 ms
        /// </summary>
        public event EventHandler<TappedEventArgs> LongTap;

        /// <summary>
        /// DoubleTap is called, when user clicks with a mouse button or tap with a finger two or more times on map
        /// </summary>
        public event EventHandler<TappedEventArgs> DoubleTap;

        /// <summary>
        /// Zoom is called, when map should be zoomed
        /// </summary>
        public event EventHandler<ZoomedEventArgs> Zoomed;


        /// <summary>
        /// Properties
        /// </summary>

        /// <summary>
        /// Allow map panning through touch or mouse
        /// </summary>
        public bool PanLock { get; set; }

        /// <summary>
        /// Allow a rotation with a pinch gesture
        /// </summary>
        public bool RotationLock { get; set; }

        /// <summary>
        /// Allow zooming though touch or mouse
        /// </summary>
        public bool ZoomLock { get; set; }
        
        /// <summary>
        /// After how many degrees start rotation to take place
        /// </summary>
        public double UnSnapRotationDegrees { get; set; }

        /// <summary>
        /// With how many degrees from 0 should map snap to 0 degrees
        /// </summary>
        public double ReSnapRotationDegrees { get; set; }

        
        /// <summary>
        /// Event handlers
        /// </summary>

        /// <summary>
        /// Called, when map should zoom out
        /// </summary>
        /// <param name="screenPosition">Center of zoom out event</param>
        private bool OnZoomOut(Geometries.Point screenPosition)
        {
            var args = new ZoomedEventArgs(screenPosition, ZoomDirection.ZoomOut);

            Zoomed?.Invoke(this, args);

            if (args.Handled)
                return true;

            DefaultZoomedHandler(args);

            return true;
        }

        /// <summary>
        /// Called, when map should zoom in
        /// </summary>
        /// <param name="screenPosition">Center of zoom in event</param>
        private bool OnZoomIn(Geometries.Point screenPosition)
        {
            var args = new ZoomedEventArgs(screenPosition, ZoomDirection.ZoomIn);

            Zoomed?.Invoke(this, args);

            if (args.Handled)
                return true;

            DefaultZoomedHandler(args);

            return true;
        }

        /// <summary>
        /// Called, when mouse/finger/pen hovers around
        /// </summary>
        /// <param name="screenPosition">Actual position of mouse/finger/pen</param>
        private bool OnHovered(Geometries.Point screenPosition)
        {
            var args = new HoveredEventArgs(screenPosition, Map.Viewport, ModifierCtrlPressed, ModifierShiftPressed);

            // First try the event handlers on the map control itself. They take priority over all other events.
            Hovered?.Invoke(this, args);

            // Then try the old Map.InvokeHover method, for backward compatibility
            Map.InvokeHover(screenPosition, _scale, Renderer.SymbolCache);

            if (args.Handled)
                return true;

            InvokeHoverEvents(args);

            if (args.MapNeedsRefresh)
                RefreshGraphics();

            return args.Handled;
        }

        public void InvokeHoverEvents(HoveredEventArgs args)
        {
            // TODO: handle widgets

            // First iterate over the layers (in z-order). 
            // Per layer, check which features are under the mouse.
            // Then send the event to the feature.
            // If the feature does not handle the event, pass the event to the layer.
            // (or: even if the feature handles the event, pass the event to the layer, but set some HandledBy property?)

            var reversedLayer = Map.Layers.Reverse();
            foreach (var layer in reversedLayer)
            {
                if (!layer.IsVisibleOnViewport(Map.Viewport)) continue;
                var featuresInView = layer.GetFeaturesInView(layer.Envelope, Map.Viewport.Resolution);
                foreach (var feature in featuresInView)
                {
                    // Check if the mouse is above the feature
                    bool mouseIsTouchingFeature = InfoHelper.IsTouchingTakingIntoAccountSymbolStyles(
                        ScreenToWorld(args.ScreenPosition), feature, layer.Style, Map.Viewport.Resolution,
                        Renderer.SymbolCache);

                    if (!mouseIsTouchingFeature) continue;

                    args.Layer = layer;
                    args.Feature = feature;

                    if (feature != _previousHoverObject)
                    {
                        _previousHoverObject?.OnHoverStopped(args);
                        args.Handled = false;
                        feature.OnHoveredOnce(args);
                    }
                    _previousHoverObject = feature;
                    if (args.Handled) return;
                    feature.OnHovered(args);
                    if (args.Handled) return;

                }
                if (layer != _previousHoverObject)
                {
                    _previousHoverObject?.OnHoverStopped(args);
                    args.Handled = false;
                    layer.OnHoveredOnce(args);
                }
                _previousHoverObject = layer;
                if (args.Handled) return;
                layer.OnHovered(args);
                if (args.Handled) return;
            }
        }


        /// <summary>
        /// Called, when mouse/finger/pen swiped over map
        /// </summary>
        /// <param name="velocityX">Velocity in x direction in pixel/second</param>
        /// <param name="velocityY">Velocity in y direction in pixel/second</param>
        private bool OnSwiped(double velocityX, double velocityY)
        {
            var args = new SwipedEventArgs(velocityX, velocityY);

            Swipe?.Invoke(this, args);

            // TODO
            // Perform standard behavior

            return args.Handled;
        }

        /// <summary>
        /// Called, when mouse/finger/pen flinged over map
        /// </summary>
        /// <param name="velocityX">Velocity in x direction in pixel/second</param>
        /// <param name="velocityY">Velocity in y direction in pixel/second</param>
        private bool OnFlinged(double velocityX, double velocityY)
        {
            var args = new SwipedEventArgs(velocityX, velocityY);

            Fling?.Invoke(this, args);

            // TODO
            // Perform standard behavior

            return args.Handled;
        }

        /// <summary>
        /// Called, when mouse/finger/pen click/touch map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        private bool OnTouchStart(List<Geometries.Point> touchPoints, int? timestamp = null)
        {
            var args = new TouchedEventArgs(touchPoints, timestamp);

            TouchStarted?.Invoke(this, args);

            if (args.Handled)
                return true;

            if (touchPoints.Count >= 2)
            {
                (_previousCenter, _previousRadius, _previousAngle) = GetPinchValues(touchPoints);
                _mode = TouchMode.Zooming;
                _innerRotation = _map.Viewport.Rotation;
                _previousTouchStart = null;
            }
            else
            {
                _mode = TouchMode.Dragging;
                _previousCenter = touchPoints.First();
                _previousTouchStart = _previousCenter;
            }

            return true;
        }

        /// <summary>
        /// Overload for OnTouchStart with a single point as argument.
        /// </summary>
        /// <param name="touchPoint">Touched point</param>
        private bool OnTouchStart(Geometries.Point touchPoint, int? timestamp = null)
        {
            return OnTouchStart(new List<Point>() { touchPoint }, timestamp);
        }

        /// <summary>
        /// Called, when mouse/finger/pen anymore click/touch map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        /// <param name="releasedPoint">Released point, which was touched before</param>
        private bool OnTouchEnd(List<Geometries.Point> touchPoints, Geometries.Point releasedPoint, int? timestamp = null)
        {
            var args = new TouchedEventArgs(touchPoints, timestamp);

            TouchEnded?.Invoke(this, args);

            // Last touch released
            if (touchPoints.Count == 0) // Will always be true for WPF ManipulationCompleted
            {
                // Check if the touch event was actually a tap/click
                if (_previousTouchStart != null && IsClick(_previousTouchStart, releasedPoint) && _mode == TouchMode.Dragging)
                {
                    _mode = TouchMode.None;
                    return OnSingleTapped(releasedPoint, timestamp);
                }

                _invalid = true;
                OnViewChanged(true);
                RefreshGraphics();
                InvalidateCanvas();

                _mode = TouchMode.None;
                _map.ViewChanged(true);
            }

            return args.Handled;
        }

        
        private bool OnTouchEnd(Geometries.Point releasedPoint, int? timestamp = null)
        {
            return OnTouchEnd(new List<Point>(), releasedPoint, timestamp);
        }

        /// <summary>
        /// Called, when mouse/finger/pen moves over map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        private bool OnTouchMove(List<Geometries.Point> touchPoints)
        {
            var args = new TouchedEventArgs(touchPoints);

            TouchMove?.Invoke(this, args);

            if (args.Handled)
                return true;
               

            switch (_mode)
            {
                case TouchMode.Dragging:
                    {
                        if (touchPoints.Count != 1)
                            return false;

                        var touchPosition = touchPoints.First();

                        if (_previousCenter != null && !_previousCenter.IsEmpty())
                        {
                            _map.Viewport.Transform(touchPosition.X, touchPosition.Y, _previousCenter.X, _previousCenter.Y);

                            ViewportLimiter.LimitExtent(_map.Viewport, _map.PanMode, _map.PanLimits, _map.Envelope);

                            RefreshGraphics();
                        }

                        _previousCenter = touchPosition;
                    }
                    break;
                case TouchMode.Zooming:
                    {
                        if (touchPoints.Count < 2)
                            return false;

                        var (prevCenter, prevRadius, prevAngle) = (_previousCenter, _previousRadius, _previousAngle);
                        var (center, radius, angle) = GetPinchValues(touchPoints);

                        double rotationDelta = 0;

                        if (RotationLock)
                        {
                            _innerRotation += angle - prevAngle;
                            _innerRotation %= 360;

                            if (_innerRotation > 180)
                                _innerRotation -= 360;
                            else if (_innerRotation < -180)
                                _innerRotation += 360;

                            if (_map.Viewport.Rotation == 0 && Math.Abs(_innerRotation) >= Math.Abs(UnSnapRotationDegrees) && Math.Abs(radius) > (SystemParameters.MinimumHorizontalDragDistance*20.0))
                                rotationDelta = _innerRotation;
                            else if (_map.Viewport.Rotation != 0)
                            {
                                if (Math.Abs(_innerRotation) <= Math.Abs(ReSnapRotationDegrees))
                                    rotationDelta = -_map.Viewport.Rotation;
                                else
                                    rotationDelta = _innerRotation - _map.Viewport.Rotation;
                            }
                        }

                        _map.Viewport.Transform(center.X, center.Y, prevCenter.X, prevCenter.Y, radius / prevRadius, rotationDelta);

                        (_previousCenter, _previousRadius, _previousAngle) = (center, radius, angle);

                        ViewportLimiter.Limit(_map.Viewport,
                            _map.ZoomMode, _map.ZoomLimits, _map.Resolutions,
                            _map.PanMode, _map.PanLimits, _map.Envelope);

                        Refresh();
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Called, when mouse/finger/pen tapped on map 2 or more times
        /// </summary>
        /// <param name="screenPosition">First clicked/touched position on screen</param>
        /// <param name="numOfTaps">Number of taps on map (2 is a double click/tap)</param>
        private bool OnDoubleTapped(Geometries.Point screenPosition, int numOfTaps)
        {
            // Cancel the dragging mode that was set by the first (single) tap.
            _mode = TouchMode.None; 

            var args = new TappedEventArgs(screenPosition, numOfTaps, ModifierCtrlPressed, ModifierShiftPressed, Map.Viewport);

            DoubleTap?.Invoke(this, args);

            if (args.Handled)
                return true;

            // TODO
            //var tapWasHandled = Map.InvokeInfo(screenPosition, screenPosition, _scale, Renderer.SymbolCache, WidgetTouched, numOfTaps, ModifierCtrlPressed, ModifierShiftPressed);

            InvokeTappedEvents(args);

            if (args.MapNeedsRefresh)
                RefreshGraphics();

            if (args.Handled)
                return true;

            // By default, zoom in on double tap
            return OnZoomIn(screenPosition);
        }

        /// <summary>
        /// Called, when mouse/finger/pen tapped on map one time
        /// </summary>
        /// <param name="screenPosition">Clicked/touched position on screen</param>
        private bool OnSingleTapped(Geometries.Point screenPosition, int? timestamp = null)
        {
            var args = new TappedEventArgs(screenPosition, 1, ModifierCtrlPressed, ModifierShiftPressed, Map.Viewport);

            if (!_previousTap.IsEmpty() && timestamp != null && _previousTapTime != null)
            {
                // TODO make this configurable?
                var maxDoubleTapDistance = 12.0f * Math.Max(SystemParameters.MinimumHorizontalDragDistance,
                    SystemParameters.MinimumVerticalDragDistance);

                var previousTapWasCloseby = Algorithms.Distance(_previousTap, screenPosition) < maxDoubleTapDistance;

                var previousTapWasRecent = (timestamp - _previousTapTime) < 300;

                if (previousTapWasCloseby && previousTapWasRecent)
                {
                    _previousTap = default(Point);
                    _previousTapTime = null;
                    OnDoubleTapped(screenPosition, 2);
                }
            }

            _previousTap = screenPosition;
            _previousTapTime = timestamp;

            // First try the event handlers on the MapControl itself
            SingleTap?.Invoke(this, args);

            if (args.Handled)
                return true;

            // Then try the old Map.InvokeInfo method, for backwards compatibility
            Map.InvokeInfo(screenPosition, screenPosition, _scale, Renderer.SymbolCache, null, 1, ModifierCtrlPressed, ModifierShiftPressed);

            if (args.Handled)
                return true;

            InvokeTappedEvents(args);

            if (args.MapNeedsRefresh)
                RefreshGraphics();

            return args.Handled;
        }

        public void InvokeTappedEvents(TappedEventArgs args)
        {
            if (args.NumOfTaps < 1)
            {
                throw new ArgumentException();
            }

            // TODO: handle widgets

            // First iterate over the layers
            var reversedLayer = Map.Layers.Reverse();
            foreach (var layer in reversedLayer)
            {
                if (!layer.IsVisibleOnViewport(Map.Viewport)) continue;
                var featuresInView = layer.GetFeaturesInView(layer.Envelope, Map.Viewport.Resolution);
                foreach (var feature in featuresInView)
                {
                    // Check if the mouse is above the feature
                    bool mouseIsTouchingFeature = InfoHelper.IsTouchingTakingIntoAccountSymbolStyles(
                        ScreenToWorld(args.ScreenPosition), feature, layer.Style, Map.Viewport.Resolution,
                        Renderer.SymbolCache);

                    if (!mouseIsTouchingFeature) continue;

                    args.Layer = layer;
                    args.Feature = feature;

                    if (args.NumOfTaps < 2)
                    {
                        feature.OnSingleTap(args);
                    }
                    else
                    {
                        feature.OnDoubleTap(args);
                    }
                    if (args.Handled) return;

                }

                if (args.NumOfTaps < 2)
                {
                    layer.OnSingleTap(args);
                }
                else
                {
                    layer.OnDoubleTap(args);
                }
                if (args.Handled) return;
            }
        }

        /// <summary>
        /// Called, when mouse/finger/pen tapped long on map
        /// </summary>
        /// <param name="screenPosition">Clicked/touched position on screen</param>
        private bool OnLongTapped(Geometries.Point screenPosition)
        {
            var args = new TappedEventArgs(screenPosition, 1);

            LongTap?.Invoke(this, args);

            return args.Handled;
        }

        /// <summary>
        /// Public functions
        /// </summary>

#if !__WPF__ && !__UWP__
        public new void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            Unsubscribe();
            base.Dispose(disposing);
        }
#endif

        /// <summary>
        /// Unsubscribe from map events </summary>
        public void Unsubscribe()
        {
            UnsubscribeFromMapEvents(_map);
        }

        /// <summary>
        /// Converting function for world to screen
        /// </summary>
        /// <param name="worldPosition">Position in world coordinates</param>
        /// <returns>Position in screen coordinates</returns>
        public Geometries.Point WorldToScreen(Geometries.Point worldPosition)
        {
            return WorldToScreen(Map.Viewport, _scale, worldPosition);
        }

        /// <summary>
        /// Converting function for screen to world
        /// </summary>
        /// <param name="screenPosition">Position in screen coordinates</param>
        /// <returns>Position in world coordinates</returns>
        public Geometries.Point ScreenToWorld(Geometries.Point screenPosition)
        {
            return ScreenToWorld(Map.Viewport, _scale, screenPosition);
        }

        /// <summary>
        /// Converting function for world to screen respecting scale
        /// </summary>
        /// <param name="viewport">Viewport</param>
        /// <param name="scale">Scale</param>
        /// <param name="worldPosition">Position in world coordinates</param>
        /// <returns>Position in screen coordinates</returns>
        public Point WorldToScreen(IViewport viewport, float scale, Point worldPosition)
        {
            var screenPosition = viewport.WorldToScreen(worldPosition);
            return new Point(screenPosition.X * scale, screenPosition.Y * scale);
        }

        /// <summary>
        /// Converting function for screen to world respecting scale
        /// </summary>
        /// <param name="viewport">Viewport</param>
        /// <param name="scale">Scale</param>
        /// <param name="screenPosition">Position in screen coordinates</param>
        /// <returns>Position in world coordinates</returns>
        public Point ScreenToWorld(IViewport viewport, float scale, Point screenPosition)
        {
            var worldPosition = viewport.ScreenToWorld(screenPosition.X * scale, screenPosition.Y * scale);
            return new Point(worldPosition.X, worldPosition.Y);
        }

        /// <summary>
        /// Private static functions
        /// </summary>
        
        private static (Geometries.Point centre, double radius, double angle) GetPinchValues(List<Geometries.Point> locations)
        {
            if (locations.Count < 2)
                throw new ArgumentException();

            double centerX = 0;
            double centerY = 0;

            foreach (var location in locations)
            {
                centerX += location.X;
                centerY += location.Y;
            }

            centerX = centerX / locations.Count;
            centerY = centerY / locations.Count;

            var radius = Algorithms.Distance(centerX, centerY, locations[0].X, locations[0].Y);

            var angle = Math.Atan2(locations[1].Y - locations[0].Y, locations[1].X - locations[0].X) * 180.0 / Math.PI;

            return (new Geometries.Point(centerX, centerY), radius, angle);
        }

        /// <summary>
        /// Private functions
        /// </summary>

        /// <summary>
        /// Subscribe to map events
        /// </summary>
        /// <param name="map">Map, to which events to subscribe</param>
        private void SubscribeToMapEvents(Map map)
        {
            map.DataChanged += MapDataChanged;
            map.PropertyChanged += MapPropertyChanged;
            map.RefreshGraphics += MapRefreshGraphics;
        }

        /// <summary>
        /// Unsubcribe from map events
        /// </summary>
        /// <param name="map">Map, to which events to unsubscribe</param>
        private void UnsubscribeFromMapEvents(Map map)
        {
            var temp = map;
            if (temp != null)
            {
                temp.DataChanged -= MapDataChanged;
                temp.PropertyChanged -= MapPropertyChanged;
                temp.RefreshGraphics -= MapRefreshGraphics;
                temp.AbortFetch();
            }
        }


        private void DefaultZoomedHandler(ZoomedEventArgs e)
        {
            if (!_map.Viewport.Initialized) return;
            if (ZoomLock) return;

            double resolution = e.Direction == ZoomDirection.ZoomIn ? ZoomHelper.ZoomIn(Map.Resolutions, Map.Viewport.Resolution) : ZoomHelper.ZoomOut(Map.Resolutions, Map.Viewport.Resolution);

            resolution = ViewportLimiter.LimitResolution(resolution, _map.Viewport.Width, _map.Viewport.Height,
                _map.ZoomMode, _map.ZoomLimits, _map.Resolutions, _map.Envelope);

            // 1) Temporarily center on the mouse position
            Map.Viewport.Center = Map.Viewport.ScreenToWorld(e.ScreenPosition);

            // 2) Then zoom 
            Map.Viewport.Resolution = resolution;

            // 3) Then move the temporary center of the map back to the mouse position
            Map.Viewport.Center = Map.Viewport.ScreenToWorld(
                Map.Viewport.Width - e.ScreenPosition.X,
                Map.Viewport.Height - e.ScreenPosition.Y);

            e.Handled = true;

            RefreshGraphics();
            _map.ViewChanged(true);
            OnViewChanged(true);
        }

        private static bool IsClick(Point currentPosition, Point previousPosition)
        {
            return
                Math.Abs(currentPosition.X - previousPosition.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - previousPosition.Y) < SystemParameters.MinimumVerticalDragDistance;
        }


        public void Refresh()
        {
            RefreshData();
            RefreshGraphics();
        }

        public void RefreshGraphics()
        {
            InvalidateCanvas();
            _invalid = true;
        }

        public void RefreshData()
        {
            _map?.ViewChanged(true);
        }

        public void Clear()
        {
            _map?.ClearCache();
            RefreshGraphics();
        }





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
                    UpdateSize();
                }

                RefreshGraphics();
            }
        }

    }
}
