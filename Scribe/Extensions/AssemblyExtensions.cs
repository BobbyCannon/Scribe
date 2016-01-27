#region References

using System;
using System.IO;
using System.Reflection;

#endregion

namespace Scribe.Extensions
{
	public static class AssemblyExtensions
	{
		#region Methods

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

		#endregion
	}
}