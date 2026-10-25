namespace PresenceEtudiant.Mobile;

public partial class JustificationPage : ContentPage
{
    private readonly JustificationViewModel _vm;

    public JustificationPage(JustificationViewModel vm)
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
