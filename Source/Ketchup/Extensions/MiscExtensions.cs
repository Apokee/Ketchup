using Ketchup.Api;

namespace Ketchup.Extensions
{
    public static class MiscExtensions
    {
        public static bool IsHalted(this IDcpu16 dcpu16)
        {
            return dcpu16.Memory[dcpu16.PC] == 0x8382; // ADD PC, -1
        }
    }
}
