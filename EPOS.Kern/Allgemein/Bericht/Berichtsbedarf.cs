using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Bedarf eines Berichtslaufs</b> (Konzept Berichtsvorlagen 5.1, 8.5; Etappe BV-E3) — was der
    /// <see cref="BerichtsDatenSammler"/> über den Regellauf hinaus erhebt: die Stundenreihen des Laufs
    /// (<see cref="Zeitreihen"/>), den Kapitalwertverlauf der Wirtschaftlichkeit (<see cref="Verlauf"/>) und
    /// die Emissionsbilanz gekoppelt gegen getrennt (<see cref="Emissionsbilanz"/>). Simulation und
    /// Wirtschaftlichkeitsrechnung laufen immer; diese drei nur, wenn der Bericht sie zeigt.
    ///
    /// <para><b>Die Flags sind die des Katalogs</b> (<see cref="Vorlagenbedarf"/>): Ein Platzhalter nennt seinen
    /// Bedarf in <see cref="Vorlagenfeld.Bedarf"/>, ein Kapitel den seines Bausteins
    /// (<see cref="Berichtskapitel.Bedarf"/>) — eine zweite Liste gibt es nicht.</para>
    ///
    /// <para><b>Drei Wege zum Bedarf.</b> Die <see cref="Vorgabe"/> ist das Verhalten ohne Vorlage: die
    /// Vereinigung der angehakten Kapitel — Zeitreihen mit „Ergebnisse je Variante“, Verlauf und Emissionsbilanz
    /// mit „Wirtschaftlichkeit“. <see cref="AusVorlage"/> liest die Platzhalter einer geprüften Vorlage
    /// (<see cref="BerichtCtrl.PruefeVorStart"/>, <see cref="Startbefund.Bedarf"/>). <see cref="FuerLauf"/> bildet
    /// den Bedarf des Laufs aus Vorlage, Weg der Rückfrage und Mappe — die Mappe folgt den Häkchen.</para>
    ///
    /// <para><b>Nicht hier:</b> Die Stundenreihen, die die RECHNUNG braucht (ein gepflegter Strom-Leistungspreis
    /// bemisst sich an der Bezugsspitze), erhebt der Sammler unabhängig vom Bedarf
    /// (<see cref="KostenEmissionRechner.StromLeistungspreisGepflegt(int, IEnumerable{int})"/>) — der Bedarf sagt,
    /// was der Bericht zeigt, nicht, was die Zahlen richtig macht.</para>
    /// </summary>
    public sealed class Berichtsbedarf
    {
        /// <summary>Alle drei Flags.</summary>
        private const Vorlagenbedarf ALLE_FLAGS =
            Vorlagenbedarf.Zeitreihen | Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz;

        /// <summary>Legt den Bedarf aus den Flags des Katalogs an; fremde Bits fallen weg.</summary>
        public Berichtsbedarf(Vorlagenbedarf flags)
        {
            Flags = flags & ALLE_FLAGS;
        }

        /// <summary>Alles erheben — der Sammler ohne Angabe (<see cref="BerichtsDatenSammler.SammleFuerBericht(int, string, List{int}, Berichtsbedarf, IProgress{BerichtsDatenSammler.Fortschritt}, System.Threading.CancellationToken, Vergleichssicht)"/> mit <c>null</c>).</summary>
        public static Berichtsbedarf Alles { get; } = new Berichtsbedarf(ALLE_FLAGS);

        /// <summary>Nichts über den Regellauf hinaus.</summary>
        public static Berichtsbedarf Nichts { get; } = new Berichtsbedarf(Vorlagenbedarf.Keiner);

        /// <summary>Die Flags des Bedarfs.</summary>
        public Vorlagenbedarf Flags { get; }

        /// <summary>Die Stundenreihen des Laufs (<see cref="ZeitreihenSatz"/>).</summary>
        public bool Zeitreihen { get { return (Flags & Vorlagenbedarf.Zeitreihen) != 0; } }

        /// <summary>Der Kapitalwertverlauf aller drei Szenarien.</summary>
        public bool Verlauf { get { return (Flags & Vorlagenbedarf.Verlauf) != 0; } }

        /// <summary>Die Emissionsbilanz gekoppelte gegen getrennte Erzeugung.</summary>
        public bool Emissionsbilanz { get { return (Flags & Vorlagenbedarf.Emissionsbilanz) != 0; } }

        /// <summary>Die Vereinigung mit einem weiteren Bedarf; <c>null</c> ändert nichts.</summary>
        public Berichtsbedarf Mit(Berichtsbedarf weiterer)
        {
            return weiterer == null ? this : new Berichtsbedarf(Flags | weiterer.Flags);
        }

        /// <inheritdoc/>
        public override string ToString() { return Flags.ToString(); }

        /// <inheritdoc/>
        public override bool Equals(object obj) { return obj is Berichtsbedarf b && b.Flags == Flags; }

        /// <inheritdoc/>
        public override int GetHashCode() { return (int)Flags; }

        // =====================================================================
        //  Die drei Wege
        // =====================================================================

        /// <summary>
        /// <b>Die Vorgabe</b> — der Bedarf ohne Vorlage und der Bedarf der Mappe: die Vereinigung der Bedarfe
        /// der angehakten Kapitel (<see cref="Berichtskapitel.Bedarf"/>). Das ist das Verhalten vor BV-E3:
        /// Zeitreihen mit „Ergebnisse je Variante“ (BerichtSeiteGaben; Ganglinien im Wortbericht, Monatswerte
        /// der Detailblätter), Verlauf und Emissionsbilanz mit „Wirtschaftlichkeit“ (Kapitel und Blatt).
        /// <paramref name="konfig"/> <c>null</c> heißt alle Bausteine.
        /// </summary>
        public static Berichtsbedarf Vorgabe(BerichtsKonfiguration konfig)
        {
            Vorlagenbedarf b = Vorlagenbedarf.Keiner;
            foreach (Berichtskapitel k in Berichtskapitel.Alle)
                if (k.IstAktiv(konfig)) b |= k.Bedarf;
            return new Berichtsbedarf(b);
        }

        /// <summary>
        /// <b>Der Bedarf einer Vorlage</b> (Konzept 5.1): die Vereinigung der <see cref="Vorlagenfeld.Bedarf"/>
        /// der genutzten Schlüssel. Ein Kapitelplatzhalter <c>{{kapitel.&lt;name&gt;}}</c> trägt den Bedarf seines
        /// Bausteins, sofern sein Häkchen gesetzt ist — sonst schreibt die Engine an seiner Stelle nichts; der
        /// Sammelanker <c>{{bericht.inhalt}}</c> den der angehakten Kapitel, ebenso eine Vorlage ganz ohne
        /// Platzhalter (die Engine hängt ihr den Sammelanker an). Ist die Vorlage nicht lesbar, füllt der Lauf
        /// die Standardvorlage oder geht den bisherigen Weg — dann gilt die <see cref="Vorgabe"/>.
        ///
        /// <para><b>Die Vorlage bestimmt, WAS der Bericht zeigt — nicht, WIE eine gezeigte Zahl entsteht.</b> Die
        /// Zahlen der Wirtschaftlichkeit rechnen mit den Stundenreihen des Laufs, wo es sie gibt (Strommatrix,
        /// Aufteilung des KWK-Stroms, stündliche Einspeisung, das Konsistenz-Gate des Verlaufs bei Tarif und
        /// KWKG). Zeigt die Vorlage ein Kapitel am Häkchen „Wirtschaftlichkeit“ (Wirtschaftlichkeit, Anhang E),
        /// erhebt der Lauf die Reihen deshalb wie ohne Vorlage — nach dem Häkchen „Ergebnisse je Variante“
        /// (<see cref="Vorgabe"/>) —, auch wenn sie das Kapitel „Ergebnisse“ nicht führt; ebenso, wenn sie einen
        /// Einzelwert der Wirtschaftlichkeit führt (<see cref="IstWirtschaftswert"/>, BV-E4). Eine Vorlage ohne
        /// Zahl der Wirtschaftlichkeit (etwa nur ein Deckblatt) lässt sie weg.</para>
        /// </summary>
        public static Berichtsbedarf AusVorlage(Pruefbefund befund, BerichtsKonfiguration konfig)
        {
            if (befund == null || !befund.IstLesbar) return Vorgabe(konfig);

            Vorlagenbedarf b = Vorlagenbedarf.Keiner;
            bool sammelanker = befund.AnzahlPlatzhalter == 0;
            bool wirtschaft = false;
            foreach (string schluessel in befund.Schluessel ?? Array.Empty<string>())
            {
                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(schluessel);
                if (feld == null) continue;
                if (string.Equals(feld.Schluessel, WordVorlagenfueller.SAMMELANKER, StringComparison.Ordinal))
                {
                    sammelanker = true;
                    continue;
                }
                if (feld.Art == Vorlagenfeldart.Kapitel)
                {
                    Berichtskapitel k = Berichtskapitel.Finde(feld.Schluessel);
                    if (k != null && k.IstAktiv(konfig))
                    {
                        b |= feld.Bedarf;
                        wirtschaft |= IstWirtschaft(k);
                    }
                    continue;
                }
                b |= feld.Bedarf;
                // BV-E4: Eine Zahl der Wirtschaftlichkeit als Einzelwert entsteht wie im Kapitel.
                wirtschaft |= IstWirtschaftswert(feld.Schluessel);
            }
            if (sammelanker)
            {
                b |= Vorgabe(konfig).Flags;
                foreach (Berichtskapitel k in Berichtskapitel.Alle)
                    if (k.IstAktiv(konfig)) wirtschaft |= IstWirtschaft(k);
            }

            // Die Zahlen der Wirtschaftlichkeit wie ohne Vorlage: mit den Stundenreihen, wenn das Häkchen
            // „Ergebnisse je Variante“ sie verlangt (Kopfkommentar).
            if (wirtschaft && Vorgabe(konfig).Zeitreihen) b |= Vorlagenbedarf.Zeitreihen;
            return new Berichtsbedarf(b);
        }

        /// <summary>
        /// Die Bereiche der Einzelwerte der Wirtschaftlichkeit (Katalog v3, BV-E4): Zeilen je Stand, Stamm und beste
        /// Variante, Szenarientafel, Parameter und Szenarien der Gruppe — auch in der Paarsicht; dazu die Tabellen der
        /// Wirtschaftlichkeit (Katalog v4, BV-E5).
        /// </summary>
        private static readonly string[] Wirtschaftsbereiche =
        {
            "wirtschaft.", "stamm.wirtschaft.", "stand.wirtschaft.", "stand.bandbreite.",
            "stand.a.wirtschaft.", "stand.b.wirtschaft.", "stand.a.bandbreite.", "stand.b.bandbreite.",
            // BV-E5: die Tabellen der Wirtschaftlichkeit
            "tabelle.wirtschaft.", "tabelle.anhang_e.", "stand.tabelle.kwkg_module", "stand.tabelle.betriebskosten", "stand.tabelle.mehrjahres",
            "stand.tabelle.vermiedene_kosten", "stand.tabelle.sensitivitaet", "stand.tabelle.strommengen",
            "stand.tabelle.emissionsbilanz",
            // BV-E5: die Bilder der Wirtschaftlichkeit und ihre Schalter
            "bild.wirtschaft.", "stand.bild.zahlungsstrom", "hat.bild.wirtschaft.", "hat.bild.zahlungsstrom",
        };

        /// <summary>Ist der Schlüssel ein Einzelwert der Wirtschaftlichkeit (<see cref="Wirtschaftsbereiche"/>)?</summary>
        internal static bool IstWirtschaftswert(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return false;
            foreach (string b in Wirtschaftsbereiche)
                if (schluessel.StartsWith(b, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Hängt das Kapitel am Häkchen „Wirtschaftlichkeit“ (Wirtschaftlichkeit, Anhang E)?</summary>
        private static bool IstWirtschaft(Berichtskapitel k)
        {
            return string.Equals(k.Baustein, BerichtsKonfiguration.B_WIRTSCHAFT, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Der Bedarf eines Laufs</b> — was <c>BerichtSeiteGaben</c> an den Sammler reicht: der Bedarf der
        /// Vorlage, die der Word-Lauf füllt, vereinigt mit dem der Mappe. Ohne Startbefund (kein Word, oder die
        /// Vorprüfung scheiterte), mit dem Weg „Mit Standardvorlage“ und beim Rückfall auf den bisherigen Weg
        /// gilt die <see cref="Vorgabe"/> — die Standardvorlage führt jedes angehakte Kapitel über den
        /// Sammelanker. Der Vorlagenteil wird mit der Konfiguration DIESES Laufs gebildet: Ein Startbefund aus
        /// der Vorprüfung der Seite trägt die gespeicherte Auswahl (<see cref="Startbefund.Bedarf"/>), der Lauf
        /// die des Auftrags.
        /// </summary>
        /// <param name="konfig">Die Konfiguration des Laufs (Häkchen).</param>
        /// <param name="start">Die Vorprüfung des Word-Laufs; <c>null</c> = keine.</param>
        /// <param name="weg">Die Antwort der erweiterten Rückfrage.</param>
        /// <param name="mitExcel">Entsteht auch die Mappe? Sie folgt den Häkchen (<see cref="Vorgabe"/>).</param>
        public static Berichtsbedarf FuerLauf(BerichtsKonfiguration konfig, Startbefund start, Startweg weg, bool mitExcel)
        {
            Berichtsbedarf word;
            if (start == null || start.Wahl == null || start.Wahl.Eintrag == null) word = Vorgabe(konfig);
            else if (weg == Startweg.Standard && !start.Wahl.Eintrag.IstStandard) word = Vorgabe(konfig);
            else if (start.Wahl.Grund == Vorlagenwahlgrund.Rueckfall) word = Vorgabe(konfig);
            else word = AusVorlage(start.Pruefbefund, konfig);
            return mitExcel ? word.Mit(Vorgabe(konfig)) : word;
        }
    }
}
