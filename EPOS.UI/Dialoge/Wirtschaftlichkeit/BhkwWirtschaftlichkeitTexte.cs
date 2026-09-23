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

    /// <summary>Der OK-Knopf der Fußleiste. Er schreibt und schließt — „Speichern"
    /// ist hier also keine Behauptung, sondern das, was der Knopf tut. Hausschlüssel
    /// <c>ADM_BTN_SPEICHERN</c> (W-E2, Mockup-Prüfung 04) — kein eigener,
    /// gleichlautender Schlüssel für denselben Text.</summary>
    public string Speichern { get; } = T("ADM_BTN_SPEICHERN", "Speichern");

    /// <summary>Der Abbrechen-Knopf — derselbe Hausschlüssel wie in den
    /// Nachbardialogen der Wirtschaftlichkeit.</summary>
    public string Abbrechen { get; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");

    // ------------------------------------------------------- Gruppentitel
    public string G1 { get; } = T("BHW_G1", "Anlagen");
    // ETAPPE E2 (Mockup-Pruefung 03/#59): Der Rueckfalltext fuehrte einen Zusatz
    // („— leer bzw. 0 = Projektvorgabe"), den der Ressourcentext nicht hat. Damit
    // stand je nach Sprachstand eine andere Ueberschrift da. Der Rueckfall ist der
    // deutsche Ressourcentext, Wort fuer Wort — die Regel, dass leer bzw. 0 die
    // Projektvorgabe meint, steht in der Herleitungszeile der Gruppe.
    public string G1b { get; } = T("BHW_G1B", "Angaben der gewählten Anlage");
    public string G2 { get; } = T("BHW_G2", "Projektweite KWK-Angaben");
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
    public string ASatzEinsp { get; } = T("BHW_A_SATZ_EINSP", "Satz Einspeisung [ct/kWh] (0 = kein Zuschlag):");
    public string ASatzEigen { get; } = T("BHW_A_SATZ_EIGEN", "Satz Eigenstrom [ct/kWh] (0 = kein Zuschlag):");
    public string AKontingent { get; } = T("BHW_A_KONTINGENT", "Vbh-Kontingent [h] (0 = nach § 8 abgeleitet):");
    public string ADeckel { get; } = T("BHW_A_DECKEL", "Vbh-Jahresdeckel [h/a] (0 = Staffel):");
    public string AKostenanteil { get; } = T("BHW_A_KOSTENANTEIL",
        "Anteil Neuherstellungskosten [%] (§ 8 Abs. 2/3):");
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
    /// <summary>
    /// AUFTRAG #325 (Anwenderwunsch 17.09.2026): Der Satz, mit dem der EINGESPEISTE
    /// KWK-Strom vergütet wird, steht bei den Angaben des BHKW und nicht mehr in
    /// einem Sammelabschnitt der Wirtschaftlichkeitsparameter. Der Schlüssel zieht
    /// unverändert mit — es ist dasselbe Feld an derselben Modelleigenschaft
    /// (<c>WirtschaftlichkeitParameter.EinspeiseverguetungKWK</c>), nur an einer
    /// anderen Eingabestelle.
    /// </summary>
    public string PEinspKwk { get; } = T("WPAR_EINSP_KWK",
        "Einspeisevergütung KWK-Strom [€/kWh]:");

    /// <summary>
    /// Was der Satz sonst noch bewegt (SP-E-5, sinngemäß für den KWK-Satz): Er
    /// bewertet den eingespeisten BHKW-Strom in der Wirtschaftlichkeit UND stellt
    /// den Verkaufspreis v_bhkw der Speicherwelt. Wer ihn ändert, muss erfahren,
    /// was er damit noch ändert.
    /// </summary>
    public string PEinspKwkHinweis { get; } = T("BHW_P_EINSP_KWK_HINWEIS",
        "Der Satz gilt für die ganze Anwendung: Er bewertet den eingespeisten " +
        "BHKW-Strom in der Wirtschaftlichkeit UND stellt den Verkaufspreis, mit dem " +
        "Stromspeicher und Speicherflotte den BHKW-Überschuss rechnen (v_bhkw). " +
        "0 heißt „nicht gepflegt“; dann gilt für BHKW-Strom der PV-Satz aus den " +
        "Wirtschaftlichkeits-Parametern, und ohne den rechnet die Speicherwelt mit 0 " +
        "und weist das im Protokoll aus. Der KWK-Zuschlag kommt obendrauf — er " +
        "ersetzt die Vergütung nicht.");

    public string PAbschlag { get; } = T("BHW_P_ABSCHLAG", "Abschlag Negativstunden [%]:");
    public string PPauschal { get; } = T("BHW_P_PAUSCHAL", "Pauschale § 9 KWKG (nur bis 2 kWel, einmalig)");
    public string PStichtag { get; } = T("BHW_P_STICHTAG", "Stichtag (Bestellung/Genehmigung, § 6):");
    public string PIbn { get; } = T("BHW_P_IBN", "Förderbeginn (Startjahr der Reihen):");

    /// <summary>Die leise Zeile unter der Gruppe: WARUM hier nur noch fünf Angaben
    /// stehen. Ohne sie sucht ein Anwender, der den Dialog kennt, die Sätze.</summary>
    public string PNurProjektweit { get; } = T("BHW_P_NUR_PROJEKTWEIT",
        "Satz, Kontingent, Jahresdeckel, Anlagenart, Eigenstrom-Tatbestand und der " +
        "Anteil an den Neuherstellungskosten stehen an der Anlage (§ 7 und § 8 KWKG " +
        "bemessen sie je Anlage) — oben unter „Angaben der gewählten Anlage“; die beiden " +
        "Sätze und das Kontingent mit einem Knopf für den Katalogvorschlag am Feld.");

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
    public string SModusHerleitung { get; } = T("BHW_S_MODUS_HERLEITUNG",
        "Ausweis: Die Befreiung wird gezeigt und nicht im Kapitalwert gerechnet. " +
        "Erlös: Sie wird als Erlös gebucht — nur, wenn der Bezugspreis die " +
        "Stromsteuer enthält.");
    public string BtnBhkwTarif { get; } = T("BHW_BTN_BHKW_TARIF", "BHKW-Tarif…");

    /// <summary>Der Sprung laeuft ueber die Huelle und schliesst diesen Dialog
    /// vorher (siehe <see cref="BhkwSprung"/>). Er nimmt denselben Weg wie OK —
    /// erst schreiben, dann hinaus —, sonst waeren die Eingaben des Anwenders mit
    /// dem Sprung verloren. Der Satz sagt es, bevor es geschieht.</summary>
    public string SSprungHinweis { get; } = T("BHW_S_SPRUNG_HINWEIS",
        "Der Sprung speichert die Eingaben, schließt diesen Dialog und öffnet ihn danach wieder.");

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

    // -------------------------------- Vorschlagsknöpfe am Feld (Gruppe 1b)
    /// <summary>Beschriftung der drei Knöpfe — dieselbe an jedem, weil die Zeile
    /// darüber sagt, welches Feld gemeint ist.</summary>
    public string BtnVorschlagFeld { get; } = T("BHW_BTN_VORSCHLAG_FELD", "Vorschlag übernehmen");

    public string HerleitungEinsp { get; } = T("BHW_HERLEITUNG_EINSP", "Einspeisung {0} ct/kWh — {1}");
    public string HerleitungEigen { get; } = T("BHW_HERLEITUNG_EIGEN", "Eigenstrom {0} ct/kWh — {1}");

    /// <summary>Grundlage des Kontingentvorschlags: abgeleiteter Wert und Herleitung
    /// aus <c>KwkgKontingentRechner</c>.</summary>
    public string HerleitungKontingent { get; } = T("BHW_HERLEITUNG_KONTINGENT",
        "Kontingent {0} Vbh — {1}");

    /// <summary>Sperrgrund: ohne elektrische Nennleistung gibt es keine
    /// Leistungsstaffel und damit keinen Satz.</summary>
    public string SperrOhnePel { get; } = T("BHW_SPERR_OHNE_PEL",
        "Kein Vorschlag: Für diese Anlage ist keine elektrische Nennleistung erfasst.");

    /// <summary>Sperrgrund: ohne Tatbestand des § 6 Abs. 3 gibt es keinen Zuschlag
    /// auf selbst genutzten Strom.</summary>
    public string SperrOhneEigenfall { get; } = T("BHW_SPERR_OHNE_EIGENFALL",
        "Kein Vorschlag: Ohne Tatbestand nach § 6 Abs. 3 gibt es keinen Zuschlag auf " +
        "selbst genutzten Strom (§ 7 Abs. 2) — Feld „Eigenstrom nach § 6 Abs. 3“ füllen.");

    /// <summary>Sperrgrund: ohne Anlagenart wählt § 8 keine Kontingentstufe.</summary>
    public string SperrOhneAnlagenart { get; } = T("BHW_SPERR_OHNE_ANLAGENART",
        "Kein Vorschlag: Ohne Anlagenart wählt § 8 KWKG keine Kontingentstufe — " +
        "Feld „Anlagenart“ füllen; modernisiert und nachgerüstet brauchen zusätzlich " +
        "den Anteil an den Neuherstellungskosten.");

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

    /// <summary>ETAPPE B7: Der Lauf liegt, trägt aber keine Erlösposition — dann
    /// steht das da, statt einer leeren Gruppe.</summary>
    public string VOhneRubrik { get; } = T("BHW_G6_OHNE_RUBRIK",
        "Noch kein Lauf — die Rubrik füllt sich mit „Berechnen“ im Reiter Wirtschaftlichkeit.");

    // ----------------------------------------------------------- Speichern
    public string MsgFehler { get; } = T("BHW_MSG_FEHLER", "{0} Angabe(n) konnten nicht gespeichert werden.");

    // ------------------- ETAPPE E7c — Überlagerung „Sätze und Herkunft" (U22, K‑1)
    //
    // Gebaut so weit, wie die zwei Felder des zweiten Falls des § 2 Nr. 16 KWKG es
    // brauchen (Entscheid E7‑Q2 (5)): Kennzeichen „Vorrichtung zur Abwärmeabfuhr" und
    // Stromkennzahl σ mit Vorschlag, Herkunft, eigenem Wert und „Übernehmen". Die
    // Schlüssel folgen dem Ressourcenplan des Mockups (BHW_UEB_*, BHW_FLD_*,
    // BHW_HERL_*).

    /// <summary>Knopf bei den Angaben der gewählten Anlage, der die Überlagerung öffnet.</summary>
    public string UebKnopf { get; } = T("BHW_UEB_KNOPF_ANLAGE", "Sätze und Herkunft…");

    /// <summary>Titel der Überlagerung; {0} = Bezeichner der Anlage.</summary>
    public string UebTitel { get; } = T("BHW_UEB_TITEL", "Sätze und Herkunft — {0}");

    public string UebGKwkg { get; } = T("BHW_UEB_G_KWKG", "KWK-Zuschlag — diese Anlage");
    public string UebKwkStrom { get; } = T("BHW_UEB_KWK_STROM", "KWK-Strom (§ 2 Nr. 16 KWKG)");
    public string UebFall1 { get; } = T("BHW_FLD_FALL1", "Fall 1 — Nettostromerzeugung");
    public string UebFall2 { get; } = T("BHW_FLD_ABWAERMEABFUHR", "Fall 2 — Vorrichtung zur Abwärmeabfuhr");

    /// <summary>Wirkung von Fall 1 aus dem gebuchten Lauf; {0} = Nettostromerzeugung [MWh].</summary>
    public string UebFall1Menge { get; } = T("BHW_UEB_FALL1_MENGE",
        "→ {0} MWh Nettostromerzeugung im zuletzt gebuchten Lauf");

    /// <summary>Wirkung von Fall 1 ohne gebuchten Lauf.</summary>
    public string UebFall1OhneLauf { get; } = T("BHW_UEB_FALL1_OHNE_LAUF",
        "→ die Nettostromerzeugung der Anlage (Klemme minus Hilfsstrom)");

    /// <summary>Die Formel des zweiten Falls.</summary>
    public string UebFall2Formel { get; } = T("BHW_UEB_FALL2_FORMEL",
        "→ min(Nettostromerzeugung ; Nutzwärme × σ) — Nutzwärme: Wärmeproduktion des Moduls " +
        "abzüglich seines Anteils am Wärmeüberschuss (nach P_el)");

    public string UebSpGroesse { get; } = T("BHW_UEB_SP_GROESSE", "Größe");
    public string UebSpVorschlag { get; } = T("BHW_UEB_SP_VORSCHLAG", "Vorschlag");
    public string UebSpHerkunft { get; } = T("BHW_UEB_SP_HERKUNFT", "Herkunft");
    public string UebSpEigen { get; } = T("BHW_UEB_SP_EIGEN", "eigener Wert");
    public string UebSpGilt { get; } = T("BHW_UEB_SP_GILT", "gilt");
    public string UebLeerVorschlag { get; } = T("BHW_UEB_LEER_VORSCHLAG", "leer = Vorschlag");
    public string UebSigma { get; } = T("BHW_FLD_STROMKENNZAHL", "Stromkennzahl σ");

    /// <summary>Herkunft des Vorschlags; {0} = P_el, {1} = P_th [kW].</summary>
    public string UebHerkunftSigma { get; } = T("BHW_HERL_STROMKENNZAHL",
        "P_el ÷ P_th der Gerätezeile: {0} kW ÷ {1} kW");

    /// <summary>Herkunft, wenn die Gerätezeile keinen Vorschlag hergibt — zugleich der
    /// Sperrgrund des Knopfes „Vorschlag übernehmen".</summary>
    public string UebHerkunftSigmaOhne { get; } = T("BHW_HERL_STROMKENNZAHL_OHNE",
        "Gerätezeile ohne P_el oder P_th — kein Vorschlag");

    /// <summary>Spalte „gilt": {0} = σ.</summary>
    public string UebGiltVorschlag { get; } = T("BHW_UEB_GILT_VORSCHLAG", "{0} Vorschlag");

    /// <summary>Spalte „gilt" bei eigenem Wert: {0} = eigener Wert, {1} = Vorschlag.</summary>
    public string UebGiltEigen { get; } = T("BHW_UEB_GILT_EIGEN", "{0} eigener Wert — Vorschlag {1}");

    /// <summary>Spalte „gilt" bei eigenem Wert ohne Vorschlag: {0} = eigener Wert.</summary>
    public string UebGiltEigenOhne { get; } = T("BHW_UEB_GILT_EIGEN_OHNE", "{0} eigener Wert");

    /// <summary>Spalte „gilt" ohne jede Kennzahl — kein Ersatz, keine Vorgabe (E7‑Q2 (2)).</summary>
    public string UebGiltKeine { get; } = T("BHW_UEB_GILT_KEINE",
        "keine — kein KWK-Strom nach Fall 2, kein Zuschlag");

    /// <summary>Spalte „gilt" bei Fall 1: σ wird nicht gelesen.</summary>
    public string UebGiltFall1 { get; } = T("BHW_UEB_GILT_FALL1", "— (Fall 1 liest keine Stromkennzahl)");

    public string UebInfo { get; } = T("BHW_UEB_INFO_SIGMA",
        "Leer heißt: σ wird beim Rechnen aus P_el ÷ P_th der Gerätezeile gebildet. Ein eigener " +
        "Wert gilt dauerhaft, auch wenn sich die Gerätezeile ändert; „Vorschlag übernehmen“ " +
        "leert das Feld — der Weg zurück. Gibt es weder einen eigenen Wert noch P_el und P_th, " +
        "gibt es keinen KWK-Strom nach Fall 2 und für diese Anlage keinen Zuschlag. Gelesen wird " +
        "σ nur bei Fall 2.");

    public string UebUebernehmen { get; } = T("BHW_UEB_UEBERNEHMEN", "Übernehmen");

    /// <summary>Die Zeile bei den Angaben der Anlage: Fall 1.</summary>
    public string AKwkFall1 { get; } = T("BHW_A_KWK_FALL1",
        "KWK-Strom (§ 2 Nr. 16 KWKG): Fall 1 — Nettostromerzeugung.");

    /// <summary>Die Zeile bei den Angaben der Anlage: Fall 2; {0} = σ, {1} = Herkunft.</summary>
    public string AKwkFall2 { get; } = T("BHW_A_KWK_FALL2",
        "KWK-Strom (§ 2 Nr. 16 KWKG): Fall 2 — Vorrichtung zur Abwärmeabfuhr, Stromkennzahl σ {0} ({1}).");

    /// <summary>Die Zeile bei den Angaben der Anlage: Fall 2 ohne Kennzahl; {0} = Grund.</summary>
    public string AKwkFall2Ohne { get; } = T("BHW_A_KWK_FALL2_OHNE",
        "KWK-Strom (§ 2 Nr. 16 KWKG): Fall 2 — Vorrichtung zur Abwärmeabfuhr, aber keine " +
        "Stromkennzahl ({0}) — kein Zuschlag nach Fall 2.");
}
