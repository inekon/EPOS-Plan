namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte des Dialogs „Brauchwasser-Zapfprofil" in der Stufe Einfach samt Vorschau
/// und Fußleiste (Umsetzungskonzept Zapfprofilgenerator 5.1, 5.3) — EIN Parameter statt
/// neunzig.
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
    public string KennzahlStochastikErklaerung { get; set; } = "„Stochastisch rechnen“ in der Fußleiste zieht das Ensemble für Perzentile und Auslegung; die Bilanzreihe bleibt deterministisch. Die Einzelwerte zeigt die Stufe Experte.";

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

    // ------------------------------------------------------------ Fussleiste

    /// <summary><c>ZPG_BTN_STOCHASTIK</c></summary>
    public string KnopfStochastik { get; set; } = "Stochastisch rechnen";

    /// <summary><c>ZPG_BTN_AUSLEGUNG</c></summary>
    public string KnopfAuslegung { get; set; } = "Auslegung…";

    /// <summary><c>ZPG_STATUS_AUSLEGUNG</c></summary>
    public string StatusAuslegung { get; set; } = "Auslegung übernommen: {0}";

    /// <summary><c>ZPG_AUSLEGUNG_OHNE_PUNKT</c></summary>
    public string AuslegungOhnePunkt { get; set; } = "Eingaben ohne Punkt";

    /// <summary><c>ZPG_STATUS_VORSCHAU</c></summary>
    public string StatusVorschau { get; set; } = "Vorschau aktuell · deterministisch · Stochastik noch nicht gerechnet";

    /// <summary><c>ZPG_STATUS_OHNE_VORSCHAU</c></summary>
    public string StatusOhneVorschau { get; set; } = "Keine Vorschau — {0}";
}
