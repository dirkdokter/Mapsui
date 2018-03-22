using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapsui.UI
{
    public class BaseUiEventArgs : System.EventArgs
    {
        public Viewport Viewport { get; protected set; }
        public bool Handled { get; set; } = false;
        public bool MapNeedsRefresh { get; set; } = false;
        public bool ModifierCtrl { get; protected set; }
        public bool ModifierShift { get; protected set; }
    }
}
