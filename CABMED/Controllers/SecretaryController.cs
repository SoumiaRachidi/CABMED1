using System.Web.Mvc;

namespace CABMED.Controllers
{
    public class SecretaryController : Controller
    {
        // GET: Secretary/Index
        public ActionResult Index()
        {
            var role = (Session["Role"] as string)?.ToLower();
            if (role != "secretaire")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.UserName = Session["UserName"] as string;
            return View();
        }
    }
}
