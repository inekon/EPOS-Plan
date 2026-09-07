using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Spaltenfilter der BEDARFSKATALOGE</b> (Anwenderentscheid
    /// <b>W14a-E-10</b> vom 07.09.2026, Stufe <b>S3.1</b> des
    /// <c>Konzept_Katalogfilter_EPOS-Plan.md</c>, Abschnitte 2.9 und 4.9) —
    /// Brauchwasser (16), Prozesswaerme (32) und Stromverbraucher (41).
    ///
    /// <para><b>Zweierlei wird geprueft</b>, wie in
    /// <see cref="KatalogspaltenfilterTests"/>: die Ausprägung ohne Datenbank (fuenf
    /// Spalten, dieselben fuer alle drei Kataloge) und die gemessenen Zeilen- und
    /// Trefferzahlen gegen <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>.</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN.</b> Aendert sich eine Zahl, ist
    /// das keine Testschwaeche, sondern eine Verhaltensaenderung.</para>
    ///
    /// <para>Kultur gepinnt (Hausregel seit iU9-W8) — die Zahlenausdruecke
    /// <c>&gt;=1</c> und <c>0,1..1</c> und die Anzeigetexte haengen daran.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogfilterBedarfTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        private readonly CultureInfo _vorher = CultureInfo.DefaultThreadCurrentCulture;

        public KatalogfilterBedarfTests(TestDatenbank db)
        {
            _db = db;

            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _vorher;
            CultureInfo.DefaultThreadCurrentUICulture = _vorher;
        }

        // =================================================================
        //  1 - Die Auspraegung, ohne Datenbank
        // =================================================================

        /// <summary>
        /// <b>Die drei Bedarfskataloge sind Drillinge</b> (Konzept 2.9): dieselbe
        /// Tabellenform, deshalb DASSELBE Profil — fuenf Spalten in derselben
        /// Reihenfolge. Bis zur Stufe S3.1 zeigten Prozess und Brauchwasser zwei
        /// Spalten und der Stromverbraucher eine; der Unterschied war Bestand.
        /// </summary>
        [Theory]
        [InlineData(BedarfsArt.Brauchwasser)]
        [InlineData(BedarfsArt.Prozesswaerme)]
        [InlineData(BedarfsArt.Stromverbraucher)]
        public void Alle_drei_Bedarfskataloge_tragen_dieselben_fuenf_Spalten(BedarfsArt art)
        {
            Katalogfilterprofil p = Katalogfilterprofil.FuerBedarf(art);

            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpBezeichner,
                Katalogfilterprofil.SpTyp,
                Katalogfilterprofil.SpJahressummeMwh,
                Katalogfilterprofil.SpBeschreibung,
                Katalogfilterprofil.SpAuslieferung
            }, p.Spalten.Select(s => s.Schluessel).ToArray());

            // Die Jahressumme ist eine ZAHL und traegt ihre Einheit im Kopf.
            Katalogspalte summe = p.Spalte(Katalogfilterprofil.SpJahressummeMwh);
            Assert.Equal(Katalogspaltenart.Zahl, summe.Art);
            Assert.Equal("MWh", summe.Einheit);
            Assert.True(summe.Rechtsbuendig);

            // Die Auslieferung ist ein KENNZEICHEN: Sortierpfeil ja, Trichter nein
            // (5.6.2) - "nur eigene Saetze" ist eine Spalte, kein Schalter.
            Katalogspalte aus = p.Spalte(Katalogfilterprofil.SpAuslieferung);
            Assert.Equal(Katalogspaltenart.JaNein, aus.Art);
            Assert.True(aus.Sortierbar);
            Assert.False(aus.Filterbar);
        }

        /// <summary>
        /// <b>Jeder Bedarfskatalog fuehrt seinen EIGENEN Filterstand</b> — der
        /// Schluessel unterscheidet die drei, und keiner faellt mit einem der acht
        /// Anlagenkataloge zusammen (Stufe S3.1 auf S2.5).
        /// </summary>
        [Fact]
        public void Jeder_Bedarfskatalog_hat_seinen_eigenen_Schluessel()
        {
            var schluessel = new List<string>();
            foreach (BedarfsArt a in new[] { BedarfsArt.Brauchwasser, BedarfsArt.Prozesswaerme,
                                             BedarfsArt.Stromverbraucher })
                schluessel.Add(Katalogfilterprofil.FuerBedarf(a).Schluessel);

            Assert.Equal(3, schluessel.Distinct().Count());

            foreach (Anlagenart art in Katalogfilterprofil.AlleArten)
                Assert.DoesNotContain(Katalogfilterprofil.Finde(art).Schluessel, schluessel);

            // Und der Stand ist je Schluessel EINE Instanz - dieselbe fuer Verwaltung
            // und Projektdialog (Q2).
            Katalogfilterregister.Leeren();
            Katalogfilterstand a1 = Katalogfilterregister.Stand(schluessel[0]);
            Assert.Same(a1, Katalogfilterregister.Stand(schluessel[0]));
            Assert.NotSame(a1, Katalogfilterregister.Stand(schluessel[1]));
            Katalogfilterregister.Leeren();
        }

        /// <summary>
        /// <b>Die Spalte „im Projekt verwendet" haengt am PROFIL, nicht an der
        /// Anlagenart</b> (S3.1): Auch ein Bedarfskatalog hat einen Projektdialog.
        /// </summary>
        [Fact]
        public void Das_Bedarfsprofil_traegt_die_Verwendungsspalte_hinten()
        {
            Katalogfilterprofil grund = Katalogfilterprofil.FuerBedarf(BedarfsArt.Brauchwasser);
            Katalogfilterprofil mit = grund.MitVerwendungsspalte();

            Assert.Equal(grund.Spalten.Count + 1, mit.Spalten.Count);
            Assert.Equal(Katalogfilterprofil.SpVerwendet,
                         mit.Spalten[mit.Spalten.Count - 1].Schluessel);
            Assert.Equal(grund.Schluessel, mit.Schluessel);
        }

        // =================================================================
        //  2 - Die gemessenen Zahlen (Kenndaten_Test.sqlite)
        // =================================================================

        /// <summary>
        /// Die Zeilenzahlen der drei Kataloge, gemessen am 07.09.2026 — dieselben
        /// Zahlen wie im Befund 1.2 des Konzepts.
        /// </summary>
        [Theory]
        [InlineData(BedarfsArt.Brauchwasser, 16)]
        [InlineData(BedarfsArt.Prozesswaerme, 32)]
        [InlineData(BedarfsArt.Stromverbraucher, 41)]
        public void Die_Zeilenzahl_je_Bedarfskatalog(BedarfsArt art, int erwartet)
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = BedarfStammCtrl.Katalogfilterzeilen(art);
            Assert.Equal(erwartet, zeilen.Count);

            // Die Reihenfolge ist die des Vorlaeufers: ORDER BY Bezeichner.
            Assert.Equal(BedarfStammCtrl.Bezeichner(art).OrderBy(n => n, StringComparer.Ordinal).Count(),
                         zeilen.Count);
        }

        /// <summary>
        /// <b>Die Jahressumme der Liste ist DIESELBE Zahl wie
        /// <see cref="BedarfStammCtrl.Jahressumme"/></b> — die Summe der zwoelf
        /// Monatswerte, ohne Faktor 1000 (Einheitenregel W8-O-5c): Die Monatswerte
        /// stehen in MWh, und genau so nennt sie das Einheitenkuerzel der Verwaltung.
        /// </summary>
        [Fact]
        public void Die_Jahressumme_der_Liste_ist_die_Summe_der_zwoelf_Monatswerte()
        {
            if (!_db.Vorhanden) return;

            foreach (BedarfsArt art in new[] { BedarfsArt.Brauchwasser, BedarfsArt.Prozesswaerme,
                                               BedarfsArt.Stromverbraucher })
                foreach (Katalogfilterzeile z in BedarfStammCtrl.Katalogfilterzeilen(art))
                {
                    double? gezeigt = z.Zahl(Katalogfilterprofil.SpJahressummeMwh);
                    Assert.NotNull(gezeigt);
                    Assert.Equal(BedarfStammCtrl.Jahressumme(art, z.Bezeichner),
                                 gezeigt.Value, 9);
                }
        }

        /// <summary>
        /// <b>Der Schreibschutz steht als SPALTE da</b> (4.9: „nur eigene Sätze" ist im
        /// Spaltenmodell eine Kennzeichenspalte). Gemessen: 6 der 16
        /// Brauchwassersaetze gehoeren zur Auslieferung, bei den beiden anderen
        /// Katalogen keiner.
        /// </summary>
        [Theory]
        [InlineData(BedarfsArt.Brauchwasser, 6)]
        [InlineData(BedarfsArt.Prozesswaerme, 0)]
        [InlineData(BedarfsArt.Stromverbraucher, 0)]
        public void Die_Auslieferungssaetze_je_Bedarfskatalog(BedarfsArt art, int erwartet)
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = BedarfStammCtrl.Katalogfilterzeilen(art);
            Assert.Equal(erwartet, zeilen.Count(z => z.Geschuetzt));

            // Das Kennzeichen der SPALTE sagt dasselbe wie z.Geschuetzt.
            string ja = WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA;
            Assert.Equal(erwartet,
                         zeilen.Count(z => z.Text(Katalogfilterprofil.SpAuslieferung) == ja));
        }

        /// <summary>
        /// <b>Die Beschreibung ist der eigentliche Suchraum</b> (Konzept 2.9): Die
        /// VDI-6002-Saetze tragen ihren Kennwert im Text. Gemessen am 07.09.2026:
        /// „VDI" trifft <b>7 von 16</b> Brauchwassersaetzen, „l/d" ebenfalls 7 —
        /// und ohne die Beschreibung als Spalte faende man keinen davon.
        /// </summary>
        [Fact]
        public void Die_Beschreibung_traegt_den_Kennwert_der_VDI_6002_Saetze()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.FuerBedarf(BedarfsArt.Brauchwasser);
            IReadOnlyList<Katalogfilterzeile> zeilen =
                BedarfStammCtrl.Katalogfilterzeilen(BedarfsArt.Brauchwasser);

            Assert.Equal(7, Treffer(profil, zeilen, Katalogfilterprofil.SpBeschreibung, "VDI"));
            Assert.Equal(7, Treffer(profil, zeilen, Katalogfilterprofil.SpBeschreibung, "l/d"));

            // Und die Zelle traegt EINE Zeile - kein Umbruch, keine Doppelleerzeichen.
            foreach (Katalogfilterzeile z in zeilen)
            {
                string t = z.Text(Katalogfilterprofil.SpBeschreibung);
                Assert.DoesNotContain("\n", t);
                Assert.DoesNotContain("  ", t);
            }
        }

        /// <summary>
        /// <b>Die Jahressumme laesst sich als ZAHL eingrenzen</b> (Frage Q1). Gemessen
        /// am 07.09.2026 gegen die Testdatenbank: <c>&gt;1</c> trifft 7 von 16
        /// Brauchwasser-, 31 von 32 Prozess- und 39 von 41 Stromsaetzen (der eine
        /// Prozesssatz mit genau 1,000 MWh faellt aus dem STRENGEN Vergleich heraus).
        /// </summary>
        [Theory]
        [InlineData(BedarfsArt.Brauchwasser, 7)]
        [InlineData(BedarfsArt.Prozesswaerme, 31)]
        [InlineData(BedarfsArt.Stromverbraucher, 39)]
        public void Die_Jahressumme_laesst_sich_eingrenzen(BedarfsArt art, int erwartet)
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.FuerBedarf(art);
            IReadOnlyList<Katalogfilterzeile> zeilen = BedarfStammCtrl.Katalogfilterzeilen(art);

            Assert.Equal(erwartet,
                         Treffer(profil, zeilen, Katalogfilterprofil.SpJahressummeMwh, ">1"));
        }

        /// <summary>
        /// <b>Die Vergleichszeilen eines Bedarfssatzes</b> (Stufe S3.3): Typ,
        /// Beschreibung, Jahressumme und die zwoelf Monatswerte — fuenfzehn Zeilen.
        /// <para>Der Monatsgang ist der Grund, warum zwei Profile mit derselben
        /// Jahressumme verschieden rechnen; ohne ihn saehe ein Vergleich das nicht.</para>
        /// </summary>
        [Fact]
        public void Die_Vergleichszeilen_tragen_Kopf_und_zwoelf_Monatswerte()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen =
                BedarfStammCtrl.Katalogfilterzeilen(BedarfsArt.Brauchwasser);
            Assert.NotEmpty(zeilen);

            IReadOnlyList<Parameterwert> werte =
                BedarfStammCtrl.Vergleichszeilen(BedarfsArt.Brauchwasser, zeilen[0].Bezeichner);

            Assert.Equal(15, werte.Count);
            Assert.Equal("Typ", werte[0].Eintrag.Spalte);
            Assert.Equal("Beschreibung", werte[1].Eintrag.Spalte);
            Assert.Equal("Jahressumme", werte[2].Eintrag.Spalte);
            for (int i = 0; i < 12; i++)
                Assert.Equal("Monat_" + (i + 1), werte[3 + i].Eintrag.Spalte);

            // Alle fuenfzehn sind SIMULATIONSwerte - der Rechenweg liest sie.
            Assert.All(werte, w => Assert.True(w.Eintrag.Gerechnet));

            // Ein Satz, den es nicht gibt, liefert dieselbe Liste mit Leerwerten.
            IReadOnlyList<Parameterwert> leer =
                BedarfStammCtrl.Vergleichszeilen(BedarfsArt.Brauchwasser, "gibt es nicht");
            Assert.Equal(15, leer.Count);
            Assert.All(leer, w => Assert.Equal(ParameterVerwendung.LEER, w.Wert));
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        private static int Treffer(Katalogfilterprofil profil,
                                   IReadOnlyList<Katalogfilterzeile> zeilen,
                                   string spalte, string ausdruck)
        {
            var stand = new Katalogfilterstand();
            stand.Setzen(spalte, ausdruck);
            return Katalogfilter.Anwenden(profil, zeilen, stand).Count;
        }
    }
}
