using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPOS.UI.Seiten.Berichte;
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

        /// <summary>Der Stamm der Vergleichsgruppe in der Testdatenbank: 1026 mit den Varianten
        /// 1027 „Andere WP" und 1029 „Erdwärme".</summary>
        private const int GRUPPE_STAMM = 1026;

        /// <summary>Die Grundmenge des Ausweises ohne Träger und ohne PV (7 + Zeitraum, Menge,
        /// Einspeisevergütung PV und KWK).</summary>
        private const int GRUNDMENGE = 11;

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
        // Das Kostenkapitel: Einzelzahl in der Tafel, Gruppenzahl in der Fußzeile
        // (Anwenderentscheid 29.09.2026)
        // =================================================================

        /// <summary>
        /// <b>Das Kostenkapitel des Berichts zeigt die EINZELZAHL, die Fußzeile nennt die
        /// Gruppenzahl.</b> Führt der Bericht Stamm und Variante, bleiben Kosten und Emissionen des
        /// Standes, wie Kostenseite und Übersicht der App sie zeigen; darunter sagt je eine Fußzeile,
        /// was der Stand mit bepreistem Netzbezug trüge, und nennt die Menge. Die Kostentafel führt
        /// die Energiekosten, die Emissionstafel das CO₂ — zwei verschiedene Sätze. Das Kapitel
        /// Wirtschaftlichkeit bleibt bei der Gruppenzahl.
        /// </summary>
        [Fact]
        public void Im_Bericht_zeigt_das_Kostenkapitel_die_Einzelzahl_mit_Fussnote()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            KennzahlenKatalog.Berechne(stamm);
            KennzahlenKatalog.Berechne(variante);
            double varianteVorher = variante.Energiekosten.Value;
            double co2Einzeln = stamm.CO2Gesamt.Value;
            Assert.Equal(GAS_EUR, stamm.Kennzahlen["ko.energie"].Value, 4);

            BerichtsDatenSammler.StromGruppenzahlErmitteln(daten);

            double gruppenzahl = GAS_EUR + NETZBEZUG * 1000.0 * STROMPREIS;

            // DER STAND BLEIBT BEI DER EINZELBETRACHTUNG — Zahl für Zahl unberührt.
            Assert.False(stamm.StromImVergleichBepreisen);
            Assert.Equal(GAS_EUR, stamm.Energiekosten.Value, 4);
            Assert.Equal(GAS_EUR, stamm.Kennzahlen["ko.energie"].Value, 4);
            Assert.Equal(co2Einzeln, stamm.CO2Gesamt.Value, 6);
            Assert.Equal(NETZBEZUG, stamm.StrombedarfOhneVerwendungMWh.Value, 2);
            Assert.Null(stamm.StromGruppenregelMWh);
            Assert.Equal(varianteVorher, variante.Energiekosten.Value, 4);

            // DANEBEN LIEGT DIE GRUPPENZAHL — allein für die Fußzeile.
            Assert.NotNull(stamm.Gruppenzahl);
            Assert.Equal(gruppenzahl, stamm.Gruppenzahl.EnergiekostenEuroJahr.Value, 2);
            Assert.Equal(NETZBEZUG, stamm.Gruppenzahl.NetzbezugMWh, 2);
            Assert.True(stamm.Gruppenzahl.CO2TonnenJahr.Value > co2Einzeln,
                        "CO₂ einzeln " + co2Einzeln + ", Gruppe " + stamm.Gruppenzahl.CO2TonnenJahr);
            Assert.Equal(new List<string> { "mit PV" }, stamm.Gruppenzahl.Verwender);
            Assert.Null(variante.Gruppenzahl);          // sie verwendet selbst Strom

            // DAS KAPITEL WIRTSCHAFTLICHKEIT BLEIBT BEI DER GRUPPENZAHL — die beiden Kapitel
            // weisen verschieden aus, und genau das sagt die Fußzeile.
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis s = Finde(ctrl.Berechne(daten, ctrl.LadeParameter(STAMM)),
                                                 STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(gruppenzahl, s.EnergiekostenJahr.Value, 2);
            Assert.NotEqual(stamm.Energiekosten.Value, s.EnergiekostenJahr.Value);

            // DIE KOSTENTAFEL: Einzelzahl in der Zelle, Gruppenzahl allein in der Fußzeile.
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Berichtstabelle kosten = Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_KOSTEN, false, de);
            Assert.False(kosten.IstLeer);
            Assert.Contains(kosten.Zeilen, z => z.Zellen.Count > 1 && z.Zellen[1].Zahl.HasValue &&
                                                Math.Abs(z.Zellen[1].Zahl.Value - GAS_EUR) < 0.01);
            Assert.DoesNotContain(kosten.Zeilen, z => z.Zellen.Count > 1 && z.Zellen[1].Zahl.HasValue &&
                                                     Math.Abs(z.Zellen[1].Zahl.Value - gruppenzahl) < 0.01);
            string satzKosten = Assert.Single(kosten.Hinweise);
            Assert.Contains("Gruppenregel", satzKosten);
            Assert.Contains("„Stamm“", satzKosten);
            Assert.Contains("„mit PV“", satzKosten);
            Assert.Contains(NETZBEZUG.ToString("N1", de) + " MWh/a", satzKosten);
            Assert.Contains(stamm.Gruppenzahl.EnergiekostenEuroJahr.Value.ToString("N0", de) + " €/a", satzKosten);

            // DIE EMISSIONSTAFEL trägt ihre eigene Fußzeile mit der CO₂-Zahl.
            Berichtstabelle emission = Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_EMISSION, false, de);
            Assert.False(emission.IstLeer);
            string satzEmission = Assert.Single(emission.Hinweise);
            Assert.NotEqual(satzKosten, satzEmission);
            Assert.Contains(stamm.Gruppenzahl.CO2TonnenJahr.Value.ToString("N1", de) + " t/a", satzEmission);

            // Keine andere Gruppe trägt eine Fußzeile; die Gesamttafel trägt beide.
            Assert.Empty(Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_ENERGIE, false, de).Hinweise);
            IReadOnlyList<string> gesamt = Berichtstabellen.Vergleichsgesamt(daten, false, de).Hinweise;
            Assert.Contains(satzKosten, gesamt);
            Assert.Contains(satzEmission, gesamt);

            // EXCEL: dieselben beiden Zeilen als Anmerkung unter dem Blatt „Vergleich".
            Berichtstabelle liste = Berichtstabellen.Vergleichsliste(daten, de);
            Assert.Contains(satzKosten, liste.Hinweise);
            Assert.Contains(satzEmission, liste.Hinweise);

            // Englisch aus der Ressource.
            var en = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            string englisch = Assert.Single(
                Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_KOSTEN, true, en).Hinweise);
            Assert.Contains("group rule", englisch);
            Assert.Contains("individual assessment", englisch);
        }

        /// <summary>
        /// <b>Im Wortbericht steht jede Fußzeile unter IHRER Tafel</b> — die der Emissionen unter der
        /// Emissionstafel, die der Kosten unter der Kostentafel (Katalogfolge: Emissionen vor Kosten),
        /// nicht beide gesammelt am Ende des Kapitels.
        /// </summary>
        [Fact]
        public void Im_Wortbericht_steht_jede_Fussnote_unter_ihrer_Tafel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            KennzahlenKatalog.Berechne(stamm);
            KennzahlenKatalog.Berechne(variante);
            BerichtsDatenSammler.StromGruppenzahlErmitteln(daten);
            Assert.NotNull(stamm.Gruppenzahl);

            var konfig = new BerichtsKonfiguration();
            konfig.AktiveBausteine.Add(BerichtsKonfiguration.B_VERGLEICH);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-gruppenregel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string docx = Path.Combine(ordner, "bericht.docx");
                new WordBerichtGenerator().Erzeuge(daten, konfig, docx, vorlage);
                List<string> zeilen = Berichtsstruktur.Word(docx);

                string satzKosten = stamm.StromGruppenregelFussnote(BerichtTexte.Kultur, KennzahlenKatalog.GR_KOSTEN);
                string satzEmission = stamm.StromGruppenregelFussnote(BerichtTexte.Kultur, KennzahlenKatalog.GR_EMISSION);
                int fussEmission = zeilen.FindIndex(z => z.Contains(satzEmission, StringComparison.Ordinal));
                int fussKosten = zeilen.FindIndex(z => z.Contains(satzKosten, StringComparison.Ordinal));
                Assert.True(fussEmission >= 0, "Die Fußzeile der Emissionen fehlt im Wortbericht.");
                Assert.True(fussKosten > fussEmission,
                            "Die Fußzeile der Kosten steht nicht nach der der Emissionen: " +
                            fussEmission + " / " + fussKosten);

                // Zwischen den beiden Fußzeilen steht die Kostentafel — sie sind also nicht
                // gesammelt hintereinander gesetzt, sondern je unter ihre Tafel.
                Assert.Contains(zeilen.GetRange(fussEmission + 1, fussKosten - fussEmission - 1),
                                z => z.StartsWith("Tabelle ", StringComparison.Ordinal));
            }
            finally { try { Directory.Delete(ordner, true); } catch (Exception) { } }
        }

        /// <summary>Ein Bericht mit einem Stand ist kein Vergleich — es gibt keine Gruppenzahl und
        /// unter keiner Tafel eine Fußzeile.</summary>
        [Fact]
        public void Ein_Bericht_mit_einem_Stand_behaelt_die_Einzelzahl_ohne_Fussnote()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            daten.Varianten.Remove(variante);
            KennzahlenKatalog.Berechne(stamm);

            BerichtsDatenSammler.StromGruppenzahlErmitteln(daten);

            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Assert.False(stamm.StromImVergleichBepreisen);
            Assert.Null(stamm.Gruppenzahl);
            Assert.Equal(GAS_EUR, stamm.Energiekosten.Value, 4);
            Assert.Equal(NETZBEZUG, stamm.StrombedarfOhneVerwendungMWh.Value, 2);
            Assert.Empty(daten.StromGruppenregelFussnoten(de, KennzahlenKatalog.GR_KOSTEN));
            Assert.Empty(daten.StromGruppenregelFussnoten(de, KennzahlenKatalog.GR_EMISSION));
            Assert.Empty(Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_KOSTEN, false, de).Hinweise);
            Assert.Empty(Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_EMISSION, false, de).Hinweise);
        }

        /// <summary>Verwendet kein Stand der Gruppe Strom, wirkt die Regel nicht — keine Gruppenzahl,
        /// keine Fußzeile.</summary>
        [Fact]
        public void Ohne_Stromverwendung_in_der_Gruppe_gibt_es_keine_Fussnote()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND (IFNULL(ID_WP, 0) > 0 " +
                "OR IFNULL(ID_PV, 0) > 0 OR IFNULL(ID_SP, 0) > 0 OR IFNULL(ID_BHKW, 0) > 0 " +
                "OR IFNULL(Heizstab, 0) <> 0)",
                new DbParam("@p", VARIANTE));

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out _);
            BerichtsDatenSammler.StromGruppenzahlErmitteln(daten);

            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Assert.False(stamm.StromImVergleichBepreisen);
            Assert.Null(stamm.Gruppenzahl);
            Assert.Equal(GAS_EUR, stamm.Energiekosten.Value, 4);
            Assert.Null(stamm.StromGruppenregelMWh);
            Assert.Empty(daten.StromGruppenregelFussnoten(de, KennzahlenKatalog.GR_KOSTEN));
        }

        /// <summary>Beide Fußzeilen stehen in beiden Ressourcendateien und nennen die Tafelzahl.</summary>
        [Fact]
        public void Die_Fussnoten_kommen_aus_den_Ressourcen()
        {
            foreach (string schluessel in new[] { VariantenDaten.SCHLUESSEL_FUSSNOTE_KOSTEN,
                                                  VariantenDaten.SCHLUESSEL_FUSSNOTE_EMISSION })
            {
                string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                    schluessel, new System.Globalization.CultureInfo("de-DE"));
                string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                    schluessel, new System.Globalization.CultureInfo("en-US"));
                Assert.False(string.IsNullOrEmpty(de), schluessel);
                Assert.False(string.IsNullOrEmpty(en), schluessel);
                Assert.NotEqual(de, en);
                // Vier Stellen: Stand, Verwender, Menge, Zahl der Tafel.
                foreach (string stelle in new[] { "{0}", "{1}", "{2}", "{3}" })
                {
                    Assert.Contains(stelle, de);
                    Assert.Contains(stelle, en);
                }
            }
            Assert.Equal(VariantenDaten.FUSSNOTE_KOSTEN,
                WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                    VariantenDaten.SCHLUESSEL_FUSSNOTE_KOSTEN, new System.Globalization.CultureInfo("de-DE")));
            Assert.Equal(VariantenDaten.FUSSNOTE_EMISSION,
                WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                    VariantenDaten.SCHLUESSEL_FUSSNOTE_EMISSION, new System.Globalization.CultureInfo("de-DE")));
        }

        // =================================================================
        // Die Szenarioabdeckung zählt je Lauf (Konzept § 2.11.5, § 3.5; Register EZ‑15)
        // =================================================================

        /// <summary>
        /// <b>Der Ausweis „n von m Parametern szenariert" zählt nach der Gruppenregel.</b> Allein
        /// zählt der Stamm ohne Stromverwendung keinen Stromträger. Im Lauf mit der Variante zählt er
        /// den Auslieferungsträger, der seinen Netzbezug bepreist — als Stromträger mit Arbeits-,
        /// Grund- und Leistungspreis, auch wenn dieser keinen Leistungspreis führt. Verwendet kein
        /// Stand des Laufs Strom, zählt keiner ihn.
        /// </summary>
        [Fact]
        public void Die_Szenarioabdeckung_zaehlt_den_Stromtraeger_nach_der_Gruppenregel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            DataRepository.ExecuteSQL("UPDATE energy_carrier SET price_power = ? WHERE id = ?",
                new DbParam("@l", 0.0), new DbParam("@c", STROM));

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(STAMM);
            var stamm = new KeyValuePair<int, string>(STAMM, "Stamm");
            var variante = new KeyValuePair<int, string>(VARIANTE, "mit PV");

            int allein = SzenarioAbdeckung.Lesen(p, new[] { stamm }).Parameter;
            int nurVariante = SzenarioAbdeckung.Lesen(p, new[] { variante }).Parameter;
            int lauf = SzenarioAbdeckung.Lesen(p, new[] { stamm, variante }).Parameter;

            // Die Grundmenge zählt je Ausweis einmal; der Stamm bringt im Lauf seinen Stromträger mit.
            Assert.Equal(allein + nurVariante - GRUNDMENGE + 3, lauf);

            // Gegenprobe: Die Variante verliert jeden stromverwendenden Erzeuger.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND (IFNULL(ID_WP, 0) > 0 " +
                "OR IFNULL(ID_PV, 0) > 0 OR IFNULL(ID_SP, 0) > 0 OR IFNULL(ID_BHKW, 0) > 0 " +
                "OR IFNULL(Heizstab, 0) <> 0)",
                new DbParam("@p", VARIANTE));
            int varianteOhne = SzenarioAbdeckung.Lesen(p, new[] { variante }).Parameter;
            Assert.Equal(allein + varianteOhne - GRUNDMENGE,
                         SzenarioAbdeckung.Lesen(p, new[] { stamm, variante }).Parameter);
        }

        /// <summary>
        /// <b>Die Szenarioabdeckung der Ergebnisseite zählt je Lauf</b> — Stamm, angehakte Varianten
        /// und Referenz, dieselbe Menge, über die der Lauf seine Gruppenregel bestimmt. In der Gruppe
        /// 1026 verwendet allein „Erdwärme" (1029) Strom; Stamm und „Andere WP" (1027) führen keinen
        /// Erzeuger, der Strom verwendet. Wird „Erdwärme" abgehakt, zählen die beiden übrigen keinen
        /// Stromträger mehr, und der Ausweis ändert sich mit dem Haken, ohne neues Laden.
        /// </summary>
        [Fact]
        public void Die_Szenarioabdeckung_der_Seite_folgt_dem_Haken_der_einzigen_Stromvariante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt IN (?, ?) AND (IFNULL(ID_WP, 0) > 0 " +
                "OR IFNULL(ID_PV, 0) > 0 OR IFNULL(ID_SP, 0) > 0 OR IFNULL(ID_BHKW, 0) > 0 " +
                "OR IFNULL(Heizstab, 0) <> 0)",
                new DbParam("@a", GRUPPE_STAMM), new DbParam("@b", STAMM));
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(GRUPPE_STAMM));
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(STAMM));
            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(VARIANTE));

            var seite = new WirtschaftlichkeitSeiteGaben(GRUPPE_STAMM, "Beispiel WP WG 1");
            IReadOnlyDictionary<string, object> gaben = seite.Gaben();
            WirtschaftlichkeitStand stand = ((Func<WirtschaftlichkeitStand>)gaben["Laden"])();
            var anzeigen = (Func<int, ErgebnisAnsicht>)gaben["Anzeigen"];
            var waehlen = (Action<IReadOnlyList<int>>)gaben["VergleichGewaehlt"];

            List<KeyValuePair<int, string>> alle = stand.Staende
                .Select(s => new KeyValuePair<int, string>(s.Id, s.Text)).ToList();
            Assert.Equal(new[] { GRUPPE_STAMM, STAMM, VARIANTE }.OrderBy(i => i),
                         alle.Select(s => s.Key).OrderBy(i => i));
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(GRUPPE_STAMM);

            // Alle angehakt: der Lauf der ganzen Gruppe mit der Gruppenregel.
            SzenarioAbdeckung mit = SzenarioAbdeckung.Lesen(p, alle);
            string beimLaden = stand.Ansicht.Szenarioabdeckung;
            Assert.Equal(mit.Satz(BerichtTexte.Kultur), beimLaden);

            // „Erdwärme" abgehakt: Stamm und „Andere WP" zählen keinen Stromträger mehr.
            waehlen(new List<int> { GRUPPE_STAMM, STAMM });
            SzenarioAbdeckung ohne = SzenarioAbdeckung.Lesen(p, alle.Where(s => s.Key != VARIANTE));
            string abgehakt = anzeigen(0).Szenarioabdeckung;
            Assert.Equal(ohne.Satz(BerichtTexte.Kultur), abgehakt);
            Assert.NotEqual(beimLaden, abgehakt);

            // Der Unterschied: die Parameter von „Erdwärme" und je Stand ohne eigene
            // Stromverwendung die drei Preise seines Stromträgers.
            int erdwaerme = SzenarioAbdeckung.Lesen(p, alle.Where(s => s.Key == VARIANTE)).Parameter - GRUNDMENGE;
            Assert.Equal(ohne.Parameter + erdwaerme + 2 * 3, mit.Parameter);

            // Wieder angehakt: derselbe Ausweis wie beim Laden.
            waehlen(new List<int> { GRUPPE_STAMM, STAMM, VARIANTE });
            Assert.Equal(beimLaden, anzeigen(0).Szenarioabdeckung);
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
