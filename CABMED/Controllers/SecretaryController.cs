using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CABMED.Models;
using CABMED.Services;
using CABMED.ViewModels;

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
        public ActionResult ApproveRequest(int requestId, DateTime appointmentDate, TimeSpan appointmentTime, int doctorId, string comments)
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

                var doctor = _db.Users.FirstOrDefault(u => u.UserId == doctorId && u.Role != null && u.Role.ToLower() == "medecin");
                if (doctor == null)
                {
                    return Json(new { success = false, message = "Médecin invalide" });
                }

                var patient = _db.Users.FirstOrDefault(u => u.UserId == request.PatientId);
                if (patient == null)
                {
                    return Json(new { success = false, message = "Patient introuvable" });
                }

                var startDateTime = appointmentDate.Date + appointmentTime;
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
                rendezVous.Statut = "Confirmé";
                rendezVous.Motif = request.SymptomsDescription;

                _db.SaveChanges();

                var processedBy = Session["UserName"] as string ?? "Secrétaire";
                var doctorName = ((doctor.Prenom ?? string.Empty) + " " + (doctor.Nom ?? string.Empty)).Trim();
                AppointmentRequestRepository.ApproveRequest(requestId, appointmentDate, appointmentTime, doctor.UserId, doctorName, comments, processedBy, rendezVous.RendezVousId);

                return Json(new { success = true, message = "Demande approuvée avec succès" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erreur lors de l'approbation" });
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
                        rendezVous.Statut = "Refusé";
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
