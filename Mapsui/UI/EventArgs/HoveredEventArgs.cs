using System;

namespace Mapsui.UI
{
    public class HoveredEventArgs : BaseUiEventArgs
    {
        public Geometries.Point ScreenPosition { get; }

        public HoveredEventArgs(Geometries.Point screenPosition, Viewport viewport = null)
        {
            ScreenPosition = screenPosition;
            Viewport = viewport;
        }
    }
}
