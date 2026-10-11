using System;

namespace WindowsFormsApplication1
{
    /// <summary>Das Verfahren der Aufheizung vor Nutzungsbeginn (Entwurf Vorheizrampe Fassung 2, 2.2, F1).</summary>
    public enum Aufheizverfahren
    {
        /// <summary>Die Sollwertrampe nach Aufheizleistung — der Bestand, bitgleich.</summary>
        Sollwertrampe = 0,

        /// <summary>Option 1: Vorheizzeit vorgeben, Deckel und Prüfung im Lauf (2.5).</summary>
        Vorgabe = 1,

        /// <summary>Option 2: Vorheizzeit berechnen (2.6) — Welle V3; bis dahin rechnet der Kern Option 1 nicht damit.</summary>
        Berechnet = 2,
    }

    /// <summary>Die Art der Toleranz des Deckels (2.1 „Toleranz“, F3).</summary>
    public enum Vorheiztoleranzart
    {
        /// <summary>x in Prozent von Φ_K,max.</summary>
        Prozent = 0,

        /// <summary>Δ in kW über Φ_K,max.</summary>
        Kilowatt = 1,
    }

    /// <summary>Die Geltung von Deckel und Vorheizzeit (2.7, F6).</summary>
    public enum Vorheizgeltung
    {
        /// <summary>Je Gebäude (Zonenanteile kommen mit Welle V3; bis dahin wie je Zone).</summary>
        Gebaeude = 0,

        /// <summary>Je Zone.</summary>
        Zone = 1,
    }

    /// <summary>
    /// <b>Der Ankunftsbezug eines Gebäudes mit Heizkreis</b> (Welle V3b; entschieden mit F17 (b), Anwender 11.10.2026, Welle
    /// V3c; kein Schema, keine Oberfläche): wogegen Vorausschau und Nachweis die Ankunft am Beginn von h_s messen. Gilt nur für
    /// Zonen mit wirksamer Wärmeübergabe (<see cref="GebaeudeModellEingang.KopplungWirksam"/>); jede andere Zone misst gegen
    /// θ_T − ε. Die Wahl bleibt <c>internal</c>, weil Tests und Messung (a) als Gegenprobe rechnen.
    /// </summary>
    internal enum Vorheizankunftsbezug
    {
        /// <summary>(a) Gegen θ_T − ε (Entwurf 2.5) — nur noch Gegenprobe der Tests und der Messung.</summary>
        Sollwert = 0,

        /// <summary>
        /// (b) Gegen min(θ_T, θ_stat) − ε: θ_stat ist die Raumluft, die die Übergabe am durchgewärmten Bau zur Kalenderstunde h_s
        /// stationär hält (Gleichgewicht des Zonenmodells mit dem Rand von h_s, <see cref="Vorheizankunft"/>) — die Vorgabe (F17).
        /// </summary>
        Uebergabe = 1,
    }

    /// <summary>
    /// <b>Die Projekteinstellung des Vorheizens</b> (Entwurf Vorheizrampe Fassung 2, 2.5, 2.9; Welle V2) — unveränderlich,
    /// ohne Schema (die Spalten kommen mit V4): Verfahren, Vorheizzeit t_V, Toleranz (Art und Wert), Regelgenauigkeit ε,
    /// Geltung und der Heizkreis-Schalter (F16). Die Vorgabe ist die <see cref="Aufheizverfahren.Sollwertrampe"/> — jedes
    /// bestehende Projekt rechnet bitgleich. Leere Felder (<c>null</c>, NaN, ±∞) setzen die Kern-Konstanten ein (F14, F15):
    /// Toleranz <see cref="TOLERANZ_VORGABE_PROZENT"/> %, Regelgenauigkeit <see cref="GENAUIGKEIT_VORGABE_K"/> K.
    /// </summary>
    public sealed record Vorheizvorgabe
    {
        /// <summary>Die Vorgabe: Sollwertrampe, alles leer, Heizkreis einbezogen.</summary>
        public static readonly Vorheizvorgabe Sollwertrampe = new Vorheizvorgabe(Aufheizverfahren.Sollwertrampe);

        /// <summary>Die Toleranz des Deckels, wenn das Feld leer ist [%] (F14 (a): Kern-Konstante).</summary>
        public const double TOLERANZ_VORGABE_PROZENT = 20.0;

        /// <summary>Die Regelgenauigkeit ε, wenn das Feld leer ist [K] (F15 (a)).</summary>
        public const double GENAUIGKEIT_VORGABE_K = 1.0;

        /// <summary>Die kleinste Vorheizzeit [h].</summary>
        public const int VORHEIZZEIT_MIN_H = 1;

        /// <summary>Die größte Vorheizzeit [h] (Fenster höchstens 47 h, 2.1).</summary>
        public const int VORHEIZZEIT_MAX_H = 47;

        /// <summary>Legt eine Einstellung an; leere Zahlen werden <c>null</c>.</summary>
        /// <param name="verfahren">Das Verfahren.</param>
        /// <param name="vorheizzeitH">t_V [h], 1 … 47; <c>null</c> = keine (Option 1 rechnet dann wie die Sollwertrampe).</param>
        /// <param name="toleranzart">Prozent oder kW.</param>
        /// <param name="toleranzWert">x [%] bzw. Δ [kW]; <c>null</c> = <see cref="TOLERANZ_VORGABE_PROZENT"/> %.</param>
        /// <param name="genauigkeitK">ε [K]; <c>null</c> = <see cref="GENAUIGKEIT_VORGABE_K"/>.</param>
        /// <param name="geltung">Gebäude oder Zone.</param>
        /// <param name="heizkreisEinbeziehen">„Gebäude mit Heizkreis einbeziehen“ (Vorgabe an).</param>
        public Vorheizvorgabe(Aufheizverfahren verfahren, int? vorheizzeitH = null,
                              Vorheiztoleranzart toleranzart = Vorheiztoleranzart.Prozent, double? toleranzWert = null,
                              double? genauigkeitK = null, Vorheizgeltung geltung = Vorheizgeltung.Gebaeude,
                              bool heizkreisEinbeziehen = true)
        {
            if (vorheizzeitH is int t && (t < VORHEIZZEIT_MIN_H || t > VORHEIZZEIT_MAX_H))
                throw new ArgumentOutOfRangeException(nameof(vorheizzeitH), "Die Vorheizzeit liegt zwischen 1 und 47 h.");
            Verfahren = verfahren;
            VorheizzeitH = vorheizzeitH;
            Toleranzart = toleranzart;
            ToleranzWert = Zahl(toleranzWert);
            if (ToleranzWert < 0.0) throw new ArgumentOutOfRangeException(nameof(toleranzWert), "Die Toleranz ist nicht negativ.");
            GenauigkeitK = Zahl(genauigkeitK);
            if (GenauigkeitK <= 0.0) throw new ArgumentOutOfRangeException(nameof(genauigkeitK), "Die Regelgenauigkeit ist positiv.");
            Geltung = geltung;
            HeizkreisEinbeziehen = heizkreisEinbeziehen;
        }

        /// <summary>Das Verfahren.</summary>
        public Aufheizverfahren Verfahren { get; }

        /// <summary>Die Vorheizzeit t_V [h] des Projekts; <c>null</c> = keine.</summary>
        public int? VorheizzeitH { get; }

        /// <summary>Die Art der Toleranz.</summary>
        public Vorheiztoleranzart Toleranzart { get; }

        /// <summary>Der Wert der Toleranz, wie eingegeben; <c>null</c> = Vorgabe.</summary>
        public double? ToleranzWert { get; }

        /// <summary>Die Regelgenauigkeit ε, wie eingegeben; <c>null</c> = Vorgabe.</summary>
        public double? GenauigkeitK { get; }

        /// <summary>Die Geltung.</summary>
        public Vorheizgeltung Geltung { get; }

        /// <summary>Der Heizkreis-Schalter des Projekts (F16 (a)).</summary>
        public bool HeizkreisEinbeziehen { get; }

        /// <summary>Die Vorgabe des Ankunftsbezugs: (b) gegen min(θ_T, θ_stat) − ε an einer Zone mit Heizkreis (F17 (b),
        /// Anwenderentscheid 11.10.2026, E125).</summary>
        internal const Vorheizankunftsbezug ANKUNFTSBEZUG_VORGABE = Vorheizankunftsbezug.Uebergabe;

        /// <summary>Der Ankunftsbezug eines Gebäudes mit Heizkreis (interne Wahl für Tests und Messung; Vorgabe F17 (b)).</summary>
        internal Vorheizankunftsbezug Ankunftsbezug { get; init; } = ANKUNFTSBEZUG_VORGABE;

        /// <summary>Rechnet die Einstellung Option 1 (Vorheizzeit vorgeben)?</summary>
        public bool IstVorgabe => Verfahren == Aufheizverfahren.Vorgabe;

        /// <summary>Die wirksame Regelgenauigkeit ε [K].</summary>
        public double GenauigkeitWirksamK => GenauigkeitK ?? GENAUIGKEIT_VORGABE_K;

        /// <summary>Gilt die Kern-Konstante der Toleranz (leeres Feld)?</summary>
        public bool ToleranzIstVorgabe => !ToleranzWert.HasValue;

        /// <summary>Ist die Toleranz null (x = 0 % bzw. Δ = 0 kW) — erlaubt, mit Hinweis (2.3)?</summary>
        public bool ToleranzNull => ToleranzWert == 0.0;

        /// <summary>
        /// <b>Der Deckel P_K [W]</b> aus Φ_K,max [W] (2.1, F3): (1 + x)·Φ_K,max bzw. Φ_K,max + Δ; leeres Feld = 20 %.
        /// </summary>
        public double DeckelW(double phiKMaxW)
        {
            if (Toleranzart == Vorheiztoleranzart.Kilowatt && ToleranzWert.HasValue)
                return phiKMaxW + ToleranzWert.Value * 1000.0;
            double x = (ToleranzWert ?? TOLERANZ_VORGABE_PROZENT) / 100.0;
            return (1.0 + x) * phiKMaxW;
        }

        private static double? Zahl(double? wert)
            => wert.HasValue && !double.IsNaN(wert.Value) && !double.IsInfinity(wert.Value) ? wert : null;
    }
}
