using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core;
using ServiceKeeper.Core.MQEventHandlers;
using StackExchange.Redis;
using System.Reflection;

namespace ServiceKeeper.Core.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册ServiceKeeperCore
        /// </summary>
        /// <param name="mainAssembly">可选:手动指定主程序集,指定的程序集应该是你的启动首选项程序集</param>
        public static IServiceCollection AddProducerServiceKeeper(this IServiceCollection services, Assembly? mainAssembly = null)
        {
            mainAssembly = mainAssembly ??= Assembly.GetCallingAssembly();

            AddServiceKeeperMetaData(services, ServiceRole.Producer, null);
            AddMediatR(services, mainAssembly);
            AddEventBus(services);
            AddServiceRegistry(services);
            services.AddSingleton<ProducerReceiveResponseEventHandler>();
            // Producer 特有的 注册任务 Scheduler
            services.AddSingleton<ServiceScheduler>(sp =>
            {
                var registry = sp.GetRequiredService<ServiceRegistry>();
                var eventBus = sp.GetRequiredService<IEventBus>();
                var mediator = sp.GetRequiredService<IMediator>();
                return new ServiceScheduler(mediator, eventBus, registry);
            });

            return services;
        }
        /// <summary>
        /// 注册ServiceKeeperCore
        /// </summary>
        /// <param name="receiveTaskType">指定此服务所接收的任务配置类型</param>
        /// <param name="mainAssembly">可选:手动指定主程序集,指定的程序集应该是你的启动首选项程序集</param>
        /// <returns></returns>
        public static IServiceCollection AddConsumerServiceKeeper(this IServiceCollection services, Type receiveTaskType, Assembly? mainAssembly = null)
        {
            mainAssembly ??= Assembly.GetCallingAssembly();

            AddServiceKeeperMetaData(services, ServiceRole.Consumer, receiveTaskType);
            AddMediatR(services, mainAssembly);
            AddServiceRegistry(services);
            AddEventBus(services);
            services.AddSingleton<ConsumerReceiveTaskEventHandler>();
            return services;
        }

        private static IServiceCollection AddServiceKeeperMetaData(this IServiceCollection services, ServiceRole role, Type? receiveTaskType)
        {
            if (role == ServiceRole.Consumer && receiveTaskType == null)
            {
                throw new ArgumentNullException(nameof(receiveTaskType), "使用 Consumer 模式必须要指定 receiveTaskType");
            }
            services.AddSingleton(sp =>
            {
                ServiceKeeperOptions optionService = sp.GetRequiredService<IOptions<ServiceKeeperOptions>>().Value;
                ServiceMetadata serviceMetadata = new(optionService.ServiceDescription, role, Assembly.GetEntryAssembly()!.GetName().Name!, receiveTaskType, optionService.RenewSeconds*3, optionService.RenewSeconds);
                return serviceMetadata;
            });
            return services;
        }

        /// <summary>
        /// MediatR注册所有程序集
        /// </summary>
        private static IServiceCollection AddMediatR(this IServiceCollection services, Assembly rootAssembly)
        {
            var referencedAssemblies = GetAllReferencedAssemblies(rootAssembly);
            var assembliesToScan = new List<Assembly> { rootAssembly };
            assembliesToScan.AddRange(referencedAssemblies);
            services.AddMediatR(config => { config.RegisterServicesFromAssemblies(assembliesToScan.ToArray()); });
            return services;
        }

        private static IServiceCollection AddServiceRegistry(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                //var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
                IConnectionMultiplexer redisConnection = sp.GetService<IConnectionMultiplexer>() ?? throw new Exception("IConnectionMultiplexer 获取失败,请先注册 IConnectionMultiplexer");
                ServiceMetadata serviceMetadata = sp.GetRequiredService<ServiceMetadata>();
                IMediator mediator = sp.GetService<IMediator>() ?? throw new Exception("IMediator 获取失败,请先注册 MediatR");
                ServiceKeeperOptions optionService = sp.GetRequiredService<IOptions<ServiceKeeperOptions>>().Value;
                return new ServiceRegistry(/*lifetime,*/ redisConnection, serviceMetadata, mediator, optionService.DatabaseNumber);
            });
            return services;
        }

        /// <summary>
        /// RabbitMq初始化
        /// </summary>
        private static IServiceCollection AddEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                var optionService = sp.GetRequiredService<IOptions<ServiceKeeperOptions>>().Value;
                var factory = new ConnectionFactory()
                {
                    HostName = optionService.MQHostName,
                    DispatchConsumersAsync = true,
                    UserName = optionService.MQUserName,
                    Password = optionService.MQPassword
                };
                RabbitMQConnection mqConnection = new(factory);
                return new RabbitMQEventBus(mqConnection, sp, optionService.MQExchangeName);
            });
            return services;
        }

        private static IEnumerable<Assembly> GetAllReferencedAssemblies(Assembly assembly)
        {
            var assemblies = new List<Assembly>();
            var referencedNames = assembly.GetReferencedAssemblies();

            foreach (var name in referencedNames)
            {
                try
                {
                    var referencedAssembly = Assembly.Load(name);
                    assemblies.Add(referencedAssembly);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading assembly '{name.FullName}': {ex.Message}");
                }
            }

            return assemblies;
        }
    }
}
