#region References

using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Attributes;

#endregion

namespace Scribe.Website.Controllers
{
	public class AccountController : BaseController
	{
		#region Constructors

		public AccountController(IScribeDatabase dataDatabase, IAuthenticationService authenticationService)
			: base(dataDatabase, authenticationService)
		{
		}

		#endregion

		#region Methods

		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			GetCurrentUser(null,false);
			ViewBag.ReturnUrl = returnUrl;
			return View(new Credentials());
		}

		[HttpPost]
		[AllowAnonymous]
		public ActionResult Login(Credentials model, string returnUrl)
		{
			if (!ModelState.IsValid)
			{
				ModelState.AddModelError("userName", Constants.LoginInvalidError);
				return View(model);
			}

			var accountService = new AccountService(DataDatabase, AuthenticationService);
			if (!accountService.LogIn(model))
			{
				ModelState.AddModelError("userName", Constants.LoginInvalidError);
				ModelState.AddModelError("password", Constants.LoginInvalidError);
				return View(model);
			}

			DataDatabase.SaveChanges();

			if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}

			return RedirectToAction("Home", "Page");
		}

		public ActionResult Logout()
		{
			AuthenticationService.LogOut();
			return RedirectToAction("Home", "Page");
		}

		[AllowAnonymous]
		public ActionResult Unauthorized()
		{
			return View();
		}

		[ActionName("Profile")]
		public ActionResult UserProfile()
		{
			return View(ProfileView.Create(GetCurrentUser()));
		}

		[ActionName("Profile")]
		[HttpPost]
		public ActionResult UserProfile(ProfileView profile)
		{
			var service = new AccountService(DataDatabase, AuthenticationService);
			service.Update(profile);
			DataDatabase.SaveChanges();
			return View(profile);
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult Users()
		{
			var accountService = new AccountService(DataDatabase, AuthenticationService);
			var service = new ScribeService(DataDatabase, accountService, null, GetCurrentUser());
			return View(service.GetUsers(new PagedRequest { PerPage = int.MaxValue }));
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult UsersWithTag(string tag)
		{
			var accountService = new AccountService(DataDatabase, AuthenticationService);
			var service = new ScribeService(DataDatabase, accountService, null, GetCurrentUser());
			return View(service.GetUsers(new PagedRequest { Filter = $"Tags={tag}", Page = 1, PerPage = int.MaxValue }));
		}

		[MvcAuthorize(Roles = "Administrator")]
		[ActionName("User")]
		public ActionResult UserView(int id)
		{
			var accountService = new AccountService(DataDatabase, AuthenticationService);
			var service = new ScribeService(DataDatabase, accountService, null, GetCurrentUser());
			return View(service.GetUser(id));
		}

		#endregion
	}
}