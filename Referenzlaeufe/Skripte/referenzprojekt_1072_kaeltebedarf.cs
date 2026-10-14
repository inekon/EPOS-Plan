#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1072 "Referenzprojekt Kältebedarf dezentral" an (K6c, Konzept
// Kältebedarf 4 und 8.1): eine Kopie des Referenzprojekts 1055 "Kältemaschine mit Kältespeicher" (Kältemaschine 20 kW
// mit Trockenkühler und eigenem Zähler auf Träger 58, Kaltwasserspeicher 2 m³, die Wärmepumpe heizt nur), an der Kopie
// drei Kältebedarfsprofile in Z_Projekt_Kaeltebedarf.
//
// WOZU. Ohne 1072 stünden die Profilbuchung des Kältebedarfs, die dezentrale Deckung über Split-Geräte (EER fest und
// linear), der anteilige Netzbezug des Split-Stroms über den Projektstromträger, der eigene Zähler auf einem
// abweichenden Kühlträger und die Ergebniszeilen je Split-Zeile (Tab_ErgebnisKaelteDezentral) außerhalb des
// Regressionsnetzes. Das Paar 1055/1072 zeigt die Wirkung allein der drei Profilzeilen.
//
// DIE GESÄTEN ZEILEN (Projektkopien aus dem Katalog über den Kopierweg des Programms, KaeltebedarfStammCtrl.CopyFromStamm,
// danach umbenannt; Zuordnung über WizardCtrl.Add_Projekt_Kaelte):
//   (a) "Büro Kälteprofil"   Katalog "Raumkühlung Büro"  1,0 MWh/a  zentral (Kältefolge: Kältemaschine, Kältespeicher)
//   (b) "Split Büro"         Katalog "Raumkühlung Büro"  3,0 MWh/a  split, EER fest 3,0 (Bemessung 35 °C),
//                                                                    Kühlträger leer = Stromträger des Projekts, anteilig
//   (c) "Split Serverraum"   Katalog "Serverraum"        2,0 MWh/a  split, EER linear 4,0 bei 10 °C / 2,5 bei 35 °C,
//                                                                    Kühlträger 58 "Elektrische Energie 2", eigener Zähler
// Art aller drei Zeilen: Raum; kein Betriebskalender. Tab_Einstellungen.Kuehlbetrieb = 1 wie 1055. Die Summen bleiben
// klein, damit die Kältemaschine (20 kW, Gebäudespitze 15 kW) nicht an ihre Grenze läuft; der Probelauf prüft das.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1055 -> 1072 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle Projekttabellen, keine
//      Rechenergebnisse. Die Kopie fällt auf MAX(Tab_Projekt.ID) + 1; vorausgesetzt ist 1071 als höchste Projekt-ID.
//   2. Je Zeile die Projektkopie des Katalogsatzes samt Typprofil (ID_Stamm), umbenannt, und die Zuordnung.
//   3. Kopfzellen (Beschreibung, Kosten_Geaendert leer).
//   4. Prüfung: jede gesäte Zelle, je Projekttabelle so viele Zeilen wie 1055 (außer den drei Kältebedarfstabellen mit je
//      drei Zeilen), integrity_check, foreign_key_check.
//   5. Probelauf auf einer Wegwerfkopie: Kälte voll gedeckt, zwei Zeilen in Tab_ErgebnisKaelteDezentral, kein Befund
//      der dezentralen Kälte.
// Keine Katalogzeile, kein VACUUM.
//
// EINFRIERREGEL. 1072 gehört zu "gesäte Kältebedarfs-Zuordnungen eines Referenzprojekts" und - als Kopie von 1055 - zu
// "gesäte Kältemaschinendaten"; gehalten von EPOS.Kern.Tests/KaelteDezentralReferenzprojektWacheTests.
//
// WIEDERHOLBAR. Steht das Projekt schon, ändert das Skript nichts (Rückgabe 0, wenn es 1072 ist und die Zellen
// stimmen, sonst 2). Mit --trocken wird nur die Vorlage geprüft. Geschrieben wird in eine Arbeitsdatei neben der
// Datenbank, geprüft, und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1072_kaeltebedarf.cs -- Referenzlaeufe/Kenndaten_Test.sqlite [--trocken]

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

const int VORLAGE = 1055;
const string VORLAGE_NAME = "Kältemaschine mit Kältespeicher";
const int VORHER_HOECHSTE = 1071;
const int NEU = 1072;
const int KUEHLTRAEGER = 58;
const string NAME = "Referenzprojekt Kältebedarf dezentral";
const string BESCHREIBUNG = "Referenzprojekt Kältebedarf dezentral: Kopie von Projekt 1055 mit drei Kältebedarfsprofilen - " +
                            "„Büro Kälteprofil“ zentral über Kältemaschine und Kältespeicher, „Split Büro“ dezentral mit EER fest 3,0 " +
                            "anteilig über den Stromträger des Projekts, „Split Serverraum“ dezentral mit EER linear (4,0 bei 10 °C, " +
                            "2,5 bei 35 °C) auf dem Kühlträger 58 mit eigenem Zähler.";

// Die drei Zeilen: Katalogsatz, Name der Projektkopie, Summe [MWh/a], Deckung, EER-Weg, EER/T am Punkt 1 und 2, Träger, Zähler.
var zeilen = new List<Zeile>
{
    new Zeile("Raumkühlung Büro", "Büro Kälteprofil", 1.0, KaeltebedarfSchema.DECKUNG_ZENTRAL, null, null, null, null, null, null, false),
    new Zeile("Raumkühlung Büro", "Split Büro", 3.0, KaeltebedarfSchema.DECKUNG_SPLIT, KaeltebedarfSchema.EER_FEST, 3.0, 35.0, null, null, null, false),
    new Zeile("Serverraum", "Split Serverraum", 2.0, KaeltebedarfSchema.DECKUNG_SPLIT, KaeltebedarfSchema.EER_LINEAR, 4.0, 10.0, 2.5, 35.0, KUEHLTRAEGER, true),
};
var kaelteTabellen = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{ KaeltebedarfSchema.TAB_ZUORDNUNG, KaeltebedarfSchema.TAB_KOPF, KaeltebedarfSchema.TAB_TYP };

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1072_kaeltebedarf.cs -- <Kenndaten_Test.sqlite> [--trocken]");
    return 2;
}
datei = Path.GetFullPath(datei);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

// Ohne Lizenz schreibt der Kern nur mit der Werkzeugfreigabe (Muster EPOS.Referenzlauf).
Schreibnaht.WerkzeugFreigabe("referenzprojekt_1072_kaeltebedarf.cs (Saatskript der Testdatenbank)");
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
// 1055 rechnet ohne Kältebedarfsprofil, mit Kühlbetrieb.
if (Zahl("SELECT COUNT(*) FROM " + KaeltebedarfSchema.TAB_ZUORDNUNG + " WHERE ID_Projekt = ?", VORLAGE) != 0 ||
    Zahl("SELECT COUNT(*) FROM " + KaeltebedarfSchema.TAB_KOPF + " WHERE ID_Projekt = ?", VORLAGE) != 0)
{ Ende(); Console.Error.WriteLine("Die Vorlage " + VORLAGE + " führt schon Kältebedarfsprofile."); return 2; }
if (Zahl("SELECT Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE) != 1)
{ Ende(); Console.Error.WriteLine("Die Vorlage " + VORLAGE + " rechnet ohne Kühlbetrieb."); return 2; }
// Die Katalogsätze und der Kühlträger stehen.
foreach (string kat in zeilen.Select(z => z.Katalog).Distinct())
    if (Zahl("SELECT COUNT(*) FROM " + KaeltebedarfSchema.TAB_KOPF_STAMM + " WHERE Bezeichner = ? AND ReadOnly = 1", kat) != 1)
    { Ende(); Console.Error.WriteLine("Der Katalogsatz '" + kat + "' fehlt."); return 2; }
if (Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ? AND \"ID_Energieträger\" = ?", VORLAGE, KUEHLTRAEGER) != 1)
{ Ende(); Console.Error.WriteLine("Der Kühlträger " + KUEHLTRAEGER + " ist kein Träger der Vorlage."); return 2; }
if (trocken) { Ende(); Console.WriteLine("--trocken: Vorlage passt, Projekt " + NEU + " würde angelegt."); return 0; }

Ende();
string arbeit = datei + ".k6carbeit";
string probe = datei + ".k6cprobe";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm", probe, probe + "-wal", probe + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");

// Je Zeile: Projektkopie des Katalogsatzes (Kopierweg des Programms), umbenannt, dann die Zuordnung.
var modelle = new List<Z_ProjektKaeltebedarfModel>();
foreach (Zeile z in zeilen)
{
    int kopie = KaeltebedarfStammCtrl.CopyFromStamm(z.Katalog, id);
    if (kopie <= 0) return Abbruch("Die Projektkopie von '" + z.Katalog + "' ist gescheitert.");
    DataRepository.ExecuteNonQuery("UPDATE " + KaeltebedarfSchema.TAB_KOPF + " SET Bezeichner = ? WHERE ID = ? AND ID_Projekt = ?",
                                   new DbParam("?", z.Name), new DbParam("?", kopie), new DbParam("?", id));
    modelle.Add(new Z_ProjektKaeltebedarfModel
    {
        ID_Kaeltebedarf = kopie, Bezeichner = z.Name, Summe = z.SummeMwh, Deckung = z.Deckung, EerWeg = z.EerWeg,
        Eer1 = z.Eer1, Taussen1 = z.T1, Eer2 = z.Eer2, Taussen2 = z.T2, KuehlIdCarrier = z.Traeger, KuehlEigenerZaehler = z.Zaehler,
        KaelteArt = KaelteArtSchema.RAUM,
    });
    Console.WriteLine("  Projektkopie " + kopie + " '" + z.Name + "' aus '" + z.Katalog + "'");
}
if (!new WizardCtrl().Add_Projekt_Kaelte(id, modelle)) return Abbruch("Die Zuordnungen wurden nicht gespeichert.");
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Kosten_Geaendert = NULL WHERE ID = ?",
                               new DbParam("?", BESCHREIBUNG), new DbParam("?", id));

string fehler = Abweichung(id);
if (fehler != null) return Abbruch(fehler);

// Gegenprobe: je Projekttabelle (Spalte ID_Projekt) so viele Zeilen wie die Vorlage, die Kältebedarfstabellen mit drei.
var tabellen = DataRepository.GetDataTable("SELECT m.name FROM sqlite_master m WHERE m.type = 'table' AND EXISTS " +
                                           "(SELECT 1 FROM pragma_table_info(m.name) p WHERE p.name = 'ID_Projekt') ORDER BY m.name")
                             .Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToList();
foreach (string t in tabellen)
{
    if (t.StartsWith("Tab_Ergebnis", StringComparison.OrdinalIgnoreCase) || t.Equals("Tab_Projekt", StringComparison.OrdinalIgnoreCase)) continue;
    long nV = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", VORLAGE);
    long nN = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", id);
    long soll = kaelteTabellen.Contains(t) ? nV + zeilen.Count : nV;
    if (nN != soll) return Abbruch(t + ": " + nN + " Zeilen statt " + soll);
    if (nN > 0) Console.WriteLine("  Zeilen " + t.PadRight(34) + nN);
}
object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
if (DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count != 0) return Abbruch("foreign_key_check meldet Zeilen.");
Ende();

// Probelauf auf einer Wegwerfkopie - die Arbeitsdatei bleibt ohne Rechenergebnis.
File.Copy(arbeit, probe);
DataRepository.PfadUeberschreibung = probe;
string probeFehler = Probelauf(id);
Ende();
foreach (string a in new[] { probe, probe + "-wal", probe + "-shm" }) if (File.Exists(a)) File.Delete(a);
if (probeFehler != null) return Abbruch("Probelauf: " + probeFehler);

File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt; integrity_check ok, foreign_key_check leer.");
return 0;

long Zahl(string sql, params object[] w) => Convert.ToInt64(DataRepository.ExecuteScalar(sql, w.Select(x => new DbParam("?", x)).ToArray()), CultureInfo.InvariantCulture);

string Text(object o) => o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

bool Gleich(object ist, object soll) => soll == null ? ist == DBNull.Value
    : ist != DBNull.Value && Convert.ToString(ist, CultureInfo.InvariantCulture) == Convert.ToString(soll, CultureInfo.InvariantCulture);

// Die erste Abweichung der gesäten Zellen eines Projekts, sonst null.
string Abweichung(int projekt)
{
    DataRow p = DataRepository.GetDataTable("SELECT Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", new DbParam("?", projekt)).Rows[0];
    if (Text(p["Beschreibung"]) != BESCHREIBUNG) return "Beschreibung weicht ab";
    if (p["Kosten_Geaendert"] != DBNull.Value) return "Kosten_Geaendert nicht leer";
    if (Zahl("SELECT Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = ?", projekt) != 1) return "Kuehlbetrieb nicht 1";
    DataTable t = DataRepository.GetDataTable(
        "SELECT z.Summe, z.ID_Betriebskalender, z.Deckung, z.Split_EER_Weg, z.Split_EER_1, z.Split_Taussen_1, z.Split_EER_2, " +
        "z.Split_Taussen_2, z.Kuehl_ID_Carrier, z.Kuehl_EigenerZaehler, z.Kaelte_Art, z.Bezeichner AS ZBez, k.Bezeichner AS KBez, " +
        "k.ID_Projekt AS KProj, s.Bezeichner AS SBez, (SELECT COUNT(*) FROM " + KaeltebedarfSchema.TAB_TYP + " y WHERE y.ID_Kaeltebedarf = k.ID " +
        "AND y.ID_Projekt = k.ID_Projekt) AS Typen FROM " + KaeltebedarfSchema.TAB_ZUORDNUNG + " z INNER JOIN " + KaeltebedarfSchema.TAB_KOPF +
        " k ON z.ID_Kaeltebedarf = k.ID LEFT JOIN " + KaeltebedarfSchema.TAB_KOPF_STAMM + " s ON k.ID_Stamm = s.ID WHERE z.ID_Projekt = ? ORDER BY z.ID",
        new DbParam("?", projekt));
    if (t.Rows.Count != zeilen.Count) return t.Rows.Count + " Zuordnungen statt " + zeilen.Count;
    for (int i = 0; i < zeilen.Count; i++)
    {
        Zeile s = zeilen[i];
        DataRow r = t.Rows[i];
        var soll = new (string Spalte, object Wert)[]
        {
            ("ZBez", s.Name), ("KBez", s.Name), ("SBez", s.Katalog), ("KProj", (long)projekt), ("Typen", 1L),
            ("Summe", s.SummeMwh), ("ID_Betriebskalender", null), ("Deckung", s.Deckung), ("Split_EER_Weg", s.EerWeg),
            ("Split_EER_1", s.Eer1), ("Split_Taussen_1", s.T1), ("Split_EER_2", s.Eer2), ("Split_Taussen_2", s.T2),
            ("Kuehl_ID_Carrier", s.Traeger), ("Kuehl_EigenerZaehler", s.Zaehler ? 1L : 0L), ("Kaelte_Art", KaelteArtSchema.RAUM),
        };
        foreach (var (spalte, wert) in soll)
            if (!Gleich(r[spalte], wert))
                return s.Name + ": " + spalte + " = " + (Text(r[spalte]) ?? "leer") + " statt " + (Text(wert) ?? "leer");
    }
    return null;
}

// Der Probelauf: Kälte voll gedeckt, zwei Split-Ergebniszeilen, die Hauptzahl gesetzt, kein Befund der dezentralen Kälte.
string Probelauf(int projekt)
{
    var lauf = new SimulationRunner();
    int kopf = lauf.SimuliereUndSpeichere(projekt, out string f);
    if (kopf <= 0) return "Lauf gescheitert: " + f;
    Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf?.Kaskade;
    if (kaskade == null) return "keine Kältekaskade";
    Console.WriteLine("  Probelauf: Kältefolge Bedarf {0:F1} kWh, Rest {1:F3} kWh", kaskade.BedarfGesamtKwh, kaskade.RestGesamtKwh);
    if (kaskade.RestGesamtKwh > 1e-3 * kaskade.BedarfGesamtKwh) return "Unterdeckung der Kältefolge " + kaskade.RestGesamtKwh.ToString("F1", CultureInfo.InvariantCulture) + " kWh";
    long split = Zahl("SELECT COUNT(*) FROM Tab_ErgebnisKaelteDezentral WHERE ID_Ergebnis = ?", kopf);
    if (split != 2) return split + " Zeilen in Tab_ErgebnisKaelteDezentral statt 2";
    SimulationErgebnisCtrl.KaelteErgebnis e = SimulationErgebnisCtrl.Kaelte(lauf.simulation_Waermebedarf);
    if (e.DeckungsgradGesamtProzent == null) return "keine Hauptzahl des Deckungsgrads";
    Console.WriteLine("  Probelauf: Kältebedarf {0:F3} MWh, dezentral {1:F3} MWh, Strom dezentral {2:F3} MWh, Deckungsgrad {3:F1} %, davon Kältefolge {4:F1} %",
                      e.KaeltebedarfMwh, e.DezentralMwh, e.DezentralStromMwh, e.DeckungsgradGesamtProzent, e.DeckungsgradKaeltefolgeProzent);
    var befunde = new List<Warnbefund>();
    Warnkriterien.DezentraleKaeltePruefen(projekt, befunde);
    if (befunde.Count != 0) return befunde.Count + " Befunde der dezentralen Kälte";
    return null;
}

int Abbruch(string grund)
{
    Ende();
    foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm", probe, probe + "-wal", probe + "-shm" }) if (File.Exists(a)) File.Delete(a);
    Console.Error.WriteLine("Abbruch, die Datenbank bleibt unverändert: " + grund);
    return 2;
}

void Ende()
{
    DataRepository.PfadUeberschreibung = null;
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
}

record Zeile(string Katalog, string Name, double SummeMwh, string Deckung, string EerWeg, double? Eer1, double? T1,
             double? Eer2, double? T2, int? Traeger, bool Zaehler);
