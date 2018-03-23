using System;
using Mapsui.Layers;
using Mapsui.Providers;

namespace Mapsui.UI
{
    public class HoveredEventArgs : BaseUiEventArgs
    {
        public Geometries.Point ScreenPosition { get; }

        public HoveredEventArgs(Geometries.Point screenPosition, Viewport viewport = null, bool modifierCtrl = false, bool modifierShift = false)
        {
            ScreenPosition = screenPosition;
            Viewport = viewport;
            ModifierCtrl = modifierCtrl;
            ModifierShift = modifierShift;
        }
    }
}
