#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Scribe.Data.Entities;
using Scribe.Website.Attributes;
using Speedy;

#endregion

namespace Scribe.Website.Services.Settings
{
	public abstract class SettingsService
	{
		#region Fields

		private static readonly Dictionary<string, string> _cache;
		private readonly string _name;
		private readonly PropertyInfo[] _propertyInfos;
		private readonly IRepository<Setting> _settings;

		#endregion

		#region Constructors

		static SettingsService()
		{
			_cache = new Dictionary<string, string>();
		}

		protected SettingsService(IRepository<Setting> settings, User user)
		{
			var type = GetType();

			_name = type.ToAssemblyName();
			_propertyInfos = type.GetProperties();
			_settings = settings;

			User = user;
		}

		#endregion

		#region Properties

		public User User { get; }

		#endregion

		#region Methods

		public static void ClearCache()
		{
			_cache.Clear();
		}

		public void Load(bool ignoreCache = false)
		{
			var userId = User?.Id;
			var values = _settings.Where(x => x.UserId == userId && x.Type == _name).ToList();
			var ignoreSettingType = typeof(IgnoreSettingAttribute);

			foreach (var propertyInfo in _propertyInfos)
			{
				if (propertyInfo.CustomAttributes.Any(x => x.AttributeType == ignoreSettingType) || (propertyInfo.SetMethod == null))
				{
					continue;
				}

				var key = GenerateKey(_name, propertyInfo.Name);
				if (!ignoreCache && _cache.ContainsKey(key))
				{
					propertyInfo.SetValue(this, _cache[key].FromJson(propertyInfo.PropertyType));
					continue;
				}

				var setting = values.SingleOrDefault(x => x.Name == propertyInfo.Name);
				if (setting != null)
				{
					propertyInfo.SetValue(this, setting.Value.FromJson(propertyInfo.PropertyType));
					UpdateCache(GenerateKey(_name, propertyInfo.Name), setting.Value);
				}
			}
		}

		public void Save()
		{
			var userId = User?.Id;
			var values = _settings.Where(x => x.UserId == userId && x.Type == _name).ToList();
			var ignoreSettingType = typeof(IgnoreSettingAttribute);

			foreach (var propertyInfo in _propertyInfos)
			{
				if (propertyInfo.CustomAttributes.Any(x => x.AttributeType == ignoreSettingType) || (propertyInfo.SetMethod == null))
				{
					continue;
				}

				var propertyValue = propertyInfo.GetValue(this, null);
				var value = propertyValue?.ToJson();

				var setting = values.FirstOrDefault(s => s.Name == propertyInfo.Name);
				if (setting != null)
				{
					setting.Value = value;
					setting.UserId = User?.Id;
				}
				else
				{
					_settings.Add(new Setting
					{
						Name = propertyInfo.Name,
						Type = _name,
						Value = value,
						SyncId = Guid.NewGuid(),
						UserId = User?.Id
					});
				}

				UpdateCache(GenerateKey(_name, propertyInfo.Name), value);
			}
		}

		private static string GenerateKey(string type, string property)
		{
			return $"{type}-{property}";
		}

		private static void UpdateCache(string key, string value)
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