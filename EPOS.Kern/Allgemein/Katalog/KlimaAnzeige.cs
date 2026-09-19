using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Wie eine Klimaregion ihre Herkunft SAGT</b> (Auftrag KL-6,
    /// Anwenderentscheid 19.09.2026: „Wird die Quelle und Auswahl (z. B. TRY 2045
    /// sommerwarm …) der Klimadaten angezeigt? Diese sollte auch bei der Klimaregion
    /// sichtbar sein.").
    ///
    /// <para><b>EINE Stelle für drei Anzeigen.</b> Die Regionsliste des
    /// Klimadaten-Dialogs, die Herkunftszeile der Startseite und die Einträge des
    /// Klimaregion-Auswahlfeldes sagen dasselbe — nur unterschiedlich lang. Stünde
    /// der Satzbau dreimal da, stünde er dreimal anders; hier steht er einmal.</para>
    ///
    /// <para><b>Schlüssel hinein, Text heraus.</b> Was in der Datenbank steht, sind
    /// die sprachneutralen Schlüssel aus <see cref="DbWerte"/>
    /// (<c>Tab_Klimaregion(_STAMM).Quelle</c> und <c>.Szenario</c>) und eine Zahl
    /// (<c>.Bezugsjahr</c>); was hier herauskommt, ist der Text der eingestellten
    /// Sprache. Die Übersetzung liegt im Kern, weil der Kern schon die Ressourcen
    /// führt (<c>MyResource</c>) und weil <c>KlimaImportAblauf</c> denselben Satz
    /// längst in den Freitext <c>Details</c> schreibt — zwei Wege zu einem Satz wären
    /// einer zu viel.</para>
    ///
    /// <para><b>Leer heißt leer.</b> Jede Angabe kann fehlen: Der Altbestand vor
    /// Schemaschritt 95/97 sagt gar nichts, PVGIS kennt keine TRY-Szenarien, und der
    /// Kopf einer TRY-Datei nennt das Bezugsjahr dieses Hauses nicht. Was fehlt, wird
    /// WEGGELASSEN — nie durch eine Vorgabe ersetzt. Fehlt alles, ist das Ergebnis
    /// die leere Zeichenfolge; die Katalogliste macht daraus ihren Halbgeviertstrich
    /// (<see cref="ParameterVerwendung.LEER"/>).</para>
    /// </summary>
    public static class KlimaAnzeige
    {
        /// <summary>Das Trennzeichen der langen Fassung — dasselbe, mit dem
        /// <c>Tab_Klimaregion_STAMM.Details</c> seine Glieder reiht.</summary>
        public const string TRENNER = " \u00b7 ";

        /// <summary>
        /// <b>Die lange Fassung</b> für die Spalte „Quelle" der Regionsliste und für
        /// die Herkunftszeile der Startseite: „TRY-Regionaldaten (Deutschland) · 2045
        /// · sommerwarm".
        ///
        /// <para>Ohne Szenario und Jahr bleibt es bei der Quelle allein — genau das,
        /// was vor Schemaschritt 97 dastand.</para>
        /// </summary>
        /// <param name="quelle">Schlüssel aus <c>Tab_Klimaregion(_STAMM).Quelle</c>.</param>
        /// <param name="szenario">Schlüssel aus <c>.Szenario</c>; leer erlaubt.</param>
        /// <param name="bezugsjahr">Wert aus <c>.Bezugsjahr</c>; <c>null</c> erlaubt.</param>
        public static string Quellenzeile(string quelle, string szenario, int? bezugsjahr)
        {
            return Reihen(TRENNER, Quellentext(quelle), Jahrtext(bezugsjahr), Szenariotext(szenario));
        }

        /// <summary>
        /// <b>Die kurze Fassung</b> für einen Eintrag des Auswahlfeldes: „TRY 2045
        /// sommerwarm" bzw. „PVGIS".
        ///
        /// <para>Sie nennt die Quelle mit ihrem KÜRZEL, weil sie in eine Klammer
        /// hinter den Regionsnamen passen muss; welche der beiden TRY-Quellen es war,
        /// sagt die Liste im Klimadaten-Dialog.</para>
        /// </summary>
        public static string Kurzform(string quelle, string szenario, int? bezugsjahr)
        {
            return Reihen(" ", Quellenkuerzel(quelle), Jahrtext(bezugsjahr), Szenariotext(szenario));
        }

        /// <summary>
        /// <b>Ein Eintrag des Klimaregion-Auswahlfeldes</b>: „hagelloch (TRY 2045
        /// sommerwarm)", „München (PVGIS)" — und bei einer Region ohne jede Angabe
        /// der blanke Name.
        /// </summary>
        public static string Eintrag(string name, string quelle, string szenario, int? bezugsjahr)
        {
            string bezeichner = (name ?? "").Trim();
            string kurz = Kurzform(quelle, szenario, bezugsjahr);

            if (kurz.Length == 0) return bezeichner;

            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.START_KLIMA_EINTRAG, bezeichner, kurz);
        }

        /// <summary>Der Anzeigetext eines Quellenschlüssels; ein unbekannter bleibt leer.</summary>
        public static string Quellentext(string schluessel)
        {
            switch ((schluessel ?? "").Trim())
            {
                case DbWerte.KLIMA_QUELLE_PVGIS: return MyResource.Resource.KLIMA_QUELLE_PVGIS;
                case DbWerte.KLIMA_QUELLE_TRY_DATEI: return MyResource.Resource.KLIMA_QUELLE_TRY_DATEI;
                case DbWerte.KLIMA_QUELLE_TRY_REGIONAL: return MyResource.Resource.KLIMA_QUELLE_TRY_REGIONAL;
                default: return "";
            }
        }

        /// <summary>
        /// Das KÜRZEL eines Quellenschlüssels („PVGIS", „TRY"); ein unbekannter bleibt
        /// leer. Beide TRY-Wege tragen dasselbe Kürzel — in einer Klammer hinter dem
        /// Regionsnamen zählt, WELCHES Wetterjahr gemeint ist, nicht, aus welcher
        /// Datei es kam.
        /// </summary>
        public static string Quellenkuerzel(string schluessel)
        {
            switch ((schluessel ?? "").Trim())
            {
                case DbWerte.KLIMA_QUELLE_PVGIS: return MyResource.Resource.KLIMA_QUELLE_KURZ_PVGIS;
                case DbWerte.KLIMA_QUELLE_TRY_DATEI:
                case DbWerte.KLIMA_QUELLE_TRY_REGIONAL: return MyResource.Resource.KLIMA_QUELLE_KURZ_TRY;
                default: return "";
            }
        }

        /// <summary>Der Anzeigetext eines Szenarioschlüssels; ein unbekannter bleibt
        /// leer — <b>nie</b> „mittleres Jahr" als Vorgabe.</summary>
        public static string Szenariotext(string schluessel)
        {
            switch ((schluessel ?? "").Trim())
            {
                case DbWerte.KLIMA_SZENARIO_MITTEL: return MyResource.Resource.KLIMA_TRY_SZ_MITTEL;
                case DbWerte.KLIMA_SZENARIO_SOMMERWARM: return MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM;
                case DbWerte.KLIMA_SZENARIO_WINTERKALT: return MyResource.Resource.KLIMA_TRY_SZ_WINTERKALT;
                default: return "";
            }
        }

        /// <summary>
        /// Das Bezugsjahr als Zahl OHNE Tausendertrennung — <c>InvariantCulture</c>:
        /// Eine Jahreszahl ist eine Jahreszahl, kein Betrag („2045", nie „2.045").
        /// <c>null</c> und 0 ergeben die leere Zeichenfolge.
        /// </summary>
        public static string Jahrtext(int? bezugsjahr)
        {
            if (!bezugsjahr.HasValue || bezugsjahr.Value == 0) return "";
            return bezugsjahr.Value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Reiht die nicht leeren Glieder mit dem Trennzeichen.</summary>
        private static string Reihen(string trenner, params string[] glieder)
        {
            var teile = new List<string>(glieder.Length);
            foreach (string g in glieder)
                if (!string.IsNullOrWhiteSpace(g)) teile.Add(g.Trim());

            return string.Join(trenner, teile);
        }
    }
}
