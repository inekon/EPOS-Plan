namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Die Komponentenart, deren Simulationsparameter der
/// <c>KomponentenKonfigurationDialog</c> zeigt — ein SPRACHNEUTRALER Steuerwert,
/// nie ein Anzeigetext (Drei-Schichten-Regel).
///
/// <para><b>Warum eine eigene Aufzählung und nicht der <c>DbWert</c> der
/// Kartenzeile.</b> Der Dialog entscheidet allein, welche Felder er zeichnet; die
/// Zuordnung <c>DbWerte.ERZEUGER_*</c> → Art trifft die Seite, die die Karten
/// ohnehin führt. So kennt der Dialog keine Datenbankwerte, und ein Test kommt ohne
/// sie aus.</para>
///
/// <para><b>Nur drei Arten haben Parameter.</b> Solarthermie, Photovoltaik,
/// Stromspeicher und Pufferspeicher führen keinen Laufparameter — für sie steht
/// <see cref="Keine"/>, und die Karte trägt gar keinen Knopf: Ein Knopf, der einen
/// leeren Dialog öffnet, ist keiner.</para>
/// </summary>
public enum Komponentenart
{
    /// <summary>Diese Art hat keine Simulationsparameter — kein Knopf, kein Dialog.</summary>
    Keine = 0,

    /// <summary>Wärmepumpe: die Konfiguration der ANLAGE und der projektweite Heizstab.</summary>
    Waermepumpe = 1,

    /// <summary>Heizkessel: die projektweite Betriebsbereitschaft [h/a].</summary>
    Heizkessel = 2,

    /// <summary>BHKW: die projektweite Betriebsart und die untere Leistungsgrenze [%].</summary>
    Bhkw = 3
}
