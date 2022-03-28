using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Event
{
    public abstract class BaseEventBus : IEventBus, IDisposable
    {
        public readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscriptionManager SubsManager;

        public EventBusConfig EventBusConfig { get; private set; }
        
        #region ctor and dependecy injection
        public BaseEventBus(EventBusConfig config, IServiceProvider serviceProvider)
        {
            EventBusConfig = config;
            ServiceProvider = serviceProvider;
            SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
        }
        #endregion

        #region trim eventName
        public virtual string ProcessEventName(string eventName)
        {
            if (EventBusConfig.DeleteEventPrefix)
                eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());

            if (EventBusConfig.DeleteEventSuffix)
                eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());

            return eventName;
        }
        #endregion

        #region merger eventName and appName
        public virtual string GetSubName(string eventName)
        {
            //bkz NatificationService.OrderCreated
            return $"{EventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }
        #endregion

        #region for override method
        public virtual void Dispose()
        {
            EventBusConfig = null;
            SubsManager.Clear();
        }
        #endregion

        #region get subs
        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = ProcessEventName(eventName);

            var processed = false;

            if (SubsManager.HasSubscriptionForEvent(eventName))
            {
                //subscriptionları getir.
                var subscriptions = SubsManager.GetHandlersForEvent(eventName);


                using (var scope = ServiceProvider.CreateScope())
                {
                    foreach (var subscription in subscriptions)
                    {
                        var handler = ServiceProvider.GetService(subscription.HandlerType);
                        if (handler == null) continue;

                        var eventType = SubsManager.GetEventTypeByName($"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }

                processed = true;
            }

            return processed;
        }
        #endregion

        public abstract void Publish(IntegrationEvent @event);

        public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

        public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    }
}
