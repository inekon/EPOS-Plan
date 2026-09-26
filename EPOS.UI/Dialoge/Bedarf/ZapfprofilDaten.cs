using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DATENSEITE des Dialogs „Brauchwasser-Zapfprofil" (Umsetzungskonzept
//  Zapfprofilgenerator 5.1–5.3; Stufe Z1, Gruppe 3; die Angaben der Stufen Erweitert und
//  Experte mit Stufe Z4, Gruppe 2a). Nur Daten: keine Datenbank, keine
//  Fachklasse des Kerns. Die Hülle (EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.cs) ist die
//  einzige Stelle, die den Arbeitsstand des Kerns (ZapfprofilStand) in diese Typen
//  übersetzt und zurück; der Dialog rechnet nichts nach.
// =====================================================================================

/// <summary>Die Tiefe des Dialogs (5.1): Die Stufe blendet nur ein und aus, Überschreibungen bleiben.</summary>
public enum ZapfprofilStufe
{
    Einfach = 0,
    Erweitert = 1,
    Experte = 2
}

/// <summary>
/// Die Weiche des Brauchwasserkanals als DTO (Konzept 2.2, ZU4): der Wert der
/// Optionsgruppe „Rechenweg Brauchwasser" im Bedarfsprofil-Dialog.
/// </summary>
public enum ZapfprofilWeg
{
    /// <summary>Bestandsprofile — der heutige Weg (Vorgabe ohne Zapfprofil).</summary>
    Bestand = 0,

    /// <summary>Der Kanal kommt aus dem Zapfprofilgenerator.</summary>
    Generator = 1
}

/// <summary>Das Bedarfsniveau einer Zone; die Zahlen sind die des Kerns (1 … 3).</summary>
public enum ZapfprofilNiveau
{
    Niedrig = 1,
    Mittel = 2,
    Hoch = 3
}

/// <summary>Die Topologie der Trinkwassererwärmung einer Zone (Stufe Erweitert); die Zahlen des Kerns (1 … 4).</summary>
public enum ZapfprofilTopologie
{
    Speicher = 1,
    Frischwasserstation = 2,
    Durchfluss = 3,
    Wohnungsstation = 4
}

/// <summary>Die Einheit eines Jahresmesswerts; die Zahlen des Kerns.</summary>
public enum ZapfprofilMesswerteinheit
{
    KwhJeJahr = 1,
    KubikmeterJeJahr = 2
}

/// <summary>Die Bilanzgrenze eines Jahresmesswerts (4.1); die Zahlen des Kerns.</summary>
public enum ZapfprofilBilanzgrenze
{
    /// <summary>An der Zapfstelle.</summary>
    Zapfstelle = 1,

    /// <summary>Mit Verteil- und Zirkulationsverlust.</summary>
    MitVerteilung = 2,

    /// <summary>Zusätzlich mit Speicherverlust.</summary>
    MitSpeicher = 3
}

/// <summary>Die Methode der Zirkulation (4.3); die Zahlen des Kerns.</summary>
public enum ZapfprofilZirkulationsmethode
{
    Leitungslaenge = 1,
    Anteil = 2,
    Flaechenkennwert = 3
}

/// <summary>Die Lage der Zirkulationsleitung; die Zahlen des Kerns.</summary>
public enum ZapfprofilLeitungslage
{
    InnerhalbHuelle = 1,
    AusserhalbHuelle = 2
}

/// <summary>Wie die Jahresmenge einer Zone entsteht — die Spalte „Rechenweg" der Zonenliste.</summary>
public enum ZapfprofilZonenrechenweg
{
    /// <summary>Mengengerüst aus dem Katalog (Tagesbedarf „auto").</summary>
    Katalog = 0,

    /// <summary>Der manuelle Tagesbedarf gilt.</summary>
    Manuell = 1,

    /// <summary>Ein Jahresmesswert setzt die Menge (Kalibrierung, 4.8).</summary>
    Messwert = 2,

    /// <summary>Der Rechenweg hat die Zone abgelehnt — sie trägt 0.</summary>
    Abgelehnt = 3
}

/// <summary>Wie schwer eine Meldung wiegt — sie entscheidet über leise Zeile oder Banner, nie über das Blockieren.</summary>
public enum ZapfprofilMeldungsart
{
    /// <summary>Ein nicht blockierender Hinweis des Rechenwegs (am Feld bzw. in der Liste).</summary>
    Hinweis = 0,

    /// <summary>Eine benannte Ablehnung: Die Zone (oder die Zirkulation) trägt 0, die übrigen rechnen.</summary>
    Ablehnung = 1,

    /// <summary>Ein Zustand, den der Anwender beheben muss (Pflichtfeld, Speichern abgelehnt, keine Vorschau).</summary>
    Fehler = 2
}

/// <summary>
/// Der Zustand der Vorschau (Hausregel „ein Reiter zeichnet nie ein vorbelegtes DTO"): Ohne
/// Rechnung gibt es keine Ansicht, und der Grund steht daneben.
/// </summary>
public enum ZapfprofilVorschauZustand
{
    /// <summary>Noch nicht gerechnet (etwa ohne Zone).</summary>
    NichtGerechnet = 0,

    /// <summary>Gerechnet — auch mit abgelehnten Zonen, die 0 tragen.</summary>
    Gerechnet = 1,

    /// <summary>Der Generator konnte für das Projekt nicht rechnen; <see cref="ZapfprofilVorschauDaten.Grund"/> nennt warum.</summary>
    Abgebrochen = 2
}

/// <summary>
/// Eine Meldung mit sprachneutraler <see cref="Kennung"/> (Hausregel „eine Meldung, die der
/// Anwender nicht versteht, bekommt eine Kennung"): <c>ZPG_SATZ_…</c> (ein Satz des Kerns —
/// Ablehnung, Hinweis, Grund des Schreibwegs) oder <c>ZPG_MSG_…</c>, <c>ZPG_AUS_…</c> (ein Satz
/// der Hülle) — der Ressourcenschlüssel des Textes.
/// </summary>
/// <param name="Kennung">Der Ressourcenschlüssel der Meldung.</param>
/// <param name="Zone">Die betroffene Zone; leer = Projekt bzw. Zirkulation.</param>
/// <param name="Text">Der Satz in der Oberflächensprache.</param>
/// <param name="Art">Hinweis, Ablehnung oder Fehler.</param>
/// <param name="Klartext">Der Wortlaut des Kerns (deutsch) für Protokoll und Assistent; leer, wenn es keinen gibt.</param>
/// <param name="Position">
/// Die Position der Zone in der Eingabe (0-basiert), wo die Hülle sie eindeutig bestimmen kann;
/// <c>null</c> = ohne Zone oder nicht bestimmt. Der Dialog ordnet eine Meldung darüber ihrer Zone
/// zu; ohne Position nur über einen Namen, den genau eine Zone trägt — sonst steht sie bei den
/// allgemeinen Meldungen, nie an zwei Zonen.
/// </param>
public sealed record ZapfprofilMeldung(string Kennung, string Zone, string Text, ZapfprofilMeldungsart Art,
                                       string Klartext = "", int? Position = null);

/// <summary>
/// Eine Nutzungsart des Katalogs, wie die Auswahl sie zeigt (5.3): Name, Bezugsart samt
/// Einheit, der Katalogwert je Niveau und die Herkunft als KURZTEXT — Verfahren oder
/// Eigenkonstruktion und Quelle, nie ein Hersteller- oder Produktname, nie ein Beleg.
/// </summary>
public sealed class ZapfprofilNutzungsartDaten
{
    /// <summary>Die Id der Katalogzeile (<c>Tab_TwwNutzungsart_STAMM.ID</c>).</summary>
    public int Id { get; set; }

    /// <summary>Der neutrale Name der Nutzungsart.</summary>
    public string Name { get; set; } = "";

    /// <summary>Die Bezugsart als Zahl des Kerns (1 Personen … 7 Fläche).</summary>
    public int Bezugsart { get; set; }

    /// <summary>Die Bezugsart als Text („Wohneinheiten").</summary>
    public string Bezugsgroesse { get; set; } = "";

    /// <summary>Die Einheit je Bezug als Kurztext („WE").</summary>
    public string Einheit { get; set; } = "";

    /// <summary>Der spezifische Bedarf je Niveau (niedrig, mittel, hoch) [kWh je Einheit und Tag].</summary>
    public double[] BedarfJeNiveauKwhJeEinheitTag { get; set; } = new double[3];

    /// <summary>Die Herkunft als Kurztext („Verfahren · Quelle, Ausgabe").</summary>
    public string Herkunft { get; set; } = "";

    /// <summary>Der Stand der Katalogzeile als Text (Auslieferung, eigen, Import).</summary>
    public string Status { get; set; } = "";

    /// <summary>Die Katalogversion der Zeile.</summary>
    public string Katalogversion { get; set; } = "";

    /// <summary>Gehört die Zeile zur Auslieferung (schreibgeschützt)?</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Kann eine Zone diese Nutzungsart wählen? Sonst nennt <see cref="Sperrgrund"/> den Grund.</summary>
    public bool Waehlbar { get; set; } = true;

    /// <summary>Warum die Nutzungsart nicht wählbar ist; leer, solange sie es ist.</summary>
    public string Sperrgrund { get; set; } = "";

    // ---- Stufe Erweitert und Experte (Z4): Vorgaben am Feld, nie Normtabellen -------

    /// <summary>Die Kalenderart als Zahl des Kerns (1 Wohnen … 5 Auslastungsgang).</summary>
    public int Kalenderart { get; set; }

    /// <summary>Die Kalenderart als Text („Wohnen").</summary>
    public string Kalender { get; set; } = "";

    /// <summary>
    /// Führt die Nutzungsart eine Wohnungstabelle (5.3: „nur Wohnen")? Allein die Bezugsart
    /// entscheidet (Wohneinheiten oder Personen, 4.1) — dasselbe Kriterium wie der Kern
    /// (<c>Mengengeruest.WohnungstabelleWirksam</c>, Z4, Gruppe 2a Punkt 6); der Kalender der
    /// Nutzungsart spielt dabei keine Rolle.
    /// </summary>
    public bool Wohnen { get; set; }

    /// <summary>Die Bilanzgrenze der Katalogwerte als Zahl des Kerns (1 … 3).</summary>
    public int Bilanzgrenze { get; set; }

    /// <summary>Die Bezugstemperatur der Zapfung [°C] — die Vorgabe der Zapftemperatur; <c>null</c> ohne Angabe.</summary>
    public double? ZapftemperaturC { get; set; }

    /// <summary>Die zwölf Monatsfaktoren des Katalogs — die Vorgabe des Auslastungsgangs.</summary>
    public double[] Monatsfaktoren { get; set; } = new double[12];

    /// <summary>Der Tagesgangsatz der Nutzungsart — die Vorgabe der Zone; <c>null</c> ohne Satz.</summary>
    public int? IdTagesgangsatz { get; set; }
}

/// <summary>
/// Ein Eintrag einer Katalogauswahl der höheren Stufen (Tagesgangsatz, Ausstattungsklasse,
/// Gebäude des Projekts): Id, neutraler Name, Herkunft als Kurztext und — wenn er nicht wählbar
/// ist — der Grund. Nie ein Tabellenwert, nie ein Beleg.
/// </summary>
public sealed class ZapfprofilKatalogeintragDaten
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Herkunft { get; set; } = "";
    public bool Waehlbar { get; set; } = true;
    public string Sperrgrund { get; set; } = "";
}

/// <summary>
/// Die Vorgaben des Parametersatzes für die Felder der Stufen Erweitert und Experte (Platzhalter
/// „leer = Vorgabe"): <c>null</c> = der Parametersatz führt den Wert nicht. Nur Anzeige — der
/// Rechenweg liest den Parametersatz selbst.
/// </summary>
public sealed class ZapfprofilVorgabenDaten
{
    public double? KaltwasserMittelC { get; set; }
    public double? KaltwasserAmplitudeK { get; set; }
    public double? WohnflaecheJeWeM2 { get; set; }
    public double? ZirkLaufzeitH { get; set; }
    public double? ZirkAnteil { get; set; }
    public double? ZirkVerlustWJeM { get; set; }

    /// <summary>Die Lage der Zirkulationsleitung als Zahl des Kerns (1, 2).</summary>
    public int? ZirkLage { get; set; }

    public double? ZirkKennwertLage1 { get; set; }
    public double? ZirkKennwertLage2 { get; set; }
    public double? KaltwasserAuslegungC { get; set; }
    public double? SpeicherC { get; set; }
    public double? LadefensterH { get; set; }
    public double? LadefensterBeginnH { get; set; }
    public double? AnzeigetemperaturC { get; set; }
    public double? SchwelleKw { get; set; }
}

/// <summary>Eine Zeile der Wohnungstabelle (5.3, nur Wohnen): Anzahl, Raumzahl, Personen, Ausstattungsklasse.</summary>
public sealed class ZapfprofilWohnungDaten
{
    /// <summary>Die Id der Zeile (<c>Tab_TwwWohnungstyp.ID</c>); 0 = neu.</summary>
    public int Id { get; set; }

    /// <summary>Die Anzahl der Wohnungen dieses Typs; <c>null</c> = nicht eingegeben (Pflicht, &gt; 0).</summary>
    public int? Anzahl { get; set; }

    /// <summary>Die Raumzahl; <c>null</c> = keine Angabe.</summary>
    public double? Raumzahl { get; set; }

    /// <summary>Die Personen je Wohnung; <c>null</c> = Belegung nach Raumzahl aus dem Katalog.</summary>
    public double? Personen { get; set; }

    /// <summary>Die Ausstattungsklasse; <c>null</c> = Vorgabeklasse.</summary>
    public int? IdAusstattung { get; set; }

    /// <summary>Eine unabhängige Kopie.</summary>
    public ZapfprofilWohnungDaten Kopie() => (ZapfprofilWohnungDaten)MemberwiseClone();
}

/// <summary>
/// Ein Ferienzeitraum einer Zone (5.3 „Kalender / Ferien", vier Zeiträume) als Tag und Monat —
/// die Hülle rechnet ihn in den Jahrestag des Kerns um (Rechenjahr ohne Schaltjahr). Leer =
/// keine Angabe; ein Beginn ohne Ende gilt nicht.
/// </summary>
public sealed class ZapfprofilFerienDaten
{
    public int? BeginnTag { get; set; }
    public int? BeginnMonat { get; set; }
    public int? EndeTag { get; set; }
    public int? EndeMonat { get; set; }

    /// <summary>Trägt der Zeitraum irgendeine Angabe?</summary>
    public bool Belegt => BeginnTag.HasValue || BeginnMonat.HasValue || EndeTag.HasValue || EndeMonat.HasValue;

    /// <summary>Eine unabhängige Kopie.</summary>
    public ZapfprofilFerienDaten Kopie() => (ZapfprofilFerienDaten)MemberwiseClone();
}

/// <summary>
/// <b>Die Angaben einer Zone in den Stufen Erweitert und Experte</b> (5.3) — jede nullbare Größe
/// heißt <c>null</c> = Vorgabe; die Einheit steht im Namen. Die Stufe blendet nur ein und aus: Die
/// Angaben bleiben, auch wenn der Anwender zurück auf Einfach schaltet.
/// </summary>
public sealed class ZapfprofilZonenangabenDaten
{
    /// <summary>Anzahl der Ferienzeiträume je Zone.</summary>
    public const int FERIENZEITRAEUME = 4;

    /// <summary>Anzahl der Monate des Auslastungsgangs.</summary>
    public const int MONATE = 12;

    /// <summary>Das gebundene Gebäude des Projekts (A8, belegt nur vor); <c>null</c> = Kalender der Nutzungsart.</summary>
    public int? IdGebaeude { get; set; }

    /// <summary>Der Tagesgangsatz (Experte); <c>null</c> = der Satz der Nutzungsart.</summary>
    public int? IdTagesgangsatz { get; set; }

    public double? PersonenJeWe { get; set; }
    public double? WohnflaecheJeWeM2 { get; set; }
    public ZapfprofilTopologie Topologie { get; set; } = ZapfprofilTopologie.Speicher;

    /// <summary>Zirkulation vorhanden? <c>false</c> nimmt die Zone aus dem Zirkulationsanteil (4.3).</summary>
    public bool Zirkulation { get; set; } = true;

    /// <summary>Die vier Ferienzeiträume der Zone.</summary>
    public List<ZapfprofilFerienDaten> Ferien { get; set; } = NeueFerien();

    public double? Jahresmesswert { get; set; }
    public ZapfprofilMesswerteinheit? JahresmesswertEinheit { get; set; }
    public ZapfprofilBilanzgrenze? JahresmesswertBilanzgrenze { get; set; }
    public string JahresmesswertQuelle { get; set; } = "";
    public string JahresmesswertZeitraum { get; set; } = "";

    /// <summary>Der Speicherverlust [kWh/a] — gebraucht, wenn der Messwert den Speicherverlust einschließt (Grenze 3).</summary>
    public double? SpeicherverlustKwhJeJahr { get; set; }

    /// <summary>Tagesbedarf „auto" (Katalog) oder „manuell".</summary>
    public bool TagesbedarfAuto { get; set; } = true;

    public double? TagesbedarfManuellKwh { get; set; }
    public double? BedarfSpezKwhJeEinheitTag { get; set; }
    public double? ZapftemperaturC { get; set; }
    public double? KaltwasserMittelC { get; set; }
    public double? KaltwasserAmplitudeK { get; set; }

    /// <summary>Zwölf Faktoren des Auslastungsgangs (Experte); <c>null</c> je Monat = Katalog.</summary>
    public double?[] Auslastung { get; set; } = new double?[MONATE];

    /// <summary>Die Wohnungstabelle (nur Wohnen); leer = keine.</summary>
    public List<ZapfprofilWohnungDaten> Wohnungen { get; set; } = new();

    /// <summary>Vier leere Ferienzeiträume.</summary>
    public static List<ZapfprofilFerienDaten> NeueFerien()
        => Enumerable.Range(0, FERIENZEITRAEUME).Select(_ => new ZapfprofilFerienDaten()).ToList();

    /// <summary>
    /// Wie viele Größen die Zone überschreibt (Zähler „n Werte überschrieben") — jede nullbare
    /// Größe mit Wert, jeder Schalter abseits seiner Vorgabe, jeder Ferienzeitraum und jeder
    /// Auslastungsmonat mit Wert, eine gepflegte Wohnungstabelle. Die Bindung an ein Gebäude ist
    /// keine Überschreibung. EINE Stelle: Die Hülle zählt den Stand des Kerns hierüber.
    ///
    /// <para><b>Nur WIRKSAME Abweichungen zählen</b> (Z4, Gruppe 2a Punkt 5): das Paar
    /// „Tagesbedarf auto/manuell" zählt EINMAL (ein manueller Wert bei „auto" ist wirkungslos und
    /// zählt für sich allein nicht); der Speicherverlust nur mit Bilanzgrenze 3 UND einem
    /// Messwert (sonst rechnet ihn niemand); die Wohnungstabelle nur, wenn
    /// <paramref name="wohnungstabelleWirksam"/> gilt (Bezugsart Wohneinheiten/Personen der
    /// Nutzungsart, <c>Mengengeruest.WohnungstabelleWirksam</c> im Kern) — sonst ist sie verdeckt
    /// und zählt nicht, auch mit gepflegten Zeilen.</para>
    /// </summary>
    public int Ueberschrieben(bool wohnungstabelleWirksam = true)
    {
        int n = 0;
        if (IdTagesgangsatz.HasValue) n++;
        if (PersonenJeWe.HasValue) n++;
        if (WohnflaecheJeWeM2.HasValue) n++;
        if (Topologie != ZapfprofilTopologie.Speicher) n++;
        if (!Zirkulation) n++;
        n += (Ferien ?? new List<ZapfprofilFerienDaten>()).Count(f => f is not null && f.Belegt);
        if (Jahresmesswert.HasValue) n++;
        if (SpeicherverlustKwhJeJahr.HasValue && JahresmesswertBilanzgrenze == ZapfprofilBilanzgrenze.MitSpeicher
            && Jahresmesswert.HasValue) n++;
        if (!TagesbedarfAuto) n++;                    // das Paar auto/manuell zählt einmal
        if (BedarfSpezKwhJeEinheitTag.HasValue) n++;
        if (ZapftemperaturC.HasValue) n++;
        if (KaltwasserMittelC.HasValue) n++;
        if (KaltwasserAmplitudeK.HasValue) n++;
        if (Auslastung is not null) n += Auslastung.Count(a => a.HasValue);
        if (wohnungstabelleWirksam && Wohnungen is { Count: > 0 }) n++;
        return n;
    }

    /// <summary>Eine unabhängige Kopie samt Ferien, Auslastung und Wohnungstabelle.</summary>
    public ZapfprofilZonenangabenDaten Kopie()
    {
        var k = (ZapfprofilZonenangabenDaten)MemberwiseClone();
        k.Ferien = (Ferien ?? NeueFerien()).Select(f => f?.Kopie() ?? new ZapfprofilFerienDaten()).ToList();
        k.Auslastung = (double?[])(Auslastung ?? new double?[MONATE]).Clone();
        k.Wohnungen = (Wohnungen ?? new List<ZapfprofilWohnungDaten>()).Select(w => w.Kopie()).ToList();
        return k;
    }
}

/// <summary>
/// <b>Die gebäudeweiten Größen</b> der Stufen Erweitert und Experte (5.3): Zirkulation,
/// Leitungsinhalt, Ladeleistung samt Ladefenster, Speichertemperatur und Kaltwasser der Auslegung —
/// Größen des Projekts (<c>Tab_TwwProjekt</c>), nicht einer Zone. Jede nullbare Größe heißt
/// <c>null</c> = Vorgabe.
///
/// <para><b>Mit der Überlagerung „Auslegung" geteilt:</b> Ladeleistung, Ladefenster und
/// Speichertemperatur stehen auch in <see cref="ZapfprofilAuslegungEingabeDaten"/>. Der Dialog
/// gleicht sie mit dem OK der Überlagerung an (<see cref="AusAuslegung"/>), die Überlagerung beginnt
/// mit ihnen (<see cref="InAuslegung"/>) — eine Liste der geteilten Größen, hier.</para>
/// </summary>
public sealed class ZapfprofilGebaeudeDaten
{
    public bool ZirkAuto { get; set; } = true;
    public ZapfprofilZirkulationsmethode ZirkMethode { get; set; } = ZapfprofilZirkulationsmethode.Flaechenkennwert;
    public ZapfprofilLeitungslage? ZirkLage { get; set; }
    public double? ZirkLaengeM { get; set; }
    public double? ZirkVerlustWJeM { get; set; }
    public double? ZirkAnteil { get; set; }
    public double? ZirkKennwert { get; set; }
    public double? ZirkFlaecheM2 { get; set; }
    public double? ZirkLaufzeitH { get; set; }
    public double? ZirkManuellKw { get; set; }
    public double? LeitungsinhaltL { get; set; }
    public bool LadeAuto { get; set; } = true;
    public double? LadeManuellKw { get; set; }
    public double? LadefensterH { get; set; }
    public double? LadefensterBeginnH { get; set; }
    public double? SpeicherC { get; set; }
    public double? KaltwasserAuslegungC { get; set; }

    /// <summary>Eine unabhängige Kopie.</summary>
    public ZapfprofilGebaeudeDaten Kopie() => (ZapfprofilGebaeudeDaten)MemberwiseClone();

    /// <summary>
    /// Wie viele Größen von der <paramref name="vorgabe"/> abweichen (die Vorgaben der DDL; ohne
    /// Vorgabe die des DTO) — der Anteil des Gebäudes am Zähler „n Werte überschrieben".
    ///
    /// <para><b>Nur WIRKSAME Abweichungen zählen</b> (Z4, Gruppe 2a Punkt 5): die Paare
    /// „Zirkulation/Ladeleistung auto/manuell" zählen je EINMAL — ein manueller Wert bei „auto"
    /// ist wirkungslos; die Angaben einer Zirkulationsmethode zählen nur, wenn sie auch die
    /// GEWÄHLTE ist und die Zirkulation „auto" rechnet (der Kern liest sie bei manueller
    /// Zirkulation nicht).</para>
    /// </summary>
    public int Ueberschrieben(ZapfprofilGebaeudeDaten? vorgabe)
    {
        ZapfprofilGebaeudeDaten v = vorgabe ?? new ZapfprofilGebaeudeDaten();
        int n = 0;
        // Das Paar Zirkulation auto/manuell zählt einmal — ein manueller Wert bei "auto" nicht.
        if (ZirkAuto != v.ZirkAuto || (!ZirkAuto && ZirkManuellKw != v.ZirkManuellKw)) n++;
        if (ZirkMethode != v.ZirkMethode) n++;
        // Die Angaben einer Methode zählen nur bei "auto" und nur für die GEWÄHLTE Methode.
        bool GewaehlteMethode(ZapfprofilZirkulationsmethode m) => ZirkAuto && ZirkMethode == m;
        if (GewaehlteMethode(ZapfprofilZirkulationsmethode.Flaechenkennwert))
        {
            if (ZirkLage != v.ZirkLage) n++;
            if (ZirkKennwert != v.ZirkKennwert) n++;
            if (ZirkFlaecheM2 != v.ZirkFlaecheM2) n++;
        }
        if (GewaehlteMethode(ZapfprofilZirkulationsmethode.Leitungslaenge))
        {
            if (ZirkLaengeM != v.ZirkLaengeM) n++;
            if (ZirkVerlustWJeM != v.ZirkVerlustWJeM) n++;
        }
        if (GewaehlteMethode(ZapfprofilZirkulationsmethode.Anteil))
            if (ZirkAnteil != v.ZirkAnteil) n++;
        if (ZirkLaufzeitH != v.ZirkLaufzeitH) n++;
        if (LeitungsinhaltL != v.LeitungsinhaltL) n++;
        // Das Paar Ladeleistung auto/manuell zählt einmal.
        if (LadeAuto != v.LadeAuto || (!LadeAuto && LadeManuellKw != v.LadeManuellKw)) n++;
        if (LadefensterH != v.LadefensterH) n++;
        if (LadefensterBeginnH != v.LadefensterBeginnH) n++;
        if (SpeicherC != v.SpeicherC) n++;
        if (KaltwasserAuslegungC != v.KaltwasserAuslegungC) n++;
        return n;
    }

    /// <summary>Übernimmt die geteilten Größen aus den Eingaben der Überlagerung „Auslegung" (ihr OK).</summary>
    public void AusAuslegung(ZapfprofilAuslegungEingabeDaten a)
    {
        if (a is null) return;
        LadeAuto = a.LadeAuto;
        LadeManuellKw = a.LadeManuellKw;
        LadefensterH = a.LadefensterH;
        LadefensterBeginnH = a.LadefensterBeginnH;
        SpeicherC = a.SpeicherC;
    }

    /// <summary>Setzt die geteilten Größen in die Eingaben der Überlagerung — sie beginnt mit dem Stand des Dialogs.</summary>
    public void InAuslegung(ZapfprofilAuslegungEingabeDaten a)
    {
        if (a is null) return;
        a.LadeAuto = LadeAuto;
        a.LadeManuellKw = LadeManuellKw;
        a.LadefensterH = LadefensterH;
        a.LadefensterBeginnH = LadefensterBeginnH;
        a.SpeicherC = SpeicherC;
    }
}

/// <summary>
/// Eine Zone (5.3): die vier Felder der Stufe Einfach — Zonenname, Nutzungsart, Bezugsmenge,
/// Bedarfsniveau — und die Angaben der Stufen Erweitert und Experte
/// (<see cref="Angaben"/>). Fehlen die Angaben (<c>null</c>), behält die Hülle die Größen des
/// Arbeitsstands des Kerns (bzw. der Vorlage eines Duplikats) und setzt nur die vier Felder
/// zurück — die Stufe behält Überschreibungen in beiden Fällen.
/// </summary>
public sealed class ZapfprofilZoneDaten
{
    /// <summary>Die Id der Zone (<c>Tab_TwwZone.ID</c>); 0 = neu angelegt.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Die Id der Zone, aus der diese per „Duplizieren" entstand; 0 = keine. Die Hülle übernimmt
    /// von ihr die Größen der höheren Stufen.
    /// </summary>
    public int IdVorlage { get; set; }

    /// <summary>Der Zonenname.</summary>
    public string Name { get; set; } = "";

    /// <summary>Die gewählte Nutzungsart; 0 = noch keine (Pflichtfeld).</summary>
    public int IdNutzungsart { get; set; }

    /// <summary>Die Bezugsmenge in der Bezugsart der Nutzungsart; <c>null</c> = nicht eingegeben (Pflichtfeld, &gt; 0).</summary>
    public double? Bezugsmenge { get; set; }

    /// <summary>Das Bedarfsniveau; Vorgabe mittel.</summary>
    public ZapfprofilNiveau Niveau { get; set; } = ZapfprofilNiveau.Mittel;

    /// <summary>
    /// Wie viele Größen der höheren Stufen diese Zone beim Öffnen überschreibt (nicht auf
    /// Vorgabe) — die Zahl der Hülle; sie gilt, solange die Zone keine <see cref="Angaben"/>
    /// trägt (<see cref="UeberschriebenZahl"/>).
    /// </summary>
    public int Ueberschrieben { get; set; }

    /// <summary>
    /// Die Angaben der Stufen Erweitert und Experte; <c>null</c> = der Arbeitsstand des Kerns
    /// bleibt, wie er ist (die Hülle füllt sie beim Öffnen immer).
    /// </summary>
    public ZapfprofilZonenangabenDaten? Angaben { get; set; }

    /// <summary>
    /// Der Anteil der Zone am Zähler „n Werte überschrieben" — aus den Angaben, sonst die Zahl der
    /// Hülle. <paramref name="wohnungstabelleWirksam"/> gilt nur für die Angaben (Z4, Gruppe 2a
    /// Punkt 5); Vorgabe <c>true</c> — ohne Katalogbezug zählt eine gepflegte Tabelle wie bisher.
    /// </summary>
    public int UeberschriebenZahl(bool wohnungstabelleWirksam = true) => Angaben?.Ueberschrieben(wohnungstabelleWirksam) ?? Ueberschrieben;

    /// <summary>Eine unabhängige Kopie (der Dialog arbeitet bis OK auf Kopien) samt Angaben.</summary>
    public ZapfprofilZoneDaten Kopie() => new()
    {
        Id = Id,
        IdVorlage = IdVorlage,
        Name = Name,
        IdNutzungsart = IdNutzungsart,
        Bezugsmenge = Bezugsmenge,
        Niveau = Niveau,
        Ueberschrieben = Ueberschrieben,
        Angaben = Angaben?.Kopie()
    };
}

/// <summary>
/// Der ARBEITSSTAND des Dialogs als DTO: Weg und Zonen in ihrer Reihenfolge. Er geht in den
/// Vorschau-Delegaten der Hülle und mit OK als <see cref="ZapfprofilErgebnisDaten"/> zurück.
/// </summary>
public sealed class ZapfprofilEingabeDaten
{
    /// <summary>Der Weg; das OK des Zapfprofils stellt ihn auf <see cref="ZapfprofilWeg.Generator"/> (ZU4).</summary>
    public ZapfprofilWeg Weg { get; set; } = ZapfprofilWeg.Generator;

    /// <summary>Die Zonen in ihrer Reihenfolge.</summary>
    public List<ZapfprofilZoneDaten> Zonen { get; set; } = new();

    /// <summary>
    /// Die mit OK der Überlagerung „Auslegung" übernommenen Eingaben samt Punkt (Stufe Z2);
    /// <c>null</c> = die Auslegung wurde nicht berührt — die Projektgrößen bleiben, wie sie sind.
    /// </summary>
    public ZapfprofilAuslegungEingabeDaten? Auslegung { get; set; }

    /// <summary>
    /// Haben sich die Zonen geändert, nachdem ein Auslegungspunkt übernommen war (mit OK der
    /// Überlagerung oder schon im Stand beim Öffnen)? Dann ist der Punkt überholt: Das Speichern
    /// verwirft ihn; ein neues OK der Überlagerung setzt die Marke zurück.
    /// </summary>
    public bool PunktUeberholt { get; set; }

    /// <summary>
    /// Der Rechenweg der Jahresreihe (Stufe Experte, 5.3; <c>Tab_TwwProjekt.Jahresreihe_Stochastisch</c>):
    /// <c>true</c> = in die Bilanz geht das gezogene Jahr zum Seed, auf die Jahresmenge gebracht
    /// (4.4). Eine Größe des Projekts, nicht einer Zone; die Vorschau bleibt deterministisch (5.1),
    /// das Jahr zum Seed zieht erst der Lauf.
    /// </summary>
    public bool JahresreiheStochastisch { get; set; }

    /// <summary>
    /// <b>Rechnet der Jahresgang über die eingespielten Typtage?</b> (Stufe Experte, 4.2;
    /// <c>Tab_TwwProjekt.Typtage_Aktiv</c>, Stufe Z4b) <c>false</c> = der Formvektor wie im Bestand
    /// (Monats-, Wochen- und Tagesfaktoren des Katalogs). Eine Größe des PROJEKTS, nicht einer
    /// Zone. Ohne eingespielte Typtage ist der Schalter gesperrt.
    /// </summary>
    public bool TyptageAktiv { get; set; }

    /// <summary>
    /// Die gewählte Klimazone des eingespielten Pakets (<c>Tab_TwwProjekt.Typtage_Klimazone</c>);
    /// <c>null</c> = keine Wahl — dann lehnt der Rechenweg die Zonen benannt ab.
    /// </summary>
    public int? TyptageKlimazone { get; set; }

    /// <summary>
    /// Die gewählte Gebäudeart des eingespielten Pakets
    /// (<c>Tab_TwwProjekt.Typtage_Gebaeudeart</c>); leer = keine Wahl.
    /// </summary>
    public string TyptageGebaeudeart { get; set; } = "";

    /// <summary>Der Seed des Zufalls (ganze Zahl ≥ 0, <c>Tab_TwwProjekt.Seed</c>); <c>null</c> = der Stand bleibt, wie er ist.</summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Die Realisierungen der Jahresreihe — die R Jahre der Konsistenzprobe
    /// (<c>Tab_TwwProjekt.Realisierungen</c>); <c>null</c> = der Stand bleibt, wie er ist.
    /// </summary>
    public int? Realisierungen { get; set; }

    /// <summary>
    /// Die Temperatur der Literanzeige θ_Anzeige [°C] als Laufangabe (4.0, N9 (h); Stufe Z4) —
    /// nicht gespeichert; <c>null</c> = die Einstellung <c>Zapfprofil.Anzeigetemperatur</c>, sonst keine Literanzeige.
    /// </summary>
    public double? AnzeigetemperaturC { get; set; }

    /// <summary>
    /// Die Schwelle der Stundenzählung [kW] als Laufangabe (4.6; Stufe Z4) — nicht gespeichert;
    /// <c>null</c> = die Einstellung <c>Zapfprofil.Stundenschwelle</c>, sonst keine Zählung.
    /// </summary>
    public double? SchwelleKw { get; set; }

    /// <summary>
    /// Die gebäudeweiten Größen der Stufen Erweitert und Experte (Zirkulation, Leitungsinhalt,
    /// Ladeleistung, Speichertemperatur, Kaltwasser der Auslegung); <c>null</c> = die Projektzeile
    /// des Arbeitsstands bleibt, wie sie ist.
    /// </summary>
    public ZapfprofilGebaeudeDaten? Gebaeude { get; set; }

    /// <summary>
    /// Die Stufe des Dialogs als Laufangabe (nicht gespeichert): Sie geht mit dem Öffnen der
    /// Überlagerung „Auslegung" in den Auslegungslauf — die Stufe Einfach trägt die Marke
    /// „Schnellauslegung" (N11 (c)).
    /// </summary>
    public ZapfprofilStufe Stufe { get; set; } = ZapfprofilStufe.Einfach;

    /// <summary>Eine unabhängige Kopie samt Zonen, Auslegung und Gebäude.</summary>
    public ZapfprofilEingabeDaten Kopie() => new()
    {
        Weg = Weg,
        Zonen = Zonen.Select(z => z.Kopie()).ToList(),
        Auslegung = Auslegung?.Kopie(),
        PunktUeberholt = PunktUeberholt,
        JahresreiheStochastisch = JahresreiheStochastisch,
        TyptageAktiv = TyptageAktiv,
        TyptageKlimazone = TyptageKlimazone,
        TyptageGebaeudeart = TyptageGebaeudeart,
        Seed = Seed,
        Realisierungen = Realisierungen,
        AnzeigetemperaturC = AnzeigetemperaturC,
        SchwelleKw = SchwelleKw,
        Gebaeude = Gebaeude?.Kopie(),
        Stufe = Stufe
    };
}

/// <summary>
/// Die Kennzahlen der Bilanz (Konzept 4.6) in ihrer Quelleneinheit — die Einheit steht im
/// Namen, umgerechnet wird erst an der Anzeige (<c>Energieeinheit</c>). Ein Wert, den es für
/// eine Ansicht nicht gibt, ist <c>null</c>, nicht 0.
/// </summary>
public sealed class ZapfprofilKennzahlenDaten
{
    /// <summary>Jahresbedarf der Zapfung [kWh/a].</summary>
    public double JahresbedarfZapfungKwh { get; set; }

    /// <summary>Jahresverlust der Zirkulation [kWh/a].</summary>
    public double JahresverlustZirkulationKwh { get; set; }

    /// <summary>Zapfung und Zirkulation zusammen [kWh/a].</summary>
    public double JahresbedarfGesamtKwh { get; set; }

    /// <summary>Anteil der Zirkulation an Zapfung + Zirkulation [-].</summary>
    public double Zirkulationsanteil { get; set; }

    /// <summary>Tagesmittel der Zapfung [kWh/d].</summary>
    public double TagesmittelZapfungKwh { get; set; }

    /// <summary>Tagesmittel der Zapfung in Litern; <c>null</c> ohne Anzeigetemperatur.</summary>
    public double? ZapfungLiterJeTag { get; set; }

    /// <summary>Spezifischer Jahreswert [kWh je Einheit und Jahr]; nur in der Ansicht einer Zone.</summary>
    public double? SpezifischKwhJeEinheitJahr { get; set; }

    /// <summary>Die Einheit des spezifischen Werts („WE"); leer ohne ihn.</summary>
    public string SpezifischEinheit { get; set; } = "";

    /// <summary>Größter Stundenwert von Zapfung + Zirkulation [kW]; <c>null</c>, wo der Kern ihn nicht ausweist.</summary>
    public double? GroessterStundenwertKw { get; set; }

    /// <summary>Der Vermerk am größten Stundenwert („Bilanzwert, keine Auslegungsgröße").</summary>
    public string VermerkGroessterStundenwert { get; set; } = "";

    /// <summary>Volllaststunden [h/a]; <c>null</c>, wo der Kern sie nicht ausweist.</summary>
    public double? VolllaststundenH { get; set; }

    /// <summary>Stunden über der Schwelle; <c>null</c> ohne Schwelle.</summary>
    public int? StundenUeberSchwelle { get; set; }

    /// <summary>Die Schwelle der Zählung [kW]; <c>null</c> ohne Schwelle.</summary>
    public double? SchwelleKw { get; set; }
}

/// <summary>
/// Die Vorschau für EINE Auswahl „Anzeigen für" — die Summe aller Zonen oder eine Zone:
/// Tagesgang je Tagtyp im größten Monat, die Woche mit dem größten Tagesbedarf, zwölf
/// Monatswerte von Zapfung und Zirkulation, die Kennzahlen und die fertigen Zeichenmodelle.
/// Leistungen in kW (kWh je Stunde), Energien in kWh.
/// </summary>
public sealed class ZapfprofilAnsichtDaten
{
    /// <summary>Die Zone der Ansicht; 0 = Summe aller Zonen.</summary>
    public int IdZone { get; set; }

    /// <summary>Der Eintrag in „Anzeigen für".</summary>
    public string Titel { get; set; } = "";

    /// <summary>Trägt die Zone 0, weil der Rechenweg sie abgelehnt hat?</summary>
    public bool Abgelehnt { get; set; }

    /// <summary>Der Monat 1 … 12 des Tagesgangs (der größte der Ansicht).</summary>
    public int Monat { get; set; } = 1;

    /// <summary>24 Stundenmittel der Werktage [kW]; <c>null</c> ohne Werktag im Monat.</summary>
    public double[]? WerktagKw { get; set; }

    /// <summary>24 Stundenmittel der Samstage [kW]; <c>null</c> ohne Samstag im Monat.</summary>
    public double[]? SamstagKw { get; set; }

    /// <summary>24 Stundenmittel der Sonn- und Feiertage [kW]; <c>null</c> ohne solchen Tag.</summary>
    public double[]? SonnFeiertagKw { get; set; }

    /// <summary>Tage je Tagtyp im Mittel (Werktag, Samstag, Sonn-/Feiertag).</summary>
    public int[] TageJeTagtyp { get; set; } = new int[3];

    /// <summary>24 Stundenmittel der Zirkulation im Monat [kW].</summary>
    public double[] ZirkulationTagKw { get; set; } = new double[24];

    /// <summary>Der Jahrestag, mit dem die Woche beginnt.</summary>
    public int WochenStarttag { get; set; } = 1;

    /// <summary>168 Stundenwerte der Zapfung in der Woche mit dem größten Tagesbedarf [kW].</summary>
    public double[] WocheZapfungKw { get; set; } = new double[168];

    /// <summary>168 Stundenwerte der Zirkulation in derselben Woche [kW].</summary>
    public double[] WocheZirkulationKw { get; set; } = new double[168];

    /// <summary>Zwölf Monatssummen der Zapfung [kWh].</summary>
    public double[] MonateZapfungKwh { get; set; } = new double[12];

    /// <summary>Zwölf Monatssummen der Zirkulation [kWh].</summary>
    public double[] MonateZirkulationKwh { get; set; } = new double[12];

    /// <summary>Die Kennzahlen der Ansicht.</summary>
    public ZapfprofilKennzahlenDaten Kennzahlen { get; set; } = new();

    /// <summary>Das Bild „Tagesgang" (Zeichenmodell des Kerns für <c>DiagrammSvg</c>).</summary>
    public Zeichenmodell? TagesgangModell { get; set; }

    /// <summary>Das Bild „Wochenprofil".</summary>
    public Zeichenmodell? WochenprofilModell { get; set; }

    /// <summary>Das Bild „Jahresgang" (MWh).</summary>
    public Zeichenmodell? JahresgangModell { get; set; }

    /// <summary>Die Bildunterschrift des Tagesgangs.</summary>
    public string UnterschriftTagesgang { get; set; } = "";

    /// <summary>Die Bildunterschrift des Wochenprofils.</summary>
    public string UnterschriftWoche { get; set; } = "";

    /// <summary>Die Bildunterschrift des Jahresgangs.</summary>
    public string UnterschriftJahresgang { get; set; } = "";

    /// <summary>
    /// Die Konsistenzproben der stochastischen Jahresreihe (4.4) — in der Summe je gerechneter
    /// Zone eine, in der Ansicht einer Zone ihre eigene; leer auf dem deterministischen Weg und bei
    /// einer abgelehnten Zone.
    /// </summary>
    public List<ZapfprofilKonsistenzDaten> Konsistenzen { get; set; } = new();

    /// <summary>Die Schätzhilfe des Tagesbedarfs (Stufe Erweitert, 5.3) — nur in der Ansicht einer gerechneten Zone.</summary>
    public ZapfprofilSchaetzhilfeDaten? Tagesbedarf { get; set; }

    /// <summary>Die Auslastung nach Monat, Wochentag und Tagesstunde (Mittel 1) — nur in der Ansicht einer gerechneten Zone.</summary>
    public ZapfprofilAuslastungDaten? Auslastung { get; set; }

    /// <summary>Der Auslastungsgang der Zone (Experte): wirksam, Katalog, überschrieben — nur in der Ansicht einer Zone.</summary>
    public ZapfprofilAuslastungsgangDaten? Auslastungsgang { get; set; }

    /// <summary>Die Dauerlinie der Ansicht (Reiter „Dauerlinie", Stufe Z4); <c>null</c> bei einer abgelehnten Zone.</summary>
    public ZapfprofilDauerlinieDaten? Dauerlinie { get; set; }
}

/// <summary>
/// Eine Schätzhilfe (5.3, Mockup „auto/manuell"): Vorschlag des Verfahrens (<c>null</c> = keiner),
/// manueller Wert, angesetzter Wert, Einheit und der Rechenweg als Satz der Oberflächensprache.
/// Welcher Wert gilt, sagt <see cref="Auto"/>; die Logik hängt an den Zahlen, nie am Text.
/// </summary>
public sealed class ZapfprofilSchaetzhilfeDaten
{
    public bool Auto { get; set; } = true;
    public double? Vorschlag { get; set; }
    public double? Manuell { get; set; }
    public double Angesetzt { get; set; }

    /// <summary>Setzt ein Jahresmesswert den Wert an? Dann ist <see cref="Angesetzt"/> der kalibrierte Wert (die Zeile heißt nicht „angesetzt").</summary>
    public bool Kalibriert { get; set; }
    public string Einheit { get; set; } = "";
    public string Rechenweg { get; set; } = "";
}

/// <summary>Die Auslastung einer Zone (Mittel 1, dimensionslos): zwölf Monate, sieben Wochentage (Montag zuerst), 24 Stunden.</summary>
public sealed class ZapfprofilAuslastungDaten
{
    public double[] Monate { get; set; } = new double[12];
    public double[] Wochentage { get; set; } = new double[7];
    public double[] Stunden { get; set; } = new double[24];
}

/// <summary>Der Auslastungsgang einer Zone (Experte): je Monat der wirksame Faktor, der des Katalogs und ob die Zone ihn überschreibt.</summary>
public sealed class ZapfprofilAuslastungsgangDaten
{
    public double[] Wirksam { get; set; } = new double[12];
    public double[] Katalog { get; set; } = new double[12];
    public bool[] Ueberschrieben { get; set; } = new bool[12];
}

/// <summary>Eine Perzentilmarke der Dauerlinie: P_p, ihr Wert [kW] und ihr Rang auf der absteigenden Linie (1 = größte Stunde).</summary>
public sealed record ZapfprofilDauerlinienmarkeDaten(int Perzentil, double LeistungKw, int Rang);

/// <summary>
/// Die Dauerlinie einer Ansicht (Stufe Z4): die geordneten Stundenwerte von Zapfung und Zirkulation
/// [kW], die Perzentilmarken, die Stunden über der Schwelle und das Bild.
/// </summary>
public sealed class ZapfprofilDauerlinieDaten
{
    public double[] GesamtKw { get; set; } = new double[0];
    public List<ZapfprofilDauerlinienmarkeDaten> Marken { get; set; } = new();
    public double? SchwelleKw { get; set; }
    public int? StundenUeberSchwelle { get; set; }
    public Zeichenmodell? Modell { get; set; }
}

/// <summary>
/// Die KONSISTENZPROBE der stochastischen Jahresreihe einer Zone (4.4), wie der Kern sie
/// ausweist: Jahresenergie deterministisch und im Mittel der R gezogenen Jahre, deren Streuung
/// s_R, die Toleranz <c>max(1 %, 3 · s_R / √R)</c>, erfüllt ja/nein und der Faktor, der das Jahr
/// zum Seed auf die Jahresmenge bringt. Zahlen in ihrer Quelleneinheit; der Dialog formatiert.
/// </summary>
public sealed class ZapfprofilKonsistenzDaten
{
    /// <summary>Die Zone.</summary>
    public string Zone { get; set; } = "";

    /// <summary>Die Position der Zone in der Eingabe (0-basiert).</summary>
    public int Position { get; set; }

    /// <summary>Die Jahresenergie des deterministischen Pfads [kWh/a].</summary>
    public double DeterministischKwh { get; set; }

    /// <summary>Das Mittel der Jahresenergie über die R gezogenen Jahre [kWh/a].</summary>
    public double MittelKwh { get; set; }

    /// <summary>Die Standardabweichung s_R der Jahresenergie über die R Jahre [kWh/a].</summary>
    public double StandardabweichungKwh { get; set; }

    /// <summary>Die Zahl der gezogenen Jahre R.</summary>
    public int Realisierungen { get; set; }

    /// <summary>Die Toleranz der Probe [kWh/a].</summary>
    public double ToleranzKwh { get; set; }

    /// <summary>Liegt das Mittel innerhalb der Toleranz?</summary>
    public bool Erfuellt { get; set; }

    /// <summary>Der Faktor der Energieprobe, der das Jahr zum Seed auf die Jahresmenge bringt [-].</summary>
    public double Faktor { get; set; }

    /// <summary>
    /// Die relative Abweichung des Mittels vom deterministischen Pfad [-], wie der Kern sie
    /// rechnet (<c>Jahreskonsistenz.Abweichung</c>); <c>null</c> ohne Jahresmenge.
    /// </summary>
    public double? Abweichung { get; set; }
}

/// <summary>Der Jahreswert einer Zone für die Spalte „MWh/a" der Zonenliste.</summary>
public sealed class ZapfprofilZonenwertDaten
{
    /// <summary>Die Id der Zone; 0 bei einer noch ungespeicherten.</summary>
    public int IdZone { get; set; }

    /// <summary>Die Position der Zone in der Eingabe (0-basiert) — der Schlüssel auch für neue Zonen.</summary>
    public int Position { get; set; }

    /// <summary>Der Zonenname.</summary>
    public string Zone { get; set; } = "";

    /// <summary>Jahresbedarf der Zapfung [kWh/a].</summary>
    public double JahresbedarfZapfungKwh { get; set; }

    /// <summary>Trägt die Zone 0 (Ablehnung)?</summary>
    public bool Abgelehnt { get; set; }

    /// <summary>
    /// Die wirksame Bezugsmenge des Kerns in der Bezugsart der Nutzungsart — bei einer
    /// Wohnungstabelle die Menge aus ihr; <c>null</c> bei einer abgelehnten Zone.
    /// </summary>
    public double? BezugsmengeWirksam { get; set; }

    /// <summary>Der Anteil der Zone an der Zapfung aller Zonen [-]; <c>null</c> ohne Zapfung.</summary>
    public double? Anteil { get; set; }

    /// <summary>Wie die Jahresmenge der Zone entsteht (Spalte „Rechenweg").</summary>
    public ZapfprofilZonenrechenweg Rechenweg { get; set; }
}

/// <summary>
/// Das Ergebnis des Vorschau-Delegaten: Zustand, Grund, die Ansichten (die Summe zuerst), die
/// Jahreswerte je Zone, die Meldungen und der Statustext für den Füller der Fußleiste.
/// </summary>
public sealed class ZapfprofilVorschauDaten
{
    /// <summary>Der Zustand der Vorschau.</summary>
    public ZapfprofilVorschauZustand Zustand { get; set; }

    /// <summary>Warum es keine Vorschau gibt; leer bei <see cref="ZapfprofilVorschauZustand.Gerechnet"/>.</summary>
    public string Grund { get; set; } = "";

    /// <summary>Die Ansichten „Anzeigen für": die Summe aller Zonen zuerst, dann je Zone.</summary>
    public List<ZapfprofilAnsichtDaten> Ansichten { get; set; } = new();

    /// <summary>Der Jahreswert je Zone in der Reihenfolge der Eingabe.</summary>
    public List<ZapfprofilZonenwertDaten> Zonen { get; set; } = new();

    /// <summary>Hinweise und Ablehnungen des Rechenwegs.</summary>
    public List<ZapfprofilMeldung> Meldungen { get; set; } = new();

    /// <summary>
    /// Die Warnliste der Bilanz (Warnlogik, Stufe Z4): je Hinweis des Rechenwegs Titel
    /// (<c>ZPG_WARN_…</c>), Satz und Stufe (Warnung oder Hinweis), in der Reihenfolge des Kerns.
    /// </summary>
    public List<ZapfprofilWarnDaten> Warnliste { get; set; } = new();

    /// <summary>
    /// <b>Das Herkunftsprotokoll des Laufs</b> (Karte „Herkunft", N19): je Wert eine Zeile in der
    /// Reihenfolge, in der der Rechenweg ihn festlegt — fertige Texte der Oberflächensprache. Die
    /// Liste ist immer gefüllt; ob die Karte steht, entscheidet die Stufe des Dialogs.
    /// </summary>
    public List<ZapfprofilHerkunftZeile> Herkunft { get; set; } = new();

    /// <summary>Die Schätzhilfe der Zirkulation (Stufe Erweitert, 5.3); <c>null</c>, wenn die Zirkulation abgelehnt ist.</summary>
    public ZapfprofilSchaetzhilfeDaten? Zirkulation { get; set; }

    /// <summary>Der Statustext der Fußleiste.</summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// Ist die Jahresreihe stochastisch gerechnet (Rechenweg „stochastisch", 4.4)? Nur das Ergebnis
    /// des Laufs „Stochastisch rechnen" (Delegat <c>Jahresreihe</c>) — die Vorschau bleibt
    /// deterministisch (5.1).
    /// </summary>
    public bool Stochastisch { get; set; }

    /// <summary>Der Seed des Laufs — nur bei <see cref="Stochastisch"/>, sonst <c>null</c>.</summary>
    public int? Seed { get; set; }

    /// <summary>Die Zahl der gezogenen Jahre — nur bei <see cref="Stochastisch"/>, sonst <c>null</c>.</summary>
    public int? Realisierungen { get; set; }

    /// <summary>Die Ansicht der Summe; <c>null</c> ohne Rechnung.</summary>
    public ZapfprofilAnsichtDaten? Summe => Ansichten.Count > 0 ? Ansichten[0] : null;
}

/// <summary>Die Kontextzeile des Dialogs (5.1): Projekt, Klimaregion, Kalender, Bilanzgrenze — fertige Texte.</summary>
public sealed class ZapfprofilKontextDaten
{
    public string Projekt { get; set; } = "";
    public string Klimaregion { get; set; } = "";
    public string Kalender { get; set; } = "";
    public string Bilanzgrenze { get; set; } = "";
}

/// <summary>
/// <b>Der Stand des Dialogs</b> beim Öffnen: Projekt und Kontext, der Katalog der
/// Nutzungsarten, der Arbeitsstand, die Stufe, die erste Vorschau und — wenn der Generator
/// nicht verfügbar ist — der benannte Grund.
/// </summary>
public sealed class ZapfprofilDaten
{
    /// <summary>Das Projekt (<c>Tab_Projekt.ID</c>).</summary>
    public int IdProjekt { get; set; }

    /// <summary>Die Kontextzeile.</summary>
    public ZapfprofilKontextDaten Kontext { get; set; } = new();

    /// <summary>Die wählbaren und gesperrten Nutzungsarten, geordnet nach Name und Katalogversion.</summary>
    public List<ZapfprofilNutzungsartDaten> Katalog { get; set; } = new();

    /// <summary>Der Arbeitsstand, mit dem der Dialog öffnet.</summary>
    public ZapfprofilEingabeDaten Eingabe { get; set; } = new();

    /// <summary>Trägt der Stand beim Öffnen einen Auslegungspunkt (Projektgrößen)? Eine Zonenänderung macht ihn überholt.</summary>
    public bool MitPunkt { get; set; }

    /// <summary>Die Stufe beim Öffnen.</summary>
    public ZapfprofilStufe Stufe { get; set; } = ZapfprofilStufe.Einfach;

    /// <summary>Die Vorschau zum Arbeitsstand beim Öffnen; <c>null</c>, wenn nicht gerechnet wurde.</summary>
    public ZapfprofilVorschauDaten? Vorschau { get; set; }

    /// <summary>Kann der Generator hier rechnen? Sonst nennt <see cref="Sperrgrund"/> den Grund.</summary>
    public bool Verfuegbar { get; set; }

    /// <summary>Der benannte Grund, warum der Generator nicht verfügbar ist; leer, wenn er es ist.</summary>
    public string Sperrgrund { get; set; } = "";

    /// <summary>Die Vorgabe des Seeds (DDL von <c>Tab_TwwProjekt.Seed</c>); <c>null</c> = unbekannt.</summary>
    public int? SeedVorgabe { get; set; }

    /// <summary>Die Vorgabe der Realisierungen der Jahresreihe (DDL); <c>null</c> = unbekannt.</summary>
    public int? RealisierungenVorgabe { get; set; }

    /// <summary>Die Untergrenze der Realisierungen (Schema); <c>null</c> = keine bekannt.</summary>
    public int? RealisierungenMindestens { get; set; }

    /// <summary>Die Obergrenze der Realisierungen der Jahresreihe (Kern); <c>null</c> = keine bekannt.</summary>
    public int? RealisierungenHoechstens { get; set; }

    // ---- Stufen Erweitert und Experte (Z4) ------------------------------------------

    /// <summary>Die gebäudeweiten Größen einer Projektzeile ohne Eingabe (DDL); <c>null</c> = unbekannt.</summary>
    public ZapfprofilGebaeudeDaten? GebaeudeVorgabe { get; set; }

    /// <summary>Die Vorgaben des Parametersatzes für die Platzhalter der höheren Stufen.</summary>
    public ZapfprofilVorgabenDaten Vorgaben { get; set; } = new();

    /// <summary>Die Tagesgangsätze des Katalogs (Experte); ein unvollständiger ist gesperrt mit Grund.</summary>
    public List<ZapfprofilKatalogeintragDaten> Tagesgangsaetze { get; set; } = new();

    /// <summary>Die Ausstattungsklassen der Wohnungstabelle — neutraler Name, nie ein Tabellenwert.</summary>
    public List<ZapfprofilKatalogeintragDaten> Ausstattungen { get; set; } = new();

    /// <summary>Die Gebäude des Projekts, an die eine Zone ihren Kalender binden kann (A8).</summary>
    public List<ZapfprofilKatalogeintragDaten> Gebaeude { get; set; } = new();

    /// <summary>Die zwölf Monatsnamen in der Oberflächensprache (Auslastungsgang); leer = die Nummern.</summary>
    public List<string> Monatsnamen { get; set; } = new();

    /// <summary>
    /// <b>Der eingespielte Stand der Typtage</b> (Stufe Z4b): Er trägt die Wahllisten der Klimazone
    /// und der Gebäudeart und den Grund, aus dem der Schalter gesperrt ist, solange nichts
    /// eingespielt ist. Nie ein Wert der Richtlinie (Konzept Kapitel 6).
    /// </summary>
    public TwwTyptagStandDaten Typtagstand { get; set; } = new();

    /// <summary>Wie viele Werte der Arbeitsstand beim Öffnen überschreibt (<see cref="UeberschriebenIn"/>).</summary>
    public int Ueberschrieben => UeberschriebenIn(Eingabe);

    /// <summary>
    /// <b>Der Zähler „n Werte überschrieben"</b> — an EINER Stelle: die Größen der höheren Stufen
    /// je Zone (<see cref="ZapfprofilZoneDaten.UeberschriebenZahl"/>, ihre Wohnungstabelle nur
    /// gezählt, wenn die Nutzungsart der Zone sie wirksam führt — <see cref="Katalog"/>,
    /// <see cref="ZapfprofilNutzungsartDaten.Wohnen"/>, Z4 Gruppe 2a Punkt 5), die gebäudeweiten
    /// Größen abseits ihrer Vorgabe und die der Stochastik — der Rechenweg „stochastisch", ein
    /// Seed oder eine Zahl der Realisierungen abseits der Vorgabe. Die Stufe blendet nur aus, die
    /// Überschreibung bleibt.
    /// </summary>
    public int UeberschriebenIn(ZapfprofilEingabeDaten? eingabe)
    {
        if (eingabe is null) return 0;
        int n = eingabe.Zonen.Sum(z => z.UeberschriebenZahl(Katalog.FirstOrDefault(a => a.Id == z.IdNutzungsart)?.Wohnen == true));
        if (eingabe.Gebaeude is { } g) n += g.Ueberschrieben(GebaeudeVorgabe);
        if (eingabe.JahresreiheStochastisch) n++;
        if (eingabe.TyptageAktiv) n++;
        if (eingabe.Seed is int seed && SeedVorgabe is int sv && seed != sv) n++;
        if (eingabe.Realisierungen is int r && RealisierungenVorgabe is int rv && r != rv) n++;
        return n;
    }
}

/// <summary>
/// Die RÜCKGABE des Dialogs an den Bedarfsprofil-Dialog (5.2): der geprüfte Arbeitsstand samt
/// Weg. Geschrieben wird erst mit dem OK des Bedarfsprofil-Dialogs, im selben Vorgang wie die
/// Profilzuordnungen.
/// </summary>
/// <param name="Eingabe">Der Arbeitsstand; der Weg steht darin.</param>
public sealed record ZapfprofilErgebnisDaten(ZapfprofilEingabeDaten Eingabe)
{
    /// <summary>Der Weg des Arbeitsstands.</summary>
    public ZapfprofilWeg Weg => Eingabe.Weg;
}
// =====================================================================================
//  Die Überlagerung „AUSLEGUNG" (Umsetzungskonzept Zapfprofilgenerator 4.5, 4.7, 5.1;
//  Stufe Z2, Gruppe 2). Nur Daten: Die Hülle übersetzt das Ergebnis des Kerns
//  (Auslegungsergebnis) in diese Typen und die Eingaben zurück in die Projektgrößen; der
//  Dialog rechnet nichts nach.
// =====================================================================================

/// <summary>Die Quelle des Bedarfstags; die Zahlen des Kerns (1 … 5), 0 = Vorgaberegel.</summary>
public enum ZapfprofilBedarfstagquelle
{
    Vorgaberegel = 0,
    Stundenprofil = 1,
    A100Referenz = 2,
    Din4708Profil = 3,
    Konstruktor = 4,
    Ecodesign = 5
}

/// <summary>Die Erzeugerart am Speicher — die Wahl des Anwenders, gespeichert ab Schritt 124 (N10 (i)).</summary>
public enum ZapfprofilErzeugerart
{
    KeineAngabe = 0,
    Kessel = 1,
    Waermepumpe = 2
}

/// <summary>Der Werkstoff des Übertragers — gespeichert ab Schritt 124 (N10 (i)).</summary>
public enum ZapfprofilWerkstoff
{
    KeineAngabe = 0,
    Stahl = 1,
    Edelstahl = 2
}

/// <summary>Die Speicherart der Summenlinie; die Zahlen des Kerns.</summary>
public enum ZapfprofilSpeicherart
{
    Ladespeicher = 1,
    GemischterSpeicher = 2
}

/// <summary>Der Zustand der Auslegung (Hausregel „kein vorbelegtes DTO als Ergebnis").</summary>
public enum ZapfprofilAuslegungZustand
{
    /// <summary>Noch nicht gerechnet (etwa ohne Zone).</summary>
    NichtGerechnet = 0,

    /// <summary>Gerechnet — auch mit abgelehnten Zonen oder ohne rechenbaren Punkt.</summary>
    Gerechnet = 1,

    /// <summary>Die Auslegung konnte für das Projekt nicht rechnen; der Grund steht daneben.</summary>
    Abgebrochen = 2
}

/// <summary>Der Stand einer Karte der Dreiergruppe (Zahlen des Kerns).</summary>
public enum ZapfprofilKartenstand
{
    Gerechnet = 1,
    NichtGerechnet = 2,
    NichtRechenbar = 3,
    AusserhalbGueltigkeit = 4
}

/// <summary>Der Bezug des Füllstands im Verfahrensvergleich (N10 (k), N11 (d)); die Zahlen sind die des Kerns.</summary>
public enum ZapfprofilFuellstandbezug
{
    /// <summary>Die Vorgabe: Nenninhalt des Punkts, sonst Punkt; ohne Punkt Nenninhalt des Bands, sonst V_max.</summary>
    Vorgabe = 0,
    NenninhaltPunkt = 1,
    Punkt = 2,
    NenninhaltBand = 3,
    BandMax = 4
}

/// <summary>Wie schwer ein Eintrag der Warnliste wiegt — nie blockierend.</summary>
public enum ZapfprofilWarnstufe
{
    Hinweis = 0,
    Warnung = 1
}

/// <summary>Ein Zapfereignis eines Bedarfstags: Beginn [Minute], Dauer [min], Energie [kWh].</summary>
public sealed record ZapfprofilEreignisDaten(int MinuteBeginn, int DauerMin, double EnergieKwh);

/// <summary>
/// Ein Bedarfstag, wie die Auswahl ihn zeigt — eine Katalogzeile oder der konstruierte,
/// noch nicht gespeicherte Entwurf (<see cref="Id"/> 0). Herkunft als Kurztext, nie ein Beleg.
/// </summary>
public sealed class ZapfprofilBedarfstagDaten
{
    /// <summary>Die Id der Katalogzeile; 0 = Entwurf des Konstruktors.</summary>
    public int Id { get; set; }

    /// <summary>Der neutrale Name.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Die Quelle (Art) des Tages.</summary>
    public ZapfprofilBedarfstagquelle Quelle { get; set; }

    /// <summary>Die Herkunft als Kurztext.</summary>
    public string Herkunft { get; set; } = "";

    /// <summary>Die Katalogversion.</summary>
    public string Katalogversion { get; set; } = "";

    /// <summary>Kann die Auslegung den Tag als Katalogtag wählen? Sonst nennt <see cref="Sperrgrund"/> den Grund.</summary>
    public bool Waehlbar { get; set; } = true;

    /// <summary>Warum der Tag nicht wählbar ist.</summary>
    public string Sperrgrund { get; set; } = "";

    /// <summary>Die Ereignisse — nur beim Entwurf gefüllt (er geht mit ihnen zurück).</summary>
    public List<ZapfprofilEreignisDaten> Ereignisse { get; set; } = new();

    /// <summary>
    /// Die Zeilen, aus denen der Konstruktor den Entwurf baute — ein erneutes Öffnen des
    /// Konstruktors beginnt mit ihnen. Leer bei einer Katalogzeile und bei einem Entwurf, den der
    /// Kern zurückgibt (er trägt nur Ereignisse).
    /// </summary>
    public List<ZapfprofilKonstruktorZeileDaten> Konstruktorzeilen { get; set; } = new();

    /// <summary>Die Energie des Tages [kWh].</summary>
    public double TagessummeKwh { get; set; }

    /// <summary>Die größte Minutenleistung [kW].</summary>
    public double MinutenspitzeKw { get; set; }

    /// <summary>
    /// Die Bezugsmenge des Tages in seiner <see cref="Bezugsart"/> (Schritt 124, N10 (j)); <c>null</c> =
    /// ohne Bezug — der Tag gilt, wie er ist, und wird nie skaliert.
    /// </summary>
    public double? Bezugsmenge { get; set; }

    /// <summary>Die Bezugsart der Bezugsmenge als Zahl des Kerns (1 Personen … 7 Fläche); <c>null</c> ohne Bezug.</summary>
    public int? Bezugsart { get; set; }

    /// <summary>Eine unabhängige Kopie samt Ereignissen.</summary>
    public ZapfprofilBedarfstagDaten Kopie() => new()
    {
        Id = Id,
        Bezeichner = Bezeichner,
        Quelle = Quelle,
        Herkunft = Herkunft,
        Katalogversion = Katalogversion,
        Waehlbar = Waehlbar,
        Sperrgrund = Sperrgrund,
        Ereignisse = Ereignisse.ToList(),
        Konstruktorzeilen = Konstruktorzeilen.Select(z => z.Kopie()).ToList(),
        TagessummeKwh = TagessummeKwh,
        MinutenspitzeKw = MinutenspitzeKw,
        Bezugsmenge = Bezugsmenge,
        Bezugsart = Bezugsart
    };
}

/// <summary>
/// Eine Zeile des Konstruktors (A100, NA.5.2.3): Zeitfenster [h], wahlweise eine Zapfregel des
/// Katalogs mit Anzahl der Vorgänge oder ein Volumen [l] bei einer Zapftemperatur [°C].
/// </summary>
public sealed class ZapfprofilKonstruktorZeileDaten
{
    /// <summary>Beginn des Fensters [h, 0 … 24).</summary>
    public double? BeginnH { get; set; }

    /// <summary>Ende des Fensters [h, bis 24].</summary>
    public double? EndeH { get; set; }

    /// <summary>Der Name der Zapfregel; leer = Volumen und Temperatur direkt.</summary>
    public string Regel { get; set; } = "";

    /// <summary>Die Anzahl der Vorgänge der Regel.</summary>
    public double? Anzahl { get; set; }

    /// <summary>Das Volumen [l] ohne Regel.</summary>
    public double? VolumenL { get; set; }

    /// <summary>Die Zapftemperatur [°C] ohne Regel.</summary>
    public double? ZapftemperaturC { get; set; }

    /// <summary>Der Verbraucher („Küche") — neutral.</summary>
    public string Verbraucher { get; set; } = "";

    /// <summary>Eine unabhängige Kopie.</summary>
    public ZapfprofilKonstruktorZeileDaten Kopie() => new()
    {
        BeginnH = BeginnH,
        EndeH = EndeH,
        Regel = Regel,
        Anzahl = Anzahl,
        VolumenL = VolumenL,
        ZapftemperaturC = ZapftemperaturC,
        Verbraucher = Verbraucher
    };
}

/// <summary>Eine Zapfregel des Konstruktors aus dem Katalog: Volumenstrom, Dauer, Zapftemperatur.</summary>
public sealed record ZapfprofilRegelDaten(string Name, double VolumenstromLJeMin, double DauerMin, double ZapftemperaturC)
{
    /// <summary>Volumen eines Vorgangs [l].</summary>
    public double VolumenJeVorgangL => VolumenstromLJeMin * DauerMin;
}

/// <summary>Das Ergebnis des Konstruktors: der Entwurf oder die benannte Ablehnung.</summary>
/// <param name="Tag">Der konstruierte Tag; <c>null</c> bei Ablehnung.</param>
/// <param name="Meldungen">Die Ablehnungen (leer bei Erfolg).</param>
public sealed record ZapfprofilKonstruktorErgebnis(ZapfprofilBedarfstagDaten? Tag, IReadOnlyList<ZapfprofilMeldung> Meldungen);

/// <summary>
/// Die EINGABEN der Überlagerung „Auslegung" — Teil des Arbeitsstands: die Quelle des
/// Bedarfstags samt Katalogtag oder Entwurf, Speichertemperatur, Erzeuger- und
/// Übertragerleistung, Speicherart, Sensorhöhe, die Laufangaben Erzeugerart und Werkstoff und
/// der mit OK übernommene Punkt. Jede nullbare Größe heißt: Vorgabe.
/// </summary>
public sealed class ZapfprofilAuslegungEingabeDaten
{
    /// <summary>Die Ladeleistung auto (Schätzhilfe) oder manuell (<c>Tab_TwwProjekt.Lade_Auto</c>, 4.7; Stufe Z4).</summary>
    public bool LadeAuto { get; set; } = true;

    /// <summary>Die manuelle Ladeleistung [kW]; wirkt nur bei <see cref="LadeAuto"/> = false.</summary>
    public double? LadeManuellKw { get; set; }

    /// <summary>Das Ladefenster [h/d] und sein Beginn [h]; <c>null</c> = Vorgabe des Katalogs.</summary>
    public double? LadefensterH { get; set; }
    public double? LadefensterBeginnH { get; set; }

    /// <summary>Nutzanteil und Zuschlag des Verfahrensvergleichs [-]; <c>null</c> = Vorgabe.</summary>
    public double? Nutzanteil { get; set; }
    public double? Zuschlag { get; set; }

    /// <summary>Die Personen des Vergleichs auto (Mengengerüst) oder manuell (<c>Tab_TwwProjekt.Personen_Auto</c>, <c>Personen_Manuell</c>).</summary>
    public bool PersonenAuto { get; set; } = true;
    public double? PersonenManuell { get; set; }

    /// <summary>Der Bezug des Füllstands (N10 (k), N11 (d); <c>Tab_TwwProjekt.Fuellstand_Bezug</c>).</summary>
    public ZapfprofilFuellstandbezug FuellstandBezug { get; set; }

    /// <summary>Die Quelle des Bedarfstags; <see cref="ZapfprofilBedarfstagquelle.Vorgaberegel"/> = Vorgaberegel.</summary>
    public ZapfprofilBedarfstagquelle Quelle { get; set; }

    /// <summary>Der gewählte Katalogtag (Quellen 2 … 5); <c>null</c> = keiner.</summary>
    public int? IdBedarfstag { get; set; }

    /// <summary>Der konstruierte, noch nicht gespeicherte Tag (Quelle Konstruktor ohne Id).</summary>
    public ZapfprofilBedarfstagDaten? Entwurf { get; set; }

    /// <summary>
    /// Die Zeilen des Bedarfstag-Konstruktors (Schemaschritt T5, Anwenderentscheid ZU25) — die
    /// Eingaben, aus denen der Tag entstand. Der Konstruktor öffnet mit ihnen, auch wenn der Tag
    /// schon gespeichert ist und es deshalb keinen <see cref="Entwurf"/> mehr gibt. Leer = der
    /// Konstruktor beginnt mit einer Zeile wie beim ersten Mal.
    /// </summary>
    public List<ZapfprofilKonstruktorZeileDaten> Konstruktorzeilen { get; set; } = new();

    /// <summary>
    /// Die Bezugsart eines <b>gespeicherten</b> Konstruktortags (Zahl des Kerns; aus seiner
    /// Katalogzeile) — der wieder geöffnete Konstruktor beginnt mit ihr, wenn es keinen
    /// <see cref="Entwurf"/> gibt. <c>null</c> = ohne Bezug.
    /// </summary>
    public int? KonstruktorBezugsart { get; set; }

    /// <summary>Die Bezugsmenge des gespeicherten Konstruktortags; <c>null</c> = ohne Bezug.</summary>
    public double? KonstruktorBezugsmenge { get; set; }

    /// <summary>
    /// Der benannte Hinweis, wenn der gespeicherte Konstruktortag keinen vollständigen Bezug trägt
    /// (ein Datensatz vor Schritt 124, ein Tag ohne Bezug) oder nicht mehr im Katalog steht; leer
    /// = keiner. Der Konstruktor zeigt ihn, statt still „ohne Bezug" zu beginnen.
    /// </summary>
    public string KonstruktorBezugHinweis { get; set; } = "";

    /// <summary>Speichertemperatur [°C]; <c>null</c> = Vorgabe.</summary>
    public double? SpeicherC { get; set; }

    /// <summary>Erzeugerleistung [kW]; <c>null</c> = angesetzte Ladeleistung.</summary>
    public double? ErzeugerKw { get; set; }

    /// <summary>Wärmeübertragerleistung [kW]; <c>null</c> = aus U·A, Fläche oder Schätzformel.</summary>
    public double? UebertragerKw { get; set; }

    /// <summary>Die Speicherart.</summary>
    public ZapfprofilSpeicherart Speicherart { get; set; } = ZapfprofilSpeicherart.Ladespeicher;

    /// <summary>Sensorhöhe h_sensor/h_sto [-]; <c>null</c> = Vorgabe.</summary>
    public double? SensorhoeheAnteil { get; set; }

    /// <summary>Die Erzeugerart am Speicher (<c>Tab_TwwProjekt.Erzeugerart</c>); <see cref="ZapfprofilErzeugerart.KeineAngabe"/> = Vorschlag des Anlagenbestands.</summary>
    public ZapfprofilErzeugerart Erzeugerart { get; set; }

    /// <summary>Der Werkstoff des Übertragers (<c>Tab_TwwProjekt.Uebertrager_Werkstoff</c>).</summary>
    public ZapfprofilWerkstoff Werkstoff { get; set; }

    /// <summary>Das Volumen des übernommenen Punkts [l]; <c>null</c> = keiner.</summary>
    public double? PunktVolumenL { get; set; }

    /// <summary>Die Leistung des übernommenen Punkts [kW]; <c>null</c> = keiner.</summary>
    public double? PunktLeistungKw { get; set; }

    /// <summary>
    /// „Stochastisch rechnen" (4.5 b): zieht je Topologiegruppe das Ensemble des Bedarfstags für
    /// Perzentil, Streuband und Gleichzeitigkeit — eine Laufangabe, nicht gespeichert.
    /// </summary>
    public bool Stochastisch { get; set; }

    /// <summary>Das Auslegungsperzentil (95 oder 99, K3; <c>Tab_TwwProjekt.Perzentil</c>); <c>null</c> = Vorgabe.</summary>
    public int? Perzentil { get; set; }

    /// <summary>
    /// Die Realisierungen des Bedarfstags (<c>Tab_TwwProjekt.Realisierungen_Auslegung</c>);
    /// <c>null</c> = Vorgabe ⌈Vielfaches · 1/(1 − p)⌉ aus dem Parametersatz (4.4).
    /// </summary>
    public int? RealisierungenAuslegung { get; set; }

    /// <summary>Eine unabhängige Kopie samt Entwurf.</summary>
    public ZapfprofilAuslegungEingabeDaten Kopie() => new()
    {
        Stochastisch = Stochastisch,
        Perzentil = Perzentil,
        RealisierungenAuslegung = RealisierungenAuslegung,
        Quelle = Quelle,
        IdBedarfstag = IdBedarfstag,
        Entwurf = Entwurf?.Kopie(),
        Konstruktorzeilen = Konstruktorzeilen.Select(z => z.Kopie()).ToList(),
        SpeicherC = SpeicherC,
        ErzeugerKw = ErzeugerKw,
        UebertragerKw = UebertragerKw,
        Speicherart = Speicherart,
        SensorhoeheAnteil = SensorhoeheAnteil,
        Erzeugerart = Erzeugerart,
        Werkstoff = Werkstoff,
        PunktVolumenL = PunktVolumenL,
        PunktLeistungKw = PunktLeistungKw,
        LadeAuto = LadeAuto,
        LadeManuellKw = LadeManuellKw,
        LadefensterH = LadefensterH,
        LadefensterBeginnH = LadefensterBeginnH,
        Nutzanteil = Nutzanteil,
        Zuschlag = Zuschlag,
        PersonenAuto = PersonenAuto,
        PersonenManuell = PersonenManuell,
        FuellstandBezug = FuellstandBezug,
        KonstruktorBezugsart = KonstruktorBezugsart,
        KonstruktorBezugsmenge = KonstruktorBezugsmenge,
        KonstruktorBezugHinweis = KonstruktorBezugHinweis
    };
}

/// <summary>Eine Karte der Dreiergruppe: Stand, Volumen und/oder Leistung, empfohlen, Satz.</summary>
public sealed class ZapfprofilKarteDaten
{
    public ZapfprofilKartenstand Stand { get; set; } = ZapfprofilKartenstand.NichtGerechnet;

    /// <summary>Volumen [l]; <c>null</c>, wo die Karte keines ausweist.</summary>
    public double? VolumenL { get; set; }

    /// <summary>Leistung [kW]; <c>null</c>, wo die Karte keine ausweist.</summary>
    public double? LeistungKw { get; set; }

    /// <summary>Ist dieser Wert der eine empfohlene Punkt?</summary>
    public bool Empfohlen { get; set; }

    /// <summary>Der Satz der Karte (Rechenweg oder Grund) in der Oberflächensprache bzw. im Wortlaut des Kerns.</summary>
    public string Text { get; set; } = "";
}

/// <summary>Die EINE Empfehlung einer Topologiegruppe.</summary>
public sealed class ZapfprofilEmpfehlungDaten
{
    public bool Rechenbar { get; set; }

    /// <summary>Speicher: Punkt der Summenlinie (V, Φ); sonst die Minutenspitze.</summary>
    public bool Speicher { get; set; }

    public double? VolumenL { get; set; }
    public double? LeistungKw { get; set; }

    /// <summary>Der nächste Nenninhalt der Liste ≥ V — nur Anzeige.</summary>
    public double? NenninhaltL { get; set; }

    /// <summary>Trägt der Punkt den Vermerk „Schnellauslegung"?</summary>
    public bool Schnellauslegung { get; set; }

    /// <summary>Der Vermerk (Entwurfsstand, Spitzen unterschätzt …).</summary>
    public string Vermerk { get; set; } = "";

    /// <summary>Warum es keinen Punkt gibt; leer, wenn rechenbar.</summary>
    public string Grund { get; set; } = "";
}

/// <summary>Eine Zeile des Verfahrensvergleichs (nachrichtlich).</summary>
public sealed class ZapfprofilVerfahrenDaten
{
    /// <summary>Der Name des Verfahrens in der Oberflächensprache.</summary>
    public string Verfahren { get; set; } = "";

    /// <summary>Volumen [l]; <c>null</c> = nicht gerechnet („–").</summary>
    public double? VolumenL { get; set; }

    public bool Gueltig { get; set; }
    public bool ImBand { get; set; }

    /// <summary>Ist es der größte Wert im Band?</summary>
    public bool Groesster { get; set; }

    /// <summary>Nur nachrichtlich (der klassische Faustwert) — nie im Band.</summary>
    public bool Nachrichtlich { get; set; }

    public string Kennwert { get; set; } = "";
    public string Rechenweg { get; set; } = "";
}

/// <summary>Der Verfahrensvergleich der Speicherauslegung nach V4 (nur Speicher, nachrichtlich).</summary>
public sealed class ZapfprofilVergleichDaten
{
    public List<ZapfprofilVerfahrenDaten> Verfahren { get; set; } = new();
    public double? BandMinL { get; set; }
    public double? BandMaxL { get; set; }
    public double? NenninhaltL { get; set; }
    public bool Mehrspeicher { get; set; }

    /// <summary>Die Bedarfskennzahl N des Kriteriums N_L ≥ N; <c>null</c> ohne gültigen Normvergleich.</summary>
    public double? KennzahlN { get; set; }

    /// <summary>Die angesetzte Ladeleistung [kW] und ihr Rechenweg.</summary>
    public double LadeleistungKw { get; set; }
    public bool LadeManuell { get; set; }
    public string LadeRechenweg { get; set; } = "";

    public double? Personen { get; set; }
    public double Nutzanteil { get; set; }
    public double Zuschlag { get; set; }

    /// <summary>D_max [kWh]; der Strich „–" hängt an <see cref="ProfilbasiertVorhanden"/>, nie am Text.</summary>
    public double DmaxKwh { get; set; }
    public bool ProfilbasiertVorhanden { get; set; }

    /// <summary>Der maßgebende Zeitpunkt als Satz; leer bei D_max = 0.</summary>
    public string Zeitpunkt { get; set; } = "";

    public double? FuellstandBezugL { get; set; }
    public string FuellstandBezug { get; set; } = "";

    /// <summary>Welches Volumen <see cref="FuellstandBezugL"/> ist; <see cref="ZapfprofilFuellstandbezug.Vorgabe"/> ohne Bezug.</summary>
    public ZapfprofilFuellstandbezug FuellstandBezugArt { get; set; }

    /// <summary>Die Schätzhilfe der Ladeleistung (Vorschlag, manuell, angesetzt, Rechenweg).</summary>
    public ZapfprofilSchaetzhilfeDaten? Ladeleistung { get; set; }

    /// <summary>Die Personen des Vergleichs: Vorschlag aus dem Mengengerüst und ob der manuelle Wert gilt.</summary>
    public double? PersonenVorschlag { get; set; }
    public bool PersonenManuell { get; set; }
    public double? KapazitaetKwh { get; set; }
    public double? MinFuellstandKwh { get; set; }
    public double? ReserveAnteil { get; set; }

    /// <summary>Das Wochenbild der Stundenbilanz.</summary>
    public Zeichenmodell? WochenModell { get; set; }
}

/// <summary>
/// Eine Zapfkategorie im Editor der Stufe Experte (4.4, Katalogkopie): Name, mittlerer Volumenstrom
/// μ und Streuung σ [l/min], Dauer [min], Anteil [-], obere Kappung [l/min] (<c>null</c> = keine)
/// und die Herkunft als Kurztext (nur Anzeige — die Provenienz führt der Kern beim Speichern nach).
/// </summary>
public sealed class ZapfprofilKategorieDaten
{
    public string Name { get; set; } = "";
    public double? VolumenstromLJeMin { get; set; }
    public double? StreuungLJeMin { get; set; }
    public int? DauerMin { get; set; }
    public double? Anteil { get; set; }
    public double? KappungLJeMin { get; set; }
    public string Herkunft { get; set; } = "";

    public ZapfprofilKategorieDaten Kopie() => (ZapfprofilKategorieDaten)MemberwiseClone();
}

/// <summary>
/// Die Kategorien einer Nutzungsart für den Editor: die Liste, die Summe der Anteile samt Hinweis,
/// ob an Ort und Stelle gespeichert wird (<see cref="Frei"/>) oder nur als neue Katalogversion
/// (<see cref="Sperrgrund"/>), und der Vorgabesatz als Startwerte.
/// </summary>
public sealed class ZapfprofilKategorienDaten
{
    public int IdNutzungsart { get; set; }
    public List<ZapfprofilKategorieDaten> Kategorien { get; set; } = new();
    public double SummeAnteil { get; set; }
    public string Hinweis { get; set; } = "";
    public bool Frei { get; set; }
    public string Sperrgrund { get; set; } = "";
    public List<ZapfprofilKategorieDaten> Vorgabe { get; set; } = new();
}

/// <summary>Das Ergebnis des Speicherns der Kategorien: die Nutzungsart, die sie jetzt trägt, ob sie neu ist, und die Meldung einer Ablehnung.</summary>
public sealed record ZapfprofilKategorienErgebnis(bool Ok, int IdNutzungsart, bool NeueZeile, ZapfprofilMeldung? Meldung);

/// <summary>Ein Eintrag der Warnliste: Kennung (Ressourcenschlüssel), Titel, Satz des Kerns, Stufe.</summary>
public sealed record ZapfprofilWarnDaten(string Kennung, string Titel, string Text, ZapfprofilWarnstufe Stufe);

/// <summary>
/// <b>Eine Zeile der Karte „Herkunft"</b> (Herkunftsprotokoll des Kerns, N19): welche Größe
/// welcher Zone welchen Wert trägt, welchen Stand er hat, woher er kommt und was dabei geschah.
/// Alles fertige Texte der Oberflächensprache — <see cref="Vermerk"/> ist der Satz des Kerns,
/// <see cref="Stand"/> und <see cref="Quelle"/> sind die übersetzten Aufzählungen.
///
/// <para><b><see cref="Groesse"/> ist der Feldname des Protokolls als DATEN</b> — der Bezeichner
/// des Rechenwegs (<c>Tagesbedarf</c>, <c>Zirkulation.Laufzeit</c>, <c>Auslegung.ErzeugerKw</c>),
/// in beiden Sprachen derselbe, wie ein Zonen- oder Katalogname. Das Protokoll ist der
/// Rechennachweis: Seine Feldnamen stehen ebenso in Kern, Tests und Referenzlauf, und eine
/// Namenstafel wäre eine zweite Quelle der Wahrheit.</para>
/// </summary>
public sealed record ZapfprofilHerkunftZeile(string Groesse, string Zone, string Wert, string Stand,
                                            string Quelle, string Vermerk);

/// <summary>Das Ergebnis einer Topologiegruppe (Zonen gleicher Topologie).</summary>
public sealed class ZapfprofilAuslegungsgruppeDaten
{
    /// <summary>Die Topologie in der Oberflächensprache.</summary>
    public string Topologie { get; set; } = "";

    /// <summary>Topologie Speicher?</summary>
    public bool Speicher { get; set; }

    public List<string> Zonen { get; set; } = new();

    /// <summary>Der Bedarfstag der Gruppe (Name) bzw. leer; der Satz der Wahl daneben.</summary>
    public string Bedarfstag { get; set; } = "";
    public string BedarfstagWahl { get; set; } = "";
    public bool KonstruktorOeffnen { get; set; }
    public bool SpitzenUnterschaetzt { get; set; }

    /// <summary>Die EINE Speichertemperatur der Gruppe [°C] und ihre Herkunft.</summary>
    public double? SpeicherC { get; set; }
    public string SpeicherCHerkunft { get; set; } = "";

    /// <summary>Die Dreiergruppe: Hauptwert (Summenlinie bzw. Minutenspitze), Perzentil, Normvergleich.</summary>
    public ZapfprofilKarteDaten Hauptwert { get; set; } = new();
    public ZapfprofilKarteDaten Perzentil { get; set; } = new();
    public ZapfprofilKarteDaten Normvergleich { get; set; } = new();

    /// <summary>
    /// Das Perzentil aus dem Ensemble des Bedarfstags (4.5 b) samt Streuband, Gleichzeitigkeit
    /// und Konsistenzhinweis; <c>null</c>, solange nicht „stochastisch" gerechnet ist oder das
    /// Ensemble nicht rechenbar war (dann nennt <see cref="Perzentil"/> den Grund).
    /// </summary>
    public ZapfprofilPerzentilDaten? PerzentilErgebnis { get; set; }

    public ZapfprofilEmpfehlungDaten Empfehlung { get; set; } = new();

    /// <summary>Summenlinie: Ladezeit [h/d], Zeitkonstante [min], Zahl der Wertepaare, Vermerk.</summary>
    public double? LadezeitH { get; set; }
    public double? ZeitkonstanteMin { get; set; }
    public int Wertepaare { get; set; }
    public string Vermerk { get; set; } = "";
    public Zeichenmodell? SummenlinieModell { get; set; }
    public Zeichenmodell? WertepaarModell { get; set; }

    /// <summary>Normvergleich: Kennzahl N, Zonen außerhalb, Hinweis zur Wärmepumpe.</summary>
    public double? KennzahlN { get; set; }
    public List<string> ZonenAusserhalb { get; set; } = new();
    public bool HinweisWaermepumpe { get; set; }

    /// <summary>Der Verfahrensvergleich; <c>null</c> außerhalb der Topologie Speicher oder wenn nicht rechenbar.</summary>
    public ZapfprofilVergleichDaten? Vergleich { get; set; }

    /// <summary>Die Warnliste der Gruppe.</summary>
    public List<ZapfprofilWarnDaten> Warnliste { get; set; } = new();
}

/// <summary>Ein Perzentil des Streubands: die Stufe (50, 90, 95, 99) und ihr Wert (+∞ = ohne Nachweis).</summary>
public sealed record ZapfprofilPerzentilZeileDaten(int Perzentil, double Wert);

/// <summary>
/// <b>Das Perzentil einer Topologiegruppe</b> (4.4, 4.5 b), wie der Kern es ausweist: p, Seed,
/// die Zahl der gezogenen Tage und ob sie für ein empirisches p-Perzentil genügt (R ≥ 1/(1 − p)),
/// der maßgebende Jahrestag, das Streuband der Auslegungsgröße (Speicher: erforderliches Volumen
/// beim Φ_N des Summenlinienpunkts in Litern; sonst Minutenspitze in kW), die Gleichzeitigkeit
/// als ERGEBNIS (GLF_V bzw. GLF_P) mit der Zahl der Einheiten, nachrichtlich Minuten- und
/// Stundenspitze, der Vergleich μ + z·σ/√N und der Konsistenzhinweis.
/// </summary>
public sealed class ZapfprofilPerzentilDaten
{
    /// <summary>Das Auslegungsperzentil p (95 oder 99).</summary>
    public int Perzentil { get; set; }

    public long Seed { get; set; }

    /// <summary>Die Zahl der gezogenen Tage R.</summary>
    public int Realisierungen { get; set; }

    /// <summary>Die Mindestzahl ⌈1/(1 − p)⌉ für ein empirisches p-Perzentil.</summary>
    public int Mindestzahl { get; set; }

    /// <summary>Genügt R für ein empirisches p-Perzentil? Sonst trägt der Wert den Vermerk „nicht belastbar".</summary>
    public bool Belastbar { get; set; }

    /// <summary>Der maßgebende Jahrestag (1 … 365).</summary>
    public int Tag { get; set; }

    /// <summary>
    /// Der maßgebende Tag als Datum in der Oberflächensprache („17. Januar") — aus dem Rechenjahr
    /// ohne Schaltjahr, unabhängig vom Kalenderjahr; leer = die Hülle hat keines gesetzt.
    /// </summary>
    public string Datum { get; set; } = "";

    /// <summary>Speicher: Die Werte sind erforderliche Volumina [l] beim Φ_N; sonst Minutenspitzen [kW].</summary>
    public bool Volumen { get; set; }

    /// <summary>Φ_N des Summenlinienpunkts, bei dem die Volumina gelten [kW] (nur Speicher).</summary>
    public double? LeistungKw { get; set; }

    /// <summary>Das Streuband: P50, P90, P95 und P99 der Auslegungsgröße.</summary>
    public List<ZapfprofilPerzentilZeileDaten> Streuband { get; set; } = new();

    /// <summary>Kleinster und größter Wert über die Realisierungen (+∞ = ohne Nachweis).</summary>
    public double Minimum { get; set; }
    public double Maximum { get; set; }

    /// <summary>Realisierungen ohne Nachweis beim Φ_N (Volumen ∞, nur Speicher).</summary>
    public int OhneNachweis { get; set; }

    /// <summary>Minutenspitze und größte Stundenleistung der Gruppe beim Perzentil p [kW] — nachrichtlich.</summary>
    public double MinutenspitzeKw { get; set; }
    public double StundenspitzeKw { get; set; }

    /// <summary>Die Gleichzeitigkeit als Ergebnis: GLF_V bei Speicher, sonst GLF_P; <c>null</c> ohne Last.</summary>
    public double? Gleichzeitigkeit { get; set; }

    /// <summary>Die Einheiten Σ n_E der Gruppe — der Bezug der Gleichzeitigkeit.</summary>
    public int Einheiten { get; set; }

    /// <summary>Der Vergleich μ + z·σ/√N [kW]; <c>null</c> ohne Quantil im Parametersatz.</summary>
    public double? WurzelNKw { get; set; }

    /// <summary>
    /// Wohnungsstation (4.5, N10 (d)): das Perzentil p der Minutenspitze JE EINHEIT [kW] — die
    /// Auslegungsgröße jeder Station, aus der Zone, in der sie am größten ist; <c>null</c> bei
    /// jeder anderen Topologie.
    /// </summary>
    public double? SpitzeJeEinheitKw { get; set; }

    /// <summary>Die Zone der Spitze je Einheit; leer ohne sie.</summary>
    public string SpitzeJeEinheitZone { get; set; } = "";

    /// <summary>Ist der Konsistenzhinweis geprüft (nur Speicher, mit Schwelle im Parametersatz)?</summary>
    public bool KonsistenzGeprueft { get; set; }

    /// <summary>Liegt die stochastische Spitze über der Schwelle zur Leistung des Summenlinienpunkts?</summary>
    public bool KonsistenzAuffaellig { get; set; }

    /// <summary>
    /// Die Werte der Probe, wie der Kern sie ausweist — der Satz der Oberfläche entsteht aus ihnen
    /// (N11 (k)): die verglichene Größe, das Perzentil p der größten Stundenleistung [kW] …
    /// </summary>
    public double? KonsistenzSpitzeKw { get; set; }

    /// <summary>… die Schwelle aus dem Parametersatz [-] …</summary>
    public double? KonsistenzSchwelle { get; set; }

    /// <summary>… und die Leistung Φ_N des Summenlinienpunkts, auf die sie sich bezieht [kW]; je <c>null</c> ohne Probe.</summary>
    public double? KonsistenzLeistungKw { get; set; }

    /// <summary>Der Wert des gewählten Perzentils; +∞ = ohne Nachweis, NaN ohne Streuband.</summary>
    public double Wert => Streuband.FirstOrDefault(z => z.Perzentil == Perzentil)?.Wert ?? double.NaN;
}

/// <summary>Das Ergebnis des Rechen-Delegaten der Überlagerung.</summary>
public sealed class ZapfprofilAuslegungDaten
{
    public ZapfprofilAuslegungZustand Zustand { get; set; }

    /// <summary>Ist mit „Stochastisch rechnen" gerechnet (das Perzentil gezogen)?</summary>
    public bool Stochastisch { get; set; }

    /// <summary>Warum nicht gerechnet wurde; leer bei <see cref="ZapfprofilAuslegungZustand.Gerechnet"/>.</summary>
    public string Grund { get; set; } = "";

    /// <summary>Der Statustext der Fußleiste.</summary>
    public string Status { get; set; } = "";

    public List<ZapfprofilAuslegungsgruppeDaten> Gruppen { get; set; } = new();

    /// <summary>Abgelehnte Zonen und allgemeine Hinweise.</summary>
    public List<ZapfprofilMeldung> Meldungen { get; set; } = new();

    /// <summary>
    /// <b>Das Herkunftsprotokoll der Auslegung</b> (Karte „Herkunft", N19): je Wert eine Zeile in
    /// der Reihenfolge des Rechenwegs, fertige Texte der Oberflächensprache.
    /// </summary>
    public List<ZapfprofilHerkunftZeile> Herkunft { get; set; } = new();

    /// <summary>
    /// Steht die Karte „Herkunft"? Die Überlagerung führt keine eigene Stufe, deshalb entscheidet
    /// die Hülle: ab Stufe Erweitert ja, in der Stufe Einfach nein (N19). Leer und sichtbar heißt
    /// „nichts zu vermerken" — benannt, nicht still.
    /// </summary>
    public bool HerkunftSichtbar { get; set; }

    /// <summary>Die angesetzte Erzeugerart und woher sie kommt (Eingabe, Anlagenbestand, keine).</summary>
    public ZapfprofilErzeugerart ErzeugerartAngesetzt { get; set; }
    public string ErzeugerartHerkunft { get; set; } = "";

    /// <summary>
    /// Der Vorschlag der Erzeugerart aus dem Projekt (Anlagenbestand, eindeutig);
    /// <see cref="ZapfprofilErzeugerart.KeineAngabe"/> ohne oder bei mehrdeutigem Bestand (N10 (i)).
    /// </summary>
    public ZapfprofilErzeugerart ErzeugerartVorschlag { get; set; }

    /// <summary>
    /// Die Gruppe des EINEN Punkts, den OK übernimmt: die erste Speichergruppe mit rechenbarer
    /// Empfehlung, sonst die erste Gruppe mit rechenbarer Empfehlung; <c>null</c> ohne Punkt.
    /// </summary>
    public ZapfprofilAuslegungsgruppeDaten? Punktgruppe
        => Gruppen.FirstOrDefault(g => g.Speicher && g.Empfehlung.Rechenbar)
           ?? Gruppen.FirstOrDefault(g => g.Empfehlung.Rechenbar);
}

/// <summary>
/// Der Stand der Überlagerung beim Öffnen: Kontext, Stufe, Eingaben, die wählbaren
/// Bedarfstage, die Zapfregeln des Konstruktors samt Namensvorschlag, der Vorschlag der
/// Erzeugerart aus dem Anlagenbestand und das erste Ergebnis.
/// </summary>
public sealed class ZapfprofilAuslegungStartDaten
{
    /// <summary>Die Kontextzeile (Projekt · Zonen).</summary>
    public string Kontext { get; set; } = "";

    /// <summary>Die Stufe des Zapfprofil-Dialogs (Einfach: der Punkt trägt „Schnellauslegung").</summary>
    public ZapfprofilStufe Stufe { get; set; } = ZapfprofilStufe.Einfach;

    public ZapfprofilAuslegungEingabeDaten Eingabe { get; set; } = new();

    /// <summary>Die Bedarfstage des Katalogs.</summary>
    public List<ZapfprofilBedarfstagDaten> Bedarfstage { get; set; } = new();

    /// <summary>Die Zapfregeln des Konstruktors; leer mit Grund, wenn der Katalog keine trägt.</summary>
    public List<ZapfprofilRegelDaten> Regeln { get; set; } = new();
    public string RegelnGrund { get; set; } = "";

    /// <summary>Ein freier Name für den konstruierten Tag.</summary>
    public string NameVorschlag { get; set; } = "";

    /// <summary>Kann die Auslegung hier rechnen? Sonst nennt <see cref="Sperrgrund"/> den Grund.</summary>
    public bool Verfuegbar { get; set; }
    public string Sperrgrund { get; set; } = "";

    /// <summary>Das Ergebnis zu <see cref="Eingabe"/>; <c>null</c>, wenn nicht gerechnet wurde.</summary>
    public ZapfprofilAuslegungDaten? Ergebnis { get; set; }

    /// <summary>Die Wertemenge des Auslegungsperzentils (Schema, K3); leer = keine Wahl.</summary>
    public List<int> Perzentile { get; set; } = new();

    /// <summary>Die Vorgabe des Perzentils (DDL); <c>null</c> = unbekannt.</summary>
    public int? PerzentilVorgabe { get; set; }

    /// <summary>Die Untergrenze der Realisierungen des Bedarfstags (Schema); <c>null</c> = keine bekannt.</summary>
    public int? RealisierungenMindestens { get; set; }

    /// <summary>Die Obergrenze der Realisierungen des Bedarfstags (Kern); <c>null</c> = keine bekannt.</summary>
    public int? RealisierungenHoechstens { get; set; }

    /// <summary>Je Perzentil die Vorgabe der Realisierungen ⌈Vielfaches · 1/(1 − p)⌉ (Kern, Parametersatz).</summary>
    public Dictionary<int, int> RealisierungenVorgabe { get; set; } = new();

    /// <summary>Warum die Vorgabe der Realisierungen nicht bestimmbar ist; leer, wenn sie es ist.</summary>
    public string RealisierungenVorgabeGrund { get; set; } = "";

    /// <summary>Je Perzentil die Mindestzahl ⌈1/(1 − p)⌉ — darunter ist das Perzentil „nicht belastbar".</summary>
    public Dictionary<int, int> Mindestzahl { get; set; } = new();

    /// <summary>
    /// Die Bezugsarten eines konstruierten Tags (Wertemenge des Schemas,
    /// <c>Tab_TwwBedarfstag_STAMM.Bezugsart</c>) mit ihrem Namen in der Oberflächensprache.
    /// </summary>
    public List<ZapfprofilKatalogeintragDaten> Bezugsarten { get; set; } = new();

    /// <summary>
    /// Die Bezüge des Füllstands (Wertemenge des Schemas, <c>Tab_TwwProjekt.Fuellstand_Bezug</c>) mit ihrem
    /// Namen; die Vorgabe (0) steht nicht darin — sie nennt der Dialog selbst.
    /// </summary>
    public List<ZapfprofilKatalogeintragDaten> Fuellstandbezuege { get; set; } = new();
}

/// <summary>
/// <b>Wo die gemessene Spitze im Band der synthetischen Dauerlinie liegt</b> (Kapitel 7 Zeile Z5,
/// Kennzahl (b)). <see cref="Unbestimmt"/> heißt „nicht entscheidbar“ — ohne Stundenwerte der
/// Messung; nie eine stille Antwort. Die Zahlen sind die des Kerns (<c>Spitzenlage</c>), damit die
/// Hülle sie ohne Tabelle abbilden kann.
/// </summary>
public enum ZapfprofilSpitzenlage
{
    /// <summary>Nicht entscheidbar.</summary>
    Unbestimmt = 0,

    /// <summary>Unter der unteren Bandgrenze — die Rechnung überschätzt die Spitze stärker als erwartet.</summary>
    Unterhalb = 1,

    /// <summary>Im Band — die Abnahme der Validierung ist erfüllt.</summary>
    ImBand = 2,

    /// <summary>Über der oberen Bandgrenze — die Rechnung unterschätzt die Spitze.</summary>
    Oberhalb = 3,

    /// <summary>
    /// Nicht bewertbar (Anwenderentscheid ZU35): weniger Einheiten als die Mindestzahl — die
    /// Grenzen stehen zur Anschauung, die Zeile ist gelb gekennzeichnet, nie rot.
    /// </summary>
    NichtBewertbar = 4
}

/// <summary>
/// <b>Die Ampel des Vergleichs</b> (Anwenderentscheid ZU35): die Zahlen des Kerns
/// (<c>Vergleichsampel</c>), damit die Hülle sie ohne Tabelle abbilden kann.
/// </summary>
public enum ZapfprofilVergleichsampel
{
    /// <summary>Erfüllt.</summary>
    Gruen = 0,

    /// <summary>Nicht entschieden oder nicht bewertbar.</summary>
    Gelb = 1,

    /// <summary>Verletzt.</summary>
    Rot = 2
}

/// <summary>
/// <b>Die Form EINES Tagtyps im Vergleich</b> (Kennzahl (d)): der Tagtyp in der Oberflächensprache,
/// die Zahl der eingegangenen Tage beider Seiten, die mittlere Abweichung der 24 Stundenanteile
/// gegen die Schwelle und der Anteil der Tagesenergie, der in anderen Stunden liegt.
/// </summary>
public sealed record ZapfprofilFormabgleichDaten(string Tagtyp, int TageGemessen, int TageGerechnet,
                                                 double MittlereAbweichung, double VerschobenerAnteil,
                                                 bool ImRahmen);

/// <summary>
/// <b>Der Vergleichsbericht „synthetisch gegen gemessen“</b> (Umsetzungskonzept
/// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 3) — was der Reiter
/// „Kennzahlen“ ab der Stufe Erweitert zeigt.
///
/// <para><b>Nur Verhältniszahlen</b> (Konzept Kapitel 9 K5): Jede Zahl ist ein Verhältnis oder ein
/// Anteil — keine gemessene Menge, keine gemessene Leistung. So verlässt nichts die Anlage, was
/// das Objekt beschreibt.</para>
///
/// <para><b>Was nicht entschieden ist, bleibt <c>null</c></b> und wird benannt: ohne Ensemble keine
/// Spitzenstreuung, ohne Stundenwerte kein Band, ohne vollen Tag kein Formmaß. Ein vorbelegtes
/// Feld gäbe als Ergebnis aus, was keines ist (Hausregel <c>EPOS.UI/CLAUDE.md</c>).</para>
/// </summary>
public sealed class ZapfprofilMessvergleichDaten
{
    /// <summary>Ist der Vergleich gerechnet? Sonst nennt <see cref="Abbruch"/> den Grund.</summary>
    public bool Ok { get; set; }

    /// <summary>Die Bezeichnung der verglichenen Messreihe.</summary>
    public string Reihe { get; set; } = "";

    /// <summary>Der Grund, aus dem der Vergleich nicht rechenbar ist; leer = gerechnet.</summary>
    public string Abbruch { get; set; } = "";

    /// <summary>Die Kennung des Grunds (Meldungskennung für „erklären lassen“); leer = gerechnet.</summary>
    public string Kennung { get; set; } = "";

    /// <summary>Kennzahl (a): Q_gemessen / Q_gerechnet [-]; <c>null</c> ohne Ergebnis.</summary>
    public double? EnergieVerhaeltnis { get; set; }

    /// <summary>Kennzahl (a): das Verhältnis minus 1 [-].</summary>
    public double? EnergieAbweichung { get; set; }

    /// <summary>Kennzahl (b): die gemessene Stundenspitze, bezogen auf die größte gerechnete [-].</summary>
    public double? Spitzenverhaeltnis { get; set; }

    /// <summary>Kennzahl (b): die untere Bandgrenze als Verhältnis [-].</summary>
    public double? BandUnten { get; set; }

    /// <summary>Kennzahl (b): die obere Bandgrenze als Verhältnis [-].</summary>
    public double? BandOben { get; set; }

    /// <summary>Kennzahl (b): das untere Perzentil der Dauerlinie [-] (Vorgabe 0,95).</summary>
    public double PerzentilUnten { get; set; }

    /// <summary>Kennzahl (b): das obere Perzentil der Dauerlinie [-] (Vorgabe 0,999).</summary>
    public double PerzentilOben { get; set; }

    /// <summary>Kennzahl (b): über wie viele Stundenwerte die Dauerlinie gebildet ist.</summary>
    public int Dauerlinienwerte { get; set; }

    /// <summary>Kennzahl (b): wo die Messspitze im Band liegt.</summary>
    public ZapfprofilSpitzenlage Lage { get; set; }

    /// <summary>Kennzahl (b): die Einheiten der Anlage, gegen die die Mindestzahl gehalten wurde (0 = unbekannt).</summary>
    public int BandEinheiten { get; set; }

    /// <summary>Kennzahl (b): die Mindestzahl der Einheiten, ab der das Band bewertet (ZU35).</summary>
    public int BandMindestEinheiten { get; set; }

    /// <summary>Die Gesamtampel des Vergleichs (Band und Form; ZU35) — nur gültig, wenn <see cref="Ok"/>.</summary>
    public ZapfprofilVergleichsampel Gesamtampel { get; set; } = ZapfprofilVergleichsampel.Rot;

    /// <summary>Die untere Grenze der Realisierungsspitzen [-]; <c>null</c> ohne Ensemble.</summary>
    public double? StreuungUnten { get; set; }

    /// <summary>Die obere Grenze der Realisierungsspitzen [-]; <c>null</c> ohne Ensemble.</summary>
    public double? StreuungOben { get; set; }

    /// <summary>Die Streubreite oben/unten [-]; <c>null</c> ohne Ensemble, 1 = keine Streuung.</summary>
    public double? Streubreite { get; set; }

    /// <summary>Die Zahl der Realisierungen des Ensembles; 0 ohne Ensemble.</summary>
    public int Realisierungen { get; set; }

    /// <summary>
    /// Wie viele Zonen ein Ensemble tragen, wenn es <b>mehr als eine</b> ist; sonst 0. Dann ist die
    /// Stichprobe der Realisierungsspitzen nicht zu bilden — jede Zone zieht ihre Realisierungen
    /// für sich, die Spitze der Summe ist nicht die Summe der Spitzen. Die Zeile trägt dann einen
    /// Strich mit genau diesem Grund, nicht mit „ohne Ensemble“.
    /// </summary>
    public int EnsembleZonen { get; set; }

    /// <summary>Kennzahl (c): die Zahl der Einheiten N; <c>null</c> ohne Einheitenzahl.</summary>
    public int? Einheiten { get; set; }

    /// <summary>Kennzahl (c): 1/√N [-].</summary>
    public double? WurzelNVerhaeltnis { get; set; }

    /// <summary>Kennzahl (c): Spitzenverhältnis · √N [-]; 1 = die Überschätzung folgt genau 1/√N.</summary>
    public double? Skalierungsmass { get; set; }

    /// <summary>Kennzahl (d): die größte mittlere Abweichung über die Tagtypen [-]; <c>null</c> ohne vollen Tag.</summary>
    public double? Formmass { get; set; }

    /// <summary>Kennzahl (d): die Schwelle des Formabgleichs [-] (Parameter).</summary>
    public double Formschwelle { get; set; }

    /// <summary>Kennzahl (d): liegt die Form im Rahmen? Ohne Maß <c>false</c> („nicht entschieden“).</summary>
    public bool FormImRahmen { get; set; }

    /// <summary>Kennzahl (d): je Tagtyp, den BEIDE Seiten führen, eine Zeile.</summary>
    public List<ZapfprofilFormabgleichDaten> Form { get; set; } = new();

    /// <summary>Kennzahl (e): die größte absolute Abweichung der Monatsanteile [-]; <c>null</c> ohne Ergebnis.</summary>
    public double? MonateGroessteAbweichung { get; set; }

    /// <summary>Kennzahl (e): der Monat der größten Abweichung (1 … 12); 0 ohne Ergebnis.</summary>
    public int MonateGroessterMonat { get; set; }

    /// <summary>Die benannten Hinweise (Teiljahr, Schalttag, Feiertage, Lücken, Ensemble) als Warnzeilen.</summary>
    public List<ZapfprofilWarnDaten> Hinweise { get; set; } = new();

    /// <summary>Rechnete die verglichene Jahresreihe stochastisch?</summary>
    public bool Stochastisch { get; set; }
}

/// <summary>
/// <b>Was „Aus Messreihe kalibrieren“ ergibt</b> (Stufe Z5, Gruppe 3, Punkt 5): der Jahresmesswert
/// aus der Reihe samt Bilanzgrenze, Quelle und Zeitraum — die Werte, die der Dialog in die Felder
/// der Zone schreibt — oder der benannte Grund, aus dem es nicht geht.
/// </summary>
public sealed class ZapfprofilMesskalibrierungDaten
{
    /// <summary>Steht der Messwert?</summary>
    public bool Ok { get; set; }

    /// <summary>Der Grund, aus dem kein Messwert entsteht; leer = er steht.</summary>
    public string Abbruch { get; set; } = "";

    /// <summary>Die Kennung des Grunds (Meldungskennung); leer = er steht.</summary>
    public string Kennung { get; set; } = "";

    /// <summary>Die Bezeichnung der Messreihe.</summary>
    public string Reihe { get; set; } = "";

    /// <summary>Der Jahresmesswert in der Einheit <see cref="EinheitId"/>.</summary>
    public double? Wert { get; set; }

    /// <summary>Die Einheit des Messwerts als Kennung der Maske (kWh/a oder m³/a).</summary>
    public int EinheitId { get; set; }

    /// <summary>Die Bilanzgrenze des Messwerts als Kennung der Maske; <c>null</c> bei einem Volumen.</summary>
    public int? BilanzgrenzeId { get; set; }

    /// <summary>Die Quelle, die in das Feld „Quelle“ geht.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Der Zeitraum, der in das Feld „Zeitraum“ geht.</summary>
    public string Zeitraum { get; set; } = "";

    /// <summary>Ist der Wert aus einem Teiljahr hochgerechnet? Dann nennt ein Hinweis den Bias.</summary>
    public bool Hochgerechnet { get; set; }

    /// <summary>Die benannten Hinweise (Hochrechnung, Bias) als Warnzeilen.</summary>
    public List<ZapfprofilWarnDaten> Hinweise { get; set; } = new();
}

/// <summary>Ein vorgeschlagener Tagesgang je Tagtyp in der Vorschau des Kalibriervorschlags.</summary>
/// <param name="Tagtyp">Der Tagtyp in der Oberflächensprache.</param>
/// <param name="Tage">Wie viele vollständige Messtage in das Mittel eingegangen sind.</param>
/// <param name="Anteile">Die 24 Stundenanteile [-], Summe 1.</param>
public sealed record ZapfprofilVorschlagsgangDaten(string Tagtyp, int Tage, List<double> Anteile);

/// <summary>
/// <b>Der Kalibriervorschlag einer Nichtwohn-Zone als Vorschau</b> (Stufe Z5, Gruppe 3, Punkt 5):
/// Tagesbedarf, Wochenfaktoren und die Tagesgänge je Tagtyp, dazu die Bezeichnung der Kopie, die
/// entstehen würde. <b>Nichts ist geschrieben</b>, solange der Anwender die Rückfrage nicht mit Ja
/// beantwortet hat.
///
/// <para>Diese Zahlen sind Parameter der eigenen Kopie und keine Kennzahl eines Berichts; sie
/// tragen deshalb absolute Werte (Konzept 4.8) und bleiben in der Datenbank des Anwenders.</para>
/// </summary>
public sealed class ZapfprofilVorschlagDaten
{
    /// <summary>Steht ein Vorschlag?</summary>
    public bool Ok { get; set; }

    /// <summary>Der Grund, aus dem kein Vorschlag entsteht; leer = er steht.</summary>
    public string Abbruch { get; set; } = "";

    /// <summary>Die Kennung des Grunds (Meldungskennung); leer = er steht.</summary>
    public string Kennung { get; set; } = "";

    /// <summary>Die Bezeichnung der Messreihe.</summary>
    public string Reihe { get; set; } = "";

    /// <summary>Die Nutzungsart, von der die Kopie abstammt.</summary>
    public string Vorlage { get; set; } = "";

    /// <summary>Die Bezeichnung und Katalogversion, die die Kopie tragen würde.</summary>
    public string Kopie { get; set; } = "";

    /// <summary>Der gemessene Tagesbedarf [kWh/d].</summary>
    public double TagesbedarfKwh { get; set; }

    /// <summary>Der gemessene Tagesbedarf je Einheit [kWh/(Einheit·d)] — das mittlere Niveau der Kopie.</summary>
    public double TagesbedarfJeEinheitKwh { get; set; }

    /// <summary>Die Bezugsmenge, auf die der Vorschlag bezogen ist.</summary>
    public double Bezugsmenge { get; set; }

    /// <summary>Die Zahl der vollständigen Messtage, die eingegangen sind.</summary>
    public int VolleTage { get; set; }

    /// <summary>Die sieben Wochenfaktoren [-] (Montag zuerst), Summe 1.</summary>
    public List<double> Wochenfaktoren { get; set; } = new();

    /// <summary>Die vorgeschlagenen Tagesgänge je Tagtyp; ein fehlender Tagtyp behält den der Vorlage.</summary>
    public List<ZapfprofilVorschlagsgangDaten> Tagesgaenge { get; set; } = new();

    /// <summary>Die benannten Hinweise als Warnzeilen.</summary>
    public List<ZapfprofilWarnDaten> Hinweise { get; set; } = new();
}

/// <summary>
/// <b>Was die Übernahme eines Kalibriervorschlags ergeben hat</b>: die Id der neuen Anwenderkopie
/// und ihre Bezeichnung — oder der benannte Grund. Die Zone stellt der Dialog danach auf die Kopie
/// um; die Vorlage bleibt unberührt (K7).
/// </summary>
public sealed class ZapfprofilVorschlagErgebnisDaten
{
    /// <summary>Ist die Kopie angelegt?</summary>
    public bool Ok { get; set; }

    /// <summary>Der Grund, aus dem nichts entstanden ist; leer = angelegt.</summary>
    public string Abbruch { get; set; } = "";

    /// <summary>Die Id der neuen Nutzungsart; 0 ohne Erfolg.</summary>
    public int IdNutzungsart { get; set; }

    /// <summary>Die Bezeichnung der neuen Nutzungsart samt Katalogversion.</summary>
    public string Kopie { get; set; } = "";

    /// <summary>Die Meldung für die Statuszeile.</summary>
    public string Meldung { get; set; } = "";

    /// <summary>
    /// Die benannten Hinweise des Vorschlags, den die Kopie trägt (fehlender Tagtyp, fehlender
    /// Wochentag, kurze Reihe) — sie gehören zu den Werten der Kopie und stehen deshalb NACH der
    /// Übernahme weiter in der Warnliste; die Vorschau ist dann längst zu.
    /// </summary>
    public List<ZapfprofilWarnDaten> Hinweise { get; set; } = new();
}
