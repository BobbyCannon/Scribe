#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EasyDataFramework;
using Scribe.Models.Views;

#endregion

namespace Scribe.Models.Entities
{
	/// <summary>
	/// Represents a user of the scribe wiki.
	/// </summary>
	public class User : Entity
	{
		#region Constructors

		[SuppressMessage("ReSharper", "DoNotCallOverridableMethodsInConstructor")]
		public User()
		{
			CreatedFiles = new Collection<File>();
			CreatedPages = new Collection<Page>();
			ModifiedFiles = new Collection<File>();
			PagesBeingEdited = new Collection<Page>();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the files created by this user.
		/// </summary>
		public virtual ICollection<File> CreatedFiles { get; set; }

		/// <summary>
		/// Gets the pages created by this user.
		/// </summary>
		public virtual ICollection<Page> CreatedPages { get; set; }

		/// <summary>
		/// Gets or sets the display name of the user.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the email address for the user.
		/// </summary>
		public string EmailAddress { get; set; }

		/// <summary>
		/// Gets or sets the flag indicating this account is an active directory account.
		/// </summary>
		public bool IsActiveDirectory { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the account is enabled.
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Gets the files modified by this user.
		/// </summary>
		public virtual ICollection<File> ModifiedFiles { get; set; }

		/// <summary>
		/// Gets the list of pages being edited by this users.
		/// </summary>
		public virtual ICollection<Page> PagesBeingEdited { get; set; }

		/// <summary>
		/// Gets the hashed password for the user.
		/// </summary>
		/// <remarks>
		/// Use <see cref="SetPassword" /> to set the user's password, and <see cref="User.HashPassword" /> to encrypt a plain text
		/// password for authentication with the salt and password.
		/// </remarks>
		public string PasswordHash { get; private set; }

		/// <summary>
		/// Do not use this property to set the password - use <see cref="SetPassword" /> instead. Use
		/// <see cref="HashPassword" /> for authentication with the salt and password.
		/// </summary>
		public string Salt { get; private set; }

		/// <summary>
		/// Gets or sets the tags for the user in the format ",Administrator,Editor,Approver," (no spaces between tags).
		/// </summary>
		public string Tags { get; set; }

		/// <summary>
		/// Gets the user name for the user.
		/// </summary>
		public string UserName { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Hashes a combination of the password and salt using SHA1 via FormsAuthentication, or SHA256 is FormsAuthentication is
		/// not enabled.
		/// </summary>
		public static string HashPassword(string password, string salt)
		{
			SHA256 sha = new SHA256Managed();
			var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(password + salt));

			var stringBuilder = new StringBuilder();
			foreach (var b in hash)
			{
				stringBuilder.AppendFormat("{0:x2}", b);
			}

			return stringBuilder.ToString();
		}

		/// <summary>
		/// Checks to see if the user is in the provided role.
		/// </summary>
		/// <param name="role"> The role to check for. </param>
		/// <returns> True if the user is in the role or otherwise false. </returns>
		public bool InRole(string role)
		{
			return Tags.IndexOf("," + role + ",", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		/// <summary>
		/// Encrypts and sets the password for the user.
		/// </summary>
		/// <param name="password"> The password in plain text format. </param>
		public void SetPassword(string password)
		{
			Salt = GetSalt();
			PasswordHash = HashPassword(password, Salt);
		}

		/// <summary>
		/// Converts the user into a user view.
		/// </summary>
		/// <returns> </returns>
		public UserView ToView()
		{
			return new UserView
			{
				DisplayName = DisplayName,
				EmailAddress = EmailAddress,
				Id = Id,
				Tags = Tags.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().OrderBy(x => x),
				UserName = UserName
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Salt" /> class, generating a new salt value.
		/// </summary>
		private string GetSalt()
		{
			var builder = new StringBuilder(16);
			var random = new Random();

			for (var i = 0; i < 16; i++)
			{
				builder.Append((char) random.Next(33, 126));
			}

			return builder.ToString();
		}

		#endregion
	}
}