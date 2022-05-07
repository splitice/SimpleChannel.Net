using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleChannel.Net.AsyncThreading
{
    public class SimpleValueChannel<T>: IAsyncChannel<T> where T: class
    {
        private T _value = null;
        private object _valueLock = new object();
        private SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private bool _producing = true;

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

        public async Task<T> Take()
        {
            await _semaphore.WaitAsync();
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

        public void CloseProducer()
        {
            _producing = false;
        }

        public bool Producing
        {
            get { return _producing; }
        }

        public void CloseConsumer()
        {
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        /// <summary>
        /// Returns 1 if there is data queued, 0 otherwise
        /// </summary>
        public int Queued
        {
            get { lock (_valueLock) return _semaphore.CurrentCount != 0 ? 1 : 0; }
        }
    }
}
