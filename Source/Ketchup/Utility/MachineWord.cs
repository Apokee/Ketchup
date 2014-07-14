using System;

namespace Ketchup.Utility
{
    public static class MachineWord
    {
        /// <summary>
        /// Converts boolean to canonical machine word representation.
        /// </summary>
        /// <param name="boolean">Value to convert.</param>
        /// <returns>Canonical machine word representation.</returns>
        public static ushort FromBoolean(bool boolean)
        {
            return (ushort)(boolean ? 1 : 0);
        }

        /// <summary>
        /// Converts machine word to boolean.
        /// </summary>
        /// <remarks>
        /// 0x0000 is treated as false, any other value is treated as true.
        /// </remarks>
        /// <param name="word">Machine word to convert.</param>
        /// <returns>Boolean representation of the machine word.</returns>
        public static bool ToBoolean(ushort word)
        {
            return word != 0x0000;
        }

        /// <summary>
        /// Converts Int16 to canonical machine word representation.
        /// </summary>
        /// <param name="int16">Value to convert.</param>
        /// <returns>Canonical machine word representation.</returns>
        public static ushort FromInt16(short int16)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(int16), 0);
        }

        /// <summary>
        /// Converts machine word to Int16.
        /// </summary>
        /// <param name="word">Machine word to convert.</param>
        /// <returns>Int16 representation of the machine word.</returns>
        public static short ToInt16(ushort word)
        {
            return BitConverter.ToInt16(BitConverter.GetBytes(word), 0);
        }

        /// <summary>
        /// Converts UInt16 to canonical machine word representation.
        /// </summary>
        /// <param name="uint16">Value to convert.</param>
        /// <returns>Canonical machine word representation.</returns>
        public static ushort FromUInt16(ushort uint16)
        {
            return uint16;
        }

        /// <summary>
        /// Converts machine to UInt16.
        /// </summary>
        /// <param name="word">Machine word to convert.</param>
        /// <returns>UInt15 representation of the machine word.</returns>
        public static ushort ToUInt16(ushort word)
        {
            return word;
        }
    }
}
