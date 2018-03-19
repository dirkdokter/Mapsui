using System;
using System.Collections.Generic;

namespace Mapsui.UI
{
    public class TouchedEventArgs : EventArgs
    {
        public List<Geometries.Point> ScreenPoints { get; }
        public bool Handled { get; set; } = false;
        public int? Timestamp { get; set; }

        public TouchedEventArgs(List<Geometries.Point> screenPoints, int? timestamp = null)
        {
            ScreenPoints = screenPoints;
            Timestamp = timestamp;
        }
    }
}
