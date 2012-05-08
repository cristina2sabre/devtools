namespace CoApp.Toolkit.Text.Sgml {
    using System;
    using System.Text;

    internal abstract class Ucs4Decoder : Decoder {
        internal byte[] temp = new byte[4];
        internal int tempBytes;

        public override int GetCharCount(byte[] bytes, int index, int count) {
            return (count + tempBytes)/4;
        }

        internal abstract int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            var i = tempBytes;

            if(tempBytes > 0) {
                for(; i < 4; i++) {
                    temp[i] = bytes[byteIndex];
                    byteIndex++;
                    byteCount--;
                }
                i = 1;
                GetFullChars(temp, 0, 4, chars, charIndex);
                charIndex++;
            }
            else {
                i = 0;
            }
            i = GetFullChars(bytes, byteIndex, byteCount, chars, charIndex) + i;

            var j = (tempBytes + byteCount)%4;
            byteCount += byteIndex;
            byteIndex = byteCount - j;
            tempBytes = 0;

            if(byteIndex >= 0) {
                for(; byteIndex < byteCount; byteIndex++) {
                    temp[tempBytes] = bytes[byteIndex];
                    tempBytes++;
                }
            }
            return i;
        }

        internal static char UnicodeToUTF16(UInt32 code) {
            byte lowerByte, higherByte;
            lowerByte = (byte) (0xD7C0 + (code >> 10));
            higherByte = (byte) (0xDC00 | code & 0x3ff);
            return ((char) ((higherByte << 8) | lowerByte));
        }
    }
}