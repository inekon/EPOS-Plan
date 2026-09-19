using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// U31 — der STAND DER BETRIEBSSEITE: der Laufstand über dem Raster und die
    /// Gruppe „Endenergie je Komponente" darunter.
    ///
    /// <para><b>Gemessen an <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>.</b> Projekt
    /// 1030 („BHKW-Kaskade") führt den einzigen Lauf mit ZWEI Blockheizkraftwerken:
    /// „BHKW EW M 50 S [K] Erdgas" mit 862,18 MWh und „EC-POWER XRGI 9" mit
    /// 186,09 MWh Brennstoff. Beide sind eigene Anlagenzeilen, und die Endenergie ist
    /// anlagenscharf — deshalb zwei Zeilen und nicht eine Summe. Dazu der Gaskessel
    /// desselben Laufs mit 5.403,1 MWh; der Pufferspeicher hat überhaupt keine
    /// Endenergie und bekommt deshalb keine Zeile (keine stille 0).</para>
    ///
    /// <para><b>#363 (19.09.2026):</b> Der Kessel hatte bis dahin ebenfalls keine
    /// Zeile — nicht, weil er nichts verbraucht, sondern weil der GESPEICHERTE Lauf
    /// dieser Datenbank von vor Befund B-1 stammt und <c>Verbrauch</c> dort leer ist.
    /// Der Auflöser leitet den Einsatz seither aus Wärme und Nutzungsgrad ab
    /// (dieselbe Ableitung wie die Steuerseite), und damit steht der Kessel in der
    /// Gruppe, wo er hingehört.</para>
    ///
    /// <para><b>Nichts wird geschrieben.</b> Beide Auskünfte lesen nur; die
    /// Arbeitskopie steht trotzdem, weil <c>DataRepository</c> ohne sie auf die
    /// Datenbank des Anwenders zeigte.</para>
    ///
    /// <para>Die Kultur ist auf de-DE gepinnt und im <see cref="Dispose"/>
    /// zurückgestellt (iU9‑#167).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenBetriebsstandTests : IDisposable
    {
        private const int PROJEKT_MIT_LAUF = 1030;
        private const int PROJEKT_OHNE_LAUF = 1007;

        private const string ANLAGE_GROSS = "BHKW EW M 50 S [K] Erdgas";
        private const string ANLAGE_KLEIN = "EC-POWER XRGI 9";

        /// <summary>862,18 MWh × 1000 — Brennstoff des großen Moduls im Lauf 212.</summary>
        private const double BEDARF_GROSS = 862180.0;

        /// <summary>186,09 MWh × 1000 — Brennstoff des kleinen Moduls im Lauf 212.</summary>
        private const double BEDARF_KLEIN = 186090.0;

        /// <summary>#363: der Gaskessel desselben Laufs. Nur der Wortanfang: Sein
        /// Bezeichner steht in der Testdatenbank nicht in UTF-8, das Umlautzeichen
        /// käme beim Lesen als Ersatzzeichen an.</summary>
        private const string ANLAGE_KESSEL = "Vitocrossal 200 CM2";

        /// <summary>5.403,1 MWh × 1000 — sein Brennstoff, aus Wärme und
        /// Jahresnutzungsgrad (100 %) des Laufs 212 abgeleitet.</summary>
        private const double BEDARF_KESSEL = 5403100.0;

        private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

        public KostenBetriebsstandTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _kulturVorher;
            CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        /// <summary>Die Zeile zu einer Anlage. Gesucht wird ENTHALTEN, nicht am Ende:
        /// Ein Bezeichner mit Umlaut kommt aus der Testdatenbank mit Ersatzzeichen an
        /// (sie steht nicht durchgängig in UTF-8), und der Vergleich soll daran nicht
        /// scheitern.</summary>
        private static KostenBetriebsstand.Endenergiezeile Zeile(
            List<KostenBetriebsstand.Endenergiezeile> liste, string anlage)
        {
            foreach (KostenBetriebsstand.Endenergiezeile z in liste)
                if (z.Komponente.Contains(anlage, StringComparison.Ordinal)) return z;
            return null;
        }

        // =====================================================================
        // Der Laufstand über dem Raster
        // =====================================================================

        /// <summary>
        /// Der Kern KENNT den Zeitpunkt des gespeicherten Ergebnisses
        /// (<c>Tab_Ergebnis.Zeitstempel</c>) — die Zeile nennt ihn deshalb, statt sich
        /// auf „aus dem gespeicherten Simulationsergebnis" zurückzuziehen.
        /// </summary>
        [Fact]
        public void Der_Laufstand_nennt_den_Zeitpunkt_des_juengsten_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string text = KostenBetriebsstand.Laufstand(PROJEKT_MIT_LAUF);

            Assert.StartsWith("Mengen stammen aus dem Simulationslauf vom ", text);
            Assert.Contains("30.08.2026", text);
            Assert.NotEqual(WindowsFormsApplication1.MyResource.Resource.KDLG_LAUFSTAND_OHNE_DATUM, text);
        }

        /// <summary>Ohne gespeichertes Ergebnis steht dort der GRUND, den auch die
        /// Zeilen ohne Bezugsgröße nennen — ein Text, kein zweiter.</summary>
        [Fact]
        public void Ohne_Lauf_nennt_der_Laufstand_den_bestehenden_Grund()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KDLG_BASIS_GRUND_LAUF,
                         KostenBetriebsstand.Laufstand(PROJEKT_OHNE_LAUF));
        }

        /// <summary>Im Stammkontext gibt es kein Projekt und deshalb keine Zeile.</summary>
        [Fact]
        public void Ohne_Projekt_bleibt_der_Laufstand_leer()
        {
            Assert.Equal("", KostenBetriebsstand.Laufstand(0));
        }

        // =====================================================================
        // Endenergie je Komponente
        // =====================================================================

        /// <summary>
        /// Je Anlage mit Endenergie eine Zeile — die zwei BHKW-Module und der
        /// Gaskessel; der Pufferspeicher hat keine Endenergie und bekommt keine.
        /// </summary>
        [Fact]
        public void Jede_Anlage_mit_Endenergie_bekommt_eine_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<KostenBetriebsstand.Endenergiezeile> zeilen =
                KostenBetriebsstand.Endenergie(PROJEKT_MIT_LAUF);

            Assert.Equal(3, zeilen.Count);
            Assert.NotNull(Zeile(zeilen, ANLAGE_GROSS));
            Assert.NotNull(Zeile(zeilen, ANLAGE_KLEIN));
            Assert.NotNull(Zeile(zeilen, ANLAGE_KESSEL));
        }

        /// <summary>
        /// #363: Der Kessel steht mit dem Brennstoff DIESES Laufs in der Gruppe —
        /// abgeleitet aus Wärme und Nutzungsgrad, weil der gespeicherte Lauf die
        /// Spalte <c>Verbrauch</c> nicht führt. Ohne die Ableitung stünde hier gar
        /// nichts, und die Betriebszeilen des Kessels blieben ohne Bezugsgröße.
        /// </summary>
        [Fact]
        public void Der_Kessel_steht_mit_dem_Brennstoff_des_Laufs_in_der_Gruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenBetriebsstand.Endenergiezeile z =
                Zeile(KostenBetriebsstand.Endenergie(PROJEKT_MIT_LAUF), ANLAGE_KESSEL);

            Assert.NotNull(z);
            Assert.Equal(BEDARF_KESSEL, z.BedarfKwh, 6);
            Assert.True(z.KostenEuro.HasValue);
        }

        /// <summary>
        /// Die Menge ist die des Laufs, mal 1000 (der Lauf führt MWh) — und sie steht
        /// gesetzt in der Spalte, mit Tausenderpunkten und ohne Nachkommastellen.
        /// </summary>
        [Fact]
        public void Die_Menge_ist_der_Brennstoff_des_Laufs_in_kWh()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<KostenBetriebsstand.Endenergiezeile> zeilen =
                KostenBetriebsstand.Endenergie(PROJEKT_MIT_LAUF);

            KostenBetriebsstand.Endenergiezeile gross = Zeile(zeilen, ANLAGE_GROSS);
            KostenBetriebsstand.Endenergiezeile klein = Zeile(zeilen, ANLAGE_KLEIN);

            Assert.Equal(BEDARF_GROSS, gross.BedarfKwh, 2);
            Assert.Equal(BEDARF_KLEIN, klein.BedarfKwh, 2);
            Assert.Equal("862.180 kWh", gross.BedarfText);
            Assert.Equal("186.090 kWh", klein.BedarfText);
        }

        /// <summary>
        /// EINE Wahrheit: Die Gruppe rechnet nicht selbst, sie zeigt, was
        /// <see cref="EndenergieAufloeser"/> zur selben Anlage sagt — Menge, Kosten
        /// und Herkunft.
        /// </summary>
        [Fact]
        public void Die_Gruppe_zeigt_genau_das_was_der_Aufloeser_sagt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<KostenBetriebsstand.Endenergiezeile> zeilen =
                KostenBetriebsstand.Endenergie(PROJEKT_MIT_LAUF);
            KostenBetriebsstand.Endenergiezeile gross = Zeile(zeilen, ANLAGE_GROSS);

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(PROJEKT_MIT_LAUF);
            EndenergieAufloeser.Groesse g = a.FuerPosition(
                BetriebskostenCtrl.KOMPONENTE_BHKW, AnlagenId(ANLAGE_GROSS));

            Assert.NotNull(g);
            Assert.Equal(g.BedarfKwh, gross.BedarfKwh, 6);
            Assert.Equal(g.KostenEuro.HasValue, gross.KostenEuro.HasValue);
            if (g.KostenEuro.HasValue)
                Assert.Equal(g.KostenEuro.Value, gross.KostenEuro.Value, 6);
            Assert.Equal(g.Basis, gross.Basis);
        }

        /// <summary>
        /// KEINE STILLE 0: Ohne Arbeitspreis steht in der Kostenspalte ein
        /// Gedankenstrich, mit Preis der gesetzte Betrag je Jahr.
        /// </summary>
        [Fact]
        public void Ohne_Arbeitspreis_steht_ein_Gedankenstrich_statt_einer_Null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (KostenBetriebsstand.Endenergiezeile z in
                     KostenBetriebsstand.Endenergie(PROJEKT_MIT_LAUF))
            {
                if (z.KostenEuro.HasValue)
                {
                    Assert.EndsWith("€/a", z.KostenText);
                    Assert.Equal(z.KostenEuro.Value.ToString("#,##0.00", CultureInfo.CurrentCulture),
                                 z.KostenText.Replace(" €/a", ""));
                }
                else
                {
                    Assert.Equal("—", z.KostenText);
                }
            }
        }

        /// <summary>Ohne Projekt gibt es nichts zu zeigen — eine leere Liste, kein Fehler.</summary>
        [Fact]
        public void Ohne_Projekt_bleibt_die_Gruppe_leer()
        {
            Assert.Empty(KostenBetriebsstand.Endenergie(0));
        }

        /// <summary><c>Tab_Energieanlagen.ID</c> zu einem Bezeichner des Projekts 1030.</summary>
        private static int AnlagenId(string bezeichner)
        {
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", PROJEKT_MIT_LAUF), new DbParam("@b", bezeichner));
            return dt != null && dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["ID"]) : 0;
        }
    }
}
