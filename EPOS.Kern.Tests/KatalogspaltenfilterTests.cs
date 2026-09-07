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
    /// <b>Der SPALTENFILTER der Katalogverwaltung</b> (Anwenderentscheid
    /// <b>W14a-E-10</b> vom 07.09.2026, Stufe S1 des
    /// <c>Konzept_Katalogfilter_EPOS-Plan.md</c>) — <see cref="Katalogfilter"/>,
    /// <see cref="Katalogfilterprofil"/> und die acht
    /// <c>…StammCtrl.Katalogfilterzeilen()</c>.
    ///
    /// <para><b>Zweierlei wird geprueft.</b> Erstens die RECHNUNG ohne Datenbank: die
    /// Verknuepfung (Spaltenfilter UND, Suche ODER ueber Spalten und UND ueber
    /// Begriffe), die Platzhalter, der Sortierzyklus auf → ab → aus. Zweitens die
    /// gemessenen TREFFERZAHLEN gegen <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> —
    /// dieselben Zahlen, die das Mockup nennt (Anhang A des Konzepts).</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN</b>, wie in
    /// <see cref="KatalogVerwaltungTests"/>: Aendert sich eine Zahl, ist das keine
    /// Testschwaeche, sondern eine Verhaltensaenderung des Filters.</para>
    ///
    /// <para>Kultur gepinnt (Hausregel seit iU9-W8) — der Ausdruck <c>&gt;=0,95</c> und
    /// die Anzeigetexte haengen daran.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogspaltenfilterTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        private readonly CultureInfo _vorher = CultureInfo.DefaultThreadCurrentCulture;

        public KatalogspaltenfilterTests(TestDatenbank db)
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
        //  1 - Die Rechnung, ohne Datenbank
        // =================================================================

        private static Katalogfilterprofil Probeprofil()
        {
            return Katalogfilterprofil.Finde(Anlagenart.Heizkessel);
        }

        private static List<Katalogfilterzeile> Probezeilen()
        {
            return new List<Katalogfilterzeile>
            {
                new Katalogfilterzeile(1, "Alpha")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Alpha")
                    .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
                    .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
                    .MitZahl(Katalogfilterprofil.SpPtherm, 15.0)
                    .MitZahl(Katalogfilterprofil.SpEta, 0.97, 3)
                    .MitKennzeichen(Katalogfilterprofil.SpBrennwert, true),

                new Katalogfilterzeile(2, "Beta")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Beta")
                    .MitText(Katalogfilterprofil.SpHersteller, "Buderus")
                    .MitText(Katalogfilterprofil.SpBrennstoff, "Heizöl EL")
                    .MitZahl(Katalogfilterprofil.SpPtherm, 80.0)
                    .MitZahl(Katalogfilterprofil.SpEta, 0.90, 3)
                    .MitKennzeichen(Katalogfilterprofil.SpBrennwert, false),

                new Katalogfilterzeile(3, "Gamma")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Gamma")
                    .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
                    .MitText(Katalogfilterprofil.SpBrennstoff, "Stadtgas")
                    .MitZahl(Katalogfilterprofil.SpPtherm, 40.0)
                    .MitZahl(Katalogfilterprofil.SpEta, null, 3)
                    .MitKennzeichen(Katalogfilterprofil.SpBrennwert, false)
            };
        }

        /// <summary>Ohne Stand steht die ganze Liste da — die Vorbelegung zeigt ALLES (3.3).</summary>
        [Fact]
        public void Ohne_Filter_steht_die_ganze_Liste()
        {
            var stand = new Katalogfilterstand();
            Assert.Equal(3, Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand).Count);
            Assert.False(stand.Gesetzt);
        }

        /// <summary>
        /// <b>Zwei Spaltenfilter wirken UND</b> (5.6.3): „enthaelt Gas" trifft Erdgas UND
        /// Stadtgas, der Zahlenausdruck <c>10..50</c> davon nur einen.
        /// </summary>
        [Fact]
        public void Spaltenfilter_wirken_UND()
        {
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpBrennstoff, "Gas");
            Assert.Equal(2, Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand).Count);

            stand.Setzen(Katalogfilterprofil.SpPtherm, "10..50");
            var treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Equal(2, treffer.Count);          // Alpha 15 und Gamma 40

            stand.Setzen(Katalogfilterprofil.SpPtherm, "10..20");
            treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Single(treffer);
            Assert.Equal("Alpha", treffer[0].Bezeichner);

            Assert.True(stand.Gesetzt);
            Assert.Equal(2, stand.Anzahl);
        }

        /// <summary>
        /// <b>Gross/klein egal — auch bei Umlauten</b> (5.6.3, Regel aus
        /// <c>VdiAuswahlFilter.Passt:70</c> mit <c>CurrentCultureIgnoreCase</c>): „öl"
        /// findet „Heizöl EL".
        /// </summary>
        [Fact]
        public void Textfilter_ist_gross_klein_unabhaengig_auch_bei_Umlauten()
        {
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpBrennstoff, "öl");

            var treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Single(treffer);
            Assert.Equal("Beta", treffer[0].Bezeichner);
        }

        /// <summary>
        /// <b>Ein unverstandener Zahlenausdruck ist KEIN Filter</b> — die Liste bleibt
        /// vollstaendig, statt zu leeren (3.3).
        /// </summary>
        [Fact]
        public void Unverstandener_Zahlenausdruck_ist_kein_Filter()
        {
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpPtherm, "10..");
            Assert.Equal(3, Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand).Count);
        }

        /// <summary>
        /// <b>Ein Leerwert trifft keinen Zahlenvergleich</b>: „Gamma" hat kein η und
        /// faellt aus <c>&gt;=0,5</c> heraus — in der Spalte steht dort der
        /// Halbgeviertstrich.
        /// </summary>
        [Fact]
        public void Leerwert_faellt_aus_dem_Zahlenfilter()
        {
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpEta, ">=0,5");

            var treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Equal(2, treffer.Count);
            Assert.DoesNotContain(treffer, z => z.Bezeichner == "Gamma");
            Assert.Equal(ParameterVerwendung.LEER,
                         Probezeilen()[2].Text(Katalogfilterprofil.SpEta));
        }

        /// <summary>
        /// <b>Die Suche wirkt ODER ueber alle Spalten und UND ueber die Begriffe</b>
        /// (5.6.3, Regel von <see cref="VdiAuswahlFilter.Passt"/>): „vaillant gas" trifft
        /// den Satz, dessen HERSTELLER „Vaillant" und dessen BRENNSTOFF „Erdgas" heisst.
        /// </summary>
        [Fact]
        public void Suche_ist_ODER_ueber_Spalten_und_UND_ueber_Begriffe()
        {
            var stand = new Katalogfilterstand { Suche = "vaillant" };
            Assert.Equal(2, Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand).Count);

            stand.Suche = "vaillant erdgas";
            var treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Single(treffer);
            Assert.Equal("Alpha", treffer[0].Bezeichner);

            stand.Suche = "vaillant buderus";     // beides zugleich gibt es nicht
            Assert.Empty(Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand));
        }

        /// <summary>
        /// <b>Die Platzhalter <c>*</c> und <c>?</c> gehen durch
        /// <see cref="Suchmuster"/></b> — MIT Platzhalter verankert, OHNE Teilsuche.
        /// „Al*" trifft „Alpha", „*a" trifft alle drei (Alpha, Beta, Gamma).
        /// </summary>
        [Fact]
        public void Suche_versteht_die_Platzhalter()
        {
            var stand = new Katalogfilterstand { Suche = "Al*" };
            var treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Single(treffer);
            Assert.Equal("Alpha", treffer[0].Bezeichner);

            stand.Suche = "Bet?";
            treffer = Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand);
            Assert.Single(treffer);
            Assert.Equal("Beta", treffer[0].Bezeichner);
        }

        /// <summary>
        /// <b>Der Sortierzyklus: auf → ab → aus</b> (5.6.2), immer hoechstens EINE
        /// Spalte. Der dritte Klick stellt die Reihenfolge des Controllers wieder her.
        /// </summary>
        [Fact]
        public void Sortierzyklus_auf_ab_aus()
        {
            var profil = Probeprofil();
            var stand = new Katalogfilterstand();

            stand.Sortieren(Katalogfilterprofil.SpPtherm);
            Assert.True(stand.Aufsteigend);
            Assert.Equal(new[] { "Alpha", "Gamma", "Beta" },
                         Katalogfilter.Anwenden(profil, Probezeilen(), stand)
                                      .Select(z => z.Bezeichner).ToArray());

            stand.Sortieren(Katalogfilterprofil.SpPtherm);
            Assert.False(stand.Aufsteigend);
            Assert.Equal(new[] { "Beta", "Gamma", "Alpha" },
                         Katalogfilter.Anwenden(profil, Probezeilen(), stand)
                                      .Select(z => z.Bezeichner).ToArray());

            stand.Sortieren(Katalogfilterprofil.SpPtherm);
            Assert.Equal("", stand.Sortierspalte);
            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" },
                         Katalogfilter.Anwenden(profil, Probezeilen(), stand)
                                      .Select(z => z.Bezeichner).ToArray());

            // Eine ANDERE Spalte beginnt wieder mit "auf".
            stand.Sortieren(Katalogfilterprofil.SpPtherm);
            stand.Sortieren(Katalogfilterprofil.SpHersteller);
            Assert.Equal(Katalogfilterprofil.SpHersteller, stand.Sortierspalte);
            Assert.True(stand.Aufsteigend);
        }

        /// <summary>Leerwerte stehen beim Sortieren HINTEN — in beiden Richtungen.</summary>
        [Fact]
        public void Leerwerte_stehen_beim_Sortieren_hinten()
        {
            var profil = Probeprofil();
            var stand = new Katalogfilterstand();

            stand.Sortieren(Katalogfilterprofil.SpEta);
            Assert.Equal("Gamma",
                Katalogfilter.Anwenden(profil, Probezeilen(), stand).Last().Bezeichner);

            stand.Sortieren(Katalogfilterprofil.SpEta);   // absteigend
            Assert.Equal("Gamma",
                Katalogfilter.Anwenden(profil, Probezeilen(), stand).Last().Bezeichner);
        }

        /// <summary>
        /// <b>„Filter zuruecksetzen" nimmt die SPALTENfilter zurueck, nicht die Suche</b>
        /// (5.6.4: „Er ist kein Filter, sondern ein Ruecksetzer"), und er ist genau dann
        /// zu sehen, wenn mindestens ein Spaltenfilter gesetzt ist.
        /// </summary>
        [Fact]
        public void Zuruecksetzen_nimmt_die_Spaltenfilter_und_laesst_die_Suche()
        {
            var stand = new Katalogfilterstand { Suche = "vaillant" };
            Assert.False(stand.Gesetzt);                 // Suche allein zeigt den Knopf nicht

            stand.Setzen(Katalogfilterprofil.SpBrennstoff, "Gas");
            Assert.True(stand.Gesetzt);

            stand.Zuruecksetzen();
            Assert.False(stand.Gesetzt);
            Assert.Equal("vaillant", stand.Suche);
            Assert.Equal(2, Katalogfilter.Anwenden(Probeprofil(), Probezeilen(), stand).Count);
        }

        /// <summary>Ein leerer Ausdruck nimmt den Filter derselben Spalte zurueck.</summary>
        [Fact]
        public void Leerer_Ausdruck_nimmt_den_Filter_zurueck()
        {
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpBrennstoff, "Gas");
            Assert.True(stand.Gefiltert(Katalogfilterprofil.SpBrennstoff));

            stand.Setzen(Katalogfilterprofil.SpBrennstoff, "   ");
            Assert.False(stand.Gefiltert(Katalogfilterprofil.SpBrennstoff));
        }

        // =================================================================
        //  2 - Die acht Profile
        // =================================================================

        /// <summary>
        /// Jede der acht Anlagenarten hat ein Profil mit FUENF bis NEUN Spalten
        /// (5.6.5), lauter verschiedenen Schluesseln, und die erste Spalte ist immer
        /// eine Textspalte.
        /// </summary>
        [Fact]
        public void Acht_Profile_mit_fuenf_bis_neun_Spalten()
        {
            int arten = 0;
            foreach (Anlagenart art in Katalogfilterprofil.AlleArten)
            {
                arten++;
                Katalogfilterprofil profil = Katalogfilterprofil.Finde(art);

                Assert.InRange(profil.Spalten.Count, 5, 9);
                Assert.Equal(profil.Spalten.Count,
                             profil.Spalten.Select(s => s.Schluessel).Distinct().Count());
                Assert.Equal(Katalogspaltenart.Text, profil.Spalten[0].Art);

                foreach (Katalogspalte s in profil.Spalten)
                    Assert.False(string.IsNullOrWhiteSpace(s.Titel));
            }
            Assert.Equal(8, arten);
        }

        /// <summary>
        /// <b>Eine Kennzeichenspalte traegt NUR den Sortierpfeil</b> (5.6.2): Brennwert
        /// beim Heizkessel, Kuehlen bei der Waermepumpe. „Ein Feld ‚enthaelt ja' fuer
        /// zwei Werte ist ein Bedienelement ohne Gewinn."
        /// </summary>
        [Fact]
        public void Kennzeichenspalten_tragen_keinen_Trichter()
        {
            Katalogspalte brennwert =
                Katalogfilterprofil.Finde(Anlagenart.Heizkessel)
                                   .Spalte(Katalogfilterprofil.SpBrennwert);
            Assert.Equal(Katalogspaltenart.JaNein, brennwert.Art);
            Assert.True(brennwert.Sortierbar);
            Assert.False(brennwert.Filterbar);

            Katalogspalte kuehlen =
                Katalogfilterprofil.Finde(Anlagenart.Waermepumpe)
                                   .Spalte(Katalogfilterprofil.SpKuehlen);
            Assert.Equal(Katalogspaltenart.JaNein, kuehlen.Art);
            Assert.False(kuehlen.Filterbar);
        }

        /// <summary>Zahlenspalten stehen rechtsbuendig und tragen ihre Einheit im Kopf.</summary>
        [Fact]
        public void Zahlenspalten_stehen_rechts_und_nennen_die_Einheit()
        {
            Katalogspalte pth = Katalogfilterprofil.Finde(Anlagenart.Heizkessel)
                                                   .Spalte(Katalogfilterprofil.SpPtherm);
            Assert.Equal(Katalogspaltenart.Zahl, pth.Art);
            Assert.True(pth.Rechtsbuendig);
            Assert.Equal("kW", pth.Einheit);
            Assert.Contains("[kW]", pth.Kopftext);

            Katalogspalte name = Katalogfilterprofil.Finde(Anlagenart.Heizkessel)
                                                    .Spalte(Katalogfilterprofil.SpBezeichner);
            Assert.False(name.Rechtsbuendig);
            Assert.DoesNotContain("[", name.Kopftext);
        }

        /// <summary>
        /// Der Uebersetzer wird wirklich benutzt — ohne ihn stehen die SCHLUESSEL da
        /// (dasselbe Vorgehen wie <see cref="KatalogBrowserProfil.Finde"/>).
        /// </summary>
        [Fact]
        public void Die_Beschriftungen_kommen_von_aussen()
        {
            Katalogfilterprofil roh = Katalogfilterprofil.Finde(Anlagenart.Heizkessel);
            Assert.Equal("KFLT_SP_BEZEICHNER", roh.Spalten[0].Titel);

            Katalogfilterprofil uebersetzt =
                Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => "<" + s + ">");
            Assert.Equal("<KFLT_SP_BEZEICHNER>", uebersetzt.Spalten[0].Titel);
        }

        // =================================================================
        //  3 - Die gemessenen Trefferzahlen (Kenndaten_Test.sqlite)
        // =================================================================

        /// <summary>
        /// <b>Heizkessel — der Filterstand des Mockups M1</b>: Brennstoff enthaelt „Gas",
        /// P_th <c>10..60</c>, η <c>&gt;=0,95</c>. Gemessen am 07.09.2026:
        /// 52 / 54 / 32 einzeln, <b>15 von 63</b> zusammen; laesst man je einen weg,
        /// bleiben 25 / 22 / 43 (Anhang A des Konzepts).
        /// </summary>
        [Fact]
        public void Heizkessel_Trefferzahlen_des_Mockups()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Heizkessel);
            var zeilen = new HeizkesselStammCtrl().Katalogfilterzeilen();
            Assert.Equal(63, zeilen.Count);

            Assert.Equal(52, Treffer(profil, zeilen, (Katalogfilterprofil.SpBrennstoff, "Gas")));
            Assert.Equal(54, Treffer(profil, zeilen, (Katalogfilterprofil.SpPtherm, "10..60")));
            Assert.Equal(32, Treffer(profil, zeilen, (Katalogfilterprofil.SpEta, ">=0,95")));

            Assert.Equal(15, Treffer(profil, zeilen,
                (Katalogfilterprofil.SpBrennstoff, "Gas"),
                (Katalogfilterprofil.SpPtherm, "10..60"),
                (Katalogfilterprofil.SpEta, ">=0,95")));

            Assert.Equal(25, Treffer(profil, zeilen,
                (Katalogfilterprofil.SpPtherm, "10..60"),
                (Katalogfilterprofil.SpEta, ">=0,95")));
            Assert.Equal(22, Treffer(profil, zeilen,
                (Katalogfilterprofil.SpBrennstoff, "Gas"),
                (Katalogfilterprofil.SpEta, ">=0,95")));
            Assert.Equal(43, Treffer(profil, zeilen,
                (Katalogfilterprofil.SpBrennstoff, "Gas"),
                (Katalogfilterprofil.SpPtherm, "10..60")));
        }

        /// <summary>
        /// <b>Der Brennstoff kommt als NAME, nicht als Nummer</b> (5.6.3) — genau
        /// deshalb trifft „enthaelt Gas" dieselben 52 Saetze wie bisher die Klappliste
        /// „Brennstoffgruppe" (<c>HeizkesselStammCtrl.Filtern("Gas", 0)</c>).
        /// </summary>
        [Fact]
        public void Heizkessel_Brennstoffspalte_trifft_dieselbe_Menge_wie_die_Klappliste()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            int klappliste = ctrl.Filtern("Gas", 0).Count;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Heizkessel);
            int spalte = Treffer(profil, ctrl.Katalogfilterzeilen(),
                                 (Katalogfilterprofil.SpBrennstoff, "Gas"));

            Assert.Equal(52, klappliste);
            Assert.Equal(klappliste, spalte);
        }

        /// <summary>
        /// <b>Befund D-1 steht als Spalte da.</b> 6 von 63 Saetzen tragen das Kennzeichen
        /// <c>Brennwert</c>, aber 46 Beschreibungen nennen das Wort — die SUCHE ueber
        /// alle Spalten findet die 46 nicht, weil die Beschreibung keine Spalte ist;
        /// genau daran faellt der Datenfehler auf (Frage Q8).
        /// </summary>
        [Fact]
        public void Heizkessel_Brennwertkennzeichen_traegt_sechs_Saetze()
        {
            if (!_db.Vorhanden) return;

            var zeilen = new HeizkesselStammCtrl().Katalogfilterzeilen();
            int ja = zeilen.Count(z => z.Text(Katalogfilterprofil.SpBrennwert) ==
                                       WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA);
            Assert.Equal(6, ja);
        }

        /// <summary>
        /// <b>Waermepumpe — der Filterstand des Mockups M2</b>: Quelle enthaelt „Luft",
        /// P_N <c>5..12</c>, VL max <c>&gt;=60</c>. Gemessen: 34 / 31 / 18 einzeln,
        /// <b>7 von 51</b> zusammen. VL max kommt aus <c>Max(Vorlauf)</c> ueber
        /// <c>Tab_Kenndaten_STAMM</c>.
        /// </summary>
        [Fact]
        public void Waermepumpe_Trefferzahlen_des_Mockups()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Waermepumpe);
            var zeilen = new WPStammCtrl().Katalogfilterzeilen();
            Assert.Equal(51, zeilen.Count);

            Assert.Equal(34, Treffer(profil, zeilen, (Katalogfilterprofil.SpQuelle, "Luft")));
            Assert.Equal(31, Treffer(profil, zeilen, (Katalogfilterprofil.SpNennleistung, "5..12")));
            Assert.Equal(18, Treffer(profil, zeilen, (Katalogfilterprofil.SpVlMax, ">=60")));

            Assert.Equal(7, Treffer(profil, zeilen,
                (Katalogfilterprofil.SpQuelle, "Luft"),
                (Katalogfilterprofil.SpNennleistung, "5..12"),
                (Katalogfilterprofil.SpVlMax, ">=60")));

            // 15 von 51 Saetzen tragen eine Kuehlleistung > 0 (Anhang A).
            Assert.Equal(15, zeilen.Count(z => z.Text(Katalogfilterprofil.SpKuehlen) ==
                                               WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA));
        }

        /// <summary>
        /// <b>Stufe S2.2 — die elf Bedienelemente treffen dieselbe Menge wie die
        /// Spalten.</b> Der <c>WaermepumpenKatalogDialog</c> hatte bis zum
        /// Anwenderentscheid <b>W14a-E-10</b> eine eigene Filterleiste: sieben
        /// Klapplisten und vier Zahlenfelder, bedient von
        /// <see cref="WaermepumpenKatalogFilter"/> ueber
        /// <c>WPStammCtrl.KatalogZeilen()</c>. Dieser Fall haelt den ALTEN Weg
        /// gegen den NEUEN — dieselbe Datenbank, derselbe Filterstand des
        /// Mockups M2, dieselbe Zahl.
        ///
        /// <para>Er ist die GEGENPROBE, um derentwillen der alte Filter stehen
        /// bleibt, obwohl er seit S2.2 keinen Wirt mehr hat: Ohne ihn liesse sich
        /// nicht mehr belegen, dass die Spalten treffen, was die Bedienelemente
        /// getroffen haben.</para>
        /// </summary>
        [Fact]
        public void Waermepumpe_Spalten_treffen_dieselbe_Menge_wie_die_elf_Bedienelemente()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new WPStammCtrl();

            // (a) DER ALTE WEG: elf Bedienelemente, hier drei davon belegt -
            //     Quelle = "Luft-Wasser", P_N 5..12 kW, VL max ab 60 Grad.
            //     Die uebrigen stehen auf "Alle" (null) bzw. auf ihrem Anschlag.
            var alt = WaermepumpenKatalogFilter.Anwenden(
                ctrl.KatalogZeilen(),
                new WaermepumpenKatalogFilter.Kriterien(
                    Funktionsprinzip: "Luft-Wasser",
                    LeistungMin: 5, LeistungMax: 12,
                    VorlaufMin: 60, VorlaufMax: 1000));

            // (b) DER NEUE WEG: dieselben drei Aussagen als Spaltenausdruecke.
            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Waermepumpe);
            int neu = Treffer(profil, ctrl.Katalogfilterzeilen(),
                              (Katalogfilterprofil.SpQuelle, "Luft-Wasser"),
                              (Katalogfilterprofil.SpNennleistung, "5..12"),
                              (Katalogfilterprofil.SpVlMax, ">=60"));

            Assert.Equal(7, alt.Count);
            Assert.Equal(alt.Count, neu);
        }

        /// <summary>
        /// <b>Warum Bauart, Auslegung, Regelung und Aufstellung KEINE Spalte
        /// werden</b> (S2.2) — das ist gemessen, nicht vergessen.
        ///
        /// <para><b>Bauart</b> ist in 45 von 51 Saetzen leer; eine Spalte, die
        /// fast immer leer ist, kostet Breite und traegt nichts. <b>Auslegung</b>
        /// („Heizen" / „Heizen/Kuehlen") ist GERECHNET aus
        /// <c>Kuehlleistung &gt; 0</c> — dieselbe Aussage wie die Spalte „Kuehlen",
        /// und die zwei Mengen sind hier Satz fuer Satz gleich. Regelung und
        /// Aufstellung stehen im Kenndatenblock der Detailansicht.</para>
        /// </summary>
        [Fact]
        public void Waermepumpe_Bauart_ist_fast_leer_und_Auslegung_sagt_dasselbe_wie_Kuehlen()
        {
            if (!_db.Vorhanden) return;

            var alt = new WPStammCtrl().KatalogZeilen();
            Assert.Equal(51, alt.Count);

            // Bauart: 45 von 51 leer.
            Assert.Equal(45, alt.Count(z => string.IsNullOrWhiteSpace(z.Bauart)));

            // Auslegung == Spalte "Kuehlen": dieselbe Menge, Satz fuer Satz.
            var mitKuehlung = alt
                .Where(z => z.Auslegung == WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN_KUEHLEN)
                .Select(z => z.Bezeichnung)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            var spalteJa = new WPStammCtrl().Katalogfilterzeilen()
                .Where(z => z.Text(Katalogfilterprofil.SpKuehlen) ==
                            WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA)
                .Select(z => z.Bezeichner)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(15, mitKuehlung.Count);
            Assert.Equal(mitKuehlung, spalteJa);
        }

        /// <summary>
        /// <b>BHKW</b> — die abgeleitete Stromkennzahl σ = P_el / P_th steht als Spalte
        /// da und laesst sich filtern. Gemessen: 79 Saetze, „Gas" 72, P_el <c>5..50</c>
        /// 30, σ <c>&gt;=0,5</c> 57, Motortyp enthaelt „Otto" 21.
        /// </summary>
        [Fact]
        public void Bhkw_Spalten_und_Trefferzahlen()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Bhkw);
            var zeilen = new BHKWStammCtrl().Katalogfilterzeilen();
            Assert.Equal(79, zeilen.Count);
            Assert.Equal(8, profil.Spalten.Count);

            Assert.Equal(72, Treffer(profil, zeilen, (Katalogfilterprofil.SpBrennstoff, "Gas")));
            Assert.Equal(30, Treffer(profil, zeilen, (Katalogfilterprofil.SpPel, "5..50")));
            Assert.Equal(57, Treffer(profil, zeilen, (Katalogfilterprofil.SpSigma, ">=0,5")));
            Assert.Equal(21, Treffer(profil, zeilen, (Katalogfilterprofil.SpMotortyp, "Otto")));
        }

        /// <summary>
        /// <b>Solarkollektoren</b> — der erste Filter dieses Katalogs ueberhaupt
        /// (bis hierher <c>KatalogFilterArt.Keiner</c>). Gemessen: 7 Saetze, davon
        /// 4 Flachkollektoren und 3 mit einer Apertur ab 2 m².
        /// </summary>
        [Fact]
        public void Solarkollektoren_Spalten_und_Trefferzahlen()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Solarkollektoren);
            var zeilen = SolarkollektorenStammCtrl.Katalogfilterzeilen();
            Assert.Equal(7, zeilen.Count);
            Assert.Equal(6, profil.Spalten.Count);

            Assert.Equal(4, Treffer(profil, zeilen, (Katalogfilterprofil.SpKollektortyp, "Flach")));
            Assert.Equal(3, Treffer(profil, zeilen, (Katalogfilterprofil.SpApertur, ">=2")));
        }

        /// <summary>
        /// <b>Pufferspeicher</b> — der Ausdruck <c>200..500</c> ersetzt die sechs festen
        /// Volumenstufen (Frage Q5). Gemessen: 13 Saetze, davon 5 im Bereich.
        /// </summary>
        [Fact]
        public void Pufferspeicher_Spalten_und_Trefferzahlen()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Pufferspeicher);
            var zeilen = PufferSpStammCtrl.Katalogfilterzeilen();
            Assert.Equal(13, zeilen.Count);
            Assert.Equal(5, profil.Spalten.Count);

            Assert.Equal(5, Treffer(profil, zeilen, (Katalogfilterprofil.SpVolumen, "200..500")));
            Assert.Equal(3, zeilen.Select(z => z.Text(Katalogfilterprofil.SpSpeichertyp))
                                  .Distinct().Count());
        }

        /// <summary>
        /// <b>PV-Module</b> — dieselbe Bedienung wie im Mockup M3, hier gegen die sechs
        /// Saetze der Testdatenbank statt gegen die 20 749 nach dem CEC-Import:
        /// „Ablytek" 3, P_STC <c>260..300</c> 4, η <c>&gt;=17</c> 3. Ein Modul ohne
        /// Kantenmasse traegt in der Flaechenspalte den Halbgeviertstrich (W6-E-1).
        /// </summary>
        [Fact]
        public void Photovoltaik_Spalten_und_Trefferzahlen()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Photovoltaik);
            var zeilen = PhotovoltaikStammCtrl.Katalogfilterzeilen();
            Assert.Equal(6, zeilen.Count);
            Assert.Equal(7, profil.Spalten.Count);

            Assert.Equal(3, Treffer(profil, zeilen, (Katalogfilterprofil.SpHersteller, "Ablytek")));
            Assert.Equal(4, Treffer(profil, zeilen, (Katalogfilterprofil.SpPstc, "260..300")));
            Assert.Equal(3, Treffer(profil, zeilen, (Katalogfilterprofil.SpEta, ">=17")));

            Assert.Equal(1, zeilen.Count(z => z.Text(Katalogfilterprofil.SpModulflaeche) ==
                                              ParameterVerwendung.LEER));
        }

        /// <summary>
        /// <b>Wechselrichter</b> — die Herstellerklappliste faellt zugunsten der Spalte.
        /// Die Testdatenbank fuehrt genau ein Geraet (2,5 kW, η_euro 0,968, 2 MPPT,
        /// Herkunft HAND); die Zahlen stehen als Werte in den Spalten.
        /// </summary>
        [Fact]
        public void Wechselrichter_Spalten_und_Werte()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Wechselrichter);
            var zeilen = WechselrichterStammCtrl.Katalogfilterzeilen();
            Assert.Single(zeilen);
            Assert.Equal(7, profil.Spalten.Count);

            Katalogfilterzeile z = zeilen[0];
            Assert.Equal(2.5, z.Zahl(Katalogfilterprofil.SpPac));
            Assert.Equal(0.968, z.Zahl(Katalogfilterprofil.SpEtaEuro));
            Assert.Equal(2.0, z.Zahl(Katalogfilterprofil.SpMppt));
            Assert.Equal("HAND", z.Text(Katalogfilterprofil.SpHerkunft));

            Assert.Equal(1, Treffer(profil, zeilen, (Katalogfilterprofil.SpPac, "1..5")));
            Assert.Equal(0, Treffer(profil, zeilen, (Katalogfilterprofil.SpPac, ">5")));
        }

        /// <summary>
        /// <b>Stromspeicher</b> — die C-Rate = P / E steht als Spalte da, und der
        /// HERSTELLER kommt aus dem Bezeichnerpraefix (Befund D-3).
        ///
        /// <para><b>Gemessen am 07.09.2026:</b> KEINER der fuenf Saetze der
        /// Testdatenbank traegt einen Doppelpunkt — die fuenf sind von Hand gepflegt und
        /// aelter als der Importweg, der „Hersteller: Modell" schreibt
        /// (<c>StromspeicherImportSatz</c>). Die Herstellerspalte steht dort deshalb auf
        /// dem Halbgeviertstrich; der Name selbst bleibt in der Bezeichnerspalte und wird
        /// von der Suche ueber alle Spalten gefunden. Diese Probe haelt die Quote fest —
        /// sie ist die Begruendung fuer den eigenen Schemaschritt (Frage Q7).</para>
        /// </summary>
        [Fact]
        public void Stromspeicher_Spalten_C_Rate_und_Herstellerquote()
        {
            if (!_db.Vorhanden) return;

            Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Stromspeicher);
            var zeilen = StromspeicherStammCtrl.Katalogfilterzeilen();
            Assert.Equal(5, zeilen.Count);
            Assert.Equal(8, profil.Spalten.Count);

            Assert.Equal(3, Treffer(profil, zeilen, (Katalogfilterprofil.SpChemie, "Lithium-Ionen")));
            Assert.Equal(4, Treffer(profil, zeilen, (Katalogfilterprofil.SpEnergie, "10..13")));

            // C-Rate: BYD HVS+ 12.8 hat 12,8 kW auf 12,8 kWh -> 1,00
            Katalogfilterzeile byd = zeilen.First(z => z.Bezeichner.StartsWith("BYD HVS"));
            Assert.Equal(1.0, byd.Zahl(Katalogfilterprofil.SpCrate));

            // Die HERSTELLERQUOTE des Praefixes: 0 von 5.
            int mitPraefix = zeilen.Count(z => z.Text(Katalogfilterprofil.SpHersteller) !=
                                               ParameterVerwendung.LEER);
            Assert.Equal(0, mitPraefix);
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        /// <summary>Die Trefferzahl zu einem Satz Spaltenausdruecke.</summary>
        private static int Treffer(Katalogfilterprofil profil,
                                   IReadOnlyList<Katalogfilterzeile> zeilen,
                                   params (string Spalte, string Ausdruck)[] filter)
        {
            var stand = new Katalogfilterstand();
            foreach (var f in filter) stand.Setzen(f.Spalte, f.Ausdruck);
            return Katalogfilter.Anwenden(profil, zeilen, stand).Count;
        }
    }
}
