using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Leser des Regelsatzes nach Anhang IV und die Zuordnung der Geräteklasse</b> (Konzept
    /// Kältemittel als Stammdatum 3.3 und 4.5, KM4-E2).
    ///
    /// <para>Die Regeln stehen als Zeilen der Klasse <c>FGAS</c> im Gesetzeskatalog; der Schlüssel trägt
    /// die Regel im festen Vokabular <c>FGAS_IV_&lt;GERAETEKLASSE&gt;_&lt;BAND&gt;_&lt;KRITERIUM&gt;</c>
    /// (<c>DbWerte.FGAS_*</c>). Eine Zeile, deren Schlüssel das Vokabular nicht kennt, wird
    /// <b>benannt verworfen</b> — sie steht mit Grund in der Liste <c>verworfen</c>, nie still.</para>
    ///
    /// <para>Die Geräteklasse kommt aus den Gerätefeldern: an der Kältemaschine <c>Geraeteart</c>
    /// (<c>KWS_*</c> → Kühler, <c>ABSORPTION</c> → kein Befund, <c>SPLIT</c>/<c>MULTISPLIT</c>/<c>VRF</c>
    /// in KM4 nicht prüfbar), an der Wärmepumpe <c>Bauart</c> und <c>Typ</c> (nur Luft-Wasser:
    /// Monoblock/Kompakt → Nr. 8, Split → Split Luft/Wasser; KM4-Q7 a). Abgleich Trim und
    /// <c>OrdinalIgnoreCase</c>.</para>
    /// </summary>
    public static class Kaeltemittelregeln
    {
        /// <summary>Bauart der Wärmepumpe (<c>Tab_WP.Bauart</c>): Monoblock.</summary>
        public const string WP_BAUART_MONOBLOCK = "Monoblock";

        /// <summary>Bauart der Wärmepumpe (<c>Tab_WP.Bauart</c>, VDI-3805-Import): Kompaktgerät.</summary>
        public const string WP_BAUART_KOMPAKT = "Kompakt";

        /// <summary>Bauart der Wärmepumpe (<c>Tab_WP.Bauart</c>): Split.</summary>
        public const string WP_BAUART_SPLIT = "Split";

        /// <summary>Liest die Regeln aus dem Gesetzeskatalog (<see cref="GesetzKatalog.AlleDerKlasse"/>).</summary>
        public static IReadOnlyList<KaeltemittelRegel> Lesen(GesetzKatalog katalog, List<string> verworfen = null)
        {
            if (katalog == null) return Array.Empty<KaeltemittelRegel>();
            return Zerlegen(katalog.AlleDerKlasse(DbWerte.GESETZ_KLASSE_FGAS), verworfen);
        }

        /// <summary>
        /// Zerlegt Katalogzeilen in Regeln. Zeilen einer anderen Klasse werden übergangen; Zeilen mit
        /// unbekanntem Schlüssel kommen mit Grund in <paramref name="verworfen"/>.
        /// </summary>
        public static IReadOnlyList<KaeltemittelRegel> Zerlegen(IEnumerable<GesetzParameter> zeilen, List<string> verworfen = null)
        {
            var liste = new List<KaeltemittelRegel>();
            if (zeilen == null) return liste;
            foreach (GesetzParameter p in zeilen)
            {
                if (p == null || !string.Equals(p.Klasse, DbWerte.GESETZ_KLASSE_FGAS, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!SchluesselZerlegen(p.Schluessel, out string klasse, out string band, out string kriterium, out string grund))
                {
                    verworfen?.Add(p.Schluessel + ": " + grund);
                    continue;
                }
                liste.Add(new KaeltemittelRegel(p.Schluessel, klasse, band, kriterium, p.JahrVon, p.Wert, p.Status, p.Quelle));
            }
            return liste;
        }

        /// <summary>
        /// Zerlegt einen Schlüssel von hinten: letztes Glied Kriterium, vorletztes Band, der Rest nach
        /// dem Präfix die Geräteklasse. <c>false</c> mit Grund, wenn ein Glied nicht im Vokabular steht.
        /// </summary>
        public static bool SchluesselZerlegen(string schluessel, out string geraeteklasse, out string band,
                                              out string kriterium, out string grund)
        {
            geraeteklasse = band = kriterium = null;
            string s = schluessel ?? "";
            if (!s.StartsWith(DbWerte.FGAS_PRAEFIX_IV, StringComparison.Ordinal))
            {
                grund = "Präfix " + DbWerte.FGAS_PRAEFIX_IV + " fehlt";
                return false;
            }
            string rest = s.Substring(DbWerte.FGAS_PRAEFIX_IV.Length);
            int k = rest.LastIndexOf('_');
            if (k <= 0) { grund = "Kriterium fehlt"; return false; }
            string krit = rest.Substring(k + 1);
            rest = rest.Substring(0, k);
            int b = rest.LastIndexOf('_');
            if (b <= 0) { grund = "Band oder Geräteklasse fehlt"; return false; }
            string bnd = rest.Substring(b + 1);
            string kl = rest.Substring(0, b);

            if (!Enthaelt(DbWerte.FGAS_KRITERIEN, krit)) { grund = "unbekanntes Kriterium " + krit; return false; }
            if (!Enthaelt(DbWerte.FGAS_BAENDER, bnd)) { grund = "unbekanntes Band " + bnd; return false; }
            if (!Enthaelt(DbWerte.FGAS_KLASSEN, kl)) { grund = "unbekannte Geräteklasse " + kl; return false; }

            geraeteklasse = kl; band = bnd; kriterium = krit; grund = null;
            return true;
        }

        /// <summary>
        /// Geräteklasse der Kältemaschine aus <c>Geraeteart</c>: <c>KWS_*</c> → <c>KUEHLER</c>,
        /// <c>ABSORPTION</c> → <see cref="Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION"/>; Split,
        /// Multisplit, VRF, leer und Unbekanntes → <c>null</c> (nicht prüfbar; die Split-Regeln greifen
        /// an der Kältemaschine erst mit K-D/K-E).
        /// </summary>
        public static string GeraeteklasseKaeltemaschine(string geraeteart)
        {
            string g = (geraeteart ?? "").Trim();
            if (Gleich(g, KaelteKatalogfelderSchema.GERAETEART_KWS_LUFT) ||
                Gleich(g, KaelteKatalogfelderSchema.GERAETEART_KWS_WASSER) ||
                Gleich(g, KaelteKatalogfelderSchema.GERAETEART_KWS_FREIKUEHLUNG))
                return DbWerte.FGAS_KLASSE_KUEHLER;
            if (Gleich(g, KaelteKatalogfelderSchema.GERAETEART_ABSORPTION))
                return Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION;
            return null;
        }

        /// <summary>
        /// Geräteklasse der Wärmepumpe aus <c>Bauart</c> und <c>Typ</c> (KM4-Q7 a): nur Luft-Wasser;
        /// Monoblock oder Kompakt → <c>MONOBLOCK</c> (Nr. 8), Split → <c>SPLIT_LUFTWASSER</c> (Nr. 9);
        /// sonst — Sole- und Wasser-Wasser, leere Bauart — <c>null</c> (nicht prüfbar).
        /// </summary>
        public static string GeraeteklasseWaermepumpe(string bauart, string typ)
        {
            if (!Gleich((typ ?? "").Trim(), DbWerte.WP_BAUART_LUFT_WASSER)) return null;
            string b = (bauart ?? "").Trim();
            if (Gleich(b, WP_BAUART_MONOBLOCK) || Gleich(b, WP_BAUART_KOMPAKT)) return DbWerte.FGAS_KLASSE_MONOBLOCK;
            if (Gleich(b, WP_BAUART_SPLIT)) return DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER;
            return null;
        }

        /// <summary>Der Prüfstoff aus einer Zeile der Kältemitteltabelle; <c>null</c> ohne Zeile.</summary>
        public static KaeltemittelStoff Stoff(KaeltemittelCtrl.Kaeltemittelsatz satz)
        {
            return satz == null ? null : new KaeltemittelStoff(satz.Code, satz.Gruppe, satz.GWP, satz.F_Gas);
        }

        private static bool Gleich(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool Enthaelt(IReadOnlyList<string> vorrat, string wert)
        {
            foreach (string v in vorrat)
                if (string.Equals(v, wert, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
