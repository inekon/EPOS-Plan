using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Modellansicht (MVD) einer IFC-Datei und der Hinweis zur Exporteinstellung.</b> Der Kopf einer
    /// IFC-Datei nennt die Ansicht, nach der exportiert wurde, in <c>FILE_DESCRIPTION</c>, etwa
    /// <c>ViewDefinition [CoordinationView_V2.0]</c> oder <c>ViewDefinition [ReferenceView_V1.2, QuantityTakeOffAddOnView]</c>.
    ///
    /// <para><b>Kein Urteil über die Ansicht:</b> Viele Exporte lassen <c>FILE_DESCRIPTION</c> leer, auch wenn eine Ansicht
    /// gewählt war; was der Import braucht, steht ohnehin im Inhalt. Deshalb nennt das Protokoll die Ansicht nur als
    /// Info-Zeile und urteilt über das, was die Datei trägt: Fehlen Raumgrenzen der 2. Ebene UND Basismengen, steht genau
    /// ein Hinweis zur Exporteinstellung im Protokoll — bei IFC2X3 samt Stoffwerten und Rückgabe der angereicherten
    /// Datei.</para>
    /// </summary>
    internal static class IfcModellansicht
    {
        private const string P = IfcImportProfil.MELDUNGSPRAEFIX;

        /// <summary>Das Kennwort vor der Klammer der Ansichten in <c>FILE_DESCRIPTION</c>.</summary>
        internal const string KENNWORT = "ViewDefinition";

        /// <summary>
        /// <b>Liest die Ansichtsnamen</b> aus den Einträgen von <c>FILE_DESCRIPTION</c>: je Eintrag
        /// <c>ViewDefinition [A, B]</c> — Groß- und Kleinschreibung des Kennworts gleich, Leerzeichen beliebig, mehrere
        /// Ansichten durch Komma oder Semikolon getrennt, fehlt die schließende Klammer, gilt der Rest des Eintrags.
        /// Doppelte Namen zählen einmal (ohne Rücksicht auf die Schreibung, die erste gilt). Leere, fremde und kaputte
        /// Einträge ergeben nichts; <c>null</c> ergibt eine leere Liste.
        /// </summary>
        public static IReadOnlyList<string> AusBeschreibung(IEnumerable<string> eintraege)
        {
            var ansichten = new List<string>();
            if (eintraege == null) return ansichten;
            var gesehen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string eintrag in eintraege)
            {
                if (string.IsNullOrWhiteSpace(eintrag)) continue;
                int stelle = 0;
                while (true)
                {
                    int k = eintrag.IndexOf(KENNWORT, stelle, StringComparison.OrdinalIgnoreCase);
                    if (k < 0) break;
                    int i = k + KENNWORT.Length;
                    while (i < eintrag.Length && char.IsWhiteSpace(eintrag[i])) i++;
                    if (i >= eintrag.Length || eintrag[i] != '[') { stelle = i; continue; }
                    int zu = eintrag.IndexOf(']', i + 1);
                    string inhalt = zu < 0 ? eintrag.Substring(i + 1) : eintrag.Substring(i + 1, zu - i - 1);
                    foreach (string teil in inhalt.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string name = Bereinigt(teil);
                        if (name.Length > 0 && gesehen.Add(name)) ansichten.Add(name);
                    }
                    if (zu < 0) break;
                    stelle = zu + 1;
                }
            }
            return ansichten;
        }

        /// <summary>Ein Ansichtsname ohne Rand, Anführungszeichen und Steuerzeichen; Leerzeichen im Namen bleiben einfach.</summary>
        private static string Bereinigt(string teil)
        {
            var zeichen = teil.Where(c => !char.IsControl(c) && c != '\'' && c != '"' && c != '[' && c != ']').ToArray();
            string name = new string(zeichen).Trim();
            return string.Join(" ", name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Die Ansichten als Anzeigetext („A, B“); leer = <c>null</c>.</summary>
        public static string Anzeige(IReadOnlyList<string> ansichten)
            => ansichten == null || ansichten.Count == 0 ? null : string.Join(", ", ansichten);

        /// <summary>Ist das ein Mengensatz, den der Import liest (<c>BaseQuantities</c>, <c>Qto_…BaseQuantities</c>, <c>Qto_…Quantities</c>)?</summary>
        internal static bool IstBasismengensatz(string satzname)
        {
            if (string.IsNullOrWhiteSpace(satzname)) return false;
            string n = satzname.Trim();
            if (string.Equals(n, "BaseQuantities", StringComparison.OrdinalIgnoreCase)) return true;
            return n.StartsWith("Qto_", StringComparison.OrdinalIgnoreCase)
                   && n.EndsWith("Quantities", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <b>Die Protokollzeilen</b> eines gelesenen Modells: vorn die Info-Zeile mit Schema und Modellansicht
        /// (<c>DATEI_MVD</c> bzw. <c>DATEI_OHNE_MVD</c>), und — fehlen Raumgrenzen der 2. Ebene und Basismengen — genau ein
        /// Hinweis zur Exporteinstellung (<c>EXPORT_OHNE_GRENZEN_MENGEN</c>, bei IFC2X3 <c>…_IFC2X3</c>, oder
        /// <c>…_IFC2X3_RUECKGABE</c>, wenn <c>STOFFWERTE_NICHT_GELESEN</c> die Stoffwerte schon nennt).
        /// </summary>
        public static void Melden(IModel modell, IfcGebaeudeAbbild abbild)
        {
            if (modell == null || abbild == null) return;
            IList<string> beschreibung;
            try { beschreibung = modell.Header?.FileDescription?.Description; }
            catch (Exception) { beschreibung = null; }
            abbild.Modellansichten = AusBeschreibung(beschreibung);

            string schema = string.IsNullOrWhiteSpace(abbild.Schemastand)
                ? IfcSchemaStaende.Kurzform(abbild.SchemaStand) ?? ""
                : abbild.Schemastand;
            string mvd = Anzeige(abbild.Modellansichten);
            abbild.Meldungen.Insert(0, mvd == null
                ? new PruefMeldung(PruefStufe.Info, P + "DATEI_OHNE_MVD", schema)
                : new PruefMeldung(PruefStufe.Info, P + "DATEI_MVD", schema, mvd));

            bool grenzen = abbild.ZahlRaumgrenzenZweiteEbene > 0
                           || modell.Instances.OfType<IIfcRelSpaceBoundary>()
                                    .Any(r => IfcAbbildBauer.IstZweiteEbene(r, abbild.SchemaStand, out _));
            bool mengen = modell.Instances.OfType<IIfcElementQuantity>()
                                .Any(q => IstBasismengensatz(q.Name.HasValue ? q.Name.Value.ToString() : null));
            if (grenzen || mengen) return;

            string schluessel = P + "EXPORT_OHNE_GRENZEN_MENGEN";
            if (abbild.SchemaStand == IfcSchemaStand.Ifc2x3)
                schluessel += abbild.Meldungen.Any(m => m.Schluessel == P + "STOFFWERTE_NICHT_GELESEN")
                    ? "_IFC2X3_RUECKGABE" : "_IFC2X3";
            abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, schluessel));
        }
    }
}
