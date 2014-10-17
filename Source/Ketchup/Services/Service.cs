namespace Ketchup.Services
{
    internal static class Service
    {
        private static readonly object Lock = new object();

        private static IDebugService _debugService;
        private static IGuiService _guiService;

        public static IDebugService Debug
        {
            get
            {
                if (_debugService == null)
                {
                    lock (Lock)
                    {
                        if (_debugService == null)
                        {
                            _debugService = new DebugService();
                        }
                    }
                }

                return _debugService;
            }
        }

        public static IGuiService Gui
        {
            get
            {
                if (_guiService == null)
                {
                    lock (Lock)
                    {
                        if (_guiService == null)
                        {
                            _guiService = new GuiService();
                        }
                    }
                }

                return _guiService;
            }
        }
    }
}
