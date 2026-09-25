using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NReco.Csv;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Leser eines Normformvektorpakets</b> (Umsetzungskonzept Zapfprofilgenerator 2.5,
    /// 4.2, Kapitel 6; Stufe Z4b) — er liest die Typtage, die der <b>lizenzierte Anwender</b>
    /// selbst einspielt, aus einem <see cref="Stream"/> (Muster
    /// <see cref="TryPaketLeser.AusStrom"/>). Das Repositorium, die Auslieferung, die
    /// Testdatenbank und die CI tragen keinen einzigen dieser Werte.
    ///
    /// <para><b>Das Paketformat</b> ist ein ZIP-Archiv (oder ein Ordner, den die Hülle zu
    /// <see cref="TwwPaketdatei"/> liest) mit sechs Dateien, davon eine wahlfrei. Jede Datei ist
    /// UTF-8 (BOM erlaubt), hat eine Kopfzeile mit den Spaltennamen, benutzt <c>;</c> oder
    /// <c>,</c> als Trenner, folgt RFC 4180 und schreibt Zahlen in invarianter Kultur mit Punkt
    /// (auch Exponentialschreibweise). Ein leeres Feld ist „fehlt". Die Reihenfolge der Zeilen
    /// ist frei, die der Spalten auch; unbekannte Spalten sind ein Fehler, keine stille Annahme.</para>
    ///
    /// <list type="table">
    /// <item><term><c>typtage.csv</c></term><description><c>code;jahreszeit;tagart;bewoelkung</c>
    /// — die Typtagkategorien. <c>jahreszeit</c> ist <c>uebergang</c>, <c>sommer</c> oder
    /// <c>winter</c>, <c>tagart</c> ist <c>werktag</c> oder <c>sonntag</c>, <c>bewoelkung</c> ist
    /// <c>heiter</c>, <c>bewoelkt</c> oder <c>ohne</c> (keine Unterscheidung).</description></item>
    /// <item><term><c>klimazonen.csv</c></term><description><c>zone;bezeichnung</c> — die
    /// Klimazonen; <c>zone</c> ganzzahlig ≥ 1, <c>bezeichnung</c> wahlfrei und nur für den
    /// Bericht.</description></item>
    /// <item><term><c>typtage_je_zone.csv</c></term><description><c>zone;gebaeudeart;typtag;anzahl</c>
    /// — die Zahl der Kalendertage je Kategorie; <c>anzahl</c> ganzzahlig ≥ 0, Summe je Zone und
    /// Gebäudeart genau 365.</description></item>
    /// <item><term><c>f_twe_tt.csv</c></term><description><c>gebaeudeart;zone;typtag;faktor</c> —
    /// der Faktor der Tagesenergie. Er ist eine Schwankung um den Jahresmittelwert und
    /// <b>darf negativ sein</b>; geprüft wird nur, dass er endlich ist. Positiv bleibt die
    /// Tagesmenge selbst (Klemmung, Konzept 4.2).</description></item>
    /// <item><term><c>kennwerte.csv</c></term><description><c>schluessel;wert;text</c> — die
    /// Grenzen des Verfahrens als Zahl (<c>wert</c>) oder als Text (<c>text</c>). Pflicht:
    /// <c>quelle</c> (Text), <c>wintergrenze</c> und je Gebäudeart
    /// <c>heizgrenze.&lt;gebaeudeart&gt;</c>; <c>bewoelkung.schwelle</c>, sobald eine Kategorie
    /// nach Bewölkung unterscheidet. Wahlfrei: <c>ausgabe</c> (Text) und
    /// <c>pruefsumme.toleranz</c>.</description></item>
    /// <item><term><c>tagesgaenge.csv</c> (wahlfrei)</term><description><c>gebaeudeart;typtag;aufloesung_min;index;anteil</c>
    /// — der normierte Tagesgang je Kategorie: <c>aufloesung_min</c> teilt 1440 ohne Rest und ist
    /// zugleich ein Teiler oder ein Vielfaches von 60 (<see cref="AufloesungTauglich"/>),
    /// <c>index</c> läuft von 0 bis 1440/<c>aufloesung_min</c> − 1 ohne Lücke, jeder
    /// <c>anteil</c> ist ≥ 0, ihre Summe 1 (Toleranz <see cref="SUMME_TOLERANZ"/>). Ohne diese
    /// Datei trägt der Tagesgangsatz der Zone die Tagesform.</description></item>
    /// </list>
    ///
    /// <para><b>Zwei Stufen.</b> Taugt das Paket seiner Form nach nicht — eine Datei fehlt, eine
    /// Spalte ist unbekannt oder fehlt, ein Feld ist keine Zahl, ein Schlüssel doppelt, eine
    /// Tagessumme nicht 365, eine Zone oder Kategorie unvollständig —, ist es <b>als Ganzes
    /// abgelehnt</b>: Der Leser gibt <c>null</c> und einen <see cref="ZapfSatz"/> zurück, der
    /// Datei und Zeile nennt. Was die Rechnung nicht entscheidet — eine fehlende Prüfsumme, eine
    /// Klimazone ohne Werte, ein Tagesgang ohne Kategorie —, kommt als benannter Hinweis mit.</para>
    ///
    /// <para><b>Rein.</b> Der Leser kennt keine Datenbank und keinen Dienst; die Datei wählt die
    /// Hülle über <c>Dienste.Datei</c> und übergibt einen Strom (Konzept 2.5). Geschrieben wird
    /// nichts — das tut <c>TwwTyptagCtrl</c>.</para>
    /// </summary>
    internal static class Normformvektorleser
    {
        /// <summary>Die Datei der Typtagkategorien.</summary>
        internal const string DATEI_TYPTAGE = "typtage.csv";

        /// <summary>Die Datei der Klimazonen.</summary>
        internal const string DATEI_KLIMAZONEN = "klimazonen.csv";

        /// <summary>Die Datei der Kalendertage je Kategorie.</summary>
        internal const string DATEI_ANZAHL = "typtage_je_zone.csv";

        /// <summary>Die Datei der Faktoren der Tagesenergie.</summary>
        internal const string DATEI_FAKTOREN = "f_twe_tt.csv";

        /// <summary>Die Datei der Kennwerte des Verfahrens.</summary>
        internal const string DATEI_KENNWERTE = "kennwerte.csv";

        /// <summary>Die wahlfreie Datei der normierten Tagesgänge.</summary>
        internal const string DATEI_GAENGE = "tagesgaenge.csv";

        /// <summary>Die Pflichtdateien des Pakets in Lesereihenfolge.</summary>
        internal static readonly IReadOnlyList<string> PFLICHTDATEIEN = new[]
        {
            DATEI_TYPTAGE, DATEI_KLIMAZONEN, DATEI_ANZAHL, DATEI_FAKTOREN, DATEI_KENNWERTE
        };

        /// <summary>Die Tage des Rechenjahres, auf die sich die Kalendertage je Zone summieren.</summary>
        internal const int TAGE_JE_JAHR = Zapfkalender.TAGE;

        /// <summary>Toleranz der Summenproben (Tagesgang Σ = 1) [-].</summary>
        internal const double SUMME_TOLERANZ = 1e-6;

        /// <summary>Die Minuten eines Tages — <c>aufloesung_min</c> muss sie ohne Rest teilen.</summary>
        internal const int MINUTEN_JE_TAG = 1440;

        /// <summary>
        /// <b>Höchstzahl der Einträge eines Pakets</b> (numerische Setzung): Das Format kennt sechs
        /// Dateien; 200 Einträge lassen Ordner, Beilagen und Schreibweisen zu und fangen ein Archiv
        /// ab, das nicht dieses Paket ist. Geprüft wird das Zentralverzeichnis, bevor ein Byte
        /// entpackt wird.
        /// </summary>
        internal const int HOECHSTENS_EINTRAEGE = 200;

        /// <summary>
        /// <b>Höchste entpackte Gesamtgröße eines Pakets [Byte]</b> (numerische Setzung, 64 MB): Die
        /// sechs Dateien tragen Text — 45 Faktorblöcke und Tagesgänge im Minutenraster bleiben weit
        /// darunter. Die Grenze fängt das aufgeblähte Archiv ab, ohne es zu entpacken — und ein
        /// zweites Mal beim Lesen, denn das Zentralverzeichnis ist nur eine Behauptung der Datei.
        /// </summary>
        internal const long HOECHSTENS_BYTE_ENTPACKT = 64L * 1024 * 1024;

        // =================================================================================
        //  Einlesen
        // =================================================================================

        /// <summary>
        /// <b>Liest ein Paket aus einem Strom</b> (ein ZIP-Archiv; der Strom muss suchbar sein
        /// oder wird in den Speicher gelesen). Ergebnis ist der Satz oder <c>null</c> samt
        /// benanntem <paramref name="fehler"/>; <paramref name="hinweise"/> nimmt auf, was die
        /// Rechnung nicht entscheidet.
        ///
        /// <para><b>Die Gesamtgröße gilt zweimal</b> (Muster
        /// <c>Allgemein/Import/Ifc/IfcLeser.Entpacken</c>): erst aus dem Zentralverzeichnis, bevor
        /// ein Byte entpackt wird, dann <b>beim Lesen</b> — je Eintrag bis zum verbleibenden Rest und
        /// ein Byte darüber, denn das Verzeichnis ist nur eine Behauptung der Datei. Beide Male ist
        /// die Ablehnung <c>NORMVEKTOR_PAKET_ZU_GROSS</c>. Die zweite Wand ist <b>Vorsorge</b>:
        /// <see cref="ZipArchiveEntry.Open"/> begrenzt den Entpackstrom heute selbst auf die
        /// ausgewiesene Größe, ein zu klein ausgewiesener Eintrag kommt also <b>gekürzt</b> herein
        /// statt zu groß — und fällt dann der Formprüfung zu (gemessen in
        /// <c>EPOS.Kern.Tests/NormformvektorleserTests</c>). Der Leser verlässt sich nicht darauf.
        /// <paramref name="grenzeGesamt"/> ist ein Parameter mit
        /// <see cref="HOECHSTENS_BYTE_ENTPACKT"/> als Vorgabe, damit ein Test das Greifen der Prüfung
        /// an einem kleinen Archiv messen kann statt an 64 MB.</para>
        /// </summary>
        internal static Normformvektorsatz AusStrom(Stream strom, out ZapfSatz fehler,
                                                    ICollection<ZapfSatz> hinweise = null,
                                                    long grenzeGesamt = HOECHSTENS_BYTE_ENTPACKT)
        {
            fehler = null;
            if (strom == null)
            {
                fehler = ZapfSatz.Neu("NORMVEKTOR_PAKET_UNLESBAR", "kein Strom");
                return null;
            }
            List<TwwPaketdatei> dateien;
            try
            {
                Stream lesbar = strom;
                MemoryStream kopie = null;
                if (!strom.CanSeek)
                {
                    kopie = new MemoryStream();
                    strom.CopyTo(kopie);
                    kopie.Position = 0;
                    lesbar = kopie;
                }
                using (kopie)
                using (var zip = new ZipArchive(lesbar, ZipArchiveMode.Read, leaveOpen: true))
                {
                    // Die Mengengrenze VOR dem Entpacken, allein aus dem Zentralverzeichnis (wie im
                    // TRY-Paketleser kein Byte eines Eintrags): Eintragszahl und entpackte Groesse.
                    // Die Summe bricht AN der Grenze ab, damit sie an erfundenen Laengen nicht
                    // ueberlaeuft.
                    long entpackt = 0;
                    foreach (ZipArchiveEntry e in zip.Entries)
                    {
                        long l = Math.Max(0L, e.Length);
                        if (l > grenzeGesamt - entpackt) { entpackt = grenzeGesamt + 1; break; }
                        entpackt += l;
                    }
                    if (zip.Entries.Count > HOECHSTENS_EINTRAEGE || entpackt > grenzeGesamt)
                    {
                        fehler = ZapfSatz.Neu("NORMVEKTOR_PAKET_ZU_GROSS", zip.Entries.Count, HOECHSTENS_EINTRAEGE,
                                              entpackt, grenzeGesamt);
                        return null;
                    }
                    dateien = new List<TwwPaketdatei>();
                    long gesamt = 0;
                    foreach (ZipArchiveEntry e in zip.Entries.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(Path.GetExtension(e.Name), ".csv", StringComparison.OrdinalIgnoreCase)) continue;
                        // Und nun dieselbe Grenze BEIM Lesen: Das Verzeichnis kann gelogen haben.
                        string inhalt = EintragLesen(e, grenzeGesamt - gesamt, out long gelesen);
                        if (inhalt == null)
                        {
                            fehler = ZapfSatz.Neu("NORMVEKTOR_PAKET_ZU_GROSS", zip.Entries.Count, HOECHSTENS_EINTRAEGE,
                                                  gesamt + gelesen, grenzeGesamt);
                            return null;
                        }
                        gesamt += gelesen;
                        dateien.Add(new TwwPaketdatei(e.Name, inhalt));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is NotSupportedException
                                       || ex is ObjectDisposedException || ex is ArgumentException)
            {
                fehler = ZapfSatz.Neu("NORMVEKTOR_PAKET_UNLESBAR", ex.Message);
                return null;
            }
            return AusDateien(dateien, out fehler, hinweise);
        }

        /// <summary>
        /// <b>Liest einen Archiveintrag, höchstens <paramref name="grenze"/> + 1 Byte</b> (Muster
        /// <c>IfcLeser.Entpacken</c>): Ergebnis ist der Text in UTF-8 (BOM erlaubt) oder <c>null</c>,
        /// wenn der Eintrag entpackt über die Grenze geht — dann hat das Zentralverzeichnis gelogen.
        /// <paramref name="gelesen"/> nennt die gelesenen Byte.
        /// </summary>
        private static string EintragLesen(ZipArchiveEntry eintrag, long grenze, out long gelesen)
        {
            using (Stream quelle = eintrag.Open())
            using (var ziel = new MemoryStream())
            {
                var block = new byte[81920];
                int n;
                while ((n = quelle.Read(block, 0, block.Length)) > 0)
                {
                    ziel.Write(block, 0, n);
                    if (ziel.Length > grenze) { gelesen = ziel.Length; return null; }
                }
                gelesen = ziel.Length;
                ziel.Position = 0;
                using (var leser = new StreamReader(ziel, Encoding.UTF8, true))
                    return leser.ReadToEnd();
            }
        }

        /// <summary>
        /// <b>Liest ein Paket aus schon gelesenen Dateien</b> (ein Ordner, ein Archiv, ein
        /// Testhelfer). Dieselben Prüfungen wie <see cref="AusStrom"/>; der Dateiname entscheidet,
        /// Groß- und Kleinschreibung spielt keine Rolle, Ordneranteile fallen weg.
        /// </summary>
        internal static Normformvektorsatz AusDateien(IReadOnlyList<TwwPaketdatei> dateien, out ZapfSatz fehler,
                                                      ICollection<ZapfSatz> hinweise = null)
        {
            fehler = null;
            var nachName = new Dictionary<string, TwwPaketdatei>(StringComparer.OrdinalIgnoreCase);
            foreach (TwwPaketdatei d in dateien ?? new TwwPaketdatei[0])
                nachName[Path.GetFileName(d.Name ?? "")] = d;

            foreach (string pflicht in PFLICHTDATEIEN)
                if (!nachName.ContainsKey(pflicht))
                {
                    fehler = ZapfSatz.Neu("NORMVEKTOR_DATEI_FEHLT", pflicht);
                    return null;
                }

            try
            {
                var satz = new Normformvektorsatz();
                Kategorien(Tabelle(nachName[DATEI_TYPTAGE], "code", "jahreszeit", "tagart", "bewoelkung"), satz);
                IReadOnlyList<int> zonen = Klimazonen(Tabelle(nachName[DATEI_KLIMAZONEN], "zone", "bezeichnung"));
                Anzahlen(Tabelle(nachName[DATEI_ANZAHL], "zone", "gebaeudeart", "typtag", "anzahl"), satz, zonen);
                Faktoren(Tabelle(nachName[DATEI_FAKTOREN], "gebaeudeart", "zone", "typtag", "faktor"), satz, zonen);
                Kennwerte(Tabelle(nachName[DATEI_KENNWERTE], "schluessel", "wert", "text"), satz);
                if (nachName.TryGetValue(DATEI_GAENGE, out TwwPaketdatei gaenge))
                    Gaenge(Tabelle(gaenge, "gebaeudeart", "typtag", "aufloesung_min", "index", "anteil"), satz, hinweise);
                Abschliessen(satz, zonen, hinweise);
                return satz;
            }
            catch (Paketabbruch p)
            {
                fehler = p.Satz;
                return null;
            }
        }

        // =================================================================================
        //  Die Teile des Pakets
        // =================================================================================

        private static void Kategorien(Pakettabelle t, Normformvektorsatz satz)
        {
            var liste = new List<Typtagkategorie>();
            var codes = new HashSet<string>(StringComparer.Ordinal);
            var merkmale = new HashSet<(Typtagjahreszeit, Typtagart, Typtagbewoelkung)>();
            foreach (Paketzeile z in t.Zeilen)
            {
                string code = t.Text(z, "code");
                if (code.Length == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FELD_LEER", t.Datei, z.Nummer, "code"));
                if (!codes.Add(code)) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_TYPTAG_DOPPELT", t.Datei, z.Nummer, code));
                Typtagjahreszeit js = Jahreszeit(t, z);
                Typtagart ta = Tagart(t, z);
                Typtagbewoelkung bw = Bewoelkung(t, z);
                // Der Merkmalsdreier ist der SCHLÜSSEL der Zuordnung (Typtagzuordnung sucht damit):
                // Zwei Kategorien mit demselben Dreier wären nicht unterscheidbar, und die zweite
                // bliebe still ungenutzt. Benannt abgelehnt, nicht der Reihenfolge überlassen.
                if (!merkmale.Add((js, ta, bw)))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_MERKMALE_DOPPELT", t.Datei, z.Nummer, code,
                        Typtagzuordnung.Jahreszeitbegriff(js), Typtagzuordnung.Tagartbegriff(ta),
                        Typtagzuordnung.Bewoelkungsbegriff(bw)));
                liste.Add(new Typtagkategorie(code, js, ta, bw));
            }
            if (liste.Count == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_OHNE_TYPTAGE"));
            satz.KategorienSetzen(liste);

            // Je Jahreszeit und Tagart genau eine Lesart: entweder "ohne" allein oder heiter UND bewoelkt.
            foreach (Typtagjahreszeit js in Enum.GetValues(typeof(Typtagjahreszeit)).Cast<Typtagjahreszeit>())
                foreach (Typtagart ta in Enum.GetValues(typeof(Typtagart)).Cast<Typtagart>())
                {
                    List<Typtagkategorie> gruppe = liste.Where(k => k.Jahreszeit == js && k.Tagart == ta).ToList();
                    if (gruppe.Count == 0)
                        throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KATEGORIEN_LUECKE",
                            Typtagzuordnung.Jahreszeitbegriff(js), Typtagzuordnung.Tagartbegriff(ta)));
                    bool ohne = gruppe.Any(k => k.Bewoelkung == Typtagbewoelkung.Ohne);
                    bool beide = gruppe.Any(k => k.Bewoelkung == Typtagbewoelkung.Heiter)
                                 && gruppe.Any(k => k.Bewoelkung == Typtagbewoelkung.Bewoelkt);
                    if (ohne && gruppe.Count > 1 || !ohne && !beide)
                        throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KATEGORIEN_BEWOELKUNG",
                            Typtagzuordnung.Jahreszeitbegriff(js), Typtagzuordnung.Tagartbegriff(ta), gruppe.Count));
                }
        }

        private static Typtagjahreszeit Jahreszeit(Pakettabelle t, Paketzeile z)
        {
            string s = t.Text(z, "jahreszeit").ToLowerInvariant();
            switch (s)
            {
                case "uebergang": case "übergang": case "u": case "ü": return Typtagjahreszeit.Uebergang;
                case "sommer": case "s": return Typtagjahreszeit.Sommer;
                case "winter": case "w": return Typtagjahreszeit.Winter;
                default: throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KATEGORIE_UNBEKANNT", t.Datei, z.Nummer, "jahreszeit", t.Text(z, "jahreszeit")));
            }
        }

        private static Typtagart Tagart(Pakettabelle t, Paketzeile z)
        {
            string s = t.Text(z, "tagart").ToLowerInvariant();
            switch (s)
            {
                case "werktag": case "w": return Typtagart.Werktag;
                case "sonntag": case "s": return Typtagart.Sonntag;
                default: throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KATEGORIE_UNBEKANNT", t.Datei, z.Nummer, "tagart", t.Text(z, "tagart")));
            }
        }

        private static Typtagbewoelkung Bewoelkung(Pakettabelle t, Paketzeile z)
        {
            string s = t.Text(z, "bewoelkung").ToLowerInvariant();
            switch (s)
            {
                case "heiter": case "h": return Typtagbewoelkung.Heiter;
                case "bewoelkt": case "bewölkt": case "b": return Typtagbewoelkung.Bewoelkt;
                case "ohne": case "x": case "": return Typtagbewoelkung.Ohne;
                default: throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KATEGORIE_UNBEKANNT", t.Datei, z.Nummer, "bewoelkung", t.Text(z, "bewoelkung")));
            }
        }

        private static IReadOnlyList<int> Klimazonen(Pakettabelle t)
        {
            var zonen = new List<int>();
            foreach (Paketzeile z in t.Zeilen)
            {
                long n = t.Ganz(z, "zone");
                if (n < 1) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZONE_UNBEKANNT", t.Datei, z.Nummer, n));
                if (zonen.Contains((int)n)) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZONE_DOPPELT", t.Datei, z.Nummer, n));
                zonen.Add((int)n);
            }
            if (zonen.Count == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_OHNE_ZONEN"));
            return zonen;
        }

        private static void Anzahlen(Pakettabelle t, Normformvektorsatz satz, IReadOnlyList<int> zonen)
        {
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Paketzeile z in t.Zeilen)
            {
                int zone = Zone(t, z, zonen);
                string art = Gebaeudeart(t, z);
                string typtag = Typtag(t, z, satz);
                long anzahl = t.Ganz(z, "anzahl");
                if (anzahl < 0 || anzahl > TAGE_JE_JAHR)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ANZAHL_UNGUELTIG", t.Datei, z.Nummer, anzahl));
                if (!gesehen.Add(zone + "\u0001" + art + "\u0001" + typtag))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZEILE_DOPPELT", t.Datei, z.Nummer));
                satz.AnzahlSetzen(zone, art, typtag, (int)anzahl);
            }
        }

        private static void Faktoren(Pakettabelle t, Normformvektorsatz satz, IReadOnlyList<int> zonen)
        {
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Paketzeile z in t.Zeilen)
            {
                int zone = Zone(t, z, zonen);
                string art = Gebaeudeart(t, z);
                string typtag = Typtag(t, z, satz);
                double f = t.Zahl(z, "faktor");
                // Der Faktor ist eine Schwankung um den Jahresmittelwert und darf negativ sein
                // (Grundlagen 5, Abschnitt 2.5); geprueft wird nur, dass er endlich ist.
                if (double.IsNaN(f) || double.IsInfinity(f))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FAKTOR_UNGUELTIG", t.Datei, z.Nummer));
                if (!gesehen.Add(zone + "\u0001" + art + "\u0001" + typtag))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZEILE_DOPPELT", t.Datei, z.Nummer));
                satz.FaktorSetzen(zone, art, typtag, f);
            }
        }

        private static void Kennwerte(Pakettabelle t, Normformvektorsatz satz)
        {
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Paketzeile z in t.Zeilen)
            {
                string schluessel = t.Text(z, "schluessel");
                if (schluessel.Length == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FELD_LEER", t.Datei, z.Nummer, "schluessel"));
                if (!gesehen.Add(schluessel)) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZEILE_DOPPELT", t.Datei, z.Nummer));
                string text = t.Hat("text") ? t.Text(z, "text") : "";
                if (string.Equals(schluessel, Typtagkennwert.TEXT_QUELLE, StringComparison.Ordinal)) { satz.Quelle = text; continue; }
                if (string.Equals(schluessel, Typtagkennwert.TEXT_AUSGABE, StringComparison.Ordinal)) { satz.Ausgabe = text; continue; }
                double wert = t.Zahl(z, "wert");
                if (double.IsNaN(wert) || double.IsInfinity(wert))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KEINE_ZAHL", t.Datei, z.Nummer, "wert", t.Text(z, "wert")));
                satz.KennwertSetzen(schluessel, wert);
            }
        }

        /// <summary>
        /// <b>Taugt eine Auflösung des Tagesgangs?</b> Sie muss den Tag ohne Rest teilen UND sich
        /// auf Stunden zusammenfassen lassen: entweder ein Teiler von 60 (1 … 60 Minuten je
        /// Abschnitt, mehrere Abschnitte je Stunde) oder ein Vielfaches von 60 (ein Abschnitt
        /// deckt ganze Stunden). Ein Wert wie 16 Minuten teilt den Tag (90 Abschnitte), lässt sich
        /// aber nicht auf Stunden summieren — <see cref="Typtaggang.Stundenanteile"/> gilt allein
        /// für diese beiden Fälle, und ein Gang, der sich nicht stündlich summieren lässt, wird
        /// benannt abgelehnt statt still verrechnet. Die Richtlinie führt 1 min und 15 min
        /// (Grundlagen 5, Abschnitt 2.6); 2 s lässt sich in Minuten nicht ausdrücken.
        /// </summary>
        internal static bool AufloesungTauglich(long aufloesungMin)
            => aufloesungMin >= 1 && aufloesungMin <= MINUTEN_JE_TAG && MINUTEN_JE_TAG % aufloesungMin == 0
               && (60 % aufloesungMin == 0 || aufloesungMin % 60 == 0);

        private static void Gaenge(Pakettabelle t, Normformvektorsatz satz, ICollection<ZapfSatz> hinweise)
        {
            // Je (Gebaeudeart, Typtag) die Abschnitte sammeln, dann Raster und Summe pruefen.
            var gesammelt = new Dictionary<string, (string Art, string Typtag, int Aufloesung, SortedDictionary<int, double> Werte)>(StringComparer.Ordinal);
            int ohneKategorie = 0;
            foreach (Paketzeile z in t.Zeilen)
            {
                string art = Gebaeudeart(t, z);
                string typtag = t.Text(z, "typtag");
                if (satz.Kategorie(typtag) == null) { ohneKategorie++; continue; }
                long aufloesung = t.Ganz(z, "aufloesung_min");
                if (!AufloesungTauglich(aufloesung))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_GANG_AUFLOESUNG", t.Datei, z.Nummer, aufloesung));
                long index = t.Ganz(z, "index");
                double anteil = t.Zahl(z, "anteil");
                if (double.IsNaN(anteil) || double.IsInfinity(anteil) || anteil < 0)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_GANG_ANTEIL", t.Datei, z.Nummer));
                string schluessel = art + "\u0001" + typtag;
                if (!gesammelt.TryGetValue(schluessel, out var g))
                {
                    g = (art, typtag, (int)aufloesung, new SortedDictionary<int, double>());
                    gesammelt[schluessel] = g;
                }
                if (g.Aufloesung != (int)aufloesung)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_GANG_AUFLOESUNG", t.Datei, z.Nummer, aufloesung));
                if (index < 0 || index >= MINUTEN_JE_TAG / g.Aufloesung)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_GANG_INDEX_ZEILE", t.Datei, z.Nummer, index));
                if (g.Werte.ContainsKey((int)index))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZEILE_DOPPELT", t.Datei, z.Nummer));
                g.Werte[(int)index] = anteil;
            }
            if (ohneKategorie > 0 && hinweise != null)
                hinweise.Add(ZapfSatz.Neu("NORMVEKTOR_GAENGE_UEBERGANGEN", ohneKategorie));

            foreach (var g in gesammelt.Values)
            {
                int abschnitte = MINUTEN_JE_TAG / g.Aufloesung;
                if (g.Werte.Count != abschnitte || g.Werte.Keys.First() != 0 || g.Werte.Keys.Last() != abschnitte - 1)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_GANG_INDEX", g.Art, g.Typtag, abschnitte, g.Werte.Count));
                double summe = g.Werte.Values.Sum();
                if (Math.Abs(summe - 1.0) > SUMME_TOLERANZ)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_GANG_SUMME", g.Art, g.Typtag, summe));
                satz.GangSetzen(new Typtaggang(g.Art, g.Typtag, g.Aufloesung, g.Werte.Values.ToArray()));
            }
        }

        // =================================================================================
        //  Die Proben über das ganze Paket
        // =================================================================================

        private static void Abschliessen(Normformvektorsatz satz, IReadOnlyList<int> zonen, ICollection<ZapfSatz> hinweise)
        {
            if (satz.Quelle.Length == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_QUELLE_FEHLT"));
            if (!satz.Kennwert(Typtagkennwert.WINTERGRENZE).HasValue)
                throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KENNWERT_FEHLT", Typtagkennwert.WINTERGRENZE));
            foreach (string art in satz.Gebaeudearten)
                if (!satz.Kennwert(Typtagkennwert.Heizgrenze(art)).HasValue)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KENNWERT_FEHLT", Typtagkennwert.Heizgrenze(art)));
            bool nachBewoelkung = satz.Kategorien.Any(k => k.Bewoelkung != Typtagbewoelkung.Ohne);
            if (nachBewoelkung && !satz.Kennwert(Typtagkennwert.BEWOELKUNG_SCHWELLE).HasValue)
                throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KENNWERT_FEHLT", Typtagkennwert.BEWOELKUNG_SCHWELLE));

            double? toleranz = satz.Kennwert(Typtagkennwert.PRUEFSUMME_TOLERANZ);
            if (!toleranz.HasValue && hinweise != null)
                hinweise.Add(ZapfSatz.Neu("NORMVEKTOR_PRUEFSUMME_OHNE_TOLERANZ", Typtagkennwert.PRUEFSUMME_TOLERANZ));

            int geprueft = 0;
            foreach (string art in satz.Gebaeudearten)
                foreach (int zone in satz.Klimazonen)
                {
                    // Eine Zone ohne jede Angabe zu dieser Gebaeudeart fuehrt das Paket eben nicht.
                    bool etwas = satz.Kategorien.Any(k => satz.Anzahl(zone, art, k.Code).HasValue
                                                          || satz.Faktor(zone, art, k.Code).HasValue);
                    if (!etwas) continue;
                    if (!satz.Vollstaendig(zone, art, out IReadOnlyList<string> fehlend))
                        throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_UNVOLLSTAENDIG", zone, art, fehlend.ToArray()));
                    int summe = satz.Tagesumme(zone, art);
                    if (summe != TAGE_JE_JAHR)
                        throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_SUMME_TAGE", zone, art, summe, TAGE_JE_JAHR));
                    geprueft++;
                    if (!toleranz.HasValue) continue;
                    double probe = satz.Kategorien.Sum(k => (satz.Anzahl(zone, art, k.Code) ?? 0) * (satz.Faktor(zone, art, k.Code) ?? 0.0));
                    if (Math.Abs(probe) > toleranz.Value && hinweise != null)
                        ZapfSatzEinmal(hinweise, ZapfSatz.Neu("NORMVEKTOR_PRUEFSUMME", zone, art, probe, toleranz.Value));
                }
            if (geprueft == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_OHNE_WERTE"));

            foreach (int zone in zonen)
                if (!satz.Klimazonen.Contains(zone) && hinweise != null)
                    hinweise.Add(ZapfSatz.Neu("NORMVEKTOR_ZONE_OHNE_WERTE", zone));
        }

        private static void ZapfSatzEinmal(ICollection<ZapfSatz> liste, ZapfSatz s)
        {
            if (liste != null && s != null && !liste.Contains(s)) liste.Add(s);
        }

        // =================================================================================
        //  Gemeinsame Felder
        // =================================================================================

        private static int Zone(Pakettabelle t, Paketzeile z, IReadOnlyList<int> zonen)
        {
            long n = t.Ganz(z, "zone");
            if (!zonen.Contains((int)n)) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_ZONE_UNBEKANNT", t.Datei, z.Nummer, n));
            return (int)n;
        }

        private static string Gebaeudeart(Pakettabelle t, Paketzeile z)
        {
            string s = t.Text(z, "gebaeudeart");
            if (s.Length == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FELD_LEER", t.Datei, z.Nummer, "gebaeudeart"));
            return s;
        }

        private static string Typtag(Pakettabelle t, Paketzeile z, Normformvektorsatz satz)
        {
            string s = t.Text(z, "typtag");
            if (satz.Kategorie(s) == null) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_TYPTAG_UNBEKANNT", t.Datei, z.Nummer, s));
            return s;
        }

        // =================================================================================
        //  Der Tabellenleser (Kopfzeile, Trenner, Feldzahl)
        // =================================================================================

        /// <summary>Eine Datenzeile: ihre Nummer in der Datei (die Kopfzeile ist 1) und ihre Felder.</summary>
        private sealed record Paketzeile(int Nummer, string[] Felder);

        /// <summary>Eine gelesene Paketdatei: Name, Spalten nach Index, Datenzeilen.</summary>
        private sealed class Pakettabelle
        {
            internal string Datei = "";
            internal readonly Dictionary<string, int> Spalten = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            internal readonly List<Paketzeile> Zeilen = new List<Paketzeile>();

            internal bool Hat(string spalte) => Spalten.ContainsKey(spalte);

            private string Feld(Paketzeile z, string spalte)
                => Spalten.TryGetValue(spalte, out int i) && i < z.Felder.Length ? (z.Felder[i] ?? "").Trim() : "";

            internal string Text(Paketzeile z, string spalte) => Feld(z, spalte);

            internal double Zahl(Paketzeile z, string spalte)
            {
                string s = Feld(z, spalte);
                if (s.Length == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FELD_LEER", Datei, z.Nummer, spalte));
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KEINE_ZAHL", Datei, z.Nummer, spalte, s));
                return w;
            }

            internal long Ganz(Paketzeile z, string spalte)
            {
                string s = Feld(z, spalte);
                if (s.Length == 0) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FELD_LEER", Datei, z.Nummer, spalte));
                if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long w))
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_KEINE_GANZE_ZAHL", Datei, z.Nummer, spalte, s));
                return w;
            }
        }

        /// <summary>
        /// Eine Datei als Tabelle: Trenner aus der Kopfzeile (<c>;</c> vor <c>,</c>), jede Spalte
        /// des Kopfes muss eine der genannten sein, jede genannte muss im Kopf stehen, und jede
        /// Datenzeile trägt die Feldzahl des Kopfes. Leere Zeilen fallen weg.
        /// </summary>
        private static Pakettabelle Tabelle(TwwPaketdatei d, params string[] spalten)
        {
            var t = new Pakettabelle { Datei = Path.GetFileName(d.Name ?? "") };
            string text = d.Inhalt ?? "";
            if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);
            int bruch = text.IndexOf('\n');
            string kopfzeile = bruch < 0 ? text : text.Substring(0, bruch);
            string trenner = kopfzeile.IndexOf(';') >= 0 ? ";" : ",";
            var erlaubt = new HashSet<string>(spalten, StringComparer.OrdinalIgnoreCase);

            // NReco setzt KEINE Vorgabe fuer BufferSize (ohne sie teilt der Leser durch null); 64 kB
            // begrenzt die Laenge EINES Satzes - wie der Katalogimport und der Ganglinienleser.
            var csv = new CsvReader(new StringReader(text), trenner) { BufferSize = 65536, TrimFields = true };
            bool kopf = true;
            while (csv.Read())
            {
                var felder = new string[csv.FieldsCount];
                for (int i = 0; i < felder.Length; i++) felder[i] = csv[i];
                int nummer = csv.ReadLinesCount;
                if (kopf)
                {
                    kopf = false;
                    for (int i = 0; i < felder.Length; i++)
                    {
                        string s = (felder[i] ?? "").Trim();
                        if (!erlaubt.Contains(s)) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_SPALTE_UNBEKANNT", t.Datei, s));
                        if (t.Spalten.ContainsKey(s)) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_SPALTE_DOPPELT", t.Datei, s));
                        t.Spalten[s] = i;
                    }
                    continue;
                }
                if (felder.All(f => (f ?? "").Trim().Length == 0)) continue;
                if (felder.Length != t.Spalten.Count)
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_FELDZAHL", t.Datei, nummer, felder.Length, t.Spalten.Count));
                t.Zeilen.Add(new Paketzeile(nummer, felder));
            }
            if (kopf) throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_OHNE_KOPFZEILE", t.Datei));
            // Die wahlfreie Spalte "text" und "bezeichnung" darf fehlen; alle uebrigen sind Pflicht.
            foreach (string s in spalten)
                if (!t.Hat(s) && s != "text" && s != "bezeichnung")
                    throw Abbruch(ZapfSatz.Neu("NORMVEKTOR_SPALTE_FEHLT", t.Datei, s));
            return t;
        }

        // =================================================================================
        //  Der Abbruch
        // =================================================================================

        /// <summary>Das Paket taugt seiner Form nach nicht — geworfen und ganz oben gefangen.</summary>
        private sealed class Paketabbruch : Exception
        {
            internal Paketabbruch(ZapfSatz satz) : base(satz?.Klartext ?? "") { Satz = satz; }

            internal ZapfSatz Satz { get; }
        }

        private static Paketabbruch Abbruch(ZapfSatz satz) => new Paketabbruch(satz);
    }
}
