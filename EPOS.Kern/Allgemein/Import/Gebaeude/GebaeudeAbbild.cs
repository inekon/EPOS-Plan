using System.Collections.Generic;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // Die Bauteilart ist die EINE Kern-Aufzählung aus Simulation/Gebaeude/BauteilEingang.cs (G3):
    // dieselben neun Werte in derselben Reihenfolge; das Abbild führt keine zweite.

    /// <summary>Die Randbedingung eines Bauteils, wie sie aus Typ und Nachbarschaft folgt (3.5).</summary>
    internal enum Randbedingung
    {
        /// <summary>Außenluft.</summary>
        Aussenluft = 0,
        /// <summary>Erdreich.</summary>
        Erdreich = 1,
        /// <summary>Zwischen Räumen — entschieden wird erst über die Nachbarn.</summary>
        Innen = 2,
        /// <summary>Gegen einen unbeheizten Raum.</summary>
        Unbeheizt = 3,
        /// <summary>Nicht bestimmbar.</summary>
        Unbekannt = 4,
    }

    /// <summary>Wie vollständig ein Aufbau aus der Datei ist (Datenaustauschkonzept 3.6).</summary>
    internal enum Aufbaustatus
    {
        /// <summary>Jede Schicht mit Dicke, λ, ρ und c — U-Wert und Masse sind aus den Schichten bestimmbar.</summary>
        Vollstaendig = 0,
        /// <summary>Mindestens eine Schicht nur mit R-Wert (oder ohne ρ bzw. c) — U-Wert bestimmbar, Masse nicht (3.6, Punkt 2).</summary>
        Masselos = 1,
        /// <summary>Mindestens eine Schicht ohne Wärmedurchlasswiderstand — weder U-Wert noch Masse aus den Schichten.</summary>
        Unvollstaendig = 2,
        /// <summary>Kein Aufbau: Verweis fehlt, zeigt ins Leere oder der Aufbau hat keine Schicht.</summary>
        OhneAufbau = 3,
    }

    /// <summary>In welcher Richtung die Schichten eines Aufbaus gezählt sind.</summary>
    internal enum Schichtrichtung
    {
        /// <summary>Erste Schicht außen — die gbXML-Hausannahme (3.7).</summary>
        AussenNachInnen = 0,
        /// <summary>Erste Schicht innen — die Zählung von <c>Tab_Bauteilschicht.Reihenfolge</c>.</summary>
        InnenNachAussen = 1,
    }

    /// <summary>Woraus die Entscheidung „beheizt/unbeheizt" eines Raums stammt (3.3).</summary>
    internal enum BeheiztQuelle
    {
        /// <summary>Aus dem Attribut der Datei (gbXML <c>Space/@conditionType</c>).</summary>
        Attribut = 0,
        /// <summary>Aus der Namensregel (Keller, Garage, … — Umsetzungskonzept 3.5 Nr. 3).</summary>
        Name = 1,
        /// <summary>Weder Attribut noch Namenstreffer — angenommen beheizt.</summary>
        Annahme = 2,
    }

    /// <summary>
    /// <b>Das formatfreie, normierte Zwischenmodell eines Gebäudeimports</b> (Softwarearchitektur
    /// 1.2/1.5): was die Datei beschreibt, in SI-Einheiten, noch ohne EPOS-Semantik. Der Leser
    /// füllt es (<see cref="IGebaeudeLeser"/>), der Ablauf bildet daraus den Satz
    /// (<see cref="GebaeudeImportAblauf.Zuordnen"/>). Je Format eine Ableitung
    /// (<see cref="GbxmlAbbild"/>, später <c>IfcGebaeudeAbbild</c>), weil das Abbild die Datei
    /// spiegelt und mit ihr veraltet.
    ///
    /// <para><b>Einheiten stehen im Namen</b> (<c>…M2</c>, <c>…M3</c>, <c>…Grad</c>, <c>…WmK</c>):
    /// Hier ist alles bereits SI; die Umrechnung liegt beim Leser.</para>
    ///
    /// <para><b>Der Nordwinkel wird nie still angewandt</b> (3.2): Beim gbXML-Weg stehen die Azimute
    /// so im Abbild, wie die Datei sie schreibt. Beim IFC-Weg dreht der Leser sie nach
    /// Umsetzungskonzept 3.4 um <c>TrueNorth</c> bzw. die Drehung der <c>IfcMapConversion</c> —
    /// und sagt das mit einer Meldung; <see cref="NordwinkelGrad"/> nennt die angewandte Drehung.</para>
    /// </summary>
    internal class GebaeudeAbbild
    {
        /// <summary>
        /// Zahl der beim Lesen verlorenen Entitäten (IFC: Summe beider Verlustkanäle des Parsers,
        /// Datenaustauschkonzept 4, Ergänzung 3); gbXML kennt keinen Verlustkanal und führt 0. Der
        /// Ablauf legt die Zahl in <see cref="GebaeudeQuelle.FehlendeEntitaeten"/> ab.
        /// </summary>
        public int FehlendeEntitaeten { get; set; }

        /// <summary>Format des Abbilds (<see cref="GebaeudeQuelle.FORMAT_GBXML"/> bzw. <see cref="GebaeudeQuelle.FORMAT_IFC"/>).</summary>
        public string Format { get; set; } = "";

        /// <summary>Der gelesene Schemastand, wie er in der Datei steht; <c>null</c> = keiner.</summary>
        public string Schemastand { get; set; }

        /// <summary>Die Gebäude der Datei in Dateireihenfolge — eines je Lauf wird zugeordnet (U13).</summary>
        public List<AbbildGebaeude> Gebaeude { get; } = new List<AbbildGebaeude>();

        /// <summary>Bauteile, die sich keinem Gebäude zuordnen lassen (kein bekannter Nachbarraum) — sie zählen nirgends.</summary>
        public List<AbbildBauteil> BauteileOhneGebaeude { get; } = new List<AbbildBauteil>();

        /// <summary>Dateiweite Meldungen (Version, Einheiten, Nordrichtung, Aufbauten …).</summary>
        public List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Die Drehung des Modells gegen Nord [°], wie gelesen; <c>null</c> = die Datei sagt nichts (Annahme 0°).</summary>
        public double? NordwinkelGrad { get; set; }

        /// <summary>Ortsangabe der Datei, nur zur Anzeige.</summary>
        public string Ort { get; set; }

        /// <summary>Geografische Breite [°], nur zur Anzeige; die Klimaregion wählt der Anwender.</summary>
        public double? BreiteGrad { get; set; }

        /// <summary>Geografische Länge [°], nur zur Anzeige.</summary>
        public double? LaengeGrad { get; set; }
    }

    /// <summary>Ein Gebäude des Abbilds mit seinen Räumen und Bauteilen.</summary>
    internal sealed class AbbildGebaeude
    {
        /// <summary>Kennung aus der Datei, ungekürzt.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Gebäudeart der Datei (gbXML <c>@buildingType</c>), nur Anzeige.</summary>
        public string Art { get; set; }

        /// <summary>Typ der Quellentität für <c>Tab_Importzuordnung.Quelltyp</c> (gbXML <c>Building</c>, IFC <c>IfcBuilding</c>).</summary>
        public string Quelltyp { get; set; } = "Building";

        /// <summary>Was die Klappliste zeigt: der Name, sonst die Kennung.</summary>
        public string Anzeigename => string.IsNullOrWhiteSpace(Name) ? Kennung : Name;

        /// <summary>
        /// Das Baujahr, wie es aus der Datei gezogen ist (IFC: <c>Pset_BuildingCommon.YearOfConstruction</c>,
        /// erste vierstellige Zahl, <see cref="Baujahrregel"/>); <c>null</c> = keines. Die Zuordnung leitet
        /// daraus die Baualtersklasse ab, wenn der Anwender keine vorgibt.
        /// </summary>
        public int? Baujahr { get; set; }

        /// <summary>Der Text, aus dem <see cref="Baujahr"/> gezogen ist, wie gelesen; <c>null</c> = keiner.</summary>
        public string BaujahrText { get; set; }

        /// <summary>Die Räume des Gebäudes.</summary>
        public List<AbbildRaum> Raeume { get; } = new List<AbbildRaum>();

        /// <summary>
        /// Die Bauteile, die an mindestens einen Raum dieses Gebäudes grenzen; eine Trennwand zweier
        /// Gebäude steht in beiden.
        /// </summary>
        public List<AbbildBauteil> Bauteile { get; } = new List<AbbildBauteil>();

        /// <summary>Meldungen, die dieses Gebäude betreffen (Zonenvorschlag, Raumzuordnung …).</summary>
        public List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Zahl der verschiedenen Zonen, auf die die Räume verweisen.</summary>
        public int ZahlZonen { get; set; }

        /// <summary>Zahl der Geschosse, die Räume tragen.</summary>
        public int ZahlGeschosseMitRaeumen { get; set; }

        /// <summary>Die Zonenregel, die der Leser vorschlüge (<c>X1</c>, <c>X2</c>, <c>X4</c>); gewählt wird in G4c immer X4.</summary>
        public string Zonenvorschlag { get; set; } = GebaeudeImportProfil.ZONENREGEL_X4;
    }

    /// <summary>Ein Raum des Abbilds.</summary>
    internal sealed class AbbildRaum
    {
        /// <summary>Kennung aus der Datei.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Typ der Quellentität für <c>Tab_Importzuordnung.Quelltyp</c> (gbXML <c>Space</c>, IFC <c>IfcSpace</c>).</summary>
        public string Quelltyp { get; set; } = "Space";

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Fläche [m²]; <c>null</c> = nicht gelesen.</summary>
        public double? FlaecheM2 { get; set; }

        /// <summary>Volumen [m³]; <c>null</c> = nicht gelesen.</summary>
        public double? VolumenM3 { get; set; }

        /// <summary>
        /// Lichte Raumhöhe [m], wie die Datei sie angibt (IFC: <c>Qto_SpaceBaseQuantities.Height</c>);
        /// <c>null</c> = nicht gelesen — dann gilt Volumen ÷ Fläche (Umsetzungskonzept 3.4).
        /// </summary>
        public double? HoeheM { get; set; }

        /// <summary>Ist der Raum beheizt?</summary>
        public bool Beheizt { get; set; } = true;

        /// <summary>Woraus <see cref="Beheizt"/> folgt.</summary>
        public BeheiztQuelle BeheiztQuelle { get; set; } = BeheiztQuelle.Annahme;

        /// <summary>Der gelesene Nutzungszustand (gbXML <c>@conditionType</c>), wie er in der Datei steht.</summary>
        public string Zustandsangabe { get; set; }

        /// <summary>Raumtyp der Datei (gbXML <c>@spaceType</c>) — nur Hinweis, nirgends abgebildet (3.7).</summary>
        public string Raumtyp { get; set; }

        /// <summary>Luftwechsel [1/h]; <c>null</c> = nicht gelesen.</summary>
        public double? LuftwechselJeH { get; set; }

        /// <summary>Personenzahl; <c>null</c> = nicht gelesen oder in m² je Person angegeben.</summary>
        public double? Personen { get; set; }

        /// <summary>Fläche je Person [m²]; <c>null</c> = nicht gelesen oder als Personenzahl angegeben.</summary>
        public double? FlaecheJePersonM2 { get; set; }

        /// <summary>Beleuchtungsleistung je Fläche [W/m²] (Auslegungswert der Datei).</summary>
        public double? LichtWm2 { get; set; }

        /// <summary>Geräteleistung je Fläche [W/m²] (Auslegungswert der Datei).</summary>
        public double? GeraeteWm2 { get; set; }

        /// <summary>Heizsollwert [°C] (gbXML aus der Zone).</summary>
        public double? SollHeizenC { get; set; }

        /// <summary>Kühlsollwert der Auslegung [°C] (gbXML aus der Zone) — gelesen, aber keinem Zielfeld zugeordnet.</summary>
        public double? SollKuehlenC { get; set; }

        /// <summary>Kennung des Geschosses; <c>null</c> = keine.</summary>
        public string GeschossKennung { get; set; }

        /// <summary>Kennung der Zone; <c>null</c> = keine.</summary>
        public string ZonenKennung { get; set; }
    }

    /// <summary>Ein Nachbarraum eines Bauteils samt der Sicht dieses Raums auf die Fläche.</summary>
    internal sealed class AbbildNachbar
    {
        /// <summary>Legt einen Nachbarn an.</summary>
        public AbbildNachbar(string kennung, string sicht)
        {
            Kennung = kennung ?? "";
            Sicht = string.IsNullOrWhiteSpace(sicht) ? null : sicht.Trim();
        }

        /// <summary>Kennung des Raums, wie gelesen — sie kann ins Leere zeigen.</summary>
        public string Kennung { get; }

        /// <summary>Die Art der Fläche aus Sicht dieses Raums (gbXML <c>AdjacentSpaceId/@surfaceType</c>); <c>null</c> = keine Angabe.</summary>
        public string Sicht { get; }
    }

    /// <summary>
    /// Ein Bauteil des Abbilds — eine Fläche (gbXML <c>Surface</c>) oder eine Öffnung darin
    /// (<c>Opening</c>).
    /// </summary>
    internal sealed class AbbildBauteil
    {
        /// <summary>Kennung aus der Datei, ungekürzt.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Typ der Quellentität (<c>Surface</c>, <c>Opening</c> …).</summary>
        public string Quelltyp { get; set; } = "";

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Die Art, wie die Datei sie nennt (gbXML <c>@surfaceType</c> bzw. <c>@openingType</c>).</summary>
        public string Quellart { get; set; }

        /// <summary>Die normierte Bauteilart.</summary>
        public Bauteilart Art { get; set; }

        /// <summary>Die Randbedingung aus dem Typ; <see cref="Randbedingung.Innen"/> heißt „über die Nachbarn entscheiden".</summary>
        public Randbedingung Randbedingung { get; set; }

        /// <summary>Die angrenzenden Räume in Dateireihenfolge (0 bis 2).</summary>
        public List<AbbildNachbar> Nachbarn { get; } = new List<AbbildNachbar>();

        /// <summary>Bruttofläche [m²] einschließlich der Öffnungen; <c>null</c> = Geometrie fehlt.</summary>
        public double? BruttoflaecheM2 { get; set; }

        /// <summary>
        /// Nettofläche [m²], wie die Datei sie selbst angibt (IFC: <c>NetSideArea</c>); <c>null</c> = keine.
        /// Sie ist allein der Rückfall des Fensterabzugs (U14), wenn Brutto − Öffnungen negativ wird —
        /// die Fläche selbst kommt immer aus <see cref="BruttoflaecheM2"/>.
        /// </summary>
        public double? NettoflaecheM2 { get; set; }

        /// <summary>
        /// Gehört das Bauteil ohne jeden Nachbarraum zur Hülle seines Gebäudes? Der IFC-Leser setzt es
        /// für ein Außenbauteil ohne Raumgrenze (<c>IsExternal = true</c>), das über die räumliche Struktur
        /// einem Gebäude zugeordnet ist; es zählt dann nach seiner <see cref="Randbedingung"/>. gbXML setzt
        /// es nie — dort hängt jede Fläche an ihren <c>AdjacentSpaceId</c>.
        /// </summary>
        public bool HuelleOhneNachbar { get; set; }

        /// <summary>Azimut [°], 0 = Nord, im Uhrzeigersinn; <c>null</c> = unbestimmt (auch bei waagerechten Flächen).</summary>
        public double? AzimutGrad { get; set; }

        /// <summary>Neigung [°], 0 = waagerecht nach oben, 90 = senkrecht, 180 = waagerecht nach unten.</summary>
        public double? NeigungGrad { get; set; }

        /// <summary>Der U-Wert [W/(m²K)], den die Datei für dieses Bauteil einträgt; <c>null</c> = keiner.</summary>
        public double? UWertWm2K { get; set; }

        /// <summary>Woher <see cref="UWertWm2K"/> stammt (<c>Construction</c>, <c>Opening</c>, <c>WindowType</c>).</summary>
        public string UWertQuelle { get; set; }

        /// <summary>Gesamtenergiedurchlassgrad g [–] einer Öffnung; <c>null</c> = keiner.</summary>
        public double? GWert { get; set; }

        /// <summary>Der Aufbau; <c>null</c> = keiner.</summary>
        public AbbildAufbau Aufbau { get; set; }

        /// <summary>Die Öffnungen der Fläche (Fenster, Türen).</summary>
        public List<AbbildBauteil> Oeffnungen { get; } = new List<AbbildBauteil>();

        /// <summary>Meldungen zu genau diesem Bauteil (Geometrie, Aufbau, Verweise).</summary>
        public List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();
    }

    /// <summary>Ein Aufbau (gbXML <c>Construction</c>) mit seinen Schichten.</summary>
    internal sealed class AbbildAufbau
    {
        /// <summary>Kennung aus der Datei.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Der eingetragene U-Wert [W/(m²K)]; er hat Vorrang vor der Rechnung aus den Schichten.</summary>
        public double? UWertWm2K { get; set; }

        /// <summary>Die Schichten in der Richtung <see cref="Richtung"/>.</summary>
        public List<AbbildSchicht> Schichten { get; } = new List<AbbildSchicht>();

        /// <summary>Wie vollständig der Aufbau ist.</summary>
        public Aufbaustatus Status { get; set; } = Aufbaustatus.OhneAufbau;

        /// <summary>
        /// Die Zählrichtung von <see cref="Schichten"/>. Das Abbild lässt die Folge der Datei stehen;
        /// umgekehrt wird erst beim Schreiben in <c>Tab_Bauteilschicht</c> (innen → außen,
        /// Datenaustauschkonzept 3.4).
        /// </summary>
        public Schichtrichtung Richtung { get; set; } = Schichtrichtung.AussenNachInnen;

        /// <summary>Ist die Richtung nur angenommen (gbXML: „erste Schicht außen", 3.7) statt belegt?</summary>
        public bool RichtungAngenommen { get; set; } = true;
    }

    /// <summary>Eine Schicht eines Aufbaus, Stoffwerte in SI.</summary>
    internal sealed class AbbildSchicht
    {
        /// <summary>Kennung des Baustoffs (gbXML <c>Material/@id</c>).</summary>
        public string BaustoffKennung { get; set; } = "";

        /// <summary>Name des Baustoffs; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Dicke [m]; <c>null</c> = keine oder ungültig.</summary>
        public double? DickeM { get; set; }

        /// <summary>Wärmeleitfähigkeit λ [W/(mK)]; <c>null</c> = keine oder ≤ 0 (Fehlstelle).</summary>
        public double? LambdaWmK { get; set; }

        /// <summary>Rohdichte ρ [kg/m³]; <c>null</c> = keine oder ≤ 0 (Fehlstelle).</summary>
        public double? RhoKgM3 { get; set; }

        /// <summary>Spezifische Wärmekapazität c [J/(kgK)]; <c>null</c> = keine oder ≤ 0 (Fehlstelle).</summary>
        public double? CpJkgK { get; set; }

        /// <summary>Wärmedurchlasswiderstand R [m²K/W], wie eingetragen; <c>null</c> = keiner.</summary>
        public double? RWertM2KW { get; set; }

        /// <summary>Sind Dicke, λ, ρ und c vorhanden und größer null?</summary>
        public bool Vollstaendig => DickeM > 0.0 && LambdaWmK > 0.0 && RhoKgM3 > 0.0 && CpJkgK > 0.0;

        /// <summary>Trägt die Schicht einen Wärmedurchlasswiderstand — d/λ oder den eingetragenen R-Wert?</summary>
        public bool HatWiderstand => (DickeM > 0.0 && LambdaWmK > 0.0) || RWertM2KW > 0.0;

        /// <summary>Nur R-Wert, keine vollständigen Stoffwerte — die Schicht ist masselos (3.6, Punkt 2).</summary>
        public bool NurRWert => !Vollstaendig && RWertM2KW > 0.0 && !(DickeM > 0.0 && LambdaWmK > 0.0);
    }
}
