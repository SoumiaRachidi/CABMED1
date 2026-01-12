using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using CABMED.Models;
using CABMED.ViewModels;

namespace CABMED.Controllers
{
    public class AdminController : Controller
    {
        private CabinetEntities db = new CabinetEntities();

        private bool CheckAdminAccess()
        {
            if (Session["Role"]?.ToString()?.ToLower() != "admin")
            {
                return false;
            }
            return true;
        }

        // GET: Admin/Index (Dashboard)
        public ActionResult Index()
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.TotalDoctors = db.Users.Count(u => u.Role.ToLower() == "medecin" && u.IsActive == true);
            ViewBag.TotalSecretaries = db.Users.Count(u => u.Role.ToLower() == "secretaire" && u.IsActive == true);
            ViewBag.TotalPatients = db.Users.Count(u => u.Role.ToLower() == "patient" && u.IsActive == true);

            return View();
        }

        // GET: Admin/StaffList
        public ActionResult StaffList()
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var staff = db.Users
                .Where(u => u.Role.ToLower() == "medecin" || u.Role.ToLower() == "secretaire")
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.Nom)
                .ToList();

            return View(staff);
        }

        // GET: Admin/CreateStaff
        public ActionResult CreateStaff()
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // POST: Admin/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(CreateStaffViewModel model)
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var existingUser = db.Users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());
                    if (existingUser != null)
                    {
                        ViewBag.Error = "Cet email est déjà utilisé dans le système.";
                        return View(model);
                    }

                    string sanitizedRole = model.Role.ToLower().Trim();

                    if (sanitizedRole != "medecin" && sanitizedRole != "secretaire")
                    {
                        ViewBag.Error = "Le rôle doit être 'medecin' ou 'secretaire'.";
                        return View(model);
                    }

                    string telephone = string.IsNullOrWhiteSpace(model.Telephone) ? null : model.Telephone.Trim();
                    string specialite = string.IsNullOrWhiteSpace(model.Specialite) ? null : model.Specialite.Trim();

                    var newStaff = new Users
                    {
                        Nom = model.Nom.Trim(),
                        Prenom = model.Prenom.Trim(),
                        Email = model.Email.Trim(),
                        Telephone = telephone,
                        Role = sanitizedRole,
                        Specialite = specialite,
                        PasswordHash = HashPassword(model.Password),
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(newStaff);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] = $"Le membre du personnel {model.Prenom} {model.Nom} a été créé avec succès.";
                    return RedirectToAction("StaffList");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.Error = "Une erreur s'est produite lors de la création du membre du personnel. Veuillez réessayer.";
                    return View(model);
                }
            }
        }

        // POST: Admin/DeleteStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStaff(int id)
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var staff = db.Users.Find(id);

                    if (staff == null)
                    {
                        TempData["ErrorMessage"] = "Le membre du personnel est introuvable.";
                        return RedirectToAction("StaffList");
                    }

                    if (staff.Role.ToLower() != "medecin" && staff.Role.ToLower() != "secretaire")
                    {
                        TempData["ErrorMessage"] = "Vous ne pouvez supprimer que des médecins ou des secrétaires.";
                        return RedirectToAction("StaffList");
                    }

                    staff.IsActive = false;
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] = $"Le membre du personnel {staff.Prenom} {staff.Nom} a été désactivé avec succès.";
                    return RedirectToAction("StaffList");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Une erreur s'est produite lors de la suppression. Veuillez réessayer.";
                    return RedirectToAction("StaffList");
                }
            }
        }

        // GET: Admin/PatientsList
        public ActionResult PatientsList()
        {
            // Allow both admin and secretary to access patient list
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "admin" && role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            var patients = db.Users
                .Where(u => u.Role.ToLower() == "patient")
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToList();

            return View(patients);
        }

        // GET: Admin/PatientDetails/5
        public ActionResult PatientDetails(int id)
        {
            // Allow both admin and secretary to access patient details
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "admin" && role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;

            var patient = db.Users.FirstOrDefault(u => u.UserId == id && u.Role.ToLower() == "patient");
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction("PatientsList");
            }

            // Get patient's appointments
            ViewBag.Appointments = db.RendezVous
                .Include("Users1") // Doctor
                .Where(r => r.PatientId == id)
                .OrderByDescending(r => r.DateHeureDebut)
                .Take(10)
                .ToList();

            // Get patient's consultations
            ViewBag.Consultations = db.Consultations
                .Include("RendezVous")
                .Include("Users") // Doctor
                .Include("Prescriptions")
                .Where(c => c.RendezVous.PatientId == id)
                .OrderByDescending(c => c.DateConsultation)
                .Take(10)
                .ToList();

            return View(patient);
        }

        // GET: Admin/EditStaff/5 (read-only by default)
        public ActionResult EditStaff(int id, bool? enableEdit)
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            var staff = db.Users.FirstOrDefault(u => u.UserId == id);
            if (staff == null || (staff.Role.ToLower() != "medecin" && staff.Role.ToLower() != "secretaire"))
            {
                TempData["ErrorMessage"] = "Employé introuvable.";
                return RedirectToAction("StaffList");
            }

            var vm = new EditStaffViewModel
            {
                UserId = staff.UserId,
                Nom = staff.Nom,
                Prenom = staff.Prenom,
                Email = staff.Email,
                Telephone = staff.Telephone,
                Specialite = staff.Specialite,
                Role = staff.Role
            };

            ViewBag.EnableEdit = enableEdit == true;
            return View(vm);
        }

        // POST: Admin/EditStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStaff(EditStaffViewModel model)
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.EnableEdit = true; // keep fields enabled on validation error
                return View(model);
            }

            var staff = db.Users.FirstOrDefault(u => u.UserId == model.UserId);
            if (staff == null || (staff.Role.ToLower() != "medecin" && staff.Role.ToLower() != "secretaire"))
            {
                TempData["ErrorMessage"] = "Employé introuvable.";
                return RedirectToAction("StaffList");
            }

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // email uniqueness check (if changed)
                    var emailNorm = (model.Email ?? string.Empty).Trim().ToLower();
                    if (!string.Equals(staff.Email, emailNorm, StringComparison.OrdinalIgnoreCase))
                    {
                        var exists = db.Users.Any(u => u.Email.ToLower() == emailNorm && u.UserId != staff.UserId);
                        if (exists)
                        {
                            ViewBag.EnableEdit = true;
                            ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                            return View(model);
                        }
                    }

                    staff.Nom = (model.Nom ?? string.Empty).Trim();
                    staff.Prenom = (model.Prenom ?? string.Empty).Trim();
                    staff.Email = emailNorm;
                    staff.Telephone = string.IsNullOrWhiteSpace(model.Telephone) ? null : model.Telephone.Trim();
                    staff.Specialite = string.IsNullOrWhiteSpace(model.Specialite) ? null : model.Specialite.Trim();

                    db.SaveChanges();
                    tx.Commit();

                    TempData["SuccessMessage"] = "Profil mis à jour avec succès.";
                    return RedirectToAction("EditStaff", new { id = model.UserId });
                }
                catch
                {
                    tx.Rollback();
                    ViewBag.EnableEdit = true;
                    ModelState.AddModelError("", "Erreur lors de la mise à jour du profil.");
                    return View(model);
                }
            }
        }

        private string HashPassword(string password)
        {
            return FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1");
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
