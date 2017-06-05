﻿using System;
using System.Text;
using System.Text.RegularExpressions;
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

        private TagBase PreTag { get; set; }
        string CreateTextFromPath(string path)
        {
            var util = new TagTextUtil(Log);
            var taglst = util.parseTextFromPath(path, Encoding.UTF8);

            var sb = new StringBuilder();
            foreach (TagBase tag in taglst)
            {
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
                    sb.Append(ToText(tag));
                    continue;
                }
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
                var util = new ZenHanUtil();
                txt = txt.Replace("＊" + RegexUtil.getGroup(m, 1), "＊" + util.ToHankaku(RegexUtil.getGroup(m, 1)));
            }
            sb.Append(CharUtil.sjis2utf(txt));  //namespaceをUTFに変換
            return sb.ToString();
        }
    }
}
