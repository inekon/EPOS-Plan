#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank das Referenzprojekt 1049 "Referenzprojekt Solarthermie" an - das
// einzige Referenzprojekt mit einem Kollektorfeld, das in der Kaskade DECKT (Anwenderentscheid
// 26.09.2026, Statusnummer #560).
//
// WOZU. Bis R21 fuehrte kein Referenzprojekt eine deckende Solarthermie: 1007 und 1046 nennen
// "Solarthermie" in der Kaskade ohne Kollektorfeld, 1026 (kein Referenzprojekt) fuehrt sein Feld
// an dritter Stelle und deckt praktisch nichts. Damit lagen die Waerme-Autarkie der Solarthermie
// (SolarWaermeMonate, Direkt- und Speicheranteil) und die Nachrang-Vorgabe 30 % bei Solarthermie
// am Puffer (Ladeordnung.SCHWELLE_AUS_NACHRANG_SOLAR_DEFAULT) ausserhalb des Regressionsnetzes.
//
// VORLAGE 1018 "BHKW Test Muenchen". Der Auftrag nannte 1030; 1030 rechnet aber 6,1 GWh
// Waermebedarf (Spitze 2,2 MW) aus einer externen Lastreihe - ein Feld von 30 bis 40 Modulen
// deckte dort unter 1 %. 1018 hat DIESELBE Kaskade (BHKW -> Heizkessel auf einen Puffer), wie
// 1030 keinen Warmwasserbedarf und rechnet ein Gebaeude nach VDI 6007 mit 68 MWh/a in Muenchen -
// die Groessenordnung, in der ein Feld dieser Groesse sichtbar deckt. Der Puffer hat 3.000 l
// statt der vorgesehenen 2.000 l: Mit 2.000 l deckte das Feld 12,5 %, mit 40 statt 35 Modulen
// kaum mehr (12,9 %, der Puffer begrenzt); mit 3.000 l sind es rund 15 %.
//
// WAS DIESES SKRIPT TUT.
//   1. Die Kopie 1018 -> 1049 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): alle
//      Projekttabellen samt Z_AnlageSenke, keine Rechenergebnisse. IDs vergibt der Kopierweg
//      (MAX + 1 je Tabelle); nur die Projekt-ID muss 1049 sein, sonst Abbruch.
//   2. Zellen der Kopie:
//        Tab_Projekt        Beschreibung, Aenderungs- und Erstelldatum fest (2026-09-26)
//        Tab_Einstellungen  Kaskade Tool_1..4 = Solarthermie, BHKW, Heizkessel, ''
//        Tab_Pufferspeicher der Puffer, den BHKW und Kessel laden (Kopie von "Stora B 1000-6
//                           ER 1 B"): "Pufferspeicher 3000 l", 3.000 l, Vorlauf 60 / Ruecklauf
//                           35 (dT 25 K), Schwelle_Aus 95 %, Schwelle_Aus_Nachrang LEER (die
//                           Automatik 30 % wegen Solarthermie am Puffer greift); die
//                           Anlagenzeile des Puffers traegt denselben Namen
//        Lade-Prioritaet    BHKW 2, Kessel 3 (Tab_Energieanlagen.WS_Ladeprio und
//                           Z_AnlageSenke.Ladeprio) - die Solarthermie laedt mit 1 vorrangig
//   3. Neue Zeilen:
//        Tab_Solarkollektoren Kopie des Katalogsatzes STAMM_KOLLEKTOR (Flachkollektor,
//                           Apertur 2,35 m2) mit den Spalten von SolarkollektorenCtrl.CopyFromStamm
//        Tab_Energieanlagen "Kollektorfeld Sued", ID_Type 2, MODULE Module, Neigung 35 Grad,
//                           Azimut 0 (Sued); Senken Rang 1 Heizkreis (Direktdeckung), Rang 2
//                           Puffer Heizung mit Ladeprio 1 (Speicheranteil)
//        Z_AnlageSenke      die zwei Senkenzeilen des Felds
//   Keine Rechenergebnisse, keine Katalogzeile, kein VACUUM. Alle Werte sind neutrale, runde
//   Pruefwerte.
//
// EINFRIERREGEL. 1049 ist Referenzprojekt: Kollektorfeld (Katalogsatz, Modulanzahl, Neigung,
// Azimut), Puffer-Temperaturpaar, Volumen und Schwellen sowie Kaskade und Lade-Prioritaeten
// gehoeren zur Einfrierregel "gesaete Solardaten" (CLAUDE.md, Referenzlaeufe/LIESMICH.md).
//
// WIEDERHOLBAR. Steht "Referenzprojekt Solarthermie" schon mit allen Zielzellen, aendert das
// Skript nichts (Rueckgabe 0). Weicht dort etwas ab, weicht die Vorlage 1018 ab oder faellt die
// Kopie nicht auf 1049, bricht es ab, ohne die Datei zu aendern (Rueckgabe 2): Geschrieben wird
// in eine Arbeitsdatei neben der Datenbank, geprueft, und erst dann ersetzt sie das Original.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekt_1049_solarthermie.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
//     ... -- <datei> --trocken      nur pruefen, nichts schreiben

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

const int VORLAGE = 1018;
const string VORLAGE_NAME = "BHKW Test München";
const int NEU = 1049;
const string NAME = "Referenzprojekt Solarthermie";
const string BESCHREIBUNG =
    "Referenzprojekt der Solarthermie: Kopie von Projekt 1018 (Gebäude nach VDI 6007, München) " +
    "mit Kaskade Solarthermie → BHKW → Heizkessel. Kollektorfeld 35 Flachkollektoren, 35° Süd, " +
    "deckt direkt und lädt vorrangig einen Puffer 3.000 l (60/35 °C, Abschaltschwelle 95 %, " +
    "Nachrang-Schwelle leer = Automatik 30 %). Hält Wärme-Autarkie und Nachrang-Vorgabe im " +
    "Regressionsnetz.";
const string DATUM = "2026-09-26 00:00:00";

const int STAMM_KOLLEKTOR = 3;          // Flachkollektor, Apertur 2,35 m2, h0 0,737
const string FELD = "Kollektorfeld Süd";
const int MODULE = 35;
const int NEIGUNG = 35;
const int AZIMUT = 0;

const int PUFFER_VORLAGE = 1054175;     // "Stora B 1000-6 ER 1 B" (965 l) in 1018
const string PUFFER_VORLAGE_NAME = "Stora B 1000-6 ER 1 B";
const string PUFFER = "Pufferspeicher 3000 l";
const double VOLUMEN = 3000;
const int VORLAUF = 60;
const int RUECKLAUF = 35;
const double SCHWELLE_AUS = 95;

const int LADEPRIO_SOLAR = 1, LADEPRIO_BHKW = 2, LADEPRIO_KESSEL = 3;
const int TYP_SOLAR = 2, TYP_KESSEL = 10, TYP_BHKW = 11, TYP_PUFFER = 12;

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekt_1049_solarthermie.cs -- <Kenndaten_Test.sqlite> [--trocken]");
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
Schreibnaht.WerkzeugFreigabe("Referenzlaeufe/Skripte/referenzprojekt_1049_solarthermie.cs");

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
    if (!string.Equals(ist, soll, StringComparison.Ordinal))
        f.Add(was + ": '" + (ist ?? "NULL") + "' statt '" + (soll ?? "NULL") + "'");
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

// Abdruck aller Zeilen eines Projekts ueber alle Projekttabellen samt Senkenzeilen.
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
    Tabelle("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?) ORDER BY 1",
            "Z_AnlageSenke");
    return sb.ToString();
}

long AnlageJeTyp(int projekt, int typ)
{
    DataTable dt = T("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", projekt, typ);
    return dt.Rows.Count == 1 ? Convert.ToInt64(dt.Rows[0]["ID"]) : -dt.Rows.Count;
}

// Die Vorlage in dem Zustand, von dem dieses Skript ausgeht.
List<string> PruefeVorlage()
{
    var f = new List<string>();
    if (Z("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Projektname = ?", VORLAGE, VORLAGE_NAME) != 1)
    { f.Add("Vorlage " + VORLAGE + " '" + VORLAGE_NAME + "' fehlt"); return f; }
    DataTable e = T("SELECT Tool_1, Tool_2, Tool_3, Tool_4 FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE);
    if (e.Rows.Count != 1) f.Add("Vorlage: " + e.Rows.Count + " Einstellungszeilen statt 1");
    else
    {
        SollText(f, "Vorlage Tool_1", Txt(e.Rows[0], "Tool_1"), DbWerte.ERZEUGER_BHKW);
        SollText(f, "Vorlage Tool_2", Txt(e.Rows[0], "Tool_2"), DbWerte.ERZEUGER_HEIZKESSEL);
        SollText(f, "Vorlage Tool_3", Txt(e.Rows[0], "Tool_3"), "");
    }
    if (AnlageJeTyp(VORLAGE, TYP_SOLAR) != 0) f.Add("Vorlage fuehrt schon ein Kollektorfeld");
    foreach (int typ in new[] { TYP_KESSEL, TYP_BHKW })
    {
        long a = AnlageJeTyp(VORLAGE, typ);
        if (a <= 0) { f.Add("Vorlage: nicht genau eine Anlage vom Typ " + typ); continue; }
        DataTable z = T("SELECT Rang, Ziel, ID_Puffer, Ladeprio FROM Z_AnlageSenke WHERE ID_Anlage = ?", a);
        if (z.Rows.Count != 1) { f.Add("Vorlage: Anlage " + a + " hat " + z.Rows.Count + " Senken statt 1"); continue; }
        SollText(f, "Vorlage Senke " + a, Txt(z.Rows[0], "Ziel"), DbWerte.WS_ZIEL_PUFFER_HEIZUNG);
        Soll(f, "Vorlage Senkenpuffer " + a, D(z.Rows[0], "ID_Puffer"), PUFFER_VORLAGE);
    }
    if (Z("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ? AND Bezeichner = ?",
          PUFFER_VORLAGE, VORLAGE, PUFFER_VORLAGE_NAME) != 1)
        f.Add("Vorlage: Puffer " + PUFFER_VORLAGE + " '" + PUFFER_VORLAGE_NAME + "' fehlt");
    if (Z("SELECT COUNT(*) FROM Tab_Solarkollektoren_STAMM WHERE ID = ? AND Kollektortyp = 'Flachkollektor'", STAMM_KOLLEKTOR) != 1)
        f.Add("Katalog: Flachkollektor " + STAMM_KOLLEKTOR + " fehlt");
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

    DataTable e = T("SELECT Tool_1, Tool_2, Tool_3, Tool_4 FROM Tab_Einstellungen WHERE ID_Projekt = ?", id);
    if (e.Rows.Count != 1) f.Add(e.Rows.Count + " Einstellungszeilen statt 1");
    else
    {
        SollText(f, "Tool_1", Txt(e.Rows[0], "Tool_1"), DbWerte.ERZEUGER_SOLARTHERMIE);
        SollText(f, "Tool_2", Txt(e.Rows[0], "Tool_2"), DbWerte.ERZEUGER_BHKW);
        SollText(f, "Tool_3", Txt(e.Rows[0], "Tool_3"), DbWerte.ERZEUGER_HEIZKESSEL);
        SollText(f, "Tool_4", Txt(e.Rows[0], "Tool_4"), "");
    }

    DataTable pu = T("SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?", id, PUFFER);
    long puffer = 0;
    if (pu.Rows.Count != 1) f.Add(pu.Rows.Count + " Puffer '" + PUFFER + "' statt 1");
    else
    {
        DataRow r = pu.Rows[0];
        puffer = Convert.ToInt64(r["ID"]);
        Soll(f, "Puffer Gesamtvolumen", D(r, "Gesamtvolumen"), VOLUMEN);
        Soll(f, "Puffer Vorlauf", D(r, "Vorlauf"), VORLAUF);
        Soll(f, "Puffer Ruecklauf", D(r, "Ruecklauf"), RUECKLAUF);
        Soll(f, "Puffer Schwelle_Aus", D(r, "Schwelle_Aus"), SCHWELLE_AUS);
        Soll(f, "Puffer Schwelle_Aus_Nachrang", D(r, "Schwelle_Aus_Nachrang"), null);
        if (Z("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? AND ID_PUFFER = ? AND Bezeichner = ?",
              id, TYP_PUFFER, puffer, PUFFER) != 1)
            f.Add("Anlagenzeile des Puffers traegt nicht den Namen '" + PUFFER + "'");
    }

    DataTable kol = T("SELECT k.*, s.ID AS Stamm FROM Tab_Solarkollektoren k JOIN Tab_Solarkollektoren_STAMM s " +
                      "ON s.Bezeichner = k.Bezeichner WHERE k.ID_Projekt = ?", id);
    if (kol.Rows.Count != 1) f.Add(kol.Rows.Count + " Kollektorsaetze statt 1");
    else Soll(f, "Kollektor aus Katalogsatz", D(kol.Rows[0], "Stamm"), STAMM_KOLLEKTOR);

    DataTable sol = T("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", id, TYP_SOLAR);
    if (sol.Rows.Count != 1) { f.Add(sol.Rows.Count + " Kollektorfelder statt 1"); return f; }
    DataRow a = sol.Rows[0];
    long feld = Convert.ToInt64(a["ID"]);
    SollText(f, "Feld Bezeichner", Txt(a, "Bezeichner"), FELD);
    if (kol.Rows.Count == 1) Soll(f, "Feld ID_Solar", D(a, "ID_Solar"), D(kol.Rows[0], "ID"));
    Soll(f, "Feld Modulanzahl", D(a, "Kollektormodulanzahl"), MODULE);
    Soll(f, "Feld Neigung", D(a, "Neigung"), NEIGUNG);
    Soll(f, "Feld Azimut", D(a, "Azimut"), AZIMUT);
    SollText(f, "Feld WS_Ziel", Txt(a, "WS_Ziel"), DbWerte.WS_ZIEL_HEIZKREIS);
    SollText(f, "Feld WS_Ziel2", Txt(a, "WS_Ziel2"), DbWerte.WS_ZIEL_PUFFER_HEIZUNG);
    Soll(f, "Feld WS_ID_Puffer2", D(a, "WS_ID_Puffer2"), puffer);
    Soll(f, "Feld WS_Ladeprio2", D(a, "WS_Ladeprio2"), LADEPRIO_SOLAR);

    var senken = new List<(long Anlage, int Rang, string Ziel, long? Puffer, int Prio)>
    {
        (feld, 1, DbWerte.WS_ZIEL_HEIZKREIS, null, 0),
        (feld, 2, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, puffer, LADEPRIO_SOLAR),
        (AnlageJeTyp(id, TYP_BHKW), 1, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, puffer, LADEPRIO_BHKW),
        (AnlageJeTyp(id, TYP_KESSEL), 1, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, puffer, LADEPRIO_KESSEL),
    };
    foreach (long anl in senken.Select(s => s.Anlage).Distinct())
    {
        var soll = senken.Where(s => s.Anlage == anl).ToList();
        DataTable z = T("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage = ? ORDER BY Rang", anl);
        if (z.Rows.Count != soll.Count) { f.Add("Anlage " + anl + ": " + z.Rows.Count + " Senken statt " + soll.Count); continue; }
        for (int i = 0; i < soll.Count; i++)
        {
            string k = "Senke " + anl + "/" + soll[i].Rang + " ";
            Soll(f, k + "Rang", D(z.Rows[i], "Rang"), soll[i].Rang);
            SollText(f, k + "Ziel", Txt(z.Rows[i], "Ziel"), soll[i].Ziel);
            SollText(f, k + "Bedarfsart", Txt(z.Rows[i], "Bedarfsart"), "Beides");
            Soll(f, k + "ID_Puffer", D(z.Rows[i], "ID_Puffer"), soll[i].Puffer);
            Soll(f, k + "Ladeprio", D(z.Rows[i], "Ladeprio"), soll[i].Prio);
            Soll(f, k + "Ladegrenze", D(z.Rows[i], "Ladegrenze"), 0);
        }
        if (anl != feld)
            Soll(f, "Anlage " + anl + " WS_Ladeprio",
                 D(T("SELECT WS_Ladeprio FROM Tab_Energieanlagen WHERE ID = ?", anl).Rows[0], "WS_Ladeprio"), soll[0].Prio);
    }

    foreach (string t in new[] { "Tab_Ergebnis", "Tab_ErgebnisWirtschaftlichkeit" })
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
string arbeit = datei + ".r22arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, NAME);
Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id);
if (id != NEU) return Abbruch("Die Kopie fiel auf " + id + " statt " + NEU + ".");

X("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?", BESCHREIBUNG, DATUM, DATUM, id);
X("UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = ?, Tool_4 = '' WHERE ID_Projekt = ?",
  DbWerte.ERZEUGER_SOLARTHERMIE, DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL, id);

// Der Puffer der Kopie: der, den BHKW und Kessel laden.
long bhkw = AnlageJeTyp(id, TYP_BHKW), kessel = AnlageJeTyp(id, TYP_KESSEL);
if (bhkw <= 0 || kessel <= 0) return Abbruch("BHKW oder Kessel der Kopie nicht eindeutig.");
long puffer = Z("SELECT ID_Puffer FROM Z_AnlageSenke WHERE ID_Anlage = ? AND Rang = 1", bhkw);
if (puffer <= 0 || Z("SELECT ID_Puffer FROM Z_AnlageSenke WHERE ID_Anlage = ? AND Rang = 1", kessel) != puffer ||
    Z("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ? AND Bezeichner = ?", puffer, id, PUFFER_VORLAGE_NAME) != 1)
    return Abbruch("Der gemeinsame Puffer von BHKW und Kessel ist in der Kopie nicht aufzufinden.");
X("UPDATE Tab_Pufferspeicher SET Bezeichner = ?, Gesamtvolumen = ?, Vorlauf = ?, Ruecklauf = ?, Schwelle_Aus = ?, " +
  "Schwelle_Aus_Nachrang = NULL WHERE ID = ?", PUFFER, VOLUMEN, VORLAUF, RUECKLAUF, SCHWELLE_AUS, puffer);
X("UPDATE Tab_Energieanlagen SET Bezeichner = ? WHERE ID_Projekt = ? AND ID_Type = ? AND ID_PUFFER = ?", PUFFER, id, TYP_PUFFER, puffer);

// Lade-Prioritaeten: Solarthermie 1 (vorrangig), BHKW 2, Kessel 3.
foreach (var (anlage, prio) in new[] { (bhkw, LADEPRIO_BHKW), (kessel, LADEPRIO_KESSEL) })
{
    X("UPDATE Tab_Energieanlagen SET WS_Ladeprio = ? WHERE ID = ?", prio, anlage);
    X("UPDATE Z_AnlageSenke SET Ladeprio = ? WHERE ID_Anlage = ? AND Rang = 1", prio, anlage);
}

// Kollektorfeld: Katalogkopie, Anlagenzeile, zwei Senken.
// Dieselben Spalten wie SolarkollektorenCtrl.CopyFromStamm (die Klasse ist intern).
long kollektor = Z("SELECT MAX(ID) FROM Tab_Solarkollektoren") + 1;
if (X("INSERT INTO Tab_Solarkollektoren (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Kollektortyp, Modulflaeche, " +
      "Aperturflaeche, h0, k1, k2, Kdir, Kdfu, Investitionskosten) SELECT ?, ?, Bezeichner, Firma, Beschreibung, " +
      "Kollektortyp, Modulflaeche, Aperturflaeche, h0, k1, k2, Kdir, Kdfu, Investitionskosten " +
      "FROM Tab_Solarkollektoren_STAMM WHERE ID = ?", kollektor, id, STAMM_KOLLEKTOR) != 1)
    return Abbruch("Kollektorsatz " + STAMM_KOLLEKTOR + " nicht kopiert.");
X("INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, ID_Solar, Kollektormodulanzahl, Neigung, Azimut, " +
  "ID_Carrier, WS_Typ, WS_Ziel, WS_ID_Puffer, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV, " +
  "WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2, Aufteilung_Methode) " +
  "VALUES (?, ?, ?, ?, ?, ?, ?, 0, 'Beides', ?, NULL, 0, 0, 0, ?, ?, ?, 0, 'Berechnet')",
  id, FELD, TYP_SOLAR, kollektor, MODULE, NEIGUNG, AZIMUT,
  DbWerte.WS_ZIEL_HEIZKREIS, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, puffer, LADEPRIO_SOLAR);
long feld = AnlageJeTyp(id, TYP_SOLAR);
if (feld <= 0) return Abbruch("Kollektorfeld nicht angelegt.");
const string INSERT_SENKE =
    "INSERT INTO Z_AnlageSenke (ID_Anlage, Rang, Ziel, Bedarfsart, ID_Puffer, Ladeprio, Ladeprio_PV, Ladegrenze) " +
    "VALUES (?, ?, ?, 'Beides', ?, ?, 0, 0)";
X(INSERT_SENKE, feld, 1, DbWerte.WS_ZIEL_HEIZKREIS, null, 0);
X(INSERT_SENKE, feld, 2, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, puffer, LADEPRIO_SOLAR);

// ---------------------------------------------------------------------------------------
// 3. Pruefen, dann ersetzen
// ---------------------------------------------------------------------------------------
List<string> ziel = PruefeZiel(id);
if (ziel.Count > 0) return Abbruch("Zielzellen weichen ab:\n  " + string.Join("\n  ", ziel));
if (Abdruck(VORLAGE) != abdruckVorher) return Abbruch("Die Vorlage " + VORLAGE + " hat sich veraendert.");
Console.WriteLine("Zeilen 1049:  " + string.Join(", ", Projekttabellen()
    .Select(p => (p.Tabelle, n: Z("SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" = ?", id)))
    .Where(p => p.n > 0).Select(p => p.Tabelle + " " + p.n)) +
    ", Z_AnlageSenke " + Z("SELECT COUNT(*) FROM Z_AnlageSenke WHERE ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)", id));
Console.WriteLine("integrity:    " + S("PRAGMA integrity_check"));
long fk = T("PRAGMA foreign_key_check").Rows.Count;
if (fk != 0) return Abbruch("foreign_key_check meldet " + fk + " Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
Console.WriteLine("Projekt " + NEU + " '" + NAME + "' angelegt (" + MODULE + " Kollektoren, Puffer " + VOLUMEN + " l).");
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
