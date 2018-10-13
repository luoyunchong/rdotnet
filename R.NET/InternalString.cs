using RDotNet.Internals;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace RDotNet
{
    /// <summary>
    /// Internal string.
    /// </summary>
    [DebuggerDisplay("Content = {ToString()}; RObjectType = {Type}")]
    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    public class InternalString : SymbolicExpression
    {
        /// <summary>
        /// Convert string to utf8
        /// </summary>
        /// <param name="stringToConvert">string to convert</param>

        public static IntPtr NativeUtf8FromString(string stringToConvert)
        {
            int len = Encoding.UTF8.GetByteCount(stringToConvert);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(stringToConvert, 0, stringToConvert.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        /// <summary>
        /// Convert utf8 to string
        /// </summary>
        /// <param name="utf8">utf8 to convert</param>

        public static string StringFromNativeUtf8(IntPtr utf8)
        {
            int len = 0;
            while (Marshal.ReadByte(utf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(utf8, buffer, 0, buffer.Length);
            Encoding encoding = GetType(buffer);
            if (encoding.Equals(Encoding.UTF8))
            {
                string r = Encoding.UTF8.GetString(buffer);
                if (System.Text.RegularExpressions.Regex.IsMatch(r, @"[\u4e00-\u9fbb]+$"))
                {
                    return r;
                }
                else
                {
                    byte[] newBuffer = Encoding.Convert(Encoding.Default, Encoding.UTF8, buffer);
                    return Encoding.UTF8.GetString(newBuffer);
                }
            }
            else
            {
                byte[] newBuffer = Encoding.Convert(encoding, Encoding.UTF8, buffer);
                return Encoding.UTF8.GetString(newBuffer);
            }
        }

        /// <summary>
        /// 通过给定的文件流，判断文件的编码类型
        /// </summary>
        /// <param name="ss">字节流</param>
        /// <returns>文件的编码类型</returns>
        public static System.Text.Encoding GetType(byte[] ss)
        {
            byte[] unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] unicodeBig = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] utf8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
            sbyte[] tsbyte = (sbyte[])(Array)ss;
            Encoding reVal = Encoding.Default;
            if (InternalString.UTF8Probability(tsbyte) > 0 || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = Encoding.Unicode;
            }
            return reVal;

        }

        /// <summary>
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>

        /// <summary>
        /// 判断是UTF8编码的可能性
        /// </summary>
        /// <param name="rawtext">要判断的 <see cref="sbyte"/> 字节数组</param>
        /// <returns>返回 0 至 100 之间的可能性</returns>
        private static int UTF8Probability(sbyte[] rawtext)
        {
            int score = 0;
            int i, rawtextlen = 0;
            int goodbytes = 0, asciibytes = 0;

            // Maybe also use UTF8 Byte order Mark:  EF BB BF

            // Check to see if characters fit into acceptable ranges
            rawtextlen = rawtext.Length;
            for (i = 0; i < rawtextlen; i++)
            {
                if ((rawtext[i] & (sbyte)0x7F) == rawtext[i])
                {
                    // One byte
                    asciibytes++;
                    // Ignore ASCII, can throw off count
                }
                else if (-64 <= rawtext[i] && rawtext[i] <= -33 && i + 1 < rawtextlen && -128 <= rawtext[i + 1] && rawtext[i + 1] <= -65)
                {
                    goodbytes += 2;
                    i++;
                }
                else if (-32 <= rawtext[i] && rawtext[i] <= -17 && i + 2 < rawtextlen && -128 <= rawtext[i + 1] && rawtext[i + 1] <= -65 && -128 <= rawtext[i + 2] && rawtext[i + 2] <= -65)
                {
                    goodbytes += 3;
                    i += 2;
                }
            }

            if (asciibytes == rawtextlen)
            {
                return 0;
            }

            score = (int)(100 * ((float)goodbytes / (float)(rawtextlen - asciibytes)));

            // If not above 98, reduce to zero to prevent coincidental matches
            // Allows for some (few) bad formed sequences
            if (score > 98)
            {
                return score;
            }
            else if (score > 95 && goodbytes > 30)
            {
                return score;
            }
            else
            {
                return 0;
            }
        }


        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="engine">The <see cref="REngine"/> handling this instance.</param>
        /// <param name="pointer">The pointer to a string.</param>
        public InternalString(REngine engine, IntPtr pointer)
                : base(engine, pointer)
        { }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="engine">The <see cref="REngine"/> handling this instance.</param>
        /// <param name="s">The string</param>
        public InternalString(REngine engine, string s)
            : base(engine, engine.GetFunction<Rf_mkChar>()(s))
        { }

        /// <summary>
        /// Converts to the string into .NET Framework string.
        /// </summary>
        /// <param name="s">The R string.</param>
        /// <returns>The .NET Framework string.</returns>
        public static implicit operator string(InternalString s)
        {
            return s.ToString();
        }

        /// <summary>
        /// Gets the string representation of the string object.
        /// This returns <c>"NA"</c> if the value is <c>NA</c>, whereas <see cref="GetInternalValue()"/> returns <c>null</c>.
        /// </summary>
        /// <returns>The string representation.</returns>
        /// <seealso cref="GetInternalValue()"/>
        public override string ToString()
        {
            IntPtr pointer = IntPtr.Add(handle, Marshal.SizeOf(typeof(VECTOR_SEXPREC)));
            return StringFromNativeUtf8(pointer);
        }

        /// <summary>
        /// Gets the string representation of the string object.
        /// This returns <c>null</c> if the value is <c>NA</c>, whereas <see cref="ToString()"/> returns <c>"NA"</c>.
        /// </summary>
        /// <returns>The string representation.</returns>
        public string GetInternalValue()
        {
            if (handle == Engine.NaStringPointer)
            {
                return null;
            }
            IntPtr pointer = IntPtr.Add(handle, Marshal.SizeOf(typeof(VECTOR_SEXPREC)));
            return StringFromNativeUtf8(pointer);
        }
    }
}