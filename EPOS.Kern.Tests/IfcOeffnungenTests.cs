using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G5-2 — der Abzug der Öffnungen</b> (Abstimmung G5, A1): Fläche je Öffnung in der Rangfolge Mengensatz,
    /// Gesamtmaße, Körper der Öffnung, Körper des Fensters bzw. der Tür, samt Herkunft; Abzug am Wirt gegen Handrechnung,
    /// Loch und Nische, Nettofläche ≤ 0, Fenster mit der Orientierung des Wirts, Dachfenster in der geneigten Platte, kein
    /// Doppeln, die Gegenprobe mit Mengensätzen und die Probenbytes (<see cref="IfcProbenErzeuger.Oeffnungsproben"/>).
    /// Ohne Datenbank.
    /// </summary>
    public sealed class IfcOeffnungenTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const double TOL = 1e-6;
        private static readonly double DACHNEIGUNG = Math.Acos(0.8) * 180.0 / Math.PI;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("en-US");

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static GebaeudeImportAblauf Lesen(string datei)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static IEnumerable<AbbildBauteil> Bauteile(GebaeudeImportAblauf a)
            => a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.Abbild.BauteileOhneGebaeude);

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string name) => Bauteile(a).Single(b => b.Name == name);

        private static AbbildBauteil Oeffnung(GebaeudeImportAblauf a, string name)
            => Bauteile(a).SelectMany(b => b.Oeffnungen).Single(o => o.Name == name);

        private static void Nah(double erwartet, double? ist, string wo = "")
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(erwartet - ist.Value) <= TOL, wo + ": erwartet " + erwartet.ToString("R") + ", ist " + ist.Value.ToString("R"));
        }

        /// <summary>Ein achsparalleler Quader [m] als geschlossener Körper aus zwölf Dreiecken, Umlauf nach außen.</summary>
        private static Dateikoerper Quader(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            var punkte = new List<double[]>();
            for (int i = 0; i < 8; i++)
                punkte.Add(new[] { (i & 1) == 0 ? x0 : x1, (i & 2) == 0 ? y0 : y1, (i & 4) == 0 ? z0 : z1 });
            int[][] flaechen =
            {
                new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 },
                new[] { 1, 3, 7, 5 }, new[] { 3, 2, 6, 7 }, new[] { 2, 0, 4, 6 },
            };
            var dreiecke = new List<int[]>();
            foreach (int[] f in flaechen)
            {
                dreiecke.Add(new[] { f[0], f[1], f[2] });
                dreiecke.Add(new[] { f[0], f[2], f[3] });
            }
            return new Dateikoerper { PunkteM = punkte, Dreiecke = dreiecke };
        }

        // ==================================================================
        //  Rechenkern der Öffnungen
        // ==================================================================

        [Fact]
        public void Profilflaeche_und_Tiefe_eines_Quaders_quer_zur_Wand()
        {
            Dateikoerper k = Quader(1.0, -0.3, 0.9, 2.5, 0.0, 2.1);   // 1,5 × 1,2 m, 0,3 m tief
            double[] n = { 0.0, -1.0, 0.0 };
            Nah(1.8, IfcOeffnungen.Profilflaeche(k, n));
            Nah(0.3, IfcOeffnungen.Tiefe(k, n));
            Nah(1.5 * 0.3, IfcOeffnungen.Profilflaeche(k, new[] { 0.0, 0.0, 1.0 }));   // Draufsicht
            double[] h = IfcOeffnungen.Hauptnormale(k);
            Assert.Equal(1.0, Math.Abs(h[1]), 9);
            Assert.Null(IfcOeffnungen.Profilflaeche(null, n));
        }

        [Theory]
        [InlineData(0.1, 0.3, true)]
        [InlineData(0.3, 0.3, false)]
        [InlineData(0.2995, 0.3, false)]   // innerhalb der Toleranz 1 mm: durchgehend
        [InlineData(0.4, 0.3, false)]
        public void Nische_nur_wenn_weniger_tief_als_der_Wirt_dick(double tiefe, double dicke, bool nische)
        {
            Assert.Equal(nische, IfcOeffnungen.Nische(tiefe, dicke));
            Assert.False(IfcOeffnungen.Nische(null, dicke));
            Assert.False(IfcOeffnungen.Nische(tiefe, null));
        }

        [Theory]
        [InlineData(25.0, 3.0, 22.0, false)]
        [InlineData(1.0, 1.0, 0.0, true)]
        [InlineData(0.5, 1.0, 0.0, true)]
        public void Netto_ist_Brutto_minus_Abzug_nie_negativ(double brutto, double abzug, double netto, bool nichtPositiv)
        {
            Nah(netto, IfcOeffnungen.Netto(brutto, abzug, out bool np));
            Assert.Equal(nichtPositiv, np);
        }

        // ==================================================================
        //  Probe ohne Mengensätze
        // ==================================================================

        [Fact]
        public void Rangfolge_und_Herkunft_der_Oeffnungsflaeche()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_oeffnungen.ifc");
            (string Name, double Flaeche, Flaechenherkunft Herkunft)[] erwartet =
            {
                ("Fenster A", 1.5 * 1.2, Flaechenherkunft.Mengensatz),     // OverallWidth × OverallHeight
                ("Fenster B", 1.2 * 1.0, Flaechenherkunft.Koerper),        // Körper der Öffnung vor dem des Fensters (0,99 m²)
                ("Tür", 1.0 * 2.0, Flaechenherkunft.Koerper),              // Öffnung ohne Körper: Körper der Tür
                ("Dachfenster", 1.0 * 1.2, Flaechenherkunft.Koerper),      // Öffnung in der Dachebene
                ("Fenster Gaube", 2.0 * 1.2, Flaechenherkunft.Mengensatz),
            };
            foreach ((string name, double flaeche, Flaechenherkunft herkunft) in erwartet)
            {
                AbbildBauteil o = Oeffnung(a, name);
                Nah(flaeche, o.BruttoflaecheM2, name);
                Assert.Equal(herkunft, o.Flaechenherkunft);
            }
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_KOERPER");
            Assert.Equal("3", m.Werte[0]);
            Assert.DoesNotContain(a.Meldungen, x => x.Schluessel == P + "KEINE_MENGEN" || x.Schluessel == P + "OEFFNUNG_UNBEMESSEN");
        }

        [Fact]
        public void Abzug_am_Wirt_gegen_Handrechnung_mit_Loch_und_Nische()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_oeffnungen.ifc");
            (string Name, double Brutto, double? Netto, double Loch)[] erwartet =
            {
                ("Wand Süd", 25.0, 25.0 - 1.8 - 1.2, 0.0),
                ("Wand West", 20.0, 20.0 - 2.0, 0.0),
                ("Wand Nord", 25.0, 25.0 - 1.0, 1.0),        // Loch abgezogen, Nische nicht
                ("Wand Ost", 20.0, null, 0.0),               // ohne Öffnung keine eigene Nettofläche
                ("Dachplatte", 100.0, 100.0 - 1.2, 0.0),
            };
            foreach ((string name, double brutto, double? netto, double loch) in erwartet)
            {
                AbbildBauteil b = Bauteil(a, name);
                Nah(brutto, b.BruttoflaecheM2, name);
                Assert.Equal(Flaechenherkunft.Koerper, b.Flaechenherkunft);
                if (netto.HasValue) Nah(netto.Value, b.NettoflaecheM2, name);
                else Assert.Null(b.NettoflaecheM2);
                Nah(loch, b.LochflaecheM2, name);
            }
            PruefMeldung l = Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_LOCH");
            Assert.Equal(new[] { "1", "1" }, l.Werte.Take(2));
            Assert.Contains("Loch", l.Werte[2]);
            PruefMeldung n = Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_NISCHE");
            Assert.Equal("1", n.Werte[0]);
            Assert.Contains("Nische", n.Werte[1]);
        }

        /// <summary>
        /// B2 (Abstimmung G5): Die „Wand Gaube“ (Körper 2 × 1 m = 2 m²) trägt das „Fenster Gaube“ (2,0 × 1,2 m = 2,4 m²) —
        /// mehr Öffnung als Wand. Das Abbild hält Brutto 2 und Netto 0 (nie negativ), ohne eigene Warnung des Lesers; der
        /// Bauteilvorschlag legt die Wand nicht an und nennt sie in genau einer Info-Zeile (Zahl 1, Name). Das Fenster bleibt mit
        /// seinen 2,4 m² und der Orientierung der Wand (Ost, senkrecht) — es ist die Hülle an ihrer Stelle. Jedes übrige Bauteil
        /// behält seine Fläche (Netto, sonst Brutto des Abbilds).
        /// </summary>
        [Fact]
        public void B2_Wand_mit_Nettoflaeche_null_entfaellt_ihr_Fenster_bleibt()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_oeffnungen.ifc");
            AbbildBauteil g = Bauteil(a, "Wand Gaube");
            Nah(2.0, g.BruttoflaecheM2);
            Nah(0.0, g.NettoflaecheM2);
            Assert.DoesNotContain(a.Meldungen, x => x.Schluessel.EndsWith("NETTO_NULL", StringComparison.Ordinal));

            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            Assert.DoesNotContain(v.Zeilen, z => z.Kennung == g.Kennung);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.NETTO_NULL_ENTFALLEN);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1", "Wand Gaube" }, m.Werte);

            GebaeudeBauteilzeile f = Assert.Single(v.Zeilen, z => z.Kennung == Oeffnung(a, "Fenster Gaube").Kennung);
            Nah(2.4, f.Bauteil.Flaeche, "Fenster Gaube");
            Nah(90.0, f.Bauteil.Azimut, "Fenster Gaube Azimut");

            foreach (AbbildBauteil b in Bauteile(a).Where(b => b != g))
            {
                GebaeudeBauteilzeile z = Assert.Single(v.Zeilen, x => x.Kennung == b.Kennung);
                Nah((b.NettoflaecheM2 ?? b.BruttoflaecheM2).Value, z.Bauteil.Flaeche, b.Name);
            }
        }

        [Fact]
        public void B2_Die_Zeile_nennt_hoechstens_zehn_Namen()
        {
            var namen = Enumerable.Range(1, 12).Select(i => "W" + i).ToList();
            Assert.Equal("W1, W2, W3, W4, W5, W6, W7, W8, W9, W10, …", GebaeudeBauteilvorschlag.Entfallenliste(namen));
            Assert.Equal("W1, W2", GebaeudeBauteilvorschlag.Entfallenliste(namen.Take(2).ToList()));
            Assert.Equal("W1, W2, W3, W4, W5, W6, W7, W8, W9, W10", GebaeudeBauteilvorschlag.Entfallenliste(namen.Take(10).ToList()));
        }

        [Fact]
        public void Fenster_und_Tuer_tragen_die_Orientierung_des_Wirts_das_Dachfenster_die_der_Platte()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_oeffnungen.ifc");
            (string Name, double Azimut, double Neigung)[] erwartet =
            {
                ("Fenster A", 180.0, 90.0), ("Fenster B", 180.0, 90.0), ("Tür", 270.0, 90.0),
                ("Fenster Gaube", 90.0, 90.0), ("Dachfenster", 180.0, DACHNEIGUNG),
            };
            foreach ((string name, double azimut, double neigung) in erwartet)
            {
                AbbildBauteil o = Oeffnung(a, name);
                Nah(azimut, o.AzimutGrad, name);
                Nah(neigung, o.NeigungGrad, name);
            }
            Nah(DACHNEIGUNG, Bauteil(a, "Dachplatte").NeigungGrad);

            // Bis in die Bauteilzeilen: Fenster und Tür mit Fläche und Orientierung des Wirts, die Wände netto.
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            foreach ((string name, double azimut, double neigung) in erwartet)
            {
                AbbildBauteil o = Oeffnung(a, name);
                GebaeudeBauteilzeile z = Assert.Single(v.Zeilen, x => x.Kennung == o.Kennung);
                Nah(o.BruttoflaecheM2.Value, z.Bauteil.Flaeche, name);
                Nah(azimut, z.Bauteil.Azimut, name);
                Nah(neigung, z.Bauteil.Neigung, name);
            }
            Nah(22.0, Assert.Single(v.Zeilen, x => x.Kennung == Bauteil(a, "Wand Süd").Kennung).Bauteil.Flaeche);
            Nah(24.0, Assert.Single(v.Zeilen, x => x.Kennung == Bauteil(a, "Wand Nord").Kennung).Bauteil.Flaeche);
            Nah(98.8, Assert.Single(v.Zeilen, x => x.Kennung == Bauteil(a, "Dachplatte").Kennung).Bauteil.Flaeche);
        }

        [Fact]
        public void Kein_Fenster_doppelt_jedes_Fenster_an_genau_einem_Wirt()
        {
            foreach (string datei in new[] { "ifc4_g5_oeffnungen.ifc", "ifc4_g5_oeffnungen_mengen.ifc", "ifc4_haus.ifc" })
            {
                GebaeudeImportAblauf a = Lesen(datei);
                List<string> kennungen = Bauteile(a).SelectMany(b => b.Oeffnungen).Select(o => o.Kennung).ToList();
                Assert.Equal(kennungen.Count, kennungen.Distinct().Count());
                Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == P + "OHNE_WIRT");
                GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
                foreach (string k in kennungen) Assert.True(v.Zeilen.Count(z => z.Kennung == k) <= 1, datei + ": " + k);
            }
            Assert.Equal(5, Bauteile(Lesen("ifc4_g5_oeffnungen.ifc")).Sum(b => b.Oeffnungen.Count));
        }

        // ==================================================================
        //  Gegenprobe mit Mengensätzen
        // ==================================================================

        [Fact]
        public void Gegenprobe_Mengensatz_bleibt_Quelle_Netto_gleich_Brutto_minus_Oeffnungen()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_oeffnungen_mengen.ifc");
            (string Name, double Brutto, double Netto)[] werte =
            {
                ("Wand Süd", 25.0, 22.0), ("Wand West", 20.0, 18.0), ("Wand Nord", 25.0, 24.0), ("Wand Ost", 20.0, 20.0),
                ("Dachplatte", 100.0, 98.8), ("Wand Gaube", 2.0, 0.0),
            };
            foreach ((string name, double brutto, double netto) in werte)
            {
                AbbildBauteil b = Bauteil(a, name);
                Nah(brutto, b.BruttoflaecheM2, name);
                Nah(netto, b.NettoflaecheM2, name);
                Assert.Equal(Flaechenherkunft.Mengensatz, b.Flaechenherkunft);
                double rechnung = brutto - b.Oeffnungen.Sum(o => o.BruttoflaecheM2 ?? 0.0) - b.LochflaecheM2;
                if (netto > 0.0) Assert.True(Math.Abs(rechnung - netto) <= 0.02 * brutto, name + ": " + rechnung.ToString("R"));
            }
            foreach (string name in new[] { "Fenster A", "Fenster B", "Tür", "Dachfenster", "Fenster Gaube" })
                Assert.Equal(Flaechenherkunft.Mengensatz, Oeffnung(a, name).Flaechenherkunft);
            Nah(1.0, Bauteil(a, "Wand Nord").LochflaecheM2);
            Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_LOCH");
            Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_NISCHE");
            Assert.DoesNotContain(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_KOERPER"
                                                    || x.Schluessel == P + "KOERPER_ABWEICHUNG");

            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            foreach ((string name, double _, double netto) in werte.Where(w => w.Netto > 0.0))
                Nah(netto, Assert.Single(v.Zeilen, x => x.Kennung == Bauteil(a, name).Kennung).Bauteil.Flaeche, name);
        }

        // ==================================================================
        //  Probe: Aussparungshaus — kein doppelter Abzug, Teile nicht doppelt (G5-N)
        // ==================================================================

        [Fact]
        public void Im_Koerper_ausgesparte_Oeffnungen_werden_nicht_doppelt_abgezogen()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_aussparung.ifc");

            // Südwand: Körper 25 − 1,8 (Fenster A ausgespart) = 23,2; brutto 25; netto 25 − 1,8 − 1,2 = 22.
            AbbildBauteil sued = Bauteil(a, "Wand Süd");
            Assert.Equal(Flaechenherkunft.Koerper, sued.Flaechenherkunft);
            Nah(23.2, sued.Koerperflaeche.FlaecheM2, "Süd Körper");
            Nah(25.0, sued.BruttoflaecheM2, "Süd brutto");
            Nah(22.0, sued.NettoflaecheM2, "Süd netto");
            Nah(1.8, Oeffnung(a, "Fenster A").BruttoflaecheM2, "Fenster A");
            Nah(1.2, Oeffnung(a, "Fenster B").BruttoflaecheM2, "Fenster B");

            // Ostwand: ein ausgespartes Loch 1 × 1 m — Körper 19, brutto 20, netto 19.
            AbbildBauteil ost = Bauteil(a, "Wand Ost");
            Nah(19.0, ost.Koerperflaeche.FlaecheM2, "Ost Körper");
            Nah(20.0, ost.BruttoflaecheM2, "Ost brutto");
            Nah(19.0, ost.NettoflaecheM2, "Ost netto");
            Nah(1.0, ost.LochflaecheM2, "Ost Loch");

            // Westwand ohne Öffnung: unverändert.
            Nah(20.0, Bauteil(a, "Wand West").BruttoflaecheM2, "West");
            Assert.Null(Bauteil(a, "Wand West").NettoflaecheM2);

            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "OEFFNUNG_AUSGESPART");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "2", "2.8", "Öffnung Loch, Fenster A" }, m.Werte);
        }

        [Fact]
        public void Teile_mit_Mengensatz_neben_einem_Koerper_ohne_Mengensatz_zaehlen_nicht_doppelt()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_aussparung.ifc");
            AbbildBauteil nord = Bauteil(a, "Wand Nord");
            Nah(25.0, nord.Koerperflaeche.FlaecheM2, "Nord Körper");
            Nah(2.0, nord.BruttoflaecheM2, "Nord Rest");
            Assert.Equal(Flaechenherkunft.Koerper, nord.Flaechenherkunft);
            foreach ((string name, double brutto) in new[] { ("Wand Nord-1", 15.0), ("Wand Nord-2", 8.0) })
            {
                AbbildBauteil teil = Bauteil(a, name);
                Nah(brutto, teil.BruttoflaecheM2, name);
                Assert.Equal(Flaechenherkunft.Mengensatz, teil.Flaechenherkunft);
            }
            // Körper, Teile und Rest zusammen: genau die Körperfläche, keine Fläche doppelt.
            Nah(25.0, new[] { "Wand Nord", "Wand Nord-1", "Wand Nord-2" }.Sum(n => Bauteil(a, n).BruttoflaecheM2.Value), "Summe");
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "KOERPER_REST");
            Assert.Equal(new[] { "1", "Wand Nord" }, m.Werte);
        }

        // ==================================================================
        //  Proben
        // ==================================================================

        [Fact(Skip = "Erzeuger: schreibt die Öffnungsproben nach Referenzlaeufe/Importproben — nur von Hand, siehe IfcProbenTests.")]
        public void Erzeuger_schreibt_die_Oeffnungsproben()
        {
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Oeffnungsproben())
                File.WriteAllBytes(Path.Combine(IfcProbenTests.Ordner(), p.Key), p.Value);
        }

        [Fact]
        public void Die_abgelegten_Oeffnungsproben_sind_byte_gleich_neu_erzeugbar()
        {
            var funde = new List<string>();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Oeffnungsproben())
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), p.Key);
                if (!File.Exists(pfad)) funde.Add(p.Key + ": fehlt");
                else if (!File.ReadAllBytes(pfad).SequenceEqual(p.Value)) funde.Add(p.Key + ": weicht ab");
                else Assert.True(p.Value.Length < 32 * 1024, p.Key + " ist keine Kleinstdatei");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }
    }
}
