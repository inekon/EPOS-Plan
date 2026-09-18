using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DER STAND DER BETRIEBSSEITE (U31) — was über und unter dem Positionsraster
    /// der Betriebskosten steht: der Laufstand, aus dem die Mengen stammen, und die
    /// Endenergie je Komponente, an der sich die Prozentzeilen bemessen.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Beides ist Fachauskunft: Der
    /// Laufstand beantwortet „woher kommen die Mengen", die Endenergiegruppe
    /// beantwortet „woran bemisst sich ‚% der Endenergiekosten'". Gerechnet wird
    /// nichts Neues — die Mengen liefert <see cref="EndenergieAufloeser"/>, den
    /// Zeitpunkt führt <c>Tab_Ergebnis.Zeitstempel</c>. Hier steht nur, welche
    /// Zeilen die Betriebsseite zeigt und wie sie heißen; die Hülle reicht durch.</para>
    ///
    /// <para><b>Keine stillen Nullen.</b> Fehlt der Arbeitspreis eines beteiligten
    /// Trägers, liefert der Auflöser keine Kosten — dann steht in der Spalte ein
    /// Gedankenstrich, nie eine 0, die sich wie ein gerechneter Wert liest.</para>
    /// </summary>
    internal static class KostenBetriebsstand
    {
        /// <summary>Der Gedankenstrich für eine Größe, die es nicht gibt.</summary>
        private const string STRICH = "—";

        /// <summary>Jahressuffix der Kostenspalte („€/a").</summary>
        private const string EURO_JAHR = DbWerte.KOSTEN_EINHEIT_EURO + "/a";

        // =====================================================================
        // Der Laufstand über dem Raster
        // =====================================================================

        /// <summary>
        /// Die Zeile über dem Raster der Betriebsseite: aus welchem Simulationslauf
        /// die Mengen stammen.
        ///
        /// <para>Drei Fälle, in dieser Reihenfolge: Es gibt ein gespeichertes
        /// Ergebnis MIT Zeitpunkt (<c>KDLG_LAUFSTAND</c> samt Datum), es gibt eines
        /// OHNE Zeitpunkt (<c>KDLG_LAUFSTAND_OHNE_DATUM</c> — eine Zeile aus einer
        /// Datenbank, die den Stempel noch nicht gesetzt hat), oder es gibt keines
        /// (<c>KDLG_BASIS_GRUND_LAUF</c>, „kein Simulationslauf" — derselbe Grund,
        /// den auch die Zeilen ohne Bezugsgröße nennen).</para>
        /// </summary>
        /// <param name="idProjekt">Das Projekt; 0 oder kleiner = kein Projektmodus,
        /// die Zeile entfällt.</param>
        internal static string Laufstand(int idProjekt)
        {
            if (idProjekt <= 0) return "";

            DateTime? stand;
            if (!ErgebnisStand(idProjekt, out stand))
                return MyResource.Resource.KDLG_BASIS_GRUND_LAUF;

            return stand.HasValue
                ? string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.KDLG_LAUFSTAND,
                                stand.Value.ToString("g", CultureInfo.CurrentCulture))
                : MyResource.Resource.KDLG_LAUFSTAND_OHNE_DATUM;
        }

        /// <summary>
        /// Der jüngste Lauf des Projekts. Rückgabe <c>false</c> = kein Ergebnis;
        /// <paramref name="zeitpunkt"/> ist <c>null</c>, wenn die Zeile keinen
        /// Zeitstempel führt. Dieselbe Abfrage wie in
        /// <c>TechnikPlanwertCtrl</c> — jüngster Lauf ist der mit der höchsten Id.
        /// </summary>
        private static bool ErgebnisStand(int idProjekt, out DateTime? zeitpunkt)
        {
            zeitpunkt = null;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Zeitstempel FROM Tab_Ergebnis WHERE ID_Projekt = ? " +
                    "ORDER BY ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return false;

                DataRow r = dt.Rows[0];
                if (r["ID"] == DBNull.Value || Convert.ToInt32(r["ID"]) <= 0) return false;
                if (r.Table.Columns.Contains("Zeitstempel") && r["Zeitstempel"] != DBNull.Value)
                    zeitpunkt = Convert.ToDateTime(r["Zeitstempel"]);
                return true;
            }
            catch { return false; }
        }

        // =====================================================================
        // Endenergie je Komponente
        // =====================================================================

        /// <summary>EINE Zeile der Gruppe „Endenergie je Komponente" — fertig gesetzt.</summary>
        internal sealed class Endenergiezeile
        {
            /// <summary>Komponente und Anlagenbezeichner („BHKW — Modul 1").</summary>
            public string Komponente = "";

            /// <summary>Jahresbedarf, gesetzt („4.342.100 kWh").</summary>
            public string BedarfText = "";

            /// <summary>Arbeitskosten, gesetzt („312.631,20 €/a") oder Gedankenstrich.</summary>
            public string KostenText = "";

            /// <summary>Woher die Menge stammt — <c>EndenergieAufloeser.Groesse.Basis</c>.</summary>
            public string Basis = "";

            /// <summary>Der Jahresbedarf als Zahl [kWh/a] (Prüfhilfe, keine Anzeige).</summary>
            public double BedarfKwh;

            /// <summary>Die Arbeitskosten als Zahl [€/a]; <c>null</c> = kein Preis.</summary>
            public double? KostenEuro;
        }

        /// <summary>
        /// Die Endenergie jeder Anlage des Projekts, die eine führt — Blockheizkraftwerk
        /// und Heizkessel über den Brennstoff, die Wärmepumpe über den Strom.
        ///
        /// <para>Photovoltaik, Solarthermie und die Speicher haben keine Endenergie
        /// (Konzept § 4.5: dort ist nur der feste Jahresbetrag zulässig); sie bekommen
        /// deshalb auch keine Zeile. Leere Liste heißt: nichts zu zeigen — kein Lauf,
        /// keine Anlage mit Endenergie oder kein Projekt.</para>
        /// </summary>
        internal static List<Endenergiezeile> Endenergie(int idProjekt)
        {
            var liste = new List<Endenergiezeile>();
            if (idProjekt <= 0) return liste;

            EndenergieAufloeser aufloeser = EndenergieAufloeser.FuerProjekt(idProjekt);
            if (aufloeser == null) return liste;

            Dictionary<string, int> komponentenId = KomponentenNachName();

            List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen;
            try { anlagen = ProjektEnergietraegerCtrl.AnlagenMitTraeger(idProjekt); }
            catch { return liste; }

            foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
            {
                int id;
                if (a == null || !komponentenId.TryGetValue(a.Komponente ?? "", out id)) continue;

                EndenergieAufloeser.Groesse g;
                try { g = aufloeser.FuerPosition(id, a.AnlageId); }
                catch { continue; }
                if (g == null) continue;

                liste.Add(new Endenergiezeile
                {
                    Komponente = string.IsNullOrEmpty(a.Bezeichner)
                        ? a.Komponente
                        : a.Komponente + " — " + a.Bezeichner,
                    BedarfKwh = g.BedarfKwh,
                    KostenEuro = g.KostenEuro,
                    BedarfText = g.BedarfKwh.ToString("#,##0", CultureInfo.CurrentCulture) +
                                 " " + DbWerte.EINHEIT_KWH,
                    KostenText = g.KostenEuro.HasValue
                        ? g.KostenEuro.Value.ToString("#,##0.00", CultureInfo.CurrentCulture) +
                          " " + EURO_JAHR
                        : STRICH,
                    Basis = g.Basis ?? ""
                });
            }

            return liste;
        }

        /// <summary>Komponentenname → <c>Tab_KostenKomponente.ID</c>; leer bei Fehlern.</summary>
        private static Dictionary<string, int> KomponentenNachName()
        {
            var karte = new Dictionary<string, int>(StringComparer.Ordinal);
            try
            {
                foreach (KeyValuePair<int, string> k in KostenVorlagenCtrl.Komponenten())
                    if (!string.IsNullOrEmpty(k.Value)) karte[k.Value] = k.Key;
            }
            catch { }
            return karte;
        }
    }
}
