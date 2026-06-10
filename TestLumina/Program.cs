using System;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace Test {
    public unsafe class Program {
        public static void Main() {
            var str = new Utf8String();
            byte[] bytes = new byte[] { 65, 66, 67, 0 };
            fixed (byte* ptr = bytes) {
                str.SetString(ptr);
            }
            str.SetString(bytes); // Will this compile?
            ReadOnlySpan<byte> span = bytes;
            str.SetString(span); // Will this compile?
        }
    }
}
