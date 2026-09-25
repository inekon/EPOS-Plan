#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Pruefprojekt 1048 "Pruefprojekt PV mit Preisen" an - ein
// PV-Projekt mit VOLLSTAENDIGEM Preissatz, OHNE Referenzrolle (Welle E25; Anlass: Befund der
// Welle E9a, dass kein Testprojekt die PV-Erloesseite der Wirtschaftlichkeit mit echten Preisen
// traegt; Entscheid E21-Q9 a und E25-Q1 bis Q10, alle nach Empfehlung a, 25.09.2026).
//
// WOZU. Die PV-Erloesseite - Einspeiseerloes PV, vermiedener Bezug, Szenarien C (Traegerpreise)
// und D (Erloessaetze), PV-Block der Formelmappe, Kapitalwert - war bis dahin nur synthetisch
// oder auf Arbeitskopien mit nachgetragenen Preisen geprueft (1040 mit den Preisen von 1030).
// 1048 traegt die Preise selbst; EPOS.Kern.Tests/PvPreisProjektTests rechnet darauf.
//
// KEIN REFERENZPROJEKT. 1048 steht in keiner Referenzbasis, in keiner Projektliste von kern.yml,
// ios.yml oder Referenzlaeufe/LIESMICH.md, und fuer 1048 gilt keine Einfrierregel. Die Vorlage
// 1040 (ein Referenzprojekt) bleibt Zelle fuer Zelle, wie sie war - das Skript prueft das.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1040 -> 1048 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl,
//      "Projekt speichern unter"): alle Projekttabellen, keine Rechenergebnisse, keine
//      Verguetungszeile (Tab_ProjektPhotovoltaik). IDs vergibt der Kopierweg zur Laufzeit
//      (MAX + 1 je Tabelle); nur die Projekt-ID muss 1048 sein, sonst Abbruch.
//   2. Zellen der Kopie:
//        Tab_Projekt           Beschreibung, Aenderungs- und Erstelldatum fest (2026-09-25)
//        Tab_Gebaeude          Gebaeude_Modell 'TAGESBILANZ' -> NULL (VDI 6007, wie 1045;
//                              der Tagesbilanz-Weg bleibt allein bei 1040, Stufe GA)
//        Tab_Energieanlagen    PV_Leistung (MODULANZAHL) 20 -> 40: 40 x 260 W = 10,40 kWp
//        energy_project_settings Erdgas E (63, die kopierte Zeile): Arbeitspreis 0,80 EUR/Nm3
//                              (Guenstig 0,70 / Unguenstig 0,95), Grundpreis 150 EUR/a
//   3. Neue Zeilen:
//        energy_project_settings Elektrische Energie (60) nach dem Muster der 1030-Zeile
//                              (Umrechnung 51, hi = hs = 1, co2/so2/nox 560/200/280,
//                              Preisbasis kWh): Arbeitspreis 0,30 EUR/kWh (0,26 / 0,36),
//                              Grundpreis 120 EUR/a (100 / 150), Leistungspreis 0
//        Tab_ProjektWirtschaftlichkeit ueber WirtschaftlichkeitCtrl (Vorgaben des Programms),
//                              dann Zins 3 %, 20 a, Preissteigerung Energie 2 %/a, Betrieb
//                              1,5 %/a, Einspeiseverguetung PV 0,08 EUR/kWh (0,10 / 0,06);
//                              uebrige Szenariofelder leer (Vorgaben bzw. "wie Erwartet")
//        Tab_ProjektWerte      an der PV-Anlage, mit vorhandenen Positionen des Lexikons und
//                              OHNE NutzungsdauerID (E25-Q7):
//                              Kat. 1  80 "Photovoltaik"  EUR_PRO_KWP 1.200 EUR/kWp = 12.480 EUR
//                                      (Guenstig 10.400 / Unguenstig 14.560 EUR), 25 a
//                              Kat. 2 149 "Wartung / Inspektion PV-Anlage" 150 EUR/a (120 / 180)
//                              Kat. 2 150 "Instandhaltung PV-Module / Gestell" 1 % der Investition
//      Die 20 kopierten Investitionszeilen von 1040 bleiben (E25-Q10). Keine Tarifstruktur
//      (Flat, E25-Q4), keine Verguetungszeile (flache Einspeiseverguetung, E25-Q3), keine
//      Rechenergebnisse, keine Katalogzeile, kein VACUUM.
//   Alle Werte sind neutrale, runde Pruefwerte ohne Produktbezug.
//
// WIEDERHOLBAR. Steht "Pruefprojekt PV mit Preisen" schon mit allen Zielzellen, aendert das
// Skript nichts (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1040 ab oder faellt die
// Kopie nicht auf 1048, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2): Geschrieben wird
// in eine Arbeitsdatei neben der Datenbank, geprueft, und erst dann ersetzt sie das Original.
// Nach einer Neufassung der Testdatenbank (etwa einer Datenpflege anderer Projekte) wird das
// Skript auf der neuen Fassung erneut gezogen - die Zeilen-IDs ergeben sich dann neu.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

const int VORLAGE = 1040;
const string VORLAGE_NAME = "zwei Puffer je Kanal";
const int NEU = 1048;
const string NAME = "Prüfprojekt PV mit Preisen";
const string BESCHREIBUNG =
    "Prüfprojekt ohne Referenzrolle: Kopie von Projekt 1040 mit Gebäude nach VDI 6007 und " +
    "40 PV-Modulen (10,40 kWp) und einem vollständigen Preissatz - Strom und Erdgas mit " +
    "Günstig und Ungünstig, Einspeisevergütung PV 0,08 €/kWh (0,10 / 0,06), PV-Investition " +
    "je kWp und PV-Betriebskosten. Es hält die PV-Erlösseite der Wirtschaftlichkeit in den " +
    "Tests (PvPreisProjektTests) und steht in keiner Referenzbasis.";
const string DATUM = "2026-09-25 00:00:00";

const int MODULE_VORLAGE = 20;
const int MODULE = 40;
const double MODULLEISTUNG_W = 260.0;
const double KWP = MODULE * MODULLEISTUNG_W / 1000.0;   // 10,40

const int TRAEGER_STROM = 60;       // "Elektrische Energie"
const int TRAEGER_ERDGAS = 63;      // "Erdgas E"

const int KOMPONENTE_PV = 3;
const int STAMM_PV = 80;            // "Photovoltaik" (Hauptkomponente)
const int STAMM_WARTUNG = 149;      // "Wartung / Inspektion PV-Anlage"
const int STAMM_INSTAND = 150;      // "Instandhaltung PV-Module / Gestell"
const int VORLAGE_INVEST_PV = 5;    // Tab_KostenVorlage "Standard" PV Kat. 1 (Herkunft)
const int VORLAGE_BETRIEB_PV = 16;  // Tab_KostenVorlage "Standard" PV Kat. 2 (Herkunft)
const double SATZ_KWP = 1200.0;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run pruefprojekt_1048_pv_preise.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs");

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
// Datumsspalten liest der Datenzugriff als DateTime zurueck; verglichen wird in der Schreibform.
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

// Die Projekttabellen (Spalte ID_Projekt oder ProjektID) - fuer den Zellabdruck der Vorlage.
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

// Abdruck aller Zeilen eines Projekts ueber alle Projekttabellen (Tab_Projekt ueber ID).
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

// Die Vorlage in dem Zustand, von dem dieses Skript ausgeht.
List<string> PruefeVorlage()
{
    var f = new List<string>();
    if (Z("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Projektname = ?", VORLAGE, VORLAGE_NAME) != 1)
    { f.Add("Vorlage " + VORLAGE + " '" + VORLAGE_NAME + "' fehlt"); return f; }
    DataTable g = T("SELECT Gebaeude_Modell FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE);
    if (g.Rows.Count != 1) f.Add("Vorlage: " + g.Rows.Count + " Gebaeude statt 1");
    else SollText(f, "Vorlage Gebaeude_Modell", Txt(g.Rows[0], "Gebaeude_Modell"), "TAGESBILANZ");
    DataTable pv = T("SELECT a.PV_Leistung, p.Leistung FROM Tab_Energieanlagen a JOIN Tab_PV p ON p.ID = a.ID_PV " +
                     "WHERE a.ID_Projekt = ? AND a.ID_PV > 0", VORLAGE);
    if (pv.Rows.Count != 1) f.Add("Vorlage: " + pv.Rows.Count + " PV-Anlagen statt 1");
    else
    {
        Soll(f, "Vorlage PV_Leistung (Modulanzahl)", D(pv.Rows[0], "PV_Leistung"), MODULE_VORLAGE);
        Soll(f, "Vorlage Modulleistung", D(pv.Rows[0], "Leistung"), MODULLEISTUNG_W);
    }
    if (Z("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ?", VORLAGE) != 1 ||
        Z("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?", VORLAGE, TRAEGER_ERDGAS) != 1)
        f.Add("Vorlage: Traegerzeilen weichen ab (erwartet genau Erdgas E)");
    if (Z("SELECT COUNT(*) FROM Tab_ProjektWirtschaftlichkeit WHERE ID_Projekt = ?", VORLAGE) != 0)
        f.Add("Vorlage: fuehrt einen Parametersatz");
    if (Z("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ? AND KomponentenID = ?", VORLAGE, KOMPONENTE_PV) != 0)
        f.Add("Vorlage: fuehrt PV-Kostenpositionen");
    if (Z("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", VORLAGE) != 20)
        f.Add("Vorlage: nicht 20 Kostenpositionen");
    if (Z("SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ? AND Kuehlbetrieb = 0", VORLAGE) != 1)
        f.Add("Vorlage: Kuehlbetrieb nicht aus");
    foreach (long k in new long[] { STAMM_PV, STAMM_WARTUNG, STAMM_INSTAND })
        if (Z("SELECT COUNT(*) FROM Tab_Kostenfaktor WHERE StammID = ?", k) != 1)
            f.Add("Positionslexikon: StammID " + k + " fehlt");
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

    DataTable g = T("SELECT Gebaeude_Modell FROM Tab_Gebaeude WHERE ID_Projekt = ?", id);
    if (g.Rows.Count != 1) f.Add(g.Rows.Count + " Gebaeude statt 1");
    else SollText(f, "Gebaeude_Modell", Txt(g.Rows[0], "Gebaeude_Modell"), null);
    if (Z("SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ? AND Kuehlbetrieb = 0", id) != 1)
        f.Add("Kuehlbetrieb nicht aus");

    DataTable pv = T("SELECT a.ID, a.PV_Leistung, p.Leistung FROM Tab_Energieanlagen a JOIN Tab_PV p ON p.ID = a.ID_PV " +
                     "WHERE a.ID_Projekt = ? AND a.ID_PV > 0", id);
    long anlage = 0;
    if (pv.Rows.Count != 1) f.Add(pv.Rows.Count + " PV-Anlagen statt 1");
    else
    {
        anlage = Convert.ToInt64(pv.Rows[0]["ID"]);
        Soll(f, "PV_Leistung (Modulanzahl)", D(pv.Rows[0], "PV_Leistung"), MODULE);
        Soll(f, "Modulleistung", D(pv.Rows[0], "Leistung"), MODULLEISTUNG_W);
    }

    DataTable eps = T("SELECT * FROM energy_project_settings WHERE ID_Projekt = ? ORDER BY [ID_Energieträger]", id);
    if (eps.Rows.Count != 2) f.Add(eps.Rows.Count + " Traegerzeilen statt 2");
    foreach (DataRow r in eps.Rows)
    {
        int c = Convert.ToInt32(r["ID_Energieträger"]);
        string k = "Traeger " + c + " ";
        if (c == TRAEGER_ERDGAS)
        {
            Soll(f, k + "Arbeitspreis", D(r, "custom_price_work"), 0.80);
            Soll(f, k + "Arbeitspreis Guenstig", D(r, "custom_price_work_best"), 0.70);
            Soll(f, k + "Arbeitspreis Unguenstig", D(r, "custom_price_work_worst"), 0.95);
            Soll(f, k + "Grundpreis", D(r, "custom_price_base"), 150.0);
            Soll(f, k + "Grundpreis Guenstig", D(r, "custom_price_base_best"), null);
            Soll(f, k + "Grundpreis Unguenstig", D(r, "custom_price_base_worst"), null);
            Soll(f, k + "Leistungspreis", D(r, "custom_price_power"), 0.0);
            Soll(f, k + "co2", D(r, "co2"), 240.0);
            SollText(f, k + "Preisbasis", Txt(r, "Preisbasis"), "Nm³");
        }
        else if (c == TRAEGER_STROM)
        {
            Soll(f, k + "ID_Umrechnung", D(r, "ID_Umrechnung"), 51);
            Soll(f, k + "hi", D(r, "custom_hi"), 1.0);
            Soll(f, k + "hs", D(r, "custom_hs"), 1.0);
            Soll(f, k + "Arbeitspreis", D(r, "custom_price_work"), 0.30);
            Soll(f, k + "Arbeitspreis Guenstig", D(r, "custom_price_work_best"), 0.26);
            Soll(f, k + "Arbeitspreis Unguenstig", D(r, "custom_price_work_worst"), 0.36);
            Soll(f, k + "Grundpreis", D(r, "custom_price_base"), 120.0);
            Soll(f, k + "Grundpreis Guenstig", D(r, "custom_price_base_best"), 100.0);
            Soll(f, k + "Grundpreis Unguenstig", D(r, "custom_price_base_worst"), 150.0);
            Soll(f, k + "Leistungspreis", D(r, "custom_price_power"), 0.0);
            Soll(f, k + "Leistungspreis Guenstig", D(r, "custom_price_power_best"), null);
            Soll(f, k + "Leistungspreis Unguenstig", D(r, "custom_price_power_worst"), null);
            Soll(f, k + "co2", D(r, "co2"), 560.0);
            Soll(f, k + "so2", D(r, "so2"), 200.0);
            Soll(f, k + "nox", D(r, "nox"), 280.0);
            SollText(f, k + "Anteil_Modus", Txt(r, "Anteil_Modus"), "Gesamtwert");
            SollText(f, k + "Preisbasis", Txt(r, "Preisbasis"), "kWh");
        }
        else f.Add("unerwarteter Traeger " + c);
    }

    DataTable w = T("SELECT * FROM Tab_ProjektWirtschaftlichkeit WHERE ID_Projekt = ?", id);
    if (w.Rows.Count != 1) f.Add(w.Rows.Count + " Parametersaetze statt 1");
    else
    {
        DataRow r = w.Rows[0];
        Soll(f, "Zinssatz", D(r, "Zinssatz"), 3.0);
        Soll(f, "Betrachtungszeitraum", D(r, "Betrachtungszeitraum"), 20);
        Soll(f, "Preissteigerung_Energie", D(r, "Preissteigerung_Energie"), 2.0);
        Soll(f, "Preissteigerung_Betrieb", D(r, "Preissteigerung_Betrieb"), 1.5);
        Soll(f, "Einspeiseverguetung", D(r, "Einspeiseverguetung"), 0.08);
        Soll(f, "Einspeiseverguetung_Best", D(r, "Einspeiseverguetung_Best"), 0.10);
        Soll(f, "Einspeiseverguetung_Worst", D(r, "Einspeiseverguetung_Worst"), 0.06);
        SollText(f, "GeaendertAm", Dat(r, "GeaendertAm"), DATUM);
    }

    if (Z("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", id) != 23)
        f.Add(Z("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", id) + " Kostenpositionen statt 23");
    DataTable k3 = T("SELECT * FROM Tab_ProjektWerte WHERE ProjektID = ? AND KomponentenID = ? ORDER BY KategorieID, StammID", id, KOMPONENTE_PV);
    if (k3.Rows.Count != 3) f.Add(k3.Rows.Count + " PV-Positionen statt 3");
    else
    {
        var soll = new[]
        {
            (Stamm: STAMM_PV, Kat: 1, Wert: KWP * SATZ_KWP, Best: KWP * 1000.0, Worst: KWP * 1400.0, Dauer: 25.0,
             Gruppe: DbWerte.KOSTEN_GRUPPE_ALLGEMEIN, Art: "KAPITALGEBUNDEN", Bem: "EUR_PRO_KWP", Satz: (double?)SATZ_KWP,
             Vorlage: VORLAGE_INVEST_PV, Pflicht: 0),
            (Stamm: STAMM_WARTUNG, Kat: 2, Wert: 150.0, Best: 120.0, Worst: 180.0, Dauer: 0.0,
             Gruppe: DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI, Art: "BETRIEBSGEBUNDEN", Bem: "JAHRESBETRAG", Satz: (double?)null,
             Vorlage: VORLAGE_BETRIEB_PV, Pflicht: 1),
            (Stamm: STAMM_INSTAND, Kat: 2, Wert: 0.0, Best: 0.0, Worst: 0.0, Dauer: 0.0,
             Gruppe: DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI, Art: "BETRIEBSGEBUNDEN", Bem: "PROZENT_INVESTITION", Satz: (double?)1.0,
             Vorlage: VORLAGE_BETRIEB_PV, Pflicht: 1),
        };
        for (int i = 0; i < 3; i++)
        {
            DataRow r = k3.Rows[i];
            var s = soll[i];
            string k = "Position " + s.Stamm + " ";
            Soll(f, k + "StammID", D(r, "StammID"), s.Stamm);
            Soll(f, k + "KategorieID", D(r, "KategorieID"), s.Kat);
            Soll(f, k + "EingegebenerWert", D(r, "EingegebenerWert"), s.Wert);
            Soll(f, k + "Bestcase", D(r, "Bestcase"), s.Best);
            Soll(f, k + "Worstcase", D(r, "Worstcase"), s.Worst);
            Soll(f, k + "Nutzungsdauer", D(r, "Nutzungsdauer"), s.Dauer);
            Soll(f, k + "Einheitpreis", D(r, "Einheitpreis"), s.Satz);
            Soll(f, k + "Menge", D(r, "Menge"), null);
            Soll(f, k + "ID_Anlage", D(r, "ID_Anlage"), anlage);
            Soll(f, k + "VorlageID", D(r, "VorlageID"), s.Vorlage);
            Soll(f, k + "IstPflicht", D(r, "IstPflicht"), s.Pflicht);
            Soll(f, k + "NutzungsdauerID", D(r, "NutzungsdauerID"), null);
            SollText(f, k + "Gruppe", Txt(r, "Gruppe"), s.Gruppe);
            SollText(f, k + "Kostenart", Txt(r, "Kostenart"), s.Art);
            SollText(f, k + "Bemessung", Txt(r, "Bemessung"), s.Bem);
            SollText(f, k + "Einheit", Txt(r, "Einheit"), "€");
        }
    }

    foreach (string t in new[] { "Tab_ProjektPhotovoltaik", "Tab_ProjektTarif", "Tab_Ergebnis", "Tab_ErgebnisWirtschaftlichkeit" })
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
string arbeit = datei + ".e25arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");

X("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?", BESCHREIBUNG, DATUM, DATUM, id);
X("UPDATE Tab_Gebaeude SET Gebaeude_Modell = NULL WHERE ID_Projekt = ?", id);
long anlagePv = Z("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_PV > 0", id);
X("UPDATE Tab_Energieanlagen SET PV_Leistung = ? WHERE ID = ?", MODULE, anlagePv);

X("UPDATE energy_project_settings SET custom_price_work = 0.80, custom_price_work_best = 0.70, " +
  "custom_price_work_worst = 0.95, custom_price_base = 150 WHERE ID_Projekt = ? AND [ID_Energieträger] = ?", id, TRAEGER_ERDGAS);
X("INSERT INTO energy_project_settings (ID_Projekt, [ID_Energieträger], ID_Umrechnung, custom_hi, custom_hs, " +
  "custom_price_work, custom_price_base, custom_price_power, co2, so2, nox, Anteil_Modus, Preisbasis, " +
  "custom_price_work_best, custom_price_work_worst, custom_price_base_best, custom_price_base_worst) " +
  "VALUES (?, ?, 51, 1, 1, 0.30, 120, 0, 560, 200, 280, 'Gesamtwert', 'kWh', 0.26, 0.36, 100, 150)", id, TRAEGER_STROM);

var wc = new WirtschaftlichkeitCtrl();
if (!wc.SpeichereParameter(wc.LadeParameter(id))) return Abbruch("Parametersatz nicht angelegt.");
X("UPDATE Tab_ProjektWirtschaftlichkeit SET Zinssatz = 3, Betrachtungszeitraum = 20, Preissteigerung_Energie = 2, " +
  "Preissteigerung_Betrieb = 1.5, Einspeiseverguetung = 0.08, Einspeiseverguetung_Best = 0.10, " +
  "Einspeiseverguetung_Worst = 0.06, GeaendertAm = ? WHERE ID_Projekt = ?", DATUM, id);

const string INSERT_WERT =
    "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, Bestcase, " +
    "Worstcase, Nutzungsdauer, Worstcase_Nutzungsdauer, Bestcase_Nutzungsdauer, Einheit, Gruppe, Kostenart, " +
    "Bemessung, IstErloes, Menge, Einheitpreis, VorlageID, ID_Anlage, IstPflicht) " +
    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, 0, 0, '€', ?, ?, ?, 0, NULL, ?, ?, ?, ?)";
X(INSERT_WERT, id, STAMM_PV, KOMPONENTE_PV, 1, KWP * SATZ_KWP, KWP * 1000.0, KWP * 1400.0, 25.0,
  DbWerte.KOSTEN_GRUPPE_ALLGEMEIN, "KAPITALGEBUNDEN", "EUR_PRO_KWP", SATZ_KWP, VORLAGE_INVEST_PV, anlagePv, 0);
X(INSERT_WERT, id, STAMM_WARTUNG, KOMPONENTE_PV, 2, 150.0, 120.0, 180.0, 0.0,
  DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI, "BETRIEBSGEBUNDEN", "JAHRESBETRAG", null, VORLAGE_BETRIEB_PV, anlagePv, 1);
X(INSERT_WERT, id, STAMM_INSTAND, KOMPONENTE_PV, 2, 0.0, 0.0, 0.0, 0.0,
  DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI, "BETRIEBSGEBUNDEN", "PROZENT_INVESTITION", 1.0, VORLAGE_BETRIEB_PV, anlagePv, 1);

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = PruefeZiel(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (Abdruck(VORLAGE) != abdruckVorher) return Abbruch("Die Vorlage " + VORLAGE + " hat sich veraendert.");
Console.WriteLine("Zeilen 1048:  " + string.Join(", ", Projekttabellen()
    .Select(p => (p.Tabelle, n: Z("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)));
Console.WriteLine("integrity:    " + S("PRAGMA integrity_check"));
long fk = T("PRAGMA foreign_key_check").Rows.Count;
if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt (" + KWP.ToString("0.00", CultureInfo.InvariantCulture) + " kWp).");
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
