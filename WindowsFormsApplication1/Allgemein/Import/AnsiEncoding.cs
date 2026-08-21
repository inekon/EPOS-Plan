using System;
using System.Text;

namespace WindowsFormsApplication1
{
    // ANSI-Encoding fuer Herstellerdaten-Importe (VDI 3805, PVsyst-PAN): Die Dateien sind
    // ANSI/Windows-1252 kodiert. Encoding.Default ist dafuer ungeeignet - unter .NET Core/5+
    // ist das UTF-8, jedes Umlaut-Byte (z. B. 0xE4 fuer "ae") wird beim Dekodieren zu U+FFFD
    // und der Name landet dauerhaft beschaedigt in der Datenbank.
    // Robust ueber beide Runtimes:
    //  - .NET Framework: Windows-1252 (1252) ist direkt verfuegbar.
    //  - .NET Core/5+: 1252 ist ohne CodePagesEncodingProvider NICHT verfuegbar
    //    (NotSupportedException). Dann ISO-8859-1 (Latin-1, 28591) verwenden - nativ
    //    verfuegbar und fuer deutsche Umlaute (ä ö ü Ä Ö Ü ß) identisch mit 1252.
    public static class AnsiEncoding
    {
        public static Encoding Get()
        {
            try
            {
                return Encoding.GetEncoding(1252);
            }
            catch (NotSupportedException)
            {
                return Encoding.GetEncoding(28591);
            }
        }
    }
}
