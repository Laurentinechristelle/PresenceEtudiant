namespace PresenceEtudiant.Mobile;

public partial class HistoriquePage : ContentPage
{
    private readonly HistoriqueViewModel _vm;

    public HistoriquePage(HistoriqueViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.ChargerAsync();
    }
}
