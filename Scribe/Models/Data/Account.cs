#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Scribe.Models.Data
{
	public class Account
	{
		#region Properties

		[Required]
		public string EmailAddress { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		/// <summary>
		/// This is for bots. This is not real data.
		/// </summary>
		public string PhoneNumber { get; set; }

		[Required]
		public string UserName { get; set; }

		#endregion
	}
}