using System;
using System.Collections.Generic;
using System.Threading;

namespace SpeicherEngine;

/// <summary>
/// Die Betriebsfuehrung einer Flotte (Spezifikation 5 und 6).
/// </summary>
/// <remarks>
/// Die ersten beiden Ziele sind REAKTIV: sie entscheiden allein aus dem laufenden
/// Intervall und brauchen keine Zukunftsinformation. Die drei uebrigen sind PLANEND;
/// sie verlangen einen <see cref="IFlottenPlaner"/> und einen
/// <see cref="FlottenPrognoseSnapshot"/>, der den ganzen Horizont abdeckt.
/// </remarks>
public enum FlottenBetriebsziel
{
    /// <summary>Reaktiv: Ueberschuss sofort laden, bei Nettobedarf sofort entladen (Spezifikation 5.2).</summary>
    PvGreedy,

    /// <summary>Reaktiv: am Netzanschlusspunkt gegen das wirtschaftliche Peak-Ziel fahren (Spezifikation 5.1).</summary>
    PeakShaving,

    /// <summary>Planend: lexikografisch Netzbezug, dann PV-Abregelung, dann gespeicherte Energie minimieren (Spezifikation 6.4).</summary>
    PvPlanung,

    /// <summary>Planend: prognostizierte Arbeits-, Leistungs-, Export-, Verschleiss- und Endenergiekosten minimieren (Spezifikation 6.3).</summary>
    Arbitrage,

    /// <summary>Planend: zuerst die groesste Peak-Verletzung minimieren, danach die wirtschaftlichen Kosten (Spezifikation 6.5).</summary>
    MultiUse
}

/// <summary>
/// Die Aufteilung einer reaktiven Gesamtanforderung auf die Einheiten der Flotte
/// (Spezifikation 5.4). Fuer planende Ziele gilt sie nicht — dort ist die Leistung
/// jeder Einheit eine eigene Variable des MILP.
/// </summary>
public enum FlottenVerteilung
{
    /// <summary>Anteilig nach nutzbarer Energie beim Entladen und freiem Energieplatz beim Laden; begrenzte Einheiten fallen aus der Verteilmenge, ihr Rest geht deterministisch an die uebrigen.</summary>
    KapazitaetsProportional,

    /// <summary>Die Einheiten nacheinander auslasten, in stabiler Konfigurationsreihenfolge.</summary>
    Kaskade,

    /// <summary>Beim Entladen nach Grenzverschleiss und Wirkungsgrad, beim Laden nach Wirkungsgrad sortieren; greedy, ohne Anspruch auf globale Kostenoptimalitaet.</summary>
    Grenzkosten
}

/// <summary>
/// Welche zwei Groessen eine Suchachse der Auslegung aufspannt.
/// </summary>
/// <remarks>
/// <para><b>Es gibt genau EINE gueltige Kopplung:</b>
/// <see cref="KapazitaetUndLeistung"/>. Der Anwender gibt einen Bereich fuer Kapazitaet
/// und fuer Leistung vor; die C-Rate ist kein Eingabewert mehr, sondern die ABGELEITETE
/// Kennzahl <c>C = P / E</c> jedes gefundenen Geraets.</para>
/// <para><b>Die zwei C-Rate-Kopplungen sind NUR NOCH LESEWERTE.</b> Ein vor der
/// Umstellung gespeicherter Stand traegt sie als Zahl 1 beziehungsweise 2; er soll sich
/// oeffnen lassen und nicht abstuerzen. <see cref="FlottenAltstand.Normalisiere(FlottenAuslegungsAchse)"/>
/// setzt sie BENANNT auf <see cref="KapazitaetUndLeistung"/> um und rechnet dabei die
/// gespeicherten C-Raten-Grenzen in Leistungs- beziehungsweise Kapazitaetsgrenzen um —
/// die Absicht des Anwenders bleibt erhalten, statt still verworfen zu werden. Eine
/// Oberflaeche bietet sie nicht mehr an.</para>
/// </remarks>
public enum FlottenAuslegungsmodus
{
    /// <summary>Kapazitaet [kWh] und Leistung [kW] spannen die Suche auf — die einzige gueltige Kopplung.</summary>
    KapazitaetUndLeistung = 0,

    /// <summary>NUR ZUM LESEN aelterer Staende: Kapazitaet [kWh] und C-Rate [1/h]; wird auf <see cref="KapazitaetUndLeistung"/> umgesetzt.</summary>
    KapazitaetUndCRate = 1,

    /// <summary>NUR ZUM LESEN aelterer Staende: Leistung [kW] und C-Rate [1/h]; wird auf <see cref="KapazitaetUndLeistung"/> umgesetzt.</summary>
    LeistungUndCRate = 2
}

/// <summary>
/// WOHER die Geraete kommen, unter denen die Groessensuche waehlt (Anwenderentscheid
/// vom 15.09.2026).
/// </summary>
/// <remarks>
/// Gesucht wird nicht mehr eine freie Groesse, sondern ein GERAET: Der Anwender gibt
/// Bereiche fuer Kapazitaet und Leistung vor, und die Suche rechnet die Speicher durch,
/// die es wirklich gibt. Welche Saetze das sind, sagt diese Quelle; die Aufloesung
/// selbst gehoert in den Kern — die Engine kennt keine Datenbank.
/// </remarks>
public enum FlottenKandidatenquelle
{
    /// <summary>Die Speicher des offenen PROJEKTS (Projektkatalog).</summary>
    Projektkatalog = 0,

    /// <summary>Die Speicher der STAMMDATEN; ein gefundenes Geraet laesst sich danach ins Projekt uebernehmen.</summary>
    Stammdaten = 1
}

/// <summary>
/// Die NEUTRALEN VORGABEN eines Geraets, das seine Kennwerte nicht fuehrt
/// (Anwenderentscheid vom 15.09.2026) — an EINER Stelle.
/// </summary>
/// <remarks>
/// <para>Die Parameter der Suche kommen vom GERAET: Wirkungsgrade, SoC-Fenster und
/// Alterung stehen am Kandidaten, dafuer sind es Geraete. Fehlt einem Satz etwas, wird
/// nicht geraten und nicht aus einem fremden Geraet geliehen, sondern es gilt die hier
/// hinterlegte neutrale Vorgabe — und der Kandidat wird in der Kandidatentabelle
/// GEKENNZEICHNET (<see cref="FlottenGeraetekandidat.NeutraleKennwerte"/>).</para>
/// <para>Es sind dieselben Werte, die <see cref="FlottenEinheit"/> seit jeher als
/// Eigenschaftsinitialisierer traegt; die Initialisierer verweisen auf diese Konstanten,
/// damit es keine zweite Wahrheit gibt. „Keine Alterung" heisst: leere
/// <see cref="FlottenEinheit.RainflowKurve"/> und Grenzverschleiss 0 — ein Geraet ohne
/// Lebensdauerkurve bekommt keine erfundene.</para>
/// </remarks>
public static class FlottenGeraetevorgaben
{
    /// <summary>Ladewirkungsgrad eta_c [-] eines Geraets ohne eigene Angabe: 0,95.</summary>
    public const double LADEWIRKUNGSGRAD = 0.95;

    /// <summary>Entladewirkungsgrad eta_d [-] eines Geraets ohne eigene Angabe: 0,95.</summary>
    public const double ENTLADEWIRKUNGSGRAD = 0.95;

    /// <summary>Untere SoC-Marke [-] eines Geraets ohne eigenes Band: 0,10.</summary>
    public const double SOC_MIN = 0.10;

    /// <summary>Obere SoC-Marke [-] eines Geraets ohne eigenes Band: 0,90.</summary>
    public const double SOC_MAX = 0.90;

    /// <summary>
    /// Traegt das Geraet ein brauchbares Richtungswirkungsgradpaar? Sonst gelten
    /// <see cref="LADEWIRKUNGSGRAD"/> und <see cref="ENTLADEWIRKUNGSGRAD"/>.
    /// </summary>
    public static bool WirkungsgradGefuehrt(double lade, double entlade)
        => lade > 0.0 && lade <= 1.0 && entlade > 0.0 && entlade <= 1.0;

    /// <summary>Traegt das Geraet ein brauchbares SoC-Band? Sonst gelten <see cref="SOC_MIN"/> und <see cref="SOC_MAX"/>.</summary>
    public static bool SocBandGefuehrt(double min, double max)
        => min >= 0.0 && max <= 1.0 && max > min;

    /// <summary>
    /// Fuellt die LUECKEN einer Geraeteeinheit mit den neutralen Vorgaben und sagt, ob
    /// eine Luecke zu fuellen war.
    /// </summary>
    /// <param name="einheit">Die aus dem Katalog- oder Anlagensatz gebaute Einheit.</param>
    /// <returns><c>true</c>, wenn wenigstens eine neutrale Vorgabe greifen musste.</returns>
    public static bool LueckenFuellen(FlottenEinheit einheit)
    {
        if (einheit is null) return false;
        bool gefuellt = false;

        if (!WirkungsgradGefuehrt(einheit.Ladewirkungsgrad, einheit.Entladewirkungsgrad))
        {
            einheit.Ladewirkungsgrad = LADEWIRKUNGSGRAD;
            einheit.Entladewirkungsgrad = ENTLADEWIRKUNGSGRAD;
            gefuellt = true;
        }
        if (!SocBandGefuehrt(einheit.SocMin, einheit.SocMax))
        {
            einheit.SocMin = SOC_MIN;
            einheit.SocMax = SOC_MAX;
            gefuellt = true;
        }
        if (einheit.SocStart < einheit.SocMin) einheit.SocStart = einheit.SocMin;
        if (einheit.SocStart > einheit.SocMax) einheit.SocStart = einheit.SocMax;
        return gefuellt;
    }
}

/// <summary>
/// WELCHE KENNWERTE EINES KANDIDATEN VOM GERAET STAMMEN — und welche damit beim Anwender
/// bleiben (Anwenderentscheid 15.09.2026).
/// </summary>
/// <remarks>
/// Die Aufzaehlung ist die eine Herleitung: Was hier steht, hat der Geraetesatz wirklich
/// gefuehrt; was fehlt, kommt aus der Vorlage des Anwenders (Schritt 1). Die
/// Kandidatentabelle zeigt sie als Herleitungszeile.
/// </remarks>
[Flags]
public enum FlottenKennwertherkunft
{
    /// <summary>Nichts vom Geraet — der Fall gibt es nur ohne Geraetewahl.</summary>
    Keine = 0,

    /// <summary>Kapazitaet, Lade- und Entladeleistung. Sie kommen IMMER vom Geraet: Sie sind der Gegenstand der Suche.</summary>
    Groesse = 1,

    /// <summary>Lade- und Entladewirkungsgrad.</summary>
    Wirkungsgrade = 2,

    /// <summary>Das SoC-Band samt Start-Ladezustand.</summary>
    SocBand = 4,

    /// <summary>Der Hilfsverbrauch.</summary>
    Hilfsverbrauch = 8,

    /// <summary>Die drei Investitionssaetze (fix, je kWh, je kW).</summary>
    Kosten = 16
}

/// <summary>
/// <b>GERAET ODER EIGENE PARAMETER — die EINE Regel</b> (Anwenderentscheid 15.09.2026).
/// </summary>
/// <remarks>
/// <para><b>Die Entscheidung in einem Satz:</b> Was das Geraet mitbringt, kommt vom
/// Geraet; was der Anwender in Schritt 1 gesetzt hat, bleibt seins. Es gibt keinen
/// Vorrang je Feld auszuhandeln — ein Geraetesatz FUEHRT einen Kennwert oder er fuehrt
/// ihn nicht, und nur im ersten Fall ueberschreibt er die Vorlage.</para>
/// <para><b>Was das Geraet immer mitbringt</b>, ist seine GROESSE: Kapazitaet, Lade- und
/// Entladeleistung. Sie ist der Gegenstand der Suche; eine Groesse aus der Vorlage waere
/// keine Suche.</para>
/// <para><b>Was ein Geraetesatz nie fuehrt</b> — Peak-Reserve, Grenzverschleiss,
/// Betriebs- und Durchsatzkosten, Ersatz, Restwert und die Alterungskurve —, bleibt
/// unangetastet in der Vorlage stehen. Ohne diese Regel fielen die Eingaben des
/// Anwenders bei jeder Geraetewahl weg.</para>
/// <para><b>Welche Kennwerte ein Satz fuehrt, sagt der Kern</b>
/// (<see cref="FlottenGeraetekandidat.Gefuehrt"/>): Er liest die Quelle und weiss als
/// Einziger, ob ein Wert aus dem Satz oder aus einer neutralen Vorgabe stammt. Die
/// Engine raet es nicht aus dem Zahlenwert.</para>
/// </remarks>
public static class FlottenGeraeteuebernahme
{
    /// <summary>
    /// WAS EIN GERAETESATZ FUEHRT — gelesen an der frisch gebauten Einheit, BEVOR
    /// <see cref="FlottenGeraetevorgaben.LueckenFuellen"/> darueber gelaufen ist.
    /// </summary>
    /// <remarks>
    /// <para>Ein Kennwert gilt als GEFUEHRT, wenn er brauchbar ist (derselbe Test, mit
    /// dem die neutralen Vorgaben entscheiden, ob sie greifen muessen) UND nicht genau
    /// auf der neutralen Vorgabe liegt. Der zweite Teil ist noetig, weil die Quellen die
    /// Luecken eines Satzes bereits mit ebendieser Vorgabe schliessen — danach ist an der
    /// Zahl allein nicht mehr zu sehen, woher sie kommt. Ein Satz, der zufaellig genau
    /// die neutrale Vorgabe traegt, gilt damit als nicht gefuehrt; das aendert an seiner
    /// Rechnung nichts, denn Geraetewert und Vorgabe sind dieselbe Zahl.</para>
    /// <para>Die GROESSE steht immer darin: Ohne Kapazitaet und Leistung waere der Satz
    /// kein Geraet, und die Quellen lassen einen solchen Satz gar nicht erst durch.</para>
    /// </remarks>
    /// <param name="roh">Die Einheit, wie die Quelle sie gebaut hat.</param>
    public static FlottenKennwertherkunft Gefuehrt(FlottenEinheit roh)
    {
        if (roh is null) return FlottenKennwertherkunft.Keine;

        var gefuehrt = FlottenKennwertherkunft.Groesse;
        if (FlottenGeraetevorgaben.WirkungsgradGefuehrt(roh.Ladewirkungsgrad, roh.Entladewirkungsgrad)
            && !(roh.Ladewirkungsgrad == FlottenGeraetevorgaben.LADEWIRKUNGSGRAD
                 && roh.Entladewirkungsgrad == FlottenGeraetevorgaben.ENTLADEWIRKUNGSGRAD))
            gefuehrt |= FlottenKennwertherkunft.Wirkungsgrade;
        if (FlottenGeraetevorgaben.SocBandGefuehrt(roh.SocMin, roh.SocMax)
            && !(roh.SocMin == FlottenGeraetevorgaben.SOC_MIN
                 && roh.SocMax == FlottenGeraetevorgaben.SOC_MAX))
            gefuehrt |= FlottenKennwertherkunft.SocBand;
        if (roh.HilfsverbrauchKw > 0.0)
            gefuehrt |= FlottenKennwertherkunft.Hilfsverbrauch;
        if (roh.EigeneKosten)
            gefuehrt |= FlottenKennwertherkunft.Kosten;
        return gefuehrt;
    }

    /// <summary>
    /// Traegt die Kennwerte des Geraets in eine aus der Vorlage gebaute Einheit ein.
    /// </summary>
    /// <param name="ziel">Die Einheit, die aus der Vorlage des Anwenders entstanden ist.</param>
    /// <param name="kandidat">Das gewaehlte Geraet samt der Auskunft, was sein Satz fuehrt.</param>
    /// <returns>Was wirklich vom Geraet kam — die Herleitung des Kandidaten.</returns>
    public static FlottenKennwertherkunft Uebernehmen(FlottenEinheit ziel,
                                                      FlottenGeraetekandidat kandidat)
    {
        if (ziel is null || kandidat?.Geraet is null) return FlottenKennwertherkunft.Keine;

        FlottenEinheit g = kandidat.Geraet;
        FlottenKennwertherkunft gefuehrt = kandidat.Gefuehrt;
        var herkunft = FlottenKennwertherkunft.Groesse;

        // DIE GROESSE KOMMT IMMER VOM GERAET.
        ziel.KapazitaetKWh = g.KapazitaetKWh;
        ziel.LadeleistungKw = g.LadeleistungKw;
        ziel.EntladeleistungKw = g.EntladeleistungKw;
        ziel.AnlageId = g.AnlageId;

        if (gefuehrt.HasFlag(FlottenKennwertherkunft.Wirkungsgrade))
        {
            ziel.Ladewirkungsgrad = g.Ladewirkungsgrad;
            ziel.Entladewirkungsgrad = g.Entladewirkungsgrad;
            herkunft |= FlottenKennwertherkunft.Wirkungsgrade;
        }

        if (gefuehrt.HasFlag(FlottenKennwertherkunft.SocBand))
        {
            ziel.SocMin = g.SocMin;
            ziel.SocMax = g.SocMax;
            ziel.SocStart = g.SocStart;
            herkunft |= FlottenKennwertherkunft.SocBand;
        }

        if (gefuehrt.HasFlag(FlottenKennwertherkunft.Hilfsverbrauch))
        {
            ziel.HilfsverbrauchKw = g.HilfsverbrauchKw;
            herkunft |= FlottenKennwertherkunft.Hilfsverbrauch;
        }

        if (gefuehrt.HasFlag(FlottenKennwertherkunft.Kosten))
        {
            ziel.EigeneKosten = true;
            ziel.InvestitionEuro = g.InvestitionEuro;
            ziel.InvestitionEuroProKWh = g.InvestitionEuroProKWh;
            ziel.InvestitionEuroProKw = g.InvestitionEuroProKw;
            herkunft |= FlottenKennwertherkunft.Kosten;
        }

        // Der Start-Ladezustand muss im geltenden Band liegen — gleichgueltig, aus
        // welcher der beiden Quellen Band und Start stammen.
        if (ziel.SocStart < ziel.SocMin) ziel.SocStart = ziel.SocMin;
        if (ziel.SocStart > ziel.SocMax) ziel.SocStart = ziel.SocMax;

        return herkunft;
    }
}

/// <summary>
/// EIN Geraet, das als Kandidat der Groessensuche in Frage kommt (Anwenderentscheid vom
/// 15.09.2026).
/// </summary>
/// <remarks>
/// Die Liste fuellt der Kern aus der gewaehlten <see cref="FlottenKandidatenquelle"/>;
/// WELCHE davon gerechnet werden, entscheidet allein
/// <see cref="FlottenGeraetewahl.Waehle"/> — dieselbe Regel fuer die Kandidatenzahl der
/// Seite, die Kandidatentabelle und den Lauf.
/// </remarks>
public sealed class FlottenGeraetekandidat
{
    /// <summary>Kennung des Satzes in seiner Quelle (Katalog-ID beziehungsweise Anlagen-ID als Text).</summary>
    public string Quellkennung { get; set; } = string.Empty;

    /// <summary>Das Geraet mit allen Kennwerten, die sein Satz fuehrt.</summary>
    public FlottenEinheit Geraet { get; set; } = new();

    /// <summary>
    /// An diesem Geraet musste wenigstens eine neutrale Vorgabe greifen
    /// (<see cref="FlottenGeraetevorgaben"/>); die Kandidatentabelle kennzeichnet es.
    /// </summary>
    public bool NeutraleKennwerte { get; set; }

    /// <summary>
    /// WAS DER SATZ WIRKLICH FUEHRT (<see cref="FlottenGeraeteuebernahme"/>). Gefuellt
    /// vom Kern beim Lesen der Quelle; <see cref="FlottenKennwertherkunft.Groesse"/>
    /// steht immer darin, denn ohne Kapazitaet und Leistung waere der Satz kein Geraet.
    /// </summary>
    public FlottenKennwertherkunft Gefuehrt { get; set; } = FlottenKennwertherkunft.Groesse;

    /// <summary>
    /// Der NORMIERTE ABSTAND zur Vorgabe des Anwenders; <c>0</c> = das Geraet liegt in
    /// beiden Bereichen. Gefuellt von <see cref="FlottenGeraetewahl.Waehle"/>.
    /// </summary>
    public double Abweichung { get; set; }

    /// <summary>Das Geraet liegt in BEIDEN vorgegebenen Bereichen.</summary>
    public bool Treffer => Abweichung <= 0.0;

    /// <summary>Kapazitaet [kWh] des Geraets.</summary>
    public double KapazitaetKWh => Geraet?.KapazitaetKWh ?? 0.0;

    /// <summary>Entladeleistung [kW] des Geraets — die Groesse der zweiten Achse.</summary>
    public double LeistungKw => Geraet?.EntladeleistungKw ?? 0.0;
}

/// <summary>
/// Die feste Reihenfolge, in der eine DIREKTE Netzeinspeisung auf PV und BHKW
/// aufgeteilt wird. Sie macht den Direktexport reproduzierbar; eine Energieherkunft
/// im Speicher fuehrt das Modell ausdruecklich nicht.
/// </summary>
public enum FlottenErzeugerPrioritaet
{
    /// <summary>PV speist zuerst ein, das BHKW deckt den Rest.</summary>
    PvVorBhkw,

    /// <summary>Das BHKW speist zuerst ein, PV deckt den Rest.</summary>
    BhkwVorPv
}

/// <summary>
/// Der Informationsstand, auf dem ein Fahrplan beruht. Er wird ausgewiesen, damit
/// Laeufe mit unterschiedlichem Wissen nicht ungekennzeichnet in dieselbe Rangfolge
/// geraten (Spezifikation 9.4).
/// </summary>
public enum PrognoseArt
{
    /// <summary>Nachweislich vorher bekannt: <c>BekanntSeit &lt;= Entscheidungszeitpunkt</c>, archivierter Snapshot, lueckenlos ueber den Horizont.</summary>
    VerifiziertBekannt,

    /// <summary>Ausdruecklich gewaehltes Idealwissen aus der Istreihe — eine optimistische Vergleichsgrenze, kein historisch erreichbarer Fahrplan.</summary>
    Oracle
}

/// <summary>
/// Das Ergebnis eines Planungslaufs. Die Stufen bleiben unterscheidbar, damit ein
/// Solverfehler nicht als optimierter Nullfahrplan erscheint (Spezifikation 6.5).
/// </summary>
public enum FlottenPlanStatus
{
    /// <summary>Beweisbar optimal — und zwar allein fuer das formulierte endliche Horizontproblem.</summary>
    Optimal,

    /// <summary>Zulaessige Loesung ohne Optimalitaetsnachweis, etwa ein Incumbent beim Zeitlimit.</summary>
    Zulaessig,

    /// <summary>Das Horizontproblem hat keine zulaessige Loesung.</summary>
    Unzulaessig,

    /// <summary>Das Zeitlimit lief ab, ohne dass eine zulaessige Loesung vorlag.</summary>
    Zeitlimit,

    /// <summary>Der Planer ist gescheitert; der Grund steht in <see cref="FlottenPlan.StatusText"/>.</summary>
    Fehler
}

/// <summary>
/// Die Zielformulierung, mit der der Simulator einen <see cref="IFlottenPlaner"/>
/// beauftragt. Sie leitet sich aus dem planenden <see cref="FlottenBetriebsziel"/> ab.
/// </summary>
public enum FlottenPlanerModus
{
    /// <summary>Lexikografisch Netzbezug, PV-Abregelung und gespeicherte Energie minimieren; Netzladung und Batterieexport sind ausgeschlossen.</summary>
    PvPlanung,

    /// <summary>Die wirtschaftliche Zielfunktion des Horizonts minimieren.</summary>
    Arbitrage,

    /// <summary>Zuerst die unvermeidbare Peak-Ueberschreitung minimieren, diese fixieren und darunter wirtschaftlich optimieren.</summary>
    MultiUse
}

/// <summary>
/// Die Vorgabe fuer den Energiestand am Ende des Rechenzeitraums (Spezifikation 9.2).
/// Sie verhindert, dass ein zu Beginn gefuellter und am Ende leerer Speicher einen
/// kuenstlichen Vorteil erzeugt.
/// </summary>
public enum FlottenEndbedingung
{
    /// <summary>Keine Vorgabe; die Endenergieaenderung wird stattdessen ueber <see cref="FlottenSimulationOptionen.EnergieAusgleichEuroProKWh"/> bewertet.</summary>
    KeineVorgabe,

    /// <summary>Je Speicher ist der Zielwert aus <see cref="FlottenSimulationOptionen.EndenergieZielKWh"/> zu erreichen.</summary>
    JeSpeicherZiel,

    /// <summary>Je Speicher ist die Anfangsenergie wieder zu erreichen (zyklischer Abschluss).</summary>
    JeSpeicherWieAnfang
}

/// <summary>
/// EINE physische AC-Speichereinheit der Flotte — Technik, Grenzen, Verschleiss und
/// Kosten (Spezifikation 4 und 9).
/// </summary>
/// <remarks>
/// <para>
/// Einheiten: Energie [kWh], Leistung [kW AC], Wirkungsgrade und SoC-Marken als Bruch
/// (0,95 = 95 %), Preise und Betraege [EUR]. Alle Leistungen liegen AC-seitig vor;
/// die Wirkungsgrade beziehen sich auf denselben AC-Speicher-AC-Pfad.
/// </para>
/// <para>
/// <see cref="KapazitaetKWh"/> ist die VOLLBEREICHSkapazitaet. Nennt ein Hersteller nur
/// die innerhalb seines eigenen SoC-Fensters nutzbare Energie, wird sie nicht noch
/// einmal um dasselbe Fenster gekuerzt (Spezifikation 4.1).
/// </para>
/// </remarks>
public sealed class FlottenEinheit
{
    /// <summary>Stabile Kennung der Einheit; sie verbindet Konfiguration, Kennzahlen, Verfuegbarkeitsreihen und Suchachsen.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Anzeigename der Einheit; er hat keine Rechenwirkung.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Bezug auf die Energieanlage des Projekts; <c>null</c> bei einer nur im Studienstand gefuehrten Einheit.</summary>
    public string? AnlageId { get; set; }

    /// <summary>Die Einheit bringt EIGENE Kostensaetze mit. Ist der Schalter aus, belegt der Kern die Kostenfelder aus den gemeinsam verwendeten Projektkosten.</summary>
    public bool EigeneKosten { get; set; }

    /// <summary>Vollbereichskapazitaet C [kWh] beim aktuellen Alterungszustand.</summary>
    public double KapazitaetKWh { get; set; }

    /// <summary>Hoechste Ladeleistung P_c [kW AC], nicht negativ.</summary>
    public double LadeleistungKw { get; set; }

    /// <summary>Hoechste Entladeleistung P_d [kW AC], nicht negativ. Lade- und Entladerichtung werden getrennt begrenzt.</summary>
    public double EntladeleistungKw { get; set; }

    /// <summary>Ladewirkungsgrad eta_c [-], Standard <see cref="FlottenGeraetevorgaben.LADEWIRKUNGSGRAD"/>. Aus einem reinen Rundlaufwert folgt <c>eta_c = eta_d = sqrt(eta_RT)</c>.</summary>
    public double Ladewirkungsgrad { get; set; } = FlottenGeraetevorgaben.LADEWIRKUNGSGRAD;

    /// <summary>Entladewirkungsgrad eta_d [-], Standard <see cref="FlottenGeraetevorgaben.ENTLADEWIRKUNGSGRAD"/>. Gemeinsam mit <see cref="Ladewirkungsgrad"/> ergibt sich <c>eta_RT = eta_c * eta_d</c>.</summary>
    public double Entladewirkungsgrad { get; set; } = FlottenGeraetevorgaben.ENTLADEWIRKUNGSGRAD;

    /// <summary>Untere SoC-Marke [-] des nutzbaren Bands, Standard <see cref="FlottenGeraetevorgaben.SOC_MIN"/>; die technische Mindestenergie ist <c>SocMin * KapazitaetKWh</c>.</summary>
    public double SocMin { get; set; } = FlottenGeraetevorgaben.SOC_MIN;

    /// <summary>Obere SoC-Marke [-] des nutzbaren Bands, Standard <see cref="FlottenGeraetevorgaben.SOC_MAX"/>; die hoechste Energie ist <c>SocMax * KapazitaetKWh</c>.</summary>
    public double SocMax { get; set; } = FlottenGeraetevorgaben.SOC_MAX;

    /// <summary>SoC [-] zu Beginn des Rechenzeitraums, Standard 0,50. Bei mehreren Projektjahren wird der Endstand des Vorjahres hier eingesetzt.</summary>
    public double SocStart { get; set; } = 0.50;

    /// <summary>
    /// Geschuetzte Peak-Reserve [kWh] oberhalb der technischen Mindestenergie
    /// (Spezifikation 8.4). Im Normalbetrieb hebt sie die Entladeuntergrenze an; bei
    /// einer tatsaechlichen Peak-Anforderung darf die Ausfuehrung sie bis
    /// <c>SocMin * KapazitaetKWh</c> freigeben. Der Planer haelt sie im Horizont
    /// konservativ vor.
    /// </summary>
    public double PeakReserveKWh { get; set; }

    /// <summary>Hilfsverbrauch A [kW AC] der Einheit; er zaehlt als zusaetzliche Standortlast und wird nicht nochmals im Wirkungsgrad gefuehrt.</summary>
    public double HilfsverbrauchKw { get; set; }

    /// <summary>Grenzverschleiss [EUR je abgegebener AC-kWh] (Spezifikation 8.1). Er ist ein ENTSCHEIDUNGSpreis der Zielfunktion und wird nicht zusaetzlich als Auszahlung gebucht.</summary>
    public double GrenzverschleissEuroProKWhEntladung { get; set; }

    /// <summary>Leistungs- und kapazitaetsunabhaengiger Investitionsanteil [EUR].</summary>
    public double InvestitionEuro { get; set; }

    /// <summary>Kapazitaetsbezogene Investition [EUR/kWh], bezogen auf <see cref="KapazitaetKWh"/>.</summary>
    public double InvestitionEuroProKWh { get; set; }

    /// <summary>Leistungsbezogene Investition [EUR/kW], bezogen auf das MAXIMUM aus Lade- und Entladeleistung.</summary>
    public double InvestitionEuroProKw { get; set; }

    /// <summary>Fester jaehrlicher Betriebsaufwand [EUR/a].</summary>
    public double JaehrlicheFixeOpexEuro { get; set; }

    /// <summary>Kapazitaetsbezogener jaehrlicher Betriebsaufwand [EUR/(kWh*a)].</summary>
    public double JaehrlicheOpexEuroProKWhKapazitaet { get; set; }

    /// <summary>Leistungsbezogener jaehrlicher Betriebsaufwand [EUR/(kW*a)], bezogen auf das MAXIMUM aus Lade- und Entladeleistung.</summary>
    public double JaehrlicheOpexEuroProKw { get; set; }

    /// <summary>Durchsatzkosten [EUR je abgegebener AC-kWh]; sie werden je Jahreskonto auf die tatsaechliche AC-Entladung angesetzt.</summary>
    public double DurchsatzkostenEuroProKWhEntladung { get; set; }

    /// <summary>Kosten einer Ersatzbeschaffung [EUR]; sie werden im faelligen Jahr in voller Hoehe gebucht.</summary>
    public double ErsatzkostenEuro { get; set; }

    /// <summary>
    /// Ersatzintervall = Nutzungsdauer der Einheit [a]; 0 bedeutet „keine eigene Angabe".
    /// Faellig ist jedes Jahr, dessen Nummer ohne Rest durch diesen Wert teilbar ist; aus
    /// demselben Wert folgt der lineare Restwert
    /// (<see cref="FlottenWirtschaftlichkeit.LinearerRestwert"/>). ETAPPE E10: Vor einer
    /// Bewertung setzt der Kern bei 0 die Nutzungsdauer der Nutzungsdauertabelle
    /// (Stromspeicher, Standardzeile) ein; bleibt es 0, gibt es weder Ersatz noch Restwert.
    /// </summary>
    public int ErsatzintervallJahre { get; set; }

    /// <summary>
    /// ALTFELD (Etappe E10, Empfehlung E10-Q3 a): ein fester Restwert der Einheit [EUR] als
    /// Geraetedatum — <b>nicht mehr rechenwirksam</b>. Der Kapitalwert setzt den Restwert je
    /// Einheit linear aus <see cref="ErsatzintervallJahre"/> an
    /// (<see cref="FlottenWirtschaftlichkeit.LinearerRestwert"/>). Das Feld bleibt im
    /// gespeicherten Stand, damit ein gepflegter Wert nicht verloren geht.
    /// </summary>
    public double RestwertEuro { get; set; }

    /// <summary>
    /// Lebensdauerkurve als Stuetzstellen (Entladetiefe, Zyklen bis zum
    /// End-of-Life-Kriterium). Eine leere Liste bedeutet: Zyklen werden gezaehlt,
    /// aber kein Miner-Schaden berechnet. Ausserhalb der gelieferten Stuetzstellen
    /// bricht die Auswertung ab, statt zu extrapolieren.
    /// </summary>
    public List<FlottenRainflowPunkt> RainflowKurve { get; set; } = new();
}

/// <summary>Eine Stuetzstelle der Lebensdauerkurve (Spezifikation 8.2).</summary>
public sealed class FlottenRainflowPunkt
{
    /// <summary>Zyklustiefe DoD [-] im Bereich groesser 0 bis 1.</summary>
    public double Entladetiefe { get; set; }

    /// <summary>Erreichbare Zyklenzahl bei dieser Tiefe bis zum vereinbarten End-of-Life-Kriterium; zwischen den Stuetzstellen wird logarithmisch interpoliert.</summary>
    public double ZyklenBisEol { get; set; }
}

/// <summary>
/// EIN Viertelstundenintervall des Standorts: Last, Erzeugung und Preise
/// (Spezifikation 3.1 und 4.1).
/// </summary>
/// <remarks>
/// Alle Leistungen sind AC-seitige, nicht negative Mittelwerte des Intervalls [kW],
/// alle Preise [EUR/kWh]; negative Bezugspreise sind ausdruecklich zulaessig. Die
/// Reihe traegt UTC-Zeitstempel im Viertelstundenraster.
/// </remarks>
public sealed class FlottenNetzintervall
{
    /// <summary>Beginn des Intervalls; die Reihe ist lueckenlos und aufsteigend, die Intervalldauer betraegt 0,25 h.</summary>
    public DateTimeOffset Zeitstempel { get; set; }

    /// <summary>Bruttolast L [kW] des Standorts ohne Speicher und ohne Erzeugerabzug.</summary>
    public double LastKw { get; set; }

    /// <summary>Verfuegbare PV-AC-Leistung [kW] vor jeder Abregelung.</summary>
    public double PvKw { get; set; }

    /// <summary>BHKW-Leistung [kW] aus dem vorbereiteten Projektlauf; sie ist Eingabe, kein Optimierungsfreiheitsgrad.</summary>
    public double BhkwKw { get; set; }

    /// <summary>Arbeitspreis des Netzbezugs [EUR/kWh] in diesem Intervall.</summary>
    public double BezugspreisEuroProKWh { get; set; }

    /// <summary>Verguetung der PV-Netzeinspeisung [EUR/kWh].</summary>
    public double PvVerkaufspreisEuroProKWh { get; set; }

    /// <summary>Verguetung der BHKW-Netzeinspeisung [EUR/kWh].</summary>
    public double BhkwVerkaufspreisEuroProKWh { get; set; }

    /// <summary>Verguetung der Batterieeinspeisung [EUR/kWh]; <see cref="FlottenTarif.BatterieVerkaufspreisEuroProKWh"/> hat Vorrang, wenn er gesetzt ist. Der Bezugspreis wird nie als Verkaufspreis verwendet.</summary>
    public double BatterieVerkaufspreisEuroProKWh { get; set; }
}

/// <summary>Unveraenderlicher Informationsstand, der einem Planer nachweislich bekannt war.</summary>
/// <remarks>
/// Der Konstruktor KOPIERT die gelieferten Intervalle; ein Horizontausschnitt
/// (<see cref="Ausschnitt"/>) teilt anschliessend nur noch diesen unveraenderten
/// Laufpuffer und legt keine neuen Zeilen an. Fuer
/// <see cref="PrognoseArt.VerifiziertBekannt"/> muss die Art passen,
/// <see cref="BekanntSeit"/> vor oder auf dem Entscheidungszeitpunkt liegen und der
/// Snapshot den ganzen Horizont lueckenlos abdecken; die Istreihe wird nie still als
/// historische Prognose verwendet.
/// </remarks>
public sealed class FlottenPrognoseSnapshot
{
    private readonly FlottenNetzintervall[] _intervalle;

    /// <summary>Legt einen Snapshot an und kopiert die gelieferten Intervalle in einen eigenen Puffer.</summary>
    /// <param name="id">Kennung des Snapshots; sie erscheint im Ergebnis jedes damit geplanten Intervalls.</param>
    /// <param name="bekanntSeit">Zeitpunkt, ab dem dieser Stand vorlag.</param>
    /// <param name="entscheidungszeitpunkt">Zeitpunkt, fuer den der Stand verwendet wird.</param>
    /// <param name="art">Verifiziert bekannter Stand oder ausdruecklich gewaehltes Idealwissen.</param>
    /// <param name="intervalle">Die Viertelstundenwerte des Snapshots.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> oder <paramref name="intervalle"/> ist <c>null</c>.</exception>
    public FlottenPrognoseSnapshot(
        string id,
        DateTimeOffset bekanntSeit,
        DateTimeOffset entscheidungszeitpunkt,
        PrognoseArt art,
        IReadOnlyList<FlottenNetzintervall> intervalle)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        BekanntSeit = bekanntSeit;
        Entscheidungszeitpunkt = entscheidungszeitpunkt;
        Art = art;
        _intervalle = new FlottenNetzintervall[intervalle?.Count ?? throw new ArgumentNullException(nameof(intervalle))];
        for (var i = 0; i < _intervalle.Length; i++)
        {
            var x = intervalle[i];
            _intervalle[i] = new FlottenNetzintervall
            {
                Zeitstempel = x.Zeitstempel,
                LastKw = x.LastKw,
                PvKw = x.PvKw,
                BhkwKw = x.BhkwKw,
                BezugspreisEuroProKWh = x.BezugspreisEuroProKWh,
                PvVerkaufspreisEuroProKWh = x.PvVerkaufspreisEuroProKWh,
                BhkwVerkaufspreisEuroProKWh = x.BhkwVerkaufspreisEuroProKWh,
                BatterieVerkaufspreisEuroProKWh = x.BatterieVerkaufspreisEuroProKWh
            };
        }

        Intervalle = Array.AsReadOnly(_intervalle);
    }

    private FlottenPrognoseSnapshot(string id, DateTimeOffset bekanntSeit,
        DateTimeOffset entscheidungszeitpunkt, PrognoseArt art,
        FlottenNetzintervall[] owned, int start, int count)
    {
        Id = id;
        BekanntSeit = bekanntSeit;
        Entscheidungszeitpunkt = entscheidungszeitpunkt;
        Art = art;
        _intervalle = owned;
        Intervalle = new ArraySegment<FlottenNetzintervall>(owned, start, count);
    }

    /// <summary>Kennung des Snapshots; sie wird je geplantem Intervall als <see cref="FlottenIntervallErgebnis.PrognoseId"/> ausgewiesen.</summary>
    public string Id { get; }

    /// <summary>Zeitpunkt, ab dem dieser Stand bekannt war. Ein verifiziert bekannter Snapshot darf nichts enthalten, was erst danach bekannt wurde.</summary>
    public DateTimeOffset BekanntSeit { get; }

    /// <summary>Zeitpunkt, fuer den dieser Stand gilt beziehungsweise ausgeschnitten wurde.</summary>
    public DateTimeOffset Entscheidungszeitpunkt { get; }

    /// <summary>Verifiziert bekannter Stand oder ausdruecklich gewaehltes Idealwissen.</summary>
    public PrognoseArt Art { get; }

    /// <summary>Die Viertelstundenwerte des Snapshots beziehungsweise des Ausschnitts.</summary>
    public IReadOnlyList<FlottenNetzintervall> Intervalle { get; }

    /// <summary>Schneidet den Planungshorizont ab einem Entscheidungszeitpunkt heraus, ohne Zeilen zu kopieren.</summary>
    /// <param name="entscheidungszeitpunkt">Erstes Intervall des Horizonts; es muss im Snapshot vorkommen.</param>
    /// <param name="maximalIntervalle">Hoechstlaenge des Horizonts; das Ende des Snapshots begrenzt sie zusaetzlich.</param>
    /// <returns>Ein Snapshot mit denselben Kenndaten, dessen <see cref="Intervalle"/> nur den Horizont zeigen.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximalIntervalle"/> ist kleiner oder gleich 0.</exception>
    /// <exception cref="ArgumentException">Der Snapshot deckt den Entscheidungszeitpunkt nicht ab.</exception>
    public FlottenPrognoseSnapshot Ausschnitt(DateTimeOffset entscheidungszeitpunkt, int maximalIntervalle)
    {
        if (maximalIntervalle <= 0) throw new ArgumentOutOfRangeException(nameof(maximalIntervalle));
        var start = Array.FindIndex(_intervalle, x => x.Zeitstempel == entscheidungszeitpunkt);
        if (start < 0) throw new ArgumentException("Snapshot deckt den Entscheidungszeitpunkt nicht ab.");
        return new FlottenPrognoseSnapshot(Id, BekanntSeit, entscheidungszeitpunkt, Art,
            _intervalle, start, Math.Min(maximalIntervalle, _intervalle.Length - start));
    }
}

/// <summary>
/// Der Auftrag an einen <see cref="IFlottenPlaner"/> fuer EINEN Horizont
/// (Spezifikation 6.1).
/// </summary>
public sealed class FlottenPlanAnfrage
{
    /// <summary>Erstes Intervall des Horizonts; der gelieferte Fahrplan muss genau hier beginnen.</summary>
    public DateTimeOffset Entscheidungszeitpunkt { get; set; }

    /// <summary>Die Zielformulierung des Horizontproblems.</summary>
    public FlottenPlanerModus Modus { get; set; }

    /// <summary>Der Horizontausschnitt des gewaehlten Snapshots; seine Laenge bestimmt die Zahl der zu planenden Intervalle.</summary>
    public FlottenPrognoseSnapshot Prognose { get; set; } = null!;

    /// <summary>Die Einheiten der Flotte in stabiler Reihenfolge; alle Listen je Speicher folgen dieser Reihenfolge.</summary>
    public List<FlottenEinheit> Einheiten { get; set; } = new();

    /// <summary>Der TATSAECHLICH erreichte Energiestand [kWh] je Speicher zu Beginn des Horizonts — nicht der geplante.</summary>
    public List<double> EnergieKWh { get; set; } = new();

    /// <summary>Je Speicher und Prognoseintervall ein Faktor von 0 bis 1.</summary>
    /// <remarks>Er skaliert Lade- und Entladegrenze der Einheit; 0 bedeutet, dass die Einheit in diesem Intervall nicht verfuegbar ist. Eine leere Liste bedeutet volle Verfuegbarkeit.</remarks>
    public List<List<double>> VerfuegbarkeitsfaktorJeSpeicher { get; set; } = new();

    /// <summary>Die Betriebsoptionen des Laufs; aus ihnen stammen Netzgrenzen, Freigaben, Peak-Ziel und Erzeugerprioritaet.</summary>
    public FlottenSimulationOptionen Optionen { get; set; } = new();

    /// <summary>Der in dieser Abrechnungsperiode bereits erreichte Hoechstbezug [kW]. Ein bereits bezahlter Peak kann nicht nachtraeglich eingespart werden.</summary>
    public double BisherigerAbrechnungspeakKw { get; set; }

    /// <summary>Leistungspreis [EUR/kW] der Abrechnungsperiode; er bewertet im Horizontziel nur den Zuwachs ueber <see cref="BisherigerAbrechnungspeakKw"/> hinaus.</summary>
    public double LeistungspreisEuroProKw { get; set; }

    /// <summary>Bewertung der am Horizontende gespeicherten Energie [EUR/kWh]; sie verhindert eine kostenlose Entleerung am Horizontende.</summary>
    public double EndenergieAusgleichEuroProKWh { get; set; }

    /// <summary>Zeitlimit des Planungslaufs, Standard 60 s. Laeuft es ab, bleibt ein Incumbent <see cref="FlottenPlanStatus.Zulaessig"/>, sonst gilt <see cref="FlottenPlanStatus.Zeitlimit"/>.</summary>
    public TimeSpan Zeitlimit { get; set; } = TimeSpan.FromSeconds(60);
}

/// <summary>Der Fahrplan EINES Horizonts, wie ihn ein <see cref="IFlottenPlaner"/> liefert.</summary>
public sealed class FlottenPlan
{
    /// <summary>Kennung des Fahrplans; sie erscheint je ausgefuehrtem Intervall als <see cref="FlottenIntervallErgebnis.PlanId"/>.</summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>Kennung des Snapshots, auf dem der Fahrplan beruht (<see cref="FlottenPrognoseSnapshot.Id"/>).</summary>
    public string PrognoseId { get; set; } = string.Empty;

    /// <summary>Erstes Intervall des Horizonts; es muss zur Anfrage passen.</summary>
    public DateTimeOffset Entscheidungszeitpunkt { get; set; }

    /// <summary>Der Loesungsstand des Horizontproblems.</summary>
    public FlottenPlanStatus Status { get; set; }

    /// <summary>Klartext zum Status, insbesondere der Grund bei <see cref="FlottenPlanStatus.Fehler"/>; sonst <c>null</c>.</summary>
    public string? StatusText { get; set; }

    /// <summary>Der erreichte Zielfunktionswert [EUR] des Horizonts. Er ist eine Planungsgroesse, keine Rechnung.</summary>
    public double ZielfunktionswertEuro { get; set; }

    /// <summary>Im Horizont unvermeidbare Ueberschreitung [kW] einer harten Bezugsgrenze; sie wird ausgewiesen statt versteckt.</summary>
    public double GeplanteImportverletzungKw { get; set; }

    /// <summary>Die geplanten Intervalle in zeitlicher Reihenfolge, lueckenlos ab <see cref="Entscheidungszeitpunkt"/>.</summary>
    public List<FlottenPlanIntervall> Intervalle { get; set; } = new();
}

/// <summary>Ein geplantes Intervall des Fahrplans.</summary>
/// <remarks>
/// Lade- und Entladeleistung stehen GETRENNT und sind beide nicht negativ; die Flotte
/// laedt und entlaedt im selben Intervall nicht gleichzeitig. Der Simulator fuehrt
/// diese Sollwerte gegen den tatsaechlichen Zustand aus.
/// </remarks>
public sealed class FlottenPlanIntervall
{
    /// <summary>Beginn des Intervalls; er muss zum Ausfuehrungsintervall passen.</summary>
    public DateTimeOffset Zeitstempel { get; set; }

    /// <summary>Geplante Ladeleistung [kW AC] je Speicher, in der Reihenfolge der Einheiten.</summary>
    public List<double> LadeleistungKwJeSpeicher { get; set; } = new();

    /// <summary>Geplante Entladeleistung [kW AC] je Speicher, in der Reihenfolge der Einheiten.</summary>
    public List<double> EntladeleistungKwJeSpeicher { get; set; } = new();

    /// <summary>Geplante PV-Abregelung [kW]; sie ist nur aus verbleibendem PV-Ueberschuss moeglich.</summary>
    public double PvAbregelungKw { get; set; }
}

/// <summary>
/// Die Schnittstelle zu einem planenden Verfahren. Sie haelt den Solver aus
/// <c>SpeicherEngine</c> heraus: die MILP-Fassung steht in <c>SpeicherPlanung</c>.
/// </summary>
public interface IFlottenPlaner
{
    /// <summary>Plant EINEN Horizont.</summary>
    /// <param name="anfrage">Zustand, Prognose, Einheiten und Randbedingungen des Horizonts.</param>
    /// <param name="cancellationToken">Abbruchmarke; sie wirkt bis in den Solverlauf.</param>
    /// <returns>Der Fahrplan mit unterscheidbarem <see cref="FlottenPlan.Status"/> — ein Fehler oder Zeitlimit darf nicht als optimierter Nullfahrplan erscheinen.</returns>
    FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken);
}

/// <summary>
/// Die Betriebseinstellungen eines Flottenlaufs: Ziel, Verteilung, Grenzen, Freigaben
/// und Planungstakt.
/// </summary>
public sealed class FlottenSimulationOptionen
{
    /// <summary>Die gefahrene Betriebsfuehrung, Standard <see cref="FlottenBetriebsziel.PvGreedy"/>.</summary>
    public FlottenBetriebsziel Betriebsziel { get; set; } = FlottenBetriebsziel.PvGreedy;

    /// <summary>Die Aufteilung der reaktiven Gesamtanforderung auf die Einheiten, Standard <see cref="FlottenVerteilung.KapazitaetsProportional"/>.</summary>
    public FlottenVerteilung Verteilung { get; set; } = FlottenVerteilung.KapazitaetsProportional;

    /// <summary>HARTE technische Netzbezugsgrenze [kW]; <c>null</c> = keine. Eine verbleibende Ueberschreitung macht die Variante unzulaessig, es wird keine Last abgeworfen.</summary>
    public double? NetzbezugGrenzeKw { get; set; }

    /// <summary>HARTE technische Einspeisegrenze [kW]; <c>null</c> = keine. Nicht abregelbare Erzeugung bleibt sichtbar statt fiktiv vernichtet zu werden.</summary>
    public double? NetzeinspeisungGrenzeKw { get; set; }

    /// <summary>Laden aus dem Netz ist freigegeben. Ohne Freigabe darf nur verbleibender PV-Ueberschuss geladen werden.</summary>
    public bool NetzladungErlaubt { get; set; }

    /// <summary>Einspeisen aus der Batterie ist freigegeben. Eine fehlende Freigabe verhindert keine zulaessige direkte PV-Einspeisung.</summary>
    public bool BatterieexportErlaubt { get; set; }

    /// <summary>WEICHES wirtschaftliches Peak-Ziel [kW]; <c>null</c> = keines. Es darf verfehlt werden — abgerechnet wird der reale Restpeak — und ist von einer harten Anschlussgrenze zu unterscheiden. Bei <see cref="PeakZielAdaptiv"/> ist es der STARTWERT H₀ der Ratsche.</summary>
    public double? WirtschaftlicherPeakZielwertKw { get; set; }

    /// <summary>
    /// Die Entladeschwelle wird im Lauf NACHGEZOGEN, statt das ganze Jahr fest zu stehen
    /// (kausale Ratsche, Spezifikation 5.1.1 Regel R).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Schwelle H ist dann kein Parameter, sondern ein Zustand, der nur steigen kann:
    /// Kann die Flotte die anstehende Netzlast nicht bis auf H druecken
    /// (<c>N − D &gt; H</c> mit D = verfuegbarer Entladeleistung aus
    /// <c>Grenzen()</c>), wird H auf das Erreichbare nachgezogen; danach gilt die Regel
    /// aus 5.1 unveraendert. <see cref="WirtschaftlicherPeakZielwertKw"/> ist in diesem
    /// Fall der Startwert H₀ (Vorgabe: die Grundlast), und
    /// <see cref="FlottenSimulationErgebnis.ErreichtesPeakZielKw"/> traegt das erreichte
    /// H am Ende.
    /// </para>
    /// <para>
    /// <b>Die SERIALISIERTE Vorgabe ist <c>false</c></b>: Ein gespeicherter Stand rechnet
    /// unveraendert weiter — insbesondere der Stand <c>@Projektflotte</c> des
    /// Pruefprojekts 1046, der die Regressionsbasis R7 haelt. Die Vorgabe fuer NEUE
    /// Staende steht in <c>FlottenVorgaben.PeakZielAdaptivFuer</c> (Muster #183).
    /// </para>
    /// <para>
    /// Die Ratsche wirkt nur dort, wo es ein Peak-Ziel gibt — bei den Betriebszielen
    /// <see cref="FlottenBetriebsziel.PeakShaving"/> und
    /// <see cref="FlottenBetriebsziel.MultiUse"/> — und nur im Lauf MIT Flotte; der
    /// Referenzlauf ohne Speicher haette keine Entladeleistung und zoege die Schwelle
    /// stur auf die Spitze.
    /// </para>
    /// </remarks>
    public bool PeakZielAdaptiv { get; set; }

    /// <summary>Preisschwelle [EUR/kWh], unterhalb derer die reaktive Arbitrage laedt; <c>null</c> = kein Schwellenregler.</summary>
    public double? ArbitrageLadepreisSchwelle { get; set; }

    /// <summary>Preisschwelle [EUR/kWh], oberhalb derer die reaktive Arbitrage entlaedt; <c>null</c> = kein Schwellenregler.</summary>
    public double? ArbitrageEntladepreisSchwelle { get; set; }

    /// <summary>Der zugelassene Informationsstand der Planung, Standard <see cref="PrognoseArt.VerifiziertBekannt"/>.</summary>
    public PrognoseArt PrognoseArt { get; set; } = PrognoseArt.VerifiziertBekannt;

    /// <summary>Die feste Reihenfolge der DIREKTEN Netzeinspeisung, Standard <see cref="FlottenErzeugerPrioritaet.PvVorBhkw"/>.</summary>
    public FlottenErzeugerPrioritaet ErzeugerPrioritaet { get; set; } = FlottenErzeugerPrioritaet.PvVorBhkw;

    /// <summary>Laenge des Planungshorizonts in Intervallen, Standard 192 (48 h). Das Ende der Istreihe kuerzt ihn zusaetzlich.</summary>
    public int PlanungshorizontIntervalle { get; set; } = 192;

    /// <summary>Abstand der Neuplanung in Intervallen, Standard 1. Neu geplant wird mit der REAL erreichten Energie.</summary>
    public int NeuplanungAlleIntervalle { get; set; } = 1;

    /// <summary>Die Vorgabe fuer den Energiestand am Ende des Rechenzeitraums, Standard <see cref="FlottenEndbedingung.KeineVorgabe"/>.</summary>
    public FlottenEndbedingung Endbedingung { get; set; } = FlottenEndbedingung.KeineVorgabe;

    /// <summary>Zielenergie [kWh] je Speicher fuer <see cref="FlottenEndbedingung.JeSpeicherZiel"/>, in der Reihenfolge der Einheiten.</summary>
    public List<double> EndenergieZielKWh { get; set; } = new();

    /// <summary>Bewertung der Endenergieaenderung [EUR/kWh]; <c>null</c> = keine. Ohne Endenergiegleichheit ist sie Pflicht, damit Anfangsenergie kein kostenloser Ertrag wird.</summary>
    public double? EnergieAusgleichEuroProKWh { get; set; }

    /// <summary>Erlaubt den protokollierten Rueckfall auf die reaktive Regel, wenn die Planung scheitert. Ohne diese ausdrueckliche Freigabe bricht der Lauf ab.</summary>
    public bool PrognoseFallbackErlaubt { get; set; }
}

/// <summary>
/// Alle Zeitreihen EINES Flottenlaufs: Istwerte, archivierte Prognosen, Projektjahre
/// und Verfuegbarkeiten.
/// </summary>
public sealed class FlottenEingang
{
    /// <summary>Kennung der gerechneten Konfiguration; sie verbindet Zeitreihen und Zusammenfassung.</summary>
    public string KonfigurationId { get; set; } = string.Empty;

    /// <summary>Kennung des verwendeten Datenstands; die Uebernahme eines Kandidaten prueft sie mit.</summary>
    public string DatenId { get; set; } = string.Empty;

    /// <summary>Die TATSAECHLICHEN Standortwerte, gegen die ausgefuehrt wird. Nur die Simulationsumgebung kennt sie, das Entscheidungssystem nicht.</summary>
    public List<FlottenNetzintervall> Istwerte { get; set; } = new();

    /// <summary>Die archivierten Prognosestaende; der Simulator waehlt je Entscheidungszeitpunkt den zuletzt bekannten passenden Snapshot.</summary>
    public List<FlottenPrognoseSnapshot> Prognosen { get; set; } = new();

    /// <summary>Die tatsaechlichen Projektjahre. Sind sie belegt, wird jedes Jahr neu simuliert und der End-SoC zum Start-SoC des Folgejahres.</summary>
    public List<FlottenProjektjahr> Projektjahre { get; set; } = new();

    /// <summary>Verfuegbarkeitsreihe (Faktor 0 bis 1) je <see cref="FlottenEinheit.Id"/>; ein fehlender Eintrag bedeutet volle Verfuegbarkeit.</summary>
    public Dictionary<string, List<double>> VerfuegbarkeitsfaktorNachEinheitId { get; set; } = new();
}

/// <summary>Ein tatsaechliches Projektjahr mit eigenen Zeitreihen (Spezifikation 9.4).</summary>
public sealed class FlottenProjektjahr
{
    /// <summary>Das Kalenderjahr; die Jahre werden aufsteigend gerechnet und muessen lueckenlos sein.</summary>
    public int Jahr { get; set; }

    /// <summary>Die Istwerte dieses Jahres im Viertelstundenraster.</summary>
    public List<FlottenNetzintervall> Istwerte { get; set; } = new();

    /// <summary>Die archivierten Prognosestaende dieses Jahres.</summary>
    public List<FlottenPrognoseSnapshot> Prognosen { get; set; } = new();

    /// <summary>Verfuegbarkeitsreihe (Faktor 0 bis 1) je <see cref="FlottenEinheit.Id"/> fuer dieses Jahr.</summary>
    public Dictionary<string, List<double>> VerfuegbarkeitsfaktorNachEinheitId { get; set; } = new();

    /// <summary>Das Jahr ist vollstaendig. Ein Teiljahr ist kein Jahrescashflow und wird nicht diskontiert.</summary>
    public bool IstVollstaendigesJahr { get; set; } = true;
}

/// <summary>Die vollstaendige Konfiguration EINER Variante: Flotte, Betrieb, Tarif, Wirtschaftlichkeit und Suchraum.</summary>
public sealed class FlottenStudieKonfiguration
{
    /// <summary>Die physisch gleichzeitig betriebenen Speicher dieser Variante. Vergleichsvarianten werden nicht addiert.</summary>
    public List<FlottenEinheit> Einheiten { get; set; } = new();

    /// <summary>Die Betriebseinstellungen des Laufs.</summary>
    public FlottenSimulationOptionen Optionen { get; set; } = new();

    /// <summary>Der Tarif, mit dem Referenz und Variante gleichermassen abgerechnet werden.</summary>
    public FlottenTarif Tarif { get; set; } = new();

    /// <summary>Die Kapitalwertrechnung; ohne Jahreskonten bleibt sie unbewertet.</summary>
    public FlottenWirtschaftlichkeitEingang Wirtschaftlichkeit { get; set; } = new();

    /// <summary>Der Suchraum der Auslegungsoptimierung; ohne Achsen wird nur diese eine Flotte gerechnet.</summary>
    public FlottenAuslegungEingang Auslegung { get; set; } = new();
}

/// <summary>
/// Das AUSGEFUEHRTE Ergebnis EINES Intervalls (Spezifikation 7.2).
/// </summary>
/// <remarks>
/// Alle Leistungen [kW AC], Energien [kWh]. Bei den Listen je Speicher gilt die
/// Vorzeichenkonvention <c>p = Entladen - Laden</c>: POSITIV ist Entladen, NEGATIV
/// ist Laden. <see cref="NetzleistungKw"/> ist als einzige Groesse ebenfalls
/// vorzeichenbehaftet (positiv Bezug, negativ Einspeisung); Bezug, Einspeisung und
/// die Verletzungsgroessen sind dagegen getrennte, nicht negative Reihen.
/// </remarks>
public sealed class FlottenIntervallErgebnis
{
    /// <summary>Beginn des Intervalls (UTC-Stempel der Istreihe).</summary>
    public DateTimeOffset Zeitstempel { get; set; }

    /// <summary>Bruttolast L [kW] des Intervalls, unveraendert aus der Istreihe.</summary>
    public double LastKw { get; set; }

    /// <summary>Verfuegbare PV-AC-Leistung [kW] VOR der Abregelung.</summary>
    public double PvVerfuegbarKw { get; set; }

    /// <summary>BHKW-Leistung [kW] des Intervalls.</summary>
    public double BhkwKw { get; set; }

    /// <summary>Summierter Hilfsverbrauch [kW] der Flotte; er zaehlt als zusaetzliche Standortlast.</summary>
    public double HilfsverbrauchKw { get; set; }

    /// <summary>Netzleistung G [kW], VORZEICHENBEHAFTET: positiv Bezug, negativ Einspeisung.</summary>
    public double NetzleistungKw { get; set; }

    /// <summary>Netzbezug [kW], nicht negativ: <c>max(G, 0)</c>. Nur diese Groesse geht in den Reststrombedarf des Projekts ein.</summary>
    public double NetzbezugKw { get; set; }

    /// <summary>Netzeinspeisung [kW], nicht negativ: <c>max(-G, 0)</c>. Bezug und Einspeisung schliessen einander aus.</summary>
    public double NetzeinspeisungKw { get; set; }

    /// <summary>Anteil der PV an der Einspeisung [kW] nach der festen Erzeugerprioritaet.</summary>
    public double PvNetzeinspeisungKw { get; set; }

    /// <summary>Anteil des BHKW an der Einspeisung [kW] nach der festen Erzeugerprioritaet.</summary>
    public double BhkwNetzeinspeisungKw { get; set; }

    /// <summary>Anteil der Batterie an der Einspeisung [kW]. Die drei Anteile ergeben zusammen <see cref="NetzeinspeisungKw"/> — ohne Doppelzaehlung.</summary>
    public double BatterieNetzeinspeisungKw { get; set; }

    /// <summary>Abgeregelte PV-Leistung K [kW]; sie ist nur aus verbleibendem PV-Ueberschuss moeglich und wird getrennt von der Einspeisung gefuehrt.</summary>
    public double PvAbregelungKw { get; set; }

    /// <summary>Umwandlungsverlust der ganzen Flotte [kWh] im Intervall: <c>dt * ((1-eta_c)*c + (1/eta_d-1)*d)</c>.</summary>
    public double UmwandlungsverlustKWh { get; set; }

    /// <summary>Derselbe Verlust [kWh] je Speicher, in der Reihenfolge der Einheiten.</summary>
    public List<double> UmwandlungsverlustKWhJeSpeicher { get; set; } = new();

    /// <summary>Summe der Betraege von Soll minus Ist [kW] ueber alle Speicher — das Mass dafuer, wie weit die physikalischen Grenzen den Sollwert beschnitten haben.</summary>
    public double SollabweichungKw { get; set; }

    /// <summary>Verbleibende Ueberschreitung der HARTEN Bezugsgrenze [kW]; groesser 0 macht die Variante unzulaessig.</summary>
    public double TechnischeImportverletzungKw { get; set; }

    /// <summary>Verbleibende Ueberschreitung der HARTEN Einspeisegrenze [kW]; groesser 0 macht die Variante unzulaessig.</summary>
    public double TechnischeExportverletzungKw { get; set; }

    /// <summary>Ueberschreitung des WEICHEN Peak-Ziels [kW]. Sie ist eine Warnung, keine Unzulaessigkeit.</summary>
    public double WirtschaftlichePeakverletzungKw { get; set; }

    /// <summary>
    /// Das in DIESEM Intervall geltende weiche Peak-Ziel H [kW]; <c>null</c>, wenn der
    /// Lauf keines fuehrt.
    /// </summary>
    /// <remarks>
    /// Ohne <see cref="FlottenSimulationOptionen.PeakZielAdaptiv"/> ist es in jedem
    /// Intervall der feste Zielwert; mit der Ratsche (Spezifikation 5.1.1) ist die Reihe
    /// eine TREPPE, die nur steigt. Das Netzbild zeichnet sie an der Stelle, an der es
    /// vorher eine waagerechte Linie zog.
    /// </remarks>
    public double? PeakZielKw { get; set; }

    /// <summary>Angeforderte Leistung [kW] je Speicher aus Regel oder Fahrplan; positiv Entladen, negativ Laden.</summary>
    public List<double> SollleistungKwJeSpeicher { get; set; } = new();

    /// <summary>TATSAECHLICH ausgefuehrte Leistung [kW] je Speicher nach allen Begrenzungen; positiv Entladen, negativ Laden. Nur sie schreibt den Zustand fort.</summary>
    public List<double> IstleistungKwJeSpeicher { get; set; } = new();

    /// <summary>Energie [kWh] je Speicher VOR dem Intervall.</summary>
    public List<double> EnergieStartKWhJeSpeicher { get; set; } = new();

    /// <summary>Energie [kWh] je Speicher NACH dem Intervall, fortgeschrieben allein aus der Istleistung.</summary>
    public List<double> EnergieEndeKWhJeSpeicher { get; set; } = new();

    /// <summary>Kennung des ausgefuehrten Fahrplans; <c>null</c> bei reaktivem Betrieb oder nach einem Rueckfall.</summary>
    public string? PlanId { get; set; }

    /// <summary>Kennung des zugrunde liegenden Prognosestands; <c>null</c> bei reaktivem Betrieb oder nach einem Rueckfall.</summary>
    public string? PrognoseId { get; set; }

    /// <summary>Das Intervall wurde nach einem Planungsfehler mit der reaktiven Rueckfallregel gefahren.</summary>
    public bool PlanFallback { get; set; }

    /// <summary>Der protokollierte Grund des Rueckfalls; <c>null</c>, wenn keiner vorlag.</summary>
    public string? PlanFallbackGrund { get; set; }
}

/// <summary>Die Nutzungs- und Beanspruchungskennzahlen EINER Einheit ueber den Rechenzeitraum (Spezifikation 8.1).</summary>
public sealed class FlottenSpeicherKennzahlen
{
    /// <summary>Die Kennung der Einheit (<see cref="FlottenEinheit.Id"/>).</summary>
    public string SpeicherId { get; set; } = string.Empty;

    /// <summary>Energie [kWh] zu Beginn des Rechenzeitraums.</summary>
    public double AnfangsenergieKWh { get; set; }

    /// <summary>Energie [kWh] am Ende des Rechenzeitraums; bei mehreren Projektjahren wird sie zum Startwert des Folgejahres.</summary>
    public double EndenergieKWh { get; set; }

    /// <summary>Aufgenommene Energie [kWh] AC-seitig, vor den Ladeverlusten.</summary>
    public double LadeenergieAcKWh { get; set; }

    /// <summary>Abgegebene Energie [kWh] AC-seitig, nach den Entladeverlusten; auf sie beziehen sich Durchsatzkosten und Grenzverschleiss.</summary>
    public double EntladeenergieAcKWh { get; set; }

    /// <summary>Aequivalente Vollzyklen [-]: <c>(eta_c*Ladeenergie + Entladeenergie/eta_d) / (2*Kapazitaet)</c>. Die Kennzahl beschreibt den Energieumsatz, nicht die Lebensdauer.</summary>
    public double AequivalenteVollzyklen { get; set; }

    /// <summary>Miner-Schaden [-] aus der Rainflow-Auswertung; 1 bedeutet das vereinbarte End-of-Life-Kriterium der gelieferten Kurve. Ohne Kurve bleibt er 0.</summary>
    public double RainflowSchaden { get; set; }

    /// <summary>Die gezaehlten Rainflow-Zyklen des zusammenhaengenden SoC-Verlaufs.</summary>
    public List<FlottenRainflowZyklus> RainflowZyklen { get; set; } = new();
}

/// <summary>Ein gezaehlter Rainflow-Zyklus (Spezifikation 8.2).</summary>
public sealed class FlottenRainflowZyklus
{
    /// <summary>Zyklustiefe DoD [-] als Schwingweite des SoC.</summary>
    public double Entladetiefe { get; set; }

    /// <summary>Mittlerer SoC [-] des Zyklus; er wird ausgewiesen, nicht bewertet.</summary>
    public double MittlererSoc { get; set; }

    /// <summary>Anzahl [-]: 1,0 fuer einen Vollzyklus, 0,5 fuer einen Halbzyklus des Reststapels.</summary>
    public double Anzahl { get; set; }
}

/// <summary>Das Ergebnis einer Rainflow-Auswertung.</summary>
public sealed class FlottenRainflowErgebnis
{
    /// <summary>Die gezaehlten Zyklen in der Reihenfolge ihrer Schliessung.</summary>
    public List<FlottenRainflowZyklus> Zyklen { get; set; } = new();

    /// <summary>Der Miner-Schaden [-] innerhalb der gelieferten Lebensdauerkurve; ohne Kurve bleibt er 0.</summary>
    public double Schaden { get; set; }
}

/// <summary>Der vollstaendige Zeitreihen- und Kennzahlenstand EINES Laufs — der Referenz ohne Speicher oder der Variante mit Flotte.</summary>
public sealed class FlottenSimulationErgebnis
{
    /// <summary>Kennung der gerechneten Konfiguration.</summary>
    public string KonfigurationId { get; set; } = string.Empty;

    /// <summary>Kennung des verwendeten Datenstands.</summary>
    public string DatenId { get; set; } = string.Empty;

    /// <summary>Der Lauf haelt alle HARTEN Grenzen und die Endbedingung ein.</summary>
    public bool Zulaessig { get; set; }

    /// <summary>Der erste festgestellte Grund der Unzulaessigkeit; <c>null</c>, wenn der Lauf zulaessig ist.</summary>
    public string? Unzulaessigkeitsgrund { get; set; }

    /// <summary>Die ausgefuehrten Intervalle in zeitlicher Reihenfolge.</summary>
    public List<FlottenIntervallErgebnis> Intervalle { get; set; } = new();

    /// <summary>Die Kennzahlen je Einheit, in der Reihenfolge der Einheiten; beim Referenzlauf ohne Speicher ist die Liste leer.</summary>
    public List<FlottenSpeicherKennzahlen> SpeicherKennzahlen { get; set; } = new();

    /// <summary>Netzbezug [kWh] ueber den Rechenzeitraum.</summary>
    public double NetzbezugKWh { get; set; }

    /// <summary>Netzeinspeisung [kWh] ueber den Rechenzeitraum, getrennt vom Bezug gefuehrt.</summary>
    public double NetzeinspeisungKWh { get; set; }

    /// <summary>Abgeregelte PV-Energie [kWh] ueber den Rechenzeitraum.</summary>
    public double PvAbregelungKWh { get; set; }

    /// <summary>Umwandlungsverluste [kWh] ueber den Rechenzeitraum; sie stecken bereits in der Netzrechnung und werden nicht nochmals bewertet.</summary>
    public double VerlusteKWh { get; set; }

    /// <summary>Hoechster Netzbezug [kW] eines Abrechnungsintervalls — die Groesse, die der Leistungspreis bewertet.</summary>
    public double MaximalerNetzbezugKw { get; set; }

    /// <summary>
    /// Die KAUSAL ERREICHTE Schwelle H_end [kW] am Ende des Laufs; <c>null</c>, wenn die
    /// Ratsche nicht gefahren wurde (Spezifikation 5.1.1).
    /// </summary>
    /// <remarks>
    /// Sie ist der Gegenwert zum Vorausschau-Optimum M* der Bisektion
    /// („Peak-Ziel bestimmen"): Es gilt <c>M* ≤ H_end</c>, und die Differenz
    /// <c>H_end − M*</c> ist der Wert einer Prognose. Ein FESTES Ziel traegt hier
    /// <c>null</c> — es ist keine erreichte, sondern eine gesetzte Schwelle.
    /// </remarks>
    public double? ErreichtesPeakZielKw { get; set; }

    /// <summary>Zahl der Intervalle, die nach einem Planungsfehler reaktiv gefahren wurden.</summary>
    public int PlanFallbackIntervalle { get; set; }

    /// <summary>
    /// Die Betriebsdiagnose dieses Laufs: sie sagt, WARUM die Flotte getan hat, was sie
    /// getan hat — und insbesondere, warum sie nichts getan hat (Konzept Stromspeicher-Dialoge 2.4).
    /// Der Referenzlauf OHNE Speicher traegt eine leere Diagnose; sie ist nie <c>null</c>.
    /// </summary>
    public FlottenDiagnose Diagnose { get; set; } = new();
}

/// <summary>
/// Der sprachneutrale Grund EINES Diagnosebefunds. Die Aufzaehlung wird gespeichert und
/// angezeigt; der Anzeigetext gehoert in die Oberflaeche, nicht in die Rechenbibliothek.
/// </summary>
public enum FlottenDiagnoseGrund
{
    /// <summary>Die Nettolast <c>n</c> lag ueber dem wirtschaftlichen Peak-Ziel <c>H</c>. Liegt der Anteil bei 1, fiel die Last im ganzen Zeitraum nie unter <c>H</c>.</summary>
    LastUeberPeakZiel = 0,

    /// <summary>Der Ladedeckel <c>max(0, H - n)</c> der Peak-Regel war 0: Wiederaufladung haette einen neuen Peak ueber <c>H</c> erzeugt (Spezifikation 5.1).</summary>
    LadedeckelDurchPeakregel = 1,

    /// <summary>Der Ladedeckel <c>max(0, -n)</c> des Netzladeverbots war 0: ohne <see cref="FlottenSimulationOptionen.NetzladungErlaubt"/> ist Laden nur aus verbleibendem Ueberschuss moeglich.</summary>
    LadedeckelDurchNetzladeverbot = 2,

    /// <summary>Es lag eine Entladeanforderung vor, die Flotte hatte aber keine abgebbare Energie — der Speicher stand auf seiner unteren Marke.</summary>
    EntladeanforderungOhneEnergie = 3,

    /// <summary>Im gesamten Rechenzeitraum wurde nie eine Entladung angefordert.</summary>
    KeineEntladeanforderung = 4,

    /// <summary>Im gesamten Rechenzeitraum wurde nie eine Ladung angefordert.</summary>
    KeineLadeanforderung = 5
}

/// <summary>Ein gezaehlter Diagnosebefund: der Grund und die Zahl der betroffenen Intervalle.</summary>
public sealed class FlottenDiagnoseBefund
{
    /// <summary>Der sprachneutrale Grund.</summary>
    public FlottenDiagnoseGrund Grund { get; set; }

    /// <summary>Zahl der Intervalle, auf die der Grund zutrifft.</summary>
    public int Intervalle { get; set; }

    /// <summary>Anteil [-] von <see cref="Intervalle"/> an allen gerechneten Intervallen; 0, wenn nichts gerechnet wurde.</summary>
    public double Anteil { get; set; }
}

/// <summary>Die Betriebsdiagnose EINER Einheit ueber den Rechenzeitraum.</summary>
public sealed class FlottenEinheitDiagnose
{
    /// <summary>Die Kennung der Einheit (<see cref="FlottenEinheit.Id"/>).</summary>
    public string SpeicherId { get; set; } = string.Empty;

    /// <summary>Zahl der Intervalle, in denen die Verteilung dieser Einheit eine Entladung zugewiesen hat.</summary>
    public int IntervalleMitEntladeanforderung { get; set; }

    /// <summary>Zahl der Intervalle, in denen die Verteilung dieser Einheit eine Ladung zugewiesen hat.</summary>
    public int IntervalleMitLadeanforderung { get; set; }

    /// <summary>Zahl der Intervalle, in denen die Flotte entladen sollte und diese Einheit keine abgebbare Energie hatte.</summary>
    public int IntervalleEntladeanforderungOhneEnergie { get; set; }

    /// <summary>Aufgenommene Energie [kWh] AC-seitig; sie entspricht <see cref="FlottenSpeicherKennzahlen.LadeenergieAcKWh"/>.</summary>
    public double LadeenergieAcKWh { get; set; }

    /// <summary>Abgegebene Energie [kWh] AC-seitig; sie entspricht <see cref="FlottenSpeicherKennzahlen.EntladeenergieAcKWh"/>.</summary>
    public double EntladeenergieAcKWh { get; set; }

    /// <summary>Die Einheit hat im gesamten Zeitraum weder geladen noch entladen.</summary>
    public bool Arbeitslos { get; set; }
}

/// <summary>
/// Die Diagnose EINES Flottenlaufs (Konzept Stromspeicher-Dialoge 2.4 Punkt 5, Aufgabe #183).
/// </summary>
/// <remarks>
/// <para>
/// Sie zaehlt die Sperren, die im Bestand unsichtbar blieben: eine Nettolast dauerhaft
/// ueber dem Peak-Ziel, einen Ladedeckel 0 aus der Peak-Regel oder aus dem
/// Netzladeverbot und eine Entladeanforderung an einen leeren Speicher. Aus Lade- und
/// Entladeenergie folgt die Aussage <see cref="Arbeitslos"/>.
/// </para>
/// <para>
/// Die Diagnose aendert KEINEN Rechenwert. Sie wird nur fuer den Lauf MIT Flotte
/// gefuehrt; der Referenzlauf ohne Speicher traegt eine leere Diagnose.
/// </para>
/// </remarks>
public sealed class FlottenDiagnose
{
    /// <summary>Zahl der gerechneten Intervalle; 0 beim Referenzlauf ohne Speicher.</summary>
    public int IntervalleGesamt { get; set; }

    /// <summary>Zahl der Intervalle mit <c>n &gt; H</c>; 0, wenn kein Peak-Ziel gesetzt ist.</summary>
    public int IntervalleLastUeberPeakZiel { get; set; }

    /// <summary>Zahl der Intervalle, in denen die Betriebsfuehrung eine Entladung der Flotte angefordert hat.</summary>
    public int IntervalleMitEntladeanforderung { get; set; }

    /// <summary>Zahl der Intervalle, in denen die Betriebsfuehrung eine Ladung der Flotte angefordert hat.</summary>
    public int IntervalleMitLadeanforderung { get; set; }

    /// <summary>Zahl der Intervalle, in denen die Peak-Regel den Ladedeckel auf 0 gesetzt hat.</summary>
    public int IntervalleLadedeckelNullPeakregel { get; set; }

    /// <summary>Zahl der Intervalle, in denen das Netzladeverbot den Ladedeckel auf 0 gesetzt hat.</summary>
    public int IntervalleLadedeckelNullNetzladeverbot { get; set; }

    /// <summary>Zahl der Intervalle mit Entladeanforderung, in denen die GESAMTE Flotte keine abgebbare Energie hatte.</summary>
    public int IntervalleEntladeanforderungOhneEnergie { get; set; }

    /// <summary>
    /// Zahl der Intervalle, in denen die Ratsche die Schwelle NACHGEZOGEN hat
    /// (<c>N − D &gt; H</c>, Spezifikation 5.1.1); 0 bei festem Peak-Ziel.
    /// </summary>
    public int IntervalleSchwelleNachgezogen { get; set; }

    /// <summary>Aufgenommene Energie [kWh] AC-seitig ueber die ganze Flotte.</summary>
    public double LadeenergieAcKWh { get; set; }

    /// <summary>Abgegebene Energie [kWh] AC-seitig ueber die ganze Flotte.</summary>
    public double EntladeenergieAcKWh { get; set; }

    /// <summary>Die Flotte hat im gesamten Zeitraum weder geladen noch entladen. Ohne Einheiten ist die Aussage <c>false</c>.</summary>
    public bool Arbeitslos { get; set; }

    /// <summary>Die gezaehlten Gruende in fester Reihenfolge; ein Grund ohne betroffene Intervalle steht nicht in der Liste.</summary>
    public List<FlottenDiagnoseBefund> Gruende { get; set; } = new();

    /// <summary>Die Diagnose je Einheit, in der Reihenfolge der Einheiten.</summary>
    public List<FlottenEinheitDiagnose> Einheiten { get; set; } = new();
}

/// <summary>
/// Die vollstaendige Gegenueberstellung EINER Variante: derselbe Standortdatensatz und
/// derselbe Tarif, einmal ohne Speicher und einmal mit Flotte (Spezifikation 9.1).
/// </summary>
public sealed class FlottenStudienErgebnis
{
    /// <summary>Der Referenzlauf ohne jeden Speicher — gleiche Last, gleiche Erzeugung, gleiche Netzgrenzen.</summary>
    public FlottenSimulationErgebnis ReferenzOhneSpeicher { get; set; } = new();

    /// <summary>Der Lauf mit der untersuchten Flotte.</summary>
    public FlottenSimulationErgebnis Variante { get; set; } = new();

    /// <summary>Die Stromrechnung der Referenz.</summary>
    public FlottenRechnung Referenzrechnung { get; set; } = new();

    /// <summary>Die Stromrechnung der Variante; die Differenz beider Rechnungen darf negativ sein und wird nicht auf 0 gekappt.</summary>
    public FlottenRechnung Variantenrechnung { get; set; } = new();

    /// <summary>Endenergie minus Anfangsenergie [kWh] je Speicher, in der Reihenfolge der Einheiten.</summary>
    public List<double> EndenergieAenderungKWhJeSpeicher { get; set; } = new();

    /// <summary>Die bewertete Bestandsaenderung [EUR]: Summe der Endenergieaenderung mal <see cref="FlottenSimulationOptionen.EnergieAusgleichEuroProKWh"/>.</summary>
    public double EndenergieAusgleichEuro { get; set; }

    /// <summary>Die Kapitalwertrechnung, sofern Jahreskonten vorlagen; sonst <c>null</c>.</summary>
    public FlottenWirtschaftlichkeitErgebnis? Wirtschaftlichkeit { get; set; }
}

/// <summary>Die tarifliche Seite der Abrechnungsperiode, die fuer Referenz und Variante gleichermassen gilt.</summary>
public sealed class FlottenTarif
{
    /// <summary>Leistungspreis [EUR/kW] auf den hoechsten Netzbezug der Periode. Er wird einmal je Periode angesetzt.</summary>
    public double LeistungspreisEuroProKw { get; set; }

    /// <summary>Fixkosten [EUR] der Periode; sie stehen in beiden Rechnungen und heben sich in der Differenz auf.</summary>
    public double FixkostenEuro { get; set; }

    /// <summary>Eigener Verkaufspreis der Batterieeinspeisung [EUR/kWh]; <c>null</c> = der Wert der Zeitreihe gilt. Ein Batterieexport verlangt einen endlichen eigenen Preis.</summary>
    public double? BatterieVerkaufspreisEuroProKWh { get; set; }
}

/// <summary>Die Stromrechnung EINER Abrechnungsperiode (Spezifikation 9.1).</summary>
public sealed class FlottenRechnung
{
    /// <summary>Arbeitskosten [EUR]: Bezugskosten abzueglich der getrennt bewerteten PV-, BHKW- und Batterieerloese.</summary>
    public double EnergiekostenEuro { get; set; }

    /// <summary>Leistungskosten [EUR]: <see cref="PeakKw"/> mal Leistungspreis.</summary>
    public double LeistungskostenEuro { get; set; }

    /// <summary>Fixkosten [EUR] der Periode.</summary>
    public double FixkostenEuro { get; set; }

    /// <summary>Der abgerechnete Hoechstbezug [kW] der Periode.</summary>
    public double PeakKw { get; set; }

    /// <summary>Die Summe der drei Bestandteile [EUR].</summary>
    public double GesamtEuro { get; set; }
}

/// <summary>Ein vollstaendiges Jahreskonto der Kapitalwertrechnung (Spezifikation 9.3).</summary>
public sealed class FlottenJahreskonto
{
    /// <summary>Die Jahresnummer ab 1; die Konten muessen lueckenlos bei 1 beginnen.</summary>
    public int Jahr { get; set; }

    /// <summary>Die Stromrechnung der Referenz ohne Speicher in diesem Jahr.</summary>
    public FlottenRechnung Referenzrechnung { get; set; } = new();

    /// <summary>Die Stromrechnung der Variante in diesem Jahr.</summary>
    public FlottenRechnung Variantenrechnung { get; set; } = new();

    /// <summary>Betriebsaufwand [EUR/a] der Flotte: fest, kapazitaets- und leistungsbezogen.</summary>
    public double OpexEuro { get; set; }

    /// <summary>Durchsatzkosten [EUR/a] auf die tatsaechliche AC-Entladung.</summary>
    public double DurchsatzkostenEuro { get; set; }

    /// <summary>Ersatzinvestitionen [EUR], gebucht im faelligen Jahr.</summary>
    public double ErsatzkostenEuro { get; set; }

    /// <summary>Die bewertete Endenergieaenderung [EUR] dieses Jahres.</summary>
    public double EndenergieAusgleichEuro { get; set; }

    /// <summary>Netto-Cashflow [EUR/a]: Referenzrechnung minus Variantenrechnung minus Betrieb, Durchsatz und Ersatz, zuzueglich Energieausgleich.</summary>
    public double NettoCashflowEuro { get; set; }

    /// <summary>Das Konto deckt ein vollstaendiges Jahr ab. Teiljahre werden nicht diskontiert.</summary>
    public bool IstVollstaendigesJahr { get; set; } = true;

    /// <summary>Klartext zur Herkunft des Kontos — tatsaechlich simuliertes Projektjahr oder ausdruecklich wiederholte Referenzjahr-Projektion.</summary>
    public string Projektionskennzeichnung { get; set; } = string.Empty;
}

/// <summary>Die Eingaben der Kapitalwertrechnung.</summary>
public sealed class FlottenWirtschaftlichkeitEingang
{
    /// <summary>Die Einheiten, aus denen Investition, Betrieb, Ersatz und Restwert stammen.</summary>
    public List<FlottenEinheit> Einheiten { get; set; } = new();

    /// <summary>Die vollstaendigen Jahreskonten; ohne sie findet keine Bewertung statt.</summary>
    public List<FlottenJahreskonto> Jahreskonten { get; set; } = new();

    /// <summary>Kalkulationszins r [-] als Bruch (0,03 = 3 %); er muss groesser als -1 sein. Nominale Reihen verlangen einen nominalen Zins.</summary>
    public double Kalkulationszins { get; set; }

    /// <summary>Zusaetzlicher Restwert [EUR] am Ende der Laufzeit, neben den Restwerten der Einheiten.</summary>
    public double RestwertEuro { get; set; }

    /// <summary>Ein einzelnes Referenzjahr wird AUSDRUECKLICH ueber die Projektlaufzeit wiederholt. Das ist eine vereinfachte Projektion und erzeugt keine neuen realen Jahresdaten.</summary>
    public bool ReferenzjahrExplizitWiederholen { get; set; }

    /// <summary>Projektlaufzeit [a] fuer die Wiederholung; nur bei <see cref="ReferenzjahrExplizitWiederholen"/> gefordert und dann positiv.</summary>
    public int ProjektjahreBeiWiederholung { get; set; }
}

/// <summary>Das Ergebnis der Kapitalwertrechnung.</summary>
public sealed class FlottenWirtschaftlichkeitErgebnis
{
    /// <summary>CAPEX [EUR] der Flotte: je Einheit Festbetrag, EUR/kWh und EUR/kW auf die groessere der beiden Richtungsleistungen.</summary>
    public double InvestitionEuro { get; set; }

    /// <summary>Kapitalwert [EUR] gegenueber der Variante ohne Zusatzspeicher: <c>-CAPEX + Summe CF/(1+r)^a + Restwert/(1+r)^n</c>.</summary>
    public double KapitalwertEuro { get; set; }

    /// <summary>
    /// ETAPPE E10: der Restwert [EUR] am Ende der Laufzeit, NOMINAL — der zusaetzliche
    /// Restwert der Studie plus der lineare Restwert je Einheit
    /// (<see cref="FlottenWirtschaftlichkeit.LinearerRestwert"/>); im Kapitalwert steht er
    /// abgezinst.
    /// </summary>
    public double RestwertEuro { get; set; }

    /// <summary>Erstes Jahr, in dem der kumulierte DISKONTIERTE Cashflow nicht mehr negativ ist; <c>null</c>, wenn das nie eintritt.</summary>
    public int? DiskontierteAmortisationJahr { get; set; }

    /// <summary>Die undiskontierten Netto-Cashflows [EUR] ab Jahr 1.</summary>
    public List<double> JahresCashflowsEuro { get; set; } = new();

    /// <summary>Die bewerteten Jahreskonten in Jahresreihenfolge.</summary>
    public List<FlottenJahreskonto> Jahreskonten { get; set; } = new();

    /// <summary>Das Ergebnis beruht auf einer ausdruecklich wiederholten Referenzjahr-Projektion und ist entsprechend zu kennzeichnen.</summary>
    public bool IstWiederholteReferenzjahrProjektion { get; set; }
}

/// <summary>
/// EINE Suchachse der Auslegung: sie ersetzt ueber <see cref="ErsetztEinheitId"/> genau
/// ihre Einheit, alle uebrigen Einheiten bleiben fest (Spezifikation 12.2).
/// </summary>
/// <remarks>
/// <para><b>Unter <see cref="FlottenSuchmethode.Groesse"/> sind die Kandidaten GERAETE</b>
/// (Anwenderentscheid vom 15.09.2026): Der Bereich fuer Kapazitaet und Leistung waehlt
/// unter den Saetzen der <see cref="Quelle"/>; gerechnet wird, was es wirklich gibt.
/// Nichts wird mehr skaliert, und die Kandidatenzahl ist die Zahl der gefundenen Geraete
/// — sie kann nicht mehr als Produkt zweier Achsen explodieren.</para>
/// <para>Unter <see cref="FlottenSuchmethode.Stueckzahl"/> ist es unveraendert die
/// <see cref="Vorlage"/>, nur in anderer Stueckzahl.</para>
/// </remarks>
public sealed class FlottenAuslegungsAchse
{
    /// <summary>Die Achse nimmt an der Suche teil, Standard <c>true</c>.</summary>
    public bool Aktiv { get; set; } = true;

    /// <summary>Kennung der Einheit, die diese Achse ersetzt; <c>null</c> = die Achse fuegt eine neue Einheit hinzu.</summary>
    public string? ErsetztEinheitId { get; set; }

    /// <summary>
    /// Welche zwei Groessen die Achse aufspannt. Gueltig ist allein
    /// <see cref="FlottenAuslegungsmodus.KapazitaetUndLeistung"/>; ein aelterer Stand mit
    /// einer C-Rate-Kopplung wird von <see cref="FlottenAltstand.Normalisiere(FlottenAuslegungsAchse)"/>
    /// benannt umgesetzt.
    /// </summary>
    public FlottenAuslegungsmodus Modus { get; set; }

    /// <summary>
    /// WOHER die Geraete kommen, unter denen die Groessensuche waehlt; Vorgabe
    /// <see cref="FlottenKandidatenquelle.Projektkatalog"/>.
    /// </summary>
    /// <remarks>
    /// Sie ist AUSGESCHRIEBEN und nicht bloss der Aufzaehlungsstandard: Ein Stand, der
    /// das Feld nicht fuehrt, soll im eigenen Projekt suchen und nicht ungefragt den
    /// ganzen Stammdatenbestand aufziehen.
    /// </remarks>
    public FlottenKandidatenquelle Quelle { get; set; } = FlottenKandidatenquelle.Projektkatalog;

    /// <summary>
    /// Die Geraete der <see cref="Quelle"/> — der VOLLE Bestand, aus dem
    /// <see cref="FlottenGeraetewahl.Waehle"/> nach Kapazitaets- und Leistungsbereich
    /// auswaehlt.
    /// </summary>
    /// <remarks>
    /// <para>Gefuellt wird die Liste vom Kern (er kennt die Datenbank, die Engine nicht);
    /// die AUSWAHL trifft allein die Engine. So zaehlen Kandidatenzeile,
    /// Kandidatentabelle und Lauf dieselben Geraete, und zwei Laeufe auf derselben
    /// Datenbank liefern dieselbe Reihenfolge.</para>
    /// <para>Sie gehoert zum Suchraum und wird mitgespeichert; vor jedem Lauf und bei
    /// jedem Quellenwechsel schreibt der Kern sie frisch.</para>
    /// </remarks>
    public List<FlottenGeraetekandidat> Geraete { get; set; } = new();

    /// <summary>Kleinste Stueckzahl der Achse, Standard 1; 0 ist zulaessig und bedeutet „diese Einheit entfaellt".</summary>
    public int AnzahlVon { get; set; } = 1;

    /// <summary>Groesste Stueckzahl der Achse, Standard 1.</summary>
    public int AnzahlBis { get; set; } = 1;

    /// <summary>Kleinste Kapazitaet [kWh] der Achse.</summary>
    public double KapazitaetVonKWh { get; set; }

    /// <summary>Groesste Kapazitaet [kWh] der Achse.</summary>
    public double KapazitaetBisKWh { get; set; }

    /// <summary>NUR ZUM LESEN aelterer Staende: die Schrittweite der Kapazitaet [kWh] des frueheren Rasters. Die Geraetesuche rastert nicht.</summary>
    public double KapazitaetSchrittKWh { get; set; }

    /// <summary>Kleinste Leistung [kW] der Achse.</summary>
    public double LeistungVonKw { get; set; }

    /// <summary>Groesste Leistung [kW] der Achse.</summary>
    public double LeistungBisKw { get; set; }

    /// <summary>NUR ZUM LESEN aelterer Staende: die Schrittweite der Leistung [kW] des frueheren Rasters. Die Geraetesuche rastert nicht.</summary>
    public double LeistungSchrittKw { get; set; }

    /// <summary>NUR ZUM LESEN aelterer Staende: kleinste C-Rate [1/h]; <see cref="FlottenAltstand"/> rechnet sie in eine Leistungs- oder Kapazitaetsgrenze um.</summary>
    public double CRateVon { get; set; }

    /// <summary>NUR ZUM LESEN aelterer Staende: groesste C-Rate [1/h]; <see cref="FlottenAltstand"/> rechnet sie in eine Leistungs- oder Kapazitaetsgrenze um.</summary>
    public double CRateBis { get; set; }

    /// <summary>NUR ZUM LESEN aelterer Staende: die Schrittweite der C-Rate [1/h] des frueheren Rasters.</summary>
    public double CRateSchritt { get; set; }

    /// <summary>
    /// Die Einheit, die unter <see cref="FlottenSuchmethode.Stueckzahl"/> vervielfacht
    /// wird: Wirkungsgrade, SoC-Band, Reserve, Hilfsverbrauch und Kostensaetze. Unter
    /// <see cref="FlottenSuchmethode.Groesse"/> bringt jeder Kandidat seine eigenen
    /// Kennwerte mit — dort wird sie nicht gelesen.
    /// </summary>
    public FlottenEinheit Vorlage { get; set; } = new();
}

/// <summary>
/// WAS ein Lauf der Rastersuche variiert (Auftrag #247, Anwenderentscheid SD-E-10 vom
/// 12.09.2026, Konzept „Stromspeicher-Dialoge" 8.3).
/// </summary>
/// <remarks>
/// <para><b>Je Lauf EINE Variationsart</b> (SD-Q16): Groesse und Stueckzahl werden nie im
/// selben Lauf variiert. Unter <see cref="Groesse"/> steht die Stueckzahl jeder
/// eingeschalteten Einheit fest, unter <see cref="Stueckzahl"/> ihre Groesse. Wer beides
/// pruefen will, faehrt zwei Laeufe — so bleibt die Kandidatenzahl beherrschbar (266
/// Groessen ODER 4 Stueckzahlen je Einheit, nicht 1 064), und Karte und Kurve bleiben
/// zweidimensional.</para>
/// <para><b>Das Mischraster Stueckzahl × Groesse gibt es seit #247 nicht mehr.</b> Bis
/// dahin lief <c>BildeAchse</c> Stueckzahl × erste Groesse × zweite Groesse in EINEM
/// Lauf; ein Katalog- oder Projektspeicher bekam dabei Fantasiegroessen mit den
/// Kostensaetzen seines Geraets (Konzept 8.2).</para>
/// </remarks>
public enum FlottenSuchmethode
{
    /// <summary>Nur die eingestellte Flotte bewerten — genau ein Kandidat, nichts wird variiert.</summary>
    Bewerten = 0,

    /// <summary>
    /// Die GROESSE suchen: das GERAET finden, das in die vorgegebenen Bereiche fuer
    /// Kapazitaet und Leistung passt. Kandidaten sind die Saetze der
    /// <see cref="FlottenAuslegungsAchse.Quelle"/>, ausgewaehlt von
    /// <see cref="FlottenGeraetewahl.Waehle"/>; die Stueckzahl steht fest auf
    /// <see cref="FlottenAuslegungsAchse.AnzahlVon"/> (0 gilt als 1).
    /// </summary>
    Groesse = 1,

    /// <summary>
    /// Die STUECKZAHL suchen: an jeder eingeschalteten Einheit die Stueckzahl von–bis
    /// (0 = die Einheit entfaellt); die Groesse bleibt die der Vorlage. Kein Feinraster —
    /// zwischen zwei ganzen Zahlen gibt es nichts zu verfeinern.
    /// </summary>
    Stueckzahl = 2
}

/// <summary>
/// Der BEFUND der Vorpruefung eines Suchraums (Auftrag #247) — sprachneutral, weil die
/// Engine keine Ressourcen kennt; den Wortlaut waehlt die Oberflaeche.
/// </summary>
public enum FlottenSuchbefund
{
    /// <summary>Der Suchraum ist brauchbar.</summary>
    Inordnung = 0,

    /// <summary>
    /// Eine Suchmethode ist gewaehlt, aber keine Einheit traegt „variieren" — es gibt
    /// nichts zu suchen. Abhilfe: auf einer Einheitenkarte „variieren" einschalten.
    /// </summary>
    KeineAktiveAchse = 1,

    /// <summary>Ein Stueckzahlbereich ist leer oder negativ (Bis &lt; Von).</summary>
    StueckzahlbereichLeer = 2,

    /// <summary>Ein Groessenbereich ist unbrauchbar (leer, negativ, nicht positiv).</summary>
    GroessenbereichUnbrauchbar = 3,

    /// <summary>
    /// Die gewaehlte Quelle fuehrt kein Geraet, das gerechnet werden koennte. Abhilfe:
    /// die andere Quelle waehlen oder den Bestand pflegen.
    /// </summary>
    KeineGeraete = 4
}

/// <summary>Der Suchraum der Auslegungsoptimierung.</summary>
public sealed class FlottenAuslegungEingang
{
    /// <summary>Die Suchachsen; ohne aktive Achse bleibt es bei der konfigurierten Flotte.</summary>
    public List<FlottenAuslegungsAchse> Achsen { get; set; } = new();

    /// <summary>Die mitzurechnenden Betriebsziele; eine leere Liste bedeutet: nur das eingestellte Ziel.</summary>
    public List<FlottenBetriebsziel> Betriebsziele { get; set; } = new();

    /// <summary>Obergrenze der Kandidatenzahl, Standard 10 000. Ein groesseres Raster wird ABGEWIESEN und nicht gekuerzt.</summary>
    /// <remarks>Sie zaehlt BEIDE Phasen — Grobraster und Feinraster (Auftrag #224).</remarks>
    public int MaximaleKandidaten { get; set; } = 10000;

    /// <summary>
    /// NUR ZUM LESEN aelterer Staende: der Schalter der frueheren zweiten Suchphase.
    /// </summary>
    /// <remarks>
    /// <para><b>Die Geraetesuche hat nichts zu verfeinern.</b> Die zweite Phase legte
    /// Stuetzstellen ZWISCHEN die Rasterpunkte der Groessenachse und baute daraus eine
    /// Einheit — das war moeglich, solange eine freie Groesse aus einer Vorlage skaliert
    /// wurde. Kandidaten sind jetzt GERAETE; zwischen zwei Geraeten liegt kein drittes,
    /// und eine zwischengerechnete Groesse waere ein Speicher, den es nicht gibt. Damit
    /// gilt hier dieselbe Begruendung wie seit jeher fuer
    /// <see cref="FlottenSuchmethode.Stueckzahl"/>: zwischen zwei ganzen Zahlen gibt es
    /// nichts zu verfeinern.</para>
    /// <para>Das Feld bleibt, damit ein aelterer Stand es weiterhin lesen kann; auf den
    /// Lauf wirkt es nicht mehr, und <see cref="FlottenKandidatenzahl.FeinHoechstens"/>
    /// ist immer 0.</para>
    /// </remarks>
    public bool Feinraster { get; set; } = true;

    /// <summary>
    /// WAS dieser Lauf variiert (Auftrag #247, SD-E-10). Vorgabe
    /// <see cref="FlottenSuchmethode.Groesse"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum die Vorgabe „Groesse" ist und warum sie ein
    /// Eigenschaftsinitialisierer ist</b> — dieselbe Bauart und derselbe Grund wie bei
    /// <see cref="Feinraster"/>: Ein Stand, der das Feld nicht fuehrt (jeder vor #247
    /// gespeicherte), wird beim Einlesen ueber den Initialisierer auf
    /// <see cref="FlottenSuchmethode.Groesse"/> gesetzt und rechnet damit wie bisher —
    /// seine Stueckzahlbereiche gelten als fest auf dem Von-Wert (Konzept 8.3). Der
    /// Aufzaehlungswert 0 waere <see cref="FlottenSuchmethode.Bewerten"/> gewesen und
    /// haette jedem Altbestand still die Suche abgeschaltet.</para>
    /// <para>Ob ueberhaupt gesucht wird, sagt daneben
    /// <c>SpeicherAuslegungKonfiguration.FlottenGroessenOptimieren</c>; beide werden in
    /// <c>SpeicherFlottenStudieCtrl.Konfiguration</c> gleichgezogen.</para>
    /// </remarks>
    public FlottenSuchmethode Suchmethode { get; set; } = FlottenSuchmethode.Groesse;
}

/// <summary>Aus welcher Phase der Rastersuche ein Kandidat stammt (Auftrag #224).</summary>
public enum FlottenKandidatPhase
{
    /// <summary>Phase 1 — das vollstaendige Grobraster aus den Suchachsen.</summary>
    Grob = 0,

    /// <summary>Phase 2 — das engere Raster um das Grob-Optimum auf der Groessenachse.</summary>
    Fein = 1
}

/// <summary>
/// Die ZAEHLREGEL der Rastersuche (Auftrag #224): wie viele Kandidaten ein Suchraum
/// ergibt, ohne dass ein einziger gerechnet wuerde.
/// </summary>
/// <remarks>
/// <para><b>Eine Wahrheit, keine zweite Formel in der Seite.</b> Die Kandidatenzeile der
/// Station „4 Optimierung" zeigt dieselben Zahlen, mit denen
/// <see cref="FlottenOptimierer.Rechne"/> das Raster annimmt oder abweist — sie kommen
/// beide aus <see cref="FlottenOptimierer.Kandidatenzahl"/>.</para>
/// <para><b><see cref="FeinHoechstens"/> ist eine OBERGRENZE.</b> Wie breit das Feinraster
/// wird, haengt an der Lage des Grob-Optimums: Liegt es am Rand des Suchraums, wird das
/// Fenster einseitig gekappt. Vor dem Lauf steht nur der groesstmoegliche Fall fest, und
/// genau gegen ihn wird die Grenze geprueft — ein Lauf, der erst in Phase 2 auffliegt,
/// haette die erste Phase schon verrechnet.</para>
/// </remarks>
/// <param name="Grob">Kandidaten des Grobrasters: Hardwarevarianten × Betriebsziele.</param>
/// <param name="FeinHoechstens">Hoechstzahl der Kandidaten des Feinrasters; 0 = keines.</param>
/// <param name="Grenze">Die eingestellte Obergrenze (<see cref="FlottenAuslegungEingang.MaximaleKandidaten"/>).</param>
/// <param name="Gueltig">Der Suchraum ist lesbar; <c>false</c> = ein Bereich ist unbrauchbar und die Zahlen sagen nichts.</param>
/// <param name="Befund">
/// Der BEFUND der Vorpruefung (Auftrag #247): Er nennt den Grund sprachneutral, damit die
/// Oberflaeche die richtige Abhilfe zeigen kann — <see cref="FlottenSuchbefund.KeineAktiveAchse"/>
/// laesst die Zahlen ausdruecklich GUELTIG (ein Lauf ohne Achse rechnet die eingestellte
/// Flotte), verlangt aber eine Handlung des Anwenders.
/// </param>
public readonly record struct FlottenKandidatenzahl(long Grob, long FeinHoechstens, int Grenze,
                                                   bool Gueltig,
                                                   FlottenSuchbefund Befund = FlottenSuchbefund.Inordnung)
{
    /// <summary>Beide Phasen zusammen.</summary>
    public long Gesamt => Grob + FeinHoechstens;

    /// <summary>Haelt das Raster die Grenze ein?</summary>
    public bool Zulaessig => Gueltig && Grenze > 0 && Gesamt <= Grenze;
}

/// <summary>
/// Die Groessen EINER Einheit eines Kandidaten (Auftrag #193, Konzept
/// „Stromspeicher-Dialoge" 2.5).
/// </summary>
/// <remarks>
/// Die Summenwerte der <see cref="FlottenKandidatZusammenfassung"/> beantworten die
/// Frage „wie gross ist die Flotte", nicht die Frage „wie gross ist EIN Geraet". Die
/// Groessen-Sicht braucht beides: Die Rasterkarte laesst die Einheit waehlen und zeigt
/// bei mehr als einer die Summe (Konzept 2.5), und ohne die Einzelgroessen liesse sich
/// die Achse einer Flotte aus zwei verschiedenen Einheiten gar nicht bilden.
/// </remarks>
public sealed class FlottenKandidatEinheit
{
    /// <summary>Kennung der Einheit (<see cref="FlottenEinheit.Id"/>).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Kapazitaet [kWh] dieser einen Einheit.</summary>
    public double KapazitaetKWh { get; set; }

    /// <summary>Ladeleistung [kW] dieser einen Einheit.</summary>
    public double LadeleistungKw { get; set; }

    /// <summary>Entladeleistung [kW] dieser einen Einheit.</summary>
    public double EntladeleistungKw { get; set; }

    /// <summary>
    /// C-Rate [1/h] dieser Einheit: <c>Entladeleistung / Kapazitaet</c>; ohne Kapazitaet 0.
    /// </summary>
    public double CRate => KapazitaetKWh > 0.0 ? EntladeleistungKw / KapazitaetKWh : 0.0;
}

/// <summary>Die Zusammenfassung EINES geprueften Kandidaten; die vollstaendige Zeitreihe haelt nur der beste.</summary>
/// <remarks>
/// <b>Seit Auftrag #193 traegt sie auch die BETRIEBSkennzahlen</b> (Konzept
/// „Stromspeicher-Dialoge" 1.6 und 2.5): Durchsatz, Vollzyklen, Bezugsspitze,
/// Betriebsersparnis und die Aussage <see cref="Arbeitslos"/>. Bis dahin stand je
/// Kandidat nur der Kapitalwert da — ein arbeitsloser Kandidat war von einem
/// arbeitenden nicht zu unterscheiden. Die Zahlen kommen aus dem Kandidatenlauf, den
/// die Rastersuche ohnehin rechnet; eine zweite Simulation gibt es nicht.
/// </remarks>
public sealed class FlottenKandidatZusammenfassung
{
    /// <summary>Kennung des Kandidaten; sie enthaelt je Einheit Kennung, Kapazitaet sowie Lade- und Entladeleistung und das Betriebsziel.</summary>
    public string KandidatId { get; set; } = string.Empty;

    /// <summary>Das gerechnete Betriebsziel dieses Kandidaten.</summary>
    public FlottenBetriebsziel Betriebsziel { get; set; }

    /// <summary>Der Kandidat haelt alle HARTEN Grenzen ein. Ein unzulaessiger Kandidat gewinnt auch mit hohem Kapitalwert nicht.</summary>
    public bool Zulaessig { get; set; }

    /// <summary>Kapitalwert [EUR] des Kandidaten gegenueber der Variante ohne Zusatzspeicher.</summary>
    public double KapitalwertEuro { get; set; }

    /// <summary>Summe der Kapazitaeten [kWh] der Flotte dieses Kandidaten.</summary>
    public double KapazitaetKWh { get; set; }

    /// <summary>Summe der Ladeleistungen [kW] der Flotte dieses Kandidaten.</summary>
    public double LadeleistungKw { get; set; }

    /// <summary>Summe der Entladeleistungen [kW] der Flotte dieses Kandidaten.</summary>
    public double EntladeleistungKw { get; set; }

    /// <summary>Grund der Unzulaessigkeit oder des Rechenfehlers; <c>null</c> bei einem zulaessigen Kandidaten.</summary>
    public string? Grund { get; set; }

    // =====================================================================
    // Die Achsenwerte der Groessen-Sicht (Auftrag #193, Konzept 2.5)
    // =====================================================================

    /// <summary>Die Groessen JE EINHEIT, in der Reihenfolge der Einheiten; bei der Nullvariante leer.</summary>
    public List<FlottenKandidatEinheit> Einheiten { get; set; } = new();

    /// <summary>
    /// C-Rate [1/h] der Flotte: <c>Entladeleistung / Kapazitaet</c> ueber die Summen;
    /// ohne Kapazitaet 0. Bei gleich grossen Einheiten ist sie die C-Rate der Einheit.
    /// </summary>
    public double CRate => KapazitaetKWh > 0.0 ? EntladeleistungKw / KapazitaetKWh : 0.0;

    /// <summary>
    /// Die Stelle auf der ERSTEN Suchachse (Kapazitaet bzw. — im Modus
    /// <see cref="FlottenAuslegungsmodus.LeistungUndCRate"/> — Leistung); <c>-1</c>, wo
    /// der Achsenmodus sie nicht liefert.
    /// </summary>
    /// <remarks>
    /// Sie ist nur bei GENAU EINER aktiven Suchachse belegt. Mehrere Achsen spannen kein
    /// zweidimensionales Raster auf; eine Rasterkarte gaebe es dafuer nicht, und ein
    /// erfundener Index waere schlimmer als keiner.
    /// </remarks>
    public int Rasterzeile { get; set; } = -1;

    /// <summary>Die Stelle auf der ZWEITEN Suchachse (Leistung bzw. C-Rate); <c>-1</c> wie <see cref="Rasterzeile"/>.</summary>
    public int Rasterspalte { get; set; } = -1;

    /// <summary>
    /// Aus welcher PHASE der Suche dieser Kandidat stammt (Auftrag #224); Vorgabe
    /// <see cref="FlottenKandidatPhase.Grob"/>.
    /// </summary>
    /// <remarks>
    /// Ein Feinrasterpunkt liegt ZWISCHEN den Stuetzstellen des Grobrasters und traegt
    /// deshalb <see cref="Rasterzeile"/> = <see cref="Rasterspalte"/> = <c>-1</c>: Er hat
    /// keine Stelle im Grobgitter. Die Karte und die Schnitte ordnen ihn ueber seine WERTE
    /// ein, nicht ueber einen Index.
    /// </remarks>
    public FlottenKandidatPhase Phase { get; set; } = FlottenKandidatPhase.Grob;

    /// <summary>
    /// Die STUECKZAHL je aktiver Suchachse, in deren Reihenfolge (Auftrag #247); leer,
    /// wo es keine Suchachse gibt (Nullvariante, reine Bewertung).
    /// </summary>
    /// <remarks>
    /// <para>Sie ist die Achsenauskunft der Methode <see cref="FlottenSuchmethode.Stueckzahl"/>:
    /// Bei EINER variierten Einheit traegt sie die x-Achse der Kurve „Kapitalwert ueber
    /// Stueckzahl", bei ZWEI die zwei ganzzahligen Achsen der Rasterkarte n1 × n2
    /// (Konzept „Stromspeicher-Dialoge" 8.3, SD-Q17).</para>
    /// <para><b>Warum eine eigene Liste und nicht die Zahl der Einheiten.</b>
    /// <see cref="Einheiten"/> fuehrt ALLE Einheiten des Kandidaten — auch die festen,
    /// die keine Achse ersetzt. Aus ihrer Zahl liesse sich die Stueckzahl EINER Achse
    /// nicht zurueckrechnen, und aus den Kennungen abgelesen waere sie eine
    /// Textvereinbarung.</para>
    /// </remarks>
    public List<int> Stueckzahlen { get; set; } = new();

    // =====================================================================
    // Die Betriebskennzahlen des Kandidatenlaufs (Auftrag #193)
    // =====================================================================

    /// <summary>
    /// Jahresdurchsatz [kWh]: die AC-seitig ABGEGEBENE Energie der ganzen Flotte im
    /// ersten gerechneten Jahr. Die Entladung ist die Groesse, die Nutzen stiftet und
    /// Verschleiss kostet; die Ladung steht daneben in der Diagnose.
    /// </summary>
    public double DurchsatzKWh { get; set; }

    /// <summary>Vollzyklen [1/a]: <see cref="DurchsatzKWh"/> geteilt durch <see cref="KapazitaetKWh"/>; ohne Kapazitaet 0.</summary>
    public double Vollzyklen { get; set; }

    /// <summary>Hoechster Netzbezug [kW] des Kandidatenlaufs — die Groesse, die der Leistungspreis bewertet.</summary>
    public double BezugsspitzeKw { get; set; }

    /// <summary>
    /// Betriebsersparnis [EUR/a] gegenueber „ohne Speicher", OHNE Kapitaldienst:
    /// Rechnungsdifferenz des ersten Jahres abzueglich Betriebsaufwand und
    /// Durchsatzkosten. Investition, Ersatzinvestition und Restwert bleiben aussen vor —
    /// sie stecken im <see cref="KapitalwertEuro"/>.
    /// </summary>
    public double ErsparnisEuroJahr { get; set; }

    /// <summary>
    /// Die Flotte dieses Kandidaten hat im gerechneten Zeitraum weder geladen noch
    /// entladen (<see cref="FlottenDiagnose.Arbeitslos"/>, Aufgabe #183). Ein solcher
    /// Kandidat kostet nur.
    /// </summary>
    public bool Arbeitslos { get; set; }

    // =====================================================================
    //  Die Herkunft des Geraets (Anwenderentscheid vom 15.09.2026)
    // =====================================================================

    /// <summary>
    /// Kennung des Geraets in seiner Quelle (Katalog- beziehungsweise Anlagen-ID als
    /// Text); leer, wo kein Geraet dahintersteht (Nullvariante, Stueckzahlsuche, reine
    /// Bewertung).
    /// </summary>
    public string Quellkennung { get; set; } = string.Empty;

    /// <summary>
    /// Der NORMIERTE ABSTAND des Geraets zur Vorgabe des Anwenders; <c>0</c> = es liegt
    /// in beiden Bereichen.
    /// </summary>
    /// <remarks>
    /// Sie steht in der Kandidatentabelle, damit ein naheliegendes Geraet nicht fuer
    /// einen Treffer gehalten wird. Die Regel steht in
    /// <see cref="FlottenGeraetewahl"/>.
    /// </remarks>
    public double Abweichung { get; set; }

    /// <summary>
    /// An diesem Geraet musste wenigstens eine neutrale Vorgabe greifen
    /// (<see cref="FlottenGeraetevorgaben"/>); die Kandidatentabelle kennzeichnet es.
    /// </summary>
    public bool NeutraleKennwerte { get; set; }

    /// <summary>
    /// DIE HERLEITUNG DES KANDIDATEN: was vom GERAET stammt. Alles Uebrige kommt aus der
    /// Vorlage des Anwenders (<see cref="FlottenGeraeteuebernahme"/>). Bei mehreren
    /// Suchachsen bleibt sie leer — dort gibt es mehrere Geraete.
    /// </summary>
    public FlottenKennwertherkunft Kennwertherkunft { get; set; }
}

/// <summary>
/// Das Ergebnis der Rastersuche. Die Aussage gilt ausdruecklich nur fuer das gepruefte
/// endliche Raster (Spezifikation 9.4).
/// </summary>
public sealed class FlottenAuslegungErgebnis
{
    /// <summary>Die Nullvariante ohne Zusatzspeicher hat gewonnen: kein zulaessiger Kandidat erreicht einen Kapitalwert ueber 0.</summary>
    public bool NullvarianteGewonnen { get; set; }

    /// <summary>Die Nullvariante haelt selbst alle harten Grenzen ein. Ist sie unzulaessig, kann eine technisch noetige Variante trotz negativem Kapitalwert gewinnen.</summary>
    public bool NullvarianteZulaessig { get; set; }

    /// <summary>Die Aussage zum Ergebnis im Klartext; sie nennt die Grenze der Optimalitaetsbehauptung.</summary>
    public string Aussage { get; set; } = "Beste Variante im geprueften endlichen Raster";

    /// <summary>Die Zusammenfassung des besten zulaessigen Kandidaten; <c>null</c>, wenn es keinen gibt.</summary>
    public FlottenKandidatZusammenfassung? BesterKandidat { get; set; }

    /// <summary>Die vollstaendige Zeitreihe des besten Kandidaten; <c>null</c>, wenn es keinen gibt.</summary>
    public FlottenSimulationErgebnis? BesteZeitreihe { get; set; }

    /// <summary>Die Konfiguration des besten Kandidaten — die Vorlage fuer eine Uebernahme als Projektflotte.</summary>
    public FlottenStudieKonfiguration? BesteKonfiguration { get; set; }

    /// <summary>Die vollstaendige Gegenueberstellung des besten Kandidaten samt Kapitalwertrechnung.</summary>
    public FlottenStudienErgebnis? BesteStudie { get; set; }

    /// <summary>Die Zusammenfassungen ALLER geprueften Kandidaten, auch der unzulaessigen.</summary>
    public List<FlottenKandidatZusammenfassung> Kandidaten { get; set; } = new();

    /// <summary>
    /// Die GROESSENKOPPLUNG, mit der dieses Raster entstanden ist — der
    /// <see cref="FlottenAuslegungsAchse.Modus"/> der ersten AKTIVEN Suchachse.
    /// </summary>
    /// <remarks>
    /// <para>Sie steht HIER und nicht bei der Konfiguration, damit die Groessen-Sicht
    /// EINE Quelle hat: Ein Ergebnis wird aufbewahrt, weitergereicht und angezeigt,
    /// waehrend der Arbeitsstand daneben weiterbearbeitet wird — die Achsen der Karte
    /// gehoeren zum gerechneten Raster, nicht zum Stand von jetzt (Auftrag #226).</para>
    /// <para>Ohne aktive Suchachse gibt es kein Raster; dann bleibt die Vorbelegung
    /// <see cref="FlottenAuslegungsmodus.KapazitaetUndLeistung"/> stehen — die einzige
    /// gueltige Kopplung. Ein Ergebnis aus fremder Quelle, das eine C-Rate-Kopplung
    /// traegt, wird von <see cref="FlottenAltstand.Normalisiere(FlottenAuslegungErgebnis)"/>
    /// benannt umgesetzt.</para>
    /// </remarks>
    public FlottenAuslegungsmodus Achsenmodus { get; set; } = FlottenAuslegungsmodus.KapazitaetUndLeistung;

    /// <summary>
    /// Die zweite Phase ist wirklich gelaufen (Auftrag #224) — <c>false</c>, wenn sie
    /// abgeschaltet war, wenn es keine aktive Suchachse gab oder wenn das Grobraster
    /// keinen zulaessigen Besten hatte, um den herum zu verfeinern waere.
    /// </summary>
    public bool FeinrasterGerechnet { get; set; }

    /// <summary>
    /// Die gemessene Rechendauer der ganzen Suche — der Wert, den der Kasten „Bestes
    /// Ergebnis" neben der Kandidatenzahl nennt (Mappe V7, Zelle B7).
    /// </summary>
    public TimeSpan Rechendauer { get; set; }
}

/// <summary>Die Fortschrittsmeldung der Rastersuche.</summary>
public sealed class FlottenFortschritt
{
    /// <summary>Zahl der bereits geprueften Kandidaten.</summary>
    public int Abgeschlossen { get; set; }

    /// <summary>Zahl der insgesamt zu pruefenden Kandidaten.</summary>
    public int Gesamt { get; set; }

    /// <summary>Kennung des zuletzt geprueften Kandidaten.</summary>
    public string KandidatId { get; set; } = string.Empty;
}
