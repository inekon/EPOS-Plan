using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Steuert den Spotpreisimport: Datei einlesen (<see cref="SpotpreisLeser"/>),
    /// Kalender aufbereiten (<see cref="SpotreihenAufbereitung"/>) und als Preisreihe
    /// ablegen (<see cref="PreisreiheCtrl"/>) - Fachkonzept Stromspeicher 4.1 a,
    /// Arbeitspaket AP4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Drei getrennte Schichten, mit Absicht.</b> Das Zerlegen der Datei kennt keine
    /// Datenbank, die Kalenderarithmetik kennt weder Datei noch Datenbank, und erst
    /// dieser Controller fuegt beides zusammen und schreibt. Damit haengt die
    /// Verifikation der beiden schwierigen Teile - Zeitzonenumstellung und Schaltjahr -
    /// nicht an der Oberflaeche (Umsetzungskonzept AP4, Verifikationspunkt 4).
    /// </para>
    /// <para>
    /// <b>Kein Abbruch bei Problemzeilen.</b> Eine unlesbare Zeile, eine Luecke oder ein
    /// Mehrfacheintrag beendet den Import nicht - sie stehen im Protokoll, und der
    /// Anwender entscheidet. Nur eine Datei ganz ohne brauchbare Zeile wird abgelehnt.
    /// </para>
    /// </remarks>
    public class SpotpreisImportCtrl
    {
        /// <summary>Ergebnis eines Importlaufs.</summary>
        public sealed class Lauf
        {
            /// <summary>Was der Dateileser gefunden hat.</summary>
            public SpotDateiErgebnis Datei;

            /// <summary>Was die Kalenderaufbereitung daraus gemacht hat.</summary>
            public SpotreihenErgebnis Reihe;

            /// <summary>Kalenderjahr der Datei.</summary>
            public int Jahr;

            /// <summary>Vergebene <c>Tab_Preisreihe.ID</c>; 0, wenn nicht gespeichert wurde.</summary>
            public int ID_Preisreihe;

            /// <summary>Das Validierungsprotokoll als fertiger Anzeigetext (zweisprachig).</summary>
            public string Protokoll = "";

            /// <summary>true, wenn eine brauchbare Reihe entstanden ist.</summary>
            public bool Erfolgreich;
        }

        /// <summary>
        /// Liest eine Datei und bereitet sie auf - OHNE zu speichern. Der Dialog zeigt
        /// damit das Validierungsprotokoll, bevor der Anwender die Ablage bestaetigt.
        /// </summary>
        /// <exception cref="ArgumentException">Wenn der Pfad leer ist.</exception>
        public Lauf Pruefe(string pfad)
        {
            Lauf lauf = new Lauf();

            lauf.Datei = SpotpreisLeser.LiesDatei(pfad);
            lauf.Jahr = lauf.Datei.Jahr;

            if (lauf.Datei.Zeilen.Count == 0)
            {
                lauf.Protokoll = MyResource.Resource.PREIS_IMPORT_KEINE_DATEN;
                return lauf;
            }

            lauf.Reihe = SpotreihenAufbereitung.AusStundenwerten(lauf.Datei.Zeilen);
            lauf.Protokoll = Protokolltext(lauf);
            lauf.Erfolgreich = lauf.Reihe.StundenreiheCtKwh.Length == RasterAdapter.StundenJahr;

            return lauf;
        }

        /// <summary>
        /// Speichert eine gepruefte Reihe als <c>Tab_Preisreihe</c>-Datensatz.
        /// </summary>
        /// <param name="lauf">Ergebnis von <see cref="Pruefe"/>.</param>
        /// <param name="bezeichner">Anzeigename der Reihe.</param>
        /// <param name="idProjekt">
        /// Projekt, dem die Reihe gehoert; 0 legt sie als STAMMREIHE ab, die allen
        /// Projekten zur Verfuegung steht (Fachkonzept 8.4).
        /// </param>
        /// <param name="fortschritt">Rueckmeldung je 1.000 geschriebener Werte, oder <c>null</c>.</param>
        /// <returns>Die vergebene ID, oder -1.</returns>
        public int Speichere(Lauf lauf, string bezeichner, int idProjekt, Action<int> fortschritt = null)
        {
            if (lauf == null) throw new ArgumentNullException(nameof(lauf));
            if (lauf.Reihe == null || !lauf.Erfolgreich)
                throw new InvalidOperationException("Es liegt keine gepruefte Reihe vor.");

            PreisreiheModel kopf = new PreisreiheModel();
            kopf.ID_Projekt = idProjekt;
            kopf.Bezeichner = string.IsNullOrEmpty(bezeichner)
                ? DbWerte.SP_PREISQUELLE_SPOTMARKT + " " + lauf.Jahr.ToString(CultureInfo.InvariantCulture)
                : bezeichner;
            kopf.Jahr = lauf.Jahr;
            kopf.Aufloesung = DbWerte.PREISREIHE_AUFLOESUNG_STUNDE;
            kopf.Einheit = DbWerte.PREISREIHE_EINHEIT_CT_KWH;

            int id = new PreisreiheCtrl().Insert(kopf, lauf.Reihe.StundenreiheCtKwh, fortschritt);
            lauf.ID_Preisreihe = id > 0 ? id : 0;
            return id;
        }

        // =================================================================
        // Validierungsprotokoll (Fachkonzept 4.1)
        // =================================================================

        /// <summary>
        /// Baut das Validierungsprotokoll aus den Zahlen der Engine. Die Engine liefert
        /// bewusst nur Befundarten und Kalenderstellen - der Text entsteht hier, damit
        /// er ueber <c>MyResource</c> zweisprachig bleibt (Drei-Schichten-Regel).
        /// </summary>
        public static string Protokolltext(Lauf lauf)
        {
            if (lauf == null || lauf.Reihe == null) return "";

            CultureInfo k = CultureInfo.CurrentCulture;
            SpotDateiErgebnis d = lauf.Datei;
            SpotreihenErgebnis r = lauf.Reihe;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_KOPF,
                                        lauf.Jahr, d.ZeilenGesamt));

            if (d.ZeilenUnlesbar > 0)
                sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_UNLESBAR,
                                            d.ZeilenUnlesbar, Zeilenliste(d.UnlesbareZeilen)));

            if (d.ZeilenFremdesJahr > 0)
                sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_FREMDES_JAHR,
                                            d.ZeilenFremdesJahr, lauf.Jahr));

            if (r.ZeilenSchaltjahr > 0)
                sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_SCHALTJAHR, r.ZeilenSchaltjahr));

            foreach (SpotBefund b in r.Befunde)
            {
                switch (b.Art)
                {
                    case SpotBefundArt.DoppelstundeGemittelt:
                        sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_DOPPELSTUNDE,
                                                    Datum(b), b.Stunde, b.Anzahl));
                        break;

                    case SpotBefundArt.FehlendeStundeErgaenzt:
                        sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_FEHLSTUNDE,
                                                    Datum(b), b.Stunde));
                        break;

                    case SpotBefundArt.StundeOhneWert:
                        sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_LUECKE,
                                                    Datum(b), b.Stunde));
                        break;

                    case SpotBefundArt.MehrfachEintrag:
                        sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_MEHRFACH,
                                                    Datum(b), b.Stunde, b.Anzahl));
                        break;
                }
            }

            sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_ERGEBNIS,
                                        r.StundenreiheCtKwh.Length,
                                        r.MinCtKwh.ToString("0.000", k),
                                        r.MaxCtKwh.ToString("0.000", k),
                                        r.MittelCtKwh.ToString("0.000", k)));

            if (r.NegativeWerte > 0)
                sb.AppendLine(string.Format(MyResource.Resource.PREIS_IMPORT_NEGATIV, r.NegativeWerte));

            sb.AppendLine(r.Vollstaendig
                ? MyResource.Resource.PREIS_IMPORT_VOLLSTAENDIG
                : MyResource.Resource.PREIS_IMPORT_UNVOLLSTAENDIG);

            return sb.ToString();
        }

        /// <summary>Kalenderstelle eines Befunds als "TT.MM." in der Oberflaechenkultur.</summary>
        private static string Datum(SpotBefund b)
        {
            return b.Tag.ToString("00", CultureInfo.CurrentCulture) + "." +
                   b.Monat.ToString("00", CultureInfo.CurrentCulture) + ".";
        }

        private static string Zeilenliste(List<int> zeilen)
        {
            if (zeilen == null || zeilen.Count == 0) return "";
            return string.Join(", ", zeilen.ConvertAll(z => z.ToString(CultureInfo.CurrentCulture)));
        }
    }
}
