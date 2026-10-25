using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PresenceEtudiant.Mobile;

// Base de tous les ViewModels : prévient l'écran quand une valeur change
public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Notifier([CallerMemberName] string? nom = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));

    protected bool Changer<T>(ref T champ, T valeur, [CallerMemberName] string? nom = null)
    {
        if (EqualityComparer<T>.Default.Equals(champ, valeur)) return false;
        champ = valeur;
        Notifier(nom);
        return true;
    }
}
