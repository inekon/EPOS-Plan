using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Befund eines Bauteils</b> (Abstimmung G5, B1; Farbmodus „Befund“) — eine abgeleitete Größe, keine Spalte. Der
    /// Zahlwert ordnet: ein höherer Befund geht vor.
    /// </summary>
    internal enum Bauteilbefund
    {
        /// <summary>Ohne Befund (grau).</summary>
        Ohne = 0,

        /// <summary>Der Körper ist nicht oder nur teilweise lesbar; die Fläche kommt aus dem Mengensatz oder fehlt (orange).</summary>
        KoerperUnlesbar = 1,

        /// <summary>Dem Bauteil fehlt, was die Rechnung braucht: kein wirksamer U-Wert oder keine Fläche (rot).</summary>
        OhneEigenschaften = 2,
    }

    /// <summary>Der benannte Grund eines <see cref="Bauteilbefund"/>s; je Grund ein Ressourcentext (<see cref="Bauteilbefunde.Text"/>).</summary>
    internal enum Bauteilbefundgrund
    {
        /// <summary>Kein Grund — ohne Befund.</summary>
        Keiner = 0,

        /// <summary>Kein wirksamer U-Wert: weder Datei, Projektdatei, Katalog, Vorgabe noch Ersatzaufbau liefern einen.</summary>
        OhneUWert,

        /// <summary>Keine Fläche (weder Mengensatz noch Körper liefern eine).</summary>
        OhneFlaeche,

        /// <summary>Die Darstellung ist nicht oder nur teilweise lesbar (eine Darstellungsart, die der Leser nicht kennt).</summary>
        DarstellungNichtLesbar,

        /// <summary>Ein boolescher Körper ist allein mit seinem ersten Operanden gelesen — der Beschnitt fehlt.</summary>
        OhneBeschnitt,

        /// <summary>Die Schale ist offen (Flächenmodell, offene Schale, fehlende Fläche).</summary>
        SchaleOffen,

        /// <summary>Ein Loch des Profils oder einer Fläche ließ sich nicht anbinden; gelesen ist nur der Außenring.</summary>
        LochNichtAngebunden,

        /// <summary>Der Körper ist entartet: Er hat Dreiecke, aber keine maßgebliche Fläche für seine Bauteilart.</summary>
        KoerperEntartet,
    }

    /// <summary>Zahl und Fläche der Bauteile eines Befunds — eine Legendenzeile.</summary>
    internal readonly record struct Befundsumme(Bauteilbefund Befund, int Zahl, double Flaeche_M2);

    /// <summary>
    /// <b>Der Befund je Bauteil</b> (Abstimmung G5, B1) — der Vertrag für den Farbmodus „Befund“, die Legende und den
    /// Steckbrief:
    /// <list type="bullet">
    /// <item><b>Rot</b> (<see cref="Bauteilbefund.OhneEigenschaften"/>): keine Fläche, oder kein wirksamer U-Wert nach der
    /// Rangfolge Datei, Projektdatei, Katalog, Ersatzaufbau (dazu die Vorgabe der Baualtersklasse): weder U der Zeile noch U
    /// aus Schichten noch ein Aufbau.</item>
    /// <item><b>Orange</b> (<see cref="Bauteilbefund.KoerperUnlesbar"/>): der Körper aus der Datei ist nicht oder nur teilweise
    /// lesbar (<see cref="Koerpergrund"/>); gebildet beim Lesen der Datei (<see cref="IfcAbbildBauer"/>) und über
    /// <see cref="AbbildBauteil.Koerpergrund"/> bis <see cref="GebaeudeBauteilzeile.Koerpergrund"/> getragen.</item>
    /// <item><b>Grau</b> (<see cref="Bauteilbefund.Ohne"/>): alles andere.</item>
    /// </list>
    /// Fehlen Eigenschaften und ist der Körper unlesbar, gilt rot — die Rechnung kann das Bauteil nicht tragen, der Körper ist
    /// nur Anzeige.
    /// <para><b>Gespeicherte Bauteile</b> (<see cref="Grund(BauteilModel)"/>): Ihr Befund folgt allein aus den Spalten von
    /// <c>Tab_Bauteil</c> — <c>U_Wert</c> NULL ohne <c>ID_Aufbau</c> oder Fläche ≤ 0 heißt rot. Den Körperbefund gibt es nur beim
    /// Import: Er wird nicht gespeichert (kein Schemaschritt), ein gespeichertes Bauteil ist sonst grau.</para>
    /// </summary>
    internal static class Bauteilbefunde
    {
        /// <summary>Der Befund eines Grunds.</summary>
        internal static Bauteilbefund Befund(Bauteilbefundgrund g) => g switch
        {
            Bauteilbefundgrund.Keiner => Bauteilbefund.Ohne,
            Bauteilbefundgrund.OhneUWert or Bauteilbefundgrund.OhneFlaeche => Bauteilbefund.OhneEigenschaften,
            _ => Bauteilbefund.KoerperUnlesbar,
        };

        /// <summary>Der Grund einer Zeile des Importvorschlags: Fläche, dann U-Wert, dann der Körper.</summary>
        internal static Bauteilbefundgrund Grund(GebaeudeBauteilzeile z)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            BauteilModel b = z.Bauteil;
            if (!(b.Flaeche > 0.0) || double.IsInfinity(b.Flaeche)) return Bauteilbefundgrund.OhneFlaeche;
            if (!(b.U_Wert > 0.0) && !(z.USchichten > 0.0) && !b.ID_Aufbau.HasValue) return Bauteilbefundgrund.OhneUWert;
            return z.Koerpergrund;
        }

        /// <summary>
        /// Der Grund eines gespeicherten Bauteils (ohne Körperbefund, siehe Klassenkommentar): Fläche ≤ 0, dann <c>U_Wert</c>
        /// NULL (oder ≤ 0) ohne Aufbau.
        /// </summary>
        internal static Bauteilbefundgrund Grund(BauteilModel b)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (!(b.Flaeche > 0.0) || double.IsInfinity(b.Flaeche)) return Bauteilbefundgrund.OhneFlaeche;
            if (!(b.U_Wert > 0.0) && !b.ID_Aufbau.HasValue) return Bauteilbefundgrund.OhneUWert;
            return Bauteilbefundgrund.Keiner;
        }

        /// <summary>
        /// <b>Der Körpergrund</b> aus dem Ergebnis des Körper-Lesers (<see cref="IfcRaumkoerper.Lesen"/>): ohne Darstellung
        /// keiner; Träger, die der Leser nicht kennt (<paramref name="nichtLesbar"/>, auch nur einer von mehreren) →
        /// <see cref="Bauteilbefundgrund.DarstellungNichtLesbar"/>; sonst nach den Vermerken des Körpers in der Folge
        /// <c>OhneBeschnitt</c>, <c>Offen</c>, <c>Loch</c>. Die Vermerke <c>Bogen</c> (Sehnenzug), <c>Uneben</c> (Fächer) und
        /// <c>Mehrschale</c> (Hohlräume ohne Einfluss auf die Hüllfläche) sind Näherungen, kein Befund.
        /// </summary>
        /// <param name="hatDarstellung">Hat das Element eine Darstellung, die gelesen wurde?</param>
        /// <param name="k">Der gelesene Körper; <c>null</c> = keiner.</param>
        /// <param name="nichtLesbar">Die Arten der nicht lesbaren Träger.</param>
        internal static Bauteilbefundgrund Koerpergrund(bool hatDarstellung, Dateikoerper k, IReadOnlyCollection<string> nichtLesbar)
        {
            if (!hatDarstellung) return Bauteilbefundgrund.Keiner;
            if ((nichtLesbar != null && nichtLesbar.Count > 0) || k == null) return Bauteilbefundgrund.DarstellungNichtLesbar;
            IReadOnlyList<Koerpervermerk> v = k.Vermerke;
            if (v == null) return Bauteilbefundgrund.Keiner;
            if (v.Contains(Koerpervermerk.OhneBeschnitt)) return Bauteilbefundgrund.OhneBeschnitt;
            if (v.Contains(Koerpervermerk.Offen)) return Bauteilbefundgrund.SchaleOffen;
            if (v.Contains(Koerpervermerk.Loch)) return Bauteilbefundgrund.LochNichtAngebunden;
            return Bauteilbefundgrund.Keiner;
        }

        /// <summary>Der Anzeigetext eines Grunds; leer ohne Grund.</summary>
        internal static string Text(Bauteilbefundgrund g) => g switch
        {
            Bauteilbefundgrund.OhneUWert => MyResource.Resource.IMP_BEFUND_GRUND_OHNE_UWERT,
            Bauteilbefundgrund.OhneFlaeche => MyResource.Resource.IMP_BEFUND_GRUND_OHNE_FLAECHE,
            Bauteilbefundgrund.DarstellungNichtLesbar => MyResource.Resource.IMP_BEFUND_GRUND_DARSTELLUNG,
            Bauteilbefundgrund.OhneBeschnitt => MyResource.Resource.IMP_BEFUND_GRUND_OHNE_BESCHNITT,
            Bauteilbefundgrund.SchaleOffen => MyResource.Resource.IMP_BEFUND_GRUND_SCHALE_OFFEN,
            Bauteilbefundgrund.LochNichtAngebunden => MyResource.Resource.IMP_BEFUND_GRUND_LOCH,
            Bauteilbefundgrund.KoerperEntartet => MyResource.Resource.IMP_BEFUND_GRUND_ENTARTET,
            _ => "",
        };

        /// <summary>Der Name eines Befunds (Legende, Steckbrief).</summary>
        internal static string Name(Bauteilbefund b) => b switch
        {
            Bauteilbefund.OhneEigenschaften => MyResource.Resource.IMP_BEFUND_OHNE_EIGENSCHAFTEN,
            Bauteilbefund.KoerperUnlesbar => MyResource.Resource.IMP_BEFUND_KOERPER_UNLESBAR,
            _ => MyResource.Resource.IMP_BEFUND_OHNE,
        };

        /// <summary>Zahl und Fläche je Befund (ohne, Körper unlesbar, ohne Eigenschaften — in dieser Folge, auch leer).</summary>
        internal static IReadOnlyList<Befundsumme> Summen(IEnumerable<GebaeudeBauteilzeile> zeilen)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));
            var liste = zeilen.Select(z => (Befund: z.Befund, Flaeche: z.Bauteil.Flaeche)).ToList();
            return new[] { Bauteilbefund.Ohne, Bauteilbefund.KoerperUnlesbar, Bauteilbefund.OhneEigenschaften }
                .Select(b => new Befundsumme(b, liste.Count(x => x.Befund == b), liste.Where(x => x.Befund == b).Sum(x => x.Flaeche)))
                .ToArray();
        }
    }
}
