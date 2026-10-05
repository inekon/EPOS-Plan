using System;
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
        /// <summary>
        /// Aus der Lage (Regel B5, Mehrzonenkonzept 6.1): ein Raum im Untergeschoss ohne Grenze gegen
        /// Außenluft — angenommen unbeheizt.
        /// </summary>
        Lage = 3,
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

        /// <summary>
        /// Die Postleitzahl des Standorts — nur der Export (G7a) setzt sie, aus der freiwilligen Eingabe
        /// des Exportdialogs (gespeichert wird sie nicht); <c>null</c> = keine, dann entfällt
        /// <c>Location</c> ganz. Der Leser lässt sie leer und legt die Ortsangabe in <see cref="Ort"/> ab.
        /// </summary>
        public string Plz { get; set; }

        /// <summary>
        /// Die Kennung des <c>Campus</c>, die der Export schreibt (<c>epos-campus-&lt;Geb.ID&gt;</c>,
        /// G7a); der Leser lässt sie leer.
        /// </summary>
        public string CampusKennung { get; set; }
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
        /// ohne ihn der Baujahr-Rückfall des Lesers, erste vierstellige Zahl, <see cref="Baujahrregel"/>); <c>null</c> = keines. Die Zuordnung leitet
        /// daraus die Baualtersklasse ab, wenn der Anwender keine vorgibt.
        /// </summary>
        public int? Baujahr { get; set; }

        /// <summary>Der Text, aus dem <see cref="Baujahr"/> gezogen ist, wie gelesen; <c>null</c> = keiner.</summary>
        public string BaujahrText { get; set; }

        /// <summary>
        /// Die Baualtersklasse des Gebäudes (Kürzel aus <c>Tab_Gebaeude.Baualtersklasse</c>; Export G7c, IFC
        /// <c>EPOS_Gebaeude.Baualtersklasse</c>); <c>null</c> = keine. Der Leser lässt sie leer.
        /// </summary>
        public string Baualtersklasse { get; set; }

        /// <summary>
        /// Die Jahresergebnisse des Gebäudes aus dem letzten Rechenlauf (Export G7c, IFC <c>EPOS_Ergebnis</c> am
        /// <c>IfcBuilding</c>); <c>null</c> = kein Rechenlauf. Der Leser lässt es leer.
        /// </summary>
        public AbbildErgebnis Ergebnis { get; set; }

        /// <summary>Die Räume des Gebäudes.</summary>
        public List<AbbildRaum> Raeume { get; } = new List<AbbildRaum>();

        /// <summary>
        /// Die Geschosse des Gebäudes in Dateireihenfolge (IFC: <c>IfcBuildingStorey</c>); gbXML führt
        /// keine. Die Zuordnung braucht sie nur für den Rückfall der Dachfläche (Grundfläche des
        /// obersten Geschosses, Umsetzungskonzept 3.4).
        /// </summary>
        public List<AbbildGeschoss> Geschosse { get; } = new List<AbbildGeschoss>();

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

        /// <summary>
        /// Zahl der Raumgrenzen der Bauteile dieses Gebäudes (IFC: <c>IfcRelSpaceBoundary</c>); gbXML
        /// führt keine eigenen Grenzen und lässt 0 — dort trägt jede Fläche ihre Räume selbst
        /// (<see cref="AbbildBauteil.Nachbarn"/>).
        /// </summary>
        public int ZahlGrenzen { get; set; }

        /// <summary>Davon die der 2. Ebene (IFC; <c>IfcRelSpaceBoundary2ndLevel</c> oder nach Name/Beschreibung).</summary>
        public int ZahlGrenzenZweiteEbene { get; set; }

        /// <summary>
        /// Zahl der Trenndecken, die der IFC-Leser ohne Raumgrenzen aus den Raumbezügen
        /// (<c>IfcRelReferencedInSpatialStructure</c>) bildet — Decken und Dächer innen, die Räume zweier
        /// Geschosse referenzieren (Mehrzonenkonzept 6.5). Mit ihnen sind die Geschosszonen (Z4) gekoppelt;
        /// gbXML führt 0.
        /// </summary>
        public int ZahlTrenndeckenReferenz { get; set; }

        /// <summary>
        /// Koppeln die Trenndecken aus den Raumbezügen (<see cref="ZahlTrenndeckenReferenz"/>) alle Geschosse mit
        /// beheizten Räumen zu einem Verbund (auch über ein unbeheiztes Geschoss)? Ein Geschosspaar trägt, wenn seine
        /// Trenndeckenfläche mindestens <see cref="IfcAbbildBauer.TRENNDECKE_ANTEIL_MIN"/> der beheizten Grundfläche des
        /// kleineren Geschosses erreicht — nach der Schätzung aus den Raummengen (unvollständige Bezüge) gilt das für jedes
        /// Paar mit beheizten Räumen auf beiden Seiten. Dann ist die Geschosszonierung (Z4) ohne Raumgrenzen die Vorgabe (M7); sonst
        /// bleibt eine Zone vorgegeben (6.5).
        /// </summary>
        public bool GeschosseGekoppelt { get; set; }

        /// <summary>
        /// Die Flächenpaare der Raumkörper (<see cref="Koerpernachbarschaft"/>): gezählt immer, gebildet nur ohne
        /// Raumgrenzen (<see cref="KoerperpaareGebildet"/>) — Rangfolge Raumgrenzen vor Körpern vor Raumbezügen.
        /// </summary>
        public int ZahlKoerperpaare { get; set; }

        /// <summary>Summe der Trennwände aus den Körperpaaren [m²].</summary>
        public double KoerperTrennwandM2 { get; set; }

        /// <summary>Summe der Trenndecken aus den Körperpaaren [m²].</summary>
        public double KoerperTrenndeckeM2 { get; set; }

        /// <summary>Tragen Trennflächen aus den Körperpaaren die Nachbarschaft der Räume (Gebäude ohne Raumgrenzen)?</summary>
        public bool KoerperpaareGebildet { get; set; }

        /// <summary>Die Zonenregel, die der Leser vorschlüge (<c>X1</c>, <c>X2</c>, <c>X4</c>); gewählt wird in G4c immer X4.</summary>
        public string Zonenvorschlag { get; set; } = GebaeudeImportProfil.ZONENREGEL_X4;

        /// <summary>
        /// Der Beschreibungstext, den der Export in <c>Campus/Description</c> schreibt (Produktausweis,
        /// Wasserzeichen der Testlizenz, Vermerke; G7a); <c>null</c> = keiner. Der Leser lässt ihn leer.
        /// </summary>
        public string Beschreibung { get; set; }
    }

    /// <summary>Ein Geschoss des Abbilds (IFC: <c>IfcBuildingStorey</c>).</summary>
    internal sealed class AbbildGeschoss
    {
        /// <summary>Kennung aus der Datei — dieselbe, die <see cref="AbbildRaum.GeschossKennung"/> nennt.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>
        /// Höhenlage [m] — nur für die Reihenfolge, nie als absoluter Wert (Umsetzungskonzept 3.5
        /// Nr. 2); <c>null</c> = unbekannt.
        /// </summary>
        public double? LageM { get; set; }

        /// <summary>Bruttogrundfläche [m²], wie die Datei sie angibt (IFC: <c>Qto_BuildingStoreyBaseQuantities.GrossFloorArea</c>); <c>null</c> = keine.</summary>
        public double? GrundflaecheM2 { get; set; }

        /// <summary>Was Belege nennen: der Name, sonst die Kennung.</summary>
        public string Anzeigename => string.IsNullOrWhiteSpace(Name) ? Kennung : Name;
    }

    /// <summary>
    /// Die Jahresergebnisse einer Zone für <c>Results</c> im gbXML-Export (Stufe G7b, Datenaustauschkonzept 5.6);
    /// jede Größe <c>null</c> = keine.
    /// </summary>
    internal sealed class AbbildErgebnis
    {
        /// <summary>Jahresheizwärme [kWh].</summary>
        public double? EnergieKWh { get; set; }

        /// <summary>Heizlast [W].</summary>
        public double? HeizlastW { get; set; }

        /// <summary>Mittlere Raumlufttemperatur des Jahres [°C].</summary>
        public double? MitteltemperaturC { get; set; }

        /// <summary>
        /// Jahreskältebedarf [kWh] — sensibel, ohne Entfeuchtung (Kühlkonzept 9.2); <c>null</c> = die Kühlung ist
        /// nicht gerechnet.
        /// </summary>
        public double? KaeltebedarfKWh { get; set; }

        /// <summary>Kältelast (Spitze) [W]; <c>null</c> = nicht gerechnet oder nicht gespeichert.</summary>
        public double? KaeltelastW { get; set; }

        /// <summary>Beginn des Rechenjahrs (<c>startTime</c>).</summary>
        public DateTime Beginn { get; set; }

        /// <summary>Der Zeitpunkt des Rechenlaufs (<c>Tab_Ergebnis.Zeitstempel</c>); <c>null</c> = unbekannt. Nur am Gebäude.</summary>
        public DateTime? Rechenzeitpunkt { get; set; }

        /// <summary>Der Wetterdatensatz des Rechenlaufs (Klimaregion des Laufs); <c>null</c> = unbekannt. Nur am Gebäude.</summary>
        public string Wetterdatensatz { get; set; }

        /// <summary>Trägt das Ergebnis eine Kältegröße?</summary>
        public bool MitKaelte => KaeltebedarfKWh.HasValue || KaeltelastW.HasValue;
    }

    /// <summary>Ein Raum des Abbilds.</summary>
    internal sealed class AbbildRaum
    {
        /// <summary>Die Jahresergebnisse der Zone dieses Raums (nur Export); <c>null</c> = keine.</summary>
        public AbbildErgebnis Ergebnis { get; set; }

        /// <summary>Kennung aus der Datei.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Typ der Quellentität für <c>Tab_Importzuordnung.Quelltyp</c> (gbXML <c>Space</c>, IFC <c>IfcSpace</c>).</summary>
        public string Quelltyp { get; set; } = "Space";

        /// <summary>Name aus der Datei; <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Fläche [m²]; <c>null</c> = nicht gelesen.</summary>
        public double? FlaecheM2 { get; set; }

        /// <summary>
        /// Stammt <see cref="FlaecheM2"/> aus dem Grundriss (<see cref="GrundrissM"/>, Gaußsche Trapezformel), weil die Datei
        /// für den Raum keine Flächenmenge führt (IFC, Mehrzonenkonzept 6.5)? <c>false</c> = aus der Datei oder keine.
        /// </summary>
        public bool FlaecheAusGrundriss { get; set; }

        /// <summary>Volumen [m³]; <c>null</c> = nicht gelesen.</summary>
        public double? VolumenM3 { get; set; }

        /// <summary>
        /// Lichte Raumhöhe [m], wie die Datei sie angibt (IFC: <c>Qto_SpaceBaseQuantities.Height</c>);
        /// <c>null</c> = nicht gelesen — dann gilt Volumen ÷ Fläche (Umsetzungskonzept 3.4).
        /// </summary>
        public double? HoeheM { get; set; }

        /// <summary>
        /// Der Körper des Raums aus der Datei (IFC: <c>IfcSpace</c>, Darstellung „Body“; Datenaustauschkonzept 15.3),
        /// formatfrei in Weltkoordinaten [m]; <c>null</c> = keiner. gbXML lässt ihn leer. Nur Anzeige.
        /// </summary>
        public Dateikoerper Koerper { get; set; }

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

        /// <summary>
        /// Ist eine Nachtabsenkung gesetzt (Nachtsollwert unter dem Tagessollwert; Export G7c, IFC
        /// <c>Pset_SpaceThermalRequirements.DiscontinuedHeating</c>)? <c>null</c> = unbekannt. Der Leser lässt es leer.
        /// </summary>
        public bool? Nachtabsenkung { get; set; }

        /// <summary>
        /// Luftwechsel durch Nutzerlüftung [1/h] (<c>Luftwechsel_Nutzer</c>; Export G7c, IFC
        /// <c>Pset_SpaceThermalRequirements.NaturalVentilationRate</c>); <c>null</c> = keiner. Der Leser lässt ihn leer.
        /// </summary>
        public double? LuftwechselNutzerJeH { get; set; }

        /// <summary>
        /// Die Konditionierung der Zone dieses Raums — Nutzung, Matrixzellen und Kalender (IFC <c>EPOS_Zone</c> und
        /// <c>EPOS_Kalender_*</c>, Datenaustauschkonzept 6.3 und 16.3); <c>null</c> = keine. Der Export setzt sie je Zone
        /// (nicht auf dem Klassenweg), der IFC-Leser nimmt sie aus einer Datei mit diesen Sätzen zurück.
        /// </summary>
        public AbbildKonditionierung Konditionierung { get; set; }

        /// <summary>Kennung des Geschosses; <c>null</c> = keine.</summary>
        public string GeschossKennung { get; set; }

        /// <summary>Name des Geschosses (gbXML <c>BuildingStorey/Name</c>); <c>null</c> = keiner — IFC führt die Geschosse selbst.</summary>
        public string GeschossName { get; set; }

        /// <summary>
        /// Höhenlage des Geschosses [m] (gbXML <c>BuildingStorey/Level</c>), nur für die Reihenfolge der
        /// Geschosse im Grundriss (Stufe G6c, Welle D); <c>null</c> = keine — IFC führt sie am Geschoss
        /// (<see cref="AbbildGeschoss.LageM"/>).
        /// </summary>
        public double? GeschossLageM { get; set; }

        /// <summary>
        /// Der Grundriss des Raums in der waagerechten Projektion [m] (IFC: Profilring der senkrechten Extrusion seiner
        /// Körperdarstellung, <see cref="IfcRaumgrundriss"/>; je Punkt x, y in Weltkoordinaten, ohne Schlusspunkt);
        /// <c>null</c> = keiner. Trägt ohne Raumgrenzen die Trenndecke zwischen übereinanderliegenden Räumen
        /// (Mehrzonenkonzept 6.5). gbXML setzt ihn nie.
        /// </summary>
        public IReadOnlyList<double[]> GrundrissM { get; set; }

        /// <summary>
        /// Kennung der Zone; <c>null</c> = keine (gbXML <c>@zoneIdRef</c>; IFC die oberste
        /// <c>IfcZone</c> bzw. <c>IfcSpatialZone</c> mit <c>THERMAL</c>, die den Raum fasst — Regel Z1).
        /// </summary>
        public string ZonenKennung { get; set; }

        /// <summary>Der Name der Zone aus <see cref="ZonenKennung"/> (IFC); <c>null</c> = keiner.</summary>
        public string ZonenName { get; set; }

        /// <summary>
        /// Liegt der Raum in mehreren Zonen der Datei? Dann gehört er im Vorschlag in keine
        /// (<see cref="ZonenKennung"/> bleibt leer; Mehrzonenkonzept 6.1).
        /// </summary>
        public bool ZoneMehrfach { get; set; }

        /// <summary>
        /// Die Klassifikation des Raums (IFC <c>IfcClassificationReference</c>) als „Quelle|Kennung";
        /// <c>null</c> = keine — Regel Z2.
        /// </summary>
        public string Klassifikation { get; set; }

        /// <summary>
        /// Die Beheizungsregel, nach der <see cref="Beheizt"/> gilt (IFC: <c>B1</c> … <c>B6</c>,
        /// Mehrzonenkonzept 6.1); <c>null</c> = das Format nummeriert nicht (gbXML).
        /// </summary>
        public string Beheizungsregel { get; set; }

        /// <summary>Der Beschreibungstext, den der Export in <c>Space/Description</c> schreibt (G7a); <c>null</c> = keiner. Der Leser lässt ihn leer.</summary>
        public string Beschreibung { get; set; }

        /// <summary>
        /// Der Beschreibungstext der Zone (<see cref="ZonenKennung"/>), den der Export in
        /// <c>Zone/Description</c> schreibt (Produktausweis je Zone, G7a); <c>null</c> = keiner. Der
        /// Leser lässt ihn leer.
        /// </summary>
        public string ZonenBeschreibung { get; set; }

        /// <summary>
        /// Die Raumtemperatur eines CAD-Exports [°C] (IFC <c>InsideTemperature (°C)</c> aus einem beliebigen Satz) —
        /// für die Zonenregel Z6, wenn <see cref="SollHeizenC"/> fehlt, und als Heizsollwert nur auf Wunsch des Anwenders
        /// (<see cref="GebaeudeCadSollwert"/>, Schalter im Zuordnungsdialog, Vorgabe aus). <c>null</c> = keine.
        /// <see cref="Raumtyp"/> trägt unter IFC <c>HSETU_RaumAllgemein.RoomType</c> ohne Präfix <c>mrt</c>, sonst
        /// <c>Pset_SpaceCommon.Category</c> bzw. <c>ObjectType</c> — er benennt die Nutzungsklasse der Regel Z6.
        /// </summary>
        public double? RaumtemperaturC { get; set; }
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

        /// <summary>
        /// Die Randbedingung der ersten Seite (IFC <c>ElementReferences[0].AdjacentType</c> eines CAD-Exports, Konzept
        /// HottCAD-Verbund 3.1); <see cref="Randbedingung.Innen"/> = beheizt; <c>null</c> = keine Angabe oder <c>btaNone</c>.
        /// </summary>
        public Randbedingung? RandbedingungSeiteA { get; set; }

        /// <summary>Die Randbedingung der zweiten Seite (<c>ElementReferences[1].AdjacentType</c>); sonst wie <see cref="RandbedingungSeiteA"/>.</summary>
        public Randbedingung? RandbedingungSeiteB { get; set; }

        /// <summary>Die Orientierung der ersten Seite [°], 0 = Nord, im Uhrzeigersinn (<c>ElementReferences[0].Orientation (°)</c>); <c>null</c> = keine.</summary>
        public double? OrientierungSeiteA { get; set; }

        /// <summary>Die Orientierung der zweiten Seite [°] (<c>ElementReferences[1].Orientation (°)</c>); <c>null</c> = keine.</summary>
        public double? OrientierungSeiteB { get; set; }

        /// <summary>
        /// <b>Die wirksame Randbedingung aus den beiden Seiten</b>: die nicht beheizte Seite; beide beheizt →
        /// <see cref="Randbedingung.Innen"/>; <c>null</c> = die Datei führt keine Seiten (dann gilt <see cref="Randbedingung"/>).
        /// </summary>
        public Randbedingung? RandbedingungWirksam { get; set; }

        /// <summary>Der Beleg der wirksamen Seite: <see cref="BELEG_KELLERDECKE"/>, <see cref="BELEG_OBERSTE_DECKE"/> oder <c>null</c>.</summary>
        public string RandbedingungBeleg { get; set; }

        /// <summary>Beleg der Seite <c>btaCellarCeiling</c>: unbeheizt, weil darunter der Keller liegt.</summary>
        public const string BELEG_KELLERDECKE = "Kellerdecke";

        /// <summary>Beleg der Seite <c>btaUppermostStorey</c>: unbeheizt, weil darüber der Dachraum liegt.</summary>
        public const string BELEG_OBERSTE_DECKE = "oberste Geschossdecke";

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
        /// für ein Außenbauteil ohne Raumgrenze (<c>IsExternal = true</c> oder die Angrenzung eines
        /// CAD-Exports: außen, Erdreich, unbeheizt), das über die räumliche Struktur einem Gebäude
        /// zugeordnet ist und das die Datei nicht ausdrücklich aus der Hüllfläche nimmt; es zählt dann nach
        /// seiner <see cref="Randbedingung"/>. gbXML setzt es nie — dort hängt jede Fläche an ihren
        /// <c>AdjacentSpaceId</c>.
        /// </summary>
        public bool HuelleOhneNachbar { get; set; }

        /// <summary>
        /// Für ein Bauteil der Hülle ohne Nachbarraum gegen <see cref="Randbedingung.Unbeheizt"/> (IFC-Angrenzung
        /// eines CAD-Exports, Mehrzonenkonzept 6.5): Ist es für die Zone Boden (<c>true</c>, etwa die Kellerdecke)
        /// oder Decke (<c>false</c>, die oberste Geschossdecke)? <c>null</c> = nach der Bauteilart.
        /// </summary>
        public bool? ZonenbodenOhneNachbar { get; set; }

        /// <summary>
        /// Eine Innenwand ohne jeden Nachbarraum, die einseitig als innere Masse zählt (Mehrzonenkonzept 6.5): Der
        /// IFC-Leser setzt es für eine Wand innen (<c>IsExternal = FALSE</c> oder die Angrenzung „beheizt" eines
        /// CAD-Exports) in einem Gebäude ohne Raumgrenzen, der weder Raumgrenzen noch Raumbezüge einen Nachbarn
        /// geben. Sie zählt mit ihrer Fläche einmal (eine Seite) zur inneren Masse — in der Zonierung in der Zone
        /// ihres Geschosses. gbXML setzt es nie.
        /// </summary>
        public bool InnenEinseitig { get; set; }

        /// <summary>
        /// Woraus die Nachbarn einer Trenndecke ohne Raumgrenzen stammen (Mehrzonenkonzept 6.5): <see cref="TRENNDECKE_BEZUG"/>,
        /// <see cref="TRENNDECKE_GESCHOSS"/> oder <see cref="TRENNDECKE_GRUNDRISS"/>; <c>null</c> = keine solche Trenndecke.
        /// </summary>
        public string Trenndeckenherkunft { get; set; }

        /// <summary>Die Raumbezüge der Datei (<c>IfcRelReferencedInSpatialStructure</c>) nennen beide Räume.</summary>
        public const string TRENNDECKE_BEZUG = "BEZUG";

        /// <summary>Die Decke steht im Geschoss eines von zwei übereinanderliegenden Geschossen; die Räume ohne Grundriss.</summary>
        public const string TRENNDECKE_GESCHOSS = "GESCHOSS";

        /// <summary>Die Grundrisse der beiden Räume überdecken sich; die Fläche ist die Überlappung.</summary>
        public const string TRENNDECKE_GRUNDRISS = "GRUNDRISS";

        /// <summary>Trenndecke aus einem Flächenpaar der Raumkörper (<see cref="Koerpernachbarschaft"/>).</summary>
        public const string TRENNDECKE_KOERPER = "KOERPER";

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

        /// <summary>
        /// Der Rahmenanteil [0–1] einer Öffnung aus der Datei (IFC <c>HSETU_EcoCad.FractionOfFrame</c>, Konzept HottCAD-Verbund
        /// 3.3); <c>null</c> = keiner, dann gilt die Vorgabe des Gebäudes. Der g-Wert bleibt davon unberührt.
        /// </summary>
        public double? Rahmenanteil { get; set; }

        /// <summary>Die Herkunft des <see cref="Rahmenanteil"/>s (<see cref="Importherkunft.Ifc"/>, wenn gelesen).</summary>
        public Importherkunft RahmenanteilHerkunft { get; set; } = Importherkunft.Leer;

        /// <summary>Der Beleg des <see cref="Rahmenanteil"/>s: Satz, Eigenschaft und Skala der Datei.</summary>
        public string RahmenanteilBeleg { get; set; }

        /// <summary>
        /// Die längenbezogenen Wärmebrücken des Bauteils als Summe ψ·L [W/K] (<c>Tab_Bauteil.Psi_L</c>; Export G7c,
        /// IFC <c>EPOS_Bauteil.WaermebrueckeUA</c>); <c>null</c> = keine. Der Leser lässt sie leer.
        /// </summary>
        public double? WaermebrueckeWK { get; set; }

        /// <summary>
        /// Breite [m] des Rechtecks, das der Export schreibt (<c>RectangularGeometry/Width</c>, G7a);
        /// Breite × <see cref="HoeheM"/> ist die <see cref="BruttoflaecheM2"/>. Der Leser lässt sie leer.
        /// </summary>
        public double? BreiteM { get; set; }

        /// <summary>Höhe [m] des Rechtecks, das der Export schreibt (<c>RectangularGeometry/Height</c>, G7a); der Leser lässt sie leer.</summary>
        public double? HoeheM { get; set; }

        /// <summary>
        /// Die Kennung des Fenstertyps einer Öffnung, den der Export schreibt
        /// (<c>epos-fenstertyp-&lt;Bauteil.ID&gt;</c> mit U- und g-Wert, G7a); <c>null</c> = keiner — dann
        /// stehen U und g an der Öffnung selbst (Tür). Der Leser lässt sie leer.
        /// </summary>
        public string FenstertypKennung { get; set; }

        /// <summary>Der Aufbau; <c>null</c> = keiner.</summary>
        public AbbildAufbau Aufbau { get; set; }

        /// <summary>Die Öffnungen der Fläche (Fenster, Türen).</summary>
        public List<AbbildBauteil> Oeffnungen { get; } = new List<AbbildBauteil>();

        /// <summary>
        /// Die Raumgrenzen des Bauteils (IFC <c>IfcRelSpaceBoundary</c>, die der 2. Ebene, wenn es welche
        /// gibt) — je Raum und Seite eine; leer = keine gelesen (gbXML: die Räume stehen in
        /// <see cref="Nachbarn"/>). Die Zonierung (Stufe G6c) ordnet über sie jede Seite ihrer Zone zu.
        /// </summary>
        public List<AbbildGrenze> Grenzen { get; } = new List<AbbildGrenze>();

        /// <summary>
        /// Die Dicke des Bauteils [m] (IFC: <c>Width</c> bzw. <c>Depth</c> der Mengen, sonst die Summe der
        /// Schichtdicken); <c>null</c> = keine. Grenze der Paarbildung über die Geometrie (6.2).
        /// </summary>
        public double? DickeM { get; set; }

        /// <summary>Das Geschoss, das das Bauteil enthält (IFC <c>IfcRelContainedInSpatialStructure</c>); <c>null</c> = keines.</summary>
        public string GeschossKennung { get; set; }

        /// <summary>
        /// Der Randpunktring der Fläche in Weltkoordinaten [m] (gbXML <c>PlanarGeometry/PolyLoop</c>; je Punkt
        /// x, y, z), in der Reihenfolge der Datei, ohne doppelte Folgepunkte und Schlusspunkt; <c>null</c> =
        /// keiner, nicht lesbar oder nicht eben (mehr als 1 mm neben der Ebene). Die Fläche selbst kommt
        /// weiter aus <see cref="BruttoflaecheM2"/>; der Ring trägt allein die Zonengeometrie (Stufe G6c,
        /// Welle D). IFC führt die Ringe an den Raumgrenzen (<see cref="AbbildGrenze.RandpunkteM"/>).
        /// </summary>
        public IReadOnlyList<double[]> RandpunkteM { get; set; }

        /// <summary>Meldungen zu genau diesem Bauteil (Geometrie, Aufbau, Verweise).</summary>
        public List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();
    }

    /// <summary>
    /// <b>Eine Raumgrenze</b> (IFC <c>IfcRelSpaceBoundary</c>): die Seite eines Bauteils, die ein Raum
    /// sieht, samt Fläche und Lage in Weltkoordinaten, soweit die Datei eine auswertbare Geometrie trägt
    /// (Mehrzonenkonzept 6.2). Einheiten SI.
    /// </summary>
    internal sealed class AbbildGrenze
    {
        /// <summary>Kennung aus der Datei (<c>GlobalId</c>).</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Der Raum dieser Seite; <c>null</c> = kein Raum (<c>IfcExternalSpatialElement</c> oder leer).</summary>
        public string RaumKennung { get; set; }

        /// <summary>
        /// Die Lage aus <c>InternalOrExternalBoundary</c>: <see cref="Randbedingung.Aussenluft"/>
        /// (<c>EXTERNAL</c>), <see cref="Randbedingung.Erdreich"/> (<c>EXTERNAL_EARTH</c>),
        /// <see cref="Randbedingung.Innen"/> (<c>INTERNAL</c>), sonst <see cref="Randbedingung.Unbekannt"/>.
        /// </summary>
        public Randbedingung Lage { get; set; } = Randbedingung.Unbekannt;

        /// <summary>Eine virtuelle Grenze (<c>VIRTUAL</c>) — keine Bauteilfläche, sondern eine Luftverbindung.</summary>
        public bool Virtuell { get; set; }

        /// <summary>Fläche des Außenrands [m²]; <c>null</c> = keine auswertbare Geometrie.</summary>
        public double? FlaecheM2 { get; set; }

        /// <summary>Summe der Innenränder derselben Ebene [m²] — schon ausgeschnittene Öffnungen (6.2).</summary>
        public double AusschnittM2 { get; set; }

        /// <summary>Flächenschwerpunkt in Weltkoordinaten [m]; <c>null</c> = keine Geometrie.</summary>
        public double[] SchwerpunktM { get; set; }

        /// <summary>Einheitsnormale in Weltkoordinaten (vom Raum weg); <c>null</c> = keine.</summary>
        public double[] Normale { get; set; }

        /// <summary>
        /// Der Randpunktring des Außenrands in Weltkoordinaten [m] (je Punkt x, y, z), in der Reihenfolge der
        /// Datei, ohne doppelte Folgepunkte und Schlusspunkt — derselbe Ring, aus dem
        /// <see cref="FlaecheM2"/>, <see cref="SchwerpunktM"/> und <see cref="Normale"/> folgen, also eben auf
        /// 1 mm; <c>null</c> = keine auswertbare Geometrie oder keine Platzierung des Raums (Stufe G6c, Welle D).
        /// </summary>
        public IReadOnlyList<double[]> RandpunkteM { get; set; }

        /// <summary>Woher die Grenze stammt: eine Raumgrenze der Datei oder ein Flächenpaar der Raumkörper.</summary>
        public Grenzherkunft Herkunft { get; set; } = Grenzherkunft.Raumgrenze;

        /// <summary>Die Kennung der Gegengrenze aus der Datei (<c>CorrespondingBoundary</c>); <c>null</c> = keine.</summary>
        public string GegenstueckKennung { get; set; }

        /// <summary>Der Typ einer Geometrie, die sich nicht auswerten lässt; <c>null</c> = ausgewertet oder keine.</summary>
        public string Geometriefehler { get; set; }
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

        /// <summary>Der Beschreibungstext, den der Export in <c>Construction/Description</c> schreibt (G7a); <c>null</c> = keiner. Der Leser lässt ihn leer.</summary>
        public string Beschreibung { get; set; }

        /// <summary>
        /// Ist der Aufbau eine gekennzeichnete Ersatzschichtung, die der Export für ein Bauteil ohne
        /// Schichten bildet (Datenaustauschkonzept 5.3, D10)? Nur der Export setzt es; der Leser lässt es aus.
        /// </summary>
        public bool IstErsatz { get; set; }
    }

    /// <summary>Eine Schicht eines Aufbaus, Stoffwerte in SI.</summary>
    internal sealed class AbbildSchicht
    {
        /// <summary>Kennung des Baustoffs (gbXML <c>Material/@id</c>).</summary>
        public string BaustoffKennung { get; set; } = "";

        /// <summary>
        /// Kennung der Schicht (gbXML <c>Layer/@id</c>), die der Export schreibt (G7a); <c>null</c> =
        /// keine. Der Leser lässt sie leer — er folgt dem Verweis auf den Baustoff.
        /// </summary>
        public string Kennung { get; set; }

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

    /// <summary>Ein Konditionierungskalender einer Zone mit seiner Bemerkung (Herkunft und Vermerk, ≤ 200 Zeichen).</summary>
    internal sealed record AbbildKalender(Konditionierungskalender Kalender, string Bemerkung);

    /// <summary>
    /// <b>Die Konditionierung einer Zone im Abbild</b> (Datenaustauschkonzept 6.3, 16.3): die Nutzung, die vier Matrixzellen,
    /// wie die Zone sie rechnet (Heizsollwert Tag und Nacht, Kühlsollwert, Luftwechsel der Nutzer), und die angelegten Kalender
    /// je Größe. Formatfrei; der IFC-Export schreibt sie als <c>EPOS_Zone</c> und <c>EPOS_Kalender_&lt;Größe&gt;</c>, der
    /// IFC-Leser liest sie zurück und gibt sie über <see cref="AlsZonenkonditionierung"/> an den Zonenplan.
    /// </summary>
    internal sealed class AbbildKonditionierung
    {
        /// <summary>Die Nutzung (<c>WOHNEN</c>, <c>BUERO</c>, <c>SCHULE</c>); <c>null</c> = keine.</summary>
        public string Nutzung { get; set; }

        /// <summary>Heizsollwert Tag [°C] — die Zelle HEIZSOLL/TAG.</summary>
        public double? HeizsollTagC { get; set; }

        /// <summary>Heizsollwert Nacht [°C] — die Zelle HEIZSOLL/NACHT.</summary>
        public double? HeizsollNachtC { get; set; }

        /// <summary>Kühlsollwert [°C] — die Zelle KUEHLSOLL/TAG.</summary>
        public double? KuehlsollC { get; set; }

        /// <summary>Luftwechsel der Nutzer [1/h] — die Zelle LUEFTUNG/TAG.</summary>
        public double? LuftwechselNutzerJeH { get; set; }

        /// <summary>Die Kalender in der Reihenfolge der Größen (<see cref="Konditionierungsgroessen.Alle"/>).</summary>
        public List<AbbildKalender> Kalender { get; } = new List<AbbildKalender>();

        /// <summary>Der Kalender einer Größe; <c>null</c> = keiner.</summary>
        internal AbbildKalender KalenderVon(Konditionierungsgroesse g) => Kalender.Find(k => k.Kalender.Groesse == g);

        /// <summary>Trägt die Zone etwas — eine Zelle, eine Nutzung oder einen Kalender?</summary>
        internal bool Traegt => Nutzung != null || HeizsollTagC.HasValue || HeizsollNachtC.HasValue || KuehlsollC.HasValue
                                || LuftwechselNutzerJeH.HasValue || Kalender.Count > 0;

        /// <summary>
        /// <b>Die Konditionierung für den Zonenplan</b> — derselbe Schreibweg wie die Projektdatei
        /// (<see cref="ZonenplanCtrl.ProjektdateiUebernehmen"/>): je Größe die Zellen als Vorgaben und der Kalender mit
        /// Herkunft <see cref="Konditionierungsherkunft.IfcDatei"/>; die Bemerkung nennt die Herkunft „aus IFC-Datei (EPOS)“
        /// und den mitgereisten Vermerk.
        /// </summary>
        internal Zonenkonditionierung AlsZonenkonditionierung(string zone)
        {
            var groessen = new List<Groessenkonditionierung>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                var vorgaben = new List<Vorgabebeleg>();
                void Zelle(string zeile, double? wert)
                {
                    if (wert is double w && double.IsFinite(w))
                        vorgaben.Add(new Vorgabebeleg(zeile, Matrixzelle.AusWert(w), null));
                }
                if (g == Konditionierungsgroesse.Heizsoll)
                {
                    Zelle(DbWerte.KOND_ZEILE_TAG, HeizsollTagC);
                    Zelle(DbWerte.KOND_ZEILE_NACHT, HeizsollNachtC);
                }
                if (g == Konditionierungsgroesse.Kuehlsoll) Zelle(DbWerte.KOND_ZEILE_TAG, KuehlsollC);
                if (g == Konditionierungsgroesse.Lueftung) Zelle(DbWerte.KOND_ZEILE_TAG, LuftwechselNutzerJeH);
                AbbildKalender k = KalenderVon(g);
                groessen.Add(new Groessenkonditionierung
                {
                    Groesse = g,
                    Herkunft = k != null || vorgaben.Count > 0 ? Konditionierungsherkunft.IfcDatei : Konditionierungsherkunft.Vorlage,
                    Kalender = k?.Kalender,
                    BemerkungText = k == null ? null : IfcKonditionierungssatz.Herkunftsbemerkung(k.Bemerkung),
                    Vorgaben = vorgaben,
                });
            }
            return new Zonenkonditionierung { Zone = zone ?? "", Nutzung = Nutzung, Groessen = groessen };
        }
    }
}
