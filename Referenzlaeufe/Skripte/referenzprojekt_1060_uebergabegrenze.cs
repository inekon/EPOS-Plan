#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1060 "Referenzprojekt Übergabegrenze" an (UB-E2-d; Umsetzungskonzept
// Uebergabegrenze und Bivalenz 4.4, Fachkonzept 5.3): eine Kopie des Referenzprojekts 1056 "Referenz Kopplung mit
// Fahrplan" (Stufe AK1, Heizkreis mit Radiator und Heizkurve am Gebaeude, Profilweg), in der allein die Uebergabegrenze
// der Waermepumpe wirkt.
//
// WOZU. Ohne 1060 rechnet kein Projekt der Testdatenbank die Betriebsbereiche B1 bis B4: die Einbindung ist in allen
// Bestandsprojekten leer (Bestandsweg, U-1), die Bereichsrechnung, die Bivalenzpunkte, die groesste Uebergabe und die
// Ruecklaufstufe "Vorwaermer" des Kessels stuenden ausserhalb des Regressionsnetzes. 1056 bleibt unveraendert.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1056 -> 1060 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle Projekttabellen, keine
//      Rechenergebnisse. Die Kopie faellt dort auf die naechste freie Projekt-ID (heute 1063); EINZIGE ABWEICHUNG vom
//      Kopierweg: Die Projekt-ID wird danach auf die vorgemerkte 1060 umnummeriert - jede Spalte ID_Projekt, ProjektID,
//      ID_ProjektRef und ID_Referenzprojekt, die auf die Kopie zeigt, bei abgeschalteten Fremdschluesseln, danach
//      foreign_key_check. Vorausgesetzt ist, dass keine Zeile der Datenbank auf 1060 zeigt.
//   2. Die Uebergabe am Gebaeude: Heizkoerper (RADIATOR wie 1056) mit dem Auslegungspunkt 75/60 °C und dem Exponenten
//      1,3; Auslegungsraum- und -aussentemperatur, Nennleistung und Heizkurve bleiben wie in 1056 (leer = Vorgabe).
//   3. Die Waermepumpe: Vorlauf_Max 55 °C, Einbindung DIREKT, Vorwaermbetrieb 1, bivalenter Betrieb mit Betriebsart
//      Parallelbetrieb ohne Abschaltpunkt; Nachtsperre aufgehoben (Sperrung 0, Sperrzeit 0 bis 0, keine Sperrfenster).
//      Die acht Geraetespalten der Projektkopie (Spreizung_*, Mindestvolumenstrom_Prozent, Ruecklauf_Max,
//      Ruecklauf_Bezug, Ruecklauf_Abwertung_ProzentJeK, Kaeltemittel) bleiben leer = Vorgabe.
//   4. Kessel und BHKW: Zeitprogramm leer, Vorlauf_Max wie Bestand (leer). Der Kessel der Kopie wird ein neutraler
//      Gas-Brennwertkessel (Projektkopie, Ptherm 28 kW wie 1056, Erdgas E, Brennwert und Brennwertkennlinie ein, die
//      Kennlinienspalten leer = Normvorgabe) - nur ein Kessel mit Brennwertkennlinie fuehrt die Ruecklaufstufe
//      "Vorwaermer"; der Elektrokessel von 1056 fuehrte keine.
//   5. Die Kaskade: Waermepumpe vor Kessel (Tool_1 Waermepumpe, Tool_2 Heizkessel, Tool_3 BHKW); Stromspeicher und
//      Kopplungsstufe AK1 wie 1056.
// Keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde Pruefwerte; Name und Beschreibung ohne Produktdaten.
//
// EINFRIERREGEL. 1060 gehoert zu "gesaete Uebergabegrenzdaten" und - als Kopie von 1056 - zu "gesaete Auslegungsdaten
// der Uebergabe"; gehalten von EPOS.Kern.Tests/UebergabegrenzeReferenzprojektWacheTests. Die Basis R45 friert es ein.
//
// WIEDERHOLBAR. Steht "Referenzprojekt Übergabegrenze" schon, aendert das Skript nichts (Rueckgabe 0, wenn es 1060 ist,
// sonst 2). Geschrieben wird in eine Arbeitsdatei neben der Datenbank, geprueft (integrity_check, foreign_key_check),
// und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1060_uebergabegrenze.cs -- Referenzlaeufe/Kenndaten_Test.sqlite

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;

const int VORLAGE = 1056;
const string VORLAGE_NAME = "Referenz Kopplung mit Fahrplan";
const int NEU = 1060;
const string NAME = "Referenzprojekt Übergabegrenze";
const string BESCHREIBUNG = "Referenzprojekt Übergabegrenze: Kopie von Projekt 1056 (Stufe AK1, Heizkreis mit Heizkurve) ohne Fahrplan - " +
                            "Heizkörper 75/60 °C, Höchstvorlauf der Wärmepumpe 55 °C, Einbindung direkt, Parallelbetrieb mit " +
                            "Vorwärmbetrieb, Brennwertkessel in Reihe hinter der Wärmepumpe.";
const string KESSEL_NAME = "Brennwertkessel 28 kW";
string[] PROJEKTSPALTEN = { "ID_Projekt", "ProjektID", "ID_ProjektRef", "ID_Referenzprojekt" };

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1060_uebergabegrenze.cs -- <Kenndaten_Test.sqlite>");
    return 2;
}
datei = Path.GetFullPath(datei);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

// Ohne Lizenz schreibt der Kern nur mit der Werkzeugfreigabe (Muster EPOS.Referenzlauf).
Schreibnaht.WerkzeugFreigabe("referenzprojekt_1060_uebergabegrenze.cs (Saatskript der Testdatenbank)");
DataRepository.PfadUeberschreibung = datei;
object vorhanden = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", NAME));
if (vorhanden != null && vorhanden != DBNull.Value)
{
    Ende();
    int v = Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
    Console.WriteLine("Projekt " + v + " '" + NAME + "' steht schon - nichts zu tun.");
    return v == NEU ? 0 : 2;
}
Ende();

string arbeit = datei + ".ubarbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);

// Projektspalten aller Tabellen; keine Zeile darf auf 1060 zeigen.
var spalten = new List<(string Tabelle, string Spalte)>();
using (var con = Verbindung())
{
    var tabellen = new List<string>();
    using (var cmd = con.CreateCommand())
    {
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
        using var r = cmd.ExecuteReader();
        while (r.Read()) tabellen.Add(r.GetString(0));
    }
    foreach (string t in tabellen)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT name FROM pragma_table_info(@t)";
        cmd.Parameters.AddWithValue("@t", t);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            string s = r.GetString(0);
            if (PROJEKTSPALTEN.Any(p => p.Equals(s, StringComparison.OrdinalIgnoreCase))) spalten.Add((t, s));
        }
    }
    foreach (var (t, s) in spalten.Append(("Tab_Projekt", "ID")))
        if (Zahl(con, "SELECT COUNT(*) FROM [" + t + "] WHERE [" + s + "] = " + NEU) != 0)
            return Abbruch("Die ID " + NEU + " ist belegt: " + t + "." + s);
}

DataRepository.PfadUeberschreibung = arbeit;
int kopie = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Ende();
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + kopie);
if (kopie <= 0) return Abbruch("Die Kopie scheiterte.");

using (var con = Verbindung())
{
    Ausfuehren(con, "PRAGMA foreign_keys = OFF");
    using (var tx = con.BeginTransaction())
    {
        long zeilen = 0;
        foreach (var (t, s) in spalten)
            zeilen += Ausfuehren(con, "UPDATE [" + t + "] SET [" + s + "] = " + NEU + " WHERE [" + s + "] = " + kopie, tx);
        zeilen += Ausfuehren(con, "UPDATE Tab_Projekt SET ID = " + NEU + " WHERE ID = " + kopie, tx);
        Ausfuehren(con, "UPDATE sqlite_sequence SET seq = (SELECT MAX(ID) FROM Tab_Projekt) WHERE name = 'Tab_Projekt'", tx);
        tx.Commit();
        Console.WriteLine("Umnummeriert: " + kopie + " -> " + NEU + " (" + zeilen + " Zellen)");
    }
    Ausfuehren(con, "PRAGMA foreign_keys = ON");
}

DataRepository.PfadUeberschreibung = arbeit;
DbParam P(object w) => new DbParam("?", w);
DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Kosten_Geaendert = NULL WHERE ID = ?", P(BESCHREIBUNG), P(NEU));

// 2. Uebergabe am Gebaeude.
int geb = DataRepository.ExecuteNonQuery(
    "UPDATE Tab_Gebaeude SET Uebergabe_Art = 'RADIATOR', Auslegung_Vorlauf = 75, Auslegung_Ruecklauf = 60, Uebergabe_Exponent = 1.3 " +
    "WHERE ID_Projekt = ? AND Heizkreis_Aktiv = 1", P(NEU));
if (geb != 1) return Abbruch("Statt eines Gebäudes mit Heizkreis stehen " + geb + ".");

// 3. Waermepumpe.
int wp = DataRepository.ExecuteNonQuery(
    "UPDATE Tab_Energieanlagen SET Vorlauf_Max = 55, Einbindung = 'DIREKT', Vorwaermbetrieb = 1, Bivalenter_Betrieb = 1, " +
    "Betriebsart = ?, Abschaltpunkt = NULL, Sperrung = 0, Sperrzeit_von = 0, Sperrzeit_bis = 0, Zeitprogramm = NULL " +
    "WHERE ID_Projekt = ? AND ID_Type = 1", P(DbWerte.WP_BETRIEBSART_PARALLEL), P(NEU));
if (wp != 1) return Abbruch("Statt einer Wärmepumpe stehen " + wp + ".");
DataRepository.ExecuteNonQuery("DELETE FROM Tab_Sperrfenster WHERE ID_Energieanlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)", P(NEU));

// 4. Kessel und BHKW ohne Zeitprogramm; der Kessel als neutraler Gas-Brennwertkessel.
DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET Zeitprogramm = NULL, Sperrung = 0 WHERE ID_Projekt = ? AND ID_Type IN (10, 11)", P(NEU));
int kessel = DataRepository.ExecuteNonQuery(
    "UPDATE Tab_Heizkessel SET Bezeichner = ?, Firma = NULL, Beschreibung = 'Brennwert-Kessel', Brennstoff = 3, Wirkungsgrad_Gas = 0.97, " +
    "Brennwert = 1, Kennlinie_Brennwert = 1, Wirkungsgrad_Teillast30 = NULL, Mindestleistung = NULL, Anfahrverlust_kWh = NULL, " +
    "Mindestlaufzeit_min = NULL WHERE ID_Projekt = ?", P(KESSEL_NAME), P(NEU));
if (kessel != 1) return Abbruch("Statt eines Kessels stehen " + kessel + ".");
DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET Bezeichner = ? WHERE ID_Projekt = ? AND ID_Type = 10", P(KESSEL_NAME), P(NEU));

// 5. Kaskade: Waermepumpe vor Kessel, BHKW danach.
DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Tool_1 = 'Wärmepumpe', Tool_2 = 'Heizkessel', Tool_3 = 'BHKW' WHERE ID_Projekt = ?", P(NEU));

object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
if (DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count != 0) return Abbruch("foreign_key_check meldet Zeilen.");
long rest = 0;
using (var con = Verbindung())
    foreach (var (t, s) in spalten) rest += Zahl(con, "SELECT COUNT(*) FROM [" + t + "] WHERE [" + s + "] = " + kopie);
if (rest != 0) return Abbruch(rest + " Zeilen zeigen noch auf " + kopie + ".");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt.");
return 0;

SqliteConnection Verbindung()
{
    var c = new SqliteConnection("Data Source=" + arbeit + ";Pooling=False");
    c.Open();
    return c;
}

long Zahl(SqliteConnection c, string sql)
{
    using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
}

int Ausfuehren(SqliteConnection c, string sql, SqliteTransaction tx = null)
{
    using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    cmd.Transaction = tx;
    return cmd.ExecuteNonQuery();
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
    SqliteConnection.ClearAllPools();
}
