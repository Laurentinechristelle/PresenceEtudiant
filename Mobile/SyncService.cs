namespace PresenceEtudiant.Mobile;

public class SyncService(LocalDb db, ApiService api)
{
    readonly SemaphoreSlim _verrou = new(1, 1);   // évite deux synchronisations en même temps

    public async Task SynchroniserAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;   // hors connexion : on attend
        if (!await _verrou.WaitAsync(0)) return;
        try
        {
            var attente = await db.EnAttenteAsync();
            if (attente.Count == 0) return;

            var envoi = attente.Select(a => new AppelDto(a.Id, a.SeanceId, a.ClasseId, a.Date, a.Intitule, a.Valide, a.Presences)).ToList();
            var resultats = await api.EnvoyerAppelsAsync(envoi);

            foreach (var r in resultats)
            {
                var local = attente.First(a => a.Id == r.Id);
                // Quel que soit le statut, le serveur a répondu : on sort de la file d'attente.
                // "deja_verrouille" et "doublon" : le serveur garde SA version (message à afficher à l'utilisateur).
                local.EnAttenteSync = false;
                if (r.Statut is "valide" or "deja_verrouille") local.Verrouille = true;
                await db.SauverAppelAsync(local);
            }
        }
        catch (Exception) { /* serveur injoignable, délai dépassé, jeton expiré : on réessaiera plus tard */ }
        finally { _verrou.Release(); }
    }
}
