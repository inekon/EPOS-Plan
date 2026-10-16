using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Abbildung Bilanz → Bilddaten</b> (<see cref="EnergieflussAbbildung"/>, Konzept Energieflussbild 3.1 und 3.4,
    /// Schritt SK-12): jede Kante der Bilanz wird genau ein Band, jeder Knoten genau ein Bildknoten, nichts geht verloren;
    /// Farbrolle nach der Regel „in den Bus die Quelle, aus dem Bus die Verwendung“, Brennstoff, Umwelt, Kälte, Verlust,
    /// Rest; Bandart Verlust bzw. gestrichelt; Verlustknoten mit der Spalte ihres Eigentümers; Zeigertexte, Kurznamen und
    /// Jahressummen übernommen; Spaltentitel in der Sprache der Bilanz; und der Renderer zeichnet die abgebildeten
    /// Gaben vollständig (Bänder schließen an, Verlustknoten stehen in der Lücke).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EnergieflussAbbildungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const float KB = ChartRenderer.EF_KNOTENBREITE;

        // =====================================================================
        //  Eine Bilanz von Hand, die jede Regel der Farbvergabe berührt
        // =====================================================================

        private static readonly (string Id, EnergieflussKnotenart Art, int Spalte)[] KNOTEN =
        {
            (Energiefluss.NETZBEZUG, EnergieflussKnotenart.Quelle, 0), (Energiefluss.PHOTOVOLTAIK, EnergieflussKnotenart.Quelle, 0),
            ("traeger:7", EnergieflussKnotenart.Quelle, 0), (Energiefluss.UMWELTWAERME, EnergieflussKnotenart.Quelle, 0),
            (Energiefluss.UMWELTKAELTE, EnergieflussKnotenart.Quelle, 0), (Energiefluss.KONDENSATION, EnergieflussKnotenart.Quelle, 0),
            (Energiefluss.SOLARTHERMIE, EnergieflussKnotenart.Quelle, 0), (Energiefluss.KUEHLSTROM_EIGEN, EnergieflussKnotenart.Quelle, 0),
            (Energiefluss.UNGEDECKT, EnergieflussKnotenart.Quelle, 0), (Energiefluss.NICHT_ZUGEORDNET_QUELLE, EnergieflussKnotenart.Quelle, 0),
            (Energiefluss.BHKW, EnergieflussKnotenart.Wandler, 1), (Energiefluss.HEIZKESSEL, EnergieflussKnotenart.Wandler, 1),
            (Energiefluss.VERLUST + Energiefluss.BHKW, EnergieflussKnotenart.Verlust, 1),
            (Energiefluss.STROM, EnergieflussKnotenart.Bus, 2),
            (Energiefluss.WP_HEIZEN, EnergieflussKnotenart.Wandler, 3), (Energiefluss.HEIZSTAB, EnergieflussKnotenart.Wandler, 3),
            (Energiefluss.ELEKTROKESSEL, EnergieflussKnotenart.Wandler, 3), (Energiefluss.WP_KUEHLEN, EnergieflussKnotenart.Wandler, 3),
            ("kaeltemaschine:1", EnergieflussKnotenart.Wandler, 3), (Energiefluss.FREIE_KUEHLUNG, EnergieflussKnotenart.Wandler, 3),
            ("split:1", EnergieflussKnotenart.Wandler, 3), ("weitere:split", EnergieflussKnotenart.Wandler, 3),
            (Energiefluss.VERLUST + Energiefluss.ELEKTROKESSEL, EnergieflussKnotenart.Verlust, 3),
            (Energiefluss.WAERME, EnergieflussKnotenart.Bus, 4), (Energiefluss.KAELTE, EnergieflussKnotenart.Bus, 4),
            ("puffer:3:0", EnergieflussKnotenart.Speicher, 5), ("weitere:puffer", EnergieflussKnotenart.Speicher, 5),
            ("kaeltespeicher:4:0", EnergieflussKnotenart.Speicher, 5), ("stromspeicher:1", EnergieflussKnotenart.Speicher, 5),
            ("verlust:puffer:3:0", EnergieflussKnotenart.Verlust, 5), ("fuellstand:puffer:3:0", EnergieflussKnotenart.Verlust, 5),
            ("verlust:stromspeicher:1", EnergieflussKnotenart.Verlust, 5),
            (Energiefluss.HEIZUNG, EnergieflussKnotenart.Senke, 6), (Energiefluss.WARMWASSER, EnergieflussKnotenart.Senke, 6),
            (Energiefluss.PROZESSWAERME, EnergieflussKnotenart.Senke, 6), (Energiefluss.KUEHLUNG, EnergieflussKnotenart.Senke, 6),
            (Energiefluss.STROMBEDARF, EnergieflussKnotenart.Senke, 6), (Energiefluss.EINSPEISUNG_PV, EnergieflussKnotenart.Senke, 6),
            (Energiefluss.EINSPEISUNG_BHKW, EnergieflussKnotenart.Senke, 6), (Energiefluss.NICHT_ZUGEORDNET, EnergieflussKnotenart.Senke, 6),
        };

        /// <summary>Jede Kante mit ihrer erwarteten Rolle (Konzept 3.4) und Bandart.</summary>
        private static readonly (string Von, string Nach, EnergieflussForm Form, Farbrolle Rolle, EnergieflussBandart Art)[] KANTEN =
        {
            (Energiefluss.NETZBEZUG, Energiefluss.STROM, EnergieflussForm.Strom, Farbrolle.STROM_NETZ, EnergieflussBandart.Normal),
            (Energiefluss.PHOTOVOLTAIK, Energiefluss.STROM, EnergieflussForm.Strom, Farbrolle.STROM_PV, EnergieflussBandart.Normal),
            (Energiefluss.BHKW, Energiefluss.STROM, EnergieflussForm.Strom, Farbrolle.STROM_BHKW, EnergieflussBandart.Normal),
            (Energiefluss.BHKW, Energiefluss.WAERME, EnergieflussForm.Waerme, Farbrolle.WAERME_BHKW, EnergieflussBandart.Normal),
            ("traeger:7", Energiefluss.BHKW, EnergieflussForm.Brennstoff, Farbrolle.BRENNSTOFF, EnergieflussBandart.Normal),
            ("traeger:7", Energiefluss.HEIZKESSEL, EnergieflussForm.Brennstoff, Farbrolle.BRENNSTOFF, EnergieflussBandart.Normal),
            (Energiefluss.BHKW, Energiefluss.VERLUST + Energiefluss.BHKW, EnergieflussForm.Verlust, Farbrolle.VERLUST, EnergieflussBandart.Verlust),
            (Energiefluss.HEIZKESSEL, Energiefluss.WAERME, EnergieflussForm.Waerme, Farbrolle.WAERME_KESSEL, EnergieflussBandart.Normal),
            (Energiefluss.KONDENSATION, Energiefluss.HEIZKESSEL, EnergieflussForm.Umwelt, Farbrolle.UMWELTWAERME, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.WP_HEIZEN, EnergieflussForm.Strom, Farbrolle.WAERME_WP, EnergieflussBandart.Normal),
            (Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN, EnergieflussForm.Umwelt, Farbrolle.UMWELTWAERME, EnergieflussBandart.Normal),
            (Energiefluss.WP_HEIZEN, Energiefluss.WAERME, EnergieflussForm.Waerme, Farbrolle.WAERME_WP, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.HEIZSTAB, EnergieflussForm.Strom, Farbrolle.HEIZSTAB, EnergieflussBandart.Normal),
            (Energiefluss.HEIZSTAB, Energiefluss.WAERME, EnergieflussForm.Waerme, Farbrolle.HEIZSTAB, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.ELEKTROKESSEL, EnergieflussForm.Strom, Farbrolle.WAERME_KESSEL, EnergieflussBandart.Normal),
            (Energiefluss.ELEKTROKESSEL, Energiefluss.WAERME, EnergieflussForm.Waerme, Farbrolle.WAERME_KESSEL, EnergieflussBandart.Normal),
            (Energiefluss.ELEKTROKESSEL, Energiefluss.VERLUST + Energiefluss.ELEKTROKESSEL, EnergieflussForm.Verlust, Farbrolle.VERLUST, EnergieflussBandart.Verlust),
            (Energiefluss.SOLARTHERMIE, Energiefluss.WAERME, EnergieflussForm.Waerme, Farbrolle.WAERME_SOLAR, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.WP_KUEHLEN, EnergieflussForm.Strom, Farbrolle.KAELTE, EnergieflussBandart.Normal),
            (Energiefluss.UMWELTKAELTE, Energiefluss.WP_KUEHLEN, EnergieflussForm.Umwelt, Farbrolle.UMWELTWAERME, EnergieflussBandart.Normal),
            (Energiefluss.WP_KUEHLEN, Energiefluss.KAELTE, EnergieflussForm.Kaelte, Farbrolle.KAELTE, EnergieflussBandart.Normal),
            (Energiefluss.KUEHLSTROM_EIGEN, "kaeltemaschine:1", EnergieflussForm.Strom, Farbrolle.STROM_NETZ, EnergieflussBandart.Normal),
            ("kaeltemaschine:1", Energiefluss.KAELTE, EnergieflussForm.Kaelte, Farbrolle.KAELTE, EnergieflussBandart.Normal),
            (Energiefluss.FREIE_KUEHLUNG, Energiefluss.KAELTE, EnergieflussForm.Kaelte, Farbrolle.KAELTE, EnergieflussBandart.Normal),
            (Energiefluss.STROM, "split:1", EnergieflussForm.Strom, Farbrolle.KAELTE_DEZENTRAL, EnergieflussBandart.Normal),
            ("split:1", Energiefluss.KAELTE, EnergieflussForm.Kaelte, Farbrolle.KAELTE_DEZENTRAL, EnergieflussBandart.Normal),
            (Energiefluss.STROM, "weitere:split", EnergieflussForm.Strom, Farbrolle.KAELTE_DEZENTRAL, EnergieflussBandart.Normal),
            ("weitere:split", Energiefluss.KAELTE, EnergieflussForm.Kaelte, Farbrolle.KAELTE_DEZENTRAL, EnergieflussBandart.Normal),
            (Energiefluss.WAERME, "puffer:3:0", EnergieflussForm.Waerme, Farbrolle.SPEICHERLADUNG, EnergieflussBandart.Normal),
            ("puffer:3:0", Energiefluss.HEIZUNG, EnergieflussForm.Waerme, Farbrolle.HEIZWAERME, EnergieflussBandart.Normal),
            ("puffer:3:0", Energiefluss.WARMWASSER, EnergieflussForm.Waerme, Farbrolle.WARMWASSER, EnergieflussBandart.Normal),
            ("puffer:3:0", "verlust:puffer:3:0", EnergieflussForm.Verlust, Farbrolle.VERLUST, EnergieflussBandart.Verlust),
            ("puffer:3:0", "fuellstand:puffer:3:0", EnergieflussForm.Waerme, Farbrolle.SPEICHERLADUNG, EnergieflussBandart.Normal),
            (Energiefluss.WAERME, "weitere:puffer", EnergieflussForm.Waerme, Farbrolle.SPEICHERLADUNG, EnergieflussBandart.Normal),
            ("weitere:puffer", Energiefluss.PROZESSWAERME, EnergieflussForm.Waerme, Farbrolle.PROZESSWAERME, EnergieflussBandart.Normal),
            (Energiefluss.WAERME, Energiefluss.HEIZUNG, EnergieflussForm.Waerme, Farbrolle.HEIZWAERME, EnergieflussBandart.Normal),
            (Energiefluss.WAERME, Energiefluss.WARMWASSER, EnergieflussForm.Waerme, Farbrolle.WARMWASSER, EnergieflussBandart.Normal),
            (Energiefluss.WAERME, Energiefluss.PROZESSWAERME, EnergieflussForm.Waerme, Farbrolle.PROZESSWAERME, EnergieflussBandart.Normal),
            (Energiefluss.KAELTE, "kaeltespeicher:4:0", EnergieflussForm.Kaelte, Farbrolle.SPEICHERLADUNG, EnergieflussBandart.Normal),
            ("kaeltespeicher:4:0", Energiefluss.KUEHLUNG, EnergieflussForm.Kaelte, Farbrolle.KAELTE, EnergieflussBandart.Normal),
            (Energiefluss.KAELTE, Energiefluss.KUEHLUNG, EnergieflussForm.Kaelte, Farbrolle.KAELTE, EnergieflussBandart.Normal),
            (Energiefluss.STROM, "stromspeicher:1", EnergieflussForm.Strom, Farbrolle.SPEICHERLADUNG, EnergieflussBandart.Normal),
            ("stromspeicher:1", Energiefluss.STROMBEDARF, EnergieflussForm.Strom, Farbrolle.STROM_SPEICHER, EnergieflussBandart.Normal),
            ("stromspeicher:1", "verlust:stromspeicher:1", EnergieflussForm.Verlust, Farbrolle.VERLUST, EnergieflussBandart.Verlust),
            (Energiefluss.STROM, Energiefluss.STROMBEDARF, EnergieflussForm.Strom, Farbrolle.VERBRAUCH_GESAMT, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.EINSPEISUNG_PV, EnergieflussForm.Strom, Farbrolle.UEBERSCHUSS, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW, EnergieflussForm.Strom, Farbrolle.UEBERSCHUSS, EnergieflussBandart.Normal),
            (Energiefluss.STROM, Energiefluss.NICHT_ZUGEORDNET, EnergieflussForm.Rest, Farbrolle.REST, EnergieflussBandart.NichtZugeordnet),
            (Energiefluss.NICHT_ZUGEORDNET_QUELLE, Energiefluss.WAERME, EnergieflussForm.Rest, Farbrolle.REST, EnergieflussBandart.NichtZugeordnet),
            (Energiefluss.UNGEDECKT, Energiefluss.HEIZUNG, EnergieflussForm.Rest, Farbrolle.REST, EnergieflussBandart.NichtZugeordnet),
        };

        private static EnergieflussBilanz VonHand(bool englisch = false)
        {
            var b = new EnergieflussBilanz { IdProjekt = 4711, Version = "Stamm", Englisch = englisch, Simuliert = true };
            int i = 0;
            foreach (var k in KNOTEN)
                b.Knoten.Add(new EnergieflussKnoten
                {
                    Id = k.Id, Name = "N " + k.Id, Kurzname = "K " + k.Id, Art = k.Art, Spalte = k.Spalte, Rang = i++,
                    Zeigertext = "Z " + k.Id, Geprueft = k.Art == EnergieflussKnotenart.Wandler || k.Art == EnergieflussKnotenart.Bus || k.Art == EnergieflussKnotenart.Speicher,
                });
            double mwh = 1.0;
            foreach (var e in KANTEN)
            {
                b.Fluesse.Add(new EnergieflussFluss { Von = e.Von, Nach = e.Nach, Mwh = mwh, Form = e.Form, Zeigertext = "Z " + e.Von + "→" + e.Nach });
                b.KnotenMit(e.Von).AbflussMwh += mwh;
                b.KnotenMit(e.Nach).ZuflussMwh += mwh;
                mwh += 0.5;
            }
            b.Hinweise.Add("ein Hinweis");
            return b;
        }

        private static EnergieflussBildBand Band(EnergieflussBildDaten d, string von, string nach)
            => Assert.Single(d.Baender, x => x.Von == von && x.Nach == nach);

        // =====================================================================
        //  Vollständigkeit
        // =====================================================================

        [Fact]
        public void Jede_Kante_wird_genau_ein_Band_und_jeder_Knoten_genau_ein_Bildknoten()
        {
            EnergieflussBilanz b = VonHand();
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            Assert.Equal(b.Knoten.Count, d.Knoten.Count);
            Assert.Equal(b.Fluesse.Count, d.Baender.Count);
            Assert.Equal(b.Knoten.Select(k => k.Id), d.Knoten.Select(k => k.Kennung));
            Assert.Equal(b.Fluesse.Select(f => (f.Von, f.Nach, f.Mwh)), d.Baender.Select(x => (x.Von, x.Nach, x.Mwh)));
            Assert.Equal(b.Fluesse.Sum(f => f.Mwh), d.Baender.Sum(x => x.Mwh), 9);
        }

        [Fact]
        public void Knoten_tragen_Spalte_Art_Namen_Kurznamen_Zeigertext_und_Jahressumme()
        {
            EnergieflussBilanz b = VonHand();
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            foreach (EnergieflussKnoten k in b.Knoten)
            {
                EnergieflussBildKnoten bk = Assert.Single(d.Knoten, x => x.Kennung == k.Id);
                Assert.Equal(k.Spalte, bk.Spalte);
                Assert.Equal(k.Art, bk.Art);
                Assert.Equal(k.Name, bk.Name);
                Assert.Equal(k.Kurzname, bk.Kurzname);
                Assert.Equal(k.Zeigertext, bk.Zeigertext);
                Assert.Equal(k.Art == EnergieflussKnotenart.Quelle ? k.AbflussMwh : k.ZuflussMwh, bk.Mwh, 9);
                Assert.Equal(k.Art == EnergieflussKnotenart.Verlust ? Farbrolle.VERLUST : Farbrolle.KNOTEN, bk.Rolle);
            }
            // Ein Verlust- oder Füllstandsknoten trägt die Spalte seines Eigentümers (Konzept 3.1) — ganzzahlig.
            Assert.Equal(1, Assert.Single(d.Knoten, x => x.Kennung == Energiefluss.VERLUST + Energiefluss.BHKW).Spalte);
            Assert.Equal(5, Assert.Single(d.Knoten, x => x.Kennung == "fuellstand:puffer:3:0").Spalte);
        }

        [Fact]
        public void Baender_tragen_Zeigertext_der_Bilanz_woertlich()
        {
            EnergieflussBilanz b = VonHand();
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            foreach (EnergieflussFluss f in b.Fluesse)
                Assert.Equal(f.Zeigertext, Band(d, f.Von, f.Nach).Zeigertext);
            // Ein leerer Zeigertext wird null — der Renderer bildet dann seinen Vorgabetext.
            b.Fluesse[0].Zeigertext = "";
            b.Knoten[0].Zeigertext = "";
            d = EnergieflussAbbildung.Bild(b);
            Assert.Null(d.Baender[0].Zeigertext);
            Assert.Null(d.Knoten[0].Zeigertext);
        }

        // =====================================================================
        //  Rollen und Bandarten (Konzept 3.4)
        // =====================================================================

        [Fact]
        public void Rollen_folgen_der_Regel_in_den_Bus_die_Quelle_aus_dem_Bus_die_Verwendung()
        {
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(VonHand());
            foreach (var e in KANTEN)
            {
                EnergieflussBildBand band = Band(d, e.Von, e.Nach);
                Assert.True(e.Rolle == band.Rolle, e.Von + " → " + e.Nach + ": erwartet " + e.Rolle + ", ist " + band.Rolle);
                Assert.True(e.Art == band.Art, e.Von + " → " + e.Nach + ": erwartet " + e.Art + ", ist " + band.Art);
            }
        }

        [Fact]
        public void Unbekannte_Knoten_fallen_auf_die_Energieform_zurueck()
        {
            var b = new EnergieflussBilanz { Simuliert = true };
            b.Knoten.Add(new EnergieflussKnoten { Id = "fremd_a", Art = EnergieflussKnotenart.Wandler, Spalte = 1 });
            b.Knoten.Add(new EnergieflussKnoten { Id = "fremd_b", Art = EnergieflussKnotenart.Wandler, Spalte = 3 });
            b.Fluesse.Add(new EnergieflussFluss { Von = "fremd_a", Nach = "fremd_b", Mwh = 1.0, Form = EnergieflussForm.Strom });
            b.Fluesse.Add(new EnergieflussFluss { Von = "fremd_b", Nach = "fremd_a", Mwh = 1.0, Form = EnergieflussForm.Kaelte });
            b.Fluesse.Add(new EnergieflussFluss { Von = "fremd_b", Nach = "unbekannt", Mwh = 1.0, Form = EnergieflussForm.Waerme });
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            Assert.Equal(Farbrolle.STROM_NETZ, Band(d, "fremd_a", "fremd_b").Rolle);
            Assert.Equal(Farbrolle.KAELTE, Band(d, "fremd_b", "fremd_a").Rolle);
            Assert.Equal(Farbrolle.HEIZWAERME, Band(d, "fremd_b", "unbekannt").Rolle);
            Assert.All(d.Baender, x => Assert.Equal(EnergieflussBandart.Normal, x.Art));
        }

        // =====================================================================
        //  Spaltentitel und Sprache
        // =====================================================================

        [Fact]
        public void Spaltentitel_in_der_Sprache_der_Bilanz_und_nur_die_Busse_die_im_Bild_stehen()
        {
            // Die Bustitel sind die Namen der Busknoten (hier „N strom“ …), die übrigen Titel Ressourcen.
            Assert.Equal(new[] { "Quellen", "Wandler", "N strom", "Wandler", "N waerme · N kaelte", "Speicher", "Bedarf" },
                         EnergieflussAbbildung.Bild(VonHand()).Spaltentitel);
            Assert.Equal(new[] { "Sources", "Converters", "N strom", "Converters", "N waerme · N kaelte", "Storage", "Demand" },
                         EnergieflussAbbildung.Bild(VonHand(englisch: true)).Spaltentitel);
            EnergieflussBilanz echt = Energiefluss.Aus(Baum(), null, false, "Stamm", 4711);
            Assert.Equal(new[] { "Quellen", "Wandler", "Strom", "Wandler", "Wärme", "Speicher", "Bedarf" }, EnergieflussAbbildung.Bild(echt).Spaltentitel);
            EnergieflussBilanz en = Energiefluss.Aus(Baum(), null, true, "Stamm", 4711);
            Assert.Equal(new[] { "Sources", "Converters", en.KnotenMit(Energiefluss.STROM).Name, "Converters", en.KnotenMit(Energiefluss.WAERME).Name, "Storage", "Demand" },
                         EnergieflussAbbildung.Bild(en).Spaltentitel);

            // Ohne Kältebus heißt die Spalte „Wärme“; ohne beide der Vorgabetitel.
            EnergieflussBilanz nurWaerme = VonHand();
            nurWaerme.Knoten.RemoveAll(k => k.Id == Energiefluss.KAELTE);
            Assert.Equal("N " + Energiefluss.WAERME, EnergieflussAbbildung.Bild(nurWaerme).Spaltentitel[Energiefluss.SPALTE_BUSSE]);
            Assert.Equal("Wärme · Kälte", EnergieflussAbbildung.Bild(new EnergieflussBilanz()).Spaltentitel[Energiefluss.SPALTE_BUSSE]);
            Assert.Equal(EnergieflussAbbildung.SPALTEN, EnergieflussAbbildung.Bild(new EnergieflussBilanz()).Spaltentitel.Count);
        }

        [Fact]
        public void Ohne_Bilanz_leere_Bilddaten_ohne_Ausnahme()
        {
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(null);
            Assert.Empty(d.Knoten);
            Assert.Empty(d.Baender);
            Assert.Empty(ChartRenderer.EnergieflussModell(d).Befehle);
            Assert.Empty(ChartRenderer.EnergieflussModell(EnergieflussAbbildung.Bild(new EnergieflussBilanz())).Befehle);
        }

        // =====================================================================
        //  Aus dem Flussbau bis ins Zeichenmodell: nichts geht verloren
        // =====================================================================

        private static ErgebnisModel Baum()
        {
            var e = new ErgebnisModel { Energiebedarf = new ErgebnisEnergiebedarfModel() };
            e.Energiebedarf.Strombedarf_Gesamt = 20.0;
            e.Energiebedarf.Stromrestbedarf = 40.0;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.HEIZUNG] = 50.0;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.BRAUCHWASSER] = 10.0;
            e.Energiebedarf.Waermebedarf_Gesamt = 60.0;
            e.Waermepumpe = new ErgebnisWaermepumpeModel { Waermeproduktion_WP = 46.0, Stromverbrauch_WP = 15.0, Stromverbrauch_Heizstab = 5.0 };
            e.Heizkessel = new ErgebnisHeizkesselModel();
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Gas", CarrierId = 7, Verbrauch = 12.0, Waermeproduktion = 11.0 });
            var p = new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 3, Bezeichner = "Puffer 1", Verwendung = DbWerte.PSP_VERWENDUNG_HEIZUNG,
                Ladung_gesamt = 30000.0, Entladung_gesamt = 28000.0, Verluste_gesamt = 1400.0, SOC_Ende = 600.0,
            };
            p.Entladung_Kanal[Kanal.HEIZUNG] = 25000.0;
            p.Entladung_Kanal[Kanal.BRAUCHWASSER] = 3000.0;
            e.Pufferspeicher.Add(p);
            return e;
        }

        [Fact]
        public void Bilanz_aus_dem_Flussbau_wird_vollstaendig_gezeichnet_Verlustknoten_in_der_Luecke()
        {
            EnergieflussBilanz b = Energiefluss.Aus(Baum(), null, false, "Stamm", 4711);
            Assert.False(b.Leer);
            Assert.Contains(b.Knoten, k => k.Art == EnergieflussKnotenart.Verlust);
            Assert.Contains(b.Knoten, k => k.Id.StartsWith(Energiefluss.FUELLSTAND, StringComparison.Ordinal));
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);

            foreach (var (mass, werte) in new[] { (ChartRenderer.ENERGIEFLUSS_BREIT, false), (ChartRenderer.ENERGIEFLUSS_SCHMAL, false), (ChartRenderer.ENERGIEFLUSS_BERICHT, true) })
            {
                Zeichenmodell z = ChartRenderer.EnergieflussModell(d, mass, werte);
                var rechtecke = z.Befehle.OfType<Rechteck>().Where(r => r.Marke != null && r.Marke.StartsWith("knoten:", StringComparison.Ordinal))
                                 .ToDictionary(r => r.Marke.Substring("knoten:".Length), r => r);
                var baender = z.Befehle.OfType<Pfad>().Where(p => p.Marke != null && p.Marke.StartsWith("fluss:", StringComparison.Ordinal)).ToList();
                Assert.Equal(b.Knoten.Count, rechtecke.Count);
                Assert.Equal(b.Fluesse.Count, baender.Count);
                int n = ChartRenderer.EF_STUETZSTELLEN;
                foreach (EnergieflussFluss f in b.Fluesse)
                {
                    Pfad p = Assert.Single(baender, x => x.Marke == f.Marke);
                    Rechteck von = rechtecke[f.Von], nach = rechtecke[f.Nach];
                    Assert.Equal(von.X + KB, p.Punkte[0].X, 3);
                    Assert.Equal(nach.X, p.Punkte[n - 1].X, 3);
                    Assert.InRange(p.Punkte[0].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
                    Assert.InRange(p.Punkte[n - 1].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
                    Assert.Equal(f.Zeigertext, p.Wert);
                }
                foreach (EnergieflussKnoten k in b.Knoten)
                    Assert.Equal(k.Zeigertext, rechtecke[k.Id].Wert);
                // Verlust- und Füllstandsknoten stehen in der Lücke rechts neben ihrem Eigentümer, vor der nächsten Spalte.
                foreach (EnergieflussKnoten k in b.Knoten.Where(k => k.Art == EnergieflussKnotenart.Verlust))
                {
                    string eigentuemer = Assert.Single(b.Fluesse, f => f.Nach == k.Id).Von;
                    Rechteck o = rechtecke[eigentuemer], v = rechtecke[k.Id];
                    float naechste = rechtecke.Where(r => b.KnotenMit(r.Key).Art != EnergieflussKnotenart.Verlust && r.Value.X > o.X + KB)
                                              .Select(r => r.Value.X).DefaultIfEmpty(z.Breite).Min();
                    Assert.True(v.X > o.X + KB && v.X + v.Breite < naechste, k.Id + " in der Lücke (" + mass.Breite + ")");
                }
                Assert.True(z.Gleicht(ChartRenderer.EnergieflussModell(d, mass, werte)));
            }
        }
    }
}
