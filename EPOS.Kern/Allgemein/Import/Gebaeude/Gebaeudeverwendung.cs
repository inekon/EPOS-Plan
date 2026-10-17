using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Verwendung eines importierten Gebäudes</b> (Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>, Steuerwerte
    /// <see cref="GebaeudeStammCtrl.FILTERWERT_WOHN"/> und <see cref="GebaeudeStammCtrl.FILTERWERT_SONSTIGE"/>), abgeleitet
    /// aus dem, was die Datei sagt (Anwenderwunsch 08.10.2026): Ohne sie stand ein importiertes Verwaltungsgebäude als
    /// Wohngebäude da. Ohne Datenbank, ohne Oberfläche — eine Regel für alle Formate.
    /// <list type="number">
    /// <item><b>Gebäudeart</b>: trägt die Datei eine (gbXML <c>buildingType</c>, IFC <c>ObjectType</c> des Gebäudes), gilt
    /// sie — die Wohntypen der Typologie und <c>SingleFamily</c>, <c>MultiFamily</c>, <c>Dormitory</c> sind Wohngebäude,
    /// jede andere benannte Art ein Nichtwohngebäude; nichtssagende Arten (<c>Unknown</c>, <c>NOTDEFINED</c>, „Gebäude“)
    /// zählen nicht.</item>
    /// <item><b>Zonennutzungen nach Fläche</b>: sonst die Nutzungen der Zonen bzw. Räume mit Fläche (DIN-V-18599-10-Profile
    /// der Projektdatei, Raumtypen von gbXML und IFC) — die Mehrheit der Fläche mit erkannter Nutzung entscheidet; Gleichstand
    /// entscheidet nicht.</item>
    /// <item><b>Kein Anhaltspunkt</b>: die Vorgabe des neuen Satzes bleibt.</item>
    /// </list>
    /// </summary>
    internal static class Gebaeudeverwendung
    {
        /// <summary>Woraus die Verwendung folgt.</summary>
        public enum Anhalt
        {
            /// <summary>Die Datei sagt nichts Verwertbares — die Vorgabe bleibt.</summary>
            Keiner,
            /// <summary>Die Gebäudeart der Datei.</summary>
            Gebaeudeart,
            /// <summary>Die Nutzungen der Zonen bzw. Räume nach Fläche.</summary>
            Zonennutzung,
        }

        /// <summary>Eine Zone oder ein Raum: Bezeichnung, Wohnnutzung (<c>null</c> = nicht erkannt) und Fläche [m²].</summary>
        public sealed record Zonenanteil(string Bezeichnung, bool? Wohnen, double FlaecheM2);

        /// <summary>
        /// Das Ergebnis: der Steuerwert (<c>null</c> = keine Ableitung, die Vorgabe bleibt), der Anhalt und die Herleitung als
        /// Protokollmeldung (Schlüssel <c>IMP_GEB_PROT_VERWENDUNG_*</c>).
        /// </summary>
        public sealed record Ergebnis(string Verwendung, Anhalt Anhalt, PruefMeldung Herleitung)
        {
            /// <summary>Gibt die Datei die Verwendung her?</summary>
            public bool Abgeleitet => Verwendung != null;
        }

        /// <summary>Meldungsschlüssel: aus der Gebäudeart; {0} Verwendung, {1} Gebäudeart.</summary>
        public const string MELDUNG_ART = GebaeudeImportAblauf.MELDUNG + "VERWENDUNG_ART";

        /// <summary>Meldungsschlüssel: aus den Zonennutzungen; {0} Verwendung, {1} Wohnanteil in %, {2} Fläche mit erkannter Nutzung in m².</summary>
        public const string MELDUNG_ZONEN = GebaeudeImportAblauf.MELDUNG + "VERWENDUNG_ZONEN";

        /// <summary>Meldungsschlüssel: kein Anhaltspunkt; {0} die Vorgabe.</summary>
        public const string MELDUNG_KEINE = GebaeudeImportAblauf.MELDUNG + "VERWENDUNG_KEINE";

        /// <summary>Die Gebäudearten, die nichts sagen (kleingeschrieben, getrimmt).</summary>
        private static readonly HashSet<string> NICHTSSAGEND = new HashSet<string>(StringComparer.Ordinal)
        {
            "unknown", "notdefined", "userdefined", "building", "gebäude", "gebaeude", "sonstige", "sonstiges", "other", "-",
        };

        /// <summary>Wortteile einer Wohnnutzung (Gebäudeart wie Raumtyp; kleingeschrieben).</summary>
        private static readonly string[] WOHNEN = new[]
        {
            "wohn", "familienhaus", "reihenhaus", "reihenmittelhaus", "reiheneckhaus", "doppelhaus", "einfamilien", "mehrfamilien",
            "singlefamily", "single family", "multifamily", "multi family", "dormitory", "residential", "apartment", "dwelling",
            "living",
        };

        /// <summary>Wortteile eines Raumtyps, der in jedem Gebäude vorkommt (Verkehr, Nebenräume) — nicht erkannt.</summary>
        private static readonly string[] NEUTRAL = new[]
        {
            "flur", "corridor", "treppe", "stair", "wc", "toilet", "restroom", "bad", "bath", "dusch", "shower", "abstell",
            "storage", "technik", "mechanical", "electrical", "keller", "schacht", "shaft", "aufzug", "elevator", "circulation",
            "verkehr", "plenum", "garage", "diele", "eingang", "entrance", "lobby", "vestibule",
        };

        /// <summary>
        /// Wohnt die GEBÄUDEART? <c>true</c> = Wohngebäude, <c>false</c> = eine benannte andere Art, <c>null</c> = leer oder
        /// nichtssagend.
        /// </summary>
        public static bool? WohnenAusGebaeudeart(string art)
        {
            string a = Normal(art);
            if (a.Length == 0 || NICHTSSAGEND.Contains(a)) return null;
            return Enthaelt(a, WOHNEN);
        }

        /// <summary>
        /// Wohnt die Nutzung einer DIN-V-18599-10-Profilnummer der Projektdatei (<see cref="Din18599Nutzung"/>)? Die Wohnzeilen
        /// der Datei (70, 71) sind Wohnen, die Normnummern 1 bis 43 Nichtwohnen; jede andere Nummer und keine sagen nichts.
        /// </summary>
        public static bool? WohnenAusDinNummer(int? nummer)
        {
            if (nummer is not int n) return null;
            if (string.Equals(Din18599Nutzung.Nutzung(n), DbWerte.KOND_NUTZUNG_WOHNEN, StringComparison.Ordinal)) return true;
            return n >= 1 && n <= 43 ? false : null;
        }

        /// <summary>
        /// Wohnt der RAUMTYP einer Datei (gbXML <c>spaceType</c>, IFC <c>Pset_SpaceCommon.Category</c>/<c>ObjectType</c>)?
        /// Ein Wohnwort heißt Wohnen; Verkehrs- und Nebenräume, die jedes Gebäude hat, sagen nichts; jeder andere benannte
        /// Typ heißt Nichtwohnen.
        /// </summary>
        public static bool? WohnenAusRaumtyp(string raumtyp)
        {
            string t = Normal(raumtyp);
            if (t.Length == 0 || NICHTSSAGEND.Contains(t)) return null;
            if (Enthaelt(t, WOHNEN)) return true;
            if (Enthaelt(t, NEUTRAL)) return null;
            return false;
        }

        /// <summary>
        /// <b>Leitet die Verwendung ab</b>: zuerst aus der Gebäudeart, sonst aus den Zonennutzungen nach Fläche, sonst bleibt
        /// <paramref name="vorgabe"/> (Rückgabe ohne Steuerwert).
        /// </summary>
        public static Ergebnis Ableiten(string gebaeudeart, IEnumerable<Zonenanteil> zonen, string vorgabe = GebaeudeStammCtrl.FILTERWERT_WOHN)
        {
            if (WohnenAusGebaeudeart(gebaeudeart) is bool artWohnt)
            {
                string w = Wert(artWohnt);
                return new Ergebnis(w, Anhalt.Gebaeudeart,
                                    new PruefMeldung(PruefStufe.Info, MELDUNG_ART, GebaeudeStammCtrl.Verwendungstext(w), gebaeudeart.Trim()));
            }

            double wohnen = 0, andere = 0;
            foreach (Zonenanteil z in zonen ?? Enumerable.Empty<Zonenanteil>())
            {
                if (z == null || z.Wohnen is not bool zw || !(z.FlaecheM2 > 0) || double.IsInfinity(z.FlaecheM2)) continue;
                if (zw) wohnen += z.FlaecheM2; else andere += z.FlaecheM2;
            }
            double summe = wohnen + andere;
            if (summe > 0 && wohnen != andere)
            {
                string w = Wert(wohnen > andere);
                return new Ergebnis(w, Anhalt.Zonennutzung,
                                    new PruefMeldung(PruefStufe.Info, MELDUNG_ZONEN, GebaeudeStammCtrl.Verwendungstext(w),
                                                     Math.Round(100.0 * wohnen / summe).ToString(CultureInfo.InvariantCulture),
                                                     Math.Round(summe, 1).ToString(CultureInfo.InvariantCulture)));
            }
            return new Ergebnis(null, Anhalt.Keiner,
                                new PruefMeldung(PruefStufe.Info, MELDUNG_KEINE, GebaeudeStammCtrl.Verwendungstext(vorgabe ?? GebaeudeStammCtrl.FILTERWERT_WOHN)));
        }

        /// <summary>Der Steuerwert zu „wohnt“.</summary>
        public static string Wert(bool wohnt) => wohnt ? GebaeudeStammCtrl.FILTERWERT_WOHN : GebaeudeStammCtrl.FILTERWERT_SONSTIGE;

        /// <summary>
        /// Die Räume eines gelesenen Gebäudes als Anteile — Raumtyp und Fläche, nur beheizte Räume mit Fläche (gbXML, IFC und
        /// die Projektdatei ohne Zonen).
        /// </summary>
        internal static IReadOnlyList<Zonenanteil> Raumanteile(AbbildGebaeude g)
            => g == null ? Array.Empty<Zonenanteil>()
             : g.Raeume.Where(r => r.Beheizt && r.FlaecheM2 is double f && f > 0)
                       .Select(r => new Zonenanteil(r.Name ?? r.Kennung, WohnenAusRaumtyp(r.Raumtyp), r.FlaecheM2.Value))
                       .ToList();

        private static string Normal(string text) => (text ?? "").Trim().ToLowerInvariant();

        private static bool Enthaelt(string text, string[] teile)
        {
            foreach (string t in teile)
                if (text.Contains(t, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
