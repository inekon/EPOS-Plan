namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Eine Umrechnungsregel des Regelblocks (iU9-W4.4, Etappe K3, Konzept
/// Kosten/Energieträger § 4.3) — die Anzeigefassung von
/// <c>UmrechnungsRegel</c>.
///
/// <para><b>Der Block arbeitet auf einer Speicherkopie</b>, damit der Prüfer
/// die Frage „was wäre, wenn ich diese Regel abschalte?" beantworten kann, ohne
/// dass dafür etwas geschrieben oder erneut gelesen werden müsste. Geschrieben
/// wird ausschließlich, was der Anwender angefasst hat.</para>
/// </summary>
public sealed class UmrechnungsregelZeile
{
    /// <summary>Laufende Nummer in der Liste — der Schlüssel der Zeile.</summary>
    public int Nummer { get; set; }

    public string Name { get; set; } = "";
    public string Von { get; set; } = "";
    public string Nach { get; set; } = "";
    public double Faktor { get; set; } = 1;
    public bool Aktiv { get; set; } = true;
}

/// <summary>Eine Zeile der Preishistorie (<c>energy_price</c>), fertig formatiert.</summary>
/// <param name="GueltigAb">Datum, ab dem der Stand gilt.</param>
/// <param name="Heizwert">Heizwert je Abrechnungseinheit.</param>
/// <param name="Basiseinheit">Die Einheit, in der der Arbeitspreis steht.</param>
/// <param name="Arbeitspreis">Arbeitspreis je Abrechnungseinheit.</param>
/// <param name="Grundpreis">Grundpreis [€/a].</param>
/// <param name="Leistungspreis">Leistungspreis.</param>
/// <param name="Id">Der Schlüssel der Zeile (<c>energy_price.id</c>); 0 = unbekannt.
/// Er steht am Ende, damit er beim Bauen weggelassen werden darf — gebraucht wird
/// er allein zum Löschen GENAU DIESER Zeile.</param>
public sealed record PreishistorieZeile(string GueltigAb, string Heizwert, string Basiseinheit,
                                        string Arbeitspreis, string Grundpreis,
                                        string Leistungspreis, int Id = 0);

/// <summary>
/// Eine Zeile des Emissions-Abschnitts (Etappe E3, Konzept
/// Emissionsarten § 4.1): Art · Wert · Einheit · Herkunft · Katalog.
/// </summary>
public sealed class EmissionsFeldZeile
{
    /// <summary>Kürzel der Art — der Schlüssel (CO2, SO2, NOX, CH4 …).</summary>
    public string Kuerzel { get; set; } = "";

    /// <summary>Angezeigter Name der Art.</summary>
    public string Name { get; set; } = "";

    /// <summary>Einheit der Art (g/kWh, mg/kWh).</summary>
    public string Einheit { get; set; } = "";

    /// <summary>Der Wert; <c>null</c> = nicht gepflegt.</summary>
    public double? Wert { get; set; }

    /// <summary>Herkunft im Klartext — im Bestand die gekürzte Spalte mit Tooltip.</summary>
    public string Herkunft { get; set; } = "";

    /// <summary>
    /// Im Projektkontext sind nur die drei Kernarten pflegbar; die übrigen
    /// stehen mit ihrem KATALOGWERT da — lesbar, aber nicht hier änderbar.
    /// </summary>
    public bool NurLesend { get; set; }
}

/// <summary>
/// Was die Trägerkarte zu EINER Größe (CO₂-Wert, Arbeits-, Leistungspreis) zu sagen
/// hat: entweder sie fehlt — dann steht hier der Hinweis und die Beschriftung des
/// Übernahmewegs —, oder sie ist aus der Kategorie GELIEHEN — dann steht hier die
/// Herleitungszeile.
///
/// <para><b>Warum eine Zeile für beides.</b> Ein Wert ist entweder eine Lücke oder
/// ein geliehener Wert; nie beides und nie gleichzeitig eine dritte Sache. Zwei
/// Listen nebeneinander wären zwei Wahrheiten über dieselbe Größe.</para>
///
/// <para><b>Der Knopf verspricht nichts.</b> Er öffnet die Auswahl; geschrieben wird
/// erst, was der Anwender darin bestätigt.</para>
/// </summary>
public sealed class Wertluecke
{
    /// <summary>Schlüssel der Größe (<c>CO2</c>, <c>ARBEITSPREIS</c>, <c>LEISTUNGSPREIS</c>).</summary>
    public string Groesse { get; set; } = "";

    /// <summary>„Für diesen Energieträger ist kein … gepflegt."; leer = keine Lücke.</summary>
    public string Hinweis { get; set; } = "";

    /// <summary>Beschriftung des Übernahmewegs; leer = kein Weg (Katalogkontext).</summary>
    public string KnopfText { get; set; } = "";

    /// <summary>Die Herleitung eines geliehenen Wertes; leer = nichts geliehen.</summary>
    public string Leihzeile { get; set; } = "";
}

/// <summary>
/// Die Rückfrage des Übernahmewegs: welcher Träger derselben Kategorie soll den Wert
/// stellen? Auch bei GENAU EINEM Kandidaten wird gefragt — der Anwenderentscheid
/// verlangt ausdrücklich die Bestätigung, nicht die Bequemlichkeit.
/// </summary>
public sealed class Uebernahmewahl
{
    /// <summary>Schlüssel der Größe, um die es geht.</summary>
    public string Groesse { get; set; } = "";

    /// <summary>Titel der Überlagerung.</summary>
    public string Titel { get; set; } = "";

    /// <summary>Die Frage über der Auswahl.</summary>
    public string Frage { get; set; } = "";

    /// <summary>Was der Weg sagt, wenn die Kategorie nichts hergibt.</summary>
    public string LeerText { get; set; } = "";

    /// <summary>Die Kandidaten mit Name, Wert und Einheit — Id = <c>energy_carrier.id</c>.</summary>
    public IReadOnlyList<(int Id, string Text)> Kandidaten { get; set; }
        = Array.Empty<(int, string)>();
}

/// <summary>
/// Der Bearbeitungsstand einer Trägerkarte (iU9-W4.4) — was
/// <c>EnergietraegerEinstellungen</c> zeigt und ändert.
///
/// <para>Der WinForms-Vorläufer <c>ucFuelSettings</c> (2 103 Z.) hielt all das
/// in Steuerelementen und rechnete nebenher; hier ist es ein Wert. Die
/// Datenseite — <c>energy_carrier</c>, <c>energy_price</c>,
/// <c>energy_project_settings</c>, <c>energy_conversion</c> — liegt seit dieser
/// Welle im Kern-Controller <c>EnergietraegerPreisCtrl</c>.</para>
/// </summary>
public sealed class EnergietraegerStand
{
    // ---- Kopf ---------------------------------------------------------

    /// <summary>„‹Name›  (VDI 3805 ‹Code›)" — die erste Kopfzeile.</summary>
    public string TraegerZeile { get; set; } = "";

    /// <summary>„Gruppe: ‹Gruppe›" — die zweite Kopfzeile.</summary>
    public string GruppeZeile { get; set; } = "";

    // ---- Preise -------------------------------------------------------

    public double Arbeitspreis { get; set; }
    public double Leistungspreis { get; set; }
    public double Grundpreis { get; set; }
    public double Heizwert { get; set; }
    public double Brennwert { get; set; }

    /// <summary>Führt der Träger einen Heizwert? (<c>pricing_model.has_hi</c>)</summary>
    public bool MitHeizwert { get; set; }

    /// <summary>Führt er einen Brennwert? (<c>has_hs</c>)</summary>
    public bool MitBrennwert { get; set; }

    /// <summary>Führt er einen Leistungspreis? (<c>has_powerprice</c>)</summary>
    public bool MitLeistungspreis { get; set; }

    public string EinheitArbeitspreis { get; set; } = "";
    public string EinheitLeistungspreis { get; set; } = "";
    public string EinheitHeizwert { get; set; } = "";
    public string EinheitBrennwert { get; set; } = "";
    public string EinheitGrundpreis { get; set; } = "€/a";

    /// <summary>Die Abrechnungseinheit des Trägers (Anzeige neben „Basiseinheit:").</summary>
    public string Basiseinheit { get; set; } = "";

    /// <summary>
    /// Die wählbaren Preisbasen — Id = Index, Text die Einheit des Arbeitspreises
    /// („€/Nm³", „€/kWh"). Die Karte zeigt die Klappliste direkt unter dem
    /// Arbeitspreis, sobald es mehr als einen Eintrag gibt (ET-D-4).
    /// </summary>
    public IReadOnlyList<(int Id, string Text)> Preisbasen { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Die gewählte Preisbasis.</summary>
    public int? PreisbasisId { get; set; }

    /// <summary>
    /// Die leise Zeile unter dem Arbeitspreis, solange „€/kWh" fehlt, weil der
    /// Träger einen Heizwert führt, aber keinen gepflegt hat; leer = keine Zeile.
    /// </summary>
    public string PreisbasisHinweis { get; set; } = "";

    /// <summary>
    /// ETAPPE E7c (Schritt F, Mockup U32): die Herleitungszeile unter der Preisbasis —
    /// gesetzt, wenn die Datenbank den Kartenzustand noch nicht speichern kann
    /// (Schemastand vor 112); leer = keine Zeile.
    /// </summary>
    public string PreisbasisHerleitung { get; set; } = "";

    /// <summary>Der Leistungspreis-Modus: <c>true</c> = Monat, <c>false</c> = Jahr (FK6).</summary>
    public bool LeistungsModusMonat { get; set; }

    /// <summary>Statuszeile der Saisonreihe (FK6a); leer = keine gepflegt.</summary>
    public string ReihenStatus { get; set; } = "";

    // ---- Die Leistungspreis-Staffel des Stromträgers (Q11) ---------------

    /// <summary>
    /// Zeigt die Karte die zweistufige Leistungspreis-Staffel? Nur beim
    /// STROMträger im PROJEKTkontext — sie steht an der Projektübersteuerung
    /// (<c>energy_project_settings</c>, Schemaschritt 104), der Katalog führt keine.
    /// </summary>
    public bool MitStaffel { get; set; }

    /// <summary>Staffelgrenze [kW]; leer = nicht gepflegt.</summary>
    public double? StaffelGrenze { get; set; }

    /// <summary>Preis bis zur Grenze [€/(kW·a)]; leer = nicht gepflegt.</summary>
    public double? StaffelPreis1 { get; set; }

    /// <summary>Preis über der Grenze [€/(kW·a)]; leer = nicht gepflegt.</summary>
    public double? StaffelPreis2 { get; set; }

    // ---- Formel (nur mit Heizwert) -------------------------------------

    /// <summary>Zeigt die Formelgruppe? Ohne Heizwert gibt es keine Formel.</summary>
    public bool MitFormel { get; set; }

    /// <summary>„0,0812 €" — der Preis je Kilowattstunde.</summary>
    public string PreisJeKwh { get; set; } = "";

    /// <summary>
    /// „0,6500 €/kg ÷ 8,00 kWh/kg = 0,0812 €/kWh", bei der Preisbasis kWh
    /// „0,0812 €/kWh × 8,00 kWh/kg = 0,6500 €/kg (gespeichert je kg)", ohne
    /// Heizwert „Direktabrechnung nach kWh".
    /// </summary>
    public string FormelText { get; set; } = "";

    /// <summary>
    /// Die HERLEITUNGSZEILE des Blocks „Preis und Heizwert" (ET-D):
    /// „→ 0,0720 €/kWh · Umrechnungsfaktor Hs/Hi = 1,1048". Der Faktor steht nur
    /// dabei, wenn Hi und Hs beide über null liegen. Leer = keine Zeile.
    /// </summary>
    public string HerleitungPreis { get; set; } = "";

    // ---- Umrechnungsblock (Etappe K3) ----------------------------------

    /// <summary>Die Regeln des Brennstoffs — Speicherkopie, siehe Zeilenklasse.</summary>
    public IReadOnlyList<UmrechnungsregelZeile> Regeln { get; set; }
        = Array.Empty<UmrechnungsregelZeile>();

    /// <summary>„effektiv: 1 ‹Einheit› = X kWh (Hi) / Y kWh (Hs)".</summary>
    public string EffektivText { get; set; } = "";

    /// <summary>Der rote Verstoßhinweis (L2); leer = alles in Ordnung.</summary>
    public string VerstossText { get; set; } = "";

    // ---- Preisblöcke ----------------------------------------------------

    /// <summary>„Strompreis Details" — nur beim Stromträger belegt (SP-E-2).</summary>
    public StrompreisDetailsStand? Zerlegung { get; set; }

    /// <summary>Preiszerlegung — nur bei der Brennstoff-Familie belegt (B2).</summary>
    public BrennstoffBestandteileStand? Bestandteile { get; set; }


    // ---- Historie -------------------------------------------------------

    /// <summary>Gültig-ab-Datum des zu schreibenden Standes.</summary>
    public DateOnly? GueltigAb { get; set; }

    /// <summary>Die Preishistorie, jüngste zuerst.</summary>
    public IReadOnlyList<PreishistorieZeile> Historie { get; set; }
        = Array.Empty<PreishistorieZeile>();

    /// <summary>
    /// Leise Zeile unter der Tabelle — im Katalogkontext der Grund, warum dort
    /// keine Zeile entsteht. Leer = kein Hinweis.
    /// </summary>
    public string HistorieHinweis { get; set; } = "";

    // ---- Katalogübernahme ------------------------------------------------

    /// <summary>
    /// Zeigt den Knopf „Katalogwerte übernehmen"? Nur im Projektkontext — im
    /// Katalog SIND die Felder die Katalogwerte.
    /// </summary>
    public bool MitKatalogUebernahme { get; set; }

    /// <summary>
    /// „Katalogwerte übernommen — noch nicht gespeichert."; leer = kein Hinweis.
    /// Die Übernahme ist eine einmalige Kopie: Geschrieben wird erst mit
    /// „Speichern" bzw. „OK".
    /// </summary>
    public string UebernahmeHinweis { get; set; } = "";

    // ---- Fehlende und geliehene Werte -----------------------------------

    /// <summary>
    /// Was zu CO₂-Wert, Arbeits- und Leistungspreis zu sagen ist: fehlende Werte mit
    /// ihrem Übernahmeweg, geliehene mit ihrer Herleitung. Leere Liste = alles
    /// gepflegt und nichts geliehen.
    ///
    /// <para>Die Karte zeigt die Preiszeilen im Preisteil, die CO₂-Zeile im
    /// Emissionsteil — jede Aussage steht dort, wo der Wert gepflegt wird.</para>
    /// </summary>
    public IReadOnlyList<Wertluecke> Wertluecken { get; set; }
        = Array.Empty<Wertluecke>();

    // ---- Emissionen (Etappe E3) -----------------------------------------

    /// <summary>Ist der Emissionsarten-Katalog verfügbar (Migrationsschritt 57)?</summary>
    public bool EmissionenVerfuegbar { get; set; }

    /// <summary>Der Modus-Schalter: <c>true</c> = CO₂-Äquivalent (F7).</summary>
    public bool ModusCo2e { get; set; }

    /// <summary>„[Projekt]" bzw. „[globale Vorgabe]" — wo der Modus wirkt.</summary>
    public string ModusOrt { get; set; } = "";

    /// <summary>Die Feldzeilen der ausgewählten Arten (F5).</summary>
    public IReadOnlyList<EmissionsFeldZeile> Emissionszeilen { get; set; }
        = Array.Empty<EmissionsFeldZeile>();

    /// <summary>„CO₂-Äquivalent gesamt (ausgewählte Arten): … g/kWh" (F6).</summary>
    public string EmissionsSumme { get; set; } = "";

    /// <summary>Der F3-Hinweis; leer = keiner.</summary>
    public string EmissionsHinweis { get; set; } = "";

    /// <summary>
    /// Die Fußnote des Emissionsblocks (ET-D-2): welche Größe gezeigt wird und
    /// was weitergeführt, aber nicht gezeigt wird. Leer = keine Fußnote.
    /// </summary>
    public string EmissionsFussnote { get; set; } = "";

    /// <summary>
    /// Im Katalogkontext ist die Bilanzierungsmethode Projektsache und deshalb
    /// nur lesbar; <see cref="ModusHinweis"/> sagt warum.
    /// </summary>
    public bool ModusNurLesend { get; set; }

    /// <summary>Warum die Klappliste gesperrt ist; leer = sie ist es nicht.</summary>
    public string ModusHinweis { get; set; } = "";

    /// <summary>Die drei Bestandsfelder — sie gelten, solange es keinen Katalog gibt.</summary>
    public double AltCO2 { get; set; }
    public double AltSO2 { get; set; }
    public double AltNOx { get; set; }
}

/// <summary>
/// Die ANSICHT einer Trägerkarte (iU9-W4.4): der Bearbeitungsstand und alles,
/// was die Hülle daraus gerechnet hat.
///
/// <para><b>Warum ein Bündel.</b> Ein Blazor-Dialog in einer
/// <c>BlazorDialogForm</c> bekommt seine Parameter EINMAL, beim Aufbau. Alles,
/// was sich während des Dialogs ändert, muss die Komponente deshalb selbst
/// halten und über einen Delegaten nachfragen — dasselbe Muster wie
/// <see cref="KostenKomponenteStand"/> in der Kostenverwaltung.</para>
/// </summary>
public sealed class EnergietraegerAnsicht
{
    /// <summary>Der Bearbeitungsstand; <c>null</c> = kein Träger gewählt.</summary>
    public EnergietraegerStand? Stand { get; set; }

    /// <summary>Summen- und Kohärenzzeile der Strompreis-Details.</summary>
    public PreisblockAnzeige ZerlegungAnzeige { get; set; } = new("", "", false);

    /// <summary>Dasselbe für die Preiszerlegung.</summary>
    public PreisblockAnzeige BestandteilAnzeige { get; set; } = new("", "", false);

    /// <summary>Der Arbeitspreis in ct/kWh — Bezugsgröße der Restzeile.</summary>
    public double ArbeitspreisCtKwh { get; set; }

    /// <summary>
    /// Der Rest-Vorschlag für die Beschaffung [ct/kWh]: Arbeitspreis minus Summe
    /// der übrigen aktiven Anteile. <c>null</c> = kein sinnvoller Vorschlag.
    /// </summary>
    public double? BeschaffungVorschlag { get; set; }

    public Schnellwahlsatz? SatzRegelfall { get; set; }
    public Schnellwahlsatz? SatzReduziert { get; set; }
    public Schnellwahlsatz? SatzRegel { get; set; }
    public Schnellwahlsatz? Satz53a { get; set; }
    public Schnellwahlsatz? Satz54 { get; set; }
    public Schnellwahlsatz? SatzCo2 { get; set; }

    // ---- Die Anzeigekante der Preisbestandteile (ET-D-1) -----------------
    // Gerechnet wird in ct/kWh; ANGEZEIGT und EINGEGEBEN wird in der
    // Abrechnungseinheit. Umgerechnet wird genau einmal, in der Komponente,
    // ueber EnergietraegerPreiskarte.AnteilJeEinheit / AnteilCtKwh.

    /// <summary>„€/m³" bzw. „ct/kWh", solange kein Heizwert da ist.</summary>
    public string BestandteilEinheit { get; set; } = "";

    /// <summary>Heizwert je Abrechnungseinheit [kWh]; 0 = keiner, dann bleibt ct/kWh.</summary>
    public double BestandteilHeizwert { get; set; }

    /// <summary>Warum die Bestandteile in ct/kWh stehen; leer = sie tun es nicht.</summary>
    public string BestandteilHinweis { get; set; } = "";

    /// <summary>„5,50 €/MWh (Hs)" — die Herleitung der Energiesteuerzeile.</summary>
    public string HerleitungEnergiesteuer { get; set; } = "";

    /// <summary>„65 €/t × 2,109 kg/m³" — die Herleitung der BEHG-Zeile.</summary>
    public string HerleitungCo2 { get; set; } = "";

    /// <summary>
    /// Der Rest-Vorschlag für „Beschaffung und Vertrieb" [ct/kWh] — Arbeitspreis
    /// minus Summe der übrigen aktiven Anteile. <c>null</c> = kein Vorschlag.
    /// </summary>
    public double? VertriebVorschlag { get; set; }

    /// <summary>Zeigt die beiden Einstiegskacheln (Ä1: nur beim Stromträger).</summary>
    public bool MitStromkarten { get; set; }

    /// <summary>Zeigt die Karte „Kostenprofil" (Projektwahrheit, nur im Projekt).</summary>
    public bool MitKostenprofil { get; set; }

    /// <summary>Statuszeile der Kostenprofil-Karte.</summary>
    public string KarteProfilStatus { get; set; } = "";

    /// <summary>Statuszeile der Spotpreis-Karte.</summary>
    public string KarteSpotStatus { get; set; } = "";

    /// <summary>Bezeichnung des Trägers im Stammkopf (Ä9).</summary>
    public string StammName { get; set; } = "";

    /// <summary>Gruppe des Trägers im Stammkopf.</summary>
    public int? StammGruppe { get; set; }
}
