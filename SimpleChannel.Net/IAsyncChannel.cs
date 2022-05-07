using System;
using System.Threading.Tasks;

namespace SimpleChannel.Net
{
    public interface IAsyncChannel: IDisposable
    {
        /// <summary>
        /// Acknowledge the last consumed message
        /// </summary>
        void Ack();
    }

    /// <summary>
    /// Common interface for communication channels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncChannel<T> : IAsyncChannel
    {
        /// <summary>
        /// True if the producer side of the channel is open
        /// </summary>
        bool Producing { get; }

        /// <summary>
        /// Close the producer side of the channel
        /// </summary>
        void CloseProducer();

        /// <summary>
        /// Close the consumer side of the channel
        /// </summary>
        void CloseConsumer();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toPut"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        bool Offer(T toPut, int ms);

        /// <summary>
        /// Put message onto exchange channel
        /// </summary>
        /// <param name="item"></param>
        void Put(T item);

        /// <summary>
        /// Blocking consume the next message
        /// </summary>
        /// <param name="noAck"></param>
        /// <returns></returns>
        Task<T> Take();

        /// <summary>
        /// Number of queued messages
        /// </summary>
        int Queued { get; }
    }
}
