using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>§ 6.3 Nr. 30, die Kern-Regel und die Kohärenzzeile „Anlagenart
    /// fehlt"</b> (Entscheid E7‑Q1, Lesart b, 23.09.2026): „NULL ⇒ kein Zuschlag" greift
    /// nur dort, wo das Kontingent nach § 8 aus der Anlagenart abzuleiten ist. Ein
    /// gepflegtes Kontingent bleibt wirksam — so rechnet
    /// <c>KwkgKontingentRechner.Ableiten</c> schon (0 mit Grund); neu ist allein die
    /// Kohärenzzeile, genau in diesem Fall.
    ///
    /// <para><b>Der Prüfstand</b> ist Projekt 1030: Beide BHKW-Anlagen (14920 und 14921)
    /// tragen <c>KWKG_Anlagenart</c> NULL bei gepflegtem Kontingent 30.000 h. Die
    /// Testdaten-Pflege der Orchestrierung setzt ihre Anlagenart auf „neue Anlage"
    /// (<see cref="DbWerte.KWKG_ANLAGENART_NEU"/>); der letzte Fall hält fest, dass sich die
    /// Anker dadurch nicht bewegen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgAnlagenartFehltTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int ANLAGE_GROSS = 14920;
        private const string NAME_GROSS = "BHKW EW M 50 S [K] Erdgas";

        /// <summary>Die Anker (WirtschaftlichkeitAnkerTests) — Jahr 1 des Zuschlags und
        /// Kapitalwert im Szenario Erwartet.</summary>
        private const double ANKER_JAHR1_EUR = 7315.956634;
        private const double ANKER_KAPITALWERT_EUR = -21895377.28;

        /// <summary>Jahr 1 allein der 9-kW-Anlage (gemessen, Ankerweg).</summary>
        private const double JAHR1_KLEIN_EUR = 1116.031276;

        [Fact]
        public void Ein_gepflegtes_Kontingent_bleibt_wirksam_und_keine_Zeile_erscheint()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Die Lage vor der Testdaten-Pflege — unabhängig davon, ob die Testdatenbank sie
            // schon trägt: gepflegtes Kontingent, keine Anlagenart.
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Anlagenart = NULL WHERE ID IN (14920, 14921)");
            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT);

            Gleich(ANKER_JAHR1_EUR, e.KwkgErloesJahr1, "Jahr 1");
            Assert.Null(Zeile(e));
        }

        /// <summary>
        /// Weder Kontingent noch Anlagenart an der 50-kW-Anlage: Ihr Kontingent ist aus der
        /// Anlagenart abzuleiten, die fehlt — kein Zuschlag für sie, die Herleitung sagt
        /// es, und die Kohärenzzeile „Anlagenart fehlt" nennt sie (HINWEIS, ohne Betrag).
        /// Gemessen: Jahr 1 7.315,96 → 1.116,03 €, Kapitalwert −21.945.748,56 € (dieselben
        /// Zahlen wie vor E7c — neu ist allein die Zeile).
        /// </summary>
        [Fact]
        public void Ohne_Kontingent_und_ohne_Anlagenart_kein_Zuschlag_und_eine_Kohaerenzzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Setze(ANLAGE_GROSS, "KWKG_Vbh_Kontingent = NULL, KWKG_Anlagenart = NULL");
            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT);

            Gleich(JAHR1_KLEIN_EUR, e.KwkgErloesJahr1, "Jahr 1 ohne die 50-kW-Anlage");
            Assert.Contains(string.Format(Resource.WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART, NAME_GROSS),
                            e.Hinweis ?? "");

            KohaerenzHinweis h = Zeile(e);
            Assert.NotNull(h);
            Assert.Equal(KohaerenzSchwere.HINWEIS, h.Schwere);
            Assert.Null(h.Betrag);
            Assert.Equal(string.Format(Resource.KOH_KWKG_ANLAGENART_FEHLT, "„" + NAME_GROSS + "“"), h.Text);
        }

        /// <summary>
        /// Mit Anlagenart leitet § 8 das Kontingent ab (neue Anlage → 30.000 Vbh) — dieselbe
        /// Zahl wie das gepflegte, also derselbe Zuschlag, und keine Zeile.
        /// </summary>
        [Fact]
        public void Mit_Anlagenart_leitet_Paragraf_8_das_Kontingent_ab_und_keine_Zeile_erscheint()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Setze(ANLAGE_GROSS, "KWKG_Vbh_Kontingent = NULL, KWKG_Anlagenart = '" + DbWerte.KWKG_ANLAGENART_NEU + "'");
            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT);

            Gleich(ANKER_JAHR1_EUR, e.KwkgErloesJahr1, "Jahr 1");
            Assert.Null(Zeile(e));
        }

        /// <summary>
        /// Auf dem Ersatzweg geht das Kontingent der Anlage gewichtet in die Gesamtanlage
        /// ein — fehlt es samt Anlagenart, meldet die Kohärenzprüfung es ebenso.
        /// </summary>
        [Fact]
        public void Auch_der_Ersatzweg_meldet_die_fehlende_Anlagenart()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

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
            Setze(ANLAGE_GROSS, "KWKG_Vbh_Kontingent = NULL, KWKG_Anlagenart = NULL");

            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT);
            Assert.Contains(Resource.WIRT_KWKG_ERSATZ_GEWICHTET.Substring(0, 30), e.Hinweis ?? "");
            KohaerenzHinweis h = Zeile(e);
            Assert.NotNull(h);
            Assert.Contains("„" + NAME_GROSS + "“", h.Text);
        }

        /// <summary>
        /// Ein BHKW ohne KWKG-Sätze rechnet keinen Zuschlag und fragt deshalb auch nach
        /// keiner Anlagenart — Projekt 1024 (A-Tron 21, weder Satz noch Kontingent noch
        /// Anlagenart) bekommt keine Zeile.
        /// </summary>
        [Fact]
        public void Ohne_KWKG_Saetze_erscheint_keine_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(1024);
            Assert.Null(Zeile(e));
            Assert.Equal(0.0, e.KwkgErloesJahr1);
        }

        /// <summary>
        /// <b>Die Testdaten-Pflege der Orchestrierung</b> (§ 6.3 Nr. 30, Entscheid E7‑Q1):
        /// Die Anlagenart der beiden 1030-BHKW wird „neue Anlage" — die einzige Art, die
        /// ohne Kostenanteil zur Kontingentklasse 30.000 h führt (§ 8 Abs. 1), und beide
        /// liegen nach der elektrischen Leistung der Gerätezeile bei höchstens 50 kW. Die
        /// Anker bewegen sich nicht: Das Kontingent ist gepflegt, und der Satzvorschlag
        /// rechnete eine leere Anlagenart schon als neue Anlage.
        /// </summary>
        [Fact]
        public void Die_Testdaten_1030_mit_Anlagenart_lassen_die_Anker_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Anlagenart = ? WHERE ID IN (14920, 14921)",
                new DbParam("@a", DbParamTyp.VarWChar, 24) { Wert = DbWerte.KWKG_ANLAGENART_NEU });
            Assert.True(Zahl("SELECT MAX(b.Pel) FROM Tab_Energieanlagen AS a INNER JOIN Tab_BHKW AS b " +
                             "ON a.ID_BHKW = b.ID WHERE a.ID IN (14920, 14921)") <= 50);

            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT);
            Gleich(ANKER_JAHR1_EUR, e.KwkgErloesJahr1, "Jahr 1");
            Assert.NotNull(e.Kapitalwert);
            Assert.Equal(ANKER_KAPITALWERT_EUR, e.Kapitalwert.Value, 2);
            Assert.Null(Zeile(e));
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        private static KohaerenzHinweis Zeile(WirtschaftlichkeitErgebnis e)
            => (e.KohaerenzHinweise ?? new List<KohaerenzHinweis>())
                   .FirstOrDefault(h => h.Text.StartsWith("Anlagenart fehlt", StringComparison.Ordinal));

        private static void Setze(int idAnlage, string setze)
        {
            DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET " + setze + " WHERE ID = ?",
                                           new DbParam("@id", idAnlage));
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static void Gleich(double erwartet, double gemessen, string was)
        {
            Assert.True(Math.Abs(erwartet - gemessen) <= 0.01,
                        was + ": erwartet " + erwartet.ToString("F6") + " €, gemessen " +
                        gemessen.ToString("F6") + " €.");
        }

        /// <summary>Die Kette der Ankertests (<c>WirtschaftlichkeitAnkerTests.Rechne</c>).</summary>
        private static WirtschaftlichkeitErgebnis Rechne(int idProjekt)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(idProjekt);
            var v = new VariantenDaten
            {
                IdProjekt = idProjekt,
                IstStamm = true,
                Projektname = "Prüffall Nr. 30",
                Ergebnis = new ErgebnisCtrl().Load(idProjekt)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = idProjekt, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            WirtschaftlichkeitErgebnis e = new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == idProjekt);
            Assert.NotNull(e);
            return e;
        }
    }
}
