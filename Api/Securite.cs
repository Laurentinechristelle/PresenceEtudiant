using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace PresenceEtudiant.Api;

// Garde les données en mémoire et les enregistre dans un fichier JSON
public class Depot
{
    private readonly string _chemin;
    public readonly object Verrou = new();
    public Donnees D { get; private set; } = new();

    public Depot(string chemin)
    {
        _chemin = chemin;
        try
        {
            if (File.Exists(chemin))
                D = JsonSerializer.Deserialize<Donnees>(File.ReadAllText(chemin)) ?? new Donnees();
        }
        catch (Exception)
        {
            D = new Donnees();
        }
    }

    public void Sauver()
    {
        File.WriteAllText(_chemin, JsonSerializer.Serialize(D, new JsonSerializerOptions { WriteIndented = true }));
    }
}

// Mots de passe (PBKDF2) et jeton de connexion signé (HMAC) : tout est fourni par .NET
public static class Securite
{
    private static readonly byte[] Cle = Encoding.UTF8.GetBytes("cle-de-developpement-presence-etudiant-2026");

    public static (string Sel, string Hash) Hacher(string motDePasse)
    {
        var sel = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, 100_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(sel), Convert.ToBase64String(hash));
    }

    public static bool Verifier(string motDePasse, string sel, string hash)
    {
        var calcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, Convert.FromBase64String(sel), 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(calcule, Convert.FromBase64String(hash));
    }

    public static string Jeton(int enseignantId)
    {
        var charge = $"{enseignantId}.{DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()}";
        return charge + "." + Signer(charge);
    }

    private static string Signer(string charge) =>
        Convert.ToHexString(HMACSHA256.HashData(Cle, Encoding.UTF8.GetBytes(charge)));

    public static int? LireJeton(string? entete)
    {
        if (entete is null || !entete.StartsWith("Bearer ")) return null;
        var parties = entete.Substring(7).Split('.');
        if (parties.Length != 3) return null;
        var attendu = Signer($"{parties[0]}.{parties[1]}");
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(attendu), Encoding.UTF8.GetBytes(parties[2]))) return null;
        if (!long.TryParse(parties[1], out var expire) || expire < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;
        return int.TryParse(parties[0], out var id) ? id : null;
    }

    // Filtre appliqué à tout /api : sans jeton valide -> 401
    public static async ValueTask<object?> Authentifier(EndpointFilterInvocationContext contexte, EndpointFilterDelegate suite)
    {
        var uid = LireJeton(contexte.HttpContext.Request.Headers.Authorization.ToString());
        if (uid is null) return Results.Unauthorized();
        contexte.HttpContext.Items["uid"] = uid.Value;
        return await suite(contexte);
    }

    public static int Uid(HttpContext http) => (int)http.Items["uid"]!;
}

public static class Calculs
{
    // Seuls les appels VERROUILLÉS (validés) comptent dans le taux
    public static List<LigneAssiduite> Assiduite(Donnees d, int classeId, int uid, int seuil)
    {
        if (!d.Classes.Any(c => c.Id == classeId && c.EnseignantId == uid)) return new List<LigneAssiduite>();
        var verrouilles = d.Appels
            .Where(a => a.Verrouille && a.EnseignantId == uid && d.Seances.Any(s => s.Id == a.SeanceId && s.ClasseId == classeId))
            .ToList();

        return d.Etudiants.Where(e => e.ClasseId == classeId).OrderBy(e => e.Nom).Select(e =>
        {
            var lignes = verrouilles.Select(a => a.Presences.FirstOrDefault(p => p.EtudiantId == e.Id))
                                    .Where(p => p != null).Select(p => p!).ToList();
            int total = lignes.Count;
            int presents = lignes.Count(p => p.Present);
            int taux = total == 0 ? 100 : (int)Math.Round(100.0 * presents / total);
            return new LigneAssiduite(e.Id, e.Matricule, e.Nom, e.Prenom, total, presents, taux, total > 0 && taux < seuil);
        }).ToList();
    }

    public static byte[] Csv(List<LigneAssiduite> lignes)
    {
        var sb = new StringBuilder("Matricule;Nom;Prenom;Seances;Presents;Taux(%)\n");
        foreach (var x in lignes) sb.AppendLine($"{x.Matricule};{x.Nom};{x.Prenom};{x.Seances};{x.Presents};{x.Taux}");
        var enc = new UTF8Encoding(true);   // BOM pour qu'Excel lise les accents
        return enc.GetPreamble().Concat(enc.GetBytes(sb.ToString())).ToArray();
    }
}

public static class Seed
{
    // Données de démonstration (fictives) : une classe de l'ENSPD à Douala, avec 4 séances déjà validées
    public static void Remplir(Depot depot)
    {
        lock (depot.Verrou)
        {
            var d = depot.D;
            if (d.Enseignants.Count > 0) return;

            var prof = new Enseignant { Id = 1, Email = "prof@test.cm", Nom = "Tchinda", Prenom = "Olivier" };
            (prof.Sel, prof.Hash) = Securite.Hacher("Motdepasse1!");
            d.Enseignants.Add(prof);

            var classe = new Classe { Id = 1, Nom = "GIT 5 - Développement mobile (ENSPD Douala)", EnseignantId = prof.Id };
            d.Classes.Add(classe);

            (string Nom, string Prenom)[] noms =
            {
                ("Mbarga", "Jean-Pierre"), ("Nguemo", "Carine"), ("Tchoumi", "Brice"), ("Fotso", "Mireille"),
                ("Kamga", "Arnaud"), ("Ngono", "Estelle"), ("Essomba", "Patrick"), ("Njoya", "Aminatou"),
                ("Mvondo", "Serge"), ("Tagne", "Larissa"), ("Atangana", "Pascal"), ("Nkeng", "Blessing"),
                ("Abessolo", "Yannick"), ("Mohamadou", "Hadidja"), ("Etoundi", "Prisca")
            };
            for (int i = 0; i < noms.Length; i++)
                d.Etudiants.Add(new Etudiant { Id = i + 1, Matricule = $"22G{4001 + i}", Nom = noms[i].Nom, Prenom = noms[i].Prenom, ClasseId = classe.Id });

            var plan = new List<(string Intitule, int JoursAvant, int[] Absents, Dictionary<int, string> Motifs)>
            {
                ("Cours 1 - Introduction à .NET MAUI", 21, new[] { 2, 7, 11 },
                    new Dictionary<int, string> { [7] = "Paludisme (certificat médical)" }),
                ("TP 1 - Calculatrice MAUI", 14, new[] { 2, 4, 7 },
                    new Dictionary<int, string> { [2] = "Problème de transport (Douala - Yassa)" }),
                ("Cours 2 - MVVM et navigation", 7, new[] { 2, 7, 13 },
                    new Dictionary<int, string> { [13] = "Entretien de stage à Bonabéri" }),
                ("Atelier - Maquettes Figma", 3, new[] { 0, 9 },
                    new Dictionary<int, string> { [9] = "Décès dans la famille (Bafoussam)" })
            };
            foreach (var (intitule, jours, absents, motifs) in plan)
            {
                var seance = new Seance { Id = Guid.NewGuid(), ClasseId = classe.Id, Date = DateTime.Today.AddDays(-jours).AddHours(8), Intitule = intitule };
                var appel = new Appel { Id = Guid.NewGuid(), SeanceId = seance.Id, EnseignantId = prof.Id, Verrouille = true, ValideLe = seance.Date.AddMinutes(20).ToUniversalTime(), Version = 1 };
                for (int i = 0; i < noms.Length; i++)
                {
                    var absent = absents.Contains(i);
                    string? motif = null;
                    if (absent && motifs.TryGetValue(i, out var m)) motif = m;
                    appel.Presences.Add(new Presence { EtudiantId = i + 1, Present = !absent, Motif = motif });
                }
                d.Seances.Add(seance);
                d.Appels.Add(appel);
            }
            depot.Sauver();
        }
    }
}
