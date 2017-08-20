using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Xml;
using journal.console.lib.Models;
using kj.kihon;
using wordxml.Models;

namespace journal.console.lib.Consoles
{
    public class journal03_util : kihon_base
    {
        public List<ParaItem> ParaList { get; set; }

        public journal03_util(ILogger log) : base(log)
        {
            ParaList = new List<ParaItem>();
        }

        public void Run(string jobdir)
        {
            jobdir.existDir();
            var xmldir = jobdir.combine("xml");
            xmldir.existDir();
            var htmdir = jobdir.combine("html");
            htmdir.createDirIfNotExist();

            foreach (var xmlpath in xmldir.getFiles("*.xml"))
            {
                System.Console.WriteLine(xmlpath);
                ParseDocumentXml(xmlpath);
                var txt = CreateHtmlText();
                var outpath = htmdir.combine(xmlpath.getFileNameWithoutExtension() + ".htm");
                System.Console.WriteLine($"==>{outpath}");
                FileUtil.writeTextToFile(txt, Encoding.UTF8, outpath);
            }
        }

        string CreateHtmlText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html>");
            foreach (var para in ParaList)
            {
                sb.AppendLine($"<p>{para.Text}</p>");
            }
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        #region <w:t>の中のテキスト
        string ParseRun(XmlNode node)
        {
            var sb = new StringBuilder();
            if (node.NodeType == XmlNodeType.Text)
            {
                sb.Append(node.Value);
                return sb.ToString();
            }
            sb.Append(node.OuterXml);
            return sb.ToString();
        }
        #endregion

        #region <文章>
        void ParseBunsho(XmlNode bunsho)
        {
            //runは複数
            var sb = new StringBuilder();
            foreach (XmlNode run in bunsho.ChildNodes)  //<w:r>
            {
                sb.Append(ParseRun(run));
            }
            ParaList.Last().Text = sb.ToString();
        }
        #endregion

        #region Parse Xml Path
        public void ParseDocumentXml(string documentPath)
        {
            var sb = new StringBuilder();
            using (var rd = XmlReader.Create(documentPath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(rd);
                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.Name == "文章")
                    {
                        ParaList.Add(new ParaItem());
                        ParseBunsho(node);

                    }
                }
            }
        }
        #endregion
    }
}
