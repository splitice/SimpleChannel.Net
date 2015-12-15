using System.Collections.Generic;
using System.Threading;

namespace SimpleChannel.Net.Threading
{
    // A channel is more or less a thread safe data queue
    public class Channel<T> : IChannel<T>
    {
        private SemaphoreSlim takePerm;
        private Queue<T> queue = new Queue<T>();
        private object _lock = new object();

        public Channel()
        {
            takePerm = new SemaphoreSlim(0);
        }

        public virtual T Take()
        {
            T val = default(T);
            Poll(out val, -1);
            return val;
        }

        public void Ack()
        {
            //Do nothing
        }

        public virtual bool Poll(out T val, int ms)
        {
            if (takePerm.Wait(ms))
            {
                try
                {
                    lock (_lock)
                    {
                        val = queue.Dequeue();
                    }
                }
                catch (ThreadInterruptedException)
                {
                    takePerm.Release();
                    throw;
                }
                return true;
            }

            val = default(T);
            return false;
        }

        public virtual bool Offer(T toPut, int ms)
        {
            lock (_lock)
            {
                //Debug.WriteLine(Thread.CurrentThread.Name);
                queue.Enqueue(toPut);
            }
            takePerm.Release();
            return true;
        }

        public virtual void Put(T toPut)
        {
            Offer(toPut, -1);
        }
    }
}

