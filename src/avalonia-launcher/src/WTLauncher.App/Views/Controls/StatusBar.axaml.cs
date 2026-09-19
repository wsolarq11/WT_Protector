using Avalonia.Automation;
using Avalonia.Controls;
using WTLauncher.App.Localization;

namespace WTLauncher.App.Views.Controls;

public sealed partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
        var levelName = LauncherLocalization.Format("status.level");
        AutomationProperties.SetName(StatusNeutralIcon, levelName);
        AutomationProperties.SetName(StatusSuccessIcon, levelName);
        AutomationProperties.SetName(StatusWarningIcon, levelName);
        AutomationProperties.SetName(StatusErrorIcon, levelName);
        AutomationProperties.SetName(StatusTextBlock, LauncherLocalization.Format("status.message"));
    }
}
