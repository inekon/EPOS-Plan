namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Die Beschriftungen des Dialogs „BHKW-Wirtschaftlichkeit" — einmal aufgeloest,
/// nicht bei jedem Zeichnen.
///
/// <para><b>Warum eine eigene Klasse.</b> Razor kann einen Ausdruck mit
/// Zeichenketten nicht bequem in einem Attributwert tragen
/// (<c>Titel="@BhwTexte.T("BHW_G1", "Anlagen")"</c> ist keine gueltige
/// Attributsyntax). Alle Texte stehen deshalb hier, werden beim Aufbau des
/// Dialogs EINMAL aus dem Ressourcenkatalog geholt und in der Komponente nur
/// noch als Feld gelesen. Das ist zugleich die Stelle, an der der
/// Feldkarten-Abgleich die Beschriftungen nachschlagen kann.</para>
///
/// <para>Schluessel und deutscher Rueckfall sind wortgleich aus der geloeschten
/// WinForms-Fassung <c>Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs</c>
/// uebernommen (Etappe B5, Konzept § 6.4).</para>
/// </summary>
public sealed class BhkwWirtschaftlichkeitTexte
{
    private static string T(string schluessel, string rueckfall) => BhwTexte.T(schluessel, rueckfall);

    // ------------------------------------------------------------ Rahmen
    public string Titel { get; } = T("BHW_TITEL", "BHKW-Wirtschaftlichkeit");
    public string Schliessen { get; } = T("BHW_BTN_SCHLIESSEN", "Schließen");

    // ------------------------------------------------------- Gruppentitel
    public string G1 { get; } = T("BHW_G1", "Anlagen");
    public string G1b { get; } = T("BHW_G1B", "Angaben der gewählten Anlage — leer bzw. 0 = Projektvorgabe");
    public string G2 { get; } = T("BHW_G2", "KWK-Zuschlag (Projektvorgabe)");
    public string G3 { get; } = T("BHW_G3", "Energiesteuer (Projektvorgabe)");
    public string G4 { get; } = T("BHW_G4", "Stromsteuer (Projektvorgabe)");
    public string G5 { get; } = T("BHW_G5", "Hilfsstrom");
    public string G6 { get; } = T("BHW_G6", "Vorschau — zuletzt gebuchter Lauf");
    public string GKohaerenz { get; } = T("BHW_G_KOHAERENZ", "Kohärenzprüfung (Energie- und Stromsteuer)");

    // ------------------------------------------- Gruppe 1: Tabellenspalten
    /// <summary>Kopf der Wahlspalte. NEU in B5b: Die WinForms-Fassung waehlte ueber
    /// die markierte Zeile der <c>ListView</c>; ein <c>Raster</c> hat keine
    /// Zeilenmarkierung, also bekommt die Wahl eine eigene Spalte.</summary>
    public string SpWahl { get; } = T("BHW_SP_WAHL", "Wahl");
    public string SpProjekt { get; } = T("BHW_SP_PROJEKT", "Projekt");
    public string SpAnlage { get; } = T("BHW_SP_ANLAGE", "Anlage");
    public string SpPel { get; } = T("BHW_SP_PEL", "P_el [kW]");
    public string SpBrennstoff { get; } = T("BHW_SP_BRENNSTOFF", "Brennstoff");
    public string SpStichtag { get; } = T("BHW_SP_STICHTAG", "Stichtag");
    public string SpIbn { get; } = T("BHW_SP_IBN", "Inbetriebnahme");
    public string SpAnlagenart { get; } = T("BHW_SP_ANLAGENART", "Anlagenart");

    // ----------------------------------------- Gruppe 1b: Angaben der Anlage
    public string AStichtag { get; } = T("BHW_A_STICHTAG", "Stichtag (Bestellung/Genehmigung):");
    public string AIbn { get; } = T("BHW_A_IBN", "Inbetriebnahme:");
    public string AAnlagenart { get; } = T("BHW_A_ANLAGENART", "Anlagenart:");
    public string AEigenfall { get; } = T("BHW_A_EIGENFALL", "Eigenstrom nach § 6 Abs. 3:");
    public string ASatzEinsp { get; } = T("BHW_A_SATZ_EINSP", "Satz Einspeisung [ct/kWh] (0 = Projektsatz):");
    public string ASatzEigen { get; } = T("BHW_A_SATZ_EIGEN", "Satz Eigenstrom [ct/kWh] (0 = Projektsatz):");
    public string AKontingent { get; } = T("BHW_A_KONTINGENT", "Vbh-Kontingent [h] (0 = Projektwert):");
    public string ADeckel { get; } = T("BHW_A_DECKEL", "Vbh-Jahresdeckel [h/a] (0 = Staffel):");
    public string AEnergiesteuer { get; } = T("BHW_A_ENERGIESTEUER", "Energiesteuerentlastung (Anlage):");
    public string AAufteilung { get; } = T("BHW_A_AUFTEILUNG", "Brennstoff auf Strom/Wärme (Anlage):");
    public string AHilfsanteil { get; } = T("BHW_A_HILFSANTEIL", "Hilfsenergieanteil [% des Endenergiebedarfs] (0 = keine):");
    public string AHilfsBasis { get; } = T("BHW_A_HILFS_BASIS",
        "Vorschlag BHKW 2–4 %. Bemessen wird am Endenergiebedarf (Brennstoff) dieser Anlage — nicht an den Kosten.");

    /// <summary>NEU in B5b: In WinForms waren die elf Felder ohne gewaehlte Zeile
    /// nur gesperrt (<c>FelderAktiv(false)</c>) und blieben sichtbar. Im Blazor-Layout
    /// entfaellt die leere Feldwand; an ihre Stelle tritt dieser Satz.</summary>
    public string AOhneWahl { get; } = T("BHW_A_OHNE_WAHL", "Keine Anlage gewählt.");

    // ---------------------------------------------- Gruppe 2: KWK-Zuschlag
    public string PBonusEigen { get; } = T("BHW_P_BONUS_EIGEN", "Bonus Eigenstrom [ct/kWh] (0 = aus):");
    public string PBonusEinsp { get; } = T("BHW_P_BONUS_EINSP", "Bonus Einspeisung [ct/kWh]:");
    public string PDeckel { get; } = T("BHW_P_DECKEL", "Vbh-Deckel-Override [h/a]:");
    public string PKontingent { get; } = T("BHW_P_KONTINGENT", "Vbh-Kontingent gesamt [h] (0 = automatisch):");
    public string PAbschlag { get; } = T("BHW_P_ABSCHLAG", "Abschlag Negativstunden [%]:");
    public string PTatbestand { get; } = T("BHW_P_TATBESTAND", "Eigenstrom-Tatbestand (§ 6 Abs. 3):");
    public string PAnlagenart { get; } = T("BHW_P_ANLAGENART", "Anlagenart (§ 8):");
    public string PKostenanteil { get; } = T("BHW_P_KOSTENANTEIL", "Anteil Neuherstellungskosten [%]:");
    public string PPauschal { get; } = T("BHW_P_PAUSCHAL", "Pauschale § 9 KWKG (nur bis 2 kWel, einmalig)");
    public string PStichtag { get; } = T("BHW_P_STICHTAG", "Stichtag, Vorgabe je Anlage:");
    public string PIbn { get; } = T("BHW_P_IBN", "Inbetriebnahme, Vorgabe je Anlage:");
    public string BtnVorschlag { get; } = T("BHW_BTN_VORSCHLAG", "Vorschlag in die Satzfelder übernehmen");

    // --------------------------------------------- Gruppe 3: Energiesteuer
    public string EWahl { get; } = T("BHW_E_WAHL", "Energiesteuerentlastung:");
    public string EAufteilung { get; } = T("BHW_E_AUFTEILUNG", "Brennstoff auf Strom/Wärme:");
    public string ENutzungsgrad { get; } = T("BHW_E_NUTZUNGSGRAD", "Jahresnutzungsgrad [%] (0 = nicht erfasst):");
    public string EOhneHerkunft { get; } = T("BHW_E_OHNE_HERKUNFT",
        "Keine Gutschrift im zuletzt gebuchten Lauf — es wurde kein Satz verwendet.");

    // ---------------------------------------------- Gruppe 4: Stromsteuer
    public string SUnternehmensart { get; } = T("BHW_S_UNTERNEHMENSART", "Unternehmensart:");
    public string SRaeumlich { get; } = T("BHW_S_RAEUMLICH", "Räumlicher Zusammenhang (4,5 km) gegeben");
    public string SHocheffizienz { get; } = T("BHW_S_HOCHEFFIZIENZ", "Hocheffizienz nachgewiesen");
    public string SModus { get; } = T("BHW_S_MODUS", "Modus § 9 Abs. 1 Nr. 3:");
    public string SModusB6 { get; } = T("BHW_S_MODUS_B6",
        "ab B6 — bis dahin gilt fest „Ausweis“ (nicht im Kapitalwert).");
    public string BtnStrombezug { get; } = T("BHW_BTN_STROMBEZUG", "Strombezug…");
    public string BtnBhkwTarif { get; } = T("BHW_BTN_BHKW_TARIF", "BHKW-Tarif…");

    /// <summary>NEU in B5b: Der Sprung in einen WinForms-Dialog laeuft ueber die
    /// Huelle und schliesst diesen Dialog vorher (siehe <see cref="BhkwSprung"/>).
    /// Der Satz sagt es, bevor es geschieht.</summary>
    public string SSprungHinweis { get; } = T("BHW_S_SPRUNG_HINWEIS",
        "Der Sprung schließt diesen Dialog und öffnet ihn danach wieder — bitte vorher speichern.");

    // ------------------------------------------------- Kohaerenz und Hilfsstrom
    public string KLeer { get; } = T("BHW_K_LEER", "Keine Auffälligkeit im zuletzt gebuchten Lauf.");
    public string HBasis { get; } = T("BHW_H_BASIS",
        "Der Anteil wird je Anlage oben gepflegt und am ENDENERGIEBEDARF (Brennstoff) der Anlage bemessen — " +
        "nicht an den Kosten. Die Menge mindert die zuschlagsfähige Nettostromerzeugung.");
    public string HKessel { get; } = T("BHW_H_KESSEL",
        "Heizkessel der Gruppe: Der Hilfsenergieanteil wird für Kessel mitgerechnet, aber nicht hier gepflegt.");
    public string HOhneLauf { get; } = T("BHW_H_OHNE_LAUF",
        "Mengenkette: noch kein gebuchtes Ergebnis — bitte in der Wirtschaftlichkeit „Berechnen“.");
    public string HKette1 { get; } = T("BHW_H_KETTE1",
        "Stromerzeugung brutto {0} MWh/a − Hilfsstrom {1} MWh/a = Nettostromerzeugung {2} MWh/a");
    public string HKette2 { get; } = T("BHW_H_KETTE2",
        "davon Eigenverbrauch {0} MWh/a, Einspeisung {1} MWh/a");

    // ------------------------------------------------------ Herleitung Gruppe 2
    public string HerleitungEinsp { get; } = T("BHW_HERLEITUNG_EINSP", "Einspeisung {0} ct/kWh — {1}");
    public string HerleitungEigen { get; } = T("BHW_HERLEITUNG_EIGEN", "Eigenstrom {0} ct/kWh — {1}");

    // -------------------------------------------------------- Warnzeilen Gruppe 1
    public string WAusschreibung { get; } = T("BHW_W_AUSSCHREIBUNG",
        "Ausschreibung nach § 8a KWKG: {0} über {1} kW.");
    public string WStromsteuer { get; } = T("BHW_W_STROMSTEUER",
        "Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 entfällt: {0} über {1} kW.");
    public string WHeizoel { get; } = T("BHW_W_HEIZOEL",
        "Heizöl-Ausschluss ab Inbetriebnahme 2025: {0}.");

    // ----------------------------------------------------------- Gruppe 6
    public string VOhneLauf { get; } = T("BHW_V_OHNE_LAUF",
        "Noch kein gebuchtes Ergebnis — die Vorschau erscheint nach „Berechnen“ in der Wirtschaftlichkeit.");
    public string VZuschlag { get; } = T("BHW_V_ZUSCHLAG", "KWK-Zuschlag p. a.");
    public string VEnergiesteuer { get; } = T("BHW_V_ENERGIESTEUER", "Energiesteuer p. a.");
    public string VStromsteuer { get; } = T("BHW_V_STROMSTEUER", "Stromsteuer p. a.");
    public string VEinspeisung { get; } = T("BHW_V_EINSPEISUNG", "Einspeiseerlös KWK p. a.");
    public string VVermieden { get; } = T("BHW_V_VERMIEDEN", "Vermiedene Stromkosten p. a. (Ausweis)");
    public string VStand { get; } = T("BHW_V_STAND", "Stand: {0} — nach dem Speichern neu berechnen.");

    // ----------------------------------------------------------- Speichern
    public string MsgFehler { get; } = T("BHW_MSG_FEHLER", "{0} Angabe(n) konnten nicht gespeichert werden.");
}
