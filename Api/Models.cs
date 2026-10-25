namespace PresenceEtudiant.Api;

// Les données sont gardées dans un simple fichier JSON (presence-donnees.json) : pas de base à installer.
public class Enseignant
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Sel { get; set; } = "";
    public string Hash { get; set; } = "";
}

public class Classe
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public int EnseignantId { get; set; }
}

public class Etudiant
{
    public int Id { get; set; }
    public string Matricule { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public int ClasseId { get; set; }
}

public class Seance
{
    public Guid Id { get; set; }
    public int ClasseId { get; set; }
    public DateTime Date { get; set; }
    public string Intitule { get; set; } = "";
}

public class Presence
{
    public int EtudiantId { get; set; }
    public bool Present { get; set; }
    public string? Motif { get; set; }
    public string? Commentaire { get; set; }
}

public class Appel
{
    public Guid Id { get; set; }
    public Guid SeanceId { get; set; }
    public int EnseignantId { get; set; }
    public bool Verrouille { get; set; }
    public DateTime? ValideLe { get; set; }
    public int Version { get; set; }
    public List<Presence> Presences { get; set; } = new();
}

public class Donnees
{
    public List<Enseignant> Enseignants { get; set; } = new();
    public List<Classe> Classes { get; set; } = new();
    public List<Etudiant> Etudiants { get; set; } = new();
    public List<Seance> Seances { get; set; } = new();
    public List<Appel> Appels { get; set; } = new();
}
