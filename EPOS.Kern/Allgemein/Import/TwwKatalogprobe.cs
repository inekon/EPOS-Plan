using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Katalogprobe</b> (Anwenderentscheid ZU26, N23): fährt ein Katalogpaket der
    /// Brauchwasser-Nutzungsarten durch <c>TwwNutzungsartCtrl.PaketLesen</c> und
    /// <c>TwwNutzungsartCtrl.Importieren</c> und schreibt Zählung und Ausgänge als Protokollzeilen.
    ///
    /// <para><b>Wozu.</b> Der Katalog läuft auf iOS (ZU26), und sein Import nimmt dort ein
    /// ZIP-Archiv. Von Hand ist das eine Sitzung vor dem Gerät; dieser Probelauf macht daraus einen
    /// maschinellen Nachweis: Der Prüfmodus der iOS-Schale ruft ihn mit dem Probepaket aus dem
    /// App-Paket, die Tests unter Windows mit demselben Paket aus
    /// <c>EPOS.Kern.Tests/Proben/Zapfprofil/Katalogpaket/</c>.</para>
    ///
    /// <para><b>Plattformfrei und ohne Oberfläche.</b> Hier steht nur, was zu zählen ist; wo das
    /// Paket herkommt und wohin die Zeilen gehen, sagt der Aufrufer. Dieselbe Aufteilung wie bei
    /// <see cref="Importmessung"/>: Die Schale steuert den Weg zum Paket bei, gemessen wird im
    /// Kern.</para>
    ///
    /// <para><b>Zwei Läufe, in dieser Reihenfolge.</b> Erst der PRÜFLAUF (<c>pruefen = true</c>):
    /// derselbe Bericht, nichts geschrieben — er rollt zurück. Dann der echte IMPORT. Weil der
    /// Prüflauf nichts hinterlässt, müssen beide Zählungen gleich ausfallen; eine Abweichung ist
    /// selbst der Befund. Gezählt wird der Katalog vor und nach dem Import, damit die Zeilen auch
    /// sagen, dass wirklich geschrieben wurde.</para>
    ///
    /// <para><b>Die Probe wirft nicht.</b> Jede Ausnahme wird eine Zeile; der Rückgabewert ist die
    /// Zahl der Befunde (Ablehnungen, Abbrüche, Abweichungen) — 0 heißt „in Ordnung".</para>
    /// </summary>
    internal static class TwwKatalogprobe
    {
        /// <summary>Präfix aller Zeilen der Probe.</summary>
        public const string PROBE = "KATALOGPROBE";

        /// <summary>
        /// Fährt die Probe für das Paket unter <paramref name="paketpfad"/> (ZIP-Archiv, Ordner oder
        /// eine Datei des Paketordners) und schreibt ihre Zeilen über <paramref name="zeile"/>.
        /// </summary>
        /// <returns>Die Zahl der Befunde; 0 = in Ordnung.</returns>
        public static int Probelauf(string paketpfad, Action<string> zeile)
        {
            if (zeile == null) throw new ArgumentNullException(nameof(zeile));
            int befunde = 0;
            try
            {
                Schreiben(zeile, PROBE + " start paket=" + Wort(Dateiname(paketpfad)));

                IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(paketpfad, out ZapfSatz fehler);
                if (fehler != null)
                {
                    Schreiben(zeile, PROBE + " paket ergebnis=ABGELEHNT grund=" + Wort(fehler.Kennung));
                    return Ende(zeile, befunde + 1);
                }

                Schreiben(zeile, PROBE + " paket ergebnis=GELESEN dateien=" + Ganz(dateien.Count)
                                 + " namen=" + Wort(string.Join(",", dateien.Select(d => d.Name))));

                int vorher = Katalogzeilen();

                // Der PRUEFLAUF zuerst - er rollt zurueck und laesst den Katalog, wie er ist.
                Zaehlung pruefung = Lauf(zeile, dateien, true, ref befunde);

                // Dann der echte IMPORT: Weil der Prueflauf nichts hinterlassen hat, muss er
                // dieselben Zahlen ergeben.
                Zaehlung import = Lauf(zeile, dateien, false, ref befunde);

                int nachher = Katalogzeilen();
                Schreiben(zeile, PROBE + " katalog nutzungsarten_vorher=" + Ganz(vorher)
                                 + " nutzungsarten_nachher=" + Ganz(nachher)
                                 + " zuwachs=" + Ganz(nachher - vorher));

                if (!pruefung.Gleich(import))
                {
                    Schreiben(zeile, PROBE + " vergleich ergebnis=ABWEICHUNG pruefung=" + Wort(pruefung.Kurz())
                                     + " import=" + Wort(import.Kurz()));
                    befunde++;
                }
                else
                {
                    Schreiben(zeile, PROBE + " vergleich ergebnis=GLEICH");
                }
            }
            catch (Exception ex)
            {
                befunde++;
                Schreiben(zeile, PROBE + " abgebrochen ausnahme="
                                 + Wort(ex.GetType().Name + ": " + Einzeilig(ex.Message)));
            }
            return Ende(zeile, befunde);
        }

        // =============================================================================
        //  Ein Lauf
        // =============================================================================

        /// <summary>Die Zählung eines Laufs — sprachfrei, damit sie sich vergleichen lässt.</summary>
        private sealed class Zaehlung
        {
            internal int Nutzungsarten, Bedarfstage, Parameter;
            internal int Angelegt, Ersetzt, Uebersprungen, Abgelehnt, Hinweise;

            internal bool Gleich(Zaehlung a)
                => a != null && Nutzungsarten == a.Nutzungsarten && Bedarfstage == a.Bedarfstage
                   && Parameter == a.Parameter && Angelegt == a.Angelegt && Ersetzt == a.Ersetzt
                   && Uebersprungen == a.Uebersprungen && Abgelehnt == a.Abgelehnt;

            internal string Kurz()
                => "n" + Nutzungsarten + "/b" + Bedarfstage + "/p" + Parameter
                   + "/a" + Angelegt + "/e" + Ersetzt + "/u" + Uebersprungen + "/x" + Abgelehnt;
        }

        /// <summary>
        /// EIN Lauf über das gelesene Paket: die Zählzeile, je abgelehnter Eintrag eine Zeile mit
        /// seinem Grund und je Hinweis eine Zeile — alles sprachfrei über die Kennung des Satzes.
        /// </summary>
        private static Zaehlung Lauf(Action<string> zeile, IReadOnlyList<TwwPaketdatei> dateien,
                                     bool pruefen, ref int befunde)
        {
            string art = pruefen ? "pruefung" : "import";
            var z = new Zaehlung();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien, pruefen);

            if (b.Abbruch != null)
            {
                Schreiben(zeile, PROBE + " lauf=" + art + " ergebnis=ABBRUCH grund=" + Wort(b.Abbruch.Kennung));
                befunde++;
                return z;
            }

            z.Nutzungsarten = b.ZeilenVon(TwwImportbereich.Nutzungsart).Count;
            z.Bedarfstage = b.ZeilenVon(TwwImportbereich.Bedarfstag).Count;
            z.Parameter = b.ZeilenVon(TwwImportbereich.Parameter).Count;
            z.Angelegt = b.Angelegt;
            z.Ersetzt = b.Ersetzt;
            z.Uebersprungen = b.Uebersprungen;
            z.Abgelehnt = b.Abgelehnt;
            z.Hinweise = b.Hinweise.Count;

            Schreiben(zeile, PROBE + " lauf=" + art + " ergebnis=OK"
                             + " nutzungsarten=" + Ganz(z.Nutzungsarten)
                             + " bedarfstage=" + Ganz(z.Bedarfstage)
                             + " parameter=" + Ganz(z.Parameter)
                             + " angelegt=" + Ganz(z.Angelegt)
                             + " ersetzt=" + Ganz(z.Ersetzt)
                             + " uebersprungen=" + Ganz(z.Uebersprungen)
                             + " abgelehnt=" + Ganz(z.Abgelehnt)
                             + " hinweise=" + Ganz(z.Hinweise));

            foreach (TwwImportzeile r in b.Zeilen.Where(r => r.Ausgang == TwwImportausgang.Abgelehnt))
            {
                Schreiben(zeile, PROBE + " lauf=" + art + " abgelehnt bereich="
                                 + r.Bereich.ToString().ToUpperInvariant()
                                 + " eintrag=" + Wort(r.Nutzungsart)
                                 + " grund=" + Wort(r.Grund == null ? "" : r.Grund.Kennung));
                befunde++;
            }

            foreach (ZapfSatz h in b.Hinweise)
                Schreiben(zeile, PROBE + " lauf=" + art + " hinweis=" + Wort(h.Kennung));

            return z;
        }

        // =============================================================================
        //  Handreichungen
        // =============================================================================

        /// <summary>Die Zahl der Nutzungsarten im Katalog; <c>-1</c>, wenn sie nicht zu lesen ist.</summary>
        private static int Katalogzeilen()
        {
            try { return TwwNutzungsartCtrl.Katalogfilterzeilen().Count; }
            catch { return -1; }
        }

        private static int Ende(Action<string> zeile, int befunde)
        {
            Schreiben(zeile, PROBE + " ende ergebnis=" + (befunde == 0 ? "OK" : "BEFUNDE")
                             + " befunde=" + Ganz(befunde));
            return befunde;
        }

        private static void Schreiben(Action<string> zeile, string text)
        {
            try { zeile(text); } catch { }
        }

        /// <summary>Ein Wert in Anführungszeichen, einzeilig und ohne eigene Anführungszeichen.</summary>
        private static string Wort(string wert)
            => "\"" + Einzeilig(wert ?? "").Replace('"', '\'') + "\"";

        private static string Ganz(int wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static string Einzeilig(string text)
            => (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();

        /// <summary>Der Dateiname ohne Pfad — ein Pfad der Sandbox sagt im Protokoll nichts.</summary>
        private static string Dateiname(string pfad)
        {
            try { return System.IO.Path.GetFileName(pfad ?? ""); }
            catch { return ""; }
        }
    }
}
