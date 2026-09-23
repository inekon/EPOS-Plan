using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E8c (E8b‑Q2) — <b>jede Bemessungsart trägt im Bericht ihren Namen, und jede
    /// bemessene Art rechnet in der Formelmappe Menge × Satz.</b>
    ///
    /// <para><b>Der Befund.</b> <c>WirtschaftlichkeitZeilen.BemessungText</c> führte eine
    /// eigene Liste und kannte 4 von 17 Arten; die übrigen standen in der Spalte „Bemessung"
    /// der Betriebskostentabelle als „fester Betrag" — auch neben der Menge-×-Satz-Formel,
    /// die die Formelmappe (Stufe 3) für dieselbe Zeile schreibt. Der Text kommt jetzt aus
    /// dem <c>BemessungKatalog</c>, und ob eine Art bemessen ist, beantwortet
    /// <c>BetriebskostenCtrl.Bemessungsfaktor</c> für Herleitung und Formelmappe gleich.</para>
    ///
    /// <para><b>Je Art EIN Fall:</b> der Text in beiden Sprachen, die Herleitung und die Zeile
    /// der Formelmappe — Formel, Menge, Satz und der Betrag, der derselbe bleibt. Ein Wächter
    /// hält die Fälle gegen die Konstanten <c>DbWerte.BEMESSUNG_*</c>: Eine neue Art ohne Fall
    /// oder ohne Katalogeintrag fällt auf, statt im Bericht wieder „fester Betrag" zu heißen.
    /// Rein: keine Datenbank, keine Oberfläche; die Kultur ist gepinnt, weil die Texte aus den
    /// Ressourcen kommen.</para>
    /// </summary>
    public class BemessungstexteAlleArtenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>Feste Art: keine Herleitung, keine Formel, der erfasste Betrag gilt.</summary>
        private const string FEST = "fest";

        /// <summary>Satz je Einheit: Betrag = Menge × Satz.</summary>
        private const string JE_EINHEIT = "";

        /// <summary>Prozentart: Betrag = Menge × Satz / 100.</summary>
        private const string PROZENT = "/100";

        private const int BHKW = 7;              // Tab_KostenKomponente.ID
        private const int PUFFERSPEICHER = 6;

        /// <summary>Irgendeine Zeile des Betriebskostenblocks — die Formel bezieht sich auf sie.</summary>
        private const int ZEILE = 12;

        /// <summary>Der erfasste Betrag [€/a] — er gilt nur an einer festen Art.</summary>
        private const double ERFASST = 500.0;

        // =====================================================================
        //  Je Art ein Fall
        // =====================================================================

        /// <summary>
        /// Der Name der Art in beiden Sprachen, ihre Herleitung und ihre Zeile in der
        /// Formelmappe. „fester Betrag" heißt nur noch die feste Position; jede der sechzehn
        /// bemessenen Arten bekommt Menge × Satz (Prozentarten geteilt durch 100), und der
        /// Betrag der Zelle ist der des Rechenwegs.
        /// </summary>
        [Theory]
        [InlineData(DbWerte.BEMESSUNG_BETRAG, "fester Betrag", "fixed amount", FEST)]
        [InlineData(DbWerte.BEMESSUNG_JAHRESBETRAG, "fester Jahresbetrag", "fixed annual amount", FEST)]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_INVESTITION, "% der Investition", "% of investment", PROZENT)]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, "% der Erzeugerkosten", "% of generator costs", PROZENT)]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, "% der Endenergiekosten", "% of final energy costs", PROZENT)]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, "% des Endenergiebedarfs", "% of final energy demand", PROZENT)]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, "% der Brennstoffkosten", "% of fuel costs", PROZENT)]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN, "% der Stromkosten", "% of electricity costs", PROZENT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, "je kWh thermisch", "per kWh thermal", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, "je kWh elektrisch", "per kWh electric", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, "je kW Leistung", "per kW capacity", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, "je kW Heizleistung", "per kW heating capacity", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, "je kW elektrisch", "per kW electric", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWP, "je kWp Leistung", "per kWp capacity", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, "je kWh Kapazität", "per kWh capacity", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, "je m² Kollektorfläche", "per m² collector area", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH, "je kWh", "per kWh", JE_EINHEIT)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_H, "je Stunde", "per hour", JE_EINHEIT)]
        public void Jede_Bemessungsart_nennt_sich_und_rechnet_in_der_Formelmappe(
            string art, string deutsch, string englisch, string rechnung)
        {
            // (1) Der Name — in beiden Sprachen; „fester Betrag" nur an der festen Position.
            Assert.Equal(deutsch, WirtschaftlichkeitZeilen.BemessungText(art));
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal(englisch, WirtschaftlichkeitZeilen.BemessungText(art));
            if (rechnung != FEST)
                Assert.NotEqual(WirtschaftlichkeitZeilen.BemessungText(DbWerte.BEMESSUNG_BETRAG),
                                WirtschaftlichkeitZeilen.BemessungText(art, BHKW));

            // (2) Eine Betriebsposition mit Menge und Satz — an einer Prozentart ist die
            // Menge ein Betrag in €. Der Betrag kommt aus dem EINEN Rechenweg.
            double menge = rechnung == PROZENT ? 40000.0 : 1500.0;
            const double satz = 2.5;
            var n = new KostenPositionNachweis
            {
                Bezeichnung = "Probe",
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessung = art,
                Komponente = BHKW,
                Menge = menge,
                Einheitpreis = satz,
                BetragJahr = BetriebskostenCtrl.Betrag(art, ERFASST, menge, satz, false)
            };

            // (3) Die Herleitung: bemessen „Menge Einheit × Satz Einheit", fest leer —
            // auch dann, wenn an der festen Zeile Menge und Satz stehen.
            string herleitung = WirtschaftlichkeitZeilen.Herleitung(n, DE);
            if (rechnung == FEST)
                Assert.Equal("", herleitung);
            else
            {
                Assert.StartsWith(menge.ToString("N2", DE) + " ", herleitung);
                Assert.Contains(" × " + satz.ToString("N3", DE) + " ", herleitung);
            }

            // (4) Die Zeile der Formelmappe (Stufe 3), so wie der Tabellenbericht sie legt:
            // erst der Betrag als Wert, dann Menge, Satz und Formel.
            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            IXLCell betrag = ws.Cell(ZEILE, ExcelFormelmappe.BK_SPALTE_BETRAG);
            betrag.Value = n.BetragJahr;
            var register = new Formelregister();
            bool formel = ExcelFormelmappe.Betriebskostenzeile(ws, ZEILE, n, register);

            if (rechnung == FEST)
            {
                Assert.Null(BetriebskostenCtrl.Bemessungsfaktor(art));
                Assert.False(formel);
                Assert.False(betrag.HasFormula);
                Assert.Equal(ERFASST, betrag.GetDouble(), 9);
                Assert.True(ws.Cell(ZEILE, ExcelFormelmappe.BK_SPALTE_MENGE).IsEmpty());
                Assert.True(ws.Cell(ZEILE, ExcelFormelmappe.BK_SPALTE_SATZ).IsEmpty());
                return;
            }

            double erwartet = rechnung == PROZENT ? menge * satz / 100.0 : menge * satz;
            Assert.Equal((double?)(rechnung == PROZENT ? 0.01 : 1.0), BetriebskostenCtrl.Bemessungsfaktor(art));
            Assert.True(formel, art + ": Die Formelmappe schreibt kein Menge × Satz.");
            Assert.Equal("F" + ZEILE + "*G" + ZEILE + rechnung, betrag.FormulaA1);
            Assert.Equal(menge, ws.Cell(ZEILE, ExcelFormelmappe.BK_SPALTE_MENGE).GetDouble(), 9);
            Assert.Equal(satz, ws.Cell(ZEILE, ExcelFormelmappe.BK_SPALTE_SATZ).GetDouble(), 9);
            Assert.Equal(1, register.Anzahl);
            Assert.Equal(0, register.Abweichungen);

            // Der Betrag bleibt der des Rechenwegs: Die Formel rechnet auf genau ihn.
            Assert.Equal(erwartet, n.BetragJahr, 9);
            wb.RecalculateAllFormulas();
            Assert.Equal(erwartet, betrag.GetDouble(), 9);
        }

        // =====================================================================
        //  Gewerk, leer und unbekannt
        // =====================================================================

        /// <summary>
        /// Der Name folgt dem Gewerk wie im Kostendialog: „je kW Leistung" heißt am BHKW
        /// „je kW elektr. Leistung" und am Pufferspeicher „je Liter" (Anwenderentscheid
        /// 15.09.2026) — passend zur Herleitung „… Ltr. × … €/Ltr.·a" daneben.
        /// </summary>
        [Fact]
        public void Die_Leistungsart_traegt_den_Namen_ihres_Gewerks()
        {
            string art = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG;
            Assert.Equal("je kW Leistung", WirtschaftlichkeitZeilen.BemessungText(art));
            Assert.Equal("je kW elektr. Leistung", WirtschaftlichkeitZeilen.BemessungText(art, BHKW));
            Assert.Equal("je Liter", WirtschaftlichkeitZeilen.BemessungText(art, PUFFERSPEICHER));
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("per litre", WirtschaftlichkeitZeilen.BemessungText(art, PUFFERSPEICHER));

            var n = new KostenPositionNachweis
            {
                Bemessung = art, Komponente = PUFFERSPEICHER, Menge = 1000.0, Einheitpreis = 0.7
            };
            Assert.Equal("1.000,00 Ltr. × 0,700 €/Ltr.·a", WirtschaftlichkeitZeilen.Herleitung(n, DE));
        }

        /// <summary>
        /// Eine leere Spalte und ein unbekannter Steuerwert heißen „fester Betrag" — der
        /// Rechenweg rechnet beide wie BETRAG mit dem erfassten Betrag („nie stillschweigend
        /// 0"), und genau das sagt die Zeile. Herleitung und Formel gibt es für sie nicht.
        /// </summary>
        [Fact]
        public void Leer_und_unbekannt_heissen_fester_Betrag_wie_im_Rechenweg()
        {
            Assert.Equal("fester Betrag", WirtschaftlichkeitZeilen.BemessungText(null));
            Assert.Equal("fester Betrag", WirtschaftlichkeitZeilen.BemessungText(""));
            Assert.Equal("fester Betrag", WirtschaftlichkeitZeilen.BemessungText("GIBT_ES_NICHT", BHKW));

            Assert.Null(BetriebskostenCtrl.Bemessungsfaktor(null));
            Assert.Null(BetriebskostenCtrl.Bemessungsfaktor("GIBT_ES_NICHT"));
            Assert.Equal(ERFASST, BetriebskostenCtrl.Betrag("GIBT_ES_NICHT", ERFASST, 10.0, 2.0, false));

            var n = new KostenPositionNachweis
            {
                Bemessung = "GIBT_ES_NICHT", Komponente = BHKW, Menge = 10.0, Einheitpreis = 2.0,
                BetragJahr = ERFASST
            };
            Assert.Equal("", WirtschaftlichkeitZeilen.Herleitung(n, DE));
        }

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// Jede Konstante <c>DbWerte.BEMESSUNG_*</c> hat genau einen Fall oben und einen
        /// Eintrag im <c>BemessungKatalog</c>. Eine neue Art ohne beides ist rot — sonst
        /// stünde sie im Bericht wieder als „fester Betrag" (Befund E8b‑Q2).
        /// </summary>
        [Fact]
        public void Jede_Bemessungskonstante_hat_ihren_Fall_und_ihren_Katalogeintrag()
        {
            var konstanten = typeof(DbWerte)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) &&
                            f.Name.StartsWith("BEMESSUNG_", StringComparison.Ordinal))
                .Select(f => (string)f.GetRawConstantValue())
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            MethodInfo theorie = typeof(BemessungstexteAlleArtenTests).GetMethod(
                nameof(Jede_Bemessungsart_nennt_sich_und_rechnet_in_der_Formelmappe));
            var faelle = theorie.GetCustomAttributes<InlineDataAttribute>()
                .SelectMany(a => a.GetData(theorie))
                .Select(d => (string)d[0])
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            Assert.NotEmpty(konstanten);
            Assert.Equal(konstanten, faelle);
            foreach (string k in konstanten)
                Assert.True(BemessungKatalog.Finde(k) != null, k + " fehlt im BemessungKatalog.");
        }
    }
}
