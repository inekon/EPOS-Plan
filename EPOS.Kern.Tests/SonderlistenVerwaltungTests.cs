using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Strom;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die drei Sonderlisten als Katalogliste</b> (Konzept Administrationsdialoge, Stufe 5,
    /// V16): die Datenseite der Gebäudeverwaltung (A9), der Gebäudetypen (A10) und der Liste
    /// der Lastgänge der Lastspitzenkappung (A11) gegen die Testdatenbank.
    ///
    /// <para><b>Geprüft wird</b>, was die Oberfläche nicht selbst prüfen kann: die Zeilen und
    /// ihre Spalten (in EINER Abfrage je Liste), die Verwendung als Löschsperre, die
    /// Schreibwege (Kenndaten, Beschreibung, Anlegen mit Kurvenzahl, Duplizieren samt
    /// Tageskurven und „Veraenderbar"), die Sperre der Auslieferungssätze — und dass die
    /// Parametersätze der Hüllen nur <c>[Parameter]</c> ihrer Komponenten treffen.</para>
    ///
    /// <para>Jeder schreibende Fall hat seine EIGENE Arbeitskopie; ohne Testdatenbank
    /// schweigen die Fälle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SonderlistenVerwaltungTests
    {
        private static int Anzahl(string sql, params DbParam[] p)
            => Convert.ToInt32(DataRepository.ExecuteScalar(sql, p));

        // =================================================================================
        //  A9 — Gebäude
        // =================================================================================

        /// <summary>
        /// ALLE 277 Sätze mit den fünf Spalten des Profils; Verwendung und Baujahr stehen als
        /// KLARTEXT (der Trichter filtert auf dem angezeigten Wert), die zwei Verwendungen
        /// enthalten einander nicht.
        /// </summary>
        [Fact]
        public void Die_Gebaeudezeilen_tragen_die_fuenf_Spalten_als_Klartext()
        {
            using (var db = new TestDatenbank())
            using (new Kulturvorrichtung())
            {
                if (!db.Vorhanden) return;

                IReadOnlyList<Katalogfilterzeile> zeilen = GebaeudeStammCtrl.Katalogfilterzeilen();
                Assert.Equal(Anzahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"), zeilen.Count);

                int wohn = Anzahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Wohngebaeude_Nicht_Wohngebaeude = 'Wohngebaeude'");
                int sonst = Anzahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Wohngebaeude_Nicht_Wohngebaeude = 'Nicht Wohngebaeude'");
                Assert.Equal(wohn, zeilen.Count(z => z.Text(Katalogfilterprofil.SpVerwendung) == "Wohngebäude"));
                Assert.Equal(sonst, zeilen.Count(z => z.Text(Katalogfilterprofil.SpVerwendung) == "Gewerbe+Sonstige"));

                // Der Trichter "Gewerbe" trifft genau die Nichtwohngebaeude.
                var stand = new Katalogfilterstand();
                stand.Setzen(Katalogfilterprofil.SpVerwendung, "Gewerbe");
                Assert.Equal(sonst, Katalogfilter.Anwenden(Katalogfilterprofil.FuerGebaeude(), zeilen, stand).Count);

                // Das Baujahr ist der Klartext der Baualtersklasse, nicht ihr Buchstabe.
                IReadOnlyList<string> klassen = GebaeudeStammCtrl.Baualtersklassen();
                Assert.All(zeilen.Where(z => z.Text(Katalogfilterprofil.SpBaujahr) != ParameterVerwendung.LEER),
                           z => Assert.Contains(z.Text(Katalogfilterprofil.SpBaujahr), klassen));
                Assert.All(zeilen, z => Assert.NotNull(z.Zahl(Katalogfilterprofil.SpFlaecheM2)));
            }
        }

        /// <summary>
        /// <b>Die Verwendungssperre</b>: Je Gebäudename die Projekte, die eine Kopie führen —
        /// in EINER Abfrage, groß/klein egal.
        /// </summary>
        [Fact]
        public void Die_Projektverwendung_nennt_die_Projekte_je_Gebaeude()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                IReadOnlyDictionary<string, IReadOnlyList<string>> verwendung = GebaeudeStammCtrl.Projektverwendung();

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT g.Gebaeudename, p.Projektname FROM Tab_Gebaeude AS g " +
                    "INNER JOIN Tab_Projekt AS p ON p.ID = g.ID_Projekt LIMIT 1");
                if (dt.Rows.Count == 0) return;
                string name = Convert.ToString(dt.Rows[0][0]);
                string projekt = Convert.ToString(dt.Rows[0][1]);

                Assert.True(verwendung.ContainsKey(name.ToUpperInvariant()));
                Assert.Contains(projekt, verwendung[name]);
            }
        }

        /// <summary>
        /// <b>Das Stammblatt schreibt über den Weg des Katalogeditors</b> (Welle #465): Der
        /// Feldsatz der Hülle geht durch denselben Arbeitsstand — Prüfung und Ableitungen —
        /// und über <c>GebaeudeKatalogHuelle.Schreiben</c> in die Datenbank. Geschrieben sind
        /// danach die geänderten Werte; jede Spalte, die weder bearbeitet noch abgeleitet wird,
        /// bleibt, und die NULL-Spalten des Gebäudemodells bleiben NULL. Ein Auslieferungssatz
        /// wird weder geschrieben noch gelöscht.
        /// </summary>
        [Fact]
        public void Das_Stammblatt_schreibt_ueber_den_Weg_des_Katalogeditors_und_schont_die_Auslieferung()
        {
            using (var db = new TestDatenbank())
            using (new Kulturvorrichtung())
            {
                if (!db.Vorhanden) return;

                GebaeudePrueftexte prueftexte = GebaeudeKatalogHuelle.Prueftexte();
                GebaeudeHuelleTexte huelltexte = GebaeudeKatalogHuelle.Texte();

                // Der erste Satz, der die Pruefung des Editors besteht - einzelne Saetze des
                // Bestands tragen einen U-Wert ausserhalb des Bereichs und muessen erst
                // berichtigt werden (dieselbe Regel wie im Editor).
                DataTable vorher = DataRepository.GetDataTable("SELECT * FROM Tab_Gebaeude_STAMM ORDER BY ID");
                var arbeit = new GebaeudeArbeitsstand();
                int zeile = -1;
                GebaeudeStammblattDaten satz = null;
                for (int i = 0; i < vorher.Rows.Count && zeile < 0; i++)
                {
                    satz = GebaeudeAdminHuelle.Satz(Convert.ToString(vorher.Rows[i]["Bezeichner"]));
                    arbeit.Laden(satz.Feldsatz, neu: false);
                    if (arbeit.Pruefen(false, prueftexte, huelltexte) is null) zeile = i;
                }
                Assert.True(zeile >= 0, "kein Satz besteht die Pruefung");
                string name = satz.Name;
                Assert.NotNull(satz.Feldsatz);
                Assert.Equal(0, arbeit.Abweichungen(satz.Feldsatz));

                arbeit.Stand.Beschreibung = "Stufe #465";
                arbeit.Stand.UWertAussenwand = 0.33;
                arbeit.Stand.Luftwechselrate = 0.55;
                Assert.Equal(3, arbeit.Abweichungen(satz.Feldsatz));

                GebaeudePruefbefund befund = arbeit.Pruefen(false, prueftexte, huelltexte);
                Assert.True(befund is null, befund?.Meldung);
                arbeit.Ableiten();
                GebaeudeKatalogErgebnis ergebnis = GebaeudeKatalogHuelle.Schreiben(arbeit.Stand, false, name);
                Assert.True(ergebnis.Erfolg, ergebnis.Meldung);

                DataRow n = DataRepository.GetDataTable(
                    "SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", new DbParam("@b", name)).Rows[0];
                Assert.Equal("Stufe #465", n["Beschreibung"]);
                Assert.Equal(0.33, Convert.ToDouble(n["k_Wert_Außenwand"]), 6);
                Assert.Equal(0.55, Convert.ToDouble(n["Luftwechselrate"]), 6);

                // Geschrieben sind nur die bearbeiteten Spalten und die ABLEITUNGEN des
                // Editors - alles andere steht wie vorher, auch jedes NULL.
                var bearbeitet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Beschreibung", "k_Wert_Außenwand", "Luftwechselrate",
                    // die Ableitungen (GebaeudeKatalogHuelle.NachModell, GebaeudeArbeitsstand.Ableiten)
                    "Bewohner", "Flaeche_Nutzer", "gesamte_Fensterflaeche", "Nutzflaeche",
                    "Fensterflaeche_Ost_West", "Maximaleraumtemperatur", "Wochenende", "Ferien",
                    "WW_Bedarf", "Ferienbeginn_1", "Baualtersklasse", "Wohngebaeude_Nicht_Wohngebaeude",
                    // zwei Bestandsspalten, die das Modell als double fuehrt: Der Weg des Editors
                    // schreibt ein NULL als 0 zurueck - so wie die Projektkopie (CopyFromStamm)
                    // es ohnehin liest.
                    "spez_Waermeverbrauch", "Waermebedarf"
                };
                var abweichend = new List<string>();
                foreach (DataColumn s in vorher.Columns)
                    if (!bearbeitet.Contains(s.ColumnName) && !Gleich(vorher.Rows[zeile][s.ColumnName], n[s.ColumnName]))
                        abweichend.Add(s.ColumnName + ": " + vorher.Rows[zeile][s.ColumnName] + " -> " + n[s.ColumnName]);
                Assert.True(abweichend.Count == 0, string.Join(" | ", abweichend));

                // Ein Auslieferungssatz: weder Schreiben noch Loeschen.
                string zweiter = Convert.ToString(vorher.Rows[zeile == 0 ? 1 : 0]["Bezeichner"]);
                DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE Bezeichner = ?",
                                          new DbParam("@b", zweiter));
                GebaeudeKatalogDaten geschuetzt = GebaeudeAdminHuelle.Satz(zweiter).Feldsatz;
                geschuetzt.Beschreibung = "darf nicht stehen";
                Assert.False(GebaeudeKatalogHuelle.Schreiben(geschuetzt, false, zweiter).Erfolg);
                Assert.NotEqual("darf nicht stehen", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT Beschreibung FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", new DbParam("@b", zweiter))));
                Assert.False(GebaeudeStammCtrl.Loeschen(zweiter));
                Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?",
                                       new DbParam("@b", zweiter)));

                // Ein eigener Satz wird geloescht.
                Assert.True(GebaeudeStammCtrl.Loeschen(name));
                Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?",
                                       new DbParam("@b", name)));
            }

            static bool Gleich(object a, object b)
            {
                if (a == DBNull.Value || b == DBNull.Value) return a == DBNull.Value && b == DBNull.Value;
                if (a is double or float or long or int && b is double or float or long or int)
                    return Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 1e-9;
                return Equals(a, b);
            }
        }

        /// <summary>Die Hülle liest einen Satz fürs Stammblatt: Kenndaten, H_ges, vier Bauteile, „Alle Daten".</summary>
        [Fact]
        public void Die_Huelle_liest_den_Satz_fuers_Stammblatt()
        {
            using (var db = new TestDatenbank())
            using (new Kulturvorrichtung())
            {
                if (!db.Vorhanden) return;

                string name = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"));
                GebaeudeStammblattDaten satz = GebaeudeAdminHuelle.Satz(name);

                Assert.Equal(name, satz.Name);
                Assert.Equal(4, satz.Huelle.Count);
                Assert.Contains("W/(m²K)", satz.Huelle[0].Wert);
                Assert.True(satz.AlleDaten.Count > 20);
                Assert.Contains(satz.AlleDaten, w => w.IstAbschnitt);
                Assert.NotNull(satz.HgesWK);

                // #465: der Feldsatz des Katalogeditors - derselbe, den der Editor laedt.
                Assert.NotNull(satz.Feldsatz);
                Assert.Equal(name, satz.Feldsatz!.Name);
                GebaeudeKatalogDaten editor = GebaeudeKatalogHuelle.AusModell(GebaeudeKatalogHuelle.Laden(name));
                var arbeit = new GebaeudeArbeitsstand();
                arbeit.Laden(editor, neu: false);
                Assert.Equal(0, arbeit.Abweichungen(satz.Feldsatz));

                Assert.Null(GebaeudeAdminHuelle.Satz("gibt es nicht"));
            }
        }

        // =================================================================================
        //  A10 — Gebäudetypen
        // =================================================================================

        /// <summary>
        /// Zwölf Typen mit Kurvenzahl und Beschreibung; nicht veränderbare tragen das Schloss —
        /// dieselbe Regel wie <c>GebaeudetypDaten.Aenderbar</c>.
        /// </summary>
        [Fact]
        public void Die_Typzeilen_tragen_Kurvenzahl_und_Schloss()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                IReadOnlyList<Katalogfilterzeile> zeilen = TagVCtrl.Katalogfilterzeilen();
                Assert.Equal(Anzahl("SELECT COUNT(*) FROM Tab_DBTagV_STAMM"), zeilen.Count);

                foreach (Katalogfilterzeile z in zeilen)
                {
                    int daten = Anzahl("SELECT COUNT(*) FROM Tab_DBTagVDaten_STAMM WHERE ID_TagV = ?", new DbParam("@id", z.Id));
                    Assert.Equal(daten / 24, (int)z.Zahl(Katalogfilterprofil.SpKurven)!.Value);

                    int frei = Anzahl("SELECT COUNT(*) FROM Tab_DBTagV_STAMM WHERE ID = ? AND Veraenderbar = 1 AND ReadOnly = 0",
                                      new DbParam("@id", z.Id));
                    Assert.Equal(frei == 0, z.Geschuetzt);
                }
                Assert.Contains(zeilen, z => z.Geschuetzt);
                Assert.Contains(zeilen, z => !z.Geschuetzt);
            }
        }

        /// <summary>
        /// <b>Duplizieren eines Auslieferungstyps</b>: Die Kopie trägt alle Tageskurven Zeile für
        /// Zeile, ist <c>Veraenderbar</c> und kein Auslieferungssatz — in EINER Transaktion
        /// (Kern <c>Katalogkopie</c> mit festen Spalten).
        /// </summary>
        [Fact]
        public void Duplizieren_eines_Typs_nimmt_die_Kurven_mit_und_ist_veraenderbar()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                DataTable alt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Beschreibung FROM Tab_DBTagV_STAMM WHERE Veraenderbar = 0 ORDER BY ID LIMIT 1");
                if (alt.Rows.Count == 0) return;
                int id = Convert.ToInt32(alt.Rows[0]["ID"]);

                Katalogkopie.Ergebnis e = TagVCtrl.Duplizieren(id, "Stufe 5 Kopie");
                Assert.True(e.Ok, e.Meldung);

                DataTable kopf = DataRepository.GetDataTable(
                    "SELECT * FROM Tab_DBTagV_STAMM WHERE ID = ?", new DbParam("@id", e.Id));
                Assert.Equal("Stufe 5 Kopie", kopf.Rows[0]["Bezeichner"]);
                Assert.Equal(alt.Rows[0]["Beschreibung"], kopf.Rows[0]["Beschreibung"]);
                Assert.Equal(1L, Convert.ToInt64(kopf.Rows[0]["Veraenderbar"]));
                Assert.Equal(0L, Convert.ToInt64(kopf.Rows[0]["ReadOnly"]));

                const string werte = "SELECT Verteilung FROM Tab_DBTagVDaten_STAMM WHERE ID_TagV = ? ORDER BY ID";
                DataTable a = DataRepository.GetDataTable(werte, new DbParam("@id", id));
                DataTable b = DataRepository.GetDataTable(werte, new DbParam("@id", e.Id));
                Assert.Equal(a.Rows.Count, b.Rows.Count);
                for (int i = 0; i < a.Rows.Count; i++) Assert.Equal(a.Rows[i][0], b.Rows[i][0]);

                // Das Original bleibt gesperrt; die Kopie ist ohne Schloss in der Liste.
                Assert.True(TagVCtrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);
                Assert.False(TagVCtrl.Katalogfilterzeilen().Single(z => z.Id == e.Id).Geschuetzt);
            }
        }

        /// <summary>„Neu…" mit fünf oder acht Kurven — 120 bzw. 192 Datenzeilen zu 0.</summary>
        [Theory]
        [InlineData(5, 120)]
        [InlineData(8, 192)]
        public void Anlegen_nimmt_die_Kurvenzahl(int kurven, int zeilen)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                int id = TagVCtrl.Anlegen("Stufe 5 neu " + kurven, "Probe", kurven);
                Assert.True(id > 0);
                Assert.Equal(zeilen, Anzahl("SELECT COUNT(*) FROM Tab_DBTagVDaten_STAMM WHERE ID_TagV = ?",
                                            new DbParam("@id", id)));
            }
        }

        /// <summary>Die Beschreibung schreibt nur ein veränderbarer Typ.</summary>
        [Fact]
        public void Die_Beschreibung_schreibt_nur_ein_veraenderbarer_Typ()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                object gesperrt = DataRepository.ExecuteScalar("SELECT ID FROM Tab_DBTagV_STAMM WHERE Veraenderbar = 0 LIMIT 1");
                object frei = DataRepository.ExecuteScalar("SELECT ID FROM Tab_DBTagV_STAMM WHERE Veraenderbar = 1 AND ReadOnly = 0 LIMIT 1");
                if (gesperrt == null || frei == null) return;

                TagVCtrl.BeschreibungSchreiben(Convert.ToInt32(gesperrt), "darf nicht");
                Assert.NotEqual("darf nicht", DataRepository.ExecuteScalar(
                    "SELECT Beschreibung FROM Tab_DBTagV_STAMM WHERE ID = ?", new DbParam("@id", gesperrt)));

                Assert.True(TagVCtrl.BeschreibungSchreiben(Convert.ToInt32(frei), "Stufe 5"));
                Assert.Equal("Stufe 5", DataRepository.ExecuteScalar(
                    "SELECT Beschreibung FROM Tab_DBTagV_STAMM WHERE ID = ?", new DbParam("@id", frei)));
            }
        }

        /// <summary>Die Verwendung zählt die Gebäude des Katalogs je Typname.</summary>
        [Fact]
        public void Die_Gebaeudeverwendung_zaehlt_je_Typ()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                IReadOnlyDictionary<string, int> verwendung = TagVCtrl.Gebaeudeverwendung();
                Assert.Equal(Anzahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Typ IS NOT NULL"),
                             verwendung.Values.Sum());
                Assert.All(verwendung.Values, n => Assert.True(n > 0));
            }
        }

        // =================================================================================
        //  A11 — Lastgänge der Lastspitzenkappung
        // =================================================================================

        /// <summary>
        /// Je Ganglinie eine Zeile mit dem Schlüssel „G" + Platz, Quelle, Intervall in Minuten
        /// und dem Jahresmaximum — dem höchsten abgelegten Wert.
        /// </summary>
        [Fact]
        public void Die_Lastgangzeilen_tragen_Quelle_Intervall_und_Maximum()
        {
            using (var db = new TestDatenbank())
            using (new Kulturvorrichtung())
            {
                if (!db.Vorhanden) return;

                object projekt = DataRepository.ExecuteScalar(
                    "SELECT ID_Projekt FROM Tab_Stromganglinie WHERE ID_Projekt IS NOT NULL LIMIT 1");
                int idProjekt = projekt == null ? 0 : Convert.ToInt32(projekt);

                List<GanglinienEintrag> eintraege = PeakShavingCtrl.LeseGanglinien(idProjekt);
                IReadOnlyList<Katalogfilterzeile> zeilen = PeakShavingCtrl.Katalogfilterzeilen(eintraege, idProjekt);

                Assert.Equal(eintraege.Count, zeilen.Count);
                for (int i = 0; i < zeilen.Count; i++)
                {
                    GanglinienEintrag e = eintraege[i];
                    Katalogfilterzeile z = zeilen[i];
                    Assert.Equal("G" + i, z.Schluessel);
                    Assert.Equal(e.AusStamm ? "Stamm" : "Projekt", z.Text(Katalogfilterprofil.SpQuelle));

                    double? minuten = PeakShavingCtrl.IntervallMinuten(e.Zeitinterval);
                    Assert.Equal(minuten, z.Zahl(Katalogfilterprofil.SpIntervallMin));

                    string tabelle = e.AusStamm ? "Tab_StromganglinieDaten_STAMM" : "Tab_StromganglinieDaten";
                    object max = DataRepository.ExecuteScalar(
                        "SELECT MAX(Wert) FROM " + tabelle + " WHERE ID_Ganglinie = ?", new DbParam("@id", e.Id));
                    double? erwartet = max == null || max == DBNull.Value ? null : Convert.ToDouble(max);
                    Assert.Equal(erwartet, z.Zahl(Katalogfilterprofil.SpJahresmaximumKw));
                }
                Assert.Contains(zeilen, z => z.Text(Katalogfilterprofil.SpQuelle) == "Stamm");
            }
        }

        // =================================================================================
        //  Die Parametersätze der Hüllen
        // =================================================================================

        /// <summary>
        /// Die Parametersätze der zwei Hüllen in <c>EPOS.UI.Daten</c> treffen nur
        /// <c>[Parameter]</c> ihrer Komponenten — ein unbekannter Schlüssel bräche erst beim
        /// ersten Zeichnen.
        /// </summary>
        [Fact]
        public void Die_Parametersaetze_treffen_die_Parameter_ihrer_Komponenten()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                Pruefe(typeof(GebaeudeAdminDialog), GebaeudeAdminHuelle.Gaben());
                Pruefe(typeof(PeakShavingDialog), new PeakShavingHuelle(0).Gaben());
            }

            static void Pruefe(Type komponente, IReadOnlyDictionary<string, object> gaben)
            {
                foreach (string schluessel in gaben.Keys)
                {
                    var eigenschaft = komponente.GetProperty(schluessel);
                    Assert.True(eigenschaft != null && Attribute.IsDefined(eigenschaft,
                                    typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                                komponente.Name + " führt keinen [Parameter] " + schluessel);
                }
            }
        }
    }
}
