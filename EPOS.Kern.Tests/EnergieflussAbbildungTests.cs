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
    /// Schritte SK-12 und SK-2c, Variante A): Speicher werden in ihren Bus gefaltet (Entladung als Busband, Verlust als
    /// Abzweig am Bus, Ladung und Entladung im Zeigertext, Name „· Speicher“), Kleinstverluste unter 1 % und Nullbänder
    /// entfallen, jede andere Kante wird genau ein Band; Farbrolle = Energieform; Bandart Verlust bzw. gestrichelt; Spur je
    /// Knoten; Zeigertexte, Kurznamen und Jahressummen übernommen; Legende in der Sprache der Bilanz; und der Renderer
    /// zeichnet die abgebildeten Gaben vollständig (Bänder schließen an, Verluste ohne Knotenbalken).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EnergieflussAbbildungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  Eine Bilanz von Hand, die jede Regel berührt
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

        /// <summary>Jede Kante mit ihrer Energieform; die Rolle folgt der Form (Konzept 3.4, SK-Q14).</summary>
        private static readonly (string Von, string Nach, EnergieflussForm Form)[] KANTEN =
        {
            (Energiefluss.NETZBEZUG, Energiefluss.STROM, EnergieflussForm.Strom),
            (Energiefluss.PHOTOVOLTAIK, Energiefluss.STROM, EnergieflussForm.Strom),
            (Energiefluss.BHKW, Energiefluss.STROM, EnergieflussForm.Strom),
            (Energiefluss.BHKW, Energiefluss.WAERME, EnergieflussForm.Waerme),
            ("traeger:7", Energiefluss.BHKW, EnergieflussForm.Brennstoff),
            ("traeger:7", Energiefluss.HEIZKESSEL, EnergieflussForm.Brennstoff),
            (Energiefluss.BHKW, Energiefluss.VERLUST + Energiefluss.BHKW, EnergieflussForm.Verlust),
            (Energiefluss.HEIZKESSEL, Energiefluss.WAERME, EnergieflussForm.Waerme),
            (Energiefluss.KONDENSATION, Energiefluss.HEIZKESSEL, EnergieflussForm.Umwelt),
            (Energiefluss.STROM, Energiefluss.WP_HEIZEN, EnergieflussForm.Strom),
            (Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN, EnergieflussForm.Umwelt),
            (Energiefluss.WP_HEIZEN, Energiefluss.WAERME, EnergieflussForm.Waerme),
            (Energiefluss.STROM, Energiefluss.HEIZSTAB, EnergieflussForm.Strom),
            (Energiefluss.HEIZSTAB, Energiefluss.WAERME, EnergieflussForm.Waerme),
            (Energiefluss.STROM, Energiefluss.ELEKTROKESSEL, EnergieflussForm.Strom),
            (Energiefluss.ELEKTROKESSEL, Energiefluss.WAERME, EnergieflussForm.Waerme),
            (Energiefluss.ELEKTROKESSEL, Energiefluss.VERLUST + Energiefluss.ELEKTROKESSEL, EnergieflussForm.Verlust),
            (Energiefluss.SOLARTHERMIE, Energiefluss.WAERME, EnergieflussForm.Waerme),
            (Energiefluss.STROM, Energiefluss.WP_KUEHLEN, EnergieflussForm.Strom),
            (Energiefluss.UMWELTKAELTE, Energiefluss.WP_KUEHLEN, EnergieflussForm.Umwelt),
            (Energiefluss.WP_KUEHLEN, Energiefluss.KAELTE, EnergieflussForm.Kaelte),
            (Energiefluss.KUEHLSTROM_EIGEN, "kaeltemaschine:1", EnergieflussForm.Strom),
            ("kaeltemaschine:1", Energiefluss.KAELTE, EnergieflussForm.Kaelte),
            (Energiefluss.FREIE_KUEHLUNG, Energiefluss.KAELTE, EnergieflussForm.Kaelte),
            (Energiefluss.STROM, "split:1", EnergieflussForm.Strom),
            ("split:1", Energiefluss.KAELTE, EnergieflussForm.Kaelte),
            (Energiefluss.STROM, "weitere:split", EnergieflussForm.Strom),
            ("weitere:split", Energiefluss.KAELTE, EnergieflussForm.Kaelte),
            (Energiefluss.WAERME, "puffer:3:0", EnergieflussForm.Waerme),
            ("puffer:3:0", Energiefluss.HEIZUNG, EnergieflussForm.Waerme),
            ("puffer:3:0", Energiefluss.WARMWASSER, EnergieflussForm.Waerme),
            ("puffer:3:0", "verlust:puffer:3:0", EnergieflussForm.Verlust),
            ("puffer:3:0", "fuellstand:puffer:3:0", EnergieflussForm.Waerme),
            (Energiefluss.WAERME, "weitere:puffer", EnergieflussForm.Waerme),
            ("weitere:puffer", Energiefluss.PROZESSWAERME, EnergieflussForm.Waerme),
            (Energiefluss.WAERME, Energiefluss.HEIZUNG, EnergieflussForm.Waerme),
            (Energiefluss.WAERME, Energiefluss.WARMWASSER, EnergieflussForm.Waerme),
            (Energiefluss.WAERME, Energiefluss.PROZESSWAERME, EnergieflussForm.Waerme),
            (Energiefluss.KAELTE, "kaeltespeicher:4:0", EnergieflussForm.Kaelte),
            ("kaeltespeicher:4:0", Energiefluss.KUEHLUNG, EnergieflussForm.Kaelte),
            (Energiefluss.KAELTE, Energiefluss.KUEHLUNG, EnergieflussForm.Kaelte),
            (Energiefluss.STROM, "stromspeicher:1", EnergieflussForm.Strom),
            ("stromspeicher:1", Energiefluss.STROMBEDARF, EnergieflussForm.Strom),
            ("stromspeicher:1", "verlust:stromspeicher:1", EnergieflussForm.Verlust),
            (Energiefluss.STROM, Energiefluss.STROMBEDARF, EnergieflussForm.Strom),
            (Energiefluss.STROM, Energiefluss.EINSPEISUNG_PV, EnergieflussForm.Strom),
            (Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW, EnergieflussForm.Strom),
            (Energiefluss.STROM, Energiefluss.NICHT_ZUGEORDNET, EnergieflussForm.Rest),
            (Energiefluss.NICHT_ZUGEORDNET_QUELLE, Energiefluss.WAERME, EnergieflussForm.Rest),
            (Energiefluss.UNGEDECKT, Energiefluss.HEIZUNG, EnergieflussForm.Rest),
        };

        private static readonly Dictionary<EnergieflussForm, Farbrolle> ROLLE = new Dictionary<EnergieflussForm, Farbrolle>
        {
            { EnergieflussForm.Strom, Farbrolle.STROM }, { EnergieflussForm.Waerme, Farbrolle.WAERME }, { EnergieflussForm.Kaelte, Farbrolle.KAELTE },
            { EnergieflussForm.Brennstoff, Farbrolle.BRENNSTOFF }, { EnergieflussForm.Umwelt, Farbrolle.UMWELTWAERME },
            { EnergieflussForm.Verlust, Farbrolle.VERLUST }, { EnergieflussForm.Rest, Farbrolle.REST },
        };

        private static readonly string[] SPEICHER = { "puffer:3:0", "weitere:puffer", "kaeltespeicher:4:0", "stromspeicher:1" };

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

        private static EnergieflussBildKnoten Knoten(EnergieflussBildDaten d, string id)
            => Assert.Single(d.Knoten, x => x.Kennung == id);

        private static double Fluss(EnergieflussBilanz b, string von, string nach) => b.Fluss(von, nach);

        // =====================================================================
        //  Speicher gefaltet, sonst nichts verloren
        // =====================================================================

        [Fact]
        public void Speicher_werden_in_ihren_Bus_gefaltet_alle_anderen_Kanten_bleiben_Baender()
        {
            EnergieflussBilanz b = VonHand();
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            foreach (string s in SPEICHER)
            {
                Assert.DoesNotContain(d.Knoten, k => k.Kennung == s);
                Assert.DoesNotContain(d.Baender, x => x.Von == s || x.Nach == s);
            }
            // Jede Kante, die keinen Speicher berührt, ist genau ein Band mit Wert und Zeigertext wörtlich.
            foreach (var e in KANTEN.Where(e => !SPEICHER.Contains(e.Von) && !SPEICHER.Contains(e.Nach)))
            {
                EnergieflussBildBand band = Band(d, e.Von, e.Nach);
                Assert.Equal(e.Von == Energiefluss.WAERME && (e.Nach == Energiefluss.HEIZUNG || e.Nach == Energiefluss.WARMWASSER || e.Nach == Energiefluss.PROZESSWAERME)
                             || e.Von == Energiefluss.KAELTE && e.Nach == Energiefluss.KUEHLUNG
                             || e.Von == Energiefluss.STROM && e.Nach == Energiefluss.STROMBEDARF
                             ? Fluss(b, e.Von, e.Nach) + SPEICHER.Sum(s => Fluss(b, s, e.Nach))
                             : Fluss(b, e.Von, e.Nach), band.Mwh, 9);
                if (band.Mwh == Fluss(b, e.Von, e.Nach)) Assert.Equal("Z " + e.Von + "→" + e.Nach, band.Zeigertext);
            }
            // Die Entladung in eine Senke verbreitert das direkte Band; der Zeigertext nennt den Speicheranteil.
            EnergieflussBildBand heizung = Band(d, Energiefluss.WAERME, Energiefluss.HEIZUNG);
            Assert.Contains("davon " + Energiefluss.Zahl(Fluss(b, "puffer:3:0", Energiefluss.HEIZUNG), Energiefluss.Kultur(false)) + " MWh/a über N puffer:3:0", heizung.Zeigertext);
            Assert.StartsWith("N waerme · Speicher → N heizung: ", heizung.Zeigertext);
            // Verlust und Füllstandsänderung des Speichers sind Abzweige am Bus.
            Assert.Equal(Fluss(b, "puffer:3:0", "verlust:puffer:3:0"), Band(d, Energiefluss.WAERME, "verlust:puffer:3:0").Mwh, 9);
            Assert.Equal(Fluss(b, "puffer:3:0", "fuellstand:puffer:3:0"), Band(d, Energiefluss.WAERME, "fuellstand:puffer:3:0").Mwh, 9);
            Assert.Equal("Z puffer:3:0→verlust:puffer:3:0", Band(d, Energiefluss.WAERME, "verlust:puffer:3:0").Zeigertext);
            Assert.Equal(Fluss(b, "stromspeicher:1", "verlust:stromspeicher:1"), Band(d, Energiefluss.STROM, "verlust:stromspeicher:1").Mwh, 9);
            // Der Bus trägt den Zusatz „· Speicher“ und Ladung/Entladung im Zeigertext; die Kurzform bleibt.
            EnergieflussBildKnoten waerme = Knoten(d, Energiefluss.WAERME);
            Assert.Equal("N waerme · Speicher", waerme.Name);
            Assert.Equal("K waerme", waerme.Kurzname);
            Assert.StartsWith("Z waerme · N puffer:3:0: Ladung ", waerme.Zeigertext);
            Assert.Contains("· N weitere:puffer: Ladung ", waerme.Zeigertext);
            Assert.Equal("N strom · Speicher", Knoten(d, Energiefluss.STROM).Name);
            Assert.Equal("N kaelte · Speicher", Knoten(d, Energiefluss.KAELTE).Name);
            // Nichts geht verloren: jede Menge der Bilanz steckt in einem Band, bis auf die Ladungen der Speicher.
            double ladungen = SPEICHER.Sum(s => KANTEN.Where(e => e.Nach == s).Sum(e => Fluss(b, e.Von, s)));
            Assert.Equal(b.Fluesse.Sum(f => f.Mwh) - ladungen, d.Baender.Sum(x => x.Mwh), 6);
            Assert.Equal(b.Knoten.Count - SPEICHER.Length, d.Knoten.Count);
        }

        [Fact]
        public void Kleinstverluste_und_Nullbaender_entfallen_mit_Vermerk_im_Zeigertext()
        {
            var b = new EnergieflussBilanz { Simuliert = true };
            b.Knoten.Add(new EnergieflussKnoten { Id = "gas", Name = "Erdgas", Art = EnergieflussKnotenart.Quelle, AbflussMwh = 100.0, Zeigertext = "Z gas" });
            b.Knoten.Add(new EnergieflussKnoten { Id = "kessel", Name = "Kessel", Art = EnergieflussKnotenart.Wandler, Spalte = 1, ZuflussMwh = 100.0, AbflussMwh = 100.0, Zeigertext = "Z kessel" });
            b.Knoten.Add(new EnergieflussKnoten { Id = "verlust:kessel", Name = "Verlust", Art = EnergieflussKnotenart.Verlust, Spalte = 1, ZuflussMwh = 0.9 });
            b.Knoten.Add(new EnergieflussKnoten { Id = "heizung", Name = "Heizung", Art = EnergieflussKnotenart.Senke, Spalte = 6, ZuflussMwh = 99.1 });
            b.Knoten.Add(new EnergieflussKnoten { Id = "ungedeckt", Name = "ungedeckt", Art = EnergieflussKnotenart.Quelle, AbflussMwh = 0.01 });
            b.Fluesse.Add(new EnergieflussFluss { Von = "gas", Nach = "kessel", Mwh = 100.0, Form = EnergieflussForm.Brennstoff });
            b.Fluesse.Add(new EnergieflussFluss { Von = "kessel", Nach = "heizung", Mwh = 99.1, Form = EnergieflussForm.Waerme });
            b.Fluesse.Add(new EnergieflussFluss { Von = "kessel", Nach = "verlust:kessel", Mwh = 0.9, Form = EnergieflussForm.Verlust });
            b.Fluesse.Add(new EnergieflussFluss { Von = "ungedeckt", Nach = "heizung", Mwh = 0.01, Form = EnergieflussForm.Rest });
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            Assert.Equal(2, d.Baender.Count);
            Assert.DoesNotContain(d.Knoten, k => k.Kennung == "verlust:kessel" || k.Kennung == "ungedeckt");
            Assert.Equal("Z kessel · Verlust 0,90 MWh/a (unter 1 %, nicht gezeichnet)", Knoten(d, "kessel").Zeigertext);
            Assert.Equal(new[] { Farbrolle.WAERME, Farbrolle.BRENNSTOFF }, d.Legende.Select(l => l.Rolle).ToArray());
            // Ab 1 % bleibt der Verlust ein Band.
            b.Fluesse[2].Mwh = 1.0;
            Assert.Equal(3, EnergieflussAbbildung.Bild(b).Baender.Count);
        }

        [Fact]
        public void Knoten_tragen_Spur_Art_Namen_Kurznamen_Zeigertext_und_Jahressumme()
        {
            EnergieflussBilanz b = VonHand();
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            foreach (EnergieflussKnoten k in b.Knoten.Where(k => k.Art != EnergieflussKnotenart.Speicher && k.Art != EnergieflussKnotenart.Bus))
            {
                EnergieflussBildKnoten bk = Knoten(d, k.Id);
                Assert.Equal(k.Art, bk.Art);
                Assert.Equal(k.Name, bk.Name);
                Assert.Equal(k.Kurzname, bk.Kurzname);
                Assert.Equal(k.Zeigertext, bk.Zeigertext);
                Assert.Equal(k.Art == EnergieflussKnotenart.Quelle ? k.AbflussMwh : k.ZuflussMwh, bk.Mwh, 9);
                Assert.Equal(k.Art == EnergieflussKnotenart.Verlust ? Farbrolle.VERLUST : Farbrolle.KNOTEN, bk.Rolle);
            }
            var spuren = new Dictionary<string, EnergieflussSpur>
            {
                { Energiefluss.STROM, EnergieflussSpur.Strom }, { Energiefluss.WAERME, EnergieflussSpur.Waerme }, { Energiefluss.KAELTE, EnergieflussSpur.Kaelte },
                { Energiefluss.BHKW, EnergieflussSpur.Gemischt }, { Energiefluss.HEIZKESSEL, EnergieflussSpur.Waerme },
                { Energiefluss.WP_HEIZEN, EnergieflussSpur.Waerme }, { Energiefluss.HEIZSTAB, EnergieflussSpur.Waerme },
                { Energiefluss.WP_KUEHLEN, EnergieflussSpur.Kaelte }, { "kaeltemaschine:1", EnergieflussSpur.Kaelte }, { "split:1", EnergieflussSpur.Kaelte },
                { Energiefluss.NETZBEZUG, EnergieflussSpur.Strom }, { Energiefluss.PHOTOVOLTAIK, EnergieflussSpur.Strom },
                { Energiefluss.UMWELTWAERME, EnergieflussSpur.Waerme }, { Energiefluss.UMWELTKAELTE, EnergieflussSpur.Kaelte },
                { Energiefluss.KUEHLSTROM_EIGEN, EnergieflussSpur.Kaelte }, { Energiefluss.UNGEDECKT, EnergieflussSpur.Waerme },
                { "traeger:7", EnergieflussSpur.Waerme },   // der größere Abfluss geht in den Kessel
                { Energiefluss.HEIZUNG, EnergieflussSpur.Waerme }, { Energiefluss.KUEHLUNG, EnergieflussSpur.Kaelte },
                { Energiefluss.STROMBEDARF, EnergieflussSpur.Strom }, { Energiefluss.NICHT_ZUGEORDNET, EnergieflussSpur.Strom },
                { Energiefluss.EINSPEISUNG_BHKW, EnergieflussSpur.Strom },
                { Energiefluss.VERLUST + Energiefluss.BHKW, EnergieflussSpur.Gemischt }, { "verlust:puffer:3:0", EnergieflussSpur.Waerme },
                { "verlust:stromspeicher:1", EnergieflussSpur.Strom },
            };
            foreach (var s in spuren) Assert.True(s.Value == Knoten(d, s.Key).Spur, s.Key + ": erwartet " + s.Value + ", ist " + Knoten(d, s.Key).Spur);
        }

        [Fact]
        public void Leere_Zeigertexte_werden_null_der_Renderer_bildet_dann_seinen_Vorgabetext()
        {
            EnergieflussBilanz b = VonHand();
            b.Fluesse[0].Zeigertext = "";
            b.Knoten[0].Zeigertext = "";
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            Assert.Null(Band(d, b.Fluesse[0].Von, b.Fluesse[0].Nach).Zeigertext);
            Assert.Null(Knoten(d, b.Knoten[0].Id).Zeigertext);
        }

        // =====================================================================
        //  Rollen, Bandarten, Legende (Konzept 3.4, SK-Q14)
        // =====================================================================

        [Fact]
        public void Rolle_ist_die_Energieform_des_Bandes_Verlust_und_Rest_nach_Bandart()
        {
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(VonHand());
            foreach (var e in KANTEN.Where(e => !SPEICHER.Contains(e.Von) && !SPEICHER.Contains(e.Nach)))
            {
                EnergieflussBildBand band = Band(d, e.Von, e.Nach);
                Assert.True(ROLLE[e.Form] == band.Rolle, e.Von + " → " + e.Nach + ": erwartet " + ROLLE[e.Form] + ", ist " + band.Rolle);
                EnergieflussBandart art = e.Form == EnergieflussForm.Verlust ? EnergieflussBandart.Verlust
                                        : e.Form == EnergieflussForm.Rest ? EnergieflussBandart.NichtZugeordnet : EnergieflussBandart.Normal;
                Assert.True(art == band.Art, e.Von + " → " + e.Nach + ": erwartet " + art + ", ist " + band.Art);
            }
            Assert.Equal(Farbrolle.VERLUST, Band(d, Energiefluss.WAERME, "verlust:puffer:3:0").Rolle);
            Assert.Equal(EnergieflussBandart.Verlust, Band(d, Energiefluss.WAERME, "verlust:puffer:3:0").Art);
            Assert.Equal(Farbrolle.WAERME, Band(d, Energiefluss.WAERME, "fuellstand:puffer:3:0").Rolle);
            foreach (EnergieflussForm f in Enum.GetValues(typeof(EnergieflussForm)))
                Assert.Equal(ROLLE[f], EnergieflussAbbildung.Rolle(f));
        }

        [Fact]
        public void Legende_nennt_die_vorkommenden_Formen_in_fester_Reihenfolge_und_in_der_Sprache_der_Bilanz()
        {
            Assert.Equal(new[] { "Strom", "Wärme", "Kälte", "Brennstoff", "Umweltwärme / Umwelt (Kälte)", "Verlust", "nicht zugeordnet / ungedeckt" },
                         EnergieflussAbbildung.Bild(VonHand()).Legende.Select(l => l.Text).ToArray());
            IReadOnlyList<EnergieflussBildLegende> en = EnergieflussAbbildung.Bild(VonHand(englisch: true)).Legende;
            Assert.Equal(new[] { "Electricity", "Heat", "Cooling", "Fuel", "Ambient heat / ambient (cooling)", "Loss", "unassigned / uncovered" },
                         en.Select(l => l.Text).ToArray());
            Assert.Equal(new[] { Farbrolle.STROM, Farbrolle.WAERME, Farbrolle.KAELTE, Farbrolle.BRENNSTOFF, Farbrolle.UMWELTWAERME, Farbrolle.VERLUST, Farbrolle.REST },
                         en.Select(l => l.Rolle).ToArray());
            Assert.Equal(new[] { false, false, false, false, false, false, true }, en.Select(l => l.Gestrichelt).ToArray());
            Assert.Equal("Storage", Energiefluss.T("EF_SP_SPEICHER", true));
            Assert.EndsWith(" · Storage", Knoten(EnergieflussAbbildung.Bild(VonHand(englisch: true)), Energiefluss.WAERME).Name);
        }

        [Fact]
        public void Unbekannte_Knoten_bekommen_Rolle_und_Spur_aus_der_Energieform()
        {
            var b = new EnergieflussBilanz { Simuliert = true };
            b.Knoten.Add(new EnergieflussKnoten { Id = "fremd_a", Art = EnergieflussKnotenart.Wandler, Spalte = 1 });
            b.Knoten.Add(new EnergieflussKnoten { Id = "fremd_b", Art = EnergieflussKnotenart.Wandler, Spalte = 3 });
            b.Knoten.Add(new EnergieflussKnoten { Id = "fremd_c", Art = EnergieflussKnotenart.Senke, Spalte = 6 });
            b.Fluesse.Add(new EnergieflussFluss { Von = "fremd_a", Nach = "fremd_b", Mwh = 1.0, Form = EnergieflussForm.Strom });
            b.Fluesse.Add(new EnergieflussFluss { Von = "fremd_b", Nach = "fremd_c", Mwh = 1.0, Form = EnergieflussForm.Kaelte });
            b.Fluesse.Add(new EnergieflussFluss { Von = "fremd_b", Nach = "unbekannt", Mwh = 1.0, Form = EnergieflussForm.Waerme });
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            Assert.Equal(2, d.Baender.Count);
            Assert.Equal(Farbrolle.STROM, Band(d, "fremd_a", "fremd_b").Rolle);
            Assert.Equal(Farbrolle.KAELTE, Band(d, "fremd_b", "fremd_c").Rolle);
            Assert.Equal(EnergieflussSpur.Strom, Knoten(d, "fremd_a").Spur);
            Assert.Equal(EnergieflussSpur.Kaelte, Knoten(d, "fremd_b").Spur);
            Assert.Equal(EnergieflussSpur.Kaelte, Knoten(d, "fremd_c").Spur);
            Assert.All(d.Baender, x => Assert.Equal(EnergieflussBandart.Normal, x.Art));
        }

        [Fact]
        public void Ohne_Bilanz_leere_Bilddaten_ohne_Ausnahme()
        {
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(null);
            Assert.Empty(d.Knoten);
            Assert.Empty(d.Baender);
            Assert.Empty(d.Legende);
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
        public void Bilanz_aus_dem_Flussbau_wird_vollstaendig_gezeichnet_Speicher_am_Bus_Verluste_ohne_Balken()
        {
            EnergieflussBilanz b = Energiefluss.Aus(Baum(), null, false, "Stamm", 4711);
            Assert.False(b.Leer);
            Assert.Contains(b.Knoten, k => k.Art == EnergieflussKnotenart.Speicher);
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);
            Assert.DoesNotContain(d.Knoten, k => k.Art == EnergieflussKnotenart.Speicher);
            Assert.Contains(d.Baender, x => x.Von == Energiefluss.WAERME && x.Nach.StartsWith(Energiefluss.VERLUST, StringComparison.Ordinal));
            Assert.NotEmpty(d.Legende);

            foreach (var (mass, werte) in new[] { (ChartRenderer.ENERGIEFLUSS_BREIT, false), (ChartRenderer.ENERGIEFLUSS_SCHMAL, false), (ChartRenderer.ENERGIEFLUSS_BERICHT, true) })
            {
                Zeichenmodell z = ChartRenderer.EnergieflussModell(d, mass, werte);
                Gezeichnet(d, z, "(" + mass.Breite + ")");
                Assert.True(z.Gleicht(ChartRenderer.EnergieflussModell(d, mass, werte)));
            }
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen(d));
        }

        /// <summary>
        /// Jedes Band der Bilddaten ist genau ein Pfad, der an seinem Quellknoten beginnt und — außer einem Verlustabzweig —
        /// an seinem Zielknoten endet; jeder Knoten außer den Verlustknoten ist genau ein Rechteck; Zeigertexte wörtlich.
        /// </summary>
        internal static void Gezeichnet(EnergieflussBildDaten d, Zeichenmodell z, string wo)
        {
            var rechtecke = z.Befehle.OfType<Rechteck>().Where(r => r.Marke != null && r.Marke.StartsWith("knoten:", StringComparison.Ordinal))
                             .ToDictionary(r => r.Marke.Substring("knoten:".Length), r => r);
            var baender = z.Befehle.OfType<Pfad>().Where(p => p.Marke != null && p.Marke.StartsWith("fluss:", StringComparison.Ordinal)).ToList();
            var verluste = new HashSet<string>(d.Knoten.Where(k => k.Art == EnergieflussKnotenart.Verlust).Select(k => k.Kennung), StringComparer.Ordinal);
            Assert.Equal(d.Knoten.Count(k => k.Art != EnergieflussKnotenart.Verlust), rechtecke.Count);
            Assert.Equal(d.Baender.Count, baender.Count);
            foreach (EnergieflussBildBand f in d.Baender)
            {
                Pfad p = Assert.Single(baender, x => x.Marke == "fluss:" + f.Von + ":" + f.Nach);
                int n = p.Punkte.Count / 2;
                Rechteck von = rechtecke[f.Von];
                Assert.Equal(von.X + von.Breite, p.Punkte[0].X, 3);
                Assert.InRange(p.Punkte[0].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
                if (verluste.Contains(f.Nach))
                {
                    Assert.False(rechtecke.ContainsKey(f.Nach), wo + " " + f.Nach + " hat keinen Balken");
                    Assert.Equal(2 * ChartRenderer.EF_STUETZSTELLEN, p.Punkte.Count);
                    Assert.True(p.Punkte[n - 1].X - p.Punkte[0].X <= 44f + 0.01f, wo + " Abzweig kurz");
                }
                else
                {
                    Rechteck nach = rechtecke[f.Nach];
                    Assert.Equal(nach.X, p.Punkte[n - 1].X, 3);
                    Assert.InRange(p.Punkte[n - 1].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
                }
                if (f.Zeigertext != null) Assert.Equal(f.Zeigertext, p.Wert);
            }
            foreach (EnergieflussBildKnoten k in d.Knoten.Where(k => k.Art != EnergieflussKnotenart.Verlust))
                if (k.Zeigertext != null) Assert.Equal(k.Zeigertext, rechtecke[k.Kennung].Wert);
            foreach (Rechteck r in rechtecke.Values)
                Assert.True(r.X >= 0 && r.Y >= 0 && r.X + r.Breite <= z.Breite && r.Y + r.Hoehe <= z.Hoehe - ChartRenderer.EF_LEGENDE_HOEHE, wo + " " + r.Marke + " im Bild");
        }
    }
}
