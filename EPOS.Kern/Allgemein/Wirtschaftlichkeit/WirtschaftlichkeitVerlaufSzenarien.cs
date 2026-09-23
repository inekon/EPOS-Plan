using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E6 (Konzept § 2.13 (5), Analysepapier A3) — der <b>Kapitalwert-Verlauf mit
    /// allen drei Szenarien</b>: je Szenario ein vollständiger Lauf von
    /// <see cref="WirtschaftlichkeitCtrl.BerechneVerlauf(BerichtsDaten, WirtschaftlichkeitParameter, int, string, int)"/>,
    /// gesammelt in EINEM Modell.
    ///
    /// <para><b>Warum ein Sammelmodell und kein neuer Rechenweg.</b>
    /// <see cref="WirtschaftlichkeitVerlauf"/> trägt genau ein Szenario, und der
    /// Rechenaufruf nimmt einen Szenario-Text. Drei Aufrufe desselben Weges — jeder mit
    /// seinem Parametersatz (<see cref="WirtschaftlichkeitParameter.FuerSzenario"/>) —
    /// liefern die drei Linien je Stand; gerechnet wird ohne Speichern, gespeichert wird
    /// nichts (Muster <see cref="WirtschaftlichkeitCtrl.BerechneBandbreite"/>). Die
    /// Berichtsdaten werden dabei nur einmal gesammelt; der Mehraufwand ist die
    /// Zahlungsbildrechnung.</para>
    ///
    /// <para><b>Was das Bild daraus zeichnet</b> (Konzept § 2.13 (5)): je Stand mit
    /// Differenzlinie eine Farbe, je Szenario eine Strichart
    /// (<c>ChartRenderer.VerlaufsReihenSzenarien</c>), dazu je Linie ihren
    /// <see cref="Nulldurchgang"/> — die dynamische Amortisation in diesem Szenario.</para>
    /// </summary>
    public sealed class WirtschaftlichkeitVerlaufSzenarien
    {
        /// <summary>
        /// Die Szenarien in Zeichen-, Legenden- und Spaltenreihenfolge: Ungünstig ·
        /// Erwartet · Günstig — dieselbe Reihenfolge wie die Bandbreite der Seite.
        /// </summary>
        public static readonly IReadOnlyList<string> Reihenfolge = new[]
        {
            WirtschaftlichkeitSzenario.WORST,
            WirtschaftlichkeitSzenario.ERWARTET,
            WirtschaftlichkeitSzenario.BEST
        };

        /// <summary>Der Horizont aller drei Läufe [a] — Jahre 0 … <see cref="Jahre"/>.</summary>
        public int Jahre { get; set; }

        /// <summary>
        /// Je Szenario (Persistenzwert, <see cref="WirtschaftlichkeitSzenario"/>) der
        /// vollständige Lauf. Ein Szenario ohne Eintrag wurde nicht gerechnet.
        /// </summary>
        public Dictionary<string, WirtschaftlichkeitVerlauf> Laeufe { get; }
            = new Dictionary<string, WirtschaftlichkeitVerlauf>(StringComparer.Ordinal);

        /// <summary>Der Lauf eines Szenarios; <c>null</c>, wenn es nicht gerechnet wurde.</summary>
        public WirtschaftlichkeitVerlauf Lauf(string szenario)
        {
            if (szenario == null) return null;
            WirtschaftlichkeitVerlauf v;
            return Laeufe.TryGetValue(szenario, out v) ? v : null;
        }

        /// <summary>
        /// Die Stände mit Differenzlinie, in der Reihenfolge der Gruppe (Stamm zuerst) —
        /// über alle drei Läufe vereinigt: Ein Stand erscheint, sobald EIN Szenario eine
        /// Linie für ihn hat. Die Reihenfolge ist die der absoluten Reihen des ersten
        /// gerechneten Laufs; die Referenz steht nicht darin (sie wäre die Nulllinie).
        /// </summary>
        public List<KeyValuePair<int, string>> Versionen()
        {
            var ids = new HashSet<int>();
            foreach (WirtschaftlichkeitVerlauf lauf in Laeufe.Values)
                foreach (VerlaufSerie d in lauf.Differenz)
                    if (d != null && d.Kumuliert != null) ids.Add(d.IdProjekt);

            var liste = new List<KeyValuePair<int, string>>();
            var gesehen = new HashSet<int>();
            foreach (string s in Reihenfolge)
            {
                WirtschaftlichkeitVerlauf lauf = Lauf(s);
                if (lauf == null) continue;
                foreach (VerlaufSerie a in lauf.Absolut)
                {
                    if (a == null || !ids.Contains(a.IdProjekt) || !gesehen.Add(a.IdProjekt)) continue;
                    liste.Add(new KeyValuePair<int, string>(a.IdProjekt, a.Anzeige ?? ""));
                }
            }
            return liste;
        }

        /// <summary>
        /// Die Differenzlinie eines Standes in einem Szenario — kumulierter Barwert der
        /// Differenz zur Referenz je Jahr, ohne Restwert. <c>null</c>, wenn es sie nicht gibt.
        /// </summary>
        public VerlaufSerie Differenz(int idProjekt, string szenario)
        {
            WirtschaftlichkeitVerlauf lauf = Lauf(szenario);
            if (lauf == null) return null;
            foreach (VerlaufSerie d in lauf.Differenz)
                if (d != null && d.IdProjekt == idProjekt && d.Kumuliert != null) return d;
            return null;
        }

        /// <summary>
        /// Der Nulldurchgang der Differenzlinie (<see cref="KapitalwertRechner.Nulldurchgang"/>)
        /// — die dynamische Amortisation dieses Standes in diesem Szenario [a];
        /// <c>null</c> = kein Durchgang im Horizont oder keine Linie.
        /// </summary>
        public double? Nulldurchgang(int idProjekt, string szenario)
        {
            VerlaufSerie d = Differenz(idProjekt, szenario);
            return d == null ? null : KapitalwertRechner.Nulldurchgang(d.Kumuliert);
        }

        /// <summary>
        /// Der Barwert des Restwerts am Horizontende, als Differenz zur Referenz — er steht
        /// nicht in der Linie (Kapitalwertdifferenz = Endwert der Linie + dieser Wert).
        /// <c>null</c>, wenn es die Linie nicht gibt.
        /// </summary>
        public double? RestwertDifferenz(int idProjekt, string szenario)
        {
            VerlaufSerie d = Differenz(idProjekt, szenario);
            return d == null ? (double?)null : d.RestwertBarwert;
        }

        /// <summary>Keine einzige Differenzlinie — dann gibt es kein Bild.</summary>
        public bool Leer { get { return Versionen().Count == 0; } }

        /// <summary>
        /// Die Stände OHNE Reihe samt Grund — aus dem ersten gerechneten Lauf (der Grund
        /// hängt an den Eingangsdaten, nicht am Szenario).
        /// </summary>
        public List<VerlaufSerie> OhneReihe()
        {
            var liste = new List<VerlaufSerie>();
            foreach (string s in Reihenfolge)
            {
                WirtschaftlichkeitVerlauf lauf = Lauf(s);
                if (lauf == null) continue;
                foreach (VerlaufSerie a in lauf.Absolut)
                    if (a != null && a.Fehlgrund != null) liste.Add(a);
                break;
            }
            return liste;
        }
    }
}
