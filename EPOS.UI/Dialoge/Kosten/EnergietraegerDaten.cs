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
public sealed record PreishistorieZeile(string GueltigAb, string Heizwert, string Basiseinheit,
                                        string Arbeitspreis, string Grundpreis,
                                        string Leistungspreis);

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

    /// <summary>Die wählbaren Preisbasen (<c>cmbUnit</c>) — Id = Index.</summary>
    public IReadOnlyList<(int Id, string Text)> Preisbasen { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Die gewählte Preisbasis.</summary>
    public int? PreisbasisId { get; set; }

    /// <summary>Der Leistungspreis-Modus: <c>true</c> = Monat, <c>false</c> = Jahr (FK6).</summary>
    public bool LeistungsModusMonat { get; set; }

    /// <summary>Statuszeile der Saisonreihe (FK6a); leer = keine gepflegt.</summary>
    public string ReihenStatus { get; set; } = "";

    // ---- Formel (nur mit Heizwert) -------------------------------------

    /// <summary>Zeigt die Formelgruppe? Ohne Heizwert gibt es keine Formel.</summary>
    public bool MitFormel { get; set; }

    /// <summary>„0,0812 €" — der Preis je Kilowattstunde.</summary>
    public string PreisJeKwh { get; set; } = "";

    /// <summary>„0,65 € ÷ 8,00 kWh = 0,0812 €/kWh" bzw. „Direktabrechnung nach kWh".</summary>
    public string FormelText { get; set; } = "";

    // ---- Umrechnungsblock (Etappe K3) ----------------------------------

    /// <summary>Die Regeln des Brennstoffs — Speicherkopie, siehe Zeilenklasse.</summary>
    public IReadOnlyList<UmrechnungsregelZeile> Regeln { get; set; }
        = Array.Empty<UmrechnungsregelZeile>();

    /// <summary>„effektiv: 1 ‹Einheit› = X kWh (Hi) / Y kWh (Hs)".</summary>
    public string EffektivText { get; set; } = "";

    /// <summary>Der rote Verstoßhinweis (L2); leer = alles in Ordnung.</summary>
    public string VerstossText { get; set; } = "";

    // ---- Preisblöcke ----------------------------------------------------

    /// <summary>Aufschlagsblock — nur beim Stromträger belegt (AP4).</summary>
    public StromAufschlaegeStand? Aufschlaege { get; set; }

    /// <summary>Preiszerlegung — nur bei der Brennstoff-Familie belegt (B2).</summary>
    public BrennstoffBestandteileStand? Bestandteile { get; set; }

    /// <summary>„Bezugspreis inkl. Aufschläge: … ct/kWh" (Ä16); leer = kein Strom.</summary>
    public string EffektivpreisText { get; set; } = "";

    /// <summary>Ä16: Der Schalter „Aufschläge in der Wirtschaftlichkeit berücksichtigen".</summary>
    public bool MitAufschlagSchalter { get; set; }

    /// <summary>Sein Stand.</summary>
    public bool AufschlaegeAnwenden { get; set; }

    // ---- Historie -------------------------------------------------------

    /// <summary>Gültig-ab-Datum des zu schreibenden Standes.</summary>
    public DateOnly? GueltigAb { get; set; }

    /// <summary>Die Preishistorie, jüngste zuerst.</summary>
    public IReadOnlyList<PreishistorieZeile> Historie { get; set; }
        = Array.Empty<PreishistorieZeile>();

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

    /// <summary>Summen- und Restzeile des Aufschlagsblocks.</summary>
    public PreisblockAnzeige AufschlagAnzeige { get; set; } = new("", "", false);

    /// <summary>Dasselbe für die Preiszerlegung.</summary>
    public PreisblockAnzeige BestandteilAnzeige { get; set; } = new("", "", false);

    /// <summary>Der Arbeitspreis in ct/kWh — Bezugsgröße der Restzeile.</summary>
    public double ArbeitspreisCtKwh { get; set; }

    public Schnellwahlsatz? SatzRegelfall { get; set; }
    public Schnellwahlsatz? SatzReduziert { get; set; }
    public Schnellwahlsatz? SatzRegel { get; set; }
    public Schnellwahlsatz? Satz53a { get; set; }
    public Schnellwahlsatz? Satz54 { get; set; }
    public Schnellwahlsatz? SatzCo2 { get; set; }

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
