using System;
using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    /// <summary>
    /// View model for creating and displaying medical consultations
    /// </summary>
    public class ConsultationViewModel
    {
        public int ConsultationId { get; set; }

        [Required(ErrorMessage = "Le rendez-vous est obligatoire")]
        public int RendezVousId { get; set; }

        [Required(ErrorMessage = "Le médecin est obligatoire")]
        public int MedecinId { get; set; }

        [Required(ErrorMessage = "Les symptômes sont obligatoires")]
        [Display(Name = "Symptômes")]
        [StringLength(2000, ErrorMessage = "Les symptômes ne peuvent pas dépasser 2000 caractères")]
        public string Symptoms { get; set; }

        [Required(ErrorMessage = "Le diagnostic est obligatoire")]
        [Display(Name = "Diagnostic")]
        [StringLength(2000, ErrorMessage = "Le diagnostic ne peut pas dépasser 2000 caractères")]
        public string Diagnostic { get; set; }

        [Display(Name = "Notes médicales")]
        [StringLength(4000, ErrorMessage = "Les notes ne peuvent pas dépasser 4000 caractères")]
        public string Notes { get; set; }

        [Required]
        [Display(Name = "Date de consultation")]
        public DateTime DateConsultation { get; set; }

        // Patient information for display
        public string PatientNom { get; set; }
        public string PatientPrenom { get; set; }
        public string PatientTelephone { get; set; }
        public DateTime? PatientDateNaissance { get; set; }
        public string PatientAntecedents { get; set; }

        // Appointment information
        public string Motif { get; set; }
        public DateTime DateHeureDebut { get; set; }

        // Doctor information
        public string DoctorName { get; set; }
        public string DoctorSpecialite { get; set; }

        public ConsultationViewModel()
        {
            DateConsultation = DateTime.Now;
        }
    }
}
