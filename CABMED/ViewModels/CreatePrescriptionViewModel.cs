using System;
using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    /// <summary>
    /// View model for creating prescriptions (ordonnances)
    /// </summary>
    public class CreatePrescriptionViewModel
    {
        [Required(ErrorMessage = "La consultation est obligatoire")]
        public int ConsultationId { get; set; }

        [Required(ErrorMessage = "Le nom du médicament est obligatoire")]
        [Display(Name = "Médicament")]
        [StringLength(200, ErrorMessage = "Le nom du médicament ne peut pas dépasser 200 caractères")]
        public string Medicament { get; set; }

        [Required(ErrorMessage = "La posologie est obligatoire")]
        [Display(Name = "Posologie / Dosage")]
        [StringLength(500, ErrorMessage = "La posologie ne peut pas dépasser 500 caractères")]
        public string Posologie { get; set; }

        // Additional fields for better prescription management
        [Display(Name = "Fréquence")]
        [StringLength(200)]
        public string Frequence { get; set; }

        [Display(Name = "Durée du traitement")]
        [StringLength(100)]
        public string Duree { get; set; }

        [Display(Name = "Instructions supplémentaires")]
        [StringLength(1000)]
        public string Instructions { get; set; }

        // For display purposes
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialite { get; set; }
        public DateTime DatePrescription { get; set; }

        public CreatePrescriptionViewModel()
        {
            DatePrescription = DateTime.Now;
        }
    }
}
