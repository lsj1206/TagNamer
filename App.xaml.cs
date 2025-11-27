using System.Configuration;
using System.Data;
using System.Windows;
using ModernWpf;

namespace TagNamer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ModernWpf 테마 설정 (Light 모드)
        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
    }
}

