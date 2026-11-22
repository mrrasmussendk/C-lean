namespace Gate.Effects;

/// <summary>The gate itself failed or is misconfigured. Becomes exit code 2, never a pass.</summary>
internal sealed class GateFailure(string message) : Exception(message);
