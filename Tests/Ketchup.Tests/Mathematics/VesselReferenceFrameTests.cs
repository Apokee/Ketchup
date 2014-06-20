using System;
using System.Collections.Generic;
using FluentAssertions;
using Ketchup.Extensions;
using Xunit.Extensions;

namespace Ketchup.Mathematics
{
    public sealed class VesselReferenceFrameTests
    {
        #region Tests

        [Theory, PropertyData("GetRoll_IsCorrect_TestCases")]
        internal void GetRoll_IsCorrect(VesselReferenceFrame vesselReferenceFrame, double expectedRoll)
        {
            // Arrange
            const double epsilon = 1e-12;

            var nedReferenceFrame = new NedReferenceFrame(
                Vector3d.zero,
                new Vector3d(1, 0, 0),
                new Vector3d(0, 1, 0),
                new Vector3d(0, 0, -1)
            );

            // Act
            var result = vesselReferenceFrame.GetRoll(nedReferenceFrame);

            // Assert
            result.Should().BeApproximately(expectedRoll, epsilon);
        }

        #endregion

        #region Test Cases

        public static IEnumerable<object[]> GetRoll_IsCorrect_TestCases
        {
            get
            {
                for (var i = -175; i <= 180; i += 5)
                {
                    yield return new object[] { GetVesselReferenceFrame(i), i };
                }
            }
        }

        #endregion

        #region Helpers

        private static VesselReferenceFrame GetVesselReferenceFrame(double degrees)
        {
            var rightRad = (360 - degrees).DegreesToRadians();
            var bottomDegrees = (270 - degrees).DegreesToRadians();

            return new VesselReferenceFrame(Vector3d.zero,
                new Vector3d(1, 0, 0),
                new Vector3d(0, Math.Cos(rightRad), Math.Sin(rightRad)),
                new Vector3d(0, Math.Cos(bottomDegrees), Math.Sin(bottomDegrees))
            );
        }

        #endregion
    }
}
