using System;
using RabbitMQ.Client;

namespace SimpleChannel.Net.RabbitMQ
{
    /// <summary>
    /// A channel like interface using AMQP / RabbitMQ.
    /// 
    /// Create a new instance per binding (input, output etc)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AmqpBasicExchangeChannel<T> : AmqpBasicAbstractChannel<T> where T : class
    {
        private string _queueName;

        public String QueueName
        {
            get
            {
                if (_queueName != null)
                {
                    return _queueName;
                }
                _queueName = Model.QueueDeclare().QueueName;
                return _queueName;
            }
        }

        public AmqpBasicExchangeChannel(IModel model, String exchangeName)
            : base(model, exchangeName, exchangeName)
        {
            try
            {
                Model.ExchangeDeclare(Name, "fanout");
            }
            catch
            {

            }
        }

        public void BindExchange(String routingKey)
        {
            Model.QueueBind(queue: QueueName, exchange: Name, routingKey: routingKey);
        }

        protected override QueueingBasicConsumer MakeConsumer()
        {
            return MakeConsumer(QueueName);
        }
    }
}
