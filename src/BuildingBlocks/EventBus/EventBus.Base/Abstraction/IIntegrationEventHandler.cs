using EventBus.Base.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstraction
{
    public interface IIntegrationEventHandler<TIntegrationEvent> : IntegrationEventHandler where TIntegrationEvent : IntegrationEvent // TIntegrationEvent'in IntegrationEvent sınıfından türemiş olması zorunlu.
    {
        Task Handle(TIntegrationEvent @event);
    }

    public interface IntegrationEventHandler
    {

    }
}
