using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7b — <b>die zweistufige Leistungspreis-Staffel am Stromträger</b>
    /// (Entscheid Q11, Anwender 22.09.2026: „kein HT/NT"; der Rest nach Empfehlung: die
    /// Staffel zieht in die Kostenverwaltung neben die Energiepreisstruktur, Weg 2 aus
    /// Nach #291).
    ///
    /// <para>Bis E7b stand die Staffel im Tarifsatz und rechnete allein im Zonenmodell,
    /// bemessen an der höchsten STUNDENlast der Strommatrix. Jetzt steht sie an der
    /// Projektübersteuerung des Stromträgers (Schemaschritt 104), der
    /// <see cref="KostenEmissionRechner"/> liest sie dort und bemisst sie an der
    /// VIERTELSTUNDENspitze — wie jeden Leistungspreis des Stromträgers. Eine gepflegte
    /// Staffel geht dem konstanten Satz und der Saisonreihe vor.</para>
    ///
    /// <para>Die Probe: Projekt 1030 (Stromträger 60 „Elektrische Energie" mit eigener
    /// Zeile in energy_project_settings), Staffelgrenze 1 500 kW, 60 und 90 €/(kW·a),
    /// Spitze 2 011 kW: 1 500 × 60 + 511 × 90 = 90 000 + 45 990 = <b>135 990 €/a</b> —
    /// derselbe Betrag, den das Zonenmodell für den Tarifsatz der Messprobe rechnete
    /// (dort an der Stundenspitze, die in 1030 ebenfalls 2 011 kW beträgt).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class LeistungspreisStaffelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Projekt mit eigener Stromträgerzeile (Träger 60).</summary>
        private const int PROJEKT = 1030;

        /// <summary><c>energy_carrier.id</c> von „Elektrische Energie".</summary>
        private const int STROM = 60;

        private const double STAFFELBETRAG = 135990.0;

        private static LeistungspreisStaffel Staffel(double? grenze, double? p1, double? p2) =>
            new LeistungspreisStaffel { GrenzeKW = grenze, Preis1EurKWa = p1, Preis2EurKWa = p2 };

        // =================================================================
        // 1 — Die Rechnung selbst (ohne Datenbank)
        // =================================================================

        [Fact]
        public void Der_Betrag_rechnet_bis_zur_Grenze_mit_Preis_1_und_darueber_mit_Preis_2()
        {
            Assert.Equal(STAFFELBETRAG, Staffel(1500, 60, 90).Betrag(2011), 6);
            Assert.Equal(800 * 60.0, Staffel(1500, 60, 90).Betrag(800), 6);        // ganz in Stufe 1
            Assert.Equal(2011 * 90.0, Staffel(0, 60, 90).Betrag(2011), 6);         // keine Grenze: alles Stufe 2
            Assert.Equal(2011 * 90.0, Staffel(-5, 60, 90).Betrag(2011), 6);        // negative Grenze wie 0
            Assert.Equal(2011 * 90.0, Staffel(null, 60, 90).Betrag(2011), 6);      // leere Grenze wie 0
            Assert.Equal(0.0, Staffel(1500, 60, 90).Betrag(0), 6);                 // ohne Spitze kein Betrag
            Assert.Equal(STAFFELBETRAG, LeistungspreisStaffel.Betrag(2011, 1500, 60, 90), 6);
        }

        [Fact]
        public void Gepflegt_ist_die_Staffel_erst_mit_einem_Preis()
        {
            Assert.False(new LeistungspreisStaffel().Gepflegt);
            Assert.False(Staffel(1500, null, null).Gepflegt);                      // Grenze allein rechnet nichts
            Assert.False(Staffel(1500, 0, 0).Gepflegt);
            Assert.True(Staffel(1500, 60, null).Gepflegt);
            Assert.True(Staffel(null, null, 90).Gepflegt);
        }

        [Fact]
        public void An_der_Spitze_gilt_der_Preis_ihrer_Stufe()
        {
            Assert.True(Staffel(1500, 60, 90).ZweiteStufe(2011));
            Assert.Equal(90.0, Staffel(1500, 60, 90).PreisAnDerSpitze(2011), 6);
            Assert.False(Staffel(1500, 60, 90).ZweiteStufe(1000));
            Assert.Equal(60.0, Staffel(1500, 60, 90).PreisAnDerSpitze(1000), 6);
            Assert.False(Staffel(1500, 60, 0).ZweiteStufe(2011));                  // ohne Preis 2 bleibt Stufe 1
            Assert.Equal(60.0, Staffel(1500, 60, 0).PreisAnDerSpitze(2011), 6);
            Assert.Equal(60.0, Staffel(1500, 60, 90).PreisAnDerSpitze(0), 6);      // Spitze unbekannt
        }

        // =================================================================
        // 2 — Lesen und Schreiben über den Controller der Trägerkarte
        // =================================================================

        [Fact]
        public void Die_Staffel_wird_am_Stromtraeger_geschrieben_und_gelesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Vorbestand: keine Staffel.
            Assert.False(EnergietraegerPreisCtrl.StaffelLesen(PROJEKT, STROM).Gepflegt);

            Assert.True(EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, Staffel(1500, 60, 90)));
            LeistungspreisStaffel gelesen = EnergietraegerPreisCtrl.StaffelLesen(PROJEKT, STROM);
            Assert.Equal(1500.0, gelesen.GrenzeKW);
            Assert.Equal(60.0, gelesen.Preis1EurKWa);
            Assert.Equal(90.0, gelesen.Preis2EurKWa);

            // Dieselbe Staffel über den Leseweg der Karte (SELECT *).
            EnergietraegerPreisCtrl.Projektpreis p = EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT, STROM);
            Assert.NotNull(p);
            Assert.True(p.Staffel.Gepflegt);
            Assert.Equal(90.0, p.Staffel.Preis2EurKWa);

            // Ein geleertes Feld schreibt NULL - „nicht gepflegt", nicht 0.
            Assert.True(EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, new LeistungspreisStaffel()));
            LeistungspreisStaffel leer = EnergietraegerPreisCtrl.StaffelLesen(PROJEKT, STROM);
            Assert.Null(leer.GrenzeKW);
            Assert.Null(leer.Preis1EurKWa);
            Assert.Null(leer.Preis2EurKWa);

            // Ohne Zeile des Trägers wird nichts geschrieben — und nichts angelegt.
            Assert.False(EnergietraegerPreisCtrl.StaffelSchreiben(1026, STROM, Staffel(1500, 60, 90)));
            Assert.False(EnergietraegerPreisCtrl.StaffelLesen(1026, STROM).Gepflegt);
        }

        // =================================================================
        // 3 — Der Rechenweg: KostenEmissionRechner
        // =================================================================

        [Fact]
        public void Die_Staffel_bepreist_die_Viertelstundenspitze_des_Netzbezugs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten ohne = Rechne(PROJEKT, Spitze(2011, 1500));
            Assert.True(ohne.StromkostenNetz.HasValue);
            Assert.Null(ohne.EnergieLeistungsanteil);

            EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, Staffel(1500, 60, 90));
            VariantenDaten mit = Rechne(PROJEKT, Spitze(2011, 1500));

            Assert.Equal(STAFFELBETRAG, mit.EnergieLeistungsanteil.Value, 2);
            Assert.Equal(ohne.StromkostenNetz.Value + STAFFELBETRAG, mit.StromkostenNetz.Value, 2);
            Assert.Equal(ohne.Energiekosten.Value + STAFFELBETRAG, mit.Energiekosten.Value, 2);
            Assert.True(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT),
                        "Die Staffel allein braucht die Bezugsspitze — also Stundenreihen im Lauf.");
        }

        [Fact]
        public void Die_Staffel_geht_dem_konstanten_Leistungspreis_vor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_power = ? WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@l", 50.0), new DbParam("@p", PROJEKT), new DbParam("@c", STROM));
            VariantenDaten satz = Rechne(PROJEKT, Spitze(2011, 1500));
            Assert.Equal(50.0 * 2011, satz.EnergieLeistungsanteil.Value, 2);        // Vorbedingung

            EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, Staffel(1500, 60, 90));
            VariantenDaten staffel = Rechne(PROJEKT, Spitze(2011, 1500));
            Assert.Equal(STAFFELBETRAG, staffel.EnergieLeistungsanteil.Value, 2);
        }

        /// <summary>
        /// <b>Entscheid E7b‑Q3</b> (Anwender 23.09.2026): „Eine gepflegte Staffel ersetzt
        /// beides, sie addiert sich nicht. Entweder Leistungspreis gesetzt oder eine Reihe,
        /// keine Addition." Die drei Quellen des Strom-Leistungspreises schließen einander
        /// aus — Rangfolge Staffel, Saisonreihe, konstanter Satz — und der Anteil ist nie
        /// ihre Summe. Gemessen am Leistungsanteil UND an den Netzkosten: Der Rest ohne
        /// Leistungsanteil ist in allen vier Fällen derselbe, der Anteil steht also genau
        /// einmal darin.
        ///
        /// <para>Spitze: Jahr 2 011 kW, jeder Monat 1 500 kW. Satz 50 €/(kW·a) (Modus JAHR)
        /// → 100 550 €/a; Saisonreihe 4 €/(kW·Monat) in jedem Monat → 12 × 4 × 1 500 =
        /// 72 000 €/a; Staffel 1 500 kW / 60 / 90 → 135 990 €/a.</para>
        /// </summary>
        [Fact]
        public void Satz_Saisonreihe_und_Staffel_schliessen_einander_aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZeitreihenSatz spitze = Spitze(2011, 1500);

            // Ohne jeden Leistungspreis: kein Anteil — der Rest der Netzkosten.
            VariantenDaten ohne = Rechne(PROJEKT, spitze);
            Assert.Null(ohne.EnergieLeistungsanteil);
            double rest = ohne.StromkostenNetz.Value;

            // (1) Nur der Satz.
            Leistungspreis(50.0);
            VariantenDaten satz = Rechne(PROJEKT, spitze);
            Assert.Equal(50.0 * 2011, satz.EnergieLeistungsanteil.Value, 2);
            Assert.Equal(rest + 50.0 * 2011, satz.StromkostenNetz.Value, 2);

            // (2) Satz UND Saisonreihe: Die Reihe ersetzt den Satz — nicht 72 000 + 100 550.
            Saisonreihe(4.0);
            VariantenDaten reihe = Rechne(PROJEKT, spitze);
            Assert.Equal(12 * 4.0 * 1500, reihe.EnergieLeistungsanteil.Value, 2);
            Assert.Equal(rest + 12 * 4.0 * 1500, reihe.StromkostenNetz.Value, 2);

            // (3) Satz, Reihe UND Staffel: Die Staffel ersetzt beide.
            EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, Staffel(1500, 60, 90));
            VariantenDaten staffel = Rechne(PROJEKT, spitze);
            Assert.Equal(STAFFELBETRAG, staffel.EnergieLeistungsanteil.Value, 2);
            Assert.Equal(rest + STAFFELBETRAG, staffel.StromkostenNetz.Value, 2);
            Assert.Equal(ohne.Energiekosten.Value + STAFFELBETRAG, staffel.Energiekosten.Value, 2);
        }

        [Fact]
        public void Ohne_Spitze_faellt_der_Staffelanteil_aus_und_wird_benannt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten ohneStaffel = Rechne(PROJEKT, null);
            EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, Staffel(1500, 60, 90));
            VariantenDaten v = Rechne(PROJEKT, null);

            Assert.Null(v.EnergieLeistungsanteil);
            Assert.Equal(ohneStaffel.StromkostenNetz.Value, v.StromkostenNetz.Value, 2);
            Assert.Equal("Elektrische Energie", v.LeistungspreisOhneSpitze);
        }

        // =================================================================
        // 4 — Eine neue Version erbt die Staffel
        // =================================================================

        [Fact]
        public void Eine_neue_Version_erbt_die_Staffel_des_Stromtraegers()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM, Staffel(1500, 60, 90));

            const int NEU = 99731;
            DataRepository.ExecuteSQL("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (?, ?)",
                new DbParam("@id", NEU), new DbParam("@n", "Staffelprobe E7b"));
            new VariantenCtrl().KopiereEnergieEinstellungen(PROJEKT, NEU);

            LeistungspreisStaffel geerbt = EnergietraegerPreisCtrl.StaffelLesen(NEU, STROM);
            Assert.True(geerbt.Gepflegt);
            Assert.Equal(1500.0, geerbt.GrenzeKW);
            Assert.Equal(60.0, geerbt.Preis1EurKWa);
            Assert.Equal(90.0, geerbt.Preis2EurKWa);
        }

        // =================================================================
        // Handgriffe (wie StromLeistungspreisTests)
        // =================================================================

        /// <summary>Ein Zeitreihensatz, der nur die Bezugsspitze trägt.</summary>
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

        /// <summary>Der konstante Leistungspreis der Projektübersteuerung [€/(kW·a)].</summary>
        private static void Leistungspreis(double satz)
        {
            DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_power = ? WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@l", satz), new DbParam("@p", PROJEKT), new DbParam("@c", STROM));
        }

        /// <summary>Eine Saisonreihe des Stromträgers auf Projektebene — zwölf gleiche
        /// Monatssätze [€/(kW·Monat)], wie sie der Dialog „Leistungspreis-Reihe" anlegt.</summary>
        private static void Saisonreihe(double jeMonat)
        {
            var werte = new double[12];
            for (int i = 0; i < 12; i++) werte[i] = jeMonat;
            int id = new PreisreiheCtrl().Insert(new PreisreiheModel
            {
                ID_Projekt = PROJEKT,
                ID_Energietraeger = STROM,
                Bezeichner = "Saisonreihe E7b-Q3",
                Jahr = 2026,
                Aufloesung = DbWerte.PREISREIHE_AUFLOESUNG_MONAT,
                Einheit = DbWerte.PREISREIHE_EINHEIT_EUR_KW_MONAT
            }, werte);
            Assert.True(id > 0, "Die Saisonreihe ließ sich nicht anlegen.");
        }
    }
}
