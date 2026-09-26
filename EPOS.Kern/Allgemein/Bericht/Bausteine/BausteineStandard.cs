using System;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>Baustein 1: Deckblatt (Konzept Kap. 4).</summary>
    public class DeckblattBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_DECKBLATT; } }
        public string Titel { get { return "Deckblatt"; } }

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);

            k.Titel(daten.Stammprojektname);
            k.Untertitel(UNTERTITEL);

            string varianten = string.Join(", ",
                daten.Varianten.Where(v => !v.IstStamm).Select(v => v.Anzeige));
            if (varianten.Length == 0) varianten = "— (nur Stammprojekt)";

            string version = "";
            try { version = ProduktFassung(); } catch { }

            var paare = new System.Collections.Generic.List<string>
            {
                "Projekt", daten.Stammprojektname,
                "Kunde", stamm != null && stamm.Projekt != null ? stamm.Projekt.m_szKunde : "",
                "Bearbeiter", stamm != null && stamm.Projekt != null ? stamm.Projekt.m_szBearbeiter : "",
                "Verglichene Varianten", varianten,
                "Berichtsdatum", daten.ErstelltAm.ToString("dd.MM.yyyy", k.Kultur),
                "EPOS-Plan-Version", version,
            };

            // A12 (E27) — der Produktausweis nach E10 im Berichtskopf, im Wortlaut aus EINEM
            // Ressourcenschluessel. Er steht, sobald ein Gebaeude eines Stands auf dem
            // VDI-Weg gerechnet hat; ohne ein solches Gebaeude sagte er nichts ueber diesen
            // Bericht und entfaellt.
            if (ProduktausweisNoetig(daten))
            {
                paare.Add(ZEILE_GEBAEUDEMODELL);
                // Anlagenkopplung AK1 (9.4, B-A3): EIN Satz dazu, sobald ein Gebäude gekoppelt rechnete.
                paare.Add(KopplungImBericht(daten)
                    ? MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007 + ". " + MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG
                    : MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007);
            }
            k.Eigenschaften(paare.ToArray());

            k.Hinweis("Erstellt mit EPOS-Plan · Energieplanungs-Software · Energie · Planung · Optimierung · Simulation");
            k.Seitenumbruch();
        }

        /// <summary>Die Beschriftung der Ausweiszeile im Berichtskopf (A12) — Schlüssel der Übersetzung in <see cref="BerichtTexte"/>.</summary>
        internal const string ZEILE_GEBAEUDEMODELL = "Gebäudemodell";

        /// <summary>Der Untertitel des Deckblatts — Schlüssel der Übersetzung in <see cref="BerichtTexte"/>;
        /// derselbe Text steht hinter dem Platzhalter <c>{{bericht.untertitel}}</c>.</summary>
        internal const string UNTERTITEL = "Variantenvergleich — Energie- und Wärmeversorgung";

        /// <summary>
        /// Trägt der Berichtskopf den Produktausweis nach E10 (A12)? Ja, sobald ein Stand des
        /// Berichts ein Gebäude auf dem VDI-Weg gerechnet hat (<c>Tab_ErgebnisGebaeude</c>, E30).
        /// </summary>
        /// <summary>
        /// Hat ein Stand des Berichts ein Gebäude GEKOPPELT gerechnet (Anlagenkopplung AK1,
        /// <c>Tab_ErgebnisGebaeude.Uebergabe_Art</c> bzw. <c>Kuehl_Uebergabe_Art</c>, E37)? Dann
        /// bekommt der Ausweis nach E10 den Satz, dass Wärme- und Kühlübergabe, Heizkurve und
        /// Raumregler EPOS-Erweiterungen sind (Konzept 9.4, B-A3).
        /// </summary>
        internal static bool KopplungImBericht(BerichtsDaten daten)
        {
            if (daten == null || daten.Varianten == null) return false;
            return daten.Varianten.Any(v => v != null && v.Ergebnis != null && v.Ergebnis.Gebaeude != null
                                            && v.Ergebnis.Gebaeude.Any(g => g != null && (g.IstGekoppelt || g.IstKuehlgekoppelt)));
        }

        internal static bool ProduktausweisNoetig(BerichtsDaten daten)
        {
            if (daten == null || daten.Varianten == null) return false;
            return daten.Varianten.Any(v => v != null && v.Ergebnis != null && v.Ergebnis.Gebaeude != null
                                            && v.Ergebnis.Gebaeude.Any(g => g != null && g.IstVdi6007));
        }

        /// <summary>
        /// Produktfassung des Programms — dieselbe Zeichenkette, die bis iU5-U3
        /// <c>System.Windows.Forms.Application.ProductVersion</c> geliefert hat.
        /// </summary>
        /// <remarks>
        /// <b>Warum nachgebildet und nicht einfach die Assembly-Version.</b> Der Wert steht
        /// auf dem Deckblatt des Word-Berichts; er darf sich durch den Umzug in den Kern
        /// nicht ändern. <c>Application.ProductVersion</c> geht in genau dieser Reihenfolge
        /// vor: erst das <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>
        /// des EINSTIEGS-Assemblies, sonst die Produktversion aus der Win32-Ressource
        /// derselben Datei, sonst die Notfallzeichenkette „1.0.0.0". Genau das steht hier.
        ///
        /// <b>Der Bestand nimmt heute den zweiten Zweig.</b> Die Anwendung setzt
        /// <c>GenerateAssemblyInfo=false</c> und deklariert in
        /// <c>Properties\AssemblyInfo.cs</c> nur <c>AssemblyVersion</c> und
        /// <c>AssemblyFileVersion</c> („1.1.0.0") — ohne informelle Fassung. Der Übersetzer
        /// schreibt daraus die Win32-Ressource, deren ProductVersion damit ebenfalls
        /// „1.1.0.0" lautet. Das Deckblatt zeigt also weiterhin 1.1.0.0.
        ///
        /// <b>Rückfall ohne Einstiegs-Assembly.</b> Unter einem Prüfstand oder in einem
        /// fremden Wirt kann <c>GetEntryAssembly()</c> null sein; dann gilt diese Assembly
        /// (EPOS.Kern) als Bezug. Ohne Rückfall stünde dort eine leere Zeile im Bericht.
        /// </remarks>
        internal static string ProduktFassung()
        {
            System.Reflection.Assembly einstieg =
                System.Reflection.Assembly.GetEntryAssembly() ?? typeof(DeckblattBaustein).Assembly;

            System.Reflection.AssemblyInformationalVersionAttribute merkmal =
                (System.Reflection.AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    einstieg, typeof(System.Reflection.AssemblyInformationalVersionAttribute));

            string fassung = merkmal != null ? merkmal.InformationalVersion : null;

            if (string.IsNullOrEmpty(fassung))
            {
                string datei = einstieg.Location;
                if (!string.IsNullOrEmpty(datei) && System.IO.File.Exists(datei))
                {
                    string ausRessource =
                        System.Diagnostics.FileVersionInfo.GetVersionInfo(datei).ProductVersion;
                    if (ausRessource != null) fassung = ausRessource.Trim();
                }
            }

            return string.IsNullOrEmpty(fassung) ? "1.0.0.0" : fassung;
        }
    }

    /// <summary>Baustein 2: Inhaltsverzeichnis als TOC-Feld (Konzept Kap. 4).</summary>
    public class InhaltsverzeichnisBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_INHALT; } }
        public string Titel { get { return "Inhaltsverzeichnis"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>.</summary>
        public const string UEBERSCHRIFT = "Inhalt";

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1(UEBERSCHRIFT);
            k.TocFeld();
            k.Seitenumbruch();
        }
    }

    /// <summary>Baustein 8: Anhang — Simulationsstände, Datenquellen, Hinweise (Konzept Kap. 4).</summary>
    public class AnhangBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_ANHANG; } }
        public string Titel { get { return "Anhang"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>.</summary>
        public const string UEBERSCHRIFT = "Anhang";

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1(UEBERSCHRIFT);

            k.Ueberschrift2("Simulationsstände");
            // BV-E5: dieselbe Tafel wie {{tabelle.anhang.simulationsstaende}}.
            k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Simulationsstaende(daten, BerichtTexte.Englisch, k.Kultur)));

            k.Ueberschrift2("Datengrundlage und Methodik");
            k.Text("Für diesen Bericht wurde jedes aufgeführte Projekt neu simuliert " +
                   "(stündliche Jahresrechnung) und anschließend wirtschaftlich bewertet; " +
                   "die Zahlen aller Kapitel stammen damit aus demselben Rechenlauf.");
            k.Text("Grundlage sind die je Projekt gespeicherten Simulationsergebnisse der " +
                   "EPOS-Plan-Simulation (stündliche Jahresrechnung). Varianten sind eigenständige Projektkopien, " +
                   "verknüpft über die Variantenliste des Stammprojekts.");
            k.Text("Herstellerdaten stammen aus den hinterlegten Katalogen oder manuellen Eingaben. " +
                   "Klimadaten aus der dem Projekt zugeordneten Klimaregion.");
            k.Hinweis("Emissionsfaktoren und Energiepreise werden mit den Kennzahlgruppen " +
                      "Emissionen und Kosten ausgewiesen, sobald deren Verrechnung aktiv ist (Ausbaustufe).");

            if (daten.Warnungen.Count > 0)
            {
                k.Ueberschrift2("Hinweise dieses Berichtslaufs");
                foreach (string wtext in daten.Warnungen) k.Hinweis("• " + wtext);
            }
        }
    }
}
