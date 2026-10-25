using System.Text.Json;

namespace PresenceEtudiant.Mobile;

// Ce qui est gardé DANS le téléphone (fichiers JSON) : c'est ce qui permet le hors connexion
public class AppelLocal
{
    public Guid Id { get; set; }
    public Guid SeanceId { get; set; }
    public int ClasseId { get; set; }
    public DateTime Date { get; set; }
    public string Intitule { get; set; } = "";
    public bool Valide { get; set; }
    public bool Verrouille { get; set; }        // verrou local, actif dès "Valider l'appel"
    public bool EnAttenteSync { get; set; }     // file d'attente d'envoi
    public List<PresenceDto> Presences { get; set; } = new();
}

public class LocalDb
{
    private readonly string _fichierAppels = Path.Combine(FileSystem.AppDataDirectory, "appels_locaux.json");
    private readonly string _fichierClasses = Path.Combine(FileSystem.AppDataDirectory, "classes_locales.json");
    private readonly SemaphoreSlim _verrou = new(1, 1);
    private List<AppelLocal>? _appels;
    private List<ClasseDto>? _classes;

    private static async Task<T?> LireAsync<T>(string fichier)
    {
        try
        {
            if (!File.Exists(fichier)) return default;
            return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(fichier));
        }
        catch (Exception)
        {
            return default;
        }
    }

    private async Task<List<AppelLocal>> ListeAppelsAsync()
    {
        _appels ??= await LireAsync<List<AppelLocal>>(_fichierAppels) ?? new List<AppelLocal>();
        return _appels;
    }

    public async Task SauverClassesAsync(List<ClasseDto> classes)
    {
        await _verrou.WaitAsync();
        try
        {
            _classes = classes;
            await File.WriteAllTextAsync(_fichierClasses, JsonSerializer.Serialize(classes));
        }
        finally { _verrou.Release(); }
    }

    public async Task<List<ClasseDto>> ClassesAsync()
    {
        _classes ??= await LireAsync<List<ClasseDto>>(_fichierClasses) ?? new List<ClasseDto>();
        return _classes;
    }

    public async Task<AppelLocal?> AppelAsync(Guid id)
    {
        await _verrou.WaitAsync();
        try { return (await ListeAppelsAsync()).FirstOrDefault(a => a.Id == id); }
        finally { _verrou.Release(); }
    }

    public async Task SauverAppelAsync(AppelLocal appel)
    {
        await _verrou.WaitAsync();
        try
        {
            var liste = await ListeAppelsAsync();
            var i = liste.FindIndex(x => x.Id == appel.Id);
            if (i >= 0) liste[i] = appel; else liste.Add(appel);
            await File.WriteAllTextAsync(_fichierAppels, JsonSerializer.Serialize(liste));
        }
        finally { _verrou.Release(); }
    }

    public async Task<List<AppelLocal>> EnAttenteAsync()
    {
        await _verrou.WaitAsync();
        try { return (await ListeAppelsAsync()).Where(a => a.EnAttenteSync).ToList(); }
        finally { _verrou.Release(); }
    }
}
