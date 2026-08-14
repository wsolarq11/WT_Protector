using WTLauncher.Analyzers;

// ponytail: inline arrays in test assertions — perf-irrelevant in tests.
#pragma warning disable CA1861

using WTLauncher.Tests;
namespace WTLauncher.Tests.Analyzer;

/// <summary>Tests for <see cref="JsonRegistrationAnalyzer"/> (WT1006).</summary>
[Trait("Category", TestCategories.Analyzer)]
public sealed class JsonRegistrationAnalyzerTests
{
    private const string ShimLauncherJson = @"
// Shim attribute matching System.Text.Json.Serialization.JsonSerializableAttribute
// to avoid dependency on System.Text.Json assembly in the test compilation.
namespace System.Text.Json.Serialization
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class JsonSerializableAttribute : System.Attribute
    {
        public JsonSerializableAttribute(System.Type type) { }
    }

    internal abstract class JsonSerializerContext { }
}

namespace WTLauncher.Core
{
    [System.Text.Json.Serialization.JsonSerializable(typeof(RegisteredType))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(AnotherRegisteredType))]
    internal sealed partial class LauncherJsonContext : JsonSerializerContext
    {
        public static LauncherJsonContext Default { get; } = new();
    }

    public static class LauncherJson
    {
        public static void Save<T>(T value, string path, bool backup) where T : notnull { }
        public static T? Load<T>(string path) where T : notnull => default;
        public static T LoadRequired<T>(string path, string name) where T : notnull => default!;
    }

    internal interface ILauncherJsonIO
    {
        void Save<T>(T value, string path, bool backup) where T : notnull;
        T? Load<T>(string path) where T : notnull;
    }

    public class RegisteredType { }
    public class AnotherRegisteredType { }
    public class UnregisteredType { }
}
";

    [Fact]
    public void SaveWithUnregisteredType_ReportsDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        LauncherJson.Save(new UnregisteredType(), ""path"", false);
    }
}
";
        var diags = AnalyzerTestHelper.Run<JsonRegistrationAnalyzer>(
            source, assemblyName: "WTLauncher.App", ShimLauncherJson);
        var wt1006 = Assert.Single(diags, d => d.Id == JsonRegistrationAnalyzer.DiagnosticId);
        Assert.Contains("UnregisteredType", wt1006.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveWithRegisteredType_NoDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        LauncherJson.Save(new RegisteredType(), ""path"", false);
    }
}
";
        var diags = AnalyzerTestHelper.Run<JsonRegistrationAnalyzer>(
            source, assemblyName: "WTLauncher.App", ShimLauncherJson);
        Assert.DoesNotContain(diags,
            d => d.Id == JsonRegistrationAnalyzer.DiagnosticId);
    }

    [Fact]
    public void LoadWithRegisteredType_NoDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        var result = LauncherJson.Load<RegisteredType>(""path"");
    }
}
";
        var diags = AnalyzerTestHelper.Run<JsonRegistrationAnalyzer>(
            source, assemblyName: "WTLauncher.App", ShimLauncherJson);
        Assert.DoesNotContain(diags,
            d => d.Id == JsonRegistrationAnalyzer.DiagnosticId);
    }

    [Fact]
    public void LoadWithUnregisteredType_ReportsDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        var result = LauncherJson.Load<UnregisteredType>(""path"");
    }
}
";
        var diags = AnalyzerTestHelper.Run<JsonRegistrationAnalyzer>(
            source, assemblyName: "WTLauncher.App", ShimLauncherJson);
        var wt1006 = Assert.Single(diags, d => d.Id == JsonRegistrationAnalyzer.DiagnosticId);
        Assert.Contains("UnregisteredType", wt1006.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void NotAppCompilation_NoDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        LauncherJson.Save(new UnregisteredType(), ""path"", false);
    }
}
";
        var diags = AnalyzerTestHelper.Run<JsonRegistrationAnalyzer>(
            source, assemblyName: "WTLauncher.Core", ShimLauncherJson);
        Assert.DoesNotContain(diags,
            d => d.Id == JsonRegistrationAnalyzer.DiagnosticId);
    }
}
