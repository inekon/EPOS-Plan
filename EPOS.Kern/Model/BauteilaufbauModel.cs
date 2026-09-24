using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Bauteilaufbau samt seiner Schichten — eine Zeile aus <c>Tab_Bauteilaufbau_STAMM</c>
    /// (Katalog) oder <c>Tab_Bauteilaufbau</c> (Projektkopie); Schemaschritt S-B,
    /// <see cref="BauteilaufbauSchema"/>.
    ///
    /// <para><b>Der Aufbau ist ein Aggregat:</b> Er wird mit seinen <see cref="Schichten"/>
    /// gelesen, geschrieben und kopiert. Ein Aufbau ohne seine Schichten ist ein Aufbau ohne
    /// U-Wert (Softwarearchitektur 2.6).</para>
    /// </summary>
    public class BauteilaufbauModel
    {
        /// <summary>Primärschlüssel; 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>Projekt der Kopie; <c>null</c> = Katalogsatz.</summary>
        public int? ID_Projekt;

        /// <summary>Name des Aufbaus (Pflicht, höchstens 80 Zeichen).</summary>
        public string Bezeichner = "";

        /// <summary>Beschreibung (höchstens 250 Zeichen); <c>null</c> = keine.</summary>
        public string Beschreibung;

        /// <summary>Bauteilart, für die der Aufbau gedacht ist; <c>null</c> = für jede.</summary>
        public string Bauteilart;

        /// <summary>Regelwerk oder Dateiname des Imports; <c>null</c> = nicht angegeben.</summary>
        public string Quelle;

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); die Schichten erben sie.</summary>
        public string Herkunft;

        /// <summary>Kennung der Quellentität eines Imports; <c>null</c> = keine.</summary>
        public string Quellkennung;

        /// <summary>„Gehört zur Auslieferung" — nur im Katalog.</summary>
        public bool ReadOnly;

        /// <summary>Die Schichten, innen → außen; beim Speichern gilt die Listenreihenfolge.</summary>
        public List<BauteilschichtModel> Schichten = new List<BauteilschichtModel>();

        /// <summary>Summe der Schichtdicken [m].</summary>
        public double Gesamtdicke => Schichten.Sum(s => s.Dicke);

        /// <summary>Eine entkoppelte Kopie samt Schichten.</summary>
        public BauteilaufbauModel Kopie()
        {
            var k = (BauteilaufbauModel)MemberwiseClone();
            k.Schichten = Schichten.Select(s => s.Kopie()).ToList();
            return k;
        }
    }
}
