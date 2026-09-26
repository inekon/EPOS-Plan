using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Regel der Gebäudeart: ein Muster über die normierte <c>Tab_Gebaeude.Gebaeudeart</c>
    /// (<c>*</c> steht für beliebig viele Zeichen) und der Wert von <c>buildingTypeEnum</c>.
    /// </summary>
    internal sealed class Gebaeudetypregel
    {
        /// <summary>Legt die Regel an; der Wert muss ein Wert von <c>buildingTypeEnum</c> sein.</summary>
        internal Gebaeudetypregel(string muster, string wert)
        {
            if (string.IsNullOrWhiteSpace(muster)) throw new ArgumentException("Die Regel braucht ein Muster.", nameof(muster));
            if (!GbxmlVokabular.Gebaeudearten.Contains(wert, StringComparer.Ordinal))
                throw new ArgumentException("Kein Wert von buildingTypeEnum: " + wert, nameof(wert));
            Muster = GebaeudeExportProfil.Normieren(muster);
            Wert = wert;
        }

        /// <summary>Das normierte Muster.</summary>
        internal string Muster { get; }

        /// <summary>Der Wert von <c>buildingTypeEnum</c>.</summary>
        internal string Wert { get; }
    }

    /// <summary>Das Ergebnis der Abbildung einer Gebäudeart auf <c>buildingType</c>.</summary>
    /// <param name="Wert">Der Wert von <c>buildingTypeEnum</c>; ohne Treffer <see cref="GbxmlVokabular.Unknown"/>.</param>
    /// <param name="Bekannt">Hat eine Regel getroffen? Ohne Treffer meldet der Ablauf den Rückfall.</param>
    /// <param name="Normiert">Die normierte Gebäudeart, wie sie verglichen wurde (leer bei NULL).</param>
    internal readonly record struct Gebaeudetypwahl(string Wert, bool Bekannt, string Normiert);

    /// <summary>
    /// <b>Das Profil des Gebäudeexports</b> (Stufe G7a; Softwarearchitektur 1.6) — was je Format und
    /// Aufruf verschieden ist, als <b>Daten</b>: das Format (gbXML), die Uhr, die Sprache der
    /// Dateitexte, die Programmangaben für <c>DocumentHistory</c>, das Kennzeichen der Testlizenz und
    /// die Tabelle der Gebäudearten. Die Hülle belegt es; Ablauf und Schreiber lesen es nur.
    ///
    /// <para><b>Die Uhr steckt im Profil</b> (<see cref="Uhr"/>), damit ein Test die Datei byte-gleich
    /// wiederholen kann: Der Zeitstempel steht an genau einer Stelle
    /// (<c>DocumentHistory/CreatedBy/@date</c>).</para>
    ///
    /// <para><b>Keine Anwender- und keine Lizenzdaten in der Datei:</b> Das Profil trägt nur, ob eine
    /// Testlizenz läuft (<see cref="Testlizenz"/>, aus <c>LizenzToken.Typ == "demo"</c>, siehe
    /// <see cref="IstTestlizenz"/>) — das Wasserzeichen setzt der Ablauf in
    /// <c>Campus/Description</c> (Softwarearchitektur 4.3, Lizenzkonzept 2.1).</para>
    ///
    /// <para><b>Die Gebäudeart</b> (<see cref="Gebaeudetyp"/>): Die Gebäudeart des Gebäudes wird
    /// getrimmt, umlautnormiert und ohne Groß-/Kleinschreibung verglichen; die erste passende Regel der
    /// Tabelle gilt, ohne Treffer <c>Unknown</c> mit Meldung.</para>
    /// </summary>
    internal sealed class GebaeudeExportProfil
    {
        /// <summary>Der Programmname in <c>DocumentHistory</c> (<c>ProgramInfo</c> und die neutrale <c>PersonInfo</c>).</summary>
        public const string PROGRAMMNAME = "EPOS-Plan";

        /// <summary>Dateifilter des Speicherdialogs.</summary>
        public const string DATEIFILTER = "(*.xml)|*.xml";

        /// <summary>Der Lizenztyp der Testversion (<c>LizenzToken.Typ</c>).</summary>
        public const string LIZENZTYP_TEST = "demo";

        /// <summary>
        /// Die Tabelle der Gebäudearten (Umsetzungsauftrag G7a, 2.3): Einfamilien- und Reihenhäuser →
        /// <c>SingleFamily</c>, Mehrfamilienhäuser und Wohnblock → <c>MultiFamily</c>, Hotel,
        /// Krankenhaus und Altenheim → <c>HospitalOrHealthcare</c>, Schule → <c>SchoolOrUniversity</c>,
        /// Verwaltung → <c>Office</c>, Kaufhalle und Kaufhaus → <c>Retail</c>, Industriehalle →
        /// <c>Manufacturing</c>, Sporthalle → <c>Gymnasium</c>, Hallenbad → <c>SportsArena</c>; der Rest
        /// (Gewerbe, Sonstige, NULL) ist <c>Unknown</c>.
        /// </summary>
        public static readonly IReadOnlyList<Gebaeudetypregel> StandardGebaeudetypen = new[]
        {
            new Gebaeudetypregel("Einfamilienhaus", "SingleFamily"),
            new Gebaeudetypregel("Reihen*haus", "SingleFamily"),
            new Gebaeudetypregel("*Mehrfamilienhaus", "MultiFamily"),
            new Gebaeudetypregel("Wohnblock", "MultiFamily"),
            new Gebaeudetypregel("Hotel", "Hotel"),
            new Gebaeudetypregel("Krankenhaus", "HospitalOrHealthcare"),
            new Gebaeudetypregel("Altenheim", "HospitalOrHealthcare"),
            new Gebaeudetypregel("Schule", "SchoolOrUniversity"),
            new Gebaeudetypregel("Verwaltung*", "Office"),
            new Gebaeudetypregel("Kaufhalle", "Retail"),
            new Gebaeudetypregel("Kaufhaus", "Retail"),
            new Gebaeudetypregel("Industriehalle", "Manufacturing"),
            new Gebaeudetypregel("Sporthalle", "Gymnasium"),
            new Gebaeudetypregel("Hallenbad", "SportsArena"),
        };

        /// <summary>Legt das Profil an.</summary>
        /// <param name="sprache">Die Sprache der Dateitexte (die Oberflächensprache).</param>
        /// <param name="uhr">Die Uhr des Zeitstempels.</param>
        /// <param name="testlizenz">Läuft eine Testlizenz (<see cref="IstTestlizenz"/>)?</param>
        /// <param name="programmversion">Die Programmversion für <c>DocumentHistory</c>.</param>
        /// <param name="gebaeudetypen">Die Tabelle der Gebäudearten; <c>null</c> = <see cref="StandardGebaeudetypen"/>.</param>
        internal GebaeudeExportProfil(CultureInfo sprache, Func<DateTime> uhr, bool testlizenz, string programmversion,
                                      IReadOnlyList<Gebaeudetypregel> gebaeudetypen = null)
        {
            if (string.IsNullOrWhiteSpace(programmversion))
                throw new ArgumentException("Das Profil braucht eine Programmversion.", nameof(programmversion));
            Sprache = sprache ?? throw new ArgumentNullException(nameof(sprache));
            Uhr = uhr ?? throw new ArgumentNullException(nameof(uhr));
            Testlizenz = testlizenz;
            Programmversion = programmversion.Trim();
            Gebaeudetypen = gebaeudetypen ?? StandardGebaeudetypen;
        }

        /// <summary>Das Format (<see cref="GebaeudeQuelle.FORMAT_GBXML"/>).</summary>
        internal string Format => GebaeudeQuelle.FORMAT_GBXML;

        /// <summary>Die Sprache der Dateitexte (Produktausweis, Vorbehalt, Vermerke).</summary>
        internal CultureInfo Sprache { get; }

        /// <summary>Die Uhr des Zeitstempels in <c>DocumentHistory/CreatedBy/@date</c>.</summary>
        internal Func<DateTime> Uhr { get; }

        /// <summary>Läuft eine Testlizenz? Dann setzt der Ablauf das Wasserzeichen in <c>Campus/Description</c>.</summary>
        internal bool Testlizenz { get; }

        /// <summary>Der Programmname (<see cref="PROGRAMMNAME"/>).</summary>
        internal string Programmname => PROGRAMMNAME;

        /// <summary>Die Programmversion.</summary>
        internal string Programmversion { get; }

        /// <summary>Die Tabelle der Gebäudearten.</summary>
        internal IReadOnlyList<Gebaeudetypregel> Gebaeudetypen { get; }

        /// <summary>Ein neuer Schreiber des Formats je Lauf.</summary>
        internal IGebaeudeSchreiber SchreiberErzeugen() => new GbxmlSchreiber();

        /// <summary>Ist der Lizenztyp der der Testversion (<c>LizenzToken.Typ == "demo"</c>)?</summary>
        internal static bool IstTestlizenz(string lizenztyp) => string.Equals(lizenztyp, LIZENZTYP_TEST, StringComparison.Ordinal);

        /// <summary>
        /// Die Gebäudeart als <c>buildingType</c>: die erste Regel, deren Muster die normierte Art trifft;
        /// ohne Treffer (auch NULL, leer, „Gewerbe", „Sonstige") <see cref="GbxmlVokabular.Unknown"/>
        /// mit <see cref="Gebaeudetypwahl.Bekannt"/> = <c>false</c>.
        /// </summary>
        internal Gebaeudetypwahl Gebaeudetyp(string gebaeudeart)
        {
            string normiert = Normieren(gebaeudeart);
            if (normiert.Length > 0)
                foreach (Gebaeudetypregel r in Gebaeudetypen)
                    if (Passt(normiert, r.Muster)) return new Gebaeudetypwahl(r.Wert, true, normiert);
            return new Gebaeudetypwahl(GbxmlVokabular.Unknown, false, normiert);
        }

        /// <summary>
        /// Normiert einen Text für den Vergleich: getrimmt, innere Leerraumfolgen zu einem Leerzeichen,
        /// klein (invariant), ä → ae, ö → oe, ü → ue, ß → ss; <c>null</c> wird leer.
        /// </summary>
        internal static string Normieren(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sb = new StringBuilder(text.Length + 4);
            bool leer = false;
            foreach (char z in text.Trim().ToLowerInvariant())
            {
                if (char.IsWhiteSpace(z))
                {
                    if (!leer) sb.Append(' ');
                    leer = true;
                    continue;
                }
                leer = false;
                switch (z)
                {
                    case 'ä': sb.Append("ae"); break;
                    case 'ö': sb.Append("oe"); break;
                    case 'ü': sb.Append("ue"); break;
                    case 'ß': sb.Append("ss"); break;
                    default: sb.Append(z); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Trifft das Muster (mit <c>*</c> für beliebig viele Zeichen) den ganzen Text?</summary>
        private static bool Passt(string text, string muster)
        {
            string[] teile = muster.Split('*');
            if (teile.Length == 1) return string.Equals(text, muster, StringComparison.Ordinal);
            if (!text.StartsWith(teile[0], StringComparison.Ordinal)) return false;
            int pos = teile[0].Length;
            for (int i = 1; i < teile.Length - 1; i++)
            {
                int fund = text.IndexOf(teile[i], pos, StringComparison.Ordinal);
                if (fund < 0) return false;
                pos = fund + teile[i].Length;
            }
            string letzter = teile[teile.Length - 1];
            return text.Length - pos >= letzter.Length && text.EndsWith(letzter, StringComparison.Ordinal);
        }
    }
}
