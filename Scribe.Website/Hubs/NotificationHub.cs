#region References

using Microsoft.AspNet.SignalR;

#endregion

namespace Scribe.Website.Hubs
{
	public class NotificationHub : Hub<INotificationHub>
	{
		#region Methods

		public void PageAvailableForEdit(int id)
		{
			Clients.All.PageAvailableForEdit(id);
		}

		public void PageLockedForEdit(int id, string editorName)
		{
			Clients.All.PageLockedForEdit(id, editorName);
		}

		public void PageUpdated(int id)
		{
			Clients.All.PageUpdated(id);
		}

		#endregion
	}
}