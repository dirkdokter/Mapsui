using System;

namespace Mapsui.UI
{
    public class TappedEventArgs : EventArgs
    {
        public Geometries.Point ScreenPosition { get; }
        public int NumOfTaps { get; }
        public bool Handled { get; set; } = false;

        public bool ModifierCtrl { get; }
        public bool ModifierShift { get; }

        public TappedEventArgs(Geometries.Point screenPosition, int numOfTaps, bool modifierCtrl = false, bool modifierShift = false)
        {
            ScreenPosition = screenPosition;
            NumOfTaps = numOfTaps;
            ModifierCtrl = modifierCtrl;
            ModifierShift = modifierShift;
        }
    }
}
