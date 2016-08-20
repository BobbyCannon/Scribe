#region References

using System;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Website.Hubs;
using Scribe.Website.Services;
using Scribe.Website.Services.Settings;

#endregion

namespace Scribe.Website.WebApi
{
	[ApiExplorerSettings(IgnoreApi = true)]
	public class SettingsController : BaseApiController
	{
		#region Constructors

		public SettingsController(IScribeDatabase database, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(database, authenticationService)
		{
		}

		#endregion

		#region Methods

		[HttpPost]
		[AllowAnonymous]
		public void Reload()
		{
			using (var datacontext = new ScribeSqlDatabase())
			{
				SettingsService.ClearCache();

				var settingsService = SiteSettings.Load(datacontext, true);

				MvcApplication.IsConfigured = datacontext.Users.Any();
				MvcApplication.PrintCss = settingsService.PrintCss;
				MvcApplication.ViewCss = settingsService.ViewCss;
			}
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public SettingsView Save(SettingsView settings)
		{
			var service = SiteSettings.Load(Database, true);
			service.Apply(settings);
			service.Save();
			Database.SaveChanges();

			MvcApplication.PrintCss = settings.PrintCss;
			MvcApplication.ViewCss = settings.ViewCss;

			var path = HostingEnvironment.MapPath("~/App_Data/Indexes");
			var deleteIndex = service.EnableGuestMode != settings.EnableGuestMode;

			if (deleteIndex && path != null && Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}

			return settings;
		}

		[HttpPost]
		[AllowAnonymous]
		public void Setup(SetupView setup)
		{
			if (Database.Users.Any())
			{
				throw new InvalidOperationException("The site has already been setup.");
			}

			var accountService = new AccountService(Database, AuthenticationService);
			var account = new Account
			{
				EmailAddress = setup.EmailAddress,
				UserName = setup.UserName,
				Password = setup.Password
			};

			var userSettings = accountService.Register(account);
			userSettings.User.Tags = ",Administrator,";
			Database.SaveChanges();

			AuthenticationService.LogIn(userSettings.User, true);
			Database.SaveChanges();
			
			var siteSettings = SiteSettings.Load(Database);
			setup.ContactEmail = userSettings.User.EmailAddress;
			siteSettings.Apply(setup);
			siteSettings.Save();
			Database.SaveChanges();

			MvcApplication.IsConfigured = true;
		}

		#endregion
	}
}