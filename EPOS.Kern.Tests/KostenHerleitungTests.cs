using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// U28/U29 — die HERLEITUNG einer Kostenzeile und der dreiteilige Summenfuß.
    ///
    /// <para><b>Was hier bewiesen wird.</b> Werkzeugtipp und Herleitungszeile
    /// entstehen aus EINER Methode (<see cref="KostenHerleitung.Bilde"/>): dieselbe
    /// Bezugsgröße, dieselbe Einheit, dieselbe Runde. Die Zahlenprobe ist die des
    /// Mockups <c>Dialog_Formel_Zahlenprobe.html</c> (Abschnitte 1 und 2, 300-kW-
    /// Blockheizkraftwerk). Gerechnet wird nichts — die Zahlen liegen im Dialog
    /// bereits vor.</para>
    ///
    /// <para>Die Kultur ist auf de-DE gepinnt und im <see cref="Dispose"/>
    /// zurückgestellt (iU9‑#167): <c>CultureInfo.CurrentCulture</c> ist
    /// threadgebunden, und xunit gibt denselben Pool-Thread weiter.</para>
    /// </summary>
    public class KostenHerleitungTests : IDisposable
    {
        private const int BHKW = 7;             // Tab_KostenKomponente.ID
        private const int PUFFERSPEICHER = 6;

        private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

        public KostenHerleitungTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _kulturVorher;
            CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        private static KostenVorlagenPosition Position(string bemessung, double? satz,
                                                       double? betrag = null, bool erloes = false)
        {
            return new KostenVorlagenPosition
            {
                Bezeichnung = "Probe",
                Bemessung = bemessung,
                Satz = satz,
                BetragNetto = betrag,
                IstErloes = erloes
            };
        }

        private static KostenProjektPositionenCtrl.Zeile Projektzeile(
            double? basis, int runde, string herkunft, string grund = "")
        {
            return new KostenProjektPositionenCtrl.Zeile
            {
                Basis = basis,
                Runde = runde,
                BasisHerkunft = herkunft,
                BasisGrund = grund
            };
        }

        // =====================================================================
        // U28 — die Herleitungszeile, Zahlenprobe des Mockups
        // =====================================================================

        /// <summary>Runde 1: der Satz je kW elektrischer Leistung am 300-kW-Modul.
        /// Die Größe heißt beim Namen — <c>P_el</c>, die Spalte, aus der die Kaskade
        /// sie liest (Anwenderentscheid 15.09.2026).</summary>
        [Fact]
        public void Eine_Zeile_je_kW_nennt_Baugroesse_Herkunft_und_Runde_1()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 653.60, 196080.0), BHKW,
                Projektzeile(300.0, 1, KostenHerleitung.HERKUNFT_ANLAGE), true);

            Assert.Equal("300,00 kW", a.BasisText);
            Assert.Equal("P_el der Anlage", a.HerkunftText);
            Assert.Equal("× 300,00 kW · P_el der Anlage · Runde 1", a.Zeile);
            Assert.False(a.Absolut);
            Assert.False(a.OhneBasis);
        }

        /// <summary>Runde 2: „% der Erzeugerkosten" bemisst sich an den
        /// Hauptpositionen derselben Komponente — 196.080,00 € des Moduls.</summary>
        [Fact]
        public void Eine_Prozentzeile_der_Runde_2_nennt_die_Hauptpositionen()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, 5.0, 9804.0), BHKW,
                Projektzeile(196080.0, 2, KostenHerleitung.HERKUNFT_HAUPT), true);

            Assert.Equal("196.080,00 €", a.BasisText);
            Assert.Equal("× 196.080,00 € · Hauptpositionen · Runde 2", a.Zeile);
        }

        /// <summary>Runde 3: „% der Investition" bemisst sich an der eingefrorenen
        /// Summe der Stufe, die getragen hat — hier der Anlage.</summary>
        [Fact]
        public void Eine_Prozentzeile_der_Runde_3_nennt_die_Stufe()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0, 21888.40), BHKW,
                Projektzeile(218884.0, 3, KostenHerleitung.HERKUNFT_STUFE_ANLAGE), true);

            Assert.Equal("× 218.884,00 € · Stufe Anlage · Runde 3", a.Zeile);
        }

        /// <summary>Die Betriebsseite kennt keine Kaskade: Ihre Menge kommt aus dem
        /// Lauf, und die Zeile nennt deshalb keine Runde.</summary>
        [Fact]
        public void Eine_Betriebszeile_je_kWh_nennt_den_Lauf_und_keine_Runde()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.02, 33000.0), BHKW,
                Projektzeile(1650000.0, 0, KostenHerleitung.HERKUNFT_LAUF), true);

            Assert.Equal("1.650.000,00 kWh", a.BasisText);
            Assert.Equal("× 1.650.000,00 kWh · Lauf", a.Zeile);
        }

        /// <summary>Eine absolute Bemessung braucht keine Bezugsgröße — die Zeile
        /// sagt genau das, und das Kettensymbol bleibt.</summary>
        [Fact]
        public void Eine_absolute_Zeile_traegt_Satz_gleich_Betrag()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_BETRAG, 13000.0, 13000.0), BHKW,
                Projektzeile(null, 1, ""), true);

            Assert.True(a.Absolut);
            Assert.True(a.Kette);
            Assert.Equal("Satz = Betrag", a.Zeile);
        }

        /// <summary>Im Stammkontext gibt es keine Bezugsgröße und deshalb auch
        /// nichts herzuleiten — auch nicht an einer absoluten Zeile.</summary>
        [Fact]
        public void Im_Stammkontext_bleibt_die_Herleitungszeile_weg()
        {
            KostenHerleitung.Angabe ohneProjekt = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 653.60), BHKW, null, false);
            KostenHerleitung.Angabe absolut = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_BETRAG, 13000.0), BHKW, null, false);

            Assert.Equal("", ohneProjekt.Zeile);
            Assert.Equal("", absolut.Zeile);
        }

        /// <summary>Ohne Bezugsgröße bleibt es beim ⚠ der Zeile und beim Grund unter
        /// dem Raster (Anwenderbefund 14.09.2026) — eine zweite Zeile sagte dasselbe
        /// noch einmal.</summary>
        [Fact]
        public void Ohne_Bezugsgroesse_entsteht_keine_Herleitungszeile_sondern_ein_Grund()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KWP, 1200.0, 0.0), BHKW,
                Projektzeile(null, 1, "", WirtschaftlichkeitCtrl.BASISGRUND_GEWERK), true);

            Assert.Equal("", a.Zeile);
            Assert.True(a.OhneBasis);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.KDLG_BASIS_GRUND_GEWERK, a.Kurztext);
        }

        // =====================================================================
        // U28 — Werkzeugtipp und Zeile aus EINER Quelle
        // =====================================================================

        /// <summary>Der Werkzeugtipp des Betragsfeldes bleibt wortgleich zum
        /// Bestand — er kommt jetzt nur aus demselben Aufruf wie die Zeile.</summary>
        [Fact]
        public void Der_Werkzeugtipp_bleibt_wortgleich_und_nennt_dieselbe_Bezugsgroesse()
        {
            KostenHerleitung.Angabe menge = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 653.60, 196080.0), BHKW,
                Projektzeile(300.0, 1, KostenHerleitung.HERKUNFT_ANLAGE), true);
            KostenHerleitung.Angabe prozent = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, 5.0, 9804.0), BHKW,
                Projektzeile(196080.0, 2, KostenHerleitung.HERKUNFT_HAUPT), true);

            Assert.Equal(string.Format(WindowsFormsApplication1.MyResource.Resource.KDLG_TT_BETRAG_BASIS_MENGE,
                                       "653,60 €/kW", "300,00 kW"), menge.Kurztext);
            Assert.Equal(string.Format(WindowsFormsApplication1.MyResource.Resource.KDLG_TT_BETRAG_BASIS_PROZENT,
                                       "5,00 %", "196.080,00 €"), prozent.Kurztext);
            Assert.Contains(menge.BasisText, menge.Kurztext);
            Assert.Contains(menge.BasisText, menge.Zeile);
        }

        /// <summary>Die Einheit folgt dem GEWERK: Am Pufferspeicher bemisst sich
        /// „je kW Leistung" am Volumen in Litern (Anwenderentscheid 15.09.2026), und
        /// die Herleitungszeile nennt genau diese Größe.</summary>
        [Fact]
        public void Am_Pufferspeicher_bemisst_dieselbe_Art_das_Volumen()
        {
            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 1.50, 3000.0), PUFFERSPEICHER,
                Projektzeile(2000.0, 1, KostenHerleitung.HERKUNFT_ANLAGE), true);

            Assert.Equal("2.000,00 Ltr.", a.BasisText);
            Assert.Equal("Gesamtvolumen der Anlage", a.HerkunftText);
        }

        // =====================================================================
        // U29 — die drei Beträge
        // =====================================================================

        /// <summary>Die Zahlenprobe des Mockups: 240.772,40 € brutto, 6.000,00 €
        /// Zuschuss, 234.772,40 € I₀ — und I₀ ist genau die bestehende
        /// Nettosumme.</summary>
        [Fact]
        public void Der_Fuss_trennt_Bruttoinvestition_Zuschuss_und_I0()
        {
            var zeilen = new List<KostenVorlagenPosition>
            {
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 653.60, 196080.0),
                Position(DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, 5.0, 9804.0),
                Position(DbWerte.BEMESSUNG_BETRAG, 13000.0, 13000.0),
                Position(DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0, 21888.40),
                Position(DbWerte.BEMESSUNG_BETRAG, 6000.0, 6000.0, erloes: true),
            };

            KostenSummenCtrl.Investitionsfuss f = KostenSummenCtrl.Fuss(zeilen);

            Assert.Equal(240772.40, f.Brutto, 2);
            Assert.Equal(6000.00, f.Zuschuss, 2);
            Assert.Equal(234772.40, f.Investition, 2);
            Assert.True(f.MitZuschuss);
            Assert.Equal("Investition brutto 240.772,40 € · Zuschuss 6.000,00 € · I₀ 234.772,40 €",
                         KostenSummenCtrl.FussText(f));
        }

        /// <summary>Gegenprobe: Ohne Zuschusszeile ist netto = I₀ — dann entfällt die
        /// dritte Zeile, weil sie nichts Neues sagte.</summary>
        [Fact]
        public void Ohne_Zuschusszeile_entfaellt_die_dritte_Zeile()
        {
            var zeilen = new List<KostenVorlagenPosition>
            {
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 653.60, 196080.0),
                Position(DbWerte.BEMESSUNG_BETRAG, 13000.0, 13000.0),
            };

            KostenSummenCtrl.Investitionsfuss f = KostenSummenCtrl.Fuss(zeilen);

            Assert.False(f.MitZuschuss);
            Assert.Equal(f.Brutto, f.Investition, 6);
            Assert.Equal("", KostenSummenCtrl.FussText(f));
        }

        /// <summary>Ein versehentlich NEGATIV erfasster Zuschuss erhöht die
        /// Investition nicht — dieselbe Regel wie in
        /// <c>WirtschaftlichkeitCtrl.LiesInvestitionen</c> (K5).</summary>
        [Fact]
        public void Ein_negativ_erfasster_Zuschuss_mindert_trotzdem()
        {
            var zeilen = new List<KostenVorlagenPosition>
            {
                Position(DbWerte.BEMESSUNG_BETRAG, 100000.0, 100000.0),
                Position(DbWerte.BEMESSUNG_BETRAG, -6000.0, -6000.0, erloes: true),
            };

            KostenSummenCtrl.Investitionsfuss f = KostenSummenCtrl.Fuss(zeilen);

            Assert.Equal(6000.0, f.Zuschuss, 6);
            Assert.Equal(94000.0, f.Investition, 6);
        }

        /// <summary>Eine nicht gepflegte Zeile ist keine 0: Sie zählt nirgends mit.</summary>
        [Fact]
        public void Eine_Zeile_ohne_Betrag_zaehlt_nicht_mit()
        {
            var zeilen = new List<KostenVorlagenPosition>
            {
                Position(DbWerte.BEMESSUNG_BETRAG, 100000.0, 100000.0),
                Position(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 653.60),
            };

            Assert.Equal(100000.0, KostenSummenCtrl.Fuss(zeilen).Brutto, 6);
        }
    }
}
