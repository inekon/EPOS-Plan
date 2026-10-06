using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Zuordnungsstufe eines Bauteils</b> (Konzept Bauteilaufbau beim Import 5.2) — eine abgeleitete
    /// Größe, keine Spalte.
    /// </summary>
    internal enum Bauteilzuordnungsstufe
    {
        /// <summary>Transparent (Fenster, Vorhangfassade): U und g wie gelesen, nicht bewertet.</summary>
        Transparent = 0,

        /// <summary>A: vollständiger relevanter Aufbau (Datei, Projektdatei, Katalog); U der Datei, wenn vorhanden, sonst aus den Schichten.</summary>
        A = 1,

        /// <summary>B: U-Wert aus der Datei, kein vollständiger Aufbau.</summary>
        B = 2,

        /// <summary>C: nur Geometrie — U aus der Vorgabe der Baualtersklasse bzw. ohne U (Innenbauteil).</summary>
        C = 3,
    }

    /// <summary>
    /// <b>Was einem Bauteil der Stufe B oder C zum vollständigen Aufbau fehlt</b> — als Flaggen, damit die
    /// Oberfläche (BA-3) sie einzeln nennen kann. Stufe A trägt <see cref="Keine"/>.
    /// </summary>
    [Flags]
    internal enum Aufbauluecke
    {
        /// <summary>Nichts — der Aufbau ist vollständig (oder das Bauteil ist transparent).</summary>
        Keine = 0,

        /// <summary>Die Datei trägt keine Schichten für das Bauteil.</summary>
        KeineSchichten = 1,

        /// <summary>Mindestens einer Schicht fehlt die Dicke.</summary>
        Dicke = 2,

        /// <summary>Mindestens einer Schicht fehlt λ (auch nach Namensabgleich).</summary>
        Lambda = 4,

        /// <summary>Mindestens einer Schicht fehlt ρ (auch nach Namensabgleich).</summary>
        Rohdichte = 8,

        /// <summary>Mindestens einer Schicht fehlt c (auch nach Namensabgleich und Tafel).</summary>
        Waermekapazitaet = 16,

        /// <summary>Hüllbauteil ohne erkennbare Dämmschicht (keine Schicht mit bekanntem λ ≤ <see cref="Bauteilzuordnung.DAEMMUNG_LAMBDA_MAX"/>).</summary>
        Daemmung = 32,

        /// <summary>Keine Schicht mit bekannter Kapazität ≥ <see cref="Bauteilzuordnung.SPEICHERND_KAPAZITAET_MIN"/>.</summary>
        SpeicherndeSchicht = 64,

        /// <summary>Vermerk, keine Lücke: Das Bauteil trägt statt des fehlenden Aufbaus einen Ersatzaufbau (BA-2, Kennzeichen <c>Typaufbau</c>).</summary>
        Ersatzaufbau = 128,
    }

    /// <summary>Die Werte einer Schicht nach Datei und Namensabgleich; <c>null</c> = fehlt.</summary>
    internal readonly record struct Schichtwerte(double? Dicke_M, double? Lambda_WmK, double? Rohdichte_KgM3, double? Cp_JkgK,
                                                 bool Luftschicht = false);

    /// <summary>Zahl und Fläche der Bauteile einer Stufe.</summary>
    internal readonly record struct Zuordnungssumme(Bauteilzuordnungsstufe Stufe, int Zahl, double Flaeche_M2);

    /// <summary>
    /// <b>Die Zuordnungsstufe je Bauteil</b> (Konzept Bauteilaufbau beim Import 5.2, BA-1) — der Vertrag für
    /// Farbmodus, Liste und Spalte der Oberfläche (BA-3):
    /// <list type="bullet">
    /// <item><see cref="Stufe(GebaeudeBauteilzeile)"/> für eine Zeile des Importvorschlags (Herkunft je Wert);</item>
    /// <item><see cref="Stufe(BauteilModel)"/> für eine gespeicherte Zeile (<c>Tab_Bauteil</c>);</item>
    /// <item><see cref="Summen"/> Zahl und Fläche je Stufe (Protokoll, Legende);</item>
    /// <item><see cref="Luecken"/> was einem Aufbau fehlt.</item>
    /// </list>
    /// Regel: transparent → <see cref="Bauteilzuordnungsstufe.Transparent"/>; mit echtem Aufbau → A; sonst (ohne
    /// Aufbau oder mit Ersatzaufbau, Kennzeichen <c>Typaufbau</c>, BA-2) mit U der Datei → B; sonst C. Ein
    /// Ersatzaufbau hebt ein Bauteil nie auf A.
    /// </summary>
    internal static class Bauteilzuordnung
    {
        /// <summary>Höchstes λ einer Dämmschicht [W/(mK)] (Dämmstoffe nach DIN 4108-4 liegen darunter).</summary>
        internal const double DAEMMUNG_LAMBDA_MAX = 0.1;

        /// <summary>Kleinste flächenbezogene Kapazität einer speichernden Schicht [J/(m²K)] — etwa 1 cm Putz.</summary>
        internal const double SPEICHERND_KAPAZITAET_MIN = 20_000.0;

        /// <summary>Ist die Bauteilart transparent (Fenster, Vorhangfassade)?</summary>
        internal static bool IstTransparent(string bauteilart)
            => string.Equals(bauteilart, DbWerte.BAUTEILART_FENSTER, StringComparison.Ordinal)
               || string.Equals(bauteilart, DbWerte.BAUTEILART_VORHANGFASSADE, StringComparison.Ordinal);

        /// <summary>Die Stufe einer Zeile des Importvorschlags.</summary>
        internal static Bauteilzuordnungsstufe Stufe(GebaeudeBauteilzeile z)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            if (IstTransparent(z.Bauteil.Bauteilart)) return Bauteilzuordnungsstufe.Transparent;
            if (z.Bauteil.ID_Aufbau.HasValue && z.Typaufbau == null) return Bauteilzuordnungsstufe.A;
            if (z.UDatei.HasValue
                || (z.HerkunftU != Importherkunft.Leer && !ImportherkunftWerte.IstVorgabe(z.HerkunftU)))
                return Bauteilzuordnungsstufe.B;
            return Bauteilzuordnungsstufe.C;
        }

        /// <summary>
        /// Die Stufe einer gespeicherten Zeile: mit echtem Aufbau A; mit U-Wert, der nicht aus der Vorgabe stammt
        /// (<c>Herkunft</c> ≠ VORGABE), B; sonst C. <paramref name="aufbauten"/> sind die Aufbauten des Projekts
        /// nach Id: Trägt der Aufbau der Zeile das Kennzeichen <c>Typaufbau</c>, ist er ein Ersatzaufbau (BA-2), und
        /// die Zeile bleibt B bzw. C. Ein Aufbau, der dort fehlt, gilt als echt.
        /// </summary>
        internal static Bauteilzuordnungsstufe Stufe(BauteilModel b, IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (IstTransparent(b.Bauteilart)) return Bauteilzuordnungsstufe.Transparent;
            if (b.ID_Aufbau is int id
                && !(aufbauten != null && aufbauten.TryGetValue(id, out BauteilaufbauModel a) && !string.IsNullOrEmpty(a?.Typaufbau)))
                return Bauteilzuordnungsstufe.A;
            if (b.U_Wert.HasValue && !string.Equals(b.Herkunft, DbWerte.HERKUNFT_VORGABE, StringComparison.Ordinal))
                return Bauteilzuordnungsstufe.B;
            return Bauteilzuordnungsstufe.C;
        }

        /// <summary>Zahl und Fläche je Stufe A, B, C (in dieser Folge, auch leer); transparente Zeilen zählen nicht.</summary>
        internal static IReadOnlyList<Zuordnungssumme> Summen(IEnumerable<GebaeudeBauteilzeile> zeilen)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));
            var liste = zeilen.Select(z => (Stufe: Stufe(z), Flaeche: z.Bauteil.Flaeche)).ToList();
            return new[] { Bauteilzuordnungsstufe.A, Bauteilzuordnungsstufe.B, Bauteilzuordnungsstufe.C }
                .Select(s => new Zuordnungssumme(s, liste.Count(x => x.Stufe == s), liste.Where(x => x.Stufe == s).Sum(x => x.Flaeche)))
                .ToArray();
        }

        /// <summary>
        /// Was einem Aufbau fehlt — aus den Werten seiner Schichten nach Datei und Namensabgleich.
        /// <paramref name="huelle"/>: Hüllbauteil (dann zählt eine fehlende Dämmschicht). Ohne Schichten
        /// <see cref="Aufbauluecke.KeineSchichten"/>.
        /// </summary>
        internal static Aufbauluecke Luecken(IReadOnlyList<Schichtwerte> schichten, bool huelle)
        {
            if (schichten == null || schichten.Count == 0) return Aufbauluecke.KeineSchichten;
            Aufbauluecke l = Aufbauluecke.Keine;
            bool daemmung = false, speichernd = false;
            foreach (Schichtwerte s in schichten)
            {
                if (!(s.Dicke_M > 0.0)) l |= Aufbauluecke.Dicke;
                if (s.Luftschicht) continue;   // ruhende Luft: Widerstand aus Tabelle 8, keine Masse
                if (!s.Lambda_WmK.HasValue) l |= Aufbauluecke.Lambda;
                if (!s.Rohdichte_KgM3.HasValue) l |= Aufbauluecke.Rohdichte;
                if (!s.Cp_JkgK.HasValue) l |= Aufbauluecke.Waermekapazitaet;
                if (s.Lambda_WmK is double lam && lam <= DAEMMUNG_LAMBDA_MAX) daemmung = true;
                if (s.Dicke_M is double d && s.Rohdichte_KgM3 is double r && s.Cp_JkgK is double c
                    && d * r * c >= SPEICHERND_KAPAZITAET_MIN) speichernd = true;
            }
            if (huelle && !daemmung) l |= Aufbauluecke.Daemmung;
            if (!speichernd) l |= Aufbauluecke.SpeicherndeSchicht;
            return l;
        }
    }
}
