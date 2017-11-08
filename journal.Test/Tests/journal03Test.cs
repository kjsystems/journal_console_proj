using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal.Test.Tests
{
    [TestFixture]
    public class journal03Test
    {
        private string SampleDir => AppDomain.CurrentDomain.BaseDirectory.combine("SampleData");

        [Test]
        public void XMLファイルの読み込み()
        {
            var xmlpath = SampleDir.combine("001.xml");
            Assert.AreEqual(true, xmlpath.existFile(false), "ファイルがない");

            var util = new journal03_util(new ErrorLogger());
            util.ParseDocumentXml(xmlpath);

            //段落の数
            Assert.AreEqual(16, util.ParaList.Count);

            //問答
            Assert.AreEqual(1, util.ParaList[2].Mondo, "問答");
            Assert.AreEqual(6, util.ParaList[3].Mondo, "問答");
            Assert.AreEqual(0, util.ParaList[4].Mondo, "問答");
            //字下
            Assert.AreEqual(28, util.ParaList[10].Jisage, "字下");
            Assert.AreEqual(28, util.ParaList[11].Jisage, "字下");
            Assert.AreEqual(0, util.ParaList[12].Jisage, "字下");
        }
    }
}