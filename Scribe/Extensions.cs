#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Security;

#endregion

namespace Scribe
{
	public static class Extensions
	{
		#region Methods

		/// <summary>
		/// Calculate an MD5 hash for the string.
		/// </summary>
		/// <param name="input"> The string to hash. </param>
		/// <returns> The MD5 formatted hash for the input. </returns>
		public static string CalculateHash(this string input)
		{
			// Calculate MD5 hash from input.
			var md5 = MD5.Create();
			var inputBytes = Encoding.ASCII.GetBytes(input);
			var hash = md5.ComputeHash(inputBytes);

			// Convert byte array to hex string.
			var sb = new StringBuilder();
			foreach (var item in hash)
			{
				sb.Append(item.ToString("X2"));
			}

			// Return the MD5 string.
			return sb.ToString().ToLower();
		}

		public static string CleanMessage(this Exception ex)
		{
			var offset = ex.Message.IndexOf("\r\nParameter");
			return offset > 0 ? ex.Message.Substring(0, offset) : ex.Message;
		}

		public static bool ContainsAny(this string value, IEnumerable<string> values)
		{
			return values.Any(value.Contains);
		}

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
		/// Converts the string to an int. If it cannot be parse it will return the default value.
		/// </summary>
		/// <param name="input"> The string to convert. </param>
		/// <param name="defaultValue"> The default value to return. Defaults to 0. </param>
		/// <returns> The int value or the default value. </returns>
		public static int ConvertToInt(this string input, int defaultValue = 0)
		{
			int response;
			return !int.TryParse(input, out response) ? defaultValue : response;
		}

		public static string GetDisplayName(this IIdentity identity)
		{
			return !identity.Name.Contains(';') ? string.Empty : identity.Name.Split(';').Last();
		}

		/// <summary>
		/// Gets the domain for the provided URI.
		/// </summary>
		/// <param name="uri"> The URI to process. </param>
		/// <returns> The URI for only the domain. </returns>
		public static string GetDomain(this Uri uri)
		{
			var response = uri.Scheme + "://" + uri.Host;

			if (((uri.Scheme == "https") && (uri.Port != 443)) || ((uri.Scheme == "http") && (uri.Port != 80)))
			{
				response += ":" + uri.Port;
			}

			return response;
		}

		public static int GetId(this IIdentity identity)
		{
			return !identity.Name.Contains(';') ? 0 : identity.Name.Split(';').First().ConvertToInt();
		}

		public static IEnumerable<string> GetTags(this IIdentity identity)
		{
			return Roles.GetRolesForUser(identity.Name);
		}

		public static byte[] ReadEmbeddedBinaryFile(this Assembly assembly, string path)
		{
			using (var stream = assembly.GetManifestResourceStream(path))
			{
				if (stream == null)
				{
					throw new Exception("Embedded file not found.");
				}

				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				return data;
			}
		}

		public static string ReadEmbeddedFile(this Assembly assembly, string path)
		{
			using (var stream = assembly.GetManifestResourceStream(path))
			{
				if (stream == null)
				{
					throw new Exception("Embedded file not found.");
				}

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// Continues to run the action until we hit the timeout. If an exception occurs then delay for the
		/// provided delay time.
		/// </summary>
		/// <param name="action"> The action to attempt to retry. </param>
		/// <param name="timeout"> The timeout to stop retrying. </param>
		/// <param name="delay"> The delay between retries. </param>
		/// <returns> The response from the action. </returns>
		public static void Retry(Action action, int timeout, int delay)
		{
			var watch = Stopwatch.StartNew();

			try
			{
				action();
			}
			catch (Exception)
			{
				Thread.Sleep(delay);

				var remaining = (int) (timeout - watch.Elapsed.TotalMilliseconds);
				if (remaining <= 0)
				{
					throw;
				}

				Retry(action, remaining, delay);
			}
		}

		public static string ToDetailedString(this Exception ex)
		{
			var builder = new StringBuilder();
			AddExceptionToBuilder(builder, ex);
			return builder.ToString();
		}

		public static string ToSingleLine(this string value)
		{
			return value.Replace("\r\n", string.Empty)
				.Replace("\r", string.Empty)
				.Replace("\n", string.Empty);
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
				builder.Append($", {count} {unitText}");
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

		public static DateTime TruncateTo(this DateTime dt, DateTruncate truncateTo)
		{
			switch (truncateTo)
			{
				case DateTruncate.Year:
					return new DateTime(dt.Year, 01, 01);

				case DateTruncate.Month:
					return new DateTime(dt.Year, dt.Month, 01);

				case DateTruncate.Day:
					return new DateTime(dt.Year, dt.Month, dt.Day);

				case DateTruncate.Hour:
					return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);

				case DateTruncate.Minute:
					return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);

				case DateTruncate.Second:
				default:
					return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
			}
		}

		private static void AddExceptionToBuilder(StringBuilder builder, Exception ex)
		{
			builder.Append(builder.Length > 0 ? "\r\n" + ex.Message : ex.Message);

			if (ex.InnerException != null)
			{
				AddExceptionToBuilder(builder, ex.InnerException);
			}
		}

		#endregion

		#region Enumerations

		public enum DateTruncate
		{
			Year,
			Month,
			Day,
			Hour,
			Minute,
			Second
		}

		#endregion
	}
}