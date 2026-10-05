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
            a.Gebaeudename = Zeilen(c, SQL_GEBAEUDE).Select(z => Name(z)).OrderBy(n => n, StringComparer.Ordinal).FirstOrDefault();
            var geschossName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> z in Zeilen(c, SQL_GESCHOSS))
                if (Text(z, "UUID") is string u && !geschossName.ContainsKey(u)) geschossName[u] = Name(z);
            a.Geschosse.AddRange(geschossName.Select(p => new SqprojGeschoss(p.Key, p.Value))
                                             .OrderBy(g => g.Name, StringComparer.Ordinal).ThenBy(g => g.Uuid, StringComparer.Ordinal));
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
                    Bimuuid = Text(z, "BIMUUID"),
                    FlaecheM2 = Positiv(Zahl(z, "Area")),
                    VolumenM3 = Positiv(Zahl(z, "Volume")),
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
            foreach (SqprojZone zone in zonen)
            {
                if (!gruppeJeZone.TryGetValue(zone.Uuid, out string gu) || !gruppen.TryGetValue(gu, out Dictionary<string, object> gz)) continue;
                if (!gruppeGelesen.TryGetValue(gu, out SqprojProfilgruppe gruppe))
                {
                    int? code = Ganz(gz, "ProfileUsageDayType");
                    gruppe = new SqprojProfilgruppe { Uuid = gu, Name = Name(gz), TagesartCode = code, Tagesart = SqprojProtokoll.Tagesart(code) };
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
                        int ba = zp.Betriebsart ?? -1;
                        a.Betriebsarten[ba] = a.Betriebsarten.TryGetValue(ba, out int n) ? n + 1 : 1;
                    }
                    gruppeGelesen[gu] = gruppe;
                }
                zone.Gruppe = gruppe;
                if (gruppe.Tagesart == SqprojTagesart.Unbekannt && tagesartGemeldet.Add(gu))
                    a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.TAGESART_UNBEKANNT, gruppe.Name,
                        gruppe.TagesartCode.HasValue ? SqprojProtokoll.Z(gruppe.TagesartCode.Value) : "—"));
            }
            foreach (KeyValuePair<int, int> p in a.Betriebsarten)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.BETRIEBSART,
                    p.Key < 0 ? "—" : SqprojProtokoll.Z(p.Key), SqprojProtokoll.Z(p.Value)));
            if (abschnittsartUnbekannt > 0)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.ABSCHNITTSART_UNBEKANNT, SqprojProtokoll.Z(abschnittsartUnbekannt)));
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
