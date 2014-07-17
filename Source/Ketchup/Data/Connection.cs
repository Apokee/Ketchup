using System;

namespace Ketchup.Data
{
    internal sealed class Connection
    {
        private readonly string _string;

        public ConnectionType ConnectionType { get; private set; }
        public Guid KetchupId { get; private set; }

        public Connection(ConnectionType connectionType, Guid ketchupId)
        {
            ConnectionType = connectionType;
            KetchupId = ketchupId;

            _string = String.Format("{0}:{1}", (byte)connectionType, ketchupId.ToString("N"));
        }

        public Connection(string serialized)
        {
            var colonIndex = serialized.IndexOf(':');

            var connectionType = serialized.Substring(0, colonIndex);
            var ketchupId = serialized.Substring(colonIndex + 1);

            ConnectionType = (ConnectionType)Byte.Parse(connectionType);
            KetchupId = new Guid(ketchupId);
        }

        public override string ToString()
        {
            return _string;
        }
    }
}
