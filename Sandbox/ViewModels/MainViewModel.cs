using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sandbox.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public static IReadOnlyList<ResourcesLiteral.LocalizedString> Languages =>
    [
        ResourcesLiteral.Chinese,
        ResourcesLiteral.English
    ];

    partial void OnSelectedIndexChanged(int value)
    {
        LocalizationManager.Culture = value switch
        {
            0 => CultureInfo.GetCultureInfo("zh"),
            1 => CultureInfo.GetCultureInfo("en"),
            _ => LocalizationManager.Culture
        };
    }
}