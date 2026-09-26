#:package Microsoft.Data.Sqlite

// Datenpflege der Betriebskosten von Projekt 1030 "Referenz BHKW-Kaskade" und Projekt 1026
// "Beispiel WP WG 1" (Welle E30, Statusnummer #545; Befunde B3 und B5 der Sichtpruefung 1030,
// Anwenderentscheide vom 26.09.2026 ~09:50, Fragen E30-Q8 a, Q9 a, Q10 a).
//
// WOZU.
//   B3 - Doppelstruktur der Wartung an 1030: Die Betraege 18.000 EUR/a (BHKW-Kaskade) und
//        2.000 EUR/a (Kessel) standen in zwei Altzeilen OHNE Vorlage ("BHKW", "Heizkessel" als
//        fester Betrag), daneben die leeren Pflichtzeilen der Vorlagen 11 und 12 fuer dieselbe
//        Wartung. Wer an einer Pflichtzeile spaeter einen Satz pflegte (oder "Saetze
//        vorbelegen..." ausloeste), buchte die Wartung doppelt. Die Betraege ziehen in die
//        Pflichtzeilen um (fester Jahresbetrag), die Altzeilen fallen weg.
//   B5 - Die Hilfsenergie-Pflichtzeilen von 1030 (drei) und 1026 (zwei) trugen noch
//        "% der Endenergiekosten" (Weg A), ihre Vorlagen seit Schemaschritt 94 "% des
//        Endenergiebedarfs" (Weg B). Ohne Satz ist das Umstellen ergebnisneutral; mit Satz
//        waeren die Saetze beider Wege nicht austauschbar (Faktor ~3).
//
// ERGEBNISNEUTRAL. 1030 behaelt 20.000,00 EUR/a Betriebskosten in allen drei Szenarien
// (18.000 + 2.000, ganzzahlig, damit unabhaengig von der Summationsreihenfolge), alle
// Kapitalwert-Anker bleiben bitgleich (Probe in Phase 0 auf einer Kopie). Die 18.000 EUR bleiben
// an Anlage 14920 - sie meinen die ganze Kaskade (E30-Q9 a). Nicht angefasst: 1019 (kein
// Referenzprojekt) und 1018 (PROZENT_BRENNSTOFFKOSTEN, Pruefzeile von ProjektkostenArtenTests).
//
// ZIELZELLEN (Tab_ProjektWerte):
//   101600588  1030 Wartung BHKW (Pflicht, 14920)          EUR_PRO_KWH_ELEKTRISCH -> JAHRESBETRAG, 0 -> 18000
//   101600585  1030 Vollwartung / Wartung Kessel (11334)   EUR_PRO_KWH_THERMISCH  -> JAHRESBETRAG, 0 -> 2000
//   101600097  1030 "BHKW" BETRAG 18000 (Altzeile)         geloescht
//   101600098  1030 "Heizkessel" BETRAG 2000 (Altzeile)    geloescht
//   101600587, 101600590, 101600593 (1030), 101600570, 101600576 (1026)
//              PROZENT_ENDENERGIEKOSTEN -> PROZENT_ENDENERGIEBEDARF (Satz bleibt leer)
//
// WIEDERHOLBAR. Steht schon alles auf dem Ziel, aendert das Skript nichts (Rueckgabe 0). Steht
// weder der Vorzustand noch das Ziel da, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2).
// Geschrieben wird in einer Arbeitsdatei neben der Datenbank, in EINER Transaktion; erst nach
// Zielpruefung, integrity_check und foreign_key_check ersetzt sie das Original.
//
// Aufruf (dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

const string WEG_A = "PROZENT_ENDENERGIEKOSTEN";
const string WEG_B = "PROZENT_ENDENERGIEBEDARF";

// (ID, Projekt, Anlage, Bemessung vorher, Wert vorher, Bemessung Ziel, Wert Ziel)
var umzug = new (long Id, long Projekt, long Anlage, string BemVor, double WertVor, string BemZiel, double WertZiel)[]
{
    (101600588, 1030, 14920, "EUR_PRO_KWH_ELEKTRISCH", 0, "JAHRESBETRAG", 18000),
    (101600585, 1030, 11334, "EUR_PRO_KWH_THERMISCH", 0, "JAHRESBETRAG", 2000),
};
// Altzeilen: (ID, Projekt, Anlage, Stamm, Betrag)
var alt = new (long Id, long Projekt, long Anlage, long Stamm, double Betrag)[]
{
    (101600097, 1030, 14920, 83, 18000),
    (101600098, 1030, 11334, 79, 2000),
};
// Hilfsenergie-Pflichtzeilen: (ID, Projekt, Anlage)
var hilfs = new (long Id, long Projekt, long Anlage)[]
{
    (101600587, 1030, 11334), (101600590, 1030, 14920), (101600593, 1030, 14921),
    (101600570, 1026, 14917), (101600576, 1026, 11275),
};

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run datenpflege_1030_1026_betriebskosten.cs -- <Kenndaten_Test.sqlite> [--trocken]");
    return 2;
}
datei = Path.GetFullPath(datei);
using (var fs = File.OpenRead(datei))
{
    var kopf = new byte[15];
    if (fs.Read(kopf, 0, 15) != 15 || Encoding.ASCII.GetString(kopf) != "SQLite format 3")
    {
        Console.Error.WriteLine(datei + " ist keine SQLite-Datei (LFS-Zeiger? git lfs pull).");
        return 2;
    }
}
foreach (string rest in new[] { datei + "-wal", datei + "-shm" })
    if (File.Exists(rest))
    {
        Console.Error.WriteLine("Neben der Datenbank liegt " + Path.GetFileName(rest) + " - erst schliessen/aufraeumen.");
        return 2;
    }

// ---------------------------------------------------------------------------------------
// Zustand lesen
// ---------------------------------------------------------------------------------------
Dictionary<long, Dictionary<string, object>> Zeilen(SqliteConnection cn, IEnumerable<long> ids)
{
    var d = new Dictionary<long, Dictionary<string, object>>();
    using var c = cn.CreateCommand();
    c.CommandText = "SELECT ID, ProjektID, ID_Anlage, StammID, KategorieID, Bemessung, EingegebenerWert, " +
                    "Bestcase, Worstcase, Einheitpreis, Menge, IstPflicht, StartJahr, Wiederholperiode_a " +
                    "FROM Tab_ProjektWerte WHERE ID IN (" + string.Join(",", ids) + ")";
    using var r = c.ExecuteReader();
    while (r.Read())
    {
        var z = new Dictionary<string, object>();
        for (int i = 0; i < r.FieldCount; i++) z[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
        d[Convert.ToInt64(z["ID"])] = z;
    }
    return d;
}
double? Zahl(object o) => o == null ? null : Convert.ToDouble(o, CultureInfo.InvariantCulture);
string Text(object o) => o == null ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

// Abweichungen vom VORZUSTAND bzw. vom ZIEL - leere Liste = der Zustand steht.
List<string> Pruefe(SqliteConnection cn, bool ziel)
{
    var f = new List<string>();
    var ids = umzug.Select(u => u.Id).Concat(alt.Select(a => a.Id)).Concat(hilfs.Select(h => h.Id));
    var z = Zeilen(cn, ids);

    void Gemeinsam(long id, long projekt, long anlage, int kategorie)
    {
        var r = z[id];
        if (Zahl(r["ProjektID"]) != projekt) f.Add(id + ": ProjektID " + Text(r["ProjektID"]));
        if (Zahl(r["ID_Anlage"]) != anlage) f.Add(id + ": ID_Anlage " + Text(r["ID_Anlage"]));
        if (Zahl(r["KategorieID"]) != kategorie) f.Add(id + ": KategorieID " + Text(r["KategorieID"]));
        if ((Zahl(r["Bestcase"]) ?? 0) != 0 || (Zahl(r["Worstcase"]) ?? 0) != 0) f.Add(id + ": Best/Worst gepflegt");
        if (r["Einheitpreis"] != null) f.Add(id + ": Einheitpreis gepflegt (" + Text(r["Einheitpreis"]) + ")");
        if (r["StartJahr"] != null || r["Wiederholperiode_a"] != null) f.Add(id + ": Startjahr/Periode gepflegt");
    }

    foreach (var u in umzug)
    {
        if (!z.ContainsKey(u.Id)) { f.Add(u.Id + ": Zeile fehlt"); continue; }
        Gemeinsam(u.Id, u.Projekt, u.Anlage, 2);
        string bem = ziel ? u.BemZiel : u.BemVor;
        double wert = ziel ? u.WertZiel : u.WertVor;
        if (Text(z[u.Id]["Bemessung"]) != bem) f.Add(u.Id + ": Bemessung " + Text(z[u.Id]["Bemessung"]) + " statt " + bem);
        if (Zahl(z[u.Id]["EingegebenerWert"]) != wert) f.Add(u.Id + ": Wert " + Text(z[u.Id]["EingegebenerWert"]) + " statt " + wert);
        if (Zahl(z[u.Id]["IstPflicht"]) != 1) f.Add(u.Id + ": keine Pflichtzeile");
    }
    foreach (var a in alt)
    {
        if (ziel) { if (z.ContainsKey(a.Id)) f.Add(a.Id + ": Altzeile steht noch"); continue; }
        if (!z.ContainsKey(a.Id)) { f.Add(a.Id + ": Altzeile fehlt"); continue; }
        Gemeinsam(a.Id, a.Projekt, a.Anlage, 2);
        var r = z[a.Id];
        if (Zahl(r["StammID"]) != a.Stamm) f.Add(a.Id + ": StammID " + Text(r["StammID"]));
        if (Text(r["Bemessung"]) != "BETRAG") f.Add(a.Id + ": Bemessung " + Text(r["Bemessung"]));
        if (Zahl(r["EingegebenerWert"]) != a.Betrag) f.Add(a.Id + ": Betrag " + Text(r["EingegebenerWert"]));
    }
    foreach (var h in hilfs)
    {
        if (!z.ContainsKey(h.Id)) { f.Add(h.Id + ": Zeile fehlt"); continue; }
        Gemeinsam(h.Id, h.Projekt, h.Anlage, 2);
        string bem = ziel ? WEG_B : WEG_A;
        if (Text(z[h.Id]["Bemessung"]) != bem) f.Add(h.Id + ": Bemessung " + Text(z[h.Id]["Bemessung"]) + " statt " + bem);
        if ((Zahl(z[h.Id]["EingegebenerWert"]) ?? 0) != 0) f.Add(h.Id + ": Betrag gepflegt");
    }
    return f;
}

string Skalar(SqliteConnection cn, string sql)
{
    using var c = cn.CreateCommand(); c.CommandText = sql;
    return Convert.ToString(c.ExecuteScalar(), CultureInfo.InvariantCulture);
}

void Schliessen(SqliteConnection cn) { cn.Close(); cn.Dispose(); SqliteConnection.ClearAllPools(); }

// ---------------------------------------------------------------------------------------
// 1. Stand der Datei
// ---------------------------------------------------------------------------------------
List<string> abwZiel, abwVor;
// Nur gelesen - aber NICHT mit Mode=ReadOnly: Die Testdatenbank steht im WAL-Modus, und eine
// lesende Verbindung liesse -wal/-shm neben der Datei liegen (sie darf nicht aufraeumen).
using (var cn = new SqliteConnection("Data Source=" + datei + ";Mode=ReadWrite;Pooling=False"))
{
    cn.Open();
    Console.WriteLine("Datei:        " + datei);
    Console.WriteLine("Schemastand:  " + Skalar(cn, "SELECT SchemaVersion FROM Tab_Applikation"));
    abwZiel = Pruefe(cn, true);
    abwVor = Pruefe(cn, false);
    Schliessen(cn);
}
if (abwZiel.Count == 0)
{
    Console.WriteLine("1030/1026 stehen schon auf dem Ziel - nichts zu tun.");
    return 0;
}
if (abwVor.Count > 0)
{
    Console.Error.WriteLine("Weder Vorzustand noch Ziel - Abbruch vor dem Schreiben:");
    foreach (string a in abwVor) Console.Error.WriteLine("  " + a);
    return 2;
}
if (trocken)
{
    Console.WriteLine("--trocken: Vorzustand passt, die Pflege wuerde laufen.");
    return 0;
}

// ---------------------------------------------------------------------------------------
// 2. In einer Arbeitsdatei schreiben
// ---------------------------------------------------------------------------------------
string arbeit = datei + ".e30arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);

int Abbruch(string grund)
{
    SqliteConnection.ClearAllPools();
    foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
    Console.Error.WriteLine("Abbruch, die Datenbank bleibt unveraendert: " + grund);
    return 2;
}

using (var cn = new SqliteConnection("Data Source=" + arbeit + ";Pooling=False"))
{
    cn.Open();
    int n = 0;
    using (var tx = cn.BeginTransaction())
    {
        int X(string sql, params object[] w)
        {
            using var c = cn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = sql;
            for (int i = 0; i < w.Length; i++) c.Parameters.AddWithValue("$p" + i, w[i]);
            return c.ExecuteNonQuery();
        }
        foreach (var u in umzug)
            n += X("UPDATE Tab_ProjektWerte SET Bemessung = $p0, EingegebenerWert = $p1 WHERE ID = $p2 AND Bemessung = $p3",
                   u.BemZiel, u.WertZiel, u.Id, u.BemVor);
        foreach (var a in alt)
            n += X("DELETE FROM Tab_ProjektWerte WHERE ID = $p0 AND ProjektID = $p1 AND Bemessung = 'BETRAG'", a.Id, a.Projekt);
        foreach (var h in hilfs)
            n += X("UPDATE Tab_ProjektWerte SET Bemessung = $p0 WHERE ID = $p1 AND Bemessung = $p2 AND Einheitpreis IS NULL",
                   WEG_B, h.Id, WEG_A);
        if (n != umzug.Length + alt.Length + hilfs.Length)
        {
            tx.Rollback();
            Schliessen(cn);
            return Abbruch(n + " Zeilen getroffen statt " + (umzug.Length + alt.Length + hilfs.Length) + ".");
        }
        tx.Commit();
    }

    // -----------------------------------------------------------------------------------
    // 3. Pruefen, dann ersetzen
    // -----------------------------------------------------------------------------------
    List<string> ziel = Pruefe(cn, true);
    string integ = Skalar(cn, "PRAGMA integrity_check");
    long fk;
    using (var c = cn.CreateCommand())
    {
        c.CommandText = "PRAGMA foreign_key_check";
        using var r = c.ExecuteReader();
        fk = 0; while (r.Read()) fk++;
    }
    Console.WriteLine("Zeilen:       " + n + " (2 umgezogen, 2 geloescht, 5 auf Weg B)");
    Console.WriteLine("integrity:    " + integ);
    Schliessen(cn);
    if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
    if (integ != "ok") return Abbruch("integrity_check: " + integ);
    if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
}
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
Console.WriteLine("Betriebskosten 1030/1026 gepflegt.");
return 0;
