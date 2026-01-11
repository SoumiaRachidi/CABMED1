using System;

namespace CABMED.ViewModels
{
    public class AppointmentViewModel
    {
        public int RendezVousId { get; set; }
        public int? ConsultationId { get; set; }
        public int PatientId { get; set; }
        public DateTime DateHeureDebut { get; set; }
        public DateTime DateHeureFin { get; set; }
        public string HeureDebut => DateHeureDebut.ToString("HH:mm");
        public string HeureFin => DateHeureFin.ToString("HH:mm");
        public string PatientNom { get; set; }
        public string PatientPrenom { get; set; }
        public string PatientTelephone { get; set; }
        public string Motif { get; set; }
        public string Statut { get; set; }

        // Extended properties for appointment request tracking
        public string RequestStatus { get; set; } = "En attente"; // "En attente", "Approuvé", "Refusé", "Confirmé", "Terminé"
        public DateTime? DateDemande { get; set; }
        public string UrgencyLevel { get; set; }
        public string PreferredSpecialty { get; set; }
        public string SymptomsDescription { get; set; }
        public string AssignedDoctor { get; set; }
        public int? AssignedDoctorId { get; set; }
        public string SecretaryComments { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedBy { get; set; }

        // Display helper properties
        public string StatusBadgeClass
        {
            get
            {
                switch (RequestStatus?.ToLower())
                {
                    case "en attente":
                        return "badge-warning";
                    case "approuvé":
                    case "confirmé":
                        return "badge-success";
                    case "refusé":
                        return "badge-danger";
                    case "terminé":
                        return "badge-info";
                    default:
                        return "badge-secondary";
                }
            }
        }

        public string PriorityClass
        {
            get
            {
                switch (UrgencyLevel?.ToLower())
                {
                    case "élevé":
                    case "urgent":
                        return "priority-high";
                    case "moyen":
                        return "priority-medium";
                    case "faible":
                        return "priority-low";
                    default:
                        return "priority-normal";
                }
            }
        }

        public bool IsUrgent => UrgencyLevel?.ToLower() == "élevé" || UrgencyLevel?.ToLower() == "urgent";
        public bool IsPending => RequestStatus?.ToLower() == "en attente";
        public bool IsApproved => RequestStatus?.ToLower() == "approuvé" || RequestStatus?.ToLower() == "confirmé";
        public bool IsDeclined => RequestStatus?.ToLower() == "refusé";
    }
}
