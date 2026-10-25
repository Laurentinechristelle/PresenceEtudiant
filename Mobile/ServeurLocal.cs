using System.Diagnostics;

namespace PresenceEtudiant.Mobile;

// Sur Windows : lance tout seul le serveur (dossier Api) si il n'est pas déjà allumé.
// Sur Android : ne fait rien (lance le serveur avec Lancer-Serveur.bat).
public static class ServeurLocal
{
    static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };
    static readonly SemaphoreSlim _verrou = new(1, 1);
    static Process? _processus;

    public static async Task<bool> EstPretAsync()
    {
        try
        {
            var r = await _http.GetAsync("http://localhost:5080/");
            return r.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> DemarrerAsync()
    {
#if WINDOWS
        await _verrou.WaitAsync();
        try
        {
            if (await EstPretAsync()) return true;

            var csproj = TrouverApi();
            if (csproj is null) return false;

            var info = new ProcessStartInfo("dotnet",
                $"run --no-launch-profile --project \"{csproj}\" --urls http://localhost:5080 -- --parent-pid {Environment.ProcessId}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(csproj)!
            };
            info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            _processus = Process.Start(info);
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try { _processus?.Kill(true); } catch (Exception) { }
            };

            // La première fois, il faut compiler le serveur : on patiente jusqu'à 3 minutes
            for (int i = 0; i < 180; i++)
            {
                if (await EstPretAsync()) return true;
                if (_processus is { HasExited: true }) return false;
                await Task.Delay(1000);
            }
            return false;
        }
        finally
        {
            _verrou.Release();
        }
#else
        await Task.CompletedTask;
        return true;
#endif
    }

    // Remonte les dossiers jusqu'à trouver Api\PresenceEtudiant.Api.csproj
    static string? TrouverApi()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null)
        {
            var candidat = Path.Combine(dossier.FullName, "Api", "PresenceEtudiant.Api.csproj");
            if (File.Exists(candidat)) return candidat;
            dossier = dossier.Parent;
        }
        return null;
    }
}
