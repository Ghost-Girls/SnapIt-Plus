using System.Windows;

namespace SnapIt.Common.Entities;

public class Constants
{
    public const string AppStoreId = "9PHGBMZ7RBZX";
    public const string AppLogo = "/SnapIt.UI;component/Themes/snapit.png";
    public static string AppName => Application.ResourceAssembly.GetName().Name;
    public static string AppTitle => $"{AppName} - Window Manager";
    public static string AppVersion => $"version {Application.ResourceAssembly.GetName().Version}";
    public static readonly string RootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
    public const string AppFeedbackUrl = "github.com/Ghost-Girls/SnapIt-Plus/issues";
    public const string AppPurchaseUrl = "getsnapit.com/checkout";
    public const string AppVersionCheckUrl = "raw.githubusercontent.com/enginkirmaci/SnapIt/main/latest-version.json";
    public const string AppNewVersionUrl = "github.com/Ghost-Girls/SnapIt-Plus/releases/download/{0}/setup_SnapIt_{0}.exe";
    public const string AppPrivacyUrl = "github.com/Ghost-Girls/SnapIt-Plus/blob/main/PrivacyPolicy.md";
    public const string AppRegistryKey = "SnapIt";
    public const string GithubUrl = "github.com/Ghost-Girls/SnapIt-Plus";
    public const string MainRegion = "MainRegion";
    //public const string AppUrl = "getsnapit.com";
}