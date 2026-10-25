using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

public class ConnexionViewModel : Observable
{
    private readonly ApiService _api;
    private string _email = "prof@test.cm";            // compte de démonstration prérempli
    private string _motDePasse = "Motdepasse1!";
    private string _message = "";
    private bool _enCours;

    public string Email { get => _email; set => Changer(ref _email, value); }
    public string MotDePasse { get => _motDePasse; set => Changer(ref _motDePasse, value); }
    public string Message { get => _message; set => Changer(ref _message, value); }
    public bool EnCours { get => _enCours; set => Changer(ref _enCours, value); }

    public ICommand SeConnecterCommand { get; }
    public ICommand CreerCompteCommand { get; }

    public ConnexionViewModel(ApiService api)
    {
        _api = api;
        SeConnecterCommand = new Command(async () => await SeConnecter());
        CreerCompteCommand = new Command(async () => await Shell.Current.GoToAsync("inscription"));
    }

    // Si un jeton est déjà enregistré, on saute l'écran de connexion (utile hors connexion)
    public async Task VerifierJetonAsync()
    {
        var jeton = await Stockage.LireJetonAsync();
        if (jeton is not null) await Shell.Current.GoToAsync("//classe");
    }

    private async Task SeConnecter()
    {
        Message = "";
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            Message = "Champ obligatoire";
            return;
        }
        EnCours = true;
        try
        {
            Message = "Démarrage du serveur (la première fois, cela peut durer 1 à 2 minutes)...";
            await ServeurLocal.DemarrerAsync();
            Message = "";
            if (await _api.ConnexionAsync(Email, MotDePasse)) await Shell.Current.GoToAsync("//classe");
            else Message = "Identifiant ou mot de passe incorrect";
        }
        catch (Exception)
        {
            Message = "Serveur injoignable. Vérifie que l'API est lancée (Lancer-Serveur.bat).";
        }
        finally
        {
            EnCours = false;
        }
    }
}
