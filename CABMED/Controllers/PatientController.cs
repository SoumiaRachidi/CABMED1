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
                    PopulatePatientInfo(model);
                    model.Status = "En attente";
                    model.DateDemande = DateTime.Now;

                    var rendezVousId = CreatePendingRendezVous(model);
                    if (rendezVousId == null)
                    {
                        ModelState.AddModelError("", "Impossible de créer le rendez-vous car aucun créneau n'est disponible.");
                    }
                    else
                    {
                        model.RendezVousId = rendezVousId.Value;
                        AppointmentRequestRepository.Add(model);

                        TempData["SuccessMessage"] = "Votre demande de rendez-vous a été soumise avec succès. Notre secrétariat vous contactera sous peu.";
                        TempData["LastRequestId"] = model.RequestId;
                        return RedirectToAction("AppointmentStatus");
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Une erreur est survenue lors de l'envoi de votre demande. Veuillez réessayer.");
                }
            }

            ViewBag.PatientInfo = BuildPatientInfo();

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

            return View();
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
            target.RequestStatus = request.Status;
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
            return Convert.ToInt32(Session["UserId"] ?? 0);
        }

        private int GetUpcomingAppointmentsCount(IEnumerable<AppointmentViewModel> appointments)
        {
            return appointments.Count(a => HasStatus(a, "Approuvé", "Confirmé") && a.DateHeureDebut >= DateTime.Now);
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
                .Where(a => HasStatus(a, "Approuvé", "Confirmé") && a.DateHeureDebut > DateTime.MinValue)
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

        private int? CreatePendingRendezVous(AppointmentRequestViewModel request)
        {
            if (request.PatientId == 0)
            {
                return null;
            }

            var start = ResolvePreferredStart(request);
            var rendezVous = new RendezVous
            {
                PatientId = request.PatientId,
                MedecinId = request.PatientId,
                DateHeureDebut = start,
                DateHeureFin = start.AddMinutes(30),
                Statut = "En attente",
                Motif = request.SymptomsDescription
            };

            _db.RendezVous.Add(rendezVous);
            _db.SaveChanges();
            return rendezVous.RendezVousId;
        }

        private static DateTime ResolvePreferredStart(AppointmentRequestViewModel request)
        {
            var date = request.PreferredDate ?? DateTime.Today;
            var time = request.PreferredTime ?? new TimeSpan(9, 0, 0);
            var start = date.Date + time;

            if (start < DateTime.Now)
            {
                start = DateTime.Now.AddHours(1);
            }

            return start;
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
