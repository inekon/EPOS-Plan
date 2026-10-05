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

        /// <summary>
        /// Hat eine Zuordnung von Hand (<see cref="Raumumhaengung"/>) die Zone gebildet oder einen Raum in sie
        /// oder aus ihr gebracht? Die Mindestgröße wird dann nur gemeldet, nicht mehr zugeschlagen.
        /// </summary>
        internal bool Handgeaendert { get; set; }

        /// <summary>
        /// Die Nutzung der Zone aus dem <see cref="Zonenplan"/> (<c>WOHNEN</c>, <c>BUERO</c>, <c>SCHULE</c>); <c>null</c> = keine.
        /// Beim Speichern bekommt die Zone die ausgelieferten Vorlagen dieser Nutzung als Kalenderkopien.
        /// </summary>
        internal string Nutzung { get; set; }

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
    /// <b>Eine Zuordnung von Hand</b> (Stufe G6c, Welle D; Mehrzonenkonzept 6.4 und 6.7): der Raum
    /// <paramref name="Raum"/> geht in die Zone mit dem Schlüssel <paramref name="Zielzone"/>
    /// (<see cref="Importzone.Schluessel"/>, nie ihr Name) oder — ohne Zielzone — als eigene Zone ab. Die
    /// Zonierung legt die Zuordnungen in ihrer Reihenfolge auf den Vorschlag der Regel.
    /// </summary>
    /// <param name="Raum">Die Kennung des Raums in der Datei.</param>
    /// <param name="Zielzone">Der Schlüssel der Zielzone; <c>null</c> oder leer = als eigene Zone abtrennen.</param>
    internal sealed record Raumumhaengung(string Raum, string Zielzone);

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
    /// wenn mehr als ein Geschoss Räume trägt und die Datei Raumgrenzen führt oder — ohne sie — die
    /// Trenndecken aus den Raumbezügen alle beheizten Geschosse koppeln
    /// (<see cref="AbbildGebaeude.GeschosseGekoppelt"/>), sonst Z5; gbXML X1, sonst X2, sonst X4. Ohne
    /// Raumgrenzen und ohne solche Kopplung ist Z4 nur auf ausdrückliche Wahl zu haben, mit Warnung — die
    /// Geschosszonen wären entkoppelt (6.5); Z1 bis Z3 brauchen die Grenzen.</item>
    /// <item><b>Beheizung:</b> Eine Zone fasst nur Räume gleichen Zustands (Regeln B1…B6 des Lesers bzw.
    /// <c>@conditionType</c>, übersteuert durch die Haken der Raumliste); die unbeheizten Räume einer Gruppe
    /// bilden eine eigene, frei schwingende Zone. Unter Z5/X4 gibt es nur die eine beheizte Zone; die
    /// unbeheizten Räume bleiben draußen (Einzonenweg, G4b).</item>
    /// <item><b>Seiten:</b> je Bauteil die Grenzen der Räume, je Zone zusammengefasst. Außen liegt, was
    /// die Grenze (bzw. das Bauteil) <c>EXTERNAL</c>/<c>EXTERNAL_EARTH</c> nennt. Eine innere Grenze
    /// findet ihr Gegenüber (M13, 6.2): (1) liegen die inneren Grenzen des Bauteils in genau zwei Zonen,
    /// ist es eindeutig; (2) sonst das Gegenstück der Datei; (3) sonst die Geometrie in Weltkoordinaten —
    /// Flächeninhalt innerhalb <see cref="PAAR_FLAECHE_TOLERANZ"/>, Schwerpunktabstand höchstens
    /// Bauteildicke + <see cref="PAAR_ABSTAND_ZUSCHLAG_M"/> (Grenzen liegen auf den Bauteiloberflächen),
    /// Normalen entgegengesetzt, und nur, wenn genau ein freier Kandidat trägt; (4) sonst „ohne
    /// Gegenstück" — Randbedingung unbeheizt, benannt. Ohne Polygon trägt eine solche Grenze geschätzt
    /// 2/n der Bauteilfläche (benannt als aufgeteilt).</item>
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
    /// <item><b>Zuordnung von Hand</b> (Welle D; Mehrzonenkonzept 6.4, 6.7): Nach dem Vorschlag samt
    /// Mindestgröße legt die Zonierung die <see cref="Raumumhaengung"/>en in ihrer Reihenfolge auf — ein Raum
    /// geht in eine andere Zone gleicher Beheizung oder als eigene Zone ab (angehängt, Schlüssel
    /// <see cref="HAND_PRAEFIX"/> + Raum), eine leer gewordene Zone entfällt. Seiten, Trennflächen, Gegenprobe
    /// und Obergrenze bilden sich daraus neu; eine von Hand veränderte Zone unter der Mindestgröße wird
    /// benannt gemeldet und nicht zugeschlagen. Unter Z5/X4 wird nichts umgehängt (benannt). Geschrieben
    /// wird nichts: <see cref="Umhaengen"/> liefert eine neue Zonierung.</item>
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
        /// <summary>I — {0} Zone, {1} Fläche: unbeheizt ohne jede Fläche — entfällt, die Räume bleiben außerhalb der Zonen.</summary>
        internal const string ZONE_OHNE_FLAECHEN = "ZONE_OHNE_FLAECHEN";
        /// <summary>W — {0} Zahl der Zonen, {1} Grenze, {2} vorgeschlagene Regel, {3} deren Zonenzahl: zu viele Zonen.</summary>
        internal const string ZU_VIELE_ZONEN_VORSCHLAG = "ZU_VIELE_ZONEN_VORSCHLAG";
        /// <summary>I — {0} Zone A, {1} Zone B, {2} Fläche: virtuelle Grenze — Vorschlag eines Luftaustauschs.</summary>
        internal const string LUFTVERBINDUNG = "LUFTVERBINDUNG";
        /// <summary>I — {0} Raum, {1} alte Zone, {2} neue Zone: von Hand umgehängt.</summary>
        internal const string RAUM_UMGEHAENGT = "RAUM_UMGEHAENGT";
        /// <summary>I — {0} Raum, {1} alte Zone: von Hand als eigene Zone abgetrennt.</summary>
        internal const string RAUM_ABGETRENNT = "RAUM_ABGETRENNT";
        /// <summary>I — {0} Zone: nach einer Zuordnung von Hand leer — sie entfällt.</summary>
        internal const string ZONE_ENTFALLEN = "ZONE_ENTFALLEN";
        /// <summary>W — {0} Raum: kein Raum einer Zone dieses Gebäudes — nicht umgehängt.</summary>
        internal const string UMHAENGEN_RAUM_UNBEKANNT = "UMHAENGEN_RAUM_UNBEKANNT";
        /// <summary>W — {0} Raum, {1} Schlüssel der Zielzone: keine solche Zone — nicht umgehängt.</summary>
        internal const string UMHAENGEN_ZONE_UNBEKANNT = "UMHAENGEN_ZONE_UNBEKANNT";
        /// <summary>W — {0} Raum, {1} Zielzone: nicht gleich beheizt — nicht umgehängt.</summary>
        internal const string UMHAENGEN_BEHEIZUNG = "UMHAENGEN_BEHEIZUNG";
        /// <summary>W — {0} Regel: unter einer Zone je Gebäude wird nicht umgehängt.</summary>
        internal const string UMHAENGEN_EINZONIG = "UMHAENGEN_EINZONIG";
        /// <summary>W — {0} Zone, {1} Fläche, {2} Mindestgröße: nach der Zuordnung von Hand zu klein — bleibt, nicht zugeschlagen.</summary>
        internal const string ZONE_ZU_KLEIN_HAND = "ZONE_ZU_KLEIN_HAND";
        /// <summary>Meldung (W): Räume des Plans in keiner Zone — Zahl und bis fünf Namen.</summary>
        internal const string RAEUME_NICHT_ZUGEORDNET = "RAEUME_NICHT_ZUGEORDNET";
        /// <summary>Meldung (I): eine Zone des Plans ohne Raum — 0 m², sie wird nicht übernommen.</summary>
        internal const string ZONE_LEER = "ZONE_LEER";
        /// <summary>Meldung (W): eine Zone des Plans mit beheizten und unbeheizten Räumen.</summary>
        internal const string PLAN_BEHEIZUNG_GEMISCHT = "PLAN_BEHEIZUNG_GEMISCHT";

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

        /// <summary>Der Anfang des Schlüssels einer Zone, die eine Zuordnung von Hand bildet: <c>HAND|</c> + Raum + Beheizung.</summary>
        internal const string HAND_PRAEFIX = "HAND|";

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

        /// <summary>Ist ein Raum wirksam beheizt — mit den Haken der Raumliste, mit denen die Zonierung gebildet ist?</summary>
        internal bool RaumBeheizt(AbbildRaum r) => r != null && (_beheizt?.Invoke(r) ?? r.Beheizt);

        /// <summary>Die Zuordnungen von Hand, mit denen gebildet ist, in ihrer Reihenfolge (auch die abgelehnten).</summary>
        internal IReadOnlyList<Raumumhaengung> Umhaengungen { get; private set; } = Array.Empty<Raumumhaengung>();

        /// <summary>Hat mindestens eine Zuordnung von Hand gewirkt?</summary>
        internal bool Handzuordnung { get; private set; }

        /// <summary>Der Zonenplan, aus dem gebildet ist; <c>null</c> = der Regelvorschlag.</summary>
        internal Zonenplan Plan { get; private set; }

        /// <summary>Die Namen der Zonen des Plans ohne Raum, in Planreihenfolge — in der Bilanz mit 0 m².</summary>
        internal List<string> LeereZonen { get; } = new List<string>();

        /// <summary>Die Räume dieses Gebäudes in keiner Zone des Plans, in Dateireihenfolge; leer ohne Plan.</summary>
        internal List<AbbildRaum> NichtZugeordnet { get; } = new List<AbbildRaum>();

        /// <summary>
        /// <b>Hängt einen Raum um</b> (Stufe G6c, Welle D): die Zonierung derselben Datei, Regel und Haken mit
        /// dieser Zuordnung hinter den bisherigen — <paramref name="zielzone"/> ist der Schlüssel einer Zone,
        /// <c>null</c> heißt „als eigene Zone abtrennen". Schreibt nichts; eine abgelehnte Zuordnung steht
        /// benannt in den Meldungen der neuen Zonierung.
        /// </summary>
        internal GebaeudeZonierung Umhaengen(string raum, string zielzone)
        {
            if (_abbild == null) return this;
            var liste = new List<Raumumhaengung>(Umhaengungen) { new Raumumhaengung(raum, zielzone) };
            return Bilden(_abbild, _index, Regel, _uebersteuert, liste);
        }

        private readonly Dictionary<string, int> _zoneJeRaum = new Dictionary<string, int>(StringComparer.Ordinal);
        private string _praefix = "";
        private GebaeudeAbbild _abbild;
        private int _index;
        private IReadOnlyDictionary<string, bool> _uebersteuert;
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
                if (g.Raeume.Count > 1)
                {
                    int gruppen = Z6Gruppen(g.Raeume, r => r.Beheizt).Select(x => x.Kennung + "|" + (x.Warm ? "B" : "U"))
                                                                    .Distinct(StringComparer.Ordinal).Count();
                    if (gruppen > 1 && gruppen < g.Raeume.Count) regeln.Add(IfcImportProfil.ZONENREGEL_Z6);
                }
                regeln.Add(IfcImportProfil.ZONENREGEL_Z5);
                string vorgabe = (grenzen || g.GeschosseGekoppelt) && geschosse > 1 ? IfcImportProfil.ZONENREGEL_Z4 : IfcImportProfil.ZONENREGEL_Z5;
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
        /// <param name="umhaengungen">Die Zuordnungen von Hand in ihrer Reihenfolge (Welle D); <c>null</c> = keine.</param>
        /// <param name="plan">Der Zonenplan (Mehrzonenkonzept 6.4): Zonen, Namen, Nutzung und Räume kommen allein aus ihm;
        /// Regel und Umhängungen wirken dann nicht, die Mindestgröße M8 schlägt nichts zu. <c>null</c> = der Regelvorschlag.</param>
        internal static GebaeudeZonierung Bilden(GebaeudeAbbild abbild, int index, string regel = null,
                                                 IReadOnlyDictionary<string, bool> beheiztUebersteuert = null,
                                                 IReadOnlyList<Raumumhaengung> umhaengungen = null,
                                                 Zonenplan plan = null)
        {
            var z = new GebaeudeZonierung();
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return z;
            z._abbild = abbild;
            z._index = index;
            z._uebersteuert = beheiztUebersteuert;
            z.Umhaengungen = umhaengungen == null ? Array.Empty<Raumumhaengung>() : umhaengungen.Where(u => u != null).ToList();
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
            z.Regel = plan?.Regel ?? regel ?? vorgabe;
            z.Plan = plan;
            // Trennflächen aus den Raumkörpern tragen die Nachbarschaft wie Raumgrenzen (6.2); Z1 bis Z3 bleiben an diese gebunden.
            bool koerper = !grenzen && z.Gebaeude.KoerperpaareGebildet;
            if (!grenzen && !z.Gebaeude.GeschosseGekoppelt && !koerper)
                z.Melden(PruefStufe.Warnung, KEINE_GRENZEN, z.Gebaeude.Anzeigename);
            else if (grenzen && string.Equals(z.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal) && z.Gebaeude.ZahlGrenzenZweiteEbene == 0)
                z.Melden(PruefStufe.Warnung, NUR_1STLEVEL, z.Gebaeude.Anzeigename, Ganz(z.Gebaeude.ZahlGrenzen));
            if (!regeln.Contains(z.Regel))
            {
                z.Melden(PruefStufe.Fehler, ZONENREGEL_UNGUELTIG, z.Regel, string.Join(", ", regeln));
                return z;
            }
            z.Einzonig = plan == null && IstEinzonig(z.Regel);

            double gesamt = z.Gebaeude.Raeume.Where(r => r.FlaecheM2 > 0.0).Sum(r => r.FlaecheM2.Value);
            z.MindestflaecheM2 = Math.Max(MINDESTFLAECHE_M2, MINDESTANTEIL * gesamt);

            if (plan != null)
            {
                // Der Plan ist die Zonierung von Hand: keine Regel, kein Zuschlag nach M8, keine entfallende Zone.
                bool bezugPlan = !grenzen && (z.Gebaeude.GeschosseGekoppelt || koerper);
                if (!grenzen && !bezugPlan) z.Melden(PruefStufe.Warnung, GRENZEN_ENTKOPPELT, z.Regel);
                z.AusPlanGruppieren(plan);
                z.Seiten(melden: false);
                z.KleineMelden();
                z.Seiten(melden: true);
                if (bezugPlan && !z.BeheizteVerbunden())
                    z.Melden(PruefStufe.Warnung, GRENZEN_ENTKOPPELT, z.Regel);
                z.Abschluss();
                return z;
            }

            z.Gruppieren();
            if (z.Einzonig)
            {
                if (z.Umhaengungen.Count > 0) z.Melden(PruefStufe.Warnung, UMHAENGEN_EINZONIG, z.Regel);
                z.Melden(PruefStufe.Info, ZONENREGEL, z.Regel, Ganz(z.Zonen.Count), z.Vorgabe);
                return z;
            }
            // Ohne Raumgrenzen und ohne tragende Trenndecken aus den Raumbezügen sind die Zonen entkoppelt (6.5); mit
            // ihnen nur, wenn sie nicht alle beheizten Zonen verbinden (geprüft nach den Seiten, mit den Haken).
            bool bezug = !grenzen && (z.Gebaeude.GeschosseGekoppelt || koerper);
            if (!grenzen && !bezug)
                z.Melden(PruefStufe.Warnung, GRENZEN_ENTKOPPELT, z.Regel);

            z.Seiten(melden: false);
            z.Zuschlagen();
            z.HandzuordnungAnwenden();
            z.OhneFlaechenEntfallen();
            z.KleineMelden();
            z.Seiten(melden: true);
            if (bezug && !z.BeheizteVerbunden())
                z.Melden(PruefStufe.Warnung, GRENZEN_ENTKOPPELT, z.Regel);
            z.Abschluss();
            return z;
        }

        // ------------------------------------------------------------------
        //  Gruppen
        // ------------------------------------------------------------------

        /// <summary>Die Räume in Zonen je Regel und Beheizung, in Dateireihenfolge.</summary>
        private void Gruppieren()
        {
            if (Regel == IfcImportProfil.ZONENREGEL_Z6) { Z6Gruppieren(); return; }
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

        /// <summary>
        /// Die Zonen aus dem Plan in Planreihenfolge, die Räume je Zone in Dateireihenfolge. Eine Zone ohne Raum steht in
        /// <see cref="LeereZonen"/> (Info), die Räume ohne Zone in <see cref="NichtZugeordnet"/> (Warnung, Zahl und bis
        /// fünf Namen); beide liegen außerhalb der Zonen und zählen nicht zur Zonenfläche.
        /// </summary>
        private void AusPlanGruppieren(Zonenplan plan)
        {
            foreach (Planzone pz in plan.Zonen)
            {
                List<AbbildRaum> raeume = Gebaeude.Raeume
                    .Where(r => string.Equals(plan.ZoneVon(r.Kennung), pz.Schluessel, StringComparison.Ordinal)).ToList();
                if (raeume.Count == 0)
                {
                    LeereZonen.Add(pz.Name);
                    Melden(PruefStufe.Info, ZONE_LEER, pz.Name);
                    continue;
                }
                bool warm = raeume.Any(_beheizt);
                if (raeume.Any(r => _beheizt(r) != warm)) Melden(PruefStufe.Warnung, PLAN_BEHEIZUNG_GEMISCHT, pz.Name);
                var zone = new Importzone
                {
                    Schluessel = pz.Schluessel, Name = pz.Name, IstBeheizt = warm, Nutzung = pz.Nutzung,
                    Quellkennung = pz.Quellkennung ?? (raeume.Count == 1 ? raeume[0].Kennung : Gebaeude.Kennung),
                    Handgeaendert = pz.Angelegt || pz.Geaendert,
                };
                zone.Raeume.AddRange(raeume);
                Zonen.Add(zone);
            }
            NichtZugeordnet.AddRange(plan.NichtZugeordnet);
            if (NichtZugeordnet.Count > 0)
                Melden(PruefStufe.Warnung, RAEUME_NICHT_ZUGEORDNET, Ganz(NichtZugeordnet.Count),
                       Liste(NichtZugeordnet.Select(Zonenplan.Raumname).ToList()));
            Handzuordnung = Zonen.Any(x => x.Handgeaendert);
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
            if (s.HuelleOhneNachbar && (s.Randbedingung == Randbedingung.Aussenluft || s.Randbedingung == Randbedingung.Erdreich
                                        || s.Randbedingung == Randbedingung.Unbeheizt))
            {
                // Ohne Raumgrenze: die Zone des Geschosses (Z4), sonst die erste beheizte Zone.
                int zone = Geschosszone(s);
                if (zone >= 0) seiten.Add(new Seite { Kennung = s.Kennung, Zone = zone, Lage = s.Randbedingung });
            }
            return seiten;
        }

        private static bool Aussen(Randbedingung r) => r == Randbedingung.Aussenluft || r == Randbedingung.Erdreich;

        /// <summary>Die Zone eines Bauteils ohne Raum: die beheizte Zone seines Geschosses (Z4), sonst die erste beheizte; −1 = keine.</summary>
        private int Geschosszone(AbbildBauteil s)
        {
            int zone = Zonen.FindIndex(z => z.IstBeheizt && s.GeschossKennung != null && z.Raeume.Any(r => r.GeschossKennung == s.GeschossKennung));
            return zone >= 0 ? zone : Zonen.FindIndex(z => z.IstBeheizt);
        }

        /// <summary>Hängen alle beheizten Zonen über Trennflächen (auch über unbeheizte Zonen) zusammen?</summary>
        private bool BeheizteVerbunden()
        {
            var warm = Enumerable.Range(0, Zonen.Count).Where(i => Zonen[i].IstBeheizt).ToList();
            if (warm.Count < 2) return true;
            var erreicht = new HashSet<int> { warm[0] };
            bool weiter = true;
            while (weiter)
            {
                weiter = false;
                foreach (Zonenflaeche f in Flaechen.Where(f => f.Rand == Zonenrand.Zone && f.Nachbarzone >= 0))
                    if (erreicht.Contains(f.Zone) != erreicht.Contains(f.Nachbarzone)) { erreicht.Add(f.Zone); erreicht.Add(f.Nachbarzone); weiter = true; }
            }
            return warm.All(erreicht.Contains);
        }

        private void Bauteil(AbbildBauteil s, List<Zonenflaeche> teile, Dictionary<(int, int), double> luft)
        {
            // Eine Innenwand ohne Nachbarraum (IFC ohne Raumgrenzen): einseitig innere Masse in der Zone ihres Geschosses (6.5).
            if (s.InnenEinseitig && s.Nachbarn.Count == 0 && s.Grenzen.Count == 0)
            {
                int zone = Geschosszone(s);
                if (zone >= 0) teile.Add(new Zonenflaeche { Bauteil = s, Zone = zone, Rand = Zonenrand.Innen, BruttoM2 = s.BruttoflaecheM2 });
                return;
            }
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
            if (gruppen.Count == 2)
            {
                // Eindeutig: die übrigen Grenzen liegen in genau einer anderen Zone (6.2 Schritt 2) — die
                // Gegenstücke der Datei sagen nichts anderes.
                int a = gruppen[0], b = gruppen[1];
                Paar(s, teile, innen.Where(x => x.Zone == a).ToList(), innen.Where(x => x.Zone == b).ToList(), geometrie, brutto);
                return;
            }

            // Mehrdeutig: je Grenze ihr Gegenstück — Datei, dann Geometrie (6.2 Schritte 1 bis 4). Eine
            // Grenze ohne Polygon trägt geschätzt den Anteil 2/n der Bauteilfläche (zwei Seiten, n Grenzen).
            if (innen.Any(x => !x.Flaeche.HasValue) && brutto > 0.0)
            {
                foreach (Seite x in innen.Where(x => !x.Flaeche.HasValue)) x.Flaeche = brutto.Value * 2.0 / innen.Count;
                _aufgeteilt.Add(s.Kennung);
            }
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
                // Ein Hüllbauteil ohne Nachbarraum gegen unbeheizt nennt sein Gegenüber durch die Angrenzung der Datei.
                ohne = !s.HuelleOhneNachbar
                       && !s.Nachbarn.Any(n => n.Kennung != x.Raum) && !s.Grenzen.Any(g => g.RaumKennung != null && g.RaumKennung != x.Raum);
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
        //  Regel Z6: nach Raumtemperatur und Nutzung
        // ------------------------------------------------------------------

        /// <summary>
        /// Die Nutzungsklassen (Z6) in fester Reihenfolge — sprachneutrale Schlüssel; der Anzeigetext steht in
        /// <c>GIMP_NUTZUNG_&lt;KLASSE&gt;</c>.
        /// </summary>
        internal static readonly IReadOnlyList<string> NUTZUNGSKLASSEN = new[]
        {
            "Buero", "Wohnen", "Schlafen", "Gastronomie", "Kueche", "Sport", "Verkehr", "Sanitaer", "Lager", "Technik", "Sonstige",
        };

        /// <summary>Die Klasse ohne Zuordnung.</summary>
        internal const string NUTZUNG_SONSTIGE = "Sonstige";

        /// <summary>Der Raumtyp der Datei (ohne Präfix <c>mrt</c>, Groß-/Kleinschreibung egal) → Nutzungsklasse.</summary>
        private static readonly IReadOnlyDictionary<string, string> RAUMTYP_KLASSE = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Office"] = "Buero", ["Conference"] = "Buero", ["Examination"] = "Buero",
            ["Living"] = "Wohnen", ["Child"] = "Wohnen", ["Guests"] = "Wohnen",
            ["Sleeping"] = "Schlafen",
            ["Restaurant"] = "Gastronomie", ["Eating"] = "Gastronomie",
            ["Kitchen"] = "Kueche",
            ["Fitness"] = "Sport", ["Sauna"] = "Sport",
            ["Hall"] = "Verkehr", ["HallWay"] = "Verkehr", ["Stairway"] = "Verkehr", ["Ante"] = "Verkehr",
            ["WC"] = "Sanitaer", ["Bath"] = "Sanitaer", ["Shower"] = "Sanitaer", ["Locker"] = "Sanitaer",
            ["Store"] = "Lager", ["StorageRoom"] = "Lager", ["Basement"] = "Lager", ["Roof"] = "Lager",
            ["CentralHeating"] = "Technik", ["Connection"] = "Technik",   // Anschlussräume (Hausanschluss, Heizung, Lüftung)
            ["AdjoiningRoom"] = "Sonstige", ["Workshop"] = "Sonstige",
        };

        /// <summary>
        /// Die Namensmuster je Klasse (Teilwort, klein; deutsch und englisch, die Muster der Regel B4 eingeschlossen) in
        /// Prüfreihenfolge — die erste Klasse mit Treffer gilt.
        /// </summary>
        private static readonly (string Klasse, string[] Muster)[] NAME_KLASSE =
        {
            ("Sanitaer", new[] { "wc", "bad", "dusch", "umkleide", "sanit", "toilet", "bath", "shower", "washroom", "locker" }),
            ("Kueche", new[] { "küche", "kueche", "kitchen" }),
            ("Gastronomie", new[] { "restaurant", "gastst", "gastraum", "speiseraum", "kantine", "cafe", "café", "bistro", "dining", "canteen" }),
            ("Schlafen", new[] { "schlaf", "sleep", "bedroom" }),
            ("Buero", new[] { "büro", "buero", "office", "besprech", "konferenz", "meeting", "conference" }),
            ("Sport", new[] { "sport", "fitness", "gym", "turn", "sauna" }),
            ("Verkehr", new[] { "flur", "diele", "treppe", "foyer", "eingang", "windfang", "corridor", "hallway", "stair", "lobby", "entrance" }),
            ("Technik", new[] { "technik", "heizraum", "heizung", "schacht", "aufzug", "hausanschluss", "verteiler", "lüftung", "plant", "shaft", "server", "mechanical" }),
            ("Lager", new[] { "lager", "abstell", "keller", "garage", "carport", "dachboden", "speicher", "store", "storage", "basement", "attic" }),
            ("Wohnen", new[] { "wohn", "living", "kinderzimmer", "guest" }),
        };

        /// <summary>
        /// <b>Die Nutzungsklasse eines Raums</b> (Z6): aus dem Raumtyp der Datei über eine feste Tabelle, sonst aus dem
        /// Raumnamen (<see cref="Nutzung"/>) über Namensmuster, sonst <see cref="NUTZUNG_SONSTIGE"/>. Ein sprachneutraler
        /// Schlüssel aus <see cref="NUTZUNGSKLASSEN"/>.
        /// </summary>
        internal static string Nutzungsklasse(AbbildRaum r)
        {
            if (r == null) return NUTZUNG_SONSTIGE;
            string typ = (r.Raumtyp ?? "").Trim();
            if (typ.Length > 3 && typ.StartsWith("mrt", StringComparison.Ordinal)) typ = typ.Substring(3);
            if (typ.Length > 0 && RAUMTYP_KLASSE.TryGetValue(typ, out string klasse)) return klasse;
            string name = Nutzung(r.Name);
            if (name.Length > 0)
                foreach ((string k, string[] muster) in NAME_KLASSE)
                    if (muster.Any(m => name.Contains(m, StringComparison.Ordinal))) return k;
            return NUTZUNG_SONSTIGE;
        }

        /// <summary>Der Anzeigetext einer Nutzungsklasse (<c>GIMP_NUTZUNG_&lt;KLASSE&gt;</c>); ohne Ressource der Schlüssel.</summary>
        internal static string NutzungText(string klasse)
            => Ressource("GIMP_NUTZUNG_" + (klasse ?? "").ToUpperInvariant()) ?? klasse ?? "";

        /// <summary>Die auf ganze °C gerundete Raumsolltemperatur (Z6): Heizsollwert, sonst Raumtemperatur der Datei.</summary>
        internal static double? Z6Temperatur(AbbildRaum r)
        {
            double? t = r?.SollHeizenC ?? r?.RaumtemperaturC;
            return t.HasValue && !double.IsNaN(t.Value) && !double.IsInfinity(t.Value)
                ? Math.Round(t.Value, MidpointRounding.AwayFromZero) : (double?)null;
        }

        /// <summary>
        /// <b>Die Gruppen der Regel Z6</b> je Raum in Dateireihenfolge: Kennung <c>T|&lt;°C&gt;</c> aus Beheizung und gerundeter
        /// Temperatur; ein Raum ohne Temperatur geht zur Temperaturgruppe gleicher Beheizung, in der seine Nutzungsklasse die
        /// größte Fläche hat (Gleichstand: die wärmere), sonst in die Gruppe <c>N|&lt;Klasse&gt;</c> („ohne Sollwert").
        /// </summary>
        internal static List<(AbbildRaum Raum, string Kennung, bool Warm, double? Temperatur, string Klasse)> Z6Gruppen(
            IEnumerable<AbbildRaum> raeume, Func<AbbildRaum, bool> beheizt)
        {
            var liste = raeume.Select(r => (Raum: r, Warm: beheizt(r), T: Z6Temperatur(r), Klasse: Nutzungsklasse(r))).ToList();
            var ergebnis = new List<(AbbildRaum, string, bool, double?, string)>(liste.Count);
            foreach (var x in liste)
            {
                double? t = x.T;
                if (!t.HasValue)
                    t = liste.Where(y => y.T.HasValue && y.Warm == x.Warm && y.Klasse == x.Klasse)
                             .GroupBy(y => y.T.Value)
                             .Select(gr => (T: gr.Key, A: gr.Sum(y => y.Raum.FlaecheM2 > 0.0 ? y.Raum.FlaecheM2.Value : 0.0)))
                             .OrderByDescending(k => k.A).ThenByDescending(k => k.T)
                             .Select(k => (double?)k.T).FirstOrDefault();
                string kennung = t.HasValue ? "T|" + t.Value.ToString("0", CultureInfo.InvariantCulture) : "N|" + x.Klasse;
                ergebnis.Add((x.Raum, kennung, x.Warm, t, x.Klasse));
            }
            return ergebnis;
        }

        /// <summary>Die Solltemperatur je Zonenschlüssel (Z6); <c>null</c> = ohne Sollwert.</summary>
        private readonly Dictionary<string, double?> _z6Temperatur = new Dictionary<string, double?>(StringComparer.Ordinal);

        /// <summary>
        /// Die Zonen der Regel Z6: gebäudeweit je Gruppe, in fester Ordnung — absteigend nach Solltemperatur (ohne Sollwert
        /// zuletzt), beheizt vor unbeheizt, dann Name ordinal; die Räume einer Zone in Dateireihenfolge.
        /// </summary>
        private void Z6Gruppieren()
        {
            var jeSchluessel = new Dictionary<string, Importzone>(StringComparer.Ordinal);
            var zonen = new List<Importzone>();
            foreach (var x in Z6Gruppen(Gebaeude.Raeume, _beheizt))
            {
                string schluessel = Regel + "|" + x.Kennung + "|" + (x.Warm ? "B" : "U");
                if (!jeSchluessel.TryGetValue(schluessel, out Importzone zone))
                {
                    zone = new Importzone { Schluessel = schluessel, IstBeheizt = x.Warm, Quellkennung = Gebaeude.Kennung };
                    jeSchluessel[schluessel] = zone;
                    _z6Temperatur[schluessel] = x.Temperatur;
                    zonen.Add(zone);
                }
                zone.Raeume.Add(x.Raum);
            }
            foreach (Importzone zone in zonen)
            {
                double? t = _z6Temperatur[zone.Schluessel];
                List<string> klassen = zone.Raeume.GroupBy(Nutzungsklasse)
                    .Select(gr => (Klasse: gr.Key, A: gr.Sum(r => r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 0.0), N: gr.Count()))
                    .OrderByDescending(k => k.A).ThenByDescending(k => k.N).ThenBy(k => k.Klasse, StringComparer.Ordinal)
                    .Take(3).Select(k => NutzungText(k.Klasse)).ToList();
                string kopf = t.HasValue
                    ? t.Value.ToString("0", CultureInfo.InvariantCulture) + " °C"
                    : Ressource("GIMP_ZONE_OHNE_SOLLWERT") ?? "ohne Sollwert";
                zone.Name = kopf + " – " + string.Join(", ", klassen);
                string partner = zone.Schluessel.Substring(0, zone.Schluessel.Length - 1) + "B";
                if (!zone.IstBeheizt && jeSchluessel.ContainsKey(partner))
                    zone.Name += " " + (Ressource("GIMP_ZONE_UNBEHEIZT") ?? "(unbeheizt)");
            }
            Zonen.AddRange(zonen.OrderBy(z => _z6Temperatur[z.Schluessel].HasValue ? 0 : 1)
                                .ThenByDescending(z => _z6Temperatur[z.Schluessel] ?? 0.0)
                                .ThenBy(z => z.IstBeheizt ? 0 : 1)
                                .ThenBy(z => z.Name, StringComparer.Ordinal));
            ZuordnungNeu();
        }

        /// <summary>
        /// Die Zone gleicher Beheizung mit der nächstliegenden Solltemperatur zu Zone <paramref name="i"/> (M8 unter Z6, vor
        /// der größten gemeinsamen Grenzfläche); bei Gleichstand der Temperatur die mit der größeren gemeinsamen Fläche
        /// (<paramref name="nachbarn"/>), dann die größere Zone, dann die frühere. Ohne Sollwert auf einer Seite gilt der
        /// Abstand als unendlich. −1 = keine.
        /// </summary>
        private int NaechsteTemperatur(int i, IReadOnlyDictionary<int, double> nachbarn)
        {
            double? ti = _z6Temperatur.TryGetValue(Zonen[i].Schluessel, out double? a) ? a : null;
            return Enumerable.Range(0, Zonen.Count)
                .Where(j => j != i && Zonen[j].IstBeheizt == Zonen[i].IstBeheizt)
                .Select(j =>
                {
                    double? tj = _z6Temperatur.TryGetValue(Zonen[j].Schluessel, out double? b) ? b : null;
                    double abstand = ti.HasValue && tj.HasValue ? Math.Abs(ti.Value - tj.Value) : double.MaxValue;
                    return (J: j, Abstand: abstand, Gemeinsam: nachbarn.TryGetValue(j, out double g) ? g : 0.0, Flaeche: Zonen[j].FlaecheM2 ?? 0.0);
                })
                .OrderBy(k => k.Abstand).ThenByDescending(k => k.Gemeinsam).ThenByDescending(k => k.Flaeche).ThenBy(k => k.J)
                .Select(k => k.J).DefaultIfEmpty(-1).First();
        }

        /// <summary>Ein Text der Ressourcen in der Kultur der Ressourcen; <c>null</c> = keiner.</summary>
        private static string Ressource(string schluessel)
        {
            try
            {
                string text = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture);
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        //  Mindestgröße (M8)
        // ------------------------------------------------------------------

        /// <summary>Der Zuschlag nach M8: die kleinste Zone unter der Mindestgröße an ihren Nachbarn, bis keine mehr geht.</summary>
        private void Zuschlagen()
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
                    if (Regel == IfcImportProfil.ZONENREGEL_Z6)
                    {
                        // Z6: stets zur Zone gleicher Beheizung mit der nächstliegenden Solltemperatur — vor der Grenzfläche.
                        int naechste = NaechsteTemperatur(i, nachbarn);
                        if (naechste < 0) continue;
                        klein = i;
                        ziel = naechste;
                        gemeinsam = nachbarn.TryGetValue(naechste, out double g) ? g : 0.0;
                        break;
                    }
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
        }

        /// <summary>
        /// <b>Eine unbeheizte Zone ohne jede Fläche entfällt</b> (Mehrzonenkonzept 6.5): keine Hülle, keine Trennfläche, keine
        /// innere Masse — der Bauteilweg rechnet keine Zone ohne Bauteil, und sie koppelt an nichts. Ihre Räume bleiben
        /// außerhalb der Zonen (unbeheizt), benannt (<see cref="ZONE_OHNE_FLAECHEN"/>, I). Das tritt ohne Raumgrenzen auf, wenn
        /// die Datei die Hüllbauteile eines unbeheizten Raums nicht zur Hülle zählt und kein Bezug ihn mit einem beheizten
        /// Raum verbindet. Eine von Hand gebildete Zone bleibt.
        /// </summary>
        private void OhneFlaechenEntfallen()
        {
            bool entfallen = false;
            for (int i = Zonen.Count - 1; i >= 0; i--)
            {
                if (Zonen[i].IstBeheizt || Zonen[i].Handgeaendert) continue;
                if (Flaechen.Any(f => f.Zone == i || (f.Rand == Zonenrand.Zone && f.Nachbarzone == i))) continue;
                Melden(PruefStufe.Info, ZONE_OHNE_FLAECHEN, Zonen[i].Name, Zahl(Zonen[i].FlaecheM2 ?? 0.0));
                Zonen.RemoveAt(i);
                entfallen = true;
            }
            if (!entfallen) return;
            ZuordnungNeu();
            Seiten(melden: false);
        }

        /// <summary>
        /// Die Zonen unter der Mindestgröße, die bleiben: benannt — eine von Hand veränderte als Warnung (sie
        /// wird nicht zugeschlagen, der Anwender hat sie so gewollt), die übrigen wie im Vorschlag.
        /// </summary>
        private void KleineMelden()
        {
            foreach (Importzone zone in Zonen.Where(x => x.FlaecheM2 < MindestflaecheM2))
            {
                zone.ZuKlein = true;
                if (zone.Handgeaendert)
                    Melden(PruefStufe.Warnung, ZONE_ZU_KLEIN_HAND, zone.Name, Zahl(zone.FlaecheM2 ?? 0.0), Zahl(MindestflaecheM2));
                else
                    Melden(PruefStufe.Info, ZONE_ZU_KLEIN, zone.Name, Zahl(zone.FlaecheM2 ?? 0.0), Zahl(MindestflaecheM2));
            }
        }

        // ------------------------------------------------------------------
        //  Zuordnung von Hand (Welle D)
        // ------------------------------------------------------------------

        /// <summary>
        /// Legt die Zuordnungen von Hand in ihrer Reihenfolge auf den Vorschlag: ein Raum geht in eine Zone
        /// gleicher Beheizung (in Dateireihenfolge eingereiht) oder als eigene Zone ab (angehängt); eine leer
        /// gewordene Zone entfällt. Was nicht geht — ein unbekannter Raum, eine unbekannte Zielzone, eine
        /// Zielzone anderer Beheizung —, bleibt benannt ungetan. Eine Zuordnung, die nichts ändert (der Raum
        /// liegt schon dort, oder er ist allein und soll abgetrennt werden), bleibt still.
        /// </summary>
        private void HandzuordnungAnwenden()
        {
            foreach (Raumumhaengung u in Umhaengungen)
            {
                int von = ZoneVon(u.Raum);
                AbbildRaum r = von >= 0 ? Zonen[von].Raeume.FirstOrDefault(x => string.Equals(x.Kennung, u.Raum, StringComparison.Ordinal)) : null;
                if (r == null)
                {
                    Melden(PruefStufe.Warnung, UMHAENGEN_RAUM_UNBEKANNT, u.Raum ?? "");
                    continue;
                }
                Importzone quelle = Zonen[von];
                string raumname = string.IsNullOrWhiteSpace(r.Name) ? r.Kennung : r.Name.Trim();
                if (string.IsNullOrEmpty(u.Zielzone))
                {
                    if (quelle.Raeume.Count == 1) continue;
                    string schluessel = HAND_PRAEFIX + r.Kennung + "|" + (quelle.IstBeheizt ? "B" : "U");
                    string frei = schluessel;
                    for (int n = 2; Zonen.Any(x => string.Equals(x.Schluessel, frei, StringComparison.Ordinal)); n++)
                        frei = schluessel + "|" + n.ToString(CultureInfo.InvariantCulture);
                    var neu = new Importzone
                    {
                        Schluessel = frei, Name = raumname, IstBeheizt = quelle.IstBeheizt, Quellkennung = r.Kennung, Handgeaendert = true,
                    };
                    quelle.Raeume.Remove(r);
                    neu.Raeume.Add(r);
                    Zonen.Add(neu);
                    Melden(PruefStufe.Info, RAUM_ABGETRENNT, raumname, quelle.Name);
                }
                else
                {
                    int ziel = Zonen.FindIndex(x => string.Equals(x.Schluessel, u.Zielzone, StringComparison.Ordinal));
                    if (ziel < 0)
                    {
                        Melden(PruefStufe.Warnung, UMHAENGEN_ZONE_UNBEKANNT, raumname, u.Zielzone);
                        continue;
                    }
                    if (ziel == von) continue;
                    Importzone zone = Zonen[ziel];
                    if (zone.IstBeheizt != quelle.IstBeheizt)
                    {
                        Melden(PruefStufe.Warnung, UMHAENGEN_BEHEIZUNG, raumname, zone.Name);
                        continue;
                    }
                    quelle.Raeume.Remove(r);
                    Einreihen(zone.Raeume, r);
                    zone.Handgeaendert = true;
                    Melden(PruefStufe.Info, RAUM_UMGEHAENGT, raumname, quelle.Name, zone.Name);
                }
                quelle.Handgeaendert = true;
                if (quelle.Raeume.Count == 0)
                {
                    Zonen.Remove(quelle);
                    Melden(PruefStufe.Info, ZONE_ENTFALLEN, quelle.Name);
                }
                ZuordnungNeu();
                Handzuordnung = true;
            }
        }

        /// <summary>Reiht einen Raum nach der Dateireihenfolge vor dem ersten späteren Raum der Liste ein, sonst ans Ende.</summary>
        private void Einreihen(List<AbbildRaum> raeume, AbbildRaum r)
        {
            int stelle = Gebaeude.Raeume.IndexOf(r);
            int vor = raeume.FindIndex(x => Gebaeude.Raeume.IndexOf(x) > stelle);
            if (vor < 0) raeume.Add(r);
            else raeume.Insert(vor, r);
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
