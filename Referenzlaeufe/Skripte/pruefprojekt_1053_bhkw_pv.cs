#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Pruefprojekt 1053 "Test BHKW mit PV ohne Kaskade" an - ein BHKW
// mit Photovoltaik OHNE Speicherflotte, OHNE Stromspeicher und OHNE Einspeisegrenze, OHNE
// Referenzrolle.
//
// WOZU. Die BHKW-Einspeisung hat ohne Flotte zwei Quellen: Mit Photovoltaik liest der
// Zeitreihensatz (ZeitreihenExtraktor, Schluessel BHKW_UEBERSCHUSS) die Viertelstundenbilanz der
// PV-Stufe (SimulationPV.BhkwUeberschuss), der BHKW-Reiter und seine Kennzahl EinspeisungMwh
// die Stundenformel des KWK-Splits (SimulationControl.BhkwEinspeisungDesLaufs). Kein
// Referenzprojekt rechnet BHKW und PV ohne Flotte; 1053 erzeugt den Fall, damit beide Reihen
// nebeneinander gemessen werden koennen (EPOS.Kern.Tests/BhkwPvPruefprojektTests haelt die Form).
//
// KEIN REFERENZPROJEKT. 1053 steht in keiner Referenzbasis, in keiner Projektliste von kern.yml,
// ios.yml oder Referenzlaeufe/LIESMICH.md, und fuer 1053 gilt keine Einfrierregel. Die Vorlage
// 1018 (ein Referenzprojekt) bleibt Zelle fuer Zelle, wie sie war - das Skript prueft das.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1018 -> 1053 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle
//      Projekttabellen, keine Rechenergebnisse. Nur die Projekt-ID muss 1053 sein, sonst Abbruch.
//   2. Zellen der Kopie:
//        Tab_Projekt        Beschreibung, Aenderungs- und Erstelldatum fest (2026-10-04),
//                           Kosten_Geaendert leer (die Testdatenbank fuehrt leere Kostenstempel)
//        Tab_Einstellungen  Tool_1 'BHKW', Tool_2 bis Tool_4 leer (nur das BHKW in der Kaskade;
//                           der Kesselanteil der Waerme bleibt ungedeckt), Tool_5 'Photovoltaik',
//                           Tool_6 leer (kein Stromspeicher); Einspeisegrenze bleibt leer
//   3. Neue Zeilen:
//        Tab_PV             Projektkopie des Katalogmoduls "Jinkosolar JKM 260P-60" (dasselbe
//                           Modul wie 1048) ueber PhotovoltaikCtrl.CopyFromStamm
//        Tab_Energieanlagen PV-Anlage: 60 Module x 260 W = 15,60 kWp, Neigung 30, Azimut 0 (wie 1048)
//        Stromverbraucher   Katalogsatz "Hotel_1" (Typprofil "Hotel") ueber
//                           StromverbraucherStammCtrl.CopyFromStamm, Zuordnung mit 50 MWh/a
//      Die beiden Katalogkopierer sind intern; das Skript ruft sie ueber Reflexion, damit die
//      Kopie Spalte fuer Spalte dem Programm entspricht.
//   Warum 60 Module (gemessen am Lauf von 1053): Die Stromlast des Hotels (50 MWh/a) liegt im
//   Sommer (Juni bis August, 8 bis 18 Uhr) im Mittel bei 7,35 kW, hoechstens bei 9,60 kW; die
//   PV-Spitze von 15,6 kWp erreicht 15,65 kWh je Stunde und liegt damit ueber der Last
//   (2 290 Stunden mit PV-Ueberschuss). Das BHKW (14,5 kW el., 2 340 Betriebsstunden) liegt
//   ueber der Hotellast (1 868 Stunden mit BHKW-Ueberschuss); 740 Stunden haben beides.
//
// WIEDERHOLBAR. Steht "Test BHKW mit PV ohne Kaskade" schon mit allen Zielzellen, aendert das
// Skript nichts (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1018 ab oder faellt die
// Kopie nicht auf 1053, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2): Geschrieben wird in
// eine Arbeitsdatei neben der Datenbank, geprueft, und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/pruefprojekt_1053_bhkw_pv.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WindowsFormsApplication1;

const int VORLAGE = 1018;
const string VORLAGE_NAME = "BHKW Test München";
const int NEU = 1053;
const string NAME = "Test BHKW mit PV ohne Kaskade";
const string BESCHREIBUNG =
    "Prüfprojekt ohne Referenzrolle: Kopie von Projekt 1018 mit dem BHKW allein in der Kaskade, " +
    "60 PV-Modulen (15,60 kWp) und dem Stromverbraucher Hotel (50 MWh/a), ohne Stromspeicher, " +
    "Speicherflotte und Einspeisegrenze. Es erzeugt BHKW- und PV-Überschuss im selben Lauf " +
    "(BhkwPvPruefprojektTests) und steht in keiner Referenzbasis.";
const string DATUM = "2026-10-04 00:00:00";

const string MODUL = "Jinkosolar JKM 260P-60";
const int MODULE = 60;
const double MODULLEISTUNG_W = 260.0;
const int NEIGUNG = 30;
const int AZIMUT = 0;
const string VERBRAUCHER = "Hotel_1";
const double VERBRAUCHER_MWH = 50.0;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run pruefprojekt_1053_bhkw_pv.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/pruefprojekt_1053_bhkw_pv.cs");

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
    if (!string.Equals(ist ?? "", soll ?? "", StringComparison.Ordinal))
        f.Add(was + ": '" + (ist ?? "NULL") + "' statt '" + (soll ?? "NULL") + "'");
}

// Die internen Katalogkopierer des Programms (Reflexion; die Klassen sind intern).
Assembly kern = typeof(ProjektDuplizierenCtrl).Assembly;
int PvKopieren(int projekt)
{
    Type t = kern.GetType("WindowsFormsApplication1.PhotovoltaikCtrl", true);
    MethodInfo m = t.GetMethod("CopyFromStamm", new[] { typeof(string), typeof(int) });
    return (int)m.Invoke(Activator.CreateInstance(t, true), new object[] { MODUL, projekt });
}
int VerbraucherKopieren(int projekt)
{
    Type t = kern.GetType("WindowsFormsApplication1.StromverbraucherStammCtrl", true);
    MethodInfo m = t.GetMethod("CopyFromStamm", BindingFlags.Public | BindingFlags.Static, null,
                               new[] { typeof(string), typeof(int) }, null);
    return (int)m.Invoke(null, new object[] { VERBRAUCHER, projekt });
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
    return sb.ToString();
}

// Gemeinsame Pruefung von Vorlage und Ziel: kein Stromspeicher, keine Flotte, keine Grenze.
void OhneSpeicher(List<string> f, int id, string k)
{
    if (Z("SELECT COUNT(*) FROM Tab_Stromspeicher WHERE ID_Projekt = ?", id) != 0) f.Add(k + "fuehrt einen Stromspeicher");
    if (Z("SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", id) != 0) f.Add(k + "fuehrt eine Speicherflotte");
    DataTable e = T("SELECT Einspeisegrenze_Wert, Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = ?", id);
    if (e.Rows.Count != 1) { f.Add(k + e.Rows.Count + " Einstellungszeilen statt 1"); return; }
    Soll(f, k + "Einspeisegrenze_Wert", D(e.Rows[0], "Einspeisegrenze_Wert"), null);
    Soll(f, k + "Kuehlbetrieb", D(e.Rows[0], "Kuehlbetrieb"), 0);
}

List<string> PruefeVorlage()
{
    var f = new List<string>();
    if (Z("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Projektname = ?", VORLAGE, VORLAGE_NAME) != 1)
    { f.Add("Vorlage " + VORLAGE + " '" + VORLAGE_NAME + "' fehlt"); return f; }
    DataTable e = T("SELECT Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6 FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE);
    if (e.Rows.Count == 1)
    {
        SollText(f, "Vorlage Tool_1", Txt(e.Rows[0], "Tool_1"), DbWerte.ERZEUGER_BHKW);
        SollText(f, "Vorlage Tool_2", Txt(e.Rows[0], "Tool_2"), DbWerte.ERZEUGER_HEIZKESSEL);
        SollText(f, "Vorlage Tool_5", Txt(e.Rows[0], "Tool_5"), "");
    }
    OhneSpeicher(f, VORLAGE, "Vorlage ");
    if (Z("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_PV > 0", VORLAGE) != 0) f.Add("Vorlage: fuehrt PV");
    if (Z("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_BHKW > 0", VORLAGE) != 1) f.Add("Vorlage: nicht genau ein BHKW");
    if (Z("SELECT COUNT(*) FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", VORLAGE) != 0) f.Add("Vorlage: fuehrt Stromverbraucher");
    if (Z("SELECT COUNT(*) FROM Tab_PV_STAMM WHERE Bezeichner = ?", MODUL) != 1) f.Add("Katalogmodul '" + MODUL + "' fehlt");
    if (Z("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?", VERBRAUCHER) != 1) f.Add("Katalogverbraucher '" + VERBRAUCHER + "' fehlt");
    return f;
}

List<string> PruefeZiel(int id)
{
    var f = new List<string>();
    if (id != NEU) f.Add("Projekt-ID " + id + " statt " + NEU);
    DataTable p = T("SELECT * FROM Tab_Projekt WHERE ID = ?", id);
    SollText(f, "Beschreibung", Txt(p.Rows[0], "Beschreibung"), BESCHREIBUNG);
    SollText(f, "Aenderungsdatum", Dat(p.Rows[0], "Aenderungsdatum"), DATUM);
    SollText(f, "Erstelldatum", Dat(p.Rows[0], "Erstelldatum"), DATUM);
    SollText(f, "Kosten_Geaendert", Dat(p.Rows[0], "Kosten_Geaendert"), null);

    DataTable e = T("SELECT Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6 FROM Tab_Einstellungen WHERE ID_Projekt = ?", id);
    if (e.Rows.Count == 1)
    {
        SollText(f, "Tool_1", Txt(e.Rows[0], "Tool_1"), DbWerte.ERZEUGER_BHKW);
        foreach (string s in new[] { "Tool_2", "Tool_3", "Tool_4", "Tool_6" }) SollText(f, s, Txt(e.Rows[0], s), "");
        SollText(f, "Tool_5", Txt(e.Rows[0], "Tool_5"), DbWerte.ERZEUGER_PHOTOVOLTAIK);
    }
    OhneSpeicher(f, id, "");
    if (Z("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_BHKW > 0", id) != 1) f.Add("nicht genau ein BHKW");

    DataTable pv = T("SELECT a.PV_Leistung, a.Neigung, a.Azimut, p.Leistung, p.Bezeichner, p.ID_Projekt FROM Tab_Energieanlagen a " +
                     "JOIN Tab_PV p ON p.ID = a.ID_PV WHERE a.ID_Projekt = ? AND a.ID_PV > 0", id);
    if (pv.Rows.Count != 1) f.Add(pv.Rows.Count + " PV-Anlagen statt 1");
    else
    {
        DataRow r = pv.Rows[0];
        Soll(f, "PV_Leistung (Modulanzahl)", D(r, "PV_Leistung"), MODULE);
        Soll(f, "Neigung", D(r, "Neigung"), NEIGUNG);
        Soll(f, "Azimut", D(r, "Azimut"), AZIMUT);
        Soll(f, "Modulleistung", D(r, "Leistung"), MODULLEISTUNG_W);
        Soll(f, "Modul ID_Projekt", D(r, "ID_Projekt"), id);
        SollText(f, "Modul", Txt(r, "Bezeichner"), MODUL);
    }

    DataTable sv = T("SELECT z.Bezeichner, z.Summe, v.ID_Projekt, v.Typ FROM Z_Projekt_Stromverbraucher z " +
                     "JOIN Tab_Stromverbraucher v ON v.ID = z.ID_Stromverbraucher WHERE z.ID_Projekt = ?", id);
    if (sv.Rows.Count != 1) f.Add(sv.Rows.Count + " Stromverbraucher statt 1");
    else
    {
        SollText(f, "Stromverbraucher", Txt(sv.Rows[0], "Bezeichner"), VERBRAUCHER);
        Soll(f, "Stromverbraucher Summe", D(sv.Rows[0], "Summe"), VERBRAUCHER_MWH);
        Soll(f, "Stromverbraucher ID_Projekt", D(sv.Rows[0], "ID_Projekt"), id);
    }
    foreach (string t in new[] { "Tab_Ergebnis", "Tab_ErgebnisStromMatrix", "Tab_ErgebnisWirtschaftlichkeit" })
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
string arbeit = datei + ".p1053arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");

X("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?", BESCHREIBUNG, DATUM, DATUM, id);
X("UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = '', Tool_3 = '', Tool_4 = '', Tool_5 = ?, Tool_6 = '' WHERE ID_Projekt = ?",
  DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_PHOTOVOLTAIK, id);

int modul = PvKopieren(id);
if (modul <= 0) return Abbruch("PV-Modul '" + MODUL + "' nicht kopiert.");
X("INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, ID_PV, PV_Leistung, Neigung, Azimut, ID_Carrier, " +
  "WQ_TemperaturModus) VALUES (?, ?, ?, ?, ?, ?, ?, 0, 'Berechnet')",
  id, MODUL, WizardItemClass.PV_TYP, modul, MODULE, NEIGUNG, AZIMUT);

int verbraucher = VerbraucherKopieren(id);
if (verbraucher <= 0) return Abbruch("Stromverbraucher '" + VERBRAUCHER + "' nicht kopiert.");
// Dieselbe Zeile wie WizardCtrl.Add_Projekt_Stromverbraucher (die Klasse ist intern).
X("INSERT INTO Z_Projekt_Stromverbraucher (ID, ID_Projekt, ID_Stromverbraucher, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)",
  Z("SELECT MAX(ID) FROM Z_Projekt_Stromverbraucher") + 1, id, verbraucher, VERBRAUCHER, VERBRAUCHER_MWH);
// Die Kostenstempel-Trigger stempeln die kopierten Kostenzeilen; die Testdatenbank fuehrt leere
// Stempel (KostenStempelSchemaTests).
X("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL WHERE ID = ?", id);

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = PruefeZiel(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (Abdruck(VORLAGE) != abdruckVorher) return Abbruch("Die Vorlage " + VORLAGE + " hat sich veraendert.");
Console.WriteLine("Zeilen 1053:  " + string.Join(", ", Projekttabellen()
    .Select(p => (p.Tabelle, n: Z("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)));
Console.WriteLine("integrity:    " + S("PRAGMA integrity_check"));
long fk = T("PRAGMA foreign_key_check").Rows.Count;
if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt (" +
    (MODULE * MODULLEISTUNG_W / 1000.0).ToString("0.00", CultureInfo.InvariantCulture) + " kWp).");
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
