using Ketchup.Api;
using UnityEngine;

namespace Ketchup.Extensions
{
    public static class MiscExtensions
    {
        public static bool IsHalted(this IDcpu16 dcpu16)
        {
            return dcpu16.Memory[dcpu16.PC] == 0x8382; // ADD PC, -1
        }

        public static Rect CenteredOnScreen(this Rect rect)
        {
            return new Rect(rect)
            {
                x = (Screen.width / 2f) - (rect.width / 2),
                y = (Screen.height / 2f) - (rect.height / 2)
            };
        }
    }
}
