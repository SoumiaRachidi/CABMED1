using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CABMED.Models;
using CABMED.ViewModels;
using CABMED.Services;

namespace CABMED.Controllers
{
    public class SecretaryController : Controller
    {
        private readonly CabinetEntities _db = new CabinetEntities();

        // GET: Secretary/Index
        public ActionResult Index()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            var allRequests = AppointmentRequestRepository.GetAll();
            ViewBag.PendingRequests = GetPendingRequestsCount();
            ViewBag.ApprovedToday = GetApprovedTodayCount();
            ViewBag.TotalRequestsToday = GetTotalRequestsTodayCount();
            ViewBag.NextPendingRequest = allRequests
              .Where(r => string.Equals(r.Status, "En attente", StringComparison.InvariantCultureIgnoreCase))
              .OrderByDescending(r => r.DateDemande)
              .FirstOrDefault();

            return View();
        }

        // GET: Secretary/AppointmentRequests
        public ActionResult AppointmentRequests()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            var requests = AppointmentRequestRepository.GetAll();
            return View(requests);
        }

        // GET: Secretary/GetDoctors
        public ActionResult GetDoctors()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return new HttpStatusCodeResult(403);
            }

            var doctors = _db.Users
              .Where(u => u.Role != null && u.Role.ToLower() == "medecin")
              .OrderBy(u => u.Nom)
              .ThenBy(u => u.Prenom)
              .Select(u => new
              {
                  id = u.UserId,
                  name = ((u.Prenom ?? string.Empty) + " " + (u.Nom ?? string.Empty)).Trim(),
                  specialite = u.Specialite
              })
              .ToList();

            return Json(doctors, JsonRequestBehavior.AllowGet);
        }

        // POST: Secretary/ApproveRequest
        [HttpPost]
        public ActionResult ApproveRequest(int requestId, string appointmentDate, string appointmentTime, int doctorId, string comments)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return Json(new { success = false, message = "Accès non autorisé" });
            }

            try
            {
                // Validate doctorId first
                if (doctorId <= 0)
                {
                    return Json(new { success = false, message = "ID du médecin invalide. Veuillez sélectionner un médecin." });
                }

                if (!TryParseAppointmentDate(appointmentDate, out var parsedDate) ||
                  !TryParseAppointmentTime(appointmentTime, out var parsedTime))
                {
                    return Json(new { success = false, message = "Date ou heure de rendez-vous invalide" });
                }

                var request = AppointmentRequestRepository.GetById(requestId);
                if (request == null)
                {
                    return Json(new { success = false, message = "Demande introuvable (ID: " + requestId + ")" });
                }

                var doctor = _db.Users.FirstOrDefault(u => u.UserId == doctorId && u.Role != null && u.Role.ToLower() == "medecin");
                if (doctor == null)
                {
                    return Json(new { success = false, message = "Médecin introuvable (ID: " + doctorId + "). Veuillez sélectionner un autre médecin." });
                }

                var patient = _db.Users.FirstOrDefault(u => u.UserId == request.PatientId);
                if (patient == null)
                {
                    return Json(new { success = false, message = "Patient introuvable (ID: " + request.PatientId + ")" });
                }

                var startDateTime = parsedDate.Date + parsedTime;
                var endDateTime = startDateTime.AddMinutes(30);

                RendezVous rendezVous = null;
                if (request.RendezVousId.HasValue)
                {
                    rendezVous = _db.RendezVous.FirstOrDefault(r => r.RendezVousId == request.RendezVousId.Value);
                }

                if (rendezVous == null)
                {
                    rendezVous = new RendezVous();
                    _db.RendezVous.Add(rendezVous);
                }

                rendezVous.PatientId = patient.UserId;
                rendezVous.MedecinId = doctor.UserId;
                rendezVous.DateHeureDebut = startDateTime;
                rendezVous.DateHeureFin = endDateTime;
                rendezVous.Statut = "Confirme";
                rendezVous.Motif = !string.IsNullOrWhiteSpace(request.SymptomsDescription) ? request.SymptomsDescription : "Consultation";

                _db.SaveChanges();

                var processedBy = Session["UserName"] as string ?? "Secrétaire";
                var doctorName = ((doctor.Prenom ?? string.Empty) + " " + (doctor.Nom ?? string.Empty)).Trim();
                AppointmentRequestRepository.ApproveRequest(requestId, parsedDate, parsedTime, doctor.UserId, doctorName, comments, processedBy, rendezVous.RendezVousId);

                return Json(new { success = true, message = "Demande approuvée avec succès" });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException valEx)
            {
                var errorMessages = valEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullError = string.Join("; ", errorMessages);
                System.Diagnostics.Debug.WriteLine("Validation error in ApproveRequest: " + fullError);
                return Json(new { success = false, message = "Erreur de validation: " + fullError });
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.InnerException?.Message ?? dbEx.InnerException?.Message ?? dbEx.Message;
                System.Diagnostics.Debug.WriteLine("Database error in ApproveRequest: " + innerMessage);
                return Json(new { success = false, message = "Erreur base de données: " + innerMessage });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine("Error in ApproveRequest: " + innerMessage); 
                return Json(new { success = false, message = "Erreur: " + innerMessage });
            }
        }

        // POST: Secretary/DeclineRequest
        [HttpPost]
        public ActionResult DeclineRequest(int requestId, string reason)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return Json(new { success = false, message = "Accès non autorisé" });
            }

            try
            {
                var request = AppointmentRequestRepository.GetById(requestId);
                if (request == null)
                {
                    return Json(new { success = false, message = "Demande introuvable" });
                }

                if (request.RendezVousId.HasValue)
                {
                    var rendezVous = _db.RendezVous.FirstOrDefault(r => r.RendezVousId == request.RendezVousId.Value);
                    if (rendezVous != null)
                    {
                        rendezVous.Statut = "Refuse";
                        _db.SaveChanges();
                    }
                }

                var processedBy = Session["UserName"] as string ?? "Secrétaire";
                var success = AppointmentRequestRepository.DeclineRequest(requestId, reason, processedBy);
                if (!success)
                {
                    return Json(new { success = false, message = "Demande introuvable" });
                }

                return Json(new { success = true, message = "Demande refusée" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erreur lors du refus" });
            }
        }

        // GET: Secretary/ViewRequest
        public ActionResult ViewRequest(int id)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            var request = AppointmentRequestRepository.GetById(id);
            if (request == null)
            {
                return HttpNotFound();
            }

            return View(request);
        }

        // GET: Secretary/ViewRequestDetails
        public ActionResult ViewRequestDetails(int id)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            var request = AppointmentRequestRepository.GetById(id);
            if (request == null)
            {
                return HttpNotFound();
            }

            // Get patient details
            var patient = _db.Users.FirstOrDefault(u => u.UserId == request.PatientId);
            if (patient != null)
            {
                ViewBag.PatientFullName = ((patient.Prenom ?? string.Empty) + " " + (patient.Nom ?? string.Empty)).Trim();
                ViewBag.PatientEmail = patient.Email;
                ViewBag.PatientPhone = patient.Telephone;
                ViewBag.PatientDateNaissance = patient.DateNaissance;
            }

            // Get assigned doctor details if available
            if (!string.IsNullOrWhiteSpace(request.AssignedDoctor))
            {
                ViewBag.AssignedDoctorName = request.AssignedDoctor;
            }

            // Get appointment details if linked
            if (request.RendezVousId.HasValue)
            {
                var rendezVous = _db.RendezVous
                    .Include("Users")    // Patient
                    .Include("Users1")   // Doctor
                    .FirstOrDefault(r => r.RendezVousId == request.RendezVousId.Value);

                if (rendezVous != null)
                {
                    ViewBag.AppointmentDate = rendezVous.DateHeureDebut;
                    ViewBag.AppointmentEndDate = rendezVous.DateHeureFin;
                    ViewBag.AppointmentStatus = rendezVous.Statut;
                    
                    var doctor = rendezVous.Users1;
                    if (doctor != null)
                    {
                        ViewBag.DoctorFullName = ((doctor.Prenom ?? string.Empty) + " " + (doctor.Nom ?? string.Empty)).Trim();
                        ViewBag.DoctorSpecialite = doctor.Specialite;
                    }
                }
            }

            ViewBag.UserName = Session["UserName"] as string;
            return View(request);
        }

        // GET: Secretary/ManageAppointments
        public ActionResult ManageAppointments()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            // Get all appointments from database
            var appointments = _db.RendezVous
                .Include("Users")    // Patient
                .Include("Users1")   // Doctor
                .OrderByDescending(r => r.DateHeureDebut)
                .ToList();

            return View(appointments);
        }

        // GET: Secretary/TodayAppointments
        public ActionResult TodayAppointments()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var appointments = _db.RendezVous
                .Include("Users")    // Patient
                .Include("Users1")   // Doctor
                .Where(r => r.DateHeureDebut >= today && r.DateHeureDebut < tomorrow)
                .OrderBy(r => r.DateHeureDebut)
                .ToList();

            return View(appointments);
        }

        // GET: Secretary/CreateAppointment
        public ActionResult CreateAppointment()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            // Get all patients
            ViewBag.Patients = _db.Users
                .Where(u => u.Role != null && u.Role.ToLower() == "patient" && u.IsActive == true)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToList();

            // Get all doctors
            ViewBag.Doctors = _db.Users
                .Where(u => u.Role != null && u.Role.ToLower() == "medecin" && u.IsActive == true)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToList();

            return View();
        }

        // POST: Secretary/CreateAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAppointment(int patientId, int doctorId, DateTime appointmentDate, string appointmentTime, string motif)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // Validate inputs
                if (patientId <= 0)
                {
                    TempData["ErrorMessage"] = "Veuillez sélectionner un patient";
                    return RedirectToAction("CreateAppointment");
                }

                if (doctorId <= 0)
                {
                    TempData["ErrorMessage"] = "Veuillez sélectionner un médecin";
                    return RedirectToAction("CreateAppointment");
                }

                // Verify patient exists
                var patient = _db.Users.FirstOrDefault(u => u.UserId == patientId && u.Role.ToLower() == "patient");
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient introuvable";
                    return RedirectToAction("CreateAppointment");
                }

                // Verify doctor exists
                var doctor = _db.Users.FirstOrDefault(u => u.UserId == doctorId && u.Role.ToLower() == "medecin");
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Médecin introuvable";
                    return RedirectToAction("CreateAppointment");
                }

                if (!TryParseAppointmentTime(appointmentTime, out var parsedTime))
                {
                    TempData["ErrorMessage"] = "Heure invalide. Format attendu: HH:mm";
                    return RedirectToAction("CreateAppointment");
                }

                var startDateTime = appointmentDate.Date + parsedTime;
                var endDateTime = startDateTime.AddMinutes(30);

                // Check for conflicts
                var hasConflict = _db.RendezVous.Any(r =>
                    r.MedecinId == doctorId &&
                    ((r.DateHeureDebut <= startDateTime && r.DateHeureFin > startDateTime) ||
                     (r.DateHeureDebut < endDateTime && r.DateHeureFin >= endDateTime) ||
                     (r.DateHeureDebut >= startDateTime && r.DateHeureFin <= endDateTime)));

                if (hasConflict)
                {
                    TempData["ErrorMessage"] = "Le médecin a déjà un rendez-vous à cette heure";
                    return RedirectToAction("CreateAppointment");
                }

                var rendezVous = new RendezVous
                {
                    PatientId = patientId,
                    MedecinId = doctorId,
                    DateHeureDebut = startDateTime,
                    DateHeureFin = endDateTime,
                    Statut = "Confirme", // Without accent to match database
                    Motif = string.IsNullOrWhiteSpace(motif) ? "Consultation" : motif.Trim()
                };

                _db.RendezVous.Add(rendezVous);
                _db.SaveChanges();

                TempData["SuccessMessage"] = $"Rendez-vous créé avec succès pour {patient.Prenom} {patient.Nom} le {startDateTime:dd/MM/yyyy à HH:mm}";
                return RedirectToAction("ManageAppointments");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException valEx)
            {
                var errorMessages = valEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullError = string.Join("; ", errorMessages);
                System.Diagnostics.Debug.WriteLine("Validation error in CreateAppointment: " + fullError);
                TempData["ErrorMessage"] = "Erreur de validation: " + fullError;
                return RedirectToAction("CreateAppointment");
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.InnerException?.Message ?? dbEx.InnerException?.Message ?? dbEx.Message;
                System.Diagnostics.Debug.WriteLine("Database error in CreateAppointment: " + innerMessage);
                TempData["ErrorMessage"] = "Erreur base de données: " + innerMessage;
                return RedirectToAction("CreateAppointment");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine("Error in CreateAppointment: " + innerMessage);
                TempData["ErrorMessage"] = "Erreur: " + innerMessage;
                return RedirectToAction("CreateAppointment");
            }
        }

        // GET: Secretary/Reports
        public ActionResult Reports()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            // Calculate dates BEFORE the query (fix for EF LINQ to Entities error)
            var today = DateTime.Today;
            var weekAgo = today.AddDays(-7);
    var monthAgo = today.AddDays(-30);

       // Statistics
 ViewBag.TotalAppointmentsToday = _db.RendezVous.Count(r => System.Data.Entity.DbFunctions.TruncateTime(r.DateHeureDebut) == today);
 ViewBag.TotalAppointmentsWeek = _db.RendezVous.Count(r => r.DateHeureDebut >= weekAgo);
            ViewBag.TotalAppointmentsMonth = _db.RendezVous.Count(r => r.DateHeureDebut >= monthAgo);
   ViewBag.PendingRequests = GetPendingRequestsCount();
        ViewBag.TotalPatients = _db.Users.Count(u => u.Role != null && u.Role.ToLower() == "patient");
     ViewBag.TotalDoctors = _db.Users.Count(u => u.Role != null && u.Role.ToLower() == "medecin");

            return View();
        }

        // GET: Secretary/PatientHistory?patientId=123
        /// <summary>
        /// View patient medical history and records (Secretary access)
        /// </summary>
        public ActionResult PatientHistory(int patientId)
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            var patient = _db.Users.FirstOrDefault(u => u.UserId == patientId && u.Role.ToLower() == "patient");
            if (patient == null)
            {
                return HttpNotFound();
            }

            ViewBag.UserName = Session["UserName"] as string;
            ViewBag.PatientName = (patient.Prenom + " " + patient.Nom).Trim();
            ViewBag.PatientEmail = patient.Email;
            ViewBag.PatientPhone = patient.Telephone;
            ViewBag.PatientDateNaissance = patient.DateNaissance;
            ViewBag.PatientId = patient.UserId;

            // Get all consultations for this patient
            var consultations = _db.Consultations
                .Include("RendezVous")
                .Include("Users") // Doctor
                .Include("Prescriptions")
                .Include("ResultatsExamens")
                .Where(c => c.RendezVous.PatientId == patientId)
                .OrderByDescending(c => c.DateConsultation ?? c.RendezVous.DateHeureDebut)
                .ToList();

            return View(consultations);
        }

        // GET: Secretary/PatientsList
        /// <summary>
        /// List all patients for secretary to view records
        /// </summary>
        public ActionResult PatientsList()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            var patients = _db.Users
                .Where(u => u.Role != null && u.Role.ToLower() == "patient" && u.IsActive == true)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToList();

            return View(patients);
        }

        private int GetPendingRequestsCount()
        {
            return AppointmentRequestRepository.CountByStatus("En attente");
        }

        private int GetApprovedTodayCount()
        {
            return AppointmentRequestRepository.Count(r =>
              string.Equals(r.Status, "Approuvé", StringComparison.InvariantCultureIgnoreCase) &&
              r.ProcessedDate?.Date == DateTime.Today);
        }

        private int GetTotalRequestsTodayCount()
        {
            return AppointmentRequestRepository.Count(r => r.DateDemande.Date == DateTime.Today);
        }

        private static bool TryParseAppointmentDate(string value, out DateTime date)
        {
            if (DateTime.TryParseExact(value ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }

            return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
        }

        private static bool TryParseAppointmentTime(string value, out TimeSpan time)
        {
            if (TimeSpan.TryParseExact(value ?? string.Empty, new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out time))
            {
                return true;
            }

            return TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out time);
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