using System;
using System.Threading;

namespace SimpleChannel.Net.Threading
{
    public class NullChannel<T>: IChannel<T> where T: class
    {
        private bool _producing = true;

        public bool Offer(T toPut, int ms)
        {
            return true;
        }

        public void Put(T item)
        {
        }

        public bool Poll(out T val, int timeout)
        {
            val = null;
            return false;
        }

        public T Take()
        {
            throw new NotImplementedException("This method can not be implemented on a Null Channel");
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
            
        }
    }
}
