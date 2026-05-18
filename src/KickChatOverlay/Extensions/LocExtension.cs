using System.Windows.Data;
using System.Windows.Markup;
using KickChatOverlay.Services;

namespace KickChatOverlay.Extensions;

[MarkupExtensionReturnType(typeof(string))]
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        if (provideValueTarget?.TargetObject == null)
        {
            return $"[{Key}]";
        }

        var binding = new System.Windows.Data.Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            FallbackValue = $"[{Key}]"
        };

        return binding.ProvideValue(serviceProvider);
    }
}
