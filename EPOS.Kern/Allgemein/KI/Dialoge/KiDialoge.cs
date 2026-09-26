using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sprachneutralen Schluessel des Dialogkatalogs — die EINE Stelle, an der ein
    /// Razor-Dialog erfaehrt, unter welchem Namen er in der Maskenbruecke steht
    /// (Auftrag #200, Stufe S2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum die Namen bleiben, was sie waren.</b> <c>Form_Heizkessel_Bearbeiten</c>,
    /// <c>Form_PV</c>, <c>Form_PufferSp_Bearbeiten</c> und <c>Form_WP</c> sind Typnamen
    /// gefallener WinForms-Masken; als KATALOGSCHLUESSEL sind sie trotzdem richtig. Sie
    /// stehen im Aktionsprotokoll, im Werkzeugvertrag des Modells
    /// (<c>dialog_lesen(maske: "Form_PV")</c>) und in den Hilfeschluesseln der Dialoge
    /// (<c>Form_PV.btn_Help</c>) — sie umzubenennen hiesse, alle drei zugleich zu
    /// brechen, und zwar ohne fachlichen Gewinn. Die FUENFTE Maske ist deshalb auch die
    /// erste ohne <c>Form_</c>-Vorsilbe: Sie hatte nie eine WinForms-Fassung.
    /// </para>
    /// <para>
    /// <b>Die Zuordnung Razor-Dialog → Maskenname steht hier und nicht im Dialog.</b>
    /// Jede der fuenf Komponenten nennt beim Anmelden ihre Konstante; eine Zeichenkette
    /// im Markup waere die naechste Stelle, an der sich ein Tippfehler erst zur Laufzeit
    /// zeigt.
    /// </para>
    /// </remarks>
    public static class KiMaskennamen
    {
        /// <summary>Heizkessel-Katalogeditor (<c>HeizkesselKatalogDialog</c>).</summary>
        public const string HEIZKESSEL = "Form_Heizkessel_Bearbeiten";

        /// <summary>Photovoltaik-Projektdialog (<c>PhotovoltaikDialog</c>).</summary>
        public const string PHOTOVOLTAIK = "Form_PV";

        /// <summary>
        /// Die Ueberlagerung „Anlagenwerte" der Photovoltaik (<c>PvStraengeFelder</c>,
        /// Anwenderentscheid 21.09.2026, KI-D-Q7).
        /// </summary>
        /// <remarks>
        /// Sie ist ein eigenes Fenster mit eigenem Titel, eigenem Arbeitsstand und
        /// eigener Knopfleiste — und bekommt deshalb nach der Regel „Baustein in
        /// eigenem Fenster bekommt einen Schluessel" einen eigenen Katalogeintrag
        /// statt vier Feldern an <see cref="PHOTOVOLTAIK"/>.
        /// </remarks>
        public const string PV_ANLAGENWERTE = "Form_PV_Anlagenwerte";

        /// <summary>Pufferspeicher-Katalogeditor (<c>PufferSpKatalogDialog</c>).</summary>
        public const string PUFFERSPEICHER = "Form_PufferSp_Bearbeiten";

        /// <summary>Waermepumpen-Katalogeditor (<c>WaermepumpeStammDialog</c>).</summary>
        public const string WAERMEPUMPE = "Form_WP";

        /// <summary>Stromspeicher-Auslegung (<c>StromspeicherAuslegungSeite</c>).</summary>
        public const string STROMSPEICHER_AUSLEGUNG = "StromspeicherAuslegung";

        /// <summary>
        /// Die Ansicht „Simulation" (<c>SimulationSeite</c>, Auftrag #221) — die SECHSTE
        /// Maske und die zweite ohne <c>Form_</c>-Vorsilbe.
        /// </summary>
        public const string SIMULATION = "Simulation";

        /// <summary>
        /// Die Kostenverwaltung einer Komponente (<c>KostenKomponenteDialog</c>) — die
        /// SIEBTE Maske und die erste mit einem RASTER (Anwenderbefund vom 14.09.2026).
        /// </summary>
        /// <remarks>
        /// Sie ist der Anlass fuer die Spaltenform des Eigenschaftspfades
        /// (<see cref="KiEigenschaftspfad.Sammlungszeichen"/>): Ihre Werte stehen nicht
        /// in Einzelfeldern, sondern in einer Liste von Positionen. Mit den bisherigen
        /// zwei Pfadstufen liess sich davon kein Wert benennen - der Assistent
        /// antwortete deshalb auf „wie ist die Nutzungsdauer in diesem Projekt?" mit
        /// einem VORGABEWERT aus der Dokumentation, waehrend in der offenen Maske
        /// etwas anderes stand.
        /// </remarks>
        public const string KOSTENVERWALTUNG = "Kostenverwaltung";

        // =================================================================
        //  Welle KI-F1: die Erzeugermasken des PROJEKTS
        // =================================================================
        //
        // Sie stehen neben den KATALOGeditoren gleichen Gewerks und sind
        // etwas anderes: Der Editor pflegt einen Satz des Katalogs, diese
        // Maske pflegt die ANLAGE im Projekt - Vorlauf, Ruecklauf,
        // Grenzleistung, Modulzahl. Deshalb tragen beide ihren eigenen
        // Schluessel, und beide sind die WinForms-Maskennamen des Bestands
        // (Form_Heizkessel gegen Form_Heizkessel_Bearbeiten).

        /// <summary>Heizkessel im Projekt (<c>HeizkesselDialog</c>).</summary>
        public const string HEIZKESSEL_PROJEKT = "Form_Heizkessel";

        /// <summary>BHKW im Projekt (<c>BhkwDialog</c>).</summary>
        public const string BHKW_PROJEKT = "Form_BHKWEing";

        /// <summary>Pufferspeicher im Projekt (<c>PufferspeicherDialog</c>).</summary>
        public const string PUFFERSPEICHER_PROJEKT = "Form_PufferSp";

        /// <summary>Stromspeicher im Projekt (<c>StromspeicherDialog</c>).</summary>
        public const string STROMSPEICHER_PROJEKT = "Form_Stromspeicher";

        /// <summary>Solarkollektoren im Projekt (<c>SolarkollektorenDialog</c>).</summary>
        public const string SOLARKOLLEKTOREN_PROJEKT = "Form_SolarKollektoren";

        /// <summary>
        /// Die Waermepumpen-ANLAGE eines Projekts (<c>WaermepumpeAnlageDialog</c>)
        /// samt ihren Teilbausteinen Konfiguration und Stammfelder.
        /// </summary>
        /// <remarks>
        /// Sie steht neben <see cref="WAERMEPUMPE"/>, der Stammverwaltung: Die pflegt
        /// einen Satz des Katalogs, diese hier die Anlage im Projekt.
        /// </remarks>
        public const string WAERMEPUMPE_ANLAGE = "Form_WP_Anlage";

        // =================================================================
        //  Welle KI-F2: die Masken der SIMULATIONSKONFIGURATION
        // =================================================================
        //
        // Sie gehen aus Schritt ① der Ansicht "Simulation" auf - je Karte
        // die Maske, die zu ihr gehoert: die Pufferverwaltung des Projekts,
        // die zwei Quellenmasken der Waermepumpe, das Quellprofil, die
        // Waermesenken und die Konfiguration einer Komponente. Alle sechs
        // brauchen eine gewaehlte Komponente und lassen sich nicht
        // kontextfrei oeffnen; ihr Ziel ist deshalb die Ansicht selbst.

        /// <summary>
        /// Die Pufferspeicher-Verwaltung des Projekts
        /// (<c>PufferSpProjektDialog</c>).
        /// </summary>
        /// <remarks>
        /// Sie steht neben <see cref="PUFFERSPEICHER"/> (dem Katalogeditor) und
        /// <see cref="PUFFERSPEICHER_PROJEKT"/> (der Erzeugerkarte des Projekts) und
        /// ist etwas Drittes: Hier entstehen und vergehen die Speicher des Projekts
        /// samt Schichtmodell, Schwellen und Leistungsgrenzen. Alle drei tragen die
        /// WinForms-Maskennamen des Bestands.
        /// </remarks>
        public const string PUFFERSPEICHER_VERWALTUNG = "Form_PufferSp_Projekt";

        /// <summary>Waermequelle Erdreich einer Waermepumpe (<c>QuelleErdreichDialog</c>).</summary>
        public const string QUELLE_ERDREICH = "Form_QuelleErdreich";

        /// <summary>Waermequelle Pufferspeicher (<c>QuellePufferspeicherDialog</c>).</summary>
        public const string QUELLE_PUFFERSPEICHER = "Form_QuellePufferspeicher";

        /// <summary>Das Quellprofil einer Waermepumpe (<c>QuellprofilDialog</c>).</summary>
        public const string QUELLPROFIL = "Form_Quellprofil";

        /// <summary>Die Waermesenken einer Anlage (<c>WaermesenkeDialog</c>).</summary>
        public const string WAERMESENKE = "Form_Waermesenke";

        /// <summary>
        /// Die Konfiguration EINER Komponente der Simulation
        /// (<c>KomponentenKonfigurationDialog</c>) — die dritte Maske ohne
        /// <c>Form_</c>-Vorsilbe: Sie hatte nie eine WinForms-Fassung.
        /// </summary>
        public const string KOMPONENTENKONFIGURATION = "KomponentenKonfiguration";

        // =================================================================
        //  Welle KI-F3: die Masken des BEDARFS und der KLIMADATEN
        // =================================================================
        //
        // Sie gehen aus dem Reiter „Waermebedarf" bzw. „Strombedarf" der
        // Startseite und aus dem Menue „Administration" auf. Die Schluessel
        // sind die WinForms-Maskennamen des Bestands - auch dort, wo eine
        // Razor-Komponente heute mehrere von ihnen bedient.

        /// <summary>
        /// Die Gebaeudemaske des PROJEKTS (<c>GebaeudeDialog</c>) — Projektliste und
        /// Katalog nebeneinander, in den Betriebsarten Projekt und Assistent.
        /// </summary>
        /// <remarks>
        /// Die Gebaeudeverwaltung ist eine eigene Komponente mit eigenem Schluessel
        /// (<see cref="GEBAEUDE_ADMIN"/>); diese Maske fuehrt deshalb kein Feld, das die
        /// Betriebsart „Verwaltung" meldete.
        /// </remarks>
        public const string GEBAEUDE = "Form_Gebaeude";

        /// <summary>
        /// Die GEBAEUDEVERWALTUNG (<c>GebaeudeAdminDialog</c>, Welle #465) — Katalogliste
        /// und Stammblatt; ihr Stammblatt fuehrt jedes Feld des Katalogeditors.
        /// </summary>
        /// <remarks>
        /// Der Schluessel IST der Navigationsschluessel der Verwaltung — dieselbe Regel wie
        /// bei den Verwaltungen der Erzeugerkataloge (<see cref="HEIZKESSEL_ADMIN"/>).
        /// </remarks>
        public const string GEBAEUDE_ADMIN = Masken.GebaeudeAdmin;

        /// <summary>
        /// Die Wohn-/Nutzflaechenangabe eines Projektgebaeudes
        /// (<c>GebaeudeWohnflaecheDialog</c>).
        /// </summary>
        public const string GEBAEUDE_WOHNFLAECHE = "Form_GebWohnflaeche";

        /// <summary>Der Gebaeude-Katalogeditor (<c>GebaeudeKatalogDialog</c>).</summary>
        public const string GEBAEUDE_KATALOG = "Form_Gebaeude1";

        /// <summary>Der Waermebedarf EINES Gebaeudes (<c>GebaeudeBedarfDialog</c>).</summary>
        public const string GEBAEUDE_BEDARF = "Form_Gebaeude_Bedarf";

        /// <summary>Die Gebaeudetypen-Verwaltung (<c>GebaeudetypDialog</c>).</summary>
        public const string GEBAEUDETYP = "Form_EingGebTyp";

        /// <summary>
        /// Die Verwaltung „Baustoffe" (<c>BaustoffKatalogDialog</c>, Gebaeudesimulation G3) — eine
        /// neue Maske ohne WinForms-Vorlaeufer und deshalb ohne <c>Form_</c>-Vorsilbe
        /// (Softwarearchitektur 3.8); zugleich die Vorsilbe ihres Hilfeschluessels.
        /// </summary>
        public const string BAUSTOFF_KATALOG = "BaustoffKatalog";

        /// <summary>
        /// Die Verwaltung „Bauteilaufbauten" samt Schichtenraster (<c>BauteilaufbauDialog</c>,
        /// Gebaeudesimulation G3) — wie <see cref="BAUSTOFF_KATALOG"/> ohne <c>Form_</c>-Vorsilbe.
        /// </summary>
        public const string BAUTEILAUFBAU = "Bauteilaufbau";

        /// <summary>
        /// Der Zonendialog (<c>ZonenDialog</c>, Gebaeudesimulation G3) — eine Ueberlagerung im
        /// Gebaeudeeditor, ohne WinForms-Vorlaeufer und deshalb ohne <c>Form_</c>-Vorsilbe; zugleich
        /// die Vorsilbe ihres Hilfeschluessels.
        /// </summary>
        public const string ZONE = "Zone";

        /// <summary>
        /// Der Bauteildialog (<c>BauteilDialog</c>, Gebaeudesimulation G3) — eine Ueberlagerung im
        /// Zonendialog, wie <see cref="ZONE"/> ohne <c>Form_</c>-Vorsilbe.
        /// </summary>
        public const string BAUTEIL = "Bauteil";

        /// <summary>
        /// Der Luftaustausch zwischen den Zonen eines Gebaeudes (<c>LuftaustauschDialog</c>,
        /// Gebaeudesimulation G6b) — eine Ueberlagerung im Gebaeudeeditor ab zwei Zonen, wie
        /// <see cref="ZONE"/> ohne <c>Form_</c>-Vorsilbe; zugleich die Vorsilbe ihres Hilfeschluessels.
        /// </summary>
        public const string LUFTAUSTAUSCH = "Luftaustausch";

        /// <summary>
        /// Der Gebäudeexport im Format gbXML (<c>GebaeudeExportDialog</c>, Gebaeudesimulation G7a) —
        /// eine Ueberlagerung im Gebaeudedialog, wie <see cref="ZONE"/> ohne <c>Form_</c>-Vorsilbe;
        /// zugleich die Vorsilbe ihres Hilfeschluessels.
        /// </summary>
        public const string GEBAEUDE_EXPORT = "GebaeudeExport";

        /// <summary>
        /// Das Wochen-Stundenprofil eines Bedarfstyps (<c>TypProfilDialog</c>).
        /// </summary>
        /// <remarks>
        /// <b>EINE Komponente, DREI Auspraegungen.</b> Dieselbe Maske pflegt die
        /// Stromverbraucher-, die Prozess- und die Brauchwassertypen; der
        /// Katalogschluessel ist der WinForms-Maskenname der Stromfassung, und welche
        /// Auspraegung offen ist, sagt der Typ, den sie fuehrt.
        /// </remarks>
        public const string TYPPROFIL = "Form_EingStromTyp";

        /// <summary>
        /// Der Kopfsatz eines Bedarfskatalogs samt seinen zwoelf Monatswerten
        /// (<c>TypStammDialog</c>) — ebenfalls EINE Komponente mit drei Auspraegungen.
        /// </summary>
        public const string TYPSTAMM = "Form_EingDBStromverbraucher";

        /// <summary>
        /// Die Bedarfsprofile EINES PROJEKTS (<c>BedarfsProfileDialog</c>) — ebenfalls
        /// EINE Komponente mit drei Auspraegungen.
        /// </summary>
        public const string BEDARFSPROFILE = "Form_Prozesswaerme";

        // Die ZAPFPROFIL-Masken (Welle #458, Stufe 3a) sind Ueberlagerungen der
        // Bedarfsprofile in der Auspraegung Brauchwasser. Eine WinForms-Fassung gab es
        // nie: Der Generator traegt den Namen seines Hilfeschluessels
        // (Form_Zapfprofil.btn_Help), Auslegung und Konstruktor sprechende Namen ohne
        // Vorsilbe.

        /// <summary>
        /// Der Dialog „Brauchwasser-Zapfprofil" (<c>ZapfprofilDialog</c>) — eine
        /// Ueberlagerung der Bedarfsprofile; angemeldet, solange er offen steht.
        /// </summary>
        public const string ZAPFPROFIL = "Form_Zapfprofil";

        /// <summary>
        /// Die Ueberlagerung „Auslegung Brauchwasser" des Zapfprofils
        /// (<c>ZapfprofilAuslegungDialog</c>).
        /// </summary>
        public const string ZAPFPROFIL_AUSLEGUNG = "ZapfprofilAuslegung";

        /// <summary>
        /// Die Ueberlagerung „Bedarfstag konstruieren" der Auslegung
        /// (<c>BedarfstagKonstruktor</c>).
        /// </summary>
        public const string BEDARFSTAG_KONSTRUKTOR = "BedarfstagKonstruktor";

        /// <summary>
        /// Die Ueberlagerung „Tagesgang bearbeiten" des Zapfprofils (<c>TagesgangEditor</c>,
        /// Stufe Experte) — sie gehoert zum Katalog, ihr OK schreibt den Tagesgang.
        /// </summary>
        public const string TAGESGANG_EDITOR = "TagesgangEditor";

        /// <summary>
        /// Die Ueberlagerung „Zapfkategorien und Streuung" des Zapfprofils
        /// (<c>ZapfkategorienEditor</c>, Stufe Experte) — sie gehoert zum Katalog.
        /// </summary>
        public const string ZAPFKATEGORIEN = "Zapfkategorien";

        /// <summary>
        /// Der Katalogdialog „Brauchwasser-Nutzungsarten" (<c>TwwNutzungsartAdminDialog</c>,
        /// Zapfprofilgenerator 5.4) — Navigationsziel des Menues; Katalog- und
        /// Navigationsschluessel fallen zusammen.
        /// </summary>
        public const string BRAUCHWASSER_NUTZUNGSARTEN = "Form_Brauchwasser_Nutzungsarten";

        /// <summary>
        /// Der Editor einer Nutzungsart (<c>TwwNutzungsartEditor</c>) — die Ueberlagerung hinter
        /// „Neu…", „Aendern…" und „Speichern unter…" des Katalogdialogs.
        /// </summary>
        public const string TWW_NUTZUNGSART_EDITOR = "TwwNutzungsartEditor";

        /// <summary>
        /// Der Dialog „VDI-4655-Typtage" (<c>TwwTyptagImportDialog</c>, Zapfprofilgenerator 4.2) —
        /// die Ueberlagerung hinter „VDI-4655-Typtage…" im Katalogdialog und in der Stufe Experte
        /// des Zapfprofils. Sie zeigt den eingespielten Stand; Einspielen und Loeschen bleiben
        /// Klicks des Anwenders.
        /// </summary>
        public const string BRAUCHWASSER_TYPTAGE = "Form_Brauchwasser_Typtage";

        /// <summary>
        /// Der Dialog „Messdaten" (<c>TwwMessreihenDialog</c>, Zapfprofilgenerator 4.8) — die
        /// Ueberlagerung hinter „Messdaten…" im Zapfprofil-Dialog. Sie zeigt die Messreihen des
        /// Projekts; Dateiwahl, Einspielen und Loeschen bleiben Klicks des Anwenders.
        /// </summary>
        public const string BRAUCHWASSER_MESSREIHEN = "Form_Brauchwasser_Messreihen";

        // Die drei BEDARFS-KATALOGVERWALTUNGEN sind DREI Masken auf EINER Komponente:
        // Sie tragen die WinForms-Maskennamen des Bestands, haben je ein eigenes
        // Navigationsziel im Menue und lassen sich einzeln oeffnen. Ein gemeinsamer
        // Schluessel haette dem Modell verschwiegen, welcher Katalog offen ist - und
        // „oeffne die Brauchwasserverwaltung" waere nicht mehr ausdrueckbar gewesen.

        /// <summary>Die Prozesswaerme-Katalogverwaltung (<c>BedarfAdminDialog</c>).</summary>
        public const string PROZESSWAERME_ADMIN = "Form_Prozesswaerme_Admin";

        /// <summary>Die Stromverbraucher-Katalogverwaltung (<c>BedarfAdminDialog</c>).</summary>
        public const string STROMVERBRAUCHER_ADMIN = "Form_Stromverbraucher_Admin";

        /// <summary>Die Brauchwasser-Katalogverwaltung (<c>BedarfAdminDialog</c>).</summary>
        public const string BRAUCHWASSER_ADMIN = "Form_Brauchwasser_Admin";

        /// <summary>
        /// Die Ergebnisanzeige eines Bedarfs (<c>BedarfErgebnisDialog</c>).
        /// </summary>
        public const string BEDARF_ERGEBNIS = "Form_ErgStromverbraucher";

        /// <summary>
        /// Die externen Waermebedarfsganglinien eines Projekts
        /// (<c>WaermebedarfExternDialog</c>).
        /// </summary>
        public const string WAERMEBEDARF_EXTERN = "Form_Waermebedarf";

        /// <summary>Die Solarganglinien eines Projekts (<c>SolarganglinieDialog</c>).</summary>
        public const string SOLARGANGLINIE = "Form_Solarganglinie";

        /// <summary>Die Klimadatenverwaltung (<c>KlimadatenDialog</c>).</summary>
        public const string KLIMADATEN = "Form_Klimadaten";

        // =================================================================
        //  Welle KI-F4: KOSTEN und WIRTSCHAFTLICHKEIT
        // =================================================================
        //
        // Sie gehen aus dem Menue „Administration -> Kosten", aus den zwei
        // Knoepfen der Kostenseite und aus der Fussleiste der
        // Wirtschaftlichkeitsseite auf. Die Schluessel sind auch hier die
        // WinForms-Maskennamen des Bestands; jede Razor-Datei nennt ihren
        // Vorlaeufer im Kopf.

        /// <summary>
        /// Die Energietraegerverwaltung (<c>EnergietraegerDialog</c>) samt
        /// Traegerkarte und den beiden Preisbloecken.
        /// </summary>
        /// <remarks>
        /// <b>EINE Maske, DREI Dateien.</b> <c>EnergietraegerEinstellungen</c> traegt
        /// die Karte, <c>StrompreisDetails</c> und <c>BrennstoffBestandteile</c> je
        /// einen aufklappbaren Preisblock; keiner davon geht in einem eigenen Fenster
        /// auf. Ihre Werte sind deshalb Felder DIESER Maske - dieselbe Bauart wie die
        /// Stammfelder der Waermepumpe.
        /// </remarks>
        public const string ENERGIETRAEGER = "Form_Energietraeger";

        /// <summary>
        /// „Energietraeger-Variante anlegen" (<c>EnergietraegerVarianteDialog</c>).
        /// </summary>
        public const string ENERGIETRAEGER_VARIANTE = "Form_Kosten_Auswahl";

        /// <summary>
        /// Die saisonalen Leistungspreis-Saetze eines Traegers
        /// (<c>LeistungspreisReiheDialog</c>).
        /// </summary>
        public const string LEISTUNGSPREISREIHE = "Form_LeistungspreisReihe";

        /// <summary>Das Kostenprofil eines Stromtraegers (<c>KostenprofilDialog</c>).</summary>
        public const string KOSTENPROFIL = "Form_Kostenprofil";

        /// <summary>Der Kostenfaktoren-Katalog (<c>KostenfaktorKatalogDialog</c>).</summary>
        public const string KOSTENFAKTOR_KATALOG = "Form_KostenAdmin";

        /// <summary>
        /// Emissionsarten und ihr Wertekatalog (<c>EmissionskatalogDialog</c>).
        /// </summary>
        public const string EMISSIONSKATALOG = "Form_Emissionskatalog";

        /// <summary>Die Nutzungsdauern (AfA) (<c>NutzungsdauerDialog</c>).</summary>
        public const string NUTZUNGSDAUER = "Form_Nutzungsdauer";

        /// <summary>Der Zeileneditor einer Kostenposition (<c>VorlagenPositionDialog</c>).</summary>
        public const string VORLAGENPOSITION = "Form_VorlagenPosition";

        /// <summary>
        /// Worst- und Best-Case einer Kostenposition (<c>CaseEingabeDialog</c>).
        /// </summary>
        public const string CASE_EINGABE = "Form_CaseEingabe";

        /// <summary>
        /// Die Wirtschaftlichkeits-Parameter des Projekts
        /// (<c>WirtschaftlichkeitParameterDialog</c>).
        /// </summary>
        public const string WIRTSCHAFTLICHKEIT_PARAMETER = "Form_WirtschaftlichkeitParameter";

        /// <summary>
        /// Die BHKW-Wirtschaftlichkeit (<c>BhkwWirtschaftlichkeitDialog</c>) — KWKG,
        /// Energie- und Stromsteuer.
        /// </summary>
        public const string BHKW_WIRTSCHAFTLICHKEIT = "Form_BhkwWirtschaftlichkeit";

        /// <summary>Die Tarifstruktur Strom, Rollenmodell (<c>TarifstrukturDialog</c>).</summary>
        public const string TARIFSTRUKTUR = "Form_Tarifstruktur";

        /// <summary>
        /// Die Photovoltaik-Verguetung (<c>PhotovoltaikVerguetungDialog</c>).
        /// </summary>
        public const string PV_VERGUETUNG = "Form_PhotovoltaikVerguetung";

        /// <summary>Die gesetzlichen Parameter (<c>GesetzeskatalogDialog</c>).</summary>
        public const string GESETZESKATALOG = "Form_Gesetzesparameter";

        /// <summary>
        /// Der Zeileneditor der gesetzlichen Parameter
        /// (<c>GesetzeskatalogZeileDialog</c>).
        /// </summary>
        public const string GESETZESKATALOG_ZEILE = "Form_GesetzparameterZeile";

        // Die zwei REITERBLAETTER der Ansicht „Berichte und Kosten" haben nie eine
        // WinForms-MASKE gehabt - ihre Vorlaeufer waren UserControls (UcBkKosten,
        // UcWirtschaftlichkeit). Sie tragen deshalb, wie die Ansicht „Simulation",
        // einen Schluessel ohne Form_-Vorsilbe.

        /// <summary>Das Reiterblatt „Kosten" (<c>Seiten.Berichte.KostenSeite</c>).</summary>
        public const string KOSTENSEITE = "Kostenseite";

        /// <summary>
        /// Das Reiterblatt „Wirtschaftlichkeit"
        /// (<c>Seiten.Berichte.WirtschaftlichkeitSeite</c>).
        /// </summary>
        public const string WIRTSCHAFTLICHKEITSSEITE = "Wirtschaftlichkeitsseite";

        // =================================================================
        //  Welle KI-F5: die ERZEUGERKATALOGE
        // =================================================================
        //
        // Fuenf Katalogeditoren der Geraetefamilien. Die Schluessel sind auch
        // hier die WinForms-Maskennamen des Bestands - und zugleich die Vorsilbe
        // des HILFESCHLUESSELS, den die Razor-Datei am Fragezeichen fuehrt
        // (Form_DBBHKW.btn_Help, Form_SolarDB.btn_Help, Form_AdminPV.btn_Help,
        // Form_AdminStromspeicher.btn_Help, Form_AdminWechselrichter.btn_Help).
        // Dieselbe Regel, nach der Heizkessel und Pufferspeicher heissen, wie sie
        // heissen.

        /// <summary>
        /// Der BHKW-Katalogeditor (<c>BhkwKatalogDialog</c>) — Bauart und
        /// Feldstrategie des <c>HeizkesselKatalogDialog</c>.
        /// </summary>
        public const string BHKW = "Form_DBBHKW";

        /// <summary>
        /// Der Solarkollektor-Katalogeditor (<c>SolarkollektorKatalogDialog</c>).
        /// </summary>
        public const string SOLARKOLLEKTOR = "Form_SolarDB";

        // DREI Schluessel auf EINER Komponente: Der ModulKatalogDialog ist Browser
        // und Editor in einem und kennt drei Auspraegungen (ModulKatalogArt). Jede
        // pflegt eine andere Stammtabelle mit einem anderen Feldsatz - das sind
        // drei Masken und nicht eine, genau wie die drei Bedarfskataloge der Welle
        // KI-F3 drei Schluessel auf BedarfAdminKiSicht tragen.

        /// <summary>
        /// Der PV-Modulkatalog (<c>ModulKatalogDialog</c>, Auspraegung
        /// <c>Photovoltaik</c>) — fuenfzehn Modulfelder.
        /// </summary>
        public const string PV_MODULKATALOG = "Form_AdminPV";

        /// <summary>
        /// Der Stromspeicher-Katalog (<c>ModulKatalogDialog</c>, Auspraegung
        /// <c>Stromspeicher</c>) — vierzehn Felder in zwei Gruppen.
        /// </summary>
        public const string STROMSPEICHER_KATALOG = "Form_AdminStromspeicher";

        /// <summary>
        /// Der Wechselrichter-Katalog (<c>ModulKatalogDialog</c>, Auspraegung
        /// <c>Wechselrichter</c>) — sechsundzwanzig Felder in drei Gruppen.
        /// </summary>
        public const string WECHSELRICHTER_KATALOG = "Form_AdminWechselrichter";

        // =================================================================
        //  Welle KI-F6: STROM, BERICHTE und PROJEKT
        // =================================================================
        //
        // Die letzte Welle des Entscheids KI-D-Q5. Drei der fuenf Schluessel
        // sind wieder WinForms-Maskennamen des Bestands - sie stehen so im
        // Aktionsprotokoll und in den Hilfeschluesseln der Dialoge
        // (Form_PeakShaving.btn_Help, Form_Stromganglinie_Admin.btn_Help,
        // Form_ProjektSpeichernUnter.btn_Help) - und zugleich in der
        // Navigationstabelle der Windows-Huelle. Die uebrigen hatten nie eine
        // WinForms-Fassung und tragen deshalb keine Form_-Vorsilbe.

        /// <summary>Lastspitzenkappung (<c>PeakShavingDialog</c>).</summary>
        public const string PEAK_SHAVING = "Form_PeakShaving";

        /// <summary>
        /// Die Leseregeln einer Speicher-Zeitreihe (<c>SpeicherZeitreihenDialog</c>).
        /// </summary>
        public const string SPEICHER_ZEITREIHEN = "Speicherzeitreihen";

        /// <summary>
        /// Stammdatenverwaltung der Stromganglinien (<c>StromganglinieAdminDialog</c>).
        /// </summary>
        public const string STROMGANGLINIE_ADMIN = "Form_Stromganglinie_Admin";

        /// <summary>
        /// Das Reiterblatt „Uebersicht" der Ansicht „Berichte und Kosten"
        /// (<c>UebersichtSeite</c>).
        /// </summary>
        /// <remarks>
        /// Es steht neben <see cref="KOSTENSEITE"/> und
        /// <see cref="WIRTSCHAFTLICHKEITSSEITE"/> der Welle KI-F4; die vier Blaetter
        /// derselben Ansicht tragen je einen eigenen Schluessel, weil sie ganz
        /// verschiedene Felder fuehren.
        /// </remarks>
        public const string BERICHTE_UEBERSICHT = "Berichtsuebersicht";

        /// <summary>
        /// Das Reiterblatt „Bericht" derselben Ansicht (<c>BerichtSeite</c>).
        /// </summary>
        public const string BERICHTSEITE = "Berichtsseite";

        /// <summary>
        /// „Projekt speichern unter" (<c>ProjektKopieDialog</c>) - der Nachfolger der
        /// WinForms-Maske <c>Form_ProjektSpeichernUnter</c>, deren Namen er behaelt.
        /// </summary>
        public const string PROJEKT_KOPIE = "Form_ProjektSpeichernUnter";

        /// <summary>„Als Variante speichern" (<c>ProjektVarianteDialog</c>).</summary>
        public const string PROJEKT_VARIANTE = "Projektvariante";

        // =================================================================
        //  Welle #456: die VERWALTUNGEN der Erzeugerkataloge
        // =================================================================
        //
        // VIER Schluessel auf EINER Komponente (KatalogBrowserDialog), wie die
        // drei Bedarfskataloge und die drei Modulkataloge. Sie SIND die
        // Navigationsschluessel der Verwaltungen (Masken.*) und zugleich die
        // Vorsilbe ihres Hilfeschluessels (Form_Heizkessel_Admin.btn_Help) -
        // deshalb stehen sie hier als Verweis und nicht als zweite Zeichenkette.

        /// <summary>„Administration Heizkessel" (<c>KatalogBrowserDialog</c>, Heizkessel).</summary>
        public const string HEIZKESSEL_ADMIN = Masken.HeizkesselAdmin;

        /// <summary>Die BHKW-Verwaltung (<c>KatalogBrowserDialog</c>, BHKW).</summary>
        public const string BHKW_ADMIN = Masken.BhkwAdmin;

        /// <summary>„Administration Solarkollektoren" (<c>KatalogBrowserDialog</c>).</summary>
        public const string SOLARKOLLEKTOREN_ADMIN = Masken.SolarkollektorenAdmin;

        /// <summary>„Administration Pufferspeicher" (<c>KatalogBrowserDialog</c>).</summary>
        public const string PUFFERSPEICHER_ADMIN = Masken.PufferSpAdmin;

        // =================================================================
        //  Welle #458, Stufe 2: die uebrigen Masken mit Einstellwerten
        // =================================================================
        //
        // Die Schluessel folgen der Regel des Katalogs: Wo es eine WinForms-Maske
        // gab, bleibt ihr Name (er steht in den Hilfeschluesseln), sonst ein
        // sprechender Name ohne Vorsilbe.

        /// <summary>
        /// Der Kennlinieneditor der Waermepumpe (<c>KennlinienEditorDialog</c>) — eine
        /// Ueberlagerung der Waermepumpen-Verwaltung und der Waermepumpen-Anlage.
        /// </summary>
        /// <remarks>
        /// Der Name ist der der abgeloesten Maske <c>Views/Waermepumpe/Kenndaten</c>;
        /// ihr Hilfeschluessel <c>Kenndaten.btn_Help</c> steht noch am Info-Knopf.
        /// Wie die Ueberlagerung „Anlagenwerte" meldet sie sich nur an, solange sie
        /// offen steht.
        /// </remarks>
        public const string KENNLINIEN = "Kenndaten";

        /// <summary>
        /// Der Projektkopf, Schritt 1 des Assistenten „Neues Projekt"
        /// (<c>ProjektKopfSeite</c>) — der Nachfolger von <c>Wizard_Projekt</c>, dessen
        /// Namen er behaelt.
        /// </summary>
        public const string PROJEKTKOPF = "Wizard_Projekt";

        /// <summary>
        /// Die Startseite (<c>Startseite</c> samt <c>ErzeugerReiter</c>) — der Nachfolger
        /// von <c>Form_Start</c>, dessen Namen er behaelt; ihre Info-Knoepfe tragen
        /// weiter <c>Form_Start.btn_Help_*</c>.
        /// </summary>
        public const string STARTSEITE = "Form_Start";

        /// <summary>
        /// Die Programmeinstellungen (<c>EinstellungenDialog</c>) — der Nachfolger von
        /// <c>Form_AdminSettings</c>, dessen Namen er behaelt.
        /// </summary>
        public const string EINSTELLUNGEN = "Form_AdminSettings";

        /// <summary>
        /// Der Katalogschluessel der Verwaltung zu einer Auspraegung des Katalogbrowsers —
        /// die EINE Stelle, an der der Dialog erfaehrt, unter welchem Namen er sich anmeldet.
        /// </summary>
        public static string KatalogBrowser(KatalogBrowserArt art)
        {
            switch (art)
            {
                case KatalogBrowserArt.Bhkw: return BHKW_ADMIN;
                case KatalogBrowserArt.Solarkollektoren: return SOLARKOLLEKTOREN_ADMIN;
                case KatalogBrowserArt.Pufferspeicher: return PUFFERSPEICHER_ADMIN;
                default: return HEIZKESSEL_ADMIN;
            }
        }
    }

    /// <summary>
    /// Der gefuellte Dialogkatalog: die vier Startmasken der Etappe 3b und — seit
    /// Auftrag #200 — die Stromspeicher-Ansicht (Fachkonzept 11.3/11.6, Konzept
    /// „Der Hilfe-Assistent im Dialog" 3.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum hier und nicht in KiKern.</b> Nur an dieser Stelle darf Wissen ueber die
    /// Masken stehen (Fachkonzept 3.7); <c>KiKern</c> haelt die Verwaltung und die
    /// Bauartsperre gegen Loeschknoepfe, kennt aber weder ein Control noch eine
    /// Datenklasse. Dieselbe Arbeitsteilung wie zwischen <see cref="KiRegister"/> und
    /// <c>KiAktionen</c>.
    /// </para>
    /// <para>
    /// <b>Der zweite Parameter ist seit Auftrag #200 der EIGENSCHAFTSPFAD</b>
    /// (<c>HeizkesselKatalogDaten.Ptherm</c>) und nicht mehr der WinForms-Controlname
    /// (<c>tb_th_Leistung</c>). Der alte Weg war tot: Die vier Masken sind seit iU9
    /// Razor-Komponenten, <c>Application.OpenForms</c> fuehrt sie nicht mehr, und
    /// <c>FindControlRecursive</c> fand nichts. An seine Stelle tritt die
    /// <see cref="KiMaskenbruecke"/> — der offene Dialog meldet je Feld einen Getter auf
    /// die Eigenschaft seines Daten-Objekts an. Der Typname VOR dem Punkt ist die Probe,
    /// dass Katalog und Daten-Objekt zusammengehoeren; ein Waechter haelt jeden
    /// Eigenschaftsnamen per Reflection gegen den Typ
    /// (<c>EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests</c>).
    /// </para>
    /// <para>
    /// <b>Die Maskenkoordinaten sind mit dieser Umstellung entfallen.</b> Drei der vier
    /// Eintraege trugen eine gemessene <c>KiKnopfposition</c> — der Platz, an dem der
    /// Aufrufknopf im Client-Bereich der WinForms-Maske noch frei war. Seit iU9‑W15b.5
    /// zeichnet den Knopf der Baustein <c>KiKnopf</c> im Kopf jedes Dialogs (Auftrag
    /// #199, Weg 1); es gibt keinen freien Platz mehr zu suchen und keine Maske mehr, in
    /// der man ihn suchen koennte. Die Zahlen standen sonst als gemessene Wahrheit ueber
    /// Fenster, die es nicht mehr gibt.
    /// </para>
    /// <para>
    /// <b>Der Feldumfang folgt der SICHTBAREN Maske — nach oben wie nach unten.</b> Seine
    /// Herkunft ist Fachkonzept 11.6 („Feldumfang v1 = die von der Knopfpruefung
    /// erfassten Eingabefelder"); das ist der Grund, warum drei der Katalogmasken so
    /// wenige Felder fuehren. Ihn zu ERWEITERN bleibt eine fachliche Entscheidung mit
    /// eigener Abnahme. Verliert eine Maske dagegen ein Feld, verliert es der Katalog im
    /// selben Schritt: Sonst boete der Assistent an, eine Zahl zu setzen, die in der
    /// offenen Maske niemand nachlesen kann. Der Waechter
    /// <c>EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests</c> haelt jeden Feldpfad gegen
    /// das Markup seiner Maske; ohne diese Probe stehen nur die beiden Ansichten, die
    /// ueber eine SICHTKLASSE binden (Stromspeicher, Simulation).
    /// </para>
    /// <para>
    /// <b>Kein Feld traegt einen Hilfe-Slug.</b> Die Zuordnung Feld → Slug las
    /// <c>HelpExtender.RegisterControl</c> aus <c>help_mapping.txt</c>; diese Datei liegt
    /// nicht im Repository, und es gibt im ganzen Baum keinen Aufruf von
    /// <c>SetHelpKey</c>. Der Weg dorthin steht trotzdem — deklariert wird aber nur, was
    /// belegt ist.
    /// </para>
    /// </remarks>
    public static class KiDialoge
    {
        /// <summary>
        /// JE KULTUR ein Katalog - und nicht einer je Prozess.
        /// </summary>
        /// <remarks>
        /// Die Anzeigenamen der Eintraege stehen uebersetzt in <c>MyResource.Resource</c>.
        /// Ein einziger Katalog je Prozess truege deshalb auf Dauer die Sprache seines
        /// ERSTEN Zugriffs: Im Programm folgte er dem Sprachwechsel zur Laufzeit (Menue
        /// „Sprache") nicht, und im Testlauf entschiede die Reihenfolge der Faelle
        /// darueber, in welcher Sprache er dasteht. Schluessel ist die Kultur, mit der die
        /// Ressourcen tatsaechlich aufloesen (<see cref="Kulturschluessel"/>) - so laufen
        /// Katalog und Ressourcentext nie auseinander.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, KiDialogKatalog> _kataloge =
            new ConcurrentDictionary<string, KiDialogKatalog>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Der Katalog der aktuellen Anzeigesprache - je Kultur einmal gebaut, dann fest.
        /// </summary>
        /// <remarks>
        /// Dieselbe Bauart wie <c>KiAusfuehrer.Register</c>: Der Katalog einer Kultur
        /// entsteht beim ersten Zugriff und wird danach nur noch gelesen; derselbe Zugriff
        /// unter derselben Kultur liefert immer dieselbe Instanz. Ein Katalog, dem zur
        /// Laufzeit eine Maske zuwachsen koennte, waere genau der Weg, auf dem eine nicht
        /// freigegebene Maske doch noch steuerbar wuerde (<see cref="KiDialogKatalog"/>).
        /// </remarks>
        public static KiDialogKatalog Katalog
        {
            get { return _kataloge.GetOrAdd(Kulturschluessel(), _ => Erzeuge()); }
        }

        /// <summary>
        /// Die Kultur, mit der <c>MyResource.Resource</c> tatsaechlich aufloest.
        /// </summary>
        /// <remarks>
        /// <c>Resource.Culture</c> ist die Uebersteuerung der erzeugten Ressourcenklasse;
        /// steht sie auf <c>null</c> - der Regelfall, auch nach einem Sprachwechsel ueber
        /// <c>Dienste.Sprache</c> -, liest <c>ResourceManager.GetString</c> ueber
        /// <see cref="CultureInfo.CurrentUICulture"/>. Genau diese Regel gilt hier, damit
        /// der Schluessel des Zwischenspeichers dieselbe Kultur nennt, die die Texte des
        /// Katalogs liefert. Der Name reicht als Schluessel: Die invariante Kultur fuehrt
        /// den leeren Namen, und den fuehrt sonst keine.
        /// </remarks>
        private static string Kulturschluessel()
        {
            CultureInfo kultur = MyResource.Resource.Culture ?? CultureInfo.CurrentUICulture;
            return kultur.Name;
        }

        /// <summary>Baut den vollstaendigen Katalog.</summary>
        public static KiDialogKatalog Erzeuge()
        {
            return new KiDialogKatalog(
                Heizkessel(),
                Photovoltaik(),
                PvAnlagenwerte(),
                Pufferspeicher(),
                Waermepumpe(),
                Stromspeicherauslegung(),
                Simulation(),
                Kostenverwaltung(),
                HeizkesselProjekt(),
                BhkwProjekt(),
                PufferspeicherProjekt(),
                StromspeicherProjekt(),
                SolarkollektorenProjekt(),
                WaermepumpeAnlage(),
                PufferspeicherVerwaltung(),
                QuelleErdreich(),
                QuellePufferspeicher(),
                Quellprofil(),
                Waermesenke(),
                Komponentenkonfiguration(),
                Gebaeude(),
                GebaeudeWohnflaeche(),
                GebaeudeKatalog(),
                GebaeudeVerwaltung(),
                GebaeudeBedarf(),
                Gebaeudetyp(),
                BaustoffKatalog(),
                Bauteilaufbau(),
                Zone(),
                Bauteil(),
                Luftaustausch(),
                GebaeudeExport(),
                Typprofil(),
                Typstamm(),
                Bedarfsprofile(),
                Zapfprofil(),
                ZapfprofilAuslegung(),
                BedarfstagKonstruktor(),
                TagesgangEditor(),
                Zapfkategorien(),
                BrauchwasserNutzungsarten(),
                TwwNutzungsartEditor(),
                BrauchwasserTyptage(),
                BrauchwasserMessreihen(),
                BedarfAdmin(KiMaskennamen.PROZESSWAERME_ADMIN,
                            KiDialogTexte.MaskeProzesswaermeAdmin),
                BedarfAdmin(KiMaskennamen.STROMVERBRAUCHER_ADMIN,
                            KiDialogTexte.MaskeStromverbraucherAdmin),
                BedarfAdmin(KiMaskennamen.BRAUCHWASSER_ADMIN,
                            KiDialogTexte.MaskeBrauchwasserAdmin),
                BedarfErgebnis(),
                WaermebedarfExtern(),
                Solarganglinie(),
                Klimadaten(),
                Energietraeger(),
                EnergietraegerVariante(),
                Leistungspreisreihe(),
                Kostenprofil(),
                Kostenfaktorkatalog(),
                Emissionskatalog(),
                Nutzungsdauer(),
                Vorlagenposition(),
                CaseEingabe(),
                WirtschaftlichkeitParameter(),
                BhkwWirtschaftlichkeit(),
                Tarifstruktur(),
                PhotovoltaikVerguetung(),
                Gesetzeskatalog(),
                GesetzeskatalogZeile(),
                Kostenseite(),
                Wirtschaftlichkeitsseite(),
                Bhkw(),
                Solarkollektor(),
                PvModulkatalog(),
                Stromspeicherkatalog(),
                Wechselrichterkatalog(),
                ErzeugerVerwaltung(KatalogBrowserArt.Heizkessel),
                ErzeugerVerwaltung(KatalogBrowserArt.Bhkw),
                ErzeugerVerwaltung(KatalogBrowserArt.Solarkollektoren),
                ErzeugerVerwaltung(KatalogBrowserArt.Pufferspeicher),
                PeakShaving(),
                Speicherzeitreihen(),
                StromganglinieAdmin(),
                BerichteUebersicht(),
                Berichtseite(),
                ProjektKopie(),
                ProjektVariante(),
                Kennlinien(),
                Projektkopf(),
                Startseite(),
                Einstellungen());
        }

        // =====================================================================
        // Form_*_Admin  ->  KatalogBrowserDialog   (Welle #456)
        // =====================================================================

        /// <summary>
        /// Der Typname der Sichtklasse, an der die vier Erzeugerverwaltungen ihre Felder
        /// anmelden (<c>EPOS.UI.Dialoge.Erzeuger.KatalogBrowserKiSicht</c>).
        /// </summary>
        public const string KATALOGBROWSER_SICHT = "KatalogBrowserKiSicht";

        /// <summary>
        /// Eine Verwaltung der Erzeugerkataloge (Heizkessel, BHKW, Solarkollektoren,
        /// Pufferspeicher) — die Feldkarte kommt aus dem PROFIL, dazu das Wahlfeld
        /// <c>satz</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Wahrheit, keine zweite Feldliste.</b> Welche Felder eine Verwaltung
        /// zeigt, in welcher Art und ob ihr Speicherweg sie schreibt, steht EINMAL in
        /// <see cref="KatalogBrowserProfil"/>. Eine Liste hier daneben liefe ihm davon
        /// - der Modulkatalog braucht dafuer einen eigenen Waechter. Die Karte wird
        /// deshalb aus dem Profil ERZEUGT: Feldname = Profilschluessel klein,
        /// Eigenschaftspfad = Sichtklasse + Profilschluessel (die Sichtklasse loest ihn
        /// als FELDTAFEL ueber den lebenden Feldsatz auf), Anzeigename = die Beschriftung
        /// ohne Doppelpunkt, Einheit aus dem Profil, nur lesbar = nicht editierbar.
        /// </para>
        /// <para>
        /// <b>Der Satz ist die Wahl der Liste</b> — dasselbe Muster wie die
        /// Bedarfsverwaltungen: Ihn zu setzen waehlt die Zeile, wie ein Klick. Er ist
        /// SATZWAHL (<see cref="KiDialogFeld.Satzwahl"/>): Der Schutz eines
        /// Auslieferungssatzes gilt fuer ihn nicht, sonst liesse sich aus einem
        /// geschuetzten Satz heraus nie der eigene waehlen.
        /// </para>
        /// <para>
        /// <b>Nicht ueber den Assistenten</b> gehen „Neu…", „Duplizieren…",
        /// „Loeschen" und „Import…" — sie legen Saetze an oder nehmen sie weg
        /// (KI-D-Q11). Die Knopfliste nennt deshalb nur Speichern, Verwerfen und Beenden.
        /// </para>
        /// </remarks>
        private static KiDialog ErzeugerVerwaltung(KatalogBrowserArt art)
        {
            KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(art, KiDialogTexte.Profiltext);

            var felder = new List<KiDialogFeld>(profil.Detailfelder.Count + 1)
            {
                new KiDialogFeld("satz", KATALOGBROWSER_SICHT + ".Satz",
                                 KiDialogTexte.KbrowSatzName, KiParameterTyp.Wahl,
                                 KiDialogTexte.KbrowSatzErl, satzwahl: true)
            };

            foreach (BrowserDetailfeld f in profil.Detailfelder)
                felder.Add(new KiDialogFeld(
                    f.Schluessel.ToLowerInvariant(),
                    KATALOGBROWSER_SICHT + "." + f.Schluessel,
                    f.Feldname,
                    Feldtyp(f.Art),
                    KiDialogTexte.KbrowErlaeuterung(art, f.Schluessel, f.Feldname),
                    einheit: f.Einheit,
                    leerErlaubt: f.Art == BrowserFeldArt.Text || f.Art == BrowserFeldArt.Mehrzeilig,
                    nurLesen: !f.Editierbar));

            return new KiDialog(
                maskenname: KiMaskennamen.KatalogBrowser(art),
                anzeigename: profil.Titel,
                felder: felder,
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("verwerfen", "btn_Verwerfen", KiDialogTexte.KnopfVerwerfen),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>
        /// Die Feldart des Profils als Feldtyp des Katalogs. Eine AUSWAHL (Code und
        /// Beschriftung, bisher nur am PV-Modul) steht als Text da: Die vier
        /// Erzeugerprofile fuehren keine; kaeme eine dazu, bekaeme sie ihren Weg ueber
        /// eine Wahlquelle.
        /// </summary>
        public static KiParameterTyp Feldtyp(BrowserFeldArt art)
        {
            switch (art)
            {
                case BrowserFeldArt.Zahl: return KiParameterTyp.Zahl;
                case BrowserFeldArt.Ganzzahl: return KiParameterTyp.Ganzzahl;
                case BrowserFeldArt.Schalter: return KiParameterTyp.Wahrheitswert;
                default: return KiParameterTyp.Text;
            }
        }

        /// <summary>
        /// Die Felder der vier KATALOGEDITOREN, die ihre Verwaltung unter einem ANDEREN
        /// Schluessel fuehrt: Editorfeld → Profilschluessel
        /// (<see cref="KatalogBrowserProfil"/>). Was hier fehlt, fuehrt die Verwaltung
        /// unter demselben Schluessel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum eine erklaerte Zuordnung und kein Namensvergleich.</b> Editor und
        /// Verwaltung sind EIN Weg (<c>KiAktionenDialog.Zielmaske</c>): Die Absage nennt
        /// die Verwaltung, wenn sie die genannten Felder fuehrt. Ob sie das tut, ist eine
        /// Frage der IDENTITAET - derselbe Wert desselben Katalogsatzes - und wird deshalb
        /// ueber Schluessel beantwortet. Die Beschriftungen stehen uebersetzt in
        /// <c>MyResource.Resource</c>; ein toleranter Vergleich mit ihnen traf
        /// <c>bereitschaftsverlust</c> nur unter der deutschen Beschriftung
        /// „Betriebsbereitschaftsverluste", unter <c>en-US</c> nicht mehr.
        /// </para>
        /// <para>
        /// Der Waechter <c>EPOS.Kern.Tests/KiKatalogKulturTests</c> haelt fest, dass jedes
        /// Feld der vier Editoren so in seiner Verwaltung steht und jede Zuordnung hier
        /// auf ein vorhandenes Feld zeigt.
        /// </para>
        /// </remarks>
        private static readonly Dictionary<string, Dictionary<string, string>> VERWALTUNGSFELDER =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
            {
                [KiMaskennamen.HEIZKESSEL] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = KatalogBrowserProfil.FeldBezeichner,
                    ["hersteller"] = KatalogBrowserProfil.FeldFirma,
                    ["energietraeger"] = KatalogBrowserProfil.FeldBrennstoff,
                    ["th_leistung"] = KatalogBrowserProfil.FeldPtherm,
                    ["bereitschaftsverlust"] = KatalogBrowserProfil.FeldBBVerlust
                },
                [KiMaskennamen.BHKW] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = KatalogBrowserProfil.FeldBezeichner,
                    ["hersteller"] = KatalogBrowserProfil.FeldFirma,
                    ["energietraeger"] = KatalogBrowserProfil.FeldBrennstoff,
                    ["th_leistung"] = KatalogBrowserProfil.FeldPtherm,
                    ["el_leistung"] = KatalogBrowserProfil.FeldPel,
                    ["wirkungsgrad_gesamt"] = KatalogBrowserProfil.FeldWirkungsgrad
                },
                [KiMaskennamen.SOLARKOLLEKTOR] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = KatalogBrowserProfil.FeldBezeichner,
                    ["hersteller"] = KatalogBrowserProfil.FeldFirma
                },
                [KiMaskennamen.PUFFERSPEICHER] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = KatalogBrowserProfil.FeldBezeichner,
                    ["hersteller"] = KatalogBrowserProfil.FeldFirma,
                    ["gesamtvolumen"] = KatalogBrowserProfil.FeldVolumen,
                    ["bereitschaftsverluste"] = KatalogBrowserProfil.FeldVerluste
                }
            };

        /// <summary>
        /// Der Schluessel, unter dem die ZIELMASKE der Maske <paramref name="maskenname"/>
        /// (<see cref="KiMaskenziele.Ziel"/>) deren Feld <paramref name="feld"/> fuehrt -
        /// das erklaerte Gegenstueck eines Katalogeditors in seiner Verwaltung, fuer ein
        /// Feld des Aufklappers „Alle Daten" der Schluessel ohne die Vorsilbe
        /// <c>katalog_</c>, sonst derselbe Schluessel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sprachneutral: Die Antwort haengt nur an Schluesseln und ist deshalb in jeder
        /// Anzeigesprache dieselbe.
        /// </para>
        /// <para>
        /// <b>Nur ERKLAERTE Gegenstuecke</b> (Welle #469): die Tabelle
        /// <see cref="VERWALTUNGSFELDER"/> und die Vorsilbe
        /// <see cref="KATALOGFELD_VORSILBE"/> - ein ganzer Schluesselbestandteil, den
        /// <see cref="AlleDatenFeld"/> selbst vor den Profilschluessel setzt
        /// (<c>katalog_breite</c> steht im Modulkatalog als <c>breite</c>). Eine
        /// Aehnlichkeit der Namen erklaert nichts: <c>wr_wirkungsgrad</c> (Wechselrichter)
        /// ist nicht der <c>wirkungsgrad</c> des Moduls, <c>ladeleistung</c> (Puffer) nicht
        /// die <c>speicher_ladeleistung</c> der Batterie.
        /// </para>
        /// </remarks>
        public static string Zielfeldname(string maskenname, string feld)
        {
            Dictionary<string, string> felder;
            string profilschluessel;

            if (maskenname != null && feld != null &&
                VERWALTUNGSFELDER.TryGetValue(maskenname, out felder) &&
                felder.TryGetValue(feld, out profilschluessel))
                return profilschluessel.ToLowerInvariant();

            string vorsilbe = KATALOGFELD_VORSILBE.ToLowerInvariant();
            if (feld != null && feld.Length > vorsilbe.Length &&
                feld.StartsWith(vorsilbe, StringComparison.Ordinal))
                return feld.Substring(vorsilbe.Length);

            return feld;
        }

        /// <summary>
        /// Die vier Katalogeditoren, deren Felder <see cref="Zielfeldname"/> in ihre
        /// Verwaltung uebersetzt - fuer den Waechter.
        /// </summary>
        public static IReadOnlyCollection<string> Katalogeditoren => VERWALTUNGSFELDER.Keys;

        // =====================================================================
        // „Alle Daten" der sechs Erzeugermasken des Projekts   (Welle #458, Stufe 2)
        // =====================================================================

        /// <summary>
        /// Der Typname der Sichtklasse der vier Erzeugermasken mit Projektzeile
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerProjektKiSicht</c>).
        /// </summary>
        public const string ERZEUGER_PROJEKT_SICHT = "ErzeugerProjektKiSicht";

        /// <summary>
        /// Die Vorsilbe eines Feldes des Aufklappers „Alle Daten" im Eigenschaftspfad
        /// (<c>ErzeugerProjektKiSicht.Katalog_PTHERM</c>) und — klein — im Feldnamen
        /// (<c>katalog_ptherm</c>).
        /// </summary>
        /// <remarks>
        /// Ohne sie traefen sich Profil und Anlage: <c>VORLAUF</c> des Katalogsatzes und
        /// <c>Vorlauf</c> der Projektzeile sind fuer den Katalog DERSELBE Pfad (er
        /// vergleicht ohne Gross/Klein), fuer den Anwender aber zwei Werte — der eine
        /// gilt fuer das Geraet im Katalog, der andere fuer diese Anlage.
        /// </remarks>
        public const string KATALOGFELD_VORSILBE = "Katalog_";

        /// <summary>
        /// Die Felder des Aufklappers „Alle Daten" einer Erzeugermaske, deren Katalog der
        /// KATALOGBROWSER fuehrt (Heizkessel, BHKW, Pufferspeicher, Solarkollektoren) —
        /// erzeugt aus demselben Profil wie die Verwaltung (<see cref="ErzeugerVerwaltung"/>)
        /// und mit denselben Erlaeuterungen.
        /// </summary>
        private static IEnumerable<KiDialogFeld> AlleDaten(KatalogBrowserArt art, string sicht)
        {
            KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(art, KiDialogTexte.Profiltext);

            foreach (BrowserDetailfeld f in profil.Detailfelder)
                yield return AlleDatenFeld(sicht, f.Schluessel, f.Feldname, f.Einheit, f.Art,
                                           !f.Editierbar,
                                           KiDialogTexte.KbrowErlaeuterung(art, f.Schluessel, f.Feldname));
        }

        /// <summary>
        /// Die Felder des Aufklappers „Alle Daten" einer Erzeugermaske, deren Katalog der
        /// MODULKATALOG fuehrt (Photovoltaik, Stromspeicher) — erzeugt aus dem
        /// <see cref="ModulKatalogProfil"/>. Nicht setzbar ist, was die Bruecke des
        /// Aufklappers nicht zurueckschreibt: der gesperrte Bezeichner und ein
        /// Auswahlfeld (<c>ModulFeldwertBruecke.Editierbar</c>).
        /// </summary>
        /// <param name="modulkatalog">
        /// Der Katalogeintrag des Modulkatalogs — seine Erlaeuterungen gelten fuer
        /// dieselben Felder; zugeordnet wird ueber die Beschriftung, die der
        /// Profilwaechter des Modulkatalogs festhaelt.
        /// </param>
        private static IEnumerable<KiDialogFeld> AlleDaten(ModulKatalogArt art, KiDialog modulkatalog, string sicht)
        {
            ModulKatalogProfil profil = ModulKatalogProfil.Finde(art, KiDialogTexte.Profiltext);

            foreach (ModulKatalogFeld f in profil.Felder)
            {
                string erlaeuterung = null;
                foreach (KiDialogFeld k in modulkatalog.Felder)
                    if (string.Equals(k.Anzeigename.TrimEnd(' ', ':'), f.Feldname, StringComparison.Ordinal))
                    {
                        erlaeuterung = k.Erlaeuterung;
                        break;
                    }

                yield return AlleDatenFeld(sicht, f.Schluessel, f.Feldname, f.Einheit, f.Art,
                                           f.Gesperrt || f.Art == BrowserFeldArt.Auswahl,
                                           erlaeuterung ?? KiDialogTexte.KbrowRueckfall(f.Feldname));
            }
        }

        /// <summary>Ein Feld des Aufklappers „Alle Daten" — Name, Pfad und Anzeigename mit Vorsilbe.</summary>
        private static KiDialogFeld AlleDatenFeld(string sicht, string schluessel, string feldname,
                                                  string einheit, BrowserFeldArt art, bool nurLesen,
                                                  string erlaeuterung)
            => new KiDialogFeld(
                (KATALOGFELD_VORSILBE + schluessel).ToLowerInvariant(),
                sicht + "." + KATALOGFELD_VORSILBE + schluessel,
                KiDialogTexte.AlleDatenName(feldname),
                Feldtyp(art),
                erlaeuterung,
                einheit: einheit,
                leerErlaubt: art == BrowserFeldArt.Text || art == BrowserFeldArt.Mehrzeilig ||
                             art == BrowserFeldArt.Auswahl,
                nurLesen: nurLesen);

        /// <summary>Die benannten Felder einer Maske und dahinter die ihres Aufklappers.</summary>
        private static List<KiDialogFeld> MitAlleDaten(IEnumerable<KiDialogFeld> benannt,
                                                       IEnumerable<KiDialogFeld> alleDaten)
        {
            var liste = new List<KiDialogFeld>(benannt);
            liste.AddRange(alleDaten);
            return liste;
        }

        // =====================================================================
        // Berichtsuebersicht  ->  Seiten.Berichte.UebersichtSeite   (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// Das Reiterblatt „Uebersicht" der Ansicht „Berichte und Kosten" — vier
        /// Einstellwerte und zwei Anzeigen ueber die Sichtklasse
        /// <c>EPOS.UI.Seiten.Berichte.UebersichtSeiteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Es steht neben <see cref="KiMaskennamen.KOSTENSEITE"/> und
        /// <see cref="KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE"/></b> (Welle KI-F4): Die
        /// vier Blaetter derselben Ansicht tragen je einen eigenen Schluessel, weil sie
        /// ganz verschiedene Felder fuehren.
        /// </para>
        /// <para>
        /// <b>Die Seite SCHREIBT nicht in ihren Stand</b>, sie meldet jede Wahl an die
        /// Huelle und laedt danach neu. Deshalb bindet sie ueber eine Sichtklasse: Ein
        /// unmittelbar angemeldeter <c>UebersichtStand</c> boete an, die Stamm-Id zu
        /// setzen, ohne dass die Gruppe nachzoege.
        /// </para>
        /// <para>
        /// <b>Die VERGLEICHSWAHL bleibt draussen</b> — eine Menge von Verweisen und
        /// kein Feldwert; dieselbe Regel und derselbe Grund wie beim Reiterblatt
        /// „Kosten". Ebenso draussen: die Unterschiedstabelle samt ihrer
        /// Uebernahmespalte (gerechnete Anzeige mit einer Aktion je Zeile).
        /// </para>
        /// </remarks>
        private static KiDialog BerichteUebersicht()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BERICHTE_UEBERSICHT,
                anzeigename: KiDialogTexte.MaskeBerichteUebersicht,
                felder: new[]
                {
                    new KiDialogFeld("stammprojekt", "UebersichtSeiteKiSicht.Stammprojekt",
                                     KiDialogTexte.BkuStammName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BkuStammErl, leerErlaubt: true),
                    new KiDialogFeld("nur_staemme", "UebersichtSeiteKiSicht.NurStaemme",
                                     KiDialogTexte.BkuNurStaemmeName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BkuNurStaemmeErl),
                    new KiDialogFeld("variante", "UebersichtSeiteKiSicht.Variante",
                                     KiDialogTexte.BkuVarianteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BkuVarianteErl, leerErlaubt: true),
                    new KiDialogFeld("bezeichner", "UebersichtSeiteKiSicht.Bezeichner",
                                     KiDialogTexte.BkuBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.BkuBezeichnerErl, leerErlaubt: true),
                    new KiDialogFeld("simulationsstand", "UebersichtSeiteKiSicht.Simulationsstand",
                                     KiDialogTexte.BkuSimName, KiParameterTyp.Text,
                                     KiDialogTexte.BkuSimErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("statuszeile", "UebersichtSeiteKiSicht.Statuszeile",
                                     KiDialogTexte.KseStatusName, KiParameterTyp.Text,
                                     KiDialogTexte.KseStatusErl,
                                     leerErlaubt: true, nurLesen: true)
                });
        }

        // =====================================================================
        // Berichtsseite  ->  Seiten.Berichte.BerichtSeite   (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// Das Reiterblatt „Bericht" derselben Ansicht — drei Einstellwerte und drei
        /// Anzeigen ueber <c>EPOS.UI.Seiten.Berichte.BerichtSeiteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Einstellwerte, zwei Mengen.</b> Ausgabeformat und Zielordner stehen
        /// im Stand der Seite und gehen mit dem Lauf in die Huelle; die Word-Vorlage
        /// (BV-E1, <see cref="BerichtVorlagenfeld"/>) ist die Wahl der Gruppe „Vorlage".
        /// Welche Versionen und welche Bausteine der Bericht traegt, sind MENGEN VON
        /// VERWEISEN und damit keine Feldwerte (ein <c>KiDialogFeld</c> traegt genau
        /// einen). Sie gehen als AUFSTELLUNG hinaus — dieselbe Bauart wie bei der
        /// Einheitenliste der Stromspeicher-Ansicht.
        /// </para>
        /// <para>
        /// <b>Kein Speicherweg.</b> „Erstellen" rechnet den Bericht und schreibt eine
        /// Datei; das ist eine Aktion der Stufen 2 und 3 mit eigener Rueckfrage.
        /// </para>
        /// </remarks>
        private static KiDialog Berichtseite()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BERICHTSEITE,
                anzeigename: KiDialogTexte.MaskeBerichtseite,
                felder: new[]
                {
                    new KiDialogFeld("ausgabe", "BerichtSeiteKiSicht.Ausgabe",
                                     KiDialogTexte.BkbAusgabeName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BkbAusgabeErl),
                    new KiDialogFeld("zielordner", "BerichtSeiteKiSicht.Zielordner",
                                     KiDialogTexte.BkbZielName, KiParameterTyp.Text,
                                     KiDialogTexte.BkbZielErl, leerErlaubt: true),
                    BerichtVorlagenfeld(),
                    new KiDialogFeld("varianten", "BerichtSeiteKiSicht.Varianten",
                                     KiDialogTexte.BkbVariantenName, KiParameterTyp.Text,
                                     KiDialogTexte.BkbVariantenErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("bausteine", "BerichtSeiteKiSicht.Bausteine",
                                     KiDialogTexte.BkbBausteineName, KiParameterTyp.Text,
                                     KiDialogTexte.BkbBausteineErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("statuszeile", "BerichtSeiteKiSicht.Statuszeile",
                                     KiDialogTexte.KseStatusName, KiParameterTyp.Text,
                                     KiDialogTexte.KseStatusErl,
                                     leerErlaubt: true, nurLesen: true)
                });
        }

        /// <summary>
        /// <b>Das Feld „vorlage“ des Reiterblatts „Bericht“</b> (Konzept Berichtsvorlagen 10.2, Etappe
        /// BV-E1): die Word-Vorlage des Stammprojekts — eine Wahl aus der Vorlagenliste der Seite,
        /// angemeldet in <see cref="Berichtseite"/>.
        /// </summary>
        /// <remarks>
        /// <para><b>Die Einträge sind die der Seite.</b> Die Bindung <c>BerichtSeiteKiSicht.Vorlage</c>
        /// samt Begleiter <c>VorlageWahl</c> trägt die stabilen Ids, die die Hülle der Berichtsseite je
        /// Sitzung vergibt, mit dem Namen der Vorlage als Text — dieselbe Liste, die das Auswahlfeld
        /// zeigt, auch mit dem gesperrten Eintrag einer fehlenden Vorlage.</para>
        /// <para><b>Gesetzt wird derselbe Weg wie im Auswahlfeld:</b> Die Seite lehnt während eines Laufs,
        /// bei einem gesperrten Eintrag und bei einer Id, die die Liste nicht kennt, benannt ab; die
        /// Hülle löst die Id über <see cref="BerichtsvorlagenCtrl.Finde"/> auf und speichert die
        /// Abweichung (<see cref="BerichtsvorlagenCtrl.SetzeAbweichung"/>) — gibt es die Vorlage nicht
        /// mehr, meldet die Gruppe es.</para>
        /// </remarks>
        public static KiDialogFeld BerichtVorlagenfeld()
        {
            return new KiDialogFeld("vorlage", "BerichtSeiteKiSicht.Vorlage",
                                    KiDialogTexte.BkbVorlageName, KiParameterTyp.Wahl,
                                    KiDialogTexte.BkbVorlageErl, leerErlaubt: true);
        }

        /// <summary>
        /// Die Vorlagenliste als Wahleinträge über die KENNUNGEN des Controllers (<c>standard</c>,
        /// <c>eigen:</c> + Dateiname) — für einen Aufrufer ohne die Seite; die Seite selbst reicht
        /// ihre stabilen Ids (<c>BerichtSeiteKiSicht.VorlageWahl</c>). Je Vorlage ihre Kennung und ihr
        /// Name; eine mitgelieferte Vorlage, deren Datei fehlt, bleibt wählbar (der Lauf nimmt dann den
        /// benannten Rückfall), eine eigene ohne Datei nicht.
        /// </summary>
        public static IReadOnlyList<KiWahleintrag> BerichtVorlagenwahl(IEnumerable<Vorlageneintrag> liste)
        {
            var eintraege = new List<KiWahleintrag>();
            if (liste == null) return eintraege;
            foreach (Vorlageneintrag e in liste)
                if (e != null && (e.Vorhanden || e.IstStandard)) eintraege.Add(new KiWahleintrag(e.Id, e.Name));
            return eintraege;
        }

        // =====================================================================
        // Form_ProjektSpeichernUnter  ->  ProjektKopieDialog   (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// „Projekt speichern unter" — fuenf Felder ueber die Sichtklasse
        /// <c>EPOS.UI.Dialoge.Projekt.ProjektKopieKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Schluessel ist der Name der WinForms-Maske, die der Dialog abgeloest
        /// hat</b> — er steht so im Hilfeschluessel (<c>Form_ProjektSpeichernUnter.btn_Help</c>),
        /// im Bereichsregister der Windows-Huelle und in der Navigationstabelle.
        /// </para>
        /// <para>
        /// <b>Das QUELLPROJEKT ist ein Wahlfeld</b> (KI-D-Q6). Es zu setzen belegt
        /// Beschreibung, Kunde und Bearbeiter aus dem Quellprojekt vor — genau wie ein
        /// Klick in die Liste; das ist die Absicht der Maske und keine Nebenwirkung.
        /// </para>
        /// <para>
        /// <b>Kein Speicherweg.</b> Das OK startet einen LAUF mit Fortschrittsanzeige
        /// und Abbruchknopf, der ein ganzes Projekt dupliziert; ihn ueber
        /// <c>dialog_speichern</c> auszuloesen hiesse, eine Aktion der Stufe 3 ohne
        /// ihren eigenen Bestaetigungsweg zu starten. Die PRUEFUNG ist dagegen
        /// dieselbe wie am OK-Knopf. Die SUCHE bleibt draussen: Sie schraenkt die
        /// Projektliste ein und ist damit Teil einer Menge von Verweisen.
        /// </para>
        /// </remarks>
        private static KiDialog ProjektKopie()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PROJEKT_KOPIE,
                anzeigename: KiDialogTexte.MaskeProjektKopie,
                felder: new[]
                {
                    new KiDialogFeld("quellprojekt", "ProjektKopieKiSicht.Quellprojekt",
                                     KiDialogTexte.PrkQuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PrkQuelleErl, leerErlaubt: true),
                    new KiDialogFeld("neuer_name", "ProjektKopieKiSicht.NeuerName",
                                     KiDialogTexte.PrkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.PrkNameErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "ProjektKopieKiSicht.Beschreibung",
                                     KiDialogTexte.PrkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.PrkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("kunde", "ProjektKopieKiSicht.Kunde",
                                     KiDialogTexte.PrkKundeName, KiParameterTyp.Text,
                                     KiDialogTexte.PrkKundeErl, leerErlaubt: true),
                    new KiDialogFeld("bearbeiter", "ProjektKopieKiSicht.Bearbeiter",
                                     KiDialogTexte.PrkBearbeiterName, KiParameterTyp.Text,
                                     KiDialogTexte.PrkBearbeiterErl, leerErlaubt: true)
                });
        }

        // =====================================================================
        // Projektvariante  ->  ProjektVarianteDialog   (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// „Als Variante speichern" — drei Einstellwerte und eine Anzeige ueber
        /// <c>EPOS.UI.Dialoge.Projekt.ProjektVarianteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der ZIELNAME ist gerechnet</b> (<c>VariantenCtrl.Zielname</c> samt
        /// Zaehler bei Namensgleichheit) und deshalb nur lesbar — eingeben laesst sich
        /// der Bezeichner, aus dem er folgt.
        /// </para>
        /// <para>
        /// <b>Der Bezeichner folgt dem Quellprojekt, bis er von Hand getippt wird.</b>
        /// Ein vom Assistenten gesetzter Bezeichner gilt als von Hand getippt und
        /// bleibt deshalb stehen, auch wenn danach ein anderes Quellprojekt gewaehlt
        /// wird — dieselbe Regel wie am Eingabefeld.
        /// </para>
        /// </remarks>
        private static KiDialog ProjektVariante()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PROJEKT_VARIANTE,
                anzeigename: KiDialogTexte.MaskeProjektVariante,
                felder: new[]
                {
                    new KiDialogFeld("aus_quelle", "ProjektVarianteKiSicht.AusQuelle",
                                     KiDialogTexte.PrvHakenName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PrvHakenErl),
                    new KiDialogFeld("quellprojekt", "ProjektVarianteKiSicht.Quellprojekt",
                                     KiDialogTexte.PrkQuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PrvQuelleErl, leerErlaubt: true),
                    new KiDialogFeld("bezeichner", "ProjektVarianteKiSicht.Bezeichner",
                                     KiDialogTexte.PrvBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.PrvBezeichnerErl, leerErlaubt: true),
                    new KiDialogFeld("zielname", "ProjektVarianteKiSicht.Zielname",
                                     KiDialogTexte.PrvZielnameName, KiParameterTyp.Text,
                                     KiDialogTexte.PrvZielnameErl,
                                     leerErlaubt: true, nurLesen: true)
                });
        }

        // =====================================================================
        // Form_PeakShaving  ->  PeakShavingDialog   (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// Die Lastspitzenkappung — die zweiundzwanzig Felder, die
        /// <c>PeakShavingDialog</c> sichtbar traegt, ueber die Sichtklasse
        /// <c>EPOS.UI.Dialoge.Strom.PeakShavingKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Sie ist die erste freigegebene Maske, die NICHTS ablegt.</b> „Abgelegt
        /// wird nichts" steht woertlich als Hinweiszeile darin, und ihr einziger
        /// Fussknopf schliesst sie. Es gibt deshalb keinen Speicherweg — wohl aber
        /// eine PRUEFUNG: dieselben sieben Pflichtzahlen und vier Fachregeln, die auch
        /// der Rechenknopf zieht (<c>PeakShavingEingaben.Pruefe</c>).
        /// </para>
        /// <para>
        /// <b>Drei Felder sind ABGELEITET und nur lesbar</b> — das offene Reiterblatt,
        /// die Reihenzeile und die Herkunftszeile. Ein Blattwechsel ist eine
        /// Bedienhandlung und kein Feldwert (dieselbe Begruendung wie beim Schritt der
        /// Stromspeicher-Ansicht); Reihen- und Herkunftszeile sind Anzeigen des
        /// geladenen Lastgangs.
        /// </para>
        /// <para>
        /// <b>Die DATEIWAHL bleibt draussen</b> (KI-D-Q6): Eine Datei einzulesen ist
        /// ein Ladevorgang. Die QUELLE steht trotzdem im Katalog — sie ist die Spalte
        /// „Quelle" der Lastgangliste (Stufe 5 der Neuordnung): „Ganglinie" waehlt die
        /// erste Ganglinienzeile, „Datei" die zuletzt eingelesene Datei, wie ein Klick;
        /// ohne eingelesene Datei lehnt die Setzung benannt ab. Die GANGLINIE waehlt
        /// ihre Zeile. Der Pfad selbst laedt nichts.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> „Berechnen", „Minimale Schwelle" und „In Variante
        /// uebernehmen" sind rechnende bzw. datenbankwirksame Aktionen der Stufen 2 und
        /// 3 — dieselbe Begruendung wie bei der Stromspeicher-Ansicht.
        /// </para>
        /// </remarks>
        private static KiDialog PeakShaving()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PEAK_SHAVING,
                anzeigename: KiDialogTexte.MaskePeakShaving,
                felder: new[]
                {
                    // ---- Der Lastgang -----------------------------------------------
                    new KiDialogFeld("quelle", "PeakShavingKiSicht.Quelle",
                                     KiDialogTexte.PeakQuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PeakQuelleErl),
                    new KiDialogFeld("ganglinie", "PeakShavingKiSicht.Ganglinie",
                                     KiDialogTexte.PeakGanglinieName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PeakGanglinieErl, leerErlaubt: true),
                    new KiDialogFeld("reihe", "PeakShavingKiSicht.Reihe",
                                     KiDialogTexte.PeakReiheName, KiParameterTyp.Text,
                                     KiDialogTexte.PeakReiheErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("herkunft", "PeakShavingKiSicht.Herkunft",
                                     KiDialogTexte.PeakHerkunftName, KiParameterTyp.Text,
                                     KiDialogTexte.PeakHerkunftErl,
                                     leerErlaubt: true, nurLesen: true),

                    // ---- Der Speicher -----------------------------------------------
                    new KiDialogFeld("leistung", "PeakShavingKiSicht.Leistung",
                                     KiDialogTexte.PeakPName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakPErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kapazitaet", "PeakShavingKiSicht.Kapazitaet",
                                     KiDialogTexte.PeakKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad", "PeakShavingKiSicht.Wirkungsgrad",
                                     KiDialogTexte.PeakEtaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakEtaErl, leerErlaubt: true),
                    new KiDialogFeld("soc_min", "PeakShavingKiSicht.SocMin",
                                     KiDialogTexte.PeakSocMinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakSocMinErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("soc_max", "PeakShavingKiSicht.SocMax",
                                     KiDialogTexte.PeakSocMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakSocMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("start_soc", "PeakShavingKiSicht.StartSoc",
                                     KiDialogTexte.PeakStartSocName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakStartSocErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),

                    // ---- Die Schwelle -----------------------------------------------
                    new KiDialogFeld("adaptiv", "PeakShavingKiSicht.Adaptiv",
                                     KiDialogTexte.PeakAdaptivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PeakAdaptivErl),
                    new KiDialogFeld("zielschwelle", "PeakShavingKiSicht.Zielschwelle",
                                     KiDialogTexte.PeakZielName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakZielErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),

                    // ---- Die Wirtschaftlichkeit -------------------------------------
                    new KiDialogFeld("leistungspreis", "PeakShavingKiSicht.Leistungspreis",
                                     KiDialogTexte.PeakLpName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakLpErl, leerErlaubt: true),
                    new KiDialogFeld("bezugspreis", "PeakShavingKiSicht.Bezugspreis",
                                     KiDialogTexte.PeakBezugspreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakBezugspreisErl, leerErlaubt: true),
                    new KiDialogFeld("kompatibel", "PeakShavingKiSicht.Kompatibel",
                                     KiDialogTexte.PeakKompatName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PeakKompatErl),
                    new KiDialogFeld("kosten_kapazitaet", "PeakShavingKiSicht.KostenKapazitaet",
                                     KiDialogTexte.PeakCCapName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakCCapErl, leerErlaubt: true),
                    new KiDialogFeld("kosten_leistung", "PeakShavingKiSicht.KostenLeistung",
                                     KiDialogTexte.PeakCPowName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakCPowErl, leerErlaubt: true),
                    new KiDialogFeld("investition_fix", "PeakShavingKiSicht.InvestitionFix",
                                     KiDialogTexte.PeakIFixName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakIFixErl, leerErlaubt: true),
                    new KiDialogFeld("zins", "PeakShavingKiSicht.Zins",
                                     KiDialogTexte.PeakZinsName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakZinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("nutzungsdauer", "PeakShavingKiSicht.Nutzungsdauer",
                                     KiDialogTexte.PeakNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PeakNutzungsdauerErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR, leerErlaubt: true),

                    // ---- Die Anzeige ------------------------------------------------
                    new KiDialogFeld("ladezustand", "PeakShavingKiSicht.Ladezustand",
                                     KiDialogTexte.PeakSocName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PeakSocErl),
                    new KiDialogFeld("reiter", "PeakShavingKiSicht.Reiter",
                                     KiDialogTexte.PeakReiterName, KiParameterTyp.Text,
                                     KiDialogTexte.PeakReiterErl,
                                     leerErlaubt: true, nurLesen: true)
                });
        }

        // =====================================================================
        // Speicherzeitreihen  ->  SpeicherZeitreihenDialog   (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// Die Leseregeln einer Speicher-Zeitreihe — die achtzehn Felder, die
        /// <c>SpeicherZeitreihenDialog</c> sichtbar traegt, ueber die Sichtklasse
        /// <c>EPOS.UI.Dialoge.Strom.SpeicherZeitreihenKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Maske, die REGELN einstellt, ist kein Ladevorgang.</b> Draussen
        /// bleibt nach KI-D-Q6, was LAEDT — die Dateiwahl und der Knopf „Uebernehmen";
        /// hier steht, WIE die gewaehlte Datei gelesen wird. Jedes Setzen rechnet die
        /// Vorschau neu, genau wie ein Griff in die Klappliste.
        /// </para>
        /// <para>
        /// <b>Zwei Felder sind ABGELEITET</b> — der Dateiname der Herleitungszeile und
        /// die Rolle der Reihe. Die Rolle kommt als Parameter des Wirtes herein (Last,
        /// PV oder Bezugspreis) und entscheidet, welche Einheiten zur Wahl stehen; sie
        /// hier zu setzen hiesse, eine andere Reihe einzulesen als die, fuer die der
        /// Anwender den Knopf gedrueckt hat.
        /// </para>
        /// <para>
        /// <b>Datums- und Uhrzeitfelder stehen NEBEN dem Zeitstempel.</b> Die Maske
        /// zeigt je nach Zeitangabe das eine oder das andere Paar — beide gehoeren zu
        /// ihr, und welches gerade sichtbar ist, sagt das Feld <c>zeitangabe</c>. Ein
        /// Katalog, der nur die gerade sichtbare Haelfte fuehrte, wechselte seinen
        /// Umfang mit einer Klappliste.
        /// </para>
        /// <para>
        /// <b>Die VORSCHAUTABELLE bleibt draussen</b>: Sie ist eine Anzeige der ersten
        /// zwanzig Zeilen und kein Eingabefeld.
        /// </para>
        /// </remarks>
        private static KiDialog Speicherzeitreihen()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SPEICHER_ZEITREIHEN,
                anzeigename: KiDialogTexte.MaskeSpeicherzeitreihen,
                felder: new[]
                {
                    // ---- Woran der Anwender gerade arbeitet -------------------------
                    new KiDialogFeld("datei", "SpeicherZeitreihenKiSicht.Datei",
                                     KiDialogTexte.SzrDateiName, KiParameterTyp.Text,
                                     KiDialogTexte.SzrDateiErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("rolle", "SpeicherZeitreihenKiSicht.Rolle",
                                     KiDialogTexte.SzrRolleName, KiParameterTyp.Text,
                                     KiDialogTexte.SzrRolleErl,
                                     leerErlaubt: true, nurLesen: true),

                    // ---- CSV-Format -------------------------------------------------
                    new KiDialogFeld("trennzeichen", "SpeicherZeitreihenKiSicht.Trennzeichen",
                                     KiDialogTexte.SzrTrennzeichenName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrTrennzeichenErl),
                    new KiDialogFeld("dezimaltrenner", "SpeicherZeitreihenKiSicht.Dezimaltrenner",
                                     KiDialogTexte.SzrDezimalName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrDezimalErl),
                    new KiDialogFeld("kodierung", "SpeicherZeitreihenKiSicht.Kodierung",
                                     KiDialogTexte.SzrKodierungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrKodierungErl),
                    new KiDialogFeld("kopfzeile", "SpeicherZeitreihenKiSicht.Kopfzeile",
                                     KiDialogTexte.SzrKopfzeileName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SzrKopfzeileErl),
                    new KiDialogFeld("zeilen_ueberspringen",
                                     "SpeicherZeitreihenKiSicht.ZeilenUeberspringen",
                                     KiDialogTexte.SzrUeberspringenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SzrUeberspringenErl),

                    // ---- Spalten und Zeit -------------------------------------------
                    new KiDialogFeld("zeitangabe", "SpeicherZeitreihenKiSicht.Zeitangabe",
                                     KiDialogTexte.SzrZeitangabeName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrZeitangabeErl),
                    new KiDialogFeld("zeitstempelspalte",
                                     "SpeicherZeitreihenKiSicht.Zeitstempelspalte",
                                     KiDialogTexte.SzrZeitstempelspalteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrZeitstempelspalteErl),
                    new KiDialogFeld("zeitstempelformat",
                                     "SpeicherZeitreihenKiSicht.Zeitstempelformat",
                                     KiDialogTexte.SzrZeitstempelformatName, KiParameterTyp.Text,
                                     KiDialogTexte.SzrZeitstempelformatErl, leerErlaubt: true),
                    new KiDialogFeld("datumsspalte", "SpeicherZeitreihenKiSicht.Datumsspalte",
                                     KiDialogTexte.SzrDatumsspalteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrDatumsspalteErl),
                    new KiDialogFeld("datumsformat", "SpeicherZeitreihenKiSicht.Datumsformat",
                                     KiDialogTexte.SzrDatumsformatName, KiParameterTyp.Text,
                                     KiDialogTexte.SzrDatumsformatErl, leerErlaubt: true),
                    new KiDialogFeld("uhrzeitspalte", "SpeicherZeitreihenKiSicht.Uhrzeitspalte",
                                     KiDialogTexte.SzrUhrzeitspalteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrUhrzeitspalteErl),
                    new KiDialogFeld("uhrzeitformat", "SpeicherZeitreihenKiSicht.Uhrzeitformat",
                                     KiDialogTexte.SzrUhrzeitformatName, KiParameterTyp.Text,
                                     KiDialogTexte.SzrUhrzeitformatErl, leerErlaubt: true),
                    new KiDialogFeld("wertspalte", "SpeicherZeitreihenKiSicht.Wertspalte",
                                     KiDialogTexte.SzrWertspalteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrWertspalteErl),
                    new KiDialogFeld("zeitzone", "SpeicherZeitreihenKiSicht.Zeitzone",
                                     KiDialogTexte.SzrZeitzoneName, KiParameterTyp.Text,
                                     KiDialogTexte.SzrZeitzoneErl, leerErlaubt: true),
                    new KiDialogFeld("intervallbezug", "SpeicherZeitreihenKiSicht.Intervall",
                                     KiDialogTexte.SzrIntervallName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrIntervallErl),
                    new KiDialogFeld("einheit", "SpeicherZeitreihenKiSicht.Einheit",
                                     KiDialogTexte.SzrEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SzrEinheitErl)
                });
        }

        // =====================================================================
        // Form_Stromganglinie_Admin  ->  StromganglinieAdminDialog  (Welle KI-F6)
        // =====================================================================

        /// <summary>
        /// Die Stammdatenverwaltung der Stromganglinien — zwei Felder ueber die
        /// Sichtklasse <c>EPOS.UI.Dialoge.Strom.StromganglinieAdminKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Diese Maske fuehrt genau EINEN Einstellwert</b>: das Zeitraster, mit dem
        /// eine eingelesene Datei abgelegt wird. Alles andere darauf ist SUCHE, AUSWAHL
        /// und ANZEIGE — die virtualisierte Katalogliste samt Filterstand, die
        /// gerechneten Spalten Jahresarbeit und Spitze, der Loeschweg mit seiner
        /// Rueckfrage und die Importkette. Nach KI-D-Q5 bleibt beides draussen: Eine
        /// Menge von Verweisen ist kein Feldwert, und eine Datei einzulesen ist ein
        /// Ladevorgang.
        /// </para>
        /// <para>
        /// <b>Freigegeben ist sie trotzdem</b>, damit <c>dialog_lesen</c> nennt, woran
        /// der Anwender gerade arbeitet, und <c>feld_setzen</c> benannt ablehnt statt
        /// „Maske nicht freigegeben" — dieselbe Begruendung wie beim Reiterblatt
        /// „Kosten" und bei der Leistungspreisreihe der Welle KI-F4. Die MARKIERUNG
        /// steht als Anzeige daneben: Sie entscheidet, was der Loeschknopf traefe.
        /// </para>
        /// </remarks>
        private static KiDialog StromganglinieAdmin()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STROMGANGLINIE_ADMIN,
                anzeigename: KiDialogTexte.MaskeStromganglinieAdmin,
                felder: new[]
                {
                    new KiDialogFeld("zeitintervall",
                                     "StromganglinieAdminKiSicht.Zeitintervall",
                                     KiDialogTexte.SgaZeitintervallName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SgaZeitintervallErl),
                    new KiDialogFeld("markierte_ganglinie",
                                     "StromganglinieAdminKiSicht.Gewaehlt",
                                     KiDialogTexte.SgaGewaehltName, KiParameterTyp.Text,
                                     KiDialogTexte.SgaGewaehltErl,
                                     leerErlaubt: true, nurLesen: true)
                });
        }

        // =====================================================================
        // Kostenseite  ->  Seiten.Berichte.KostenSeite   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Das Reiterblatt „Kosten" — drei Felder aus
        /// <c>EPOS.UI.Seiten.Berichte.KostenSeiteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Seite fuehrt genau EINEN Einstellwert</b>: die markierte Anlage. Sie
        /// entscheidet, welche Komponente die Kostenverwaltung oeffnet und welche
        /// Energietraegerzeile hervorgehoben steht. Alles andere - die drei
        /// Kennzahlkarten, die Gegenueberstellung der Versionen, die Komponenten- und
        /// die Traegertabelle - ist gerechnete ANZEIGE.
        /// </para>
        /// <para>
        /// <b>Freigegeben ist sie trotzdem</b>, damit <c>dialog_lesen</c> nennt, woran
        /// der Anwender arbeitet, und <c>feld_setzen</c> benannt ablehnt statt „Maske
        /// nicht freigegeben" - dieselbe Begruendung wie bei der Solarganglinienmaske
        /// der Welle KI-F3. Die VERGLEICHSWAHL bleibt draussen: Sie ist eine Menge von
        /// Verweisen, und ein Maskenfeld traegt genau einen Wert.
        /// </para>
        /// </remarks>
        private static KiDialog Kostenseite()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOSTENSEITE,
                anzeigename: KiDialogTexte.MaskeKostenseite,
                felder: new[]
                {
                    new KiDialogFeld("anlage", "KostenSeiteKiSicht.Anlage",
                                     KiDialogTexte.KseAnlageName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KseAnlageErl, leerErlaubt: true),
                    new KiDialogFeld("projektzeile", "KostenSeiteKiSicht.Projektzeile",
                                     KiDialogTexte.KseProjektName, KiParameterTyp.Text,
                                     KiDialogTexte.KseProjektErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("statuszeile", "KostenSeiteKiSicht.Statuszeile",
                                     KiDialogTexte.KseStatusName, KiParameterTyp.Text,
                                     KiDialogTexte.KseStatusErl,
                                     leerErlaubt: true, nurLesen: true)
                });
        }

        // =====================================================================
        // Wirtschaftlichkeitsseite  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Das Reiterblatt „Wirtschaftlichkeit" — siebzehn Felder aus
        /// <c>EPOS.UI.Seiten.Berichte.WirtschaftlichkeitSeiteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Diese Seite traegt Einstellwerte und ist deshalb drin.</b> Sie
        /// entscheidet, WELCHE Staende gegeneinander gerechnet werden
        /// (Vergleichssicht, Referenz, Paar A und B), unter WELCHEM Szenario - und sie
        /// pflegt die nicht monetarisierbaren Wirkungen nach DIN EN 17463 (ETAPPE E17:
        /// als Liste mit Kategorie, Beschreibung, Dauer und drei Wirkungsgraden je Zeile,
        /// dazu die Zahl der Zeilen und die Beurteilung als Anzeige).
        /// </para>
        /// <para>
        /// <b>Jedes dieser Felder ist an der Seite ein WEG und kein Wert</b>: Die
        /// Seite holt sich zu jeder Wahl einen NEUEN Stand aus der Huelle und rechnet
        /// das Warnband nach. Sie bindet deshalb ueber eine Sichtklasse, die jede
        /// Setzung durch denselben Rueckruf schickt wie ein Griff in die Klappliste.
        /// </para>
        /// <para>
        /// <b>Die zwei Anzeigewahlen von Block 2 der ValERI-Bewertung sind drin</b>
        /// (KI-D-Q11): Stand und Szenario der Zahlungsreihen waehlen nur, welche schon
        /// gelieferte Jahrestafel die Seite zeigt - kein neuer Stand, kein Nachrechnen.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die Kennzahltabelle, die Herleitungszeilen und das Bild
        /// des Kapitalwertverlaufs</b> (gerechnete Anzeige) sowie die VERGLEICHSGRUPPE:
        /// Welche Varianten angehakt sind, ist eine Menge von Verweisen und kein
        /// Feldwert.
        /// </para>
        /// <para>
        /// <b>Drin ist seit Welle #458 (Stufe 2) die Bedienleiste des Verlaufs</b>
        /// (<c>KapitalwertVerlaufAbschnitt</c>, ein Baustein dieser Seite): der Zeitraum
        /// und je Stand und je Szenario ein Haken als SPALTE. Ein Haken zeichnet nur neu;
        /// gerechnet wird erst mit „Aktualisieren", das der Anwender drueckt.
        /// </para>
        /// </remarks>
        private static KiDialog Wirtschaftlichkeitsseite()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE,
                anzeigename: KiDialogTexte.MaskeWirtschaftsseite,
                felder: new[]
                {
                    new KiDialogFeld("szenario", "WirtschaftlichkeitSeiteKiSicht.Szenario",
                                     KiDialogTexte.WseSzenarioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseSzenarioErl, leerErlaubt: true),
                    new KiDialogFeld("vergleichssicht",
                                     "WirtschaftlichkeitSeiteKiSicht.Vergleichssicht",
                                     KiDialogTexte.WseSichtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseSichtErl, leerErlaubt: true),
                    new KiDialogFeld("referenz", "WirtschaftlichkeitSeiteKiSicht.Referenz",
                                     KiDialogTexte.WseReferenzName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseReferenzErl, leerErlaubt: true),
                    new KiDialogFeld("stand_a", "WirtschaftlichkeitSeiteKiSicht.StandA",
                                     KiDialogTexte.WseAName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseAErl, leerErlaubt: true),
                    new KiDialogFeld("stand_b", "WirtschaftlichkeitSeiteKiSicht.StandB",
                                     KiDialogTexte.WseBName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseBErl, leerErlaubt: true),
                    // ---- ETAPPE E17 (V-G11): die nicht monetarisierbaren Wirkungen als
                    //      Liste - die Zahl legt Zeilen an oder nimmt sie vom Ende, je Zeile
                    //      Kategorie, Beschreibung, Dauer und drei Wirkungsgrade; die
                    //      Beurteilung ist Anzeige. Geschrieben wird mit „Speichern".
                    new KiDialogFeld("wirkung_anzahl",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungsanzahl",
                                     KiDialogTexte.WseWirkungAnzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WseWirkungAnzahlErl, min: 0),
                    new KiDialogFeld("wirkung_kategorie",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Kategorie",
                                     KiDialogTexte.WseWirkungKategorieName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseWirkungKategorieErl,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN),
                    new KiDialogFeld("wirkung_beschreibung",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Beschreibung",
                                     KiDialogTexte.WseWirkungName, KiParameterTyp.Text,
                                     KiDialogTexte.WseWirkungErl, leerErlaubt: true,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN),
                    new KiDialogFeld("wirkung_dauer",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Dauer",
                                     KiDialogTexte.WseWirkungDauerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseWirkungDauerErl, leerErlaubt: true,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN),
                    new KiDialogFeld("wirkung_organisation",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Organisation",
                                     KiDialogTexte.WseWirkungOrganisationName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseWirkungGradErl, leerErlaubt: true,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN),
                    new KiDialogFeld("wirkung_mitarbeiter",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Mitarbeiter",
                                     KiDialogTexte.WseWirkungMitarbeiterName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseWirkungGradErl, leerErlaubt: true,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN),
                    new KiDialogFeld("wirkung_umwelt",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Umwelt",
                                     KiDialogTexte.WseWirkungUmweltName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseWirkungGradErl, leerErlaubt: true,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN),
                    new KiDialogFeld("wirkung_beurteilung",
                                     "WirtschaftlichkeitSeiteKiSicht.Wirkungen[].Beurteilung",
                                     KiDialogTexte.WseWirkungBeurteilungName, KiParameterTyp.Text,
                                     KiDialogTexte.WseWirkungBeurteilungErl, leerErlaubt: true,
                                     zeilenkennzeichen: WIRKUNGSKENNZEICHEN, nurLesen: true),
                    new KiDialogFeld("zahlungsreihen_stand",
                                     "WirtschaftlichkeitSeiteKiSicht.ZahlungsreihenStand",
                                     KiDialogTexte.WseZrStandName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseZrStandErl, leerErlaubt: true),
                    new KiDialogFeld("zahlungsreihen_szenario",
                                     "WirtschaftlichkeitSeiteKiSicht.ZahlungsreihenSzenario",
                                     KiDialogTexte.WseZrSzenarioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WseZrSzenarioErl, leerErlaubt: true),

                    // ---- Der Abschnitt „Verlauf" (Welle #458, Stufe 2) ---------------
                    new KiDialogFeld("verlauf_zeitraum",
                                     "WirtschaftlichkeitSeiteKiSicht.VerlaufZeitraum",
                                     KiDialogTexte.WseVerlaufZeitraumName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WseVerlaufZeitraumErl, leerErlaubt: true),
                    new KiDialogFeld("verlauf_anzeige",
                                     "WirtschaftlichkeitSeiteKiSicht.Verlaufsschalter[].An",
                                     KiDialogTexte.WseVerlaufAnzeigeName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WseVerlaufAnzeigeErl, zeilenkennzeichen: "Name")
                });
        }

        // =====================================================================
        // Form_WirtschaftlichkeitParameter  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die Wirtschaftlichkeits-Parameter — 35 Felder aus
        /// <c>EPOS.UI.Dialoge.Wirtschaftlichkeit.WirtschaftlichkeitParameterKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Objekte, ein Feldsatz.</b> Siebzehn Felder gehoeren dem Parametersatz
        /// des Projekts, achtzehn den zwei SZENARIOSAETZEN Best und Worst - je Groesse
        /// eines. Vierzehn der Szenariofelder zeigen den WIRKSAMEN Wert und nicht den
        /// gepflegten (die vier aus E9b unten tragen den gepflegten): Ein leeres
        /// Feld gaebe es sonst fuer jede Vorgabe, und niemand saehe, womit gerechnet
        /// wird. Gesetzt wird dagegen der gepflegte Wert - wer tippt, pflegt.
        /// </para>
        /// <para>
        /// <b>ETAPPE E9b: vier Felder mehr</b> - die Zeilen 8 und 9 der Tafel,
        /// Betrachtungszeitraum und Mengenaenderung je Szenario. Sie haben keine Vorgabe
        /// und tragen deshalb den GEPFLEGTEN Wert (leer = wie Erwartet).
        /// </para>
        /// <para>
        /// <b>Die Erwartet-Spalte der Szenariotabelle steht NICHT im Katalog</b>: Sie
        /// wiederholt, was oben unter „Allgemein" gepflegt wird, und traegt deshalb
        /// kein eigenes Feld. Ebenfalls draussen: die Herleitungszeilen, der Knopf
        /// „Vorgaben" (er setzt alle achtzehn Felder der Szenariotafel zurueck - ein Weg,
        /// kein Wert), der ±-Knopf der Einspeiseverguetung (ETAPPE E9b: er oeffnet die
        /// Maske Form_CaseEingabe, die ihre Felder selbst anmeldet) und der
        /// Gesetzeskatalog, der als Ueberlagerung aufgeht und seinen eigenen Schluessel
        /// traegt.
        /// </para>
        /// </remarks>
        private static KiDialog WirtschaftlichkeitParameter()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER,
                anzeigename: KiDialogTexte.MaskeWirtParameter,
                felder: new[]
                {
                    // ---- Allgemein --------------------------------------------------
                    new KiDialogFeld("zinssatz", "WirtschaftlichkeitParameterKiSicht.Zinssatz",
                                     KiDialogTexte.WpaZinsName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaZinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("betrachtungszeitraum",
                                     "WirtschaftlichkeitParameterKiSicht.Betrachtungszeitraum",
                                     KiDialogTexte.WpaJahreName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaJahreErl,
                                     einheit: KiDialogTexte.EinheitJahre),
                    new KiDialogFeld("preissteigerung_energie",
                                     "WirtschaftlichkeitParameterKiSicht.PreissteigerungEnergie",
                                     KiDialogTexte.WpaPreisEName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaPreisEErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("preissteigerung_betrieb",
                                     "WirtschaftlichkeitParameterKiSicht.PreissteigerungBetrieb",
                                     KiDialogTexte.WpaPreisBName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaPreisBErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("preissteigerung_investition",
                                     "WirtschaftlichkeitParameterKiSicht.PreissteigerungInvestition",
                                     KiDialogTexte.WpaPreisIName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaPreisIErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A, leerErlaubt: true),

                    // ---- Strom, Brennstoff, Bilanzierung ----------------------------
                    new KiDialogFeld("einspeiseverguetung",
                                     "WirtschaftlichkeitParameterKiSicht.Einspeiseverguetung",
                                     KiDialogTexte.WpaEinspName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaEinspErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH),
                    // ETAPPE E19 (Konzept § 6.3 Nr. 33, E19‑Q5 a): die Unternehmensart nach
                    // StromStG — im Dialog nur ohne BHKW sichtbar; mit BHKW lehnt der Setzer
                    // benannt ab und nennt den Dialog „BHKW-Wirtschaftlichkeit".
                    new KiDialogFeld("unternehmensart",
                                     "WirtschaftlichkeitParameterKiSicht.Unternehmensart",
                                     KiDialogTexte.BhwUaName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwUaErl, leerErlaubt: true),
                    new KiDialogFeld("co2_preis", "WirtschaftlichkeitParameterKiSicht.Co2Preis",
                                     KiDialogTexte.WpaCo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaCo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_T),
                    new KiDialogFeld("kraftwerkspark",
                                     "WirtschaftlichkeitParameterKiSicht.Kraftwerkspark",
                                     KiDialogTexte.WpaParkName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaParkErl, leerErlaubt: true),
                    new KiDialogFeld("bilanzjahr", "WirtschaftlichkeitParameterKiSicht.Bilanzjahr",
                                     KiDialogTexte.WpaBilanzjahrName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaBilanzjahrErl),
                    new KiDialogFeld("emissionsmethode",
                                     "WirtschaftlichkeitParameterKiSicht.Emissionsmethode",
                                     KiDialogTexte.WpaMethodeName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaMethodeErl, leerErlaubt: true),
                    new KiDialogFeld("biomassekonvention",
                                     "WirtschaftlichkeitParameterKiSicht.Biomassekonvention",
                                     KiDialogTexte.WpaBiomasseName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBiomasseErl, leerErlaubt: true),
                    new KiDialogFeld("nachhaltigkeitsnachweis",
                                     "WirtschaftlichkeitParameterKiSicht.Nachhaltigkeitsnachweis",
                                     KiDialogTexte.WpaNachweisName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaNachweisErl),

                    // ---- Szenarien: je Groesse Best und Worst -----------------------
                    new KiDialogFeld("best_zinssatz",
                                     "WirtschaftlichkeitParameterKiSicht.BestZinssatz",
                                     KiDialogTexte.WpaSzBestZins, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzZinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("worst_zinssatz",
                                     "WirtschaftlichkeitParameterKiSicht.WorstZinssatz",
                                     KiDialogTexte.WpaSzWorstZins, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzZinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("best_preissteigerung_energie",
                                     "WirtschaftlichkeitParameterKiSicht.BestPreissteigerungEnergie",
                                     KiDialogTexte.WpaSzBestPreisE, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzPreisEErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("worst_preissteigerung_energie",
                                     "WirtschaftlichkeitParameterKiSicht.WorstPreissteigerungEnergie",
                                     KiDialogTexte.WpaSzWorstPreisE, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzPreisEErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("best_preissteigerung_betrieb",
                                     "WirtschaftlichkeitParameterKiSicht.BestPreissteigerungBetrieb",
                                     KiDialogTexte.WpaSzBestPreisB, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzPreisBErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("worst_preissteigerung_betrieb",
                                     "WirtschaftlichkeitParameterKiSicht.WorstPreissteigerungBetrieb",
                                     KiDialogTexte.WpaSzWorstPreisB, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzPreisBErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("best_preissteigerung_investition",
                                     "WirtschaftlichkeitParameterKiSicht.BestPreissteigerungInvestition",
                                     KiDialogTexte.WpaSzBestPreisI, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzPreisIErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("worst_preissteigerung_investition",
                                     "WirtschaftlichkeitParameterKiSicht.WorstPreissteigerungInvestition",
                                     KiDialogTexte.WpaSzWorstPreisI, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzPreisIErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A),
                    new KiDialogFeld("best_investition",
                                     "WirtschaftlichkeitParameterKiSicht.BestInvestitionAenderung",
                                     KiDialogTexte.WpaSzBestInvest, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzInvestErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("worst_investition",
                                     "WirtschaftlichkeitParameterKiSicht.WorstInvestitionAenderung",
                                     KiDialogTexte.WpaSzWorstInvest, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzInvestErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("best_ertrag",
                                     "WirtschaftlichkeitParameterKiSicht.BestErtragAenderung",
                                     KiDialogTexte.WpaSzBestErtrag, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzErtragErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("worst_ertrag",
                                     "WirtschaftlichkeitParameterKiSicht.WorstErtragAenderung",
                                     KiDialogTexte.WpaSzWorstErtrag, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzErtragErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("best_nutzungsdauer",
                                     "WirtschaftlichkeitParameterKiSicht.BestNutzungsdauerAenderung",
                                     KiDialogTexte.WpaSzBestDauer, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzDauerErl,
                                     einheit: KiDialogTexte.EinheitJahre),
                    new KiDialogFeld("worst_nutzungsdauer",
                                     "WirtschaftlichkeitParameterKiSicht.WorstNutzungsdauerAenderung",
                                     KiDialogTexte.WpaSzWorstDauer, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzDauerErl,
                                     einheit: KiDialogTexte.EinheitJahre),

                    // ---- ETAPPE E9b: Zeilen 8 und 9 der Szenariotafel ----------------
                    // Betrachtungszeitraum und Mengenaenderung je Szenario - OHNE
                    // Vorgabe (E9a-Q5): leer heisst "wie Erwartet". Anders als die
                    // vierzehn Felder darueber tragen sie den GEPFLEGTEN Wert.
                    new KiDialogFeld("best_zeitraum",
                                     "WirtschaftlichkeitParameterKiSicht.BestZeitraum",
                                     KiDialogTexte.WpaSzBestZeitraum, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSzZeitraumErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true),
                    new KiDialogFeld("worst_zeitraum",
                                     "WirtschaftlichkeitParameterKiSicht.WorstZeitraum",
                                     KiDialogTexte.WpaSzWorstZeitraum, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSzZeitraumErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true),
                    new KiDialogFeld("best_menge",
                                     "WirtschaftlichkeitParameterKiSicht.BestMenge",
                                     KiDialogTexte.WpaSzBestMenge, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzMengeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("worst_menge",
                                     "WirtschaftlichkeitParameterKiSicht.WorstMenge",
                                     KiDialogTexte.WpaSzWorstMenge, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaSzMengeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    // ---- Ende ETAPPE E9b ----------------------------------------------

                    // ---- ETAPPE E15 (V-G7): die Gruppe "Risiko" ----------------------
                    // Art (leer = aus), Zinszuschlag, R_loss und p_loss - nullbar, die
                    // Felder der nicht gewaehlten Art rechnen nicht (RisikoModul).
                    new KiDialogFeld("risiko_art",
                                     "WirtschaftlichkeitParameterKiSicht.RisikoArt",
                                     KiDialogTexte.WpaRisikoArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaRisikoArtErl, leerErlaubt: true),
                    new KiDialogFeld("risiko_zinszuschlag",
                                     "WirtschaftlichkeitParameterKiSicht.RisikoZinszuschlag",
                                     KiDialogTexte.WpaRisikoZuschlagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaRisikoZuschlagErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("risiko_verlust",
                                     "WirtschaftlichkeitParameterKiSicht.RisikoVerlust",
                                     KiDialogTexte.WpaRisikoVerlustName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaRisikoVerlustErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("risiko_wahrscheinlichkeit",
                                     "WirtschaftlichkeitParameterKiSicht.RisikoWahrscheinlichkeit",
                                     KiDialogTexte.WpaRisikoPName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaRisikoPErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true)
                    // ---- Ende ETAPPE E15 ---------------------------------------------
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_BhkwWirtschaftlichkeit  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die BHKW-Wirtschaftlichkeit — 25 Felder aus
        /// <c>EPOS.UI.Dialoge.Wirtschaftlichkeit.BhkwWirtschaftlichkeitKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Zwei Arbeitsstaende, ein Feldsatz.</b> Zwoelf Felder gehoeren der
        /// GEWAEHLTEN Anlage, zwoelf den projektweiten Vorgaben; das dreizehnte ist
        /// die Anlagenwahl selbst. Die Vorsilben <c>anlage_</c> und <c>projekt_</c>
        /// halten beide auseinander - Energiesteuerwahl und Aufteilungsmethode gibt es
        /// naemlich ZWEIMAL, einmal je Ebene.
        /// </para>
        /// <para>
        /// <b>Die Anlagentabelle bleibt draussen</b> (Projekt, Bezeichner, Leistung,
        /// Brennstoff sind Anzeige), ebenso die drei Vorschlagszeilen, die
        /// Kohaerenzpruefung, die Mengenkette und die Vorschau: Sie stehen als Text
        /// unter den Feldern, aus denen sie entstehen.
        /// </para>
        /// </remarks>
        private static KiDialog BhkwWirtschaftlichkeit()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BHKW_WIRTSCHAFTLICHKEIT,
                anzeigename: KiDialogTexte.MaskeBhkwWirtschaft,
                felder: new[]
                {
                    new KiDialogFeld("modul", "BhkwWirtschaftlichkeitKiSicht.Modul",
                                     KiDialogTexte.BhwModulName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwModulErl, leerErlaubt: true),

                    // ---- Die gewaehlte Anlage ---------------------------------------
                    new KiDialogFeld("anlage_stichtag",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageStichtag",
                                     KiDialogTexte.BhwStichtagAName, KiParameterTyp.Text,
                                     KiDialogTexte.BhwStichtagAErl, leerErlaubt: true),
                    new KiDialogFeld("anlage_inbetriebnahme",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageInbetriebnahme",
                                     KiDialogTexte.BhwIbnAName, KiParameterTyp.Text,
                                     KiDialogTexte.BhwIbnAErl, leerErlaubt: true),
                    new KiDialogFeld("anlage_anlagenart",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageAnlagenart",
                                     KiDialogTexte.BhwArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwArtErl, leerErlaubt: true),
                    new KiDialogFeld("anlage_eigenfall",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageEigenfall",
                                     KiDialogTexte.BhwFallName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwFallErl, leerErlaubt: true),
                    new KiDialogFeld("anlage_satz_einspeisung",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageSatzEinspeisung",
                                     KiDialogTexte.BhwSatzEinspName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwSatzEinspErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH, leerErlaubt: true),
                    new KiDialogFeld("anlage_satz_eigen",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageSatzEigen",
                                     KiDialogTexte.BhwSatzEigenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwSatzEigenErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH, leerErlaubt: true),
                    new KiDialogFeld("anlage_kontingent",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageKontingent",
                                     KiDialogTexte.BhwKontingentName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwKontingentErl,
                                     einheit: KiDialogTexte.EINHEIT_STUNDE, leerErlaubt: true),
                    new KiDialogFeld("anlage_deckel", "BhkwWirtschaftlichkeitKiSicht.AnlageDeckel",
                                     KiDialogTexte.BhwDeckelName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwDeckelErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A, leerErlaubt: true),
                    new KiDialogFeld("anlage_kostenanteil",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageKostenanteil",
                                     KiDialogTexte.BhwKostenanteilName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwKostenanteilErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("anlage_energiesteuer",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageEnergiesteuer",
                                     KiDialogTexte.BhwEsAName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwEsAErl, leerErlaubt: true),
                    new KiDialogFeld("anlage_aufteilung",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageAufteilung",
                                     KiDialogTexte.BhwAufAName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwAufAErl, leerErlaubt: true),
                    new KiDialogFeld("anlage_hilfsenergie",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageHilfsenergie",
                                     KiDialogTexte.BhwHilfsName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwHilfsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    // ETAPPE E7c (E7c1-Q7): der zweite Fall des § 2 Nr. 16 KWKG —
                    // Kennzeichen und Stromkennzahl der Anlage (KWKG_Abwaermeabfuhr,
                    // KWKG_Stromkennzahl), gepflegt in der Überlagerung „Sätze und Herkunft".
                    new KiDialogFeld("anlage_abwaermeabfuhr",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageAbwaermeabfuhr",
                                     KiDialogTexte.BhwAbwaermeName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BhwAbwaermeErl),
                    new KiDialogFeld("anlage_stromkennzahl",
                                     "BhkwWirtschaftlichkeitKiSicht.AnlageStromkennzahl",
                                     KiDialogTexte.BhwSigmaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwSigmaErl, leerErlaubt: true),

                    // ---- Die projektweiten Vorgaben ---------------------------------
                    new KiDialogFeld("projekt_einspeisung_kwk",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektEinspeisungKwk",
                                     KiDialogTexte.BhwEinspKwkName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwEinspKwkErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH, leerErlaubt: true),
                    new KiDialogFeld("projekt_abschlag",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektAbschlag",
                                     KiDialogTexte.BhwAbschlagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwAbschlagErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("projekt_pauschalmodus",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektPauschalmodus",
                                     KiDialogTexte.BhwPauschalName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BhwPauschalErl),
                    new KiDialogFeld("projekt_stichtag",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektStichtag",
                                     KiDialogTexte.BhwStichtagPName, KiParameterTyp.Text,
                                     KiDialogTexte.BhwStichtagPErl, leerErlaubt: true),
                    new KiDialogFeld("projekt_inbetriebnahme",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektInbetriebnahme",
                                     KiDialogTexte.BhwIbnPName, KiParameterTyp.Text,
                                     KiDialogTexte.BhwIbnPErl, leerErlaubt: true),
                    new KiDialogFeld("projekt_energiesteuer",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektEnergiesteuer",
                                     KiDialogTexte.BhwEsPName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwEsPErl, leerErlaubt: true),
                    new KiDialogFeld("projekt_aufteilung",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektAufteilung",
                                     KiDialogTexte.BhwAufPName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwAufPErl, leerErlaubt: true),
                    new KiDialogFeld("projekt_jahresnutzungsgrad",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektJahresnutzungsgrad",
                                     KiDialogTexte.BhwNutzungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhwNutzungsgradErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("projekt_unternehmensart",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektUnternehmensart",
                                     KiDialogTexte.BhwUaName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwUaErl, leerErlaubt: true),
                    new KiDialogFeld("projekt_raeumlich",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektRaeumlich",
                                     KiDialogTexte.BhwRaeumlichName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BhwRaeumlichErl),
                    new KiDialogFeld("projekt_hocheffizienz",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektHocheffizienz",
                                     KiDialogTexte.BhwHocheffizienzName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BhwHocheffizienzErl),
                    new KiDialogFeld("projekt_befreiungsmodus",
                                     "BhkwWirtschaftlichkeitKiSicht.ProjektBefreiungsmodus",
                                     KiDialogTexte.BhwModusName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhwModusErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Tarifstruktur  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die Tarifstruktur Strom — 14 Felder aus
        /// <c>EPOS.UI.Dialoge.Wirtschaftlichkeit.TarifstrukturKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ein Modell, das Rollenmodell</b> (Entscheid Q11, 22.09.2026: „kein
        /// HT/NT"): je ein Arbeits-, Grund- und Leistungspreis fuer Bezug und
        /// Reststrom, dazu die Einspeisung. Modellwahl, Hochtarif-Fenster,
        /// Zonenpreise und die zweistufige Staffel stehen nicht mehr auf der Maske
        /// und deshalb auch nicht hier; die Staffel fuehrt die Maske
        /// <see cref="KiMaskennamen.ENERGIETRAEGER"/>.
        /// </para>
        /// <para>
        /// <b>Die vier LEISTUNGSSTUFEN je Rolle bleiben draussen.</b> Zwoelf Zellen je
        /// Rolle (Obergrenze, Sommer- und Winterpreis × vier Stufen) entstehen aus
        /// einer Schleife ueber ihren Index und sind damit eine WERTETAFEL - dieselbe
        /// Regel, mit der diese Welle die Monatssaetze der Preisreihe und die
        /// Stundentafel des Kostenprofils auslaesst.
        /// </para>
        /// </remarks>
        private static KiDialog Tarifstruktur()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.TARIFSTRUKTUR,
                anzeigename: KiDialogTexte.MaskeTarifstruktur,
                felder: new[]
                {
                    new KiDialogFeld("aktiv", "TarifstrukturKiSicht.Aktiv",
                                     KiDialogTexte.TarAktivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.TarAktivErl),
                    new KiDialogFeld("gueltig_ab", "TarifstrukturKiSicht.GueltigAb",
                                     KiDialogTexte.TarGueltigAbName, KiParameterTyp.Text,
                                     KiDialogTexte.TarGueltigAbErl, leerErlaubt: true),

                    // ---- Winterspanne (Modell „Staffel" der Bezugsrollen) -----------
                    new KiDialogFeld("winter_von", "TarifstrukturKiSicht.WinterVon",
                                     KiDialogTexte.TarWinterVonName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.TarWinterVonErl, leerErlaubt: true),
                    new KiDialogFeld("winter_bis", "TarifstrukturKiSicht.WinterBis",
                                     KiDialogTexte.TarWinterBisName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.TarWinterBisErl, leerErlaubt: true),

                    // ---- Rollenmodell: Bezug ----------------------------------------
                    new KiDialogFeld("bezug_arbeitspreis",
                                     "TarifstrukturKiSicht.BezugArbeitspreis",
                                     KiDialogTexte.TarBezugArbeitName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarBezugArbeitErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH, leerErlaubt: true),
                    new KiDialogFeld("bezug_grundpreis", "TarifstrukturKiSicht.BezugGrundpreis",
                                     KiDialogTexte.TarBezugGrundName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarBezugGrundErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_A, leerErlaubt: true),
                    new KiDialogFeld("bezug_leistungsmodell",
                                     "TarifstrukturKiSicht.BezugLeistungsmodell",
                                     KiDialogTexte.TarBezugModellName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TarLeistungsmodellErl, leerErlaubt: true),
                    new KiDialogFeld("bezug_monatspreis", "TarifstrukturKiSicht.BezugMonatspreis",
                                     KiDialogTexte.TarBezugMonatName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarMonatspreisErl, leerErlaubt: true),

                    // ---- Rollenmodell: Reststrom ------------------------------------
                    new KiDialogFeld("rest_arbeitspreis", "TarifstrukturKiSicht.RestArbeitspreis",
                                     KiDialogTexte.TarRestArbeitName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarRestArbeitErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH, leerErlaubt: true),
                    new KiDialogFeld("rest_grundpreis", "TarifstrukturKiSicht.RestGrundpreis",
                                     KiDialogTexte.TarRestGrundName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarRestGrundErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_A, leerErlaubt: true),
                    new KiDialogFeld("rest_leistungsmodell",
                                     "TarifstrukturKiSicht.RestLeistungsmodell",
                                     KiDialogTexte.TarRestModellName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TarLeistungsmodellErl, leerErlaubt: true),
                    new KiDialogFeld("rest_monatspreis", "TarifstrukturKiSicht.RestMonatspreis",
                                     KiDialogTexte.TarRestMonatName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarMonatspreisErl, leerErlaubt: true),

                    // ---- Rollenmodell: Einspeisung ----------------------------------
                    new KiDialogFeld("einspeisung_arbeitspreis",
                                     "TarifstrukturKiSicht.EinspeisungArbeitspreis",
                                     KiDialogTexte.TarEinspArbeitName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarEinspArbeitErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH, leerErlaubt: true),
                    new KiDialogFeld("einspeisung_grundpreis",
                                     "TarifstrukturKiSicht.EinspeisungGrundpreis",
                                     KiDialogTexte.TarEinspGrundName, KiParameterTyp.Zahl,
                                     KiDialogTexte.TarEinspGrundErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_A, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PhotovoltaikVerguetung  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die Photovoltaik-Verguetung — 16 Felder aus
        /// <c>EPOS.UI.Dialoge.Wirtschaftlichkeit.PhotovoltaikVerguetungKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Draussen bleiben die Herleitungen</b> — die rechnerische Leistung, der
        /// anzulegende Wert aus dem Katalog, der Zulaessigkeitsstatus, die
        /// Anlagenwarnungen, die Vorschau und die Kennzahlzeile: Sie stehen als Text
        /// unter den Feldern, aus denen sie entstehen, und niemand tippt sie. Der
        /// Marktwertimport ist ein LADEVORGANG und gehoert ins Aktionsregister.
        /// </remarks>
        private static KiDialog PhotovoltaikVerguetung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PV_VERGUETUNG,
                anzeigename: KiDialogTexte.MaskePvVerguetung,
                felder: new[]
                {
                    new KiDialogFeld("aktiv", "PhotovoltaikVerguetungKiSicht.Aktiv",
                                     KiDialogTexte.PvvAktivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PvvAktivErl),
                    new KiDialogFeld("leistung", "PhotovoltaikVerguetungKiSicht.Leistung",
                                     KiDialogTexte.PvvLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KWP, leerErlaubt: true),
                    new KiDialogFeld("inbetriebnahme",
                                     "PhotovoltaikVerguetungKiSicht.Inbetriebnahme",
                                     KiDialogTexte.PvvIbnName, KiParameterTyp.Text,
                                     KiDialogTexte.PvvIbnErl, leerErlaubt: true),
                    new KiDialogFeld("degradation", "PhotovoltaikVerguetungKiSicht.Degradation",
                                     KiDialogTexte.PvvDegradationName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvDegradationErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_A, leerErlaubt: true),
                    new KiDialogFeld("einspeiseart", "PhotovoltaikVerguetungKiSicht.Einspeiseart",
                                     KiDialogTexte.PvvEinspeiseartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvvEinspeiseartErl, leerErlaubt: true),
                    new KiDialogFeld("anzulegender_wert",
                                     "PhotovoltaikVerguetungKiSicht.AnzulegenderWert",
                                     KiDialogTexte.PvvAwName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvAwErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH, leerErlaubt: true),
                    new KiDialogFeld("vermarktungsform",
                                     "PhotovoltaikVerguetungKiSicht.Vermarktungsform",
                                     KiDialogTexte.PvvVermarktungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvvVermarktungErl, leerErlaubt: true),
                    new KiDialogFeld("direktvermarktungsentgelt",
                                     "PhotovoltaikVerguetungKiSicht.Direktvermarktungsentgelt",
                                     KiDialogTexte.PvvDvName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvDvErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH, leerErlaubt: true),
                    new KiDialogFeld("ppa_preis", "PhotovoltaikVerguetungKiSicht.PpaPreis",
                                     KiDialogTexte.PvvPpaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvPpaErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH, leerErlaubt: true),
                    new KiDialogFeld("ppa_spotaufschlag",
                                     "PhotovoltaikVerguetungKiSicht.PpaSpotaufschlag",
                                     KiDialogTexte.PvvPpaAufschlagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvPpaAufschlagErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH, leerErlaubt: true),
                    new KiDialogFeld("paragraf_51", "PhotovoltaikVerguetungKiSicht.Paragraf51",
                                     KiDialogTexte.PvvPar51Name, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvvPar51Erl, leerErlaubt: true),
                    new KiDialogFeld("messsystemjahr",
                                     "PhotovoltaikVerguetungKiSicht.Messsystemjahr",
                                     KiDialogTexte.PvvImsysName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvvImsysErl, leerErlaubt: true),
                    new KiDialogFeld("ausfallanteil",
                                     "PhotovoltaikVerguetungKiSicht.Ausfallanteil",
                                     KiDialogTexte.PvvAusfallName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvvAusfallErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("paragraf_51a_kompensation",
                                     "PhotovoltaikVerguetungKiSicht.Paragraf51aKompensation",
                                     KiDialogTexte.PvvPar51aName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PvvPar51aErl),
                    new KiDialogFeld("bezug_aus_preisreihe",
                                     "PhotovoltaikVerguetungKiSicht.BezugAusPreisreihe",
                                     KiDialogTexte.PvvBezugName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PvvBezugErl),
                    new KiDialogFeld("kappung", "PhotovoltaikVerguetungKiSicht.Kappung",
                                     KiDialogTexte.PvvKappungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvvKappungErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Gesetzesparameter  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die gesetzlichen Parameter — zwei Felder aus
        /// <c>EPOS.UI.Dialoge.Wirtschaftlichkeit.GesetzeskatalogKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Die Katalogliste bleibt draussen</b>: Sie ist der Baustein
        /// <c>Katalogliste</c> mit eigenem Filter, eigener Suche und eigener
        /// Sortierung - sechs Spalten Anzeige je Zeile, keine davon hier eingebbar.
        /// Gepflegt wird eine Zeile im ZEILENEDITOR, und der traegt seinen eigenen
        /// Katalogschluessel.
        /// </remarks>
        private static KiDialog Gesetzeskatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GESETZESKATALOG,
                anzeigename: KiDialogTexte.MaskeGesetzeskatalog,
                felder: new[]
                {
                    new KiDialogFeld("klasse", "GesetzeskatalogKiSicht.Klasse",
                                     KiDialogTexte.GskKlasseName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GskKlasseErl, leerErlaubt: true),
                    new KiDialogFeld("zeile", "GesetzeskatalogKiSicht.Zeile",
                                     KiDialogTexte.GskZeileName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GskZeileErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("schliessen", "btn_Schliessen", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_GesetzparameterZeile  (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Der Zeileneditor der gesetzlichen Parameter — sieben Felder aus
        /// <c>EPOS.UI.Dialoge.Wirtschaftlichkeit.GesetzeskatalogZeileKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Warum eine EIGENE Maske.</b> Der Editor ist nur als Ueberlagerung im
        /// Gesetzeskatalog zu sehen, pflegt aber einen ganz anderen Gegenstand: EINE
        /// Jahreszeile mit Schluessel, Jahr, Wert, Einheit, Status und Quelle. Ein
        /// gemeinsamer Katalogeintrag haette dem Modell verschwiegen, ob es gerade
        /// ueber die Liste oder ueber eine Zeile spricht.
        /// </remarks>
        private static KiDialog GesetzeskatalogZeile()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GESETZESKATALOG_ZEILE,
                anzeigename: KiDialogTexte.MaskeGesetzeszeile,
                felder: new[]
                {
                    new KiDialogFeld("schluessel", "GesetzeskatalogZeileKiSicht.Schluessel",
                                     KiDialogTexte.GszSchluesselName, KiParameterTyp.Text,
                                     KiDialogTexte.GszSchluesselErl, leerErlaubt: true),
                    new KiDialogFeld("klasse", "GesetzeskatalogZeileKiSicht.Klasse",
                                     KiDialogTexte.GskKlasseName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GszKlasseErl, leerErlaubt: true),
                    new KiDialogFeld("jahr", "GesetzeskatalogZeileKiSicht.Jahr",
                                     KiDialogTexte.GszJahrName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GszJahrErl, leerErlaubt: true),
                    new KiDialogFeld("wert", "GesetzeskatalogZeileKiSicht.Wert",
                                     KiDialogTexte.GszWertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GszWertErl, leerErlaubt: true),
                    new KiDialogFeld("einheit", "GesetzeskatalogZeileKiSicht.Einheit",
                                     KiDialogTexte.LprEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GszEinheitErl, leerErlaubt: true),
                    new KiDialogFeld("status", "GesetzeskatalogZeileKiSicht.Status",
                                     KiDialogTexte.GszStatusName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GszStatusErl, leerErlaubt: true),
                    new KiDialogFeld("quelle", "GesetzeskatalogZeileKiSicht.Quelle",
                                     KiDialogTexte.NudQuelleName, KiParameterTyp.Text,
                                     KiDialogTexte.GszQuelleErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_KostenAdmin  ->  KostenfaktorKatalogDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Der Kostenfaktoren-Katalog — zwei Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.KostenfaktorKatalogKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Das Raster bleibt draussen.</b> Es zeigt je Faktor nur seine
        /// Bezeichnung, und die ist der Anzeigetext der WAHL - ein zweites Feld
        /// daneben waere dieselbe Zeichenkette ein zweites Mal. Deklariert sind der
        /// Bezeichner der Neuzeile und die Markierung, auf die „Loeschen" greift.
        /// </remarks>
        private static KiDialog Kostenfaktorkatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOSTENFAKTOR_KATALOG,
                anzeigename: KiDialogTexte.MaskeKostenfaktorkatalog,
                felder: new[]
                {
                    new KiDialogFeld("kostenfaktor",
                                     "KostenfaktorKatalogKiSicht.Kostenfaktor",
                                     KiDialogTexte.KfkFaktorName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KfkFaktorErl, leerErlaubt: true),
                    new KiDialogFeld("neuer_name", "KostenfaktorKatalogKiSicht.NeuerName",
                                     KiDialogTexte.KfkNeuName, KiParameterTyp.Text,
                                     KiDialogTexte.KfkNeuErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk)
                });
        }

        // =====================================================================
        // Form_Emissionskatalog  ->  EmissionskatalogDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Emissionsarten und ihr Wertekatalog — zwoelf Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.EmissionskatalogKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Maske, drei Ebenen.</b> Oben die Bilanzierungsmethode, darunter
        /// zwei Raster (Arten und ihre Werte) und ueber ihnen je ein EDITOR als
        /// Ueberlagerung. Die Editoren gehen IN dieser Maske auf und tragen deshalb
        /// keinen eigenen Katalogschluessel - dieselbe Lage wie die zwei
        /// Reiterblaetter des Gebaeudekatalogs.
        /// </para>
        /// <para>
        /// <b>Was die Raster ZEIGEN, bleibt draussen</b>: Kuerzel, Name, Einheit, GWP
        /// und Herkunft stehen dort als Anzeige. Deklariert sind die MARKIERUNGEN -
        /// sie entscheiden, worauf „Aendern", „Loeschen" und „Uebernehmen" greifen -
        /// und die lebenden Felder der beiden Editoren.
        /// </para>
        /// </remarks>
        private static KiDialog Emissionskatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.EMISSIONSKATALOG,
                anzeigename: KiDialogTexte.MaskeEmissionskatalog,
                felder: new[]
                {
                    new KiDialogFeld("als_co2e", "EmissionskatalogKiSicht.AlsCo2e",
                                     KiDialogTexte.EmkModusName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EmkModusErl),
                    new KiDialogFeld("emissionsart", "EmissionskatalogKiSicht.Emissionsart",
                                     KiDialogTexte.EmkArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EmkArtErl, leerErlaubt: true),
                    new KiDialogFeld("emissionswert", "EmissionskatalogKiSicht.Emissionswert",
                                     KiDialogTexte.EmkWertName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EmkWertErl, leerErlaubt: true),

                    // ---- Arteneditor ------------------------------------------------
                    new KiDialogFeld("art_kuerzel", "EmissionskatalogKiSicht.ArtKuerzel",
                                     KiDialogTexte.EmkKuerzelName, KiParameterTyp.Text,
                                     KiDialogTexte.EmkKuerzelErl, leerErlaubt: true),
                    new KiDialogFeld("art_name", "EmissionskatalogKiSicht.ArtName",
                                     KiDialogTexte.EmkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.EmkNameErl, leerErlaubt: true),
                    new KiDialogFeld("art_einheit", "EmissionskatalogKiSicht.ArtEinheit",
                                     KiDialogTexte.EmkEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EmkEinheitErl, leerErlaubt: true),
                    new KiDialogFeld("art_gwp", "EmissionskatalogKiSicht.ArtGwp",
                                     KiDialogTexte.EmkGwpName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EmkGwpErl, leerErlaubt: true),
                    new KiDialogFeld("art_quelle", "EmissionskatalogKiSicht.ArtQuelle",
                                     KiDialogTexte.EmkArtQuelleName, KiParameterTyp.Text,
                                     KiDialogTexte.EmkArtQuelleErl, leerErlaubt: true),

                    // ---- Werteeditor ------------------------------------------------
                    new KiDialogFeld("wert_quelle", "EmissionskatalogKiSicht.WertQuelle",
                                     KiDialogTexte.EmkWertQuelleName, KiParameterTyp.Text,
                                     KiDialogTexte.EmkWertQuelleErl, leerErlaubt: true),
                    new KiDialogFeld("wert", "EmissionskatalogKiSicht.Wert",
                                     KiDialogTexte.EmkWertZahlName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EmkWertZahlErl, leerErlaubt: true),
                    new KiDialogFeld("wert_ist_co2e", "EmissionskatalogKiSicht.WertIstCo2e",
                                     KiDialogTexte.EmkWertCo2eName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EmkWertCo2eErl),
                    new KiDialogFeld("wert_als_vorlage",
                                     "EmissionskatalogKiSicht.WertAlsVorlage",
                                     KiDialogTexte.EmkWertVorlageName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EmkWertVorlageErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Nutzungsdauer  ->  NutzungsdauerDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die Nutzungsdauern (AfA) — sieben Kopffelder und fuenf SPALTEN aus
        /// <c>EPOS.UI.Dialoge.Kosten.NutzungsdauerKiSicht</c> (Etappe E10: je Neuzeile und
        /// Tabellenzeile dazu die Saetze Instandsetzung und Wartung).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Tabelle wird an Ort und Stelle bearbeitet</b> und kommt deshalb als
        /// Spalten in den Katalog - dieselbe Bauart wie das Positionsraster der
        /// Kostenverwaltung. Aus jeder Spaltendeklaration wird je vorhandener Zeile
        /// ein gewoehnliches Feld; den Klartextnamen einer Zeile liefert ihre
        /// POSITIONSART.
        /// </para>
        /// <para>
        /// <b>Technik und Auslieferungsmarke sind ANZEIGE.</b> Die Technik ordnet die
        /// Gruppen und wird beim Anlegen einmal gesetzt; die Marke sagt, dass ein
        /// Satz zur Auslieferung gehoert und deshalb nur weich zu loeschen ist.
        /// Beide stehen nicht als Spalte, weil sie sich in der Tabelle nicht aendern
        /// lassen.
        /// </para>
        /// </remarks>
        private static KiDialog Nutzungsdauer()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.NUTZUNGSDAUER,
                anzeigename: KiDialogTexte.MaskeNutzungsdauer,
                felder: new[]
                {
                    new KiDialogFeld("suche", "NutzungsdauerKiSicht.Suche",
                                     KiDialogTexte.NudSucheName, KiParameterTyp.Text,
                                     KiDialogTexte.NudSucheErl, leerErlaubt: true),
                    new KiDialogFeld("neue_technik", "NutzungsdauerKiSicht.NeueTechnik",
                                     KiDialogTexte.NudTechnikName, KiParameterTyp.Wahl,
                                     KiDialogTexte.NudTechnikErl, leerErlaubt: true),
                    new KiDialogFeld("neue_positionsart",
                                     "NutzungsdauerKiSicht.NeuePositionsart",
                                     KiDialogTexte.NudArtName, KiParameterTyp.Text,
                                     KiDialogTexte.NudArtErl, leerErlaubt: true),
                    new KiDialogFeld("neue_nutzungsdauer",
                                     "NutzungsdauerKiSicht.NeueNutzungsdauer",
                                     KiDialogTexte.NudNeuWertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudNeuWertErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true),
                    new KiDialogFeld("neue_afa", "NutzungsdauerKiSicht.NeueAfa",
                                     KiDialogTexte.NudNeuAfaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudNeuAfaErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true),
                    // ETAPPE E10 (Stufe S3): die zwei Saetze der Neuzeile - Instandsetzung
                    // und Wartung in % der Investition je Jahr (VDI 2067 Blatt 1, Tab. A2).
                    new KiDialogFeld("neue_instandsetzung",
                                     "NutzungsdauerKiSicht.NeueInstandsetzung",
                                     KiDialogTexte.NudNeuInstandsetzungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudNeuInstandsetzungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true,
                                     min: 0, max: 100),
                    new KiDialogFeld("neue_wartung", "NutzungsdauerKiSicht.NeueWartung",
                                     KiDialogTexte.NudNeuWartungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudNeuWartungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true,
                                     min: 0, max: 100),

                    // ---- Die Tabelle: je Satz eine Zeile ----------------------------
                    new KiDialogFeld("nutzungsdauer",
                                     "NutzungsdauerKiSicht.Zeilen[].Nutzungsdauer",
                                     KiDialogTexte.NudWertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudWertErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN_NUTZUNGSDAUER),
                    new KiDialogFeld("afa_steuerlich",
                                     "NutzungsdauerKiSicht.Zeilen[].AfaSteuerlich",
                                     KiDialogTexte.NudAfaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudAfaErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN_NUTZUNGSDAUER),
                    new KiDialogFeld("instandsetzung",
                                     "NutzungsdauerKiSicht.Zeilen[].InstandsetzungProzent",
                                     KiDialogTexte.NudInstandsetzungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudInstandsetzungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN_NUTZUNGSDAUER,
                                     min: 0, max: 100),
                    new KiDialogFeld("wartung",
                                     "NutzungsdauerKiSicht.Zeilen[].WartungProzent",
                                     KiDialogTexte.NudWartungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.NudWartungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN_NUTZUNGSDAUER,
                                     min: 0, max: 100),
                    new KiDialogFeld("quelle", "NutzungsdauerKiSicht.Zeilen[].Quelle",
                                     KiDialogTexte.NudQuelleName, KiParameterTyp.Text,
                                     KiDialogTexte.NudQuelleErl, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN_NUTZUNGSDAUER)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        /// <summary>
        /// Die Eigenschaft, aus der eine Zeile der Nutzungsdauertabelle ihren
        /// Klartextnamen bekommt — „Nutzungsdauer (Waermeerzeuger)" statt
        /// „Nutzungsdauer 3".
        /// </summary>
        private const string ZEILENKENNZEICHEN_NUTZUNGSDAUER = "Positionsart";

        // =====================================================================
        // Form_VorlagenPosition  ->  VorlagenPositionDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Der Zeileneditor einer Kostenposition — sechs Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.VorlagenPositionKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Zwei WAHLFELDER mit verschiedenen Schluesseln</b> (KI-D-Q6): Die
        /// KOSTENART traegt den Listenplatz der VDI-2067-Liste, die POSITIONSART die
        /// Id des Nutzungsdauersatzes. Beide setzt der Assistent ueber ihren
        /// Anzeigetext, nie ueber eine rohe Zahl.
        /// </remarks>
        private static KiDialog Vorlagenposition()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.VORLAGENPOSITION,
                anzeigename: KiDialogTexte.MaskeVorlagenposition,
                felder: new[]
                {
                    new KiDialogFeld("bezeichnung", "VorlagenPositionKiSicht.Bezeichnung",
                                     KiDialogTexte.VopBezeichnungName, KiParameterTyp.Text,
                                     KiDialogTexte.VopBezeichnungErl, leerErlaubt: true),
                    new KiDialogFeld("kostenart", "VorlagenPositionKiSicht.Kostenart",
                                     KiDialogTexte.VopKostenartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.VopKostenartErl, leerErlaubt: true),
                    new KiDialogFeld("ist_erloes", "VorlagenPositionKiSicht.IstErloes",
                                     KiDialogTexte.VopErloesName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.VopErloesErl),
                    new KiDialogFeld("positionsart", "VorlagenPositionKiSicht.Positionsart",
                                     KiDialogTexte.VopPositionsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.VopPositionsartErl, leerErlaubt: true),
                    new KiDialogFeld("empfehlung_von", "VorlagenPositionKiSicht.EmpfehlungVon",
                                     KiDialogTexte.VopVonName, KiParameterTyp.Zahl,
                                     KiDialogTexte.VopVonErl, leerErlaubt: true),
                    new KiDialogFeld("empfehlung_bis", "VorlagenPositionKiSicht.EmpfehlungBis",
                                     KiDialogTexte.VopBisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.VopBisErl, leerErlaubt: true),
                    // ETAPPE E7c (Schritt E, Entscheid A6): Ersatz und Restwert je
                    // Position — dreiwertig, leer = wie bisher.
                    new KiDialogFeld("ersatz_fuehren", "VorlagenPositionKiSicht.ErsatzFuehren",
                                     KiDialogTexte.VopErsatzName, KiParameterTyp.Wahl,
                                     KiDialogTexte.VopErsatzErl, leerErlaubt: true),
                    new KiDialogFeld("restwert_ansetzen", "VorlagenPositionKiSicht.RestwertAnsetzen",
                                     KiDialogTexte.VopRestwertName, KiParameterTyp.Wahl,
                                     KiDialogTexte.VopRestwertErl, leerErlaubt: true),
                    // ETAPPE E16 (V‑G3, DIN EN 17463 6.3.1): die Wiederholperiode einer
                    // Betriebsposition — 1 = jaehrlich, n = alle n Jahre ab dem Startjahr.
                    new KiDialogFeld("wiederholperiode", "VorlagenPositionKiSicht.Wiederholperiode",
                                     KiDialogTexte.VopWiederholperiodeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.VopWiederholperiodeErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_CaseEingabe  ->  CaseEingabeDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Worst- und Best-Case einer Kostenposition — seit ETAPPE E9b zehn Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.CaseEingabeKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der PROZENTMODUS fuehrt die Maske.</b> Er entscheidet, ob die zwei
        /// Kostenfelder einen Betrag oder eine Abweichung vom Erwartungswert tragen -
        /// und damit ihre Einheit und ihre Grenzen. Ohne gepflegten Erwartungswert ist
        /// er gesperrt; dann gibt es nichts, wovon abzuweichen waere. Geschrieben wird
        /// beim OK IMMER in Euro, auch wenn der Anwender Prozente getippt hat.
        /// </para>
        /// <para>
        /// <b>ETAPPE E9b: der allgemeine Baustein.</b> Dieselbe Maske pflegt das
        /// Best/Worst-Paar eines Traegerpreises oder Erloessatzes (Szenariopaar). Drei
        /// NUR LESBARE Felder sagen, was gepflegt wird, gegen welchen Erwartet-Wert und
        /// in welcher Einheit; Nutzungsdauer, Startjahr und Zuschuss gibt es dann nicht.
        /// </para>
        /// </remarks>
        private static KiDialog CaseEingabe()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.CASE_EINGABE,
                anzeigename: KiDialogTexte.MaskeCaseEingabe,
                felder: new[]
                {
                    new KiDialogFeld("prozentmodus", "CaseEingabeKiSicht.Prozentmodus",
                                     KiDialogTexte.CseModusName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.CseModusErl),
                    new KiDialogFeld("best_case", "CaseEingabeKiSicht.BestCase",
                                     KiDialogTexte.CseBestName, KiParameterTyp.Zahl,
                                     KiDialogTexte.CseBestErl, leerErlaubt: true),
                    new KiDialogFeld("worst_case", "CaseEingabeKiSicht.WorstCase",
                                     KiDialogTexte.CseWorstName, KiParameterTyp.Zahl,
                                     KiDialogTexte.CseWorstErl, leerErlaubt: true),
                    new KiDialogFeld("best_nutzungsdauer",
                                     "CaseEingabeKiSicht.BestNutzungsdauer",
                                     KiDialogTexte.CseBestDauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.CseBestDauerErl,
                                     einheit: KiDialogTexte.EinheitJahre),
                    new KiDialogFeld("worst_nutzungsdauer",
                                     "CaseEingabeKiSicht.WorstNutzungsdauer",
                                     KiDialogTexte.CseWorstDauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.CseWorstDauerErl,
                                     einheit: KiDialogTexte.EinheitJahre),
                    new KiDialogFeld("startjahr", "CaseEingabeKiSicht.Startjahr",
                                     KiDialogTexte.CseJahrName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.CseJahrErl),
                    new KiDialogFeld("ist_zuschuss", "CaseEingabeKiSicht.IstZuschuss",
                                     KiDialogTexte.CseZuschussName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.CseZuschussErl),

                    // ---- ETAPPE E9b: die Auskunft ueber das gepflegte Paar -----------
                    // Nur lesbar: Was die Maske pflegt (Kosten, Traegerpreis oder
                    // Erloessatz), gegen welchen Erwartet-Wert und in welcher Einheit.
                    new KiDialogFeld("groesse", "CaseEingabeKiSicht.Groesse",
                                     KiDialogTexte.CseGroesseName, KiParameterTyp.Text,
                                     KiDialogTexte.CseGroesseErl, nurLesen: true),
                    new KiDialogFeld("erwartet", "CaseEingabeKiSicht.Erwartet",
                                     KiDialogTexte.CseErwartetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.CseErwartetErl, leerErlaubt: true,
                                     nurLesen: true),
                    new KiDialogFeld("einheit", "CaseEingabeKiSicht.Einheit",
                                     KiDialogTexte.CseEinheitName, KiParameterTyp.Text,
                                     KiDialogTexte.CseEinheitErl, nurLesen: true)
                    // ---- Ende ETAPPE E9b ----------------------------------------------
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Energietraeger  ->  EnergietraegerDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die Energietraegerverwaltung — 45 Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.EnergietraegerKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Bloecke in einem Feldsatz.</b> Oben der Listenkopf (Filter,
        /// gewaehlter Traeger, die zwei Stammfelder der Katalogverwaltung), darunter
        /// die Traegerkarte mit ihren Preisen, Einheiten und Emissionen, ganz unten
        /// der Preisblock, der zur Familie des Traegers gehoert: die ZERLEGUNG des
        /// Strompreises oder die BESTANDTEILE des Brennstoffpreises. Beide Bloecke
        /// sind Bausteine IN der Karte und keine eigenen Masken - siehe
        /// <see cref="KiMaskennamen.ENERGIETRAEGER"/>.
        /// </para>
        /// <para>
        /// <b>Jeder Bestandteil traegt Wert UND Schalter.</b> Der Schalter
        /// unterscheidet „gepflegt" von „kein Anteil"; ein Betrag hinter einem
        /// ausgeschalteten Schalter wirkt nicht. Beides steht auf der Maske
        /// nebeneinander, also auch im Katalog.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die drei RASTER</b> — die Emissionszeilen mit ihrem
        /// Katalogknopf je Zeile, die Umrechnungsregeln und die Preishistorie —,
        /// die Wertluecken mit ihren Leihknoepfen, die Schnellwahlsaetze und die
        /// vier Unterdialoge (Kostenprofil, Spotpreisimport, saisonale Saetze,
        /// Emissionskatalog). Drei davon tragen einen eigenen Katalogschluessel.
        /// </para>
        /// </remarks>
        private static KiDialog Energietraeger()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.ENERGIETRAEGER,
                anzeigename: KiDialogTexte.MaskeEnergietraeger,
                felder: new[]
                {
                    // ---- Listenkopf und Stammfelder ---------------------------------
                    new KiDialogFeld("suche", "EnergietraegerKiSicht.Suche",
                                     KiDialogTexte.EtSucheName, KiParameterTyp.Text,
                                     KiDialogTexte.EtSucheErl, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "EnergietraegerKiSicht.Energietraeger",
                                     KiDialogTexte.EtTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EtTraegerErl, leerErlaubt: true),
                    new KiDialogFeld("stammname", "EnergietraegerKiSicht.Stammname",
                                     KiDialogTexte.EtStammnameName, KiParameterTyp.Text,
                                     KiDialogTexte.EtStammnameErl, leerErlaubt: true),
                    new KiDialogFeld("stammgruppe", "EnergietraegerKiSicht.Stammgruppe",
                                     KiDialogTexte.EtStammgruppeName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EtStammgruppeErl, leerErlaubt: true),

                    // ---- Die Traegerkarte -------------------------------------------
                    new KiDialogFeld("arbeitspreis", "EnergietraegerKiSicht.Arbeitspreis",
                                     KiDialogTexte.EtArbeitspreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtArbeitspreisErl),
                    new KiDialogFeld("grundpreis", "EnergietraegerKiSicht.Grundpreis",
                                     KiDialogTexte.EtGrundpreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtGrundpreisErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_A),
                    new KiDialogFeld("leistungspreis", "EnergietraegerKiSicht.Leistungspreis",
                                     KiDialogTexte.EtLeistungspreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtLeistungspreisErl),
                    new KiDialogFeld("leistungspreis_monatlich",
                                     "EnergietraegerKiSicht.LeistungspreisMonatlich",
                                     KiDialogTexte.EtLpModusName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtLpModusErl),

                    // ---- Q11: die zweistufige Leistungspreis-Staffel des
                    //      Stromträgers (nur Stromträger im Projektkontext) -----------
                    new KiDialogFeld("leistungspreis_staffelgrenze",
                                     "EnergietraegerKiSicht.StaffelGrenze",
                                     KiDialogTexte.EtStaffelGrenzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStaffelGrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("leistungspreis_staffel_unten",
                                     "EnergietraegerKiSicht.StaffelPreis1",
                                     KiDialogTexte.EtStaffelPreis1Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStaffelPreis1Erl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A, leerErlaubt: true),
                    new KiDialogFeld("leistungspreis_staffel_oben",
                                     "EnergietraegerKiSicht.StaffelPreis2",
                                     KiDialogTexte.EtStaffelPreis2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStaffelPreis2Erl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A, leerErlaubt: true),
                    new KiDialogFeld("heizwert", "EnergietraegerKiSicht.Heizwert",
                                     KiDialogTexte.EtHeizwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtHeizwertErl),
                    new KiDialogFeld("brennwert", "EnergietraegerKiSicht.Brennwert",
                                     KiDialogTexte.EtBrennwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtBrennwertErl),
                    new KiDialogFeld("preisbasis", "EnergietraegerKiSicht.Preisbasis",
                                     KiDialogTexte.EtPreisbasisName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EtPreisbasisErl, leerErlaubt: true),
                    new KiDialogFeld("basiseinheit", "EnergietraegerKiSicht.Basiseinheit",
                                     KiDialogTexte.EtBasiseinheitName, KiParameterTyp.Text,
                                     KiDialogTexte.EtBasiseinheitErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("effektivpreis", "EnergietraegerKiSicht.Effektivpreis",
                                     KiDialogTexte.EtEffektivName, KiParameterTyp.Text,
                                     KiDialogTexte.EtEffektivErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gueltig_ab", "EnergietraegerKiSicht.GueltigAb",
                                     KiDialogTexte.EtGueltigAbName, KiParameterTyp.Text,
                                     KiDialogTexte.EtGueltigAbErl, leerErlaubt: true),

                    // ---- Emissionen der Karte ---------------------------------------
                    new KiDialogFeld("emissionen_als_co2e",
                                     "EnergietraegerKiSicht.EmissionenAlsCo2e",
                                     KiDialogTexte.EtModusName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtModusErl),
                    new KiDialogFeld("co2", "EnergietraegerKiSicht.Co2",
                                     KiDialogTexte.EtCo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtCo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_G_KWH),
                    new KiDialogFeld("so2", "EnergietraegerKiSicht.So2",
                                     KiDialogTexte.EtSo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtSo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_G_KWH),
                    new KiDialogFeld("nox", "EnergietraegerKiSicht.Nox",
                                     KiDialogTexte.EtNoxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtNoxErl,
                                     einheit: KiDialogTexte.EINHEIT_G_KWH),

                    // ---- Baustein „Strompreis Details" ------------------------------
                    new KiDialogFeld("strom_beschaffung",
                                     "EnergietraegerKiSicht.StromBeschaffung",
                                     KiDialogTexte.EtStromBeschaffungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromBeschaffungErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_beschaffung_aktiv",
                                     "EnergietraegerKiSicht.StromBeschaffungAktiv",
                                     KiDialogTexte.EtStromBeschaffungAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_vertrieb", "EnergietraegerKiSicht.StromVertrieb",
                                     KiDialogTexte.EtStromVertriebName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromVertriebErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_vertrieb_aktiv",
                                     "EnergietraegerKiSicht.StromVertriebAktiv",
                                     KiDialogTexte.EtStromVertriebAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_netzentgelt",
                                     "EnergietraegerKiSicht.StromNetzentgelt",
                                     KiDialogTexte.EtStromNetzName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromNetzErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_netzentgelt_aktiv",
                                     "EnergietraegerKiSicht.StromNetzentgeltAktiv",
                                     KiDialogTexte.EtStromNetzAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("stromsteuer", "EnergietraegerKiSicht.Stromsteuer",
                                     KiDialogTexte.EtStromsteuerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromsteuerErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("stromsteuer_aktiv",
                                     "EnergietraegerKiSicht.StromsteuerAktiv",
                                     KiDialogTexte.EtStromsteuerAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_konzession",
                                     "EnergietraegerKiSicht.StromKonzession",
                                     KiDialogTexte.EtStromKonzessionName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromKonzessionErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_konzession_aktiv",
                                     "EnergietraegerKiSicht.StromKonzessionAktiv",
                                     KiDialogTexte.EtStromKonzessionAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_umlagen", "EnergietraegerKiSicht.StromUmlagen",
                                     KiDialogTexte.EtStromUmlagenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromUmlagenErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_umlagen_aktiv",
                                     "EnergietraegerKiSicht.StromUmlagenAktiv",
                                     KiDialogTexte.EtStromUmlagenAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_umlagen_einzeln",
                                     "EnergietraegerKiSicht.StromUmlagenEinzeln",
                                     KiDialogTexte.EtStromEinzelnName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtStromEinzelnErl),
                    new KiDialogFeld("strom_umlage_kwkg",
                                     "EnergietraegerKiSicht.StromUmlageKwkg",
                                     KiDialogTexte.EtStromKwkgName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromKwkgErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_umlage_kwkg_aktiv",
                                     "EnergietraegerKiSicht.StromUmlageKwkgAktiv",
                                     KiDialogTexte.EtStromKwkgAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_umlage_offshore",
                                     "EnergietraegerKiSicht.StromUmlageOffshore",
                                     KiDialogTexte.EtStromOffshoreName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromOffshoreErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_umlage_offshore_aktiv",
                                     "EnergietraegerKiSicht.StromUmlageOffshoreAktiv",
                                     KiDialogTexte.EtStromOffshoreAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("strom_umlage_stromnev",
                                     "EnergietraegerKiSicht.StromUmlageStromNev",
                                     KiDialogTexte.EtStromNevName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtStromNevErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("strom_umlage_stromnev_aktiv",
                                     "EnergietraegerKiSicht.StromUmlageStromNevAktiv",
                                     KiDialogTexte.EtStromNevAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),

                    // ---- Baustein „Preisbestandteile" -------------------------------
                    new KiDialogFeld("brennstoff_energiesteuer",
                                     "EnergietraegerKiSicht.BrennstoffEnergiesteuer",
                                     KiDialogTexte.EtBsEnergiesteuerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtBsEnergiesteuerErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("brennstoff_energiesteuer_aktiv",
                                     "EnergietraegerKiSicht.BrennstoffEnergiesteuerAktiv",
                                     KiDialogTexte.EtBsEnergiesteuerAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("brennstoff_co2", "EnergietraegerKiSicht.BrennstoffCo2",
                                     KiDialogTexte.EtBsCo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtBsCo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("brennstoff_co2_aktiv",
                                     "EnergietraegerKiSicht.BrennstoffCo2Aktiv",
                                     KiDialogTexte.EtBsCo2AktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("brennstoff_netzentgelt",
                                     "EnergietraegerKiSicht.BrennstoffNetzentgelt",
                                     KiDialogTexte.EtBsNetzName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtBsNetzErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("brennstoff_netzentgelt_aktiv",
                                     "EnergietraegerKiSicht.BrennstoffNetzentgeltAktiv",
                                     KiDialogTexte.EtBsNetzAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl),
                    new KiDialogFeld("brennstoff_vertrieb",
                                     "EnergietraegerKiSicht.BrennstoffVertrieb",
                                     KiDialogTexte.EtBsVertriebName, KiParameterTyp.Zahl,
                                     KiDialogTexte.EtBsVertriebErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("brennstoff_vertrieb_aktiv",
                                     "EnergietraegerKiSicht.BrennstoffVertriebAktiv",
                                     KiDialogTexte.EtBsVertriebAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.EtAnteilAktivErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Kosten_Auswahl  ->  EnergietraegerVarianteDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// „Energietraeger-Variante anlegen" — zwei Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.EnergietraegerVarianteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Der Traeger ist ein WAHLFELD</b> (KI-D-Q6) und traegt als Schluessel
        /// seine Katalog-Id; ihn zu setzen belegt den Variantennamen vor - derselbe
        /// Weg wie ein Griff in die Klappliste. Der Dialog legt beim OK einen NEUEN
        /// Katalogsatz an; das ist ein datenbankwirksamer Weg und laeuft deshalb
        /// ueber <c>dialog_speichern</c>, nicht als Formularaktion.
        /// </remarks>
        private static KiDialog EnergietraegerVariante()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.ENERGIETRAEGER_VARIANTE,
                anzeigename: KiDialogTexte.MaskeEnergietraegerVariante,
                felder: new[]
                {
                    new KiDialogFeld("energietraeger",
                                     "EnergietraegerVarianteKiSicht.Energietraeger",
                                     KiDialogTexte.EtvTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.EtvTraegerErl, leerErlaubt: true),
                    new KiDialogFeld("variantenname",
                                     "EnergietraegerVarianteKiSicht.Variantenname",
                                     KiDialogTexte.EtvNameName, KiParameterTyp.Text,
                                     KiDialogTexte.EtvNameErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_LeistungspreisReihe  ->  LeistungspreisReiheDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Die saisonalen Leistungspreis-Saetze — vier Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.LeistungspreisReiheKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Die zwoelf Monatssaetze sind EINE ZAHLENREIHE</b> (Welle #458 Stufe 3b):
        /// ein Feld <c>monatssaetze</c> mit zwoelf Werten, Januar bis Dezember, gesetzt
        /// mit <c>reihe_setzen</c> ganz oder ab einem Monat. Ihre Grenzen sind die der
        /// zwoelf Eingabefelder (0 bis 100 000). Das JAHR ist der Einstellwert, der die
        /// ganze Reihe traegt.
        /// </remarks>
        private static KiDialog Leistungspreisreihe()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.LEISTUNGSPREISREIHE,
                anzeigename: KiDialogTexte.MaskeLeistungspreisreihe,
                felder: new[]
                {
                    new KiDialogFeld("jahr", "LeistungspreisReiheKiSicht.Jahr",
                                     KiDialogTexte.LprJahrName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.LprJahrErl, leerErlaubt: true),
                    new KiDialogFeld("einheit", "LeistungspreisReiheKiSicht.Einheit",
                                     KiDialogTexte.LprEinheitName, KiParameterTyp.Text,
                                     KiDialogTexte.LprEinheitErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("kontext", "LeistungspreisReiheKiSicht.Kontext",
                                     KiDialogTexte.LprKontextName, KiParameterTyp.Text,
                                     KiDialogTexte.LprKontextErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("monatssaetze", "LeistungspreisReiheKiSicht.Monatssaetze",
                                     KiDialogTexte.LprMonatssaetzeName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.LprMonatssaetzeErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_MONAT,
                                     reihe: KiZahlenreihen.Monate(), min: 0, max: 100000)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Kostenprofil  ->  KostenprofilDialog   (Welle KI-F4)
        // =====================================================================

        /// <summary>
        /// Das Kostenprofil eines Stromtraegers — fuenf Felder aus
        /// <c>EPOS.UI.Dialoge.Kosten.KostenprofilKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die zwei Wertetafeln sind ZAHLENREIHEN</b> (Welle #458 Stufe 3b): die
        /// zwoelf Monatsniveaus (<c>monatswerte</c>) und die 7 × 24 Abweichungen je
        /// Wochentag und Stunde (<c>wochenwerte</c>, Montag Stunde 1 bis Sonntag Stunde
        /// 24). Einzeln waeren es 180 Felder; als Reihe ist jede EIN Feld, gesetzt mit
        /// <c>reihe_setzen</c> ganz oder ab einer Stelle - „Dienstag" ist die Stelle 25.
        /// Die Griffe der Maske („Jan.-Wert in alle Monate", „Tag kopieren", „Für alle
        /// Tage") sind damit ein Aufruf mit der passenden Liste.
        /// </para>
        /// <para>
        /// Einstellwerte daneben sind der BEZEICHNER und der Wochentag, dessen
        /// Stundenkurve dasteht.
        /// </para>
        /// </remarks>
        private static KiDialog Kostenprofil()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOSTENPROFIL,
                anzeigename: KiDialogTexte.MaskeKostenprofil,
                felder: new[]
                {
                    new KiDialogFeld("bezeichner", "KostenprofilKiSicht.Bezeichner",
                                     KiDialogTexte.KprBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.KprBezeichnerErl, leerErlaubt: true),
                    new KiDialogFeld("wochentag", "KostenprofilKiSicht.Wochentag",
                                     KiDialogTexte.KprWochentagName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KprWochentagErl, leerErlaubt: true),
                    new KiDialogFeld("einheit", "KostenprofilKiSicht.Einheit",
                                     KiDialogTexte.KprEinheitName, KiParameterTyp.Text,
                                     KiDialogTexte.KprEinheitErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("monatswerte", "KostenprofilKiSicht.Monatswerte",
                                     KiDialogTexte.KprMonatswerteName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.KprMonatswerteErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH,
                                     reihe: KiZahlenreihen.Monate()),
                    new KiDialogFeld("wochenwerte", "KostenprofilKiSicht.Wochenwerte",
                                     KiDialogTexte.KprWochenwerteName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.KprWochenwerteErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH,
                                     reihe: KiZahlenreihen.Wochenstunden())
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Waermebedarf  ->  WaermebedarfExternDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die externen Waermebedarfsganglinien eines Projekts — zwei Felder der
        /// GEWAEHLTEN Zuordnung (<c>EPOS.UI.Dialoge.Bedarf.WaermebedarfExternZeile</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist eine ZEILE und kein Dialogstand</b> — dieselbe
        /// Bauart wie bei den Erzeugermasken des Projekts: Der Dialog fuehrt eine
        /// Liste, und angemeldet ist, was in der gewaehlten Zeile steht. Ist keine
        /// gewaehlt, sind die Felder leer — derselbe Zustand, den der Anwender sieht.
        /// </para>
        /// <para>
        /// <b>Der KANAL ist ein WAHLFELD</b> (KI-D-Q6) und traegt als Schluessel seinen
        /// Steuerwert (Heizung, Brauchwasser, Prozesswaerme). Er gilt JE ZUORDNUNG:
        /// Dieselbe Ganglinie darf einem Projekt mehrfach zugeordnet sein und dabei
        /// einmal in den Heizbedarf und einmal in den Brauchwasserbedarf laufen.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben der Katalog mit seinem Filterbaustein, der CSV-Import
        /// samt Vorschau und Ablage</b> (ein Ladevorgang) und die Ganglinienvorschau.
        /// </para>
        /// </remarks>
        private static KiDialog WaermebedarfExtern()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMEBEDARF_EXTERN,
                anzeigename: KiDialogTexte.MaskeWaermebedarfExtern,
                felder: new[]
                {
                    new KiDialogFeld("kanal", "WaermebedarfExternZeile.Kanal",
                                     KiDialogTexte.WbxKanalName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WbxKanalErl, leerErlaubt: true),
                    new KiDialogFeld("ganglinie", "WaermebedarfExternZeile.Bezeichner",
                                     KiDialogTexte.WbxGanglinieName, KiParameterTyp.Text,
                                     KiDialogTexte.WbxGanglinieErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Solarganglinie  ->  SolarganglinieDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Solarganglinien eines Projekts — drei Felder aus
        /// <c>EPOS.UI.Dialoge.Solarthermie.SolarganglinieKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Diese Maske fuehrt keinen Einstellwert der Anlage.</b> Sie ordnet
        /// Ganglinien zu und zeigt zu der markierten Zeile Name und Beschreibung. Ihr
        /// Wahlfeld ist die KATALOGWAHL — sie markiert die Ganglinie, die „In das
        /// Projekt uebernehmen" aufnimmt.
        /// </para>
        /// <para>
        /// Freigegeben ist sie trotzdem, damit <c>dialog_lesen</c> nennt, welche
        /// Ganglinie gewaehlt ist, und <c>feld_setzen</c> benannt ablehnt statt „Maske
        /// nicht freigegeben" — dieselbe Begruendung wie bei den Speichermasken des
        /// Projekts.
        /// </para>
        /// </remarks>
        private static KiDialog Solarganglinie()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SOLARGANGLINIE,
                anzeigename: KiDialogTexte.MaskeSolarganglinie,
                felder: new[]
                {
                    new KiDialogFeld("katalogganglinie",
                                     "SolarganglinieKiSicht.Katalogganglinie",
                                     KiDialogTexte.SglKatalogName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SglKatalogErl, leerErlaubt: true),
                    new KiDialogFeld("projektganglinie",
                                     "SolarganglinieKiSicht.Projektganglinie",
                                     KiDialogTexte.SglProjektName, KiParameterTyp.Text,
                                     KiDialogTexte.SglProjektErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beschreibung", "SolarganglinieKiSicht.Beschreibung",
                                     KiDialogTexte.SglBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.SglBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Klimadaten  ->  KlimadatenDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Klimadatenverwaltung — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Klimadaten.KlimadatenKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die QUELLE fuehrt die Maske.</b> Sie entscheidet, ob der Standort aus dem
        /// PVGIS-Dienst kommt, aus einer einzelnen TRY-Datei oder aus einem TRY-Paket —
        /// und damit, welche Felder darunter ueberhaupt dastehen. Drei der neun sind
        /// deshalb Wahlfelder (Quelle, Jahr, Szenario).
        /// </para>
        /// <para>
        /// <b>Die beiden DATEIPFADE sind nur lesbar</b> (KI-D-Q6, Grenzfall): Getippt
        /// wird ein Pfad nicht, er wird ueber den Dateidialog der Plattform gewaehlt;
        /// das Feld der Maske ist gesperrt. Der Assistent liest ihn und sagt, welche
        /// Datei ansteht.
        /// </para>
        /// <para>
        /// <b>„Daten einlesen" ist ein LADEVORGANG und kein Speicherweg</b>: Er holt
        /// eine Zeitreihe aus dem Netz oder aus einer Datei, dauert Minuten und laesst
        /// sich abbrechen — ein rechnender Weg der Stufe 2, der ins Aktionsregister
        /// gehoert. Ebenso draussen: die Regionsliste mit ihrem Filterbaustein, die
        /// zwei Diagramme und der Loeschweg.
        /// </para>
        /// </remarks>
        private static KiDialog Klimadaten()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KLIMADATEN,
                anzeigename: KiDialogTexte.MaskeKlimadaten,
                felder: new[]
                {
                    new KiDialogFeld("quelle", "KlimadatenKiSicht.Quelle",
                                     KiDialogTexte.KlimaQuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KlimaQuelleErl),
                    new KiDialogFeld("ortsname", "KlimadatenKiSicht.Ortsname",
                                     KiDialogTexte.KlimaOrtName, KiParameterTyp.Text,
                                     KiDialogTexte.KlimaOrtErl, leerErlaubt: true),
                    new KiDialogFeld("laengengrad", "KlimadatenKiSicht.Laengengrad",
                                     KiDialogTexte.KlimaLaengeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KlimaLaengeErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("breitengrad", "KlimadatenKiSicht.Breitengrad",
                                     KiDialogTexte.KlimaBreiteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KlimaBreiteErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("bezeichnung", "KlimadatenKiSicht.Bezeichnung",
                                     KiDialogTexte.KlimaBezeichnungName, KiParameterTyp.Text,
                                     KiDialogTexte.KlimaBezeichnungErl, leerErlaubt: true),
                    new KiDialogFeld("jahr", "KlimadatenKiSicht.Jahr",
                                     KiDialogTexte.KlimaJahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KlimaJahrErl, leerErlaubt: true),
                    new KiDialogFeld("szenario", "KlimadatenKiSicht.Szenario",
                                     KiDialogTexte.KlimaSzenarioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KlimaSzenarioErl),
                    new KiDialogFeld("try_datei", "KlimadatenKiSicht.TryDatei",
                                     KiDialogTexte.KlimaTryDateiName, KiParameterTyp.Text,
                                     KiDialogTexte.KlimaTryDateiErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("try_paket", "KlimadatenKiSicht.TryPaket",
                                     KiDialogTexte.KlimaTryPaketName, KiParameterTyp.Text,
                                     KiDialogTexte.KlimaTryPaketErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_Prozesswaerme  ->  BedarfsProfileDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Bedarfsprofile eines Projekts — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.BedarfsProfileKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Zwei Listen, ein Einstellwert.</b> Links die Zuordnungen des Projekts,
        /// rechts der Katalog; eingestellt wird der JAHRESVERBRAUCH der markierten
        /// Zuordnung, und der Knopf „Uebernehmen" schreibt ihn in die Zeile. Die
        /// ANZEIGEEINHEIT gilt dabei fuer Eingabe und Infoblock zugleich.
        /// </para>
        /// <para>
        /// <b>Die Zapfprofil-Weiche</b> (Welle #458, Stufe 3a): Beim Brauchwasser eines
        /// gespeicherten Projekts steht die Optionsgruppe „Rechenweg Brauchwasser"
        /// (Bestandsprofile oder Zapfprofil); gesetzt wird sie ueber den Weg des Klicks,
        /// „Zapfprofil" ohne Zone lehnt die Maske benannt ab. In den anderen
        /// Auspraegungen ist das Feld leer und das Setzen nennt den Grund. Geschrieben
        /// wird die Weiche mit dem OK dieser Maske, im selben Vorgang wie das Zapfprofil.
        /// </para>
        /// <para>
        /// <b>EINE Maske, DREI Auspraegungen</b> (Prozesswaerme, Stromverbraucher,
        /// Brauchwasser). Der Katalogschluessel ist der WinForms-Maskenname der
        /// Prozesswaermefassung; welche gerade offen ist, sagt das Feld
        /// <c>bedarfsart</c>.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die beiden LISTEN</b> — die Zuordnungen des Projekts und
        /// der Katalog mit seinem eigenen Filterbaustein — sowie die Knoepfe, die
        /// Katalogsaetze anlegen, aendern und loeschen.
        /// </para>
        /// </remarks>
        private static KiDialog Bedarfsprofile()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BEDARFSPROFILE,
                anzeigename: KiDialogTexte.MaskeBedarfsprofile,
                felder: new[]
                {
                    new KiDialogFeld("einheit", "BedarfsProfileKiSicht.Einheit",
                                     KiDialogTexte.BpfEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BpfEinheitErl),
                    new KiDialogFeld("neuer_wert", "BedarfsProfileKiSicht.NeuerWert",
                                     KiDialogTexte.BpfNeuerWertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BpfNeuerWertErl, leerErlaubt: true),
                    new KiDialogFeld("rechenweg", "BedarfsProfileKiSicht.Rechenweg",
                                     KiDialogTexte.BpfRechenwegName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BpfRechenwegErl),
                    new KiDialogFeld("profil", "BedarfsProfileKiSicht.Profil",
                                     KiDialogTexte.BpfProfilName, KiParameterTyp.Text,
                                     KiDialogTexte.BpfProfilErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("typ", "BedarfsProfileKiSicht.Typ",
                                     KiDialogTexte.BpfTypName, KiParameterTyp.Text,
                                     KiDialogTexte.BpfTypErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beschreibung", "BedarfsProfileKiSicht.Beschreibung",
                                     KiDialogTexte.BpfBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.BpfBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("jahresverbrauch", "BedarfsProfileKiSicht.Jahresverbrauch",
                                     KiDialogTexte.BpfJahresverbrauchName, KiParameterTyp.Text,
                                     KiDialogTexte.BpfJahresverbrauchErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("summe", "BedarfsProfileKiSicht.Summe",
                                     KiDialogTexte.BpfSummeName, KiParameterTyp.Text,
                                     KiDialogTexte.BpfSummeErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("bedarfsart", "BedarfsProfileKiSicht.Bedarfsart",
                                     KiDialogTexte.BedarfsartName, KiParameterTyp.Text,
                                     KiDialogTexte.BedarfsartErl, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Zapfprofil  ->  Dialoge.Bedarf.ZapfprofilDialog   (Welle #458, Stufe 3a)
        // =====================================================================

        /// <summary>
        /// Der Typname der Sichtklasse des Zapfprofils
        /// (<c>EPOS.UI.Dialoge.Bedarf.ZapfprofilKiSicht</c>).
        /// </summary>
        private const string ZAPFPROFIL_SICHT = "ZapfprofilKiSicht";

        /// <summary>
        /// Das Brauchwasser-Zapfprofil — die Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.ZapfprofilKiSicht</c>: fuenf SPALTEN der Zonenliste, vier der
        /// Wohnungstabelle der gewaehlten Zone und die Angaben der Stufen Erweitert und Experte.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung mit eigenem Arbeitsstand</b> — dieselbe Bauart wie
        /// <see cref="Kennlinien"/>: Der Dialog bearbeitet eine KOPIE der Zonen, „OK" gibt sie
        /// an die Bedarfsprofile zurueck, geschrieben wird mit deren OK. Angemeldet sind
        /// Auffrischen und Pruefen (dieselbe Pruefung wie am OK: Fehleingabe, Pflichtfelder
        /// je Zone), KEIN Speicherweg — <c>dialog_speichern</c> lehnt benannt ab.
        /// </para>
        /// <para>
        /// <b>Die Zonen sind SPALTEN</b> mit dem Zonennamen als Kennzeichen: Name,
        /// Nutzungsart (Wahl aus dem Katalog; eine gesperrte nennt ihren Grund),
        /// Bezugsgroesse, Bedarfsniveau und — nur lesbar — der Jahresbedarf der Vorschau.
        /// Gesetzt wird auf den Wegen der Eingabefelder: Die Vorschau rechnet entprellt neu,
        /// und eine Aenderung, die die Auslegung aendert, macht deren Punkt ueberholt — wie
        /// von Hand. Die Wahl der Zone zeigt deren Eingabeblock (Satzwahl), „Anzeigen fuer"
        /// waehlt die Ansicht der Vorschau.
        /// </para>
        /// <para>
        /// <b>Die Stufen Erweitert und Experte</b> (Rechenweg der Jahresreihe ab Erweitert,
        /// Seed und Realisierungen, Belegung und Anlage, Schaetzhilfen, Fachwerte) sind nur
        /// setzbar, solange sie auf der Maske stehen; sonst nennt die Absage die Stufe, den
        /// Rechenweg, die Nutzungsart, die Einheit oder die Methode, die sie zeigt. Die Angaben
        /// einer Zone gelten der gewaehlten Zone. Neu, Duplizieren und Entfernen einer Zone oder
        /// eines Wohnungstyps, „Auslegung…", „Stochastisch rechnen", der Tagesgang-Editor und die
        /// Zapfkategorien bleiben Klicks des Anwenders.
        /// </para>
        /// </remarks>
        private static KiDialog Zapfprofil()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.ZAPFPROFIL,
                anzeigename: KiDialogTexte.MaskeZapfprofil,
                felder: new[]
                {
                    new KiDialogFeld("stufe", ZAPFPROFIL_SICHT + ".Stufe",
                                     KiDialogTexte.ZpgStufeName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgStufeErl),
                    new KiDialogFeld("zone", ZAPFPROFIL_SICHT + ".Zone",
                                     KiDialogTexte.ZpgZoneName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgZoneErl, satzwahl: true),

                    // ---- Die Zonen: SPALTEN mit dem Zonennamen als Kennzeichen -------
                    new KiDialogFeld("zonenname", ZAPFPROFIL_SICHT + ".Zonen[].Zonenname",
                                     KiDialogTexte.ZpgZonennameName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgZonennameErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("nutzungsart", ZAPFPROFIL_SICHT + ".Zonen[].Nutzungsart",
                                     KiDialogTexte.ZpgNutzungsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgNutzungsartErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("bezugsmenge", ZAPFPROFIL_SICHT + ".Zonen[].Bezugsmenge",
                                     KiDialogTexte.ZpgBezugsmengeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgBezugsmengeErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("niveau", ZAPFPROFIL_SICHT + ".Zonen[].Niveau",
                                     KiDialogTexte.ZpgNiveauName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgNiveauErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("jahresbedarf", ZAPFPROFIL_SICHT + ".Zonen[].Jahresbedarf",
                                     KiDialogTexte.ZpgJahresbedarfName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgJahresbedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen", nurLesen: true),

                    new KiDialogFeld("ansicht", ZAPFPROFIL_SICHT + ".Ansicht",
                                     KiDialogTexte.ZpgAnsichtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgAnsichtErl),

                    // ---- Die Stochastik der Jahresreihe (Stufe Experte) --------------
                    new KiDialogFeld("rechenweg_jahresreihe", ZAPFPROFIL_SICHT + ".Rechenweg",
                                     KiDialogTexte.ZpgRechenwegName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgRechenwegErl),
                    new KiDialogFeld("messreihe", ZAPFPROFIL_SICHT + ".Messreihe",
                                     KiDialogTexte.ZpgMessreiheName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgMessreiheErl, leerErlaubt: true),
                    new KiDialogFeld("seed", ZAPFPROFIL_SICHT + ".Seed",
                                     KiDialogTexte.ZpgSeedName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgSeedErl, leerErlaubt: true),
                    new KiDialogFeld("realisierungen", ZAPFPROFIL_SICHT + ".Realisierungen",
                                     KiDialogTexte.ZpgRealisierungenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgRealisierungenErl,
                                     einheit: KiDialogTexte.ZpgEinheitJahre, leerErlaubt: true),

                    // ---- Die Wahl des TYPTAGWEGS (Stufe Experte, Z4b) ----------------
                    // Eine Groesse des PROJEKTS: Ohne eingespielte Typtage lehnt die Maske
                    // benannt ab - dieselbe Sperre wie am Schalter des Dialogs.
                    new KiDialogFeld("typtageweg", ZAPFPROFIL_SICHT + ".Typtageweg",
                                     KiDialogTexte.ZpgTyptagewegName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.ZpgTyptagewegErl),
                    new KiDialogFeld("typtagzone", ZAPFPROFIL_SICHT + ".Typtagzone",
                                     KiDialogTexte.ZpgTyptagzoneName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgTyptagzoneErl, leerErlaubt: true),
                    new KiDialogFeld("typtagart", ZAPFPROFIL_SICHT + ".Typtagart",
                                     KiDialogTexte.ZpgTyptagartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgTyptagartErl, leerErlaubt: true)
                }
                .Concat(ZapfprofilHoehereStufen())
                .ToArray(),
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        /// <summary>
        /// Die Felder der Stufen Erweitert und Experte des Zapfprofils (Stufe Z4, Gruppe 2a) — wie der
        /// Eingabeblock gelten sie der GEWÄHLTEN Zone (Satzwahl „zone") und dem Gebäude; die
        /// Wohnungstabelle der gewählten Zone steht als SPALTEN mit der Zeilennummer als Kennzeichen.
        /// Ein Feld, das die Stufe (oder die Nutzungsart, die Einheit des Messwerts, die Methode der
        /// Zirkulation) nicht zeigt, lehnt die Maske benannt ab. Die Grenzen sind die der Eingabefelder.
        /// </summary>
        private static IEnumerable<KiDialogFeld> ZapfprofilHoehereStufen()
        {
            // ---- Die Wohnungstabelle der gewählten Zone (Erweitert, nur Wohnen) ----------------
            yield return new KiDialogFeld("wohnung_anzahl", ZAPFPROFIL_SICHT + ".Wohnungen[].Anzahl",
                                          KiDialogTexte.ZpgWohnungAnzahlName, KiParameterTyp.Ganzzahl,
                                          KiDialogTexte.ZpgWohnungAnzahlErl, zeilenkennzeichen: "Kennzeichen", min: 1);
            yield return new KiDialogFeld("wohnung_raumzahl", ZAPFPROFIL_SICHT + ".Wohnungen[].Raumzahl",
                                          KiDialogTexte.ZpgWohnungRaumzahlName, KiParameterTyp.Zahl,
                                          KiDialogTexte.ZpgWohnungRaumzahlErl, leerErlaubt: true,
                                          zeilenkennzeichen: "Kennzeichen", min: 0);
            yield return new KiDialogFeld("wohnung_personen", ZAPFPROFIL_SICHT + ".Wohnungen[].Personen",
                                          KiDialogTexte.ZpgWohnungPersonenName, KiParameterTyp.Zahl,
                                          KiDialogTexte.ZpgWohnungPersonenErl, leerErlaubt: true,
                                          zeilenkennzeichen: "Kennzeichen", min: 0);
            yield return new KiDialogFeld("wohnung_ausstattung", ZAPFPROFIL_SICHT + ".Wohnungen[].Ausstattung",
                                          KiDialogTexte.ZpgWohnungAusstattungName, KiParameterTyp.Wahl,
                                          KiDialogTexte.ZpgWohnungAusstattungErl, leerErlaubt: true,
                                          zeilenkennzeichen: "Kennzeichen");

            // ---- Belegung und Anlage der gewählten Zone (Erweitert) ------------------------------
            yield return Zahl("personen_je_we", "PersonenJeWe", KiDialogTexte.ZpgPersonenJeWeName,
                              KiDialogTexte.ZpgPersonenJeWeErl, KiDialogTexte.ZpgEinheitPersonenJeWe, 0, null);
            yield return Zahl("wohnflaeche_je_we", "WohnflaecheJeWe", KiDialogTexte.ZpgWohnflaecheJeWeName,
                              KiDialogTexte.ZpgWohnflaecheJeWeErl, KiDialogTexte.EINHEIT_M2, 0, null);
            yield return Wahl("topologie", "Topologie", KiDialogTexte.ZpgTopologieName, KiDialogTexte.ZpgTopologieErl, false);
            yield return Wahl("zirkulation_vorhanden", "ZirkulationVorhanden", KiDialogTexte.ZpgZirkulationVorhandenName,
                              KiDialogTexte.ZpgZirkulationVorhandenErl, false);
            yield return Wahl("kalender", "Kalender", KiDialogTexte.ZpgKalenderName, KiDialogTexte.ZpgKalenderErl, false);
            // Die vier Ferienzeitraeume als TABELLE (Muster Gebaeudekatalog): Spalten mit dem Zeitraum als Kennzeichen.
            yield return Ferienspalte("ferien_beginn_tag", "BeginnTag", KiDialogTexte.ZpgFerienBeginnTagName, 31);
            yield return Ferienspalte("ferien_beginn_monat", "BeginnMonat", KiDialogTexte.ZpgFerienBeginnMonatName, 12);
            yield return Ferienspalte("ferien_ende_tag", "EndeTag", KiDialogTexte.ZpgFerienEndeTagName, 31);
            yield return Ferienspalte("ferien_ende_monat", "EndeMonat", KiDialogTexte.ZpgFerienEndeMonatName, 12);
            yield return Zahl("jahresmesswert", "Jahresmesswert", KiDialogTexte.ZpgJahresmesswertName,
                              KiDialogTexte.ZpgJahresmesswertErl, null, 0, null);
            yield return Wahl("messwert_einheit", "MesswertEinheit", KiDialogTexte.ZpgMesswertEinheitName,
                              KiDialogTexte.ZpgMesswertEinheitErl, true);
            yield return Wahl("messwert_grenze", "MesswertGrenze", KiDialogTexte.ZpgMesswertGrenzeName,
                              KiDialogTexte.ZpgMesswertGrenzeErl, true);
            yield return Zahl("speicherverlust", "Speicherverlust", KiDialogTexte.ZpgSpeicherverlustName,
                              KiDialogTexte.ZpgSpeicherverlustErl, KiDialogTexte.EINHEIT_KWH_A, 0, null);
            yield return new KiDialogFeld("messwert_quelle", ZAPFPROFIL_SICHT + ".MesswertQuelle",
                                          KiDialogTexte.ZpgMesswertQuelleName, KiParameterTyp.Text,
                                          KiDialogTexte.ZpgMesswertQuelleErl, leerErlaubt: true);
            yield return new KiDialogFeld("messwert_zeitraum", ZAPFPROFIL_SICHT + ".MesswertZeitraum",
                                          KiDialogTexte.ZpgMesswertZeitraumName, KiParameterTyp.Text,
                                          KiDialogTexte.ZpgMesswertZeitraumErl, leerErlaubt: true);

            // ---- Tagesbedarf der Zone, Ladeleistung und Zirkulation des Gebäudes (Erweitert) -----
            yield return Wahl("tagesbedarf_modus", "TagesbedarfModus", KiDialogTexte.ZpgTagesbedarfModusName,
                              KiDialogTexte.ZpgTagesbedarfModusErl, false);
            yield return Zahl("tagesbedarf_manuell", "TagesbedarfManuell", KiDialogTexte.ZpgTagesbedarfManuellName,
                              KiDialogTexte.ZpgTagesbedarfManuellErl, KiDialogTexte.EINHEIT_KWH_D, 0, null);
            yield return Wahl("ladeleistung_modus", "LadeleistungModus", KiDialogTexte.ZpgLadeleistungModusName,
                              KiDialogTexte.ZpgLadeleistungModusErl, false);
            yield return Zahl("ladeleistung_manuell", "LadeleistungManuell", KiDialogTexte.ZpgLadeleistungManuellName,
                              KiDialogTexte.ZpgLadeleistungManuellErl, KiDialogTexte.EINHEIT_KW, 0, null);
            yield return Zahl("ladefenster", "Ladefenster", KiDialogTexte.ZpgLadefensterName,
                              KiDialogTexte.ZpgLadefensterErl, KiDialogTexte.EINHEIT_H_D, 0, 24);
            yield return Zahl("ladefenster_beginn", "LadefensterBeginn", KiDialogTexte.ZpgLadefensterBeginnName,
                              KiDialogTexte.ZpgLadefensterBeginnErl, KiDialogTexte.EINHEIT_STUNDE, 0, 24);
            yield return Wahl("zirkulation_modus", "ZirkulationModus", KiDialogTexte.ZpgZirkulationModusName,
                              KiDialogTexte.ZpgZirkulationModusErl, false);
            yield return Wahl("zirk_methode", "ZirkMethode", KiDialogTexte.ZpgZirkMethodeName, KiDialogTexte.ZpgZirkMethodeErl, false);
            yield return Zahl("zirk_laenge", "ZirkLaenge", KiDialogTexte.ZpgZirkLaengeName,
                              KiDialogTexte.ZpgZirkLaengeErl, KiDialogTexte.EINHEIT_METER, 0, null);
            yield return Zahl("zirk_verlust", "ZirkVerlust", KiDialogTexte.ZpgZirkVerlustName,
                              KiDialogTexte.ZpgZirkVerlustErl, KiDialogTexte.EINHEIT_W_M, 0, null);
            yield return Zahl("zirk_anteil", "ZirkAnteil", KiDialogTexte.ZpgZirkAnteilName,
                              KiDialogTexte.ZpgZirkAnteilErl, KiDialogTexte.EINHEIT_FAKTOR, 0, null);
            yield return Wahl("zirk_lage", "ZirkLage", KiDialogTexte.ZpgZirkLageName, KiDialogTexte.ZpgZirkLageErl, true);
            yield return Zahl("zirk_manuell", "ZirkManuell", KiDialogTexte.ZpgZirkManuellName,
                              KiDialogTexte.ZpgZirkManuellErl, KiDialogTexte.EINHEIT_KW, 0, null);
            yield return Zahl("leitungsinhalt", "Leitungsinhalt", KiDialogTexte.ZpgLeitungsinhaltName,
                              KiDialogTexte.ZpgLeitungsinhaltErl, KiDialogTexte.EINHEIT_LITER, 0, null);

            // ---- Fachwerte der gewählten Zone und des Gebäudes (Experte) -------------------------
            yield return Zahl("bedarf_spez", "BedarfSpez", KiDialogTexte.ZpgBedarfSpezName,
                              KiDialogTexte.ZpgBedarfSpezErl, null, 0, null);
            yield return Zahl("zapftemperatur", "Zapftemperatur", KiDialogTexte.ZpgZapftemperaturName,
                              KiDialogTexte.ZpgZapftemperaturErl, KiDialogTexte.EINHEIT_GRAD_C, null, null);
            yield return Zahl("kaltwasser_mittel", "KaltwasserMittel", KiDialogTexte.ZpgKaltwasserMittelName,
                              KiDialogTexte.ZpgKaltwasserMittelErl, KiDialogTexte.EINHEIT_GRAD_C, null, null);
            yield return Zahl("kaltwasser_amplitude", "KaltwasserAmplitude", KiDialogTexte.ZpgKaltwasserAmplitudeName,
                              KiDialogTexte.ZpgKaltwasserAmplitudeErl, KiDialogTexte.EINHEIT_KELVIN, 0, null);
            yield return new KiDialogFeld("auslastungsgang", ZAPFPROFIL_SICHT + ".Auslastungsgang",
                                          KiDialogTexte.ZpgAuslastungsgangName, KiParameterTyp.ZahlListe,
                                          KiDialogTexte.ZpgAuslastungsgangErl, leerErlaubt: true,
                                          reihe: KiZahlenreihen.Monate(), min: 0);
            yield return Wahl("tagesgangsatz", "Tagesgangsatz", KiDialogTexte.ZpgTagesgangsatzName,
                              KiDialogTexte.ZpgTagesgangsatzErl, true);
            yield return Zahl("kaltwasser_auslegung", "KaltwasserAuslegung", KiDialogTexte.ZpgKaltwasserAuslegungName,
                              KiDialogTexte.ZpgKaltwasserAuslegungErl, KiDialogTexte.EINHEIT_GRAD_C, null, null);
            yield return Zahl("speichertemperatur", "Speichertemperatur", KiDialogTexte.ZpgSpeichertemperaturName,
                              KiDialogTexte.ZpgSpeichertemperaturErl, KiDialogTexte.EINHEIT_GRAD_C, null, null);
            yield return Zahl("zirk_kennwert", "ZirkKennwert", KiDialogTexte.ZpgZirkKennwertName,
                              KiDialogTexte.ZpgZirkKennwertErl, KiDialogTexte.EINHEIT_KWH_M2A, 0, null);
            yield return Zahl("zirk_flaeche", "ZirkFlaeche", KiDialogTexte.ZpgZirkFlaecheName,
                              KiDialogTexte.ZpgZirkFlaecheErl, KiDialogTexte.EINHEIT_M2, 0, null);
            yield return Zahl("zirk_laufzeit", "ZirkLaufzeit", KiDialogTexte.ZpgZirkLaufzeitName,
                              KiDialogTexte.ZpgZirkLaufzeitErl, KiDialogTexte.EINHEIT_H_D, 0, 24);
            yield return Zahl("anzeigetemperatur", "Anzeigetemperatur", KiDialogTexte.ZpgAnzeigetemperaturName,
                              KiDialogTexte.ZpgAnzeigetemperaturErl, KiDialogTexte.EINHEIT_GRAD_C, null, null);
            yield return Zahl("stundenschwelle", "Stundenschwelle", KiDialogTexte.ZpgStundenschwelleName,
                              KiDialogTexte.ZpgStundenschwelleErl, KiDialogTexte.EINHEIT_KW, 0, null);

            static KiDialogFeld Zahl(string name, string eigenschaft, string anzeigename, string erlaeuterung, string einheit,
                                     double? min, double? max)
                => new KiDialogFeld(name, ZAPFPROFIL_SICHT + "." + eigenschaft, anzeigename, KiParameterTyp.Zahl, erlaeuterung,
                                    einheit: einheit, leerErlaubt: true, min: min, max: max);

            static KiDialogFeld Ferienspalte(string name, string eigenschaft, string anzeigename, double max)
                => new KiDialogFeld(name, ZAPFPROFIL_SICHT + ".Ferien[]." + eigenschaft, anzeigename, KiParameterTyp.Ganzzahl,
                                    KiDialogTexte.ZpgFerienErl, leerErlaubt: true, zeilenkennzeichen: "Zeitraum", min: 1, max: max);

            static KiDialogFeld Wahl(string name, string eigenschaft, string anzeigename, string erlaeuterung, bool leer)
                => new KiDialogFeld(name, ZAPFPROFIL_SICHT + "." + eigenschaft, anzeigename, KiParameterTyp.Wahl, erlaeuterung,
                                    leerErlaubt: leer);
        }

        // =====================================================================
        // ZapfprofilAuslegung  ->  Dialoge.Bedarf.ZapfprofilAuslegungDialog   (Welle #458, Stufe 3a)
        // =====================================================================

        /// <summary>
        /// Der Typname der Sichtklasse der Auslegung
        /// (<c>EPOS.UI.Dialoge.Bedarf.ZapfprofilAuslegungKiSicht</c>).
        /// </summary>
        private const string AUSLEGUNG_SICHT = "ZapfprofilAuslegungKiSicht";

        /// <summary>
        /// Die Auslegung Brauchwasser — einundzwanzig Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.ZapfprofilAuslegungKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung des Zapfprofils mit eigenem Arbeitsstand</b>: „OK" legt die
        /// Eingaben samt dem EINEN empfohlenen Punkt in den Arbeitsstand des Zapfprofils,
        /// geschrieben wird mit dem OK der Bedarfsprofile. Angemeldet sind Auffrischen und
        /// Pruefen (die Fehleingabe, die das OK anhaelt), KEIN Speicherweg.
        /// </para>
        /// <para>
        /// <b>Gesetzt wird auf den Wegen der Eingabefelder</b>, und jede Setzung rechnet die
        /// Karten entprellt neu (mit „Stochastisch rechnen" nebenlaeufig samt Ensemble). Der
        /// Bedarfstag ist eine Wahl aus Quelle UND Katalogtag in einem; gesperrte Quellen
        /// nennen ihren Grund. Perzentil und Realisierungen stehen nur mit „Stochastisch
        /// rechnen" auf der Maske und sind nur dann setzbar. Der empfohlene Punkt ist
        /// Ergebnis und nur lesbar. Die Eingaben des Verfahrensvergleichs (Ladeleistung und
        /// Personen auto/manuell, Ladezeitfenster, nutzbarer Anteil, Zuschlag, Bezug des
        /// Fuellstands) wirken nur auf den nachrichtlichen Vergleich; manuelle Werte sind nur
        /// mit „manuell" wirksam.
        /// </para>
        /// <para>
        /// <b>„Bedarfstag konstruieren…" und „An Speicherauslegung uebergeben…" bleiben
        /// Klicks des Anwenders</b> — das eine oeffnet eine eigene Ueberlagerung, das andere
        /// ist gesperrt.
        /// </para>
        /// </remarks>
        private static KiDialog ZapfprofilAuslegung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.ZAPFPROFIL_AUSLEGUNG,
                anzeigename: KiDialogTexte.MaskeZapfprofilAuslegung,
                felder: new[]
                {
                    new KiDialogFeld("bedarfstag", AUSLEGUNG_SICHT + ".Bedarfstag",
                                     KiDialogTexte.ZpgaBedarfstagName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaBedarfstagErl),
                    new KiDialogFeld("speichertemperatur", AUSLEGUNG_SICHT + ".Speichertemperatur",
                                     KiDialogTexte.ZpgaSpeichertemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaSpeichertemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("erzeugerleistung", AUSLEGUNG_SICHT + ".Erzeugerleistung",
                                     KiDialogTexte.ZpgaErzeugerleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaErzeugerleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("uebertragerleistung", AUSLEGUNG_SICHT + ".Uebertragerleistung",
                                     KiDialogTexte.ZpgaUebertragerleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaUebertragerleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("speicherart", AUSLEGUNG_SICHT + ".Speicherart",
                                     KiDialogTexte.ZpgaSpeicherartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaSpeicherartErl),
                    new KiDialogFeld("sensorhoehe", AUSLEGUNG_SICHT + ".Sensorhoehe",
                                     KiDialogTexte.ZpgaSensorhoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaSensorhoeheErl, leerErlaubt: true),
                    new KiDialogFeld("erzeugerart", AUSLEGUNG_SICHT + ".Erzeugerart",
                                     KiDialogTexte.ZpgaErzeugerartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaErzeugerartErl),
                    new KiDialogFeld("werkstoff", AUSLEGUNG_SICHT + ".Werkstoff",
                                     KiDialogTexte.ZpgaWerkstoffName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaWerkstoffErl),

                    // ---- Die Eingaben des Verfahrensvergleichs (4.7, N11 (d); Z4, Gruppe 2b) --
                    new KiDialogFeld("lade_modus", AUSLEGUNG_SICHT + ".LadeleistungModus",
                                     KiDialogTexte.ZpgaLadeModusName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaLadeModusErl),
                    new KiDialogFeld("lade_manuell", AUSLEGUNG_SICHT + ".LadeleistungManuell",
                                     KiDialogTexte.ZpgaLadeManuellName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaLadeManuellErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("ladefenster", AUSLEGUNG_SICHT + ".Ladefenster",
                                     KiDialogTexte.ZpgaLadefensterName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaLadefensterErl, leerErlaubt: true),
                    new KiDialogFeld("ladefenster_beginn", AUSLEGUNG_SICHT + ".LadefensterBeginn",
                                     KiDialogTexte.ZpgaLadefensterBeginnName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaLadefensterBeginnErl, leerErlaubt: true),
                    new KiDialogFeld("nutzanteil", AUSLEGUNG_SICHT + ".Nutzanteil",
                                     KiDialogTexte.ZpgaNutzanteilName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaNutzanteilErl, leerErlaubt: true),
                    new KiDialogFeld("zuschlag", AUSLEGUNG_SICHT + ".Zuschlag",
                                     KiDialogTexte.ZpgaZuschlagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaZuschlagErl, leerErlaubt: true),
                    new KiDialogFeld("personen_modus", AUSLEGUNG_SICHT + ".PersonenModus",
                                     KiDialogTexte.ZpgaPersonenModusName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaPersonenModusErl),
                    new KiDialogFeld("personen_manuell", AUSLEGUNG_SICHT + ".PersonenManuell",
                                     KiDialogTexte.ZpgaPersonenManuellName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgaPersonenManuellErl, leerErlaubt: true),
                    new KiDialogFeld("fuellstand_bezug", AUSLEGUNG_SICHT + ".FuellstandBezug",
                                     KiDialogTexte.ZpgaFuellstandBezugName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaFuellstandBezugErl),

                    // ---- Die Stochastik des Bedarfstags (4.5 b) ------------------------
                    new KiDialogFeld("stochastisch", AUSLEGUNG_SICHT + ".Stochastisch",
                                     KiDialogTexte.ZpgaStochastischName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.ZpgaStochastischErl),
                    new KiDialogFeld("perzentil", AUSLEGUNG_SICHT + ".Perzentil",
                                     KiDialogTexte.ZpgaPerzentilName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgaPerzentilErl),
                    new KiDialogFeld("realisierungen", AUSLEGUNG_SICHT + ".Realisierungen",
                                     KiDialogTexte.ZpgaRealisierungenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgaRealisierungenErl,
                                     einheit: KiDialogTexte.ZpgaEinheitTage, leerErlaubt: true),

                    // ---- Das Ergebnis, das OK uebernimmt --------------------------------
                    new KiDialogFeld("punkt", AUSLEGUNG_SICHT + ".Punkt",
                                     KiDialogTexte.ZpgaPunktName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgaPunktErl, leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // BedarfstagKonstruktor  ->  Dialoge.Bedarf.BedarfstagKonstruktor   (Welle #458, Stufe 3a)
        // =====================================================================

        /// <summary>
        /// Der Typname der Sichtklasse des Konstruktors
        /// (<c>EPOS.UI.Dialoge.Bedarf.BedarfstagKonstruktorKiSicht</c>).
        /// </summary>
        private const string KONSTRUKTOR_SICHT = "BedarfstagKonstruktorKiSicht";

        /// <summary>
        /// Der Konstruktor eines Bedarfstags nach A100 — zehn Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.BedarfstagKonstruktorKiSicht</c>, sieben davon SPALTEN
        /// der Zeilentabelle.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung der Auslegung mit eigenem Arbeitsstand</b>: „OK" baut den
        /// Tag ueber den Delegaten der Huelle und gibt ihn als Entwurf in die Auslegung —
        /// geschrieben wird nichts. Angemeldet sind Auffrischen und Pruefen: dieselbe Pruefung
        /// wie am OK (Fehleingaben, dann die benannte Pruefung der Huelle), ohne dass sich
        /// etwas schliesst; KEIN Speicherweg.
        /// </para>
        /// <para>
        /// <b>Die Zeilen sind SPALTEN</b> mit dem Zeitfenster samt Verbraucher als
        /// Kennzeichen. Anzahl steht nur mit einer Zapfregel, Volumen und Zapftemperatur nur
        /// bei „Volumen direkt" auf der Maske — sonst nennt die Absage, was sie bedienbar
        /// macht. Zeitfenster und Mengen tragen die Grenzen ihrer Felder. Eine Zeile legt
        /// der Anwender an oder entfernt sie. Bezugsart und Bezugsmenge gehoeren zusammen:
        /// die Menge ist nur mit einer Bezugsart setzbar, „ohne Bezug" nimmt sie weg.
        /// </para>
        /// </remarks>
        private static KiDialog BedarfstagKonstruktor()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BEDARFSTAG_KONSTRUKTOR,
                anzeigename: KiDialogTexte.MaskeBedarfstagKonstruktor,
                felder: new[]
                {
                    new KiDialogFeld("name", KONSTRUKTOR_SICHT + ".Name",
                                     KiDialogTexte.ZpgkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgkNameErl),

                    // ---- Der Bezug des Tags (Schritt 124, N10 (j); Z4, Gruppe 2b) ------
                    new KiDialogFeld("bezugsart", KONSTRUKTOR_SICHT + ".Bezugsart",
                                     KiDialogTexte.ZpgkBezugsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgkBezugsartErl, leerErlaubt: true),
                    new KiDialogFeld("bezugsmenge", KONSTRUKTOR_SICHT + ".Bezugsmenge",
                                     KiDialogTexte.ZpgkBezugsmengeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgkBezugsmengeErl, leerErlaubt: true),

                    // ---- Die Zeilen: SPALTEN mit Zeitfenster und Verbraucher -----------
                    new KiDialogFeld("beginn", KONSTRUKTOR_SICHT + ".Zeilen[].Beginn",
                                     KiDialogTexte.ZpgkBeginnName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgkBeginnErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("ende", KONSTRUKTOR_SICHT + ".Zeilen[].Ende",
                                     KiDialogTexte.ZpgkEndeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgkEndeErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("regel", KONSTRUKTOR_SICHT + ".Zeilen[].Regel",
                                     KiDialogTexte.ZpgkRegelName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgkRegelErl,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("anzahl", KONSTRUKTOR_SICHT + ".Zeilen[].Anzahl",
                                     KiDialogTexte.ZpgkAnzahlName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgkAnzahlErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("volumen", KONSTRUKTOR_SICHT + ".Zeilen[].Volumen",
                                     KiDialogTexte.ZpgkVolumenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgkVolumenErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("temperatur", KONSTRUKTOR_SICHT + ".Zeilen[].Temperatur",
                                     KiDialogTexte.ZpgkTemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgkTemperaturErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("verbraucher", KONSTRUKTOR_SICHT + ".Zeilen[].Verbraucher",
                                     KiDialogTexte.ZpgkVerbraucherName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgkVerbraucherErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen")
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // TagesgangEditor  ->  Dialoge.Bedarf.TagesgangEditor   (Zapfprofil Z4, Gruppe 2b)
        // =====================================================================

        /// <summary>Der Typname der Sichtklasse des Tagesgang-Editors (<c>EPOS.UI.Dialoge.Bedarf.TagesgangEditorKiSicht</c>).</summary>
        private const string TAGESGANG_SICHT = "TagesgangEditorKiSicht";

        /// <summary>
        /// Der Tagesgang-Editor des Zapfprofils — fünf Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TagesgangEditorKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung, die zum KATALOG gehoert</b>: Ihr „OK" schreibt den Tagesgang in einer
        /// Transaktion (bei gesperrter Nutzungsart als neue Katalogversion) — das bleibt der Klick des
        /// Anwenders. Angemeldet sind Auffrischen und Pruefen (die Fehleingabe, die das OK anhaelt),
        /// KEIN Speicherweg.
        /// </para>
        /// <para>
        /// <b>Zwei Zahlenreihen</b>: die 24 Stundenanteile des gezeigten Tagtyps und die sieben
        /// Wochenfaktoren, je in Prozent; „OK" normiert sie auf 100 %. Setzbar nur, solange die Maske
        /// sie bedienbar zeigt — eine gesperrte Nutzungsart erst nach „Als eigene Kopie
        /// bearbeiten…", das ein Klick des Anwenders bleibt, ebenso „Vorlage laden", „Normieren" und
        /// „Zuruecksetzen".
        /// </para>
        /// </remarks>
        private static KiDialog TagesgangEditor()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.TAGESGANG_EDITOR,
                anzeigename: KiDialogTexte.MaskeTagesgangEditor,
                felder: new[]
                {
                    new KiDialogFeld("tagtyp", TAGESGANG_SICHT + ".Tagtyp",
                                     KiDialogTexte.ZpgtTagtypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgtTagtypErl),
                    new KiDialogFeld("stunden", TAGESGANG_SICHT + ".Stunden",
                                     KiDialogTexte.ZpgtStundenName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.ZpgtStundenErl,
                                     reihe: KiZahlenreihen.Stunden(), min: 0),
                    new KiDialogFeld("wochenfaktoren", TAGESGANG_SICHT + ".Wochenfaktoren",
                                     KiDialogTexte.ZpgtWochenfaktorenName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.ZpgtWochenfaktorenErl,
                                     reihe: KiZahlenreihen.Wochentage(), min: 0),
                    new KiDialogFeld("vorlage", TAGESGANG_SICHT + ".Vorlage",
                                     KiDialogTexte.ZpgtVorlageName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgtVorlageErl, leerErlaubt: true),
                    new KiDialogFeld("katalogversion", TAGESGANG_SICHT + ".Katalogversion",
                                     KiDialogTexte.ZpgtKatalogversionName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtKatalogversionErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Zapfkategorien  ->  Dialoge.Bedarf.ZapfkategorienEditor   (Zapfprofil Z4, Gruppe 2b)
        // =====================================================================

        /// <summary>Der Typname der Sichtklasse des Kategorien-Editors (<c>EPOS.UI.Dialoge.Bedarf.ZapfkategorienEditorKiSicht</c>).</summary>
        private const string KATEGORIEN_SICHT = "ZapfkategorienEditorKiSicht";

        /// <summary>
        /// Der Editor der Zapfkategorien des Zapfprofils — sieben Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.ZapfkategorienEditorKiSicht</c>, sechs davon SPALTEN des Rasters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung, die zum KATALOG gehoert</b> (Katalogkopie, Status eigen): Ihr „OK"
        /// schreibt die Kategorien in einer Transaktion — das bleibt der Klick des Anwenders. Angemeldet
        /// sind Auffrischen und Pruefen (Fehleingabe und die Regeln des Kerns), KEIN Speicherweg.
        /// </para>
        /// <para>
        /// <b>Die Kategorien sind SPALTEN</b> mit ihrem Namen als Kennzeichen; die Grenzen sind die der
        /// Felder (μ, σ ≥ 0, Dauer 1 … 1440 min, Anteil ≥ 0, Kappung ≥ 0 oder leer). Neu, Entfernen,
        /// Reihenfolge, „Vorgabesatz laden" und „Als eigene Kopie bearbeiten…" bleiben Klicks des
        /// Anwenders.
        /// </para>
        /// </remarks>
        private static KiDialog Zapfkategorien()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.ZAPFKATEGORIEN,
                anzeigename: KiDialogTexte.MaskeZapfkategorien,
                felder: new[]
                {
                    new KiDialogFeld("katalogversion", KATEGORIEN_SICHT + ".Katalogversion",
                                     KiDialogTexte.ZpgzKatalogversionName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgzKatalogversionErl, leerErlaubt: true),
                    new KiDialogFeld("name", KATEGORIEN_SICHT + ".Zeilen[].Name",
                                     KiDialogTexte.ZpgzNameName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgzNameErl, zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("volumenstrom", KATEGORIEN_SICHT + ".Zeilen[].Volumenstrom",
                                     KiDialogTexte.ZpgzVolumenstromName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgzVolumenstromErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen", min: 0),
                    new KiDialogFeld("dauer", KATEGORIEN_SICHT + ".Zeilen[].Dauer",
                                     KiDialogTexte.ZpgzDauerName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgzDauerErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen", min: 1, max: 1440),
                    new KiDialogFeld("anteil", KATEGORIEN_SICHT + ".Zeilen[].Anteil",
                                     KiDialogTexte.ZpgzAnteilName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgzAnteilErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen", min: 0),
                    new KiDialogFeld("streuung", KATEGORIEN_SICHT + ".Zeilen[].Streuung",
                                     KiDialogTexte.ZpgzStreuungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgzStreuungErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen", min: 0),
                    new KiDialogFeld("kappung", KATEGORIEN_SICHT + ".Zeilen[].Kappung",
                                     KiDialogTexte.ZpgzKappungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgzKappungErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen", min: 0)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Brauchwasser_Nutzungsarten  ->  Dialoge.Bedarf.TwwNutzungsartAdminDialog   (Zapfprofil Z4, Gruppe 3)
        // =====================================================================

        /// <summary>Der Typname der Sichtklasse des Katalogdialogs (<c>EPOS.UI.Dialoge.Bedarf.TwwNutzungsartAdminKiSicht</c>).</summary>
        private const string TWW_KATALOG_SICHT = "TwwNutzungsartAdminKiSicht";

        /// <summary>
        /// Der Katalogdialog „Brauchwasser-Nutzungsarten" — vier Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TwwNutzungsartAdminKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Verwaltung ohne Einstellwert</b>: Das Stammblatt ist lesend; geschrieben wird ueber
        /// den Editor (eigene Maske <see cref="KiMaskennamen.TWW_NUTZUNGSART_EDITOR"/>), ueber
        /// „Tagesgang…" und „Kategorien…" (die Editoren des Zapfprofils) und ueber Loeschen und
        /// Import — Handlungen, die Zeilen anlegen oder wegnehmen und Klicks des Anwenders bleiben
        /// (KI-D-Q11). Die WAHL der Zeile ist <c>satzwahl</c>; Katalogversion, Stand und Sperrgrund
        /// der gewaehlten Zeile stehen als Anzeige daneben.
        /// </para>
        /// </remarks>
        private static KiDialog BrauchwasserNutzungsarten()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BRAUCHWASSER_NUTZUNGSARTEN,
                anzeigename: KiDialogTexte.MaskeBrauchwasserNutzungsarten,
                felder: new[]
                {
                    new KiDialogFeld("satz", TWW_KATALOG_SICHT + ".Satz",
                                     KiDialogTexte.ZpgkSatzName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ZpgkSatzErl, leerErlaubt: true, satzwahl: true),
                    new KiDialogFeld("katalogversion", TWW_KATALOG_SICHT + ".Katalogversion",
                                     KiDialogTexte.ZpgkKatalogversionName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgkKatalogversionErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("stand", TWW_KATALOG_SICHT + ".Stand",
                                     KiDialogTexte.ZpgkStandName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgkStandErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("sperrgrund", TWW_KATALOG_SICHT + ".Sperrgrund",
                                     KiDialogTexte.ZpgkSperrgrundName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgkSperrgrundErl, leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>Der Typname der Sichtklasse des Dialogs der Typtage (<c>EPOS.UI.Dialoge.Bedarf.TwwTyptagImportKiSicht</c>).</summary>
        private const string TWW_TYPTAG_SICHT = "TwwTyptagImportKiSicht";

        /// <summary>
        /// Der Dialog „VDI-4655-Typtage" — acht ANZEIGEN aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TwwTyptagImportKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung ohne Einstellwert</b>: Der Dialog zeigt den eingespielten Stand und
        /// den Bericht der Pruefung; geschrieben wird ueber „Einspielen" und „Loeschen" — Handlungen,
        /// die Zeilen anlegen oder wegnehmen und Klicks des Anwenders bleiben (KI-D-Q11). Auch die
        /// PAKETWAHL bleibt beim Anwender: Ein Dateipfad ist keine Eingabe, die ein Assistent setzen
        /// darf. Freigegeben sind deshalb allein Anzeigen, damit <c>dialog_lesen</c> nennt, was
        /// eingespielt ist und was die Pruefung ergeben hat.
        /// </para>
        /// <para>
        /// <b>Kein Wert der Richtlinie</b>: Quelle, Ausgabe, Tag des Einspielens, Zonen,
        /// Gebaeudearten und die Zahl der Zeilen sagen, was eingespielt IST — nie, wie gross ein
        /// Faktor ist (Konzept Kapitel 6).
        /// </para>
        /// </remarks>
        private static KiDialog BrauchwasserTyptage()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BRAUCHWASSER_TYPTAGE,
                anzeigename: KiDialogTexte.MaskeBrauchwasserTyptage,
                felder: new[]
                {
                    new KiDialogFeld("quelle", TWW_TYPTAG_SICHT + ".Quelle",
                                     KiDialogTexte.ZpgtQuelleName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtQuelleErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("ausgabe", TWW_TYPTAG_SICHT + ".Ausgabe",
                                     KiDialogTexte.ZpgtAusgabeName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtAusgabeErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("importdatum", TWW_TYPTAG_SICHT + ".Importdatum",
                                     KiDialogTexte.ZpgtDatumName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtDatumErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("klimazonen", TWW_TYPTAG_SICHT + ".Klimazonen",
                                     KiDialogTexte.ZpgtZonenName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtZonenErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gebaeudearten", TWW_TYPTAG_SICHT + ".Gebaeudearten",
                                     KiDialogTexte.ZpgtArtenName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtArtenErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("zeilen", TWW_TYPTAG_SICHT + ".Zeilen",
                                     KiDialogTexte.ZpgtZeilenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgtZeilenErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("grund", TWW_TYPTAG_SICHT + ".Grund",
                                     KiDialogTexte.ZpgtGrundName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtGrundErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("pruefbericht", TWW_TYPTAG_SICHT + ".Pruefbericht",
                                     KiDialogTexte.ZpgtBerichtName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgtBerichtErl, leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>Der Typname der Sichtklasse des Dialogs „Messdaten" (<c>EPOS.UI.Dialoge.Bedarf.TwwMessreihenKiSicht</c>).</summary>
        private const string TWW_MESSREIHEN_SICHT = "TwwMessreihenKiSicht";

        /// <summary>
        /// Der Dialog „Messdaten" — zwoelf Anzeigen aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TwwMessreihenKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung ohne Einstellwert</b>: Der Dialog zeigt die eingespielten Reihen und
        /// den Bericht der Pruefung; geschrieben wird ueber „Einspielen" und „Loeschen" — Handlungen,
        /// die Zeilen anlegen oder wegnehmen und Klicks des Anwenders bleiben (KI-D-Q11). Auch die
        /// DATEIWAHL bleibt beim Anwender, und die Eingaben darueber (Bezeichnung, Quelle, Groesse,
        /// Lueckenschwelle, Zeitstempel) beschreiben allein die gewaehlte Datei — ohne sie sind sie
        /// ohne Sinn. Freigegeben sind deshalb allein Anzeigen, damit <c>dialog_lesen</c> nennt, was
        /// eingespielt ist und was die Pruefung ergeben hat.
        /// </para>
        /// <para>
        /// <b>Keine Messwerte</b>: Bezeichnung, Groesse, Raster, Beginn, Tage, Nulllaeufe, Quelle und
        /// Tag des Einspielens sagen, WAS eingespielt ist — nie, wie gross eine gemessene Menge oder
        /// eine gemessene Spitze ist (Konzept Kapitel 9 K5).
        /// </para>
        /// </remarks>
        private static KiDialog BrauchwasserMessreihen()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BRAUCHWASSER_MESSREIHEN,
                anzeigename: KiDialogTexte.MaskeBrauchwasserMessreihen,
                felder: new[]
                {
                    new KiDialogFeld("anzahl", TWW_MESSREIHEN_SICHT + ".Anzahl",
                                     KiDialogTexte.ZpgmAnzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgmAnzahlErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("reihen", TWW_MESSREIHEN_SICHT + ".Reihen",
                                     KiDialogTexte.ZpgmReihenName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmReihenErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gewaehlt", TWW_MESSREIHEN_SICHT + ".Gewaehlt",
                                     KiDialogTexte.ZpgmGewaehltName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmGewaehltErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("groesse", TWW_MESSREIHEN_SICHT + ".Groesse",
                                     KiDialogTexte.ZpgmGroesseName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmGroesseErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("aufloesung", TWW_MESSREIHEN_SICHT + ".AufloesungMin",
                                     KiDialogTexte.ZpgmAufloesungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgmAufloesungErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beginn", TWW_MESSREIHEN_SICHT + ".Beginn",
                                     KiDialogTexte.ZpgmBeginnName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmBeginnErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("tage", TWW_MESSREIHEN_SICHT + ".Tage",
                                     KiDialogTexte.ZpgmTageName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ZpgmTageErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("nulllaeufe", TWW_MESSREIHEN_SICHT + ".Nulllaeufe",
                                     KiDialogTexte.ZpgmNulllaeufeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ZpgmNulllaeufeErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("quelle", TWW_MESSREIHEN_SICHT + ".Quelle",
                                     KiDialogTexte.ZpgmQuelleName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmQuelleErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("importdatum", TWW_MESSREIHEN_SICHT + ".Importdatum",
                                     KiDialogTexte.ZpgmDatumName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmDatumErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("grund", TWW_MESSREIHEN_SICHT + ".Grund",
                                     KiDialogTexte.ZpgmGrundName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmGrundErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("pruefbericht", TWW_MESSREIHEN_SICHT + ".Pruefbericht",
                                     KiDialogTexte.ZpgmBerichtName, KiParameterTyp.Text,
                                     KiDialogTexte.ZpgmBerichtErl, leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>Der Typname der Sichtklasse des Editors einer Nutzungsart (<c>EPOS.UI.Dialoge.Bedarf.TwwNutzungsartEditorKiSicht</c>).</summary>
        private const string TWW_EDITOR_SICHT = "TwwNutzungsartEditorKiSicht";

        /// <summary>
        /// Der Editor einer Nutzungsart — neunzehn Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TwwNutzungsartEditorKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung, die zum KATALOG gehoert</b>: Ihr „OK" schreibt die Nutzungsart in einer
        /// Transaktion — das bleibt der Klick des Anwenders. Angemeldet sind Auffrischen und Pruefen
        /// (Fehleingaben und Pflichtangaben), KEIN Speicherweg. Die Grenzen sind die der Felder:
        /// Bedarf, Bandbreite, Ferienfaktor und Monatsfaktoren nicht negativ; eine Wahl nur aus ihrer
        /// Liste. Die Wochenfaktoren bearbeitet „Tagesgang…" — hier stehen sie nicht.
        /// </para>
        /// </remarks>
        private static KiDialog TwwNutzungsartEditor()
        {
            var felder = new List<KiDialogFeld>
            {
                new KiDialogFeld("bezeichner", TWW_EDITOR_SICHT + ".Bezeichner",
                                 KiDialogTexte.ZpgkeBezeichnerName, KiParameterTyp.Text, KiDialogTexte.ZpgkeBezeichnerErl),
                new KiDialogFeld("katalogversion", TWW_EDITOR_SICHT + ".Katalogversion",
                                 KiDialogTexte.ZpgkKatalogversionName, KiParameterTyp.Text, KiDialogTexte.ZpgkeKatalogversionErl),
                new KiDialogFeld("bezugsart", TWW_EDITOR_SICHT + ".Bezugsart",
                                 KiDialogTexte.ZpgkeBezugsartName, KiParameterTyp.Wahl, KiDialogTexte.ZpgkeBezugsartErl),
                new KiDialogFeld("tagesgangsatz", TWW_EDITOR_SICHT + ".Tagesgangsatz",
                                 KiDialogTexte.ZpgkeTagesgangsatzName, KiParameterTyp.Wahl, KiDialogTexte.ZpgkeTagesgangsatzErl),
                new KiDialogFeld("bilanzgrenze", TWW_EDITOR_SICHT + ".Bilanzgrenze",
                                 KiDialogTexte.ZpgkeBilanzgrenzeName, KiParameterTyp.Wahl, KiDialogTexte.ZpgkeBilanzgrenzeErl),
                new KiDialogFeld("kalender", TWW_EDITOR_SICHT + ".Kalender",
                                 KiDialogTexte.ZpgkeKalenderName, KiParameterTyp.Wahl, KiDialogTexte.ZpgkeKalenderErl)
            };
            foreach ((string feld, string eigenschaft, string name) in new[]
                     {
                         ("bedarf_niedrig", "BedarfNiedrig", KiDialogTexte.ZpgkeBedarfNiedrigName),
                         ("bedarf_mittel", "BedarfMittel", KiDialogTexte.ZpgkeBedarfMittelName),
                         ("bedarf_hoch", "BedarfHoch", KiDialogTexte.ZpgkeBedarfHochName)
                     })
                felder.Add(new KiDialogFeld(feld, TWW_EDITOR_SICHT + "." + eigenschaft, name, KiParameterTyp.Zahl,
                                            KiDialogTexte.ZpgkeBedarfErl, einheit: KiDialogTexte.ZpgkeEinheitBedarf, min: 0));
            foreach ((string feld, string eigenschaft, string name) in new[]
                     {
                         ("bedarf_niedrig_min", "BedarfNiedrigMin", KiDialogTexte.ZpgkeGrenze(KiDialogTexte.ZpgkeBedarfNiedrigName, false)),
                         ("bedarf_niedrig_max", "BedarfNiedrigMax", KiDialogTexte.ZpgkeGrenze(KiDialogTexte.ZpgkeBedarfNiedrigName, true)),
                         ("bedarf_mittel_min", "BedarfMittelMin", KiDialogTexte.ZpgkeGrenze(KiDialogTexte.ZpgkeBedarfMittelName, false)),
                         ("bedarf_mittel_max", "BedarfMittelMax", KiDialogTexte.ZpgkeGrenze(KiDialogTexte.ZpgkeBedarfMittelName, true)),
                         ("bedarf_hoch_min", "BedarfHochMin", KiDialogTexte.ZpgkeGrenze(KiDialogTexte.ZpgkeBedarfHochName, false)),
                         ("bedarf_hoch_max", "BedarfHochMax", KiDialogTexte.ZpgkeGrenze(KiDialogTexte.ZpgkeBedarfHochName, true))
                     })
                felder.Add(new KiDialogFeld(feld, TWW_EDITOR_SICHT + "." + eigenschaft, name, KiParameterTyp.Zahl,
                                            KiDialogTexte.ZpgkeBandbreiteErl, leerErlaubt: true,
                                            einheit: KiDialogTexte.ZpgkeEinheitBedarf, min: 0));
            felder.Add(new KiDialogFeld("zapftemperatur", TWW_EDITOR_SICHT + ".Zapftemperatur",
                                        KiDialogTexte.ZpgkeZapftemperaturName, KiParameterTyp.Zahl,
                                        KiDialogTexte.ZpgkeTemperaturErl, einheit: KiDialogTexte.EINHEIT_GRAD_C));
            felder.Add(new KiDialogFeld("kaltwasser", TWW_EDITOR_SICHT + ".Kaltwasser",
                                        KiDialogTexte.ZpgkeKaltwasserName, KiParameterTyp.Zahl,
                                        KiDialogTexte.ZpgkeTemperaturErl, einheit: KiDialogTexte.EINHEIT_GRAD_C));
            felder.Add(new KiDialogFeld("ferienfaktor", TWW_EDITOR_SICHT + ".Ferienfaktor",
                                        KiDialogTexte.ZpgkeFerienfaktorName, KiParameterTyp.Zahl,
                                        KiDialogTexte.ZpgkeFerienfaktorErl, leerErlaubt: true, min: 0));
            felder.Add(new KiDialogFeld("monatsfaktoren", TWW_EDITOR_SICHT + ".Monatsfaktoren",
                                        KiDialogTexte.ZpgkeMonatsfaktorenName, KiParameterTyp.ZahlListe,
                                        KiDialogTexte.ZpgkeMonatsfaktorenErl, reihe: KiZahlenreihen.Monate(), min: 0));

            return new KiDialog(
                maskenname: KiMaskennamen.TWW_NUTZUNGSART_EDITOR,
                anzeigename: KiDialogTexte.MaskeTwwNutzungsartEditor,
                felder: felder,
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_*_Admin  ->  BedarfAdminDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// EINE Bedarfs-Katalogverwaltung — fuenf Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.BedarfAdminKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>DREI Katalogeintraege, EIN Feldsatz.</b> Prozesswaerme, Stromverbraucher
        /// und Brauchwasser sind drei Masken mit eigenem Namen und eigenem
        /// Navigationsziel; gezeichnet wird dieselbe Komponente, und die Felder sind
        /// dieselben. Deshalb baut diese Methode alle drei — der Unterschied ist der
        /// Schluessel und der Anzeigename.
        /// </para>
        /// <para>
        /// <b>Warum sie trotz KI-D-Q5 im Katalog stehen.</b> Sie sind mehr als eine
        /// Liste mit Suchfeld: Neben der Katalogliste steht ein Infoblock mit Typ,
        /// Beschreibung und Jahressumme des markierten Satzes — und genau danach fragt
        /// der Anwender. Die LISTENWAHL ist das Wahlfeld dieser Maske.
        /// </para>
        /// <para>
        /// <b>Seit Stufe 4 der Neuordnung sind die Kenndaten EINGABEN</b> (Welle #456):
        /// Typ, Beschreibung und die zwoelf Monatswerte stehen im Stammblatt als Felder,
        /// „Speichern" schreibt sie. Der Katalog folgt der Maske — alle vierzehn sind
        /// setzbar, der Typ als WAHL aus der Typliste. Name, Jahressumme und
        /// Bedarfsart bleiben Anzeigen. Der Satz ist SATZWAHL: Aus einem
        /// Auslieferungssatz heraus laesst sich der eigene waehlen.
        /// </para>
        /// <para>
        /// <b>Die Monatswerte sind zwoelf benannte Felder</b> (<c>januar</c> …
        /// <c>dezember</c>) und keine Spalte: Ein Monat ist kein Zeilentyp, und die Maske
        /// zeigt sie als zwoelf Zahlenfelder unter ihren Monatsnamen.
        /// </para>
        /// </remarks>
        private static KiDialog BedarfAdmin(string maskenname, string anzeigename)
        {
            var felder = new List<KiDialogFeld>
            {
                new KiDialogFeld("satz", "BedarfAdminKiSicht.Satz",
                                 KiDialogTexte.BadmSatzName, KiParameterTyp.Wahl,
                                 KiDialogTexte.BadmSatzErl, leerErlaubt: true, satzwahl: true),
                new KiDialogFeld("typ", "BedarfAdminKiSicht.Typ",
                                 KiDialogTexte.BadmTypName, KiParameterTyp.Wahl,
                                 KiDialogTexte.BadmTypErl),
                new KiDialogFeld("beschreibung", "BedarfAdminKiSicht.Beschreibung",
                                 KiDialogTexte.BadmBeschreibungName, KiParameterTyp.Text,
                                 KiDialogTexte.BadmBeschreibungErl, leerErlaubt: true),
                new KiDialogFeld("jahressumme", "BedarfAdminKiSicht.Jahressumme",
                                 KiDialogTexte.BadmJahressummeName, KiParameterTyp.Text,
                                 KiDialogTexte.BadmJahressummeErl,
                                 leerErlaubt: true, nurLesen: true),
                new KiDialogFeld("bedarfsart", "BedarfAdminKiSicht.Bedarfsart",
                                 KiDialogTexte.BedarfsartName, KiParameterTyp.Text,
                                 KiDialogTexte.BedarfsartErl, nurLesen: true)
            };

            for (int m = 1; m <= 12; m++)
                felder.Add(new KiDialogFeld(MONATSFELDER[m - 1],
                                            "BedarfAdminKiSicht." + MONATSEIGENSCHAFTEN[m - 1],
                                            KiDialogTexte.Monat(m), KiParameterTyp.Zahl,
                                            KiDialogTexte.BadmMonatErl(m),
                                            einheit: KiDialogTexte.EINHEIT_MWH));

            return new KiDialog(
                maskenname: maskenname,
                anzeigename: anzeigename,
                felder: felder,
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("verwerfen", "btn_Verwerfen", KiDialogTexte.KnopfVerwerfen),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>
        /// Die Feldnamen der zwoelf Monatswerte einer Bedarfsverwaltung — sprachneutral,
        /// ASCII (<see cref="KiName"/>).
        /// </summary>
        public static readonly IReadOnlyList<string> MONATSFELDER = new[]
        {
            "januar", "februar", "maerz", "april", "mai", "juni",
            "juli", "august", "september", "oktober", "november", "dezember"
        };

        /// <summary>Die Eigenschaften der Sichtklasse zu <see cref="MONATSFELDER"/>.</summary>
        private static readonly string[] MONATSEIGENSCHAFTEN =
        {
            "Januar", "Februar", "Maerz", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };

        // =====================================================================
        // Form_ErgStromverbraucher  ->  BedarfErgebnisDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Ergebnisanzeige eines Bedarfs — vier Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.BedarfErgebnisKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Maske, die nur ZEIGT.</b> Einstellbar sind allein die vier Schalter
        /// der Anzeige: die Anzeigeeinheit, die Sicht der Tabelle, die Sicht der Grafik
        /// und der Jahresverlauf. Sie schreiben nichts in die Datenbank; sie wechseln,
        /// was dasteht.
        /// </para>
        /// <para>
        /// <b>Die KENNZAHLEN selbst bleiben draussen</b>: Sie sind eine Liste von
        /// Zeilen mit eigenen Bezeichnern und wechseln mit der Auspraegung; ein
        /// Katalogfeld traegt EINEN Wert. Ebenso die zwoelf Monatswerte und die drei
        /// Zeitstufen des Ganglinienbausteins — ein Bild mit eigenem Navigator.
        /// </para>
        /// <para>
        /// <b>Der REITER steht nicht im Katalog:</b> Er wechselt nur das Blatt, nicht
        /// den Stand — dieselbe Regel wie bei den Knoepfen, die bloss die Liste
        /// umstellen.
        /// </para>
        /// </remarks>
        private static KiDialog BedarfErgebnis()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BEDARF_ERGEBNIS,
                anzeigename: KiDialogTexte.MaskeBedarfErgebnis,
                felder: new[]
                {
                    new KiDialogFeld("einheit", "BedarfErgebnisKiSicht.Einheit",
                                     KiDialogTexte.BergEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BergEinheitErl),
                    new KiDialogFeld("tabellensicht", "BedarfErgebnisKiSicht.Tabellensicht",
                                     KiDialogTexte.BergTabellensichtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BergTabellensichtErl, leerErlaubt: true),
                    new KiDialogFeld("grafiksicht", "BedarfErgebnisKiSicht.Grafiksicht",
                                     KiDialogTexte.BergGrafiksichtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BergGrafiksichtErl, leerErlaubt: true),
                    new KiDialogFeld("jahresverlauf", "BedarfErgebnisKiSicht.Jahresverlauf",
                                     KiDialogTexte.BergJahresverlaufName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BergJahresverlaufErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk)
                });
        }

        // =====================================================================
        // Form_Gebaeude_Bedarf  ->  GebaeudeBedarfDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Der Waermebedarf EINES Gebaeudes — sieben Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeBedarfKiSicht</c>; die Diagrammwahl je Zone kam mit
        /// Stufe G6b (W5) dazu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Maske, die RECHNET und nicht schreibt.</b> Drei Kennzahlen, eine
        /// Monatsuebersicht und ein Bild; eingestellt werden koennen nur die
        /// ANZEIGEEINHEIT und der Schalter zwischen Jahresganglinie und Dauerlinie.
        /// Beide liegen in den lebenden Feldern der Maske, die Kennzahlen im
        /// eingefrorenen Ergebnis — deshalb eine Sichtklasse ueber beides.
        /// </para>
        /// <para>
        /// <b>Die Kennzahlen stehen in MWh und kW</b>, so wie der Rechenkern sie
        /// liefert; die Maske rechnet nur fuer die Anzeige um.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die zwoelf Monatssummen</b> (eine Zahlenfolge ohne
        /// Zeilentyp) und das Bild.
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeBedarf()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_BEDARF,
                anzeigename: KiDialogTexte.MaskeGebaeudeBedarf,
                felder: new[]
                {
                    new KiDialogFeld("einheit", "GebaeudeBedarfKiSicht.Einheit",
                                     KiDialogTexte.GebbEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebbEinheitErl),
                    new KiDialogFeld("sortiert", "GebaeudeBedarfKiSicht.Sortiert",
                                     KiDialogTexte.GebbSortiertName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebbSortiertErl),
                    new KiDialogFeld("diagramm", "GebaeudeBedarfKiSicht.Diagramm",
                                     KiDialogTexte.GebbDiagrammName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebbDiagrammErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("gebaeude", "GebaeudeBedarfKiSicht.Gebaeude",
                                     KiDialogTexte.GebbGebaeudeName, KiParameterTyp.Text,
                                     KiDialogTexte.GebbGebaeudeErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("heizwaerme", "GebaeudeBedarfKiSicht.HeizwaermeMwh",
                                     KiDialogTexte.GebbHeizwaermeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebbHeizwaermeErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("max_last", "GebaeudeBedarfKiSicht.MaxLastKw",
                                     KiDialogTexte.GebbMaxLastName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebbMaxLastErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("vollbenutzungsstunden",
                                     "GebaeudeBedarfKiSicht.VollbenutzungsstundenH",
                                     KiDialogTexte.GebbVollbenutzungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebbVollbenutzungErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk)
                });
        }

        // =====================================================================
        // Form_EingGebTyp  ->  GebaeudetypDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Gebaeudetypen-Verwaltung — vier Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudetypKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der NAME ist eine WAHL und kein Textfeld</b> (KI-D-Q6): Die Liste links
        /// waehlt den Typ, und mit ihm laedt die Maske einen anderen Satz samt seinen
        /// fuenf oder acht Tageskurven. Ebenso die KURVE — sie sagt, welche 24
        /// Stundenwerte gerade dastehen.
        /// </para>
        /// <para>
        /// <b>Die 24 Stundenwerte der GEWAEHLTEN Kurve sind EINE ZAHLENREIHE</b>
        /// (Welle #458 Stufe 3b, <c>stundenwerte</c>, Stunde 1 bis 24): dieselben Zahlen,
        /// die „Stundenwerte…" zeigt und „Übernehmen" in den Arbeitsstand legt. Eine
        /// andere Kurve ist ein anderer Stand - die Maske haelt den Kurvenwechsel an,
        /// solange geaenderte Werte ungespeichert sind, und der Assistent folgt ihr:
        /// setzen, speichern, dann die naechste Kurve waehlen.
        /// </para>
        /// </remarks>
        private static KiDialog Gebaeudetyp()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDETYP,
                anzeigename: KiDialogTexte.MaskeGebaeudetyp,
                felder: new[]
                {
                    new KiDialogFeld("typ", "GebaeudetypKiSicht.Typ",
                                     KiDialogTexte.GtypTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GtypTypErl, leerErlaubt: true),
                    new KiDialogFeld("kurve", "GebaeudetypKiSicht.Kurve",
                                     KiDialogTexte.GtypKurveName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GtypKurveErl, leerErlaubt: true),
                    // Welle #458: Die Beschreibung ist seit der Neuordnung der
                    // Verwaltungen im Stammblatt bearbeitbar (BeiBeschreibung) - und
                    // damit auch fuer den Assistenten; ein Auslieferungstyp bleibt
                    // ueber Schreibgeschuetzt geschuetzt.
                    new KiDialogFeld("beschreibung", "GebaeudetypKiSicht.Beschreibung",
                                     KiDialogTexte.GtypBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GtypBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("stundenwerte", "GebaeudetypKiSicht.Stundenwerte",
                                     KiDialogTexte.GtypStundenwerteName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.GtypStundenwerteErl,
                                     reihe: KiZahlenreihen.Stunden())
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // BaustoffKatalog, Bauteilaufbau  ->  die zwei Kataloge der Gebaeudesimulation (G3)
        // =====================================================================

        /// <summary>
        /// Die Verwaltung „Baustoffe" — die Satzwahl und die sieben Kenndaten aus
        /// <c>EPOS.UI.Dialoge.Bedarf.BaustoffKatalogKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// Der Baustoff ist die SATZWAHL (Schluessel = Id; zwei Hersteller duerfen einen Stoff
        /// gleichen Namens fuehren) und bleibt frei, wenn ein Auslieferungssatz geschuetzt ist.
        /// Die Kenndaten schreiben in den Arbeitsstand des Stammblatts; geschrieben wird mit
        /// „Speichern", dieselbe Pruefung wie am Knopf (<c>BaustoffCtrl.Pruefen</c>).
        /// </remarks>
        private static KiDialog BaustoffKatalog()
        {
            const string SICHT = "BaustoffKatalogKiSicht.";
            return new KiDialog(
                maskenname: KiMaskennamen.BAUSTOFF_KATALOG,
                anzeigename: KiDialogTexte.MaskeBaustoffKatalog,
                felder: new[]
                {
                    new KiDialogFeld("baustoff", SICHT + "Baustoff", KiDialogTexte.BstBaustoffName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BstBaustoffErl,
                                     leerErlaubt: true, satzwahl: true),
                    new KiDialogFeld("bezeichner", SICHT + "Bezeichner", KiDialogTexte.BstBezeichnerName,
                                     KiParameterTyp.Text, KiDialogTexte.BstBezeichnerErl),
                    new KiDialogFeld("gruppe", SICHT + "Gruppe", KiDialogTexte.BstGruppeName,
                                     KiParameterTyp.Text, KiDialogTexte.BstGruppeErl, leerErlaubt: true),
                    new KiDialogFeld("hersteller", SICHT + "Hersteller", KiDialogTexte.BstHerstellerName,
                                     KiParameterTyp.Text, KiDialogTexte.BstHerstellerErl, leerErlaubt: true),
                    new KiDialogFeld("lambda", SICHT + "Lambda", KiDialogTexte.BstLambdaName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BstLambdaErl,
                                     einheit: KiDialogTexte.EINHEIT_LAMBDA, leerErlaubt: true,
                                     min: BaustoffCtrl.LAMBDA_MIN, max: BaustoffCtrl.LAMBDA_MAX),
                    new KiDialogFeld("rho", SICHT + "Rho", KiDialogTexte.BstRhoName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BstRhoErl,
                                     einheit: KiDialogTexte.EINHEIT_RHO, leerErlaubt: true,
                                     min: BaustoffCtrl.RHO_MIN, max: BaustoffCtrl.RHO_MAX),
                    new KiDialogFeld("cp", SICHT + "Cp", KiDialogTexte.BstCpName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BstCpErl,
                                     einheit: KiDialogTexte.EINHEIT_CP, leerErlaubt: true,
                                     min: BaustoffCtrl.CP_MIN, max: BaustoffCtrl.CP_MAX),
                    new KiDialogFeld("quelle", SICHT + "Quelle", KiDialogTexte.BstQuelleName,
                                     KiParameterTyp.Text, KiDialogTexte.BstQuelleErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>
        /// Die Verwaltung „Bauteilaufbauten" — die Satzwahl, vier Kopffelder und das Raster der
        /// Schichten aus <c>EPOS.UI.Dialoge.Bedarf.BauteilaufbauKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// Die Schichten sind ein RASTER (<c>Schichten[]</c>, Kennzeichen die Nummer innen = 1):
        /// Baustoff (Wahl — die Schicht uebernimmt λ, ρ und c_p als Kopie), Dicke in mm, λ, ρ,
        /// c_p und der Schalter Luftschicht. Anlegen, Verschieben und Entfernen einer Schicht
        /// bleiben Klicks des Anwenders. Geschrieben wird der Aufbau als EIN Aggregat mit
        /// „Speichern"; die Pruefregeln sind die des Knopfes (<c>BauteilaufbauCtrl.EingabePruefen</c>).
        /// </remarks>
        private static KiDialog Bauteilaufbau()
        {
            const string SICHT = "BauteilaufbauKiSicht.";
            string schicht = SICHT + "Schichten" + KiEigenschaftspfad.Sammlungszeichen + ".";
            const string NUMMER = "Nummer";
            return new KiDialog(
                maskenname: KiMaskennamen.BAUTEILAUFBAU,
                anzeigename: KiDialogTexte.MaskeBauteilaufbau,
                felder: new[]
                {
                    new KiDialogFeld("aufbau", SICHT + "Aufbau", KiDialogTexte.BtaAufbauName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtaAufbauErl,
                                     leerErlaubt: true, satzwahl: true),
                    new KiDialogFeld("bezeichner", SICHT + "Bezeichner", KiDialogTexte.BtaBezeichnerName,
                                     KiParameterTyp.Text, KiDialogTexte.BtaBezeichnerErl),
                    new KiDialogFeld("bauteilart", SICHT + "Bauteilart", KiDialogTexte.BtaBauteilartName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtaBauteilartErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", SICHT + "Beschreibung", KiDialogTexte.BtaBeschreibungName,
                                     KiParameterTyp.Text, KiDialogTexte.BtaBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("quelle", SICHT + "Quelle", KiDialogTexte.BtaQuelleName,
                                     KiParameterTyp.Text, KiDialogTexte.BtaQuelleErl, leerErlaubt: true),
                    new KiDialogFeld("schicht_baustoff", schicht + "Baustoff", KiDialogTexte.BtaSchichtBaustoffName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtaSchichtBaustoffErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER),
                    new KiDialogFeld("schicht_dicke", schicht + "DickeMm", KiDialogTexte.BtaSchichtDickeName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtaSchichtDickeErl,
                                     einheit: KiDialogTexte.EINHEIT_MM, zeilenkennzeichen: NUMMER,
                                     min: BauteilaufbauCtrl.DickeMm(GebaeudeFestwerte.SCHICHT_DICKE_MIN_M),
                                     max: BauteilaufbauCtrl.DickeMm(GebaeudeFestwerte.SCHICHT_DICKE_MAX_M)),
                    new KiDialogFeld("schicht_lambda", schicht + "Lambda", KiDialogTexte.BtaSchichtLambdaName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtaSchichtLambdaErl,
                                     einheit: KiDialogTexte.EINHEIT_LAMBDA, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER,
                                     min: BaustoffCtrl.LAMBDA_MIN, max: BaustoffCtrl.LAMBDA_MAX),
                    new KiDialogFeld("schicht_rho", schicht + "Rho", KiDialogTexte.BtaSchichtRhoName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtaSchichtRhoErl,
                                     einheit: KiDialogTexte.EINHEIT_RHO, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER,
                                     min: BaustoffCtrl.RHO_MIN, max: BaustoffCtrl.RHO_MAX),
                    new KiDialogFeld("schicht_cp", schicht + "Cp", KiDialogTexte.BtaSchichtCpName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtaSchichtCpErl,
                                     einheit: KiDialogTexte.EINHEIT_CP, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER,
                                     min: BaustoffCtrl.CP_MIN, max: BaustoffCtrl.CP_MAX),
                    new KiDialogFeld("schicht_luftschicht", schicht + "IstLuftschicht",
                                     KiDialogTexte.BtaSchichtLuftschichtName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.BtaSchichtLuftschichtErl, zeilenkennzeichen: NUMMER)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>
        /// Der Zonendialog des Gebäudeeditors (Gebaeudesimulation G3, Welle D2; Stufe G6b W2) — sechzehn
        /// Felder (Bezeichnung, Nutzfläche und die Werte der Zone; leer = der Wert des Gebäudes) und das
        /// Raster der Bauteile aus <c>EPOS.UI.Dialoge.Bedarf.ZonenKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// Die Bauteile sind ein RASTER zum LESEN (<c>Bauteile[]</c>, Kennzeichen die Nummer ab 1):
        /// Anlegen, Öffnen und Entfernen bleiben Klicks des Anwenders, die Werte eines Bauteils setzt
        /// der Assistent im Bauteildialog (<see cref="Bauteil"/>). Der Dialog schreibt nicht — er
        /// gibt die Zone an den Arbeitsstand des Gebäudeeditors zurück, und dessen OK schreibt.
        /// </remarks>
        private static KiDialog Zone()
        {
            const string SICHT = "ZonenKiSicht.";
            string bauteil = SICHT + "Bauteile" + KiEigenschaftspfad.Sammlungszeichen + ".";
            const string NUMMER = "Nummer";
            return new KiDialog(
                maskenname: KiMaskennamen.ZONE,
                anzeigename: KiDialogTexte.MaskeZone,
                felder: new[]
                {
                    new KiDialogFeld("bezeichnung", SICHT + "Bezeichnung", KiDialogTexte.ZonBezeichnungName,
                                     KiParameterTyp.Text, KiDialogTexte.ZonBezeichnungErl),
                    new KiDialogFeld("nutzflaeche", SICHT + "Nutzflaeche", KiDialogTexte.ZonNutzflaecheName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonNutzflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true),
                    // Stufe G6b (W2): die Werte der Zone - leer = der Wert des Gebaeudes.
                    new KiDialogFeld("raumhoehe", SICHT + "Raumhoehe", KiDialogTexte.ZonRaumhoeheName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonRaumhoeheErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true, min: 0.0),
                    new KiDialogFeld("volumen", SICHT + "Volumen", KiDialogTexte.ZonVolumenName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonVolumenErl,
                                     einheit: KiDialogTexte.EINHEIT_M3, leerErlaubt: true, min: 0.0),
                    new KiDialogFeld("beheizt", SICHT + "Beheizt", KiDialogTexte.ZonBeheiztName,
                                     KiParameterTyp.Wahrheitswert, KiDialogTexte.ZonBeheiztErl),
                    new KiDialogFeld("soll_tag", SICHT + "SollTag", KiDialogTexte.ZonSollTagName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("soll_nacht", SICHT + "SollNacht", KiDialogTexte.ZonSollNachtName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("soll_wochenende", SICHT + "SollWochenende", KiDialogTexte.ZonSollWochenendeName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("soll_ferien", SICHT + "SollFerien", KiDialogTexte.ZonSollFerienName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("max_temperatur", SICHT + "MaxTemperatur", KiDialogTexte.ZonMaxTemperaturName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("infiltration", SICHT + "Infiltration", KiDialogTexte.ZonInfiltrationName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonLueftungErl,
                                     einheit: KiDialogTexte.EINHEIT_1_H, leerErlaubt: true, min: 0.01, max: 10.0),
                    new KiDialogFeld("nutzerlueftung", SICHT + "Nutzerlueftung", KiDialogTexte.ZonNutzerlueftungName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonLueftungErl,
                                     einheit: KiDialogTexte.EINHEIT_1_H, leerErlaubt: true, min: 0.0, max: 10.0),
                    new KiDialogFeld("gewinne", SICHT + "Gewinne", KiDialogTexte.ZonGewinneName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonAnteiligErl,
                                     einheit: KiDialogTexte.EINHEIT_W, leerErlaubt: true, min: 0.0),
                    new KiDialogFeld("bewohner", SICHT + "Bewohner", KiDialogTexte.ZonBewohnerName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonAnteiligErl,
                                     leerErlaubt: true, min: 0.0),
                    new KiDialogFeld("strahlungsanteil", SICHT + "Strahlungsanteil", KiDialogTexte.ZonStrahlungName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonStrahlungErl,
                                     leerErlaubt: true, min: 0.0, max: 1.0),
                    new KiDialogFeld("heizleistung_max", SICHT + "HeizleistungMax", KiDialogTexte.ZonHeizleistungName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonHeizleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true, min: 0.01),
                    new KiDialogFeld("bauteil_art", bauteil + "Art", KiDialogTexte.ZonBauteilArtName,
                                     KiParameterTyp.Text, KiDialogTexte.ZonBauteilArtErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_bezeichnung", bauteil + "Bezeichnung",
                                     KiDialogTexte.ZonBezeichnungName, KiParameterTyp.Text,
                                     KiDialogTexte.ZonBauteilBezeichnungErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_flaeche", bauteil + "Flaeche", KiDialogTexte.BtFlaecheName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonBauteilFlaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_uwert", bauteil + "UWert", KiDialogTexte.BtUWertName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonBauteilUWertErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_azimut", bauteil + "Azimut", KiDialogTexte.BtAzimutName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZonBauteilAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_aufbau", bauteil + "Aufbau", KiDialogTexte.ZonBauteilAufbauName,
                                     KiParameterTyp.Text, KiDialogTexte.ZonBauteilAufbauErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_rand", bauteil + "Rand", KiDialogTexte.BtRandName,
                                     KiParameterTyp.Text, KiDialogTexte.ZonBauteilRandErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("bauteil_nachbar", bauteil + "Nachbar", KiDialogTexte.ZonBauteilNachbarName,
                                     KiParameterTyp.Text, KiDialogTexte.ZonBauteilNachbarErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        /// <summary>
        /// Der Bauteildialog (Gebaeudesimulation G3, Welle D2; Stufe G6b W2) — vierzehn Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.BauteilKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// Bauteilart, Randbedingung, Nachbarzone, Zuordnung und Aufbau sind WAHLFELDER (Listenplätze
        /// bzw. Ids der Maske; die Nachbarzone mit der Id der Zone im Arbeitsstand, auch einer vorläufigen).
        /// Die Grenzen sind die der Prüfregeln (<c>GebaeudeZonenCtrl.BauteilPruefen</c>, Mehrzonenkonzept
        /// 5.3); die Prüfung selbst läuft wie am OK-Knopf. Einen Katalogaufbau übernimmt der Anwender
        /// von Hand — die Kopie in das Projekt ist ein Schreibweg des OK im Gebäudeeditor.
        /// </remarks>
        private static KiDialog Bauteil()
        {
            const string SICHT = "BauteilKiSicht.";
            return new KiDialog(
                maskenname: KiMaskennamen.BAUTEIL,
                anzeigename: KiDialogTexte.MaskeBauteil,
                felder: new[]
                {
                    new KiDialogFeld("art", SICHT + "Art", KiDialogTexte.BtArtName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtArtErl),
                    new KiDialogFeld("bezeichnung", SICHT + "Bezeichnung", KiDialogTexte.ZonBezeichnungName,
                                     KiParameterTyp.Text, KiDialogTexte.BtBezeichnungErl),
                    new KiDialogFeld("flaeche", SICHT + "Flaeche", KiDialogTexte.BtFlaecheName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtFlaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true, min: 0.0),
                    new KiDialogFeld("azimut", SICHT + "Azimut", KiDialogTexte.BtAzimutName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     min: 0.0, max: 360.0),
                    new KiDialogFeld("neigung", SICHT + "Neigung", KiDialogTexte.BtNeigungName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     min: 0.0, max: 180.0),
                    new KiDialogFeld("randbedingung", SICHT + "Randbedingung", KiDialogTexte.BtRandName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtRandErl),
                    // Stufe G6b (W2): Nachbarzone und Zuordnung einer Trennflaeche.
                    new KiDialogFeld("nachbarzone", SICHT + "Nachbarzone", KiDialogTexte.BtNachbarzoneName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtNachbarzoneErl, leerErlaubt: true),
                    new KiDialogFeld("zuordnung", SICHT + "Zuordnung", KiDialogTexte.BtZuordnungName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtZuordnungErl, leerErlaubt: true),
                    new KiDialogFeld("gwert", SICHT + "GWert", KiDialogTexte.BtGWertName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtGWertErl,
                                     leerErlaubt: true, min: 0.0, max: 1.0),
                    new KiDialogFeld("rahmenanteil", SICHT + "Rahmenanteil", KiDialogTexte.BtRahmenName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtRahmenErl, leerErlaubt: true,
                                     min: GebaeudeZonenCtrl.RAHMENANTEIL_MIN, max: GebaeudeZonenCtrl.RAHMENANTEIL_MAX),
                    new KiDialogFeld("verschattung", SICHT + "Verschattung", KiDialogTexte.BtVerschattungName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtVerschattungErl,
                                     leerErlaubt: true, min: 0.0, max: 1.0),
                    new KiDialogFeld("psil", SICHT + "PsiL", KiDialogTexte.BtPsiLName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtPsiLErl,
                                     einheit: KiDialogTexte.EINHEIT_W_K, leerErlaubt: true, min: 0.0),
                    new KiDialogFeld("uwert", SICHT + "UWert", KiDialogTexte.BtUWertName,
                                     KiParameterTyp.Zahl, KiDialogTexte.BtUWertErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K, leerErlaubt: true,
                                     min: GebaeudeFestwerte.U_MIN, max: GebaeudeFestwerte.U_MAX),
                    new KiDialogFeld("aufbau", SICHT + "Aufbau", KiDialogTexte.BtAufbauName,
                                     KiParameterTyp.Wahl, KiDialogTexte.BtAufbauErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        /// <summary>
        /// Der Gebäudeexport im Format gbXML (Gebaeudesimulation G7a, Welle W3) — die Postleitzahl und
        /// die Bestätigung der Meldungen aus <c>EPOS.UI.Dialoge.Export.GebaeudeExportKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// Gespeichert wird mit einem Klick des Anwenders auf „Speichern…" (die Dateiwahl der Plattform);
        /// der Assistent setzt die Postleitzahl und LIEST die Bestätigung — bestätigen, die Meldungen
        /// gelesen zu haben, kann nur der Anwender. Format und Umfang stehen fest und sind keine Felder.
        /// </remarks>
        private static KiDialog GebaeudeExport()
        {
            const string SICHT = "GebaeudeExportKiSicht.";
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_EXPORT,
                anzeigename: KiDialogTexte.MaskeGebaeudeExport,
                felder: new[]
                {
                    new KiDialogFeld("plz", SICHT + "Plz", KiDialogTexte.GexpPlzName,
                                     KiParameterTyp.Text, KiDialogTexte.GexpPlzErl, leerErlaubt: true),
                    new KiDialogFeld("bestaetigt", SICHT + "Bestaetigt", KiDialogTexte.GexpBestaetigtName,
                                     KiParameterTyp.Wahrheitswert, KiDialogTexte.GexpBestaetigtErl, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        /// <summary>
        /// Der Luftaustausch zwischen den Zonen eines Gebäudes (Gebaeudesimulation G6b, Welle W2) —
        /// das Raster der Luftströme aus <c>EPOS.UI.Dialoge.Bedarf.LuftaustauschKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// Die Luftströme sind ein RASTER (<c>Luftstroeme[]</c>, Kennzeichen die Nummer ab 1): die beiden
        /// Zonen zum Lesen, der Volumenstrom setzbar. Zeilen anlegen und entfernen und die Zonen wählen
        /// bleiben Klicks des Anwenders. Der Dialog schreibt nicht — er gibt die Luftströme an den
        /// Arbeitsstand des Gebäudeeditors zurück, und dessen OK schreibt.
        /// </remarks>
        private static KiDialog Luftaustausch()
        {
            string zeile = "LuftaustauschKiSicht.Luftstroeme" + KiEigenschaftspfad.Sammlungszeichen + ".";
            const string NUMMER = "Nummer";
            return new KiDialog(
                maskenname: KiMaskennamen.LUFTAUSTAUSCH,
                anzeigename: KiDialogTexte.MaskeLuftaustausch,
                felder: new[]
                {
                    new KiDialogFeld("zone_a", zeile + "ZoneA", KiDialogTexte.ZluftZoneAName,
                                     KiParameterTyp.Text, KiDialogTexte.ZluftZoneErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("zone_b", zeile + "ZoneB", KiDialogTexte.ZluftZoneBName,
                                     KiParameterTyp.Text, KiDialogTexte.ZluftZoneErl,
                                     leerErlaubt: true, zeilenkennzeichen: NUMMER, nurLesen: true),
                    new KiDialogFeld("volumenstrom", zeile + "Volumenstrom", KiDialogTexte.ZluftVolumenstromName,
                                     KiParameterTyp.Zahl, KiDialogTexte.ZluftVolumenstromErl,
                                     einheit: KiDialogTexte.EINHEIT_M3_H, leerErlaubt: true,
                                     zeilenkennzeichen: NUMMER, min: 0.0)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_EingStromTyp  ->  TypProfilDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Das Wochen-Stundenprofil eines Bedarfstyps — vier Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TypProfilKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der TYP ist eine WAHL</b> (KI-D-Q6): Die Liste laedt einen anderen Satz
        /// samt seinen 168 Wochenwerten. Der WOCHENTAG ist es ebenfalls — er sagt,
        /// welche 24 Felder gerade dastehen.
        /// </para>
        /// <para>
        /// <b>Die 7 x 24 UEBERNOMMENEN Wochenwerte sind EINE ZAHLENREIHE</b> (Welle #458
        /// Stufe 3b, <c>wochenwerte</c>, Montag Stunde 1 bis Sonntag Stunde 24): der
        /// Stand, den „Speichern in DB" schreibt. Gesetzt wird er so, wie „Änderungen
        /// Übernehmen" ihn setzt; die 24 Felder des gezeigten Tages ziehen nach. Ein Tag
        /// ist ein Ausschnitt ab seiner ersten Stunde (Dienstag = Stelle 25).
        /// </para>
        /// </remarks>
        private static KiDialog Typprofil()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.TYPPROFIL,
                anzeigename: KiDialogTexte.MaskeTypprofil,
                felder: new[]
                {
                    new KiDialogFeld("typ", "TypProfilKiSicht.Typ",
                                     KiDialogTexte.TprofTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TprofTypErl, leerErlaubt: true),
                    new KiDialogFeld("wochentag", "TypProfilKiSicht.Wochentag",
                                     KiDialogTexte.TprofWochentagName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TprofWochentagErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "TypProfilKiSicht.Beschreibung",
                                     KiDialogTexte.TprofBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.TprofBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("wochenwerte", "TypProfilKiSicht.Wochenwerte",
                                     KiDialogTexte.TprofWochenwerteName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.TprofWochenwerteErl,
                                     reihe: KiZahlenreihen.Wochenstunden())
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_EingDBStromverbraucher  ->  TypStammDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Der Kopfsatz eines Bedarfskatalogs — vier Felder an
        /// <c>EPOS.UI.Dialoge.Bedarf.TypStammDaten</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die einzige Maske dieser Welle OHNE Sichtklasse.</b> Ihr Satz ist
        /// veraenderlich, und jedes der drei Felder bindet unmittelbar ans Markup — der
        /// Katalog haengt sich also an dasselbe Objekt, das die Maske zeigt. Genau
        /// dafuer ist der Weg da; eine Sichtklasse waere hier eine Schicht ohne Zweck.
        /// </para>
        /// <para>
        /// <b>Der VERBRAUCHERTYP ist ein WAHLFELD</b> (KI-D-Q6) und traegt als
        /// Schluessel den Namen des Typkatalogsatzes. Seine Eintraege kennt nur der
        /// Dialog; er reicht sie beim Anmelden als Lieferant herein.
        /// </para>
        /// <para>
        /// <b>Die zwoelf MONATSWERTE sind EINE ZAHLENREIHE</b> (Welle #458 Stufe 3b):
        /// das Feld <c>monatswerte</c> an <c>TypStammDaten.Monat</c>, derselben Liste,
        /// an der die zwoelf Eingabefelder haengen. Die Pflichtpruefung ueber alle zwoelf
        /// bleibt die des Dialogs - der Haken „Pruefen" meldet einen leeren Monat.
        /// </para>
        /// </remarks>
        private static KiDialog Typstamm()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.TYPSTAMM,
                anzeigename: KiDialogTexte.MaskeTypstamm,
                felder: new[]
                {
                    new KiDialogFeld("name", "TypStammDaten.Name",
                                     KiDialogTexte.TstammNameName, KiParameterTyp.Text,
                                     KiDialogTexte.TstammNameErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("typ", "TypStammDaten.Typ",
                                     KiDialogTexte.TstammTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TstammTypErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "TypStammDaten.Beschreibung",
                                     KiDialogTexte.TstammBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.TstammBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("monatswerte", "TypStammDaten.Monat",
                                     KiDialogTexte.TstammMonatswerteName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.TstammMonatswerteErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH,
                                     reihe: KiZahlenreihen.Monate())
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_Gebaeude  ->  GebaeudeDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Gebaeudemaske — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Maske des PROJEKTS.</b> Projektliste und Katalog stehen nebeneinander (in
        /// den Betriebsarten Projekt und Assistent). Die Gebaeudeverwaltung ist eine eigene
        /// Komponente mit eigener Maske (<see cref="GebaeudeVerwaltung"/>); ein Feld, das
        /// hier die Betriebsart „Verwaltung" meldete, gibt es deshalb nicht.
        /// </para>
        /// <para>
        /// <b>Setzbar sind die vier FILTERFELDER</b> der Katalogliste (Stufe G3, Welle K):
        /// die Trichter der Spalten Verwendung, Gebaeudeart und Baujahr und die Suche ueber
        /// alle Spalten. Mit ihnen findet der Anwender den Satz, den er uebernehmen will. Die
        /// drei Trichter sind Wahlfelder ueber die Werte, die in ihrer Spalte stehen (der
        /// Trichter filtert auf dem Anzeigetext, „enthaelt"); leer nimmt den Filter zurueck.
        /// Die fuenf Felder des Detailblocks sind nur lesbar — sie zeigen, was am markierten
        /// Satz steht.
        /// </para>
        /// <para>
        /// <b>Die Werte der Zuordnung stehen NICHT hier</b>, sondern unter
        /// <see cref="KiMaskennamen.GEBAEUDE_WOHNFLAECHE"/>: Wohnflaeche,
        /// Jahresnutzungsgrad, Art der Angabe und die dezentrale Warmwasserbereitung
        /// pflegt der Knopf „Aendern…" in jener Maske. Sie hier zu setzen hiesse, eine
        /// Zahl zu aendern, die der Anwender auf dieser Maske nicht nachlesen kann.
        /// </para>
        /// </remarks>
        private static KiDialog Gebaeude()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE,
                anzeigename: KiDialogTexte.MaskeGebaeude,
                felder: new[]
                {
                    // ---- Der Filter ueber die Katalogliste: drei Trichter, eine Suche --
                    new KiDialogFeld("verwendung", "GebaeudeKiSicht.Verwendung",
                                     KiDialogTexte.GebVerwendungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebVerwendungErl, leerErlaubt: true),
                    new KiDialogFeld("filter_gebaeudeart", "GebaeudeKiSicht.FilterGebaeudeart",
                                     KiDialogTexte.GebFilterArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebFilterArtErl, leerErlaubt: true),
                    new KiDialogFeld("filter_baujahr", "GebaeudeKiSicht.FilterBaujahr",
                                     KiDialogTexte.GebFilterBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebFilterBaujahrErl, leerErlaubt: true),
                    new KiDialogFeld("suche", "GebaeudeKiSicht.Suche",
                                     KiDialogTexte.GebSucheName, KiParameterTyp.Text,
                                     KiDialogTexte.GebSucheErl, leerErlaubt: true),

                    // ---- Der Detailblock des markierten Satzes ----------------------
                    new KiDialogFeld("name", "GebaeudeKiSicht.Name",
                                     KiDialogTexte.GebNameName, KiParameterTyp.Text,
                                     KiDialogTexte.GebNameErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gebaeudeart", "GebaeudeKiSicht.Gebaeudeart",
                                     KiDialogTexte.GebArtName, KiParameterTyp.Text,
                                     KiDialogTexte.GebArtErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beschreibung", "GebaeudeKiSicht.Beschreibung",
                                     KiDialogTexte.GebBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GebBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("wohnflaeche", "GebaeudeKiSicht.Wohnflaeche",
                                     KiDialogTexte.GebWohnflaecheName, KiParameterTyp.Text,
                                     KiDialogTexte.GebWohnflaecheErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("angabeart", "GebaeudeKiSicht.Angabeart",
                                     KiDialogTexte.GebAngabeartName, KiParameterTyp.Text,
                                     KiDialogTexte.GebAngabeartErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_GebWohnflaeche  ->  GebaeudeWohnflaecheDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Wohn-/Nutzflaechenangabe eines Projektgebaeudes — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeWohnflaecheKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Hier stehen die vier Werte der ZUORDNUNG</b>, die
        /// <see cref="KiMaskennamen.GEBAEUDE"/> nur anzeigt: die Bedarfsart, die Zahl
        /// dazu, der Jahresnutzungsgrad des Kessels und die dezentrale
        /// Warmwasserbereitung. Die fuenf Kopfangaben sind nur lesbar — sie kommen aus
        /// dem Katalogsatz und werden dort gepflegt.
        /// </para>
        /// <para>
        /// <b>Die BEDARFSART ist ein WAHLFELD</b> (KI-D-Q6) und traegt als Schluessel
        /// ihren Listenplatz. Sie entscheidet die Einheit der Zahl daneben und den
        /// Rechenweg: Wohnflaeche oder Ruecktrechnung aus einem Verbrauch.
        /// </para>
        /// <para>
        /// <b>Die Maske SCHREIBT nicht, sie entscheidet</b> — der einzige Weg zum Wirt
        /// ist OK, und OK schliesst sie; <c>dialog_speichern</c> lehnt deshalb benannt ab.
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeWohnflaeche()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_WOHNFLAECHE,
                anzeigename: KiDialogTexte.MaskeGebaeudeWohnflaeche,
                felder: new[]
                {
                    new KiDialogFeld("bedarfsart", "GebaeudeWohnflaecheKiSicht.Bedarfsart",
                                     KiDialogTexte.GebwBedarfsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebwBedarfsartErl, leerErlaubt: true),
                    new KiDialogFeld("wert", "GebaeudeWohnflaecheKiSicht.Wert",
                                     KiDialogTexte.GebwWertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebwWertErl),
                    new KiDialogFeld("jahresnutzungsgrad",
                                     "GebaeudeWohnflaecheKiSicht.Jahresnutzungsgrad",
                                     KiDialogTexte.GebwNutzungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebwNutzungsgradErl),
                    new KiDialogFeld("dezentral_warmwasser",
                                     "GebaeudeWohnflaecheKiSicht.DezentralWarmwasser",
                                     KiDialogTexte.GebwDezentralName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebwDezentralErl),

                    new KiDialogFeld("gebaeudename", "GebaeudeWohnflaecheKiSicht.Gebaeudename",
                                     KiDialogTexte.GebwNameName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwNameErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gebaeudeart", "GebaeudeWohnflaecheKiSicht.Gebaeudeart",
                                     KiDialogTexte.GebwArtName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwArtErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beschreibung", "GebaeudeWohnflaecheKiSicht.Beschreibung",
                                     KiDialogTexte.GebwBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("baujahr", "GebaeudeWohnflaecheKiSicht.Baujahr",
                                     KiDialogTexte.GebwBaujahrName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwBaujahrErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("angabeart", "GebaeudeWohnflaecheKiSicht.Angabeart",
                                     KiDialogTexte.GebwAngabeartName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwAngabeartErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Gebaeude1  ->  GebaeudeKatalogDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Der Gebaeude-Katalogeditor — 59 Deklarationen (darunter die vierzehn
        /// Modellparameter VDI 6007 der Stufen G1 und G2 samt Rechenweg, die drei der
        /// Kuehlung, die Randbedingung der Bodenplatte und die Ferien als vier Spalten) aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogKiSicht</c>: die groesste Maske des
        /// Katalogs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Zwei Reiterblaetter, ein Feldsatz.</b> Kenngroessen, Flaechen und U-Werte
        /// binden unmittelbar an den Satz; Raumtemperaturen, Waermebruecken,
        /// Anschlussmasse und Luftwechsel fuehrt die Maske in einem EIGENEN Stand und
        /// gibt sie erst mit „Werte uebernehmen" in den Satz. Die Sichtklasse legt beide
        /// unter einem Namen zusammen — dieselbe Bauart wie bei der
        /// Komponentenkonfiguration.
        /// </para>
        /// <para>
        /// <b>Fuenf Klapplisten sind WAHLFELDER</b> (KI-D-Q6): Gebaeudetyp und
        /// Gebaeudeart tragen den Namen des Katalogsatzes als Schluessel, Baualtersklasse
        /// und Bauart ihren Listenplatz, die VERWENDUNG ihren Steuerwert. Die Bauart zieht
        /// die Bauweise nach — derselbe Weg, den die Klappliste geht. Das BAUJAHR daneben
        /// ist eine Ganzzahl (1500 bis 2100, leer = unbekannt).
        /// </para>
        /// <para>
        /// <b>Die sechzehn FERIENZAHLEN sind eine TABELLE</b> (Welle #458 Stufe 3b): vier
        /// Zeitraeume mit Namen (Winter, Ostern, Sommer, Herbst) und je Beginn und Ende,
        /// Tag und Monat. Das ist die Spaltenform mit dem Zeitraum als Zeilenkennzeichen
        /// (<c>ferien_beginn_tag_3</c> heisst „… (Sommer)"), keine Zahlenreihe: Die Zeilen
        /// tragen Namen, und ein Zeitraum ist mit <c>formular_ausfuellen</c> in einem
        /// Block gesetzt. Die Grenzen sind die der Eingabefelder (Tag 1 bis 31, Monat 1
        /// bis 12); die vier Regeln ueber alle acht Paare prueft der Dialog (Haken
        /// „Pruefen").
        /// </para>
        /// <para>
        /// <b>Das HUELL-RASTER</b> (Bauteil, Kennwert, Groesse, Randbedingung) ist kein
        /// eigenes Raster fuer den Assistenten: Kennwert und Groesse jeder Zeile binden an
        /// die Felder <c>u_*</c>, <c>flaeche_*</c>, <c>wbvk_*</c> und <c>anschluss_*</c>
        /// dieser Liste - eine Spalte daneben waere derselbe Wert unter zweitem Namen. Die
        /// Groesse der Fensterzeile wird gerechnet (Nord + Sued + Ost + West). Neu ist die
        /// RANDBEDINGUNG der Bodenplatte, ein Wahlfeld (Erdreich, Keller, Aussenluft) auf
        /// dem Weg der Klappliste.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben</b> die BAUWEISE, die aus Bauart und Wohnflaeche gerechnet
        /// wird, und die Liste der Brauchwasserprofile, die eine eigene Maske pflegt.
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeKatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_KATALOG,
                anzeigename: KiDialogTexte.MaskeGebaeudeKatalog,
                felder: GebaeudeKatalogFelder(verwaltung: false),
                knoepfe: new[]
                {
                    new KiDialogKnopf("werte_uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfWerteUebernehmen),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("speichern", "btn_Speichern",
                                      KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_Gebaeude_Admin  ->  GebaeudeAdminDialog   (Welle #465)
        // =====================================================================

        /// <summary>
        /// Die GEBAEUDEVERWALTUNG — das Wahlfeld <c>satz</c> und die Felder des
        /// Katalogeditors aus DERSELBEN Liste (<see cref="GebaeudeKatalogFelder"/>) an
        /// derselben Sichtklasse (<c>GebaeudeKatalogKiSicht</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Wahrheit, keine zweite Feldliste.</b> Das Stammblatt der Verwaltung fuehrt
        /// jedes Feld, das der Katalogeditor bearbeitet, auf demselben Arbeitsstand
        /// (<c>GebaeudeArbeitsstand</c>) mit derselben Pruefung und demselben Schreibweg; die
        /// Feldkarte ist deshalb dieselbe. Zwei Unterschiede: Der NAME ist hier nur lesbar
        /// (umbenannt wird ueber „Duplizieren…", einen anderen Satz waehlt <c>satz</c>), und
        /// die BETRIEBSART des Editors gibt es in der Verwaltung nicht.
        /// </para>
        /// <para>
        /// <b>Der Satz ist die Wahl der Liste</b> — dasselbe Muster wie die
        /// Erzeugerverwaltungen (<see cref="ErzeugerVerwaltung"/>): SATZWAHL, frei auch aus
        /// einem Auslieferungssatz heraus; ein Auslieferungssatz ist schreibgeschuetzt, die
        /// Absage nennt „Duplizieren…" und „Schloss aufheben…".
        /// </para>
        /// <para>
        /// <b>Nicht ueber den Assistenten</b> gehen „Neu…", „Duplizieren…", „Schloss
        /// aufheben…" und „Loeschen" — sie legen Saetze an, nehmen sie weg oder heben einen
        /// Schutz auf (KI-D-Q11, AD-Q15).
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeVerwaltung()
        {
            var felder = new List<KiDialogFeld>
            {
                new KiDialogFeld("satz", "GebaeudeKatalogKiSicht.Satz",
                                 KiDialogTexte.KbrowSatzName, KiParameterTyp.Wahl,
                                 KiDialogTexte.KbrowSatzErl, satzwahl: true)
            };
            felder.AddRange(GebaeudeKatalogFelder(verwaltung: true));

            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_ADMIN,
                anzeigename: KiDialogTexte.MaskeGebaeudeAdmin,
                felder: felder,
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("verwerfen", "btn_Verwerfen", KiDialogTexte.KnopfVerwerfen),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        /// <summary>
        /// <b>Die Felder eines Gebaeude-Katalogsatzes</b> — die EINE Liste fuer den
        /// Katalogeditor (<see cref="KiMaskennamen.GEBAEUDE_KATALOG"/>) und die Verwaltung
        /// (<see cref="KiMaskennamen.GEBAEUDE_ADMIN"/>).
        /// </summary>
        /// <param name="verwaltung">
        /// <c>true</c>: der Name nur lesbar, ohne Betriebsart (siehe
        /// <see cref="GebaeudeVerwaltung"/>).
        /// </param>
        private static List<KiDialogFeld> GebaeudeKatalogFelder(bool verwaltung)
        {
            var felder = new List<KiDialogFeld>
                {
                    // ---- Kenngroessen ----------------------------------------------
                    verwaltung
                        ? new KiDialogFeld("name", "GebaeudeKatalogKiSicht.Name",
                                           KiDialogTexte.GebkNameName, KiParameterTyp.Text,
                                           KiDialogTexte.GebaNameErl, leerErlaubt: true, nurLesen: true)
                        : new KiDialogFeld("name", "GebaeudeKatalogKiSicht.Name",
                                           KiDialogTexte.GebkNameName, KiParameterTyp.Text,
                                           KiDialogTexte.GebkNameErl),
                    new KiDialogFeld("gebaeudetyp", "GebaeudeKatalogKiSicht.Typ",
                                     KiDialogTexte.GebkTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkTypErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "GebaeudeKatalogKiSicht.Beschreibung",
                                     KiDialogTexte.GebkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GebkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("gebaeudeart", "GebaeudeKatalogKiSicht.Gebaeudeart",
                                     KiDialogTexte.GebkArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkArtErl, leerErlaubt: true),
                    new KiDialogFeld("baualtersklasse", "GebaeudeKatalogKiSicht.Baualtersklasse",
                                     KiDialogTexte.GebkBaualtersklasseName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkBaualtersklasseErl),
                    // Das Baujahr (G4a): eine Jahreszahl neben der Klasse, leer = unbekannt; ist es
                    // gesetzt, FUEHRT es - die Klasse folgt ihm (E47, F2).
                    new KiDialogFeld("baujahr", "GebaeudeKatalogKiSicht.Baujahr",
                                     KiDialogTexte.GebkBaujahrName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkBaujahrErl, leerErlaubt: true,
                                     min: GebaeudeSchema.BAUJAHR_MIN, max: GebaeudeSchema.BAUJAHR_MAX),
                    new KiDialogFeld("verwendung", "GebaeudeKatalogKiSicht.Verwendung",
                                     KiDialogTexte.GebkVerwendungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkVerwendungErl, leerErlaubt: true),
                    // Der Energiestandard (E47, F3): der Code als Schluessel, leer = keiner; die
                    // Wahlliste folgt der Verwendung (Effizienzhaus 115/100 und 85 nur Wohnen).
                    new KiDialogFeld("energiestandard", "GebaeudeKatalogKiSicht.Energiestandard",
                                     KiDialogTexte.GebkEnergiestandardName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkEnergiestandardErl, leerErlaubt: true),
                    new KiDialogFeld("bauart", "GebaeudeKatalogKiSicht.Bauart",
                                     KiDialogTexte.GebkBauartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkBauartErl),
                    new KiDialogFeld("wohnflaeche", "GebaeudeKatalogKiSicht.WohnflaecheGesamt",
                                     KiDialogTexte.GebkWohnflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWohnflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("flaeche_nutzer", "GebaeudeKatalogKiSicht.FlaecheNutzer",
                                     KiDialogTexte.GebkFlaecheNutzerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFlaecheNutzerErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("waermegewinne", "GebaeudeKatalogKiSicht.Waermegewinne",
                                     KiDialogTexte.GebkWaermegewinneName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWaermegewinneErl,
                                     einheit: KiDialogTexte.EINHEIT_W),
                    new KiDialogFeld("fensterdurchlassgrad",
                                     "GebaeudeKatalogKiSicht.Fensterdurchlassgrad",
                                     KiDialogTexte.GebkDurchlassgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkDurchlassgradErl),
                    new KiDialogFeld("raumhoehe", "GebaeudeKatalogKiSicht.Raumhoehe",
                                     KiDialogTexte.GebkRaumhoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkRaumhoeheErl,
                                     einheit: KiDialogTexte.EINHEIT_METER),

                    // ---- Flaechen ---------------------------------------------------
                    new KiDialogFeld("fensterflaeche_nord",
                                     "GebaeudeKatalogKiSicht.FensterflaecheNord",
                                     KiDialogTexte.GebkFfNordName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFfNordErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("fensterflaeche_sued",
                                     "GebaeudeKatalogKiSicht.FensterflaecheSued",
                                     KiDialogTexte.GebkFfSuedName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFfSuedErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("fensterflaeche_ostwest",
                                     "GebaeudeKatalogKiSicht.FensterflaecheOstWest",
                                     KiDialogTexte.GebkFfOstWestName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFfOstWestErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("flaeche_aussenwand",
                                     "GebaeudeKatalogKiSicht.FlaecheAussenwand",
                                     KiDialogTexte.GebkAussenwandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAussenwandErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("dachflaeche", "GebaeudeKatalogKiSicht.Dachflaeche",
                                     KiDialogTexte.GebkDachflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkDachflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("grundflaeche", "GebaeudeKatalogKiSicht.Grundflaeche",
                                     KiDialogTexte.GebkGrundflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkGrundflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("sonstige_flaechen",
                                     "GebaeudeKatalogKiSicht.SonstigeFlaechen",
                                     KiDialogTexte.GebkSonstFlaechenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkSonstFlaechenErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),

                    // ---- U-Werte ----------------------------------------------------
                    new KiDialogFeld("u_aussenwand", "GebaeudeKatalogKiSicht.UWertAussenwand",
                                     KiDialogTexte.GebkUAussenwandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUAussenwandErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_fenster", "GebaeudeKatalogKiSicht.UWertFenster",
                                     KiDialogTexte.GebkUFensterName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUFensterErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_dachflaeche", "GebaeudeKatalogKiSicht.UWertDachflaeche",
                                     KiDialogTexte.GebkUDachName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUDachErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_grundflaeche", "GebaeudeKatalogKiSicht.UWertGrundflaeche",
                                     KiDialogTexte.GebkUGrundName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUGrundErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_sonstiges", "GebaeudeKatalogKiSicht.UWertSonstiges",
                                     KiDialogTexte.GebkUSonstigesName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUSonstigesErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),

                    // ---- Raumtemperaturen (zweites Reiterblatt) ---------------------
                    new KiDialogFeld("soll_tag", "GebaeudeKatalogKiSicht.SollTag",
                                     KiDialogTexte.GebkSollTagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkSollTagErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("nachtabsenkung", "GebaeudeKatalogKiSicht.Nachtabsenkung",
                                     KiDialogTexte.GebkNachtName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkNachtErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    // Die Nachtzeit (E43): volle Stunden 0 bis 23, beide leer = Vorgabe 22 bis 6 Uhr;
                    // nur eines oder beide gleich haelt die Pruefung des Arbeitsstands an.
                    new KiDialogFeld("nacht_beginn", "GebaeudeKatalogKiSicht.NachtBeginn",
                                     KiDialogTexte.GebkNachtBeginnName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkNachtBeginnErl, leerErlaubt: true,
                                     einheit: KiDialogTexte.EINHEIT_STUNDE,
                                     min: Nachtzeit.STUNDE_MIN, max: Nachtzeit.STUNDE_MAX),
                    new KiDialogFeld("nacht_ende", "GebaeudeKatalogKiSicht.NachtEnde",
                                     KiDialogTexte.GebkNachtEndeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkNachtEndeErl, leerErlaubt: true,
                                     einheit: KiDialogTexte.EINHEIT_STUNDE,
                                     min: Nachtzeit.STUNDE_MIN, max: Nachtzeit.STUNDE_MAX),
                    new KiDialogFeld("max_temperatur", "GebaeudeKatalogKiSicht.MaxTemperatur",
                                     KiDialogTexte.GebkMaxTemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkMaxTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("wochenendabsenkung",
                                     "GebaeudeKatalogKiSicht.Wochenendabsenkung",
                                     KiDialogTexte.GebkWochenendeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWochenendeErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("soll_ferien", "GebaeudeKatalogKiSicht.SollFerien",
                                     KiDialogTexte.GebkSollFerienName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkSollFerienErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Waermebruecken und Anschlussmasse --------------------------
                    new KiDialogFeld("wbvk_fenster_wand",
                                     "GebaeudeKatalogKiSicht.WbvkFensterWand",
                                     KiDialogTexte.GebkFensterWandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFensterWandErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("wbvk_wand_dach", "GebaeudeKatalogKiSicht.WbvkWandDach",
                                     KiDialogTexte.GebkWandDachName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWandDachErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("wbvk_aussenwand_keller",
                                     "GebaeudeKatalogKiSicht.WbvkAussenwandKeller",
                                     KiDialogTexte.GebkWandKellerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWandKellerErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("anschluss_fenster_wand",
                                     "GebaeudeKatalogKiSicht.AnschlussFensterWand",
                                     KiDialogTexte.GebkAnschlussFensterName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAnschlussFensterErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("anschluss_wand_dach",
                                     "GebaeudeKatalogKiSicht.AnschlussWandDach",
                                     KiDialogTexte.GebkAnschlussDachName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAnschlussDachErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("anschluss_aussenwand_keller",
                                     "GebaeudeKatalogKiSicht.AnschlussAussenwandKeller",
                                     KiDialogTexte.GebkAnschlussKellerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAnschlussKellerErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("luftwechselrate", "GebaeudeKatalogKiSicht.Luftwechselrate",
                                     KiDialogTexte.GebkLuftwechselName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkLuftwechselErl,
                                     einheit: KiDialogTexte.EINHEIT_1_H, leerErlaubt: true),

                    // ---- Modellparameter VDI 6007 (Stufen G1 und G2) -------------------
                    new KiDialogFeld("rahmenanteil", "GebaeudeKatalogKiSicht.Rahmenanteil",
                                     KiDialogTexte.GebkRahmenanteilName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkRahmenanteilErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("verschattungsfaktor", "GebaeudeKatalogKiSicht.Verschattungsfaktor",
                                     KiDialogTexte.GebkVerschattungsfaktorName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkVerschattungsfaktorErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("masseanteil_aussen", "GebaeudeKatalogKiSicht.MasseanteilAussen",
                                     KiDialogTexte.GebkMasseanteilAussenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkMasseanteilAussenErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("innenflaechenfaktor", "GebaeudeKatalogKiSicht.Innenflaechenfaktor",
                                     KiDialogTexte.GebkInnenflaechenfaktorName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkInnenflaechenfaktorErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("heizung_strahlungsanteil", "GebaeudeKatalogKiSicht.HeizungStrahlungsanteil",
                                     KiDialogTexte.GebkHeizungStrahlungsanteilName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkHeizungStrahlungsanteilErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("heizleistung_max", "GebaeudeKatalogKiSicht.HeizleistungMax",
                                     KiDialogTexte.GebkHeizleistungMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkHeizleistungMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("aussenbauteile_strahlung", "GebaeudeKatalogKiSicht.AussenbauteileStrahlung",
                                     KiDialogTexte.GebkAussenbauteileStrahlungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebkAussenbauteileStrahlungErl),
                    new KiDialogFeld("luftwechsel_infiltration", "GebaeudeKatalogKiSicht.LuftwechselInfiltration",
                                     KiDialogTexte.GebkLuftwechselInfiltrationName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkLuftwechselInfiltrationErl,
                                     einheit: KiDialogTexte.EINHEIT_1_H, leerErlaubt: true),
                    new KiDialogFeld("luftwechsel_nutzer", "GebaeudeKatalogKiSicht.LuftwechselNutzer",
                                     KiDialogTexte.GebkLuftwechselNutzerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkLuftwechselNutzerErl,
                                     einheit: KiDialogTexte.EINHEIT_1_H, leerErlaubt: true),
                    new KiDialogFeld("sommerlueftung", "GebaeudeKatalogKiSicht.Sommerlueftung",
                                     KiDialogTexte.GebkSommerlueftungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebkSommerlueftungErl),
                    new KiDialogFeld("fensterflaeche_ost", "GebaeudeKatalogKiSicht.FensterflaecheOst",
                                     KiDialogTexte.GebkFensterflaecheOstName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFensterflaecheOstErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true),
                    new KiDialogFeld("fensterflaeche_west", "GebaeudeKatalogKiSicht.FensterflaecheWest",
                                     KiDialogTexte.GebkFensterflaecheWestName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFensterflaecheWestErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true),
                    new KiDialogFeld("kellertemperatur", "GebaeudeKatalogKiSicht.Kellertemperatur",
                                     KiDialogTexte.GebkKellertemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKellertemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    // ---- Huell-Raster: die Randbedingung der Bodenplatte (Welle #458 3b) --
                    new KiDialogFeld("randbedingung", "GebaeudeKatalogKiSicht.Randbedingung",
                                     KiDialogTexte.GebkRandbedingungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkRandbedingungErl),
                    // ---- Ferien: vier Zeitraeume als Spalten (Welle #458 Stufe 3b) ---------
                    new KiDialogFeld("ferien_beginn_tag", "GebaeudeKatalogKiSicht.Ferien[].BeginnTag",
                                     KiDialogTexte.GebkFerienBeginnTagName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkFerienBeginnTagErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Zeitraum", min: 1, max: 31),
                    new KiDialogFeld("ferien_beginn_monat", "GebaeudeKatalogKiSicht.Ferien[].BeginnMonat",
                                     KiDialogTexte.GebkFerienBeginnMonatName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkFerienBeginnMonatErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Zeitraum", min: 1, max: 12),
                    new KiDialogFeld("ferien_ende_tag", "GebaeudeKatalogKiSicht.Ferien[].EndeTag",
                                     KiDialogTexte.GebkFerienEndeTagName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkFerienEndeTagErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Zeitraum", min: 1, max: 31),
                    new KiDialogFeld("ferien_ende_monat", "GebaeudeKatalogKiSicht.Ferien[].EndeMonat",
                                     KiDialogTexte.GebkFerienEndeMonatName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.GebkFerienEndeMonatErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Zeitraum", min: 1, max: 12),
                    // ---- Kuehlung (Stufe KU1, Kuehlkonzept 8.1) ----------------------------
                    new KiDialogFeld("kuehlung_aktiv", "GebaeudeKatalogKiSicht.KuehlungAktiv",
                                     KiDialogTexte.GebkKuehlungAktivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebkKuehlungAktivErl),
                    new KiDialogFeld("kuehl_sollwert", "GebaeudeKatalogKiSicht.KuehlSollwert",
                                     KiDialogTexte.GebkKuehlSollwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlSollwertErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("kuehlleistung_max", "GebaeudeKatalogKiSicht.KuehlleistungMax",
                                     KiDialogTexte.GebkKuehlleistungMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlleistungMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    // ---- Kuehluebergabe (E37, Anlagenkopplung 8.1) - Unterabschnitt der Kuehlung.
                    //      Schalter und Art ueber die Wege des Arbeitsstands, die Zahlen leer = NULL.
                    new KiDialogFeld("kuehluebergabe_aktiv", "GebaeudeKatalogKiSicht.KuehluebergabeAktiv",
                                     KiDialogTexte.GebkKuehluebergabeAktivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebkKuehluebergabeAktivErl),
                    new KiDialogFeld("kuehl_uebergabe_art", "GebaeudeKatalogKiSicht.KuehlUebergabeArt",
                                     KiDialogTexte.GebkKuehlUebergabeArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkKuehlUebergabeArtErl),
                    new KiDialogFeld("kuehl_uebergabe_exponent", "GebaeudeKatalogKiSicht.KuehlUebergabeExponent",
                                     KiDialogTexte.GebkKuehlUebergabeExponentName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlUebergabeExponentErl, leerErlaubt: true, min: 1.0, max: 1.6),
                    new KiDialogFeld("kuehl_uebergabe_nennleistung", "GebaeudeKatalogKiSicht.KuehlUebergabeNennleistung",
                                     KiDialogTexte.GebkKuehlUebergabeNennleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlUebergabeNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kuehl_auslegung_vorlauf", "GebaeudeKatalogKiSicht.KuehlAuslegungVorlauf",
                                     KiDialogTexte.GebkKuehlAuslegungVorlaufName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlAuslegungVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true, min: 4, max: 22),
                    new KiDialogFeld("kuehl_auslegung_ruecklauf", "GebaeudeKatalogKiSicht.KuehlAuslegungRuecklauf",
                                     KiDialogTexte.GebkKuehlAuslegungRuecklaufName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlAuslegungRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("kuehl_auslegung_raum", "GebaeudeKatalogKiSicht.KuehlAuslegungRaumtemperatur",
                                     KiDialogTexte.GebkKuehlAuslegungRaumName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlAuslegungRaumErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true, min: 20, max: 30),
                    new KiDialogFeld("kuehl_vorlaufgrenze", "GebaeudeKatalogKiSicht.KuehlVorlaufgrenze",
                                     KiDialogTexte.GebkKuehlVorlaufgrenzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkKuehlVorlaufgrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true, min: 4, max: 22),
                    // ---- Waermeuebergabe (Stufe AK1, Anlagenkopplung 9.1, 9.2) ------------
                    //
                    // Schalter, Art und Zeitprogramm gehen ueber die Wege des Arbeitsstands
                    // (Vorschlag der Heizkurve, „ideal" haelt NULL, strenger Leser des
                    // Zeitprogramms); die Zahlen schreiben leer NULL - es gilt die Vorgabe.
                    new KiDialogFeld("heizkreis_aktiv", "GebaeudeKatalogKiSicht.HeizkreisAktiv",
                                     KiDialogTexte.GebkHeizkreisAktivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebkHeizkreisAktivErl),
                    new KiDialogFeld("uebergabe_art", "GebaeudeKatalogKiSicht.UebergabeArt",
                                     KiDialogTexte.GebkUebergabeArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkUebergabeArtErl),
                    new KiDialogFeld("uebergabe_exponent", "GebaeudeKatalogKiSicht.UebergabeExponent",
                                     KiDialogTexte.GebkUebergabeExponentName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUebergabeExponentErl, leerErlaubt: true, min: 1.0, max: 1.6),
                    new KiDialogFeld("uebergabe_nennleistung", "GebaeudeKatalogKiSicht.UebergabeNennleistung",
                                     KiDialogTexte.GebkUebergabeNennleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUebergabeNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("auslegung_vorlauf", "GebaeudeKatalogKiSicht.AuslegungVorlauf",
                                     KiDialogTexte.GebkAuslegungVorlaufName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAuslegungVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true, min: 25, max: 90),
                    new KiDialogFeld("auslegung_ruecklauf", "GebaeudeKatalogKiSicht.AuslegungRuecklauf",
                                     KiDialogTexte.GebkAuslegungRuecklaufName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAuslegungRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("auslegung_raum", "GebaeudeKatalogKiSicht.AuslegungRaumtemperatur",
                                     KiDialogTexte.GebkAuslegungRaumName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAuslegungRaumErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true, min: 15, max: 26),
                    new KiDialogFeld("auslegung_aussen", "GebaeudeKatalogKiSicht.AuslegungAussentemperatur",
                                     KiDialogTexte.GebkAuslegungAussenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAuslegungAussenErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true, min: -30, max: 5),
                    new KiDialogFeld("heizkurve_aktiv", "GebaeudeKatalogKiSicht.HeizkurveAktiv",
                                     KiDialogTexte.GebkHeizkurveAktivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebkHeizkurveAktivErl),
                    new KiDialogFeld("heizkurve_niveau", "GebaeudeKatalogKiSicht.HeizkurveNiveau",
                                     KiDialogTexte.GebkHeizkurveNiveauName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkHeizkurveNiveauErl,
                                     einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true, min: -10, max: 10),
                    new KiDialogFeld("heizkurve_steilheit", "GebaeudeKatalogKiSicht.HeizkurveSteilheit",
                                     KiDialogTexte.GebkHeizkurveSteilheitName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkHeizkurveSteilheitErl, leerErlaubt: true, min: 0.2, max: 3),
                    new KiDialogFeld("proportionalband", "GebaeudeKatalogKiSicht.Proportionalband",
                                     KiDialogTexte.GebkProportionalbandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkProportionalbandErl,
                                     einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true, min: 0, max: 5),
                    new KiDialogFeld("sollwertprofil", "GebaeudeKatalogKiSicht.Sollwertprofil",
                                     KiDialogTexte.GebkSollwertprofilName, KiParameterTyp.Text,
                                     KiDialogTexte.GebkSollwertprofilErl, leerErlaubt: true),
                    new KiDialogFeld("rechenweg", "GebaeudeKatalogKiSicht.Rechenweg",
                                     KiDialogTexte.GebkRechenwegName, KiParameterTyp.Text,
                                     KiDialogTexte.GebkRechenwegErl, nurLesen: true)
                };

            if (!verwaltung)
            {
                felder.Add(new KiDialogFeld("betriebsart", "GebaeudeKatalogKiSicht.Betriebsart",
                                            KiDialogTexte.GebkBetriebsartName, KiParameterTyp.Text,
                                            KiDialogTexte.GebkBetriebsartErl, nurLesen: true));

                // ---- Zonen eines Gebaeudes im Projekt (Stufe G6a): ein RASTER zum LESEN, Kennzeichen
                //      die Nummer ab 1. Anlegen, Oeffnen, Duplizieren, Umordnen und Entfernen bleiben
                //      Klicks des Anwenders; die Werte einer Zone setzt der Assistent im Zonendialog.
                const string ZONE = "GebaeudeKatalogKiSicht.Zonen[].";
                felder.Add(new KiDialogFeld("zone_name", ZONE + "Name", KiDialogTexte.GebzZoneName, KiParameterTyp.Text,
                                            KiDialogTexte.GebzZoneNameErl, leerErlaubt: true, zeilenkennzeichen: "Nummer", nurLesen: true));
                felder.Add(new KiDialogFeld("zone_nutzflaeche", ZONE + "Nutzflaeche", KiDialogTexte.GebzZoneNutzflaecheName,
                                            KiParameterTyp.Zahl, KiDialogTexte.GebzZoneNutzflaecheErl, einheit: KiDialogTexte.EINHEIT_M2,
                                            leerErlaubt: true, zeilenkennzeichen: "Nummer", nurLesen: true));
                felder.Add(new KiDialogFeld("zone_ht", ZONE + "HT", KiDialogTexte.GebzZoneHTName, KiParameterTyp.Zahl,
                                            KiDialogTexte.GebzZoneHTErl, einheit: KiDialogTexte.EINHEIT_W_K,
                                            leerErlaubt: true, zeilenkennzeichen: "Nummer", nurLesen: true));
                felder.Add(new KiDialogFeld("zone_bauteile", ZONE + "Bauteile", KiDialogTexte.GebzZoneBauteileName,
                                            KiParameterTyp.Ganzzahl, KiDialogTexte.GebzZoneBauteileErl,
                                            leerErlaubt: true, zeilenkennzeichen: "Nummer", nurLesen: true));
            }
            return felder;
        }

        // =====================================================================
        // KomponentenKonfiguration  ->  KomponentenKonfigurationDialog (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Konfiguration EINER Komponente der Simulation — sechzehn Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.KomponentenKonfigurationKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die dritte Maske ohne <c>Form_</c>-Vorsilbe:</b> Sie hatte nie eine
        /// WinForms-Fassung. Bis zum 16.09.2026 standen ihre Felder offen an den
        /// Erzeugerkarten; seither stehen sie hinter einem Knopf je Karte.
        /// </para>
        /// <para>
        /// <b>EINE Maske, ZWEI Arbeitskopien — und deshalb ein Sichtmodell.</b> Beim
        /// Heizkessel und beim BHKW zeigt sie die projektweiten Laufparameter
        /// (<c>Tab_Einstellungen</c>), bei der Waermepumpe die Konfiguration DIESER
        /// Anlage. Der Katalog nennt EIN Daten-Objekt vor dem Punkt; die Sichtklasse
        /// legt beide Staende unter einem Namen zusammen. Hinzu kommt, dass
        /// <c>ParameterDaten</c> oeffentliche FELDER traegt und keine Eigenschaften —
        /// die Maskenbruecke loest ueber <c>GetProperty</c> auf und faende dort nichts.
        /// </para>
        /// <para>
        /// <b>Die dreizehn Felder der Waermepumpe stehen ZWEIMAL im Katalog</b> — hier
        /// und unter <see cref="KiMaskennamen.WAERMEPUMPE_ANLAGE"/>. Das ist richtig:
        /// Eine Maske ist, was offen ist, und der Anwender sieht dieselben Werte
        /// einmal im Anlagendialog und einmal in dieser Konfiguration. Welche Maske
        /// gerade gilt, sagt die Anmeldung und nicht der Katalog.
        /// </para>
        /// <para>
        /// <b>Die drei Klapplisten sind WAHLFELDER</b> (KI-F1b, KI-D-Q6): die
        /// BHKW-Betriebsart (Steuerwert 0/1/2 des Bestands), die Betriebsart der
        /// Waermepumpe (Steuerwert als Schluessel UND Text) und der ENERGIETRAEGER
        /// (<c>CarrierId</c>, Schluessel ist die Id des Katalogsatzes). Ihre Eintraege
        /// liefert die Maske zur Laufzeit — der Traegerkatalog haengt am Gewerk.
        /// </para>
        /// </remarks>
        private static KiDialog Komponentenkonfiguration()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOMPONENTENKONFIGURATION,
                anzeigename: KiDialogTexte.MaskeKomponentenkonfiguration,
                felder: new[]
                {
                    // ---- Die projektweiten Laufparameter ----------------------------
                    new KiDialogFeld("kessel_bereitschaft",
                                     "KomponentenKonfigurationKiSicht.Bereitschaft",
                                     KiDialogTexte.KkonfBereitschaftName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KkonfBereitschaftErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A),
                    new KiDialogFeld("bhkw_betriebsart",
                                     "KomponentenKonfigurationKiSicht.BhkwBetriebsart",
                                     KiDialogTexte.KkonfBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KkonfBetriebsartErl),
                    new KiDialogFeld("bhkw_leistungsgrenze",
                                     "KomponentenKonfigurationKiSicht.BhkwLeistungsgrenze",
                                     KiDialogTexte.KkonfGrenzeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.KkonfGrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),

                    // ---- Die Konfiguration DIESER Waermepumpe -----------------------
                    new KiDialogFeld("heizstab", "KomponentenKonfigurationKiSicht.Heizstab",
                                     KiDialogTexte.WpaHeizstabName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaHeizstabErl),
                    new KiDialogFeld("sperrzeit", "KomponentenKonfigurationKiSicht.Sperrung",
                                     KiDialogTexte.WpaSperrungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaSperrungErl),
                    new KiDialogFeld("sperrzeit_von",
                                     "KomponentenKonfigurationKiSicht.SperrzeitVon",
                                     KiDialogTexte.WpaSperrzeitVonName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitVonErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("sperrzeit_bis",
                                     "KomponentenKonfigurationKiSicht.SperrzeitBis",
                                     KiDialogTexte.WpaSperrzeitBisName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitBisErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("bivalenter_betrieb",
                                     "KomponentenKonfigurationKiSicht.BivalenterBetrieb",
                                     KiDialogTexte.WpaBivalentName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaBivalentErl),
                    new KiDialogFeld("betriebsart", "KomponentenKonfigurationKiSicht.Betriebsart",
                                     KiDialogTexte.WpaBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBetriebsartErl, leerErlaubt: true),
                    new KiDialogFeld("energietraeger",
                                     "KomponentenKonfigurationKiSicht.Energietraeger",
                                     KiDialogTexte.KkonfTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KkonfTraegerErl),
                    new KiDialogFeld("bivalenztemperatur",
                                     "KomponentenKonfigurationKiSicht.Abschaltpunkt",
                                     KiDialogTexte.WpaAbschaltpunktName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaAbschaltpunktErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Die Gruppe „Kuehlbetrieb" DIESER Waermepumpe (Stufe KU2 Welle 3)
                    //      - dieselben fuenf Felder und Wege wie unter Form_WP_Anlage.
                    new KiDialogFeld("kuehlbetrieb", "KomponentenKonfigurationKiSicht.Kuehlbetrieb",
                                     KiDialogTexte.WpaKuehlbetriebName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaKuehlbetriebErl),
                    new KiDialogFeld("kuehl_vorlauf", "KomponentenKonfigurationKiSicht.KuehlVorlauf",
                                     KiDialogTexte.WpaKuehlVorlaufName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaKuehlVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("hilfsstromanteil",
                                     "KomponentenKonfigurationKiSicht.KuehlHilfsstromanteil",
                                     KiDialogTexte.WpaHilfsstromName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaHilfsstromErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("kuehltraeger", "KomponentenKonfigurationKiSicht.KuehlCarrierId",
                                     KiDialogTexte.WpaKuehltraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaKuehltraegerErl, leerErlaubt: true),
                    new KiDialogFeld("kuehl_abrechnung", "KomponentenKonfigurationKiSicht.KuehlAbrechnung",
                                     KiDialogTexte.WpaAbrechnungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaAbrechnungErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Quellprofil  ->  QuellprofilDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Ein Quellprofil — der Kopf und die zwoelf Monatswerte, fuenf Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.QuellprofilKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die zwoelf MONATSWERTE sind EINE ZAHLENREIHE</b> (Welle #458 Stufe 3b,
        /// <c>monatswerte</c>, Januar bis Dezember, °C) - in der Betriebsart „Monat", in
        /// der die Maske sie zeigt. Steht eine andere Betriebsart, liest die Reihe leer
        /// und das Setzen lehnt benannt ab. Die 365 bzw. 8 760 Werte der Betriebsarten
        /// Tag und Stunde bleiben draussen: Sie sind Zeitreihen, gepflegt ueber „Alle
        /// Werte gleich setzen…" und den CSV-Weg, und keine Stelle traegt einen Namen,
        /// den der Anwender auf der Maske liest.
        /// </para>
        /// <para>
        /// <b>Das gewaehlte PROFIL ist ein WAHLFELD</b> (KI-F1b, KI-D-Q6): Seine
        /// Eintraege sind die Profile des Projekts samt dem Eintrag 0 „neues Profil" —
        /// auch er ist eine gueltige Wahl der Maske. Sie LAEDT den Profilkopf samt
        /// Werten; das ist derselbe Weg, den die Klappliste nimmt.
        /// </para>
        /// <para>
        /// <b>Die BETRIEBSART ist ein WAHLFELD</b> und traegt als Schluessel den
        /// Steuerwert des Bestands (Monat, Tag, Stunde), als Text den Eintrag der
        /// Klappliste. Sie entscheidet, wie viele Werte das Profil fuehrt.
        /// </para>
        /// </remarks>
        private static KiDialog Quellprofil()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.QUELLPROFIL,
                anzeigename: KiDialogTexte.MaskeQuellprofil,
                felder: new[]
                {
                    new KiDialogFeld("bezeichnung", "QuellprofilKiSicht.Bezeichnung",
                                     KiDialogTexte.QprofBezeichnungName, KiParameterTyp.Text,
                                     KiDialogTexte.QprofBezeichnungErl),
                    new KiDialogFeld("beschreibung", "QuellprofilKiSicht.Beschreibung",
                                     KiDialogTexte.QprofBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.QprofBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("betriebsart", "QuellprofilKiSicht.Betriebsart",
                                     KiDialogTexte.QprofBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QprofBetriebsartErl, leerErlaubt: true),
                    new KiDialogFeld("profil", "QuellprofilKiSicht.Profil",
                                     KiDialogTexte.QprofProfilName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QprofProfilErl, leerErlaubt: true),
                    new KiDialogFeld("monatswerte", "QuellprofilKiSicht.Monatswerte",
                                     KiDialogTexte.QprofMonatswerteName, KiParameterTyp.ZahlListe,
                                     KiDialogTexte.QprofMonatswerteErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C,
                                     reihe: KiZahlenreihen.Monate())
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Waermesenke  ->  WaermesenkeDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Waermesenken einer Anlage — neun Felder der GEWAEHLTEN Zeile aus
        /// <c>EPOS.UI.Dialoge.Simulation.WaermesenkeKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist eine ZEILE und kein Dialogstand</b> — dieselbe
        /// Bauart wie bei <see cref="Photovoltaik"/>: Der Dialog fuehrt eine Liste, und
        /// angemeldet ist, was in der gewaehlten Zeile steht. Ist keine gewaehlt, sind
        /// die Felder leer — derselbe Zustand, den der Anwender sieht.
        /// </para>
        /// <para>
        /// <b>Der gewaehlte SPEICHER ist ein WAHLFELD</b> (KI-F1b, KI-D-Q6): Seine
        /// Eintraege sind die Pufferspeicher des Projekts, die der Dialog zur Laufzeit
        /// liefert. Die gesperrten GRUPPENKOEPFE der Klappliste (negative Id) stehen
        /// NICHT darin — sie sind Ueberschriften und waren nie eine gueltige Wahl.
        /// </para>
        /// <para>
        /// <b>Der PARALLELVERBUND bleibt draussen</b> — eine MENGE von Verweisen; ein
        /// Katalogfeld traegt EINEN Wert. Der RANG der Zeile bleibt ebenfalls draussen:
        /// Er wird nicht eingegeben, sondern ueber „nach oben"/„nach unten" verschoben,
        /// und er traegt keine eigene Beschriftung.
        /// </para>
        /// <para>
        /// <b>„Senke hinzufuegen", „nach oben", „nach unten" und „Pufferspeicher
        /// anlegen…" sind keine Knoepfe dieser Liste</b> — sie aendern die LISTE und
        /// nicht einen Wert; „Entfernen" ist ueberdies ein Loeschknopf und damit nicht
        /// deklarierbar (Bauartsperre in <see cref="KiDialogKnopf"/>).
        /// </para>
        /// </remarks>
        private static KiDialog Waermesenke()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMESENKE,
                anzeigename: KiDialogTexte.MaskeWaermesenke,
                felder: new[]
                {
                    new KiDialogFeld("ziel", "WaermesenkeKiSicht.Ziel",
                                     KiDialogTexte.WsenZielName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenZielErl, leerErlaubt: true),
                    new KiDialogFeld("bedarfsart", "WaermesenkeKiSicht.Bedarfsart",
                                     KiDialogTexte.WsenBedarfsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenBedarfsartErl, leerErlaubt: true),
                    new KiDialogFeld("speicher", "WaermesenkeKiSicht.Speicher",
                                     KiDialogTexte.WsenSpeicherName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenSpeicherErl),
                    new KiDialogFeld("ladeprioritaet", "WaermesenkeKiSicht.Ladeprioritaet",
                                     KiDialogTexte.WsenLadeprioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenLadeprioErl),
                    new KiDialogFeld("ladeprioritaet_pv", "WaermesenkeKiSicht.LadeprioritaetPv",
                                     KiDialogTexte.WsenLadeprioPvName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenLadeprioPvErl),
                    new KiDialogFeld("ladegrenze_aktiv", "WaermesenkeKiSicht.LadegrenzeAktiv",
                                     KiDialogTexte.WsenLadegrenzeAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WsenLadegrenzeAktivErl),
                    new KiDialogFeld("ladegrenze", "WaermesenkeKiSicht.Ladegrenze",
                                     KiDialogTexte.WsenLadegrenzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WsenLadegrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("einspeisehoehe_aktiv",
                                     "WaermesenkeKiSicht.EinspeisehoeheAktiv",
                                     KiDialogTexte.WsenHoeheAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WsenHoeheAktivErl),
                    new KiDialogFeld("einspeisehoehe", "WaermesenkeKiSicht.Einspeisehoehe",
                                     KiDialogTexte.WsenHoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WsenHoeheErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_QuelleErdreich  ->  QuelleErdreichDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Waermequelle ERDREICH einer Sole-/Wasser-Waermepumpe — acht Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.QuelleErdreichKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ZWEI Quellsysteme, ein Feldsatz.</b> Der Erdkollektor fuehrt Verlegetiefe
        /// und Flaeche, die Erdsonde Laenge je Sonde und Anzahl; die Wahl zwischen
        /// beiden ist ein Wahrheitswert (<c>Erdsonde</c>). Sie SPERRT nur — der andere
        /// Zweig behaelt seine Werte, und deshalb stehen alle vier Zahlen im Katalog
        /// und nicht nur die des gewaehlten Zweigs.
        /// </para>
        /// <para>
        /// <b>BODENTYP und KLIMAZONE sind WAHLFELDER</b> (KI-F1b, KI-D-Q6): Ihre
        /// Eintraege liefert die Maske zur Laufzeit — der Bodenkatalog nach VDI 4640
        /// und die Zonenliste. Der Bodentyp traegt dabei den KATALOGSCHLUESSEL und
        /// nicht seinen Listenplatz (Abweichung A-3): Wird der Katalog umsortiert,
        /// zeigte ein Platz danach auf den falschen Boden. Die Klimazone traegt ihre
        /// Nummer (1…15, 0 = nicht zugeordnet).
        /// </para>
        /// <para>
        /// <b>„Simulation starten" ist kein Knopf dieser Liste.</b> Er rechnet lange,
        /// meldet Fortschritt und laesst sich abbrechen; solche Wege gehoeren in das
        /// Aktionsregister (Stufe 2), nicht in die Positivliste einer Maske. Der
        /// Kartenknopf „…" traegt nur ein Zeichen und keine Beschriftung.
        /// </para>
        /// </remarks>
        private static KiDialog QuelleErdreich()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.QUELLE_ERDREICH,
                anzeigename: KiDialogTexte.MaskeQuelleErdreich,
                felder: new[]
                {
                    new KiDialogFeld("erdsonde", "QuelleErdreichKiSicht.Erdsonde",
                                     KiDialogTexte.QerdSondeName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.QerdSondeErl),
                    new KiDialogFeld("verlegetiefe", "QuelleErdreichKiSicht.Verlegetiefe",
                                     KiDialogTexte.QerdTiefeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdTiefeErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("flaeche", "QuelleErdreichKiSicht.Flaeche",
                                     KiDialogTexte.QerdFlaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdFlaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true),
                    new KiDialogFeld("laenge_sonde", "QuelleErdreichKiSicht.LaengeSonde",
                                     KiDialogTexte.QerdLaengeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdLaengeErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("anzahl_sonden", "QuelleErdreichKiSicht.AnzahlSonden",
                                     KiDialogTexte.QerdAnzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.QerdAnzahlErl, leerErlaubt: true),
                    new KiDialogFeld("spreizung", "QuelleErdreichKiSicht.Spreizung",
                                     KiDialogTexte.QerdSpreizungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdSpreizungErl,
                                     einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true),

                    // ---- Die zwei Klapplisten des Standorts (KI-F1b, KI-D-Q6) ----
                    new KiDialogFeld("bodentyp", "QuelleErdreichKiSicht.Bodentyp",
                                     KiDialogTexte.QerdBodentypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QerdBodentypErl),
                    new KiDialogFeld("klimazone", "QuelleErdreichKiSicht.Klimazone",
                                     KiDialogTexte.QerdKlimazoneName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QerdKlimazoneErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_QuellePufferspeicher  ->  QuellePufferspeicherDialog  (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Waermequelle PUFFERSPEICHER — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.QuellePufferspeicherKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ZWEI Erzeugerarten in einer Maske.</b> Die Waermepumpe zieht
        /// Verdampferwaerme aus dem Speicher und pflegt dazu Quelltemperatur,
        /// Spreizung, Regeneration und „Quelle unbegrenzt"; der Heizkessel nimmt seine
        /// Eintrittstemperatur aus dem Puffer und pflegt dafuer den Temperaturbezug
        /// samt festem Vorlauf und Ruecklauf. Die Entnahmehoehe gilt beiden. Welche
        /// Felder sichtbar sind, entscheidet die Erzeugerart; die jeweils andere Seite
        /// bleibt unangetastet.
        /// </para>
        /// <para>
        /// <b>Der gewaehlte PUFFER ist ein WAHLFELD</b> (KI-F1b, KI-D-Q6): Seine
        /// Eintraege sind die Pufferliste DIESES Projekts, und die kennt nur der
        /// Dialog — er liefert sie zur Laufzeit. Der Schluessel ist die Id der Zeile,
        /// der Text ihre Anzeige.
        /// </para>
        /// <para>
        /// <b>„Pufferspeicher anlegen…" ist kein Knopf dieser Liste.</b> Er oeffnet
        /// eine zweite Maske als Ueberlagerung, setzt aber keinen Wert; die
        /// Positivliste dieser Welle fuehrt nur OK und Abbrechen.
        /// </para>
        /// </remarks>
        private static KiDialog QuellePufferspeicher()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.QUELLE_PUFFERSPEICHER,
                anzeigename: KiDialogTexte.MaskeQuellePuffer,
                felder: new[]
                {
                    new KiDialogFeld("quelltemperatur",
                                     "QuellePufferspeicherKiSicht.Quelltemperatur",
                                     KiDialogTexte.QpufTemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("spreizung", "QuellePufferspeicherKiSicht.Spreizung",
                                     KiDialogTexte.QpufSpreizungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufSpreizungErl,
                                     einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true),
                    new KiDialogFeld("regeneration", "QuellePufferspeicherKiSicht.Regeneration",
                                     KiDialogTexte.QpufRegenerationName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufRegenerationErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("unbegrenzt", "QuellePufferspeicherKiSicht.Unbegrenzt",
                                     KiDialogTexte.QpufUnbegrenztName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.QpufUnbegrenztErl),
                    new KiDialogFeld("temperatur_fest",
                                     "QuellePufferspeicherKiSicht.TemperaturFest",
                                     KiDialogTexte.QpufFestName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.QpufFestErl),
                    new KiDialogFeld("vorlauf", "QuellePufferspeicherKiSicht.Vorlauf",
                                     KiDialogTexte.QpufVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.QpufVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "QuellePufferspeicherKiSicht.Ruecklauf",
                                     KiDialogTexte.QpufRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.QpufRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("anschlusshoehe",
                                     "QuellePufferspeicherKiSicht.Anschlusshoehe",
                                     KiDialogTexte.QpufAnschlusshoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufAnschlusshoeheErl, leerErlaubt: true),

                    // ---- Der gewaehlte Speicher (KI-F1b, KI-D-Q6) ------------------
                    new KiDialogFeld("puffer", "QuellePufferspeicherKiSicht.Puffer",
                                     KiDialogTexte.QpufPufferName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QpufPufferErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PufferSp_Projekt  ->  PufferSpProjektDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Pufferspeicher-Verwaltung des Projekts — zweiundzwanzig Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.PufferSpProjektKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ein SICHTMODELL und nicht das Daten-Objekt.</b> Dieser Dialog
        /// bekommt keinen veraenderlichen Satz herein und gibt keinen heraus: Was
        /// hereinkommt, sind zwei Ids, und was der Anwender bearbeitet, steht bis zum
        /// „Uebernehmen" in den Eingabefeldern der Maske — erst dort entsteht der
        /// unveraenderliche Record <c>PspEingaben</c>. Ein daran angemeldeter Katalog
        /// zeigte dem Assistenten den Stand von vorhin und setzte ins Leere. Dieselbe
        /// Lage wie bei der Stromspeicher- und der Simulationsansicht, dieselbe
        /// Antwort: eine flache Sichtklasse ueber die lebenden Felder.
        /// </para>
        /// <para>
        /// <b>Die drei ENTNAHMEHOEHEN stehen im Katalog</b> (KI-F1b): Die Maske
        /// beschriftet sie seither in beiden Sprachen
        /// (<c>PSP_LABEL_ENTNAHME_HEIZUNG</c> und die zwei Geschwister), und damit ist
        /// ihr Anzeigename abgelesen und nicht erfunden.
        /// </para>
        /// <para>
        /// <b>Die NUTZUNG steht als DREI Wahrheitswerte da.</b> Auf der Maske ist sie
        /// EINE Mehrfachwahl aus drei Klassen; ein Katalogfeld traegt EINEN Wert
        /// (<c>KiDialogFeld</c>). Deshalb fragt der Katalog je Klasse „ist sie im
        /// Set?" — und das Setzen nimmt sie ueber denselben Rueckruf hinein oder
        /// heraus, den ein Klick nimmt.
        /// </para>
        /// <para>
        /// <b>Der Katalogsatz „Aus Katalog" bleibt draussen.</b> Seine Id ist nur der
        /// Anzeigeindex der Klappliste, und das Waehlen KOPIERT Volumen und Verluste
        /// in die Eingabefelder — das ist ein Ladevorgang und kein Feldwert. Wer ihn
        /// als Feld deklarierte, boete dem Assistenten an, „einen Wert zu setzen", der
        /// in Wahrheit zwei andere Felder ueberschreibt.
        /// </para>
        /// <para>
        /// <b>Knoepfe: OK, Abbrechen und UEBERNEHMEN.</b> „Entfernen" ist ein
        /// Loeschknopf und damit nicht deklarierbar (Bauartsperre in
        /// <see cref="KiDialogKnopf"/>); „Neuer Pufferspeicher" und „Katalog
        /// ansehen…" wechseln den Stand der Maske, ohne einen Wert zu setzen, und
        /// gehoeren nicht in die Positivliste dieser Welle.
        /// </para>
        /// </remarks>
        private static KiDialog PufferspeicherVerwaltung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PUFFERSPEICHER_VERWALTUNG,
                anzeigename: KiDialogTexte.MaskePufferSpVerwaltung,
                felder: new[]
                {
                    new KiDialogFeld("bezeichner", "PufferSpProjektKiSicht.Bezeichner",
                                     KiDialogTexte.PspvBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.PspvBezeichnerErl),
                    new KiDialogFeld("volumen", "PufferSpProjektKiSicht.Volumen",
                                     KiDialogTexte.PspvVolumenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvVolumenErl,
                                     einheit: KiDialogTexte.EINHEIT_LITER, leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverluste",
                                     "PufferSpProjektKiSicht.Bereitschaftsverluste",
                                     KiDialogTexte.PspvVerlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvVerlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH_24H, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "PufferSpProjektKiSicht.Vorlauf",
                                     KiDialogTexte.PspvVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "PufferSpProjektKiSicht.Ruecklauf",
                                     KiDialogTexte.PspvRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("einschaltschwelle",
                                     "PufferSpProjektKiSicht.Einschaltschwelle",
                                     KiDialogTexte.PspvSchwelleEinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvSchwelleEinErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("abschaltschwelle",
                                     "PufferSpProjektKiSicht.Abschaltschwelle",
                                     KiDialogTexte.PspvSchwelleAusName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvSchwelleAusErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("schwelle_nachrangig",
                                     "PufferSpProjektKiSicht.SchwelleNachrangig",
                                     KiDialogTexte.PspvSchwelleNachrangName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvSchwelleNachrangErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("mindestfuellstand",
                                     "PufferSpProjektKiSicht.Mindestfuellstand",
                                     KiDialogTexte.PspvMindestfuellstandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvMindestfuellstandErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("schichten", "PufferSpProjektKiSicht.Schichten",
                                     KiDialogTexte.PspvSchichtenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvSchichtenErl, leerErlaubt: true),
                    new KiDialogFeld("hoehe", "PufferSpProjektKiSicht.Hoehe",
                                     KiDialogTexte.PspvHoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvHoeheErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("lambda", "PufferSpProjektKiSicht.Lambda",
                                     KiDialogTexte.PspvLambdaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvLambdaErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("nutztemperatur_bw",
                                     "PufferSpProjektKiSicht.NutztemperaturBw",
                                     KiDialogTexte.PspvNutztemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvNutztemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ladeleistung", "PufferSpProjektKiSicht.Ladeleistung",
                                     KiDialogTexte.PspvLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("entladeleistung", "PufferSpProjektKiSicht.Entladeleistung",
                                     KiDialogTexte.PspvEntladeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntladeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("entladeprioritaet",
                                     "PufferSpProjektKiSicht.Entladeprioritaet",
                                     KiDialogTexte.PspvEntladeprioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PspvEntladeprioErl, leerErlaubt: true),

                    // ---- Die NUTZUNG: drei Wahrheitswerte statt einer Mehrfachwahl --
                    new KiDialogFeld("nutzung_heizung",
                                     "PufferSpProjektKiSicht.NutzungHeizung",
                                     KiDialogTexte.PspvNutzungHeizungName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PspvNutzungHeizungErl),
                    new KiDialogFeld("nutzung_brauchwasser",
                                     "PufferSpProjektKiSicht.NutzungBrauchwasser",
                                     KiDialogTexte.PspvNutzungBwName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PspvNutzungBwErl),
                    new KiDialogFeld("nutzung_prozess",
                                     "PufferSpProjektKiSicht.NutzungProzess",
                                     KiDialogTexte.PspvNutzungProzessName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PspvNutzungProzessErl),

                    // ---- Die drei Entnahmehoehen -----------------------------------
                    new KiDialogFeld("entnahmehoehe_heizung",
                                     "PufferSpProjektKiSicht.EntnahmehoeheHeizung",
                                     KiDialogTexte.PspvEntnahmeHeizungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntnahmeHeizungErl, leerErlaubt: true),
                    new KiDialogFeld("entnahmehoehe_brauchwasser",
                                     "PufferSpProjektKiSicht.EntnahmehoeheBrauchwasser",
                                     KiDialogTexte.PspvEntnahmeBwName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntnahmeBwErl, leerErlaubt: true),
                    new KiDialogFeld("entnahmehoehe_prozess",
                                     "PufferSpProjektKiSicht.EntnahmehoeheProzess",
                                     KiDialogTexte.PspvEntnahmeProzessName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntnahmeProzessErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_WP_Anlage  ->  WaermepumpeAnlageDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Die Waermepumpen-ANLAGE eines Projekts — achtundzwanzig Felder aus
        /// <c>EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageKiSicht</c>, die den Feldsatz
        /// <c>WaermepumpeAnlageDaten</c> unveraendert durchreicht und die
        /// Projekteinstellung „Extrapolation der WP-Kennlinie erlauben" dazu traegt
        /// (Welle #458), dazu die fuenf Felder der Gruppe „Kuehlbetrieb" (Stufe KU2
        /// Welle 3).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>DREI Bloecke, EIN Daten-Objekt.</b> Die Maske besteht aus dem Dialog und
        /// zwei Bausteinen: der KONFIGURATION (Heizstab, Sperrzeit, bivalenter Betrieb,
        /// Betriebsart, Bivalenztemperatur) und den STAMMFELDERN (Hersteller, Typ,
        /// Nennleistung …). Beide schreiben in denselben Satz — die Stammfelder ueber
        /// ein Abbild, das der Dialog bei jeder Eingabe zurueckschreibt und nach einer
        /// Feldsetzung des Assistenten neu aufbaut. Deshalb steht hier EIN
        /// Katalogeintrag und nicht drei.
        /// </para>
        /// <para>
        /// <b>Die BETRIEBSART ist eine Aufzaehlung.</b> Ihre Werte sind Steuerwerte des
        /// Bestands (<c>DbWerte.WP_BETRIEBSART_*</c>: alternativ, parallel,
        /// teilparallel) und stehen so in <c>Tab_Energieanlagen.Betriebsart</c> — nicht
        /// der Anzeigetext der Klappliste. Was sie bedeuten, steht in der Erlaeuterung.
        /// </para>
        /// <para>
        /// <b>Die MODULKOSTEN sind Anzeige.</b> Der Stammfeldblock zeigt sie als
        /// Lesewert; gepflegt werden Geraetekosten in der Kostenverwaltung. Ohne
        /// <c>nurLesen</c> boete der Assistent an, eine Zahl zu setzen, die der naechste
        /// Kostenlauf wortlos ueberschriebe.
        /// </para>
        /// <para>
        /// <b>Der ENERGIETRAEGER ist eine Wahl</b> (KI-F1b): Die Id (<c>CarrierId</c>)
        /// ist der Schluessel, die Eintraege reicht der Dialog aus dem Traegerkatalog
        /// herein. Die KENNLINIEN fehlen mit Absicht: Sie sind eine Tabelle von
        /// Stuetzstellen mit eigenem Editor, kein Maskenfeld.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe ausser OK und Abbrechen.</b> „Kennlinien aus dem Katalog
        /// uebernehmen" und „In Stamm uebernehmen…" sind datenbankwirksam und gehoeren
        /// in das Aktionsregister mit Bestaetigung und Sicherungspunkt.
        /// </para>
        /// </remarks>
        private static KiDialog WaermepumpeAnlage()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMEPUMPE_ANLAGE,
                anzeigename: KiDialogTexte.MaskeWpAnlage,
                felder: new[]
                {
                    // ---- Woran der Anwender gerade arbeitet -------------------------
                    new KiDialogFeld("anlage", "WaermepumpeAnlageKiSicht.Bezeichner",
                                     KiDialogTexte.WpaAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaAnlageErl,
                                     leerErlaubt: true, nurLesen: true),

                    // ---- Auslegung fuer die Verteilung ------------------------------
                    new KiDialogFeld("vorlauf", "WaermepumpeAnlageKiSicht.Vorlauf",
                                     KiDialogTexte.WpaVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "WaermepumpeAnlageKiSicht.Ruecklauf",
                                     KiDialogTexte.WpaRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("nutzungsdauer", "WaermepumpeAnlageKiSicht.Nutzungszeit",
                                     KiDialogTexte.WpaNutzungsdauerName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaNutzungsdauerErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR, leerErlaubt: true),

                    // ---- Die Projekteinstellung neben der Auslegung (Welle #458) -----
                    //
                    // „Extrapolation der WP-Kennlinie erlauben" gilt allen Waermepumpen
                    // des Projekts und schreibt SOFORT (ExtrapolationSchreiben) - sie
                    // haengt nicht am Feldsatz der Anlage; deshalb meldet der Dialog
                    // eine Sichtklasse an, die beides traegt.
                    new KiDialogFeld("extrapolation", "WaermepumpeAnlageKiSicht.Extrapolation",
                                     KiDialogTexte.WpaExtrapolationName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaExtrapolationErl),

                    // ---- Der Block „Konfiguration" ---------------------------------
                    new KiDialogFeld("heizstab", "WaermepumpeAnlageKiSicht.Heizstab",
                                     KiDialogTexte.WpaHeizstabName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaHeizstabErl),
                    new KiDialogFeld("sperrzeit", "WaermepumpeAnlageKiSicht.Sperrung",
                                     KiDialogTexte.WpaSperrungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaSperrungErl),
                    new KiDialogFeld("sperrzeit_von", "WaermepumpeAnlageKiSicht.SperrzeitVon",
                                     KiDialogTexte.WpaSperrzeitVonName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitVonErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("sperrzeit_bis", "WaermepumpeAnlageKiSicht.SperrzeitBis",
                                     KiDialogTexte.WpaSperrzeitBisName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitBisErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("bivalenter_betrieb", "WaermepumpeAnlageKiSicht.BivalenterBetrieb",
                                     KiDialogTexte.WpaBivalentName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaBivalentErl),
                    new KiDialogFeld("energietraeger", "WaermepumpeAnlageKiSicht.CarrierId",
                                     KiDialogTexte.WpaTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaTraegerErl),
                    new KiDialogFeld("betriebsart", "WaermepumpeAnlageKiSicht.Betriebsart",
                                     KiDialogTexte.WpaBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBetriebsartErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("bivalenztemperatur", "WaermepumpeAnlageKiSicht.Abschaltpunkt",
                                     KiDialogTexte.WpaAbschaltpunktName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaAbschaltpunktErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Die Gruppe „Kuehlbetrieb" der Konfiguration (Stufe KU2 Welle 3;
                    //      Kuehlkonzept 8.2, E15, E33, E34) ----------------------------
                    //
                    // Der Schalter ist in der Maske WEICH gesperrt (keine Kuehlkennlinie im
                    // Projekt, Quellspeicher); die Sichtklasse lehnt dieselbe Setzung benannt
                    // ab. Kuehl-Vorlauf, Kuehltraeger und Abrechnungsart sind Wahlfelder - ihre
                    // Eintraege traegt die Sichtklasse als Begleiteigenschaft <Eigenschaft>Wahl.
                    // Der Hilfsstromanteil steht in PROZENT, wie die Maske ihn zeigt.
                    new KiDialogFeld("kuehlbetrieb", "WaermepumpeAnlageKiSicht.Kuehlbetrieb",
                                     KiDialogTexte.WpaKuehlbetriebName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaKuehlbetriebErl),
                    new KiDialogFeld("kuehl_vorlauf", "WaermepumpeAnlageKiSicht.KuehlVorlauf",
                                     KiDialogTexte.WpaKuehlVorlaufName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaKuehlVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("hilfsstromanteil", "WaermepumpeAnlageKiSicht.KuehlHilfsstromanteil",
                                     KiDialogTexte.WpaHilfsstromName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaHilfsstromErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("kuehltraeger", "WaermepumpeAnlageKiSicht.KuehlCarrierId",
                                     KiDialogTexte.WpaKuehltraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaKuehltraegerErl, leerErlaubt: true),
                    new KiDialogFeld("kuehl_abrechnung", "WaermepumpeAnlageKiSicht.KuehlAbrechnung",
                                     KiDialogTexte.WpaAbrechnungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaAbrechnungErl),

                    // ---- Die Felder des Geraets (Stammfeldblock) --------------------
                    new KiDialogFeld("hersteller", "WaermepumpeAnlageKiSicht.Firma",
                                     KiDialogTexte.WpaFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "WaermepumpeAnlageKiSicht.Beschreibung",
                                     KiDialogTexte.WpaBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("typ", "WaermepumpeAnlageKiSicht.Typ",
                                     KiDialogTexte.WpaTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaTypErl, leerErlaubt: true),
                    new KiDialogFeld("leistungsstufen", "WaermepumpeAnlageKiSicht.Regelung",
                                     KiDialogTexte.WpaRegelungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaRegelungErl, leerErlaubt: true),
                    new KiDialogFeld("aufstellung", "WaermepumpeAnlageKiSicht.Aufstellung",
                                     KiDialogTexte.WpaAufstellungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaAufstellungErl, leerErlaubt: true),
                    new KiDialogFeld("baujahr", "WaermepumpeAnlageKiSicht.Baujahr",
                                     KiDialogTexte.WpaBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBaujahrErl),
                    new KiDialogFeld("nennleistung", "WaermepumpeAnlageKiSicht.Nennleistung",
                                     KiDialogTexte.WpaNennleistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("heizstab_leistung", "WaermepumpeAnlageKiSicht.HeizstabLeistung",
                                     KiDialogTexte.WpaHeizstabLeistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaHeizstabLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kuehlleistung", "WaermepumpeAnlageKiSicht.Kuehlleistung",
                                     KiDialogTexte.WpaKuehlleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaKuehlleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("modulkosten", "WaermepumpeAnlageKiSicht.Modulkosten",
                                     KiDialogTexte.WpaModulkostenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaModulkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_SolarKollektoren  ->  SolarkollektorenDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Solarkollektoren im Projekt — die fuenf Zahlen der Kollektorgruppe
        /// (<c>EPOS.UI.Dialoge.Solarthermie.SolarkollektorenEingaben</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist der ARBEITSSTAND und nicht die Projektzeile.</b>
        /// Diese Maske schreibt die Zeile erst beim Knopf „Uebernehmen"; bis dahin
        /// fuehrt sie die Eingaben fuer sich, und genau die sieht der Anwender. Eine
        /// Setzung in die Zeile bliebe auf der Maske unsichtbar, und das naechste
        /// „Uebernehmen" ueberschriebe sie wortlos — deshalb zeigt der Katalog auf den
        /// Stand, an dem auch die Eingabefelder haengen.
        /// </para>
        /// <para>
        /// <b>Die APERTURFLAECHE fehlt.</b> Sie ist Anzeige und faellt aus Modulflaeche
        /// mal Anzahl; ein eigenes Feld traegt sie im Arbeitsstand nicht, und eine
        /// Groesse, die es nur als gerechnete Zeichenkette gibt, laesst sich weder
        /// benennen noch pruefen. Wer die Flaeche aendern will, aendert die Anzahl.
        /// </para>
        /// </remarks>
        private static KiDialog SolarkollektorenProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT,
                anzeigename: KiDialogTexte.MaskeSolarkollektoren,
                felder: MitAlleDaten(new[]
                {
                    new KiDialogFeld("anzahl_module", "SolarkollektorenKiSicht.Anzahl",
                                     KiDialogTexte.SkAnzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkAnzahlErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("neigung", "SolarkollektorenKiSicht.Neigung",
                                     KiDialogTexte.SkNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("azimut", "SolarkollektorenKiSicht.Azimut",
                                     KiDialogTexte.SkAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "SolarkollektorenKiSicht.Vorlauf",
                                     KiDialogTexte.SkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "SolarkollektorenKiSicht.Ruecklauf",
                                     KiDialogTexte.SkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                }, AlleDaten(KatalogBrowserArt.Solarkollektoren, "SolarkollektorenKiSicht")),
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PufferSp  ->  PufferspeicherDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Pufferspeicher im Projekt — die gewaehlte Zeile der Projektliste
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerProjektKiSicht</c>) mit EINEM Feld der
        /// Anlage, dazu der Aufklapper „Alle Daten".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ein Feld der Anlage, und das ist kein Versehen.</b> Diese Maske fuehrt
        /// keine Einstellwerte der ANLAGE: Sie waehlt den Speicher aus dem Katalog und
        /// zeigt seine Werte. Der Name der gewaehlten Zeile beantwortet „welcher
        /// Pufferspeicher ist im Projekt gewaehlt?" aus der Maske.
        /// </para>
        /// <para>
        /// <b>Einstellbar ist der KATALOGSATZ im Aufklapper „Alle Daten"</b> (Welle #458,
        /// Stufe 2): Seine Felder entstehen aus dem Profil des Katalogbrowsers
        /// (<see cref="AlleDaten(KatalogBrowserArt,string)"/>), gesetzt wird ueber die
        /// Feldtafel der Sichtklasse, gespeichert ueber den Knopf des Aufklappers; ein
        /// Auslieferungssatz lehnt mit dem Weg „Duplizieren…" ab.
        /// </para>
        /// </remarks>
        private static KiDialog PufferspeicherProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PUFFERSPEICHER_PROJEKT,
                anzeigename: KiDialogTexte.MaskePufferSpProjekt,
                felder: MitAlleDaten(new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerProjektKiSicht.Bezeichner",
                                     KiDialogTexte.PspAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.PspAnlageErl,
                                     leerErlaubt: true, nurLesen: true)
                }, AlleDaten(KatalogBrowserArt.Pufferspeicher, ERZEUGER_PROJEKT_SICHT)),
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Stromspeicher  ->  StromspeicherDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Stromspeicher im Projekt — die gewaehlte Zeile der Projektliste
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>), mit EINEM Feld.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Feldumfang wie beim Pufferspeicher.</b> Die Maske waehlt das Geraet und
        /// zeigt seine Katalogwerte; die Betriebsfuehrung der Speicher steht in der
        /// Ansicht „Stromspeicher-Auslegung"
        /// (<see cref="KiMaskennamen.STROMSPEICHER_AUSLEGUNG"/>) und dort im Katalog
        /// mit siebenundzwanzig Feldern.
        /// </para>
        /// <para>
        /// <b>Der ENERGIETRAEGER ist eine Wahl</b> (KI-F1b, KI-D-Q6): Auf der Maske
        /// steht er ueber Gruppe und Art, im Daten-Objekt als Id
        /// (<c>ErzeugerZeile.CarrierId</c>); die Eintraege reicht der Dialog als
        /// Wahlquelle herein, gesetzt wird ueber den angezeigten Text — dieselbe Regel
        /// wie bei der Traegervariante der Kessel- und BHKW-Maske.
        /// </para>
        /// </remarks>
        private static KiDialog StromspeicherProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STROMSPEICHER_PROJEKT,
                anzeigename: KiDialogTexte.MaskeStromspeicherProjekt,
                felder: MitAlleDaten(new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerProjektKiSicht.Bezeichner",
                                     KiDialogTexte.StspAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.StspAnlageErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("energietraeger", "ErzeugerProjektKiSicht.CarrierId",
                                     KiDialogTexte.StspTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.StspTraegerErl)
                }, AlleDaten(ModulKatalogArt.Stromspeicher, Stromspeicherkatalog(), ERZEUGER_PROJEKT_SICHT)),
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Heizkessel  ->  HeizkesselDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Heizkessel im Projekt — die Werte, die der Projektdialog an der GEWAEHLTEN
        /// Zeile fuehrt (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist eine ZEILE und kein Dialogstand</b> — dieselbe
        /// Bauart wie bei <see cref="Photovoltaik"/>: Der Dialog fuehrt eine
        /// Projektliste, angemeldet wird die gewaehlte Zeile, und der Getter holt sie
        /// bei jedem Lesen neu. Ist keine gewaehlt, sind die Felder leer — derselbe
        /// Zustand, den der Anwender auf der Maske sieht.
        /// </para>
        /// <para>
        /// <b>Die TRAEGERVARIANTE ist eine Wahl</b> (KI-F1b, KI-D-Q6): ein Verweis in
        /// eine kontextabhaengige Liste (<c>ErzeugerZeile.CarrierId</c>, gefuellt aus
        /// den Varianten der Traegergruppe). Die Eintraege reicht der Dialog als
        /// Wahlquelle herein; der Assistent setzt sie ueber den angezeigten Text und
        /// raet keine rohe Id — dieselbe Regel wie bei der Bemessung der
        /// Kostenverwaltung.
        /// </para>
        /// <para>
        /// <b>Der Aufklapper „Alle Daten"</b> zeigt die Spalten des gewaehlten
        /// KATALOGsatzes ueber ein Profil
        /// (<c>EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs</c>); was dort steht,
        /// gehoert dem Katalog und nicht der Anlage. Seit Welle #458 (Stufe 2) steht er
        /// als FELDTAFEL im Katalog — die Felder entstehen aus demselben Profil
        /// (<see cref="AlleDaten(KatalogBrowserArt,string)"/>, Vorsilbe
        /// <see cref="KATALOGFELD_VORSILBE"/>), gesetzt wird in die lebende Feldliste des
        /// Aufklappers, gespeichert ueber seinen Knopf. Dasselbe gilt fuer BHKW,
        /// Pufferspeicher, Solarkollektoren, Stromspeicher und Photovoltaik.
        /// </para>
        /// </remarks>
        private static KiDialog HeizkesselProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.HEIZKESSEL_PROJEKT,
                anzeigename: KiDialogTexte.MaskeHeizkesselProjekt,
                felder: MitAlleDaten(new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerProjektKiSicht.Bezeichner",
                                     KiDialogTexte.HkpAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.HkpAnlageErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("vorlauf", "ErzeugerProjektKiSicht.Vorlauf",
                                     KiDialogTexte.HkpVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkpVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "ErzeugerProjektKiSicht.CarrierId",
                                     KiDialogTexte.HkpTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.HkpTraegerErl),
                    new KiDialogFeld("ruecklauf", "ErzeugerProjektKiSicht.Ruecklauf",
                                     KiDialogTexte.HkpRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkpRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                }, AlleDaten(KatalogBrowserArt.Heizkessel, ERZEUGER_PROJEKT_SICHT)),
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_BHKWEing  ->  BhkwDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// BHKW im Projekt — die Werte der gewaehlten Projektzeile
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>).
        /// </summary>
        /// <remarks>
        /// <b>Die untere GRENZLEISTUNG ist der Unterschied zum Heizkessel.</b> Sie sagt,
        /// bis wohin das Modul moduliert; 0 heisst „Projektvorgabe"
        /// (<c>Tab_Einstellungen.Leistungsgrenze</c>), und genau das steht in ihrer
        /// Erlaeuterung. Die Traegervariante ist eine Wahl wie beim Heizkessel.
        /// </remarks>
        private static KiDialog BhkwProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BHKW_PROJEKT,
                anzeigename: KiDialogTexte.MaskeBhkwProjekt,
                felder: MitAlleDaten(new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerProjektKiSicht.Bezeichner",
                                     KiDialogTexte.BhkwAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.BhkwAnlageErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("grenzleistung", "ErzeugerProjektKiSicht.Grenzleistung",
                                     KiDialogTexte.BhkwGrenzleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkwGrenzleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "ErzeugerProjektKiSicht.Vorlauf",
                                     KiDialogTexte.BhkwVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.BhkwVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "ErzeugerProjektKiSicht.CarrierId",
                                     KiDialogTexte.BhkwTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhkwTraegerErl),
                    new KiDialogFeld("ruecklauf", "ErzeugerProjektKiSicht.Ruecklauf",
                                     KiDialogTexte.BhkwRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.BhkwRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                }, AlleDaten(KatalogBrowserArt.Bhkw, ERZEUGER_PROJEKT_SICHT)),
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Kostenverwaltung  ->  KostenKomponenteDialog
        // =====================================================================

        /// <summary>
        /// Kostenverwaltung einer Komponente — die erste Maske des Katalogs, die mit
        /// SPALTEN arbeitet (Anwenderbefund vom 14.09.2026); seit der Welle KI-F7
        /// angemeldet ueber <c>EPOS.UI.Dialoge.Kosten.KostenKomponenteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Kopffelder und vier Spalten.</b> Die Kopffelder sagen, WORAN der
        /// Anwender gerade arbeitet (Komponente, Projekt, Art der Vorlage); sie sind
        /// samt und sonders <c>nurLesen</c> - die Huelle fuellt sie, der Anwender
        /// aendert sie nicht. Die Spalten sind das Raster: je Position eine Zeile, und
        /// aus jeder Spaltendeklaration wird je vorhandener Zeile ein gewoehnliches
        /// Feld (<c>nutzungsdauer_1</c>, <c>nutzungsdauer_2</c>, …).
        /// </para>
        /// <para>
        /// <b>Die DREI LUECKEN der Welle KI-F4 sind geschlossen</b> (Anwenderentscheid
        /// 21.09.2026, KI-D-Q7). Komponentenwahl, PV-Wahl und PV-Projekt stehen nicht
        /// am Arbeitsstand: Die erste liegt in einem privaten Feld des Dialogs, die
        /// zwei anderen im Baustein <c>ErtragBonus</c> des Reiters „Ertrag". Sie zu
        /// deklarieren hiess, die Maske von ihrem Daten-Objekt auf eine Sichtklasse
        /// umzustellen und dabei ihre Markup-Probe aufzugeben; der Anwender hat das
        /// entschieden. Alle bisherigen Felder laufen unveraendert weiter - dieselben
        /// Namen, dieselben Arten, dasselbe <c>nurLesen</c>.
        /// </para>
        /// <para>
        /// <b>Der Betrag ist Anzeige, kein Eingabefeld.</b> Er faellt aus Satz und
        /// Bemessung und traegt im Daten-Objekt nur deshalb einen Setzer, weil die
        /// Huelle ihn fuellt. Ohne <c>nurLesen</c> boete der Assistent an, ihn zu
        /// setzen - und die naechste Neuberechnung ueberschriebe es wortlos.
        /// </para>
        /// <para>
        /// <b>Die Bemessung ist eine Wahl je Zeile</b> (KI-F1b, KI-D-Q6): ein Verweis in
        /// eine kontextabhaengige Liste (<c>KostenKomponenteStand.Bemessungen</c>),
        /// gesetzt ueber den angezeigten Text statt ueber die rohe Id — dieselbe Regel
        /// wie ueberall: Aufzaehlungswerte stammen aus dem Bestand, nie aus
        /// Modelltext.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> „Speichern" und „OK" sind datenbankwirksam und laufen
        /// deshalb ueber <c>dialog_speichern</c> mit Bestaetigung UND Sicherungspunkt
        /// (Anwenderentscheid KI-D-Q4) - nicht als Formularaktion.
        /// </para>
        /// </remarks>
        private static KiDialog Kostenverwaltung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOSTENVERWALTUNG,
                anzeigename: KiDialogTexte.MaskeKostenverwaltung,
                felder: new[]
                {
                    // ---- Woran der Anwender gerade arbeitet -------------------------
                    new KiDialogFeld("komponente", "KostenKomponenteKiSicht.Titel",
                                     KiDialogTexte.KvTitelName, KiParameterTyp.Text,
                                     KiDialogTexte.KvTitelErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("bezug", "KostenKomponenteKiSicht.Untertitel",
                                     KiDialogTexte.KvUntertitelName, KiParameterTyp.Text,
                                     KiDialogTexte.KvUntertitelErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("auslieferungsvorlage", "KostenKomponenteKiSicht.NurLesen",
                                     KiDialogTexte.KvNurLesenName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.KvNurLesenErl,
                                     nurLesen: true),

                    // Welle KI-F7, erste der drei Luecken: die KOMPONENTENWAHL der
                    // Kontextleiste. Sie steht in einem privaten Feld des Dialogs, und
                    // ihre Liste kennt nur er - die Sichtklasse traegt beides.
                    //
                    // NUR LESEN, und zwar aus demselben Grund wie die Variante
                    // darunter: Eine andere Komponente zu waehlen laedt einen anderen
                    // Positionssatz nach. Das ist ein Vorgang und kein Feldwert; der
                    // Assistent nennt die offene Komponente samt ihren Alternativen.
                    new KiDialogFeld("komponentenwahl",
                                     "KostenKomponenteKiSicht.Komponentenwahl",
                                     KiDialogTexte.KvKomponenteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KvKomponenteErl,
                                     leerErlaubt: true, nurLesen: true),

                    // Welle KI-F4: die VARIANTE der Vorlage - das einzige Wahlfeld der
                    // Maske, das am Stand haengt und bis dahin fehlte. Sie steht nur im
                    // Stammkontext (VariantePflegbar); im Projekt ist die Liste leer.
                    //
                    // NUR LESEN, und das ist der Punkt: Die Variante zu WECHSELN laedt
                    // einen anderen Positionssatz nach (BeiVariante -> KontextLaden).
                    // Der Katalog bindet hier unmittelbar an den Stand; ein Setzer
                    // schriebe die Id, ohne dass das Raster nachzieht - die Maske zeigte
                    // dann die neue Variante ueber den alten Zeilen. Der Assistent NENNT
                    // sie deshalb samt ihren Alternativen und lehnt das Setzen benannt
                    // ab; ein Wechsel ist ein Ladevorgang und kein Feldwert.
                    new KiDialogFeld("variante", "KostenKomponenteKiSicht.VarianteId",
                                     KiDialogTexte.KvVarianteName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KvVarianteErl,
                                     leerErlaubt: true, nurLesen: true),

                    // ---- Der Reiter „Ertrag" (Welle KI-F7) ---------------------------
                    // Zweite und dritte Luecke: Sie liegen im Baustein ErtragBonus.
                    //
                    // Die VERGUETUNGSWAHL ist nur lesbar - sie zu wechseln schreibt
                    // ueber die Huelle und baut das Reiterblatt neu auf (und oeffnet
                    // dabei je nach Wahl den Verguetungsdialog). Das PV-PROJEKT ist
                    // setzbar: Es waehlt allein das Ziel des Knopfes
                    // „PV-Verguetungsdialog oeffnen…" und laedt nichts nach. Im
                    // Projektmodus zeigt die Maske seine Liste nicht (VV-Q7); dann hat
                    // das Feld keine Eintraege, und die Setzung wird benannt abgelehnt.
                    new KiDialogFeld("pv_verguetung", "KostenKomponenteKiSicht.PvVerguetung",
                                     KiDialogTexte.KvPvWahlName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KvPvWahlErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("pv_projekt", "KostenKomponenteKiSicht.PvProjekt",
                                     KiDialogTexte.KvPvProjektName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KvPvProjektErl,
                                     leerErlaubt: true),

                    // ---- Das Raster: je Position eine Zeile --------------------------
                    new KiDialogFeld("position", "KostenKomponenteKiSicht.Zeilen[].Bezeichnung",
                                     KiDialogTexte.KvPositionName, KiParameterTyp.Text,
                                     KiDialogTexte.KvPositionErl,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("bemessung", "KostenKomponenteKiSicht.Zeilen[].BemessungId",
                                     KiDialogTexte.KvBemessungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KvBemessungErl, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("satz", "KostenKomponenteKiSicht.Zeilen[].Satz",
                                     KiDialogTexte.KvSatzName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KvSatzErl,
                                     leerErlaubt: true, zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("nutzungsdauer", "KostenKomponenteKiSicht.Zeilen[].Nutzungsdauer",
                                     KiDialogTexte.KvNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KvNutzungsdauerErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("betrag", "KostenKomponenteKiSicht.Zeilen[].BetragText",
                                     KiDialogTexte.KvBetragName, KiParameterTyp.Text,
                                     KiDialogTexte.KvBetragErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN, nurLesen: true)
                });
        }

        /// <summary>
        /// Die Eigenschaft, aus der eine Rasterzeile ihren Klartextnamen bekommt -
        /// „Nutzungsdauer (Zubehoer)" statt „Nutzungsdauer 2".
        /// </summary>
        private const string ZEILENKENNZEICHEN = "Bezeichnung";

        /// <summary>
        /// Dasselbe fuer die Straenge einer Photovoltaikanlage: „Dach Sued · Module in
        /// Reihe" statt „Module in Reihe 2". Ein Strang ohne Bezeichner faellt auf
        /// seine Nummer zurueck - so, wie ihn auch die Maske zeigt.
        /// </summary>
        private const string STRANGKENNZEICHEN = "Bezeichner";

        /// <summary>
        /// ETAPPE E17 (V‑G11): das Kennzeichen einer Zeile der Wirkungsliste — „Wirkung 2:
        /// Komfort" (<c>WirkungKiZeile.Kennzeichen</c>, Nummer und Beschreibung).
        /// </summary>
        private const string WIRKUNGSKENNZEICHEN = "Kennzeichen";

        // =====================================================================
        // Form_Heizkessel_Bearbeiten  ->  HeizkesselKatalogDialog
        // =====================================================================

        /// <summary>
        /// Heizkessel bearbeiten — die SECHS Felder, die <c>HeizkesselKatalogDialog</c>
        /// sichtbar traegt, als Eigenschaften von
        /// <c>EPOS.UI.Dialoge.Erzeuger.HeizkesselKatalogDaten</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>NEUN Felder sind mit dem Anwenderentscheid vom 15.09.2026 entfallen</b>
        /// („Der Dialog ueber Button Bearbeiten soll keine Kosten und Emissionen
        /// enthalten"): <c>investitionskosten</c>, <c>wartungskosten</c>,
        /// <c>raumbedarf</c>, <c>nutzungsdauer</c>, <c>co2</c>, <c>so2</c>, <c>nox</c>,
        /// <c>co</c> und <c>staub</c>. Die Maske zeigt sie nicht mehr; gepflegt werden
        /// sie seit Phase 1 im AUFKLAPPER „Alle Daten" des Projektdialogs (Profil in
        /// <c>EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs</c>). Dorthin fuehrt
        /// KEIN Weg des Assistenten: Der Aufklapper ist keine angemeldete Katalogmaske,
        /// und ein Katalogfeld, das in der offenen Maske niemand sieht, waere genau die
        /// stille Setzung, die Fachkonzept 11.6 ausschliesst — der Anwender bestaetigte
        /// eine Zahl, die er nirgends nachlesen kann.
        /// </para>
        /// <para>
        /// Zwei Namen fallen auf, und beide sind Absicht: Die thermische Leistung heisst
        /// im Daten-Objekt <c>Ptherm</c> (nicht <c>ThLeistung</c>), und der
        /// Bereitschaftsverlust <c>Betriebsbereitschaftverlust</c> — so stehen sie in der
        /// Oberflaeche, und der Katalog schreibt keine zweite Schreibweise daneben.
        /// </para>
        /// </remarks>
        private static KiDialog Heizkessel()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.HEIZKESSEL,
                anzeigename: KiDialogTexte.MaskeHeizkessel,
                felder: new[]
                {
                    new KiDialogFeld("th_leistung", "HeizkesselKatalogDaten.Ptherm",
                                     KiDialogTexte.HkLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_gas", "HeizkesselKatalogDaten.Wirkungsgrad_Gas",
                                     KiDialogTexte.HkWgGasName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWgGasErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_oel", "HeizkesselKatalogDaten.Wirkungsgrad_Oel",
                                     KiDialogTexte.HkWgOelName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWgOelErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverlust",
                                     "HeizkesselKatalogDaten.Betriebsbereitschaftverlust",
                                     KiDialogTexte.HkBbVerlustName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkBbVerlustErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "HeizkesselKatalogDaten.Vorlauf",
                                     KiDialogTexte.HkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "HeizkesselKatalogDaten.Ruecklauf",
                                     KiDialogTexte.HkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Die uebrigen Eingabefelder der Maske (KI-F1b, KI-D-Q6) ----
                    new KiDialogFeld("name", "HeizkesselKatalogDaten.Name",
                                     KiDialogTexte.HkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.HkNameErl),
                    new KiDialogFeld("hersteller", "HeizkesselKatalogDaten.Firma",
                                     KiDialogTexte.HkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.HkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "HeizkesselKatalogDaten.Beschreibung",
                                     KiDialogTexte.HkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.HkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "HeizkesselKatalogDaten.Brennstoff",
                                     KiDialogTexte.HkTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.HkTraegerErl, leerErlaubt: true),
                    new KiDialogFeld("brennwert", "HeizkesselKatalogDaten.Brennwert",
                                     KiDialogTexte.HkBrennwertName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.HkBrennwertErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PV  ->  PhotovoltaikDialog
        // =====================================================================

        /// <summary>
        /// Photovoltaik — die Werte, die der Projektdialog neben der Geraetewahl
        /// fuehrt; sie stehen an der gewaehlten Zeile und, seit der Welle KI-F7, an
        /// der Sichtklasse <c>EPOS.UI.Dialoge.Erzeuger.PhotovoltaikKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <b>Das Daten-Objekt ist hier eine ZEILE und kein Dialogstand.</b> Der Dialog
        /// fuehrt eine Projektliste; angemeldet wird die GEWAEHLTE Zeile, und der Getter
        /// holt sie bei jedem Lesen neu. Ist keine gewaehlt, sind die Felder leer —
        /// derselbe Zustand, den der Anwender auf der Maske sieht. Die Sichtklasse
        /// reicht jede Zeileneigenschaft unveraendert durch und faellt mit der Zeile:
        /// Ohne gewaehlte Zeile meldet der Dialog gar keine Sicht an.
        /// <para>
        /// <b>Die zwei AUSLEGUNGSTEMPERATUREN sind PROJEKTWEIT</b> (Welle KI-F7,
        /// Anwenderentscheid 21.09.2026, KI-D-Q7). Sie stehen nicht an der Zeile,
        /// sondern in <c>Tab_Einstellungen.Ausleg_T_Kalt</c> und
        /// <c>…Ausleg_T_Heiss</c>; die Maske zeigt sie im Strangabschnitt und schreibt
        /// sie ueber <c>KonfigurationCtrl.AuslegungstemperaturenSchreiben</c> zurueck.
        /// Genau deshalb braucht <c>Form_PV</c> eine Sichtklasse: An der Zeile gibt es
        /// diese zwei Groessen nicht, und ein zweites Daten-Objekt je Maske kennt die
        /// Bruecke nicht. Wer eine der beiden setzt, aendert sie fuer JEDE Anlage des
        /// Projekts — das sagt ihre Erlaeuterung.
        /// </para>
        /// <para>
        /// <b>Die Maske besteht aus DREI Dateien</b> (Welle KI-F1): der Projektdialog
        /// selbst, der Baustein der MODELLFELDER (Rechenmodell,
        /// Wechselrichter-Wirkungsgrad, Systemverluste) und der Baustein der STRAENGE.
        /// Alle drei binden an dieselbe Zeile, deshalb steht hier EIN Katalogeintrag.
        /// </para>
        /// <para>
        /// <b>Die STRAENGE sind eine LISTE</b> und damit die zweite Maske mit Spalten
        /// (nach der Kostenverwaltung): Je Strangzeile wird aus jeder Spaltendeklaration
        /// ein gewoehnliches Feld, und das Zeilenkennzeichen ist der Bezeichner des
        /// Strangs („Dach Sued") statt seiner Nummer. Das MODUL und das GERAET eines
        /// Strangs fehlen dabei: Beide sind Verweise in kontextabhaengige Katalogauswahlen
        /// und stehen im Daten-Objekt allein als Id — dieselbe Regel wie ueberall.
        /// </para>
        /// <para>
        /// <b>Die Anlagenwerte des Wechselrichters haben einen EIGENEN Schluessel</b>
        /// (<c>WrNennleistungKw</c>, <c>WrEta10/50/100</c>). Bis zur Welle KI-F7 blieben
        /// sie ganz draussen: Sie stehen in einer eigenen Ueberlagerung mit eigenem
        /// Arbeitsstand und eigenem OK, auf der offenen Maske sieht der Anwender sie
        /// nicht, und ein Katalogfeld, das dort niemand nachlesen kann, waere genau die
        /// stille Setzung, die Fachkonzept 11.6 ausschliesst. Der ANWENDER hat sie am
        /// 21.09.2026 freigegeben (KI-D-Q7) — nicht als Felder dieser Maske, sondern
        /// nach der Regel „Baustein in eigenem Fenster bekommt einen Schluessel":
        /// <see cref="KiMaskennamen.PV_ANLAGENWERTE"/> meldet sich an, SOLANGE die
        /// Ueberlagerung offen steht, und ist damit genau dann lesbar, wenn der
        /// Anwender die Zahlen auch vor sich hat. Der Einwand von 11.6 bleibt also
        /// beantwortet, nicht uebergangen.
        /// </para>
        /// </remarks>
        private static KiDialog Photovoltaik()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PHOTOVOLTAIK,
                anzeigename: KiDialogTexte.MaskePv,
                felder: MitAlleDaten(new[]
                {
                    // ---- Die Anlage -------------------------------------------------
                    new KiDialogFeld("neigung", "PhotovoltaikKiSicht.Neigung",
                                     KiDialogTexte.PvNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("azimut", "PhotovoltaikKiSicht.Azimut",
                                     KiDialogTexte.PvAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "PhotovoltaikKiSicht.CarrierId",
                                     KiDialogTexte.PvTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvTraegerErl),
                    new KiDialogFeld("anzahl_module", "PhotovoltaikKiSicht.AnzahlModule",
                                     KiDialogTexte.PvAnzahlName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvAnzahlErl,
                                     leerErlaubt: true),

                    // ---- Die Modellfelder (PvModellFelder) --------------------------
                    // Das Rechenmodell ist auf der Maske ein AUSWAHLFELD mit den zwei
                    // Eintraegen „Einfach" und „Erweitert" - und seit dem
                    // Anwenderentscheid 21.09.2026 (KI-D-Q7) auch im Katalog eine WAHL
                    // statt eines Wahrheitswerts (KI-D-Q6: der Assistent trifft einen
                    // Eintrag ueber denselben Text, den der Anwender liest). Die
                    // Eigenschaft bleibt ein bool; die Schluessel der zwei Eintraege
                    // sind die Listenplaetze 0 und 1 der Maske.
                    new KiDialogFeld("modell_erweitert", "PhotovoltaikKiSicht.ModellErweitert",
                                     KiDialogTexte.PvModellName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvModellErl),
                    new KiDialogFeld("wr_wirkungsgrad", "PhotovoltaikKiSicht.WrWirkungsgrad",
                                     KiDialogTexte.PvWrWirkungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvWrWirkungsgradErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("systemverluste", "PhotovoltaikKiSicht.Systemverluste",
                                     KiDialogTexte.PvSystemverlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvSystemverlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),

                    // ---- Wechselrichter und Straenge (PvStraengeFelder) -------------
                    new KiDialogFeld("mit_wechselrichter", "PhotovoltaikKiSicht.MitWechselrichter",
                                     KiDialogTexte.PvMitWrName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PvMitWrErl),

                    // ---- Die zwei AUSLEGUNGSTEMPERATUREN (Welle KI-F7) --------------
                    // Sie stehen im Strangabschnitt, gehoeren aber dem PROJEKT
                    // (Tab_Einstellungen) - siehe Klassenkommentar.
                    new KiDialogFeld("auslegung_kalt", "PhotovoltaikKiSicht.AuslegungKalt",
                                     KiDialogTexte.PvAuslegKaltName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvAuslegKaltErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("auslegung_heiss", "PhotovoltaikKiSicht.AuslegungHeiss",
                                     KiDialogTexte.PvAuslegHeissName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvAuslegHeissErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    new KiDialogFeld("strang", "PhotovoltaikKiSicht.Straenge[].Bezeichner",
                                     KiDialogTexte.PvStrangName, KiParameterTyp.Text,
                                     KiDialogTexte.PvStrangErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_geraet", "PhotovoltaikKiSicht.Straenge[].Geraetenummer",
                                     KiDialogTexte.PvStrangGeraetName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangGeraetErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_mppt", "PhotovoltaikKiSicht.Straenge[].Mppt",
                                     KiDialogTexte.PvStrangMpptName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangMpptErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_module_reihe", "PhotovoltaikKiSicht.Straenge[].ModuleReihe",
                                     KiDialogTexte.PvStrangReiheName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangReiheErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_parallel", "PhotovoltaikKiSicht.Straenge[].StraengeParallel",
                                     KiDialogTexte.PvStrangParallelName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangParallelErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_neigung", "PhotovoltaikKiSicht.Straenge[].Neigung",
                                     KiDialogTexte.PvStrangNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_azimut", "PhotovoltaikKiSicht.Straenge[].Azimut",
                                     KiDialogTexte.PvStrangAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     zeilenkennzeichen: STRANGKENNZEICHEN)
                }, AlleDaten(ModulKatalogArt.Photovoltaik, PvModulkatalog(), "PhotovoltaikKiSicht")),
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PV_Anlagenwerte  ->  PvStraengeFelder (Ueberlagerung)
        // =====================================================================

        /// <summary>
        /// Die Ueberlagerung „Anlagenwerte" der Photovoltaik — die vier Kennwerte des
        /// Wechselrichters als Rueckfall ohne Strangzuordnung
        /// (<c>EPOS.UI.Dialoge.Erzeuger.PvAnlagenwerteKiSicht</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Was Fachkonzept 11.6 sagte.</b> Ein Katalogfeld, das der Anwender auf der
        /// offenen Maske nicht nachlesen kann, ist eine stille Setzung; die vier Werte
        /// stehen in einem eigenen Fenster mit eigenem Arbeitsstand und eigenem OK und
        /// blieben deshalb bis zur Welle KI-F6 ganz draussen.
        /// </para>
        /// <para>
        /// <b>Was der ANWENDER entschieden hat</b> (21.09.2026, KI-D-Q7): Sie werden
        /// freigegeben — nicht als vier weitere Felder von <c>Form_PV</c>, sondern als
        /// EIGENE Maske nach der Regel „Baustein in eigenem Fenster bekommt einen
        /// Schluessel". Damit bleibt der Einwand von 11.6 beantwortet: Die Maske meldet
        /// sich an, SOLANGE die Ueberlagerung offen steht, und liest und schreibt dabei
        /// den ARBEITSSTAND des Fensters (<c>_nenn</c>, <c>_eta10/50/100</c>) — genau
        /// die Zahlen, die vor dem Anwender stehen. „OK" uebernimmt sie in die Anlage,
        /// „Abbrechen" verwirft sie; der Assistent aendert daran nichts.
        /// </para>
        /// <para>
        /// <b>Ihr Oeffnungsziel ist das von <c>Form_PV</c></b>: Das Fenster geht aus dem
        /// Abschnitt „Wechselrichter und Straenge" auf und hat keinen eigenen Weg im
        /// Menue — dieselbe Bauart wie bei den Masken der Simulationskonfiguration.
        /// </para>
        /// <para>
        /// <b>Null heisst „nicht bekannt".</b> Die Maske nimmt 0 als „nicht bekannt" an
        /// und schreibt dann NULL in die Anlage; deshalb sind alle vier
        /// <c>leerErlaubt</c>. Die drei Wirkungsgrade sind Faktoren zwischen 0 und 1,
        /// keine Prozentzahlen — das sagt ihre Erlaeuterung, denn eine „97" traefe sonst
        /// wortlos die Grenze der Maske.
        /// </para>
        /// </remarks>
        private static KiDialog PvAnlagenwerte()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PV_ANLAGENWERTE,
                anzeigename: KiDialogTexte.MaskePvAnlagenwerte,
                felder: new[]
                {
                    new KiDialogFeld("wr_nennleistung", "PvAnlagenwerteKiSicht.Nennleistung",
                                     KiDialogTexte.PvaNennleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvaNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("wr_eta10", "PvAnlagenwerteKiSicht.Eta10",
                                     KiDialogTexte.PvaEta10Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvaEtaErl, leerErlaubt: true),
                    new KiDialogFeld("wr_eta50", "PvAnlagenwerteKiSicht.Eta50",
                                     KiDialogTexte.PvaEta50Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvaEtaErl, leerErlaubt: true),
                    new KiDialogFeld("wr_eta100", "PvAnlagenwerteKiSicht.Eta100",
                                     KiDialogTexte.PvaEta100Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvaEtaErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PufferSp_Bearbeiten  ->  PufferSpKatalogDialog
        // =====================================================================

        /// <summary>
        /// Pufferspeicher bearbeiten — das eine Feld der Knopfpruefung
        /// (<c>EPOS.UI.Dialoge.Erzeuger.PufferSpKatalogDaten</c>).
        /// </summary>
        /// <remarks>
        /// <b>Nur ein Feld — und das ist kein Versehen.</b> Die uebrigen Eingaben der
        /// Maske (Hersteller, Speichertyp, Bereitschaftsverluste) liefen im Vorlaeufer
        /// ueber stille Parser und gehoeren nach Fachkonzept 11.7 erst nach ihrer
        /// Umstellung in den Katalog. Ob die Razor-Fassung das anders sieht, ist eine
        /// fachliche Frage mit eigener Abnahme.
        /// <para>
        /// <b>Die INVESTITIONSKOSTEN kommen nicht mehr in Frage.</b> Sie verlassen die
        /// Maske mit dem Anwenderentscheid vom 15.09.2026 (kein Kosteneintrag im
        /// Bearbeiten-Dialog) und sind seither im Aufklapper „Alle Daten" des
        /// Projektdialogs zu pflegen — dort fuehrt sie seit Welle #458 (Stufe 2) auch
        /// der Dialogkatalog, als Feld des Aufklappers der Maske Form_PufferSp.
        /// Dieser Editor hat sie nie gefuehrt; hier steht nur, dass das so bleibt.
        /// </para>
        /// </remarks>
        private static KiDialog Pufferspeicher()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PUFFERSPEICHER,
                anzeigename: KiDialogTexte.MaskePufferSp,
                felder: new[]
                {
                    new KiDialogFeld("gesamtvolumen", "PufferSpKatalogDaten.Gesamtvolumen",
                                     KiDialogTexte.PspVolumenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspVolumenErl,
                                     einheit: KiDialogTexte.EINHEIT_LITER, leerErlaubt: true),

                    // ---- Die uebrigen Eingabefelder der Maske (KI-F1b, KI-D-Q6) ----
                    new KiDialogFeld("name", "PufferSpKatalogDaten.Name",
                                     KiDialogTexte.PspNameName, KiParameterTyp.Text,
                                     KiDialogTexte.PspNameErl),
                    new KiDialogFeld("hersteller", "PufferSpKatalogDaten.Firma",
                                     KiDialogTexte.PspFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.PspFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("speichertyp", "PufferSpKatalogDaten.SpeichertypIndex",
                                     KiDialogTexte.PspSpeichertypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PspSpeichertypErl, leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverluste",
                                     "PufferSpKatalogDaten.Bereitschaftsverluste",
                                     KiDialogTexte.PspVerlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspVerlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH_24H, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_WP  ->  WaermepumpeStammDialog
        // =====================================================================

        /// <summary>
        /// Waermepumpen verwalten — das eine Feld der Knopfpruefung
        /// (<c>EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Begruendung fuer den Feldumfang wie beim Pufferspeicher (Fachkonzept 11.7):
        /// <c>btn_Speichern_Click</c> prueft ausschliesslich die Modulkosten, und zwar
        /// mit <c>leerErlaubt: false</c>.
        /// </para>
        /// <para>
        /// <b>Die MODULKOSTEN sind seit dem Anwenderentscheid 21.09.2026 (KI-D-Q7) nur
        /// lesbar.</b> Der Katalog fuehrte sie setzbar, <c>WaermepumpeStammFelder</c>
        /// zeigt sie aber seit W14a-O-1 als Lesewert mit Herleitungszeile: Gepflegt
        /// werden sie in der Kostenverwaltung. Ein Setzer bot damit an, eine Zahl zu
        /// aendern, die auf der Maske kein Eingabefeld hat — dieselbe Lage und
        /// dieselbe Antwort wie bei <c>Form_WP_Anlage</c>, wo sie schon vorher
        /// <c>nurLesen</c> war.
        /// </para>
        /// </remarks>
        private static KiDialog Waermepumpe()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMEPUMPE,
                anzeigename: KiDialogTexte.MaskeWp,
                felder: new[]
                {
                    new KiDialogFeld("modulkosten", "WaermepumpeStammDaten.Modulkosten",
                                     KiDialogTexte.WpModulkostenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpModulkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: false,
                                     nurLesen: true),

                    // ---- Die Stammfelder des Bausteins (KI-F1b, KI-D-Q6) ----------
                    new KiDialogFeld("name", "WaermepumpeStammDaten.Name",
                                     KiDialogTexte.WpNameName, KiParameterTyp.Text,
                                     KiDialogTexte.WpNameErl),
                    new KiDialogFeld("hersteller", "WaermepumpeStammDaten.Firma",
                                     KiDialogTexte.WpaFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "WaermepumpeStammDaten.Beschreibung",
                                     KiDialogTexte.WpaBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("typ", "WaermepumpeStammDaten.Typ",
                                     KiDialogTexte.WpaTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaTypErl, leerErlaubt: true),
                    new KiDialogFeld("leistungsstufen", "WaermepumpeStammDaten.Regelung",
                                     KiDialogTexte.WpaRegelungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaRegelungErl, leerErlaubt: true),
                    new KiDialogFeld("aufstellung", "WaermepumpeStammDaten.Aufstellung",
                                     KiDialogTexte.WpaAufstellungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaAufstellungErl, leerErlaubt: true),
                    new KiDialogFeld("baujahr", "WaermepumpeStammDaten.Baujahr",
                                     KiDialogTexte.WpaBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBaujahrErl, leerErlaubt: true),
                    new KiDialogFeld("nennleistung", "WaermepumpeStammDaten.Nennleistung",
                                     KiDialogTexte.WpaNennleistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("heizstab_leistung", "WaermepumpeStammDaten.Heizstab",
                                     KiDialogTexte.WpaHeizstabLeistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaHeizstabLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kuehlleistung", "WaermepumpeStammDaten.Kuehlleistung",
                                     KiDialogTexte.WpKuehlleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpKuehlleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("ok", "btn_Beenden", KiDialogTexte.KnopfOk)
                });
        }

        // =====================================================================
        // StromspeicherAuslegung  ->  StromspeicherAuslegungSeite
        // =====================================================================

        /// <summary>
        /// Die FUENFTE Maske (Auftrag #200, Anwenderentscheid KI‑D‑Q3): die
        /// Stromspeicher-Ansicht — siebzehn Felder aus
        /// <c>EPOS.UI.Seiten.Strom.StromspeicherKiSicht</c>; das siebzehnte,
        /// <c>peak_ziel_adaptiv</c>, kam mit der kausalen Ratsche dazu
        /// (Spezifikation 5.1.1, Auftrag #215).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum gerade sie und warum als erste Nicht-Katalogmaske.</b> Sie ist die
        /// Ansicht mit der haeufigsten unverstandenen Meldung des Hauses („Die Flotte hat
        /// im gesamten Zeitraum weder geladen noch entladen", Kennung
        /// <c>FLOTTE_ARBEITSLOS</c>) — und die Antwort darauf steht in ihren eigenen
        /// Zaehlern. Ohne Feldwerte kann der Assistent sie nur allgemein erklaeren; mit
        /// ihnen nennt er das Peak-Ziel, die Netzladefreigabe und die Zahl der
        /// Intervalle, in denen der Ladedeckel auf 0 stand.
        /// </para>
        /// <para>
        /// <b>Sechs Felder sind ABGELEITET und nur lesbar</b> (Diagnose und Ergebnis der
        /// letzten Bewertung). Sie sind trotzdem Felder und keine zweite Gattung: Die
        /// Deklaration traegt Anzeigename, Art und Erlaeuterung, und der Feldblock zeigt
        /// sie neben den Eingaben — der Anwender liest sie auf derselben Ansicht ebenso
        /// nebeneinander. Die Setzseite bleibt bei ihnen leer, und die Stufe S3 wird sie
        /// darum ablehnen; das ist der Unterschied, den sie brauchen.
        /// </para>
        /// <para>
        /// <b>Eine Flotte hat MEHRERE Einheiten, ein Maskenfeld traegt EINEN Wert</b>
        /// (<see cref="KiDialogFeld"/>). Die drei Summenfelder nennen deshalb die Flotte
        /// als Ganzes, und <c>einheiten_liste</c> traegt die Aufstellung je Einheit als
        /// Text — genau die Zeile, die die Ansicht zeigt. Eine Deklaration je Einheit
        /// ginge nicht: Ihre Zahl steht erst zur Laufzeit fest.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> Die Ansicht fuehrt „Berechnen", „Peak-Ziel
        /// bestimmen…" und „Speichern" — das sind rechnende und datenbankwirksame
        /// Aktionen der Stufen 2 und 3 des Aufgabensteuerungskonzepts. Sie gehoeren in
        /// das Aktionsregister mit Bestaetigung und Sicherungspunkt (Auftrag #201) und
        /// nicht in eine Knopfliste, die eine Formularaktion ausloest.
        /// </para>
        /// </remarks>
        private static KiDialog Stromspeicherauslegung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                anzeigename: KiDialogTexte.MaskeSpeicherauslegung,
                felder: new[]
                {
                    // ---- Wo der Anwender steht (Auftrag #224) -----------------------
                    new KiDialogFeld("schritt", "StromspeicherKiSicht.Schritt",
                                     KiDialogTexte.SpaSchrittName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaSchrittErl, leerErlaubt: true),

                    // ---- Die Flotte -------------------------------------------------
                    new KiDialogFeld("einheiten", "StromspeicherKiSicht.Einheitenzahl",
                                     KiDialogTexte.SpaEinheitenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SpaEinheitenErl),
                    new KiDialogFeld("einheiten_liste", "StromspeicherKiSicht.EinheitenListe",
                                     KiDialogTexte.SpaListeName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaListeErl, leerErlaubt: true),
                    new KiDialogFeld("kapazitaet_gesamt", "StromspeicherKiSicht.KapazitaetGesamtKWh",
                                     KiDialogTexte.SpaKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),
                    new KiDialogFeld("ladeleistung_gesamt", "StromspeicherKiSicht.LadeleistungGesamtKw",
                                     KiDialogTexte.SpaLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("entladeleistung_gesamt",
                                     "StromspeicherKiSicht.EntladeleistungGesamtKw",
                                     KiDialogTexte.SpaEntladeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaEntladeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),

                    // ---- Die Betriebsfuehrung ---------------------------------------
                    new KiDialogFeld("betriebsziel", "StromspeicherKiSicht.Betriebsziel",
                                     KiDialogTexte.SpaBetriebszielName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SpaBetriebszielErl),
                    new KiDialogFeld("peak_ziel", "StromspeicherKiSicht.PeakZielKw",
                                     KiDialogTexte.SpaPeakZielName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaPeakZielErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("peak_ziel_adaptiv", "StromspeicherKiSicht.PeakZielAdaptiv",
                                     KiDialogTexte.SpaAdaptivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaAdaptivErl),
                    new KiDialogFeld("netzladung", "StromspeicherKiSicht.NetzladungErlaubt",
                                     KiDialogTexte.SpaNetzladungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaNetzladungErl),
                    new KiDialogFeld("start_soc", "StromspeicherKiSicht.StartSocProzent",
                                     KiDialogTexte.SpaStartSocName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaStartSocErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("peak_reserve", "StromspeicherKiSicht.PeakReserveKWh",
                                     KiDialogTexte.SpaReserveName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaReserveErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),

                    // ---- Station 4 „Optimierung" (Auftrag #224) ---------------------
                    new KiDialogFeld("groessen_optimieren", "StromspeicherKiSicht.GroessenOptimieren",
                                     KiDialogTexte.SpaSucheName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaSucheErl),
                    // Seit Auftrag #247 (SD-E-10) sagt die METHODE, was variiert wird —
                    // Groesse oder Stueckzahl. Nur lesend: Sie haengt an zwei Feldern
                    // desselben Standes, und sie zu setzen ist die Aufgabe der
                    // Optionsgruppe, die beide gleichzieht.
                    new KiDialogFeld("suchmethode", "StromspeicherKiSicht.Suchmethode",
                                     KiDialogTexte.SpaMethodeName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaMethodeErl),
                    new KiDialogFeld("feinraster", "StromspeicherKiSicht.Feinraster",
                                     KiDialogTexte.SpaFeinrasterName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaFeinrasterErl),
                    new KiDialogFeld("maximale_kandidaten", "StromspeicherKiSicht.MaximaleKandidaten",
                                     KiDialogTexte.SpaMaxKandidatenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SpaMaxKandidatenErl),
                    new KiDialogFeld("kandidatenzahl", "StromspeicherKiSicht.Kandidatenzahl",
                                     KiDialogTexte.SpaKandidatenzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SpaKandidatenzahlErl),

                    // ---- Das beste Ergebnis der Suche (nur lesend) ------------------
                    new KiDialogFeld("bestes_kapitalwert", "StromspeicherKiSicht.BesterKapitalwertEuro",
                                     KiDialogTexte.SpaBestwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaBestwertErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("bestes_kapazitaet", "StromspeicherKiSicht.BesteKapazitaetKWh",
                                     KiDialogTexte.SpaBestkapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaBestkapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH, leerErlaubt: true),
                    new KiDialogFeld("bestes_ersparnis", "StromspeicherKiSicht.BesteErsparnisEuroJahr",
                                     KiDialogTexte.SpaBestersparnisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaBestersparnisErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("bestes_phase", "StromspeicherKiSicht.BestePhase",
                                     KiDialogTexte.SpaBestphaseName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaBestphaseErl, leerErlaubt: true),

                    // ---- Die Diagnose (nur lesend) ----------------------------------
                    new KiDialogFeld("diagnose_arbeitslos", "StromspeicherKiSicht.Arbeitslos",
                                     KiDialogTexte.SpaArbeitslosName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaArbeitslosErl),
                    new KiDialogFeld("diagnose_gruende", "StromspeicherKiSicht.DiagnoseGruende",
                                     KiDialogTexte.SpaGruendeName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaGruendeErl, leerErlaubt: true),
                    new KiDialogFeld("pruefhinweise", "StromspeicherKiSicht.Pruefhinweise",
                                     KiDialogTexte.SpaHinweiseName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaHinweiseErl, leerErlaubt: true),

                    // ---- Das Ergebnis der letzten Bewertung (nur lesend) -------------
                    new KiDialogFeld("ergebnis_bezugsspitze",
                                     "StromspeicherKiSicht.BezugsspitzeKw",
                                     KiDialogTexte.SpaSpitzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaSpitzeErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("ergebnis_netzbezug", "StromspeicherKiSicht.NetzbezugKWh",
                                     KiDialogTexte.SpaNetzbezugName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaNetzbezugErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH, leerErlaubt: true),
                    new KiDialogFeld("ergebnis_kapitalwert", "StromspeicherKiSicht.KapitalwertEuro",
                                     KiDialogTexte.SpaKapitalwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaKapitalwertErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),

                    // =================================================================
                    //  Welle KI-F6: die STATIONEN 1 bis 4 der Ansicht
                    // =================================================================
                    //
                    // Bis hierher fuehrte die Ansicht nur, was auf Station 1 als SUMME
                    // und auf Station 4 als Suchergebnis steht. Die Bloecke selbst -
                    // Einheiteneditor, Datenquellen und Kostensaetze, Betriebsfuehrung,
                    // Netzgrenzen, Prognose und Suchraum - blieben aussen vor
                    // (Statuszeile #420, Punkt b). Sie kommen jetzt als FELDER DER
                    // ANSICHT dazu und nicht als eigene Masken: Keiner der sieben
                    // Bausteine macht ein Fenster auf, alle sieben stehen auf einem
                    // ihrer fuenf Blaetter - dieselbe Begruendung wie beim Reiter
                    // „Stromspeicher" der Simulationsansicht (Welle KI-F2).

                    // ---- Station 1: je EINHEIT eine Zeile ---------------------------
                    //
                    // Eine SAMMLUNG und keine Einzelfelder: Wie viele Einheiten eine
                    // Flotte fuehrt, steht erst zur Laufzeit fest. Die RAINFLOW-Kurve
                    // bleibt draussen - eine Tabelle IN der Zeile mit eigenem Editor.
                    new KiDialogFeld("einheit_name", "StromspeicherKiSicht.Einheitenzeilen[].Name",
                                     KiDialogTexte.FleNameName, KiParameterTyp.Text,
                                     KiDialogTexte.FleNameErl,
                                     leerErlaubt: true, zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_kapazitaet",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Kapazitaet",
                                     KiDialogTexte.FleKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_ladeleistung",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Ladeleistung",
                                     KiDialogTexte.FleLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_entladeleistung",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Entladeleistung",
                                     KiDialogTexte.FleEntladeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleEntladeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_ladewirkungsgrad",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Ladewirkungsgrad",
                                     KiDialogTexte.FleLadewirkungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleLadewirkungsgradErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_entladewirkungsgrad",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Entladewirkungsgrad",
                                     KiDialogTexte.FleEntladewirkungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleEntladewirkungsgradErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_soc_min",
                                     "StromspeicherKiSicht.Einheitenzeilen[].SocMin",
                                     KiDialogTexte.FleSocMinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleSocMinErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_soc_max",
                                     "StromspeicherKiSicht.Einheitenzeilen[].SocMax",
                                     KiDialogTexte.FleSocMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleSocMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_soc_start",
                                     "StromspeicherKiSicht.Einheitenzeilen[].SocStart",
                                     KiDialogTexte.FleSocStartName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleSocStartErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_peak_reserve",
                                     "StromspeicherKiSicht.Einheitenzeilen[].PeakReserve",
                                     KiDialogTexte.FlePeakReserveName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FlePeakReserveErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_hilfsverbrauch",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Hilfsverbrauch",
                                     KiDialogTexte.FleHilfsverbrauchName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleHilfsverbrauchErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_grenzverschleiss",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Grenzverschleiss",
                                     KiDialogTexte.FleGrenzverschleissName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleGrenzverschleissErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_eigene_kosten",
                                     "StromspeicherKiSicht.Einheitenzeilen[].EigeneKosten",
                                     KiDialogTexte.FleEigeneKostenName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.FleEigeneKostenErl,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_investition_fix",
                                     "StromspeicherKiSicht.Einheitenzeilen[].InvestitionFix",
                                     KiDialogTexte.FleInvestFixName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleInvestFixErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_investition_kapazitaet",
                                     "StromspeicherKiSicht.Einheitenzeilen[].InvestitionProKWh",
                                     KiDialogTexte.FleInvestKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleInvestKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_investition_leistung",
                                     "StromspeicherKiSicht.Einheitenzeilen[].InvestitionProKw",
                                     KiDialogTexte.FleInvestLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleInvestLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_betrieb_fix",
                                     "StromspeicherKiSicht.Einheitenzeilen[].BetriebFix",
                                     KiDialogTexte.FleOpexFixName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleOpexFixErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_A,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_betrieb_kapazitaet",
                                     "StromspeicherKiSicht.Einheitenzeilen[].BetriebProKWh",
                                     KiDialogTexte.FleOpexKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleOpexKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH_A,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_betrieb_leistung",
                                     "StromspeicherKiSicht.Einheitenzeilen[].BetriebProKw",
                                     KiDialogTexte.FleOpexLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleOpexLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_durchsatzkosten",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Durchsatzkosten",
                                     KiDialogTexte.FleDurchsatzkostenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleDurchsatzkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_ersatzkosten",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Ersatzkosten",
                                     KiDialogTexte.FleErsatzkostenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleErsatzkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_ersatzintervall",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Ersatzintervall",
                                     KiDialogTexte.FleErsatzintervallName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.FleErsatzintervallErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),
                    new KiDialogFeld("einheit_restwert",
                                     "StromspeicherKiSicht.Einheitenzeilen[].Restwert",
                                     KiDialogTexte.FleRestwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.FleRestwertErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO,
                                     zeilenkennzeichen: EINHEITENKENNZEICHEN),

                    // ---- Station 2: Datenquellen und Kostensaetze -------------------
                    new KiDialogFeld("lastquelle", "StromspeicherKiSicht.Lastquelle",
                                     KiDialogTexte.Spa2LastquelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa2LastquelleErl),
                    new KiDialogFeld("pv_quelle", "StromspeicherKiSicht.PvQuelle",
                                     KiDialogTexte.Spa2PvQuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa2PvQuelleErl),
                    new KiDialogFeld("preisquelle", "StromspeicherKiSicht.Preisquelle",
                                     KiDialogTexte.Spa2PreisquelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa2PreisquelleErl),
                    new KiDialogFeld("modelljahr_zuordnen",
                                     "StromspeicherKiSicht.EposModelljahrZuordnen",
                                     KiDialogTexte.Spa2ModelljahrName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.Spa2ModelljahrErl),
                    new KiDialogFeld("investitionsquelle", "StromspeicherKiSicht.Investitionsquelle",
                                     KiDialogTexte.Spa2InvestquelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa2InvestquelleErl),
                    new KiDialogFeld("betriebsquelle", "StromspeicherKiSicht.Betriebsquelle",
                                     KiDialogTexte.Spa2BetriebsquelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa2BetriebsquelleErl),
                    new KiDialogFeld("investition_leistung", "StromspeicherKiSicht.InvestitionProKw",
                                     KiDialogTexte.Spa2InvestKwName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2InvestKwErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW),
                    new KiDialogFeld("investition_kapazitaet", "StromspeicherKiSicht.InvestitionProKWh",
                                     KiDialogTexte.Spa2InvestKwhName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2InvestKwhErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH),
                    new KiDialogFeld("betrieb_leistung", "StromspeicherKiSicht.BetriebProKw",
                                     KiDialogTexte.Spa2BetriebKwName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2BetriebKwErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A),
                    new KiDialogFeld("betrieb_kapazitaet", "StromspeicherKiSicht.BetriebProKWh",
                                     KiDialogTexte.Spa2BetriebKwhName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2BetriebKwhErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH_A),
                    new KiDialogFeld("betrieb_entladen", "StromspeicherKiSicht.BetriebProKWhEntladen",
                                     KiDialogTexte.Spa2BetriebEntladenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2BetriebEntladenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH),
                    new KiDialogFeld("leistungspreis", "StromspeicherKiSicht.Leistungspreis",
                                     KiDialogTexte.Spa2LeistungspreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2LeistungspreisErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A),
                    new KiDialogFeld("energie_ausgleich", "StromspeicherKiSicht.EnergieAusgleich",
                                     KiDialogTexte.Spa2AusgleichName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2AusgleichErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH, leerErlaubt: true),
                    new KiDialogFeld("kalkulationszins", "StromspeicherKiSicht.KalkulationszinsProzent",
                                     KiDialogTexte.Spa2ZinsName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2ZinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("jahresprojektion", "StromspeicherKiSicht.Jahresprojektion",
                                     KiDialogTexte.Spa2ProjektionsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa2ProjektionsartErl),
                    new KiDialogFeld("projektjahre", "StromspeicherKiSicht.Projektjahre",
                                     KiDialogTexte.Spa2ProjektjahreName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.Spa2ProjektjahreErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR),
                    new KiDialogFeld("restwert_studie", "StromspeicherKiSicht.RestwertStudieEuro",
                                     KiDialogTexte.Spa2RestwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa2RestwertErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO),

                    // ---- Station 3: Betriebsfuehrung, Netz und Prognose --------------
                    new KiDialogFeld("verteilung", "StromspeicherKiSicht.Verteilung",
                                     KiDialogTexte.Spa3VerteilungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa3VerteilungErl),
                    new KiDialogFeld("erzeuger_prioritaet", "StromspeicherKiSicht.ErzeugerPrioritaet",
                                     KiDialogTexte.Spa3PrioritaetName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa3PrioritaetErl),
                    new KiDialogFeld("batterieexport", "StromspeicherKiSicht.BatterieexportErlaubt",
                                     KiDialogTexte.Spa3ExportName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.Spa3ExportErl),
                    new KiDialogFeld("netzbezug_grenze", "StromspeicherKiSicht.NetzbezugGrenzeKw",
                                     KiDialogTexte.Spa3BezugsgrenzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa3BezugsgrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("netzeinspeisung_grenze",
                                     "StromspeicherKiSicht.NetzeinspeisungGrenzeKw",
                                     KiDialogTexte.Spa3EinspeisegrenzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa3EinspeisegrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("informationsstand", "StromspeicherKiSicht.Informationsstand",
                                     KiDialogTexte.Spa3InformationsstandName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa3InformationsstandErl),
                    new KiDialogFeld("planungshorizont",
                                     "StromspeicherKiSicht.PlanungshorizontIntervalle",
                                     KiDialogTexte.Spa3PlanungshorizontName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.Spa3PlanungshorizontErl),
                    new KiDialogFeld("neuplanung", "StromspeicherKiSicht.NeuplanungAlleIntervalle",
                                     KiDialogTexte.Spa3NeuplanungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.Spa3NeuplanungErl),
                    new KiDialogFeld("endbedingung", "StromspeicherKiSicht.Endbedingung",
                                     KiDialogTexte.Spa3EndbedingungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa3EndbedingungErl),
                    new KiDialogFeld("prognose_fallback",
                                     "StromspeicherKiSicht.PrognoseFallbackErlaubt",
                                     KiDialogTexte.Spa3FallbackName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.Spa3FallbackErl),

                    // ---- Station 4: je SUCHACHSE eine Zeile --------------------------
                    new KiDialogFeld("achse_variieren",
                                     "StromspeicherKiSicht.Suchachsen[].Variieren",
                                     KiDialogTexte.Spa4VariierenName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.Spa4VariierenErl,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_quelle", "StromspeicherKiSicht.Suchachsen[].Quelle",
                                     KiDialogTexte.Spa4QuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.Spa4QuelleErl,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_kapazitaet_von",
                                     "StromspeicherKiSicht.Suchachsen[].KapazitaetVon",
                                     KiDialogTexte.Spa4KapazitaetVonName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa4KapazitaetVonErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_kapazitaet_bis",
                                     "StromspeicherKiSicht.Suchachsen[].KapazitaetBis",
                                     KiDialogTexte.Spa4KapazitaetBisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa4KapazitaetBisErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_leistung_von",
                                     "StromspeicherKiSicht.Suchachsen[].LeistungVon",
                                     KiDialogTexte.Spa4LeistungVonName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa4LeistungVonErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_leistung_bis",
                                     "StromspeicherKiSicht.Suchachsen[].LeistungBis",
                                     KiDialogTexte.Spa4LeistungBisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.Spa4LeistungBisErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_anzahl_von",
                                     "StromspeicherKiSicht.Suchachsen[].AnzahlVon",
                                     KiDialogTexte.Spa4AnzahlVonName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.Spa4AnzahlVonErl,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN),
                    new KiDialogFeld("achse_anzahl_bis",
                                     "StromspeicherKiSicht.Suchachsen[].AnzahlBis",
                                     KiDialogTexte.Spa4AnzahlBisName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.Spa4AnzahlBisErl,
                                     zeilenkennzeichen: ACHSENKENNZEICHEN)
                });
        }

        /// <summary>
        /// Die Eigenschaft, aus der eine Einheitenzeile der Flotte ihren Klartextnamen
        /// bekommt — „Speicher Halle · Kapazitaet" statt „Kapazitaet 2" (Welle KI-F6).
        /// </summary>
        private const string EINHEITENKENNZEICHEN = "Name";

        /// <summary>
        /// Dasselbe fuer die Suchkarten der Station „Optimierung": Ihr Name ist der
        /// Name der Einheit, die die Achse ersetzt — so beschriftet der Block sie auch.
        /// </summary>
        private const string ACHSENKENNZEICHEN = "Achse";

        // =====================================================================
        // Simulation  ->  SimulationSeite
        // =====================================================================

        /// <summary>
        /// Die SECHSTE Maske (Auftrag #221, Anwenderentscheid KI‑D‑E‑1): die Ansicht
        /// „Simulation" — sechsundvierzig Felder aus
        /// <c>EPOS.UI.Seiten.Simulation.SimulationKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum sie dazukommt.</b> Der Anwender hat am 11.09.2026 gemeldet: „Die
        /// KI-Buttons haben keine unterschiedliche Funktion im Kontext. Daher ist es
        /// nicht sinnvoll, auf einer Sicht zwei KI-Buttons zu sehen. Es muss einen
        /// Kontext in der KI-Funktion der zweiten Sicht geben." Bis hierher unterschied
        /// die Simulationsansicht vom Hauptfenster genau EINE Zeichenkette — der Bereich
        /// aus dem Hilfeschluessel. Sie meldet jetzt an, WAS auf ihr steht.
        /// </para>
        /// <para>
        /// <b>Drei Gruppen, drei Rollen.</b> Schritt ① traegt die KASKADE (lesend — sie
        /// ist eine Reihenfolge und kein Eingabefeld), danach stehen die FUENF
        /// LAUFPARAMETER (lesbar UND setzbar; sie sind die einzigen Zahlen der Ansicht,
        /// die der Anwender selbst eintraegt), und Schritt ③ traegt die KENNZAHLEN des
        /// Laufs (lesend — sie sind gerechnet). Die Setzseite folgt wie ueberall der
        /// Schreibbarkeit der Eigenschaft (<c>KiFeldzugang.Setzbar</c>); eine zweite
        /// Liste „was ist setzbar" gibt es nicht.
        /// </para>
        /// <para>
        /// <b>Die Betriebsart ist eine GANZZAHL und keine Aufzaehlung.</b> 0, 1 und 2
        /// stehen so in <c>Tab_Einstellungen</c> (Vorlaeufer <c>RadioButton.Tag</c>,
        /// <c>bhkwSimulationsArt</c>); ein Aufzaehlungstyp brauchte eine zweite
        /// Namensliste, die es nur fuer den Assistenten gaebe. Was 0, 1 und 2 bedeuten,
        /// steht in der Erlaeuterung.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> „Simulation starten ▶" und „Ergebnis speichern" sind
        /// rechnende bzw. datenbankwirksame Aktionen der Stufen 2 und 3 und gehoeren in
        /// das Aktionsregister mit Bestaetigung und Sicherungspunkt — dieselbe
        /// Begruendung wie bei der Stromspeicher-Ansicht.
        /// </para>
        /// <para>
        /// <b>Welle KI-F2: die BLAETTER der Ansicht kommen dazu.</b> Einundzwanzig
        /// Einstellwerte des Reiters „Stromspeicher" von Schritt ③ und der Lesepunkt
        /// aus der Fusszeile von Schritt ①. Sie gehen nicht auf, sie STEHEN auf der
        /// Ansicht — eine Maske ist, was offen ist, und eine eigene Maske je Reiterblatt
        /// waere eine Maske, die der Anwender nie oeffnet. Jedes dieser Felder schreibt
        /// SOFORT, ueber denselben Weg wie das Feld auf dem Bildschirm
        /// (<c>SpeicherfeldSchreiben</c> je Feldschluessel bzw.
        /// <c>LesepunktSchreiben</c>); der Speicherknopf, den
        /// <c>dialog_speichern</c> druecken koennte, fehlt hier weiterhin.
        /// </para>
        /// <para>
        /// <b>Die PREISREIHE ist eine Wahl</b> (KI‑F1b): Ihre Einträge hängen an der
        /// Preisquelle und kommen bei jedem Zugriff frisch aus dem Stand
        /// (<c>SpeicherPreisreiheWahl</c>); eine Id, die die Liste gerade nicht führt,
        /// weist die Sichtklasse ab.
        /// </para>
        /// <para>
        /// <b>Welle #458: der Kühlschalter und die Werte JE ANLAGE</b> — die
        /// Projekteinstellung „Kühlung rechnen" und, für die gewählte Karte
        /// (<c>quellanlage</c>), Wärmequelle, konstante Quelltemperatur, WP-Priorität und
        /// Betriebsmodus. Sie gehen dieselben Wege wie die Überlagerungen der Karte; eine
        /// offene Überlagerung hält die Setzung benannt an, weil sie ihren Stand von
        /// vorhin mit OK zurückschriebe.
        /// </para>
        /// </remarks>
        private static KiDialog Simulation()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SIMULATION,
                anzeigename: KiDialogTexte.MaskeSimulation,
                felder: new[]
                {
                    // ---- Wo der Anwender steht --------------------------------------
                    new KiDialogFeld("schritt", "SimulationKiSicht.Ansichtsschritt",
                                     KiDialogTexte.SimSchrittName, KiParameterTyp.Text,
                                     KiDialogTexte.SimSchrittErl, leerErlaubt: true),
                    // Welle #458, Stufe 2: das Blatt ist ein WAHLFELD (Satzwahl - es
                    // wechselt, was gezeigt wird), und die Schalter der Anzeige des
                    // offenen Blattes sind eine SPALTE, je Schalter eine Zeile.
                    new KiDialogFeld("reiter", "SimulationKiSicht.Reiter",
                                     KiDialogTexte.SimReiterName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SimReiterErl, leerErlaubt: true, satzwahl: true),
                    new KiDialogFeld("anzeige", "SimulationKiSicht.Ergebnisschalter[].An",
                                     KiDialogTexte.SimAnzeigeName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimAnzeigeErl, zeilenkennzeichen: "Name"),

                    // ---- Schritt ① : Kaskade und Reihenfolge (nur lesend) -----------
                    new KiDialogFeld("kaskade", "SimulationKiSicht.Kaskade",
                                     KiDialogTexte.SimKaskadeName, KiParameterTyp.Text,
                                     KiDialogTexte.SimKaskadeErl, leerErlaubt: true),
                    new KiDialogFeld("nicht_aufgenommen", "SimulationKiSicht.NichtAufgenommen",
                                     KiDialogTexte.SimOhnePlatzName, KiParameterTyp.Text,
                                     KiDialogTexte.SimOhnePlatzErl, leerErlaubt: true),

                    // ---- Die vier Laufparameter (lesbar und setzbar) -----------------
                    new KiDialogFeld("netzverluste", "SimulationKiSicht.Netzverluste",
                                     KiDialogTexte.SimNetzverlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimNetzverlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("bhkw_betriebsart", "SimulationKiSicht.BhkwBetriebsart",
                                     KiDialogTexte.SimBetriebsartName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SimBetriebsartErl),
                    new KiDialogFeld("bhkw_leistungsgrenze", "SimulationKiSicht.BhkwLeistungsgrenze",
                                     KiDialogTexte.SimLeistungsgrenzeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SimLeistungsgrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    // 16.09.2026 (Auftrag #299): Hier stand das Feld "wp_heizstab" -
                    // der PROJEKTweite Heizstabschalter. Er gehoert seither der
                    // WAERMEPUMPE (Tab_Energieanlagen.Heizstab je Anlage), und die
                    // Simulationsmaske fuehrt keinen projektweiten Wert mehr, den die KI
                    // setzen koennte. Aus fuenf Laufparametern sind damit VIER geworden.
                    new KiDialogFeld("kessel_bereitschaft", "SimulationKiSicht.KesselBereitschaft",
                                     KiDialogTexte.SimBereitschaftName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimBereitschaftErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A),

                    // ---- Der Kuehlschalter von Schritt ① (Welle #458) ---------------
                    //
                    // Die Projekteinstellung „Kuehlung rechnen" steht neben den
                    // Netzverlusten und schreibt wie sie SOFORT - ueber denselben
                    // Delegaten wie der Schalter (KuehlbetriebSchreiben).
                    new KiDialogFeld("kuehlbetrieb", "SimulationKiSicht.Kuehlbetrieb",
                                     KiDialogTexte.SimKuehlbetriebName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimKuehlbetriebErl),

                    // ---- Die Projektstufe der Anlagenkopplung (Konzept Anlagenkopplung 9.4) ----
                    //
                    // Neben dem Kuehlschalter und wie er SOFORT geschrieben - ueber denselben
                    // Delegaten wie die Wahl (AnlagenkopplungSchreiben). Wahlfeld mit den
                    // gebauten Stufen; eine nicht gebaute lehnt die Sicht benannt ab.
                    new KiDialogFeld("anlagenkopplung", "SimulationKiSicht.Anlagenkopplung",
                                     KiDialogTexte.SimAnlagenkopplungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SimAnlagenkopplungErl),

                    // ---- Die Werte JE ANLAGE von Schritt ① (Welle #458) -------------
                    //
                    // Quelle, konstante Quelltemperatur, WP-Prioritaet und Betriebsmodus
                    // setzt der Anwender ueber die Ueberlagerungen der Karte
                    // (Quellenwahl, WertAbfrage, BetriebsmodusDialog). Hier stehen sie
                    // als Felder der Ansicht - fuer die GEWAEHLTE Karte (quellanlage,
                    // eine Satzwahl) und mit denselben Schreibwegen und Vorpruefungen
                    // wie die Ueberlagerungen (SimulationKonfigSeite.Ki*Setzen). Die
                    // Ueberlagerung „Betriebsmodus" steht deshalb mit dem Grund
                    // FeldDesWirts auf der Ausnahmeliste und bekommt keine eigene Maske.
                    new KiDialogFeld("quellanlage", "SimulationKiSicht.Quellanlage",
                                     KiDialogTexte.SimAnlageName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SimAnlageErl, leerErlaubt: true, satzwahl: true),
                    new KiDialogFeld("waermequelle", "SimulationKiSicht.Waermequelle",
                                     KiDialogTexte.SimQuelleName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SimQuelleErl, leerErlaubt: true),
                    new KiDialogFeld("quelltemperatur_konstant",
                                     "SimulationKiSicht.QuelltemperaturKonstant",
                                     KiDialogTexte.SimQuelltempName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimQuelltempErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C),
                    new KiDialogFeld("wp_prioritaet", "SimulationKiSicht.WpPrioritaet",
                                     KiDialogTexte.SimPrioritaetName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SimPrioritaetErl),
                    new KiDialogFeld("wp_betriebsmodus", "SimulationKiSicht.WpBetriebsmodus",
                                     KiDialogTexte.SimBetriebsmodusName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SimBetriebsmodusErl, leerErlaubt: true),

                    // ---- Schritt ③ : die Kennzahlen des Laufs (nur lesend) ----------
                    new KiDialogFeld("waermebedarf", "SimulationKiSicht.WaermebedarfMwh",
                                     KiDialogTexte.SimWaermebedarfName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimWaermebedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("waermedeckung", "SimulationKiSicht.WaermedeckungProzent",
                                     KiDialogTexte.SimWaermedeckungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimWaermedeckungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("restwaerme", "SimulationKiSicht.RestwaermeMwh",
                                     KiDialogTexte.SimRestwaermeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimRestwaermeErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("strombedarf", "SimulationKiSicht.StrombedarfMwh",
                                     KiDialogTexte.SimStrombedarfName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimStrombedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("stromdeckung", "SimulationKiSicht.StromdeckungProzent",
                                     KiDialogTexte.SimStromdeckungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimStromdeckungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("reststrom", "SimulationKiSicht.ReststromMwh",
                                     KiDialogTexte.SimReststromName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimReststromErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("speicher_entladung", "SimulationKiSicht.SpeicherentladungMwh",
                                     KiDialogTexte.SimSpeicherEntladungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpeicherEntladungErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("speicher_soc_band", "SimulationKiSicht.SpeicherSocBand",
                                     KiDialogTexte.SimSpeicherSocName, KiParameterTyp.Text,
                                     KiDialogTexte.SimSpeicherSocErl, leerErlaubt: true),
                    new KiDialogFeld("laufhinweise", "SimulationKiSicht.Laufhinweise",
                                     KiDialogTexte.SimHinweiseName, KiParameterTyp.Text,
                                     KiDialogTexte.SimHinweiseErl, leerErlaubt: true),

                    // ---- Das Blatt „Ergebnis" von ③ (Welle KI-F6) -------------------
                    //
                    // Das EINZIGE echte Eingabefeld der neun Reiterblaetter. Alles
                    // andere darauf sind Schalter EINES BILDES - „sortiert", die
                    // Reihenhaken, die Streuwolken, die Nullzeilen, die Farbwahl -;
                    // sie schreiben nichts und leben in privaten Feldern, die der
                    // Reiter bei jedem Zeichenlauf neu aufbaut. Ein Setzer darauf
                    // schriebe in ein Feld, das der naechste Aufbau verwirft. Die zwei
                    // GANGLINIENreiter merken ihren Stand zwar ueber die Sitzung
                    // (Ganglinienregister), lesen ihn aber nur EINMAL beim Aufbau
                    // (_erstBelegt) - ein gesetzter Wert stuende im Gedaechtnis und
                    // nicht im offenen Bild.
                    new KiDialogFeld("autarkie_speicher", "SimulationKiSicht.AutarkieSpeicherKWh",
                                     KiDialogTexte.SimAutarkieName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimAutarkieErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),

                    // ---- Der Lesepunkt der Fusszeile von ① (Welle KI-F2) -----------
                    new KiDialogFeld("lesepunkt_davor", "SimulationKiSicht.LesepunktDavor",
                                     KiDialogTexte.SimLesepunktName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimLesepunktErl),

                    // ---- Der Reiter „Stromspeicher" von ③ (Welle KI-F2) ------------
                    //
                    // Einundzwanzig EINSTELLWERTE der aktiven Speichervariante. Sie
                    // stehen auf einem BLATT der Ansicht und nicht in einem Dialog -
                    // eine Maske ist, was offen ist, und offen ist die Simulation.
                    // Jedes Feld schreibt SOFORT, ueber denselben Weg wie das Feld auf
                    // dem Bildschirm (SpeicherfeldSchreiben je Feldschluessel).
                    new KiDialogFeld("speicher_soc_min", "SimulationKiSicht.SpeicherSocMin",
                                     KiDialogTexte.SimSpSocMinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpSocMinErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("speicher_soc_max", "SimulationKiSicht.SpeicherSocMax",
                                     KiDialogTexte.SimSpSocMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpSocMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("speicher_ladeleistung",
                                     "SimulationKiSicht.SpeicherLadeleistung",
                                     KiDialogTexte.SimSpLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("speicher_kapazitaet",
                                     "SimulationKiSicht.SpeicherKapazitaet",
                                     KiDialogTexte.SimSpKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),
                    new KiDialogFeld("speicher_ladeschwelle",
                                     "SimulationKiSicht.SpeicherLadeschwelle",
                                     KiDialogTexte.SimSpLadeschwelleName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpLadeschwelleErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("speicher_betriebsart",
                                     "SimulationKiSicht.SpeicherBetriebsart",
                                     KiDialogTexte.SimSpBetriebsartName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpBetriebsartErl, leerErlaubt: true),
                    new KiDialogFeld("speicher_berechnungsart",
                                     "SimulationKiSicht.SpeicherBerechnungsart",
                                     KiDialogTexte.SimSpBerechnungsartName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpBerechnungsartErl, leerErlaubt: true),
                    new KiDialogFeld("speicher_peakziel", "SimulationKiSicht.SpeicherPeakZiel",
                                     KiDialogTexte.SimSpPeakZielName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpPeakZielErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("speicher_peakziel_adaptiv",
                                     "SimulationKiSicht.SpeicherPeakZielAdaptiv",
                                     KiDialogTexte.SimSpPeakAdaptivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpPeakAdaptivErl),
                    new KiDialogFeld("speicher_kompatibilitaet",
                                     "SimulationKiSicht.SpeicherKompatibilitaet",
                                     KiDialogTexte.SimSpKompatibilitaetName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpKompatibilitaetErl),
                    new KiDialogFeld("speicher_laden_pv", "SimulationKiSicht.SpeicherLadenAusPv",
                                     KiDialogTexte.SimSpLadenPvName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpLadenPvErl),
                    new KiDialogFeld("speicher_laden_bhkw",
                                     "SimulationKiSicht.SpeicherLadenAusBhkw",
                                     KiDialogTexte.SimSpLadenBhkwName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpLadenBhkwErl),
                    new KiDialogFeld("speicher_netzentladung",
                                     "SimulationKiSicht.SpeicherNetzentladung",
                                     KiDialogTexte.SimSpNetzentladungName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpNetzentladungErl),

                    // Sichtbar, aber dauerhaft gesperrt (Ausbaustufe 11) - deshalb
                    // nurLesen: Der Assistent soll nicht anbieten, einen Schalter zu
                    // legen, den der Anwender nicht legen kann.
                    new KiDialogFeld("speicher_bhkw_stromgefuehrt",
                                     "SimulationKiSicht.SpeicherBhkwStromgefuehrt",
                                     KiDialogTexte.SimSpStromgefuehrtName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpStromgefuehrtErl, nurLesen: true),

                    new KiDialogFeld("speicher_kapitalzins",
                                     "SimulationKiSicht.SpeicherKapitalzins",
                                     KiDialogTexte.SimSpKapitalzinsName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpKapitalzinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("speicher_nutzungsdauer",
                                     "SimulationKiSicht.SpeicherNutzungsdauer",
                                     KiDialogTexte.SimSpNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpNutzungsdauerErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR),
                    new KiDialogFeld("speicher_leistungspreis",
                                     "SimulationKiSicht.SpeicherLeistungspreis",
                                     KiDialogTexte.SimSpLeistungspreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpLeistungspreisErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A),
                    new KiDialogFeld("speicher_netzladeaufschlag",
                                     "SimulationKiSicht.SpeicherNetzladeaufschlag",
                                     KiDialogTexte.SimSpNetzaufschlagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpNetzaufschlagErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("speicher_preisquelle",
                                     "SimulationKiSicht.SpeicherPreisquelle",
                                     KiDialogTexte.SimSpPreisquelleName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpPreisquelleErl, leerErlaubt: true),
                    new KiDialogFeld("speicher_preisreihe",
                                     "SimulationKiSicht.SpeicherPreisreihe",
                                     KiDialogTexte.SimSpPreisreiheName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpPreisreiheErl),
                    new KiDialogFeld("speicher_aufschlag",
                                     "SimulationKiSicht.SpeicherAufschlag",
                                     KiDialogTexte.SimSpAufschlagName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpAufschlagErl)
                });
        }

        // =====================================================================
        // Form_DBBHKW  ->  BhkwKatalogDialog   (Welle KI-F5)
        // =====================================================================

        /// <summary>
        /// Der BHKW-Katalogeditor — die dreizehn Felder, die
        /// <c>BhkwKatalogDialog</c> sichtbar traegt, als Eigenschaften von
        /// <c>EPOS.UI.Dialoge.Erzeuger.BhkwKatalogDaten</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Dieselbe Feldstrategie wie beim Heizkessel</b> — und derselbe Schnitt:
        /// Die Gruppen „Eingabedaten zur Berechnung der Kosten", „Emissionen nach
        /// BEHG-V" und „Emissionsfaktoren" sind am 15.09.2026 aus dem Editor gefallen
        /// („Der Dialog ueber Button Bearbeiten soll keine Kosten und Emissionen
        /// enthalten"). Die fuenf Kostenposten, Raumbedarf, Wartung, Nutzungsdauer und
        /// die fuenf Emissionsfaktoren bleiben im Feldsatz, weil die Huelle sie beim
        /// Speichern unveraendert zurueckschreibt — sie stehen aber NICHT in dieser
        /// Maske und gehoeren damit nicht in den Katalog (Fachkonzept 11.6: ein
        /// Katalogfeld, das in der offenen Maske niemand sieht, waere eine stille
        /// Setzung).
        /// </para>
        /// <para>
        /// <b>Der MODULNAME ist nur lesbar</b> — anders als beim Heizkessel.
        /// <c>BHKWStammCtrl.Update</c> filtert per <c>Bezeichner</c>; ein hier
        /// geaenderter Name traefe keinen Satz, und die Maske sperrt das Feld im Modus
        /// „Bearbeiten" deshalb selbst (Abweichung A-3 des Protokolls W6). Umbenannt
        /// wird ueber „Speichern unter", und dazu braucht es einen Namen, den der
        /// Assistent nicht beisteuert.
        /// </para>
        /// <para>
        /// <b>Der GESAMTWIRKUNGSGRAD ist eine Anzeige</b> (Schemaschritt 99,
        /// Anwenderentscheid 20.09.2026): Gepflegt werden die zwei Anteile, die Summe
        /// laeuft daneben mit. Wer ihn setzen koennte, schriebe die dritte von drei
        /// Zahlen, von denen zwei einander widersprechen.
        /// </para>
        /// </remarks>
        private static KiDialog Bhkw()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BHKW,
                anzeigename: KiDialogTexte.MaskeBhkwKatalog,
                felder: new[]
                {
                    new KiDialogFeld("name", "BhkwKatalogDaten.Bezeichner",
                                     KiDialogTexte.BhkkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.BhkkNameErl, nurLesen: true),
                    new KiDialogFeld("hersteller", "BhkwKatalogDaten.Firma",
                                     KiDialogTexte.BhkkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.BhkkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("motortyp", "BhkwKatalogDaten.Motortyp",
                                     KiDialogTexte.BhkkMotortypName, KiParameterTyp.Text,
                                     KiDialogTexte.BhkkMotortypErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "BhkwKatalogDaten.Beschreibung",
                                     KiDialogTexte.BhkkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.BhkkBeschreibungErl, leerErlaubt: true),

                    new KiDialogFeld("th_leistung", "BhkwKatalogDaten.Ptherm",
                                     KiDialogTexte.BhkkPthermName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkkPthermErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("el_leistung", "BhkwKatalogDaten.Pel",
                                     KiDialogTexte.BhkkPelName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkkPelErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_el", "BhkwKatalogDaten.WirkungsgradEl",
                                     KiDialogTexte.BhkkWgElName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkkWgElErl, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_th", "BhkwKatalogDaten.WirkungsgradTh",
                                     KiDialogTexte.BhkkWgThName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkkWgThErl, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_gesamt", "BhkwKatalogDaten.Wirkungsgrad",
                                     KiDialogTexte.BhkkWgGesamtName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkkWgGesamtErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("grenzleistung", "BhkwKatalogDaten.Grenzleistung",
                                     KiDialogTexte.BhkkGrenzleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkkGrenzleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "BhkwKatalogDaten.Brennstoff",
                                     KiDialogTexte.BhkkTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhkkTraegerErl, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "BhkwKatalogDaten.Vorlauf",
                                     KiDialogTexte.BhkkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.BhkkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "BhkwKatalogDaten.Ruecklauf",
                                     KiDialogTexte.BhkkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.BhkkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_SolarDB  ->  SolarkollektorKatalogDialog   (Welle KI-F5)
        // =====================================================================

        /// <summary>
        /// Der Solarkollektor-Katalogeditor — die dreizehn Felder von
        /// <c>EPOS.UI.Dialoge.Solarthermie.SolarkollektorKatalogDaten</c>, die
        /// <c>SolarkollektorKatalogDialog</c> zeigt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der KOLLEKTORNAME ist hier IMMER nur lesbar</b> — nicht nur im Modus
        /// „Bearbeiten": Die Maske setzt <c>NurLesen="true"</c> ohne Bedingung, weil der
        /// Name der Schluessel des Katalogsatzes ist. Ein neuer Name entsteht ueber
        /// „Speichern unter", das ihn abfragt.
        /// </para>
        /// <para>
        /// <b>Die INVESTITIONSKOSTEN stehen NICHT hier.</b>
        /// <c>SolarkollektorKatalogDaten.Kosten</c> fuehrt den Preis weiter — die Huelle
        /// liest ihn und schreibt ihn unveraendert zurueck, sonst nullte jedes
        /// „Ueberschreiben" die Spalte —, gezeigt wird er seit dem 15.09.2026 im
        /// Aufklapper „Alle Daten anzeigen" des Projektdialogs. Dorthin fuehrt kein Weg
        /// des Assistenten, und ein Katalogfeld ohne Feld auf der Maske waere die stille
        /// Setzung, die Fachkonzept 11.6 ausschliesst.
        /// </para>
        /// <para>
        /// <b>h0, k1, k2, Kdir und Kdiff sind FORMELZEICHEN</b> und stehen auf beiden
        /// Oberflaechen gleich — der Katalog schreibt keine zweite Schreibweise daneben.
        /// </para>
        /// </remarks>
        private static KiDialog Solarkollektor()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SOLARKOLLEKTOR,
                anzeigename: KiDialogTexte.MaskeSolarkollektorKatalog,
                felder: new[]
                {
                    new KiDialogFeld("name", "SolarkollektorKatalogDaten.Name",
                                     KiDialogTexte.SkkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.SkkNameErl, nurLesen: true),
                    new KiDialogFeld("hersteller", "SolarkollektorKatalogDaten.Firma",
                                     KiDialogTexte.SkkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.SkkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "SolarkollektorKatalogDaten.Beschreibung",
                                     KiDialogTexte.SkkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.SkkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("kollektortyp", "SolarkollektorKatalogDaten.Kollektortyp",
                                     KiDialogTexte.SkkTypName, KiParameterTyp.Text,
                                     KiDialogTexte.SkkTypErl, leerErlaubt: true),

                    new KiDialogFeld("modulflaeche", "SolarkollektorKatalogDaten.Modulflaeche",
                                     KiDialogTexte.SkkModulflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkModulflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("aperturflaeche", "SolarkollektorKatalogDaten.Aperturflaeche",
                                     KiDialogTexte.SkkAperturflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkAperturflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("h0", "SolarkollektorKatalogDaten.H0",
                                     KiDialogTexte.SkkH0Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkH0Erl),
                    new KiDialogFeld("k1", "SolarkollektorKatalogDaten.K1",
                                     KiDialogTexte.SkkK1Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkK1Erl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("k2", "SolarkollektorKatalogDaten.K2",
                                     KiDialogTexte.SkkK2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkK2Erl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K2),
                    new KiDialogFeld("kdir", "SolarkollektorKatalogDaten.Kdir",
                                     KiDialogTexte.SkkKdirName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkKdirErl),
                    new KiDialogFeld("kdiff", "SolarkollektorKatalogDaten.Kdiff",
                                     KiDialogTexte.SkkKdiffName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SkkKdiffErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD),
                    new KiDialogFeld("vorlauf", "SolarkollektorKatalogDaten.Vorlauf",
                                     KiDialogTexte.SkkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "SolarkollektorKatalogDaten.Ruecklauf",
                                     KiDialogTexte.SkkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_AdminPV, Form_AdminStromspeicher, Form_AdminWechselrichter
        //   ->  ModulKatalogDialog   (Welle KI-F5)
        // =====================================================================
        //
        // DREI MASKEN AUF EINER KOMPONENTE. Der ModulKatalogDialog ist Browser und
        // Editor in einem (Familie C der Vermessung): Liste links, Felder rechts,
        // "Speichern" schreibt unmittelbar in die Stammtabelle. Welche Felder rechts
        // stehen, sagt das ModulKatalogProfil - je Auspraegung ein anderer Satz aus
        // einer anderen Tabelle. Das sind drei Masken und nicht eine; dieselbe Lage
        // wie bei den drei Bedarfskatalogen der Welle KI-F3, die sich eine
        // Sichtklasse teilen.
        //
        // WAS DRAUSSEN BLEIBT - und warum:
        //
        //   * DIE KATALOGLISTE. Sie fuehrt nach dem CEC-Import 20 749 PV-Module,
        //     6 658 Stromspeicher und ueber zweitausend Wechselrichter. Eine
        //     Wahlliste dieser Groesse ist keine Auswahl mehr, sondern eine Menge
        //     von Verweisen (KI-D-Q5); gewaehlt wird ueber das gefilterte Raster,
        //     und das ist keine Klappliste, die sich setzen liesse.
        //   * DER FILTERSTAND der Liste (Spaltentrichter, Suchzeile). Er engt eine
        //     Anzeige ein und ist kein Einstellwert des Katalogsatzes.
        //   * DER AUFKLAPPER "Alle Parameter und ihre Verwendung". Reine Auskunft.

        /// <summary>
        /// Der PV-Modulkatalog — die fuenfzehn Felder, die
        /// <c>ModulKatalogDialog</c> in der Auspraegung <c>Photovoltaik</c> zeigt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der BEZEICHNER ist nur lesbar</b> — er ist der WHERE-Schluessel des
        /// UPDATE, und das Profil sperrt ihn deshalb selbst
        /// (<c>ModulKatalogFeld.Gesperrt</c>). Ein neuer Name entsteht ueber „Neu…",
        /// das ihn abfragt.
        /// </para>
        /// <para>
        /// <b>Die ZELLTECHNOLOGIE ist ein Wahlfeld</b> (KI-D-Q6). Ihre Eintraege
        /// stehen im Profil (<c>ModulKatalogProfil.Technologien</c>) und kommen zur
        /// Laufzeit ueber die Begleiteigenschaft <c>TechnologieWahl</c> der
        /// Sichtklasse herein; der Schluessel ist der Listenplatz, genau wie beim
        /// Auswahlfeld der Maske.
        /// </para>
        /// </remarks>
        private static KiDialog PvModulkatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PV_MODULKATALOG,
                anzeigename: KiDialogTexte.MaskePvModulkatalog,
                felder: new[]
                {
                    new KiDialogFeld("name", "ModulKatalogKiSicht.Bezeichner",
                                     KiDialogTexte.ModkPvBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.ModkPvBezeichnerErl, nurLesen: true),
                    new KiDialogFeld("hersteller", "ModulKatalogKiSicht.Firma",
                                     KiDialogTexte.ModkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.ModkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "ModulKatalogKiSicht.Beschreibung",
                                     KiDialogTexte.ModkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.ModkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("pmax", "ModulKatalogKiSicht.Leistung",
                                     KiDialogTexte.ModkPmaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkPmaxErl,
                                     einheit: KiDialogTexte.EINHEIT_W),
                    new KiDialogFeld("wirkungsgrad", "ModulKatalogKiSicht.Wirkungsgrad",
                                     KiDialogTexte.ModkWirkungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkWirkungsgradErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("u_mpp", "ModulKatalogKiSicht.UMpp",
                                     KiDialogTexte.ModkUMppName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkUMppErl,
                                     einheit: KiDialogTexte.EINHEIT_VOLT, leerErlaubt: true),
                    new KiDialogFeld("u_leerlauf", "ModulKatalogKiSicht.ULeerlauf",
                                     KiDialogTexte.ModkULeerlaufName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkULeerlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_VOLT, leerErlaubt: true),
                    new KiDialogFeld("i_mpp", "ModulKatalogKiSicht.IMpp",
                                     KiDialogTexte.ModkIMppName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkIMppErl,
                                     einheit: KiDialogTexte.EINHEIT_AMPERE, leerErlaubt: true),
                    new KiDialogFeld("i_kurzschluss", "ModulKatalogKiSicht.IKurzschluss",
                                     KiDialogTexte.ModkIKurzschlussName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkIKurzschlussErl,
                                     einheit: KiDialogTexte.EINHEIT_AMPERE, leerErlaubt: true),
                    new KiDialogFeld("gamma_pmp", "ModulKatalogKiSicht.GammaPmp",
                                     KiDialogTexte.ModkTempkoeffName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkTempkoeffErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT_K, leerErlaubt: true),
                    new KiDialogFeld("laenge", "ModulKatalogKiSicht.Laenge",
                                     KiDialogTexte.ModkLaengeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkLaengeErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("breite", "ModulKatalogKiSicht.Breite",
                                     KiDialogTexte.ModkBreiteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkBreiteErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("modulkosten", "ModulKatalogKiSicht.Modulkosten",
                                     KiDialogTexte.ModkPvKostenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkPvKostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("t_noct", "ModulKatalogKiSicht.TNoct",
                                     KiDialogTexte.ModkTNoctName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkTNoctErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("technologie", "ModulKatalogKiSicht.Technologie",
                                     KiDialogTexte.ModkTechnologieName, KiParameterTyp.Wahl,
                                     KiDialogTexte.ModkTechnologieErl, leerErlaubt: true)
                },
                knoepfe: Modulkatalogknoepfe());
        }

        /// <summary>
        /// Der Stromspeicher-Katalog — die vierzehn Felder der Auspraegung
        /// <c>Stromspeicher</c>, in zwei Gruppen: die Bestandsfelder und die
        /// Geraetetechnik nach Fachkonzept Stromspeicher 5.1.
        /// </summary>
        /// <remarks>
        /// <b>Die SPEICHERART ist ein Textfeld und kein Wahlfeld</b> — so steht sie
        /// im Profil (<c>BrowserFeldArt.Text</c> mit der Vorbelegung
        /// <c>DbWerte.SP_TYP_LITHIUM_IONEN</c>). Der Katalog schreibt keine
        /// Klappliste dorthin, wo die Maske ein freies Feld zeigt; sonst boete der
        /// Assistent eine Auswahl an, die es auf der Maske nicht gibt.
        /// </remarks>
        private static KiDialog Stromspeicherkatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STROMSPEICHER_KATALOG,
                anzeigename: KiDialogTexte.MaskeStromspeicherkatalog,
                felder: new[]
                {
                    new KiDialogFeld("name", "ModulKatalogKiSicht.Bezeichner",
                                     KiDialogTexte.ModkBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.ModkBezeichnerErl, nurLesen: true),
                    new KiDialogFeld("hersteller", "ModulKatalogKiSicht.Firma",
                                     KiDialogTexte.ModkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.ModkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("typ", "ModulKatalogKiSicht.Typ",
                                     KiDialogTexte.ModkTypName, KiParameterTyp.Text,
                                     KiDialogTexte.ModkTypErl),
                    new KiDialogFeld("energie", "ModulKatalogKiSicht.Energie",
                                     KiDialogTexte.ModkEnergieName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkEnergieErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),
                    new KiDialogFeld("leistung", "ModulKatalogKiSicht.Leistung",
                                     KiDialogTexte.ModkLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("ladezustand", "ModulKatalogKiSicht.Ladezustand",
                                     KiDialogTexte.ModkLadezustandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkLadezustandErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("degradation", "ModulKatalogKiSicht.Degradation",
                                     KiDialogTexte.ModkDegradationName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkDegradationErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("modulkosten", "ModulKatalogKiSicht.Modulkosten",
                                     KiDialogTexte.ModkKostenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkKostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KWH),

                    // ---- Geraetetechnik: leer heisst „nicht gepflegt" ---------------
                    new KiDialogFeld("wirkungsgrad_rt", "ModulKatalogKiSicht.WirkungsgradRt",
                                     KiDialogTexte.ModkWirkungsgradRtName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkWirkungsgradRtErl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("zyklen", "ModulKatalogKiSicht.Zyklen",
                                     KiDialogTexte.ModkZyklenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.ModkZyklenErl, leerErlaubt: true),
                    new KiDialogFeld("verschleisskosten", "ModulKatalogKiSicht.Verschleisskosten",
                                     KiDialogTexte.ModkVerschleissName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkVerschleissErl,
                                     einheit: KiDialogTexte.EinheitZykluskosten, leerErlaubt: true),
                    new KiDialogFeld("leistungskosten", "ModulKatalogKiSicht.Leistungskosten",
                                     KiDialogTexte.ModkLeistungskostenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkLeistungskostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW, leerErlaubt: true),
                    new KiDialogFeld("investition_fix", "ModulKatalogKiSicht.InvestitionFix",
                                     KiDialogTexte.ModkInvestFixName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkInvestFixErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("standby", "ModulKatalogKiSicht.Standby",
                                     KiDialogTexte.ModkStandbyName, KiParameterTyp.Zahl,
                                     KiDialogTexte.ModkStandbyErl,
                                     einheit: KiDialogTexte.EINHEIT_W, leerErlaubt: true)
                },
                knoepfe: Modulkatalogknoepfe());
        }

        /// <summary>
        /// Der Wechselrichter-Katalog — die sechsundzwanzig Felder der Auspraegung
        /// <c>Wechselrichter</c> in drei Gruppen: Geraet, Eingang und Wirkungsgrad.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die HERKUNFT ist Auskunft und keine Eingabe</b>: Der Geraeteimport setzt
        /// sie, die Handpflege bekommt „HAND". Das Profil sperrt sie, der Katalog
        /// deklariert sie deshalb nur lesend.
        /// </para>
        /// <para>
        /// <b>Die acht Wirkungsgrade sind FAKTOREN von 0 bis 1</b> und keine Prozente
        /// — so fuehrt sie <c>PvErweitertesModell.EtaWechselrichter</c>, und so stehen
        /// sie auf der Maske.
        /// </para>
        /// </remarks>
        private static KiDialog Wechselrichterkatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WECHSELRICHTER_KATALOG,
                anzeigename: KiDialogTexte.MaskeWechselrichterkatalog,
                felder: new[]
                {
                    // ---- Gruppe „Geraet" -------------------------------------------
                    new KiDialogFeld("name", "ModulKatalogKiSicht.Bezeichner",
                                     KiDialogTexte.WrkBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.WrkBezeichnerErl, nurLesen: true),
                    new KiDialogFeld("hersteller", "ModulKatalogKiSicht.Firma",
                                     KiDialogTexte.WrkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.WrkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "ModulKatalogKiSicht.Beschreibung",
                                     KiDialogTexte.WrkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.WrkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("p_ac_nenn", "ModulKatalogKiSicht.PAcNenn",
                                     KiDialogTexte.WrkPAcNennName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkPAcNennErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("s_ac_max", "ModulKatalogKiSicht.SAcMax",
                                     KiDialogTexte.WrkSAcMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkSAcMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_KVA, leerErlaubt: true),
                    new KiDialogFeld("p_dc_max", "ModulKatalogKiSicht.PDcMax",
                                     KiDialogTexte.WrkPDcMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkPDcMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kosten", "ModulKatalogKiSicht.Kosten",
                                     KiDialogTexte.WrkKostenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkKostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("herkunft", "ModulKatalogKiSicht.Herkunft",
                                     KiDialogTexte.WrkHerkunftName, KiParameterTyp.Text,
                                     KiDialogTexte.WrkHerkunftErl,
                                     leerErlaubt: true, nurLesen: true),

                    // ---- Gruppe „Eingang" ------------------------------------------
                    new KiDialogFeld("u_mpp_min", "ModulKatalogKiSicht.UMppMin",
                                     KiDialogTexte.WrkUMppMinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkUMppMinErl,
                                     einheit: KiDialogTexte.EINHEIT_VOLT, leerErlaubt: true),
                    new KiDialogFeld("u_mpp_max", "ModulKatalogKiSicht.UMppMax",
                                     KiDialogTexte.WrkUMppMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkUMppMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_VOLT, leerErlaubt: true),
                    new KiDialogFeld("u_dc_max", "ModulKatalogKiSicht.UDcMax",
                                     KiDialogTexte.WrkUDcMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkUDcMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_VOLT, leerErlaubt: true),
                    new KiDialogFeld("u_start", "ModulKatalogKiSicht.UStart",
                                     KiDialogTexte.WrkUStartName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkUStartErl,
                                     einheit: KiDialogTexte.EINHEIT_VOLT, leerErlaubt: true),
                    new KiDialogFeld("i_dc_max", "ModulKatalogKiSicht.IDcMax",
                                     KiDialogTexte.WrkIDcMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkIDcMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_AMPERE, leerErlaubt: true),
                    new KiDialogFeld("i_sc_max", "ModulKatalogKiSicht.IScMax",
                                     KiDialogTexte.WrkIScMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkIScMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_AMPERE, leerErlaubt: true),
                    new KiDialogFeld("anzahl_mppt", "ModulKatalogKiSicht.AnzahlMppt",
                                     KiDialogTexte.WrkAnzahlMpptName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WrkAnzahlMpptErl, leerErlaubt: true),
                    new KiDialogFeld("straenge_je_mppt", "ModulKatalogKiSicht.StraengeJeMppt",
                                     KiDialogTexte.WrkStraengeJeMpptName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WrkStraengeJeMpptErl, leerErlaubt: true),

                    // ---- Gruppe „Wirkungsgrad" -------------------------------------
                    new KiDialogFeld("eta05", "ModulKatalogKiSicht.Eta05",
                                     KiDialogTexte.WrkEta05Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEta05Erl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta10", "ModulKatalogKiSicht.Eta10",
                                     KiDialogTexte.WrkEta10Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEta10Erl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta20", "ModulKatalogKiSicht.Eta20",
                                     KiDialogTexte.WrkEta20Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEta20Erl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta30", "ModulKatalogKiSicht.Eta30",
                                     KiDialogTexte.WrkEta30Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEta30Erl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta50", "ModulKatalogKiSicht.Eta50",
                                     KiDialogTexte.WrkEta50Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEta50Erl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta100", "ModulKatalogKiSicht.Eta100",
                                     KiDialogTexte.WrkEta100Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEta100Erl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta_euro", "ModulKatalogKiSicht.EtaEuro",
                                     KiDialogTexte.WrkEtaEuroName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEtaEuroErl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("eta_max", "ModulKatalogKiSicht.EtaMax",
                                     KiDialogTexte.WrkEtaMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkEtaMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_FAKTOR, leerErlaubt: true),
                    new KiDialogFeld("p_standby", "ModulKatalogKiSicht.PStandby",
                                     KiDialogTexte.WrkPStandbyName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkPStandbyErl,
                                     einheit: KiDialogTexte.EINHEIT_W, leerErlaubt: true),
                    new KiDialogFeld("p_nacht", "ModulKatalogKiSicht.PNacht",
                                     KiDialogTexte.WrkPNachtName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WrkPNachtErl,
                                     einheit: KiDialogTexte.EINHEIT_W, leerErlaubt: true)
                },
                knoepfe: Modulkatalogknoepfe());
        }

        /// <summary>
        /// Die Knoepfe, die alle drei Auspraegungen des Modulkatalogs tragen.
        /// </summary>
        /// <remarks>
        /// <para><b>„Löschen" fehlt, und zwar nicht aus Versehen:</b> Ein Loeschknopf
        /// ist nicht deklarierbar (Fachkonzept 1.2/11.7), und <c>KiDialogKnopf</c>
        /// weist ihn schon im Konstruktor ab. Der Katalogsatz verschwindet damit nicht
        /// auf Zuruf des Modells.</para>
        /// <para><b>„Neu…" fragt einen NAMEN ab</b> und steht deshalb — wie „Speichern
        /// unter" bei den Katalogeditoren — nur in der Liste, nicht im Speicherweg des
        /// Assistenten: Den Namen kann er nicht beisteuern.</para>
        /// </remarks>
        private static KiDialogKnopf[] Modulkatalogknoepfe()
        {
            return new[]
            {
                new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                new KiDialogKnopf("neu", "btn_Neu", KiDialogTexte.KnopfNeu),
                new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfOk)
            };
        }

        // =====================================================================
        // Kenndaten  ->  Dialoge.Waermepumpe.KennlinienEditorDialog   (Welle #458, Stufe 2)
        // =====================================================================

        /// <summary>
        /// Der Kennlinieneditor der Waermepumpe — acht Felder aus
        /// <c>EPOS.UI.Dialoge.Waermepumpe.KennlinienKiSicht</c>, drei davon SPALTEN.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Ueberlagerung mit eigenem Arbeitsstand</b> — dieselbe Bauart wie
        /// <see cref="KiMaskennamen.PV_ANLAGENWERTE"/>: Der Editor bearbeitet eine KOPIE
        /// der Stuetzstellen, „OK" gibt sie an den Wirt zurueck (Waermepumpen-Verwaltung
        /// oder -Anlage), und der gleicht sie im Kern ab. Angemeldet sind Auffrischen und
        /// Schreibschutz, KEIN Speicherweg — <c>dialog_speichern</c> lehnt benannt ab.
        /// </para>
        /// <para>
        /// <b>Die Stuetzstellen sind SPALTEN der gewaehlten Vorlaufstufe</b>; nur deren
        /// Zeilen stehen im Raster. Die Stufe selbst ist ein Wahlfeld und SATZWAHL: Sie
        /// wechselt, was angezeigt wird, und bleibt auch am Auslieferungssatz frei.
        /// </para>
        /// <para>
        /// <b>Anlegen und Entfernen bleiben beim Anwender</b> (KI-D-Q11): „Neue
        /// Vorlauftemperatur", „Daten uebernehmen" und der Papierkorb einer Zeile. Der
        /// Assistent fuellt die Felder davor.
        /// </para>
        /// </remarks>
        private static KiDialog Kennlinien()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KENNLINIEN,
                anzeigename: KiDialogTexte.MaskeKennlinien,
                felder: new[]
                {
                    new KiDialogFeld("vorlauf", "KennlinienKiSicht.Vorlauf",
                                     KiDialogTexte.WpklVorlaufName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpklVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true,
                                     satzwahl: true),
                    new KiDialogFeld("neuer_vorlauf", "KennlinienKiSicht.NeuerVorlauf",
                                     KiDialogTexte.WpklNeuerVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpklNeuerVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Die Stuetzstellen der gewaehlten Stufe: SPALTEN --------------
                    new KiDialogFeld("temperatur", "KennlinienKiSicht.Zeilen[].Temperatur",
                                     KiDialogTexte.WpklTemperaturName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpklTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("cop", "KennlinienKiSicht.Zeilen[].Cop",
                                     KiDialogTexte.WpklCopName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpklCopErl, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen"),
                    new KiDialogFeld("ptherm", "KennlinienKiSicht.Zeilen[].Ptherm",
                                     KiDialogTexte.WpklPthermName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpklPthermErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true,
                                     zeilenkennzeichen: "Kennzeichen"),

                    // ---- Die Gruppe „Neue Stuetzstelle" ---------------------------------
                    new KiDialogFeld("neu_temperatur", "KennlinienKiSicht.NeuTemperatur",
                                     KiDialogTexte.WpklNeuTemperaturName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpklNeuTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("neu_cop", "KennlinienKiSicht.NeuCop",
                                     KiDialogTexte.WpklNeuCopName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpklNeuCopErl, leerErlaubt: true),
                    new KiDialogFeld("neu_ptherm", "KennlinienKiSicht.NeuPtherm",
                                     KiDialogTexte.WpklNeuPthermName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpklNeuPthermErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Wizard_Projekt  ->  Seiten.Assistent.ProjektKopfSeite   (Welle #458, Stufe 2)
        // =====================================================================

        /// <summary>
        /// Der Projektkopf des Assistenten „Neues Projekt" — fuenf Felder aus
        /// <c>EPOS.UI.Seiten.Assistent.ProjektKopfKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Sichtklasse, obwohl die Seite in ein Daten-Objekt schreibt</b>
        /// (<c>ProjektKopfDaten</c>): Die Klimaregion steht darin als Id UND Name, und
        /// die Seite setzt beide zugleich; der Name steht im Bearbeiten-Modus fest. Beides
        /// kennt nur der Weg der Seite, nicht die nackte Eigenschaft.
        /// </para>
        /// <para>
        /// <b>Die Pruefung ist die der Seite</b> (<c>ProjektKopfRegeln</c>): Name leer,
        /// Name vergeben, Klimaregion fehlt — derselbe Hinweis unter den Feldern und
        /// dasselbe Veto beim Verlassen der Seite. <b>Kein Speicherweg:</b> Angelegt wird
        /// das Projekt mit „Fertig"; das bleibt der Klick des Anwenders.
        /// </para>
        /// <para>
        /// <b>Die zwei Datumsfelder bleiben draussen</b> — sie sind gesperrt und zeigen,
        /// was das Programm gespeichert hat.
        /// </para>
        /// </remarks>
        private static KiDialog Projektkopf()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PROJEKTKOPF,
                anzeigename: KiDialogTexte.MaskeProjektkopf,
                felder: new[]
                {
                    new KiDialogFeld("name", "ProjektKopfKiSicht.Name",
                                     KiDialogTexte.PkopfNameName, KiParameterTyp.Text,
                                     KiDialogTexte.PkopfNameErl),
                    new KiDialogFeld("klimaregion", "ProjektKopfKiSicht.Klimaregion",
                                     KiDialogTexte.PkopfKlimaName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PkopfKlimaErl),
                    new KiDialogFeld("kunde", "ProjektKopfKiSicht.Kunde",
                                     KiDialogTexte.PkopfKundeName, KiParameterTyp.Text,
                                     KiDialogTexte.PkopfKundeErl, leerErlaubt: true),
                    new KiDialogFeld("bearbeiter", "ProjektKopfKiSicht.Bearbeiter",
                                     KiDialogTexte.PkopfBearbeiterName, KiParameterTyp.Text,
                                     KiDialogTexte.PkopfBearbeiterErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "ProjektKopfKiSicht.Beschreibung",
                                     KiDialogTexte.PkopfBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.PkopfBeschreibungErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("weiter", "btn_Weiter", KiDialogTexte.KnopfWeiter)
                });
        }

        // =====================================================================
        // Form_Start  ->  Seiten.Start.Startseite   (Welle #458, Stufe 2)
        // =====================================================================

        /// <summary>
        /// Die Startseite — zwei Einstellwerte des offenen Projekts aus
        /// <c>EPOS.UI.Seiten.Start.StartseiteKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Klimaregion</b> des Kopfbandes ist ein Wahlfeld ueber die Stammregionen;
        /// geschrieben wird sie mit dem Knopf „Speichern" daneben, und genau der ist der
        /// Speicherweg der Maske. <b>Die Solarart</b> ist die Weiche der
        /// Solarthermiekachel (Profil oder Ganglinie); sie wirkt wie der Klick sofort.
        /// </para>
        /// <para>
        /// <b>Die Projekt- und Variantenwahl bleibt draussen:</b> Sie oeffnet ein anderes
        /// Projekt — eine Navigation, kein Einstellwert. Ohne offenes Projekt ist die
        /// Maske schreibgeschuetzt.
        /// </para>
        /// </remarks>
        private static KiDialog Startseite()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STARTSEITE,
                anzeigename: KiDialogTexte.MaskeStartseite,
                felder: new[]
                {
                    new KiDialogFeld("klimaregion", "StartseiteKiSicht.Klimaregion",
                                     KiDialogTexte.StartKlimaName, KiParameterTyp.Wahl,
                                     KiDialogTexte.StartKlimaErl, leerErlaubt: true),
                    new KiDialogFeld("solarart", "StartseiteKiSicht.Solarart",
                                     KiDialogTexte.StartSolarartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.StartSolarartErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern)
                });
        }

        // =====================================================================
        // Form_AdminSettings  ->  Dialoge.Admin.EinstellungenDialog   (Welle #458, Stufe 2)
        // =====================================================================

        /// <summary>
        /// Der Typname der Sichtklasse der Programmeinstellungen
        /// (<c>EPOS.UI.Dialoge.Admin.EinstellungenKiSicht</c>).
        /// </summary>
        public const string EINSTELLUNGEN_SICHT = "EinstellungenKiSicht";

        /// <summary>Die Vorsilbe der Farbfelder: <c>farbe_waerme_wp</c>.</summary>
        public const string FARBFELD_VORSILBE = "farbe_";

        /// <summary>
        /// Die Programmeinstellungen — neun benannte Werte und je Farbrolle der
        /// Diagramme ein Feld der FELDTAFEL.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Setzbar sind die Adressen</b> (Wiki, Geokodierung, PVGIS, DWD-Portal,
        /// TRY-Regionaldaten), <b>die Kuehlungsvorgabe neuer Projekte</b>, <b>die Rubrik
        /// „Bericht"</b> (Firma und Vorlagenordner der Berichtsvorlagen, BV-E1 — der
        /// Ordner wird erst im OK-Weg geprueft und nur bestehend uebernommen —, dazu das
        /// Firmenlogo der Kopfzeile, BV-E2) und <b>die
        /// Diagrammfarben</b>. Die Farbfelder ENTSTEHEN aus der Rollenliste
        /// (<see cref="Zeichnung.Diagrammfarben.Gruppen"/>) — derselben, aus der die Huelle
        /// die Rubrik „Diagramme" fuellt; eine zweite Liste gibt es nicht. Feldname =
        /// Vorsilbe + Rollenname klein, Anzeigename = der Name der Rolle, der Wert ein
        /// Farbton <c>#RRGGBB</c>.
        /// </para>
        /// <para>
        /// <b>Nicht ueber den Assistenten</b> gehen die fuenf Ordner (Dateiwahlen), der
        /// Name der Datenbank (ein Datenbankwechsel beim naechsten Start) und der
        /// Abschalter des Assistenten selbst. „Standardwerte" und „Hausfarben" setzen
        /// zurueck und bleiben Knoepfe des Anwenders.
        /// </para>
        /// <para>
        /// <b>Speichern ist der Weg von „OK"</b>, nur ohne zu schliessen.
        /// </para>
        /// </remarks>
        private static KiDialog Einstellungen()
        {
            var felder = new List<KiDialogFeld>
            {
                new KiDialogFeld("wiki_url", EINSTELLUNGEN_SICHT + ".WikiUrl",
                                 KiDialogTexte.AdmsetWikiName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetWikiErl, leerErlaubt: true),
                new KiDialogFeld("geokodierung_url", EINSTELLUNGEN_SICHT + ".GeokodierungUrl",
                                 KiDialogTexte.AdmsetGeokodierungName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetGeokodierungErl, leerErlaubt: true),
                new KiDialogFeld("pvgis_url", EINSTELLUNGEN_SICHT + ".PvgisUrl",
                                 KiDialogTexte.AdmsetPvgisName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetPvgisErl, leerErlaubt: true),
                new KiDialogFeld("try_portal_url", EINSTELLUNGEN_SICHT + ".TryPortalUrl",
                                 KiDialogTexte.AdmsetTryPortalName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetTryPortalErl, leerErlaubt: true),
                new KiDialogFeld("try_regional_url", EINSTELLUNGEN_SICHT + ".TryRegionalUrl",
                                 KiDialogTexte.AdmsetTryRegionalName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetTryRegionalErl, leerErlaubt: true),
                new KiDialogFeld("neue_projekte_kuehlung", EINSTELLUNGEN_SICHT + ".NeueProjekteMitKuehlung",
                                 KiDialogTexte.AdmsetKuehlungName, KiParameterTyp.Wahrheitswert,
                                 KiDialogTexte.AdmsetKuehlungErl),

                // BV-E1 (Konzept Berichtsvorlagen 10.3): die Rubrik „Bericht" - an den Arbeitsstand
                // des Dialogs; ohne Rubrik oder bei gesperrter Ordnerwahl lehnt die Sicht benannt ab.
                new KiDialogFeld("bericht_firma", EINSTELLUNGEN_SICHT + ".BerichtFirma",
                                 KiDialogTexte.AdmsetBerichtFirmaName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetBerichtFirmaErl, leerErlaubt: true),
                new KiDialogFeld("bericht_vorlagenordner", EINSTELLUNGEN_SICHT + ".BerichtVorlagenordner",
                                 KiDialogTexte.AdmsetBerichtOrdnerName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetBerichtOrdnerErl, leerErlaubt: true),

                // BV-E2 (Entscheid BV-E2-1): das Firmenlogo der Kopfzeile - ein Dateipfad, leer = ohne Logo.
                new KiDialogFeld("bericht_logo", EINSTELLUNGEN_SICHT + ".BerichtLogo",
                                 KiDialogTexte.AdmsetBerichtLogoName, KiParameterTyp.Text,
                                 KiDialogTexte.AdmsetBerichtLogoErl, leerErlaubt: true)
            };

            foreach (Zeichnung.Rollengruppe gruppe in Zeichnung.Diagrammfarben.Gruppen)
                foreach (Zeichnung.Farbrolle rolle in gruppe.Rollen)
                {
                    string name = Zeichnung.Diagrammfarben.Anzeigename(rolle);
                    felder.Add(new KiDialogFeld(
                        FARBFELD_VORSILBE + rolle.Name.ToLowerInvariant(),
                        EINSTELLUNGEN_SICHT + "." + rolle.Name,
                        name, KiParameterTyp.Text,
                        KiDialogTexte.AdmsetFarbeErl(name, gruppe.Titel)));
                }

            return new KiDialog(
                maskenname: KiMaskennamen.EINSTELLUNGEN,
                anzeigename: KiDialogTexte.MaskeEinstellungen,
                felder: felder,
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }
    }
}
