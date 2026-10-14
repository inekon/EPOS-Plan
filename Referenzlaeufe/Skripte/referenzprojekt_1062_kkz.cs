#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1062 "Referenzprojekt KK Zonen" an (RP-KKZ; Entwurf KK Abschnitt 4,
// E106 Q-KK-6 erweitert): eine Kopie des Referenzprojekts 1061 "Referenzprojekt KK" (Stufe AK3, Heizungspuffer,
// Raumeinfluss, Fahrplan, reversible Waermepumpe, Kuehlkurve am Gebaeude mit Raumeinfluss 3 K/K), das Gebaeude ueber
// die Wege des Zonendialogs in zwei Zonen geteilt.
//
// WOZU. Ohne 1062 rechnet kein Projekt der Testdatenbank ein Mehrzonengebaeude mit gekoppelter Kaelteseite: die
// Kuehluebergabe je Zone, die eine Kuehlkurve je Kuehlkreis am niedrigsten Kuehlsollwert und der Raumeinfluss der
// Kuehlkurve ueber die Zone mit der groessten Ueberschreitung stuenden ausserhalb des Regressionsnetzes. 1061 bleibt
// unveraendert.
//
// DER ZONENSCHNITT ("Gebaeude als eine Zone uebernehmen", GebaeudeZonenCtrl.Uebernahme, dann der OK-Weg des Dialogs,
// GebaeudeZonenCtrl.Schreiben). Die Uebernahme zerlegt Waende und Sonstiges in vier Viertel und die Fenster je
// Richtung; daraus entstehen zwei Zonen zu je der Haelfte der Nutzflaeche, des Dachs und der Bodenplatte:
//   1 "Süd und West"   alle Bauteile mit Azimut Sued oder West (Waende, Sonstiges, Fenster je zu 100 %) - die Zone
//                      mit den Nachmittagslasten; dazu die Trennwand zur Zone 2 (30 m², U 0,6 W/(m²K), eine Seite);
//   2 "Nord und Ost"   alle Bauteile mit Azimut Nord oder Ost - die Zone mit den Morgenlasten; Kuehluebergabe
//                      GEBLAESEKONVEKTOR (das Gebaeude kuehlt mit der Kuehldecke).
// So wechselt die fuehrende Zone der Kuehlkurve im Tageslauf. Alle uebrigen Zonenfelder bleiben leer (Wert des
// Gebaeudes); keine Zonenkalender, kein Luftstrom.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1061 -> 1062 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle Projekttabellen, keine
//      Rechenergebnisse. Die Kopie faellt auf MAX(Tab_Projekt.ID) + 1; vorausgesetzt ist 1061 als hoechste Projekt-ID.
//   2. Kopfzellen (Beschreibung, Kosten_Geaendert leer) und der Zonenschnitt ueber die Programmwege.
// Keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde Pruefwerte oder aus dem Gebaeude abgeleitet.
//
// EINFRIERREGEL. 1062 gehoert zu "gesaete Zonendaten" (Zonen, Bauteile, Trennflaeche, Kuehluebergabe der Zone) und
// - als Kopie von 1061 - zu "gesaete Auslegungsdaten der Uebergabe" (Kuehlkurve am Gebaeude); gehalten von
// EPOS.Kern.Tests/ZonenKuehlkurveReferenzprojektWacheTests. Die Basis R44 friert KK5b ein.
//
// WIEDERHOLBAR. Steht "Referenzprojekt KK Zonen" schon, aendert das Skript nichts (Rueckgabe 0, wenn es 1062 ist,
// sonst 2). Geschrieben wird in eine Arbeitsdatei neben der Datenbank, geprueft (zwei Zonen, integrity_check,
// foreign_key_check), und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1062_kkz.cs -- Referenzlaeufe/Kenndaten_Test.sqlite

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

const int VORLAGE = 1061;
const string VORLAGE_NAME = "Referenzprojekt KK";
const int NEU = 1062;
const string NAME = "Referenzprojekt KK Zonen";
const string BESCHREIBUNG = "Referenzprojekt KK Zonen: Kopie von Projekt 1061 (Stufe AK3, Kühlkurve mit Raumeinfluss 3 K/K), " +
                            "das Gebäude in zwei Zonen Süd/West und Nord/Ost geteilt, die Zone Nord/Ost kühlt mit Gebläsekonvektor - " +
                            "Kühlübergabe je Zone und Kühlkurve im Mehrzonenweg.";
const string ZONE_SW = "Süd und West", ZONE_NO = "Nord und Ost", TRENNWAND = "Trennwand Nord/Ost";
const double TRENNWAND_FLAECHE = 30.0, TRENNWAND_U = 0.6, ANTEIL = 0.5;
const int Z_SW = -1, Z_NO = -2;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1062_kkz.cs -- <Kenndaten_Test.sqlite>");
    return 2;
}
datei = Path.GetFullPath(datei);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

// Ohne Lizenz schreibt der Kern nur mit der Werkzeugfreigabe (Muster EPOS.Referenzlauf).
Schreibnaht.WerkzeugFreigabe("referenzprojekt_1062_kkz.cs (Saatskript der Testdatenbank)");
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
if (hoechste != VORLAGE) { Ende(); Console.Error.WriteLine("Die höchste Projekt-ID ist " + hoechste + " - erwartet " + VORLAGE); return 2; }

Ende();
string arbeit = datei + ".kkzarbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ? WHERE ID = ?", new DbParam("?", BESCHREIBUNG), new DbParam("?", id));

// Der Zonenschnitt ueber "Gebaeude als eine Zone uebernehmen" und den OK-Weg des Dialogs.
int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", new DbParam("?", id)),
                          CultureInfo.InvariantCulture);
int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?",
                                                            new DbParam("?", id), new DbParam("?", idZ)), CultureInfo.InvariantCulture);
GebaeudeZonenCtrl.Uebernahmevorschlag u = GebaeudeZonenCtrl.Uebernahme(id, idZ, null);
if (!u.Ok || u.Zone == null || !u.Zone.Nutzflaeche.HasValue) return Abbruch("Übernahme scheitert: " + u.Meldung);
double nutz = u.Zone.Nutzflaeche.Value;
bool SuedWest(BauteilModel b) => b.Azimut is double a && (Math.Abs(a - 180.0) < 1e-9 || Math.Abs(a - 270.0) < 1e-9);
bool NordOst(BauteilModel b) => b.Azimut is double a && (Math.Abs(a) < 1e-9 || Math.Abs(a - 90.0) < 1e-9);
List<BauteilModel> Teil(Func<BauteilModel, bool> richtung)
{
    var l = new List<BauteilModel>();
    foreach (BauteilModel b in u.Zone.Bauteile)
    {
        BauteilModel k = b.Kopie();
        k.ID = 0;
        if (b.Azimut.HasValue)
        {
            if (!richtung(b)) continue;
        }
        else
        {
            k.Flaeche = ANTEIL * b.Flaeche;     // Dach und Bodenplatte je zur Haelfte
            k.Psi_L = b.Psi_L.HasValue ? ANTEIL * b.Psi_L.Value : (double?)null;
        }
        l.Add(k);
    }
    return l;
}
if (u.Zone.Bauteile.Any(b => b.Azimut.HasValue && !SuedWest(b) && !NordOst(b)))
    return Abbruch("Die Übernahme trägt ein Bauteil mit einem Azimut außerhalb N/O/S/W.");
List<BauteilModel> sw = Teil(SuedWest);
sw.Add(new BauteilModel
{
    Bezeichner = TRENNWAND, Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = TRENNWAND_FLAECHE, U_Wert = TRENNWAND_U,
    Neigung = 90.0, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = Z_NO,
});
var zonen = new List<ZoneModel>
{
    new ZoneModel { ID = Z_SW, Bezeichner = ZONE_SW, Nutzflaeche = ANTEIL * nutz, Bauteile = sw },
    new ZoneModel { ID = Z_NO, Bezeichner = ZONE_NO, Nutzflaeche = ANTEIL * nutz, Bauteile = Teil(NordOst),
                    Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR },
};
var luft = new List<ZonenluftstromModel>();
string pruef = GebaeudeZonenCtrl.Pruefen(zonen, luft);
if (pruef != null) return Abbruch("Zonenprüfung: " + pruef);
GebaeudeZonenCtrl.Schreibergebnis e = new GebaeudeZonenCtrl().Schreiben(gebaeude, zonen, luft, null);
if (!e.Ok) return Abbruch("Zonen nicht geschrieben: " + e.Meldung);
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL WHERE ID = ?", new DbParam("?", id));

long nZonen = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = ?", new DbParam("?", gebaeude)),
                              CultureInfo.InvariantCulture);
long nBauteile = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Zone IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude = ?)",
                                                              new DbParam("?", gebaeude)), CultureInfo.InvariantCulture);
Console.WriteLine("Zonen:        " + nZonen + " (Gebäude " + gebaeude + "), Bauteile " + nBauteile + ", Nutzfläche je Zone " +
                  (ANTEIL * nutz).ToString("0.##", CultureInfo.InvariantCulture) + " m²");
if (nZonen != 2) return Abbruch("Statt zwei Zonen stehen " + nZonen + ".");
object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
if (DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count != 0) return Abbruch("foreign_key_check meldet Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt.");
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
