using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests;

/// <summary>
/// Der MESSLAUF der Vorprüfung in Station 4 „Optimierung" (Auftrag #254).
///
/// <para><b>Wozu.</b> Die Ansicht ruft die Vorprüfung bei jeder Änderung eines
/// Suchraumfelds. Was ein solcher Aufruf kostet und wohin die Zeit geht, ist keine
/// Meinungsfrage — dieser Fall misst es gegen die Testdatenbank, Projekt 1046, im
/// EPOS-Weg mit einem gerechneten Jahreslauf. Er ist mit
/// <c>[Trait("Kategorie","Messung")]</c> gekennzeichnet und lässt sich damit
/// ausfiltern (<c>--filter "Kategorie!=Messung"</c>).</para>
///
/// <para><b>Er prüft KEINE Zeitschranke.</b> Eine Zeitgrenze wäre auf fremden Läufern
/// flatterhaft; die Wache über die eingesparte Arbeit ist der deterministische Zähler
/// <c>StromspeicherAuslegungCtrl.Vorbereitungen</c> in
/// <see cref="StromspeicherAuslegungCtrlTests"/>. Hier stehen die Zahlen im
/// Prüfbericht und sonst nirgends.</para>
/// </summary>
[Collection("Testdatenbank")]
[Trait("Kategorie", "Messung")]
public sealed class VorpruefungMessungTests
{
    /// <summary>Das Prüfprojekt der Speicherflotte (Basis R7, SP‑O‑8).</summary>
    private const int Pruefprojekt = 1046;

    /// <summary>Aufrufe je Messreihe nach dem Aufwärmen.</summary>
    private const int Laeufe = 100;

    /// <summary>Aufwärmläufe vor der Messung — sie zählen nicht mit.</summary>
    private const int Aufwaermen = 3;

    private readonly ITestOutputHelper _aus;

    public VorpruefungMessungTests(ITestOutputHelper ausgabe) => _aus = ausgabe;

    /// <summary>
    /// Misst <c>Vorpruefen</c> bei je einer Änderung eines Suchraumfelds und schlüsselt
    /// den Aufwand nach Vorbereitung, Eingangsaufbau und Prüfung auf.
    /// </summary>
    [Fact]
    public void Vorpruefung_je_Suchraumfeld_messen()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Messlauf erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        string fehler = ctrl.SimulationslaufVorbereiten(
            new SimulationWaermebedarf(), new SimulationStrombedarf(), out SimulationControl lauf);
        Assert.True(fehler == null, "Der Simulationslauf ließ sich nicht vorbereiten: " + fehler);
        Assert.Null(ctrl.SimulationslaufRechnen(lauf, null, CancellationToken.None));
        ctrl.LaufUebernehmen(lauf);

        SpeicherOptimierungEingaben eingaben = Messstand(ctrl);
        FlottenAuslegungsAchse achse = eingaben.Auslegung.Flotte.Auslegung.Achsen[0];

        // --- Aufwaermen (JIT, Verbindungsaufbau, Dateicache) ----------------
        for (int i = 0; i < Aufwaermen; i++)
        {
            achse.KapazitaetSchrittKWh = 10.0 + i;
            ctrl.Vorpruefen(eingaben);
        }

        // --- Die Messreihe: je Aufruf EIN geaendertes Suchraumfeld ----------
        var zugriffe = new Zaehlzugriff(DataRepository.Zugriff);
        DataRepository.Zugriff = zugriffe;
        double[] zeiten = new double[Laeufe];
        try
        {
            for (int i = 0; i < Laeufe; i++)
            {
                achse.KapazitaetSchrittKWh = 10.0 + (i % 20);
                var uhr = Stopwatch.StartNew();
                ctrl.Vorpruefen(eingaben);
                uhr.Stop();
                zeiten[i] = uhr.Elapsed.TotalMilliseconds;
            }
        }
        finally { DataRepository.Zugriff = zugriffe.Innen; }

        // --- Die Aufschluesselung: Vorbereiten / Eingang / Pruefe -----------
        double vorbereiten = 0, eingang = 0, pruefen = 0;
        const int Teilproben = 10;
        for (int i = 0; i < Teilproben; i++)
        {
            achse.KapazitaetSchrittKWh = 10.0 + i;

            var uhr = Stopwatch.StartNew();
            StromspeicherOptimierungVorbereitung v =
                SpeicherAuslegungCtrl.Vorbereiten(ctrl.Lauf, Pruefprojekt, eingaben.Kopie());
            uhr.Stop();
            vorbereiten += uhr.Elapsed.TotalMilliseconds;
            Assert.NotNull(v);

            uhr.Restart();
            FlottenEingang e = SpeicherFlottenStudieCtrl.Eingang(v);
            FlottenStudieKonfiguration k = SpeicherFlottenStudieCtrl.Konfiguration(v.Eingaben);
            uhr.Stop();
            eingang += uhr.Elapsed.TotalMilliseconds;

            uhr.Restart();
            FlottenPlausibilitaet.Pruefe(e, k, v.Eingaben.Auslegung.VerwendeteKosten);
            uhr.Stop();
            pruefen += uhr.Elapsed.TotalMilliseconds;
        }

        CultureInfo k0 = CultureInfo.InvariantCulture;
        double[] sortiert = (double[])zeiten.Clone();
        Array.Sort(sortiert);
        _aus.WriteLine("Messlauf Vorpruefung, Projekt " + Pruefprojekt + ", EPOS-Weg, " +
                       Laeufe.ToString(k0) + " Aufrufe je ein geaendertes Suchraumfeld");
        _aus.WriteLine("  Mittel       : " + zeiten.Average().ToString("0.000", k0) + " ms");
        _aus.WriteLine("  Median       : " + Median(sortiert).ToString("0.000", k0) + " ms");
        _aus.WriteLine("  Kleinster    : " + sortiert[0].ToString("0.000", k0) + " ms");
        _aus.WriteLine("  Groesster    : " + sortiert[^1].ToString("0.000", k0) + " ms");
        _aus.WriteLine("  Summe        : " + zeiten.Sum().ToString("0.0", k0) + " ms");
        _aus.WriteLine("  DB-Vorgaenge : " + zugriffe.Gesamt.ToString(k0) + " gesamt, " +
                       (zugriffe.Gesamt / (double)Laeufe).ToString("0.00", k0) + " je Aufruf");
        _aus.WriteLine("Aufschluesselung (" + Teilproben.ToString(k0) + " Teilproben, Mittel je Aufruf):");
        _aus.WriteLine("  Vorbereiten  : " + (vorbereiten / Teilproben).ToString("0.000", k0) + " ms");
        _aus.WriteLine("  Eingang+Konf : " + (eingang / Teilproben).ToString("0.000", k0) + " ms");
        _aus.WriteLine("  Pruefe       : " + (pruefen / Teilproben).ToString("0.000", k0) + " ms");

        // Die einzige Zusicherung des Falls ist eine FACHLICHE: Dieselben Eingaben
        // liefern dieselben Hinweise - die Messung darf das Ergebnis nicht verschieben.
        achse.KapazitaetSchrittKWh = 25.0;
        IReadOnlyList<FlottenHinweis> a = ctrl.Vorpruefen(eingaben);
        IReadOnlyList<FlottenHinweis> b = ctrl.Vorpruefen(eingaben);
        Assert.Equal(a.Select(x => x.Kennung), b.Select(x => x.Kennung));
        Assert.Equal(a.Select(x => x.Text), b.Select(x => x.Text));
    }

    private static double Median(double[] sortiert) =>
        sortiert.Length % 2 == 1
            ? sortiert[sortiert.Length / 2]
            : 0.5 * (sortiert[sortiert.Length / 2 - 1] + sortiert[sortiert.Length / 2]);

    /// <summary>
    /// Der Arbeitsstand des Messlaufs: die Flotte des Prüfprojekts, EPOS-Quellen und
    /// EINE Suchachse, an der das gemessene Feld hängt.
    /// </summary>
    private static SpeicherOptimierungEingaben Messstand(StromspeicherAuslegungCtrl ctrl)
    {
        SpeicherOptimierungEingaben eingaben = ctrl.Vorgaben().Eingaben.Kopie();
        SpeicherAuslegungKonfiguration a = eingaben.Auslegung ??= new SpeicherAuslegungKonfiguration();

        a.FlotteImProjektAktiv = false;
        a.Lastquelle = SpeicherAuslegungQuelle.Epos;
        a.PvQuelle = SpeicherAuslegungQuelle.Epos;
        a.Preisquelle = SpeicherAuslegungQuelle.Epos;
        a.LastDatei = null;
        a.PvDatei = null;
        a.PreisDatei = null;
        a.FlottenProjektjahre = new List<FlottenProjektjahr>();
        a.Investitionsquelle = SpeicherKostenQuelle.Dialog;
        a.Betriebsquelle = SpeicherKostenQuelle.Dialog;
        a.DirekteKosten = new SpeicherKostensaetze
        {
            InvestEurProKwh = 300.0,
            InvestEurProKw = 200.0,
            InvestVorhanden = true,
            BetriebEurProKwJahr = 5.0,
            BetriebVorhanden = true
        };
        a.VerwendeteKosten = new SpeicherKostensaetze();

        FlottenStudieKonfiguration flotte = a.Flotte ??= new FlottenStudieKonfiguration();
        flotte.Optionen.EnergieAusgleichEuroProKWh ??= 0.0;
        flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = 1;
        flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen = true;

        a.FlottenGroessenOptimieren = true;
        flotte.Auslegung.Suchmethode = FlottenSuchmethode.Groesse;
        flotte.Auslegung.Achsen.Clear();
        FlottenEinheit vorlage = flotte.Einheiten.Count > 0
            ? SpeicherAuslegungKopie.Von(flotte.Einheiten[0])
            : new FlottenEinheit { Id = "E1", KapazitaetKWh = 100, LadeleistungKw = 50, EntladeleistungKw = 50 };
        flotte.Auslegung.Achsen.Add(new FlottenAuslegungsAchse
        {
            Aktiv = true,
            Vorlage = vorlage,
            ErsetztEinheitId = vorlage.Id,
            Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
            AnzahlVon = 1,
            AnzahlBis = 1,
            KapazitaetVonKWh = 100,
            KapazitaetBisKWh = 200,
            KapazitaetSchrittKWh = 50,
            LeistungVonKw = 50,
            LeistungBisKw = 100,
            LeistungSchrittKw = 50
        });
        return eingaben;
    }

    /// <summary>
    /// Ein zählender Mantel um die Zugriffsschicht — er reicht jeden Aufruf durch und
    /// notiert nur, wie viele es waren.
    /// </summary>
    private sealed class Zaehlzugriff : IDatenzugriff
    {
        public Zaehlzugriff(IDatenzugriff innen) => Innen = innen;

        public IDatenzugriff Innen { get; }

        public int Gesamt { get; private set; }

        private void Zaehle() => Gesamt++;

        public DataTable GetDataTable(string sql, params DbParam[] parameter)
        { Zaehle(); return Innen.GetDataTable(sql, parameter); }

        public bool ExecuteSQL(string sql, params DbParam[] parameter)
        { Zaehle(); return Innen.ExecuteSQL(sql, parameter); }

        public int ExecuteNonQuery(string sql, params DbParam[] parameter)
        { Zaehle(); return Innen.ExecuteNonQuery(sql, parameter); }

        public int ExecuteInsertAndGetId(string insertSql, DbParam[] parameter)
        { Zaehle(); return Innen.ExecuteInsertAndGetId(insertSql, parameter); }

        public object ExecuteScalar(string sql, params DbParam[] parameter)
        { Zaehle(); return Innen.ExecuteScalar(sql, parameter); }

        public DbVorgang Vorgang()
        { Zaehle(); return Innen.Vorgang(); }

        public bool TabelleVorhanden(string name)
        { Zaehle(); return Innen.TabelleVorhanden(name); }

        public bool SpalteVorhanden(string tabelle, string spalte)
        { Zaehle(); return Innen.SpalteVorhanden(tabelle, spalte); }

        public List<string> SpaltenVonTabelle(string tabelle)
        { Zaehle(); return Innen.SpaltenVonTabelle(tabelle); }

        public DataTable IndexListe(string tabelle)
        { Zaehle(); return Innen.IndexListe(tabelle); }

        public DataTable FremdschluesselListe(string tabelle)
        { Zaehle(); return Innen.FremdschluesselListe(tabelle); }

        public bool DatenbankVorhanden() => Innen.DatenbankVorhanden();

        public string DatenbankPfad => Innen.DatenbankPfad;
    }
}
