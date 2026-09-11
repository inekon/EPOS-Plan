// DER HILFETEXT ZU EINEM KATALOGFELD (Auftrag #201).
//
// dialog_parameter_erklaeren liefert zusaetzlich zur Katalog-Erlaeuterung den Hilfeartikel
// zum Slug des Feldes. Der Hilfekatalog (WikiHelpCatalog) liegt in der Windows-Anwendung
// und liest seinen Bestand aus dem Dokumentationsordner; der Kern kennt ihn nicht und soll
// ihn nicht kennen - dasselbe Verhaeltnis wie bei Dienste.* (Paket iU5).
//
// Deshalb steht hier eine NAHT mit stiller Standardfassung: Ohne eingelegten Lieferanten
// gibt es keinen Hilfetext, und das ist kein Fehler - heute deklariert ohnehin kein
// Katalogfeld einen Slug (Begruendung in KiDialoge: die Zuordnung Feld -> Slug las
// help_mapping.txt, und die Datei liegt nicht im Repository).

using System;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Hilfeeintrag zu einem Feld: Kurztext und Adresse.</summary>
    public sealed class KiFeldhilfeEintrag
    {
        /// <summary>Legt einen Eintrag an; <c>null</c> wird zu leer.</summary>
        public KiFeldhilfeEintrag(string kurztext, string adresse)
        {
            Kurztext = kurztext ?? "";
            Adresse = adresse ?? "";
        }

        /// <summary>Der Kurztext (Tooltip der Hilfe).</summary>
        public string Kurztext { get; }

        /// <summary>Die Adresse der Wikiseite.</summary>
        public string Adresse { get; }

        /// <summary>Traegt der Eintrag ueberhaupt etwas?</summary>
        public bool Belegt => Kurztext.Length > 0 || Adresse.Length > 0;
    }

    /// <summary>
    /// Die Naht zum Hilfekatalog der Plattform.
    /// </summary>
    /// <remarks>
    /// Die Windows-Huelle legt in <c>Program.Main</c> einen Lieferanten ein, der
    /// <c>WikiHelpCatalog.Aktueller.Get(slug)</c> befragt. Ohne Lieferanten -
    /// Aktionsharnisch, Referenzlauf, iOS - liefert <see cref="Fuer"/> <c>null</c>, und
    /// die Erklaerung antwortet aus der Katalog-Erlaeuterung allein. Ein fehlender
    /// Hilfetext ist ein Schoenheitsfehler und kein Grund, die Erklaerung scheitern zu
    /// lassen.
    /// </remarks>
    public static class KiFeldhilfe
    {
        /// <summary>Der eingelegte Lieferant; <c>null</c> = keiner.</summary>
        public static Func<string, KiFeldhilfeEintrag> Lieferant { get; set; }

        /// <summary>Der Hilfeeintrag zum Slug; <c>null</c>, wenn es keinen gibt.</summary>
        public static KiFeldhilfeEintrag Fuer(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            Func<string, KiFeldhilfeEintrag> quelle = Lieferant;
            if (quelle == null) return null;

            try
            {
                KiFeldhilfeEintrag eintrag = quelle(slug);
                return eintrag != null && eintrag.Belegt ? eintrag : null;
            }
            catch (Exception)
            {
                // Ein Hilfekatalog, der sich verschluckt, darf keine Erklaerung kippen.
                return null;
            }
        }
    }
}
