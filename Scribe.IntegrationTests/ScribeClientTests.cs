﻿#region References

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Data;
using Scribe.Services;

#endregion

namespace Scribe.IntegrationTests
{
	[TestClass]
	public class ScribeClientTests
	{
		#region Constants

		private const string TestService = "api/Service";
		private const string TestSite = "http://localhost";

		#endregion

		#region Methods

		[TestMethod]
		public void DeletePageAsUser()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				client.DeletePage(page.Id);

				TestHelper.ExpectedException<Exception>(() =>
				{
					var result = client.GetPage(page.Id);
					Assert.AreEqual("Hello Page", result.Title);
				}, "Bad Request");

				Assert.AreEqual(0, context.Pages.Count());
			}
		}

		[TestMethod]
		public void GetPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				var result = client.GetPage(page.Id);
				Assert.AreEqual("Hello Page", result.Title);
			}
		}

		[TestMethod]
		public void GetPages()
		{
			using (var context = TestHelper.GetContext(true))
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				context.SaveChanges();
			}

			var client = new ScribeClient(TestSite, TestService);
			client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
			var pages = client.GetPages().ToList();
			Assert.AreEqual(1, pages.Count);
			Assert.AreEqual("Hello Page", pages[0].Title);
		}

		#endregion
	}
}