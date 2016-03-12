#region References

using System;
using System.Collections.Generic;
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

		private static readonly Dictionary<string, string> _cache;
		private readonly IScribeContext _dataContext;
		private readonly User _user;

		#endregion

		#region Constructors

		public SettingsService(IScribeContext dataContext, User user)
		{
			_dataContext = dataContext;
			_user = user;
		}

		static SettingsService()
		{
			_cache = new Dictionary<string, string>();
		}

		#endregion

		#region Properties

		public bool EnableGuestMode
		{
			get { return GetSetting("Enable Guest Mode", false); }
			set { AddOrUpdateSetting("Enable Guest Mode", value.ToString()); }
		}

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

		public string PrintCss
		{
			get { return GetSetting("Print CSS", string.Empty); }
			set { AddOrUpdateSetting("Print CSS", value); }
		}

		public bool SoftDelete
		{
			get { return GetSetting("Soft Delete", false); }
			set { AddOrUpdateSetting("Soft Delete", value.ToString()); }
		}

		public string ViewCss
		{
			get { return GetSetting("View CSS", string.Empty); }
			set { AddOrUpdateSetting("View CSS", value); }
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
				EnableGuestMode = EnableGuestMode,
				LdapConnectionString = LdapConnectionString,
				OverwriteFilesOnUpload = OverwriteFilesOnUpload,
				PrintCss = PrintCss,
				SoftDelete = SoftDelete,
				ViewCss = ViewCss
			};
		}

		public void Save(SettingsView settings)
		{
			if (_user == null || !_user.InRole("Administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to be able to save settings.");
			}

			EnableGuestMode = settings.EnableGuestMode;
			LdapConnectionString = settings.LdapConnectionString ?? string.Empty;
			OverwriteFilesOnUpload = settings.OverwriteFilesOnUpload;
			PrintCss = settings.PrintCss;
			SoftDelete = settings.SoftDelete;
			ViewCss = settings.ViewCss;
		}

		private void AddOrUpdateSetting(string name, string value)
		{
			if (_user == null || !_user.InRole("Administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to be able to change settings.");
			}

			var setting = _dataContext.Settings.FirstOrDefault(x => x.Name == name);
			if (setting == null)
			{
				setting = new Setting { Name = name };
				_dataContext.Settings.Add(setting);
			}

			setting.Value = value;

			UpdateCache(name, value);
		}

		private string GetSetting(string name, string defaultValue = null)
		{
			if (_cache.ContainsKey(name))
			{
				return _cache[name];
			}

			var response = _dataContext.Settings.FirstOrDefault(x => x.Name == name)?.Value;
			if (response != null)
			{
				UpdateCache(name, response);
			}

			return response ?? defaultValue;
		}

		private bool GetSetting(string name, bool defaultValue = false)
		{
			if (_cache.ContainsKey(name))
			{
				return bool.Parse(_cache[name]);
			}

			var value = _dataContext.Settings.FirstOrDefault(x => x.Name == name)?.Value;
			if (value == null)
			{
				return defaultValue;
			}

			bool response;

			if (!bool.TryParse(value, out response))
			{
				return defaultValue;
			}

			UpdateCache(name, value);
			return response;
		}

		private void UpdateCache(string key, string value)
		{
			lock (_cache)
			{
				if (_cache.ContainsKey(key))
				{
					_cache[key] = value;
				}
				else
				{
					_cache.Add(key, value);
				}
			}
		}

		#endregion
	}
}