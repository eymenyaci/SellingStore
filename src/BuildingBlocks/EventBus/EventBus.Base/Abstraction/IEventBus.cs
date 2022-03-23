using EventBus.Base.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstraction
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);

        void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        void RemoveSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        bool HasSubscriptionForEvent<T>() where T : IntegrationEvent;
        bool HasSubscriptionForEvent(string eventName);
        Type GetEventTypeByName(string eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(String eventName);
        string GetEventKey<T>();
        

    }
}
