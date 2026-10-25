using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

public class JustificationViewModel : Observable
{
    private readonly ApiService _api;
    private readonly Session _session;
    private AbsenceDto? _choisie;
    private string _motif = "", _commentaire = "", _message = "";

    public ObservableCollection<AbsenceDto> Absences { get; } = new();
    public AbsenceDto? Choisie { get => _choisie; set => Changer(ref _choisie, value); }
    public string Motif { get => _motif; set => Changer(ref _motif, value); }
    public string Commentaire { get => _commentaire; set => Changer(ref _commentaire, value); }
    public string Message { get => _message; set => Changer(ref _message, value); }

    public ICommand EnregistrerCommand { get; }

    public JustificationViewModel(ApiService api, Session session)
    {
        _api = api; _session = session;
        EnregistrerCommand = new Command(async () => await Enregistrer());
    }

    public async Task ChargerAsync()
    {
        Absences.Clear();
        try
        {
            foreach (var a in await _api.AbsencesAsync(_session.JustifAppelId)) Absences.Add(a);
            Message = Absences.Count == 0 ? "Aucune absence dans cette séance." : "";
        }
        catch (Exception)
        {
            Message = "Chargement impossible. Vérifie le réseau puis réessaie.";
        }
    }

    private async Task Enregistrer()
    {
        if (Choisie is null)
        {
            Message = "Choisis d'abord un étudiant absent";
            return;
        }
        if (string.IsNullOrWhiteSpace(Motif))
        {
            Message = "Motif manquant";
            return;
        }
        try
        {
            await _api.JustifierAsync(_session.JustifAppelId, Choisie.EtudiantId, new JustificationDto(Motif, Commentaire));
            Motif = "";
            Commentaire = "";
            await ChargerAsync();
            Message = "Justification enregistrée";
        }
        catch (Exception)
        {
            Message = "Enregistrement impossible. Réessaie.";
        }
    }
}
