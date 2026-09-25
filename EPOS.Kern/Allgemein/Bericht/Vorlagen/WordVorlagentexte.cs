using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Texte der Word-Engine</b> (Konzept Berichtsvorlagen 4.10): Fundorte, Gründe und
    /// Meldungen des <see cref="Fuellergebnis"/>. Deutsch stehen sie hier, als Schlüssel der
    /// <see cref="BerichtTexte"/>-Liste, die die englische Fassung trägt; die Wache in
    /// <c>WordVorlagenfuellerTests</c> hält jeden Text dort fest. In den Bericht selbst schreibt die
    /// Engine in BV-E1 keinen eigenen Text — nur Werte und den Leerwert „—“ des Katalogs.
    /// </summary>
    public static class WordVorlagentexte
    {
        // ------------------------------------------------------------- Fundorte

        /// <summary>Fundort im Rumpf.</summary>
        public const string FUNDORT_RUMPF = "Rumpf, Absatz {0}";

        /// <summary>Fundort in der Kopfzeile eines Abschnitts.</summary>
        public const string FUNDORT_KOPF = "Kopfzeile (Abschnitt {0}), Absatz {1}";

        /// <summary>Fundort in der Kopfzeile der ersten Seite.</summary>
        public const string FUNDORT_KOPF_ERSTE = "Kopfzeile (Abschnitt {0}, erste Seite), Absatz {1}";

        /// <summary>Fundort in der Kopfzeile der geraden Seiten.</summary>
        public const string FUNDORT_KOPF_GERADE = "Kopfzeile (Abschnitt {0}, gerade Seiten), Absatz {1}";

        /// <summary>Fundort in einer Kopfzeile, auf die kein Abschnitt verweist.</summary>
        public const string FUNDORT_KOPF_OHNE = "Kopfzeile, Absatz {0}";

        /// <summary>Fundort in der Fußzeile eines Abschnitts.</summary>
        public const string FUNDORT_FUSS = "Fußzeile (Abschnitt {0}), Absatz {1}";

        /// <summary>Fundort in der Fußzeile der ersten Seite.</summary>
        public const string FUNDORT_FUSS_ERSTE = "Fußzeile (Abschnitt {0}, erste Seite), Absatz {1}";

        /// <summary>Fundort in der Fußzeile der geraden Seiten.</summary>
        public const string FUNDORT_FUSS_GERADE = "Fußzeile (Abschnitt {0}, gerade Seiten), Absatz {1}";

        /// <summary>Fundort in einer Fußzeile, auf die kein Abschnitt verweist.</summary>
        public const string FUNDORT_FUSS_OHNE = "Fußzeile, Absatz {0}";

        /// <summary>Fundort in den Fußnoten.</summary>
        public const string FUNDORT_FUSSNOTEN = "Fußnoten, Absatz {0}";

        /// <summary>Fundort in den Endnoten.</summary>
        public const string FUNDORT_ENDNOTEN = "Endnoten, Absatz {0}";

        /// <summary>Zusatz: in einer Tabellenzelle.</summary>
        public const string FUNDORT_TABELLE = " · Tabelle {0}, Zeile {1}, Spalte {2}";

        /// <summary>Zusatz: in einem Textfeld.</summary>
        public const string FUNDORT_TEXTFELD = " · Textfeld";

        /// <summary>Zusatz: ein Inhaltssteuerelement.</summary>
        public const string FUNDORT_SDT = " · Inhaltssteuerelement";

        /// <summary>Zusatz: der Anfang des Absatzes.</summary>
        public const string FUNDORT_AUSZUG = ": „{0}“";

        // ------------------------------------------------------------- Gründe

        /// <summary>Grund: den Schlüssel kennt der Katalog nicht.</summary>
        public const string GRUND_UNBEKANNT = "unbekannter Schlüssel";

        /// <summary>Grund: Blöcke, Stand- und Gebäudewerte, Tabellen und Bilder folgen mit späteren Etappen.</summary>
        public const string GRUND_NICHT_UNTERSTUETZT = "in dieser Fassung noch nicht unterstützt";

        /// <summary>Grund: die Art passt nicht an die Stelle (Konzept 4.3).</summary>
        public const string GRUND_FALSCHE_STELLE = "passt nicht an diese Stelle";

        /// <summary>Grund: dasselbe Kapitel steht schon an einer früheren Stelle der Vorlage (Konzept 5.3).</summary>
        public const string GRUND_DOPPELT = "Kapitel steht schon an einer früheren Stelle";

        // ------------------------------------------------------------- Meldungen

        /// <summary>Warnung: die Vorlage trägt keinen Platzhalter (Konzept 6.1).</summary>
        public const string OHNE_PLATZHALTER =
            "Die Vorlage enthält keinen Platzhalter — die Kapitel stehen am Ende des Dokuments ({{bericht.inhalt}}).";

        /// <summary>Warnung: eine Quelle warf beim Auflösen.</summary>
        public const string AUSNAHME = "{0}: Ausnahme beim Auflösen, eingesetzt ist „—“ ({1}).";

        /// <summary>Warnung: ein verknüpftes Bild wurde entfernt.</summary>
        public const string VERKNUEPFTES_BILD = "Verknüpftes Bild entfernt: {0}";

        /// <summary>Warnung: eine andere externe Verknüpfung wurde entfernt.</summary>
        public const string VERKNUEPFUNG = "Verknüpfung entfernt ({0}): {1}";

        /// <summary>Hinweis: der Verweis auf die Dokumentvorlage wurde entfernt.</summary>
        public const string DOKUMENTVORLAGE = "Verweis auf die Dokumentvorlage entfernt: {0}";

        /// <summary>Hinweis: ein fehlender Stil wurde angelegt (Konzept 6.2).</summary>
        public const string STIL_ANGELEGT = "Formatvorlage „{0}“ angelegt — sie fehlte in der Vorlage.";

        /// <summary>Hinweis: Kommentare wurden entfernt (Konzept 6.7).</summary>
        public const string KOMMENTARE = "Kommentare der Vorlage entfernt: {0}";

        /// <summary>Hinweis: ein DATE- oder TIME-Feld zeigt das Datum des Öffnens (Konzept 6.7).</summary>
        public const string DATUMSFELD = "Das Feld {0} zeigt das Datum des Öffnens — {{bericht.datum}} verwenden?";

        /// <summary>Hinweis: eine Dokumentvorlage wurde als Dokument gespeichert (Konzept 6.1).</summary>
        public const string DOTX = "Die Vorlage ist eine Dokumentvorlage (.dotx); der Bericht ist ein Dokument (.docx).";

        /// <summary>Laufmeldung: ein Schlüssel blieb leer.</summary>
        public const string LEER = "{0}: leer ({1}×)";

        /// <summary>Fehler: ein Kapitel steht nicht allein im Rumpf oder in einem Inhaltssteuerelement.</summary>
        public const string KAPITEL_ORT =
            "{0}: Ein Kapitel steht allein in einem Absatz des Rumpfs oder eines Inhaltssteuerelements — der Platzhalter bleibt stehen ({1}).";

        /// <summary>Fehler: eine Liste steht in Kopf- oder Fußzeile, Fußnote oder Textfeld.</summary>
        public const string LISTE_ORT =
            "{0}: Eine Liste steht im Rumpf, in einer Tabellenzelle oder in einem Inhaltssteuerelement — der Platzhalter bleibt stehen ({1}).";

        /// <summary>Fehler: ein Inhaltssteuerelement im Satz trägt eine Liste oder ein Kapitel.</summary>
        public const string SDT_IM_SATZ =
            "{0}: Ein Inhaltssteuerelement im Satz trägt nur Text, Zahl oder Datum — es bleibt stehen ({1}).";

        /// <summary>Fehler: ein Inhaltssteuerelement um Tabellenzeilen oder -zellen.</summary>
        public const string SDT_ZEILE =
            "{0}: Inhaltssteuerelemente um Tabellenzeilen oder -zellen werden nicht gefüllt — es bleibt stehen ({1}).";

        /// <summary>Fehler: ein Kapitel steht mehrfach in der Vorlage — nur die erste Stelle wird gefüllt (Konzept 5.3).</summary>
        public const string KAPITEL_DOPPELT =
            "{0}: Das Kapitel steht schon an einer früheren Stelle der Vorlage — diese Stelle bleibt stehen ({1}).";

        /// <summary>Fehler: ein Bildplatzhalter in einer Fuß- oder Endnote (Konzept 4.3).</summary>
        public const string BILD_ORT =
            "{0}: Ein Bild steht im Rumpf, in einer Tabellenzelle, einem Inhaltssteuerelement, einer Kopf- oder Fußzeile oder einem Textfeld — das Bild bleibt stehen ({1}).";

        /// <summary>Fehler: der Bildschlüssel steht im Alternativtext einer Form ohne Bild.</summary>
        public const string BILD_OHNE_BILD =
            "{0}: Der Alternativtext gehört zu einer Form ohne Bild — sie bleibt stehen ({1}).";

        /// <summary>Ausnahme: leere Vorlage.</summary>
        public const string VORLAGE_LEER = "Die Berichtsvorlage ist leer.";

        /// <summary>Ausnahme: die Vorlage ist kein Word-Dokument.</summary>
        public const string VORLAGE_UNLESBAR = "Die Berichtsvorlage lässt sich nicht als Word-Dokument (.docx) öffnen.";

        /// <summary>Ausnahme: Vorlage mit Makros (Konzept 6.1).</summary>
        public const string VORLAGE_MAKROS = "Vorlagen mit Makros (.docm, .dotm) werden nicht verwendet — bitte als .docx speichern.";

        /// <summary>Alle Texte — für die Wache „jeder Text zweisprachig“.</summary>
        public static readonly IReadOnlyList<string> Alle = new[]
        {
            FUNDORT_RUMPF, FUNDORT_KOPF, FUNDORT_KOPF_ERSTE, FUNDORT_KOPF_GERADE, FUNDORT_KOPF_OHNE,
            FUNDORT_FUSS, FUNDORT_FUSS_ERSTE, FUNDORT_FUSS_GERADE, FUNDORT_FUSS_OHNE,
            FUNDORT_FUSSNOTEN, FUNDORT_ENDNOTEN, FUNDORT_TABELLE, FUNDORT_TEXTFELD, FUNDORT_SDT, FUNDORT_AUSZUG,
            GRUND_UNBEKANNT, GRUND_NICHT_UNTERSTUETZT, GRUND_FALSCHE_STELLE, GRUND_DOPPELT,
            OHNE_PLATZHALTER, AUSNAHME, VERKNUEPFTES_BILD, VERKNUEPFUNG, DOKUMENTVORLAGE, STIL_ANGELEGT,
            KOMMENTARE, DATUMSFELD, DOTX, LEER, KAPITEL_ORT, LISTE_ORT, SDT_IM_SATZ, SDT_ZEILE,
            KAPITEL_DOPPELT, BILD_ORT, BILD_OHNE_BILD,
            VORLAGE_LEER, VORLAGE_UNLESBAR, VORLAGE_MAKROS,
        };

        /// <summary>Der Text in der gewählten Sprache.</summary>
        public static string T(string deutsch, bool englisch)
        {
            return BerichtTexte.T(deutsch, englisch);
        }

        /// <summary>
        /// Das Muster in der gewählten Sprache, mit den Werten gefüllt. Ersetzt werden nur
        /// <c>{0}</c>, <c>{1}</c> …, nicht <c>string.Format</c> — die Muster zitieren Platzhalter in
        /// doppelten geschweiften Klammern, die stehen bleiben müssen.
        /// </summary>
        public static string F(bool englisch, string muster, params object[] werte)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            string text = BerichtTexte.T(muster, englisch) ?? "";
            for (int i = 0; i < werte.Length; i++)
                text = text.Replace("{" + i.ToString(CultureInfo.InvariantCulture) + "}",
                                    System.Convert.ToString(werte[i], kultur) ?? "");
            return text;
        }
    }
}
