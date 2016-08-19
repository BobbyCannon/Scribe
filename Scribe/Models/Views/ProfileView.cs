namespace Scribe.Models.Views
{
	public class ProfileView
	{
		#region Properties

		public bool Disabled { get; set; }
		public string DisplayName { get; set; }
		public string EmailAddress { get; set; }
		public string PictureUrl { get; set; }
		public int UserId { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }

		#endregion
	}
}