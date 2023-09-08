using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using ServiceKeeper.Core.Entity;
using System.Text.Json;
using System.Text;
using ServiceKeeper.Core.EventBus.EventHandler;
using System.Threading.Channels;

namespace ServiceKeeper.Core.EventBus
{
    class RabbitMQEventBus : IEventBus, IDisposable
    {
        private readonly IModel _consumerChannel;
        private readonly string _exchangeName;
        private readonly RabbitMQConnection _persistentConnection;
        private readonly SubscriptionsManager _subsManager;
        private readonly IServiceProvider _serviceProvider;

        public RabbitMQEventBus(RabbitMQConnection persistentConnection, IServiceProvider serviceProvider, string exchangeName)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _subsManager = new SubscriptionsManager();
            _exchangeName = exchangeName;
            _serviceProvider = serviceProvider;
            _consumerChannel = CreateConsumerChannel();
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object? sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using var channel = _persistentConnection.CreateModel();
            channel.QueueUnbind(queue: eventName, exchange: _exchangeName, routingKey: eventName);

            if (_subsManager.IsEmpty)
            {
                _consumerChannel.Close();
            }
        }

        public void Publish(string eventName, TaskDetail task)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            //Connection 可以创建多个 Channel ，Channel 不是线程安全的不能在线程间共享。
            using var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");

            byte[] body;
            if (task.Task == null)
            {
                body = Array.Empty<byte>();
            }
            else
            {
                JsonSerializerOptions options = new()
                {
                    WriteIndented = true
                };
                body = JsonSerializer.SerializeToUtf8Bytes(task, task.GetType(), options);
            }
            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent
            channel.BasicPublish(exchange: _exchangeName, routingKey: eventName, mandatory: true, basicProperties: properties, body: body);
        }

        public void Reply(string eventName, MqReply reply)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");
            byte[] body;
            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };
            body = JsonSerializer.SerializeToUtf8Bytes(reply, reply.GetType(), options);

            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent
            channel.BasicPublish(exchange: _exchangeName, routingKey: eventName, mandatory: true, basicProperties: properties, body: body);
        }

        public void Subscribe(string eventName, Type handlerType)
        {
            CheckHandlerType(handlerType);
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription(eventName, handlerType);
            StartBasicConsume(eventName);
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                _consumerChannel.QueueDeclare(queue: eventName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _consumerChannel.QueueBind(queue: eventName, exchange: _exchangeName, routingKey: eventName);
            }
        }

        private static void CheckHandlerType(Type handlerType)
        {
            if (!typeof(IIntegrationEventHandler).IsAssignableFrom(handlerType))
            {
                throw new ArgumentException($"{handlerType} 不继承自 IIntegrationEventHandler", nameof(handlerType));
            }
        }

        public void Unsubscribe(string eventName, Type handlerType)
        {
            CheckHandlerType(handlerType);
            _subsManager.RemoveSubscription(eventName);
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();
            _subsManager.Clear();
            _persistentConnection.Dispose();
            //serviceScope.Dispose();
        }

        private void StartBasicConsume(string eventName)
        {
            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.Received += Consumer_Received;
                _consumerChannel.BasicConsume(queue: eventName, autoAck: false, consumer: consumer);
            }
        }


        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;//这个框架中，就是用eventName当RoutingKey
            var body = Encoding.UTF8.GetString(eventArgs.Body.Span);//框架要求所有的消息都是字符串的json
            Console.WriteLine($"{DateTime.Now:G} : 接收到任务!");

            try
            {
                if (_subsManager.HasSubscriptionForEvent(eventName))
                {
                    Type subscription = _subsManager.GetHandlerForEvent(eventName);
                    IIntegrationEventHandler handler = (_serviceProvider.GetServices<IIntegrationEventHandler>().FirstOrDefault(handler => handler.ToString() == subscription.ToString()) ?? (IIntegrationEventHandler?)_serviceProvider.GetService(subscription)) ?? throw new ApplicationException($"无法创建{subscription}类型的服务");
                    if (handler.GetEventName() == eventName)
                    {
                        await handler.Handle(eventName, body);
                        //如果在获取消息时采用不自动应答，但是获取消息后不调用basicAck，
                        //RabbitMQ会认为消息没有投递成功，不仅所有的消息都会保留到内存中，
                        //而且在客户重新连接后，会将消息重新投递一遍。这种情况无法完全避免，因此EventHandler的代码需要幂等
                        //multiple：批量确认标志。如果值为true，则执行批量确认，此deliveryTag之前收到的消息全部进行确认; 如果值为false，则只对当前收到的消息进行确认
                        _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    }
                }
                else throw new ApplicationException($"无法处理{eventName}类型的消息");
            }
            catch (Exception ex)
            {
                //requeue：表示如何处理这条消息，如果值为true，则重新放入RabbitMQ的发送队列，如果值为false，则通知RabbitMQ销毁这条消息
                _consumerChannel.BasicReject(eventArgs.DeliveryTag, false);
                Debug.Fail(ex.ToString());
            }

        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");


            channel.CallbackException += (sender, ea) =>
            {
                Debug.Fail(ea.ToString());
            };

            return channel;
        }


    }
}
