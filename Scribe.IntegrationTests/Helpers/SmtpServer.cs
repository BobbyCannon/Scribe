#region References

using System;
using System.Collections.Generic;
using System.Threading;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer2 = SmtpServer.SmtpServer;

#endregion

namespace Scribe.IntegrationTests.Helpers
{
	public class SmtpServer : IDisposable
	{
		#region Fields

		private readonly MessageStore _messageStore;
		private readonly SmtpServer2 _smtpServer;
		private CancellationTokenSource _tokenSource;

		#endregion

		#region Constructors

		public SmtpServer()
		{
			_messageStore = new MessageStore();
			var options = new OptionsBuilder()
				.ServerName("localhost")
				.Port(25)
				.MessageStore(_messageStore)
				.Build();

			_tokenSource = new CancellationTokenSource();
			_smtpServer = new SmtpServer2(options);
			
		}

		#endregion

		#region Properties

		public IEnumerable<IMimeMessage> Messages => _messageStore.Messages;

		#endregion

		#region Methods

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Start()
		{
			_smtpServer.StartAsync(_tokenSource.Token);
			Thread.Sleep(100);
		}

		public void Stop()
		{
			if (_tokenSource.IsCancellationRequested || _tokenSource == null)
			{
				return;
			}

			try
			{
				_tokenSource.Cancel();
				_tokenSource.Dispose();
				_tokenSource = null;
			}
			catch
			{
				// Ignore
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Stop();
			}
		}

		#endregion
	}
}