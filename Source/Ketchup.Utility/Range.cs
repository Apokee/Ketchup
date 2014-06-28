using System;

namespace Ketchup.Utility
{
    public class Range
    {
        #region Constants

        /// <summary>
        /// [-32768, 32767]
        /// </summary>
        public static readonly Range SignedInt16 = new Range(Int16.MinValue, Int16.MaxValue);

        /// <summary>
        /// [0, 65535]
        /// </summary>
        public static readonly Range UnsignedInt16 = new Range(UInt16.MinValue, UInt16.MaxValue);

        /// <summary>
        /// [-1, 1]
        /// </summary>
        public static readonly Range SignedUnary = new Range(-1, 1);

        /// <summary>
        /// [0, 1]
        /// </summary>
        public static readonly Range UnsignedUnary = new Range(0, 1);

        /// <summary>
        /// [-180, 180]
        /// </summary>
        public static readonly Range SignedDegreesCircle = new Range(-180, 180);

        /// <summary>
        /// [0, 360]
        /// </summary>
        public static readonly Range UnsignedDegreesCircle = new Range(0, 360);

        /// <summary>
        /// [-90, 90]
        /// </summary>
        public static readonly Range SignedDegreesHalfCircle = new Range(-90, 90);

        /// <summary>
        /// [0, 180]
        /// </summary>
        public static readonly Range UnsignedDegreesHalfCircle = new Range(0, 180);

        #endregion

        #region Instance Members

        private readonly string _string;

        public double Min { get; private set; }
        public double Max { get; private set; }

        public double Magnitude { get { return Max - Min; } }

        #endregion

        #region Constructors

        public Range(double min, double max)
        {
            if (!(min < max))
            {
                throw new ArgumentException("min must be strictly less than max.");
            }

            Min = min;
            Max = max;

            _string = String.Format("[{0}, {1}]", min, max);
        }

        #endregion

        #region Instance Methods

        public bool Contains(double value)
        {
            return value >= Min && value <= Max;
        }

        /// <summary>
        /// Reduces <see cref="value"/> into the specified range assuming the range wraps around indefinitely.
        /// </summary>
        /// <param name="value">The value to reduce.</param>
        /// <returns>A value in the specified range.</returns>
        public double Reduce(double value)
        {
            if (value < Min)
            {
                return Max - ((Min - value) % Magnitude);
            }
            else if (value > Max)
            {
                return Min + ((value - Max) % Magnitude);
            }
            else
            {
                return value;
            }
        }

        public double ScaleFrom(Range fromRange, double value)
        {
            return Scale(fromRange, this, value);
        }

        public double ScaleTo(Range toRange, double value)
        {
            return Scale(this, toRange, value);
        }

        public override string ToString()
        {
            return _string;
        }

        #endregion

        #region Static Methods

        #region Scale

        public static double Scale(Range fromRange, Range toRange, double value)
        {
            if (value < fromRange.Min || value > fromRange.Max)
            {
                throw new ArgumentException("value must be in the range of fromRange");
            }

            return toRange.Min + (toRange.Magnitude * ((value - fromRange.Min) / fromRange.Magnitude));
        }

        /// <summary>
        /// Scales <see cref="value"/> from a range of [-32768, 32767] to [-1, 1].
        /// </summary>
        /// <param name="value">Value in the range [-32768, 32767] to scale.</param>
        /// <returns>Scaled <see cref="value"/> in the range [-1, 1].</returns>
        public static float ScaleSignedInt16ToSignedUnary(short value)
        {
            return (float)Scale(SignedInt16, SignedUnary, value);
        }

        /// <summary>
        /// Scales <see cref="value"/> from a range of [-1, 1] to [-32768, 32767].
        /// </summary>
        /// <param name="value">Value in the range [-1, 1] to scale.</param>
        /// <returns>Scaled <see cref="value"/> in the range [-32768, 32767]</returns>
        public static short ScaleSignedUnaryToSignedInt16(float value)
        {
            return (short)Scale(SignedUnary, SignedInt16, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float ScaleUnsignedInt16ToUnsignedUnary(ushort value)
        {
            return (float)Scale(UnsignedInt16, UnsignedUnary, value);
        }

        /// <summary>
        /// Scales <see cref="value"/> from a range of [0, 1] to [0, 65535].
        /// </summary>
        /// <param name="value">Value in the range [0, 1] to scale.</param>
        /// <returns>Scaled <see cref="value"/> in the range [0, 65535]</returns>
        public static ushort ScaleUnsignedUnaryToUnsignedInt16(float value)
        {
            return (ushort)Scale(UnsignedUnary, UnsignedInt16, value);
        }

        #endregion

        #endregion
    }
}
