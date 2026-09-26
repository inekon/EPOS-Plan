using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Spalte der Umkehrtabelle: der gespeicherte Wert von <c>Tab_Bauteil.Randbedingung</c>.</summary>
    internal enum Umkehrspalte
    {
        /// <summary><c>AUSSENLUFT</c>.</summary>
        Aussenluft = 0,
        /// <summary><c>ERDREICH</c>.</summary>
        Erdreich = 1,
        /// <summary><c>UNBEHEIZT</c>.</summary>
        Unbeheizt = 2,
        /// <summary><c>ZONE</c> — eine Trennfläche zur Nachbarzone (<c>ID_Nachbarzone</c>).</summary>
        Zone = 3,
        /// <summary>NULL — „innerhalb der Zone" an Innenwand und Decke, sonst Außenluft (<see cref="GebaeudeZonenabbildung.LeerHeisstInnen"/>).</summary>
        Leer = 4,
    }

    /// <summary>Wie die Nachbarn einer Fläche geschrieben werden (<c>AdjacentSpaceId</c>).</summary>
    internal enum Umkehrnachbarn
    {
        /// <summary>Keine — die Zelle ist abgelehnt.</summary>
        Keine = 0,
        /// <summary>Einmal der eigene Raum (Außenluft, Erdreich).</summary>
        EigenerRaum = 1,
        /// <summary>Erst der eigene Raum, dann der Platzhalter-<c>Space</c> „unbeheizt" des Gebäudes.</summary>
        MitPlatzhalter = 2,
        /// <summary>Zweimal der eigene Raum — innere Masse derselben Zone.</summary>
        InnenBeidseitig = 3,
        /// <summary>Keine eigene Fläche: eine <c>Opening</c> im Wirt (gleiche Zone, gleiche Randbedingung).</summary>
        ImWirt = 4,
        /// <summary>Erst der eigene Raum, dann der Raum der Nachbarzone — eine Trennfläche zwischen Zonen.</summary>
        Nachbarzone = 5,
    }

    /// <summary>Was vom Rückimport zu erwarten ist.</summary>
    internal enum Umkehrergebnis
    {
        /// <summary>Bauteilart und wirksame Randbedingung kommen gleich zurück.</summary>
        Gleich = 0,
        /// <summary>Ein benannter Wechsel der Bauteilart oder der Randbedingung (<see cref="Umkehrzelle.Grund"/>).</summary>
        Wechsel = 1,
        /// <summary>Nicht exportierbar — benannt abgelehnt (<see cref="Umkehrzelle.Grund"/>).</summary>
        Ablehnung = 2,
    }

    /// <summary>
    /// Eine Zelle der Umkehrtabelle: eine EPOS-Zeile (Bauteilart, gespeicherte Randbedingung,
    /// Wärmestromrichtung aus der wirksamen Neigung), ihr Zielwert in gbXML und die erwartete Rückkehr.
    /// </summary>
    internal sealed class Umkehrzelle
    {
        internal Umkehrzelle(Bauteilart art, Umkehrspalte spalte, Waermestromrichtung richtung, Bauteilrand rand,
                             string flaechenart, string oeffnungsart, Umkehrnachbarn nachbarn,
                             Bauteilart? rueckArt, Bauteilrand? rueckRand, string grund)
        {
            Art = art;
            Spalte = spalte;
            Richtung = richtung;
            Rand = rand;
            Flaechenart = flaechenart;
            Oeffnungsart = oeffnungsart;
            Nachbarn = nachbarn;
            RueckArt = rueckArt;
            RueckRand = rueckRand;
            Grund = grund;
            Ergebnis = nachbarn == Umkehrnachbarn.Keine ? Umkehrergebnis.Ablehnung
                     : rueckArt == art && rueckRand == rand ? Umkehrergebnis.Gleich
                     : Umkehrergebnis.Wechsel;
            Sicht = SichtFuer(flaechenart);
            GegenSicht = nachbarn == Umkehrnachbarn.InnenBeidseitig || nachbarn == Umkehrnachbarn.Nachbarzone
                ? Gegenstueck(Sicht) : null;
        }

        /// <summary>Die EPOS-Bauteilart.</summary>
        internal Bauteilart Art { get; }

        /// <summary>Die gespeicherte Randbedingung.</summary>
        internal Umkehrspalte Spalte { get; }

        /// <summary>Die Wärmestromrichtung aus der wirksamen Neigung.</summary>
        internal Waermestromrichtung Richtung { get; }

        /// <summary>Die wirksame Randbedingung (<see cref="GebaeudeZonenabbildung.RandAusZeile(Bauteilart, string)"/>).</summary>
        internal Bauteilrand Rand { get; }

        /// <summary>Die Flächenart (<c>Surface/@surfaceType</c>); <c>null</c> bei einer Öffnung und bei Ablehnung.</summary>
        internal string Flaechenart { get; }

        /// <summary>Die Öffnungsart (<c>Opening/@openingType</c>); <c>null</c> bei einer Fläche und bei Ablehnung.</summary>
        internal string Oeffnungsart { get; }

        /// <summary>Wie die Nachbarn geschrieben werden.</summary>
        internal Umkehrnachbarn Nachbarn { get; }

        /// <summary>
        /// Die Sicht des eigenen Raums (<c>AdjacentSpaceId/@surfaceType</c> des ersten Nachbarn): die
        /// Flächenart, wenn sie Boden oder Decke benennt; bei Wänden und Öffnungen <c>null</c>.
        /// </summary>
        internal string Sicht { get; }

        /// <summary>Die Sicht der Gegenseite bei innerer Masse und an einer Trennfläche zur Nachbarzone (zweiter Eintrag); sonst <c>null</c>.</summary>
        internal string GegenSicht { get; }

        /// <summary>Die erwartete Bauteilart nach dem Rückimport; <c>null</c> bei Ablehnung.</summary>
        internal Bauteilart? RueckArt { get; }

        /// <summary>Die erwartete wirksame Randbedingung nach dem Rückimport; <c>null</c> bei Ablehnung.</summary>
        internal Bauteilrand? RueckRand { get; }

        /// <summary>Gleich, benannter Wechsel oder Ablehnung — folgt aus der erwarteten Rückkehr.</summary>
        internal Umkehrergebnis Ergebnis { get; }

        /// <summary>Der Name des Wechsels bzw. der Ablehnung (<c>GbxmlUmkehrung.GRUND_*</c>); <c>null</c> bei „gleich".</summary>
        internal string Grund { get; }

        /// <summary>Kurzfassung für Tests und Protokolle.</summary>
        public override string ToString()
            => Art + "/" + Spalte + "/" + Richtung + " → " + (Flaechenart ?? Oeffnungsart ?? "—") + " " + Nachbarn
               + " ⇒ " + (RueckArt?.ToString() ?? "—") + "/" + (RueckRand?.ToString() ?? "—") + " " + Ergebnis
               + (Grund == null ? "" : " (" + Grund + ")");

        private static string SichtFuer(string flaechenart)
            => flaechenart != null && GebaeudeAggregation.SichtIstBoden(flaechenart).HasValue ? flaechenart : null;

        private static string Gegenstueck(string sicht)
        {
            switch (sicht)
            {
                case GbxmlVokabular.Ceiling: return GbxmlVokabular.InteriorFloor;
                case GbxmlVokabular.InteriorFloor: return GbxmlVokabular.Ceiling;
                default: return null;
            }
        }
    }

    /// <summary>
    /// <b>Die Umkehrtabelle des gbXML-Exports</b> (Stufe G7a; Datenaustauschkonzept 5.2 als Umkehrung
    /// der Tabelle 3.5) — vollständig: 9 Bauteilarten × 5 gespeicherte Randbedingungen
    /// (<see cref="Umkehrspalte"/>), je Zelle für die drei Wärmestromrichtungen der wirksamen Neigung.
    /// Jede Zelle nennt den Zielwert (Flächen- bzw. Öffnungsart, Nachbarn, Sicht) und die
    /// <b>erwartete Rückkehr</b> durch den eigenen Import (<see cref="GbxmlLeser"/>,
    /// <see cref="GebaeudeHuelleneinordnung"/>, <see cref="GebaeudeBauteilvorschlag"/>): gleich, ein
    /// benannter Wechsel oder eine Ablehnung. Die Rückkehr wird im Test am Leser gemessen, nicht
    /// angenommen.
    ///
    /// <para><b>Die Regel dahinter:</b> Die Flächenart ist die gbXML-übliche für die Lage des Bauteils,
    /// die Randbedingung tragen die Nachbarn — ein Nachbar (der eigene Raum) an Außenluft und Erdreich,
    /// zwei (eigener Raum und Platzhalter <c>Unconditioned</c>) gegen unbeheizt, zweimal der eigene Raum
    /// bei innerer Masse. Der eigene Raum steht immer zuerst.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Gegen unbeheizt</b> ist die Fläche eine Innenfläche zwischen zwei Räumen:
    /// <c>InteriorWall</c>, <c>Ceiling</c> bzw. <c>InteriorFloor</c>. Außenwand, Dach und Bodenplatte
    /// kehren deshalb als Innenwand bzw. Decke gegen unbeheizt zurück — die Randbedingung bleibt, die
    /// Art wechselt benannt (so ordnet auch der Import fremde Dateien ein).</item>
    /// <item><b>Innenwand an Außenluft oder Erdreich</b> wird Außenwand (<c>ExteriorWall</c>,
    /// <c>UndergroundWall</c>): Eine <c>InteriorWall</c> mit einem Nachbarn kehrte als „unbeheizt"
    /// zurück (<see cref="GebaeudeHuelleneinordnung"/>).</item>
    /// <item><b>Decke an Außenluft</b> wird <c>Roof</c> (Dach); <b>Dach an Erdreich</b>
    /// <c>UndergroundCeiling</c> (Decke).</item>
    /// <item><b>NULL</b> heißt an Innenwand und Decke „innerhalb der Zone", sonst Außenluft
    /// (<see cref="GebaeudeZonenabbildung.LeerHeisstInnen"/>); die Außenluft kommt dann ausdrücklich
    /// zurück — wirksam gleich.</item>
    /// <item><b>Sonstiges</b> nach der wirksamen Neigung: aufwärts wie Dach bzw. Decke, waagerecht wie
    /// Wand, abwärts wie Boden.</item>
    /// <item><b>Fenster, Tür, Vorhangfassade</b> sind Öffnungen im Wirt (<c>FixedWindow</c>,
    /// <c>NonSlidingDoor</c>); eine Vorhangfassade kehrt als Fenster zurück, ein Fenster an Erdreich
    /// rechnet der Import an Außenluft.</item>
    /// <item><b>Nachbarzone</b> (<c>ZONE</c>, Stufe G6b): Die Trennfläche ist eine Innenfläche nach
    /// der Neigung (<c>InteriorWall</c>, <c>Ceiling</c>, <c>InteriorFloor</c>) mit dem eigenen Raum und
    /// dem Raum der Nachbarzone. Der Import fasst die beheizten Räume zu EINER Zone zusammen (X4): Die
    /// Fläche kehrt als innere Masse zurück (Innenwand bzw. Decke, innerhalb der Zone) — benannt; gegen
    /// eine unbeheizte Nachbarzone kehrt sie gegen unbeheizt zurück. Eine Öffnung in einer Trennfläche
    /// wird abgelehnt; eine Zeile <c>ZONE</c> ohne Nachbarzone ebenso (der Ablauf prüft sie).</item>
    /// </list>
    /// </summary>
    internal static class GbxmlUmkehrung
    {
        // ------------------------------------------------------------------
        //  Die Namen der Wechsel und Ablehnungen
        // ------------------------------------------------------------------

        /// <summary>Ablehnung: Randbedingung Nachbarzone ohne Verweis auf die Zone (<c>ID_Nachbarzone</c> leer).</summary>
        internal const string GRUND_ZONE = "ZONE_OHNE_NACHBARZONE";

        /// <summary>Wechsel: Eine Trennfläche zur (beheizten) Nachbarzone kehrt als innere Masse zurück (Import X4, eine Zone).</summary>
        internal const string GRUND_ZONE_INNEN = "ZONE_ALS_INNERE_MASSE";

        /// <summary>Ablehnung: eine Öffnung (Fenster, Tür, Vorhangfassade) in einer Trennfläche zur Nachbarzone.</summary>
        internal const string GRUND_OEFFNUNG_ZONE = "OEFFNUNG_ZUR_NACHBARZONE";

        /// <summary>Wechsel: Außenwand gegen unbeheizt kehrt als Innenwand gegen unbeheizt zurück.</summary>
        internal const string GRUND_AUSSENWAND_UNBEHEIZT = "AUSSENWAND_UNBEHEIZT_ALS_INNENWAND";

        /// <summary>Wechsel: Dach an Erdreich oder gegen unbeheizt kehrt als Decke zurück.</summary>
        internal const string GRUND_DACH_ALS_DECKE = "DACH_ALS_DECKE";

        /// <summary>Wechsel: Bodenplatte gegen unbeheizt kehrt als Decke (Boden der Zone) zurück.</summary>
        internal const string GRUND_BODENPLATTE_UNBEHEIZT = "BODENPLATTE_UNBEHEIZT_ALS_DECKE";

        /// <summary>Wechsel: Innenwand an Außenluft oder Erdreich kehrt als Außenwand zurück.</summary>
        internal const string GRUND_INNENWAND_ALS_AUSSENWAND = "INNENWAND_ALS_AUSSENWAND";

        /// <summary>Wechsel: Decke an Außenluft kehrt als Dach zurück (<c>Roof</c>).</summary>
        internal const string GRUND_DECKE_ALS_DACH = "DECKE_ALS_DACH";

        /// <summary>Wechsel: Sonstiges kehrt nach der wirksamen Neigung als Dach, Decke, Wand oder Boden zurück.</summary>
        internal const string GRUND_SONSTIGES = "SONSTIGES_NACH_NEIGUNG";

        /// <summary>Wechsel: Vorhangfassade kehrt als Fenster zurück (<c>FixedWindow</c> im Wirt).</summary>
        internal const string GRUND_VORHANGFASSADE = "VORHANGFASSADE_ALS_FENSTER";

        /// <summary>Wechsel: Ein Fenster an Erdreich rechnet der Import an Außenluft.</summary>
        internal const string GRUND_FENSTER_ERDREICH = "FENSTER_ERDREICH_ALS_AUSSENLUFT";

        /// <summary>Die neun Bauteilarten in der Reihenfolge von <see cref="Bauteilart"/>.</summary>
        internal static readonly IReadOnlyList<Bauteilart> Arten = (Bauteilart[])Enum.GetValues(typeof(Bauteilart));

        /// <summary>Die fünf Spalten.</summary>
        internal static readonly IReadOnlyList<Umkehrspalte> Spalten = (Umkehrspalte[])Enum.GetValues(typeof(Umkehrspalte));

        /// <summary>Die drei Wärmestromrichtungen.</summary>
        internal static readonly IReadOnlyList<Waermestromrichtung> Richtungen = (Waermestromrichtung[])Enum.GetValues(typeof(Waermestromrichtung));

        /// <summary>Die ganze Tabelle: Art × Spalte × Richtung, in dieser Reihenfolge (9 × 5 × 3 = 135 Zellen).</summary>
        internal static readonly IReadOnlyList<Umkehrzelle> Tabelle = Bauen();

        private static readonly Dictionary<(Bauteilart, Umkehrspalte, Waermestromrichtung), Umkehrzelle> Index =
            Tabelle.ToDictionary(z => (z.Art, z.Spalte, z.Richtung));

        /// <summary>Die Zelle zu Art, Spalte und Wärmestromrichtung.</summary>
        internal static Umkehrzelle Zelle(Bauteilart art, Umkehrspalte spalte, Waermestromrichtung richtung)
            => Index[(art, spalte, richtung)];

        /// <summary>
        /// Die Zelle einer EPOS-Zeile: gespeicherte Randbedingung (NULL erlaubt) und wirksame Neigung [°].
        /// </summary>
        /// <exception cref="ArgumentException">Die Randbedingung ist kein bekannter Persistenzwert.</exception>
        /// <exception cref="GebaeudeModellException">Die Neigung liegt nicht in 0° … 180°.</exception>
        internal static Umkehrzelle Zelle(Bauteilart art, string randbedingung, double neigungGrad)
        {
            Umkehrspalte spalte = SpalteAus(randbedingung)
                ?? throw new ArgumentException("Unbekannte Randbedingung: " + randbedingung, nameof(randbedingung));
            return Zelle(art, spalte, Bauteilreduktion.RichtungAusNeigung(neigungGrad));
        }

        /// <summary>Die Spalte eines gespeicherten Werts; NULL → <see cref="Umkehrspalte.Leer"/>; <c>null</c> für einen unbekannten Wert.</summary>
        internal static Umkehrspalte? SpalteAus(string randbedingung)
        {
            switch (randbedingung)
            {
                case null: return Umkehrspalte.Leer;
                case DbWerte.RANDBEDINGUNG_AUSSENLUFT: return Umkehrspalte.Aussenluft;
                case DbWerte.RANDBEDINGUNG_ERDREICH: return Umkehrspalte.Erdreich;
                case DbWerte.RANDBEDINGUNG_UNBEHEIZT: return Umkehrspalte.Unbeheizt;
                case DbWerte.RANDBEDINGUNG_ZONE: return Umkehrspalte.Zone;
                default: return null;
            }
        }

        /// <summary>Der gespeicherte Wert einer Spalte; <see cref="Umkehrspalte.Leer"/> → <c>null</c>.</summary>
        internal static string Wert(Umkehrspalte spalte)
        {
            switch (spalte)
            {
                case Umkehrspalte.Aussenluft: return DbWerte.RANDBEDINGUNG_AUSSENLUFT;
                case Umkehrspalte.Erdreich: return DbWerte.RANDBEDINGUNG_ERDREICH;
                case Umkehrspalte.Unbeheizt: return DbWerte.RANDBEDINGUNG_UNBEHEIZT;
                case Umkehrspalte.Zone: return DbWerte.RANDBEDINGUNG_ZONE;
                default: return null;
            }
        }

        // ------------------------------------------------------------------
        //  Die Tabelle
        // ------------------------------------------------------------------

        private static List<Umkehrzelle> Bauen()
        {
            var zellen = new List<Umkehrzelle>();
            foreach (Bauteilart art in Arten)
                foreach (Umkehrspalte spalte in Spalten)
                    foreach (Waermestromrichtung richtung in Richtungen)
                    {
                        Bauteilrand rand = GebaeudeZonenabbildung.RandAusZeile(art, Wert(spalte)).Value;
                        Ziel z = spalte == Umkehrspalte.Zone ? Trennflaeche(art, richtung) : Regel(art, rand, richtung);
                        zellen.Add(new Umkehrzelle(art, spalte, richtung, rand, z.Flaechenart, z.Oeffnungsart, z.Nachbarn,
                                                   z.RueckArt, z.RueckRand, z.Grund));
                    }
            return zellen;
        }

        /// <summary>Der Zielwert einer Zeile mit wirksamer Randbedingung Außenluft, Erdreich, unbeheizt oder innen.</summary>
        private static Ziel Regel(Bauteilart art, Bauteilrand rand, Waermestromrichtung r)
        {
            const Bauteilrand AL = Bauteilrand.Aussenluft, ER = Bauteilrand.Erdreich, UB = Bauteilrand.Unbeheizt, IN = Bauteilrand.Innen;
            switch (art)
            {
                case Bauteilart.Aussenwand:
                    return rand == AL ? F(GbxmlVokabular.ExteriorWall, Umkehrnachbarn.EigenerRaum, Bauteilart.Aussenwand, AL)
                         : rand == ER ? F(GbxmlVokabular.UndergroundWall, Umkehrnachbarn.EigenerRaum, Bauteilart.Aussenwand, ER)
                         : F(GbxmlVokabular.InteriorWall, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Innenwand, UB, GRUND_AUSSENWAND_UNBEHEIZT);

                case Bauteilart.Dach:
                    return rand == AL ? F(GbxmlVokabular.Roof, Umkehrnachbarn.EigenerRaum, Bauteilart.Dach, AL)
                         : rand == ER ? F(GbxmlVokabular.UndergroundCeiling, Umkehrnachbarn.EigenerRaum, Bauteilart.Decke, ER, GRUND_DACH_ALS_DECKE)
                         : F(GbxmlVokabular.Ceiling, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Decke, UB, GRUND_DACH_ALS_DECKE);

                case Bauteilart.Bodenplatte:
                    return rand == AL ? F(GbxmlVokabular.ExposedFloor, Umkehrnachbarn.EigenerRaum, Bauteilart.Bodenplatte, AL)
                         : rand == ER ? F(GbxmlVokabular.SlabOnGrade, Umkehrnachbarn.EigenerRaum, Bauteilart.Bodenplatte, ER)
                         : F(GbxmlVokabular.InteriorFloor, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Decke, UB, GRUND_BODENPLATTE_UNBEHEIZT);

                case Bauteilart.Innenwand:
                    return rand == AL ? F(GbxmlVokabular.ExteriorWall, Umkehrnachbarn.EigenerRaum, Bauteilart.Aussenwand, AL, GRUND_INNENWAND_ALS_AUSSENWAND)
                         : rand == ER ? F(GbxmlVokabular.UndergroundWall, Umkehrnachbarn.EigenerRaum, Bauteilart.Aussenwand, ER, GRUND_INNENWAND_ALS_AUSSENWAND)
                         : rand == UB ? F(GbxmlVokabular.InteriorWall, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Innenwand, UB)
                         : F(GbxmlVokabular.InteriorWall, Umkehrnachbarn.InnenBeidseitig, Bauteilart.Innenwand, IN);

                case Bauteilart.Decke:
                {
                    string zwischen = r == Waermestromrichtung.Abwaerts ? GbxmlVokabular.InteriorFloor : GbxmlVokabular.Ceiling;
                    return rand == AL ? F(GbxmlVokabular.Roof, Umkehrnachbarn.EigenerRaum, Bauteilart.Dach, AL, GRUND_DECKE_ALS_DACH)
                         : rand == ER ? F(GbxmlVokabular.UndergroundCeiling, Umkehrnachbarn.EigenerRaum, Bauteilart.Decke, ER)
                         : rand == UB ? F(zwischen, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Decke, UB)
                         : F(zwischen, Umkehrnachbarn.InnenBeidseitig, Bauteilart.Decke, IN);
                }

                case Bauteilart.Sonstiges:
                    switch (r)
                    {
                        case Waermestromrichtung.Aufwaerts:
                            return rand == AL ? F(GbxmlVokabular.Roof, Umkehrnachbarn.EigenerRaum, Bauteilart.Dach, AL, GRUND_SONSTIGES)
                                 : rand == ER ? F(GbxmlVokabular.UndergroundCeiling, Umkehrnachbarn.EigenerRaum, Bauteilart.Decke, ER, GRUND_SONSTIGES)
                                 : F(GbxmlVokabular.Ceiling, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Decke, UB, GRUND_SONSTIGES);
                        case Waermestromrichtung.Abwaerts:
                            return rand == AL ? F(GbxmlVokabular.ExposedFloor, Umkehrnachbarn.EigenerRaum, Bauteilart.Bodenplatte, AL, GRUND_SONSTIGES)
                                 : rand == ER ? F(GbxmlVokabular.SlabOnGrade, Umkehrnachbarn.EigenerRaum, Bauteilart.Bodenplatte, ER, GRUND_SONSTIGES)
                                 : F(GbxmlVokabular.InteriorFloor, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Decke, UB, GRUND_SONSTIGES);
                        default:
                            return rand == AL ? F(GbxmlVokabular.ExteriorWall, Umkehrnachbarn.EigenerRaum, Bauteilart.Aussenwand, AL, GRUND_SONSTIGES)
                                 : rand == ER ? F(GbxmlVokabular.UndergroundWall, Umkehrnachbarn.EigenerRaum, Bauteilart.Aussenwand, ER, GRUND_SONSTIGES)
                                 : F(GbxmlVokabular.InteriorWall, Umkehrnachbarn.MitPlatzhalter, Bauteilart.Innenwand, UB, GRUND_SONSTIGES);
                    }

                case Bauteilart.Fenster:
                    return rand == ER ? O(GbxmlVokabular.FixedWindow, Bauteilart.Fenster, AL, GRUND_FENSTER_ERDREICH)
                         : O(GbxmlVokabular.FixedWindow, Bauteilart.Fenster, rand);

                case Bauteilart.Tuer:
                    return O(GbxmlVokabular.NonSlidingDoor, Bauteilart.Tuer, rand);

                case Bauteilart.Vorhangfassade:
                    return O(GbxmlVokabular.FixedWindow, Bauteilart.Fenster, rand == ER ? AL : rand, GRUND_VORHANGFASSADE);

                default:
                    throw new ArgumentOutOfRangeException(nameof(art), art, "Bauteilart ohne Umkehrregel.");
            }
        }

        /// <summary>
        /// Der Zielwert einer Trennfläche zur Nachbarzone: eine Innenfläche nach der Neigung mit dem Raum
        /// der Nachbarzone; zurück kommt sie als innere Masse (die Art aus der Flächenart). Öffnungen abgelehnt.
        /// </summary>
        private static Ziel Trennflaeche(Bauteilart art, Waermestromrichtung r)
        {
            if (art == Bauteilart.Fenster || art == Bauteilart.Tuer || art == Bauteilart.Vorhangfassade)
                return Ziel.Abgelehnt(GRUND_OEFFNUNG_ZONE);
            string flaechenart = r == Waermestromrichtung.Aufwaerts ? GbxmlVokabular.Ceiling
                               : r == Waermestromrichtung.Abwaerts ? GbxmlVokabular.InteriorFloor
                               : GbxmlVokabular.InteriorWall;
            Bauteilart rueck = r == Waermestromrichtung.Horizontal ? Bauteilart.Innenwand : Bauteilart.Decke;
            return F(flaechenart, Umkehrnachbarn.Nachbarzone, rueck, Bauteilrand.Innen, GRUND_ZONE_INNEN);
        }

        private static Ziel F(string flaechenart, Umkehrnachbarn nachbarn, Bauteilart rueckArt, Bauteilrand rueckRand, string grund = null)
            => new Ziel(flaechenart, null, nachbarn, rueckArt, rueckRand, grund);

        private static Ziel O(string oeffnungsart, Bauteilart rueckArt, Bauteilrand rueckRand, string grund = null)
            => new Ziel(null, oeffnungsart, Umkehrnachbarn.ImWirt, rueckArt, rueckRand, grund);

        private readonly struct Ziel
        {
            internal Ziel(string flaechenart, string oeffnungsart, Umkehrnachbarn nachbarn, Bauteilart? rueckArt,
                          Bauteilrand? rueckRand, string grund)
            {
                Flaechenart = flaechenart;
                Oeffnungsart = oeffnungsart;
                Nachbarn = nachbarn;
                RueckArt = rueckArt;
                RueckRand = rueckRand;
                Grund = grund;
            }

            internal static Ziel Abgelehnt(string grund) => new Ziel(null, null, Umkehrnachbarn.Keine, null, null, grund);

            internal string Flaechenart { get; }
            internal string Oeffnungsart { get; }
            internal Umkehrnachbarn Nachbarn { get; }
            internal Bauteilart? RueckArt { get; }
            internal Bauteilrand? RueckRand { get; }
            internal string Grund { get; }
        }
    }
}
