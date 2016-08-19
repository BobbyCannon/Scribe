#region References

using System;
using TestR.PowerShell;
using TestR.Web;

#endregion

namespace Scribe.IntegrationTests
{
    public class BaseTests : BrowserTestCmdlet
    {
        #region Methods

        /// <summary>
        /// Run a test against each browser. BrowserType property will determine which browsers to run the test against.
        /// </summary>
        /// <param name="action"> The action to run each browser against. </param>
        /// <seealso cref="BrowserType" />
        protected void ForEachBrowser(Action<Browser> action)
        {
	        BrowserType = BrowserType.All;
            ForEachBrowser(action, false);
        }

        #endregion
    }
}