using System.Collections.Generic;
using journal.console.lib.Consoles;
using journal.console.lib.Models;
using kj.kihon;
using NUnit.Framework;

namespace journal.Test.Tests
{
    [TestFixture]
    public class journal06Test
    {
        [Test]
        public void rubyタグ内にタグあり()
        {
            var util = new journal06_util(new ErrorLogger());
            var paralst = new List<ParaItem>();
            paralst.Add(new ParaItem
            {
                Text = "三条<ruby><大字>実</大字>実<rt>さね</rt></ruby>年"
            });
            Assert.AreEqual("<字下 0><問答 0>三条<ruby><大字>実</大字>実<rt>さね</rt></ruby>年\r\n"
                ,util.CreateTextFromParaList(paralst));
        }
        public void rubyタグ()
        {
            var util = new journal06_util(new ErrorLogger());
            var paralst = new List<ParaItem>();
            paralst.Add(new ParaItem
            {
                Text = "公家・三条<ruby>実<rt>さね</rt></ruby><ruby>万<rt>つむ</rt></ruby>（享和二〈一八〇二〉年"
            });
            Assert.AreEqual("<字下 0><問答 0>公家・三条<ruby>実<rt>さね</rt></ruby><ruby>万<rt>つむ</rt></ruby>（享和二〈一八〇二〉年\r\n"
                ,util.CreateTextFromParaList(paralst));
        }
        [Test]
        public void 下付上付が連続する場合は割注()
        {
            var util = new journal06_util(new ErrorLogger());
            var paralst = new List<ParaItem>();
            paralst.Add(new ParaItem
            {
                Text = "流罪<下付>一</下付><上付>テ</上付>配所"
            });
            Assert.AreEqual("<字下 0><問答 0>流罪<割注>テ<項段>一</割注>配所\r\n"
                ,util.CreateTextFromParaList(paralst));
        }
        [Test]
        public void 添タグ()
        {
            var util = new journal06_util(new ErrorLogger());
            var paralst = new List<ParaItem>();
            paralst.Add(new ParaItem
            {
                Text = "<添>〇<GR>＼</添>秋くれば"
            });
            Assert.AreEqual("<字下 0><問答 0><ruby>〇<rt>＼</rt></ruby>秋くれば\r\n",util.CreateTextFromParaList(paralst));
        }
        [Test]
        public void LTとRTタグは圏点とRTに変換()
        {
            var util = new journal06_util(new ErrorLogger());
            var paralst = new List<ParaItem>();
            paralst.Add(new ParaItem
            {
                Text = "初かりなきて<ruby>を<rt>お</rt><lt>〻</lt></ruby>もしろし"
            });
            Assert.AreEqual("<字下 0><問答 0>初かりなきて<圏点 位置=左 種類=\"〻\"><ruby>を<rt>お</rt></ruby></圏点>もしろし\r\n",util.CreateTextFromParaList(paralst));
        }
        [Test]
        public void LTタグ()
        {
            var util = new journal06_util(new ErrorLogger());
            var paralst = new List<ParaItem>();
            paralst.Add(new ParaItem
            {
                Text = "初かりなきて<ruby>を<lt>〻</lt></ruby>もしろし"
            });
            Assert.AreEqual("<字下 0><問答 0>初かりなきて<ruby>を<lt>〻</lt></ruby>もしろし\r\n",util.CreateTextFromParaList(paralst));
        }
    }
}