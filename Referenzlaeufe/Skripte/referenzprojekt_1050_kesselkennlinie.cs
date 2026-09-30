#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1050 "Referenzprojekt Kesselkennlinie" an - das
// einzige Referenzprojekt, dessen Heizkessel eine GEPFLEGTE Kennlinie traegt (Konzept
// Kesselkennlinie 4.3, Entscheid F4 vom 29.09.2026: Kopie von 1023, keine AK1-Variante, nicht in
// der CI-Auswahl).
//
// WOZU. Mit der Etappe E2 rechnet jeder Brennstoffkessel je Stunde mit dem Wirkungsgrad seiner
// Laststufe. Die uebrigen Referenzkessel tragen kein eigenes eta30 und rechnen mit der
// Normvorgabe nach Bauart - 1023 als einziger Brennwertkessel mit eta100 + 0,06, alle anderen
// als Niedertemperaturkessel flach. Ein gepflegtes eta30 ueber Hs/Hi-naher Groesse, der Schalter
// der Brennwertkennlinie (E3) und die Taktfelder (E4) stuenden sonst ausserhalb des
// Regressionsnetzes.
//
// VORLAGE 1023 "Woehler - Test1": zwei Waermepumpen auf einen Puffer, danach ein Gas-Brennwert-
// kessel (Brennwert = 1, 19,3 kW) direkt am Heizkreis. Der Kessel laeuft rund 5 000 Stunden im
// Jahr ueber die ganze Breite der Laststufen - die Teillastkurve wirkt in jeder Stufe.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1023 -> 1050 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle
//      Projekttabellen samt Z_AnlageSenke, keine Rechenergebnisse. IDs vergibt der Kopierweg;
//      nur die Projekt-ID muss 1050 sein, sonst Abbruch.
//   2. Zellen der Kopie:
//        Tab_Projekt    Beschreibung, Aenderungs- und Erstelldatum fest (2026-09-30)
//        Tab_Heizkessel die Kennlinie des Projektkessels mit den neutralen Werten aus Konzept
//                       4.3: Wirkungsgrad_Gas (eta100) 0,97, Wirkungsgrad_Teillast30 1,05,
//                       Kennlinie_Brennwert 1, Mindestleistung 3,86 kW (20 % von 19,3 kW),
//                       Anfahrverlust_kWh 0,1; Mindestlaufzeit_min bleibt leer (Normvorgabe)
//   Keine neue Zeile, keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde
//   Pruefwerte, kein Datenblatt.
//
// EINFRIERREGEL. 1050 ist Referenzprojekt: die fuenf Kennlinienspalten seines Kessels, sein
// Wirkungsgrad_Gas und sein Brennwert gehoeren zur Einfrierregel "gesaete Kesseldaten"
// (CLAUDE.md, Referenzlaeufe/LIESMICH.md), ebenso das Anlegen oder Entfernen des Projekts.
//
// WIEDERHOLBAR. Steht "Referenzprojekt Kesselkennlinie" schon mit allen Zielzellen, aendert das
// Skript nichts (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1023 ab oder faellt die
// Kopie nicht auf 1050, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2): Geschrieben wird
// in eine Arbeitsdatei neben der Datenbank, geprueft, und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1050_kesselkennlinie.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

const int VORLAGE = 1023;
const string VORLAGE_NAME = "Wöhler - Test1";
const int NEU = 1050;
const string NAME = "Referenzprojekt Kesselkennlinie";
const string BESCHREIBUNG =
    "Referenzprojekt der Kesselkennlinie: Kopie von Projekt 1023 (zwei Wärmepumpen auf einen Puffer, " +
    "danach ein Gas-Brennwertkessel 19,3 kW am Heizkreis) mit gepflegter Kennlinie des Kessels: " +
    "Wirkungsgrad bei Nennlast 0,97, bei 30 % Last 1,05, Brennwertkennlinie an, Mindestleistung " +
    "3,86 kW (20 %), Anfahrverlust 0,1 kWh, Mindestlaufzeit leer. Hält Teillast-, Brennwert- und " +
    "Taktrechnung des Kessels im Regressionsnetz; nicht in der CI-Auswahl.";
const string DATUM = "2026-09-30 00:00:00";

const double NENNLEISTUNG = 19.3;       // kW, Projektkessel von 1023
const double ETA100_VORLAGE = 0.874;
const double ETA100 = 0.97;
const double ETA30 = 1.05;
const double MINDESTLEISTUNG = 3.86;    // 20 % von 19,3 kW
const double ANFAHRVERLUST = 0.1;       // kWh je Start
const int TYP_KESSEL = 10;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1050_kesselkennlinie.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/referenzprojekt_1050_kesselkennlinie.cs");

// ---------------------------------------------------------------------------------------
// Hilfen
// ---------------------------------------------------------------------------------------
DbParam P(object w) => new DbParam("?", w ?? DBNull.Value);
object S(string sql, params object[] w) => DataRepository.ExecuteScalar(sql, w.Select(P).ToArray());
long Z(string sql, params object[] w)
{
    object o = S(sql, w);
    return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
}
int X(string sql, params object[] w) => DataRepository.ExecuteNonQuery(sql, w.Select(P).ToArray());
DataTable T(string sql, params object[] w) => DataRepository.GetDataTable(sql, w.Select(P).ToArray());
double? D(DataRow r, string s) => r[s] == DBNull.Value ? (double?)null : Convert.ToDouble(r[s], CultureInfo.InvariantCulture);
string Txt(DataRow r, string s) => r[s] == DBNull.Value ? null : Convert.ToString(r[s], CultureInfo.InvariantCulture);
string Dat(DataRow r, string s) => r[s] is DateTime d ? d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : Txt(r, s);

void Soll(List<string> f, string was, double? ist, double? soll)
{
    bool gleich = ist.HasValue && soll.HasValue ? Math.Abs(ist.Value - soll.Value) <= 1e-9 : ist == soll;
    if (!gleich) f.Add(was + ": " + (ist?.ToString("R", CultureInfo.InvariantCulture) ?? "NULL") +
                       " statt " + (soll?.ToString("R", CultureInfo.InvariantCulture) ?? "NULL"));
}
void SollText(List<string> f, string was, string ist, string soll)
{
    if (!string.Equals(ist, soll, StringComparison.Ordinal))
        f.Add(was + ": '" + (ist ?? "NULL") + "' statt '" + (soll ?? "NULL") + "'");
}

List<(string Tabelle, string Spalte)> Projekttabellen()
{
    var l = new List<(string, string)>();
    foreach (DataRow r in T("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name").Rows)
    {
        string t = Convert.ToString(r[0]);
        List<string> sp = DataRepository.SpaltenVonTabelle(t);
        string s = sp.FirstOrDefault(c => c.Equals("ID_Projekt", StringComparison.OrdinalIgnoreCase)) ??
                   sp.FirstOrDefault(c => c.Equals("ProjektID", StringComparison.OrdinalIgnoreCase));
        if (s != null) l.Add((t, s));
    }
    return l;
}

// Abdruck aller Zeilen eines Projekts ueber alle Projekttabellen samt Senkenzeilen.
string Abdruck(int projekt)
{
    var sb = new StringBuilder();
    void Tabelle(string sql, string kopf)
    {
        DataTable dt = T(sql, projekt);
        sb.Append('#').Append(kopf).Append('\n');
        foreach (DataRow r in dt.Rows)
        {
            foreach (DataColumn c in dt.Columns)
                sb.Append(r[c] == DBNull.Value ? "∅" : Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append('|');
            sb.Append('\n');
        }
    }
    Tabelle("SELECT * FROM \"Tab_Projekt\" WHERE \"ID\" = ?", "Tab_Projekt");
    foreach (var (t, s) in Projekttabellen())
        Tabelle("SELECT * FROM \"" + t + "\" WHERE \"" + s + "\" = ? ORDER BY 1", t);
    Tabelle("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?) ORDER BY 1",
            "Z_AnlageSenke");
    return sb.ToString();
}

// Der eine Kessel eines Projekts: die Projektkopie in Tab_Heizkessel, die seine Anlagenzeile nennt.
DataTable KesselVon(int projekt)
    => T("SELECT h.* FROM Tab_Heizkessel h JOIN Tab_Energieanlagen a ON a.ID_Projekt = h.ID_Projekt " +
         "AND a.Bezeichner = h.Bezeichner AND a.ID_Type = ? WHERE h.ID_Projekt = ?", TYP_KESSEL, projekt);

// Die Vorlage in dem Zustand, von dem dieses Skript ausgeht.
List<string> PruefeVorlage()
{
    var f = new List<string>();
    if (Z("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Projektname = ?", VORLAGE, VORLAGE_NAME) != 1)
    { f.Add("Vorlage " + VORLAGE + " '" + VORLAGE_NAME + "' fehlt"); return f; }
    if (Z("SELECT COUNT(*) FROM Tab_Heizkessel WHERE ID_Projekt = ?", VORLAGE) != 1)
        f.Add("Vorlage: nicht genau ein Projektkessel");
    DataTable k = KesselVon(VORLAGE);
    if (k.Rows.Count != 1) { f.Add("Vorlage: " + k.Rows.Count + " Kessel mit Anlagenzeile statt 1"); return f; }
    DataRow r = k.Rows[0];
    Soll(f, "Vorlage Ptherm", D(r, "Ptherm"), NENNLEISTUNG);
    Soll(f, "Vorlage Brennwert", D(r, "Brennwert"), 1);
    Soll(f, "Vorlage Wirkungsgrad_Gas", D(r, "Wirkungsgrad_Gas"), ETA100_VORLAGE);
    Soll(f, "Vorlage Wirkungsgrad_Teillast30", D(r, "Wirkungsgrad_Teillast30"), null);
    Soll(f, "Vorlage Kennlinie_Brennwert", D(r, "Kennlinie_Brennwert"), 0);
    Soll(f, "Vorlage Mindestleistung", D(r, "Mindestleistung"), null);
    Soll(f, "Vorlage Anfahrverlust_kWh", D(r, "Anfahrverlust_kWh"), null);
    Soll(f, "Vorlage Mindestlaufzeit_min", D(r, "Mindestlaufzeit_min"), null);
    return f;
}

// Das fertige Projekt: jede Zielzelle.
List<string> PruefeZiel(int id)
{
    var f = new List<string>();
    if (id != NEU) f.Add("Projekt-ID " + id + " statt " + NEU);
    DataTable p = T("SELECT * FROM Tab_Projekt WHERE ID = ?", id);
    SollText(f, "Beschreibung", Txt(p.Rows[0], "Beschreibung"), BESCHREIBUNG);
    SollText(f, "Aenderungsdatum", Dat(p.Rows[0], "Aenderungsdatum"), DATUM);
    SollText(f, "Erstelldatum", Dat(p.Rows[0], "Erstelldatum"), DATUM);

    if (Z("SELECT COUNT(*) FROM Tab_Heizkessel WHERE ID_Projekt = ?", id) != 1)
        f.Add("nicht genau ein Projektkessel");
    DataTable k = KesselVon(id);
    if (k.Rows.Count != 1) { f.Add(k.Rows.Count + " Kessel mit Anlagenzeile statt 1"); return f; }
    DataRow r = k.Rows[0];
    Soll(f, "Ptherm", D(r, "Ptherm"), NENNLEISTUNG);
    Soll(f, "Brennwert", D(r, "Brennwert"), 1);
    Soll(f, "Wirkungsgrad_Gas", D(r, "Wirkungsgrad_Gas"), ETA100);
    Soll(f, "Wirkungsgrad_Teillast30", D(r, "Wirkungsgrad_Teillast30"), ETA30);
    Soll(f, "Kennlinie_Brennwert", D(r, "Kennlinie_Brennwert"), 1);
    Soll(f, "Mindestleistung", D(r, "Mindestleistung"), MINDESTLEISTUNG);
    Soll(f, "Anfahrverlust_kWh", D(r, "Anfahrverlust_kWh"), ANFAHRVERLUST);
    Soll(f, "Mindestlaufzeit_min", D(r, "Mindestlaufzeit_min"), null);

    foreach (string t in new[] { "Tab_Ergebnis", "Tab_ErgebnisWirtschaftlichkeit" })
        if (Z("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", id) != 0) f.Add(t + " fuehrt Zeilen");
    return f;
}

// ---------------------------------------------------------------------------------------
// 1. Stand der Datei
// ---------------------------------------------------------------------------------------
DataRepository.PfadUeberschreibung = datei;
Console.WriteLine("Datei:        " + datei);
Console.WriteLine("Schemastand:  " + S("SELECT SchemaVersion FROM Tab_Applikation"));

long vorhanden = Z("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", NAME);
if (vorhanden > 0)
{
    List<string> abw = PruefeZiel((int)vorhanden);
    Ende();
    if (abw.Count == 0)
    {
        Console.WriteLine("Projekt " + vorhanden + " '" + NAME + "' steht schon mit allen Zielzellen - nichts zu tun.");
        return 0;
    }
    Console.Error.WriteLine("Projekt " + vorhanden + " steht, weicht aber ab:");
    foreach (string a in abw) Console.Error.WriteLine("  " + a);
    return 2;
}

List<string> vorlage = PruefeVorlage();
long frei = Z("SELECT MAX(ID) FROM Tab_Projekt") + 1;
if (frei != NEU) vorlage.Add("Die Kopie fiele auf " + frei + " statt " + NEU);
if (vorlage.Count > 0)
{
    Ende();
    Console.Error.WriteLine("Abbruch vor dem Schreiben:");
    foreach (string a in vorlage) Console.Error.WriteLine("  " + a);
    return 2;
}
if (trocken)
{
    Ende();
    Console.WriteLine("--trocken: Vorlage passt, Projekt " + NEU + " wuerde angelegt.");
    return 0;
}
string abdruckVorher = Abdruck(VORLAGE);

// ---------------------------------------------------------------------------------------
// 2. In einer Arbeitsdatei schreiben
// ---------------------------------------------------------------------------------------
Ende();
string arbeit = datei + ".r27arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");

X("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?", BESCHREIBUNG, DATUM, DATUM, id);

DataTable kessel = KesselVon(id);
if (kessel.Rows.Count != 1) return Abbruch("Der Kessel der Kopie ist nicht eindeutig.");
long kesselId = Convert.ToInt64(kessel.Rows[0]["ID"], CultureInfo.InvariantCulture);
if (X("UPDATE Tab_Heizkessel SET Wirkungsgrad_Gas = ?, Wirkungsgrad_Teillast30 = ?, Kennlinie_Brennwert = 1, " +
      "Mindestleistung = ?, Anfahrverlust_kWh = ?, Mindestlaufzeit_min = NULL WHERE ID = ? AND ID_Projekt = ?",
      ETA100, ETA30, MINDESTLEISTUNG, ANFAHRVERLUST, kesselId, id) != 1)
    return Abbruch("Die Kennlinie des Kessels " + kesselId + " ist nicht geschrieben.");

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = PruefeZiel(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (Abdruck(VORLAGE) != abdruckVorher) return Abbruch("Die Vorlage " + VORLAGE + " hat sich veraendert.");
Console.WriteLine("Zeilen 1050:  " + string.Join(", ", Projekttabellen()
    .Select(p => (p.Tabelle, n: Z("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)) +
    ", Z_AnlageSenke " + Z("SELECT COUNT(*) FROM Z_AnlageSenke WHERE ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)", id));
Console.WriteLine("integrity:    " + S("PRAGMA integrity_check"));
long fk = T("PRAGMA foreign_key_check").Rows.Count;
if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt (Kessel " + kesselId + ": eta100 " +
                  ETA100.ToString(CultureInfo.InvariantCulture) + ", eta30 " + ETA30.ToString(CultureInfo.InvariantCulture) + ").");
return 0;

int Abbruch(string grund)
{
    Ende();
    foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
    Console.Error.WriteLine("Abbruch, die Datenbank bleibt unveraendert: " + grund);
    return 2;
}

void Ende()
{
    DataRepository.PfadUeberschreibung = null;
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
}
