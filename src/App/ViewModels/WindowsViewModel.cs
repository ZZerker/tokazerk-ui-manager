using CommunityToolkit.Mvvm.ComponentModel;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.App.ViewModels;

public sealed partial class WindowsViewModel : SectionViewModel
{
    private bool isLoadingSelection;

    public WindowsViewModel()
    {
        this.IsEnabled = false;
        this.TargetWindowChoices = CreateChoices(VariantKind.TargetWindow, this.OnTargetWindowSelected);
        this.FloatTargetChoices = CreateChoices(VariantKind.FloatTarget, this.OnFloatTargetSelected);
    }

    public override string Title => "Windows";

    public IReadOnlyList<VariantChoiceRowViewModel> TargetWindowChoices { get; }

    public IReadOnlyList<VariantChoiceRowViewModel> FloatTargetChoices { get; }

    public event EventHandler? SelectionChanged;

    [ObservableProperty]
    private string selectedTargetWindowId = VariantChoice.DEFAULT_ID;

    [ObservableProperty]
    private string selectedFloatTargetId = VariantChoice.DEFAULT_ID;

    [ObservableProperty]
    private string? error;

    public void Load(CurrentState state)
    {
        this.SelectChoice(this.TargetWindowChoices, state.Settings.Variants.Get(VariantKind.TargetWindow));
        this.SelectChoice(this.FloatTargetChoices, state.Settings.Variants.Get(VariantKind.FloatTarget));
        this.Error = state.SettingsError;
    }

    private static IReadOnlyList<VariantChoiceRowViewModel> CreateChoices(
        VariantKind kind,
        Action<VariantChoiceRowViewModel> selectionChanged) =>
        VariantTable.Get(kind).Choices
            .Select(choice => new VariantChoiceRowViewModel(choice.Id, choice.Label, selectionChanged))
            .ToArray();

    private void OnTargetWindowSelected(VariantChoiceRowViewModel selected)
    {
        var changed = this.SelectedTargetWindowId != selected.Id;
        this.SelectedTargetWindowId = selected.Id;
        SetExclusiveSelection(this.TargetWindowChoices, selected);

        if (changed && !this.isLoadingSelection)
        {
            this.SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnFloatTargetSelected(VariantChoiceRowViewModel selected)
    {
        var changed = this.SelectedFloatTargetId != selected.Id;
        this.SelectedFloatTargetId = selected.Id;
        SetExclusiveSelection(this.FloatTargetChoices, selected);

        if (changed && !this.isLoadingSelection)
        {
            this.SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SelectChoice(IReadOnlyList<VariantChoiceRowViewModel> choices, string choiceId)
    {
        var selected = choices.FirstOrDefault(choice => choice.Id == choiceId)
            ?? choices.Single(choice => choice.Id == VariantChoice.DEFAULT_ID);

        this.isLoadingSelection = true;
        try
        {
            selected.IsSelected = true;
        }
        finally
        {
            this.isLoadingSelection = false;
        }
    }

    private static void SetExclusiveSelection(
        IReadOnlyList<VariantChoiceRowViewModel> choices,
        VariantChoiceRowViewModel selected)
    {
        foreach (var choice in choices)
        {
            choice.IsSelected = ReferenceEquals(choice, selected);
        }
    }
}
