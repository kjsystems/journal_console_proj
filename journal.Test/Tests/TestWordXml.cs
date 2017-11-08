using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using journal.search.lib.Services;
using kj.kihon;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using wordxml.Models;

namespace journal.Test
{
    [TestFixture]
    class TestWordXml
    {
        XmlDocument CreateDocument(string txt)
        {
            string xmlContent = @"<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">"
                                + txt
                                + "</w:document>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            return doc;
        }

        [Test]
        public void TestWordRun_下線点線()
        {
            var xml = @"<w:r w:rsidRPr=""0057566F""><w:rPr>
                <w:szCs w:val=""21""/><w:u w:val=""dotted""/></w:rPr>
                <w:t>レ</w:t></w:r>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:r", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("<上線 種類=点線>レ</上線>", parser.ParseRun(n));
        }

        [Test]
        public void TestWordRun_上付き()
        {
            var xml = @"<w:r w:rsidRPr=""0057566F""><w:rPr>
                <w:rFonts w:ascii=""HG教科書体"" w:eastAsia=""HG教科書体"" w:hAnsiTheme=""majorEastAsia"" w:hint=""eastAsia""/>
                <w:szCs w:val=""21""/><w:vertAlign w:val=""subscript""/></w:rPr>
                <w:t>レ</w:t></w:r>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:r", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("<下付>レ</下付>", parser.ParseRun(n));
        }

        [Test]
        public void TestWordRun_下付き()
        {
            var xml = @"<w:r w:rsidRPr=""0057566F""><w:rPr>
                <w:szCs w:val=""21""/><w:vertAlign w:val=""superscript""/></w:rPr>
                <w:t>レ</w:t></w:r>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:r", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("<上付>レ</上付>", parser.ParseRun(n));
        }

        [Test]
        public void TestWordRun_圏点()
        {
            var xml = @"<w:r w:rsidRPr=""0057566F""><w:rPr>
                <w:szCs w:val=""21""/><w:em w:val=""comma""/></w:rPr>
                <w:t>勿論</w:t></w:r>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:r", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("<圏点>勿論</圏点>", parser.ParseRun(n));
        }

        [Test]
        public void TestWordPara_上付き注釈() //（＊１）
        {
            var xml = @"<w:p w:rsidR=""0091204C"" w:rsidRDefault=""0091204C"" w:rsidP=""00C136F2"">
<w:r w:rsidR=""00E91A82"" w:rsidRPr=""00E91A82""><w:rPr>
<w:rFonts w:ascii=""HG教科書体"" w:eastAsia=""HG教科書体"" w:hAnsi=""ＭＳ 明朝"" w:hint=""eastAsia""/>
<w:szCs w:val=""21""/><w:vertAlign w:val=""superscript""/></w:rPr><w:t>（＊</w:t></w:r>
<w:r w:rsidRPr=""00E91A82""><w:rPr><w:rStyle w:val=""af""/>
<w:rFonts w:ascii=""HG教科書体"" w:eastAsia=""HG教科書体"" w:hAnsi=""ＭＳ 明朝""/>
<w:szCs w:val=""21""/></w:rPr><w:endnoteReference w:id=""1""/></w:r>
<w:r w:rsidR=""00E91A82"" w:rsidRPr=""00E91A82""><w:rPr>
<w:rFonts w:ascii=""HG教科書体"" w:eastAsia=""HG教科書体"" w:hAnsi=""ＭＳ 明朝"" w:hint=""eastAsia""/>
<w:szCs w:val=""21""/><w:vertAlign w:val=""superscript""/></w:rPr><w:t>）</w:t></w:r>
</w:p>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:p", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("<上付>（＊</上付>１<上付>）</上付>", parser.ParseParagraph(n));
        }

        [Test]
        public void TestWordPara_最初の行１字下げ() //（＊１）
        {
            var xml = @"<w:p w:rsidR=""00057E78"" w:rsidRDefault=""002F0078"" w:rsidP=""00C136F2"">
<w:pPr><w:ind w:firstLineChars=""100"" w:firstLine=""210""/>
<w:rPr><w:rFonts w:ascii=""HG教科書体"" w:eastAsia=""HG教科書体"" w:hAnsi=""ＭＳ 明朝""/><w:szCs w:val=""21""/>
</w:rPr></w:pPr>
<w:r><w:rPr><w:szCs w:val=""21""/></w:rPr><w:t>『源家長日記』（以下、『日記</w:t></w:r>
</w:p>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:p", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("『源家長日記』（以下、『日記", parser.ParseParagraph(n));
            Assert.AreEqual(1, parser.ParaList[0].Jisage, "jisage");
            Assert.AreEqual(-1, parser.ParaList[0].Mondo, "mondo");
        }

        [Test]
        public void TestWordPara_ルビ() //（＊１）
        {
            var xml = @"
<w:p w:rsidR=""00057E78"" w:rsidRDefault=""002F0078"" w:rsidP=""00C136F2"">
<w:r><w:rPr><w:szCs w:val=""21""/></w:rPr><w:t>これら</w:t></w:r>
<w:r w:rsidR=""00DB0DE2""><w:rPr></w:rPr>
    <w:ruby>
        <w:rubyPr>
            <w:rubyAlign w:val=""distributeSpace""/><w:hps w:val=""10""/><w:hpsRaise w:val=""18""/><w:hpsBaseText w:val=""21""/><w:lid w:val=""ja-JP""/>
        </w:rubyPr>
        <w:rt>
            <w:r w:rsidR=""00DB0DE2"" w:rsidRPr=""00DB0DE2""><w:rPr><w:sz w:val=""10""/><w:szCs w:val=""21""/></w:rPr><w:t>（＊にカ）</w:t></w:r>
        </w:rt>
        <w:rubyBase>
            <w:r w:rsidR=""00DB0DE2""><w:rPr><w:szCs w:val=""21""/></w:rPr><w:t>ま</w:t></w:r>
        </w:rubyBase>
    </w:ruby>
</w:r></w:p>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:p", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("これら<ruby>ま<rt>（＊にカ）</rt></ruby>", parser.ParseParagraph(n));
        }

        [Test]
        public void TestWordPara_一太郎ルビ() //（＊１）
        {
            var xml = @"
<w:p w:rsidR=""00000000"" w:rsidRDefault=""00B250F1"">
<w:r><w:rPr><w:rFonts w:hint=""eastAsia""/></w:rPr><w:t xml:space=""preserve"">あ</w:t>
</w:r>
<w:r><w:fldChar w:fldCharType=""begin""/></w:r>
<w:r><w:instrText xml:space=""preserve""> eq \o\ad(\s\up 9(</w:instrText></w:r>
<w:r><w:instrText>せい</w:instrText></w:r>
<w:r><w:instrText>),</w:instrText></w:r>
<w:r><w:instrText>青</w:instrText></w:r>
<w:r><w:instrText>)</w:instrText></w:r>
<w:r><w:fldChar w:fldCharType=""end""/></w:r>
</w:p>";
            var n = CreateDocument(xml).DocumentElement.FirstChild;
            Assert.AreEqual("w:p", n.Name);
            var parser = new WordXmlParser(210, new ErrorLogger());
            Assert.AreEqual("あ<ruby>青<rt>せい</rt></ruby>", parser.ParseParagraph(n));
        }
    }
}