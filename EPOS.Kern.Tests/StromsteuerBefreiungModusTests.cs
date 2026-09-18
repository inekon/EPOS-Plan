using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE B6 — § 9 Abs. 1 Nr. 3 StromStG als AUSWEIS statt als Erlös</b>
    /// (Befund B-1 des Wirtschaftlichkeitskonzepts, Entscheidung K3).
    ///
    /// <para><b>Die Lage vorher.</b> Die Befreiung wurde zweimal geführt: als Ausweiswert
    /// <c>StromsteuerBefreiungJahr1</c> UND als jahresscharfe Erlösreihe
    /// (<c>KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG</c>) im Kapitalwert. Die
    /// Vorschrift ist aber keine Rückerstattung: Auf selbst erzeugten und selbst
    /// verbrauchten Strom entsteht gar keine Stromsteuer — der Vorteil steckt bereits in
    /// der kleineren Bezugsrechnung.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Vier Dinge:
    /// <list type="number">
    ///   <item><description>Die Spalte des Schemaschritts 88 steht, und NULL bedeutet
    ///     AUSWEIS.</description></item>
    ///   <item><description>Der Parametersatz schreibt und liest beide Modi.</description></item>
    ///   <item><description>AUSWEIS rechnet und ZEIGT den Betrag, hängt aber keine Reihe
    ///     an; ERLOES hängt sie an und hebt den Kapitalwert um ihren Barwert.</description></item>
    ///   <item><description>ERLOES bekommt die Kohärenzzeile zur Doppelzählung,
    ///     AUSWEIS nicht.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist; die
    /// Kultur ist gepinnt, weil die Kohärenztexte aus den Satellitenressourcen kommen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromsteuerBefreiungModusTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>
        /// „Referenz BHKW-Kaskade" — <b>das einzige Projekt der Testdatenbank, an dem
        /// § 9 Abs. 1 Nr. 3 überhaupt einen Betrag ergibt</b>: zwei Gasmodule von 50 kW
        /// und 9 kW, beide unter der Leistungsgrenze von 2.000 kW und unter dem
        /// CO₂-Grenzwert von 270 g je kWh Energieertrag.
        ///
        /// <para><b>Warum nicht das Projekt des Befundes.</b> Der Befund B-1 wurde an
        /// „Wöhler – Test2" (1024) gemessen. Dessen BHKW verbrennt ÖL und liegt damit
        /// über dem CO₂-Grenzwert — der Rechner verweigert die Befreiung mit
        /// Begründung, ganz unabhängig vom Modus. Mit 1024 misst der Prüffall
        /// die Abwesenheit einer Zahl, mit 1030 den Unterschied zwischen zwei
        /// Modi.</para>
        /// </summary>
        private const int PROJEKT = 1030;

        /// <summary>Strombedarf des gespeicherten Laufs von <see cref="PROJEKT"/>
        /// [MWh/a] — die Jahressumme, aus der die flache Bedarfsreihe entsteht.</summary>
        private const double BEDARF_MWH = 4790.09;

        /// <summary>BHKW-Stromerzeugung desselben Laufs [MWh/a]. Sie liegt in jeder
        /// Stunde unter dem Bedarf und ist deshalb vollständig Eigenverbrauch — die
        /// Bemessungsgrundlage des § 9 Abs. 1 Nr. 3.</summary>
        private const double BHKW_MWH = 432.3;

        // =================================================================
        // 1 — Schemaschritt 88
        // =================================================================

        /// <summary>
        /// Der Zielstand trägt den Schritt 88, und die eine Spalte hängt an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>. Die Liste ist die EINE Quelle, aus der
        /// sich Migration, Werkzeug und Testdatenbank bedienen.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_88()
        {
            Assert.True(SchemaStand.Zielversion >= 88,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 88.");
            Assert.Single(SchemaKatalog.Schritt88_StromsteuerModus);
            Assert.All(SchemaKatalog.Schritt88_StromsteuerModus,
                       s => Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Tabelle));
            Assert.Equal(SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS,
                         SchemaKatalog.Schritt88_StromsteuerModus[0].Name);
        }

        /// <summary>Die Testdatenbank führt die Spalte, und sie ist im ganzen Bestand
        /// leer — der Schritt schreibt keinen Wert.</summary>
        [Fact]
        public void Die_Testdatenbank_fuehrt_die_leere_Spalte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.SpalteVorhanden(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT,
                                                       SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS));
            object gepflegt = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT +
                " WHERE [" + SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS + "] IS NOT NULL");
            Assert.Equal(0, Convert.ToInt32(gepflegt));
        }

        // =================================================================
        // 2 — Der Parametersatz
        // =================================================================

        /// <summary>
        /// DIE VORGABE. Eine leere Zelle heißt AUSWEIS — nicht „nicht gepflegt" und
        /// schon gar nicht ERLOES. Damit verhält sich eine nicht migrierte Datenbank wie
        /// eine migrierte.
        /// </summary>
        [Fact]
        public void Ohne_gepflegten_Wert_gilt_Ausweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);

            Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS, p.StromsteuerBefreiungModus);
            Assert.False(p.StromsteuerBefreiungAlsErloes);
        }

        /// <summary>Beide Modi überstehen Speichern und Laden — der Schreibweg des
        /// Dialogs hängt daran.</summary>
        [Theory]
        [InlineData(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES, true)]
        [InlineData(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS, false)]
        public void Der_Modus_ueberlebt_Speichern_und_Laden(string modus, bool alsErloes)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            p.StromsteuerBefreiungModus = modus;
            Assert.True(ctrl.SpeichereParameter(p));

            WirtschaftlichkeitParameter zurueck = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Equal(modus, zurueck.StromsteuerBefreiungModus);
            Assert.Equal(alsErloes, zurueck.StromsteuerBefreiungAlsErloes);
        }

        /// <summary>Ein unbekannter Bestandswert ist kein ERLOES: Die Leseseite kennt
        /// genau einen ausdrücklichen zweiten Fall, alles andere ist AUSWEIS.</summary>
        [Fact]
        public void Ein_unbekannter_Wert_gilt_als_Ausweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT +
                " SET [" + SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS + "] = ? WHERE ID_Projekt = ?",
                new DbParam("@m", "IRGENDWAS"), new DbParam("@p", PROJEKT));

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS, p.StromsteuerBefreiungModus);
        }

        // =================================================================
        // 3 — Die Rechenwirkung
        // =================================================================

        /// <summary>
        /// DER BEFUND SELBST: Im Modus AUSWEIS steht der Betrag
        /// als Ausweis da, geht aber NICHT in den Kapitalwert. Im Modus ERLOES liegt der
        /// Kapitalwert um den Barwert der Reihe höher — derselbe Ausweisbetrag, zwei
        /// verschiedene Kapitalwerte.
        /// </summary>
        [Fact]
        public void Ausweis_zeigt_den_Betrag_und_haelt_ihn_aus_dem_Kapitalwert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis ausweis = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS);
            WirtschaftlichkeitErgebnis erloes = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES);

            // Der AUSWEISBETRAG ist in beiden Modi derselbe - gerechnet wird immer.
            Assert.True(ausweis.StromsteuerBefreiungJahr1 > 0,
                        "Projekt " + PROJEKT + " bucht keine Befreiung - der Prüffall misst nichts.");
            Assert.Equal(erloes.StromsteuerBefreiungJahr1, ausweis.StromsteuerBefreiungJahr1, 6);

            // Der Merker wandert mit ins Ergebnis.
            Assert.False(ausweis.StromsteuerBefreiungAlsErloes);
            Assert.True(erloes.StromsteuerBefreiungAlsErloes);

            // Und NUR der Modus ERLOES hebt den Kapitalwert.
            Assert.True(ausweis.Kapitalwert.HasValue && erloes.Kapitalwert.HasValue,
                        "Ohne Kapitalwert misst der Prüffall nichts.");
            Assert.True(erloes.Kapitalwert.Value > ausweis.Kapitalwert.Value,
                        "ERLOES muss den Kapitalwert um den Barwert der Reihe heben.");
        }

        /// <summary>
        /// Die Höhe der Differenz ist kein Zufallswert: Sie ist genau der Barwert der
        /// flachen Reihe über den Betrachtungszeitraum — Rentenbarwertfaktor × Jahr-1-Betrag.
        /// Damit ist bewiesen, dass AUSWEIS die GANZE Reihe herausnimmt und nicht nur
        /// ihr erstes Jahr.
        /// </summary>
        [Fact]
        public void Die_Differenz_ist_der_Barwert_der_Reihe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            WirtschaftlichkeitErgebnis ausweis = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS);
            WirtschaftlichkeitErgebnis erloes = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES);
            if (!ausweis.Kapitalwert.HasValue || !erloes.Kapitalwert.HasValue) return;

            double i = p.Zinssatz / 100.0;
            int T = p.Betrachtungszeitraum;
            double rbf = 0;
            for (int t = 1; t <= T; t++) rbf += 1.0 / Math.Pow(1.0 + i, t);

            double erwartet = ausweis.StromsteuerBefreiungJahr1 * rbf;
            double gemessen = erloes.Kapitalwert.Value - ausweis.Kapitalwert.Value;

            // Toleranz wie die Referenzsuite: relativ 1e-4 auf Beträge ab 1 €.
            Assert.True(Math.Abs(gemessen - erwartet) <= Math.Max(0.01, Math.Abs(erwartet) * 1e-4),
                        "Barwert erwartet " + erwartet.ToString("N2") +
                        " €, gemessen " + gemessen.ToString("N2") + " €.");
        }

        // =================================================================
        // 4 — Die Kohärenzzeile zur Doppelzählung
        // =================================================================

        /// <summary>
        /// ERLOES bekommt die Warnung zur Doppelzählung, AUSWEIS nicht. Geprüft wird über
        /// den sprachneutralen Betrag und die Schwere, nicht über Textraten: Die Zeile
        /// trägt den gebuchten Jahresbetrag.
        /// </summary>
        [Fact]
        public void Nur_der_Modus_Erloes_warnt_vor_der_Doppelzaehlung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis ausweis = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS);
            WirtschaftlichkeitErgebnis erloes = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES);

            Assert.Contains(erloes.KohaerenzHinweise,
                h => h.Schwere == KohaerenzSchwere.WARNUNG &&
                     h.Text.Contains("Doppelzählung", StringComparison.Ordinal));
            Assert.DoesNotContain(ausweis.KohaerenzHinweise,
                h => h.Text.Contains("Doppelzählung", StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Zeile steht auch in der englischen Oberfläche — beide Sprachen führen den
        /// Schlüssel. Ohne den Fall bliebe unbemerkt, dass ein neuer Text nur deutsch
        /// nachgetragen wurde.
        /// </summary>
        [Fact]
        public void Die_Doppelzaehlungszeile_steht_in_beiden_Sprachen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            using (new Kulturvorrichtung("en-US"))
            {
                WirtschaftlichkeitErgebnis erloes = Rechne(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES);
                Assert.Contains(erloes.KohaerenzHinweise,
                    h => h.Schwere == KohaerenzSchwere.WARNUNG &&
                         h.Text.Contains("double count", StringComparison.Ordinal));
            }
        }

        // =================================================================
        // Prüfstand
        // =================================================================

        /// <summary>
        /// Rechnet Projekt <see cref="PROJEKT"/> als Stammprojekt mit dem gewünschten
        /// Modus und liefert das Ergebnis des Szenarios ERWARTET.
        ///
        /// <para>Der Weg ist der der Anwendung: Parametersatz laden und schreiben,
        /// Kosten und Emissionen rechnen, <c>WirtschaftlichkeitCtrl.Berechne</c> auf
        /// einer Vergleichsgruppe aus EINEM Projekt. Nichts wird nachgerechnet.</para>
        /// </summary>
        private static WirtschaftlichkeitErgebnis Rechne(string modus)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            p.StromsteuerBefreiungModus = modus;

            // Die VIER Bedingungen des § 9 Abs. 1 Nr. 3 haengen nicht am Modus, und der
            // Parametersatz des Projekts 1024 fuehrt zwei von ihnen ungepflegt. Ohne sie
            // bliebe die Befreiung null - der Pruefstand maesse dann die Abwesenheit
            // einer Zahl statt den Unterschied zwischen zwei Modi. Die beiden Haken
            // stehen deshalb HIER und in BEIDEN Laeufen gleich; gemessen wird allein,
            // was der Modus daraus macht.
            p.HocheffizienzNachweis = true;
            p.RaeumlicherZusammenhang = true;
            ctrl.SpeichereParameter(p);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall B6",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT),
                Zeitreihen = Stundenreihen()
            };
            KostenEmissionRechner.Berechne(v);

            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);

            List<WirtschaftlichkeitErgebnis> alle = new WirtschaftlichkeitCtrl().Berechne(daten, p);
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            return e;
        }

        /// <summary>
        /// Die Stundenreihen des Prüffalls — <b>flach, und das mit Absicht</b>.
        ///
        /// <para><b>Warum sie hier entstehen und nicht aus der Datenbank kommen.</b> Die
        /// Befreiung setzt den KWK-EIGENVERBRAUCH voraus, und der ist nur aus
        /// Stundenreihen bestimmbar: Ohne sie meldet der Rechner „nicht bestimmbar" und
        /// setzt die Befreiung auf null (<c>STEUER_STROMST_EIGEN_UNKLAR</c>). Die
        /// Testdatenbank führt zu keinem ihrer Projekte einen Reihensatz — mit ihr allein
        /// wäre der Befund B-1 an KEINEM Projekt messbar. Die drei Reihen stehen deshalb
        /// hier.</para>
        ///
        /// <para><b>Sie sind nicht ausgedacht, sondern die Jahressummen des gespeicherten
        /// Laufs, gleichmäßig auf 8760 Stunden gelegt</b>: Strombedarf
        /// <see cref="BEDARF_MWH"/>, BHKW-Erzeugung <see cref="BHKW_MWH"/>, Netzbezug als
        /// deren Differenz. Weil die Erzeugung in jeder Stunde unter dem Bedarf liegt, ist
        /// sie in jeder Stunde Eigenverbrauch — der KWK-Eigenverbrauch des Jahres ist
        /// damit genau <see cref="BHKW_MWH"/>, und der erwartete Ausweisbetrag ist
        /// Regelsatz × diese Menge.</para>
        /// </summary>
        private static ZeitreihenSatz Stundenreihen()
        {
            int n = ZeitreihenSatz.Stunden;
            var bedarf = new double[n];
            var bhkw = new double[n];
            var bezug = new double[n];

            double bedarfKWh = BEDARF_MWH * 1000.0 / n;
            double bhkwKWh = BHKW_MWH * 1000.0 / n;

            for (int h = 0; h < n; h++)
            {
                bedarf[h] = bedarfKWh;
                bhkw[h] = bhkwKWh;
                bezug[h] = bedarfKWh - bhkwKWh;
            }

            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = bedarf;
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = bhkw;
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = bezug;
            return z;
        }
    }
}
