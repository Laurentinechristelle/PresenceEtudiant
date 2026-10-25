using System.Diagnostics;
using PresenceEtudiant.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var depot = new Depot(Path.Combine(app.Environment.ContentRootPath, "presence-donnees.json"));
Seed.Remplir(depot);   // compte et classe de démonstration (données fictives)

// Si l'application MAUI (Windows) m'a lancé, je m'arrête quand elle se ferme : --parent-pid <numéro>
var pidArg = args.SkipWhile(a => a != "--parent-pid").Skip(1).FirstOrDefault();
if (int.TryParse(pidArg, out var parentPid))
{
    _ = Task.Run(async () =>
    {
        try { await Process.GetProcessById(parentPid).WaitForExitAsync(); } catch (Exception) { }
        app.Lifetime.StopApplication();
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();   // sert wwwroot/index.html : la console de test de l'API

// ---------- Écrans 1 et 2 : Connexion / Inscription ----------
var auth = app.MapGroup("/auth");

auth.MapPost("/inscription", (InscriptionDto d) =>
{
    if (string.IsNullOrWhiteSpace(d.Nom)) return Results.BadRequest(new { champ = "nom", message = "Champ obligatoire" });
    if (string.IsNullOrWhiteSpace(d.Prenom)) return Results.BadRequest(new { champ = "prenom", message = "Champ obligatoire" });
    if (string.IsNullOrWhiteSpace(d.Email) || !d.Email.Contains('@')) return Results.BadRequest(new { champ = "email", message = "Adresse e-mail invalide" });
    if (string.IsNullOrEmpty(d.MotDePasse) || d.MotDePasse.Length < 8) return Results.BadRequest(new { champ = "motDePasse", message = "8 caractères minimum" });
    if (d.MotDePasse != d.Confirmation) return Results.BadRequest(new { champ = "confirmation", message = "Les mots de passe sont différents" });

    lock (depot.Verrou)
    {
        if (depot.D.Enseignants.Any(e => e.Email == d.Email))
            return Results.Conflict(new { champ = "email", message = "Un compte existe déjà avec cet e-mail" });

        var (sel, hash) = Securite.Hacher(d.MotDePasse);
        var nouveau = new Enseignant
        {
            Id = depot.D.Enseignants.Count == 0 ? 1 : depot.D.Enseignants.Max(e => e.Id) + 1,
            Nom = d.Nom, Prenom = d.Prenom, Email = d.Email, Sel = sel, Hash = hash
        };
        depot.D.Enseignants.Add(nouveau);
        depot.Sauver();
        return Results.Ok(new { token = Securite.Jeton(nouveau.Id) });
    }
});

auth.MapPost("/connexion", (ConnexionDto d) =>
{
    lock (depot.Verrou)
    {
        var e = depot.D.Enseignants.FirstOrDefault(x => x.Email == d.Email);
        if (e is null || string.IsNullOrEmpty(d.MotDePasse) || !Securite.Verifier(d.MotDePasse, e.Sel, e.Hash))
            return Results.Json(new { message = "Identifiant ou mot de passe incorrect" }, statusCode: 401);
        return Results.Ok(new { token = Securite.Jeton(e.Id) });
    }
});

// ---------- Écrans 3 à 8 (jeton obligatoire) ----------
var api = app.MapGroup("/api").AddEndpointFilter(Securite.Authentifier);

// Écran 3 : liste de classe
api.MapGet("/classes", (HttpContext http) =>
{
    var uid = Securite.Uid(http);
    lock (depot.Verrou)
    {
        var d = depot.D;
        var liste = d.Classes.Where(c => c.EnseignantId == uid)
            .Select(c => new ClasseDto(c.Id, c.Nom,
                d.Etudiants.Where(e => e.ClasseId == c.Id).OrderBy(e => e.Nom)
                    .Select(e => new EtudiantDto(e.Id, e.Matricule, e.Nom, e.Prenom)).ToList()))
            .ToList();
        return Results.Ok(liste);
    }
});

// Écran 4 : synchronisation des appels faits hors connexion (idempotent : pas de doublon si on renvoie)
api.MapPost("/sync/appels", (List<AppelDto> envoi, HttpContext http) =>
{
    var uid = Securite.Uid(http);
    var resultats = new List<ResultatSync>();
    lock (depot.Verrou)
    {
        var d = depot.D;
        foreach (var a in envoi)
        {
            var classe = d.Classes.FirstOrDefault(c => c.Id == a.ClasseId && c.EnseignantId == uid);
            if (classe is null) { resultats.Add(new ResultatSync(a.Id, "refuse", "Classe inconnue")); continue; }

            var appel = d.Appels.FirstOrDefault(x => x.Id == a.Id);
            if (appel is not null)
            {
                if (appel.EnseignantId != uid) { resultats.Add(new ResultatSync(a.Id, "refuse", "Appel d'un autre enseignant")); continue; }
                // VERROUILLAGE : un appel validé ne peut plus être modifié
                if (appel.Verrouille) { resultats.Add(new ResultatSync(a.Id, "deja_verrouille", "Appel déjà validé, modification refusée")); continue; }
            }
            else
            {
                if (d.Appels.Any(x => x.SeanceId == a.SeanceId))
                { resultats.Add(new ResultatSync(a.Id, "doublon", "Un appel existe déjà pour cette séance")); continue; }

                if (!d.Seances.Any(s => s.Id == a.SeanceId))
                    d.Seances.Add(new Seance { Id = a.SeanceId, ClasseId = classe.Id, Date = a.Date, Intitule = a.Intitule });

                appel = new Appel { Id = a.Id, SeanceId = a.SeanceId, EnseignantId = uid };
                d.Appels.Add(appel);
            }

            var idsClasse = d.Etudiants.Where(e => e.ClasseId == classe.Id).Select(e => e.Id).ToHashSet();
            foreach (var p in a.Presences.Where(q => idsClasse.Contains(q.EtudiantId)))
            {
                var existante = appel.Presences.FirstOrDefault(x => x.EtudiantId == p.EtudiantId);
                if (existante is null) appel.Presences.Add(new Presence { EtudiantId = p.EtudiantId, Present = p.Present });
                else existante.Present = p.Present;
            }
            appel.Version++;

            if (a.Valide)
            {
                // Les étudiants non cochés sont enregistrés absents, puis l'appel est verrouillé
                foreach (var id in idsClasse.Where(i => appel.Presences.All(x => x.EtudiantId != i)))
                    appel.Presences.Add(new Presence { EtudiantId = id, Present = false });
                appel.Verrouille = true;
                appel.ValideLe = DateTime.UtcNow;
            }
            resultats.Add(new ResultatSync(a.Id, a.Valide ? "valide" : "enregistre", null));
        }
        depot.Sauver();
    }
    return Results.Ok(resultats);
});

// Écran 5 : absents d'une séance
api.MapGet("/appels/{appelId:guid}/absences", (Guid appelId, HttpContext http) =>
{
    var uid = Securite.Uid(http);
    lock (depot.Verrou)
    {
        var d = depot.D;
        var appel = d.Appels.FirstOrDefault(a => a.Id == appelId && a.EnseignantId == uid);
        if (appel is null) return Results.Ok(new List<AbsenceDto>());
        var liste = appel.Presences.Where(p => !p.Present)
            .Select(p => (p, e: d.Etudiants.FirstOrDefault(x => x.Id == p.EtudiantId)))
            .Where(x => x.e is not null)
            .OrderBy(x => x.e!.Nom)
            .Select(x => new AbsenceDto(x.e!.Id, x.e.Nom, x.e.Prenom, x.p.Motif))
            .ToList();
        return Results.Ok(liste);
    }
});

// Écran 5 : justification d'absence (autorisée même sur un appel verrouillé)
api.MapPut("/appels/{appelId:guid}/justifications/{etudiantId:int}",
    (Guid appelId, int etudiantId, JustificationDto d, HttpContext http) =>
{
    var uid = Securite.Uid(http);
    if (string.IsNullOrWhiteSpace(d.Motif)) return Results.BadRequest(new { champ = "motif", message = "Motif manquant" });
    lock (depot.Verrou)
    {
        var appel = depot.D.Appels.FirstOrDefault(a => a.Id == appelId && a.EnseignantId == uid);
        var p = appel?.Presences.FirstOrDefault(x => x.EtudiantId == etudiantId);
        if (p is null) return Results.NotFound();
        if (p.Present) return Results.BadRequest(new { message = "Cet étudiant était présent" });
        p.Motif = d.Motif;
        p.Commentaire = d.Commentaire;
        depot.Sauver();
        return Results.Ok();
    }
});

// Écran 6 : taux d'assiduité (seuil : 75 % par défaut)
api.MapGet("/classes/{classeId:int}/assiduite", (int classeId, int? seuil, HttpContext http) =>
{
    var uid = Securite.Uid(http);
    lock (depot.Verrou) return Results.Ok(Calculs.Assiduite(depot.D, classeId, uid, seuil ?? 75));
});

// Écran 7 : historique par séance
api.MapGet("/classes/{classeId:int}/seances", (int classeId, HttpContext http) =>
{
    var uid = Securite.Uid(http);
    lock (depot.Verrou)
    {
        var d = depot.D;
        var liste = d.Appels.Where(a => a.EnseignantId == uid)
            .Select(a => (a, s: d.Seances.FirstOrDefault(x => x.Id == a.SeanceId)))
            .Where(x => x.s is not null && x.s.ClasseId == classeId)
            .OrderByDescending(x => x.s!.Date)
            .Select(x => new SeanceResume(x.a.Id, x.s!.Intitule, x.s.Date, x.a.Verrouille,
                x.a.Presences.Count(p => p.Present), x.a.Presences.Count(p => !p.Present)))
            .ToList();
        return Results.Ok(liste);
    }
});

// Écran 8 : export CSV (s'ouvre dans Excel)
api.MapGet("/classes/{classeId:int}/export", (int classeId, HttpContext http) =>
{
    var uid = Securite.Uid(http);
    lock (depot.Verrou)
        return Results.File(Calculs.Csv(Calculs.Assiduite(depot.D, classeId, uid, 75)), "text/csv", "assiduite.csv");
});

app.Run();
