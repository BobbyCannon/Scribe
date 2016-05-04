#region References

using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Scribe.Data;
using Scribe.Data.Migrations;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.UnitTests.Properties;
using Speedy;
using TestR.Desktop;
using TestR.Logging;
using Database = System.Data.Entity.Database;

#endregion

namespace Scribe.UnitTests
{
	public static class TestHelper
	{
		#region Constructors

		static TestHelper()
		{
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ScribeSqlDatabase, Configuration>(true));
		}

		#endregion

		#region Properties

		public static bool RunUnitTestAgainstDatabase => false;

		#endregion

		#region Methods

		public static void AddConsoleLogger()
		{
			if (LogManager.Loggers.Count <= 0)
			{
				LogManager.Loggers.Add(new ConsoleLogger { Level = LogLevel.Verbose });
			}
		}

		public static void AddDefaultSettings(IScribeDatabase database, User administrator)
		{
			var service = new SettingsService(database, administrator);
			var settings = new SettingsView
			{
				EnableGuestMode = false,
				LdapConnectionString = string.Empty,
				OverwriteFilesOnUpload = false,
				PrintCss = string.Empty,
				ViewCss = string.Empty
			};

			service.Save(settings);
		}

		public static File AddFile(IScribeDatabase database, User user, string name, string type, byte[] data)
		{
			var service = new ScribeService(database, null, null, user);
			var id = service.SaveFile(new FileView { Name = name, Data = data, Type = type });
			return database.Files.First(x => x.Id == id);
		}

		public static PageVersion AddPage(IScribeDatabase database, string title, string content, User user, ApprovalStatus status = ApprovalStatus.None, bool published = false, bool homepage = false, params string[] tags)
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

			if (homepage)
			{
				var settingsService = new SettingsService(database, user);
				var settings = settingsService.GetSettings();
				settings.FrontPagePrivateId = view.Id;
				settings.FrontPagePublicId = view.Id;
				settingsService.Save(settings);
				database.SaveChanges();
			}

			return database.PageVersions.OrderByDescending(x => x.PageId == view.Id).First();
		}

		public static SettingsView AddSettings(IScribeDatabase database, User administrator, SettingsView settings)
		{
			var service = new SettingsService(database, administrator);
			service.Save(settings);
			database.SaveChanges();
			return settings;
		}

		public static User AddUser(IScribeDatabase database, string userName, string password, params string[] roles)
		{
			var user = new User
			{
				DisplayName = userName,
				UserName = userName.Replace(" ", ""),
				EmailAddress = $"{userName}@domain.com",
				Tags = $",{string.Join(",", roles)},"
			};

			user.SetPassword(password);
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

		public static IScribeDatabase GetContext(bool clearDatabase = true)
		{
			if (RunUnitTestAgainstDatabase)
			{
				var context = new ScribeSqlDatabase();
				if (clearDatabase)
				{
					context.Database.ExecuteSqlCommand(Resources.ClearDatabase);
				}
				return context;
			}

			return new ScribeDatabase();
		}

		public static IScribeContextProvider GetContextProvider()
		{
			var provider = new Mock<IScribeContextProvider>();

			if (RunUnitTestAgainstDatabase)
			{
				provider.Setup(x => x.GetContext(It.IsAny<bool>())).Returns<bool>(clearDatabase =>
				{
					var context = new ScribeSqlDatabase();

					if (clearDatabase)
					{
						context.Database.ExecuteSqlCommand(Resources.ClearDatabase);
					}

					return context;
				});
			}
			else
			{
				var context = new ScribeDatabase();
				provider.Setup(x => x.GetContext(It.IsAny<bool>())).Returns<bool>(clearDatabase =>
				{
					if (clearDatabase)
					{
						context?.Dispose();
						context = new ScribeDatabase();
					}

					return context;
				});
			}

			return provider.Object;
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

		public static PageVersion UpdatePage(IScribeDatabase database, User user, PageView view, Action<PageView> action, ApprovalStatus status = ApprovalStatus.None, bool published = false)
		{
			var service = new ScribeService(database, null, GetSearchService(), user);
			action(view);

			service.SavePage(view);
			database.SaveChanges();

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

			database.SaveChanges();

			return database.PageVersions.OrderByDescending(x => x.PageId == view.Id).First();
		}

		#endregion
	}
}