using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Leser des Normformvektorpakets</b> (Umsetzungskonzept Zapfprofilgenerator 2.5,
    /// Kapitel 6; Stufe Z4b): ein gültiges Paket kommt vollständig herein, und JEDER
    /// Strukturfehler wird benannt abgelehnt — mit Datei und Zeile, nie still.
    ///
    /// <para><b>Kein Wert der Richtlinie.</b> Die Pakete dieser Klasse sind erfunden
    /// (<see cref="Typtagpaketbauer.Erfunden"/>); ein zweiter Fall liest das Paket aus der
    /// abgeleiteten JSON-Datei (ZU19) und schweigt, wenn sie fehlt.</para>
    /// </summary>
    public sealed class NormformvektorleserTests
    {
        // =================================================================================
        //  Das gültige Paket
        // =================================================================================

        [Fact]
        public void Ein_gueltiges_Paket_kommt_vollstaendig_herein()
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            var hinweise = new List<ZapfSatz>();
            Normformvektorsatz satz = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler, hinweise);

            Assert.Null(fehler);
            Assert.NotNull(satz);
            Assert.True(satz.Traegt);
            Assert.Equal(6, satz.Kategorien.Count);
            Assert.Equal(new[] { 3 }, satz.Klimazonen);
            Assert.Equal(new[] { "probehaus" }, satz.Gebaeudearten);
            Assert.Equal(365, satz.Tagesumme(3, "probehaus"));
            Assert.True(satz.Vollstaendig(3, "probehaus", out IReadOnlyList<string> fehlend));
            Assert.Empty(fehlend);
            Assert.Equal("Anwenderpaket (erfunden, Probe)", satz.Quelle);
            Assert.Equal("2026-09", satz.Ausgabe);
            Assert.Equal(15.0, satz.Kennwert(Typtagkennwert.Heizgrenze("probehaus")));
            Assert.Equal(5.0, satz.Kennwert(Typtagkennwert.WINTERGRENZE));
            Assert.Empty(satz.Gaenge);

            // Die Kategorie zu Jahreszeit, Tagart und Bewoelkung: "ohne" gilt fuer heiter wie bewoelkt.
            Typtagkategorie k = satz.Kategorie(Typtagjahreszeit.Winter, Typtagart.Sonntag, Typtagbewoelkung.Bewoelkt);
            Assert.NotNull(k);
            Assert.Equal(Typtagbewoelkung.Ohne, k.Bewoelkung);

            // Kein Hinweis: Die Toleranz steht im Paket, und die Pruefsumme ist geschlossen.
            Assert.Empty(hinweise);
        }

        [Fact]
        public void Ein_Paket_aus_einem_Strom_liest_dasselbe()
        {
            Typtagpaketbauer b = Typtagpaketbauer.MitBewoelkung();
            string ordner = Path.Combine(Path.GetTempPath(), "epos-typtagpaket-" + Guid.NewGuid().ToString("N"));
            try
            {
                string zip = b.AlsZip(ordner);
                using FileStream fs = File.OpenRead(zip);
                Normformvektorsatz satz = Normformvektorleser.AusStrom(fs, out ZapfSatz fehler);
                Assert.Null(fehler);
                Assert.NotNull(satz);
                Assert.Equal(10, satz.Kategorien.Count);
                Assert.Equal(365, satz.Tagesumme(3, "probehaus"));
                Assert.Equal(5.0, satz.Kennwert(Typtagkennwert.BEWOELKUNG_SCHWELLE));
            }
            finally
            {
                if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
            }
        }

        [Fact]
        public void Ein_Strom_ohne_Archiv_wird_benannt_abgelehnt()
        {
            using var s = new MemoryStream(Encoding.UTF8.GetBytes("kein ZIP"));
            Normformvektorsatz satz = Normformvektorleser.AusStrom(s, out ZapfSatz fehler);
            Assert.Null(satz);
            Assert.Equal("NORMVEKTOR_PAKET_UNLESBAR", fehler.Kennung);

            Assert.Null(Normformvektorleser.AusStrom(null, out ZapfSatz ohne));
            Assert.Equal("NORMVEKTOR_PAKET_UNLESBAR", ohne.Kennung);
        }

        /// <summary>
        /// Das Paket aus <c>Referenzlaeufe/Skripte/vdi4655_abgeleitet.json</c> (ZU19, abgeleitete
        /// Werte — kein Original) liest sich vollständig: 15 Zonen, drei Gebäudearten, je Zone und
        /// Gebäudeart 365 Kalendertage. Ohne die Datei schweigt der Fall.
        /// </summary>
        [Fact]
        public void Das_abgeleitete_Paket_liest_sich_vollstaendig()
        {
            Typtagpaketbauer b = Typtagpaketbauer.AusAbgeleiteterJson();
            if (b == null) return;
            var hinweise = new List<ZapfSatz>();
            Normformvektorsatz satz = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler, hinweise);
            Assert.Null(fehler);
            Assert.NotNull(satz);
            Assert.Equal(10, satz.Kategorien.Count);
            Assert.Equal(15, satz.Klimazonen.Count);
            Assert.Equal(3, satz.Gebaeudearten.Count);
            foreach (string art in satz.Gebaeudearten)
                foreach (int zone in satz.Klimazonen)
                {
                    Assert.Equal(365, satz.Tagesumme(zone, art));
                    Assert.True(satz.Vollstaendig(zone, art, out _), "Zone " + zone + " / " + art);
                }
            // Die Faktoren der abgeleiteten Datei sind NICHT renormiert - die Pruefsumme steht
            // deshalb nicht im Paket, und der Leser sagt das als Hinweis (Regel des Skripts).
            Assert.Contains(hinweise, h => h.Kennung == "NORMVEKTOR_PRUEFSUMME_OHNE_TOLERANZ");
            // Ein negativer Faktor ist erlaubt: Er ist eine Schwankung um den Jahresmittelwert.
            Assert.Contains(satz.Klimazonen, z => satz.Kategorien.Any(k => (satz.Faktor(z, satz.Gebaeudearten[0], k.Code) ?? 0) < 0));
        }

        // =================================================================================
        //  Jeder Strukturfehler benannt
        // =================================================================================

        /// <summary>
        /// Je Zeile ein verletztes Paket und die Kennung, mit der es abgelehnt wird. Die Liste
        /// deckt jede Prüfung des Lesers ab — eine neue Prüfung ohne Fall fällt in
        /// <see cref="Jede_Ablehnung_des_Lesers_hat_ihren_Fall"/> auf.
        /// </summary>
        public static TheoryData<string, string> Fehlerfaelle()
        {
            var d = new TheoryData<string, string>();
            foreach (var f in Faelle) d.Add(f.Name, f.Kennung);
            return d;
        }

        private static readonly (string Name, string Kennung, Func<List<TwwPaketdatei>, List<TwwPaketdatei>> Verletzen)[] Faelle =
        {
            ("Pflichtdatei fehlt", "NORMVEKTOR_DATEI_FEHLT",
                d => Typtagpaketbauer.Ohne(d, Normformvektorleser.DATEI_FAKTOREN)),
            ("Kopfzeile fehlt", "NORMVEKTOR_OHNE_KOPFZEILE",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "")),
            ("Spalte unbekannt", "NORMVEKTOR_SPALTE_UNBEKANNT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "zone;bezeichnung;hoehe\n3;Zone 3;120\n")),
            ("Spalte doppelt", "NORMVEKTOR_SPALTE_DOPPELT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "zone;zone\n3;3\n")),
            ("Spalte fehlt", "NORMVEKTOR_SPALTE_FEHLT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL, "zone;gebaeudeart;typtag\n3;probehaus;T01\n")),
            ("Feldzahl", "NORMVEKTOR_FELDZAHL",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "zone;bezeichnung\n3;Zone 3;zuviel\n")),
            ("Feld leer", "NORMVEKTOR_FELD_LEER",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_TYPTAGE)
                            .Replace("T01;", ";", StringComparison.Ordinal))),
            ("keine Zahl", "NORMVEKTOR_KEINE_ZAHL",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_FAKTOREN,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_FAKTOREN)
                            .Replace(";1E-05", ";sehr viel", StringComparison.Ordinal))),
            ("keine ganze Zahl", "NORMVEKTOR_KEINE_GANZE_ZAHL",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_ANZAHL)
                            .Replace(";90", ";90,5", StringComparison.Ordinal))),
            ("Kategorie unbekannt", "NORMVEKTOR_KATEGORIE_UNBEKANNT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_TYPTAGE)
                            .Replace(";uebergang;", ";fruehling;", StringComparison.Ordinal))),
            ("Typtag doppelt", "NORMVEKTOR_TYPTAG_DOPPELT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_TYPTAGE)
                            .Replace("T02;", "T01;", StringComparison.Ordinal))),
            ("Typtag unbekannt", "NORMVEKTOR_TYPTAG_UNBEKANNT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_ANZAHL)
                            .Replace(";T01;", ";T99;", StringComparison.Ordinal))),
            ("Zone doppelt", "NORMVEKTOR_ZONE_DOPPELT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "zone;bezeichnung\n3;A\n3;B\n")),
            ("Zone unbekannt", "NORMVEKTOR_ZONE_UNBEKANNT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_ANZAHL)
                            .Replace("3;probehaus;T01", "9;probehaus;T01", StringComparison.Ordinal))),
            ("Zone ohne Nummer", "NORMVEKTOR_ZONE_UNBEKANNT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "zone;bezeichnung\n0;A\n")),
            ("Zeile doppelt", "NORMVEKTOR_ZEILE_DOPPELT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_ANZAHL) + "3;probehaus;T01;90\n")),
            ("Anzahl ungueltig", "NORMVEKTOR_ANZAHL_UNGUELTIG",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_ANZAHL)
                            .Replace(";90", ";-1", StringComparison.Ordinal))),
            ("Summe nicht 365", "NORMVEKTOR_SUMME_TAGE",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_ANZAHL)
                            .Replace(";90", ";89", StringComparison.Ordinal))),
            ("unvollstaendig", "NORMVEKTOR_UNVOLLSTAENDIG",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_FAKTOREN,
                        string.Join("\n", Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_FAKTOREN)
                            .Split('\n').Where(z => !z.Contains(";T01;", StringComparison.Ordinal))) )),
            ("ohne Typtage", "NORMVEKTOR_OHNE_TYPTAGE",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE, "code;jahreszeit;tagart;bewoelkung\n")),
            ("ohne Zonen", "NORMVEKTOR_OHNE_ZONEN",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KLIMAZONEN, "zone;bezeichnung\n")),
            ("ohne Werte", "NORMVEKTOR_OHNE_WERTE",
                d => Typtagpaketbauer.Ersetzen(
                        Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_ANZAHL, "zone;gebaeudeart;typtag;anzahl\n"),
                        Normformvektorleser.DATEI_FAKTOREN, "gebaeudeart;zone;typtag;faktor\n")),
            ("Quelle fehlt", "NORMVEKTOR_QUELLE_FEHLT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KENNWERTE,
                        string.Join("\n", Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_KENNWERTE)
                            .Split('\n').Where(z => !z.StartsWith("quelle;", StringComparison.Ordinal))))),
            ("Kennwert fehlt", "NORMVEKTOR_KENNWERT_FEHLT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_KENNWERTE,
                        string.Join("\n", Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_KENNWERTE)
                            .Split('\n').Where(z => !z.StartsWith("wintergrenze;", StringComparison.Ordinal))))),
            ("Kategorienluecke", "NORMVEKTOR_KATEGORIEN_LUECKE",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE,
                        string.Join("\n", Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_TYPTAGE)
                            .Split('\n').Where(z => !z.StartsWith("T06;", StringComparison.Ordinal))))),
            // Zwei Typtage mit demselben Merkmalsdreier: Der Dreier ist der Schluessel der
            // Zuordnung, der zweite bliebe still ungenutzt (Befund Gruppe 1).
            ("Merkmalsdreier doppelt", "NORMVEKTOR_MERKMALE_DOPPELT",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_TYPTAGE)
                            .Replace("T02;uebergang;sonntag;ohne", "T02;uebergang;werktag;ohne", StringComparison.Ordinal))),
            ("Bewoelkung halb", "NORMVEKTOR_KATEGORIEN_BEWOELKUNG",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_TYPTAGE,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_TYPTAGE)
                            .Replace("T01;uebergang;werktag;ohne", "T01;uebergang;werktag;heiter", StringComparison.Ordinal))),
            ("Gang: Aufloesung", "NORMVEKTOR_GANG_AUFLOESUNG",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_GAENGE,
                        "gebaeudeart;typtag;aufloesung_min;index;anteil\nprobehaus;T01;7;0;1\n")),
            // 16 Minuten teilen den Tag (90 Abschnitte), lassen sich aber nicht auf Stunden
            // summieren - weder Teiler noch Vielfaches von 60 (Befund Gruppe 1).
            ("Gang: Aufloesung nicht stuendlich", "NORMVEKTOR_GANG_AUFLOESUNG",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_GAENGE,
                        "gebaeudeart;typtag;aufloesung_min;index;anteil\nprobehaus;T01;16;0;1\n")),
            ("Gang: Index der Zeile", "NORMVEKTOR_GANG_INDEX_ZEILE",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_GAENGE,
                        "gebaeudeart;typtag;aufloesung_min;index;anteil\nprobehaus;T01;720;5;1\n")),
            ("Gang: Luecke", "NORMVEKTOR_GANG_INDEX",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_GAENGE,
                        "gebaeudeart;typtag;aufloesung_min;index;anteil\nprobehaus;T01;720;0;1\n")),
            ("Gang: Summe", "NORMVEKTOR_GANG_SUMME",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_GAENGE,
                        "gebaeudeart;typtag;aufloesung_min;index;anteil\nprobehaus;T01;720;0;0.4\nprobehaus;T01;720;1;0.4\n")),
            ("Gang: Anteil negativ", "NORMVEKTOR_GANG_ANTEIL",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_GAENGE,
                        "gebaeudeart;typtag;aufloesung_min;index;anteil\nprobehaus;T01;720;0;-0.1\nprobehaus;T01;720;1;1.1\n")),
            ("Faktor nicht endlich", "NORMVEKTOR_FAKTOR_UNGUELTIG",
                d => Typtagpaketbauer.Ersetzen(d, Normformvektorleser.DATEI_FAKTOREN,
                        Typtagpaketbauer.Inhalt(d, Normformvektorleser.DATEI_FAKTOREN)
                            .Replace(";1E-05", ";NaN", StringComparison.Ordinal))),
        };

        [Theory]
        [MemberData(nameof(Fehlerfaelle))]
        public void Jeder_Strukturfehler_wird_benannt_abgelehnt(string name, string kennung)
        {
            var f = Faelle.Single(x => x.Name == name && x.Kennung == kennung);
            List<TwwPaketdatei> dateien = f.Verletzen(Typtagpaketbauer.Erfunden().Dateien());
            Normformvektorsatz satz = Normformvektorleser.AusDateien(dateien, out ZapfSatz fehler);
            Assert.Null(satz);
            Assert.NotNull(fehler);
            Assert.Equal(kennung, fehler.Kennung);
            Assert.NotEqual("", fehler.Klartext);
        }

        /// <summary>
        /// Jede Kennung <c>NORMVEKTOR_…</c>, die der Leser wirft, hat oben ihren Fall — bis auf
        /// die, die kein Strukturfehler EINER Datei sind (Strom unlesbar, Mengengrenze des Archivs,
        /// drei Hinweise); sie haben ihren eigenen Fall weiter unten.
        /// </summary>
        [Fact]
        public void Jede_Ablehnung_des_Lesers_hat_ihren_Fall()
        {
            string quelle = Leserquelle();
            if (quelle == null) return;
            var ohneFall = new[]
            {
                "NORMVEKTOR_PAKET_UNLESBAR", "NORMVEKTOR_PAKET_ZU_GROSS", "NORMVEKTOR_GAENGE_UEBERGANGEN",
                "NORMVEKTOR_PRUEFSUMME_OHNE_TOLERANZ", "NORMVEKTOR_PRUEFSUMME", "NORMVEKTOR_ZONE_OHNE_WERTE"
            };
            var gedeckt = new HashSet<string>(Faelle.Select(f => f.Kennung), StringComparer.Ordinal);
            var offen = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(quelle), "\"(NORMVEKTOR_[A-Z0-9_]+)\"")
                .Select(m => m.Groups[1].Value).Distinct()
                .Where(k => !gedeckt.Contains(k) && !ohneFall.Contains(k)).ToList();
            Assert.True(offen.Count == 0, "Ablehnung ohne Fall: " + string.Join(", ", offen));
        }

        // =================================================================================
        //  Die Hinweise
        // =================================================================================

        [Fact]
        public void Eine_verletzte_Pruefsumme_ist_ein_Hinweis_keine_Ablehnung()
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            // Die Toleranz bleibt, der letzte Faktor wird verstellt: Die Pruefsumme kippt.
            b.Faktor[(3, "probehaus", b.Kategorien[^1].Code)] = 0.001;
            var hinweise = new List<ZapfSatz>();
            Normformvektorsatz satz = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler, hinweise);
            Assert.Null(fehler);
            Assert.NotNull(satz);
            Assert.Contains(hinweise, h => h.Kennung == "NORMVEKTOR_PRUEFSUMME");
        }

        [Fact]
        public void Ein_Tagesgang_ohne_Kategorie_ist_ein_Hinweis()
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            b.Gaenge.Add(("probehaus", "T99", 720, new[] { 0.5, 0.5 }));
            var hinweise = new List<ZapfSatz>();
            Normformvektorsatz satz = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler, hinweise);
            Assert.Null(fehler);
            Assert.Empty(satz.Gaenge);
            Assert.Contains(hinweise, h => h.Kennung == "NORMVEKTOR_GAENGE_UEBERGANGEN");
        }

        [Fact]
        public void Ein_gueltiger_Tagesgang_kommt_mit_und_faellt_auf_Stunden()
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            foreach (var k in b.Kategorien) b.Gaenge.Add(("probehaus", k.Code, 720, new[] { 0.25, 0.75 }));
            Normformvektorsatz satz = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(6, satz.Gaenge.Count);

            Typtaggang g = satz.Gang("probehaus", b.Kategorien[0].Code);
            Assert.NotNull(g);
            Assert.Equal(720, g.AufloesungMin);
            double[] stunden = g.Stundenanteile();
            Assert.Equal(24, stunden.Length);
            Assert.Equal(1.0, stunden.Sum(), 12);
            // 720 Minuten = 12 Stunden: der erste Abschnitt verteilt 0,25 auf die Stunden 0..11.
            Assert.Equal(0.25 / 12.0, stunden[0], 12);
            Assert.Equal(0.75 / 12.0, stunden[12], 12);
        }

        /// <summary>
        /// Die Mengengrenze des Archivs (Befund Gruppe 1): Eintragszahl und entpackte Größe werden
        /// aus dem Zentralverzeichnis geprüft, bevor ein Byte entpackt wird — wie im TRY-Paketleser.
        /// </summary>
        [Fact]
        public void Ein_Archiv_mit_zu_vielen_Eintraegen_wird_benannt_abgelehnt()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-typtagpaket-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(ordner);
                string pfad = Path.Combine(ordner, "viele.zip");
                using (FileStream fs = File.Create(pfad))
                using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (TwwPaketdatei d in Typtagpaketbauer.Erfunden().Dateien())
                        using (var s = new StreamWriter(zip.CreateEntry(d.Name).Open(), new UTF8Encoding(false)))
                            s.Write(d.Inhalt);
                    for (int i = 0; i <= Normformvektorleser.HOECHSTENS_EINTRAEGE; i++)
                        zip.CreateEntry("beilage_" + i + ".txt");
                }
                using FileStream lesen = File.OpenRead(pfad);
                Assert.Null(Normformvektorleser.AusStrom(lesen, out ZapfSatz fehler));
                Assert.Equal("NORMVEKTOR_PAKET_ZU_GROSS", fehler.Kennung);
            }
            finally
            {
                if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
            }
        }

        /// <summary>
        /// Das Raster eines Tagesgangs muss sich auf Stunden summieren lassen: Teiler von 60 oder
        /// Vielfaches von 60, und dabei Teiler von 1440. Alles andere wird benannt abgelehnt —
        /// <c>Typtaggang.Stundenanteile()</c> gilt allein für diese Fälle (Befund Gruppe 1).
        /// </summary>
        [Fact]
        public void Nur_ein_stuendlich_summierbares_Raster_taugt()
        {
            foreach (int gut in new[] { 1, 2, 5, 15, 30, 60, 120, 480, 720, 1440 })
                Assert.True(Normformvektorleser.AufloesungTauglich(gut), gut + " Minuten sollten taugen.");
            // Teiler von 1440, aber nicht von 60 und kein Vielfaches von 60: nicht stuendlich.
            foreach (int schlecht in new[] { 8, 9, 16, 18, 24, 32, 36, 45, 48, 80, 90, 96, 144, 160, 288 })
                Assert.False(Normformvektorleser.AufloesungTauglich(schlecht), schlecht + " Minuten duerfen nicht taugen.");
            // Kein Teiler des Tages, kein Wert, negativ, zu gross.
            foreach (int schlecht in new[] { 0, -15, 7, 1441, 2880 })
                Assert.False(Normformvektorleser.AufloesungTauglich(schlecht), schlecht + " Minuten duerfen nicht taugen.");
        }

        [Fact]
        public void Ein_Trenner_Komma_und_ein_BOM_stoeren_nicht()
        {
            List<TwwPaketdatei> dateien = Typtagpaketbauer.Erfunden().Dateien();
            // Der Trenner gilt JE DATEI. Die Kennwerte bleiben beim Semikolon, weil ihr Quelltext
            // selbst ein Komma traegt - genau der Grund, aus dem ein Paket beide Trenner mischen darf.
            var umgestellt = dateien.Select(d => new TwwPaketdatei(d.Name,
                "﻿" + (d.Name == Normformvektorleser.DATEI_KENNWERTE ? d.Inhalt : d.Inhalt.Replace(';', ',')))).ToList();
            Normformvektorsatz satz = Normformvektorleser.AusDateien(umgestellt, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(6, satz.Kategorien.Count);
        }

        /// <summary><c>EPOS.Kern/Allgemein/Import/Normformvektorleser.cs</c>, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Leserquelle([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei ?? ""), AppContext.BaseDirectory })
            {
                DirectoryInfo o = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                for (int i = 0; i < 8 && o != null; i++, o = o.Parent)
                {
                    string kandidat = Path.Combine(o.FullName, "EPOS.Kern", "Allgemein", "Import", "Normformvektorleser.cs");
                    if (File.Exists(kandidat)) return kandidat;
                }
            }
            return null;
        }
    }
}
