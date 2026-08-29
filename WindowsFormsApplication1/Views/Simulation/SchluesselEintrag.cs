namespace WindowsFormsApplication1
{
    /// <summary>
    /// PAKET Q1 (Konzept 8.1 Punkt 4, „Schlüssel- statt Indexkopplung"): ein Eintrag
    /// einer Auswahlliste, der seinen STEUERWERT selbst trägt.
    ///
    /// <para><b>Wogegen er hilft.</b> Die Wärmequellen-Auswahl las bis Q1 ihren
    /// Steuerwert über <c>SelectedIndex</c> aus einer ZWEITEN Liste
    /// (<c>WaermequelleClass.TypWerte</c> neben <c>TypAnzeige</c>). Beide mussten
    /// zeichengenau dieselbe Reihenfolge haben — die Klasse trug dafür eine ausdrückliche
    /// Warnung („neue Wärmequellen immer ANHÄNGEN, nie einfügen oder umsortieren"),
    /// weil ein Umsortieren Bestandsprojekte still auf die falsche Quelle gezeigt hätte.
    /// Mit dem Wert am Eintrag gibt es diese Kopplung nicht mehr; die Reihenfolge der
    /// Liste ist wieder eine reine Anzeigefrage.</para>
    ///
    /// <para><b>Drei-Schichten-Regel:</b> <see cref="Wert"/> ist der Steuerwert
    /// (Persistenzwert aus <c>DbWerte</c> oder ein sprachneutraler Schlüssel bzw. eine
    /// ID), <see cref="ToString"/> liefert den lokalisierten Anzeigetext. Eine ComboBox
    /// zeigt genau diesen; kein Anzeigetext wird je zum Steuerwert.</para>
    /// </summary>
    internal sealed class SchluesselEintrag
    {
        /// <summary>Der Steuerwert — Zeichenkette aus <c>DbWerte</c> oder eine ID.</summary>
        public readonly object Wert;

        private readonly string _anzeige;

        public SchluesselEintrag(object wert, string anzeige)
        {
            Wert = wert;
            _anzeige = anzeige ?? "";
        }

        /// <summary>Der lokalisierte Anzeigetext — das, was die Auswahlliste zeigt.</summary>
        public override string ToString() { return _anzeige; }
    }
}
