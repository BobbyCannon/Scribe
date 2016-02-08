#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestR.Helpers;
using TestR.PowerShell;

#endregion

namespace Scribe.IntegrationTests
{
	[TestClass]
	public class PageTests : BrowserTestCmdlet
	{
		#region Constants

		private const string TestSite = "http://localhost";

		#endregion

		#region Methods

		[TestMethod]
		public void AddPage()
		{
			ForEachBrowser(browser =>
			{
				using (var context = TestHelper.GetContext())
				{
					var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
					TestHelper.AddDefaultSettings(context, user);
					TestHelper.AddUser(context, "John Doe", "Password!");
					context.SaveChanges();
				}

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				browser.NavigateTo($"{TestSite}/NewPage");
				browser.Elements.TextInputs["pageTitle"].Text = "My Page";
				browser.Elements.TextArea["pageText"].Text = "The quick brown fox jumped over the lazy dog's back.";
				browser.Elements.TextInputs["addTag"].Text = "New Tag";
				browser.Elements.Links["addTagButton"].Click();

				Utility.Wait(() => browser.Elements.Buttons["saveButton"]["disabled"] == "false");

				browser.Elements.Buttons["saveButton"].Click();
				browser.WaitForNavigation();

				Assert.AreEqual($"{TestSite}/Page/1/MyPage", browser.Uri);
				Assert.AreEqual("My Page", browser.Elements["pageTitle"].Text);
			});
		}

		[TestMethod]
		public void EditPage()
		{
			ForEachBrowser(browser =>
			{
				using (var context = TestHelper.GetContext())
				{
					var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
					TestHelper.AddDefaultSettings(context, user);
					var john = TestHelper.AddUser(context, "John Doe", "Password!");
					TestHelper.AddPage(context, "Hello Page", "Hello World", john, "myTag");
					context.SaveChanges();
				}

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				browser.NavigateTo($"{TestSite}/EditPage/1/ExistingPage");
				browser.Elements.TextInputs["pageTitle"].Text = "My Welcome Page";
				browser.Elements.TextArea["pageText"].Text = "World, hello to you...";
				browser.Elements.Buttons["saveButton"].Click();
				browser.WaitForNavigation();
			});
		}

		#endregion
	}
}