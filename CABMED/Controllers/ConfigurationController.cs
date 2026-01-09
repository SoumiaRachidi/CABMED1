using System.Web.Mvc;

namespace CABMED.Controllers
{
    public class ConfigurationController : Controller
    {
        private bool IsAdmin()
        {
            var role = (Session["Role"] as string)?.ToLower();
            return role == "admin";
        }

        // GET: Configuration/Index
        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // GET: Configuration/Departments
        public ActionResult Departments()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // GET: Configuration/Specialties
        public ActionResult Specialties()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // GET: Configuration/WorkingHours
        public ActionResult WorkingHours()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // GET: Configuration/Appointments
        public ActionResult Appointments()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View();
        }
    }
}
