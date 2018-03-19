using System.Collections.Generic;
using System.Linq;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.Utilities;

namespace Mapsui.Samples.Common.Maps
{
    public static class InteractiveInfoLayersSample
    {
        public static Map CreateMap()
        {
            var map = InfoLayersSample.CreateMap();
            map.Info += TestHandler;
            return map;
        }

        private static void TestHandler(object sender, InfoEventArgs e)
        {
            if (e.Layer?.Name == "Info Layer")
            {
                e.Handled = true;
            }
        }
    }
}