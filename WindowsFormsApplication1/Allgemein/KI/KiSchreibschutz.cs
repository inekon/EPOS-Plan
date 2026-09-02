using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Schreibschutz-Waechter der Schreibaktionen (Fachkonzept 4.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Assistent umgeht NIE einen Schreibschutz</b> - auch nicht mit Bestaetigung.
    /// <c>SchreibschutzUebergehen</c> (<c>BHKWStammCtrl.cs:157</c>) wird an keiner Stelle
    /// des Assistenten gesetzt; wer einen geschuetzten Satz aendern will, nimmt den
    /// Fachdialog. Diese Klasse ist die Stelle, an der das nachweisbar wird.
    /// </para>
    /// <para>
    /// <b>Zwei Sperren, nicht eine.</b>
    /// <see cref="IstKatalogtabelle"/> weist Katalogtabellen der Auslieferung pauschal ab
    /// - Katalogpflege ist gar nicht erst deklariert (Fachkonzept 1.2), und diese Wache
    /// haelt das auch dann durch, wenn spaeter jemand eine Aktion nachtraegt.
    /// <see cref="Gesperrt"/> prueft den EINZELNEN Satz auf das Feld <c>ReadOnly</c>.
    /// </para>
    /// <para>
    /// <b>Schematolerant wie der Bestand.</b> Nicht jede Tabelle fuehrt <c>ReadOnly</c>.
    /// Geprueft wird deshalb ueber <c>DataTable.Columns.Contains("ReadOnly")</c> - genau
    /// das Muster, mit dem <c>BHKWStammCtrl</c>, <c>GebaeudeStammCtrl</c>,
    /// <c>ProzesswaermeCtrl</c> und die uebrigen den Wert lesen. Fuehrt die Tabelle das
    /// Feld nicht, gibt es dort auch keinen Schreibschutz - und die Wache greift, sobald
    /// eine Migration das Feld nachtraegt, ohne dass hier etwas zu aendern waere.
    /// </para>
    /// <para>
    /// <b>Im Zweifel abweisen.</b> Laesst sich der Satz nicht lesen, gilt er als gesperrt.
    /// Eine Wache, die bei einer Ausnahme durchwinkt, ist keine Wache.
    /// </para>
    /// <para>
    /// <b>Warum oeffentlich.</b> Damit der Aktionsharnisch dieselbe Wache pruefen kann,
    /// die auch das Programm benutzt - und nicht eine nachgebaute zweite.
    /// </para>
    /// </remarks>
    public static class KiSchreibschutz
    {
        /// <summary>Namensendung der Auslieferungskataloge.</summary>
        public const string KATALOG_ENDUNG = "_STAMM";

        /// <summary>Ist das eine Katalogtabelle der Auslieferung?</summary>
        public static bool IstKatalogtabelle(string tabelle)
        {
            return !string.IsNullOrEmpty(tabelle) &&
                   tabelle.EndsWith(KATALOG_ENDUNG, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Klartextgrund, warum in diesen Satz nicht geschrieben werden darf;
        /// <c>null</c> heisst: nichts spricht dagegen.
        /// </summary>
        /// <param name="tabelle">Zieltabelle, z. B. <c>Tab_ProjektWerte</c>.</param>
        /// <param name="idSpalte">Name der Schluesselspalte, z. B. <c>ID</c>.</param>
        /// <param name="id">Schluessel des Satzes.</param>
        public static string Gesperrt(string tabelle, string idSpalte, int id)
        {
            if (string.IsNullOrEmpty(tabelle) || string.IsNullOrEmpty(idSpalte)) return null;

            if (IstKatalogtabelle(tabelle))
                return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SchutzKatalog, tabelle);

            DataTable dt;
            try
            {
                dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + tabelle + "] WHERE [" + idSpalte + "] = ? LIMIT 1",
                    new DbParam("@id", (Int32)id));
            }
            catch (Exception ex)
            {
                return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SchutzUnpruefbar,
                                     tabelle, id, ex.Message);
            }

            // Kein Satz: dafuer ist die Vorbedingung der Aktion zustaendig, nicht die Wache.
            if (dt == null || dt.Rows.Count == 0) return null;

            // Die Tabelle fuehrt keinen Schreibschutz - dann gibt es hier nichts zu holen.
            if (!dt.Columns.Contains("ReadOnly")) return null;

            object wert = dt.Rows[0]["ReadOnly"];
            if (wert == null || wert == DBNull.Value) return null;

            bool geschuetzt;
            try { geschuetzt = Convert.ToBoolean(wert, CultureInfo.InvariantCulture); }
            catch { return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SchutzUnpruefbar,
                                         tabelle, id, Convert.ToString(wert) ?? ""); }

            return geschuetzt
                ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SchutzSatz, tabelle, id)
                : null;
        }

        /// <summary>
        /// Fuehrt diese Tabelle ueberhaupt ein Feld <c>ReadOnly</c>? Nur fuer den
        /// Aktionsharnisch - er soll berichten koennen, welcher Zweig geprueft wurde.
        /// </summary>
        public static bool FuehrtSchreibschutz(string tabelle)
        {
            if (string.IsNullOrEmpty(tabelle)) return false;
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] LIMIT 1");
                return dt != null && dt.Columns.Contains("ReadOnly");
            }
            catch { return false; }
        }
    }
}
