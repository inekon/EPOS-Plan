#:project ../../EPOS.Kern/EPOS.Kern.csproj
#:property Nullable=disable
#:property TreatWarningsAsErrors=false
#:property WarningLevel=0

// Legt in der Testdatenbank die sechs Referenzprojekte der Zapfprofil-Optionen an (Konzept Zapfprofil Benutzerfuehrung,
// ZB12, Welle W7): je eine Kopie des Referenzprojekts 1045 "Pruefprojekt Ost/West Straenge" (Anlage, Gebaeude, Klima),
// an der Kopie das Zapfprofil einer Optionsgruppe gesaet.
//
// WOZU. Bis hierher rechnete allein 1045 ueber den Zapfprofilgenerator - eine Zone Wohnen, Personen, deterministisch,
// an das Gebaeude gebunden, ohne Zirkulation. Mischnutzung, Wohnungstabelle, Zirkulation, Kalender und Ferien der Zone,
// Feiertagsland, stochastische Jahresreihe, Typtage, Kalibrierung, manuelle Werte und die Auslegung standen ausserhalb
// des Regressionsnetzes. Jedes der sechs Projekte haengt eine Gruppe davon hinein.
//
// DIE SECHS PROJEKTE (alle Katalogzeilen aus dem Testkatalog TEST-1, gesucht ueber Bezeichner und Katalogversion):
//   1066 Mischnutzung  Zonen Wohnen (Wohnungstabelle: 4 x 2 Raeume, 2 x 3 Raeume, 3 x 4 Raeume, Belegung nach Raumzahl
//                      aus dem Katalog, Ausstattung "Testklasse A"; am Gebaeude), Buero (40 Beschaeftigte), Hotel
//                      (20 Zimmer), Schule (150 Personen, ohne Zirkulation); Zirkulation nach Leitungslaenge
//                      (120 m, 10 W/m, Laufzeit 18 h), aufgeteilt auf die drei Zonen mit Zirkulation.
//   1067 Kalender      Zone Schule (200 Personen) mit dem Kalender ihrer Nutzungsart (Schulferien) und fuenf eigenen
//                      Ferienzeitraeumen, einer davon ueber den Jahreswechsel; Zone Buero (30 Beschaeftigte) an das
//                      Gebaeude gebunden, das Gebaeude mit Feiertagsland "BY" und Wochenmaske Sa + So (Bitmaske 96).
//   1068 Stochastisch  Zone Wohnen wie 1045, die Jahresreihe stochastisch mit Seed 1068 und 10 Realisierungen.
//   1069 Typtage       Zone Wohnen wie 1045, der Jahresgang ueber die eingespielten Typtage (Klimazone 4, Gebaeudeart
//                      "referenzhaus"). Der Typtagsatz ist ERFUNDEN (Quelle "Referenztyptage (fiktiv)", runde Werte, kein
//                      Wert einer Richtlinie und nichts aus vdi4655_abgeleitet.json) und wird ueber den Importweg des
//                      Kerns eingespielt (TwwTyptagCtrl.Importieren, ein im Skript gebautes ZIP-Paket).
//   1070 Kalibrierung  Zone "Wohnen gemessen" (10 Personen) mit Jahresmesswert 7 000 kWh/a an der Zapfstelle; Zone
//                      "Gewerbe manuell" (20 Beschaeftigte) mit manuellem Tagesbedarf 15 kWh/d, eigener Zapftemperatur
//                      55 degC, Kaltwasser 12 degC +/- 4 K und eigenem Auslastungsgang; Zirkulation manuell 0,8 kW
//                      in 16 h Laufzeit.
//   1071 Auslegung     Je eine Zone mit Topologie Speicher (Wohnen, 12 Personen), Frischwasserstation (Hotel, 15 Zimmer),
//                      Durchfluss (Buero, 25 Beschaeftigte) und Wohnungsstation (Ein- und Zweifamilienhaus, 4 Personen);
//                      Auslegung mit dem Ecodesign-Tag "Ecodesign-Zapfprofil L" aus dem freien Paketteil, Erzeugerart
//                      Waermepumpe, Werkstoff Edelstahl. Die Auslegung wirkt nicht auf die Simulation; ihre Werte haelt
//                      EPOS.Kern.Tests/ZapfprofilOptionenReferenzprojekteWacheTests eingefroren.
//
// WAS DIESES SKRIPT TUT.
//   1. Prueft, dass keines der sechs Projekte steht (sonst benannter Abbruch), dass 1065 die hoechste Projekt-ID ist,
//      dass 1045 auf dem Generator rechnet, dass keine Typtage eingespielt sind und dass jede Katalogzeile genau einmal
//      steht.
//   2. Kopiert 1045 sechsmal auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl): 1066 bis 1071.
//   3. Spielt den erfundenen Typtagsatz ein und speichert je Projekt den Arbeitsstand ueber den Schreibweg des
//      Kerns (ZapfprofilCtrl.Speichern) - Zonen, Wohnungstypen, Ferien, Projektzeile in einem Vorgang.
//   4. Setzt das Aenderungsdatum der sechs Projektzeilen auf 2026-10-13 (der Schreibweg schreibt die Uhrzeit), am
//      Gebaeude von 1067 Feiertagsland und Wochenmaske, an den Projekten Beschreibung und Kosten_Geaendert leer.
//   5. Rechnet jedes Projekt ueber den Brauchwasserweg des Laufs (SimulationWaermebedarf) - ohne Abbruch, ohne
//      abgelehnte Zone - und 1071 die Auslegung; druckt die Kennzahlen. Danach integrity_check und foreign_key_check.
// Geschrieben wird in eine Arbeitsdatei neben der Datenbank; erst nach allen Pruefungen ersetzt sie das Original.
//
// EINFRIERREGEL. Die sechs Projekte gehoeren zu "gesaete Zapfprofil-Eingaben eines Referenzprojekts", ebenso der
// erfundene Typtagsatz in Tab_TwwTyptag_IMPORT und die Ferienzeitraeume in Tab_TwwZonenferien.
//
// NICHT WIEDERHOLBAR AUF DERSELBEN DATEI. Steht eines der sechs Projekte schon, bricht das Skript benannt ab (Rueckgabe 2)
// und aendert nichts. Fuer einen zweiten Lauf die Datenbank aus der Sicherung zuruecklegen - dann vergibt er dieselben
// Ids. Mit --trocken werden nur die Vorbedingungen geprueft.
//
// Aufruf (vorher sichern; dotnet ab SDK 10):
//     dotnet run Referenzlaeufe/Skripte/referenzprojekte_zapfprofil_optionen.cs -- Referenzlaeufe/Kenndaten_Test.sqlite [--trocken]

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

const int VORLAGE = 1045;
const string VORLAGE_NAME = "Prüfprojekt Ost/West Stränge";
const int VORHER_HOECHSTE = 1065;
const string KATALOG = "TEST-1";
const string DATUM = "2026-10-13";

var projekte = new (int Id, string Name, string Beschreibung)[]
{
    (1066, "Referenzprojekt Zapfprofil Mischnutzung",
     "Referenzprojekt Zapfprofil Mischnutzung: Kopie von Projekt 1045 mit vier Zonen (Wohnen mit Wohnungstabelle, Büro, Hotel, " +
     "Schule) und Zirkulation nach Leitungslänge, aufgeteilt auf die Zonen."),
    (1067, "Referenzprojekt Zapfprofil Kalender",
     "Referenzprojekt Zapfprofil Kalender: Kopie von Projekt 1045; eine Zone mit dem Kalender ihrer Nutzungsart und fünf eigenen " +
     "Ferienzeiträumen, eine Zone am Gebäude mit Feiertagsland Bayern."),
    (1068, "Referenzprojekt Zapfprofil Stochastisch",
     "Referenzprojekt Zapfprofil Stochastisch: Kopie von Projekt 1045, die Jahresreihe stochastisch mit festem Seed und zehn Realisierungen."),
    (1069, "Referenzprojekt Zapfprofil Typtage",
     "Referenzprojekt Zapfprofil Typtage: Kopie von Projekt 1045, der Jahresgang über einen erfundenen Typtagsatz (Herkunft fiktiv)."),
    (1070, "Referenzprojekt Zapfprofil Kalibrierung",
     "Referenzprojekt Zapfprofil Kalibrierung: Kopie von Projekt 1045; eine Zone mit Jahresmesswert, eine mit manuellem Tagesbedarf " +
     "und eigenen Fachwerten, Zirkulation manuell."),
    (1071, "Referenzprojekt Zapfprofil Auslegung",
     "Referenzprojekt Zapfprofil Auslegung: Kopie von Projekt 1045 mit den vier Topologien Speicher, Frischwasserstation, Durchfluss " +
     "und Wohnungsstation; Auslegung mit einem Ecodesign-Bedarfstag."),
};

// Der erfundene Typtagsatz: Klimazone, Gebaeudeart und sechs Kategorien (Code, Jahreszeit, Tagart, Kalendertage, Faktor).
// Kalendertage summieren sich auf 365, und Sigma n * F ist genau 0 (Pruefsumme der Methodik). Alle Zahlen erfunden.
const int TYPTAG_ZONE = 4;
const string TYPTAG_ART = "referenzhaus";
var typtage = new (string Code, string Jahreszeit, string Tagart, int Tage, string Faktor)[]
{
    ("R01", "uebergang", "werktag",  90,  "0.00005"),
    ("R02", "uebergang", "sonntag",  20, "-0.0001"),
    ("R03", "sommer",    "werktag", 100, "-0.0001"),
    ("R04", "sommer",    "sonntag",  25, "-0.0002"),
    ("R05", "winter",    "werktag", 105,  "0.0001"),
    ("R06", "winter",    "sonntag",  25,  "0.00008"),
};

string datei = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool trocken = args.Contains("--trocken");
if (datei == null || !File.Exists(datei))
{
    Console.Error.WriteLine("Aufruf: dotnet run referenzprojekte_zapfprofil_optionen.cs -- <Kenndaten_Test.sqlite> [--trocken]");
    return 2;
}
datei = Path.GetFullPath(datei);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

// Ohne Lizenz schreibt der Kern nur mit der Werkzeugfreigabe (Muster EPOS.Referenzlauf).
Schreibnaht.WerkzeugFreigabe("referenzprojekte_zapfprofil_optionen.cs (Saatskript der Testdatenbank)");
DataRepository.PfadUeberschreibung = datei;
Console.WriteLine("Schemastand:  " + DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation"));

// --- 1. Vorbedingungen ----------------------------------------------------------------------------------------------
foreach (var p in projekte)
{
    object stehtName = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", p.Name));
    object stehtId = DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", p.Id));
    if ((stehtName != null && stehtName != DBNull.Value) || (stehtId != null && stehtId != DBNull.Value))
    {
        Ende();
        Console.Error.WriteLine("Abbruch, die Datenbank bleibt unveraendert: Projekt " + p.Id + " '" + p.Name + "' steht schon " +
                                "(ID " + Text(stehtName) + ", Name an ID " + p.Id + ": '" + Text(stehtId) + "'). " +
                                "Fuer einen neuen Lauf die Sicherung zuruecklegen.");
        return 2;
    }
}
long hoechste = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Projekt"), CultureInfo.InvariantCulture);
if (hoechste != VORHER_HOECHSTE) return Vorbedingung("Die hoechste Projekt-ID ist " + hoechste + " - erwartet " + VORHER_HOECHSTE + ".");
object vorlageId = DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", new DbParam("?", VORLAGE_NAME));
if (vorlageId == null || Convert.ToInt32(vorlageId, CultureInfo.InvariantCulture) != VORLAGE)
    return Vorbedingung("Die Vorlage '" + VORLAGE_NAME + "' ist nicht Projekt " + VORLAGE + ".");
if (ZapfprofilCtrl.Weg(VORLAGE) != BrauchwasserWeg.Generator)
    return Vorbedingung("Die Vorlage " + VORLAGE + " rechnet nicht ueber den Zapfprofilgenerator.");
if (!DataRepository.TabelleVorhanden(TwwZonenferienSchema.TAB_ZONENFERIEN) || !TwwTyptagCtrl.TabelleVorhanden())
    return Vorbedingung("Die Tabellen der Zonenferien oder der Typtage fehlen - erst Werkzeuge/Testdatenbankschema ziehen.");
if (TwwTyptagCtrl.Stand().Zeilen != 0)
    return Vorbedingung("In Tab_TwwTyptag_IMPORT sind schon Typtage eingespielt - das Skript ersetzt keinen fremden Satz.");

int nWohnen = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Wohnen groß (abgeleitet)");
int nEfh = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Ein- und Zweifamilienhaus (abgeleitet)");
int nStudent = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Studentenwohnheim (abgeleitet)");
int nHotel = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Hotel (aus Messung, je Zimmer)");
int nBuero = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Büro (Setzung)");
int nSchule = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Schule (Setzung)");
int nGewerbe = Katalogzeile("Tab_TwwNutzungsart_STAMM", "Gewerbe Schichtbetrieb (Setzung)");
int tagL = Katalogzeile("Tab_TwwBedarfstag_STAMM", "Ecodesign-Zapfprofil L");
object ausst = DataRepository.ExecuteScalar("SELECT ID FROM Tab_TwwDin4708Wert_STAMM WHERE Art = 'AUSSTATTUNG' AND Schluessel = ? AND Katalogversion = ?",
                                            new DbParam("?", "Testklasse A"), new DbParam("?", KATALOG));
if (new[] { nWohnen, nEfh, nStudent, nHotel, nBuero, nSchule, nGewerbe, tagL }.Any(i => i <= 0) || ausst == null || ausst == DBNull.Value)
    return Vorbedingung("Eine Katalogzeile des Testkatalogs " + KATALOG + " fehlt oder steht mehrfach.");
int ausstattungA = Convert.ToInt32(ausst, CultureInfo.InvariantCulture);
foreach (string raum in new[] { "2", "3", "4" })
    if (DataRepository.ExecuteScalar("SELECT ID FROM Tab_TwwDin4708Wert_STAMM WHERE Art = 'BELEGUNG' AND Schluessel = ? AND Katalogversion = ?",
                                     new DbParam("?", raum), new DbParam("?", KATALOG)) == null)
        return Vorbedingung("Die Belegung nach Raumzahl " + raum + " fehlt im Testkatalog " + KATALOG + ".");
if (trocken) { Ende(); Console.WriteLine("--trocken: Vorbedingungen erfuellt, die Projekte 1066 bis 1071 wuerden angelegt."); return 0; }

// --- 2. Arbeitskopie und Kopien -------------------------------------------------------------------------------------
Ende();
string arbeit = datei + ".zb12arbeit";
foreach (string a in new[] { arbeit, arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
File.Copy(datei, arbeit);
DataRepository.PfadUeberschreibung = arbeit;

foreach (var p in projekte)
{
    int id = new ProjektDuplizierenCtrl().Duplizieren(VORLAGE_NAME, p.Name);
    Console.WriteLine("Kopie:        " + VORLAGE + " -> " + id + " '" + p.Name + "'");
    if (id != p.Id) return Abbruch("Die Kopie fiel auf " + id + " statt " + p.Id + ".");
    DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Kosten_Geaendert = NULL WHERE ID = ?",
                                   new DbParam("?", p.Beschreibung), new DbParam("?", id));
}

// --- 3. Typtage einspielen (Importweg des Kerns) ------------------------------------------------------------------------
using (var paket = new MemoryStream())
{
    using (var zip = new ZipArchive(paket, ZipArchiveMode.Create, true))
    {
        void Datei(string name, string inhalt)
        {
            using StreamWriter s = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
            s.Write(inhalt);
        }
        Datei(Normformvektorleser.DATEI_TYPTAGE, "code;jahreszeit;tagart;bewoelkung\n" +
              string.Concat(typtage.Select(t => t.Code + ";" + t.Jahreszeit + ";" + t.Tagart + ";ohne\n")));
        Datei(Normformvektorleser.DATEI_KLIMAZONEN, "zone;bezeichnung\n" + TYPTAG_ZONE + ";Zone " + TYPTAG_ZONE + " (fiktiv)\n");
        Datei(Normformvektorleser.DATEI_ANZAHL, "zone;gebaeudeart;typtag;anzahl\n" +
              string.Concat(typtage.Select(t => TYPTAG_ZONE + ";" + TYPTAG_ART + ";" + t.Code + ";" + t.Tage + "\n")));
        Datei(Normformvektorleser.DATEI_FAKTOREN, "gebaeudeart;zone;typtag;faktor\n" +
              string.Concat(typtage.Select(t => TYPTAG_ART + ";" + TYPTAG_ZONE + ";" + t.Code + ";" + t.Faktor + "\n")));
        Datei(Normformvektorleser.DATEI_KENNWERTE, "schluessel;wert;text\n" +
              "ausgabe;;2026-10\nquelle;;Referenztyptage (fiktiv)\n" +
              "heizgrenze." + TYPTAG_ART + ";16;\npruefsumme.toleranz;1e-9;\nwintergrenze;2;\n");
    }
    paket.Position = 0;
    TwwTyptagimportBericht bericht = TwwTyptagCtrl.Importieren(paket, DATUM);
    if (bericht.Abbruch != null) return Abbruch("Der Typtagimport lehnte ab: " + bericht.Abbruch.Klartext);
    Console.WriteLine("Typtage:      " + bericht.Zeilen + " Zeilen eingespielt" +
                      (bericht.Hinweise.Count == 0 ? "" : " - Hinweise: " + string.Join("; ", bericht.Hinweise.Select(h => h.Klartext))));
}

// --- 4. Die Zapfprofile ------------------------------------------------------------------------------------------------
var staende = new Dictionary<int, ZapfprofilStand>();
foreach (var p in projekte)
{
    int id = p.Id;
    int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("?", id)),
                                   CultureInfo.InvariantCulture);
    ProjektStand basis = ZapfprofilCtrl.Lies(id).Projekt with { Seed = id };
    ProjektStand projekt;
    List<ZonenStand> zonen;
    switch (id)
    {
        case 1066:
            projekt = basis with
            {
                ZirkAuto = true, ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge,
                ZirkLaengeM = 120.0, ZirkVerlustWJeM = 10.0, ZirkLaufzeitH = 18.0
            };
            zonen = new List<ZonenStand>
            {
                Zone(nWohnen, "Wohnen", 16.0) with
                {
                    IdGebaeude = gebaeude,
                    Wohnungen = new[]
                    {
                        new WohnungstypStand { Anzahl = 4, Raumzahl = 2.0, IdAusstattung = ausstattungA, Reihenfolge = 1 },
                        new WohnungstypStand { Anzahl = 2, Raumzahl = 3.0, IdAusstattung = ausstattungA, Reihenfolge = 2 },
                        new WohnungstypStand { Anzahl = 3, Raumzahl = 4.0, IdAusstattung = ausstattungA, Reihenfolge = 3 },
                    }
                },
                Zone(nBuero, "Büro", 40.0),
                Zone(nHotel, "Hotel", 20.0),
                Zone(nSchule, "Schule", 150.0) with { Zirkulation = false },
            };
            break;
        case 1067:
            projekt = basis;
            zonen = new List<ZonenStand>
            {
                Zone(nSchule, "Schule", 200.0) with
                {
                    Zirkulation = false,
                    Ferien = new[]
                    {
                        new ZonenferienStand { Name = "Weihnachten", Beginn = 355, Ende = 6 },
                        new ZonenferienStand { Name = "Winter", Beginn = 44, Ende = 48 },
                        new ZonenferienStand { Name = "Ostern", Beginn = 90, Ende = 103 },
                        new ZonenferienStand { Name = "Pfingsten", Beginn = 146, Ende = 151 },
                        new ZonenferienStand { Name = "Sommer", Beginn = 205, Ende = 247 },
                    }
                },
                Zone(nBuero, "Büro", 30.0) with { IdGebaeude = gebaeude, Zirkulation = false },
            };
            break;
        case 1068:
            projekt = basis with { JahresreiheStochastisch = true, Realisierungen = 10 };
            zonen = new List<ZonenStand> { Zone(nWohnen, "Wohnen", 8.3) with { IdGebaeude = gebaeude, Zirkulation = false } };
            break;
        case 1069:
            projekt = basis with { TyptageAktiv = true, TyptageKlimazone = TYPTAG_ZONE, TyptageGebaeudeart = TYPTAG_ART };
            zonen = new List<ZonenStand> { Zone(nWohnen, "Wohnen", 8.3) with { IdGebaeude = gebaeude, Zirkulation = false } };
            break;
        case 1070:
            projekt = basis with { ZirkAuto = false, ZirkManuellKw = 0.8, ZirkLaufzeitH = 16.0 };
            zonen = new List<ZonenStand>
            {
                Zone(nWohnen, "Wohnen gemessen", 10.0) with
                {
                    IdGebaeude = gebaeude,
                    Jahresmesswert = 7000.0, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
                    JahresmesswertBilanzgrenze = ZapfBilanzgrenze.Zapfstelle,
                    JahresmesswertQuelle = "Ablesung (fiktiv)", JahresmesswertZeitraum = "2025"
                },
                Zone(nGewerbe, "Gewerbe manuell", 20.0) with
                {
                    TagesbedarfAuto = false, TagesbedarfManuellKwh = 15.0,
                    ZapftemperaturC = 55.0, KaltwasserMittelC = 12.0, KaltwasserAmplitudeK = 4.0,
                    Auslastung = new double?[] { 1.0, 1.0, 1.0, 1.0, 1.0, 0.8, 0.6, 0.5, 1.0, 1.0, 1.0, 0.7 }
                },
            };
            break;
        default: // 1071
            projekt = basis with
            {
                BedarfstagQuelle = ZapfBedarfstagquelle.Ecodesign, IdBedarfstag = tagL,
                Erzeugerart = ZapfErzeugerart.Waermepumpe, UebertragerWerkstoff = ZapfUebertragerwerkstoff.Edelstahl
            };
            zonen = new List<ZonenStand>
            {
                Zone(nWohnen, "Speicher", 21.0) with
                {
                    IdGebaeude = gebaeude, Topologie = ZapfTopologie.Speicher,
                    Wohnungen = new[]
                    {
                        new WohnungstypStand { Anzahl = 6, Raumzahl = 3.0, Reihenfolge = 1 },
                        new WohnungstypStand { Anzahl = 4, Raumzahl = 4.0, Reihenfolge = 2 },
                    }
                },
                Zone(nStudent, "Frischwasserstation", 8.0) with
                {
                    Topologie = ZapfTopologie.Frischwasserstation,
                    Wohnungen = new[] { new WohnungstypStand { Anzahl = 8, Raumzahl = 2.0, Reihenfolge = 1 } }
                },
                Zone(nEfh, "Durchfluss", 3.0) with
                {
                    Topologie = ZapfTopologie.Durchfluss, Zirkulation = false,
                    Wohnungen = new[] { new WohnungstypStand { Anzahl = 1, Raumzahl = 4.0, Reihenfolge = 1 } }
                },
                Zone(nWohnen, "Wohnungsstation", 8.0) with
                {
                    Topologie = ZapfTopologie.Wohnungsstation, Zirkulation = false,
                    Wohnungen = new[] { new WohnungstypStand { Anzahl = 4, Raumzahl = 3.0, Personen = 2.0, Reihenfolge = 1 } }
                },
            };
            break;
    }
    try
    {
        staende[id] = ZapfprofilCtrl.Speichern(id, new ZapfprofilStand(BrauchwasserWeg.Generator, zonen, projekt));
    }
    catch (ZapfprofilSpeicherException ex) { return Abbruch("Projekt " + id + ": Speichern lehnte ab: " + ex.Satz.Klartext); }
    DataRepository.ExecuteNonQuery("UPDATE Tab_TwwProjekt SET Aenderungsdatum = ? WHERE ID_Projekt = ?",
                                   new DbParam("?", DATUM), new DbParam("?", id));
    if (id == 1067)
        DataRepository.ExecuteNonQuery("UPDATE Tab_Gebaeude SET Feiertagsland = ?, Wochenendtage = ? WHERE ID = ?",
                                       new DbParam("?", "BY"), new DbParam("?", 96L), new DbParam("?", gebaeude));
}

// --- 5. Rechnen und pruefen -------------------------------------------------------------------------------------------
foreach (var p in projekte)
{
    var pc = new ProjektCtrl();
    pc.ReadSingle(p.Id);
    var sim = new SimulationWaermebedarf();
    sim.Waermebedarf_berechnen(p.Id, pc.m_ID_Klimaregion);
    if (!string.IsNullOrEmpty(sim.Fehlertext)) return Abbruch("Projekt " + p.Id + ": der Generatorweg brach ab: " + sim.Fehlertext);
    ZapfprofilErgebnis e = sim.Zapfprofil;
    if (e == null) return Abbruch("Projekt " + p.Id + " rechnete nicht ueber den Generator.");
    if (e.Ablehnungen.Count > 0)
        return Abbruch("Projekt " + p.Id + ": abgelehnt: " + string.Join("; ", e.Ablehnungen.Select(a => a.Zone + ": " + a.Klartext)));
    Console.WriteLine();
    Console.WriteLine("Projekt " + p.Id + " '" + p.Name + "': Zapfung " + Z(e.Zapfung.JahressummeKwh) + " kWh/a, Zirkulation " +
                      Z(e.Zirkulation.JahressummeKwh) + " kWh/a, Kanal " + Z(sim.brauchwasserwerte.Sum()) + " kWh/a" +
                      (e.Stochastisch ? ", stochastisch" : ""));
    foreach (ZonenErgebnis z in e.JeZone)
        Console.WriteLine("  Zone " + z.Zone.PadRight(22) + " Bezug " + Z(z.Bezugsmenge) + ", Zapfung " + Z(z.JahresbedarfZapfungKwh) +
                          " kWh/a, Zirkulation " + Z(z.JahresverlustZirkulationKwh) + " kWh/a" +
                          (z.Kalibrierfaktor.HasValue ? ", Kalibrierfaktor " + Z(z.Kalibrierfaktor.Value) : "") +
                          ", Ruhetage " + (z.Kalender?.Count(t => t == ZapfTagtyp.Ruhetag) ?? 0));
    foreach (ZapfHinweis h in e.Hinweise) Console.WriteLine("  Hinweis: " + h.Text);
}

// Die Auslegung von 1071 (wirkt nicht auf die Simulation).
{
    if (!ZapfprofilCtrl.KalenderLesen(1071, out int w0, out bool[] we)) return Abbruch("Der Kalender von 1071 fehlt.");
    var uhr = System.Diagnostics.Stopwatch.StartNew();
    Auslegungsrechnung r = ZapfprofilCtrl.Auslegung(1071, ZapfprofilCtrl.Lies(1071), w0, we,
                                                    new Auslegungslauf(null, null, Stochastisch: true));
    Console.WriteLine("Auslegung in " + uhr.ElapsedMilliseconds + " ms");
    Console.WriteLine();
    Console.WriteLine("Auslegung 1071: " + r.Ergebnis.Gruppen.Count + " Gruppen, " + r.Ergebnis.Ablehnungen.Count + " Ablehnungen, Erzeugerart " + r.Erzeugerart);
    foreach (Auslegungsablehnung a in r.Ergebnis.Ablehnungen) Console.WriteLine("  Ablehnung " + a.Zone + ": " + a.Klartext);
    foreach (Auslegungsgruppe g in r.Ergebnis.Gruppen)
    {
        Auslegungsempfehlung em = g.Empfehlung;
        Console.WriteLine("  Gruppe " + g.Topologie + " [" + string.Join(", ", g.Zonen) + "] Bedarfstag " + (g.Bedarfstag?.Bezeichner ?? "-") +
                          ": Empfehlung " + em?.Verfahren + " rechenbar " + em?.Rechenbar + " V " + Z(em?.VolumenL) + " l, P " + Z(em?.LeistungKw) +
                          " kW, Nenninhalt " + Z(em?.NenninhaltL) + " l" + (em?.Rechenbar == false ? " - " + em.GrundText : ""));
        foreach (Auslegungswert w in g.Dreiergruppe)
            Console.WriteLine("    " + w.Verfahren + " " + w.Status + " V " + Z(w.VolumenL) + " l, P " + Z(w.LeistungKw) + " kW" + (w.Empfohlen ? " (empfohlen)" : ""));
    }
}

object integ = DataRepository.ExecuteScalar("PRAGMA integrity_check");
if (!"ok".Equals(Convert.ToString(integ, CultureInfo.InvariantCulture))) return Abbruch("integrity_check meldet " + integ);
if (DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count != 0) return Abbruch("foreign_key_check meldet Zeilen.");
Ende();
File.Copy(arbeit, datei, true);
File.Delete(arbeit);
foreach (string a in new[] { arbeit + "-wal", arbeit + "-shm" }) if (File.Exists(a)) File.Delete(a);
Console.WriteLine();
Console.WriteLine("Projekte 1066 bis 1071 angelegt; integrity_check ok, foreign_key_check leer.");
return 0;

// Eine Zone mit den Vorgaben des Stands; Reihenfolge vergibt der Schreibweg.
ZonenStand Zone(int nutzungsart, string name, double bezugsmenge)
    => new ZonenStand { IdNutzungsart = nutzungsart, Name = name, Bezugsmenge = bezugsmenge, Niveau = ZapfNiveau.Mittel,
                        Topologie = ZapfTopologie.Speicher, Zirkulation = true, TagesbedarfAuto = true };

// Die Id einer Katalogzeile ueber (Bezeichner, Katalogversion); 0, wenn sie fehlt oder mehrfach steht.
int Katalogzeile(string tabelle, string bezeichner)
{
    DataTable t = DataRepository.GetDataTable("SELECT ID FROM " + tabelle + " WHERE Bezeichner = ? AND Katalogversion = ?",
                                              new DbParam("?", bezeichner), new DbParam("?", KATALOG));
    return t != null && t.Rows.Count == 1 ? Convert.ToInt32(t.Rows[0][0], CultureInfo.InvariantCulture) : 0;
}

string Z(double? d) => d.HasValue ? d.Value.ToString("R", CultureInfo.InvariantCulture) : "-";

string Text(object o) => o == null || o == DBNull.Value ? "-" : Convert.ToString(o, CultureInfo.InvariantCulture);

int Vorbedingung(string grund)
{
    Ende();
    Console.Error.WriteLine("Abbruch, die Datenbank bleibt unveraendert: " + grund);
    return 2;
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
