using System.Collections.Generic;
using FluentAssertions;
using Ketchup.Utility;
using Xunit.Extensions;

namespace Ketchup.Mathematics
{
    public sealed class RangeTests
    {
        #region Tests

        [Theory, PropertyData("Reduce_IsCorrect_TestCases")]
        internal void Reduce_IsCorrect(Range toRange, double value, double expectedResult)
        {
            // Arrange
            const double epsilon = 1e-12;

            // Act
            var result = toRange.Reduce(value);

            // Assert
            result.Should().BeApproximately(expectedResult, epsilon);
        }

        #endregion

        #region Test Cases

        public static IEnumerable<object[]> Reduce_IsCorrect_TestCases
        {
            get
            {
                var rangeNonNegative = new Range(0.0, 100.0);
                yield return new object[] { rangeNonNegative, 0.0, 0.0 };
                yield return new object[] { rangeNonNegative, 100.0, 100.0 };
                yield return new object[] { rangeNonNegative, 50.0, 50.0 };
                yield return new object[] { rangeNonNegative, 101.0, 1.0 };
                yield return new object[] { rangeNonNegative, -1.0, 99.0 };
                yield return new object[] { rangeNonNegative, 33.33, 33.33 };
                yield return new object[] { rangeNonNegative, 66.67, 66.67 };
                yield return new object[] { rangeNonNegative, 133.33, 33.33 };
                yield return new object[] { rangeNonNegative, 166.67, 66.67 };
                yield return new object[] { rangeNonNegative, -33.33, 66.67 };
                yield return new object[] { rangeNonNegative, -66.67, 33.33 };
                yield return new object[] { rangeNonNegative, 333.33, 33.33 };
                yield return new object[] { rangeNonNegative, -233.33, 66.67 };


                var rangeNonPositive = new Range(-100.0, 0.0);
                yield return new object[] { rangeNonPositive, -100.0, -100.0 };
                yield return new object[] { rangeNonPositive, 0.0, 0.0 };
                yield return new object[] { rangeNonPositive, -50.0, -50.0 };
                yield return new object[] { rangeNonPositive, 1.0, -99.0 };
                yield return new object[] { rangeNonPositive, -101.0, -1.0 };
                yield return new object[] { rangeNonPositive, -66.67, -66.67 };
                yield return new object[] { rangeNonPositive, -33.33, -33.33 };
                yield return new object[] { rangeNonPositive, 33.33, -66.67 };
                yield return new object[] { rangeNonPositive, 66.67, -33.33 };
                yield return new object[] { rangeNonPositive, -133.33, -33.33 };
                yield return new object[] { rangeNonPositive, -166.67, -66.67 };
                yield return new object[] { rangeNonPositive, 233.33, -66.67 };
                yield return new object[] { rangeNonPositive, -333.33, -33.33 };

                var rangeBoth = new Range(-50.0, 50.0);
                yield return new object[] { rangeBoth, -50.0, -50.0 };
                yield return new object[] { rangeBoth, 50.0, 50.0 };
                yield return new object[] { rangeBoth, 0.0, 0.0 };
                yield return new object[] { rangeBoth, 51.0, -49.0 };
                yield return new object[] { rangeBoth, -50.0, -50.0 };
                yield return new object[] { rangeBoth, -16.67, -16.67 };
                yield return new object[] { rangeBoth, 16.67, 16.67 };
                yield return new object[] { rangeBoth, 83.33, -16.67 };
                yield return new object[] { rangeBoth, 116.67, 16.67 };
                yield return new object[] { rangeBoth, -83.33, 16.67 };
                yield return new object[] { rangeBoth, -116.67, -16.67 };
                yield return new object[] { rangeBoth, 283.33, -16.67 };
                yield return new object[] { rangeBoth, -283.33, 16.67 };

                var rangePositive = new Range(100.0, 200.0);
                yield return new object[] { rangePositive, 100.0, 100.0 };
                yield return new object[] { rangePositive, 200.0, 200.0 };
                yield return new object[] { rangePositive, 150.0, 150.0 };
                yield return new object[] { rangePositive, 201.0, 101.0 };
                yield return new object[] { rangePositive, 99.0, 199.0 };
                yield return new object[] { rangePositive, 133.33, 133.33 };
                yield return new object[] { rangePositive, 166.67, 166.67 };
                yield return new object[] { rangePositive, 233.33, 133.33 };
                yield return new object[] { rangePositive, 266.67, 166.67 };
                yield return new object[] { rangePositive, 66.67, 166.67 };
                yield return new object[] { rangePositive, 33.33, 133.33 };
                yield return new object[] { rangePositive, 433.33, 133.33 };
                yield return new object[] { rangePositive, -133.33, 166.67 };

                var rangeNegative = new Range(-200.0, -100.0);
                yield return new object[] { rangeNegative, -200.0, -200.0 };
                yield return new object[] { rangeNegative, -100.0, -100.0 };
                yield return new object[] { rangeNegative, -150.0, -150.0 };
                yield return new object[] { rangeNegative, -99.0, -199.0 };
                yield return new object[] { rangeNegative, -201.0, -101.0 };
                yield return new object[] { rangeNegative, -166.67, -166.67 };
                yield return new object[] { rangeNegative, -133.33, -133.33 };
                yield return new object[] { rangeNegative, -66.67, -166.67 };
                yield return new object[] { rangeNegative, -33.33, -133.33 };

                yield return new object[] { rangeNegative, -233.33, -133.33 };
                yield return new object[] { rangeNegative, -266.67, -166.67 };
                yield return new object[] { rangeNegative, 133.33, -166.67 };
                yield return new object[] { rangeNegative, -433.33, -133.33 };

                var rangeSignedUnary = new Range(-1, 1);
                yield return new object[] { rangeSignedUnary, -1.0, -1.0 };
                yield return new object[] { rangeSignedUnary, 1.0, 1.0 };
                yield return new object[] { rangeSignedUnary, 0.0, 0.0 };
                yield return new object[] { rangeSignedUnary, 1.1, -0.9 };
                yield return new object[] { rangeSignedUnary, -1.1, 0.9 };
                yield return new object[] { rangeSignedUnary, 0.33, 0.33 };
                yield return new object[] { rangeSignedUnary, 0.67, 0.67 };
                yield return new object[] { rangeSignedUnary, 1.33, -0.67 };
                yield return new object[] { rangeSignedUnary, 1.67, -0.33 };
                yield return new object[] { rangeSignedUnary, -1.33, 0.67 };
                yield return new object[] { rangeSignedUnary, -1.67, 0.33 };
                yield return new object[] { rangeSignedUnary, 3.33, -0.67 };
                yield return new object[] { rangeSignedUnary, -3.33, 0.67 };
            }
        }

        #endregion
    }
}
