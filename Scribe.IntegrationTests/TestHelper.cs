#region References

using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Scribe.Data;
using Scribe.Data.Entities;
using Scribe.Data.Migrations;
using Scribe.IntegrationTests.Properties;
using Scribe.Models.Data;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Website.Services;
using Scribe.Website.Services.Settings;
using Speedy;
using TestR.Desktop;
using TestR.Logging;
using Database = System.Data.Entity.Database;

#endregion

namespace Scribe.IntegrationTests
{
	public static class TestHelper
	{
		#region Constructors

		static TestHelper()
		{
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ScribeSqlDatabase, Configuration>(true));
		}

		#endregion

		#region Methods

		public static void AddConsoleLogger()
		{
			if (LogManager.Loggers.Count <= 0)
			{
				LogManager.Loggers.Add(new ConsoleLogger { Level = LogLevel.Verbose });
			}
		}

		public static void AddDefaultSettings(IScribeDatabase database)
		{
			var service = SiteSettings.Load(database, true);
			var settings = new SettingsView
			{
				ContactEmail = "admin@domain.com",
				EnableGuestMode = false,
				LdapConnectionString = string.Empty,
				MailServer = "127.0.0.1",
				OverwriteFilesOnUpload = false,
				SoftDelete = false,
				ViewCss = string.Empty,
				PrintCss = string.Empty
			};

			service.Apply(settings);
			service.Save();
		}

		public static File AddFile(IScribeDatabase database, User user, string name, string type, byte[] data)
		{
			var service = new ScribeService(database, null, GetSearchService(), user);
			var id = service.SaveFile(new FileView { Name = name, Data = data, Type = type });
			return database.Files.First(x => x.Id == id);
		}

		public static PageVersion AddPage(IScribeDatabase database, string title, string content, User user, ApprovalStatus status, bool published = false, params string[] tags)
		{
			var service = new ScribeService(database, null, GetSearchService(), user);
			var view = service.SavePage(new PageView { ApprovalStatus = status, Title = title, Text = content, Tags = tags });

			switch (status)
			{
				case ApprovalStatus.Approved:
					service.UpdatePage(new PageUpdate { Id = view.Id, Type = PageUpdateType.Approve });
					break;

				case ApprovalStatus.Rejected:
					service.UpdatePage(new PageUpdate { Id = view.Id, Type = PageUpdateType.Reject });
					break;
			}

			if (published)
			{
				service.UpdatePage(new PageUpdate { Id = view.Id, Type = PageUpdateType.Publish });
			}

			return database.PageVersions.First(x => x.Id == view.Id);
		}

		public static void AddSettings(IScribeDatabase database, SettingsView settings)
		{
			var service = SiteSettings.Load(database);
			service.Apply(settings);
			service.Save();
		}

		public static User AddUser(IScribeDatabase database, string userName, string emailAddress, string password, params string[] roles)
		{
			var user = new User
			{
				DisplayName = userName,
				EmailAddress = emailAddress,
				IsEnabled = true,
				IsActiveDirectory = false,
				Tags = $",{string.Join(",", roles)},",
				UserName = userName
			};

			user.SetPassword(password);
			user.UpdatePictureUrl();
			database.Users.Add(user);
			return user;
		}

		public static void AreEqual<T>(T expected, T actual)
		{
			var compareObjects = new CompareLogic();
			compareObjects.Config.MaxDifferences = int.MaxValue;

			var result = compareObjects.Compare(expected, actual);
			Assert.IsTrue(result.AreEqual, result.DifferencesString);
		}

		public static void ClearDatabase()
		{
			GetDatabase().Dispose();
		}

		public static void ExpectedException<T>(Action work, string errorMessage) where T : Exception
		{
			try
			{
				work();
			}
			catch (HttpResponseException ex)
			{
				// todo: Can we make this better? blah...
				var exception = ex.Response.Content.ToJson();
				Assert.IsTrue(exception.Contains(errorMessage));
				return;
			}
			catch (T ex)
			{
				if (!ex.Message.Contains(errorMessage))
				{
					Assert.Fail("Expected <" + ex.Message + "> to contain <" + errorMessage + ">.");
				}
				return;
			}

			Assert.Fail("The expected exception was not thrown.");
		}

		public static IScribeDatabase GetDatabase(bool clearDatabase = true)
		{
			var database = new ScribeSqlDatabase();

			if (clearDatabase)
			{
				database.Database.ExecuteSqlCommand(Resources.ClearDatabase);
			}

			return database;
		}

		public static ISearchService GetSearchService()
		{
			var service = new Mock<ISearchService>();
			return service.Object;
		}

		public static void PrintChildren(Element parent, string prefix = "")
		{
			var element = parent;
			if (element != null)
			{
				Console.WriteLine(prefix + element.ToDetailString().Replace(Environment.NewLine, ", "));
				prefix += "  ";
			}

			foreach (var child in parent.Children)
			{
				PrintChildren(child, prefix);
			}
		}

		public static Helpers.SmtpServer StartSmtpServer()
		{
			var server = new Helpers.SmtpServer();
			server.Start();
			return server;
		}

		internal static void AreEqual<T>(T expected, Func<T> actual, int timeout, int delay = 100)
		{
			Extensions.Retry(() => AreEqual(expected, actual()), timeout, delay);
		}

		#endregion
	}
}