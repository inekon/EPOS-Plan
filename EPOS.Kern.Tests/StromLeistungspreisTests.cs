using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderbefund vom 16.09.2026</b> (Berichte &amp; Kosten →
    /// Wirtschaftlichkeit): „Die Energiekosten sind nicht vollständig. Es wird nur der
    /// Arbeitspreis genommen. Der Strompreis setzt sich aus Arbeitspreis, Leistungspreis
    /// und Grundpreis zusammen. Die Effekte der Lastspitzenkappung gehen so im Vergleich
    /// verloren."
    ///
    /// <para><b>Die Lage im Bestand.</b> Der Leistungspreiszweig des
    /// <see cref="KostenEmissionRechner"/> war gegen den Stromträger gesperrt — sein
    /// Leistungspreis galt als Sache der Tarifstruktur. Ein Projekt ohne aktiven Tarif
    /// bezahlte damit Arbeit und Grundpreis, nie die Spitze; ein Speicher, der die
    /// Bezugsspitze halbiert, senkte die Energiekosten um keinen Cent.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Der Regelweg rechnet
    /// <c>Arbeit × Arbeitspreis + Grundpreis + Leistungspreis × Bezugsspitze</c>; die
    /// Spitze ist das VIERTELSTUNDEN-Maximum der Netzbezugsreihe der Variante
    /// (Anwenderentscheid 17.09.2026, SP-E-1 a / SP-E-1-Q1). Jede Zahl unten ist von
    /// Hand hergeleitet: Der Netzbezug des Projekts 1026 beträgt 19,08 MWh/a, der
    /// Arbeitspreis 0,35 €/kWh — 19,08 × 1 000 × 0,35 = <b>6 678,00 €/a</b>. Was der
    /// Leistungspreis darauf legt, steht im jeweiligen Fall.</para>
    ///
    /// <para>Jeder Fall legt seine EIGENE Arbeitskopie an (die Fälle schreiben Preise);
    /// <c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromLeistungspreisTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Beispiel WP WG 1" — Wärmepumpe, PV und Stromspeicher; Netzbezug
        /// 19,08 MWh/a, kein Brennstoffverbrauch. Die Energiekosten sind damit reine
        /// Stromkosten, und jeder Summand ist einzeln ablesbar.</summary>
        private const int PROJEKT_WP = 1026;

        /// <summary><c>energy_carrier.id</c> von „Elektrische Energie".</summary>
        private const int STROM = 60;

        /// <summary>
        /// Die zweite Version der Gruppe für den Entscheid LS-E-2: Sie führt eine eigene
        /// Zeile in <c>energy_project_settings</c> für den Stromträger — dort steht die
        /// Projektübersteuerung, um die es geht.
        /// </summary>
        private const int PROJEKT_VERSION = 1030;

        /// <summary>Arbeitspreisanteil der Fälle unten: 19,08 MWh × 1 000 × 0,35 €/kWh.</summary>
        private const double ARBEIT = 6678.0;

        // =================================================================
        // 1 — Der Rechenweg selbst
        // =================================================================

        /// <summary>
        /// MODUS JAHR: Satz [€/(kW·a)] × JAHRESspitze. 60 €/(kW·a) × 40 kW = 2 400 €/a;
        /// zusammen mit dem Arbeitspreis 6 678,00 + 2 400,00 = <b>9 078,00 €/a</b>.
        /// Der Anteil steht zusätzlich getrennt in <c>EnergieLeistungsanteil</c> und
        /// steckt vollständig in <c>StromkostenNetz</c>.
        /// </summary>
        [Fact]
        public void Modus_JAHR_bepreist_die_Jahresspitze()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 60.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            VariantenDaten v = Rechne(PROJEKT_WP, Spitze(40.0, 30.0));

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(ARBEIT + 2400.0, v.Energiekosten.Value, 2);
            Assert.Equal(2400.0, v.EnergieLeistungsanteil.Value, 2);
            Assert.Equal(ARBEIT + 2400.0, v.StromkostenNetz.Value, 2);
            Assert.Equal(40.0, v.BezugsspitzeKW.Value, 6);
            Assert.Null(v.LeistungspreisOhneSpitze);
        }

        /// <summary>
        /// MODUS MONAT: Σ über zwölf Monate (Monatsspitze × Satz [€/(kW·Monat)]).
        /// Zwölf Monatsspitzen zu 30 kW ergeben 360 kW-Monate; 5 €/(kW·Monat) × 360 =
        /// 1 800 €/a, zusammen 6 678,00 + 1 800,00 = <b>8 478,00 €/a</b>.
        ///
        /// <para>Die JAHRESspitze (40 kW) bleibt dabei ausdrücklich unbenutzt — sie
        /// gehört zu einem einzelnen Monat und würde zwölfmal berechnet die Rechnung
        /// mehr als verdoppeln.</para>
        /// </summary>
        [Fact]
        public void Modus_MONAT_bepreist_die_zwoelf_Monatsspitzen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 5.0, DbWerte.LEISTUNGSPREIS_MODUS_MONAT);

            VariantenDaten v = Rechne(PROJEKT_WP, Spitze(40.0, 30.0));

            Assert.Equal(ARBEIT + 1800.0, v.Energiekosten.Value, 2);
            Assert.Equal(1800.0, v.EnergieLeistungsanteil.Value, 2);
        }

        /// <summary>
        /// Kein gepflegter Leistungspreis (0 zählt wie überall als nicht gepflegt):
        /// Das Ergebnis bleibt der Arbeitspreis, und es entsteht KEIN Leistungsanteil.
        /// Das ist der Bestandsfall — genau er darf sich nicht bewegen.
        /// </summary>
        [Fact]
        public void Ohne_Leistungspreis_bleibt_alles_wie_zuvor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 0.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            VariantenDaten v = Rechne(PROJEKT_WP, Spitze(40.0, 30.0));

            Assert.Equal(ARBEIT, v.Energiekosten.Value, 2);
            Assert.Null(v.EnergieLeistungsanteil);
            Assert.Null(v.LeistungspreisOhneSpitze);
        }

        /// <summary>
        /// Ohne Zeitreihen gibt es keine Bezugsspitze — dann entsteht KEIN Anteil, aber
        /// der gepflegte Leistungspreis wird BENANNT. Ein Satz, der still unter den
        /// Tisch fällt, sähe wie ein günstiges Ergebnis aus.
        /// </summary>
        [Fact]
        public void Ohne_Bezugsspitze_faellt_der_Anteil_aus_und_wird_benannt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 60.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            VariantenDaten v = Rechne(PROJEKT_WP, null);

            Assert.Equal(ARBEIT, v.Energiekosten.Value, 2);
            Assert.Null(v.EnergieLeistungsanteil);
            Assert.Null(v.BezugsspitzeKW);
            Assert.Equal("Elektrische Energie", v.LeistungspreisOhneSpitze);
        }

        // =================================================================
        // 2 — Der Anwenderbefund: die Kappung wird sichtbar
        // =================================================================

        /// <summary>
        /// <b>DER BEFUND SELBST.</b> Stamm und Speichervariante rechnen dieselbe ARBEIT
        /// (derselbe Netzbezug), unterscheiden sich aber in der Bezugsspitze: 40 kW
        /// gegen 25 kW. Bei 60 €/(kW·a) ist die Differenz der Energiekosten genau
        /// (40 − 25) × 60 = <b>900 €/a</b> — der Wert der Lastspitzenkappung, der vor
        /// dieser Rechnung im Vergleich gar nicht auftauchte.
        /// </summary>
        [Fact]
        public void Die_Kappung_der_Spitze_senkt_die_Energiekosten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 60.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            VariantenDaten stamm = Rechne(PROJEKT_WP, Spitze(40.0, 40.0));
            VariantenDaten variante = Rechne(PROJEKT_WP, Spitze(25.0, 25.0));

            // Gleiche Arbeit — der Unterschied sitzt allein im Leistungsanteil.
            Assert.Equal(stamm.Energiekosten.Value - stamm.EnergieLeistungsanteil.Value,
                         variante.Energiekosten.Value - variante.EnergieLeistungsanteil.Value, 2);

            Assert.Equal(900.0, stamm.Energiekosten.Value - variante.Energiekosten.Value, 2);
            Assert.Equal((stamm.BezugsspitzeKW.Value - variante.BezugsspitzeKW.Value) * 60.0,
                         stamm.Energiekosten.Value - variante.Energiekosten.Value, 2);
        }

        // =================================================================
        // 3 — Die Tarifstruktur ersetzt weiterhin ALLES
        // =================================================================

        /// <summary>
        /// Der Tarifweg rechnet
        /// <c>Energie = Energiekosten − StromkostenNetz + Bezugskosten(tarif)</c>
        /// (<see cref="WirtschaftlichkeitCtrl"/>). Er bleibt nur dann eine einzige
        /// Wahrheit, wenn <c>StromkostenNetz</c> den GANZEN Stromanteil trägt — also
        /// auch den neuen Leistungsanteil. Geprüft wird genau diese Invariante: Der
        /// REST, den der Tarifweg stehen lässt, ist mit und ohne Leistungspreis
        /// derselbe.
        /// </summary>
        [Fact]
        public void Der_Tarifweg_rechnet_den_Leistungsanteil_mit_heraus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 0.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);
            VariantenDaten ohne = Rechne(PROJEKT_WP, Spitze(40.0, 30.0));

            Katalogpreise(0.35, 60.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);
            VariantenDaten mit = Rechne(PROJEKT_WP, Spitze(40.0, 30.0));

            Assert.NotEqual(ohne.Energiekosten.Value, mit.Energiekosten.Value);
            Assert.Equal(ohne.Energiekosten.Value - ohne.StromkostenNetz.Value,
                         mit.Energiekosten.Value - mit.StromkostenNetz.Value, 2);
        }

        // =================================================================
        // 4 — Die Spitze selbst (ohne Datenbank)
        // =================================================================

        /// <summary>
        /// Die VIERTELSTUNDEN-Spitze ist nie kleiner als die Spitze des
        /// Stundenmittels — und in einer Reihe mit kurzen Lastspitzen deutlich größer.
        /// Genau darum ist <c>StromMatrix.MaxBezugKW</c> (ein Stundenmittel) nicht die
        /// Basis des Leistungspreises.
        ///
        /// <para>Die Probe: eine Reihe, die in jeder Stunde eine Viertelstunde lang
        /// 100 kW zieht und sonst nichts. Viertelstundenspitze 100 kW,
        /// Stundenmittelspitze 25 kW.</para>
        /// </summary>
        [Fact]
        public void Die_Viertelstundenspitze_ist_nie_kleiner_als_die_Stundenmittelspitze()
        {
            var reihe = new double[ZeitreihenSatz.Stunden * 4];
            for (int h = 0; h < ZeitreihenSatz.Stunden; h++) reihe[h * 4] = 100.0;

            Netzbezugsspitze s = Netzbezugsspitze.AusReihe(reihe);

            double stundenmittel = 0;
            for (int h = 0; h < ZeitreihenSatz.Stunden; h++)
            {
                double m = (reihe[h * 4] + reihe[h * 4 + 1] + reihe[h * 4 + 2] + reihe[h * 4 + 3]) / 4.0;
                if (m > stundenmittel) stundenmittel = m;
            }

            Assert.Equal(100.0, s.JahrKW, 6);
            Assert.Equal(25.0, stundenmittel, 6);
            Assert.True(s.JahrKW >= stundenmittel);
        }

        /// <summary>
        /// Die Monatsgrenzen folgen dem festen Raster des Rechenkerns (365 Tage, kein
        /// Schaltjahr): Eine Spitze am ersten Tag des MÄRZ (Tag 60, also
        /// Viertelstunde 31·96 + 28·96 = 5 664) steht im dritten Monatswert und in
        /// keinem anderen.
        /// </summary>
        [Fact]
        public void Die_Monatsspitzen_treffen_den_richtigen_Monat()
        {
            var reihe = new double[ZeitreihenSatz.Stunden * 4];
            reihe[(31 + 28) * 96] = 77.0;

            Netzbezugsspitze s = Netzbezugsspitze.AusReihe(reihe);

            Assert.Equal(77.0, s.JahrKW, 6);
            Assert.Equal(77.0, s.MonatKW[2], 6);
            for (int m = 0; m < 12; m++)
                if (m != 2) Assert.Equal(0.0, s.MonatKW[m], 6);
            Assert.Equal(77.0, s.MonatssummeKW, 6);
        }

        /// <summary>Eine Reihe in einem fremden Raster liefert KEINE Spitze — ein
        /// Fantasiewert wäre schlimmer als ein fehlender.</summary>
        [Fact]
        public void Ein_fremdes_Raster_liefert_keine_Spitze()
        {
            Assert.Null(Netzbezugsspitze.AusReihe(null));
            Assert.Null(Netzbezugsspitze.AusReihe(new double[1000]));
            Assert.NotNull(Netzbezugsspitze.AusReihe(new double[ZeitreihenSatz.Stunden]));
        }

        // =================================================================
        // 5 — Die Vergleichszeile, in beiden Sprachen
        // =================================================================

        /// <summary>
        /// Die Herleitungszeile „Bezugsspitze Strom [kW]" erscheint, sobald irgendein
        /// Lauf der Gruppe eine Spitze führt — und verschwindet, wenn keiner sie führt
        /// (sonst stünde in jedem Bericht eine „—"-Zeile).
        /// </summary>
        [Fact]
        public void Die_Vergleichszeile_erscheint_nur_mit_Spitze()
        {
            var mit = new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis { IstStamm = true, BezugsspitzeKW = 40.0 }
            };
            var ohne = new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis { IstStamm = true }
            };

            Assert.Contains(WirtschaftlichkeitZeilen.Kennzahlen(mit, null),
                            z => z.Schluessel == "BEZUGSSPITZE_STROM");
            Assert.DoesNotContain(WirtschaftlichkeitZeilen.Kennzahlen(ohne, null),
                                  z => z.Schluessel == "BEZUGSSPITZE_STROM");
        }

        /// <summary>Der Titel der Zeile kommt aus <c>MyResource</c> und steht in BEIDEN
        /// Sprachen — ein vertippter Schlüssel fiele sonst nirgends auf.</summary>
        [Fact]
        public void Der_Zeilentitel_steht_in_beiden_Ressourcendateien()
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;

            string de = rm.GetString("WIRT_ZEILE_BEZUGSSPITZE_STROM", new CultureInfo("de-DE"));
            string en = rm.GetString("WIRT_ZEILE_BEZUGSSPITZE_STROM", new CultureInfo("en-US"));

            Assert.False(string.IsNullOrEmpty(de));
            Assert.False(string.IsNullOrEmpty(en));
            Assert.NotEqual(de, en);
            Assert.Contains("kW", de);
            Assert.Contains("kW", en);
        }

        // =================================================================
        // 6 — LS-E-2: der Leistungspreis der GRUPPE, nicht nur des Stamms
        // =================================================================

        /// <summary>
        /// <b>Entscheid LS-E-2</b> (Auftrag VF-1): Ob die Wirtschaftlichkeit ihre Gruppe
        /// mit Zeitreihen rechnet, entschied bis hierher allein der STAMM. Der
        /// Leistungspreis ist aber eine Projektübersteuerung — eine Variante kann ihn
        /// führen, ohne dass der Stamm es tut. Dann rechnete die Gruppe ohne Reihen, und
        /// der Variante fiel der Leistungsanteil ihrer Energiekosten still weg, weil ihre
        /// Bezugsspitze nie eingesammelt wurde.
        ///
        /// <para>Die Probe setzt den Satz NUR an der zweiten Version: Der Stamm allein
        /// sagt weiter „nein", die Gruppe sagt „ja".</para>
        /// </summary>
        [Fact]
        public void Der_Leistungspreis_einer_Variante_zaehlt_fuer_die_ganze_Gruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Katalog führt keinen Leistungspreis — sonst träfe er jedes Projekt.
            Katalogpreise(0.35, 0.0, null);
            Assert.False(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT_WP));
            Assert.False(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT_VERSION));

            // Nur die zweite Version bekommt den Satz.
            DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_power = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@l", 60.0), new DbParam("@p", PROJEKT_VERSION), new DbParam("@c", STROM));

            Assert.True(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT_VERSION),
                "Vorbedingung: Die Version führt den Satz.");

            // Der Stamm ALLEIN sagt weiter „nein" …
            Assert.False(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT_WP));
            // … die GRUPPE sagt „ja".
            Assert.True(KostenEmissionRechner.StromLeistungspreisGepflegt(
                PROJEKT_WP, new List<int> { PROJEKT_WP, PROJEKT_VERSION }));

            // Ohne Versionen bleibt es beim Stamm; eine leere Liste ändert nichts.
            Assert.False(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT_WP, null));
            Assert.False(KostenEmissionRechner.StromLeistungspreisGepflegt(
                PROJEKT_WP, new List<int>()));
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        /// <summary>Ein Zeitreihensatz, der nur die Bezugsspitze trägt — mehr braucht
        /// die Kostenrechnung von ihm nicht.</summary>
        private static ZeitreihenSatz Spitze(double jahrKW, double monatKW)
        {
            var s = new Netzbezugsspitze { JahrKW = jahrKW };
            for (int m = 0; m < 12; m++) s.MonatKW[m] = monatKW;
            return new ZeitreihenSatz { Bezugsspitze = s };
        }

        private static VariantenDaten Rechne(int idProjekt, ZeitreihenSatz zeitreihen)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(idProjekt);
            Assert.NotNull(erg);
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = erg, Zeitreihen = zeitreihen };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        private static void Katalogpreise(double arbeit, double leistung, string modus)
        {
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_work = ?, price_power = ?, price_power_modus = ? WHERE id = ?",
                new DbParam("@a", arbeit), new DbParam("@l", leistung),
                new DbParam("@m", modus), new DbParam("@c", STROM));
        }
    }
}
