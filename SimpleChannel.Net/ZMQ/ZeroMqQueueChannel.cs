using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Core;
using NetMQ.Sockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;
using SimpleChannel.Net.Common;
using SimpleChannel.Net.Serialization;

namespace SimpleChannel.Net.ZMQ
{
    public class ZeroMqQueueChannel<T> : ZeroMqAbstractChannel, IRemoteChannel<T> where T : class
    {
        protected readonly String Name;
        private PublisherSocket _publisherSocket;
        private SubscriberSocket _subscriberSocket;
        private string _connectionString;
        private bool _producing = true;
        private IChannelSerializer _serializer;
        private bool _bind;

        public SocketOptions SendHighWatermark
        {
            get { return _publisherSocket.Options; }
        }

        public ZeroMqQueueChannel(String name, String connectionString, bool bind)
        {
            Name = name;
            _serializer = new ChannelDatacontractSerializer(new[] { typeof(T), typeof(RemoteCloseProducer) });
            _connectionString = connectionString;
            _bind = bind;
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
            PublisherInit();
            bool pollWrite = _publisherSocket.Poll(PollEvents.PollOut, TimeSpan.FromMilliseconds(ms)) == PollEvents.PollOut;
            if (!pollWrite)
            {
                return false;
            }

            try
            {
                bool ret = _publisherSocket.SendMoreFrame(Name)
                    .TrySendFrame(TimeSpan.FromMilliseconds(ms), Serializer.Serialize(toPut));
                return ret;
            }
            catch (NetMQ.TerminatingException)
            {
                return false;
            }
        }

        private static String AddressAny(string fullZmqAddr)
        {
            string address;
            int length = fullZmqAddr.IndexOf("://", StringComparison.Ordinal);
            address = fullZmqAddr.Substring(length + "://".Length);
            length = address.IndexOf(":", StringComparison.Ordinal);
            if (length != -1)
            {
                address = address.Substring(0, length);
            }
            IPAddress ip;
            try
            {
                ip = IPAddress.Parse(address);
            }
            catch (FormatException ex)
            {
                throw new FormatException(String.Format("Unable to parse IP address: {0}", address));
            }
            if (Equals(ip, IPAddress.Any) || Equals(ip, IPAddress.IPv6Any))
            {
                ip = IPAddress.Loopback;
                return fullZmqAddr.Substring(0, length) + "://" + ip;
            }
            return fullZmqAddr;
        }

        private void PublisherInit()
        {
            if (_publisherSocket != null) return;

            String connstr = _connectionString;
            if (!_bind || _subscriberSocket != null)
            {
                connstr = ">" + AddressAny(connstr);
            }
            _publisherSocket = new PublisherSocket(connstr);
            _publisherSocket.Options.SendHighWatermark = 1;
        }

        private void SubscriberInit()
        {
            if (_subscriberSocket != null) return;
            _subscriberSocket = new SubscriberSocket();
            var connstr = _connectionString;
            if (!_bind || _publisherSocket != null)
            {
                connstr = ">" + AddressAny(connstr);
            }
            _subscriberSocket.Bind(connstr);
            _subscriberSocket.Subscribe(Name);
        }


        private void InternalPut(object item, String routingKey)
        {
            try
            {
                _publisherSocket.SendMoreFrame(Name).SendFrame(Serializer.Serialize(item));
            }
            catch (NetMQ.TerminatingException)
            {
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
            SubscriberInit();

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
                temp = Serializer.Deserialize(result);
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
                if (_publisherSocket != null)
                {
                    return _publisherSocket.HasOut ? 1 : 0;
                }
                return 0;
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

        public IChannelSerializer Serializer
        {
            get { return _serializer; }
            set { _serializer = value; }
        }
    }
}
