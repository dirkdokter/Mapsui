using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapsui.Layers;
using Mapsui.Providers;

namespace Mapsui.UI
{
    public class BaseUiEventArgs : System.EventArgs
    {
        public bool Handled { get; set; } = false;
        public bool MapNeedsRefresh { get; set; } = false;

        public Viewport Viewport { get; protected set; }
        public ILayer Layer { get; set; }
        public IFeature Feature { get; set; }

        public bool ModifierCtrl { get; protected set; }
        public bool ModifierShift { get; protected set; }
    }
}
