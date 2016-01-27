#region References

using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;

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
			ViewBag.ReturnUrl = returnUrl;
			return View(new LoginModel());
		}

		[HttpPost]
		[AllowAnonymous]
		public ActionResult Login(LoginModel model, string returnUrl)
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
			var accountService = new AccountService(DataContext, AuthenticationService);
			accountService.Update(profile);
			DataContext.SaveChanges();
			return View(profile);
		}

		#endregion
	}
}