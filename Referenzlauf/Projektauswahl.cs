using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>Steckbrief eines Projekts: was ist konfiguriert, was ist zugeordnet.</summary>
    internal sealed class Projektprofil
    {
        public int ID;
        public string Name = "";
        public int IdKlimaregion;

        /// <summary>Tool_1..Tool_6 aus Tab_Einstellungen (Index 4 = PV, Index 5 = Stromspeicher).</summary>
        public readonly string[] Tools = new string[6];

        /// <summary>ID_Type aus Tab_Energieanlagen: 1=WP, 2=Solar, 3=PV, 4=Batterie, 10=Kessel, 11=BHKW, 12=Puffer.</summary>
        public readonly SortedSet<int> Anlagentypen = new SortedSet<int>();

        /// <summary>Es gibt eine Pufferspeicher-Zuordnung mit Erzeuger "Wärmepumpe" (nur die rechnet mit).</summary>
        public bool PufferFuerWP;

        /// <summary>Pufferspeicher-Zuordnung fuer irgendeinen Erzeuger.</summary>
        public bool PufferIrgendwo;

        /// <summary>
        /// Mindestens eine Waermepumpe nutzt einen Pufferspeicher als WAERMEQUELLE
        /// (Tab_Energieanlagen.WQ_Typ = 'Pufferspeicher'). Das ist ein eigener Codepfad:
        /// nur diese Projekte legen einen Quellspeicher an, erzeugen die
        /// QUELLE_&lt;AnlagenID&gt;-Serien, die quellspeicher_*.csv und eine
        /// Tab_ErgebnisPufferspeicher-Zeile mit Verwendung = 'Quelle'.
        /// Ohne so ein Projekt bliebe die halbe Speicherlogik regressionsfrei.
        /// </summary>
        public bool QuellspeicherWP;

        /// <summary>
        /// Anzahl der BHKW-Anlagenzeilen (ID_Type = 11). Mehr als eine bedeutet eine
        /// Kaskade aus mehreren Modulen - ein eigener Codepfad seit W4-E2: nur dort
        /// unterscheiden sich die drei Vollbenutzungsstunden-Groessen (Summe thermisch,
        /// ungewichtetes Mittel, leistungsgewichtet), und nur dort greift die
        /// Ausschreibungsgrenze je Anlage statt je Projektsumme. Ohne so ein Projekt
        /// bliebe die gesamte Kaskadenlogik des KWK-Zuschlags regressionsfrei.
        /// </summary>
        public int BhkwModule;

        public IEnumerable<string> GesetzteTools
        {
            get { return Tools.Where(t => !string.IsNullOrWhiteSpace(t)); }
        }

        public bool HatTool(string name)
        {
            return Tools.Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Nur die Waermepumpe ist als Erzeuger konfiguriert - das minimale Projekt.</summary>
        public bool NurWaermepumpe
        {
            get
            {
                var gesetzt = GesetzteTools.ToList();
                return gesetzt.Count == 1 &&
                       string.Equals(gesetzt[0], "Wärmepumpe", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Stabile Kennung der Erzeugerkombination - Grundlage der Vielfaltsauswahl.</summary>
        public string ToolSignatur
        {
            get { return string.Join("+", GesetzteTools.OrderBy(t => t, StringComparer.Ordinal)); }
        }

        /// <summary>
        /// Feinere Kennung: Erzeugerkombination plus zugeordnete Anlagen und Pufferlage.
        /// Zwei Projekte mit gleicher Tool-Signatur koennen ganz unterschiedliche Codepfade
        /// treffen - etwa "nur Waermepumpe" mit und ohne aktiven Pufferspeicher.
        /// </summary>
        public string Profilsignatur
        {
            get
            {
                return ToolSignatur + "|" + string.Join(",", Anlagentypen) + "|" +
                       (PufferFuerWP ? "PufWP" : "-") + "|" + (QuellspeicherWP ? "QuellSp" : "-");
            }
        }

        /// <summary>Vielfalt: mehr Gewerke und mehr Anlagen = interessanterer Regressionsfall.</summary>
        public int Vielfalt
        {
            get
            {
                return GesetzteTools.Count() * 2 + Anlagentypen.Count +
                       (PufferFuerWP ? 2 : 0) + (QuellspeicherWP ? 2 : 0);
            }
        }

        public string Ausstattung
        {
            get
            {
                string tools = string.Join(", ", GesetzteTools);
                if (tools.Length == 0) tools = "(keine)";
                string typen = string.Join(",", Anlagentypen.Select(TypName));
                return "Tools: " + tools + " | Anlagen: " + typen +
                       (PufferFuerWP ? " | Puffer(WP)" : PufferIrgendwo ? " | Puffer(anderer Erzeuger)" : "") +
                       (QuellspeicherWP ? " | Quellspeicher(WP)" : "");
            }
        }

        public static string TypName(int idType)
        {
            switch (idType)
            {
                case 1: return "WP";
                case 2: return "Solar";
                case 3: return "PV";
                case 4: return "Batterie";
                case 10: return "Kessel";
                case 11: return "BHKW";
                case 12: return "Puffer";
                default: return "Typ" + idType;
            }
        }
    }

    /// <summary>
    /// Liest die Projektlandschaft der Arbeitskopie und waehlt daraus eine
    /// Referenzmenge mit moeglichst unterschiedlicher Anlagenausstattung.
    /// </summary>
    internal static class Projektauswahl
    {
        public const int MIN_PROJEKTE = 5;

        // Paket 7: von 8 auf 9 erhoeht. Die neunte Stelle traegt die Pflichtkategorie
        // "Waermepumpe mit Quellspeicher" - ohne sie deckt die Referenzmenge den
        // QUELLE_-Pfad (Quellspeicher, quellspeicher_*.csv, Verwendung='Quelle')
        // nirgends ab. Die bisherigen acht Projekte bleiben unveraendert gewaehlt.
        public const int MAX_PROJEKTE = 9;

        /// <summary>Liest alle Projekte samt Konfiguration und Anlagenausstattung.</summary>
        public static List<Projektprofil> ProfileLesen()
        {
            var profile = new Dictionary<int, Projektprofil>();

            DataTable projekte = DataRepository.GetDataTable(
                "SELECT ID, Projektname, ID_Klimaregion FROM Tab_Projekt ORDER BY ID");
            foreach (DataRow r in projekte.Rows)
            {
                var p = new Projektprofil();
                p.ID = ZuInt(r["ID"]);
                p.Name = r["Projektname"] == DBNull.Value ? "" : Convert.ToString(r["Projektname"]);
                p.IdKlimaregion = ZuInt(r["ID_Klimaregion"]);
                if (p.ID > 0) profile[p.ID] = p;
            }

            DataTable einstellungen = DataRepository.GetDataTable(
                "SELECT ID_Projekt, Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6 FROM Tab_Einstellungen");
            foreach (DataRow r in einstellungen.Rows)
            {
                int id = ZuInt(r["ID_Projekt"]);
                Projektprofil p;
                if (!profile.TryGetValue(id, out p)) continue;
                for (int i = 0; i < 6; i++)
                {
                    object v = r["Tool_" + (i + 1)];
                    p.Tools[i] = v == DBNull.Value ? "" : Convert.ToString(v).Trim();
                }
            }

            DataTable anlagen = DataRepository.GetDataTable(
                "SELECT ID_Projekt, ID_Type FROM Tab_Energieanlagen");
            foreach (DataRow r in anlagen.Rows)
            {
                int id = ZuInt(r["ID_Projekt"]);
                Projektprofil p;
                if (!profile.TryGetValue(id, out p)) continue;
                int typ = ZuInt(r["ID_Type"]);
                if (typ > 0) p.Anlagentypen.Add(typ);
                if (typ == 11) p.BhkwModule++;   // Kaskadenmerkmal, siehe Projektprofil.BhkwModule
            }

            DataTable puffer = DataRepository.GetDataTable(
                "SELECT ID_Projekt, Erzeuger FROM Z_ProjektPufferSp");
            foreach (DataRow r in puffer.Rows)
            {
                int id = ZuInt(r["ID_Projekt"]);
                Projektprofil p;
                if (!profile.TryGetValue(id, out p)) continue;
                p.PufferIrgendwo = true;
                string erz = r["Erzeuger"] == DBNull.Value ? "" : Convert.ToString(r["Erzeuger"]);
                // SimulationControl.Do_Simulation wertet ausschliesslich "Wärmepumpe" aus.
                if (string.Equals(erz, "Wärmepumpe", StringComparison.OrdinalIgnoreCase))
                    p.PufferFuerWP = true;
            }

            // Quellspeicher der Waermepumpe (Paket 7). Die Spalte WQ_Typ entsteht erst
            // mit SchemaSicherstellen bzw. der Migration - auf einer alten Datenbank
            // bleibt das Merkmal deshalb still ungesetzt, statt den Lauf abzubrechen.
            try
            {
                DataTable quelle = DataRepository.GetDataTable(
                    "SELECT ID_Projekt FROM Tab_Energieanlagen WHERE ID_Type = 1 AND WQ_Typ = 'Pufferspeicher'");
                if (quelle != null)
                    foreach (DataRow r in quelle.Rows)
                    {
                        Projektprofil p;
                        if (profile.TryGetValue(ZuInt(r["ID_Projekt"]), out p)) p.QuellspeicherWP = true;
                    }
            }
            catch { /* Spalte (noch) nicht vorhanden */ }

            return profile.Values.OrderBy(p => p.ID).ToList();
        }

        /// <summary>
        /// Waehlt die Referenzprojekte. Erst werden die Pflichtkategorien abgedeckt
        /// (WP+Puffer, Kessel, BHKW, Solarthermie, nur-WP), danach wird mit noch nicht
        /// vertretenen Erzeugerkombinationen auf MAX_PROJEKTE aufgefuellt.
        /// Deterministisch: bei gleichem Rang gewinnt die kleinere Projekt-ID.
        /// </summary>
        public static List<Tuple<Projektprofil, string>> Waehlen(List<Projektprofil> alle, Protokoll log)
        {
            // Nur Projekte, die ueberhaupt simulierbar sind.
            var kandidaten = alle.Where(p =>
                p.GesetzteTools.Any() && p.IdKlimaregion > 0).ToList();

            foreach (var p in alle.Except(kandidaten))
            {
                string grund = !p.GesetzteTools.Any()
                    ? "keine Erzeuger in Tab_Einstellungen"
                    : "keine Klimaregion";
                log.Zeile("  uebergangen: " + p.ID + " " + p.Name + " (" + grund + ")");
            }

            var gewaehlt = new List<Tuple<Projektprofil, string>>();
            var ids = new HashSet<int>();

            Action<Projektprofil, string> nimm = (p, grund) =>
            {
                if (p != null && ids.Add(p.ID))
                    gewaehlt.Add(Tuple.Create(p, grund));
            };

            // 1. Pflichtkategorien - jeweils der vielfaeltigste passende Kandidat.
            nimm(BesteWahl(kandidaten, ids, p => p.HatTool("Wärmepumpe") && p.PufferFuerWP, false),
                 "Pflichtkategorie: Waermepumpe mit Pufferspeicher");
            nimm(BesteWahl(kandidaten, ids, p => p.HatTool("Heizkessel"), false),
                 "Pflichtkategorie: Heizkessel");
            nimm(BesteWahl(kandidaten, ids, p => p.HatTool("BHKW"), false),
                 "Pflichtkategorie: BHKW");
            nimm(BesteWahl(kandidaten, ids, p => p.HatTool("Solarthermie"), false),
                 "Pflichtkategorie: Solarthermie");
            // Beim Minimalfall gewinnt das einfachste Projekt, nicht das reichhaltigste.
            nimm(BesteWahl(kandidaten, ids, p => p.NurWaermepumpe, true),
                 "Pflichtkategorie: nur Waermepumpe (Minimalfall)");
            // Paket 7: der Quellspeicher-Pfad. BEWUSST als letzte Pflichtkategorie -
            // so bleiben die fuenf bisherigen Wahlen (und damit die eingefrorene
            // Referenzmenge) unveraendert, es kommt nur ein Projekt hinzu.
            nimm(BesteWahl(kandidaten, ids, p => p.QuellspeicherWP, false),
                 "Pflichtkategorie: Waermepumpe mit Quellspeicher");
            // W4-E2: die BHKW-Kaskade. Ebenfalls BEWUSST hinten angehaengt, damit die
            // bisherigen Wahlen unveraendert bleiben. Die Profilsignatur allein reicht
            // hier nicht - ein Projekt mit zwei BHKW-Modulen sieht darin genauso aus wie
            // eines mit einem Modul, wuerde also nie gezogen.
            nimm(BesteWahl(kandidaten, ids, p => p.BhkwModule > 1, false),
                 "Pflichtkategorie: BHKW-Kaskade mit mehreren Modulen");

            // 2a. Auffuellen mit noch nicht vertretenen Erzeugerkombinationen.
            var toolSignaturen = new HashSet<string>(gewaehlt.Select(g => g.Item1.ToolSignatur));
            foreach (var p in kandidaten
                        .Where(p => !ids.Contains(p.ID))
                        .OrderByDescending(p => p.Vielfalt).ThenBy(p => p.ID))
            {
                if (gewaehlt.Count >= MAX_PROJEKTE) break;
                if (!toolSignaturen.Add(p.ToolSignatur)) continue;
                nimm(p, "neue Erzeugerkombination (" + p.ToolSignatur + ")");
            }

            // 2b. Weiter auffuellen, solange sich wenigstens die Anlagenausstattung
            //     oder die Pufferlage von allem bisher Gewaehlten unterscheidet.
            var profilSignaturen = new HashSet<string>(gewaehlt.Select(g => g.Item1.Profilsignatur));
            foreach (var p in kandidaten
                        .Where(p => !ids.Contains(p.ID))
                        .OrderByDescending(p => p.Vielfalt).ThenBy(p => p.ID))
            {
                if (gewaehlt.Count >= MAX_PROJEKTE) break;
                if (!profilSignaturen.Add(p.Profilsignatur)) continue;
                nimm(p, "abweichende Anlagenausstattung (" + p.Ausstattung + ")");
            }

            // 3. Notfalls auf die Mindestzahl auffuellen.
            foreach (var p in kandidaten
                        .Where(p => !ids.Contains(p.ID))
                        .OrderByDescending(p => p.Vielfalt).ThenBy(p => p.ID))
            {
                if (gewaehlt.Count >= MIN_PROJEKTE) break;
                nimm(p, "Auffuellung auf die Mindestzahl von " + MIN_PROJEKTE);
            }

            return gewaehlt.OrderBy(g => g.Item1.ID).ToList();
        }

        private static Projektprofil BesteWahl(List<Projektprofil> kandidaten, HashSet<int> schonGewaehlt,
                                               Func<Projektprofil, bool> filter, bool moeglichstEinfach)
        {
            var passend = kandidaten.Where(p => !schonGewaehlt.Contains(p.ID) && filter(p));
            var sortiert = moeglichstEinfach
                ? passend.OrderBy(p => p.Vielfalt).ThenBy(p => p.ID)
                : passend.OrderByDescending(p => p.Vielfalt).ThenBy(p => p.ID);
            return sortiert.FirstOrDefault();
        }

        private static int ZuInt(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }
}
