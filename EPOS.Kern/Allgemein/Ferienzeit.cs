using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FERIENZEITEN eines Gebäudekatalogsatzes — Umrechnung Tag/Monat ↔ Jahrestag und
    /// die vier Prüfregeln (iU9-W9.0c).
    ///
    /// <para><b>Herkunft.</b> Alles stand in <c>Views/Gebäude/Form_Gebaeude2.cs</c>:
    /// <c>JahrestagUmrechner</c> (:73-82) schrieb Tag und Monat in zwei
    /// <c>Control</c>-Felder, <c>BerechneJahrestag</c> (:83-104) rechnete zurück, und
    /// <c>btn_Speichern_Click</c> (:177-198) prüfte die vier Zeiträume. Die drei sind
    /// reine Rechnung ohne Oberfläche und ohne Datenbank — sie gehören in den Kern und
    /// sind dort mit xunit prüfbar.</para>
    ///
    /// <para><b>Der Jahrestag zählt ab 1.</b> Der 1. Januar ist Tag 1
    /// (<c>differenz.Days + 1</c>). Gerechnet wird immer im LAUFENDEN Jahr — das ist
    /// Bestand und hat eine Folge: In einem Schaltjahr liegt derselbe Kalendertag ab dem
    /// 1. März einen Tag später als im Gemeinjahr. Der Katalogsatz speichert die Zahl,
    /// nicht das Datum; wörtlich übernommen (Regel F3).</para>
    ///
    /// <para><b>0 und 366 heißen „keine Angabe".</b> Beim Anzeigen bleiben beide Felder
    /// leer, beim Speichern ergibt ein leeres Feld die 0 — und ein Winterferienbeginn 0
    /// wird zum Schluss auf 366 gehoben, damit die Simulation ihn hinter das Jahresende
    /// legt (<c>btn_Speichern_Click</c>:198).</para>
    /// </summary>
    public static class Ferienzeit
    {
        /// <summary>
        /// Tag und Monat → Jahrestag. Leere, nicht numerische oder unmögliche Angaben
        /// ergeben 0 („kein Ferientag") — wörtlich <c>BerechneJahrestag</c>:83-104.
        /// </summary>
        /// <param name="monat">Monatsnummer 1…12 als Text.</param>
        /// <param name="tag">Tag im Monat 1…31 als Text.</param>
        public static int Jahrestag(string monat, string tag)
        {
            if (string.IsNullOrWhiteSpace(monat) || string.IsNullOrWhiteSpace(tag)) return 0;

            int m, t;
            if (!ZahlText.GanzzahlParsen(monat, out m) || !ZahlText.GanzzahlParsen(tag, out t)) return 0;
            if (m < 1 || m > 12 || t < 1 || t > 31) return 0;

            try
            {
                DateTime jahresanfang = new DateTime(DateTime.Now.Year, 1, 1);
                DateTime datum = new DateTime(DateTime.Now.Year, m, t);
                return (datum - jahresanfang).Days + 1;
            }
            catch
            {
                // Unmoegliches Datum (z.B. 30. Februar) -> kein Ferientag.
                return 0;
            }
        }

        /// <summary>
        /// Dieselbe Rechnung mit Zahlen statt Text — der Weg der Razor-Komponente, deren
        /// beide Felder <c>Ganzzahlfeld</c> sind und schon geparst haben.
        /// </summary>
        public static int Jahrestag(int? monat, int? tag)
        {
            if (!monat.HasValue || !tag.HasValue) return 0;
            return Jahrestag(monat.Value.ToString(), tag.Value.ToString());
        }

        /// <summary>
        /// Jahrestag → (Tag, Monat). 0 und 366 sind „keine Angabe" und liefern
        /// <c>(null, null)</c>; so ließ <c>JahrestagUmrechner</c>:73-82 beide Felder leer.
        /// </summary>
        public static (int? Tag, int? Monat) TagUndMonat(int jahrestag)
        {
            if (jahrestag == 0 || jahrestag == 366) return (null, null);

            DateTime jahresanfang = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime datum = jahresanfang.AddDays(jahrestag - 1);
            return (datum.Day, datum.Month);
        }

        /// <summary>
        /// Die VIER Prüfregeln aus <c>btn_Speichern_Click</c>:177-196, in derselben
        /// Reihenfolge. Rückgabe ist der Schlüssel der Meldung, oder <c>null</c>, wenn
        /// alles stimmt.
        ///
        /// <para><b>Winter geht über die Jahresgrenze.</b> Die erste Regel lautet
        /// <c>Ferienbeginn_1 &lt; Ferienende_1</c> ⇒ Fehler — Winterferien beginnen im
        /// Dezember und enden im Januar, der Beginn muss also die GRÖSSERE Zahl sein.
        /// Ostern, Sommer und Herbst liegen innerhalb des Jahres und prüfen umgekehrt
        /// (<c>Beginn &gt; Ende</c> ⇒ Fehler).</para>
        ///
        /// <para><b>Gleichheit ist erlaubt</b> — bei allen vier. Zwei leere Zeiträume
        /// (0/0) sind damit gültig, und genau das ist der Regelfall eines Katalogsatzes
        /// ohne Ferien.</para>
        /// </summary>
        /// <param name="beginn">Vier Jahrestage: Winter, Ostern, Sommer, Herbst.</param>
        /// <param name="ende">Dieselben vier als Ende.</param>
        /// <returns>
        /// <see cref="MELDUNG_WINTER"/>, <see cref="MELDUNG_OSTERN"/>,
        /// <see cref="MELDUNG_SOMMER"/>, <see cref="MELDUNG_HERBST"/> oder <c>null</c>.
        /// </returns>
        public static string Pruefen(int[] beginn, int[] ende)
        {
            if (beginn == null || ende == null || beginn.Length < 4 || ende.Length < 4)
                return null;

            if (beginn[0] < ende[0]) return MELDUNG_WINTER;
            if (beginn[1] > ende[1]) return MELDUNG_OSTERN;
            if (beginn[2] > ende[2]) return MELDUNG_SOMMER;
            if (beginn[3] > ende[3]) return MELDUNG_HERBST;
            return null;
        }

        /// <summary>
        /// Der Winterferienbeginn 0 wird 366 (<c>btn_Speichern_Click</c>:198): So liegt er
        /// hinter dem Jahresende und die Simulation findet keinen Wintertag.
        /// </summary>
        public static int WinterbeginnGehoben(int jahrestag)
        {
            return jahrestag == 0 ? 366 : jahrestag;
        }

        /// <summary>Ressourcenschlüssel „Die Ferien müssen über die Jahresgrenze gehen!"</summary>
        public const string MELDUNG_WINTER = "GEBK_MSG_FERIEN_WINTER";

        /// <summary>Ressourcenschlüssel „Fehler: Bei der Eingabe der Osterferien!"</summary>
        public const string MELDUNG_OSTERN = "GEBK_MSG_FERIEN_OSTERN";

        /// <summary>Ressourcenschlüssel „Fehler: Bei der Eingabe der Sommerferien!"</summary>
        public const string MELDUNG_SOMMER = "GEBK_MSG_FERIEN_SOMMER";

        /// <summary>Ressourcenschlüssel „Fehler: Bei der Eingabe der Herbstferien!"</summary>
        public const string MELDUNG_HERBST = "GEBK_MSG_FERIEN_HERBST";
    }
}
