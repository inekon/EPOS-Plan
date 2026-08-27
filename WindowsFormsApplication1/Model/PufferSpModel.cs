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
        }
    }
}
