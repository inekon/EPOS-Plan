namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte des Dialogs „Brauchwasser-Zapfprofil" in allen drei Stufen samt Vorschau
/// und Fußleiste (Umsetzungskonzept Zapfprofilgenerator 5.1, 5.3) — EIN Parameter statt
/// zweihundertvierzig.
/// </summary>
/// <remarks>
/// <para><b>Warum gebündelt.</b> Hausregel „ab etwa zehn Anzeigetexten ein Bündel"
/// (<c>EPOS.UI/CLAUDE.md</c>). Beschriftungen, kein Zustand: Kontext, Katalog, Arbeitsstand
/// und Vorschau stehen in <see cref="ZapfprofilDaten"/>; Meldungen bringen ihren Satz selbst
/// mit (<see cref="ZapfprofilMeldung"/>).</para>
/// <para>Jede Eigenschaft trägt ihren Ressourcenschlüssel im Kommentar; der Vorgabewert ist der
/// deutsche Rückfall und gleicht dem Wert der neutralen Ressource (Wache
/// <c>ZapfprofilTexteTests</c>). Platzhalter <c>{0}</c> … füllt der Dialog. Begriffe nach
/// Glossar § 14 „Trinkwarmwasser und Zapfprofil".</para>
/// </remarks>
public sealed class ZapfprofilTexte
{
    // ------------------------------------------------------------ Kopf und Kontext

    /// <summary><c>ZPG_TITEL</c></summary>
    public string Titel { get; set; } = "Brauchwasser-Zapfprofil";

    /// <summary><c>ZPG_TITEL_PROJEKT</c></summary>
    public string TitelProjekt { get; set; } = "Brauchwasser-Zapfprofil – {0}";

    /// <summary><c>ZPG_INFO_BEDIENUNG</c></summary>
    public string InfoBedienung { get; set; } = "Bedienung";

    /// <summary><c>ZPG_INFO_RECHENWEG</c></summary>
    public string InfoRechenweg { get; set; } = "Rechenweg";

    /// <summary><c>ZPG_KONTEXT_KLIMAREGION</c></summary>
    public string KontextKlimaregion { get; set; } = "Klimaregion {0}";

    /// <summary><c>ZPG_KONTEXT_KALENDER</c></summary>
    public string KontextKalender { get; set; } = "Kalender: 1. Januar = {0}, 365 Tage";

    /// <summary><c>ZPG_KONTEXT_BILANZGRENZE</c></summary>
    public string KontextBilanzgrenze { get; set; } = "Bilanzgrenze: Zapfenergie an der Zapfstelle; Zirkulation als eigene Teilreihe";

    /// <summary><c>ZPG_KONTEXT_UEBERSCHRIEBEN</c></summary>
    public string KontextUeberschrieben { get; set; } = "{0} Werte überschrieben";

    /// <summary><c>ZPG_LBL_STUFE</c></summary>
    public string LabelStufe { get; set; } = "Stufe";

    /// <summary><c>ZPG_STUFE_EINFACH</c></summary>
    public string StufeEinfach { get; set; } = "Einfach";

    /// <summary><c>ZPG_STUFE_ERWEITERT</c></summary>
    public string StufeErweitert { get; set; } = "Erweitert";

    /// <summary><c>ZPG_STUFE_EXPERTE</c></summary>
    public string StufeExperte { get; set; } = "Experte";

    /// <summary><c>ZPG_GRUND_NOCH_NICHT</c></summary>
    public string GrundNochNicht { get; set; } = "In dieser Fassung noch nicht verfügbar.";

    // ------------------------------------------------------------ Zonenliste

    /// <summary><c>ZPG_GRP_ZONEN</c></summary>
    public string GruppeZonen { get; set; } = "Nutzungszonen";

    /// <summary><c>ZPG_GRP_ZONEN_SUMME</c></summary>
    public string GruppeZonenSumme { get; set; } = "{0} MWh/a Zapfung";

    /// <summary><c>ZPG_GRP_ZONE</c></summary>
    public string GruppeZone { get; set; } = "{0} · Nutzung";

    /// <summary><c>ZPG_SP_ZONE</c></summary>
    public string SpalteZone { get; set; } = "Zone";

    /// <summary><c>ZPG_SP_NUTZUNGSART</c></summary>
    public string SpalteNutzungsart { get; set; } = "Nutzungsart";

    /// <summary><c>ZPG_SP_BEZUGSGROESSE</c></summary>
    public string SpalteBezugsgroesse { get; set; } = "Bezugsgröße";

    /// <summary><c>ZPG_SP_JAHRESBEDARF</c></summary>
    public string SpalteJahresbedarf { get; set; } = "MWh/a";

    /// <summary><c>ZPG_SUMME_ZONEN</c></summary>
    public string SummeZonen { get; set; } = "Summe {0} Zonen";

    /// <summary><c>ZPG_BTN_ZONE_NEU</c></summary>
    public string KnopfZoneNeu { get; set; } = "Zone hinzufügen…";

    /// <summary><c>ZPG_BTN_ZONE_DUPLIZIEREN</c></summary>
    public string KnopfZoneDuplizieren { get; set; } = "Duplizieren";

    /// <summary><c>ZPG_BTN_ZONE_ENTFERNEN</c></summary>
    public string KnopfZoneEntfernen { get; set; } = "Entfernen";

    /// <summary><c>ZPG_ZONE_NAME_VORGABE</c></summary>
    public string ZoneNameVorgabe { get; set; } = "Zone {0}";

    /// <summary><c>ZPG_ZONE_KOPIE</c></summary>
    public string ZoneKopie { get; set; } = "{0} (Kopie)";

    // ------------------------------------------------------------ Felder der Stufe Einfach

    /// <summary><c>ZPG_LBL_ZONENNAME</c></summary>
    public string LabelZonenname { get; set; } = "Zonenname";

    /// <summary><c>ZPG_LBL_NUTZUNGSART</c></summary>
    public string LabelNutzungsart { get; set; } = "Nutzungsart";

    /// <summary><c>ZPG_LBL_BEZUGSMENGE</c></summary>
    public string LabelBezugsmenge { get; set; } = "Bezugsgröße";

    /// <summary><c>ZPG_LBL_NIVEAU</c></summary>
    public string LabelNiveau { get; set; } = "Bedarfsniveau";

    /// <summary><c>ZPG_NIVEAU_NIEDRIG</c></summary>
    public string NiveauNiedrig { get; set; } = "niedrig";

    /// <summary><c>ZPG_NIVEAU_MITTEL</c></summary>
    public string NiveauMittel { get; set; } = "mittel";

    /// <summary><c>ZPG_NIVEAU_HOCH</c></summary>
    public string NiveauHoch { get; set; } = "hoch";

    /// <summary><c>ZPG_HINW_BEZUGSMENGE_EINHEIT</c></summary>
    public string HinweisBezugsmengeEinheit { get; set; } = "Einheit aus dem Katalog der Nutzungsart";

    /// <summary><c>ZPG_HINW_NIVEAU_VORGABE</c></summary>
    public string HinweisNiveauVorgabe { get; set; } = "Vorgabe · {0} = {1} kWh/({2}·d) aus dem Katalog";

    /// <summary><c>ZPG_HINW_WEITERE_VORGABE</c></summary>
    public string HinweisWeitereVorgabe { get; set; } = "Weitere Angaben stehen auf den Vorgaben des Katalogs.";

    /// <summary><c>ZPG_HINW_WEITERE_ERWEITERT</c></summary>
    public string HinweisWeitereErweitert { get; set; } = "Weitere Angaben stehen in der Stufe Erweitert.";

    /// <summary><c>ZPG_HINW_NUTZUNGSART_KATALOG</c></summary>
    public string HinweisNutzungsartKatalog { get; set; } = "Katalog · {0}";

    /// <summary><c>ZPG_KAT_KEINE_AUSWAHL</c></summary>
    public string KatalogKeineAuswahl { get; set; } = "– bitte wählen –";

    // ------------------------------------------------------------ Katalogauswahl

    /// <summary><c>ZPG_SP_BEZUGSART</c></summary>
    public string SpalteBezugsart { get; set; } = "Bezugsart";

    /// <summary><c>ZPG_SP_HERKUNFT</c></summary>
    public string SpalteHerkunft { get; set; } = "Herkunft";

    /// <summary><c>ZPG_SP_STATUS</c></summary>
    public string SpalteStatus { get; set; } = "Status";

    /// <summary><c>ZPG_SP_KATALOGVERSION</c></summary>
    public string SpalteKatalogversion { get; set; } = "Katalogversion";

    // ------------------------------------------------------------ Vorschau

    /// <summary><c>ZPG_GRP_VORSCHAU</c></summary>
    public string GruppeVorschau { get; set; } = "Vorschau";

    /// <summary><c>ZPG_VORSCHAU_LIVE</c></summary>
    public string VorschauLive { get; set; } = "live · deterministischer Pfad";

    /// <summary><c>ZPG_LBL_ANZEIGEN_FUER</c></summary>
    public string LabelAnzeigenFuer { get; set; } = "Anzeigen für";

    /// <summary><c>ZPG_ANSICHT_SUMME</c></summary>
    public string AnsichtSumme { get; set; } = "Summe aller Zonen";

    /// <summary><c>ZPG_REITER_TAGESGANG</c></summary>
    public string ReiterTagesgang { get; set; } = "Tagesgang";

    /// <summary><c>ZPG_REITER_WOCHENPROFIL</c></summary>
    public string ReiterWochenprofil { get; set; } = "Wochenprofil";

    /// <summary><c>ZPG_REITER_JAHRESGANG</c></summary>
    public string ReiterJahresgang { get; set; } = "Jahresgang";

    /// <summary><c>ZPG_REITER_DAUERLINIE</c></summary>
    public string ReiterDauerlinie { get; set; } = "Dauerlinie";

    /// <summary><c>ZPG_REITER_KENNZAHLEN</c></summary>
    public string ReiterKennzahlen { get; set; } = "Kennzahlen";

    /// <summary><c>ZPG_TAGTYP_WERKTAG</c></summary>
    public string TagtypWerktag { get; set; } = "Werktag";

    /// <summary><c>ZPG_TAGTYP_SAMSTAG</c></summary>
    public string TagtypSamstag { get; set; } = "Samstag";

    /// <summary><c>ZPG_TAGTYP_SONNTAG</c></summary>
    public string TagtypSonntag { get; set; } = "Sonn-/Feiertag";

    /// <summary><c>ZPG_REIHE_ZAPFUNG</c></summary>
    public string ReiheZapfung { get; set; } = "Zapfung";

    /// <summary><c>ZPG_REIHE_ZIRKULATION</c></summary>
    public string ReiheZirkulation { get; set; } = "Zirkulation";

    // ------------------------------------------------------------ Kennzahlen

    /// <summary><c>ZPG_KZ_BILANZ</c></summary>
    public string KennzahlBilanz { get; set; } = "Bilanz · deterministisch, live";

    /// <summary><c>ZPG_KZ_STOCHASTIK</c></summary>
    public string KennzahlStochastik { get; set; } = "Stochastik · noch nicht gerechnet";

    /// <summary><c>ZPG_KZ_STOCHASTIK_ERKLAERUNG</c></summary>
    public string KennzahlStochastikErklaerung { get; set; } = "„Stochastisch rechnen“ in der Fußleiste zieht das Ensemble des Bedarfstags: P50 … P99 und die Gleichzeitigkeit stehen in Karte (b) der Auslegung. Bei Rechenweg „stochastisch“ zieht es auch die Jahresreihe; ihre Konsistenzprobe zeigt die Stufe Experte nach dem Lauf.";

    /// <summary><c>ZPG_SP_KENNZAHL</c></summary>
    public string SpalteKennzahl { get; set; } = "Kennzahl";

    /// <summary><c>ZPG_SP_WERT</c></summary>
    public string SpalteWert { get; set; } = "Wert";

    /// <summary><c>ZPG_SP_VERMERK</c></summary>
    public string SpalteVermerk { get; set; } = "Vermerk";

    /// <summary><c>ZPG_KZ_ZAPFUNG</c></summary>
    public string KennzahlZapfung { get; set; } = "Jahresbedarf Zapfung";

    /// <summary><c>ZPG_KZ_ZIRKULATION</c></summary>
    public string KennzahlZirkulation { get; set; } = "Zirkulation";

    /// <summary><c>ZPG_KZ_ZIRKULATION_VERMERK</c></summary>
    public string KennzahlZirkulationVermerk { get; set; } = "eigene Teilreihe";

    /// <summary><c>ZPG_KZ_GESAMT</c></summary>
    public string KennzahlGesamt { get; set; } = "Brauchwasser gesamt";

    /// <summary><c>ZPG_KZ_ZIRKULATIONSANTEIL</c></summary>
    public string KennzahlZirkulationsanteil { get; set; } = "Zirkulationsanteil";

    /// <summary><c>ZPG_KZ_TAGESMITTEL</c></summary>
    public string KennzahlTagesmittel { get; set; } = "Tagesmittel Zapfung";

    /// <summary><c>ZPG_KZ_LITER_JE_TAG</c></summary>
    public string KennzahlLiterJeTag { get; set; } = "≈ {0} l/d";

    /// <summary><c>ZPG_KZ_SPEZIFISCH</c></summary>
    public string KennzahlSpezifisch { get; set; } = "Spezifisch {0}";

    /// <summary><c>ZPG_KZ_VOLLLAST</c></summary>
    public string KennzahlVolllast { get; set; } = "Volllaststunden";

    /// <summary><c>ZPG_KZ_VOLLLAST_VERMERK</c></summary>
    public string KennzahlVolllastVermerk { get; set; } = "Brauchwasser gesamt ÷ größter Stundenwert";

    /// <summary><c>ZPG_KZ_GROESSTER_STUNDENWERT</c></summary>
    public string KennzahlGroessterStundenwert { get; set; } = "Größter Stundenwert (Zapfung + Zirkulation)";

    /// <summary><c>ZPG_KZ_VERMERK_STUNDENWERT</c></summary>
    public string KennzahlVermerkStundenwert { get; set; } = "Bilanzwert, keine Auslegungsgröße";

    /// <summary><c>ZPG_KZ_STUNDEN_UEBER_SCHWELLE</c></summary>
    public string KennzahlStundenUeberSchwelle { get; set; } = "Stunden über Schwelle {0} kW";

    /// <summary><c>ZPG_KZ_GLEICHZEITIGKEIT</c></summary>
    public string KennzahlGleichzeitigkeit { get; set; } = "Gleichzeitigkeit";

    /// <summary><c>ZPG_KZ_GLEICHZEITIGKEIT_VERMERK</c></summary>
    public string KennzahlGleichzeitigkeitVermerk { get; set; } = "entsteht erst in der Superposition; kein Eingabefaktor";

    /// <summary><c>ZPG_KZ_ABGELEHNT</c></summary>
    public string KennzahlAbgelehnt { get; set; } = "trägt 0";

    // ------------------------------------------------------------ Stochastik (Stufe Experte, Z3)

    /// <summary><c>ZPG_GRP_STOCHASTIK</c></summary>
    public string GruppeStochastik { get; set; } = "Stochastik · Jahresreihe";

    /// <summary><c>ZPG_LBL_RECHENWEG_JAHRESREIHE</c></summary>
    public string LabelRechenwegJahresreihe { get; set; } = "Rechenweg der Jahresreihe";

    /// <summary><c>ZPG_OPT_DETERMINISTISCH</c></summary>
    public string OptionDeterministisch { get; set; } = "deterministisch";

    /// <summary><c>ZPG_OPT_STOCHASTISCH</c></summary>
    public string OptionStochastisch { get; set; } = "stochastisch";

    /// <summary><c>ZPG_HINW_RECHENWEG_JAHRESREIHE</c></summary>
    public string HinweisRechenwegJahresreihe { get; set; } = "Wählt nur, welche Reihe in die Bilanz geht: bei „stochastisch“ das gezogene Jahr zum Seed, auf die Jahresmenge gebracht. Das Perzentil der Auslegung ist davon unabhängig.";

    /// <summary><c>ZPG_LBL_SEED</c></summary>
    public string LabelSeed { get; set; } = "Zufallssaat (Seed)";

    /// <summary><c>ZPG_HINW_SEED</c></summary>
    public string HinweisSeed { get; set; } = "Ganze Zahl ab 0 · leer = Vorgabe {0}; derselbe Seed zieht auf jeder Plattform dieselbe Reihe — für die Jahresreihe und das Ensemble der Auslegung.";

    /// <summary><c>ZPG_LBL_REALISIERUNGEN</c></summary>
    public string LabelRealisierungen { get; set; } = "Realisierungen";

    /// <summary><c>ZPG_EINHEIT_JAHRE</c></summary>
    public string EinheitJahre { get; set; } = "Jahre";

    /// <summary><c>ZPG_HINW_REALISIERUNGEN</c></summary>
    public string HinweisRealisierungen { get; set; } = "Ganze Zahl von {0} bis {1} · leer = Vorgabe {2}; die gezogenen Jahre prüfen nur die Konsistenz.";

    /// <summary><c>ZPG_HINW_KONSISTENZ_ORT</c></summary>
    public string HinweisKonsistenzOrt { get; set; } = "Die Konsistenzprobe steht nach dem Lauf im Reiter Kennzahlen.";

    /// <summary><c>ZPG_HINW_VORSCHAU_DETERMINISTISCH</c></summary>
    public string HinweisVorschauDeterministisch { get; set; } = "Die Vorschau zeigt den deterministischen Pfad; die Jahresreihe entsteht erst im Lauf stochastisch — mit derselben Jahresmenge.";

    /// <summary><c>ZPG_KZ_STOCHASTIK_LAEUFT</c></summary>
    public string KennzahlStochastikLaeuft { get; set; } = "Stochastik · rechnet …";

    /// <summary><c>ZPG_STATUS_JAHRESREIHE_LAEUFT</c></summary>
    public string StatusJahresreiheLaeuft { get; set; } = "Jahresreihe rechnet … · Seed {0} · {1} Jahre";

    /// <summary><c>ZPG_HINW_JAHRESREIHE_ABGEBROCHEN</c></summary>
    public string HinweisJahresreiheAbgebrochen { get; set; } = "Die Jahresreihe ist abgebrochen — „Stochastisch rechnen“ zieht sie erneut.";

    /// <summary><c>ZPG_HINW_JAHRESREIHE_DETERMINISTISCH</c></summary>
    public string HinweisJahresreiheDeterministisch { get; set; } = "Bei Rechenweg „deterministisch“ zieht „Stochastisch rechnen“ keine Jahresreihe.";

    /// <summary><c>ZPG_KZ_STOCHASTIK_GERECHNET</c></summary>
    public string KennzahlStochastikGerechnet { get; set; } = "Stochastik · Jahresreihe zum Seed {0}, {1} Jahre gezogen";

    /// <summary><c>ZPG_KZ_STOCHASTIK_JAHRESREIHE</c></summary>
    public string KennzahlStochastikJahresreihe { get; set; } = "Die Bilanzreihe ist das gezogene Jahr zum Seed, auf die Jahresmenge gebracht. Die Konsistenzprobe zeigt die Stufe Experte.";

    /// <summary><c>ZPG_KZ_KONSISTENZ</c></summary>
    public string KennzahlKonsistenz { get; set; } = "Konsistenzprobe Energie · {0}";

    /// <summary><c>ZPG_KZ_KONSISTENZ_VERMERK</c></summary>
    public string KennzahlKonsistenzVermerk { get; set; } = "Mittel der {0} Jahre {1} kWh/a gegen {2} kWh/a deterministisch · s_R {3} kWh/a · Toleranz ±{4} kWh/a";

    /// <summary><c>ZPG_KZ_KONSISTENZ_ERFUELLT</c></summary>
    public string KonsistenzErfuellt { get; set; } = "innerhalb der Toleranz";

    /// <summary><c>ZPG_KZ_KONSISTENZ_ABWEICHEND</c></summary>
    public string KonsistenzAbweichend { get; set; } = "außerhalb der Toleranz — das Jahr zum Seed ist auf die Jahresmenge gebracht";

    /// <summary><c>ZPG_MSG_FEHLEINGABE</c></summary>
    public string MeldungFehleingabe { get; set; } = "Bitte die markierten Felder berichtigen: {0}.";

    // ------------------------------------------------------------ Fussleiste

    /// <summary><c>ZPG_BTN_STOCHASTIK</c></summary>
    public string KnopfStochastik { get; set; } = "Stochastisch rechnen";

    /// <summary><c>ZPG_BTN_STOCHASTIK_TITEL</c></summary>
    public string KnopfStochastikTitel { get; set; } = "Zieht nebenläufig das Ensemble des Bedarfstags für Perzentil und Gleichzeitigkeit (in der Auslegung) und bei Rechenweg „stochastisch“ die Jahre der Konsistenzprobe; beides lässt sich abbrechen.";

    /// <summary><c>ZPG_BTN_AUSLEGUNG</c></summary>
    public string KnopfAuslegung { get; set; } = "Auslegung…";

    /// <summary><c>ZPG_STATUS_AUSLEGUNG</c></summary>
    public string StatusAuslegung { get; set; } = "Auslegung übernommen: {0}";

    /// <summary><c>ZPG_AUSLEGUNG_OHNE_PUNKT</c></summary>
    public string AuslegungOhnePunkt { get; set; } = "Eingaben ohne Punkt";

    /// <summary><c>ZPG_PUNKT_UEBERHOLT</c></summary>
    public string PunktUeberholt { get; set; } = "Die Zonen haben sich geändert — der Auslegungspunkt ist überholt und wird beim Speichern verworfen. „Auslegung…“ öffnen und neu übernehmen.";

    /// <summary><c>ZPG_STATUS_VORSCHAU</c></summary>
    public string StatusVorschau { get; set; } = "Vorschau aktuell · deterministisch · Stochastik noch nicht gerechnet";

    /// <summary><c>ZPG_STATUS_OHNE_VORSCHAU</c></summary>
    public string StatusOhneVorschau { get; set; } = "Keine Vorschau — {0}";

    // ------------------------------------------------------------ Stufen Erweitert und Experte (Z4)

    /// <summary><c>ZPG_HINW_WEITERE_EXPERTE</c></summary>
    public string HinweisWeitereExperte { get; set; } = "Fachwerte, Tagesgänge und Zufallssaat stehen in der Stufe Experte.";

    /// <summary><c>ZPG_GRUND_DAUERLINIE</c></summary>
    public string GrundDauerlinie { get; set; } = "Die Dauerlinie zeigt die Stufe Erweitert.";

    /// <summary><c>ZPG_GRUND_FOLGT</c></summary>
    public string GrundFolgt { get; set; } = "Folgt mit der nächsten Fassung des Dialogs.";

    /// <summary><c>ZPG_VORGABE_EINTRAG</c></summary>
    public string VorgabeEintrag { get; set; } = "Vorgabe · {0}";

    /// <summary><c>ZPG_AUS_AUTO</c></summary>
    public string OptionAuto { get; set; } = "auto";

    /// <summary><c>ZPG_AUS_MANUELL</c></summary>
    public string OptionManuell { get; set; } = "manuell";

    /// <summary><c>ZPG_OPT_JA</c></summary>
    public string OptionJa { get; set; } = "ja";

    /// <summary><c>ZPG_OPT_NEIN</c></summary>
    public string OptionNein { get; set; } = "nein";

    /// <summary><c>ZPG_FELD_ZEILE</c></summary>
    public string FeldZeile { get; set; } = "{0}, Zeile {1}";

    // ------------------------------------------------------------ Zonenliste ab Erweitert

    /// <summary><c>ZPG_SP_TOPOLOGIE</c></summary>
    public string SpalteTopologie { get; set; } = "Topologie";

    /// <summary><c>ZPG_SP_ANTEIL</c></summary>
    public string SpalteAnteil { get; set; } = "Anteil";

    /// <summary><c>ZPG_SP_RECHENWEG</c></summary>
    public string SpalteRechenweg { get; set; } = "Rechenweg";

    /// <summary><c>ZPG_RECHENWEG_KATALOG</c></summary>
    public string RechenwegKatalog { get; set; } = "Katalog";

    /// <summary><c>ZPG_RECHENWEG_MANUELL</c></summary>
    public string RechenwegManuell { get; set; } = "manuell";

    /// <summary><c>ZPG_RECHENWEG_MESSWERT</c></summary>
    public string RechenwegMesswert { get; set; } = "Messwert";

    /// <summary><c>ZPG_AUS_TOPOLOGIE_SPEICHER</c></summary>
    public string TopologieSpeicher { get; set; } = "Speicher";

    /// <summary><c>ZPG_AUS_TOPOLOGIE_FRISCHWASSERSTATION</c></summary>
    public string TopologieFrischwasserstation { get; set; } = "Frischwasserstation";

    /// <summary><c>ZPG_AUS_TOPOLOGIE_DURCHFLUSS</c></summary>
    public string TopologieDurchfluss { get; set; } = "Durchfluss";

    /// <summary><c>ZPG_AUS_TOPOLOGIE_WOHNUNGSSTATION</c></summary>
    public string TopologieWohnungsstation { get; set; } = "Wohnungsstation";

    // ------------------------------------------------------------ Nutzungsart aufgeklappt

    /// <summary><c>ZPG_KAT_AUFGEKLAPPT</c></summary>
    public string KatalogAufgeklappt { get; set; } = "Katalog der Nutzungsarten";

    /// <summary><c>ZPG_SP_KALENDER</c></summary>
    public string SpalteKalender { get; set; } = "Kalender";

    /// <summary><c>ZPG_HINW_BEZUGSMENGE_WOHNUNGSTABELLE</c></summary>
    public string HinweisBezugsmengeWohnungstabelle { get; set; } = "Wirksam aus der Wohnungstabelle: {0} {1} — OK übernimmt sie als Bezugsgröße.";

    // ------------------------------------------------------------ Belegung und Anlage (Erweitert)

    /// <summary><c>ZPG_GRP_BELEGUNG</c></summary>
    public string GruppeBelegung { get; set; } = "Belegung und Anlage";

    /// <summary><c>ZPG_LBL_WOHNUNGSTABELLE</c></summary>
    public string LabelWohnungstabelle { get; set; } = "Wohnungstabelle";

    /// <summary><c>ZPG_LBL_ANZAHL</c></summary>
    public string LabelAnzahl { get; set; } = "Anzahl";

    /// <summary><c>ZPG_LBL_RAUMZAHL</c></summary>
    public string LabelRaumzahl { get; set; } = "Raumzahl";

    /// <summary><c>ZPG_LBL_PERSONEN</c></summary>
    public string LabelPersonen { get; set; } = "Personen je Wohnung";

    /// <summary><c>ZPG_LBL_AUSSTATTUNG</c></summary>
    public string LabelAusstattung { get; set; } = "Ausstattungsklasse";

    /// <summary><c>ZPG_SP_AKTION</c></summary>
    public string SpalteAktion { get; set; } = "Aktion";

    /// <summary><c>ZPG_BTN_WOHNUNG_NEU</c></summary>
    public string KnopfWohnungNeu { get; set; } = "Wohnungstyp hinzufügen";

    /// <summary><c>ZPG_BTN_WOHNUNG_ENTFERNEN</c></summary>
    public string KnopfWohnungEntfernen { get; set; } = "Entfernen";

    /// <summary><c>ZPG_AUSSTATTUNG_VORGABE</c></summary>
    public string AusstattungVorgabe { get; set; } = "Vorgabeklasse";

    /// <summary><c>ZPG_HINW_WOHNUNGSTABELLE</c></summary>
    public string HinweisWohnungstabelle { get; set; } = "Personen leer = Belegung nach Raumzahl aus dem Katalog, sonst Personen je Wohneinheit; Ausstattung leer = Vorgabeklasse. Mit Tabelle gilt ihre Menge als Bezugsgröße.";

    /// <summary><c>ZPG_HINW_WOHNUNGSTABELLE_LEER</c></summary>
    public string HinweisWohnungstabelleLeer { get; set; } = "Keine Wohnungstabelle — es gilt die Bezugsgröße.";

    /// <summary><c>ZPG_LBL_PERSONEN_JE_WE</c></summary>
    public string LabelPersonenJeWe { get; set; } = "Personen je Wohneinheit";

    /// <summary><c>ZPG_EINHEIT_PERSONEN_JE_WE</c></summary>
    public string EinheitPersonenJeWe { get; set; } = "P/WE";

    /// <summary><c>ZPG_HINW_PERSONEN_JE_WE</c></summary>
    public string HinweisPersonenJeWe { get; set; } = "Vorgabe · Belegung nach der Wohnungsgröße aus dem Katalog (Verfahren nach DIN 4708-2).";

    /// <summary><c>ZPG_LBL_WOHNFLAECHE_JE_WE</c></summary>
    public string LabelWohnflaecheJeWe { get; set; } = "Wohnfläche je Wohneinheit";

    /// <summary><c>ZPG_HINW_WOHNFLAECHE_JE_WE</c></summary>
    public string HinweisWohnflaecheJeWe { get; set; } = "Vorgabe · {0} m² (Annahme); Flächenformel nach DIN V 18599-10.";

    /// <summary><c>ZPG_LBL_TOPOLOGIE</c></summary>
    public string LabelTopologie { get; set; } = "Anlagentopologie";

    /// <summary><c>ZPG_HINW_TOPOLOGIE</c></summary>
    public string HinweisTopologie { get; set; } = "Vorgabe · Speicher; wirkt nur auf die Auslegung.";

    /// <summary><c>ZPG_LBL_ZIRKULATION_VORHANDEN</c></summary>
    public string LabelZirkulationVorhanden { get; set; } = "Zirkulation vorhanden";

    /// <summary><c>ZPG_HINW_ZIRKULATION_VORHANDEN</c></summary>
    public string HinweisZirkulationVorhanden { get; set; } = "Vorgabe · ja; „nein“ nimmt die Zone aus dem Zirkulationsanteil.";

    /// <summary><c>ZPG_LBL_KALENDER</c></summary>
    public string LabelKalender { get; set; } = "Kalender";

    /// <summary><c>ZPG_KALENDER_NUTZUNGSART</c></summary>
    public string KalenderNutzungsart { get; set; } = "Kalender der Nutzungsart · {0}";

    /// <summary><c>ZPG_KALENDER_GEBAEUDE</c></summary>
    public string KalenderGebaeude { get; set; } = "Ferien des Gebäudes {0}";

    /// <summary><c>ZPG_HINW_KALENDER</c></summary>
    public string HinweisKalender { get; set; } = "Vorgabe · Kalender der Nutzungsart; ein Gebäude belegt Ferien und Fläche nur vor — eigene Ferien der Zone gehen vor.";

    /// <summary><c>ZPG_LBL_FERIEN</c></summary>
    public string LabelFerien { get; set; } = "Ferienzeitraum {0}";

    /// <summary><c>ZPG_LBL_BEGINN_TAG</c></summary>
    public string LabelBeginnTag { get; set; } = "Beginn Tag";

    /// <summary><c>ZPG_LBL_BEGINN_MONAT</c></summary>
    public string LabelBeginnMonat { get; set; } = "Beginn Monat";

    /// <summary><c>ZPG_LBL_ENDE_TAG</c></summary>
    public string LabelEndeTag { get; set; } = "Ende Tag";

    /// <summary><c>ZPG_LBL_ENDE_MONAT</c></summary>
    public string LabelEndeMonat { get; set; } = "Ende Monat";

    /// <summary><c>ZPG_HINW_FERIEN</c></summary>
    public string HinweisFerien { get; set; } = "Leer = keine Ferien. Ein Zeitraum über den Jahreswechsel beginnt im Dezember; ohne Beginn läuft er vom 1. Januar an. Gerechnet wird im Jahr ohne Schalttag.";

    /// <summary><c>ZPG_LBL_BUNDESLAND</c></summary>
    public string LabelBundesland { get; set; } = "Bundesland";

    /// <summary><c>ZPG_BUNDESLAND_KLIMAREGION</c></summary>
    public string BundeslandKlimaregion { get; set; } = "wie Klimaregion";

    /// <summary><c>ZPG_GRUND_BUNDESLAND</c></summary>
    public string GrundBundesland { get; set; } = "Wirkt erst mit einer Kalendertabelle je Bundesland; bis dahin gelten Wochenende und Feiertage der Klimaregion.";

    /// <summary><c>ZPG_LBL_JAHRESMESSWERT</c></summary>
    public string LabelJahresmesswert { get; set; } = "Jahresmesswert";

    /// <summary><c>ZPG_LBL_MESSWERT_EINHEIT</c></summary>
    public string LabelMesswertEinheit { get; set; } = "Einheit des Messwerts";

    /// <summary><c>ZPG_MESSWERT_KWH</c></summary>
    public string MesswertKwh { get; set; } = "kWh/a";

    /// <summary><c>ZPG_MESSWERT_M3</c></summary>
    public string MesswertM3 { get; set; } = "m³/a";

    /// <summary><c>ZPG_LBL_MESSWERT_GRENZE</c></summary>
    public string LabelMesswertGrenze { get; set; } = "Bilanzgrenze des Messwerts";

    /// <summary><c>ZPG_GRENZE_ZAPFSTELLE</c></summary>
    public string GrenzeZapfstelle { get; set; } = "an der Zapfstelle";

    /// <summary><c>ZPG_GRENZE_VERTEILUNG</c></summary>
    public string GrenzeVerteilung { get; set; } = "mit Verteil- und Zirkulationsverlust";

    /// <summary><c>ZPG_GRENZE_SPEICHER</c></summary>
    public string GrenzeSpeicher { get; set; } = "zusätzlich mit Speicherverlust";

    /// <summary><c>ZPG_LBL_SPEICHERVERLUST</c></summary>
    public string LabelSpeicherverlust { get; set; } = "Speicherverlust";

    /// <summary><c>ZPG_LBL_MESSWERT_QUELLE</c></summary>
    public string LabelMesswertQuelle { get; set; } = "Quelle des Messwerts";

    /// <summary><c>ZPG_LBL_MESSWERT_ZEITRAUM</c></summary>
    public string LabelMesswertZeitraum { get; set; } = "Zeitraum des Messwerts";

    /// <summary><c>ZPG_HINW_JAHRESMESSWERT</c></summary>
    public string HinweisJahresmesswert { get; set; } = "Leer = kein Messwert. Ein Messwert skaliert die Zone mit einem ausgewiesenen Faktor; ein Volumen gilt an der Zapfstelle und wird über Zapf- und Kaltwassertemperatur umgerechnet.";

    // ------------------------------------------------------------ Tagesbedarf, Ladeleistung, Zirkulation (Erweitert)

    /// <summary><c>ZPG_GRP_SCHAETZHILFEN</c></summary>
    public string GruppeSchaetzhilfen { get; set; } = "Tagesbedarf, Ladeleistung, Zirkulation";

    /// <summary><c>ZPG_HINW_SCHAETZHILFEN</c></summary>
    public string HinweisSchaetzhilfen { get; set; } = "Muster auto/manuell: Vorschlag, manueller Wert und der angesetzte Wert, der weiterverwendet wird. Der angesetzte Wert folgt aus dem Umschalter.";

    /// <summary><c>ZPG_LBL_TAGESBEDARF</c></summary>
    public string LabelTagesbedarf { get; set; } = "Tagesbedarf";

    /// <summary><c>ZPG_LBL_TAGESBEDARF_MANUELL</c></summary>
    public string LabelTagesbedarfManuell { get; set; } = "Tagesbedarf manuell";

    /// <summary><c>ZPG_LBL_VORSCHLAG</c></summary>
    public string LabelVorschlag { get; set; } = "Vorschlag: {0} {1}";

    /// <summary><c>ZPG_BTN_VORSCHLAG</c></summary>
    public string KnopfVorschlag { get; set; } = "Als manuellen Wert übernehmen";

    /// <summary><c>ZPG_HINW_VORSCHLAG_OHNE</c></summary>
    public string HinweisVorschlagOhne { get; set; } = "Das Verfahren liefert keinen Vorschlag — der Rechenweg nennt den Grund.";

    /// <summary><c>ZPG_HINW_OHNE_VORSCHLAG</c></summary>
    public string HinweisOhneVorschlag { get; set; } = "Einen Vorschlag gibt es erst mit einer gerechneten Vorschau.";

    /// <summary><c>ZPG_HINW_MANUELL</c></summary>
    public string HinweisManuell { get; set; } = "Wirkt nur bei „manuell“.";

    /// <summary><c>ZPG_LBL_ANGESETZT</c></summary>
    public string LabelAngesetzt { get; set; } = "Angesetzt: {0} {1} ({2})";

    /// <summary><c>ZPG_ANGESETZT_KALIBRIERT</c></summary>
    public string AngesetztKalibriert { get; set; } = "kalibriert auf den Jahresmesswert";

    /// <summary><c>ZPG_LBL_RECHENWEG</c></summary>
    public string LabelRechenweg { get; set; } = "Rechenweg: {0}";

    /// <summary><c>ZPG_LBL_LADELEISTUNG</c></summary>
    public string LabelLadeleistung { get; set; } = "Ladeleistung (Gebäude)";

    /// <summary><c>ZPG_LBL_LADELEISTUNG_MANUELL</c></summary>
    public string LabelLadeleistungManuell { get; set; } = "Ladeleistung manuell";

    /// <summary><c>ZPG_LBL_LADEFENSTER</c></summary>
    public string LabelLadefenster { get; set; } = "Ladezeitfenster";

    /// <summary><c>ZPG_LBL_LADEFENSTER_BEGINN</c></summary>
    public string LabelLadefensterBeginn { get; set; } = "Beginn des Ladefensters";

    /// <summary><c>ZPG_HINW_LADEFENSTER</c></summary>
    public string HinweisLadefenster { get; set; } = "Vorgabe · {0} h/d ab {1} Uhr; effektive Ladezeit je Tag, Sperr- und Heizzeiten abgezogen.";

    /// <summary><c>ZPG_HINW_LADELEISTUNG</c></summary>
    public string HinweisLadeleistung { get; set; } = "Wirkt nur auf die Auslegung, nie auf die Bilanzreihe. Den Vorschlag rechnet die Auslegung: größter Tagesbedarf samt Zirkulation ÷ Ladezeitfenster.";

    /// <summary><c>ZPG_LADE_ANGESETZT_AUTO</c></summary>
    public string LadeAngesetztAuto { get; set; } = "Angesetzt: Vorschlag der Auslegung (auto)";

    /// <summary><c>ZPG_LADE_ANGESETZT_MANUELL</c></summary>
    public string LadeAngesetztManuell { get; set; } = "Angesetzt: {0} kW (manuell)";

    /// <summary><c>ZPG_LBL_ZIRKULATION</c></summary>
    public string LabelZirkulation { get; set; } = "Zirkulation (Gebäude)";

    /// <summary><c>ZPG_LBL_ZIRK_MANUELL</c></summary>
    public string LabelZirkManuell { get; set; } = "Zirkulation manuell";

    /// <summary><c>ZPG_LBL_ZIRK_METHODE</c></summary>
    public string LabelZirkMethode { get; set; } = "Methode der Zirkulation";

    /// <summary><c>ZPG_ZIRK_METHODE_LAENGE</c></summary>
    public string ZirkMethodeLaenge { get; set; } = "Leitungslänge × spezifischer Verlust";

    /// <summary><c>ZPG_ZIRK_METHODE_ANTEIL</c></summary>
    public string ZirkMethodeAnteil { get; set; } = "Anteil am Tagesbedarf";

    /// <summary><c>ZPG_ZIRK_METHODE_FLAECHE</c></summary>
    public string ZirkMethodeFlaeche { get; set; } = "Flächenkennwert";

    /// <summary><c>ZPG_LBL_ZIRK_LAENGE</c></summary>
    public string LabelZirkLaenge { get; set; } = "Leitungslänge";

    /// <summary><c>ZPG_LBL_ZIRK_VERLUST</c></summary>
    public string LabelZirkVerlust { get; set; } = "Spezifischer Verlust";

    /// <summary><c>ZPG_LBL_ZIRK_ANTEIL</c></summary>
    public string LabelZirkAnteil { get; set; } = "Anteil am Tagesbedarf";

    /// <summary><c>ZPG_LBL_ZIRK_LAGE</c></summary>
    public string LabelZirkLage { get; set; } = "Lage der Leitung";

    /// <summary><c>ZPG_LAGE_INNEN</c></summary>
    public string LageInnen { get; set; } = "innerhalb der thermischen Hülle";

    /// <summary><c>ZPG_LAGE_AUSSEN</c></summary>
    public string LageAussen { get; set; } = "außerhalb der thermischen Hülle";

    /// <summary><c>ZPG_HINW_ZIRK_VORGABEN</c></summary>
    public string HinweisZirkVorgaben { get; set; } = "Leer = Vorgabe · {0} W/m bzw. Anteil {1}.";

    /// <summary><c>ZPG_HINW_ZIRKULATION</c></summary>
    public string HinweisZirkulation { get; set; } = "Eigener Kanal neben dem Zapfprofil, nie in das Profil eingerechnet; Kennwert, Fläche und Laufzeit zeigt die Stufe Experte.";

    /// <summary><c>ZPG_LBL_LEITUNGSINHALT</c></summary>
    public string LabelLeitungsinhalt { get; set; } = "Leitungsinhalt";

    /// <summary><c>ZPG_HINW_LEITUNGSINHALT</c></summary>
    public string HinweisLeitungsinhalt { get; set; } = "Wasserinhalt der Leitungen zwischen Erwärmer und entferntester Zapfstelle; er entscheidet mit über die Großanlage nach DVGW W 551.";

    // ------------------------------------------------------------ Fachwerte (Experte)

    /// <summary><c>ZPG_GRP_FACHWERTE</c></summary>
    public string GruppeFachwerte { get; set; } = "Fachwerte · {0}";

    /// <summary><c>ZPG_LBL_BEDARF_SPEZ</c></summary>
    public string LabelBedarfSpez { get; set; } = "Spezifischer Bedarf";

    /// <summary><c>ZPG_HINW_BEDARF_SPEZ</c></summary>
    public string HinweisBedarfSpez { get; set; } = "Leer = Katalogwert des Niveaus ({0} kWh/({1}·d)).";

    /// <summary><c>ZPG_LBL_ZAPFTEMPERATUR</c></summary>
    public string LabelZapftemperatur { get; set; } = "Zapftemperatur";

    /// <summary><c>ZPG_HINW_ZAPFTEMPERATUR</c></summary>
    public string HinweisZapftemperatur { get; set; } = "Leer = Bezugstemperatur des Katalogs ({0} °C); eine Großanlage erwartet die Mindesttemperatur nach DVGW W 551.";

    /// <summary><c>ZPG_LBL_KALTWASSER_MITTEL</c></summary>
    public string LabelKaltwasserMittel { get; set; } = "Kaltwasser Jahresmittel";

    /// <summary><c>ZPG_LBL_KALTWASSER_AMPLITUDE</c></summary>
    public string LabelKaltwasserAmplitude { get; set; } = "Kaltwasser Amplitude";

    /// <summary><c>ZPG_HINW_KALTWASSER</c></summary>
    public string HinweisKaltwasser { get; set; } = "Leer = Konvention {0} °C ± {1} K, sinusförmig über das Jahr (nur Bilanz).";

    /// <summary><c>ZPG_LBL_AUSLASTUNGSGANG</c></summary>
    public string LabelAuslastungsgang { get; set; } = "Auslastungsgang";

    /// <summary><c>ZPG_HINW_AUSLASTUNGSGANG</c></summary>
    public string HinweisAuslastungsgang { get; set; } = "Zwölf Monatsfaktoren; leer = Faktor des Katalogs (im Feld grau).";

    /// <summary><c>ZPG_LBL_TAGESGANGSATZ</c></summary>
    public string LabelTagesgangsatz { get; set; } = "Tagesgangsatz";

    /// <summary><c>ZPG_TAGESGANGSATZ_VORGABE</c></summary>
    public string TagesgangsatzVorgabe { get; set; } = "Satz der Nutzungsart";

    /// <summary><c>ZPG_HINW_TAGESGANGSATZ</c></summary>
    public string HinweisTagesgangsatz { get; set; } = "Vorgabe · der Satz der Nutzungsart; ein unvollständiger Satz ist gesperrt.";

    /// <summary><c>ZPG_BTN_TAGESGANG</c></summary>
    public string KnopfTagesgang { get; set; } = "Tagesgang bearbeiten…";

    /// <summary><c>ZPG_BTN_KATEGORIEN</c></summary>
    public string KnopfKategorien { get; set; } = "Zapfkategorien und Streuung…";

    /// <summary><c>ZPG_GRP_FACHWERTE_GEBAEUDE</c></summary>
    public string GruppeFachwerteGebaeude { get; set; } = "Fachwerte · Gebäude";

    /// <summary><c>ZPG_LBL_KALTWASSER_AUSLEGUNG</c></summary>
    public string LabelKaltwasserAuslegung { get; set; } = "Kaltwasser der Auslegung";

    /// <summary><c>ZPG_HINW_KALTWASSER_AUSLEGUNG</c></summary>
    public string HinweisKaltwasserAuslegung { get; set; } = "Leer = {0} °C; gilt für alle Tage der Auslegung.";

    /// <summary><c>ZPG_LBL_SPEICHERTEMPERATUR</c></summary>
    public string LabelSpeichertemperatur { get; set; } = "Speichertemperatur";

    /// <summary><c>ZPG_HINW_SPEICHERTEMPERATUR</c></summary>
    public string HinweisSpeichertemperatur { get; set; } = "Leer = {0} °C; eine Großanlage nimmt die Mindesttemperatur nach DVGW W 551.";

    /// <summary><c>ZPG_LBL_ZIRK_KENNWERT</c></summary>
    public string LabelZirkKennwert { get; set; } = "Zirkulationsverlust (Kennwert)";

    /// <summary><c>ZPG_LBL_ZIRK_FLAECHE</c></summary>
    public string LabelZirkFlaeche { get; set; } = "Zirkulationsfläche";

    /// <summary><c>ZPG_LBL_ZIRK_LAUFZEIT</c></summary>
    public string LabelZirkLaufzeit { get; set; } = "Laufzeit der Zirkulation";

    /// <summary><c>ZPG_HINW_ZIRK_EXPERTE</c></summary>
    public string HinweisZirkExperte { get; set; } = "Leer = Kennwert nach der Lage ({0} bzw. {1} kWh/(m²·a), Verfahren nach DIN V 4701-10), Fläche aus Wohnfläche je Wohneinheit bzw. Gebäude, Laufzeit {2} h/d nach DVGW W 551.";

    /// <summary><c>ZPG_LBL_ANZEIGETEMPERATUR</c></summary>
    public string LabelAnzeigetemperatur { get; set; } = "Temperatur der Literanzeige";

    /// <summary><c>ZPG_LBL_STUNDENSCHWELLE</c></summary>
    public string LabelStundenschwelle { get; set; } = "Schwelle der Stundenzählung";

    /// <summary><c>ZPG_HINW_ANZEIGE</c></summary>
    public string HinweisAnzeige { get; set; } = "Nur für die Kennzahlen, nicht gespeichert; leer = Einstellung, sonst {0} °C bzw. {1} kW.";

    // ------------------------------------------------------------ Vorschau: Dauerlinie und Warnliste (Z4)

    /// <summary><c>ZPG_UNTERSCHRIFT_DAUERLINIE</c></summary>
    public string UnterschriftDauerlinie { get; set; } = "Sortierte Stundenwerte der Bilanzreihe (Zapfung und Zirkulation) — Bilanzwerte, keine Auslegungsgröße.";

    /// <summary><c>ZPG_DAUERLINIE_MARKE</c></summary>
    public string DauerlinieMarke { get; set; } = "P{0} · {1} kW (Rang {2})";

    /// <summary><c>ZPG_DAUERLINIE_SCHWELLE</c></summary>
    public string DauerlinieSchwelle { get; set; } = "{0} Stunden über {1} kW";

    /// <summary><c>ZPG_DAUERLINIE_OHNE</c></summary>
    public string DauerlinieOhne { get; set; } = "Die Zone trägt 0 — keine Dauerlinie.";

    /// <summary><c>ZPG_GRP_WARNLISTE</c></summary>
    public string GruppeWarnliste { get; set; } = "Hinweise der Bilanz";

    /// <summary><c>ZPG_WARNLISTE_UNTER</c></summary>
    public string WarnlisteUnter { get; set; } = "Nicht blockierend; die Hinweise einer Zone stehen auch an ihrem Eingabeblock.";

    /// <summary><c>ZPG_WARNLISTE_LEER</c></summary>
    public string WarnlisteLeer { get; set; } = "Keine Hinweise.";

    /// <summary><c>ZPG_AUS_STUFE_WARNUNG</c></summary>
    public string StufeWarnung { get; set; } = "Warnung";

    /// <summary><c>ZPG_AUS_STUFE_HINWEIS</c></summary>
    public string StufeHinweis { get; set; } = "Hinweis";
}
