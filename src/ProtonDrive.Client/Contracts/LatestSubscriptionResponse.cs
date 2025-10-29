using System.Text.Json.Serialization;
using ProtonDrive.Shared.Text.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed record LatestSubscriptionResponse : ApiResponse
{
    [JsonPropertyName("LastSubscriptionEnd")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime? CancellationTimeUtc { get; init; }
}
