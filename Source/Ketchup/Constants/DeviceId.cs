namespace Ketchup.Constants
{
    public enum DeviceId : uint
    {
        Firmware            = 0x37464B39, // TODO: the proposed specification doesn't specify this
        GenericClock        = 0x12d0b402,
        GenericKeyboard     = 0x30cf7406,
        Lem1802Monitor      = 0x7349f615,
        M35FdFloppyDrive    = 0x4fd524c5,
        Sped3               = 0x42babf3c,
    }
}
