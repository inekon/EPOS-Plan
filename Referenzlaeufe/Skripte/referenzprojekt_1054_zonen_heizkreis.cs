#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:include referenzprojekt_1052_bauplan.cs
#:include referenzprojekt_1054_bauplan.cs
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1054 "Referenzprojekt Zonen mit Heizkreis" an (Welle AK1z,
// Waermeuebergabe je Zone, E63): eine Kopie des Zonenprojekts 1052 mit Anlagenkopplung AK1, Heizkreis am
// Gebaeude und eigener Uebergabe an der Zone "Gastronomie und Verwaltung".
//
// WOZU. Ohne 1054 rechnet kein Projekt der Testdatenbank Zonen GEKOPPELT: Vorlauf, Ruecklauf und begrenzte
// Stunden je Zone (Tab_ErgebnisZone) und die Auslegung je Zone stuenden ausserhalb des Regressionsnetzes.
// 1052 bleibt unveraendert - es traegt den Nachweis der Aufheizoptimierung mit Zonen.
//
// WAS DIESES SKRIPT TUT. Die gesaeten Zellen stehen im Bauplan (referenzprojekt_1054_bauplan.cs, Kopf):
//   1. Die Kopie 1052 -> 1054 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle Projekttabellen
//      samt Zonen, Bauteilen, Luftstrom und Zonenkalendern, keine Rechenergebnisse. Die Kopie faellt auf
//      MAX(Tab_Projekt.ID) + 1; vorausgesetzt ist 1053 als hoechste Projekt-ID.
//   2. Der Bauplan: Kopfzellen, Kopplungsstufe ueber KonfigurationCtrl, Gebaeude- und Zonenzellen,
//      Kostenstempel leer.
// Keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde Pruefwerte.
//
// EINFRIERREGEL. 1054 gehoert zur Einfrierregel "gesaete Auslegungsdaten der Uebergabe" und - als Kopie von
// 1052 - zu "gesaete Zonendaten" (Referenzlaeufe/LIESMICH.md); es kommt mit dem Einfrieren in die Basis.
//
// WIEDERHOLBAR. Steht "Referenzprojekt Zonen mit Heizkreis" schon mit allen Zielzellen, aendert das Skript
// nichts (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1052 ab oder faellt die Kopie nicht auf 1054,
// bricht es ab, ohne die Datei zu aendern (Rueckgabe 2): Geschrieben wird in eine Arbeitsdatei neben der
// Datenbank, geprueft (Zielzellen, 1052 unberuehrt, integrity_check, foreign_key_check), und erst dann
// ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1054_zonen_heizkreis.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using B = EPOS.Referenzlaeufe.Skripte.Zonenprojekt1052;
using H = EPOS.Referenzlaeufe.Skripte.Zonenprojekt1054;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1054_zonen_heizkreis.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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

// Die Kultur des deutschen Anwenders wie beim Saatskript von 1052 (Texte der Programmwege).
var kultur = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;
CultureInfo.CurrentCulture = kultur;
CultureInfo.CurrentUICulture = kultur;
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/referenzprojekt_1054_zonen_heizkreis.cs");

// ---------------------------------------------------------------------------------------
// 1. Stand der Datei
// ---------------------------------------------------------------------------------------
DataRepository.PfadUeberschreibung = datei;
Console.WriteLine("Datei:        " + datei);
Console.WriteLine("Schemastand:  " + DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation"));

long vorhanden = B.Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", H.NAME);
if (vorhanden > 0)
{
    List<string> abw = H.Pruefen((int)vorhanden);
    if (vorhanden != H.NEU) abw.Insert(0, "Projekt-ID " + vorhanden + " statt " + H.NEU);
    Ende();
    if (abw.Count == 0)
    {
        Console.WriteLine("Projekt " + vorhanden + " '" + H.NAME + "' steht schon mit allen Zielzellen - nichts zu tun.");
        return 0;
    }
    Console.Error.WriteLine("Projekt " + vorhanden + " steht, weicht aber ab:");
    foreach (string a in abw) Console.Error.WriteLine("  " + a);
    return 2;
}

var vorlage = new List<string>();
if (B.Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?", H.VORLAGE_NAME) != 1 ||
    B.Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", H.VORLAGE_NAME) != H.VORLAGE)
    vorlage.Add("Vorlage " + H.VORLAGE + " '" + H.VORLAGE_NAME + "' fehlt oder ist nicht eindeutig");
else
    foreach (string a in B.Pruefen(H.VORLAGE)) vorlage.Add("Vorlage " + H.VORLAGE + ": " + a);
if (KonfigurationCtrl.AnlagenkopplungLesen(H.VORLAGE) != null) vorlage.Add("Vorlage: Anlagenkopplung gesetzt");
long hoechste = B.Zahl("SELECT MAX(ID) FROM Tab_Projekt");
long belegt = B.Projekttabellen().Select(p => B.Zahl("SELECT MAX(\"" + p.Spalte + "\") FROM \"" + p.Tabelle + "\"")).DefaultIfEmpty(0).Max();
if (hoechste != H.NEU - 1) vorlage.Add("Die höchste Projekt-ID ist " + hoechste + " - erwartet " + (H.NEU - 1));
if (belegt >= H.NEU) vorlage.Add("Die Projekt-ID " + H.NEU + " ist in einer Projekttabelle belegt (" + belegt + ")");
if (vorlage.Count > 0)
{
    Ende();
    Console.Error.WriteLine("Abbruch vor dem Schreiben:");
    foreach (string a in vorlage) Console.Error.WriteLine("  " + a);
    return 2;
}
Console.WriteLine("Vorlage:      " + H.VORLAGE + " (" + H.Zaehlung(H.VORLAGE) + ")");
if (trocken)
{
    Ende();
    Console.WriteLine("--trocken: Vorlage passt, Projekt " + H.NEU + " wuerde angelegt.");
    return 0;
}
string vorlageVorher = B.Abdruck(H.VORLAGE, false);

// ---------------------------------------------------------------------------------------
// 2. In einer Arbeitsdatei schreiben
// ---------------------------------------------------------------------------------------
Ende();
string arbeit = datei + ".ak1zarbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(H.VORLAGE_NAME, H.NAME);
Console.WriteLine("Kopie:        " + H.VORLAGE + " -> " + id);
if (id != H.NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + H.NEU + ".");
string fehler = H.Bauen(id);
if (fehler != null) return Abbruch(fehler);

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = H.Pruefen(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (B.Abdruck(H.VORLAGE, false) != vorlageVorher) return Abbruch("Die Vorlage " + H.VORLAGE + " hat sich verändert.");
if (B.Pruefen(H.VORLAGE).Count > 0) return Abbruch("Die Vorlage " + H.VORLAGE + " weicht vom Bauplan ab.");
Console.WriteLine("Zeilen 1054:  " + string.Join(", ", B.Projekttabellen()
    .Select(p => (p.Tabelle, n: B.Zahl("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)));
Console.WriteLine("Zonendaten:   " + H.Zaehlung(id));
object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
Console.WriteLine("integrity:    " + integ);
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
long fk = B.Tabelle("PRAGMA foreign_key_check").Rows.Count;
if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + H.NEU + " '" + H.NAME + "' angelegt.");
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
