namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Preisbestandteile EINES Brennstoffpreises, je Zeile von
    // energy_project_settings (Konzept BHKW-Wirtschaftlichkeit § 5.1, Etappe B2
    // Paket A; Spalten aus SchemaMigration Schritt 60).
    //
    // Abgrenzung zum ARBEITSPREIS: Der steht weiterhin in energy_price bzw.
    // energy_project_settings.custom_price_work und wird von ucFuelSettings gepflegt.
    // Hier stehen die Anteile, aus denen er BESTEHT.
    //
    // Abgrenzung zum StromAufschlagModel — und der Unterschied ist fachlich, nicht
    // formal:
    //
    //   * Der Strom-Block ist ein AUFSCHLAG. Netzentgelt, Umlagen, Stromsteuer,
    //     Konzession und Vertrieb werden auf den Bezugspreis ADDIERT.
    //   * Dieser Block ist eine ZERLEGUNG. Energiesteuer, CO2-Anteil, Netz-/
    //     Messentgelt und Vertrieb sind bereits IM erfassten Preis enthalten; sie
    //     werden sichtbar gemacht, nicht aufgeschlagen (Konzept 4.1, Leitentscheidung
    //     BW1). Deshalb gibt es hier auch keine Override-Spalte: Der erfasste Preis
    //     ist der Gesamtwert.
    //
    // Alle Werte in ct/kWh.
    // ---------------------------------------------------------------------------
    public class BrennstoffBestandteilModel
    {
        // --- Zeilenbezug ---

        /// <summary>Projekt (energy_project_settings.ID_Projekt).</summary>
        public int ID_Projekt;

        /// <summary>Energieträger (energy_project_settings.ID_Energieträger).</summary>
        public int ID_Energietraeger;

        // --- Bestandteile (Werte in ct/kWh) ---
        //
        // KEINE VORSCHLAGSWERTE. Alle vier stehen auf null, und null heisst „kein
        // Anteil erfasst" — nicht „nicht gepflegt, also Vorschlagswert".
        //
        // Das ist der eine entscheidende Unterschied zum StromAufschlagModel, dessen
        // Felder mit den Vorschlagswerten des Fachkonzepts vorbelegt sind und deren
        // Leseweg NULL wieder auf genau diese Werte zurückfallen lässt — bei Projekt
        // 1030 gemessene 11,746 ct/kWh trotz fünf abgeschalteter Flags (E5-Falle,
        // Konzept § 5.1). Für einen Brennstoff wäre eine solche Vorbelegung eine
        // Behauptung über die Lieferantenrechnung des Anwenders: Wieviel Energiesteuer
        // in seinem Gaspreis steckt, weiss allein er. Ein Vorschlagssatz kommt deshalb
        // nur über die Schnellwahl des Dialogs (Katalogsatz des Jahres, Konzept § 6.2)
        // ins Feld — und dann steht er als gepflegter Wert da, nicht als stiller
        // Rückfall.
        //
        // Fachlich hängt daran die Kohärenzprüfung (BW2): Sie darf eine
        // Steuerentlastung nur dann als konsistent ausweisen, wenn dieselbe Steuer im
        // Preis als Belastung enthalten ist. Ein unterstellter Anteil machte diese
        // Prüfung wertlos, weil dann jeder Preis die Steuer „enthielte".

        /// <summary>Energiesteueranteil [ct/kWh]; null = kein Anteil erfasst.</summary>
        public double? Energiesteuer;
        public bool Energiesteuer_Aktiv;

        /// <summary>CO₂-Anteil nach BEHG [ct/kWh]; null = kein Anteil erfasst.</summary>
        public double? CO2;
        public bool CO2_Aktiv;

        /// <summary>Netz-/Messentgelt [ct/kWh]; null = kein Anteil erfasst.</summary>
        public double? Netzentgelt;
        public bool Netzentgelt_Aktiv;

        /// <summary>Vertrieb [ct/kWh]; null = kein Anteil erfasst.</summary>
        public double? Vertrieb;
        public bool Vertrieb_Aktiv;

        // --- Modus ---

        /// <summary>
        /// Werte aus <see cref="DbWerte"/>.SP_AUFSCHLAG_MODUS_* — dieselben zwei
        /// Persistenzwerte wie beim Strom, kein zweites Vokabular für dieselbe
        /// Unterscheidung.
        ///
        /// <para>Vorgabe ist <b>Gesamtwert</b>: „Der erfasste Preis ist der Preis, die
        /// Bestandteile sind Ausweis." Das ist der Wert, der nichts auslöst, und
        /// zugleich der Regelfall einer Lieferantenrechnung. Beim Strom ist die Vorgabe
        /// umgekehrt <c>Aufgeschluesselt</c> — dort sind die Komponenten ein Aufschlag
        /// auf einen Nettopreis, hier eine Zerlegung eines Bruttopreises.</para>
        /// </summary>
        public string Modus = DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT;

        /// <summary>
        /// true, wenn die Zeile aus der Datenbank stammt. false heisst: Es gab keine
        /// Zeile (oder die Spalten fehlen noch). Die Oberfläche weist das aus, statt
        /// eine Pflege vorzutäuschen.
        /// </summary>
        public bool AusDatenbank;

        public BrennstoffBestandteilModel()
        {
        }
    }
}
