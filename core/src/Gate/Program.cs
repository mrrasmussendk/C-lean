using Gate.Cli;
using Gate.Contracts;
using Gate.Effects;

try
{
    return Commands.Run(args);
}
catch (GateFailure failure)
{
    Surroundings.PrintError($"gate: {failure.Message}");
    return C6Hardening.GateFailed;
}
catch (Exception unexpected) when (unexpected is IOException or UnauthorizedAccessException or InvalidOperationException or FormatException or KeyNotFoundException)
{
    Surroundings.PrintError($"gate: stopped by {unexpected.GetType().Name}: {unexpected.Message} This is a defect in the gate or in a file it was given; nothing was passed.");
    return C6Hardening.GateFailed;
}
