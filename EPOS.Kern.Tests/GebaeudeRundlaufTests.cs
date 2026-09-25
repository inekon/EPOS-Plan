using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Rundlauf eines Gebäudesatzes über JEDE Spalte</b> — gegen die Testdatenbank, auf
    /// allen Schreibwegen des Gebäudes: Katalogsatz öffnen und ohne Änderung speichern (OK im
    /// Katalogeditor wie im Stammblatt der Gebäudeverwaltung), „Speichern unter", Übernahme in
    /// ein Projekt (<see cref="GebaeudeStammCtrl.CopyFromStamm(int?, string, int, int)"/>) und
    /// OK in „Hülle und Zonen…" (Projektkopie). Nach jedem Weg steht jede Spalte so da wie
    /// vorher.
    ///
    /// <para><b>Befund 25.09.2026 (Datenverlust beim Anwender):</b> Ein Katalogsatz, an dem nur
    /// die Bauart geändert wurde, trug nach dem Speichern <c>WW_Bedarf = 0</c> statt 700, und eine
    /// neue Projektkopie trug <c>WW_Bedarf = 0</c>. Ursache war die Ableitung
    /// <c>WW_Bedarf = 0</c> des Vorläufers (<c>Form_Gebaeude2.btn_Speichern_Click</c>), die seit
    /// G1 W5 in JEDEM OK-Weg des Dialogs lief (<see cref="GebaeudeArbeitsstand.Ableiten"/>).
    /// Der Dialog zeigt den Warmwasserbedarf nicht; er darf ihn also auch nicht ändern.</para>
    ///
    /// <para><b>Die Spalten kommen aus dem Schema</b> (<c>SELECT *</c>), nicht aus einer Liste:
    /// Eine künftige Spalte ist von selbst mitgeprüft. Ausgenommen sind allein die Schlüssel und
    /// Verweise des jeweiligen Wegs. Geduldet werden nur zwei benannte Abweichungen, die kein
    /// Leser sieht (<c>Befund.Geduldet</c>): NULL einer Bestandsspalte wird zu dem Wert, den jeder
    /// Leser für NULL einsetzt, und die zwei gerechneten Spalten tragen ihre Rechnung. Jede andere
    /// Abweichung ist ein Verlust.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — jeder Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeRundlaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const string KATALOG = "Tab_Gebaeude_STAMM";
        private const string PROJEKT = "Tab_Gebaeude";

        /// <summary>Die Spalten der Übergabe (AK-S1), der Kühlübergabe (KAK-S1) und das Baujahr (G4a).</summary>
        private static readonly string[] NEUE_WEGE_SPALTEN =
        {
            "Heizkreis_Aktiv", "Uebergabe_Art", "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn",
            "Auslegung_Vorlauf", "Auslegung_Ruecklauf", "Auslegung_Raumtemperatur",
            "Auslegung_Aussentemperatur", "Heizkurve_Aktiv", "Heizkurve_Niveau", "Heizkurve_Steilheit",
            "Regler_Proportionalband", "Sollwertprofil",
            "Kuehluebergabe_Aktiv", "Kuehl_Uebergabe_Art", "Kuehl_Uebergabe_Exponent",
            "Kuehl_Uebergabe_Leistung_Nenn", "Kuehl_Auslegung_Vorlauf", "Kuehl_Auslegung_Ruecklauf",
            "Kuehl_Auslegung_Raumtemperatur", "Kuehl_Vorlaufgrenze",
            "Baujahr",
        };

        // =============================================================================
        //  Der Befund - WW_Bedarf
        // =============================================================================

        /// <summary>
        /// Der Weg des Anwenders: Katalogsatz öffnen, nur die Bauart ändern, speichern — der
        /// Warmwasserbedarf bleibt stehen (Befund: 700 → 0).
        /// </summary>
        [Fact]
        public void Bauart_aendern_und_speichern_haelt_den_Warmwasserbedarf()
        {
            if (!_db.Vorhanden) return;

            (int id, string name) = BeschreibbarerSatzMitWarmwasser();
            double vorher = Zahl(Zeile(KATALOG, id)["WW_Bedarf"]);
            Assert.True(vorher > 0);

            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(GebaeudeKatalogHuelle.AusModell(GebaeudeKatalogHuelle.Laden(name)), false);
            arbeit.BauartWaehlen(arbeit.Stand.Bauart == 2 ? 1 : 2);
            arbeit.Ableiten();
            GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.Schreiben(arbeit.Stand, false, name);
            Assert.True(e.Erfolg, e.Meldung);

            Assert.Equal(vorher, Zahl(Zeile(KATALOG, id)["WW_Bedarf"]));
        }

        /// <summary>
        /// Übernahme ins Projekt und danach OK in „Hülle und Zonen…" — die Projektkopie trägt den
        /// Warmwasserbedarf des Katalogsatzes und behält ihn.
        /// </summary>
        [Fact]
        public void Projektkopie_traegt_den_Warmwasserbedarf_und_behaelt_ihn_in_Huelle_und_Zonen()
        {
            if (!_db.Vorhanden) return;

            (int id, string name) = BeschreibbarerSatzMitWarmwasser();
            double katalog = Zahl(Zeile(KATALOG, id)["WW_Bedarf"]);
            (int idProjekt, int idZ) = EineZuordnung();

            int kopie = new GebaeudeStammCtrl().CopyFromStamm(id, name, idProjekt, idZ);
            Assert.True(kopie > 0);
            Assert.Equal(katalog, Zahl(Zeile(PROJEKT, kopie)["WW_Bedarf"]));

            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(GebaeudeKatalogHuelle.AusModell(GebaeudeStammCtrl.LiesProjektkopie(kopie)), false);
            arbeit.Ableiten();
            GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.ProjektSchreiben(idProjekt, kopie, arbeit.Stand);
            Assert.True(e.Erfolg, e.Meldung);

            Assert.Equal(katalog, Zahl(Zeile(PROJEKT, kopie)["WW_Bedarf"]));
        }

        // =============================================================================
        //  Der breite Rundlauf - jede Spalte, jeder Satz
        // =============================================================================

        /// <summary>
        /// OK ohne Änderung (Katalogeditor und Stammblatt der Verwaltung gehen denselben Weg:
        /// Arbeitsstand laden, <see cref="GebaeudeArbeitsstand.Ableiten"/>,
        /// <see cref="GebaeudeKatalogHuelle.Schreiben"/>) — für JEDEN Katalogsatz der
        /// Testdatenbank steht danach jede Spalte wie vorher.
        /// </summary>
        [Fact]
        public void Katalog_OK_ohne_Aenderung_haelt_jede_Spalte_jedes_Satzes()
        {
            if (!_db.Vorhanden) return;

            // Die Auslieferungssperre gilt dem Anwender, nicht dem Rundlauf.
            DataRepository.ExecuteSQL("UPDATE [" + KATALOG + "] SET ReadOnly = 0");

            var befund = new Befund();
            foreach ((int id, string name) in Katalogsaetze())
            {
                Dictionary<string, object> vorher = Zeile(KATALOG, id);
                GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.Schreiben(OhneAenderung(GebaeudeKatalogHuelle.Laden(name)), false, name);
                Assert.True(e.Erfolg, name + ": " + e.Meldung);
                befund.Vergleichen(id, vorher, Zeile(KATALOG, id));
            }
            Assert.True(befund.Leer, befund.Bericht("Katalog, OK ohne Änderung"));
        }

        /// <summary>
        /// „Speichern unter" ohne Änderung — der neue Satz trägt jede Spalte des Ursprungs (bis auf
        /// Id, Name und Auslieferungskennung).
        /// </summary>
        [Fact]
        public void Katalog_Speichern_unter_traegt_jede_Spalte_jedes_Satzes()
        {
            if (!_db.Vorhanden) return;

            var befund = new Befund("ID", "Bezeichner", "ReadOnly");
            int n = 0;
            foreach ((int id, string name) in Katalogsaetze())
            {
                string neu = "Rundlauf " + (++n).ToString(CultureInfo.InvariantCulture);
                GebaeudeKatalogDaten d = OhneAenderung(GebaeudeKatalogHuelle.Laden(name));
                d.Name = neu;
                GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.Schreiben(d, true, neu);
                Assert.True(e.Erfolg, name + ": " + e.Meldung);
                int idNeu = new GebaeudeStammCtrl().Lies(neu).ID;
                befund.Vergleichen(id, Zeile(KATALOG, id), Zeile(KATALOG, idNeu));
            }
            Assert.True(befund.Leer, befund.Bericht("Katalog, Speichern unter"));
        }

        /// <summary>
        /// Übernahme in ein Projekt (<see cref="GebaeudeStammCtrl.CopyFromStamm(int?, string, int, int)"/>,
        /// der Weg von Assistent, Gebäudedialog und Startseite) — die Projektkopie trägt JEDE
        /// Fachspalte des Katalogsatzes; ausgenommen sind nur die Schlüssel und Verweise.
        /// </summary>
        [Fact]
        public void CopyFromStamm_traegt_jede_Fachspalte_jedes_Katalogsatzes()
        {
            if (!_db.Vorhanden) return;

            (int idProjekt, int idZ) = EineZuordnung();
            var ctrl = new GebaeudeStammCtrl();
            var befund = new Befund("ID", "ReadOnly", "ID_ProjektGebaeude", "ID_Projekt", "ID_Gebaeude_Stamm");
            foreach ((int id, string name) in Katalogsaetze())
            {
                int kopie = ctrl.CopyFromStamm(id, name, idProjekt, idZ);
                Assert.True(kopie > 0, name);
                Dictionary<string, object> katalog = Zeile(KATALOG, id);
                katalog["Gebaeudename"] = katalog["Bezeichner"];
                katalog.Remove("Bezeichner");
                Dictionary<string, object> projekt = Zeile(PROJEKT, kopie);
                Assert.Equal((long)id, Convert.ToInt64(projekt["ID_Gebaeude_Stamm"], CultureInfo.InvariantCulture));
                befund.Vergleichen(id, katalog, projekt);
            }
            Assert.True(befund.Leer, befund.Bericht("Projektkopie aus dem Katalog"));
        }

        /// <summary>
        /// OK ohne Änderung in „Hülle und Zonen…" — für JEDE Projektkopie der Testdatenbank steht
        /// danach jede Spalte wie vorher.
        /// </summary>
        [Fact]
        public void Projektkopie_OK_ohne_Aenderung_haelt_jede_Spalte_jeder_Kopie()
        {
            if (!_db.Vorhanden) return;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM [" + PROJEKT + "] WHERE ID_Projekt IS NOT NULL ORDER BY ID");
            Assert.NotEmpty(dt.Rows);
            var befund = new Befund();
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                int idProjekt = Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture);
                Dictionary<string, object> vorher = Zeile(PROJEKT, id);
                GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.ProjektSchreiben(
                    idProjekt, id, OhneAenderung(GebaeudeStammCtrl.LiesProjektkopie(id)));
                Assert.True(e.Erfolg, id + ": " + e.Meldung);
                befund.Vergleichen(id, vorher, Zeile(PROJEKT, id));
            }
            Assert.True(befund.Leer, befund.Bericht("Projektkopie, OK in Hülle und Zonen"));
        }

        /// <summary>
        /// Die Spalten der Übergabe (AK-S1), der Kühlübergabe (KAK-S1) und das Baujahr AUSDRÜCKLICH
        /// belegt — die Testdatenbank führt sie fast überall auf NULL/0, ein Rundlauf über den
        /// Bestand sähe einen Verlust an ihnen also nicht. Ein Satz mit gesetzten Werten übersteht
        /// OK, „Speichern unter", die Übernahme ins Projekt und OK in „Hülle und Zonen…".
        /// </summary>
        [Fact]
        public void Uebergabe_Kuehluebergabe_und_Baujahr_ueberstehen_jeden_Weg()
        {
            if (!_db.Vorhanden) return;

            (int id, string name) = BeschreibbarerSatzMitWarmwasser();
            DataRepository.ExecuteSQL(
                "UPDATE [" + KATALOG + "] SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Uebergabe_Exponent = 1.25, " +
                "Uebergabe_Leistung_Nenn = 42.5, Auslegung_Vorlauf = 55, Auslegung_Ruecklauf = 45, " +
                "Auslegung_Raumtemperatur = 21, Auslegung_Aussentemperatur = -12, Heizkurve_Aktiv = 1, " +
                "Heizkurve_Niveau = 2, Heizkurve_Steilheit = 1.1, Regler_Proportionalband = 1, Sollwertprofil = NULL, " +
                "Kuehlung_Aktiv = 1, Kuehl_Sollwert = 26, Kuehluebergabe_Aktiv = 1, Kuehl_Uebergabe_Art = ?, " +
                "Kuehl_Uebergabe_Exponent = 1.1, Kuehl_Uebergabe_Leistung_Nenn = 12.5, Kuehl_Auslegung_Vorlauf = 16, " +
                "Kuehl_Auslegung_Ruecklauf = 19, Kuehl_Auslegung_Raumtemperatur = 26, Kuehl_Vorlaufgrenze = 18, " +
                "Baujahr = 1987 WHERE ID = ?",
                new DbParam("@a", Waermeuebergabevorgaben.Arten.First(a => Waermeuebergabevorgaben.ArtRechnet(a))),
                new DbParam("@k", Waermeuebergabevorgaben.KuehlArten.First(a => Waermeuebergabevorgaben.KuehlArtRechnet(a))),
                new DbParam("@id", id));
            Dictionary<string, object> soll = Zeile(KATALOG, id);
            foreach (string s in NEUE_WEGE_SPALTEN.Where(s => s != "Sollwertprofil"))
                Assert.True(soll[s] != DBNull.Value && Zahl0(soll[s]) != 0, s + " ist nicht belegt");

            // OK ohne Aenderung
            Assert.True(GebaeudeKatalogHuelle.Schreiben(OhneAenderung(GebaeudeKatalogHuelle.Laden(name)), false, name).Erfolg);
            var befund = new Befund();
            befund.Vergleichen(id, soll, Zeile(KATALOG, id));

            // Speichern unter
            const string NEU = "Rundlauf Kopplung";
            GebaeudeKatalogDaten d = OhneAenderung(GebaeudeKatalogHuelle.Laden(name));
            d.Name = NEU;
            Assert.True(GebaeudeKatalogHuelle.Schreiben(d, true, NEU).Erfolg);
            var befundUnter = new Befund("ID", "Bezeichner", "ReadOnly");
            befundUnter.Vergleichen(id, soll, Zeile(KATALOG, new GebaeudeStammCtrl().Lies(NEU).ID));

            // Uebernahme ins Projekt und OK in "Huelle und Zonen..."
            (int idProjekt, int idZ) = EineZuordnung();
            int kopie = new GebaeudeStammCtrl().CopyFromStamm(id, name, idProjekt, idZ);
            Assert.True(kopie > 0);
            Dictionary<string, object> katalog = Zeile(KATALOG, id);
            katalog["Gebaeudename"] = katalog["Bezeichner"];
            katalog.Remove("Bezeichner");
            var befundKopie = new Befund("ID", "ReadOnly", "ID_ProjektGebaeude", "ID_Projekt", "ID_Gebaeude_Stamm");
            befundKopie.Vergleichen(id, katalog, Zeile(PROJEKT, kopie));

            Dictionary<string, object> kopieVorher = Zeile(PROJEKT, kopie);
            Assert.True(GebaeudeKatalogHuelle.ProjektSchreiben(idProjekt, kopie,
                OhneAenderung(GebaeudeStammCtrl.LiesProjektkopie(kopie))).Erfolg);
            var befundProjekt = new Befund();
            befundProjekt.Vergleichen(kopie, kopieVorher, Zeile(PROJEKT, kopie));

            Assert.True(befund.Leer, befund.Bericht("Kopplung, OK"));
            Assert.True(befundUnter.Leer, befundUnter.Bericht("Kopplung, Speichern unter"));
            Assert.True(befundKopie.Leer, befundKopie.Bericht("Kopplung, Projektkopie"));
            Assert.True(befundProjekt.Leer, befundProjekt.Bericht("Kopplung, Hülle und Zonen"));
        }

        /// <summary>
        /// Die Sicht <c>Abfrage_Projektgebaeude</c> führt jede Fachspalte der Projektkopie, und der
        /// Namensleser (<see cref="ProjektGebaeudeCtrl"/>) liefert den Warmwasserbedarf und die
        /// Schalter der Übergabe so, wie sie in <c>Tab_Gebaeude</c> stehen.
        /// </summary>
        [Fact]
        public void Die_Sicht_fuehrt_jede_Fachspalte_der_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            var sicht = new HashSet<string>(Spalten("Abfrage_Projektgebaeude"), StringComparer.OrdinalIgnoreCase);
            string[] fehlen = Spalten(PROJEKT)
                .Where(s => s != "ID_ProjektGebaeude" && s != "ID_Gebaeude_Stamm" && !sicht.Contains(s))
                .ToArray();
            Assert.True(fehlen.Length == 0, "Es fehlen in der Sicht: " + string.Join(", ", fehlen));
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        /// <summary>
        /// Der Weg des Dialogs ohne Eingabe: Feldsatz aus dem Modell, Arbeitsstand laden, im OK-Weg
        /// ableiten — so, wie Katalogeditor und Stammblatt es vor dem Schreiben tun.
        /// </summary>
        private static GebaeudeKatalogDaten OhneAenderung(GebaeudeModel m)
        {
            Assert.NotNull(m);
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(GebaeudeKatalogHuelle.AusModell(m), false);
            arbeit.Ableiten();
            return arbeit.Stand;
        }

        private static IEnumerable<(int Id, string Name)> Katalogsaetze()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT ID, Bezeichner FROM [" + KATALOG + "] ORDER BY ID");
            Assert.NotEmpty(dt.Rows);
            return dt.Rows.Cast<DataRow>()
                .Select(r => (Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture), Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture)))
                .ToList();
        }

        /// <summary>Ein Katalogsatz mit Warmwasserbedarf &gt; 0, dessen Schloss für den Fall offen ist.</summary>
        private static (int Id, string Name) BeschreibbarerSatzMitWarmwasser()
        {
            DataRow r = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM [" + KATALOG + "] WHERE WW_Bedarf > 0 ORDER BY ID LIMIT 1").Rows[0];
            int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
            DataRepository.ExecuteSQL("UPDATE [" + KATALOG + "] SET ReadOnly = 0 WHERE ID = ?", new DbParam("@id", id));
            return (id, Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
        }

        private static (int IdProjekt, int IdZ) EineZuordnung()
        {
            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            return (Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture),
                    Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture));
        }

        private static string[] Spalten(string tabelle)
            => DataRepository.GetDataTable("PRAGMA table_info(\"" + tabelle + "\")").Rows.Cast<DataRow>()
                .Select(r => Convert.ToString(r["name"], CultureInfo.InvariantCulture)).ToArray();

        private static Dictionary<string, object> Zeile(string tabelle, int id)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] WHERE ID = ?", new DbParam("@id", id));
            Assert.Single(dt.Rows);
            var zeile = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (DataColumn c in dt.Columns) zeile[c.ColumnName] = dt.Rows[0][c];
            return zeile;
        }

        private static double Zahl(object w) => Convert.ToDouble(w, CultureInfo.InvariantCulture);

        private static double Zahl0(object w) => w is string ? 1 : Zahl(w);

        /// <summary>Die Abweichungen je Spalte über viele Sätze — mit Beispielen für die Meldung.</summary>
        private sealed class Befund
        {
            private readonly HashSet<string> _ausgenommen;
            private readonly SortedDictionary<string, List<string>> _je = new(StringComparer.Ordinal);

            public Befund(params string[] ausgenommen)
                => _ausgenommen = new HashSet<string>(ausgenommen, StringComparer.Ordinal);

            public bool Leer => _je.Count == 0;

            public void Vergleichen(int id, Dictionary<string, object> soll, Dictionary<string, object> ist)
            {
                foreach (KeyValuePair<string, object> s in soll)
                {
                    if (_ausgenommen.Contains(s.Key)) continue;
                    if (!ist.TryGetValue(s.Key, out object i))
                    {
                        Merken(s.Key, id + ": Spalte fehlt im Ziel");
                        continue;
                    }
                    if (!Gleich(s.Value, i) && !Geduldet(s.Key, s.Value, i, ist))
                        Merken(s.Key, id + ": " + Text(s.Value) + " -> " + Text(i));
                }
            }

            /// <summary>
            /// Die zwei Abweichungen, die KEIN Verlust sind:
            /// <list type="bullet">
            /// <item>Eine BESTANDSSPALTE (die 53 der Sicht vor M3) war NULL und trägt jetzt den Wert,
            /// den jeder Leser für NULL ohnehin sieht — 0, "" oder die Vorbelegung des Modells
            /// (<see cref="GebaeudeModel"/>: Baualtersklasse, Gebäudeart, Verwendung). Die Spalten ab
            /// M3 sind NULL-erhaltend: Dort ist NULL die Vorgabe und jede Änderung ein Fehler.</item>
            /// <item>Eine GERECHNETE Spalte trägt ihre Rechnung aus den übrigen Werten derselben Zeile
            /// (<c>Bewohner</c> = Wohnfläche / Fläche je Nutzer, <c>gesamte_Fensterflaeche</c> = Süd +
            /// Ost/West + Nord) — die Ableitungen der Hülle (<c>NachModell</c>).</item>
            /// </list>
            /// </summary>
            private static bool Geduldet(string spalte, object vorher, object nachher, Dictionary<string, object> zeile)
            {
                if ((vorher == null || vorher == DBNull.Value) && BESTAND.Contains(spalte) && nachher != null && nachher != DBNull.Value)
                {
                    if (nachher is string s)
                        return s.Length == 0 || (VORBELEGUNG.TryGetValue(spalte, out string v) && s == v);
                    return Convert.ToDouble(nachher, CultureInfo.InvariantCulture) == 0;
                }
                if (spalte == "Bewohner")
                    return Gleich(nachher, Zahl(zeile["Wohnflaeche_gesamt"]) / Zahl(zeile["Flaeche_Nutzer"]));
                if (spalte == "gesamte_Fensterflaeche")
                    return Gleich(nachher, Zahl(zeile["Fensterflaeche_Sued"]) + Zahl(zeile["Fensterflaeche_Ost_West"])
                                           + Zahl(zeile["Fensterflaeche_Nord"]));
                return false;
            }

            private static readonly HashSet<string> BESTAND = new(GebaeudeSchema.SICHT_GEBAEUDE, StringComparer.Ordinal);

            private static readonly Dictionary<string, string> VORBELEGUNG = Vorbelegung();

            private static Dictionary<string, string> Vorbelegung()
            {
                var leer = new GebaeudeModel();
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Baualtersklasse"] = leer.Baualtersklasse,
                    ["Gebaeudeart"] = leer.Gebaeudeart,
                    ["Wohngebaeude_Nicht_Wohngebaeude"] = leer.Wohngebaeude_Nicht_Wohngebaeude,
                };
            }

            private void Merken(string spalte, string beispiel)
            {
                if (!_je.TryGetValue(spalte, out List<string> l)) _je[spalte] = l = new List<string>();
                l.Add(beispiel);
            }

            public string Bericht(string weg)
            {
                var sb = new StringBuilder(weg + ": " + _je.Count + " Spalte(n) weichen ab");
                foreach (KeyValuePair<string, List<string>> p in _je)
                    sb.Append("\n  ").Append(p.Key).Append(" (").Append(p.Value.Count).Append(" Sätze): ")
                      .Append(string.Join("; ", p.Value.Take(4)));
                return sb.ToString();
            }

            private static bool Gleich(object a, object b)
            {
                bool na = a == null || a == DBNull.Value, nb = b == null || b == DBNull.Value;
                if (na || nb) return na && nb;
                if (a is string || b is string)
                    return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture),
                                         Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.Ordinal);
                double x = Convert.ToDouble(a, CultureInfo.InvariantCulture), y = Convert.ToDouble(b, CultureInfo.InvariantCulture);
                return Math.Abs(x - y) <= 1e-12 * Math.Max(1.0, Math.Abs(x));
            }

            private static string Text(object w)
                => w == null || w == DBNull.Value ? "NULL"
                 : w is string s ? "'" + s + "'"
                 : Convert.ToString(w, CultureInfo.InvariantCulture);
        }
    }
}
