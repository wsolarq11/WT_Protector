using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LaunchPlanTests
{
    [Fact]
    public void ConstructorRejectsMissingRequiredFields()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan(null!, "args", "C:\\work", "cmd", false));
        Assert.Equal("Executable", exception.ParamName);

        exception = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan("cmd.exe", " ", "C:\\work", "cmd", false));
        Assert.Equal("Arguments", exception.ParamName);

        exception = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan("cmd.exe", "args", " ", "cmd", false));
        Assert.Equal("WorkingDirectory", exception.ParamName);

        exception = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan("cmd.exe", "args", "C:\\work", " ", false));
        Assert.Equal("Command", exception.ParamName);
    }

    [Fact]
    public void ConstructorTrimsRequiredFields()
    {
        var plan = new LaunchPlan(" cmd.exe ", " /c echo started ", " C:\\work ", " echo started ", true);

        Assert.Equal("cmd.exe", plan.Executable);
        Assert.Equal("/c echo started", plan.Arguments);
        Assert.Equal("C:\\work", plan.WorkingDirectory);
        Assert.Equal("echo started", plan.Command);
        Assert.True(plan.RequiresConfirmation);
        Assert.Equal("cmd.exe /c echo started", plan.CommandLine);
    }
}
