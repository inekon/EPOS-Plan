// Der Bauplan des Referenzprojekts 1052 "Referenzprojekt Zonen" (Mehrzonenkonzept 9, Stufe G6d) -
// die EINE Stelle, an der Zonenschnitt, gesaete Zellen und Programmwege stehen. Zwei Nutzer:
//   - das Saatskript referenzprojekt_1052_zonen.cs (#:include), das 1052 in der Testdatenbank anlegt;
//   - die Wache EPOS.Kern.Tests/ZonenReferenzprojektWacheTests (verlinkt im Testprojekt), die jede
//     gesaete Zelle gegen diesen Plan haelt und dieselben Programmwege auf einer Arbeitskopie
//     nachgeht (bitgleicher Abdruck).
// Darum nur oeffentliche Wege des Kerns (das Skript sieht keine internen) und kein SQL, das schreibt -
// ausser den Kopfzellen von Tab_Projekt (Beschreibung, Datum; Muster 1049/1050).
//
// DER ZONENSCHNITT. Vorlage ist 1018 "BHKW Test München" (Hotel-G-136, ein einzoniges VDI-6007-
// Gebaeude, Zuordnung 500 m² Wohnflaeche). "Gebaeude als eine Zone uebernehmen"
// (GebaeudeZonenCtrl.Uebernahme, der Weg des Zonendialogs) liefert die hochgerechnete Huelle der
// Projektkopie (Faktor 500 / 1975,34 m²); daraus entstehen drei Zonen:
//   1 "Gästezimmer"                  beheizt, 60 % der Nutzflaeche und der Aussenhuelle
//                                    (Waende, Fenster, Sonstiges, Dach, je mit ihrem psi·L),
//                                    dazu 60 % der Bodenplatte als Kellerdecke zur Zone 3 und
//                                    die Trennwand zur Zone 2;
//   2 "Gastronomie und Verwaltung"   beheizt, 40 % derselben Flaechen, Kellerdecke 40 %;
//   3 "Keller"                       unbeheizt, die ganze Bodenplatte am Erdreich, Kellerwaende am
//                                    Erdreich (Umfang des flaechengleichen Quadrats × 2,5 m),
//                                    Nutzflaeche = Bodenplatte, keine inneren Gewinne, keine Bewohner,
//                                    Infiltration 0,2 1/h, kein Nutzerluftwechsel.
// Die Trennwand (30 m², U 0,6 W/(m²K), Gruppe nach der 4-K-Regel) fuehrt Zone 1, die Kellerdecken
// (U der Bodenplatte) die beheizten Zonen - je Paar EINE Seite (Trennflaechenbilanz). Zwischen 1 und
// 2 stroemen 150 m³/h (Ueberstroemung ueber den Flur). Alle uebrigen Zonenfelder bleiben leer
// (Wert des Gebaeudes).
//
// DIE ZONENKALENDER. Je beheizte Zone ein Heizkalender aus einer ausgelieferten Vorlage der Groesse
// "Heizsollwert" (KonditionierungsvorlageCtrl.Uebernehmen, der Weg "Vorlage uebernehmen"):
// Gaestezimmer "Wohnen" (20 °C, Nachtabsenkung 18 °C von 22 bis 6 Uhr), Gastronomie "Büro"
// (20 °C, Nacht 16 °C von 18 bis 7 Uhr, Wochenende und Feiertage 16 °C) - die Spruenge liegen zu
// verschiedenen Stunden. Gewaehlt wird die Vorlage ueber Nutzung und Auslieferung, nicht ueber ihre Id.
//
// DIE PROJEKTEINSTELLUNG. Aufheizoptimierung an (KonfigurationCtrl.AufheizvorgabeSetzen): Bemessung
// (a) kaelteste Stunde, rho 20 % ausdruecklich, taeglich, kein Aufschlag; die Quelle ist die
// Zielleistung (keine Heizleistungsgrenze an Gebaeude und Zonen). Keine manuelle Aufheizzeit am
// Gebaeude (Rueckfall auf die Projektart). Alles uebrige wie 1018.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using WindowsFormsApplication1;

namespace EPOS.Referenzlaeufe.Skripte
{
    /// <summary>Der Bauplan des Referenzprojekts 1052 (Kopf der Datei).</summary>
    public static class Zonenprojekt1052
    {
        public const int VORLAGE = 1018;
        public const string VORLAGE_NAME = "BHKW Test München";
        public const int NEU = 1052;
        /// <summary>Die Nummer vor 1052 gehoert dem Referenzprojekt 1051 (RP1).</summary>
        public const int VORBEHALTEN = 1051;
        public const string NAME = "Referenzprojekt Zonen";
        public const string BESCHREIBUNG =
            "Referenzprojekt des Mehrzonenwegs: Kopie von Projekt 1018 (Hotel, BHKW und Kessel), das Gebäude in drei " +
            "Zonen — Gästezimmer (60 %) und Gastronomie/Verwaltung (40 %), beheizt, mit Trennwand und Luftaustausch, " +
            "darunter ein unbeheizter Keller am Erdreich; je beheizte Zone ein eigener Heizkalender (Wohnen, Büro), " +
            "Aufheizoptimierung an. Hält Zonenschleife, Trennflächen, Zonenluftstrom, Zonenkalender und " +
            "Aufheizplanung je Zone im Regressionsnetz.";
        public const string DATUM = "2026-10-03 00:00:00";

        public const string ZONE_GAESTE = "Gästezimmer";
        public const string ZONE_GASTRO = "Gastronomie und Verwaltung";
        public const string ZONE_KELLER = "Keller";

        /// <summary>Anteil der Gaestezimmer an Nutzflaeche und Aussenhuelle; die Gastronomie traegt den Rest.</summary>
        public const double ANTEIL_GAESTE = 0.6;
        public const double ANTEIL_GASTRO = 0.4;

        public const string TRENNWAND = "Trennwand Gastronomie";
        public const double TRENNWAND_FLAECHE = 30.0;     // m²
        public const double TRENNWAND_U = 0.6;            // W/(m²K)
        public const string KELLERDECKE = "Kellerdecke";
        public const string KELLERBODEN = "Kellerboden";
        public const string KELLERWAENDE = "Kellerwände";
        public const double KELLERHOEHE = 2.5;            // m, Hoehe der Kellerwaende
        public const double KELLER_INFILTRATION = 0.2;    // 1/h
        public const double LUFTSTROM = 150.0;            // m³/h zwischen Gaestezimmern und Gastronomie

        public const string NUTZUNG_GAESTE = "WOHNEN";
        public const string NUTZUNG_GASTRO = "BUERO";

        /// <summary>
        /// Die Sollwerte, die "Vorlage uebernehmen" in die Bestandsfelder der Zone schreibt (Tag, Nacht,
        /// Wochenende, Ferien in °C; leer = Wert des Gebaeudes): Wohnen 20/18, Buero 20/16/16/16.
        /// </summary>
        public static readonly double?[] SOLL_GAESTE = { 20.0, 18.0, null, null };
        public static readonly double?[] SOLL_GASTRO = { 20.0, 16.0, 16.0, 16.0 };

        /// <summary>Die Aufheizvorgabe des Projekts: an, (a), rho 20 %, taeglich, ohne Aufschlag.</summary>
        public static readonly Aufheizvorgabe AUFHEIZ = new Aufheizvorgabe(true, null, null, 0.2, null, null, null);

        // vorlaeufige Ids im Arbeitsstand (wie im Zonendialog: negativ)
        private const int Z_GAESTE = -1, Z_GASTRO = -2, Z_KELLER = -3;

        // =====================================================================
        //  Lesen (nur lesend, ueber DataRepository)
        // =====================================================================

        private static DbParam P(object w) => new DbParam("?", w ?? DBNull.Value);

        public static long Zahl(string sql, params object[] w)
        {
            object o = DataRepository.ExecuteScalar(sql, w.Select(P).ToArray());
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        public static DataTable Tabelle(string sql, params object[] w) => DataRepository.GetDataTable(sql, w.Select(P).ToArray());

        /// <summary>Die Zuordnung (Z_ProjektGebaeude.ID) des einen Gebaeudes eines Projekts; 0 = keins oder mehrere.</summary>
        public static int Zuordnung(int projekt)
            => Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", projekt) == 1
                ? (int)Zahl("SELECT ID FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", projekt) : 0;

        /// <summary>Die Projektkopie in Tab_Gebaeude zur Zuordnung; 0 = keine.</summary>
        public static int Gebaeude(int projekt)
            => (int)Zahl("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?", projekt, Zuordnung(projekt));

        // =====================================================================
        //  Der Plan
        // =====================================================================

        /// <summary>Der Zonenschnitt als Arbeitsstand des Zonendialogs.</summary>
        public sealed class Plan
        {
            public double Faktor;
            public List<ZoneModel> Zonen = new List<ZoneModel>();
            public List<ZonenluftstromModel> Luftstroeme = new List<ZonenluftstromModel>();
        }

        /// <summary>
        /// Bildet den Zonenschnitt aus der Uebernahme des Gebaeudes von <paramref name="projekt"/> (ein
        /// Projekt ohne Zonen: die Vorlage 1018 oder ihre frische Kopie). <c>null</c> mit Meldung, wenn
        /// die Uebernahme nicht gelingt.
        /// </summary>
        public static Plan Planen(int projekt, out string meldung)
        {
            meldung = null;
            int idZ = Zuordnung(projekt);
            if (idZ <= 0) { meldung = "Projekt " + projekt + ": nicht genau ein Gebäude"; return null; }
            GebaeudeZonenCtrl.Uebernahmevorschlag v = GebaeudeZonenCtrl.Uebernahme(projekt, idZ, null);
            if (!v.Ok || v.Zone == null) { meldung = "Übernahme scheitert: " + v.Meldung; return null; }

            List<BauteilModel> huelle = v.Zone.Bauteile;
            BauteilModel boden = huelle.SingleOrDefault(b => b.Bauteilart == DbWerte.BAUTEILART_BODENPLATTE);
            if (boden == null) { meldung = "Die Übernahme trägt keine Bodenplatte."; return null; }
            if (!v.Zone.Nutzflaeche.HasValue) { meldung = "Die Übernahme trägt keine Nutzfläche."; return null; }
            double nutz = v.Zone.Nutzflaeche.Value;

            List<BauteilModel> Aussen(double anteil, int nachbarKeller)
            {
                var l = new List<BauteilModel>();
                foreach (BauteilModel b in huelle.Where(b => b != boden))
                {
                    BauteilModel k = b.Kopie();
                    k.ID = 0;
                    k.Flaeche = anteil * b.Flaeche;
                    k.Psi_L = b.Psi_L.HasValue ? anteil * b.Psi_L.Value : (double?)null;
                    l.Add(k);
                }
                l.Add(new BauteilModel
                {
                    Bezeichner = KELLERDECKE, Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = anteil * boden.Flaeche,
                    U_Wert = boden.U_Wert, Neigung = 180.0, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE,
                    ID_Nachbarzone = nachbarKeller,
                });
                return l;
            }

            List<BauteilModel> gaeste = Aussen(ANTEIL_GAESTE, Z_KELLER);
            gaeste.Add(new BauteilModel
            {
                Bezeichner = TRENNWAND, Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = TRENNWAND_FLAECHE,
                U_Wert = TRENNWAND_U, Neigung = 90.0, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = Z_GASTRO,
            });
            List<BauteilModel> gastro = Aussen(ANTEIL_GASTRO, Z_KELLER);
            var keller = new List<BauteilModel>
            {
                new BauteilModel
                {
                    Bezeichner = KELLERBODEN, Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Flaeche = boden.Flaeche,
                    U_Wert = boden.U_Wert, Neigung = 180.0, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH,
                },
                new BauteilModel
                {
                    Bezeichner = KELLERWAENDE, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                    Flaeche = 4.0 * Math.Sqrt(boden.Flaeche) * KELLERHOEHE,
                    U_Wert = boden.U_Wert, Neigung = 90.0, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH,
                },
            };

            var plan = new Plan { Faktor = v.Faktor };
            plan.Zonen.Add(new ZoneModel { ID = Z_GAESTE, Bezeichner = ZONE_GAESTE, Nutzflaeche = ANTEIL_GAESTE * nutz, Bauteile = gaeste });
            plan.Zonen.Add(new ZoneModel { ID = Z_GASTRO, Bezeichner = ZONE_GASTRO, Nutzflaeche = ANTEIL_GASTRO * nutz, Bauteile = gastro });
            plan.Zonen.Add(new ZoneModel
            {
                ID = Z_KELLER, Bezeichner = ZONE_KELLER, Nutzflaeche = boden.Flaeche, IstBeheizt = false,
                Interne_Waermegewinne = 0.0, Bewohner = 0.0, Luftwechsel_Infiltration = KELLER_INFILTRATION,
                Luftwechsel_Nutzer = 0.0, Bauteile = keller,
            });
            plan.Luftstroeme.Add(new ZonenluftstromModel { ID_ZoneA = Z_GAESTE, ID_ZoneB = Z_GASTRO, Volumenstrom = LUFTSTROM });
            return plan;
        }

        // =====================================================================
        //  Bauen - die Programmwege auf einer frischen Kopie der Vorlage
        // =====================================================================

        /// <summary>Die ausgelieferte Heizsollwert-Vorlage einer Nutzung; <c>null</c> = keine oder mehrere.</summary>
        public static KonditionierungsvorlageCtrl.Vorlage Heizvorlage(string nutzung)
        {
            List<KonditionierungsvorlageCtrl.Vorlage> l = new KonditionierungsvorlageCtrl().Liste(Konditionierungsgroesse.Heizsoll)
                .Where(v => v.Ausgeliefert && string.Equals(v.Nutzung, nutzung, StringComparison.Ordinal)).ToList();
            return l.Count == 1 ? l[0] : null;
        }

        /// <summary>
        /// Baut 1052 auf der frischen Kopie <paramref name="projekt"/> der Vorlage: Kopfzellen, Zonen samt
        /// Trennflaechen und Luftstrom (OK-Weg des Zonendialogs), Heizkalender je beheizter Zone ("Vorlage
        /// uebernehmen"), Aufheizvorgabe des Projekts. <c>null</c> = gelungen, sonst der Grund.
        /// </summary>
        public static string Bauen(int projekt)
        {
            // 1) Kopfzellen des Projekts (Muster 1049/1050).
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?",
                                           P(BESCHREIBUNG), P(DATUM), P(DATUM), P(projekt));

            // 2) Der Zonenschnitt ueber "Gebaeude als eine Zone uebernehmen" und den OK-Weg des Dialogs.
            Plan plan = Planen(projekt, out string m);
            if (plan == null) return m;
            int gebaeude = Gebaeude(projekt);
            if (gebaeude <= 0) return "Die Projektkopie des Gebäudes fehlt.";
            string pruef = GebaeudeZonenCtrl.Pruefen(plan.Zonen, plan.Luftstroeme);
            if (pruef != null) return "Zonenprüfung: " + pruef;
            GebaeudeZonenCtrl.Schreibergebnis e = new GebaeudeZonenCtrl().Schreiben(gebaeude, plan.Zonen, plan.Luftstroeme, null);
            if (!e.Ok) return "Zonen nicht geschrieben: " + e.Meldung;

            // 3) Je beheizte Zone ihr Heizkalender aus der Vorlage (wirksame Matrix der Zone als Ziel).
            var kond = new KonditionierungCtrl();
            foreach ((int vorlaeufig, string nutzung) in new[] { (Z_GAESTE, NUTZUNG_GAESTE), (Z_GASTRO, NUTZUNG_GASTRO) })
            {
                KonditionierungsvorlageCtrl.Vorlage vorlage = Heizvorlage(nutzung);
                if (vorlage == null) return "Die ausgelieferte Heizvorlage '" + nutzung + "' fehlt oder ist mehrdeutig.";
                int zone = e.Zonen[vorlaeufig];
                Konditionierungsarbeitsstand a = kond.ArbeitsstandLesen(gebaeude, null, out string ma);
                if (ma != null) return "Arbeitsstand: " + ma;
                KonditionierungCtrl.Ergebnis k = new KonditionierungsvorlageCtrl().Uebernehmen(
                    vorlage.Id, KonditionierungCtrl.Eigner.Zone(gebaeude, zone), a.Matrix(zone));
                if (!k.Ok) return "Vorlage '" + vorlage.Bezeichner + "' nicht übernommen: " + k.Meldung;
            }

            // 4) Die Projekteinstellung.
            if (!KonfigurationCtrl.AufheizvorgabeSetzen(projekt, AUFHEIZ)) return "Die Aufheizvorgabe ist nicht geschrieben.";

            // 5) Kopfzellen erneut: ein Schreibweg darf das Aenderungsdatum gestempelt haben, und die
            //    Kopie stempelt ueber die Trigger des Kostenstempels (KostenStempelSchema) die Uhrzeit
            //    des Laufs in Kosten_Geaendert - leer wie bei jedem anderen Projekt der Testdatenbank,
            //    sonst waere die Datei nicht wiederholbar.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Aenderungsdatum = ?, Erstelldatum = ?, Kosten_Geaendert = NULL WHERE ID = ?",
                                           P(DATUM), P(DATUM), P(projekt));
            return null;
        }

        // =====================================================================
        //  Pruefen - jede gesaete Zelle gegen den Plan der Vorlage
        // =====================================================================

        private static readonly FieldInfo[] ZONENFELDER = typeof(ZoneModel).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name != nameof(ZoneModel.ID) && f.Name != nameof(ZoneModel.ID_Gebaeude) && f.Name != nameof(ZoneModel.Bauteile))
            .OrderBy(f => f.MetadataToken).ToArray();

        private static readonly FieldInfo[] BAUTEILFELDER = typeof(BauteilModel).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name != nameof(BauteilModel.ID) && f.Name != nameof(BauteilModel.ID_Zone))
            .OrderBy(f => f.MetadataToken).ToArray();

        private static string Wert(object o) => o switch
        {
            null => "∅",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => o.ToString(),
        };

        /// <summary>
        /// Eine Zone als Text: alle Felder ausser den Schluesseln, die Nachbarzone als ihr Rang in der
        /// Liste; Flaechen nach dem Schreiben bitgleich (REAL rundet nicht).
        /// </summary>
        public static string Text(ZoneModel z, IList<ZoneModel> liste, bool mitRang = true)
        {
            var sb = new StringBuilder();
            foreach (FieldInfo f in ZONENFELDER)
                if (mitRang || f.Name != nameof(ZoneModel.Rang)) sb.Append(f.Name).Append('=').Append(Wert(f.GetValue(z))).Append(';');
            foreach (BauteilModel b in z.Bauteile)
            {
                sb.Append("\n  ");
                foreach (FieldInfo f in BAUTEILFELDER)
                {
                    if (!mitRang && f.Name == nameof(BauteilModel.Rang)) continue;
                    object w = f.GetValue(b);
                    if (f.Name == nameof(BauteilModel.ID_Nachbarzone) && w is int n)
                        w = "Zone " + (liste.Select((x, i) => (x, i)).Single(p => p.x.ID == n).i + 1).ToString(CultureInfo.InvariantCulture);
                    sb.Append(f.Name).Append('=').Append(Wert(w)).Append(';');
                }
            }
            return sb.ToString();
        }

        /// <summary>Die Abweichungen des Projekts <paramref name="projekt"/> vom Plan der Vorlage; leer = steht wie gesaet.</summary>
        public static List<string> Pruefen(int projekt)
        {
            var f = new List<string>();
            DataTable p = Tabelle("SELECT Beschreibung, Aenderungsdatum, Erstelldatum, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", projekt);
            if (p.Rows.Count != 1) { f.Add("Projekt " + projekt + " fehlt"); return f; }
            if (!string.Equals(Convert.ToString(p.Rows[0][0], CultureInfo.InvariantCulture), BESCHREIBUNG, StringComparison.Ordinal))
                f.Add("Beschreibung weicht ab");
            foreach (int i in new[] { 1, 2 })
                if (!string.Equals(Datum(p.Rows[0][i]), DATUM, StringComparison.Ordinal)) f.Add(p.Columns[i].ColumnName + " weicht ab");
            if (p.Rows[0][3] != DBNull.Value) f.Add("Kosten_Geaendert ist gestempelt");

            Plan plan = Planen(VORLAGE, out string m);
            if (plan == null) { f.Add("Plan der Vorlage: " + m); return f; }
            int gebaeude = Gebaeude(projekt);
            if (gebaeude <= 0) { f.Add("Gebäude fehlt"); return f; }

            // Zonen und Bauteile (jede Zelle, die Nachbarzone als Rang).
            var ctrl = new GebaeudeZonenCtrl();
            List<ZoneModel> ist = ctrl.LesenJeGebaeude(gebaeude);
            if (ist.Count != plan.Zonen.Count) { f.Add(ist.Count + " Zonen statt " + plan.Zonen.Count); return f; }
            for (int i = 0; i < ist.Count; i++)
            {
                ZoneModel s = plan.Zonen[i].Kopie();
                s.Rang = i + 1;
                double?[] soll = i == 0 ? SOLL_GAESTE : i == 1 ? SOLL_GASTRO : null;
                if (soll != null)
                {
                    s.Raumsolltemperatur_Tag = soll[0];
                    s.Raumsolltemperatur_Nachtabsenkung = soll[1];
                    s.Raumsolltemperatur_Wochenende = soll[2];
                    s.Raumsolltemperatur_Ferien = soll[3];
                }
                for (int j = 0; j < s.Bauteile.Count; j++) s.Bauteile[j].Rang = j + 1;
                string a = Text(ist[i], ist), b = Text(s, plan.Zonen);
                if (a != b) f.Add("Zone " + (i + 1) + " weicht ab:\n    ist  " + a + "\n    soll " + b);
            }

            // Der Luftstrom: ein Paar, Zonen 1 und 2.
            List<ZonenluftstromModel> l = ctrl.LuftstroemeJeGebaeude(gebaeude);
            if (l.Count != 1 || l[0].ID_ZoneA != Math.Min(ist[0].ID, ist[1].ID) || l[0].ID_ZoneB != Math.Max(ist[0].ID, ist[1].ID)
                || BitConverter.DoubleToInt64Bits(l[0].Volumenstrom) != BitConverter.DoubleToInt64Bits(LUFTSTROM))
                f.Add("Luftstrom weicht ab (" + l.Count + " Zeilen)");

            // Die Konditionierung: je beheizte Zone genau ein Kalender, Heizsollwert, aus ihrer Vorlage; sonst keiner.
            var kond = new KonditionierungCtrl();
            foreach ((ZoneModel z, string nutzung) in new[] { (ist[0], NUTZUNG_GAESTE), (ist[1], NUTZUNG_GASTRO), (ist[2], (string)null) })
            {
                Konditionierungsstand st = kond.StandLesen(KonditionierungCtrl.Eigner.Zone(gebaeude, z.ID), out string ms);
                if (ms != null) f.Add(z.Bezeichner + ": " + ms);
                IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> angelegt = st.Angelegt();
                if (nutzung == null)
                {
                    if (angelegt.Count != 0 || st.VorgabenAnzahl != 0) f.Add(z.Bezeichner + " trägt Konditionierung");
                    continue;
                }
                KonditionierungsvorlageCtrl.Vorlage v = Heizvorlage(nutzung);
                if (angelegt.Count != 1 || !angelegt.ContainsKey(Konditionierungsgroesse.Heizsoll))
                    f.Add(z.Bezeichner + ": " + angelegt.Count + " Kalender statt des Heizkalenders");
                else if (v == null || st.Herkunft(Konditionierungsgroesse.Heizsoll).Vorlage != v.Bezeichner)
                    f.Add(z.Bezeichner + ": Herkunft '" + st.Herkunft(Konditionierungsgroesse.Heizsoll) + "' statt Vorlage " + v?.Bezeichner);
            }
            if (Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL", gebaeude) != 0)
                f.Add("Das Gebäude trägt einen eigenen Kalender");

            // Die Projekteinstellung und das Gebaeude.
            Aufheizvorgabe av = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
            if (!AUFHEIZ.Equals(av)) f.Add("Aufheizvorgabe " + av + " statt " + AUFHEIZ);
            if (Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Aufheizzeit_Manuell_H IS NOT NULL", projekt) != 0)
                f.Add("Ein Gebäude trägt eine manuelle Aufheizzeit");
            if (Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Heizleistung_Max IS NOT NULL", projekt) != 0)
                f.Add("Ein Gebäude trägt eine Heizleistungsgrenze");

            foreach (string t in new[] { "Tab_Ergebnis", "Tab_ErgebnisWirtschaftlichkeit" })
                if (Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", projekt) != 0) f.Add(t + " führt Zeilen");
            return f;
        }

        private static string Datum(object o)
            => o is DateTime d ? d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
               : o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        // =====================================================================
        //  Abdruck
        // =====================================================================

        /// <summary>Die Tabellen mit Projektspalte (ID_Projekt bzw. ProjektID), nach Namen.</summary>
        public static List<(string Tabelle, string Spalte)> Projekttabellen()
        {
            var l = new List<(string, string)>();
            foreach (DataRow r in Tabelle("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name").Rows)
            {
                string t = Convert.ToString(r[0], CultureInfo.InvariantCulture);
                List<string> sp = DataRepository.SpaltenVonTabelle(t);
                string s = sp.FirstOrDefault(c => c.Equals("ID_Projekt", StringComparison.OrdinalIgnoreCase)) ??
                           sp.FirstOrDefault(c => c.Equals("ProjektID", StringComparison.OrdinalIgnoreCase));
                if (s != null) l.Add((t, s));
            }
            return l;
        }

        /// <summary>
        /// Heisst die Spalte einen Schluessel (Primaer- oder Fremdschluessel, Projektspalte)? Ein Namensteil
        /// "ID" zwischen Unterstrichen (ID, ID_Projekt, WS_ID_Puffer, Gebaeude_ID) oder ProjektID.
        /// </summary>
        private static bool Schluessel(string spalte)
            => spalte.Split('_').Any(t => t.Equals("ID", StringComparison.OrdinalIgnoreCase))
               || spalte.Equals("ProjektID", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Der Abdruck eines Projekts ueber alle Projekttabellen samt Senken, Zonen, Bauteilen, Luftstroemen
        /// und Konditionierung seiner Gebaeude. <paramref name="ohneSchluessel"/>: Schluesselspalten fallen
        /// weg (Reihenfolge nach Schluessel bleibt) - so sind zwei Kopien derselben Programmwege mit
        /// verschiedenen Ids vergleichbar; die Bezuege zwischen den Zonen haelt <see cref="Text"/>.
        /// </summary>
        public static string Abdruck(int projekt, bool ohneSchluessel)
        {
            var sb = new StringBuilder();
            void Teil(string sql, string kopf)
            {
                DataTable dt = Tabelle(sql, projekt);
                sb.Append('#').Append(kopf).Append('\n');
                foreach (DataRow r in dt.Rows)
                {
                    foreach (DataColumn c in dt.Columns)
                    {
                        if (ohneSchluessel && Schluessel(c.ColumnName)) continue;
                        object w = r[c];
                        sb.Append(w == DBNull.Value ? "∅" : w is double d ? d.ToString("R", CultureInfo.InvariantCulture)
                                  : Convert.ToString(w, CultureInfo.InvariantCulture)).Append('|');
                    }
                    sb.Append('\n');
                }
            }
            const string GEB = "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?";
            const string ZON = "SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN (" + GEB + ")";
            Teil("SELECT * FROM Tab_Projekt WHERE ID = ?", "Tab_Projekt");
            foreach ((string t, string s) in Projekttabellen())
                Teil("SELECT * FROM \"" + t + "\" WHERE \"" + s + "\" = ? ORDER BY 1", t);
            Teil("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?) ORDER BY 1", "Z_AnlageSenke");
            Teil("SELECT * FROM Tab_Zone WHERE ID_Gebaeude IN (" + GEB + ") ORDER BY ID_Gebaeude, Rang", "Tab_Zone");
            Teil("SELECT * FROM Tab_Bauteil WHERE ID_Zone IN (" + ZON + ") ORDER BY ID_Zone, Rang", "Tab_Bauteil");
            Teil("SELECT * FROM Tab_Zonenluftstrom WHERE ID_ZoneA IN (" + ZON + ") ORDER BY ID", "Tab_Zonenluftstrom");
            Teil("SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude IN (" + GEB + ") ORDER BY ID", "Tab_Konditionierungsvorgabe");
            Teil("SELECT * FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + GEB + ") ORDER BY ID", "Tab_Konditionierungskalender");
            Teil("SELECT p.* FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender " +
                 "WHERE k.ID_Gebaeude IN (" + GEB + ") ORDER BY p.ID", "Tab_Konditionierungsperiode");
            if (ohneSchluessel)
            {
                // Die Bezuege zwischen den Zonen, die der schluessellose Teil nicht sieht.
                int g = Gebaeude(projekt);
                var ctrl = new GebaeudeZonenCtrl();
                List<ZoneModel> z = g > 0 ? ctrl.LesenJeGebaeude(g) : new List<ZoneModel>();
                sb.Append("#Zonenbezuege\n");
                foreach (ZoneModel x in z) sb.Append(Text(x, z)).Append('\n');
                foreach (ZonenluftstromModel l in g > 0 ? ctrl.LuftstroemeJeGebaeude(g) : new List<ZonenluftstromModel>())
                    sb.Append(z.FindIndex(x => x.ID == l.ID_ZoneA) + 1).Append('-').Append(z.FindIndex(x => x.ID == l.ID_ZoneB) + 1).Append('\n');
                foreach (DataRow r in Tabelle("SELECT ID_Zone, Groesse FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + GEB +
                                              ") ORDER BY ID", projekt).Rows)
                    sb.Append("Kalender ").Append(r[0] == DBNull.Value ? "Gebäude" : "Zone " + (z.FindIndex(x => x.ID == Convert.ToInt32(r[0], CultureInfo.InvariantCulture)) + 1))
                      .Append(' ').Append(r[1]).Append('\n');
                foreach (DataRow r in Tabelle("SELECT ID_Zone, Groesse, Zeile FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude IN (" + GEB +
                                              ") ORDER BY ID", projekt).Rows)
                    sb.Append("Vorgabe ").Append(r[0] == DBNull.Value ? "Gebäude" : "Zone " + (z.FindIndex(x => x.ID == Convert.ToInt32(r[0], CultureInfo.InvariantCulture)) + 1))
                      .Append(' ').Append(r[1]).Append(' ').Append(r[2]).Append('\n');
            }
            return sb.ToString();
        }
    }
}
