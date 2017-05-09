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
        public void TestWordPara_上付き注釈()  //（＊１）
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
            Assert.AreEqual("<上付>（＊１）</上付>", parser.ParseParagraph(n));
        }
    }
}