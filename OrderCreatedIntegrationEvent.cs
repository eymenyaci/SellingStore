using System;


public class OrderCreatedIntegrationEvent : IntegrationEvent
{
    public int Id { get; set; }

    public OrderCreatedIntegrationEvent(int id)
    {
        Id = id;
    }

}
