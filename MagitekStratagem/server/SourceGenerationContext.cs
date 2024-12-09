using System.Text.Json.Serialization;

namespace MagitekStratagemServer
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(TrackerServiceDto))]
    [JsonSerializable(typeof(TrackerServiceDto[]))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
