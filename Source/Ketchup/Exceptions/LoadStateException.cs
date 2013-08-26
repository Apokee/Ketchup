using System;

namespace Ketchup.Exceptions
{
    public sealed class LoadStateException : Exception
    {
        private const string StandardMessage = "Failed to load {0} state.";

        internal LoadStateException(string device, string message)
            : base(String.Format("{0} {1}", String.Format(StandardMessage, device), message)) { }

        internal LoadStateException(Exception e)
            : base(StandardMessage, e) { }
    }
}
