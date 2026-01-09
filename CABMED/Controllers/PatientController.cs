using System;
using System.Web.Mvc;

namespace CABMED.Controllers
{
    public class PatientController : Controller
    {
        // GET: Patient/Index
        public ActionResult Index()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "patient")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;
            return View();
        }
    }
}
