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
    ///
    /// <para><b>Der Leistungspreis</b> (Anwenderentscheid 29.09.2026, Register EZ‑17): Den
    /// Netzbezug des Standes ohne stromverwendenden Erzeuger bepreist der Vergleich mit Arbeits-
    /// und Grundpreis, den Leistungspreis des Trägers setzt er nicht an — bei einem solchen Stand
    /// ist er eine Größe der Lastoptimierung und wird benannt. Die Variante mit Stromverwendung
    /// trägt ihren Leistungspreis unverändert. Dasselbe gilt im Rollentarif für den Leistungspreis des
    /// Reststromtarifs (Anwenderentscheid 30.09.2026, Register EZ‑18).</para>
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
        /// den Auslieferungsträger, der seinen Netzbezug bepreist — mit Arbeits- und Grundpreis; den
        /// Leistungspreis setzt ein Stand ohne stromverwendenden Erzeuger nicht an (Register EZ‑17),
        /// er zählt deshalb nicht, auch nicht, wenn der Träger einen führt. Verwendet kein Stand des
        /// Laufs Strom, zählt keiner ihn.
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

            // Hergeleitet: der Stamm allein mit Erdgas E (Arbeits- und Grundpreis); die Variante mit
            // Erdgas E, ihrem Stromträger — dem Rückfallträger ohne Zuordnung, Arbeits-, Grund- und
            // Leistungspreis (EZ‑18) — und DV-Entgelt und PPA-Preis ihrer PV-Vergütung.
            Assert.Equal(GRUNDMENGE + 2, allein);
            Assert.Equal(GRUNDMENGE + 2 + 3 + 2, nurVariante);

            // Die Grundmenge zählt je Ausweis einmal; der Stamm bringt im Lauf seinen Stromträger mit
            // — Arbeits- und Grundpreis, keinen Leistungspreis.
            Assert.Equal(allein + nurVariante - GRUNDMENGE + 2, lauf);

            // Den Leistungspreis zählt die Variante mit Wärmepumpe immer: Ihr Stromträger ist der
            // Rückfallträger, der ihren Netzbezug bepreist (Register EZ‑18) — ein gepflegter
            // Leistungspreis ändert m nicht. Der Stamm ohne Stromverwendung zählt ihn nie (EZ‑17).
            DataRepository.ExecuteSQL("UPDATE energy_carrier SET price_power = ? WHERE id = ?",
                new DbParam("@l", 60.0), new DbParam("@c", STROM));
            int alleinMit = SzenarioAbdeckung.Lesen(p, new[] { stamm }).Parameter;
            int nurVarianteMit = SzenarioAbdeckung.Lesen(p, new[] { variante }).Parameter;
            Assert.Equal(allein, alleinMit);
            Assert.Equal(nurVariante, nurVarianteMit);
            Assert.Equal(alleinMit + nurVarianteMit - GRUNDMENGE + 2,
                         SzenarioAbdeckung.Lesen(p, new[] { stamm, variante }).Parameter);

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
            // Stromverwendung Arbeits- und Grundpreis seines Stromträgers (EZ‑17: kein Leistungspreis).
            int erdwaerme = SzenarioAbdeckung.Lesen(p, alle.Where(s => s.Key == VARIANTE)).Parameter - GRUNDMENGE;
            Assert.Equal(ohne.Parameter + erdwaerme + 2 * 2, mit.Parameter);

            // Wieder angehakt: derselbe Ausweis wie beim Laden.
            waehlen(new List<int> { GRUPPE_STAMM, STAMM, VARIANTE });
            Assert.Equal(beimLaden, anzeigen(0).Szenarioabdeckung);
        }

        // =================================================================
        // Der Leistungspreis nur bei Stromverwendung (Anwenderentscheid 29.09.2026, EZ‑17)
        // =================================================================

        /// <summary>
        /// <b>Die Kopie der Gruppenregel trägt Arbeits- und Grundpreis, keinen Leistungspreis.</b>
        /// Der Auslieferungsträger führt 60 €/(kW·a) und 120 €/a Grundpreis; beide Stände haben eine
        /// Bezugsspitze von 40 kW. Der Stamm ohne Stromverwendung: 50 + 16,12 MWh × 0,35 €/kWh + 120
        /// = 5.812,00 €/a — ohne 40 kW × 60 €/(kW·a) = 2.400 €/a; sein Hinweis nennt Satz und Träger.
        /// Die Variante mit Wärmepumpe: 50 + 6,12 MWh × 0,35 €/kWh + 120 + 2.400 = 4.712,00 €/a — ihr
        /// Leistungspreis bleibt, und kein Hinweis.
        /// </summary>
        [Fact]
        public void Im_Vergleich_setzt_der_Stand_ohne_Verwendung_keinen_Leistungspreis_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            Katalogleistungspreis(60.0, 120.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            stamm.Zeitreihen = Spitze(40.0, 30.0);
            variante.Zeitreihen = Spitze(40.0, 30.0);
            KostenEmissionRechner.Berechne(stamm);
            KostenEmissionRechner.Berechne(variante);

            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> alle = ctrl.Berechne(daten, ctrl.LadeParameter(STAMM));
            WirtschaftlichkeitErgebnis s = Finde(alle, STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            WirtschaftlichkeitErgebnis v = Finde(alle, VARIANTE, WirtschaftlichkeitSzenario.ERWARTET);

            Assert.Null(s.Fehlgrund);
            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * STROMPREIS + 120.0, s.EnergiekostenJahr.Value, 2);
            Assert.Equal(GAS_EUR + (NETZBEZUG - EINSPARUNG) * 1000.0 * STROMPREIS + 120.0 + 40.0 * 60.0,
                         v.EnergiekostenJahr.Value, 2);

            // Der Hinweis steht beim Stamm neben dem der Gruppenregel — in jedem Szenario.
            string hinweis = "Leistungspreis 60,00 €/(kW·a) des Stromträgers „Elektrische Energie“ " +
                             "nicht angesetzt: Der Stand führt keinen Erzeuger, der Strom verwendet";
            foreach (string sz in WirtschaftlichkeitSzenario.Alle)
            {
                WirtschaftlichkeitErgebnis stand = Finde(alle, STAMM, sz);
                Assert.Contains("Gruppenregel", stand.Hinweis ?? "");
                Assert.Contains(hinweis, stand.Hinweis ?? "");
            }
            Assert.DoesNotContain("nicht angesetzt", v.Hinweis ?? "");

            // Das Original des Stamms (Einzelbetrachtung) bleibt je Stand und nennt nichts.
            Assert.Equal(GAS_EUR, stamm.Energiekosten.Value, 4);
            Assert.Null(stamm.LeistungspreisNichtAngesetzt);
            Assert.Equal(40.0 * 60.0, variante.EnergieLeistungsanteil.Value, 2);
        }

        /// <summary>
        /// <b>Im Bericht meldet die Gruppenregel den Leistungspreis als Hinweis</b> — in der
        /// Hinweisliste des Berichtslaufs, wo der Sammler auch den Leistungspreis ohne Bezugsspitze
        /// meldet. Die Gruppenzahl der Fußzeile trägt Arbeits- und Grundpreis, keinen Leistungspreis;
        /// der Stand selbst behält seine Einzelzahl. Eine Warnung „ohne Bezugsspitze" entsteht nicht —
        /// der Stand ohne Stromverwendung setzt den Leistungspreis gar nicht an. Die Variante mit
        /// Stromverwendung ruft ihn weiter ab.
        /// </summary>
        [Fact]
        public void Im_Bericht_meldet_die_Gruppenregel_den_Leistungspreis_als_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            Katalogleistungspreis(60.0, 0.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            BerichtsDatenSammler.StromGruppenzahlErmitteln(daten);

            // Der Stand bleibt die Einzelzahl; die Gruppenzahl der Fußzeile ohne Leistungspreis.
            Assert.False(stamm.StromImVergleichBepreisen);
            Assert.Equal(GAS_EUR, stamm.Energiekosten.Value, 4);
            Assert.Null(stamm.LeistungspreisNichtAngesetzt);
            Assert.Null(stamm.LeistungspreisOhneSpitze);
            Assert.NotNull(stamm.Gruppenzahl);
            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * STROMPREIS,
                         stamm.Gruppenzahl.EnergiekostenEuroJahr.Value, 2);

            Berichtshinweis h = Assert.Single(daten.Hinweisliste, x => x.Text.StartsWith(
                "Leistungspreis 60,00 €/(kW·a) des Stromträgers „Elektrische Energie“ nicht angesetzt",
                StringComparison.Ordinal));
            Assert.Equal(Berichtshinweisstufe.Hinweis, h.Stufe);
            Assert.Equal(stamm.Anzeige, h.Stand);
            Assert.Contains(daten.Warnungen, w => w.EndsWith(h.Text, StringComparison.Ordinal));
            Assert.DoesNotContain(daten.Warnungen, w => w.Contains("Bezugsspitze"));

            // Die Variante mit Stromverwendung: unverändert — ohne Zeitreihen fehlt ihr die Spitze.
            Assert.Null(variante.Gruppenzahl);
            Assert.Null(variante.LeistungspreisNichtAngesetzt);
            Assert.Equal("Elektrische Energie", variante.LeistungspreisOhneSpitze);
        }

        /// <summary>
        /// <b>Gleich, ob der Träger zugeordnet ist:</b> Dem Stamm ist der Stromträger zugeordnet
        /// und an ihm eine Staffel gepflegt (bis 1.500 kW 60 €/(kW·a), darüber 90 €/(kW·a)). Bei
        /// 2.000 kW Bezugsspitze trüge sie 1.500 × 60 + 500 × 90 = 135.000 €/a — der Vergleich setzt
        /// sie nicht an, und der Hinweis nennt die Staffel; ein Rückfall ist nicht vermerkt.
        /// </summary>
        [Fact]
        public void Der_zugeordnete_Stromtraeger_setzt_seine_Staffel_ebenso_nicht_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            Assert.True(new WizardCtrl().TraegerSatzAnlegen(STAMM, STROM));
            DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_work = ?, custom_price_base = ?, " +
                "custom_price_power = ? WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@w", STROMPREIS), new DbParam("@g", 0.0), new DbParam("@l", 0.0),
                new DbParam("@p", STAMM), new DbParam("@c", STROM));
            Assert.True(EnergietraegerPreisCtrl.StaffelSchreiben(STAMM, STROM,
                new LeistungspreisStaffel { GrenzeKW = 1500.0, Preis1EurKWa = 60.0, Preis2EurKWa = 90.0 }));
            Assert.Equal(STROM, Emissionsquelle.StromTraeger(STAMM));
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(STAMM));

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out _);
            stamm.Zeitreihen = Spitze(2000.0, 1800.0);
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis s = Finde(ctrl.Berechne(daten, ctrl.LadeParameter(STAMM)),
                                                 STAMM, WirtschaftlichkeitSzenario.ERWARTET);

            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * STROMPREIS, s.EnergiekostenJahr.Value, 2);
            Assert.Contains("Leistungspreis 60,00 €/(kW·a) bis 1.500 kW, darüber 90,00 €/(kW·a) " +
                            "des Stromträgers „Elektrische Energie“ nicht angesetzt", s.Hinweis ?? "");
            Assert.DoesNotContain("dem Projekt ist kein Stromträger zugeordnet", s.Hinweis ?? "");
        }

        /// <summary>
        /// Der Hinweis nennt den Satz je Monat in seiner Einheit; die Gruppenzahl trägt trotz
        /// 12 × 30 kW Monatsspitze keinen Anteil. Führt der Träger keinen Leistungspreis, entsteht
        /// kein Hinweis — die Zahl ist dieselbe.
        /// </summary>
        [Fact]
        public void Der_Hinweis_nennt_den_Satz_je_Monat_und_ohne_Leistungspreis_keinen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            Katalogleistungspreis(5.0, 0.0, DbWerte.LEISTUNGSPREIS_MODUS_MONAT);

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out _);
            stamm.Zeitreihen = Spitze(40.0, 30.0);
            BerichtsDatenSammler.StromGruppenzahlErmitteln(daten);
            Assert.Single(daten.Hinweisliste, x => x.Text.StartsWith(
                "Leistungspreis 5,00 €/(kW·Monat) des Stromträgers", StringComparison.Ordinal));
            double mitSatz = stamm.Gruppenzahl.EnergiekostenEuroJahr.Value;
            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * STROMPREIS, mitSatz, 2);

            // Ohne Leistungspreis am Träger: dieselbe Gruppenzahl, kein Hinweis.
            Katalogleistungspreis(0.0, 0.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);
            BerichtsDaten ohne = Gruppe(out VariantenDaten stammOhne, out _);
            stammOhne.Zeitreihen = Spitze(40.0, 30.0);
            BerichtsDatenSammler.StromGruppenzahlErmitteln(ohne);
            Assert.Equal(mitSatz, stammOhne.Gruppenzahl.EnergiekostenEuroJahr.Value, 6);
            Assert.DoesNotContain(ohne.Hinweisliste, x => x.Text.Contains("nicht angesetzt"));

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis s = Finde(ctrl.Berechne(ohne, ctrl.LadeParameter(STAMM)),
                                                 STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Contains("Gruppenregel", s.Hinweis ?? "");
            Assert.DoesNotContain("nicht angesetzt", s.Hinweis ?? "");
        }

        /// <summary>Hinweis und Sätze stehen in beiden Ressourcendateien; der deutsche Rückfall ist
        /// der Wortlaut der Ressource.</summary>
        [Theory]
        [InlineData("WIRT_HINWEIS_LEISTUNGSPREIS_NICHT_ANGESETZT")]
        [InlineData("WIRT_HINWEIS_LEISTUNGSPREIS_TARIF_NICHT_ANGESETZT")]
        [InlineData("WIRT_LP_SATZ_STAFFEL")]
        [InlineData("WIRT_LP_SATZ_SAISON")]
        [InlineData("WIRT_LP_SATZ_MONAT")]
        public void Der_Leistungspreishinweis_kommt_aus_der_Ressource(string schluessel)
        {
            string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                schluessel, new System.Globalization.CultureInfo("de-DE"));
            string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                schluessel, new System.Globalization.CultureInfo("en-US"));
            Assert.False(string.IsNullOrEmpty(de));
            Assert.False(string.IsNullOrEmpty(en));
            Assert.NotEqual(de, en);
            if (schluessel == "WIRT_HINWEIS_LEISTUNGSPREIS_NICHT_ANGESETZT")
                Assert.Equal(KostenEmissionRechner.HINWEIS_LEISTUNGSPREIS_NICHT_ANGESETZT, de);
            if (schluessel == "WIRT_HINWEIS_LEISTUNGSPREIS_TARIF_NICHT_ANGESETZT")
                Assert.Equal(WirtschaftlichkeitCtrl.HINWEIS_LEISTUNGSPREIS_TARIF_NICHT_ANGESETZT, de);
        }

        // =================================================================
        // Der Rollentarif unter EZ‑17 (Anwenderentscheid 30.09.2026, EZ‑18)
        // =================================================================

        /// <summary>
        /// <b>Der Rollentarif setzt an der Kopie der Gruppenregel keinen Leistungspreis an.</b> Die
        /// Tarifstruktur ist aktiv: Bezug 0,30 €/kWh und 8 €/(kW·Monat), Reststrom 0,32 €/kWh, 100 €/a
        /// und 10 €/(kW·Monat), beide monatlich. Beide Stände führen Stundenreihen mit gleichbleibendem
        /// Netzbezug (Bedarf = Netzbezug), der Stromträger dazu einen Leistungspreis von 60 €/(kW·a).
        /// Der Stamm ohne Stromverwendung: 50 + 16,12 MWh × 0,32 €/kWh + 100 = 5.308,40 €/a — ohne
        /// 12 × 1,8402 kW × 10 = 220,82 €/a Leistungsanteil; die vermiedenen Kosten tragen keinen
        /// Leistungsanteil (16,12 MWh × (0,30 − 0,32) €/kWh − 100 = −422,40 €/a), und der Hinweis nennt
        /// das Modell des Reststromtarifs statt des Stromträgers, dessen Preise der Tarif ersetzt. Die
        /// Variante mit Wärmepumpe: 50 + 6,12 MWh × 0,32 + 100 + 12 × 0,6986 kW × 10 = 2.192,24 €/a —
        /// ihr Leistungsanteil bleibt (vermieden 12 × 0,6986 × (8 − 10) = −16,77 €/a), kein Hinweis.
        /// </summary>
        [Fact]
        public void Im_Rollentarif_setzt_der_Stand_ohne_Verwendung_keinen_Leistungspreis_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            Katalogleistungspreis(60.0, 0.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);
            Rollentarif(DbWerte.LEISTUNGSMODELL_MONATLICH, 8.0, 10.0);

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            stamm.Zeitreihen = Stundenreihen(NETZBEZUG);
            variante.Zeitreihen = Stundenreihen(NETZBEZUG - EINSPARUNG);
            KostenEmissionRechner.Berechne(stamm);
            KostenEmissionRechner.Berechne(variante);

            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> alle = ctrl.Berechne(daten, ctrl.LadeParameter(STAMM));
            WirtschaftlichkeitErgebnis s = Finde(alle, STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            WirtschaftlichkeitErgebnis v = Finde(alle, VARIANTE, WirtschaftlichkeitSzenario.ERWARTET);

            double stammKW = NETZBEZUG * 1000.0 / ZeitreihenSatz.Stunden;
            double varianteKW = (NETZBEZUG - EINSPARUNG) * 1000.0 / ZeitreihenSatz.Stunden;

            // Der Stamm: Arbeits- und Grundpreis des Reststromtarifs, kein Leistungsanteil.
            Assert.Null(s.Fehlgrund);
            Assert.Equal(NETZBEZUG * 1000.0 * 0.32 + 100.0, s.StromkostenTarif.Value, 2);
            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * 0.32 + 100.0, s.EnergiekostenJahr.Value, 2);
            Assert.Equal(0.0, s.VermiedenLeistungJahr, 6);
            Assert.Equal(NETZBEZUG * 1000.0 * (0.30 - 0.32) - 100.0, s.VermiedenGesamtJahr, 2);

            // Der Hinweis nennt das Modell des Reststromtarifs — in jedem Szenario —, nicht den Träger.
            string hinweis = "Leistungspreis des Reststromtarifs nach dem Modell „" +
                             WindowsFormsApplication1.MyResource.Resource.TARIF_LM_MONATLICH +
                             "“ nicht angesetzt: Der Stand führt keinen Erzeuger, der Strom verwendet";
            foreach (string sz in WirtschaftlichkeitSzenario.Alle)
            {
                WirtschaftlichkeitErgebnis stand = Finde(alle, STAMM, sz);
                Assert.Contains("Gruppenregel", stand.Hinweis ?? "");
                Assert.Contains(hinweis, stand.Hinweis ?? "");
                Assert.DoesNotContain("des Stromträgers „Elektrische Energie“ nicht angesetzt", stand.Hinweis ?? "");
            }

            // Die Variante mit Stromverwendung rechnet unverändert mit Leistungsanteil.
            Assert.Null(v.Fehlgrund);
            Assert.Equal((NETZBEZUG - EINSPARUNG) * 1000.0 * 0.32 + 100.0 + 12.0 * varianteKW * 10.0,
                         v.StromkostenTarif.Value, 2);
            Assert.Equal(GAS_EUR + (NETZBEZUG - EINSPARUNG) * 1000.0 * 0.32 + 100.0 + 12.0 * varianteKW * 10.0,
                         v.EnergiekostenJahr.Value, 2);
            Assert.Equal(12.0 * varianteKW * (8.0 - 10.0), v.VermiedenLeistungJahr, 2);
            Assert.DoesNotContain("nicht angesetzt", v.Hinweis ?? "");

            // Gegenprobe: Mit Leistungsanteil trüge der Stamm 220,82 €/a mehr.
            Assert.Equal(220.82, 12.0 * stammKW * 10.0, 2);
        }

        /// <summary>
        /// Der Hinweis nennt das Modell des Reststromtarifs im Klartext der Tarifstruktur — hier
        /// die Staffel. Führt der Reststromtarif keinen Leistungspreis, steht kein Hinweis und die
        /// Zahl ist dieselbe.
        /// </summary>
        [Fact]
        public void Der_Tarifhinweis_nennt_das_Modell_und_ohne_Leistungspreis_keinen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Pruefstand();
            Rollentarif(DbWerte.LEISTUNGSMODELL_STAFFEL, 0.0, 0.0,
                        new LeistungsStufe(0.0, 40.0, 60.0));

            BerichtsDaten daten = Gruppe(out VariantenDaten stamm, out VariantenDaten variante);
            stamm.Zeitreihen = Stundenreihen(NETZBEZUG);
            variante.Zeitreihen = Stundenreihen(NETZBEZUG - EINSPARUNG);
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis s = Finde(ctrl.Berechne(daten, ctrl.LadeParameter(STAMM)),
                                                 STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            double mitStaffel = s.EnergiekostenJahr.Value;
            Assert.Equal(GAS_EUR + NETZBEZUG * 1000.0 * 0.32 + 100.0, mitStaffel, 2);
            Assert.Contains("nach dem Modell „" + WindowsFormsApplication1.MyResource.Resource.TARIF_LM_STAFFEL +
                            "“ nicht angesetzt", s.Hinweis ?? "");

            // Ohne Leistungspreis am Reststromtarif: dieselbe Zahl, kein Hinweis.
            Rollentarif(DbWerte.LEISTUNGSMODELL_MONATLICH, 0.0, 0.0);
            BerichtsDaten ohne = Gruppe(out VariantenDaten stammOhne, out VariantenDaten varianteOhne);
            stammOhne.Zeitreihen = Stundenreihen(NETZBEZUG);
            varianteOhne.Zeitreihen = Stundenreihen(NETZBEZUG - EINSPARUNG);
            WirtschaftlichkeitErgebnis so = Finde(ctrl.Berechne(ohne, ctrl.LadeParameter(STAMM)),
                                                  STAMM, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(mitStaffel, so.EnergiekostenJahr.Value, 6);
            Assert.DoesNotContain("nicht angesetzt", so.Hinweis ?? "");
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

        /// <summary>Leistungs- und Grundpreis des Auslieferungs-Stromträgers im Katalog.</summary>
        private static void Katalogleistungspreis(double leistung, double grund, string modus)
        {
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_power = ?, price_base = ?, price_power_modus = ? WHERE id = ?",
                new DbParam("@l", leistung), new DbParam("@g", grund),
                new DbParam("@m", modus), new DbParam("@c", STROM));
        }

        /// <summary>Ein Zeitreihensatz, der nur die Bezugsspitze trägt (Jahres- und zwölf gleiche
        /// Monatsspitzen) — wie in <c>StromLeistungspreisTests</c>.</summary>
        private static ZeitreihenSatz Spitze(double jahrKW, double monatKW)
        {
            var s = new Netzbezugsspitze { JahrKW = jahrKW };
            for (int m = 0; m < 12; m++) s.MonatKW[m] = monatKW;
            return new ZeitreihenSatz { Bezugsspitze = s };
        }

        /// <summary>Stundenreihen mit gleichbleibendem Netzbezug und demselben Strombedarf — die
        /// Grundlage der Strommatrix, ohne die der Rollentarif nicht rechnet.</summary>
        private static ZeitreihenSatz Stundenreihen(double netzbezugMWh)
        {
            var z = new ZeitreihenSatz();
            var reihe = new double[ZeitreihenSatz.Stunden];
            for (int h = 0; h < reihe.Length; h++) reihe[h] = netzbezugMWh * 1000.0 / ZeitreihenSatz.Stunden;
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = reihe;
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = (double[])reihe.Clone();
            return z;
        }

        /// <summary>Eine aktive Tarifstruktur im Rollenmodell für den Stamm: Bezug 0,30 €/kWh,
        /// Reststrom 0,32 €/kWh und 100 €/a, beide Rollen mit <paramref name="modell"/> und den
        /// Monatspreisen bzw. Stufen.</summary>
        private static void Rollentarif(string modell, double bezugMonat, double reststromMonat,
                                        params LeistungsStufe[] stufen)
        {
            var t = new TarifParameter { IdStamm = STAMM, Aktiv = true, Modus = DbWerte.TARIF_MODUS_ROLLEN };
            t.Bezug.ArbeitspreisEurKWh = 0.30;
            t.Bezug.Leistungsmodell = modell;
            t.Bezug.MonatspreisEurKWMonat = bezugMonat;
            t.Bezug.Stufen = new List<LeistungsStufe>(stufen);
            t.Reststrom.ArbeitspreisEurKWh = 0.32;
            t.Reststrom.GrundpreisEurJahr = 100.0;
            t.Reststrom.Leistungsmodell = modell;
            t.Reststrom.MonatspreisEurKWMonat = reststromMonat;
            t.Reststrom.Stufen = new List<LeistungsStufe>(stufen);
            var ctrl = new WirtschaftlichkeitCtrl();
            Assert.True(ctrl.SpeichereTarif(t));
            Assert.True(ctrl.LadeTarif(STAMM).Wirksam);
        }
    }
}
