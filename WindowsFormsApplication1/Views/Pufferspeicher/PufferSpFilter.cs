using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Volumen- und Herstellerfilter der beiden Pufferspeicher-Katalogdialoge
    /// (<see cref="Form_PufferSp"/> und <see cref="Form_PufferSp_Admin"/>).
    ///
    /// <para>
    /// <b>Warum zentral (Paket 9 / L5).</b> Die sechs Filterstufen standen in beiden
    /// Dialogen doppelt — einmal als <c>Items.Add</c>-Folge und einmal als
    /// <c>if/else</c>-Kette, die den ANGEZEIGTEN Text gegen deutsche Literale verglich.
    /// Genau daraus entstand Bestandsfehler <b>B0-10</b>: Ohne Treffer blieb der
    /// Volumenteil des Prädikats leer, das SQL endete auf „… and  order by …" und die
    /// Liste blieb stumm leer. Der B0-Fix hat die Vorbelegung ergänzt, aber die
    /// Textvergleiche stehen gelassen — mit lokalisierten Einträgen hätten sie ab jetzt
    /// NIE mehr getroffen und der Filter wäre wirkungslos geworden.
    /// </para>
    ///
    /// <para>
    /// <b>Drei-Schichten-Regel.</b> Die Auswahl entscheidet über den
    /// <see cref="ComboBox.SelectedIndex"/> — sprachneutral, Schicht 2. Der angezeigte
    /// Text ist reine Anzeige und kommt aus dem Ressourcenkatalog; die SQL-Fragmente
    /// sind Persistenz und stehen ausschließlich hier.
    /// </para>
    /// </summary>
    internal static class PufferSpFilter
    {
        /// <summary>
        /// SQL-Prädikat je Filterstufe, in der Reihenfolge der Einträge, die
        /// <see cref="VolumenfilterFuellen"/> anlegt. Index 0 = „Alle".
        ///
        /// <para>
        /// <b>NULL-Absicherung in Stufe 0 (Paket-9-Nacharbeit).</b> Der Bestandsausdruck
        /// <c>Gesamtvolumen Like '%'</c> wandelt die Zahl in Text und vergleicht; für
        /// <c>NULL</c> ergibt das in Jet/ACE wieder <c>NULL</c> — der Satz fällt also aus
        /// „Alle" heraus. Ein Katalogsatz ohne gepflegtes Gesamtvolumen (etwa aus einem
        /// VDI-3805-Import) wäre damit im Dialog unsichtbar, ohne dass irgendwo eine
        /// Meldung erscheint. Die Klammer ist nötig, weil die Aufrufer das Prädikat mit
        /// <c>and</c> an den Herstellerfilter hängen.
        /// Die übrigen fünf Stufen bleiben wortgleich zum Bestand.
        /// </para>
        /// </summary>
        private static readonly string[] VOLUMEN_SQL =
        {
            "(Gesamtvolumen IS NULL OR Gesamtvolumen Like '%')",
            "Gesamtvolumen <100",
            "Gesamtvolumen >=100 and Gesamtvolumen <200",
            "Gesamtvolumen >=200 and Gesamtvolumen <500",
            "Gesamtvolumen >=500 and Gesamtvolumen <1000",
            "Gesamtvolumen >=1000"
        };

        /// <summary>Die Anzeigetexte der sechs Filterstufen in derselben Reihenfolge.</summary>
        private static string[] VolumenTexte()
        {
            return new[]
            {
                MyResource.Resource.PSP_FILTER_ALLE,
                MyResource.Resource.PSP_FILTER_BIS_100L,
                MyResource.Resource.PSP_FILTER_100_BIS_200L,
                MyResource.Resource.PSP_FILTER_200_BIS_500L,
                MyResource.Resource.PSP_FILTER_500_BIS_1000L,
                MyResource.Resource.PSP_FILTER_UEBER_1000L
            };
        }

        /// <summary>
        /// Füllt den Volumenfilter und stellt ihn auf „Alle". Bewusst über
        /// <c>SelectedIndex</c> statt über <c>Text</c>: Damit stimmen Anzeige und
        /// Auswahlindex von Anfang an überein — der Index ist der Steuerwert.
        ///
        /// <para>
        /// <b>Das Auslösen von <c>SelectedIndexChanged</c> ist gewollt</b> und wurde in
        /// der Paket-9-Nacharbeit ausdrücklich NICHT unterdrückt. Zwei Messungen dazu:
        /// </para>
        /// <list type="bullet">
        /// <item>Die Bestandsvorbelegung <c>comboBox_Volumen.Text = "Alle"</c> löste das
        /// Ereignis <b>ebenfalls genau einmal</b> aus (der <c>Text</c>-Setzer der ComboBox
        /// sucht den Eintrag und setzt <c>SelectedIndex</c>). Der Aufruf von
        /// <c>SetFilter()</c> beim Öffnen ist also kein Zugewinn aus Paket 9 —
        /// Sortierung und Trefferliste sind unverändert.</item>
        /// <item><c>Form_PufferSp_Load</c> füllt die rechte Liste zunächst aus
        /// <c>Tab_Pufferspeicher</c> (Projekttabelle!); erst <c>SetFilter()</c> ersetzt
        /// sie durch den KATALOG <c>Tab_Pufferspeicher_STAMM</c>. Würde man das Ereignis
        /// beim Füllen abklemmen, stünde beim Öffnen die falsche Tabelle im Dialog.</item>
        /// </list>
        /// </summary>
        public static void VolumenfilterFuellen(ComboBox cb)
        {
            if (cb == null) return;

            cb.Items.Clear();
            cb.Items.AddRange(VolumenTexte());
            cb.SelectedIndex = 0;   // "Alle"
        }

        /// <summary>
        /// Das SQL-Prädikat zur aktuellen Auswahl. Freitext (die ComboBox ist
        /// editierbar) und eine leere Auswahl liefern „alle Volumina" — dieselbe
        /// Vorbelegung wie nach dem B0-10-Fix, jetzt aber sprachunabhängig.
        /// </summary>
        public static string VolumenSql(ComboBox cb)
        {
            if (cb == null) return VOLUMEN_SQL[0];

            int index = cb.SelectedIndex;
            if (index < 0 || index >= VOLUMEN_SQL.Length)
            {
                // Freitext: über den angezeigten Text versuchen, sonst "alle".
                index = Array.FindIndex(VolumenTexte(),
                    t => string.Equals(t, (cb.Text ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
                if (index < 0) return VOLUMEN_SQL[0];
            }

            return VOLUMEN_SQL[index];
        }

        /// <summary>
        /// Das SQL-Prädikat des Herstellerfilters. „Alle" und die leere Eingabe stehen
        /// für „ohne Einschränkung".
        ///
        /// Der Herstellerfilter kennt keinen festen Eintrag „Alle" — die Liste kommt aus
        /// den Stammdaten, der Text wird nur vorbelegt (Bestand). Verglichen wird
        /// deshalb weiterhin gegen einen Text, aber gegen den RESSOURCENWERT und nicht
        /// mehr gegen ein deutsches Literal; damit passen Vorbelegung und Vergleich in
        /// jeder Sprache zusammen.
        /// </summary>
        public static string HerstellerSql(ComboBox cb)
        {
            string text = cb == null ? "" : (cb.Text ?? "").Trim();

            if (text.Length == 0 ||
                string.Equals(text, MyResource.Resource.PSP_FILTER_ALLE, StringComparison.OrdinalIgnoreCase))
                return "Hersteller Like '%'";

            // Bestand: einfaches Anführungszeichen verdoppeln, damit ein Herstellername
            // mit Apostroph das Prädikat nicht zerreißt.
            return "Hersteller='" + text.Replace("'", "''") + "'";
        }

        /// <summary>Vorbelegung des Herstellerfilters („Alle").</summary>
        public static void HerstellerfilterVorbelegen(ComboBox cb)
        {
            if (cb != null) cb.Text = MyResource.Resource.PSP_FILTER_ALLE;
        }
    }
}
