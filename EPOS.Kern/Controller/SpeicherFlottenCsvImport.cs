using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>Explizite Spalten-, Zeit- und Einheitenzuordnung einer Flotten-CSV.</summary>
public sealed class SpeicherFlottenCsvOptionen
{
    public SpeicherZeitreihenOptionen Zeitbasis { get; set; } = new();
    public SpeicherZeitreihenEinheit LeistungEinheit { get; set; } = SpeicherZeitreihenEinheit.Kilowatt;
    public SpeicherZeitreihenEinheit PreisEinheit { get; set; } = SpeicherZeitreihenEinheit.EuroJeKilowattstunde;
    public int LastSpalte { get; set; } = 1;
    public int PvSpalte { get; set; } = -1;
    public int BhkwSpalte { get; set; } = -1;
    public int BezugSpalte { get; set; } = 2;
    public int PvPreisSpalte { get; set; } = -1;
    public int BhkwPreisSpalte { get; set; } = -1;
    public int BatteriePreisSpalte { get; set; } = -1;
    public int SnapshotIdSpalte { get; set; } = 3;
    public int BekanntSeitSpalte { get; set; } = 4;
    public int EntscheidungSpalte { get; set; } = 5;
}

/// <summary>
/// CSV-Import für vollständige Projektjahre und nachweislich bekannte
/// Prognose-Snapshots. Interne Profilpersistenz bleibt davon unberührt.
/// </summary>
public static class SpeicherFlottenCsvImport
{
    private sealed class Rohzeile
    {
        public DateTimeOffset Zeit;
        public double Last;
        public double Pv;
        public double Bhkw;
        public double Bezug;
        public double PvPreis;
        public double BhkwPreis;
        public double BatteriePreis;
        public string SnapshotId = "";
        public DateTimeOffset BekanntSeit;
        public DateTimeOffset Entscheidung;
    }

    /// <summary>Erkennt Trennzeichen, Kodierung und gebräuchliche Spaltennamen.</summary>
    public static SpeicherFlottenCsvOptionen Vorbelegung(byte[] inhalt, bool prognosen)
    {
        PruefeKeinJson(inhalt, null);
        SpeicherZeitreihenOptionen zeitbasis = BesteVorschau(inhalt, out string[] kopf);
        var o = new SpeicherFlottenCsvOptionen { Zeitbasis = zeitbasis };
        if (kopf == null || kopf.Length == 0) return o;

        string[] h = kopf.Select(NormalisiereKopf).ToArray();
        int zeitstempel = Finde(h, "zeitstempel", "timestamp", "datetime", "dateandtime", "time");
        if (zeitstempel >= 0) o.Zeitbasis.ZeitstempelSpalte = zeitstempel;
        int datum = Finde(h, "datum", "date");
        int uhrzeit = Finde(h, "uhrzeit", "timeofday");
        if (zeitstempel < 0 && datum >= 0 && uhrzeit >= 0)
        {
            o.Zeitbasis.ZeitstempelSpalte = -1;
            o.Zeitbasis.DatumSpalte = datum;
            o.Zeitbasis.UhrzeitSpalte = uhrzeit;
        }

        o.LastSpalte = Waehle(o.LastSpalte, Finde(h, "lastkw", "last", "loadkw", "load", "strombedarfkw", "strombedarf"));
        o.PvSpalte = Finde(h, "pvkw", "pv", "photovoltaikkw", "photovoltaik");
        o.BhkwSpalte = Finde(h, "bhkwkw", "bhkw", "chpkw", "chp");
        o.BezugSpalte = Waehle(o.BezugSpalte, Finde(h, "buyeurkwh", "bezugeurkwh", "bezugspreiseurkwh", "bezugspreis", "buyprice", "price"));
        o.PvPreisSpalte = Finde(h, "pvselleurkwh", "pvpreiseurkwh", "pvverkaufspreis", "pvprice");
        o.BhkwPreisSpalte = Finde(h, "bhkwselleurkwh", "bhkwpreiseurkwh", "bhkwverkaufspreis", "chpprice");
        o.BatteriePreisSpalte = Finde(h, "batteryselleurkwh", "batteriepreiseurkwh", "batterieverkaufspreis", "batteryprice");
        if (prognosen)
        {
            o.SnapshotIdSpalte = Waehle(o.SnapshotIdSpalte, Finde(h, "snapshotid", "prognoseid", "forecastid"));
            o.BekanntSeitSpalte = Waehle(o.BekanntSeitSpalte, Finde(h, "bekanntseit", "knownsince", "knownat", "issuedat"));
            o.EntscheidungSpalte = Waehle(o.EntscheidungSpalte, Finde(h, "entscheidung", "entscheidungszeitpunkt", "decision", "decisiontime", "decisionat"));
        }
        o.Zeitbasis.WertSpalte = o.LastSpalte;
        return o;
    }

    public static List<FlottenProjektjahr> JahresdatenLesen(
        SpeicherImportDatei datei, SpeicherFlottenCsvOptionen optionen)
    {
        List<Rohzeile> roh = Lesen(datei, optionen, false);
        Normalisiere(roh, optionen, out DateTimeOffset[] zeit, out double[] last,
            out double[] pv, out double[] bhkw, out double[] bezug,
            out double[] pvPreis, out double[] bhkwPreis, out double[] batteriePreis);

        TimeZoneInfo zone = SpeicherZeitreihenImport.FindeZeitzone(optionen.Zeitbasis.ZeitzoneId);
        var gruppen = Enumerable.Range(0, zeit.Length)
            .GroupBy(i => TimeZoneInfo.ConvertTime(zeit[i], zone).Year)
            .OrderBy(g => g.Key).ToArray();
        if (gruppen.Length == 0) throw new FormatException("Die CSV-Datei enthält keine Jahresdaten.");
        for (int i = 1; i < gruppen.Length; i++)
            if (gruppen[i].Key != gruppen[i - 1].Key + 1)
                throw new FormatException($"Die Projektjahre müssen lückenlos sein; nach {gruppen[i - 1].Key} fehlt das Jahr {gruppen[i - 1].Key + 1}.");

        var ergebnis = new List<FlottenProjektjahr>(gruppen.Length);
        foreach (var gruppe in gruppen)
        {
            List<FlottenNetzintervall> intervalle = gruppe.Select(i => Intervall(
                zeit[i], last[i], pv[i], bhkw[i], bezug[i], pvPreis[i], bhkwPreis[i], batteriePreis[i])).ToList();
            SpeicherFlottenStudieCtrl.PruefeGanzesJahr(intervalle);
            ergebnis.Add(new FlottenProjektjahr
            {
                Jahr = gruppe.Key,
                Istwerte = intervalle,
                IstVollstaendigesJahr = true
            });
        }
        return ergebnis;
    }

    public static List<FlottenPrognoseSnapshot> PrognosenLesen(
        SpeicherImportDatei datei, SpeicherFlottenCsvOptionen optionen)
    {
        List<Rohzeile> roh = Lesen(datei, optionen, true);
        var ergebnis = new List<FlottenPrognoseSnapshot>();
        foreach (var gruppe in roh.GroupBy(x =>
                     (x.SnapshotId, x.BekanntSeit, x.Entscheidung))
                 .OrderBy(g => g.Key.Entscheidung))
        {
            Rohzeile erster = gruppe.First();
            if (erster.BekanntSeit > erster.Entscheidung)
                throw new FormatException($"Snapshot '{erster.SnapshotId}' war zum Entscheidungszeitpunkt noch nicht bekannt.");

            List<Rohzeile> zeilen = gruppe.ToList();
            Normalisiere(zeilen, optionen, out DateTimeOffset[] zeit, out double[] last,
                out double[] pv, out double[] bhkw, out double[] bezug,
                out double[] pvPreis, out double[] bhkwPreis, out double[] batteriePreis);
            if (zeit.Length == 0 || zeit[0] != erster.Entscheidung)
                throw new FormatException($"Snapshot '{erster.SnapshotId}' muss mit seinem Entscheidungszeitpunkt beginnen.");
            var intervalle = new List<FlottenNetzintervall>(zeit.Length);
            for (int i = 0; i < zeit.Length; i++)
                intervalle.Add(Intervall(zeit[i], last[i], pv[i], bhkw[i], bezug[i],
                    pvPreis[i], bhkwPreis[i], batteriePreis[i]));
            ergebnis.Add(new FlottenPrognoseSnapshot(erster.SnapshotId, erster.BekanntSeit,
                erster.Entscheidung, PrognoseArt.VerifiziertBekannt, intervalle));
        }
        if (ergebnis.Count == 0) throw new FormatException("Die CSV-Datei enthält keine Prognose-Snapshots.");
        return ergebnis;
    }

    private static List<Rohzeile> Lesen(SpeicherImportDatei datei,
        SpeicherFlottenCsvOptionen o, bool prognosen)
    {
        if (datei?.Inhalt == null || datei.Inhalt.Length == 0)
            throw new ArgumentException(prognosen ? "Die Prognosedatei ist leer." : "Die Jahresdatendatei ist leer.");
        ArgumentNullException.ThrowIfNull(o);
        ArgumentNullException.ThrowIfNull(o.Zeitbasis);
        PruefeKeinJson(datei.Inhalt, datei.Dateiname);
        PruefeOptionen(o, prognosen);

        SpeicherZeitreihenOptionen basis = Kopiere(o.Zeitbasis);
        basis.Rolle = SpeicherZeitreihenRolle.Last;
        basis.Einheit = o.LeistungEinheit;
        basis.WertSpalte = o.LastSpalte;
        SpeicherZeitreihenImport.PruefeGrundangaben(datei.Inhalt, basis, true);
        List<string[]> csv = SpeicherZeitreihenImport.LeseCsv(datei.Inhalt, basis,
            SpeicherZeitreihenImport.MaximaleDatensaetze + basis.ZuUeberspringendeZeilen +
            (basis.Kopfzeile ? 1 : 0) + 1);
        int beginn = basis.ZuUeberspringendeZeilen + (basis.Kopfzeile ? 1 : 0);
        if (csv.Count <= beginn) throw new FormatException("Die CSV-Datei enthält keine Datensätze.");
        if (csv.Count - beginn > SpeicherZeitreihenImport.MaximaleDatensaetze)
            throw new FormatException("Die CSV-Datei enthält zu viele Datensätze.");

        var ergebnis = new List<Rohzeile>(csv.Count - beginn);
        TimeZoneInfo zone = null, zoneBekannt = null, zoneEntscheidung = null;
        SpeicherZeitreihenOptionen bekannt = Metazeit(basis, o.BekanntSeitSpalte);
        SpeicherZeitreihenOptionen entscheidung = Metazeit(basis, o.EntscheidungSpalte);
        for (int i = beginn; i < csv.Count; i++)
        {
            string[] f = csv[i];
            int nr = i + 1;
            PruefeSpalten(f, o, prognosen, nr);
            var x = new Rohzeile
            {
                Zeit = SpeicherZeitreihenImport.ParseZeit(f, basis, ref zone, nr),
                Last = Zahl(f, o.LastSpalte, o.Zeitbasis.Dezimaltrenner, nr),
                Pv = ZahlOptional(f, o.PvSpalte, o.Zeitbasis.Dezimaltrenner, nr),
                Bhkw = ZahlOptional(f, o.BhkwSpalte, o.Zeitbasis.Dezimaltrenner, nr),
                Bezug = Zahl(f, o.BezugSpalte, o.Zeitbasis.Dezimaltrenner, nr),
                PvPreis = ZahlOptional(f, o.PvPreisSpalte, o.Zeitbasis.Dezimaltrenner, nr),
                BhkwPreis = ZahlOptional(f, o.BhkwPreisSpalte, o.Zeitbasis.Dezimaltrenner, nr),
                BatteriePreis = ZahlOptional(f, o.BatteriePreisSpalte, o.Zeitbasis.Dezimaltrenner, nr)
            };
            if (x.Last < 0 || x.Pv < 0 || x.Bhkw < 0)
                throw new FormatException($"Zeile {nr}: Last, PV und BHKW dürfen nicht negativ sein.");
            if (prognosen)
            {
                x.SnapshotId = (f[o.SnapshotIdSpalte] ?? "").Trim();
                if (x.SnapshotId.Length == 0) throw new FormatException($"Zeile {nr}: Die Snapshot-ID fehlt.");
                x.BekanntSeit = SpeicherZeitreihenImport.ParseZeit(f, bekannt, ref zoneBekannt, nr);
                x.Entscheidung = SpeicherZeitreihenImport.ParseZeit(f, entscheidung, ref zoneEntscheidung, nr);
            }
            ergebnis.Add(x);
        }
        return ergebnis;
    }

    private static void Normalisiere(IReadOnlyList<Rohzeile> roh, SpeicherFlottenCsvOptionen o,
        out DateTimeOffset[] zeit, out double[] last, out double[] pv, out double[] bhkw,
        out double[] bezug, out double[] pvPreis, out double[] bhkwPreis, out double[] batteriePreis)
    {
        if (roh.Count < 2)
            throw new FormatException("Jede Jahresreihe und jeder Prognose-Snapshot benötigt mindestens zwei Datensätze.");
        List<DateTimeOffset> quelleZeit = roh.Select(x => x.Zeit).ToList();
        TimeSpan raster = SpeicherZeitreihenImport.ErmittleUndPruefeRaster(quelleZeit);
        if (o.Zeitbasis.Konvention == IntervallKonvention.Ende)
            for (int i = 0; i < quelleZeit.Count; i++) quelleZeit[i] -= raster;
        last = Werte(quelleZeit, roh.Select(x => x.Last).ToArray(), raster,
            SpeicherZeitreihenRolle.Last, o.LeistungEinheit, out zeit);
        pv = Werte(quelleZeit, roh.Select(x => x.Pv).ToArray(), raster,
            SpeicherZeitreihenRolle.Pv, o.LeistungEinheit, out _);
        bhkw = Werte(quelleZeit, roh.Select(x => x.Bhkw).ToArray(), raster,
            SpeicherZeitreihenRolle.Pv, o.LeistungEinheit, out _);
        bezug = Werte(quelleZeit, roh.Select(x => x.Bezug).ToArray(), raster,
            SpeicherZeitreihenRolle.Bezug, o.PreisEinheit, out _);
        pvPreis = Werte(quelleZeit, roh.Select(x => x.PvPreis).ToArray(), raster,
            SpeicherZeitreihenRolle.Bezug, o.PreisEinheit, out _);
        bhkwPreis = Werte(quelleZeit, roh.Select(x => x.BhkwPreis).ToArray(), raster,
            SpeicherZeitreihenRolle.Bezug, o.PreisEinheit, out _);
        batteriePreis = Werte(quelleZeit, roh.Select(x => x.BatteriePreis).ToArray(), raster,
            SpeicherZeitreihenRolle.Bezug, o.PreisEinheit, out _);
    }

    private static double[] Werte(IReadOnlyList<DateTimeOffset> zeit, IReadOnlyList<double> werte,
        TimeSpan raster, SpeicherZeitreihenRolle rolle, SpeicherZeitreihenEinheit einheit,
        out DateTimeOffset[] zielZeit)
    {
        SpeicherZeitreihenImport.Normalisiere(zeit, werte, raster, rolle, einheit,
            out zielZeit, out double[] ziel);
        return ziel;
    }

    private static FlottenNetzintervall Intervall(DateTimeOffset zeit, double last, double pv,
        double bhkw, double bezug, double pvPreis, double bhkwPreis, double batteriePreis) => new()
    {
        Zeitstempel = zeit,
        LastKw = last,
        PvKw = pv,
        BhkwKw = bhkw,
        BezugspreisEuroProKWh = bezug,
        PvVerkaufspreisEuroProKWh = pvPreis,
        BhkwVerkaufspreisEuroProKWh = bhkwPreis,
        BatterieVerkaufspreisEuroProKWh = batteriePreis
    };

    private static void PruefeOptionen(SpeicherFlottenCsvOptionen o, bool prognosen)
    {
        if (o.LastSpalte < 0 || o.BezugSpalte < 0)
            throw new ArgumentException("Last- und Bezugspreisspalte müssen ausgewählt sein.", nameof(o));
        foreach (int spalte in new[] { o.PvSpalte, o.BhkwSpalte, o.PvPreisSpalte,
                     o.BhkwPreisSpalte, o.BatteriePreisSpalte })
            if (spalte < -1) throw new ArgumentException("Optionale Spalten dürfen nur -1 oder eine vorhandene Spaltennummer sein.", nameof(o));
        if (prognosen && (o.SnapshotIdSpalte < 0 || o.BekanntSeitSpalte < 0 || o.EntscheidungSpalte < 0))
            throw new ArgumentException("Für Prognosen müssen Snapshot-ID, Bekannt seit und Entscheidungszeitpunkt ausgewählt sein.", nameof(o));
        if (!Enum.IsDefined(o.LeistungEinheit) || !Enum.IsDefined(o.PreisEinheit))
            throw new ArgumentException("Leistungs- und Preiseinheit müssen gültig sein.", nameof(o));
        SpeicherZeitreihenImport.PruefeEinheit(SpeicherZeitreihenRolle.Last, o.LeistungEinheit);
        SpeicherZeitreihenImport.PruefeEinheit(SpeicherZeitreihenRolle.Bezug, o.PreisEinheit);
    }

    private static void PruefeSpalten(string[] felder, SpeicherFlottenCsvOptionen o,
        bool prognosen, int zeile)
    {
        var benoetigt = new List<int> { o.LastSpalte, o.BezugSpalte };
        benoetigt.AddRange(new[] { o.PvSpalte, o.BhkwSpalte, o.PvPreisSpalte,
            o.BhkwPreisSpalte, o.BatteriePreisSpalte }.Where(x => x >= 0));
        if (o.Zeitbasis.ZeitstempelSpalte >= 0) benoetigt.Add(o.Zeitbasis.ZeitstempelSpalte);
        else { benoetigt.Add(o.Zeitbasis.DatumSpalte); benoetigt.Add(o.Zeitbasis.UhrzeitSpalte); }
        if (prognosen) benoetigt.AddRange(new[] { o.SnapshotIdSpalte, o.BekanntSeitSpalte, o.EntscheidungSpalte });
        int max = benoetigt.Max();
        if (felder.Length <= max)
            throw new FormatException($"Zeile {zeile} hat nur {felder.Length} Spalten; benötigt wird Spalte {max + 1}.");
    }

    private static double Zahl(string[] f, int spalte, char dezimal, int zeile)
        => SpeicherZeitreihenImport.ParseZahl(f[spalte], dezimal, zeile);

    private static double ZahlOptional(string[] f, int spalte, char dezimal, int zeile)
        => spalte < 0 ? 0 : Zahl(f, spalte, dezimal, zeile);

    private static SpeicherZeitreihenOptionen Metazeit(SpeicherZeitreihenOptionen basis, int spalte)
    {
        SpeicherZeitreihenOptionen o = Kopiere(basis);
        o.ZeitstempelSpalte = spalte;
        o.DatumSpalte = -1;
        o.UhrzeitSpalte = -1;
        return o;
    }

    private static SpeicherZeitreihenOptionen BesteVorschau(byte[] inhalt, out string[] kopf)
    {
        SpeicherZeitreihenOptionen bester = null;
        SpeicherZeitreihenVorschau beste = null;
        foreach (SpeicherZeitreihenEncoding encoding in Enum.GetValues<SpeicherZeitreihenEncoding>())
        foreach (char trennzeichen in new[] { ';', ',', '\t' })
        {
            var o = new SpeicherZeitreihenOptionen
            {
                Encoding = encoding,
                Trennzeichen = trennzeichen,
                Dezimaltrenner = trennzeichen == ',' ? '.' : ','
            };
            try
            {
                SpeicherZeitreihenVorschau v = SpeicherZeitreihenImport.Vorschau(inhalt, o);
                if (beste == null || v.Spaltenzahl > beste.Spaltenzahl)
                {
                    bester = o;
                    beste = v;
                }
            }
            catch (FormatException) { }
        }
        if (beste == null) throw new FormatException("Die Datei konnte mit keiner unterstützten CSV-Kodierung gelesen werden.");
        kopf = beste.Zeilen.Count > 0 ? beste.Zeilen[0] : Array.Empty<string>();
        return bester;
    }

    private static int Finde(IReadOnlyList<string> kopf, params string[] namen)
    {
        for (int i = 0; i < kopf.Count; i++)
            if (namen.Contains(kopf[i], StringComparer.Ordinal)) return i;
        return -1;
    }

    private static int Waehle(int fallback, int erkannt) => erkannt >= 0 ? erkannt : fallback;

    private static string NormalisiereKopf(string text)
    {
        var b = new StringBuilder();
        foreach (char c in (text ?? "").Trim().ToLowerInvariant())
            if (char.IsLetterOrDigit(c)) b.Append(c);
        return b.ToString();
    }

    private static void PruefeKeinJson(byte[] inhalt, string dateiname)
    {
        if (!string.IsNullOrWhiteSpace(dateiname) && dateiname.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("JSON-Import wird nicht unterstützt. Bitte Datenbankdaten oder eine CSV-Datei verwenden.");
        if (inhalt == null) return;
        string anfang = Encoding.UTF8.GetString(inhalt, 0, Math.Min(inhalt.Length, 256)).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (anfang.StartsWith("{", StringComparison.Ordinal) || anfang.StartsWith("[", StringComparison.Ordinal))
            throw new FormatException("JSON-Import wird nicht unterstützt. Bitte Datenbankdaten oder eine CSV-Datei verwenden.");
    }

    private static SpeicherZeitreihenOptionen Kopiere(SpeicherZeitreihenOptionen o) => new()
    {
        Rolle = o.Rolle,
        Trennzeichen = o.Trennzeichen,
        Dezimaltrenner = o.Dezimaltrenner,
        Encoding = o.Encoding,
        Kopfzeile = o.Kopfzeile,
        ZuUeberspringendeZeilen = o.ZuUeberspringendeZeilen,
        WertSpalte = o.WertSpalte,
        ZeitstempelSpalte = o.ZeitstempelSpalte,
        DatumSpalte = o.DatumSpalte,
        UhrzeitSpalte = o.UhrzeitSpalte,
        ZeitstempelFormat = o.ZeitstempelFormat ?? "",
        DatumFormat = o.DatumFormat ?? "",
        UhrzeitFormat = o.UhrzeitFormat ?? "",
        ZeitzoneId = o.ZeitzoneId ?? "",
        Konvention = o.Konvention,
        Einheit = o.Einheit
    };
}
