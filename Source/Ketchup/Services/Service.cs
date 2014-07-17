namespace Ketchup.Services
{
    internal static class Service
    {
        private static readonly object Lock = new object();

        private static IDebugService _debugService;

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
    }
}
