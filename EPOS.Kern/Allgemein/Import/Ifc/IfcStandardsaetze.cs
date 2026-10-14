using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die IFC-Standardsätze der Simulationsdaten</b> (Welle DA-3, Konzept Datenaustausch der Simulationsdaten 3.2, 4.2 und
    /// 4.4; E138 F6 (a) und F8 (b)) — eine Festlegung für den Schreiber (<see cref="IfcSchreiber"/>), die Anreicherung
    /// (<see cref="IfcAnreicherung"/>) und den Leser (<c>IfcAbbildBauer</c>):
    /// <list type="bullet">
    /// <item>je Raum <c>Pset_SpaceOccupancyRequirements</c> (Personenzahl, Fläche je Person), <c>Pset_SpaceThermalLoad</c>
    /// (Personen, Licht, Geräte als Leistung des Raums in W), <c>Pset_SpaceThermalDesign</c> (Auslegungstemperaturen),
    /// <c>Pset_SpaceLightingRequirements</c> (Kunstlicht ja/nein); die Zeitpläne bleiben in <c>EPOS_Kalender_*</c> (F6 a);</item>
    /// <item>je Fenster <c>Pset_WindowCommon.GlazingAreaFraction</c> = 1 − Rahmenanteil;</item>
    /// <item>je Baustoff <c>Pset_MaterialOptical</c> mit den Konstanten des Rechenwegs (F8 b): solare Reflexion außen
    /// = 1 − Absorptionsgrad, langwelliger Emissionsgrad außen.</item>
    /// </list>
    /// <para><b>Annahme zu den Namen:</b> Die IFC4-Schemakopie liegt nicht im Repositorium; die Satz- und Eigenschaftsnamen folgen
    /// der veröffentlichten Pset-Tafel von IFC4 ADD2 TC1 und sind im Protokoll DA-3 als Annahme geführt. Gelesen wird
    /// unabhängig von Groß-/Kleinschreibung.</para>
    /// </summary>
    internal static class IfcStandardsaetze
    {
        // Räume
        internal const string BELEGUNG = "Pset_SpaceOccupancyRequirements";
        internal const string BELEGUNG_ART = "OccupancyType";
        internal const string BELEGUNG_ZAHL = "OccupancyNumber";
        internal const string BELEGUNG_SPITZE = "OccupancyNumberPeak";
        internal const string BELEGUNG_STUNDEN = "OccupancyTimePerDay";
        internal const string BELEGUNG_FLAECHE = "AreaPerOccupant";

        internal const string LAST = "Pset_SpaceThermalLoad";
        internal const string LAST_PERSONEN = "People";
        internal const string LAST_LICHT = "Lighting";
        internal const string LAST_GERAETE = "EquipmentSensible";
        internal const string LAST_GERAETE_LATENT = "EquipmentLatent";
        internal const string LAST_LUFTWECHSEL = "AirExchangeRate";

        internal const string AUSLEGUNG = "Pset_SpaceThermalDesign";
        internal const string AUSLEGUNG_HEIZEN = "HeatingDryBulb";
        internal const string AUSLEGUNG_KUEHLEN = "CoolingDryBulb";
        internal const string AUSLEGUNG_LUEFTUNG = "VentilationAirFlowrate";
        internal const string AUSLEGUNG_HEIZLUFT = "HeatingDesignAirflow";
        internal const string AUSLEGUNG_KUEHLLUFT = "CoolingDesignAirflow";
        internal const string AUSLEGUNG_LUFTWECHSEL = "AirExchangeRate";

        internal const string LICHT = "Pset_SpaceLightingRequirements";
        internal const string LICHT_KUNSTLICHT = "ArtificialLighting";
        internal const string LICHT_BELEUCHTUNGSSTAERKE = "Illuminance";

        internal const string SOLLWERTE = "Pset_SpaceThermalRequirements";
        internal const string SOLL_KUEHLEN = "SpaceTemperatureSummerMax";
        internal const string SOLL_HOECHST = "SpaceTemperatureMax";
        internal const string SOLL_FEUCHTE = "SpaceHumidity";
        internal const string SOLL_NACHTABSENKUNG = "DiscontinuedHeating";
        internal const string SOLL_FREIE_LUEFTUNG = "NaturalVentilation";
        internal const string SOLL_FREIE_LUEFTUNG_RATE = "NaturalVentilationRate";
        internal const string SOLL_MECH_LUEFTUNG = "MechanicalVentilation";
        internal const string SOLL_MECH_LUEFTUNG_RATE = "MechanicalVentilationRate";
        internal const string SOLL_KLIMA = "AirConditioning";

        /// <summary>Herstellereigene Eigenschaften, gelesen nach dem Namen in jedem Satz — nur als Vorschlag (Konzept 4.2).</summary>
        internal const string HERSTELLER_LUEFTUNGSART = "RoomVentilationType";
        internal const string HERSTELLER_MINDESTLUFTWECHSEL = "RatedMinAirExchangeRate";

        /// <summary>Die Rückfallwerte in <c>EPOS_Zone</c>, die kein Standardsatz trägt (Konzept 3.3).</summary>
        internal const string EPOS_AUSSENLUFT_PERSON = "AussenluftJePerson";
        internal const string EPOS_WAERME_PERSON = "WaermeJePersonW";
        internal const string EPOS_INFILTRATION = "LuftwechselInfiltration";
        internal const string EPOS_PERSONEN = "Personen";
        internal const string EPOS_GERAETE = "GeraeteWm2";
        internal const string EPOS_LICHT = "LichtWm2";
        internal const string EPOS_WAERMEBRUECKE = "WaermebrueckeUA";
        internal const string EPOS_WIDERSTAND = "Waermedurchlasswiderstand";
        internal const string EPOS_GEBAEUDEART = "Gebaeudeart";

        // Fenster
        internal const string FENSTER = "Pset_WindowCommon";
        internal const string FENSTER_GLASANTEIL = "GlazingAreaFraction";
        internal const string VERGLASUNG = "Pset_DoorWindowGlazingType";
        internal const string VERGLASUNG_U = "ThermalTransmittance";
        internal const string VERGLASUNG_U_WINTER = "ThermalTransmittanceWinter";
        internal const string VERGLASUNG_LICHT = "VisibleLightTransmittance";

        // Baustoffe
        internal const string OPTIK = "Pset_MaterialOptical";
        internal const string OPTIK_REFLEXION = "SolarReflectanceFront";
        internal const string OPTIK_EMISSION = "ThermalIrEmissivityFront";

        /// <summary>Die Abweichung von den Konstanten des Rechenwegs, ab der der Leser meldet (wie gbXML).</summary>
        internal const double OPTIK_TOLERANZ = 0.005;

        /// <summary>Die Nachkommastellen der umgerechneten Werte (W → W/m², W → W je Person): Rundungsrauschen bleibt außen vor.</summary>
        internal const int STELLEN = 6;

        /// <summary>Die Personenzahl eines Raums: die Zahl, sonst Fläche ÷ Fläche je Person; <c>null</c> = keine.</summary>
        internal static double? Personenzahl(AbbildRaum r)
            => r.Personen ?? (r.FlaecheJePersonM2 > 0.0 && r.FlaecheM2 > 0.0 ? r.FlaecheM2 / r.FlaecheJePersonM2 : null);

        /// <summary>
        /// <b>Die Standardsätze eines Raums</b> aus den Feldern von EPOS (Satz → Werte; ein Satz ohne Wert entfällt). Personen,
        /// Licht und Geräte gehen als Leistung des Raums in W (Feld × Personen bzw. × Fläche), die Auslegungstemperaturen sind die
        /// Sollwerte. Unbeheizte Räume tragen nur die Belegung.
        /// </summary>
        internal static List<(string Satz, List<IfcSatzwert> Werte)> Raumsaetze(AbbildRaum r)
        {
            var aus = new List<(string, List<IfcSatzwert>)>();
            if (r == null) return aus;
            var belegung = new List<IfcSatzwert>();
            if (r.Personen.HasValue) belegung.Add(new IfcSatzwert(BELEGUNG_ZAHL, IfcSatzwertart.Zahl, r.Personen));
            if (r.FlaecheJePersonM2 > 0.0) belegung.Add(new IfcSatzwert(BELEGUNG_FLAECHE, IfcSatzwertart.Zahl, r.FlaecheJePersonM2));
            Hinzu(aus, BELEGUNG, belegung);
            if (!r.Beheizt) return aus;

            var last = new List<IfcSatzwert>();
            double? personen = Personenzahl(r);
            double? waerme = r.GbxmlLasten?.WaermeJePersonW;
            if (personen.HasValue && waerme.HasValue
                && !string.Equals(r.GbxmlLasten?.WaermeArt, "Latent", StringComparison.OrdinalIgnoreCase))
                last.Add(new IfcSatzwert(LAST_PERSONEN, IfcSatzwertart.Leistung, Math.Round(personen.Value * waerme.Value, STELLEN)));
            if (r.LichtWm2.HasValue && r.FlaecheM2 > 0.0)
                last.Add(new IfcSatzwert(LAST_LICHT, IfcSatzwertart.Leistung, Math.Round(r.LichtWm2.Value * r.FlaecheM2.Value, STELLEN)));
            if (r.GeraeteWm2.HasValue && r.FlaecheM2 > 0.0)
                last.Add(new IfcSatzwert(LAST_GERAETE, IfcSatzwertart.Leistung, Math.Round(r.GeraeteWm2.Value * r.FlaecheM2.Value, STELLEN)));
            Hinzu(aus, LAST, last);

            var auslegung = new List<IfcSatzwert>();
            if (r.SollHeizenC.HasValue) auslegung.Add(new IfcSatzwert(AUSLEGUNG_HEIZEN, IfcSatzwertart.Temperatur, r.SollHeizenC));
            if (r.SollKuehlenC.HasValue) auslegung.Add(new IfcSatzwert(AUSLEGUNG_KUEHLEN, IfcSatzwertart.Temperatur, r.SollKuehlenC));
            Hinzu(aus, AUSLEGUNG, auslegung);

            if (r.LichtWm2.HasValue)
                Hinzu(aus, LICHT, new List<IfcSatzwert> { new IfcSatzwert(LICHT_KUNSTLICHT, IfcSatzwertart.Wahrheit, Wahr: r.LichtWm2.Value > 0.0) });
            return aus;
        }

        /// <summary>Der Glasanteil eines Fensters (1 − Rahmenanteil); <c>null</c> ohne Rahmenanteil oder außerhalb 0 … 1.</summary>
        internal static double? Glasanteil(AbbildBauteil b)
            => b?.Rahmenanteil is double f && f >= 0.0 && f <= 1.0 ? Math.Round(1.0 - f, STELLEN) : null;

        /// <summary>Die optischen Werte eines opaken Baustoffs: die Konstanten des Rechenwegs (F8 b).</summary>
        internal static List<IfcSatzwert> Optikwerte() => new List<IfcSatzwert>
        {
            new IfcSatzwert(OPTIK_REFLEXION, IfcSatzwertart.Zahl, Math.Round(1.0 - GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK, STELLEN)),
            new IfcSatzwert(OPTIK_EMISSION, IfcSatzwertart.Zahl, GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN),
        };

        /// <summary>
        /// Die Eigenschaften, die der Leser zählt, aber keinem Feld zuordnet (durchgereicht, Protokoll DA-3) — je Satz.
        /// </summary>
        internal static readonly IReadOnlyList<(string Satz, string Name)> DURCHGEREICHT = new[]
        {
            (BELEGUNG, BELEGUNG_ART), (BELEGUNG, BELEGUNG_SPITZE), (BELEGUNG, BELEGUNG_STUNDEN),
            (LAST, LAST_GERAETE_LATENT), (LAST, LAST_LUFTWECHSEL),
            (AUSLEGUNG, AUSLEGUNG_HEIZLUFT), (AUSLEGUNG, AUSLEGUNG_KUEHLLUFT), (AUSLEGUNG, AUSLEGUNG_LUFTWECHSEL),
            (LICHT, LICHT_BELEUCHTUNGSSTAERKE),
            (SOLLWERTE, SOLL_HOECHST), (SOLLWERTE, SOLL_FEUCHTE), (SOLLWERTE, SOLL_FREIE_LUEFTUNG), (SOLLWERTE, SOLL_MECH_LUEFTUNG),
            (SOLLWERTE, SOLL_KLIMA),
        };

        private static void Hinzu(List<(string, List<IfcSatzwert>)> aus, string satz, List<IfcSatzwert> werte)
        {
            if (werte.Count > 0) aus.Add((satz, werte));
        }
    }
}
