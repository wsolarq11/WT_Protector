using WTLauncher.App.Features;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class PresetServiceTests
{
    [Fact]
    public async Task RequestNewPresetAsync_NormalizesAcceptedName()
    {
        var dialogs = new StubDialogService
        {
            OnRequestNewPreset = (_, _) => Task.FromResult<string?>("  Daily Work  "),
        };
        var service = new PresetService(dialogs);

        var result = await service.RequestNewPresetAsync(
            new LauncherPresetSaveTarget(["one"]),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var success = Assert.IsType<ServiceSuccess<string>>(result);
        Assert.Equal("Daily Work", success.Value);
    }

    [Fact]
    public async Task RequestNewPresetAsync_WhenDialogFails_ReturnsFailureStatus()
    {
        var expected = new IOException("dialog failed");
        var dialogs = new StubDialogService
        {
            OnRequestNewPreset = (_, _) => Task.FromException<string?>(expected),
        };
        var service = new PresetService(dialogs);

        var result = await service.RequestNewPresetAsync(
            new LauncherPresetSaveTarget(["one"]),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var failure = Assert.IsType<ServiceFailure<string>>(result);
        Assert.Equal(LauncherPresetStatus.SaveDialogFailed(expected), failure.Status);
    }

    [Fact]
    public async Task RequestNewPresetAsync_WhenDialogCancels_ReturnsCanceledStatus()
    {
        var service = new PresetService(new StubDialogService());

        var result = await service.RequestNewPresetAsync(
            new LauncherPresetSaveTarget(["one"]),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var canceled = Assert.IsType<ServiceCanceled<string>>(result);
        Assert.Equal(LauncherPresetStatus.SaveCanceled, canceled.Status);
    }

    [Fact]
    public void AddPreset_ReturnsUpdatedFileWithoutMutatingInput()
    {
        var service = new PresetService(new StubDialogService());
        var original = new LauncherPresetFile();

        var result = service.AddPreset(original, "Daily", ["one"]);

        Assert.Empty(original.Presets);
        var success = Assert.IsType<ServiceSuccess<PresetMutation>>(result);
        Assert.Equal("Daily", success.Value.Preset.Name);
        Assert.Single(success.Value.PresetFile.Presets);
    }

    [Fact]
    public async Task ConfirmDeletePresetAsync_ReturnsApprovedTarget()
    {
        var target = new LauncherPresetDeleteTarget("daily", "Daily", ["one"]);
        var dialogs = new StubDialogService
        {
            OnConfirmDeletePreset = _ => Task.FromResult(true),
        };
        var service = new PresetService(dialogs);

        var result = await service.ConfirmDeletePresetAsync(target);

        Assert.Same(target, Assert.IsType<ServiceSuccess<LauncherPresetDeleteTarget>>(result).Value);
    }

    [Fact]
    public void DeletePreset_ReturnsFallbackInTypedPayload()
    {
        var service = new PresetService(new StubDialogService());
        var original = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                new LauncherPreset { Id = "other", Name = "Other", EntryIds = ["one"] },
            ],
            DefaultPresetId = "daily",
        };

        var result = service.DeletePreset(original, "daily");

        var success = Assert.IsType<ServiceSuccess<PresetDeleteMutation>>(result);
        Assert.Equal("daily", success.Value.Preset.Id);
        Assert.Equal("other", success.Value.FallbackPresetId);
        Assert.Single(success.Value.PresetFile.Presets);
    }

    [Fact]
    public void Eligibility_UsesPresetWorkflowRules()
    {
        var service = new PresetService(new StubDialogService());
        var presetFile = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = "Daily",
                    EntryIds = ["one"],
                },
                new LauncherPreset
                {
                    Id = "locked",
                    Name = "Locked",
                    EntryIds = ["one"],
                    Locked = true,
                },
            ],
            DefaultPresetId = "daily",
        };

        Assert.True(service.CanUpdatePreset(presetFile, "daily"));
        Assert.True(service.CanDeletePreset(presetFile, "daily"));
        Assert.False(service.CanUpdatePreset(presetFile, "locked"));
        Assert.False(service.CanDeletePreset(presetFile, "locked"));
    }

    [Fact]
    public void CountEntryReferences_NormalizesInputAndUsesEntryIdComparer()
    {
        var service = new PresetService(new StubDialogService());
        var presetFile = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset { Id = "one", Name = "One", EntryIds = ["Demo"] },
                new LauncherPreset { Id = "two", Name = "Two", EntryIds = ["Demo", "other"] },
            ],
            DefaultPresetId = "one",
        };

        Assert.Equal(2, service.CountEntryReferences(presetFile, " Demo "));
        Assert.Equal(0, service.CountEntryReferences(presetFile, " "));
    }

    [Fact]
    public void BuildSelectionPreview_DelegatesCompleteSelectionContract()
    {
        var service = new PresetService(new StubDialogService());

        var preview = service.BuildSelectionPreview(
            "Daily",
            ["one", "extra"],
            ["one", "two", "extra"],
            ["one", "two"]);

        Assert.Equal(1, preview.MatchedCount);
        Assert.Equal(1, preview.MissingCount);
        Assert.Equal(1, preview.ExtraCount);
        Assert.True(preview.HasChanges);
    }
}
