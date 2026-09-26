using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #351 (U23 und U26) — <b>der Satz sieht überall gleich aus, und wo er vom
    /// Vorschlag abweicht, steht es dabei.</b>
    ///
    /// <para><b>Der Befund.</b> Die beiden Satzfelder des BHKW-Dialogs führen vier
    /// Nachkommastellen, weil die Leistungsstaffel des § 7 sie erzeugt (300 kW:
    /// 50 × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40 = 1.670 ÷ 300 = 5,5667 ct/kWh).
    /// Herleitung, Bericht und Erlösrubrik rundeten dieselbe Größe auf zwei Stellen —
    /// derselbe Satz stand an zwei Orten verschieden da. Und der Modulnachweis trug den
    /// ANGESETZTEN Satz neben der Herleitung des VORSCHLAGS, ohne beide zu vergleichen:
    /// Wer 6,0000 ct/kWh neben einer Tranchenrechnung las, die 5,5667 ergibt, musste
    /// selbst nachrechnen, ob das ein eigener Wert ist.</para>
    ///
    /// <para><b>Was hier gemessen wird.</b> Das Format steht EINMAL im Kern
    /// (<see cref="KwkgSatzHerkunft"/>), der Vergleich läuft auf der Stellenzahl des
    /// FELDES, und die Erlösrubrik trägt je Geldzeile eine Unterzeile mit Satz und
    /// Herkunft. Die Gegenprobe zum Vergleich ist wichtiger als der Hauptfall: Ein
    /// Vergleich auf der gerundeten ANZEIGE hielte 5,5667 und 5,57 für denselben Wert
    /// und verschwiege genau die Abweichung, um die es geht.</para>
    /// </summary>
    public class KwkgSatzHerkunftTests : IDisposable
    {
        /// <summary>Deutsche Ressourcentexte gegen <c>Contains</c> — ohne Pinnung wäre
        /// der Fall auf dem Windows-Läufer (en-US) rot.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        // =================================================================
        //  U26 — das Format steht an EINER Stelle
        // =================================================================

        /// <summary>Vier Nachkommastellen, dieselben wie im Zahlenfeld des Dialogs.</summary>
        [Fact]
        public void Der_Satz_traegt_vier_Nachkommastellen()
        {
            Assert.Equal(4, KwkgSatzHerkunft.NACHKOMMASTELLEN);
            Assert.Equal("5,5667", KwkgSatzHerkunft.Satz(1670.0 / 300.0, DE));
            Assert.Equal("5,5667 ct/kWh", KwkgSatzHerkunft.SatzMitEinheit(1670.0 / 300.0, DE));

            // GEGENPROBE: Mit der alten Schreibweise stünde dort 5,57 — die Zahl, die
            // das Feld gerade NICHT führt.
            Assert.NotEqual("5,57", KwkgSatzHerkunft.Satz(1670.0 / 300.0, DE));
        }

        /// <summary>
        /// Die Herleitung des <see cref="KwkgSatzRechner"/> nennt den MISCHSATZ mit
        /// denselben vier Stellen — sie ist der Text, den Dialog, Bericht und
        /// Nachweisumschlag weitertragen. Die Tranchensätze daneben bleiben bei zwei
        /// Stellen: Sie sind Katalogwerte, keine gerechneten Mischwerte.
        /// </summary>
        [Fact]
        public void Die_Herleitung_des_Mischsatzes_nennt_vier_Stellen()
        {
            KwkgSatzVorschlag v = KwkgSatzRechner.Vorschlag(
                300, 2026, DbWerte.KWKG_ANLAGENART_MODERNISIERT, DbWerte.KWKG_EIGENFALL_KEINER,
                Staffel(), DE);

            Assert.Equal(1670.0 / 300.0, v.SatzEinspeisungCt, 6);
            Assert.Contains("5,5667", v.HerleitungEinspeisung);
            Assert.Contains("50,0 kW × 8,00", v.HerleitungEinspeisung);
            Assert.DoesNotContain("Mischsatz 5,57 ", v.HerleitungEinspeisung);
        }

        // =================================================================
        //  U23 — der Vermerk zur Herkunft
        // =================================================================

        /// <summary>Feld und Vorschlag gleich: „Vorschlag", keine zweite Zahl.</summary>
        [Fact]
        public void Ein_Satz_wie_der_Vorschlag_heisst_Vorschlag()
        {
            string v = KwkgSatzHerkunft.Vermerk(1670.0 / 300.0, 1670.0 / 300.0, DE);

            Assert.Equal("Vorschlag", v);
            Assert.DoesNotContain("eigener Wert", v);
        }

        /// <summary>Feld und Vorschlag verschieden: BEIDE Werte, beide mit Einheit.</summary>
        [Fact]
        public void Ein_abweichender_Satz_nennt_den_eigenen_Wert_und_den_Vorschlag()
        {
            string v = KwkgSatzHerkunft.Vermerk(6.0, 1670.0 / 300.0, DE);

            Assert.Equal("eigener Wert 6,0000 ct/kWh — Vorschlag 5,5667 ct/kWh", v);
        }

        /// <summary>
        /// DIE GEGENPROBE ZUM VERGLEICH: Er läuft auf den vier Stellen des Feldes, nicht
        /// auf einer gerundeten Anzeige. 5,5667 und 5,57 sind auf zwei Stellen dasselbe
        /// und müssen hier verschieden sein; zwei Sätze, die sich erst in der fünften
        /// Stelle unterscheiden, sind dagegen derselbe Wert — das Feld kann sie gar
        /// nicht auseinanderhalten.
        /// </summary>
        [Fact]
        public void Der_Vergleich_laeuft_auf_vier_Stellen()
        {
            Assert.True(KwkgSatzHerkunft.Abweichend(5.57, 1670.0 / 300.0));
            Assert.False(KwkgSatzHerkunft.Abweichend(5.5667, 1670.0 / 300.0));
            Assert.False(KwkgSatzHerkunft.Abweichend(5.566701, 5.566699));
        }

        /// <summary>
        /// Ein gebuchter Stand OHNE Vorschlag (Nachweisumschlag aus der Zeit vor diesem
        /// Auftrag) bekommt keinen Vermerk — nicht „Vorschlag 0,0000".
        /// </summary>
        [Fact]
        public void Ohne_bekannten_Vorschlag_bleibt_der_Vermerk_leer()
        {
            Assert.Equal("", KwkgSatzHerkunft.Vermerk(5.5667, null, DE));
            Assert.Equal("5,5667 ct/kWh", KwkgSatzHerkunft.SatzUndHerkunft(5.5667, null, DE));
        }

        // =================================================================
        //  U23 in der Erlösrubrik
        // =================================================================

        /// <summary>
        /// Unter jeder der beiden Geldzeilen des KWK-Zuschlags steht der Satz, mit dem
        /// sie gerechnet wurde, und seine Herkunft. Abweichender Einspeisesatz, dem
        /// Vorschlag gleicher Eigenstromsatz — beide Fälle in EINEM Lauf.
        /// </summary>
        [Fact]
        public void Die_Erloesrubrik_nennt_Satz_und_Herkunft_je_Geldzeile()
        {
            WirtschaftlichkeitErgebnis e = EinModul(6.0, 1670.0 / 300.0,
                                                    835.0 / 300.0, 835.0 / 300.0);
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Equal("eigener Wert 6,0000 ct/kWh — Vorschlag 5,5667 ct/kWh",
                         Text(zeilen, "ERL_A1_SATZ", e));
            Assert.Equal("2,7833 ct/kWh · Vorschlag", Text(zeilen, "ERL_A2_SATZ", e));
        }

        /// <summary>
        /// Die beiden Satzzeilen sind UNTERZEILEN und stehen nie in der Summe: Sie
        /// tragen Text, keinen Wert — Excel bekommt in ihrer Wertspalte nichts, und die
        /// Summandenbildung kann sie gar nicht greifen.
        /// </summary>
        [Fact]
        public void Die_Satzzeilen_tragen_Text_und_keinen_Summanden()
        {
            List<WirtZeile> zeilen = Rubrik(EinModul(6.0, 1670.0 / 300.0, 0, 0));

            foreach (string schluessel in new[] { "ERL_A1_SATZ", "ERL_A2_SATZ" })
            {
                WirtZeile z = Zeile(zeilen, schluessel);
                Assert.NotNull(z);
                Assert.True(z.IstText);
                Assert.Equal(1, z.Einzug);
                Assert.False(z.IstSumme);
                Assert.Null(z.ExcelWert(EinModul(6.0, 1670.0 / 300.0, 0, 0)));
            }
        }

        /// <summary>
        /// OHNE Modulnachweis (ein Lauf ganz ohne KWK-Anlage) entsteht die Zeile gar
        /// nicht — dieselbe Regel wie für die beiden Geldzeilen darüber.
        /// </summary>
        [Fact]
        public void Ohne_Modulnachweis_entfaellt_die_Satzzeile()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                Anzeige = "Variante",
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                Kapitalwert = 1
            };

            Assert.Null(Zeile(Rubrik(e), "ERL_A1_SATZ"));
            Assert.Null(Zeile(Rubrik(e), "ERL_A2_SATZ"));
        }

        /// <summary>Mehrere Module: je Modul ein Abschnitt mit seinem Bezeichner.</summary>
        [Fact]
        public void Mehrere_Module_stehen_je_mit_ihrem_Bezeichner()
        {
            WirtschaftlichkeitErgebnis e = EinModul(6.0, 1670.0 / 300.0, 0, 0);
            e.KwkgModule.Add(new KwkgModulNachweis
            {
                Bezeichner = "BHKW klein",
                EinspeisungMWh = 100,
                SatzEinspeisungCt = 8.0,
                VorschlagEinspeisungCt = 8.0
            });

            string t = Text(Rubrik(e), "ERL_A1_SATZ", e);
            Assert.Contains("BHKW gross: eigener Wert 6,0000 ct/kWh", t);
            Assert.Contains("BHKW klein: 8,0000 ct/kWh · Vorschlag", t);
        }

        // =================================================================
        //  U26 — die Ausgabewege holen das Format, sie schreiben es nicht auf
        // =================================================================

        /// <summary>
        /// <b>Die Wache über das eine Format.</b> Word und Excel schrieben ihre beiden
        /// Satzspalten mit einer eigenen Stellenzahl (<c>k.F(…, 2)</c> bzw.
        /// <c>"#,##0.00"</c>) — zwei Orte, an denen dieselbe Groesse anders aussehen
        /// konnte als im Feld. Der Fall haelt die QUELLEN: Beide Modultafeln nennen
        /// <see cref="KwkgSatzHerkunft"/>, und keine von beiden traegt neben einem
        /// Satzfeld noch ein eigenes Zweistellenformat.
        ///
        /// <para>Gemessen wird die Quelle und nicht das gerenderte Dokument: Ein
        /// vollstaendiger Word- oder Excel-Lauf braucht Vorlagen, Grafiken und alle
        /// Blaetter und sagte ueber diese eine Naht nichts Zusaetzliches (dasselbe
        /// Vorgehen wie in <c>ErgebnisNachweisPersistenzTests</c>).</para>
        /// </summary>
        [Fact]
        public void Word_und_Excel_nehmen_die_Stellenzahl_aus_dem_Kern()
        {
            string wurzel = Wurzel();
            var befunde = new List<string>();

            foreach (string rel in new[]
                     {
                         // BV-E5: die Word-Tafel der KWK-Module baut Berichtstabellen.KwkgModule, der Baustein schreibt sie nur.
                         Path.Combine("EPOS.Kern", "Allgemein", "Bericht", "Tabellen",
                                      "Berichtstabellen.Wirtschaft.cs"),
                         Path.Combine("EPOS.Kern", "Allgemein", "Bericht",
                                      "ExcelBerichtGenerator.cs")
                     })
            {
                string pfad = Path.Combine(wurzel, rel);
                Assert.True(File.Exists(pfad), rel + " fehlt.");
                string[] zeilen = File.ReadAllLines(pfad);

                bool nenntKern = false;
                foreach (string zeile in zeilen)
                {
                    if (zeile.Contains("KwkgSatzHerkunft")) nenntKern = true;
                    if (!zeile.Contains("SatzEigenCt") && !zeile.Contains("SatzEinspeisungCt"))
                        continue;
                    if (zeile.Contains(", 2)") || zeile.Contains("#,##0.00\""))
                        befunde.Add(rel + ": " + zeile.Trim());
                }
                if (!nenntKern) befunde.Add(rel + ": nennt KwkgSatzHerkunft nicht");
            }

            Assert.True(befunde.Count == 0,
                "Ein Ausgabeweg schreibt die Stellenzahl des KWK-Satzes selbst:" +
                Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", befunde) + Environment.NewLine +
                "Das Format steht in KwkgSatzHerkunft (NACHKOMMASTELLEN, ZAHLFORMAT, " +
                "EXCELFORMAT) — einmal, nicht je Ausgabeweg.");
        }

        // =================================================================
        //  Vorrichtung
        // =================================================================

        /// <summary>Ein Lauf mit EINEM Modul und frei gesetzten Sätzen.</summary>
        private static WirtschaftlichkeitErgebnis EinModul(double satzEinsp, double vorschlagEinsp,
                                                           double satzEigen, double vorschlagEigen)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                Anzeige = "Variante",
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                KwkgErloesJahr1 = 32022,
                Kapitalwert = 1,
                KwkgModule = new List<KwkgModulNachweis>
                {
                    new KwkgModulNachweis
                    {
                        Bezeichner = "BHKW gross",
                        PelKW = 300,
                        EigenMWh = 1068.2,
                        EinspeisungMWh = 495.0,
                        SatzEinspeisungCt = satzEinsp,
                        VorschlagEinspeisungCt = vorschlagEinsp,
                        SatzEigenCt = satzEigen,
                        VorschlagEigenCt = vorschlagEigen
                    }
                }
            };
        }

        private static List<WirtZeile> Rubrik(WirtschaftlichkeitErgebnis e)
        {
            var menge = new List<WirtschaftlichkeitErgebnis> { e };
            return WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null), menge);
        }

        private static WirtZeile Zeile(List<WirtZeile> zeilen, string schluessel)
        {
            foreach (WirtZeile z in zeilen) if (z.Schluessel == schluessel) return z;
            return null;
        }

        private static string Text(List<WirtZeile> zeilen, string schluessel,
                                   WirtschaftlichkeitErgebnis e)
        {
            WirtZeile z = Zeile(zeilen, schluessel);
            Assert.NotNull(z);
            return z.Text(e);
        }

        /// <summary>Die echte Staffel des § 7 Abs. 1: 8 / 6 / 5 / 4,4 ct/kWh bei den
        /// Grenzen 50 / 100 / 250 / 2000 kW.</summary>
        /// <summary>Der Aufstieg zur Repowurzel — dasselbe Vorgehen wie in
        /// <c>LokalisierungWirtschaftlichkeitWacheTests.Wurzel</c>.</summary>
        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null &&
                   !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;

            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }

        private static Func<string, int, GesetzParameter> Staffel()
        {
            return (schluessel, jahr) =>
            {
                double w = 0;
                if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1) w = 50;
                else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2) w = 100;
                else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3) w = 250;
                else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4) w = 2000;
                else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW) w = 8.0;
                else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW) w = 6.0;
                else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW) w = 5.0;
                else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW) w = 4.4;
                return w > 0 ? new GesetzParameter(1, schluessel, "KWKG", 2020, w, "ct/kWh", "", "") : null;
            };
        }
    }
}
