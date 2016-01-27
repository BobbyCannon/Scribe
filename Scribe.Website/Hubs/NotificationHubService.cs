#region References

using Microsoft.AspNet.SignalR;

#endregion

namespace Scribe.Website.Hubs
{
	public class NotificationHubService : INotificationHub
	{
		#region Fields

		private readonly IHubContext _context;

		#endregion

		#region Constructors

		public NotificationHubService()
		{
			_context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
		}

		#endregion

		#region Methods

		public void PageAvailableForEdit(int id)
		{
			_context.Clients.All.PageAvailableForEdit(id);
		}

		public void PageLockedForEdit(int id, string editorName)
		{
			_context.Clients.All.PageLockedForEdit(id, editorName);
		}

		public void PageUpdated(int id)
		{
			_context.Clients.All.PageUpdated(id);
		}

		#endregion
	}
}