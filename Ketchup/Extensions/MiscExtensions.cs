using System.Linq;
using UnityEngine;

namespace Ketchup.Extensions
{
    public static class MiscExtensions
    {
        public static bool IsPrimary(this PartModule partModule)
        {
            return partModule.vessel.parts.FirstOrDefault(i => i.Modules.Contains(partModule.ClassID)) == partModule.part;
        }

        public static Rect CenterScreen(this Rect rect)
        {
            if (Screen.width > 0 && Screen.height > 0 && rect.width > 0 && rect.height > 0)
            {
                rect.x = Screen.width / 2 - rect.width / 2;
                rect.y = Screen.height / 2 - rect.height / 2;
            }

            return rect;
        }
    }
}
