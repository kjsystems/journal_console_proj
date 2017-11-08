using journal.search.lib.Models;
using journal.search.lib.Services;
using NUnit.Framework;

namespace journal.Test.Tests
{
    [TestFixture]
    public class FilterTest
    {
        [Test]
        public void FilterChosha()
        {
            var manager = new SearchManager();
            manager.AddChoshaFilter("久保田　淳");
            Assert.AreEqual("chosha eq '久保田　淳'", SearchFilter.CreateFilter(manager.FilterList));
        }
    }
}