using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Raum der Raumliste des Zuordnungsdialogs</b> — mit dem Haken „beheizt" und dem Grund
    /// der Entscheidung (Umsetzungskonzept 3.5 Nr. 3: „Der Dialog zeigt die Raumliste mit dem
    /// Haken, damit die Regel sichtbar und korrigierbar ist").
    ///
    /// <para><b>Datei und Anwender stehen nebeneinander.</b> <see cref="BeheiztLautDatei"/> ist,
    /// was der Leser entschieden hat (Attribut, Namensregel oder Annahme); <see cref="Beheizt"/>
    /// ist, was gilt — die Übersteuerung des Anwenders schlägt die Datei. Die Zuordnung liest den
    /// wirksamen Wert über <see cref="BeheiztWirksam"/>, das Abbild bleibt unverändert: Der
    /// Zustand lebt im Ablauf, und eine zurückgenommene Übersteuerung stellt die Datei wieder her.</para>
    ///
    /// <para>Werte, keine Anzeigetexte — den Grund beschriftet
    /// <see cref="GebaeudeZuordnungsModell.RaumGrundText"/>.</para>
    /// </summary>
    internal sealed class GebaeudeRaumzeile
    {
        internal GebaeudeRaumzeile(AbbildRaum raum, IReadOnlyDictionary<string, bool> uebersteuert)
        {
            Kennung = raum.Kennung ?? "";
            Name = raum.Name;
            FlaecheM2 = raum.FlaecheM2;
            VolumenM3 = raum.VolumenM3;
            BeheiztLautDatei = raum.Beheizt;
            Quelle = raum.BeheiztQuelle;
            Zustandsangabe = raum.Zustandsangabe;
            Namenstreffer = raum.BeheiztQuelle == BeheiztQuelle.Name ? Raumnamenregel.Treffer(raum.Name) : null;
            Beheizt = BeheiztWirksam(raum, uebersteuert);
            Uebersteuert = Beheizt != BeheiztLautDatei;
        }

        /// <summary>Kennung des Raums aus der Datei — der Schlüssel der Übersteuerung.</summary>
        public string Kennung { get; }

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; }

        /// <summary>Was die Liste zeigt: der Name, sonst die Kennung.</summary>
        public string Anzeigename => string.IsNullOrWhiteSpace(Name) ? Kennung : Name;

        /// <summary>Fläche [m²]; <c>null</c> = nicht gelesen.</summary>
        public double? FlaecheM2 { get; }

        /// <summary>Volumen [m³]; <c>null</c> = nicht gelesen.</summary>
        public double? VolumenM3 { get; }

        /// <summary>Gilt der Raum als beheizt — mit der Übersteuerung des Anwenders?</summary>
        public bool Beheizt { get; }

        /// <summary>Was der Leser entschieden hat.</summary>
        public bool BeheiztLautDatei { get; }

        /// <summary>Woraus <see cref="BeheiztLautDatei"/> folgt.</summary>
        public BeheiztQuelle Quelle { get; }

        /// <summary>Der gelesene Nutzungszustand, wie er in der Datei steht; <c>null</c> = keiner.</summary>
        public string Zustandsangabe { get; }

        /// <summary>Das Muster der Namensregel, das getroffen hat; <c>null</c> = keines.</summary>
        public string Namenstreffer { get; }

        /// <summary>Weicht <see cref="Beheizt"/> von der Datei ab, weil der Anwender umgestellt hat?</summary>
        public bool Uebersteuert { get; }

        /// <summary>
        /// Der wirksame Zustand eines Raums: die Übersteuerung, wenn sie diesen Raum nennt, sonst
        /// die Entscheidung des Lesers. <c>null</c> als Übersteuerung heißt „keine".
        /// </summary>
        internal static bool BeheiztWirksam(AbbildRaum raum, IReadOnlyDictionary<string, bool> uebersteuert)
            => uebersteuert != null && raum.Kennung != null && uebersteuert.TryGetValue(raum.Kennung, out bool b)
                ? b
                : raum.Beheizt;
    }
}
