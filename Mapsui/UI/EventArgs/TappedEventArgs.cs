using System;

namespace Mapsui.UI
{
    /*public enum TappedInput
    {
        Touchscreen,
        MouseLeft,
        MouseRight,
        MouseMiddle
    }*/

    public class TappedEventArgs : BaseUiEventArgs
    {
        public Geometries.Point ScreenPosition { get; }
        public int NumOfTaps { get; }

        //public TappedInput TappedInput { get; }


        public TappedEventArgs(Geometries.Point screenPosition, int numOfTaps, /*TappedInput tappedInput,*/ bool modifierCtrl = false, bool modifierShift = false, Viewport viewport = null)
        {
            ScreenPosition = screenPosition;
            NumOfTaps = numOfTaps;
            //TappedInput = tappedInput;
            ModifierCtrl = modifierCtrl;
            ModifierShift = modifierShift;
            Viewport = viewport;
        }
    }
}
