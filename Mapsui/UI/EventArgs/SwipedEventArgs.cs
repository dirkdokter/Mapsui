using System;

namespace Mapsui.UI
{
    public class SwipedEventArgs : BaseUiEventArgs
    {
        public double VelocityX { get; } // Velocity in pixel/second
        public double  VelocityY { get; } // Velocity in pixel/second

        public SwipedEventArgs(double velocityX, double velocityY)
        {
            VelocityX = velocityX;
            VelocityY = velocityY;
        }
    }
}
