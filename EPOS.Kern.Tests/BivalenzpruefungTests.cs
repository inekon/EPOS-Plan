using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Dialogprüfung der Gruppe „Bivalenz und Übergabe"</b> (Übergabegrenze UB‑E2; Fachkonzept 6.2,
    /// Umsetzungskonzept 6.2–6.4): Lesewerte nach Kältemittel, Einbindung (Werte, Vorbelegung), jede weiche Sperre
    /// einmal mit Gegenprobe, der maßgebende Abschaltpunkt; dazu die Schreibwege an der Testdatenbank (Einbindung und
    /// Vorwärmbetrieb über <see cref="WErzeugerCtrl.KonfigurationSchreiben"/>, Kältemittel der Projektkopie).
    /// </summary>
    [Collection("Testdatenbank")]
    public class BivalenzpruefungTests
    {
        private static readonly string[] KASKADE_WP_VOR_KESSEL =
            { DbWerte.ERZEUGER_WAERMEPUMPE, DbWerte.ERZEUGER_HEIZKESSEL, "", "" };

        private static Bivalenzpruefeingang Eingang(Geraetegrenzwerte? grenzen = null, IReadOnlyList<string?>? kaskade = null,
                                                    bool vorwaerm = false, bool heizstab = false)
            => new Bivalenzpruefeingang
            {
                HoechstvorlaufC = 55.0,
                Grenzen = grenzen ?? Bivalenzpruefung.Grenzen("R410A", 55.0),
                Bivalent = true,
                Betriebsart = Bivalenzbetriebsart.Teilparallel,
                Vorwaermbetrieb = vorwaerm,
                Kaskade = kaskade ?? KASKADE_WP_VOR_KESSEL,
                Heizstab = heizstab,
            };

        [Fact]
        public void Grenzen_R410A_R744_und_allgemein()
        {
            Geraetegrenzwerte r410 = Bivalenzpruefung.Grenzen("R410A", 55.0);
            Assert.Equal((5.0, 10.0, 3.0), (r410.SpreizungAuslegungK, r410.SpreizungMaxK, r410.SpreizungMinK));
            Assert.Equal(0.60, r410.MindestvolumenstromAnteil, 9);
            Assert.Equal(52.0, r410.RuecklaufMaxC, 9);
            Assert.Equal(Geraetegrenzherkunft.VorgabeKaeltemittel, r410.SpreizungHerkunft);
            Assert.Equal(Geraetegrenzherkunft.Abgeleitet, r410.RuecklaufHerkunft);
            Assert.Null(r410.BezugsruecklaufC);

            Geraetegrenzwerte r744 = Bivalenzpruefung.Grenzen("R744", 80.0);
            Assert.Equal(30.0, r744.SpreizungMaxK, 9);
            Assert.Equal(40.0, r744.RuecklaufMaxC, 9);
            Assert.Equal(Geraetegrenzherkunft.VorgabeKaeltemittel, r744.RuecklaufHerkunft);
            Assert.Equal(30.0, r744.BezugsruecklaufC);
            Assert.Equal(2.5, r744.AbwertungProzentJeK);
            Assert.Equal(40.0, r744.RuecklaufGrenzeR744C);

            Geraetegrenzwerte leer = Bivalenzpruefung.Grenzen(null, 50.0);
            Assert.Equal(Geraetegrenzherkunft.Vorgabe, leer.SpreizungHerkunft);
            Assert.Equal(47.0, leer.RuecklaufMaxC, 9);
            Assert.True(double.IsNaN(Bivalenzpruefung.Grenzen("SONSTIGES", double.NaN).RuecklaufMaxC));
        }

        [Fact]
        public void Einbindung_normiert_und_vorbelegt()
        {
            Assert.Equal("PUFFER", Bivalenzpruefung.EinbindungNormiert(" puffer "));
            Assert.Null(Bivalenzpruefung.EinbindungNormiert("Speicher"));
            Assert.Null(Bivalenzpruefung.EinbindungNormiert(""));
            Assert.Equal("PUFFER", Bivalenzpruefung.EinbindungVorbelegung(true, true));
            Assert.Equal("DIREKT", Bivalenzpruefung.EinbindungVorbelegung(true, false));
            Assert.Null(Bivalenzpruefung.EinbindungVorbelegung(false, true));

            // Die Abbildung: neue Anlage mit Heizungspuffer → PUFFER, ohne → DIREKT; Bestand bleibt wie gelesen.
            var mitPuffer = new WErzeugerModel { WS_ID_Puffer = 7 };
            BivalenzAbbildung.Vorbelegen(mitPuffer);
            Assert.Equal("PUFFER", mitPuffer.Einbindung);
            var ohne = new WErzeugerModel();
            BivalenzAbbildung.Vorbelegen(ohne);
            Assert.Equal("DIREKT", ohne.Einbindung);
            var d = new WaermepumpeAnlageDaten();
            BivalenzAbbildung.AnlageLesen(new WErzeugerModel { Einbindung = null, Vorwaermbetrieb = true }, d);
            Assert.Null(d.Einbindung);
            Assert.True(d.Vorwaermbetrieb);
            var m = new WErzeugerModel();
            d.Einbindung = "weiche";
            BivalenzAbbildung.AnlageSchreiben(d, m);
            Assert.Equal("WEICHE", m.Einbindung);
            Assert.True(m.Vorwaermbetrieb);
            WErzeugerCtrl.KonfigurationFelder f = BivalenzAbbildung.MitUebergabe(new WErzeugerCtrl.KonfigurationFelder(), d);
            Assert.True(f.Uebergabe);
            Assert.Equal("WEICHE", f.Einbindung);
            Assert.True(f.Vorwaermbetrieb);
        }

        [Fact]
        public void Ohne_Befund_im_Regelfall()
            => Assert.Empty(Bivalenzpruefung.Pruefen(Eingang(vorwaerm: true)));

        [Fact]
        public void Spreizung_min_nicht_unter_max()
        {
            var g = Bivalenzpruefung.Grenzen("R410A", 55.0) with { SpreizungMinK = 10.0 };
            Bivalenzbefund b = Assert.Single(Bivalenzpruefung.Pruefen(Eingang(g)));
            Assert.Equal(Bivalenzbefundart.Spreizung, b.Art);
            Assert.False(b.NurHinweis);
        }

        [Fact]
        public void Hoechstvorlauf_unter_der_Flaechenheizung()
        {
            var e = Eingang();
            Assert.Empty(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { HoechstvorlaufC = 55.0, FlaechenVorlaufC = 35.0, Grenzen = e.Grenzen }));
            Bivalenzbefund b = Assert.Single(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { HoechstvorlaufC = 30.0, FlaechenVorlaufC = 35.0 }));
            Assert.Equal(Bivalenzbefundart.Hoechstvorlauf, b.Art);
            Assert.Equal((30.0, 35.0), (b.Wert1, b.Wert2));
        }

        /// <summary>θ_R,UE des Zahlenbeispiels: Heizkörper 75/60/20 °C, n 1,3, 10 kW bei θ_WP,max 55 °C (46,48 °C).</summary>
        private static double RuecklaufZahlenbeispiel()
            => Uebergabegrenze.Gebaeude(null, new Uebergabezone(10.0, 75.0, 60.0, 20.0, 1.3), 55.0, 20.0).RuecklaufC;

        [Fact]
        public void Ruecklauf_nie_R410A_am_Zahlenbeispiel_ohne_Sperre()
        {
            double rUe = RuecklaufZahlenbeispiel();
            Assert.Equal(46.48, rUe, 2);
            // Der Auslegungsrücklauf 60 °C liegt über der Grenze 52 °C — maßgebend ist aber θ_R,UE.
            Assert.Empty(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { Grenzen = Bivalenzpruefung.Grenzen("R410A", 55.0), RuecklaufUebergabeC = rUe }));
        }

        [Fact]
        public void Ruecklauf_nie_R744_am_Zahlenbeispiel_mit_Sperre()
        {
            double rUe = RuecklaufZahlenbeispiel();
            Bivalenzbefund b = Assert.Single(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { Grenzen = Bivalenzpruefung.Grenzen("R744", 55.0), RuecklaufUebergabeC = rUe }));
            Assert.Equal(Bivalenzbefundart.RuecklaufNie, b.Art);
            Assert.False(b.NurHinweis);
            Assert.Equal(40.0, b.Wert1, 9);
            Assert.Equal(rUe, b.Wert2, 9);
            // Gleich der Grenze: keine Sperre.
            Assert.Empty(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { Grenzen = Bivalenzpruefung.Grenzen("R744", 55.0), RuecklaufUebergabeC = 40.0 }));
        }

        [Fact]
        public void Ruecklauf_nie_ohne_Uebergabedaten_ohne_Sperre()
        {
            Assert.Empty(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang { Grenzen = Bivalenzpruefung.Grenzen("R744", 55.0) }));
        }

        [Fact]
        public void Vorwaermbetrieb_ohne_Kessel_oder_Heizstab()
        {
            var nurWp = new string?[] { DbWerte.ERZEUGER_WAERMEPUMPE, "", "", "" };
            Bivalenzbefund b = Assert.Single(Bivalenzpruefung.Pruefen(Eingang(kaskade: nurWp, vorwaerm: true)));
            Assert.Equal(Bivalenzbefundart.VorwaermOhneKessel, b.Art);
            Assert.Empty(Bivalenzpruefung.Pruefen(Eingang(kaskade: nurWp, vorwaerm: true, heizstab: true)));
            Assert.Empty(Bivalenzpruefung.Pruefen(Eingang(kaskade: nurWp, vorwaerm: false)));
        }

        [Fact]
        public void Waermepumpe_hinter_dem_Kessel_bei_Vorwaermbetrieb()
        {
            var kesselZuerst = new string?[] { DbWerte.ERZEUGER_HEIZKESSEL, DbWerte.ERZEUGER_WAERMEPUMPE, "", "" };
            Bivalenzbefund b = Assert.Single(Bivalenzpruefung.Pruefen(Eingang(kaskade: kesselZuerst, vorwaerm: true)));
            Assert.Equal(Bivalenzbefundart.Kaskade, b.Art);
            Assert.Equal((2.0, 1.0), (b.Wert1, b.Wert2));
            Assert.Empty(Bivalenzpruefung.Pruefen(Eingang(kaskade: kesselZuerst, vorwaerm: false)));
            // Bei alternativ ist der Vorwärmbetrieb nicht wählbar — keine Kaskadenregel.
            var alt = new Bivalenzpruefeingang
            {
                Bivalent = true, Betriebsart = Bivalenzbetriebsart.Alternativ, Vorwaermbetrieb = true, Kaskade = kesselZuerst,
            };
            Assert.Empty(Bivalenzpruefung.Pruefen(alt));
            Assert.False(Bivalenzpruefung.VorwaermbetriebWaehlbar(Bivalenzbetriebsart.Alternativ));
        }

        [Fact]
        public void Hybrid_Mindestanteil_ist_ein_Hinweis()
        {
            Bivalenzbefund b = Assert.Single(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { Bivalent = true, HybridAnteil = 0.25, HybridMindestanteil = 0.30 }));
            Assert.Equal(Bivalenzbefundart.Gmodg, b.Art);
            Assert.True(b.NurHinweis);
            Assert.Empty(Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
                { Bivalent = true, HybridAnteil = 0.70, HybridMindestanteil = 0.30 }));
        }

        [Fact]
        public void Massgebender_Abschaltpunkt_ist_der_waermere()
        {
            Assert.Equal(-3.5, Bivalenzrechner.Massgebend(-10.0, -3.5), 9);
            Assert.Equal(3.0, Bivalenzrechner.Massgebend(3.0, -3.5), 9);
        }

        /// <summary>Einbindung und Vorwärmbetrieb über den schmalen Schreibweg; die Anlagenanweisung führt beide zuletzt.</summary>
        [Fact]
        public void Schreibweg_der_Konfiguration_und_Anlagenanweisung()
        {
            Assert.Contains("Kuehl_Frei_Leistung_kW, Einbindung, Vorwaermbetrieb) VALUES", AnlagenSql.SQL_ANLAGE_INSERT,
                            StringComparison.Ordinal);
            DbParam[] p = AnlagenSql.AnlagenParameter(1, new WErzeugerModel { Einbindung = "puffer", Vorwaermbetrieb = true });
            Assert.Equal("PUFFER", p[p.Length - 2].Wert);
            Assert.Equal(1, p[p.Length - 1].Wert);
            DbParam[] leer = AnlagenSql.AnlagenParameter(1, new WErzeugerModel { Einbindung = "Speicher" });
            Assert.True(leer[leer.Length - 2].Wert == null || leer[leer.Length - 2].Wert == DBNull.Value);
            Assert.Equal(0, leer[leer.Length - 1].Wert);

            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Tab_Energieanlagen WHERE ID_Type = ? ORDER BY ID LIMIT 1",
                new DbParam("@typ", WizardItemClass.WP_TYP));
            Assert.True(dt.Rows.Count == 1, "keine Wärmepumpen-Anlage in der Testdatenbank");
            int id = Convert.ToInt32(dt.Rows[0]["ID"]), projekt = Convert.ToInt32(dt.Rows[0]["ID_Projekt"]);

            var e = WErzeugerCtrl.KonfigurationSchreiben(id, projekt,
                new WErzeugerCtrl.KonfigurationFelder(Uebergabe: true, Einbindung: "weiche", Vorwaermbetrieb: true));
            Assert.True(e.Ok, e.Meldung);
            DataTable z = DataRepository.GetDataTable(
                "SELECT Einbindung, Vorwaermbetrieb FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", id));
            Assert.Equal("WEICHE", Convert.ToString(z.Rows[0]["Einbindung"]));
            Assert.Equal(1L, Convert.ToInt64(z.Rows[0]["Vorwaermbetrieb"]));

            // Ohne Gruppenschalter bleibt alles stehen; mit leerer Einbindung wird sie NULL (U-1).
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(id, projekt, new WErzeugerCtrl.KonfigurationFelder()).Ok);
            z = DataRepository.GetDataTable("SELECT Einbindung FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", id));
            Assert.Equal("WEICHE", Convert.ToString(z.Rows[0]["Einbindung"]));
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(id, projekt,
                new WErzeugerCtrl.KonfigurationFelder(Uebergabe: true)).Ok);
            z = DataRepository.GetDataTable("SELECT Einbindung, Vorwaermbetrieb FROM Tab_Energieanlagen WHERE ID = ?",
                                            new DbParam("@id", id));
            Assert.Equal(DBNull.Value, z.Rows[0]["Einbindung"]);
            Assert.Equal(0L, Convert.ToInt64(z.Rows[0]["Vorwaermbetrieb"]));
        }

        /// <summary>Die Schnellwahl speichert das Kältemittel in der Projektkopie; ein gefülltes Vorlauf_Max bleibt.</summary>
        [Fact]
        public void Schnellwahl_speichert_das_Kaeltemittel_des_Geraets()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            DataTable dt = DataRepository.GetDataTable("SELECT ID FROM Tab_WP ORDER BY ID LIMIT 1");
            Assert.True(dt.Rows.Count == 1, "keine Projektkopie in Tab_WP");
            int idWp = Convert.ToInt32(dt.Rows[0]["ID"]);

            var d = new WaermepumpeAnlageDaten
            {
                IdWp = idWp, VorlaufMax = 60.0, Kaeltemittelliste = BivalenzAbbildung.Kaeltemittelliste(),
            };
            WaermepumpeKonfiguration.SchnellwahlAnwenden(d, "R290");
            Assert.Equal(60.0, d.VorlaufMax);
            Assert.True(BivalenzAbbildung.GeraetSchreiben(d));
            Assert.Equal("R290", WaermepumpeGeraeteCtrl.KaeltemittelLesen(idWp));

            // Die Klappliste zeigt beim Öffnen das gespeicherte Kältemittel.
            var neu = new WaermepumpeAnlageDaten { IdWp = idWp };
            BivalenzAbbildung.Lesen(new WErzeugerModel { ID_WP = idWp }, neu);
            Assert.Equal("R290", neu.Kaeltemittel);

            d.Kaeltemittel = null;
            Assert.True(BivalenzAbbildung.GeraetSchreiben(d));
            Assert.Null(WaermepumpeGeraeteCtrl.KaeltemittelLesen(idWp));
            Assert.False(WaermepumpeGeraeteCtrl.KaeltemittelSchreiben(-1, "R290"));
        }
    }
}
