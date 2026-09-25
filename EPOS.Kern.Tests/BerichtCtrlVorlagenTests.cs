using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Verdrahtung des Berichts mit den Vorlagen</b> (Konzept Berichtsvorlagen 4.10, 6.8, 10.2,
    /// 10.3; Etappe BV-E1, Teil B1a): <see cref="BerichtCtrl.ErzeugeWordLauf"/> wählt die Vorlage über
    /// den <see cref="BerichtsvorlagenCtrl"/>, liest sie einmal und füllt sie; ohne Standardvorlage der
    /// benannte alte Weg; <see cref="BerichtCtrl.PruefeVorStart"/> mit Rückfrage, zweitem Einstieg und
    /// denselben Bytes für Prüfung und Lauf; <see cref="BerichtCtrl.Laufmeldung"/> in beiden Sprachen.
    ///
    /// <para><b>Rahmen.</b> Pfade und Einstellungen werden HEREINGEREICHT (<see cref="Probepfade"/>,
    /// <see cref="FluechtigeEinstellungen"/>): Die Standardvorlage und die Stilvorlage kommen als Kopie
    /// aus dem Repository in einen Temp-Ordner, der als Auslieferungsordner gilt; kein
    /// <c>Dienste.*</c> wird getauscht. Die Testdatenbank braucht nur der Fall mit gespeicherter
    /// Konfiguration und der volle Standardbericht; die übrigen füllen Bausteine ohne Datenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtCtrlVorlagenTests : IDisposable
    {
        private const string FIRMA = "Probe GmbH";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-b1a");
        private readonly string _dokumente;
        private readonly string _app;
        private readonly string _quellen;
        private readonly string _ziel;
        private readonly FluechtigeEinstellungen _einstellungen = new FluechtigeEinstellungen();
        private readonly BerichtsvorlagenCtrl _vorlagen;
        private readonly BerichtCtrl _ctrl;
        private readonly byte[] _standard;

        public BerichtCtrlVorlagenTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
            _dokumente = Directory.CreateDirectory(Path.Combine(_wurzel, "Dokumente")).FullName;
            _app = Directory.CreateDirectory(Path.Combine(_wurzel, "App", "Vorlagen")).FullName;
            _quellen = Directory.CreateDirectory(Path.Combine(_wurzel, "Quellen")).FullName;
            _ziel = Directory.CreateDirectory(Path.Combine(_wurzel, "Berichte")).FullName;
            _standard = Repovorlage(BerichtsvorlageDateiWacheTests.STANDARD);
            if (_standard != null) File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD), _standard);
            _vorlagen = new BerichtsvorlagenCtrl(new Probepfade(_dokumente, _app), _einstellungen, () => FIRMA);
            _ctrl = new BerichtCtrl(_vorlagen);
        }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_wurzel);
        }

        /// <summary>Pfade, deren Dokumente- und Auslieferungsordner der Fall bestimmt.</summary>
        private sealed class Probepfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Probepfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }

        // =====================================================================
        //  Lauf mit der Standardvorlage
        // =====================================================================

        /// <summary>
        /// Die Standardvorlage über den Controller: die Datei entsteht unter dem gewohnten Namen im
        /// Zielordner, der Validator hat nichts, kein Platzhalter bleibt stehen, die Firma steht in der
        /// Fußzeile — und die Laufmeldung nennt die Vorlage samt Grund. Der Name bleibt der alte, ein
        /// zweiter Lauf weicht auf <c>_2</c> aus.
        /// </summary>
        [Fact]
        public void Standardvorlage_ergibt_einen_gueltigen_Bericht_ohne_Unbekannte_und_die_Laufmeldung_nennt_sie()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            konfig.ZielOrdner = _ziel;

            Berichtslauf lauf = _ctrl.ErzeugeWordLauf(daten, konfig);

            string datum = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.Equal(Path.Combine(_ziel, "Stammprojekt_Bericht_" + datum + ".docx"), lauf.Pfad);
            Assert.True(File.Exists(lauf.Pfad));
            Assert.Empty(Validierungsfehler(lauf.Pfad));
            Assert.False(lauf.IstRueckfall);
            Assert.Equal(Vorlagenwahlgrund.Standard, lauf.Grund);
            Assert.Equal(BerichtsvorlagenCtrl.ID_STANDARD, lauf.VorlageId);
            Assert.Equal(R.BV_VORLAGEN_STANDARD, lauf.VorlageName);
            Assert.NotNull(lauf.Fuellergebnis);
            Assert.Empty(lauf.Unbekannte);
            Assert.Empty(lauf.Rueckfaelle);
            Assert.Equal("", lauf.Rueckfall);
            Assert.Equal(Vorlagenpruefer.Pruefsumme(_standard), lauf.Pruefsumme);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(lauf.Pfad, false))
            {
                string fuss = string.Concat(doc.MainDocumentPart.FooterParts.Select(f => f.Footer.InnerText));
                Assert.Contains(FIRMA, fuss, StringComparison.Ordinal);
                Assert.DoesNotContain("{{", doc.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
            }

            string meldung = BerichtCtrl.Laufmeldung(lauf, false);
            _ausgabe.WriteLine(meldung);
            Assert.StartsWith("Word-Vorlage: „Standard (EPOS-Plan)“ (Standardvorlage)", meldung, StringComparison.Ordinal);
            Assert.DoesNotContain("Rückfall", meldung, StringComparison.Ordinal);
            Assert.DoesNotContain("gelb", meldung, StringComparison.Ordinal);
            Assert.Equal(KiMeldungskennung.BV_LAUF_VORLAGE, BerichtCtrl.Laufabschnitte(lauf, false)[0].Kennung);

            // Die alte Signatur liefert den Pfad — und weicht dem ersten Bericht aus.
            string zweiter = _ctrl.ErzeugeWord(daten, konfig);
            Assert.Equal(Path.Combine(_ziel, "Stammprojekt_Bericht_" + datum + "_2.docx"), zweiter);
        }

        // =====================================================================
        //  Abweichung, Vorgabe, fehlende Vorlage
        // =====================================================================

        /// <summary>
        /// Abweichung auf eine eigene Vorlage — eine Kopie der Beispielvorlage: Deckblatt gefüllt, die
        /// acht <c>{{kapitel.*}}</c> kennt Katalog v1 nicht, sie stehen gelb im Bericht und in der
        /// Laufmeldung; die zwei Kommentare sind entfernt und genannt.
        /// </summary>
        [Fact]
        public void Abweichung_auf_eine_eigene_Vorlage_laesst_die_Kapitel_gelb_stehen_und_nennt_sie()
        {
            byte[] beispiel = Repovorlage(BerichtsvorlageDateiWacheTests.BEISPIEL);
            if (beispiel == null) return;
            Vorlageneintrag eigen = Hinzu(BerichtsvorlageDateiWacheTests.BEISPIEL, beispiel);
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            Berichtslauf lauf = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);

            Assert.Empty(Validierungsfehler(lauf.Pfad));
            Assert.Equal(Vorlagenwahlgrund.Abweichung, lauf.Grund);
            Assert.Equal("eigen:" + BerichtsvorlageDateiWacheTests.BEISPIEL, lauf.VorlageId);
            Assert.Equal("Berichtsvorlage_Beispiel", lauf.VorlageName);
            Assert.Equal(8, lauf.Unbekannte.Count);
            Assert.All(lauf.Unbekannte, b => Assert.StartsWith("{{kapitel.", b.Normalform, StringComparison.Ordinal));
            Assert.Equal(2, lauf.EntfernteKommentare);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(lauf.Pfad, false))
            {
                List<Run> gelb = doc.MainDocumentPart.Document.Body.Descendants<Run>()
                    .Where(r => r.RunProperties?.Highlight?.Val?.Value == HighlightColorValues.Yellow).ToList();
                Assert.Equal(8, gelb.Count);
            }

            string meldung = BerichtCtrl.Laufmeldung(lauf, false);
            _ausgabe.WriteLine(meldung);
            Assert.Contains("Word-Vorlage: „Berichtsvorlage_Beispiel“ (Für dieses Projekt gewählt)", meldung, StringComparison.Ordinal);
            Assert.Contains("Nicht ersetzte Platzhalter, im Bericht gelb markiert: 8", meldung, StringComparison.Ordinal);
            Assert.Contains("• {{kapitel.projekt|ohne titel}} – unbekannter Schlüssel; Rumpf, Absatz ", meldung, StringComparison.Ordinal);
            Assert.Contains("Kommentare der Vorlage entfernt: 2", meldung, StringComparison.Ordinal);
            Assert.Equal(new[] { KiMeldungskennung.BV_LAUF_VORLAGE, KiMeldungskennung.BV_LAUF_UNBEKANNT, KiMeldungskennung.BV_LAUF_LEER,
                                 KiMeldungskennung.BV_LAUF_KOMMENTARE },
                         BerichtCtrl.Laufabschnitte(lauf, false).Select(a => a.Kennung));
        }

        /// <summary>
        /// Eine gespeicherte, aber fehlende Vorlage (Abweichung aus der Datenbank): ohne Vorgabe die
        /// Standardvorlage, mit Vorgabe die Vorgabe — beides mit der Meldung „nicht vorhanden – … verwendet“
        /// im Rückfall der Laufmeldung.
        /// </summary>
        [Fact]
        public void Fehlende_gespeicherte_Vorlage_nimmt_Vorgabe_oder_Standard_und_nennt_den_Rueckfall()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = DataRepository.ExecuteInsertAndGetId("INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                                                          new[] { new DbParam("@name", "Vorlagenwahl BV-E1 B1a") });
            Assert.True(id > 0);
            BerichtsKonfiguration gespeichert = Konfig();
            gespeichert.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
            gespeichert.VorlageWordDatei = "Angebot.docx";
            Assert.True(_ctrl.Speichere(id, gespeichert));
            BerichtsKonfiguration konfig = _ctrl.Lade(id);
            Assert.Equal("Angebot.docx", konfig.VorlageWordDatei);

            Berichtslauf ohneVorgabe = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);
            Assert.Equal(Vorlagenwahlgrund.Standard, ohneVorgabe.Grund);
            Assert.Equal(R.BV_VORLAGEN_STANDARD, ohneVorgabe.VorlageName);
            Assert.Equal("eigen:Angebot.docx", ohneVorgabe.Wahl.FehlendeId);
            Assert.Equal(new[] { "„Angebot“ nicht vorhanden – „Standard (EPOS-Plan)“ verwendet" }, ohneVorgabe.Rueckfaelle);
            string meldung = BerichtCtrl.Laufmeldung(ohneVorgabe, false);
            Assert.Contains("Rückfall bei der Vorlagenwahl:\r\n• „Angebot“ nicht vorhanden – „Standard (EPOS-Plan)“ verwendet",
                            meldung, StringComparison.Ordinal);

            Vorlageneintrag buero = Hinzu("Buero.docx", Probevorlagen.AusAbsaetzen("Büro {{projekt.name}}"));
            _vorlagen.SetzeVorgabeWord(buero);
            Berichtslauf mitVorgabe = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);
            Assert.Equal(Vorlagenwahlgrund.Vorgabe, mitVorgabe.Grund);
            Assert.Equal("Buero", mitVorgabe.VorlageName);
            Assert.Equal(new[] { "„Angebot“ nicht vorhanden – „Buero“ verwendet" }, mitVorgabe.Rueckfaelle);
            Assert.Contains("Word-Vorlage: „Buero“ (Ihre Vorgabe)", BerichtCtrl.Laufmeldung(mitVorgabe, false), StringComparison.Ordinal);
            Assert.Equal("Büro Stammprojekt", ErsterAbsatz(mitVorgabe.Pfad));
        }

        /// <summary>
        /// Ohne Standardvorlage entsteht der Bericht auf dem bisherigen Weg — mit der Stilvorlage
        /// <c>Berichtsvorlage.docx</c>, fehlt auch sie, mit den eingebauten Formaten; beides benannt.
        /// </summary>
        [Fact]
        public void Ohne_Standardvorlage_entsteht_der_Bericht_auf_dem_alten_Weg_und_der_Rueckfall_ist_benannt()
        {
            byte[] stil = Repovorlage(BerichtsvorlageDateiWacheTests.STILVORLAGE);
            if (stil == null) return;
            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD));
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_RUECKFALL), stil);

            Berichtslauf mitStil = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), Konfig());
            Assert.True(mitStil.IstRueckfall);
            Assert.Null(mitStil.Fuellergebnis);
            Assert.Equal("", mitStil.Pruefsumme);
            Assert.Equal(Vorlagenwahlgrund.Rueckfall, mitStil.Grund);
            Assert.Equal(BerichtsvorlagenCtrl.DATEI_RUECKFALL, mitStil.VorlageName);
            Assert.Equal(new[] { "Die Standardvorlage Berichtsvorlage_Standard.docx fehlt – verwendet wird Berichtsvorlage.docx" },
                         mitStil.Rueckfaelle);
            Assert.True(File.Exists(mitStil.Pfad));
            Assert.Empty(Validierungsfehler(mitStil.Pfad));
            string meldung = BerichtCtrl.Laufmeldung(mitStil, false);
            _ausgabe.WriteLine(meldung);
            Assert.StartsWith("Word-Vorlage: „Berichtsvorlage.docx“ (Rückfall – die Standardvorlage fehlt)", meldung, StringComparison.Ordinal);
            Assert.Contains("verwendet wird Berichtsvorlage.docx", meldung, StringComparison.Ordinal);

            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_RUECKFALL));
            Berichtslauf eingebaut = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), Konfig());
            Assert.True(eingebaut.IstRueckfall);
            Assert.Equal(R.BV_LAUF_EINGEBAUT, eingebaut.VorlageName);
            Assert.Equal(new[] { "Die Standardvorlage Berichtsvorlage_Standard.docx fehlt – der Bericht entsteht mit den eingebauten Formaten" },
                         eingebaut.Rueckfaelle);
            Assert.True(File.Exists(eingebaut.Pfad));
            Assert.StartsWith("Word-Vorlage: „Eingebaute Formate“", BerichtCtrl.Laufmeldung(eingebaut, false), StringComparison.Ordinal);
        }

        /// <summary>
        /// Eine eigene Vorlage, die kein Word-Dokument ist, bricht den Bericht nicht ab: Die
        /// Standardvorlage springt ein, der Rückfall nennt den Grund.
        /// </summary>
        [Fact]
        public void Eine_nicht_fuellbare_eigene_Vorlage_faellt_benannt_auf_die_Standardvorlage()
        {
            if (_standard == null) return;
            Vorlageneintrag kaputt = Hinzu("Kaputt.docx", Encoding.UTF8.GetBytes("kein Word-Dokument"));
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, kaputt);

            Berichtslauf lauf = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);

            Assert.False(lauf.IstRueckfall);
            Assert.Equal(R.BV_VORLAGEN_STANDARD, lauf.VorlageName);
            Assert.Equal(new[] { "„Kaputt“ ließ sich nicht füllen: " + WordVorlagentexte.VORLAGE_UNLESBAR }, lauf.Rueckfaelle);
            Assert.Empty(Validierungsfehler(lauf.Pfad));
        }

        // =====================================================================
        //  Leerwerte
        // =====================================================================

        /// <summary>
        /// Leere Platzhalter je Schlüssel zusammengefasst (Konzept 4.10): an allen Stellen leer, an einer
        /// Stelle leer — die Laufmeldung nennt die Zahl und die Stellen.
        /// </summary>
        [Fact]
        public void Leere_Platzhalter_werden_je_Schluessel_zusammengefasst()
        {
            Vorlageneintrag eigen = Hinzu("Leer.docx", Probevorlagen.AusAbsaetzen(
                "Kunde {{projekt.kunde}} / {{projekt.kunde}}", "Region {{projekt.klimaregion}}", "Name {{projekt.name}}"));
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            Berichtslauf lauf = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);

            Assert.Equal(2, lauf.Stellen["projekt.kunde"]);
            Assert.Equal(new[] { "{{projekt.klimaregion}}: leer", "{{projekt.kunde}}: leer bei 2 von 2 Stellen" },
                         lauf.LeereZusammengefasst);
            string meldung = BerichtCtrl.Laufmeldung(lauf, false);
            Assert.Contains("Platzhalter ohne Wert, mit dem Leerwert gefüllt: 2\r\n• {{projekt.klimaregion}}: leer\r\n" +
                            "• {{projekt.kunde}}: leer bei 2 von 2 Stellen", meldung, StringComparison.Ordinal);
            Assert.DoesNotContain("projekt.name", meldung, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Vorprüfung
        // =====================================================================

        /// <summary>
        /// Fehler in der gewählten Vorlage: Die Vorprüfung verlangt die erweiterte Rückfrage mit Zahl
        /// der Projekte, Name der Vorlage, Befund und drei Wegen. „Mit meiner Vorlage“ lässt die
        /// unbekannte Stelle gelb stehen, „Mit Standardvorlage“ nennt die ersetzte.
        /// </summary>
        [Fact]
        public void Vorpruefung_mit_Fehlern_braucht_die_Rueckfrage_mit_drei_Wegen()
        {
            if (_standard == null) return;
            Vorlageneintrag eigen = Hinzu("Eigen.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kundename}}"));
            BerichtsKonfiguration konfig = Konfig();
            konfig.VariantenIds.Add(4711);
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            Startbefund start = _ctrl.PruefeVorStart(konfig, false, 1);

            Assert.True(start.HatFehler);
            Assert.True(start.BrauchtRueckfrage);
            Assert.True(start.StandardAngeboten);
            Assert.True(start.KannGewaehlteFuellen);
            Assert.False(start.SpracheAbweichend);
            Assert.False(start.SichtUnpassend);
            Assert.False(start.OhneWirtschaftlichkeit);
            Assert.Equal(2, start.AnzahlProjekte);
            Berichtsmeldung befund = Assert.Single(start.Befunde);
            Assert.Equal(KiMeldungskennung.VF_PRUEF_UNBEKANNT, befund.Kennung);
            Assert.StartsWith("Unbekannter Platzhalter {{projekt.kundename}} (", befund.Text, StringComparison.Ordinal);
            _ausgabe.WriteLine(start.Rueckfrage);
            Assert.StartsWith("Für diesen Bericht werden 2 Projekt(e) neu simuliert und in die Word-Vorlage „Eigen“ gefüllt.",
                              start.Rueckfrage, StringComparison.Ordinal);
            Assert.Contains("Die Vorprüfung meldet:\r\n• Unbekannter Platzhalter {{projekt.kundename}}", start.Rueckfrage, StringComparison.Ordinal);
            Assert.Contains(R.BV_START_GELB, start.Rueckfrage, StringComparison.Ordinal);
            Assert.EndsWith(R.BV_START_FRAGE, start.Rueckfrage, StringComparison.Ordinal);
            Assert.Equal("Mit meiner Vorlage", start.WegGewaehlt);
            Assert.Equal("Mit Standardvorlage", start.WegStandard);
            Assert.Equal("Abbrechen", start.WegAbbrechen);

            Berichtslauf gewaehlt = _ctrl.ErzeugeWord(Berichtsdatenproben.Gruppendaten(2), konfig, start, Startweg.Gewaehlt);
            Assert.Equal("Eigen", gewaehlt.VorlageName);
            Assert.Equal("{{projekt.kundename}}", Assert.Single(gewaehlt.Unbekannte).Normalform);

            Berichtslauf standard = _ctrl.ErzeugeWord(Berichtsdatenproben.Gruppendaten(2), konfig, start, Startweg.Standard);
            Assert.Equal(R.BV_VORLAGEN_STANDARD, standard.VorlageName);
            Assert.Equal(Vorlagenwahlgrund.Standard, standard.Grund);
            Assert.Empty(standard.Unbekannte);
            Assert.Equal(new[] { "„Eigen“ für diesen Bericht durch „Standard (EPOS-Plan)“ ersetzt" }, standard.Rueckfaelle);
            Assert.NotEqual(gewaehlt.Pfad, standard.Pfad);

            // Die Standardvorlage selbst braucht keine Rückfrage.
            Startbefund ohne = _ctrl.PruefeVorStart(Konfig(), false, 1);
            Assert.False(ohne.BrauchtRueckfrage);
            Assert.Equal("", ohne.Rueckfrage);
            Assert.Empty(ohne.Befunde);
            Assert.True(ohne.Pruefbefund.OhneBefund, Probevorlagen.Liste(ohne.Pruefbefund));
        }

        /// <summary>
        /// Abweichende Sprache und unpassende Sicht verlangen die Rückfrage auch ohne Fehler; die Sicht
        /// folgt der Regel 4.7 (<c>stand.b</c> allein in Sicht 1 bei genau einer Variante).
        /// </summary>
        [Fact]
        public void Abweichende_Sprache_und_unpassende_Sicht_verlangen_die_Rueckfrage()
        {
            Vorlageneintrag englisch = Hinzu("English.docx",
                Probevorlagen.Baue(b => b.Absatz("{{projekt.name}}").Eigenschaften(null, "en")));
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, englisch);

            Startbefund deutsch = _ctrl.PruefeVorStart(konfig, false, 1);
            Assert.False(deutsch.HatFehler);
            Assert.True(deutsch.SpracheAbweichend);
            Assert.True(deutsch.BrauchtRueckfrage);
            Assert.Equal(KiMeldungskennung.VF_PRUEF_SPRACHE, Assert.Single(deutsch.Befunde).Kennung);
            Assert.False(_ctrl.PruefeVorStart(konfig, true, 1).BrauchtRueckfrage);

            Vorlageneintrag paar = Hinzu("Paar.docx", Probevorlagen.AusAbsaetzen("A {{stand.a.kennzahl.eff.jaz}}"));
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, paar);
            Startbefund sicht1 = _ctrl.PruefeVorStart(konfig, false, 1);
            Assert.True(sicht1.SichtUnpassend);
            Assert.Contains(sicht1.Befunde, b => b.Kennung == KiMeldungskennung.BV_START_SICHT && b.Text == R.BV_START_SICHT);
            Assert.False(_ctrl.PruefeVorStart(konfig, false, 2).SichtUnpassend);

            Assert.True(BerichtCtrl.NutztPaarvergleich(new[] { "stand.a.kennzahl.eff.jaz" }, 1));
            Assert.True(BerichtCtrl.NutztPaarvergleich(new[] { "stand.b.kennzahl.eff.jaz" }, 2));
            Assert.False(BerichtCtrl.NutztPaarvergleich(new[] { "stand.b.kennzahl.eff.jaz" }, 1));
            Assert.False(BerichtCtrl.NutztPaarvergleich(new[] { "stand.anzahl", "projekt.name" }, 2));
        }

        /// <summary>
        /// Zweiter Einstieg (Wirtschaftlichkeitsseite): Führt die gewählte Vorlage keinen Schlüssel der
        /// Wirtschaftlichkeit, bietet die Rückfrage die Standardvorlage an und nennt die gewählte. Die
        /// Standardvorlage selbst deckt die Wirtschaftlichkeit über <c>{{bericht.inhalt}}</c>.
        /// </summary>
        [Fact]
        public void Zweiter_Einstieg_ohne_Wirtschaftlichkeit_bietet_die_Standardvorlage_an()
        {
            if (_standard == null) return;
            Vorlageneintrag eigen = Hinzu("Deckblatt.docx", Probevorlagen.AusAbsaetzen("Projekt {{projekt.name}}"));
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            Startbefund ersterEinstieg = _ctrl.PruefeVorStart(konfig, false, 1);
            Assert.False(ersterEinstieg.BrauchtRueckfrage);

            Startbefund zweiterEinstieg = _ctrl.PruefeVorStart(konfig, false, 1, erzwingtWirtschaftlichkeit: true);
            Assert.False(zweiterEinstieg.HatFehler);
            Assert.True(zweiterEinstieg.OhneWirtschaftlichkeit);
            Assert.True(zweiterEinstieg.BrauchtRueckfrage);
            Assert.True(zweiterEinstieg.StandardAngeboten);
            Berichtsmeldung befund = Assert.Single(zweiterEinstieg.Befunde);
            Assert.Equal(KiMeldungskennung.BV_START_OHNE_WIRTSCHAFT, befund.Kennung);
            Assert.Equal("Die Vorlage „Deckblatt“ enthält keinen Platzhalter der Wirtschaftlichkeit – für diesen Bericht " +
                         "wird die Standardvorlage angeboten", befund.Text);
            Assert.Contains("in die Word-Vorlage „Deckblatt“ gefüllt", zweiterEinstieg.Rueckfrage, StringComparison.Ordinal);
            Assert.DoesNotContain(R.BV_START_GELB, zweiterEinstieg.Rueckfrage, StringComparison.Ordinal);

            Startbefund standard = _ctrl.PruefeVorStart(Konfig(), false, 1, erzwingtWirtschaftlichkeit: true);
            Assert.True(standard.Pruefbefund.HatWirtschaftlichkeit);
            Assert.False(standard.BrauchtRueckfrage);
        }

        /// <summary>
        /// Geprüft = gefüllt (Konzept 6.8): Nach der Vorprüfung wird die Vorlage in der Datei geändert;
        /// der Lauf füllt trotzdem die Bytes, die geprüft wurden — gleiche Prüfsumme, alter Text, kein
        /// neuer unbekannter Platzhalter.
        /// </summary>
        [Fact]
        public void Geprueft_und_gefuellt_werden_dieselben_Bytes()
        {
            Vorlageneintrag eigen = Hinzu("Stand.docx", Probevorlagen.AusAbsaetzen("Erster Stand {{projekt.name}}"));
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            Startbefund start = _ctrl.PruefeVorStart(konfig, false, 1);
            Assert.False(start.BrauchtRueckfrage);
            File.WriteAllBytes(eigen.Pfad, Probevorlagen.AusAbsaetzen("Zweiter Stand {{projekt.gibtsnicht}}"));

            Berichtslauf lauf = _ctrl.ErzeugeWord(Berichtsdatenproben.Gruppendaten(2), konfig, start);

            Assert.Equal(start.Pruefsumme, lauf.Pruefsumme);
            Assert.NotEqual(Vorlagenpruefer.Pruefsumme(File.ReadAllBytes(eigen.Pfad)), lauf.Pruefsumme);
            Assert.Equal("Erster Stand Stammprojekt", ErsterAbsatz(lauf.Pfad));
            Assert.Empty(lauf.Unbekannte);
        }

        // =====================================================================
        //  Laufmeldung in beiden Sprachen
        // =====================================================================

        /// <summary>
        /// Die Laufmeldung spricht die Sprache des Berichts: auf Deutsch die deutschen Rahmentexte, auf
        /// Englisch (Oberfläche und Bericht englisch) durchgehend englisch — auch der Rückfall und die
        /// Gründe der gelben Stellen.
        /// </summary>
        [Fact]
        public void Laufmeldung_steht_auf_Deutsch_und_auf_Englisch()
        {
            byte[] beispiel = Repovorlage(BerichtsvorlageDateiWacheTests.BEISPIEL);
            if (beispiel == null || _standard == null) return;
            Vorlageneintrag eigen = Hinzu(BerichtsvorlageDateiWacheTests.BEISPIEL, beispiel);
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            string deutsch = BerichtCtrl.Laufmeldung(_ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig), false);
            Assert.StartsWith("Word-Vorlage: „Berichtsvorlage_Beispiel“ (Für dieses Projekt gewählt)", deutsch, StringComparison.Ordinal);
            Assert.Contains("Nicht ersetzte Platzhalter, im Bericht gelb markiert: 8", deutsch, StringComparison.Ordinal);
            Assert.Contains("unbekannter Schlüssel; Rumpf, Absatz ", deutsch, StringComparison.Ordinal);

            int vorher = Sprache.Nummer;
            try
            {
                using var englischeKultur = new Kulturvorrichtung("en-US");
                Sprache.Nummer = 1;
                Berichtslauf lauf = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);
                string englisch = BerichtCtrl.Laufmeldung(lauf, true);
                _ausgabe.WriteLine(englisch);
                Assert.True(lauf.Englisch);
                Assert.StartsWith("Word template: “Berichtsvorlage_Beispiel” (Chosen for this project)", englisch, StringComparison.Ordinal);
                Assert.Contains("Placeholders not replaced, highlighted yellow in the report: 8", englisch, StringComparison.Ordinal);
                Assert.Contains("unknown key; Body, paragraph ", englisch, StringComparison.Ordinal);
                Assert.Contains("Template comments removed: 2", englisch, StringComparison.Ordinal);

                BerichtsKonfiguration fehlt = Konfig();
                fehlt.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
                fehlt.VorlageWordDatei = "Offer.docx";
                string rueckfall = BerichtCtrl.Laufmeldung(_ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), fehlt), true);
                Assert.Contains("Fallback in the template choice:\r\n• “Offer” not available – “Standard (EPOS-Plan)” used",
                                rueckfall, StringComparison.Ordinal);
                Assert.StartsWith("Word template: “Standard (EPOS-Plan)” (Standard template)", rueckfall, StringComparison.Ordinal);
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>Jeder Text der Laufmeldung und der Rückfrage steht in beiden Sprachen.</summary>
        [Fact]
        public void Texte_der_Laufmeldung_und_der_Rueckfrage_stehen_in_beiden_Sprachen()
        {
            ResourceSet englisch = R.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en-US"), true, false);
            ResourceSet deutsch = R.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
            Assert.NotNull(englisch);
            List<string> schluessel = deutsch.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key)
                .Where(k => k.StartsWith("BV_LAUF_", StringComparison.Ordinal) || k.StartsWith("BV_START_", StringComparison.Ordinal))
                .OrderBy(k => k, StringComparer.Ordinal).ToList();
            Assert.True(schluessel.Count >= 24, "Zu wenige Texte gefunden: " + schluessel.Count);
            foreach (string k in schluessel)
                Assert.False(string.IsNullOrWhiteSpace(englisch.GetString(k)), k + " fehlt in Resource.en-US.resx");
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Eine Konfiguration mit dem Deckblatt (ohne Datenbank) und dem Zielordner des Falls.</summary>
        private BerichtsKonfiguration Konfig()
        {
            var k = new BerichtsKonfiguration { ZielOrdner = _ziel };
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_DECKBLATT);
            return k;
        }

        /// <summary>Legt eine Quelldatei an und fügt sie dem Vorlagenordner hinzu.</summary>
        private Vorlageneintrag Hinzu(string name, byte[] inhalt)
        {
            string quelle = Path.Combine(_quellen, name);
            File.WriteAllBytes(quelle, inhalt);
            Vorlagenergebnis r = _vorlagen.Hinzufuegen(quelle);
            Assert.True(r.Erfolg, r.Meldung);
            return r.Eintrag;
        }

        /// <summary>Eine Vorlage des Repositoriums als Bytes; <c>null</c> ohne Repositorium.</summary>
        private static byte[] Repovorlage(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            Assert.True(File.Exists(pfad), "Die Vorlage fehlt: " + pfad);
            return File.ReadAllBytes(pfad);
        }

        /// <summary>Der Text des ersten Absatzes im Rumpf.</summary>
        private static string ErsterAbsatz(string pfad)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            return doc.MainDocumentPart.Document.Body.Elements<Paragraph>().First().InnerText;
        }

        /// <summary>Die Befunde des Validators in allen Office-Fassungen 2007 bis 2021.</summary>
        private List<string> Validierungsfehler(string pfad)
        {
            var befunde = new List<string>();
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
                foreach (ValidationErrorInfo f in new OpenXmlValidator(fassung).Validate(doc).Take(5))
                {
                    string text = fassung + ": " + f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath;
                    befunde.Add(text);
                    _ausgabe.WriteLine(text);
                }
            return befunde;
        }
    }
}
