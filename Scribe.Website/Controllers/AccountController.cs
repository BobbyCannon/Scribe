#region References

using System;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Data.Entities;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Website.Attributes;
using Scribe.Website.Services;
using Scribe.Website.Services.Notifications;
using Scribe.Website.Services.Settings;

#endregion

namespace Scribe.Website.Controllers
{
	public class AccountController : BaseController
	{
		#region Constructors

		public AccountController(IScribeDatabase database, IAuthenticationService authenticationService, INotificationService notificationService)
			: base(database, authenticationService)
		{
			NotificationService = notificationService;
		}

		#endregion

		#region Properties

		public INotificationService NotificationService { get; }

		#endregion

		#region Methods

		[AllowAnonymous]
		public ActionResult ForgotPassword()
		{
			return View();
		}

		[AllowAnonymous]
		public ActionResult InvalidPasswordReset()
		{
			return View();
		}

		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			GetCurrentUser(null, false);
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
		public ActionResult Register()
		{
			GetCurrentUser(null, false);
			return View(new Account());
		}

		[HttpPost]
		[AllowAnonymous]
		public ActionResult Register(Account account)
		{
			try
			{
				var settings = Create(account);
				AuthenticationService.LogIn(settings.User, false);
				return RedirectToAction("Home", "Page");
			}
			catch (Exception ex)
			{
				ViewBag.Error = ex.Message.ToSingleLine();
				return View(account);
			}
		}

		[AllowAnonymous]
		public ActionResult ResetPassword(string id)
		{
			Guid token;
			User user;
			var service = new AccountService(Database, AuthenticationService);

			if (!Guid.TryParse(id, out token) || (user = service.ValidatePasswordResetToken(token)) == null)
			{
				return RedirectToAction("InvalidPasswordReset");
			}

			Database.SaveChanges();
			AuthenticationService.LogIn(user, false);

			return RedirectToAction("Profile", "Account");
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
			var user = service.Update(GetCurrentUser(), profile);
			Database.SaveChanges();
			AuthenticationService.UpdateLogin(user);
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

		private UserSettings Create(Account account)
		{
			var service = new AccountService(Database, AuthenticationService);
			var userSettings = service.Register(account);

			Database.SaveChanges();

			return userSettings;
		}

		#endregion
	}
}