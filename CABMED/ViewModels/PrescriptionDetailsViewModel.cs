using System;

namespace CABMED.ViewModels
{
    public class PrescriptionDetailsViewModel
    {
        public int PrescriptionId { get; set; }
        public DateTime DatePrescription { get; set; }

        public string PatientName { get; set; }
        public int PatientId { get; set; }

        public string DoctorName { get; set; }
        public string DoctorSpecialite { get; set; }

        public string Medicament { get; set; }
        public string Posologie { get; set; }
    }
}
