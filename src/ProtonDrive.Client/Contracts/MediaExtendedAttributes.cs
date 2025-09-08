namespace ProtonDrive.Client.Contracts;

public sealed class MediaExtendedAttributes
{
    public int? Width { get; set; }

    public int? Height { get; set; }

    /// <summary>
    /// Represents the duration of a video or audio file in seconds.
    /// Although the value is a floating-point number, it should be treated as an integer:
    /// <list type="bullet">
    /// <item><description>When reading the duration, round it down to the nearest whole number using <c>Math.Floor</c>.</description></item>
    /// <item><description>When writing the duration, store only whole seconds (discard any fractional part).</description></item>
    /// </list>
    /// </summary>
    public double? Duration { get; set; }
}
