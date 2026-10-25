namespace PresenceEtudiant.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Écrans ouverts par-dessus les autres (avec bouton Retour)
        Routing.RegisterRoute("inscription", typeof(InscriptionPage));
        Routing.RegisterRoute("appel", typeof(AppelPage));
        Routing.RegisterRoute("historique", typeof(HistoriquePage));
        Routing.RegisterRoute("justification", typeof(JustificationPage));
        Routing.RegisterRoute("assiduite", typeof(AssiduitePage));
    }
}
