#region References

using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class FileController : BaseController
	{
		#region Constructors

		public FileController(IScribeContext dataContext, IAuthenticationService authenticationService)
			: base(dataContext, authenticationService)
		{
		}

		#endregion

		#region Methods

		[AllowAnonymous]
		public FileResult File(string name)
		{
			var service = new ScribeService(DataContext, null, null, GetCurrentUser(false));
			var file = service.GetFile(name, true);
			if (file != null)
			{
				Response.AddHeader("Content-Disposition", "inline; filename=" + file.Name);
				return new FileContentResult(file.Data, file.Type);
			}

			var contentType = MimeMapping.GetMimeMapping(name);
			if (!contentType.StartsWith("image"))
			{
				throw new HttpException(404, "Failed to find the file requested.");
			}

			Response.AddHeader("Content-Disposition", "inline; filename=404.png");
			var data = Assembly.GetExecutingAssembly().ReadEmbeddedBinaryFile("Scribe.Website.Content.404.png");
			return new FileContentResult(data, "image/png");
		}

		public ActionResult Files()
		{
			var service = new ScribeService(DataContext, null, null, GetCurrentUser());
			return View(service.GetFiles(new PagedRequest(perPage: int.MaxValue)));
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

			var service = new ScribeService(DataContext, null, null, GetCurrentUser());
			var fileData = new FileView
			{
				Name = Path.GetFileName(file.FileName),
				Type = contentType,
				Data = data
			};

			service.SaveFile(fileData);
			DataContext.SaveChanges();

			return new JsonNetResult(service.GetFiles(new PagedRequest(perPage: int.MaxValue)));
		}

		#endregion
	}
}