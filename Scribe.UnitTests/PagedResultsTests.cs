#region References

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Data;

#endregion

namespace Scribe.UnitTests
{
	[TestClass]
	public class PagedResultsTests
	{
		#region Methods

		[TestMethod]
		public void HasMoreWithFullLastPage()
		{
			var result = new PagedResults<string>
			{
				PerPage = 4,
				Results = new List<string> { "5", "6", "7", "8" },
				Page = 2,
				TotalCount = 8
			};

			Assert.IsFalse(result.HasMore);
		}

		[TestMethod]
		public void HasMoreWithFullNextToLastPage()
		{
			var result = new PagedResults<string>
			{
				PerPage = 4,
				Results = new List<string> { "5", "6", "7", "8" },
				Page = 2,
				TotalCount = 9
			};

			Assert.IsTrue(result.HasMore);
		}

		[TestMethod]
		public void HasMoreWithNoData()
		{
			var result = new PagedResults<string>
			{
				PerPage = 4,
				Results = new List<string>(),
				Page = 1,
				TotalCount = 0
			};

			Assert.IsFalse(result.HasMore);
		}

		[TestMethod]
		public void HasMoreWithPartialLastPage()
		{
			var result = new PagedResults<string>
			{
				PerPage = 4,
				Results = new List<string> { "5", "6", "7" },
				Page = 2,
				TotalCount = 7
			};

			Assert.IsFalse(result.HasMore);
		}

		[TestMethod]
		public void TotalPages()
		{
			var result = new PagedResults<string> { PerPage = 4, TotalCount = 8 };
			Assert.AreEqual(2, result.TotalPages);
		}

		[TestMethod]
		public void TotalPagesWithAlmostEmptyLastPage()
		{
			var result = new PagedResults<string> { PerPage = 4, TotalCount = 9 };
			Assert.AreEqual(3, result.TotalPages);
		}

		[TestMethod]
		public void TotalPagesWithAlmostFullLastPage()
		{
			var result = new PagedResults<string> { PerPage = 4, TotalCount = 7 };
			Assert.AreEqual(2, result.TotalPages);
		}

		[TestMethod]
		public void TotalPagesWithNoData()
		{
			var result = new PagedResults<string> { PerPage = 4, TotalCount = 0 };
			Assert.AreEqual(0, result.TotalPages);
		}

		#endregion
	}
}