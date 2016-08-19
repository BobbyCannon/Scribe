#region References

using System;
using System.Data;
using System.Data.Entity;
using Scribe.Data.Entities;
using Scribe.Data.Mapping;
using Speedy;
using Speedy.EntityFramework;

#endregion

namespace Scribe.Data
{
	public class ScribeSqlDatabase : EntityFrameworkDatabase, IScribeDatabase
	{
		#region Constructors

		public ScribeSqlDatabase()
			: this("Name=DefaultConnection")
		{
		}

		public ScribeSqlDatabase(string connectionString)
			: base(connectionString, new DatabaseOptions())
		{
		}

		public ScribeSqlDatabase(string connectionString, DatabaseOptions options)
			: base(connectionString, options)
		{
		}

		#endregion

		#region Properties

		public IRepository<Event> Events => GetRepository<Event>();
		public IRepository<EventValue> EventValues => GetRepository<EventValue>();
		public IRepository<File> Files => GetRepository<File>();
		public IRepository<Page> Pages => GetRepository<Page>();
		public IRepository<PageVersion> PageVersions => GetRepository<PageVersion>();
		public IRepository<Setting> Settings => GetRepository<Setting>();
		public IRepository<User> Users => GetRepository<User>();

		#endregion

		#region Methods

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Configurations.Add(new EventMap());
			modelBuilder.Configurations.Add(new EventValueMap());
			modelBuilder.Configurations.Add(new FileMap());
			modelBuilder.Configurations.Add(new PageMap());
			modelBuilder.Configurations.Add(new PageVersionMap());
			modelBuilder.Configurations.Add(new SettingsMap());
			modelBuilder.Configurations.Add(new UserMap());
		}

		protected override void ProcessException(Exception exception)
		{
			var exceptionDetails = exception.ToDetailedString();
			CheckException("Username must be a string or array type with a maximum length", Constants.UserNameLengthError, exception, exceptionDetails);
			CheckException("EmailAddress must be a string or array type with a maximum length", Constants.EmailAddressLengthError, exception, exceptionDetails);
			CheckException("IX_Users_EmailAddress", Constants.EmailAddressAlreadyBeingUsed, exception, exceptionDetails);
			CheckException("IX_Users_ProfileName", Constants.UserNameAlreadyBeingUsed, exception, exceptionDetails);
			CheckException("IX_Users_UserName", Constants.UserNameAlreadyBeingUsed, exception, exceptionDetails);
			CheckException("IX_Users_DisplayName", Constants.UserNameAlreadyBeingUsed, exception, exceptionDetails);
		}

		private void CheckException(string value, string message, Exception exception, string exceptionDetails)
		{
			if (exceptionDetails.Contains(value))
			{
				throw new ConstraintException(message, exception);
			}
		}

		#endregion
	}
}