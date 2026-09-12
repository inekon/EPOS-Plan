using System;
using System.Collections.Generic;
using System.IO;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Was das Lesen einer einspaltigen Ganglinien-Textdatei ergeben hat.</summary>
    public sealed class GanglinienTextErgebnis
    {
        /// <summary>Stehen die Werte? Bei <c>false</c> sagt <see cref="Meldungen"/>, warum nicht.</summary>
        public bool Erfolgreich;

        /// <summary>
        /// Die Kopfzeile, wenn eine gelesen wurde — beim Solarganglinien-Katalog die
        /// Beschreibung des Satzes. Ohne Kopfzeilenschalter leer.
        /// </summary>
        public string Beschreibung = "";

        /// <summary>Die Werte, eine Zeile ein Wert, in Dateireihenfolge.</summary>
        public List<string> Werte = new List<string>();

        /// <summary>Was aufgefallen ist. Bei Erfolg leer.</summary>
        public List<PruefMeldung> Meldungen = new List<PruefMeldung>();
    }

    /// <summary>
    /// <b>Die einspaltige Ganglinien-Textdatei</b> (iU9-W13.0h) — ein Wert je Zeile,
    /// 8 760 Stunden im Jahr.
    ///
    /// <para><b>Warum es die Klasse gibt.</b> Bis Welle 13 las
    /// <c>ToolsClass.OpenText</c> diese Dateien — und ZEIGTE bei einem Formatfehler
    /// selbst eine Meldung (<c>ToolsClass.cs:34</c>). Ein Parser mit Dialog ist auf
    /// iOS nicht zu gebrauchen und in einem Test nicht zu pruefen. Hier steht
    /// dasselbe Lesen ohne Anzeige; was aufgefallen ist, steht als
    /// <see cref="PruefMeldung"/> im Ergebnis, und der Wirt entscheidet, ob daraus
    /// ein Warnbanner, eine <c>MessageBox</c> oder eine iOS-Blase wird — dieselbe
    /// Regel wie bei <see cref="GanglinienDatei"/> und
    /// <see cref="GanglinienImportAblauf"/>.</para>
    ///
    /// <para><b>Der Kopfzeilenschalter</b> ist der Grund, warum es EINE Klasse fuer
    /// ZWEI Aufrufer ist. Der externe Waermebedarf liest die Datei ohne Kopf: Jede
    /// Zeile ist ein Wert (<c>Form_AdminWaermeeinlesen:155-156</c>). Die
    /// Solarganglinie nimmt dieselbe Datei, aber ihre ERSTE Zeile ist die
    /// Beschreibung des Katalogsatzes (<c>Form_Solarganglinie_Admin:136-138</c>) —
    /// diese Maske kommt mit Welle 14b und soll dann nicht dieselbe Klasse ein
    /// zweites Mal brauchen (Risiko R-W14-8).</para>
    ///
    /// <para><b>Die Leerzeile ist behoben</b> (Befund W13-B11):
    /// <c>textFile[i].Substring(textFile[i].Length - 1, 1)</c> warf bei einer leeren
    /// Zeile eine <c>ArgumentOutOfRangeException</c> — mitten im Parser, ohne dass
    /// der Anwender erfuhr, welche Zeile es war. Eine Leerzeile ist jetzt eine
    /// Meldung mit Zeilennummer, wie das Trennzeichen am Zeilenende auch.</para>
    /// </summary>
    public static class GanglinienTextDatei
    {
        /// <summary>Die beiden Trennzeichen, deren Auftreten am Zeilenende die Datei verwirft.</summary>
        private static readonly char[] Trennzeichen = { ',', ';' };

        /// <summary>
        /// Liest die Datei.
        /// </summary>
        /// <param name="pfad">Vollstaendiger Pfad; leer ergibt einen Misserfolg ohne Wurf.</param>
        /// <param name="mitKopfzeile">
        /// <c>true</c>: Die erste Zeile ist die Beschreibung und zaehlt nicht als Wert.
        /// <c>false</c> (Waermebedarf): jede Zeile ist ein Wert.
        /// </param>
        public static GanglinienTextErgebnis Lies(string pfad, bool mitKopfzeile)
        {
            var erg = new GanglinienTextErgebnis();

            if (string.IsNullOrWhiteSpace(pfad))
            {
                erg.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, "IMP_TXT_KEIN_PFAD"));
                return erg;
            }

            string[] zeilen;
            try
            {
                zeilen = File.ReadAllLines(pfad);
            }
            catch (Exception ex)
            {
                erg.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, "IMP_TXT_LESEFEHLER", ex.Message));
                return erg;
            }

            // Die Formatpruefung des Vorlaeufers, woertlich: KEINE Zeile darf auf ein
            // Trennzeichen enden - die Datei muss zeilenorientiert sein. Neu ist nur,
            // dass die Meldung die ZEILE nennt und eine Leerzeile nicht mehr wirft.
            for (int i = 0; i < zeilen.Length; i++)
            {
                string zeile = zeilen[i];

                if (zeile.Length == 0)
                {
                    erg.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, "IMP_TXT_LEERZEILE",
                        Nummer(i + 1)));
                    return erg;
                }

                char letztes = zeile[zeile.Length - 1];
                if (letztes == Trennzeichen[0] || letztes == Trennzeichen[1])
                {
                    erg.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, "IMP_TXT_TRENNZEICHEN",
                        Nummer(i + 1), letztes.ToString()));
                    return erg;
                }
            }

            if (mitKopfzeile)
            {
                erg.Beschreibung = zeilen.Length > 0 ? zeilen[0] : "";
                for (int i = 1; i < zeilen.Length; i++) erg.Werte.Add(zeilen[i]);
            }
            else
            {
                erg.Werte.AddRange(zeilen);
            }

            erg.Erfolgreich = true;
            return erg;
        }

        private static string Nummer(int zeile)
        {
            return zeile.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
