using journal.search.lib.Services;
using NUnit.Framework;
using System.Linq;

namespace journal.Test
{
    [TestFixture]
    public class TestQurey
    {
        [Test]
        public void TestQureyHonbun_藤原定家()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "藤原定家" }.ToList());
            Assert.AreEqual(19, result.Count);
        }
        [Test]
        public void TestQureyHonbun_逢恋()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "逢恋" }.ToList());
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(1, result[0].Go);
            Assert.AreEqual(38, result[0].Page, "Page");
            Assert.AreEqual("001-04", result[0].FileName, "FileName");
            Assert.AreEqual("今宵は越えぬ逢坂の関（初逢恋・一七一）", result[0].Text, "Text");
            Assert.AreEqual("藤原重家の詠法", result[0].Title, "Title");
            Assert.AreEqual("―典拠のある作を中心に―", result[0].SubTitle, "SubTitle");

            //ページ順
            Assert.AreEqual(38, result[0].Page);
            Assert.AreEqual(43, result[1].Page);
            Assert.AreEqual(59, result[2].Page);
            Assert.AreEqual(108, result[3].Page);
            Assert.AreEqual(118, result[4].Page);
        }
        [Test]
        public void TestQureyHonbun_正徹()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "正徹" }.ToList());
            Assert.AreEqual(51, result.Count);
        }
        [Test]
        public void TestQureyHonbun_正徹あああ()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "正徹あああ" }.ToList());
            Assert.AreEqual(0, result.Count);
        }
        [Test]
        public void TestQureyHonbun_And_逢恋_正徹()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "逢恋", "正徹" }.ToList());
            Assert.AreEqual(0, result.Count);
        }
        [Test]
        public void TestQureyHonbun_Or_逢恋_正徹()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "逢恋", "正徹" }.ToList());
            Assert.AreEqual(0, result.Count);
        }
        [Test]
        public void TestQureyHonbun_And_藤原定家_古今和歌集()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "藤原定家", "古今和歌集" }.ToList());
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("001-01", result[0].FileName);
        }
        [Test]
        public void TestQureyHonbun_Or_藤原定家_古今和歌集()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "藤原定家", "古今和歌集" }.ToList(), SearchManager.JournalSearchMode.Or);
            Assert.AreEqual(43, result.Count);
        }
        [Test]
        public void TestQureyHonbun_Or_定家()
        {
            var ser = new SearchManager();
            var result = ser.QueryHonbun(new[] { "定家" }.ToList(), SearchManager.JournalSearchMode.Or);
            Assert.AreEqual(158, result.Count);
        }
    }
}
