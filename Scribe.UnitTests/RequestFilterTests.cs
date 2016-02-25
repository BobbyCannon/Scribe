#region References

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Data;

#endregion

namespace Scribe.UnitTests
{
	[TestClass]
	public class RequestFilterTests
	{
		#region Methods

		[TestMethod]
		public void DualFilters()
		{
			var input = "Item1=Value1;Item2=Value2;";
			var actual = RequestFilter.Parse(input);

			Assert.AreEqual(2, actual.Count);
			Assert.AreEqual("Item1", actual.First().Key);
			Assert.AreEqual("Value1", actual.First().Value);
			Assert.AreEqual("Item2", actual.Last().Key);
			Assert.AreEqual("Value2", actual.Last().Value);
		}

		[TestMethod]
		public void DualFiltersWithoutTermination()
		{
			var input = "Item1=Value1;Item2=Value2";
			var actual = RequestFilter.Parse(input);

			Assert.AreEqual(2, actual.Count);
			Assert.AreEqual("Item1", actual.First().Key);
			Assert.AreEqual("Value1", actual.First().Value);
			Assert.AreEqual("Item2", actual.Last().Key);
			Assert.AreEqual("Value2", actual.Last().Value);
		}

		[TestMethod]
		public void FilterKeyShouldTrimButValueShouldNot()
		{
			var input = "Item = Value;";
			var actual = RequestFilter.Parse(input);

			Assert.AreEqual(1, actual.Count);
			Assert.AreEqual("Item", actual.First().Key);
			Assert.AreEqual(" Value", actual.First().Value);
		}

		[TestMethod]
		public void SingleFilter()
		{
			var input = "Item1=Value2;";
			var actual = RequestFilter.Parse(input);

			Assert.AreEqual(1, actual.Count);
			Assert.AreEqual("Item1", actual.First().Key);
			Assert.AreEqual("Value2", actual.First().Value);
		}

		[TestMethod]
		public void SingleFilterWithoutTermination()
		{
			var input = "Item1=Value";
			var actual = RequestFilter.Parse(input);

			Assert.AreEqual(1, actual.Count);
			Assert.AreEqual("Item1", actual.First().Key);
			Assert.AreEqual("Value", actual.First().Value);
		}

		#endregion
	}
}