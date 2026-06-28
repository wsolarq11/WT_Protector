using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LaunchPlanTests
{
    [Fact]
    public void ConstructorRejectsMissingRequiredFields()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new LaunchPlan(null!, "args", "C:\\work", "cmd", false));
        Assert.Equal("Executable", ex.ParamName);

        var ex2 = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan("cmd.exe", " ", "C:\\work", "cmd", false));
        Assert.Equal("Arguments", ex2.ParamName);

        var ex3 = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan("cmd.exe", "args", " ", "cmd", false));
        Assert.Equal("WorkingDirectory", ex3.ParamName);

        var ex4 = Assert.Throws<ArgumentException>(() =>
            new LaunchPlan("cmd.exe", "args", "C:\\work", " ", false));
        Assert.Equal("Command", ex4.ParamName);
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
        Assert.Equal("cmd.exe -d \"C:\\work\" pwsh -NoExit -Command \"echo started\"", plan.CommandLine);
    }
}
