using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Seiten.Berichte;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die zwei Vorgaben der Installation im Abschnitt „Bericht" der Programmeinstellungen</b>
    /// (Konzept Berichtsvorlagen 10.3): die vorgegebene Word-Vorlage (Einstellung
    /// <see cref="BerichtsvorlagenCtrl.EINSTELLUNG_VORGABE_WORD"/>) und die vorgegebene
    /// Excel-Vorlage (<see cref="BerichtsvorlagenCtrl.EINSTELLUNG_VORGABE_EXCEL"/>).
    ///
    /// <para><b>Die Listen sind die der Berichtsseite</b> — sie kommen aus demselben Controller
    /// (<see cref="BerichtsvorlagenCtrl.Liste"/>, <see cref="BerichtsvorlagenCtrl.ListeExcel"/>);
    /// die Oberfläche sucht keine Dateien und kennt keine Datenbank. Die Kennungen des Controllers
    /// (<c>standard</c>, <c>ohne</c>, <c>ausfuehrlich</c>, <c>eigen:</c> + Dateiname) werden hier je
    /// Dialog auf kleine Zahlen abgebildet, wie es die Gruppe „Vorlage" der Berichtsseite tut.</para>
    ///
    /// <para><b>Eine gespeicherte Vorgabe, deren Datei es nicht mehr gibt</b>, steht als GESPERRTER
    /// Eintrag in der Liste, gewählt, mit dem Satz, den der Kern beim Erstellen nennt
    /// (<c>„… nicht vorhanden – … verwendet"</c> bzw. <c>„Excel-Vorlage … nicht vorhanden – die
    /// Mappe entsteht ohne Vorlage."</c>). Welche Vorlage der Kern statt ihrer nähme, sagt derselbe
    /// Satz — Anzeige und Verhalten bleiben gleich. Ermittelt wird der Fall über
    /// <see cref="BerichtsvorlagenCtrl.VorlageFuer"/> bzw.
    /// <see cref="BerichtsvorlagenCtrl.ExcelVorlageFuer"/> OHNE Konfiguration: Dann bleibt die
    /// Abweichung eines Stammprojekts außen vor, und übrig ist genau die Kette Vorgabe → Standard.</para>
    ///
    /// <para><b>Geschrieben wird erst im OK-Weg</b> des Dialogs (<c>VorgabeWordChanged</c>,
    /// <c>VorgabeExcelChanged</c>), je Wert einmal und nur geändert — dasselbe Muster wie Firma,
    /// Vorlagenordner und Logo. Die Wahl der Standardvorlage bzw. „ohne Vorlage" ENTFERNT die
    /// Einstellung, damit die Vorgabe des Kerns gilt.</para>
    /// </summary>
    internal sealed class EinstellungenBerichtVorgaben
    {
        private readonly BerichtsvorlagenCtrl _vorlagen;

        /// <summary>Kennung → Id und zurück — je Dialog stabil.</summary>
        private readonly Dictionary<int, string> _kennungen = new Dictionary<int, string>();
        private readonly Dictionary<string, int> _ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private int _naechste = 1;

        internal EinstellungenBerichtVorgaben(BerichtsvorlagenCtrl vorlagen)
        {
            _vorlagen = vorlagen ?? throw new ArgumentNullException(nameof(vorlagen));
        }

        /// <summary>
        /// Belegt die sechs Parameter der zwei Auswahlfelder im Satz des Dialogs: je Feld die Liste,
        /// die Wahl (nur wenn es eine gibt), die Vorgabe für „Standardwerte" und den Rückweg.
        /// </summary>
        internal void Belegen(IDictionary<string, object> gaben)
        {
            IReadOnlyList<Vorlagenzeile> word = WordZeilen(out int? wordWahl);
            gaben["VorgabeWordVorlagen"] = word;
            if (wordWahl.HasValue) gaben["VorgabeWord"] = wordWahl.Value;
            gaben["VorgabeWordStandard"] = IdFuer(BerichtsvorlagenCtrl.ID_STANDARD);
            gaben["VorgabeWordChanged"] = EventCallback.Factory.Create<int?>(this, WordGewaehlt);

            IReadOnlyList<Vorlagenzeile> excel = ExcelZeilen(out int? excelWahl);
            gaben["VorgabeExcelVorlagen"] = excel;
            if (excelWahl.HasValue) gaben["VorgabeExcel"] = excelWahl.Value;
            gaben["VorgabeExcelStandard"] = IdFuer(BerichtsvorlagenCtrl.ID_OHNE);
            gaben["VorgabeExcelChanged"] = EventCallback.Factory.Create<int?>(this, ExcelGewaehlt);
        }

        // =====================================================================
        //  Die zwei Listen
        // =====================================================================

        /// <summary>
        /// Die Word-Vorlagen der Vorgabe: die Standardvorlage und die eigenen des Vorlagenordners —
        /// dazu, gesperrt und gewählt, eine gespeicherte Vorgabe, deren Datei fehlt.
        /// </summary>
        internal IReadOnlyList<Vorlagenzeile> WordZeilen(out int? gewaehlt)
        {
            var zeilen = new List<Vorlagenzeile>();
            foreach (Vorlageneintrag e in Sicher(() => _vorlagen.Liste()))
                zeilen.Add(new Vorlagenzeile(IdFuer(e.Id), e.Name, false, "", e.IstStandard));

            Vorlagenwahl wahl = null;
            try { wahl = _vorlagen.VorlageFuer(null); }
            catch (Exception) { wahl = null; }
            gewaehlt = Waehlen(zeilen, wahl);
            return zeilen;
        }

        /// <summary>
        /// Die Excel-Vorlagen der Vorgabe: „Ohne Vorlage (EPOS-Plan)", die mitgelieferte ausführliche
        /// und die eigenen des Vorlagenordners — dazu, gesperrt und gewählt, eine fehlende Vorgabe.
        /// </summary>
        internal IReadOnlyList<Vorlagenzeile> ExcelZeilen(out int? gewaehlt)
        {
            var zeilen = new List<Vorlagenzeile>();
            foreach (Vorlageneintrag e in Sicher(() => _vorlagen.ListeExcel()))
                zeilen.Add(new Vorlagenzeile(IdFuer(e.Id), e.Name, false, "",
                                             BerichtsvorlagenCtrl.IstAusfuehrlichExcel(e)));

            Vorlagenwahl wahl = null;
            try { wahl = _vorlagen.ExcelVorlageFuer(null); }
            catch (Exception) { wahl = null; }
            gewaehlt = Waehlen(zeilen, wahl);
            return zeilen;
        }

        /// <summary>
        /// Die Wahl zu einer aufgelösten Vorlage: die fehlende Vorgabe als gesperrter Eintrag mit dem
        /// Satz des Kerns, sonst der Eintrag der Liste.
        /// </summary>
        private int? Waehlen(List<Vorlagenzeile> zeilen, Vorlagenwahl wahl)
        {
            if (wahl == null) return null;
            if (wahl.FehlendeId != null)
            {
                int id = IdFuer(wahl.FehlendeId);
                string hinweis = wahl.Meldungen == null
                    ? ""
                    : wahl.Meldungen.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "";
                if (zeilen.All(z => z.Id != id))
                    zeilen.Add(new Vorlagenzeile(id, NameAusKennung(wahl.FehlendeId), true, hinweis));
                return id;
            }
            return wahl.Eintrag == null ? (int?)null : IdFuer(wahl.Eintrag.Id);
        }

        // =====================================================================
        //  Die zwei Rückwege
        // =====================================================================

        /// <summary>
        /// Schreibt die Vorgabe der Word-Vorlage; die Standardvorlage entfernt die Einstellung. Eine
        /// Id, die die Liste nicht kennt, ändert nichts.
        /// </summary>
        internal void WordGewaehlt(int? id)
        {
            string kennung = Kennung(id);
            if (kennung == null) return;
            _vorlagen.SetzeVorgabeWord(IstStandard(kennung) ? null : kennung);
        }

        /// <summary>
        /// Schreibt die Vorgabe der Excel-Vorlage; „ohne Vorlage" entfernt die Einstellung
        /// (das tut <see cref="BerichtsvorlagenCtrl.SetzeVorgabeExcel"/> selbst).
        /// </summary>
        internal void ExcelGewaehlt(int? id)
        {
            string kennung = Kennung(id);
            if (kennung == null) return;
            _vorlagen.SetzeVorgabeExcel(kennung);
        }

        private static bool IstStandard(string kennung)
        {
            return string.Equals(kennung, BerichtsvorlagenCtrl.ID_STANDARD, StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        //  Kennungen
        // =====================================================================

        /// <summary>Die Id einer Kennung — je Dialog stabil.</summary>
        internal int IdFuer(string kennung)
        {
            string k = (kennung ?? "").Trim();
            if (_ids.TryGetValue(k, out int id)) return id;
            id = _naechste++;
            _ids[k] = id;
            _kennungen[id] = k;
            return id;
        }

        /// <summary>Die Kennung einer Id; <c>null</c>, wenn die Liste sie nicht kennt.</summary>
        internal string Kennung(int? id)
        {
            if (!id.HasValue) return null;
            return _kennungen.TryGetValue(id.Value, out string kennung) ? kennung : null;
        }

        /// <summary>Der Anzeigename einer Kennung, deren Eintrag es nicht mehr gibt — der Dateiname ohne Endung.</summary>
        private static string NameAusKennung(string kennung)
        {
            string t = (kennung ?? "").Trim();
            if (!t.StartsWith(BerichtsvorlagenCtrl.ID_PRAEFIX_EIGEN, StringComparison.OrdinalIgnoreCase)) return t;
            string datei = t.Substring(BerichtsvorlagenCtrl.ID_PRAEFIX_EIGEN.Length).Trim();
            try { return System.IO.Path.GetFileNameWithoutExtension(datei); }
            catch (ArgumentException) { return datei; }
        }

        private static IReadOnlyList<Vorlageneintrag> Sicher(Func<IReadOnlyList<Vorlageneintrag>> quelle)
        {
            try { return quelle() ?? Array.Empty<Vorlageneintrag>(); }
            catch (Exception) { return Array.Empty<Vorlageneintrag>(); }
        }
    }
}
