using System.Threading;

namespace SimpleChannel.Net.Threading
{

    public class BoundedChannel<T> : Channel<T>
    {
        private SemaphoreSlim putPerm;
        public BoundedChannel(int n)
        {
            putPerm = new SemaphoreSlim(n);
        }

        public override bool Offer(T toPut, int ms)
        {
            bool canPut;
            try
            {
                canPut = putPerm.Wait(ms);
            }
            catch
            {
                putPerm.Release();
                throw;
            }
            if (!canPut)
            {
                return false;
            }
            else
            {
                return base.Offer(toPut, ms);
            }
        }
        public override bool Poll(out T val, int ms)
        {
            if (base.Poll(out val, ms))
            {
                putPerm.Release();
                return true;
            }
            return false;
        }
    }

}
