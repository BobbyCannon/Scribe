#region References

using System.Web.Http;
using Bloodhound.Models;
using Scribe.Data;
using Scribe.Website.Services;
using Scribe.Website.Services.Notifications;
using Scribe.Website.Services.Settings;

#endregion

namespace Scribe.Website.WebApi
{
	public class AccountController : BaseApiController
	{
		#region Constructors

		public AccountController(IScribeDatabase database, IAuthenticationService authenticationService, INotificationService notificationService)
			: base(database, authenticationService)
		{
			NotificationService = notificationService;
		}

		#endregion

		#region Properties

		public INotificationService NotificationService { get; set; }

		#endregion

		#region Methods

		[HttpPost]
		[AllowAnonymous]
		[Route("api/Account/ForgotPassword")]
		public string ForgotPassword([FromBody] string userName)
		{
			var service = new AccountService(Database, AuthenticationService);
			var userSettings = service.SetForgotPasswordToken(userName);
			var siteSettings = SiteSettings.Load(Database);

			Database.SaveChanges();

			try
			{
				NotificationService.SendNotification(siteSettings, AccountService.CreateResetPasswordRequestMessage(siteSettings, userSettings, GetHostUri()));
			}
			catch
			{
				MvcApplication.Tracker?.AddEvent(AnalyticEvents.FailedToSendEmail.ToString(),
					new EventValue("User Id", userSettings.User.Id.ToString()),
					new EventValue("Email Address", userSettings.User.EmailAddress));

				throw;
			}

			return userName;
		}

		#endregion
	}
}