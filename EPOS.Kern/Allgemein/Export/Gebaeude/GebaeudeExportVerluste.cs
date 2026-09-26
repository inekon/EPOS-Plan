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
            ["Maximaleraumtemperatur"] = V, ["Heizung_Strahlungsanteil"] = V, ["Heizleistung_Max"] = V,
            ["Luftwechsel_Nutzer"] = V,
            // Innere Gewinne: der Import setzt die Vorgabe E43 (5 W/m²), nicht den Mittelwert der Datei.
            ["Interne_Waermegewinne"] = V,
            // Bewohner kommen als Fläche je Nutzer des Gebäudes zurück.
            ["Bewohner"] = V,
            ["Kuehl_Sollwert"] = V, ["Kuehlleistung_Max"] = V, ["Kuehlung_Aktiv"] = V, ["Kuehl_Sollwert_Nacht"] = V,
            ["Uebergabe_Art"] = V, ["Uebergabe_Exponent"] = V, ["Uebergabe_Leistung_Nenn"] = V,
            ["Kuehl_Uebergabe_Art"] = V, ["Kuehl_Uebergabe_Exponent"] = V, ["Kuehl_Uebergabe_Leistung_Nenn"] = V,
        };

        /// <summary>Die Felder eines Bauteils.</summary>
        internal static readonly IReadOnlyDictionary<string, Exporteinstufung> Bauteil = new Dictionary<string, Exporteinstufung>
        {
            ["ID"] = N, ["ID_Zone"] = N, ["Rang"] = N, ["Herkunft"] = N, ["Quellkennung"] = N,
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
            ["Maximaleraumtemperatur"] = V, ["Luftwechselrate"] = V, ["Luftwechsel_Nutzer"] = V,
            ["Baualtersklasse"] = V, ["Baujahr"] = V, ["Gebaeudeart"] = V, ["Wohngebaeude_Nicht_Wohngebaeude"] = V,
            ["Rahmenanteil"] = V, ["Verschattungsfaktor"] = V, ["Kellertemperatur"] = V, ["Grundflaeche_Randbedingung"] = V,
            ["Heizung_Strahlungsanteil"] = V, ["Heizleistung_Max"] = V, ["Aussenbauteile_Strahlung"] = V, ["Sommerlueftung"] = V,
            ["Kuehl_Sollwert"] = V, ["Kuehlleistung_Max"] = V, ["Kuehlung_Aktiv"] = V, ["Kuehl_Sollwert_Nacht"] = V,
            ["Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand"] = V, ["Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach"] = V,
            ["Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke"] = V, ["Abmessung_Anschluß_Fenster_Wand"] = V,
            ["Abmessung_Anschluß_Wand_Dach"] = V, ["Abmessung_Anschluß_Außenwand_Kellerdecke"] = V,
            ["Heizkreis_Aktiv"] = V, ["Uebergabe_Art"] = V, ["Uebergabe_Exponent"] = V, ["Uebergabe_Leistung_Nenn"] = V,
            ["Auslegung_Vorlauf"] = V, ["Auslegung_Ruecklauf"] = V, ["Auslegung_Raumtemperatur"] = V, ["Auslegung_Aussentemperatur"] = V,
            ["Heizkurve_Aktiv"] = V, ["Heizkurve_Niveau"] = V, ["Heizkurve_Steilheit"] = V, ["Regler_Proportionalband"] = V,
            ["Sollwertprofil"] = V, ["Kuehluebergabe_Aktiv"] = V, ["Kuehl_Uebergabe_Art"] = V, ["Kuehl_Uebergabe_Exponent"] = V,
            ["Kuehl_Uebergabe_Leistung_Nenn"] = V, ["Kuehl_Auslegung_Vorlauf"] = V, ["Kuehl_Auslegung_Ruecklauf"] = V,
            ["Kuehl_Auslegung_Raumtemperatur"] = V, ["Kuehl_Vorlaufgrenze"] = V,
            ["Nachtabsenkung_Beginn"] = V, ["Nachtabsenkung_Ende"] = V, ["Wochenende"] = V, ["Ferien"] = V,
            ["Ferienbeginn_1"] = V, ["Ferienende_1"] = V, ["Ferienbeginn_2"] = V, ["Ferienende_2"] = V,
            ["Ferienbeginn_3"] = V, ["Ferienende_3"] = V, ["Ferienbeginn_4"] = V, ["Ferienende_4"] = V,
            ["Gebaeude_Modell"] = N, ["WW_Bedarf"] = N, ["spez_Waermeverbrauch"] = N, ["Waermebedarf"] = N,
            ["k_Wert_Außenwand"] = N, ["k_Wert_Fenster"] = N, ["k_Wert_Dachflaeche"] = N, ["k_Wert_Grundflaeche"] = N,
            ["k_Wert_Sonstiges"] = N, ["Flaeche_Außenwand"] = N, ["gesamte_Fensterflaeche"] = N, ["Dachflaeche"] = N,
            ["Grundflaeche"] = N, ["Sonstige_Flaechen"] = N, ["Fensterflaeche_Sued"] = N, ["Fensterflaeche_OstWest"] = N,
            ["Fensterflaeche_Nord"] = N, ["Fensterflaeche_Ost"] = N, ["Fensterflaeche_West"] = N,
        };

        /// <summary>
        /// Die benannten Verluste, die ein Satz tatsächlich trägt — die Feldnamen der Zonen und Bauteile
        /// mit einem gesetzten Wert, dazu die Gebäudefelder, die der Export nie mitnimmt (Sollwerte außer
        /// Tag, Bauweise). Sortiert, damit die Meldung stabil ist.
        /// </summary>
        internal static IReadOnlyList<string> Getragen(ProjektGebaeudeModel g, IEnumerable<ZoneModel> zonen)
        {
            var namen = new SortedSet<string>(System.StringComparer.Ordinal)
            {
                "Raumsolltemperatur_Nachtabsenkung", "Raumsolltemperatur_Wochenende", "Raumsolltemperatur_Ferien",
                "Maximaleraumtemperatur", "Bauweise",
            };
            if (g != null)
            {
                if (g.Luftwechsel_Nutzer.HasValue) namen.Add("Luftwechsel_Nutzer");
                if (g.Heizleistung_Max.HasValue) namen.Add("Heizleistung_Max");
                if (g.Kuehlleistung_Max.HasValue) namen.Add("Kuehlleistung_Max");
                if (g.Heizung_Strahlungsanteil.HasValue) namen.Add("Heizung_Strahlungsanteil");
                if (g.Rahmenanteil.HasValue) namen.Add("Rahmenanteil");
                if (g.Verschattungsfaktor.HasValue) namen.Add("Verschattungsfaktor");
                if (!string.IsNullOrWhiteSpace(g.Uebergabe_Art)) namen.Add("Uebergabe_Art");
            }
            foreach (ZoneModel z in zonen ?? new List<ZoneModel>())
            {
                if (z == null) continue;
                if (z.Luftwechsel_Nutzer.HasValue) namen.Add("Luftwechsel_Nutzer");
                if (z.Heizleistung_Max.HasValue) namen.Add("Heizleistung_Max");
                if (z.Kuehlleistung_Max.HasValue) namen.Add("Kuehlleistung_Max");
                if (z.Interne_Waermegewinne.HasValue) namen.Add("Interne_Waermegewinne");
                if (z.Bewohner.HasValue) namen.Add("Bewohner");
                if (!string.IsNullOrWhiteSpace(z.Uebergabe_Art)) namen.Add("Uebergabe_Art");
                if (!string.IsNullOrWhiteSpace(z.Kuehl_Uebergabe_Art)) namen.Add("Kuehl_Uebergabe_Art");
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
