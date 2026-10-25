namespace PresenceEtudiant.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var fenetre = new Window(new AppShell()) { Title = "Présence Étudiant" };
#if WINDOWS
        // Sur PC, la fenêtre a la forme d'un téléphone
        fenetre.Width = 430;
        fenetre.Height = 860;
        fenetre.X = 120;
        fenetre.Y = 30;
#endif
        return fenetre;
    }
}
