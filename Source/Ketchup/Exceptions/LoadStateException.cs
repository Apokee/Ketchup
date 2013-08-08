using System;

namespace Ketchup.Exceptions
{
    public sealed class LoadStateException : Exception
    {
        private const string StandardMessage = "Failed to load DCPU-16 state.";

        internal LoadStateException(string message)
            : base(String.Format("{0} {1}", StandardMessage, message)) { }

        internal LoadStateException(Exception e)
            : base(StandardMessage, e) { }
    }
}
