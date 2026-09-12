using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Leser der PVsyst-<c>.OND</c>-Dateien</b> (Konzept Wechselrichter 5.2,
    /// Anwenderentscheid <b>W6‑O‑1</b> vom 06.09.2026: „der OND-Import soll umgesetzt
    /// werden").
    ///
    /// <para><b>Zwilling zu <see cref="PanDataService"/>, Zeile für Zeile.</b> Dasselbe
    /// Format (Abschnitte mit <c>Schlüssel=Wert</c>, geschachtelt über Einrückung),
    /// dieselbe Kodierung (ANSI/Windows-1252, <see cref="AnsiEncoding"/>), dasselbe
    /// Dezimalzeichen (Punkt) und dieselbe Lebensdauer der Sitzungsliste: Sie ist ein
    /// INSTANZFELD und stirbt mit dem Dialog (Lehre aus Befund W13‑B46 — statisch
    /// überlebte sie den Projektwechsel).</para>
    ///
    /// <para><b>Was eine <c>.OND</c> von einer <c>.PAN</c> unterscheidet</b> ist der
    /// Kennlinienblock: <c>ProfilPIO</c> ist eine eigene, eingerückte Wertetabelle
    /// (<c>Point_1 = P_in, P_out</c>) und kein einzelner Schlüssel. PVsyst führt sie in
    /// DREI Fassungen — untere, nominale und obere MPP-Spannung
    /// (<c>ProfilPIOV1/V2/V3</c>). <b>Genommen wird die NOMINALE</b> (Konzept 5.2); die
    /// anderen zwei brauchte erst ein spannungsabhängiges Modell (Stufe E3 des
    /// PV-Ertragsmodells, zurückgestellt). Welche Fassung es war, schreibt der Import in
    /// die Beschreibung des Katalogsatzes.</para>
    ///
    /// <para><b>Der Kern kennt keine Anzeigetexte.</b> Jede Rückmeldung ist ein
    /// SCHLÜSSEL mit Platzhalterwerten (<see cref="CecFortschritt"/>); der Wirt
    /// übersetzt — dieselbe Regel wie bei <see cref="CECDataService"/> und
    /// <see cref="CecWechselrichterDienst"/>.</para>
    /// </summary>
    public class OndWechselrichterDienst
    {
        /// <summary>Die Kopfzeile, an der eine Datei als Wechselrichter erkannt wird.</summary>
        internal const string OBJEKTMARKE = "pvGInverter";

        /// <summary>Die nominale Kennlinienfassung — die von dreien, die genommen wird.</summary>
        internal const string PROFIL_NOMINAL = "ProfilPIOV2";

        /// <summary>Die einzige Fassung, wenn die Datei nur eine führt.</summary>
        internal const string PROFIL_EINZELN = "ProfilPIO";

        private readonly List<OndWechselrichter> _geraete = new List<OndWechselrichter>();

        /// <summary>Die Geräte dieser Sitzung — mehrere <c>.OND</c>-Dateien nacheinander.</summary>
        public IReadOnlyList<OndWechselrichter> AlleGeraete => _geraete;

        /// <summary>Leert die Sitzungsliste.</summary>
        public void Leeren() => _geraete.Clear();

        /// <summary>
        /// Liest eine <c>.OND</c>-Datei und nimmt sie in die Sitzungsliste auf.
        /// </summary>
        /// <returns>
        /// Erfolg und die Rückmeldung als Schlüssel: <c>OND_MSG_GELESEN</c> mit der Zahl
        /// der Geräte, <c>OND_MSG_DATEI_FEHLT</c>, <c>OND_MSG_KEIN_GERAET</c> (die Datei
        /// ist keine Wechselrichterdatei) oder <c>OND_MSG_LESEFEHLER</c>.
        /// </returns>
        public (bool Erfolg, CecFortschritt Meldung) AusDatei(string pfad)
        {
            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad))
                return (false, new CecFortschritt("OND_MSG_DATEI_FEHLT"));

            try
            {
                string inhalt = File.ReadAllText(pfad, AnsiEncoding.Get());
                OndWechselrichter geraet = Zerlege(inhalt, Path.GetFileName(pfad));

                if (geraet == null || geraet.PNomConv <= 0.0)
                    return (false, new CecFortschritt("OND_MSG_KEIN_GERAET"));

                Aufnehmen(geraet);
                return (true, new CecFortschritt("OND_MSG_GELESEN",
                    _geraete.Count.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                return (false, new CecFortschritt("OND_MSG_LESEFEHLER", ex.Message));
            }
        }

        /// <summary>
        /// Nimmt ein gelesenes Gerät in die Sitzungsliste auf. Ein gleichnamiges
        /// (erneut eingelesene Datei) ERSETZT seinen Altbestand, statt die Liste doppelt
        /// zu füllen — wörtlich <see cref="PanDataService.Aufnehmen"/>.
        /// </summary>
        public void Aufnehmen(OndWechselrichter geraet)
        {
            if (geraet == null) return;

            string name = (geraet.Name ?? "").Trim();
            int idx = _geraete.FindIndex(g =>
                string.Equals((g.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));

            if (idx >= 0) _geraete[idx] = geraet;
            else _geraete.Add(geraet);
        }

        /// <summary>Die Hersteller der Sitzungsliste, aufsteigend und ohne Dubletten.</summary>
        public IEnumerable<string> Hersteller()
        {
            return _geraete.Select(g => g.Hersteller)
                           .Where(h => !string.IsNullOrEmpty(h))
                           .Distinct()
                           .OrderBy(h => h, StringComparer.CurrentCulture);
        }

        // =================================================================
        //  Der Zerleger
        // =================================================================

        /// <summary>
        /// Zerlegt den Text einer <c>.OND</c>-Datei. Statisch wie
        /// <see cref="PanDataService.ParsePan"/>: Sie zerlegt nur Text und gehört keiner
        /// Sitzung an.
        /// </summary>
        /// <param name="inhalt">Der Dateiinhalt, bereits als ANSI dekodiert.</param>
        /// <param name="dateiname">Dateiname — Rückfall für den Bezeichner.</param>
        /// <returns>
        /// Das Gerät; <c>null</c>, wenn der Text kein <c>pvGInverter</c>-Objekt ist.
        /// </returns>
        public static OndWechselrichter Zerlege(string inhalt, string dateiname = "")
        {
            if (string.IsNullOrEmpty(inhalt)) return null;
            if (inhalt.IndexOf(OBJEKTMARKE, StringComparison.OrdinalIgnoreCase) < 0) return null;

            var g = new OndWechselrichter
            {
                Quelldatei = Path.GetFileNameWithoutExtension(dateiname ?? "")
            };

            // Die Kennlinienfassungen werden ALLE eingesammelt; gewaehlt wird erst am
            // Ende - eine Datei nennt V2 nicht zwingend als erste.
            var profile = new Dictionary<string, List<(double PIn, double POut)>>(
                StringComparer.OrdinalIgnoreCase);

            bool imHandel = false;
            string offenesProfil = null;

            foreach (string rohzeile in (inhalt ?? "").Split('\n'))
            {
                string zeile = (rohzeile ?? "").Trim();
                if (zeile.Length == 0) continue;

                // --- Blockgrenzen ---------------------------------------
                if (zeile.StartsWith("PVObject_Commercial", StringComparison.Ordinal))
                { imHandel = true; continue; }

                if (zeile.StartsWith("End of PVObject", StringComparison.Ordinal))
                { imHandel = false; continue; }

                if (zeile.StartsWith("End of TCubicProfile", StringComparison.Ordinal))
                { offenesProfil = null; continue; }

                int gleich = zeile.IndexOf('=');
                if (gleich <= 0) continue;

                string schluessel = zeile.Substring(0, gleich).Trim();
                string wert = zeile.Substring(gleich + 1).Trim();

                // --- Ein Kennlinienblock beginnt ------------------------
                if (IstProfilmarke(schluessel) &&
                    wert.StartsWith("TCubicProfile", StringComparison.OrdinalIgnoreCase))
                {
                    offenesProfil = schluessel;
                    if (!profile.ContainsKey(schluessel))
                        profile[schluessel] = new List<(double, double)>();
                    continue;
                }

                // --- Ein Wertepaar IM Kennlinienblock -------------------
                if (offenesProfil != null &&
                    schluessel.StartsWith("Point_", StringComparison.OrdinalIgnoreCase))
                {
                    string[] paar = wert.Split(',');
                    if (paar.Length >= 2)
                        profile[offenesProfil].Add((Zahl(paar[0]), Zahl(paar[1])));
                    continue;
                }

                // Sonstige Schluessel im Profilblock (NPtsMax, Mode, LastCompile ...)
                // gehen den Katalog nichts an.
                if (offenesProfil != null) continue;

                if (imHandel)
                {
                    switch (schluessel)
                    {
                        case "Manufacturer": g.Manufacturer = wert; break;
                        case "Model": g.Model = wert; break;
                        case "Comment": g.Comment = wert; break;
                        case "DataSource": g.DataSource = wert; break;
                        case "YearBeg": g.YearBeg = Ganzzahl(wert); break;
                    }
                    continue;
                }

                switch (schluessel)
                {
                    // Leistungen [kW]
                    case "PNomConv": g.PNomConv = Zahl(wert); break;
                    case "PMaxOUT": g.PMaxOUT = Zahl(wert); break;
                    case "PNomDC": g.PNomDC = Zahl(wert); break;
                    case "PMaxDC": g.PMaxDC = Zahl(wert); break;

                    // Schwellen [W]
                    case "PSeuil": g.PSeuil = Zahl(wert); break;
                    case "Pnight":
                    case "PNight":
                    case "Night_Loss": g.Pnight = Zahl(wert); break;

                    // Eingang [V] / [A]
                    case "VMppMin": g.VMppMin = Zahl(wert); break;
                    case "VMppNom": g.VMppNom = Zahl(wert); break;
                    case "VMPPMax":
                    case "VMppMax": g.VMPPMax = Zahl(wert); break;
                    case "VAbsMax": g.VAbsMax = Zahl(wert); break;
                    case "VStart": g.VStart = Zahl(wert); break;
                    case "IMaxDC": g.IMaxDC = Zahl(wert); break;
                    case "NbMPPT": g.NbMPPT = Ganzzahl(wert); break;
                    case "NbInputs": g.NbInputs = Ganzzahl(wert); break;

                    // Wirkungsgrad [%]
                    case "EfficMax": g.EfficMax = Zahl(wert); break;
                    case "EfficEuro": g.EfficEuro = Zahl(wert); break;

                    // Herstellerangaben ohne Commercial-Block (aeltere PVsyst-Staende)
                    case "Manufacturer":
                        if (string.IsNullOrEmpty(g.Manufacturer)) g.Manufacturer = wert;
                        break;
                    case "Model":
                        if (string.IsNullOrEmpty(g.Model)) g.Model = wert;
                        break;
                }
            }

            Kennlinie(g, profile);
            return g;
        }

        /// <summary>
        /// Wählt die Kennlinienfassung: die NOMINALE (<c>ProfilPIOV2</c>) bei drei
        /// Fassungen, sonst die einzelne (<c>ProfilPIO</c>), sonst die erste, die
        /// Punkte hat (Konzept 5.2).
        /// </summary>
        private static void Kennlinie(
            OndWechselrichter g,
            Dictionary<string, List<(double PIn, double POut)>> profile)
        {
            if (g == null || profile.Count == 0) return;

            foreach (string name in Vorrang(profile))
            {
                if (!profile.TryGetValue(name, out List<(double, double)> punkte)) continue;
                if (punkte.Count == 0) continue;

                g.Kennlinienfassung = name;
                g.Kennlinienpunkte = punkte;
                return;
            }
        }

        /// <summary>
        /// Die Reihenfolge, in der nach einer Kennlinienfassung gesucht wird: erst die
        /// nominale, dann die einzelne, dann alles Übrige in Namensordnung.
        /// </summary>
        private static IEnumerable<string> Vorrang(
            Dictionary<string, List<(double PIn, double POut)>> profile)
        {
            yield return PROFIL_NOMINAL;
            yield return PROFIL_EINZELN;

            foreach (string name in profile.Keys.OrderBy(k => k, StringComparer.Ordinal))
                if (!string.Equals(name, PROFIL_NOMINAL, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(name, PROFIL_EINZELN, StringComparison.OrdinalIgnoreCase))
                    yield return name;
        }

        /// <summary>Ist der Schlüssel der Kopf eines Kennlinienblocks?</summary>
        internal static bool IstProfilmarke(string schluessel)
        {
            return !string.IsNullOrEmpty(schluessel)
                && schluessel.StartsWith(PROFIL_EINZELN, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Eine Zahl aus der Datei — invariant, mit Komma als zweitem Dezimalzeichen.
        /// Wörtlich <c>PanDataService.ParseD</c>.
        /// </summary>
        internal static double Zahl(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            return double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out double d) ? d : 0.0;
        }

        /// <summary>Eine Ganzzahl aus der Datei; 0, wenn sie nicht lesbar ist.</summary>
        internal static int Ganzzahl(string s)
        {
            return int.TryParse((s ?? "").Trim(), NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out int i) ? i : 0;
        }
    }
}
