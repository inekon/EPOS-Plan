using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Schicht eines Aufbaus der Projektdatei</b> (<c>TcBuildingElementDimensionLayer</c>, Befund Projektdatei N.2–N.4):
    /// Dicke [m], λ [W/(m·K)], ρ [kg/m³] und c [J/(kg·K)] — c steht in der Datei in kJ/(kg·K) und ist hier schon mit 1 000
    /// multipliziert. Platzhalter (−987654321,99) sind <c>null</c>. Der Name ist ein Projektdatum (Stoff- oder Produktname)
    /// und kommt nie in Katalog, Wiki oder Repositorium.
    /// </summary>
    internal sealed class SqprojSchicht
    {
        internal string Kennung { get; init; }
        internal string Name { get; init; }
        internal int Stelle { get; init; }
        internal int? Schichttyp { get; init; }
        internal int? Stofftyp { get; init; }
        internal int? Stoffgruppe { get; init; }
        internal double? DickeM { get; init; }
        internal double? LambdaWmK { get; init; }
        internal double? RhoKgM3 { get; init; }
        internal double? CpJkgK { get; init; }

        /// <summary>Dämmschicht nach den Codes der Datei (<c>LayerType</c> 3 oder <c>MaterialType</c> 1, Befund N.2).</summary>
        internal bool IstDaemmung => Schichttyp == SqprojBauteilcodes.SCHICHT_DAEMMUNG || Stofftyp == SqprojBauteilcodes.STOFF_DAEMMSTOFF;

        /// <summary>Dicke, λ, ρ und c gesetzt und positiv.</summary>
        internal bool Vollstaendig => DickeM > 0.0 && LambdaWmK > 0.0 && RhoKgM3 > 0.0 && CpJkgK > 0.0;
    }

    /// <summary>
    /// <b>Ein Aufbau der Projektdatei</b> (<c>TcBuildingElementDimension</c>) mit seinen Schichten <b>innen → außen</b>
    /// (<c>SortNum</c> aufsteigend, Befund N.4). <see cref="UWert"/> ist das U der Datei (mit Rsi/Rse der Datei, die in den
    /// Spalten <c>Internal-</c>/<c>ExternalCoefficientOfHeatTransfer</c> als Widerstände stehen); EPOS setzt Rsi/Rse nach
    /// der Lage an (DIN EN ISO 6946) und übernimmt sie nicht.
    /// </summary>
    internal sealed class SqprojAufbau
    {
        internal string Kennung { get; init; } = "";
        internal string Name { get; init; }
        internal double? UWert { get; init; }
        internal double? RsiM2KW { get; init; }
        internal double? RseM2KW { get; init; }
        internal double? DickeM { get; init; }
        internal List<SqprojSchicht> Schichten { get; } = new List<SqprojSchicht>();

        /// <summary>Opak mit Schichten: mindestens eine Schicht und U &gt; 0.</summary>
        internal bool HatSchichten => Schichten.Count > 0 && UWert > 0.0;

        /// <summary>Die Schichtfolge als Vergleichsschlüssel (Dicke, λ, ρ, c je Schicht) — gleiche Folge, gleicher Aufbau.</summary>
        internal string Signatur => string.Join("|", Schichten.Select(s => string.Join(";",
            Text(s.DickeM), Text(s.LambdaWmK), Text(s.RhoKgM3), Text(s.CpJkgK))));

        private static string Text(double? w) => w.HasValue ? Math.Round(w.Value, 6).ToString("R", CultureInfo.InvariantCulture) : "-";
    }

    /// <summary>Ein Raumbezug einer Hüllfläche (<c>BmElementReference</c>): Raum, Rolle (<c>ReferenceType</c>), Reihenfolge.</summary>
    internal sealed record SqprojBezug(string RaumUuid, int? Rolle, int Rang);

    /// <summary>
    /// <b>Eine raumbezogene Hüllfläche der Projektdatei</b> (<c>BmElement</c> Level 3, Befund N.1): ihre <see cref="Gid"/>
    /// (Normalform) ist die Eigenschaft <c>GUID</c> des IFC-Bauteils; <see cref="AufbauKennung"/> zeigt über
    /// <c>CatalogDimUUID</c> auf den Aufbau. Eine Zeile kann <b>zwei</b> Räume tragen (Innenwand, Geschossdecke).
    /// </summary>
    internal sealed class SqprojHuellflaeche
    {
        internal string Uuid { get; init; } = "";
        internal string Gid { get; init; }
        internal int? Elementtyp { get; init; }
        internal int? Nachbarart { get; init; }
        internal string AufbauKennung { get; init; }
        internal double? UWert { get; init; }
        internal double? NettoM2 { get; init; }
        internal List<SqprojBezug> Bezuege { get; } = new List<SqprojBezug>();

        /// <summary>
        /// Der Raum auf der Innenseite der Schichtfolge (Befund N.10): bei einer Decke der obere Raum (Rolle
        /// <see cref="SqprojBauteilcodes.ROLLE_BODEN"/>), sonst der erste Bezug; <c>null</c> ohne Bezug.
        /// </summary>
        internal string InnenRaum
            => (Bezuege.FirstOrDefault(b => b.Rolle == SqprojBauteilcodes.ROLLE_BODEN) ?? Bezuege.OrderBy(b => b.Rang).FirstOrDefault())?.RaumUuid;
    }

    /// <summary>Die gedeuteten Codes der Bauteiltabellen (nur die belegten Werte des Befunds N.2, N.6).</summary>
    internal static class SqprojBauteilcodes
    {
        /// <summary><c>LayerType</c> 3: Dämmschicht (0: tragende/massive Schicht).</summary>
        internal const int SCHICHT_DAEMMUNG = 3;
        /// <summary><c>MaterialType</c> 1: Dämmstoff.</summary>
        internal const int STOFF_DAEMMSTOFF = 1;
        /// <summary><c>ReferenceType</c> 8: der Raum, dessen Boden die Fläche ist (bei einer Decke der obere Raum).</summary>
        internal const int ROLLE_BODEN = 8;
        /// <summary><c>RepositoryLevel</c> der raumbezogenen Hüllflächen.</summary>
        internal const int LEVEL_HUELLFLAECHE = 3;
        /// <summary>Der Platzhalter „nicht gesetzt“ in Zahlenspalten.</summary>
        internal const double PLATZHALTER = -987654321.99;
        /// <summary>Die Null-Kennung „nicht gesetzt“ in Kennungsspalten (Normalform).</summary>
        internal const string NULLKENNUNG = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        /// <summary>Unter dieser Zahl steht c in kJ/(kg·K) (Befund N.3) und wird mit 1 000 multipliziert.</summary>
        internal const double CP_KJ_GRENZE = 50.0;

        /// <summary>Ein Zahlenwert ohne Platzhalter (alles unter −10⁸ gilt als Platzhalter).</summary>
        internal static double? Gesetzt(double? w) => w is double d && d > -1e8 ? d : null;

        /// <summary>Eine Kennung in der GUID-Normalform ohne Null-Kennung; <c>null</c> = nicht gesetzt.</summary>
        internal static string Kennung(string text)
        {
            string k = IfcAbbildBauer.GuidNormalform(text);
            return k == null || string.Equals(k, NULLKENNUNG, StringComparison.OrdinalIgnoreCase) ? null : k;
        }

        /// <summary>c in J/(kg·K): ein Wert unter <see cref="CP_KJ_GRENZE"/> steht in kJ/(kg·K) (Befund N.3).</summary>
        internal static double? CpJkgK(double? roh) => roh is double c && c > 0.0 ? (c < CP_KJ_GRENZE ? c * 1000.0 : c) : null;
    }
}
