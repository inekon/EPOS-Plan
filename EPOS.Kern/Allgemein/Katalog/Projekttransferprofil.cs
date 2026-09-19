using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Projektliste des Transferdialogs als Katalogliste</b> (Auftrag PI-1) —
    /// der sechste Zwilling des Musters aus <see cref="Katalogfilterprofil"/>: der
    /// Bauplan steht als Komponente, die Unterschiede stehen als Daten im Kern.
    ///
    /// <para><b>Warum die Klappliste weichen musste.</b> Der Export wählte bis PI-1
    /// EIN Projekt aus einem <c>Auswahlfeld</c>. Eine Mehrfachwahl in einer
    /// Klappliste gibt es nicht, und eine Wahl über vierundzwanzig und mehr Projekte
    /// ohne Suche, Sortierung und Trichter wäre ein Rückschritt hinter jede andere
    /// Liste des Hauses. Die <c>Katalogliste</c> kann beides seit Stufe S3.4
    /// (Kontrollkästchen statt Wahlknopf) — sie bekommt hier nur ein Profil.</para>
    ///
    /// <para><b>Die Zeile trägt die Projekt-Id, nicht den Namen.</b>
    /// <see cref="Katalogfilterzeile.Schluessel"/> ist die Id als Text: Projektnamen
    /// sind im Bestand nicht zwingend eindeutig (erst <c>Tab_Projekt.ID</c> ist es),
    /// und die Wahl des Dialogs überlebt einen Filterwechsel nur, wenn sie an etwas
    /// Bleibendem hängt.</para>
    /// </summary>
    public static class Projekttransferprofil
    {
        /// <summary>Der Schlüssel der Ausprägung — er steht im <c>Katalogfilterregister</c> NICHT.</summary>
        public const string SCHLUESSEL = "PROJEKTTRANSFER";

        public const string SpProjekt = "PROJEKT";
        public const string SpArt = "ART";
        public const string SpVarianten = "VARIANTEN";
        public const string SpKunde = "KUNDE";
        public const string SpGeaendert = "GEAENDERT";
        public const string SpMitgenommen = "MITGENOMMEN";

        /// <summary>
        /// Die sechs Spalten der Transferliste.
        /// </summary>
        /// <param name="text">
        /// Übersetzer Schlüssel → Text; <c>null</c> nimmt den Ressourcentext mit
        /// deutschem Rückfall. Der Kern kennt keine Anzeigetexte — dasselbe Muster wie
        /// <c>Katalogfilterprofil.Finde</c>.
        /// </param>
        public static Katalogfilterprofil Profil(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => Rueckfall(s));

            return Katalogfilterprofil.AusSpalten(SCHLUESSEL, new[]
            {
                new Katalogspalte(SpProjekt,   t("PTR_SP_PROJEKT")),
                new Katalogspalte(SpArt,       t("PTR_SP_ART")),
                new Katalogspalte(SpVarianten, t("PTR_SP_VARIANTEN"), "",
                                  Katalogspaltenart.Zahl),
                new Katalogspalte(SpKunde,     t("PTR_SP_KUNDE")),
                new Katalogspalte(SpGeaendert, t("PTR_SP_GEAENDERT")),
                // JaNein traegt nie einen Trichter (Konzept_Katalogfilter 5.6.2) — zwei
                // Werte brauchen kein Eingabefeld, der Sortierpfeil genuegt.
                new Katalogspalte(SpMitgenommen, t("PTR_SP_MITGENOMMEN"), "",
                                  Katalogspaltenart.JaNein)
            });
        }

        /// <summary>
        /// <b>Die Zeilen der Transferliste</b> — aus demselben Bestand, den der Dialog
        /// ohnehin schon hat (<c>ProjektCtrl.NamenListe()</c>): kein neues SQL.
        /// </summary>
        /// <param name="bestand">Alle Projekte.</param>
        /// <param name="nachgezogen">
        /// Die Projekt-Ids, die nur mitreisen, weil eine ihrer Varianten gewählt ist
        /// (<see cref="Projektgruppierung.Nachgezogen"/>) — sie tragen in der Spalte
        /// „mitgenommen" ein Ja.
        /// </param>
        public static IReadOnlyList<Katalogfilterzeile> Zeilen(
            IReadOnlyList<ProjektKopfZeile> bestand,
            IReadOnlyCollection<int> nachgezogen = null)
        {
            var zeilen = new List<Katalogfilterzeile>();
            if (bestand == null) return zeilen;

            var nach = new HashSet<int>(nachgezogen ?? Array.Empty<int>());
            IReadOnlyDictionary<int, int> variantenzahl = Projektgruppierung.Variantenzahl(bestand);

            foreach (ProjektKopfZeile z in bestand)
            {
                if (z == null) continue;

                var zeile = new Katalogfilterzeile(z.Id, z.Name);
                zeile.Schluessel = z.Id.ToString(CultureInfo.InvariantCulture);

                zeile.MitText(SpProjekt, z.Name);
                zeile.MitText(SpArt, Artbezeichnung(z));
                zeile.MitZahl(SpVarianten,
                              variantenzahl.TryGetValue(z.Id, out int n) ? n : 0, 0);
                zeile.MitText(SpKunde, z.Kunde);
                zeile.MitText(SpGeaendert, Datum(z.Geaendert));
                zeile.MitKennzeichen(SpMitgenommen, nach.Contains(z.Id));

                zeilen.Add(zeile);
            }
            return zeilen;
        }

        /// <summary>
        /// „Stamm" bzw. „Variante von &lt;Stamm&gt;" — die Auskunft, ohne die eine
        /// schmale Liste drei gleich beginnende Namen nicht mehr unterscheidet
        /// (derselbe Grund wie bei <see cref="ProjektKopfZeile.StammName"/>, W15a-E-1).
        /// </summary>
        private static string Artbezeichnung(ProjektKopfZeile z)
        {
            if (z.StammId <= 0 || z.StammId == z.Id) return Rueckfall("PTR_ART_STAMM");

            string stamm = string.IsNullOrEmpty(z.StammName) ? "" : z.StammName;
            return string.Format(Rueckfall("PTR_ART_VARIANTE"), stamm);
        }

        /// <summary>
        /// Das Änderungsdatum als Text der Programmsprache — die Spalte filtert und
        /// sortiert auf dem ANGEZEIGTEN Wert (Konzept_Katalogfilter 5.6.3), deshalb
        /// das sortierfreundliche Muster „yyyy-MM-dd" statt eines Kurzdatums.
        /// </summary>
        private static string Datum(DateTime? wert)
            => wert.HasValue ? wert.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";

        /// <summary>Ressourcentext mit deutschem Rückfall — das Hausmuster der Controller.</summary>
        private static string Rueckfall(string schluessel)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                if (!string.IsNullOrEmpty(s)) return s;
            }
            catch { }

            switch (schluessel)
            {
                case "PTR_SP_PROJEKT": return "Projekt";
                case "PTR_SP_ART": return "Art";
                case "PTR_SP_VARIANTEN": return "Varianten";
                case "PTR_SP_KUNDE": return "Kunde";
                case "PTR_SP_GEAENDERT": return "Geändert";
                case "PTR_SP_MITGENOMMEN": return "mitgenommen";
                case "PTR_ART_STAMM": return "Stamm";
                case "PTR_ART_VARIANTE": return "Variante von {0}";
                default: return schluessel;
            }
        }
    }
}
