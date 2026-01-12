using System;
using System.Collections.Generic;

namespace CABMED.ViewModels
{
    public class PrescriptionDetailsViewModel
    {
        public int PrescriptionId { get; set; }
        public int ConsultationId { get; set; }
        public DateTime DatePrescription { get; set; }

        public string PatientName { get; set; }
        public int PatientId { get; set; }

        public string DoctorName { get; set; }
        public string DoctorSpecialite { get; set; }

        // Single medication (for backward compatibility)
        public string Medicament { get; set; }
        public string Posologie { get; set; }

        // Multiple medications support
        public List<PrescriptionMedicationViewModel> Medications { get; set; }

        public PrescriptionDetailsViewModel()
        {
            Medications = new List<PrescriptionMedicationViewModel>();
        }
    }

    public class PrescriptionMedicationViewModel
    {
        public string Medicament { get; set; }
        public string Posologie { get; set; }
    }
}
