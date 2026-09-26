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

    /// <summary><c>ZPG_BTN_MESSDATEN</c></summary>
    public string KnopfMessdaten { get; set; } = "Messdaten…";

    /// <summary><c>ZPG_MESSDATEN_TITEL</c></summary>
    public string MessdatenTitel { get; set; } = "Messdaten";

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

    /// <summary><c>ZPG_GRP_TYPTAGE</c></summary>
    public string GruppeTyptage { get; set; } = "Typtage nach VDI 4655";

    /// <summary><c>ZPG_LBL_TYPTAGE_AKTIV</c></summary>
    public string LabelTyptageAktiv { get; set; } = "Typtage nach VDI 4655 rechnen";

    /// <summary><c>ZPG_LBL_TYPTAGE_ZONE</c></summary>
    public string LabelTyptageZone { get; set; } = "Klimazone";

    /// <summary><c>ZPG_LBL_TYPTAGE_GEBAEUDEART</c></summary>
    public string LabelTyptageGebaeudeart { get; set; } = "Gebäudeart";

    /// <summary><c>ZPG_HINW_TYPTAGE</c></summary>
    public string HinweisTyptage { get; set; } = "Eine Angabe des Projekts: Der Jahresgang jeder Zone entsteht dann aus den Typtagen des eingespielten Pakets und dem Wetter der Klimaregion. Die Auslegung bleibt unberührt.";

    /// <summary><c>ZPG_HINW_TYPTAGE_OHNE</c></summary>
    public string HinweisTyptageOhne { get; set; } = "Ohne eingespielte Typtage ist dieser Weg nicht verfügbar — „VDI-4655-Typtage…“ spielt ein Paket ein.";

    /// <summary><c>ZPG_BTN_TYPTAGE</c></summary>
    public string KnopfTyptage { get; set; } = "VDI-4655-Typtage…";

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

    // ------------------------------------------------------- Karte „Herkunft"

    /// <summary><c>ZPG_GRP_HERKUNFT</c></summary>
    public string GruppeHerkunft { get; set; } = "Herkunft";

    /// <summary><c>ZPG_HERKUNFT_UNTER</c></summary>
    public string HerkunftUnter { get; set; } = "Woher jeder Wert dieser Rechnung kommt — in der Reihenfolge, in der der Rechenweg ihn festlegt.";

    /// <summary><c>ZPG_HERKUNFT_LEER</c></summary>
    public string HerkunftLeer { get; set; } = "nichts zu vermerken";

    /// <summary><c>ZPG_HERKUNFT_SP_GROESSE</c></summary>
    public string HerkunftSpalteGroesse { get; set; } = "Größe";

    /// <summary><c>ZPG_HERKUNFT_SP_WERT</c></summary>
    public string HerkunftSpalteWert { get; set; } = "Wert";

    /// <summary><c>ZPG_HERKUNFT_SP_ZONE</c></summary>
    public string HerkunftSpalteZone { get; set; } = "Zone";

    /// <summary><c>ZPG_HERKUNFT_SP_STAND</c></summary>
    public string HerkunftSpalteStand { get; set; } = "Stand";

    /// <summary><c>ZPG_HERKUNFT_SP_QUELLE</c></summary>
    public string HerkunftSpalteQuelle { get; set; } = "Quelle";

    /// <summary><c>ZPG_HERKUNFT_SP_VERMERK</c></summary>
    public string HerkunftSpalteVermerk { get; set; } = "Vermerk";

    /// <summary><c>ZPG_AUS_STUFE_WARNUNG</c></summary>
    public string StufeWarnung { get; set; } = "Warnung";

    /// <summary><c>ZPG_AUS_STUFE_HINWEIS</c></summary>
    public string StufeHinweis { get; set; } = "Hinweis";

    // ------------------------------------------------------------ Editoren Tagesgang und Zapfkategorien (Z4, Gruppe 2b)

    /// <summary><c>ZPG_GRUND_OHNE_NUTZUNGSART</c></summary>
    public string GrundOhneNutzungsart { get; set; } = "Zuerst eine Nutzungsart wählen.";

    /// <summary><c>ZPG_TAGTYP_RUHETAG</c></summary>
    public string TagtypRuhetag { get; set; } = "Ruhetag";

    /// <summary><c>ZPG_TGE_TITEL</c></summary>
    public string TgeTitel { get; set; } = "Tagesgang bearbeiten";

    /// <summary><c>ZPG_TGE_KONTEXT</c></summary>
    public string TgeKontext { get; set; } = "Nutzungsart {0} · Tagesgangsatz {1}";

    /// <summary><c>ZPG_TGE_GRP_TAGESGANG</c></summary>
    public string TgeGruppeTagesgang { get; set; } = "Tagesgang je Tagtyp [%]";

    /// <summary><c>ZPG_TGE_LBL_TAGTYP</c></summary>
    public string TgeLabelTagtyp { get; set; } = "Tagtyp";

    /// <summary><c>ZPG_TGE_FELD_STUNDE</c></summary>
    public string TgeFeldStunde { get; set; } = "{0} · Stunde {1}";

    /// <summary><c>ZPG_TGE_HERKUNFT</c></summary>
    public string TgeHerkunft { get; set; } = "Herkunft: {0}";

    /// <summary><c>ZPG_TGE_SUMME_OK</c></summary>
    public string SummeOk { get; set; } = "summiert zu 100 %";

    /// <summary><c>ZPG_TGE_SUMME_ABWEICHEND</c></summary>
    public string SummeAbweichend { get; set; } = "Summe {0} % — korrigieren oder normieren; „OK“ normiert auf 100 %.";

    /// <summary><c>ZPG_TGE_SUMME_NULL</c></summary>
    public string SummeNull { get; set; } = "Summe 0 % — ohne Anteile lässt sich nichts normieren.";

    /// <summary><c>ZPG_TGE_VORSCHAU_NORMIERT</c></summary>
    public string TgeVorschauNormiert { get; set; } = "Nach der Normierung: {0}";

    /// <summary><c>ZPG_TGE_BTN_NORMIEREN</c></summary>
    public string KnopfNormieren { get; set; } = "Normieren";

    /// <summary><c>ZPG_TGE_BTN_TAG_KOPIEREN</c></summary>
    public string KnopfTagKopieren { get; set; } = "Tag kopieren";

    /// <summary><c>ZPG_TGE_BTN_TAG_EINFUEGEN</c></summary>
    public string KnopfTagEinfuegen { get; set; } = "Tag einfügen";

    /// <summary><c>ZPG_TGE_GRUND_EINFUEGEN</c></summary>
    public string TgeGrundEinfuegen { get; set; } = "Zuerst einen Tag kopieren.";

    /// <summary><c>ZPG_TGE_GRP_WOCHE</c></summary>
    public string TgeGruppeWoche { get; set; } = "Wochenfaktoren Mo–So [%]";

    /// <summary><c>ZPG_TGE_HINW_WOCHE</c></summary>
    public string TgeHinweisWoche { get; set; } = "Anteil jedes Wochentags an der Wochenmenge (Σ 100 %); Sonn- und Feiertage rechnen mit dem Sonntag.";

    /// <summary><c>ZPG_TGE_GRP_VORLAGE</c></summary>
    public string TgeGruppeVorlage { get; set; } = "Vorlage aus dem Katalog";

    /// <summary><c>ZPG_TGE_LBL_VORLAGE</c></summary>
    public string TgeLabelVorlage { get; set; } = "Tagesgangsatz";

    /// <summary><c>ZPG_TGE_BTN_VORLAGE</c></summary>
    public string TgeKnopfVorlage { get; set; } = "Vorlage laden";

    /// <summary><c>ZPG_TGE_HINW_VORLAGE</c></summary>
    public string TgeHinweisVorlage { get; set; } = "„Vorlage laden“ übernimmt die vier Tagesgänge des gewählten Satzes, die Wochenfaktoren bleiben; „Zurücksetzen“ stellt die Werte beim Öffnen wieder her.";

    /// <summary><c>ZPG_TGE_GRUND_VORLAGE</c></summary>
    public string TgeGrundVorlage { get; set; } = "Zuerst einen Tagesgangsatz wählen.";

    /// <summary><c>ZPG_TGE_STATUS_GESPEICHERT</c></summary>
    public string TgeStatusGespeichert { get; set; } = "Tagesgang gespeichert — die Zone „{0}“ rechnet mit „{1}“.";

    /// <summary><c>ZPG_BTN_ZURUECKSETZEN</c></summary>
    public string KnopfZuruecksetzen { get; set; } = "Zurücksetzen";

    /// <summary><c>ZPG_BTN_KOPIE</c></summary>
    public string KnopfKopie { get; set; } = "Als eigene Kopie bearbeiten…";

    /// <summary><c>ZPG_LBL_KATALOGVERSION_KOPIE</c></summary>
    public string LabelKatalogversion { get; set; } = "Katalogversion der Kopie";

    /// <summary><c>ZPG_HINW_KOPIE</c></summary>
    public string HinweisKopie { get; set; } = "„OK“ legt die Nutzungsart unter dieser Katalogversion neu an (Status eigen) und stellt die Zone auf sie um; die gesperrte bleibt, wie sie ist.";

    /// <summary><c>ZPG_HINW_FREI</c></summary>
    public string HinweisFrei { get; set; } = "„OK“ schreibt an Ort und Stelle in den Katalog — in einer Transaktion; der Freigabevermerk der Nutzungsart entfällt.";

    /// <summary><c>ZPG_HINW_NUR_LESEN</c></summary>
    public string HinweisNurLesen { get; set; } = "Nur lesbar — bearbeiten lässt sich eine eigene Kopie.";

    /// <summary><c>ZPG_KATEG_TITEL</c></summary>
    public string KatTitel { get; set; } = "Zapfkategorien und Streuung";

    /// <summary><c>ZPG_KATEG_KONTEXT</c></summary>
    public string KatKontext { get; set; } = "Nutzungsart {0} · {1} Kategorien · Σ Anteile {2}";

    /// <summary><c>ZPG_KATEG_HINW_REGELN</c></summary>
    public string KatHinweisRegeln { get; set; } = "μ und σ ≥ 0 l/min, Dauer 1 … 1440 min, Anteil ≥ 0 mit Σ > 0 (die Rechnung normiert auf 1), Kappung > 0 oder leer; jeder Name einmal.";

    /// <summary><c>ZPG_KATEG_SP_REIHENFOLGE</c></summary>
    public string KatSpalteReihenfolge { get; set; } = "Reihenfolge";

    /// <summary><c>ZPG_KATEG_SP_NAME</c></summary>
    public string KatSpalteName { get; set; } = "Kategorie";

    /// <summary><c>ZPG_KATEG_SP_VOLUMENSTROM</c></summary>
    public string KatSpalteVolumenstrom { get; set; } = "Volumenstrom μ [l/min]";

    /// <summary><c>ZPG_KATEG_SP_DAUER</c></summary>
    public string KatSpalteDauer { get; set; } = "Dauer [min]";

    /// <summary><c>ZPG_KATEG_SP_ANTEIL</c></summary>
    public string KatSpalteAnteil { get; set; } = "Anteil [–]";

    /// <summary><c>ZPG_KATEG_SP_STREUUNG</c></summary>
    public string KatSpalteStreuung { get; set; } = "Streuung σ [l/min]";

    /// <summary><c>ZPG_KATEG_SP_KAPPUNG</c></summary>
    public string KatSpalteKappung { get; set; } = "Kappung [l/min]";

    /// <summary><c>ZPG_KATEG_SP_HERKUNFT</c></summary>
    public string KatSpalteHerkunft { get; set; } = "Herkunft";

    /// <summary><c>ZPG_KATEG_KAPPUNG_KEINE</c></summary>
    public string KatKappungKeine { get; set; } = "keine";

    /// <summary><c>ZPG_KATEG_BTN_NEU</c></summary>
    public string KatKnopfNeu { get; set; } = "Kategorie hinzufügen";

    /// <summary><c>ZPG_KATEG_BTN_ENTFERNEN</c></summary>
    public string KatKnopfEntfernen { get; set; } = "Entfernen";

    /// <summary><c>ZPG_KATEG_BTN_HOCH</c></summary>
    public string KatKnopfHoch { get; set; } = "Nach oben";

    /// <summary><c>ZPG_KATEG_BTN_RUNTER</c></summary>
    public string KatKnopfRunter { get; set; } = "Nach unten";

    /// <summary><c>ZPG_KATEG_GRUND_LETZTE</c></summary>
    public string KatGrundLetzte { get; set; } = "Die letzte Kategorie bleibt — ohne Kategorie rechnet die Zone nicht stochastisch.";

    /// <summary><c>ZPG_KATEG_GRUND_RAND</c></summary>
    public string KatGrundRand { get; set; } = "Die Kategorie steht schon am Rand der Liste.";

    /// <summary><c>ZPG_KATEG_BTN_VORGABE</c></summary>
    public string KatKnopfVorgabe { get; set; } = "Vorgabesatz laden";

    /// <summary><c>ZPG_KATEG_HINW_VORGABE</c></summary>
    public string KatHinweisVorgabe { get; set; } = "Der Vorgabesatz sind die frei dokumentierten Kategorien des Katalogs (Modellannahme); er ersetzt die Zeilen im Editor.";

    /// <summary><c>ZPG_KATEG_GRUND_OHNE_VORGABE</c></summary>
    public string KatGrundOhneVorgabe { get; set; } = "Der Katalog führt keinen Vorgabesatz der Zapfkategorien.";

    /// <summary><c>ZPG_KATEG_NEU_NAME</c></summary>
    public string KatNeuName { get; set; } = "Kategorie {0}";

    /// <summary><c>ZPG_KATEG_LEER</c></summary>
    public string KatLeer { get; set; } = "Die Nutzungsart führt keine Zapfkategorien — „Vorgabesatz laden“ oder „Kategorie hinzufügen“.";

    /// <summary><c>ZPG_KATEG_STATUS_GESPEICHERT</c></summary>
    public string KatStatusGespeichert { get; set; } = "Zapfkategorien gespeichert — die Zone „{0}“ rechnet mit „{1}“.";

    // =====================================================================
    // Vergleich mit einer Messreihe und Kalibrierung (Stufe Z5, Gruppe 3)
    // =====================================================================

    /// <summary><c>ZPG_GRP_VERGLEICH</c></summary>
    public string GruppeVergleich { get; set; } = "Vergleich mit einer Messreihe";

    /// <summary><c>ZPG_LBL_MESSREIHE</c></summary>
    public string LabelMessreihe { get; set; } = "Messreihe";

    /// <summary><c>ZPG_BTN_VERGLEICH</c></summary>
    public string KnopfVergleich { get; set; } = "Vergleich rechnen";

    /// <summary><c>ZPG_KZ_VERGLEICH</c></summary>
    public string KennzahlVergleich { get; set; } = "Vergleich mit der Messung";

    /// <summary><c>ZPG_VERGL_OHNE_REIHE</c></summary>
    public string VergleichOhneReihe { get; set; } = "Es ist keine Messreihe eingespielt — „Messdaten…“ spielt eine ein.";

    /// <summary><c>ZPG_VERGL_OHNE_WAHL</c></summary>
    public string VergleichOhneWahl { get; set; } = "Wählen Sie die Messreihe, gegen die verglichen werden soll.";

    /// <summary><c>ZPG_VERGL_NUR_ERWEITERT</c></summary>
    public string VergleichNurErweitert { get; set; } = "Der Vergleich mit einer Messreihe steht ab der Stufe Erweitert.";

    /// <summary><c>ZPG_VERGL_LEER</c></summary>
    public string VergleichLeer { get; set; } = "Noch kein Vergleich gerechnet.";

    /// <summary><c>ZPG_VERGL_LAEUFT</c></summary>
    public string VergleichLaeuft { get; set; } = "Vergleich läuft — Messreihe „{0}“";

    /// <summary><c>ZPG_VERGL_ABGEBROCHEN</c></summary>
    public string VergleichAbgebrochen { get; set; } = "Der Vergleich ist abgebrochen; der Arbeitsstand bleibt.";

    /// <summary><c>ZPG_VERGL_VERALTET</c></summary>
    public string VergleichVeraltet { get; set; } = "Der Vergleich gehört zu einem früheren Arbeitsstand — neu rechnen.";

    /// <summary><c>ZPG_VERGL_STATUS</c></summary>
    public string VergleichStatus { get; set; } = "Vergleich mit „{0}“ gerechnet";

    /// <summary><c>ZPG_VERGL_STOCHASTISCH</c></summary>
    public string VergleichStochastisch { get; set; } = "Die verglichene Jahresreihe ist stochastisch gerechnet.";

    /// <summary><c>ZPG_KZ_ENERGIE_VERHAELTNIS</c></summary>
    public string KzEnergie { get; set; } = "Energie gemessen/gerechnet";

    /// <summary><c>ZPG_KZ_ENERGIE_ABWEICHUNG</c></summary>
    public string KzEnergieAbweichung { get; set; } = "Abweichung der Energie";

    /// <summary><c>ZPG_KZ_SPITZE</c></summary>
    public string KzSpitze { get; set; } = "Messspitze / größte gerechnete Stunde";

    /// <summary><c>ZPG_KZ_BAND</c></summary>
    public string KzBand { get; set; } = "Band der Dauerlinie (P{0}–P{1})";

    /// <summary><c>ZPG_KZ_SPITZENSTREUUNG</c></summary>
    public string KzSpitzenstreuung { get; set; } = "Streuung der Realisierungsspitzen";

    /// <summary><c>ZPG_KZ_WURZELN</c></summary>
    public string KzWurzelN { get; set; } = "√N-Skalierungsmaß";

    /// <summary><c>ZPG_KZ_FORM</c></summary>
    public string KzForm { get; set; } = "Formabgleich — größte mittlere Abweichung";

    /// <summary><c>ZPG_KZ_FORM_TAGTYP</c></summary>
    public string KzFormTagtyp { get; set; } = "Form {0}";

    /// <summary><c>ZPG_KZ_MONATE</c></summary>
    public string KzMonate { get; set; } = "Größte Abweichung der Monatsanteile";

    /// <summary><c>ZPG_LAGE_IM_BAND</c></summary>
    public string LageImBand { get; set; } = "im Band";

    /// <summary><c>ZPG_LAGE_OBERHALB</c></summary>
    public string LageOberhalb { get; set; } = "über dem Band — die Rechnung unterschätzt die Spitze";

    /// <summary><c>ZPG_LAGE_UNTERHALB</c></summary>
    public string LageUnterhalb { get; set; } = "unter dem Band — die Rechnung überschätzt die Spitze stärker als erwartet";

    /// <summary><c>ZPG_LAGE_UNBESTIMMT</c></summary>
    public string LageUnbestimmt { get; set; } = "nicht entscheidbar";

    /// <summary><c>ZPG_LAGE_NICHT_BEWERTBAR</c></summary>
    public string LageNichtBewertbar { get; set; } = "nicht bewertbar";

    // {0} Einheiten der Anlage, {1} Mindestzahl (ZU35).
    /// <summary><c>ZPG_VERGL_BAND_NICHT_BEWERTBAR</c></summary>
    public string VermerkBandNichtBewertbar { get; set; } = "nicht bewertbar — {0} Einheiten, bewertet wird ab {1}";

    /// <summary><c>ZPG_FORM_IM_RAHMEN</c></summary>
    public string FormImRahmen { get; set; } = "✓ im Rahmen";

    /// <summary><c>ZPG_FORM_UEBER_SCHWELLE</c></summary>
    public string FormUeberSchwelle { get; set; } = "≠ über der Schwelle {0}";

    /// <summary><c>ZPG_VERGL_OHNE_ENSEMBLE</c></summary>
    public string VermerkOhneEnsemble { get; set; } = "ohne Ensemble nicht entscheidbar";

    /// <summary><c>ZPG_VERGL_ENSEMBLE_ZONEN</c></summary>
    public string VermerkEnsembleZonen { get; set; } = "mehrere stochastische Zonen — Stichprobe nicht bildbar";

    /// <summary><c>ZPG_VERGL_OHNE_WERT</c></summary>
    public string VermerkOhneWert { get; set; } = "nicht entschieden";

    /// <summary><c>ZPG_VERGL_MONAT</c></summary>
    public string VermerkMonat { get; set; } = "Monat {0}";

    /// <summary><c>ZPG_VERGL_TAGE</c></summary>
    public string VermerkTage { get; set; } = "{0} Messtage / {1} Rechentage";

    /// <summary><c>ZPG_VERGL_DAUERLINIE</c></summary>
    public string VermerkDauerlinie { get; set; } = "über {0} Stundenwerte";

    /// <summary><c>ZPG_VERGL_EINHEITEN</c></summary>
    public string VermerkEinheiten { get; set; } = "N = {0}, 1/√N = {1}";

    /// <summary><c>ZPG_VERGL_STREUBREITE</c></summary>
    public string VermerkStreubreite { get; set; } = "{0} Realisierungen, Streubreite {1}";

    /// <summary><c>ZPG_BTN_KALIBRIEREN</c></summary>
    public string KnopfKalibrieren { get; set; } = "Aus Messreihe kalibrieren";

    /// <summary><c>ZPG_BTN_VORSCHLAG_KALIBRIERT</c></summary>
    public string KnopfKalibriervorschlag { get; set; } = "Vorschlag übernehmen…";

    /// <summary><c>ZPG_HINW_KALIBRIEREN</c></summary>
    public string HinweisKalibrieren { get; set; } = "Der Jahresmesswert kommt aus der Messreihe, die der Reiter „Kennzahlen“ nennt; ein Teiljahr wird über den Jahresgang der Rechnung hochgerechnet, und ein Hinweis nennt den Bias.";

    /// <summary><c>ZPG_FRAGE_KALIBRIEREN</c></summary>
    public string FrageKalibrieren { get; set; } = "Der Jahresmesswert der Zone „{0}“ wird aus der Messreihe „{1}“ gesetzt; ein vorhandener Wert fällt weg. Übernehmen?";

    /// <summary><c>ZPG_STATUS_KALIBRIERT</c></summary>
    public string StatusKalibriert { get; set; } = "Jahresmesswert aus „{0}“ übernommen";

    /// <summary><c>ZPG_KAL_LAEUFT</c></summary>
    public string KalibrierungLaeuft { get; set; } = "Kalibrierung läuft — Messreihe „{0}“";

    /// <summary><c>ZPG_KAL_ABGEBROCHEN</c></summary>
    public string KalibrierungAbgebrochen { get; set; } = "Die Kalibrierung ist abgebrochen; der Arbeitsstand bleibt.";

    /// <summary><c>ZPG_VORSCHLAG_TITEL</c></summary>
    public string VorschlagTitel { get; set; } = "Kalibriervorschlag";

    /// <summary><c>ZPG_VORSCHLAG_VORLAGE</c></summary>
    public string VorschlagVorlage { get; set; } = "Vorlage";

    /// <summary><c>ZPG_VORSCHLAG_KOPIE</c></summary>
    public string VorschlagKopie { get; set; } = "Bezeichnung der Kopie";

    /// <summary><c>ZPG_VORSCHLAG_TAGESBEDARF</c></summary>
    public string VorschlagTagesbedarf { get; set; } = "Tagesbedarf";

    /// <summary><c>ZPG_VORSCHLAG_JE_EINHEIT</c></summary>
    public string VorschlagJeEinheit { get; set; } = "Tagesbedarf je Einheit";

    /// <summary><c>ZPG_VORSCHLAG_TAGE</c></summary>
    public string VorschlagTage { get; set; } = "Vollständige Messtage";

    /// <summary><c>ZPG_VORSCHLAG_WOCHE</c></summary>
    public string VorschlagWoche { get; set; } = "Wochenfaktoren Mo–So";

    /// <summary><c>ZPG_VORSCHLAG_GAENGE</c></summary>
    public string VorschlagGaenge { get; set; } = "Tagesgänge je Tagtyp [%]";

    /// <summary><c>ZPG_VORSCHLAG_STUNDE</c></summary>
    public string VorschlagStunde { get; set; } = "Stunde";

    /// <summary><c>ZPG_VORSCHLAG_HINWEIS</c></summary>
    public string VorschlagHinweis { get; set; } = "Die Übernahme legt eine eigene Nutzungsart an; die Vorlage bleibt unberührt, und die Zone rechnet danach mit der Kopie.";

    /// <summary><c>ZPG_BTN_VORSCHLAG_UEBERNEHMEN</c></summary>
    public string KnopfVorschlagUebernehmen { get; set; } = "Übernehmen";

    /// <summary><c>ZPG_FRAGE_VORSCHLAG</c></summary>
    public string FrageVorschlag { get; set; } = "Aus der Messreihe „{0}“ entsteht die Nutzungsart „{1}“; die Zone rechnet danach mit ihr, die Vorlage bleibt unberührt. Übernehmen?";

    /// <summary><c>ZPG_VORSCHLAG_OHNE</c></summary>
    public string VorschlagOhne { get; set; } = "Für diese Zone gibt es keinen Vorschlag: {0}";
}
