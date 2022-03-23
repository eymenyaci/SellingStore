using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonConstructorAttribute = Newtonsoft.Json.JsonConstructorAttribute;

namespace EventBus.Base.Event
{
    public class IntegrationEvent
    {
        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public DateTime CreatedDate { get; private set; }

        //Dışarıdan parametreler gelmezse Otomatik uniq Id oluşur ve Event tarihini otomatik atar.
        [JsonConstructor]
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
        }

        //Dışarıdan Id ve CreatedDate parametresi gelirse burası çalışır.
        public IntegrationEvent(Guid id, DateTime createdDate)
        {
            Id = id;
            CreatedDate = createdDate;
        }
    }
}
