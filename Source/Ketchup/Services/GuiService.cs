namespace Ketchup.Services
{
    internal class GuiService : IGuiService
    {
        private const int BaseWindowId = 1404013894;

        private int _nextWindowId = BaseWindowId;

        public int GetNewWindowId()
        {
            return _nextWindowId++;
        }
    }
}
