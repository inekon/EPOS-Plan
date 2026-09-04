using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die drei Proben der PROJEKTPFLEGE (iU9-W15a.0k) — Duplizieren, Verwaltungsfelder,
    /// Loeschen.
    ///
    /// <para><b>Sie sind wichtiger als P1-P5.</b> Der Transfercontroller wird mit dieser
    /// Welle nur VERSCHOBEN; die drei Wege hier werden NEU GEBAUT — der Loeschweg zieht
    /// aus <c>MenueCtrl</c> in den Kern, die Namenspruefung und die Verwaltungsfelder aus
    /// der Maske. Was eine Razor-Fassung an ihnen aendert, muss hier auffallen.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Probe</b> — alle drei schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektpflegeTests
    {
        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030).</summary>
        private const string PROJEKT = "Referenz BHKW-Kaskade (Regressionstest)";

        /// <summary>
        /// Das Projekt, an dem ALLE DREI Loeschvorarbeiten haengen: zwei Pufferspeicher mit
        /// Anlagenverweis, eine Berichtskonfiguration und zwei Varianten (1023, 1024).
        /// </summary>
        private const string MIT_ANHANG = "Wöhler";

        // =============================================================================
        //  P7 — Duplizieren
        // =============================================================================
        [Fact]
        public void P7_Die_Kopie_traegt_je_Tabelle_dieselbe_Zeilenzahl_wie_die_Quelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            var plan = new List<ProjektDuplizierenCtrl.Spec>(dup.ErmittlePlan());

            int quelle = dup.GetProjektId(PROJEKT);
            Assert.True(quelle > 0);

            var vorher = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in plan) vorher[s.Tabelle] = Zaehle(s, quelle);

            int neu = dup.Duplizieren(PROJEKT, "Kopie P7");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.NotEqual(quelle, neu);

            foreach (var s in plan)
                Assert.True(vorher[s.Tabelle] == Zaehle(s, neu),
                            s.Tabelle + ": Quelle " + vorher[s.Tabelle] + ", Kopie " + Zaehle(s, neu) + ".");

            // FreieProjektId (Befund 27.08.2026): Die neue Id war VORHER in JEDER
            // Projekttabelle frei - sonst erbte die Kopie den Rueckstand eines
            // geloeschten Projekts. Nachweisbar am Ergebnis: Nach dem Kopieren traegt
            // jede Tabelle genau die Zeilen der Quelle, nicht mehr. Zusaetzlich darf
            // die Quelle unveraendert dastehen.
            foreach (var s in plan)
                Assert.True(vorher[s.Tabelle] == Zaehle(s, quelle),
                            s.Tabelle + ": das Duplizieren hat die QUELLE veraendert.");
        }

        [Fact]
        public void P7b_Die_drei_Vorpruefungen_melden_ohne_zu_kopieren()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();

            Assert.Equal(DuplizierBefund.NamenLeer, dup.PruefeNamen("", "Neu"));
            Assert.Equal(DuplizierBefund.NamenLeer, dup.PruefeNamen(PROJEKT, "   "));
            Assert.Equal(DuplizierBefund.QuelleFehlt, dup.PruefeNamen("gibt es nicht", "Neu"));
            Assert.Equal(DuplizierBefund.ZielExistiert, dup.PruefeNamen(PROJEKT, MIT_ANHANG));
            Assert.Equal(DuplizierBefund.Ok, dup.PruefeNamen(PROJEKT, "So heisst keines"));

            // Befund W15a-B10: Die alte Praefix-Pruefung der Maske
            // (ListView.FindItemWithText) haette einen Namen abgelehnt, der nur der
            // ANFANG eines vorhandenen ist. Die richtige Pruefung laesst ihn zu.
            Assert.Equal(DuplizierBefund.Ok, dup.PruefeNamen(PROJEKT, "Wöhl"));
        }

        [Fact]
        public void P7c_Ein_Abbruch_laesst_keine_halbe_Kopie_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            using var abbruch = new System.Threading.CancellationTokenSource();
            abbruch.Cancel();

            int neu = dup.Duplizieren(PROJEKT, "Kopie P7c", null, abbruch.Token);

            Assert.Equal(-1, neu);
            Assert.Equal(0, dup.GetProjektId("Kopie P7c"));
        }

        // =============================================================================
        //  P8 — Verwaltungsfelder (der Befund aus c631053)
        // =============================================================================
        [Fact]
        public void P8_Die_Verwaltungsfelder_landen_auf_der_Kopie_ohne_Klimaregion_und_Erstelldatum_zu_ruehren()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            int neu = dup.Duplizieren(PROJEKT, "Kopie P8");
            Assert.True(neu > 0);

            var vorher = new ProjektCtrl();
            vorher.ReadSingle("Kopie P8");
            Assert.True(vorher.rows > 0);
            int klimaVorher = vorher.m_ID_Klimaregion;
            DateTime erstelltVorher = vorher.m_Erstelldatum;

            VerwaltungsfelderBefund befund = dup.VerwaltungsfelderSetzen(
                "Kopie P8", "Beschreibung P8", "Kunde P8", "Bearbeiter P8", out string fehlertext);

            Assert.Equal(VerwaltungsfelderBefund.Ok, befund);
            Assert.Equal("", fehlertext);

            var nachher = new ProjektCtrl();
            nachher.ReadSingle("Kopie P8");
            Assert.Equal("Beschreibung P8", nachher.m_szBeschreibung);
            Assert.Equal("Kunde P8", nachher.m_szKunde);
            Assert.Equal("Bearbeiter P8", nachher.m_szBearbeiter);

            // Der Befund aus c631053: Ein frisch gefuellter ProjektCtrl haette
            // ID_Klimaregion auf 0 und das Erstelldatum auf heute gesetzt.
            Assert.Equal(klimaVorher, nachher.m_ID_Klimaregion);
            Assert.Equal(erstelltVorher.Date, nachher.m_Erstelldatum.Date);
        }

        [Fact]
        public void P8b_Eine_nicht_vorhandene_Kopie_meldet_KopieFehlt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VerwaltungsfelderBefund befund = new ProjektDuplizierenCtrl()
                .VerwaltungsfelderSetzen("So heisst keines", "a", "b", "c", out string fehlertext);

            Assert.Equal(VerwaltungsfelderBefund.KopieFehlt, befund);
            Assert.Equal("", fehlertext);
        }

        // =============================================================================
        //  P9 — Loeschen mit Kaskade
        // =============================================================================
        [Fact]
        public void P9_Das_Loeschen_raeumt_Berichtskonfiguration_und_Variantenverknuepfung_mit_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ProjektCtrl.IdVonName(MIT_ANHANG);
            Assert.True(id > 0);

            // Vorbedingung: alle drei Vorarbeiten haben etwas zu tun.
            Assert.True(Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = " + id) > 0);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + id +
                             " OR ID_ProjektRef = " + id) > 0);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = " + id +
                             " AND ID_PUFFER IS NOT NULL AND ID_PUFFER <> 0") > 0);

            // Die Varianten selbst muessen das Loeschen ueberstehen.
            var varianten = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Projekt FROM Tab_Variante WHERE ID_ProjektRef = " + id);
            foreach (DataRow r in dt.Rows) varianten.Add(Convert.ToInt32(r[0]));
            Assert.NotEmpty(varianten);

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(id, MIT_ANHANG);

            Assert.Equal(LoeschStand.Geloescht, befund.Stand);
            Assert.Equal(MIT_ANHANG, befund.Projektname);
            Assert.Equal("", befund.Fehlertext);

            // Entscheid O-3: Der Name ist eindeutig - eine Zeile, keine Rueckfrage.
            Assert.Equal(1, befund.Anzahl);

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + id +
                                 " OR ID_ProjektRef = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = " + id));

            // Die Varianten werden wieder eigenstaendig - sie bleiben stehen.
            foreach (int v in varianten)
                Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + v));
        }

        [Fact]
        public void P9b_Ohne_Namen_wird_nichts_angefasst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Zahl("SELECT COUNT(*) FROM Tab_Projekt");

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(0, "");

            Assert.Equal(LoeschStand.NameLeer, befund.Stand);
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_Projekt"));
        }

        // =============================================================================
        //  P9c/P9d — ein Name, der MEHRERE Projekte trifft (Entscheid W15a-O-3)
        // =============================================================================

        /// <summary>
        /// Der Loeschweg laeuft ueber den NAMEN. Regulaer trifft der genau ein Projekt:
        /// <c>Tab_Projekt</c> traegt den eindeutigen Index <c>Projektname</c>. Ein
        /// ALTBESTAND ohne diesen Index kann zwei gleichnamige Projekte fuehren — dann
        /// darf der Weg nicht still beide mitnehmen (Anwenderentscheid vom 04.09.2026:
        /// „Projektname darf nicht gleich sein, daher löschen. Rückfragen in diesem
        /// Fall.").
        ///
        /// <para>Die Probe stellt genau diesen Altbestand auf der ARBEITSKOPIE her: Index
        /// weg, eine zweite Zeile desselben Namens dazu.</para>
        /// </summary>
        [Fact]
        public void P9c_Ein_mehrdeutiger_Name_meldet_die_Anzahl_und_loescht_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ProjektCtrl.IdVonName(MIT_ANHANG);
            Assert.True(id > 0);

            int zwilling = ZwillingAnlegen(id);
            Assert.True(zwilling > 0);
            Assert.Equal(2, ProjektCtrl.AnzahlGleicherNamen(MIT_ANHANG));

            // Vorbedingung: die Vorarbeiten haetten etwas zu tun.
            int berichte = Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = " + id);
            int varianten = Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + id +
                                 " OR ID_ProjektRef = " + id);
            Assert.True(berichte > 0);
            Assert.True(varianten > 0);

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(id, MIT_ANHANG);

            Assert.Equal(LoeschStand.Mehrdeutig, befund.Stand);
            Assert.Equal(MIT_ANHANG, befund.Projektname);
            Assert.Equal(2, befund.Anzahl);

            // NICHTS ist angefasst - beide Zeilen stehen, die Vorarbeiten sind nicht gelaufen.
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + id));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + zwilling));
            Assert.Equal(berichte, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = " + id));
            Assert.Equal(varianten, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + id +
                                         " OR ID_ProjektRef = " + id));
        }

        /// <summary>
        /// Mit der ausdruecklichen Freigabe laeuft derselbe Weg wie eh und je — ueber den
        /// NAMEN, also fuer ALLE Projekte dieses Namens, mit allen Vorarbeiten.
        /// </summary>
        [Fact]
        public void P9d_Mit_ausdruecklicher_Freigabe_fallen_alle_Projekte_des_Namens()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ProjektCtrl.IdVonName(MIT_ANHANG);
            Assert.True(id > 0);

            int zwilling = ZwillingAnlegen(id);
            Assert.True(zwilling > 0);

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(
                id, MIT_ANHANG, mehrdeutigZugelassen: true);

            Assert.Equal(LoeschStand.Geloescht, befund.Stand);
            Assert.Equal(2, befund.Anzahl);

            Assert.Equal(0, ProjektCtrl.AnzahlGleicherNamen(MIT_ANHANG));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + zwilling));

            // Die drei Vorarbeiten sind gelaufen.
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + id +
                                 " OR ID_ProjektRef = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = " + id));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = " + id));
        }

        [Fact]
        public void AnzahlGleicherNamen_zaehlt_und_bleibt_bei_Unfug_still()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(1, ProjektCtrl.AnzahlGleicherNamen(PROJEKT));
            Assert.Equal(0, ProjektCtrl.AnzahlGleicherNamen("So heisst keines"));
            Assert.Equal(0, ProjektCtrl.AnzahlGleicherNamen(""));
            Assert.Equal(0, ProjektCtrl.AnzahlGleicherNamen(null));

            // Wie IdVonName: der Name ist ein PARAMETER, kein verketteter Text.
            Assert.Equal(0, ProjektCtrl.AnzahlGleicherNamen("O'Brien GmbH"));
        }

        /// <summary>
        /// Legt auf der ARBEITSKOPIE ein zweites Projekt mit dem Namen von
        /// <paramref name="quelle"/> an — den Altbestand ohne den eindeutigen Index.
        /// Gibt dessen Id zurueck.
        /// </summary>
        private static int ZwillingAnlegen(int quelle)
        {
            // Der eindeutige Index stammt aus der SQLite-Migration; ohne ihn waere die
            // zweite Zeile gar nicht anzulegen.
            DataRepository.ExecuteNonQuery("DROP INDEX IF EXISTS Projektname");

            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Projekt (Projektname, Bearbeiter, Beschreibung, Kunde, " +
                "Aenderungsdatum, ID_Klimaregion, Erstelldatum) " +
                "SELECT Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, " +
                "ID_Klimaregion, Erstelldatum FROM Tab_Projekt WHERE ID = ?",
                new DbParam("@id", quelle));

            return Zahl("SELECT MAX(ID) FROM Tab_Projekt");
        }

        // =============================================================================
        //  Die Projektliste (iU9-W15a.0a)
        // =============================================================================
        [Fact]
        public void Die_Namensliste_traegt_jedes_Projekt_mit_Kunde_Beschreibung_und_Datum()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<ProjektKopfZeile> liste = ProjektCtrl.NamenListe();

            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Projekt"), liste.Count);
            Assert.All(liste, z => Assert.True(z.Id > 0));
            Assert.All(liste, z => Assert.False(string.IsNullOrEmpty(z.Name)));
            Assert.All(liste, z => Assert.NotNull(z.Kunde));
            Assert.All(liste, z => Assert.NotNull(z.Beschreibung));
            Assert.Contains(liste, z => z.Name == PROJEKT);

            // Sortiert wie ReadAll: nach Projektname.
            for (int i = 1; i < liste.Count; i++)
                Assert.True(string.CompareOrdinal(liste[i - 1].Name, liste[i].Name) <= 0
                            || StringComparer.CurrentCultureIgnoreCase.Compare(liste[i - 1].Name, liste[i].Name) <= 0,
                            "Die Liste ist nicht nach Namen sortiert: " + liste[i - 1].Name + " / " + liste[i].Name);
        }

        [Fact]
        public void IdVonName_findet_das_Projekt_und_bleibt_bei_Unfug_still()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ProjektCtrl.IdVonName(PROJEKT) > 0);
            Assert.Equal(0, ProjektCtrl.IdVonName(""));
            Assert.Equal(0, ProjektCtrl.IdVonName(null));

            // Befund W15a-B1: Der Vorlaeufer verkettete den Namen in das WHERE und brach
            // an einem Apostroph. Hier ist er ein Parameter - die Abfrage laeuft und
            // findet eben nichts.
            Assert.Equal(0, ProjektCtrl.IdVonName("O'Brien GmbH"));
        }

        // =============================================================================
        //  Die Klimaregion der Assistentenseite (iU9-W15a.0f/0g)
        // =============================================================================
        [Fact]
        public void Der_Projektkopf_traegt_die_neun_Felder_der_Assistentenseite()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektKopfDaten kopf = ProjektCtrl.Kopf(PROJEKT);

            Assert.NotNull(kopf);
            Assert.Equal(PROJEKT, kopf.Name);
            Assert.NotNull(kopf.Beschreibung);
            Assert.NotNull(kopf.Kunde);
            Assert.NotNull(kopf.Bearbeiter);
            Assert.True(kopf.IdKlimaregion > 0);
            Assert.False(string.IsNullOrEmpty(kopf.Klimaname),
                         "Der Regionsname wurde weder als Projektkopie noch ueber den STAMM-Rueckfall gefunden.");
            Assert.True(kopf.NameAenderbar);

            // Leerer Name = der Neu-Zweig: ein leerer Satz mit heutigem Datum.
            ProjektKopfDaten leer = ProjektCtrl.Kopf("");
            Assert.NotNull(leer);
            Assert.Equal("", leer.Name);
            Assert.Equal(DateTime.Now.Date, leer.Erstelldatum.Date);

            // Ein geratener Name liefert null - der Aufrufer bleibt stehen.
            Assert.Null(ProjektCtrl.Kopf("So heisst keines"));
        }

        [Fact]
        public void Die_Klimaregion_wird_ueber_die_Projektkopie_und_notfalls_ueber_den_STAMM_gefunden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(PROJEKT);
            Assert.True(ctrl.rows > 0);

            string name = KlimaregionStammCtrl.NameZuProjektregion(ctrl.m_ID_Klimaregion, ctrl.m_ID);
            Assert.False(string.IsNullOrEmpty(name));

            // 0 heisst „keine Region" und darf nicht in die Datenbank greifen.
            Assert.Equal("", KlimaregionStammCtrl.NameZuProjektregion(0, ctrl.m_ID));

            // Der Weg zurueck: der Stammname liefert wieder eine Stamm-Id.
            var stamm = new KlimaregionStammCtrl();
            stamm.ReadAll();
            Assert.True(stamm.rows > 0);
            string ersterName = stamm.items[0].m_szName;
            Assert.True(KlimaregionStammCtrl.IdVonName(ersterName) > 0);
            Assert.Equal(0, KlimaregionStammCtrl.IdVonName("O'Brien Region"));
            Assert.Equal(0, KlimaregionStammCtrl.IdVonName(""));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private static int Zaehle(ProjektDuplizierenCtrl.Spec spec, int projektId)
            => Zahl("SELECT COUNT(*) FROM [" + spec.Tabelle + "] WHERE " +
                    string.Format(spec.Filter, projektId));

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }
    }
}
