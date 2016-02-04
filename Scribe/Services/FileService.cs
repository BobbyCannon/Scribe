#region References

using System;
using System.Data.Entity.Migrations;
using System.Linq;
using Scribe.Data;
using Scribe.Models.Entities;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public class FileService
	{
		#region Fields

		private readonly IScribeContext _context;
		private readonly SettingsService _settings;
		private readonly User _user;

		#endregion

		#region Constructors

		public FileService(IScribeContext context, User user)
		{
			_context = context;
			_user = user;
			_settings = new SettingsService(context, user);
		}

		#endregion

		#region Methods

		public void Add(string fileName, string type, byte[] data)
		{
			var file = _context.Files.FirstOrDefault(x => x.Name == fileName) ?? new File { Name = fileName, CreatedBy = _user, CreatedOn = DateTime.UtcNow };

			if (!_settings.OverwriteFilesOnUpload && file.Id != 0)
			{
				throw new InvalidOperationException("The file already exists and cannot be overwritten.");
			}

			file.Data = data;
			file.Size = data.Length;
			file.Type = type;
			file.ModifiedOn = file.Id == 0 ? file.CreatedOn : DateTime.UtcNow;
			file.ModifiedBy = _user;

			_context.Files.AddOrUpdate(file);
		}

		public void Delete(string name)
		{
			_context.Files.RemoveRange(_context.Files.Where(x => x.Name == name));
		}

		public File GetFile(string name)
		{
			return _context.Files.FirstOrDefault(x => x.Name == name);
		}

		public FilesView GetFiles()
		{
			return new FilesView
			{
				Files = _context.Files
					.OrderBy(x => x.Name)
					.Select(x => new FileView
					{
						Name = x.Name,
						Size = (x.Size / 1024).ToString() + " kb",
						Type = x.Type
					})
					.ToList()
			};
		}

		#endregion
	}
}