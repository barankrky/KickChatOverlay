using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace KickChatOverlay.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture?.Name == value.Name) return;
            _currentCulture = value;
            Thread.CurrentThread.CurrentUICulture = value;
            CultureInfo.CurrentCulture = value;
            CultureInfo.CurrentUICulture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }

    public string CurrentLanguageCode => _currentCulture.TwoLetterISOLanguageName;

    private LocalizationService()
    {
        _resourceManager = new ResourceManager("KickChatOverlay.Resources.LocStrings", typeof(LocalizationService).Assembly);
        _currentCulture = CultureInfo.CurrentUICulture;
    }

    public string this[string key]
    {
        get
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? $"[{key}]";
        }
    }

    public string Format(string key, params object[] args)
    {
        var format = this[key];
        return string.Format(format, args);
    }

    public void SetCulture(string cultureCode)
    {
        CurrentCulture = new CultureInfo(cultureCode);
    }
}
