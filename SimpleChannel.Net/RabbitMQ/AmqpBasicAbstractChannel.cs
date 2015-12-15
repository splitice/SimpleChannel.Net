using System;
using System.IO;
using System.Runtime.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleChannel.Net.RabbitMQ
{
    public abstract class AmqpBasicAbstractChannel<T> : IChannel<T> where T : class
    {
        protected readonly IModel Model;
        protected readonly String Name;
        private readonly DataContractSerializer _ser;
        private ulong? _lastDelivery = null;
        private QueueingBasicConsumer _consumer;
        private string _exchange;

        protected AmqpBasicAbstractChannel(IModel model, String name, String exchange = "")
        {
            Model = model;
            Name = name;
            _ser = new DataContractSerializer(typeof(T));
            _consumer = null;
            _exchange = exchange;
        }

        protected QueueingBasicConsumer MakeConsumer(String queueName)
        {
            QueueingBasicConsumer consumer = new QueueingBasicConsumer(Model);
            Model.BasicConsume(queueName, false, consumer);
            return consumer;
        }

        protected abstract QueueingBasicConsumer MakeConsumer();

        private BasicDeliverEventArgs DequeueMessage(int timeoutMilseconds = 400)
        {
            if (_consumer == null)
            {
                _consumer = MakeConsumer();
            }
            BasicDeliverEventArgs result;
            _consumer.Queue.Dequeue(timeoutMilseconds, out result);
            return result;
        }


        public bool Offer(T toPut, int ms)
        {
            //TODO: just use timeout (but not block)?
            throw new NotImplementedException();
        }

        public void Put(T item, String routingKey)
        {
            MemoryStream stream = new MemoryStream();
            _ser.WriteObject(stream, item);
            Model.BasicPublish(_exchange, routingKey, null, stream.ToArray());
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
            //Have we acked the last consumed message?
            if (!noAck && _lastDelivery != null)
            {
                throw new Exception("Last message must be ACK'ed before another can be consumed");
            }

            //Get the message
            var result = DequeueMessage(timeout);
            if (result == null)
            {
                val = default(T);
                return false;
            }

            //Deserialize
            try
            {
                var stream = new MemoryStream(result.Body);
                val = _ser.ReadObject(stream) as T;
            }
            catch (Exception)
            {
                val = default(T);
                Model.BasicAck(result.DeliveryTag, false);
                return false;
            }
            _lastDelivery = result.DeliveryTag;

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
        /// Acknowledge the last consumed message
        /// </summary>
        public void Ack()
        {
            if (!_lastDelivery.HasValue)
            {
                throw new Exception("Nothing to ACK");
            }
            Model.BasicAck(_lastDelivery.Value, false);
            _lastDelivery = null;
        }
    }
}
