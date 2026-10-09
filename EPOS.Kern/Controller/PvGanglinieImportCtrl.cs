using System.Globalization;
using System.IO;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie der Import einer PV-Ganglinie ausgegangen ist — EIN Ergebnis statt mehrerer
    /// Meldungsfenster (Muster <see cref="SolarganglinieImportBericht"/>).
    /// </summary>
    public sealed class PvGanglinieImportBericht
    {
        /// <summary>Steht die Ganglinie im Katalog?</summary>
        public bool Erfolgreich;

        /// <summary>Ist die <see cref="Meldung"/> ein Fehler? Sonst ein Hinweis (etwa „schon vorhanden").</summary>
        public bool IstFehler;

        /// <summary>Der Bezeichner (der Dateiname ohne Endung).</summary>
        public string Bezeichner = "";

        /// <summary>Die Beschreibung — der Text der Kopfzeile.</summary>
        public string Beschreibung = "";

        /// <summary>Das Raster des Katalogsatzes in Minuten (60 oder 15); 0 ohne Lesung.</summary>
        public int RasterMinuten;

        /// <summary>Die neue Katalog-ID; 0 ohne Erfolg.</summary>
        public int IdStamm;

        /// <summary>Der fertige Text für den Misserfolg; leer bei Erfolg.</summary>
        public string Meldung = "";

        /// <summary>Die Protokollzeile (Format, Anzahl, Raster, Jahresarbeit, Spitze).</summary>
        public string Protokoll = "";

        /// <summary>Die gelesene Reihe samt Format und Kennzahlen.</summary>
        public StundenganglinieLesung Lesung = new StundenganglinieLesung();
    }

    /// <summary>
    /// <b>Der Import einer PV-Ganglinie in den Katalog</b> (<c>Tab_PvGanglinie_STAMM</c>,
    /// <c>Tab_PvGanglinieDaten_STAMM</c>).
    ///
    /// <para><b>Die Kette:</b> Lesen mit Formaterkennung über <see cref="StundenganglinieDatei"/>
    /// (Trennzeichen, Dezimalzeichen, Kopfzeile, Zeitstempel, laufende Nummer; 8 760 oder 35 040
    /// Werte), der Bezeichner aus dem Dateinamen, die Beschreibung aus der Kopfzeile, die
    /// Dublettenprüfung und das Schreiben in EINER Transaktion. <b>Das Raster der Datei bleibt:</b>
    /// Eine Viertelstundenreihe wird mit ihren 35 040 Werten gespeichert
    /// (<see cref="StundenganglinieLesung.WerteImDateirasterKw"/>), nicht gemittelt.</para>
    /// </summary>
    public static class PvGanglinieImportCtrl
    {
        /// <summary>Liest, prüft den Namen und schreibt. Wirft nicht.</summary>
        /// <param name="pfad">Die Quelldatei.</param>
        /// <param name="nennleistungKwp">Die Nennleistung der Anlage [kWp]; <c>null</c> = nicht bekannt.</param>
        public static PvGanglinieImportBericht Einlesen(string pfad, double? nennleistungKwp = null)
        {
            var bericht = new PvGanglinieImportBericht
            {
                Bezeichner = string.IsNullOrEmpty(pfad) ? "" : (Path.GetFileNameWithoutExtension(pfad) ?? "")
            };
            if (string.IsNullOrWhiteSpace(pfad))
            {
                bericht.Meldung = MyResource.Resource.IMP_TXT_KEIN_PFAD;
                bericht.IstFehler = true;
                return bericht;
            }

            StundenganglinieLesung lesung = StundenganglinieDatei.Lies(pfad);
            bericht.Lesung = lesung;
            if (!lesung.Erfolgreich)
            {
                bericht.Meldung = GanglinienProtokollText.Text(lesung.ErsterFehler);
                bericht.IstFehler = true;
                return bericht;
            }

            bericht.RasterMinuten = lesung.Raster == GanglinienRaster.Viertelstunde
                ? PvGanglinieWeiche.RASTER_VIERTEL
                : PvGanglinieWeiche.RASTER_STUNDE;
            bericht.Protokoll = SolarganglinieImportCtrl.Protokolltext(lesung);
            bericht.Beschreibung = lesung.Kopftext;

            if (PvGanglinieStammCtrl.Vorhanden(bericht.Bezeichner))
            {
                bericht.Meldung = MyResource.Resource.PVG_MSG_VORHANDEN;
                return bericht;
            }

            bericht.IdStamm = PvGanglinieStammCtrl.Importieren(bericht.Bezeichner, bericht.Beschreibung,
                                                               bericht.RasterMinuten, nennleistungKwp,
                                                               lesung.WerteImDateirasterKw);
            bericht.Erfolgreich = bericht.IdStamm > 0;
            bericht.IstFehler = !bericht.Erfolgreich;
            if (!bericht.Erfolgreich) bericht.Meldung = MyResource.Resource.PVG_MSG_SCHREIBFEHLER;
            return bericht;
        }
    }
}
