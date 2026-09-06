using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Verwendungskatalogs</b> (Anwenderwunsch W14a-E-8 vom
    /// 06.09.2026, Teil 1).
    ///
    /// <para><b>Wogegen geprueft wird.</b> Nicht gegen eine eingefrorene Liste, sondern
    /// gegen die DATENBANK selbst: <c>pragma table_info</c> der Testdatenbank sagt, welche
    /// Spalten eine Stammtabelle fuehrt, und <see cref="ParameterVerwendung.Katalog"/> muss
    /// genau diese Menge nennen — keine vergessene, keine erfundene. Waere die Erwartung
    /// eine zweite Liste im Testcode, ginge sie beim naechsten Migrationsschritt
    /// zusammen mit dem Katalog verloren, und die Probe bliebe gruen, obwohl die
    /// Uebersicht eine Spalte verschweigt.</para>
    ///
    /// <para><b>Und die Belegpflicht.</b> Eine Einstufung ohne Fundstelle ist eine
    /// Behauptung. <see cref="Jede_gerechnete_Spalte_nennt_eine_Fundstelle"/> faellt rot
    /// aus, sobald eine als <c>Simulation</c> oder <c>Wirtschaftlichkeit</c> gefuehrte
    /// Spalte keine Datei und Zeile nennt.</para>
    ///
    /// <para><b>Nur lesend, eine Arbeitskopie je Klasse</b> (Regel seit iU9-W11a).
    /// Fehlt die Datei, schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ParameterVerwendungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public ParameterVerwendungTests(TestDatenbank db) { _db = db; }

        // =================================================================================
        // 1 - Vollstaendigkeit gegen pragma table_info
        // =================================================================================

        /// <summary>
        /// Fuer jede der sieben Anlagenarten: Der Katalog deckt ALLE Spalten der
        /// Stammtabelle ab, und er nennt keine, die es nicht gibt.
        /// </summary>
        [Fact]
        public void Der_Katalog_deckt_jede_Spalte_der_Stammtabelle_ab()
        {
            if (!_db.Vorhanden) return;

            foreach (Anlagenart art in ParameterVerwendung.AlleArten)
            {
                string tabelle = ParameterVerwendung.Stammtabelle(art);
                List<string> inDerDb = SpaltenDerTabelle(tabelle);
                Assert.True(inDerDb.Count > 0, tabelle + " liefert keine Spalten");

                List<string> imKatalog = ParameterVerwendung.Katalog(art)
                                                            .Select(e => e.Spalte).ToList();

                string fehlt = string.Join(", ", inDerDb.Except(imKatalog, Vergleich));
                string erfunden = string.Join(", ", imKatalog.Except(inDerDb, Vergleich));

                Assert.True(fehlt.Length == 0, art + " (" + tabelle + "): nicht im Katalog — " + fehlt);
                Assert.True(erfunden.Length == 0, art + " (" + tabelle + "): gibt es nicht — " + erfunden);
                Assert.Equal(inDerDb.Count, imKatalog.Count);
            }
        }

        /// <summary>
        /// Die Reihenfolge des Katalogs ist die der Tabelle — so liest sich die Uebersicht
        /// wie die Spaltenliste, und ein neuer Eintrag faellt an seinem Platz auf.
        /// </summary>
        [Fact]
        public void Der_Katalog_folgt_der_Reihenfolge_der_Tabelle()
        {
            if (!_db.Vorhanden) return;

            foreach (Anlagenart art in ParameterVerwendung.AlleArten)
            {
                List<string> inDerDb = SpaltenDerTabelle(ParameterVerwendung.Stammtabelle(art));
                List<string> imKatalog = ParameterVerwendung.Katalog(art)
                                                            .Select(e => e.Spalte).ToList();
                Assert.Equal(inDerDb, imKatalog);
            }
        }

        // =================================================================================
        // 2 - Belegpflicht
        // =================================================================================

        /// <summary>
        /// Jede als <c>Simulation</c> oder <c>Wirtschaftlichkeit</c> eingestufte Spalte
        /// nennt eine Fundstelle. Eine Einstufung ohne Beleg ist eine Behauptung.
        /// </summary>
        [Fact]
        public void Jede_gerechnete_Spalte_nennt_eine_Fundstelle()
        {
            foreach (Anlagenart art in ParameterVerwendung.AlleArten)
                foreach (ParameterEintrag e in ParameterVerwendung.Katalog(art))
                {
                    if (!e.Gerechnet) continue;
                    Assert.False(string.IsNullOrWhiteSpace(e.Fundstelle),
                                 art + "." + e.Spalte + " ist gerechnet, nennt aber keine Fundstelle");
                }
        }

        /// <summary>
        /// <c>Keine</c> steht allein: Wer nicht verwendet wird, traegt keine zweite Stufe —
        /// sonst waere die Kennzeichnung in der Uebersicht widerspruechlich.
        /// </summary>
        [Fact]
        public void Nicht_verwendet_steht_allein()
        {
            foreach (Anlagenart art in ParameterVerwendung.AlleArten)
                foreach (ParameterEintrag e in ParameterVerwendung.Katalog(art))
                {
                    Assert.NotNull(e.Verwendung);
                    Assert.NotEmpty(e.Verwendung);
                    if (e.Hat(Verwendung.Keine))
                        Assert.Single(e.Verwendung);
                }
        }

        /// <summary>
        /// Jeder Eintrag traegt einen Anzeigetext. Ohne Uebersetzer ist das der
        /// Ressourcenschluessel selbst — leer darf er nie sein.
        /// </summary>
        [Fact]
        public void Jeder_Eintrag_traegt_einen_Anzeigetext()
        {
            foreach (Anlagenart art in ParameterVerwendung.AlleArten)
                foreach (ParameterEintrag e in ParameterVerwendung.Katalog(art))
                {
                    Assert.False(string.IsNullOrWhiteSpace(e.Spalte));
                    Assert.False(string.IsNullOrWhiteSpace(e.Anzeigetext),
                                 art + "." + e.Spalte + " hat keinen Anzeigetext");
                    Assert.NotNull(e.Einheit);
                }
        }

        /// <summary>
        /// Die Beschriftungen kommen aus dem Ressourcenkatalog: Der Uebersetzer wird
        /// tatsaechlich gerufen, und ein vorhandener Schluessel liefert einen ANDEREN Text
        /// als der Schluessel selbst. Ohne diesen Fall koennte der Katalog seine Texte
        /// still hart verdrahten.
        /// </summary>
        [Fact]
        public void Die_Beschriftungen_laufen_ueber_den_Uebersetzer()
        {
            var gerufen = new List<string>();
            IReadOnlyList<ParameterEintrag> mit =
                ParameterVerwendung.Katalog(Anlagenart.Heizkessel, s => { gerufen.Add(s); return "<" + s + ">"; });

            Assert.Contains("HZKK_LBL_PTHERM", gerufen);
            Assert.Contains(mit, e => e.Spalte == "Ptherm" && e.Anzeigetext == "<HZKK_LBL_PTHERM>");
        }

        // =================================================================================
        // 3 - Die Einstufungen, an denen der Anwenderwunsch haengt
        // =================================================================================

        /// <summary>
        /// Die Stichproben aus dem Rechenweg: Was der Lauf liest, steht als
        /// <c>Simulation</c> im Katalog.
        /// </summary>
        [Theory]
        [InlineData(Anlagenart.Heizkessel, "Ptherm")]
        [InlineData(Anlagenart.Heizkessel, "Betriebsbereitschaftverlust")]
        [InlineData(Anlagenart.Heizkessel, "Vorlauf")]
        [InlineData(Anlagenart.Bhkw, "Grenzleistung")]
        [InlineData(Anlagenart.Bhkw, "CO2")]
        [InlineData(Anlagenart.Waermepumpe, "Heizung")]
        [InlineData(Anlagenart.Waermepumpe, "Nennleistung")]
        [InlineData(Anlagenart.Solarkollektoren, "h0")]
        [InlineData(Anlagenart.Solarkollektoren, "Aperturflaeche")]
        [InlineData(Anlagenart.Photovoltaik, "gamma_PMP")]
        [InlineData(Anlagenart.Photovoltaik, "T_NOCT")]
        [InlineData(Anlagenart.Stromspeicher, "Wirkungsgrad_RT")]
        [InlineData(Anlagenart.Pufferspeicher, "Gesamtvolumen")]
        public void Der_Rechenweg_ist_als_Simulation_gekennzeichnet(Anlagenart art, string spalte)
        {
            Assert.True(Eintrag(art, spalte).Hat(Verwendung.Simulation),
                        art + "." + spalte + " muesste Simulation sein");
        }

        /// <summary>
        /// Die Stichproben der Kostenseite: Was die Kostenplanung oder die
        /// Wirtschaftlichkeit liest, steht als <c>Wirtschaftlichkeit</c> im Katalog.
        /// </summary>
        [Theory]
        [InlineData(Anlagenart.Heizkessel, "Investitionskosten")]
        [InlineData(Anlagenart.Heizkessel, "Wartungskosten")]
        [InlineData(Anlagenart.Bhkw, "Kosten_Modul")]
        [InlineData(Anlagenart.Bhkw, "Wartungskosten_kwhel")]
        [InlineData(Anlagenart.Waermepumpe, "Modulkosten")]
        [InlineData(Anlagenart.Solarkollektoren, "Investitionskosten")]
        [InlineData(Anlagenart.Photovoltaik, "Modulkosten")]
        [InlineData(Anlagenart.Stromspeicher, "Leistungskosten")]
        [InlineData(Anlagenart.Pufferspeicher, "Investitionskosten")]
        public void Die_Kostenseite_ist_als_Wirtschaftlichkeit_gekennzeichnet(Anlagenart art, string spalte)
        {
            Assert.True(Eintrag(art, spalte).Hat(Verwendung.Wirtschaftlichkeit),
                        art + "." + spalte + " muesste Wirtschaftlichkeit sein");
        }

        /// <summary>
        /// <b>Der Befund des Kessels</b> (W14a-E-8): Seine fuenf Emissionsspalten werden
        /// gepflegt, aber nicht gerechnet — der Lauf holt die Faktoren aus
        /// <c>Tab_Brennstoff_Stamm</c> (<c>SimulationSPK.cs:151-158</c>). Faellt dieser
        /// Fall eines Tages rot aus, hat jemand den Rechenweg umgestellt, und die
        /// Kennzeichnung muss mit.
        /// </summary>
        [Theory]
        [InlineData("CO2")]
        [InlineData("SO2")]
        [InlineData("NOx")]
        [InlineData("CO")]
        [InlineData("Staub")]
        public void Die_Emissionen_des_Kessels_sind_nur_Pflege(string spalte)
        {
            ParameterEintrag e = Eintrag(Anlagenart.Heizkessel, spalte);
            Assert.False(e.Gerechnet, "Tab_Heizkessel_STAMM." + spalte + " gilt als gerechnet");
            Assert.True(e.Hat(Verwendung.Dialog));
        }

        /// <summary>
        /// <b>Der Befund der Waermepumpe</b> (W14a-E-8): Fuenf Spalten aus dem
        /// VDI-3805-Import hat kein Leser — sie stehen als <c>Keine</c> im Katalog und
        /// sind damit in der Uebersicht als „nicht verwendet" gekennzeichnet.
        /// </summary>
        [Theory]
        [InlineData("Laenge")]
        [InlineData("Breite")]
        [InlineData("Hoehe")]
        [InlineData("Gewicht")]
        [InlineData("Raum")]
        public void Die_Masse_der_Waermepumpe_sind_nicht_verwendet(string spalte)
        {
            Assert.True(Eintrag(Anlagenart.Waermepumpe, spalte).Hat(Verwendung.Keine),
                        "Tab_WP_STAMM." + spalte + " muesste „nicht verwendet“ sein");
        }

        /// <summary>
        /// <b>Die Luecke, die Teil 3 des Anwenderwunsches gefunden hat</b>:
        /// <c>Tab_WP_STAMM.Modulkosten</c> geht in die Kostenplanung
        /// (<c>TechnikPlanwertCtrl.cs:345</c>), steht aber in der Verwaltung nicht zur
        /// Pflege (Entscheid Ä19 der Welle 7). Der Fall haelt die Einstufung fest,
        /// damit der offene Punkt nicht still verschwindet.
        /// </summary>
        [Fact]
        public void Die_Modulkosten_der_Waermepumpe_sind_gerechnet()
        {
            ParameterEintrag e = Eintrag(Anlagenart.Waermepumpe, "Modulkosten");
            Assert.True(e.Gerechnet);
            Assert.Contains("TechnikPlanwertCtrl", e.Fundstelle);
        }

        /// <summary>
        /// Jede Anlagenart fuehrt mindestens einen gerechneten Parameter — ein Katalog
        /// ganz ohne Rechenbezug waere ein Zeichen dafuer, dass die Einstufung fehlt.
        /// </summary>
        [Fact]
        public void Jede_Anlagenart_fuehrt_gerechnete_Parameter()
        {
            foreach (Anlagenart art in ParameterVerwendung.AlleArten)
                Assert.Contains(ParameterVerwendung.Katalog(art), e => e.Gerechnet);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static readonly IEqualityComparer<string> Vergleich = StringComparer.OrdinalIgnoreCase;

        private static ParameterEintrag Eintrag(Anlagenart art, string spalte)
        {
            ParameterEintrag e = ParameterVerwendung.Katalog(art)
                .FirstOrDefault(x => string.Equals(x.Spalte, spalte, StringComparison.OrdinalIgnoreCase));
            Assert.True(e != null, art + " kennt keine Spalte " + spalte);
            return e;
        }

        /// <summary>
        /// Die Spalten einer Tabelle in Schemareihenfolge — ueber
        /// <see cref="DataRepository.SpaltenVonTabelle"/>, den Weg der Zugriffsschicht
        /// (<c>SELECT name FROM pragma_table_info(?) ORDER BY cid</c>). Kein neues SQL
        /// im Testcode: Was die Anwendung fragt, fragt auch die Probe.
        /// </summary>
        private static List<string> SpaltenDerTabelle(string tabelle)
        {
            return DataRepository.SpaltenVonTabelle(tabelle) ?? new List<string>();
        }
    }
}
