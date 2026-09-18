using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE BK1 — der KWK-Zuschlag gehört der ANLAGE</b> (Anwenderwunsch
    /// 17.09.2026, Entscheid BK-E-1 (a) vom 18.09.2026, Schemaschritt 89).
    ///
    /// <para><b>Die Lage vorher.</b> Der Zuschlag hatte zwei Wahrheiten — eine je
    /// Anlage (<c>Tab_Energieanlagen.KWKG_*</c>, Schritt 22) und eine je Projekt
    /// (<c>Tab_ProjektWirtschaftlichkeit.KWKG_*</c>, Schritt 28) — und dazwischen eine
    /// Rückfallkette: Was an der Anlage leer war, holte der Rechenweg aus dem Projekt.
    /// § 7 KWKG bemisst den Satz aber an der LEISTUNG der einzelnen Anlage und § 8 das
    /// Kontingent an IHRER Anlagenart; eine Kaskade aus zwei verschieden alten Modulen
    /// war so nicht abbildbar. Dazu stand der Aktivierungsschalter
    /// <c>p.KwkgBonus &gt; 0 || p.KwkgBonusEinspeisung &gt; 0</c> an SECHS Stellen im
    /// Haus. Die elf Projektspalten sind mit Schemaschritt 90 gefallen, soweit sie
    /// eine Rechengröße trugen.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Fünf Dinge:
    /// <list type="number">
    ///   <item><description>Schemaschritt 89 steht — Spalte und
    ///     Datenanweisungen.</description></item>
    ///   <item><description>Der Datenschritt ist in der Testdatenbank angekommen und
    ///     wiederholt sich folgenlos.</description></item>
    ///   <item><description><see cref="KwkgAktivierung"/> fragt die ANLAGEN, nicht das
    ///     Projekt — die eine Regel für alle sechs Stellen.</description></item>
    ///   <item><description>Der Rechenweg ist ergebnisgleich: Projekt 1030 rechnet
    ///     denselben Zuschlag wie vor dem Schritt.</description></item>
    ///   <item><description>Das Kontingent kommt je Anlage aus IHRER Anlagenart und
    ///     IHREM Kostenanteil.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist; die
    /// Kultur ist gepinnt, weil Herleitungstexte aus den Satellitenressourcen kommen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkAnlagenwahrheitTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — <b>das einzige Projekt der Testdatenbank
        /// mit gepflegten KWKG-Vorgaben</b> (Bonus 4,00 / Einspeisung 8,00 ct/kWh,
        /// Kontingent 30.000 Vbh) und zugleich eines mit ZWEI BHKW-Modulen. An ihm
        /// misst sich die Ergebnisgleichheit des Datenschritts.</summary>
        private const int PROJEKT = 1030;

        /// <summary>Strombedarf des gespeicherten Laufs von <see cref="PROJEKT"/>
        /// [MWh/a] — dieselbe Quelle wie in <c>StromsteuerBefreiungModusTests</c>.</summary>
        private const double BEDARF_MWH = 4790.09;

        /// <summary>BHKW-Stromerzeugung desselben Laufs [MWh/a].</summary>
        private const double BHKW_MWH = 432.3;

        /// <summary>
        /// <b>Der Anker der Ergebnisgleichheit.</b> KWK-Zuschlag im Jahr 1 von
        /// <see cref="PROJEKT"/> [€/a], gemessen am Stand VOR dem Schemaschritt 89 (mit
        /// der Rückfallkette Anlage → Projekt) und danach unverändert. Die Zahl steht
        /// hier, damit ein späterer Eingriff in die Kette auffällt.
        /// </summary>
        private const double ZUSCHLAG_JAHR1_EUR = 7315.956634;

        /// <summary>Eigenstrom-Satz, den Schritt 89 aus der Projektvorgabe von
        /// <see cref="PROJEKT"/> in jede BHKW-Anlagenzeile geschrieben hat
        /// [ct/kWh].</summary>
        private const double SATZ_EIGEN_CT = 4.0;

        /// <summary>Ebenso der Einspeisesatz [ct/kWh].</summary>
        private const double SATZ_EINSP_CT = 8.0;

        /// <summary>Ebenso das Vbh-Kontingent [h].</summary>
        private const double KONTINGENT_H = 30000.0;

        // =================================================================
        // 1 — Schemaschritt 89
        // =================================================================

        /// <summary>
        /// Der Zielstand trägt den Schritt 89, die eine Spalte hängt an
        /// <c>Tab_Energieanlagen</c>, und die neun Wertepaare des Datenschritts stehen
        /// vollständig da. Die Listen sind die EINE Quelle, aus der sich Migration,
        /// Werkzeug und Nachweis bedienen.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_89()
        {
            Assert.True(SchemaStand.Zielversion >= 89,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 89.");
            Assert.Single(SchemaKatalog.Schritt89_KwkAnlagenwahrheit);
            Assert.Equal(SchemaKatalog.TAB_ENERGIEANLAGEN,
                         SchemaKatalog.Schritt89_KwkAnlagenwahrheit[0].Tabelle);
            Assert.Equal(SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL,
                         SchemaKatalog.Schritt89_KwkAnlagenwahrheit[0].Name);

            Assert.Equal(9, KwkAnlagenwahrheit.Paare.Count);
            Assert.All(KwkAnlagenwahrheit.Paare, x => Assert.False(string.IsNullOrEmpty(x.Anlage)));
            Assert.All(KwkAnlagenwahrheit.Paare, x => Assert.False(string.IsNullOrEmpty(x.Projekt)));

            // Der TATBESTAND ist dabei, und das ist kein Zufall: Ohne ihn stünde an der
            // Anlage ein Satz auf selbst genutzten Strom ohne die Voraussetzung des
            // § 6 Abs. 3, die ihn trägt.
            Assert.Contains(KwkAnlagenwahrheit.Paare,
                x => x.Anlage == SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL &&
                     x.Projekt == KwkgProjektaltspalten.SPALTE_TATBESTAND);
        }

        /// <summary>Die Testdatenbank führt die neue Spalte — das Werkzeug
        /// <c>Werkzeuge/Testdatenbankschema</c> hat den Schritt gezogen.</summary>
        [Fact]
        public void Die_Testdatenbank_fuehrt_die_neue_Spalte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.SpalteVorhanden(SchemaKatalog.TAB_ENERGIEANLAGEN,
                                                       SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL));
        }

        // =================================================================
        // 2 — Der Datenschritt
        // =================================================================

        /// <summary>
        /// <b>Die Vorgaben sind unten angekommen.</b> Beide BHKW-Module des Projekts
        /// tragen die Sätze und das Kontingent, die vor Schritt 89 nur am Projekt
        /// standen — und zwar wertgleich.
        ///
        /// <para><b>Umgestellt mit Schemaschritt 90:</b> Die vier Projektspalten,
        /// gegen die dieser Fall bis dahin verglich, gibt es nicht mehr
        /// (<see cref="KwkgProjektaltspalten"/>). Die Zusicherung liegt seither an
        /// <c>Tab_Energieanlagen</c> und misst gegen die Werte, die Projekt
        /// <see cref="PROJEKT"/> vor dem Schritt trug — sie stehen hier als
        /// Konstanten, damit ein Eingriff in den Datenschritt weiterhin auffällt.
        /// Stichtag und Inbetriebnahme des Projekts bestehen fort und werden
        /// deshalb weiter gegen den Parametersatz gehalten.</para>
        /// </summary>
        [Fact]
        public void Die_Projektvorgaben_stehen_an_den_Anlagen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var anlagen = new KwkgAnlagenCtrl().LadeGruppe(PROJEKT, "");
            Assert.True(anlagen.Count >= 2, "Projekt " + PROJEKT + " führt weniger als zwei BHKW.");

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            foreach (KwkgAnlagenAngabe a in anlagen.Where(x => x.IdProjekt == PROJEKT))
            {
                Assert.Equal(SATZ_EIGEN_CT, a.SatzEigenCt);
                Assert.Equal(SATZ_EINSP_CT, a.SatzEinspCt);
                Assert.Equal(KONTINGENT_H, a.VbhKontingent);
                Assert.Equal(p.KwkgStichtag, a.Stichtag);
                Assert.Equal(p.KwkgInbetriebnahme, a.Inbetriebnahme);
            }
        }

        /// <summary>
        /// <b>Wiederholung ändert nichts.</b> Jede der neun Anweisungen trägt ihre
        /// Bedingung selbst — nach dem ersten Lauf findet die Zählung null offene
        /// Zeilen, und ein zweiter Lauf fasst keine an.
        /// </summary>
        [Fact]
        public void Der_Datenschritt_wiederholt_sich_folgenlos()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (KwkAnlagenwahrheit.Paar paar in KwkAnlagenwahrheit.Paare)
            {
                object o = DataRepository.ExecuteScalar(KwkAnlagenwahrheit.Zaehlung(paar));
                Assert.Equal(0, Convert.ToInt32(o));
            }
        }

        /// <summary>
        /// Der Schritt fasst <b>nur BHKW-Zeilen</b> an — Kessel, Wärmepumpen und
        /// Speicher kennen den KWK-Zuschlag nicht, und eine KWKG-Spalte an ihrer Zeile
        /// wäre eine Aussage, die es nicht gibt.
        /// </summary>
        [Fact]
        public void Nur_BHKW_Zeilen_tragen_KWKG_Angaben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                " WHERE ID_Type <> " + WizardItemClass.BHKW_TYP +
                " AND ([" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + "] IS NOT NULL" +
                " OR [" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + "] IS NOT NULL)");
            Assert.Equal(0, Convert.ToInt32(o));
        }

        // =================================================================
        // 3 — Der Aktivierungsschalter: EINE Regel, und sie fragt die Anlagen
        // =================================================================

        /// <summary>
        /// Die Regel selbst, auf zwei Zahlen: <c>null</c> und 0 heißen beide „nein",
        /// ein Satz größer 0 auf einer der beiden Seiten heißt „ja".
        /// </summary>
        [Theory]
        [InlineData(null, null, false)]
        [InlineData(0.0, 0.0, false)]
        [InlineData(4.0, null, true)]
        [InlineData(null, 8.0, true)]
        [InlineData(0.0, 8.0, true)]
        public void Die_Regel_liest_nur_die_beiden_Saetze(double? eigen, double? einsp, bool aktiv)
        {
            Assert.Equal(aktiv, KwkgAktivierung.SatzGefuehrt(eigen, einsp));
        }

        /// <summary>
        /// <b>Die Gegenprobe zum alten Schalter.</b> Ein Projekt, dessen ANLAGEN
        /// keinen Satz führen, ist NICHT aktiv — der alte Ausdruck
        /// <c>p.KwkgBonus &gt; 0 || p.KwkgBonusEinspeisung &gt; 0</c> hätte hier noch
        /// „aktiv" gesagt, solange die Projektspalten standen. Seit Schemaschritt 90
        /// gibt es sie nicht mehr; die Anlagen sind die einzige Quelle, und EINE
        /// Anlage mit einem Satz genügt.
        /// </summary>
        [Fact]
        public void Der_Schalter_fragt_die_Anlagen_und_nicht_das_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(KwkgAktivierung.IstAktiv(PROJEKT),
                        "Der Prüffall braucht ein Projekt mit gepflegten Anlagensätzen.");

            // Die Sätze der Anlagen weg — es gibt keine zweite Quelle mehr.
            SaetzeDerAnlagenLeeren(PROJEKT);
            Assert.False(KwkgAktivierung.IstAktiv(PROJEKT),
                         "Der Schalter liest noch eine andere Quelle als die Anlage.");

            // Eine einzige Anlage genügt, um die Gruppe wieder aktiv zu machen.
            var anlagen = new KwkgAnlagenCtrl().LadeGruppe(PROJEKT, "");
            KwkgAnlagenAngabe eine = anlagen.First(a => a.IdProjekt == PROJEKT);
            eine.SatzEinspCt = 8.0;
            Assert.True(new KwkgAnlagenCtrl().Speichere(eine, true));
            Assert.True(KwkgAktivierung.IstAktiv(PROJEKT));
        }

        /// <summary>Die Gruppenfassung antwortet für Stamm ODER Version — dasselbe
        /// Muster wie <c>KostenEmissionRechner.StromLeistungspreisGepflegt</c>.</summary>
        [Fact]
        public void Der_Schalter_antwortet_fuer_die_ganze_Gruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(KwkgAktivierung.IstAktiv(0, new[] { PROJEKT }));
            Assert.False(KwkgAktivierung.IstAktiv(0, new int[0]));
            Assert.False(KwkgAktivierung.IstAktiv(0, null));
        }

        // =================================================================
        // 4 — Die Ergebnisgleichheit
        // =================================================================

        /// <summary>
        /// <b>DER NACHWEIS DES DATENSCHRITTS.</b> Projekt <see cref="PROJEKT"/> rechnet
        /// nach dem Schritt denselben Zuschlag wie davor — <see cref="ZUSCHLAG_JAHR1_EUR"/>
        /// im Jahr 1, gemessen am Stand mit der Rückfallkette. Die Anlagen tragen jetzt
        /// selbst, was ihnen der Rückfall zugewiesen hat.
        /// </summary>
        [Fact]
        public void Der_Zuschlag_bleibt_nach_dem_Datenschritt_derselbe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.True(Math.Abs(e.KwkgErloesJahr1 - ZUSCHLAG_JAHR1_EUR) <= 0.01,
                        "KWK-Zuschlag Jahr 1 erwartet " + ZUSCHLAG_JAHR1_EUR.ToString("F6") +
                        " €, gemessen " + e.KwkgErloesJahr1.ToString("F6") + " €.");
        }

        /// <summary>
        /// <b>Die Gegenprobe dazu:</b> Nimmt man den Anlagen ihre Sätze weg, fällt der
        /// Zuschlag auf 0. Damit ist bewiesen, dass die Zahl oben aus den Anlagen
        /// kommt und nicht mehr aus einem Rückfall.
        /// </summary>
        [Fact]
        public void Ohne_Anlagensaetze_gibt_es_trotz_Projektsaetzen_keinen_Zuschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SaetzeDerAnlagenLeeren(PROJEKT);

            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.Equal(0.0, e.KwkgErloesJahr1);
        }

        // =================================================================
        // 5 — Das Kontingent je Anlage
        // =================================================================

        /// <summary>
        /// § 8 KWKG wählt die Stufe aus Anlagenart UND Kostenanteil — und beides steht
        /// seit Schritt 89 an der Anlage. Zwei Anlagen mit verschiedenem Kostenanteil
        /// bekommen deshalb verschiedene Kontingente; bis BK1 teilten sie sich die eine
        /// Projektzahl.
        /// </summary>
        [Fact]
        public void Zwei_Anlagen_mit_verschiedenem_Kostenanteil_bekommen_verschiedene_Kontingente()
        {
            var katalog = Kontingentkatalog();
            var kultur = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

            KwkgKontingentVorschlag klein = KwkgKontingentRechner.Ableiten(
                DbWerte.KWKG_ANLAGENART_MODERNISIERT, 30, 2027, katalog, kultur);
            KwkgKontingentVorschlag gross = KwkgKontingentRechner.Ableiten(
                DbWerte.KWKG_ANLAGENART_MODERNISIERT, 60, 2027, katalog, kultur);

            Assert.Equal(15000.0, klein.KontingentH);
            Assert.Equal(30000.0, gross.KontingentH);
            Assert.NotEqual(klein.Herleitung, gross.Herleitung);

            // Ohne Anlagenart gibt es kein Kontingent, sondern eine Begründung.
            KwkgKontingentVorschlag ohne = KwkgKontingentRechner.Ableiten("", 60, 2027, katalog, kultur);
            Assert.True(ohne.Unvollstaendig);
            Assert.Equal(0.0, ohne.KontingentH);
            Assert.False(string.IsNullOrEmpty(ohne.Herleitung));
        }

        /// <summary>
        /// Der Kostenanteil je Anlage übersteht Speichern und Laden — der Schreibweg
        /// des Dialogs hängt daran (zwölfte Spalte von
        /// <c>KwkgAnlagenCtrl.Speichere(g, true)</c>).
        /// </summary>
        [Fact]
        public void Der_Kostenanteil_je_Anlage_ueberlebt_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new KwkgAnlagenCtrl();
            KwkgAnlagenAngabe a = ctrl.LadeGruppe(PROJEKT, "").First(x => x.IdProjekt == PROJEKT);
            a.Kostenanteil = 42.5;
            Assert.True(ctrl.Speichere(a, true));

            KwkgAnlagenAngabe zurueck = new KwkgAnlagenCtrl().LadeGruppe(PROJEKT, "")
                                        .First(x => x.IdAnlage == a.IdAnlage);
            Assert.Equal(42.5, zurueck.Kostenanteil);
        }

        // =================================================================
        // Prüfstand
        // =================================================================

        /// <summary>Nimmt allen BHKW-Anlagen des Projekts ihre beiden Sätze — die
        /// PROJEKTspalten bleiben dabei unangetastet.</summary>
        private static void SaetzeDerAnlagenLeeren(int idProjekt)
        {
            DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                " SET [" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + "] = NULL," +
                " [" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + "] = NULL" +
                " WHERE ID_Projekt = ?", new DbParam("@p", idProjekt));
        }

        /// <summary>Ein Katalog, der die Schwellen und Stufen des § 8 beantwortet.</summary>
        private static Func<string, int, GesetzParameter> Kontingentkatalog()
        {
            return (schluessel, jahr) =>
            {
                double w = 0;
                if (schluessel == DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_50) w = 50;
                else if (schluessel == DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_25) w = 25;
                else if (schluessel == DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_10) w = 10;
                else if (schluessel == DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_25) w = 15000;
                else if (schluessel == DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_50) w = 30000;
                return w > 0 ? new GesetzParameter(1, schluessel, "KWKG", 2020, w, "h", "", "") : null;
            };
        }

        /// <summary>
        /// Rechnet <see cref="PROJEKT"/> als Stammprojekt und liefert das Ergebnis des
        /// Szenarios ERWARTET — derselbe Weg wie in <c>StromsteuerBefreiungModusTests</c>:
        /// Parametersatz laden, Kosten und Emissionen rechnen,
        /// <c>WirtschaftlichkeitCtrl.Berechne</c> auf einer Gruppe aus EINEM Projekt.
        /// Nichts wird nachgerechnet.
        /// </summary>
        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall BK1",
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
        /// Flache Stundenreihen aus den Jahressummen des gespeicherten Laufs —
        /// <b>wortgleich zu <c>StromsteuerBefreiungModusTests.Stundenreihen</c></b> und
        /// aus demselben Grund: Ohne Stundenreihen ist der Eigen-/Einspeise-Split nicht
        /// bestimmbar, und die Testdatenbank führt zu keinem Projekt einen Reihensatz.
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
