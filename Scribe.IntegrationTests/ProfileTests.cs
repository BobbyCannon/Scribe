#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Web;
using TestR.PowerShell;

#endregion

namespace Scribe.IntegrationTests
{
	[TestClass]
	public class ProfileTests : BrowserTestCmdlet
	{
		#region Constants

		private const string TestSite = "http://localhost";

		#endregion

		#region Methods

		[TestMethod]
		public void Login()
		{
			ForEachBrowser(browser =>
			{
				using (var context = TestHelper.GetContext())
				{
					browser.NavigateTo($"{TestSite}");

					var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
					TestHelper.AddDefaultSettings(context, user);
					TestHelper.AddUser(context, "John Doe", "Password!");
					context.SaveChanges();
				}

				HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				Assert.AreEqual($"{TestSite}/", browser.Uri);
				Assert.AreEqual("John Doe", browser.Elements.Links["profileLink"].Text);
			});
		}

		#endregion
	}
}