using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CABMED.Models;
using CABMED.ViewModels;

namespace CABMED.Controllers
{
    public class DoctorController : Controller
    {
        private CabinetEntities db = new CabinetEntities();

        private bool CheckDoctorAccess()
        {
            var role = (Session["Role"] as string)?.ToLower();
            return role == "medecin";
        }

        // GET: Doctor/Index
        public ActionResult Index()
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;
            return View();
        }

        // GET: Doctor/TodayAppointments
        public ActionResult TodayAppointments()
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var appointments = db.RendezVous
                .Include(r => r.Users)   // Patient
                .Include(r => r.Users1)  // Medecin
                .Where(r => r.MedecinId == doctorId.Value)
                .Where(r => r.DateHeureDebut >= today && r.DateHeureDebut < tomorrow)
                .OrderBy(r => r.DateHeureDebut)
                .Select(r => new AppointmentViewModel
                {
                    RendezVousId = r.RendezVousId,
                    DateHeureDebut = r.DateHeureDebut,
                    DateHeureFin = r.DateHeureFin,
                    PatientNom = r.Users != null ? r.Users.Nom : null,
                    PatientPrenom = r.Users != null ? r.Users.Prenom : null,
                    PatientTelephone = r.Users != null ? r.Users.Telephone : null,
                    Motif = r.Motif,
                    Statut = r.Statut,
                    ConsultationId = r.Consultations.Select(c => (int?)c.ConsultationId).FirstOrDefault()
                })
                .ToList();

            return Json(appointments, JsonRequestBehavior.AllowGet);
        }

        // GET: Doctor/UpcomingAppointments
        public ActionResult UpcomingAppointments()
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var now = DateTime.Now;

            var appointments = db.RendezVous
                .Include(r => r.Users)   // Patient
                .Include(r => r.Users1)  // Medecin
                .Where(r => r.MedecinId == doctorId.Value)
                .Where(r => r.DateHeureDebut >= now)
                .OrderBy(r => r.DateHeureDebut)
                .Select(r => new AppointmentViewModel
                {
                    RendezVousId = r.RendezVousId,
                    DateHeureDebut = r.DateHeureDebut,
                    DateHeureFin = r.DateHeureFin,
                    PatientNom = r.Users != null ? r.Users.Nom : null,
                    PatientPrenom = r.Users != null ? r.Users.Prenom : null,
                    PatientTelephone = r.Users != null ? r.Users.Telephone : null,
                    Motif = r.Motif,
                    Statut = r.Statut,
                    ConsultationId = r.Consultations.Select(c => (int?)c.ConsultationId).FirstOrDefault()
                })
                .ToList();

            return Json(appointments, JsonRequestBehavior.AllowGet);
        }

        // GET: Doctor/CreatePrescription/{consultationId}
        public ActionResult CreatePrescription(int consultationId)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var consultation = db.Consultations
                .Include(c => c.RendezVous.Users)
                .FirstOrDefault(c => c.ConsultationId == consultationId && c.MedecinId == doctorId.Value);

            if (consultation == null)
            {
                return HttpNotFound();
            }

            var vm = new PrescriptionViewModel
            {
                ConsultationId = consultation.ConsultationId,
                PatientName = consultation.RendezVous.Users != null
                    ? (consultation.RendezVous.Users.Prenom + " " + consultation.RendezVous.Users.Nom).Trim()
                    : string.Empty,
                DoctorName = Session["UserName"] as string,
                DatePrescription = DateTime.Now
            };

            return View(vm);
        }

        // POST: Doctor/CreatePrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePrescription(PrescriptionViewModel model)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var consultation = db.Consultations
                .FirstOrDefault(c => c.ConsultationId == model.ConsultationId && c.MedecinId == doctorId.Value);

            if (consultation == null)
            {
                return HttpNotFound();
            }

            var prescription = new Prescriptions
            {
                ConsultationId = consultation.ConsultationId,
                Medicament = model.Medicament,
                Posologie = model.Posologie
            };

            db.Prescriptions.Add(prescription);
            db.SaveChanges();

            return RedirectToAction("PrescriptionDetails", new { id = prescription.PrescriptionId });
        }

        // GET: Doctor/PrescriptionDetails/{id}
        public ActionResult PrescriptionDetails(int id)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var prescription = db.Prescriptions
                .Include(p => p.Consultations.RendezVous.Users)
                .Include(p => p.Consultations.Users)
                .FirstOrDefault(p => p.PrescriptionId == id && p.Consultations.MedecinId == doctorId.Value);

            if (prescription == null)
            {
                return HttpNotFound();
            }

            var patient = prescription.Consultations.RendezVous.Users;
            var doctor = prescription.Consultations.Users;

            var vm = new PrescriptionDetailsViewModel
            {
                PrescriptionId = prescription.PrescriptionId,
                DatePrescription = DateTime.Now,
                PatientName = patient != null ? (patient.Prenom + " " + patient.Nom).Trim() : string.Empty,
                PatientId = patient?.UserId ?? 0,
                DoctorName = doctor != null ? (doctor.Prenom + " " + doctor.Nom).Trim() : (Session["UserName"] as string),
                DoctorSpecialite = doctor?.Specialite,
                Medicament = prescription.Medicament,
                Posologie = prescription.Posologie
            };

            return View(vm);
        }

        // GET: Doctor/PatientHistory?patientId=123
        public ActionResult PatientHistory(int patientId)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var patient = db.Users.FirstOrDefault(u => u.UserId == patientId && u.Role.ToLower() == "patient");
            if (patient == null)
            {
                return HttpNotFound();
            }

            var history = new DoctorPatientHistoryViewModel
            {
                PatientId = patient.UserId,
                PatientName = (patient.Prenom + " " + patient.Nom).Trim(),
                DateNaissance = patient.DateNaissance,
                HistoryItems = new System.Collections.Generic.List<PatientHistoryItemViewModel>()
            };

            // Consultations for this patient with this doctor
            var consultations = db.Consultations
                .Include(c => c.RendezVous)
                .Where(c => c.MedecinId == doctorId.Value && c.RendezVous.PatientId == patientId)
                .ToList();

            foreach (var c in consultations)
            {
                history.HistoryItems.Add(new PatientHistoryItemViewModel
                {
                    Date = c.DateConsultation ?? c.RendezVous.DateHeureDebut,
                    Type = "Consultation",
                    Summary = string.IsNullOrEmpty(c.Diagnostic) ? c.Notes : c.Diagnostic
                });

                foreach (var p in c.Prescriptions)
                {
                    history.HistoryItems.Add(new PatientHistoryItemViewModel
                    {
                        Date = c.DateConsultation ?? c.RendezVous.DateHeureDebut,
                        Type = "Prescription",
                        Summary = p.Medicament
                    });
                }

                foreach (var r in c.ResultatsExamens)
                {
                    history.HistoryItems.Add(new PatientHistoryItemViewModel
                    {
                        Date = r.DateExamen,
                        Type = "Examen",
                        Summary = r.TypeExamen
                    });
                }
            }

            history.HistoryItems = history.HistoryItems
                .OrderByDescending(h => h.Date)
                .ToList();

            return View(history);
        }

        // GET: Doctor/SearchPatientHistory?q=...
        public ActionResult SearchPatientHistory(string q)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            q = (q ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(q))
            {
                return View(Enumerable.Empty<Users>());
            }

            var patients = db.RendezVous
                .Include(r => r.Users)
                .Where(r => r.MedecinId == doctorId.Value)
                .Select(r => r.Users)
                .Where(u => u.Role.ToLower() == "patient")
                .Where(u =>
                    (u.Nom + " " + u.Prenom).ToLower().Contains(q) ||
                    u.Email.ToLower().Contains(q) ||
                    u.UserId.ToString() == q)
                .Distinct()
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToList();

            return View(patients);
        }

        // GET: Doctor/StartConsultation/{id}
        /// <summary>
        /// Initiates a new medical consultation for an approved appointment
        /// </summary>
        public ActionResult StartConsultation(int id)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get the appointment with patient information
            var appointment = db.RendezVous
                .Include(r => r.Users) // Patient
                .Include(r => r.Users1) // Doctor
                .FirstOrDefault(r => r.RendezVousId == id && r.MedecinId == doctorId.Value);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Rendez-vous introuvable ou non autorisé";
                return RedirectToAction("Index");
            }

            // Check if consultation already exists
            var existingConsultation = db.Consultations
                .FirstOrDefault(c => c.RendezVousId == id);

            if (existingConsultation != null)
            {
                // Redirect to add prescription if consultation already exists
                TempData["SuccessMessage"] = "Consultation déjà créée";
                return RedirectToAction("AddPrescription", new { id = existingConsultation.ConsultationId });
            }

            var patient = appointment.Users;
            var doctor = appointment.Users1;

            var vm = new ConsultationViewModel
            {
                RendezVousId = id,
                MedecinId = doctorId.Value,
                DateConsultation = DateTime.Now,
                PatientNom = patient?.Nom,
                PatientPrenom = patient?.Prenom,
                PatientTelephone = patient?.Telephone,
                PatientDateNaissance = patient?.DateNaissance,
                PatientAntecedents = patient?.AntecedentsMedicaux,
                Motif = appointment.Motif,
                DateHeureDebut = appointment.DateHeureDebut,
                DoctorName = doctor != null ? (doctor.Prenom + " " + doctor.Nom).Trim() : Session["UserName"] as string,
                DoctorSpecialite = doctor?.Specialite
            };

            return View(vm);
        }

        // POST: Doctor/StartConsultation
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Saves the medical consultation to the database
        /// </summary>
        public ActionResult StartConsultation(ConsultationViewModel model)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                // Reload patient info if validation fails
                var appointment = db.RendezVous
                    .Include(r => r.Users)
                    .FirstOrDefault(r => r.RendezVousId == model.RendezVousId);

                if (appointment != null)
                {
                    var patient = appointment.Users;
                    model.PatientNom = patient?.Nom;
                    model.PatientPrenom = patient?.Prenom;
                    model.PatientTelephone = patient?.Telephone;
                    model.PatientDateNaissance = patient?.DateNaissance;
                    model.PatientAntecedents = patient?.AntecedentsMedicaux;
                    model.Motif = appointment.Motif;
                }

                return View(model);
            }

            // Verify the appointment belongs to this doctor
            var rdv = db.RendezVous.FirstOrDefault(r => r.RendezVousId == model.RendezVousId && r.MedecinId == doctorId.Value);
            if (rdv == null)
            {
                TempData["ErrorMessage"] = "Rendez-vous introuvable ou non autorisé";
                return RedirectToAction("Index");
            }

            // Create new consultation
            var consultation = new Consultations
            {
                RendezVousId = model.RendezVousId,
                MedecinId = doctorId.Value,
                Diagnostic = model.Diagnostic,
                Notes = model.Notes,
                DateConsultation = model.DateConsultation
            };

            db.Consultations.Add(consultation);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Consultation créée avec succès";

            // Redirect to prescription page to allow doctor to prescribe medication
            return RedirectToAction("AddPrescription", new { id = consultation.ConsultationId });
        }

        // GET: Doctor/AddPrescription/{id}
        /// <summary>
        /// Shows form to add prescription for a consultation
        /// </summary>
        public ActionResult AddPrescription(int id)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var consultation = db.Consultations
                .Include(c => c.RendezVous.Users)
                .Include(c => c.Users)
                .FirstOrDefault(c => c.ConsultationId == id && c.MedecinId == doctorId.Value);

            if (consultation == null)
            {
                TempData["ErrorMessage"] = "Consultation introuvable";
                return RedirectToAction("Index");
            }

            var patient = consultation.RendezVous.Users;
            var doctor = consultation.Users;

            var vm = new CreatePrescriptionViewModel
            {
                ConsultationId = id,
                PatientName = patient != null ? (patient.Prenom + " " + patient.Nom).Trim() : string.Empty,
                DoctorName = doctor != null ? (doctor.Prenom + " " + doctor.Nom).Trim() : Session["UserName"] as string,
                DoctorSpecialite = doctor?.Specialite,
                DatePrescription = DateTime.Now
            };

            // Get existing prescriptions for this consultation
            ViewBag.ExistingPrescriptions = db.Prescriptions
                .Where(p => p.ConsultationId == id)
                .ToList();

            return View(vm);
        }

        // POST: Doctor/AddPrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Saves prescription (ordonnance) for a consultation
        /// </summary>
        public ActionResult AddPrescription(CreatePrescriptionViewModel model)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                var consultation = db.Consultations
                    .Include(c => c.RendezVous.Users)
                    .Include(c => c.Users)
                    .FirstOrDefault(c => c.ConsultationId == model.ConsultationId);

                if (consultation != null)
                {
                    var patient = consultation.RendezVous.Users;
                    model.PatientName = patient != null ? (patient.Prenom + " " + patient.Nom).Trim() : string.Empty;
                }

                ViewBag.ExistingPrescriptions = db.Prescriptions
                    .Where(p => p.ConsultationId == model.ConsultationId)
                    .ToList();

                return View(model);
            }

            // Verify consultation belongs to this doctor
            var consult = db.Consultations
                .FirstOrDefault(c => c.ConsultationId == model.ConsultationId && c.MedecinId == doctorId.Value);

            if (consult == null)
            {
                TempData["ErrorMessage"] = "Consultation introuvable";
                return RedirectToAction("Index");
            }

            // Build complete posology string with all details
            var completePosologie = model.Posologie;
            if (!string.IsNullOrWhiteSpace(model.Frequence))
            {
                completePosologie += " | Fréquence: " + model.Frequence;
            }
            if (!string.IsNullOrWhiteSpace(model.Duree))
            {
                completePosologie += " | Durée: " + model.Duree;
            }
            if (!string.IsNullOrWhiteSpace(model.Instructions))
            {
                completePosologie += " | Instructions: " + model.Instructions;
            }

            // Create prescription
            var prescription = new Prescriptions
            {
                ConsultationId = model.ConsultationId,
                Medicament = model.Medicament,
                Posologie = completePosologie
            };

            db.Prescriptions.Add(prescription);
            
            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Médicament ajouté avec succès à l'ordonnance";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de l'ajout du médicament: " + ex.Message;
                
                // Reload data for view
                var consultation = db.Consultations
                    .Include(c => c.RendezVous.Users)
                    .Include(c => c.Users)
                    .FirstOrDefault(c => c.ConsultationId == model.ConsultationId);

                if (consultation != null)
                {
                    var patient = consultation.RendezVous.Users;
                    model.PatientName = patient != null ? (patient.Prenom + " " + patient.Nom).Trim() : string.Empty;
                }

                ViewBag.ExistingPrescriptions = db.Prescriptions
                    .Where(p => p.ConsultationId == model.ConsultationId)
                    .ToList();

                return View(model);
            }

            // Redirect back to add more prescriptions or finish
            return RedirectToAction("AddPrescription", new { id = model.ConsultationId });
        }

        // GET: Doctor/ViewPrescription/{consultationId}
        /// <summary>
        /// Display professional prescription document for a consultation
        /// </summary>
        public ActionResult ViewPrescription(int id)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var consultation = db.Consultations
                .Include(c => c.RendezVous.Users)
                .Include(c => c.Users)
                .Include(c => c.Prescriptions)
                .FirstOrDefault(c => c.ConsultationId == id && c.MedecinId == doctorId.Value);

            if (consultation == null)
            {
                TempData["ErrorMessage"] = "Consultation introuvable";
                return RedirectToAction("Index");
            }

            var patient = consultation.RendezVous.Users;
            var doctor = consultation.Users;

            var vm = new PrescriptionDetailsViewModel
            {
                ConsultationId = id,
                PrescriptionId = id, // Use consultation ID as prescription ID
                DatePrescription = consultation.DateConsultation ?? DateTime.Now,
                PatientName = patient != null ? (patient.Prenom + " " + patient.Nom).Trim() : string.Empty,
                PatientId = patient?.UserId ?? 0,
                DoctorName = doctor != null ? (doctor.Prenom + " " + doctor.Nom).Trim() : Session["UserName"] as string,
                DoctorSpecialite = doctor?.Specialite,
                Medications = consultation.Prescriptions.Select(p => new PrescriptionMedicationViewModel
                {
                    Medicament = p.Medicament,
                    Posologie = p.Posologie
                }).ToList()
            };

            return View(vm);
        }

        // GET: Doctor/ViewRecords/{id}
        /// <summary>
        /// View patient medical records for an appointment
        /// </summary>
        public ActionResult ViewRecords(int id)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var appointment = db.RendezVous
                .Include(r => r.Users)
                .FirstOrDefault(r => r.RendezVousId == id && r.MedecinId == doctorId.Value);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Rendez-vous introuvable";
                return RedirectToAction("Index");
            }

            var patientId = appointment.PatientId;

            // Redirect to patient history page
            return RedirectToAction("PatientHistory", new { patientId = patientId });
        }

        // GET: Doctor/CompleteConsultation/{consultationId}
        /// <summary>
        /// Mark consultation as complete and update appointment status to "Termine"
        /// </summary>
        public ActionResult CompleteConsultation(int id)
        {
            if (!CheckDoctorAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctorId = Session["UserID"] as int?;
            if (doctorId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var consultation = db.Consultations
                .Include(c => c.RendezVous)
                .FirstOrDefault(c => c.ConsultationId == id && c.MedecinId == doctorId.Value);

            if (consultation == null)
            {
                TempData["ErrorMessage"] = "Consultation introuvable";
                return RedirectToAction("Index");
            }

            // Update the appointment status to "Termine"
            var rendezVous = consultation.RendezVous;
            if (rendezVous != null)
            {
                rendezVous.Statut = "Termine";
                db.SaveChanges();
                TempData["SuccessMessage"] = "Consultation terminée avec succès. Le statut du rendez-vous a été mis à jour.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
