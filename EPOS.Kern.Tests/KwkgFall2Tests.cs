using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>der zweite Fall des § 2 Nr. 16 KWKG am ganzen Rechenweg</b>
    /// (Befund K‑1, Entscheide EZ‑5 und E7‑Q2 vom 23.09.2026): Trägt eine Anlage das
    /// Kennzeichen „Vorrichtung zur Abwärmeabfuhr", ist ihr KWK-Strom
    /// <c>min(Nettostromerzeugung, Nutzwärme × σ)</c>.
    ///
    /// <para><b>Der Prüfstand</b> ist Projekt 1030 („Referenz BHKW-Kaskade") mit seinen
    /// zwei BHKW-Anlagen (50 kW / P_th 81 kW und 9 kW / P_th 20,1 kW, Sätze 8/4 ct,
    /// Kontingent 30.000 h), gerechnet über den Ankerweg (gebuchtes Ergebnis ohne
    /// Stundenreihen: alles Eigenverbrauch) und — für die Einspeisung — mit flachen
    /// Stundenreihen, in denen das BHKW mehr erzeugt, als das Projekt braucht.</para>
    ///
    /// <para><b>Gemessen (E7c1, Scratchpad-Messung, Ankerweg):</b> ohne Kennzeichen
    /// Jahr 1 7.315,96 €, Kapitalwert −21.895.377,28 € (Anker, unverändert);
    /// σ = 0,5 gepflegt an der 50-kW-Anlage 6.137,94 € bzw. −21.904.948,06 €;
    /// σ = P_el ÷ P_th mit 100 MWh Wärmeüberschuss 6.448,21 € bzw. −21.902.427,26 €;
    /// ohne bestimmbare Kennzahl 1.116,03 € bzw. −21.945.748,56 €.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgFall2Tests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int ANLAGE_GROSS = 14920;      // BHKW EW M 50 S [K] Erdgas, Gerät 1018148
        private const int GERAET_GROSS = 1018148;
        private const string NAME_GROSS = "BHKW EW M 50 S [K] Erdgas";
        private const string NAME_KLEIN = "EC-POWER XRGI 9";
        private const double PEL_GROSS = 50.0, PTH_GROSS = 81.0, PEL_KLEIN = 9.0;

        /// <summary>Der Anker (WirtschaftlichkeitAnkerTests): Jahr 1 des Zuschlags ohne
        /// Kennzeichen [€].</summary>
        private const double ANKER_JAHR1_EUR = 7315.956634;

        // =================================================================
        //  Ohne Kennzeichen: Fall 1, alles wie vorher
        // =================================================================

        [Fact]
        public void Ohne_Kennzeichen_rechnet_Fall_1_und_der_Nachweis_bleibt_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(null);

            Gleich(ANKER_JAHR1_EUR, e.KwkgErloesJahr1, "Jahr 1 ohne Kennzeichen");
            Assert.Equal(2, e.KwkgModule.Count);
            Assert.All(e.KwkgModule, n =>
            {
                Assert.Null(n.Abwaermeabfuhr);
                Assert.Null(n.KwkStromMWh);
                Assert.Null(n.HerleitungKwkStrom);
            });
            Assert.DoesNotContain("§ 2 Nr. 16", e.Hinweis ?? "");
            Assert.DoesNotContain(e.KohaerenzHinweise ?? new List<KohaerenzHinweis>(),
                                  h => h.Text.StartsWith("Stromkennzahl fehlt", StringComparison.Ordinal));
        }

        // =================================================================
        //  σ gepflegt
        // =================================================================

        /// <summary>
        /// σ = 0,5 an der 50-kW-Anlage: KWK-Strom = min(373,78 ; 605,52 × 0,5) = 302,76 MWh,
        /// Kürzung 71,02 MWh. Ohne Stundenreihen ist alles Eigenverbrauch; die 9-kW-Anlage
        /// (ohne Kennzeichen) bleibt, wie sie war.
        /// <para><b>ETAPPE E7c2 — Entscheid E7c1‑Q2 b:</b> Die Vollbenutzungsstunden
        /// zählen aus dem KWK-Strom (302,76 MWh ÷ 50 kW = 6.055,2 h/a statt 7.475,69 h/a
        /// aus dem Modulstrom). Der Jahresdeckel der Staffel bindet, also wird je Jahr der
        /// gedeckelte KWK-Strom bezahlt — der Zuschlag der Anlage steht wieder auf
        /// 6.200,00 € (alt 5.021,91 €, im Verhältnis KWK-Strom / Netto gekürzt), Jahr 1
        /// gesamt <b>7.316,03 € (alt 6.137,94 €)</b>.</para>
        /// </summary>
        [Fact]
        public void Gepflegte_Stromkennzahl_kuerzt_auf_Nutzwaerme_mal_Sigma()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis ohne = Rechne(null);
            KwkgModulNachweis grossOhne = Modul(ohne, NAME_GROSS);
            KwkgModulNachweis kleinOhne = Modul(ohne, NAME_KLEIN);
            ErgebnisBHKWModulModel m = ErgebnisModul(NAME_GROSS);

            Kennzeichen(ANLAGE_GROSS, 0.5);
            WirtschaftlichkeitErgebnis mit = Rechne(null);
            KwkgModulNachweis gross = Modul(mit, NAME_GROSS);

            double netto = m.Stromproduktion;
            double kwk = Math.Min(netto, m.Waermeproduktion * 0.5);
            Assert.True(kwk < netto, "Der Prüffall braucht eine Kürzung.");

            Assert.True(gross.Abwaermeabfuhr);
            Assert.Equal(0.5, gross.Stromkennzahl.Value, 12);
            Assert.Equal(KwkStromRechner.HERKUNFT_GEPFLEGT, gross.StromkennzahlHerkunft);
            Assert.Equal(m.Waermeproduktion, gross.NutzwaermeMWh.Value, 9);
            Assert.Equal(kwk, gross.KwkStromMWh.Value, 9);
            Assert.Equal(netto - kwk, gross.KuerzungMWh.Value, 9);
            Assert.Equal(kwk, gross.EigenMWh, 9);                // ohne Stundenreihen: alles Eigen
            Assert.Equal(netto, gross.StromNettoMWh, 9);          // der Anteil bleibt physikalisch
            // E7c1-Q2 b: die Vbh aus dem KWK-Strom (alt: 7.475,69 h/a aus dem Modulstrom).
            Assert.Equal(kwk * 1000.0 / PEL_GROSS, gross.VbhElektrisch, 9);
            Gleich(6200.00, gross.Jahr1Eur, "Jahr 1 der 50-kW-Anlage");   // alt: 5.021,91 (= ohne × KWK/Netto)

            KwkgModulNachweis klein = Modul(mit, NAME_KLEIN);
            Assert.Null(klein.Abwaermeabfuhr);
            Gleich(kleinOhne.Jahr1Eur, klein.Jahr1Eur, "Jahr 1 der 9-kW-Anlage");
            Gleich(7316.031276, mit.KwkgErloesJahr1, "Jahr 1 gesamt (gemessen)");   // alt: 6.137,940960
            Assert.Contains("Vollbenutzungsstunden aus dem KWK-Strom 302,760 MWh ÷ 50 kW = 6.055 h/a",
                            mit.Hinweis ?? "");

            Assert.Contains("„" + NAME_GROSS + "“: Stromkennzahl σ 0,500 (gepflegt)", mit.Hinweis ?? "");
            Assert.Equal(gross.HerleitungKwkStrom, (mit.Hinweis ?? "").Split(" | ")
                             .First(z => z.StartsWith("KWKG § 2 Nr. 16 Fall 2", StringComparison.Ordinal)));
        }

        // =================================================================
        //  σ berechnet, Wärmeüberschuss nach P_el
        // =================================================================

        /// <summary>
        /// σ leer → P_el ÷ P_th = 50 ÷ 81; 100 MWh Wärmeüberschuss des Projekts gehen nur
        /// nach P_el auf die Module (50/59 an die große Anlage), die Wärme bleibt je Modul.
        /// Gemessen: Nutzwärme 520,774 MWh, KWK-Strom 321,466 MWh, Jahr 1 6.448,21 €.
        /// </summary>
        [Fact]
        public void Berechnete_Stromkennzahl_und_Waermeueberschuss_nach_Pel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ErgebnisBHKWModulModel m = ErgebnisModul(NAME_GROSS);
            Kennzeichen(ANLAGE_GROSS, null);
            Ueberschuss(100.0);
            WirtschaftlichkeitErgebnis e = Rechne(null);
            KwkgModulNachweis gross = Modul(e, NAME_GROSS);

            double sigma = PEL_GROSS / PTH_GROSS;
            double nutz = m.Waermeproduktion - 100.0 * PEL_GROSS / (PEL_GROSS + PEL_KLEIN);
            double kwk = Math.Min(m.Stromproduktion, nutz * sigma);

            Assert.Equal(KwkStromRechner.HERKUNFT_BERECHNET, gross.StromkennzahlHerkunft);
            Assert.Equal(sigma, gross.Stromkennzahl.Value, 12);
            Assert.Equal(nutz, gross.NutzwaermeMWh.Value, 9);
            Assert.Equal(kwk, gross.KwkStromMWh.Value, 9);
            Assert.Equal(520.774237, gross.NutzwaermeMWh.Value, 5);
            Assert.Equal(321.465579, gross.KwkStromMWh.Value, 5);
            Assert.Contains("berechnet aus P_el ÷ P_th der Gerätezeile = 50,0 kW ÷ 81,0 kW", e.Hinweis ?? "");
            Assert.Contains("Anteil am Wärmeüberschuss 84,746 MWh", e.Hinweis ?? "");

            // Die 9-kW-Anlage trägt kein Kennzeichen — ihr Anteil am Überschuss wirkt nicht.
            Assert.Null(Modul(e, NAME_KLEIN).Abwaermeabfuhr);
            // E7c1-Q2 b: Vbh aus dem KWK-Strom (321,466 MWh ÷ 50 kW = 6.429,3 h/a); der
            // Staffeldeckel bindet, der Zuschlag steht auf dem gedeckelten KWK-Strom.
            Assert.Equal(kwk * 1000.0 / PEL_GROSS, gross.VbhElektrisch, 9);
            Gleich(7316.031276, e.KwkgErloesJahr1, "Jahr 1 gesamt (gemessen)");   // alt: 6.448,212218
        }

        // =================================================================
        //  E7c1-Q1 a mit Hinweis: der Rundungsgrund winziger Kürzungen
        // =================================================================

        /// <summary>
        /// ETAPPE E7c2 — Entscheid E7c1‑Q1 (a mit Hinweis): Die Formel rechnet ohne
        /// Toleranz; mit σ = 50 ÷ 81 (berechnet) ist Nutzwärme × σ = 605,52 × 0,6173 =
        /// 373,778 MWh, der Nettostrom 373,78 MWh — die Kürzung 0,002 MWh bleibt stehen,
        /// und die Herleitung nennt ihren Grund, die Rundung.
        /// </summary>
        [Fact]
        public void Eine_Kuerzung_unter_einem_Hundertstel_nennt_den_Rundungsgrund()
        {
            Assert.Equal("", WirtschaftlichkeitCtrl.Rundungsgrund(0.0));
            Assert.Equal("", WirtschaftlichkeitCtrl.Rundungsgrund(0.01));
            Assert.Equal("", WirtschaftlichkeitCtrl.Rundungsgrund(71.02));
            Assert.Equal(" " + Resource.WIRT_KWKG_FALL2_RUNDUNG, WirtschaftlichkeitCtrl.Rundungsgrund(0.002));

            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kennzeichen(ANLAGE_GROSS, null);
            WirtschaftlichkeitErgebnis e = Rechne(null);
            KwkgModulNachweis gross = Modul(e, NAME_GROSS);

            Assert.True(gross.KuerzungMWh.Value > 0 && gross.KuerzungMWh.Value < 0.01,
                        "Der Prüffall braucht eine Kürzung unter 0,01 MWh.");
            Assert.EndsWith(Resource.WIRT_KWKG_FALL2_RUNDUNG, gross.HerleitungKwkStrom);
            Assert.Contains("Kürzung 0,002 MWh", gross.HerleitungKwkStrom);
        }

        // =================================================================
        //  Ohne bestimmbare Stromkennzahl: kein Zuschlag, Kohärenzzeile
        // =================================================================

        /// <summary>
        /// Kennzeichen gesetzt, σ leer, und die Gerätezeile führt kein P_th: keine Kennzahl,
        /// kein Ersatz — die Anlage bekommt keinen Zuschlag, die Herleitung sagt es, und die
        /// Kohärenzprüfung meldet „Stromkennzahl fehlt" als HINWEIS. Gemessen: Jahr 1
        /// 1.116,03 € (allein die 9-kW-Anlage).
        /// </summary>
        [Fact]
        public void Ohne_bestimmbare_Stromkennzahl_kein_Zuschlag_und_eine_Kohaerenzzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double kleinOhne = Modul(Rechne(null), NAME_KLEIN).Jahr1Eur;

            Kennzeichen(ANLAGE_GROSS, null);
            DataRepository.ExecuteNonQuery("UPDATE Tab_BHKW SET Ptherm = NULL WHERE ID = ?",
                                           new DbParam("@id", GERAET_GROSS));
            WirtschaftlichkeitErgebnis e = Rechne(null);

            Assert.DoesNotContain(e.KwkgModule, n => n.Bezeichner == NAME_GROSS);
            Gleich(kleinOhne, e.KwkgErloesJahr1, "Jahr 1 ohne die 50-kW-Anlage");
            Assert.Contains("„" + NAME_GROSS + "“: keine Stromkennzahl", e.Hinweis ?? "");

            KohaerenzHinweis h = Assert.Single(e.KohaerenzHinweise,
                                               x => x.Text.StartsWith("Stromkennzahl fehlt", StringComparison.Ordinal));
            Assert.Equal(KohaerenzSchwere.HINWEIS, h.Schwere);
            Assert.Null(h.Betrag);
            Assert.Contains("„" + NAME_GROSS + "“", h.Text);
            Assert.Equal(string.Format(Resource.KOH_KWKG_STROMKENNZAHL_FEHLT, "„" + NAME_GROSS + "“"), h.Text);
        }

        // =================================================================
        //  Die Kürzung geht zuerst von der Einspeisung ab
        // =================================================================

        /// <summary>
        /// Mit Stundenreihen, in denen das BHKW mehr erzeugt, als das Projekt braucht
        /// (Bedarf 30 kWh je Stunde): Die Kürzung 71,02 MWh geht allein von der
        /// Einspeisung der 50-kW-Anlage ab, ihr Eigenverbrauch bleibt. Mit σ = 0,2 reicht
        /// die Einspeisung nicht — der Rest geht vom Eigenverbrauch, und beide Mengen
        /// zusammen sind der KWK-Strom.
        /// </summary>
        [Fact]
        public void Die_Kuerzung_geht_zuerst_von_der_Einspeisung_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ErgebnisBHKWModulModel m = ErgebnisModul(NAME_GROSS);
            KwkgModulNachweis ohne = Modul(Rechne(MitEinspeisung()), NAME_GROSS);
            Assert.True(ohne.EinspeisungMWh > 100, "Der Prüffall braucht eine Einspeisung.");

            Kennzeichen(ANLAGE_GROSS, 0.5);
            KwkgModulNachweis mit = Modul(Rechne(MitEinspeisung()), NAME_GROSS);
            double kuerzung = m.Stromproduktion - m.Waermeproduktion * 0.5;

            Assert.Equal(kuerzung, mit.KuerzungMWh.Value, 9);
            Assert.Equal(ohne.EinspeisungMWh - kuerzung, mit.EinspeisungMWh, 9);
            Assert.Equal(ohne.EigenMWh, mit.EigenMWh, 9);

            Kennzeichen(ANLAGE_GROSS, 0.2);
            KwkgModulNachweis knapp = Modul(Rechne(MitEinspeisung()), NAME_GROSS);
            double kwk = m.Waermeproduktion * 0.2;
            Assert.Equal(0.0, knapp.EinspeisungMWh, 9);
            Assert.Equal(kwk, knapp.EigenMWh + knapp.EinspeisungMWh, 6);
        }

        // =================================================================
        //  Der Ersatzweg
        // =================================================================

        /// <summary>
        /// Lassen sich Anlagen und Module nicht zuordnen, verteilt der Ersatzweg die
        /// Nutzwärme des PROJEKTS (736,22 MWh) und die Nettostromerzeugung (432,3 MWh)
        /// nach P_el: an die 50-kW-Anlage 50/59. Mit σ = 0,5 ist ihr KWK-Strom
        /// min(366,356 ; 623,915 × 0,5) = 311,958 MWh, die Kürzung 54,398 MWh — ohne
        /// Stundenreihen mindert sie die Gesamtmenge, der Zuschlag sinkt im selben
        /// Verhältnis. Gemessen: Jahr 1 7.315,95 → 6.395,35 € (vor E7c1‑Q2 b); seit
        /// Q2 b zählen die Vbh aus dem KWK-Strom, der Deckel bindet — 7.316,00 €.
        /// </summary>
        [Fact]
        public void Der_Ersatzweg_verteilt_die_Nutzwaerme_des_Projekts_nach_Pel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ErgebnisBHKWModel bhkw = new ErgebnisCtrl().Load(PROJEKT).BHKW;
            ZuordnungZerstoeren();
            double ohne = Rechne(null).KwkgErloesJahr1;

            Kennzeichen(ANLAGE_GROSS, 0.5);
            WirtschaftlichkeitErgebnis e = Rechne(null);

            double anteil = PEL_GROSS / (PEL_GROSS + PEL_KLEIN);
            double netto = bhkw.Stromproduktion;
            double nutz = bhkw.Waermeproduktion - bhkw.Waermeueberschuss;
            double kuerzung = netto * anteil - Math.Min(netto * anteil, nutz * anteil * 0.5);

            // E7c1-Q2 b: Die Vbh der Gesamtanlage zählen aus ihrem KWK-Strom
            // ((432,3 − 54,398) MWh ÷ 59 kW = 6.405 h/a statt 7.327 h/a); der Deckel bindet,
            // Jahr 1 = 59 kW × Deckel × Satz — alt: ohne × (Netto − Kürzung) / Netto = 6.395,35 €.
            Assert.True(kuerzung > 0);
            Gleich(7316.00, e.KwkgErloesJahr1, "Jahr 1 auf dem Ersatzweg");
            Assert.True(e.KwkgErloesJahr1 >= ohne - 0.01);
            Assert.Contains(Resource.WIRT_KWKG_ERSATZ_GEWICHTET.Substring(0, 30), e.Hinweis ?? "");
            Assert.Contains("KWKG § 2 Nr. 16 Fall 2 auf dem Ersatzweg", e.Hinweis ?? "");
            Assert.Contains("Kürzung zusammen 54,398 MWh", e.Hinweis ?? "");
            Assert.Contains("Vollbenutzungsstunden aus dem KWK-Strom der Gesamtanlage 377,902 MWh ÷ 59 kW = 6.405 h/a",
                            e.Hinweis ?? "");
        }

        // =================================================================
        //  Der Schreibweg des Dialogs (Kern-Controller, keine Datenbank in der UI)
        // =================================================================

        /// <summary>
        /// <c>KwkgAnlagenCtrl</c> — der Weg, den die Hülle des BHKW-Dialogs geht: Er liest
        /// Kennzeichen, Stromkennzahl und P_th der Gerätezeile und schreibt die zwei
        /// Anlagenspalten im Dialogweg (<c>Speichere(g, true)</c>) mit; eine Kennzahl ≤ 0
        /// wird NULL. Der Bestandsweg mit acht Spalten (<c>Speichere(g)</c>) lässt sie
        /// stehen.
        /// </summary>
        [Fact]
        public void Der_Kern_Controller_liest_und_schreibt_Kennzeichen_und_Kennzahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new KwkgAnlagenCtrl();
            KwkgAnlagenAngabe g = ctrl.LadeGruppe(PROJEKT, "").Single(x => x.IdAnlage == ANLAGE_GROSS);
            Assert.False(g.Abwaermeabfuhr);
            Assert.Null(g.Stromkennzahl);
            Assert.Equal(PTH_GROSS, g.PthKW.Value, 9);

            g.Abwaermeabfuhr = true;
            g.Stromkennzahl = 0.55;
            Assert.True(ctrl.Speichere(g, true));
            KwkgAnlagenAngabe neu = ctrl.LadeGruppe(PROJEKT, "").Single(x => x.IdAnlage == ANLAGE_GROSS);
            Assert.True(neu.Abwaermeabfuhr);
            Assert.Equal(0.55, neu.Stromkennzahl.Value, 12);

            // Der Bestandsweg (acht E6-Spalten) fasst die zwei Spalten nicht an.
            neu.Abwaermeabfuhr = false;
            neu.Stromkennzahl = null;
            Assert.True(ctrl.Speichere(neu));
            KwkgAnlagenAngabe nachBestand = ctrl.LadeGruppe(PROJEKT, "").Single(x => x.IdAnlage == ANLAGE_GROSS);
            Assert.True(nachBestand.Abwaermeabfuhr);
            Assert.Equal(0.55, nachBestand.Stromkennzahl.Value, 12);

            // 0 ist „kein eigener Wert" und wird NULL.
            nachBestand.Stromkennzahl = 0;
            Assert.True(ctrl.Speichere(nachBestand, true));
            Assert.Null(ctrl.LadeGruppe(PROJEKT, "").Single(x => x.IdAnlage == ANLAGE_GROSS).Stromkennzahl);
        }

        // =================================================================
        //  Der Nachweisumschlag
        // =================================================================

        /// <summary>
        /// Ein Nachweis von Fall 1 schreibt KEINES der neuen Felder in den Umschlag
        /// (<c>WhenWritingNull</c>) — ein gebuchter Stand ohne Kennzeichen bleibt Zeichen
        /// für Zeichen, wie er war. Ein Nachweis von Fall 2 reist mit allen Feldern hin
        /// und zurück.
        /// </summary>
        [Fact]
        public void Der_Umschlag_fuehrt_Fall_2_nur_wo_er_gerechnet_wurde()
        {
            var fall1 = new WirtschaftlichkeitErgebnis();
            fall1.KwkgModule.Add(new KwkgModulNachweis { Bezeichner = "A", StromNettoMWh = 10 });
            string grund;
            string text1 = ErgebnisNachweisUmschlag.Schreiben(fall1, out grund);
            Assert.NotNull(text1);
            foreach (string feld in new[] { "Abwaermeabfuhr", "Stromkennzahl", "NutzwaermeMWh",
                                            "KwkStromMWh", "KuerzungMWh", "HerleitungKwkStrom" })
                Assert.DoesNotContain("\"" + feld + "\"", text1);

            var fall2 = new WirtschaftlichkeitErgebnis();
            fall2.KwkgModule.Add(new KwkgModulNachweis
            {
                Bezeichner = "B", Abwaermeabfuhr = true, Stromkennzahl = 0.5,
                StromkennzahlHerkunft = KwkStromRechner.HERKUNFT_GEPFLEGT, NutzwaermeMWh = 605.52,
                KwkStromMWh = 302.76, KuerzungMWh = 71.02, HerleitungKwkStrom = "Zeile"
            });
            ErgebnisNachweisUmschlag u = ErgebnisNachweisUmschlag.Lesen(
                ErgebnisNachweisUmschlag.Schreiben(fall2, out grund));
            KwkgModulNachweis n = Assert.Single(u.KwkgModule);
            Assert.True(n.Abwaermeabfuhr);
            Assert.Equal(0.5, n.Stromkennzahl);
            Assert.Equal(KwkStromRechner.HERKUNFT_GEPFLEGT, n.StromkennzahlHerkunft);
            Assert.Equal(302.76, n.KwkStromMWh);
            Assert.Equal(71.02, n.KuerzungMWh);
            Assert.Equal("Zeile", n.HerleitungKwkStrom);
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        private static void Kennzeichen(int idAnlage, double? sigma)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Abwaermeabfuhr = 1, KWKG_Stromkennzahl = ? WHERE ID = ?",
                new DbParam("@s", DbParamTyp.Double) { Wert = sigma.HasValue ? (object)sigma.Value : DBNull.Value },
                new DbParam("@id", idAnlage));
        }

        private static void Ueberschuss(double mwh)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ErgebnisBHKW SET Waermeueberschuss = ? WHERE ID_Ergebnis IN " +
                "(SELECT ID FROM Tab_Ergebnis WHERE ID_Projekt = ?)",
                new DbParam("@w", mwh), new DbParam("@p", PROJEKT));
        }

        /// <summary>Wortgleich zu <c>KwkgErsatzwegGewichtetTests.ZuordnungZerstoeren</c>.</summary>
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

        private static ErgebnisBHKWModulModel ErgebnisModul(string name)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            return erg.BHKW.Module.Single(x => x.Modul.Trim() == name);
        }

        private static KwkgModulNachweis Modul(WirtschaftlichkeitErgebnis e, string name)
            => e.KwkgModule.Single(n => n.Bezeichner == name);

        private static void Gleich(double erwartet, double gemessen, string was)
        {
            Assert.True(Math.Abs(erwartet - gemessen) <= 0.01,
                        was + ": erwartet " + erwartet.ToString("F6") + " €, gemessen " +
                        gemessen.ToString("F6") + " €.");
        }

        /// <summary>Die Kette der Ankertests (<c>WirtschaftlichkeitAnkerTests.Rechne</c>) —
        /// wahlweise mit Stundenreihen.</summary>
        private static WirtschaftlichkeitErgebnis Rechne(ZeitreihenSatz reihen)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall E7c",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT),
                Zeitreihen = reihen
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            WirtschaftlichkeitErgebnis e = new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            return e;
        }

        /// <summary>Flache Stundenreihen: BHKW 432,3 MWh/a gleichmäßig, Bedarf 30 kWh je
        /// Stunde — 262,8 MWh Eigenverbrauch, 169,5 MWh Einspeisung.</summary>
        private static ZeitreihenSatz MitEinspeisung()
        {
            int n = ZeitreihenSatz.Stunden;
            var bedarf = new double[n];
            var bhkw = new double[n];
            var bezug = new double[n];
            double bhkwKWh = 432.3 * 1000.0 / n;
            for (int h = 0; h < n; h++)
            {
                bedarf[h] = 30.0;
                bhkw[h] = bhkwKWh;
                bezug[h] = 0.0;
            }
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = bedarf;
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = bhkw;
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = bezug;
            return z;
        }
    }
}
