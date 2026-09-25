using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Schemaschritte der Anlagenkopplung, Stufe AK1 Welle 1 — <b>122 (AK-S1)</b> und
    /// <b>123 (AK-S3, Wärmeteil)</b> (Konzept Anlagenkopplung Kapitel 8; Entscheide E22, E24, E25).
    ///
    /// <para><b>Geprüft wird:</b> die Definitionen gegen das Papier (13 Spalten je
    /// Gebäudetabelle, die Projektspalte mit Wertliste, drei Ergebnisspalten — namentlich, mit
    /// Typ und Reihenfolge), das Format des Wochenprofils samt strengem Leser (4.3, H-F10); der
    /// Stand der Testdatenbank (alles steht, alles leer bzw. 0, die Sicht ist die geltende,
    /// STRICT bleibt, die Prüfungen greifen); beide Schritte aus dem Stand davor, wiederholbar;
    /// <b>die vier fest verdrahteten Kopierwege</b> von <c>GebaeudeStammCtrl</c>
    /// (<c>BuildValueParams</c>, <c>Insert</c>, <c>Overwrite</c>, <c>CopyFromStamm</c>, 8.6)
    /// samt Namensleser NULL-erhaltend; die Wege über die ganze Zeile (Katalog duplizieren,
    /// Projektduplikat, Projekttransfer) und das Speichern der Kaskade, das die Zeile der
    /// Projekteinstellung neu anlegt; und dass ein Lauf die drei Ergebnisspalten leer lässt.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AnlagenkopplungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt mit genau einem Gebäude und einem Einstellungssatz.</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        /// <summary>Ein Referenzprojekt mit Einstellungssatz für das Speichern der Kaskade.</summary>
        private const int REFERENZ = 1030;

        /// <summary>Die 13 Spalten von 8.1 in der Reihenfolge des Papiers.</summary>
        private static readonly string[] SPALTEN_8_1 =
        {
            "Heizkreis_Aktiv", "Uebergabe_Art", "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn",
            "Auslegung_Vorlauf", "Auslegung_Ruecklauf", "Auslegung_Raumtemperatur",
            "Auslegung_Aussentemperatur", "Heizkurve_Aktiv", "Heizkurve_Niveau",
            "Heizkurve_Steilheit", "Regler_Proportionalband", "Sollwertprofil",
        };

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Der_Zielstand_ist_mindestens_123()
        {
            Assert.True(SchemaStand.Zielversion >= 123,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 123.");
        }

        /// <summary>
        /// AK-S1 nach 8.1: dreizehn Spalten je Gebäudetabelle, 26 Einträge, ASCII, ohne
        /// Dubletten zu M3 und KU-S1; die zwei Schalter 0/1 mit Vorgabe 0, die zwei Texte mit
        /// Längenprüfung, alle Zahlen nullbares <c>REAL</c> ohne Vorgabe.
        /// </summary>
        [Fact]
        public void AK_S1_dreizehn_Spalten_je_Gebaeudetabelle_nach_8_1_mit_ihren_Typen()
        {
            Assert.Equal(26, GebaeudeSchema.Uebergabespalten.Length);
            foreach (string t in GebaeudeSchema.TABELLEN)
                Assert.Equal(SPALTEN_8_1, GebaeudeSchema.Uebergabespalten.Where(s => s.Tabelle == t).Select(s => s.Name));

            var frueher = new HashSet<string>(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key)
                                              .Concat(GebaeudeSchema.KUEHL_SPALTEN.Select(s => s.Key))
                                              .Concat(GebaeudeSchema.SICHT_BESTAND), StringComparer.OrdinalIgnoreCase);
            foreach (SchemaSpalte s in GebaeudeSchema.Uebergabespalten)
            {
                Assert.True(s.Name.All(c => c < 128), s.Name + " ist nicht ASCII.");
                Assert.DoesNotContain(s.Name, frueher);
                string typ = AnlagenkopplungSchema.SqliteTyp(s);
                Assert.Equal(StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), typ);
                if (GebaeudeSchema.UEBERGABE_SCHALTER.Contains(s.Name))
                    Assert.Equal("INTEGER NOT NULL DEFAULT 0 CHECK (\"" + s.Name + "\" IN (0,1))", typ);
                else if (s.Name == "Uebergabe_Art")
                    Assert.Equal("TEXT CHECK (length(\"Uebergabe_Art\") <= 20)", typ);
                else if (s.Name == "Sollwertprofil")
                    Assert.Equal("TEXT CHECK (length(\"Sollwertprofil\") <= " +
                                 AnlagenkopplungSchema.PROFIL_LAENGE_MAX.ToString(CultureInfo.InvariantCulture) + ")", typ);
                else
                    Assert.Equal("REAL", typ);      // nullbar, ohne Vorgabe
            }
            Assert.Equal(new[] { "Uebergabe_Art", "Sollwertprofil" }, GebaeudeSchema.UEBERGABE_TEXTSPALTEN);
        }

        /// <summary>
        /// Die Projektspalte nach 8.1: <c>Tab_Einstellungen.Anlagenkopplung</c> mit der Wertliste
        /// AUS/AK1/AK2/AK3, nullbar, ohne Vorgabe — zusammen mit den Gebäudespalten 27 Einträge.
        /// Keiner davon steht in der Rückfallebene.
        /// </summary>
        [Fact]
        public void AK_S1_die_Projektspalte_mit_Wertliste_zusammen_27_Eintraege()
        {
            SchemaSpalte p = AnlagenkopplungSchema.Projektspalte;
            Assert.Equal("Tab_Einstellungen", p.Tabelle);
            Assert.Equal("Anlagenkopplung", p.Name);
            Assert.Equal("TEXT CHECK (\"Anlagenkopplung\" IN ('AUS','AK1','AK2','AK3'))", AnlagenkopplungSchema.SqliteTyp(p));
            Assert.DoesNotContain("DEFAULT", AnlagenkopplungSchema.TYP_ANLAGENKOPPLUNG, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NOT NULL", AnlagenkopplungSchema.TYP_ANLAGENKOPPLUNG, StringComparison.OrdinalIgnoreCase);

            List<SchemaSpalte> alle = AnlagenkopplungSchema.UebergabeSpalten().ToList();
            Assert.Equal(27, alle.Count);
            Assert.Same(p, alle.Last());

            var rueckfall = new HashSet<string>(SchemaKatalog.Alle.Select(s => s.Tabelle + "." + s.Name),
                                                StringComparer.OrdinalIgnoreCase);
            foreach (SchemaSpalte s in alle.Concat(AnlagenkopplungSchema.Ergebnisspalten))
                Assert.DoesNotContain(s.Tabelle + "." + s.Name, rueckfall);
        }

        /// <summary>
        /// Der dritte Sichtneubau: die 77 Spalten von M3 und KU-S1 an ihren Stellen, dahinter
        /// die dreizehn Übergabespalten — nach derselben Bauvorschrift
        /// (<see cref="GebaeudeSchema.SichtSql"/>); sie ist die GELTENDE Sicht.
        /// </summary>
        [Fact]
        public void AK_S1_haengt_die_dreizehn_Spalten_hinter_KU_S1_an_die_Sicht()
        {
            Assert.Equal(90, GebaeudeSchema.SICHT_UEBERGABE.Length);
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUNG, GebaeudeSchema.SICHT_UEBERGABE.Take(77));
            Assert.Equal(SPALTEN_8_1, GebaeudeSchema.SICHT_UEBERGABE.Skip(77));

            Assert.Equal(GebaeudeSchema.SichtSql(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key)
                                                 .Concat(GebaeudeSchema.KUEHL_SPALTEN.Select(s => s.Key))
                                                 .Concat(SPALTEN_8_1)),
                         GebaeudeSchema.SQL_VIEW_UEBERGABE);
            foreach (string s in SPALTEN_8_1)
            {
                Assert.Contains("Tab_Gebaeude." + s, GebaeudeSchema.SQL_VIEW_UEBERGABE, StringComparison.Ordinal);
                Assert.DoesNotContain("Tab_Gebaeude." + s, GebaeudeSchema.SQL_VIEW_KUEHLUNG, StringComparison.Ordinal);
            }
            // Die GELTENDE Sicht ist die des letzten Durchgangs (der Nachtzeit); sie beginnt mit
            // der Sicht von AK-S1 an denselben Stellen.
            Assert.Equal(GebaeudeSchema.SICHT_UEBERGABE, GebaeudeSchema.SICHT_AKTUELL.Take(90));
            Assert.Equal(GebaeudeSchema.SQL_VIEW_NACHTZEIT, GebaeudeSchema.SQL_VIEW_AKTUELL);
        }

        /// <summary>
        /// AK-S3, Wärmeteil nach 8.3: drei nullbare Spalten an <c>Tab_ErgebnisEnergiebedarf</c>.
        /// Der Komfortteil (AK2) und die Kältegegenstücke (F-A16) gehören NICHT dazu — und keines
        /// der Kältegegenstücke steht schon in einem anderen Schritt (keine Doppelung).
        /// </summary>
        [Fact]
        public void AK_S3_Waermeteil_drei_Ergebnisspalten_ohne_Komfort_und_Kaelteteil()
        {
            Assert.Equal(new[]
                         {
                             "Tab_ErgebnisEnergiebedarf.Vorlauf_Mittel",
                             "Tab_ErgebnisEnergiebedarf.Ruecklauf_Mittel",
                             "Tab_ErgebnisEnergiebedarf.Uebergabe_Begrenzt_Stunden",
                         },
                         AnlagenkopplungSchema.Ergebnisspalten.Select(s => s.Tabelle + "." + s.Name));
            foreach (SchemaSpalte s in AnlagenkopplungSchema.Ergebnisspalten)
                Assert.Equal("REAL", AnlagenkopplungSchema.SqliteTyp(s));

            string[] spaeter =
            {
                "Komfort_Unterschreitungsstunden", "Komfort_Kelvinstunden", "Komfort_Laengste_Strecke",
                "Fahrplan_Begrenzt_Stunden", "Kuehl_Vorlauf_Mittel", "Komfort_Ueberschreitungsstunden",
                "Komfort_Kelvinstunden_Kuehlung",
            };
            IEnumerable<string> alleSchritte = AnlagenkopplungSchema.Ergebnisspalten
                .Concat(AnlagenkopplungSchema.UebergabeSpalten())
                .Concat(KuehlungSchema.Ergebnisspalten).Concat(KuehlungSchema.Schritt119Spalten())
                .Concat(KuehlungSchema.Erzeugerspalten).Concat(SchemaKatalog.Alle)
                .Select(s => s.Name);
            foreach (string s in spaeter)
                Assert.DoesNotContain(s, alleSchritte);
        }

        /// <summary>Die Persistenzwerte sind ASCII, passen in ihre Spalten und decken die Wertliste.</summary>
        [Fact]
        public void Die_Persistenzwerte_sind_ASCII_und_passen_in_ihre_Spalten()
        {
            foreach (string w in new[] { DbWerte.UEBERGABE_IDEAL, DbWerte.UEBERGABE_RADIATOR,
                                         DbWerte.UEBERGABE_FLAECHE, DbWerte.UEBERGABE_KONVEKTOR })
            {
                Assert.True(w.All(c => c < 128) && w.Length <= 20, w);
            }
            Assert.Equal(new[] { "AUS", "AK1", "AK2", "AK3" }, AnlagenkopplungSchema.KOPPLUNGSSTUFEN);
        }

        /// <summary>
        /// Das Wochenprofil (4.3, H8): Schreiben und strenges Lesen ergeben dieselben 168 Werte,
        /// Montag 00:00 zuerst; das Dezimaltrennzeichen ist der Punkt, auch unter deutscher Kultur.
        /// </summary>
        [Fact]
        public void Wochenprofil_Schreiben_und_Lesen_ergeben_dieselben_168_Werte()
        {
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                double[] werte = Profil();
                string text = AnlagenkopplungSchema.WochenprofilSchreiben(werte);

                Assert.Equal(167, text.Count(c => c == ';'));
                Assert.DoesNotContain(",", text);
                Assert.StartsWith("17.5;17.5;17.5;17.5;17.5;17.5;21;", text, StringComparison.Ordinal);

                AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(text);
                Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.Gelesen, p.Befund);
                Assert.Equal(168, p.Gefunden);
                Assert.Equal(werte, p.Werte);

                // Leerraum um einen Wert ist erlaubt, das Ergebnis dasselbe.
                AnlagenkopplungSchema.Wochenprofil q = AnlagenkopplungSchema.WochenprofilLesen(text.Replace(";", " ; "));
                Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.Gelesen, q.Befund);
                Assert.Equal(werte, q.Werte);
            }
            finally
            {
                CultureInfo.CurrentCulture = vorher;
            }
        }

        /// <summary>
        /// DER LESER IST STRENG (H-F10): 167 oder 169 Werte, ein Trennzeichen am Ende, ein Komma
        /// als Dezimalzeichen, ein leerer Wert oder NaN sind benannte Befunde — mit der gefundenen
        /// Zahl bzw. der Stelle, und ohne Werte. NULL und leerer Text heißen „kein Profil".
        /// </summary>
        [Fact]
        public void Wochenprofil_der_Leser_ist_streng()
        {
            string gut = AnlagenkopplungSchema.WochenprofilSchreiben(Profil());
            string[] teile = gut.Split(';');

            foreach (string leer in new[] { null, "", "   " })
            {
                AnlagenkopplungSchema.Wochenprofil k = AnlagenkopplungSchema.WochenprofilLesen(leer);
                Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.KeinProfil, k.Befund);
                Assert.Null(k.Werte);
            }

            AnlagenkopplungSchema.Wochenprofil kurz = AnlagenkopplungSchema.WochenprofilLesen(string.Join(";", teile.Take(167)));
            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl, kurz.Befund);
            Assert.Equal(167, kurz.Gefunden);
            Assert.Null(kurz.Werte);

            AnlagenkopplungSchema.Wochenprofil lang = AnlagenkopplungSchema.WochenprofilLesen(gut + ";20");
            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl, lang.Befund);
            Assert.Equal(169, lang.Gefunden);

            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl,
                         AnlagenkopplungSchema.WochenprofilLesen(gut + ";").Befund);

            var komma = (string[])teile.Clone();
            komma[9] = "20,5";
            AnlagenkopplungSchema.Wochenprofil k10 = AnlagenkopplungSchema.WochenprofilLesen(string.Join(";", komma));
            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.KeineZahl, k10.Befund);
            Assert.Equal(10, k10.Stelle);
            Assert.Null(k10.Werte);

            foreach (string schlecht in new[] { "", "NaN", "Infinity", "abc" })
            {
                var t = (string[])teile.Clone();
                t[167] = schlecht;
                AnlagenkopplungSchema.Wochenprofil f = AnlagenkopplungSchema.WochenprofilLesen(string.Join(";", t));
                Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.KeineZahl, f.Befund);
                Assert.Equal(168, f.Stelle);
            }
        }

        /// <summary>
        /// Der Schreiber rundet auf zwei Nachkommastellen, schreibt keine „-0" und passt in die
        /// Spalte — oder er lehnt benannt ab: falsche Wertzahl, eine nicht endliche Zahl, ein
        /// Text über 1 400 Zeichen.
        /// </summary>
        [Fact]
        public void Wochenprofil_der_Schreiber_passt_in_die_Spalte_oder_lehnt_ab()
        {
            double[] werte = Enumerable.Repeat(-999.994, 168).ToArray();
            werte[0] = -0.001;
            werte[1] = 20.125;
            string text = AnlagenkopplungSchema.WochenprofilSchreiben(werte);
            Assert.StartsWith("0;20.13;-999.99;", text, StringComparison.Ordinal);
            Assert.True(text.Length <= AnlagenkopplungSchema.PROFIL_LAENGE_MAX, text.Length.ToString(CultureInfo.InvariantCulture));

            Assert.Throws<ArgumentException>(() => AnlagenkopplungSchema.WochenprofilSchreiben(new double[167]));
            Assert.Throws<ArgumentException>(() => AnlagenkopplungSchema.WochenprofilSchreiben(new double[169]));
            double[] nan = Profil();
            nan[5] = double.NaN;
            Assert.Throws<ArgumentException>(() => AnlagenkopplungSchema.WochenprofilSchreiben(nan));
            double[] gross = Enumerable.Repeat(-12345.67, 168).ToArray();
            Assert.Throws<ArgumentException>(() => AnlagenkopplungSchema.WochenprofilSchreiben(gross));
            Assert.Throws<ArgumentNullException>(() => AnlagenkopplungSchema.WochenprofilSchreiben(null));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank auf Stand 123
        // =============================================================================

        /// <summary>Das Referenzprojekt der Anlagenkopplung und sein Gebäude (Kopie von 1017 bzw. 10599).</summary>
        private const int PROJEKT_REFERENZ_KOPPLUNG = 1047, GEBAEUDE_REFERENZ_KOPPLUNG = 10653;

        /// <summary>
        /// Alle Strukturen stehen, die Sicht ist die geltende, und alle neuen Spalten sind leer
        /// (die Schalter 0) — bis auf die gesäten Übergabedaten des Referenzprojekts der
        /// Anlagenkopplung 1047 (Stufe AK1; Gebäude 10653 mit Heizkreis, Radiator und Heizkurve,
        /// sonst leer). Die angefassten Tabellen bleiben STRICT.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_123_und_alle_neuen_Spalten_sind_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= 123);
            Assert.True(AnlagenkopplungSchema.UebergabeVollstaendig());
            Assert.True(AnlagenkopplungSchema.ErgebnisspaltenVollstaendig());
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.True(GebaeudeSchema.Vollstaendig());

            string sicht = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture);
            // Die geltende Sicht (die des letzten Durchgangs) - sie fuehrt die Uebergabespalten an ihren Stellen.
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, sicht);
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(GebaeudeSchema.SICHT_UEBERGABE, GebaeudeSchema.SichtSpalten().Take(90));

            // Gesät ist allein das Referenzprojekt der Anlagenkopplung 1047 (Einfrierregel „gesäte
            // Auslegungsdaten der Übergabe", anlagenkopplung_1047_referenzprojekt.py): die Stufe AK1
            // und am Gebäude 10653 Heizkreis, Radiator und Heizkurve, alles Übrige leer.
            foreach (SchemaSpalte s in AnlagenkopplungSchema.UebergabeSpalten().Concat(AnlagenkopplungSchema.Ergebnisspalten))
            {
                string ausser = s.Tabelle == "Tab_Gebaeude"
                    ? " AND ID <> " + GEBAEUDE_REFERENZ_KOPPLUNG.ToString(CultureInfo.InvariantCulture)
                    : s.Tabelle == "Tab_Einstellungen"
                        ? " AND ID_Projekt <> " + PROJEKT_REFERENZ_KOPPLUNG.ToString(CultureInfo.InvariantCulture)
                        : "";
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL AND [" +
                                      s.Name + "] <> 0" + ausser));
                if (!GebaeudeSchema.UEBERGABE_SCHALTER.Contains(s.Name))
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL" + ausser));
            }
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Anlagenkopplung FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", PROJEKT_REFERENZ_KOPPLUNG)), CultureInfo.InvariantCulture));
            DataRow referenz = DataRepository.GetDataTable("SELECT * FROM Tab_Gebaeude WHERE ID = ?",
                                                           new DbParam("?", GEBAEUDE_REFERENZ_KOPPLUNG)).Rows[0];
            foreach (SchemaSpalte s in GebaeudeSchema.Uebergabespalten.Where(x => x.Tabelle == "Tab_Gebaeude"))
            {
                if (GebaeudeSchema.UEBERGABE_SCHALTER.Contains(s.Name))
                    Assert.Equal(1L, Convert.ToInt64(referenz[s.Name], CultureInfo.InvariantCulture));
                else if (s.Name == "Uebergabe_Art")
                    Assert.Equal(DbWerte.UEBERGABE_RADIATOR, Convert.ToString(referenz[s.Name], CultureInfo.InvariantCulture));
                else
                    Assert.Equal(DBNull.Value, referenz[s.Name]);
            }
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Gebaeude") > 0);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Einstellungen") > 0);

            foreach (string t in AnlagenkopplungSchema.UebergabeSpalten().Concat(AnlagenkopplungSchema.Ergebnisspalten)
                                                      .Select(s => s.Tabelle).Distinct())
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// AK-S1 aus dem Stand VOR ihm: Sicht, Übergabespalten und Projektspalte werden entfernt,
        /// die Sicht von KU-S1 steht; der Schritt legt die 27 Spalten an und baut die Sicht neu —
        /// die Bestandswerte bleiben, M3 und KU-S1 stehen weiter, und ein zweiter Lauf legt nichts
        /// mehr an.
        /// </summary>
        [Fact]
        public void AK_S1_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();

            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (SchemaSpalte s in AnlagenkopplungSchema.UebergabeSpalten())
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_KUEHLUNG);    // der Stand nach Schritt 108
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.False(AnlagenkopplungSchema.UebergabeVollstaendig());
            Assert.False(DataRepository.SpalteVorhanden("Tab_Einstellungen", "Anlagenkopplung"));

            var bericht = new List<string>();
            Assert.Equal(27, AnlagenkopplungSchema.UebergabeAlle(bericht));
            Assert.Contains(bericht, z => z.Contains("26 von 26 Uebergabespalte(n) angelegt"));
            Assert.Contains(bericht, z => z.Contains("Tab_Einstellungen.Anlagenkopplung: angelegt"));
            Assert.True(AnlagenkopplungSchema.UebergabeVollstaendig());
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.True(GebaeudeSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());

            Assert.Equal(0, AnlagenkopplungSchema.UebergabeAlle(null));
            Assert.True(AnlagenkopplungSchema.UebergabeVollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_UEBERGABE, GebaeudeSchema.SichtSpalten());
        }

        /// <summary>AK-S3 (Wärmeteil) aus dem Stand VOR ihm, wiederholbar; kein DML.</summary>
        [Fact]
        public void AK_S3_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            long zeilen = Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf");
            foreach (SchemaSpalte s in AnlagenkopplungSchema.Ergebnisspalten)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            Assert.False(AnlagenkopplungSchema.ErgebnisspaltenVollstaendig());

            var bericht = new List<string>();
            Assert.Equal(3, AnlagenkopplungSchema.ErgebnisspaltenAlle(bericht));
            Assert.Contains(bericht, z => z.Contains("3 von 3 Ergebnisspalte(n)"));
            Assert.True(AnlagenkopplungSchema.ErgebnisspaltenVollstaendig());
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf"));
            Assert.Equal(0, AnlagenkopplungSchema.ErgebnisspaltenAlle(null));
        }

        /// <summary>
        /// Die Prüfungen der Spalten greifen: kein fremder Kopplungswert, kein Schalter außer 0/1,
        /// keine Übergabeart über 20 und kein Profil über 1 400 Zeichen — und die zulässigen Werte
        /// gehen durch, NULL eingeschlossen.
        /// </summary>
        [Fact]
        public void Die_Pruefungen_der_Spalten_weisen_ungueltige_Werte_ab()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Wirft("UPDATE Tab_Einstellungen SET Anlagenkopplung = 'AK4' WHERE ID_Projekt = " + PROJEKT));
            Assert.True(Wirft("UPDATE Tab_Einstellungen SET Anlagenkopplung = 'aus' WHERE ID_Projekt = " + PROJEKT));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 2 WHERE ID = " + GEBAEUDE));
            Assert.True(Wirft("UPDATE Tab_Gebaeude_STAMM SET Heizkurve_Aktiv = NULL WHERE ID = 1"));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Uebergabe_Art = ? WHERE ID = " + GEBAEUDE,
                              new DbParam("?", new string('X', 21))));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Sollwertprofil = ? WHERE ID = " + GEBAEUDE,
                              new DbParam("?", new string('1', 1401))));
            Assert.True(Wirft("INSERT INTO Tab_ErgebnisEnergiebedarf (ID, Vorlauf_Mittel) VALUES (999999, 'viel')"));

            foreach (string stufe in AnlagenkopplungSchema.KOPPLUNGSSTUFEN)
                Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, stufe), stufe);
            Assert.Equal("AK3", KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, null));
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.False(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, "AK9"));
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
        }

        // =============================================================================
        //  Teil 3 - die vier fest verdrahteten Kopierwege von GebaeudeStammCtrl (8.6)
        // =============================================================================

        /// <summary>
        /// KOPIERWEG 1 UND 2 — <c>BuildValueParams</c> (die Parameterliste) und <c>Insert</c> (die
        /// Spaltenliste des Katalogs): Ein Modell mit gesetzten und leeren Feldern kommt so in der
        /// Tabelle an — NULL wird nicht 0, und der Leser (<c>NeueSpaltenLesen</c>) bringt NULL
        /// als <c>null</c> zurück.
        /// </summary>
        [Fact]
        public void Kopierweg_1_und_2_BuildValueParams_und_Insert_halten_NULL()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "AK-S1-Probe Insert";
            GebaeudeModel m = Teilbelegt(NAME);
            Assert.True(new GebaeudeStammCtrl().Insert(m));

            DataRow r = Zeile("SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME);
            PruefeTeilbelegt(r);
            PruefeTeilbelegt(Lesen(NAME));
        }

        /// <summary>
        /// KOPIERWEG 3 — <c>Overwrite</c> (die SET-Liste des Katalogs): gesetzte Felder werden
        /// NULL, leere bekommen einen Wert, die Schalter kippen — in beide Richtungen ohne 0 für
        /// NULL.
        /// </summary>
        [Fact]
        public void Kopierweg_3_Overwrite_haelt_NULL_in_beide_Richtungen()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "AK-S1-Probe Overwrite";
            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(Teilbelegt(NAME)));

            GebaeudeModel g = Lesen(NAME);
            g.Heizkreis_Aktiv = false;
            g.Uebergabe_Art = null;
            g.Uebergabe_Exponent = 1.1;
            g.Auslegung_Vorlauf = null;
            g.Auslegung_Aussentemperatur = -12;
            g.Heizkurve_Aktiv = true;
            g.Heizkurve_Steilheit = 1.2;
            g.Regler_Proportionalband = null;
            g.Sollwertprofil = null;
            Assert.True(ctrl.Overwrite(g));

            DataRow r = Zeile("SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME);
            Assert.Equal(0L, Convert.ToInt64(r["Heizkreis_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Uebergabe_Art"]);
            Assert.Equal(1.1, Convert.ToDouble(r["Uebergabe_Exponent"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Uebergabe_Leistung_Nenn"]);
            Assert.Equal(DBNull.Value, r["Auslegung_Vorlauf"]);
            Assert.Equal(45.0, Convert.ToDouble(r["Auslegung_Ruecklauf"], CultureInfo.InvariantCulture));
            Assert.Equal(-12.0, Convert.ToDouble(r["Auslegung_Aussentemperatur"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(r["Heizkurve_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Heizkurve_Niveau"]);
            Assert.Equal(1.2, Convert.ToDouble(r["Heizkurve_Steilheit"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Regler_Proportionalband"]);
            Assert.Equal(DBNull.Value, r["Sollwertprofil"]);

            GebaeudeModel wieder = Lesen(NAME);
            Assert.False(wieder.Heizkreis_Aktiv);
            Assert.Null(wieder.Uebergabe_Art);
            Assert.Equal(1.1, wieder.Uebergabe_Exponent);
            Assert.Null(wieder.Auslegung_Vorlauf);
            Assert.True(wieder.Heizkurve_Aktiv);
            Assert.Null(wieder.Regler_Proportionalband);
            Assert.Null(wieder.Sollwertprofil);
        }

        /// <summary>
        /// KOPIERWEG 4 — <c>CopyFromStamm</c> (Spalten- und Parameterliste Katalog → Projekt), die
        /// Falle aus 8.6: Aus einem leeren Exponenten wird KEIN 0,0; gesetzte Werte, beide Schalter
        /// und das Profil kommen an. Der Leser der Projektzeile (<c>GebaeudeCtrl</c>) sieht dasselbe.
        /// </summary>
        [Fact]
        public void Kopierweg_4_CopyFromStamm_haelt_NULL_und_traegt_gesetzte_Werte()
        {
            if (!_db.Vorhanden) return;

            DataRow stamm = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0];
            int idStamm = Convert.ToInt32(stamm["ID"], CultureInfo.InvariantCulture);
            string bezeichner = Convert.ToString(stamm["Bezeichner"], CultureInfo.InvariantCulture);
            SetzeTeilbelegt("Tab_Gebaeude_STAMM", idStamm);

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idNeu = new GebaeudeStammCtrl().CopyFromStamm(
                bezeichner, Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture),
                Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture));
            Assert.True(idNeu > 0);

            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID = ?", idNeu));

            var projekt = new GebaeudeCtrl();
            projekt.ReadAll("ID = " + idNeu.ToString(CultureInfo.InvariantCulture));
            PruefeTeilbelegt(Assert.Single(projekt.items));
        }

        /// <summary>
        /// DER NAMENSLESER DER SICHT: <c>ProjektGebaeudeCtrl</c> liefert die dreizehn Felder eines
        /// Gebäudes so, wie sie in der Tabelle stehen; die übrigen Gebäude bleiben leer bzw. aus.
        /// </summary>
        [Fact]
        public void Der_Namensleser_der_Sicht_liefert_die_Uebergabespalten_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            DataRow g = DataRepository.GetDataTable(
                "SELECT g.ID, z.ID_Projekt FROM Tab_Gebaeude g INNER JOIN Z_ProjektGebaeude z " +
                "ON z.ID = g.ID_ProjektGebaeude ORDER BY g.ID LIMIT 1").Rows[0];
            int idGebaeude = Convert.ToInt32(g["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(g["ID_Projekt"], CultureInfo.InvariantCulture);
            SetzeTeilbelegt("Tab_Gebaeude", idGebaeude);

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            PruefeTeilbelegt(ctrl.items.Single(x => x.ID_Gebaeude == idGebaeude));

            foreach (ProjektGebaeudeModel andere in ctrl.items.Where(x => x.ID_Gebaeude != idGebaeude))
            {
                Assert.False(andere.Heizkreis_Aktiv);
                Assert.Null(andere.Uebergabe_Art);
                Assert.Null(andere.Sollwertprofil);
            }
        }

        // =============================================================================
        //  Teil 4 - die Wege über die ganze Zeile und die Projektspalte
        // =============================================================================

        /// <summary>„Duplizieren…" der Gebäudeverwaltung kopiert jede Spalte — auch die dreizehn, NULL bleibt NULL.</summary>
        [Fact]
        public void Katalog_Duplizieren_traegt_die_Uebergabespalten()
        {
            if (!_db.Vorhanden) return;

            int idStamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            SetzeTeilbelegt("Tab_Gebaeude_STAMM", idStamm);

            Katalogkopie.Ergebnis e = GebaeudeStammCtrl.Duplizieren(idStamm, "AK-S1-Probe Kopie");
            Assert.True(e.Ok, e.Meldung);
            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude_STAMM WHERE ID = ?", e.Id));
        }

        /// <summary>
        /// Das Projektduplikat (derselbe Weg wie „Variante anlegen") trägt die Gebäudespalten und
        /// die Kopplungsstufe mit — der Fall, der bei jeder neuen Spalte vergessen wird (11.3).
        /// </summary>
        [Fact]
        public void Projektduplikat_traegt_Gebaeudespalten_und_Kopplungsstufe()
        {
            if (!_db.Vorhanden) return;

            SetzeTeilbelegt("Tab_Gebaeude", GEBAEUDE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));

            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " AK-S1");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, KonfigurationCtrl.AnlagenkopplungLesen(neu));

            // Und ein Projekt auf „aus" bleibt NULL, nicht „AUS".
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, null));
            int aus = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " AK-S1 aus");
            Assert.True(aus > 0, "Duplizieren fehlgeschlagen.");
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(aus));
        }

        /// <summary>
        /// Der Projekttransfer (Export und Import eines Pakets) trägt die Gebäudespalten und die
        /// Kopplungsstufe NULL-erhaltend über die Paketgrenze.
        /// </summary>
        [Fact]
        public void Projekttransfer_traegt_Gebaeudespalten_und_Kopplungsstufe()
        {
            if (!_db.Vorhanden) return;

            SetzeTeilbelegt("Tab_Gebaeude", GEBAEUDE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));

            string ordner = Path.Combine(Path.GetTempPath(), "epos-aks1-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));

                int neu = io.Importieren(paket, "Transfer AK-S1", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu));
                Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, KonfigurationCtrl.AnlagenkopplungLesen(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>
        /// Die Konfigurationsseite speichert die Kaskade als Löschen und Neuanlegen der ganzen
        /// Zeile (<c>KonfigurationCtrl.Insert</c> mit fest verdrahteter Spaltenliste, ohne die
        /// Kopplungsstufe). Eine gesetzte Stufe steht danach weiter da, NULL bleibt NULL.
        /// </summary>
        [Fact]
        public void Das_Speichern_der_Kaskade_erhaelt_die_Kopplungsstufe()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(REFERENZ, DbWerte.ANLAGENKOPPLUNG_AK1));
            long idVorher = Zahl("SELECT ID FROM Tab_Einstellungen WHERE ID_Projekt = " + REFERENZ);

            Assert.True(Kaskadendienste(REFERENZ).Speichern());

            Assert.NotEqual(idVorher, Zahl("SELECT ID FROM Tab_Einstellungen WHERE ID_Projekt = " + REFERENZ));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, KonfigurationCtrl.AnlagenkopplungLesen(REFERENZ));

            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(REFERENZ, null));
            Assert.True(Kaskadendienste(REFERENZ).Speichern());
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(REFERENZ));
        }

        // =============================================================================
        //  Teil 5 - ergebnisneutral: ein Lauf lässt die drei Ergebnisspalten leer
        // =============================================================================

        /// <summary>
        /// Ein Lauf auf der Testdatenbank schreibt seine Ergebniszeile wie bisher — und lässt die
        /// drei Spalten von AK-S3 leer: NULL heißt „nicht erhoben", kein Rechenweg kennt sie.
        /// </summary>
        [Fact]
        public void Ein_Lauf_laesst_die_drei_Ergebnisspalten_leer()
        {
            if (!_db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            int kopf = laeufer.SimuliereUndSpeichere(REFERENZ, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            string lauf = kopf.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = " + lauf));
            foreach (SchemaSpalte s in AnlagenkopplungSchema.Ergebnisspalten)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = " + lauf +
                                      " AND [" + s.Name + "] IS NOT NULL"));
        }

        // =============================================================================
        //  Teil 6 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache der Schritte 122 und 123.</b> Alle drei Wege führen sie
        /// (Migration der Schale, Werkzeug <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests)
        /// aus derselben Quelle, und die REPO-Datei trägt sie (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_die_Schritte_122_und_123()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("AnlagenkopplungSchema.UebergabeAlle(", werkzeug);
            Assert.Contains("AnlagenkopplungSchema.ErgebnisspaltenAlle(", werkzeug);

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_122_ANLAGENKOPPLUNG_UEBERGABE = 122", migration);
            Assert.Contains("SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS = 123", migration);
            int ort121 = migration.IndexOf("new Schritt(SCHRITT_121_GEBAEUDE_KATALOGVERWEIS", StringComparison.Ordinal);
            int ort122 = migration.IndexOf("new Schritt(SCHRITT_122_ANLAGENKOPPLUNG_UEBERGABE", StringComparison.Ordinal);
            int ort123 = migration.IndexOf("new Schritt(SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS", StringComparison.Ordinal);
            Assert.True(ort121 > 0 && ort122 > ort121 && ort123 > ort122, "Die Schritte 122 und 123 stehen nicht nach 121.");
            Assert.Contains("GebaeudeSchema.SQL_VIEW_UEBERGABE", migration);
            Assert.Contains("AnlagenkopplungSchema.SqliteTyp(", migration);
            Assert.Contains("AnlagenkopplungSchema.Ergebnisspalten", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("AnlagenkopplungSchema.UebergabeAlle(null)", vorrichtung);
            Assert.Contains("AnlagenkopplungSchema.ErgebnisspaltenAlle(null)", vorrichtung);

            string export = File.ReadAllText(Path.Combine(wurzel, "Referenzlauf", "Ergebnisexport.cs"));
            Assert.Contains("AnlagenkopplungSchema.Ergebnisspalten", export);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= 123);
            foreach (SchemaSpalte s in AnlagenkopplungSchema.UebergabeSpalten().Concat(AnlagenkopplungSchema.Ergebnisspalten))
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + s.Tabelle + "') WHERE name = '" +
                                                  s.Name + "'"));
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = '" + GebaeudeSchema.VIEW + "'";
                Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Ein Wochenprofil mit runden Werten: 21 °C von 6 bis 22 Uhr, sonst 17,5 °C.</summary>
        private static double[] Profil()
            => Enumerable.Range(0, AnlagenkopplungSchema.WOCHENWERTE)
                         .Select(h => (h % 24) >= 6 && (h % 24) < 22 ? 21.0 : 17.5)
                         .ToArray();

        /// <summary>
        /// Die Belegung der Proben: der Heizkreis an, Radiator, Auslegung 55/45 °C, Band 0,5 K und
        /// ein Profil gesetzt — Exponent, Nennleistung, Raum- und Außentemperatur, Niveau und
        /// Steilheit bleiben leer, die Heizkurve aus. Runde, erfundene Werte.
        /// </summary>
        private static GebaeudeModel Teilbelegt(string name) => new GebaeudeModel
        {
            Gebaeudename = name,
            Nutzflaeche = 100,
            Heizkreis_Aktiv = true,
            Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR,
            Auslegung_Vorlauf = 55,
            Auslegung_Ruecklauf = 45,
            Regler_Proportionalband = 0.5,
            Sollwertprofil = AnlagenkopplungSchema.WochenprofilSchreiben(Profil()),
        };

        /// <summary>Dieselbe Belegung per SQL in eine Tabellenzeile (Katalog oder Projekt).</summary>
        private static void SetzeTeilbelegt(string tabelle, int id)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE [" + tabelle + "] SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Uebergabe_Exponent = NULL, " +
                "Uebergabe_Leistung_Nenn = NULL, Auslegung_Vorlauf = ?, Auslegung_Ruecklauf = ?, " +
                "Auslegung_Raumtemperatur = NULL, Auslegung_Aussentemperatur = NULL, Heizkurve_Aktiv = 0, " +
                "Heizkurve_Niveau = NULL, Heizkurve_Steilheit = NULL, Regler_Proportionalband = ?, " +
                "Sollwertprofil = ? WHERE ID = ?",
                new DbParam("?", DbWerte.UEBERGABE_RADIATOR), new DbParam("?", 55.0), new DbParam("?", 45.0),
                new DbParam("?", 0.5), new DbParam("?", AnlagenkopplungSchema.WochenprofilSchreiben(Profil())),
                new DbParam("?", id)));
        }

        /// <summary>Die Tabellenzeile trägt die Belegung — NULL als DBNull, nicht als 0.</summary>
        private static void PruefeTeilbelegt(DataRow r)
        {
            Assert.Equal(1L, Convert.ToInt64(r["Heizkreis_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, Convert.ToString(r["Uebergabe_Art"], CultureInfo.InvariantCulture));
            Assert.True(r["Uebergabe_Exponent"] == DBNull.Value, "Uebergabe_Exponent ist nicht NULL.");
            Assert.True(r["Uebergabe_Leistung_Nenn"] == DBNull.Value, "Uebergabe_Leistung_Nenn ist nicht NULL.");
            Assert.Equal(55.0, Convert.ToDouble(r["Auslegung_Vorlauf"], CultureInfo.InvariantCulture));
            Assert.Equal(45.0, Convert.ToDouble(r["Auslegung_Ruecklauf"], CultureInfo.InvariantCulture));
            Assert.True(r["Auslegung_Raumtemperatur"] == DBNull.Value, "Auslegung_Raumtemperatur ist nicht NULL.");
            Assert.True(r["Auslegung_Aussentemperatur"] == DBNull.Value, "Auslegung_Aussentemperatur ist nicht NULL.");
            Assert.Equal(0L, Convert.ToInt64(r["Heizkurve_Aktiv"], CultureInfo.InvariantCulture));
            Assert.True(r["Heizkurve_Niveau"] == DBNull.Value, "Heizkurve_Niveau ist nicht NULL.");
            Assert.True(r["Heizkurve_Steilheit"] == DBNull.Value, "Heizkurve_Steilheit ist nicht NULL.");
            Assert.Equal(0.5, Convert.ToDouble(r["Regler_Proportionalband"], CultureInfo.InvariantCulture));
            Assert.Equal(AnlagenkopplungSchema.WochenprofilSchreiben(Profil()),
                         Convert.ToString(r["Sollwertprofil"], CultureInfo.InvariantCulture));
        }

        /// <summary>Das Katalog- bzw. Projektmodell trägt die Belegung — NULL als <c>null</c>.</summary>
        private static void PruefeTeilbelegt(GebaeudeModel m)
        {
            Assert.True(m.Heizkreis_Aktiv);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, m.Uebergabe_Art);
            Assert.Null(m.Uebergabe_Exponent);
            Assert.Null(m.Uebergabe_Leistung_Nenn);
            Assert.Equal(55.0, m.Auslegung_Vorlauf);
            Assert.Equal(45.0, m.Auslegung_Ruecklauf);
            Assert.Null(m.Auslegung_Raumtemperatur);
            Assert.Null(m.Auslegung_Aussentemperatur);
            Assert.False(m.Heizkurve_Aktiv);
            Assert.Null(m.Heizkurve_Niveau);
            Assert.Null(m.Heizkurve_Steilheit);
            Assert.Equal(0.5, m.Regler_Proportionalband);
            Assert.Equal(Profil(), AnlagenkopplungSchema.WochenprofilLesen(m.Sollwertprofil).Werte);
        }

        /// <summary>Das Modell des Namenslesers trägt die Belegung — NULL als <c>null</c>.</summary>
        private static void PruefeTeilbelegt(ProjektGebaeudeModel m)
        {
            Assert.True(m.Heizkreis_Aktiv);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, m.Uebergabe_Art);
            Assert.Null(m.Uebergabe_Exponent);
            Assert.Null(m.Uebergabe_Leistung_Nenn);
            Assert.Equal(55.0, m.Auslegung_Vorlauf);
            Assert.Equal(45.0, m.Auslegung_Ruecklauf);
            Assert.Null(m.Auslegung_Raumtemperatur);
            Assert.Null(m.Auslegung_Aussentemperatur);
            Assert.False(m.Heizkurve_Aktiv);
            Assert.Null(m.Heizkurve_Niveau);
            Assert.Null(m.Heizkurve_Steilheit);
            Assert.Equal(0.5, m.Regler_Proportionalband);
            Assert.Equal(Profil(), AnlagenkopplungSchema.WochenprofilLesen(m.Sollwertprofil).Werte);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, object wert)
        {
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", wert));
            Assert.True(dt != null && dt.Rows.Count == 1, "Keine eindeutige Zeile: " + sql);
            return dt.Rows[0];
        }

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        /// <summary>Scheitert die Anweisung an einer Prüfung? Der Vorgang wird nie festgeschrieben.</summary>
        private static bool Wirft(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, parameter);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        private static SimulationKonfigDienste Kaskadendienste(int idProjekt)
            => (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(idProjekt).Gaben()["Dienste"];

        /// <summary>Die Bestandswerte der angefassten Tabellen, an denen AK-S1 nichts ändern darf.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT ID, Nutzflaeche, Raumsolltemperatur_Tag, Kuehlung_Aktiv, Kuehl_Sollwert FROM Tab_Gebaeude ORDER BY ID",
                         "SELECT ID, Nutzflaeche, Raumsolltemperatur_Tag, Kuehlung_Aktiv, Kuehl_Sollwert FROM Tab_Gebaeude_STAMM ORDER BY ID",
                         "SELECT ID, ID_Projekt, Tool_1, Kuehlbetrieb FROM Tab_Einstellungen ORDER BY ID",
                     })
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                foreach (DataRow r in dt.Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
