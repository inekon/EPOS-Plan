using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERBEFUND 11.09.2026 (#210): „Detaillierte Simulation → Reiter Stromspeicher:
    /// Der Abschnitt ‚Kennzahlen je Speicher‘ und die Einheitentabelle des Eingabestands
    /// <c>@Aktuell</c> zeigen EINE Einheit — das Projekt hat ZWEI Speicher."
    ///
    /// <para><b>Die Ursache.</b> Der Eingabestand einer NEU angelegten Flotte entstand in
    /// <see cref="SpeicherFlottenStudieCtrl.Vorbelegung"/> aus
    /// <c>StromspeicherSimCtrl.LeseParameter(projektId)</c> — und das ist EIN
    /// Parametersatz: die Anlagenzeile der AKTIVEN Variante (AP9b), im Rückfall die Summe
    /// über alle <c>SP_TYP</c>-Anlagen. Daraus wurde genau EINE
    /// <see cref="FlottenEinheit"/>. Jede weitere Speicheranlage des Projekts fiel still
    /// weg — und der Projektlauf fand sie danach nicht wieder, weil er den GESPEICHERTEN
    /// Stand rechnet.</para>
    ///
    /// <para><b>Was hier geprüft wird.</b> Eine Speicheranlage bleibt eine Einheit
    /// (Spezifikation Kapitel 11: „Eine bereits vorhandene Einzelanlage wird beim Laden als
    /// Flotte mit genau einer Einheit abgebildet"); zwei Anlagen werden zwei Einheiten —
    /// auch dann, wenn beide dasselbe Kataloggerät tragen und deshalb dieselbe Gerätezeile
    /// in <c>Tab_Stromspeicher</c> teilen (genau der Fall des Befundes). Die Referenzliste
    /// <c>REF_SP_TYP</c> bleibt draußen, und ein GESPEICHERTER Stand wird nie
    /// überschrieben.</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Wie in
    /// <see cref="SpeichervarianteSicherstellenTests"/>: Die Fälle legen den Zustand des
    /// Befundes in der ARBEITSKOPIE selbst an. Die Schlüssel liegen weit oberhalb des
    /// Bestands (Höchststände am 11.09.2026: Projekt 1046, Anlage 14938, Gerät 1017063,
    /// Variante 17).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpeicherFlottenAnlagenEinheitenTests
    {
        private const int PROJEKT = 210001;

        private const int A_SP1 = 210101;
        private const int A_SP2 = 210102;
        private const int A_REF = 210103;

        private const int G_SP1 = 210201;
        private const int G_SP2 = 210202;

        // =====================================================================
        // Aufbau
        // =====================================================================

        private static void ProjektAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" +
                PROJEKT + ", '#210 Zwei Speicher')");
        }

        private static void SpeicherGeraet(int id, string bezeichner, double energie, double leistung)
        {
            Sql("INSERT INTO Tab_Stromspeicher (ID, ID_Projekt, Bezeichner, Leistung, Energie, Wirkungsgrad_RT) " +
                "VALUES (" + id + ", " + PROJEKT + ", '" + bezeichner + "', " +
                Z(leistung) + ", " + Z(energie) + ", " + Z(0.9) + ")");
        }

        /// <summary>Die sieben Geräte-Verweisspalten der Anlagenzeile — ihr Vorgabewert 0
        /// wäre ein Fremdschlüssel auf ein Gerät, das es nicht gibt (Begründung wortgleich
        /// in <see cref="SpeichervarianteSicherstellenTests"/>).</summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void Anlage(int id, string bezeichner, int typ, int geraet)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = id + ", " + PROJEKT + ", '" + bezeichner + "', " + typ;

            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (s == "ID_SP" ? geraet.ToString(CultureInfo.InvariantCulture) : "NULL");
            }

            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        private static void Speicheranlage(int idAnlage, string bezeichner, int geraet)
            => Anlage(idAnlage, bezeichner, WizardItemClass.SP_TYP, geraet);

        private static FlottenStudieKonfiguration Flotte()
            => SpeicherFlottenStudieCtrl.Vorbelegung(PROJEKT, 0.0).Eingaben.Auslegung.Flotte;

        // =====================================================================
        // 1 — Der Befund
        // =====================================================================

        /// <summary>
        /// <b>DER BEFUND SELBST.</b> Zwei Speicheranlagen desselben Kataloggeräts — der
        /// Wizard legt dafür EINE Gerätezeile und ZWEI Anlagenzeilen an
        /// (<c>StromspeicherCtrl.CopyFromStamm</c> prüft auf Dubletten). Die neu angelegte
        /// Flotte muss beide führen; vor #210 blieb die der aktiven Variante allein übrig.
        /// </summary>
        [Fact]
        public void Zwei_Anlagen_desselben_Geraets_werden_zwei_Einheiten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "WIT-M+APX ESS", 129.0, 100.0);
            Speicheranlage(A_SP1, "Speicher Halle", G_SP1);
            Speicheranlage(A_SP2, "Speicher Verwaltung", G_SP1);
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));

            FlottenStudieKonfiguration f = Flotte();

            Assert.Equal(2, f.Einheiten.Count);
            Assert.Equal(2, f.Einheiten.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(new[] { "Speicher Halle", "Speicher Verwaltung" },
                         f.Einheiten.Select(x => x.Name).ToArray());
            Assert.Equal(new[] { A_SP1.ToString(CultureInfo.InvariantCulture),
                                 A_SP2.ToString(CultureInfo.InvariantCulture) },
                         f.Einheiten.Select(x => x.AnlageId).ToArray());
            Assert.All(f.Einheiten, e => Assert.Equal(129.0, e.KapazitaetKWh, 6));
            Assert.All(f.Einheiten, e => Assert.Equal(100.0, e.LadeleistungKw, 6));
        }

        /// <summary>
        /// Zwei Anlagen mit VERSCHIEDENEN Geräten: Jede Einheit trägt die Daten IHRER
        /// Anlagenzeile, nicht den Summen- oder den Bezugssatz der aktiven Variante.
        /// </summary>
        [Fact]
        public void Jede_Einheit_traegt_die_Daten_ihrer_eigenen_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher gross", 129.0, 100.0);
            SpeicherGeraet(G_SP2, "Speicher klein", 30.0, 15.0);
            Speicheranlage(A_SP1, "Speicher gross", G_SP1);
            Speicheranlage(A_SP2, "Speicher klein", G_SP2);
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));

            List<FlottenEinheit> e = Flotte().Einheiten;

            Assert.Equal(2, e.Count);
            Assert.Equal(129.0, e[0].KapazitaetKWh, 6);
            Assert.Equal(100.0, e[0].EntladeleistungKw, 6);
            Assert.Equal(30.0, e[1].KapazitaetKWh, 6);
            Assert.Equal(15.0, e[1].EntladeleistungKw, 6);
        }

        /// <summary>
        /// <b>Die Zusage der Spezifikation (Kapitel 11) bleibt.</b> Eine Einzelanlage wird
        /// zu genau EINER Einheit — mit ihrem Anlagennamen und ihrem Anlagenbezug.
        /// </summary>
        [Fact]
        public void Eine_Speicheranlage_bleibt_genau_eine_Einheit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher A", 50.0, 25.0);
            Speicheranlage(A_SP1, "Speicher A", G_SP1);
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));

            FlottenEinheit e = Assert.Single(Flotte().Einheiten);
            Assert.Equal("Speicher A", e.Name);
            Assert.Equal(A_SP1.ToString(CultureInfo.InvariantCulture), e.AnlageId);
            Assert.Equal(50.0, e.KapazitaetKWh, 6);
        }

        /// <summary>
        /// <b>Die Referenzliste bleibt draußen.</b> <c>REF_SP_TYP</c> führt den
        /// VERGLEICHSFALL des Projekts, keine gleichzeitig betriebene Anlage — dieselbe
        /// Grenze, die <c>LeseParameter</c> und <c>AktiveVarianteSicherstellen</c> ziehen.
        /// </summary>
        [Fact]
        public void Eine_Referenzanlage_wird_keine_Einheit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher A", 50.0, 25.0);
            SpeicherGeraet(G_SP2, "Vergleichsspeicher", 80.0, 40.0);
            Speicheranlage(A_SP1, "Speicher A", G_SP1);
            Anlage(A_REF, "Vergleichsspeicher", WizardItemClass.REF_SP_TYP, G_SP2);
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));

            FlottenEinheit e = Assert.Single(Flotte().Einheiten);
            Assert.Equal("Speicher A", e.Name);
        }

        /// <summary>
        /// <b>Ein GESPEICHERTER Stand wird nicht überschrieben.</b> Die Vorbelegung greift
        /// ausschließlich beim Anlegen — wer seine Flotte im Editor auf eine Einheit
        /// zurückgesetzt hat, bekommt die zweite Anlage nicht bei jedem Öffnen zurück.
        /// </summary>
        [Fact]
        public void Ein_gespeicherter_Stand_bleibt_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher A", 50.0, 25.0);
            SpeicherGeraet(G_SP2, "Speicher B", 30.0, 15.0);
            Speicheranlage(A_SP1, "Speicher A", G_SP1);
            Speicheranlage(A_SP2, "Speicher B", G_SP2);
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));

            SpeicherOptimierungEingaben stand = SpeicherFlottenStudieCtrl
                .Vorbelegung(PROJEKT, 0.0).Eingaben.Kopie();
            stand.Auslegung.Flotte.Einheiten.RemoveAt(1);
            SpeicherAuslegungCtrl.Speichern(PROJEKT, SpeicherAuslegungCtrl.Anlage(PROJEKT),
                SpeicherAuslegungCtrl.AktuellerStand, stand);

            Assert.Single(Flotte().Einheiten);
        }

        // =====================================================================
        // 2 — Die Kennzahlen je Speicher
        // =====================================================================

        /// <summary>
        /// <b>Was der Anwender im Abschnitt „Kennzahlen je Speicher" sieht.</b> Zwei
        /// Einheiten mit demselben Anlagenbezug und demselben Namen — verschieden ist nur
        /// die <c>Id</c> — ergeben ZWEI Kennzahlzeilen. Kein Wörterbuch, kein
        /// <c>Distinct</c> und kein <c>GroupBy</c> auf dem Rechenweg legt sie zusammen
        /// (Hypothese (b) des Auftrags #210 — sie trifft nicht zu).
        /// </summary>
        [Fact]
        public void Zwei_gleichnamige_Einheiten_ergeben_zwei_Kennzahlzeilen()
        {
            FlottenStudieKonfiguration config = new FlottenStudieKonfiguration();
            config.Einheiten.Add(Einheit("e1", "Speicher", "4711"));
            config.Einheiten.Add(Einheit("e2", "Speicher", "4711"));
            config.Optionen.Betriebsziel = FlottenBetriebsziel.PvGreedy;
            config.Optionen.EnergieAusgleichEuroProKWh = 0.3;

            FlottenStudienErgebnis studie = FlottenSimulator.Simuliere(Eingang(), config);

            Assert.Equal(2, studie.Variante.SpeicherKennzahlen.Count);
            Assert.Equal(new[] { "e1", "e2" },
                         studie.Variante.SpeicherKennzahlen.Select(x => x.SpeicherId).ToArray());
        }

        private static FlottenEinheit Einheit(string id, string name, string anlage)
            => new FlottenEinheit
            {
                Id = id, Name = name, AnlageId = anlage,
                KapazitaetKWh = 10.0, LadeleistungKw = 5.0, EntladeleistungKw = 5.0,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            };

        /// <summary>Vier Viertelstunden Last und PV — genug für einen Lauf, zu wenig für
        /// eine Aussage über Zahlenwerte.</summary>
        private static FlottenEingang Eingang()
        {
            DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var werte = new List<FlottenNetzintervall>();
            for (int i = 0; i < 4; i++)
                werte.Add(new FlottenNetzintervall
                {
                    Zeitstempel = start.AddMinutes(15 * i),
                    LastKw = 8.0,
                    PvKw = i < 2 ? 20.0 : 0.0,
                    BezugspreisEuroProKWh = 0.3
                });
            return new FlottenEingang { Istwerte = werte };
        }

        // =====================================================================
        // Werkzeug
        // =====================================================================

        private static string Z(double wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }
    }
}
