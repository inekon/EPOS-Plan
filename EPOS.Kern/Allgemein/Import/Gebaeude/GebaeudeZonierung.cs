using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Was an der Seite einer Zonenfläche liegt (Mehrzonenkonzept 6.2, Tabelle „Randbedingung").</summary>
    internal enum Zonenrand
    {
        /// <summary>Außenluft.</summary>
        Aussenluft = 0,
        /// <summary>Erdreich.</summary>
        Erdreich = 1,
        /// <summary>Eine andere Zone desselben Gebäudes — Trennfläche mit Nachbarzone.</summary>
        Zone = 2,
        /// <summary>Innerhalb der Zone: innere Masse (beide Seiten in derselben Zone).</summary>
        Innen = 3,
        /// <summary>Ein unbeheizter Raum außerhalb der Zonen, ein Raum ohne Gegenstück oder einer, den es nicht gibt.</summary>
        Unbeheizt = 4,
        /// <summary>Ein beheizter Raum eines anderen Gebäudes der Datei — innere Masse, nur die eigene Seite (E45).</summary>
        Gebaeudetrennung = 5,
    }

    /// <summary>Eine Zone des Vorschlags: Räume einer Regelgruppe mit gleichem Beheizungszustand.</summary>
    internal sealed class Importzone
    {
        /// <summary>Der Schlüssel der Gruppe (Regel, Kennung der Gruppe, Beheizung).</summary>
        internal string Schluessel { get; set; } = "";

        /// <summary>Der Anzeigename.</summary>
        internal string Name { get; set; } = "";

        /// <summary>Die Kennung der Datei, die die Zone trägt (Zone, Geschoss, Raum oder Gebäude); <c>null</c> = keine.</summary>
        internal string Quellkennung { get; set; }

        /// <summary>Beheizt (alle Räume der Zone sind es) oder frei schwingend.</summary>
        internal bool IstBeheizt { get; set; }

        /// <summary>Die Räume der Zone in Dateireihenfolge.</summary>
        internal List<AbbildRaum> Raeume { get; } = new List<AbbildRaum>();

        /// <summary>Die Namen der Zonen, die der Mindestgröße wegen hierher zugeschlagen wurden (M8).</summary>
        internal List<string> Zugeschlagen { get; } = new List<string>();

        /// <summary>Summe der Raumflächen [m²]; <c>null</c> = kein Raum trägt eine.</summary>
        internal double? FlaecheM2 => Raeume.Any(r => r.FlaecheM2 > 0.0) ? Raeume.Where(r => r.FlaecheM2 > 0.0).Sum(r => r.FlaecheM2.Value) : (double?)null;

        /// <summary>
        /// Das Volumen [m³]: Summe der Raumvolumen, wenn jeder Raum eines trägt, sonst Fläche × Höhe, wenn
        /// jeder Raum beides trägt; <c>null</c> sonst.
        /// </summary>
        internal double? VolumenM3
        {
            get
            {
                if (Raeume.Count == 0) return null;
                if (Raeume.All(r => r.VolumenM3 > 0.0)) return Raeume.Sum(r => r.VolumenM3.Value);
                if (Raeume.All(r => r.FlaecheM2 > 0.0 && r.HoeheM > 0.0)) return Raeume.Sum(r => r.FlaecheM2.Value * r.HoeheM.Value);
                return null;
            }
        }

        /// <summary>Die Raumhöhe [m] = Volumen ÷ Fläche; <c>null</c>, wenn eines fehlt.</summary>
        internal double? HoeheM => VolumenM3 is double v && FlaecheM2 is double a && a > 0.0 ? v / a : (double?)null;

        /// <summary>Kleiner als die Mindestgröße und ohne Nachbarn gleicher Beheizung (M8) — steht mit Hinweis.</summary>
        internal bool ZuKlein { get; set; }

        /// <summary>Keine Fläche gegen Außenluft oder Erdreich (Fehlerbild „Zone ohne Hülle").</summary>
        internal bool OhneAussen { get; set; }

        public override string ToString() => Name + " (" + Raeume.Count.ToString(CultureInfo.InvariantCulture) + " Räume)";
    }

    /// <summary>
    /// <b>Eine Fläche aus Sicht einer Zone</b> — der Teil eines Bauteils, den die Räume der Zone sehen,
    /// mit Randbedingung, Nachbarzone, Bruttofläche und den Öffnungen, die auf ihm liegen.
    /// </summary>
    internal sealed class Zonenflaeche
    {
        /// <summary>Das Bauteil des Abbilds.</summary>
        internal AbbildBauteil Bauteil { get; set; }

        /// <summary>Die Zone (Stelle in <see cref="GebaeudeZonierung.Zonen"/>).</summary>
        internal int Zone { get; set; }

        /// <summary>Die Nachbarzone einer Trennfläche; −1 = keine.</summary>
        internal int Nachbarzone { get; set; } = -1;

        /// <summary>Was an der anderen Seite liegt.</summary>
        internal Zonenrand Rand { get; set; }

        /// <summary>Bruttofläche dieses Teils [m²] (Außenrand bzw. Anteil); <c>null</c> = keine Geometrie.</summary>
        internal double? BruttoM2 { get; set; }

        /// <summary>Die Bruttofläche, mit der die Nachbarzone dieselbe Trennfläche beschreibt [m²]; <c>null</c> = keine.</summary>
        internal double? GegenseiteM2 { get; set; }

        /// <summary>Die in der Geometrie schon ausgeschnittenen Öffnungen [m²] (Innenränder der Ebene, 6.2).</summary>
        internal double AusschnittM2 { get; set; }

        /// <summary>Wurde die Fläche des Bauteils ohne Geometrie nach der Zahl der Grenzen aufgeteilt?</summary>
        internal bool Aufgeteilt { get; set; }

        /// <summary>Innere Masse mit beiden Seiten in der Zone (zählt zweifach in A_IW).</summary>
        internal bool Beidseitig { get; set; }

        /// <summary>Eine innere Grenze ohne Gegenstück (Rekonstruktion ohne Ergebnis, 6.2 Schritt 4).</summary>
        internal bool OhneGegenstueck { get; set; }

        /// <summary>Ein Raum dieser Seite (für die Sicht auf Boden oder Decke); <c>null</c> = keiner.</summary>
        internal string Raum { get; set; }

        /// <summary>Ein Raum der anderen Seite; <c>null</c> = keiner.</summary>
        internal string AndererRaum { get; set; }

        /// <summary>Die Öffnungen (Fenster, Türen), die auf diesem Teil liegen.</summary>
        internal List<AbbildBauteil> Oeffnungen { get; } = new List<AbbildBauteil>();

        /// <summary>Die größere Beschreibung einer Trennfläche (Gegenprobe), sonst die eigene [m²].</summary>
        internal double? GroessereM2 => BruttoM2 is double a ? (GegenseiteM2 is double b ? Math.Max(a, b) : a) : GegenseiteM2;

        public override string ToString() => (Bauteil?.Kennung ?? "") + " Z" + Zone.ToString(CultureInfo.InvariantCulture) + " " + Rand
                                             + (Nachbarzone >= 0 ? "→Z" + Nachbarzone.ToString(CultureInfo.InvariantCulture) : "");
    }

    /// <summary>Die Trennflächen zweier Zonen, von beiden Seiten gemessen (Gegenprobe ab 2 %).</summary>
    /// <param name="ZoneA">Die Zone mit der kleineren Stelle.</param>
    /// <param name="ZoneB">Die andere Zone.</param>
    /// <param name="FlaecheA">Σ Bruttofläche aus Sicht von A [m²].</param>
    /// <param name="FlaecheB">Σ Bruttofläche aus Sicht von B [m²].</param>
    internal sealed record Zonentrennung(int ZoneA, int ZoneB, double FlaecheA, double FlaecheB)
    {
        /// <summary>Weichen beide Seiten um mehr als <see cref="GebaeudeZonierung.GEGENPROBE_GRENZE"/> ab?</summary>
        internal bool Ungleich => Math.Max(FlaecheA, FlaecheB) > 0.0
                                  && Math.Abs(FlaecheA - FlaecheB) > GebaeudeZonierung.GEGENPROBE_GRENZE * Math.Max(FlaecheA, FlaecheB);
    }

    /// <summary>Eine virtuelle Grenze zwischen zwei Zonen — Vorschlag eines Zonen-Luftaustauschs (Mehrzonenkonzept 2.7).</summary>
    internal sealed record Luftverbindung(int ZoneA, int ZoneB, double FlaecheM2);

    /// <summary>
    /// <b>Die Zonierung eines importierten Gebäudes</b> (Stufe G6c; Mehrzonenkonzept 6.1, 6.2, 6.5, 6.6;
    /// Datenaustauschkonzept 3.3; Entscheid E50 mit M7, M8, M12, M13) — formatfrei auf dem
    /// <see cref="GebaeudeAbbild"/>, ohne Datenbank und ohne Oberfläche. Sie bildet einen
    /// <b>Vorschlag</b> und legt die Regel offen; der Dialog kann umschalten.
    ///
    /// <list type="bullet">
    /// <item><b>Regeln</b> in der Rangfolge der Konzepte — IFC: Z1 nach den Zonen der Datei, Z2 nach der
    /// Klassifikation, Z3 nach der Nutzung, Z4 je Geschoss, Z5 eine Zone; gbXML: X1 nach <c>Zone</c> (nur
    /// bei weniger Zonen als Räumen), X2 je Geschoss, X3 je Raum, X4 eine Zone. <b>Vorgabe</b> (M7): IFC Z4,
    /// wenn mehr als ein Geschoss Räume trägt und die Datei Raumgrenzen führt, sonst Z5; gbXML X1, sonst
    /// X2, sonst X4. Ohne Raumgrenzen ist Z4 nur auf ausdrückliche Wahl zu haben, mit Warnung — die
    /// Geschosszonen wären entkoppelt (6.5); Z1 bis Z3 brauchen die Grenzen.</item>
    /// <item><b>Beheizung:</b> Eine Zone fasst nur Räume gleichen Zustands (Regeln B1…B6 des Lesers bzw.
    /// <c>@conditionType</c>, übersteuert durch die Haken der Raumliste); die unbeheizten Räume einer Gruppe
    /// bilden eine eigene, frei schwingende Zone. Unter Z5/X4 gibt es nur die eine beheizte Zone; die
    /// unbeheizten Räume bleiben draußen (Einzonenweg, G4b).</item>
    /// <item><b>Seiten:</b> je Bauteil die Grenzen der Räume, je Zone zusammengefasst. Außen liegt, was
    /// die Grenze (bzw. das Bauteil) <c>EXTERNAL</c>/<c>EXTERNAL_EARTH</c> nennt. Eine innere Grenze
    /// findet ihr Gegenüber (M13, 6.2): (0) über das Gegenstück der Datei; (1) liegen die übrigen Grenzen
    /// des Bauteils in genau einer anderen Zone, ist sie eindeutig; (2) sonst über die Geometrie in
    /// Weltkoordinaten — Flächeninhalt innerhalb <see cref="PAAR_FLAECHE_TOLERANZ"/>, Schwerpunktabstand
    /// höchstens Bauteildicke + <see cref="PAAR_ABSTAND_ZUSCHLAG_M"/> (Grenzen liegen auf den
    /// Bauteiloberflächen), Normalen entgegengesetzt; (3) sonst „ohne Gegenstück" — Randbedingung
    /// unbeheizt, benannt.</item>
    /// <item><b>Flächen:</b> die Polygonfläche der Grenzen; ohne sie die Fläche des Bauteils, bei mehreren
    /// Zonen auf derselben Seite nach der Zahl der Grenzen geteilt (benannt). Öffnungen gehen an die Zone
    /// ihrer eigenen Grenze, sonst an den größten Teil ihres Wirts (benannt).</item>
    /// <item><b>Gegenprobe</b> (6.6): je Zonenpaar Σ A→B gegen Σ B→A; über 2 % gemeldet, gerechnet wird je
    /// Bauteil mit der größeren Beschreibung.</item>
    /// <item><b>Mindestgröße</b> (M8): Eine Zone unter max(<see cref="MINDESTFLAECHE_M2"/>,
    /// <see cref="MINDESTANTEIL"/> × Gebäudefläche) geht an die Nachbarzone gleicher Beheizung mit der
    /// größten gemeinsamen Trennfläche; ohne solche bleibt sie, benannt. Gebäudefläche ist die Summe der
    /// Raumflächen des Gebäudes (Festlegung; damit können höchstens 50 Zonen die Mindestgröße erfüllen).</item>
    /// <item><b>Obergrenze</b> (M12): über <see cref="GebaeudeZonenregeln.PFLEGEGRENZE"/> Zonen eine Warnung
    /// und der Vorschlag der gröberen Regel (auf Geschosse zusammenlegen); entschieden wird im Dialog, der
    /// Bauteilvorschlag lehnt so viele Zonen benannt ab.</item>
    /// </list>
    /// </summary>
    internal sealed class GebaeudeZonierung
    {
        // ==================================================================
        //  Meldungen — Namen ohne Präfix; der Schlüssel trägt den des Formats (IMP_IFC_PROT_, IMP_GBXML_PROT_)
        // ==================================================================

        /// <summary>I — {0} Regel, {1} Zahl der Zonen, {2} Vorgabe: Die Zonierung nach der Regel.</summary>
        internal const string ZONENREGEL = "ZONENREGEL";
        /// <summary>F — {0} Regel, {1} wählbare Regeln: Die Regel ist für diese Datei nicht zu haben.</summary>
        internal const string ZONENREGEL_UNGUELTIG = "ZONENREGEL_UNGUELTIG";
        /// <summary>W (IFC) — {0} Gebäude: Die Datei führt keine Raumgrenzen; Vorgabe Z5.</summary>
        internal const string KEINE_GRENZEN = "KEINE_GRENZEN";
        /// <summary>W (IFC) — {0} Gebäude, {1} Zahl: Nur Raumgrenzen der 1. Ebene; Vorgabe Z5.</summary>
        internal const string NUR_1STLEVEL = "NUR_1STLEVEL";
        /// <summary>W — {0} Regel: Mehrere Zonen ohne Raumgrenzen sind entkoppelt; Trenndecken selbst eintragen.</summary>
        internal const string GRENZEN_ENTKOPPELT = "GRENZEN_ENTKOPPELT";
        /// <summary>W — {0} Zahl, {1} Fläche [m²], {2} Beispiele: Innere Grenzen ohne Gegenstück → unbeheizt.</summary>
        internal const string OHNE_GEGENSTUECK = "OHNE_GEGENSTUECK";
        /// <summary>W — {0} Zone A, {1} Zone B, {2} A→B [m²], {3} B→A [m²]: Gegenprobe über 2 %.</summary>
        internal const string TRENNFLAECHE_UNGLEICH = "TRENNFLAECHE_UNGLEICH";
        /// <summary>W — {0} Zahl, {1} Beispiele: Bauteilflächen ohne Geometrie nach der Zahl der Grenzen geteilt.</summary>
        internal const string FLAECHE_AUFGETEILT = "FLAECHE_AUFGETEILT";
        /// <summary>I — {0} Zahl, {1} Beispiele: Öffnungen ohne eigene Grenze dem größten Teil ihres Wirts zugeordnet.</summary>
        internal const string OEFFNUNG_ZONE = "OEFFNUNG_ZONE";
        /// <summary>I — {0} Zone, {1} Fläche, {2} Zielzone, {3} gemeinsame Fläche: Mindestgröße — zugeschlagen.</summary>
        internal const string ZONE_ZUGESCHLAGEN = "ZONE_ZUGESCHLAGEN";
        /// <summary>I — {0} Zone, {1} Fläche, {2} Mindestgröße: zu klein, ohne Nachbarn gleicher Beheizung.</summary>
        internal const string ZONE_ZU_KLEIN = "ZONE_ZU_KLEIN";
        /// <summary>I — {0} Zone: keine Fläche gegen Außenluft oder Erdreich.</summary>
        internal const string ZONE_OHNE_AUSSEN = "ZONE_OHNE_AUSSEN";
        /// <summary>W — {0} Zahl der Zonen, {1} Grenze, {2} vorgeschlagene Regel, {3} deren Zonenzahl: zu viele Zonen.</summary>
        internal const string ZU_VIELE_ZONEN_VORSCHLAG = "ZU_VIELE_ZONEN_VORSCHLAG";
        /// <summary>I — {0} Zone A, {1} Zone B, {2} Fläche: virtuelle Grenze — Vorschlag eines Luftaustauschs.</summary>
        internal const string LUFTVERBINDUNG = "LUFTVERBINDUNG";

        // ==================================================================
        //  Festwerte
        // ==================================================================

        /// <summary>Kleinste Zonenfläche [m²] (M8).</summary>
        internal const double MINDESTFLAECHE_M2 = 2.0;

        /// <summary>Kleinster Anteil einer Zone an der Gebäudefläche (M8).</summary>
        internal const double MINDESTANTEIL = 0.02;

        /// <summary>Relative Abweichung der Trennfläche A→B gegen B→A, ab der gemeldet wird (6.6).</summary>
        internal const double GEGENPROBE_GRENZE = 0.02;

        /// <summary>Relative Flächenabweichung zweier Grenzen, bis zu der sie ein Paar sein können (6.2).</summary>
        internal const double PAAR_FLAECHE_TOLERANZ = 0.01;

        /// <summary>Zuschlag [m] auf die Bauteildicke beim Schwerpunktabstand eines Paars.</summary>
        internal const double PAAR_ABSTAND_ZUSCHLAG_M = 0.01;

        /// <summary>Die Dicke [m], wenn das Bauteil keine trägt — die größte Wand, die der Leser als eine liest.</summary>
        internal const double DICKE_ERSATZ_M = 0.6;

        private const int FREMD_BEHEIZT = -2, FREMD_UNBEHEIZT = -3, UNBEKANNT = -4;

        // ==================================================================
        //  Inhalt
        // ==================================================================

        /// <summary>Das Format des Abbilds (<see cref="GebaeudeQuelle.FORMAT_IFC"/>, <see cref="GebaeudeQuelle.FORMAT_GBXML"/>).</summary>
        internal string Format { get; private set; } = "";

        /// <summary>Die Regel, nach der gebildet ist.</summary>
        internal string Regel { get; private set; } = "";

        /// <summary>Die Vorgabe der Regel für diese Datei (M7).</summary>
        internal string Vorgabe { get; private set; } = "";

        /// <summary>Die wählbaren Regeln in Rangfolge.</summary>
        internal IReadOnlyList<string> Regeln { get; private set; } = Array.Empty<string>();

        /// <summary>Eine Zone je Gebäude (Z5, X4) — der Einzonenweg aus G4b.</summary>
        internal bool Einzonig { get; private set; }

        /// <summary>Führt die Datei Raumgrenzen für dieses Gebäude (gbXML: immer)?</summary>
        internal bool HatRaumgrenzen { get; private set; }

        /// <summary>Die Mindestgröße einer Zone [m²] (M8).</summary>
        internal double MindestflaecheM2 { get; private set; }

        /// <summary>Das Gebäude des Abbilds.</summary>
        internal AbbildGebaeude Gebaeude { get; private set; }

        /// <summary>Die Zonen in Rangfolge.</summary>
        internal List<Importzone> Zonen { get; } = new List<Importzone>();

        /// <summary>Die Flächen je Zone (leer unter Z5/X4).</summary>
        internal List<Zonenflaeche> Flaechen { get; } = new List<Zonenflaeche>();

        /// <summary>Die Trennflächen je Zonenpaar.</summary>
        internal List<Zonentrennung> Trennungen { get; } = new List<Zonentrennung>();

        /// <summary>Die virtuellen Grenzen zwischen Zonen.</summary>
        internal List<Luftverbindung> Luftverbindungen { get; } = new List<Luftverbindung>();

        /// <summary>Die Meldungen in Reihenfolge.</summary>
        internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Mehr Zonen als <see cref="GebaeudeZonenregeln.PFLEGEGRENZE"/> (M12)?</summary>
        internal bool ZuVieleZonen => Zonen.Count > GebaeudeZonenregeln.PFLEGEGRENZE;

        /// <summary>Die gröbere Regel, die bei zu vielen Zonen vorgeschlagen wird; <c>null</c> = keine nötig.</summary>
        internal string Vorschlagsregel { get; private set; }

        /// <summary>Hat die Zonierung einen Fehler (Regel nicht wählbar)?</summary>
        internal bool Abgelehnt => Meldungen.Any(m => m.Stufe == PruefStufe.Fehler);

        /// <summary>Die Zone eines Raums dieses Gebäudes (Stelle); −1 = keine.</summary>
        internal int ZoneVon(string raumKennung)
            => raumKennung != null && _zoneJeRaum.TryGetValue(raumKennung, out int z) && z >= 0 ? z : -1;

        private readonly Dictionary<string, int> _zoneJeRaum = new Dictionary<string, int>(StringComparer.Ordinal);
        private string _praefix = "";
        private GebaeudeAbbild _abbild;
        private int _index;
        private Func<AbbildRaum, bool> _beheizt;
        private Dictionary<string, AbbildRaum> _alleRaeume;
        private Dictionary<string, int> _raumGebaeude;

        // ==================================================================
        //  Regeln
        // ==================================================================

        /// <summary>Die wählbaren Regeln eines Gebäudes und die Vorgabe (M7, Datenaustauschkonzept 3.3).</summary>
        internal static (IReadOnlyList<string> Regeln, string Vorgabe, bool Grenzen) Waehlbar(GebaeudeAbbild abbild, int index)
        {
            AbbildGebaeude g = abbild.Gebaeude[index];
            bool ifc = string.Equals(abbild.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal);
            int geschosse = g.Raeume.Select(r => r.GeschossKennung).Where(s => s != null).Distinct(StringComparer.Ordinal).Count();
            var regeln = new List<string>();
            if (ifc)
            {
                bool grenzen = g.ZahlGrenzen > 0;
                if (grenzen && g.Raeume.Any(r => r.ZonenKennung != null)) regeln.Add(IfcImportProfil.ZONENREGEL_Z1);
                if (grenzen && g.Raeume.Count > 0 && g.Raeume.All(r => r.Klassifikation != null)
                    && g.Raeume.Select(r => Quelle(r.Klassifikation)).Distinct(StringComparer.Ordinal).Count() == 1
                    && g.Raeume.Select(r => r.Klassifikation).Distinct(StringComparer.Ordinal).Count() > 1)
                    regeln.Add(IfcImportProfil.ZONENREGEL_Z2);
                if (grenzen && g.Raeume.Count > 1 && g.Raeume.All(r => !string.IsNullOrWhiteSpace(r.Name))
                    && g.Raeume.Select(r => Nutzung(r.Name)).Distinct(StringComparer.Ordinal).Count() < g.Raeume.Count)
                    regeln.Add(IfcImportProfil.ZONENREGEL_Z3);
                if (geschosse > 1) regeln.Add(IfcImportProfil.ZONENREGEL_Z4);
                regeln.Add(IfcImportProfil.ZONENREGEL_Z5);
                string vorgabe = grenzen && geschosse > 1 ? IfcImportProfil.ZONENREGEL_Z4 : IfcImportProfil.ZONENREGEL_Z5;
                return (regeln, vorgabe, grenzen);
            }
            int zonen = g.Raeume.Select(r => r.ZonenKennung).Where(z => z != null).Distinct(StringComparer.Ordinal).Count();
            bool x1 = zonen >= 1 && zonen < g.Raeume.Count;
            if (x1) regeln.Add(GebaeudeImportProfil.ZONENREGEL_X1);
            if (geschosse > 1) regeln.Add(GebaeudeImportProfil.ZONENREGEL_X2);
            if (g.Raeume.Count > 1) regeln.Add(GebaeudeImportProfil.ZONENREGEL_X3);
            regeln.Add(GebaeudeImportProfil.ZONENREGEL_X4);
            string x = x1 ? GebaeudeImportProfil.ZONENREGEL_X1 : geschosse > 1 ? GebaeudeImportProfil.ZONENREGEL_X2 : GebaeudeImportProfil.ZONENREGEL_X4;
            return (regeln, x, true);
        }

        private static string Quelle(string klassifikation)
        {
            int i = klassifikation?.IndexOf('|') ?? -1;
            return i < 0 ? "" : klassifikation.Substring(0, i);
        }

        /// <summary>Die Nutzung eines Raumnamens (Z3): klein, ohne angehängte Nummer („Büro 2" → „büro").</summary>
        internal static string Nutzung(string name)
        {
            string n = (name ?? "").Trim().ToLowerInvariant();
            int ende = n.Length;
            while (ende > 0 && (char.IsDigit(n[ende - 1]) || n[ende - 1] == ' ' || n[ende - 1] == '.' || n[ende - 1] == '-' || n[ende - 1] == '_'))
                ende--;
            return ende > 0 ? n.Substring(0, ende) : n;
        }

        private static bool IstEinzonig(string regel)
            => regel == IfcImportProfil.ZONENREGEL_Z5 || regel == GebaeudeImportProfil.ZONENREGEL_X4;

        // ==================================================================
        //  Bilden
        // ==================================================================

        /// <summary>
        /// <b>Bildet die Zonierung</b> eines Gebäudes der Datei (Klassenkopf). Schreibt nichts, wirft nicht.
        /// </summary>
        /// <param name="abbild">Das gelesene Abbild.</param>
        /// <param name="index">Das Gebäude (U13: eines je Lauf).</param>
        /// <param name="regel">Die gewählte Regel; <c>null</c> = die Vorgabe.</param>
        /// <param name="beheiztUebersteuert">Die Haken der Raumliste, Raumkennung → beheizt; <c>null</c> = wie gelesen.</param>
        internal static GebaeudeZonierung Bilden(GebaeudeAbbild abbild, int index, string regel = null,
                                                 IReadOnlyDictionary<string, bool> beheiztUebersteuert = null)
        {
            var z = new GebaeudeZonierung();
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return z;
            z._abbild = abbild;
            z._index = index;
            z.Gebaeude = abbild.Gebaeude[index];
            z.Format = abbild.Format ?? "";
            z._praefix = string.Equals(z.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)
                ? IfcImportProfil.MELDUNGSPRAEFIX : GbxmlImportProfil.MELDUNGSPRAEFIX;
            z._beheizt = r => GebaeudeRaumzeile.BeheiztWirksam(r, beheiztUebersteuert);
            z._alleRaeume = new Dictionary<string, AbbildRaum>(StringComparer.Ordinal);
            z._raumGebaeude = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < abbild.Gebaeude.Count; i++)
                foreach (AbbildRaum r in abbild.Gebaeude[i].Raeume)
                    if (!z._alleRaeume.ContainsKey(r.Kennung)) { z._alleRaeume[r.Kennung] = r; z._raumGebaeude[r.Kennung] = i; }

            (IReadOnlyList<string> regeln, string vorgabe, bool grenzen) = Waehlbar(abbild, index);
            z.Regeln = regeln;
            z.Vorgabe = vorgabe;
            z.HatRaumgrenzen = grenzen;
            z.Regel = regel ?? vorgabe;
            if (!grenzen)
                z.Melden(PruefStufe.Warnung, KEINE_GRENZEN, z.Gebaeude.Anzeigename);
            else if (string.Equals(z.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal) && z.Gebaeude.ZahlGrenzenZweiteEbene == 0)
                z.Melden(PruefStufe.Warnung, NUR_1STLEVEL, z.Gebaeude.Anzeigename, Ganz(z.Gebaeude.ZahlGrenzen));
            if (!regeln.Contains(z.Regel))
            {
                z.Melden(PruefStufe.Fehler, ZONENREGEL_UNGUELTIG, z.Regel, string.Join(", ", regeln));
                return z;
            }
            z.Einzonig = IstEinzonig(z.Regel);

            double gesamt = z.Gebaeude.Raeume.Where(r => r.FlaecheM2 > 0.0).Sum(r => r.FlaecheM2.Value);
            z.MindestflaecheM2 = Math.Max(MINDESTFLAECHE_M2, MINDESTANTEIL * gesamt);

            z.Gruppieren();
            if (z.Einzonig)
            {
                z.Melden(PruefStufe.Info, ZONENREGEL, z.Regel, Ganz(z.Zonen.Count), z.Vorgabe);
                return z;
            }
            if (!grenzen)
                z.Melden(PruefStufe.Warnung, GRENZEN_ENTKOPPELT, z.Regel);

            z.Seiten(melden: false);
            z.Mindestgroesse();
            z.Seiten(melden: true);
            z.Abschluss();
            return z;
        }

        // ------------------------------------------------------------------
        //  Gruppen
        // ------------------------------------------------------------------

        /// <summary>Die Räume in Zonen je Regel und Beheizung, in Dateireihenfolge.</summary>
        private void Gruppieren()
        {
            var jeSchluessel = new Dictionary<string, Importzone>(StringComparer.Ordinal);
            foreach (AbbildRaum r in Gebaeude.Raeume)
            {
                bool warm = _beheizt(r);
                if (Einzonig && !warm) continue;   // Einzonenweg: unbeheizte Räume bleiben draußen
                (string kennung, string name) = Gruppe(r);
                string schluessel = Regel + "|" + kennung + "|" + (warm ? "B" : "U");
                if (!jeSchluessel.TryGetValue(schluessel, out Importzone zone))
                {
                    zone = new Importzone { Schluessel = schluessel, Name = name, IstBeheizt = warm, Quellkennung = QuelleDerGruppe(r, kennung) };
                    jeSchluessel[schluessel] = zone;
                    Zonen.Add(zone);
                }
                zone.Raeume.Add(r);
            }
            // Eine Gruppe mit beheizten und unbeheizten Räumen: die unbeheizte Zone trägt den Zusatz.
            foreach (Importzone u in Zonen.Where(x => !x.IstBeheizt).ToList())
            {
                string partner = u.Schluessel.Substring(0, u.Schluessel.Length - 1) + "B";
                if (jeSchluessel.ContainsKey(partner)) u.Name += " (unbeheizt)";
            }
            ZuordnungNeu();
        }

        private (string Kennung, string Name) Gruppe(AbbildRaum r)
        {
            switch (Regel)
            {
                case IfcImportProfil.ZONENREGEL_Z1:
                case GebaeudeImportProfil.ZONENREGEL_X1:
                    return r.ZonenKennung != null
                        ? (r.ZonenKennung, string.IsNullOrWhiteSpace(r.ZonenName) ? r.ZonenKennung : r.ZonenName.Trim())
                        : ("", "ohne Zone");
                case IfcImportProfil.ZONENREGEL_Z2:
                {
                    string k = r.Klassifikation ?? "";
                    int i = k.IndexOf('|');
                    return (k, i >= 0 ? k.Substring(i + 1) : k);
                }
                case IfcImportProfil.ZONENREGEL_Z3:
                    return (Nutzung(r.Name), (r.Name ?? "").Trim());
                case IfcImportProfil.ZONENREGEL_Z4:
                case GebaeudeImportProfil.ZONENREGEL_X2:
                {
                    string k = r.GeschossKennung ?? "";
                    string n = Gebaeude.Geschosse.FirstOrDefault(s => s.Kennung == k)?.Anzeigename
                               ?? (string.IsNullOrWhiteSpace(r.GeschossName) ? (k.Length > 0 ? k : "ohne Geschoss") : r.GeschossName.Trim());
                    return (k, n);
                }
                case GebaeudeImportProfil.ZONENREGEL_X3:
                    return (r.Kennung, string.IsNullOrWhiteSpace(r.Name) ? r.Kennung : r.Name.Trim());
                default:
                    return ("", string.IsNullOrWhiteSpace(Gebaeude.Anzeigename) ? GebaeudeZonenuebernahme.ZONE_BEZEICHNUNG : Gebaeude.Anzeigename.Trim());
            }
        }

        /// <summary>Die Kennung der Datei, die eine Zone trägt: Zone, Geschoss, Raum oder Gebäude.</summary>
        private string QuelleDerGruppe(AbbildRaum r, string kennung)
        {
            switch (Regel)
            {
                case IfcImportProfil.ZONENREGEL_Z1:
                case GebaeudeImportProfil.ZONENREGEL_X1:
                case IfcImportProfil.ZONENREGEL_Z4:
                case GebaeudeImportProfil.ZONENREGEL_X2:
                    return kennung.Length > 0 ? kennung : Gebaeude.Kennung;
                case GebaeudeImportProfil.ZONENREGEL_X3:
                    return r.Kennung;
                default:
                    return Gebaeude.Kennung;
            }
        }

        private void ZuordnungNeu()
        {
            _zoneJeRaum.Clear();
            for (int i = 0; i < Zonen.Count; i++)
                foreach (AbbildRaum r in Zonen[i].Raeume) _zoneJeRaum[r.Kennung] = i;
        }

        /// <summary>Die Zone eines Raums der Datei: Stelle, sonst ein Raum eines anderen Gebäudes (beheizt/unbeheizt) oder unbekannt.</summary>
        private int ZoneOderArt(string raum)
        {
            if (raum == null) return UNBEKANNT;
            if (_zoneJeRaum.TryGetValue(raum, out int z)) return z;
            if (!_alleRaeume.TryGetValue(raum, out AbbildRaum r)) return UNBEKANNT;
            if (_raumGebaeude[raum] == _index) return FREMD_UNBEHEIZT;   // ein Raum dieses Gebäudes außerhalb der Zonen
            return _beheizt(r) ? FREMD_BEHEIZT : FREMD_UNBEHEIZT;
        }

        // ------------------------------------------------------------------
        //  Seiten
        // ------------------------------------------------------------------

        /// <summary>Eine Seite eines Bauteils: ein Raum mit Lage und — soweit bekannt — Geometrie.</summary>
        private sealed class Seite
        {
            internal string Kennung;
            internal string Raum;
            internal int Zone;
            internal Randbedingung Lage;
            internal double? Flaeche;
            internal double Ausschnitt;
            internal double[] Punkt;
            internal double[] Normale;
            internal string Gegenstueck;
            internal int Partner = -1;
        }

        private readonly List<string> _ohneGegenstueck = new List<string>();
        private double _ohneGegenstueckM2;
        private readonly List<string> _aufgeteilt = new List<string>();
        private readonly List<string> _oeffnungGeschaetzt = new List<string>();

        /// <summary>Die Flächen je Zone aus den Seiten aller Bauteile des Gebäudes (Klassenkopf „Seiten").</summary>
        private void Seiten(bool melden)
        {
            Flaechen.Clear();
            Luftverbindungen.Clear();
            _ohneGegenstueck.Clear();
            _ohneGegenstueckM2 = 0.0;
            _aufgeteilt.Clear();
            _oeffnungGeschaetzt.Clear();
            var luft = new Dictionary<(int, int), double>();
            foreach (AbbildBauteil s in Gebaeude.Bauteile)
            {
                var teile = new List<Zonenflaeche>();
                Bauteil(s, teile, luft);
                Oeffnungen(s, teile);
                Flaechen.AddRange(teile);
            }
            foreach (KeyValuePair<(int, int), double> l in luft.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
                Luftverbindungen.Add(new Luftverbindung(l.Key.Item1, l.Key.Item2, l.Value));
            if (!melden) return;
            if (_ohneGegenstueck.Count > 0)
                Melden(PruefStufe.Warnung, OHNE_GEGENSTUECK, Ganz(_ohneGegenstueck.Count), Zahl(_ohneGegenstueckM2), Liste(_ohneGegenstueck));
            if (_aufgeteilt.Count > 0)
                Melden(PruefStufe.Warnung, FLAECHE_AUFGETEILT, Ganz(_aufgeteilt.Count), Liste(_aufgeteilt));
            if (_oeffnungGeschaetzt.Count > 0)
                Melden(PruefStufe.Info, OEFFNUNG_ZONE, Ganz(_oeffnungGeschaetzt.Count), Liste(_oeffnungGeschaetzt));
        }

        /// <summary>Die Seiten eines Bauteils: aus seinen Raumgrenzen, sonst aus seinen Nachbarn, sonst die Hülle ohne Nachbarn.</summary>
        private List<Seite> SeitenVon(AbbildBauteil s, Dictionary<(int, int), double> luft, out bool geometrie)
        {
            var seiten = new List<Seite>();
            geometrie = false;
            if (s.Grenzen.Count > 0)
            {
                var virtuell = new List<(int Zone, double Flaeche)>();
                foreach (AbbildGrenze g in s.Grenzen)
                {
                    if (g.RaumKennung == null) continue;   // die Außenseite (IfcExternalSpatialElement) — die Lage sagt es
                    if (g.Virtuell)
                    {
                        virtuell.Add((ZoneOderArt(g.RaumKennung), g.FlaecheM2 ?? s.BruttoflaecheM2 ?? 0.0));
                        continue;
                    }
                    seiten.Add(new Seite
                    {
                        Kennung = g.Kennung, Raum = g.RaumKennung, Zone = ZoneOderArt(g.RaumKennung),
                        Lage = g.Lage == Randbedingung.Unbekannt ? s.Randbedingung : g.Lage,
                        Flaeche = g.FlaecheM2, Ausschnitt = g.AusschnittM2, Punkt = g.SchwerpunktM, Normale = g.Normale,
                        Gegenstueck = g.GegenstueckKennung,
                    });
                }
                foreach (var a in virtuell.Where(v => v.Zone >= 0))
                    foreach (var b in virtuell.Where(v => v.Zone > a.Zone))
                    {
                        (int, int) paar = (a.Zone, b.Zone);
                        luft[paar] = Math.Max(luft.TryGetValue(paar, out double f) ? f : 0.0, Math.Max(a.Flaeche, b.Flaeche));
                    }
                geometrie = seiten.Count > 0 && seiten.All(x => x.Flaeche.HasValue);
                if (seiten.Count > 0) return seiten;
            }
            if (s.Nachbarn.Count > 0)
            {
                foreach (AbbildNachbar n in s.Nachbarn)
                    seiten.Add(new Seite { Kennung = n.Kennung, Raum = n.Kennung, Zone = ZoneOderArt(n.Kennung), Lage = s.Randbedingung });
                return seiten;
            }
            if (s.HuelleOhneNachbar && (s.Randbedingung == Randbedingung.Aussenluft || s.Randbedingung == Randbedingung.Erdreich))
            {
                // Ohne Raumgrenze: die Zone des Geschosses (Z4), sonst die erste beheizte Zone.
                int zone = Zonen.FindIndex(z => z.IstBeheizt && s.GeschossKennung != null && z.Raeume.Any(r => r.GeschossKennung == s.GeschossKennung));
                if (zone < 0) zone = Zonen.FindIndex(z => z.IstBeheizt);
                if (zone >= 0) seiten.Add(new Seite { Kennung = s.Kennung, Zone = zone, Lage = s.Randbedingung });
            }
            return seiten;
        }

        private static bool Aussen(Randbedingung r) => r == Randbedingung.Aussenluft || r == Randbedingung.Erdreich;

        private void Bauteil(AbbildBauteil s, List<Zonenflaeche> teile, Dictionary<(int, int), double> luft)
        {
            List<Seite> seiten = SeitenVon(s, luft, out bool geometrie);
            if (seiten.Count == 0) return;
            double? brutto = s.BruttoflaecheM2;

            // Außen: je Zone ein Teil. Mit Geometrie die Polygonflächen, sonst das Bauteil — auf mehrere
            // Zonen nach der Zahl der Grenzen geteilt.
            List<Seite> aussen = seiten.Where(x => Aussen(x.Lage) && x.Zone >= 0).ToList();
            if (aussen.Count > 0)
            {
                var zonen = aussen.Select(x => x.Zone).Distinct().OrderBy(x => x).ToList();
                bool teilen = !geometrie && zonen.Count > 1;
                if (teilen) _aufgeteilt.Add(s.Kennung);
                foreach (int z in zonen)
                {
                    List<Seite> hier = aussen.Where(x => x.Zone == z).ToList();
                    Randbedingung lage = hier.Any(x => x.Lage == Randbedingung.Erdreich) ? Randbedingung.Erdreich : Randbedingung.Aussenluft;
                    teile.Add(new Zonenflaeche
                    {
                        Bauteil = s, Zone = z, Rand = lage == Randbedingung.Erdreich ? Zonenrand.Erdreich : Zonenrand.Aussenluft,
                        BruttoM2 = geometrie ? hier.Sum(x => x.Flaeche.Value) : brutto * hier.Count / aussen.Count,
                        AusschnittM2 = geometrie ? hier.Sum(x => x.Ausschnitt) : 0.0,
                        Aufgeteilt = teilen, Raum = hier[0].Raum,
                    });
                }
            }

            // Innen: die Seiten nach Zonen; ihr Gegenüber über die Datei, die Eindeutigkeit oder die Geometrie.
            List<Seite> innen = seiten.Where(x => !Aussen(x.Lage)).ToList();
            if (innen.Count == 0) return;
            var gruppen = innen.Select(x => x.Zone).Distinct().ToList();
            if (gruppen.Count == 1)
            {
                int z = gruppen[0];
                if (z < 0) return;   // nur fremde Räume
                if (innen.Count >= 2)
                    teile.Add(new Zonenflaeche
                    {
                        Bauteil = s, Zone = z, Rand = Zonenrand.Innen, Beidseitig = true,
                        BruttoM2 = geometrie ? innen.Sum(x => x.Flaeche.Value) / 2.0 : brutto, Raum = innen[0].Raum, AndererRaum = innen[1].Raum,
                    });
                else
                    OhneGegenueber(s, teile, innen[0], geometrie ? innen[0].Flaeche : brutto, geometrie ? innen[0].Ausschnitt : 0.0);
                return;
            }
            if (gruppen.Count == 2 && innen.All(x => string.IsNullOrEmpty(x.Gegenstueck)))
            {
                // Eindeutig: die übrigen Grenzen liegen in genau einer anderen Zone (6.2 Schritt 2).
                int a = gruppen[0], b = gruppen[1];
                Paar(s, teile, innen.Where(x => x.Zone == a).ToList(), innen.Where(x => x.Zone == b).ToList(), geometrie, brutto);
                return;
            }

            // Mehrdeutig: je Grenze ihr Gegenstück — Datei, dann Geometrie (6.2 Schritte 1 bis 4).
            Paaren(s, innen);
            foreach (IGrouping<(int, int), Seite> paar in innen.Where(x => x.Partner >= 0)
                         .GroupBy(x => (Math.Min(x.Zone, innen[x.Partner].Zone), Math.Max(x.Zone, innen[x.Partner].Zone))))
            {
                List<Seite> liste = paar.ToList();
                (int a, int b) = paar.Key;
                if (a == b)
                {
                    if (a >= 0)
                        teile.Add(new Zonenflaeche
                        {
                            Bauteil = s, Zone = a, Rand = Zonenrand.Innen, Beidseitig = true, Raum = liste[0].Raum,
                            AndererRaum = innen[liste[0].Partner].Raum, BruttoM2 = liste.Sum(x => x.Flaeche ?? 0.0) / 2.0,
                        });
                    continue;
                }
                Paar(s, teile, liste.Where(x => x.Zone == a).ToList(), liste.Where(x => x.Zone == b).ToList(), true, brutto);
            }
            foreach (Seite x in innen.Where(x => x.Partner < 0 && x.Zone >= 0))
                OhneGegenueber(s, teile, x, x.Flaeche, x.Ausschnitt, unbekannt: true);
        }

        /// <summary>Eine innere Seite, deren Gegenüber in keiner Zone liegt: fremd beheizt, fremd unbeheizt oder unbekannt.</summary>
        private void OhneGegenueber(AbbildBauteil s, List<Zonenflaeche> teile, Seite x, double? flaeche, double ausschnitt, bool unbekannt = false)
        {
            bool ohne = unbekannt;
            if (!unbekannt)
            {
                // Das Gegenüber aus den Nachbarn des Bauteils (die Einordnung der Einzonen-Zuordnung).
                ohne = !s.Nachbarn.Any(n => n.Kennung != x.Raum) && !s.Grenzen.Any(g => g.RaumKennung != null && g.RaumKennung != x.Raum);
            }
            teile.Add(new Zonenflaeche
            {
                Bauteil = s, Zone = x.Zone, Rand = Zonenrand.Unbeheizt, BruttoM2 = flaeche, AusschnittM2 = ausschnitt,
                OhneGegenstueck = ohne, Raum = x.Raum,
            });
            if (ohne)
            {
                _ohneGegenstueck.Add(s.Kennung);
                _ohneGegenstueckM2 += flaeche ?? 0.0;
            }
        }

        /// <summary>Die Teile zweier Gruppen, die einander gegenüberliegen: Trennfläche, Gebäudetrennung oder unbeheizt.</summary>
        private void Paar(AbbildBauteil s, List<Zonenflaeche> teile, List<Seite> seiteA, List<Seite> seiteB, bool geometrie, double? brutto)
        {
            int a = seiteA[0].Zone, b = seiteB[0].Zone;
            double? fa = geometrie ? seiteA.Sum(x => x.Flaeche ?? 0.0) : brutto;
            double? fb = geometrie ? seiteB.Sum(x => x.Flaeche ?? 0.0) : brutto;
            double ausA = geometrie ? seiteA.Sum(x => x.Ausschnitt) : 0.0, ausB = geometrie ? seiteB.Sum(x => x.Ausschnitt) : 0.0;
            if (a >= 0 && b >= 0)
            {
                teile.Add(new Zonenflaeche { Bauteil = s, Zone = a, Nachbarzone = b, Rand = Zonenrand.Zone, BruttoM2 = fa, GegenseiteM2 = fb,
                                             AusschnittM2 = ausA, Raum = seiteA[0].Raum, AndererRaum = seiteB[0].Raum });
                teile.Add(new Zonenflaeche { Bauteil = s, Zone = b, Nachbarzone = a, Rand = Zonenrand.Zone, BruttoM2 = fb, GegenseiteM2 = fa,
                                             AusschnittM2 = ausB, Raum = seiteB[0].Raum, AndererRaum = seiteA[0].Raum });
                return;
            }
            // Eine der Gruppen liegt außerhalb der Zonen dieses Gebäudes.
            (List<Seite> eigen, int fremd, double? f, double aus) = a >= 0 ? (seiteA, b, fa, ausA) : (seiteB, a, fb, ausB);
            if (eigen[0].Zone < 0) return;
            if (fremd == FREMD_BEHEIZT)
                teile.Add(new Zonenflaeche { Bauteil = s, Zone = eigen[0].Zone, Rand = Zonenrand.Gebaeudetrennung, BruttoM2 = f,
                                             AusschnittM2 = aus, Raum = eigen[0].Raum });
            else
            {
                teile.Add(new Zonenflaeche { Bauteil = s, Zone = eigen[0].Zone, Rand = Zonenrand.Unbeheizt, BruttoM2 = f, AusschnittM2 = aus,
                                             Raum = eigen[0].Raum, AndererRaum = (a >= 0 ? seiteB : seiteA)[0].Raum,
                                             OhneGegenstueck = fremd == UNBEKANNT });
                if (fremd == UNBEKANNT)
                {
                    _ohneGegenstueck.Add(s.Kennung);
                    _ohneGegenstueckM2 += f ?? 0.0;
                }
            }
        }

        /// <summary>
        /// Das Gegenstück je innerer Grenze (6.2): zuerst das der Datei, dann über die Geometrie — Inhalt
        /// innerhalb 1 %, Schwerpunktabstand höchstens Dicke + Zuschlag, Normalen entgegengesetzt;
        /// gepaart wird nur, wenn genau ein freier Kandidat trägt.
        /// </summary>
        private static void Paaren(AbbildBauteil s, List<Seite> innen)
        {
            var jeKennung = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < innen.Count; i++) jeKennung[innen[i].Kennung] = i;
            for (int i = 0; i < innen.Count; i++)
            {
                if (innen[i].Partner >= 0 || string.IsNullOrEmpty(innen[i].Gegenstueck)) continue;
                if (jeKennung.TryGetValue(innen[i].Gegenstueck, out int j) && j != i && innen[j].Partner < 0 && innen[j].Raum != innen[i].Raum)
                {
                    innen[i].Partner = j;
                    innen[j].Partner = i;
                }
            }
            double dicke = s.DickeM > 0.0 ? s.DickeM.Value : DICKE_ERSATZ_M;
            bool Passt(Seite x, Seite y)
            {
                if (x.Raum == y.Raum || !(x.Flaeche > 0.0) || !(y.Flaeche > 0.0) || x.Punkt == null || y.Punkt == null) return false;
                if (Math.Abs(x.Flaeche.Value - y.Flaeche.Value) > PAAR_FLAECHE_TOLERANZ * Math.Max(x.Flaeche.Value, y.Flaeche.Value)) return false;
                double dx = x.Punkt[0] - y.Punkt[0], dy = x.Punkt[1] - y.Punkt[1], dz = x.Punkt[2] - y.Punkt[2];
                if (Math.Sqrt(dx * dx + dy * dy + dz * dz) > dicke + PAAR_ABSTAND_ZUSCHLAG_M) return false;
                if (x.Normale != null && y.Normale != null
                    && x.Normale[0] * y.Normale[0] + x.Normale[1] * y.Normale[1] + x.Normale[2] * y.Normale[2] > -0.5) return false;
                return true;
            }
            bool weiter = true;
            while (weiter)
            {
                weiter = false;
                for (int i = 0; i < innen.Count; i++)
                {
                    if (innen[i].Partner >= 0) continue;
                    List<int> kandidaten = Enumerable.Range(0, innen.Count).Where(j => j != i && innen[j].Partner < 0 && Passt(innen[i], innen[j])).ToList();
                    if (kandidaten.Count != 1) continue;
                    int k = kandidaten[0];
                    // Auch aus Sicht des Kandidaten eindeutig.
                    if (Enumerable.Range(0, innen.Count).Count(j => j != k && innen[j].Partner < 0 && Passt(innen[k], innen[j])) != 1) continue;
                    innen[i].Partner = k;
                    innen[k].Partner = i;
                    weiter = true;
                }
            }
        }

        /// <summary>
        /// Die Öffnungen eines Bauteils an seine Teile: an die Zone der eigenen Grenze, sonst an den
        /// größten Teil (benannt, wenn es mehrere gab).
        /// </summary>
        private void Oeffnungen(AbbildBauteil s, List<Zonenflaeche> teile)
        {
            if (teile.Count == 0) return;
            foreach (AbbildBauteil o in s.Oeffnungen)
            {
                var zonen = new HashSet<int>(o.Grenzen.Where(g => g.RaumKennung != null).Select(g => ZoneOderArt(g.RaumKennung)).Where(z => z >= 0));
                List<Zonenflaeche> passend = teile.Where(t => zonen.Contains(t.Zone) && (t.Nachbarzone < 0 || zonen.Count < 2 || zonen.Contains(t.Nachbarzone))).ToList();
                if (passend.Count == 0)
                {
                    passend = teile;
                    if (teile.Select(t => t.Zone).Distinct().Count() > 1 && teile.Any(t => t.Rand != Zonenrand.Zone)) _oeffnungGeschaetzt.Add(o.Kennung);
                }
                // Eine Trennfläche trägt ihre Öffnung auf beiden Seiten; sonst der größte Teil.
                Zonenflaeche ziel = passend.OrderByDescending(t => t.BruttoM2 ?? 0.0).ThenBy(t => t.Zone).First();
                ziel.Oeffnungen.Add(o);
                if (ziel.Rand == Zonenrand.Zone)
                    teile.FirstOrDefault(t => t.Rand == Zonenrand.Zone && t.Zone == ziel.Nachbarzone && t.Nachbarzone == ziel.Zone)?.Oeffnungen.Add(o);
            }
        }

        // ------------------------------------------------------------------
        //  Mindestgröße (M8)
        // ------------------------------------------------------------------

        private void Mindestgroesse()
        {
            while (true)
            {
                int klein = -1, ziel = -1;
                double gemeinsam = 0.0;
                foreach (int i in Enumerable.Range(0, Zonen.Count).Where(i => Zonen[i].FlaecheM2 < MindestflaecheM2)
                                            .OrderBy(i => Zonen[i].FlaecheM2).ThenBy(i => i))
                {
                    var nachbarn = new Dictionary<int, double>();
                    foreach (Zonenflaeche f in Flaechen.Where(f => f.Zone == i && f.Rand == Zonenrand.Zone
                                                                    && Zonen[f.Nachbarzone].IstBeheizt == Zonen[i].IstBeheizt))
                        nachbarn[f.Nachbarzone] = (nachbarn.TryGetValue(f.Nachbarzone, out double w) ? w : 0.0) + (f.GroessereM2 ?? 0.0);
                    if (nachbarn.Count == 0 || nachbarn.Values.Max() <= 0.0) continue;
                    KeyValuePair<int, double> best = nachbarn.OrderByDescending(n => n.Value).ThenBy(n => n.Key).First();
                    klein = i;
                    ziel = best.Key;
                    gemeinsam = best.Value;
                    break;
                }
                if (klein < 0) break;
                Importzone k = Zonen[klein], z = Zonen[ziel];
                Melden(PruefStufe.Info, ZONE_ZUGESCHLAGEN, k.Name, Zahl(k.FlaecheM2 ?? 0.0), z.Name, Zahl(gemeinsam));
                z.Raeume.AddRange(k.Raeume);
                z.Zugeschlagen.Add(k.Name);
                z.Zugeschlagen.AddRange(k.Zugeschlagen);
                Zonen.RemoveAt(klein);
                ZuordnungNeu();
                Seiten(melden: false);
            }
            foreach (Importzone zone in Zonen.Where(x => x.FlaecheM2 < MindestflaecheM2))
            {
                zone.ZuKlein = true;
                Melden(PruefStufe.Info, ZONE_ZU_KLEIN, zone.Name, Zahl(zone.FlaecheM2 ?? 0.0), Zahl(MindestflaecheM2));
            }
        }

        // ------------------------------------------------------------------
        //  Abschluss: Gegenprobe, Hülle, Obergrenze
        // ------------------------------------------------------------------

        private void Abschluss()
        {
            Trennungen.Clear();
            for (int a = 0; a < Zonen.Count; a++)
                for (int b = a + 1; b < Zonen.Count; b++)
                {
                    double ab = Flaechen.Where(f => f.Zone == a && f.Nachbarzone == b).Sum(f => f.BruttoM2 ?? 0.0);
                    double ba = Flaechen.Where(f => f.Zone == b && f.Nachbarzone == a).Sum(f => f.BruttoM2 ?? 0.0);
                    if (ab <= 0.0 && ba <= 0.0) continue;
                    var t = new Zonentrennung(a, b, ab, ba);
                    Trennungen.Add(t);
                    if (t.Ungleich) Melden(PruefStufe.Warnung, TRENNFLAECHE_UNGLEICH, Zonen[a].Name, Zonen[b].Name, Zahl(ab), Zahl(ba));
                }
            foreach (Luftverbindung l in Luftverbindungen)
                Melden(PruefStufe.Info, LUFTVERBINDUNG, Zonen[l.ZoneA].Name, Zonen[l.ZoneB].Name, Zahl(l.FlaecheM2));
            for (int i = 0; i < Zonen.Count; i++)
            {
                Zonen[i].OhneAussen = !Flaechen.Any(f => f.Zone == i && (f.Rand == Zonenrand.Aussenluft || f.Rand == Zonenrand.Erdreich));
                if (Zonen[i].OhneAussen) Melden(PruefStufe.Info, ZONE_OHNE_AUSSEN, Zonen[i].Name);
            }
            if (ZuVieleZonen)
            {
                // Der Vorschlag der gröberen Regel: auf Geschosse zusammenlegen, sonst eine Zone.
                string geschoss = string.Equals(Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)
                    ? IfcImportProfil.ZONENREGEL_Z4 : GebaeudeImportProfil.ZONENREGEL_X2;
                string eine = string.Equals(Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)
                    ? IfcImportProfil.ZONENREGEL_Z5 : GebaeudeImportProfil.ZONENREGEL_X4;
                Vorschlagsregel = Regel != geschoss && Regeln.Contains(geschoss) ? geschoss : eine;
                int zahl = Vorschlagsregel == eine ? 1 : Bilden(_abbild, _index, Vorschlagsregel).Zonen.Count;
                Melden(PruefStufe.Warnung, ZU_VIELE_ZONEN_VORSCHLAG, Ganz(Zonen.Count), Ganz(GebaeudeZonenregeln.PFLEGEGRENZE),
                       Vorschlagsregel, Ganz(zahl));
            }
            Melden(PruefStufe.Info, ZONENREGEL, Regel, Ganz(Zonen.Count), Vorgabe);
        }

        // ------------------------------------------------------------------
        //  Hilfen
        // ------------------------------------------------------------------

        private void Melden(PruefStufe stufe, string name, params string[] werte)
            => Meldungen.Add(new PruefMeldung(stufe, _praefix + name, werte));

        private static string Liste(List<string> kennungen)
        {
            List<string> eindeutig = kennungen.Distinct(StringComparer.Ordinal).ToList();
            return eindeutig.Count <= 5 ? string.Join(", ", eindeutig) : string.Join(", ", eindeutig.Take(5)) + ", …";
        }

        private static string Zahl(double w) => GebaeudeImportAblauf.Zahl(Math.Round(w, 2));

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
