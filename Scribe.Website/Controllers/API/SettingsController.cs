#region References

using System.IO;
using System.Web.Hosting;
using System.Web.Http;
using Scribe.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Hubs;

#endregion

namespace Scribe.Website.Controllers.API
{
	public class SettingsController : BaseApiController
	{
		#region Constructors

		public SettingsController(IScribeDatabase dataDatabase, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(dataDatabase, authenticationService)
		{
		}

		#endregion

		#region Methods

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public SettingsView Save(SettingsView settings)
		{
			var service = new SettingsService(DataDatabase, GetCurrentUser());
			var deleteIndex = service.EnableGuestMode != settings.EnableGuestMode;
			service.Save(settings);
			DataDatabase.SaveChanges();
			MvcApplication.PrintCss = settings.PrintCss;
			MvcApplication.ViewCss = settings.ViewCss;

			var path = HostingEnvironment.MapPath("~/App_Data/Indexes");
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
			var accountService = new AccountService(DataDatabase, AuthenticationService);
			var user = accountService.Add(setup.UserName, setup.Password);
			user.Tags = ",Administrator,";
			DataDatabase.SaveChanges();

			var settingsService = new SettingsService(DataDatabase, user);
			settingsService.Save(setup);
			DataDatabase.SaveChanges();

			MvcApplication.IsConfigured = true;
		}

		#endregion
	}
}