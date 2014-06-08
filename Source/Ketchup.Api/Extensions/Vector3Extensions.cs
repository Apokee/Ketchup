using System;

namespace Ketchup.Extensions
{
    public static class Vector3Extensions
    {
        public static double PreciseAngle(this Vector3d from, Vector3d to)
        {
            return Math.Acos(Math.Min(Math.Max(Vector3d.Dot(from.normalized, to.normalized), -1.0), 1.0))
                .RadiansToDegrees();
        }

        // FIXME: does this do what I intend it to do??

        public static Vector3d Negative(this Vector3d vector)
        {
            return -vector;
        }

        public static Vector3d ProjectOntoPlane(this Vector3d vector, Vector3d planeNormal)
        {
            return vector - Vector3d.Project(vector, planeNormal);
        }
    }
}
