using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine benannte Ablehnung im Ergebnis: Zone (leer = Projekt), Grund und der Satz als Kennung
    /// und Werte (N11 (k)). Die betroffene Zone — bzw. bei leerer Zone die Zirkulation — trägt 0
    /// (Konzept 2.2).
    /// </summary>
    internal sealed record ZapfAblehnung(string Zone, ZapfEingabefehler Grund, ZapfSatz Satz)
    {
        /// <summary>Der deutsche Wortlaut des Satzes (Protokoll, Test).</summary>
        public string Klartext => Satz?.Klartext ?? "";

        /// <summary>Die Kennung des Satzes; <c>null</c> ohne Satz.</summary>
        public string Kennung => Satz?.Kennung;

        /// <summary>Die Werte des Satzes, sprachfrei und getrennt; <c>null</c> ohne Satz.</summary>
        public IReadOnlyList<object> Argumente => Satz?.Werte;

        /// <summary>Die Ablehnung einer Zone aus der benannten Ausnahme des Rechenwegs — samt Satz.</summary>
        internal static ZapfAblehnung Aus(string zone, ZapfprofilEingabeException ex)
            => new ZapfAblehnung(zone, ex.Fehler, ex.Satz);

        /// <summary>Die Ablehnung einer Zone aus einem fehlenden Parameter — samt Satz.</summary>
        internal static ZapfAblehnung Aus(string zone, ParametersatzException ex)
            => new ZapfAblehnung(zone, ZapfEingabefehler.ParameterFehlt, ex.Satz);
    }

    /// <summary>
    /// Das Ergebnis einer Zone: ihre Bilanzreihen Zapfung und Zirkulation und die Kennzahlen
    /// der Zone (4.6). Eine abgelehnte Zone trägt Nullreihen und <see cref="Abgelehnt"/>.
    /// </summary>
    internal sealed record ZonenErgebnis
    {
        public int IdZone { get; init; }
        public string Zone { get; init; } = "";
        public Bilanzreihe Zapfung { get; init; }
        public Bilanzreihe Zirkulation { get; init; }
        public bool Abgelehnt { get; init; }

        /// <summary>Wirksame Bezugsmenge in der Bezugsart der Nutzungsart.</summary>
        public double Bezugsmenge { get; init; }

        /// <summary>Jahres-Nutzenergie der Zapfung [kWh/a] nach Kalibrierung.</summary>
        public double JahresbedarfZapfungKwh { get; init; }

        /// <summary>Jahresverlust der Zirkulation, Anteil der Zone [kWh/a].</summary>
        public double JahresverlustZirkulationKwh { get; init; }

        /// <summary>Spezifischer Jahreswert [kWh je Einheit und Jahr].</summary>
        public double SpezifischKwhJeEinheitJahr { get; init; }

        /// <summary>Tagesmittel der Zapfung in Litern bei der Anzeigetemperatur; <c>null</c> ohne Anzeigetemperatur.</summary>
        public double? ZapfungLiterJeTag { get; init; }

        /// <summary>Temperaturfaktor f_θ der Zone.</summary>
        public double Temperaturfaktor { get; init; } = 1.0;

        /// <summary>Faktor der Kalibrierung; <c>null</c> ohne Messwert.</summary>
        public double? Kalibrierfaktor { get; init; }

        /// <summary>
        /// Der wirksame Tagtypkalender der Zone (365 Tage), mit dem ihre Zapfreihe gerechnet ist —
        /// samt ihren Ruhetagen (eigene Ferien oder die des gebundenen Gebäudes, N8 c). Die
        /// Vorschau mittelt ihren Tagesgang damit (5.1). <c>null</c>, wenn die Zone abgelehnt
        /// wurde, bevor ihr Kalender stand.
        /// </summary>
        public IReadOnlyList<ZapfTagtyp> Kalender { get; init; }

        /// <summary>
        /// Die Konsistenzprobe der stochastischen Jahresreihe (4.4) — Mittel der R Jahre gegen den
        /// deterministischen Pfad mit Toleranz, dazu der Faktor der Energieprobe, der die Realisierung
        /// zum Seed (die Zapfreihe der Zone) auf die Jahresmenge bringt; <c>null</c> auf dem
        /// deterministischen Weg und bei einer abgelehnten Zone.
        /// </summary>
        public Jahreskonsistenz Konsistenz { get; init; }
    }

    /// <summary>
    /// <b>Die Kennzahlen der Bilanz</b> (Konzept 4.6), Einheit im Namen. Keine dieser Zahlen geht
    /// in die Auslegung; der größte Stundenwert trägt den Vermerk <see cref="VERMERK_STUNDENWERT"/>.
    /// </summary>
    internal sealed record Zapfkennzahlen
    {
        /// <summary>Die Kennung des Vermerks am größten Stundenwert („Bilanzwert, keine Auslegungsgröße").</summary>
        internal const string VERMERK_STUNDENWERT = "VERMERK_STUNDENWERT";

        /// <summary>Jahresbedarf der Zapfung [kWh/a].</summary>
        public double JahresbedarfZapfungKwh { get; init; }

        /// <summary>Jahresverlust der Zirkulation [kWh/a].</summary>
        public double JahresverlustZirkulationKwh { get; init; }

        /// <summary>Zapfung und Zirkulation zusammen [kWh/a].</summary>
        public double JahresbedarfGesamtKwh { get; init; }

        /// <summary>Anteil der Zirkulation an Zapfung + Zirkulation [-]; 0 ohne Bedarf.</summary>
        public double Zirkulationsanteil { get; init; }

        /// <summary>Tagesmittel der Zapfung [kWh/d].</summary>
        public double TagesmittelZapfungKwh { get; init; }

        /// <summary>Tagesmittel der Zapfung in Litern bei der Anzeigetemperatur; <c>null</c> ohne Anzeigetemperatur.</summary>
        public double? ZapfungLiterJeTag { get; init; }

        /// <summary>Größter Stundenwert von Zapfung + Zirkulation [kW] — Bilanzwert, keine Auslegungsgröße.</summary>
        public double GroessterStundenwertKw { get; init; }

        /// <summary>Der Vermerk zum größten Stundenwert als Satz (N11 (k)).</summary>
        public ZapfSatz VermerkGroessterStundenwert { get; init; } = ZapfSatz.Neu(VERMERK_STUNDENWERT);

        /// <summary>Volllaststunden [h/a] = Jahresbedarf gesamt / größter Stundenwert; 0 ohne Bedarf.</summary>
        public double VolllaststundenH { get; init; }

        /// <summary>Stunden von Zapfung + Zirkulation über der Schwelle; <c>null</c> ohne Schwelle.</summary>
        public int? StundenUeberSchwelle { get; init; }

        /// <summary>Die Schwelle dieser Zählung [kW]; <c>null</c> ohne Schwelle.</summary>
        public double? SchwelleKw { get; init; }
    }

    /// <summary>
    /// <b>Das Ergebnis des Bilanzrechenwegs</b> (Konzept 2.1, Fassade): die Summenreihen
    /// Zapfung und Zirkulation, je Zone ihre Reihen, die Kennzahlen, das Herkunftsprotokoll, die
    /// Hinweise und die benannten Ablehnungen. Unveränderlich.
    /// </summary>
    internal sealed record ZapfprofilErgebnis
    {
        /// <summary>Rechnet die Jahresreihe stochastisch (Realisierung zum Seed, 4.4) statt deterministisch?</summary>
        public bool Stochastisch { get; init; }

        /// <summary>Summe der Zapfreihen aller Zonen.</summary>
        public Bilanzreihe Zapfung { get; init; }

        /// <summary>Summe der Zirkulationsreihen (Zonenanteile und gebäudeweiter Rest).</summary>
        public Bilanzreihe Zirkulation { get; init; }

        /// <summary>Die Zonen in der Reihenfolge des Eingangs.</summary>
        public IReadOnlyList<ZonenErgebnis> JeZone { get; init; } = new ZonenErgebnis[0];

        /// <summary>
        /// Der Ansatz der Zirkulation VOR der Kalibrierung; <c>null</c>, wenn sie abgelehnt wurde.
        /// Der verbuchte Jahresverlust ist <see cref="Zapfkennzahlen.JahresverlustZirkulationKwh"/>
        /// — nach einer Kalibrierung mit Grenze 2 oder 3 weicht er vom Ansatz ab (N7).
        /// </summary>
        public Zirkulationsansatz Zirkulationsansatz { get; init; }

        /// <summary>Die Laufzeitbelegung der 24 Tagesstunden; <c>null</c> ohne Zirkulationsansatz.</summary>
        public IReadOnlyList<double> Laufzeitfenster { get; init; }

        /// <summary>Die Kennzahlen (4.6).</summary>
        public Zapfkennzahlen Kennzahlen { get; init; }

        /// <summary>Das Herkunftsprotokoll je Wert.</summary>
        public IReadOnlyList<Herkunftseintrag> Herkunft { get; init; } = new Herkunftseintrag[0];

        /// <summary>Nicht blockierende Hinweise.</summary>
        public IReadOnlyList<ZapfHinweis> Hinweise { get; init; } = new ZapfHinweis[0];

        /// <summary>Benannte Ablehnungen; die betroffene Zone bzw. die Zirkulation trägt 0.</summary>
        public IReadOnlyList<ZapfAblehnung> Ablehnungen { get; init; } = new ZapfAblehnung[0];

        /// <summary>Ist alles gerechnet, ohne Ablehnung?</summary>
        public bool Vollstaendig => Ablehnungen.Count == 0;
    }
}
