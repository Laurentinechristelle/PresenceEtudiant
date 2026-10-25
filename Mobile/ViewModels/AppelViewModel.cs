using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

// Une ligne de la liste d'appel
public class LigneAppel : Observable
{
    private bool _present = true;

    public int EtudiantId { get; init; }
    public string Nom { get; init; } = "";
    public string Matricule { get; init; } = "";
    public string Initiales { get; init; } = "";

    public bool Present
    {
        get => _present;
        set
        {
            if (Changer(ref _present, value)) Notifier(nameof(Statut));
        }
    }

    // Texte écrit : l'état ne dépend pas de la couleur seule
    public string Statut => Present ? "Présent" : "Absent";
}

public class AppelViewModel : Observable
{
    private readonly AppelService _appels;
    private readonly LocalDb _db;
    private readonly Session _session;
    private string _resume = "", _message = "";
    private bool _verrouille;

    public ObservableCollection<LigneAppel> Lignes { get; } = new();
    public string Resume { get => _resume; set => Changer(ref _resume, value); }
    public string Message { get => _message; set => Changer(ref _message, value); }
    public bool Verrouille { get => _verrouille; set => Changer(ref _verrouille, value); }

    public ICommand BasculerCommand { get; }
    public ICommand ToutPresentCommand { get; }
    public ICommand ValiderCommand { get; }

    public AppelViewModel(AppelService appels, LocalDb db, Session session)
    {
        _appels = appels; _db = db; _session = session;
        BasculerCommand = new Command<LigneAppel>(async ligne =>
        {
            if (ligne is not null) await Basculer(ligne);
        });
        ToutPresentCommand = new Command(async () =>
        {
            foreach (var ligne in Lignes.Where(l => !l.Present).ToList()) await Basculer(ligne);
        });
        ValiderCommand = new Command(async () => await Valider());
    }

    public async Task ChargerAsync()
    {
        var appel = await _db.AppelAsync(_session.AppelId);
        if (appel is null || _session.Classe is null) return;

        Lignes.Clear();
        foreach (var e in _session.Classe.Etudiants)
        {
            var p = appel.Presences.FirstOrDefault(x => x.EtudiantId == e.Id);
            Lignes.Add(new LigneAppel
            {
                EtudiantId = e.Id,
                Nom = $"{e.Nom} {e.Prenom}",
                Matricule = e.Matricule,
                Initiales = $"{e.Nom[0]}{e.Prenom[0]}",
                Present = p?.Present ?? true
            });
        }
        Verrouille = appel.Verrouille;
        MettreAJourResume();
    }

    private void MettreAJourResume() => Resume = $"Présents : {Lignes.Count(l => l.Present)} / {Lignes.Count}";

    private async Task Basculer(LigneAppel ligne)
    {
        if (Verrouille)
        {
            Message = "Appel verrouillé : modification impossible";
            return;
        }
        await _appels.BasculerAsync(_session.AppelId, ligne.EtudiantId);
        ligne.Present = !ligne.Present;
        MettreAJourResume();
    }

    private async Task Valider()
    {
        await _appels.ValiderAsync(_session.AppelId);
        Verrouille = true;
        Message = "Appel validé et verrouillé. Envoi au serveur dès que le réseau est disponible.";
    }
}
