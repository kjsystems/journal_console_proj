using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using journal.lib.Models;
using kj.kihon;

namespace journal.console.lib.Models
{
    public class CreateWakaXmlCore : kihon_base
    {
        private string OutPath { get; set; }

        public CreateWakaXmlCore(ILogger log) : base(log)
        {
        }

        const string MADO_MIGI = "窓右";
        const string MADO_HIDARI = "窓左";
        Encoding Enc = Encoding.UTF8;

        string outMadoList(List<MadoItem> madolst)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<窓一覧>\r\n");
            foreach (var item in madolst.Select((v, i) => new {v, i}))
            {
                int level = item.v.IsLeft == true ? 1 : 0;
                sb.Append($"<窓 ID=\"{item.i + 1}\" HREF=\"{item.v.href}\" レベル=\"{level}\" 内容=\"{item.v.text}\" />\r\n");
            }
            sb.Append("</窓一覧>\r\n");
            return sb.ToString();
        }

        // 02-001.pdf ==> 002-02-001.pdf
        // .\image\02-001.pdf ==> 002-02-001.pdf
        string ChangeSanshoFileName(string fname)
        {
            fname = fname.getFileName();  //参照のディレクトリなど削除する
            var reg = new Regex(@"^([0-9]{2}-[0-9]{3})\..*?");
            if (reg.IsMatch(fname))
            {
                var match = reg.Match(fname);
                // 出力ファイル名からせ先頭3バイトを取得 001-01.xml ==> 001 
                var sento = OutPath.getFileNameWithoutExtension().Substring(0, 3);
                return $"{sento}-{fname}";
            }
            return fname;
        }


        //<窓左><窓右>を取り除く
        string outWakaXmlBunsho(TagList taglst, ref int jisage, ref int mondo)
        {
            bool bMado = false;
            StringBuilder sb = new StringBuilder();
            foreach (TagBase tag in taglst)
            {
                string[] mushi = {"KEISEN"};
                if (Array.IndexOf(mushi, tag.getName()) >= 0)
                    continue;
                if (tag.getName() == "改行") continue;
                if (tag.getName() == "設定")
                {
                    sb.Append($"<設定 縦横=\"{tag.getValue("縦横")}\"/>");
                    continue;
                }
                //<名前>は窓を抽出するときに使用済み
                if (Array.IndexOf(new string[] {"名前"}, tag.getName()) >= 0)
                    continue;

                //窓の間は無視する
                string[] tbl = {MADO_HIDARI, MADO_MIGI};
                if (Array.IndexOf(tbl, tag.getName()) >= 0)
                {
                    bMado = tag.isOpen();
                    continue;
                }
                if (bMado == true) continue;
                if (tag.getName() == "問答")
                {
                    mondo = tag.getValue("", true).toInt(-1);
                    //bFoundMondo = true;
                    continue;
                }
                if (tag.getName() == "字下")
                {
                    jisage = tag.getValue("", true).toInt(-1);
                    //bFoundJisage = true;
                    continue;
                }
                if (tag.getName() == "頁")
                {
                    sb.Append($"<頁 内容=\"{tag.getValue("", true)}\"/>");
                    continue;
                }
                if (tag.getName() == "字Ｓ" || (Array.IndexOf(new string[] {"揃字", "字揃"}, tag.getName()) >= 0 &&
                                              tag.getValue("") == "右"))
                {
                    sb.Append($"<字エ/>"); //XMLでＳは使えないので
                    continue;
                }
                if (tag.getName() == "参照")
                {
                    if(tag.isOpen()!=true)
                        continue;
                    var fname = tag.getValue("");
                    if (string.IsNullOrEmpty(fname))
                    {
                        fname = tag.getValue("fname");
                        if (string.IsNullOrEmpty(fname))
                        {
                            Log.err(Path, tag.GyoNo, "wakaxml", "<参照>にファイル名がない");
                            continue;
                        }
                    }
                    
                    
                    // 02-001.pdf ==> 002-02-001.pdf
                    var linkFname = ChangeSanshoFileName(fname);

                    sb.Append($"<参照 fname=\"{linkFname}\">{linkFname}</参照>");
                    continue;
                }
                string[] tgt = {"上線", "下線"};
                if (Array.IndexOf(tgt, tag.getName()) >= 0 && tag.isOpen())
                {
                    sb.Append($"<{tag.getName()} 種類=\"{tag.getValue("種類")}\">");
                    continue;
                }
                if (tag.getName() == "外字")
                {
                    string[] tbl1 = {"耀しんにょう"};
                    string[] tbl2 = {"gaiji-001.png"};
                    string fname = tag.getValue("");
                    int idx = Array.IndexOf(tbl1, tag.getValue(""));
                    if (idx < 0)
                        Log.err(Path, tag.GyoNo, "waka_xml", $"対象のファイル名なし {tag.ToString()}");
                    else
                    {
                        fname = fname.Replace(tbl1[idx], tbl2[idx]);
                    }
                    sb.Append($"<外字 fname=\"..\\gaiji\\{fname}\"/>");
                    continue;
                }
                if (tag.isText() == true)
                    ((TagText) tag).text_ = ((TagText) tag).text_.Replace("/", "");

                sb.Append(replaceWakaXmlText(tag.ToString()));
            }
            return sb.ToString();
        }

        string replaceWakaXmlText(string buf)
        {
            return buf.Replace("α", "〳〵")
                    //.Replace("β", "")		//下記に変更 2016.4.1
                    .Replace("β", "〻")
                ;
        }

        public string createBunshoText(List<string> strlst)
        {
            var sb = new StringBuilder();
            var jisage = -1;
            var mondo = -1;
            foreach (var item in strlst.Select((v, i) => new {v, i}))
            {
                int gyono = item.i + 1;
                TagList taglst = TagTextUtil.parseText(item.v, ref gyono, false);
                int idx = item.i + 1;
                //bool bFoundJisage = false;
                //bool bFoundMondo = false;
                string buf = outWakaXmlBunsho(taglst, ref jisage, ref mondo);

                //どっちかだけtrueなら一方をリセット
                //if (bFoundJisage == true && bFoundMondo != true) //字下のみ
                //{
                //    mondo = 0; //問答リセット
                //}
                //if (bFoundJisage != true && bFoundMondo == true) //問答のみ
                //{
                //    jisage = 0; //字下リセット
                //}
                sb.Append($"<文章 ID=\"{idx}\" 行番=\"{idx}\" 組行=\"{idx}\"");
                if (jisage > 0)
                {
                    sb.Append($" 字下=\"{jisage}\"");
                }
                if (mondo > 0)
                {
                    sb.Append($" 問答=\"{mondo}\"");
                }
                sb.Append($">{CharUtil.delKaigyo(buf)}</文章>\r\n");
            }
            return sb.ToString();
        }

        //const string InitDataDir = @"C:\git\waka_proj\kenkyu.web\kenkyu.web\InitData\";
        /**
            本文XML(<窓一覧>)ではなくて入稿テキスト用 <窓右><窓左>
            */
        public List<MadoItem> getMadoList(List<string> strlst)
        {
            List<MadoItem> lst = new List<MadoItem>();
            foreach (var item in strlst.Select((v, i) => new {v, i}))
            {
                var mado = "";
                var bMado = false;
                int gyono = item.i + 1;
                var taglst = TagTextUtil.parseText(item.v, ref gyono, false);
                foreach (TagBase tag in taglst)
                {
                    string[] tbl = {MADO_MIGI, MADO_HIDARI};
                    if (tag.isOpen() == true && Array.IndexOf(tbl, tag.getName()) >= 0)
                    {
                        bMado = true;
                        mado = tag.getName();
                    }
                    if (tag.isOpen() != true && Array.IndexOf(tbl, tag.getName()) >= 0)
                    {
                        bMado = false;
                    }
                    //窓の文字列を取得
                    if (tag.isText() == true && bMado == true)
                    {
                        lst.Add(new MadoItem()
                        {
                            IsLeft = (mado == MADO_HIDARI),
//                            IsRight = (mado == MADO_MIGI),
                            text = tag.ToString(),
                            href = item.i + 1
                        });
                        continue;
                    }
                    if (tag.getName() == "名前")
                    {
                        var name = lst.FirstOrDefault(m => m.text == tag.getValue(""));
                        if (name == null)
                        {
                            Log.err(Path, item.i + 1, "madolist", $"<名前>で指定した窓がない name={tag.getValue("")}");
                            continue;
                        }
                        name.href = item.i + 1; //リンク先を設定する
                    }
                }
            }
            if (lst.Count == 0)
            {
                Log.err(Path, 0, "madolist", "<窓右><窓左>を1つも取得できない");
            }

            return lst;
        }

        /**
        strlstに $"<文章>{cur}</文章>\r\n");  して出力
            */
        string createWakaXmlFromStrList(List<string> strlst)
        {
            //窓情報を抽出
            //	<窓右><窓左>テキストを読み込む
//			kenkyu02_util util = new kenkyu02_util(Log);
            List<MadoItem> madolst = getMadoList(strlst);

            StringBuilder sb = new StringBuilder();
            sb.Append(XMLUtil.XMLHEAD + "\r\n");
            sb.Append("<WEB和歌>\r\n");
            //窓一覧
            sb.Append(outMadoList(madolst));
            //本文 置き換え
            sb.Append(createBunshoText(strlst)
                .Replace("<下線>庭<上付></下線>皇后宮会</上付>", "<下線>庭</下線><上付>皇后宮会</上付>")
                .Replace("<下線>祖父三品匠作、於此所伴好客翫時鳥、故有此詠</文章>", "<下線>祖父三品匠作、於此所伴好客翫時鳥、故有此詠</下線></文章>")
                .Replace("<字エ/></下線>", "<字エ/>") //上とセット
                .Replace("<縦横>（2紙）</文章>", "<縦横>（2紙）</縦横></文章>")
                .Replace("組行=\"34\"></縦横>", "組行=\"34\">")
                .Replace("組行=\"35\"></縦横>", "組行=\"35\">")
                //.Replace("堀川 属性=\"GROUP\">", "堀川<ruby 属性=\"GROUP\">")
//        .Replace("拙稿2016を参照されたい。</小字>", "拙稿2016を参照されたい。")
//        .Replace("桑門明静　在判」</小字>", "桑門明静　在判」")
//        .Replace("全不称自説之有謂」とある。</小字>", "全不称自説之有謂」とある。")
//        .Replace("拙稿2015で述べた。</小字>", "拙稿2015で述べた。")
//        .Replace("箇所ではない。</小字>", "箇所ではない。")
//        .Replace("</太字>】</小字>", "</太字>】")
//        .Replace("に結実している。</小字>", "に結実している。")
//        .Replace("二〇一四・三）も存している。</小字>", "二〇一四・三）も存している。")
                .Replace("<圏点 種類=\"ヽ\"><割注>旅宿</圏点>", "<割注><圏点 種類=\"ヽ\">旅宿</圏点>")
//        .Replace("補注９を参照いただきたい。</小字>", "補注９を参照いただきたい。")
//        .Replace("よろしくご了解いただきたい。</小字>", "よろしくご了解いただきたい。")
                //       .Replace("解釈が加えられている。</小字>", "解釈が加えられている。")
                //       .Replace("確定しておられる。</小字>", "確定しておられる。")
//        .Replace("検討する必要があるように考える。</小字>", "検討する必要があるように考える。")
//        .Replace("例がここに分類できるよう。</小字>", "例がここに分類できるよう。")
//        .Replace("簡略な表現となっている。</小字>", "簡略な表現となっている。")
                //.Replace("<ruby>言家", "</ruby>言家")
                .Replace("<上線>庭<上付></上線>皇后宮会</上付>", "<上線>庭</上線><上付>皇后宮会</上付>")
            );

            sb.Append("</WEB和歌>");
            return sb.ToString();
        }

        /**

        */
        public void run(string txtpath, string outpath, bool isCheckOnly = false)
        {
            try
            {
                Path = txtpath;
                OutPath = outpath;

                //文字列をまずは置き換える
                //出力ファイルを切り替え .out
                //var strlst = FileUtil.getTextListFromPath(Path, Enc);

                //<改行>ごとに読み込む
                var util = new FileUtil(Log);
                var strlst = util.CreateKaigyoListFromPath(Path, Enc);

                if (isCheckOnly)
                    return;

                //strlstに $"<文章>{cur}</文章>\r\n");  して出力
                FileUtil.writeTextToFile(createWakaXmlFromStrList(strlst), Encoding.UTF8, outpath);
            }
            catch (Exception ex)
            {
                Log.err(Path, Gyono, "wakaxml", ex.Message);
            }
        }
    }
}