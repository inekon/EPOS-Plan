namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Abschnitt (Streifen) eines inhomogenen Aufbaus — eine Zeile aus <c>Tab_Bauteilabschnitt(_STAMM)</c>
    /// (Schritt 221, gerechnet seit BK-5). Die Schichten eines Abschnitts verweisen über seine <see cref="Nummer"/>
    /// (<see cref="BauteilschichtModel.Abschnitt"/>); in der Datenbank über die ID.
    /// </summary>
    public class BauteilabschnittModel
    {
        /// <summary>Primärschlüssel; 0 = noch nicht gespeichert.</summary>
        public int ID;
        /// <summary>Nummer des Abschnitts je Aufbau, ab 1.</summary>
        public int Nummer;
        /// <summary>Flächenanteil 0…1; <c>null</c> = nicht angegeben (dann rechnet der Aufbau nicht).</summary>
        public double? Flaechenanteil;
        /// <summary>Beschreibung (höchstens 250 Zeichen); <c>null</c> = keine.</summary>
        public string Text;
        /// <summary>Kennung des Quellsatzes eines Imports; <c>null</c> = keine.</summary>
        public string Quellkennung;

        /// <summary>Eine entkoppelte Kopie.</summary>
        public BauteilabschnittModel Kopie() => (BauteilabschnittModel)MemberwiseClone();
    }
}
