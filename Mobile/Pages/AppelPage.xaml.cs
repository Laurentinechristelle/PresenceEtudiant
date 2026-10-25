namespace PresenceEtudiant.Mobile;

public partial class AppelPage : ContentPage
{
    private readonly AppelViewModel _vm;

    public AppelPage(AppelViewModel vm)
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
