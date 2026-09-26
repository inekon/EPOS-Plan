using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// <b>Die Tww-Kataloge des Zapfprofilgenerators in der Vorlage</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.2, 6 (b), (c); Stufe Z0, Posten P10): die eigene Regel
    /// unabhängig von <c>--kataloge</c>, die Prüfposten und das Katalogpaket.
    ///
    /// <para>Quelle ist eine KOPIE der Testdatenbank; ihr fiktiver Testkatalog (Status
    /// <c>EIGEN</c>, Herkunftsart <c>FIKTIV</c>) muss aus der Vorlage fallen. Wo eine Probe
    /// Auslieferungszeilen braucht, legt sie sie in der Kopie bzw. im Katalogpaket mit
    /// erfundenen, runden Werten an (Herkunftsart <c>EIGENKONSTRUKTION</c>) — nie in der
    /// Testdatenbank selbst. Das Katalogpaket liegt im Temp-Ordner, außerhalb des
    /// Repositorys, wie es die Option verlangt.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class TwwVorlageTests
    {
        private const string QUELLE = "Katalogpaket (erfunden)";
        private const string VERSION = "PAKET-1";

        // =============================================================================
        //  Die Regel
        // =============================================================================
        [Fact]
        public void T1_Die_Tww_Regel_laesst_nur_AUSLIEFERUNG_auch_bei_Kataloge_readonly()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Bearbeiten(quelle, () =>
            {
                long satz = SatzAnlegen("Probe Satz", TwwSchema.STATUS_AUSLIEFERUNG);
                // ReadOnly 0: Die ReadOnly-Regel darf sie trotzdem nicht treffen.
                long nutzung = NutzungAnlegen("Probe Nutzung", TwwSchema.STATUS_AUSLIEFERUNG, satz, readOnly: 0);
                NutzungAnlegen("Probe Import", TwwSchema.STATUS_IMPORT, satz, readOnly: 0);

                // Eine Zone samt Wohnungstyp in einem Projekt: Projektdaten, fallen in Schritt 2.
                long projekt = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Projekt"));
                long zone = DataRepository.ExecuteInsertAndGetId(
                    "INSERT INTO Tab_TwwZone (ID_Projekt, ID_Nutzungsart, Reihenfolge, Name, Bezugsmenge) VALUES (?, ?, 1, 'Z', 10.0)",
                    new[] { new DbParam("?", projekt), new DbParam("?", nutzung) });
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwWohnungstyp (ID_Zone, Anzahl, Reihenfolge) VALUES (?, 2, 1)", new DbParam("?", zone)));
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly", "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);

            Assert.Contains("Schritt 3c — Zapfprofil-Kataloge (Tww)", e.Ausgabe);
            Assert.Contains("ok      Fremdschluessel eingeschaltet", e.Ausgabe);
            Assert.Contains("ok      keine Zeile mit Status IMPORT", e.Ausgabe);
            Assert.Contains("ok      nur Status AUSLIEFERUNG", e.Ausgabe);
            Assert.Contains("ok      keine verwaiste Zeile", e.Ausgabe);
            Assert.Contains("ok      keine Zeile mit Herkunftsart FIKTIV", e.Ausgabe);
            Assert.Contains("ok      keine Zeile aus einem Normimport", e.Ausgabe);
            Assert.Contains("ok      keine Eingabe aus den lokalen Normdaten (ZU11, Referenzlaeufe/Normzahlen/", e.Ausgabe);
            // Satz und Nutzungsart der Quelle, der Vorgabesatz ihrer Gruppe (Wohnen) und der ganze
            // freie Paketteil (Parameter, Bedarfstag, abgeleitete Saetze und Nutzungsarten samt ihren
            // Kategorien).
            Assert.Contains("Tww-Auslieferungszeilen (Status AUSLIEFERUNG): " +
                            (2 + PaketteilVorgabesatz(TwwSchema.KATEGORIENGRUPPE_WOHNEN) + PaketteilKoepfe()), e.Ausgabe);
            // Die Nutzungsart kam mit ReadOnly 0 — die Vorlage sperrt sie (Auslieferung ist unveraenderlich).
            Assert.Contains("ReadOnly = 1 gesetzt: 1 Zeile(n) mit Status AUSLIEFERUNG", e.Ausgabe);
            Assert.Contains("ok      jede Zeile mit Status AUSLIEFERUNG traegt ReadOnly = 1", e.Ausgabe);

            Lesen(ziel, () =>
            {
                Assert.Equal(new[] { "Probe Nutzung" }.Concat(PaketteilNamen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)).ToArray(),
                             Namen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
                Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT ReadOnly FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = 'Probe Nutzung'")));
                Assert.Equal(new[] { "Probe Satz" }.Concat(PaketteilNamen(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM)).ToArray(),
                             Namen(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
                Assert.Equal(4L + Paketteil(TwwSchema.TAB_TWW_TAGESGANG_STAMM).Count, Zahl(TwwSchema.TAB_TWW_TAGESGANG_STAMM));
                foreach (string t in new[]
                         {
                             TwwSchema.TAB_TWW_DIN4708_WERT_STAMM,
                             TwwSchema.TAB_TWW_ZONE, TwwSchema.TAB_TWW_WOHNUNGSTYP, TwwSchema.TAB_TWW_PROJEKT
                         })
                    Assert.True(Zahl(t) == 0, t + " ist nicht leer.");
                // Bedarfstage, Ereignisse und Parameter: nur der freie Paketteil.
                Assert.Equal(Paketteil(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM).Count, (int)Zahl(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM));
                Assert.Equal(Paketteil(TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM).Count, (int)Zahl(TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM));
                Assert.Equal(Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count, (int)Zahl(TwwSchema.TAB_TWW_PARAMETER_STAMM));
            });
        }

        // =============================================================================
        //  Das Katalogpaket
        // =============================================================================
        [Fact]
        public void T2_Das_Katalogpaket_ersetzt_den_Tww_Katalog_der_Quelle()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            string paket = PaketSchreiben(o, parameterStatus: TwwSchema.STATUS_AUSLIEFERUNG,
                                          parameterHerkunft: TwwSchema.HERKUNFT_EIGENKONSTRUKTION);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--katalogpaket", paket);
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Tww-Paket   " + paket, e.Ausgabe);
            Assert.Contains("eingespielt: " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv  ->  1 Zeile(n)", e.Ausgabe);
            Assert.Contains("eingespielt: " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv  ->  4 Zeile(n)", e.Ausgabe);
            Assert.Contains("eingespielt: " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ".csv  ->  2 Zeile(n)", e.Ausgabe);
            // Die fuenf Zeilen des Pakets, dazu der ganze freie Paketteil; an der Nutzungsart 60 tritt der
            // Vorgabesatz zurueck (sie fuehrt eigene Kategorien), an den abgeleiteten bindet er.
            Assert.Contains("Tww-Auslieferungszeilen (Status AUSLIEFERUNG): " + (5 + PaketteilKoepfe()), e.Ausgabe);
            Assert.Contains("Zapfkategorien (Vorgabesaetze, 6 Zeile(n) in 2 Gruppe(n)): an " +
                            Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM).Count + " Nutzungsart(en)", e.Ausgabe);

            Lesen(ziel, () =>
            {
                // Genau das Paket, mit seinen Ids — der fiktive Testkatalog ist fort.
                Assert.Equal(new[] { "Paketnutzung" }.Concat(PaketteilNamen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)).ToArray(),
                             Namen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
                Assert.Equal(40L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID = 60")));
                Assert.Equal(4L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = 40")));
                Assert.Equal(QUELLE + "; Satz", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT Quelle FROM Tab_TwwTagesgang_STAMM WHERE Tagtyp = 1")));
                Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT ReadOnly FROM Tab_TwwParameter_STAMM WHERE Schluessel = 'Paket.Probe'")));
                // Die Parameter des Paketteils treten der Katalogversion des Pakets bei.
                Assert.Equal(Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count, PaketteilParameter(" AND Katalogversion = ?", VERSION));
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwDin4708Wert_STAMM")));
                // Die Zapfkategorien des Pakets hängen an der Nutzungsart 60 und sind gesperrt.
                Assert.Equal(2L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart = 60 AND ReadOnly = 1")));
                Assert.Equal((long)PaketteilKategorien(), Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart <> 60")));
            });
        }

        /// <summary>
        /// Die Zapfkategorien (Schemaschritt T2) folgen der Tww-Regel wie ein Kopf: Es bleibt die
        /// Kategorie mit Status AUSLIEFERUNG an einer bleibenden Nutzungsart, und sie bekommt
        /// ReadOnly = 1; eine EIGENE und eine FIKTIVE fallen, die einer fallenden Nutzungsart
        /// (IMPORT) gehen mit ihr (Kaskade). Keine verwaiste Kategorie.
        /// </summary>
        [Fact]
        public void T8_Die_Zapfkategorien_folgen_der_Tww_Regel()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Bearbeiten(quelle, () =>
            {
                long satz = SatzAnlegen("Probe Satz", TwwSchema.STATUS_AUSLIEFERUNG);
                long nutzung = NutzungAnlegen("Probe Nutzung", TwwSchema.STATUS_AUSLIEFERUNG, satz, readOnly: 1);
                long import = NutzungAnlegen("Probe Import", TwwSchema.STATUS_IMPORT, satz, readOnly: 0);
                KategorieAnlegen(nutzung, "Bleibt", TwwSchema.STATUS_AUSLIEFERUNG, TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
                KategorieAnlegen(nutzung, "Eigen", TwwSchema.STATUS_EIGEN, TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
                KategorieAnlegen(nutzung, "Fiktiv", TwwSchema.STATUS_AUSLIEFERUNG, TwwSchema.HERKUNFT_FIKTIV);
                KategorieAnlegen(import, "Mit Import", TwwSchema.STATUS_AUSLIEFERUNG, TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly", "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("ok      keine verwaiste Zeile", e.Ausgabe);
            Assert.Contains("ok      nur Status AUSLIEFERUNG", e.Ausgabe);
            Assert.Contains("ok      jede Zeile mit Status AUSLIEFERUNG traegt ReadOnly = 1", e.Ausgabe);
            // Satz, Nutzungsart und die eine Kategorie (sie kam mit ReadOnly 0), dazu der ganze freie
            // Paketteil; an der Nutzungsart tritt der Vorgabesatz zurueck — sie fuehrt eine Kategorie.
            Assert.Contains("Tww-Auslieferungszeilen (Status AUSLIEFERUNG): " + (3 + PaketteilKoepfe()), e.Ausgabe);
            Assert.Contains("ReadOnly = 1 gesetzt: 1 Zeile(n) mit Status AUSLIEFERUNG", e.Ausgabe);

            Lesen(ziel, () =>
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT k.Kategorie, k.ReadOnly, n.Bezeichner FROM Tab_TwwZapfkategorie_STAMM k " +
                    "JOIN Tab_TwwNutzungsart_STAMM n ON n.ID = k.ID_Nutzungsart WHERE n.Bezeichner = 'Probe Nutzung'");
                DataRow r = Assert.Single(dt.Rows.Cast<DataRow>());
                Assert.Equal("Bleibt", Convert.ToString(r["Kategorie"]));
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"]));
                Assert.Equal("Probe Nutzung", Convert.ToString(r["Bezeichner"]));
            });
        }

        // =============================================================================
        //  Der freie Paketteil (Referenzlaeufe/Katalogpaket_frei)
        // =============================================================================

        /// <summary>
        /// Ohne Katalogpaket steht der freie Paketteil in der Vorlage: jede Zeile der CSV-Dateien mit
        /// ihren Werten, Herkunftsart FREI, Status AUSLIEFERUNG, ReadOnly 1 und der Rueckfallversion
        /// (die Quelle fuehrt nach der Tww-Regel keinen Parameter); die Ereignisse haengen am
        /// eingespielten Bedarfstag, keine Waise; der fiktive Testkatalog faellt.
        /// </summary>
        [Fact]
        public void T9_Der_freie_Paketteil_steht_in_jeder_Vorlage()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Freier Paketteil: " + Path.Combine(Werkzeuglauf.Repowurzel, "Referenzlaeufe", "Katalogpaket_frei"), e.Ausgabe);
            Assert.Contains("Katalogversion der Paketteil-Zeilen: FREI-1 (der Katalog fuehrt keine eigene)", e.Ausgabe);
            foreach (string t in new[] { TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, TwwSchema.TAB_TWW_TAGESGANG_STAMM,
                                         TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_PARAMETER_STAMM,
                                         TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM })
            {
                int n = Paketteil(t).Count;
                Assert.Contains("eingespielt: " + t + ".csv  ->  " + n + " von " + n + " Zeile(n)", e.Ausgabe);
            }
            Assert.Contains("ok      keine verwaiste Zeile", e.Ausgabe);
            Assert.Contains("ok      keine Zeile mit Herkunftsart FIKTIV", e.Ausgabe);
            Assert.Contains("ok      jede Zeile mit Status AUSLIEFERUNG traegt ReadOnly = 1", e.Ausgabe);
            Assert.Contains("ok      jeder Parameter des Paketteils ist ein Schluessel des Programms, in Einheit und Bereich (" +
                            Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count + ")", e.Ausgabe);
            Assert.Contains("Tww-Zeilen mit Herkunftsart FREI/VERFAHREN/EIGENKONSTRUKTION (freier Paketteil): " +
                            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " " + Paketteil(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM).Count +
                            ", " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " " + Paketteil(TwwSchema.TAB_TWW_TAGESGANG_STAMM).Count +
                            ", " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " " + Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM).Count +
                            ", " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " " + Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count,
                            e.Ausgabe);

            Lesen(ziel, () =>
            {
                // Parameter: Wert fuer Wert wie die Datei.
                foreach (Dictionary<string, string> z in Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM))
                {
                    DataRow r = Assert.Single(DataRepository.GetDataTable(
                        "SELECT * FROM Tab_TwwParameter_STAMM WHERE Schluessel = ?", new DbParam("?", z["Schluessel"])).Rows.Cast<DataRow>());
                    Assert.Equal(double.Parse(z["Wert"], CultureInfo.InvariantCulture), Convert.ToDouble(r["Wert"]));
                    foreach (string s in new[] { "Einheit", "Quelle", "Ausgabe", "Version" })
                        Assert.Equal(z[s], Convert.ToString(r[s]));
                    // FREI oder - die Setzungen der Speicherauslegung aus V4 (N27) - EIGENKONSTRUKTION, wie die Datei.
                    FreiUndGesperrt(r, z["Herkunftsart"]);
                    Assert.Equal(TwwKatalogversionFrei, Convert.ToString(r["Katalogversion"]));
                }
                // Bedarfstag samt Ereignissen.
                foreach (Dictionary<string, string> z in Paketteil(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM))
                {
                    DataRow r = Assert.Single(DataRepository.GetDataTable(
                        "SELECT * FROM Tab_TwwBedarfstag_STAMM WHERE Bezeichner = ?", new DbParam("?", z["Bezeichner"])).Rows.Cast<DataRow>());
                    Assert.Equal(long.Parse(z["Quelle_Art"], CultureInfo.InvariantCulture), Convert.ToInt64(r["Quelle_Art"]));
                    Assert.Equal(z["Quelle"], Convert.ToString(r["Quelle"]));
                    // Bezugsmenge und Bezugsart (Schritt 124) wie die Datei; leeres Feld = NULL.
                    foreach (string s in new[] { "Bezugsmenge", "Bezugsart" })
                        Assert.Equal(z[s], r[s] == DBNull.Value ? "" : Convert.ToString(r[s], CultureInfo.InvariantCulture));
                    FreiUndGesperrt(r);
                    var soll = Paketteil(TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM).Where(x => x["ID_Bedarfstag"] == z["ID"])
                        .Select(x => x["Minute_Beginn"] + "|" + x["Dauer_min"] + "|" + x["Energie_Kwh"] + "|" + x["Reihenfolge"]).ToArray();
                    var ist = DataRepository.GetDataTable(
                            "SELECT Minute_Beginn, Dauer_min, Energie_Kwh, Reihenfolge FROM Tab_TwwBedarfstagEreignis_STAMM " +
                            "WHERE ID_Bedarfstag = ? ORDER BY Reihenfolge", new DbParam("?", r["ID"])).Rows.Cast<DataRow>()
                        .Select(x => string.Join("|", x.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)))).ToArray();
                    Assert.Equal(soll, ist);
                }
                // Die Nutzungsarten dieser Vorlage sind genau die abgeleiteten des Paketteils (ZU20), und
                // jede traegt den Vorgabesatz ihrer Gruppe.
                Assert.Equal((long)Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM).Count, Zahl(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
                Assert.Equal((long)PaketteilKategorien(), Zahl(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM));
            });
        }

        /// <summary>
        /// Die Kategorien-Datei des Paketteils fuehrt einen Vorgabesatz JE GRUPPE (Steuerspalte
        /// Gruppe, Stufe Z5): Jede Nutzungsart mit Status AUSLIEFERUNG ohne eigene Kategorien bekommt
        /// den Satz ihrer Gruppe — Wohnen (Kalenderart 1) den Wohnsatz, Nichtwohnen (Kalenderart 2
        /// bis 5) den Nichtwohnsatz, Werte wie die Datei, Herkunftsart FREI, ReadOnly 1 —, eine
        /// Nutzungsart mit eigenen Kategorien behaelt allein ihre. Die Parameter treten der
        /// Katalogversion der Quelle bei.
        /// </summary>
        [Fact]
        public void T10_Die_Zapfkategorien_des_Paketteils_binden_an_Nutzungsarten_ohne_eigene()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Bearbeiten(quelle, () =>
            {
                long satz = SatzAnlegen("Probe Satz", TwwSchema.STATUS_AUSLIEFERUNG);
                NutzungAnlegen("Ohne Kategorien", TwwSchema.STATUS_AUSLIEFERUNG, satz, readOnly: 1);
                NutzungAnlegen("Nichtwohnen ohne Kategorien", TwwSchema.STATUS_AUSLIEFERUNG, satz, readOnly: 1, kalenderart: 2);
                long mit = NutzungAnlegen("Mit Kategorie", TwwSchema.STATUS_AUSLIEFERUNG, satz, readOnly: 1);
                KategorieAnlegen(mit, "Eigene", TwwSchema.STATUS_AUSLIEFERUNG, TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwParameter_STAMM (Schluessel, Wert, Einheit, Katalogversion, Quelle, Version, Herkunftsart, " +
                    "Status, ReadOnly) VALUES ('Quelle.Probe', 1.0, '-', ?, ?, ?, 'EIGENKONSTRUKTION', 'AUSLIEFERUNG', 1)",
                    new DbParam("?", VERSION), new DbParam("?", QUELLE), new DbParam("?", VERSION)));
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly", "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Katalogversion der Paketteil-Zeilen: " + VERSION + " (die des Katalogs)", e.Ausgabe);
            // Die zwei Nutzungsarten der Quelle ohne eigene Kategorien und die abgeleiteten des Paketteils.
            int wohnen = 1 + PaketteilArten(TwwSchema.KATEGORIENGRUPPE_WOHNEN);
            int nichtwohnen = 1 + PaketteilArten(TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN);
            Assert.Contains("Zapfkategorien (Vorgabesaetze, 6 Zeile(n) in 2 Gruppe(n)): an " + (wohnen + nichtwohnen) +
                            " Nutzungsart(en) mit Status AUSLIEFERUNG ohne eigene Kategorien gebunden — " +
                            nichtwohnen + " x Nichtwohnen (2 Kategorien), " +
                            wohnen + " x Wohnen (4 Kategorien); 1 Nutzungsart(en) fuehren eigene Kategorien des Katalogs", e.Ausgabe);
            Assert.Contains("ok      keine verwaiste Zeile", e.Ausgabe);

            Lesen(ziel, () =>
            {
                string Kategorien(string nutzung) => string.Join("\n", DataRepository.GetDataTable(
                        "SELECT k.Kategorie, k.Reihenfolge, k.Volumenstrom_l_min, k.Dauer_min, k.Anteil, k.Sigma, k.Kappung_l_min, " +
                        "k.Quelle, k.Ausgabe, k.Version, k.Herkunftsart, k.Status, k.ReadOnly FROM Tab_TwwZapfkategorie_STAMM k " +
                        "JOIN Tab_TwwNutzungsart_STAMM n ON n.ID = k.ID_Nutzungsart WHERE n.Bezeichner = ? ORDER BY k.Reihenfolge",
                        new DbParam("?", nutzung)).Rows.Cast<DataRow>()
                    .Select(r => string.Join("|", r.ItemArray.Select(v => v is bool w ? (w ? "1" : "0")
                                                                             : Convert.ToString(v, CultureInfo.InvariantCulture)))));
                string Soll(string gruppe) => string.Join("\n", Paketteil(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM)
                    .Where(z => z[TwwSchema.STEUERSPALTE_GRUPPE] == gruppe).Select(z => string.Join("|",
                    new[] { "Kategorie", "Reihenfolge", "Volumenstrom_l_min", "Dauer_min", "Anteil", "Sigma", "Kappung_l_min",
                            "Quelle", "Ausgabe", "Version", "Herkunftsart", "Status", "ReadOnly" }
                        .Select(s => s == "ReadOnly" ? "1" : Zahltext(z[s])))));
                Assert.Equal(Soll(TwwSchema.KATEGORIENGRUPPE_WOHNEN), Kategorien("Ohne Kategorien"));
                Assert.Equal(Soll(TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN), Kategorien("Nichtwohnen ohne Kategorien"));
                Assert.StartsWith("Eigene|", Kategorien("Mit Kategorie"));
                Assert.DoesNotContain("\n", Kategorien("Mit Kategorie"));
                Assert.Equal(Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count,
                             PaketteilParameter(" AND Katalogversion = ? AND ReadOnly = 1", VERSION));
            });
        }

        /// <summary>
        /// Schluesselgleichheit: Fuehrt das Katalogpaket einen Parameter des Paketteils (gleicher
        /// Schluessel, gleiche Katalogversion), gilt die Zeile des Pakets, und der Bericht meldet es;
        /// die uebrigen Zeilen des Paketteils kommen dazu.
        /// </summary>
        [Fact]
        public void T11_Eine_gleiche_Zeile_des_Katalogpakets_geht_vor_und_wird_gemeldet()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            string paket = PaketSchreiben(o, parameterStatus: TwwSchema.STATUS_AUSLIEFERUNG,
                                          parameterHerkunft: TwwSchema.HERKUNFT_EIGENKONSTRUKTION);
            string gleich = Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM)[0]["Schluessel"];
            File.AppendAllText(Path.Combine(paket, TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv"),
                gleich + ";99.0;-;" + VERSION + ";" + QUELLE + ";" + VERSION + ";EIGENKONSTRUKTION;AUSLIEFERUNG\n", new UTF8Encoding(false));

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--katalogpaket", paket);
            Assert.True(e.Code == 0, e.Alles);
            int n = Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count;
            Assert.Contains("eingespielt: " + TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv  ->  " + (n - 1) + " von " + n + " Zeile(n)", e.Ausgabe);
            Assert.Contains("MELDUNG " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " \"" + gleich + "\" (" + VERSION +
                            "): das Katalogpaket fuehrt dieselbe Zeile — die Zeile des Paketteils tritt zurueck", e.Ausgabe);

            Lesen(ziel, () =>
            {
                DataRow r = Assert.Single(DataRepository.GetDataTable(
                    "SELECT * FROM Tab_TwwParameter_STAMM WHERE Schluessel = ?", new DbParam("?", gleich)).Rows.Cast<DataRow>());
                Assert.Equal(99.0, Convert.ToDouble(r["Wert"]));
                Assert.Equal(TwwSchema.HERKUNFT_EIGENKONSTRUKTION, Convert.ToString(r["Herkunftsart"]));
                Assert.Equal(n - 1, PaketteilParameter("", null));
            });
        }

        /// <summary>
        /// <b>T12 (Stufe Z4b):</b> Die eingespielten Typtage des Anwenders
        /// (<c>Tab_TwwTyptag_IMPORT</c>, Schemaschritt T3 „Typtage") fallen aus der Vorlage —
        /// unabhängig von <c>--kataloge</c> —, der Bericht nennt es, und der Prüfposten
        /// „keine Zeile aus einem Normimport" bleibt grün. Die Zeilen der Quelle sind erfunden.
        /// </summary>
        [Fact]
        public void T12_Die_eingespielten_Typtage_fallen_aus_der_Vorlage()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Bearbeiten(quelle, () =>
            {
                long satz = SatzAnlegen("Probe Satz", TwwSchema.STATUS_AUSLIEFERUNG);
                NutzungAnlegen("Probe Nutzung", TwwSchema.STATUS_AUSLIEFERUNG, satz, readOnly: 1);

                // Drei erfundene Zeilen der eingespielten Typtage: Systematik, Anzahl, Kennwert.
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwTyptag_IMPORT (Art, Klimazone, Gebaeudeart, Typtag, Zeilenindex, Wert, " +
                    "Quelle, Datum_Import) VALUES (?, 0, '', 'PT1', 0, 1.0, 'Anwenderpaket (erfunden)', '2026-09-24')",
                    new DbParam("?", TwwSchema.TYPTAG_ART_KATEGORIE)));
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwTyptag_IMPORT (Art, Klimazone, Gebaeudeart, Typtag, Zeilenindex, Wert, " +
                    "Quelle, Datum_Import) VALUES (?, 3, 'probehaus', 'PT1', 0, 365.0, 'Anwenderpaket (erfunden)', '2026-09-24')",
                    new DbParam("?", TwwSchema.TYPTAG_ART_ANZAHL)));
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwTyptag_IMPORT (Art, Klimazone, Gebaeudeart, Typtag, Zeilenindex, Wert, " +
                    // Der Kennwert ist ERFUNDEN (ZU23): kein Wert einer Richtlinie im Repositorium.
                    "Quelle, Datum_Import) VALUES (?, 0, '', 'wintergrenze', 0, 3.0, 'Anwenderpaket (erfunden)', '2026-09-24')",
                    new DbParam("?", TwwSchema.TYPTAG_ART_KENNWERT)));
            });

            Lesen(quelle, () => Assert.Equal(3L, Zahl(TwwSchema.TAB_TWW_TYPTAG_IMPORT)));

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Tab_TwwTyptag_IMPORT geleert (Typtage des lizenzierten Anwenders, nie in der Vorlage)", e.Ausgabe);
            Assert.Contains("ok      keine Zeile aus einem Normimport", e.Ausgabe);

            Lesen(ziel, () =>
            {
                Assert.Equal(0L, Zahl(TwwSchema.TAB_TWW_TYPTAG_IMPORT));
                // Die Tabelle bleibt im Schema — geleert, nicht entfernt.
                Assert.True(DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TYPTAG_IMPORT));
            });
        }

        /// <summary>
        /// <b>T13 (ZU20):</b> Die fuenf aus VDI 6002 ABGELEITETEN Nutzungsarten des freien Paketteils
        /// stehen in der Vorlage — Status AUSLIEFERUNG, ReadOnly 1, Herkunftsart VERFAHREN in jeder
        /// Provenienzgruppe und die Quelle "abgeleitet aus VDI 6002 Blatt n" —, jede mit ihrem
        /// Tagesgangsatz, dessen vier Tagesgaengen und dem Vorgabesatz ihrer Gruppe. Die drei
        /// fiktiven Testnutzungen der Quelle sind fort, und der Pruefposten "keine Zeile mit
        /// Herkunftsart FIKTIV" bleibt gruen.
        /// </summary>
        [Fact]
        public void T13_Die_abgeleiteten_Nutzungsarten_stehen_in_der_Vorlage()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("ok      keine Zeile mit Herkunftsart FIKTIV", e.Ausgabe);
            Assert.Contains("ok      jede Zeile mit Status AUSLIEFERUNG traegt ReadOnly = 1", e.Ausgabe);

            Lesen(ziel, () =>
            {
                // Genau die Nutzungsarten des Paketteils, keine Testnutzung.
                Assert.Equal(PaketteilNamen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM), Namen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
                Assert.DoesNotContain(Namen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM), n => n.Contains("(fiktiv)"));
                Assert.Equal(PaketteilNamen(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM), Namen(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));

                foreach (Dictionary<string, string> z in Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM))
                {
                    DataRow r = Assert.Single(DataRepository.GetDataTable(
                        "SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ?",
                        new DbParam("?", z["Bezeichner"])).Rows.Cast<DataRow>());
                    Assert.Equal(TwwSchema.STATUS_AUSLIEFERUNG, Convert.ToString(r["Status"]));
                    Assert.True(r["ReadOnly"] is bool b ? b : Convert.ToInt64(r["ReadOnly"]) == 1, "ReadOnly ist nicht 1.");
                    Assert.Equal(TwwKatalogversionFrei, Convert.ToString(r["Katalogversion"]));
                    foreach (string g in new[] { "Bedarf", "Jahresgang", "Wochengang" })
                    {
                        Assert.Equal(TwwSchema.HERKUNFT_VERFAHREN, Convert.ToString(r[g + "_Herkunftsart"]));
                        Assert.StartsWith("abgeleitet aus VDI 6002 Blatt ", Convert.ToString(r[g + "_Quelle"]));
                        Assert.Equal(z[g + "_Quelle"], Convert.ToString(r[g + "_Quelle"]));
                    }
                    // Bedarf und Faktoren Wert fuer Wert wie die Datei.
                    foreach (string s in new[] { "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch", "Bezug_Zapftemperatur",
                                                 "Bezug_Kaltwasser", "Monat_1", "Monat_12", "Woche_1", "Woche_7" })
                        Assert.Equal(double.Parse(z[s], CultureInfo.InvariantCulture), Convert.ToDouble(r[s]), 12);
                    // Ihr Tagesgangsatz ist der des Paketteils, mit vier Tagesgaengen.
                    string satz = PaketteilNamen(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM)
                        [int.Parse(z["ID_Tagesgangsatz"], CultureInfo.InvariantCulture) - 1];
                    Assert.Equal(satz, Convert.ToString(DataRepository.ExecuteScalar(
                        "SELECT Bezeichner FROM Tab_TwwTagesgangsatz_STAMM WHERE ID = ?", new DbParam("?", r["ID_Tagesgangsatz"]))));
                    Assert.Equal(4L, Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ?", new DbParam("?", r["ID_Tagesgangsatz"]))));
                    // Der Vorgabesatz ihrer Gruppe haengt an ihr.
                    Assert.Equal((long)PaketteilVorgabesatz(TwwSchema.Kategoriengruppe(
                                     Convert.ToInt64(r["Kalenderart"], CultureInfo.InvariantCulture))),
                                 Convert.ToInt64(DataRepository.ExecuteScalar(
                                     "SELECT COUNT(*) FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart = ? AND ReadOnly = 1",
                                     new DbParam("?", r["ID"]))));
                }
                // Jeder Tagesgang traegt VERFAHREN und die abgeleitete Quelle.
                Assert.Equal((long)Paketteil(TwwSchema.TAB_TWW_TAGESGANG_STAMM).Count,
                             Convert.ToInt64(DataRepository.ExecuteScalar(
                                 "SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE Herkunftsart = ? AND " +
                                 "Quelle LIKE 'abgeleitet aus VDI 6002 Blatt %'", new DbParam("?", TwwSchema.HERKUNFT_VERFAHREN))));
            });
        }

        [Fact]
        public void T3_Ein_Katalogpaket_mit_Status_EIGEN_wird_benannt_abgelehnt()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            string paket = PaketSchreiben(o, parameterStatus: TwwSchema.STATUS_EIGEN,
                                          parameterHerkunft: TwwSchema.HERKUNFT_EIGENKONSTRUKTION);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--katalogpaket", paket);
            Assert.True(e.Code == 5, e.Alles);
            Assert.Contains(TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv Zeile 2: Status \"EIGEN\"", e.Fehlerausgabe);
            Assert.False(File.Exists(ziel), "Bei einem Abbruch darf keine Zieldatei entstehen.");
        }

        [Fact]
        public void T4_Ein_Katalogpaket_mit_Herkunftsart_FIKTIV_faellt_in_der_Pruefung()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string paket = PaketSchreiben(o, parameterStatus: TwwSchema.STATUS_AUSLIEFERUNG,
                                          parameterHerkunft: TwwSchema.HERKUNFT_FIKTIV);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, o.Datei("Kenndaten.sqlite"),
                                                           "--katalogpaket", paket, "--trocken");
            Assert.True(e.Code == 5, e.Alles);
            Assert.Contains("FEHLER  keine Zeile mit Herkunftsart FIKTIV", e.Ausgabe);
            Assert.Contains(TwwSchema.TAB_TWW_PARAMETER_STAMM + ".Herkunftsart: 1", e.Ausgabe);
        }

        [Fact]
        public void T5_Ein_Katalogpaket_im_Repository_oder_mit_fremder_Datei_wird_verweigert()
        {
            string quelle = Werkzeuglauf.Testdatenbank ?? Path.Combine(Path.GetTempPath(), "gibt-es-nicht.sqlite");
            string ziel = Path.Combine(Path.GetTempPath(), "vorlagenprobe-nie.sqlite");

            Werkzeuglauf.Ergebnis imRepo = Werkzeuglauf.Starten(
                quelle, ziel, "--katalogpaket", Path.Combine(Werkzeuglauf.Repowurzel, "Referenzlaeufe"), "--trocken");
            Assert.True(imRepo.Code == 2, imRepo.Alles);
            Assert.Contains("Das Katalogpaket liegt im Repository", imRepo.Fehlerausgabe);

            using var o = new Arbeitsordner();
            string fremd = Path.Combine(o.Pfad, "paket");
            Directory.CreateDirectory(fremd);
            File.WriteAllText(Path.Combine(fremd, "Tab_Gebaeude_STAMM.csv"), "ID\n1\n", new UTF8Encoding(false));
            Werkzeuglauf.Ergebnis falsch = Werkzeuglauf.Starten(quelle, ziel, "--katalogpaket", fremd, "--trocken");
            Assert.True(falsch.Code == 2, falsch.Alles);
            Assert.Contains("Tab_Gebaeude_STAMM.csv ist keine Tww-Katalogtabelle", falsch.Fehlerausgabe);
        }

        // =============================================================================
        //  Beispielpakete und lokale Normdaten
        // =============================================================================

        /// <summary>
        /// Ein Beispielpaket, dessen Zone eine Nutzungsart nutzt, die die Vorlage nicht führt
        /// (hier: die fiktive des Testkatalogs, die die Tww-Regel entfernt): Der Import nimmt
        /// sie mit Status IMPORT mit, die Pruefung faellt — und nennt das Paket.
        /// </summary>
        [Fact]
        public void T6_Ein_Beispielpaket_mit_fehlender_Nutzungsart_faellt_und_wird_genannt()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string werkbank = o.Datei("werkbank.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, werkbank);
            string beispiel = o.Datei("tww-beispiel.wpx");

            Bearbeiten(werkbank, () =>
            {
                long projekt = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", Vorlage.BEISPIELPROJEKT)));
                long nutzung = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_TwwNutzungsart_STAMM WHERE Status = ?", new DbParam("?", TwwSchema.STATUS_EIGEN)));
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwZone (ID_Projekt, ID_Nutzungsart, Reihenfolge, Name, Bezugsmenge) VALUES (?, ?, 1, 'Z', 10.0)",
                    new DbParam("?", projekt), new DbParam("?", nutzung)));
                Assert.True(new ProjektExportImportCtrl().Exportieren(Vorlage.BEISPIELPROJEKT, beispiel),
                            "Der Export des Beispielprojekts ist fehlgeschlagen.");
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, o.Datei("Kenndaten.sqlite"),
                                                           "--beispiele", beispiel, "--trocken");
            Assert.True(e.Code == 5, e.Alles);
            Assert.Contains("FEHLER  keine Zeile mit Status IMPORT", e.Ausgabe);
            Assert.Contains("mitgebracht von Beispielpaket tww-beispiel.wpx: Katalogzeile", e.Ausgabe);
        }

        /// <summary>
        /// Eine Eingabe unter <c>Referenzlaeufe/Normzahlen/</c> (ZU11) laesst die Pruefung
        /// fallen — hier die Quelle selbst, in einem Temp-Ordner mit diesem Pfadstueck.
        /// </summary>
        [Fact]
        public void T7_Eine_Eingabe_aus_den_lokalen_Normdaten_faellt_in_der_Pruefung()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string ordner = Path.Combine(o.Pfad, "Referenzlaeufe", "Normzahlen");
            Directory.CreateDirectory(ordner);
            string quelle = Path.Combine(ordner, "quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, o.Datei("Kenndaten.sqlite"), "--trocken");
            Assert.True(e.Code == 5, e.Alles);
            Assert.Contains("FEHLER  keine Eingabe aus den lokalen Normdaten (ZU11", e.Ausgabe);
            Assert.Contains(quelle, e.Ausgabe);
        }

        /// <summary>
        /// <b>T14 (N16, Restlücke):</b> Der Prüfposten zum Typtagweg. <b>Negativ:</b> Ein Lauf ohne
        /// Beispielpaket hat kein Projekt in der Vorlage — der Posten steht grün. <b>Positiv:</b> Ein
        /// Beispielpaket, dessen Projekt <c>Typtage_Aktiv = 1</c> trägt, fällt: Die Vorlage leert
        /// <c>Tab_TwwTyptag_IMPORT</c>, das Projekt liefe beim Anwender benannt auf eine Ablehnung
        /// (<c>ZapfEingabefehler.TyptageUngueltig</c>). Der Bericht nennt die Projektkennung.
        /// </summary>
        [Fact]
        public void T14_Ein_Beispielprojekt_auf_dem_Typtagweg_faellt_bei_leerer_Typtagtabelle()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);

            // Negativ: kein Beispielpaket, also kein Projekt in der Vorlage — der Posten ist gruen.
            Werkzeuglauf.Ergebnis ohne = Werkzeuglauf.Starten(quelle, o.Datei("Kenndaten.sqlite"), "--trocken");
            Assert.True(ohne.Code == 0, ohne.Alles);
            Assert.Contains("ok      kein Beispielprojekt mit " + TwwSchema.SPALTE_TYPTAGE_AKTIV + " = 1 bei leerer " +
                            TwwSchema.TAB_TWW_TYPTAG_IMPORT, ohne.Ausgabe);

            // Positiv: das Beispielprojekt traegt den Typtagweg, die Typtagtabelle bleibt leer.
            string werkbank = o.Datei("werkbank.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, werkbank);
            string beispiel = o.Datei("tww-typtag-beispiel.wpx");

            Bearbeiten(werkbank, () =>
            {
                long projekt = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", Vorlage.BEISPIELPROJEKT)));
                if (Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("?", projekt))) == 0)
                    Assert.True(DataRepository.ExecuteSQL("INSERT INTO Tab_TwwProjekt (ID_Projekt) VALUES (?)",
                                                          new DbParam("?", projekt)));
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Tab_TwwProjekt SET " + TwwSchema.SPALTE_TYPTAGE_AKTIV + " = 1, " +
                    TwwSchema.SPALTE_TYPTAGE_KLIMAZONE + " = 3, " + TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART +
                    " = 'probehaus' WHERE ID_Projekt = ?", new DbParam("?", projekt)));
                Assert.True(new ProjektExportImportCtrl().Exportieren(Vorlage.BEISPIELPROJEKT, beispiel),
                            "Der Export des Beispielprojekts ist fehlgeschlagen.");
            });

            Werkzeuglauf.Ergebnis mit = Werkzeuglauf.Starten(quelle, o.Datei("Kenndaten2.sqlite"),
                                                             "--beispiele", beispiel, "--trocken");
            Assert.True(mit.Code == 5, mit.Alles);
            Assert.Contains("FEHLER  kein Beispielprojekt mit " + TwwSchema.SPALTE_TYPTAGE_AKTIV + " = 1 bei leerer " +
                            TwwSchema.TAB_TWW_TYPTAG_IMPORT, mit.Ausgabe);
            Assert.Contains("Klimazone 3, Gebaeudeart probehaus", mit.Ausgabe);
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>
        /// Schreibt ein Katalogpaket mit erfundenen, runden Werten: ein Tagesgangsatz (Id 40,
        /// Trenner ';') mit vier Tagesgängen (Trenner ',', Quelle mit Trenner in
        /// Anführungszeichen), eine Nutzungsart (Id 60) und ein Parameter ohne Spalte
        /// ReadOnly, dessen Status und Herkunftsart die Probe wählt.
        /// </summary>
        private static string PaketSchreiben(Arbeitsordner o, string parameterStatus, string parameterHerkunft)
        {
            string ordner = Path.Combine(o.Pfad, "katalogpaket");
            Directory.CreateDirectory(ordner);
            var utf8 = new UTF8Encoding(false);

            File.WriteAllText(Path.Combine(ordner, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv"),
                "ID;Bezeichner;Katalogversion;Status;ReadOnly\n40;Paketsatz;" + VERSION + ";AUSLIEFERUNG;1\n", utf8);

            var tg = new StringBuilder();
            tg.Append("ID,ID_Tagesgangsatz,Tagtyp,")
              .Append(string.Join(",", Enumerable.Range(1, 24).Select(h => "Anteil_" + h.ToString("00", CultureInfo.InvariantCulture))))
              .Append(",Quelle,Version,Herkunftsart\n");
            for (int t = 1; t <= 4; t++)
                tg.Append(t + 40).Append(",40,").Append(t).Append(',')
                  .Append(string.Join(",", Enumerable.Range(1, 24).Select(h => h == 12 ? "1.0" : "0")))
                  .Append(",\"").Append(QUELLE).Append("; Satz\",").Append(VERSION).Append(",EIGENKONSTRUKTION\n");
            File.WriteAllText(Path.Combine(ordner, TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv"), tg.ToString(), utf8);

            var spalten = new List<string>
            {
                "ID", "Bezeichner", "Katalogversion", "Bezugsart", "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
                "Bedarf_Quelle", "Bedarf_Version", "Bedarf_Herkunftsart", "Bezug_Zapftemperatur", "Bezug_Kaltwasser",
                "Bilanzgrenze", "Kalenderart", "Jahresgang_Quelle", "Jahresgang_Version", "Jahresgang_Herkunftsart",
                "Wochengang_Quelle", "Wochengang_Version", "Wochengang_Herkunftsart", "ID_Tagesgangsatz", "Status", "ReadOnly"
            };
            var werte = new List<string>
            {
                "60", "Paketnutzung", VERSION, "1", "1", "2", "3",
                QUELLE, VERSION, "EIGENKONSTRUKTION", "50", "10",
                "1", "1", QUELLE, VERSION, "EIGENKONSTRUKTION",
                QUELLE, VERSION, "EIGENKONSTRUKTION", "40", "AUSLIEFERUNG", "1"
            };
            for (int m = 1; m <= 12; m++) { spalten.Add("Monat_" + m); werte.Add("1"); }
            for (int w = 1; w <= 7; w++) { spalten.Add("Woche_" + w); werte.Add(w <= 5 ? "0.2" : "0"); }
            File.WriteAllText(Path.Combine(ordner, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"),
                string.Join(";", spalten) + "\n" + string.Join(";", werte) + "\n", utf8);

            // Zwei Zapfkategorien der Nutzungsart 60, ohne Spalte ReadOnly (dann 1).
            File.WriteAllText(Path.Combine(ordner, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ".csv"),
                "ID_Nutzungsart;Kategorie;Reihenfolge;Volumenstrom_l_min;Dauer_min;Anteil;Sigma;Kappung_l_min;" +
                "Quelle;Version;Herkunftsart;Status\n" +
                "60;Paket kurz;1;4;1;0.5;1;;" + QUELLE + ";" + VERSION + ";EIGENKONSTRUKTION;AUSLIEFERUNG\n" +
                "60;Paket lang;2;8;5;0.5;2;12;" + QUELLE + ";" + VERSION + ";EIGENKONSTRUKTION;AUSLIEFERUNG\n", utf8);

            File.WriteAllText(Path.Combine(ordner, TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv"),
                "Schluessel;Wert;Einheit;Katalogversion;Quelle;Version;Herkunftsart;Status\n" +
                "Paket.Probe;1.0;-;" + VERSION + ";" + QUELLE + ";" + VERSION + ";" + parameterHerkunft + ";" +
                parameterStatus + "\n", utf8);
            return ordner;
        }

        private static long SatzAnlegen(string bezeichner, string status)
        {
            long id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_TwwTagesgangsatz_STAMM (Bezeichner, Katalogversion, Status, ReadOnly) VALUES (?, ?, ?, 1)",
                new[] { new DbParam("?", bezeichner), new DbParam("?", VERSION), new DbParam("?", status) });
            string spalten = string.Join(", ", Enumerable.Range(1, 24).Select(h => "Anteil_" + h.ToString("00", CultureInfo.InvariantCulture)));
            string werte = string.Join(", ", Enumerable.Range(1, 24).Select(h => h == 12 ? "1.0" : "0.0"));
            for (int t = 1; t <= 4; t++)
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_TwwTagesgang_STAMM (ID_Tagesgangsatz, Tagtyp, " + spalten + ", Quelle, Version, Herkunftsart) " +
                    "VALUES (?, ?, " + werte + ", ?, ?, 'EIGENKONSTRUKTION')",
                    new DbParam("?", id), new DbParam("?", t), new DbParam("?", QUELLE), new DbParam("?", VERSION)));
            return id;
        }

        private static long NutzungAnlegen(string bezeichner, string status, long satz, int readOnly, int kalenderart = 1)
        {
            var spalten = new List<string>
            {
                "Bezeichner", "Katalogversion", "Bezugsart", "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
                "Bedarf_Quelle", "Bedarf_Version", "Bedarf_Herkunftsart", "Bezug_Zapftemperatur", "Bezug_Kaltwasser",
                "Bilanzgrenze", "Kalenderart", "Jahresgang_Quelle", "Jahresgang_Version", "Jahresgang_Herkunftsart",
                "Wochengang_Quelle", "Wochengang_Version", "Wochengang_Herkunftsart", "ID_Tagesgangsatz", "Status", "ReadOnly"
            };
            var werte = new List<object>
            {
                bezeichner, VERSION, 1, 1.0, 2.0, 3.0,
                QUELLE, VERSION, "EIGENKONSTRUKTION", 50.0, 10.0,
                1, kalenderart, QUELLE, VERSION, "EIGENKONSTRUKTION",
                QUELLE, VERSION, "EIGENKONSTRUKTION", satz, status, readOnly
            };
            for (int m = 1; m <= 12; m++) { spalten.Add("Monat_" + m); werte.Add(1.0); }
            for (int w = 1; w <= 7; w++) { spalten.Add("Woche_" + w); werte.Add(w <= 5 ? 0.2 : 0.0); }
            return DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_TwwNutzungsart_STAMM (" + string.Join(", ", spalten) + ") VALUES (" +
                string.Join(", ", spalten.Select(_ => "?")) + ")",
                werte.Select(w => new DbParam("?", w)).ToArray());
        }

        private static void KategorieAnlegen(long nutzung, string kategorie, string status, string herkunft)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_TwwZapfkategorie_STAMM (ID_Nutzungsart, Kategorie, Reihenfolge, Volumenstrom_l_min, " +
                "Dauer_min, Anteil, Sigma, Quelle, Version, Herkunftsart, Status, ReadOnly) " +
                "VALUES (?, ?, 1, 4.0, 1, 1.0, 1.0, ?, ?, ?, ?, 0)",
                new DbParam("?", nutzung), new DbParam("?", kategorie), new DbParam("?", QUELLE),
                new DbParam("?", VERSION), new DbParam("?", herkunft), new DbParam("?", status)));
        }

        /// <summary>Die Rueckfallversion der Paketteil-Zeilen (TwwKataloge.KATALOGVERSION_FREI).</summary>
        private const string TwwKatalogversionFrei = "FREI-1";

        /// <summary>Der Vorgabesatz einer Gruppe im Paketteil (Steuerspalte <c>Gruppe</c>).</summary>
        private static int PaketteilVorgabesatz(string gruppe) =>
            Paketteil(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM).Count(z => z[TwwSchema.STEUERSPALTE_GRUPPE] == gruppe);

        /// <summary>
        /// Die Zapfkategorien, die der Paketteil an seine EIGENEN Nutzungsarten bindet (ZU20): je
        /// abgeleitete Nutzungsart der Vorgabesatz ihrer Gruppe, und die Gruppe folgt der Kalenderart.
        /// </summary>
        private static int PaketteilKategorien() =>
            Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)
                .Sum(z => PaketteilVorgabesatz(TwwSchema.Kategoriengruppe(
                    long.Parse(z["Kalenderart"], CultureInfo.InvariantCulture))));

        /// <summary>
        /// Die Kopfzeilen mit Status AUSLIEFERUNG, die der freie Paketteil selbst in jede Vorlage
        /// bringt: Tagesgangsaetze, Nutzungsarten, Parameter, Bedarfstage und die Zapfkategorien
        /// seiner Nutzungsarten (Tagesgaenge und Ereignisse sind keine Koepfe).
        /// </summary>
        private static int PaketteilKoepfe() =>
            Paketteil(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM).Count +
            Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM).Count +
            Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count +
            Paketteil(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM).Count +
            PaketteilKategorien();

        /// <summary>Die Nutzungsarten des Paketteils, deren Gruppe (aus der Kalenderart) die genannte ist.</summary>
        private static int PaketteilArten(string gruppe) =>
            Paketteil(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)
                .Count(z => TwwSchema.Kategoriengruppe(long.Parse(z["Kalenderart"], CultureInfo.InvariantCulture)) == gruppe);

        /// <summary>Die Bezeichner einer Datei des Paketteils in ihrer Reihenfolge.</summary>
        private static string[] PaketteilNamen(string tabelle) =>
            Paketteil(tabelle).Select(z => z["Bezeichner"]).ToArray();

        /// <summary>
        /// Die Zeilen einer Datei des freien Paketteils im Repositorium (Spaltenname -> Text). Die
        /// Dateien fuehren weder Trenner noch Anfuehrungszeichen in Feldern; leer bleibt leer.
        /// </summary>
        private static List<Dictionary<string, string>> Paketteil(string tabelle)
        {
            string[] zeilen = File.ReadAllLines(Path.Combine(Werkzeuglauf.Repowurzel, "Referenzlaeufe", "Katalogpaket_frei",
                                                             tabelle + ".csv"), Encoding.UTF8)
                                  .Where(z => z.Trim().Length > 0).ToArray();
            string[] kopf = zeilen[0].TrimStart('\uFEFF').Split(';');
            return zeilen.Skip(1).Select(z =>
            {
                string[] f = z.Split(';');
                Assert.Equal(kopf.Length, f.Length);
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < kopf.Length; i++) d[kopf[i]] = f[i];
                return d;
            }).ToList();
        }

        /// <summary>Ein Feld der Datei, wie die Datenbank es als Text zurueckgibt (Zahlen invariant).</summary>
        private static string Zahltext(string feld) =>
            double.TryParse(feld, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                ? d.ToString(CultureInfo.InvariantCulture) : feld;

        /// <summary>
        /// Die Parameterzeilen der Vorlage, die eine Zeile des Paketteils sind — Schlüssel und
        /// Herkunftsart wie die Datei (FREI oder EIGENKONSTRUKTION, N27) —, mit der Zusatzbedingung
        /// <paramref name="zusatz"/> und ihrer Katalogversion <paramref name="version"/> (oder keiner).
        /// </summary>
        private static int PaketteilParameter(string zusatz, string version)
            => Paketteil(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count(z => Convert.ToInt64(DataRepository.ExecuteScalar(
                   "SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Schluessel = ? AND Herkunftsart = ?" + zusatz,
                   new[] { new DbParam("?", z["Schluessel"]), new DbParam("?", z["Herkunftsart"]) }
                       .Concat(version == null ? Array.Empty<DbParam>() : new[] { new DbParam("?", version) }).ToArray())) == 1);

        /// <summary>Eine Zeile des Paketteils in der Vorlage: Herkunftsart wie erwartet (FREI), Status AUSLIEFERUNG, ReadOnly 1.</summary>
        private static void FreiUndGesperrt(DataRow r, string herkunft = TwwSchema.HERKUNFT_FREI)
        {
            Assert.Equal(herkunft, Convert.ToString(r["Herkunftsart"]));
            Assert.Equal(TwwSchema.STATUS_AUSLIEFERUNG, Convert.ToString(r["Status"]));
            Assert.True(r["ReadOnly"] is bool b ? b : Convert.ToInt64(r["ReadOnly"]) == 1, "ReadOnly ist nicht 1.");
        }

        private static string[] Namen(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT Bezeichner FROM \"" + tabelle + "\" ORDER BY ID");
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0])).ToArray();
        }

        private static long Zahl(string tabelle) =>
            Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));

        /// <summary>Ändert die QUELLkopie über die Zugriffsschicht (mit Werkzeugfreigabe).</summary>
        private static void Bearbeiten(string datei, Action aktion)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (Tww-Probe vorbereiten)");
                aktion();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }

        private static void Lesen(string datei, Action aktion)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                aktion();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }
    }
}
