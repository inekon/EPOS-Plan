using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wertesatz und die Auflösung der Platzhalter</b> (Konzept Berichtsvorlagen 4.8 bis
    /// 4.10, 5.1; Etappe BV-E1): <see cref="Berichtswerte"/> und
    /// <see cref="Vorlagenfeldkatalog.Loese(Vorlagenfeld, Berichtswerte, IReadOnlyList{Formatangabe})"/>.
    ///
    /// <para><b>Alle Schlüssel</b> werden gegen die Proben der Berichtswachen aufgelöst
    /// (<see cref="Berichtsdatenproben.Projektdaten1030"/> und
    /// <see cref="Berichtsdatenproben.Gruppendaten"/>), deutsch und englisch; die Kennzahlen
    /// rechnet der Fall wie der Sammler über <see cref="KennzahlenKatalog.Berechne"/>. Die
    /// gefüllte Probe trägt dazu Projektdaten, Klimaregion, Simulationsstand und Warnungen und
    /// prüft jeden handgepflegten Schlüssel auf den Wortlaut.</para>
    ///
    /// <para><b>Sammlung „Testdatenbank“</b>, weil die Probe 1030 simuliert; die Kultur ist
    /// gepinnt, obwohl die Auflösung sie nicht liest — ein Fall prüft gerade das.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtswerteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public BerichtswerteTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        private static readonly DateTime Erstellt = new DateTime(2026, 9, 25, 14, 30, 0);
        private static readonly DateTime Angelegt = new DateTime(2026, 1, 15, 9, 0, 0);
        private static readonly DateTime Geaendert = new DateTime(2026, 9, 20, 10, 0, 0);
        private static readonly DateTime Simuliert = new DateTime(2026, 9, 24, 8, 5, 0);

        private static readonly IReadOnlyList<Formatangabe> Ohne = Array.Empty<Formatangabe>();

        // =====================================================================
        //  Proben
        // =====================================================================

        /// <summary>Die Kennzahlen jedes Stands, wie der Sammler sie rechnet.</summary>
        private static BerichtsDaten MitKennzahlen(BerichtsDaten daten)
        {
            foreach (VariantenDaten v in daten.Varianten) KennzahlenKatalog.Berechne(v);
            return daten;
        }

        /// <summary>Die Gruppe mit drei Ständen, dazu Projektdaten, Klimaregion, Simulationsstand,
        /// Berichtsdatum und zwei Warnungen am Stamm.</summary>
        private static BerichtsDaten GefuellteProbe()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(3);
            daten.ErstelltAm = Erstellt;
            daten.Warnungen.Add("Erster Hinweis");
            daten.Warnungen.Add("Zweiter Hinweis\r\nmit zweiter Zeile");

            VariantenDaten stamm = daten.Varianten[0];
            stamm.Projekt = new ProjektModel
            {
                m_szProjektname = "Musterquartier",
                m_szKunde = "Muster AG",
                m_szBearbeiter = "Erika Muster",
                m_szBeschreibung = "Erste Zeile\r\nZweite Zeile",
                m_Erstelldatum = Angelegt,
                m_Aenderungsdatum = Geaendert,
            };
            stamm.Details = new ProjektDetails { KlimaregionName = "Region 5" };
            stamm.SimulationsStand = Simuliert;
            return MitKennzahlen(daten);
        }

        private static Berichtswerte Werte(BerichtsDaten daten, bool englisch, Erstellerangaben ersteller = null)
        {
            return Berichtswerte.Aus(daten, Berichtsdatenproben.VolleKonfiguration(), englisch,
                                     ersteller ?? new Erstellerangaben { Firma = "Planungsbüro Beispiel" });
        }

        private static Platzhalterwert Wert(Berichtswerte werte, string platzhalter)
        {
            Platzhalter p = Platzhaltersyntax.Lies(platzhalter);
            Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(p.Schluessel);
            Assert.True(feld != null, "Unbekannter Schlüssel: " + p.Schluessel);
            return Vorlagenfeldkatalog.Loese(feld, werte, p.Angaben);
        }

        private static string Text(Berichtswerte werte, string platzhalter) { return Wert(werte, platzhalter).Text; }

        private static string Ressource(string name, bool englisch)
        {
            return R.ResourceManager.GetString(name, englisch ? En : De);
        }

        // =====================================================================
        //  1 — alle Schlüssel gegen die Proben der Berichtswachen
        // =====================================================================

        [Fact]
        public void Alle_Schluessel_loesen_sich_fuer_das_Referenzprojekt_1030_auf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = MitKennzahlen(Berichtsdatenproben.Projektdaten1030());
            int mitWert = 0;
            foreach (bool englisch in new[] { false, true })
                mitWert += PruefeAlle(Werte(daten, englisch));
            _ausgabe.WriteLine("1030: " + Vorlagenfeldkatalog.Alle.Count + " Schlüssel je Sprache, davon mit Wert (beide Sprachen): " + mitWert);

            Berichtswerte de = Werte(daten, false);
            Assert.Equal("Referenzprojekt 1030", Text(de, "{{bericht.titel}}"));
            Assert.Equal("Referenzprojekt 1030", Text(de, "{{projekt.name}}"));

            // Der Lauf hat gerechnet: Die Wärmebilanz trägt Zahlen, keine Striche.
            Platzhalterwert waerme = Wert(de, "{{stamm.kennzahl.energie.waermebedarf}}");
            Assert.False(waerme.IstLeer);
            Assert.True(waerme.Zahl > 0);
            Assert.EndsWith(" MWh/a", waerme.Text);

            // Die Probe führt keine Projektdaten: leer mit Grund, nie 0.
            Platzhalterwert kunde = Wert(de, "{{projekt.kunde}}");
            Assert.True(kunde.IstLeer);
            Assert.Equal("", kunde.Text);
            Assert.Equal(Ressource(nameof(R.BV_GRUND_KEINE_PROJEKTDATEN), false), kunde.Grund);
        }

        [Fact]
        public void Alle_Schluessel_loesen_sich_fuer_die_synthetische_Gruppe_auf()
        {
            foreach (int staende in new[] { 1, 3, 7 })
            {
                BerichtsDaten daten = MitKennzahlen(Berichtsdatenproben.Gruppendaten(staende));
                foreach (bool englisch in new[] { false, true })
                {
                    PruefeAlle(Werte(daten, englisch));
                    PruefeAlle(Berichtswerte.Aus(daten, null, englisch, null));
                }
            }
        }

        [Fact]
        public void Alle_Schluessel_loesen_sich_fuer_die_gefuellte_Probe_auf()
        {
            BerichtsDaten daten = GefuellteProbe();
            foreach (bool englisch in new[] { false, true })
                PruefeAlle(Werte(daten, englisch));
        }

        /// <summary>
        /// Löst jeden Schlüssel auf und prüft, was für alle gilt: keine Ausnahme, Art wie im
        /// Katalog, ohne Wert der Leerwert (nie 0), eine Stammkennzahl genau mit Wert, Format und
        /// Einheit ihres Verzeichnisses, Beschriftung und Einheit wie der Kennzahlenkatalog im Modus
        /// des Laufs. Liefert die Zahl der Schlüssel mit Wert.
        /// </summary>
        private static int PruefeAlle(Berichtswerte werte)
        {
            CultureInfo kultur = werte.Englisch ? En : De;
            Assert.Equal(kultur, werte.Kultur);
            int mitWert = 0;

            foreach (Vorlagenfeld feld in Vorlagenfeldkatalog.Alle)
            {
                Platzhalterwert w = Vorlagenfeldkatalog.Loese(feld, werte, Ohne);
                string wo = feld.Schluessel + (werte.Englisch ? " (en)" : " (de)");

                Assert.NotNull(w);
                Assert.True(w.Ausnahme == null, wo + ": " + w.Ausnahme);
                Assert.Equal(feld.Art, w.Art);
                Assert.NotNull(w.Text);
                if (w.IstLeer) Assert.True(w.Text == feld.Leerwert, wo + ": Leerwert erwartet, war „" + w.Text + "“");
                else mitWert++;

                if (feld.Art == Vorlagenfeldart.Zahl && !w.IstLeer)
                {
                    Assert.True(w.Zahl.HasValue, wo);
                    Assert.StartsWith(w.Zahl.Value.ToString(feld.Format ?? "N0", kultur), w.Text);
                }
                if (feld.Art == Vorlagenfeldart.Zahl && w.IstLeer) Assert.Null(w.Zahl);
                if (feld.Art == Vorlagenfeldart.Datum) Assert.Equal(w.IstLeer, !w.Datum.HasValue);

                Vorlagenfeldableitung a = feld.Ableitung;
                if (a == null) continue;
                // Die Muster der Fassung 3 (Standwerte, Paarsicht, Wirtschaftlichkeit) prüft VorlagenfeldStandwerteTests.
                if (a.Muster != Vorlagenfeldkatalog.MUSTER_STAMM_KENNZAHL && a.Muster != Vorlagenfeldkatalog.MUSTER_KENNZAHL_BESCHRIFTUNG &&
                    a.Muster != Vorlagenfeldkatalog.MUSTER_KENNZAHL_EINHEIT) continue;
                Kennzahl k = werte.FindeKennzahl(a.Parameter);
                Assert.NotNull(k);

                if (a.Muster == Vorlagenfeldkatalog.MUSTER_STAMM_KENNZAHL)
                {
                    double? erwartet = werte.Stamm != null && werte.Stamm.Kennzahlen.TryGetValue(k.Schluessel, out double? x) ? x : null;
                    if (!erwartet.HasValue)
                    {
                        Assert.True(w.IstLeer, wo);
                        Assert.Equal(Vorlagenfeld.STRICH, w.Text);
                        Assert.False(string.IsNullOrEmpty(w.Grund), wo + ": ohne Grund");
                    }
                    else
                    {
                        string einheit = k.Einheit == "–" ? "" : " " + k.Einheit;
                        Assert.Equal(erwartet.Value.ToString(k.Format, kultur) + einheit, w.Text);
                        Assert.Equal(erwartet.Value, w.Zahl);
                    }
                }
                else if (a.Muster == Vorlagenfeldkatalog.MUSTER_KENNZAHL_BESCHRIFTUNG)
                    Assert.Equal(k.Label(werte.Englisch), w.Text);
                else if (a.Muster == Vorlagenfeldkatalog.MUSTER_KENNZAHL_EINHEIT)
                    Assert.Equal(k.Einheit, w.Text);
                else
                    Assert.Fail(wo + ": unbekanntes Muster " + a.Muster);
            }
            return mitWert;
        }

        // =====================================================================
        //  2 — der Wortlaut jedes handgepflegten Schlüssels
        // =====================================================================

        [Fact]
        public void Gefuellte_Probe_deutsch()
        {
            Berichtswerte w = Werte(GefuellteProbe(), false);

            Assert.Equal("Stammprojekt", Text(w, "{{bericht.titel}}"));
            Assert.Equal("Variantenvergleich — Energie- und Wärmeversorgung", Text(w, "{{bericht.untertitel}}"));
            Assert.Equal("25.09.2026", Text(w, "{{bericht.datum}}"));
            Assert.Equal(Erstellt, Wert(w, "{{bericht.datum}}").Datum);
            Assert.Equal("Variante A, Variante B", Text(w, "{{bericht.varianten.liste}}"));
            Assert.Equal("2", Text(w, "{{bericht.varianten.anzahl}}"));
            Assert.Equal(2.0, Wert(w, "{{bericht.varianten.anzahl}}").Zahl);
            Assert.Equal("CO₂-Emissionen", Text(w, "{{bericht.emissionsmodus}}"));

            Platzhalterwert ausweis = Wert(w, "{{bericht.gebaeudemodell.ausweis}}");
            Assert.True(ausweis.IstLeer);
            Assert.Equal("—", ausweis.Text);
            Assert.Equal("kein Gebäude nach VDI 6007 gerechnet", ausweis.Grund);

            Platzhalterwert warnungen = Wert(w, "{{bericht.warnungen}}");
            Assert.False(warnungen.IstLeer);
            Assert.Equal(new[] { "Erster Hinweis", "Zweiter Hinweis\nmit zweiter Zeile" }, warnungen.Zeilen);
            Assert.Equal("Erster Hinweis\nZweiter Hinweis\nmit zweiter Zeile", warnungen.Text);

            Platzhalterwert inhalt = Wert(w, "{{bericht.inhalt}}");
            Assert.Equal(Berichtskapitel.Alle.Select(k => k.Name), inhalt.Kapitel);
            Assert.Equal("", inhalt.Text);

            Assert.Equal("Seite", Text(w, "{{text.seite}}"));
            Assert.Equal("Kunde", Text(w, "{{text.kunde}}"));
            Assert.Equal("Bearbeiter", Text(w, "{{text.bearbeiter}}"));
            Assert.Equal("Ersteller", Text(w, "{{text.ersteller}}"));
            Assert.Equal("Verglichene Varianten", Text(w, "{{text.varianten}}"));
            Assert.Equal("Berichtsdatum", Text(w, "{{text.datum}}"));
            Assert.Equal("Erstellt mit", Text(w, "{{text.erstellt_mit}}"));

            Assert.Equal("Planungsbüro Beispiel", Text(w, "{{ersteller.firma}}"));
            Assert.Equal("EPOS-Plan", Text(w, "{{ersteller.programm}}"));
            Assert.Equal(DeckblattBaustein.ProduktFassung(), Text(w, "{{ersteller.version}}"));

            Assert.Equal("Musterquartier", Text(w, "{{projekt.name}}"));
            Assert.Equal("Muster AG", Text(w, "{{projekt.kunde}}"));
            Assert.Equal("Erika Muster", Text(w, "{{projekt.bearbeiter}}"));
            Assert.Equal("Erste Zeile\nZweite Zeile", Text(w, "{{projekt.beschreibung}}"));
            Assert.Equal("Region 5", Text(w, "{{projekt.klimaregion}}"));
            Assert.Equal("15.01.2026", Text(w, "{{projekt.angelegt}}"));
            Assert.Equal("20.09.2026", Text(w, "{{projekt.geaendert}}"));
            Assert.Equal("24.09.2026 08:05", Text(w, "{{projekt.simulationsstand}}"));

            // Stammkennzahlen der Probe: Energiekosten 12 000 €, Speichertemperatur 62 °C, keine WP.
            Assert.Equal("12.000 €/a", Text(w, "{{stamm.kennzahl.ko.energie}}"));
            Assert.Equal("62,0 °C", Text(w, "{{stamm.kennzahl.eff.t_oben_mittel}}"));
            Platzhalterwert jaz = Wert(w, "{{stamm.kennzahl.eff.jaz}}");
            Assert.True(jaz.IstLeer);
            Assert.Equal("—", jaz.Text);
            Assert.Equal("für dieses Projekt nicht verfügbar", jaz.Grund);

            Assert.Equal("Jahresarbeitszahl (JAZ) WP", Text(w, "{{kennzahl.eff.jaz.beschriftung}}"));
            Assert.Equal("–", Text(w, "{{kennzahl.eff.jaz.einheit}}"));
            Assert.Equal("€/a", Text(w, "{{kennzahl.ko.energie.einheit}}"));
        }

        [Fact]
        public void Gefuellte_Probe_englisch()
        {
            Berichtswerte w = Werte(GefuellteProbe(), true);

            Assert.Equal("Variant comparison — energy and heat supply", Text(w, "{{bericht.untertitel}}"));
            Assert.Equal("9/25/2026", Text(w, "{{bericht.datum}}"));
            Assert.Equal("Variante A, Variante B", Text(w, "{{bericht.varianten.liste}}"));
            Assert.Equal("CO₂ emissions", Text(w, "{{bericht.emissionsmodus}}"));
            Assert.Equal("no building calculated according to VDI 6007", Wert(w, "{{bericht.gebaeudemodell.ausweis}}").Grund);

            Assert.Equal("Page", Text(w, "{{text.seite}}"));
            Assert.Equal("Customer", Text(w, "{{text.kunde}}"));
            Assert.Equal("Editor", Text(w, "{{text.bearbeiter}}"));
            Assert.Equal("Prepared by", Text(w, "{{text.ersteller}}"));
            Assert.Equal("Compared variants", Text(w, "{{text.varianten}}"));
            Assert.Equal("Report date", Text(w, "{{text.datum}}"));
            Assert.Equal("Created with", Text(w, "{{text.erstellt_mit}}"));

            Assert.Equal("1/15/2026", Text(w, "{{projekt.angelegt}}"));
            Assert.Equal("9/20/2026", Text(w, "{{projekt.geaendert}}"));
            Assert.Equal(Simuliert.ToString("g", En), Text(w, "{{projekt.simulationsstand}}"));

            Assert.Equal("12,000 €/a", Text(w, "{{stamm.kennzahl.ko.energie}}"));
            Assert.Equal("62.0 °C", Text(w, "{{stamm.kennzahl.eff.t_oben_mittel}}"));
            Assert.Equal("not available for this project", Wert(w, "{{stamm.kennzahl.eff.jaz}}").Grund);
            Assert.Equal("Heat pump SPF", Text(w, "{{kennzahl.eff.jaz.beschriftung}}"));
        }

        [Fact]
        public void Nur_das_Stammprojekt_nennt_den_Rueckfall_und_zaehlt_null_Varianten()
        {
            BerichtsDaten daten = MitKennzahlen(Berichtsdatenproben.Gruppendaten(1));
            Berichtswerte de = Werte(daten, false);
            Assert.Equal("— (nur Stammprojekt)", Text(de, "{{bericht.varianten.liste}}"));
            Assert.False(Wert(de, "{{bericht.varianten.liste}}").IstLeer);

            // Null Varianten sind eine Zahl, kein Leerwert.
            Platzhalterwert anzahl = Wert(de, "{{bericht.varianten.anzahl}}");
            Assert.False(anzahl.IstLeer);
            Assert.Equal("0", anzahl.Text);

            Assert.Equal("— (base project only)", Text(Werte(daten, true), "{{bericht.varianten.liste}}"));
        }

        // =====================================================================
        //  3 — Leerwerte (Konzept 4.10)
        // =====================================================================

        [Fact]
        public void Ohne_Projektdaten_leer_mit_Grund_der_Name_faellt_auf_den_Baum_zurueck()
        {
            Berichtswerte w = Werte(MitKennzahlen(Berichtsdatenproben.Gruppendaten(2)), false);
            string grund = "keine Projektdaten";

            Assert.Equal("Stammprojekt", Text(w, "{{projekt.name}}"));
            foreach (string s in new[] { "projekt.kunde", "projekt.bearbeiter", "projekt.beschreibung" })
            {
                Platzhalterwert p = Wert(w, "{{" + s + "}}");
                Assert.True(p.IstLeer, s);
                Assert.Equal("", p.Text);        // Leerwert leer
                Assert.Equal(grund, p.Grund);
            }
            foreach (string s in new[] { "projekt.klimaregion", "projekt.angelegt", "projekt.geaendert" })
            {
                Platzhalterwert p = Wert(w, "{{" + s + "}}");
                Assert.True(p.IstLeer, s);
                Assert.Equal("—", p.Text);       // Leerwert Strich
                Assert.Equal(grund, p.Grund);
            }
            Platzhalterwert stand = Wert(w, "{{projekt.simulationsstand}}");
            Assert.Equal("—", stand.Text);
            Assert.Equal("kein Simulationsergebnis", stand.Grund);

            // Ohne Firma bleibt die Zeile leer statt mit Strich.
            Platzhalterwert firma = Wert(Werte(MitKennzahlen(Berichtsdatenproben.Gruppendaten(2)), false, new Erstellerangaben()),
                                         "{{ersteller.firma}}");
            Assert.True(firma.IstLeer);
            Assert.Equal("", firma.Text);
        }

        [Fact]
        public void Leere_Texte_im_Projekt_sind_leer_ohne_Grund()
        {
            BerichtsDaten daten = GefuellteProbe();
            daten.Varianten[0].Projekt.m_szKunde = "   ";
            daten.Varianten[0].Details.KlimaregionName = "";
            Berichtswerte w = Werte(daten, false);

            Platzhalterwert kunde = Wert(w, "{{projekt.kunde}}");
            Assert.True(kunde.IstLeer);
            Assert.Equal("", kunde.Text);
            Assert.Null(kunde.Grund);

            Platzhalterwert region = Wert(w, "{{projekt.klimaregion}}");
            Assert.Equal("—", region.Text);
            Assert.Null(region.Grund);
        }

        [Fact]
        public void Ohne_Stamm_nennen_Stammwerte_den_Grund()
        {
            BerichtsDaten daten = MitKennzahlen(Berichtsdatenproben.Gruppendaten(3));
            daten.Varianten.RemoveAt(0);
            Berichtswerte w = Werte(daten, false);

            Assert.Null(w.Stamm);
            Assert.Equal(2, w.Varianten.Count);
            Assert.Equal("kein Stammprojekt", Wert(w, "{{stamm.kennzahl.ko.energie}}").Grund);
            Assert.Equal("kein Stammprojekt", Wert(w, "{{projekt.kunde}}").Grund);
            Assert.Equal("kein Stammprojekt", Wert(w, "{{projekt.klimaregion}}").Grund);
            Assert.Equal("kein Stammprojekt", Wert(w, "{{projekt.simulationsstand}}").Grund);
            Assert.Equal("Stammprojekt", Text(w, "{{projekt.name}}"));   // der Name des Baums
        }

        [Fact]
        public void Der_Grund_einer_fehlenden_Kennzahl_folgt_dem_Stand()
        {
            BerichtsDaten daten = MitKennzahlen(Berichtsdatenproben.Gruppendaten(2));
            VariantenDaten stamm = daten.Varianten[0];

            stamm.Fehler = "Simulation abgebrochen";
            Assert.Equal("Lauf fehlgeschlagen", Wert(Werte(daten, false), "{{stamm.kennzahl.eff.jaz}}").Grund);
            Assert.Equal("run failed", Wert(Werte(daten, true), "{{stamm.kennzahl.eff.jaz}}").Grund);

            stamm.Fehler = null;
            stamm.Ergebnis = null;
            Assert.Equal("kein Simulationsergebnis", Wert(Werte(daten, false), "{{stamm.kennzahl.eff.jaz}}").Grund);

            // Ein Wert im Verzeichnis gilt, auch wenn der Baum sonst nichts trägt.
            Assert.Equal("12.000 €/a", Text(Werte(daten, false), "{{stamm.kennzahl.ko.energie}}"));
        }

        [Fact]
        public void Nicht_endliche_Zahlen_sind_leer()
        {
            BerichtsDaten daten = MitKennzahlen(Berichtsdatenproben.Gruppendaten(2));
            daten.Varianten[0].Kennzahlen["ko.energie"] = double.NaN;
            daten.Varianten[0].Kennzahlen["ko.stromsaldo"] = double.PositiveInfinity;
            Berichtswerte w = Werte(daten, false);

            Assert.Equal("—", Text(w, "{{stamm.kennzahl.ko.energie}}"));
            Assert.Equal("— (für dieses Projekt nicht verfügbar)", Text(w, "{{stamm.kennzahl.ko.stromsaldo|mit grund}}"));
        }

        // =====================================================================
        //  4 — Formatangaben (Konzept 4.8)
        // =====================================================================

        [Fact]
        public void Formatangaben_einer_Zahl()
        {
            BerichtsDaten daten = GefuellteProbe();
            daten.Varianten[0].Kennzahlen["eff.jaz"] = 3.456;
            Berichtswerte de = Werte(daten, false);
            Berichtswerte en = Werte(daten, true);

            Assert.Equal("12.000 €/a", Text(de, "{{stamm.kennzahl.ko.energie}}"));
            Assert.Equal("12.000,00 €/a", Text(de, "{{stamm.kennzahl.ko.energie|stellen 2}}"));
            Assert.Equal("12,000.00 €/a", Text(en, "{{stamm.kennzahl.ko.energie|stellen 2}}"));
            Assert.Equal("12.000", Text(de, "{{stamm.kennzahl.ko.energie|ohne einheit}}"));
            Assert.Equal("12.000,0", Text(de, "{{stamm.kennzahl.ko.energie|stellen 1|ohne einheit}}"));
            Assert.Equal("12.000 €/a", Text(de, "{{stamm.kennzahl.ko.energie|mit einheit}}"));
            Assert.Equal("12.000 €/a", Text(de, "{{stamm.kennzahl.ko.energie|ohne einheit|mit einheit}}"));
            Assert.Equal("62 °C", Text(de, "{{stamm.kennzahl.eff.t_oben_mittel|stellen 0}}"));

            // Die dimensionslose Kennzahl trägt „–“ als Einheit — angehängt wird nichts.
            Assert.Equal("3,46", Text(de, "{{stamm.kennzahl.eff.jaz}}"));
            Assert.Equal("3.5", Text(en, "{{stamm.kennzahl.eff.jaz|stellen 1}}"));
            Assert.Equal(3.456, Wert(de, "{{stamm.kennzahl.eff.jaz|stellen 1}}").Zahl);
        }

        [Fact]
        public void Unpassende_und_unbekannte_Angaben_uebergeht_die_Aufloesung()
        {
            Berichtswerte w = Werte(GefuellteProbe(), false);
            Assert.Equal("12.000 €/a", Text(w, "{{stamm.kennzahl.ko.energie|datum lang}}"));
            Assert.Equal("12.000 €/a", Text(w, "{{stamm.kennzahl.ko.energie|fett}}"));
            Assert.Equal("12.000 €/a", Text(w, "{{stamm.kennzahl.ko.energie|ohne titel}}"));
            Assert.Equal("25.09.2026", Text(w, "{{bericht.datum|stellen 2}}"));
            Assert.Equal("Muster AG", Text(w, "{{projekt.kunde|ohne einheit}}"));
        }

        [Fact]
        public void Formatangaben_eines_Datums()
        {
            Berichtswerte de = Werte(GefuellteProbe(), false);
            Berichtswerte en = Werte(GefuellteProbe(), true);

            Assert.Equal("25.09.2026", Text(de, "{{bericht.datum|datum}}"));
            Assert.Equal(Erstellt.ToString("D", De), Text(de, "{{bericht.datum|datum lang}}"));
            Assert.Contains("September 2026", Text(de, "{{bericht.datum|datum lang}}"));
            Assert.Equal(Erstellt.ToString("D", En), Text(en, "{{bericht.datum|datum lang}}"));
            Assert.Contains("September 25, 2026", Text(en, "{{bericht.datum|datum lang}}"));
            Assert.Equal("25.09.2026 14:30", Text(de, "{{bericht.datum|datum mit zeit}}"));
            Assert.Equal(Erstellt.ToString("g", En), Text(en, "{{bericht.datum|datum mit zeit}}"));

            // Der Simulationsstand trägt die Uhrzeit von sich aus; |datum nimmt sie weg.
            Assert.Equal("24.09.2026", Text(de, "{{projekt.simulationsstand|datum}}"));
            Assert.Equal(Angelegt.ToString("D", De), Text(de, "{{projekt.angelegt|datum lang}}"));
        }

        [Fact]
        public void Leer_statt_strich_und_mit_grund()
        {
            Berichtswerte de = Werte(GefuellteProbe(), false);
            Berichtswerte en = Werte(GefuellteProbe(), true);

            Assert.Equal("—", Text(de, "{{stamm.kennzahl.eff.jaz}}"));
            Assert.Equal("", Text(de, "{{stamm.kennzahl.eff.jaz|leer statt strich}}"));
            Assert.Equal("— (für dieses Projekt nicht verfügbar)", Text(de, "{{stamm.kennzahl.eff.jaz|mit grund}}"));
            Assert.Equal("— (not available for this project)", Text(en, "{{stamm.kennzahl.eff.jaz|mit grund}}"));
            Assert.Equal("(für dieses Projekt nicht verfügbar)", Text(de, "{{stamm.kennzahl.eff.jaz|leer statt strich|mit grund}}"));

            // „mit grund“ gilt nur für Zahlen; beim Text bleibt es beim Leerwert.
            Assert.Equal("—", Text(de, "{{bericht.gebaeudemodell.ausweis|mit grund}}"));
            Assert.Equal("", Text(de, "{{bericht.gebaeudemodell.ausweis|leer statt strich}}"));

            // Ein Wert bleibt ein Wert.
            Assert.Equal("12.000 €/a", Text(de, "{{stamm.kennzahl.ko.energie|leer statt strich|mit grund}}"));

            BerichtsDaten ohneStand = GefuellteProbe();
            ohneStand.Varianten[0].SimulationsStand = null;
            Assert.Equal("", Text(Werte(ohneStand, false), "{{projekt.simulationsstand|leer statt strich}}"));
        }

        // =====================================================================
        //  5 — Alias, Ersteller, Emissionsmodus, Kapitel, Produktausweis
        // =====================================================================

        [Fact]
        public void Der_Alias_bericht_programmversion_ist_ersteller_version()
        {
            var ersteller = new Erstellerangaben { Firma = "F", Programm = "EPOS-Plan", Version = "1.2.0.4" };
            Berichtswerte w = Werte(GefuellteProbe(), false, ersteller);

            Assert.Same(Vorlagenfeldkatalog.Finde("ersteller.version"), Vorlagenfeldkatalog.Finde("bericht.programmversion"));
            Assert.Equal("1.2.0.4", Text(w, "{{bericht.programmversion}}"));
            Assert.Equal("1.2.0.4", Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{Bericht.Programmversion}}"), w).Text);
        }

        [Fact]
        public void Programm_und_Fassung_ergaenzt_der_Kern_das_Original_bleibt_unberuehrt()
        {
            var ersteller = new Erstellerangaben { Firma = "Planungsbüro" };
            Berichtswerte w = Werte(GefuellteProbe(), false, ersteller);

            Assert.Equal(Erstellerangaben.PROGRAMM, w.Ersteller.Programm);
            Assert.Equal(DeckblattBaustein.ProduktFassung(), w.Ersteller.Version);
            Assert.Equal("Planungsbüro", w.Ersteller.Firma);
            Assert.Null(ersteller.Programm);
            Assert.Null(ersteller.Version);
            Assert.NotSame(ersteller, w.Ersteller);

            Berichtswerte ohne = Berichtswerte.Aus(GefuellteProbe(), null, false, null);
            Assert.Null(ohne.Ersteller.Firma);
            Assert.Equal("EPOS-Plan", Text(ohne, "{{ersteller.programm}}"));

            var eigen = new Erstellerangaben { Programm = "EPOS-Plan Server", Version = "9.9" };
            Berichtswerte mit = Werte(GefuellteProbe(), false, eigen);
            Assert.Equal("EPOS-Plan Server", Text(mit, "{{ersteller.programm}}"));
            Assert.Equal("9.9", Text(mit, "{{ersteller.version}}"));
        }

        [Fact]
        public void Die_Beschriftungen_der_Emissionskennzahlen_folgen_dem_Modus_des_Laufs()
        {
            BerichtsDaten daten = GefuellteProbe();
            foreach (VariantenDaten v in daten.Varianten) v.EmissionsModus = DbWerte.EMISSION_MODUS_CO2E;

            Berichtswerte de = Werte(daten, false);
            Assert.Equal(DbWerte.EMISSION_MODUS_CO2E, de.Emissionsmodus);
            Assert.Equal(EmissionsAusweis.KennzahlGesamt(DbWerte.EMISSION_MODUS_CO2E, false), Text(de, "{{kennzahl.em.co2.beschriftung}}"));
            Assert.Equal(EmissionsAusweis.KennzahlSpezifisch(DbWerte.EMISSION_MODUS_CO2E, true),
                         Text(Werte(daten, true), "{{kennzahl.em.co2_spez.beschriftung}}"));
            Assert.Equal(EmissionsAusweis.KennzahlKaeltestrom(DbWerte.EMISSION_MODUS_CO2E, false), Text(de, "{{kennzahl.kaelte.co2.beschriftung}}"));
            Assert.Equal("CO₂-Äquivalent (GWP₁₀₀)", Text(de, "{{bericht.emissionsmodus}}"));
            Assert.NotEqual(Text(Werte(GefuellteProbe(), false), "{{kennzahl.em.co2.beschriftung}}"),
                            Text(de, "{{kennzahl.em.co2.beschriftung}}"));

            // Verschiedene Modi je Stand: gemischt ausgewiesen.
            daten.Varianten[1].EmissionsModus = DbWerte.EMISSION_MODUS_CO2;
            Berichtswerte gemischt = Werte(daten, false);
            Assert.Equal(EmissionsAusweis.MODUS_GEMISCHT, gemischt.Emissionsmodus);
            Assert.Equal(EmissionsAusweis.Groesse(EmissionsAusweis.MODUS_GEMISCHT, false), Text(gemischt, "{{bericht.emissionsmodus}}"));
        }

        [Fact]
        public void Der_Sammelanker_folgt_den_Haekchen_in_Berichtsreihenfolge()
        {
            var konfig = new BerichtsKonfiguration();
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_ANHANG);
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_DECKBLATT);
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_VERGLEICH);
            Berichtswerte w = Berichtswerte.Aus(GefuellteProbe(), konfig, false, null);

            Platzhalterwert inhalt = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("bericht.inhalt"), w, Ohne);
            Assert.Equal(new[] { Berichtskapitel.DECKBLATT, Berichtskapitel.VERGLEICH, Berichtskapitel.ANHANG },
                         inhalt.Kapitel);

            Platzhalterwert keins = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("bericht.inhalt"),
                                                              Berichtswerte.Aus(GefuellteProbe(), new BerichtsKonfiguration(), false, null), Ohne);
            Assert.True(keins.IstLeer);
            Assert.Empty(keins.Kapitel);

            // Ohne Konfiguration alle Kapitel — der Anhang E mit dem Häkchen „Wirtschaftlichkeit“.
            Assert.Equal(Berichtskapitel.Alle.Select(k => k.Name),
                         Berichtswerte.Aus(GefuellteProbe(), null, false, null).AktiveKapitel);
        }

        /// <summary>
        /// Katalog v2 (BV-E2): ein Kapitel liefert sich selbst, wenn sein Häkchen gesetzt ist, sonst nichts
        /// (Entfall); der Anhang E folgt dem Häkchen „Wirtschaftlichkeit“; der Schalter ist der Wert des
        /// Häkchens; der Kapitelkopf die eigene Überschrift in der Berichtssprache; das Logo die Bytes der
        /// Erstellerangaben, ohne Logo leer mit Grund.
        /// </summary>
        [Fact]
        public void Kapitel_Schalter_Kapitelkoepfe_und_Logo_loesen_sich_auf()
        {
            var konfig = new BerichtsKonfiguration();
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_PROJEKT);
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_WIRTSCHAFT);
            Berichtswerte w = Berichtswerte.Aus(GefuellteProbe(), konfig, false, null);

            Assert.Equal(new[] { Berichtskapitel.PROJEKT }, Wert(w, "{{kapitel.projekt}}").Kapitel);
            Assert.Equal(new[] { Berichtskapitel.ANHANG_E }, Wert(w, "{{kapitel.anhang_e}}").Kapitel);
            Platzhalterwert aus = Wert(w, "{{kapitel.vergleich|ohne titel}}");
            Assert.True(aus.IstLeer);
            Assert.Empty(aus.Kapitel);

            Assert.True(Wert(w, "{{baustein.projekt}}").Schalter);
            Assert.False(Wert(w, "{{baustein.vergleich}}").Schalter);

            Assert.Equal("Berechnungsergebnisse je Variante", Text(w, "{{text.kapitel_ergebnisse}}"));
            Assert.Equal("Results per variant", Text(Berichtswerte.Aus(GefuellteProbe(), konfig, true, null), "{{text.kapitel_ergebnisse}}"));
            Assert.Equal(R.WIRT_AE_TITEL, Text(w, "{{text.kapitel_anhang_e}}"));

            Platzhalterwert ohneLogo = Wert(w, "{{bild.ersteller.logo}}");
            Assert.True(ohneLogo.IstLeer);
            Assert.Null(ohneLogo.Bild);
            Assert.Equal("", ohneLogo.Text);
            Assert.Equal(R.BV_GRUND_KEIN_LOGO, ohneLogo.Grund);

            byte[] png = WordVorlagenfuellerTests.Png(40, 20);
            Platzhalterwert logo = Wert(Berichtswerte.Aus(GefuellteProbe(), konfig, false,
                new Erstellerangaben { Logo = png, LogoDateiname = "firma.png" }), "{{bild.ersteller.logo}}");
            Assert.False(logo.IstLeer);
            Assert.Equal(Bildformat.Png, logo.Bild.Format);
            Assert.Equal(40, logo.Bild.Breite);
            Assert.Equal(20, logo.Bild.Hoehe);
            Assert.Equal("firma.png", logo.Text);
        }

        [Fact]
        public void Der_Produktausweis_steht_sobald_ein_Gebaeude_nach_VDI_6007_rechnete()
        {
            BerichtsDaten daten = GefuellteProbe();
            daten.Varianten[1].Ergebnis.Gebaeude.Add(new ErgebnisGebaeudeModel { Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007 });

            Platzhalterwert de = Wert(Werte(daten, false), "{{bericht.gebaeudemodell.ausweis}}");
            Assert.False(de.IstLeer);
            Assert.Equal(Ressource(nameof(R.GEB_PRODUKTAUSWEIS_VDI6007), false), de.Text);
            Assert.Equal(Ressource(nameof(R.GEB_PRODUKTAUSWEIS_VDI6007), true),
                         Text(Werte(daten, true), "{{bericht.gebaeudemodell.ausweis}}"));

            daten.Varianten[1].Ergebnis.Gebaeude[0].UebergabeArt = "Heizkörper";
            Assert.Equal(Ressource(nameof(R.GEB_PRODUKTAUSWEIS_VDI6007), false) + ". " +
                         Ressource(nameof(R.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG), false),
                         Text(Werte(daten, false), "{{bericht.gebaeudemodell.ausweis}}"));
        }

        // =====================================================================
        //  6 — Robustheit
        // =====================================================================

        [Fact]
        public void Eine_werfende_Quelle_wird_zum_Leerwert_mit_Grund()
        {
            var feld = new Vorlagenfeld("probe.wirft", Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Bericht,
                                        w => throw new InvalidOperationException("kaputt"));
            Berichtswerte werte = Werte(GefuellteProbe(), false);

            Platzhalterwert wert = Vorlagenfeldkatalog.Loese(feld, werte, Ohne);
            Assert.True(wert.IstLeer);
            Assert.Equal("—", wert.Text);
            Assert.Equal("Wert nicht bestimmbar", wert.Grund);
            Assert.Equal("kaputt", wert.Ausnahme);

            Platzhalterwert mitGrund = Vorlagenfeldkatalog.Loese(feld, werte, new[] { Platzhaltersyntax.LiesAngabe("mit grund") });
            Assert.Equal("— (Wert nicht bestimmbar)", mitGrund.Text);
        }

        [Fact]
        public void Loese_einer_Marke_nur_fuer_bekannte_Felder()
        {
            Berichtswerte w = Werte(GefuellteProbe(), false);
            Assert.Equal("Muster AG", Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{ Projekt.Kunde }}"), w).Text);
            Assert.Null(Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{projekt.gibtsnicht}}"), w));
            Assert.Null(Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{#je stand}}"), w));
            Assert.Null(Vorlagenfeldkatalog.Loese((Platzhalter)null, w));
            Assert.Throws<ArgumentNullException>(() => Vorlagenfeldkatalog.Loese(null, w, Ohne));
            Assert.Throws<ArgumentNullException>(() => Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("projekt.kunde"), null, Ohne));
            Assert.Throws<ArgumentNullException>(() => Berichtswerte.Aus(null, null, false, null));

            // Ohne Angabenliste gilt das Katalogformat.
            Assert.Equal("12.000 €/a", Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stamm.kennzahl.ko.energie"), w, null).Text);
        }

        [Fact]
        public void Die_Kultur_kommt_aus_dem_Parameter_nicht_aus_der_Oberflaeche()
        {
            using (new Kulturvorrichtung("en-US"))
            {
                Berichtswerte de = Werte(GefuellteProbe(), false);
                Assert.Equal("25.09.2026", Text(de, "{{bericht.datum}}"));
                Assert.Equal("12.000 €/a", Text(de, "{{stamm.kennzahl.ko.energie}}"));
                Assert.Equal("Seite", Text(de, "{{text.seite}}"));
                Assert.Equal("kein Gebäude nach VDI 6007 gerechnet", Wert(de, "{{bericht.gebaeudemodell.ausweis}}").Grund);
                Assert.Equal("Variantenvergleich — Energie- und Wärmeversorgung", Text(de, "{{bericht.untertitel}}"));
            }
            Berichtswerte en = Werte(GefuellteProbe(), true);
            Assert.Equal("9/25/2026", Text(en, "{{bericht.datum}}"));
            Assert.Equal("Page", Text(en, "{{text.seite}}"));
        }

        [Fact]
        public void Der_Wertesatz_beruehrt_den_Baum_nicht()
        {
            BerichtsDaten daten = GefuellteProbe();
            int kennzahlen = daten.Varianten[0].Kennzahlen.Count;
            Berichtswerte w = Werte(daten, false);
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle) Vorlagenfeldkatalog.Loese(f, w, Ohne);

            Assert.Same(daten, w.Daten);
            Assert.Equal(kennzahlen, daten.Varianten[0].Kennzahlen.Count);
            Assert.Equal(2, daten.Warnungen.Count);
            Assert.Equal("Erste Zeile\r\nZweite Zeile", daten.Varianten[0].Projekt.m_szBeschreibung);
        }
    }
}
