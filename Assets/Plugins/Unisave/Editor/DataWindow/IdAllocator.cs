namespace Unisave.Editor.DataWindow
{
    public class IdAllocator
    {
        private int next = 1;

        public int NextId()
        {
            return next++;
        }
    }
}
