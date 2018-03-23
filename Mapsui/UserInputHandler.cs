using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapsui.Geometries;

namespace Mapsui
{
    public class UserInputHandler
    {
        private Point _previousMousePosition;
        private Point _mouseDownPosition;

        public event EventHandler PinchEvent;
        public event EventHandler MouseScrollEvent;
        public event EventHandler ClickEvent;
        public event EventHandler ClickEvent;

    }
}
