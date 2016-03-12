#region References

using System;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Exceptions;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.UnitTests
{
	[TestClass]
	public class ScribeServiceTests
	{
		#region Methods

		[TestMethod]
		public void BeginEditingPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);

				TestHelper.UpdatePage(context, john, page.ToView(), x =>
				{
					x.Title = "Hello Page2";
					x.Text = "Hello World2";
				});

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				var actual = service.BeginEditingPage(page.Id);

				Assert.AreEqual("Hello Page2", actual.Title);
				Assert.AreEqual("Hello World2", actual.Text);
				Assert.AreEqual("John Doe", actual.EditingBy);
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);

				var entity = context.PageVersions.First(x => x.Id == page.Id);
				entity.EditingBy = user;
				entity.EditingOn = DateTime.Now;
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.CancelEditingPage(page.Id);

				entity = context.PageVersions.First(x => x.Id == page.Id);
				Assert.AreEqual(null, entity.EditingBy);
				Assert.AreEqual(null, entity.EditingById);
				Assert.AreEqual(SqlDateTime.MinValue.Value, entity.EditingOn);
			}
		}

		[TestMethod]
		public void DeleteFile()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeleteFile(file1.Id);
				var actual = service.GetFiles().Results.ToList();

				Assert.AreEqual(1, actual.Count);
				TestHelper.AreEqual(file2.ToView(), actual[0]);
			}
		}

		[TestMethod]
		public void DeleteFileWithInvalidId()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				TestHelper.ExpectedException<Exception>(() => { service.DeleteFile(int.MaxValue); }, "Failed to find the file with the provided ID.");
			}
		}

		[TestMethod]
		public void DeleteFileWithSoftDelete()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeleteFile(file1.Id);
				var actual = service.GetFiles();

				Assert.AreEqual(1, actual.Results.Count());
				TestHelper.AreEqual(file2.ToView(), actual.Results.First());
				Assert.AreEqual(2, context.Files.Count());
				Assert.AreEqual(1, context.Files.Count(x => x.IsDeleted));
			}
		}

		[TestMethod]
		public void DeletePage()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				context.SaveChanges();

				Assert.AreEqual(0, context.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeletePage(page.Id);
			}

			using (var context = provider.GetContext(false))
			{
				Assert.AreEqual(0, context.PageVersions.Count(x => x.Page.IsDeleted));
			}
		}

		[TestMethod]
		public void DeletePageWithHistory()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Hello Page2");
				var page2 = TestHelper.AddPage(context, "Another Page", "Yep", john);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Another Page2");

				Assert.AreEqual(0, context.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeletePage(page.Id);

				Assert.AreEqual(1, context.Pages.Count());
				Assert.AreEqual(1, context.PageVersions.Count());
				Assert.AreEqual(0, context.Pages.Count(x => x.Id == page.Id));
				Assert.AreEqual(0, context.PageVersions.Count(x => x.PageId == page.Id));
			}
		}

		[TestMethod]
		public void DeletePageWithInvalidId()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeletePage(int.MaxValue);
			}
		}

		[TestMethod]
		public void DeletePageWithSoftDelete()
		{
			var provider = TestHelper.GetContextProvider();
			PageVersion pageVersion;

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				pageVersion = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				context.SaveChanges();

				Assert.AreEqual(0, context.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeletePage(pageVersion.Id);
			}

			using (var context = provider.GetContext(false))
			{
				Assert.AreEqual(1, context.PageVersions.Count(x => x.Page.IsDeleted));
				Assert.AreEqual(pageVersion.Id, context.PageVersions.First(x => x.Page.IsDeleted).Id);
			}
		}

		[TestMethod]
		public void DeletePageWithSoftDeleteWithHistory()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Hello Page2");

				Assert.AreEqual(0, context.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeletePage(page.Id);

				var actual = context.PageVersions.Where(x => x.Page.IsDeleted).ToList();
				Assert.AreEqual(2, actual.Count);
				Assert.AreEqual(1, actual[0].Id);
				Assert.AreEqual(2, actual[1].Id);
			}
		}

		[TestMethod]
		public void DeleteTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3", "Tag4");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Tags = new[] { "Tag1", "Tag2", "Tag3" });
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.DeleteTag("Tag2");

				var actual = context.PageVersions.ToList();
				Assert.AreEqual(3, actual.Count);
				Assert.AreEqual(",Tag1,Tag2,Tag3,Tag4,", actual[0].Tags);
				Assert.AreEqual(",Tag1,Tag3,", actual[1].Tags);
				Assert.AreEqual(",Tag1,", actual[2].Tags);
			}
		}

		[TestMethod]
		public void DeleteTagEmpty()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, null);
				var service = new ScribeService(context, null, searchService, john);
				TestHelper.ExpectedException<Exception>(() => service.DeleteTag(string.Empty), "The tag name must be provided.");
			}
		}

		[TestMethod]
		public void GetFileById()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFile(file1.Id);

				TestHelper.AreEqual(file1.ToView(), actual);
			}
		}

		[TestMethod]
		public void GetFiles()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFiles();

				Assert.AreEqual(2, actual.Results.Count());
				TestHelper.AreEqual(file1.ToView(), actual.Results.First());
				TestHelper.AreEqual(file2.ToView(), actual.Results.Last());
			}
		}

		[TestMethod]
		public void GetFrontPageNonPublic()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = false });
				var page = TestHelper.AddPage(context, "Front Page", "Hello World", user, ApprovalStatus.Approved, true, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Front Page2", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual("Front Page2", actual.Title);
			}
		}

		[TestMethod]
		public void GetFrontPagePublic()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				TestHelper.AddPage(context, "Front Page1", "Hello World1", user);
				var page2 = TestHelper.AddPage(context, "Front Page2", "Hello World2", user, ApprovalStatus.Approved, true, true);
				TestHelper.UpdatePage(context, user, page2.ToView(), x => x.Title = "New Front Page2", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual("New Front Page2", actual.Title);
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual(page.Title, actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifference()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user);
				TestHelper.UpdatePage(context, user, page1.ToView(), x =>
				{
					x.Title = "Page2";
					x.Text = "Hello...";
				});

				var service = new ScribeService(context, null, null, null);
				var pageId = context.PageVersions.First(x => x.Title == "Page2").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page1</del><ins class='diffmod'>Page2</ins></p>\n", actual.Title);
				Assert.AreEqual("<p><del class='diffmod'>Hello World</del><ins class='diffmod'>Hello...</ins></p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestCurrentVersion()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page4", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var pageId = context.PageVersions.First(x => x.Title == "Page4").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page2</del><ins class='diffmod'>Page4</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestFirstEditedVersion()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page4");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page5", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var pageId = context.PageVersions.First(x => x.Title == "Page3").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page1</del><ins class='diffmod'>Page3</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestLastEditedVersion()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page4");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page5", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var pageId = context.PageVersions.First(x => x.Title == "Page5").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page3</del><ins class='diffmod'>Page5</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestSecondEditedVersion()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page4");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page5", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPageDifference(context.PageVersions.First(x => x.Title == "Page3").Id);

				Assert.AreEqual("<p><del class='diffmod'>Page1</del><ins class='diffmod'>Page3</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestUnpublishedVersion()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page4", ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var pageId = context.PageVersions.First(x => x.Title == "Page3").Id;

				TestHelper.ExpectedException<PageNotFoundException>(() => service.GetPageDifference(pageId), "Failed to find the page with that version ID.");
			}
		}

		[TestMethod]
		public void GetPageDifferenceOfFirstPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user);
				TestHelper.UpdatePage(context, user, page1.ToView(), x =>
				{
					x.Title = "Page2";
					x.Text = "Hello...";
				});

				var service = new ScribeService(context, null, null, null);
				var pageId = context.PageVersions.First(x => x.Title == "Page1").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("Page1", actual.Title);
				Assert.AreEqual("<p>Hello World</p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPageHistory()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user);
				var anotherPage = TestHelper.AddPage(context, "Another Page", "Hello World", user);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(context, user, anotherPage.ToView(), x => x.Title = "Another Page2");
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page3");

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPageHistory(page1.Id);

				Assert.AreEqual("Page3", actual.Title);
				var actualVersions = actual.Versions.ToList();
				Assert.AreEqual(3, actualVersions.Count);
				Assert.AreEqual(5, actualVersions[0].Id);
				Assert.AreEqual(3, actualVersions[0].Number);
				Assert.AreEqual(3, actualVersions[1].Id);
				Assert.AreEqual(2, actualVersions[1].Number);
				Assert.AreEqual(1, actualVersions[2].Id);
				Assert.AreEqual(1, actualVersions[2].Number);
			}
		}

		[TestMethod]
		public void GetPageHistoryForGuess()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page4");

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPageHistory(page1.Id);

				Assert.AreEqual("Page3", actual.Title);
				var actualVersions = actual.Versions.ToList();
				Assert.AreEqual(2, actualVersions.Count);
				Assert.AreEqual(context.PageVersions.First(x => x.Title == "Page3").Id, actualVersions[0].Id);
				Assert.AreEqual(context.PageVersions.First(x => x.Title == "Page1").Id, actualVersions[1].Id);
			}
		}

		[TestMethod]
		public void GetPageHistoryWithSomeApproved()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddDefaultSettings(context, user);
				var page1 = TestHelper.AddPage(context, "Page1", "Hello World", user);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page1.ToView(), x => x.Title = "Page3");

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPageHistory(page1.Id);

				Assert.AreEqual("Page3", actual.Title);
				var actualVersions = actual.Versions.ToList();
				Assert.AreEqual(3, actualVersions.Count);
				Assert.AreEqual(3, actualVersions[0].Id);
				Assert.AreEqual(2, actualVersions[1].Id);
				Assert.AreEqual(1, actualVersions[2].Id);
			}
		}

		[TestMethod]
		public void GetPageInvalidId()
		{
			using (var context = TestHelper.GetContext())
			{
				var service = new ScribeService(context, null, null, null);
				TestHelper.ExpectedException<Exception>(() => service.GetPage(int.MaxValue), "Failed to find the page with that ID.");
			}
		}

		[TestMethod]
		public void GetPageOfPublishedPageWithLinkToPublishedPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(context, "Page1", "Hello, [Page2](Page2)", user, ApprovalStatus.Approved, true);
				TestHelper.AddPage(context, "Page2", "Hello World", user, ApprovalStatus.Approved, true);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page1", actual.Title);
				Assert.AreEqual("<p>Hello, <a href=\"/Page/2/Page2\">Page2</a></p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPageOfPublishedPageWithLinkToUnpublishedPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(context, "Page1", "Hello, [Page2](Page2)", user, ApprovalStatus.Approved, true);
				TestHelper.AddPage(context, "Page2", "Hello World", user);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page1", actual.Title);
				Assert.AreEqual("<p>Hello, <a href=\"/NewPage?suggestedTitle=Page2\" class=\"missing-page-link\">Page2</a></p>\n", actual.Html);
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				page.EditingById = john.Id;
				context.SaveChanges();

				var expectedEditingOn = DateTime.UtcNow;
				var actualEntity = context.PageVersions.First(x => x.Id == page.Id);
				Assert.AreEqual(SqlDateTime.MinValue, actualEntity.EditingOn);

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				var actual = service.GetPagePreview(page.ToView());
				actualEntity = context.PageVersions.First(x => x.Id == page.Id);

				Assert.AreEqual("<p>Hello World</p>\n", actual);
				Assert.IsTrue(actualEntity.EditingOn >= expectedEditingOn);
			}
		}

		[TestMethod]
		public void GetPagePublishWithOnlyApprovedHistory()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(context, "Test", "Hello World", user, ApprovalStatus.Approved, true, false, "tag");
				TestHelper.UpdatePage(context, user, page.ToView(), x =>
				{
					x.Title = "Test2";
					x.Text = "Hello World2";
				});

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				TestHelper.AreEqual(new[] { "tag" }, actual.Tags);
				Assert.AreEqual("Hello World", actual.Text);
				Assert.AreEqual("Test", actual.Title);
				Assert.AreEqual(ApprovalStatus.Approved, actual.ApprovalStatus);
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Hello Page2");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Hello Page3");
				var page2 = TestHelper.AddPage(context, "Another Page", "Hello World", john);
				TestHelper.UpdatePage(context, user, page2.ToView(), x => x.Title = "Another Page2");
				TestHelper.AddPage(context, "More Page", "Hello World", john);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages().Results.ToList();

				Assert.AreEqual(3, actual.Count);
				Assert.AreEqual("Another Page2", actual[0].Title);
				Assert.AreEqual("Hello Page3", actual[1].Title);
				Assert.AreEqual("More Page", actual[2].Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingStatusOfApproved()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", user);
				TestHelper.AddPage(context, "Page2", "Hello World", user, ApprovalStatus.Pending);
				TestHelper.AddPage(context, "Page3", "Hello World", user, ApprovalStatus.Approved);
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				var actual = service.GetPages(new PagedRequest("Status=Approved"));

				Assert.AreEqual("Status=Approved", actual.Filter);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Page3", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingStatusOfPending()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", user);
				TestHelper.AddPage(context, "Page2", "Hello World", user, ApprovalStatus.Pending);
				TestHelper.AddPage(context, "Page3", "Hello World", user, ApprovalStatus.Approved);
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetPages(new PagedRequest("Status=Pending"));

				Assert.AreEqual("Status=Pending", actual.Filter);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Page2", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");
				TestHelper.AddPage(context, "Page3", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag3");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetPages(new PagedRequest("Tags=Tag3"));

				Assert.AreEqual("Tags=Tag3", actual.Filter);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Page1", actual.Results.First().Title);
				Assert.AreEqual("Page3", actual.Results.Last().Title);
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
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				page.Page.IsDeleted = true;
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(1, context.PageVersions.Count());
				Assert.AreEqual(1, context.PageVersions.Count(x => x.Page.IsDeleted));
				Assert.AreEqual(0, actual.Results.Count());
			}
		}

		[TestMethod]
		public void GetPagesShouldOnlyReturnPublishedPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				var settings = new SettingsView { EnableGuestMode = true, LdapConnectionString = string.Empty };
				TestHelper.AddSettings(context, user, settings);
				TestHelper.AddPage(context, "Hello Page", "Hello World", user);
				TestHelper.AddPage(context, "Public Page", "Hello Real World", user, ApprovalStatus.Approved, true);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(2, context.PageVersions.Count());
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Public Page", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithFilter()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest("Page 5"));

				Assert.AreEqual("Page 5", actual.Filter);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Hello Page 5", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrder()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "First Page", "Hello World", john);
				TestHelper.AddPage(context, "Second Page", "Hello World", john);
				TestHelper.AddPage(context, "Third Page", "Hello World", john);
				TestHelper.AddPage(context, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(context, "Fifth Page", "Hello World", john);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(order: "Title"));

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Title", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("Fifth Page", results[0].Title);
				Assert.AreEqual("First Page", results[1].Title);
				Assert.AreEqual("Fourth Page", results[2].Title);
				Assert.AreEqual("Second Page", results[3].Title);
				Assert.AreEqual("Third Page", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderDescending()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "First Page", "Hello World", john);
				TestHelper.AddPage(context, "Second Page", "Hello World", john);
				TestHelper.AddPage(context, "Third Page", "Hello World", john);
				TestHelper.AddPage(context, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(context, "Fifth Page", "Hello World", john);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(order: "Title=Descending"));

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Title=Descending", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("Third Page", results[0].Title);
				Assert.AreEqual("Second Page", results[1].Title);
				Assert.AreEqual("Fourth Page", results[2].Title);
				Assert.AreEqual("First Page", results[3].Title);
				Assert.AreEqual("Fifth Page", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderAscendingThenByDescending()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "a", "a", john);
				TestHelper.AddPage(context, "c", "a", john);
				TestHelper.AddPage(context, "b", "c", john);
				TestHelper.AddPage(context, "e", "b", john);
				TestHelper.AddPage(context, "d", "b", john);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(order: "Text=Ascending;Title=Descending;"));

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Text=Ascending;Title=Descending;", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("c", results[0].Title);
				Assert.AreEqual("a", results[1].Title);
				Assert.AreEqual("e", results[2].Title);
				Assert.AreEqual("d", results[3].Title);
				Assert.AreEqual("b", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderDescendingThenByAscending()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "a", "a", john);
				TestHelper.AddPage(context, "c", "a", john);
				TestHelper.AddPage(context, "b", "c", john);
				TestHelper.AddPage(context, "e", "b", john);
				TestHelper.AddPage(context, "d", "b", john);

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(order: "Text=Descending;Title=Ascending;"));

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Text=Descending;Title=Ascending;", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("b", results[0].Title);
				Assert.AreEqual("d", results[1].Title);
				Assert.AreEqual("e", results[2].Title);
				Assert.AreEqual("a", results[3].Title);
				Assert.AreEqual("c", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagedFilter()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					if (i % 2 == 0)
					{
						TestHelper.AddPage(context, "Even Page " + i, "Hello World", john);
					}
					else
					{
						TestHelper.AddPage(context, "Odd Page " + i, "Hello World", john);
					}
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest("Odd", 1, 2));

				Assert.AreEqual("Odd", actual.Filter);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(2, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Odd Page 1", actual.Results.First().Title);
				Assert.AreEqual("Odd Page 3", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagedFilterSecondPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					if (i % 2 == 0)
					{
						TestHelper.AddPage(context, "Even Page " + i, "Hello World", john);
					}
					else
					{
						TestHelper.AddPage(context, "Odd Page " + i, "Hello World", john);
					}
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest("Odd", 2, 2));

				Assert.AreEqual("Odd", actual.Filter);
				Assert.AreEqual(2, actual.Page);
				Assert.AreEqual(2, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Odd Page 5", actual.Results.First().Title);
				Assert.AreEqual("Odd Page 7", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPageRequestOutOfRange()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 30, perPage: 5));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(2, actual.Page);
				Assert.AreEqual(5, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(2, actual.TotalPages);
				Assert.AreEqual(4, actual.Results.Count());
				Assert.AreEqual("Hello Page 6", actual.Results.First().Title);
				Assert.AreEqual("Hello Page 9", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagingFirstPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 1, perPage: 4));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(4, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(4, actual.Results.Count());
				Assert.AreEqual("Hello Page 1", actual.Results.First().Title);
				Assert.AreEqual("Hello Page 4", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagingLastPartialPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 3, perPage: 4));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(3, actual.Page);
				Assert.AreEqual(4, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Hello Page 9", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagingSecondPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 2, perPage: 4));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(2, actual.Page);
				Assert.AreEqual(4, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(4, actual.Results.Count());
				Assert.AreEqual("Hello Page 5", actual.Results.First().Title);
				Assert.AreEqual("Hello Page 8", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPageWithHistory()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page4");

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page4", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageWithHistoryAsGuest()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnableGuestMode = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Title = "Page4");

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page2", actual.Title);
			}
		}

		[TestMethod]
		public void GetTags()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3", "Tag4");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Tags = new[] { "Tag1", "Tag2", "Tag3" });
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");
				TestHelper.AddPage(context, "Page3", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag3");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetTags(new PagedRequest("Tag3")).Results.Select(x => x.Tag).ToArray();

				Assert.AreEqual(3, actual.Length);
				TestHelper.AreEqual(new[] { "Tag1", "Tag2", "Tag3" }, actual);
			}
		}

		[TestMethod]
		public void GetUsers()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user);
				var actual = service.GetUsers();

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Administrator", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void GetUsersFilterUsingTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user1 = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddUser(context, "John Doe", "Password1", "foo");
				TestHelper.AddUser(context, "Jane Smith", "Password2", "bar");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user1);
				var actual = service.GetUsers(new PagedRequest("Tags=foo"));

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("JohnDoe", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void GetUsersFilterUsingUserName()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user1 = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddUser(context, "John Doe", "Password1", "foo");
				TestHelper.AddUser(context, "Jane Smith", "Password2", "bar");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user1);
				var actual = service.GetUsers(new PagedRequest("UserName=Jane"));

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("JaneSmith", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void RenameTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3", "Tag4");
				TestHelper.UpdatePage(context, user, page.ToView(), x => x.Tags = new[] { "Tag1", "Tag2", "Tag3" });
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");
				context.SaveChanges();

				var service = new ScribeService(context, null, TestHelper.GetSearchService(), john);
				service.RenameTag(new RenameValues { OldName = "Tag2", NewName = "TagTwo" });

				var actual = service.GetTags().Results.Select(x => x.Tag).ToArray();
				Assert.AreEqual(3, actual.Length);
				TestHelper.AreEqual(new[] { "Tag1", "Tag3", "TagTwo" }, actual);

				var firstPage1 = context.PageVersions.First();
				Assert.AreEqual(1, firstPage1.Id);
				Assert.AreEqual("Page1", firstPage1.Title);
				Assert.AreEqual(",Tag1,Tag2,Tag3,Tag4,", firstPage1.Tags);
			}
		}

		[TestMethod]
		public void SavePage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), user);
				var actualView = service.SavePage(input);

				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);

				var actualEntity = context.PageVersions.First();
				Assert.AreEqual(input.Title, actualEntity.Title);
				Assert.AreEqual(input.Text, actualEntity.Text);
			}
		}

		[TestMethod]
		public void SavePageForHistory()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				context.SaveChanges();

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), user);
				var actualView = service.SavePage(input);

				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);

				var actualEntity = context.PageVersions.First();
				Assert.AreEqual(input.Title, actualEntity.Title);
				Assert.AreEqual(input.Text, actualEntity.Text);
				Assert.AreEqual(1, actualEntity.Page.Versions.Count);

				input.Id = actualEntity.Id;
				input.Text = "Boom, nope.";

				actualView = service.SavePage(input);
				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);
				Assert.AreEqual(2, actualEntity.Page.Versions.Count);
			}
		}

		[TestMethod]
		public void SavePageWithDuplicateTitles()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				TestHelper.AddPage(context, "Title", "Hello World", user);

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(context, null, TestHelper.GetSearchService(), user);
				TestHelper.ExpectedException<InvalidOperationException>(() => service.SavePage(input), "This page title is already used.");
			}
		}

		[TestMethod]
		public void SaveUser()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user);
				var view = service.GetUser(user.Id);

				view.DisplayName = "Jane Smith";
				view.UserName = "JaneSmith";
				view.Tags = new[] { "approver", "publisher" };

				var actual = service.SaveUser(view);

				Assert.AreEqual("Jane Smith", actual.DisplayName);
				Assert.AreEqual("JaneSmith", actual.UserName);
				Assert.AreEqual(2, actual.Tags.Count());
				Assert.AreEqual("approver", actual.Tags.First());
				Assert.AreEqual("publisher", actual.Tags.Last());
			}
		}

		[TestMethod]
		public void UpdatePageForApproval()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "approver");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Approve });
				Assert.AreEqual(ApprovalStatus.Approved, actualView.ApprovalStatus);

				var actualEntity = context.PageVersions.First();
				Assert.AreEqual(ApprovalStatus.Approved, actualEntity.ApprovalStatus);
			}
		}

		[TestMethod]
		public void UpdatePageForApprovalWithoutTag()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				TestHelper.ExpectedException<UnauthorizedAccessException>(() => service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Approve }), "You do not have the permission to update this page.");
			}
		}

		[TestMethod]
		public void UpdatePageForPublish()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "publisher");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Publish });
				Assert.IsTrue(actualView.IsPublished);

				var actualEntity = context.PageVersions.First();
				Assert.IsTrue(actualEntity.IsPublished);
			}
		}

		[TestMethod]
		public void UpdatePageForRejected()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "approver");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Reject });
				Assert.AreEqual(ApprovalStatus.Rejected, actualView.ApprovalStatus);

				var actualEntity = context.PageVersions.First();
				Assert.AreEqual(ApprovalStatus.Rejected, actualEntity.ApprovalStatus);
			}
		}

		[TestMethod]
		public void UpdatePageForRejectedWithoutTag()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				TestHelper.ExpectedException<UnauthorizedAccessException>(() => service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Reject }), "You do not have the permission to update this page.");
			}
		}

		[TestMethod]
		public void UpdatePageForSetHomePage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.SetHomepage });

				var actualEntity = context.PageVersions.First();
				Assert.IsTrue(actualEntity.Page.IsHomePage);
			}
		}

		[TestMethod]
		public void UpdatePageForSetHomePageWithoutTag()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				TestHelper.ExpectedException<UnauthorizedAccessException>(() => service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.SetHomepage }), "You do not have the permission to update this page.");
			}
		}

		[TestMethod]
		public void UpdatePageForUnpublish()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "publisher");
				var page = TestHelper.AddPage(context, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(context, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Unpublish });
				Assert.IsFalse(actualView.IsPublished);

				var actualEntity = context.PageVersions.First();
				Assert.IsFalse(actualEntity.IsPublished);
			}
		}

		#endregion
	}
}