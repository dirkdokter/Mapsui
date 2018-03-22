using System;

namespace Mapsui.UI
{
    public class TappedEventArgs : BaseUiEventArgs
    {
        public Geometries.Point ScreenPosition { get; }
        public int NumOfTaps { get; }



        public TappedEventArgs(Geometries.Point screenPosition, int numOfTaps, bool modifierCtrl = false, bool modifierShift = false, Viewport viewport = null)
        {
            ScreenPosition = screenPosition;
            NumOfTaps = numOfTaps;
            ModifierCtrl = modifierCtrl;
            ModifierShift = modifierShift;
            Viewport = viewport;
        }
    }
}
