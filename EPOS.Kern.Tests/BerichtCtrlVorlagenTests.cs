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
            Vorlageneintrag eigen = Hinzu(EIGENE, EigeneMitUnbekannten());
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            Berichtslauf lauf = _ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig);

            Assert.Empty(Validierungsfehler(lauf.Pfad));
            Assert.Equal(Vorlagenwahlgrund.Abweichung, lauf.Grund);
            Assert.Equal("eigen:" + EIGENE, lauf.VorlageId);
            Assert.Equal("Kapitelentwurf", lauf.VorlageName);
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
            Assert.Contains("Word-Vorlage: „Kapitelentwurf“ (Für dieses Projekt gewählt)", meldung, StringComparison.Ordinal);
            Assert.Contains("Nicht ersetzte Platzhalter, im Bericht gelb markiert: 8", meldung, StringComparison.Ordinal);
            Assert.Contains("• {{kapitel.eins|ohne titel}} – unbekannter Schlüssel; Rumpf, Absatz ", meldung, StringComparison.Ordinal);
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
            Assert.Empty(ohne.Pruefbefund.Meldungen);
        }

        /// <summary>
        /// Eine unpassende Sicht verlangt die Rückfrage auch ohne Fehler; die Sicht folgt der Regel 4.7
        /// (<c>stand.b</c> allein in Sicht 1 bei genau einer Variante). Eine abweichende Sprache hält nicht an
        /// (BV-Q7 b): Der Bericht entsteht in der Sprache der Vorlage, die Startrückfrage nennt sie nur.
        /// </summary>
        [Fact]
        public void Abweichende_Sprache_haelt_nicht_an_unpassende_Sicht_verlangt_die_Rueckfrage()
        {
            Vorlageneintrag englisch = Hinzu("English.docx",
                Probevorlagen.Baue(b => b.Absatz("{{projekt.name}}").Eigenschaften(null, "en")));
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, englisch);

            Startbefund deutsch = _ctrl.PruefeVorStart(konfig, false, 1);
            Assert.False(deutsch.HatFehler);
            Assert.True(deutsch.SpracheAbweichend);
            Assert.False(deutsch.BrauchtRueckfrage);
            Assert.Empty(deutsch.Befunde);
            Berichtssprache sprache = Berichtssprache.Fuer(deutsch, Startweg.Gewaehlt, null, false, false);
            Assert.True(sprache.Englisch);
            Assert.True(sprache.AusVorlage);
            Assert.Equal("English", sprache.Vorlage);
            Assert.Equal("Der Bericht wird auf Englisch erstellt – in der Sprache der Vorlage „English“.", sprache.Hinweis(false));
            Assert.Equal("The report will be created in English – the language of the template “English”.", sprache.Hinweis(true));
            // Mit Standardvorlage (sprachneutral) gilt wieder die Oberflächensprache.
            Assert.False(Berichtssprache.Fuer(deutsch, Startweg.Standard, null, false, false).Englisch);
            Assert.False(_ctrl.PruefeVorStart(konfig, true, 1).BrauchtRueckfrage);
            Assert.Equal("", Berichtssprache.Fuer(_ctrl.PruefeVorStart(konfig, true, 1), Startweg.Gewaehlt, null, false, true).Hinweis(true));

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
        //  Stellen der Kapitel (Anhang-E-Checkliste, BV-E2)
        // =====================================================================

        /// <summary>
        /// <see cref="BerichtCtrl.KapitelstellenDerVorlage"/> mit der Standardvorlage: je Bausteinschlüssel der
        /// Kapitelkopf, das Deckblatt aus Platzhaltern, der Anhang E unter <c>anhang_e</c>. Ein abgewähltes
        /// Häkchen heißt „nicht im Bericht“ (<c>null</c>) — außer beim Deckblatt, das die Vorlage aus
        /// Platzhaltern selbst trägt.
        /// </summary>
        [Fact]
        public void Kapitelstellen_der_Standardvorlage_folgen_Kapitelkoepfen_und_Haekchen()
        {
            if (_standard == null) return;
            BerichtsKonfiguration voll = Berichtsdatenproben.VolleKonfiguration();
            IReadOnlyDictionary<string, string> stellen = _ctrl.KapitelstellenDerVorlage(voll, false);

            Assert.Equal(Berichtskapitel.Alle.Select(k => k.Stellenschluessel).OrderBy(s => s, StringComparer.Ordinal),
                         stellen.Keys.OrderBy(s => s, StringComparer.Ordinal));
            Assert.Equal("Deckblatt", stellen[BerichtsKonfiguration.B_DECKBLATT]);
            Assert.Equal("Inhalt", stellen[BerichtsKonfiguration.B_INHALT]);
            Assert.Equal("Projektbeschreibung", stellen[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Equal("Komponenten & Varianten", stellen[BerichtsKonfiguration.B_KOMPONENTEN]);
            Assert.Equal("Berechnungsergebnisse je Variante", stellen[BerichtsKonfiguration.B_ERGEBNISSE]);
            Assert.Equal("Variantenvergleich", stellen[BerichtsKonfiguration.B_VERGLEICH]);
            Assert.Equal("Wirtschaftlichkeit", stellen[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal("Anhang", stellen[BerichtsKonfiguration.B_ANHANG]);
            Assert.Equal(R.WIRT_AE_TITEL, stellen[Berichtskapitel.ANHANG_E]);

            // Nur das Häkchen „Deckblatt“: jedes Kapitel ohne Stelle, das Deckblatt aus Platzhaltern bleibt.
            IReadOnlyDictionary<string, string> nurDeckblatt = _ctrl.KapitelstellenDerVorlage(Konfig(), false);
            Assert.Equal("Deckblatt", nurDeckblatt[BerichtsKonfiguration.B_DECKBLATT]);
            Assert.All(Berichtskapitel.Alle.Where(k => k.Name != Berichtskapitel.DECKBLATT),
                       k => Assert.Null(nurDeckblatt[k.Stellenschluessel]));
            var ohneDeckblatt = new BerichtsKonfiguration();
            ohneDeckblatt.AktiveBausteine.Add(BerichtsKonfiguration.B_ANHANG);
            Assert.Equal("Deckblatt", _ctrl.KapitelstellenDerVorlage(ohneDeckblatt, false)[BerichtsKonfiguration.B_DECKBLATT]);
        }

        /// <summary>
        /// Eine eigene Vorlage: Der umbenannte Kapitelkopf ist die Stelle, ein Kapitelkopf aus
        /// <c>{{text.kapitel_*}}</c> folgt der Sprache, ein Kapitel ohne Kapitelkopf nennt seine eigene
        /// Überschrift, was die Vorlage nicht führt, steht nicht im Bericht — dieselben Stellen macht die
        /// Checkliste zur Spalte „Stelle“. Lässt sich die gewählte Vorlage nicht lesen, gilt die
        /// Standardvorlage; fehlt auch sie, der bisherige Weg mit den eigenen Überschriften.
        /// </summary>
        [Fact]
        public void Kapitelstellen_einer_eigenen_Vorlage_und_ihre_Rueckfaelle()
        {
            if (_standard == null) return;
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Stile(Probevorlagen.Stil("EPOSKapitelkopf", "EPOS Kapitelkopf", 0))
                .Roh("<w:pPr><w:pStyle w:val=\"EPOSKapitelkopf\"/></w:pPr><w:r><w:t>5 Wirtschaftliche Bewertung</w:t></w:r>")
                .Absatz("{{kapitel.wirtschaftlichkeit|ohne titel}}")
                .Roh("<w:pPr><w:pStyle w:val=\"EPOSKapitelkopf\"/></w:pPr><w:r><w:t>{{text.kapitel_anhang_e}}</w:t></w:r>")
                .Absatz("{{kapitel.anhang_e|ohne titel}}")
                .Absatz("{{kapitel.projekt}}"));
            Vorlageneintrag eigen = Hinzu("Bewertung.docx", vorlage);
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            konfig.ZielOrdner = _ziel;
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            IReadOnlyDictionary<string, string> deutsch = _ctrl.KapitelstellenDerVorlage(konfig, false);
            Assert.Equal("5 Wirtschaftliche Bewertung", deutsch[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal(R.WIRT_AE_TITEL, deutsch[Berichtskapitel.ANHANG_E]);
            Assert.Equal("Projektbeschreibung", deutsch[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Null(deutsch[BerichtsKonfiguration.B_ANHANG]);
            Assert.Null(deutsch[BerichtsKonfiguration.B_DECKBLATT]);

            IReadOnlyDictionary<string, string> englisch = _ctrl.KapitelstellenDerVorlage(konfig, true);
            Assert.Equal(R.ResourceManager.GetString(nameof(R.WIRT_AE_TITEL), CultureInfo.GetCultureInfo("en-US")),
                         englisch[Berichtskapitel.ANHANG_E]);
            Assert.Equal("Project description", englisch[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Equal("5 Wirtschaftliche Bewertung", englisch[BerichtsKonfiguration.B_WIRTSCHAFT]);

            List<ChecklistenPunkt> punkte = AnhangECheckliste.Punkte(new ChecklistenLage(), deutsch);
            Assert.Equal("Wortbericht: „5 Wirtschaftliche Bewertung“ › „Kennzahlen im Szenario „Erwartet““ · Tabellenbericht: " +
                         "Blatt „Wirtschaftlichkeit“, Block „Erwartet“", punkte.Single(p => p.Nummer == "1").Stelle);
            Assert.Equal("Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Übersicht“", punkte.Single(p => p.Nummer == "0.1").Stelle);
            Assert.Equal("Wortbericht: „Projektbeschreibung“ · Tabellenbericht: Blatt „Übersicht“",
                         punkte.Single(p => p.Nummer == "0.2").Stelle);

            // Die gewählte Vorlage ist nicht lesbar: die Standardvorlage.
            File.WriteAllText(eigen.Pfad, "kein Word-Dokument");
            Assert.Equal("Wirtschaftlichkeit", _ctrl.KapitelstellenDerVorlage(konfig, false)[BerichtsKonfiguration.B_WIRTSCHAFT]);

            // Ohne Standardvorlage der bisherige Weg: die eigenen Überschriften der angehakten Kapitel.
            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD));
            BerichtsvorlagenCtrl.EntferneAbweichung(konfig);
            IReadOnlyDictionary<string, string> alt = _ctrl.KapitelstellenDerVorlage(konfig, false);
            Assert.Equal("Deckblatt", alt[BerichtsKonfiguration.B_DECKBLATT]);
            Assert.Equal("Berechnungsergebnisse je Variante", alt[BerichtsKonfiguration.B_ERGEBNISSE]);
            Assert.Equal("Wirtschaftlichkeit", alt[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal(R.WIRT_AE_TITEL, alt[Berichtskapitel.ANHANG_E]);
            Assert.Null(_ctrl.KapitelstellenDerVorlage(Konfig(), false)[BerichtsKonfiguration.B_WIRTSCHAFT]);
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
            if (_standard == null) return;
            Vorlageneintrag eigen = Hinzu(EIGENE, EigeneMitUnbekannten());
            BerichtsKonfiguration konfig = Konfig();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, eigen);

            string deutsch = BerichtCtrl.Laufmeldung(_ctrl.ErzeugeWordLauf(Berichtsdatenproben.Gruppendaten(2), konfig), false);
            Assert.StartsWith("Word-Vorlage: „Kapitelentwurf“ (Für dieses Projekt gewählt)", deutsch, StringComparison.Ordinal);
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
                Assert.StartsWith("Word template: “Kapitelentwurf” (Chosen for this project)", englisch, StringComparison.Ordinal);
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
        //  BV-E9: die Sprache des Laufs aus der Vorlage (BV-Q7 b, Konzept 4.9)
        // =====================================================================

        /// <summary>Die Einzelwerte, an denen die Sprache des Berichts sichtbar wird: Festtext, Datum, Zahlen.</summary>
        private static readonly string[] SPRACHPROBE =
        {
            "{{bericht.untertitel}}", "{{text.seite}}", "{{bericht.datum}}",
            "{{stamm.kennzahl.ko.energie}}", "{{stamm.kennzahl.eff.t_oben_mittel}}",
        };

        /// <summary>Eine Vorlage aus <see cref="SPRACHPROBE"/>, je Schlüssel ein Absatz, mit oder ohne Sprache.</summary>
        private static byte[] Sprachvorlage(string sprache)
        {
            return Probevorlagen.Baue(b =>
            {
                foreach (string s in SPRACHPROBE) b.Absatz(s);
                if (sprache != null) b.Eigenschaften(null, sprache);
            });
        }

        /// <summary>Die Gruppe mit Berichtsdatum 25.09.2026 und berechneten Kennzahlen (Energiekosten 12 000 €, 62 °C).</summary>
        private static BerichtsDaten Sprachdaten()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            daten.ErstelltAm = new DateTime(2026, 9, 25, 14, 30, 0);
            foreach (VariantenDaten v in daten.Varianten) KennzahlenKatalog.Berechne(v);
            return daten;
        }

        /// <summary>Die Texte der Absätze im Rumpf, in Dokumentfolge.</summary>
        private static List<string> Absaetze(string pfad)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            return doc.MainDocumentPart.Document.Body.Elements<Paragraph>().Select(p => p.InnerText).ToList();
        }

        private static readonly string[] ENGLISCH =
            { "Variant comparison — energy and heat supply", "Page", "9/25/2026", "12,000 €/a", "62.0 °C" };

        private static readonly string[] DEUTSCH =
            { "Variantenvergleich — Energie- und Wärmeversorgung", "Seite", "25.09.2026", "12.000 €/a", "62,0 °C" };

        /// <summary>
        /// <b>Vorlage englisch, Oberfläche deutsch → englischer Bericht</b> (BV-Q7 b): Festtexte, Datum und Zahlen stehen
        /// englisch — über die Vorprüfung wie über den Lauf ohne sie. Die Vorprüfung hält nicht an, nennt die Sprache aber;
        /// die Laufmeldung (in der Oberflächensprache) nennt sie auch. Danach gilt wieder die Oberflächensprache: Weder
        /// <see cref="BerichtTexte.Englisch"/> noch die Anzeigekultur bleiben umgeschaltet.
        /// </summary>
        [Fact]
        public void Vorlage_englisch_bei_deutscher_Oberflaeche_ergibt_einen_englischen_Bericht()
        {
            int vorher = Sprache.Nummer;
            try
            {
                Sprache.Nummer = 0;
                Vorlageneintrag englisch = Hinzu("Offer.docx", Sprachvorlage("en"));
                BerichtsKonfiguration konfig = Konfig();
                BerichtsvorlagenCtrl.SetzeAbweichung(konfig, englisch);
                CultureInfo oberflaeche = CultureInfo.CurrentUICulture;

                Startbefund start = _ctrl.PruefeVorStart(konfig, false, 1);
                Assert.False(start.BrauchtRueckfrage);
                Assert.Equal("Der Bericht wird auf Englisch erstellt – in der Sprache der Vorlage „Offer“.",
                             Berichtssprache.Fuer(start, Startweg.Gewaehlt, null, false, false).Hinweis(false));

                Berichtslauf lauf = _ctrl.ErzeugeWord(Sprachdaten(), konfig, start);
                Assert.True(lauf.Englisch);
                Assert.Equal(ENGLISCH, Absaetze(lauf.Pfad));
                string meldung = BerichtCtrl.Laufmeldung(lauf, false);
                _ausgabe.WriteLine(meldung);
                Assert.StartsWith("Word-Vorlage: „Offer“ (Für dieses Projekt gewählt)\r\nSprache des Berichts: Englisch (aus der Vorlage)",
                                  meldung, StringComparison.Ordinal);
                Assert.Contains(BerichtCtrl.Laufabschnitte(lauf, false), a => a.Kennung == KiMeldungskennung.VF_PRUEF_SPRACHE);

                // Der Lauf ohne Vorprüfung liest die Sprache aus den Bytes.
                Berichtslauf ohne = _ctrl.ErzeugeWordLauf(Sprachdaten(), konfig);
                Assert.True(ohne.Englisch);
                Assert.Equal(ENGLISCH, Absaetze(ohne.Pfad));

                // Nichts bleibt umgeschaltet.
                Assert.False(BerichtTexte.Englisch);
                Assert.Null(BerichtTexte.Laufsprache);
                Assert.Equal(oberflaeche, CultureInfo.CurrentUICulture);

                // Mit Standardvorlage (sprachneutral) entsteht der Bericht in der Oberflächensprache.
                Assert.False(_ctrl.ErzeugeWord(Sprachdaten(), konfig, start, Startweg.Standard).Englisch);
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>
        /// <b>Umgekehrt: Vorlage deutsch, Oberfläche englisch → deutscher Bericht</b>; die Laufmeldung steht englisch und
        /// nennt die Sprache.
        /// </summary>
        [Fact]
        public void Vorlage_deutsch_bei_englischer_Oberflaeche_ergibt_einen_deutschen_Bericht()
        {
            int vorher = Sprache.Nummer;
            try
            {
                using var englischeKultur = new Kulturvorrichtung("en-US");
                Sprache.Nummer = 1;
                Vorlageneintrag deutsch = Hinzu("Angebot.docx", Sprachvorlage("de"));
                BerichtsKonfiguration konfig = Konfig();
                BerichtsvorlagenCtrl.SetzeAbweichung(konfig, deutsch);

                Startbefund start = _ctrl.PruefeVorStart(konfig, true, 1);
                Assert.False(start.BrauchtRueckfrage);
                Assert.True(start.SpracheAbweichend);
                Berichtslauf lauf = _ctrl.ErzeugeWord(Sprachdaten(), konfig, start);
                Assert.False(lauf.Englisch);
                Assert.Equal(DEUTSCH, Absaetze(lauf.Pfad));
                Assert.Contains("\r\nReport language: German (from the template)", BerichtCtrl.Laufmeldung(lauf, true), StringComparison.Ordinal);
                Assert.True(BerichtTexte.Englisch);
                Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>
        /// <b>Ohne Angabe gilt die Oberflächensprache</b> — in beiden Richtungen, ohne Hinweis und ohne Zeile in der
        /// Laufmeldung; eine Vorlage in der Sprache der Oberfläche ebenso.
        /// </summary>
        [Fact]
        public void Ohne_Sprache_in_der_Vorlage_gilt_die_Oberflaechensprache()
        {
            int vorher = Sprache.Nummer;
            try
            {
                Vorlageneintrag neutral = Hinzu("Neutral.docx", Sprachvorlage(null));
                Vorlageneintrag deutsch = Hinzu("Deutsch.docx", Sprachvorlage("de"));
                foreach (bool englisch in new[] { false, true })
                {
                    using var kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
                    Sprache.Nummer = englisch ? 1 : 0;
                    BerichtsKonfiguration konfig = Konfig();
                    BerichtsvorlagenCtrl.SetzeAbweichung(konfig, neutral);
                    Startbefund start = _ctrl.PruefeVorStart(konfig, englisch, 1);
                    Assert.Equal("", Berichtssprache.Fuer(start, Startweg.Gewaehlt, null, false, englisch).Hinweis(englisch));
                    Berichtslauf lauf = _ctrl.ErzeugeWord(Sprachdaten(), konfig, start);
                    Assert.Equal(englisch, lauf.Englisch);
                    Assert.Equal(englisch ? ENGLISCH : DEUTSCH, Absaetze(lauf.Pfad));
                    Assert.DoesNotContain(BerichtCtrl.Laufabschnitte(lauf, englisch), a => a.Kennung == KiMeldungskennung.VF_PRUEF_SPRACHE);
                }

                Sprache.Nummer = 0;
                BerichtsKonfiguration gleich = Konfig();
                BerichtsvorlagenCtrl.SetzeAbweichung(gleich, deutsch);
                Startbefund passend = _ctrl.PruefeVorStart(gleich, false, 1);
                Assert.False(passend.SpracheAbweichend);
                Assert.Equal("", Berichtssprache.Fuer(passend, Startweg.Gewaehlt, null, false, false).Hinweis(false));
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>
        /// <b>Word und Excel in einer Sprache</b>: Widersprechen sich die Sprachen der Word- und der Excel-Vorlage, gewinnt
        /// die Word-Vorlage; der Excel-Befund bekommt den Widerspruch als Befund der Rückfrage (einmal, auch beim zweiten
        /// Abgleich), und die Mappe entsteht in der Sprache der Word-Vorlage. Ohne Word-Sprache bestimmt die Excel-Vorlage;
        /// „ohne Excel-Vorlage“ lässt sie außen vor.
        /// </summary>
        [Fact]
        public void Widerspruch_zwischen_Word_und_Excel_fragt_zurueck_und_die_Word_Vorlage_gewinnt()
        {
            int vorher = Sprache.Nummer;
            try
            {
                Sprache.Nummer = 0;
                Vorlageneintrag word = Hinzu("Offer.docx", Sprachvorlage("en"));
                Vorlageneintrag excel = Hinzu("Mappe.xlsx", Excelprobe.MitEigenschaft(Excelprobe.Mappe(wb =>
                {
                    ClosedXML.Excel.IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                    for (int i = 0; i < SPRACHPROBE.Length; i++) ws.Cell(i + 1, 1).Value = SPRACHPROBE[i];
                }), Vorlagenpruefer.EIGENSCHAFT_SPRACHE, "de"));
                BerichtsKonfiguration konfig = Konfig();
                konfig.Ausgabe = "Beide";
                BerichtsvorlagenCtrl.SetzeAbweichung(konfig, word);
                BerichtsvorlagenCtrl.SetzeAbweichungExcel(konfig, excel);

                Startbefund start = _ctrl.PruefeVorStart(konfig, false, 1);
                Excelstartbefund excelStart = _ctrl.PruefeExcelVorStart(konfig, false, 1);
                Assert.False(excelStart.BrauchtRueckfrage);
                Assert.False(Berichtssprache.Fuer(null, Startweg.Gewaehlt, excelStart, false, true).Englisch);   // Excel allein: deutsch

                Excelstartbefund abgeglichen = BerichtCtrl.SpracheAbgleichen(start, excelStart);
                Assert.True(abgeglichen.Sprachwiderspruch);
                Assert.True(abgeglichen.BrauchtRueckfrage);
                Assert.False(abgeglichen.HatFehler);
                Berichtsmeldung punkt = Assert.Single(abgeglichen.Befunde);
                Assert.Equal(KiMeldungskennung.VF_PRUEF_SPRACHE, punkt.Kennung);
                Assert.Equal("Excel-Vorlage „Mappe“ ist auf Deutsch angelegt, die Word-Vorlage „Offer“ auf Englisch – Bericht und Mappe entstehen auf Englisch",
                             punkt.Text);
                Assert.Same(abgeglichen, BerichtCtrl.SpracheAbgleichen(start, abgeglichen));
                Assert.Same(excelStart, BerichtCtrl.SpracheAbgleichen(null, excelStart));

                Berichtssprache sprache = Berichtssprache.Fuer(start, Startweg.Gewaehlt, abgeglichen, false, false);
                Assert.True(sprache.Widerspruch);
                Assert.True(sprache.Englisch);
                Assert.Equal("Offer", sprache.Vorlage);
                Assert.False(Berichtssprache.Fuer(start, Startweg.Gewaehlt, abgeglichen, true, false).Widerspruch);

                BerichtsDaten daten = Sprachdaten();
                Berichtslauf mappe = _ctrl.ErzeugeExcelLauf(daten, konfig, abgeglichen, false, sprache);
                Assert.True(mappe.Englisch);
                Assert.Equal(ENGLISCH.Take(2), Zellen(mappe.Pfad));
                Assert.Contains("\r\nSprache des Berichts: Englisch (aus der Vorlage)", BerichtCtrl.LaufmeldungExcel(mappe, false), StringComparison.Ordinal);

                // Ohne Sprache des Laufs füllt der Lauf die Excel-Vorlage in ihrer eigenen Sprache.
                Assert.Equal(DEUTSCH.Take(2), Zellen(_ctrl.ErzeugeExcelLauf(daten, konfig, excelStart, false).Pfad));
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>
        /// Die Texte der Festtext-Zellen (Untertitel, „Seite“) der Spalte A des ersten Blatts. Datum und Zahlen schreibt
        /// die Mappe als Werte mit Zahlenformat — ihre Anzeige wählt Excel nach der Sprache des Betrachters.
        /// </summary>
        private static List<string> Zellen(string pfad)
        {
            using var mappe = new ClosedXML.Excel.XLWorkbook(pfad);
            ClosedXML.Excel.IXLWorksheet blatt = mappe.Worksheets.First();
            Assert.Equal(ClosedXML.Excel.XLDataType.DateTime, blatt.Cell(3, 1).DataType);
            Assert.Equal(ClosedXML.Excel.XLDataType.Number, blatt.Cell(4, 1).DataType);
            return Enumerable.Range(1, 2).Select(z => blatt.Cell(z, 1).GetString()).ToList();
        }

        /// <summary>
        /// <b>Die Klammer des Laufs</b> (<see cref="BerichtTexte.ImLauf"/>): Wörterbuch, Kultur, MyResource und die
        /// Diagrammbeschriftung folgen ihr — der Kapitalwertverlauf beschriftet „Year“ und „50,000“ —, auch auf den
        /// Arbeitsfäden, die der Lauf über die Kulturweitergabe startet; danach steht alles wie vorher.
        /// </summary>
        [Fact]
        public void Die_Klammer_des_Laufs_schaltet_Texte_Zahlen_und_Diagramme_und_stellt_zurueck()
        {
            int vorher = Sprache.Nummer;
            try
            {
                Sprache.Nummer = 0;
                CultureInfo oberflaeche = CultureInfo.CurrentUICulture;
                Assert.Equal(("Jahr", "50.000"), Achsen());

                using (BerichtTexte.ImLauf(true))
                {
                    Assert.True(BerichtTexte.Englisch);
                    Assert.False(BerichtTexte.OberflaecheEnglisch);
                    Assert.Equal(true, BerichtTexte.Laufsprache);
                    Assert.Equal("en-US", BerichtTexte.Kultur.Name);
                    Assert.Equal("Contents", BerichtTexte.T("Inhalt"));
                    Assert.Equal("Page", R.BV_TEXT_SEITE);
                    Assert.Equal(("Year", "50,000"), Achsen());
                    bool aufDemFaden = SpeicherEngine.Kulturweitergabe.Starten(() => BerichtTexte.Englisch && R.BV_TEXT_SEITE == "Page").Result;
                    Assert.True(aufDemFaden);

                    using (BerichtTexte.ImLauf(false)) Assert.Equal("Seite", R.BV_TEXT_SEITE);
                    Assert.Equal("Page", R.BV_TEXT_SEITE);
                }

                Assert.False(BerichtTexte.Englisch);
                Assert.Null(BerichtTexte.Laufsprache);
                Assert.Equal(oberflaeche, CultureInfo.CurrentUICulture);
                Assert.Equal(("Jahr", "50.000"), Achsen());
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>Die Beschriftung der Jahresachse und der größte Wert der y-Achse des Kapitalwertverlaufs.</summary>
        private static (string Jahr, string Wert) Achsen()
        {
            var reihe = new ChartRenderer.Reihe("A", new double[] { 0, 10000, 30000, 50000 }, ChartRenderer.C_STAMM);
            WindowsFormsApplication1.Zeichnung.Zeichenmodell m = ChartRenderer.KapitalwertVerlaufModell("K", new List<ChartRenderer.Reihe> { reihe }, null);
            List<string> x = m.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>()
                              .Where(t => t.Marke == "xachse").Select(t => t.Inhalt).ToList();
            List<string> y = m.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>()
                              .Where(t => t.Marke == "yachse").Select(t => t.Inhalt).ToList();
            return (x.Last(), y.First(t => t.Contains("50", StringComparison.Ordinal)));
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

        /// <summary>Der Dateiname der eigenen Vorlage mit unbekannten Platzhaltern.</summary>
        private const string EIGENE = "Kapitelentwurf.docx";

        /// <summary>
        /// Eine eigene Vorlage mit acht Kapitelplatzhaltern, die der Katalog nicht kennt, einem Kundenfeld (leer
        /// in der Gruppe) und zwei Kommentaren — die Stellen, die im Bericht gelb bleiben, leer sind und
        /// entfernt werden.
        /// </summary>
        private static byte[] EigeneMitUnbekannten()
        {
            string[] namen = { "eins", "zwei", "drei", "vier", "fuenf", "sechs", "sieben", "acht" };
            return Probevorlagen.Baue(b =>
            {
                b.Absatz("{{bericht.titel}}").Absatz("{{projekt.kunde}}");
                foreach (string n in namen) b.Absatz("{{kapitel." + n + "|ohne titel}}");
                b.Kommentare("Erläuterung", "Noch eine");
            });
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
