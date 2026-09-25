using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Zone eines Projektgebäudes samt ihrer Bauteile — eine Zeile aus <c>Tab_Zone</c>
    /// (Schemaschritt S-C, <see cref="ZonenSchema"/>).
    ///
    /// <para><b>NULL heißt „Wert des Gebäudes"</b> bei allen Sollwert-, Lüftungs-, Kühl- und
    /// Übergabefeldern; die Feldnamen spiegeln die Spaltennamen des Gebäudes buchstabengetreu.
    /// <see cref="Interne_Waermegewinne"/> und <see cref="Bewohner"/> NULL heißen „anteilig aus dem
    /// Gebäude über den Flächenschlüssel".</para>
    ///
    /// <para><b>Vorläufige Zeilen tragen eine NEGATIVE Id</b> (Muster A6): Der Schreibweg
    /// <c>GebaeudeZonenCtrl.SpeichernJeGebaeude</c> gleicht über die Ids ab — eine positive Id
    /// wird geändert, eine negative oder 0 angelegt, eine fehlende entfernt. Nach dem Speichern
    /// trägt das Modell die vergebene Id.</para>
    /// </summary>
    public class ZoneModel
    {
        /// <summary>Primärschlüssel; ≤ 0 = vorläufig (neu anzulegen).</summary>
        public int ID;

        /// <summary>Das Projektgebäude (<c>Tab_Gebaeude.ID</c>) — Eltern, Kaskade.</summary>
        public int ID_Gebaeude;

        /// <summary>Reihenfolge, lückenlos ab 1 — vergibt der Schreibweg aus der Listenreihenfolge.</summary>
        public int Rang;

        /// <summary>Name der Zone (Pflicht, höchstens 80 Zeichen).</summary>
        public string Bezeichner = "";

        /// <summary>Nutzfläche [m²]; <c>null</c> = aus den Bauteilen.</summary>
        public double? Nutzflaeche;

        /// <summary>Raumhöhe [m]; <c>null</c> = Raumhöhe des Gebäudes.</summary>
        public double? Raumhoehe;

        /// <summary>Volumen [m³]; <c>null</c> = Fläche × Höhe.</summary>
        public double? Volumen;

        /// <summary>Die Zone wird beheizt (Vorgabe: ja).</summary>
        public bool IstBeheizt = true;

        /// <summary>Raumsollwert am Tag [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Raumsolltemperatur_Tag;

        /// <summary>Raumsollwert der Nachtabsenkung [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Raumsolltemperatur_Nachtabsenkung;

        /// <summary>Raumsollwert am Wochenende [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Raumsolltemperatur_Wochenende;

        /// <summary>Raumsollwert in den Ferien [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Raumsolltemperatur_Ferien;

        /// <summary>Höchste Raumtemperatur [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Maximaleraumtemperatur;

        /// <summary>Strahlungsanteil der Heizung [-]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Heizung_Strahlungsanteil;

        /// <summary>Leistungsgrenze der Heizung [kW]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Heizleistung_Max;

        /// <summary>Luftwechsel durch Infiltration [1/h]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Luftwechsel_Infiltration;

        /// <summary>Luftwechsel durch die Nutzer [1/h]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Luftwechsel_Nutzer;

        /// <summary>Innere Wärmegewinne; <c>null</c> = anteilig aus dem Gebäude über den Flächenschlüssel.</summary>
        public double? Interne_Waermegewinne;

        /// <summary>Bewohner; <c>null</c> = anteilig aus dem Gebäude über den Flächenschlüssel.</summary>
        public double? Bewohner;

        /// <summary>KU-S1: Kühlsollwert [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Kuehl_Sollwert;

        /// <summary>KU-S1: Leistungsgrenze der Kühlung [kW]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Kuehlleistung_Max;

        /// <summary>KU-S1: Schalter „wird gekühlt"; <c>null</c> = Wert des Gebäudes.</summary>
        public bool? Kuehlung_Aktiv;

        /// <summary>KU-S1: Kühlsollwert der Nacht [°C]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Kuehl_Sollwert_Nacht;

        /// <summary>AK-S1: Übergabeart (<c>DbWerte.UEBERGABE_*</c>); <c>null</c> = Wert des Gebäudes.</summary>
        public string Uebergabe_Art;

        /// <summary>AK-S1: Exponent der Übergabe; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Uebergabe_Exponent;

        /// <summary>AK-S1: Nennleistung der Übergabe [kW]; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Uebergabe_Leistung_Nenn;

        /// <summary>KAK-S1 (E37, Schritt 137): Kühlübergabeart (<c>DbWerte.KUEHLUEBERGABE_*</c>, auch <c>IDEAL</c>); <c>null</c> = Wert des Gebäudes.</summary>
        public string Kuehl_Uebergabe_Art;

        /// <summary>KAK-S1: Exponent der Kühlübergabe; <c>null</c> = Wert des Gebäudes.</summary>
        public double? Kuehl_Uebergabe_Exponent;

        /// <summary>KAK-S1: Nennleistung der Kühlübergabe [kW], sensibel; <c>null</c> = Anteil der Zonenfläche.</summary>
        public double? Kuehl_Uebergabe_Leistung_Nenn;

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); <c>null</c> = nicht angegeben.</summary>
        public string Herkunft;

        /// <summary>IFC-GUID oder gbXML-id der Quellentität; <c>null</c> = keine.</summary>
        public string Quellkennung;

        /// <summary>Die Bauteile der Zone; beim Speichern gilt die Listenreihenfolge als Rang.</summary>
        public List<BauteilModel> Bauteile = new List<BauteilModel>();

        /// <summary>Eine entkoppelte Kopie samt Bauteilen.</summary>
        public ZoneModel Kopie()
        {
            var k = (ZoneModel)MemberwiseClone();
            k.Bauteile = Bauteile.Select(b => b.Kopie()).ToList();
            return k;
        }
    }
}
