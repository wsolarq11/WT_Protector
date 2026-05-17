namespace WTLauncher.Core;

public sealed class LaunchPlanException : Exception
{
    public LaunchPlanException(string message)
        : base(message)
    {
    }
}
