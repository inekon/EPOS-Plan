using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class PufferSpModel
    {
        public int ID;
        public string Name;
        public string Firma;
        public string Speichertyp;
        public double Betriebsbereitschaftverlust;
        public int Gesamtvolumen;
        public double Investitionskosten;

        // =====================================================================
        // Klassen-Set (Migrationsschritt 49, Paket K2)
        //   Tab_Pufferspeicher.Nutzung_Heizung / _Brauchwasser / _Prozess
        //   Konzept Brauchwasser/Heizung/Pufferspeicher § 6.1, Entscheidung
        //   F5-Alternative / L6 vom 27.08.2026
        //
        //   Die drei Flags lösen die EINWERTIGE Spalte Verwendung ab: Ein Speicher
        //   bediente bisher entweder Heizung oder Brauchwasser oder - als „Kombi" -
        //   beides. Jetzt trägt er ein SET aus bis zu drei unabhängigen Klassen,
        //   womit auch {Heizung, Prozess} oder {H, B, P} möglich sind. „Kombi" ist
        //   nur noch der ANZEIGENAME des Sets {Heizung, Brauchwasser} und kein
        //   eigener Persistenzwert mehr; Verwendung bleibt als Lese-Altlast stehen
        //   und wird beim Speichern als abgeleiteter Altwert mitgeschrieben.
        //
        //   RÜCKFALLREGEL. Fehlen die Spalten (Datenbank noch nicht auf Schemastand
        //   49) oder ist das gelesene Set LEER, leitet der Controller es aus
        //   Verwendung ab: Heizung -> {H}, Brauchwasser -> {B}, Kombi -> {H, B},
        //   Quelle/leer/unbekannt -> {H}. Ein leeres Set ist damit nie ein
        //   Datenzustand, sondern immer nur „noch nicht gesetzt" - ein Speicher, den
        //   niemand entlädt, wäre fachlich sinnlos, und der Dialog erzwingt deshalb
        //   mindestens ein Häkchen (Konzept 6.1).
        //
        //   Gelesen wird spaltentolerant in PufferSpCtrl.KlassenSetAusZeile,
        //   geschrieben über PufferSpCtrl.KlassenSetSchreiben - ein eigenes,
        //   zielgenaues UPDATE, damit ein noch nicht migrierter Bestand das
        //   Speichern der ganzen Puffer-Zeile nicht scheitern lässt.
        // =====================================================================

        /// <summary>Klassen-Set: Der Speicher bedient den Heizkanal.</summary>
        public bool Nutzung_Heizung;

        /// <summary>Klassen-Set: Der Speicher bedient den Brauchwasserkanal.</summary>
        public bool Nutzung_Brauchwasser;

        /// <summary>Klassen-Set: Der Speicher bedient den Prozesswärmekanal.</summary>
        public bool Nutzung_Prozess;

        // =====================================================================
        // Schichtung und Leistungsgrenzen (Migrationsschritt 53, Paket P1)
        //   Tab_Pufferspeicher.Schichten_Anzahl, Hoehe, Lambda_Eff, T_Nutz_BW,
        //   Entnahme_Heizung, Entnahme_BW, Entnahme_Prozess,
        //   Ladeleistung_Max, Entladeleistung_Max
        //   Konzept Brauchwasser/Heizung/Pufferspeicher § 7.2 (Zustand und
        //   Parameter), § 6.3 (Lade-/Entladeleistung)
        //
        //   ALLE Vorbelegungen sind VERHALTENSNEUTRAL: N = 1 ist das heutige
        //   Ein-Zonen-Modell, die NULL-Werte bedeuten „Standard" (Höhe aus dem
        //   H/D-Verhältnis 2,5, Lambda 1,5 W/(m·K), T_Nutz = Rücklauf,
        //   Entnahmehöhen nach Kanal), und 0 kW heißt „unbegrenzt". Ein Bestand
        //   ohne gepflegte Schichtdaten rechnet damit exakt wie vor dem Paket.
        //
        //   NULLBARE Felder, wo NULL eine eigene Bedeutung hat: Eine 0 in Hoehe
        //   oder T_Nutz_BW wäre eine ANGABE („0 m", „0 °C") und nicht dasselbe
        //   wie „nicht gepflegt". Bei den beiden Leistungsgrenzen ist es
        //   umgekehrt - dort IST 0 die Bedeutung „unbegrenzt", und ein zweiter
        //   Zustand daneben wäre überflüssig.
        //
        //   Gelesen wird spaltentolerant über PufferSpCtrl.SchichtdatenAusZeile,
        //   geschrieben über PufferSpCtrl.SchichtdatenSchreiben - ein eigenes,
        //   zielgenaues UPDATE nach dem Muster des Klassen-Sets, damit ein noch
        //   nicht migrierter Bestand das Speichern der ganzen Puffer-Zeile nicht
        //   scheitern lässt.
        // =====================================================================

        /// <summary>Kleinste zulässige Schichtenzahl und zugleich die Vorbelegung (Ein-Zonen).</summary>
        public const int SCHICHTEN_DEFAULT = 1;

        /// <summary>Größte zulässige Schichtenzahl (Konzept 7.2: „N 1…10").</summary>
        public const int SCHICHTEN_MAX = 10;

        /// <summary>Schichtenzahl 1…10; 1 = Ein-Zonen-Speicher wie im Bestand.</summary>
        public int Schichten_Anzahl;

        /// <summary>Behälterhöhe [m]; <c>null</c> = aus dem H/D-Verhältnis 2,5 rechnen.</summary>
        public double? Hoehe;

        /// <summary>Effektive vertikale Wärmeleitfähigkeit [W/(m·K)]; <c>null</c> = 1,5.</summary>
        public double? Lambda_Eff;

        /// <summary>Mindest-Nutztemperatur des Brauchwasserkanals [°C]; <c>null</c> = Rücklauf.</summary>
        public double? T_Nutz_BW;

        /// <summary>Entnahmehöhe Heizung 0…1 (0 = unten, 1 = oben); <c>null</c> = Standard.</summary>
        public double? Entnahme_Heizung;

        /// <summary>Entnahmehöhe Brauchwasser 0…1; <c>null</c> = Standard.</summary>
        public double? Entnahme_BW;

        /// <summary>Entnahmehöhe Prozesswärme 0…1; <c>null</c> = Standard.</summary>
        public double? Entnahme_Prozess;

        /// <summary>Größte Ladeleistung [kW]; 0 = unbegrenzt.</summary>
        public double Ladeleistung_Max;

        /// <summary>Größte Entladeleistung [kW]; 0 = unbegrenzt.</summary>
        public double Entladeleistung_Max;

        public PufferSpModel()
        {
            ID = 0;
            Name = "";
            Firma = "";
            Speichertyp = "";
            Betriebsbereitschaftverlust = 0;
            Gesamtvolumen = 0;
            Investitionskosten = 0;

            // Vorbelegung = Rückfallregel für eine unbekannte Verwendung: {Heizung}.
            // Ein frisch erzeugtes Modell trägt damit nie das leere Set.
            Nutzung_Heizung = true;
            Nutzung_Brauchwasser = false;
            Nutzung_Prozess = false;

            // Schichtung (Schritt 53): das verhaltensneutrale Ein-Zonen-Modell.
            Schichten_Anzahl = SCHICHTEN_DEFAULT;
            Hoehe = null;
            Lambda_Eff = null;
            T_Nutz_BW = null;
            Entnahme_Heizung = null;
            Entnahme_BW = null;
            Entnahme_Prozess = null;
            Ladeleistung_Max = 0;
            Entladeleistung_Max = 0;
        }
    }
}
