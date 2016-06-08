#region References

using System;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Website.Hubs;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class AnalyticsController : BaseController
	{
		#region Fields

		private readonly INotificationHub _notificationHub;
		private readonly AnalyticsService _service;

		#endregion

		#region Constructors

		public AnalyticsController(IScribeDatabase database, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(database, authenticationService)
		{
			_notificationHub = notificationHub;
			_service = new AnalyticsService(Database);
		}

		#endregion

		#region Methods

		public ActionResult Home(DateTime? startDate, DateTime? endDate)
		{
			return View(_service.Get(startDate, endDate));
		}

		#endregion
	}
}