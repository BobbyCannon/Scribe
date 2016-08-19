#region References

using Scribe.Website.Services.Settings;
using NotificationData = Scribe.Models.Data.Notification;

#endregion

namespace Scribe.Website.Services.Notifications
{
	public interface INotificationService
	{
		#region Properties

		NotificationData LastNotification { get; }

		#endregion

		#region Methods

		void SendNotification(SiteSettings settings, NotificationData message);

		#endregion
	}
}