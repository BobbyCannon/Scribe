#region References

using System;
using System.Net;
using System.Net.Http;
using System.Text;

#endregion

namespace Scribe.Web
{
	/// <summary>
	/// This class is used for making GET and POST calls to an HTTP endpoint.
	/// </summary>
	public class HttpHelper
	{
		#region Constructors

		/// <summary>
		/// Initializes a new HTTP helper to point at a specific URI, and with the specified session identifier.
		/// </summary>
		/// <param name="baseUri"> The base URI of the service. </param>
		public HttpHelper(string baseUri)
		{
			BaseUri = baseUri;
			Cookies = new CookieCollection();
			Timeout = new TimeSpan(0, 0, 100);
		}

		#endregion

		#region Properties

		public string BaseUri { get; set; }

		public CookieCollection Cookies { get; set; }

		public bool IsAuthenticated
		{
			get
			{
				foreach (Cookie cookie in Cookies)
				{
					if (cookie.Name == ".ASPXAUTH")
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Gets or sets the number of milliseconds to wait before the request times out. The default value is 100 seconds.
		/// </summary>
		public TimeSpan Timeout { get; set; }

		#endregion

		#region Methods

		public virtual HttpResponseMessage Get(string uri)
		{
			using (var handler = new HttpClientHandler())
			{
				using (var client = CreateHttpClient(uri, handler))
				{
					var response = client.GetAsync(uri).Result;
					return ProcessResponse(response, handler);
				}
			}
		}

		public virtual HttpResponseMessage Post(string uri)
		{
			return InternalPost(uri, string.Empty);
		}

		public virtual HttpResponseMessage Post<TContent>(string uri, TContent content)
		{
			return InternalPost(uri, content);
		}

		public virtual T Read<T>(HttpResponseMessage message)
		{
			return message.Content.ReadAsStringAsync().Result.FromJson<T>();
		}

		private HttpClient CreateHttpClient(string uri, HttpClientHandler handler)
		{
			foreach (Cookie ck in Cookies)
			{
				handler.CookieContainer.Add(ck);
			}

			var client = new HttpClient(handler);
			client.BaseAddress = new Uri(BaseUri);
			client.Timeout = Timeout;

			return client;
		}

		private HttpResponseMessage InternalPost<T>(string uri, T content)
		{
			using (var handler = new HttpClientHandler())
			{
				using (var client = CreateHttpClient(uri, handler))
				{
					using (var objectContent = new StringContent(content.ToJson(), Encoding.UTF8, "application/json"))
					{
						var response = client.PostAsync(uri, objectContent).Result;
						return ProcessResponse(response, handler);
					}
				}
			}
		}

		private HttpResponseMessage ProcessResponse(HttpResponseMessage response, HttpClientHandler handler)
		{
			if (handler.CookieContainer != null && Uri.IsWellFormedUriString(BaseUri, UriKind.Absolute))
			{
				Cookies = handler.CookieContainer.GetCookies(new Uri(BaseUri));
			}

			return response;
		}

		#endregion
	}
}