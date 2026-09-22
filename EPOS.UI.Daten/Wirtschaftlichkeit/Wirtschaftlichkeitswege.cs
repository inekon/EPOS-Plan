using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT der Datenseite von Kosten und Wirtschaftlichkeit (Etappe E3,
    /// Schritt 1): die zwei Wege, die HEUTE noch in einer Windows-Hülle stecken
    /// und deshalb nicht auf jeder Plattform zu haben sind.
    ///
    /// <para><b>Warum es sie gibt.</b> Die vier nahtlosen Hüllen dieses Feldes
    /// führen selbst keine einzige WinForms-Anweisung; sie RUFEN aber zwei
    /// Hüllen, die noch ein eigenes Fenster zeigen und deshalb erst mit
    /// E3 Schritt 6 wandern: <c>GesetzeskatalogHuelle</c>
    /// (<c>Views/Admin/</c>) und <c>PhotovoltaikVerguetungHuelle</c>
    /// (<c>Views/Wirtschaftlichkeit/</c>). Statt der zwei Aufrufe wegen die
    /// ganze Datenseite in der Windows-Anwendung zu lassen, kommen sie als
    /// benannte Naht herein — dasselbe Muster wie <see cref="Katalogwege"/>:
    /// Windows hängt sie in <c>Program.Main</c> ein, iOS lässt sie leer.</para>
    ///
    /// <para><b>Kein Delegat ist kein Knopf.</b> Ohne eingehängten Haken setzt
    /// die Hülle den betroffenen Schlüssel gar nicht erst in ihren
    /// Parametersatz; <c>WirtschaftlichkeitParameterDialog</c> zeigt den
    /// Gesetzeskatalog dann nicht an (<c>@if (GesetzeGaben is not null)</c>),
    /// der Abschnitt „Ertrag/Bonus" keinen Weg in die PV-Vergütung. Alles
    /// Übrige der beiden Masken arbeitet vollständig.</para>
    ///
    /// <para><b>Diese Naht ist auf Abruf.</b> Mit E3 Schritt 6 wandern beide
    /// gerufenen Hüllen selbst nach <c>EPOS.UI.Daten</c> (Fenster-Adapter im
    /// Muster <c>EnergietraegerFenster</c>); dann rufen die Hüllen einander
    /// unmittelbar, und diese Datei entfällt.</para>
    /// </summary>
    internal static class Wirtschaftlichkeitswege
    {
        /// <summary>
        /// Der Parametersatz des Gesetzeskatalogs zu EINER Gesetzesklasse
        /// (<c>DbWerte.GESETZ_KLASSE_*</c>) — unter Windows
        /// <c>GesetzeskatalogHuelle.Gaben(klasse)</c>. <c>null</c> = diese Schale
        /// zeigt den Katalog nicht als Überlagerung an.
        /// </summary>
        internal static Func<string, IReadOnlyDictionary<string, object>> GesetzeskatalogGaben;

        /// <summary>
        /// Öffnet den PV-Vergütungsdialog für ein Projekt
        /// (<c>Tab_Projekt.ID</c>) — unter Windows
        /// <c>PhotovoltaikVerguetungHuelle.Oeffnen(null, idProjekt)</c>, also ein
        /// eigenes Fenster über dem Kostendialog (Risiko R2, deshalb
        /// nachgelagert). <c>null</c> = diese Schale kennt den Weg nicht.
        /// </summary>
        internal static Action<int> PvVerguetungOeffnen;

        // =================================================================
        //  Die Überlagerungen der zwei Seiten (E3 Schritt 3)
        // =================================================================
        //
        // KostenSeiteGaben und WirtschaftlichkeitSeiteGaben liegen seit E3/3 in
        // EPOS.UI.Daten. Ihre Unterdialoge kommen bereits als Gaben in eine
        // Ueberlagerung - nur liegen die Huellen, die diese Gaben bauen, noch in
        // der Schale, weil sie DANEBEN ein eigenes Fenster zeigen (E3 Schritt 5
        // und 6). Bis dahin holt die Seite sie hier ab; ohne Haken liefert sie
        // null, und die Ueberlagerung erscheint nicht.

        /// <summary>
        /// Der Parametersatz der Kostenverwaltung im PROJEKTMODUS
        /// (<c>idProjekt</c>, <c>projektname</c>, <c>komponente</c>,
        /// <c>betrieb</c>, <c>idAnlage</c>) — unter Windows
        /// <c>KostenKomponenteHuelle.GabenProjekt</c>. Wandert mit E3 Schritt 5.
        /// </summary>
        internal static Func<int, string, string, bool, int, IReadOnlyDictionary<string, object>>
            KostenVerwaltungGaben;

        /// <summary>
        /// Der Parametersatz des PV-Vergütungsdialogs zu einem Stammprojekt —
        /// unter Windows <c>PhotovoltaikVerguetungHuelle.Gaben</c>.
        /// Wandert mit E3 Schritt 6.
        /// </summary>
        internal static Func<int, IReadOnlyDictionary<string, object>> PvVerguetungGaben;

        /// <summary>
        /// Der Parametersatz des Sammeldialogs „BHKW-Wirtschaftlichkeit" zu einem
        /// Stammprojekt und den Ergebnissen des letzten Laufs — unter Windows
        /// <c>BhkwWirtschaftlichkeitHuelle.Gaben</c>. Dessen Titel wertet die
        /// Seite nicht aus (sie zeigt den Dialog als Überlagerung); die Schale
        /// verwirft ihn beim Einhängen. Wandert mit E3 Schritt 6.
        /// </summary>
        internal static Func<int, List<WirtschaftlichkeitErgebnis>,
                             IReadOnlyDictionary<string, object>> BhkwGaben;

        /// <summary>
        /// Der Parametersatz der Tarifstruktur zu einem Stammprojekt und einer
        /// Sicht — unter Windows <c>TarifstrukturHuelle.Gaben</c>.
        /// Wandert mit E3 Schritt 6.
        /// </summary>
        internal static Func<int, TarifSicht, IReadOnlyDictionary<string, object>> TarifGaben;

        /// <summary>
        /// Der Parametersatz des Kapitalwertverlaufs — unter Windows
        /// <c>KapitalwertVerlaufHuelle.Gaben</c>. Der Rückgabeweg
        /// <c>neuGesammelt</c> sagt der Seite, ob der Verlauf die Gruppe neu
        /// gesammelt hat; sie frischt dann ihre Zeilen auf. Wandert mit
        /// E3 Schritt 6.
        /// </summary>
        internal static VerlaufGabenWeg VerlaufGaben;

        /// <summary>
        /// Die Bauform von <see cref="VerlaufGaben"/> — ein <c>Func&lt;&gt;</c>
        /// trägt kein <c>out</c>.
        /// </summary>
        internal delegate IReadOnlyDictionary<string, object> VerlaufGabenWeg(
            int idStamm, string stammName, List<int> variantenIds,
            out Func<bool> neuGesammelt);
    }
}
