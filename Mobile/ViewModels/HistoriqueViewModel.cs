using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

public class HistoriqueViewModel : Observable
{
    private readonly ApiService _api;
    private readonly Session _session;
    private string _message = "";

    public ObservableCollection<SeanceResume> Seances { get; } = new();
    public string Message { get => _message; set => Changer(ref _message, value); }

    // Un appui sur une séance ouvre la justification des absences de cette séance
    public ICommand OuvrirCommand { get; }

    public HistoriqueViewModel(ApiService api, Session session)
    {
        _api = api; _session = session;
        OuvrirCommand = new Command<SeanceResume>(async seance =>
        {
            if (seance is null) return;
            _session.JustifAppelId = seance.AppelId;
            await Shell.Current.GoToAsync("justification");
        });
    }

    public async Task ChargerAsync()
    {
        Seances.Clear();
        Message = "";
        if (_session.Classe is null) return;
        try
        {
            foreach (var s in await _api.SeancesAsync(_session.Classe.Id)) Seances.Add(s);
            if (Seances.Count == 0) Message = "Pas encore de séance : fais un premier appel.";
        }
        catch (Exception)
        {
            Message = "Chargement impossible. Vérifie le réseau puis reviens sur cet écran.";
        }
    }
}
