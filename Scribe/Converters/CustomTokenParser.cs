#region References

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Scribe.Extensions;

#endregion

namespace Scribe.Converters
{
	/// <summary>
	/// Deserializes and caches the custom tokens XML file, which contains a set of text replacements for the markup.
	/// </summary>
	public class CustomTokenParser
	{
		#region Fields

		private static readonly IEnumerable<TextToken> _tokens;

		#endregion

		#region Constructors

		public CustomTokenParser()
		{
			ParseTokenRegexes();
		}

		static CustomTokenParser()
		{
			_tokens = Deserialize();
		}

		#endregion

		#region Methods

		public string ReplaceTokensAfterParse(string html)
		{
			foreach (var token in _tokens)
			{
				html = token.CachedRegex.Replace(html, token.HtmlReplacement);
			}

			return html;
		}

		private static IEnumerable<TextToken> Deserialize()
		{
			var xml = Assembly.GetExecutingAssembly().ReadEmbeddedFile("Scribe.Converters.CustomTokens.xml");

			using (var stream = new StringReader(xml))
			{
				var serializer = new XmlSerializer(typeof (List<TextToken>));
				IEnumerable<TextToken> textTokens = (List<TextToken>) serializer.Deserialize(stream);
				return textTokens ?? new List<TextToken>();
			}
		}

		private static void ParseTokenRegexes()
		{
			foreach (var token in _tokens)
			{
				var regex = new Regex(token.SearchRegex, RegexOptions.Compiled | RegexOptions.Singleline);
				token.CachedRegex = regex;
			}
		}

		#endregion
	}
}