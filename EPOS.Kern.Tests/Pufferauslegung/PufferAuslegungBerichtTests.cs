using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Pufferspeicher-Auslegung im Bericht</b> (Konzept Pufferspeicher-Auslegung, Stufe P3): der
    /// Abschnitt der Projektbeschreibung erscheint nur mit einer gespeicherten Zeile in
    /// <c>Tab_PufferAuslegung</c> und zeigt je Puffer Speicherklasse, Vorlage, Nutzungsprofil, Zonenvolumina,
    /// bemessendes Kriterium mit Herkunft, Empfehlung, gewähltes Volumen, Kennzahlen, Hinweise und
    /// Berechnungsdatum — auf Deutsch und Englisch. Dazu <see cref="PufferAuslegungCtrl.Gespeichert"/> auf
    /// einer Projektkopie der Testdatenbank: Auslegung speichern, lesen, Bericht schreiben.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PufferAuslegungBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        /// <summary>Der Kombipuffer des Zapfprofil-Referenzprojekts 1045.</summary>
        private const int P_ZAPF = 1045;
        private const int PUFFER_1045_KOMBI = 1054210;

        private static BerichtsDaten Daten(List<PufferAuslegungGespeichert> zeilen)
        {
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(new VariantenDaten { IstStamm = true, Projektname = "Probe", Pufferauslegungen = zeilen });
            return d;
        }

        private static string[] Schreibe(BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            new ProjektbeschreibungBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return main.Document.Body.Descendants<Paragraph>().Select(p => p.InnerText).ToArray();
        }

        /// <summary>Ein Kombipuffer mit runden Werten und zwei Hinweisen.</summary>
        private static PufferAuslegungGespeichert Probe() => new PufferAuslegungGespeichert
        {
            IdZeile = 1,
            IdPuffer = 7,
            Puffername = "Speicher 1",
            GewaehltL = 1000,
            KlasseHeizung = true,
            KlasseBrauchwasser = true,
            Vorlage = PufferVorlage.WP_BIVALENT,
            Nutzungsprofil = PufferNutzungsprofil.WOHNEN,
            NutzungsprofilHerkunft = Textbaustein.Klar("Zapfprofil"),
            VolumenHeizungL = 700,
            VolumenBrauchwasserL = 300,
            EmpfehlungL = 1000,
            Bemessend = "Heizung: K4",
            BemessendHerkunft = Textbaustein.Klar("VDI 4645 Gl. 23"),
            BerechnetAm = new DateTime(2026, 10, 3, 12, 30, 0),
            NachgerechnetL = 1000,
            StartsJeTag = 4,
            VerlustKwhJeTag = 2.5,
            VerlustWJeK = 2.3148,
            Warnungen = new[]
            {
                new PufferWarnung(PufferWarncode.STARTS_TAG, PufferStufe.Warnung, "Klartext", "Probe", PufferZone.Heizung),
                new PufferWarnung("PA-UNBEKANNT", PufferStufe.Hinweis, "Klartext ohne Ressource", "Probe", null)
            }
        };

        [Fact]
        public void Ohne_gespeicherte_Zeile_entfaellt_der_Abschnitt()
        {
            string[] ohne = Schreibe(Daten(new List<PufferAuslegungGespeichert>()));
            Assert.DoesNotContain(R.PAUS_TITEL, ohne);
            Assert.DoesNotContain(R.BER_PAUS_EINLEITUNG, ohne);
            Assert.DoesNotContain(R.PAUS_TITEL, Schreibe(Daten(null)));
        }

        [Fact]
        public void Mit_Zeile_zeigt_der_Abschnitt_Klasse_Vorlage_Zonen_Kriterium_Kennzahlen_Hinweise_und_Datum()
        {
            string[] t = Schreibe(Daten(new List<PufferAuslegungGespeichert> { Probe() }));
            string alles = string.Join("\n", t);

            Assert.Contains(R.PAUS_TITEL, t);
            Assert.Contains("Speicher 1", t);
            Assert.Contains(R.PAUS_KLASSE_HEIZUNG + " + " + R.PAUS_KLASSE_BRAUCHWASSER, alles);
            Assert.Contains(R.PAUS_VORLAGE_WP_BIVALENT + " (" + R.PAUS_VORLAGE_WP_BIVALENT_UNTER + ")", alles);
            Assert.Contains(R.PAUS_NP_WOHNEN + " — Zapfprofil", alles);
            Assert.Contains(R.PAUS_ZONE_HEIZUNG + "\n700 l", alles);
            Assert.Contains(R.PAUS_ZONE_BRAUCHWASSER + "\n300 l", alles);
            Assert.DoesNotContain(R.PAUS_ZONE_PROZESS, alles);
            Assert.Contains(R.PAUS_ZONE_HEIZUNG + ": " + R.PAUS_KRIT_K4 + " (K4)", alles);
            Assert.Contains("VDI 4645 Gl. 23", alles);
            Assert.Contains(R.PAUS_EMPFEHLUNG + "\n1.000 l", alles);
            Assert.Contains(R.BER_PAUS_GEWAEHLT + "\n1.000 l", alles);
            Assert.Contains("4,0 1/d", alles);
            Assert.Contains("2,50 kWh/d", alles);
            Assert.Contains("2,31 W/K", alles);
            Assert.Contains("03.10.2026 12:30", alles);
            Assert.Contains("• " + R.PAUS_STUFE_WARNUNG + ": " + R.PA_STARTS_TAG, t);
            Assert.Contains("• " + R.PAUS_STUFE_HINWEIS + ": Klartext ohne Ressource", t);
            Assert.DoesNotContain(t, z => z.StartsWith(R.BER_PAUS_NACHRECHNUNG_ABWEICHEND.Substring(0, 20), StringComparison.Ordinal));
        }

        [Fact]
        public void Neuer_Speicher_kein_Puffer_ohne_Hinweise_und_abweichende_Nachrechnung()
        {
            PufferAuslegungGespeichert g = Probe() with
            {
                IdPuffer = null, Puffername = null, GewaehltL = null, EmpfehlungL = 0, NachgerechnetL = 400,
                Warnungen = Array.Empty<PufferWarnung>()
            };
            string[] t = Schreibe(Daten(new List<PufferAuslegungGespeichert> { g }));
            string alles = string.Join("\n", t);
            Assert.Contains(R.BER_PAUS_NEUER_SPEICHER, t);
            Assert.Contains(R.PAUS_EMPFEHLUNG + "\n" + R.PAUS_KEIN_PUFFER, alles);
            Assert.Contains(R.BER_PAUS_GEWAEHLT + "\n—", alles);
            Assert.Contains("• " + R.PAUS_WARNUNGEN_LEER, t);
            Assert.Contains(string.Format(R.BER_PAUS_NACHRECHNUNG_ABWEICHEND, "400"), t);

            string[] fehler = Schreibe(Daten(new List<PufferAuslegungGespeichert> { g with { Fehlertext = "keine Reihen" } }));
            Assert.Contains(string.Format(R.BER_PAUS_NACHRECHNUNG_FEHLT, "keine Reihen"), fehler);
        }

        [Fact]
        public void Der_Abschnitt_steht_auf_Englisch_in_der_Sprache_des_Berichts()
        {
            string[] t;
            using (BerichtTexte.ImLauf(true)) t = Schreibe(Daten(new List<PufferAuslegungGespeichert> { Probe() }));
            Assert.Contains("Buffer storage sizing", t);
            string alles = string.Join("\n", t);
            Assert.Contains("Sizing criterion", alles);
            Assert.Contains("Heating zone: Lock-out time (K4)", alles);
            Assert.Contains("Selected volume", alles);
            Assert.DoesNotContain("Bemessendes Kriterium", alles);
        }

        /// <summary>
        /// Herkunft des bemessenden Kriteriums und des Nutzungsprofils kommen als Ressourcenschlüssel aus dem Kern
        /// (Stufe P4a): Der Bericht löst sie in seiner Sprache auf — in Englisch ohne die deutschen Marken.
        /// </summary>
        [Fact]
        public void Herkunftstexte_stehen_in_der_Sprache_des_Berichts()
        {
            PufferAuslegungGespeichert g = Probe() with
            {
                BemessendHerkunft = HeizzoneRechner.HERKUNFT_K4,
                NutzungsprofilHerkunft = Nutzungsprofil.Ableiten(new[] { "Mehrfamilienhaus" }, false, null).HerkunftBaustein
            };
            string de = string.Join("\n", Schreibe(Daten(new List<PufferAuslegungGespeichert> { g })));
            Assert.Contains("VDI 4645 E 2026-03, Gleichung 23 mit Tabellen 14 und 15", de);
            Assert.Contains(R.PAUS_NP_WOHNEN + " — Zapf-Nutzungsart „Mehrfamilienhaus“", de);

            string en;
            using (BerichtTexte.ImLauf(true)) en = string.Join("\n", Schreibe(Daten(new List<PufferAuslegungGespeichert> { g })));
            Assert.Contains("VDI 4645 draft 2026-03, equation 23 with tables 14 and 15", en);
            Assert.Contains("Draw-off use type “Mehrfamilienhaus”", en);
            foreach (string marke in new[] { "Gleichung", "Tabellen", "Zapf-Nutzungsart", "Wärmespeicher-Tool", "Vorgabe" })
                Assert.DoesNotContain(marke, en);
        }

        // =============================================================================
        //  Testdatenbank: Projektkopie, Auslegung speichern, lesen, Bericht
        // =============================================================================

        [Fact]
        public void Gespeichert_liest_die_Zeile_der_Projektkopie_und_der_Bericht_zeigt_sie()
        {
            if (!_db.Vorhanden) return;
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("@p", P_ZAPF)));
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Pufferbericht");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int puffer = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = (SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?)",
                new DbParam("@k", kopie), new DbParam("@p", PUFFER_1045_KOMBI)));

            // Ohne gespeicherte Zeile: nichts — der Abschnitt entfällt.
            Assert.Empty(PufferAuslegungCtrl.Gespeichert(kopie));

            PufferAuslegungErgebnis a = PufferAuslegungCtrl.Durchrechnen(kopie, puffer, out PufferAuslegungVorbelegung v, out string fehler);
            Assert.True(a != null, fehler);
            Assert.True(PufferAuslegungCtrl.Speichern(kopie, puffer, v.Eingang, a) > 0);

            IReadOnlyList<PufferAuslegungGespeichert> zeilen = PufferAuslegungCtrl.Gespeichert(kopie);
            PufferAuslegungGespeichert g = Assert.Single(zeilen);
            Assert.Equal(puffer, g.IdPuffer);
            Assert.False(string.IsNullOrWhiteSpace(g.Puffername));
            Assert.True(g.KlasseHeizung && g.KlasseBrauchwasser);
            Assert.Equal(a.EmpfehlungL, g.EmpfehlungL);
            Assert.Equal(a.Zone(PufferZone.Brauchwasser)?.VolumenL, g.VolumenBrauchwasserL);
            Assert.Equal(a.Bemessend, g.Bemessend);
            Assert.NotNull(g.BerechnetAm);
            Assert.Null(g.Fehlertext);
            Assert.Equal(a.EmpfehlungL, g.NachgerechnetL);            // gleicher Stand → gleiche Nachrechnung
            Assert.Equal(a.Kennzahlen.Verlust?.KwhJeTag, g.VerlustKwhJeTag);
            Assert.Equal(a.Warnungen.Select(w => w.Code), g.Warnungen.Select(w => w.Code));
            Assert.Equal(v.Eingang.Vorlage, g.Vorlage);

            // Ohne Nachrechnung: nur die gespeicherten Spalten.
            PufferAuslegungGespeichert roh = Assert.Single(PufferAuslegungCtrl.Gespeichert(kopie, nachrechnen: false));
            Assert.Null(roh.NachgerechnetL);
            Assert.Empty(roh.Warnungen);
            Assert.Equal(g.EmpfehlungL, roh.EmpfehlungL);

            string[] t = Schreibe(Daten(zeilen.ToList()));
            Assert.Contains(R.PAUS_TITEL, t);
            Assert.Contains(g.Puffername, t);
        }

        // =============================================================================
        //  Platzhalter {{tabelle.pufferauslegung}} (Katalog v12, Welle P4c)
        // =============================================================================

        /// <summary>
        /// Steht der Platzhalter in der Vorlage, schreibt er die Tafel an seiner Stelle, und der Baustein des Kapitels
        /// Projekt lässt seinen Abschnitt weg; ohne Platzhalter schreibt der Baustein wie bisher (Gegenprobe).
        /// </summary>
        [Fact]
        public void Der_Platzhalter_schreibt_die_Tafel_an_seiner_Stelle_und_der_Baustein_schweigt()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-pa-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                var konfig = new BerichtsKonfiguration();
                konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_PROJEKT);

                (string[] Absaetze, string[] Zellen) Fuelle(params string[] absaetze)
                {
                    byte[] vorlage = Probevorlagen.AusAbsaetzen(absaetze);
                    string ziel = Path.Combine(ordner, Guid.NewGuid().ToString("N") + ".docx");
                    new WordBerichtGenerator().ErzeugeMitVorlage(Daten(new List<PufferAuslegungGespeichert> { Probe() }), konfig,
                                                                 vorlage, new Erstellerangaben(), ziel);
                    using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                    Body body = doc.MainDocumentPart!.Document.Body!;
                    return (body.Elements<Paragraph>().Select(p => p.InnerText).ToArray(),
                            body.Descendants<TableCell>().Select(c => c.InnerText).ToArray());
                }

                var mit = Fuelle("{{kapitel.projekt}}", "{{tabelle.pufferauslegung}}");
                Assert.Contains("Speicher 1", mit.Zellen);
                Assert.Contains(R.PAUS_EMPFEHLUNG, mit.Zellen);
                Assert.DoesNotContain(R.BER_PAUS_EINLEITUNG, mit.Absaetze);
                Assert.DoesNotContain(R.PAUS_TITEL, mit.Absaetze);

                var ohne = Fuelle("{{kapitel.projekt}}");
                Assert.Contains(R.BER_PAUS_EINLEITUNG, ohne.Absaetze);
                Assert.Contains(R.PAUS_TITEL, ohne.Absaetze);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { }
            }
        }
    }
}
