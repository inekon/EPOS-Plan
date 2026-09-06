using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Prüfstand der Stufe S3 des Wechselrichterkonzepts</b> — der RECHENWEG
    /// (Anwenderentscheide <b>W6‑E‑2</b> und <b>W6‑E‑3</b> vom 06.09.2026,
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 4.1, 4.3, 4.4 und 8/S3).
    ///
    /// <para><b>Zwei Hälften, und die Trennung hat einen Grund.</b> Teil 1 rechnet OHNE
    /// Datenbank auf <see cref="PvStrangModell"/> — dort sind die Aussagen EXAKT
    /// nachrechenbar, weil die Eingangsreihen von Hand gesetzt sind. Teil 2 fährt einen
    /// echten Simulationslauf gegen eine Arbeitskopie der Testdatenbank; dort ist die
    /// Aussage „dieselbe Anlage rechnet auf zwei Wegen dasselbe", und die ist nur mit
    /// echten Klimadaten etwas wert.</para>
    ///
    /// <para><b>Was hier NICHT steht: der Nachweis der Byte-Gleichheit.</b> Den führt
    /// der Referenzlauf 1030 / 1007 / 1017 gegen
    /// <c>Referenzlaeufe/2026-09-05_R2_Zeitbasis</c> — kein Referenzprojekt hat eine
    /// Strangzeile, und wäre auch nur ein CSV verschieden, wäre die Vorrangregel
    /// verletzt. Teil 2 führt die Gegenprobe DAZU: dieselbe Anlage, einmal mit und
    /// einmal ohne Schalter.</para>
    ///
    /// <para><b>Eine Arbeitskopie je Klasse</b>; fehlt die Datei, schweigen die Fälle.
    /// <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvStrangRechnungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public PvStrangRechnungTests(TestDatenbank db) { _db = db; }

        // =================================================================================
        // Teil 1 — die Kennlinie (Prüfstand-Nachweis 1)
        // =================================================================================

        /// <summary>
        /// <b>An den sechs Stützstellen ist das Ergebnis exakt der eingegebene Wert.</b>
        /// Nicht „auf zwölf Stellen" — exakt: Die Interpolationsformeln sind so
        /// geschrieben, dass am linken Rand der Zähler exakt 0 wird und am rechten Rand
        /// der jeweils nächste Abschnitt beginnt.
        /// </summary>
        [Fact]
        public void Die_Kennlinie_trifft_die_sechs_Stuetzstellen_exakt()
        {
            double?[] etas = { 0.900, 0.940, 0.962, 0.970, 0.975, 0.970 };

            for (int i = 0; i < PvStrangModell.Stuetzstellen.Length; i++)
                Assert.Equal(etas[i].Value,
                             PvStrangModell.EtaWechselrichter(PvStrangModell.Stuetzstellen[i], etas));
        }

        /// <summary>
        /// <b>Dazwischen linear.</b> Geprüft wird an drei Mittelpunkten gegen die von
        /// Hand gerechnete Gerade — und unterhalb der kleinsten Stützstelle gegen die
        /// Gerade durch den Ursprung.
        /// </summary>
        [Fact]
        public void Die_Kennlinie_interpoliert_dazwischen_linear()
        {
            double?[] etas = { 0.900, 0.940, 0.962, 0.970, 0.975, 0.970 };

            // Mitte zwischen 5 % (0,900) und 10 % (0,940): 7,5 % -> 0,920
            Assert.Equal(0.920, PvStrangModell.EtaWechselrichter(0.075, etas), 12);

            // Mitte zwischen 20 % (0,962) und 30 % (0,970): 25 % -> 0,966
            Assert.Equal(0.966, PvStrangModell.EtaWechselrichter(0.25, etas), 12);

            // Mitte zwischen 50 % (0,975) und 100 % (0,970): 75 % -> 0,9725
            Assert.Equal(0.9725, PvStrangModell.EtaWechselrichter(0.75, etas), 12);

            // Unter der kleinsten Stuetzstelle: Gerade durch (0; 0). Bei 2,5 % die
            // Haelfte von 0,900.
            Assert.Equal(0.450, PvStrangModell.EtaWechselrichter(0.025, etas), 12);

            // Ueber 100 %: konstant - dahinter greift das Clipping.
            Assert.Equal(0.970, PvStrangModell.EtaWechselrichter(1.60, etas));
            Assert.Equal(0.0, PvStrangModell.EtaWechselrichter(0.0, etas));
        }

        /// <summary>
        /// <b>Eine fehlende Stützstelle wird übersprungen</b> (Konzept 3.3.1): Ohne
        /// 20 % und 30 % läuft die Gerade von 10 % zu 50 % durch. Bei 30 % Auslastung
        /// ist das genau die Hälfte zwischen 0,940 und 0,980.
        /// </summary>
        [Fact]
        public void Eine_fehlende_Stuetzstelle_wird_uebersprungen()
        {
            double?[] etas = { null, 0.940, null, null, 0.980, 0.960 };

            Assert.Equal(0.940, PvStrangModell.EtaWechselrichter(0.10, etas));
            Assert.Equal(0.960, PvStrangModell.EtaWechselrichter(0.30, etas), 12);
            Assert.Equal(0.980, PvStrangModell.EtaWechselrichter(0.50, etas));

            // Unter 10 % gilt jetzt die Gerade zur kleinsten VORHANDENEN Stuetzstelle.
            Assert.Equal(0.470, PvStrangModell.EtaWechselrichter(0.05, etas), 12);
        }

        /// <summary>
        /// <b>Ohne jede Stützstelle gilt die Dreipunkt-Vorgabe</b> eines typischen
        /// Strang-Wechselrichters — dieselbe wie im Anlagenweg, und der Aufrufer
        /// erfährt es (<see cref="PvStrangModell.Kennlinie"/> liefert <c>false</c>).
        /// </summary>
        [Fact]
        public void Ohne_jede_Stuetzstelle_gilt_die_Dreipunkt_Vorgabe()
        {
            var leer = new double?[6];

            double[] x, y;
            Assert.False(PvStrangModell.Kennlinie(leer, out x, out y));

            Assert.Equal(PvErweitertesModell.WR_ETA10_VORGABE,
                         PvStrangModell.EtaWechselrichter(0.10, leer));
            Assert.Equal(PvErweitertesModell.WR_ETA50_VORGABE,
                         PvStrangModell.EtaWechselrichter(0.50, leer));
            Assert.Equal(PvErweitertesModell.WR_ETA100_VORGABE,
                         PvStrangModell.EtaWechselrichter(1.00, leer));
        }

        /// <summary>
        /// <b>Die Brücke zum Anlagenweg</b> (Abnahme S3 (2) des Konzepts): Ein Gerät,
        /// das NUR die Stützstellen 10 / 50 / 100 % führt, rechnet <b>zeichengleich</b>
        /// zu <see cref="PvErweitertesModell.EtaWechselrichter"/> — geprüft ohne
        /// Toleranz über 1 601 Auslastungen von 0 bis 1,6.
        ///
        /// <para>Daran hängt der DB-Fall
        /// <see cref="Ein_Strang_ohne_Clipping_rechnet_wie_die_Anlage_vereinfacht"/>:
        /// Er wäre ohne diese Gleichheit gar nicht möglich.</para>
        /// </summary>
        [Fact]
        public void Die_Dreipunkt_Kennlinie_rechnet_zeichengleich_zum_Anlagenweg()
        {
            const double e10 = 0.94, e50 = 0.975, e100 = 0.97;
            double?[] etas = { null, e10, null, null, e50, e100 };

            for (int i = 0; i <= 1600; i++)
            {
                double x = i / 1000.0;
                Assert.Equal(PvErweitertesModell.EtaWechselrichter(x, e10, e50, e100),
                             PvStrangModell.EtaWechselrichter(x, etas));
            }
        }

        // =================================================================================
        // Teil 1b — Clipping, Nachtverbrauch, Gruppierung (Nachweise 2 und 6)
        // =================================================================================

        /// <summary>
        /// <b>Der Clipping-Verlust ist die Summe der Kappungen</b> (Nachweis 2): Über
        /// eine Reihe von Hand gesetzter Stunden wird jede Kappung einzeln nachgerechnet
        /// und mit der mitgeführten Jahressumme verglichen — dazu die Kennzahlen
        /// Ertrag, Kennlinienverlust und Anteil.
        /// </summary>
        [Fact]
        public void Der_Clipping_Verlust_ist_die_Summe_der_Kappungen()
        {
            PvStrangModell.Geraetegruppe g = Gruppe(nennKw: 2.0, eta: 0.97);

            double[] dc = { 0.0, 0.5, 1.0, 2.0, 2.5, 3.0, 2.0625, 0.15 };

            double erwartetClip = 0.0, erwartetErtrag = 0.0, erwartetVerlust = 0.0;
            foreach (double d in dc)
            {
                double ac = PvStrangModell.Stunde(g, d);

                double eta = d > 0.0 ? 0.97 : 0.0;
                double roh = d * eta;
                double kappung = Math.Max(0.0, roh - 2.0);

                erwartetClip += kappung;
                erwartetErtrag += roh - kappung;
                if (d > 0.0) erwartetVerlust += d - roh;

                Assert.Equal(roh - kappung, ac, 12);
            }

            Assert.Equal(erwartetClip, g.ClippingKwh, 12);
            Assert.Equal(erwartetErtrag, g.ErtragKwh, 12);
            Assert.Equal(erwartetVerlust, g.WrVerlustKwh, 12);
            Assert.Equal(dc.Sum(), g.DcSysKwh, 12);

            // Der Anteil bezieht sich auf den UNGEKLIPPTEN Wechselstromertrag.
            Assert.Equal(erwartetClip * 100.0 / (erwartetErtrag + erwartetClip),
                         g.ClippingAnteilProzent, 12);

            // Der Jahresnutzungsgrad ist Σ P_AC / Σ P_DC,sys - mit Clipping deutlich
            // unter dem Kennlinienwirkungsgrad.
            Assert.Equal(erwartetErtrag / dc.Sum(), g.Jahresnutzungsgrad, 12);
            Assert.True(g.Jahresnutzungsgrad < 0.97);

            // Volllaststunden AC = Ertrag / AC-Nennleistung.
            Assert.Equal(erwartetErtrag / 2.0, g.VolllaststundenAc, 12);
        }

        /// <summary>
        /// <b>Ohne AC-Nennleistung gibt es kein Clipping</b> — die Auslastung bezieht
        /// sich dann ersatzweise auf die DC-Nennleistung der angeschlossenen Stränge
        /// (dieselbe Rückfallebene wie im Anlagenweg).
        /// </summary>
        [Fact]
        public void Ohne_AC_Nennleistung_wird_nicht_geklippt()
        {
            PvStrangModell.Geraetegruppe g = Gruppe(nennKw: null, eta: 0.97);
            g.KwpDc = 4.0;

            double ac = PvStrangModell.Stunde(g, 4.0);

            Assert.Equal(4.0 * 0.97, ac, 12);
            Assert.Equal(0.0, g.ClippingKwh);
            Assert.Equal(0.0, g.DcAc);
        }

        /// <summary>
        /// <b>Der Nachtverbrauch fällt nur unterhalb der Einschaltschwelle an</b>
        /// (Nachweis 6) — und er ist eine NEGATIVE Erzeugung, keine Zahl neben dem
        /// Ergebnis.
        /// </summary>
        [Fact]
        public void Der_Nachtverbrauch_faellt_nur_unter_der_Einschaltschwelle_an()
        {
            PvStrangModell.Geraetegruppe g = Gruppe(nennKw: 2.0, eta: 0.97,
                                                    standbyW: 20.0, nachtW: 5.0);

            // Vier Nachtstunden (0 und knapp unter 20 W) und zwei Betriebsstunden.
            Assert.Equal(-0.005, PvStrangModell.Stunde(g, 0.0), 12);
            Assert.Equal(-0.005, PvStrangModell.Stunde(g, 0.0199), 12);
            Assert.Equal(-0.005, PvStrangModell.Stunde(g, 0.020), 12);   // genau auf der Schwelle
            Assert.Equal(-0.005, PvStrangModell.Stunde(g, 0.0), 12);

            Assert.Equal(0.0201 * 0.97 * (0.0201 / 2.0) / 0.05, PvStrangModell.Stunde(g, 0.0201), 12);
            Assert.Equal(1.0 * 0.97, PvStrangModell.Stunde(g, 1.0), 12);

            Assert.Equal(4, g.Nachtstunden);
            Assert.Equal(4 * 0.005, g.NachtKwh, 12);
            Assert.Equal(0.0, g.ClippingKwh);
        }

        /// <summary>
        /// <b>Ohne gepflegten Nachtverbrauch entsteht keiner</b> — eine Nachtstunde
        /// liefert dann glatte 0 und keinen erfundenen Verbrauch.
        /// </summary>
        [Fact]
        public void Ohne_gepflegten_Nachtverbrauch_bleibt_die_Nacht_bei_null()
        {
            PvStrangModell.Geraetegruppe g = Gruppe(nennKw: 2.0, eta: 0.97);

            Assert.Equal(0.0, PvStrangModell.Stunde(g, 0.0));

            Assert.Equal(1, g.Nachtstunden);
            Assert.Equal(0.0, g.NachtKwh);
            Assert.Equal(0.0, g.ErtragKwh);
        }

        /// <summary>
        /// <b>Die Gruppierung</b> (Konzept 3.4, Q6): Stränge desselben Geräts kommen
        /// zusammen, zwei Gerätenummern sind zwei Geräte, und ein Strang ohne Gerät
        /// fällt heraus und wird GEZÄHLT — damit der Aufrufer ihn melden kann, statt
        /// still zu rechnen.
        /// </summary>
        [Fact]
        public void Die_Gruppierung_trennt_nach_Geraet_und_Nummer()
        {
            var geraete = new Dictionary<int, WechselrichterModel>
            {
                { 7, Geraet(2.0, 0.97) },
                { 8, Geraet(3.0, 0.97) }
            };

            var straenge = new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Rang = 1, ID_Wechselrichter = 7, Geraetenummer = 1, Mppt = 1, Module_Reihe = 10 },
                new AnlageStrangModel { Rang = 2, ID_Wechselrichter = 7, Geraetenummer = 1, Mppt = 2, Module_Reihe = 10 },
                new AnlageStrangModel { Rang = 3, ID_Wechselrichter = 7, Geraetenummer = 2, Mppt = 1, Module_Reihe = 10 },
                new AnlageStrangModel { Rang = 4, ID_Wechselrichter = 8, Geraetenummer = 1, Mppt = 1, Module_Reihe = 10 },
                new AnlageStrangModel { Rang = 5, ID_Wechselrichter = null, Module_Reihe = 10 },
                new AnlageStrangModel { Rang = 6, ID_Wechselrichter = 99, Module_Reihe = 10 }
            };

            int ohneGeraet;
            List<PvStrangModell.Geraetegruppe> gruppen =
                PvStrangModell.Gruppieren(straenge, geraete, out ohneGeraet);

            Assert.Equal(2, ohneGeraet);          // kein Geraet + unbekanntes Geraet
            Assert.Equal(3, gruppen.Count);

            Assert.Equal(new[] { 7, 7, 8 }, gruppen.Select(g => g.ID_Wechselrichter).ToArray());
            Assert.Equal(new[] { 1, 2, 1 }, gruppen.Select(g => g.Geraetenummer).ToArray());
            Assert.Equal(new[] { 2, 1, 1 }, gruppen.Select(g => g.Straenge.Count).ToArray());

            // Der MPP-Eingang trennt NICHT (Q7): zwei Tracker eines Geraets sind eine
            // Summe und eine Clipping-Grenze.
            Assert.Equal(new[] { 1, 2 }, gruppen[0].Straenge.Select(s => s.MpptOderEins).ToArray());
        }

        /// <summary>
        /// <b>Ost/West — die Aussage, für die die Stufe gebaut wird</b> (Nachweis 5,
        /// Abnahme S3 (3) des Konzepts), hier EXAKT:
        ///
        /// <code>
        /// Ertrag(zwei getrennte Geräte) − Ertrag(ein gemeinsames Gerät)
        ///     = Clipping(gemeinsam) − Clipping(Ost) − Clipping(West)
        /// </code>
        ///
        /// <para>Die Reihe ist so gewählt, dass jede Stunde entweder null ist oder über
        /// 5 % Auslastung liegt — dann ist die Kennlinie konstant, und die Gleichung
        /// gilt Zeile für Zeile. Im echten Jahreslauf kommt der Anlaufast unter 5 %
        /// dazu; der DB-Fall
        /// <see cref="Ost_West_an_einem_Geraet_kostet_das_gemeinsame_Clipping"/> misst
        /// ihn und benennt ihn.</para>
        /// </summary>
        [Fact]
        public void Ost_West_an_einem_Geraet_kostet_genau_das_gemeinsame_Clipping()
        {
            PvStrangModell.Geraetegruppe gemeinsam = Gruppe(nennKw: 2.0, eta: 0.97);
            PvStrangModell.Geraetegruppe ost = Gruppe(nennKw: 2.0, eta: 0.97);
            PvStrangModell.Geraetegruppe west = Gruppe(nennKw: 2.0, eta: 0.97);

            // Ein Tagesgang: morgens Ost, mittags beide, abends West.
            double[] o = { 0.0, 0.40, 1.30, 1.80, 1.40, 0.60, 0.15, 0.0 };
            double[] w = { 0.0, 0.15, 0.55, 1.35, 1.85, 1.40, 0.45, 0.0 };

            for (int i = 0; i < o.Length; i++)
            {
                PvStrangModell.Stunde(gemeinsam, o[i] + w[i]);
                PvStrangModell.Stunde(ost, o[i]);
                PvStrangModell.Stunde(west, w[i]);
            }

            double differenz = ost.ErtragKwh + west.ErtragKwh - gemeinsam.ErtragKwh;
            double gemeinsamesClipping =
                gemeinsam.ClippingKwh - ost.ClippingKwh - west.ClippingKwh;

            Assert.True(gemeinsamesClipping > 0.0,
                        "Ohne gemeinsames Clipping prueft der Fall nichts.");
            Assert.Equal(gemeinsamesClipping, differenz, 12);

            // Die Gleichstromseite ist von der Geraetewahl unberuehrt.
            Assert.Equal(ost.DcSysKwh + west.DcSysKwh, gemeinsam.DcSysKwh, 12);
        }

        // =================================================================================
        // Teil 2 — der Lauf gegen die Testdatenbank
        // =================================================================================

        /// <summary>
        /// <b>Nachweis 3 — ohne Zuordnung ändert sich nichts.</b> Dieselbe Anlage
        /// rechnet mit <c>PV_Wechselrichterweg</c> NULL, mit „vereinfacht" und sogar mit
        /// „mit Wechselrichter" OHNE Strangzeile denselben Jahresertrag — bitgleich,
        /// nicht auf zwölf Stellen.
        ///
        /// <para>Das ist die Gegenprobe zum Referenzlauf: Er belegt, dass die elf
        /// Referenzprojekte byte-gleich bleiben; dieser Fall belegt, dass auch der
        /// SCHALTER allein nichts ändert. Beide Bedingungen der Vorrangregel müssen
        /// erfüllt sein (Konzept 7.1).</para>
        /// </summary>
        [Fact]
        public void Ohne_Strangzeile_rechnet_der_Schalter_nichts()
        {
            if (!_db.Vorhanden) return;

            const string ohneWahl = "S3 Schalter NULL";
            const string vereinfacht = "S3 Schalter vereinfacht";
            const string katalog = "S3 Schalter Katalog ohne Strang";

            int a1 = PvAnlageAnlegen(ohneWahl, weg: null);
            int a2 = PvAnlageAnlegen(vereinfacht, weg: DbWerte.PV_WR_WEG_VEREINFACHT);
            int a3 = PvAnlageAnlegen(katalog, weg: DbWerte.PV_WR_WEG_KATALOG);

            var pv = Lauf();

            double erwartet = Ertrag(pv, ohneWahl);
            Assert.True(erwartet > 0.0, "Ohne Ertrag prueft der Fall nichts.");

            Assert.Equal(erwartet, Ertrag(pv, vereinfacht));
            Assert.Equal(erwartet, Ertrag(pv, katalog));

            // Und keine der drei weist ein Geraet aus - es gibt keinen Strang.
            Assert.All(new[] { ohneWahl, vereinfacht, katalog },
                       n => Assert.Empty(Zeile(pv, n).Geraete));

            AnlageLoeschen(a1);
            AnlageLoeschen(a2);
            AnlageLoeschen(a3);
        }

        /// <summary>
        /// <b>Nachweis 4 / Abnahme S3 (2) des Konzepts.</b> Eine Anlage mit EINEM
        /// Strang, dessen Gerät die Stützstellen 10 / 50 / 100 % genau der
        /// Dreipunkt-Kennlinie der Anlagenzeile führt, rechnet denselben Jahresertrag
        /// wie dieselbe Anlage auf dem vereinfachten Weg im Modell ERWEITERT —
        /// <b>bitgleich</b>.
        ///
        /// <para>Beide Anlagen laufen im SELBEN Simulationslauf und werden über
        /// <c>Modul_Ergebnisse</c> auseinandergehalten; damit hängt der Vergleich an
        /// keiner Summe über fremde Anlagen.</para>
        ///
        /// <para><b>Warum das überhaupt möglich ist:</b> Die Modulformel ist dieselbe
        /// (ein Strang mit allen Modulen ist das Modulfeld), die Nennleistung wird in
        /// derselben Reihenfolge gebildet, und die Sechspunkt-Kennlinie ist an den drei
        /// gemeinsamen Punkten zeichengleich zur Dreipunkt-Kennlinie
        /// (<see cref="Die_Dreipunkt_Kennlinie_rechnet_zeichengleich_zum_Anlagenweg"/>).</para>
        /// </summary>
        [Fact]
        public void Ein_Strang_ohne_Clipping_rechnet_wie_die_Anlage_vereinfacht()
        {
            if (!_db.Vorhanden) return;

            const string ohne = "S3 Vergleich vereinfacht";
            const string mit = "S3 Vergleich mit Strang";

            int aOhne = PvAnlageAnlegen(ohne, weg: null);
            int aMit = PvAnlageAnlegen(mit, weg: DbWerte.PV_WR_WEG_KATALOG);

            int geraet = GeraetAnlegen("S3 Muster 3000TL", nennKw: 3.0,
                                       eta10: PvErweitertesModell.WR_ETA10_VORGABE,
                                       eta50: PvErweitertesModell.WR_ETA50_VORGABE,
                                       eta100: PvErweitertesModell.WR_ETA100_VORGABE);

            Assert.True(new AnlageStrangCtrl().SchreibenJeAnlage(aMit, new List<AnlageStrangModel>
            {
                new AnlageStrangModel
                {
                    ID_Wechselrichter = geraet, Geraetenummer = 1, Mppt = 1,
                    Module_Reihe = MODULE, Straenge_Parallel = 1
                }
            }));

            var pv = Lauf();
            double ertragOhne = Ertrag(pv, ohne);
            double ertragMit = Ertrag(pv, mit);

            Assert.True(ertragOhne > 0.0, "Ohne Ertrag prueft der Fall nichts.");
            Assert.Equal(ertragOhne, ertragMit);

            // Und die Anlage MIT Strang weist ihre Geraete aus, die andere nicht.
            Assert.Single(Zeile(pv, mit).Geraete);
            Assert.Empty(Zeile(pv, ohne).Geraete);

            AnlageLoeschen(aOhne);
            AnlageLoeschen(aMit);
            GeraetLoeschen(geraet);
        }

        /// <summary>
        /// <b>Nachweis 5 / Abnahme S3 (3) des Konzepts — der Ost/West-Fall am echten
        /// Jahreslauf.</b> Zwei Stränge (Ost und West) an EINEM Gerät gegen zwei
        /// getrennte Anlagen mit je einem Gerät desselben Typs.
        ///
        /// <para>Die Aussage: Der gemeinsame Wechselrichter erntet WENIGER, und der
        /// Unterschied ist das gemeinsame Clipping. Die exakte Gleichung dazu steht in
        /// <see cref="Ost_West_an_einem_Geraet_kostet_genau_das_gemeinsame_Clipping"/>;
        /// hier kommt der ANLAUFAST der Kennlinie unter 5 % Auslastung dazu, der in der
        /// Dämmerung nicht additiv ist. Statt einer Toleranz steht deshalb die
        /// ZERLEGUNG: Aus <c>Ertrag = Σ P_DC,sys − Kennlinienverlust − Clipping</c>
        /// folgt Zeile für Zeile</para>
        ///
        /// <code>
        /// (Ost + West) − gemeinsam
        ///     = Gleichstromversatz − Kennliniengewinn + gemeinsames Clipping
        /// </code>
        ///
        /// <para>und diese Gleichung geht auf sechs Nachkommastellen auf. Der
        /// KENNLINIENGEWINN ist dabei kein Fehler, sondern eine zweite Aussage der
        /// Stufe: Ein gemeinsames Gerät läuft in der Dämmerung auf höherer Teillast als
        /// zwei getrennte und verliert dort weniger. Er mindert den Ost/West-Nachteil,
        /// er hebt ihn nicht auf — geprüft wird auch das
        /// (<c>0 &lt; Kennliniengewinn &lt; gemeinsames Clipping</c>).</para>
        /// </summary>
        [Fact]
        public void Ost_West_an_einem_Geraet_kostet_das_gemeinsame_Clipping()
        {
            if (!_db.Vorhanden) return;

            const string gemeinsam = "S3 OstWest gemeinsam";
            const string nurOst = "S3 OstWest nur Ost";
            const string nurWest = "S3 OstWest nur West";

            int aGem = PvAnlageAnlegen(gemeinsam, weg: DbWerte.PV_WR_WEG_KATALOG, module: 2 * MODULE);
            int aOst = PvAnlageAnlegen(nurOst, weg: DbWerte.PV_WR_WEG_KATALOG);
            int aWest = PvAnlageAnlegen(nurWest, weg: DbWerte.PV_WR_WEG_KATALOG);

            // Ein knapp ausgelegtes Geraet - ohne Clipping prueft der Fall nichts.
            int geraet = GeraetAnlegen("S3 Muster 2000TL", nennKw: 2.0,
                                       eta10: 0.97, eta50: 0.97, eta100: 0.97);

            var ctrl = new AnlageStrangCtrl();

            Assert.True(ctrl.SchreibenJeAnlage(aGem, new List<AnlageStrangModel>
            {
                Strang(geraet, mppt: 1, azimut: -90),
                Strang(geraet, mppt: 2, azimut: 90)
            }));
            Assert.True(ctrl.SchreibenJeAnlage(aOst, new List<AnlageStrangModel>
            {
                Strang(geraet, mppt: 1, azimut: -90)
            }));
            Assert.True(ctrl.SchreibenJeAnlage(aWest, new List<AnlageStrangModel>
            {
                Strang(geraet, mppt: 1, azimut: 90)
            }));

            var pv = Lauf();

            PvStrangModell.Geraetegruppe gem = Assert.Single(Zeile(pv, gemeinsam).Geraete);
            PvStrangModell.Geraetegruppe ost = Assert.Single(Zeile(pv, nurOst).Geraete);
            PvStrangModell.Geraetegruppe west = Assert.Single(Zeile(pv, nurWest).Geraete);

            double differenz = ost.ErtragKwh + west.ErtragKwh - gem.ErtragKwh;
            double gemeinsamesClipping = gem.ClippingKwh - ost.ClippingKwh - west.ClippingKwh;
            double kennlinienGewinn = ost.WrVerlustKwh + west.WrVerlustKwh - gem.WrVerlustKwh;
            double dcVersatz = ost.DcSysKwh + west.DcSysKwh - gem.DcSysKwh;

            Assert.True(gemeinsamesClipping > 0.0,
                        "Ohne gemeinsames Clipping prueft der Fall nichts.");
            Assert.True(differenz > 0.0,
                        "Das gemeinsame Geraet muss WENIGER ernten als zwei getrennte.");

            // DIE ZERLEGUNG, und sie geht exakt auf. Aus
            // Ertrag = Σ P_DC,sys − Kennlinienverlust − Clipping folgt Zeile fuer Zeile
            //     (Ost + West) − gemeinsam
            //         = Gleichstromversatz − Kennliniengewinn + gemeinsames Clipping.
            Assert.Equal(dcVersatz - kennlinienGewinn + gemeinsamesClipping, differenz, 6);

            // UND DAS CLIPPING IST DER GROESSERE TEIL. Der Kennliniengewinn ist real:
            // Ein gemeinsames Geraet laeuft in der Daemmerung auf hoeherer Teillast als
            // zwei getrennte und verliert dort weniger. Er MINDERT die Aussage, er
            // widerlegt sie nicht - der Ost/West-Nachteil bleibt das Clipping.
            Assert.True(kennlinienGewinn > 0.0 && kennlinienGewinn < gemeinsamesClipping,
                        "Ost/West-Differenz " + differenz.ToString("N1") +
                        " kWh = gemeinsames Clipping " + gemeinsamesClipping.ToString("N1") +
                        " kWh − Kennliniengewinn " + kennlinienGewinn.ToString("N1") +
                        " kWh + Gleichstromversatz " + dcVersatz.ToString("N6") + " kWh.");

            // Die Gleichstromseite ist von der Geraetewahl unberuehrt - der Versatz ist
            // allein die Reihenfolge der Gleitkommaadditionen.
            Assert.Equal(1.0, gem.DcSysKwh / (ost.DcSysKwh + west.DcSysKwh), 9);

            AnlageLoeschen(aGem);
            AnlageLoeschen(aOst);
            AnlageLoeschen(aWest);
            GeraetLoeschen(geraet);
        }

        // =================================================================================
        // Innenleben
        // =================================================================================

        private const int TESTPROJEKT = 1030;
        private const int MODULE = 10;
        private const double MODULLEISTUNG_W = 275.19;

        private static PvStrangModell.Geraetegruppe Gruppe(double? nennKw, double eta,
                                                           double? standbyW = null,
                                                           double? nachtW = null)
        {
            WechselrichterModel g = Geraet(nennKw, eta);
            g.m_P_Standby = standbyW;
            g.m_P_Nacht = nachtW;
            return PvStrangModell.Anlegen(1, 1, g);
        }

        private static WechselrichterModel Geraet(double? nennKw, double eta)
        {
            return new WechselrichterModel
            {
                m_ID = 1,
                m_szName = "Probe",
                m_P_AC_Nenn = nennKw,
                m_Eta05 = eta, m_Eta10 = eta, m_Eta20 = eta,
                m_Eta30 = eta, m_Eta50 = eta, m_Eta100 = eta
            };
        }

        private static AnlageStrangModel Strang(int geraet, int mppt, int azimut)
        {
            return new AnlageStrangModel
            {
                ID_Wechselrichter = geraet, Geraetenummer = 1, Mppt = mppt,
                Module_Reihe = MODULE, Straenge_Parallel = 1, Azimut = azimut
            };
        }

        /// <summary>Ein Simulationslauf der Photovoltaik des Testprojekts.</summary>
        private static SimulationPV Lauf()
        {
            var pv = new SimulationPV();
            pv.Berechnung(TESTPROJEKT);
            return pv;
        }

        private static PVModulErgebnis Zeile(SimulationPV pv, string name)
        {
            return Assert.Single(pv.Modul_Ergebnisse,
                                 m => string.Equals(m.Name, name, StringComparison.Ordinal));
        }

        private static double Ertrag(SimulationPV pv, string name)
        {
            return Zeile(pv, name).Stromproduktion;
        }

        private static double Jahresertrag(string name)
        {
            return Ertrag(Lauf(), name);
        }

        /// <summary>
        /// Eine PV-Anlage im Testprojekt: Modell ERWEITERT, damit BEIDE Wege dieselbe
        /// Modulformel (Huld) nehmen, mit der Dreipunkt-Kennlinie und der
        /// AC-Nennleistung der Anlagenzeile.
        /// </summary>
        private static int PvAnlageAnlegen(string bezeichner, string weg, int module = MODULE)
        {
            var m = new WErzeugerCtrl
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.PV_TYP,
                ID_PV = ModulAnlegen(),
                PV_Leistung = module,
                m_Neigung = 30,
                m_Azimut = 0,
                PV_Modell = DbWerte.PV_MODELL_ERWEITERT,
                PV_Systemverluste = 12.0,
                PV_WrNennleistungKw = 3.0,
                PV_WrEta10 = PvErweitertesModell.WR_ETA10_VORGABE,
                PV_WrEta50 = PvErweitertesModell.WR_ETA50_VORGABE,
                PV_WrEta100 = PvErweitertesModell.WR_ETA100_VORGABE,
                PV_Wechselrichterweg = weg
            };
            Assert.True(m.Insert());
            return AnlagenId(bezeichner);
        }

        /// <summary>Die Projektkopie eines Wegwerf-Moduls MIT Zelltechnologie (Huld).</summary>
        private static int ModulAnlegen()
        {
            if (m_Modul > 0) return m_Modul;

            int id = DataRepository.GetMaxID(SchemaKatalog.TAB_PV) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + SchemaKatalog.TAB_PV + "] " +
                "(ID, ID_Projekt, Bezeichner, Leistung, Laenge, Breite, Wirkungsgrad, " +
                " gamma_PMP, T_NOCT, Technologie) VALUES (?,?,?,?,?,?,?,?,?,?)",
                new DbParam("@id", id), new DbParam("@p", TESTPROJEKT),
                new DbParam("@b", "Strangrechnung 275"),
                new DbParam("@l", MODULLEISTUNG_W),
                new DbParam("@lae", 1.65), new DbParam("@bre", 1.0),
                new DbParam("@wir", 16.68), new DbParam("@g", -0.39),
                new DbParam("@noct", 45.0),
                new DbParam("@tech", DbWerte.PV_TECHNOLOGIE_C_SI)));

            m_Modul = id;
            return id;
        }

        private static int m_Modul;

        private static int GeraetAnlegen(string bezeichner, double nennKw,
                                         double eta10, double eta50, double eta100)
        {
            int id = DataRepository.GetMaxID(WechselrichterCtrl.TABLE) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + WechselrichterCtrl.TABLE + "] " +
                "(ID, ID_Projekt, Bezeichner, P_AC_Nenn, Eta10, Eta50, Eta100) " +
                "VALUES (?,?,?,?,?,?,?)",
                new DbParam("@id", id), new DbParam("@p", TESTPROJEKT),
                new DbParam("@b", bezeichner), new DbParam("@n", nennKw),
                new DbParam("@e10", eta10), new DbParam("@e50", eta50),
                new DbParam("@e100", eta100)));
            return id;
        }

        private static void GeraetLoeschen(int id)
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM [" + WechselrichterCtrl.TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
        }

        private static int AnlagenId(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                " WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", TESTPROJEKT), new DbParam("@b", bezeichner));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static void AnlageLoeschen(int id)
        {
            if (id <= 0) return;
            DataRepository.ExecuteSQL(
                "DELETE FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN + " WHERE ID = ?",
                new DbParam("@id", id));
        }
    }
}
