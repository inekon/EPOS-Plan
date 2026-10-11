#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1063 "Referenzprojekt Kältemaschine Teillast" an (KM3, Fachkonzept
// Teillast und Takten der Kaeltemaschine, Abschnitt 8.2): eine Kopie des Referenzprojekts 1055 "Kältemaschine mit
// Kältespeicher" (Kaeltemaschine 20 kW mit Trockenkuehler und eigenem Zaehler, Kaltwasserspeicher 2 m3, 6/12 Grad C),
// an der Projektkopie der Kaeltemaschine die Teillastdaten gesetzt.
//
// WOZU. Mit leerem Teillast_Weg rechnet jede Kaeltemaschine den Bestandsweg (linear, Takten ohne Verlust, Randwert an
// den Kennfeldraendern); ohne 1063 stuenden Lastachse (EIRFPLR), Taktverlust unter der Mindestteillast und
// Guetegrad-Extrapolation ausserhalb des Regressionsnetzes. 1055 bleibt unveraendert; das Paar 1055/1063 zeigt die
// Wirkung allein der Teillastdaten.
//
// DIE GESAETEN WERTE (Tab_Kaeltemaschine der Kopie, Fachkonzept 8.2):
//   Teillast_Weg                KURVE              Lastachse wirksam
//   Teillastkurve_a/b/c         0,10 / 0,60 / 0,30 Kurve des Zahlenbeispiels 3.2 (g(0,5) = 1,05)
//   Teillastkurve_Lastgrad_Min  0,2                untere Gueltigkeit der Kurve
//   Taktverlustfaktor_Cd        leer               prueft die Vorgabe 0,9
//   Mindestteillast_Prozent     30 (1055: 20)      Taktstunden sichtbar
//   Verdichterregelung          STUFEN             Anzeige; die gepflegten Beiwerte gehen der Vorgabekurve vor
//   Kennfeld_Randweg            GUETEGRAD          der Trockenkuehler laeuft im Winter unter 25 Grad C Rueckkuehlung
// Alle uebrigen Zellen wie 1055 (Kennlinie, Kaeltespeicher, Kuehltraeger mit eigenem Zaehler, Kaskade).
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1055 -> 1063 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle Projekttabellen, keine
//      Rechenergebnisse. Die Kopie faellt auf MAX(Tab_Projekt.ID) + 1; vorausgesetzt ist 1062 als hoechste Projekt-ID.
//   2. Kopfzellen (Beschreibung, Kosten_Geaendert leer) und die sieben Teillastzellen der Kaeltemaschine.
//   3. Pruefung: jede gesaete Zelle, eine Kaeltemaschine und ein Kaeltespeicher wie in 1055, integrity_check,
//      foreign_key_check.
// Keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde Pruefwerte.
//
// EINFRIERREGEL. 1063 gehoert zu "gesaete Teillastdaten einer Kaeltemaschine eines Referenzprojekts" und - als Kopie
// von 1055 - zu "gesaete Kaeltemaschinendaten"; gehalten von
// EPOS.Kern.Tests/KaeltemaschineTeillastReferenzprojektWacheTests.
//
// WIEDERHOLBAR. Steht das Projekt schon, aendert das Skript nichts (Rueckgabe 0, wenn es 1063 ist, sonst 2).
// Geschrieben wird in eine Arbeitsdatei neben der Datenbank, geprueft, und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1063_kaeltemaschine_teillast.cs -- Referenzlaeufe/Kenndaten_Test.sqlite

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

const int VORLAGE = 1055;
const string VORLAGE_NAME = "Kältemaschine mit Kältespeicher";
const int VORHER_HOECHSTE = 1062;
const int NEU = 1063;
const string NAME = "Referenzprojekt Kältemaschine Teillast";
const string BESCHREIBUNG = "Referenzprojekt Kältemaschine Teillast: Kopie von Projekt 1055, die Kältemaschine rechnet mit " +
                            "Teillastkurve (0,10/0,60/0,30, gültig ab Lastgrad 0,2), Mindestteillast 30 %, Takten mit dem " +
                            "Vorgabe-Taktverlustfaktor, Verdichterregelung Stufen und Gütegrad-Extrapolation an den Kennfeldrändern.";

// Die gesaeten Zellen der Projektkopie (null = leer).
var soll = new List<(string Spalte, object Wert)>
{
    (KaeltemaschineTeillastSchema.SPALTE_TEILLAST_WEG, KaeltemaschineTeillastSchema.WEG_KURVE),
    (KaeltemaschineTeillastSchema.SPALTE_KURVE_A, 0.10),
    (KaeltemaschineTeillastSchema.SPALTE_KURVE_B, 0.60),
    (KaeltemaschineTeillastSchema.SPALTE_KURVE_C, 0.30),
    (KaeltemaschineTeillastSchema.SPALTE_KURVE_LASTGRAD_MIN, 0.2),
    (KaeltemaschineTeillastSchema.SPALTE_CD, null),
    ("Mindestteillast_Prozent", 30.0),
    (KaeltemaschineTeillastSchema.SPALTE_VERDICHTERREGELUNG, KaeltemaschineTeillastSchema.REGELUNG_STUFEN),
    (KaeltemaschineTeillastSchema.SPALTE_RANDWEG, KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD),
};

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1063_kaeltemaschine_teillast.cs -- <Kenndaten_Test.sqlite>");
    return 2;
}
datei = Path.GetFullPath(datei);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

// Ohne Lizenz schreibt der Kern nur mit der Werkzeugfreigabe (Muster EPOS.Referenzlauf).
Schreibnaht.WerkzeugFreigabe("referenzprojekt_1063_kaeltemaschine_teillast.cs (Saatskript der Testdatenbank)");
DataRepository.PfadUeberschreibung = datei;
object vorhanden = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", NAME));
if (vorhanden != null && vorhanden != DBNull.Value)
{
    Ende();
    int v = Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
    Console.WriteLine("Projekt " + v + " '" + NAME + "' steht schon - nichts zu tun.");
    return v == NEU ? 0 : 2;
}
long hoechste = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Projekt"), CultureInfo.InvariantCulture);
if (hoechste != VORHER_HOECHSTE) { Ende(); Console.Error.WriteLine("Die höchste Projekt-ID ist " + hoechste + " - erwartet " + VORHER_HOECHSTE); return 2; }
long kmVorlage = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ? AND Teillast_Weg IS NULL",
                                                              new DbParam("?", VORLAGE)), CultureInfo.InvariantCulture);
if (kmVorlage != 1) { Ende(); Console.Error.WriteLine("Die Vorlage " + VORLAGE + " führt " + kmVorlage + " Kältemaschinen ohne Teillastweg statt einer."); return 2; }

Ende();
string arbeit = datei + ".km3arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ? WHERE ID = ?", new DbParam("?", BESCHREIBUNG), new DbParam("?", id));

object kmId = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", new DbParam("?", id));
long nKm = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", new DbParam("?", id)),
                           CultureInfo.InvariantCulture);
if (nKm != 1 || kmId == null) return Abbruch("Die Kopie führt " + nKm + " Kältemaschinen statt einer.");
foreach (var (spalte, wert) in soll)
    DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine SET " + spalte + " = ? WHERE ID = ?",
                                   new DbParam("?", wert ?? (object)DBNull.Value), new DbParam("?", kmId));
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL WHERE ID = ?", new DbParam("?", id));

// Pruefung der gesaeten Zellen.
var zeile = DataRepository.GetDataTable("SELECT " + string.Join(", ", soll.Select(s => s.Spalte)) + " FROM Tab_Kaeltemaschine WHERE ID = ?",
                                        new DbParam("?", kmId)).Rows[0];
foreach (var (spalte, wert) in soll)
{
    object ist = zeile[spalte];
    bool gleich = wert == null ? ist == DBNull.Value
        : wert is double d ? ist != DBNull.Value && Math.Abs(Convert.ToDouble(ist, CultureInfo.InvariantCulture) - d) < 1e-12
        : Equals(Convert.ToString(ist, CultureInfo.InvariantCulture), wert);
    Console.WriteLine("  " + spalte.PadRight(28) + (ist == DBNull.Value ? "leer" : Convert.ToString(ist, CultureInfo.InvariantCulture)));
    if (!gleich) return Abbruch(spalte + " = " + ist + " statt " + (wert ?? "leer"));
}
long nKalt = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Verwendung = 'Kaelte'",
                                                           new DbParam("?", id)), CultureInfo.InvariantCulture);
long nPunkte = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ?",
                                                            new DbParam("?", kmId)), CultureInfo.InvariantCulture);
Console.WriteLine("Kältemaschine " + kmId + ": " + nPunkte + " Kennlinienpunkte; Kältespeicher " + nKalt);
if (nKalt != 1 || nPunkte != 6) return Abbruch("Kältespeicher oder Kennlinie weichen von 1055 ab.");
object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
if (DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count != 0) return Abbruch("foreign_key_check meldet Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt; integrity_check ok.");
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
