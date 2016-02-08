#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace Scribe.Extensions
{
	public static class TimeSpanExtensions
	{
		#region Methods

		/// <summary>
		/// Converts short unit to the long unit name.
		/// </summary>
		/// <param name="unit"> The unit to convert. </param>
		/// <param name="plural"> The flag to pluralize or not. </param>
		/// <returns> The long unit name for the short unit. </returns>
		public static string ConvertShortUnitToLongUnit(string unit, bool plural)
		{
			var data = new Dictionary<string, string>();
			data.Add("y", "year");
			data.Add("M", "month");
			data.Add("d", "day");
			data.Add("h", "hour");
			data.Add("m", "minute");
			data.Add("s", "second");

			if (data.ContainsKey(unit))
			{
				return plural ? data[unit] + "s" : data[unit];
			}

			return string.Empty;
		}

		/// <summary>
		/// Formats the time span into a human readable format.
		/// </summary>
		/// <param name="time"> The time span to convert. </param>
		/// <param name="limited"> The flag to limit to only a single value. </param>
		/// <param name="format"> The format to use to generate the string. </param>
		/// <returns> A human readable format of the time span. </returns>
		public static string ToTimeAgo(this TimeSpan time, bool limited = true, string format = "yMdhms")
		{
			var thresholds = new SortedList<long, string>();
			var secondsPerMinute = 60;
			var secondsPerHour = 60 * secondsPerMinute;
			var secondsPerDay = 24 * secondsPerHour;

			thresholds.Add(secondsPerDay * 365, "y");
			thresholds.Add(secondsPerDay * 30, "M");
			thresholds.Add(secondsPerDay, "d");
			thresholds.Add(secondsPerHour, "h");
			thresholds.Add(secondsPerMinute, "m");
			thresholds.Add(1, "s");

			var builder = new StringBuilder();
			var secondsRemaining = time.TotalSeconds;
			var thresholdsHit = 0;

			for (var i = thresholds.Keys.Count - 1; i >= 0 && thresholdsHit < 2; i--)
			{
				var threshold = thresholds.Keys[i];
				var unit = thresholds[threshold];
				if (!(secondsRemaining >= threshold))
				{
					continue;
				}

				if (!format.Contains(unit))
				{
					continue;
				}

				var count = (int) (secondsRemaining / threshold);
				secondsRemaining %= threshold;

				var unitText = ConvertShortUnitToLongUnit(unit, count > 1);
				builder.AppendFormat(", {0} {1}", count, unitText);
				thresholdsHit++;

				if (limited)
				{
					break;
				}
			}

			if (builder.Length <= 0)
			{
				var firstUnit = format.Last().ToString();
				return "less than a " + ConvertShortUnitToLongUnit(firstUnit, false);
			}

			var response = builder.Remove(0, 2).ToString();
			var lastIndex = response.LastIndexOf(", ");

			if (lastIndex > 0)
			{
				response = response.Remove(lastIndex, 2);
				response = response.Insert(lastIndex, " and ");
			}

			return response;
		}

		#endregion
	}
}