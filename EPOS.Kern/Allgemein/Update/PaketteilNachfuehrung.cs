using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Frühere Stände der ausgelieferten Paketzeilen</b> (Umsetzungskonzept Zapfprofilgenerator,
    /// Nachtrag N34 (d); Auftrag A2, Entscheid E-A2-4): EINE Regel, die eine Nutzungsart des freien
    /// Paketteils, die unter einem früheren Bezeichner oder mit einer früheren Bezugsart gespeichert
    /// oder geliefert ist, als die heutige Zeile liest — dieselbe Zeile, kein zweiter Eintrag.
    ///
    /// <para><b>Warum.</b> Der natürliche Schlüssel einer Nutzungsart ist (Bezeichner,
    /// Katalogversion), und der Katalogimport ändert und löscht keine vorhandene Zeile. Eine
    /// Datenbank, die den Paketteil unter dem früheren Namen trägt, bekäme beim erneuten Einspielen
    /// eine zweite Zeile neben der alten. Die Regel führt deshalb die gespeicherte Zeile am Platz nach
    /// (dieselbe <c>ID</c>, Name und Bezugsart) und liest eine eingehende Zeile eines älteren Pakets
    /// vor dem Dublettenscan als die heutige — dann trifft der natürliche Schlüssel.</para>
    ///
    /// <para><b>Wer sie anwendet.</b> Der Schemaschritt <see cref="TwwBezugsartSchema"/> an den
    /// gespeicherten Zeilen (<see cref="Nachfuehren"/>); der Katalogimport
    /// (<c>TwwNutzungsartCtrl.Importieren</c>) an jeder Zeile des Pakets vor dem Dublettenscan; der
    /// Projektimport (<c>ProjektExportImportCtrl</c>) an den mitreisenden Katalogköpfen vor der Suche
    /// über den natürlichen Schlüssel; die Auslieferungsvorlage am externen Katalogpaket; das
    /// Validierungswerkzeug beim Katalogbau. Alle rufen <see cref="Finden"/>. Die Saatliste
    /// <c>UMBENANNTE_NUTZUNGSARTEN</c> in <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c>
    /// spiegelt die Einträge für die Testdatenbank.</para>
    ///
    /// <para><b>Die Grenze.</b> Nur Zeilen des ausgelieferten Paketteils: Status
    /// <c>AUSLIEFERUNG</c> (in der Datenbank zudem <c>ReadOnly = 1</c>) und die Provenienz des
    /// Paketteils in der Wertgruppe Bedarf — Version <see cref="VERSION_PAKETTEIL"/> und die
    /// Herkunftsart des Eintrags —, dazu Bezeichner und Bezugsart des früheren Stands. Die
    /// Katalogversion taugt als Grenze nicht: Der Paketteil führt keine, seine Zeilen treten der
    /// Katalogversion des Katalogs bei, in den sie kommen (in der Vorlage die des zuletzt
    /// angelegten Parameters, sonst <c>FREI-1</c>; in der Testdatenbank <c>TEST-1</c>). Eine
    /// Anwenderzeile gleichen Namens — Status <c>EIGEN</c> oder <c>IMPORT</c>, in welcher
    /// Katalogversion auch immer — bleibt unberührt.</para>
    /// </summary>
    internal static class PaketteilNachfuehrung
    {
        /// <summary>
        /// Die Provenienzversion des freien Paketteils (<c>Bedarf_Version</c>,
        /// <c>Jahresgang_Version</c>, <c>Wochengang_Version</c> seiner Zeilen, Datei
        /// <c>Referenzlaeufe/Katalogpaket_frei/Tab_TwwNutzungsart_STAMM.csv</c>).
        /// </summary>
        internal const string VERSION_PAKETTEIL = "FREI-1";

        /// <summary>
        /// Ein früherer Stand einer Paketzeile und der heutige: Bezeichner und Bezugsart vorher und
        /// nachher, dazu die Herkunftsart der Wertgruppe Bedarf, die die Zeile des Paketteils trägt.
        /// </summary>
        internal sealed record Eintrag(string FruehererBezeichner, ZapfBezugsart FruehereBezugsart,
                                       string Bezeichner, ZapfBezugsart Bezugsart, Herkunftsart Herkunft)
        {
            /// <summary>Ändert der Eintrag den Namen (sonst allein die Bezugsart)?</summary>
            internal bool Umbenannt => !string.Equals(FruehererBezeichner, Bezeichner, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Die Einträge</b>, der älteste Stand zuerst. Der Hoteltyp des Paketteils hieß bis #579
        /// „Hotel (aus Messung)" und trug bis A2 die Bezugsart Betten; heute heißt er „Hotel (aus
        /// Messung, je Zimmer)" mit der Bezugsart Zimmer. Werte, Kalender und Tagesgangsatz sind in
        /// allen Ständen dieselben.
        /// </summary>
        internal static readonly IReadOnlyList<Eintrag> NUTZUNGSARTEN = new[]
        {
            new Eintrag("Hotel (aus Messung)", ZapfBezugsart.Betten,
                        "Hotel (aus Messung, je Zimmer)", ZapfBezugsart.Zimmer, Herkunftsart.Eigenkonstruktion),
            new Eintrag("Hotel (aus Messung, je Zimmer)", ZapfBezugsart.Betten,
                        "Hotel (aus Messung, je Zimmer)", ZapfBezugsart.Zimmer, Herkunftsart.Eigenkonstruktion),
        };

        /// <summary>
        /// <b>Trifft die Regel eine Zeile?</b> Die Angaben so, wie die Zeile sie trägt (Texte der
        /// Tabelle bzw. des Pakets): Bezeichner, Bezugsart, Status, Version und Herkunftsart der
        /// Wertgruppe Bedarf. Liefert den Eintrag oder <c>null</c> — dann bleibt die Zeile, wie sie ist.
        /// </summary>
        internal static Eintrag Finden(string bezeichner, long? bezugsart, string status,
                                       string bedarfVersion, string bedarfHerkunftsart)
        {
            if (!bezugsart.HasValue
                || !string.Equals((status ?? "").Trim(), TwwSchema.STATUS_AUSLIEFERUNG, StringComparison.Ordinal)
                || !string.Equals((bedarfVersion ?? "").Trim(), VERSION_PAKETTEIL, StringComparison.Ordinal))
                return null;
            string name = (bezeichner ?? "").Trim();
            string herkunft = (bedarfHerkunftsart ?? "").Trim();
            foreach (Eintrag e in NUTZUNGSARTEN)
                if (string.Equals(e.FruehererBezeichner, name, StringComparison.Ordinal)
                    && (long)e.FruehereBezugsart == bezugsart.Value
                    && string.Equals(TwwWertemengen.Text(e.Herkunft), herkunft, StringComparison.Ordinal))
                    return e;
            return null;
        }

        // =================================================================================
        // Die gespeicherten Zeilen (Schemaschritt)
        // =================================================================================

        /// <summary>
        /// Die gespeicherten Zeilen eines Eintrags samt der Frage, ob die Katalogversion den heutigen
        /// Namen schon an einer ANDEREN Zeile führt (<c>Konflikt</c> = 1). Platzhalter der Reihe nach:
        /// heutiger Bezeichner, früherer Bezeichner, frühere Bezugsart, Status, Version, Herkunftsart.
        /// </summary>
        private const string SQL_TREFFER =
            "SELECT \"ID\", \"Katalogversion\", " +
            "EXISTS (SELECT 1 FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" AS \"Andere\" " +
            "WHERE \"Andere\".\"Bezeichner\" = ? " +
            "AND \"Andere\".\"Katalogversion\" = \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\".\"Katalogversion\" " +
            "AND \"Andere\".\"ID\" <> \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\".\"ID\") AS \"Konflikt\" " +
            "FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" " +
            "WHERE \"Bezeichner\" = ? AND \"Bezugsart\" = ? AND \"Status\" = ? AND \"ReadOnly\" = 1 " +
            "AND \"Bedarf_Version\" = ? AND \"Bedarf_Herkunftsart\" = ? ORDER BY \"ID\"";

        /// <summary>Die Zahl der gespeicherten Zeilen eines Eintrags (Platzhalter wie <see cref="SQL_TREFFER"/> ohne den ersten).</summary>
        private const string SQL_OFFEN =
            "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" " +
            "WHERE \"Bezeichner\" = ? AND \"Bezugsart\" = ? AND \"Status\" = ? AND \"ReadOnly\" = 1 " +
            "AND \"Bedarf_Version\" = ? AND \"Bedarf_Herkunftsart\" = ?";

        /// <summary>Name und Bezugsart am Platz: dieselbe ID, dieselben Werte, Kategorien und Zonenbezüge.</summary>
        private const string SQL_NACHFUEHREN =
            "UPDATE \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" SET \"Bezeichner\" = ?, \"Bezugsart\" = ? WHERE \"ID\" = ?";

        /// <summary>Allein die Bezugsart — wenn die Katalogversion den heutigen Namen schon an einer anderen Zeile führt.</summary>
        private const string SQL_NUR_BEZUGSART =
            "UPDATE \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" SET \"Bezugsart\" = ? WHERE \"ID\" = ?";

        /// <summary>
        /// <b>Führt die gespeicherten Zeilen nach</b> (Schemaschritt <see cref="TwwBezugsartSchema.SCHRITT"/>),
        /// im laufenden Vorgang <paramref name="v"/>: Jede Zeile, die ein Eintrag trifft, bekommt am
        /// Platz den heutigen Bezeichner und die heutige Bezugsart. Führt ihre Katalogversion den
        /// heutigen Namen schon an einer anderen Zeile (etwa eine eingespielte Dublette), bleibt der
        /// Name und nur die Bezugsart wird nachgeführt — benannt im Bericht, nie still. Die
        /// Prüfklausel der Bezugsart muss die heutige Bezugsart schon zulassen (der Schritt baut die
        /// Tabelle vorher um). Liefert die Zahl der nachgeführten Zeilen; wiederholbar.
        /// </summary>
        internal static int Nachfuehren(DbVorgang v, IList<string> bericht)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            int zahl = 0;
            foreach (Eintrag e in NUTZUNGSARTEN)
            {
                DataTable dt = v.Lese(SQL_TREFFER,
                    new DbParam("@heute", e.Bezeichner), new DbParam("@frueher", e.FruehererBezeichner),
                    new DbParam("@bezug", (int)e.FruehereBezugsart), new DbParam("@status", TwwSchema.STATUS_AUSLIEFERUNG),
                    new DbParam("@version", VERSION_PAKETTEIL), new DbParam("@herkunft", TwwWertemengen.Text(e.Herkunft)));
                if (dt == null) continue;
                foreach (DataRow r in dt.Rows)
                {
                    long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                    string version = Convert.ToString(r["Katalogversion"], CultureInfo.InvariantCulture);
                    bool konflikt = Convert.ToInt64(r["Konflikt"], CultureInfo.InvariantCulture) != 0;
                    string ort = TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " ID " + id.ToString(CultureInfo.InvariantCulture) +
                                 " (Katalogversion " + version + ")";
                    if (e.Umbenannt && konflikt)
                    {
                        v.Ausfuehren(SQL_NUR_BEZUGSART, new DbParam("@bezug", (int)e.Bezugsart), new DbParam("@id", id));
                        Notiere(bericht, ort + ": \"" + e.FruehererBezeichner + "\" bleibt unter seinem Namen, weil die " +
                                         "Katalogversion \"" + e.Bezeichner + "\" schon fuehrt; Bezugsart " +
                                         e.FruehereBezugsart + " -> " + e.Bezugsart);
                    }
                    else
                    {
                        v.Ausfuehren(SQL_NACHFUEHREN, new DbParam("@name", e.Bezeichner),
                                     new DbParam("@bezug", (int)e.Bezugsart), new DbParam("@id", id));
                        Notiere(bericht, ort + ": " +
                                         (e.Umbenannt ? "\"" + e.FruehererBezeichner + "\" -> \"" + e.Bezeichner + "\", "
                                                      : "\"" + e.Bezeichner + "\": ") +
                                         "Bezugsart " + e.FruehereBezugsart + " -> " + e.Bezugsart);
                    }
                    zahl++;
                }
            }
            return zahl;
        }

        /// <summary>
        /// Wie viele gespeicherte Zeilen trifft die Regel noch? 0 ohne die Tabelle. Braucht die
        /// Spalten der Provenienz und des Status (Schritt T1) — sie stehen seit der Tabelle selbst.
        /// </summary>
        internal static long Offen()
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)) return 0;
            long offen = 0;
            foreach (Eintrag e in NUTZUNGSARTEN)
            {
                object o = DataRepository.ExecuteScalar(SQL_OFFEN,
                    new DbParam("@frueher", e.FruehererBezeichner), new DbParam("@bezug", (int)e.FruehereBezugsart),
                    new DbParam("@status", TwwSchema.STATUS_AUSLIEFERUNG), new DbParam("@version", VERSION_PAKETTEIL),
                    new DbParam("@herkunft", TwwWertemengen.Text(e.Herkunft)));
                if (o != null && o != DBNull.Value) offen += Convert.ToInt64(o, CultureInfo.InvariantCulture);
            }
            return offen;
        }

        private static void Notiere(IList<string> bericht, string zeile)
        {
            if (bericht != null) bericht.Add(zeile);
        }
    }
}
