using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DATENSEITE des Dialogs „Brauchwasser-Zapfprofil" (Umsetzungskonzept
//  Zapfprofilgenerator 5.1–5.3; Stufe Z1, Gruppe 3). Nur Daten: keine Datenbank, keine
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
/// Anwender nicht versteht, bekommt eine Kennung"): <c>ZPG_HINW_…</c>, <c>ZPG_EINGABE_…</c>,
/// <c>ZPG_SPEICHER_…</c> oder <c>ZPG_MSG_…</c> — der Ressourcenschlüssel des Textes.
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
}

/// <summary>
/// Eine Zone in der Stufe Einfach (5.3): Zonenname, Nutzungsart, Bezugsmenge, Bedarfsniveau.
/// Was die Stufen Erweitert und Experte führen, steht NICHT hier — die Hülle behält es am
/// Arbeitsstand des Kerns und setzt nur diese vier Felder zurück (Die Stufe behält
/// Überschreibungen).
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
    /// Wie viele Größen der höheren Stufen diese Zone überschreibt (nicht auf Vorgabe) — nur
    /// Anzeige für den Zähler „n Werte überschrieben"; die Hülle zählt, der Dialog zeigt.
    /// </summary>
    public int Ueberschrieben { get; set; }

    /// <summary>Eine unabhängige Kopie (der Dialog arbeitet bis OK auf Kopien).</summary>
    public ZapfprofilZoneDaten Kopie() => new()
    {
        Id = Id,
        IdVorlage = IdVorlage,
        Name = Name,
        IdNutzungsart = IdNutzungsart,
        Bezugsmenge = Bezugsmenge,
        Niveau = Niveau,
        Ueberschrieben = Ueberschrieben
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

    /// <summary>Eine unabhängige Kopie samt Zonen und Auslegung.</summary>
    public ZapfprofilEingabeDaten Kopie() => new()
    {
        Weg = Weg,
        Zonen = Zonen.Select(z => z.Kopie()).ToList(),
        Auslegung = Auslegung?.Kopie()
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

    /// <summary>Der Statustext der Fußleiste.</summary>
    public string Status { get; set; } = "";

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

    /// <summary>Die Stufe beim Öffnen.</summary>
    public ZapfprofilStufe Stufe { get; set; } = ZapfprofilStufe.Einfach;

    /// <summary>Die Vorschau zum Arbeitsstand beim Öffnen; <c>null</c>, wenn nicht gerechnet wurde.</summary>
    public ZapfprofilVorschauDaten? Vorschau { get; set; }

    /// <summary>Kann der Generator hier rechnen? Sonst nennt <see cref="Sperrgrund"/> den Grund.</summary>
    public bool Verfuegbar { get; set; }

    /// <summary>Der benannte Grund, warum der Generator nicht verfügbar ist; leer, wenn er es ist.</summary>
    public string Sperrgrund { get; set; } = "";

    /// <summary>Wie viele Größen der höheren Stufen der Arbeitsstand überschreibt (Summe der Zonen).</summary>
    public int Ueberschrieben => Eingabe.Zonen.Sum(z => z.Ueberschrieben);
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

/// <summary>Die Erzeugerart am Speicher — eine Laufangabe, nicht gespeichert (N10 (i)).</summary>
public enum ZapfprofilErzeugerart
{
    KeineAngabe = 0,
    Kessel = 1,
    Waermepumpe = 2
}

/// <summary>Der Werkstoff des Übertragers — eine Laufangabe, nicht gespeichert (N10 (i)).</summary>
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

    /// <summary>Die Energie des Tages [kWh].</summary>
    public double TagessummeKwh { get; set; }

    /// <summary>Die größte Minutenleistung [kW].</summary>
    public double MinutenspitzeKw { get; set; }

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
        TagessummeKwh = TagessummeKwh,
        MinutenspitzeKw = MinutenspitzeKw
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
    /// <summary>Die Quelle des Bedarfstags; <see cref="ZapfprofilBedarfstagquelle.Vorgaberegel"/> = Vorgaberegel.</summary>
    public ZapfprofilBedarfstagquelle Quelle { get; set; }

    /// <summary>Der gewählte Katalogtag (Quellen 2 … 5); <c>null</c> = keiner.</summary>
    public int? IdBedarfstag { get; set; }

    /// <summary>Der konstruierte, noch nicht gespeicherte Tag (Quelle Konstruktor ohne Id).</summary>
    public ZapfprofilBedarfstagDaten? Entwurf { get; set; }

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

    /// <summary>Die Erzeugerart am Speicher (Laufangabe, nicht gespeichert).</summary>
    public ZapfprofilErzeugerart Erzeugerart { get; set; }

    /// <summary>Der Werkstoff des Übertragers (Laufangabe, nicht gespeichert).</summary>
    public ZapfprofilWerkstoff Werkstoff { get; set; }

    /// <summary>Das Volumen des übernommenen Punkts [l]; <c>null</c> = keiner.</summary>
    public double? PunktVolumenL { get; set; }

    /// <summary>Die Leistung des übernommenen Punkts [kW]; <c>null</c> = keiner.</summary>
    public double? PunktLeistungKw { get; set; }

    /// <summary>Eine unabhängige Kopie samt Entwurf.</summary>
    public ZapfprofilAuslegungEingabeDaten Kopie() => new()
    {
        Quelle = Quelle,
        IdBedarfstag = IdBedarfstag,
        Entwurf = Entwurf?.Kopie(),
        SpeicherC = SpeicherC,
        ErzeugerKw = ErzeugerKw,
        UebertragerKw = UebertragerKw,
        Speicherart = Speicherart,
        SensorhoeheAnteil = SensorhoeheAnteil,
        Erzeugerart = Erzeugerart,
        Werkstoff = Werkstoff,
        PunktVolumenL = PunktVolumenL,
        PunktLeistungKw = PunktLeistungKw
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
    public double? KapazitaetKwh { get; set; }
    public double? MinFuellstandKwh { get; set; }
    public double? ReserveAnteil { get; set; }

    /// <summary>Das Wochenbild der Stundenbilanz.</summary>
    public Zeichenmodell? WochenModell { get; set; }
}

/// <summary>Ein Eintrag der Warnliste: Kennung (Ressourcenschlüssel), Titel, Satz des Kerns, Stufe.</summary>
public sealed record ZapfprofilWarnDaten(string Kennung, string Titel, string Text, ZapfprofilWarnstufe Stufe);

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

/// <summary>Das Ergebnis des Rechen-Delegaten der Überlagerung.</summary>
public sealed class ZapfprofilAuslegungDaten
{
    public ZapfprofilAuslegungZustand Zustand { get; set; }

    /// <summary>Warum nicht gerechnet wurde; leer bei <see cref="ZapfprofilAuslegungZustand.Gerechnet"/>.</summary>
    public string Grund { get; set; } = "";

    /// <summary>Der Statustext der Fußleiste.</summary>
    public string Status { get; set; } = "";

    public List<ZapfprofilAuslegungsgruppeDaten> Gruppen { get; set; } = new();

    /// <summary>Abgelehnte Zonen und allgemeine Hinweise.</summary>
    public List<ZapfprofilMeldung> Meldungen { get; set; } = new();

    /// <summary>Die angesetzte Erzeugerart und woher sie kommt (Eingabe, Anlagenbestand, keine).</summary>
    public ZapfprofilErzeugerart ErzeugerartAngesetzt { get; set; }
    public string ErzeugerartHerkunft { get; set; } = "";

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
}

