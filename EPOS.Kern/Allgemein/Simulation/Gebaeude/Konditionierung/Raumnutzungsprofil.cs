using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Nutzungsprofil</b> (Konzept Nutzungsprofile 4.1, NP-F1): ein Parametersatz aus Kopf, 25 nullbaren
    /// Kennwerten (NP-F6), dem Zeilenbild je Größe (NP-F7) und den Stundenprofilen (NP-F9) — die Zeile von
    /// <c>Tab_Raumnutzungsprofil</c> samt <c>Tab_Raumnutzungszeile</c> und <c>Tab_Raumnutzungsstunden</c>.
    /// Profile werden nie gerechnet, nur über den <see cref="Raumnutzungsgenerator"/> übernommen.
    /// Ohne Datenbank; die Datenbankseite ist <c>RaumnutzungCtrl</c>.
    /// </summary>
    public sealed class Raumnutzungsprofil
    {
        /// <summary>Die Id in <c>Tab_Raumnutzungsprofil</c>; 0 = neu.</summary>
        public long Id { get; set; }

        /// <summary>Die Kategorie (<c>Tab_Raumnutzungskatalog.ID</c>).</summary>
        public long IdKatalog { get; set; }

        /// <summary>Die Nummer in der Quelle als Text (NP-F5); <c>null</c> = ohne.</summary>
        public string Nummer { get; set; }

        /// <summary>Der Name, eindeutig je Kategorie ohne Unterschied der Schreibung.</summary>
        public string Bezeichner { get; set; }

        /// <summary>Die Beschreibung; <c>null</c> = ohne.</summary>
        public string Beschreibung { get; set; }

        /// <summary><c>ReadOnly = 1</c>: gehört zur Auslieferung, nur duplizierbar (NP-F19).</summary>
        public bool Ausgeliefert { get; set; }

        /// <summary>Nutzungsbeginn, Stunde 0 … 24.</summary>
        public int? Nutzung_Von { get; set; }

        /// <summary>Nutzungsende, Stunde 0 … 24.</summary>
        public int? Nutzung_Bis { get; set; }

        /// <summary>Betriebsbeginn der Anlage; leer = wie Nutzung.</summary>
        public int? Betrieb_Von { get; set; }

        /// <summary>Betriebsende der Anlage; leer = wie Nutzung.</summary>
        public int? Betrieb_Bis { get; set; }

        /// <summary>Sieben Ziffern 0/1, Montag bis Sonntag (etwa <c>1111100</c>) — der Generatoreingang (NP-F8).</summary>
        public string Nutzungstage_Woche { get; set; }

        /// <summary>
        /// Nutzungstage je Jahr, wie die Spalte sie trägt — <b>nicht mehr beschrieben</b> (E93): Die Zahl wird abgeleitet
        /// (<see cref="Raumnutzungsgenerator.Nutzungstage"/>); die Spalte bleibt nullbar stehen, <see cref="Kennwerte"/>
        /// schreibt dort nichts.
        /// </summary>
        public int? Nutzungstage_Jahr { get; set; }

        /// <summary>Die neun bundeseinheitlichen Feiertage „wie Sonntag".</summary>
        public bool? Feiertage_Wie_Sonntag { get; set; }

        /// <summary>Heizsollwert im Betriebsfenster [°C].</summary>
        public double? Heiz_Soll { get; set; }

        /// <summary>Heizsollwert außerhalb [°C].</summary>
        public double? Heiz_Soll_Ausserhalb { get; set; }

        /// <summary>Heizen außerhalb „aus".</summary>
        public bool? Heiz_Aus_Ausserhalb { get; set; }

        /// <summary>Kühlsollwert im Betriebsfenster [°C].</summary>
        public double? Kuehl_Soll { get; set; }

        /// <summary>Kühlsollwert außerhalb [°C].</summary>
        public double? Kuehl_Soll_Ausserhalb { get; set; }

        /// <summary>Kühlen außerhalb „aus".</summary>
        public bool? Kuehl_Aus_Ausserhalb { get; set; }

        /// <summary>Außenluft im Betriebsfenster in <see cref="Aussenluft_Einheit"/>.</summary>
        public double? Aussenluft { get; set; }

        /// <summary><c>1/h</c> oder <c>m3/hm2</c> (NP-F10).</summary>
        public string Aussenluft_Einheit { get; set; }

        /// <summary>Außenluft außerhalb in <see cref="Aussenluft_Einheit"/>.</summary>
        public double? Aussenluft_Ausserhalb { get; set; }

        /// <summary>Fläche je Person [m²].</summary>
        public double? Personen_Flaeche { get; set; }

        /// <summary>Wärmeabgabe je Person [W]; leer = <see cref="Matrixeingang.PERSON_W"/> (Q40).</summary>
        public double? Personen_Waerme { get; set; }

        /// <summary>Anteil der Personen im Nutzungsfenster 0 … 1; leer = 1.</summary>
        public double? Personen_Anteil { get; set; }

        /// <summary>Anteil der Personen außerhalb 0 … 1; leer = 0.</summary>
        public double? Personen_Anteil_Ausserhalb { get; set; }

        /// <summary>Gerätelast [W/m²] (Q39).</summary>
        public double? Geraete_Leistung { get; set; }

        /// <summary>Anteil der Geräte im Nutzungsfenster 0 … 1; leer = 1.</summary>
        public double? Geraete_Anteil { get; set; }

        /// <summary>Anteil der Geräte außerhalb 0 … 1; leer = wie im Fenster.</summary>
        public double? Geraete_Anteil_Ausserhalb { get; set; }

        /// <summary>Beleuchtung [W/m²] — Anteil der Gerätelast (Q38).</summary>
        public double? Beleuchtung_Leistung { get; set; }

        /// <summary>Gleichzeitigkeit der Beleuchtung 0 … 1; leer = 1.</summary>
        public double? Beleuchtung_Anteil { get; set; }

        /// <summary>Das Zeilenbild (NP-F7): Vorgabezeilen ohne <c>NENNWERT</c> und <c>SAISON</c>.</summary>
        public List<Vorgabezeile> Zeilen { get; set; } = new List<Vorgabezeile>();

        /// <summary>Die Stundenprofile (NP-F9).</summary>
        public List<Raumnutzungsstunden> Stunden { get; set; } = new List<Raumnutzungsstunden>();

        /// <summary>Die 25 Kennwerte in Schemareihenfolge (<see cref="RaumnutzungSchema.SPALTEN_KENNWERTE"/>).</summary>
        public IReadOnlyList<object> Kennwerte() => new object[]
        {
            Nutzung_Von, Nutzung_Bis, Betrieb_Von, Betrieb_Bis, Nutzungstage_Woche, null /* Nutzungstage_Jahr: abgeleitet, E93 */,
            Feiertage_Wie_Sonntag,
            Heiz_Soll, Heiz_Soll_Ausserhalb, Heiz_Aus_Ausserhalb, Kuehl_Soll, Kuehl_Soll_Ausserhalb, Kuehl_Aus_Ausserhalb,
            Aussenluft, Aussenluft_Einheit, Aussenluft_Ausserhalb,
            Personen_Flaeche, Personen_Waerme, Personen_Anteil, Personen_Anteil_Ausserhalb,
            Geraete_Leistung, Geraete_Anteil, Geraete_Anteil_Ausserhalb, Beleuchtung_Leistung, Beleuchtung_Anteil,
        };

        /// <summary>Trägt das Profil weder Kennwert noch Zeilenbild noch Stundenprofil (NP-F13)?</summary>
        public bool IstLeer => Kennwerte().All(w => w == null) && (Zeilen?.Count ?? 0) == 0 && (Stunden?.Count ?? 0) == 0;

        /// <summary>Eine tiefe Kopie (Zeilen und Stunden neu).</summary>
        public Raumnutzungsprofil Kopie()
        {
            var k = (Raumnutzungsprofil)MemberwiseClone();
            k.Zeilen = (Zeilen ?? new List<Vorgabezeile>()).Select(z => new Vorgabezeile
            {
                Groesse = z.Groesse, Zeile = z.Zeile, Wert = z.Wert, Aus = z.Aus, Von = z.Von, Bis = z.Bis, BedingtK = z.BedingtK,
            }).ToList();
            k.Stunden = (Stunden ?? new List<Raumnutzungsstunden>()).Select(s => new Raumnutzungsstunden(s.Groesse, s.Tagesart, s.Werte)).ToList();
            return k;
        }

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString() => (Nummer == null ? "" : Nummer + " ") + Bezeichner;
    }

    /// <summary>
    /// <b>Ein Stundenprofil</b> (NP-F9): Größe, Tagesart (<c>WERKTAG</c>/<c>FREI</c>) und 24 Werte als Text mit
    /// Semikolon, invariant — Sollwerte bei Heizen und Kühlen (<c>aus</c> erlaubt), Anteile 0 … 1 bei Geräten und
    /// Personen, bei der Lüftung Faktoren auf die Außenluft des Profils (ohne Außenluft: 1/h).
    /// </summary>
    public sealed record Raumnutzungsstunden(string Groesse, string Tagesart, string Werte);
}
