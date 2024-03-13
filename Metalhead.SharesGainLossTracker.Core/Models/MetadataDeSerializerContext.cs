using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AlphaVantageRoot))]
[JsonSerializable(typeof(AlphaVantageData))]
[JsonSerializable(typeof(AlphaVantageMetaData))]
[JsonSerializable(typeof(MarketstackRoot))]
[JsonSerializable(typeof(MarketstackData))]
public partial class MetadataDeSerializerContext : JsonSerializerContext
{
}
