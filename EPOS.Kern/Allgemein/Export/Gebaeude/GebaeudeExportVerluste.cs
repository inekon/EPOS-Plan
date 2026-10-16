using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Wie ein EPOS-Feld den Rundlauf Export → Import übersteht (Stufe G7a).</summary>
    internal enum Exporteinstufung
    {
        /// <summary>Der wirksame Wert kommt gleich zurück (auf 1e-6).</summary>
        Rundlaeufig = 0,
        /// <summary>Der Wert geht verloren oder kommt in anderer Gestalt zurück — benannt, vor dem Schreiben gemeldet.</summary>
        BenannterVerlust = 1,
        /// <summary>Kein Gegenstand des Exports (Schlüssel, Herkunft, Anlagen- und Bedarfsdaten).</summary>
        NichtGegenstand = 2,
    }

    /// <summary>
    /// <b>Die Verlustliste des gbXML-Exports</b> (Umsetzungsauftrag G7a, W2) — je Feld von
    /// <see cref="ZoneModel"/>, <see cref="BauteilModel"/> und <see cref="ProjektGebaeudeModel"/> die
    /// Einstufung: rundläufig, benannter Verlust oder nicht Gegenstand. Ein Test hält die Tabellen per
    /// Reflexion vollständig; der Datenbank-Rundlauf misst „rundläufig" und „benannter Verlust" an den
    /// Feldern, die sein Probegebäude trägt. Die benannten Verluste nennt der Ablauf vor dem Schreiben
    /// (<see cref="GebaeudeExportAblauf.VERLUSTE"/>).
    ///
    /// <para><b>Wirksam heißt wie im Lauf:</b> ein leerer Zonenwert ist der des Gebäudes, eine leere
    /// Neigung die Vorgabe der Bauteilart, ein leerer g-Wert der des Gebäudes, ein leerer U-Wert der aus
    /// den Schichten. Die Gestalt darf wechseln (NULL kommt gesetzt zurück, ein U-Wert ohne Aufbau als
    /// Ersatzschichtung), der wirksame Wert nicht.</para>
    /// </summary>
    internal static class GebaeudeExportVerluste
    {
        private const Exporteinstufung R = Exporteinstufung.Rundlaeufig;
        private const Exporteinstufung V = Exporteinstufung.BenannterVerlust;
        private const Exporteinstufung N = Exporteinstufung.NichtGegenstand;

        /// <summary>Die Felder der Zone.</summary>
        internal static readonly IReadOnlyDictionary<string, Exporteinstufung> Zone = new Dictionary<string, Exporteinstufung>
        {
            ["ID"] = N, ["ID_Gebaeude"] = N, ["Rang"] = N, ["Herkunft"] = N, ["Quellkennung"] = N, ["Bauteile"] = N,
            // Der Import nennt die eine Zone nach dem Gebäude (Zonenregel X4).
            ["Bezeichner"] = V,
            ["Nutzflaeche"] = R, ["Volumen"] = R, ["Raumhoehe"] = R, ["IstBeheizt"] = R,
            ["Raumsolltemperatur_Tag"] = R, ["Luftwechsel_Infiltration"] = R,
            ["Raumsolltemperatur_Nachtabsenkung"] = V, ["Raumsolltemperatur_Wochenende"] = V, ["Raumsolltemperatur_Ferien"] = V,
            ["Heizung_Strahlungsanteil"] = V,
            // Nachzug N2 (DA-7 B10): die Höchsttemperatur der Zone reist als Restdatum wie die Höchstleistungen.
            ["Maximaleraumtemperatur"] = R,
            // DA-4: Höchstleistungen, Übergabe und Kühlübergabe je Zone und die Vorheizzeit reisen als Restdaten (gbXML-Block
            // EPOS: der Zone/Description, IFC EPOS_Zone) und kommen über den Import wieder an die Zone (GebaeudeRestdaten.Setzen).
            ["Heizleistung_Max"] = R,
            // DA-1 bis DA-3, nachgeführt in DA-4 (Befund DA-7): Nutzerluftwechsel, innere Gewinne (Geräte und Licht je Zone),
            // Belegung und die Kühlsollwerte kommen über Lasten, Zeitpläne und Sollwerte je Zone zurück.
            ["Luftwechsel_Nutzer"] = R,
            ["Interne_Waermegewinne"] = R,
            ["Bewohner"] = R,
            ["Kuehl_Sollwert"] = R, ["Kuehlleistung_Max"] = R, ["Kuehlung_Aktiv"] = R, ["Kuehl_Sollwert_Nacht"] = R,
            ["Uebergabe_Art"] = R, ["Uebergabe_Exponent"] = R, ["Uebergabe_Leistung_Nenn"] = R,
            ["Kuehl_Uebergabe_Art"] = R, ["Kuehl_Uebergabe_Exponent"] = R, ["Kuehl_Uebergabe_Leistung_Nenn"] = R,
            // Schritt 181 (E63, AK1z): der Auslegungspunkt und das Proportionalband der Zone.
            ["Auslegung_Vorlauf"] = R, ["Auslegung_Ruecklauf"] = R, ["Auslegung_Raumtemperatur"] = R,
            ["Regler_Proportionalband"] = R,
            // AH-V4 (Schritt 215), DA-4: die Vorheizzeit der Zone reist als Restdatum.
            ["Vorheizzeit_H"] = R,
            // NP1 (Schritt 189, Q41): die Textkopie des Profilnamens an der Zone. Der Export nimmt sie nicht mit;
            // die Nutzung der Zone führt der IFC-Export als Zelle Nutzung von EPOS_Zone aus den Kalenderkopien
            // (RaumnutzungCtrl.ProfilUebernehmen schreibt beides zugleich), und der Import setzt sie dort wieder.
            ["Nutzungsprofil"] = N,
            // DA-2 (ZonenLastfelderSchema): die Lastfelder der Zone - gbXML trägt sie (OAFlowPerPerson, LightPowerPerArea,
            // PeopleHeatGain), der Import setzt sie wieder (GbxmlKonditionierung.Lastfelder).
            ["Aussenluft_Person"] = R, ["Licht_W_m2"] = R, ["Personen_Waerme_W"] = R,
        };

        /// <summary>Die Felder eines Bauteils.</summary>
        internal static readonly IReadOnlyDictionary<string, Exporteinstufung> Bauteil = new Dictionary<string, Exporteinstufung>
        {
            ["ID"] = N, ["ID_Zone"] = N, ["Rang"] = N, ["Herkunft"] = N, ["Quellkennung"] = N, ["Flaechenherkunft"] = N,
            ["Bezeichner"] = R, ["Bauteilart"] = R, ["Flaeche"] = R, ["Randbedingung"] = R,
            ["ID_Aufbau"] = R, ["U_Wert"] = R, ["g_Wert"] = R, ["Neigung"] = R, ["Azimut"] = R,
            ["Rahmenanteil"] = V, ["Verschattungsfaktor"] = V, ["Psi_L"] = V,
            // Eine Trennfläche zur Nachbarzone kommt als innere Masse zurück (Import X4).
            ["ID_Nachbarzone"] = V, ["Trennflaeche_Zuordnung"] = V,
        };

        /// <summary>
        /// Die Felder des Projektgebäudes. Die Summenfelder des Klassenwegs (Flächen, U-Werte,
        /// Fensterflächen je Richtung) sind nicht Gegenstand: Exportiert werden die Bauteile der Zone
        /// bzw. des Übernahmevorschlags; der Import bildet die Summen daraus neu.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, Exporteinstufung> Gebaeude = new Dictionary<string, Exporteinstufung>
        {
            ["items"] = N, ["ID_Projekt"] = N, ["ID_Gebaeude"] = N, ["Z_AuswahlWohnflaeche"] = N, ["Einheit"] = N,
            ["Jahresnutzungsgrad"] = N, ["DezentralWarmwasser"] = N, ["Typ"] = N, ["Beschreibung"] = N,
            ["Wohnflaeche_gesamt"] = N, ["Bewohner"] = N,
            ["Gebaeudename"] = R, ["Nutzflaeche"] = R, ["Raumhoehe"] = R, ["Flaeche_Nutzer"] = R,
            ["Raumsolltemperatur_Tag"] = R, ["Luftwechsel_Infiltration"] = R, ["Fensterdurchlassgrad"] = R,
            ["Interne_Waermegewinne"] = V, ["Bauweise"] = V, ["Masseanteil_Aussen"] = V, ["Innenflaechenfaktor"] = V,
            ["Raumsolltemperatur_Nachtabsenkung"] = V, ["Raumsolltemperatur_Wochenende"] = V, ["Raumsolltemperatur_Ferien"] = V,
            // Nachzug N4 (B10): die obere Raumtemperatur des Gebäudes reist als Restdatum neben dem Erdreich (gbXML-Block EPOS:
            // der Building/Description, IFC EPOS_Gebaeude) und kommt über das Zielfeld MAX_RAUMTEMPERATUR zurück.
            ["Maximaleraumtemperatur"] = R, ["Luftwechselrate"] = V, ["Luftwechsel_Nutzer"] = V,
            ["Baualtersklasse"] = V, ["Baujahr"] = V, ["Energiestandard"] = V,
            // DA-3/DA-7: die Gebäudeart reist als buildingType (gbXML) bzw. EPOS_Gebaeude.Gebaeudeart (IFC).
            ["Gebaeudeart"] = R, ["Wohngebaeude_Nicht_Wohngebaeude"] = V,
            ["Rahmenanteil"] = V, ["Verschattungsfaktor"] = V, ["Grundflaeche_Randbedingung"] = V,
            // DA-4: das Erdreich (Inventar G3, G4) reist als Restdatum (gbXML-Block EPOS: der Building/Description, IFC
            // EPOS_Gebaeude) und kommt über die Zielfelder LAENGE_AUSSENWAND_KELLER, ERDREICH_U_WIRKSAM, KELLERTEMPERATUR zurück.
            ["Kellertemperatur"] = R,
            ["Heizung_Strahlungsanteil"] = V, ["Heizleistung_Max"] = V, ["Aussenbauteile_Strahlung"] = V, ["Sommerlueftung"] = V,
            ["Kuehl_Sollwert"] = V, ["Kuehlleistung_Max"] = V, ["Kuehlung_Aktiv"] = V, ["Kuehl_Sollwert_Nacht"] = V,
            ["Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand"] = V, ["Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach"] = V,
            ["Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke"] = V, ["Abmessung_Anschluß_Fenster_Wand"] = V,
            ["Abmessung_Anschluß_Wand_Dach"] = V, ["Abmessung_Anschluß_Außenwand_Kellerdecke"] = R,
            ["Heizkreis_Aktiv"] = V, ["Uebergabe_Art"] = V, ["Uebergabe_Exponent"] = V, ["Uebergabe_Leistung_Nenn"] = V,
            ["Auslegung_Vorlauf"] = V, ["Auslegung_Ruecklauf"] = V, ["Auslegung_Raumtemperatur"] = V, ["Auslegung_Aussentemperatur"] = V,
            ["Heizkurve_Aktiv"] = V, ["Heizkurve_Niveau"] = V, ["Heizkurve_Steilheit"] = V, ["Regler_Proportionalband"] = V,
            ["Sollwertprofil"] = V, ["Kuehluebergabe_Aktiv"] = V, ["Kuehl_Uebergabe_Art"] = V, ["Kuehl_Uebergabe_Exponent"] = V,
            ["Kuehl_Uebergabe_Leistung_Nenn"] = V, ["Kuehl_Auslegung_Vorlauf"] = V, ["Kuehl_Auslegung_Ruecklauf"] = V,
            ["Kuehl_Auslegung_Raumtemperatur"] = V, ["Kuehl_Vorlaufgrenze"] = V,
            ["Nachtabsenkung_Beginn"] = V, ["Nachtabsenkung_Ende"] = V, ["Wochenende"] = V, ["Ferien"] = V,
            // E59 (Festlegung 38): die manuelle Aufheizzeit - die Datei traegt sie nicht, der Import schreibt NULL.
            ["Aufheizzeit_Manuell_H"] = V,
            // E65, DA-4: der wirksame U-Wert der Bodenplatte als Vorgabe reist als Restdatum (siehe Kellertemperatur).
            ["Erdreich_U_Wirksam"] = R,
            // AK3 (Festlegung 23): der Raumeinfluss der Heizkurve - die Datei traegt ihn nicht, der Import schreibt NULL.
            ["Heizkurve_Raumeinfluss"] = V,
            // KK (KuehlkurveSchema.SCHRITT): die Kuehlkurve - die Datei traegt sie nicht, der Import schreibt NULL; gesetzt
            // wird sie als Verlust benannt (Getragen). Projektpaket und Katalogpaket tragen die Spalten.
            ["Kuehlkurve_Aktiv"] = V, ["Kuehlkurve_Fusspunkt"] = V, ["Kuehlkurve_Raumeinfluss"] = V,
            ["Kuehlkurve_Auslegung_Weg"] = V, ["Kuehlkurve_Auslegung_Aussen"] = V,
            // K2 (KalenderbedienungSchema.SCHRITT): Wochenende und Feiertagsland - die Datei traegt sie nicht, der Import
            // schreibt NULL (Vorgabe Sa + So, nur bundeseinheitliche Feiertage); gesetzt werden sie als Verlust benannt.
            ["Wochenendtage"] = V, ["Feiertagsland"] = V,
            ["Ferienbeginn_1"] = V, ["Ferienende_1"] = V, ["Ferienbeginn_2"] = V, ["Ferienende_2"] = V,
            ["Ferienbeginn_3"] = V, ["Ferienende_3"] = V, ["Ferienbeginn_4"] = V, ["Ferienende_4"] = V,
            ["Gebaeude_Modell"] = N, ["WW_Bedarf"] = N, ["spez_Waermeverbrauch"] = N, ["Waermebedarf"] = N,
            ["k_Wert_Außenwand"] = N, ["k_Wert_Fenster"] = N, ["k_Wert_Dachflaeche"] = N, ["k_Wert_Grundflaeche"] = N,
            ["k_Wert_Sonstiges"] = N, ["Flaeche_Außenwand"] = N, ["gesamte_Fensterflaeche"] = N, ["Dachflaeche"] = N,
            ["Grundflaeche"] = N, ["Sonstige_Flaechen"] = N, ["Fensterflaeche_Sued"] = N, ["Fensterflaeche_OstWest"] = N,
            ["Fensterflaeche_Nord"] = N, ["Fensterflaeche_Ost"] = N, ["Fensterflaeche_West"] = N,
        };

        /// <summary>
        /// Der Name des Verlusts „Kalender": Das Gebäude oder eine Zone trägt einen angelegten
        /// Konditionierungskalender; die Datei trägt keinen Zeitplan, nur je Raum einen Heizsollwert
        /// (Entwurf KP2, Festlegung 9). Kein Modellfeld — er steht allein in <see cref="Getragen"/>.
        /// </summary>
        internal const string KALENDER = "Kalender";

        /// <summary>
        /// Die benannten Verluste, die ein Satz tatsächlich trägt — die Feldnamen der Zonen und Bauteile
        /// mit einem gesetzten Wert, dazu die Gebäudefelder, die der Export nie mitnimmt (Sollwerte außer
        /// Tag, Bauweise), und <see cref="KALENDER"/>, wenn <paramref name="kalender"/> sagt, dass ein
        /// angelegter Kalender mitreist. Sortiert, damit die Meldung stabil ist.
        /// </summary>
        internal static IReadOnlyList<string> Getragen(ProjektGebaeudeModel g, IEnumerable<ZoneModel> zonen,
                                                       bool kalender = false)
        {
            var namen = new SortedSet<string>(System.StringComparer.Ordinal)
            {
                "Raumsolltemperatur_Nachtabsenkung", "Raumsolltemperatur_Wochenende", "Raumsolltemperatur_Ferien",
                "Bauweise",
            };
            if (kalender) namen.Add(KALENDER);
            if (g != null)
            {
                if (g.Luftwechsel_Nutzer.HasValue) namen.Add("Luftwechsel_Nutzer");
                if (g.Heizleistung_Max.HasValue) namen.Add("Heizleistung_Max");
                if (g.Kuehlleistung_Max.HasValue) namen.Add("Kuehlleistung_Max");
                if (g.Heizung_Strahlungsanteil.HasValue) namen.Add("Heizung_Strahlungsanteil");
                if (g.Rahmenanteil.HasValue) namen.Add("Rahmenanteil");
                if (g.Verschattungsfaktor.HasValue) namen.Add("Verschattungsfaktor");
                if (!string.IsNullOrWhiteSpace(g.Uebergabe_Art)) namen.Add("Uebergabe_Art");
                if (g.Aufheizzeit_Manuell_H.HasValue) namen.Add("Aufheizzeit_Manuell_H");
                if (g.Heizkurve_Raumeinfluss.HasValue) namen.Add("Heizkurve_Raumeinfluss");
                if (g.Kuehlkurve_Aktiv) namen.Add("Kuehlkurve_Aktiv");
                if (g.Kuehlkurve_Fusspunkt.HasValue) namen.Add("Kuehlkurve_Fusspunkt");
                if (g.Kuehlkurve_Raumeinfluss.HasValue) namen.Add("Kuehlkurve_Raumeinfluss");
                if (!string.IsNullOrWhiteSpace(g.Kuehlkurve_Auslegung_Weg)) namen.Add("Kuehlkurve_Auslegung_Weg");
                if (g.Kuehlkurve_Auslegung_Aussen.HasValue) namen.Add("Kuehlkurve_Auslegung_Aussen");
                if (g.Wochenendtage != KalenderbedienungSchema.WOCHENENDE_VORGABE) namen.Add("Wochenendtage");
                if (!string.IsNullOrWhiteSpace(g.Feiertagsland)) namen.Add("Feiertagsland");
            }
            foreach (ZoneModel z in zonen ?? new List<ZoneModel>())
            {
                if (z == null) continue;
                foreach (BauteilModel b in z.Bauteile ?? new List<BauteilModel>())
                {
                    if (b == null) continue;
                    if (b.Psi_L.HasValue && b.Psi_L.Value != 0.0) namen.Add("Psi_L");
                    if (b.Rahmenanteil.HasValue) namen.Add("Rahmenanteil");
                    if (b.Verschattungsfaktor.HasValue) namen.Add("Verschattungsfaktor");
                    if (b.ID_Nachbarzone.HasValue) namen.Add("ID_Nachbarzone");
                    if (!string.IsNullOrEmpty(b.Trennflaeche_Zuordnung)) namen.Add("Trennflaeche_Zuordnung");
                }
            }
            return new List<string>(namen);
        }
    }
}
