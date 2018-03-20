using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Mapsui.Utilities;

namespace Mapsui.UI.Wpf
{
    public partial class MapControl
    {
        private double _innerRotation;

        private bool ModifierCtrlPressed => (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
        private bool ModifierShiftPressed => (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));


        private void _addEventHandlers()
        {
            MouseLeftButtonDown += MapControlMouseLeftButtonDown;
            MouseLeftButtonUp += MapControlMouseLeftButtonUp;
            MouseMove += MapControlMouseMove;
            MouseWheel += MapControlMouseWheel;

            MouseLeave += MapControlMouseLeave;
            
            ManipulationStarting += MapControlManipulationStarting;
            ManipulationStarted += MapControlManipulationStarted;
            ManipulationDelta += MapControlManipulationDelta;
            ManipulationCompleted += MapControlManipulationCompleted;

            IsManipulationEnabled = true;
        }



        ///
        /// Mouse related
        ///
        private void MapControlMouseLeave(object sender, MouseEventArgs e)
        {
            _mode = TouchMode.None;
            ReleaseMouseCapture();
        }


        private void MapControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CaptureMouse();

            if (e.ClickCount > 1)
            {
                OnDoubleTapped(e.GetPosition(this).ToMapsui(), e.ClickCount);
            }
            else
            {
                OnTouchStart(e.GetPosition(this).ToMapsui());
            }
        }

        private void MapControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                throw new Exception("This should not happen. See https://github.com/pauldendulk/Mapsui/issues/344.");
            }

            OnTouchEnd(e.GetPosition(this).ToMapsui());

            ReleaseMouseCapture();
        }

        private bool MouseButtonsReleased(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Released
                   && e.MiddleButton == MouseButtonState.Released
                   && e.RightButton == MouseButtonState.Released;
        }

        private void MapControlMouseMove(object sender, MouseEventArgs e)
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
        /// Mouse wheel
        /// 
        private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > Constants.Epsilon)
            {
                OnZoomIn(e.GetPosition(this).ToMapsui());
            }
            else if (e.Delta < Constants.Epsilon)
            {
                OnZoomOut(e.GetPosition(this).ToMapsui());
            }
            e.Handled = true;
        }


        /// 
        /// Manipulation/touch related
        /// 
        private void MapControlManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            // Capture touch to MapControl
            e.ManipulationContainer = this;
            e.Handled = true;
        }

        private void MapControlManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            OnTouchStart(e.Manipulators.ToMapsui(this), e.Timestamp); // Always one manipulator
        }

        private void MapControlManipulationDelta(object sender, ManipulationDeltaEventArgs e)
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

        private void MapControlManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            OnTouchEnd(e.Manipulators.ToMapsui(this), e.ManipulationOrigin.ToMapsui(), e.Timestamp);
        }



    }
}
