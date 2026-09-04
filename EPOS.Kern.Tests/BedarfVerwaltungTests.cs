using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der eingefrorene Nachweis der BEDARFS- und GANGLINIENVERWALTUNG</b> (iU9-W14b.0i).
    ///
    /// <para><b>Warum es diese Sammlung gibt.</b> Welle 14b loest vier Masken ab —
    /// <c>Form_Brauchwasser_Admin</c>, <c>Form_Prozesswaerme_Admin</c>,
    /// <c>Form_Stromverbraucher_Admin</c> und <c>Form_Solarganglinie_Admin</c> —, und fuer
    /// keine einzige von ihnen gibt es ein Netz: kein Referenzlauf (sie pflegen Kataloge,
    /// die der Lauf ueber die Projektzuordnung liest), keine ChartProbe (null Grafiken)
    /// und bis hierher keinen Kern-Test (Befund W14-B77). Was diese Masken heute rechnen
    /// und anzeigen, steht deshalb hier — Zahl fuer Zahl, gemessen am Bestand vom
    /// 04.09.2026 VOR der ersten portierten Zeile.</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN.</b> Aendert sich eine, ist das kein
    /// Testfehler, sondern eine Verhaltensaenderung der Verwaltung — und gehoert als
    /// A-Zeile ins Portprotokoll.</para>
    ///
    /// <para><b>Die drei Vorrechnungen stehen hier WOERTLICH so, wie die Masken sie
    /// fuehren</b> (<c>btn_Simulation_Click</c>): Brauchwasser OHNE Teiler, Prozesswaerme
    /// und Stromverbraucher MIT <c>/1000</c>, der Strom zusaetzlich mit
    /// <c>Array.Copy</c> und <c>Maximaler_Strombedarf</c>. Sie sind der Massstab, an dem
    /// sich <see cref="BedarfsVorschauCtrl"/> (W14b.0b) messen lassen muss.</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Faelle</b> (<see cref="TestDatenbank"/>); die
    /// Arbeitskopie wird je KLASSE geteilt und nur GELESEN (Regel seit W11a). Die beiden
    /// schreibenden Faelle stehen am Ende und legen sich ihren eigenen Satz an.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfVerwaltungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public BedarfVerwaltungTests(TestDatenbank db) { _db = db; }

        private static readonly BedarfsArt[] ALLE =
        {
            BedarfsArt.Stromverbraucher, BedarfsArt.Prozesswaerme, BedarfsArt.Brauchwasser
        };

        // ==================================================================
        //  1 — Die Jahressumme: die zwoelf Monatswerte, eingefroren
        // ==================================================================

        /// <summary>
        /// <c>Prozesssumme</c> der drei Masken (dreimal wortgleich, Befund W14-B53) ist
        /// <see cref="BedarfStammCtrl.Jahressumme"/>. Die Werte stammen aus
        /// <c>Tab_*_STAMM</c> der Testdatenbank.
        /// </summary>
        [Theory]
        [InlineData("EFH Wohnen, 1 Person", 0.7429)]
        [InlineData("Hotel Ferien/Freizeit, 1 Zimmer", 1.2735)]
        [InlineData("Haushalt-3", 4.0597)]
        public void Jahressumme_Brauchwasser_ist_eingefroren(string name, double erwartet)
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(erwartet, BedarfStammCtrl.Jahressumme(BedarfsArt.Brauchwasser, name), 6);
        }

        [Theory]
        [InlineData("CONT", 365.0)]
        [InlineData("Beckenwasseraufheizung", 365.0)]
        [InlineData("Beckenwasseraufheizung2", 548.0)]
        public void Jahressumme_Prozesswaerme_ist_eingefroren(string name, double erwartet)
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(erwartet, BedarfStammCtrl.Jahressumme(BedarfsArt.Prozesswaerme, name), 6);
        }

        [Theory]
        [InlineData("Büro_Konst", 365.0)]
        [InlineData("Berger-Fertigung", 5136.0)]
        [InlineData("Berger-Fertigung-doppel", 10260.0)]
        public void Jahressumme_Stromverbraucher_ist_eingefroren(string name, double erwartet)
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(erwartet, BedarfStammCtrl.Jahressumme(BedarfsArt.Stromverbraucher, name), 6);
        }

        /// <summary>
        /// Die ZWOELF Monatswerte einer Probe je Art — die Summe allein wuerde einen
        /// vertauschten Monat nicht bemerken.
        /// </summary>
        [Fact]
        public void Monatswerte_je_Art_sind_eingefroren()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(
                new[] { 0.0706, 0.0647, 0.0710, 0.0623, 0.0654, 0.0577,
                        0.0472, 0.0566, 0.0581, 0.0570, 0.0640, 0.0683 },
                BedarfStammCtrl.Monatswerte(BedarfsArt.Brauchwasser, "EFH Wohnen, 1 Person"));

            var tage = new[] { 31.0, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            Assert.Equal(tage, BedarfStammCtrl.Monatswerte(BedarfsArt.Prozesswaerme, "CONT"));
            Assert.Equal(tage, BedarfStammCtrl.Monatswerte(BedarfsArt.Stromverbraucher, "Büro_Konst"));
        }

        /// <summary>
        /// Einen Satz, den es nicht gibt, wertet die Maske mit 0 — dort lief die Schleife
        /// bei <c>rows == 0</c> gar nicht erst.
        /// </summary>
        [Fact]
        public void Jahressumme_eines_unbekannten_Satzes_ist_null()
        {
            if (!_db.Vorhanden) return;
            foreach (BedarfsArt art in ALLE)
                Assert.Equal(0.0, BedarfStammCtrl.Jahressumme(art, "gibt-es-nicht-" + Guid.NewGuid()));
        }

        /// <summary>
        /// Die drei Anzeigeformate der Jahressumme, wörtlich je Art (Befund W14-B57):
        /// <c>"F3"</c> beim Brauchwasser, OHNE Format bei der Prozesswaerme, <c>"F2"</c>
        /// beim Stromverbraucher. Die Kultur wird im Rumpf gesetzt und im
        /// <c>finally</c> zurueckgestellt (Regel seit W8).
        /// </summary>
        [Fact]
        public void Die_drei_Anzeigeformate_der_Jahressumme_sind_eingefroren()
        {
            if (!_db.Vorhanden) return;

            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.Equal("0,743",
                    BedarfStammCtrl.Jahressumme(BedarfsArt.Brauchwasser, "EFH Wohnen, 1 Person").ToString("F3"));
                Assert.Equal("365",
                    BedarfStammCtrl.Jahressumme(BedarfsArt.Prozesswaerme, "CONT").ToString());
                Assert.Equal("365,00",
                    BedarfStammCtrl.Jahressumme(BedarfsArt.Stromverbraucher, "Büro_Konst").ToString("F2"));
            }
            finally { CultureInfo.CurrentCulture = vorher; }
        }

        // ==================================================================
        //  2 — Die Liste und der Kopf: was SetControls und SetProzessInfo zeigen
        // ==================================================================

        /// <summary>
        /// Die Listenspalte je Art: Prozesswaerme fuellt aus <c>m_szProzessname</c>, die
        /// beiden anderen aus <c>m_szBezeichner</c> — dieselbe DB-Spalte
        /// <c>Bezeichner</c>, nur andere Modellfelder. Die Satzzahlen sind eingefroren.
        /// </summary>
        [Fact]
        public void Die_Listen_der_drei_Masken_sind_eingefroren()
        {
            if (!_db.Vorhanden) return;

            var bw = new BrauchwasserStammCtrl();
            bw.ReadAll();
            Assert.Equal(16, bw.rows);
            Assert.Equal("Büro/Verwaltung, 1 Person", bw.items[0].m_szBezeichner);

            var pw = new ProzesswaermeStammCtrl();
            pw.ReadAll();
            Assert.Equal(32, pw.rows);
            Assert.Equal("Beckenwasseraufheizung", pw.items[0].m_szProzessname);

            var sv = new StromverbraucherStammCtrl();
            sv.ReadAll();
            Assert.Equal(41, sv.rows);
            Assert.Equal("Berger-Fertigung", sv.items[0].m_szBezeichner);
        }

        /// <summary>
        /// <c>SetProzessInfo</c> zeigt Beschreibung und Typ des gewaehlten Satzes; ein
        /// unbekannter Name laesst die Felder STEHEN (die Maske prueft <c>rows &gt; 0</c>).
        /// </summary>
        [Fact]
        public void Der_Kopf_eines_Satzes_ist_eingefroren()
        {
            if (!_db.Vorhanden) return;

            var bw = new BrauchwasserStammCtrl();
            bw.ReadSingle("Haushalt-3");
            Assert.Equal("Test", bw.m_szTyp);
            Assert.Equal("3 Personenhaushalt mit 2,5 MWh jährlichem Stromverbrauch", bw.m_szBeschreibung);

            var pw = new ProzesswaermeStammCtrl();
            pw.ReadSingle("CONT");
            Assert.Equal("CONT", pw.m_szTyp);
            Assert.Equal("Wärmebedarf ist jahreszeitlich unabhängig", pw.m_szBeschreibung);

            var sv = new StromverbraucherStammCtrl();
            sv.ReadSingle("Büro_Konst");
            Assert.Equal("Konst", sv.m_szTyp);
            Assert.Equal("Stromberdarf ist jahreszeitlich unabhängig", sv.m_szBeschreibung);
        }

        /// <summary>
        /// <see cref="BedarfStammCtrl.Bezeichner"/> (W14b.0a) liefert GENAU die Liste, die
        /// <c>SetControls</c> gefuellt hat — Reihenfolge und Inhalt.
        /// </summary>
        [Fact]
        public void Bezeichner_liefert_die_Liste_der_Maske()
        {
            if (!_db.Vorhanden) return;

            var bw = new BrauchwasserStammCtrl();
            bw.ReadAll();
            Assert.Equal(Enumerable.Range(0, bw.rows).Select(i => bw.items[i].m_szBezeichner).ToList(),
                         BedarfStammCtrl.Bezeichner(BedarfsArt.Brauchwasser));

            var pw = new ProzesswaermeStammCtrl();
            pw.ReadAll();
            Assert.Equal(Enumerable.Range(0, pw.rows).Select(i => pw.items[i].m_szProzessname).ToList(),
                         BedarfStammCtrl.Bezeichner(BedarfsArt.Prozesswaerme));

            var sv = new StromverbraucherStammCtrl();
            sv.ReadAll();
            Assert.Equal(Enumerable.Range(0, sv.rows).Select(i => sv.items[i].m_szBezeichner).ToList(),
                         BedarfStammCtrl.Bezeichner(BedarfsArt.Stromverbraucher));

            Assert.Equal(16, BedarfStammCtrl.Bezeichner(BedarfsArt.Brauchwasser).Count);
            Assert.Equal(32, BedarfStammCtrl.Bezeichner(BedarfsArt.Prozesswaerme).Count);
            Assert.Equal(41, BedarfStammCtrl.Bezeichner(BedarfsArt.Stromverbraucher).Count);
        }

        /// <summary>
        /// <see cref="BedarfStammCtrl.Kopf"/> (W14b.0a) liefert dasselbe wie
        /// <c>SetProzessInfo</c>; einen Satz, den es nicht gibt, meldet er als
        /// <c>null</c> — der Vorlaeufer liess die Felder dann stehen.
        /// </summary>
        [Fact]
        public void Kopf_liefert_Beschreibung_und_Typ()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(("3 Personenhaushalt mit 2,5 MWh jährlichem Stromverbrauch", "Test"),
                         BedarfStammCtrl.Kopf(BedarfsArt.Brauchwasser, "Haushalt-3"));
            Assert.Equal(("Wärmebedarf ist jahreszeitlich unabhängig", "CONT"),
                         BedarfStammCtrl.Kopf(BedarfsArt.Prozesswaerme, "CONT"));
            Assert.Equal(("Stromberdarf ist jahreszeitlich unabhängig", "Konst"),
                         BedarfStammCtrl.Kopf(BedarfsArt.Stromverbraucher, "Büro_Konst"));

            foreach (BedarfsArt art in ALLE)
                Assert.Null(BedarfStammCtrl.Kopf(art, "gibt-es-nicht-" + Guid.NewGuid()));
        }

        /// <summary>
        /// <see cref="BedarfStammCtrl.Loeschen"/> (W14b.0a) prueft die ReadOnly-Sperre
        /// SELBST und meldet sie als Wert — der Stammcontroller haette dafuer einen
        /// modalen Kasten gezeigt. Der Fall schreibt und legt sich seinen Satz selbst an.
        /// </summary>
        [Fact]
        public void Loeschen_meldet_seine_drei_Ausgaenge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Auslieferungsbestand bleibt stehen.
            Assert.Equal(BedarfLoeschErgebnis.Schreibgeschuetzt,
                         BedarfStammCtrl.Loeschen(BedarfsArt.Brauchwasser, "Haushalt-3"));
            Assert.True(BedarfStammCtrl.Exists(BedarfsArt.Brauchwasser, "Haushalt-3"));

            // Ein selbst angelegter Satz geht.
            string name = "W14b-Probe-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var monat = new double[12];
            for (int m = 0; m < 12; m++) monat[m] = m + 1;
            Assert.True(BedarfStammCtrl.SaveHead(BedarfsArt.Prozesswaerme, name, "CONT", "Probe", monat, true));
            Assert.True(BedarfStammCtrl.Exists(BedarfsArt.Prozesswaerme, name));

            Assert.Equal(BedarfLoeschErgebnis.Geloescht,
                         BedarfStammCtrl.Loeschen(BedarfsArt.Prozesswaerme, name));
            Assert.False(BedarfStammCtrl.Exists(BedarfsArt.Prozesswaerme, name));

            // Ein leerer Name kommt gar nicht erst an die Datenbank.
            Assert.Equal(BedarfLoeschErgebnis.Fehlgeschlagen,
                         BedarfStammCtrl.Loeschen(BedarfsArt.Prozesswaerme, ""));
        }

        /// <summary>
        /// <c>Exists</c> und <c>IstReadOnly</c> — die beiden Sperren, an denen „Neues
        /// Profil" und „Loeschen" haengen. Der Brauchwasserkatalog ist der einzige der
        /// drei mit Auslieferungssaetzen.
        /// </summary>
        [Fact]
        public void Exists_und_IstReadOnly_sind_eingefroren()
        {
            if (!_db.Vorhanden) return;

            Assert.True(BedarfStammCtrl.Exists(BedarfsArt.Brauchwasser, "Haushalt-3"));
            Assert.True(BedarfStammCtrl.Exists(BedarfsArt.Prozesswaerme, "CONT"));
            Assert.True(BedarfStammCtrl.Exists(BedarfsArt.Stromverbraucher, "Büro_Konst"));

            foreach (BedarfsArt art in ALLE)
                Assert.False(BedarfStammCtrl.Exists(art, "gibt-es-nicht-" + Guid.NewGuid()));

            Assert.True(BedarfStammCtrl.IstReadOnly(BedarfsArt.Brauchwasser, "Haushalt-3"));
            Assert.False(BedarfStammCtrl.IstReadOnly(BedarfsArt.Brauchwasser, "Haushalt-3 neu"));
            Assert.False(BedarfStammCtrl.IstReadOnly(BedarfsArt.Prozesswaerme, "CONT"));
            Assert.False(BedarfStammCtrl.IstReadOnly(BedarfsArt.Stromverbraucher, "Büro_Konst"));
        }

        // ==================================================================
        //  3 — Die drei Vorrechnungen, woertlich wie in den Masken
        // ==================================================================

        /// <summary>
        /// <c>Form_Brauchwasser_Admin.btn_Simulation_Click</c>:79-85 — <b>OHNE</b> den
        /// Teiler 1000 (Befund W14-B49). Die Summe liegt damit in KILOWATTSTUNDEN, waehrend
        /// die Beschriftung der Maske „MWth" nennt; genau das ist der Anwenderentscheid
        /// W8-O-5 vom 04.09.2026 (Einheit am Wert, <see cref="Energieeinheit"/>).
        /// </summary>
        [Fact]
        public void Vorrechnung_Brauchwasser_ist_eingefroren()
        {
            if (!_db.Vorhanden) return;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = 0 };
            sim.Brauchwasserwaerme_berechnen(new List<string> { "EFH Wohnen, 1 Person" });

            Assert.Equal(8760, sim.brauchwasserwerte.Length);

            double summe = sim.brauchwasserwerte.Sum();
            Assert.Equal(742.9008, summe, 3);

            // Die Jahressumme des Katalogs in MWh mal 1000 — der Beleg, dass die Reihe
            // in kWh vorliegt und der Teiler in der Maske fehlte. Zwei Stellen: Die Reihe
            // ist float und wird 8 760-mal aufsummiert.
            Assert.Equal(BedarfStammCtrl.Jahressumme(BedarfsArt.Brauchwasser, "EFH Wohnen, 1 Person") * 1000,
                         summe, 2);

            WPPlan.Core.BhkwPlan.MonatsSumme(sim.brauchwasserwerte, sim.Waermebedarf_Brauchwasser_Monat,
                                             sim.mo_anfang, sim.mo_ende);
            Assert.Equal(0.0706, sim.Waermebedarf_Brauchwasser_Monat[0], 5);
            Assert.Equal(0.0683, sim.Waermebedarf_Brauchwasser_Monat[11], 5);
        }

        /// <summary>
        /// <c>Form_Prozesswaerme_Admin.btn_Simulation_Click</c>:92-99 — <b>MIT</b> Teiler.
        /// </summary>
        [Fact]
        public void Vorrechnung_Prozesswaerme_ist_eingefroren()
        {
            if (!_db.Vorhanden) return;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = 0 };
            sim.Prozesswaerme_berechnen(new List<string> { "CONT" });

            Assert.Equal(8760, sim.prozesswerte.Length);
            Assert.Equal(365000.0, sim.prozesswerte.Sum(), 1);
            Assert.Equal(365.0, sim.prozesswerte.Sum() / 1000, 4);

            WPPlan.Core.BhkwPlan.MonatsSumme(sim.prozesswerte, sim.Waermebedarf_Prozess_Monat,
                                             sim.mo_anfang, sim.mo_ende);
            Assert.Equal(31.0, sim.Waermebedarf_Prozess_Monat[0], 3);
            Assert.Equal(28.0, sim.Waermebedarf_Prozess_Monat[1], 3);
            Assert.Equal(31.0, sim.Waermebedarf_Prozess_Monat[11], 3);
        }

        /// <summary>
        /// <c>Form_Stromverbraucher_Admin.btn_Simulation_Click</c>:95-109 — die laengste
        /// der drei: Teiler, <c>Array.Copy</c> in das VIERTELstundenfeld (35 040 Plaetze,
        /// belegt werden die ersten 8 760) und <c>Maximaler_Strombedarf</c>.
        /// </summary>
        [Fact]
        public void Vorrechnung_Stromverbraucher_ist_eingefroren()
        {
            if (!_db.Vorhanden) return;

            var sim = new SimulationStrombedarf { m_ID_Projekt = 0 };
            float[] ergebnis = sim.Stromprofil_Strombedarf_berechnen(new List<string> { "Büro_Konst" });

            Assert.NotNull(ergebnis);
            Assert.Equal(8760, ergebnis.Length);
            Assert.Equal(365000.0, ergebnis.Sum(), 1);
            Assert.Equal(365.0, ergebnis.Sum() / 1000, 4);

            Array.Copy(ergebnis, sim.Strombedarf_viertelStundenwerte, ergebnis.Length);
            Assert.Equal(35040, sim.Strombedarf_viertelStundenwerte.Length);

            WPPlan.Core.BhkwPlan.MonatsSumme(sim.Strombedarf_viertelStundenwerte, sim.Strombedarf_monat,
                                             sim.mo_anfang, sim.mo_ende);
            Assert.Equal(31.0, sim.Strombedarf_monat[0], 3);
            Assert.Equal(31.0, sim.Strombedarf_monat[11], 3);

            Assert.Equal(41.666668, sim.Maximaler_Strombedarf(sim.Strombedarf_viertelStundenwerte), 5);
        }

        /// <summary>
        /// Ein Name, den der Katalog nicht kennt, ergibt in allen drei Zweigen eine LEERE
        /// Reihe — und beim Strom <b>kein</b> <c>null</c>: Die Null-Pruefung
        /// <c>Form_Stromverbraucher_Admin</c>:99 greift bei einem unbekannten Bezeichner
        /// also gar nicht (Befund W14-B78b, hier eingefroren).
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Bezeichner_ergibt_eine_leere_Reihe()
        {
            if (!_db.Vorhanden) return;

            var w = new SimulationWaermebedarf { m_ID_Projekt = 0 };
            w.Brauchwasserwaerme_berechnen(new List<string> { "gibt-es-nicht" });
            Assert.Equal(0.0, w.brauchwasserwerte.Sum(), 6);

            var p = new SimulationWaermebedarf { m_ID_Projekt = 0 };
            p.Prozesswaerme_berechnen(new List<string> { "gibt-es-nicht" });
            Assert.Equal(0.0, p.prozesswerte.Sum(), 6);

            var s = new SimulationStrombedarf { m_ID_Projekt = 0 };
            float[] ergebnis = s.Stromprofil_Strombedarf_berechnen(new List<string> { "gibt-es-nicht" });
            Assert.NotNull(ergebnis);
            Assert.Equal(0.0, ergebnis.Sum(), 6);
        }

        // ==================================================================
        //  3b — BedarfsVorschauCtrl: dieselben Zahlen aus dem Kern
        // ==================================================================

        /// <summary>
        /// <see cref="BedarfsVorschauCtrl.Rechnen"/> (W14b.0b) liefert je Art GENAU das,
        /// was die Maske gerechnet hat — die Zahlen oben, hier ein zweites Mal aus dem
        /// Kern.
        /// </summary>
        [Fact]
        public void Vorschau_Brauchwasser_ist_bitgleich_zur_Maske()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Brauchwasser, 0,
                                                            "EFH Wohnen, 1 Person");
            Assert.True(v.Erfolgreich);
            Assert.NotNull(v.Waerme);
            Assert.Null(v.Strom);

            // OHNE Teiler - der Wert liegt in kWh (Befund W14-B49 / Entscheid W8-O-5).
            Assert.Equal(v.Waerme.brauchwasserwerte.Sum(), v.Waerme.Waermebedarf_Brauchwasser, 6);
            Assert.Equal(742.9008, v.Waerme.Waermebedarf_Brauchwasser, 3);
            Assert.Equal(0.0706, v.Waerme.Waermebedarf_Brauchwasser_Monat[0], 5);
            Assert.Equal(0.0683, v.Waerme.Waermebedarf_Brauchwasser_Monat[11], 5);
        }

        [Fact]
        public void Vorschau_Prozesswaerme_ist_bitgleich_zur_Maske()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Prozesswaerme, 0, "CONT");
            Assert.True(v.Erfolgreich);
            Assert.NotNull(v.Waerme);

            // MIT Teiler.
            Assert.Equal(v.Waerme.prozesswerte.Sum() / 1000, v.Waerme.Waermebedarf_Prozess, 6);
            Assert.Equal(365.0, v.Waerme.Waermebedarf_Prozess, 4);
            Assert.Equal(31.0, v.Waerme.Waermebedarf_Prozess_Monat[0], 3);
            Assert.Equal(28.0, v.Waerme.Waermebedarf_Prozess_Monat[1], 3);
        }

        [Fact]
        public void Vorschau_Stromverbraucher_ist_bitgleich_zur_Maske()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Stromverbraucher, 0,
                                                            "Büro_Konst");
            Assert.True(v.Erfolgreich);
            Assert.NotNull(v.Strom);
            Assert.Null(v.Waerme);

            Assert.Equal(365.0, v.Strom.Strombedarf_Gebaeude_gesamt, 4);
            Assert.Equal(v.Strom.Strombedarf_Gebaeude_gesamt, v.Strom.Strombedarf_gesamt, 6);
            Assert.Equal(35040, v.Strom.Strombedarf_viertelStundenwerte.Length);
            Assert.Equal(31.0, v.Strom.Strombedarf_monat[0], 3);
            Assert.Equal(31.0, v.Strom.Strombedarf_monat[11], 3);
            Assert.Equal(41.666668, v.Strom.Strombedarf_Max, 5);
        }

        /// <summary>Ohne Bezeichner wird gar nicht gerechnet — der Dialog bleibt dann zu.</summary>
        [Fact]
        public void Vorschau_ohne_Bezeichner_rechnet_nicht()
        {
            foreach (BedarfsArt art in ALLE)
            {
                BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(art, 0, "");
                Assert.False(v.Erfolgreich);
                Assert.Null(v.Waerme);
                Assert.Null(v.Strom);
                Assert.Equal(art, v.Art);
            }
        }

        // ==================================================================
        //  4 — Der Solarganglinien-Katalog
        // ==================================================================

        /// <summary>
        /// <c>SetControls</c> von <c>Form_Solarganglinie_Admin</c>: die Kopfsaetze aus
        /// <c>Tab_Solarganglinie_STAMM</c>, sortiert nach <c>Bezeichner</c>.
        /// </summary>
        [Fact]
        public void Der_Solarganglinien_Katalog_ist_eingefroren()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new SolarganglinieStammCtrl();
            ctrl.ReadAll();

            Assert.Equal(1, ctrl.rows);
            Assert.Equal("Tsol1", ctrl.items[0].m_szBezeichner);
            Assert.Equal("Leistung Solarsystem [W]", ctrl.items[0].m_szBeschreibung);
            Assert.Equal(1, ctrl.items[0].ID);

            Assert.False(ctrl.IsReadOnly("Tsol1"));
            Assert.Equal(1, ctrl.GetStammId("Tsol1"));
            Assert.Equal(0, ctrl.GetStammId("gibt-es-nicht"));
        }

        /// <summary>
        /// <see cref="SolarganglinieStammCtrl.Exists"/> (W14b.0d) fragt die DATENBANK.
        /// Der Vorlaeufer nahm <c>listBox_Extern.FindString</c> — eine PRAEFIXsuche in
        /// der Anzeige (Befund W14-B70): „Tsol" haette dort „Tsol1" getroffen und den
        /// Import abgelehnt, obwohl der Name frei ist.
        /// </summary>
        [Fact]
        public void Exists_der_Solarganglinie_ist_keine_Praefixsuche()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new SolarganglinieStammCtrl();

            Assert.True(ctrl.Exists("Tsol1"));
            Assert.False(ctrl.Exists("Tsol"));          // der Praefix trifft NICHT mehr
            Assert.False(ctrl.Exists("Tsol1_2026"));
            Assert.False(ctrl.Exists(""));
            Assert.False(ctrl.Exists(null));
        }

        /// <summary>
        /// <see cref="SolarganglinieStammCtrl.HatProjektzuordnung"/> (W14b.0d) — die
        /// Sperre vor dem Loeschen. In der Testdatenbank ist
        /// <c>Z_ProjektSolarganglinie</c> leer, also sperrt nichts.
        /// </summary>
        [Fact]
        public void HatProjektzuordnung_sperrt_nur_zugeordnete_Ganglinien()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new SolarganglinieStammCtrl();
            Assert.False(ctrl.HatProjektzuordnung("Tsol1"));
            Assert.False(ctrl.HatProjektzuordnung("gibt-es-nicht"));
            Assert.False(ctrl.HatProjektzuordnung(null));
        }

        /// <summary>
        /// Der ganze Weg des Knopfes „Datei Einlesen…" auf einer eigenen Arbeitskopie:
        /// lesen, Dublettenpruefung, schreiben, wiederfinden, loeschen. Der Fall
        /// SCHREIBT und legt sich deshalb seine eigene Kopie an.
        /// </summary>
        [Fact]
        public void Der_Einleseweg_der_Solarganglinie_traegt_Kopf_und_8760_Werte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GanglinienTextErgebnis datei = GanglinienTextDatei.Lies(Probe("solarganglinie_8760.txt"),
                                                                    mitKopfzeile: true);
            Assert.True(datei.Erfolgreich);

            var ctrl = new SolarganglinieStammCtrl();
            string name = "W14b-SG-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            Assert.False(ctrl.Exists(name));
            Assert.True(ctrl.ImportGanglinie(name, datei.Beschreibung, datei.Werte));
            Assert.True(ctrl.Exists(name));

            ctrl.ReadAll();
            SolarganglinieModel satz = ctrl.items.First(m => m.m_szBezeichner == name);
            Assert.Equal("Solarganglinie Sued 45 Grad, Leistung Solarsystem [W]", satz.m_szBeschreibung);

            Assert.False(ctrl.HatProjektzuordnung(name));
            Assert.True(ctrl.Delete(name));
            Assert.False(ctrl.Exists(name));
        }

        // ==================================================================
        //  5 — Die Ganglinien-Textdatei MIT Kopfzeile
        // ==================================================================

        /// <summary>
        /// Das Format der Solarganglinie: erste Zeile = Beschreibung, danach 8 760 Werte
        /// (<c>Form_Solarganglinie_Admin</c>:135-138). Gelesen wird es seit W13.0h von
        /// <see cref="GanglinienTextDatei"/> mit <c>mitKopfzeile: true</c>; die Werte sind
        /// gegen <c>ToolsClass.OpenText</c> eingefroren, VOR dessen Loeschung.
        /// </summary>
        [Fact]
        public void Die_Ganglinie_mit_Kopfzeile_wird_wie_bisher_gelesen()
        {
            GanglinienTextErgebnis erg = GanglinienTextDatei.Lies(Probe("solarganglinie_8760.txt"),
                                                                  mitKopfzeile: true);

            Assert.True(erg.Erfolgreich);
            Assert.Equal("Solarganglinie Sued 45 Grad, Leistung Solarsystem [W]", erg.Beschreibung);
            Assert.Equal(8760, erg.Werte.Count);
            Assert.Equal("51.470", erg.Werte[0]);
            Assert.Equal("52.199", erg.Werte[8759]);
            Assert.Empty(erg.Meldungen);
        }

        /// <summary>
        /// Die kleine Probe aus W13 (Kopfzeile + 24 Werte) — sie belegt, dass der Schalter
        /// die Zeilenzahl nicht voraussetzt.
        /// </summary>
        [Fact]
        public void Auch_die_kurze_Probe_traegt_ihre_Kopfzeile()
        {
            GanglinienTextErgebnis erg = GanglinienTextDatei.Lies(Probe("ganglinie_mit_kopfzeile.txt"),
                                                                  mitKopfzeile: true);

            Assert.True(erg.Erfolgreich);
            Assert.Equal("Sued 45 Grad, Referenzjahr 2026", erg.Beschreibung);
            Assert.Equal(24, erg.Werte.Count);
            Assert.Equal("51.470", erg.Werte[0]);
            Assert.Equal("52.274", erg.Werte[23]);
        }

        /// <summary>
        /// <b>Dieselbe Datei OHNE Schalter</b> — der Waermebedarfsweg (W13.2): Dann ist die
        /// Kopfzeile ein WERT, und es sind 8 761 statt 8 760. Der Schalter ist also nicht
        /// Zierde, sondern der Unterschied zwischen beiden Katalogen.
        /// </summary>
        [Fact]
        public void Ohne_Schalter_zaehlt_die_Kopfzeile_als_Wert()
        {
            GanglinienTextErgebnis erg = GanglinienTextDatei.Lies(Probe("solarganglinie_8760.txt"),
                                                                  mitKopfzeile: false);

            Assert.True(erg.Erfolgreich);
            Assert.Equal("", erg.Beschreibung);
            Assert.Equal(8761, erg.Werte.Count);
        }

        /// <summary>
        /// Die drei Gegenproben. <c>ToolsClass.OpenText</c> lieferte bei Semikolon und
        /// Komma <c>false</c> und ZEIGTE dabei selbst einen Dialog; bei einer LEERZEILE
        /// warf es eine <c>ArgumentOutOfRangeException</c> mitten im Parser (Befund
        /// W14-B72 / W13-B11). Hier kommt in allen drei Faellen eine Meldung mit
        /// Zeilennummer zurueck.
        /// </summary>
        [Theory]
        [InlineData("waermebedarf_gegenprobe_semikolon.txt", "IMP_TXT_TRENNZEICHEN")]
        [InlineData("waermebedarf_gegenprobe_komma.txt", "IMP_TXT_TRENNZEICHEN")]
        [InlineData("waermebedarf_gegenprobe_leerzeile.txt", "IMP_TXT_LEERZEILE")]
        public void Die_Gegenproben_melden_statt_zu_werfen(string datei, string schluessel)
        {
            GanglinienTextErgebnis erg = GanglinienTextDatei.Lies(Probe(datei), mitKopfzeile: true);

            Assert.False(erg.Erfolgreich);
            Assert.Single(erg.Meldungen);
            Assert.Equal(schluessel, erg.Meldungen[0].Schluessel);
            Assert.Equal(PruefStufe.Fehler, erg.Meldungen[0].Stufe);
        }

        /// <summary>Ein leerer Pfad ergibt einen Misserfolg ohne Wurf.</summary>
        [Fact]
        public void Ein_leerer_Pfad_wirft_nicht()
        {
            GanglinienTextErgebnis erg = GanglinienTextDatei.Lies("", mitKopfzeile: true);
            Assert.False(erg.Erfolgreich);
            Assert.Equal("IMP_TXT_KEIN_PFAD", erg.Meldungen[0].Schluessel);
        }

        // ==================================================================
        //  Zugang zu den Proben
        // ==================================================================

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Importproben</c> aufwaerts vom Laufordner — dasselbe
        /// Vorgehen wie <c>KatalogImportTests.Ordner</c>.
        /// </summary>
        private static string Probe(string name)
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", name);
                if (File.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Die Probe fehlt: " + name);
            return name;
        }
    }
}
