using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kenkyu.lib.Model
{
	public class MadoItem
	{
		public int level {
			get { return IsRight == true ? 0 : 1; } }		//KJ_TODO:反対
		bool isleft_;
		public bool IsLeft { get { return isleft_==true; } set { isleft_ = value; } }
		public bool IsRight { get { return isleft_ != true; } set { isleft_ = value; } }
		public int href { get; set; }			//ジャンプ先
		public int Id { get; set; }

        public string text { get; set; }
		// public string text
		// {
		// 	get { return text_; }
		// 	set { this.SetProperty(ref this.text_, value); }
		// }
	}
}
