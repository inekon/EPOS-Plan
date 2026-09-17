using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache über die Saat des Gesetzeskatalogs</b> (Auftrag US-1).
    ///
    /// <para><b>Warum es sie gibt.</b> <c>GesetzKatalog.StelleKatalogSicher</c> schreibt
    /// über <c>StilleDb</c>, und die schluckt jeden Fehler. Eine Saatzeile, die an einem
    /// CHECK von <c>Tab_Gesetzesparameter</c> scheitert, kommt deshalb nicht an — ohne
    /// dass irgendetwas rot wird: Der Rechenweg fällt auf seine Konstante zurück und
    /// rechnet weiter richtig, nur Katalog und Schnellwahl bleiben leer. Genau so sind
    /// die drei Umlagenzeilen der Generation 7 verschwunden (Quelle 123 bzw. 124 Zeichen
    /// gegen <c>CHECK (length("Quelle") &lt;= 120)</c>).</para>
    ///
    /// <para><b>Die Schranken kommen aus dem Schema</b>, nicht aus dem Gedächtnis:
    /// <c>sql/schema/001_grundschema.sql</c> ist die Quelle, aus der die Datenbank
    /// entsteht. Ändert dort jemand eine Länge, misst diese Wache sofort gegen die neue.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GesetzkatalogSaatWacheTests
    {
        private const string TABELLE = "Tab_Gesetzesparameter";

        // ==================================================================
        //  1 — Die Spaltenschranken, gegen das Schema gemessen
        // ==================================================================

        /// <summary>
        /// <b>Keine Saatzeile reißt eine Längenschranke.</b> Geprüft werden alle Spalten
        /// von <c>Tab_Gesetzesparameter</c>, die im Grundschema ein
        /// <c>CHECK (length(...) &lt;= n)</c> tragen — heute Schluessel, Klasse, Einheit,
        /// Status und Quelle.
        /// </summary>
        [Fact]
        public void JedeSaatzeileBleibtInDenSchrankenDesSchemas()
        {
            IDictionary<string, int> schranken = Laengenschranken();

            // Die Wache selbst muss etwas zu messen haben: Fällt der Block im Schema weg
            // oder ändert er seine Schreibweise, ist das hier rot und nicht still.
            Assert.Equal(new[] { "Einheit", "Klasse", "Quelle", "Schluessel", "Status" },
                         schranken.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
            Assert.Equal(120, schranken["Quelle"]);

            var risse = new List<string>();
            foreach (GesetzParameter p in GesetzKatalog.Vorbelegung())
            {
                Pruefe(risse, schranken, p, "Schluessel", p.Schluessel);
                Pruefe(risse, schranken, p, "Klasse", p.Klasse);
                Pruefe(risse, schranken, p, "Einheit", p.Einheit);
                Pruefe(risse, schranken, p, "Status", p.Status);
                Pruefe(risse, schranken, p, "Quelle", p.Quelle);
            }

            Assert.True(risse.Count == 0,
                        "Saatzeilen über der Schranke von " + TABELLE + ":" +
                        Environment.NewLine + string.Join(Environment.NewLine, risse));
        }

        private static void Pruefe(ICollection<string> risse, IDictionary<string, int> schranken,
                                   GesetzParameter p, string spalte, string wert)
        {
            int grenze;
            if (!schranken.TryGetValue(spalte, out grenze)) return;
            if (wert == null || wert.Length <= grenze) return;

            risse.Add("  " + p.Schluessel + " (" + p.Klasse + ", ab " +
                      p.JahrVon.ToString(CultureInfo.InvariantCulture) + ", Generation " +
                      p.Generation.ToString(CultureInfo.InvariantCulture) + "): " + spalte +
                      " hat " + wert.Length.ToString(CultureInfo.InvariantCulture) +
                      " Zeichen, erlaubt sind " + grenze.ToString(CultureInfo.InvariantCulture) + ".");
        }

        /// <summary>
        /// Die <c>CHECK (length("Spalte") &lt;= n)</c> des Blocks
        /// <c>CREATE TABLE "Tab_Gesetzesparameter"</c> aus dem Grundschema.
        /// </summary>
        private static IDictionary<string, int> Laengenschranken()
        {
            string schema = File.ReadAllText(
                Path.Combine(Wurzel(), "sql", "schema", "001_grundschema.sql"));

            int start = schema.IndexOf("CREATE TABLE \"" + TABELLE + "\"", StringComparison.Ordinal);
            Assert.True(start >= 0, "Der CREATE-Block von " + TABELLE + " steht nicht im Grundschema.");

            int ende = schema.IndexOf(";", start, StringComparison.Ordinal);
            Assert.True(ende > start, "Der CREATE-Block von " + TABELLE + " ist nicht abgeschlossen.");

            var schranken = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(schema.Substring(start, ende - start),
                                              "CHECK\\s*\\(\\s*length\\(\"(?<s>[A-Za-z_]+)\"\\)\\s*<=\\s*(?<n>\\d+)\\s*\\)"))
                schranken[m.Groups["s"].Value] = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);

            return schranken;
        }

        // ==================================================================
        //  2 — Die Nachsaat auf einer Arbeitskopie
        // ==================================================================

        /// <summary>
        /// <b>Nach dem Einsäen stehen die drei Umlagenzeilen in der Datenbank</b> — mit
        /// 0,446, 0,941 und 1,559 ct/kWh, Stichjahr 2026.
        ///
        /// <para>Der Fall räumt auf der EIGENEN Arbeitskopie erst die Generation 7 ab und
        /// setzt die Markerzeile zurück; sonst wäre er auf einer bereits gesäten
        /// Datenbank nur eine Lesekontrolle und würde den Schreibweg — den, an dem der
        /// CHECK zuschlägt — gar nicht mehr berühren.</para>
        /// </summary>
        [Fact]
        public void DieNachsaatBringtDieDreiUmlagenzeilenInDieDatenbank()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                // Generation 7 abräumen und den Marker auf 6 zurücksetzen.
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM " + TABELLE + " WHERE Klasse = ?",
                    new DbParam("@k", DbParamTyp.VarWChar, 40) { Wert = DbWerte.GESETZ_KLASSE_UMLAGEN });
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM " + TABELLE + " WHERE Schluessel = ?",
                    new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_STROMST_REDUZIERT });
                DataRepository.ExecuteNonQuery(
                    "UPDATE " + TABELLE + " SET [Wert] = 6 WHERE Schluessel = ?",
                    new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_KATALOG_GENERATION });

                Assert.Empty(new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_UMLAGEN));

                GesetzKatalog.StelleKatalogSicher();

                Assert.True(GesetzKatalog.SaatWarnungen.Count == 0,
                            "Die Nachsaat meldet Warnungen: " +
                            string.Join(" | ", GesetzKatalog.SaatWarnungen));
                Assert.Equal(4, GesetzKatalog.ZuletztNachgesaet);

                IList<GesetzParameter> umlagen =
                    new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_UMLAGEN);
                Assert.Equal(3, umlagen.Count);

                Erwarte(umlagen, DbWerte.GESETZ_UMLAGE_KWKG, 0.446);
                Erwarte(umlagen, DbWerte.GESETZ_UMLAGE_OFFSHORE, 0.941);
                Erwarte(umlagen, DbWerte.GESETZ_UMLAGE_STROMNEV19, 1.559);

                // Der Marker steht wieder auf der Zielgeneration; ein zweiter Lauf
                // legt nichts an (Idempotenz).
                GesetzKatalog.StelleKatalogSicher();
                Assert.Equal(0, GesetzKatalog.ZuletztNachgesaet);
                Assert.Equal(3, new GesetzKatalog().AlleDerKlasse(DbWerte.GESETZ_KLASSE_UMLAGEN).Count);
            }
        }

        // ==================================================================
        //  3 — Der stille Fehler traegt jetzt einen Namen
        // ==================================================================

        /// <summary>
        /// <b>Ein Saatfehler verschwindet nicht mehr.</b> Bis Auftrag US-1 endete er in
        /// einem leeren <c>catch</c>; jetzt steht er in
        /// <c>GesetzKatalog.SaatWarnungen</c> — mit Schlüssel, Klasse, Stichjahr und dem
        /// Grund, den SQLite gemeldet hat.
        ///
        /// <para>Den Fehlschlag stellt ein Trigger auf der Arbeitskopie her: Er weist
        /// genau eine Saatzeile ab, so wie es der Längen-CHECK getan hat. Die
        /// Datenbankseite selbst bleibt damit unberührt.</para>
        /// </summary>
        [Fact]
        public void EinSaatfehlerStehtMitSchluesselUndGrundInDenWarnungen()
        {
            using (var eigene = new TestDatenbank())
            {
                if (!eigene.Vorhanden) return;

                DataRepository.ExecuteNonQuery(
                    "DELETE FROM " + TABELLE + " WHERE Klasse = ?",
                    new DbParam("@k", DbParamTyp.VarWChar, 40) { Wert = DbWerte.GESETZ_KLASSE_UMLAGEN });
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM " + TABELLE + " WHERE Schluessel = ?",
                    new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_STROMST_REDUZIERT });
                DataRepository.ExecuteNonQuery(
                    "UPDATE " + TABELLE + " SET [Wert] = 6 WHERE Schluessel = ?",
                    new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_KATALOG_GENERATION });

                DataRepository.ExecuteNonQuery(
                    "CREATE TRIGGER us1_saatprobe BEFORE INSERT ON " + TABELLE + " " +
                    "WHEN NEW.Schluessel = '" + DbWerte.GESETZ_UMLAGE_OFFSHORE + "' " +
                    "BEGIN SELECT RAISE(ABORT, 'Probe US-1: diese Zeile wird abgewiesen'); END");
                try
                {
                    GesetzKatalog.StelleKatalogSicher();
                }
                finally
                {
                    DataRepository.ExecuteNonQuery("DROP TRIGGER IF EXISTS us1_saatprobe");
                }

                Assert.Single(GesetzKatalog.SaatWarnungen);
                string w = GesetzKatalog.SaatWarnungen[0];
                Assert.Contains(DbWerte.GESETZ_UMLAGE_OFFSHORE, w);
                Assert.Contains(DbWerte.GESETZ_KLASSE_UMLAGEN, w);
                Assert.Contains("2026", w);
                Assert.Contains("Probe US-1", w);
            }
        }

        private static void Erwarte(IList<GesetzParameter> zeilen, string schluessel, double wert)
        {
            GesetzParameter p = zeilen.Single(z => z.Schluessel == schluessel);
            Assert.Equal(2026, p.JahrVon);
            Assert.Equal(wert, p.Wert);
            Assert.Equal(DbWerte.GESETZ_EINHEIT_CT_KWH, p.Einheit);
            Assert.True(p.Quelle.Length <= 120,
                        schluessel + ": die Quelle hat " +
                        p.Quelle.Length.ToString(CultureInfo.InvariantCulture) + " Zeichen.");
        }

        /// <summary>Der Aufstieg zur Repowurzel — dasselbe Vorgehen wie in
        /// <c>BerechnungshilfeEinbettungTests.Wurzel</c>.</summary>
        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null &&
                   !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;

            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
