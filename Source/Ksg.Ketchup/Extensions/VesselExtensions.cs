using Ksg.Ketchup.Mathematics;

namespace Ksg.Ketchup.Extensions
{
    internal static class VesselExtensions
    {
        public static NedReferenceFrame GetNedReferenceFrame(this Vessel vessel)
        {
            var referenceBody = vessel.mainBody;
            var referenceBodyNorth = referenceBody.transform.up.normalized;

            var down = -referenceBody.GetSurfaceNVector(vessel.latitude, vessel.longitude).normalized;
            var east = Vector3d.Cross(referenceBodyNorth, down).normalized;
            var north = Vector3d.Cross(down, east).normalized;

            return new NedReferenceFrame(vessel.findWorldCenterOfMass(), north, east, down);
        }

        public static VesselReferenceFrame GetVesselReferenceFrame(this Vessel vessel)
        {
            return new VesselReferenceFrame(
                vessel.findWorldCenterOfMass(),
                vessel.transform.up.normalized,
                vessel.transform.right.normalized,
                vessel.transform.forward.normalized
            );
        }
    }
}
