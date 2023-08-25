using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ServiceKeeper.Core.EventBus
{
    /// <summary>
    /// 提供了RabbitMQ EventBus的重连机制
    /// </summary>
    class RabbitMQConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private bool _disposed;
        private readonly static object sync_root = new();

        public RabbitMQConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool IsConnected
        {
            get { return _connection != null && _connection.IsOpen && !_disposed; }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("没有可用于执行此操作的 Rabbit MQ 连接");
            }

            return _connection!.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _connection?.Dispose();
        }

        public bool TryConnect()
        {
            lock (sync_root)
            {
                _connection = _connectionFactory.CreateConnection();

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// 异常断开尝试重连
        /// </summary>
        private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            TryConnect();
        }
        /// <summary>
        /// 回调异常尝试重连
        /// </summary>
        void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            TryConnect();
        }
        /// <summary>
        /// 非正常关闭尝试重连
        /// </summary>
        void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            TryConnect();
        }
    }
}
