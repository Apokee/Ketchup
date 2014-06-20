using System;

namespace Ketchup.Extensions
{
    internal static class DoubleExtensions
    {
        public static double DegreesToRadians(this double value)
        {
            return value * (Math.PI / 180);
        }

        public static double RadiansToDegrees(this double value)
        {
            return value * (180 / Math.PI);
        }
    }
}
