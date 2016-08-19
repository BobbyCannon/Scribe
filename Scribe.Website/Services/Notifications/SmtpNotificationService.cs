#region References

using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using Scribe.Models.Data;
using Scribe.Website.Services.Settings;

#endregion

namespace Scribe.Website.Services.Notifications
{
	public class SmtpNotificationService : INotificationService
	{
		#region Properties

		public Notification LastNotification { get; private set; }

		#endregion

		#region Methods

		[ExcludeFromCodeCoverage]
		public void SendNotification(SiteSettings settings, Notification message)
		{
			// Send the actually email.
			using (var client = new SmtpClient(settings.MailServer))
			{
				var address = message.Target.Split(';');
				var mailTo = new MailAddress(address[1], address[0]);
				var mailFrom = new MailAddress(settings.ContactEmail, settings.ContactEmail);
				var mailMessage = new MailMessage(mailFrom, mailTo);

				mailMessage.Subject = message.Title;
				mailMessage.Body = message.Content;
				mailMessage.IsBodyHtml = true;

				client.Send(mailMessage);
				LastNotification = message;
			}
		}

		#endregion
	}
}