using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

public class InscriptionViewModel : Observable
{
    private readonly ApiService _api;
    private string _nom = "", _prenom = "", _email = "", _motDePasse = "", _confirmation = "", _message = "";

    public string Nom { get => _nom; set => Changer(ref _nom, value); }
    public string Prenom { get => _prenom; set => Changer(ref _prenom, value); }
    public string Email { get => _email; set => Changer(ref _email, value); }
    public string MotDePasse { get => _motDePasse; set => Changer(ref _motDePasse, value); }
    public string Confirmation { get => _confirmation; set => Changer(ref _confirmation, value); }
    public string Message { get => _message; set => Changer(ref _message, value); }

    public ICommand CreerCommand { get; }
    public ICommand DejaUnCompteCommand { get; }

    public InscriptionViewModel(ApiService api)
    {
        _api = api;
        CreerCommand = new Command(async () => await Creer());
        DejaUnCompteCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task Creer()
    {
        Message = "";
        if (string.IsNullOrWhiteSpace(Nom) || string.IsNullOrWhiteSpace(Prenom) || string.IsNullOrWhiteSpace(Email))
        {
            Message = "Champ obligatoire";
            return;
        }
        if (MotDePasse != Confirmation)
        {
            Message = "Les mots de passe sont différents";
            return;
        }
        try
        {
            await ServeurLocal.DemarrerAsync();
            var (ok, msg) = await _api.InscriptionAsync(new InscriptionDto(Nom, Prenom, Email, MotDePasse, Confirmation));
            if (ok) await Shell.Current.GoToAsync("//classe");
            else Message = msg ?? "Erreur d'inscription";
        }
        catch (Exception)
        {
            Message = "Serveur injoignable. Vérifie que l'API est lancée (Lancer-Serveur.bat).";
        }
    }
}
