using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Worauf sich der Bedarf einer Nutzungsart bezieht (<c>Tab_TwwNutzungsart_STAMM.Bezugsart</c>, Konzept 3.1).</summary>
    internal enum ZapfBezugsart
    {
        Personen = 1,
        Wohneinheiten = 2,
        Betten = 3,
        Duschplaetze = 4,
        Sitzplaetze = 5,
        Beschaeftigte = 6,

        /// <summary>Fläche — nur Rückfall.</summary>
        Flaeche = 7
    }

    /// <summary>Die Bilanzgrenze der Bedarfswerte (<c>Bilanzgrenze</c>, Konzept 3.1).</summary>
    internal enum ZapfBilanzgrenze
    {
        /// <summary>An der Zapfstelle.</summary>
        Zapfstelle = 1,

        /// <summary>Mit Verteil- und Zirkulationsverlust.</summary>
        MitVerteilung = 2,

        /// <summary>Zusätzlich mit Speicherverlust.</summary>
        MitSpeicher = 3
    }

    /// <summary>Welcher Kalender die Nutzungsart trägt (<c>Kalenderart</c>, Konzept 3.1, 4.2).</summary>
    internal enum ZapfKalenderart
    {
        Wohnen = 1,
        Arbeitstage = 2,
        Schulferien = 3,
        Betrieb = 4,
        Auslastungsgang = 5
    }

    /// <summary>Das Bedarfsniveau einer Zone (<c>Tab_TwwZone.Niveau</c>).</summary>
    internal enum ZapfNiveau
    {
        Niedrig = 1,
        Mittel = 2,
        Hoch = 3
    }

    /// <summary>Die Topologie der Trinkwassererwärmung einer Zone (<c>Tab_TwwZone.Topologie</c>, wirkt in der Auslegung 4.5).</summary>
    internal enum ZapfTopologie
    {
        Speicher = 1,
        Frischwasserstation = 2,
        Durchfluss = 3,
        Wohnungsstation = 4
    }

    /// <summary>Die Temperaturen, auf die sich die Bedarfswerte einer Nutzungsart beziehen (°C, Konzept 4.0).</summary>
    internal sealed record Temperaturbezug(double ZapftemperaturC, double KaltwasserC);

    /// <summary>
    /// Ein Tagesgangsatz (Konzept 2.1, 3.1): vier Tagesgänge zu je 24 Stundenanteilen,
    /// Provenienz je Tagtyp. Zeile <c>t</c> von <see cref="Anteile"/> und Eintrag <c>t</c> von
    /// <see cref="JeTagtyp"/> gehören zu Tagtyp <c>t + 1</c> (Werktag, Samstag,
    /// Sonn-/Feiertag, Ruhetag).
    ///
    /// <para><b>Fehlt dem Satz ein Tagtyp</b>, steht in <see cref="JeTagtyp"/> dort
    /// <c>null</c> und die Zeile der Anteile bleibt 0; <see cref="Vollstaendig"/> sagt es.
    /// Der Rechenweg (Z1) lehnt einen unvollständigen Satz benannt ab, statt ihn still zu
    /// ergänzen.</para>
    /// </summary>
    internal sealed record Tagesgangsatz(int Id, double[,] Anteile, Provenienz[] JeTagtyp)
    {
        /// <summary>Vier Tagtypen.</summary>
        internal const int TAGTYPEN = 4;

        /// <summary>Vierundzwanzig Stunden.</summary>
        internal const int STUNDEN = 24;

        /// <summary>Der neutrale Name des Satzes.</summary>
        public string Bezeichner { get; init; } = "";

        /// <summary>Die Katalogversion des Satzes.</summary>
        public string Katalogversion { get; init; } = "";

        /// <summary>Der Stand der Katalogzeile.</summary>
        public ZapfKatalogstatus Status { get; init; }

        /// <summary>Gehört der Satz zur Auslieferung (<c>ReadOnly</c>)?</summary>
        public bool ReadOnly { get; init; }

        /// <summary>Trägt der Satz alle vier Tagtypen?</summary>
        public bool Vollstaendig
        {
            get
            {
                if (JeTagtyp == null || JeTagtyp.Length != TAGTYPEN) return false;
                foreach (Provenienz p in JeTagtyp) if (p == null) return false;
                return true;
            }
        }
    }

    /// <summary>
    /// <b>Eine Nutzungsart des Katalogs</b> (Schicht S0, Konzept 2.1, 3.1) — eine
    /// unveränderliche Katalogversion, auf die eine Zone verweist.
    ///
    /// <para>Die Positionsparameter folgen dem Papier; <see cref="BedarfJeNiveauKwhJeEinheitTag"/>
    /// trägt drei Werte (niedrig, mittel, hoch), <see cref="Monatsfaktoren"/> zwölf,
    /// <see cref="Wochenfaktoren"/> sieben (Mo–So). Die Verwaltungsangaben der Zeile
    /// (Katalogversion, Status, ReadOnly, Vorlage, Freigabe) stehen als Eigenschaften daneben.
    /// Die interne Spalte <c>Beleg</c> steht NICHT darin (Konzept 6 (e)).</para>
    /// </summary>
    internal sealed record Nutzungsart(
        int Id,
        string Name,
        ZapfBezugsart Bezug,
        double[] BedarfJeNiveauKwhJeEinheitTag,
        Temperaturbezug Bezugstemperaturen,
        ZapfBilanzgrenze Grenze,
        ZapfKalenderart Kalender,
        double? Ferienfaktor,
        double[] Monatsfaktoren,
        double[] Wochenfaktoren,
        Tagesgangsatz Tagesgaenge,
        Katalogherkunft Herkunft)
    {
        /// <summary>Die Katalogversion der Zeile; mit <see cref="Name"/> der natürliche Schlüssel.</summary>
        public string Katalogversion { get; init; } = "";

        /// <summary>Der Stand der Katalogzeile.</summary>
        public ZapfKatalogstatus Status { get; init; }

        /// <summary>Gehört die Zeile zur Auslieferung (<c>ReadOnly</c>)?</summary>
        public bool ReadOnly { get; init; }

        /// <summary>Die Vorgängerzeile, aus der diese per „Speichern unter" entstand; <c>null</c> = keine.</summary>
        public int? IdVorlage { get; init; }

        /// <summary>Der Vier-Augen-Vermerk (K7); <c>null</c> = keiner.</summary>
        public string Freigabe { get; init; }
    }

    /// <summary>Konstanten der festen Raster einer Nutzungsart.</summary>
    internal static class NutzungsartRaster
    {
        /// <summary>Drei Niveaus.</summary>
        internal const int NIVEAUS = 3;

        /// <summary>Zwölf Monate.</summary>
        internal const int MONATE = 12;

        /// <summary>Sieben Wochentage.</summary>
        internal const int WOCHENTAGE = 7;

        /// <summary>Die Spaltennamen der drei Niveaus in Tabellenreihenfolge.</summary>
        internal static readonly IReadOnlyList<string> Niveauspalten = new[] { "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch" };
    }
}
