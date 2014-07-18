using System;
using Ketchup.Api.v0;

namespace Ketchup.Data
{
    internal sealed class DeviceConnection : IConfigNode
    {
        private const string ConfigKeyType = "Type";
        private const string ConfigNodeDevicePort = "DEVICE_PORT";

        public DeviceConnectionType Type { get; private set; }
        public Port Port { get; private set; }

        public DeviceConnection(DeviceConnectionType connectionType, Port port)
        {
            Type = connectionType;
            Port = port;
        }

        public DeviceConnection(ConfigNode node)
        {
            Load(node);
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", (byte)Type, Port);
        }

        public void Load(ConfigNode node)
        {
            Type = (DeviceConnectionType)Enum.Parse(typeof(DeviceConnectionType), node.GetValue(ConfigKeyType));
            Port = new Port(node.GetNode(ConfigNodeDevicePort));
        }

        public void Save(ConfigNode node)
        {
            var devicePortNode = new ConfigNode(ConfigNodeDevicePort);
            Port.Save(devicePortNode);

            node.AddValue(ConfigKeyType, Type.ToString());
            node.AddNode(devicePortNode);
        }
    }
}
