namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Represents a container that wraps an item along with its sequence index,
/// which can be used for maintaining an optional order.
/// </summary>
public record Envelope<T>(long Index, T Value);