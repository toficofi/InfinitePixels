using UnityEngine;
using System.Collections;
using System.Text;

public class Util
{
    public static string ConvertToASCII(string unicodeString)
    {
        // Create two different encodings.
        Encoding ascii = Encoding.ASCII;
        Encoding unicode = Encoding.Unicode;

        // Convert the string into a byte array.
        byte[] unicodeBytes = unicode.GetBytes(unicodeString);

        // Perform the conversion from one encoding to the other.
        byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);

        // Convert the new byte[] into a char[] and then into a string.
        char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
        ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
        string asciiString = new string(asciiChars);

        return asciiString;

    }
}
