namespace PresenceEtudiant.Mobile;

// Garde le jeton de connexion. SecureStorage si possible, sinon Preferences (cas de Windows sans paquet).
public static class Stockage
{
    const string Cle = "jeton";

    public static async Task<string?> LireJetonAsync()
    {
        try
        {
            var v = await SecureStorage.Default.GetAsync(Cle);
            if (!string.IsNullOrEmpty(v)) return v;
        }
        catch (Exception) { }
        return Preferences.Default.ContainsKey(Cle) ? Preferences.Default.Get(Cle, "") : null;
    }

    public static async Task EcrireJetonAsync(string jeton)
    {
        try { await SecureStorage.Default.SetAsync(Cle, jeton); }
        catch (Exception) { Preferences.Default.Set(Cle, jeton); }
    }

    public static void EffacerJeton()
    {
        try { SecureStorage.Default.Remove(Cle); } catch (Exception) { }
        Preferences.Default.Remove(Cle);
    }
}
