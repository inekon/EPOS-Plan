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
            k.Untertitel("Variantenvergleich — Energie- und Wärmeversorgung");

            string varianten = string.Join(", ",
                daten.Varianten.Where(v => !v.IstStamm).Select(v => v.Anzeige));
            if (varianten.Length == 0) varianten = "— (nur Stammprojekt)";

            string version = "";
            try { version = ProduktFassung(); } catch { }

            k.Eigenschaften(
                "Projekt", daten.Stammprojektname,
                "Kunde", stamm != null && stamm.Projekt != null ? stamm.Projekt.m_szKunde : "",
                "Bearbeiter", stamm != null && stamm.Projekt != null ? stamm.Projekt.m_szBearbeiter : "",
                "Verglichene Varianten", varianten,
                "Berichtsdatum", daten.ErstelltAm.ToString("dd.MM.yyyy", k.Kultur),
                "EPOS-Plan-Version", version);

            k.Hinweis("Erstellt mit EPOS-Plan · Energieplanungs-Software · Energie · Planung · Optimierung · Simulation");
            k.Seitenumbruch();
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

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1("Inhalt");
            k.TocFeld();
            k.Seitenumbruch();
        }
    }

    /// <summary>Baustein 8: Anhang — Simulationsstände, Datenquellen, Hinweise (Konzept Kap. 4).</summary>
    public class AnhangBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_ANHANG; } }
        public string Titel { get { return "Anhang"; } }

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1("Anhang");

            k.Ueberschrift2("Simulationsstände");
            int[] w = { 3200, 1400, 2400, 2355 };
            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(k.Zelle("Projekt", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Rolle", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Simulation vom", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("Hinweis", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            t.Append(kopf);
            foreach (VariantenDaten v in daten.Varianten)
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(v.Projektname, w[0], false, v.IstStamm ? WordBerichtGenerator.STAMM_FILL : null, JustificationValues.Left));
                tr.Append(k.Zelle(v.IstStamm ? "Stamm" : "Variante", w[1], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(v.SimulationsStand.HasValue
                    ? v.SimulationsStand.Value.ToString("dd.MM.yyyy HH:mm", k.Kultur) : "—",
                    w[2], false, null, JustificationValues.Center));
                string hinweis = v.Fehler != null ? "Fehler: " + v.Fehler
                    : v.FrischSimuliert ? "für diesen Bericht neu gerechnet"
                    : v.ErgebnisVeraltet ? "älter als letzte Projektänderung" : "";
                tr.Append(k.Zelle(hinweis, w[3], false, null, JustificationValues.Left));
                t.Append(tr);
            }
            k.Fuege(t);

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
