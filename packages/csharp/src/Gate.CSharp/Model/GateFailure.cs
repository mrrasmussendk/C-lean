using System;

namespace Gate.CSharp.Model;

/// <summary>A failure of the package itself. Its message is the complete line printed to standard error.</summary>
internal sealed class GateFailure : Exception
{
    public GateFailure(string message)
        : base(message)
    {
    }
}
