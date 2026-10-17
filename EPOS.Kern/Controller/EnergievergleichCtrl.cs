using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Sammelt für den Energievergleich der Übersicht je Version das GESPEICHERTE Simulationsergebnis — ohne zu
    /// simulieren — und berechnet daraus die Kennzahlen des <see cref="KennzahlenKatalog"/>, Schritt für Schritt wie
    /// <c>BerichtsDatenSammler.SammleProjekt</c> (Ergebnisbaum, Brennstoffmengen, Verrechnung, Katalog). Die
    /// Gegenüberstellung selbst trifft <see cref="Energievergleich.Vergleiche"/>.
    /// </summary>
    public static class EnergievergleichCtrl
    {
        /// <summary>
        /// Die Kennzahlen einer Version aus ihrem letzten gespeicherten Lauf; ohne Lauf eine Version ohne
        /// <see cref="VariantenDaten.Ergebnis"/> (sie zeigt „—“).
        /// </summary>
        public static VariantenDaten Lade(int idProjekt, bool istStamm)
        {
            var v = new VariantenDaten { IdProjekt = idProjekt, IstStamm = istStamm };
            if (idProjekt <= 0) return v;

            v.Ergebnis = new ErgebnisCtrl().Load(idProjekt);
            if (v.Ergebnis == null) return v;
            v.SimulationsStand = v.Ergebnis.Zeitstempel;

            // Best effort wie im Berichtssammler: Ein fehlender Preis oder Brennstoff stoppt nichts, die betroffene
            // Kennzahl bleibt „—“.
            try { v.Brennstoffmengen = EnergieMengen.BaueBrennstoffmengen(idProjekt); }
            catch { v.Brennstoffmengen = null; }
            v.BhkwEinspeisungStrommatrixMwh = BhkwEinspeisungRueckfall(v.Ergebnis, idProjekt);
            KostenEmissionRechner.Berechne(v);
            KennzahlenKatalog.Berechne(v);
            return v;
        }

        /// <summary>
        /// EB-2 (EB-Q3): Führt das Ergebnis ein BHKW ohne gespeicherte Einspeisung (Ergebnis vor Schemaschritt 239), gilt
        /// die KWK-Einspeisung der gespeicherten Strommatrix — sie ist bei gerechneter Wirtschaftlichkeit exakt. Ohne Matrix
        /// <c>null</c>: Die Kennzahl zeigt „—“, keine stille Null.
        /// </summary>
        internal static double? BhkwEinspeisungRueckfall(ErgebnisModel ergebnis, int idProjekt)
        {
            if (ergebnis?.BHKW == null || ergebnis.BHKW.StromeinspeisungMwh.HasValue) return null;
            try
            {
                Dictionary<int, StromMatrix> m = new WirtschaftlichkeitCtrl().LadeStromMatrix(new List<int> { idProjekt });
                return m.TryGetValue(idProjekt, out StromMatrix sm) ? sm.KwkEinspeisungGesamtMWh : (double?)null;
            }
            catch { return null; }
        }

        /// <summary>Lädt die Versionen (der Stamm zuerst) und stellt sie gegenüber.</summary>
        public static EnergievergleichErgebnis Vergleiche(IReadOnlyList<int> projektIds, bool englisch, CultureInfo kultur)
        {
            var versionen = new List<VariantenDaten>();
            for (int i = 0; i < (projektIds?.Count ?? 0); i++) versionen.Add(Lade(projektIds[i], i == 0));
            return Energievergleich.Vergleiche(versionen, englisch, kultur);
        }
    }
}
