namespace Farayan
{
	using log4net;
	using NodaTime;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Configuration;
	using System.Data;
	using System.Data.SqlTypes;
	using System.Drawing;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.Remoting.Messaging;
	using System.Security.Cryptography;
	using System.Security.Principal;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Web;
	public static class FarayanUtility
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(FarayanUtility));



		/// <summary>
		/// ensures text starts with specified character <paramref name="suffix"/>
		/// </summary>
		/// <param name="value">text that will be evaluated. </param>
		/// <param name="suffix">text that must be at the start of <paramref name="value"/>.</param>
		/// <param name="onlyWhenUsable">if was true, ensures text starts with <paramref name="suffix"/> only when <paramref name="value"/> is usable (not empty). If was false and <paramref name="value"/> was null or empty, returns <paramref name="suffix"/> as result</param>
		/// <returns>text that starts with <paramref name="suffix"/> or empty string if <paramref name="value"/> was null or empty and <paramref name="onlyWhenUsable"/> was true</returns>
		public static string EnsureStartsWith(this string value, char suffix, bool onlyWhenUsable = false) {
			return value.IsUsable() || onlyWhenUsable == false ? suffix + value.Or().TrimStart(suffix) : "";
		}

		/// <summary>
		/// ensures text ends with specified character <paramref name="suffix"/>
		/// </summary>
		/// <param name="value">text that will be evaluated. </param>
		/// <param name="suffix">text that must be at the end of <paramref name="value"/>.</param>
		/// <param name="onlyWhenUsable">if was true, ensures text ends with <paramref name="suffix"/> only when <paramref name="value"/> is usable (not empty). If was false and <paramref name="value"/> was null or empty, returns <paramref name="suffix"/> as result</param>
		/// <returns>text that ends with <paramref name="suffix"/> or empty string if <paramref name="value"/> was null or empty and <paramref name="onlyWhenUsable"/> was true</returns>
		public static string EnsureEndsWith(this string value, char suffix, bool onlyWhenUsable = false) {
			return value.IsUsable() || onlyWhenUsable == false ? value.Or().TrimEnd(suffix) + suffix : "";
		}

		/// <summary>
		/// ensures text starts with specified character <paramref name="suffix"/>
		/// </summary>
		/// <param name="value">text that will be evaluated. </param>
		/// <param name="suffix">text that must be at the start of <paramref name="value"/>.</param>
		/// <param name="onlyWhenUsable">if was true, ensures text starts with <paramref name="suffix"/> only when <paramref name="value"/> is usable (not empty). If was false and <paramref name="value"/> was null or empty, returns <paramref name="suffix"/> as result</param>
		/// <returns>text that starts with <paramref name="suffix"/> or empty string if <paramref name="value"/> was null or empty and <paramref name="onlyWhenUsable"/> was true</returns>
		public static string EnsureStartsWith(this string value, string prefix, bool onlyWhenUsable = false) {
			if (prefix.IsNullOrEmpty(false))
				return value;
			return value.IsUsable() || onlyWhenUsable == false ? prefix + value.Or().TrimStart(prefix) : "";
		}

		public static bool IsUsable(this string value, bool autoTrim = true) {
			if (string.IsNullOrEmpty(value))
				return false;
			if (autoTrim && string.IsNullOrEmpty(value.Trim()))
				return false;

			return true;
		}

		public static bool IsNullOrEmpty(this string value, bool autoTrim = true) {
			if (string.IsNullOrEmpty(value))
				return true;
			if (autoTrim && string.IsNullOrEmpty(value.Trim()))
				return true;

			return false;
		}
		public static string Or(this string value, string defaultValue = "", bool autoTrim = true) {
			return value.IsUsable(autoTrim) ? value : defaultValue;
		}

		public static string TrimStart(this string value, string c) {
			if (c.IsNullOrEmpty(false))
				return value;
			if (value.IsNullOrEmpty(false))
				return "";
			if (value.IndexOf(c) == 0)
				return value.Substring(c.Length);
			return value;
		}

		public static string TrimEnd(this string value, string c) {
			if (c.IsNullOrEmpty(false))
				return value;
			if (value.IsNullOrEmpty(false))
				return "";
			if (value.LastIndexOf(c) < 0)
				return value;
			if (value.LastIndexOf(c) + c.Length == value.Length)
				return value.Substring(0, value.LastIndexOf(c));
			return value;
		}

		public static string PrintException(Exception exception) {
			string str = string.Empty + "Exception Details:\r\n";
			for (string str2 = "\t"; exception != null; str2 = str2 + "\t") {
				object obj3 = str;
				string str4 = string.Concat(new object[] { obj3, str2, "Type: \t\t", exception.GetType(), "\r\n" });
				str4 = str4 + str2 + "Message: \t" + exception.Message + "\r\n";
				str4 = str4 + str2 + "Source: \t\t" + exception.Source + "\r\n";
				if (exception.StackTrace != null)
					obj3 = str4 + str2 + "StackTrace: \t\r\n" + str2 + exception.StackTrace.Replace("\r\n", "\n").Replace("\n", "\r\n" + str2) + "\r\n";
				str = string.Concat(new object[] { obj3, str2, "TargetSite: ", exception.TargetSite, "\r\n" }) + str2 + "Data: \t\r\n";
				foreach (object obj2 in exception.Data.Keys) {
					obj3 = str;
					str = string.Concat(new object[] { obj3, str2, obj2, "\t=\t", exception.Data[obj2], "\r\n" });
				}
				str = str + (((str2 + exception.InnerException) == null) ? "" : "--------------------------------------------\r\n");
				exception = exception.InnerException;
			}
			return str;
		}

		public static int? TryParseInt(string value) {
			if (value.IsNullOrEmpty())
				return null;
			value = Regex.Replace(value, @"[^\d\.\-]", "");
			int num;
			if (int.TryParse(value, out num)) {
				return new int?(num);
			}
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="val"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static T EnsureBetween<T>(this T val, T min, T max)
			where T : IComparable<T> {
			if (val.CompareTo(min) < 0)
				return min;
			if (val.CompareTo(max) > 0)
				return max;
			return val;
		}

		public static string Join(this IEnumerable<string> data, string separator) {
			return string.Join(separator, data.ToArray());
		}

		public static string[] SplitToNonEmptyParts(this string value, params char[] separators) {
			return value.Or().Split(separators, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string[] SplitToNonEmptyParts(this string value, string separators) {
			return value.Or().Split(separators.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		}
	}
}