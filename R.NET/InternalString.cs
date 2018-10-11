using RDotNet.Internals;
using System;
using System.Diagnostics;
using System.IO;
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
            byte[] newBuffer = Encoding.Convert(GetType(buffer), Encoding.UTF8, buffer);
            return Encoding.UTF8.GetString(newBuffer);
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
            Encoding reVal = Encoding.Default;
            if (IsUtf8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
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
        private static bool IsUtf8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数
            foreach (var t in data)
            {
                var curByte = t; //当前分析的字节.
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                return false;
            }
            return true;
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