using System;
using System.Collections.Generic;

namespace Mapsui.UI
{
    public class TouchedEventArgs : BaseUiEventArgs
    {
        public List<Geometries.Point> ScreenPoints { get; }
        public int? Timestamp { get; set; }
        public TouchedEventArgs(List<Geometries.Point> screenPoints, int? timestamp = null, Viewport viewport = null)
        {
            ScreenPoints = screenPoints;
            Timestamp = timestamp;
            Viewport = viewport;
        }
    }
}
