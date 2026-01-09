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
