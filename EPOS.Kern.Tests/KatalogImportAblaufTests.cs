using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Katalogimport als EIN Kern-Ablauf</b> (iU9-W13.0a bis 0f).
    ///
    /// <para>Dieselben Proben wie in <see cref="KatalogImportTests"/>, nur durch den
    /// NEUEN Weg: <see cref="KatalogImportProfil"/> waehlt Parser und Schreibweg,
    /// <see cref="KatalogImportAblauf"/> liest, filtert, prueft vor und fuehrt aus.
    /// Erwartet werden DIESELBEN Zahlen — das ist der Sinn: Die Vierlinge sind eine
    /// Komponente geworden, ohne dass sich ein Ergebnis verschiebt.</para>
    ///
    /// <para>Die Faelle mit Datenbank teilen sich EINE Arbeitskopie je Klasse
    /// (Regel seit W11a) und schweigen, wenn die Datei fehlt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogImportAblaufTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KatalogImportAblaufTests(TestDatenbank db) { _db = db; }

        private static string Probe(string name)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string ordner = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben");
                if (Directory.Exists(ordner))
                {
                    string pfad = Path.Combine(ordner, name);
                    Assert.True(File.Exists(pfad), "Die Probe fehlt: " + pfad);
                    return pfad;
                }
            }
            Assert.Fail("Der Probenordner Referenzlaeufe/Importproben wurde nicht gefunden.");
            return null;
        }

        private static KatalogImportAblauf Ablauf(KatalogImportArt art)
        {
            return new KatalogImportAblauf(KatalogImportProfil.Finde(art));
        }

        // ==================================================================
        // 1 — Das Profil
        // ==================================================================

        /// <summary>
        /// Die Filtervorbelegungen und Nachkommastellen sind WOERTLICH die der vier
        /// Designer und muessen bitgleich bleiben — sie sind das, was der Anwender
        /// beim Oeffnen sieht.
        /// </summary>
        [Theory]
        [InlineData(KatalogImportArt.Heizkessel,       10.0,  200.0, 1)]
        [InlineData(KatalogImportArt.Pufferspeicher,    0.0, 1000.0, 0)]
        [InlineData(KatalogImportArt.Solarkollektoren,  0.0,    5.0, 2)]
        [InlineData(KatalogImportArt.Waermepumpe,       0.0,  100.0, 0)]
        public void DasProfilTraegtDieFiltervorbelegungDesDesigners(
            KatalogImportArt art, double von, double bis, int nachkomma)
        {
            KatalogImportProfil p = KatalogImportProfil.Finde(art);

            Assert.Equal(von, p.FilterVon);
            Assert.Equal(bis, p.FilterBis);
            Assert.Equal(nachkomma, p.FilterNachkommastellen);
            Assert.Equal(100000.0, p.FilterMaximum);
            Assert.Equal("(*.vdi)|*.vdi", p.Dateifilter);
        }

        /// <summary>
        /// Die Zahl der Detailfelder je Auspraegung — 7 / 5 / 11 / 10. Der Solarwert
        /// ist 11 und nicht 10, weil das Beschreibungsfeld mitzaehlt: Es steht im
        /// Designer, wurde vom Vorlaeufer aber nie befuellt (Befund W13-B25).
        /// </summary>
        [Theory]
        [InlineData(KatalogImportArt.Heizkessel, 7)]
        [InlineData(KatalogImportArt.Pufferspeicher, 5)]
        [InlineData(KatalogImportArt.Solarkollektoren, 11)]
        [InlineData(KatalogImportArt.Waermepumpe, 10)]
        public void DasProfilFuehrtDieDetailfelderDerMaske(KatalogImportArt art, int anzahl)
        {
            KatalogImportProfil p = KatalogImportProfil.Finde(art);

            Assert.Equal(anzahl, p.Detailfelder.Count);
            // Nur der Bezeichner ist aenderbar - in allen vier Designern traegt jedes
            // andere Detailfeld ein Enabled = false.
            Assert.Single(p.Detailfelder, f => f.Editierbar);
            Assert.Equal(KatalogImportProfil.FeldName, p.Detailfelder.First(f => f.Editierbar).Schluessel);
            Assert.Equal(KatalogImportProfil.FeldName, p.Detailfelder[0].Schluessel);
            Assert.Equal(KatalogImportProfil.FeldFirma, p.Detailfelder[1].Schluessel);
        }

        /// <summary>
        /// Der Unterordner der Waermepumpe traegt jetzt sein Gewerk; der alte Ordner
        /// bleibt als Rueckfall, damit ein Anwender seine dort abgelegten Kataloge
        /// weiter findet (Befund W13-B28, Abweichung A-1).
        /// </summary>
        [Fact]
        public void DerWaermepumpenordnerTraegtSeinGewerkUndBehaeltDenAltenAlsRueckfall()
        {
            Assert.Equal("VDI_Heizkessel", KatalogImportProfil.Finde(KatalogImportArt.Heizkessel).Unterordner);
            Assert.Equal("VDI_Pufferspeicher", KatalogImportProfil.Finde(KatalogImportArt.Pufferspeicher).Unterordner);
            Assert.Equal("VDI_Solarthermie", KatalogImportProfil.Finde(KatalogImportArt.Solarkollektoren).Unterordner);

            KatalogImportProfil wp = KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe);
            Assert.Equal("VDI_Waermepumpe", wp.Unterordner);
            Assert.Equal("VDI", wp.UnterordnerRueckfall);

            // Nur die Waermepumpe hat ueberhaupt einen Rueckfall.
            Assert.Single(KatalogImportProfil.AlleArten,
                x => KatalogImportProfil.Finde(x).UnterordnerRueckfall.Length > 0);
        }

        /// <summary>
        /// Jede Auspraegung trifft ihren Katalog, und jeder Katalog fuehrt
        /// <c>ImportSpalten</c>.
        /// </summary>
        [Theory]
        [InlineData(KatalogImportArt.Heizkessel, "HEIZKESSEL")]
        [InlineData(KatalogImportArt.Pufferspeicher, "PUFFERSPEICHER")]
        [InlineData(KatalogImportArt.Solarkollektoren, "SOLARKOLLEKTOREN")]
        [InlineData(KatalogImportArt.Waermepumpe, "WP")]
        public void JedeAuspraegungTrifftIhrenKatalog(KatalogImportArt art, string schluessel)
        {
            KatalogImportProfil p = KatalogImportProfil.Finde(art);

            Assert.Equal(schluessel, p.Katalogschluessel);
            Assert.NotNull(p.Katalog);
            Assert.NotNull(p.Katalog.ImportSpalten);
        }

        /// <summary>Der Uebersetzer wird benutzt; ohne ihn steht der Schluessel selbst da.</summary>
        [Fact]
        public void DerUebersetzerGehtDurchAlleBeschriftungen()
        {
            KatalogImportProfil roh = KatalogImportProfil.Finde(KatalogImportArt.Heizkessel);
            Assert.Equal("IMP_KAT_FELD_NAME", roh.Detailfelder[0].Bezeichnung);

            KatalogImportProfil uebersetzt = KatalogImportProfil.Finde(
                KatalogImportArt.Heizkessel, s => "[" + s + "]");
            Assert.Equal("[IMP_KAT_FELD_NAME]", uebersetzt.Detailfelder[0].Bezeichnung);
            Assert.Equal("[IMP_KAT_FILTER_LEISTUNG]", uebersetzt.FilterBezeichnung);
        }

        // ==================================================================
        // 2 — Lesen ueber den Ablauf
        // ==================================================================

        /// <summary>
        /// Derselbe Ausschnitt, dieselbe Satzzahl und dieselben Namen wie ueber den
        /// Parser unmittelbar (<see cref="KatalogImportTests"/>).
        /// </summary>
        [Fact]
        public void DerAblaufLiestDieselbenSaetzeWieDerParser()
        {
            if (!_db.Vorhanden) return;   // Heizkessel fragt den Brennstoffdeckel ab

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Heizkessel);
            int n = a.Lesen(Probe("heizkessel_vaillant.vdi"));

            Assert.Equal(5, n);
            Assert.Equal(new[]
            {
                "ecoVIT VKK 186/5", "ecoVIT VKK 256/5", "ecoVIT VKK 356/5",
                "ecoCRAFT VKK 806/3", "ecoCRAFT VKK 1206/3"
            }, a.Saetze.Select(s => s.Name).ToArray());
            Assert.All(a.Saetze, s => Assert.Equal("Vaillant Deutschland GmbH & Co. KG", s.Firma));
            Assert.Empty(a.Meldungen);
        }

        [Fact]
        public void DerAblaufLiestPufferspeicherSolarUndWaermepumpen()
        {
            KatalogImportAblauf psp = Ablauf(KatalogImportArt.Pufferspeicher);
            Assert.Equal(9, psp.Lesen(Probe("pufferspeicher_vaillant.vdi")));
            Assert.Equal(303.0, psp.Saetze[0].Filterwert, 6);

            KatalogImportAblauf st = Ablauf(KatalogImportArt.Solarkollektoren);
            Assert.Equal(3, st.Lesen(Probe("solarkollektoren_vaillant.vdi")));
            Assert.Equal(2.35, st.Saetze[0].Filterwert, 6);

            KatalogImportAblauf wp = Ablauf(KatalogImportArt.Waermepumpe);
            Assert.Equal(3, wp.Lesen(Probe("waermepumpen_hoval.vdi")));
            Assert.Equal(7.9, wp.Saetze[0].Filterwert, 6);
        }

        /// <summary>
        /// <b>Der Absturz ist eine Meldung geworden</b> (Befund W13-B35, Abweichung
        /// A-2): Ein Aufstellungsindex ausserhalb 1…4 riss bisher den ganzen
        /// Dateiimport mit — aus einem Katalog mit 129 Waermepumpen wurde wegen
        /// EINES Satzes nichts. Jetzt laeuft die Datei durch, und die Warnung steht
        /// im Protokoll.
        /// </summary>
        [Fact]
        public void EinUnbekannterAufstellungsindexIstEineWarnungKeinAbbruch()
        {
            KatalogImportAblauf a = Ablauf(KatalogImportArt.Waermepumpe);

            int n = a.Lesen(Probe("waermepumpen_gegenprobe_aufstellung.vdi"));

            Assert.Equal(3, n);
            Assert.Contains(a.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_AUFSTELLUNG");
            Assert.Contains(a.Meldungen, m => m.Werte.Length > 0 && m.Werte[0] == "7");
            // Ohne gueltigen Index bleibt die Aufstellung leer statt falsch.
            Assert.Equal("", a.Saetze[0].Detailwert("AUFSTELLUNG"));
        }

        /// <summary>Eine fehlende Datei ergibt 0 Saetze und eine Meldung, keinen Wurf.</summary>
        [Fact]
        public void EineFehlendeDateiErgibtEineMeldung()
        {
            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);

            Assert.Equal(0, a.Lesen(Path.Combine(Path.GetTempPath(), "w13-gibt-es-nicht.vdi")));
            Assert.Contains(a.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_LESEFEHLER");

            Assert.Equal(0, a.Lesen(""));
            Assert.Empty(a.Saetze);
        }

        /// <summary>
        /// Der Melder liefert Anfang und Ende des Lesens.
        ///
        /// <para>Der Prueflauf nimmt einen UNMITTELBAREN Melder und nicht
        /// <see cref="Progress{T}"/>: Der marshalt ueber den
        /// Synchronisationskontext, und ob es zum Zeitpunkt der Zusicherung schon
        /// angekommen ist, haengt dann davon ab, wer sonst gerade laeuft. Der Wirt
        /// braucht das Marshalling — der Test braucht die Aussage.</para>
        /// </summary>
        [Fact]
        public void DerMelderBegleitetDasLesen()
        {
            var gesehen = new List<ImportFortschritt>();
            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);

            a.Lesen(Probe("pufferspeicher_vaillant.vdi"), new Mitschrift(gesehen));

            Assert.Contains(gesehen, f => f.Schluessel == "IMP_KAT_PROT_LESEN");
            Assert.Contains(gesehen, f => f.Schluessel == "IMP_KAT_PROT_GELESEN" && f.Werte[0] == "9");
        }

        /// <summary>Ein Melder, der unmittelbar in eine Liste schreibt.</summary>
        private sealed class Mitschrift : IProgress<ImportFortschritt>
        {
            private readonly List<ImportFortschritt> _liste;
            public Mitschrift(List<ImportFortschritt> liste) { _liste = liste; }
            public void Report(ImportFortschritt wert) { _liste.Add(wert); }
        }

        /// <summary>Ein gesetztes Abbruchzeichen bricht das Lesen ab.</summary>
        [Fact]
        public void EinAbbruchBeendetDasLesen()
        {
            using (var quelle = new CancellationTokenSource())
            {
                quelle.Cancel();
                KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);

                Assert.Throws<OperationCanceledException>(
                    () => a.Lesen(Probe("pufferspeicher_vaillant.vdi"), null, quelle.Token));
                Assert.Empty(a.Saetze);
            }
        }

        // ==================================================================
        // 3 — Filtern
        // ==================================================================

        /// <summary>
        /// Der Zahlenfilter und der Suchtext zusammen — der Rumpf von
        /// <c>FuelleListe</c> ohne Steuerelemente.
        /// </summary>
        [Fact]
        public void DerFilterVerbindetZahlenbereichUndSuchtext()
        {
            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);
            a.Lesen(Probe("pufferspeicher_vaillant.vdi"));

            // Vorbelegung des Designers: 0 bis 1000 Liter
            List<int> alle = a.Anzeigeindex(0, 1000, "");
            Assert.Equal(7, alle.Count);     // 1505 und 1917 fallen heraus

            List<int> gross = a.Anzeigeindex(0, 100000, "");
            Assert.Equal(9, gross.Count);

            List<int> exclusiv = a.Anzeigeindex(0, 100000, "exclusiv");
            Assert.Equal(6, exclusiv.Count);
            Assert.All(exclusiv, i => Assert.Contains("exclusiv", a.Saetze[i].Name));

            // Zwei Begriffe wirken als UND - hier ueber Name UND Firma.
            List<int> beides = a.Anzeigeindex(0, 100000, "exclusiv vaillant");
            Assert.Equal(6, beides.Count);

            Assert.Empty(a.Anzeigeindex(0, 100000, "wolf"));
        }

        // ==================================================================
        // 4 — Vorpruefen und Ausfuehren
        // ==================================================================

        /// <summary>
        /// Ein unbekannter Bezeichner ist kein NAMENSkonflikt. Ob daraus „Neu" oder
        /// „InhaltsGleich" wird, entscheidet der Bestand: Der Vaillant-Ausschnitt
        /// steht als Katalog schon in der Testdatenbank, seine Kennwerte treffen
        /// deshalb einen vorhandenen Satz — genau der Fall, fuer den der
        /// Konfliktdialog „trotzdem importieren" anbietet (Abnahmepunkt 5).
        /// </summary>
        [Fact]
        public void EinUnbekannterBezeichnerIstKeinNamenskonflikt()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);
            a.Lesen(Probe("pufferspeicher_vaillant.vdi"));

            List<ImportPruefung> p = a.Vorpruefen(new[] { 0, 1, 2 },
                i => "W13 Ablauf Probe " + i.ToString(CultureInfo.InvariantCulture));

            Assert.Equal(3, p.Count);
            Assert.DoesNotContain(p, x => x.Befund == ImportBefund.NameVorhanden);
            Assert.DoesNotContain(p, x => x.Befund == ImportBefund.Identisch);
            Assert.All(p, x => Assert.False(x.NameDoppeltInAuswahl));
        }

        /// <summary>
        /// Ohne einen einzigen Befund ausser „Neu" bleibt der Konfliktdialog weg,
        /// und die Entscheidungsliste baut sich von selbst — der Weg, den die drei
        /// Masken bisher je einzeln nachbauten.
        /// </summary>
        [Fact]
        public void OhneKonfliktBautSichDieEntscheidungslisteVonSelbst()
        {
            var neu = new List<ImportPruefung>
            {
                new ImportPruefung { Kandidat = new ImportKandidat { Name = "A", Tag = 0 } },
                new ImportPruefung { Kandidat = new ImportKandidat { Name = "B", Tag = 1 } }
            };
            Assert.False(KatalogImportAblauf.Konfliktbehaftet(neu));

            List<KonfliktEntscheidung> e = KatalogImportAblauf.AllesImportieren(neu);
            Assert.Equal(2, e.Count);
            Assert.All(e, x => Assert.Equal(KonfliktAktion.Importieren, x.Aktion));

            // Ein einziger nicht-neuer Befund oder ein doppelter Name genuegt.
            neu[1].Befund = ImportBefund.InhaltsGleich;
            Assert.True(KatalogImportAblauf.Konfliktbehaftet(neu));

            neu[1].Befund = ImportBefund.Neu;
            neu[1].NameDoppeltInAuswahl = true;
            Assert.True(KatalogImportAblauf.Konfliktbehaftet(neu));

            Assert.False(KatalogImportAblauf.Konfliktbehaftet(null));
            Assert.Empty(KatalogImportAblauf.AllesImportieren(null));
        }

        /// <summary>
        /// <b>Der Bezeichner kommt aus dem Feld</b> (Abweichung A-4): Dieselbe Zeile,
        /// zwei verschiedene Namen — die Vorpruefung prueft, was gespeichert wuerde.
        /// Ohne den Delegaten zaehlt der Name aus der Datei.
        /// </summary>
        [Fact]
        public void DerBezeichnerDerVorpruefungKommtAusDemFeld()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Solarkollektoren);
            a.Lesen(Probe("solarkollektoren_vaillant.vdi"));

            List<ImportPruefung> ohne = a.Vorpruefen(new[] { 0 });
            Assert.Equal("auroTHERM VFK 145/3 H", ohne[0].Kandidat.Name);

            List<ImportPruefung> mit = a.Vorpruefen(new[] { 0 }, _ => "W13 Handkorrektur");
            Assert.Equal("W13 Handkorrektur", mit[0].Kandidat.Name);

            // Ein leerer Feldwert faellt auf den Dateinamen zurueck.
            List<ImportPruefung> leer = a.Vorpruefen(new[] { 0 }, _ => "");
            Assert.Equal("auroTHERM VFK 145/3 H", leer[0].Kandidat.Name);
        }

        /// <summary>
        /// Der Satzindex reist als <c>Tag</c> mit — daran findet
        /// <see cref="KatalogImportAblauf.Ausfuehren"/> den Satz wieder.
        /// </summary>
        [Fact]
        public void DerSatzindexReistAlsTagMit()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);
            a.Lesen(Probe("pufferspeicher_vaillant.vdi"));

            List<ImportPruefung> p = a.Vorpruefen(new[] { 4, 2, 7 });

            Assert.Equal(new object[] { 4, 2, 7 }, p.Select(x => x.Kandidat.Tag).ToArray());
        }

        /// <summary>
        /// <b>Der ganze Weg</b>: lesen, vorpruefen, ausfuehren — und der Satz steht
        /// im Katalog. Danach derselbe Lauf ein zweites Mal: jetzt „Identisch",
        /// und mit „Auslassen" wird nichts geschrieben (Abnahmepunkt 2 des
        /// Dublettenkonzepts).
        /// </summary>
        [Fact]
        public void DerGanzeWegSchreibtEinmalUndBeimZweitenMalNichtsMehr()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);
            a.Lesen(Probe("pufferspeicher_vaillant.vdi"));

            Func<int, string> name = i => "W13 Ablauf " + i.ToString(CultureInfo.InvariantCulture);
            var markiert = new[] { 0, 1 };

            List<ImportPruefung> p1 = a.Vorpruefen(markiert, name);
            ImportBilanz b1 = a.Ausfuehren(markiert.Length,
                KatalogImportAblauf.AllesImportieren(p1), name);

            Assert.Equal(2, b1.Markiert);
            Assert.Equal(2, b1.Gespeichert);
            Assert.Equal(0, b1.Fehler);
            Assert.True(b1.EtwasGeschrieben);

            Assert.True(new PufferSpStammCtrl().Exists("W13 Ablauf 0"));
            Assert.Equal(303, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT Gesamtvolumen FROM [Tab_Pufferspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Ablauf 0"))));

            // Zweiter Durchlauf: identisch, Vorbelegung Auslassen, kein neuer Satz.
            List<ImportPruefung> p2 = a.Vorpruefen(markiert, name);
            Assert.All(p2, x => Assert.Equal(ImportBefund.Identisch, x.Befund));
            Assert.True(KatalogImportAblauf.Konfliktbehaftet(p2));

            var auslassen = p2.Select(x => new KonfliktEntscheidung
            {
                Pruefung = x,
                Aktion = KonfliktAktion.Auslassen
            }).ToList();
            ImportBilanz b2 = a.Ausfuehren(markiert.Length, auslassen, name);

            Assert.Equal(2, b2.Duplikat);
            Assert.Equal(0, b2.Gespeichert);
            Assert.False(b2.EtwasGeschrieben);
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [Tab_Pufferspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Ablauf 0"))));
        }

        /// <summary>
        /// <b>Ueberschreiben und Umbenennen</b> (Abnahmepunkte 3 und 5): Der
        /// ueberschriebene Satz behaelt Id und Bezeichner und bekommt die neuen
        /// Importfelder; der umbenannte kommt als ZWEITER Satz dazu.
        /// </summary>
        [Fact]
        public void UeberschreibenBehaeltDieIdUndUmbenennenLegtEinenZweitenSatzAn()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);
            a.Lesen(Probe("pufferspeicher_vaillant.vdi"));

            Func<int, string> name = _ => "W13 Ueberschreiben";

            // Anlegen mit den Werten von Satz 0 (Volumen 303)
            List<ImportPruefung> p0 = a.Vorpruefen(new[] { 0 }, name);
            a.Ausfuehren(1, KatalogImportAblauf.AllesImportieren(p0), name);

            int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM [Tab_Pufferspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Ueberschreiben")));
            Assert.True(id > 0);

            // Jetzt Satz 4 (Volumen 778) unter demselben Namen: Namenskonflikt.
            List<ImportPruefung> p1 = a.Vorpruefen(new[] { 4 }, name);
            Assert.Equal(ImportBefund.NameVorhanden, p1[0].Befund);

            ImportBilanz b = a.Ausfuehren(1, new List<KonfliktEntscheidung>
            {
                new KonfliktEntscheidung { Pruefung = p1[0], Aktion = KonfliktAktion.Ueberschreiben }
            }, name);

            Assert.Equal(1, b.Ueberschrieben);
            Assert.Equal(id, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM [Tab_Pufferspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Ueberschreiben"))));
            Assert.Equal(778, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT Gesamtvolumen FROM [Tab_Pufferspeicher_STAMM] WHERE ID = ?",
                new DbParam("?", id))));

            // Umbenennen legt einen zweiten Satz an und zaehlt eigens.
            List<ImportPruefung> p2 = a.Vorpruefen(new[] { 4 }, name);
            ImportBilanz b2 = a.Ausfuehren(1, new List<KonfliktEntscheidung>
            {
                new KonfliktEntscheidung
                {
                    Pruefung = p2[0],
                    Aktion = KonfliktAktion.Umbenennen,
                    NeuerName = "W13 Ueberschreiben (2)"
                }
            }, name);

            Assert.Equal(1, b2.Umbenannt);
            Assert.Equal(0, b2.Gespeichert);
            Assert.True(new PufferSpStammCtrl().Exists("W13 Ueberschreiben (2)"));
        }

        /// <summary>
        /// Der Schreibweg ist TRANSAKTIONAL (W13.0e): Ein Bezeichner, den ein
        /// anderer Weg schon angelegt hat, wird als Duplikat gemeldet — und der
        /// Katalog behaelt genau einen Satz.
        /// </summary>
        [Fact]
        public void DerSchreibwegMeldetEinDuplikatStattEsZweimalAnzulegen()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Pufferspeicher);
            a.Lesen(Probe("pufferspeicher_vaillant.vdi"));

            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, a.Saetze[0].Anlegen("W13 Doppelt"));
            Assert.Equal(VdiUebernahmeErgebnis.Duplikat, a.Saetze[1].Anlegen("W13 Doppelt"));

            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [Tab_Pufferspeicher_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 Doppelt"))));
        }

        /// <summary>
        /// <b>Die Waermepumpe schreibt drei Tabellen in EINER Transaktion</b>
        /// (W13.0e, Befund W13-B33): Stammsatz, Heizkennlinien und — hier keine —
        /// Kuehlkennlinien.
        /// </summary>
        [Fact]
        public void DerWaermepumpenimportSchreibtStammsatzUndKennlinienZusammen()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Waermepumpe);
            a.Lesen(Probe("waermepumpen_hoval.vdi"));

            Assert.Equal(VdiUebernahmeErgebnis.Gespeichert, a.Saetze[0].Anlegen("W13 WP Probe"));

            int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM [Tab_WP_STAMM] WHERE Bezeichner = ?",
                new DbParam("?", "W13 WP Probe")));
            Assert.True(id > 0);

            // Vier Kennlinienkoepfe mit je vier Wertzeilen (nur Volllast).
            Assert.Equal(16, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [Tab_Kenndaten_STAMM] WHERE ID_WP = ?", new DbParam("?", id))));
            Assert.Equal(0, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [Tab_Kenndaten_Kuehlung_STAMM] WHERE ID_WP = ?", new DbParam("?", id))));

            // Der Regelungstext kommt aus der Stufenzahl 0 -> "stetig".
            Assert.Equal(WaermepumpeImportSatz.REGELUNG_STETIG, Convert.ToString(
                DataRepository.ExecuteScalar("SELECT Regelung FROM [Tab_WP_STAMM] WHERE ID = ?",
                    new DbParam("?", id))));
            // Nennleistung 7.9 wird ABGESCHNITTEN, nicht gerundet.
            Assert.Equal(7, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT Nennleistung FROM [Tab_WP_STAMM] WHERE ID = ?", new DbParam("?", id))));
        }

        /// <summary>
        /// Der Heizkessel rechnet: Brennstoffdeckel, Oel-/Gas-Weiche und der
        /// Wirkungsgrad durch 100. Der Vaillant-Ausschnitt fuehrt Erdgas (Index 3),
        /// der Buderus-Ausschnitt Heizoel (Index 9).
        /// </summary>
        [Fact]
        public void DerHeizkesselVerteiltDenWirkungsgradAufGasUndOel()
        {
            if (!_db.Vorhanden) return;

            KatalogImportAblauf gas = Ablauf(KatalogImportArt.Heizkessel);
            gas.Lesen(Probe("heizkessel_vaillant.vdi"));
            var g = (HeizkesselImportSatz)gas.Saetze[0];
            HeizkesselModel mg = g.NachModell("Probe Gas", g.Deckel);

            Assert.Equal(3, mg.Brennstoff);
            Assert.Equal(0.874, mg.Wirkungsgrad_Gas, 9);
            Assert.Equal(0.0, mg.Wirkungsgrad_Oel);
            Assert.Equal(19.3, mg.Ptherm, 9);
            Assert.Equal("Brennwert-Kessel", mg.Beschreibung);

            KatalogImportAblauf oel = Ablauf(KatalogImportArt.Heizkessel);
            oel.Lesen(Probe("heizkessel_buderus.vdi"));
            var o = (HeizkesselImportSatz)oel.Saetze[0];
            HeizkesselModel mo = o.NachModell("Probe Oel", o.Deckel);

            Assert.Equal(9, mo.Brennstoff);
            Assert.Equal(0.913, mo.Wirkungsgrad_Oel, 9);
            Assert.Equal(0.0, mo.Wirkungsgrad_Gas);
            Assert.Equal(14.0, mo.CO2, 9);
            Assert.Equal(95.0, mo.NOx, 9);
            Assert.Equal(15.0, mo.CO, 9);
        }

        /// <summary>
        /// Der Brennstoffdeckel greift: Ein Index oberhalb der Tabelle wird auf
        /// deren <c>MAX(ID)</c> gezogen — der Grund, warum der alte harte Deckel
        /// (&gt; 22 → 23) fiel.
        /// </summary>
        [Fact]
        public void DerBrennstoffdeckelKommtAusDerTabelle()
        {
            if (!_db.Vorhanden) return;

            int max = HeizkesselImportSatz.MaxBrennstoff();
            Assert.True(max >= 23, "Die Brennstofftabelle sollte mindestens bis Fernwaerme reichen.");

            KatalogImportAblauf a = Ablauf(KatalogImportArt.Heizkessel);
            a.Lesen(Probe("heizkessel_vaillant.vdi"));
            var satz = (HeizkesselImportSatz)a.Saetze[0];

            satz.Deckel = 2;
            Assert.Equal(2, satz.NachModell("x", 2).Brennstoff);
            satz.Deckel = max;
            Assert.Equal(3, satz.NachModell("x", max).Brennstoff);
        }

        // ==================================================================
        // 5 — Die Sammelmeldung aus der Bilanz
        // ==================================================================

        /// <summary>
        /// Die Sammelmeldung liest jetzt aus dem Katalog (W13.0f) — der WORTLAUT
        /// bleibt der des Bestands.
        /// </summary>
        [Fact]
        public void DieSammelmeldungAusDerBilanzIstDieselbeWieAusDenZahlen()
        {
            DeutscheOberflaeche(() =>
            {
                var b = new ImportBilanz
                {
                    Markiert = 10, Gespeichert = 1, Duplikat = 4,
                    Fehler = 5, Ueberschrieben = 2, Umbenannt = 3
                };

                Assert.Equal(VdiAuswahlFilter.LadeMeldung(1, 10, 4, 5, 2, 3),
                             VdiAuswahlFilter.LadeMeldung(b));

                string n = Environment.NewLine;
                Assert.Equal("1 von 10 Einträgen geladen." + n
                             + "Überschrieben: 2" + n
                             + "Unter neuem Namen: 3" + n
                             + "Bereits eingelesen (übersprungen): 4" + n
                             + "Fehlgeschlagen: 5",
                             VdiAuswahlFilter.LadeMeldung(b));

                Assert.Equal("", VdiAuswahlFilter.LadeMeldung(null));
            });
        }

        /// <summary>Auf Englisch stehen dieselben Zahlen in den englischen Texten.</summary>
        [Fact]
        public void DieSammelmeldungGibtEsAuchAufEnglisch()
        {
            EnglischeOberflaeche(() =>
            {
                string n = Environment.NewLine;
                Assert.Equal("1 of 5 entries loaded." + n + "Failed: 2",
                             VdiAuswahlFilter.LadeMeldung(1, 5, 0, 2));
            });
        }

        // ==================================================================
        // Sprache pinnen (Regel seit W8)
        // ==================================================================

        private static void DeutscheOberflaeche(Action fall) => MitSprache("de-DE", fall);

        private static void EnglischeOberflaeche(Action fall) => MitSprache("en-US", fall);

        private static void MitSprache(string kuerzel, Action fall)
        {
            CultureInfo vorher = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(kuerzel);
                fall();
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = vorher;
            }
        }
    }
}
