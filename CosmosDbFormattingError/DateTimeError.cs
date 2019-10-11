using Newtonsoft.Json;
using System;

namespace CosmosDbFormattingError
{
    public class DateTimeError
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
    }
}
