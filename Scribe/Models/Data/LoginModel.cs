#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Scribe.Models.Data
{
	public class LoginModel
	{
		#region Properties

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		/// <summary>
		/// This is for bots. This is not real data.
		/// </summary>
		public string PhoneNumber { get; set; }

		public bool RememberMe { get; set; }

		[Required]
		public string UserName { get; set; }

		#endregion
	}
}