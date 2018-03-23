using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Mapsui.Utilities;

namespace Mapsui.UI.Uwp
{
    public partial class MapControl
    {
        private double _innerRotation;

        private bool ModifierCtrlPressed => (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));
        private bool ModifierShiftPressed => (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down));

        private void _addEventHandlers()
        {

            /*PointerPressed += MapControlMouseLeftButtonDown;  XXX
            PointerReleased += MapControlMouseLeftButtonUp;  XXX
            PointerMoved += MapControlMouseMove;
            PointerWheelChanged += MapControlMouseWheel;  XXX

           
            ManipulationStarting += MapControlManipulationStarting;
            ManipulationStarted += MapControlManipulationStarted;
            ManipulationDelta += MapControlManipulationDelta;
            ManipulationCompleted += MapControlManipulationCompleted;

            IsManipulationEnabled = true;*/


            //PointerWheelChanged += MapControl_PointerWheelChanged;

            /*ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Rotate;
            Tapped += OnSingleTapped;
            DoubleTapped += OnDoubleTapped;*/

            //PointerPressed += OnPointerPressedHandler;

            Tapped += MapControlTappedHandler;
            DoubleTapped += MapControlDoubleTappedHandler;

            PointerMoved += MapControlPointerMoved;
            PointerWheelChanged += MapControlMouseWheel;

            ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Rotate;
            ManipulationStarting += MapControlManipulationStarting;
            ManipulationStarted += MapControlManipulationStarted;
            ManipulationDelta += MapControlManipulationDelta;
            ManipulationCompleted += MapControlManipulationCompleted;

        }

        private bool MouseButtonsReleased(PointerPoint p)
        {
            return !(p.Properties.IsLeftButtonPressed || p.Properties.IsMiddleButtonPressed ||
                     p.Properties.IsRightButtonPressed);
        }

        private void MapControlPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse &&
                MouseButtonsReleased(e.GetCurrentPoint(this)))
            {
                e.Handled = OnHovered(e.GetCurrentPoint(this).Position.ToMapsui());
            }
        }

        private void MapControlDoubleTappedHandler(object sender, DoubleTappedRoutedEventArgs e)
        {
            OnDoubleTapped(e.GetPosition(this).ToMapsui(), 2);
            e.Handled = true;
        }

        private void MapControlTappedHandler(object sender, TappedRoutedEventArgs e)
        {
            OnSingleTapped(e.GetPosition(this).ToMapsui(), null);
            e.Handled = true;
        }


        /// 
        /// Mouse wheel
        /// 
        private void MapControlMouseWheel(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            var position = e.GetCurrentPoint(this).Position.ToMapsui();
            if (delta > Constants.Epsilon)
            {
                OnZoomIn(position);
            }
            else if (delta < Constants.Epsilon)
            {
                OnZoomOut(position);
            }
            e.Handled = true;
        }

        private void MapControlManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void MapControlManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            OnTouchStart(e.Position.ToMapsui(), null);
            e.Handled = true;
        }
        private void MapControlManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var hasBeenManipulatedSignificantly = Math.Abs(e.Delta.Translation.X) > MinimumDragDistance
                                                  || Math.Abs(e.Delta.Translation.Y) > MinimumDragDistance;

            if (hasBeenManipulatedSignificantly)
            {
                OnTouchMove(e.Position.ToMapsui(), e.Delta.Expansion, e.Delta.Rotation);
            }

            e.Handled = true;
        }

        private void MapControlManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OnTouchEnd(e.Position.ToMapsui());
        }
        /* 

        ///
        /// Mouse related
        ///
        /*private void MapControlMouseLeave(object sender, PointerRoutedEventArgs e)
        {
            _mode = TouchMode.None;
            ReleasePointerCaptures();
        }
        
        private bool MouseButtonsReleased(PointerRoutedEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Released
                   && e.MiddleButton == MouseButtonState.Released
                   && e.RightButton == MouseButtonState.Released;
        }

        private void MapControlMouseMove(object sender, PointerRoutedEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                OnTouchMove(new List<Geometries.Point>() { e.GetPosition(this).ToMapsui() });
            }
            else if (MouseButtonsReleased(e))
            {
                OnHovered(e.GetPosition(this).ToMapsui());
            }
        }



        /// 
        /// Manipulation/touch related
        /// 
      
        private void MapControlManipulationDelta(object sender, PointerRoutedEventArgs e)
        {
            if (e.IsInertial)
            {
                Console.WriteLine(e.Manipulators.Count());
            }
            if (_mode == TouchMode.Dragging && e.Manipulators.Count() >= 2)
            {
                // Re-start the touch operation
                OnTouchStart(e.Manipulators.ToMapsui(this));
            }

            var hasBeenManipulatedSignificantly = Math.Abs(e.DeltaManipulation.Translation.X) > SystemParameters.MinimumHorizontalDragDistance
                                                  || Math.Abs(e.DeltaManipulation.Translation.Y) > SystemParameters.MinimumVerticalDragDistance;

            if (hasBeenManipulatedSignificantly)
            {
                OnTouchMove(e.Manipulators.ToMapsui(this));
            }

            e.Handled = true;
        }

        private void MapControlManipulationCompleted(object sender, PointerRoutedEventArgs e)
        {
            OnTouchEnd(e.Manipulators.ToMapsui(this), e.ManipulationOrigin.ToMapsui(), e.Timestamp);
        }
        */


    }
}
