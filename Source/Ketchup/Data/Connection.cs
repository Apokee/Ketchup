using System;

namespace Ketchup.Data
{
    internal sealed class Connection
    {
        private string _string;

        public ConnectionType ConnectionType { get; private set; }
        public Guid GlobalDeviceId { get; private set; }

        public Connection(ConnectionType connectionType, Guid globalDeviceId)
        {
            ConnectionType = connectionType;
            GlobalDeviceId = globalDeviceId;

            SetString();
        }

        public Connection(string serialized)
        {
            var colonIndex = serialized.IndexOf(':');

            var connectionType = serialized.Substring(0, colonIndex);
            var globalDeviceId = serialized.Substring(colonIndex + 1);

            ConnectionType = (ConnectionType)Byte.Parse(connectionType);
            GlobalDeviceId = new Guid(globalDeviceId);

            SetString();
        }

        private void SetString()
        {
            _string = String.Format("{0}:{1}", (byte)ConnectionType, GlobalDeviceId.ToString("N"));
        }

        public override string ToString()
        {
            return _string;
        }
    }
}
