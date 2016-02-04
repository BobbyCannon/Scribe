#region References

using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Extensions;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Web;

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

		public void Delete(FileView file)
		{
			var service = new FileService(DataContext, GetCurrentUser());
			service.Delete(file.Name);
			DataContext.SaveChanges();
		}

		[AllowAnonymous]
		public FileResult File(string name)
		{
			var service = new FileService(DataContext, GetCurrentUser(false));
			var file = service.GetFile(name);
			if (file != null)
			{
				return new FileContentResult(file.Data, file.Type);
			}

			var contentType = MimeMapping.GetMimeMapping(name);
			if (!contentType.StartsWith("image"))
			{
				throw new HttpException(404, "Failed to find the file requested.");
			}

			var data = Assembly.GetExecutingAssembly().ReadEmbeddedBinaryFile("Scribe.Website.Content.404.png");
			return new FileContentResult(data, "image/png");
		}

		public ActionResult Files()
		{
			var service = new FileService(DataContext, GetCurrentUser());
			return View(service.GetFiles());
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

			var service = new FileService(DataContext, GetCurrentUser());
			service.Add(Path.GetFileName(file.FileName), contentType, data);
			DataContext.SaveChanges();

			return new JsonNetResult(service.GetFiles());
		}

		#endregion
	}
}