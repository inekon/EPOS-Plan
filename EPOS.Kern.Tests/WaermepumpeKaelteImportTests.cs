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
    /// <b>Der Import für Kälteanlagen</b> (Entscheid E119 vom 10.10.2026): Aus einer VDI-3805-Datei nach
    /// Blatt 22 werden nur Wärmepumpen angeboten, die nach E15 kühlfähig sind (Nennkühlleistung größer
    /// null) und mindestens einen gültigen Kühlblock tragen (<see cref="KuehlblockPruefung"/>); die
    /// übrigen nennt das Leseprotokoll mit Grund.
    ///
    /// <para>Die Proben sind SYNTHETISCH — Phantasienamen und runde Werte, keine Herstellerdaten.</para>
    /// </summary>
    public sealed class WaermepumpeKaelteImportTests : IDisposable
    {
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-e119-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public WaermepumpeKaelteImportTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
        }

        /// <summary>Gerätesatz 700: Name, Wärmeleistung, Zuheizung (Feld 7), Kühlleistung (Feld 20).</summary>
        private static string Geraet(string name, string zuheizung, string kuehlleistung)
            => "700;1;1;" + name + ";10;2.5;2;" + zuheizung + ";;;;;;;;;;;;1;" + kuehlleistung + ";;;;;;;55;;;;4.5;;;";

        private static readonly string[] Heizblock =
        {
            "710.09;1;1;35;15;-5;;100;",
            "710.91;1;-5;7;2;3.5;",
            "710.91;2;7;9;2;4.5;",
        };

        /// <summary>Kühlblock in Kaltwasserlage (Vorlauf 18 °C, Quelle wärmer) — gültig.</summary>
        private static readonly string[] KuehlGueltig =
        {
            "710.09;2;2;18;;;;100;",
            "710.91;1;20;12;2.5;4.8;",
            "710.91;2;35;10;3;3.3;",
        };

        /// <summary>Kühlblock in Heizlage (Vorlauf 45 °C).</summary>
        private static readonly string[] KuehlHeizlage =
        {
            "710.09;3;2;45;;;;100;",
            "710.91;1;-5;6;2;3;",
            "710.91;2;10;8;2;4;",
        };

        /// <summary>Kühlblock mit vertauschten Achsen (Temperaturachse = Kaltwasser).</summary>
        private static readonly string[] KuehlAchsen =
        {
            "710.09;4;2;7;;;;100;",
            "710.91;1;7;11;2.5;4.4;",
        };

        /// <summary>
        /// Sechs Geräte, je eines je Befund — dazu eines ohne Zuheizung, das trotzdem kühlfähig ist.
        /// </summary>
        private string SechsGeraete()
        {
            var z = new List<string>
            {
                "010;22;201903;Phantasie AG;20260101;;;;;;1;;DEU;DE;;",
                "100;1;1;Heizung;;;;;35;",
                "110;1;1;Luft-Wasser;;;;;1;",
                "400;1;Kompakt;",
                "450;1;innen;",
            };
            void Satz(string geraet, params string[][] bloecke)
            {
                z.Add(geraet);
                z.AddRange(Heizblock);
                foreach (string[] b in bloecke) z.AddRange(b);
                // Ein Satz endet erst mit einer fremden Satzart - der naechste 700 schliesst ihn nicht.
                z.Add("900;1;");
            }
            Satz(Geraet("WP Kalt", "6", "12"), KuehlGueltig);
            Satz(Geraet("WP Nur Waerme", "6", "8"));
            Satz(Geraet("WP Heizlage", "6", "8"), KuehlHeizlage);
            Satz(Geraet("WP Achsen", "6", "8"), KuehlAchsen);
            Satz(Geraet("WP Ohne Nennkuehl", "6", ""), KuehlGueltig);
            Satz(Geraet("WP Ohne Zuheizung", "", "9"), KuehlGueltig, KuehlHeizlage);
            Satz(Geraet("WP Gemischt", "6", "8"), KuehlHeizlage, KuehlAchsen);

            string datei = Path.Combine(_ordner, "phantasie.vdi");
            File.WriteAllLines(datei, z, AnsiEncoding.Get());
            return datei;
        }

        [Fact]
        public void Der_Befund_folgt_E15_und_der_Kuehlblockpruefung()
        {
            var imp = new WaermepumpenImport();
            imp.Import(SechsGeraete());
            Assert.Equal(7, imp._list.Count);

            KaelteimportBefund[] befunde = Enumerable.Range(0, imp._list.Count)
                .Select(i => KuehlfaehigkeitsPruefung.Befund(imp, i)).ToArray();

            Assert.Equal(new[]
            {
                KaelteimportBefund.Kuehlfaehig,
                KaelteimportBefund.KeineKuehlkennlinie,
                KaelteimportBefund.NurHeizlage,
                KaelteimportBefund.NurAchsenVertauscht,
                KaelteimportBefund.KeineNennkuehlleistung,
                KaelteimportBefund.Kuehlfaehig,
                KaelteimportBefund.KeinGueltigerKuehlblock,
            }, befunde);
        }

        /// <summary>
        /// Nennkühlleistung ohne Kühlblock: nach dem Katalogkriterium von E15 kühlfähig, aber nicht
        /// rechenbar — der Kälteimport übergeht das Gerät mit „keine Kühlkennlinie“.
        /// </summary>
        [Fact]
        public void Nennkuehlleistung_ohne_Kuehlblock_wird_uebergangen()
        {
            var leer = new List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)>();
            Assert.Equal(KaelteimportBefund.KeineKuehlkennlinie, KuehlfaehigkeitsPruefung.Befund(leer, "12"));
            Assert.Equal(KaelteimportBefund.KeineKuehlkennlinie, KuehlfaehigkeitsPruefung.Befund(null, "12"));
        }

        [Fact]
        public void Der_Ablauf_bietet_nur_kuehlfaehige_Geraete_an_und_nennt_die_uebrigen()
        {
            var ablauf = new KatalogImportAblauf(KatalogImportProfil.Finde(KatalogImportArt.WaermepumpeKuehlung));

            Assert.Equal(2, ablauf.Lesen(SechsGeraete()));
            Assert.Equal(new[] { "WP Kalt", "WP Ohne Zuheizung" }, ablauf.Saetze.Select(s => s.Name).ToArray());

            // Die Zahlenspalte traegt im Kaeltemodus die Kuehlleistung.
            Assert.Equal(new[] { 12.0, 9.0 }, ablauf.Saetze.Select(s => s.Filterwert).ToArray());

            PruefMeldung bilanz = Assert.Single(ablauf.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_KAELTE_BILANZ");
            Assert.Equal(new[] { "2", "7", "5" }, bilanz.Werte);

            var gruende = ablauf.Meldungen.Where(m => m.Schluessel.StartsWith("IMP_KAT_PROT_KAELTE_")
                                                  && m.Schluessel != "IMP_KAT_PROT_KAELTE_BILANZ")
                                          .ToDictionary(m => m.Werte[0], m => m.Schluessel);
            Assert.Equal(5, gruende.Count);
            Assert.Equal("IMP_KAT_PROT_KAELTE_KEINE_KENNLINIE", gruende["WP Nur Waerme"]);
            Assert.Equal("IMP_KAT_PROT_KAELTE_HEIZLAGE", gruende["WP Heizlage"]);
            Assert.Equal("IMP_KAT_PROT_KAELTE_ACHSEN", gruende["WP Achsen"]);
            Assert.Equal("IMP_KAT_PROT_KAELTE_NENNKUEHL", gruende["WP Ohne Nennkuehl"]);
            Assert.Equal("IMP_KAT_PROT_KAELTE_UNGUELTIG", gruende["WP Gemischt"]);
            Assert.All(ablauf.Meldungen.Where(m => gruende.ContainsValue(m.Schluessel)),
                       m => Assert.Equal(PruefStufe.Info, m.Stufe));

            // Die Kuehlblockwarnungen bleiben nur fuer ein ANGEBOTENES Geraet stehen - fuer ein
            // uebergangenes nennt die eigene Zeile den Grund.
            PruefMeldung heizlage = Assert.Single(ablauf.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_KUEHLBLOCK_HEIZLAGE");
            Assert.Equal("WP Ohne Zuheizung", heizlage.Werte[0]);
            Assert.DoesNotContain(ablauf.Meldungen, m => m.Schluessel == "IMP_KAT_PROT_KUEHLBLOCK_ACHSE");
        }

        [Fact]
        public void Der_Waermepumpenimport_bleibt_ungefiltert()
        {
            var ablauf = new KatalogImportAblauf(KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe));

            Assert.Equal(7, ablauf.Lesen(SechsGeraete()));
            Assert.DoesNotContain(ablauf.Meldungen, m => m.Schluessel.StartsWith("IMP_KAT_PROT_KAELTE_"));
            Assert.Equal(10.0, ablauf.Saetze[0].Filterwert);
        }

        /// <summary>
        /// Ein übernommenes Gerät landet im Katalog mit Kühlleistung — auch ohne Zuheizung, an der der
        /// Wärmepumpenimport die Kühlleistung festmacht (Befund W13-B32).
        /// </summary>
        [Fact]
        public void Der_Kaelteimport_uebernimmt_die_Kuehlleistung_auch_ohne_Zuheizung()
        {
            string datei = SechsGeraete();
            var kaelte = new KatalogImportAblauf(KatalogImportProfil.Finde(KatalogImportArt.WaermepumpeKuehlung));
            kaelte.Lesen(datei);
            KatalogImportSatz ohneZuheizung = kaelte.Saetze.Single(s => s.Name == "WP Ohne Zuheizung");
            Assert.Equal(9.0, Convert.ToDouble(ohneZuheizung.Vergleichswerte("x")["Kuehlleistung"]));

            var waerme = new KatalogImportAblauf(KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe));
            waerme.Lesen(datei);
            KatalogImportSatz bestand = waerme.Saetze.Single(s => s.Name == "WP Ohne Zuheizung");
            Assert.Equal(0.0, Convert.ToDouble(bestand.Vergleichswerte("x")["Kuehlleistung"]));
        }

        [Fact]
        public void Das_Profil_teilt_Katalog_Ordner_und_Detailfelder_mit_der_Waermepumpe()
        {
            KatalogImportProfil wp = KatalogImportProfil.Finde(KatalogImportArt.Waermepumpe);
            KatalogImportProfil k = KatalogImportProfil.Finde(KatalogImportArt.WaermepumpeKuehlung);

            Assert.Equal(KatalogImportArt.WaermepumpeKuehlung, k.Art);
            Assert.Equal("WP", k.Katalogschluessel);
            Assert.Equal(wp.Unterordner, k.Unterordner);
            Assert.Equal(wp.UnterordnerRueckfall, k.UnterordnerRueckfall);
            Assert.Equal(KatalogImportProfil.VdiFilter, k.Dateifilter);
            Assert.Equal(wp.Detailfelder.Select(f => f.Schluessel), k.Detailfelder.Select(f => f.Schluessel));
            Assert.Equal("IMP_KAT_HINWEIS_KAELTE", k.Hinweis);
            Assert.Equal("IMP_KAT_SP_LEISTUNG_KUEHL", k.FilterSpaltentitel);
            Assert.NotEqual(wp.Listenprofil.Schluessel, k.Listenprofil.Schluessel);
            Assert.Contains(KatalogImportArt.WaermepumpeKuehlung, KatalogImportProfil.AlleArten);
        }
    }
}
