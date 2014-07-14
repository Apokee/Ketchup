using Ketchup.Extensions;

namespace Ketchup.Utility
{
    internal struct VesselReferenceFrame
    {
        private readonly Vector3d _origin;

        private readonly Vector3d _front;
        private readonly Vector3d _right;
        private readonly Vector3d _bottom;

        private readonly Vector3d _back;
        private readonly Vector3d _left;
        private readonly Vector3d _top;

        public Vector3d Origin { get { return _origin; } }

        public Vector3d Front { get { return _front; } }
        public Vector3d Right { get { return _right; } }
        public Vector3d Bottom { get { return _bottom; } }

        public Vector3d Back { get { return _back; } }
        public Vector3d Left { get { return _left; } }
        public Vector3d Top { get { return _top; } }

        public VesselReferenceFrame(Vector3d origin, Vector3d front, Vector3d right, Vector3d bottom)
        {
            _origin = origin;

            _front = front;
            _right = right;
            _bottom = bottom;

            _back = front.Negative();
            _left = right.Negative();
            _top = right.Negative();
        }

        public double GetHeading(NedReferenceFrame nedReferenceFrame)
        {
            var vesselForwardNorthEastProjection = Front.ProjectOntoPlane(nedReferenceFrame.Down);

            var headingRelNorth = vesselForwardNorthEastProjection.PreciseAngle(nedReferenceFrame.North);
            var headingRelEast = vesselForwardNorthEastProjection.PreciseAngle(nedReferenceFrame.East);

            double heading;

            if (headingRelNorth <= 90 && headingRelEast <= 90)
            {   // NE quadrant
                heading = headingRelNorth;
            }
            else if (headingRelNorth > 90 && headingRelEast <= 90)
            {   // SE quadrant
                heading = 90 + headingRelEast;
            }
            else if (headingRelNorth > 90 && headingRelNorth > 90)
            {   // SW quadrant
                heading = 90 + headingRelEast;
            }
            else
            {   // NW quadrant
                heading = 360 - headingRelNorth;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return heading == 360 ? 0 : heading;
        }

        public double GetPitch(NedReferenceFrame nedReferenceFrame)
        {
            return nedReferenceFrame.Down.PreciseAngle(Front) - 90;
        }

        public double GetRoll(NedReferenceFrame nedReferenceFrame)
        {
            var rightDownAngle = nedReferenceFrame.Down.PreciseAngle(Right);
            var bottomDownAngle = nedReferenceFrame.Down.PreciseAngle(Bottom);

            double roll;

            if (bottomDownAngle <= 90)
            {
                roll = 90 - rightDownAngle;
            }
            else
            {
                if (rightDownAngle <= 90)
                {
                    roll = rightDownAngle + 90;
                }
                else
                {
                    roll = rightDownAngle - 270;
                }
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return roll == -180 ? 180 : roll;
        }
    }
}
