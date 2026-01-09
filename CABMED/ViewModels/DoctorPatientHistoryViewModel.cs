using System;
using System.Collections.Generic;

namespace CABMED.ViewModels
{
    public class DoctorPatientHistoryViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime? DateNaissance { get; set; }

        public IList<PatientHistoryItemViewModel> HistoryItems { get; set; }
    }

    public class PatientHistoryItemViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } // Consultation, Prescription, Examen
        public string Summary { get; set; }
    }
}
