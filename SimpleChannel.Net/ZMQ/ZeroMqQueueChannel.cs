using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;
using SimpleChannel.Net.Common;

namespace SimpleChannel.Net.ZMQ
{
    public class ZeroMqQueueChannel<T> : ZeroMqAbstractChannel, IChannel<T> where T : class
    {
        protected readonly String Name;
        private readonly DataContractSerializer _ser;
        private PublisherSocket _publisherSocket;
        private SubscriberSocket _subscriberSocket;
        private string _connectionString;
        private bool _producing = true;

        public SocketOptions SendHighWatermark
        {
            get { return _publisherSocket.Options; }
        }

        public ZeroMqQueueChannel(String name, String connectionString, bool bind)
        {
            Name = name;
            _ser = new DataContractSerializer(typeof(object), new[] { typeof(T), typeof(RemoteCloseProducer) });
            String pubStr = connectionString;
            if (!bind)
            {
                pubStr = ">" + connectionString;
            }
            _publisherSocket = new PublisherSocket(pubStr);
            _connectionString = connectionString;
        }

        public bool Producing
        {
            get { return _producing; }
        }

        public void CloseProducer()
        {
            InternalPut(new RemoteCloseProducer(), null);
        }

        public void CloseConsumer()
        {
        }

        public bool Offer(T toPut, int ms)
        {
            bool pollWrite = _publisherSocket.Poll(PollEvents.PollOut, TimeSpan.FromMilliseconds(ms)) == PollEvents.PollOut;
            if (!pollWrite)
            {
                return false;
            }

            using (var stream = GetStream(toPut))
            {
                try
                {
                    return _publisherSocket.SendMoreFrame(Name).TrySendFrame(TimeSpan.FromMilliseconds(ms), stream.ToArray());
                }
                catch (NetMQ.TerminatingException)
                {
                    return false;
                }
            }
        }

        private MemoryStream GetStream(object item)
        {
            MemoryStream stream = new MemoryStream();
            _ser.WriteObject(stream, item);
            return stream;
        }

        private void InternalPut(object item, String routingKey)
        {
            using (var stream = GetStream(item))
            {
                try
                {
                    _publisherSocket.SendMoreFrame(Name).SendFrame(stream.ToArray());
                }
                catch (NetMQ.TerminatingException)
                {

                }
            }
        }

        public void Put(T item, String routingKey)
        {
            InternalPut(item, routingKey);
        }

        /// <summary>
        /// Put message onto exchange channel
        /// </summary>
        /// <param name="item"></param>
        public void Put(T item)
        {
            Put(item, Name);
        }

        /// <summary>
        /// Poll for value given timeout
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Poll(out T val, int timeout, bool noAck)
        {
            if (_subscriberSocket == null)
            {
                _subscriberSocket = new SubscriberSocket(">"+_connectionString);
                _subscriberSocket.Subscribe(Name);
            }

            //Get the message
            if (timeout >= 0)
            {
                var poll = _subscriberSocket.Poll(PollEvents.PollIn, TimeSpan.FromMilliseconds(timeout));
                if ((poll & PollEvents.PollIn) != PollEvents.PollIn)
                {
                    val = default(T);
                    return false;
                }
            }

            string messageTopicReceived = _subscriberSocket.ReceiveFrameString();
            var result = _subscriberSocket.ReceiveFrameBytes();
            if (result == null)
            {
                val = default(T);
                return false;
            }

            //Deserialize
            object temp;
            try
            {
                var stream = new MemoryStream(result);
                temp = _ser.ReadObject(stream);
            }
            catch (Exception)
            {
                val = default(T);
                return false;
            }

            if (temp is RemoteCloseProducer)
            {
                _producing = false;
                val = default(T);
                return false;
            }

            val = temp as T;

            return true;
        }

        /// <summary>
        /// Poll for value given timeout
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Poll(out T val, int timeout)
        {
            return Poll(out val, timeout, false);
        }

        /// <summary>
        /// Blocking consume the next message
        /// </summary>
        /// <param name="noAck"></param>
        /// <returns></returns>
        public T Take(bool noAck)
        {
            T val;
            Poll(out val, -1, noAck);
            return val;
        }

        /// <summary>
        /// Blocking consume the next message
        /// </summary>
        /// <returns></returns>
        public T Take()
        {
            return Take(false);
        }

        /// <summary>
        /// Returns 1 if there is data queued, 0 otherwise
        /// </summary>
        public int Queued
        {
            get
            {
                if (_subscriberSocket != null)
                {
                    return _subscriberSocket.HasIn ? 1 : 0;
                }
                return _publisherSocket.HasOut ? 1 : 0;
            }
        }

        /// <summary>
        /// Acknowledge the last consumed message
        /// </summary>
        public void Ack()
        {
        }

        public void Dispose()
        {
            if (_publisherSocket != null)
            {
                _publisherSocket.Dispose();
            }
            if (_subscriberSocket != null)
            {
                _subscriberSocket.Dispose();
            }
        }
    }
}
