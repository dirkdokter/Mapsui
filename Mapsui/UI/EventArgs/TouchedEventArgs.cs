using System;
using System.Collections.Generic;
using Mapsui.Geometries;

namespace Mapsui.UI
{
    public class TouchedEventArgs : BaseUiEventArgs
    {
        public List<Geometries.Point> ScreenPoints { get; }

        
        public Point Center { get; }
        public double Angle { get; }
        public double Radius { get; }
        public bool OriginalTouchpointsAvaiable { get;  }


        public int? Timestamp { get; set; }
        public TouchedEventArgs(List<Geometries.Point> screenPoints, int? timestamp = null, Viewport viewport = null)
        {
            ScreenPoints = screenPoints;
            OriginalTouchpointsAvaiable = true;
            Timestamp = timestamp;
            Viewport = viewport;
        }

        public TouchedEventArgs(Geometries.Point centerPoint, double radius = Double.NaN, double angle = Double.NaN, List<Geometries.Point> screenPoints = null, int? timestamp = null, Viewport viewport = null)
        {
            ScreenPoints = screenPoints;
            OriginalTouchpointsAvaiable = screenPoints != null;
            Center = centerPoint;
            Radius = radius;
            Angle = angle;
            Timestamp = timestamp;
            Viewport = viewport;
        }
    }
}
