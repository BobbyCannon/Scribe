#region References

using System;
using System.Net.Mail;
using System.Text;
using Scribe.Data.Entities;
using Scribe.Models.Views;

#endregion

namespace Scribe.Website
{
	/// <summary>
	/// Contains all extensions.
	/// </summary>
	public static class Extensions
	{
		#region Methods

		public static ProfileView Create(this User user)
		{
			if (string.IsNullOrEmpty(user.PictureUrl))
			{
				user.UpdatePictureUrl();
			}

			return new ProfileView
			{
				Disabled = !user.IsEnabled || user.IsActiveDirectory,
				DisplayName = user.DisplayName,
				EmailAddress = user.EmailAddress,
				PictureUrl = user.PictureUrl,
				UserId = user.Id,
				UserName = user.UserName
			};
		}

		/// <summary>
		/// Convert the base 64 encoded string to a non encoded string.
		/// </summary>
		/// <param name="data"> The base 64 encoded string to convert. </param>
		/// <returns> The non encoded string. </returns>
		public static string FromBase64(this string data)
		{
			var bytes = Convert.FromBase64String(data);
			return Encoding.UTF8.GetString(bytes);
		}

		/// <summary>
		/// Gets the root domain for the provided URI.
		/// </summary>
		/// <param name="uri"> The URI to process. </param>
		/// <returns> The URI for only the root domain. </returns>
		public static string GetRootDomain(this Uri uri)
		{
			var hostParts = uri.Host.Split('.');
			var host = hostParts.Length >= 2 ? hostParts[hostParts.Length - 2] + "." + hostParts[hostParts.Length - 1] : hostParts[0];
			var response = uri.Scheme + "://" + host;

			if (((uri.Scheme == "https") && (uri.Port != 443)) || ((uri.Scheme == "http") && (uri.Port != 80)))
			{
				response += ":" + uri.Port;
			}

			return response;
		}

		/// <summary>
		/// Determines if the string is a valid email address.
		/// </summary>
		/// <param name="emailaddress"> The string to validate. </param>
		/// <returns> Returns true if the email address is valid. False if otherwise. </returns>
		public static bool IsValidEmailAddress(this string emailaddress)
		{
			try
			{
				var m = new MailAddress(emailaddress);
				return true;
			}
			catch
			{
				return false;
			}
		}

		#endregion
	}
}