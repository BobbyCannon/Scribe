#region References

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Website.Attributes;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class FileController : BaseController
	{
		#region Constructors

		public FileController(IScribeDatabase database, IAuthenticationService authenticationService)
			: base(database, authenticationService)
		{
		}

		#endregion

		#region Methods

		[AllowAnonymous]
		public ActionResult File(int id)
		{
			var service = new ScribeService(Database, null, null, GetCurrentUser(null, false));

			if (!string.IsNullOrEmpty(Request.Headers["If-Modified-Since"]))
			{
				var fileInfo = service.GetFile(id);
				var previousModifiedOn = DateTime.ParseExact(Request.Headers["If-Modified-Since"], "r", CultureInfo.InvariantCulture).ToLocalTime();
				var currentModifiedOn = fileInfo.ModifiedOn.TruncateTo(Scribe.Extensions.DateTruncate.Second);

				if (currentModifiedOn <= previousModifiedOn)
				{
					Response.StatusCode = 304;
					Response.StatusDescription = "Not Modified";
					return Content(string.Empty);
				}
			}

			var file = service.GetFile(id, true);
			if (file != null)
			{
				Response.Cache.SetCacheability(HttpCacheability.Public);
				Response.Cache.SetLastModified(file.ModifiedOn);
				Response.AddHeader("Content-Disposition", "inline; filename=" + file.Name);
				return File(file.Data, file.Type);
			}

			Response.AddHeader("Content-Disposition", "inline; filename=404.png");
			var data = Assembly.GetExecutingAssembly().ReadEmbeddedBinaryFile("Scribe.Website.Content.404.png");
			return File(data, "image/png");
		}

		public ActionResult Files()
		{
			var service = new ScribeService(Database, null, null, GetCurrentUser());
			return View(service.GetFiles(new PagedRequest { PerPage = int.MaxValue }));
		}

		[HttpPost]
		public ActionResult Upload(HttpPostedFileBase file)
		{
			if (file == null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			var data = new byte[file.ContentLength];
			file.InputStream.Read(data, 0, file.ContentLength);

			var contentType = file.ContentType;
			if (contentType == "application/octet-stream")
			{
				contentType = MimeMapping.GetMimeMapping(file.FileName);
			}

			var service = new ScribeService(Database, null, null, GetCurrentUser());
			var fileData = new FileView
			{
				Name = Path.GetFileName(file.FileName),
				Type = contentType,
				Data = data
			};

			service.SaveFile(fileData);
			Database.SaveChanges();

			return new JsonNetResult(service.GetFiles(new PagedRequest { PerPage = int.MaxValue }));
		}

		#endregion
	}
}