namespace Ketchup.Constants
{
    internal enum DeviceId : uint
    {
        Firmware            = 0x37464B39, // TODO: the proposed specification doesn't specify this
        GenericClock        = 0x12D0B402,
        GenericKeyboard     = 0x30CF7406,
        Lem1802Monitor      = 0x7349F615,
        M35FdFloppyDrive    = 0x4Fd524C5,
        Sped3               = 0x42BAbf3C,
        Crash               = 0xCAE10001,
        Stop                = 0xCAE10002,
    }
}
