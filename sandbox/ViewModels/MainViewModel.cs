using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sandbox.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    public partial int ClickCount { get; set; }

    [ObservableProperty]
    public partial LocalizedString Text { get; set; }

    public static IReadOnlyList<LocalizedString> Languages { get; } =
    [
        Resources.Chinese,
        Resources.English
    ];

    public MainViewModel() => Text = LocalizedString.FromText("Fixed Text");

    [RelayCommand]
    private void Click() => ClickCount++;

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
