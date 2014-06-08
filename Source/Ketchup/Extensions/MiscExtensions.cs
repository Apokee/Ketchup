
using UnityEngine;

namespace Ketchup.Extensions
{
    public static class MiscExtensions
    {
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
