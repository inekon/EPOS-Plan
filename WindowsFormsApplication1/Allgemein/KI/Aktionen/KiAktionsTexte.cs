namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sichtbaren Texte des Aktionsregisters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TODO(B5): Alle Konstanten dieser Klasse sind auf <c>MyResource.Resource</c>
    /// umzustellen (Drei-Schichten-Regel, <c>WindowsFormsApplication1\CLAUDE.md</c>,
    /// Anzeigeschicht). In diesem Paket bleiben sie bewusst deutschsprachig fest
    /// verdrahtet, weil <c>MyResource\Resource.resx</c> und die beiden Satellitendateien
    /// gerade von einem anderen Paket bearbeitet werden. Die Texte stehen deshalb
    /// vollstaendig an EINER Stelle - der Umstieg ist dann eine Ersetzung je Konstante.
    /// </para>
    /// <para>
    /// NICHT betroffen sind Persistenz- und Schluesselwerte: Gewerknamen,
    /// Kostenkomponenten und Merkmalsschluessel bleiben deutsch und eingefroren und kommen
    /// aus <see cref="DbWerte"/> bzw. aus der Landkarte des jeweiligen Controllers.
    /// </para>
    /// </remarks>
    internal static class KiAktionsTexte
    {
        // ================================================================ Parameter

        internal const string ProjektIdName = "Projekt (ID)";
        internal const string ProjektIdErlaeuterung =
            "Schlüssel des Projekts, wie ihn projekte_auflisten liefert.";

        internal const string VonProjektName = "Quellprojekt (ID)";
        internal const string NachProjektName = "Zielprojekt (ID)";
        internal const string GewerkName = "Gewerk";
        internal const string KomponenteName = "Komponente";
        internal const string MerkmalName = "Merkmal";
        internal const string DateipfadName = "Dateipfad";
        internal const string GanglinieName = "Ganglinie (ID)";
        internal const string AnzahlName = "Anzahl";
        internal const string ProjekteName = "Projekte";
        internal const string KapazitaetName = "Nutzbare Kapazität";
        internal const string LeistungName = "Lade-/Entladeleistung";
        internal const string WirkungsgradName = "Round-Trip-Wirkungsgrad";
        internal const string SocMinName = "Untere Bandgrenze";
        internal const string SocMaxName = "Obere Bandgrenze";

        // ================================================================ Zwecke

        internal const string ZweckProjekteAuflisten =
            "Listet alle Projekte der Datenbank mit Name, Kunde und Änderungsdatum.";
        internal const string ZweckProjektLesen =
            "Liest die Kopfdaten eines Projekts (Name, Kunde, Bearbeiter, Klimaregion).";
        internal const string ZweckVariantenAuflisten =
            "Listet Stammprojekt und alle Varianten einer Vergleichsgruppe.";
        internal const string ZweckSpeichervariantenAuflisten =
            "Listet die Stromspeicher-Varianten eines Projekts und sagt, welche aktiv ist.";
        internal const string ZweckErgebnisseLesen =
            "Liest die gespeicherten Wirtschaftlichkeitsergebnisse mehrerer Projekte.";
        internal const string ZweckParameterLesen =
            "Liest Wirtschaftlichkeitsparameter und Stromtarif eines Stammprojekts.";
        internal const string ZweckKostenlagePruefen =
            "Vergleicht die erfasste Investitionsposition einer Komponente mit den Technik-Planwerten.";
        internal const string ZweckUebernahmeVorschau =
            "Zeigt, was die Übernahme eines Gewerks von einem Projekt in ein anderes ändern würde. Schreibt nichts.";
        internal const string ZweckMerkmalVorschau =
            "Zeigt, ob und wie ein einzelnes Merkmal von einem Projekt in ein anderes übernommen werden könnte. Schreibt nichts.";
        internal const string ZweckLastgangPruefen =
            "Prüft eine Lastgangdatei: Format, Spalten, Raster und Lesbarkeit. Importiert nichts.";
        internal const string ZweckGanglinienAuflisten =
            "Listet die wählbaren Stromganglinien eines Projekts und des Stammkatalogs.";
        internal const string ZweckMinimaleSpitze =
            "Ermittelt die kleinste Netzbezugsspitze, die ein Speicher über den ganzen Lastgang halten kann.";
        internal const string ZweckLetzteAktionen =
            "Nennt die zuletzt ausgeführten Assistentenaktionen dieser Sitzung.";

        // ================================================================ Erlaeuterungen

        internal const string ErlVonProjekt = "Schlüssel des Projekts, aus dem übernommen würde.";
        internal const string ErlNachProjekt = "Schlüssel des Projekts, in das übernommen würde.";
        internal const string ErlGewerk = "Gewerk der Komponenten-Übernahme.";
        internal const string ErlKomponente = "Kostenkomponente, deren Investitionsposition geprüft wird.";
        internal const string ErlMerkmal =
            "Schlüssel des Merkmals als Tabelle.Spalte, z. B. Tab_WP.Bauart. " +
            "Unbekannte Schlüssel werden mit der vollständigen Liste beantwortet.";
        internal const string ErlDateipfad = "Vollständiger Pfad der zu prüfenden Datei (CSV oder Excel).";
        internal const string ErlGanglinieId = "Schlüssel der Ganglinie aus ganglinien_auflisten.";
        internal const string ErlProjekteFuerErgebnisse =
            "Schlüssel der Projekte, deren Ergebnisse gelesen werden sollen.";
        internal const string ErlProjektIdGanglinien =
            "Schlüssel des Projekts; ohne Angabe erscheinen nur die Ganglinien des Stammkatalogs.";
        internal const string ErlProjektIdGanglinieSuche =
            "Schlüssel des Projekts, falls die Ganglinie eine Projektganglinie ist.";
        internal const string ErlAnzahl = "Wie viele der zuletzt ausgeführten Aktionen genannt werden sollen.";
        internal const string ErlKapazitaet = "Nutzbare Speicherkapazität.";
        internal const string ErlLeistung = "Lade- und Entladeleistung des Speichers.";
        internal const string ErlWirkungsgrad = "Round-Trip-Wirkungsgrad des Speichers.";
        internal const string ErlSocMin = "Untere Grenze des nutzbaren Ladebands.";
        internal const string ErlSocMax = "Obere Grenze des nutzbaren Ladebands.";

        // ================================================================ Meldungen

        internal const string ProjektUnbekannt = "Es gibt kein Projekt mit der Nummer {0}.";
        internal const string ProjekteKeine = "In der Datenbank steht kein Projekt.";
        internal const string ProjekteGefunden = "{0} Projekte gefunden.";
        internal const string ProjektGelesen = "Projekt {0}: {1}.";
        internal const string VariantenGruppe = "Vergleichsgruppe von „{0}“: {1}.";
        internal const string VarianteAufgeloest =
            "Projekt {0} ist selbst eine Variante; ich habe zum Stammprojekt {1} aufgelöst.";
        internal const string EinzelnesProjekt = "„{0}“ hat keine Varianten.";
        internal const string SpeichervariantenKeine =
            "Das Projekt führt keine Stromspeicher-Variante.";
        internal const string SpeichervariantenGefunden = "{0} Speichervariante(n), aktiv: {1}.";
        internal const string SpeichervarianteKeineAktive = "keine";
        internal const string SpeicherTabelleFehlt =
            "Die Tabellen des Stromspeicher-Moduls fehlen in dieser Datenbank.";
        internal const string ErgebnisseKeine =
            "Zu diesen Projekten ist kein Wirtschaftlichkeitsergebnis gespeichert — bitte zuerst rechnen.";
        internal const string ErgebnisseGefunden = "{0} Ergebnis(se) zu {1} Projekt(en); {2} davon aktuell.";
        internal const string ParameterGelesen =
            "Parametersatz zu Projekt {0} gelesen; Stromtarif {1}.";
        internal const string TarifAktiv = "aktiv";
        internal const string TarifAus = "aus";
        internal const string KomponenteUnbekannt =
            "Die Komponente „{0}“ führt keine Technik-Planwerte.";
        internal const string KomponenteNichtVerbaut =
            "Im Projekt {0} ist „{1}“ nicht verbaut.";
        internal const string KostenlageOhneKomponente =
            "In Tab_KostenKomponente gibt es keinen Eintrag für „{0}“.";
        internal const string KostenlagePasst =
            "Die erfasste Position von „{0}“ passt zu den Technik-Planwerten.";
        internal const string KostenlageAbweichend =
            "Die erfasste Position von „{0}“ weicht von den Technik-Planwerten ab.";
        internal const string GleicheProjekte =
            "Quell- und Zielprojekt müssen verschiedene Projekte sein.";
        internal const string GewerkNichtUnterstuetzt =
            "Das Gewerk „{0}“ wird nicht unterstützt. Möglich sind: {1}.";
        internal const string UebernahmeMoeglich =
            "Übernahme möglich: {0} anlegen, {1} ersetzen, {2} entfernen.";
        internal const string UebernahmeNichtMoeglich = "Übernahme nicht möglich: {0}";
        internal const string MerkmalUnbekannt =
            "Das Merkmal „{0}“ kenne ich nicht. Möglich sind: {1}.";
        internal const string MerkmalMoeglich =
            "Übernahme möglich: „{0}“ würde von {1} auf {2} gesetzt.";
        internal const string MerkmalGleichstand =
            "Quelle und Ziel führen bei „{0}“ bereits denselben Wert ({1}).";
        internal const string MerkmalNichtMoeglich = "Übernahme nicht möglich: {0}";
        internal const string DateiFehlt = "Die Datei „{0}“ gibt es nicht.";
        internal const string LastgangLesbar =
            "Datei lesbar: {0} Spalte(n), Vorschlag Wertspalte {1}, Raster {2}.";
        internal const string LastgangNichtLesbar = "Die Datei ist so nicht lesbar.";
        internal const string GanglinienKeine = "Es sind keine Ganglinien vorhanden.";
        internal const string GanglinienGefunden = "{0} Ganglinie(n): {1} aus dem Projekt, {2} aus dem Katalog.";
        internal const string GanglinieUnbekannt =
            "Es gibt keine Ganglinie mit der Nummer {0}.";
        internal const string GanglinieLeer = "Die Ganglinie {0} enthält keine Werte.";
        internal const string SpitzeErmittelt =
            "Kleinste haltbare Spitze: {0} kW (Ausgangsspitze {1} kW, Ersparnis {2} kW).";
        internal const string SocVerdreht =
            "Die untere Bandgrenze muss kleiner als die obere sein.";
        internal const string LetzteAktionenKeine = "In dieser Sitzung wurde noch keine Aktion ausgeführt.";
        internal const string LetzteAktionenGefunden = "{0} zuletzt ausgeführte Aktion(en).";
    }
}
