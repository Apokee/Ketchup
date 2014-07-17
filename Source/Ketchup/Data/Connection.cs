using System;
using Ketchup.Api.v0;

namespace Ketchup.Data
{
    internal sealed class Connection
    {
        public ConnectionType ConnectionType { get; private set; }
        public Kuid Kuid { get; private set; }

        public Connection(ConnectionType connectionType, Kuid id)
        {
            ConnectionType = connectionType;
            Kuid = id;
        }

        public Connection(string str)
        {
            var colonIndex = str.IndexOf(':');

            var connectionType = str.Substring(0, colonIndex);
            var id = str.Substring(colonIndex + 1);

            ConnectionType = (ConnectionType)Byte.Parse(connectionType);
            Kuid = new Kuid(id);
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", (byte)ConnectionType, Kuid);
        }
    }
}
