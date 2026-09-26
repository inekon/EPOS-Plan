using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gruppenregel „Strombedarf ohne Verwendung"</b>
    /// (<see cref="ProjektEnergietraegerCtrl.GruppeVerwendetStrom"/>,
    /// <see cref="WirtschaftlichkeitCtrl.StromGruppenregel"/>).
    ///
    /// <para>Die Regel je Stand lässt den Netzbezug eines Standes ohne stromverwendenden
    /// Erzeuger in Kosten und Emissionen außen vor. Im VERGLEICH stünde er dann ohne Preis
    /// neben dem bepreisten Reststrom einer Variante mit PV — die Variante, die Strom
    /// einspart, erschiene mit Mehrkosten. Verwendet ein Stand der Gruppe Strom, bepreisen
    /// deshalb im Vergleich alle Stände ihren Netzbezug; die Einzelbetrachtung bleibt je
    /// Stand.</para>
    ///
    /// <para>DER PRÜFSTAND: Stamm ist 1027 als reines Kesselprojekt (Wärmepumpe heraus,
    /// 16,12 MWh/a Netzbezug, Gas 1 MWh zu 0,50 €/Nm³ bei 10 kWh/Nm³ = 50 €/a). Die
    /// Variante trägt die Anlagen von 1029 (Wärmepumpe u. a. — sie verwendet Strom), ohne
    /// Kostenpositionen, und dasselbe Ergebnis mit 10 MWh/a weniger Netzbezug. Strom kostet
    /// über den Auslieferungsträger 0,35 €/kWh; keinem der beiden ist ein Stromträger
    /// zugeordnet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromGruppenregelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        public void Dispose() => _kultur.Dispose();

        private const int STAMM = 1027;
        private const int VARIANTE = 1029;
        private const int GAS = 63;
        private const int STROM = 60;
        private const double NETZBEZUG = 16.12;
        private const double EINSPARUNG = 10.0;
        private const double GAS_EUR = 50.0;
        private const double STROMPREIS = 0.35;

        // =================================================================
        // Die Gruppe verwendet Strom — der Stamm bepreist seinen Netzbezug
        // =================================================================

        [Fact]
        public void Im_Vergleich_bepreist_der_Stamm_ohne_Verwendung_seinen_Netzbezug()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out _);
            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> alle = ctrl.Berechne(daten, ctrl.LadeParameter(STAMM));

            WirtschaftlichkeitErgebnis s = Finde(alle, STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            WirtschaftlichkeitErgebnis v = Finde(alle, VARIANTE, WirtschaftlichkeitSzenario.ERWARTET);

            Assert.Null(s.Fehlgrund);
            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * STROMPREIS, s.EnergiekostenJahr.Value, 2);
            Assert.Equal(GAS_EUR + (NETZBEZUG - EINSPARUNG) * 1000.0 * STROMPREIS,
                         v.EnergiekostenJahr.Value, 2);

            // Die Variante spart Strom — und das zeigt ΔKW jetzt als Gewinn.
            Assert.True(v.KapitalwertDiff.HasValue);
            Assert.True(v.KapitalwertDiff.Value > 0, "ΔKW = " + v.KapitalwertDiff.Value);

            // Der Hinweis nennt Stand, Anlass und Menge; der Hinweis der Regel je Stand fehlt.
            Assert.NotNull(s.Hinweis);
            Assert.Contains("Gruppenregel", s.Hinweis);
            Assert.Contains("„Stamm“", s.Hinweis);
            Assert.Contains("Im Vergleich mit „mit PV“", s.Hinweis);
            Assert.Contains(NETZBEZUG.ToString("N1", BerichtTexte.Kultur), s.Hinweis);
            Assert.DoesNotContain("Energiekosten und Emissionen sind ohne diesen Strom bestimmt",
                                  s.Hinweis);

            // DIE EINZELBETRACHTUNG BLEIBT JE STAND: Das Original der Variante ist unberührt.
            Assert.Equal(GAS_EUR, stamm.Energiekosten.Value, 4);
            Assert.Equal(NETZBEZUG, stamm.StrombedarfOhneVerwendungMWh.Value, 2);
            Assert.False(stamm.StromImVergleichBepreisen);
            Assert.Null(stamm.StromGruppenregelMWh);
        }

        /// <summary>Alle drei Szenarien — und damit Bandbreite und Einstufung — rechnen mit
        /// derselben Gruppenregel.</summary>
        [Fact]
        public void Die_Gruppenregel_gilt_in_jedem_Szenario()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out _, out _);
            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> alle = ctrl.Berechne(daten, ctrl.LadeParameter(STAMM));

            foreach (string sz in WirtschaftlichkeitSzenario.Alle)
            {
                WirtschaftlichkeitErgebnis s = Finde(alle, STAMM, sz);
                Assert.True(s.EnergiekostenJahr.HasValue, sz);
                Assert.True(s.EnergiekostenJahr.Value > GAS_EUR + 1000.0, sz + ": " + s.EnergiekostenJahr);
                Assert.Contains("Gruppenregel", s.Hinweis ?? "");
            }
        }

        /// <summary>Der Verlauf nimmt dieselbe Regel: Die Stammlinie trägt den Strom, die
        /// Differenzlinie der Variante endet im Plus.</summary>
        [Fact]
        public void Der_Verlauf_folgt_der_Gruppenregel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out _, out _);
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(STAMM);
            WirtschaftlichkeitVerlauf verlauf =
                ctrl.BerechneVerlauf(daten, p, 10, WirtschaftlichkeitSzenario.ERWARTET);

            VerlaufSerie stammLinie = verlauf.Absolut.Find(x => x.IdProjekt == STAMM);
            Assert.NotNull(stammLinie);
            Assert.Null(stammLinie.Fehlgrund);
            Assert.True(stammLinie.Kumuliert[1] < -(GAS_EUR + 1000.0), "Jahr 1: " + stammLinie.Kumuliert[1]);

            VerlaufSerie diff = verlauf.Differenz.Find(x => x.IdProjekt == VARIANTE);
            Assert.NotNull(diff);
            Assert.True(diff.Kumuliert[diff.Kumuliert.Length - 1] > 0);
        }

        // =================================================================
        // Gegenproben
        // =================================================================

        /// <summary>Verwendet KEIN Stand Strom, bleibt es bei der Regel je Stand: Der Stamm
        /// rechnet nur das Gas, und der Hinweis „ohne Verwendung" steht.</summary>
        [Fact]
        public void Ohne_Stromverwendung_in_der_Gruppe_bleibt_alles_ohne_Strom()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            // Die Variante verliert jeden stromverwendenden Erzeuger.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND (IFNULL(ID_WP, 0) > 0 " +
                "OR IFNULL(ID_PV, 0) > 0 OR IFNULL(ID_SP, 0) > 0 OR IFNULL(ID_BHKW, 0) > 0 " +
                "OR IFNULL(Heizstab, 0) <> 0)",
                new DbParam("@p", VARIANTE));
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(VARIANTE));

            BerichtsDaten daten = Gruppe(out _, out _);
            Assert.Empty(WirtschaftlichkeitCtrl.StromGruppenregel(daten));

            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> alle = ctrl.Berechne(daten, ctrl.LadeParameter(STAMM));
            WirtschaftlichkeitErgebnis s = Finde(alle, STAMM, WirtschaftlichkeitSzenario.ERWARTET);

            Assert.Equal(GAS_EUR, s.EnergiekostenJahr.Value, 2);
            Assert.DoesNotContain("Gruppenregel", s.Hinweis ?? "");
            Assert.Contains("Strombedarf ohne Verwendung", s.Hinweis ?? "");
        }

        /// <summary>Ein Stand allein ist keine Gruppe — die Regel je Stand gilt.</summary>
        [Fact]
        public void Ein_Stand_allein_bleibt_bei_der_Regel_je_Stand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            daten.Varianten.Remove(variante);
            Assert.Empty(WirtschaftlichkeitCtrl.StromGruppenregel(daten));

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis s = Finde(ctrl.Berechne(daten, ctrl.LadeParameter(STAMM)),
                                                 STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(GAS_EUR, s.EnergiekostenJahr.Value, 2);
        }

        /// <summary>Die Regel selbst: Stände mit Stromverwendung werden benannt, alle
        /// übrigen bekommen die Namen der Verwender.</summary>
        [Fact]
        public void Die_Regel_benennt_Verwender_und_Betroffene()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            Assert.True(ProjektEnergietraegerCtrl.GruppeVerwendetStrom(
                new[] { STAMM, VARIANTE }, out List<int> verwender));
            Assert.Equal(new List<int> { VARIANTE }, verwender);
            Assert.False(ProjektEnergietraegerCtrl.GruppeVerwendetStrom(new[] { STAMM }, out _));

            Dictionary<int, List<string>> regel = WirtschaftlichkeitCtrl.StromGruppenregel(Gruppe(out _, out _));
            Assert.Single(regel);
            Assert.Equal(new List<string> { "mit PV" }, regel[STAMM]);
        }

        /// <summary>Der Hinweis steht in beiden Ressourcendateien.</summary>
        [Fact]
        public void Der_Hinweis_kommt_aus_der_Ressource()
        {
            string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                "WIRT_HINWEIS_STROM_GRUPPENREGEL", new System.Globalization.CultureInfo("de-DE"));
            string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                "WIRT_HINWEIS_STROM_GRUPPENREGEL", new System.Globalization.CultureInfo("en-US"));
            Assert.False(string.IsNullOrEmpty(de));
            Assert.False(string.IsNullOrEmpty(en));
            Assert.NotEqual(de, en);
            Assert.Equal(KostenEmissionRechner.HINWEIS_STROM_GRUPPENREGEL, de);
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        /// <summary>1027 als reines Kesselprojekt, 1029 ohne Kostenpositionen, beide mit
        /// rundem Gaspreis, dazu der Katalogpreis des Auslieferungs-Stromträgers.</summary>
        private static void Pruefstand()
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_WP > 0",
                new DbParam("@p", STAMM));
            DataRepository.ExecuteSQL("DELETE FROM Tab_ProjektWerte WHERE ProjektID IN (?, ?)",
                new DbParam("@a", STAMM), new DbParam("@b", VARIANTE));
            foreach (int id in new[] { STAMM, VARIANTE })
                DataRepository.ExecuteSQL(
                    "UPDATE energy_project_settings SET custom_price_work = ?, custom_hi = ?, " +
                    "custom_price_base = 0.0 WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@w", 0.5), new DbParam("@h", 10.0),
                    new DbParam("@p", id), new DbParam("@c", GAS));
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ErgebnisHeizkesselModul SET Verbrauch = ? " +
                "WHERE ID_ErgebnisHeizkessel IN (SELECT h.ID FROM Tab_ErgebnisHeizkessel AS h " +
                "INNER JOIN Tab_Ergebnis AS e ON h.ID_Ergebnis = e.ID WHERE e.ID_Projekt = ?)",
                new DbParam("@v", 1.0), new DbParam("@p", STAMM));
            DataRepository.ExecuteSQL("UPDATE energy_carrier SET price_work = ? WHERE id = ?",
                new DbParam("@p", STROMPREIS), new DbParam("@c", STROM));

            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(STAMM));
            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(VARIANTE));
        }

        /// <summary>Stamm 1027 mit seinem Lauf; Variante 1029 mit demselben Lauf, 10 MWh/a
        /// weniger Netzbezug — die Stromersparnis einer PV-Anlage.</summary>
        private static BerichtsDaten Gruppe(out VariantenDaten stamm, out VariantenDaten variante)
        {
            ErgebnisModel es = new ErgebnisCtrl().Load(STAMM);
            Assert.NotNull(es);
            Assert.Equal(NETZBEZUG, es.Energiebedarf.Stromrestbedarf, 2);
            stamm = new VariantenDaten { IdProjekt = STAMM, Ergebnis = es, IstStamm = true };
            KostenEmissionRechner.Berechne(stamm);

            ErgebnisModel ev = new ErgebnisCtrl().Load(STAMM);
            ev.Energiebedarf.Stromrestbedarf = NETZBEZUG - EINSPARUNG;
            variante = new VariantenDaten
            {
                IdProjekt = VARIANTE, Ergebnis = ev, Variantenname = "mit PV"
            };
            KostenEmissionRechner.Berechne(variante);

            var daten = new BerichtsDaten { IdStamm = STAMM };
            daten.Varianten.Add(stamm);
            daten.Varianten.Add(variante);
            return daten;
        }

        private static WirtschaftlichkeitErgebnis Finde(List<WirtschaftlichkeitErgebnis> alle,
                                                        int id, string szenario)
        {
            WirtschaftlichkeitErgebnis e = alle.Find(x => x.IdProjekt == id && x.Szenario == szenario);
            Assert.NotNull(e);
            return e;
        }
    }
}
