#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:include referenzprojekt_1052_bauplan.cs
#:include referenzprojekt_1051_bauplan.cs
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1051 "Referenzprojekt Konditionierung" an
// (Teilkonzept Konditionierungsprofile 10.2; Entwurf KP3, Festlegungen 31 und 32; E58 F5 (a), F8 (a)):
// ein eigenes Projekt neben den sechzehn der Basis R33, damit deren Zahlen byte-gleich bleiben.
//
// WOZU. Ohne 1051 traegt kein Referenzprojekt angelegte Kalender aller fuenf Groessen, Ferien,
// Feiertage, eine Heizperiode, die Nachtauskuehlung und die Aufheizoptimierung mit Rampe; Vorlagenweg
// je Groesse und Kopierweg Katalog -> Projekt stuenden ausserhalb des Regressionsnetzes. 1051 ist
// Grundlage des Einfrierens mit RP2 (Basis R34).
//
// WAS DIESES SKRIPT TUT. Bau, gesaete Zellen und Programmwege stehen im Bauplan
// (referenzprojekt_1051_bauplan.cs, Kopf); hier steht die Huelle drumherum:
//   1. Die Kopie 1007 -> 1051 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl). Die Kopie
//      faellt auf MAX(Tab_Projekt.ID) + 1 - darum laeuft dieses Skript VOR referenzprojekt_1052_zonen.cs
//      (Befund G6d): Steht 1052 schon, bricht es ab; die Testdatenbank wird dann ohne 1052 eingesetzt,
//      1051 gesaet und 1052 erneut gezogen.
//   2. Der Referenzkatalogbau aus dem per Probe gewaehlten Katalogbau (Duplizieren, Arbeitsstand, OK).
//   3. Der Bauplan am Projekt (Zuordnung Katalog -> Projekt, Aufheizvorgabe, Kopfzellen).
// Ausser dem Referenzkatalogbau keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde
// Pruefwerte oder ausgelieferte Vorlagen, kein Datenblatt.
//
// EINFRIERREGEL. 1051 ist (ab RP2) Referenzprojekt: seine Kalender, Vorgaben und Perioden, der
// Referenzkatalogbau samt Konditionierung, die Vorlagen, aus denen er uebernimmt, und die
// Aufheizvorgabe gehoeren zur Einfrierregel "gesaete Konditionierungsdaten" (Teilkonzept 10.3).
//
// WIEDERHOLBAR. Stehen "Referenzprojekt Konditionierung" und sein Katalogbau schon mit allen
// Zielzellen, aendert das Skript nichts (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1007
// ab oder faellt die Kopie nicht auf 1051, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2):
// Geschrieben wird in eine Arbeitsdatei neben der Datenbank, geprueft, und erst dann ersetzt sie das
// Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1051_konditionierung.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using B = EPOS.Referenzlaeufe.Skripte.Konditionierungsprojekt1051;
using Z = EPOS.Referenzlaeufe.Skripte.Zonenprojekt1052;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1051_konditionierung.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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

// Die Kultur des deutschen Anwenders, wie im Dialog: Die Programmwege schreiben Texte in der
// Anzeigesprache - die Herkunft eines uebernommenen Kalenders steht als "aus Vorlage Büro" in
// Tab_Konditionierungskalender.Bemerkung (unter en-US "from template Büro"; Befund Herkunftstext).
var kultur = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;
CultureInfo.CurrentCulture = kultur;
CultureInfo.CurrentUICulture = kultur;
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/referenzprojekt_1051_konditionierung.cs");

// Der Stand ausserhalb von 1051 und seinem Katalogbau, der sich nicht aendern darf: die Vorlage 1007
// (Schluessel eingeschlossen), jeder uebrige Katalogbau samt Konditionierung und die Vorlagen der
// Konditionierung samt Kalendern.
string Bestand()
{
    var sb = new StringBuilder(Z.Abdruck(B.VORLAGE, false));
    foreach (string sql in new[]
    {
        "SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner <> ? ORDER BY ID",
        "SELECT * FROM Tab_Konditionierungsvorlage_STAMM ORDER BY ID",
        "SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Vorlage IS NOT NULL OR ID_Gebaeude_Stamm IN " +
        "(SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner <> ?) ORDER BY ID",
        "SELECT * FROM Tab_Konditionierungskalender WHERE ID_Vorlage IS NOT NULL OR ID_Gebaeude_Stamm IN " +
        "(SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner <> ?) ORDER BY ID",
        "SELECT p.* FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender " +
        "WHERE k.ID_Vorlage IS NOT NULL OR k.ID_Gebaeude_Stamm IN (SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner <> ?) ORDER BY p.ID",
    })
    {
        object[] w = sql.Contains('?') ? new object[] { B.REFERENZBAU } : new object[0];
        foreach (DataRow r in Z.Tabelle(sql, w).Rows)
            sb.Append(string.Join("|", r.ItemArray.Select(o => o == DBNull.Value ? "∅" : o is double d ? d.ToString("R", CultureInfo.InvariantCulture)
                                                                : Convert.ToString(o, CultureInfo.InvariantCulture)))).Append('\n');
    }
    return sb.ToString();
}

// ---------------------------------------------------------------------------------------
// 1. Stand der Datei
// ---------------------------------------------------------------------------------------
DataRepository.PfadUeberschreibung = datei;
Console.WriteLine("Datei:        " + datei);
Console.WriteLine("Schemastand:  " + DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation"));

long vorhanden = Z.Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", B.NAME);
if (vorhanden > 0)
{
    List<string> abw = B.Pruefen((int)vorhanden);
    if (vorhanden != B.NEU) abw.Insert(0, "Projekt-ID " + vorhanden + " statt " + B.NEU);
    Ende();
    if (abw.Count == 0)
    {
        Console.WriteLine("Projekt " + vorhanden + " '" + B.NAME + "' steht schon mit allen Zielzellen - nichts zu tun.");
        return 0;
    }
    Console.Error.WriteLine("Projekt " + vorhanden + " steht, weicht aber ab:");
    foreach (string a in abw) Console.Error.WriteLine("  " + a);
    return 2;
}

var vorlage = new List<string>();
if (Z.Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?", B.VORLAGE_NAME) != 1 ||
    Z.Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", B.VORLAGE_NAME) != B.VORLAGE)
    vorlage.Add("Vorlage " + B.VORLAGE + " '" + B.VORLAGE_NAME + "' fehlt oder ist nicht eindeutig");
if (Z.Gebaeude(B.VORLAGE) <= 0) vorlage.Add("Vorlage: nicht genau ein Gebäude mit Projektkopie");
if (KonfigurationCtrl.AufheizvorgabeLesen(B.VORLAGE).An) vorlage.Add("Vorlage: Aufheizoptimierung an");
if (KonfigurationCtrl.KuehlbetriebLesen(B.VORLAGE)) vorlage.Add("Vorlage: Kühlbetrieb an");
if (Z.Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", B.KATALOGBAU) != 1)
    vorlage.Add("Der gewählte Katalogbau '" + B.KATALOGBAU + "' fehlt oder ist mehrdeutig");
if (B.Referenzbau() > 0) vorlage.Add("Der Referenzkatalogbau '" + B.REFERENZBAU + "' steht schon ohne Projekt " + B.NEU);
foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
    if (B.Buero(g) == null) vorlage.Add("Die ausgelieferte Vorlage 'Büro' der Größe " + g + " fehlt oder ist mehrdeutig");
long hoechste = Z.Zahl("SELECT MAX(ID) FROM Tab_Projekt");
long belegt = Z.Projekttabellen().Select(p => Z.Zahl("SELECT MAX(\"" + p.Spalte + "\") FROM \"" + p.Tabelle + "\"")).DefaultIfEmpty(0).Max();
if (hoechste != B.NEU - 1)
    vorlage.Add("Die höchste Projekt-ID ist " + hoechste + " - erwartet " + (B.NEU - 1) +
                (hoechste > B.NEU ? " (1052 steht schon: die Testdatenbank ohne 1052 einsetzen, 1051 säen, 1052 erneut ziehen)" : ""));
if (belegt >= B.NEU) vorlage.Add("Die Projekt-ID " + B.NEU + " ist in einer Projekttabelle belegt (" + belegt + ")");
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
    Console.WriteLine("--trocken: Vorlage passt, Projekt " + B.NEU + " wuerde angelegt.");
    return 0;
}
string bestandVorher = Bestand();

// ---------------------------------------------------------------------------------------
// 2. In einer Arbeitsdatei schreiben
// ---------------------------------------------------------------------------------------
Ende();
string arbeit = datei + ".rp1arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(B.VORLAGE_NAME, B.NAME);
Console.WriteLine("Kopie:        " + B.VORLAGE + " -> " + id);
if (id != B.NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + B.NEU + ".");

string fehler = B.KatalogbauAnlegen(B.KATALOGBAU, B.REFERENZBAU, out int kb);
if (fehler != null) return Abbruch(fehler);
Console.WriteLine("Katalogbau:   " + B.KATALOGBAU + " -> " + kb + " '" + B.REFERENZBAU + "'");
fehler = B.Bauen(id, kb);
if (fehler != null) return Abbruch(fehler);

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = B.Pruefen(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (Bestand() != bestandVorher) return Abbruch("Die Vorlage " + B.VORLAGE + ", ein Katalogbau oder eine Konditionierungsvorlage hat sich verändert.");
int geb = Z.Gebaeude(id);
Console.WriteLine("Zeilen 1051:  " + string.Join(", ", Z.Projekttabellen()
    .Select(p => (p.Tabelle, n: Z.Zahl("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)));
foreach ((string wer, string spalte, long traeger) in new[] { ("Katalogbau", "ID_Gebaeude_Stamm", (long)kb), ("Gebäude", "ID_Gebaeude", (long)geb) })
    Console.WriteLine((wer + ":").PadRight(14) + Z.Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE " + spalte + " = ?", traeger) + " Kalender, " +
                      Z.Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsvorgabe WHERE " + spalte + " = ?", traeger) + " Vorgaben, " +
                      Z.Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender WHERE k." +
                             spalte + " = ?", traeger) + " Perioden");
Console.WriteLine("integrity:    " + DataRepository.ExecuteScalar("PRAGMA integrity_check"));
long fk = Z.Tabelle("PRAGMA foreign_key_check").Rows.Count;
if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + B.NEU + " '" + B.NAME + "' angelegt.");
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
