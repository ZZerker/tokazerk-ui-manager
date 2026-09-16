using CommunityToolkit.Mvvm.ComponentModel;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.App.ViewModels;

public sealed partial class FontTierRowViewModel(FontTier tier, string label, Action changed) : ObservableObject
{
    public FontTier Tier => tier;

    public string Label => label;

    public int Minimum => FontTierInfo.MIN_PX;

    public int Maximum => FontTierInfo.MAX_PX;

    [ObservableProperty]
    private decimal? value;

    partial void OnValueChanged(decimal? oldValue, decimal? newValue)
    {
        if (newValue is null)
        {
            // NumericUpDown hands over null for an emptied box; keep the last real size instead of silently using the default.
            this.Value = oldValue ?? FontTierInfo.DefaultPx(tier);
            return;
        }

        changed();
    }

    [ObservableProperty]
    private string? warning;

    [ObservableProperty]
    private bool isError;

    public int Px => (int)(this.Value ?? FontTierInfo.DefaultPx(tier));

    public void SetPx(int px) => this.Value = px;
}
