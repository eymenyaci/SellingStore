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

        #region isEmpty and clear
        public bool IsEmpty => !_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();
        #endregion

        #region subscribe event dynamic
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
        #endregion

        #region subscribe event static
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
        #endregion

        #region dictionary delete key and value or key
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
        #endregion

        #region remove subs dynamic
        public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {

            var handlerToRemove = FindSubscriptionToRemove<T, TH>();
            var eventName = GetEventKey<T>();
            RemoveHandler(eventName, handlerToRemove);
        }
        #endregion

        #region get event key and return method dynamic
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return GetHandlersForEvent(key);
        }
        #endregion

        #region get event key and return method static
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];
        #endregion

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }

        #region get eventname and return static method
        private SubscriptionInfo FindSubscriptionToRemove<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            return FindSubscriptionToRemove(eventName, typeof(TH));
        }
        #endregion

        #region Find to remove event
        private SubscriptionInfo FindSubscriptionToRemove(String eventName, Type handlerType)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                return null;
            }

            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }
        #endregion

        #region has subs or unsubs return bool dynamic method
        public bool HasSubscriptionForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return HasSubscriptionForEvent(key);
        }
        #endregion

        #region has subs or unsubs return bool static method
        public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);
        #endregion

        #region get event name static
        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);
        #endregion

        #region get event name dynamic
        public string GetEventKey<T>()
        {
            string eventName = typeof(T).Name;
            return eventNameGetter(eventName);
        }
        #endregion




    }
}
