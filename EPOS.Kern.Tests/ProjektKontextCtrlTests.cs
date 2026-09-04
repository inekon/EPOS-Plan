using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>NACHWEIS N7 — der Projektkontext</b> (iU9-W16b.0, K2 der Vermessung).
    ///
    /// <para><b>Warum es diese Fälle gibt.</b> „Welches Projekt ist offen" hing bis
    /// hierher an einem FELD der Startmaske (<c>Form_Start.m_ID_Projekt</c>, Befund
    /// W16-B6); <c>FormStartProjektKontext</c> reichte nur durch. Damit war der
    /// Projektwechsel — die Stelle, an der Risiko R-W16-4 sitzt („ein falsch
    /// umgehängter Kontext schreibt in das FALSCHE Projekt") — allein am
    /// Windows-Gerät prüfbar. Seit <see cref="ProjektKontextCtrl"/> im Kern liegt,
    /// steht er hier.</para>
    ///
    /// <para><b>Der Aufbau folgt der Hausregel:</b> zuerst die Fälle ohne Datenbank,
    /// dann die lesenden auf der geteilten Arbeitskopie der Klasse, zuletzt die
    /// SCHREIBENDEN (<c>Tab_Applikation</c>) auf einer EIGENEN Kopie je Probe — sie
    /// dürfen die Vergleichsbasis der lesenden nicht verschieben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektKontextCtrlTests : IClassFixture<TestDatenbank>
    {
        private const int ID_1030 = 1030;
        private const string NAME_1030 = "Referenz BHKW-Kaskade (Regressionstest)";
        private const string KLIMA_1030 = "München";

        private const int ID_1007 = 1007;
        private const string NAME_1007 = "Laurentiuskirche";
        private const string KLIMA_1007 = "stuttgart";

        private readonly TestDatenbank _db;

        public ProjektKontextCtrlTests(TestDatenbank db)
        {
            _db = db;
        }

        // =========================================================================
        // Ohne Datenbank
        // =========================================================================

        /// <summary>
        /// Der frische Kontext ist „kein Projekt" — und meldet trotzdem
        /// <c>Vorhanden</c>. Das ist die Aussage der Schnittstelle: Es GIBT einen
        /// führenden Kontext, er ist nur leer. Erst ohne Träger (dann steht
        /// <c>LeererProjektKontext</c> in <c>Dienste.Projekt</c>) dürfen Aufrufer
        /// ersatzweise <c>Tab_Applikation</c> lesen.
        /// </summary>
        [Fact]
        public void Am_Anfang_ist_kein_Projekt_offen()
        {
            ProjektKontextCtrl k = new ProjektKontextCtrl();

            Assert.True(k.Vorhanden);
            Assert.Equal(0, k.Id);
            Assert.Equal("", k.Name);
            Assert.Equal("", k.Klimazone);
        }

        /// <summary>
        /// Ein leerer Name und keine Id: nichts geschieht, und es wird nichts
        /// gemeldet — wörtlich <c>Form_Start.ProjektKontextUebernehmen</c> :174.
        /// </summary>
        [Fact]
        public void Ohne_Namen_und_ohne_Id_wird_nichts_uebernommen()
        {
            ProjektKontextCtrl k = new ProjektKontextCtrl();
            int gemeldet = 0;
            k.Gewechselt += () => gemeldet++;

            Assert.False(k.Setzen(""));
            Assert.False(k.Setzen(null));
            Assert.False(k.Uebernehmen(0, ""));
            Assert.False(k.Uebernehmen(0, null));

            Assert.Equal(0, gemeldet);
            Assert.Equal(0, k.Id);
        }

        /// <summary>
        /// <c>Leeren</c> auf einem leeren Kontext meldet nichts — ein Neuzeichnen ohne
        /// Anlass wäre auf der Startseite sichtbar (dieselbe Zurückhaltung wie
        /// <c>SeitenZustand.ProjektSetzen</c>).
        /// </summary>
        [Fact]
        public void Leeren_ohne_offenes_Projekt_meldet_nichts()
        {
            ProjektKontextCtrl k = new ProjektKontextCtrl();
            int gemeldet = 0;
            k.Gewechselt += () => gemeldet++;

            k.Leeren();

            Assert.Equal(0, gemeldet);
        }

        // =========================================================================
        // Lesend - geteilte Arbeitskopie
        // =========================================================================

        /// <summary>
        /// Der Kern liefert Id, Name UND Klimazone — die drei Werte, die
        /// <c>IProjektKontext</c> zusagt und die bisher aus drei verschiedenen
        /// Feldern der Startmaske kamen.
        /// </summary>
        [Fact]
        public void Setzen_liefert_Id_Name_und_Klimazone()
        {
            if (!_db.Vorhanden) return;

            ProjektKontextCtrl k = new ProjektKontextCtrl();

            Assert.True(k.Setzen(NAME_1030));
            Assert.Equal(ID_1030, k.Id);
            Assert.Equal(NAME_1030, k.Name);
            Assert.Equal(KLIMA_1030, k.Klimazone);
        }

        /// <summary>
        /// <b>Der eigentliche N7-Fall.</b> Ein Wechsel meldet sich, und danach steht
        /// der NEUE Stand vollständig — Id, Name und Klimazone zugleich. Genau hier
        /// saß Risiko R-W16-4: Blieb einer der drei Werte auf dem vorherigen Projekt
        /// stehen, schrieben die Kacheln der Startseite anschließend in das falsche
        /// Projekt (das ist der historische „Befund 3" der Startmaske).
        /// </summary>
        [Fact]
        public void Ein_Wechsel_zieht_alle_drei_Werte_nach_und_meldet_sich()
        {
            if (!_db.Vorhanden) return;

            ProjektKontextCtrl k = new ProjektKontextCtrl();
            List<(int Id, string Name, string Klima)> gemeldet = new List<(int, string, string)>();
            k.Gewechselt += () => gemeldet.Add((k.Id, k.Name, k.Klimazone));

            Assert.True(k.Setzen(NAME_1030));
            Assert.True(k.Setzen(NAME_1007));

            Assert.Equal(2, gemeldet.Count);

            // Beim MELDEN steht der neue Stand bereits - der Empfaenger (die
            // Startseite) liest ihn im Ereignis.
            Assert.Equal((ID_1030, NAME_1030, KLIMA_1030), gemeldet[0]);
            Assert.Equal((ID_1007, NAME_1007, KLIMA_1007), gemeldet[1]);

            Assert.Equal(ID_1007, k.Id);
            Assert.Equal(NAME_1007, k.Name);
            Assert.Equal(KLIMA_1007, k.Klimazone);
        }

        /// <summary>
        /// Ein Name ohne Projekt (zwischenzeitlich gelöscht) lässt den bisherigen
        /// Kontext STEHEN und meldet <c>false</c> — der Aufrufer erkennt daran, dass er
        /// keine Erfolgsmeldung zeigen darf.
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Name_laesst_den_Kontext_stehen()
        {
            if (!_db.Vorhanden) return;

            ProjektKontextCtrl k = new ProjektKontextCtrl();
            Assert.True(k.Setzen(NAME_1030));

            int gemeldet = 0;
            k.Gewechselt += () => gemeldet++;

            Assert.False(k.Setzen("Ein Projekt, das es nicht gibt"));

            Assert.Equal(0, gemeldet);
            Assert.Equal(ID_1030, k.Id);
            Assert.Equal(NAME_1030, k.Name);
            Assert.Equal(KLIMA_1030, k.Klimazone);
        }

        /// <summary>
        /// Der NAME ist der führende Schlüssel, die Id nur der Rückfall: Ohne Namen
        /// wird er zur Id nachgeschlagen (<c>FormStartProjektKontext</c> :70-77).
        /// </summary>
        [Fact]
        public void Ohne_Namen_wird_er_zur_Id_nachgeschlagen()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                ProjektKontextCtrl k = new ProjektKontextCtrl();

                Assert.True(k.Uebernehmen(ID_1007, ""));
                Assert.Equal(NAME_1007, k.Name);
                Assert.Equal(ID_1007, k.Id);
            }
        }

        /// <summary>
        /// <c>Leeren</c> setzt auf „kein Projekt" zurück und meldet es — der Zustand
        /// nach dem Löschen des gerade offenen Projekts
        /// (<c>Form_Start.pBox_Delete_Click</c>).
        /// </summary>
        [Fact]
        public void Leeren_setzt_auf_kein_Projekt_zurueck()
        {
            if (!_db.Vorhanden) return;

            ProjektKontextCtrl k = new ProjektKontextCtrl();
            Assert.True(k.Setzen(NAME_1030));

            int gemeldet = 0;
            k.Gewechselt += () => gemeldet++;

            k.Leeren();

            Assert.Equal(1, gemeldet);
            Assert.Equal(0, k.Id);
            Assert.Equal("", k.Name);
            Assert.Equal("", k.Klimazone);
        }

        // =========================================================================
        // Schreibend - EIGENE Arbeitskopie je Probe
        // =========================================================================

        /// <summary>
        /// <c>Uebernehmen</c> schreibt <c>Tab_Applikation</c> fort — die Quelle der
        /// Kachel „Zuletzt geöffnet". Das sind die vier Zeilen aus
        /// <c>Form_Start.ZuletztGeoeffnetMerken</c> (:793-798).
        /// </summary>
        [Fact]
        public void Uebernehmen_schreibt_zuletzt_geoeffnet_fort()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                ProjektKontextCtrl k = new ProjektKontextCtrl();

                Assert.True(k.Uebernehmen(0, NAME_1030));
                Assert.Equal((NAME_1030, ID_1030), ProjektKontextCtrl.ZuletztGeoeffnet());

                Assert.True(k.Uebernehmen(0, NAME_1007));
                Assert.Equal((NAME_1007, ID_1007), ProjektKontextCtrl.ZuletztGeoeffnet());
            }
        }

        /// <summary>
        /// <c>Setzen</c> schreibt <c>Tab_Applikation</c> ausdrücklich NICHT — der
        /// Unterschied, den der Bestand macht: Der Variantenwechsel im Kopfband und
        /// die Menüwege „Neu"/„Bearbeiten" merken sich nichts, die drei Projektkacheln
        /// schon.
        /// </summary>
        [Fact]
        public void Setzen_merkt_sich_nichts()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                (string Name, int Id) vorher = ProjektKontextCtrl.ZuletztGeoeffnet();

                ProjektKontextCtrl k = new ProjektKontextCtrl();
                Assert.True(k.Setzen(NAME_1030));

                Assert.Equal(vorher, ProjektKontextCtrl.ZuletztGeoeffnet());
            }
        }

        /// <summary>
        /// <b>Der Projektwechsel-Nachweis zu Risiko R-W16-4</b> (Vermessung § 16.3):
        /// Ein Wechsel 1030 → 1007 → 1030 lässt BEIDE Projekte inhaltlich stehen.
        ///
        /// <para><b>Warum es ihn gibt.</b> Bis W16b hing „welches Projekt ist offen"
        /// an einem Feld der Startmaske. Blieb es beim Wechsel auf dem vorherigen
        /// Projekt stehen, schrieben die Kacheln anschließend in das FALSCHE Projekt —
        /// das ist der historische „Befund 3" der Startmaske und das Risiko, das die
        /// ganze Teilwelle trägt. Der Referenzlauf sieht davon nichts: Er RECHNET
        /// einen bestehenden Stand nach, er wechselt kein Projekt.</para>
        ///
        /// <para>Verglichen wird der INHALT beider Projekte: Zählstand der sieben
        /// Zuordnungstabellen, die Bitmaske des Kerns und die Anlagenbezeichner —
        /// dieselben drei Maße wie im Speicherweg-Nachweis des Assistenten
        /// (<c>AssistentCtrlTests</c>, R-W16-6).</para>
        ///
        /// <para>Auf einer EIGENEN Arbeitskopie: <c>Uebernehmen</c> schreibt
        /// <c>Tab_Applikation</c> fort.</para>
        /// </summary>
        [Fact]
        public void Ein_Projektwechsel_schreibt_in_kein_falsches_Projekt()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                IProjektKontext vorher = Dienste.Projekt;
                try
                {
                    ProjektKontextCtrl k = new ProjektKontextCtrl();
                    Dienste.Projekt = k;

                    Dictionary<string, int> stand1030 = Zaehlstand(ID_1030);
                    Dictionary<string, int> stand1007 = Zaehlstand(ID_1007);
                    int maske1030 = KomponentenBestandCtrl.Lesen(ID_1030).Bitmaske;
                    int maske1007 = KomponentenBestandCtrl.Lesen(ID_1007).Bitmaske;
                    string[] anlagen1030 = Anlagen(ID_1030);
                    string[] anlagen1007 = Anlagen(ID_1007);

                    // 1030 oeffnen - rechnen wuerde die Startseite hier.
                    Assert.True(k.Uebernehmen(0, NAME_1030));
                    Assert.Equal(ID_1030, Dienste.Projekt.Id);
                    Assert.Equal(KLIMA_1030, Dienste.Projekt.Klimazone);
                    Assert.Equal((NAME_1030, ID_1030), ProjektKontextCtrl.ZuletztGeoeffnet());

                    // Auf 1007 wechseln.
                    Assert.True(k.Uebernehmen(0, NAME_1007));
                    Assert.Equal(ID_1007, Dienste.Projekt.Id);
                    Assert.Equal(KLIMA_1007, Dienste.Projekt.Klimazone);
                    Assert.Equal((NAME_1007, ID_1007), ProjektKontextCtrl.ZuletztGeoeffnet());

                    // Und zurueck.
                    Assert.True(k.Uebernehmen(0, NAME_1030));
                    Assert.Equal(ID_1030, Dienste.Projekt.Id);
                    Assert.Equal(KLIMA_1030, Dienste.Projekt.Klimazone);

                    // BEIDE Projekte stehen inhaltlich unveraendert.
                    Assert.Equal(stand1030, Zaehlstand(ID_1030));
                    Assert.Equal(stand1007, Zaehlstand(ID_1007));
                    Assert.Equal(maske1030, KomponentenBestandCtrl.Lesen(ID_1030).Bitmaske);
                    Assert.Equal(maske1007, KomponentenBestandCtrl.Lesen(ID_1007).Bitmaske);
                    Assert.Equal(anlagen1030, Anlagen(ID_1030));
                    Assert.Equal(anlagen1007, Anlagen(ID_1007));

                    // Und die Bitmasken der beiden sind verschieden - sonst waere der
                    // Vergleich oben wertlos.
                    Assert.NotEqual(maske1030, maske1007);
                }
                finally { Dienste.Projekt = vorher; }
            }
        }

        /// <summary>Die sieben Zuordnungstabellen eines Projekts, je Zählstand.</summary>
        private static readonly string[] TABELLEN =
        {
            "Z_ProjektGebaeude", "Z_ProjektWaermebedarf", "Z_Projekt_Prozesswaerme",
            "Z_Projekt_Brauchwasser", "Z_Projekt_Stromverbraucher",
            "Z_ProjektStromganglinie", "Tab_Energieanlagen"
        };

        private static Dictionary<string, int> Zaehlstand(int idProjekt)
        {
            Dictionary<string, int> stand = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string t in TABELLEN)
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + t + " WHERE ID_Projekt = ?",
                    new DbParam("@id", idProjekt));
                stand[t] = v == null ? 0 : Convert.ToInt32(v);
            }
            return stand;
        }

        private static string[] Anlagen(int idProjekt)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            ctrl.ReadAllFilter("ID_Projekt=" + idProjekt);

            List<string> namen = new List<string>();
            for (int i = 0; i < ctrl.rows; i++)
                namen.Add(ctrl.items[i].ID_Type + ":" + (ctrl.items[i].Bezeichner ?? ""));
            namen.Sort(StringComparer.Ordinal);
            return namen.ToArray();
        }

        /// <summary>
        /// Ein gescheitertes <c>Uebernehmen</c> fasst <c>Tab_Applikation</c> nicht an —
        /// sonst zeigte die Kachel „Zuletzt geöffnet" auf ein Projekt, das nie geöffnet
        /// wurde.
        /// </summary>
        [Fact]
        public void Ein_gescheitertes_Uebernehmen_merkt_sich_nichts()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                (string Name, int Id) vorher = ProjektKontextCtrl.ZuletztGeoeffnet();

                ProjektKontextCtrl k = new ProjektKontextCtrl();
                Assert.False(k.Uebernehmen(0, "Ein Projekt, das es nicht gibt"));

                Assert.Equal(vorher, ProjektKontextCtrl.ZuletztGeoeffnet());
            }
        }
    }
}
