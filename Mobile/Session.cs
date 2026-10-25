namespace PresenceEtudiant.Mobile;

// Petite mémoire partagée entre les écrans (classe choisie, appel en cours...)
public class Session
{
    public ClasseDto? Classe { get; set; }
    public Guid AppelId { get; set; }          // appel en cours (écran Appel)
    public Guid JustifAppelId { get; set; }    // séance choisie dans l'historique (écran Justification)
}
