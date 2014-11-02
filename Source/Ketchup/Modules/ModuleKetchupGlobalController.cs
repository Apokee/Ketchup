using System;
using Ketchup.Api.v0;
using Ketchup.Utility;

namespace Ketchup.Modules
{
    [KSPModule("Device: Global Controller")]
    internal sealed class ModuleKetchupGlobalController : PartModule, IDevice
    {
        #region Device Identifiers

        public string FriendlyName { get { return "Global Controller"; } }
        public uint ManufacturerId { get { return (uint)Constants.ManufacturerId.Apokee; } }
        public uint DeviceId { get { return 0x73B0AD20; } } // FIXME: Original DeviceId was too long
        public ushort Version { get { return 0x0001; } }
        public Port Port { get; set; }

        #endregion

        #region Instance Members

        private IDcpu16 _dcpu16;

        #endregion

        #region IDevice

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
            switch(_dcpu16.A)
            {
                case 0x0000: // TODO: Test
                    _dcpu16.A = (ushort)(((uint)vessel.missionTime) >> 16);
                    break;
                case 0x0001: // TODO: Test
                    _dcpu16.A = (ushort)(((uint)vessel.missionTime) & 0x00001111);
                    break;
                case 0x0002: // TODO: Test
                    _dcpu16.A = (ushort)(((uint)Planetarium.GetUniversalTime()) >> 16);
                    break;
                case 0x0003: // TODO: Test
                    _dcpu16.A = (ushort)(((uint)Planetarium.GetUniversalTime()) & 0x00001111);
                    break;
                case 0x0004: // TODO: Test
                    // QUESTION: What if not the active vessel?
                    _dcpu16.A = (ushort)(FlightInputHandler.state.mainThrottle * 100);
                    break;
                case 0x0005:
                    // FIXME: The total number of stages in a rocket isn't really defined.
                    break;
                case 0x0006: // TODO: Test
                    _dcpu16.A = (ushort)vessel.currentStage;
                    break;
                case 0x0007: // TODO: Test
                    if (vessel.altitude >= 0)
                    {
                        _dcpu16.A = (ushort)(((uint)vessel.altitude) >> 16);
                    }
                    else
                    {
                        // FIXME: Undefined when altitude is negative (below sea-level)
                    }
                    break;
                case 0x0008: // TODO: Test
                    if (vessel.altitude >= 0)
                    {
                        _dcpu16.A = (ushort)(((uint)vessel.altitude) & 0x000011116);
                    }
                    else
                    {
                        // FIXME: Undefined when altitude is negative (below sea-level)
                    }
                    break;
                case 0x0009: // TODO: Test
                    if (vessel.terrainAltitude >= 0)
                    {
                        if (vessel.terrainAltitude <= UInt16.MaxValue)
                        {
                            _dcpu16.A = (ushort)vessel.terrainAltitude;
                        }
                        else
                        {
                            // FIXME: Undefined when terrain altitude exceeds UInt16.MaxValue
                        }
                    }
                    else
                    {
                        // FIXME: Undefined when terrain altitude is negative (below water)
                    }
                    break;
                case 0x000a: // TODO: Test
                    _dcpu16.A = FixedPoint.Convert(vessel.staticPressure);
                    break;
            }

            return 0; // FIXME: unspecified
        }

        #endregion
    }
}
