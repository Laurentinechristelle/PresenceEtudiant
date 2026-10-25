using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

public class ClasseViewModel : Observable
{
    private readonly ApiService _api;
    private readonly LocalDb _db;
    private readonly AppelService _appels;
    private readonly SyncService _sync;
    private readonly Session _session;
    private string _titre = "Ma classe", _compteur = "", _etat = "";

    public ObservableCollection<EtudiantDto> Etudiants { get; } = new();
    public string Titre { get => _titre; set => Changer(ref _titre, value); }
    public string Compteur { get => _compteur; set => Changer(ref _compteur, value); }
    public string Etat { get => _etat; set => Changer(ref _etat, value); }

    public ICommand FaireAppelCommand { get; }
    public ICommand HistoriqueCommand { get; }
    public ICommand AssiduiteCommand { get; }
    public ICommand DeconnexionCommand { get; }

    public ClasseViewModel(ApiService api, LocalDb db, AppelService appels, SyncService sync, Session session)
    {
        _api = api; _db = db; _appels = appels; _sync = sync; _session = session;
        FaireAppelCommand = new Command(async () => await FaireAppel());
        HistoriqueCommand = new Command(async () => await Shell.Current.GoToAsync("historique"));
        AssiduiteCommand = new Command(async () => await Shell.Current.GoToAsync("assiduite"));
        DeconnexionCommand = new Command(async () =>
        {
            Stockage.EffacerJeton();
            await Shell.Current.GoToAsync("//connexion");
        });
    }

    public async Task ChargerAsync()
    {
        List<ClasseDto> classes;
        Etat = "Connexion au serveur...";
        await ServeurLocal.DemarrerAsync();             // sur Windows : lance le serveur si besoin
        try
        {
            classes = await _api.ClassesAsync();         // en ligne : on prend la liste du serveur
            await _db.SauverClassesAsync(classes);       // et on la garde dans le téléphone
            Etat = "";
        }
        catch (UnauthorizedAccessException)
        {
            Stockage.EffacerJeton();                     // jeton refusé : retour à la connexion
            await Shell.Current.GoToAsync("//connexion");
            return;
        }
        catch (Exception)
        {
            classes = await _db.ClassesAsync();          // hors connexion : liste enregistrée
            Etat = "Hors connexion : liste enregistrée sur le téléphone";
        }

        await _sync.SynchroniserAsync();                 // envoie les appels en attente

        Etudiants.Clear();
        var classe = classes.FirstOrDefault();
        if (classe is null)
        {
            Titre = "Aucune classe";
            Compteur = "Aucun étudiant dans cette classe";
            return;
        }
        _session.Classe = classe;
        Titre = classe.Nom;
        foreach (var e in classe.Etudiants) Etudiants.Add(e);
        Compteur = $"{Etudiants.Count} étudiants";
    }

    private async Task FaireAppel()
    {
        if (_session.Classe is null) return;
        var appel = await _appels.DemarrerAsync(_session.Classe, $"Séance du {DateTime.Now:dd/MM/yyyy HH:mm}");
        _session.AppelId = appel.Id;
        await Shell.Current.GoToAsync("appel");
    }
}
