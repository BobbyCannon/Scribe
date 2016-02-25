#region References

using System.Data;
using System.DirectoryServices;
using System.Linq;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public class AccountService
	{
		#region Fields

		private readonly IAuthenticationService _authenticationService;
		private readonly IScribeContext _context;
		private readonly string _ldapConnectionString;

		#endregion

		#region Constructors

		public AccountService(IScribeContext context, IAuthenticationService authenticationService)
		{
			_context = context;
			_authenticationService = authenticationService;
			_ldapConnectionString = new SettingsService(context, null).LdapConnectionString;
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

		public bool LogIn(Credentials credentials)
		{
			var searchResult = GetUserAccount(credentials.UserName, credentials.Password);
			if (searchResult == null)
			{
				return Authenticate(credentials);
			}

			var user = AddOrUpdateUser(credentials.UserName, credentials.Password, searchResult);
			_authenticationService.LogIn(user, credentials.RememberMe);
			return true;
		}

		public void LogOut()
		{
			_authenticationService.LogOut();
		}

		public void Update(ProfileView profile)
		{
			var user = _context.Users.FirstOrDefault(x => x.Id == profile.UserId);
			if (user == null)
			{
				throw new DataException("The profile could not be found.");
			}

			user.DisplayName = profile.DisplayName;
			user.EmailAddress = profile.EmailAddress;
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

			var foundUser = _context.Users.FirstOrDefault(x => x.UserName == userName);
			if (foundUser != null)
			{
				foundUser.EmailAddress = user.EmailAddress;
				foundUser.DisplayName = user.DisplayName;
				foundUser.IsActiveDirectory = true;
				foundUser.SetPassword(password);
				user = foundUser;
			}

			_context.Users.AddOrUpdate(user);
			_context.SaveChanges();

			return user;
		}

		private bool Authenticate(Credentials model)
		{
			var user = _context.Users.FirstOrDefault(x => x.UserName == model.UserName);
			if (user == null)
			{
				return false;
			}

			var hash = User.HashPassword(model.Password, user.Salt);
			if (user.PasswordHash != hash)
			{
				return false;
			}

			_authenticationService.LogIn(user, model.RememberMe);
			return true;
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