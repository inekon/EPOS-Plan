using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE BK1a — der Ersatzweg des KWK-Zuschlags rechnet mit einer
    /// leistungsgewichteten virtuellen Gesamtanlage.</b>
    ///
    /// <para><b>Wozu.</b> Lassen sich Anlagen- und Ergebnismodulzeilen nicht paaren
    /// (<c>Bestimmbar = false</c>), fehlt die Zuordnung Menge → Anlage, nicht aber die
    /// Anlage. Bis BK1a las dieser Zweig als einziger noch die sechs KWKG-Spalten von
    /// <c>Tab_ProjektWirtschaftlichkeit</c>; seither bildet er aus den Anlagen EINE
    /// virtuelle Gesamtanlage, deren vier Eingangsgrößen mit der elektrischen
    /// Nennleistung gewichtet gemittelt sind.</para>
    ///
    /// <para><b>Der Prüfstand.</b> Projekt <see cref="PROJEKT"/> führt zwei BHKW-Anlagen
    /// (50 und 9 kW<sub>el</sub>) und ist das einzige Projekt der Testdatenbank mit
    /// gepflegten KWKG-Angaben. <see cref="ZuordnungZerstoeren"/> verstellt die
    /// Modulnamen und verschiebt die Zeilenzahl — danach läuft zwangsläufig der
    /// Ersatzweg. Die Vollbenutzungsstunden kommen dabei aus dem gebuchten Lauf
    /// (<c>Tab_ErgebnisBHKW.VbhElektrisch</c>) und sind damit von Nennleistung und
    /// Anlagenzahl <b>unabhängig</b> — deshalb sind die Fälle unten reine Aussagen über
    /// die Gewichtung.</para>
    ///
    /// <para><b>Die Fälle prüfen VERHÄLTNISSE, nicht Absolutbeträge</b> (bis auf den
    /// Anker): <c>reihe[1]</c> ist linear im Eigenstromsatz, weil der Satz allein in
    /// <c>bonusVoll</c> steht und jede andere Größe des Jahres unberührt bleibt. Ein
    /// Verhältnis ist deshalb die genaueste und zugleich robusteste Aussage über die
    /// Gewichtung.</para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist; die
    /// Kultur ist gepinnt, weil die Hinweistexte aus den Ressourcen kommen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgErsatzwegGewichtetTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — zwei BHKW-Anlagen, gepflegte
        /// KWKG-Angaben.</summary>
        private const int PROJEKT = 1030;

        /// <summary><c>Tab_Energieanlagen.ID</c> der 50-kW-Anlage.</summary>
        private const int ANLAGE_GROSS = 14920;

        /// <summary><c>Tab_Energieanlagen.ID</c> der 9-kW-Anlage.</summary>
        private const int ANLAGE_KLEIN = 14921;

        private const double PEL_GROSS = 50.0;
        private const double PEL_KLEIN = 9.0;

        /// <summary>Strombedarf des gespeicherten Laufs [MWh/a].</summary>
        private const double BEDARF_MWH = 4790.09;

        /// <summary>BHKW-Stromerzeugung desselben Laufs [MWh/a].</summary>
        private const double BHKW_MWH = 432.3;

        /// <summary>
        /// <b>Der Anker.</b> KWK-Zuschlag im Jahr 1 des Ersatzwegs [€/a], gemessen am
        /// Stand VOR BK1a — also mit den Projektspalten als Quelle. Beide Anlagen tragen
        /// seit Schemaschritt 89 dieselben Werte, die vorher am Projekt standen; das
        /// leistungsgewichtete Mittel gleicher Werte ist dieser Wert, und deshalb rechnet
        /// der neue Weg dieselbe Zahl.
        /// </summary>
        private const double ANKER_JAHR1_EUR = 7315.948722;

        /// <summary>Der Eigenstromsatz, den Schritt 89 an beide Anlagen geschrieben hat
        /// [ct/kWh].</summary>
        private const double SATZ_BESTAND = 4.0;

        // =================================================================
        // 1 — Der Anker: zwei gleiche Anlagen rechnen die alte Projektreihe
        // =================================================================

        /// <summary>
        /// <b>Ergebnisgleichheit für den Bestand.</b> Zwei Anlagen mit denselben Werten
        /// ergeben dasselbe gewichtete Mittel wie die eine Projektangabe von früher —
        /// und damit denselben Zuschlag.
        /// </summary>
        [Fact]
        public void Zwei_gleiche_Anlagen_rechnen_die_alte_Projektreihe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuordnungZerstoeren();
            WirtschaftlichkeitErgebnis e = Rechne();

            Assert.True(Math.Abs(e.KwkgErloesJahr1 - ANKER_JAHR1_EUR) <= 0.01,
                        "Erwartet " + ANKER_JAHR1_EUR.ToString("F6") + " €, gemessen " +
                        e.KwkgErloesJahr1.ToString("F6") + " €.");

            // Und der Weg sagt von sich, dass er der Ersatzweg ist.
            Assert.Contains(Resource.WIRT_KWKG_ERSATZ_GEWICHTET.Substring(0, 30),
                            e.Hinweis ?? "");
        }

        // =================================================================
        // 2 — Die Gewichtung nach Leistung
        // =================================================================

        /// <summary>
        /// <b>Der Kern der Etappe.</b> Sätze 4 (50 kW) und 10 (9 kW) ergeben
        /// (50·4 + 9·10) / 59 = 4,915254 ct/kWh — nicht das arithmetische Mittel 7.
        /// </summary>
        [Fact]
        public void Verschiedene_Saetze_werden_nach_der_Leistung_gewichtet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuordnungZerstoeren();
            double anker = Rechne().KwkgErloesJahr1;      // beide 4,00 ct/kWh

            SatzEigen(ANLAGE_KLEIN, 10.0);
            double gemessen = Rechne().KwkgErloesJahr1;

            double erwartetSatz = (PEL_GROSS * SATZ_BESTAND + PEL_KLEIN * 10.0)
                                / (PEL_GROSS + PEL_KLEIN);
            Gleich(anker * erwartetSatz / SATZ_BESTAND, gemessen, "gewichtetes Mittel");

            // Gegenprobe: das arithmetische Mittel (7,00) wäre eine ANDERE Zahl.
            double arithmetisch = anker * ((SATZ_BESTAND + 10.0) / 2.0) / SATZ_BESTAND;
            Assert.True(Math.Abs(arithmetisch - gemessen) > 1.0,
                        "Gewichtung und arithmetisches Mittel sind nicht unterscheidbar.");
        }

        /// <summary>
        /// Ohne elektrische Nennleistung (G ≤ 0) gibt es <b>keine stille 0</b>: Gemittelt
        /// wird arithmetisch, und der Hinweis sagt es.
        /// </summary>
        [Fact]
        public void Ohne_Nennleistung_wird_arithmetisch_gemittelt_und_gemeldet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuordnungZerstoeren();
            double anker = Rechne().KwkgErloesJahr1;

            SatzEigen(ANLAGE_KLEIN, 10.0);
            // Die Vollbenutzungsstunden kommen aus dem gebuchten Lauf und bleiben
            // deshalb stehen, wenn die Nennleistung der Gerätezeilen verschwindet.
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_BHKW SET Pel = 0 WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));

            WirtschaftlichkeitErgebnis e = Rechne();
            Gleich(anker * ((SATZ_BESTAND + 10.0) / 2.0) / SATZ_BESTAND, e.KwkgErloesJahr1,
                   "arithmetisches Mittel");
            Assert.Contains(Resource.WIRT_KWKG_ERSATZ_OHNE_LEISTUNG.Substring(0, 30),
                            e.Hinweis ?? "");
        }

        /// <summary>
        /// <b>Eine Anlage ist ihre eigene Gesamtanlage.</b> Bleibt genau eine Anlage
        /// übrig, geht ihr Satz ungewichtet ein — nichts verdünnt ihn.
        /// </summary>
        [Fact]
        public void Eine_Anlage_ergibt_ihre_eigene_Reihe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuordnungZerstoeren();
            double anker = Rechne().KwkgErloesJahr1;

            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Energieanlagen WHERE ID = ?",
                                           new DbParam("@id", ANLAGE_KLEIN));
            SatzEigen(ANLAGE_GROSS, 10.0);

            Gleich(anker * 10.0 / SATZ_BESTAND, Rechne().KwkgErloesJahr1, "eine Anlage");
        }

        // =================================================================
        // 3 — Tatbestand und Deckel
        // =================================================================

        /// <summary>
        /// Der Tatbestand des § 6 Abs. 3 wirkt <b>je Anlage</b>: „keiner" an der kleinen
        /// Anlage nimmt nur IHREN Satz weg, der große bleibt stehen.
        /// </summary>
        [Fact]
        public void Tatbestand_keiner_nimmt_nur_den_Satz_seiner_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuordnungZerstoeren();
            double anker = Rechne().KwkgErloesJahr1;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Eigenstromfall = ? WHERE ID = ?",
                new DbParam("@f", DbWerte.KWKG_EIGENFALL_KEINER), new DbParam("@id", ANLAGE_KLEIN));

            double erwartetSatz = PEL_GROSS * SATZ_BESTAND / (PEL_GROSS + PEL_KLEIN);
            Gleich(anker * erwartetSatz / SATZ_BESTAND, Rechne().KwkgErloesJahr1,
                   "Tatbestand keiner an einer Anlage");
        }

        /// <summary>
        /// <b>Der Jahresdeckel mischt sich JE JAHR.</b> Trägt die große Anlage einen
        /// festen Deckel und die kleine die Staffel des § 8 Abs. 4, bleibt der
        /// Jahresverlauf erhalten — aber nur noch mit dem Gewichtsanteil der kleinen
        /// Anlage (9/59). Ein einmal gebildeter Mittelwert hätte ihn eingeebnet.
        /// </summary>
        [Fact]
        public void Fester_Deckel_und_Staffel_mischen_sich_jahresweise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuordnungZerstoeren();
            double[] ohne = Reihe();
            double stufeOhne = ohne[1] - ohne[2];
            Assert.True(stufeOhne > 1.0,
                        "Der Prüffall braucht eine Staffel, die zwischen Jahr 1 und 2 fällt.");

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Vbh_Jahresdeckel = 1000 WHERE ID = ?",
                new DbParam("@id", ANLAGE_GROSS));

            double[] mit = Reihe();
            double stufeMit = mit[1] - mit[2];
            Gleich(stufeOhne * PEL_KLEIN / (PEL_GROSS + PEL_KLEIN), stufeMit,
                   "Stufe der Staffel im gewichteten Deckel");
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        /// <summary>
        /// Verstellt die Modulnamen des gebuchten Laufs und hängt eine dritte Modulzeile
        /// an — danach paart <c>ModulJeAnlage</c> weder über den Namen noch über die
        /// Stellung, und der Ersatzweg greift.
        /// </summary>
        private static void ZuordnungZerstoeren()
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ErgebnisBHKWModul SET Modul = 'Ohne Zuordnung ' || ID " +
                "WHERE ID_ErgebnisBHKW IN (SELECT b.ID FROM Tab_ErgebnisBHKW AS b " +
                "INNER JOIN Tab_Ergebnis AS e ON e.ID = b.ID_Ergebnis WHERE e.ID_Projekt = ?)",
                new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ErgebnisBHKWModul (ID_ErgebnisBHKW, Modul, Waermeproduktion, " +
                "Stromproduktion, Brennstoff, Verbrauch) " +
                "SELECT b.ID, 'Ohne Zuordnung Zusatz', 0.0, 0.0, 'Gas', 0.0 " +
                "FROM Tab_ErgebnisBHKW AS b " +
                "INNER JOIN Tab_Ergebnis AS e ON e.ID = b.ID_Ergebnis WHERE e.ID_Projekt = ?",
                new DbParam("@p", PROJEKT));
        }

        private static void SatzEigen(int idAnlage, double ct)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Satz_Eigen = ? WHERE ID = ?",
                new DbParam("@s", ct), new DbParam("@id", idAnlage));
        }

        private static void Gleich(double erwartet, double gemessen, string was)
        {
            Assert.True(Math.Abs(erwartet - gemessen) <= 0.01,
                        was + ": erwartet " + erwartet.ToString("F6") + " €, gemessen " +
                        gemessen.ToString("F6") + " €.");
        }

        /// <summary>Die volle KWKG-Jahresreihe des Ersatzwegs [€], Index 1…T.</summary>
        private static double[] Reihe()
        {
            BerichtsDaten daten = Daten(out WirtschaftlichkeitParameter p);
            WirtschaftlichkeitVerlauf vl = new WirtschaftlichkeitCtrl().BerechneVerlauf(
                daten, p, Math.Max(1, p.Betrachtungszeitraum), WirtschaftlichkeitSzenario.ERWARTET);
            VerlaufSerie s = vl.Absolut.FirstOrDefault(x => x.IdProjekt == PROJEKT && x.Bild != null);
            Assert.NotNull(s);
            KapitalwertRechner.ErloesReihe r = s.Bild.ErloesReihen.FirstOrDefault(
                x => x.Name == KapitalwertRechner.ErloesReihe.KWKG);
            Assert.NotNull(r);
            return r.JeJahr;
        }

        private static WirtschaftlichkeitErgebnis Rechne()
        {
            BerichtsDaten daten = Daten(out WirtschaftlichkeitParameter p);
            List<WirtschaftlichkeitErgebnis> alle = new WirtschaftlichkeitCtrl().Berechne(daten, p);
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            return e;
        }

        private static BerichtsDaten Daten(out WirtschaftlichkeitParameter p)
        {
            p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall BK1a",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT),
                Zeitreihen = Stundenreihen()
            };
            KostenEmissionRechner.Berechne(v);

            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return daten;
        }

        /// <summary>Flache Stundenreihen aus den Jahressummen des gespeicherten Laufs —
        /// wortgleich zu <c>KwkAnlagenwahrheitTests.Stundenreihen</c> und aus demselben
        /// Grund: Ohne Stundenreihen ist der Eigen-/Einspeise-Split nicht
        /// bestimmbar.</summary>
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
