using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using WeddingPlanner.Models;

namespace WeddingPlanner.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    
    public class HomeController : Controller
    {
        private WeddingPlannerContext dbContext;
        public HomeController(WeddingPlannerContext context)
        {
            dbContext = context;
        }

        
        
        [HttpGet("")]
        public IActionResult Index()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost("")]
        public IActionResult Register(User newUser)
        {
            if(ModelState.IsValid)
            {
                if(dbContext.users.Any(u => u.Email == newUser.Email))
                {
                    ModelState.AddModelError("Email", "Email is already in use!");
                    return View("Index");
                }
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                newUser.Password = hasher.HashPassword(newUser, newUser.Password);
                dbContext.Add(newUser);
                dbContext.SaveChanges();

                HttpContext.Session.SetInt32("SessionUser", newUser.UserId);
                User user = dbContext.users.FirstOrDefault(u => u.UserId == newUser.UserId);
                var newId = newUser.UserId;
                System.Console.WriteLine($"||||||||||||||||||||||| {newId} |||||||||||||||||||||||");
                return Redirect("/Dashboard");
            }
            return View("Index");
        }

        [HttpGet("login")]
        public IActionResult LoginGet()
        {
            return View("Login");
        }

        [HttpPost("login")]
        public IActionResult Login(LoginUser getUser)
        {
            if(ModelState.IsValid)
            {
                User userInDb = dbContext.users.FirstOrDefault(u => u.Email == getUser.Email);
                if(userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email");
                    System.Console.WriteLine("XXXXXXXXXXXXXXXXXXXX LOGIN FAILED XXXXXXXXXXXXXXXXXXXXXXXXX");
                    return View("login");
                }

                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(getUser, userInDb.Password, getUser.Password);
                if(result == 0)
                {
                    ModelState.AddModelError("Password", "Invalid Password");
                    return View();
                }

                HttpContext.Session.SetInt32("SessionUser", userInDb.UserId);
                System.Console.WriteLine("******************** LOGIN SUCCESS ***********************");
                
                return Redirect("/Dashboard");
            }
            System.Console.WriteLine("OOOOOOOOOOOOOOOOOOOOOO LOGIN INVALID OOOOOOOOOOOOOOOOOOOOOOOO");
            return View();
            
        }

        [HttpGet("/Dashboard")]
        public IActionResult Dashboard()
        {
            int? SessionUser = HttpContext.Session.GetInt32("SessionUser");
            if(SessionUser != null){
                List<Wedding> AllWeddings = dbContext.weddings
                .Include(wed => wed.Guests)
                .OrderBy(w => w.Date).ToList();

                ViewBag.User = HttpContext.Session.GetInt32("SessionUser");
                ViewBag.Weddings = AllWeddings;
                return View(AllWeddings);
            }
            return View("Index");
        }

        [HttpGet("/wedding/new")]
        public IActionResult NewWedding()
        {
            int? SessionUser = HttpContext.Session.GetInt32("SessionUser");
            if(SessionUser != null)
            {
                return View();
            }
            return View("Index");
            
        }

        [HttpPost("/wedding/new")]
        public IActionResult Create(Wedding newWedding, int WeddingId)
        {
            if(ModelState.IsValid)
            {
                newWedding.UserId = (int)HttpContext.Session.GetInt32("SessionUser");
                dbContext.Add(newWedding);
                dbContext.SaveChanges();

                Association newAssociation = new Association();
                newAssociation.WeddingId = newWedding.WeddingId;
                newAssociation.UserId = (int)HttpContext.Session.GetInt32("SessionUser");
                dbContext.Add(newAssociation);
                dbContext.SaveChanges();

                return Redirect($"/wedding/{newWedding.WeddingId}");
            }
            return View("NewWedding");
        }

        [HttpGet("/wedding/{WeddingId}")]
        public IActionResult ViewWedding(int WeddingId)
        {
            int? SessionUser = HttpContext.Session.GetInt32("SessionUser");
            if(SessionUser != null)
            {
                Wedding thisWedding = dbContext.weddings
                    .Include(wed => wed.Guests)
                    .ThenInclude(u => u.User)
                    .FirstOrDefault(w => w.WeddingId == WeddingId);

                List<int> exusers = new List<int>();
                foreach(Association ass in thisWedding.Guests){
                    exusers.Add(ass.UserId);
                }
                ViewBag.Wedding = thisWedding;
                ViewBag.AddedGuests = dbContext.users.Where(u => exusers.Contains(u.UserId)).ToList();
                return View();
            }
            return View("Index");
        }

        [HttpGet("/wedding/{WeddingId}/DELETE")]
        public IActionResult Delete(int WeddingId)
        {
            int? SessionUser = HttpContext.Session.GetInt32("SessionUser");
            if(SessionUser != null)
            {
                Wedding RetrievedWedding = dbContext.weddings.SingleOrDefault(wedding => wedding.WeddingId == WeddingId);
                dbContext.weddings.Remove(RetrievedWedding);
                dbContext.SaveChanges();
                return Redirect("/Dashboard");
            }
            return View("Index");
        }

        [HttpGet("/wedding/{WeddingId}/RSVP")]
        public IActionResult RSVP(Association newAssociation, int WeddingId, int UserId)
        {
            int? SessionUser = HttpContext.Session.GetInt32("SessionUser");
            if(SessionUser != null)
            {
                Wedding thisWedding = dbContext.weddings
                .Include(w => w.Guests)
                .ThenInclude(u => u.User)
                .FirstOrDefault(w => w.WeddingId == WeddingId);

                newAssociation.WeddingId = thisWedding.WeddingId;
                newAssociation.UserId = (int)HttpContext.Session.GetInt32("SessionUser");
                dbContext.Add(newAssociation);
                dbContext.SaveChanges();
                return Redirect("/Dashboard");
            }
            return View("Index");
        }

        [HttpGet("/wedding/{WeddingId}/UNRSVP")]
        public IActionResult UNRSVP(Association DisAssociate, int WeddingId, int UserId)
        {
            int? SessionUser = HttpContext.Session.GetInt32("SessionUser");
            if(SessionUser != null)
            {
                DisAssociate.UserId = (int)HttpContext.Session.GetInt32("SessionUser");
                DisAssociate = dbContext.associations.FirstOrDefault(a => a.WeddingId == WeddingId && a.UserId == DisAssociate.UserId);
                dbContext.associations.Remove(DisAssociate);
                dbContext.SaveChanges();
                return Redirect("/Dashboard");
            }
            return View("Index");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View("Index");
        }

    }
}
