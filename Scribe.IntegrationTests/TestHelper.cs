#region References

using System;
using System.Linq;
using System.Web.Http;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Scribe.Data;
using Scribe.IntegrationTests.Properties;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Services;
using TestR.Desktop;
using TestR.Logging;

#endregion

namespace Scribe.IntegrationTests
{
	public static class TestHelper
	{
		#region Methods

		public static void AddConsoleLogger()
		{
			if (LogManager.Loggers.Count <= 0)
			{
				LogManager.Loggers.Add(new ConsoleLogger { Level = LogLevel.Verbose });
			}
		}

		public static void AddDefaultSettings(IScribeContext context, User administrator)
		{
			var service = new SettingsService(context, administrator);
			var settings = new SettingsView
			{
				EnableGuestMode = false,
				LdapConnectionString = string.Empty,
				OverwriteFilesOnUpload = false,
				SoftDelete = false,
				ViewCss = string.Empty,
				PrintCss = string.Empty
			};

			service.Save(settings);
		}

		public static File AddFile(IScribeContext context, User user, string name, string type, byte[] data)
		{
			var service = new ScribeService(context, null, GetSearchService(), user);
			var id = service.SaveFile(new FileView { Name = name, Data = data, Type = type });
			return context.Files.First(x => x.Id == id);
		}

		public static PageVersion AddPage(IScribeContext context, string title, string content, User user, ApprovalStatus status, bool published = false, params string[] tags)
		{
			var service = new ScribeService(context, null, GetSearchService(), user);
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

			return context.PageVersions.First(x => x.Id == view.Id);
		}

		public static void AddSettings(IScribeContext context, User administrator, SettingsView settings)
		{
			var service = new SettingsService(context, administrator);
			service.Save(settings);
		}

		public static User AddUser(IScribeContext context, string userName, string password, params string[] roles)
		{
			var user = new User
			{
				DisplayName = userName,
				UserName = userName,
				EmailAddress = $"{userName}@domain.com",
				Tags = $",{string.Join(",", roles)},"
			};

			user.SetPassword(password);
			context.Users.Add(user);
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

		public static IScribeContext GetContext(bool clearDatabase = true)
		{
			var context = new ScribeContext();

			if (clearDatabase)
			{
				context.Database.ExecuteSqlCommand(Resources.ClearDatabase);
			}

			return context;
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

		#endregion
	}
}