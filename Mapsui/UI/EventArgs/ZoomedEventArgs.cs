using System;

namespace Mapsui.UI
{
    public enum ZoomDirection
    {
        ZoomOut = -1,
        ZoomIn = 1
    }

    public class ZoomedEventArgs : BaseUiEventArgs
    {
        public Geometries.Point ScreenPosition { get; }
        public ZoomDirection Direction { get; }

        public ZoomedEventArgs(Geometries.Point screenPosition, ZoomDirection direction, Viewport viewport = null)
        {
            ScreenPosition = screenPosition;
            Direction = direction;
            Viewport = viewport;
        }
    }
}
