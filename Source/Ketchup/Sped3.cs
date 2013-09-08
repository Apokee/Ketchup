using Ketchup.Api;

namespace Ketchup
{
    public sealed class Sped3 : PartModule, IDevice
    {
        #region Instance Fields

        private IDcpu16 _dcpu16;

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "Mackapar Suspended Particle Exciter Display, Rev 3 (SPED-3)"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.Mackapar; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Sped3; }
        }

        public ushort Version
        {
            get { return 0x0003; }
        }

        #endregion

        #region IDevice Methods

        public void OnConnect(IDcpu16 dcpu16)
        {
            _dcpu16 = dcpu16;
        }

        public void OnDisconnect()
        {
            _dcpu16 = null;
        }

        public int OnInterrupt()
        {
            return 0;
        }

        #endregion
    }
}
