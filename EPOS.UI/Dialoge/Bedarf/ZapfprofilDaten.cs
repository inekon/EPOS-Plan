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

    /// <summary>Eine unabhängige Kopie samt Zonen.</summary>
    public ZapfprofilEingabeDaten Kopie() => new()
    {
        Weg = Weg,
        Zonen = Zonen.Select(z => z.Kopie()).ToList()
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
