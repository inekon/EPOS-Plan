using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Kaskade"/> nach iU9-W10b.0b — die vier Waermeerzeuger-Plaetze und die
    /// beiden Stromplaetze von <c>Tab_Einstellungen.Tool_1..6</c>.
    ///
    /// <para><b>Ohne Datenbank.</b> Die Kaskadenlogik rechnet auf dem
    /// <see cref="KonfigurationModel"/>; bis W10b bediente sie sechs unsichtbare
    /// WinForms-Steuerelemente und war damit nur am Geraet pruefbar.</para>
    /// </summary>
    public class KaskadeTests
    {
        private static KonfigurationModel Modell(string t1, string t2, string t3, string t4)
        {
            return new KonfigurationModel
            {
                m_Tool_1 = t1,
                m_Tool_2 = t2,
                m_Tool_3 = t3,
                m_Tool_4 = t4
            };
        }

        [Fact]
        public void Lesen_liefert_immer_vier_Plaetze()
        {
            Assert.Equal(4, Kaskade.Lesen(null).Count);
            Assert.All(Kaskade.Lesen(null), p => Assert.Equal("", p));

            List<string> p4 = Kaskade.Lesen(Modell("BHKW", "", "Heizkessel", ""));
            Assert.Equal(new[] { "BHKW", "", "Heizkessel", "" }, p4);
        }

        [Fact]
        public void Belegt_entdoppelt_und_haelt_die_Reihenfolge()
        {
            KonfigurationModel k = Modell("BHKW", "", "Heizkessel", "BHKW");
            Assert.Equal(new[] { "BHKW", "Heizkessel" }, Kaskade.Belegt(k));
        }

        /// <summary>
        /// Verschieben TAUSCHT Platzinhalte und verdichtet nicht: Der leere Platz 2
        /// bleibt leer, obwohl er zwischen zwei belegten steht.
        /// </summary>
        [Fact]
        public void Verschieben_tauscht_Platzinhalte_und_laesst_Luecken_stehen()
        {
            KonfigurationModel k = Modell("BHKW", "", "Heizkessel", "");

            Assert.True(Kaskade.Verschieben(k, "Heizkessel", -1));

            Assert.Equal("Heizkessel", k.m_Tool_1);
            Assert.Equal("", k.m_Tool_2);
            Assert.Equal("BHKW", k.m_Tool_3);
            Assert.Equal("", k.m_Tool_4);
        }

        [Fact]
        public void Verschieben_ueber_den_Rand_hinaus_tut_nichts()
        {
            KonfigurationModel k = Modell("BHKW", "Heizkessel", "", "");

            Assert.False(Kaskade.Verschieben(k, "BHKW", -1));
            Assert.False(Kaskade.Verschieben(k, "Heizkessel", +1));
            Assert.False(Kaskade.Verschieben(k, "Solarthermie", -1));
            Assert.Equal("BHKW", k.m_Tool_1);
            Assert.Equal("Heizkessel", k.m_Tool_2);
        }

        /// <summary>
        /// Aufgenommen wird auf dem ersten freien Platz HINTER dem letzten belegten —
        /// die Karte erscheint damit am Ende der Kaskade.
        /// </summary>
        [Fact]
        public void Aufnehmen_nimmt_den_ersten_freien_Platz_hinter_dem_letzten_belegten()
        {
            KonfigurationModel k = Modell("", "BHKW", "", "");

            Assert.True(Kaskade.Aufnehmen(k, "Heizkessel"));
            Assert.Equal("", k.m_Tool_1);
            Assert.Equal("BHKW", k.m_Tool_2);
            Assert.Equal("Heizkessel", k.m_Tool_3);
        }

        [Fact]
        public void Aufnehmen_fuellt_eine_Luecke_erst_wenn_hinten_keiner_frei_ist()
        {
            KonfigurationModel k = Modell("", "BHKW", "Heizkessel", "Solarthermie");

            Assert.True(Kaskade.Aufnehmen(k, "Wärmepumpe"));
            Assert.Equal("Wärmepumpe", k.m_Tool_1);
        }

        [Fact]
        public void Aufnehmen_eines_schon_belegten_Werts_tut_nichts()
        {
            KonfigurationModel k = Modell("BHKW", "", "", "");
            Assert.False(Kaskade.Aufnehmen(k, "BHKW"));
            Assert.Equal("", k.m_Tool_2);
        }

        [Fact]
        public void Entfernen_leert_den_Platz_ohne_zu_verdichten()
        {
            KonfigurationModel k = Modell("BHKW", "Heizkessel", "Solarthermie", "");

            Assert.True(Kaskade.Entfernen(k, "Heizkessel"));
            Assert.Equal("BHKW", k.m_Tool_1);
            Assert.Equal("", k.m_Tool_2);
            Assert.Equal("Solarthermie", k.m_Tool_3);

            Assert.False(Kaskade.Entfernen(k, "Heizkessel"));
        }

        [Fact]
        public void StromAuswahl_bedient_Tool_5_und_Tool_6()
        {
            KonfigurationModel k = new KonfigurationModel();

            Kaskade.StromAuswahl(k, Kaskade.PLATZ_STROMERZEUGER, DbWerte.ERZEUGER_PHOTOVOLTAIK);
            Kaskade.StromAuswahl(k, Kaskade.PLATZ_ENERGIESPEICHER, DbWerte.ERZEUGER_STROMSPEICHER);

            Assert.Equal(DbWerte.ERZEUGER_PHOTOVOLTAIK, k.m_Tool_5);
            Assert.Equal(DbWerte.ERZEUGER_STROMSPEICHER, k.m_Tool_6);
            Assert.Equal(DbWerte.ERZEUGER_PHOTOVOLTAIK,
                         Kaskade.StromWert(k, Kaskade.PLATZ_STROMERZEUGER));

            Kaskade.StromAuswahl(k, Kaskade.PLATZ_STROMERZEUGER, "");
            Assert.Equal("", k.m_Tool_5);
        }

        /// <summary>„Gesamtsystem" haengt IMMER an — auch an einer leeren Kaskade.</summary>
        [Fact]
        public void Erzeugerliste_haengt_Gesamtsystem_immer_an()
        {
            Assert.Equal(new[] { DbWerte.ERZEUGER_GESAMTSYSTEM },
                         Kaskade.Erzeugerliste(new KonfigurationModel()));

            List<string> liste = Kaskade.Erzeugerliste(Modell("BHKW", "BHKW", "Heizkessel", ""));
            Assert.Equal(new[] { "BHKW", "Heizkessel", DbWerte.ERZEUGER_GESAMTSYSTEM }, liste);
        }

        [Theory]
        [InlineData(DbWerte.ERZEUGER_WAERMEPUMPE, WizardItemClass.WP_TYP)]
        [InlineData(DbWerte.ERZEUGER_HEIZKESSEL, WizardItemClass.KESSEL_TYP)]
        [InlineData(DbWerte.ERZEUGER_BHKW, WizardItemClass.BHKW_TYP)]
        [InlineData(DbWerte.ERZEUGER_SOLARTHERMIE, WizardItemClass.SOLAR_TYP)]
        [InlineData(DbWerte.ERZEUGER_GESAMTSYSTEM, 0)]
        public void TypZuAnlagentyp_bildet_die_vier_Waermeerzeuger_ab(string dbWert, int erwartet)
        {
            Assert.Equal(erwartet, Kaskade.TypZuAnlagentyp(dbWert));
        }
    }

    /// <summary>
    /// Die fuenf Controller-Wege der Simulationskonfiguration (iU9-W10b.0b) gegen
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>.
    ///
    /// <para><b>Warum eine Arbeitskopie und keine Transaktion.</b>
    /// <see cref="TestDatenbank"/> legt je Testklasse eine Kopie an und loescht sie
    /// danach; die Vergleichsbasis des Referenzlaufs bleibt unberuehrt, und auch der
    /// SCHREIBENDE Fall <see cref="QuelleSchreiben_schreibt_je_Zweig_nur_seine_Felder"/>
    /// braucht keinen Rueckbau.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationKonfigDatenTests
    {
        /// <summary>Projekt 1030: BHKW-Kaskade mit Kessel, zwei BHKW und einem Puffer.</summary>
        private const int PROJEKT = 1030;

        [Fact]
        public void AnlagenNamen_entdoppelt_und_ordnet_nach_Prioritaet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<string> namen = WErzeugerCtrl.AnlagenNamen(PROJEKT, WizardItemClass.BHKW_TYP);

            Assert.Equal(2, namen.Count);
            Assert.Equal(namen.Distinct().Count(), namen.Count);
            // Gepflegte Prioritaet (1) vor ungepflegter (NULL).
            Assert.StartsWith("BHKW EW M 50 S", namen[0]);

            Assert.Empty(WErzeugerCtrl.AnlagenNamen(0, WizardItemClass.BHKW_TYP));
            Assert.Empty(WErzeugerCtrl.AnlagenNamen(PROJEKT, 0));
        }

        [Fact]
        public void Quellnutzer_liefert_nur_Waermeerzeuger_mit_ID_und_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<AnlagenKurz> liste = WErzeugerCtrl.Quellnutzer(PROJEKT);

            Assert.NotEmpty(liste);
            Assert.All(liste, a => Assert.True(a.ID > 0));

            // Der Pufferspeicher (ID_Type 12) gehoert NICHT dazu - Invariante S-1.
            Assert.DoesNotContain(liste, a => a.Bezeichner.StartsWith("Pufferspeicher 20 m3"));

            Assert.Empty(WErzeugerCtrl.Quellnutzer(0));
        }

        [Fact]
        public void AnlagenMitWp_traegt_Temperaturpaar_und_Senkenkette()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<AnlagenInfo> kessel =
                WErzeugerCtrl.AnlagenMitWp(PROJEKT, WizardItemClass.KESSEL_TYP);

            Assert.Single(kessel);
            AnlagenInfo a = kessel[0];
            Assert.True(a.ID > 0);
            Assert.Equal(WizardItemClass.KESSEL_TYP, a.ID_Type);
            Assert.NotEqual("", a.Bezeichner);
            Assert.False(a.IstWaermepumpe);

            // Nie leer: ohne eigene Zeile die Rang-1-Vorbelegung Heizkreis/Beides.
            Assert.NotEmpty(a.Senken);
            Assert.NotNull(a.SenkeAufRang(0));
            Assert.Equal(1, a.SenkeAufRang(0).Rang);
        }

        [Fact]
        public void AnlagenMitWp_liest_die_Bauart_der_Waermepumpe_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<AnlagenInfo> wp = WErzeugerCtrl.AnlagenMitWp(1007, WizardItemClass.WP_TYP);

            Assert.Single(wp);
            Assert.True(wp[0].IstWaermepumpe);
            Assert.Equal(35, wp[0].Vorlauf);
            Assert.Equal(25, wp[0].Ruecklauf);
        }

        [Fact]
        public void LetzteErgebnisId_liefert_den_juengsten_Lauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ErgebnisCtrl.LetzteErgebnisId(PROJEKT) > 0);
            Assert.Equal(0, ErgebnisCtrl.LetzteErgebnisId(0));
            Assert.Equal(0, ErgebnisCtrl.LetzteErgebnisId(999999));
        }

        [Fact]
        public void Aussentemperatur_liefert_8760_Werte_oder_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            float[] temp = KlimaregionCtrl.Aussentemperatur(PROJEKT);
            Assert.NotNull(temp);
            Assert.Equal(8760, temp.Length);

            Assert.Null(KlimaregionCtrl.Aussentemperatur(0));
            Assert.Null(KlimaregionCtrl.Aussentemperatur(999999));
        }

        [Fact]
        public void LiesProjekt_liefert_die_Kaskade_des_Projekts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KonfigurationModel k = KonfigurationCtrl.LiesProjekt(PROJEKT);
            Assert.NotNull(k);
            Assert.Equal(PROJEKT, k.m_ID_Projekt);
            Assert.Equal(new[] { DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL },
                         Kaskade.Belegt(k));

            Assert.Null(KonfigurationCtrl.LiesProjekt(0));
            Assert.Null(KonfigurationCtrl.LiesProjekt(999999));
        }

        /// <summary>
        /// Jeder Zweig fasst genau seine Felder an — und der Kesselzweig laesst die
        /// Verdampferwerte der Waermepumpe stehen (Befund W10-B15).
        /// </summary>
        [Fact]
        public void QuelleSchreiben_schreibt_je_Zweig_nur_seine_Felder()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<AnlagenInfo> kessel =
                WErzeugerCtrl.AnlagenMitWp(PROJEKT, WizardItemClass.KESSEL_TYP);
            if (kessel.Count == 0) return;
            int id = kessel[0].ID;

            // Ausgangslage: eine Verdampfertemperatur, die der Kesselzweig NICHT
            // anfassen darf.
            WaermequelleClass.WertSchreiben(id, "WQ_Temp", 7.5);

            Assert.True(WaermequelleClass.QuelleSchreiben(id, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_PUFFER,
                IstWaermepumpe = false,
                IdPuffer = 0,
                Pufferspeicher = "Speicher X",
                Quelltemperatur = 99.0,          // muss ignoriert werden
                TemperaturModus = DbWerte.WQ_TEMPMODUS_BERECHNET
            }));

            Assert.Equal(WaermequelleClass.TYP_PUFFER,
                         WaermequelleClass.WertLesen(id, "WQ_Typ") as string);
            Assert.Equal("Speicher X", WaermequelleClass.WertLesen(id, "WQ_Puffer") as string);
            Assert.Equal(7.5, System.Convert.ToDouble(
                             WaermequelleClass.WertLesen(id, "WQ_Temp")), 3);

            // Der Zweig „Systemruecklauf" baut die Kaskade ab.
            Assert.True(WaermequelleClass.QuelleSchreiben(id, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_OHNE
            }));
            Assert.Equal(WaermequelleClass.TYP_OHNE,
                         WaermequelleClass.WertLesen(id, "WQ_Typ") as string);
            Assert.Null(WaermequelleClass.WertLesen(id, "WQ_ID_Puffer"));

            // Der Zweig „konstant" schreibt Temperatur UND Typ.
            Assert.True(WaermequelleClass.QuelleSchreiben(id, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_KONSTANT,
                Temperatur = 12.5
            }));
            Assert.Equal(12.5, System.Convert.ToDouble(
                             WaermequelleClass.WertLesen(id, "WQ_Temp")), 3);
            Assert.Equal(WaermequelleClass.TYP_KONSTANT,
                         WaermequelleClass.WertLesen(id, "WQ_Typ") as string);

            // Der Erdreichzweig schreibt sieben Felder.
            Assert.True(WaermequelleClass.QuelleSchreiben(id, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_ERDREICH,
                Quellsystem = DbWerte.WQ_QUELLSYSTEM_KOLLEKTOR,
                Tiefe = 1.5,
                Flaeche = 240,
                Anzahl = 0,
                Bodentyp = "Lehm",
                SpreizungErdreich = 4
            }));
            Assert.Equal("Lehm", WaermequelleClass.WertLesen(id, "WQ_Bodentyp") as string);
            Assert.Equal(240.0, System.Convert.ToDouble(
                             WaermequelleClass.WertLesen(id, "WQ_Flaeche")), 3);

            // Ein unbekannter Typ schreibt nichts.
            Assert.False(WaermequelleClass.QuelleSchreiben(id, new QuelleErgebnis { Typ = "XYZ" }));
            Assert.False(WaermequelleClass.QuelleSchreiben(0, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_AUSSENLUFT
            }));
        }
    }
}
