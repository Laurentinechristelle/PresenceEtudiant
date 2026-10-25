namespace PresenceEtudiant.Mobile;

public partial class AssiduitePage : ContentPage
{
    private readonly AssiduiteViewModel _vm;

    public AssiduitePage(AssiduiteViewModel vm)
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
