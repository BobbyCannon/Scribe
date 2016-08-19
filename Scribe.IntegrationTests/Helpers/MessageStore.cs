#region References

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

#endregion

namespace Scribe.IntegrationTests.Helpers
{
	public class MessageStore : IMessageStore, IMessageStoreFactory
	{
		#region Constructors

		public MessageStore()
		{
			Messages = new List<IMimeMessage>();
		}

		#endregion

		#region Properties

		public List<IMimeMessage> Messages { get; }

		#endregion

		#region Methods

		public IMessageStore CreateInstance(ISessionContext context)
		{
			return this;
		}

		public Task<string> SaveAsync(IMimeMessage message, CancellationToken cancellationToken)
		{
			Messages.Add(message);
			return Task.FromResult(string.Empty);
		}

		public Task<string> SaveAsync(ISessionContext context, IMimeMessage message, CancellationToken cancellationToken)
		{
			Messages.Add(message);
			return Task.FromResult(string.Empty);
		}

		Task<SmtpResponse> IMessageStore.SaveAsync(ISessionContext context, IMimeMessage message, CancellationToken cancellationToken)
		{
			Messages.Add(message);
			return Task.FromResult(SmtpResponse.Ok);
		}

		#endregion
	}
}