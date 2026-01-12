using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CABMED.Models;
using CABMED.Services;
using CABMED.ViewModels;

namespace CABMED.Controllers
{
    public class PatientController : Controller
    {
        private readonly CabinetEntities _db = new CabinetEntities();

        // GET: Patient/Index
        public ActionResult Index()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            var patientAppointments = GetPatientAppointments();

            ViewBag.UpcomingAppointments = GetUpcomingAppointmentsCount(patientAppointments);
            ViewBag.CompletedAppointments = GetCompletedAppointmentsCount(patientAppointments);
            ViewBag.PendingResults = GetPendingResultsCount(patientAppointments);
            ViewBag.NextAppointmentDate = GetNextAppointmentDate(patientAppointments);
            ViewBag.PrimaryDoctor = GetPrimaryDoctor(patientAppointments);
            ViewBag.RecentActivity = GetRecentActivity(patientAppointments);

            return View();
        }

        // GET: Patient/BookAppointment
        public ActionResult BookAppointment()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new AppointmentRequestViewModel();

            ViewBag.PatientInfo = BuildPatientInfo();
            PopulateDropdownLists();

            return View(model);
        }

        // POST: Patient/BookAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookAppointment(AppointmentRequestViewModel model)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var patientId = GetPatientId();
                    if (patientId == 0)
                    {
                        ModelState.AddModelError("", "Session utilisateur invalide. Veuillez vous reconnecter.");
                        ViewBag.PatientInfo = BuildPatientInfo();
                        PopulateDropdownLists();
                        return View(model);
                    }

                    PopulatePatientInfo(model);
                    
                    if (string.IsNullOrWhiteSpace(model.SymptomsDescription))
                    {
                        ModelState.AddModelError("SymptomsDescription", "La description des symptômes est obligatoire.");
                        ViewBag.PatientInfo = BuildPatientInfo();
                        PopulateDropdownLists();
                        return View(model);
                    }

                    model.Status = "En attente";
                    model.DateDemande = DateTime.Now;

                    // Don't create RendezVous record yet - it will be created when secretary approves
                    model.RendezVousId = null;
                    AppointmentRequestRepository.Add(model);

                    TempData["SuccessMessage"] = "Votre demande de rendez-vous a été soumise avec succès. Notre secrétariat vous contactera sous peu.";
                    TempData["LastRequestId"] = model.RequestId;
                    return RedirectToAction("AppointmentStatus");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Une erreur est survenue lors de l'envoi de votre demande. Veuillez réessayer.");
                }
            }

            ViewBag.PatientInfo = BuildPatientInfo();
            PopulateDropdownLists();

            return View(model);
        }

        // GET: Patient/AppointmentStatus
        public ActionResult AppointmentStatus()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData["LastRequestId"] is int lastRequestId)
            {
                ViewBag.AppointmentRequest = AppointmentRequestRepository.GetById(lastRequestId);
                TempData.Keep("LastRequestId");
            }

            return View();
        }

        // GET: Patient/MyAppointments
        public ActionResult MyAppointments()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var appointments = GetPatientAppointments();
            return View(appointments);
        }

        // GET: Patient/MedicalHistory
        public ActionResult MedicalHistory()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patientId = GetPatientId();
            if (patientId == 0)
            {
                TempData["ErrorMessage"] = "Session invalide";
                return RedirectToAction("Index");
            }

            // Get all consultations for this patient
            var consultations = _db.Consultations
                .Include(c => c.RendezVous)
                .Include(c => c.Users) // Doctor
                .Include(c => c.Prescriptions)
                .Include(c => c.ResultatsExamens)
                .Where(c => c.RendezVous.PatientId == patientId)
                .OrderByDescending(c => c.DateConsultation)
                .ToList();

            return View(consultations);
        }

        // GET: Patient/MyPrescriptions
        /// <summary>
        /// Display list of all prescriptions for the logged-in patient
        /// </summary>
        public ActionResult MyPrescriptions()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patientId = GetPatientId();
            if (patientId == 0)
            {
                TempData["ErrorMessage"] = "Session invalide";
                return RedirectToAction("Index");
            }

            // Get all consultations for this patient with their prescriptions
            var prescriptions = _db.Consultations
                .Include(c => c.RendezVous)
                .Include(c => c.Users) // Doctor
                .Include(c => c.Prescriptions)
                .Where(c => c.RendezVous.PatientId == patientId)
                .OrderByDescending(c => c.DateConsultation)
                .ToList();

            return View(prescriptions);
        }

        // GET: Patient/EmergencyContact
        /// <summary>
        /// Display emergency contact information
        /// </summary>
        public ActionResult EmergencyContact()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            // Get patient's guardian/emergency contact info
            var patientId = GetPatientId();
            if (patientId > 0)
            {
                var patient = _db.Users.FirstOrDefault(u => u.UserId == patientId);
                if (patient != null)
                {
                    var guardianInfo = ParseGuardianInfo(patient.AntecedentsMedicaux);
                    ViewBag.GuardianName = guardianInfo.GuardianName;
                    ViewBag.GuardianPhone = guardianInfo.GuardianPhone;
                    ViewBag.GuardianRelation = guardianInfo.GuardianRelation;
                }
            }

            return View();
        }

        // GET: Patient/UpdateProfile
        /// <summary>
        /// Display profile update form
        /// </summary>
        public ActionResult UpdateProfile()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patientId = GetPatientId();
            if (patientId == 0)
            {
                TempData["ErrorMessage"] = "Session invalide";
                return RedirectToAction("Index");
            }

            var patient = _db.Users.FirstOrDefault(u => u.UserId == patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Utilisateur introuvable";
                return RedirectToAction("Index");
            }

            // Parse antecedents to extract guardian info and address
            var guardianInfo = ParseGuardianInfo(patient.AntecedentsMedicaux);

            var model = new UpdateProfileViewModel
            {
                UserId = patient.UserId,
                Nom = patient.Nom,
                Prenom = patient.Prenom,
                Email = patient.Email,
                Telephone = patient.Telephone,
                DateNaissance = patient.DateNaissance,
                AntecedentsMedicaux = guardianInfo.MedicalHistory,
                Adresse = guardianInfo.Adresse,
                Ville = guardianInfo.Ville,
                CodePostal = guardianInfo.CodePostal,
                GuardianName = guardianInfo.GuardianName,
                GuardianPhone = guardianInfo.GuardianPhone,
                GuardianRelation = guardianInfo.GuardianRelation
            };

            return View(model);
        }

        // POST: Patient/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UpdateProfileViewModel model)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patientId = GetPatientId();
            if (patientId == 0 || model.UserId != patientId)
            {
                TempData["ErrorMessage"] = "Accès non autorisé";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var patient = _db.Users.FirstOrDefault(u => u.UserId == patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Utilisateur introuvable";
                return RedirectToAction("Index");
            }

            // Update basic info
            patient.Nom = model.Nom;
            patient.Prenom = model.Prenom;
            patient.Email = model.Email;
            patient.Telephone = model.Telephone;
            patient.DateNaissance = model.DateNaissance;

            // Encode guardian info and address into AntecedentsMedicaux field
            patient.AntecedentsMedicaux = EncodePatientInfo(model);

            try
            {
                _db.SaveChanges();

                // Update session
                Session["UserName"] = model.Prenom + " " + model.Nom;
                Session["UserEmail"] = model.Email;
                Session["UserPhone"] = model.Telephone;

                TempData["SuccessMessage"] = "Profil mis à jour avec succès";
                return RedirectToAction("UpdateProfile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de la mise à jour: " + ex.Message;
                return View(model);
            }
        }

        // Helper method to parse guardian info from AntecedentsMedicaux
        private (string MedicalHistory, string Adresse, string Ville, string CodePostal, 
                 string GuardianName, string GuardianPhone, string GuardianRelation) ParseGuardianInfo(string antecedents)
        {
            if (string.IsNullOrEmpty(antecedents))
            {
                return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            var medicalHistory = string.Empty;
            var adresse = string.Empty;
            var ville = string.Empty;
            var codePostal = string.Empty;
            var guardianName = string.Empty;
            var guardianPhone = string.Empty;
            var guardianRelation = string.Empty;

            // Simple parsing using delimiters
            var lines = antecedents.Split(new[] { "|||" }, StringSplitOptions.None);
            
            if (lines.Length > 0) medicalHistory = lines[0];
            if (lines.Length > 1) adresse = lines[1];
            if (lines.Length > 2) ville = lines[2];
            if (lines.Length > 3) codePostal = lines[3];
            if (lines.Length > 4) guardianName = lines[4];
            if (lines.Length > 5) guardianPhone = lines[5];
            if (lines.Length > 6) guardianRelation = lines[6];

            return (medicalHistory, adresse, ville, codePostal, guardianName, guardianPhone, guardianRelation);
        }

        // Helper method to encode patient info
        private string EncodePatientInfo(UpdateProfileViewModel model)
        {
            return string.Join("|||",
                model.AntecedentsMedicaux ?? string.Empty,
                model.Adresse ?? string.Empty,
                model.Ville ?? string.Empty,
                model.CodePostal ?? string.Empty,
                model.GuardianName ?? string.Empty,
                model.GuardianPhone ?? string.Empty,
                model.GuardianRelation ?? string.Empty
            );
        }

        // GET: Patient/TestResults
        /// <summary>
        /// Display test results for the patient
        /// </summary>
        public ActionResult TestResults()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patientId = GetPatientId();
            if (patientId == 0)
            {
                TempData["ErrorMessage"] = "Session invalide";
                return RedirectToAction("Index");
            }

            // Get all test results for this patient
            var results = _db.ResultatsExamens
                .Include(r => r.Consultations.RendezVous)
                .Include(r => r.Consultations.Users)
                .Where(r => r.Consultations.RendezVous.PatientId == patientId)
                .OrderByDescending(r => r.DateExamen)
                .ToList();

            return View(results);
        }

        // GET: Patient/ViewPrescription/{consultationId}
        /// <summary>
        /// Display prescription document for a specific consultation
        /// </summary>
        public ActionResult ViewPrescription(int id)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patientId = GetPatientId();
            if (patientId == 0)
            {
                TempData["ErrorMessage"] = "Session invalide";
                return RedirectToAction("Index");
            }

            var consultation = _db.Consultations
                .Include(c => c.RendezVous.Users)
                .Include(c => c.Users)
                .Include(c => c.Prescriptions)
                .FirstOrDefault(c => c.ConsultationId == id && c.RendezVous.PatientId == patientId);

            if (consultation == null)
            {
                TempData["ErrorMessage"] = "Ordonnance introuvable";
                return RedirectToAction("Index");
            }

            var patient = consultation.RendezVous.Users;
            var doctor = consultation.Users;

            var vm = new PrescriptionDetailsViewModel
            {
                ConsultationId = id,
                PrescriptionId = id,
                DatePrescription = consultation.DateConsultation ?? DateTime.Now,
                PatientName = patient != null ? (patient.Prenom + " " + patient.Nom).Trim() : string.Empty,
                PatientId = patient?.UserId ?? 0,
                DoctorName = doctor != null ? (doctor.Prenom + " " + doctor.Nom).Trim() : "Médecin",
                DoctorSpecialite = doctor?.Specialite,
                Medications = consultation.Prescriptions.Select(p => new PrescriptionMedicationViewModel
                {
                    Medicament = p.Medicament,
                    Posologie = p.Posologie
                }).ToList()
            };

            return View("~/Views/Doctor/ViewPrescription.cshtml", vm);
        }

        // GET: Patient/DebugSession (Debug helper - remove in production)
        public ActionResult DebugSession()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }
            
            var debugInfo = $"Session Debug Info:<br/>" +
                           $"UserID: {Session["UserID"]}<br/>" +
                           $"UserId: {Session["UserId"]}<br/>" +
                           $"UserName: {Session["UserName"]}<br/>" +
                           $"UserFirstName: {Session["UserFirstName"]}<br/>" +
                           $"UserLastName: {Session["UserLastName"]}<br/>" +
                           $"UserEmail: {Session["UserEmail"]}<br/>" +
                           $"UserPhone: {Session["UserPhone"]}<br/>" +
                           $"Role: {Session["Role"]}<br/>" +
                           $"PatientId from GetPatientId(): {GetPatientId()}";
            
            return Content(debugInfo);
        }

        // GET: Patient/ResetData (Debug helper - remove in production)
        public ActionResult ResetData()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }
            
            try
            {
                AppointmentRequestRepository.Reset();
                return Content("Repository data cleared successfully. <a href='/Patient'>Go back to Patient Dashboard</a>");
            }
            catch (Exception ex)
            {
                return Content("Error resetting data: " + ex.Message);
            }
        }

        private object BuildPatientInfo()
        {
            return new
            {
                Nom = Session["UserLastName"] as string ?? Session["UserName"] as string ?? "Patient",
                Prenom = Session["UserFirstName"] as string ?? string.Empty,
                Telephone = Session["UserPhone"] as string ?? "Téléphone non renseigné"
            };
        }

        private void PopulatePatientInfo(AppointmentRequestViewModel model)
        {
            model.PatientId = GetPatientId();
            model.PatientNom = Session["UserLastName"] as string ?? Session["UserName"] as string ?? "Patient";
            model.PatientPrenom = Session["UserFirstName"] as string ?? string.Empty;
            model.PatientTelephone = Session["UserPhone"] as string ?? "Téléphone";
            model.PatientEmail = Session["UserEmail"] as string ?? "Email";
        }

        private void PopulateDropdownLists()
        {
            // Populate specialties dropdown
            var specialties = new List<SelectListItem>
            {
                new SelectListItem { Value = "Médecine générale", Text = "Médecine générale" },
                new SelectListItem { Value = "Cardiologie", Text = "Cardiologie" },
                new SelectListItem { Value = "Dermatologie", Text = "Dermatologie" },
                new SelectListItem { Value = "Pédiatrie", Text = "Pédiatrie" },
                new SelectListItem { Value = "Gynécologie", Text = "Gynécologie" },
                new SelectListItem { Value = "Orthopédie", Text = "Orthopédie" },
                new SelectListItem { Value = "Ophtalmologie", Text = "Ophtalmologie" },
                new SelectListItem { Value = "ORL", Text = "ORL" },
                new SelectListItem { Value = "Neurologie", Text = "Neurologie" },
                new SelectListItem { Value = "Psychiatrie", Text = "Psychiatrie" }
            };

            // Populate urgency levels dropdown
            var urgencies = new List<SelectListItem>
            {
                new SelectListItem { Value = "Faible", Text = "Faible - Consultation de routine" },
                new SelectListItem { Value = "Moyen", Text = "Moyen - Dans les prochains jours" },
                new SelectListItem { Value = "Élevé", Text = "Élevé - Urgent" }
            };

            ViewBag.Specialties = new SelectList(specialties, "Value", "Text");
            ViewBag.Urgencies = new SelectList(urgencies, "Value", "Text");
        }

        private List<AppointmentViewModel> GetPatientAppointments()
        {
            var patientId = GetPatientId();
            if (patientId == 0)
            {
                return new List<AppointmentViewModel>();
            }

            var confirmedAppointments = GetDatabaseAppointments(patientId);
            var confirmedMap = confirmedAppointments
                .Where(a => a.RendezVousId != 0)
                .ToDictionary(a => a.RendezVousId, a => a);

            var requests = AppointmentRequestRepository.GetByPatient(patientId);
            var pendingAppointments = new List<AppointmentViewModel>();

            foreach (var request in requests)
            {
                if (request.RendezVousId.HasValue && confirmedMap.TryGetValue(request.RendezVousId.Value, out var existing))
                {
                    ApplyRequestMetadata(existing, request);
                }
                else
                {
                    pendingAppointments.Add(MapRequestToViewModel(request));
                }
            }

            return confirmedAppointments
                .Concat(pendingAppointments)
                .OrderByDescending(a => a.DateDemande ?? a.DateHeureDebut)
                .ThenByDescending(a => a.DateHeureDebut)
                .ToList();
        }

        private static void ApplyRequestMetadata(AppointmentViewModel target, AppointmentRequestViewModel request)
        {
            // IMPORTANT: Store the original database status FIRST before any overwrites
            var originalDatabaseStatus = target.Statut;
            
 target.DateDemande = request.DateDemande;
 target.UrgencyLevel = request.UrgencyLevel;
          target.PreferredSpecialty = request.PreferredSpecialty;
     target.SymptomsDescription = request.SymptomsDescription;
  target.AssignedDoctor = request.AssignedDoctor;
            target.AssignedDoctorId = request.AssignedDoctorId;
       target.SecretaryComments = request.SecretaryComments;
     target.ProcessedDate = request.ProcessedDate;
 target.ProcessedBy = request.ProcessedBy;

            if (request.ConfirmedStartDateTime.HasValue)
 {
    target.DateHeureDebut = request.ConfirmedStartDateTime.Value;
target.DateHeureFin = request.ConfirmedEndDateTime ?? request.ConfirmedStartDateTime.Value.AddMinutes(30);
         }
   
            // FIX: Always prioritize the database status over the request status
   // Database has the real-time status (Confirme, Termine, Annule)
   // Request status is stale and outdated after secretary approval
            if (!string.IsNullOrEmpty(originalDatabaseStatus))
    {
          // Use the actual database status - it's authoritative
         target.RequestStatus = originalDatabaseStatus;
       target.Statut = originalDatabaseStatus;
            }
    else
            {
   // Only use request status for pending requests not yet in RendezVous table
           target.RequestStatus = request.Status;
  }
        }

        private static AppointmentViewModel MapRequestToViewModel(AppointmentRequestViewModel request)
        {
            var start = request.ConfirmedStartDateTime ?? request.DateDemande;
            var end = request.ConfirmedEndDateTime ?? start.AddMinutes(30);

            return new AppointmentViewModel
            {
                RendezVousId = request.RequestId,
                PatientId = request.PatientId,
                PatientNom = request.PatientNom,
                PatientPrenom = request.PatientPrenom,
                PatientTelephone = request.PatientTelephone,
                Motif = request.SymptomsDescription,
                Statut = request.Status,
                RequestStatus = request.Status,
                DateDemande = request.DateDemande,
                UrgencyLevel = request.UrgencyLevel,
                PreferredSpecialty = request.PreferredSpecialty,
                SymptomsDescription = request.SymptomsDescription,
                AssignedDoctor = request.AssignedDoctor,
                AssignedDoctorId = request.AssignedDoctorId,
                SecretaryComments = request.SecretaryComments,
                ProcessedDate = request.ProcessedDate,
                ProcessedBy = request.ProcessedBy,
                DateHeureDebut = start,
                DateHeureFin = end
            };
        }

        private List<AppointmentViewModel> GetDatabaseAppointments(int patientId)
        {
            return _db.RendezVous
                .Include(r => r.Users)
                .Include(r => r.Users1)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.DateHeureDebut)
                .Select(r => new AppointmentViewModel
                {
                    RendezVousId = r.RendezVousId,
                    PatientId = r.PatientId,
                    PatientNom = r.Users != null ? r.Users.Nom : null,
                    PatientPrenom = r.Users != null ? r.Users.Prenom : null,
                    PatientTelephone = r.Users != null ? r.Users.Telephone : null,
                    Motif = r.Motif,
                    Statut = r.Statut,
                    RequestStatus = r.Statut,
                    DateDemande = r.DateHeureDebut,
                    DateHeureDebut = r.DateHeureDebut,
                    DateHeureFin = r.DateHeureFin,
                    SymptomsDescription = r.Motif,
                    AssignedDoctor = r.Users1 != null ? ((r.Users1.Prenom ?? string.Empty) + " " + (r.Users1.Nom ?? string.Empty)).Trim() : null,
                    AssignedDoctorId = r.MedecinId
                })
                .ToList();
        }

        private static string BuildAppointmentKey(DateTime? start, int? doctorId)
        {
            return start.HasValue ? $"{start.Value:o}|{doctorId.GetValueOrDefault()}" : null;
        }

        private int GetPatientId()
        {
            var sessionValue = Session["UserID"] ?? Session["UserId"];
            if (sessionValue == null)
            {
                return 0;
            }
            
            if (int.TryParse(sessionValue.ToString(), out int patientId))
            {
                return patientId;
            }
            
            return 0;
        }

        private int GetUpcomingAppointmentsCount(IEnumerable<AppointmentViewModel> appointments)
        {
            return appointments.Count(a => HasStatus(a, "Confirmé", "En attente") && a.DateHeureDebut >= DateTime.Now);
        }

        private int GetCompletedAppointmentsCount(IEnumerable<AppointmentViewModel> appointments)
        {
            return appointments.Count(IsCompleted);
        }

        private int GetPendingResultsCount(IEnumerable<AppointmentViewModel> appointments)
        {
            return appointments.Count(a => HasStatus(a, "En attente"));
        }

        private string GetNextAppointmentDate(IEnumerable<AppointmentViewModel> appointments)
        {
            var next = appointments
                .Where(a => HasStatus(a, "Confirmé") && a.DateHeureDebut > DateTime.MinValue)
                .OrderBy(a => a.DateHeureDebut)
                .FirstOrDefault();

            return next?.DateHeureDebut.ToString("dd/MM/yyyy à HH:mm") ?? "Aucun prévu";
        }

        private string GetPrimaryDoctor(IEnumerable<AppointmentViewModel> appointments)
        {
            return appointments
                .Where(a => !string.IsNullOrWhiteSpace(a.AssignedDoctor))
                .OrderByDescending(a => a.DateHeureDebut)
                .Select(a => a.AssignedDoctor)
                .FirstOrDefault() ?? "Non défini";
        }

        private List<dynamic> GetRecentActivity(IEnumerable<AppointmentViewModel> appointments)
        {
            return appointments
                .OrderByDescending(a => a.DateDemande ?? a.DateHeureDebut)
                .Take(5)
                .Select(a => new
                {
                    Type = GetStatusLabel(a),
                    Description = GetActivityDescription(a),
                    Date = (a.DateDemande ?? a.DateHeureDebut).ToString("dd/MM/yyyy à HH:mm")
                })
                .Cast<dynamic>()
                .ToList();
        }

        private static string GetActivityDescription(AppointmentViewModel appointment)
        {
            return string.IsNullOrWhiteSpace(appointment.SymptomsDescription)
                ? appointment.Motif
                : appointment.SymptomsDescription;
        }

        private static string GetStatusLabel(AppointmentViewModel appointment)
        {
            return appointment.RequestStatus ?? appointment.Statut ?? "Rendez-vous";
        }

        private static bool HasStatus(AppointmentViewModel appointment, params string[] statuses)
        {
            var label = GetStatusLabel(appointment);
            return statuses.Any(status => label.Equals(status, StringComparison.InvariantCultureIgnoreCase));
        }

        private static bool IsCompleted(AppointmentViewModel appointment)
        {
            if (HasStatus(appointment, "Terminé"))
            {
                return true;
            }

            return HasStatus(appointment, "Confirmé") && appointment.DateHeureDebut < DateTime.Now;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
