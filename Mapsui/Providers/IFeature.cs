using System;
using System.Collections.Generic;
using Mapsui.Geometries;
using Mapsui.Styles;
using Mapsui.UI;

namespace Mapsui.Providers
{
    public interface IFeature: IUiEventReceiver
    {
        IGeometry Geometry { get; set; }
        IDictionary<IStyle, object> RenderedGeometry { get; }
        ICollection<IStyle> Styles { get; }
        object this[string key] { get; set; }
        IEnumerable<string> Fields { get; }
    }
}
