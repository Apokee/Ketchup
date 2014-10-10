using Ketchup.Api.v0;

namespace Ketchup.Interop
{
    // TODO: This class exists to act as a placeholder when a device is disconnected while the CPU is running
    // TODO: This could be a singleton
    internal sealed class DisconnectedDevice : IDevice
    {
        public string FriendlyName
        {
            get { return "Disconnected Device"; }
        }

        public uint ManufacturerId
        {
            get { return 0x00000000; }
        }

        public uint DeviceId
        {
            get { return 0x00000000; }
        }

        public ushort Version
        {
            get { return 0x0000; }
        }

        public Port Port { get; set; }

        public void OnConnect(IDcpu16 dcpu16)
        {
        }

        public void OnDisconnect()
        {
        }

        public int OnInterrupt()
        {
            // TODO: There should be some kind of penalty for trying to interrupt a device which does not exist
            return 0;
        }
    }
}
