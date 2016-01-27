namespace Scribe.Website.Hubs
{
	public interface INotificationHub
	{
		#region Methods

		void PageAvailableForEdit(int id);
		void PageLockedForEdit(int id, string editorName);
		void PageUpdated(int id);

		#endregion
	}
}