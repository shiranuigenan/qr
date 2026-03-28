using System.Collections;

namespace QRCoder;

public partial class QRCodeGenerator
{
    internal static class AlphanumericEncoder
    {
        internal static readonly byte[] _map =
        [
            // 0..31
            255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255,
            // 32..47  (space, ! " # $ % & ' ( ) * + , - . /)
            36, 255, 255, 255, 37, 38, 255, 255, 255, 255, 39, 40, 255, 41, 42, 43,
            // 48..57  (0..9)
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            // 58..64  (: ; < = > ? @)
            44, 255, 255, 255, 255, 255, 255,
            // 65..90  (A..Z)
            10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35
            // (we don't index > 90)
        ];
        public static bool CanEncode(char c) => c <= 90 && _map[c] != 255;
        public static int GetBitLength(int textLength)
        {
            return (textLength / 2) * 11 + (textLength & 1) * 6;
        }
        public static int WriteToBitArray(string plainText, int index, int count, BitArray codeText, int codeIndex)
        {
            // Process each pair of characters.
            while (count >= 2)
            {
                // Convert each pair of characters to a number by looking them up in the alphanumeric dictionary and calculating.
                var dec = _map[plainText[index++]] * 45 + _map[plainText[index++]];
                // Convert the number to binary and store it in the BitArray.
                codeIndex = DecToBin(dec, 11, codeText, codeIndex);
                count -= 2;
            }

            // Handle the last character if the length is odd.
            if (count > 0)
            {
                codeIndex = DecToBin(_map[plainText[index]], 6, codeText, codeIndex);
            }

            return codeIndex;
        }
    }
}
