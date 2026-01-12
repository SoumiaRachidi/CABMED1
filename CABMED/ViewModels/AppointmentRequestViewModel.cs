using System;

namespace CABMED.ViewModels
{
    public class AppointmentRequestViewModel
    {
        public int RequestId { get; set; }
        public int? ConsultationId { get; set; }
        public int PatientId { get; set; }

        public string PatientNom { get; set; }
        public string PatientPrenom { get; set; }
        public string PatientTelephone { get; set; }
        public string PatientEmail { get; set; }

        public DateTime DateDemande { get; set; } = DateTime.Now;
        public DateTime? PreferredDate { get; set; }
        public TimeSpan? PreferredTime { get; set; }
        public string PreferredSpecialty { get; set; }
        public string SymptomsDescription { get; set; }
        public string AdditionalComments { get; set; }

        public string UrgencyLevel { get; set; }
        public string Status { get; set; } = "En attente";

        public string AssignedDoctor { get; set; }
        public int? AssignedDoctorId { get; set; }
        public string SecretaryComments { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedBy { get; set; }

        // --- SECTION CORRIGÉE : Gestion des Dates et Heures ---

        // 1. Les données brutes (stockées dans le fichier JSON par le Repository)
        public DateTime? ConfirmedDate { get; set; }
        public TimeSpan? ConfirmedStartTime { get; set; }
        public TimeSpan? ConfirmedEndTime { get; set; } // Ajouté pour corriger votre erreur actuelle
        public int? RendezVousId { get; set; }

        // 2. Les propriétés calculées (lues par le Contrôleur pour l'affichage)
        // Elles combinent automatiquement la Date + l'Heure

        public DateTime? ConfirmedStartDateTime
        {
            get
            {
                if (ConfirmedDate.HasValue && ConfirmedStartTime.HasValue)
                {
                    return ConfirmedDate.Value.Add(ConfirmedStartTime.Value);
                }
                return null;
            }
        }

        public DateTime? ConfirmedEndDateTime
        {
            get
            {
                if (ConfirmedDate.HasValue && ConfirmedEndTime.HasValue)
                {
                    return ConfirmedDate.Value.Add(ConfirmedEndTime.Value);
                }
                return null;
            }
        }

        // --- Helpers ---
        public bool IsUrgent =>
            !string.IsNullOrEmpty(UrgencyLevel) &&
            (UrgencyLevel.Equals("élevé", StringComparison.OrdinalIgnoreCase) ||
             UrgencyLevel.Equals("urgent", StringComparison.OrdinalIgnoreCase));
    }
}