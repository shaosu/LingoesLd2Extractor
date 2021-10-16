using BxNiom.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ld2Extractor
{
	public sealed class Helper
	{
		public static bool DEBUG = false;

		/// <summary>
		/// 4M
		/// </summary>
		public const int Buffer_Size = 1024 * 1024 * 4;

		internal static Decoder Charset_UTF8 = ASCIIEncoding.UTF8.GetDecoder();
		internal static Decoder Charset_UTF16LE = ASCIIEncoding.Unicode.GetDecoder();

		public const int MAX_Connections = 2;

		public const string SEP_Attribute = "‹";

		public const string SEP_Definition = "═";

		public const string SEP_List = "▫";

		public const string SEP_Newline = "\n";

		public const char SEP_Newline_Char = '\n';

		public const string SEP_Parts = "║";

		public const string SEP_Pinyin = "'";

		public const string SEP_Same_Meaning = "¶";

		public const string SEP_Space = " ";

		public const string SEP_Words = "│";

		public const string SEP_Etc = "…";

	}

}