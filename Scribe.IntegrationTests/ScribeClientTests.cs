#region References

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Data;
using Scribe.Models.Enumerations;
using Scribe.Services;
using Scribe.Web;

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				database.SaveChanges();

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				database.PageVersions.First().EditingById = john.Id;
				database.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				Assert.AreEqual(john.Id, database.PageVersions.First().EditingById);
				client.CancelEditingPage(page.Id);
			}

			using (var database = TestHelper.GetDatabase(false))
			{
				Assert.IsNull(database.PageVersions.First().EditingById);
			}
		}

		[TestMethod]
		public void DeleteFile()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				var file = TestHelper.AddFile(database, john, "File.png", "image/png", new byte[0]);
				database.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				client.DeleteFile(file.Id);

				TestHelper.ExpectedException<Exception>(() =>
				{
					var result = client.GetPage(file.Id);
					Assert.AreEqual("Hello Page", result.Title);
				}, "Bad Request");

				Assert.AreEqual(0, database.PageVersions.Count());
			}
		}

		[TestMethod]
		public void DeletePage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				database.SaveChanges();

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

				Assert.AreEqual(0, database.PageVersions.Count());
			}
		}

		[TestMethod]
		public void GetPage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				database.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				var result = client.GetPage(page.Id);
				Assert.AreEqual("Hello Page", result.Title);
			}
		}

		[TestMethod]
		public void GetPagePreview()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				database.SaveChanges();

				var client = new ScribeClient(TestSite, TestService);
				client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
				var view = client.BeginEditingPage(page.Id);
				var result = client.GetPagePreview(view);
				Assert.AreEqual("<p>Hello World</p>\n", result);
			}
		}

		[TestMethod]
		public void GetPages()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				database.SaveChanges();
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				TestHelper.AddPage(database, "Another Page 2", "Hello World... again", john, ApprovalStatus.None, false, "anotherTag");
				database.SaveChanges();
			}

			var client = new ScribeClient(TestSite, TestService);
			client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
			var pages = client.GetPages(new PagedRequest { Filter = "Tags.Contains(\",myTag,\")" }).Results.ToList();
			Assert.AreEqual(1, pages.Count);
			Assert.AreEqual("Hello Page", pages[0].Title);
		}

		[TestMethod]
		public void GetPagesUsingFilterWithParameters()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				TestHelper.AddPage(database, "Another Page 2", "Hello World... again", john, ApprovalStatus.None, false, "anotherTag");
				database.SaveChanges();
			}

			var client = new ScribeClient(TestSite, TestService);
			client.LogIn(new Credentials { UserName = "John Doe", Password = "Password!" });
			var pages = client.GetPages(new PagedRequest { Filter = "Tags.Contains(@0)", FilterValues = new object[] { ",anotherTag," } }).Results.ToList();
			Assert.AreEqual(1, pages.Count);
			Assert.AreEqual("Another Page 2", pages[0].Title);
		}

		[TestMethod]
		public void GetPagesUsingOrder()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
				TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				TestHelper.AddPage(database, "Another Page 2", "Hello World... again", john, ApprovalStatus.None, false, "anotherTag");
				database.SaveChanges();
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