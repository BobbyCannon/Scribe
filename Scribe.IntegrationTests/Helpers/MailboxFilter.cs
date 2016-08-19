#region References

using System.Net;
using System.Threading.Tasks;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

#endregion

namespace Scribe.IntegrationTests.Helpers
{
	public class MailboxFilter : IMailboxFilter
	{
		#region Methods

		public Task<MailboxFilterResult> CanAcceptFromAsync(IMailbox from, int size = 0)
		{
			return Task.FromResult(MailboxFilterResult.Yes);
		}

		public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size = 0)
		{
			return Task.FromResult(MailboxFilterResult.Yes);
		}

		public Task<MailboxFilterResult> CanDeliverToAsync(IMailbox to, IMailbox from)
		{
			return Task.FromResult(MailboxFilterResult.Yes);
		}

		public Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from)
		{
			return Task.FromResult(MailboxFilterResult.Yes);
		}

		public IMailboxFilter CreateSessionInstance(EndPoint remoteEndPoint)
		{
			return new MailboxFilter();
		}

		#endregion
	}
}