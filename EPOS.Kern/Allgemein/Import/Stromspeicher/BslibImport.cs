using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Zerleger der Battery Storage Library <c>bslib</c></b> — Quelle 1 des
    /// <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c> (Anwenderwunsch
    /// <b>W13‑E‑2</b>, 07.09.2026).
    ///
    /// <para><b>Herkunft.</b> <c>bslib</c> baut seine Datenbank aus der
    /// PerMod-Datenbank der <b>HTW Berlin</b> und der <b>Stromspeicher-Inspektion</b>;
    /// gepflegt wird das Paket vom Forschungszentrum Jülich (IEK‑3). Der Quelltext
    /// steht unter MIT, die erzeugte CSV-Datei laut LIESMICH unter
    /// <b>CC BY 4.0</b> — sie darf mit Namensnennung weitergegeben werden.</para>
    ///
    /// <para><b>Gemessen am 07.09.2026 (Fassung 0.7, 18.01.2023):</b> Die
    /// ausgelieferte <c>bslib_database.csv</c> führt <b>7</b> Zeilen — zwei
    /// AC-gekoppelte Systeme, drei DC-gekoppelte und zwei reine
    /// PV-Wechselrichter. Davon tragen <b>4</b> eine nutzbare Kapazität und sind
    /// damit Speicher im Sinne von <c>Tab_Stromspeicher_STAMM</c>; die beiden
    /// Wechselrichter und ein DC-System ohne Kapazitätsangabe fallen weg. Es ist
    /// also keine Geräteliste, sondern ein kleiner, VERMESSENER Satz — genau
    /// darin liegt sein Wert: Er ist die einzige der vier Quellen mit
    /// Standby-Verbräuchen.</para>
    ///
    /// <para><b>Ohne Python lesbar.</b> Die Datei ist eine gewöhnliche CSV mit
    /// Komma und Punkt; das Python-Paket ist nur ihr Transportweg
    /// (<c>pip download bslib</c> oder der Klon des Repositorys). Der Kern liest
    /// die Datei direkt.</para>
    /// </summary>
    public sealed class BslibImport
    {
        /// <summary>Ohne diese Spalten ist die Datei keine bslib-Datenbank.</summary>
        internal static readonly string[] PFLICHTSPALTEN =
        {
            "id", "manufacturer (pe)", "model (pe)", "type [-coupled]",
            "e_bat_usable [kwh]", "eta_bat", "p_bat2ac_out [w]"
        };

        private readonly List<StromspeicherImportSatz> _saetze = new List<StromspeicherImportSatz>();

        /// <summary>Die gelesenen Speicher in Dateireihenfolge.</summary>
        public IReadOnlyList<StromspeicherImportSatz> Saetze => _saetze;

        /// <summary>
        /// Zeilen, die die Datei führt, die aber KEIN Speicher sind — reine
        /// PV-Wechselrichter und Systeme ohne Kapazitätsangabe. Ihre Kennungen
        /// stehen hier, damit das Protokoll sie benennen kann statt sie
        /// stillschweigend zu verlieren.
        /// </summary>
        public IReadOnlyList<string> Uebergangen => _uebergangen;

        private readonly List<string> _uebergangen = new List<string>();

        /// <summary>Liest eine <c>bslib_database.csv</c>.</summary>
        public (bool Erfolg, SpeicherImportMeldung Meldung) AusDatei(string pfad)
        {
            _saetze.Clear();
            _uebergangen.Clear();

            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad))
                return (false, new SpeicherImportMeldung("SPIMP_MSG_DATEI_FEHLT", pfad ?? ""));

            try
            {
                List<string[]> zeilen =
                    StromspeicherImportSatz.TabelleAusText(StromspeicherImportSatz.LiesText(pfad));
                return AusTabelle(zeilen, Path.GetFileName(pfad));
            }
            catch (Exception ex)
            {
                return (false, new SpeicherImportMeldung("SPIMP_MSG_FEHLER", ex.Message));
            }
        }

        /// <summary>Der Zerleger ohne Datei — für die Prüfung.</summary>
        internal (bool Erfolg, SpeicherImportMeldung Meldung) AusTabelle(List<string[]> zeilen, string quelle)
        {
            if (zeilen == null || zeilen.Count < 2)
                return (false, new SpeicherImportMeldung("SPIMP_MSG_LEER"));

            var spalte = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < zeilen[0].Length; i++)
            {
                string name = StromspeicherImportSatz.Kopfname(zeilen[0][i]);
                if (name.Length > 0 && !spalte.ContainsKey(name)) spalte[name] = i;
            }

            List<string> fehlend = PFLICHTSPALTEN.Where(s => !spalte.ContainsKey(s)).ToList();
            if (fehlend.Count > 0)
                return (false, new SpeicherImportMeldung("SPIMP_MSG_KOPFZEILE", string.Join(", ", fehlend)));

            int iId = spalte["id"];
            int iHerstellerPe = spalte["manufacturer (pe)"];
            int iModellPe = spalte["model (pe)"];
            int iHerstellerBat = Spalte(spalte, "manufacturer (bat)");
            int iModellBat = Spalte(spalte, "model (bat)");
            int iTyp = spalte["type [-coupled]"];
            int iEnergie = spalte["e_bat_usable [kwh]"];
            int iEta = spalte["eta_bat"];
            int iEntladen = spalte["p_bat2ac_out [w]"];
            int iSoc1Ac = Spalte(spalte, "p_sys_soc1_ac [w]");
            int iSoc1Dc = Spalte(spalte, "p_sys_soc1_dc [w]");
            int iSoc0Ac = Spalte(spalte, "p_sys_soc0_ac [w]");
            int iSoc0Dc = Spalte(spalte, "p_sys_soc0_dc [w]");

            for (int z = 1; z < zeilen.Count; z++)
            {
                string[] f = zeilen[z];
                string Feld(int idx) => (idx >= 0 && idx < f.Length && f[idx] != null) ? f[idx].Trim() : "";

                string kennung = Feld(iId);
                if (kennung.Length == 0) continue;

                // Ein reiner PV-Wechselrichter ist kein Speicher; er gehoert in den
                // Wechselrichterkatalog und hat dort seinen eigenen Import.
                if (Feld(iTyp).Equals(TOPOLOGIE_PVINV, StringComparison.OrdinalIgnoreCase))
                {
                    _uebergangen.Add(kennung);
                    continue;
                }

                double energie = StromspeicherImportSatz.Zahl(Feld(iEnergie));
                if (!(energie > 0.0)) { _uebergangen.Add(kennung); continue; }

                _saetze.Add(new StromspeicherImportSatz
                {
                    Hersteller = Feld(iHerstellerPe),
                    Modell = Geraetename(Feld(iModellPe), Feld(iHerstellerBat), Feld(iModellBat)),

                    // bslib fuehrt KEINE Zellchemie - weder in der CSV noch in der
                    // PerMod-Vorlage. Der Typ bleibt deshalb leer, statt geraten zu
                    // werden; die Spalte ist Freitext und darf leer sein.
                    Technologie = "",

                    EnergieKwh = energie,
                    LeistungKw = StromspeicherImportSatz.Zahl(Feld(iEntladen)) / 1000.0,
                    WirkungsgradRt = StromspeicherImportSatz.WirkungsgradAusText(Feld(iEta)),
                    StandbyW = Standby(StromspeicherImportSatz.Zahl(Feld(iSoc1Ac)),
                                       StromspeicherImportSatz.Zahl(Feld(iSoc1Dc)),
                                       StromspeicherImportSatz.Zahl(Feld(iSoc0Ac)),
                                       StromspeicherImportSatz.Zahl(Feld(iSoc0Dc))),
                    Quelle = quelle ?? ""
                });
            }

            if (_saetze.Count == 0)
                return (false, new SpeicherImportMeldung("SPIMP_MSG_KEINE_SAETZE"));

            return (true, new SpeicherImportMeldung("SPIMP_MSG_GELADEN",
                _saetze.Count.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>Der Wert der Spalte <c>Type [-coupled]</c> für einen reinen PV-Wechselrichter.</summary>
        internal const string TOPOLOGIE_PVINV = "PVINV";

        /// <summary>
        /// Der Gerätename. Bei einem DC-gekoppelten System ist das Gerät das PAAR
        /// aus Leistungselektronik und Batterie — bslib führt beide getrennt, und
        /// erst zusammen ergeben sie den Speicher, den EPOS-Plan rechnet. Ohne
        /// benannte Batterie bleibt der Name der Leistungselektronik allein.
        /// </summary>
        internal static string Geraetename(string modellPe, string herstellerBat, string modellBat)
        {
            string batterie = string.Join(" ", new[] { herstellerBat, modellBat }
                                  .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (batterie.Length == 0) return modellPe ?? "";
            if (string.IsNullOrWhiteSpace(modellPe)) return batterie;
            return modellPe.Trim() + " / " + batterie;
        }

        /// <summary>
        /// Der Standby-Verbrauch [W] aus den VIER Messwerten von bslib.
        ///
        /// <para>Gemessen wird bei bslib je Betriebszustand (voll = SOC1, leer =
        /// SOC0) und je Seite (AC und DC); <c>Tab_Stromspeicher_STAMM</c> führt
        /// EINE Zahl. Genommen wird die Summe beider Seiten im UNGÜNSTIGEREN der
        /// zwei Zustände — der Wert, den ein Datenblatt als „Standby" nennen würde.
        /// Bei den DC-Systemen ist das der leere Zustand (S3: 4,47 + 4,56 = 9,03 W
        /// gegen 0,15 W im vollen), bei den AC-Systemen der volle (S2: 14,9 + 0,1 =
        /// 15,0 W gegen 12,1 W). Ein Mittelwert verschwiege genau den Fall, den
        /// eine Wirtschaftlichkeitsrechnung sehen muss.</para>
        /// </summary>
        internal static double Standby(double soc1Ac, double soc1Dc, double soc0Ac, double soc0Dc)
        {
            double voll = Math.Max(0.0, soc1Ac) + Math.Max(0.0, soc1Dc);
            double leer = Math.Max(0.0, soc0Ac) + Math.Max(0.0, soc0Dc);
            return Math.Max(voll, leer);
        }

        private static int Spalte(Dictionary<string, int> karte, string name)
        {
            int i;
            return karte.TryGetValue(name, out i) ? i : -1;
        }
    }
}
