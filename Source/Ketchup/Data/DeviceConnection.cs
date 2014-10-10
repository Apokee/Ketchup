using System;
using System.Globalization;
using Ketchup.Api.v0;

namespace Ketchup.Data
{
    internal sealed class DeviceConnection : IConfigNode
    {
        private const string ConfigKeyType = "Type";
        private const string ConfigKeyHardwareId = "HardwareId";
        private const string ConfigNodeDevicePort = "DEVICE_PORT";

        public DeviceConnectionType Type { get; private set; }
        public Port Port { get; private set; }
        public ushort? HardwareId { get; private set; }

        public DeviceConnection(DeviceConnectionType connectionType, Port port, ushort? hardwareId)
        {
            Type = connectionType;
            Port = port;
            HardwareId = hardwareId;
        }

        public DeviceConnection(ConfigNode node)
        {
            Load(node);
        }

        public override string ToString()
        {
            return String.Format("{0},{1}", (byte)Type, Port);
        }

        public void Load(ConfigNode node)
        {
            Type = (DeviceConnectionType)Enum.Parse(typeof(DeviceConnectionType), node.GetValue(ConfigKeyType));
            var hardwareIdStr = node.GetValue(ConfigKeyHardwareId);
            if (!String.IsNullOrEmpty(hardwareIdStr))
            {
                HardwareId = UInt16.Parse(hardwareIdStr);
            }
            Port = new Port(node.GetNode(ConfigNodeDevicePort));
        }

        public void Save(ConfigNode node)
        {
            var devicePortNode = new ConfigNode(ConfigNodeDevicePort);
            Port.Save(devicePortNode);

            node.AddValue(ConfigKeyType, Type.ToString());
            if (HardwareId != null)
            {
                node.AddValue(ConfigKeyHardwareId, HardwareId.Value.ToString(CultureInfo.InvariantCulture));
            }
            node.AddNode(devicePortNode);
        }
    }
}
