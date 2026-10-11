using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein ZIEL aus <c>help_mapping.txt</c> in lesbarer Form: der Kurztext, den ein
    /// Infoknopf zeigt, und der Wiki-Titel hinter einem Seitenpfad (Konzept
    /// Technikdokumentation, Abschnitt 7; Entscheide TD‑E1 und TD‑E2).
    /// </summary>
    /// <remarks>
    /// <para><b>Zwei Schreibweisen eines Ziels.</b> Ein Kurzname ohne führenden
    /// Schrägstrich („Pufferspeicher", „Berechnung/Wärmepumpe#rechenweg") ist eine Seite der
    /// Rubrik „Programm Dokumentation". Ein Ziel, das mit <see cref="PFAD_ANFANG"/> beginnt,
    /// ist ein SEITENPFAD außerhalb der Rubrik, etwa
    /// <c>/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen</c>: Leerzeichen als
    /// Unterstrich, Umlaute im Klartext — so, wie MediaWiki den Titel in die Adresse
    /// schreibt.</para>
    /// <para><b>Warum die Rubrik Grundlagen nur als Pfad.</b> Sie trägt Seiten mit
    /// denselben Namen wie die Rubrik „Programm Dokumentation" (Wärmepumpe, BHKW,
    /// Photovoltaik …). Ein Kurzname wäre mehrdeutig; der Pfad ist es nie.</para>
    /// <para><b>Eine Lesbarmachung für beide Schalen.</b> Der Windows-Hilfekatalog setzt
    /// damit den Kurztext seiner Grundlageneinträge, der iOS-Hilfedienst den Kurztext
    /// jedes Ziels. Das Muster „Grundlagen: {0}" kommt aus der Ressource
    /// <c>HILFE_GRUNDLAGEN_KURZTEXT</c> und folgt damit der Oberflächensprache; der
    /// Seitentitel selbst bleibt deutsch — das Wiki führt nur deutsche Seiten, übersetzt
    /// wird beim Öffnen (<c>DokuUebersetzung</c>).</para>
    /// </remarks>
    public static class Hilfeziel
    {
        /// <summary>Der Anfang eines Seitenpfads — ein Ziel, das so beginnt, ist eine Wiki-Seite außerhalb der Rubrik.</summary>
        public const string PFAD_ANFANG = "/wiki/";

        /// <summary>Titelpräfix der Wiki-Rubrik „Grundlagen" (<c>Grundlagen/Wärmepumpe</c>).</summary>
        public const string GRUNDLAGEN = "Grundlagen/";

        /// <summary>Rückfall des Musters, falls die Ressource fehlt oder kein <c>{0}</c> trägt.</summary>
        private const string GRUNDLAGEN_MUSTER = "Grundlagen: {0}";

        /// <summary>Ist das Ziel ein Seitenpfad (<c>/wiki/…</c>) und kein Kurzname der Rubrik?</summary>
        public static bool IstPfadziel(string ziel)
        {
            return !string.IsNullOrWhiteSpace(ziel) &&
                   ziel.TrimStart().StartsWith(PFAD_ANFANG, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Der Wiki-Seitentitel eines Seitenpfads oder einer Wiki-Adresse — ohne
        /// <c>/wiki/</c>, ohne Anker und Abfrage, dekodiert, Unterstrich als Leerzeichen.
        /// </summary>
        /// <example>
        /// <c>/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen</c> → <c>Grundlagen/Kessel und Spitzenlast</c>;
        /// <c>https://wiki.epos-plan.de/wiki/Grundlagen/W%C3%A4rmepumpe</c> → <c>Grundlagen/Wärmepumpe</c>.
        /// </example>
        /// <returns>Leer, wenn die Angabe kein Seitenpfad ist (etwa ein Kurzname der Rubrik).</returns>
        public static string Seitentitel(string zielOderAdresse)
        {
            if (string.IsNullOrWhiteSpace(zielOderAdresse)) return "";

            string rest = zielOderAdresse.Trim();

            // Eine volle Adresse auf ihren Pfad kuerzen. Bewusst nur http(s): Unter Unix
            // (iOS, CI) hielte Uri.TryCreate auch "/wiki/…" fuer eine absolute Dateiadresse.
            if ((rest.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 rest.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) &&
                Uri.TryCreate(rest, UriKind.Absolute, out Uri adresse))
            {
                rest = adresse.AbsolutePath;
            }

            int schnitt = rest.IndexOfAny(new[] { '#', '?' });
            if (schnitt >= 0) rest = rest.Substring(0, schnitt);

            if (!rest.StartsWith(PFAD_ANFANG, StringComparison.OrdinalIgnoreCase)) return "";

            string titel = Uri.UnescapeDataString(rest.Substring(PFAD_ANFANG.Length));
            return titel.Replace('_', ' ').Trim().Trim('/').Trim();
        }

        /// <summary>
        /// Die Wiki-Adresse eines Ziels aus <c>help_mapping.txt</c>: ein Kurzname der Rubrik
        /// („Pufferspeicher", „Berechnung/Wärmepumpe#rechenweg") oder ein Seitenpfad
        /// (<c>/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen</c>). Beide Formen gehen
        /// durch <see cref="WikiWissen.SeitenUrl"/>: Je Pfadsegment
        /// <see cref="Uri.EscapeDataString"/>, Umlaute und Sonderzeichen kodiert. Ein bereits
        /// kodierter Seitenpfad wird vorher dekodiert (<see cref="Seitentitel"/>) und damit
        /// nicht doppelt kodiert. Leer, wenn das Ziel leer ist.
        /// </summary>
        /// <param name="basis">Die Wiki-Basis ohne Schrägstrich am Ende (<see cref="WikiWissen.Basis"/>).</param>
        /// <param name="ziel">Kurzname oder Seitenpfad, wahlweise mit <c>#Anker</c>.</param>
        public static string Seitenadresse(string basis, string ziel)
        {
            if (string.IsNullOrWhiteSpace(ziel)) return "";

            string rest = ziel.Trim();
            string anker = "";

            int raute = rest.IndexOf('#');
            if (raute >= 0)
            {
                anker = rest.Substring(raute + 1).Trim();
                rest = rest.Substring(0, raute).Trim();
            }
            if (rest.Length == 0) return "";

            string titel = IstPfadziel(rest) ? Seitentitel(rest) : WikiWissen.RubrikTitel(rest);
            return WikiWissen.SeitenUrl(basis, titel, anker);
        }

        /// <summary>Zeigt das Ziel (Seitenpfad oder Wiki-Adresse) auf eine Seite der Rubrik Grundlagen?</summary>
        public static bool IstGrundlagenseite(string zielOderAdresse)
        {
            string titel = Seitentitel(zielOderAdresse);
            return titel.Length > GRUNDLAGEN.Length &&
                   titel.StartsWith(GRUNDLAGEN, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Der lesbare Kurztext eines Ziels aus <c>help_mapping.txt</c>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Seitenpfad der Rubrik Grundlagen: <c>/wiki/Grundlagen/Kessel_und_Spitzenlast</c> →
        ///       „Grundlagen: Kessel und Spitzenlast" (Muster <c>HILFE_GRUNDLAGEN_KURZTEXT</c>).</item>
        /// <item>Seitenpfad in die Rubrik „Programm Dokumentation": deren Kurzname wie beim
        ///       Kurznamenziel.</item>
        /// <item>Anderer Seitenpfad: der Seitentitel, eine Unterseite mit „: " statt „/".</item>
        /// <item>Kurzname der Rubrik: der Kapitelname ohne Anker —
        ///       <c>Berechnung/Wärmepumpe#rechenweg</c> → „Berechnung: Wärmepumpe",
        ///       <c>Pufferspeicher#schwellen</c> → „Pufferspeicher" (derselbe Kapitelname wie
        ///       im Windows-Hilfekatalog).</item>
        /// </list>
        /// </remarks>
        public static string Kurztext(string ziel)
        {
            if (string.IsNullOrWhiteSpace(ziel)) return "";

            string titel = Seitentitel(ziel);
            if (titel.Length == 0)
            {
                string angabe = ziel.Trim();

                // Eine Adresse ohne /wiki/ ist kein Ziel der Zuordnungsdatei; sie bleibt, wie sie ist.
                if (angabe.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    angabe.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return angabe;

                return Kapitelname(OhneAnker(angabe));
            }

            if (titel.Length > GRUNDLAGEN.Length &&
                titel.StartsWith(GRUNDLAGEN, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(CultureInfo.CurrentCulture, GrundlagenMuster(),
                                     titel.Substring(GRUNDLAGEN.Length).Trim());
            }

            if (titel.Length > WikiWissen.RUBRIK.Length &&
                titel.StartsWith(WikiWissen.RUBRIK, StringComparison.OrdinalIgnoreCase))
            {
                return Kapitelname(titel.Substring(WikiWissen.RUBRIK.Length));
            }

            return Kapitelname(titel);
        }

        /// <summary>
        /// Der Kapitelname eines Kurznamens: „Berechnung/Photovoltaik" → „Berechnung:
        /// Photovoltaik"; ohne Schrägstrich der Kurzname selbst.
        /// </summary>
        internal static string Kapitelname(string kurzname)
        {
            if (string.IsNullOrWhiteSpace(kurzname)) return "";

            string name = kurzname.Trim();
            int strich = name.IndexOf('/');

            return strich <= 0 ? name
                               : name.Substring(0, strich).Trim() + ": " + name.Substring(strich + 1).Trim();
        }

        /// <summary>Das Ziel ohne seine Sprungmarke (<c>#anker</c>).</summary>
        private static string OhneAnker(string ziel)
        {
            int raute = ziel.IndexOf('#');
            return (raute < 0 ? ziel : ziel.Substring(0, raute)).Trim();
        }

        /// <summary>
        /// Das Muster des Grundlagen-Kurztexts aus der Ressource; ohne <c>{0}</c> oder bei
        /// fehlendem Eintrag der deutsche Rückfall — ein Übersetzungsfehler darf den Knopf
        /// nicht stumm machen.
        /// </summary>
        private static string GrundlagenMuster()
        {
            string muster = null;
            try { muster = MyResource.Resource.HILFE_GRUNDLAGEN_KURZTEXT; }
            catch (Exception) { /* Rueckfall unten */ }

            return string.IsNullOrWhiteSpace(muster) || muster.IndexOf("{0}", StringComparison.Ordinal) < 0
                ? GRUNDLAGEN_MUSTER
                : muster;
        }
    }
}
