using System;

namespace Ketchup.Utility
{
    internal static class FixedPoint
    {
        public static ushort Convert(double value)
        {
            // TODO: Test
            // TODO: Bounds checking
            return BitConverter.ToUInt16(BitConverter.GetBytes((short)(value * 0x100)), 0);
        }
    }
}
