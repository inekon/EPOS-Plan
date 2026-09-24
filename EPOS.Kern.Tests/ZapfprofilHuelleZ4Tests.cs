using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Stufe Z4</b> (Umsetzungskonzept Zapfprofilgenerator 5.1, 5.3, 5.5; Gruppe 1):
    /// Warnliste der Bilanz mit Titeln, Schätzhilfen, Auslastung und Dauerlinie in der Vorschau, die
    /// Laufangaben der Anzeige und die Zeilen des Konstruktors im Arbeitsstand, die Eingaben des
    /// Verfahrensvergleichs, die Stufe als Marke „Schnellauslegung" und der Editor der Zapfkategorien.
    /// Mit Datenbank: die Arbeitskopie der Testdatenbank (Projekt 1007) bzw. eine leere
    /// Tww-Datenbank; ohne Testdatenbank schweigen diese Fälle. Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleZ4Tests : IDisposable
    {
        private const int PROJEKT = 1007;
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Wachen
        // =================================================================================

        /// <summary>
        /// Jede Hinweiskennung des Bilanzrechenwegs — aus dem QUELLTEXT des Zapfprofil-Ordners und der
        /// Controller (Literale in <c>new ZapfHinweis(…, "…"</c> und <c>SummeWarnen</c>) — steht in
        /// <see cref="ZapfprofilHuelle.BILANZHINWEISE"/> und hat ihren Titel in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Jede_Hinweiskennung_der_Bilanz_hat_ihren_Titel_in_beiden_Sprachen()
        {
            var muster = new Regex(@"(?:new ZapfHinweis\([^,]+,\s*|SummeWarnen\([^""\r\n]*)""(?<k>[A-Z][A-Z0-9_]+)""");
            var kennungen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string datei in new[] { Pfad("EPOS.Kern", "Allgemein", "Zapfprofil"), Pfad("EPOS.Kern", "Controller") }
                                     .SelectMany(o => Directory.GetFiles(o, "*.cs")))
                foreach (Match m in muster.Matches(File.ReadAllText(datei)))
                    kennungen.Add(m.Groups["k"].Value);
            Assert.True(kennungen.Count >= 8, "Nur " + kennungen.Count + " Kennungen: " + string.Join(", ", kennungen));

            string[] ungelistet = kennungen.Where(k => !ZapfprofilHuelle.BILANZHINWEISE.Contains(k)).ToArray();
            Assert.True(ungelistet.Length == 0, "Nicht in BILANZHINWEISE: " + string.Join(", ", ungelistet));
            string[] ohneTitel = ZapfprofilHuelle.BILANZHINWEISE.Select(k => "ZPG_WARN_" + k)
                .Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(ohneTitel.Length == 0, "Ohne Titel: " + string.Join(", ", ohneTitel));
            Assert.Equal(ZapfprofilHuelle.BILANZHINWEISE.Length, ZapfprofilHuelle.BILANZHINWEISE.Distinct().Count());

            string[] ohneGrund = Enum.GetValues(typeof(TwwKatalogAusgang)).Cast<TwwKatalogAusgang>()
                .Where(a => a != TwwKatalogAusgang.Ausgefuehrt).Select(ZapfprofilHuelle.KategorienSchluessel)
                .Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(ohneGrund.Length == 0, "Ohne Grund: " + string.Join(", ", ohneGrund));
        }

        [Fact]
        public void Die_Warnliste_nennt_Titel_Satz_und_Stufe()
        {
            var h = new ZapfHinweis("", ZapfprofilRechner.HINWEIS_ZIRKULATION_GROSS,
                ZapfSatz.Neu("HINWEIS_ZIRKULATION_GROSS", 13140.0, 1.8, 7300.0, 1.5));
            ZapfprofilWarnDaten w = ZapfprofilHuelle.Warnung(h);
            Assert.Equal("ZPG_WARN_ZIRKULATION_GROSS", w.Kennung);
            Assert.Equal("Zirkulation groß gegenüber der Zapfung", w.Titel);
            Assert.StartsWith("Die Zirkulation verliert im Jahr 13140 kWh, das 1,8-Fache der Zapfung (7300 kWh/a)", w.Text);
            Assert.Equal(ZapfprofilWarnstufe.Hinweis, w.Stufe);
            var warnung = new ZapfHinweis("", ZapfprofilRechner.HINWEIS_NETZVERLUST,
                ZapfSatz.Neu("HINWEIS_NETZVERLUST_UND_ZIRKULATION", 1000.0)) { Warnung = true };
            Assert.Equal(ZapfprofilWarnstufe.Warnung, ZapfprofilHuelle.Warnung(warnung).Stufe);
            Assert.Equal(ZapfprofilWarnstufe.Hinweis,
                         ZapfprofilHuelle.Warnung(new ZapfHinweis("", "UNBEKANNT", ZapfSatz.Neu("UNBEKANNT"))).Stufe);
            Assert.Equal("Hinweis", ZapfprofilHuelle.Warnung(new ZapfHinweis("", "UNBEKANNT", ZapfSatz.Neu("UNBEKANNT"))).Titel);
        }

        // =================================================================================
        // Arbeitsstand <-> DTO
        // =================================================================================

        [Fact]
        public void Anzeige_Konstruktorzeilen_und_Vergleich_gehen_mit_dem_Arbeitsstand()
        {
            var zeile = new ZapfprofilKonstruktorZeileDaten { BeginnH = 7.0, EndeH = 8.0, VolumenL = 100.0, ZapftemperaturC = 45.0, Verbraucher = "Dusche" };
            var entwurf = new ZapfprofilBedarfstagDaten { Bezeichner = "Eigener Tag", Katalogversion = "T9" };
            entwurf.Konstruktorzeilen.Add(zeile);
            var eingabe = new ZapfprofilEingabeDaten
            {
                AnzeigetemperaturC = 45.0,
                SchwelleKw = 5.0,
                Auslegung = new ZapfprofilAuslegungEingabeDaten
                {
                    Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
                    Entwurf = entwurf,
                    LadeAuto = false, LadeManuellKw = 12.0, LadefensterH = 8.0, LadefensterBeginnH = 22.0,
                    Nutzanteil = 0.7, Zuschlag = 0.2, PersonenAuto = false, PersonenManuell = 30.0,
                    FuellstandBezug = ZapfprofilFuellstandbezug.BandMax
                }
            };
            ZapfprofilStand stand = ZapfprofilHuelle.AlsStand(eingabe, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], new ProjektStand()));
            Assert.Equal(new ZapfAnzeige(45.0, 5.0), stand.Anzeige);
            KonstruktorzeileStand k = Assert.Single(stand.Konstruktorzeilen);
            Assert.Equal(100.0, k.VolumenL);
            Assert.Equal("Dusche", k.Verbraucher);
            Assert.False(stand.Projekt.LadeAuto);
            Assert.Equal(12.0, stand.Projekt.LadeManuellKw);
            Assert.Equal(0.7, stand.Projekt.Nutzanteil);
            Assert.False(stand.Projekt.PersonenAuto);
            Assert.Equal(30.0, stand.Projekt.PersonenManuell);
            Assert.Equal(ZapfFuellstandbezug.BandMax, stand.Projekt.FuellstandBezug);

            // Zurück: die Laufangaben in die Eingabe, Zeilen und Vergleich in die Überlagerung.
            ZapfprofilEingabeDaten zurueck = ZapfprofilHuelle.AlsEingabe(stand);
            Assert.Equal(45.0, zurueck.AnzeigetemperaturC);
            Assert.Equal(5.0, zurueck.SchwelleKw);
            ZapfprofilAuslegungEingabeDaten a = ZapfprofilHuelle.AuslegungAusStand(stand);
            Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, a.Quelle);
            Assert.Equal(100.0, Assert.Single(a.Entwurf.Konstruktorzeilen).VolumenL);
            Assert.False(a.LadeAuto);
            Assert.Equal(22.0, a.LadefensterBeginnH);
            Assert.Equal(ZapfprofilFuellstandbezug.BandMax, a.FuellstandBezug);
            Assert.Equal(30.0, a.PersonenManuell);

            // Ohne Laufangabe keine Anzeige im Stand — es gilt die Einstellung.
            Assert.Null(ZapfprofilHuelle.AlsStand(new ZapfprofilEingabeDaten(), null).Anzeige);
            Assert.Equal(eingabe.AnzeigetemperaturC, eingabe.Kopie().AnzeigetemperaturC);
            Assert.Equal(ZapfprofilFuellstandbezug.BandMax, eingabe.Auslegung.Kopie().FuellstandBezug);
        }

        // =================================================================================
        // Vorschau auf der Testdatenbank
        // =================================================================================

        [Fact]
        public void Die_Vorschau_traegt_Warnliste_Schaetzhilfen_Auslastung_und_Dauerlinie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int art = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", "Testnutzung A (fiktiv)"), new DbParam("@k", "TEST-1")));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Netzverluste = 5 WHERE ID_Projekt = ?", new DbParam("?", PROJEKT));
            var eingabe = new ZapfprofilEingabeDaten
            {
                SchwelleKw = 1.0,
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = art, Bezugsmenge = 10 } }
            };
            ZapfprofilVorschauDaten v = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, v.Zustand);

            ZapfprofilWarnDaten zu5 = Assert.Single(v.Warnliste, w => w.Kennung == "ZPG_WARN_NETZVERLUST_UND_ZIRKULATION");
            Assert.Equal(ZapfprofilWarnstufe.Warnung, zu5.Stufe);
            Assert.Equal("Netzverluste und Zirkulation", zu5.Titel);
            Assert.Equal(v.Meldungen.Count(m => m.Art == ZapfprofilMeldungsart.Hinweis), v.Warnliste.Count);

            ZapfprofilAnsichtDaten summe = v.Summe;
            Assert.NotNull(summe.Dauerlinie);
            Assert.Equal(8760, summe.Dauerlinie.GesamtKw.Length);
            Assert.Equal(4, summe.Dauerlinie.Marken.Count);
            Assert.Equal(1.0, summe.Dauerlinie.SchwelleKw);
            Assert.NotNull(summe.Dauerlinie.StundenUeberSchwelle);
            Assert.NotNull(summe.Dauerlinie.Modell);
            Assert.Null(summe.Tagesbedarf);
            Assert.NotNull(v.Zirkulation);
            Assert.Equal("kW", v.Zirkulation.Einheit);
            Assert.NotEqual("", v.Zirkulation.Rechenweg);

            ZapfprofilAnsichtDaten zone = v.Ansichten[1];
            Assert.NotNull(zone.Tagesbedarf);
            Assert.Equal("kWh/d", zone.Tagesbedarf.Einheit);
            Assert.True(zone.Tagesbedarf.Vorschlag > 0);
            Assert.Contains("kWh/d", zone.Tagesbedarf.Rechenweg);
            Assert.Equal(12, zone.Auslastung.Monate.Length);
            Assert.Equal(1.0, zone.Auslastung.Stunden.Average(), 9);
            Assert.Equal(12, zone.Auslastungsgang.Wirksam.Length);
            Assert.NotNull(zone.Dauerlinie);
        }

        // =================================================================================
        // Kategorien (Experte)
        // =================================================================================

        [Fact]
        public void Der_Editor_der_Kategorien_liest_und_speichert_benannt()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", "T1", satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(frei, "Erste", 1, 4.0, 1, 1.0, 1.0);
            TwwTestdatenbank.KategorieAnlegen(geliefert, "Kurz", 1, 2.0, 1, 1.0, 1.0, readOnly: true, herkunftsart: TwwSchema.HERKUNFT_FREI);

            ZapfprofilKategorienDaten d = ZapfprofilHuelle.Kategorien(frei);
            Assert.True(d.Frei);
            Assert.Equal("", d.Sperrgrund);
            Assert.Equal("Erste", Assert.Single(d.Kategorien).Name);
            Assert.Equal("", d.Hinweis);
            Assert.Equal("Kurz", Assert.Single(d.Vorgabe).Name);

            ZapfprofilKategorienDaten g = ZapfprofilHuelle.Kategorien(geliefert);
            Assert.False(g.Frei);
            Assert.StartsWith("Die Nutzungsart gehört zur Auslieferung", g.Sperrgrund);

            // Ein leeres Feld ist eine benannte Ablehnung, kein stiller Ersatzwert.
            var leer = new List<ZapfprofilKategorieDaten> { new ZapfprofilKategorieDaten { Name = "A", DauerMin = 1, Anteil = 1.0, StreuungLJeMin = 0.0 } };
            ZapfprofilKategorienErgebnis nein = ZapfprofilHuelle.KategorienSpeichern(frei, leer, null);
            Assert.False(nein.Ok);
            Assert.Equal("ZPG_SATZ_KATEGORIE_VOLUMENSTROM", nein.Meldung.Kennung);
            Assert.StartsWith("Die Zapfkategorien wurden nicht gespeichert — Die Zapfkategorie „A“", nein.Meldung.Text);

            ZapfprofilKategorienErgebnis ohneVersion = ZapfprofilHuelle.KategorienSpeichern(geliefert, g.Kategorien, null);
            Assert.True(ohneVersion.Ok);   // unverändert: nichts zu schreiben
            g.Kategorien[0].StreuungLJeMin = 2.0;
            ohneVersion = ZapfprofilHuelle.KategorienSpeichern(geliefert, g.Kategorien, null);
            Assert.Equal("ZPG_KATEG_GRUND_ENTWURF_UNVOLLSTAENDIG", ohneVersion.Meldung.Kennung);

            ZapfprofilKategorienErgebnis kopie = ZapfprofilHuelle.KategorienSpeichern(geliefert, g.Kategorien, "T2");
            Assert.True(kopie.Ok);
            Assert.True(kopie.NeueZeile);
            Assert.Equal(2.0, Assert.Single(ZapfprofilHuelle.Kategorien(kopie.IdNutzungsart).Kategorien).StreuungLJeMin);
        }

        // =================================================================================

        private static string Text(string schluessel, CultureInfo kultur)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, kultur);

        private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
