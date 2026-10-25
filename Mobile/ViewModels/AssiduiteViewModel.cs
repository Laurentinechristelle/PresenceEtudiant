using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PresenceEtudiant.Mobile;

public class AssiduiteViewModel : Observable
{
    private readonly ApiService _api;
    private readonly Session _session;
    private string _tauxClasse = "", _message = "";

    public ObservableCollection<LigneAssiduite> Lignes { get; } = new();
    public string TauxClasse { get => _tauxClasse; set => Changer(ref _tauxClasse, value); }
    public string Message { get => _message; set => Changer(ref _message, value); }

    public ICommand ActualiserCommand { get; }
    public ICommand ExporterCommand { get; }

    public AssiduiteViewModel(ApiService api, Session session)
    {
        _api = api; _session = session;
        ActualiserCommand = new Command(async () => await ChargerAsync());
        ExporterCommand = new Command(async () => await Exporter());
    }

    public async Task ChargerAsync()
    {
        Lignes.Clear();
        TauxClasse = "";
        Message = "";
        if (_session.Classe is null) return;
        try
        {
            var lignes = await _api.AssiduiteAsync(_session.Classe.Id);
            if (lignes.All(l => l.Seances == 0))
            {
                Message = "Pas encore de séance validée : fais un premier appel.";
                return;
            }
            foreach (var l in lignes) Lignes.Add(l);
            TauxClasse = $"Taux de la classe : {(int)Math.Round(lignes.Average(x => x.Taux))} %";
        }
        catch (Exception)
        {
            Message = "Chargement impossible. Vérifie le réseau puis appuie sur Actualiser.";
        }
    }

    private async Task Exporter()
    {
        if (_session.Classe is null) return;
        try
        {
            var octets = await _api.ExportAsync(_session.Classe.Id);
            var chemin = Path.Combine(FileSystem.CacheDirectory, "assiduite.csv");
            await File.WriteAllBytesAsync(chemin, octets);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Export assiduité", File = new ShareFile(chemin) });
        }
        catch (Exception)
        {
            Message = "Export impossible. Vérifie le réseau puis réessaie.";
        }
    }
}
