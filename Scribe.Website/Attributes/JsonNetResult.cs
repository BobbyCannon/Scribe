#region References

using System;
using System.Web.Mvc;
using Newtonsoft.Json;
using Scribe.Extensions;

#endregion

namespace Scribe.Website
{
	public class JsonNetResult : JsonResult
	{
		#region Constructors

		public JsonNetResult(object data)
			: this()
		{
			Data = data;
		}

		public JsonNetResult()
		{
		}

		#endregion

		#region Properties

		public Formatting Formatting { get; set; }

		#endregion

		#region Methods

		public override void ExecuteResult(ControllerContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			var response = context.HttpContext.Response;
			response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : "application/json";

			if (ContentEncoding != null)
			{
				response.ContentEncoding = ContentEncoding;
			}

			if (Data == null)
			{
				return;
			}

			var writer = new JsonTextWriter(response.Output) { Formatting = Formatting };
			var serializer = JsonSerializer.Create(ObjectExtensions.GetSerializerSettings());
			serializer.Serialize(writer, Data);
			writer.Flush();
		}

		#endregion
	}
}