namespace PresenceEtudiant.Mobile;

public partial class InscriptionPage : ContentPage
{
    private readonly InscriptionViewModel _vm;

    public InscriptionPage(InscriptionViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }
}
