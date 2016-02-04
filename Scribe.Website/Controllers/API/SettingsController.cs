#region References

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

		public SettingsController(IScribeContext dataContext, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(dataContext, authenticationService)
		{
		}

		#endregion

		#region Methods

		[HttpPost]
		public SettingsView Save(SettingsView settings)
		{
			var service = new SettingsService(DataContext, GetCurrentUser());
			service.Save(settings);
			DataContext.SaveChanges();
			return settings;
		}

		[HttpPost]
		[AllowAnonymous]
		public void Setup(SetupView setup)
		{
			var accountService = new AccountService(DataContext, AuthenticationService);
			var user = accountService.Add(setup.UserName, setup.Password);
			user.Roles = ",Administrator,";
			DataContext.SaveChanges();

			var settingsService = new SettingsService(DataContext, user);
			settingsService.Save(setup);
			DataContext.SaveChanges();

			MvcApplication.IsConfigured = true;
		}

		#endregion
	}
}