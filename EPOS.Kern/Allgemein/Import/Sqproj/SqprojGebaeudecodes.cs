using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die festen Tabellen Gebäudecode der Projektdatei → EPOS</b> (Entwurf Gebäudedialog 2.2, Entscheid F4): Gebäudetyp
    /// (<c>BmBuilding.BuildingType</c>) und Nutzung (<c>BmBuildingData.BuildingUsageType</c>) auf Gebäudeart und Verwendung,
    /// Wärmeschutz- und Effizienzstandard (<c>BuildingHeatInsulationStandardType</c>, <c>BuildingEnergyEfficientType</c>) auf
    /// den Energiestandard (<see cref="Energiestandard.CODES"/>), dazu die wirksame Wärmekapazität <c>Cwirk</c> als Bauweise.
    /// Vorgabe im Code, Muster wie <see cref="Din18599Nutzung"/>; sprachneutral.
    /// <para><b>Nur belegte Codes.</b> Die Projektdatei trägt keine Klartexte zu ihren Codes (das Feldregister
    /// <c>XmFields</c> ist leer). Eine Zeile steht hier nur, wenn eine echte Datei sie belegt; jeder andere Code bleibt
    /// ohne Abbildung und erscheint benannt im Protokoll (<see cref="MELDUNG_OHNE_ABBILDUNG"/>). Der Code 0 heißt „keine
    /// Angabe“ (an der Belegdatei tragen Nutzung, Wärmeschutz- und Effizienzstandard 0) und meldet nichts.</para>
    /// <para><b>Belegt</b> (Protokoll GD-2): Gebäudetyp 3 an einem Nichtwohngebäude, dessen Zonen nach der Fläche
    /// Nichtwohnprofile tragen → Verwendung Nichtwohngebäude. Gebäudeart und Energiestandard hat keine Datei belegt.</para>
    /// </summary>
    internal static class SqprojGebaeudecodes
    {
        /// <summary>Der Code „keine Angabe“ der Projektdatei.</summary>
        internal const int KEINE_ANGABE = 0;

        /// <summary>Meldungsschlüssel: Code ohne Abbildung; {0} Spalte der Datei, {1} Code.</summary>
        internal const string MELDUNG_OHNE_ABBILDUNG = GebaeudeImportAblauf.MELDUNG + "GEBAEUDECODE_OHNE_ABBILDUNG";

        /// <summary>Meldungsschlüssel: Verwendung aus der Projektdatei; {0} Verwendung, {1} Spalte der Datei, {2} Code.</summary>
        internal const string MELDUNG_VERWENDUNG = GebaeudeImportAblauf.MELDUNG + "VERWENDUNG_PROJEKTDATEI";

        /// <summary>Meldungsschlüssel: Gebäudeart aus der Projektdatei; {0} Gebäudeart, {1} Spalte der Datei, {2} Code.</summary>
        internal const string MELDUNG_GEBAEUDEART = GebaeudeImportAblauf.MELDUNG + "GEBAEUDEART_PROJEKTDATEI";

        /// <summary>Meldungsschlüssel: Energiestandard aus der Projektdatei; {0} Code des Standards, {1} Spalte der Datei, {2} Code.</summary>
        internal const string MELDUNG_ENERGIESTANDARD = GebaeudeImportAblauf.MELDUNG + "ENERGIESTANDARD_PROJEKTDATEI";

        /// <summary>Die Spaltennamen der Datei, wie sie im Protokoll stehen.</summary>
        internal const string SPALTE_TYP = "BuildingType";
        internal const string SPALTE_NUTZUNG = "BuildingUsageType";
        internal const string SPALTE_WAERMESCHUTZ = "BuildingHeatInsulationStandardType";
        internal const string SPALTE_EFFIZIENZ = "BuildingEnergyEfficientType";

        /// <summary>Gebäudetyp → Gebäudeart des Katalogs. Kein Code belegt.</summary>
        private static readonly IReadOnlyDictionary<int, string> GEBAEUDEART = new SortedDictionary<int, string>();

        /// <summary>Gebäudetyp → Verwendung (Steuerwert von <see cref="GebaeudeStammCtrl"/>).</summary>
        private static readonly IReadOnlyDictionary<int, string> VERWENDUNG_AUS_TYP = new SortedDictionary<int, string>
        {
            [3] = GebaeudeStammCtrl.FILTERWERT_SONSTIGE,
        };

        /// <summary>Nutzung → Verwendung. Kein Code außer „keine Angabe“ belegt.</summary>
        private static readonly IReadOnlyDictionary<int, string> VERWENDUNG_AUS_NUTZUNG = new SortedDictionary<int, string>();

        /// <summary>Wärmeschutz- bzw. Effizienzstandard → Code von <see cref="Energiestandard.CODES"/>. Kein Code außer „keine Angabe“ belegt.</summary>
        private static readonly IReadOnlyDictionary<int, string> ENERGIESTANDARD = new SortedDictionary<int, string>();

        /// <summary>Die Tabellen für Papiere und Wachen.</summary>
        internal static IReadOnlyDictionary<int, string> TabelleGebaeudeart => GEBAEUDEART;
        internal static IReadOnlyDictionary<int, string> TabelleVerwendungAusTyp => VERWENDUNG_AUS_TYP;
        internal static IReadOnlyDictionary<int, string> TabelleVerwendungAusNutzung => VERWENDUNG_AUS_NUTZUNG;
        internal static IReadOnlyDictionary<int, string> TabelleEnergiestandard => ENERGIESTANDARD;

        /// <summary>Was die Gebäudecodes einer Datei hergeben: Werte (<c>null</c> = keiner) und die Meldungen dazu.</summary>
        internal sealed class Ergebnis
        {
            internal string Gebaeudeart { get; set; }
            internal string Verwendung { get; set; }
            /// <summary>Die Herleitung der Verwendung (<see cref="MELDUNG_VERWENDUNG"/>); <c>null</c> ohne Verwendung.</summary>
            internal PruefMeldung VerwendungHerleitung { get; set; }
            internal string Energiestandard { get; set; }
            /// <summary>Die Bauweise je Fläche [Wh/(m²K)] aus <c>Cwirk</c>; <c>null</c> = keine.</summary>
            internal double? BauweiseJeM2 { get; set; }
            internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();
        }

        /// <summary>Bildet die Codes eines Gebäudes der Projektdatei ab (die Tabellen dieser Klasse).</summary>
        internal static Ergebnis Abbilden(SqprojGebaeude g)
            => Abbilden(g, GEBAEUDEART, VERWENDUNG_AUS_NUTZUNG, VERWENDUNG_AUS_TYP, ENERGIESTANDARD);

        /// <summary>
        /// Bildet die Codes ab — mit eigenen Tabellen (für Tests). Rangfolge der Verwendung: die Nutzung, dann der Gebäudetyp;
        /// des Energiestandards: der Effizienz-, dann der Wärmeschutzstandard. Ein unbekannter Code meldet sich benannt.
        /// </summary>
        internal static Ergebnis Abbilden(SqprojGebaeude g, IReadOnlyDictionary<int, string> gebaeudeart,
                                          IReadOnlyDictionary<int, string> verwendungAusNutzung,
                                          IReadOnlyDictionary<int, string> verwendungAusTyp,
                                          IReadOnlyDictionary<int, string> energiestandard)
        {
            var e = new Ergebnis();
            if (g == null) return e;
            e.Gebaeudeart = Nachschlagen(gebaeudeart, g.Gebaeudetyp, SPALTE_TYP, e.Meldungen, melden: false);
            if (e.Gebaeudeart != null)
                e.Meldungen.Add(Wert(MELDUNG_GEBAEUDEART, e.Gebaeudeart, SPALTE_TYP, g.Gebaeudetyp));

            string ausNutzung = Nachschlagen(verwendungAusNutzung, g.Nutzungstyp, SPALTE_NUTZUNG, e.Meldungen, melden: true);
            string ausTyp = Nachschlagen(verwendungAusTyp, g.Gebaeudetyp, SPALTE_TYP, e.Meldungen,
                                         melden: e.Gebaeudeart == null);
            if (ausNutzung != null)
                e.Verwendung = Verwendung(e, ausNutzung, SPALTE_NUTZUNG, g.Nutzungstyp);
            else if (ausTyp != null)
                e.Verwendung = Verwendung(e, ausTyp, SPALTE_TYP, g.Gebaeudetyp);

            string ausEffizienz = Nachschlagen(energiestandard, g.Effizienzstandard, SPALTE_EFFIZIENZ, e.Meldungen, melden: true);
            string ausSchutz = Nachschlagen(energiestandard, g.Waermeschutzstandard, SPALTE_WAERMESCHUTZ, e.Meldungen, melden: true);
            if (ausEffizienz != null)
            {
                e.Energiestandard = ausEffizienz;
                e.Meldungen.Add(Wert(MELDUNG_ENERGIESTANDARD, ausEffizienz, SPALTE_EFFIZIENZ, g.Effizienzstandard));
            }
            else if (ausSchutz != null)
            {
                e.Energiestandard = ausSchutz;
                e.Meldungen.Add(Wert(MELDUNG_ENERGIESTANDARD, ausSchutz, SPALTE_WAERMESCHUTZ, g.Waermeschutzstandard));
            }

            e.BauweiseJeM2 = g.CwirkWhM2K > 0.0 ? g.CwirkWhM2K : null;
            return e;
        }

        private static string Verwendung(Ergebnis e, string wert, string spalte, int? code)
        {
            e.VerwendungHerleitung = new PruefMeldung(PruefStufe.Info, MELDUNG_VERWENDUNG, GebaeudeStammCtrl.Verwendungstext(wert), spalte, Code(code));
            return wert;
        }

        private static PruefMeldung Wert(string schluessel, string wert, string spalte, int? code)
            => new PruefMeldung(PruefStufe.Info, schluessel, wert, spalte, Code(code));

        /// <summary>Der Wert eines Codes; ohne Code und für „keine Angabe“ <c>null</c>, ein unbekannter Code meldet sich (wenn <paramref name="melden"/>).</summary>
        private static string Nachschlagen(IReadOnlyDictionary<int, string> tabelle, int? code, string spalte,
                                           List<PruefMeldung> meldungen, bool melden)
        {
            if (code is not int c || c == KEINE_ANGABE) return null;
            if (tabelle != null && tabelle.TryGetValue(c, out string wert)) return wert;
            if (melden && !meldungen.Exists(m => m.Schluessel == MELDUNG_OHNE_ABBILDUNG && m.Werte.Length > 0 && m.Werte[0] == spalte))
                meldungen.Add(new PruefMeldung(PruefStufe.Info, MELDUNG_OHNE_ABBILDUNG, spalte, Code(c)));
            return null;
        }

        private static string Code(int? code) => code?.ToString(CultureInfo.InvariantCulture) ?? "";
    }
}
