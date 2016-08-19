#region References

using System;
using System.Data;
using System.DirectoryServices;
using System.Linq;
using Scribe.Data;
using Scribe.Data.Entities;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Website.Services.Settings;
using Speedy;

#endregion

namespace Scribe.Website.Services
{
	public class AccountService
	{
		#region Fields

		private readonly IAuthenticationService _authenticationService;
		private readonly IScribeDatabase _database;
		private readonly string _ldapConnectionString;

		#endregion

		#region Constructors

		public AccountService(IScribeDatabase database, IAuthenticationService authenticationService)
		{
			_database = database;
			_authenticationService = authenticationService;
			_ldapConnectionString = SiteSettings.Load(database).LdapConnectionString;
		}

		#endregion

		#region Methods

		public User Add(string userName, string password)
		{
			var user = AddOrUpdateUser(userName, password, null);
			if (user == null)
			{
				throw new DataException("The profile could not be found.");
			}

			_authenticationService.LogIn(user, false);
			return user;
		}

		public User Authenticate(Credentials model)
		{
			var user = _database.Users.FirstOrDefault(x => x.UserName == model.UserName);
			if (user == null)
			{
				return null;
			}

			var hash = User.HashPassword(model.Password, user.Salt);
			if (user.PasswordHash != hash)
			{
				return null;
			}

			return user;
		}

		public static Notification CreateEmailValidationRequestMessage(SiteSettings siteSettings, UserSettings settings, string hostUri)
		{
			var link = hostUri + "/Account/ValidateEmailAddress/" + settings.EmailAddressValidationId;
			var message = new Notification();
			var user = settings.User;

			message.Target = user.UserName + ";" + user.EmailAddress;
			message.Title = "Scribe: Email Validation";
			message.Content = $"<p>You are receiving this email because a request has been made to validate the email address of {user.EmailAddress}. Simply click on the link below or paste it as the address in your favorite browser to validate your account: </p><p><a href=\"{link}\">{link}</a></p><p>If you didn't request this email then you can just ignore it. Also your personal information has not been disclosed to anyone. If you have any questions, feel free to <a href=\"mailto:{siteSettings.ContactEmail}\">contact us</a>.</p>";

			return message;
		}

		public static Notification CreateResetPasswordRequestMessage(SiteSettings siteSettings, UserSettings settings, string hostUri)
		{
			var link = hostUri + "/Account/ResetPassword/" + settings.ResetPasswordId;
			var message = new Notification();

			var user = settings.User;
			message.Target = user.UserName + ";" + user.EmailAddress;
			message.Title = "Scribe: Reset Password";
			message.Content = $"<p>You are receiving this email because a request has been made to reset the password associated with the email address of {user.EmailAddress}. If you would like to reset the password for this account simply click on the link below or paste it into the url field on your favorite browser: </p><p><a href=\"{link}\">{link}</a></p><p>If you didn't request this email then you can just ignore it. The forgot password link will expire in 24 hours after it was issued. Also your personal information has not been disclosed to anyone. If you have any questions, feel free to <a href=\"mailto:{siteSettings.ContactEmail}\">contact us</a>.</p>";

			return message;
		}

		public bool LogIn(Credentials credentials)
		{
			var searchResult = GetUserAccount(credentials.UserName, credentials.Password);
			if (searchResult == null)
			{
				var existingUser = Authenticate(credentials);
				if (existingUser != null)
				{
					_authenticationService.LogIn(existingUser, credentials.RememberMe);
				}

				return existingUser != null;
			}

			var user = AddOrUpdateUser(credentials.UserName, credentials.Password, searchResult);
			_authenticationService.LogIn(user, credentials.RememberMe);
			return true;
		}

		public void LogOut()
		{
			_authenticationService.LogOut();
		}

		public UserSettings Register(Account account)
		{
			if (string.IsNullOrEmpty(account.UserName))
			{
				throw new ArgumentException(Constants.UserNameRequiredError, nameof(account));
			}

			if (string.IsNullOrEmpty(account.EmailAddress))
			{
				throw new ArgumentException(Constants.EmailAddressRequiredError, nameof(account));
			}

			if (string.IsNullOrEmpty(account.Password))
			{
				throw new ArgumentException(Constants.PasswordRequiredError, nameof(account));
			}

			if ((account.UserName.Length < Constants.UserNameMinimumLength)
				|| (account.UserName.Length > Constants.UserNameMaximumLength))
			{
				throw new ArgumentException(Constants.UserNameLengthError, nameof(account));
			}

			if ((account.EmailAddress.Length < Constants.EmailAddressMinimumLength)
				|| (account.EmailAddress.Length > Constants.EmailAddressMaximumLength))
			{
				throw new ArgumentException(Constants.EmailAddressLengthError, nameof(account));
			}

			if (!account.EmailAddress.IsValidEmailAddress())
			{
				throw new ArgumentException(Constants.EmailAddressNotValidError, nameof(account));
			}

			if ((account.Password.Length < Constants.PasswordMinimumLength) || (account.Password.Length > Constants.PasswordMaximumLength))
			{
				throw new ArgumentException(Constants.PasswordLengthError, nameof(account));
			}

			var user = new User();

			user.DisplayName = account.UserName;
			user.EmailAddress = account.EmailAddress;
			user.IsActiveDirectory = false;
			user.IsEnabled = true;
			user.Tags = string.Empty;
			user.UserName = account.UserName;
			user.SetPassword(account.Password);
			user.UpdatePictureUrl();

			_database.Users.AddOrUpdate(user);

			var service = UserSettings.Load(_database.Settings, user);
			service.EmailAddressValidationId = Guid.NewGuid();
			service.EmailAddressValidationExpiresOn = DateTime.UtcNow.AddDays(1);
			service.Save();

			return service;
		}

		public UserSettings SetForgotPasswordToken(string userName)
		{
			var user = _database.Users.FirstOrDefault(u => u.EmailAddress.Equals(userName, StringComparison.OrdinalIgnoreCase)
				|| u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

			if (user == null)
			{
				throw new ArgumentException(Constants.UserCouldNotBeFound, nameof(userName));
			}

			var settings = UserSettings.Load(_database.Settings, user);
			settings.ResetPasswordId = Guid.NewGuid();
			settings.ResetPasswordExpiresOn = DateTime.UtcNow.AddDays(1);
			settings.Save();

			return settings;
		}

		public User Update(User user, ProfileView profileUpdate)
		{
			if (user.Id != profileUpdate.UserId)
			{
				throw new DataException("The profile update is not for your account.");
			}

			var profile = _database.Users.FirstOrDefault(x => x.Id == profileUpdate.UserId);
			if (profile == null)
			{
				throw new DataException("The profile could not be found.");
			}

			if (user.Id != profile.Id)
			{
				throw new DataException("The profile cannot be updated by this account.");
			}
			
			profile.DisplayName = profileUpdate.DisplayName;
			profile.UserName = profileUpdate.UserName;
			profile.EmailAddress = profileUpdate.EmailAddress;

			if (!string.IsNullOrEmpty(profileUpdate.Password))
			{
				profile.SetPassword(profileUpdate.Password);
			}

			return profile;
		}

		public User ValidatePasswordResetToken(Guid token)
		{
			if (token == Guid.Empty)
			{
				return null;
			}

			var tokenId = token.ToJson();
			var setting = _database.Settings.Include(x => x.User).FirstOrDefault(x => x.Name == nameof(UserSettings.ResetPasswordId) && x.Value == tokenId);
			if (setting == null)
			{
				return null;
			}

			var user = setting.User;
			if (!user.IsEnabled)
			{
				return null;
			}

			var settings = UserSettings.Load(_database.Settings, user);
			if (settings.ResetPasswordExpiresOn <= DateTime.UtcNow)
			{
				return null;
			}

			settings.ResetPasswordExpiresOn = DateTime.MinValue;
			settings.ResetPasswordId = Guid.Empty;
			settings.Save();

			return user;
		}

		private User AddOrUpdateUser(string userName, string password, SearchResult result)
		{
			result?.GetDirectoryEntry();

			var user = new User
			{
				EmailAddress = GetProperty(result?.Properties["mail"], string.Empty),
				DisplayName = $"{GetProperty(result?.Properties["givenname"], string.Empty)} {GetProperty(result?.Properties["sn"], string.Empty)}",
				IsActiveDirectory = result != null,
				IsEnabled = true,
				Tags = string.Empty,
				UserName = userName
			};

			if (user.DisplayName == " ")
			{
				user.DisplayName = userName;
			}

			if (user.EmailAddress == string.Empty)
			{
				user.EmailAddress = "userName@unknown.com";
			}

			user.SetPassword(password);

			var foundUser = _database.Users.FirstOrDefault(x => x.UserName == userName);
			if (foundUser != null)
			{
				foundUser.EmailAddress = user.EmailAddress;
				foundUser.DisplayName = user.DisplayName;
				foundUser.IsActiveDirectory = true;
				foundUser.SetPassword(password);
				user = foundUser;
			}

			_database.Users.AddOrUpdate(user);
			_database.SaveChanges();

			return user;
		}

		private string GetProperty(ResultPropertyValueCollection collection, string defaultValue)
		{
			return collection?.Count > 0 ? collection[0].ToString() : defaultValue;
		}

		private SearchResult GetUserAccount(string username, string password)
		{
			if (_ldapConnectionString == null)
			{
				return null;
			}

			try
			{
				var connectionString = _ldapConnectionString.ToUpper();
				if (!connectionString.StartsWith("LDAP://"))
				{
					connectionString = connectionString.Insert(0, "LDAP://");
				}

				var filter = "(&(objectCategory=user)(samAccountName=" + username + "))";
				var entry = new DirectoryEntry(connectionString);

				entry.Username = username;
				entry.Password = password;

				var searcher = new DirectorySearcher(entry);
				searcher.Filter = filter;
				searcher.SearchScope = SearchScope.Subtree;

				return searcher.FindOne();
			}
			catch
			{
				return null;
			}
		}

		#endregion
	}
}