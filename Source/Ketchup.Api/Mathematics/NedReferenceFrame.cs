using Ketchup.Extensions;

namespace Ketchup.Mathematics
{
    /// <summary>
    /// North East Down (NED) reference frame.
    /// </summary>
    public struct NedReferenceFrame
    {
        private readonly Vector3d _origin;

        private readonly Vector3d _north;
        private readonly Vector3d _east;
        private readonly Vector3d _down;

        private readonly Vector3d _south;
        private readonly Vector3d _west;
        private readonly Vector3d _up;

        public Vector3d Origin { get { return _origin; } }

        public Vector3d North { get { return _north; } }
        public Vector3d East { get { return _east; } }
        public Vector3d Down { get { return _down; } }

        public Vector3d South { get { return _south; } }
        public Vector3d West { get { return _west; } }
        public Vector3d Up { get { return _up; } }

        public NedReferenceFrame(Vector3d origin, Vector3d north, Vector3d east, Vector3d down)
        {
            _origin = origin;

            _north = north;
            _east = east;
            _down = down;

            _south = north.Negative();
            _west = east.Negative();
            _up = down.Negative();
        }
    }
}
