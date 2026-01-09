using System;
using System.ComponentModel.DataAnnotations;

namespace CABMED.ViewModels
{
    public class PrescriptionViewModel
    {
        [Required]
        public int ConsultationId { get; set; }

        [Display(Name = "Médicament(s)")]
        [Required]
        [StringLength(1000)]
        public string Medicament { get; set; }

        [Display(Name = "Posologie / Instructions")]
        [Required]
        [StringLength(2000)]
        public string Posologie { get; set; }

        // Read-only info for the page
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime DatePrescription { get; set; }
    }
}
