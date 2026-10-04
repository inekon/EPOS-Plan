using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Anwenderbefund 04.10.2026: Nach Import und Öffnen eines Projekts mit Stromspeicher warf
    /// der ERSTE Simulationsstart <c>InvalidOperationException</c> „Der Strombedarf ist nicht
    /// gerechnet …" aus <see cref="StromspeicherSimCtrl.BaueLastreihe"/>; der zweite Lauf ging.
    /// </summary>
    /// <remarks>
    /// <para><b>Der Aufrufpfad.</b> <c>SimulationErgebnisHuelle.Laufen</c> holt vor dem Lauf
    /// <c>OptimierungVorgaben()</c> → <c>StromspeicherAuslegungCtrl.Vorgaben()</c> →
    /// <see cref="SpeicherFlottenStudieCtrl.Vorbelegung"/> mit dem Lauf der Hülle. Vor dem
    /// ersten Lauf ist das ihr Feld <c>sim = new SimulationControl()</c> — nicht <c>null</c>,
    /// aber ohne Strombedarf. Ohne gespeicherte Flotte geht die Vorbelegung über
    /// <c>BetriebsvorgabenSetzen</c> → <c>PeakZielVorschlag</c> → <c>BaueLastreihe</c>, und
    /// die Wache wirft. Ab dem zweiten Lauf hält die Hülle den gelaufenen Stand, der Weg ist
    /// still.</para>
    /// <para>Die Ausnahme war gefangen (benannter Rückfall), hielt aber jeden Debugger an. Der
    /// Weg prüft jetzt VOR dem Aufruf, ob ein Lastgang gerechnet ist; die Wache selbst bleibt.</para>
    /// </remarks>
    [Collection("Testdatenbank")]
    public class SpeicherVorbelegungOhneLaufTests
    {
        private const int PROJEKT = 698001;
        private const int ANLAGE = 698101;
        private const int GERAET = 698201;

        private const string WACHE = "Der Strombedarf ist nicht gerechnet";

        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void ProjektMitSpeicherAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT + ", 'Befund Erstlauf Speicher')");
            Sql("INSERT INTO Tab_Stromspeicher (ID, ID_Projekt, Bezeichner, Leistung, Energie, Wirkungsgrad_RT) " +
                "VALUES (" + GERAET + ", " + PROJEKT + ", 'Speicher 1', 50, 100, " + Z(0.9) + ")");

            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = ANLAGE + ", " + PROJEKT + ", 'Speicher 1', " + WizardItemClass.SP_TYP;
            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (s == "ID_SP" ? GERAET.ToString(CultureInfo.InvariantCulture) : "NULL");
            }
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));
        }

        /// <summary>
        /// <b>Der Befund.</b> Die Vorbelegung mit dem frischen Lauf der Hülle (Zustand vor dem
        /// ersten Simulationslauf) läuft die Wache in <c>BaueLastreihe</c> nicht an und liefert
        /// denselben benannten Rückfall wie ohne Lauf.
        /// </summary>
        [Fact]
        public void Vorbelegung_vor_dem_ersten_Lauf_laeuft_die_Wache_nicht_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektMitSpeicherAnlegen();
            var ohneLauf = SpeicherFlottenStudieCtrl.Vorbelegung(PROJEKT, 0.0, null);

            int wachtreffer = 0;
            EventHandler<FirstChanceExceptionEventArgs> zaehler = (_, e) =>
            {
                if (e.Exception is InvalidOperationException &&
                    e.Exception.Message.StartsWith(WACHE, StringComparison.Ordinal))
                    wachtreffer++;
            };
            AppDomain.CurrentDomain.FirstChanceException += zaehler;
            SpeicherOptimierungVorgaben frisch;
            try
            {
                frisch = SpeicherFlottenStudieCtrl.Vorbelegung(PROJEKT, 0.0, new SimulationControl());
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= zaehler;
            }

            Assert.Equal(0, wachtreffer);
            FlottenStudieKonfiguration f = frisch.Eingaben.Auslegung.Flotte;
            Assert.Single(f.Einheiten);
            Assert.Equal(ohneLauf.Eingaben.Auslegung.Flotte.Optionen.WirtschaftlicherPeakZielwertKw,
                         f.Optionen.WirtschaftlicherPeakZielwertKw);
        }

        /// <summary>Die Wache in <c>BaueLastreihe</c> bleibt: Wer sie direkt ruft, bekommt sie.</summary>
        [Fact]
        public void Die_Wache_in_BaueLastreihe_bleibt()
        {
            var fehler = Assert.Throws<InvalidOperationException>(
                () => new StromspeicherSimCtrl().BaueLastreihe(new SimulationControl()));
            Assert.StartsWith(WACHE, fehler.Message, StringComparison.Ordinal);
            Assert.False(SpeicherFlottenStudieCtrl.LastgangGerechnet(new SimulationControl()));
            Assert.False(SpeicherFlottenStudieCtrl.LastgangGerechnet(null));
        }

        private static string Z(double wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }
    }
}
