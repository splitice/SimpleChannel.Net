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
    public class AmqpBasicQueueChannel<T> : AmqpBasicAbstractChannel<T> where T : class
    {
        public AmqpBasicQueueChannel(IModel model, String queue): base(model, queue)
        {
        }

        protected override QueueingBasicConsumer MakeConsumer()
        {
            return MakeConsumer(Name);
        }

    }
}
