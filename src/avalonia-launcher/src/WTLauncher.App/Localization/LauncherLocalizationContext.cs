using System.Text.Json.Serialization;

namespace WTLauncher.App.Localization;

[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class LauncherLocalizationContext : JsonSerializerContext;
