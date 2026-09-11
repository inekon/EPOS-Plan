using System;
using System.Globalization;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

public sealed class SpeicherFlottenCsvImportTests
{
    [Fact]
    public void Vorbelegung_Erkennt_Flotten_und_Prognoseheader()
    {
        byte[] csv = Utf8("timestamp,load_kw,pv_kw,bhkw_kw,buy_eur_kwh,pv_sell_eur_kwh,bhkw_sell_eur_kwh,battery_sell_eur_kwh,snapshot_id,known_at,decision_at\n");

        SpeicherFlottenCsvOptionen o = SpeicherFlottenCsvImport.Vorbelegung(csv, true);

        Assert.Equal(',', o.Zeitbasis.Trennzeichen);
        Assert.Equal('.', o.Zeitbasis.Dezimaltrenner);
        Assert.Equal(0, o.Zeitbasis.ZeitstempelSpalte);
        Assert.Equal(1, o.LastSpalte);
        Assert.Equal(2, o.PvSpalte);
        Assert.Equal(3, o.BhkwSpalte);
        Assert.Equal(4, o.BezugSpalte);
        Assert.Equal(5, o.PvPreisSpalte);
        Assert.Equal(6, o.BhkwPreisSpalte);
        Assert.Equal(7, o.BatteriePreisSpalte);
        Assert.Equal(8, o.SnapshotIdSpalte);
        Assert.Equal(9, o.BekanntSeitSpalte);
        Assert.Equal(10, o.EntscheidungSpalte);
        Assert.Equal(SpeicherZeitreihenEinheit.Kilowatt, o.LeistungEinheit);
        Assert.Equal(SpeicherZeitreihenEinheit.EuroJeKilowattstunde, o.PreisEinheit);
    }

    [Fact]
    public void Jahresdaten_Liest_Zwei_Stundenjahre_und_rechnet_Einheiten_um()
    {
        var b = new StringBuilder("timestamp;load_kw;buy_eur_kwh\n");
        DateTimeOffset start = new(2025, 12, 31, 23, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < 8760 + 8760; i++)
            b.Append(start.AddHours(i).ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
                .Append(";0,002;-10\n");
        SpeicherFlottenCsvOptionen o = SpeicherFlottenCsvImport.Vorbelegung(Utf8(b.ToString()), false);
        o.LeistungEinheit = SpeicherZeitreihenEinheit.Megawatt;
        o.PreisEinheit = SpeicherZeitreihenEinheit.EuroJeMegawattstunde;

        var jahre = SpeicherFlottenCsvImport.JahresdatenLesen(
            new SpeicherImportDatei { Dateiname = "projektjahre.csv", Inhalt = Utf8(b.ToString()) }, o);

        Assert.Equal(new[] { 2026, 2027 }, jahre.Select(x => x.Jahr));
        Assert.All(jahre, x => Assert.Equal(35040, x.Istwerte.Count));
        Assert.All(jahre, x => Assert.True(x.IstVollstaendigesJahr));
        Assert.Equal(2, jahre[0].Istwerte[0].LastKw, 10);
        Assert.Equal(-.01, jahre[0].Istwerte[0].BezugspreisEuroProKWh, 10);
        Assert.Equal(0, jahre[0].Istwerte[0].PvKw);
        Assert.Equal(0, jahre[0].Istwerte[0].BatterieVerkaufspreisEuroProKWh);
    }

    [Fact]
    public void Prognosen_Gruppiert_Snapshots_expandiert_Stunden_und_erlaubt_negativePreise()
    {
        string csv =
            "timestamp;load_kw;pv_kw;buy_eur_kwh;pv_sell_eur_kwh;snapshot_id;known_at;decision_at\n" +
            "2026-01-01T00:00Z;4;1;-0,05;-0,02;A;2025-12-31T23:00Z;2026-01-01T00:00Z\n" +
            "2026-01-01T01:00Z;5;2;0,10;0,03;A;2025-12-31T23:00Z;2026-01-01T00:00Z\n" +
            "2026-01-02T00:00Z;6;3;0,20;0,04;A;2026-01-01T20:00Z;2026-01-02T00:00Z\n" +
            "2026-01-02T01:00Z;7;4;0,30;0,05;A;2026-01-01T20:00Z;2026-01-02T00:00Z\n";
        byte[] inhalt = Utf8(csv);
        SpeicherFlottenCsvOptionen o = SpeicherFlottenCsvImport.Vorbelegung(inhalt, true);

        var snapshots = SpeicherFlottenCsvImport.PrognosenLesen(
            new SpeicherImportDatei { Dateiname = "prognosen.csv", Inhalt = inhalt }, o);

        Assert.Equal(2, snapshots.Count);
        Assert.All(snapshots, x => Assert.Equal(8, x.Intervalle.Count));
        Assert.All(snapshots, x => Assert.Equal(SpeicherEngine.PrognoseArt.VerifiziertBekannt, x.Art));
        Assert.Equal(-.05, snapshots[0].Intervalle[0].BezugspreisEuroProKWh, 10);
        Assert.Equal(-.02, snapshots[0].Intervalle[0].PvVerkaufspreisEuroProKWh, 10);
        Assert.Equal(snapshots[0].Entscheidungszeitpunkt, snapshots[0].Intervalle[0].Zeitstempel);
    }

    [Fact]
    public void Prognosen_Lehnen_Zukunftsleck_und_Luecke_ab()
    {
        string zukunft =
            "timestamp;load_kw;buy_eur_kwh;snapshot_id;known_at;decision_at\n" +
            "2026-01-01T00:00Z;1;0,2;A;2026-01-01T00:15Z;2026-01-01T00:00Z\n" +
            "2026-01-01T00:15Z;1;0,2;A;2026-01-01T00:15Z;2026-01-01T00:00Z\n";
        byte[] zukunftBytes = Utf8(zukunft);
        SpeicherFlottenCsvOptionen zukunftOptionen = SpeicherFlottenCsvImport.Vorbelegung(zukunftBytes, true);
        Assert.Contains("noch nicht bekannt", Assert.Throws<FormatException>(() =>
            SpeicherFlottenCsvImport.PrognosenLesen(new SpeicherImportDatei { Inhalt = zukunftBytes }, zukunftOptionen)).Message);

        string luecke =
            "timestamp;load_kw;buy_eur_kwh;snapshot_id;known_at;decision_at\n" +
            "2026-01-01T00:00Z;1;0,2;A;2025-12-31T23:00Z;2026-01-01T00:00Z\n" +
            "2026-01-01T00:30Z;1;0,2;A;2025-12-31T23:00Z;2026-01-01T00:00Z\n";
        byte[] lueckeBytes = Utf8(luecke);
        SpeicherFlottenCsvOptionen lueckeOptionen = SpeicherFlottenCsvImport.Vorbelegung(lueckeBytes, true);
        Assert.Throws<FormatException>(() => SpeicherFlottenCsvImport.PrognosenLesen(
            new SpeicherImportDatei { Inhalt = lueckeBytes }, lueckeOptionen));
    }

    [Fact]
    public void Json_Wird_klar_abgewiesen()
    {
        byte[] json = Utf8("[{\"Jahr\":2026}]");
        FormatException ex = Assert.Throws<FormatException>(() =>
            SpeicherFlottenCsvImport.Vorbelegung(json, false));
        Assert.Contains("JSON-Import wird nicht unterstützt", ex.Message);

        ex = Assert.Throws<FormatException>(() => SpeicherFlottenStudieCtrl.JahresdatenLesen(
            new SpeicherImportDatei { Dateiname = "jahre.json", Inhalt = json }));
        Assert.Contains("JSON-Import wird nicht unterstützt", ex.Message);
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
