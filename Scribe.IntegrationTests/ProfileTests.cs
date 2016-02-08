#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestR.PowerShell;

#endregion

namespace Scribe.IntegrationTests
{
	[TestClass]
	public class ProfileTests : BrowserTestCmdlet
	{
		#region Methods

		[TestMethod]
		public void Login()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				context.SaveChanges();

				ForEachBrowser(x => x.NavigateTo("http://localhost/login"));
				TestHelper.AddUser(context, "John Doe", "Password!");
				context.SaveChanges();
			}

			ForEachBrowser(x =>
			{
				x.NavigateTo("http://localhost/login");
				x.Elements.TextInputs["userName"].Text = "John Doe";
				x.Elements.TextInputs["password"].Text = "Password!";
				x.Elements.Buttons["submit"].Click();
				x.WaitForNavigation();

				Assert.AreEqual("http://localhost/", x.Uri);
				Assert.AreEqual("John Doe", x.Elements.Links["profileLink"].Text);
			});
		}

		#endregion
	}
}