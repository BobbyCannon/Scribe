#region References

using System;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Data.Entities;
using Scribe.Exceptions;
using Scribe.Models.Data;
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);

				TestHelper.UpdatePage(database, john, page.ToView(), x =>
				{
					x.Title = "Hello Page2";
					x.Text = "Hello World2";
				});

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				var actual = service.BeginEditingPage(page.Id);

				Assert.AreEqual("Hello Page2", actual.Title);
				Assert.AreEqual("Hello World2", actual.Text);
				Assert.AreEqual("John Doe", actual.EditingBy);
			}
		}

		[TestMethod]
		public void CancelEditingPage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);

				var entity = database.PageVersions.First(x => x.Id == page.Id);
				entity.EditingBy = user;
				entity.EditingOn = DateTime.Now;
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.CancelEditingPage(page.Id);

				entity = database.PageVersions.First(x => x.Id == page.Id);
				Assert.AreEqual(null, entity.EditingBy);
				Assert.AreEqual(null, entity.EditingById);
				Assert.AreEqual(SqlDateTime.MinValue.Value, entity.EditingOn);
			}
		}

		[TestMethod]
		public void DeleteFile()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(database, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(database, john, "File2.png", "image/png", new byte[0]);
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeleteFile(file1.Id);
				var actual = service.GetFiles().Results.ToList();

				Assert.AreEqual(1, actual.Count);
				TestHelper.AreEqual(file2.ToView(), actual[0]);
			}
		}

		[TestMethod]
		public void DeleteFileWithInvalidId()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				TestHelper.ExpectedException<Exception>(() => { service.DeleteFile(int.MaxValue); }, "Failed to find the file with the provided ID.");
			}
		}

		[TestMethod]
		public void DeleteFileWithSoftDelete()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(database, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(database, john, "File2.png", "image/png", new byte[0]);
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeleteFile(file1.Id);
				var actual = service.GetFiles();

				Assert.AreEqual(1, actual.Results.Count());
				TestHelper.AreEqual(file2.ToView(), actual.Results.First());
				Assert.AreEqual(2, database.Files.Count());
				Assert.AreEqual(1, database.Files.Count(x => x.IsDeleted));
			}
		}

		[TestMethod]
		public void DeletePage()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				database.SaveChanges();

				Assert.AreEqual(0, database.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeletePage(page.Id);
			}

			using (var database = provider.GetDatabase())
			{
				Assert.AreEqual(0, database.PageVersions.Count(x => x.Page.IsDeleted));
			}
		}

		[TestMethod]
		public void DeletePageWithHistory()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Hello Page2");
				var page2 = TestHelper.AddPage(database, "Another Page", "Yep", john);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Another Page2");

				Assert.AreEqual(0, database.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeletePage(page.Id);

				Assert.AreEqual(1, database.Pages.Count());
				Assert.AreEqual(1, database.PageVersions.Count());
				Assert.AreEqual(0, database.Pages.Count(x => x.Id == page.Id));
				Assert.AreEqual(0, database.PageVersions.Count(x => x.PageId == page.Id));
			}
		}

		[TestMethod]
		public void DeletePageWithInvalidId()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeletePage(int.MaxValue);
			}
		}

		[TestMethod]
		public void DeletePageWithSoftDelete()
		{
			var provider = TestHelper.GetDatabaseProvider();
			PageVersion pageVersion;

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				pageVersion = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				database.SaveChanges();

				Assert.AreEqual(0, database.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeletePage(pageVersion.Id);
			}

			using (var database = provider.GetDatabase())
			{
				Assert.AreEqual(1, database.PageVersions.Count(x => x.Page.IsDeleted));
				Assert.AreEqual(pageVersion.Id, database.PageVersions.First(x => x.Page.IsDeleted).Id);
			}
		}

		[TestMethod]
		public void DeletePageWithSoftDeleteWithHistory()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Hello Page2");

				Assert.AreEqual(0, database.PageVersions.Count(x => x.Page.IsDeleted));
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeletePage(page.Id);

				var actual = database.PageVersions.Where(x => x.Page.IsDeleted).ToList();
				Assert.AreEqual(2, actual.Count);
				Assert.AreEqual(1, actual[0].Id);
				Assert.AreEqual(2, actual[1].Id);
			}
		}

		[TestMethod]
		public void DeleteTag()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3", "Tag4");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Tags = new[] { "Tag1", "Tag2", "Tag3" });
				TestHelper.AddPage(database, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.DeleteTag("Tag2");

				var actual = database.PageVersions.ToList();
				Assert.AreEqual(3, actual.Count);
				Assert.AreEqual(",Tag1,Tag2,Tag3,Tag4,", actual[0].Tags);
				Assert.AreEqual(",Tag1,Tag3,", actual[1].Tags);
				Assert.AreEqual(",Tag1,", actual[2].Tags);
			}
		}

		[TestMethod]
		public void DeleteTagEmpty()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(database, path, null);
				var service = new ScribeService(database, null, searchService, john);
				TestHelper.ExpectedException<Exception>(() => service.DeleteTag(string.Empty), "The tag name must be provided.");
			}
		}

		[TestMethod]
		public void GetFileById()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(database, john, "File1.png", "image/png", new byte[0]);
				TestHelper.AddFile(database, john, "File2.png", "image/png", new byte[0]);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetFile(file1.Id);

				TestHelper.AreEqual(file1.ToView(), actual);
			}
		}

		[TestMethod]
		public void GetFiles()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(database, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(database, john, "File2.png", "image/png", new byte[0]);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetFiles();

				Assert.AreEqual(2, actual.Results.Count());
				TestHelper.AreEqual(file1.ToView(), actual.Results.First());
				TestHelper.AreEqual(file2.ToView(), actual.Results.Last());
			}
		}

		[TestMethod]
		public void GetFrontPageNonPublic()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				var page = TestHelper.AddPage(database, "Front Page", "Hello World", user, ApprovalStatus.Approved, true, true);
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = false, FrontPagePrivateId = page.Id });
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Front Page2", ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual("Front Page2", actual.Title);
			}
		}

		[TestMethod]
		public void GetFrontPagePublicApprovedNotPublishEdit()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				TestHelper.AddPage(database, "Front Page1", "Hello World1", user);
				var page2 = TestHelper.AddPage(database, "Front Page2", "Hello World2", user, ApprovalStatus.Approved, true, true);
				TestHelper.UpdatePage(database, user, page2.ToView(), x => x.Title = "New Front Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page2.ToView(), x => x.Title = "New Front Page3", ApprovalStatus.Approved, false);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual("New Front Page2", actual.Title);
			}
		}

		[TestMethod]
		public void GetFrontPagePublicNotApprovedNotPublishEdit()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				TestHelper.AddPage(database, "Front Page1", "Hello World1", user);
				var page2 = TestHelper.AddPage(database, "Front Page2", "Hello World2", user, ApprovalStatus.Approved, true, true);
				TestHelper.UpdatePage(database, user, page2.ToView(), x => x.Title = "New Front Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page2.ToView(), x => x.Title = "New Front Page3", ApprovalStatus.None, false);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual("New Front Page2", actual.Title);
			}
		}

		[TestMethod]
		public void GetPage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual(page.Title, actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifference()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user);
				TestHelper.UpdatePage(database, user, page1.ToView(), x =>
				{
					x.Title = "Page2";
					x.Text = "Hello...";
				});

				var service = new ScribeService(database, null, null, null);
				var pageId = database.PageVersions.First(x => x.Title == "Page2").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page1</del><ins class='diffmod'>Page2</ins></p>\n", actual.Title);
				Assert.AreEqual("<p><del class='diffmod'>Hello World</del><ins class='diffmod'>Hello...</ins></p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestCurrentVersion()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page4", ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var pageId = database.PageVersions.First(x => x.Title == "Page4").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page2</del><ins class='diffmod'>Page4</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestFirstEditedVersion()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(database, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page4");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page5", ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var pageId = database.PageVersions.First(x => x.Title == "Page3").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page1</del><ins class='diffmod'>Page3</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestLastEditedVersion()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(database, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page4");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page5", ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var pageId = database.PageVersions.First(x => x.Title == "Page5").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("<p><del class='diffmod'>Page3</del><ins class='diffmod'>Page5</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestSecondEditedVersion()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(database, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page4");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page5", ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPageDifference(database.PageVersions.First(x => x.Title == "Page3").Id);

				Assert.AreEqual("<p><del class='diffmod'>Page1</del><ins class='diffmod'>Page3</ins></p>\n", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageDifferenceForGuestUnpublishedVersion()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page4", ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var pageId = database.PageVersions.First(x => x.Title == "Page3").Id;

				TestHelper.ExpectedException<PageNotFoundException>(() => service.GetPageDifference(pageId), "Failed to find the page with that version ID.");
			}
		}

		[TestMethod]
		public void GetPageDifferenceOfFirstPage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user);
				TestHelper.UpdatePage(database, user, page1.ToView(), x =>
				{
					x.Title = "Page2";
					x.Text = "Hello...";
				});

				var service = new ScribeService(database, null, null, null);
				var pageId = database.PageVersions.First(x => x.Title == "Page1").Id;
				var actual = service.GetPageDifference(pageId);

				Assert.AreEqual("Page1", actual.Title);
				Assert.AreEqual("<p>Hello World</p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPageHistory()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user);
				var anotherPage = TestHelper.AddPage(database, "Another Page", "Hello World", user);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(database, user, anotherPage.ToView(), x => x.Title = "Another Page2");
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page3");

				var service = new ScribeService(database, null, null, null);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user, ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page3", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page4");

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPageHistory(page1.Id);

				Assert.AreEqual("Page3", actual.Title);
				var actualVersions = actual.Versions.ToList();
				Assert.AreEqual(2, actualVersions.Count);
				Assert.AreEqual(database.PageVersions.First(x => x.Title == "Page3").Id, actualVersions[0].Id);
				Assert.AreEqual(database.PageVersions.First(x => x.Title == "Page1").Id, actualVersions[1].Id);
			}
		}

		[TestMethod]
		public void GetPageHistoryWithSomeApproved()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddDefaultSettings(database);
				var page1 = TestHelper.AddPage(database, "Page1", "Hello World", user);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page1.ToView(), x => x.Title = "Page3");

				var service = new ScribeService(database, null, null, null);
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
			using (var database = TestHelper.GetDatabase())
			{
				var service = new ScribeService(database, null, null, null);
				TestHelper.ExpectedException<Exception>(() => service.GetPage(int.MaxValue), "Failed to find the page with that ID.");
			}
		}

		[TestMethod]
		public void GetPageOfPublishedPageWithLinkToPublishedPage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(database, "Page1", "Hello, [Page2](Page2)", user, ApprovalStatus.Approved, true);
				TestHelper.AddPage(database, "Page2", "Hello World", user, ApprovalStatus.Approved, true);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page1", actual.Title);
				Assert.AreEqual("<p>Hello, <a href=\"/Page/2/Page2\">Page2</a></p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPageOfPublishedPageWithLinkToUnpublishedPage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(database, "Page1", "Hello, [Page2](Page2)", user, ApprovalStatus.Approved, true);
				TestHelper.AddPage(database, "Page2", "Hello World", user);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page1", actual.Title);
				Assert.AreEqual("<p>Hello, <a href=\"/NewPage?suggestedTitle=Page2\" class=\"missing-page-link\">Page2</a></p>\n", actual.Html);
			}
		}

		[TestMethod]
		public void GetPagePreview()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				page.EditingById = john.Id;
				database.SaveChanges();

				var expectedEditingOn = DateTime.UtcNow;
				var actualEntity = database.PageVersions.First(x => x.Id == page.Id);
				Assert.AreEqual(SqlDateTime.MinValue, actualEntity.EditingOn);

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				var actual = service.GetPagePreview(page.ToView());
				actualEntity = database.PageVersions.First(x => x.Id == page.Id);

				Assert.AreEqual("<p>Hello World</p>\n", actual);
				Assert.IsTrue(actualEntity.EditingOn >= expectedEditingOn);
			}
		}

		[TestMethod]
		public void GetPagePublishWithOnlyApprovedHistory()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var page = TestHelper.AddPage(database, "Test", "Hello World", user, ApprovalStatus.Approved, true, false, "tag");
				TestHelper.UpdatePage(database, user, page.ToView(), x =>
				{
					x.Title = "Test2";
					x.Text = "Hello World2";
				});

				var service = new ScribeService(database, null, null, null);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Hello Page2");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Hello Page3");
				var page2 = TestHelper.AddPage(database, "Another Page", "Hello World", john);
				TestHelper.UpdatePage(database, user, page2.ToView(), x => x.Title = "Another Page2");
				TestHelper.AddPage(database, "More Page", "Hello World", john);

				var service = new ScribeService(database, null, null, null);
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
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "Page1", "Hello World", user);
				TestHelper.AddPage(database, "Page2", "Hello World", user, ApprovalStatus.Pending);
				TestHelper.AddPage(database, "Page3", "Hello World", user, ApprovalStatus.Approved);
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				var values = new object[] { (int) ApprovalStatus.Approved };
				var actual = service.GetPages(new PagedRequest { Filter = "ApprovalStatus == @0", FilterValues = values });

				Assert.AreEqual("ApprovalStatus == @0", actual.Filter);
				TestHelper.AreEqual(values, actual.FilterValues);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Page3", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingStatusOfPending()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "Page1", "Hello World", user);
				TestHelper.AddPage(database, "Page2", "Hello World", user, ApprovalStatus.Pending);
				TestHelper.AddPage(database, "Page3", "Hello World", user, ApprovalStatus.Approved);
				database.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(database, path, john);
				var service = new ScribeService(database, null, searchService, john);
				var values = new object[] { (int)ApprovalStatus.Pending };
				var actual = service.GetPages(new PagedRequest { Filter = "ApprovalStatus == @0", FilterValues = values });

				Assert.AreEqual("ApprovalStatus == @0", actual.Filter);
				TestHelper.AreEqual(values, actual.FilterValues);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Page2", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingTag()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(database, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");
				TestHelper.AddPage(database, "Page3", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag3");
				database.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(database, path, john);
				var service = new ScribeService(database, null, searchService, john);
				var actual = service.GetPages(new PagedRequest { Filter = "Tags.Contains(\"Tag3\")" });

				Assert.AreEqual("Tags.Contains(\"Tag3\")", actual.Filter);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Page1", actual.Results.First().Title);
				Assert.AreEqual("Page3", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesShouldNotReturnDeletedPages()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				page.Page.IsDeleted = true;
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(1, database.PageVersions.Count());
				Assert.AreEqual(1, database.PageVersions.Count(x => x.Page.IsDeleted));
				Assert.AreEqual(0, actual.Results.Count());
			}
		}

		[TestMethod]
		public void GetPagesShouldOnlyReturnPublishedPages()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				var settings = new SettingsView { EnableGuestMode = true, LdapConnectionString = string.Empty };
				TestHelper.AddSettings(database, settings);
				TestHelper.AddPage(database, "Hello Page", "Hello World", user);
				TestHelper.AddPage(database, "Public Page", "Hello Real World", user, ApprovalStatus.Approved, true);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(2, database.PageVersions.Count());
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Public Page", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithFilter()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(database, "Hello Page " + i, "Hello World", john);
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var values = new[] { "Page 5" };
				var actual = service.GetPages(new PagedRequest { Filter = "Title.Contains(@0)", FilterValues = values });

				Assert.AreEqual("Title.Contains(@0)", actual.Filter);
				TestHelper.AreEqual(values, actual.FilterValues);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john);
				TestHelper.AddPage(database, "Second Page", "Hello World", john);
				TestHelper.AddPage(database, "Third Page", "Hello World", john);
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Order = "Title" });

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
		public void GetPagesWithOrderAscendingThenByDescending()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "a", "a", john);
				TestHelper.AddPage(database, "c", "a", john);
				TestHelper.AddPage(database, "b", "c", john);
				TestHelper.AddPage(database, "e", "b", john);
				TestHelper.AddPage(database, "d", "b", john);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Order = "Text, Title descending" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Text, Title descending", actual.Order);
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
		public void GetPagesWithOrderByCreatedOn()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john);
				TestHelper.AddPage(database, "Second Page", "Hello World", john);
				TestHelper.AddPage(database, "Third Page", "Hello World", john);
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Order = "CreatedOn" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("CreatedOn", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("First Page", results[0].Title);
				Assert.AreEqual("Second Page", results[1].Title);
				Assert.AreEqual("Third Page", results[2].Title);
				Assert.AreEqual("Fourth Page", results[3].Title);
				Assert.AreEqual("Fifth Page", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderById()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john);
				TestHelper.AddPage(database, "Second Page", "Hello World", john);
				TestHelper.AddPage(database, "Third Page", "Hello World", john);
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Order = "Id" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Id", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("First Page", results[0].Title);
				Assert.AreEqual("Second Page", results[1].Title);
				Assert.AreEqual("Third Page", results[2].Title);
				Assert.AreEqual("Fourth Page", results[3].Title);
				Assert.AreEqual("Fifth Page", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderByModifiedOn()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john);
				TestHelper.AddPage(database, "Second Page", "Hello World", john);
				TestHelper.AddPage(database, "Third Page", "Hello World", john);
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Order = "ModifiedOn" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("ModifiedOn", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("First Page", results[0].Title);
				Assert.AreEqual("Second Page", results[1].Title);
				Assert.AreEqual("Third Page", results[2].Title);
				Assert.AreEqual("Fourth Page", results[3].Title);
				Assert.AreEqual("Fifth Page", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderByTags()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john, tags: new[] { "One" });
				TestHelper.AddPage(database, "Second Page", "Hello World", john, tags: new[] { "2" });
				TestHelper.AddPage(database, "Third Page", "Hello World", john, tags: new[] { "Three" });
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john, tags: new[] { "4" });
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john, tags: new[] { "Five" });
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Order = "Tags" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Tags", actual.Order);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(5, actual.Results.Count());

				var results = actual.Results.ToArray();
				Assert.AreEqual("Second Page", results[0].Title);
				Assert.AreEqual("Fourth Page", results[1].Title);
				Assert.AreEqual("Fifth Page", results[2].Title);
				Assert.AreEqual("First Page", results[3].Title);
				Assert.AreEqual("Third Page", results[4].Title);
			}
		}

		[TestMethod]
		public void GetPagesWithOrderByTitle()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john);
				TestHelper.AddPage(database, "Second Page", "Hello World", john);
				TestHelper.AddPage(database, "Third Page", "Hello World", john);
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest{ Order = "Title" });

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "First Page", "Hello World", john);
				TestHelper.AddPage(database, "Second Page", "Hello World", john);
				TestHelper.AddPage(database, "Third Page", "Hello World", john);
				TestHelper.AddPage(database, "Fourth Page", "Hello World", john);
				TestHelper.AddPage(database, "Fifth Page", "Hello World", john);
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest{ Order = "Title descending" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Title descending", actual.Order);
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
		public void GetPagesWithOrderDescendingThenByAscending()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				TestHelper.AddPage(database, "a", "a", john);
				TestHelper.AddPage(database, "c", "a", john);
				TestHelper.AddPage(database, "b", "c", john);
				TestHelper.AddPage(database, "e", "b", john);
				TestHelper.AddPage(database, "d", "b", john);

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest{ Order = "Text descending, Title" });

				Assert.AreEqual(string.Empty, actual.Filter);
				Assert.AreEqual("Text descending, Title", actual.Order);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					if (i % 2 == 0)
					{
						TestHelper.AddPage(database, "Even Page " + i, "Hello World", john);
					}
					else
					{
						TestHelper.AddPage(database, "Odd Page " + i, "Hello World", john);
					}
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Filter = "Title.Contains(\"Odd\")", Page = 1, PerPage = 2 });

				Assert.AreEqual("Title.Contains(\"Odd\")", actual.Filter);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					if (i % 2 == 0)
					{
						TestHelper.AddPage(database, "Even Page " + i, "Hello World", john);
					}
					else
					{
						TestHelper.AddPage(database, "Odd Page " + i, "Hello World", john);
					}
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Filter = "Title.Contains(\"Odd\")", Page = 2, PerPage = 2 });

				Assert.AreEqual("Title.Contains(\"Odd\")", actual.Filter);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(database, "Hello Page " + i, "Hello World", john);
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Page = 30, PerPage = 5 });

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(database, "Hello Page " + i, "Hello World", john);
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Page = 1, PerPage = 4 });

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(database, "Hello Page " + i, "Hello World", john);
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Page = 3, PerPage = 4 });

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(database, "Hello Page " + i, "Hello World", john);
				}
				database.SaveChanges();

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPages(new PagedRequest { Page = 2, PerPage = 4 });

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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page2");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page4");

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page4", actual.Title);
			}
		}

		[TestMethod]
		public void GetPageWithHistoryAsGuest()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Hello Page", "Hello World", john);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page2", ApprovalStatus.Approved, true);
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page3");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Title = "Page4");

				var service = new ScribeService(database, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual("Page2", actual.Title);
			}
		}

		[TestMethod]
		public void GetTags()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3", "Tag4");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Tags = new[] { "Tag1", "Tag2", "Tag3" });
				TestHelper.AddPage(database, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");
				TestHelper.AddPage(database, "Page3", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag3");
				database.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(database, path, john);
				var service = new ScribeService(database, null, searchService, john);
				var actual = service.GetTags(new PagedRequest { Filter = "Tag3" }).Results.Select(x => x.Tag).ToArray();

				Assert.AreEqual(3, actual.Length);
				TestHelper.AreEqual(new[] { "Tag1", "Tag2", "Tag3" }, actual);
			}
		}

		[TestMethod]
		public void GetUsers()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				database.SaveChanges();

				var service = new ScribeService(database, null, null, user);
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
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user1 = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddUser(database, "John Doe", "Password1", "foo");
				TestHelper.AddUser(database, "Jane Smith", "Password2", "bar");
				database.SaveChanges();

				var service = new ScribeService(database, null, null, user1);
				var actual = service.GetUsers(new PagedRequest { Filter = "Tags.Contains(\"foo\")" });

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("JohnDoe", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void GetUsersFilterUsingUserName()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user1 = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddUser(database, "John Doe", "Password1", "foo");
				TestHelper.AddUser(database, "Jane Smith", "Password2", "bar");
				database.SaveChanges();

				var service = new ScribeService(database, null, null, user1);
				var actual = service.GetUsers(new PagedRequest { Filter = "UserName == \"JaneSmith\"" });

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("JaneSmith", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void RenameTag()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(database, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Page1", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2", "Tag3", "Tag4");
				TestHelper.UpdatePage(database, user, page.ToView(), x => x.Tags = new[] { "Tag1", "Tag2", "Tag3" });
				TestHelper.AddPage(database, "Page2", "Hello World", john, ApprovalStatus.None, false, false, "Tag1", "Tag2");
				database.SaveChanges();

				var service = new ScribeService(database, null, TestHelper.GetSearchService(), john);
				service.RenameTag(new RenameValues { OldName = "Tag2", NewName = "TagTwo" });

				var actual = service.GetTags().Results.Select(x => x.Tag).ToArray();
				Assert.AreEqual(3, actual.Length);
				TestHelper.AreEqual(new[] { "Tag1", "Tag3", "TagTwo" }, actual);

				var firstPage1 = database.PageVersions.First();
				Assert.AreEqual(1, firstPage1.Id);
				Assert.AreEqual("Page1", firstPage1.Title);
				Assert.AreEqual(",Tag1,Tag2,Tag3,Tag4,", firstPage1.Tags);
			}
		}

		[TestMethod]
		public void SavePage()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), user);
				var actualView = service.SavePage(input);

				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);

				var actualEntity = database.PageVersions.First();
				Assert.AreEqual(input.Title, actualEntity.Title);
				Assert.AreEqual(input.Text, actualEntity.Text);
			}
		}

		[TestMethod]
		public void SavePageForHistory()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				database.SaveChanges();

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), user);
				var actualView = service.SavePage(input);

				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);

				var actualEntity = database.PageVersions.First();
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(database);
				TestHelper.AddPage(database, "Title", "Hello World", user);

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(database, null, TestHelper.GetSearchService(), user);
				TestHelper.ExpectedException<InvalidOperationException>(() => service.SavePage(input), "This page title is already used.");
			}
		}

		[TestMethod]
		public void SaveUser()
		{
			var provider = TestHelper.GetDatabaseProvider();

			using (var database = provider.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "administrator");
				database.SaveChanges();

				var service = new ScribeService(database, null, null, user);
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
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "approver");
				var page = TestHelper.AddPage(database, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(database, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Approve });
				Assert.AreEqual(ApprovalStatus.Approved, actualView.ApprovalStatus);

				var actualEntity = database.PageVersions.First();
				Assert.AreEqual(ApprovalStatus.Approved, actualEntity.ApprovalStatus);
			}
		}

		[TestMethod]
		public void UpdatePageForApprovalWithoutTag()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(database, null, null, user);
				TestHelper.ExpectedException<UnauthorizedAccessException>(() => service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Approve }), "You do not have the permission to update this page.");
			}
		}

		[TestMethod]
		public void UpdatePageForPublish()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "publisher");
				var page = TestHelper.AddPage(database, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(database, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Publish });
				Assert.IsTrue(actualView.IsPublished);

				var actualEntity = database.PageVersions.First();
				Assert.IsTrue(actualEntity.IsPublished);
			}
		}

		[TestMethod]
		public void UpdatePageForRejected()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "approver");
				var page = TestHelper.AddPage(database, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(database, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Reject });
				Assert.AreEqual(ApprovalStatus.Rejected, actualView.ApprovalStatus);

				var actualEntity = database.PageVersions.First();
				Assert.AreEqual(ApprovalStatus.Rejected, actualEntity.ApprovalStatus);
			}
		}

		[TestMethod]
		public void UpdatePageForRejectedWithoutTag()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!");
				var page = TestHelper.AddPage(database, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(database, null, null, user);
				TestHelper.ExpectedException<UnauthorizedAccessException>(() => service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Reject }), "You do not have the permission to update this page.");
			}
		}

		[TestMethod]
		public void UpdatePageForUnpublish()
		{
			using (var database = TestHelper.GetDatabase())
			{
				var user = TestHelper.AddUser(database, "John Doe", "Password!", "publisher");
				var page = TestHelper.AddPage(database, "Title", "Hello World", user, ApprovalStatus.Pending);

				var service = new ScribeService(database, null, null, user);
				var actualView = service.UpdatePage(new PageUpdate { Id = page.Id, Type = PageUpdateType.Unpublish });
				Assert.IsFalse(actualView.IsPublished);

				var actualEntity = database.PageVersions.First();
				Assert.IsFalse(actualEntity.IsPublished);
			}
		}

		#endregion
	}
}