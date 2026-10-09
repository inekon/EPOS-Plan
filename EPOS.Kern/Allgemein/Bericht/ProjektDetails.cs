using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lesend geladene Detail-Daten eines Projekts für Projektbeschreibung,
    /// Komponententabellen und Abweichungserkennung (Phase 2).
    /// Zugriff bewusst über tolerante SQL-Reads (Spalten je DB-Stand prüfbar),
    /// nicht über die Formular-Controller — Spaltennamen sind gegen das Schema
    /// von Kenndaten.accdb verifiziert (11.08.2026).
    ///
    /// <para>
    /// DER KOMPONENTENBESTAND KOMMT AUS <c>Tab_Energieanlagen</c>, nicht aus
    /// <c>Tab_WP &amp; Co. WHERE ID_Projekt = ?</c> — siehe
    /// <see cref="LadeGewerk"/>.
    /// </para>
    /// </summary>
    public class ProjektDetails
    {
        /// <summary>Gewerk-Schlüssel → Eingabetabelle des Projekts.</summary>
        public static readonly KeyValuePair<string, string>[] GewerkTabellen = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("Wärmepumpe",    "Tab_WP"),
            new KeyValuePair<string, string>("BHKW",          "Tab_BHKW"),
            new KeyValuePair<string, string>("Spitzenkessel", "Tab_Heizkessel"),
            new KeyValuePair<string, string>("Solarthermie",  "Tab_Solarkollektoren"),
            new KeyValuePair<string, string>("Photovoltaik",  "Tab_PV"),
            new KeyValuePair<string, string>("Pufferspeicher","Tab_Pufferspeicher"),
            new KeyValuePair<string, string>("Stromspeicher", "Tab_Stromspeicher"),
        };

        public int IdProjekt;

        /// <summary>Bezeichner der Klimaregion des Projekts (Tab_Klimaregion, leer wenn keiner).</summary>
        public string KlimaregionName = "";

        /// <summary>Gebäude des Projekts (Tab_Gebaeude; null/leer möglich).</summary>
        public DataTable Gebaeude;

        /// <summary>Anlagenkonfiguration (Tab_Energieanlagen; null/leer möglich).</summary>
        public DataTable Anlagen;

        /// <summary>
        /// Die Projekteinstellungen (Tab_Einstellungen; null/leer möglich) — für die Kopplungsstufe
        /// im Variantenvergleich (Anlagenkopplung AK1, Konzept 9.4).
        /// </summary>
        public DataTable Einstellungen;

        /// <summary>
        /// Das Temperaturniveau des Prozesskanals (PW1 Stufe 1): höchster Vorlauf und tiefster
        /// Rücklauf der zugeordneten Prozesswärmesätze mit Temperaturpaar [°C]; <c>null</c>, wenn
        /// keiner ein Paar trägt (dann führt der Bericht keine Zeile).
        /// </summary>
        public double? ProzessVorlaufMax;

        /// <summary>Tiefster Rücklauf der Prozesswärmesätze mit Temperaturpaar [°C]; siehe <see cref="ProzessVorlaufMax"/>.</summary>
        public double? ProzessRuecklaufMin;

        /// <summary>Zahl der zugeordneten Prozesswärmesätze mit Temperaturpaar.</summary>
        public int ProzessMitTemperatur;

        /// <summary>
        /// Netzverluste je Kanal und Zirkulation im Bestandsweg (Entscheidungsvorlage BW4); leer, wenn
        /// das Projekt keine führt (dann führt der Bericht keine Zeile).
        /// </summary>
        public Netzverlustvorgabe Netzkanaele = Netzverlustvorgabe.Leer;

        /// <summary>Die thermische Desinfektion (BW5); „aus", wenn das Projekt keine führt (dann keine Zeile).</summary>
        public Desinfektionsvorgabe Desinfektion = Desinfektionsvorgabe.Aus;

        /// <summary>Jahresmenge der Desinfektion [MWh] nach den Regeln des Laufs; 0 ohne.</summary>
        public double DesinfektionMwh;

        /// <summary>Aufgeheiztes Volumen der Desinfektion [l] (gepflegt oder das der Brauchwasserspeicher).</summary>
        public double DesinfektionVolumenL;

        /// <summary>Rechnet das Projekt sein Brauchwasser über den Zapfprofilgenerator? (Dann gilt dessen Zirkulation.)</summary>
        public bool Zapfprofilweg;

        /// <summary>
        /// Der Ausweis der PV-Ganglinie, wenn das Projekt seine Photovoltaik über eine Ganglinie rechnet
        /// (PVG); <c>null</c> im Modulmodell. Die Kenndaten der Photovoltaik nennen dann die Quelle statt
        /// der Modulangaben.
        /// </summary>
        public PvGanglinieAusweis PvGanglinie;

        /// <summary>Gewerk → erste Komponentenzeile des Projekts (fehlt das Gewerk: kein Eintrag).</summary>
        public Dictionary<string, DataRow> Komponenten = new Dictionary<string, DataRow>();

        /// <summary>Gewerk → ALLE Komponentenzeilen des Projekts in Anlagenreihenfolge
        /// (Nutzerauftrag 28.08.2026: die Gegenüberstellung zeigt eine Zeile je
        /// Komponente, nicht mehr nur die Merkmale der ersten).</summary>
        public Dictionary<string, DataTable> KomponentenAlle = new Dictionary<string, DataTable>();

        /// <summary>Gewerk → Anzahl der Komponenten-Einträge des Projekts.</summary>
        public Dictionary<string, int> KomponentenAnzahl = new Dictionary<string, int>();

        public bool HatGewerk(string gewerk)
        { return KomponentenAnzahl.ContainsKey(gewerk) && KomponentenAnzahl[gewerk] > 0; }

        // ------------------------------------------------------------- Zonen (Stufe G6a)

        /// <summary>
        /// Der Name der gedachten Tabelle der Zonenmerkmale — die „Tabelle" der Merkmale des
        /// Variantenvergleichs (<see cref="AbweichungsErmittler.Felder"/>), die keine Spalte einer
        /// Eingabetabelle sind, sondern aus den Zonen gebildet werden (<see cref="Zonenmerkmale"/>).
        /// </summary>
        public const string ZONENMERKMALE = "Zonen";

        /// <summary>Der Steuerwert des Rechenwegs der Hülle ohne Zonen (Klassenweg).</summary>
        public const string HUELLE_KLASSENWEG = "KLASSENWEG";

        /// <summary>Der Steuerwert des Rechenwegs der Hülle, wenn jedes Gebäude Zonen trägt (Bauteilweg).</summary>
        public const string HUELLE_BAUTEILWEG = "BAUTEILWEG";

        /// <summary>
        /// Die Zonen je Projektgebäude (<c>Tab_Gebaeude.ID</c> → Zonen in Rangfolge samt Bauteilen);
        /// ein Gebäude ohne Zone fehlt. Leer, wenn die Datenbank <c>Tab_Zone</c> noch nicht kennt.
        /// </summary>
        public Dictionary<int, List<ZoneModel>> Zonen = new Dictionary<int, List<ZoneModel>>();

        /// <summary>Der U-Wert je Projektaufbau (<c>Tab_Bauteilaufbau.ID</c>) [W/(m²K)]; <c>null</c> = nicht bestimmbar.</summary>
        public Dictionary<int, double?> AufbauU = new Dictionary<int, double?>();

        /// <summary>
        /// Die Zonenmerkmale des Projekts für den Variantenvergleich (eine Zeile; Spalten
        /// <c>Zonenzahl</c>, <c>Zonenflaeche</c>, <c>Zonen_HT</c>, <c>Huellrechenweg</c>) — über alle
        /// Gebäude des Projekts; <c>null</c> ohne geladene Gebäude.
        /// </summary>
        public DataTable Zonenmerkmale;

        /// <summary>
        /// Der Konditionierungsstand je Projektgebäude (<c>Tab_Gebaeude.ID</c>; Entwurf KP3, Welle O3a, B19) — nur für ein
        /// Gebäude, das Vorgabezeilen oder Kalender trägt; die Kurzform je Größe bildet daraus <see cref="Aufheizbericht.Konditionierung"/>.
        /// Leer, wenn die Datenbank die Konditionierungstabellen noch nicht kennt.
        /// </summary>
        public Dictionary<int, Konditionierungsstand> Konditionierung = new Dictionary<int, Konditionierungsstand>();

        /// <summary>Die Zonen eines Projektgebäudes (<c>Tab_Gebaeude.ID</c>); leer = keine.</summary>
        public List<ZoneModel> ZonenVon(int idGebaeude)
            => Zonen != null && Zonen.TryGetValue(idGebaeude, out List<ZoneModel> z) ? z : new List<ZoneModel>();

        /// <summary>
        /// Die Kennwerte einer Zone aus der EINEN Formel (<see cref="Zonenkennwerte"/>) mit den Werten
        /// ihres Gebäudes (<paramref name="gebaeude"/>, eine Zeile aus <see cref="Gebaeude"/>): Nutzfläche
        /// (<c>Nutzflaeche</c>, sonst <c>Wohnflaeche_gesamt</c>), Raumhöhe und Luftwechsel des Rechenwegs;
        /// U eines Bauteils ohne eigenen Wert aus seinem Aufbau (<see cref="AufbauU"/>).
        /// </summary>
        public Zonenkennwerte Kennwerte(ZoneModel zone, DataRow gebaeude)
        {
            string modell = S(gebaeude, "Gebaeude_Modell");
            return Zonenkennwerte.Bilden(zone, id => AufbauU.TryGetValue(id, out double? u) ? u : null,
                                         D(gebaeude, "Nutzflaeche") ?? D(gebaeude, "Wohnflaeche_gesamt"), D(gebaeude, "Raumhoehe"),
                                         Zonenkennwerte.Luftwechsel(modell.Length == 0 ? null : modell, D(gebaeude, "Luftwechselrate"),
                                                                    D(gebaeude, "Luftwechsel_Infiltration"), D(gebaeude, "Luftwechsel_Nutzer")));
        }

        /// <summary>
        /// Liest die Zonen des Projekts EINMAL (<see cref="GebaeudeZonenCtrl.LesenJeProjekt"/>, zwei
        /// Abfragen) samt den U-Werten der Projektaufbauten und bildet die Zonenmerkmale. Fehlt
        /// <c>Tab_Zone</c> in einer alten Datenbank, bleibt es still bei „keine Zonen".
        /// </summary>
        private static void LadeZonen(ProjektDetails d)
        {
            try
            {
                object t = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", ZonenSchema.TAB_ZONE));
                if (t != null && t != DBNull.Value && Convert.ToInt64(t) > 0)
                {
                    d.Zonen = new GebaeudeZonenCtrl().LesenJeProjekt(d.IdProjekt);
                    if (d.Zonen.Count > 0)
                        foreach (BauteilaufbauModel a in new BauteilaufbauCtrl().LesenJeProjekt(d.IdProjekt))
                        {
                            if (a == null) continue;
                            double? u = null;
                            try { u = BauteilaufbauCtrl.Kennwerte(a, mitBezugsperiode: false).U_WM2K; } catch { u = null; }
                            d.AufbauU[a.ID] = u;
                        }
                }
            }
            catch
            {
                d.Zonen = new Dictionary<int, List<ZoneModel>>();
                d.AufbauU = new Dictionary<int, double?>();
            }
            d.Zonenmerkmale = BildeZonenmerkmale(d);
        }

        /// <summary>
        /// Liest je Gebäude des Projekts den Konditionierungsstand der Gebäudeebene (<see cref="KonditionierungCtrl.StandLesen"/>)
        /// und legt ihn ab, wenn das Gebäude Vorgabezeilen oder Kalender trägt. Ein Fehler beim Lesen lässt das Gebäude ohne
        /// Kurzform — der Bericht entsteht trotzdem.
        /// </summary>
        private static void LadeKonditionierung(ProjektDetails d)
        {
            if (d?.Gebaeude == null) return;
            var ctrl = new KonditionierungCtrl();
            foreach (DataRow g in d.Gebaeude.Rows)
            {
                int id = (int)(D(g, "ID") ?? 0);
                if (id <= 0) continue;
                try
                {
                    Konditionierungsstand stand = ctrl.StandLesen(KonditionierungCtrl.Eigner.Gebaeude(id), out _);
                    if (stand != null && !stand.TabellenLeer) d.Konditionierung[id] = stand;
                }
                catch { /* ohne Kurzform */ }
            }
        }

        /// <summary>
        /// Die eine Zeile der Zonenmerkmale über alle Gebäude des Projekts: Zahl der Zonen, Σ Nutzfläche
        /// und Σ H_T der Zonen (NULL ohne Zonen) und der Rechenweg der Hülle — Klassenweg ohne Zonen,
        /// Bauteilweg, wenn jedes Gebäude Zonen trägt, sonst „TEIL:n/m" (n von m Gebäuden).
        /// </summary>
        internal static DataTable BildeZonenmerkmale(ProjektDetails d)
        {
            if (d == null || d.Gebaeude == null) return null;
            var dt = new DataTable();
            dt.Columns.Add("Zonenzahl", typeof(long));
            dt.Columns.Add("Zonenflaeche", typeof(double));
            dt.Columns.Add("Zonen_HT", typeof(double));
            dt.Columns.Add("Huellrechenweg", typeof(string));

            long zahl = 0;
            int mitZonen = 0;
            double flaeche = 0.0, ht = 0.0;
            bool flaecheBekannt = true;
            foreach (DataRow g in d.Gebaeude.Rows)
            {
                List<ZoneModel> zonen = d.ZonenVon((int)(D(g, "ID") ?? 0));
                if (zonen.Count == 0) continue;
                mitZonen++;
                foreach (ZoneModel z in zonen)
                {
                    Zonenkennwerte k = d.Kennwerte(z, g);
                    zahl++;
                    ht += k.HT;
                    if (k.Nutzflaeche is double a) flaeche += a; else flaecheBekannt = false;
                }
            }
            int alle = d.Gebaeude.Rows.Count;
            string weg = mitZonen == 0 ? HUELLE_KLASSENWEG
                : mitZonen == alle ? HUELLE_BAUTEILWEG
                : "TEIL:" + mitZonen.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/"
                          + alle.ToString(System.Globalization.CultureInfo.InvariantCulture);
            dt.Rows.Add(zahl, zahl > 0 && flaecheBekannt ? flaeche : (object)DBNull.Value,
                        zahl > 0 ? ht : (object)DBNull.Value, weg);
            return dt;
        }

        /// <summary>Der Anzeigename des Rechenwegs der Hülle (Steuerwert aus <see cref="BildeZonenmerkmale"/>).</summary>
        public static string Huellrechenwegtext(string steuerwert)
        {
            if (string.IsNullOrEmpty(steuerwert) || steuerwert == HUELLE_KLASSENWEG) return MyResource.Resource.ABW_WERT_KLASSENWEG;
            if (steuerwert == HUELLE_BAUTEILWEG) return MyResource.Resource.ABW_WERT_BAUTEILWEG;
            string[] teil = steuerwert.StartsWith("TEIL:", StringComparison.Ordinal) ? steuerwert.Substring(5).Split('/') : null;
            return teil != null && teil.Length == 2
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.ABW_WERT_BAUTEILWEG_TEIL, teil[0], teil[1])
                : steuerwert;
        }

        public static ProjektDetails Lade(int idProjekt)
        {
            var d = new ProjektDetails { IdProjekt = idProjekt };

            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Bezeichner FROM Tab_Klimaregion WHERE ID_Projekt = ? LIMIT 1",
                    new DbParam("@p", idProjekt));
                d.KlimaregionName = o as string ?? "";
            }
            catch { }

            d.Gebaeude = LadeTabelle("Tab_Gebaeude", idProjekt);
            d.Anlagen = LadeTabelle("Tab_Energieanlagen", idProjekt);
            d.Einstellungen = LadeTabelle("Tab_Einstellungen", idProjekt);

            foreach (KeyValuePair<string, string> g in GewerkTabellen)
            {
                DataTable dt = LadeGewerk(g.Key, g.Value, idProjekt);
                int anzahl = dt != null ? dt.Rows.Count : 0;
                d.KomponentenAnzahl[g.Key] = anzahl;
                if (anzahl > 0) { d.Komponenten[g.Key] = dt.Rows[0]; d.KomponentenAlle[g.Key] = dt; }
            }

            // PVG: rechnet die Photovoltaik ueber eine PV-Ganglinie, nennt der Bericht deren Quelle.
            try { d.PvGanglinie = PvGanglinieAusweis.Aus(PvGanglinieWeiche.Lesen(idProjekt)); }
            catch { d.PvGanglinie = null; }

            // Stufe G6a: die Zonen EINMAL je Projekt, samt Aufbauten und Zonenmerkmalen.
            LadeZonen(d);

            // PW1 Stufe 1: das Temperaturniveau des Prozesskanals.
            LadeProzesstemperatur(d);

            // KP3 Welle O3a (B19): der Konditionierungsstand je Gebaeude fuer die Kurzform im Bericht.
            LadeKonditionierung(d);

            // BW4: Netzverluste je Kanal und Zirkulation im Bestandsweg.
            d.Netzkanaele = KonfigurationCtrl.NetzverlustvorgabeLesen(d.IdProjekt);
            d.Zapfprofilweg = ZapfprofilCtrl.Weg(d.IdProjekt) == BrauchwasserWeg.Generator;

            // BW5: die thermische Desinfektion samt Jahresmenge (dieselben Regeln wie im Lauf).
            d.Desinfektion = KonfigurationCtrl.DesinfektionLesen(d.IdProjekt);
            if (d.Desinfektion.Aktiv)
            {
                double kwh = WindowsFormsApplication1.Desinfektion.JahresmengeKwh(d.IdProjekt, d.Desinfektion, out double volumen, out _);
                d.DesinfektionMwh = Energieeinheit.MWh.AusKWh(kwh);
                d.DesinfektionVolumenL = volumen;
            }
            return d;
        }

        /// <summary>
        /// Die Komponenten EINES GEWERKS im Projekt — je Anlagenzeile eine
        /// Gerätezeile, in der Reihenfolge der Anlagenzeilen
        /// (<see cref="KomponentenUebernahmeCtrl.GeraeteJeAnlagenzeile"/>).
        ///
        /// <para>
        /// NICHT über <c>WHERE ID_Projekt = ?</c> auf der Gerätetabelle: Die
        /// Gerätetabellen führen Projektkopien eines Katalogsatzes, und jeder
        /// Speichervorgang legt über <c>CopyFromStamm</c> eine NEUE Kopie an, während
        /// <c>WErzeugerCtrl.Delete</c> nur die Anlagenzeilen räumt. In gewachsenen
        /// Projekten — und erst recht in Varianten, die diese Historie mitkopiert
        /// haben — steht dort Altbestand, auf den keine Anlage mehr zeigt. Gezählt
        /// wurde er trotzdem: die Unterschiedstabelle der Seite „Übersicht“ meldete
        /// Dutzende Komponenten und Gewerke als „vorhanden“, die das Projekt gar nicht
        /// führt. Verbaut ist ausschließlich, was <c>Tab_Energieanlagen</c> führt —
        /// dieselbe Liste, die die Verwaltungsdialoge unter „ausgewählt im Projekt“
        /// zeigen und die die Simulation rechnet.
        /// </para>
        /// </summary>
        private static DataTable LadeGewerk(string gewerk, string tabelle, int idProjekt)
        {
            KomponentenUebernahmeCtrl.GewerkPlan plan;
            if (KomponentenUebernahmeCtrl.Plaene.TryGetValue(gewerk ?? "", out plan))
                return KomponentenUebernahmeCtrl.GeraeteJeAnlagenzeile(plan, idProjekt);

            return LadeTabelle(tabelle, idProjekt);   // Gewerk ohne Plan: wie bisher
        }

        /// <summary>
        /// Liest das Temperaturniveau der zugeordneten Prozesswärmesätze (PW1 Stufe 1) — über die
        /// Zuordnungszeilen, mit denen der Lauf rechnet. Still: Vor dem Schemaschritt fehlen die
        /// Spalten, dann bleibt es bei „ohne".
        /// </summary>
        private static void LadeProzesstemperatur(ProjektDetails d)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT MAX(p.Vorlauf) AS VL, MIN(p.Ruecklauf) AS RL, COUNT(*) AS N FROM Tab_Prozesswaerme p " +
                "INNER JOIN Z_Projekt_Prozesswaerme z ON z.ID_Prozesswaerme = p.ID " +
                "WHERE z.ID_Projekt = ? AND p.ID_Projekt = ? AND p.Vorlauf IS NOT NULL AND p.Ruecklauf IS NOT NULL",
                StilleDb.Par("@p", DbParamTyp.Integer, d.IdProjekt),
                StilleDb.Par("@p2", DbParamTyp.Integer, d.IdProjekt));
            if (dt == null || dt.Rows.Count == 0) return;
            int n = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "N"));
            if (n <= 0) return;
            d.ProzessMitTemperatur = n;
            d.ProzessVorlaufMax = D(dt.Rows[0], "VL");
            d.ProzessRuecklaufMin = D(dt.Rows[0], "RL");
        }

        private static DataTable LadeTabelle(string tabelle, int idProjekt)
        {
            try
            {
                return DataRepository.GetDataTable(
                    "SELECT * FROM " + tabelle + " WHERE ID_Projekt = ? ORDER BY ID",
                    new DbParam("@p", idProjekt));
            }
            catch { return null; }
        }

        // ------------------------------------------------------------- tolerante Zugriffe

        public static string S(DataRow r, string col)
        { return (r != null && r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? r[col].ToString() : ""; }

        public static double? D(DataRow r, string col)
        {
            if (r == null || !r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
        }

        public static bool? B(DataRow r, string col)
        {
            if (r == null || !r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToBoolean(r[col]); } catch { return null; }
        }
    }
}
