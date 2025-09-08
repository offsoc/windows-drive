using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed record ExtendedAttributes(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    CommonExtendedAttributes? Common,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    GeoLocationExtendedAttributes? Location = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    CameraExtendedAttributes? Camera = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    MediaExtendedAttributes? Media = null);
