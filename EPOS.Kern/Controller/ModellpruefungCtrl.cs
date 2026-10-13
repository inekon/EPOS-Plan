using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Modellprüfung eines Gebäudes</b> (Entwurf Geführter Modellaufbau 2.2–2.4, Welle MA-1a, E137): liest den
    /// <see cref="Modellpruefstand"/> eines Projektgebäudes (Einzonen- oder Mehrzonenmodell), prüft ihn über
    /// <see cref="Modellpruefung"/> in fünf Gruppen und führt die Vorschläge je Gruppe (<see cref="Uebernehmen"/>) oder alle
    /// (<see cref="AlleUebernehmen"/>) über die vorhandenen Wege aus — Aufbau des Projekts, Katalogkopie
    /// (<see cref="BauteilaufbauCtrl.CopyFromStamm"/>), Typaufbau (<see cref="Ersatzaufbau"/>,
    /// <see cref="BauteilaufbauCtrl.ProjektaufbauEinfuegen"/>), Profil über den Generator
    /// (<see cref="ZonenplanCtrl.NutzungUebernehmen"/>), Ferien und Saison eines anderen Gebäudes des Projekts, Feiertagsland.
    ///
    /// <para><b>Rücknahme</b> (E137 F2): Die Instanz merkt sich je Gruppe die letzte Übernahme (Sitzungsgedächtnis, keine
    /// Tabelle) und stellt sie mit <see cref="Zuruecknehmen"/> zurück: alte Werte an Bauteil, Zone und Gebäude, angelegte
    /// Aufbauten, Kalender, Wochen und Vorgabezeilen fallen. In den Projektstoffen bleiben die Kopien der Katalogstoffe
    /// (ungenutzt, wie nach jeder Katalogkopie).</para>
    ///
    /// <para>Nur über <see cref="DataRepository"/> mit <c>?</c>-Parametern; jede Übernahme läuft in EINEM Vorgang samt
    /// <see cref="Vorgangsklammer"/>. Kein Schemaschritt, keine Leseregel des Rechenwegs.</para>
    /// </summary>
    public sealed class ModellpruefungCtrl
    {
        private readonly Dictionary<Pruefgruppe, List<Action<DbVorgang>>> _ruecknahme = new Dictionary<Pruefgruppe, List<Action<DbVorgang>>>();
        private readonly Dictionary<int, Zonenquelle> _quellen = new Dictionary<int, Zonenquelle>();

        /// <summary>Die Prüfung des Projektgebäudes <paramref name="idGebaeude"/> (<c>Tab_Gebaeude.ID</c>).</summary>
        public ModellpruefungCtrl(int idGebaeude) { IdGebaeude = idGebaeude; }

        /// <summary>Das geprüfte Gebäude.</summary>
        public int IdGebaeude { get; }

        /// <summary>
        /// Die Hinweise der Quelle zu einer Zone (DIN-Nummer, Raumtyp, Nutzungsklasse) — Stufen 1 und 2 der Profilregel; der
        /// Importdialog kennt sie, die Datenbank nicht.
        /// </summary>
        public void QuelleSetzen(int idZone, int? dinNummer, string raumtyp, string nutzungsklasse)
            => _quellen[idZone] = new Zonenquelle(dinNummer, raumtyp, nutzungsklasse);

        /// <summary>Der Anzeigename einer Prüfgruppe (Ressource <c>MP_GRUPPE_*</c>).</summary>
        public static string Gruppenname(Pruefgruppe g) => Modellpruefung.Ressource("MP_GRUPPE_" + g.ToString().ToUpperInvariant());

        /// <summary>Der Anzeigename eines Rechenstatus (Ressource <c>MP_STATUS_*</c>).</summary>
        public static string Statusname(Rechenstatus s) => Modellpruefung.Ressource("MP_STATUS_" + s.ToString().ToUpperInvariant());

        /// <summary>Kann die letzte Übernahme der Gruppe zurückgenommen werden?</summary>
        public bool KannZuruecknehmen(Pruefgruppe gruppe) => _ruecknahme.ContainsKey(gruppe);

        /// <summary><b>Prüft</b> das Gebäude; <c>null</c>, wenn es es nicht gibt.</summary>
        public Modellpruefergebnis Pruefen()
        {
            Modellpruefstand s = Lesen();
            return s == null ? null : Modellpruefung.Pruefen(s);
        }

        // =====================================================================
        //  Übernahme und Rücknahme
        // =====================================================================

        /// <summary><b>Übernimmt die Vorschläge einer Gruppe</b> in einem Vorgang und prüft danach neu.</summary>
        public Uebernahmeergebnis Uebernehmen(Pruefgruppe gruppe) => Ausfuehren(new[] { gruppe });

        /// <summary><b>Übernimmt alle Vorschläge</b> — Gruppe für Gruppe, je mit eigener Rücknahme — und prüft danach neu.</summary>
        public Uebernahmeergebnis AlleUebernehmen()
            => Ausfuehren(Enum.GetValues(typeof(Pruefgruppe)).Cast<Pruefgruppe>().OrderBy(g => (int)g).ToArray());

        private Uebernahmeergebnis Ausfuehren(Pruefgruppe[] gruppen)
        {
            var verworfen = new List<string>();
            var erledigt = new List<Pruefgruppe>();
            int zahl = 0;
            foreach (Pruefgruppe g in gruppen)
            {
                Modellpruefstand s = Lesen();
                if (s == null) return new Uebernahmeergebnis(false, Modellpruefung.Ressource("MP_MSG_GEBAEUDE_FEHLT", IdGebaeude),
                                                             erledigt, zahl, verworfen, false, null);
                List<Modellbefund> befunde = Modellpruefung.Pruefen(s).Gruppe(g).Befunde.ToList();
                foreach (Modellbefund b in befunde.Where(b => b.Vorschlag == null && b.Ampel != Pruefampel.Gruen))
                    verworfen.Add(Modellpruefung.Ressource("MP_MSG_OHNE_VORSCHLAG", b.Text));
                List<Modellbefund> mit = befunde.Where(b => b.Vorschlag != null).ToList();
                if (mit.Count == 0) continue;
                var rueck = new List<Action<DbVorgang>>();
                int n = 0;
                try
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                    {
                        var typcache = new Dictionary<string, int>(StringComparer.Ordinal);
                        foreach (Modellbefund b in mit)
                        {
                            string grund = Anwenden(v, s, b, rueck, typcache);
                            if (grund == null) n++;
                            else verworfen.Add(Modellpruefung.Ressource("MP_MSG_VERWORFEN", b.Text, grund));
                        }
                        v.Commit();
                    }
                }
                catch (Exception ex)
                {
                    return new Uebernahmeergebnis(false, Modellpruefung.Ressource("MP_MSG_FEHLER", ex.Message), erledigt, zahl, verworfen,
                                                  _ruecknahme.Count > 0, Pruefen());
                }
                if (n > 0)
                {
                    _ruecknahme[g] = rueck;
                    erledigt.Add(g);
                    zahl += n;
                }
            }
            return new Uebernahmeergebnis(true, "", erledigt, zahl, verworfen, erledigt.Count > 0, Pruefen());
        }

        /// <summary><b>Nimmt die letzte Übernahme der Gruppe zurück</b> (in umgekehrter Folge, ein Vorgang) und prüft neu.</summary>
        public Uebernahmeergebnis Zuruecknehmen(Pruefgruppe gruppe)
        {
            if (!_ruecknahme.TryGetValue(gruppe, out List<Action<DbVorgang>> schritte))
                return new Uebernahmeergebnis(false, Modellpruefung.Ressource("MP_MSG_KEINE_RUECKNAHME"), new Pruefgruppe[0], 0,
                                              new string[0], false, Pruefen());
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                {
                    for (int i = schritte.Count - 1; i >= 0; i--) schritte[i](v);
                    v.Commit();
                }
            }
            catch (Exception ex)
            {
                return new Uebernahmeergebnis(false, Modellpruefung.Ressource("MP_MSG_FEHLER", ex.Message), new[] { gruppe }, 0,
                                              new string[0], true, Pruefen());
            }
            _ruecknahme.Remove(gruppe);
            return new Uebernahmeergebnis(true, "", new[] { gruppe }, schritte.Count, new string[0], _ruecknahme.Count > 0, Pruefen());
        }

        /// <summary>Führt einen Vorschlag aus; <c>null</c> bei Erfolg, sonst der Grund.</summary>
        private string Anwenden(DbVorgang v, Modellpruefstand s, Modellbefund b, List<Action<DbVorgang>> rueck, Dictionary<string, int> typcache)
        {
            Pruefvorschlag p = b.Vorschlag;
            switch (p.Art)
            {
                case Vorschlagsart.Aufbau: return AufbauAnwenden(v, s, b, rueck, typcache);
                case Vorschlagsart.Profil: return ProfilAnwenden(v, s, b, rueck);
                case Vorschlagsart.FerienSaison: return VorgabenKopieren(v, p.IdQuelle.Value, p.Name, rueck);
                case Vorschlagsart.Feiertagsland:
                {
                    string alt = s.Feiertagsland;
                    v.Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Feiertagsland\" = ? WHERE \"ID\" = ?",
                                 new DbParam("@f", p.Name), new DbParam("@g", IdGebaeude));
                    int g = IdGebaeude;
                    rueck.Add(x => x.Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Feiertagsland\" = ? WHERE \"ID\" = ?",
                                                new DbParam("@f", (object)alt ?? DBNull.Value), new DbParam("@g", g)));
                    return null;
                }
                default: return Modellpruefung.Ressource("MP_MSG_UNBEKANNT");
            }
        }

        private static string AufbauAnwenden(DbVorgang v, Modellpruefstand s, Modellbefund b, List<Action<DbVorgang>> rueck, Dictionary<string, int> typcache)
        {
            Pruefvorschlag p = b.Vorschlag;
            BauteilModel bt = s.Zonen.SelectMany(z => z.Bauteile).First(x => x.ID == b.IdBauteil);
            int idAufbau;
            switch (p.Herkunft)
            {
                case Vorschlagsherkunft.Projektaufbau:
                    idAufbau = p.IdAufbau.Value;
                    break;
                case Vorschlagsherkunft.Katalogaufbau:
                {
                    HashSet<int> vorher = AufbauIds(s.IdProjekt);
                    idAufbau = new BauteilaufbauCtrl().CopyFromStamm(p.IdAufbau.Value, s.IdProjekt);
                    if (idAufbau <= 0) return Modellpruefung.Ressource("MP_MSG_KATALOGKOPIE");
                    if (!vorher.Contains(idAufbau)) rueck.Add(AufbauLoeschen(idAufbau));
                    break;
                }
                default:
                {
                    Ersatzergebnis e = Modellvorschlagsregeln.Typaufbau(s, bt, p.U.Value);
                    if (e == null) return Modellpruefung.Ressource("MP_MSG_TYPAUFBAU");
                    string schluessel = bt.Bauteilart + "|" + e.Typ.Code + "|" + p.U.Value.ToString("R", CultureInfo.InvariantCulture);
                    if (!typcache.TryGetValue(schluessel, out idAufbau))
                    {
                        BauteilaufbauModel m = e.Aufbau;
                        var namen = new HashSet<string>(new BauteilaufbauCtrl().LesenJeProjekt(s.IdProjekt).Select(a => a.Bezeichner), StringComparer.Ordinal);
                        m.Bezeichner = BauteilaufbauCtrl.FreierName(namen, e.Typ.Bezeichner + ", U "
                                                                         + Math.Round(p.U.Value, 3).ToString("0.###", CultureInfo.CurrentCulture));
                        m.Bauteilart = bt.Bauteilart;
                        m.Herkunft = DbWerte.HERKUNFT_VORGABE;
                        m.Typaufbau = e.Typ.Code;
                        foreach (BauteilschichtModel sch in m.Schichten) sch.ID_Baustoff = null;
                        idAufbau = BauteilaufbauCtrl.ProjektaufbauEinfuegen(v, s.IdProjekt, m);
                        typcache[schluessel] = idAufbau;
                        rueck.Add(AufbauLoeschen(idAufbau));
                    }
                    break;
                }
            }
            int? alt = bt.ID_Aufbau;
            int idb = bt.ID;
            v.Ausfuehren("UPDATE \"Tab_Bauteil\" SET \"ID_Aufbau\" = ? WHERE \"ID\" = ?", new DbParam("@a", idAufbau), new DbParam("@b", idb));
            rueck.Add(x => x.Ausfuehren("UPDATE \"Tab_Bauteil\" SET \"ID_Aufbau\" = ? WHERE \"ID\" = ?",
                                        new DbParam("@a", (object)alt ?? DBNull.Value), new DbParam("@b", idb)));
            return null;
        }

        private static Action<DbVorgang> AufbauLoeschen(int id)
            => x => x.Ausfuehren("DELETE FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID\" = ?", new DbParam("@a", id));

        private static HashSet<int> AufbauIds(int idProjekt)
            => Ids("SELECT \"ID\" FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ?", idProjekt);

        private static HashSet<int> Ids(string sql, int id)
        {
            DataTable t = DataRepository.GetDataTable(sql, new DbParam("@i", id));
            return new HashSet<int>(t.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)));
        }

        private const string SQL_KALENDER_ZONE = "SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" = ?";
        private const string SQL_VORGABE_ZONE = "SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Zone\" = ?";
        private const string SQL_WOCHE_ZONE = "SELECT \"ID\" FROM \"Tab_Konditionierungswoche\" WHERE \"ID_Zone\" = ?";

        private string ProfilAnwenden(DbVorgang v, Modellpruefstand s, Modellbefund b, List<Action<DbVorgang>> rueck)
        {
            ZoneModel z = s.Zonen.First(x => x.ID == b.IdZone);
            HashSet<int> kal = Ids(SQL_KALENDER_ZONE, z.ID), vor = Ids(SQL_VORGABE_ZONE, z.ID), wo = Ids(SQL_WOCHE_ZONE, z.ID);
            string altProfil = z.Nutzungsprofil;
            var profil = new Planprofil(b.Vorschlag.IdProfil, b.Vorschlag.Name);
            string grund = ZonenplanCtrl.NutzungUebernehmen(IdGebaeude, z.ID, profil, z.Nutzflaeche, z.Raumhoehe);
            if (grund != null) return grund;
            int idz = z.ID;
            rueck.Add(x =>
            {
                foreach (int id in Ids(SQL_KALENDER_ZONE, idz).Where(i => !kal.Contains(i)))
                    x.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID\" = ?", new DbParam("@i", id));
                foreach (int id in Ids(SQL_VORGABE_ZONE, idz).Where(i => !vor.Contains(i)))
                    x.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID\" = ?", new DbParam("@i", id));
                foreach (int id in Ids(SQL_WOCHE_ZONE, idz).Where(i => !wo.Contains(i)))
                    x.Ausfuehren("DELETE FROM \"Tab_Konditionierungswoche\" WHERE \"ID\" = ?", new DbParam("@i", id));
                if (RaumnutzungSchema.ZonenspalteVorhanden())
                    x.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_ZONE + "\" SET \"" + RaumnutzungSchema.SPALTE_ZONE_NUTZUNGSPROFIL + "\" = ? WHERE \"ID\" = ?",
                                 new DbParam("@n", (object)altProfil ?? DBNull.Value), new DbParam("@z", idz));
            });
            return null;
        }

        private string VorgabenKopieren(DbVorgang v, int quelle, string zeile, List<Action<DbVorgang>> rueck)
        {
            const string SQL_IDS = "SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL";
            HashSet<int> vorher = Ids(SQL_IDS, IdGebaeude);
            int n = v.Ausfuehren(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE + "\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\") " +
                "SELECT ?, \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\" FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" " +
                "WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Zeile\" = ?",
                new DbParam("@g", IdGebaeude), new DbParam("@q", quelle), new DbParam("@z", zeile));
            if (n <= 0) return Modellpruefung.Ressource("MP_MSG_QUELLE_LEER");
            List<int> neu = Ids(SQL_IDS, IdGebaeude).Where(i => !vorher.Contains(i)).ToList();
            rueck.Add(x =>
            {
                foreach (int id in neu)
                    x.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID\" = ?", new DbParam("@i", id));
            });
            return null;
        }

        // =====================================================================
        //  Lesen
        // =====================================================================

        private static double? D(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && !(r[spalte] is DBNull) ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture) : (double?)null;

        private static string S(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && !(r[spalte] is DBNull) ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture) : null;

        /// <summary>Liest den Prüfstand des Gebäudes; <c>null</c>, wenn es es nicht gibt.</summary>
        internal Modellpruefstand Lesen()
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT g.*, z.\"ID_Projekt\" AS \"MP_Projekt\" FROM \"Tab_Gebaeude\" g " +
                "INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" WHERE g.\"ID\" = ?",
                new DbParam("@g", IdGebaeude));
            if (t.Rows.Count == 0) return null;
            DataRow r = t.Rows[0];
            var s = new Modellpruefstand
            {
                IdGebaeude = IdGebaeude,
                IdProjekt = Convert.ToInt32(r["MP_Projekt"], CultureInfo.InvariantCulture),
                Gebaeudeart = S(r, "Gebaeudeart"),
                Wohngebaeude = S(r, "Wohngebaeude_Nicht_Wohngebaeude"),
                Baualtersklasse = S(r, "Baualtersklasse"),
                Feiertagsland = S(r, "Feiertagsland"),
                Nutzflaeche = D(r, "Nutzflaeche"),
                Raumhoehe = D(r, "Raumhoehe"),
                FlaecheNutzer = D(r, "Flaeche_Nutzer"),
                Luftwechselrate = D(r, "Luftwechselrate"),
                Bauweise = D(r, "Bauweise"),
                SollTag = D(r, "Raumsolltemperatur_Tag"),
                KuehlungAktiv = D(r, "Kuehlung_Aktiv") == 1.0,
                FlaecheAussenwand = D(r, "Flaeche_Außenwand"),
                UAussenwand = D(r, "k_Wert_Außenwand"),
                FlaecheFenster = D(r, "gesamte_Fensterflaeche"),
                UFenster = D(r, "k_Wert_Fenster"),
                FlaecheDach = D(r, "Dachflaeche"),
                UDach = D(r, "k_Wert_Dachflaeche"),
                FlaecheGrund = D(r, "Grundflaeche"),
                UGrund = D(r, "k_Wert_Grundflaeche"),
                FerienBestand = D(r, "Ferien") == 1.0,
            };
            var zonen = new GebaeudeZonenCtrl();
            s.Zonen = zonen.LesenJeGebaeude(IdGebaeude);
            s.Luftstroeme = s.Zonen.Count > 1 ? zonen.LuftstroemeJeGebaeude(IdGebaeude) : new List<ZonenluftstromModel>();
            foreach (KeyValuePair<int, Zonenquelle> q in _quellen) s.Quellen[q.Key] = q.Value;

            if (KonditionierungSchema.Lesbar())
            {
                foreach (DataRow k in DataRepository.GetDataTable(
                             "SELECT \"ID_Zone\", \"Groesse\", \"Nutzung\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"ID\"",
                             new DbParam("@g", IdGebaeude)).Rows)
                {
                    int eigner = k["ID_Zone"] is DBNull ? 0 : Convert.ToInt32(k["ID_Zone"], CultureInfo.InvariantCulture);
                    if (!s.Kalender.TryGetValue(eigner, out HashSet<string> gr)) s.Kalender[eigner] = gr = new HashSet<string>(StringComparer.Ordinal);
                    gr.Add(S(k, "Groesse"));
                    string nutzung = S(k, "Nutzung");
                    if (!string.IsNullOrWhiteSpace(nutzung) && !s.Kalendernutzung.ContainsKey(eigner)) s.Kalendernutzung[eigner] = nutzung;
                }
                foreach (DataRow k in DataRepository.GetDataTable(
                             "SELECT DISTINCT \"Zeile\" FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL",
                             new DbParam("@g", IdGebaeude)).Rows)
                    s.Gebaeudevorgaben.Add(S(k, "Zeile"));
                foreach (DataRow k in DataRepository.GetDataTable(
                             "SELECT v.\"ID_Gebaeude\", v.\"Zeile\" FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" v " +
                             "INNER JOIN \"Tab_Gebaeude\" g ON g.\"ID\" = v.\"ID_Gebaeude\" " +
                             "INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                             "WHERE z.\"ID_Projekt\" = ? AND v.\"ID_Gebaeude\" <> ? AND v.\"ID_Zone\" IS NULL ORDER BY v.\"ID_Gebaeude\"",
                             new DbParam("@p", s.IdProjekt), new DbParam("@g", IdGebaeude)).Rows)
                {
                    int g = Convert.ToInt32(k["ID_Gebaeude"], CultureInfo.InvariantCulture);
                    string zeile = S(k, "Zeile");
                    if (zeile == DbWerte.KOND_ZEILE_FERIEN) s.FerienQuelle ??= g;
                    if (zeile == DbWerte.KOND_ZEILE_SAISON) s.SaisonQuelle ??= g;
                }
            }
            DataTable land = DataRepository.GetDataTable(
                "SELECT g.\"Feiertagsland\" FROM \"Tab_Gebaeude\" g INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE z.\"ID_Projekt\" = ? AND g.\"ID\" <> ? AND g.\"Feiertagsland\" IS NOT NULL ORDER BY g.\"ID\"",
                new DbParam("@p", s.IdProjekt), new DbParam("@g", IdGebaeude));
            if (land.Rows.Count > 0) s.FeiertagslandProjekt = S(land.Rows[0], "Feiertagsland");

            s.KlimaregionVorhanden = DataRepository.GetDataTable(
                "SELECT k.\"ID\" FROM \"Tab_Projekt\" p INNER JOIN \"Tab_Klimaregion\" k ON k.\"ID\" = p.\"ID_Klimaregion\" WHERE p.\"ID\" = ?",
                new DbParam("@p", s.IdProjekt)).Rows.Count > 0;
            if (GebaeudeZonenanschluss.NordherkunftVorhanden())
            {
                DataTable q = DataRepository.GetDataTable(
                    "SELECT \"" + NordrichtungSchema.SPALTE + "\" FROM \"" + NordrichtungSchema.TAB_QUELLE + "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"ID\" DESC LIMIT 1",
                    new DbParam("@g", IdGebaeude));
                if (q.Rows.Count > 0) s.Nordwinkelherkunft = S(q.Rows[0], NordrichtungSchema.SPALTE);
            }

            if (s.Mehrzonen)
            {
                var aufbau = new BauteilaufbauCtrl();
                foreach (BauteilaufbauModel a in aufbau.LesenJeProjekt(s.IdProjekt))
                    s.Projektaufbauten[a.ID] = Kennung(a, 0);
                int folge = 0;
                s.Katalogaufbauten = aufbau.LesenKatalog().Select(a => Kennung(a, folge++)).ToList();
                s.Vorbelegung = Raumnutzungsvorbelegung.Lesen();
            }
            return s;
        }

        /// <summary>Die Kennwerte eines Aufbaus für die Regel: U, Kapazität, Kennzeichen, Schicht ohne Stoffwert.</summary>
        internal static Aufbaukennung Kennung(BauteilaufbauModel a, int folge)
        {
            bool luecke = a.Schichten.Any(x => !x.IstLuftschicht && (!(x.Lambda > 0.0) || !(x.Rho > 0.0) || !(x.Cp > 0.0)));
            BauteilaufbauKennwerte k = null;
            try { k = BauteilaufbauCtrl.Kennwerte(a, mitBezugsperiode: false); } catch (Exception) { k = null; }
            return new Aufbaukennung(a.ID, a.Bezeichner, a.Bauteilart, a.SimulationUngeeignet, k?.U_WM2K, k?.Kapazitaet_KJM2K, luecke, folge);
        }
    }
}
