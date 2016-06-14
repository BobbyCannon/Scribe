#region References

using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Attributes;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class AccountController : BaseController
	{
		#region Constructors

		public AccountController(IScribeDatabase database, IAuthenticationService authenticationService)
			: base(database, authenticationService)
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

			var accountService = new AccountService(Database, AuthenticationService);
			if (!accountService.LogIn(model))
			{
				ModelState.AddModelError("userName", Constants.LoginInvalidError);
				ModelState.AddModelError("password", Constants.LoginInvalidError);
				return View(model);
			}

			Database.SaveChanges();

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
			return View(GetCurrentUser().Create());
		}

		[ActionName("Profile")]
		[HttpPost]
		public ActionResult UserProfile(ProfileView profile)
		{
			var service = new AccountService(Database, AuthenticationService);
			service.Update(profile);
			Database.SaveChanges();
			return View(profile);
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult Users()
		{
			var accountService = new AccountService(Database, AuthenticationService);
			var service = new ScribeService(Database, accountService, null, GetCurrentUser());
			return View(service.GetUsers(new PagedRequest { PerPage = int.MaxValue }));
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult UsersWithTag(string tag)
		{
			var accountService = new AccountService(Database, AuthenticationService);
			var service = new ScribeService(Database, accountService, null, GetCurrentUser());
			ViewBag.Tag = tag;
			return View(service.GetUsers(new PagedRequest { Filter = $"Tags.Contains(\"{tag}\")", Page = 1, PerPage = int.MaxValue }));
		}

		[MvcAuthorize(Roles = "Administrator")]
		[ActionName("User")]
		public ActionResult UserView(int id)
		{
			var accountService = new AccountService(Database, AuthenticationService);
			var service = new ScribeService(Database, accountService, null, GetCurrentUser());
			return View(service.GetUser(id));
		}

		#endregion
	}
}