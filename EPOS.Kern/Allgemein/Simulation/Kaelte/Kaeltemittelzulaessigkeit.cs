using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Ergebnisklassen der Zulässigkeitsprüfung (Konzept Kältemittel als Stammdatum 3.3).</summary>
    public enum KaeltemittelErgebnisklasse
    {
        /// <summary>(1) Keine Regel trifft.</summary>
        Zulaessig = 1,

        /// <summary>(2) Ab dem Stichtag nur Restbestand oder Ausnahme — Warnung, nie Sperre.</summary>
        AbStichtag = 2,

        /// <summary>(3) Bestandsanlage — Anhang IV nicht anwendbar. Vorgesehen für ein Bestandskennzeichen
        /// (KM4-Q12 b); in dieser Fassung tritt die Klasse nicht ein.</summary>
        Bestand = 3,

        /// <summary>(4) Nicht prüfbar — nur bei gesetztem Jahr, wenn eine andere Eingangsgröße fehlt; Hinweis.</summary>
        NichtPruefbar = 4
    }

    /// <summary>
    /// Eine Regel nach Anhang IV, zerlegt aus einer Zeile der Klasse <c>FGAS</c> des Gesetzeskatalogs
    /// (<see cref="Kaeltemittelregeln.Zerlegen"/>). <see cref="Grenze"/> ist die GWP-Grenze
    /// (<c>null</c> = Zeile ohne Wert, die Regel ist ab diesem Stichtag entfallen).
    /// </summary>
    public sealed record KaeltemittelRegel(
        string Schluessel, string Geraeteklasse, string Band, string Kriterium, int Stichtag,
        double? Grenze, string Status, string Quelle);

    /// <summary>
    /// Das Kältemittel, wie die Prüfung es braucht: Code, Stoffgruppe, Tabellen-GWP und F-Gas-Kennzeichen
    /// aus <c>Tab_Kaeltemittel_STAMM</c>. <c>null</c> in einem Feld heißt „unbekannt“.
    /// </summary>
    public sealed record KaeltemittelStoff(string Code, string Gruppe, double? Gwp, bool? FGas);

    /// <summary>Der Eingang der Prüfung einer Anlage (Konzept 3.3, Tabelle „Eingangsgrößen“).</summary>
    public sealed class KaeltemittelEingang
    {
        /// <summary>Geräteklasse aus dem Vokabular (<c>DbWerte.FGAS_KLASSE_*</c>) oder
        /// <see cref="Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION"/>; leer = nicht bestimmbar.</summary>
        public string Geraeteklasse { get; init; }

        /// <summary>Nennleistung je Gerät [kW]; <c>null</c> = fehlt.</summary>
        public double? LeistungKw { get; init; }

        /// <summary>Die Tabellenzeile des Kältemittels; <c>null</c> = kein Treffer (leer, <c>SONSTIGES</c>, unbekannter Text).</summary>
        public KaeltemittelStoff Stoff { get; init; }

        /// <summary>GWP der Geräteangabe (<c>Kaeltemittel_GWP</c> der Kältemaschine); nur ohne Tabellen-GWP benutzt (KM4-Q17 a).</summary>
        public double? GeraeteGwp { get; init; }

        /// <summary>Jahr der geplanten Inbetriebnahme; <c>null</c> = keine Prüfung.</summary>
        public int? Jahr { get; init; }

        /// <summary>Der gesäte Regelsatz.</summary>
        public IReadOnlyList<KaeltemittelRegel> Regeln { get; init; } = Array.Empty<KaeltemittelRegel>();
    }

    /// <summary>
    /// Das Ergebnis der Prüfung einer Anlage. Bei Klasse 2 tragen <see cref="Regelschluessel"/>,
    /// <see cref="Grenze"/> und <see cref="Stichtag"/> die treffende Regel des frühesten Stichtags; bei
    /// Klasse 4 nennt <see cref="FehlendeGroesse"/> die fehlende Größe (<c>FEHLT_*</c>).
    /// </summary>
    public sealed record KaeltemittelErgebnis(
        KaeltemittelErgebnisklasse Klasse, string Regelschluessel, double? Grenze, int? Stichtag,
        int Jahr, double? LeistungKw, double? Gwp, bool GwpAusTabelle, string FehlendeGroesse);

    /// <summary>
    /// <b>Die Zulässigkeitsprüfung eines Kältemittels nach Anhang IV der Verordnung (EU) 2024/573</b> —
    /// reine Klasse ohne Datenbank (Konzept Kältemittel als Stammdatum 3.3 und 5.1, Entscheid E123).
    ///
    /// <para>Anhang IV untersagt das <b>Inverkehrbringen</b> (Art. 11 Abs. 1), nicht den Betrieb. Das Jahr
    /// der geplanten Inbetriebnahme ist nur ein konservativer Stellvertreter, und die Prüfung kennt die
    /// Ausnahmen (Sicherheitsanforderungen am Standort, Kühlung unter −50 °C, Durchführungsrechtsakte)
    /// nicht. Deshalb <b>warnt</b> sie und sperrt nie.</para>
    ///
    /// <para><b>Reihenfolge (5.1):</b> ohne Jahr kein Ergebnis; Absorption kein Ergebnis; fehlt
    /// Geräteklasse, Leistung oder Kältemittel → Klasse 4; sonst die Regeln der Geräteklasse und ihrer
    /// Oberklasse, deren Band die Leistung enthält, je Schlüssel die jüngste Zeile mit Stichtag ≤ Jahr
    /// (Zeitreihe); über die Schlüssel hinweg gelten alle. Trifft eine → Klasse 2 mit der treffenden
    /// Regel des frühesten Stichtags; bleibt eine Regel unentscheidbar (GWP, F-Gas-Kennzeichen oder
    /// Stoffklasse fehlt) → Klasse 4; sonst Klasse 1.</para>
    ///
    /// <para><b>Stichtag einer Zeitreihe.</b> Trifft die jüngste Zeile eines Schlüssels, gilt als
    /// Stichtag die früheste Zeile, ab der die Reihe ohne Unterbrechung trifft — ein Split über 12 kW
    /// mit GWP 2 000 ist ab 2029 betroffen, nicht erst ab der Verschärfung 2033.</para>
    /// </summary>
    public static class Kaeltemittelzulaessigkeit
    {
        /// <summary>Geräteklasse ohne F-Gas-Kreis: kein Befund (Konzept 3.3).</summary>
        public const string GERAETEKLASSE_ABSORPTION = "ABSORPTION";

        /// <summary>Fehlende Größe: Geräteklasse (leer, nicht prüfbare Bauart).</summary>
        public const string FEHLT_GERAETEKLASSE = "GERAETEKLASSE";

        /// <summary>Fehlende Größe: Nennleistung.</summary>
        public const string FEHLT_LEISTUNG = "LEISTUNG";

        /// <summary>Fehlende Größe: Kältemittel (kein Tabellentreffer und kein Geräte-GWP).</summary>
        public const string FEHLT_KAELTEMITTEL = "KAELTEMITTEL";

        /// <summary>Fehlende Größe: GWP (für ein GWP-Kriterium).</summary>
        public const string FEHLT_GWP = "GWP";

        /// <summary>Fehlende Größe: F-Gas-Kennzeichen (für „jedes F-Gas“).</summary>
        public const string FEHLT_FGAS = "FGAS";

        /// <summary>Fehlende Größe: Stoffklasse (für ein HFKW-Kriterium).</summary>
        public const string FEHLT_STOFFKLASSE = "STOFFKLASSE";

        /// <summary>
        /// <b>Gemische mit einem Bestandteil aus Anhang I Gruppe 1 (HFKW).</b> Die Stoffgruppe einer
        /// Gemischzeile (<c>GEMISCH</c>) sagt nicht, ob ein HFKW darin ist; das ist hier je Code
        /// festgelegt. Alle acht Gemische der Auslieferung enthalten R32, R125, R134a oder R143a
        /// (HFKW-32, -125, -134a, -143a, Anhang I Gruppe 1). Ein anderes Gemisch ist für ein
        /// HFKW-Kriterium „Stoffklasse unbekannt“.
        /// </summary>
        public static readonly IReadOnlyList<string> GEMISCHE_MIT_HFKW = new[]
        {
            "R410A", "R407C", "R404A", "R454B", "R454C", "R452B", "R455A", "R513A"
        };

        /// <summary>
        /// Enthält der Stoff ein HFKW im Sinn von Art. 3 Nr. 4 (Anhang I Gruppe 1)? Gruppe <c>HFKW</c> → ja;
        /// <c>HFO</c>, <c>HFCKW</c> (Anhang II, keine HFKW), <c>NATUERLICH</c> → nein; <c>GEMISCH</c> → nach
        /// <see cref="GEMISCHE_MIT_HFKW"/>, sonst unbekannt; leer oder <c>SONSTIGE</c> → unbekannt.
        /// </summary>
        public static bool? EnthaeltHfkw(KaeltemittelStoff stoff)
        {
            if (stoff == null) return null;
            string g = (stoff.Gruppe ?? "").Trim();
            if (string.Equals(g, DbWerte.KAELTEMITTEL_GRUPPE_HFKW, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(g, DbWerte.KAELTEMITTEL_GRUPPE_HFO, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g, DbWerte.KAELTEMITTEL_GRUPPE_HFCKW, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g, DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(g, DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, StringComparison.OrdinalIgnoreCase))
            {
                string code = (stoff.Code ?? "").Trim();
                foreach (string c in GEMISCHE_MIT_HFKW)
                    if (string.Equals(c, code, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return null;
        }

        /// <summary>Die Oberklasse einer Geräteklasse (<c>SPLIT_LUFTWASSER</c>, <c>SPLIT_LUFTLUFT</c> → <c>SPLIT</c>), sonst <c>null</c>.</summary>
        public static string Oberklasse(string geraeteklasse)
        {
            if (string.Equals(geraeteklasse, DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER, StringComparison.Ordinal) ||
                string.Equals(geraeteklasse, DbWerte.FGAS_KLASSE_SPLIT_LUFTLUFT, StringComparison.Ordinal))
                return DbWerte.FGAS_KLASSE_SPLIT;
            return null;
        }

        /// <summary>
        /// Liegt die Leistung im Band? Intervall mit ausschließlicher unterer und einschließlicher oberer
        /// Grenze: <c>BIS12</c> (−∞, 12], <c>UEBER12</c> (12, ∞), <c>UEBER12BIS50</c> (12, 50],
        /// <c>UEBER50</c> (50, ∞), <c>ALLE</c> jede. Ein unbekanntes Band enthält nichts.
        /// </summary>
        public static bool BandEnthaelt(string band, double leistungKw)
        {
            switch (band)
            {
                case DbWerte.FGAS_BAND_ALLE: return true;
                case DbWerte.FGAS_BAND_BIS12: return leistungKw <= 12.0;
                case DbWerte.FGAS_BAND_UEBER12: return leistungKw > 12.0;
                case DbWerte.FGAS_BAND_UEBER12BIS50: return leistungKw > 12.0 && leistungKw <= 50.0;
                case DbWerte.FGAS_BAND_UEBER50: return leistungKw > 50.0;
                default: return false;
            }
        }

        /// <summary>Das wirksame GWP nach KM4-Q17 a: Tabellenwert vor Geräteangabe.</summary>
        public static double? WirksamesGwp(KaeltemittelStoff stoff, double? geraeteGwp, out bool ausTabelle)
        {
            if (stoff != null && stoff.Gwp.HasValue) { ausTabelle = true; return stoff.Gwp; }
            ausTabelle = false;
            return geraeteGwp;
        }

        /// <summary>Prüft eine Anlage. <c>null</c> ohne Jahr und bei Absorption (kein Befund).</summary>
        public static KaeltemittelErgebnis Pruefen(KaeltemittelEingang e)
        {
            if (e == null || !e.Jahr.HasValue) return null;
            int jahr = e.Jahr.Value;
            string klasse = (e.Geraeteklasse ?? "").Trim();
            if (string.Equals(klasse, GERAETEKLASSE_ABSORPTION, StringComparison.OrdinalIgnoreCase)) return null;

            double? gwp = WirksamesGwp(e.Stoff, e.GeraeteGwp, out bool ausTabelle);

            if (klasse.Length == 0)
                return NichtPruefbar(FEHLT_GERAETEKLASSE, jahr, e.LeistungKw, gwp, ausTabelle);
            if (!e.LeistungKw.HasValue || double.IsNaN(e.LeistungKw.Value) || e.LeistungKw.Value <= 0)
                return NichtPruefbar(FEHLT_LEISTUNG, jahr, e.LeistungKw, gwp, ausTabelle);
            if (e.Stoff == null && !gwp.HasValue)
                return NichtPruefbar(FEHLT_KAELTEMITTEL, jahr, e.LeistungKw, gwp, ausTabelle);

            double leistung = e.LeistungKw.Value;
            string ober = Oberklasse(klasse);

            // Regeln der Klasse samt Oberklasse im Band, je Schlüssel aufsteigend nach Stichtag.
            var reihen = new SortedDictionary<string, List<KaeltemittelRegel>>(StringComparer.Ordinal);
            foreach (KaeltemittelRegel r in e.Regeln ?? Array.Empty<KaeltemittelRegel>())
            {
                if (r == null) continue;
                bool klassePasst = string.Equals(r.Geraeteklasse, klasse, StringComparison.Ordinal) ||
                                   (ober != null && string.Equals(r.Geraeteklasse, ober, StringComparison.Ordinal));
                if (!klassePasst || !BandEnthaelt(r.Band, leistung)) continue;
                if (!reihen.TryGetValue(r.Schluessel, out List<KaeltemittelRegel> reihe))
                    reihen[r.Schluessel] = reihe = new List<KaeltemittelRegel>();
                reihe.Add(r);
            }

            KaeltemittelRegel treffer = null;
            string offen = null;
            foreach (KeyValuePair<string, List<KaeltemittelRegel>> kv in reihen)
            {
                List<KaeltemittelRegel> reihe = kv.Value;
                reihe.Sort((a, b) => a.Stichtag.CompareTo(b.Stichtag));

                int jung = -1;
                for (int i = 0; i < reihe.Count; i++)
                    if (reihe[i].Stichtag <= jahr) jung = i;
                if (jung < 0) continue;

                bool? trifft = Trifft(reihe[jung], e.Stoff, gwp, out string fehlt);
                if (trifft == null) { offen ??= fehlt; continue; }
                if (trifft == false) continue;

                // Stichtag der Reihe: die früheste Zeile, ab der sie ohne Unterbrechung trifft.
                int frueh = jung;
                while (frueh > 0 && Trifft(reihe[frueh - 1], e.Stoff, gwp, out _) == true) frueh--;
                KaeltemittelRegel r = reihe[frueh];
                if (treffer == null || r.Stichtag < treffer.Stichtag) treffer = r;
            }

            if (treffer != null)
                return new KaeltemittelErgebnis(KaeltemittelErgebnisklasse.AbStichtag, treffer.Schluessel,
                    treffer.Grenze, treffer.Stichtag, jahr, leistung, gwp, ausTabelle, null);
            if (offen != null)
                return NichtPruefbar(offen, jahr, leistung, gwp, ausTabelle);
            return new KaeltemittelErgebnis(KaeltemittelErgebnisklasse.Zulaessig, null, null, null,
                jahr, leistung, gwp, ausTabelle, null);
        }

        /// <summary>
        /// Trifft eine Regelzeile? <c>true</c>/<c>false</c>, oder <c>null</c> mit der fehlenden Größe,
        /// wenn die Entscheidung an einer unbekannten Größe hängt. Eine Zeile ohne Wert ist entfallen
        /// und trifft nie.
        /// </summary>
        public static bool? Trifft(KaeltemittelRegel r, KaeltemittelStoff stoff, double? gwp, out string fehlt)
        {
            fehlt = null;
            if (r == null || !r.Grenze.HasValue) return false;
            switch (r.Kriterium)
            {
                case DbWerte.FGAS_KRIT_FGAS:
                    if (stoff == null || !stoff.FGas.HasValue) { fehlt = FEHLT_FGAS; return null; }
                    return stoff.FGas.Value;

                case DbWerte.FGAS_KRIT_GWP:
                {
                    // „fluorierte Treibhausgase mit einem GWP von … oder mehr“: ein Stoff, den die
                    // Tabelle als Nicht-F-Gas führt, trifft nicht — gleich welches GWP. Ohne
                    // Kennzeichen (kein Tabellentreffer, nur das Geräte-GWP nach KM4-Q17) gilt ein
                    // GWP über der Grenze als F-Gas: konservativ, denn die Prüfung warnt nur.
                    if (stoff != null && stoff.FGas == false) return false;
                    if (!gwp.HasValue) { fehlt = FEHLT_GWP; return null; }
                    return gwp.Value >= r.Grenze.Value;
                }

                case DbWerte.FGAS_KRIT_HFKW:
                {
                    bool? hfkw = EnthaeltHfkw(stoff);
                    if (hfkw == false) return false;
                    if (!gwp.HasValue) { fehlt = FEHLT_GWP; return null; }
                    if (gwp.Value < r.Grenze.Value) return false;
                    if (hfkw == null) { fehlt = FEHLT_STOFFKLASSE; return null; }
                    return true;
                }

                default:
                    return false;
            }
        }

        private static KaeltemittelErgebnis NichtPruefbar(string fehlt, int jahr, double? leistung, double? gwp, bool ausTabelle)
        {
            return new KaeltemittelErgebnis(KaeltemittelErgebnisklasse.NichtPruefbar, null, null, null,
                jahr, leistung, gwp, ausTabelle, fehlt);
        }
    }
}
