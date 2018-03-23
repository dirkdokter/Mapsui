using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mapsui.UI.Wpf
{
    public static class TouchPointHelper
    {
        public static IEnumerable<TouchPoint> GetPressedPoints(TouchPointCollection c)
        {
            return c.Where(a => (a.Action == TouchAction.Down || a.Action == TouchAction.Move));
            //return c.Where(a => (a.Action == TouchAction.Move));
        }

        public static TouchPoint GetReleasedPoint(TouchPointCollection c)
        {
            return c.First(a => (a.Action == TouchAction.Up));
        }
    }
}
