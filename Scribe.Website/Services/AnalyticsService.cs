#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Scribe.Data;
using Scribe.Website.Models;
using Scribe.Website.ViewModels;

#endregion

namespace Scribe.Website.Services
{
	public class AnalyticsService
	{
		#region Fields

		private readonly IScribeDatabase _database;

		#endregion

		#region Constructors

		public AnalyticsService(IScribeDatabase database)
		{
			_database = database;
		}

		#endregion

		#region Methods

		public AnalyticsHomeView Get(DateTime? startDate, DateTime? endDate)
		{
			var response = new AnalyticsHomeView();
			endDate = endDate ?? DateTime.Now;
			startDate = startDate ?? endDate.Value.AddMonths(-1);
			response.EndDate = endDate.Value.ToShortDateString();
			response.StartDate = startDate.Value.ToShortDateString();

			var filterStart = startDate.Value.ToUniversalTime();
			var filterStartSixMonthsAgo = filterStart.AddMonths(-6);
			var filterEnd = endDate.Value.AddDays(1).ToUniversalTime();

			var watch = Stopwatch.StartNew();

			response.MostActivePages = _database.Pages
				.Where(x => x.CurrentVersion.ModifiedOn >= filterStart && x.CurrentVersion.ModifiedOn < filterEnd)
				.Select(x => new AnalyticData<int>
				{
					Name = x.CurrentVersion.Title,
					Value = x.Versions.Count(y => y.CreatedOn >= filterStart && y.CreatedOn < filterEnd),
					Link = "/page/" + x.Id
				})
				.OrderByDescending(x => x.Value)
				.Take(10)
				.ToList();

			Debug.WriteLine("MostActivePages: " + watch.Elapsed);
			watch.Restart();

			var users = _database.Users
				.Select(x => new
				{
					Name = x.DisplayName,
					Value = x.CreatedPages
						.Where(y => y.ModifiedOn >= filterStart && y.ModifiedOn < filterEnd)
						.GroupBy(y => y.PageId)
						.Count(),
					x.Tags
				})
				.ToList();

			response.MostActiveEditors = users
				.OrderByDescending(x => x.Value)
				.Take(10)
				.Select(x => new AnalyticData<int>
				{
					Name = x.Name,
					Value = x.Value
				})
				.ToList();

			Debug.WriteLine("MostActiveEditors: " + watch.Elapsed);
			watch.Restart();

			var systemGroups = new List<string> { "administrator", "approver", "publisher" };

			response.MostActiveGroups = users
				.SelectMany(x => x.Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(y => new { Group = y, x.Value }))
				.GroupBy(x => x.Group)
				.Where(x => !systemGroups.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
				.Select(x => new AnalyticData<int>
				{
					Name = x.Key,
					Value = x.Count()
				})
				.OrderByDescending(x => x.Value)
				.Take(10)
				.ToList();

			Debug.WriteLine("MostActiveGroups: " + watch.Elapsed);
			watch.Restart();

			response.MostViewPages = _database.Events
				.Where(x => x.Name == AnalyticEvents.WebRequest.ToString())
				.Where(y => y.StartedOn >= filterStart && y.StartedOn < filterEnd)
				.GroupBy(x => x.Values.FirstOrDefault(y => y.Name == "Page Id").Value)
				.Where(x => x.Key != null)
				.Select(x => new
				{
					Event = x.OrderByDescending(y => y.CompletedOn).FirstOrDefault(),
					Value = x.Count()
				})
				.OrderByDescending(x => x.Value)
				.Select(x => new AnalyticData<int>
				{
					Name = x.Event.Values.FirstOrDefault(y => y.Name == "Page Title").Value,
					Value = x.Value,
					Link = x.Event.Values.FirstOrDefault(y => y.Name == "URI").Value
				})
				.Take(10)
				.ToList();

			Debug.WriteLine("MostViewPages: " + watch.Elapsed);
			watch.Restart();

			response.NewPagesByUser = _database.Pages
				.Where(x => !x.IsDeleted && x.CreatedOn >= filterStart)
				.GroupBy(x => x.Versions.OrderBy(y => y.Id).FirstOrDefault().CreatedBy.DisplayName)
				.Select(x => new AnalyticData<int>
				{
					Name = x.Key,
					Value = x.Count()
				})
				.OrderByDescending(x => x.Value)
				.Take(10)
				.ToList();

			Debug.WriteLine("NewPagesByUser: " + watch.Elapsed);
			watch.Restart();

			var newPagesPerMonth = _database.Pages
				.Where(x => !x.IsDeleted && x.CreatedOn >= filterStartSixMonthsAgo)
				.GroupBy(x => new { x.CreatedOn.Month, x.CreatedOn.Year })
				.OrderByDescending(x => x.Key.Year)
				.ThenByDescending(x => x.Key.Month)
				.Select(x => new AnalyticData<int>
				{
					Name = x.Key.Month + "/" + x.Key.Year,
					Value = x.Count()
				})
				.Take(12)
				.ToList();

			newPagesPerMonth.Reverse();
			response.NewPagesPerMonth = newPagesPerMonth;

			Debug.WriteLine("NewPagesPerMonth: " + watch.Elapsed);
			watch.Restart();

			return response;
		}

		#endregion
	}
}