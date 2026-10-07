namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Auf welchem Weg die Fläche eines Bauteils entstanden ist</b> (Abstimmung G5, Anforderung A4): ein Wert je
    /// Bauteilfläche, getragen vom Abbild-Bauteil (<see cref="AbbildBauteil.Flaechenherkunft"/>) bis zur Bauteilzeile des
    /// Vorschlags (<see cref="GebaeudeBauteilzeile.Flaechenherkunft"/>). Anders als <see cref="Importherkunft"/> (welche
    /// Quelle: Datei, Katalog, Vorgabe …) nennt er den Weg der Fläche. Ein WERT, kein Anzeigetext; gespeichert wird er
    /// erst mit dem Schemaschritt <c>FlaechenherkunftSchema</c> (<c>Tab_Bauteil.Flaechenherkunft</c>) über
    /// <see cref="FlaechenherkunftWerte.Wert"/>.
    /// </summary>
    internal enum Flaechenherkunft
    {
        /// <summary>Aus einem Mengensatz der Datei (<c>Qto_…</c>, <c>BaseQuantities</c> oder ein Flächenname eines CAD-Satzes).</summary>
        Mengensatz = 0,

        /// <summary>Aus den Polygonen der Raumgrenzen (<c>IfcRelSpaceBoundary</c> mit Geometrie).</summary>
        Raumgrenze = 1,

        /// <summary>Aus dem Bauteilkörper der Datei (<see cref="IfcBauteilkoerper"/>, Stufe G5-1).</summary>
        Koerper = 2,

        /// <summary>Aus der schematischen Vorgabe (Rückfall ohne gelesene Fläche).</summary>
        Schematisch = 3,
    }

    /// <summary>Die gespeicherten Werte der <see cref="Flaechenherkunft"/> (die <c>CHECK</c>-Liste des Schemaschritts).</summary>
    internal static class FlaechenherkunftWerte
    {
        internal const string MENGENSATZ = "MENGENSATZ";
        internal const string RAUMGRENZE = "RAUMGRENZE";
        internal const string KOERPER = "KOERPER";
        internal const string SCHEMATISCH = "SCHEMATISCH";

        /// <summary>Der gespeicherte Wert; <c>null</c> = nicht bestimmt (Bestand).</summary>
        internal static string Wert(Flaechenherkunft? h) => h switch
        {
            Flaechenherkunft.Mengensatz => MENGENSATZ,
            Flaechenherkunft.Raumgrenze => RAUMGRENZE,
            Flaechenherkunft.Koerper => KOERPER,
            Flaechenherkunft.Schematisch => SCHEMATISCH,
            _ => null,
        };
    }
}
