#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:include referenzprojekt_1052_bauplan.cs
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1052 "Referenzprojekt Zonen" an - das einzige
// Referenzprojekt mit Zonen (Mehrzonenkonzept 9, Stufe G6d; Entscheid M11 dort: ein neues Projekt
// statt einer Umstellung, damit die Basis der sechzehn Projekte byte-gleich bleibt).
//
// WOZU. Die Testdatenbank fuehrt ohne 1052 keine Zeile in Tab_Zone: Zonenschleife, Trennflaechen
// (Tab_Bauteil.ID_Nachbarzone), Zonenluftstrom (Tab_Zonenluftstrom), Zonenkalender und die
// Aufheizplanung je Zone stuenden ausserhalb des Regressionsnetzes; die Zonentests liefen nur ueber
// synthetische Mehrzonenfassungen. 1052 ist Grundlage des Einfrierens mit RP2 (Basis R34).
//
// WAS DIESES SKRIPT TUT. Zonenschnitt, gesaete Zellen und Programmwege stehen im Bauplan
// (referenzprojekt_1052_bauplan.cs, Kopf); hier stehen die Huelle drumherum:
//   1. Die Kopie 1018 -> 1052 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle
//      Projekttabellen samt Z_AnlageSenke, keine Rechenergebnisse. Die Kopie faellt auf
//      MAX(Tab_Projekt.ID) + 1. Steht 1051 (RP1) schon, faellt sie von selbst auf 1052. Fehlt 1051,
//      haelt fuer die Dauer der Kopie eine Platzhalterzeile Tab_Projekt.ID = 1051 die Nummer frei
//      (ohne jede Kindzeile, danach geloescht) - die einzige Zeile, die das Skript ausserhalb der
//      Programmwege schreibt, neben den Kopfzellen von Tab_Projekt.
//   2. Der Bauplan (Zonen, Trennflaechen, Luftstrom, Zonenkalender, Aufheizvorgabe).
// Keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde Pruefwerte oder aus dem
// Bestandsgebaeude abgeleitet, kein Datenblatt.
//
// EINFRIERREGEL. 1052 ist (ab RP2) Referenzprojekt: seine Zonen, Trennflaechen, Luftstroeme und
// Zonenkalender gehoeren zur Einfrierregel "gesaete Zonendaten" (Referenzlaeufe/LIESMICH.md).
//
// WIEDERHOLBAR. Steht "Referenzprojekt Zonen" schon mit allen Zielzellen, aendert das Skript nichts
// (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1018 ab oder faellt die Kopie nicht auf
// 1052, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2): Geschrieben wird in eine Arbeitsdatei
// neben der Datenbank, geprueft, und erst dann ersetzt sie das Original. Nach einer Neufassung der
// Testdatenbank ohne 1052 wird es auf der neuen Fassung erneut gezogen.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1052_zonen.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
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
using B = EPOS.Referenzlaeufe.Skripte.Zonenprojekt1052;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1052_zonen.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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
// Anzeigesprache - die Herkunft eines uebernommenen Kalenders steht als "aus Vorlage Wohnen" in
// Tab_Konditionierungskalender.Bemerkung (unter en-US "from template Wohnen"). Die Wache geht die
// Wege unter derselben Kultur nach.
var kultur = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;
CultureInfo.CurrentCulture = kultur;
CultureInfo.CurrentUICulture = kultur;
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/referenzprojekt_1052_zonen.cs");

// Der Stand ausserhalb von 1052, der sich nicht aendern darf: die Vorlage 1018 (Schluessel
// eingeschlossen) und die Vorlagen der Konditionierung samt Kalendern.
string Bestand()
{
    var sb = new StringBuilder(B.Abdruck(B.VORLAGE, false));
    foreach (string sql in new[]
    {
        "SELECT * FROM Tab_Konditionierungsvorlage_STAMM ORDER BY ID",
        "SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Vorlage IS NOT NULL ORDER BY ID",
        "SELECT * FROM Tab_Konditionierungskalender WHERE ID_Vorlage IS NOT NULL ORDER BY ID",
        "SELECT p.* FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender " +
        "WHERE k.ID_Vorlage IS NOT NULL ORDER BY p.ID",
    })
        foreach (DataRow r in B.Tabelle(sql).Rows)
            sb.Append(string.Join("|", r.ItemArray.Select(o => o == DBNull.Value ? "∅" : Convert.ToString(o, CultureInfo.InvariantCulture)))).Append('\n');
    return sb.ToString();
}

// ---------------------------------------------------------------------------------------
// 1. Stand der Datei
// ---------------------------------------------------------------------------------------
DataRepository.PfadUeberschreibung = datei;
Console.WriteLine("Datei:        " + datei);
Console.WriteLine("Schemastand:  " + DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation"));

long vorhanden = B.Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", B.NAME);
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
if (B.Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?", B.VORLAGE_NAME) != 1 ||
    B.Zahl("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", B.VORLAGE_NAME) != B.VORLAGE)
    vorlage.Add("Vorlage " + B.VORLAGE + " '" + B.VORLAGE_NAME + "' fehlt oder ist nicht eindeutig");
int gebVorlage = B.Gebaeude(B.VORLAGE);
if (gebVorlage <= 0) vorlage.Add("Vorlage: nicht genau ein Gebäude mit Projektkopie");
else if (B.Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = ?", gebVorlage) != 0) vorlage.Add("Vorlage: das Gebäude trägt Zonen");
if (KonfigurationCtrl.AufheizvorgabeLesen(B.VORLAGE).An) vorlage.Add("Vorlage: Aufheizoptimierung an");
B.Plan planVorlage = B.Planen(B.VORLAGE, out string mp);
if (planVorlage == null) vorlage.Add("Vorlage: " + mp);
else
{
    string regel = GebaeudeZonenCtrl.Pruefen(planVorlage.Zonen, planVorlage.Luftstroeme);
    if (regel != null) vorlage.Add("Zonenprüfung des Plans: " + regel);
}
foreach (string n in new[] { B.NUTZUNG_GAESTE, B.NUTZUNG_GASTRO })
    if (B.Heizvorlage(n) == null) vorlage.Add("Die ausgelieferte Heizvorlage '" + n + "' fehlt oder ist mehrdeutig");

long hoechste = B.Zahl("SELECT MAX(ID) FROM Tab_Projekt");
long belegt = B.Projekttabellen().Select(p => B.Zahl("SELECT MAX(\"" + p.Spalte + "\") FROM \"" + p.Tabelle + "\"")).DefaultIfEmpty(0).Max();
bool platzhalter = hoechste == B.VORBEHALTEN - 1;
if (hoechste != B.VORBEHALTEN && !platzhalter)
    vorlage.Add("Die höchste Projekt-ID ist " + hoechste + " - erwartet " + (B.VORBEHALTEN - 1) + " oder " + B.VORBEHALTEN);
if (belegt >= B.NEU) vorlage.Add("Die Projekt-ID " + B.NEU + " ist in einer Projekttabelle belegt (" + belegt + ")");
if (vorlage.Count > 0)
{
    Ende();
    Console.Error.WriteLine("Abbruch vor dem Schreiben:");
    foreach (string a in vorlage) Console.Error.WriteLine("  " + a);
    return 2;
}
Console.WriteLine("Faktor:       " + planVorlage.Faktor.ToString("R", CultureInfo.InvariantCulture) +
                  (platzhalter ? "; 1051 fehlt - Platzhalter fuer die Kopie" : "; 1051 steht"));
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
string arbeit = datei + ".g6darbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

if (platzhalter &&
    DataRepository.ExecuteNonQuery("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (?, ?)",
                                   new DbParam("?", B.VORBEHALTEN), new DbParam("?", "Platzhalter " + B.VORBEHALTEN)) != 1)
    return Abbruch("Der Platzhalter " + B.VORBEHALTEN + " ist nicht geschrieben.");
int id = new ProjektDuplizierenCtrl().Duplizieren(B.VORLAGE_NAME, B.NAME);
Console.WriteLine("Kopie:        " + B.VORLAGE + " -> " + id);
if (platzhalter &&
    DataRepository.ExecuteNonQuery("DELETE FROM Tab_Projekt WHERE ID = ? AND Projektname = ?",
                                   new DbParam("?", B.VORBEHALTEN), new DbParam("?", "Platzhalter " + B.VORBEHALTEN)) != 1)
    return Abbruch("Der Platzhalter " + B.VORBEHALTEN + " ist nicht entfernt.");
if (id != B.NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + B.NEU + ".");

// Die frische Kopie ergibt denselben Plan wie die Vorlage (Faktor, Flaechen, Werte).
B.Plan planKopie = B.Planen(id, out string mk);
if (planKopie == null) return Abbruch("Plan der Kopie: " + mk);
if (string.Join("\n", planKopie.Zonen.Select(z => B.Text(z, planKopie.Zonen))) !=
    string.Join("\n", planVorlage.Zonen.Select(z => B.Text(z, planVorlage.Zonen))))
    return Abbruch("Der Plan der Kopie weicht vom Plan der Vorlage ab.");

string fehler = B.Bauen(id);
if (fehler != null) return Abbruch(fehler);

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = B.Pruefen(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (Bestand() != bestandVorher) return Abbruch("Die Vorlage " + B.VORLAGE + " oder eine Konditionierungsvorlage hat sich verändert.");
if (B.Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?", B.VORBEHALTEN) != (platzhalter ? 0 : 1))
    return Abbruch("Die Nummer " + B.VORBEHALTEN + " steht nicht wie vorher.");
int geb = B.Gebaeude(id);
Console.WriteLine("Zeilen 1052:  " + string.Join(", ", B.Projekttabellen()
    .Select(p => (p.Tabelle, n: B.Zahl("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)));
Console.WriteLine("Zonen:        " + string.Join(", ", new GebaeudeZonenCtrl().LesenJeGebaeude(geb).Select(z =>
    z.Bezeichner + " " + z.Nutzflaeche?.ToString("0.##", CultureInfo.InvariantCulture) + " m² (" + z.Bauteile.Count + " Bauteile" +
    (z.IstBeheizt ? "" : ", unbeheizt") + ")")));
Console.WriteLine("Kalender:     " + B.Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ?", geb) +
                  ", Vorgaben " + B.Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude = ?", geb) +
                  ", Luftströme " + B.Zahl("SELECT COUNT(*) FROM Tab_Zonenluftstrom WHERE ID_ZoneA IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude = ?)", geb));
Console.WriteLine("integrity:    " + DataRepository.ExecuteScalar("PRAGMA integrity_check"));
long fk = B.Tabelle("PRAGMA foreign_key_check").Rows.Count;
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
