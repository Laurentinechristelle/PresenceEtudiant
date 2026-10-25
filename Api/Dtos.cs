namespace PresenceEtudiant.Api;

public record InscriptionDto(string? Nom, string? Prenom, string? Email, string? MotDePasse, string? Confirmation);
public record ConnexionDto(string? Email, string? MotDePasse);
public record EtudiantDto(int Id, string Matricule, string Nom, string Prenom);
public record ClasseDto(int Id, string Nom, List<EtudiantDto> Etudiants);
public record PresenceDto(int EtudiantId, bool Present);
public record AppelDto(Guid Id, Guid SeanceId, int ClasseId, DateTime Date, string Intitule, bool Valide, List<PresenceDto> Presences);
public record ResultatSync(Guid Id, string Statut, string? Message);
public record JustificationDto(string? Motif, string? Commentaire);
public record SeanceResume(Guid AppelId, string Intitule, DateTime Date, bool Verrouille, int Presents, int Absents);
public record LigneAssiduite(int EtudiantId, string Matricule, string Nom, string Prenom, int Seances, int Presents, int Taux, bool SousSeuil);
public record AbsenceDto(int EtudiantId, string Nom, string Prenom, string? Motif);
