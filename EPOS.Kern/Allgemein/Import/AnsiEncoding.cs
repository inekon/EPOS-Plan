using System;
using System.Text;

namespace WindowsFormsApplication1
{
    // ANSI-Encoding fuer die Importe (VDI 3805, PVsyst-PAN, CSV-Ganglinien): Die Dateien sind
    // ANSI/Windows-1252 kodiert. Encoding.Default ist dafuer ungeeignet - unter .NET Core/5+
    // ist das UTF-8, jedes Umlaut-Byte (z. B. 0xE4 fuer "ae") wird beim Dekodieren zu U+FFFD
    // und der Name landet dauerhaft beschaedigt in der Datenbank.
    //
    // Unter .NET 10 kennt das Framework die Codepage 1252 erst nach der Registrierung des
    // CodePagesEncodingProvider (Teil des Frameworks, kein Paketverweis). Sie geschieht EINMAL,
    // im statischen Konstruktor - so wirft GetEncoding(1252) keine Ausnahme mehr, die ein
    // Debugger mit "beim Ausloesen anhalten" oder das Ausnahmeprotokoll als Absturz zeigten.
    // 1252 ist fuer die deutschen Umlaute byteidentisch zu Latin-1 und kennt zusaetzlich das
    // Euro-Zeichen und die typografischen Anfuehrungszeichen (0x80-0x9F). Der Rueckfall auf
    // ISO-8859-1 (Latin-1, 28591) bleibt als Sicherheitsnetz.
    public static class AnsiEncoding
    {
        static AnsiEncoding()
        {
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); }
            catch (Exception) { /* Rueckfall Latin-1 in Get() */ }
        }

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
