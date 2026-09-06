using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Parameter des Verwendungskatalogs samt dem Wert des gewaehlten Katalogsatzes
    /// (Anwenderwunsch W14a-E-8, Teil 2).
    /// </summary>
    /// <param name="Eintrag">Spalte, Beschriftung, Einheit und Verwendung — aus
    /// <see cref="ParameterVerwendung"/>.</param>
    /// <param name="Wert">
    /// Der fertige ANZEIGETEXT in der Kultur des Anwenders;
    /// <see cref="ParameterVerwendung.LEER"/>, wenn die Spalte NULL fuehrt.
    /// </param>
    public sealed record Parameterwert(ParameterEintrag Eintrag, string Wert);

    /// <summary>
    /// <b>Die Werte hinter dem Verwendungskatalog</b> (Anwenderwunsch W14a-E-8 vom
    /// 06.09.2026): EIN Lesevorgang je Katalogsatz, der alle Spalten der Stammtabelle
    /// holt und sie mit <see cref="ParameterVerwendung"/> zu Anzeigezeilen paart.
    ///
    /// <para><b>Warum ein eigener Weg und kein neuer Detail-Lader je Katalog.</b> Die
    /// sieben <c>…StammCtrl.KatalogsatzAnzeige</c> liefern genau die Felder, die ihr
    /// Bearbeiten-Formular zeigt — das ist ihre Aufgabe und soll so bleiben. Die
    /// Uebersicht will das Gegenteil: ALLES, auch was kein Formular fuehrt. Sieben
    /// Lader um je zehn Spalten zu erweitern hiesse, dieselbe Erweiterung siebenmal zu
    /// pflegen; hier steht sie einmal, und der Katalog sagt, welche Spalten es sind.</para>
    ///
    /// <para><b><c>SELECT *</c> mit Bedacht.</b> Der Spaltensatz einer Stammtabelle
    /// waechst mit den Migrationsschritten (die Testdatenbank steht auf Stand 61, das
    /// Programm auf 64). Eine namentliche Spaltenliste liesse die Uebersicht auf einer
    /// aelteren Datenbank mit „no such column" scheitern — genau die Ueberlegung, die
    /// <c>StromspeicherSimCtrl</c> (:1046) und <c>ProjektDetails</c> (:118) schon
    /// treffen. Fehlt eine Spalte, fehlt eben ihre Zeile.</para>
    ///
    /// <para><b>Die Zahlen sehen aus wie im Katalogdialog</b> — roh in der Kultur des
    /// Anwenders, ohne Tausenderpunkt; NULL wird zum Halbgeviertstrich. Damit liest,
    /// wer Editor und Uebersicht nebeneinanderlegt, dieselben Ziffern (Regel aus
    /// <c>PhotovoltaikStammCtrl.Parameterzeilen</c>, W6-E-1).</para>
    /// </summary>
    public static class ParameterUebersichtCtrl
    {
        /// <summary>
        /// Alle Parameter einer Anlagenart mit den Werten des Satzes
        /// <paramref name="bezeichner"/>. Ein leerer Bezeichner oder ein unbekannter
        /// Satz liefert dieselbe Liste mit lauter
        /// <see cref="ParameterVerwendung.LEER"/> — die Uebersicht zeigt dann, WAS es
        /// gibt, nur eben ohne Werte.
        /// </summary>
        /// <param name="art">Welcher der sieben Kataloge.</param>
        /// <param name="bezeichner">Der gewaehlte Katalogsatz.</param>
        /// <param name="text">
        /// Uebersetzer fuer die Beschriftungsschluessel; <c>null</c> liefert den
        /// Schluessel selbst (Muster <see cref="KatalogBrowserProfil.Finde"/>).
        /// </param>
        public static IReadOnlyList<Parameterwert> Werte(Anlagenart art, string bezeichner,
                                                         Func<string, string> text = null)
        {
            IReadOnlyList<ParameterEintrag> katalog = ParameterVerwendung.Katalog(art, text);
            DataRow zeile = Satz(art, bezeichner);

            var liste = new List<Parameterwert>(katalog.Count);
            foreach (ParameterEintrag e in katalog)
                liste.Add(new Parameterwert(e, Anzeige(zeile, e.Spalte)));
            return liste;
        }

        /// <summary>
        /// Der Katalogsatz zum Bezeichner; <c>null</c>, wenn es ihn nicht gibt oder die
        /// Datenbank schweigt.
        /// </summary>
        /// <remarks>
        /// <c>ORDER BY ID</c> macht die Wahl bei einem doppelt vergebenen Bezeichner
        /// benennbar — dieselbe Regel wie <c>PhotovoltaikStammCtrl.Detail</c>.
        /// </remarks>
        private static DataRow Satz(Anlagenart art, string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return null;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + ParameterVerwendung.Stammtabelle(art) + "] " +
                    "WHERE Bezeichner = ? ORDER BY ID",
                    new DbParam("@bez", bezeichner));
                return (dt == null || dt.Rows.Count == 0) ? null : dt.Rows[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Der Anzeigetext einer Spalte. NULL, fehlende Spalte und leerer Text werden
        /// zum Halbgeviertstrich; eine 0 bleibt eine 0 — sie ist eine Aussage.
        /// </summary>
        /// <remarks>
        /// <b>Zwei Spalten sind WAHRHEITSWERTE</b> und stehen in SQLite als 0/1:
        /// <c>Brennwert</c> (nur der Heizkessel) und <c>ReadOnly</c> (alle sieben).
        /// „1" waere hier keine Auskunft; sie werden zu Ja/Nein aus demselben
        /// Ressourcenpaar, das jede Rueckfrage des Hauses benutzt.
        /// </remarks>
        private static string Anzeige(DataRow zeile, string spalte)
        {
            if (zeile == null) return ParameterVerwendung.LEER;
            if (!zeile.Table.Columns.Contains(spalte)) return ParameterVerwendung.LEER;

            object wert = zeile[spalte];
            if (wert == null || wert == DBNull.Value) return ParameterVerwendung.LEER;

            if (IstJaNein(spalte)) return Wahrheit(wert);

            if (wert is double || wert is float || wert is decimal)
                return Convert.ToDouble(wert, CultureInfo.InvariantCulture)
                              .ToString(CultureInfo.CurrentCulture);

            if (wert is int || wert is long || wert is short || wert is byte)
                return Convert.ToInt64(wert, CultureInfo.InvariantCulture)
                              .ToString(CultureInfo.CurrentCulture);

            string s = Convert.ToString(wert, CultureInfo.CurrentCulture);
            return string.IsNullOrWhiteSpace(s) ? ParameterVerwendung.LEER : s;
        }

        /// <summary>Fuehrt die Spalte einen Wahrheitswert? Siehe <see cref="Anzeige"/>.</summary>
        private static bool IstJaNein(string spalte)
        {
            return string.Equals(spalte, "Brennwert", StringComparison.OrdinalIgnoreCase)
                || string.Equals(spalte, "ReadOnly", StringComparison.OrdinalIgnoreCase);
        }

        private static string Wahrheit(object wert)
        {
            bool ja;
            if (wert is bool b) ja = b;
            else
            {
                long zahl;
                ja = long.TryParse(Convert.ToString(wert, CultureInfo.InvariantCulture),
                                   NumberStyles.Integer, CultureInfo.InvariantCulture, out zahl) && zahl != 0;
            }
            return ja ? MyResource.Resource.ALLG_BTN_JA : MyResource.Resource.ALLG_BTN_NEIN;
        }
    }
}
