using System;
using System.IO;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein ZIP-Archiv, dessen Zentralverzeichnis lügt</b> — der Prüfstand für den Größenschutz
    /// der Paketleser (N18, Gegenprüfung): Die entpackte Größe eines Eintrags steht im
    /// Zentralverzeichnis, und <b>niemand setzt sie beim Entpacken durch</b>. Ein Archiv darf also
    /// wenige Byte ausweisen und beliebig viel Nutzlast tragen; wer allein dem Verzeichnis glaubt,
    /// liest sie ganz in den Speicher.
    ///
    /// <para>Der Helfer schreibt die ausgewiesene Größe jedes Verzeichniseintrags um und lässt die
    /// Deflate-Nutzlast unberührt. Er geht dabei vom Ende des Zentralverzeichnisses aus
    /// (<c>PK\x05\x06</c>) über die Einträge (<c>PK\x01\x02</c>), nicht über eine Suche im ganzen
    /// Archiv — die Nutzlast selbst könnte dasselbe Muster tragen.</para>
    /// </summary>
    internal static class Archivluege
    {
        /// <summary>
        /// Setzt in jedem Verzeichniseintrag von <paramref name="zip"/> die ausgewiesene entpackte
        /// Größe auf <paramref name="ausgewiesen"/> Byte. Die Datei wird an ihrem Ort umgeschrieben.
        /// </summary>
        internal static void EntpackteGroesseFaelschen(string zip, uint ausgewiesen)
        {
            byte[] b = File.ReadAllBytes(zip);
            byte[] wert = BitConverter.GetBytes(ausgewiesen);

            // Das Ende des Zentralverzeichnisses steht hinten (die Proben tragen keinen Kommentar).
            int ende = -1;
            for (int i = b.Length - 22; i >= 0; i--)
                if (b[i] == 0x50 && b[i + 1] == 0x4B && b[i + 2] == 0x05 && b[i + 3] == 0x06) { ende = i; break; }
            Assert.True(ende >= 0, "Kein Ende des Zentralverzeichnisses im Archiv.");

            int stelle = BitConverter.ToInt32(b, ende + 16);
            int gefaelscht = 0;
            while (stelle + 46 <= ende)
            {
                Assert.True(b[stelle] == 0x50 && b[stelle + 1] == 0x4B && b[stelle + 2] == 0x01 && b[stelle + 3] == 0x02,
                            "Kein Verzeichniseintrag an der erwarteten Stelle.");
                Array.Copy(wert, 0, b, stelle + 24, 4);
                gefaelscht++;
                int name = BitConverter.ToUInt16(b, stelle + 28);
                int zusatz = BitConverter.ToUInt16(b, stelle + 30);
                int kommentar = BitConverter.ToUInt16(b, stelle + 32);
                stelle += 46 + name + zusatz + kommentar;
            }
            Assert.True(gefaelscht > 0, "Kein Eintrag im Zentralverzeichnis.");
            File.WriteAllBytes(zip, b);
        }
    }
}
