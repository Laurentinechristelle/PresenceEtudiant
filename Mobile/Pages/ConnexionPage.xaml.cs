namespace PresenceEtudiant.Mobile;

public partial class ConnexionPage : ContentPage
{
    private readonly ConnexionViewModel _vm;

    public ConnexionPage(ConnexionViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.VerifierJetonAsync();
    }
}
