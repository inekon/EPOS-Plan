using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorschau einer Nutzungsart im Katalogdialog</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.4, 5.6): eine Einheit der Bezugsart am mittleren Bedarfsniveau — je
    /// Tagtyp Werktag, Samstag, Sonn-/Feiertag 24 Stundenwerte [kW] (die mittlere Tagesmenge
    /// [kWh/(Einheit·d)] mal dem Stundenanteil des Tagtyps; eine Stunde, also kWh = kW) und je
    /// Monat die Menge [kWh] (mittlere Tagesmenge × Monatsfaktor × Tage des Monats, kein
    /// Schaltjahr). Wochengang, Kalender, Ferien und Zirkulation wirken erst im Lauf des Projekts —
    /// die Vorschau zeigt die Katalogwerte, sie rechnet kein Projekt.
    /// </summary>
    internal sealed record TwwNutzungsartVorschau(double[] WerktagKw, double[] SamstagKw, double[] SonnFeiertagKw,
                                                  double[] MonateKwh);

    /// <summary>
    /// Was der Katalogdialog „Brauchwasser-Nutzungsarten" (5.4) über die Pflegeaktionen hinaus
    /// braucht: welche Projekte eine Nutzungsart benutzen (EINE Abfrage je Liste — daran hängen
    /// die Sperrgründe von „Ändern…" und „Löschen") und die Vorschau der Katalogwerte.
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        /// <summary>
        /// <b>Die Verwendung aller Nutzungsarten</b> in EINER Abfrage: je Id der Nutzungsart die
        /// Namen der Projekte, deren Zonen sie benutzen (alphabetisch, ohne Doppel). Eine Zone
        /// ohne auffindbares Projekt zählt mit ihrer Projekt-Id. Ohne Tabellen leer.
        /// </summary>
        internal static IReadOnlyDictionary<int, IReadOnlyList<string>> Projektverwendung()
        {
            var ergebnis = new Dictionary<int, IReadOnlyList<string>>();
            if (!TabellenVorhanden()) return ergebnis;

            bool mitProjekt = DataRepository.TabelleVorhanden("Tab_Projekt")
                              && DataRepository.SpaltenVonTabelle("Tab_Projekt").Contains("Projektname", StringComparer.OrdinalIgnoreCase);
            DataTable dt = DataRepository.GetDataTable(mitProjekt
                ? "SELECT DISTINCT z.ID_Nutzungsart, z.ID_Projekt, p.Projektname FROM " + TwwSchema.TAB_TWW_ZONE +
                  " AS z LEFT JOIN Tab_Projekt AS p ON p.ID = z.ID_Projekt"
                : "SELECT DISTINCT z.ID_Nutzungsart, z.ID_Projekt, NULL AS Projektname FROM " + TwwSchema.TAB_TWW_ZONE + " AS z");
            if (dt == null) return ergebnis;

            var mengen = new Dictionary<int, SortedSet<string>>();
            foreach (DataRow r in dt.Rows)
            {
                int id = ZapfprofilCtrl.Ganz(r, "ID_Nutzungsart");
                string name = ZapfprofilCtrl.Text(r, "Projektname");
                if (name.Length == 0) name = "#" + ZapfprofilCtrl.Ganz(r, "ID_Projekt").ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!mengen.TryGetValue(id, out SortedSet<string> m)) mengen[id] = m = new SortedSet<string>(StringComparer.CurrentCulture);
                m.Add(name);
            }
            foreach (KeyValuePair<int, SortedSet<string>> kv in mengen) ergebnis[kv.Key] = kv.Value.ToList();
            return ergebnis;
        }

        /// <summary>
        /// Die Vorschau der Nutzungsart (siehe <see cref="TwwNutzungsartVorschau"/>); <c>null</c> ohne
        /// Nutzungsart oder ohne vollständigen Tagesgangsatz — dann benennt der Dialog den Grund.
        /// </summary>
        internal static TwwNutzungsartVorschau Vorschau(Nutzungsart n)
        {
            if (n?.Tagesgaenge == null || !n.Tagesgaenge.Vollstaendig || n.BedarfJeNiveauKwhJeEinheitTag == null
                || n.BedarfJeNiveauKwhJeEinheitTag.Length != NutzungsartRaster.NIVEAUS || n.Monatsfaktoren == null
                || n.Monatsfaktoren.Length != NutzungsartRaster.MONATE)
                return null;

            double tag = n.BedarfJeNiveauKwhJeEinheitTag[(int)ZapfNiveau.Mittel - 1];
            double[] Reihe(int tagtyp)
            {
                var r = new double[Tagesgangsatz.STUNDEN];
                for (int h = 0; h < r.Length; h++) r[h] = tag * n.Tagesgaenge.Anteile[tagtyp, h];
                return r;
            }

            var monate = new double[NutzungsartRaster.MONATE];
            for (int m = 0; m < monate.Length; m++) monate[m] = tag * n.Monatsfaktoren[m] * Zapfkalender.TageJeMonat[m];
            return new TwwNutzungsartVorschau(Reihe(0), Reihe(1), Reihe(2), monate);
        }
    }
}
