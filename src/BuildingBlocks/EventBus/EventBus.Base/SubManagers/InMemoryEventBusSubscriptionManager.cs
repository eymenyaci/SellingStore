using EventBus.Base.Abstraction;
using EventBus.Base.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.SubManagers
{
    public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;
        public event EventHandler<string> OnEventRemoved;
        public Func<string, string> eventNameGetter;

        #region ctor
        public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
            this.eventNameGetter = eventNameGetter;
        }
        #endregion

        #region IsEmpty And Clear
        public bool IsEmpty => !_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();
        #endregion

        public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            //Event Key alıyoruz yani event adını.
            var eventName = GetEventKey<T>();

            //Eventi abone ediyoruz.
            AddSubscription(typeof(TH), eventName);

            //Event tipi içeride yoksa yeni bir event event tipi olarak listeye ekliyoruz.
            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
        }

        private void AddSubscription(Type handlerType, string eventName)
        {
            //Gelen eventName değişkeninin aboneliği yoksa eventName için yeni bir liste oluşturuluyor.
            if (!HasSubscriptionForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }
            //_handlers eventName e göre HandlerType değişkenlerini karşılaştırıyor. Aynı ise exception fırlatıyor.
            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException($"Handler Type{handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }
            //eventName Listesinin altına event ekliyor.
            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        private void RemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {

            if (subsToRemove != null)
            {
                //SubscriptionInfo tipinde gelen değeri _handlers Dictionary sinden siliyoruz.
                _handlers[eventName].Remove(subsToRemove);

                //eventName _handlersta mevcut değilse 
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RaiseOnEventRemoved(eventName);
                }
            }
        }

        public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            
            var handlerToRemove = FindSubscriptionToRemove<T, TH>();
            var eventName = GetEventKey<T>();
            RemoveHandler(eventName, handlerToRemove);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return GetHandlersForEvent(key);
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }

        private SubscriptionInfo FindSubscriptionToRemove<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            return FindSubscriptionToRemove(eventName, typeof(TH));
        }
        private SubscriptionInfo FindSubscriptionToRemove(String eventName, Type handlerType)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                return null;
            }

            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }

        public bool HasSubscriptionForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return HasSubscriptionForEvent(key);
        }

        public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);

        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t=>t.Name == eventName);

        public string GetEventKey<T>()
        {
            string eventName = typeof(T).Name;
            return eventNameGetter(eventName);
        }



    }
}
