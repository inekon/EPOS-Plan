using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Die Sicht auf das Schema der GEOEFFNETEN Datenbank: welche Tabelle traegt
    /// Projektdaten, welche ist Auslieferungskatalog, in welcher Reihenfolge wird
    /// geloescht.
    ///
    /// <para><b>Warum abgeleitet und nicht aufgeschrieben.</b> Der Auftrag verlangt die
    /// Liste „nicht geraten". Eine feste Liste wie in
    /// <c>sql/tools/Reduziere-Testdatenbank.sql</c> ist beim Schreiben richtig und beim
    /// naechsten Schemaschritt falsch: Die Schritte 65 und 66 haben mit
    /// <c>Tab_Wechselrichter</c> und <c>Z_AnlageStrang</c> zwei Tabellen ergaenzt, die in
    /// jener Liste fehlen. Deshalb wird hier gefragt statt erinnert - ueber genau die
    /// Schemaauskuenfte, die der Kern ohnehin fuehrt
    /// (<see cref="DataRepository.SpaltenVonTabelle"/>,
    /// <see cref="DataRepository.FremdschluesselListe"/>).</para>
    ///
    /// <para><b>Die drei Regeln.</b>
    /// <list type="number">
    ///   <item><b>Projektspalte.</b> Eine Tabelle mit einer Spalte <c>ID_Projekt</c> oder
    ///   <c>ProjektID</c> fuehrt Projektdaten. Das ist die Stufe 1 des Vorbildskripts;
    ///   dort standen 19 Kaskadentabellen, 26 Tabellen ohne Fremdschluessel und die eine
    ///   mit abweichendem Spaltennamen (<c>Berichtskonfiguration.ProjektID</c>) - alle
    ///   drei Gruppen fallen unter dieselbe Frage.</item>
    ///   <item><b>Folgetabellen.</b> Was ueber einen Fremdschluessel an einer Tabelle der
    ///   Stufe 1 haengt, gehoert mit dazu, und was daran haengt, ebenso - die
    ///   transitive Huelle. Das sind die Stufen 3 des Vorbilds (Ganglinien-Daten,
    ///   Ergebnisdetails, Modulzeilen).</item>
    ///   <item><b>Kataloge sind ausgenommen.</b> Ein Tabellenname, der auf <c>_STAMM</c>
    ///   endet, ist Auslieferungskatalog und wird von der Projektbereinigung nie
    ///   angefasst - auch dann nicht, wenn er aus Access-Zeiten eine Spalte
    ///   <c>ID_Projekt</c> mitschleppt. Betroffen ist genau eine Tabelle,
    ///   <c>Tab_Kenndaten_Kuehlung_STAMM</c>, und das Vorbildskript nimmt sie aus
    ///   demselben Grund aus.</item>
    /// </list></para>
    ///
    /// <para><b>Der Vergleich ist ORDINAL, und das ist der Punkt.</b>
    /// <c>Tab_Brennstoff_Stamm</c> endet auf <c>_Stamm</c> in gemischter Schreibweise und
    /// ist KEIN <c>*_STAMM</c>-Katalog im Sinne der Namenskonvention, sondern ein
    /// gewoehnlicher Katalog ohne Projektbezug - <c>Reduziere-Testdatenbank.sql</c>
    /// fuehrt ihn unter „Alle uebrigen Kataloge ohne Projektbezug". Ein Vergleich ohne
    /// Ruecksicht auf Gross-/Kleinschreibung wuerde ihn unter die ReadOnly-Regel der
    /// Kataloge ziehen und damit alle 25 Brennstoffe loeschen (alle tragen
    /// <c>ReadOnly = 0</c>) - und mit ihnen die Aufloesung der Energietraeger beim
    /// Beispielimport.</para>
    /// </summary>
    internal sealed class Projektsicht
    {
        /// <summary>Die Kopftabelle der Projekte.</summary>
        internal const string TAB_PROJEKT = "Tab_Projekt";

        /// <summary>Die einzeilige Statustabelle; sie wird umgehaengt, nicht geleert.</summary>
        internal const string TAB_APPLIKATION = "Tab_Applikation";

        private static readonly string[] PROJEKTSPALTEN = { "ID_Projekt", "ProjektID" };

        private readonly Dictionary<string, List<string>> _spalten =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private Projektsicht() { }

        /// <summary>Alle Tabellen der Datei in Namensreihenfolge (ohne Sichten, ohne sqlite_*).</summary>
        internal IReadOnlyList<string> AlleTabellen { get; private set; }

        /// <summary>Die Auslieferungskataloge — Tabellennamen auf <c>_STAMM</c> (ordinal).</summary>
        internal IReadOnlyList<string> Stammtabellen { get; private set; }

        /// <summary>
        /// Stufe 1: Tabellen mit eigener Projektspalte, ohne <c>Tab_Projekt</c> selbst,
        /// ohne <c>Tab_Applikation</c> und ohne die Kataloge.
        /// </summary>
        internal IReadOnlyList<Projekttabelle> Stufe1 { get; private set; }

        /// <summary>
        /// Stufe 2: die transitive Huelle darunter, ELTERN VOR KINDERN sortiert — die
        /// Loeschbedingung jeder Zeile fragt den Bestand ihrer Elterntabelle ab und
        /// braucht sie deshalb bereits bereinigt.
        /// </summary>
        internal IReadOnlyList<Folgetabelle> Stufe2 { get; private set; }

        /// <summary>Alle Tabellen mit Projektbezug — Stufe 1, Stufe 2 und <c>Tab_Projekt</c>.</summary>
        internal IReadOnlyList<string> Projekttabellen =>
            new[] { TAB_PROJEKT }
                .Concat(Stufe1.Select(s => s.Tabelle))
                .Concat(Stufe2.Select(s => s.Tabelle))
                .ToList();

        /// <summary>Die Spalten einer Tabelle (Schemareihenfolge), aus dem Zwischenspeicher.</summary>
        internal List<string> Spalten(string tabelle)
        {
            if (!_spalten.TryGetValue(tabelle, out List<string> s))
            {
                s = DataRepository.SpaltenVonTabelle(tabelle);
                _spalten[tabelle] = s;
            }
            return s;
        }

        /// <summary>Traegt die Tabelle eine Spalte dieses Namens?</summary>
        internal bool Hat(string tabelle, string spalte) =>
            Spalten(tabelle).Any(s => string.Equals(s, spalte, StringComparison.OrdinalIgnoreCase));

        /// <summary>Ist der Name ein Auslieferungskatalog (<c>*_STAMM</c>, ordinal)?</summary>
        internal static bool IstStamm(string tabelle) =>
            tabelle != null && tabelle.EndsWith("_STAMM", StringComparison.Ordinal);

        /// <summary>Liest das Schema der aktuell eingestellten Datenbank aus.</summary>
        internal static Projektsicht Lesen()
        {
            var sicht = new Projektsicht();

            var tabellen = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");
            foreach (DataRow r in dt.Rows) tabellen.Add(Convert.ToString(r["name"]));
            sicht.AlleTabellen = tabellen;
            sicht.Stammtabellen = tabellen.Where(IstStamm).ToList();

            // ---- Stufe 1: eigene Projektspalte -------------------------------------
            var stufe1 = new List<Projekttabelle>();
            foreach (string t in tabellen)
            {
                if (IstStamm(t) || t == TAB_PROJEKT || t == TAB_APPLIKATION) continue;
                string spalte = PROJEKTSPALTEN.FirstOrDefault(p => sicht.Hat(t, p));
                if (spalte != null) stufe1.Add(new Projekttabelle(t, spalte));
            }
            sicht.Stufe1 = stufe1;

            // ---- Stufe 2: transitive Huelle ueber die Fremdschluessel ---------------
            // Tiefe 1 = haengt unmittelbar an einer Tabelle der Stufe 1. Die Tiefe ist
            // zugleich die Loeschreihenfolge: Tab_Ergebnis (Stufe 1) -> Tab_ErgebnisBHKW
            // (Tiefe 1) -> Tab_ErgebnisBHKWModul (Tiefe 2).
            var erfasst = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { TAB_PROJEKT };
            foreach (Projekttabelle p in stufe1) erfasst.Add(p.Tabelle);

            var stufe2 = new List<Folgetabelle>();
            int tiefe = 0;
            bool weiter = true;
            while (weiter && tiefe < 20)
            {
                weiter = false;
                tiefe++;
                foreach (string t in tabellen)
                {
                    if (erfasst.Contains(t) || IstStamm(t) || t == TAB_APPLIKATION) continue;
                    Folgetabelle treffer = null;
                    foreach (DataRow r in DataRepository.FremdschluesselListe(t).Rows)
                    {
                        string ziel = Convert.ToString(r["Zieltabelle"]);
                        if (!erfasst.Contains(ziel)) continue;
                        string von = Convert.ToString(r["Quellspalte"]);
                        string nach = r["Zielspalte"] == DBNull.Value ? null : Convert.ToString(r["Zielspalte"]);
                        if (string.IsNullOrEmpty(nach)) nach = Schluesselspalte(ziel);
                        treffer = new Folgetabelle(t, von, ziel, nach, tiefe);
                        break;
                    }
                    if (treffer == null) continue;
                    stufe2.Add(treffer);
                    weiter = true;
                }
                // Erst nach dem vollstaendigen Durchlauf aufnehmen, sonst zoege eine
                // Tabelle ihre Geschwister in dieselbe Tiefe.
                foreach (Folgetabelle f in stufe2) erfasst.Add(f.Tabelle);
            }
            sicht.Stufe2 = stufe2.OrderBy(f => f.Tiefe).ThenBy(f => f.Tabelle, StringComparer.Ordinal).ToList();

            return sicht;
        }

        /// <summary>
        /// Die Schluesselspalte einer Tabelle fuer den Fall, dass ein Fremdschluessel sein
        /// Ziel nicht benennt (SQLite meint dann den Primaerschluessel). Kommt im
        /// Zielschema nicht vor, ist aber der einzige Weg, an dem das Ableiten sonst
        /// still danebenlaege.
        /// </summary>
        private static string Schluesselspalte(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM pragma_table_info(?) WHERE pk > 0 ORDER BY pk", new DbParam("?", tabelle));
            return dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0]["name"]) : "ID";
        }

        /// <summary>Eine Tabelle der Stufe 1 mit dem Namen ihrer Projektspalte.</summary>
        internal sealed class Projekttabelle
        {
            internal Projekttabelle(string tabelle, string spalte) { Tabelle = tabelle; Spalte = spalte; }
            internal string Tabelle { get; }
            internal string Spalte { get; }
        }

        /// <summary>Eine Tabelle der Stufe 2 samt ihrem Weg nach oben.</summary>
        internal sealed class Folgetabelle
        {
            internal Folgetabelle(string tabelle, string spalte, string elternTabelle, string elternSpalte, int tiefe)
            {
                Tabelle = tabelle; Spalte = spalte; ElternTabelle = elternTabelle;
                ElternSpalte = elternSpalte; Tiefe = tiefe;
            }
            internal string Tabelle { get; }
            internal string Spalte { get; }
            internal string ElternTabelle { get; }
            internal string ElternSpalte { get; }
            internal int Tiefe { get; }
        }
    }
}
