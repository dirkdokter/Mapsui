using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Mapsui.UI.Wpf
{
    static class PointExtensions
    {
        public static Geometries.Point ToMapsui(this Point point)
        {
            return new Geometries.Point(point.X, point.Y);
        }

        public static Geometries.Point ToMapsui(this TouchPoint point)
        {
            return new Geometries.Point(point.Position.X, point.Position.Y);
        }

        public static List<Geometries.Point> ToMapsui(this TouchPointCollection points)
        {
            var l = new List<Geometries.Point>();

            foreach (var p in points)
            {
                l.Add(p.ToMapsui());
            }

            return l;
        }

        public static List<Geometries.Point> ToMapsui(this IEnumerable<TouchPoint> points)
        {
            return points.Select(p => p.ToMapsui()).ToList();
        }


        public static List<Geometries.Point> ToMapsui(this IEnumerable<IManipulator> manipulators, IInputElement relativeTo)
        {
            return manipulators.Select(manipulator => manipulator.GetPosition(relativeTo).ToMapsui()).ToList();
        }
}
}