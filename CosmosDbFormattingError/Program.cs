using CosmosDbRepository;
using FluentAssertions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;
using Index = Microsoft.Azure.Documents.Index;

namespace CosmosDbFormattingError
{
    class Program
    {
        static Program()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK",
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

        }

        static async Task Main(string[] args)
        {
            // This is my simple testable wrapper around the 
            var client = new DocumentClient(new Uri("https://localhost:8081"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", JsonConvert.DefaultSettings());

            var cosmosDb = new CosmosDbBuilder()
                .WithId("upsertFailure")
                .AddCollection<DateTimeError>("datetimeerror", builder =>
                {
                    builder.IncludeIndexPath("/*", Index.Range(DataType.Number, -1), Index.Range(DataType.String, -1));
                })
                .Build(client);

            var repo = cosmosDb.Repository<DateTimeError>();

            var orig = new DateTimeError
            {
                Id = Guid.NewGuid(),
                Date = DateTimeOffset.UtcNow
            };

            // round the time to hundredths of a second to expose formatting problems
            orig.Date = new DateTimeOffset(orig.Date.Ticks - orig.Date.Ticks % (TimeSpan.TicksPerSecond / 100), TimeSpan.Zero);

            await repo.AddAsync(orig);
            var successfulFind = await repo.FindFirstOrDefaultAsync(dte => dte.Date == orig.Date);
            successfulFind.Should().BeEquivalentTo(orig);

            await repo.UpsertAsync(orig);
            var unsuccessfulUpsertFind = await repo.FindFirstOrDefaultAsync(dte => dte.Date == orig.Date);
            unsuccessfulUpsertFind.Should().BeNull();

            await repo.ReplaceAsync(orig);
            var unsuccessfulReplaceFind = await repo.FindFirstOrDefaultAsync(dte => dte.Date == orig.Date);
            unsuccessfulReplaceFind.Should().BeNull();

            var successfulGet = await repo.GetAsync(orig.Id);
            successfulGet.Should().BeEquivalentTo(orig);
        }
    }
}
