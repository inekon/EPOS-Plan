using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Leser der HottCAD-Projektdatei</b> (<c>.sqproj</c>, SQLite 3; Datenaustauschkonzept 16.2, Befund Kapitel 2
    /// und 3.1–3.4). Öffnet die Datei <b>nur lesend</b> (<c>Mode=ReadOnly</c>, ohne Pool, <c>query_only</c>), prüft zuerst
    /// das Register <c>XmTables</c> und die benötigten Tabellen (<see cref="TABELLEN"/>; fehlt eine, ist die Projektdatei
    /// benannt abgelehnt) und liest dann Gebäude, Geschosse, Räume, die Zonen der belegten Typen 5 und 6 mit ihren Räumen,
    /// je Zone das Nutzungsprofil bzw. die Profilgruppe mit den Zeitprofilen der fünf Klassen samt 24 Stunden, Nennwerten
    /// und Abschnitten. Unbekannte Codes werden gezählt und benannt (Regel 16.6 Nr. 2); alle Reihenfolgen sind fest.
    ///
    /// <para><b>Fremd-SQL:</b> Die Anweisungen dieser Datei sprechen das Schema der Projektdatei, nicht das eigene; sie
    /// laufen nicht über <see cref="DataRepository"/>, sind feste Texte ohne Verkettung und stehen im
    /// <c>SqlDialektPruefer</c> unter <c>FREMDSCHEMA</c> (Musterregeln ja, <c>EXPLAIN</c> gegen die eigene Datenbank nein).
    /// Der Leser hält keine Verbindung über <see cref="Lesen"/> hinaus.</para>
    /// </summary>
    internal static class SqprojLeser
    {
        /// <summary>Die benötigten Tabellen (Datenaustauschkonzept 16.3) in Prüfreihenfolge.</summary>
        internal static readonly string[] TABELLEN =
        {
            "XmTables", "BmBuilding", "BmFloor", "BmRoom", "BmZone", "BmZoneReference",
            "PdProfile", "PdProfileUsage", "PdProfileHeating", "PdProfileCooling", "PdProfileVentilation",
            "PdProfileDevice", "PdProfilePerson", "PdProfileTimeCurve", "PdProfileReference", "PdProfileGroup",
            "PdProfileGroupReference", "PdProfileTaskSerial", "PdProfileTaskSerialReference",
        };

        /// <summary>Die höchste Tabellenfassung, die der Befund belegt (14.4 bis 17.6); darüber wird benannt gewarnt.</summary>
        internal const double FASSUNG_MAX = 17.9;

        /// <summary>Die Delphi-Nullzeit — „keine Angabe“ (Befund 2.2).</summary>
        internal const string NULLZEIT = "1899-12-30";

        /// <summary>Die Kandidatenspalten der spezifischen Geräteleistung [W/m²] in <c>PdProfileDevice</c>, in dieser Rangfolge.</summary>
        internal static readonly string[] GERAETE_SPALTEN = { "SpecificRatedPower", "SpecificRatedThermalOutput", "SpecificRatedHeatEmission" };

        // Feste Texte gegen das Schema der Projektdatei (FREMDSCHEMA im SqlDialektPruefer).
        private const string SQL_TABELLEN = "SELECT name FROM sqlite_master WHERE type = 'table'";
        private const string SQL_REGISTER = "SELECT * FROM XmTables";
        private const string SQL_GEBAEUDE = "SELECT * FROM BmBuilding";
        private const string SQL_GESCHOSS = "SELECT * FROM BmFloor";
        private const string SQL_RAUM = "SELECT * FROM BmRoom";
        private const string SQL_ZONE = "SELECT * FROM BmZone";
        private const string SQL_ZONENBEZUG = "SELECT * FROM BmZoneReference";
        private const string SQL_PROFIL = "SELECT * FROM PdProfile";
        private const string SQL_NUTZUNG = "SELECT * FROM PdProfileUsage";
        private const string SQL_GERAETE = "SELECT * FROM PdProfileDevice";
        private const string SQL_PERSON = "SELECT * FROM PdProfilePerson";
        private const string SQL_KURVE = "SELECT * FROM PdProfileTimeCurve";
        private const string SQL_PROFILBEZUG = "SELECT * FROM PdProfileReference";
        private const string SQL_GRUPPE = "SELECT * FROM PdProfileGroup";
        private const string SQL_GRUPPENBEZUG = "SELECT * FROM PdProfileGroupReference";
        private const string SQL_ABSCHNITT = "SELECT * FROM PdProfileTaskSerial";
        private const string SQL_ABSCHNITTSBEZUG = "SELECT * FROM PdProfileTaskSerialReference";
        private const string SQL_NUR_LESEN = "PRAGMA query_only = 1";

        /// <summary>Die Bauteiltabellen (BA-4b, Befund Projektdatei N.10) — optional: Fehlen sie, bleibt es beim Stand ohne Aufbauten.</summary>
        internal static readonly string[] BAUTEIL_TABELLEN =
        {
            "BmElement", "BmElementReference", "TcBuildingElementDimension", "TcBuildingElementDimensionLayer",
        };

        // Nur die benötigten Spalten — ohne Binärströme (Konzept HottCAD-Verbund 9).
        private const string SQL_HUELLFLAECHE =
            "SELECT UUID, GId, ElementType, AdjacentType, CatalogDimUUID, UValue, NetArea FROM BmElement WHERE RepositoryLevel = 3";
        // Lage und Öffnungen der Hüllflächen (Gebäudeabbild allein aus der Projektdatei) — eigene Anweisung, damit eine Datei
        // ohne diese Spalten den Stand der Aufbauten behält.
        private const string SQL_HUELLFLAECHE_LAGE =
            "SELECT UUID, GrossArea, Orientation, Slope, ParentUUID, RepositoryElementUUID FROM BmElement WHERE RepositoryLevel = 3";
        private const string SQL_FENSTER = "SELECT UUID, GValue, FractionOfFrame FROM BmElementWindow";
        private const string SQL_STANDORT = "SELECT UUID, Location, PostalCode, Latitude, Longitude FROM SmSite";
        private const string SQL_ELEMENTBEZUG =
            "SELECT UUID, Id, SortNum, ReferenceFromUUID, ReferenceToUUID, ReferenceType FROM BmElementReference";
        private const string SQL_AUFBAU =
            "SELECT UId, ShortDesc, LongDesc, UValue, Thickness, InternalCoefficientOfHeatTransfer, ExternalCoefficientOfHeatTransfer " +
            "FROM TcBuildingElementDimension";
        private const string SQL_AUFBAUSCHICHT =
            "SELECT UId, DimensionUId, SortNum, ShortDesc, LayerType, MaterialType, MaterialGroupType, Thickness, ThermalConductivity, " +
            "Density, HeatCapacity FROM TcBuildingElementDimensionLayer";

        /// <summary>
        /// <b>Öffnet die Datei nur lesend</b>: <c>Mode=ReadOnly</c>, ohne Verbindungspool (die Datei ist danach sofort frei),
        /// <c>query_only</c>. Jeder Schreibversuch scheitert mit einer <see cref="SqliteException"/>.
        /// </summary>
        internal static SqliteConnection Oeffnen(string pfad)
        {
            var b = new SqliteConnectionStringBuilder { DataSource = pfad, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
            var c = new SqliteConnection(b.ToString());
            c.Open();
            using (SqliteCommand k = c.CreateCommand())
            {
                k.CommandText = SQL_NUR_LESEN;
                k.ExecuteNonQuery();
            }
            return c;
        }

        /// <summary>Beginnt die Datei mit der SQLite-Kennung (<c>SQLite format 3\0</c>)?</summary>
        internal static bool IstSqlite(string pfad)
        {
            try
            {
                byte[] kopf = new byte[16];
                using (FileStream f = File.OpenRead(pfad))
                    if (f.Read(kopf, 0, 16) != 16) return false;
                return System.Text.Encoding.ASCII.GetString(kopf, 0, 15) == "SQLite format 3" && kopf[15] == 0;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        /// <summary>
        /// <b>Liest die Projektdatei</b> unter <paramref name="pfad"/> (eine Arbeitskopie, die der Aufrufer anlegt). Wirft
        /// nicht: Was nicht gelesen werden kann, ist eine benannte <see cref="SqprojAbbild.Ablehnung"/>.
        /// </summary>
        internal static SqprojAbbild Lesen(string pfad)
        {
            var a = new SqprojAbbild();
            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad) || !IstSqlite(pfad))
            {
                a.Ablehnung = new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.KEINE_DATEI, Path.GetFileName(pfad ?? ""));
                return a;
            }
            try
            {
                using (SqliteConnection c = Oeffnen(pfad))
                    Lesen(c, a);
            }
            catch (SqliteException ex)
            {
                a.Ablehnung = new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                a.Ablehnung = new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            catch (FormatException ex)
            {
                a.Ablehnung = new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            return a;
        }

        private static void Lesen(SqliteConnection c, SqprojAbbild a)
        {
            // 1) Register und Tabellen
            var vorhanden = new HashSet<string>(Zeilen(c, SQL_TABELLEN).Select(z => Text(z, "name")), StringComparer.OrdinalIgnoreCase);
            List<string> fehlen = TABELLEN.Where(t => !vorhanden.Contains(t)).ToList();
            if (fehlen.Count > 0)
            {
                a.Ablehnung = new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.TABELLE_FEHLT, string.Join(", ", fehlen));
                return;
            }
            var fassung = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_REGISTER))
                if (Text(z, "Name") is string n && !fassung.ContainsKey(n)) fassung[n] = Text(z, "Version");
            a.Fassung = fassung.TryGetValue("BmRoom", out string fr) ? fr : null;
            a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.FASSUNG, a.Fassung ?? "—"));
            foreach (string t in TABELLEN.Skip(1))
                if (fassung.TryGetValue(t, out string v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double f) && f > FASSUNG_MAX)
                    a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.FASSUNG_UNBEKANNT, t, v));

            // 2) Gebäude, Geschosse, Räume
            List<Dictionary<string, object>> gebaeudeZeilen = Zeilen(c, SQL_GEBAEUDE);
            a.Gebaeudename = gebaeudeZeilen.Select(z => Name(z)).OrderBy(n => n, StringComparer.Ordinal).FirstOrDefault();
            var gebaeudeKennungen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in gebaeudeZeilen)
                if (Text(z, "UUID") is string u && gebaeudeKennungen.Add(u))
                    a.Gebaeude.Add(new SqprojGebaeude
                    {
                        Uuid = u, Name = Name(z), BaujahrText = Text(z, "YearOfConstruction"),
                        Baujahr = Baujahr(z), StandortUuid = SqprojBauteilcodes.Kennung(Text(z, "SiteUUID")) == null ? null : Text(z, "SiteUUID"),
                    });
            a.Gebaeude.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name) is int n && n != 0 ? n : string.CompareOrdinal(x.Uuid, y.Uuid));
            var geschosse = new Dictionary<string, SqprojGeschoss>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_GESCHOSS))
                if (Text(z, "UUID") is string u && !geschosse.ContainsKey(u))
                    geschosse[u] = new SqprojGeschoss(u, Name(z), Text(z, "BuildingUUID"),
                                                      SqprojBauteilcodes.Gesetzt(Zahl(z, "ElevationOfRefHeight")),
                                                      Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "Height"))));
            var geschossName = geschosse.ToDictionary(p => p.Key, p => p.Value.Name, StringComparer.OrdinalIgnoreCase);
            a.Geschosse.AddRange(geschosse.Values.OrderBy(g => g.Name, StringComparer.Ordinal).ThenBy(g => g.Uuid, StringComparer.Ordinal));
            var raeume = new List<SqprojRaum>();
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_RAUM))
            {
                string geschoss = Text(z, "FloorUUID");
                raeume.Add(new SqprojRaum
                {
                    Uuid = Text(z, "UUID") ?? "",
                    Name = Name(z),
                    GeschossUuid = geschoss,
                    GeschossName = geschoss != null && geschossName.TryGetValue(geschoss, out string gn) ? gn : null,
                    Gid = Text(z, "GId"),
                    Raumart = Ganz(z, "RoomType"),
                    FlaecheM2 = Positiv(Zahl(z, "Area")),
                    VolumenM3 = Positiv(Zahl(z, "Volume")),
                    HoeheM = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "Height"))),
                    Beheizung = Ganz(z, "HeatingType"),
                });
            }
            a.Raeume.AddRange(raeume.OrderBy(r => r.GeschossName ?? "", StringComparer.Ordinal)
                                    .ThenBy(r => r.Name, StringComparer.Ordinal).ThenBy(r => r.Uuid, StringComparer.Ordinal));
            var raumKennungen = new HashSet<string>(a.Raeume.Select(r => r.Uuid), StringComparer.OrdinalIgnoreCase);
            var raumRang = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < a.Raeume.Count; i++) raumRang.TryAdd(a.Raeume[i].Uuid, i);

            // 3) Zonen der Typen 5 und 6; die übrigen gezählt
            var zonen = new List<SqprojZone>();
            var gruppeJeZone = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_ZONE))
            {
                int? typ = Ganz(z, "ZoneType");
                if (typ != SqprojAbbild.ZONENTYP_NUTZUNG && typ != SqprojAbbild.ZONENTYP_SIMULATION)
                {
                    int k = typ ?? -1;
                    a.ZonentypenUebersprungen[k] = a.ZonentypenUebersprungen.TryGetValue(k, out int n) ? n + 1 : 1;
                    continue;
                }
                var zone = new SqprojZone
                {
                    Uuid = Text(z, "UUID") ?? "", Name = Name(z), Zonentyp = typ,
                    FlaecheM2 = Positiv(Zahl(z, "Area")), VolumenM3 = Positiv(Zahl(z, "Volume")),
                };
                if (Text(z, "ProfileGroupUUID") is string g) gruppeJeZone[zone.Uuid] = g;
                zonen.Add(zone);
            }
            zonen = zonen.OrderBy(z => z.Name, StringComparer.Ordinal).ThenBy(z => z.Uuid, StringComparer.Ordinal).ToList();
            var zoneJeKennung = new Dictionary<string, SqprojZone>(StringComparer.OrdinalIgnoreCase);
            foreach (SqprojZone z in zonen) zoneJeKennung.TryAdd(z.Uuid, z);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_ZONENBEZUG))
            {
                string klasse = Text(z, "ReferenceClass");
                if (klasse != null && !string.Equals(klasse, "TModelRoom", StringComparison.Ordinal)) continue;
                if (Text(z, "UUID") is string zu && zoneJeKennung.TryGetValue(zu, out SqprojZone zone)
                    && Text(z, "ReferenceToUUID") is string ru && raumKennungen.Contains(ru)
                    && !zone.Raeume.Contains(ru, StringComparer.OrdinalIgnoreCase))
                    zone.Raeume.Add(ru);
            }
            foreach (SqprojZone z in zonen) z.Raeume.Sort((x, y) => raumRang[x].CompareTo(raumRang[y]));
            a.Zonen.AddRange(zonen);
            foreach (KeyValuePair<int, int> p in a.ZonentypenUebersprungen)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.ZONENTYP_UEBERSPRUNGEN,
                    p.Key < 0 ? "—" : SqprojProtokoll.Z(p.Key), SqprojProtokoll.Z(p.Value)));

            // 4) Profile
            var profile = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_PROFIL))
                if (Text(z, "UUID") is string u) profile.TryAdd(u, z);
            foreach (Dictionary<string, object> p in profile.Values.OrderBy(p => Text(p, "UUID"), StringComparer.Ordinal))
            {
                int k = Ganz(p, "ProfileType") ?? -1;
                if (k == SqprojProfilklasse.NUTZUNG || SqprojProfilklasse.GELESEN.Contains(k)) continue;
                a.KlassenUebersprungen[k] = a.KlassenUebersprungen.TryGetValue(k, out int n) ? n + 1 : 1;
            }
            foreach (KeyValuePair<int, int> p in a.KlassenUebersprungen)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.KLASSE_UEBERSPRUNGEN,
                    p.Key < 0 ? "—" : SqprojProtokoll.Z(p.Key), SqprojProtokoll.Z(p.Value)));
            Dictionary<string, Dictionary<string, object>> nutzung = JeKennung(Zeilen(c, SQL_NUTZUNG));
            Dictionary<string, Dictionary<string, object>> person = JeKennung(Zeilen(c, SQL_PERSON));
            Dictionary<string, Dictionary<string, object>> geraete = JeKennung(Zeilen(c, SQL_GERAETE));

            // 4a) Nutzungsprofil je Zone (PdProfileReference: UUID = Profil, ReferenceToUUID = Zone)
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_PROFILBEZUG)
                         .OrderBy(z => Text(z, "UUID") ?? "", StringComparer.Ordinal).ThenBy(z => Text(z, "ReferenceToUUID") ?? "", StringComparer.Ordinal))
            {
                if (!(Text(z, "UUID") is string pu) || !profile.TryGetValue(pu, out Dictionary<string, object> kopf)) continue;
                if (Ganz(kopf, "ProfileType") != SqprojProfilklasse.NUTZUNG) continue;
                if (!(Text(z, "ReferenceToUUID") is string zu) || !zoneJeKennung.TryGetValue(zu, out SqprojZone zone) || zone.Nutzungsprofil != null) continue;
                nutzung.TryGetValue(pu, out Dictionary<string, object> n);
                zone.Nutzungsprofil = Nutzungsprofil(kopf, n);
            }

            // 4b) Zeitkurven, Abschnitte, Gruppen
            var kurven = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_KURVE))
                if (Text(z, "ProfileUUID") is string pu)
                {
                    if (!kurven.TryGetValue(pu, out List<Dictionary<string, object>> l)) kurven[pu] = l = new List<Dictionary<string, object>>();
                    l.Add(z);
                }
            Dictionary<string, Dictionary<string, object>> abschnitte = JeKennung(Zeilen(c, SQL_ABSCHNITT));
            var abschnitteJeProfil = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_ABSCHNITTSBEZUG))
                if (Text(z, "UUID") is string su && Text(z, "ReferenceToUUID") is string pu && abschnitte.ContainsKey(su))
                {
                    if (!abschnitteJeProfil.TryGetValue(pu, out List<string> l)) abschnitteJeProfil[pu] = l = new List<string>();
                    if (!l.Contains(su, StringComparer.OrdinalIgnoreCase)) l.Add(su);
                }
            Dictionary<string, Dictionary<string, object>> gruppen = JeKennung(Zeilen(c, SQL_GRUPPE));
            var profileJeGruppe = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_GRUPPENBEZUG))
                if (Text(z, "UUID") is string gu && Text(z, "ReferenceToUUID") is string pu)
                {
                    if (!profileJeGruppe.TryGetValue(gu, out List<string> l)) profileJeGruppe[gu] = l = new List<string>();
                    if (!l.Contains(pu, StringComparer.OrdinalIgnoreCase)) l.Add(pu);
                }
            foreach (Dictionary<string, object> p in profile.Values)
                if (Text(p, "ProfileGroupUUID") is string gu && Text(p, "UUID") is string pu)
                {
                    if (!profileJeGruppe.TryGetValue(gu, out List<string> l)) profileJeGruppe[gu] = l = new List<string>();
                    if (!l.Contains(pu, StringComparer.OrdinalIgnoreCase)) l.Add(pu);
                }

            var gruppeGelesen = new Dictionary<string, SqprojProfilgruppe>(StringComparer.OrdinalIgnoreCase);
            int abschnittsartUnbekannt = 0;
            var tagesartGemeldet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string gebaeudegruppe = Zeilen(c, SQL_GEBAEUDE).Select(z => Text(z, "ProfileGroupUUID")).Where(u => u != null)
                                                           .OrderBy(u => u, StringComparer.Ordinal).FirstOrDefault();
            // Die Zonengruppen in Zonenreihenfolge, zuletzt die Gebäudegruppe (Gebäudeebene).
            var traeger = zonen.Select(z => (Zone: z, Gruppe: gruppeJeZone.TryGetValue(z.Uuid, out string g) ? g : null)).ToList();
            if (gebaeudegruppe != null) traeger.Add((null, gebaeudegruppe));
            foreach ((SqprojZone zone, string gu) in traeger)
            {
                if (gu == null || !gruppen.TryGetValue(gu, out Dictionary<string, object> gz)) continue;
                if (!gruppeGelesen.TryGetValue(gu, out SqprojProfilgruppe gruppe))
                {
                    int? code = Ganz(gz, "ProfileUsageDayType");
                    gruppe = new SqprojProfilgruppe
                    {
                        Uuid = gu, Name = Name(gz), TagesartCode = code, Tagesart = SqprojProtokoll.Tagesart(code),
                        Profilnummer = Ganz(gz, "ProfileUsageType"), Gruppenart = Ganz(gz, "ProfileGroupType"),
                    };
                    List<Dictionary<string, object>> mitglieder = (profileJeGruppe.TryGetValue(gu, out List<string> l) ? l : new List<string>())
                        .Where(profile.ContainsKey).Select(u => profile[u])
                        .OrderBy(Name, StringComparer.Ordinal).ThenBy(p => Text(p, "UUID"), StringComparer.Ordinal).ToList();
                    foreach (Dictionary<string, object> p in mitglieder)
                    {
                        int k = Ganz(p, "ProfileType") ?? -1;
                        if (!SqprojProfilklasse.GELESEN.Contains(k) || gruppe.Profile.ContainsKey(k)) continue;
                        string pu = Text(p, "UUID");
                        person.TryGetValue(pu, out Dictionary<string, object> pe);
                        geraete.TryGetValue(pu, out Dictionary<string, object> ge);
                        var zp = new SqprojZeitprofil
                        {
                            Uuid = pu, Name = Name(p), Klasse = k,
                            Personen = k == SqprojProfilklasse.PERSONEN ? Positiv(Zahl(pe, "RatedPersonOccupancyRate")) : null,
                            WattJePerson = k == SqprojProfilklasse.PERSONEN ? Positiv(Zahl(pe, "SpecificRatedDryHeatEmission")) : null,
                            GeraeteWm2 = k == SqprojProfilklasse.GERAETE ? GERAETE_SPALTEN.Select(s => Positiv(Zahl(ge, s))).FirstOrDefault(w => w.HasValue) : null,
                        };
                        string spalte = SqprojProfilklasse.Kurvenspalte(k);
                        foreach (Dictionary<string, object> kz in (kurven.TryGetValue(pu, out List<Dictionary<string, object>> kl) ? kl : new List<Dictionary<string, object>>())
                                     .OrderBy(kz => Ganz(kz, "HourType") ?? 0))
                        {
                            int? h = Ganz(kz, "HourType");
                            if (h is int stunde && stunde >= 1 && stunde <= 24 && !zp.Stunden[stunde - 1].HasValue)
                                zp.Stunden[stunde - 1] = Zahl(kz, spalte);
                            if (h is int st && st >= 1 && st <= 24) zp.Betriebsarten[st - 1] ??= Ganz(kz, "OperatingModeType");
                            zp.Betriebsart ??= Ganz(kz, "OperatingModeType");
                        }
                        foreach (Dictionary<string, object> s in (abschnitteJeProfil.TryGetValue(pu, out List<string> sl) ? sl : new List<string>())
                                     .Select(u => abschnitte[u])
                                     .OrderBy(s => Ganz(s, "TaskStartDay") ?? 1).ThenBy(s => Ganz(s, "TaskEndDay") ?? 365).ThenBy(s => Text(s, "UUID"), StringComparer.Ordinal))
                        {
                            var ab = Abschnitt(s);
                            if (ab.Abschnittsart != SqprojProtokoll.ABSCHNITTSART_BELEGT) abschnittsartUnbekannt++;
                            zp.Abschnitte.Add(ab);
                        }
                        gruppe.Profile[k] = zp;
                        a.Zeitprofile++;
                        a.Abschnitte += zp.Abschnitte.Count;
                        foreach (int ba in zp.Betriebsarten.Select(b => b ?? -1).Distinct().OrderBy(b => b))
                            a.Betriebsarten[ba] = a.Betriebsarten.TryGetValue(ba, out int n) ? n + 1 : 1;
                    }
                    gruppeGelesen[gu] = gruppe;
                }
                if (zone != null) zone.Gruppe = gruppe;
                else a.Gebaeudegruppe = gruppe;
                if (!tagesartGemeldet.Add(gu)) continue;
                if (gruppe.Tagesart == SqprojTagesart.Unbekannt)
                    a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.TAGESART_UNBEKANNT, gruppe.Name,
                        gruppe.TagesartCode.HasValue ? SqprojProtokoll.Z(gruppe.TagesartCode.Value) : "—"));
                else
                    a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.TAGESART_ANNAHME, gruppe.Name,
                        SqprojProtokoll.Z(gruppe.TagesartCode.Value), SqprojProtokoll.Z(SqprojProtokoll.Wochentage(gruppe.Tagesart).Value)));
            }
            foreach (KeyValuePair<int, int> p in a.Betriebsarten)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.BETRIEBSART,
                    p.Key < 0 ? "—" : SqprojProtokoll.Z(p.Key), SqprojProtokoll.Z(p.Value)));
            if (abschnittsartUnbekannt > 0)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.ABSCHNITTSART_UNBEKANNT, SqprojProtokoll.Z(abschnittsartUnbekannt)));

            // 5) Bauteile und Aufbauten (BA-4b) — optional
            BauteileLesen(c, a, vorhanden, fassung);
            StandorteLesen(c, a, vorhanden);
        }

        /// <summary>
        /// <b>Liest Hüllflächen, Raumbezüge und den Aufbaukatalog</b> (BA-4b, Befund Projektdatei N.1–N.10): die Level-3-Zeilen
        /// von <c>BmElement</c> mit ihren Bezügen (mehrere je Zeile zulässig), alle Aufbauten mit ihren Schichten innen → außen
        /// (<c>SortNum</c> aufsteigend), c · 1 000, Platzhalter und Null-Kennung als „nicht gesetzt“. Fehlt eine der vier
        /// Tabellen oder ist eine Spalte nicht lesbar, bleibt der Stand ohne Aufbauten — benannt, ohne die Datei abzulehnen.
        /// </summary>
        private static void BauteileLesen(SqliteConnection c, SqprojAbbild a, HashSet<string> vorhanden, Dictionary<string, string> fassung)
        {
            List<string> fehlen = BAUTEIL_TABELLEN.Where(t => !vorhanden.Contains(t)).ToList();
            if (fehlen.Count > 0)
            {
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.BAUTEILE_FEHLEN, string.Join(", ", fehlen)));
                return;
            }
            foreach (string t in BAUTEIL_TABELLEN)
                if (fassung.TryGetValue(t, out string v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double f) && f > FASSUNG_MAX)
                    a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.FASSUNG_UNBEKANNT, t, v));
            try
            {
                var aufbauten = new Dictionary<string, SqprojAufbau>(StringComparer.OrdinalIgnoreCase);
                foreach (Dictionary<string, object> z in Zeilen(c, SQL_AUFBAU))
                {
                    string k = SqprojBauteilcodes.Kennung(Text(z, "UId"));
                    if (k == null || aufbauten.ContainsKey(k)) continue;
                    aufbauten[k] = new SqprojAufbau
                    {
                        Kennung = k,
                        Name = Text(z, "ShortDesc") ?? Text(z, "LongDesc"),
                        UWert = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "UValue"))),
                        DickeM = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "Thickness"))),
                        // Rsi/Rse stehen trotz der Namen als Widerstände [m²K/W] in diesen Spalten (Befund N.5).
                        RsiM2KW = SqprojBauteilcodes.Gesetzt(Zahl(z, "InternalCoefficientOfHeatTransfer")),
                        RseM2KW = SqprojBauteilcodes.Gesetzt(Zahl(z, "ExternalCoefficientOfHeatTransfer")),
                    };
                }
                var schichten = new List<(SqprojAufbau Aufbau, int Sort, SqprojSchicht Schicht)>();
                foreach (Dictionary<string, object> z in Zeilen(c, SQL_AUFBAUSCHICHT))
                {
                    string d = SqprojBauteilcodes.Kennung(Text(z, "DimensionUId"));
                    if (d == null || !aufbauten.TryGetValue(d, out SqprojAufbau auf)) continue;
                    schichten.Add((auf, Ganz(z, "SortNum") ?? 0, new SqprojSchicht
                    {
                        Kennung = SqprojBauteilcodes.Kennung(Text(z, "UId")),
                        Name = Text(z, "ShortDesc"),
                        Schichttyp = Ganz(z, "LayerType"),
                        Stofftyp = Ganz(z, "MaterialType"),
                        Stoffgruppe = Ganz(z, "MaterialGroupType"),
                        DickeM = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "Thickness"))),
                        LambdaWmK = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "ThermalConductivity"))),
                        RhoKgM3 = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "Density"))),
                        CpJkgK = SqprojBauteilcodes.CpJkgK(SqprojBauteilcodes.Gesetzt(Zahl(z, "HeatCapacity"))),
                    }));
                }
                foreach (var g in schichten.GroupBy(x => x.Aufbau))
                {
                    int stelle = 0;
                    foreach (var x in g.OrderBy(x => x.Sort).ThenBy(x => x.Schicht.Kennung ?? "", StringComparer.Ordinal))
                        g.Key.Schichten.Add(new SqprojSchicht
                        {
                            Kennung = x.Schicht.Kennung, Name = x.Schicht.Name, Stelle = stelle++, Schichttyp = x.Schicht.Schichttyp,
                            Stofftyp = x.Schicht.Stofftyp, Stoffgruppe = x.Schicht.Stoffgruppe, DickeM = x.Schicht.DickeM,
                            LambdaWmK = x.Schicht.LambdaWmK, RhoKgM3 = x.Schicht.RhoKgM3, CpJkgK = x.Schicht.CpJkgK,
                        });
                }

                var flaechen = new Dictionary<string, SqprojHuellflaeche>(StringComparer.OrdinalIgnoreCase);
                foreach (Dictionary<string, object> z in Zeilen(c, SQL_HUELLFLAECHE))
                {
                    string u = Text(z, "UUID");
                    if (u == null || flaechen.ContainsKey(u)) continue;
                    flaechen[u] = new SqprojHuellflaeche
                    {
                        Uuid = u,
                        Gid = SqprojBauteilcodes.Kennung(Text(z, "GId")),
                        Elementtyp = Ganz(z, "ElementType"),
                        Nachbarart = Ganz(z, "AdjacentType"),
                        AufbauKennung = SqprojBauteilcodes.Kennung(Text(z, "CatalogDimUUID")),
                        UWert = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "UValue"))),
                        NettoM2 = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "NetArea"))),
                    };
                }
                foreach (Dictionary<string, object> z in Zeilen(c, SQL_ELEMENTBEZUG)
                             .OrderBy(z => Ganz(z, "SortNum") ?? 0).ThenBy(z => Ganz(z, "Id") ?? 0).ThenBy(z => Text(z, "UUID") ?? "", StringComparer.Ordinal))
                {
                    string ziel = Text(z, "ReferenceToUUID"), raum = Text(z, "ReferenceFromUUID");
                    if (ziel == null || raum == null || !flaechen.TryGetValue(ziel, out SqprojHuellflaeche h)) continue;
                    h.Bezuege.Add(new SqprojBezug(raum, Ganz(z, "ReferenceType"), h.Bezuege.Count));
                }
                foreach (SqprojAufbau x in aufbauten.Values.OrderBy(x => x.Kennung, StringComparer.Ordinal)) a.Aufbauten[x.Kennung] = x;
                a.Huellflaechen.AddRange(flaechen.Values.OrderBy(h => h.Uuid, StringComparer.Ordinal));
                LageLesen(c, a, flaechen, vorhanden);
                a.BauteileGelesen = true;
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.BAUTEILE,
                    SqprojProtokoll.Z(a.Huellflaechen.Count), SqprojProtokoll.Z(a.Aufbauten.Count),
                    SqprojProtokoll.Z(a.Aufbauten.Values.Count(x => x.HatSchichten)), SqprojProtokoll.Z(a.Aufbauten.Values.Sum(x => x.Schichten.Count))));
            }
            catch (Exception ex) when (ex is SqliteException || ex is InvalidOperationException || ex is FormatException)
            {
                a.Huellflaechen.Clear();
                a.Aufbauten.Clear();
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.BAUTEILE_UNLESBAR, ex.Message));
            }
        }

        /// <summary>
        /// <b>Liest Lage und Öffnungen der Hüllflächen</b> (Gebäudeabbild allein aus der Projektdatei): Bruttofläche,
        /// Orientierung, Neigung, Wirt (<c>ParentUUID</c>) und CAD-Objekt (<c>RepositoryElementUUID</c>), dazu g-Wert und
        /// Rahmenanteil aus <c>BmElementWindow</c> (optional). Fehlt eine Spalte, bleibt es bei Aufbau, U-Wert und Nettofläche —
        /// benannt als Info, ohne die Bauteile zu verwerfen.
        /// </summary>
        private static void LageLesen(SqliteConnection c, SqprojAbbild a, Dictionary<string, SqprojHuellflaeche> flaechen, HashSet<string> vorhanden)
        {
            try
            {
                foreach (Dictionary<string, object> z in Zeilen(c, SQL_HUELLFLAECHE_LAGE))
                {
                    if (Text(z, "UUID") is not string u || !flaechen.TryGetValue(u, out SqprojHuellflaeche h)) continue;
                    h.BruttoM2 = Positiv(SqprojBauteilcodes.Gesetzt(Zahl(z, "GrossArea")));
                    h.OrientierungGrad = SqprojBauteilcodes.Gesetzt(Zahl(z, "Orientation"));
                    h.NeigungGrad = SqprojBauteilcodes.Gesetzt(Zahl(z, "Slope"));
                    h.Eltern = SqprojBauteilcodes.Kennung(Text(z, "ParentUUID")) == null ? null : Text(z, "ParentUUID");
                    h.CadObjekt = SqprojBauteilcodes.Kennung(Text(z, "RepositoryElementUUID")) == null ? null : Text(z, "RepositoryElementUUID");
                }
                if (vorhanden.Contains("BmElementWindow"))
                    foreach (Dictionary<string, object> z in Zeilen(c, SQL_FENSTER))
                    {
                        if (Text(z, "UUID") is not string u || !flaechen.TryGetValue(u, out SqprojHuellflaeche h)) continue;
                        double? g = SqprojBauteilcodes.Gesetzt(Zahl(z, "GValue"));
                        h.GWert = g > 0.0 && g <= 1.0 ? g : null;
                        double? f = SqprojBauteilcodes.Gesetzt(Zahl(z, "FractionOfFrame"));
                        if (f > 1.0) f /= 100.0;
                        h.Rahmenanteil = f >= 0.0 && f < 1.0 ? f : null;
                    }
                a.LageGelesen = true;
            }
            catch (Exception ex) when (ex is SqliteException || ex is InvalidOperationException || ex is FormatException)
            {
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.LAGE_UNLESBAR, ex.Message));
            }
        }

        /// <summary>Liest die Standorte (<c>SmSite</c>, optional); eine unlesbare Tabelle bleibt still leer — sie ist nur Anzeige.</summary>
        private static void StandorteLesen(SqliteConnection c, SqprojAbbild a, HashSet<string> vorhanden)
        {
            if (!vorhanden.Contains("SmSite")) return;
            try
            {
                foreach (Dictionary<string, object> z in Zeilen(c, SQL_STANDORT))
                    if (Text(z, "UUID") is string u && !a.Standorte.ContainsKey(u))
                    {
                        double? breite = SqprojBauteilcodes.Gesetzt(Zahl(z, "Latitude")), laenge = SqprojBauteilcodes.Gesetzt(Zahl(z, "Longitude"));
                        a.Standorte[u] = new SqprojStandort
                        {
                            Uuid = u, Ort = Text(z, "Location"), Plz = Text(z, "PostalCode"),
                            BreiteGrad = breite is double b && b >= -90.0 && b <= 90.0 && b != 0.0 ? b : null,
                            LaengeGrad = laenge is double l && l >= -180.0 && l <= 180.0 && l != 0.0 ? l : null,
                        };
                    }
            }
            catch (Exception ex) when (ex is SqliteException || ex is InvalidOperationException || ex is FormatException)
            {
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.LAGE_UNLESBAR, ex.Message));
            }
        }

        /// <summary>
        /// <b>Das Baujahr eines Gebäudes</b>: die Jahreszahl aus <c>YearOfConstruction</c> (Text „JJJJ-MM-TT …“ oder
        /// Delphi-Tageszahl; die Nullzeit heißt „keine Angabe“), sonst <c>Constructed</c>, wenn es eine Jahreszahl ist.
        /// </summary>
        internal static int? Baujahr(Dictionary<string, object> z)
        {
            static int? Jahr(int? j) => j >= 1000 && j <= 2200 ? j : null;
            object w = Wert(z, "YearOfConstruction");
            if (w is string s && !string.IsNullOrWhiteSpace(s))
            {
                if (!s.TrimStart().StartsWith(NULLZEIT, StringComparison.Ordinal) && Jahr(Baujahrregel.Jahr(s)) is int j) return j;
            }
            else if (Zahl(z, "YearOfConstruction") is double tage && tage > 1.0)
            {
                int jahr = new DateTime(1899, 12, 30).AddDays(Math.Min(tage, 100000.0)).Year;
                if (Jahr(jahr) is int j) return j;
            }
            return Jahr(Ganz(z, "Constructed"));
        }

        private static SqprojNutzungsprofil Nutzungsprofil(Dictionary<string, object> kopf, Dictionary<string, object> n)
            => new SqprojNutzungsprofil
            {
                Uuid = Text(kopf, "UUID") ?? "",
                Name = Name(kopf),
                Profilnummer = Ganz(n, "ProfileUsageType"),
                BetriebVon = Stunde(n, "PeriodOfOperationFrom"),
                BetriebBis = Stunde(n, "PeriodOfOperationTo"),
                HeizVon = Stunde(n, "HeatedFrom"),
                HeizBis = Stunde(n, "HeatedTo"),
                Personenzahl = Positiv(Zahl(n, "UserCount")),
                Raumtemperatur = Positiv(Zahl(n, "NominalRoomTemperature")),
                Absenkung = Positiv(Zahl(n, "DropOfTemperatureSetback")),
                Zuluftwechsel = Positiv(Zahl(n, "SupplyAirChange")),
                AussenluftJePerson = Positiv(Zahl(n, "MinimumExternalAirFlowBasedOnPersons")),
                AussenluftJeFlaeche = Positiv(Zahl(n, "MinimumExternalAirFlowBasedOnArea")),
                VollnutzungPersonenH = Positiv(Zahl(n, "DailyEffectiveLoadHoursOfPersons")),
                VollnutzungGeraeteH = Positiv(Zahl(n, "DailyEffectiveLoadHoursOfDevices")),
                PersonenWm2 = Positiv(Zahl(n, "SpecificThermalOutputPowerOfPersons")),
                GeraeteWm2 = Positiv(Zahl(n, "SpecificThermalOutputOfDevices")),
                Beleuchtungsstaerke = Positiv(Zahl(n, "MaintenanceIllumination")),
            };

        private static SqprojAbschnitt Abschnitt(Dictionary<string, object> s)
        {
            string[] tage = { "TaskMonday", "TaskTuesday", "TaskWednesday", "TaskThursday", "TaskFriday", "TaskSaturday", "TaskSunday" };
            int beginn = Ganz(s, "TaskStartDay") ?? 1, ende = Ganz(s, "TaskEndDay") ?? 365;
            return new SqprojAbschnitt
            {
                Uuid = Text(s, "UUID") ?? "",
                Abschnittsart = Ganz(s, "TaskPeriodType"),
                Beginn = Math.Clamp(beginn, Kalenderregel.TAG_MIN, Kalenderregel.TAG_MAX),
                Ende = Math.Clamp(ende, Kalenderregel.TAG_MIN, Kalenderregel.TAG_MAX),
                Wochentage = tage.Select(t => (Ganz(s, t) ?? 0) != 0).ToArray(),
            };
        }

        // ------------------------------------------------------------------
        //  Zeilen und Werte
        // ------------------------------------------------------------------

        private static List<Dictionary<string, object>> Zeilen(SqliteConnection c, string sql)
        {
            var l = new List<Dictionary<string, object>>();
            using (SqliteCommand k = c.CreateCommand())
            {
                k.CommandText = sql;
                using (SqliteDataReader r = k.ExecuteReader())
                    while (r.Read())
                    {
                        var z = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < r.FieldCount; i++)
                            z[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                        l.Add(z);
                    }
            }
            return l;
        }

        private static Dictionary<string, Dictionary<string, object>> JeKennung(IEnumerable<Dictionary<string, object>> zeilen)
        {
            var d = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in zeilen)
                if (Text(z, "UUID") is string u) d.TryAdd(u, z);
            return d;
        }

        private static object Wert(Dictionary<string, object> z, string spalte)
            => z != null && z.TryGetValue(spalte, out object w) ? w : null;

        internal static string Text(Dictionary<string, object> z, string spalte)
        {
            object w = Wert(z, spalte);
            string s = w == null ? null : Convert.ToString(w, CultureInfo.InvariantCulture)?.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static string Name(Dictionary<string, object> z) => Text(z, "ShortDesc") ?? Text(z, "LongDesc") ?? Text(z, "UUID") ?? "";

        internal static double? Zahl(Dictionary<string, object> z, string spalte)
        {
            object w = Wert(z, spalte);
            switch (w)
            {
                case null: return null;
                case double d: return double.IsFinite(d) ? d : null;
                case long l: return l;
                case string s:
                    return double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double x) && double.IsFinite(x) ? x : null;
                default:
                    try { double y = Convert.ToDouble(w, CultureInfo.InvariantCulture); return double.IsFinite(y) ? y : null; }
                    catch (FormatException) { return null; }
                    catch (InvalidCastException) { return null; }
            }
        }

        private static int? Ganz(Dictionary<string, object> z, string spalte)
            => Zahl(z, spalte) is double d ? (int)Math.Round(d, MidpointRounding.AwayFromZero) : (int?)null;

        private static double? Positiv(double? w) => w > 0.0 ? w : null;

        /// <summary>
        /// <b>Die Stunde einer <c>DATE</c>-Spalte</b> (Text „JJJJ-MM-TT hh:mm:ss“ oder Delphi-Tageszahl): 0 … 23, auf die
        /// volle Stunde gerundet; die Nullzeit <c>1899-12-30 00:00:00</c> heißt „keine Angabe“ (<c>null</c>).
        /// </summary>
        internal static int? Stunde(Dictionary<string, object> z, string spalte)
        {
            object w = Wert(z, spalte);
            if (w is string s)
            {
                s = s.Trim();
                if (s.Length == 0) return null;
                if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t)) return null;
                if (t.Date == new DateTime(1899, 12, 30) && t.TimeOfDay == TimeSpan.Zero) return null;
                return (int)Math.Round(t.TimeOfDay.TotalHours, MidpointRounding.AwayFromZero) % 24;
            }
            if (Zahl(z, spalte) is double d)
            {
                if (d == 0.0) return null;
                double bruch = d - Math.Floor(d);
                return (int)Math.Round(bruch * 24.0, MidpointRounding.AwayFromZero) % 24;
            }
            return null;
        }
    }
}
