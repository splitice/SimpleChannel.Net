using System;

namespace SimpleChannel.Net
{
    public interface IChannel: IDisposable
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
    public interface IChannel<T> : IChannel
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
        /// Attempt to retreive an item off channel within timeout ms
        /// </summary>
        /// <param name="val">value retreived</param>
        /// <param name="timeout">timeout in ms</param>
        /// <returns>success</returns>
        bool Poll(out T val, int timeout);

        /// <summary>
        /// Blocking consume the next message
        /// </summary>
        /// <param name="noAck"></param>
        /// <returns></returns>
        T Take();
    }
}
