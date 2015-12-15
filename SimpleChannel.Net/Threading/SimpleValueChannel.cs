using System;
using System.Threading;

namespace SimpleChannel.Net.Threading
{
    public class SimpleValueChannel<T>: IChannel<T> where T: class
    {
        private T _value = null;
        private object _valueLock = new object();
        private SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        public bool Offer(T toPut, int ms)
        {
            Put(toPut);
            return true;
        }

        public void Put(T item)
        {
            lock (_valueLock)
            {
                if (_value != null)
                {
                    throw new Exception("Previous value not yet consumed");
                }
                _value = item;
            }
            _semaphore.Release();
        }

        public bool Poll(out T val, int timeout)
        {
            val = Take();
            return true;
        }

        public T Take()
        {
            _semaphore.Wait();
            T value;
            lock (_valueLock)
            {
                value = _value;
                _value = null;
            }
            return value;
        }

        public void Ack()
        {
            //Do nothing
        }
    }
}
