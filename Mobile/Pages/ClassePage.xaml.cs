namespace PresenceEtudiant.Mobile;

public partial class ClassePage : ContentPage
{
    private readonly ClasseViewModel _vm;

    public ClassePage(ClasseViewModel vm)
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
