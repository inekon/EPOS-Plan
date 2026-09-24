using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E17 (Konzept Wirtschaftlichkeit § 2.11.2 V‑G11; DIN EN 17463 6.1 und 8.2) — EINE
    /// nicht monetarisierbare Wirkung eines Projekts: Kategorie, Beschreibung, Dauer und die
    /// Wirkung auf Organisation, Mitarbeiter und Umwelt. Eine Zeile von
    /// <c>Tab_ProjektWirkung</c> (<see cref="ProjektWirkungSchema"/>).
    ///
    /// <para><b>Keine Rechenwirkung.</b> Die Wirkungen stehen neben dem Kapitalwert, nie in
    /// ihm: Kein Rechenweg liest diese Klasse.</para>
    /// </summary>
    public sealed class ProjektWirkung
    {
        /// <summary>Zeilen-Id (<c>Tab_ProjektWirkung.ID</c>); 0 = noch nicht gespeichert.</summary>
        public int Id { get; set; }

        /// <summary>Reihenfolge innerhalb des Projekts (1, 2, 3 …).</summary>
        public int Sortierung { get; set; }

        /// <summary>Kategorie nach 6.1 (<see cref="NichtMonetaereWirkungen.KATEGORIEN"/>).</summary>
        public string Kategorie { get; set; } = NichtMonetaereWirkungen.SONSTIG;

        /// <summary>Was die Wirkung ist — Versorgungssicherheit, Komfort, Außenwirkung …</summary>
        public string Beschreibung { get; set; } = "";

        /// <summary>Dauer 1 kurz, 2 mittel, 3 lang; <c>null</c> = nicht beurteilt.</summary>
        public int? Dauer { get; set; }

        /// <summary>Wirkung auf die Organisation 0 keine … 3 stark; <c>null</c> = nicht beurteilt.</summary>
        public int? WirkungOrganisation { get; set; }

        /// <summary>Wirkung auf die Mitarbeiter 0 … 3; <c>null</c> = nicht beurteilt.</summary>
        public int? WirkungMitarbeiter { get; set; }

        /// <summary>Wirkung auf die Umwelt 0 … 3; <c>null</c> = nicht beurteilt.</summary>
        public int? WirkungUmwelt { get; set; }

        /// <summary>
        /// Die Beurteilung nach 8.2 — <b>angezeigt, nie gespeichert</b>: Dauer × stärkste
        /// Wirkung (<see cref="NichtMonetaereWirkungen.Beurteilung(int?, int?, int?, int?)"/>).
        /// </summary>
        public int? Beurteilung
            => NichtMonetaereWirkungen.Beurteilung(Dauer, WirkungOrganisation, WirkungMitarbeiter, WirkungUmwelt);

        /// <summary>Eine flache Kopie — der Arbeitsstand einer Maske.</summary>
        public ProjektWirkung Kopie() => (ProjektWirkung)MemberwiseClone();
    }

    /// <summary>
    /// ETAPPE E17 — die <b>Regeln der nicht monetarisierbaren Wirkungen</b> an EINER Stelle:
    /// Kategorien, Skalen, die Beurteilung nach DIN EN 17463 8.2, Prüfung und Anzeigetexte.
    /// Seite, Bericht und Checkliste lesen dieselben Funktionen.
    ///
    /// <para><b>Die Skalen (E17‑Q2 a).</b> Dauer 1 kurz, 2 mittel, 3 lang; Wirkung je Bereich
    /// 0 keine, 1 gering, 2 mittel, 3 stark. <b>Beurteilung = Dauer × stärkste Wirkung</b>,
    /// also 0 bis 9: Eine lange, starke Wirkung wiegt 9, eine kurze, geringe 1. Die stärkste
    /// statt der Summe, weil drei geringe Wirkungen keine starke sind.</para>
    ///
    /// <para><b>Nicht beurteilt</b> ist eine Wirkung ohne Dauer oder ohne einen einzigen
    /// Wirkungsgrad — dann gibt es keine Zahl, und die Anzeige sagt „nicht beurteilt".</para>
    /// </summary>
    public static class NichtMonetaereWirkungen
    {
        /// <summary>Kategorie „Energiefluss" (6.1).</summary>
        public const string ENERGIEFLUSS = "ENERGIEFLUSS";

        /// <summary>Kategorie „finanziell" (6.1).</summary>
        public const string FINANZIELL = "FINANZIELL";

        /// <summary>Kategorie „sonstig" (6.1) — auch die des übernommenen Freitexts.</summary>
        public const string SONSTIG = "SONSTIG";

        /// <summary>Die drei Kategorien in Anzeigereihenfolge — ihr Listenplatz ist die Id der Wahl.</summary>
        public static readonly IReadOnlyList<string> KATEGORIEN = new[] { ENERGIEFLUSS, FINANZIELL, SONSTIG };

        /// <summary>Kleinste und größte Dauer.</summary>
        public const int DAUER_MIN = 1, DAUER_MAX = 3;

        /// <summary>Kleinster und größter Wirkungsgrad.</summary>
        public const int WIRKUNG_MIN = 0, WIRKUNG_MAX = 3;

        /// <summary>Die höchste Beurteilung (lange, starke Wirkung).</summary>
        public const int BEURTEILUNG_MAX = DAUER_MAX * WIRKUNG_MAX;

        /// <summary>
        /// <b>Die Beurteilungsregel nach DIN EN 17463 8.2</b>: Dauer × stärkste der drei
        /// Wirkungen. <c>null</c>, wenn die Dauer fehlt oder keine Wirkung beurteilt ist; ein
        /// einzelner fehlender Wirkungsgrad zählt nicht mit. Werte außerhalb der Skala ergeben
        /// ebenfalls <c>null</c> — die Regel rät nicht.
        /// </summary>
        public static int? Beurteilung(int? dauer, int? organisation, int? mitarbeiter, int? umwelt)
        {
            if (!dauer.HasValue || dauer.Value < DAUER_MIN || dauer.Value > DAUER_MAX) return null;

            int? staerkste = null;
            foreach (int? w in new[] { organisation, mitarbeiter, umwelt })
            {
                if (!w.HasValue) continue;
                if (w.Value < WIRKUNG_MIN || w.Value > WIRKUNG_MAX) return null;
                if (!staerkste.HasValue || w.Value > staerkste.Value) staerkste = w.Value;
            }
            return staerkste.HasValue ? dauer.Value * staerkste.Value : (int?)null;
        }

        /// <summary>Die Beurteilung einer Wirkung (<see cref="Beurteilung(int?, int?, int?, int?)"/>).</summary>
        public static int? Beurteilung(ProjektWirkung w)
            => w == null ? null : Beurteilung(w.Dauer, w.WirkungOrganisation, w.WirkungMitarbeiter, w.WirkungUmwelt);

        /// <summary>Trägt die Liste mindestens eine Wirkung mit Beschreibung?</summary>
        public static bool Benannt(IEnumerable<ProjektWirkung> liste)
            => liste != null && liste.Any(w => w != null && !string.IsNullOrWhiteSpace(w.Beschreibung));

        /// <summary>Ist mindestens eine Wirkung beurteilt (Anhang-E-Checkliste „erfüllt")?</summary>
        public static bool Beurteilt(IEnumerable<ProjektWirkung> liste)
            => liste != null && liste.Any(w => w != null && !string.IsNullOrWhiteSpace(w.Beschreibung)
                                                && Beurteilung(w).HasValue);

        /// <summary>
        /// Die Beschreibungen in ihrer Reihenfolge, mit „; " verbunden — der Ausweis im Kopf des
        /// Bewertungsblocks und der Text, an dem die Deklaration „benannt" hängt. Leer ohne
        /// benannte Wirkung.
        /// </summary>
        public static string Kurztext(IEnumerable<ProjektWirkung> liste)
        {
            if (liste == null) return "";
            return string.Join("; ", liste.Where(w => w != null && !string.IsNullOrWhiteSpace(w.Beschreibung))
                                          .Select(w => w.Beschreibung.Trim()));
        }

        /// <summary>Ist der Schlüssel eine der drei Kategorien?</summary>
        public static bool KategorieGueltig(string kategorie)
            => kategorie != null && KATEGORIEN.Contains(kategorie, StringComparer.Ordinal);

        /// <summary>Der Anzeigetext einer Kategorie; ein fremder Schlüssel steht roh da.</summary>
        public static string KategorieText(string kategorie)
        {
            switch (kategorie)
            {
                case ENERGIEFLUSS: return MyResource.Resource.WIRT_NM_KAT_ENERGIEFLUSS;
                case FINANZIELL: return MyResource.Resource.WIRT_NM_KAT_FINANZIELL;
                case SONSTIG: return MyResource.Resource.WIRT_NM_KAT_SONSTIG;
                default: return kategorie ?? "";
            }
        }

        /// <summary>Der Anzeigetext einer Dauer („kurz", „mittel", „lang"); leer ohne Wert.</summary>
        public static string DauerText(int? dauer)
        {
            switch (dauer)
            {
                case 1: return MyResource.Resource.WIRT_NM_DAUER_1;
                case 2: return MyResource.Resource.WIRT_NM_DAUER_2;
                case 3: return MyResource.Resource.WIRT_NM_DAUER_3;
                default: return "";
            }
        }

        /// <summary>Der Anzeigetext eines Wirkungsgrads („keine" … „stark"); leer ohne Wert.</summary>
        public static string WirkungText(int? wirkung)
        {
            switch (wirkung)
            {
                case 0: return MyResource.Resource.WIRT_NM_WIRKUNG_0;
                case 1: return MyResource.Resource.WIRT_NM_WIRKUNG_1;
                case 2: return MyResource.Resource.WIRT_NM_WIRKUNG_2;
                case 3: return MyResource.Resource.WIRT_NM_WIRKUNG_3;
                default: return "";
            }
        }

        /// <summary>
        /// Die Beurteilung als Anzeige: die Zahl mit ihrem Rahmen („6 von 9") oder
        /// „nicht beurteilt".
        /// </summary>
        public static string BeurteilungText(ProjektWirkung w, CultureInfo kultur = null)
        {
            int? b = Beurteilung(w);
            if (!b.HasValue) return MyResource.Resource.WIRT_NM_NICHT_BEURTEILT;
            return string.Format(kultur ?? CultureInfo.CurrentCulture, MyResource.Resource.WIRT_NM_BEURTEILUNG_WERT,
                                 b.Value, BEURTEILUNG_MAX);
        }

        /// <summary>Die Dauerstufen als Wahl (Id = Wert).</summary>
        public static IReadOnlyList<KeyValuePair<int, string>> Dauerstufen()
        {
            var l = new List<KeyValuePair<int, string>>();
            for (int d = DAUER_MIN; d <= DAUER_MAX; d++) l.Add(new KeyValuePair<int, string>(d, DauerText(d)));
            return l;
        }

        /// <summary>Die Wirkungsgrade als Wahl (Id = Wert).</summary>
        public static IReadOnlyList<KeyValuePair<int, string>> Wirkungsgrade()
        {
            var l = new List<KeyValuePair<int, string>>();
            for (int g = WIRKUNG_MIN; g <= WIRKUNG_MAX; g++) l.Add(new KeyValuePair<int, string>(g, WirkungText(g)));
            return l;
        }

        /// <summary>Die Kategorien als Wahl (Id = Listenplatz in <see cref="KATEGORIEN"/>).</summary>
        public static IReadOnlyList<KeyValuePair<int, string>> Kategorien()
        {
            var l = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < KATEGORIEN.Count; i++) l.Add(new KeyValuePair<int, string>(i, KategorieText(KATEGORIEN[i])));
            return l;
        }

        /// <summary>
        /// Ist die Zeile LEER — keine Beschreibung und keine Beurteilungsangabe? Leere Zeilen
        /// werden beim Speichern übergangen (eine hinzugefügte, nie ausgefüllte Zeile).
        /// </summary>
        public static bool IstLeer(ProjektWirkung w)
            => w == null || (string.IsNullOrWhiteSpace(w.Beschreibung) && !w.Dauer.HasValue
                             && !w.WirkungOrganisation.HasValue && !w.WirkungMitarbeiter.HasValue
                             && !w.WirkungUmwelt.HasValue);

        /// <summary>
        /// Prüft eine Zeile vor dem Speichern: Kategorie bekannt, Beschreibung vorhanden (wenn
        /// die Zeile nicht leer ist), Dauer und Wirkungen in ihrer Skala. Rückgabe: der Befund
        /// als Satz, <c>null</c> = in Ordnung.
        /// </summary>
        public static string Pruefen(ProjektWirkung w)
        {
            if (w == null || IstLeer(w)) return null;
            if (!KategorieGueltig(w.Kategorie))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.WIRT_NM_FEHLER_KATEGORIE, w.Kategorie ?? "");
            if (string.IsNullOrWhiteSpace(w.Beschreibung)) return MyResource.Resource.WIRT_NM_FEHLER_BESCHREIBUNG;
            if (w.Dauer.HasValue && (w.Dauer.Value < DAUER_MIN || w.Dauer.Value > DAUER_MAX))
                return MyResource.Resource.WIRT_NM_FEHLER_DAUER;
            foreach (int? g in new[] { w.WirkungOrganisation, w.WirkungMitarbeiter, w.WirkungUmwelt })
                if (g.HasValue && (g.Value < WIRKUNG_MIN || g.Value > WIRKUNG_MAX))
                    return MyResource.Resource.WIRT_NM_FEHLER_WIRKUNG;
            return null;
        }
    }
}
