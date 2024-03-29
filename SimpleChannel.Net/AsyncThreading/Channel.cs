using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleChannel.Net.AsyncThreading
{
    // A channel is more or less a thread safe data queue
    public class Channel<T> : IChannel<T>
    {
        private SemaphoreSlim takePerm;
        private Queue<T> queue = new Queue<T>();
        private SpinLock _spinner = new SpinLock();
        private bool _producing = true;

        public Channel()
        {
            takePerm = new SemaphoreSlim(0);
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
                    bool lockTaken = false;
                    try
                    {
                        _spinner.Enter(ref lockTaken);

                        Debug.Assert(queue.Count > 0);
                        val = queue.Dequeue();
                    }
                    finally
                    {
                        if (lockTaken) _spinner.Exit();
                    }
                }
                catch
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
            bool lockTaken = false;
            try
            {
                _spinner.Enter(ref lockTaken);

                queue.Enqueue(toPut);
            }
            finally
            {
                if (lockTaken) _spinner.Exit();
            }

            takePerm.Release();

            return true;
        }

        public virtual void Put(T toPut)
        {
            Offer(toPut, -1);
        }

        public void Dispose()
        {
            takePerm.Dispose();
        }

        /// <summary>
        /// Returns 1 if there is data queued, 0 otherwise
        /// </summary>
        public int Queued
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    _spinner.Enter(ref lockTaken);
                    return queue.Count;
                }
                finally
                {
                    if (lockTaken) _spinner.Exit();
                }
            }
        }
    }
}

