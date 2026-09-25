using System;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>Das Format eines Bildes, das die Word-Engine in einen Bildplatzhalter setzt.</summary>
    public enum Bildformat
    {
        /// <summary>Portable Network Graphics.</summary>
        Png,

        /// <summary>JPEG (JFIF oder EXIF).</summary>
        Jpeg,
    }

    /// <summary>
    /// <b>Das Bild eines Bildplatzhalters</b> (Konzept Berichtsvorlagen 4.2, 6.5; Etappe BV-E2, Logo der
    /// Kopfzeile): die Bytes eines PNG oder JPEG samt Format und Maßen in Bildpunkten. Die Maße kommen
    /// aus dem Dateikopf — ohne das Bild zu dekodieren; die Engine passt es mit ihnen in den Rahmen
    /// des Platzhalterbildes ein. Die Ausrichtung eines EXIF-JPEG wird nicht ausgewertet.
    /// </summary>
    public sealed class Bildinhalt
    {
        private static readonly byte[] PngSignatur = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private Bildinhalt(byte[] daten, string dateiname, Bildformat format, int breite, int hoehe)
        {
            Daten = daten;
            Dateiname = dateiname;
            Format = format;
            Breite = breite;
            Hoehe = hoehe;
        }

        /// <summary>Die Bytes der Bilddatei.</summary>
        public byte[] Daten { get; }

        /// <summary>Der Dateiname ohne Pfad (Alternativtext des gefüllten Bildes); <c>null</c> ohne.</summary>
        public string Dateiname { get; }

        /// <summary>PNG oder JPEG.</summary>
        public Bildformat Format { get; }

        /// <summary>Die Breite in Bildpunkten.</summary>
        public int Breite { get; }

        /// <summary>Die Höhe in Bildpunkten.</summary>
        public int Hoehe { get; }

        /// <summary>Der Inhaltstyp des Bildteils: <c>image/png</c> oder <c>image/jpeg</c>.</summary>
        public string Inhaltstyp { get { return Format == Bildformat.Png ? "image/png" : "image/jpeg"; } }

        /// <summary>
        /// Das Bild aus den Bytes einer Datei — ein PNG oder JPEG mit lesbaren Maßen; sonst <c>null</c>.
        /// <paramref name="dateiname"/> darf ein Pfad sein, behalten wird der Name.
        /// </summary>
        public static Bildinhalt Aus(byte[] daten, string dateiname)
        {
            if (daten == null || daten.Length == 0) return null;
            string name = null;
            if (!string.IsNullOrWhiteSpace(dateiname))
            {
                try { name = Path.GetFileName(dateiname.Trim()); }
                catch (ArgumentException) { name = dateiname.Trim(); }
            }
            if (LiesPng(daten, out int b, out int h)) return new Bildinhalt(daten, name, Bildformat.Png, b, h);
            if (LiesJpeg(daten, out b, out h)) return new Bildinhalt(daten, name, Bildformat.Jpeg, b, h);
            return null;
        }

        /// <summary>Beginnt die Datei wie ein PNG oder JPEG (ohne die Maße zu lesen)?</summary>
        public static bool IstPngOderJpeg(byte[] daten)
        {
            if (daten == null) return false;
            if (daten.Length >= PngSignatur.Length && Beginnt(daten, PngSignatur)) return true;
            return daten.Length >= 3 && daten[0] == 0xFF && daten[1] == 0xD8 && daten[2] == 0xFF;
        }

        private static bool Beginnt(byte[] daten, byte[] kopf)
        {
            for (int i = 0; i < kopf.Length; i++)
                if (daten[i] != kopf[i]) return false;
            return true;
        }

        /// <summary>PNG: Signatur, dann der Abschnitt IHDR mit Breite und Höhe (je vier Bytes, Big Endian).</summary>
        private static bool LiesPng(byte[] b, out int breite, out int hoehe)
        {
            breite = hoehe = 0;
            if (b.Length < 24 || !Beginnt(b, PngSignatur)) return false;
            if (b[12] != (byte)'I' || b[13] != (byte)'H' || b[14] != (byte)'D' || b[15] != (byte)'R') return false;
            long w = ((long)b[16] << 24) | ((long)b[17] << 16) | ((long)b[18] << 8) | b[19];
            long h = ((long)b[20] << 24) | ((long)b[21] << 16) | ((long)b[22] << 8) | b[23];
            if (w <= 0 || h <= 0 || w > int.MaxValue || h > int.MaxValue) return false;
            breite = (int)w;
            hoehe = (int)h;
            return true;
        }

        /// <summary>
        /// JPEG: die Marken bis zum ersten Bildkopf (SOF0 bis SOF15 ohne DHT, JPG und DAC), dort Höhe
        /// und Breite (je zwei Bytes, Big Endian).
        /// </summary>
        private static bool LiesJpeg(byte[] b, out int breite, out int hoehe)
        {
            breite = hoehe = 0;
            if (b.Length < 4 || b[0] != 0xFF || b[1] != 0xD8) return false;
            int i = 2;
            while (i + 3 < b.Length)
            {
                if (b[i] != 0xFF) return false;
                byte marke = b[i + 1];
                if (marke == 0xFF) { i++; continue; }                                          // Füllbyte
                if (marke == 0xD8 || marke == 0x01 || (marke >= 0xD0 && marke <= 0xD7)) { i += 2; continue; }
                if (marke == 0xD9 || marke == 0xDA) return false;                               // Ende, Bilddaten
                int laenge = (b[i + 2] << 8) | b[i + 3];
                if (laenge < 2) return false;
                bool bildkopf = marke >= 0xC0 && marke <= 0xCF && marke != 0xC4 && marke != 0xC8 && marke != 0xCC;
                if (bildkopf)
                {
                    if (i + 8 >= b.Length) return false;
                    hoehe = (b[i + 5] << 8) | b[i + 6];
                    breite = (b[i + 7] << 8) | b[i + 8];
                    return breite > 0 && hoehe > 0;
                }
                i += 2 + laenge;
            }
            return false;
        }
    }
}
