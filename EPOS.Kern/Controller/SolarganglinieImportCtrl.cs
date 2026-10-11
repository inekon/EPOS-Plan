using System;
using System.Globalization;
using System.IO;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie der Import einer Solarthermie-Ganglinie ausgegangen ist — EIN Ergebnis statt
    /// mehrerer Meldungsfenster; die Oberfläche macht daraus Statuszeile, Protokollzeile
    /// oder Warnband.
    /// </summary>
    public sealed class SolarganglinieImportBericht
    {
        /// <summary>Steht die Ganglinie im Katalog?</summary>
        public bool Erfolgreich;

        /// <summary>Ist die <see cref="Meldung"/> ein Fehler? Sonst ein Hinweis (etwa „schon vorhanden").</summary>
        public bool IstFehler;

        /// <summary>Der Bezeichner, unter dem sie steht oder stehen sollte (der Dateiname ohne Endung).</summary>
        public string Bezeichner = "";

        /// <summary>Die Beschreibung des Katalogsatzes — der Text der Kopfzeile.</summary>
        public string Beschreibung = "";

        /// <summary>Der fertige Text für den Misserfolg; leer bei Erfolg.</summary>
        public string Meldung = "";

        /// <summary>
        /// Die Protokollzeile: erkanntes Format, Anzahl Werte, Raster, Jahresarbeit und
        /// Spitze; leer, wenn die Datei nicht gelesen werden konnte.
        /// </summary>
        public string Protokoll = "";

        /// <summary>Die gelesene Reihe samt Format und Kennzahlen.</summary>
        public StundenganglinieLesung Lesung = new StundenganglinieLesung();
    }

    /// <summary>
    /// <b>Der Import einer Solarthermie-Ganglinie in den Katalog</b>
    /// (<c>Tab_Solarganglinie_STAMM</c>, <c>Tab_SolarganglinieDaten_STAMM</c>).
    ///
    /// <para><b>Die Kette:</b> Lesen mit Formaterkennung über
    /// <see cref="StundenganglinieDatei"/> (8 760 Stunden- oder 35 040 Viertelstundenwerte,
    /// Viertelstunden je Stunde gemittelt, weil die Tabelle mittlere Leistungen in kW je
    /// Stunde führt), der Bezeichner aus dem Dateinamen, die Beschreibung aus der
    /// Kopfzeile, die Dublettenprüfung gegen die Datenbank (<c>SGAD_MSG_VORHANDEN</c>) und
    /// das Schreiben in EINER Transaktion
    /// (<see cref="SolarganglinieStammCtrl.ImportGanglinie(string, string, System.Collections.Generic.IList{double})"/>).</para>
    ///
    /// <para><b>Der Bezeichner kommt aus dem Dateinamen</b>, nicht aus der Kopfzeile: Die
    /// Kopfzeile ist im einspaltigen Format die Beschreibung, und in einer CSV benennt
    /// sie die Spalte („Leistung [kW]"), nicht die Reihe.</para>
    /// </summary>
    public static class SolarganglinieImportCtrl
    {
        /// <summary>Liest, prüft den Namen und schreibt. Wirft nicht.</summary>
        /// <param name="pfad">Die Quelldatei.</param>
        public static SolarganglinieImportBericht Einlesen(string pfad)
            => Einlesen(pfad, string.IsNullOrWhiteSpace(pfad) ? null : StundenganglinieDatei.Lies(pfad));

        /// <summary>
        /// Prüft den Namen und schreibt eine schon gelesene Reihe — der Weg über den Optionendialog
        /// (<see cref="StundenganglinieDatei.AusImport"/>). Wirft nicht.
        /// </summary>
        /// <param name="pfad">Die Quelldatei (Bezeichner aus dem Dateinamen).</param>
        /// <param name="lesung">Die gelesene Reihe samt Format und Kopftext.</param>
        public static SolarganglinieImportBericht Einlesen(string pfad, StundenganglinieLesung lesung)
        {
            var bericht = new SolarganglinieImportBericht
            {
                Bezeichner = string.IsNullOrEmpty(pfad) ? "" : (Path.GetFileNameWithoutExtension(pfad) ?? "")
            };

            if (string.IsNullOrWhiteSpace(pfad))
            {
                bericht.Meldung = MyResource.Resource.IMP_TXT_KEIN_PFAD;
                bericht.IstFehler = true;
                return bericht;
            }

            lesung ??= new StundenganglinieLesung();
            bericht.Lesung = lesung;
            if (!lesung.Erfolgreich)
            {
                bericht.Meldung = StundenganglinieDatei.Ablehnungstext(pfad, lesung);
                bericht.IstFehler = true;
                return bericht;
            }

            bericht.Protokoll = Protokolltext(lesung);
            bericht.Beschreibung = lesung.Kopftext;

            var ctrl = new SolarganglinieStammCtrl();
            if (ctrl.Exists(bericht.Bezeichner))
            {
                bericht.Meldung = MyResource.Resource.SGAD_MSG_VORHANDEN;
                return bericht;
            }

            bool ok = ctrl.ImportGanglinie(bericht.Bezeichner, bericht.Beschreibung, lesung.StundenwerteKw);
            bericht.Erfolgreich = ok;
            bericht.IstFehler = !ok;
            if (!ok) bericht.Meldung = MyResource.Resource.SGAD_MSG_SCHREIBFEHLER;
            return bericht;
        }

        /// <summary>
        /// Die Protokollzeile einer gelesenen Reihe: Trennzeichen, Dezimalzeichen,
        /// Kopfzeile, Zeitstempel · Anzahl Werte und Raster · Jahresarbeit [MWh] · Spitze [kW].
        /// </summary>
        public static string Protokolltext(StundenganglinieLesung lesung)
        {
            if (lesung == null) return "";
            GanglinienImportOptionen f = lesung.Format;
            string trenn = f.Trennzeichen == '\0'
                ? MyResource.Resource.SGL_IMP_KEIN_TRENNZEICHEN
                : GanglinienDatei.TrennzeichenText(f.Trennzeichen);
            string raster = lesung.Raster == GanglinienRaster.Viertelstunde
                ? MyResource.Resource.SGL_IMP_RASTER_VIERTEL
                : MyResource.Resource.SGL_IMP_RASTER_STUNDE;

            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SGL_IMP_PROTOKOLL,
                trenn,
                f.Dezimaltrenner.ToString(),
                f.Kopfzeile ? MyResource.Resource.ALLG_BTN_JA : MyResource.Resource.ALLG_BTN_NEIN,
                f.ZeitSpalte >= 0 ? MyResource.Resource.ALLG_BTN_JA : MyResource.Resource.ALLG_BTN_NEIN,
                lesung.AnzahlWerte.ToString("N0", CultureInfo.CurrentCulture),
                raster,
                lesung.JahresarbeitMwh.ToString("N1", CultureInfo.CurrentCulture),
                lesung.SpitzeKw.ToString("N1", CultureInfo.CurrentCulture));
        }
    }
}
