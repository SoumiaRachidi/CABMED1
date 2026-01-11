using System;
using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    public class AppointmentRequestViewModel
    {
        public int RequestId { get; set; }

        [Required(ErrorMessage = "La description des symptômes est obligatoire")]
        [Display(Name = "Description des symptômes")]
        [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
        public string SymptomsDescription { get; set; }

        [Required(ErrorMessage = "La spécialité préférée est obligatoire")]
        [Display(Name = "Spécialité préférée")]
        public string PreferredSpecialty { get; set; }

        [Required(ErrorMessage = "Le niveau d'urgence est obligatoire")]
        [Display(Name = "Niveau d'urgence")]
        public string UrgencyLevel { get; set; }

        [Display(Name = "Date préférée")]
        [DataType(DataType.Date)]
        public DateTime? PreferredDate { get; set; }

        [Display(Name = "Heure préférée")]
        [DataType(DataType.Time)]
        public TimeSpan? PreferredTime { get; set; }

        [Display(Name = "Commentaires additionnels")]
        [StringLength(500, ErrorMessage = "Les commentaires ne peuvent pas dépasser 500 caractères")]
        public string AdditionalComments { get; set; }

        // Patient info (auto-filled from session)
        public int PatientId { get; set; }
        public string PatientNom { get; set; }
        public string PatientPrenom { get; set; }
        public string PatientTelephone { get; set; }
        public string PatientEmail { get; set; }

        public int? RendezVousId { get; set; }

        public int? AssignedDoctorId { get; set; }
        public string AssignedDoctor { get; set; }

        // Status tracking
        public string Status { get; set; } = "En attente"; // "En attente", "Approuvé", "Refusé"
        public DateTime DateDemande { get; set; } = DateTime.Now;
        public string SecretaryComments { get; set; }

        // Secretary processing metadata
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedBy { get; set; }

        // Confirmation details once request is approved
        public DateTime? ConfirmedDate { get; set; }
        public TimeSpan? ConfirmedStartTime { get; set; }
        public TimeSpan? ConfirmedEndTime { get; set; }

        public DateTime? ConfirmedStartDateTime => ConfirmedDate.HasValue && ConfirmedStartTime.HasValue
            ? ConfirmedDate.Value.Date + ConfirmedStartTime.Value
            : (DateTime?)null;

        public DateTime? ConfirmedEndDateTime => ConfirmedDate.HasValue && ConfirmedEndTime.HasValue
            ? ConfirmedDate.Value.Date + ConfirmedEndTime.Value
            : (DateTime?)null;
    }
}