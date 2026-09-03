using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

public class PanDataService
{
    private static List<PVModule> _allModules = new List<PVModule>();
    public IReadOnlyList<PVModule> AllModules => _allModules;

    // ── PAN-Datei parsen ───────────────────────────────────────────────
    public static PanModule ParsePan(string content, string fileName = "")
    {
        var m = new PanModule { SourceFile = Path.GetFileNameWithoutExtension(fileName) };
        var lines = content.Split('\n');

        // Aktueller Abschnitt
        bool inCommercial = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("PVObject_Commercial")) { inCommercial = true; continue; }
            if (line.StartsWith("End of PVObject") && inCommercial) { inCommercial = false; continue; }
            if (line.StartsWith("End of PVObject")) continue;
            if (line.StartsWith("PVObject_") || line.StartsWith("Version") || line.StartsWith("Flags")) continue;

            if (!line.Contains("=")) continue;
            var idx = line.IndexOf('=');
            var key = line.Substring(0, idx).Trim();
            var val = line.Substring(idx + 1).Trim();

            if (inCommercial)
            {
                switch (key)
                {
                    case "Manufacturer": m.Manufacturer = val; break;
                    case "Model": m.Model = val; break;
                    case "DataSource": m.DataSource = val; break;
                    case "YearBeg": if (int.TryParse(val, out var y)) m.YearBegin = y; break;
                    case "Width": m.Width = ParseD(val); break;
                    case "Height": m.Height = ParseD(val); break;
                    case "Depth": m.Depth = ParseD(val); break;
                    case "Weight": m.Weight = ParseD(val); break;
                }
                continue;
            }

            switch (key)
            {
                case "Technol": m.Technol = val; break;
                case "NCelS": m.NCelS = ParseI(val); break;
                case "NCelP": m.NCelP = ParseI(val); break;
                case "SubModuleLayout": m.SubModuleLayout = val; break;
                case "FrontSurface": m.FrontSurface = val; break;
                case "Bifacial": m.Bifacial = val == "1"; break;
                case "BifacialityFactor": m.BifacialityFactor = ParseD(val); break;
                case "GRef": m.GRef = ParseD(val); break;
                case "TRef": m.TRef = ParseD(val); break;
                case "PNom": m.PNom = ParseD(val); break;
                case "PNomTolLow": m.PNomTolLow = ParseD(val); break;
                case "PNomTolUp": m.PNomTolUp = ParseD(val); break;
                case "Isc": m.Isc = ParseD(val); break;
                case "Voc": m.Voc = ParseD(val); break;
                case "Imp": m.Imp = ParseD(val); break;
                case "Vmp": m.Vmp = ParseD(val); break;
                case "muISC": m.muISC = ParseD(val); break;
                case "muVocSpec": m.muVocSpec = ParseD(val); break;
                case "muPmpReq": m.muPmpReq = ParseD(val); break;
                case "RShunt": m.RShunt = ParseD(val); break;
                case "Rserie": m.Rserie = ParseD(val); break;
                case "Gamma1": m.Gamma1 = ParseD(val); break;
                case "mIsc0": m.mIsc0 = ParseD(val); break;
                case "EgRef": m.EgRef = ParseD(val); break;
                case "GammaTh": m.GammaTh = ParseD(val); break;
                case "TCoef_Gamma": m.TCoef_Gamma = ParseD(val); break;
                // Herstellerdaten ohne Commercial-Block (ältere PVsyst-Versionen)
                case "Manufacturer": if (string.IsNullOrEmpty(m.Manufacturer)) m.Manufacturer = val; break;
                case "Model": if (string.IsNullOrEmpty(m.Model)) m.Model = val; break;
            }
        }
        AddPVModul(m);
        return m;
    }

    private static double ParseD(string v) =>
     double.TryParse(v.Replace(',', '.'), System.Globalization.NumberStyles.Float,
         System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    private static int ParseI(string v) =>
        int.TryParse(v, out var r) ? r : 0;

    public IEnumerable<string> GetManufacturers() =>
      _allModules.Select(m => m.Manufacturer).Distinct().OrderBy(x => x);

    public IEnumerable<int> GetYears() =>
        _allModules.Select(m => m.Date).Where(y => y > 1990).Distinct().OrderBy(x => x);

    public IEnumerable<string> GetTechnologies() =>
        _allModules.Select(m => m.Technology).Distinct().OrderBy(x => x);

    public static void AddPVModul(PanModule m)
    {
        PVModule pv = new PVModule
        {
            Source = m,
            Database = "PAN",
            Name = $"{m.Manufacturer} {m.Model}",
            Manufacturer = m.Manufacturer,
            Technology = m.Technology,
            Bifacial = m.Bifacial ? $"Ja ({m.BifacialityFactor:F2})" : "Nein",
            STC = m.PNom,
            A_c = m.Area,
            I_sc_ref = m.Isc,
            V_oc_ref = m.Voc,
            I_mp_ref = m.Imp,
            V_mp_ref = m.Vmp,
            Date = m.YearBegin
        };

        // Die Liste ist statisch und sammelt bewusst mehrere .pan-Dateien einer
        // Sitzung. Ein gleichnamiges Modul (erneut eingelesene Datei) ersetzt
        // deshalb seinen Altbestand, statt die Auswahlliste doppelt zu fuellen.
        string name = (pv.Name ?? "").Trim();
        int idx = _allModules.FindIndex(x =>
            string.Equals((x.Name ?? "").Trim(), name, System.StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _allModules[idx] = pv;
        else _allModules.Add(pv);
    }
}