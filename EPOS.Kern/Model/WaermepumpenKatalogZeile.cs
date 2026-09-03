using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zeile des WAERMEPUMPEN-KATALOGS (iU9-W7.0b) — der Stammsatz aus
    /// <c>Tab_WP_STAMM</c>, angereichert um kleinsten und groessten Vorlauf aus
    /// <c>Tab_Kenndaten_STAMM</c>.
    ///
    /// <para><b>Herkunft.</b> Im Bestand hiess dieser Typ <c>WPData</c> und stand
    /// zusammen mit <c>WPDataCtrl</c> AM ENDE der Formulardatei
    /// <c>Views\Wärmepumpe\Form_WPFilterAuswahl.cs</c> — eine Datenklasse und ein
    /// Controller mitten im Oberflaechencode. Beide sind mit der Umstellung des
    /// Katalogdialogs hierher gewandert: der Typ hierhin, das Lesen nach
    /// <see cref="WPStammCtrl"/>, das Filtern nach
    /// <see cref="WaermepumpenKatalogFilter"/>.</para>
    ///
    /// <para><b>Warum ein Record und kein <c>WPModel</c>.</b> Der Katalog zeigt sieben
    /// Spalten und filtert ueber neun Merkmale; <c>WPModel</c> traegt daneben den
    /// ganzen Geraetesatz samt Masse und Gewicht. Ausserdem sind zwei der Merkmale
    /// GERECHNET (<see cref="MinVorlauf"/>/<see cref="MaxVorlauf"/> aus den Kennlinien,
    /// <see cref="Auslegung"/> aus der Kuehlleistung) und stehen so in keiner Tabelle.</para>
    ///
    /// <para><b><see cref="Auslegung"/> ist ein deutscher Literaltext</b> — „Heizen" bzw.
    /// „Heizen/Kühlen", woertlich aus <c>WPDataCtrl.ReadAll</c>. Er ist zugleich
    /// Anzeigetext und Filterwert; die Vorlaeufermaske war nicht lokalisiert. Er bleibt
    /// unuebersetzt, weil er aus DATEN entsteht und mit ihnen verglichen wird —
    /// dieselbe Lage wie bei <c>DbWerte.WP_BETRIEBSART_*</c> (Abweichung A-3 des
    /// Protokolls W7).</para>
    /// </summary>
    /// <param name="Hersteller">Firma des Stammsatzes.</param>
    /// <param name="Bezeichnung">Bezeichner (Modellname) — die Suchspalte.</param>
    /// <param name="Bauart">Bauart des Geraets.</param>
    /// <param name="Aufstellung">Aufstellungsart.</param>
    /// <param name="MaxVorlauf">Groesster Vorlauf aus den Kennlinien [°C]; 0 ohne Kennlinien.</param>
    /// <param name="MinVorlauf">Kleinster Vorlauf aus den Kennlinien [°C]; 0 ohne Kennlinien.</param>
    /// <param name="MaxLeistung">Nennleistung [kW].</param>
    /// <param name="ElZuheizung">Leistung des elektrischen Heizstabs [kW].</param>
    /// <param name="Funktionsprinzip">Typ (Sole-Wasser, Luft-Wasser …).</param>
    /// <param name="Regelung">Leistungsstufen (einstufig, zweistufig, stetig).</param>
    /// <param name="Auslegung">„Heizen" oder „Heizen/Kühlen" (siehe Klassenkommentar).</param>
    public sealed record WaermepumpenKatalogZeile(
        string Hersteller,
        string Bezeichnung,
        string Bauart,
        string Aufstellung,
        double MaxVorlauf,
        double MinVorlauf,
        double MaxLeistung,
        double ElZuheizung,
        string Funktionsprinzip,
        string Regelung,
        string Auslegung)
    {
        /// <summary>Der Wert der Spalte „Auslegung", wenn die Waermepumpe kuehlen kann.</summary>
        public const string AUSLEGUNG_HEIZEN_KUEHLEN = "Heizen/Kühlen";

        /// <summary>Der Wert der Spalte „Auslegung" ohne Kuehlbetrieb.</summary>
        public const string AUSLEGUNG_HEIZEN = "Heizen";
    }
}
