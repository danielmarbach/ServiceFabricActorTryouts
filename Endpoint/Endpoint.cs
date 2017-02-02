using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using OutboxActorService.Interfaces;
using OutboxMessage = NServiceBus.Outbox.OutboxMessage;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

namespace Endpoint
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Endpoint : StatelessService
    {
        public Endpoint(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            try
            {
                var configuration = new EndpointConfiguration("endpoint2");
                configuration.UseTransport<NServiceBus.AzureStorageQueueTransport>().ConnectionString("");

                configuration.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
                configuration.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
                configuration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();

                configuration.UsePersistence<ServiceFabricPersistence, StorageType.Sagas>();
                configuration.UsePersistence<ServiceFabricPersistence, StorageType.Outbox>();
                configuration.SendFailedMessagesTo("error");


                configuration.EnableFeature<MessageDrivenSubscriptions>();
                configuration.EnableOutbox();
                configuration.EnableInstallers();

                var endpoint = await NServiceBus.Endpoint.Start(configuration);

                await endpoint.SendLocal(new MyMessage());
            }
            catch (Exception ex)
            {
                
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    public class Handler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    public class MyMessage : ICommand
    {
    }

    public class ServiceFabricPersistence : PersistenceDefinition
    {
        internal ServiceFabricPersistence()
        {
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<ServiceFabricOutboxPersistence>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<ServiceFabricSagaPersistence>());
        }
    }

    internal class ServiceFabricOutboxPersistence : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ServiceFabricOutboxStorage>(DependencyLifecycle.SingleInstance);
        }
    }

    internal class ServiceFabricOutboxStorage : IOutboxStorage
    {
        private static Uri serviceUri = new Uri("fabric:/Application1/OutboxActorServiceActorService");

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var proxy = ActorProxy.Create<IOutboxActorService>(new ActorId(messageId), serviceUri);
            context.Set(proxy);
            var state = await proxy.GetMessage().ConfigureAwait(false);
            if (state.Empty)
            {
                return null;
            }

            return new OutboxMessage(messageId, state.TransportOperations.Select(s => new TransportOperation(s.MessageId, s.Options, s.Body, s.Headers)).ToArray());
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            StupidTransaction tx = (StupidTransaction) transaction;
            tx.Message = message;
            return Task.FromResult(0);
        }

        public Task SetAsDispatched(string messageId, ContextBag context)
        {
            return Task.FromResult(0);
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            return Task.FromResult<OutboxTransaction>(new StupidTransaction(context.Get<IOutboxActorService>()));
        }

        class StupidTransaction : OutboxTransaction
        {
            private IOutboxActorService actor;

            public StupidTransaction(IOutboxActorService actor)
            {
                this.actor = actor;
            }

            public OutboxMessage Message { get; set; }

            public void Dispose()
            {
                
            }

            public Task Commit()
            {
                return actor.Commit(new OutboxActorService.Interfaces.OutboxMessage(Message.TransportOperations.Select(s => new OutboxActorService.Interfaces.TransportOperation(s.MessageId, s.Options, s.Body, s.Headers)).ToArray()));
            }
        }
    }

    internal class ServiceFabricSagaPersistence : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ServiceFabricSagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }

    internal class ServiceFabricSagaPersister : ISagaPersister
    {
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session,
            ContextBag context)
        {
            throw new NotImplementedException();
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            throw new NotImplementedException();
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            throw new NotImplementedException();
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            throw new NotImplementedException();
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            throw new NotImplementedException();
        }
    }
}
