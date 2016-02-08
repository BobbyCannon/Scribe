#region References

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Entities;
using Scribe.Models.Views;
using Scribe.Services;

#endregion

namespace Scribe.UnitTests
{
	[TestClass]
	public class ScribeServiceTests
	{
		#region Methods

		[TestMethod]
		public void DeletePage()
		{
			var provider = TestHelper.GetContextProvider();
			Page page;

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				context.SaveChanges();

				Assert.AreEqual(0, context.Pages.Count(x => x.IsDeleted));
				var service = new ScribeService(context, null, null, null);
				service.DeletePage(page.Id);
			}

			using (var context = provider.GetContext(false))
			{
				Assert.AreEqual(1, context.Pages.Count(x => x.IsDeleted));
				Assert.AreEqual(page.Id, context.Pages.First(x => x.IsDeleted).Id);
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages().ToList();

				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual(page.Title, actual[0].Title);
			}
		}

		[TestMethod]
		public void GetPagesShouldNotReturnDeletedPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				page.IsDeleted = true;
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages().ToList();

				Assert.AreEqual(1, context.Pages.Count());
				Assert.AreEqual(1, context.Pages.Count(x => x.IsDeleted));
				Assert.AreEqual(0, actual.Count);
			}
		}

		[TestMethod]
		public void GetPagesShouldOnlyReturnPublicPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				var settings = new SettingsView { EnablePublicTag = true, LdapConnectionString = string.Empty };
				TestHelper.AddSettings(context, user, settings);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
				TestHelper.AddPage(context, "Public Page", "Hello Real World", john, "myTag", "public");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages().ToList();

				Assert.AreEqual(2, context.Pages.Count());
				Assert.AreEqual(1, actual.Count);
				Assert.AreEqual("Public Page", actual[0].Title);
			}
		}

		#endregion
	}
}