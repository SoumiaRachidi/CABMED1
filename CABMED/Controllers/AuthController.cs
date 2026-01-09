using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Security;
using CABMED.Models;
using CABMED.ViewModels;

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

                Session["UserID"] = user.UserId;
                Session["Role"] = user.Role;
                Session["UserName"] = (user.Prenom + " " + user.Nom).Trim();

                string role = (user.Role ?? string.Empty).ToLower();

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

                    var role = (model.Role ?? "patient").Trim().ToLower();
                    if (role != "admin" && role != "patient")
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
                    Session["Role"] = newUser.Role;
                    Session["UserName"] = (newUser.Prenom + " " + newUser.Nom).Trim();

                    if (role == "admin")
                        return RedirectToAction("Index", "Admin");
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

        // GET: Auth/CreateAdmin (TEMPORARY - Remove after creating admin)
        public ActionResult CreateAdmin()
        {
            try
            {
                var existingAdmin = db.Users.FirstOrDefault(u => u.Email.ToLower() == "admin@cabmed.com");
                if (existingAdmin != null)
                {
                    db.Users.Remove(existingAdmin);
                    db.SaveChanges();
                }

                var admin = new Users
                {
                    Nom = "Admin",
                    Prenom = "System",
                    Email = "admin@cabmed.com",
                    PasswordHash = HashPassword("admin123"),
                    Role = "admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(admin);
                db.SaveChanges();

                return Content("Admin user created successfully!<br/><br/>Email: admin@cabmed.com<br/>Password: admin123<br/>Password Hash: " + admin.PasswordHash + "<br/><br/><a href='/Auth/Login'>Go to Login</a>");
            }
            catch (Exception ex)
            {
                return Content("Error creating admin: " + ex.Message);
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
