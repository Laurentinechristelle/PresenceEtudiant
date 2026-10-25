using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PresenceEtudiant.Mobile;

public class ApiService
{
    // 10.0.2.2 = le PC vu depuis l'émulateur Android
    static string Base => DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";
    readonly HttpClient _http = new() { BaseAddress = new Uri(Base), Timeout = TimeSpan.FromSeconds(10) };

    async Task AuthAsync()
    {
        var t = await Stockage.LireJetonAsync();
        _http.DefaultRequestHeaders.Authorization = t is null ? null : new AuthenticationHeaderValue("Bearer", t);
    }

    static void Verifier(HttpResponseMessage r)
    {
        if (r.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException();
        r.EnsureSuccessStatusCode();
    }

    async Task<T> LireAsync<T>(string url)
    {
        await AuthAsync();
        var r = await _http.GetAsync(url);
        Verifier(r);
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task<bool> ConnexionAsync(string email, string mdp)
    {
        var r = await _http.PostAsJsonAsync("/auth/connexion", new ConnexionDto(email, mdp));
        if (!r.IsSuccessStatusCode) return false;
        var j = await r.Content.ReadFromJsonAsync<JetonDto>();
        await Stockage.EcrireJetonAsync(j!.Token);
        return true;
    }

    public async Task<(bool Ok, string? Message)> InscriptionAsync(InscriptionDto d)
    {
        var r = await _http.PostAsJsonAsync("/auth/inscription", d);
        if (r.IsSuccessStatusCode)
        {
            var j = await r.Content.ReadFromJsonAsync<JetonDto>();
            await Stockage.EcrireJetonAsync(j!.Token);
            return (true, null);
        }
        var e = await r.Content.ReadFromJsonAsync<ErreurDto>();
        return (false, e?.Message ?? "Erreur d'inscription");
    }

    public Task<List<ClasseDto>> ClassesAsync() => LireAsync<List<ClasseDto>>("/api/classes");
    public Task<List<LigneAssiduite>> AssiduiteAsync(int classeId) => LireAsync<List<LigneAssiduite>>($"/api/classes/{classeId}/assiduite");
    public Task<List<SeanceResume>> SeancesAsync(int classeId) => LireAsync<List<SeanceResume>>($"/api/classes/{classeId}/seances");
    public Task<List<AbsenceDto>> AbsencesAsync(Guid appelId) => LireAsync<List<AbsenceDto>>($"/api/appels/{appelId}/absences");

    public async Task<byte[]> ExportAsync(int classeId)
    {
        await AuthAsync();
        var r = await _http.GetAsync($"/api/classes/{classeId}/export");
        Verifier(r);
        return await r.Content.ReadAsByteArrayAsync();
    }

    public async Task JustifierAsync(Guid appelId, int etudiantId, JustificationDto d)
    {
        await AuthAsync();
        var r = await _http.PutAsJsonAsync($"/api/appels/{appelId}/justifications/{etudiantId}", d);
        Verifier(r);
    }

    public async Task<List<ResultatSync>> EnvoyerAppelsAsync(List<AppelDto> appels)
    {
        await AuthAsync();
        var r = await _http.PostAsJsonAsync("/api/sync/appels", appels);
        Verifier(r);
        return await r.Content.ReadFromJsonAsync<List<ResultatSync>>() ?? new();
    }
}
