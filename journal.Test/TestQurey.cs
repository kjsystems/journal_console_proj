using System;
using System.Collections.Generic;
using System.Linq;
using journal.search.lib.Services;
using NUnit.Framework;

namespace journal.Test
{
  [TestFixture]
  public class TestQurey
  {
    [Test]
    public void TestQureyHonbun_One()
    {
      var ser = new SearchManager();
      ser.CreateSearchIndexClient();
      var result = ser.QueryHonbun(new[] { "藤原定家" }.ToList());
      Assert.AreEqual(50, result.Count);
    }
    [Test]
    public void TestQureyHonbun_And()
    {
      var ser = new SearchManager();
      ser.CreateSearchIndexClient();
      var result = ser.QueryHonbun(new[] { "藤原定家","古今和歌集" }.ToList());
      Assert.AreEqual(6, result.Count);
      Assert.AreEqual("001-03", result[0].FileName);
      Assert.AreEqual("・同1991『藤原定家筆古今和歌集』汲古書院", result[0].Text);
    }
    [Test]
    public void TestQureyHonbun_Or()
    {
      var ser = new SearchManager();
      ser.CreateSearchIndexClient();
      var result = ser.QueryHonbun(new[] { "藤原定家", "古今和歌集" }.ToList(), SearchManager.JournalSearchMode.Any);
      Assert.AreEqual(50, result.Count);
    }
  }
}
