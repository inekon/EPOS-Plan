using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Der Prüfhinweis zur Nennleistung (<see cref="PvGanglinieImportCtrl.Pruefhinweis"/>); "" ohne.
        /// Er hält den Import nicht auf und steht zusätzlich am Ende der <see cref="Protokoll"/>zeile.
        /// </summary>
        public string Hinweis = "";
        /// <summary>Die gelesene Reihe samt Format und Kennzahlen.</summary>
        public StundenganglinieLesung Lesung = new StundenganglinieLesung();
    }

    /// <summary>
    /// Die Vorbelegung der Nennleistung vor dem Import einer PV-Ganglinie
    /// (<see cref="PvGanglinieImportCtrl.Vorschlagen"/>).
    /// </summary>
    public sealed class PvGanglinieVorschlag
    {
        /// <summary>Die vorgeschlagene Nennleistung [kWp]; <c>null</c> ohne lesbare Datei.</summary>
        public double? VorschlagKwp;
        /// <summary>true = aus dem Dateikopf, false = der Höchstwert der Reihe.</summary>
        public bool AusDateikopf;
        /// <summary>Der Höchstwert der Reihe im Raster der Datei [kW]; 0 ohne lesbare Datei.</summary>
        public double SpitzeKw;
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
            bericht.Hinweis = Pruefhinweis(lesung.SpitzeKw, nennleistungKwp);
            if (bericht.Hinweis.Length > 0) bericht.Protokoll = (bericht.Protokoll + " " + bericht.Hinweis).Trim();
            bericht.IstFehler = !bericht.Erfolgreich;
            if (!bericht.Erfolgreich) bericht.Meldung = MyResource.Resource.PVG_MSG_SCHREIBFEHLER;
            return bericht;
        }
    
        /// <summary>
        /// Die Toleranz der Nennleistungsprüfung: Der Spitzenwert der Reihe darf die Nennleistung um
        /// 10 % übersteigen (Wechselrichter mit DC/AC-Reserve, kalte klare Tage), erst darüber gibt es
        /// einen Hinweis.
        /// </summary>
        public const double NENNLEISTUNG_TOLERANZ = 1.1;

        /// <summary>
        /// <b>Der Prüfhinweis zur Nennleistung</b>: Liegt die Spitze der Reihe über Nennleistung × 1,1,
        /// passt entweder die Nennleistung oder die Einheit der Datei (W statt kW) nicht. "" ohne
        /// Nennleistung oder ohne Auffälligkeit. Der Hinweis hält den Import nicht auf.
        /// </summary>
        public static string Pruefhinweis(double spitzeKw, double? nennleistungKwp)
        {
            if (!nennleistungKwp.HasValue || nennleistungKwp.Value <= 0) return "";
            if (!(spitzeKw > nennleistungKwp.Value * NENNLEISTUNG_TOLERANZ)) return "";
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVG_IMP_HINWEIS_SPITZE,
                                 spitzeKw, nennleistungKwp.Value);
        }

        /// <summary>
        /// <b>Die Vorbelegung der Nennleistung</b> vor dem Import: nennt der Dateikopf eine Leistung in kWp
        /// („… 9,8 kWp …"), gilt sie, sonst der Höchstwert der Reihe im Raster der Datei. Liest die Datei
        /// einmal; wirft nicht. Ohne lesbare Datei ist <see cref="PvGanglinieVorschlag.VorschlagKwp"/> <c>null</c>.
        /// </summary>
        public static PvGanglinieVorschlag Vorschlagen(string pfad)
        {
            var v = new PvGanglinieVorschlag();
            if (string.IsNullOrWhiteSpace(pfad)) return v;
            StundenganglinieLesung lesung;
            try { lesung = StundenganglinieDatei.Lies(pfad); }
            catch { return v; }
            if (lesung == null || !lesung.Erfolgreich) return v;

            v.SpitzeKw = lesung.SpitzeKw;
            double? kopf = NennleistungAusKopf(lesung.Kopftext);
            if (kopf.HasValue)
            {
                v.VorschlagKwp = kopf.Value;
                v.AusDateikopf = true;
            }
            else if (lesung.SpitzeKw > 0)
            {
                v.VorschlagKwp = Math.Round(lesung.SpitzeKw, 2);
            }
            return v;
        }

        /// <summary>
        /// Die Nennleistung aus einem Kopftext: die erste Zahl unmittelbar vor „kWp" (Komma oder Punkt als
        /// Dezimalzeichen, ohne Tausendertrennzeichen); <c>null</c>, wenn keine dasteht oder sie nicht
        /// positiv ist.
        /// </summary>
        public static double? NennleistungAusKopf(string kopftext)
        {
            if (string.IsNullOrEmpty(kopftext)) return null;
            Match m = Regex.Match(kopftext, @"(\d+(?:[.,]\d+)?)\s*kWp", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            double wert;
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out wert)) return null;
            return wert > 0 ? wert : (double?)null;
        }
    }
}
