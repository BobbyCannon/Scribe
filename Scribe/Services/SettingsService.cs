#region References

using System;
using System.Linq;
using Scribe.Data;
using Scribe.Models.Entities;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public class SettingsService
	{
		#region Fields

		private readonly IScribeContext _dataContext;
		private readonly User _user;

		#endregion

		#region Constructors

		public SettingsService(IScribeContext dataContext, User user)
		{
			_dataContext = dataContext;
			_user = user;
		}

		#endregion

		#region Properties

		public string LdapConnectionString
		{
			get { return GetSetting("LDAP Connection String", string.Empty); }
			set { AddOrUpdateSetting("LDAP Connection String", value); }
		}

		public bool OverwriteFilesOnUpload
		{
			get { return GetSetting("Overwrite Files On Upload", true); }
			set { AddOrUpdateSetting("Overwrite Files On Upload", value.ToString()); }
		}

		#endregion

		#region Methods

		public SettingsView GetSettings()
		{
			if (_user == null || !_user.InRole("Administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to be access the settings.");
			}

			return new SettingsView
			{
				LdapConnectionString = LdapConnectionString,
				OverwriteFilesOnUpload = OverwriteFilesOnUpload
			};
		}

		public void Save(SettingsView settings)
		{
			if (_user == null || !_user.InRole("Administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to be able to save settings.");
			}

			LdapConnectionString = settings.LdapConnectionString ?? string.Empty;
			OverwriteFilesOnUpload = settings.OverwriteFilesOnUpload;
		}

		private void AddOrUpdateSetting(string name, string value)
		{
			var setting = _dataContext.Settings.FirstOrDefault(x => x.Name == name);
			if (setting == null)
			{
				setting = new Setting { Name = name };
				_dataContext.Settings.Add(setting);
			}

			setting.Value = value;
		}

		private string GetSetting(string name, string defaultValue = null)
		{
			return _dataContext.Settings.FirstOrDefault(x => x.Name == name)?.Value ?? defaultValue;
		}

		private bool GetSetting(string name, bool defaultValue = false)
		{
			var value = _dataContext.Settings.FirstOrDefault(x => x.Name == name)?.Value ?? defaultValue.ToString();
			var response = defaultValue;
			bool.TryParse(value, out response);
			return response;
		}

		#endregion
	}
}