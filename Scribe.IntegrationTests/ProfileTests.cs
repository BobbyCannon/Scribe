#region References

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Data.Entities;
using Scribe.Web;
using Scribe.Website.Services.Settings;
using Speedy;
using TestR.Helpers;
using TestR.Web;

#endregion

namespace Scribe.IntegrationTests
{
	[TestClass]
	public class ProfileTests : BaseTests
	{
		#region Constants

		private const string TestSite = "http://localhost";

		#endregion

		#region Methods

		[TestMethod]
		public void ForgotPassword()
		{
			ForEachBrowser(x =>
			{
				InitializeSite(x);
				
				x.NavigateTo(TestSite + "/ForgotPassword");
				Assert.AreEqual(TestSite + "/ForgotPassword", x.Uri);

				using (var database = TestHelper.GetDatabase(false))
				{
					var user = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					database.SaveChanges();

					using (var server = TestHelper.StartSmtpServer())
					{
						var userName = x.Elements.TextInputs["userName"];
						userName.TypingDelay = 30;

						userName.TypeText(user.EmailAddress, true);
						x.Elements.Buttons["submit"].Click();

						WaitAndValidate(x, server, user);
					}
				}
			});
		}

		[TestMethod]
		public void Login()
		{
			ForEachBrowser(browser =>
			{
				InitializeSite(browser);

				using (var database = TestHelper.GetDatabase(false))
				{
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					database.SaveChanges();
				}

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				Assert.AreEqual($"{TestSite}/", browser.Uri);
				Assert.AreEqual("John Doe", browser.Elements.Links["profileLink"].Text);
			});
		}

		[TestMethod]
		public void Register()
		{
			ForEachBrowser(browser =>
			{
				InitializeSite(browser);

				browser.NavigateTo($"{TestSite}/Register");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["emailAddress"].Text = "john.doe@test.com";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["register"].Click();
				browser.WaitForNavigation();

				Assert.AreEqual($"{TestSite}/", browser.Uri);
				Assert.AreEqual("John Doe", browser.Elements.Links["profileLink"].Text);
			});
		}

		[TestMethod]
		public void RegisterEmailAddressAlreadyUsed()
		{
			ForEachBrowser(browser =>
			{
				InitializeSite(browser);

				using (var database = TestHelper.GetDatabase(false))
				{
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					database.SaveChanges();
				}

				browser.NavigateTo($"{TestSite}/Register");
				browser.Elements.TextInputs["userName"].Text = "Johnny Doe";
				browser.Elements.TextInputs["emailAddress"].Text = "john.doe@test.com";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["register"].Click();

				Assert.AreEqual($"{TestSite}/Register", browser.Uri);
				TestHelper.AreEqual("The email address is already being used.", () => browser.Elements["error"].Text, 5000);
			});
		}

		[TestMethod]
		public void RegisterUserNameAlreadyUsed()
		{
			ForEachBrowser(browser =>
			{
				InitializeSite(browser);

				using (var database = TestHelper.GetDatabase(false))
				{
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					database.SaveChanges();
				}

				browser.NavigateTo($"{TestSite}/Register");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["emailAddress"].Text = "johnny.doe@test.com";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["register"].Click();

				Assert.AreEqual($"{TestSite}/Register", browser.Uri);
				TestHelper.AreEqual("The user name is already being used.", () => browser.Elements["error"].Text, 5000);
			});
		}

		private void InitializeSite(Browser browser)
		{
			using (var database = TestHelper.GetDatabase())
			{
				browser.NavigateTo($"{TestSite}");

				TestHelper.AddDefaultSettings(database);
				TestHelper.AddUser(database, "Administrator", "administrator@test.com", "Password!", "administrator");
				database.SaveChanges();
			}

			HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();
		}

		private void WaitAndValidate(Browser x, Helpers.SmtpServer server, User user)
		{
			var actual = Utility.Wait(() => !x.Elements.Divisions["confirmation"]
				.GetAttributeValue("class", true)
				.Contains("ng-hide"), 2000, 100);

			Assert.AreEqual(true, actual);

			var message = server.Messages.First();
			var to = message.To.First();
			Assert.AreEqual(user.EmailAddress, to.User + "@" + to.Host);

			using (var database = TestHelper.GetDatabase(false))
			{
				var siteSettings = SiteSettings.Load(database, true);
				var userSettings = new UserSettings(database.Settings, database.Users.First(y => y.Id == user.Id));
				userSettings.Load();

				var expectedStart = $"MIME-Version: 1.0\r\nFrom: \"{siteSettings.ContactEmail}\" <{siteSettings.ContactEmail}>\r\nTo: \"[UserName]\" <[EmailAddress]>\r\n".Replace("[UserName]", user.UserName).Replace("[EmailAddress]", user.EmailAddress);
				var expectedEnd = $"Subject: Scribe: Reset Password\r\nContent-Type: text/html; charset=us-ascii\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n<p>You are receiving this email because a request has been made to reset the password associated with the email address of [EmailAddress]. If you would like to reset the password for this account simply click on the link below or paste it into the url field on your favorite browser: </p><p><a href=3D\"{TestSite}/Account/ResetPassword/[PasswordToken]\">{TestSite}/Account/ResetPassword/[PasswordToken]</a></p><p>If you didn't request this email then you can just ignore it. The forgot password link will expire in 24 hours after it was issued. Also your personal information has not been disclosed to anyone. If you have any questions, feel free to <a href=3D\"mailto:{siteSettings.ContactEmail}\">contact us</a>.</p>\r\n\r\n".Replace("[EmailAddress]", user.EmailAddress).Replace("[PasswordToken]", userSettings.ResetPasswordId.ToString());
				var actualMessage = message.Mime.Replace("=\r\n", string.Empty).ToString();

				//Console.WriteLine(expectedStart.ToJson());
				//Console.WriteLine(actualMessage.ToJson());
				//Console.WriteLine(expectedEnd.ToJson());

				Assert.IsTrue(actualMessage.StartsWith(expectedStart));
				Assert.IsTrue(actualMessage.EndsWith(expectedEnd));
			}
		}

		#endregion
	}
}