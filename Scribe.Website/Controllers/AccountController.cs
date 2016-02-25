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

		public AccountController(IScribeContext dataContext, IAuthenticationService authenticationService)
			: base(dataContext, authenticationService)
		{
		}

		#endregion

		#region Methods

		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			GetCurrentUser(false);
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

			var accountService = new AccountService(DataContext, AuthenticationService);
			if (!accountService.LogIn(model))
			{
				ModelState.AddModelError("userName", Constants.LoginInvalidError);
				ModelState.AddModelError("password", Constants.LoginInvalidError);
				return View(model);
			}

			DataContext.SaveChanges();

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
			var service = new AccountService(DataContext, AuthenticationService);
			service.Update(profile);
			DataContext.SaveChanges();
			return View(profile);
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult Users()
		{
			var accountService = new AccountService(DataContext, AuthenticationService);
			var service = new ScribeService(DataContext, accountService, null, GetCurrentUser());
			return View(service.GetUsers(new PagedRequest(perPage: int.MaxValue)));
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult UsersWithTag(string tag)
		{
			var accountService = new AccountService(DataContext, AuthenticationService);
			var service = new ScribeService(DataContext, accountService, null, GetCurrentUser());
			return View(service.GetUsers(new PagedRequest($"Tags={tag}", 1, int.MaxValue)));
		}

		[MvcAuthorize(Roles = "Administrator")]
		[ActionName("User")]
		public ActionResult UserView(int id)
		{
			var accountService = new AccountService(DataContext, AuthenticationService);
			var service = new ScribeService(DataContext, accountService, null, GetCurrentUser());
			return View(service.GetUser(id));
		}

		#endregion
	}
}