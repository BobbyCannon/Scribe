#region References

using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using Scribe.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Hubs;
using Scribe.Website.Services;

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
				var settingsService = new SettingsService(datacontext, null);
				settingsService.ClearCache();

				MvcApplication.IsConfigured = datacontext.Users.Any();
				MvcApplication.PrintCss = settingsService.PrintCss;
				MvcApplication.ViewCss = settingsService.ViewCss;
			}
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public SettingsView Save(SettingsView settings)
		{
			var service = new SettingsService(Database, GetCurrentUser());
			var deleteIndex = service.EnableGuestMode != settings.EnableGuestMode;
			service.Save(settings);
			Database.SaveChanges();

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
			var accountService = new AccountService(Database, AuthenticationService);
			var user = accountService.Add(setup.UserName, setup.Password);
			user.Tags = ",Administrator,";
			Database.SaveChanges();

			var settingsService = new SettingsService(Database, user);
			settingsService.Save(setup);
			Database.SaveChanges();

			MvcApplication.IsConfigured = true;
		}

		#endregion
	}
}