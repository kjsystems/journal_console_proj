using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using journal.console.lib.Models;
using kj.kihon;
using kj.kihon.Utils;

namespace journal.console.lib.Consoles
{
    public class journal06_util : kihon_base
    {
        public journal06_util(ILogger log) : base(log)
        {
        }

        public void Run(string jobdir)
        {
            jobdir.existDir();
            var txtdir = jobdir.combine("txt");
            txtdir.existDir();
            var kjpdir = jobdir.combine("kjp").createDirIfNotExist();

            foreach (var srcpath in txtdir.getFiles("*.txt"))
            {
                Path = srcpath;
                var outpath = kjpdir.combine(srcpath.getFileNameWithoutExtension() + ".kjp");
                System.Console.WriteLine($"{srcpath}");
                System.Console.WriteLine($"==>{outpath}");

                FileUtil.writeTextToFile(CreateTextFromPath(srcpath), Encoding.UTF8, outpath);
            }
        }

        public void RunFromPath(string srcpath, string outpath)
        {
            Path = srcpath;
            System.Console.WriteLine($"{srcpath}");
            System.Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(CreateTextFromPath(srcpath), Encoding.UTF8, outpath);
        }

        // 先頭改行を削除
        string TrimLeftKaigyo(string buf)
        {
            char[] tbl = { '\r', '\n' };
            while (true)
            {
                if (buf.Length > 0 && Array.IndexOf(tbl, buf[0]) >= 0)
                {
                    buf = buf.Substring(1);
                    continue;
                }
                break;
            }
            return buf;
        }

        // <改行>LISTを作成 <改行>は最後に入る
        void CreateKaigyoList(string path, out List<ParaItem> paralst)
        {
            paralst = new List<ParaItem>();
            var util = new TagTextUtil(Log);
            var taglst = util.parseTextFromPath(path, Encoding.UTF8);

            var sb=new StringBuilder();
            foreach (var tag in taglst)
            {
                // 先頭改行を削除
                sb.Append(TrimLeftKaigyo( tag.ToString()));
                if (tag.getName() == "改行")
                {
                    paralst.Add(new ParaItem { Gyo = tag.GyoNo, Text = sb.ToString() });
                    sb.Length = 0;
                }
            }
        }

        void SetJisageMondo(ParaItem para, ref int curJisage, ref int curMondo)
        {

            var taglst = TagTextUtil.parseText(para.Text);
            foreach (TagBase tag in taglst)
            {
                //<揃字>で字下はリセットしない
                //if (tag.getName() == "揃字") 
                //{
                //    para.IsJisoroe = true;
                //    curJisage = 0;
                //    curMondo = 0;
                //}
                if (tag.getName() == "字下")
                    curJisage = tag.getValue("").toInt(0);
                if (tag.getName() == "問答")
                    curMondo = tag.getValue("").toInt(0);
            }
            para.Jisage = curJisage;
            para.Mondo = curMondo;
        }

        private TagBase PreTag { get; set; }
        string CreateTextFromPath(string path)
        {
            Path = path;

            //改行ごとに処理する
            CreateKaigyoList(path,out List<ParaItem> paralst);

            var sb = new StringBuilder();
            var curJisage = 0;
            var curMondo = 0;
            foreach (var para in paralst)
            {
                //<選択 ラベル名>を先に読み込み
                var taglst = TagTextUtil.parseText(para.Text);
                var label = taglst.FirstOrDefault(m => m.getName() == "選択"); 
                if (label!= null)
                    sb.Append(label.ToString());
                
                //字下,問答を各段落に設定する
                SetJisageMondo(para,ref curJisage,ref curMondo);

                if(para.IsJisoroe!=true)
                    sb.Append($"<字下 {para.Jisage}><問答 {para.Mondo}>");
                sb.Append(CreateTextFromPara(para));
                sb.Append("\r\n");
            }
            return sb.ToString().Replace("――", "<分禁>――</分禁>");
        }

        string CreateTextFromPara(ParaItem para)
        {
            var dict = new Dictionary<string, int>();
            dict["ruby"] = 0;
            dict["rt"] = 0;

            var sb = new StringBuilder();
            var taglst = TagTextUtil.parseText(para.Text);
            foreach (var tag in taglst)
            {
                string[] mushi = { "字下", "問答","選択" };
                if (Array.IndexOf(mushi, tag.getName()) >= 0)
                    continue;

                const string TAG_KASEN = "下線";
                const string TAG_RUBY = "ruby";
                if (tag.getName() == "上線")
                {
                    if (tag.isClose())
                        ((TagItem)tag).name_ = "/" + TAG_KASEN;
                    else
                        ((TagItem)tag).name_ = TAG_KASEN;
                }

                if (Array.IndexOf(new[] { TAG_KASEN, TAG_RUBY }, tag.getName()) >= 0)
                {
                    if (PreTag != null && PreTag.getName() == tag.getName() && PreTag.isOpen() == tag.isOpen())
                        Log.err(Path, tag.GyoNo, "parsetag", $"タグの組み合わせがおかしい tag={tag.ToString()} isopen={tag.isOpen()} pre={PreTag.isOpen()}");
                }
                //文字列
                if (tag.isText())
                {
                    if (dict["ruby"] > 0 && dict["rt"] == 2)
                    {
                        Log.err(Path, tag.GyoNo, "journal06", $"<rt>の文字列は無効 [{tag.ToString()}]");
                    }

                    sb.Append(ToText(tag));
                    continue;
                }
                if (tag.getName() == "ruby")
                {
                    dict["ruby"] = tag.isOpen() ? 1 : 0;
                    dict["rt"] = 0;  //reset
                }
                if (tag.getName() == "rt")
                    dict["rt"] = tag.isOpen() ? 1 : 2;  //閉じたら2
                //タグ
                sb.Append(ToTag(tag));
                if (tag.isTag())
                {
                    PreTag = tag;
                }
            }
            return sb.ToString();
        }

        string ToTag(TagBase tag)
        {
            //<大字>は<ス字 大字>
            if (tag.getName() == "大字")
            {
                if (tag.isOpen() != true)
                    return "</ス字>";
                return $"<ス字 大字>";
            }
            return tag.ToString();
        }

        string ToText(TagBase tag)
        {
            var sb = new StringBuilder();

            var txt = tag.ToString()
              .Replace("", "&#x3033;")
              .Replace("〱", "α")
              .Replace("α", "&#x3033;&#x3035;")
              .Replace("&#12349;", "ヽ")
              .Replace("", "&#x3035;")
              .Replace("", "&#x303b;")
              .Replace("β", "&#x303b;")
              .Replace("￥", "")
              .Replace("$", "＄")
              .Replace("＄", "&#x25e6;")
              .Replace(")", "）")
              .Replace("(", "（");
            //2桁全角を変換
            var reg = new Regex("＊([０-９]{2,3})");
            while (reg.IsMatch(txt))
            {
                var m = reg.Match(txt);
                txt = txt.Replace("＊" + RegexUtil.getGroup(m, 1), "＊<ス字 ゴシ>" + ZenHanUtil.ToHankaku(RegexUtil.getGroup(m, 1))+ "</ス字>");
            }
            //1桁全角を変換
            reg = new Regex("＊([０-９]{1})");
            while (reg.IsMatch(txt))
            {
                var m = reg.Match(txt);
                txt = txt.Replace("＊" + RegexUtil.getGroup(m, 1), "＊<ス字 ゴシ>" + RegexUtil.getGroup(m, 1) + "</ス字>");
            }
            sb.Append(CharUtil.sjis2utf(txt));  //namespaceをUTFに変換
            return sb.ToString();
        }
    }
}
