using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>DIE REGIONSLISTE ALS KATALOGLISTE</b> (Anwenderwunsch vom 19.09.2026,
    /// Auftrag KL-4): „Bei der Auswahl der Klimadaten soll das gleiche Schema (Filter,
    /// Sortieren …) verwendet werden. Die Quelle und Bezeichnung der Klimadaten soll
    /// mit angezeigt werden."
    ///
    /// <para><b>Was hier festgehalten wird.</b>
    /// <list type="number">
    ///   <item><description><see cref="KlimaregionStammCtrl.Katalogfilterzeilen"/>
    ///     liefert SIEBEN Spalten je Region, nach Namen sortiert.</description></item>
    ///   <item><description>Der Schreibschutz der Auslieferung steht als
    ///     <c>Geschuetzt</c> UND als Spalte.</description></item>
    ///   <item><description><c>Standorttext</c> nimmt den Ortsnamen aus
    ///     <c>Details</c>, wo einer steht — und die Koordinaten, wo der Text der
    ///     HERKUNFTSVERMERK einer TRY-Quelle ist.</description></item>
    ///   <item><description>Ein Altbestand ohne <c>Quelle</c> bleibt leer; das
    ///     Importdatum sortiert als ISO-Text.</description></item>
    ///   <item><description>Ohne die zwei Spalten aus Schemaschritt 95 bleibt die
    ///     Herkunft leer — die Liste steht trotzdem.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist;
    /// die Kultur ist gepinnt (Hausregel seit iU9-W8) — <c>Standorttext</c> schreibt
    /// seine Koordinaten in der Kultur des Anwenders.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KlimaregionKatalogTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  1 - Die sieben Spalten
        // =====================================================================

        /// <summary>
        /// Jede Zeile trägt die sieben Spalten der Liste — und den Bezeichner als
        /// Schlüssel, an dem Ansicht, Löschen und Schreibschutz hängen.
        /// </summary>
        [Fact]
        public void Jede_Zeile_traegt_die_sieben_Spalten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();
            Assert.NotEmpty(zeilen);

            string[] spalten =
            {
                Katalogfilterprofil.SpBezeichner,
                Katalogfilterprofil.SpQuelle,
                Katalogfilterprofil.SpStandort,
                Katalogfilterprofil.SpLongitude,
                Katalogfilterprofil.SpLatitude,
                Katalogfilterprofil.SpImportdatum,
                Katalogfilterprofil.SpSchreibschutz
            };

            foreach (Katalogfilterzeile z in zeilen)
            {
                Assert.True(z.Id > 0, "Jede Zeile traegt ihren Primaerschluessel.");
                Assert.False(string.IsNullOrWhiteSpace(z.Bezeichner));

                foreach (string s in spalten)
                    Assert.False(string.IsNullOrEmpty(z.Text(s)),
                                 "Spalte " + s + " fehlt bei " + z.Bezeichner);

                // Der Bezeichner steht als Wert UND als Schluessel der Zeile.
                Assert.Equal(z.Bezeichner, z.Text(Katalogfilterprofil.SpBezeichner));
                Assert.Equal(z.Bezeichner, z.Schluessel);
            }
        }

        /// <summary>
        /// <b>Sortiert nach Namen</b> — <c>ORDER BY Name</c>, wie schon
        /// <c>ReadAll</c>. Die Katalogliste sortiert erst um, wenn der Anwender einen
        /// Spaltenkopf anklickt.
        /// </summary>
        [Fact]
        public void Die_Liste_kommt_nach_Namen_sortiert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var namen = KlimaregionStammCtrl.Katalogfilterzeilen()
                                            .Select(z => z.Bezeichner).ToList();

            Assert.Equal(namen.OrderBy(n => n, StringComparer.Ordinal).ToList(), namen);
        }

        /// <summary>
        /// Der Schreibschutz der Auslieferung steht an ZWEI Stellen: als
        /// <c>Geschuetzt</c> (daran hängt das gedimmte Zeichnen und die Löschsperre)
        /// und als Ja/Nein-Spalte (danach lässt sich sortieren).
        /// </summary>
        [Fact]
        public void Der_Schreibschutz_steht_als_Kennzeichen_und_als_Spalte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (Katalogfilterzeile z in KlimaregionStammCtrl.Katalogfilterzeilen())
            {
                string erwartet = z.Geschuetzt
                    ? WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA
                    : WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_NEIN;

                Assert.Equal(erwartet, z.Text(Katalogfilterprofil.SpSchreibschutz));
            }
        }

        // =====================================================================
        //  2 - Standorttext
        // =====================================================================

        /// <summary>
        /// <b>Drei echte <c>Details</c>-Muster</b>, so wie sie in der Testdatenbank
        /// und nach einem TRY-Import stehen:
        /// <list type="bullet">
        ///   <item><description>der PVGIS-Vermerk — ein ORTSNAME, er
        ///     steht;</description></item>
        ///   <item><description>der Herkunftsvermerk einer TRY-DATEI — er beginnt mit
        ///     dem Namen der Quelle, nicht mit einem Ort, also Koordinaten;</description></item>
        ///   <item><description>der Herkunftsvermerk der TRY-REGIONALDATEN —
        ///     ebenso.</description></item>
        /// </list>
        /// </summary>
        [Fact]
        public void Standorttext_nimmt_den_Ort_und_sonst_die_Koordinaten()
        {
            // 1. PVGIS: der geokodierte Ort steht so in Tab_Klimaregion_STAMM.Details.
            Assert.Equal("ERA5 - PVGIS-SARAH3: Bielefeld, Nordrhein-Westfalen, Deutschland",
                KlimaregionStammCtrl.Standorttext(
                    "ERA5 - PVGIS-SARAH3: Bielefeld, Nordrhein-Westfalen, Deutschland",
                    8.531007, 52.0191005));

            // 2. TRY-Datei: der Vermerk beginnt mit dem Namen der Quelle.
            string ausDatei = string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_DETAILS_DATEI,
                "TRY2015_487_Jahr.dat", "18.09.2026",
                WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_LIZENZ, "keine")
                + " · " + WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_STANDORT_ANWENDER;

            Assert.Equal("9,1800° / 48,7700°",
                         KlimaregionStammCtrl.Standorttext(ausDatei, 9.18, 48.77));

            // 3. TRY-Regionaldaten: ebenso.
            string regional = string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_DETAILS_REGIONAL,
                "14", "Stötten", "12", "mittleres Jahr", "2015", "data.zip",
                "18.09.2026", WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_LIZENZ, "keine");

            Assert.Equal("9,8600° / 48,6600°",
                         KlimaregionStammCtrl.Standorttext(regional, 9.86, 48.66));
        }

        /// <summary>
        /// <b>Leere <c>Details</c> sind der Regelfall des Bestands</b> (28 der 32
        /// Regionen der Testdatenbank): Dann stehen die Koordinaten da — mit vier
        /// Nachkommastellen, rund 11 m, in der Kultur des Anwenders.
        /// </summary>
        [Fact]
        public void Ohne_Details_stehen_die_Koordinaten()
        {
            Assert.Equal("13,3951° / 52,5174°",
                         KlimaregionStammCtrl.Standorttext("", 13.3951309, 52.5173885));
            Assert.Equal("-57,6344° / -25,2800°",
                         KlimaregionStammCtrl.Standorttext(null, -57.6343814, -25.2800459));
        }

        // =====================================================================
        //  3 - Quelle und Importdatum (Schemaschritt 95)
        // =====================================================================

        /// <summary>
        /// <b>Der Altbestand bleibt leer.</b> Eine Region, die vor Schemaschritt 95
        /// angelegt wurde, sagt nicht, woher sie kommt — und wird nicht nachdatiert.
        /// Leer heißt in der Liste der Halbgeviertstrich (W6-E-1), nie eine
        /// Behauptung.
        /// </summary>
        [Fact]
        public void Ohne_Quelle_bleibt_die_Spalte_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();

            // Die Testdatenbank fuehrt den Bestand - alle Regionen ohne Quelle.
            Assert.All(zeilen, z => Assert.Equal(ParameterVerwendung.LEER,
                                                 z.Text(Katalogfilterprofil.SpQuelle)));
            Assert.All(zeilen, z => Assert.Equal(ParameterVerwendung.LEER,
                                                 z.Text(Katalogfilterprofil.SpImportdatum)));
        }

        /// <summary>
        /// <b>Der Kern liefert den SCHLÜSSEL, nicht den Anzeigetext</b>
        /// (Drei-Schichten-Regel): Eine Region mit Herkunft trägt <c>PVGIS</c>,
        /// <c>TRY_DATEI</c> oder <c>TRY_REGIONAL</c> in der Spalte; die Übersetzung
        /// macht die Hülle. Das Importdatum steht als ISO-Text und sortiert deshalb
        /// als Zeichenkette in der Reihenfolge des Datums.
        /// </summary>
        [Fact]
        public void Eine_Region_mit_Herkunft_traegt_Schluessel_und_ISO_Datum()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Klimaregion_STAMM SET Quelle = ?, Importdatum = ? WHERE Name = ?",
                new DbParam("@q", DbWerte.KLIMA_QUELLE_TRY_REGIONAL),
                new DbParam("@d", "2026-09-18"),
                new DbParam("@n", "Berlin"));

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Klimaregion_STAMM SET Quelle = ?, Importdatum = ? WHERE Name = ?",
                new DbParam("@q", DbWerte.KLIMA_QUELLE_PVGIS),
                new DbParam("@d", "2026-01-07"),
                new DbParam("@n", "Sevilla"));

            var zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();

            Katalogfilterzeile berlin = zeilen.Single(z => z.Bezeichner == "Berlin");
            Assert.Equal(DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                         berlin.Text(Katalogfilterprofil.SpQuelle));
            Assert.Equal("2026-09-18", berlin.Text(Katalogfilterprofil.SpImportdatum));

            Katalogfilterzeile sevilla = zeilen.Single(z => z.Bezeichner == "Sevilla");
            Assert.Equal(DbWerte.KLIMA_QUELLE_PVGIS, sevilla.Text(Katalogfilterprofil.SpQuelle));

            // ISO sortiert als ZEICHENKETTE in der Reihenfolge des Datums.
            Assert.True(string.CompareOrdinal(sevilla.Text(Katalogfilterprofil.SpImportdatum),
                                              berlin.Text(Katalogfilterprofil.SpImportdatum)) < 0);
        }

        /// <summary>
        /// <b>Ohne die zwei Spalten steht die Liste trotzdem</b> (Muster
        /// <c>KostenVorlagenCtrl.PflichtSpalteVorhanden</c>): Eine nie migrierte
        /// Datenbank soll die Maske nicht mit „no such column" zu Fall bringen —
        /// Quelle und Importdatum bleiben dann leer, alles andere steht.
        /// </summary>
        [Fact]
        public void Ohne_die_zwei_Spalten_bleibt_die_Herkunft_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "ALTER TABLE Tab_Klimaregion_STAMM DROP COLUMN " + SchemaKatalog.SPALTE_KR_QUELLE);
            DataRepository.ExecuteNonQuery(
                "ALTER TABLE Tab_Klimaregion_STAMM DROP COLUMN " + SchemaKatalog.SPALTE_KR_IMPORTDATUM);

            Assert.False(DataRepository.SpalteVorhanden(
                KlimaregionStammCtrl.TAB_REGION_STAMM, SchemaKatalog.SPALTE_KR_QUELLE));

            var zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();

            Assert.NotEmpty(zeilen);
            Assert.All(zeilen, z => Assert.Equal(ParameterVerwendung.LEER,
                                                 z.Text(Katalogfilterprofil.SpQuelle)));
            Assert.All(zeilen, z => Assert.Equal(ParameterVerwendung.LEER,
                                                 z.Text(Katalogfilterprofil.SpImportdatum)));

            // Bezeichner und Koordinaten stehen unveraendert.
            Assert.All(zeilen, z => Assert.False(string.IsNullOrEmpty(
                z.Text(Katalogfilterprofil.SpStandort))));
        }
    }
}
