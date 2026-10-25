namespace PresenceEtudiant.Mobile;

// À appeler depuis tes ViewModels (écrans 3 et 4)
public class AppelService(LocalDb db, SyncService sync)
{
    public async Task<AppelLocal> DemarrerAsync(ClasseDto classe, string intitule)
    {
        var a = new AppelLocal
        {
            Id = Guid.NewGuid(), SeanceId = Guid.NewGuid(),     // créés SUR le téléphone, sans serveur
            ClasseId = classe.Id, Date = DateTime.Now, Intitule = intitule,
            Presences = classe.Etudiants.Select(e => new PresenceDto(e.Id, true)).ToList()   // défaut : tous présents (à justifier dans ton document)
        };
        await db.SauverAppelAsync(a);
        return a;
    }

    // "Appel en un geste" : un appui sur la ligne bascule Présent / Absent
    public async Task BasculerAsync(Guid appelId, int etudiantId)
    {
        var a = await db.AppelAsync(appelId) ?? throw new InvalidOperationException("Appel introuvable");
        if (a.Verrouille) throw new InvalidOperationException("Appel verrouillé : modification impossible");
        a.Presences = a.Presences.Select(p => p.EtudiantId == etudiantId ? p with { Present = !p.Present } : p).ToList();
        a.EnAttenteSync = true;
        await db.SauverAppelAsync(a);
    }

    // "Valider l'appel" : verrouille tout de suite en local, envoie dès qu'il y a du réseau
    public async Task ValiderAsync(Guid appelId)
    {
        var a = await db.AppelAsync(appelId) ?? throw new InvalidOperationException("Appel introuvable");
        if (a.Verrouille) return;
        a.Valide = true; a.Verrouille = true; a.EnAttenteSync = true;
        await db.SauverAppelAsync(a);
        _ = sync.SynchroniserAsync();
    }
}
