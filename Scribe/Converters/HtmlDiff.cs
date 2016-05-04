#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace Scribe.Converters
{
	/// <summary>
	/// Performs a difference on two HTML sources.
	/// </summary>
	/// <remarks> Source: http://htmldiff.codeplex.com </remarks>
	public class HtmlDiff
	{
		#region Fields

		private readonly StringBuilder _content;
		private readonly string _newText;
		private string[] _newWords;
		private readonly string _oldText;
		private string[] _oldWords;
		private readonly string[] _specialCaseClosingTags = { "</strong>", "</b>", "</i>", "</big>", "</small>", "</u>", "</sub>", "</sup>", "</strike>", "</s>" };
		private readonly string[] _specialCaseOpeningTags = { "<strong[\\>\\s]+", "<b[\\>\\s]+", "<i[\\>\\s]+", "<big[\\>\\s]+", "<small[\\>\\s]+", "<u[\\>\\s]+", "<sub[\\>\\s]+", "<sup[\\>\\s]+", "<strike[\\>\\s]+", "<s[\\>\\s]+" };
		private Dictionary<string, List<int>> _wordIndices;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		/// <param name="oldText"> The old text. </param>
		/// <param name="newText"> The new text. </param>
		private HtmlDiff(string oldText, string newText)
		{
			_oldText = oldText;
			_newText = newText;
			_content = new StringBuilder();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Builds the HTML difference output
		/// </summary>
		/// <returns> HTML difference markup </returns>
		public string Build()
		{
			SplitInputsToWords();

			IndexNewWords();

			var operations = Operations();

			foreach (var item in operations)
			{
				PerformOperation(item);
			}

			return _content.ToString();
		}

		public static string Process(string oldText, string newText)
		{
			var service = new HtmlDiff(oldText, newText);
			return service.Build();
		}

		private string[] ConvertHtmlToListOfWords(string[] characterString)
		{
			var mode = Mode.Character;
			var currentWord = string.Empty;
			var words = new List<string>();

			foreach (var character in characterString)
			{
				switch (mode)
				{
					case Mode.Character:

						if (IsStartOfTag(character))
						{
							if (currentWord != string.Empty)
							{
								words.Add(currentWord);
							}

							currentWord = "<";
							mode = Mode.Tag;
						}
						else if (Regex.IsMatch(character, "\\s"))
						{
							if (currentWord != string.Empty)
							{
								words.Add(currentWord);
							}
							currentWord = character;
							mode = Mode.Whitespace;
						}
						else
						{
							currentWord += character;
						}

						break;
					case Mode.Tag:
						if (IsEndOfTag(character))
						{
							currentWord += ">";
							words.Add(currentWord);
							currentWord = "";
							mode = IsWhiteSpace(character) ? Mode.Whitespace : Mode.Character;
						}
						else
						{
							currentWord += character;
						}

						break;
					case Mode.Whitespace:

						if (IsStartOfTag(character))
						{
							if (currentWord != string.Empty)
							{
								words.Add(currentWord);
							}
							currentWord = "<";
							mode = Mode.Tag;
						}
						else if (Regex.IsMatch(character, "\\s"))
						{
							currentWord += character;
						}
						else
						{
							if (currentWord != string.Empty)
							{
								words.Add(currentWord);
							}

							currentWord = character;
							mode = Mode.Character;
						}

						break;
				}
			}

			if (currentWord != string.Empty)
			{
				words.Add(currentWord);
			}

			return words.ToArray();
		}

		private string[] Explode(string value)
		{
			return Regex.Split(value, "");
		}

		private string[] ExtractConsecutiveWords(List<string> words, Func<string, bool> condition)
		{
			int? indexOfFirstTag = null;

			for (var i = 0; i < words.Count; i++)
			{
				var word = words[i];

				if (!condition(word))
				{
					indexOfFirstTag = i;
					break;
				}
			}

			if (indexOfFirstTag != null)
			{
				var items = words.Where((s, pos) => pos >= 0 && pos < indexOfFirstTag).ToArray();
				if (indexOfFirstTag.Value > 0)
				{
					words.RemoveRange(0, indexOfFirstTag.Value);
				}
				return items;
			}
			else
			{
				var items = words.Where((s, pos) => pos >= 0 && pos <= words.Count).ToArray();
				words.RemoveRange(0, words.Count);
				return items;
			}
		}

		private DiffMatch FindMatch(int startInOld, int endInOld, int startInNew, int endInNew)
		{
			var bestMatchInOld = startInOld;
			var bestMatchInNew = startInNew;
			var bestMatchSize = 0;

			var matchLengthAt = new Dictionary<int, int>();

			for (var indexInOld = startInOld; indexInOld < endInOld; indexInOld++)
			{
				var newMatchLengthAt = new Dictionary<int, int>();

				var index = _oldWords[indexInOld];

				if (!_wordIndices.ContainsKey(index))
				{
					matchLengthAt = newMatchLengthAt;
					continue;
				}

				foreach (var indexInNew in _wordIndices[index])
				{
					if (indexInNew < startInNew)
					{
						continue;
					}

					if (indexInNew >= endInNew)
					{
						break;
					}

					var newMatchLength = (matchLengthAt.ContainsKey(indexInNew - 1) ? matchLengthAt[indexInNew - 1] : 0) + 1;
					newMatchLengthAt[indexInNew] = newMatchLength;

					if (newMatchLength > bestMatchSize)
					{
						bestMatchInOld = indexInOld - newMatchLength + 1;
						bestMatchInNew = indexInNew - newMatchLength + 1;
						bestMatchSize = newMatchLength;
					}
				}

				matchLengthAt = newMatchLengthAt;
			}

			return bestMatchSize != 0 ? new DiffMatch(bestMatchInOld, bestMatchInNew, bestMatchSize) : null;
		}

		private void FindMatchingBlocks(int startInOld, int endInOld, int startInNew, int endInNew, List<DiffMatch> matchingBlocks)
		{
			var match = FindMatch(startInOld, endInOld, startInNew, endInNew);

			if (match != null)
			{
				if (startInOld < match.StartInOld && startInNew < match.StartInNew)
				{
					FindMatchingBlocks(startInOld, match.StartInOld, startInNew, match.StartInNew, matchingBlocks);
				}

				matchingBlocks.Add(match);

				if (match.EndInOld < endInOld && match.EndInNew < endInNew)
				{
					FindMatchingBlocks(match.EndInOld, endInOld, match.EndInNew, endInNew, matchingBlocks);
				}
			}
		}

		private void IndexNewWords()
		{
			_wordIndices = new Dictionary<string, List<int>>();
			for (var i = 0; i < _newWords.Length; i++)
			{
				var word = _newWords[i];

				if (_wordIndices.ContainsKey(word))
				{
					_wordIndices[word].Add(i);
				}
				else
				{
					_wordIndices[word] = new List<int>();
					_wordIndices[word].Add(i);
				}
			}
		}

		/// <summary>
		/// This method encloses words within a specified tag (ins or del), and adds this into "content",
		/// with a twist: if there are words contain tags, it actually creates multiple ins or del,
		/// so that they don't include any ins or del. This handles cases like
		/// old: '<p> a </p>'
		/// new: '<p> ab </p>
		/// <p>
		/// c
		/// </p>
		/// '
		/// difference result: '<p> a<ins> b </ins> </p>
		/// <p>
		/// <ins> c </ins>
		/// </p>
		/// '
		/// this still doesn't guarantee valid HTML (hint: think about diffing a text containing ins or
		/// del tags), but handles correctly more cases than the earlier version.
		/// P.S.: Spare a thought for people who write HTML browsers. They live in this ... every day.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="cssClass"> </param>
		/// <param name="words"> </param>
		private void InsertTag(string tag, string cssClass, List<string> words)
		{
			while (true)
			{
				if (words.Count == 0)
				{
					break;
				}

				var nonTags = ExtractConsecutiveWords(words, x => !IsTag(x));

				var specialCaseTagInjection = string.Empty;
				var specialCaseTagInjectionIsBefore = false;

				if (nonTags.Length != 0)
				{
					var text = WrapText(string.Join("", nonTags), tag, cssClass);

					_content.Append(text);
				}
				else
				{
					// Check if strong tag

					if (_specialCaseOpeningTags.FirstOrDefault(x => Regex.IsMatch(words[0], x)) != null)
					{
						specialCaseTagInjection = "<ins class='mod'>";
						if (tag == "del")
						{
							words.RemoveAt(0);
						}
					}
					else if (_specialCaseClosingTags.Contains(words[0]))
					{
						specialCaseTagInjection = "</ins>";
						specialCaseTagInjectionIsBefore = true;
						if (tag == "del")
						{
							words.RemoveAt(0);
						}
					}
				}

				if (words.Count == 0 && specialCaseTagInjection.Length == 0)
				{
					break;
				}

				if (specialCaseTagInjectionIsBefore)
				{
					_content.Append(specialCaseTagInjection + string.Join("", ExtractConsecutiveWords(words, IsTag)));
				}
				else
				{
					_content.Append(string.Join("", ExtractConsecutiveWords(words, IsTag)) + specialCaseTagInjection);
				}
			}
		}

		private static bool IsClosingTag(string item)
		{
			return Regex.IsMatch(item, "^\\s*</[^>]+>\\s*$");
		}

		private static bool IsEndOfTag(string val)
		{
			return val == ">";
		}

		private bool IsOpeningTag(string item)
		{
			return Regex.IsMatch(item, "^\\s*<[^>]+>\\s*$");
		}

		private bool IsStartOfTag(string val)
		{
			return val == "<";
		}

		private bool IsTag(string item)
		{
			var isTag = IsOpeningTag(item) || IsClosingTag(item);
			return isTag;
		}

		private bool IsWhiteSpace(string value)
		{
			return Regex.IsMatch(value, "\\s");
		}

		private List<DiffMatch> MatchingBlocks()
		{
			var matchingBlocks = new List<DiffMatch>();
			FindMatchingBlocks(0, _oldWords.Length, 0, _newWords.Length, matchingBlocks);
			return matchingBlocks;
		}

		private List<Operation> Operations()
		{
			int positionInOld = 0,
				positionInNew = 0;
			var operations = new List<Operation>();

			var matches = MatchingBlocks();

			matches.Add(new DiffMatch(_oldWords.Length, _newWords.Length, 0));

			foreach (var match in matches)
			{
				var matchStartsAtCurrentPositionInOld = positionInOld == match.StartInOld;
				var matchStartsAtCurrentPositionInNew = positionInNew == match.StartInNew;

				Action action;

				if (matchStartsAtCurrentPositionInOld == false
					&& matchStartsAtCurrentPositionInNew == false)
				{
					action = Action.Replace;
				}
				else if (matchStartsAtCurrentPositionInOld
					&& matchStartsAtCurrentPositionInNew == false)
				{
					action = Action.Insert;
				}
				else if (matchStartsAtCurrentPositionInOld == false)
				{
					action = Action.Delete;
				}
				else // This occurs if the first few words are the same in both versions
				{
					action = Action.None;
				}

				if (action != Action.None)
				{
					operations.Add(
						new Operation(action,
							positionInOld,
							match.StartInOld,
							positionInNew,
							match.StartInNew));
				}

				if (match.Size != 0)
				{
					operations.Add(new Operation(
						Action.Equal,
						match.StartInOld,
						match.EndInOld,
						match.StartInNew,
						match.EndInNew));
				}

				positionInOld = match.EndInOld;
				positionInNew = match.EndInNew;
			}

			return operations;
		}

		private void PerformOperation(Operation operation)
		{
			switch (operation.Action)
			{
				case Action.Equal:
					ProcessEqualOperation(operation);
					break;
				case Action.Delete:
					ProcessDeleteOperation(operation, "diffdel");
					break;
				case Action.Insert:
					ProcessInsertOperation(operation, "diffins");
					break;
				case Action.None:
					break;
				case Action.Replace:
					ProcessReplaceOperation(operation);
					break;
			}
		}

		private void ProcessDeleteOperation(Operation operation, string cssClass)
		{
			var text = _oldWords.Where((s, pos) => pos >= operation.StartInOld && pos < operation.EndInOld).ToList();
			InsertTag("del", cssClass, text);
		}

		private void ProcessEqualOperation(Operation operation)
		{
			var result = _newWords.Where((s, pos) => pos >= operation.StartInNew && pos < operation.EndInNew).ToArray();
			_content.Append(string.Join("", result));
		}

		private void ProcessInsertOperation(Operation operation, string cssClass)
		{
			InsertTag("ins", cssClass, _newWords.Where((s, pos) => pos >= operation.StartInNew && pos < operation.EndInNew).ToList());
		}

		private void ProcessReplaceOperation(Operation operation)
		{
			ProcessDeleteOperation(operation, "diffmod");
			ProcessInsertOperation(operation, "diffmod");
		}

		private void SplitInputsToWords()
		{
			_oldWords = ConvertHtmlToListOfWords(Explode(_oldText));
			_newWords = ConvertHtmlToListOfWords(Explode(_newText));
		}

		private string WrapText(string text, string tagName, string cssClass)
		{
			return string.Format("<{0} class='{1}'>{2}</{0}>", tagName, cssClass, text);
		}

		#endregion
	}

	public class DiffMatch
	{
		#region Constructors

		public DiffMatch(int startInOld, int startInNew, int size)
		{
			StartInOld = startInOld;
			StartInNew = startInNew;
			Size = size;
		}

		#endregion

		#region Properties

		public int EndInNew => StartInNew + Size;

		public int EndInOld => StartInOld + Size;

		public int Size { get; }

		public int StartInNew { get; }

		public int StartInOld { get; }

		#endregion
	}

	public class Operation
	{
		#region Constructors

		public Operation(Action action, int startInOld, int endInOld, int startInNew, int endInNew)
		{
			Action = action;
			StartInOld = startInOld;
			EndInOld = endInOld;
			StartInNew = startInNew;
			EndInNew = endInNew;
		}

		#endregion

		#region Properties

		public Action Action { get; }
		public int EndInNew { get; }
		public int EndInOld { get; }
		public int StartInNew { get; }
		public int StartInOld { get; }

		#endregion
	}

	public enum Mode
	{
		Character,
		Tag,
		Whitespace
	}

	public enum Action
	{
		Equal,
		Delete,
		Insert,
		None,
		Replace
	}
}