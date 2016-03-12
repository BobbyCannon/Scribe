#region References

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Data;
using Scribe.Models.Enumerations;
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
		public void BeginEditingPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				var result = client.BeginEditingPage(page.Id);
				Assert.AreEqual("Hello Page", result.Title);
				Assert.AreEqual("John Doe", result.EditingBy);
			}
		}

		[TestMethod]
		public void CancelEditingPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.PageVersions.First().EditingById = john.Id;
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				Assert.AreEqual(john.Id, context.PageVersions.First().EditingById);
				client.CancelEditingPage(page.Id);
			}

			using (var context = TestHelper.GetContext(false))
			{
				Assert.IsNull(context.PageVersions.First().EditingById);
			}
		}

		[TestMethod]
		public void DeleteFile()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file = TestHelper.AddFile(context, john, "File.png", "image/png", new byte[0]);
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				client.DeleteFile(file.Id);

				TestHelper.ExpectedException<Exception>(() =>
				{
					var result = client.GetPage(file.Id);
					Assert.AreEqual("Hello Page", result.Title);
				}, "Bad Request");

				Assert.AreEqual(0, context.PageVersions.Count());
			}
		}

		[TestMethod]
		public void DeletePage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				Assert.IsFalse(client.IsAuthenticated);

				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				Assert.IsTrue(client.IsAuthenticated);

				client.DeletePage(page.Id);

				TestHelper.ExpectedException<Exception>(() =>
				{
					var result = client.GetPage(page.Id);
					Assert.AreEqual("Hello Page", result.Title);
				}, "Bad Request");

				Assert.AreEqual(0, context.PageVersions.Count());
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				var result = client.GetPage(page.Id);
				Assert.AreEqual("Hello Page", result.Title);
			}
		}

		[TestMethod]
		public void GetPagePreview()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				var result = client.GetPagePreview(page.ToView());
				Assert.AreEqual("<p>Hello World</p>\n", result);
			}
		}

		[TestMethod]
		public void GetPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();
			}

			var client = new ScribeClient(TestSite, TestService);
			client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
			var pages = client.GetPages().Results.ToList();
			Assert.AreEqual(1, pages.Count);
			Assert.AreEqual("Hello Page", pages[0].Title);
		}

		[TestMethod]
		public void GetPagesUsingFilter()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				TestHelper.AddPage(context, "Another Page 2", "Hello World... again", john, ApprovalStatus.None, false, "anotherTag");
				context.SaveChanges();
			}

			var client = new ScribeClient(TestSite, TestService);
			client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
			var pages = client.GetPages(new PagedRequest("Tags=anotherTag")).Results.ToList();
			Assert.AreEqual(1, pages.Count);
			Assert.AreEqual("Another Page 2", pages[0].Title);
		}

		[TestMethod]
		public void GetPagesUsingOrder()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				TestHelper.AddPage(context, "Another Page 2", "Hello World... again", john, ApprovalStatus.None, false, "anotherTag");
				context.SaveChanges();
			}

			var client = new ScribeClient(TestSite, TestService);
			client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
			var pages = client.GetPages(new PagedRequest { Order = "Tags" }).Results.ToList();
			Assert.AreEqual(2, pages.Count);
			Assert.AreEqual("Another Page 2", pages[0].Title);
			Assert.AreEqual("Hello Page", pages[1].Title);
		}

		#endregion
	}
}