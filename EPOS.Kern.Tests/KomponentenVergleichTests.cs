using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Proben der GEGENÜBERSTELLUNG „Stammprojekt und Varianten"
    /// (<see cref="KomponentenVergleich"/>) — Anwenderbefund W5‑E‑2 vom 05.09.2026.
    ///
    /// <para><b>Der Befund.</b> Die Tabelle begann mit dem Gewerk „Anlage" und
    /// zeigte darunter dreizehn PARAMETER (Betriebsart, Vor- und Rücklauf,
    /// Abschaltpunkt, Neigung, Azimut, Solaranteil, Speichervolumen …).
    /// Wortlaut: „Gewerk Anlage gibt es nicht. Dort stehen Parameter.
    /// Dargestellt werden nur die Erzeugerkomponenten, die verwendet werden,
    /// keine Parameter."</para>
    ///
    /// <para><b>Warum die Proben hier stehen.</b> Die Zeilenbildung lag bis zu
    /// diesem Befund im Oberflächencode (<c>UebersichtSeiteGaben.FuelleVergleich</c>)
    /// und war damit nur am Windows-Gerät prüfbar. Sie liegt jetzt im Kern und
    /// wird hier gegen die Testdatenbank UND gegen synthetische Bestände gehalten.</para>
    ///
    /// <para><b>Das Projekt der Proben ist das des Bildschirmfotos</b>: 1042
    /// „Booster-Kette mit Kombi-Speicher" mit seiner Variante 1044
    /// „Schichtspeicher". Beide führen zwei Wärmepumpen, einen Spitzenkessel und
    /// vier Pufferspeicher — und kein BHKW, keine Solarthermie, keine
    /// Photovoltaik, keinen Stromspeicher.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KomponentenVergleichTests
    {
        /// <summary>Das Stammprojekt des Bildschirmfotos.</summary>
        private const int STAMM = 1042;

        /// <summary>Seine Variante „Schichtspeicher".</summary>
        private const int VARIANTE = 1044;

        /// <summary>Die Merkmale des Blocks „Anlage" — sie dürfen nicht mehr erscheinen.</summary>
        private static readonly string[] ANLAGENMERKMALE =
        {
            "Betriebsart", "Vorlauftemperatur", "Rücklauftemperatur", "Bivalenter Betrieb",
            "Abschaltpunkt", "Heizstab", "Grenzleistung", "PV-Leistung", "Neigung", "Azimut",
            "Kollektormodulanzahl", "Solaranteil", "Speichervolumen (Anlage)", "Wärmequelle"
        };

        // =============================================================================
        //  V1 — der Befund selbst: kein Gewerk „Anlage", kein Gewerk „Gebäude"
        // =============================================================================
        [Fact]
        public void V1_Die_Gegenueberstellung_zeigt_weder_Anlagen_noch_Gebaeudeparameter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<KomponentenVergleichZeile> zeilen = Gegenueberstellung();
            Assert.NotEmpty(zeilen);

            foreach (KomponentenVergleichZeile z in zeilen)
            {
                Assert.NotEqual("Anlage", z.Gewerk);
                Assert.NotEqual("Gebäude", z.Gewerk);
                Assert.DoesNotContain(z.Merkmal, ANLAGENMERKMALE);
            }
        }

        // =============================================================================
        //  V2 — es erscheinen genau die VERWENDETEN Erzeugergewerke
        // =============================================================================
        [Fact]
        public void V2_Nur_verwendete_Erzeugergewerke_in_der_Reihenfolge_der_Gewerktabellen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<string> gewerke = Gegenueberstellung()
                .Where(z => z.Gewerk.Length > 0).Select(z => z.Gewerk).ToList();

            // Die Reihenfolge ist die von ProjektDetails.GewerkTabellen: Wärmepumpe,
            // BHKW, Spitzenkessel, Solarthermie, Photovoltaik, Pufferspeicher,
            // Stromspeicher — die vier ungenutzten fallen ersatzlos weg.
            Assert.Equal(new[] { "Wärmepumpe", "Spitzenkessel", "Pufferspeicher" }, gewerke);
        }

        // =============================================================================
        //  V3 — Aufbau eines Gewerkblocks: eine Anzahlzeile, dann je Komponente eine
        // =============================================================================
        [Fact]
        public void V3_Je_Gewerk_eine_Anzahlzeile_und_je_Komponente_eine_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<KomponentenVergleichZeile> zeilen = Gegenueberstellung();

            // 2 Wärmepumpen + 1 Spitzenkessel + 4 Pufferspeicher, je Gewerk eine
            // Kopfzeile: 3 + 2 + 5 = 10 Zeilen.
            Assert.Equal(10, zeilen.Count);

            // Die Kopfzeile trägt das Gewerk und die Stückzahl je Version.
            KomponentenVergleichZeile kopf = zeilen[0];
            Assert.Equal("Wärmepumpe", kopf.Gewerk);
            Assert.Equal(AbweichungsErmittler.MERKMAL_ANZAHL, kopf.Merkmal);
            Assert.Equal(new[] { "2", "2" }, kopf.Zellen);

            // Jede Zeile trägt genau eine Zelle je Version (Stamm + eine Variante).
            foreach (KomponentenVergleichZeile z in zeilen) Assert.Equal(2, z.Zellen.Count);

            // Die Komponentenzeilen tragen die Bezeichner der verbauten Geräte.
            Assert.Equal("", zeilen[1].Gewerk);            // Gewerk nur in der ersten Zeile
            Assert.Equal("CS6800iAW MB + AW 10 OR-T", zeilen[1].Zellen[0]);
            Assert.Equal("CS6800iAW MB + AW 10 OR-T", zeilen[1].Zellen[1]);
            Assert.Equal("CS7800iLW 16", zeilen[2].Zellen[0]);

            // Der Spitzenkessel führt genau eine Komponente.
            KomponentenVergleichZeile kessel = zeilen.First(z => z.Gewerk == "Spitzenkessel");
            int i = zeilen.IndexOf(kessel);
            Assert.Equal(new[] { "1", "1" }, kessel.Zellen);
            Assert.Equal("ecoTEC plus VC 1206/5-5", zeilen[i + 1].Zellen[0]);
        }

        // =============================================================================
        //  V4 — der Kurztext trägt die Merkmale der Komponente, nicht ihren Bezeichner
        // =============================================================================
        [Fact]
        public void V4_Der_Kurztext_einer_Zelle_nennt_die_Merkmale_der_Komponente()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KomponentenVergleichZeile erste = Gegenueberstellung()[1];
            Assert.Equal(2, erste.Kurztexte.Count);
            Assert.Contains("Nennleistung:", erste.Kurztexte[0]);
            Assert.DoesNotContain("Komponente:", erste.Kurztexte[0]);
        }

        // =============================================================================
        //  V5 — ein Gewerk, das NUR die Variante führt, erscheint trotzdem
        // =============================================================================
        [Fact]
        public void V5_Ein_Gewerk_nur_in_der_Variante_erscheint_mit_nicht_vorhanden_beim_Stamm()
        {
            ProjektDetails stamm = Kunstbestand(("Wärmepumpe", new[] { "WP 1" }));
            ProjektDetails variante = Kunstbestand(("Wärmepumpe", new[] { "WP 1" }),
                                                   ("Photovoltaik", new[] { "Modul A" }));

            List<KomponentenVergleichZeile> zeilen =
                KomponentenVergleich.Gegenueberstellung(new[] { stamm, variante });

            KomponentenVergleichZeile pv = zeilen.First(z => z.Gewerk == "Photovoltaik");
            Assert.Equal(new[] { AbweichungsErmittler.BESTAND_FEHLT, "1" }, pv.Zellen);

            // Die Komponentenzeile darunter zeigt beim Stamm den Strich.
            KomponentenVergleichZeile komp = zeilen[zeilen.IndexOf(pv) + 1];
            Assert.Equal(KomponentenVergleich.OHNE_WERT, komp.Zellen[0]);
            Assert.Equal("Modul A", komp.Zellen[1]);
        }

        // =============================================================================
        //  V6 — ohne Komponenten keine Zeilen (weder leere Liste noch leeres Projekt)
        // =============================================================================
        [Fact]
        public void V6_Ohne_Erzeugerkomponenten_bleibt_die_Tabelle_leer()
        {
            Assert.Empty(KomponentenVergleich.Gegenueberstellung(new ProjektDetails[0]));
            Assert.Empty(KomponentenVergleich.Gegenueberstellung(null));
            Assert.Empty(KomponentenVergleich.Gegenueberstellung(new[] { Kunstbestand() }));
        }

        // =============================================================================
        //  V7 — die Zeilenbeschriftung steht in beiden Sprachen
        // =============================================================================

        /// <summary>
        /// „Komponente" ohne Nummer bei einer, „Komponente n" bei mehreren — und beides
        /// aus dem Ressourcenkatalog, nicht aus dem Quelltext. Der Vorläufer las die
        /// zwei Schlüssel mit einem deutschen Notbehelf; hier hängen sie an
        /// <c>MyResource.Resource</c> und wechseln mit der Sprache.
        /// </summary>
        [Fact]
        public void V7_Die_Komponentenbeschriftung_steht_in_beiden_Sprachen()
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            CultureInfo katalogVorher = Resource.Culture;
            try
            {
                ProjektDetails einer = Kunstbestand(("Wärmepumpe", new[] { "WP 1" }));
                ProjektDetails zwei = Kunstbestand(("Wärmepumpe", new[] { "WP 1", "WP 2" }));

                Sprache("de-DE");
                Assert.Equal("Komponente", Zweite(einer));
                Assert.Equal("Komponente 1", Zweite(zwei));

                Sprache("en-US");
                Assert.Equal("Component", Zweite(einer));
                Assert.Equal("Component 1", Zweite(zwei));
            }
            finally
            {
                Resource.Culture = katalogVorher;
                Thread.CurrentThread.CurrentUICulture = vorher;
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        /// <summary>Das Merkmal der ersten Komponentenzeile eines Bestands.</summary>
        private static string Zweite(ProjektDetails d)
            => KomponentenVergleich.Gegenueberstellung(new[] { d })[1].Merkmal;

        private static void Sprache(string kuerzel)
        {
            var k = new CultureInfo(kuerzel);
            Thread.CurrentThread.CurrentUICulture = k;
            CultureInfo.CurrentUICulture = k;
            Resource.Culture = k;
        }

        // =============================================================================
        //  Hilfsmittel
        // =============================================================================

        /// <summary>Die Gegenüberstellung Stamm ↔ Variante des Bildschirmfotos.</summary>
        private static List<KomponentenVergleichZeile> Gegenueberstellung()
        {
            var versionen = new[] { ProjektDetails.Lade(STAMM), ProjektDetails.Lade(VARIANTE) };
            return KomponentenVergleich.Gegenueberstellung(versionen);
        }

        /// <summary>
        /// Ein Projektstand OHNE Datenbank: je Gewerk die Bezeichner seiner
        /// Komponenten. Die Felder von <see cref="ProjektDetails"/> sind öffentlich,
        /// deshalb braucht diese Probe keine Arbeitskopie.
        /// </summary>
        private static ProjektDetails Kunstbestand(params (string Gewerk, string[] Namen)[] bestand)
        {
            var d = new ProjektDetails { IdProjekt = 1 };
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                d.KomponentenAnzahl[g.Key] = 0;

            foreach ((string gewerk, string[] namen) in bestand)
            {
                var dt = new DataTable();
                dt.Columns.Add("Bezeichner", typeof(string));
                foreach (string n in namen) dt.Rows.Add(n);

                d.KomponentenAnzahl[gewerk] = namen.Length;
                if (namen.Length == 0) continue;
                d.Komponenten[gewerk] = dt.Rows[0];
                d.KomponentenAlle[gewerk] = dt;
            }
            return d;
        }
    }
}
