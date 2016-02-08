﻿#region References

using System;
using System.Linq;
using System.Web.Http;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Data;
using Scribe.Extensions;
using Scribe.IntegrationTests.Properties;
using Scribe.Models.Entities;
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
				EnablePublicTag = false,
				LdapConnectionString = string.Empty,
				OverwriteFilesOnUpload = false,
				SoftDelete = false
			};

			service.Save(settings);
		}

		public static Page AddPage(IScribeContext context, string title, string content, User user, params string[] tags)
		{
			var service = new ScribeService(context, null, null, user);
			var view = service.SavePage(new PageView { Title = title, Text = content, Tags = tags });
			return context.Pages.First(x => x.Id == view.Id);
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
				Roles = $",{string.Join(",", roles)},"
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