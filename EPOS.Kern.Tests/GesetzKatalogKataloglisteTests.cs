using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zeilenlieferung der Katalogliste für die gesetzlichen Parameter</b>
    /// (Anwenderentscheid MN-1 vom 19.09.2026: „Bei der Auswahl der ‚Gesetzlichen
    /// Parameter' soll das gleiche Schema (Filter, Sortieren …) verwendet werden.").
    ///
    /// <para><b>Was hier Gewicht hat.</b> (1) Der <c>Schluessel</c> einer Zeile ist
    /// ihre ID als Text und nicht der Gesetzesschlüssel — der kommt je
    /// Gültigkeitsjahr mehrfach vor, und zwei Zeilen mit demselben Schlüssel wären
    /// für die Katalogliste EINE Wahl. (2) Die Wertspalte trägt Text UND Zahl: Der
    /// Text ist die gewohnte Formatierung (leer = der Satz ist entfallen), die Zahl
    /// daneben lässt die Spalte als ZAHL sortieren und filtern. (3) Die sechs
    /// Spaltenschlüssel sind dieselben, unter denen die Maske ihr Profil baut.</para>
    ///
    /// <para><b>Warum die Fälle die Datenbank brauchen.</b> Der Katalog wird
    /// gelesen; ohne Arbeitskopie der Testdatenbank schweigen sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GesetzKatalogKataloglisteTests
    {
        /// <summary>Die Klasse mit den meisten Zeilen — der aussagekräftigste Fall.</summary>
        private static string Klasse(GesetzKatalog katalog)
        {
            string beste = "";
            int meiste = 0;
            foreach (string k in katalog.Klassen())
            {
                int n = katalog.Katalogfilterzeilen(k).Count;
                if (n > meiste) { meiste = n; beste = k; }
            }
            return beste;
        }

        [Fact]
        public void Jede_Zeile_traegt_die_sechs_Spalten_und_ihre_Id_als_Schluessel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GesetzKatalog.StelleKatalogSicher();
            var katalog = new GesetzKatalog();

            string klasse = Klasse(katalog);
            Assert.NotEqual("", klasse);

            IReadOnlyList<Katalogfilterzeile> zeilen = katalog.Katalogfilterzeilen(klasse);
            Assert.NotEmpty(zeilen);

            foreach (Katalogfilterzeile z in zeilen)
            {
                // Der WAHLschluessel ist die Id - sonst waeren zwei Jahreszeilen
                // desselben Satzes fuer die Liste EINE Wahl.
                Assert.Equal(z.Id.ToString(CultureInfo.InvariantCulture), z.Schluessel);

                // Der Bezeichner ist der GESETZESschluessel; er steht auch in der
                // ersten Spalte und ist damit der Suchraum.
                Assert.NotEqual("", z.Bezeichner);
                Assert.Equal(z.Bezeichner, z.Text(GesetzKatalog.SpSchluessel));

                // Das Jahr steht als ZAHL da, sonst sortierte die Spalte als Text.
                Assert.NotNull(z.Zahl(GesetzKatalog.SpJahrVon));
            }

            // Und die Schluessel sind eindeutig - der eindeutige Index des
            // Schemaschritts 87 steht ueber (Schluessel, Klasse, JahrVon).
            var schluessel = zeilen.Select(z => z.Schluessel).ToList();
            Assert.Equal(schluessel.Count, schluessel.Distinct(StringComparer.Ordinal).Count());
        }

        /// <summary>
        /// Dieselben Zeilen wie <c>Zeilen(klasse)</c>, nur anders verpackt: gleiche
        /// Anzahl, gleiche Reihenfolge, gleiche Ids, gleicher Werttext. Die eine
        /// Lieferung darf nicht anders rechnen als die andere.
        /// </summary>
        [Fact]
        public void Sie_liefert_dieselben_Zeilen_wie_die_Anzeigezeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GesetzKatalog.StelleKatalogSicher();
            var katalog = new GesetzKatalog();
            string klasse = Klasse(katalog);

            IReadOnlyList<GesetzZeile> alt = katalog.Zeilen(klasse);
            IReadOnlyList<Katalogfilterzeile> neu = katalog.Katalogfilterzeilen(klasse);

            Assert.Equal(alt.Count, neu.Count);
            for (int i = 0; i < alt.Count; i++)
            {
                Assert.Equal(alt[i].Id, neu[i].Id);
                Assert.Equal(alt[i].Schluessel, neu[i].Bezeichner);
                Assert.Equal(alt[i].JahrVon, (int)neu[i].Zahl(GesetzKatalog.SpJahrVon).Value);
                Assert.Equal(alt[i].WertText, neu[i].Text(GesetzKatalog.SpWert));
                Assert.Equal(alt[i].Einheit, neu[i].Text(GesetzKatalog.SpEinheit));
                Assert.Equal(alt[i].Status, neu[i].Text(GesetzKatalog.SpStatus));
                Assert.Equal(alt[i].Quelle, neu[i].Text(GesetzKatalog.SpQuelle));
            }
        }

        /// <summary>
        /// Eine unbekannte Klasse liefert eine leere Liste, keine Ausnahme — die
        /// Maske ruft sie, solange noch keine Klasse gewählt ist.
        /// </summary>
        [Fact]
        public void Eine_unbekannte_Klasse_liefert_eine_leere_Liste()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GesetzKatalog.StelleKatalogSicher();
            Assert.Empty(new GesetzKatalog().Katalogfilterzeilen("GIBTESNICHT"));
        }
    }
}
