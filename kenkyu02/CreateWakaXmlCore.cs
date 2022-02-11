using kenkyu.lib.Model;
using kj.kihon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using kjlib.lib.Models;

namespace kenkyu.lib
{
    public class CreateWakaXmlCore : kihon_base
    {
        public CreateWakaXmlCore(ILogger log) : base(log)
        {
        }
        const string MADO_MIGI = "窓右";
        const string MADO_HIDARI = "窓左";
//        Encoding Enc = Encoding.Unicode;

        string outMadoList(List<MadoItem> madolst)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<窓一覧>\r\n");
            foreach (var item in madolst.Select((v, i) => new { v, i }))
            {
                int level = item.v.IsLeft == true ? 0 : 1;
                sb.Append($"<窓 ID=\"{item.i + 1}\" HREF=\"{item.v.href}\" レベル=\"{level}\" 内容=\"{item.v.text}\" />\r\n");
            }
            sb.Append("</窓一覧>\r\n");
            return sb.ToString();
        }
        //<窓左><窓右>を取り除く
        string outWakaXmlBunsho(TagList taglst, ref int jisage, ref bool bFoundJisage, ref int mondo, ref bool bFoundMondo)
        {
            bool bMado = false;
            StringBuilder sb = new StringBuilder();
            foreach (TagBase tag in taglst)
            {
                string[] mushi = { "KEISEN", "著者", "著者かな", "選択", "スタ", "注釈本文", "注釈タイトル", "小見出し", "見出し名前" };
                if (Array.IndexOf(mushi, tag.getName()) >= 0)
                    continue;
                if (tag.getName() == "改行") continue;
                if (tag.getName() == "設定")
                {
                    sb.Append($"<設定 縦横=\"{tag.getValue("縦横")}\"/>");
                    continue;
                }
                //<名前>は窓を抽出するときに使用済み
                if (Array.IndexOf(new string[] { "名前" }, tag.getName()) >= 0)
                    continue;

                //窓の間は無視する
                string[] tbl = { MADO_HIDARI, MADO_MIGI };
                if (Array.IndexOf(tbl, tag.getName()) >= 0)
                {
                    bMado = tag.isOpen();
                    continue;
                }
                if (bMado == true) continue;
                if (tag.getName() == "問答")
                {
                    mondo = tag.getValue("", true).toInt(-1);
                    bFoundMondo = true;
                    continue;
                }
                if (tag.getName() == "字下")
                {
                    jisage = tag.getValue("", true).toInt(-1);
                    bFoundJisage = true;
                    continue;
                }
                if (tag.getName() == "頁")
                {
                    sb.Append($"<頁 内容=\"{tag.getValue("", true)}\"/>");
                    continue;
                }
                if (tag.getName() == "字Ｓ" || (tag.getName() == "揃字" && tag.getValue("") == "右")
                                          || (tag.getName() == "字揃" && tag.getValue("") == "右"))
                {
                    sb.Append($"<字エ/>");    //XMLでＳは使えないので
                    continue;
                }
                if (tag.getName() == "参照")
                {
                    var fname = tag.getValue("");
                    sb.Append($"<参照 fname=\"{fname}\">{fname}</参照>");
                    continue;
                }

                if (tag.getName() == "項段")
                {
                    sb.Append("<項段/>");
                    continue;
                }
                if (tag.getName() == "外字")
                {
                    string[] tbl1 = { "耀しんにょう" };
                    string[] tbl2 = { "gaiji-001.png" };
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
                    ((TagText)tag).text_ = ((TagText)tag).text_.Replace("/", "");

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
            foreach (var item in strlst.Select((v, i) => new { v, i }))
            {
                int gyono = item.i + 1;
                TagList taglst = TagTextUtil.parseText(item.v, ref gyono, false);
                int idx = item.i + 1;
                bool bFoundJisage = false;
                bool bFoundMondo = false;
                string buf = outWakaXmlBunsho(taglst, ref jisage, ref bFoundJisage, ref mondo, ref bFoundMondo);

                //どっちかだけtrueなら一方をリセット
                if (bFoundJisage == true && bFoundMondo != true)    //字下のみ
                {
                    mondo = 0;      //問答リセット
                }
                if (bFoundJisage != true && bFoundMondo == true)    //問答のみ
                {
                    jisage = 0;   //字下リセット
                }
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

        /**
		strlstに $"<文章>{cur}</文章>\r\n");  して出力
			*/
        string createWakaXmlFromStrList(List<string> strlst)
        {
            //窓情報を抽出
            //	<窓右><窓左>テキストを読み込む
            kenkyu02_util util = new kenkyu02_util(Log);
            List<MadoItem> madolst = util.getMadoList(strlst);

            StringBuilder sb = new StringBuilder();
            sb.Append(XMLUtil.XMLHEAD + "\r\n");
            sb.Append("<WEB和歌>\r\n");
            //窓一覧
            sb.Append(outMadoList(madolst));

            var text = createBunshoText(strlst);

            //本文 置き換え
            sb.Append(text
                .Replace("<上線 種類=点線>", "<上線>")
                .Replace("<上線 種類=波線>", "<上線>")
                .Replace("<上線 種類=破線>", "<上線>")
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

        void CheckXmlContent(string path)
        {
            XmlTextReader tr = new XmlTextReader(path);
            while (!tr.EOF)
            {
                int gyono = tr.LineNumber;
                try
                {
                    tr.Read();
                }
                catch (Exception ex)
                {
                    Log.err(path, gyono, "checkxml", "解析できません[" + tr.Value + "] " + ex.Message);
                    break;
                }
            }
            tr.Close();
        }

        /**

		*/
        public void run(string txtpath, string outpath)
        {
            try
            {
                Path = txtpath;

                //文字列をまずは置き換える
                //出力ファイルを切り替え .out
                //var strlst = FileUtil.getTextListFromPath(Path, Enc);

                //<改行>ごとに読み込む
                var util = new FileUtil(Log);
                var strlst = util.CreateKaigyoListFromPath(Path, Encoding.UTF8);

                //strlstに $"<文章>{cur}</文章>\r\n");  して出力
                FileUtil.writeTextToFile(createWakaXmlFromStrList(strlst), Encoding.UTF8, outpath);

                CheckXmlContent(outpath);
            }
            catch (Exception ex)
            {
                Log.err(Path, Gyono, "wakaxml", ex.Message);
            }
        }
    }
}
