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
