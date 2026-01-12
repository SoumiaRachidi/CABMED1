using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Security;
using CABMED.Models;
using CABMED.ViewModels;
using System.Globalization;
using System.Text;
using CABMED.Services;

namespace CABMED.Controllers
{
    public class AuthController : Controller
    {
        private CabinetEntities db = new CabinetEntities();

        // GET: Auth/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var email = (model.Email ?? string.Empty).Trim().ToLower();
                var password = model.Password ?? string.Empty;

                var user = db.Users.FirstOrDefault(u => u.Email.ToLower() == email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email ou mot de passe incorrect");
                    return View(model);
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Email ou mot de passe incorrect");
                    return View(model);
                }

                if (user.IsActive == false)
                {
                    ModelState.AddModelError("", "Votre compte est désactivé. Veuillez contacter l'administrateur.");
                    return View(model);
                }

                var roleNormalized = NormalizeRole(user.Role);

                Session["UserID"] = user.UserId;
                Session["UserId"] = user.UserId;
                Session["Role"] = roleNormalized;
                Session["UserName"] = (user.Prenom + " " + user.Nom).Trim();

                // Store additional user details for patient info
                Session["UserFirstName"] = user.Prenom ?? string.Empty;
                Session["UserLastName"] = user.Nom ?? string.Empty;
                Session["UserEmail"] = user.Email ?? string.Empty;
                Session["UserPhone"] = user.Telephone ?? string.Empty;

                string role = roleNormalized;

                switch (role)
                {
                    case "admin":
                        return RedirectToAction("Index", "Admin");
                    case "medecin":
                        return RedirectToAction("Index", "Doctor");
                    case "secretaire":
                        return RedirectToAction("Index", "Secretary");
                    case "patient":
                        return RedirectToAction("Index", "Patient");
                    default:
                        ModelState.AddModelError("", "Rôle utilisateur non reconnu");
                        return View(model);
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Une erreur s'est produite lors de la connexion. Veuillez réessayer.");
                return View(model);
            }
        }

        // GET: Auth/Register
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var email = (model.Email ?? string.Empty).Trim().ToLower();
                    var existingUser = db.Users.FirstOrDefault(u => u.Email.ToLower() == email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "Cet email est déjà utilisé");
                        return View(model);
                    }

                    var role = NormalizeRole(model.Role ?? "patient");
                    if (role != "admin" && role != "patient" && role != "secretaire" && role != "medecin")
                    {
                        role = "patient";
                    }

                    var newUser = new Users
                    {
                        Nom = (model.Nom ?? string.Empty).Trim(),
                        Prenom = (model.Prenom ?? string.Empty).Trim(),
                        Email = email,
                        Telephone = string.IsNullOrWhiteSpace(model.Telephone) ? null : model.Telephone.Trim(),
                        PasswordHash = HashPassword(model.Password ?? string.Empty),
                        Role = role,
                        DateNaissance = model.DateNaissance,
                        AntecedentsMedicaux = model.AntecedentsMedicaux,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges();

                    transaction.Commit();

                    // Auto-login right after register
                    Session["UserID"] = newUser.UserId;
                    Session["UserId"] = newUser.UserId;
                    Session["Role"] = role;
                    Session["UserName"] = (newUser.Prenom + " " + newUser.Nom).Trim();

                    // Store additional user details for patient info
                    Session["UserFirstName"] = newUser.Prenom ?? string.Empty;
                    Session["UserLastName"] = newUser.Nom ?? string.Empty;
                    Session["UserEmail"] = newUser.Email ?? string.Empty;
                    Session["UserPhone"] = newUser.Telephone ?? string.Empty;

                    if (role == "admin")
                        return RedirectToAction("Index", "Admin");
                    else if (role == "secretaire")
                        return RedirectToAction("Index", "Secretary");
                    else if (role == "medecin")
                        return RedirectToAction("Index", "Doctor");
                    else
                        return RedirectToAction("Index", "Patient");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Une erreur s'est produite lors de l'inscription. Veuillez réessayer.");
                    return View(model);
                }
            }
        }



        // GET: Auth/CheckUser (Debug helper)
        public ActionResult CheckUser(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();
            var user = db.Users.FirstOrDefault(u => u.Email.ToLower() == normalized);
            if (user == null)
            {
                return Content("User not found with email: " + normalized);
            }

            return Content($"User Found:<br/>ID: {user.UserId}<br/>Name: {user.Prenom} {user.Nom}<br/>Email: {user.Email}<br/>Role: {user.Role}<br/>IsActive: {user.IsActive}<br/>PasswordHash: {user.PasswordHash}<br/><br/>Test Hash for 'admin123': {HashPassword("admin123")}");
        }

        // GET: Auth/DebugRepository (Debug helper)
        public ActionResult DebugRepository()
        {
            try
            {
                var requests = CABMED.Services.AppointmentRequestRepository.GetAll();
                var info = $"Repository Status:<br/>" +
                          $"Total Requests: {requests.Count}<br/>" +
                          $"Storage Path: ~/App_Data/appointmentRequests.json<br/><br/>";
                
                if (requests.Count > 0)
                {
                    info += "Recent Requests:<br/>";
                    foreach (var req in requests.Take(5))
                    {
                        info += $"- ID: {req.RequestId}, Patient: {req.PatientNom} {req.PatientPrenom}, Status: {req.Status}<br/>";
                    }
                }
                else
                {
                    info += "No requests found.<br/>";
                }
                
                info += $"<br/><a href='/Auth/ResetRepository'>Reset Repository</a>";
                
                return Content(info);
            }
            catch (Exception ex)
            {
                return Content($"Error accessing repository: {ex.Message}<br/><br/><a href='/Auth/ResetRepository'>Reset Repository</a>");
            }
        }

        // GET: Auth/ResetRepository (Debug helper)
        public ActionResult ResetRepository()
        {
            try
            {
                CABMED.Services.AppointmentRequestRepository.Reset();
                return Content("Repository reset successfully!<br/><br/><a href='/Auth/DebugRepository'>Check Repository Status</a>");
            }
            catch (Exception ex)
            {
                return Content($"Error resetting repository: {ex.Message}");
            }
        }

        // GET: Auth/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            return FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1");
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            string hashedPassword = HashPassword(password ?? string.Empty);
            if (string.Equals(hashedPassword, storedHash, StringComparison.OrdinalIgnoreCase))
                return true;

            // Fallback: if stored value is not a 40-char SHA1 hex, allow plaintext comparison (for dev databases)
            bool looksLikeSha1 = storedHash.Length == 40 && Regex.IsMatch(storedHash, "^[0-9a-fA-F]{40}$");
            if (!looksLikeSha1)
            {
                return string.Equals(password ?? string.Empty, storedHash);
            }

            return false;
        }

        private static string NormalizeRole(string role)
        {
            var r = (role ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(r)) return r;
            // remove diacritics
            var normalized = r.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            r = sb.ToString().Normalize(NormalizationForm.FormC);
            // map common variants
            if (r == "secretaire" || r == "secrétaire") r = "secretaire";
            if (r == "medecin" || r == "médecin") r = "medecin";
            return r;
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
