using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wordxml.Models
{
    public class WordXmlCharStatus
    {
        public WordXmlCharStatus()
        {
            AttrList = new Dictionary<string, string>();
            TagList = new Dictionary<EnumAttrType, string>
            {
                {EnumAttrType.Underline, "上線"},
                {EnumAttrType.Bold, "太字"},
                {EnumAttrType.Subscript, "下付"},
                {EnumAttrType.Superscript, "上付"},
                {EnumAttrType.Kenten, "圏点"},
            };
        }

        public Dictionary<string, string> AttrList { get; set; }

        public enum EnumAttrType
        {
            Underline,
            Bold,
            Subscript,
            Superscript,
            Kenten
        }

        public EnumAttrType AttrType { get; set; }

        private Dictionary<EnumAttrType, string> TagList { get; set; }

        public static string GetText(List<WordXmlCharStatus> lst, bool isOpen)
        {
            var sb = new StringBuilder();
            if (isOpen != true) lst.Reverse();
            foreach (var s in lst)
            {
                sb.Append(s.GetText(isOpen));
            }
            return sb.ToString();
        }

        public string GetText(bool isOpen)
        {
            var sb = new StringBuilder();
            sb.Append("<");
            if (isOpen != true)
            {
                sb.Append("/");
            }
            sb.Append(TagList[AttrType]);
            if (isOpen)
            {
                foreach (var attr in AttrList)
                {
                    sb.Append($" {attr.Key}={attr.Value}");
                }
            }
            sb.Append(">");
            return sb.ToString();
        }
    }
}