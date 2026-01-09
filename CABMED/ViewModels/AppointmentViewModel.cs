using System;

namespace CABMED.ViewModels
{
    public class AppointmentViewModel
    {
        public int RendezVousId { get; set; }
        public int? ConsultationId { get; set; }
        public DateTime DateHeureDebut { get; set; }
        public DateTime DateHeureFin { get; set; }
        public string HeureDebut => DateHeureDebut.ToString("HH:mm");
        public string HeureFin => DateHeureFin.ToString("HH:mm");
        public string PatientNom { get; set; }
        public string PatientPrenom { get; set; }
        public string PatientTelephone { get; set; }
        public string Motif { get; set; }
        public string Statut { get; set; }
    }
}
