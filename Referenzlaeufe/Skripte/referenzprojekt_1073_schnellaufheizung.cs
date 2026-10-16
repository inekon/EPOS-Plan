#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1073 "Referenzprojekt Schnellaufheizung" an (SA4, E128, E153, Entwurf
// Vorheizrampe Abschnitt 7): eine Kopie des Referenzprojekts 1054 "Referenzprojekt Zonen mit Heizkreis" (Hotel in drei
// Zonen, Anlagenkopplung AK1, Radiator und Heizkurve am Gebaeude, eine Zone mit eigener Uebergabe, Aufheizoptimierung
// eingeschaltet), an der Projektkopie das Verfahren "Vorheizzeit berechnen" mit Schnellaufheizung um 3 K gesetzt.
//
// WOZU. Die Schnellaufheizung wirkt nur an Zonen mit wirksamem Heizkreis und nur in den Verfahren "Vorheizzeit
// vorgeben/berechnen". 1065 rechnet das Verfahren ohne Heizkreis (dort waere sie wirkungslos), 1054 die Sollwertrampe
// mit Heizkreis. Ohne 1073 stuenden die Ueberhoehung im Vorheizfenster, ihre Vorausschau, das Ende bei Ankunft und die
// sechs Ergebnisgroessen ausserhalb des Regressionsnetzes. 1054 ist die Vorlage, an der die Schnellaufheizung am
// deutlichsten wirkt (Messung SA1b: ohne Ueberhoehung wird jede Nacht durchgeheizt).
//
// DIE GESAETEN WERTE (Tab_Einstellungen der Kopie):
//   Aufheizoptimierung              1 (wie 1054)
//   Aufheiz_Verfahren               BERECHNET  (1054: leer = Sollwertrampe; die Schnellaufheizung braucht ein Verfahren)
//   Aufheiz_Schnellaufheizung       1
//   Aufheiz_Ueberhoehung_K          3          (rund, ueber der Kern-Vorgabe 2 K: prueft den Weg ueber die Spalte)
//   Aufheiz_Vorheizzeit_H, Aufheiz_Toleranz_Art, Aufheiz_Toleranz, Aufheiz_Regelgenauigkeit_K, Aufheiz_Geltung,
//   Aufheiz_Heizkreis_Einbeziehen   leer = Vorgaben des Kerns (Toleranz 20 %, eps 1 K, Geltung Gebaeude, Heizkreis an)
// Alle uebrigen Zellen wie 1054 (Gebaeude, Zonen, Bauteile, Zonenkalender, Kopplung, Uebergabe, Erzeuger, Kaskade).
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1054 -> 1073 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle Projekttabellen samt
//      Zonen, keine Rechenergebnisse. Die Kopie faellt auf MAX(Tab_Projekt.ID) + 1; vorausgesetzt ist 1072 als
//      hoechste Projekt-ID.
//   2. Kopfzellen (Beschreibung, Kosten_Geaendert leer) und die zehn Vorheizzellen.
//   3. Pruefung: jede gesaete Zelle, je Projekttabelle so viele Zeilen wie 1054, Zonen, integrity_check,
//      foreign_key_check.
// Keine Katalogzeile, kein VACUUM.
//
// EINFRIERREGEL. 1073 gehoert zu "gesaete Vorheizdaten eines Referenzprojekts" (samt Schnellaufheizung) und - als
// Kopie von 1054 - zu "gesaete Zonendaten" und "gesaete Auslegungsdaten der Uebergabe"; gehalten von
// EPOS.Kern.Tests/SchnellaufheizungReferenzprojektWacheTests.
//
// WIEDERHOLBAR. Steht das Projekt schon, aendert das Skript nichts (Rueckgabe 0, wenn es 1073 ist und die Zellen
// stimmen, sonst 2). Mit --trocken wird nur die Vorlage geprueft. Geschrieben wird in eine Arbeitsdatei neben der
// Datenbank, geprueft, und erst dann ersetzt sie das Original. Nach einer Neufassung der Testdatenbank (neuer
// Schemaschritt) wird es erneut gezogen.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1073_schnellaufheizung.cs -- Referenzlaeufe/Kenndaten_Test.sqlite [--trocken]

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

const int VORLAGE = 1054;
const string VORLAGE_NAME = "Referenzprojekt Zonen mit Heizkreis";
const int VORHER_HOECHSTE = 1072;
const int NEU = 1073;
const string NAME = "Referenzprojekt Schnellaufheizung";
const string BESCHREIBUNG = "Referenzprojekt Schnellaufheizung: Kopie von Projekt 1054, das Hotel in drei Zonen mit Heizkreis heizt " +
                            "vor dem Kalendersprung mit berechneter Vorheizzeit vor und hebt im Vorheizfenster den Raumsollwert der " +
                            "Heizkurve um 3 K an (Schnellaufheizung, Ende bei Ankunft; Toleranz 20 %, Ankunft innerhalb 1 K, Geltung Gebäude)";

// Die gesaeten Zellen der Projektkopie in Tab_Einstellungen (null = leer).
var soll = new List<(string Spalte, object Wert)>
{
    ("Aufheizoptimierung", 1L),
    (VorheizenSchema.SPALTE_VERFAHREN, VorheizenSchema.VERFAHREN_BERECHNET),
    (VorheizenSchema.SPALTE_VORHEIZZEIT, null),
    (VorheizenSchema.SPALTE_TOLERANZ_ART, null),
    (VorheizenSchema.SPALTE_TOLERANZ, null),
    (VorheizenSchema.SPALTE_REGELGENAUIGKEIT, null),
    (VorheizenSchema.SPALTE_GELTUNG, null),
    (VorheizenSchema.SPALTE_HEIZKREIS, null),
    (SchnellaufheizungSchema.SPALTE_SCHALTER, 1L),
    (SchnellaufheizungSchema.SPALTE_UEBERHOEHUNG, 3.0),
};

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1073_schnellaufheizung.cs -- <Kenndaten_Test.sqlite> [--trocken]");
    return 2;
}
datei = Path.GetFullPath(datei);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

// Ohne Lizenz schreibt der Kern nur mit der Werkzeugfreigabe (Muster EPOS.Referenzlauf).
Schreibnaht.WerkzeugFreigabe("referenzprojekt_1073_schnellaufheizung.cs (Saatskript der Testdatenbank)");
DataRepository.PfadUeberschreibung = datei;
Console.WriteLine("Schemastand:  " + DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation"));
object vorhanden = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", NAME));
if (vorhanden != null && vorhanden != DBNull.Value)
{
    int v = Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
    string abw = v == NEU ? Abweichung(v) : "Projekt-ID " + v + " statt " + NEU;
    Ende();
    if (abw == null) { Console.WriteLine("Projekt " + v + " '" + NAME + "' steht schon mit allen Zielzellen - nichts zu tun."); return 0; }
    Console.Error.WriteLine("Projekt " + v + " steht, weicht aber ab: " + abw);
    return 2;
}
long hoechste = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Projekt"), CultureInfo.InvariantCulture);
if (hoechste != VORHER_HOECHSTE) { Ende(); Console.Error.WriteLine("Die höchste Projekt-ID ist " + hoechste + " - erwartet " + VORHER_HOECHSTE); return 2; }
object vorlageId = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", VORLAGE_NAME));
if (vorlageId == null || Convert.ToInt32(vorlageId, CultureInfo.InvariantCulture) != VORLAGE)
{ Ende(); Console.Error.WriteLine("Die Vorlage '" + VORLAGE_NAME + "' ist nicht Projekt " + VORLAGE); return 2; }
// 1054 rechnet die Sollwertrampe mit Heizkreis (AK1): Aufheizoptimierung an, alle Vorheiz- und Schnellaufheizungszellen leer.
DataRow vz = DataRepository.GetDataTable("SELECT " + string.Join(", ", soll.Select(s => s.Spalte)) + " FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                         new DbParam("?", VORLAGE)).Rows[0];
if (Convert.ToInt64(vz["Aufheizoptimierung"], CultureInfo.InvariantCulture) != 1 || soll.Skip(1).Any(s => vz[s.Spalte] != DBNull.Value))
{ Ende(); Console.Error.WriteLine("Die Vorlage " + VORLAGE + " rechnet nicht die Sollwertrampe mit leeren Vorheizzellen."); return 2; }
if (!"AK1".Equals(Convert.ToString(DataRepository.ExecuteScalar("SELECT Anlagenkopplung FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                                                 new DbParam("?", VORLAGE)), CultureInfo.InvariantCulture)))
{ Ende(); Console.Error.WriteLine("Die Vorlage " + VORLAGE + " rechnet nicht mit Anlagenkopplung AK1."); return 2; }
if (trocken) { Ende(); Console.WriteLine("--trocken: Vorlage passt, Projekt " + NEU + " wuerde angelegt."); return 0; }

Ende();
string arbeit = datei + ".sa4arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Kosten_Geaendert = NULL WHERE ID = ?",
                               new DbParam("?", BESCHREIBUNG), new DbParam("?", id));
foreach (var (spalte, wert) in soll)
    DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET " + spalte + " = ? WHERE ID_Projekt = ?",
                                   new DbParam("?", wert ?? (object)DBNull.Value), new DbParam("?", id));

string fehler = Abweichung(id);
if (fehler != null) return Abbruch(fehler);
foreach (var (spalte, wert) in soll) Console.WriteLine("  " + spalte.PadRight(30) + (wert == null ? "leer" : Convert.ToString(wert, CultureInfo.InvariantCulture)));

// Gegenprobe: je Projekttabelle (Spalte ID_Projekt) so viele Zeilen wie die Vorlage; Gebaeude, Zonen, Kalender.
var tabellen = DataRepository.GetDataTable("SELECT m.name FROM sqlite_master m WHERE m.type = 'table' AND EXISTS " +
                                           "(SELECT 1 FROM pragma_table_info(m.name) p WHERE p.name = 'ID_Projekt') ORDER BY m.name")
                             .Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToList();
var ausgenommen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Tab_Projekt" };
foreach (string t in tabellen)
{
    if (t.StartsWith("Tab_Ergebnis", StringComparison.OrdinalIgnoreCase) || ausgenommen.Contains(t)) continue;
    long nV = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", VORLAGE);
    long nN = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", id);
    if (nV != nN) return Abbruch(t + ": " + nN + " Zeilen statt " + nV + " wie in " + VORLAGE);
    if (nN > 0) Console.WriteLine("  Zeilen " + t.PadRight(34) + nN);
}
string zonen = "SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?)";
string bauteile = "SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Zone IN (SELECT z.ID FROM Tab_Zone z JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude WHERE g.ID_Projekt = ?)";
if (Zahl(zonen, VORLAGE) != Zahl(zonen, id) || Zahl(zonen, id) != 3) return Abbruch("Zonen weichen von " + VORLAGE + " ab.");
if (Zahl(bauteile, VORLAGE) != Zahl(bauteile, id) || Zahl(bauteile, id) == 0) return Abbruch("Bauteile der Zonen weichen von " + VORLAGE + " ab.");
Console.WriteLine("  Zonen                             " + Zahl(zonen, id));
Console.WriteLine("  Bauteile der Zonen                " + Zahl(bauteile, id));
object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
if (DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count != 0) return Abbruch("foreign_key_check meldet Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt; integrity_check ok, foreign_key_check leer.");
return 0;

long Zahl(string sql, object w) => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("?", w)), CultureInfo.InvariantCulture);

// Die erste Abweichung der gesaeten Zellen eines Projekts, sonst null.
string Abweichung(int projekt)
{
    DataRow p = DataRepository.GetDataTable("SELECT Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", new DbParam("?", projekt)).Rows[0];
    if (!Equals(Convert.ToString(p["Beschreibung"], CultureInfo.InvariantCulture), BESCHREIBUNG)) return "Beschreibung weicht ab";
    if (p["Kosten_Geaendert"] != DBNull.Value) return "Kosten_Geaendert nicht leer";
    DataRow z = DataRepository.GetDataTable("SELECT " + string.Join(", ", soll.Select(s => s.Spalte)) + " FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                            new DbParam("?", projekt)).Rows[0];
    foreach (var (spalte, wert) in soll)
    {
        object ist = z[spalte];
        bool gleich = wert == null ? ist == DBNull.Value
            : ist != DBNull.Value && Convert.ToString(ist, CultureInfo.InvariantCulture) == Convert.ToString(wert, CultureInfo.InvariantCulture);
        if (!gleich) return spalte + " = " + (ist == DBNull.Value ? "leer" : ist) + " statt " + (wert ?? "leer");
    }
    return null;
}

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
