using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>Die Welle, die ein Inventardatum in einem Format trägt bzw. tragen soll (Konzept Datenaustausch 7).</summary>
    internal enum Traegerwelle
    {
        /// <summary>Schon vor den DA-Wellen getragen (Stufen G7 der Gebäudeübernahme).</summary>
        Bestand = 0,
        /// <summary>gbXML-Export und Rundlauf von Lasten und Zeitplänen (der Import kam mit DA-1).</summary>
        DA2 = 2,
        /// <summary>IFC-Standardsätze lesen und schreiben.</summary>
        DA3 = 3,
        /// <summary>Restdaten beider Formate: gbXML über den Rückfall Weg 2 (Block <c>EPOS:</c> in <c>Description</c>), IFC über
        /// <c>EPOS_Zone</c>/<c>EPOS_Gebaeude</c> — beide Richtungen.</summary>
        DA4 = 4,
        /// <summary>Nachzug N2 zu DA-4/DA-7: Höchsttemperatur (B10) über dieselben Restdaten, beide Richtungen — die der Zone (N2) und
        /// die des Gebäudes (Nachzug N4).</summary>
        N2 = 5,
        /// <summary>Kein Element und noch keine Welle im Auftrag (Rückfall nach Konzept 3.3 offen).</summary>
        Offen = 8,
        /// <summary>EPOS führt kein Feld (Konstante, Klimadatensatz oder nicht gerechnet) — es gibt nichts zu übertragen.</summary>
        KeinFeld = 9,
    }

    /// <summary>Die Formatklasse eines Datums: Standard, eigener Weg (Erweiterungspunkt bzw. <c>EPOS_*</c>), nicht übertragbar.</summary>
    internal enum Formatklasse
    {
        /// <summary>gbXML-Standardelement bzw. IFC-Standardsatz (auch nur in einer Richtung).</summary>
        Standard = 0,
        /// <summary>gbXML-Erweiterungspunkt bzw. IFC-Satz <c>EPOS_*</c> — für ein Fremdwerkzeug unsichtbar.</summary>
        Eigen = 1,
        /// <summary>Nicht übertragbar — mit Grund.</summary>
        NichtUebertragbar = 2,
    }

    /// <summary>Eine Zeile der Inventartafel.</summary>
    /// <param name="Nr">Die Kennung des Konzepts (A1 … G4).</param>
    /// <param name="WelleGbxml">Die Welle, die das Datum in gbXML trägt.</param>
    /// <param name="GrundGbxml">Warum gbXML das Datum nicht (oder nur lesend) trägt; leer, wenn es in beiden Richtungen geht.</param>
    /// <param name="WelleIfc">Die Welle, die das Datum in IFC trägt.</param>
    /// <param name="GrundIfc">Warum IFC das Datum nicht (oder nur lesend) trägt.</param>
    /// <param name="Felder">Die EPOS-Felder (<c>Tabelle.Spalte</c> bzw. <c>Modell.Feld</c>); leer, wenn EPOS kein Feld führt.</param>
    internal sealed record Inventarzeile(string Nr, Traegerwelle WelleGbxml, string GrundGbxml, Traegerwelle WelleIfc, string GrundIfc,
                                         params string[] Felder)
    {
        /// <summary>Der Weg in gbXML nach der Kerntafel <see cref="DatenaustauschInventar.Alle"/>.</summary>
        internal Austauschweg Gbxml => DatenaustauschInventar.Alle.Single(d => d.Nr == Nr).Gbxml;

        /// <summary>Der Weg in IFC nach der Kerntafel <see cref="DatenaustauschInventar.Ifc"/>.</summary>
        internal Austauschweg Ifc => DatenaustauschInventar.Ifc.Single(d => d.Nr == Nr).Weg;

        /// <summary>Die Bezeichnung des Konzepts.</summary>
        internal string Datum => DatenaustauschInventar.Alle.Single(d => d.Nr == Nr).Datum;
    }

    /// <summary>Eine benannte Ausnahme: Felder, die bewusst nicht zum Datenaustausch gehören, mit Grund.</summary>
    internal sealed record Austauschausnahme(string Grund, params string[] Felder);

    /// <summary>
    /// <b>Die Inventartafel des Datenaustauschs im Testprojekt</b> (Welle DA-7, E138 F5; Konzept Datenaustausch 2, 3.4 und 7).
    /// Sie ergänzt die Kerntafel <see cref="DatenaustauschInventar"/> (Weg je Format, Elemente) um das, was die Wache
    /// „jedes Feld eingestuft“ braucht: je Datum die EPOS-Felder (Tabelle und Spalte bzw. Modellfeld), die Welle je Format und
    /// den Grund, wenn ein Format das Datum nicht trägt; dazu die benannten Ausnahmen (Kennungen, Herkunft, Anzeige, Anlagen- und
    /// Bedarfsdaten, Summenfelder des Klassenwegs). Ein Feld einer Tabelle oder eines Modells, das weder hier noch in den
    /// Ausnahmen steht, macht <see cref="DatenaustauschRundlaufWacheTests"/> rot.
    /// </summary>
    internal static class Datenaustauschinventar
    {
        private const Traegerwelle BE = Traegerwelle.Bestand;
        private const Traegerwelle D2 = Traegerwelle.DA2;
        private const Traegerwelle D3 = Traegerwelle.DA3;
        private const Traegerwelle D4 = Traegerwelle.DA4;
        private const Traegerwelle N2 = Traegerwelle.N2;
        private const Traegerwelle OF = Traegerwelle.Offen;
        private const Traegerwelle KF = Traegerwelle.KeinFeld;

        private const string RUECKFALL = "kein Standardelement; Rückfall nach Konzept 3.3 nicht gebaut";
        private const string KEIN_FELD = "EPOS führt kein Feld";

        // Kurzformen der Felder.
        private static string Z(string s) => "Tab_Zone." + s;
        private static string B(string s) => "Tab_Bauteil." + s;
        private static string G(string s) => "Tab_Gebaeude." + s;
        private static string K(string s) => "Tab_Konditionierungskalender." + s;
        private static string P(string s) => "Tab_Konditionierungsperiode." + s;
        private static string A(string s) => "Tab_Bauteilaufbau." + s;
        private static string S(string s) => "Tab_Bauteilschicht." + s;
        private static string[] ZG(params string[] s) => s.Select(Z).Concat(s.Select(G)).ToArray();
        private static string[] F(params object[] teile) => teile.SelectMany(t => t is string[] a ? a : new[] { (string)t }).ToArray();

        /// <summary>Die 71 Daten in der Reihenfolge des Konzepts.</summary>
        internal static readonly IReadOnlyList<Inventarzeile> Tafel = new[]
        {
            new Inventarzeile("A1", BE, "nur gelesen: der Export schreibt die Lage nicht (Klimaregion statt Koordinaten)", D3, null,
                              "Tab_Klimaregion.Latitude", "Tab_Klimaregion.Longitude"),
            new Inventarzeile("A2", OF, "Location/Name nur als Hinweis; die Klimaregion ist ein EPOS-Katalog — " + RUECKFALL,
                              OF, "kein Standardsatz; EPOS_Gebaeude nicht erweitert", "Tab_Projekt.ID_Klimaregion", "Tab_Klimaregion.Klimazone_DIN4710"),
            new Inventarzeile("A3", KF, KEIN_FELD + " (Höhe steckt im Klimadatensatz)", KF, KEIN_FELD + " (Höhe steckt im Klimadatensatz)"),
            new Inventarzeile("A4", BE, null, BE, null, "Tab_Importquelle.Nordwinkel_Grad", "Tab_Importquelle.Nordwinkel_Herkunft"),
            new Inventarzeile("A5", OF, "kein Element in gbXML 6.01 — " + RUECKFALL, BE, null, G("Baujahr"), G("Baualtersklasse"), G("Energiestandard")),
            new Inventarzeile("A6", BE, null, D3, null, G("Gebaeudeart"), G("Wohngebaeude_Nicht_Wohngebaeude")),
            new Inventarzeile("A7", OF, RUECKFALL, OF, "kein Standardsatz; EPOS_Gebaeude nicht erweitert",
                              G("Bauweise"), G("Masseanteil_Aussen"), G("Innenflaechenfaktor")),
            new Inventarzeile("A8", OF, "nur im Kalender über dayType; als Feld " + RUECKFALL, OF, "kein Standardsatz; EPOS_Gebaeude nicht erweitert",
                              G("Wochenendtage")),
            new Inventarzeile("A9", OF, "dayType Holiday trägt keine Landesregel — " + RUECKFALL, OF, "kein Standardsatz; EPOS_Gebaeude nicht erweitert",
                              G("Feiertagsland")),
            new Inventarzeile("A10", D2, null, BE, null,
                              F(G("Ferien"), G("Ferienbeginn_1"), G("Ferienende_1"), G("Ferienbeginn_2"), G("Ferienende_2"), G("Ferienbeginn_3"),
                                G("Ferienende_3"), G("Ferienbeginn_4"), G("Ferienende_4"), P("Art"), P("Beginn"), P("Ende"))),

            new Inventarzeile("B1", BE, null, BE, null, Z("Bezeichner")),
            new Inventarzeile("B2", BE, null, BE, null, Z("Nutzflaeche"), G("Nutzflaeche")),
            new Inventarzeile("B3", BE, null, BE, null, Z("Volumen"), Z("Raumhoehe"), G("Raumhoehe")),
            new Inventarzeile("B4", BE, null, BE, null, Z("IstBeheizt")),
            new Inventarzeile("B5", BE, "nur gelesen: spaceType der Quelle, über die Raumtyptafel auf die DIN-Nummer abgebildet (DA-5); der Export schreibt keine Enumeration. IFC trägt die DIN-Nummer zusätzlich als EPOS_Zone.DinNummer (DA-5)", BE, null,
                              Z("Nutzungsprofil"), K("Nutzung")),
            new Inventarzeile("B6", BE, null, BE, null, ZG("Raumsolltemperatur_Tag")),
            new Inventarzeile("B7", D2, null, BE, null, F(ZG("Raumsolltemperatur_Nachtabsenkung"), G("Nachtabsenkung_Beginn"), G("Nachtabsenkung_Ende"))),
            new Inventarzeile("B8", D2, null, D3, null, F(ZG("Raumsolltemperatur_Wochenende", "Raumsolltemperatur_Ferien"), G("Wochenende"))),
            new Inventarzeile("B9", D2, null, D3, null, ZG("Kuehl_Sollwert", "Kuehl_Sollwert_Nacht", "Kuehlung_Aktiv")),
            new Inventarzeile("B10", N2, null, N2, null, ZG("Maximaleraumtemperatur")),
            new Inventarzeile("B11", D4, null, D4, null, ZG("Heizleistung_Max", "Kuehlleistung_Max")),
            new Inventarzeile("B12", BE, null, D3, null, F(ZG("Luftwechsel_Infiltration"), G("Luftwechselrate"))),
            new Inventarzeile("B13", D2, null, D3, null, ZG("Luftwechsel_Nutzer")),
            new Inventarzeile("B14", D2, null, D3, null, Z("Aussenluft_Person")),
            new Inventarzeile("B15", D2, null, D3, null, Z("Bewohner"), G("Flaeche_Nutzer")),
            new Inventarzeile("B16", D2, null, D3, null, Z("Personen_Waerme_W")),
            new Inventarzeile("B17", D2, null, D3, null, ZG("Interne_Waermegewinne")),
            new Inventarzeile("B18", D2, null, D3, null, Z("Licht_W_m2")),
            new Inventarzeile("B19", OF, RUECKFALL, OF, "kein Standardsatz; EPOS_Zone nicht erweitert", ZG("Heizung_Strahlungsanteil")),
            new Inventarzeile("B20", D4, null, D4, null, Z("Vorheizzeit_H"), G("Aufheizzeit_Manuell_H")),
            new Inventarzeile("B21", D4, null, D4, null, Z("Uebergabe_Art"), Z("Uebergabe_Exponent"), Z("Uebergabe_Leistung_Nenn"),
                              Z("Auslegung_Vorlauf"), Z("Auslegung_Ruecklauf"), Z("Auslegung_Raumtemperatur"), Z("Regler_Proportionalband")),
            new Inventarzeile("B22", D4, null, D4, null, Z("Kuehl_Uebergabe_Art"), Z("Kuehl_Uebergabe_Exponent"), Z("Kuehl_Uebergabe_Leistung_Nenn")),

            new Inventarzeile("C1", D2, null, BE, null, K("Groesse"), K("Wert"), K("Aus"), K("Woche")),
            new Inventarzeile("C2", D2, null, BE, null, K("Groesse"), K("Wert"), K("Aus"), K("Woche")),
            new Inventarzeile("C3", D2, null, BE, null, K("Groesse"), K("Wert"), K("Aus"), K("Woche"), G("Sommerlueftung")),
            new Inventarzeile("C4", D2, null, BE, null, K("Groesse"), K("Wert"), K("Aus"), K("Woche"), K("Nennwert")),
            new Inventarzeile("C5", D2, null, BE, null, K("Groesse"), K("Wert"), K("Aus"), K("Woche"), K("Nennwert")),
            new Inventarzeile("C6", D2, null, BE, null, P("Art"), P("Feiertagsregel")),
            new Inventarzeile("C7", D2, null, BE, null, P("Art"), P("Bezeichner"), P("Beginn"), P("Ende"), P("Wert"), P("Aus"), P("Woche"),
                              P("WieWochentag"), P("Gilt_Fuer")),
            new Inventarzeile("C8", D4, null, D4, null, "Tab_Konditionierungsvorgabe.Groesse", "Tab_Konditionierungsvorgabe.Zeile",
                              "Tab_Konditionierungsvorgabe.Wert", "Tab_Konditionierungsvorgabe.Aus", "Tab_Konditionierungsvorgabe.Von",
                              "Tab_Konditionierungsvorgabe.Bis", "Tab_Konditionierungsvorgabe.Bedingt_K"),
            new Inventarzeile("C9", D4, null, D4, null, "Tab_Konditionierungswoche.Groesse", "Tab_Konditionierungswoche.Name",
                              "Tab_Konditionierungswoche.Woche", P("ID_Woche")),
            new Inventarzeile("C10", KF, KEIN_FELD + " (Zeitplan steckt im Profil Beleuchtung_Anteil)", KF, KEIN_FELD + " (Zeitplan steckt im Profil Beleuchtung_Anteil)"),

            new Inventarzeile("D1", BE, null, BE, null, B("Bauteilart")),
            new Inventarzeile("D2", BE, null, BE, null, B("Flaeche")),
            new Inventarzeile("D3", BE, null, BE, null, B("Neigung")),
            new Inventarzeile("D4", BE, null, BE, null, B("Azimut")),
            new Inventarzeile("D5", BE, null, BE, null, B("Randbedingung")),
            new Inventarzeile("D6", BE, null, BE, null, B("ID_Nachbarzone"), B("Trennflaeche_Zuordnung")),
            new Inventarzeile("D7", BE, null, BE, null, B("U_Wert")),
            new Inventarzeile("D8", OF, RUECKFALL, D3, null, B("Psi_L"), G("WBVK_Anschluß_Fenster_Wand"), G("WBVK_Anschluß_Wand_Dach"),
                              G("WBVK_Anschluß_Außenwand_Kellerdecke"), G("Abmessung_Anschluß_Fenster_Wand"), G("Abmessung_Anschluß_Wand_Dach"),
                              "ProjektGebaeudeModel.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand",
                              "ProjektGebaeudeModel.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach",
                              "ProjektGebaeudeModel.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke",
                              "GebaeudeModel.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand",
                              "GebaeudeModel.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach",
                              "GebaeudeModel.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke"),

            new Inventarzeile("E1", BE, null, BE, null, B("ID_Aufbau"), S("Reihenfolge"), S("Dicke")),
            new Inventarzeile("E2", BE, null, BE, null, S("Lambda"), S("Lambda_Bemessung")),
            new Inventarzeile("E3", BE, null, BE, null, S("Rho")),
            new Inventarzeile("E4", BE, null, BE, null, S("cp")),
            new Inventarzeile("E5", BE, null, D3, null, S("IstLuftschicht"), S("R_Quelle"), A("R_Quelle")),
            new Inventarzeile("E6", OF, "kein Element; der Export schreibt die homogenisierte Ersatzschicht", OF,
                              "IfcMaterialConstituentSet nicht gebaut; der Export schreibt die Ersatzschicht",
                              S("ID_Abschnitt"), S("EN6946_Balkenbreite"), S("EN6946_Gefachbreite"), S("EN6946_Versatz"), S("EN6946_Drehung")),
            new Inventarzeile("E7", OF, "Luftfilmwiderstand an der Schemakopie 6.01 zu prüfen — " + RUECKFALL, OF,
                              "kein Standardsatz; EPOS_Bauteil nicht erweitert", A("Rsi"), A("Rse"), S("Rsi"), S("Rse")),

            new Inventarzeile("F1", BE, null, BE, null, B("U_Wert"), A("Ug"), A("Uf"), A("U_Quelle")),
            new Inventarzeile("F2", BE, null, BE, null, B("g_Wert"), A("g_Wert"), G("Fensterdurchlassgrad")),
            new Inventarzeile("F3", OF, "WindowType/Frame an der Schemakopie 6.01 zu prüfen — " + RUECKFALL, D3, null,
                              B("Rahmenanteil"), A("Rahmenanteil"), A("Rahmenbreite"), G("Rahmenanteil")),
            new Inventarzeile("F4", OF, "ShadingCoeff an der Schemakopie 6.01 zu prüfen — " + RUECKFALL, OF,
                              "Pset_DoorWindowShadingType nicht gebaut", B("Verschattungsfaktor"), A("Verschattungsfaktor"), G("Verschattungsfaktor")),
            new Inventarzeile("F5", KF, KEIN_FELD + " (nicht gerechnet)", D3, "nur gelesen: VisibleLightTransmittance wird durchgereicht, EPOS führt kein Feld"),
            new Inventarzeile("F6", BE, "nur gelesen: SolarHeatGainCoeff bei 0°; die übrigen Winkel werden verworfen, EPOS führt kein Feld",
                              KF, KEIN_FELD + " (nicht abbildbar)"),
            new Inventarzeile("F7", KF, KEIN_FELD + " (nicht gerechnet)", KF, KEIN_FELD + " (nicht gerechnet)"),
            new Inventarzeile("F8", D2, null, D3, null, G("Aussenbauteile_Strahlung")),
            new Inventarzeile("F9", D2, null, D3, null),
            new Inventarzeile("F10", KF, KEIN_FELD + " (Konstante im Klimaweg, nicht Gegenstand eines Bauteils)", KF,
                              KEIN_FELD + " (Konstante im Klimaweg, nicht Gegenstand eines Bauteils)"),

            new Inventarzeile("G1", D4, null, D4, null,
                              "Tab_Zonenluftstrom.ID_ZoneA", "Tab_Zonenluftstrom.ID_ZoneB", "Tab_Zonenluftstrom.Volumenstrom"),
            new Inventarzeile("G2", BE, null, BE, null, G("Grundflaeche"), G("Grundflaeche_Randbedingung"), B("Randbedingung")),
            new Inventarzeile("G3", D4, null, D4, null, G("Abmessung_Anschluß_Außenwand_Kellerdecke")),
            new Inventarzeile("G4", D4, null, D4, null, G("Erdreich_U_Wirksam"), G("Kellertemperatur")),
        };

        /// <summary>Die Tabellen, deren Spalten die Wache über <c>PRAGMA table_info</c> der Testdatenbank prüft.</summary>
        internal static readonly IReadOnlyList<string> Tabellen = new[]
        {
            "Tab_Zone", "Tab_Bauteil", "Tab_Gebaeude", "Tab_Zonenluftstrom", "Tab_Konditionierungskalender", "Tab_Konditionierungsperiode",
            "Tab_Konditionierungswoche", "Tab_Konditionierungsvorgabe", "Tab_Bauteilaufbau", "Tab_Bauteilschicht",
        };

        /// <summary>Die Modelle, deren öffentliche Felder die Wache per Reflexion prüft, mit ihrer Tabelle.</summary>
        internal static readonly IReadOnlyList<(Type Typ, string Tabelle)> Modelle = new[]
        {
            (typeof(ZoneModel), "Tab_Zone"), (typeof(BauteilModel), "Tab_Bauteil"), (typeof(ProjektGebaeudeModel), "Tab_Gebaeude"),
            (typeof(GebaeudeModel), "Tab_Gebaeude"),
        };

        private static string[] Ids(string tabelle, params string[] spalten) => spalten.Select(s => tabelle + "." + s).ToArray();

        /// <summary>Die Felder, die bewusst nicht zum Datenaustausch gehören — je Grund.</summary>
        internal static readonly IReadOnlyList<Austauschausnahme> Ausnahmen = new[]
        {
            new Austauschausnahme("Kennung, Fremdschlüssel oder Rang: die Datei trägt eigene Kennungen, der Import vergibt neue",
                F(Ids("Tab_Zone", "ID", "ID_Gebaeude", "Rang"), Ids("Tab_Bauteil", "ID", "ID_Zone", "Rang"),
                  Ids("Tab_Gebaeude", "ID", "ID_ProjektGebaeude", "ID_Projekt", "ID_Gebaeude_Stamm"), Ids("Tab_Zonenluftstrom", "ID"),
                  Ids("Tab_Konditionierungskalender", "ID", "ID_Gebaeude", "ID_Zone", "ID_Gebaeude_Stamm", "ID_Zone_Stamm", "ID_Vorlage"),
                  Ids("Tab_Konditionierungsperiode", "ID", "ID_Kalender", "Rang"),
                  Ids("Tab_Konditionierungswoche", "ID", "ID_Gebaeude", "ID_Zone", "ID_Gebaeude_Stamm", "ID_Zone_Stamm", "ID_Vorlage"),
                  Ids("Tab_Konditionierungsvorgabe", "ID", "ID_Gebaeude", "ID_Zone", "ID_Gebaeude_Stamm", "ID_Zone_Stamm", "ID_Vorlage"),
                  Ids("Tab_Bauteilaufbau", "ID", "ID_Projekt"), Ids("Tab_Bauteilschicht", "ID", "ID_Aufbau", "ID_Baustoff"),
                  Ids("ProjektGebaeudeModel", "items", "ID_Gebaeude"), Ids("ZoneModel", "Bauteile"))),
            new Austauschausnahme("Herkunftsmarke der Übernahme: der Import setzt sie neu (Quelle, Kennung der Quelle)",
                F(Ids("Tab_Zone", "Herkunft", "Quellkennung"), Ids("Tab_Bauteil", "Herkunft", "Quellkennung", "Flaechenherkunft", "U_Herkunft"),
                  Ids("Tab_Bauteilaufbau", "Quelle", "Herkunft", "Quellkennung", "Dicke_Quelle"), Ids("Tab_Bauteilschicht", "Quellkennung"))),
            new Austauschausnahme("Anzeige, Name oder Freitext ohne Wirkung auf die Rechnung",
                F(Ids("Tab_Bauteil", "Bezeichner"), Ids("Tab_Gebaeude", "Gebaeudename", "Typ", "Beschreibung"),
                  Ids("Tab_Konditionierungskalender", "Bemerkung"), Ids("Tab_Bauteilaufbau", "Bezeichner", "Beschreibung", "Infotext"),
                  Ids("Tab_Bauteilschicht", "Kurztext", "Langtext"))),
            new Austauschausnahme("Katalogeinordnung und -prüfung des Aufbaus bzw. der Schicht; die Rechnung liest die Schichtwerte",
                F(Ids("Tab_Bauteilaufbau", "Bauteilart", "Typaufbau", "Gruppe_Code", "Klasse_Code", "Simulation_Ungeeignet", "Gueltigkeit"),
                  Ids("Tab_Bauteilschicht", "Gruppe_Code", "Schichtart_Code", "Estrichart_Code", "Belueftungsart_Code", "Dickenart_Code"))),
            new Austauschausnahme("Bauphysik- und Ökobilanzangabe des Katalogs ohne Rechenweg der Gebäudesimulation (Feuchte, Mechanik, OI3, Einbau-ψ)",
                F(Ids("Tab_Bauteilaufbau", "Hinterlueftet", "Psi_Gesamt", "Psi_System", "Psi_Geometrie", "OI3_Rahmen_AP", "OI3_Rahmen_PEI", "OI3_Rahmen_GWP"),
                  Ids("Tab_Bauteilschicht", "Emissionsgrad", "Mue_Min", "Mue_Max", "Rauheit", "Elastizitaet", "Laengenausdehnung", "Hinterlueftet",
                      "Kapillaraktiv", "Dampfdicht", "Feuchtegehalt", "OI3_AP", "OI3_PEI", "OI3_GWP"))),
            new Austauschausnahme("Eingangswert der U_w-Bildung des Fensteraufbaus (ψ des Glasrands, DIN EN ISO 10077-1); der Austausch trägt " +
                                  "das Ergebnis U_Wert des Bauteils",
                F(Ids("Tab_Bauteilaufbau", "Psi_Glasrand"))),
            new Austauschausnahme("Summenfeld des Klassenwegs: der Import bildet es aus den Bauteilen (D1–D7) neu",
                F(Ids("Tab_Gebaeude", "Fensterflaeche_Sued", "Fensterflaeche_Ost_West", "Fensterflaeche_Nord", "Fensterflaeche_Ost", "Fensterflaeche_West",
                      "k_Wert_Außenwand", "k_Wert_Fenster", "k_Wert_Dachflaeche", "k_Wert_Grundflaeche", "k_Wert_Sonstiges", "Flaeche_Außenwand",
                      "gesamte_Fensterflaeche", "Dachflaeche", "Sonstige_Flaechen"),
                  Ids("ProjektGebaeudeModel", "Fensterflaeche_OstWest"), Ids("GebaeudeModel", "Fensterflaeche_OstWest"))),
            new Austauschausnahme("Bedarf (Brauchwasser, Wärmebedarf, Wohnfläche, Bewohnerzahl des Gebäudes) — Konzept 8, nicht Gegenstand",
                F(Ids("Tab_Gebaeude", "Wohnflaeche_gesamt", "Bewohner", "WW_Bedarf", "spez_Waermeverbrauch", "Waermebedarf"),
                  Ids("ProjektGebaeudeModel", "Z_AuswahlWohnflaeche", "Einheit", "Jahresnutzungsgrad", "DezentralWarmwasser"))),
            new Austauschausnahme("Wahl des Rechenwegs (Projektsteuerung, kein Datum des Gebäudes)", Ids("Tab_Gebaeude", "Gebaeude_Modell")),
            new Austauschausnahme("Anlagentechnik am Gebäude (Heizkreis, Übergabe und Heizkurve am Gebäude, Kühlübergabe, Kühlkurve) — Konzept 8",
                Ids("Tab_Gebaeude", "Heizkreis_Aktiv", "Uebergabe_Art", "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn", "Auslegung_Vorlauf",
                    "Auslegung_Ruecklauf", "Auslegung_Raumtemperatur", "Auslegung_Aussentemperatur", "Heizkurve_Aktiv", "Heizkurve_Niveau",
                    "Heizkurve_Steilheit", "Heizkurve_Raumeinfluss", "Regler_Proportionalband", "Sollwertprofil", "Kuehluebergabe_Aktiv",
                    "Kuehl_Uebergabe_Art", "Kuehl_Uebergabe_Exponent", "Kuehl_Uebergabe_Leistung_Nenn", "Kuehl_Auslegung_Vorlauf",
                    "Kuehl_Auslegung_Ruecklauf", "Kuehl_Auslegung_Raumtemperatur", "Kuehl_Vorlaufgrenze", "Kuehlkurve_Aktiv", "Kuehlkurve_Fusspunkt",
                    "Kuehlkurve_Raumeinfluss", "Kuehlkurve_Auslegung_Weg", "Kuehlkurve_Auslegung_Aussen")),
        };

        /// <summary>Die Formatklasse in gbXML: gbXML kennt keinen eigenen Satz; der Erweiterungspunkt ist nicht gebaut.</summary>
        internal static Formatklasse KlasseGbxml(Austauschweg w)
            => w == Austauschweg.OhneElement ? Formatklasse.NichtUebertragbar : w == Austauschweg.NurEpos ? Formatklasse.Eigen : Formatklasse.Standard;

        /// <summary>Die Formatklasse in IFC: <c>EPOS_*</c> ist der eigene Satz.</summary>
        internal static Formatklasse KlasseIfc(Austauschweg w) => KlasseGbxml(w);

        /// <summary>Kommt ein Datum im eigenen Rundlauf gleich zurück (beide Richtungen, Standard oder eigener Satz)?</summary>
        internal static bool Rundlaeufig(Austauschweg w) => w == Austauschweg.BeideRichtungen || w == Austauschweg.NurEpos;

        /// <summary>Alle eingestuften Felder (Tafel und Ausnahmen).</summary>
        internal static ISet<string> Eingestuft()
            => new HashSet<string>(Tafel.SelectMany(z => z.Felder).Concat(Ausnahmen.SelectMany(a => a.Felder)), StringComparer.Ordinal);

        /// <summary>
        /// <b>Der Kern der Wache:</b> die Felder, die weder in der Tafel noch in den Ausnahmen stehen. Ein Modellfeld gilt als eingestuft,
        /// wenn <c>Tabelle.Feld</c> oder <c>Modell.Feld</c> eingestuft ist.
        /// </summary>
        internal static IReadOnlyList<string> NichtEingestuft(IEnumerable<string> felder, Func<string, string> ersatz = null)
        {
            ISet<string> bekannt = Eingestuft();
            return felder.Where(f => !bekannt.Contains(f) && (ersatz == null || !bekannt.Contains(ersatz(f))))
                         .Distinct(StringComparer.Ordinal).ToList();
        }

        /// <summary>Die öffentlichen Felder und Eigenschaften eines Modells, als <c>Modell.Feld</c>.</summary>
        internal static IEnumerable<string> Modellfelder(Type typ)
            => typ.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Select(f => f.Name)
                  .Concat(typ.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Select(p => p.Name))
                  .Select(n => typ.Name + "." + n);

        /// <summary>Die Felder der Modelle, die nicht eingestuft sind (über die Tabelle oder den Modellnamen).</summary>
        internal static IReadOnlyList<string> ModellfelderNichtEingestuft(IEnumerable<(Type Typ, string Tabelle)> modelle)
            => modelle.SelectMany(m => NichtEingestuft(Modellfelder(m.Typ), f => m.Tabelle + f.Substring(f.IndexOf('.'))))
                      .ToList();
    }
}
