using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ergebnis der Uebernahme eines einzelnen VDI-Eintrags in die Stammdaten.
    /// Wird von allen VDI-Einlese-Dialogen gemeinsam benutzt, damit die
    /// Sammelmeldung des Mehrfachladens ueberall gleich gebildet wird.
    /// </summary>
    public enum VdiUebernahmeErgebnis
    {
        /// <summary>Datensatz wurde neu in die STAMM-Tabelle geschrieben.</summary>
        Gespeichert,

        /// <summary>Bezeichner war bereits vorhanden - Eintrag wurde uebersprungen.</summary>
        Duplikat,

        /// <summary>Uebernahme ist fehlgeschlagen (DB-Fehler o.ae.).</summary>
        Fehler,

        /// <summary>Vorhandener Katalogsatz wurde durch den Import aktualisiert (Konfliktdialog: Ueberschreiben).</summary>
        Ueberschrieben,

        /// <summary>Eintrag wurde unter neuem, vom Anwender vergebenem Namen angelegt.</summary>
        Umbenannt
    }

    /// <summary>
    /// Gemeinsame, UI-freie Hilfsfunktionen fuer die Auswahllisten der
    /// VDI-3805-Einlese-Dialoge (Waermepumpe, Solarkollektoren, Pufferspeicher,
    /// Heizkessel).
    ///
    /// Hintergrund (Anwenderanforderung 17.08.2026): alle VDI-Importe brauchen
    /// ein Suchfeld ueber die Auswahlliste und muessen mehrere Eintraege in einem
    /// Vorgang laden koennen. Die Filter- und Meldungslogik liegt hier zentral,
    /// damit alle vier Dialoge dasselbe Verhalten zeigen und die Logik ohne
    /// Formular geprueft werden kann.
    /// </summary>
    public static class VdiAuswahlFilter
    {
        /// <summary>
        /// Prueft, ob ein Listeneintrag zum Suchtext passt. Leerer Suchtext laesst
        /// alles durch, die Pruefung ist gross/klein-unabhaengig und trifft, wenn
        /// eines der uebergebenen Felder den Suchtext enthaelt.
        ///
        /// Mehrere durch Leerzeichen getrennte Begriffe wirken als UND-Verknuepfung
        /// (jeder Begriff muss in mindestens einem Feld vorkommen), damit z. B.
        /// "vitocal 200" auch dann trifft, wenn Firma und Bezeichner getrennt
        /// gefuehrt werden.
        /// </summary>
        /// <param name="suchtext">Eingabe des Anwenders aus dem Filterfeld.</param>
        /// <param name="felder">Die in der Liste sichtbaren Bezeichner (Name, Firma, ...).</param>
        public static bool Passt(string suchtext, params string[] felder)
        {
            if (string.IsNullOrWhiteSpace(suchtext)) return true;
            if (felder == null || felder.Length == 0) return false;

            string[] begriffe = suchtext.Trim().Split(new char[] { ' ', '\t' },
                                                      StringSplitOptions.RemoveEmptyEntries);

            for (int b = 0; b < begriffe.Length; b++)
            {
                bool gefunden = false;
                for (int f = 0; f < felder.Length; f++)
                {
                    string feld = felder[f];
                    if (string.IsNullOrEmpty(feld)) continue;
                    if (feld.IndexOf(begriffe[b], StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        gefunden = true;
                        break;
                    }
                }
                if (!gefunden) return false;
            }

            return true;
        }

        /// <summary>
        /// Baut die Statusrueckmeldung des Mehrfachladens. Die Meldung nennt immer
        /// "n von m"; uebersprungene (bereits eingelesene) und fehlgeschlagene
        /// Eintraege werden nur dann aufgefuehrt, wenn es sie gibt.
        /// </summary>
        /// <param name="gespeichert">Zahl der neu geschriebenen Datensaetze.</param>
        /// <param name="markiert">Zahl der markierten Eintraege (m).</param>
        /// <param name="uebersprungen">Zahl der bereits vorhandenen Bezeichner.</param>
        /// <param name="fehler">Zahl der fehlgeschlagenen Eintraege.</param>
        public static string LadeMeldung(int gespeichert, int markiert, int uebersprungen, int fehler)
        {
            return LadeMeldung(gespeichert, markiert, uebersprungen, fehler, 0, 0);
        }

        /// <summary>
        /// Baut die Statusrueckmeldung des Mehrfachladens einschliesslich der
        /// Ergebnisse des Konfliktdialogs (Paket D2). Ueberschriebene und unter
        /// neuem Namen angelegte Eintraege werden - wie uebersprungene und
        /// fehlgeschlagene - nur dann aufgefuehrt, wenn es sie gibt.
        /// </summary>
        /// <param name="gespeichert">Zahl der neu geschriebenen Datensaetze.</param>
        /// <param name="markiert">Zahl der markierten Eintraege (m).</param>
        /// <param name="uebersprungen">Zahl der bereits vorhandenen Bezeichner.</param>
        /// <param name="fehler">Zahl der fehlgeschlagenen Eintraege.</param>
        /// <param name="ueberschrieben">Zahl der ueberschriebenen Katalogsaetze.</param>
        /// <param name="umbenannt">Zahl der unter neuem Namen angelegten Eintraege.</param>
        public static string LadeMeldung(int gespeichert, int markiert, int uebersprungen, int fehler,
                                         int ueberschrieben, int umbenannt)
        {
            // Die fuenf Bausteine standen bis iU9-W13.0f HARTKODIERT DEUTSCH hier
            // im Kern (Befund W13-B19) - ein Verstoss gegen die Drei-Schichten-Regel
            // in der Schicht, die am wenigsten davon wissen darf. Jetzt kommen sie
            // aus MyResource; der WORTLAUT ist unveraendert, nur der Zeilenumbruch
            // ist aus dem Platzhalter {0} in die Verkettung gewandert.
            string meldung = string.Format(CultureInfo.CurrentCulture,
                                           MyResource.Resource.IMP_LADE_GELADEN,
                                           gespeichert, markiert);

            if (ueberschrieben > 0)
                meldung += Environment.NewLine + string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.IMP_LADE_UEBERSCHRIEBEN, ueberschrieben);

            if (umbenannt > 0)
                meldung += Environment.NewLine + string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.IMP_LADE_UMBENANNT, umbenannt);

            if (uebersprungen > 0)
                meldung += Environment.NewLine + string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.IMP_LADE_UEBERSPRUNGEN, uebersprungen);

            if (fehler > 0)
                meldung += Environment.NewLine + string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.IMP_LADE_FEHLGESCHLAGEN, fehler);

            return meldung;
        }

        /// <summary>
        /// Die Sammelmeldung aus einer <see cref="ImportBilanz"/> (iU9-W13.0b) —
        /// dieselben sechs Zahlen, nur nicht mehr einzeln durchgereicht.
        /// </summary>
        public static string LadeMeldung(ImportBilanz bilanz)
        {
            if (bilanz == null) return "";
            return LadeMeldung(bilanz.Gespeichert, bilanz.Markiert, bilanz.Duplikat,
                               bilanz.Fehler, bilanz.Ueberschrieben, bilanz.Umbenannt);
        }

        /// <summary>
        /// Bildet die Liste der markierten Zeilen auf die echten Indizes der
        /// Importliste ab. Zeilen ausserhalb der Zuordnung werden ignoriert -
        /// damit bleibt eine veraltete Markierung ohne Wirkung, statt den
        /// falschen Datensatz zu schreiben.
        /// </summary>
        /// <param name="markierteZeilen">Positionen in der (gefilterten) Liste.</param>
        /// <param name="anzeigeIndex">Zuordnung Listenposition -> Index in der Importliste.</param>
        public static List<int> QuellIndizes(System.Collections.IEnumerable markierteZeilen,
                                             List<int> anzeigeIndex)
        {
            List<int> treffer = new List<int>();
            if (markierteZeilen == null || anzeigeIndex == null) return treffer;

            foreach (object o in markierteZeilen)
            {
                if (!(o is int)) continue;
                int zeile = (int)o;
                if (zeile < 0 || zeile >= anzeigeIndex.Count) continue;
                if (!treffer.Contains(anzeigeIndex[zeile])) treffer.Add(anzeigeIndex[zeile]);
            }

            return treffer;
        }
    }
}
