using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using KiKern;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// „DER ASSISTENT SETZT EIN FELD" — Weg 5 des Konzepts „Der Hilfe-Assistent im Dialog"
/// (Auftrag #201, Stufe S3).
///
/// <para><b>Was hier bewiesen wird.</b> Die BESTÄTIGUNG führt zur Änderung: Ein
/// <c>feld_setzen</c>, das durch Vorbereitung und Freigabe geht, landet in der
/// Eigenschaft des Daten-Objekts, an der auch das Eingabefeld hängt — und zwar für jede
/// der fünf Masken des Katalogs (Anwenderentscheid KI‑D‑Q3). Dazu die ABLEHNUNGEN, die
/// der Anwender zu sehen bekommt: ein abgeleitetes Feld, ein Typfehler, ein
/// schreibgeschützter Satz.</para>
///
/// <para><b>Warum ohne Renderer.</b> Dieselbe Begründung wie in
/// <see cref="KiFeldwerteTests"/>: Die Maskenbrücke ist prozessweiter Zustand, und xunit
/// fährt Testklassen nebeneinander — eine fremde Klasse, die denselben Dialog zeichnet,
/// löst die Anmeldung ab. Geprüft wird deshalb der WEG (Anmeldung über
/// <see cref="KiMaskenanmeldung"/>, Setzen über den Ausführer des Kerns) gegen die
/// ECHTEN Daten-Objekte der fünf Masken; dass die Komponenten ihre Haken anmelden, hält
/// <c>KiMaskenhakenTests</c> am gezeichneten Dialog fest.</para>
/// </summary>
public class KiFeldSetzenTests : EposBunitContext, IDisposable
{
    private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

    public KiFeldSetzenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
    }

    public new void Dispose()
    {
        Schreibnaht.Schreibrecht = _schreibrechtVorher;
        base.Dispose();
    }

    // =====================================================================
    //  Die fünf Masken — ein Fall je Maske (KI-D-Q3)
    // =====================================================================

    [Fact]
    public async Task Heizkessel_Die_Bestaetigung_setzt_die_thermische_Leistung()
    {
        var daten = new HeizkesselKatalogDaten { Ptherm = 100 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL, "th_leistung", "250,5");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(250.5, daten.Ptherm);
    }

    /// <summary>
    /// <b>Seit der Welle KI‑F7 meldet die Photovoltaik eine SICHTKLASSE an</b>
    /// (Anwenderentscheid 21.09.2026): <c>PhotovoltaikKiSicht</c> reicht die gewählte
    /// Zeile durch und trägt zusätzlich die zwei Auslegungstemperaturen des Projekts.
    /// Der Weg an die Zeile bleibt derselbe — das zeigt dieser Fall.
    /// </summary>
    [Fact]
    public async Task Photovoltaik_Die_Bestaetigung_setzt_die_Neigung_der_gewaehlten_Zeile()
    {
        var zeile = new ErzeugerZeile { Neigung = 30 };
        var sicht = new PhotovoltaikKiSicht { Zeilenquelle = () => zeile };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "neigung", "35");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(35, zeile.Neigung);
    }

    /// <summary>
    /// <b>Die AUSLEGUNGSTEMPERATUR geht nicht an die Zeile, sondern ans PROJEKT</b>
    /// (Welle KI‑F7): Sie steht in <c>Tab_Einstellungen</c> und gilt für jede Anlage
    /// darin; die Sicht führt sie über den Schreibweg der Maske.
    /// </summary>
    [Fact]
    public async Task Photovoltaik_Die_Auslegungstemperatur_geht_ueber_den_Weg_der_Maske()
    {
        double? kalt = -10.0;
        var zeile = new ErzeugerZeile();
        var sicht = new PhotovoltaikKiSicht
        {
            Zeilenquelle = () => zeile,
            KaltLesen = () => kalt,
            KaltSetzen = wert => kalt = wert
        };

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "auslegung_kalt", "-15,5");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(-15.5, kalt);
    }

    /// <summary>
    /// <b>Das RECHENMODELL ist ein Wahlfeld</b> (KI‑D‑Q6, KI‑D‑Q7): Auf der Maske steht
    /// ein Auswahlfeld mit „Einfach" und „Erweitert", und genau über diesen Text trifft
    /// der Assistent es — der Wahrheitswert der Anlage bleibt dahinter.
    /// </summary>
    [Fact]
    public async Task Photovoltaik_Das_Rechenmodell_wird_ueber_seinen_Text_gewaehlt()
    {
        var zeile = new ErzeugerZeile { ModellErweitert = false };
        var sicht = new PhotovoltaikKiSicht
        {
            Zeilenquelle = () => zeile,
            ModellEintraege = () => new[]
            {
                new KiWahleintrag("0", "Einfach"),
                new KiWahleintrag("1", "Erweitert")
            }
        };

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "modell_erweitert",
                                           "erweitert");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.True(zeile.ModellErweitert);
    }

    /// <summary>
    /// <b>Die Überlagerung „Anlagenwerte" ist eine EIGENE Maske</b> (Welle KI‑F7): Der
    /// Assistent schreibt in ihren Arbeitsstand, nicht an ihm vorbei in die Anlage —
    /// „OK" und „Abbrechen" bleiben der Klick des Anwenders.
    /// </summary>
    [Fact]
    public async Task Anlagenwerte_Die_Bestaetigung_setzt_den_Arbeitsstand_des_Fensters()
    {
        var zeile = new ErzeugerZeile { WrEta50 = 0.95 };
        var stand = new PvAnlagenwerteKiSicht();
        stand.Laden(zeile);

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PV_ANLAGENWERTE,
                                                     () => stand, Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PV_ANLAGENWERTE, "wr_eta50", "0,97");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(0.97, stand.Eta50);

        // Die ANLAGE bleibt unangetastet, bis das Fenster übernimmt.
        Assert.Equal(0.95, zeile.WrEta50);
        stand.Uebernehmen(zeile);
        Assert.Equal(0.97, zeile.WrEta50);
    }

    /// <summary>
    /// <b>Der Fall des Anwenderauftrags</b> (Welle #456): Die „Administration Heizkessel"
    /// ist offen, und „setze die Vorlauftemperatur auf 55 °C" landet im Feldsatz des
    /// Stammblatts — über die FELDTAFEL der Sichtklasse, mit dem toleranten Feldnamen.
    /// </summary>
    [Fact]
    public async Task Administration_Heizkessel_Die_Bestaetigung_setzt_den_Vorlauf()
    {
        var vorlauf = new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldVorlauf,
            Bezeichnung = "Vorlauftemperatur:",
            Einheit = "°C",
            Art = BrowserFeldArt.Ganzzahl,
            Editierbar = true,
            Wert = "70"
        };
        int gesetzt = 0;
        var sicht = new KatalogBrowserKiSicht
        {
            Feldsuche = s => s == vorlauf.Schluessel ? vorlauf : null,
            Gesetzt = () => gesetzt++
        };

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL_ADMIN, () => sicht,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL_ADMIN, "Vorlauftemperatur", "55");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal("55", vorlauf.Wert);
        Assert.Equal(1, gesetzt);
    }

    /// <summary>
    /// <b>Ein abgelehnter Satzwechsel kommt mit dem Grund des Dialogs zurück</b> — nicht
    /// mit der Hülle der Reflection („Exception has been thrown by the target of an
    /// invocation"), die der Setzer sonst um die Ablehnung legte.
    /// </summary>
    [Fact]
    public async Task Ein_abgelehnter_Satzwechsel_nennt_den_Grund_des_Dialogs()
    {
        const string grund = "Es gibt ungespeicherte Änderungen – bitte „Speichern“ oder „Verwerfen“.";
        string satz = "Kessel A";
        var sicht = new KatalogBrowserKiSicht
        {
            SatzLesen = () => satz,
            SatzSetzen = _ => grund,
            SatzEintraege = () => new[]
            {
                new KiWahleintrag("Kessel A", "Kessel A"),
                new KiWahleintrag("Kessel B", "Kessel B")
            }
        };

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL_ADMIN, () => sicht,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL_ADMIN, "satz", "Kessel B");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Contains("ungespeicherte Änderungen", ergebnis.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("target of an invocation", ergebnis.Text, StringComparison.Ordinal);
        Assert.Equal("Kessel A", satz);
    }

    [Fact]
    public async Task Pufferspeicher_Die_Bestaetigung_setzt_das_Gesamtvolumen()
    {
        var daten = new PufferSpKatalogDaten { Gesamtvolumen = 750 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PUFFERSPEICHER, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PUFFERSPEICHER, "gesamtvolumen", "1500");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(1500, daten.Gesamtvolumen);
    }

    /// <summary>
    /// <b>Der kanonische setzbare Fall der Wärmepumpenverwaltung ist die
    /// NENNLEISTUNG</b> — nicht mehr die Modulkosten.
    /// </summary>
    /// <remarks>
    /// Die Modulkosten sind seit dem Anwenderentscheid 21.09.2026 (KI‑D‑Q7)
    /// <c>nurLesen</c>: Die Maske zeigt sie als Lesewert mit Herleitungszeile, gepflegt
    /// werden sie in der Kostenverwaltung. Ein Fall, der sie setzt, prüfte damit nicht
    /// mehr das Setzen, sondern die Ablehnung — die steht weiter unten.
    /// </remarks>
    [Fact]
    public async Task Waermepumpe_Die_Bestaetigung_setzt_die_Nennleistung()
    {
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten { Nennleistung = 12 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.WAERMEPUMPE, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE, "nennleistung", "18");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(18, daten.Nennleistung);
    }

    /// <summary>
    /// <b>Die MODULKOSTEN sind eine Anzeige und werden benannt abgelehnt</b>
    /// (Anwenderentscheid 21.09.2026, KI‑D‑Q7) — dieselbe Lage wie an
    /// <c>Form_WP_Anlage</c>.
    /// </summary>
    [Fact]
    public async Task Waermepumpe_Die_Modulkosten_sind_nur_lesbar_und_werden_abgelehnt()
    {
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten { Modulkosten = 1000 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.WAERMEPUMPE, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE, "modulkosten", "2400");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(1000, daten.Modulkosten);
    }

    /// <summary>
    /// <b>Ein Wahlfeld wird über den ANGEZEIGTEN TEXT gesetzt</b> (KI-F1b, KI-D-Q6):
    /// Der Wärmepumpen-Katalog führt vier Bauarten in einer Klappliste, und der
    /// Assistent trifft sie über denselben Text, den der Anwender liest.
    /// </summary>
    [Fact]
    public async Task Waermepumpe_Die_Bauart_wird_ueber_ihren_Text_gewaehlt()
    {
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten
        { Modulkosten = 1000, Typ = "Sole-Wasser" };

        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.WAERMEPUMPE, () => daten, Haken(),
            ("typ", () => KiMaskenanmeldung.Eintraege(
                              EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammFelder.TYPEN,
                              s => s)));

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE, "typ", "luft-wasser");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal("Luft-Wasser", daten.Typ);
    }

    /// <summary>
    /// <b>Der tolerante Feldname</b> (KI-F1b): „Nennleistung des Geräts" trifft das
    /// Feld <c>nennleistung</c>, und das Ergebnis vermerkt die Auflösung.
    /// </summary>
    /// <remarks>
    /// Bis zum Anwenderentscheid 21.09.2026 stand hier „Modulkosten des Geräts"; das
    /// Feld ist seither <c>nurLesen</c> und taugt nicht mehr als setzbarer Fall.
    /// </remarks>
    [Fact]
    public async Task Ein_tolerant_genannter_Feldname_trifft_und_wird_vermerkt()
    {
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten { Nennleistung = 12 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.WAERMEPUMPE, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE,
                                           "Nennleistung des Geräts", "18");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(18, daten.Nennleistung);
        Assert.Contains("nennleistung", ergebnis.Kurzfassung(), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Das Betriebsziel der Stromspeicher-Ansicht ist eine WAHL</b> (KI-F1b): Die
    /// fünf Ziele stehen als Klappliste auf der Maske; gesetzt wird der Name des
    /// Aufzählungswertes, genannt sein Anzeigetext.
    /// </summary>
    [Fact]
    public async Task Stromspeicher_Das_Betriebsziel_wird_ueber_seinen_Text_gewaehlt()
    {
        SpeicherOptimierungEingaben eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                                                     () => sicht, Haken());

        KiFeldzugang ziel = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.STROMSPEICHER_AUSLEGUNG, "betriebsziel");

        Assert.True(ziel.IstWahl);
        Assert.Equal(5, ziel.Wahleintraege().Count);

        KiErgebnis ergebnis = await Setzen(
            KiMaskennamen.STROMSPEICHER_AUSLEGUNG, "betriebsziel",
            ziel.Wahleintraege()[(int)SpeicherEngine.FlottenBetriebsziel.PeakShaving].Text);

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(SpeicherEngine.FlottenBetriebsziel.PeakShaving,
                     eingaben.Auslegung!.Flotte!.Optionen.Betriebsziel);
    }

    [Fact]
    public async Task Stromspeicher_Die_Bestaetigung_setzt_die_Netzladefreigabe()
    {
        SpeicherOptimierungEingaben eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                                                     () => sicht, Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                                           "netzladung", "Ja");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.True(eingaben.Auslegung!.Flotte!.Optionen.NetzladungErlaubt);
    }

    // =====================================================================
    //  Die Ablehnungen, die der Anwender sieht
    // =====================================================================

    /// <summary>
    /// Ein ABGELEITETES Feld der Stromspeicher-Ansicht (die Diagnose) hat keinen Setzer —
    /// die Ablehnung nennt es beim Namen, statt still nichts zu tun.
    /// </summary>
    [Fact]
    public async Task Ein_abgeleitetes_Feld_wird_sichtbar_abgelehnt()
    {
        SpeicherOptimierungEingaben eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                                                     () => sicht, Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                                           "diagnose_arbeitslos", "Ja");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.NotEqual("", ergebnis.Text);
    }

    [Fact]
    public async Task Ein_Typfehler_wird_sichtbar_abgelehnt()
    {
        var daten = new HeizkesselKatalogDaten { Ptherm = 100 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL, "th_leistung", "viel");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Contains("viel", ergebnis.Text);
        Assert.Equal(100, daten.Ptherm);
    }

    /// <summary>
    /// Der SCHREIBGESCHÜTZTE Katalogsatz der Wärmepumpe (<c>ReadOnly</c> der
    /// <c>_STAMM</c>-Tabellen, Fachkonzept 4.5): Der Haken meldet ihn, und schon
    /// <c>feld_setzen</c> lehnt ab — nicht erst der Speicherknopf.
    /// </summary>
    [Fact]
    public async Task Ein_schreibgeschuetzter_Katalogsatz_wird_sichtbar_abgelehnt()
    {
        // Gesetzt wird ein SETZBARES Feld (die Nennleistung): Sonst läge die Ablehnung
        // schon am nurLesen des Feldes und nicht am Schreibschutz des Satzes — genau
        // das wäre die stumpfe Probe. Die Modulkosten sind seit dem Anwenderentscheid
        // 21.09.2026 nur lesbar und haben ihren eigenen Fall weiter oben.
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten
        {
            Nennleistung = 12,
            NurLesen = true
        };

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => daten.NurLesen };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.WAERMEPUMPE, () => daten, haken);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE, "nennleistung", "18");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(12, daten.Nennleistung);
    }

    /// <summary>
    /// Auftrag #211 (Restpunkt aus Bericht #201): Der Haken <c>Schreibgeschuetzt</c> war
    /// bisher nur an der Wärmepumpe verdrahtet — bei Heizkessel, Photovoltaik und
    /// Pufferspeicher lehnte erst der Speicherweg des Controllers beim „Überschreiben"
    /// ab, der Anwender bestätigte also eine Feldsetzung und scheiterte erst beim
    /// Speichern. Diese drei Fälle beweisen, dass jetzt schon <c>feld_setzen</c>
    /// ablehnt — dasselbe Muster wie bei der Wärmepumpe oben, mit dem neuen Kennzeichen
    /// <c>NurLesen</c> der drei Daten-Objekte.
    /// </summary>
    [Fact]
    public async Task Heizkessel_Ein_schreibgeschuetzter_Katalogsatz_wird_sichtbar_abgelehnt()
    {
        var daten = new HeizkesselKatalogDaten { Ptherm = 100, NurLesen = true };

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => daten.NurLesen };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL, () => daten, haken);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL, "th_leistung", "250,5");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(100, daten.Ptherm);
    }

    [Fact]
    public async Task Photovoltaik_Ein_schreibgeschuetzter_Katalogsatz_wird_sichtbar_abgelehnt()
    {
        var zeile = new ErzeugerZeile { Neigung = 30, NurLesen = true };
        var sicht = new PhotovoltaikKiSicht { Zeilenquelle = () => zeile };

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => zeile.NurLesen };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht, haken);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "neigung", "35");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(30, zeile.Neigung);
    }

    [Fact]
    public async Task Pufferspeicher_Ein_schreibgeschuetzter_Katalogsatz_wird_sichtbar_abgelehnt()
    {
        var daten = new PufferSpKatalogDaten { Gesamtvolumen = 750, NurLesen = true };

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => daten.NurLesen };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PUFFERSPEICHER, () => daten, haken);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PUFFERSPEICHER, "gesamtvolumen", "1500");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(750, daten.Gesamtvolumen);
    }

    /// <summary>
    /// <b>Ohne Freigabe geschieht nichts</b> — der Riegel der Bestätigungsschicht sitzt im
    /// Ausführer und nicht nur im Chat (Fachkonzept 3.5).
    /// </summary>
    [Fact]
    public async Task Ohne_Bestaetigung_bleibt_das_Feld_stehen()
    {
        var daten = new PufferSpKatalogDaten { Gesamtvolumen = 750 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PUFFERSPEICHER, () => daten,
                                                     Haken());

        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiErgebnis ergebnis = await schicht.AusfuehrenAsync(
            "feld_setzen", Werte(KiMaskennamen.PUFFERSPEICHER, "gesamtvolumen", "1500"));

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(750, daten.Gesamtvolumen);
    }

    /// <summary>
    /// Die MASKE frischt sich nach dem Setzen auf — sonst stünde in der Oberfläche die
    /// alte Zahl, während der Assistent „gesetzt" meldet.
    /// </summary>
    [Fact]
    public async Task Nach_dem_Setzen_frischt_die_Maske_auf()
    {
        var daten = new PufferSpKatalogDaten();
        int gezeichnet = 0;
        var haken = new KiMaskenhaken { Auffrischen = () => gezeichnet++ };

        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PUFFERSPEICHER, () => daten, haken);

        await Setzen(KiMaskennamen.PUFFERSPEICHER, "gesamtvolumen", "1500");

        Assert.True(gezeichnet > 0, "Die Maske wurde nach dem Setzen nicht aufgefrischt.");
    }

    // =====================================================================
    //  Der Anwenderweg in der Verwaltung Heizkessel (Meldung vom 10.10.2026)
    // =====================================================================

    /// <summary>
    /// „setze Rücklauftemperatur auf 65" im offenen Projektdialog — die erste Zeile ist
    /// gewählt wie beim Öffnen. Der Assistent muss danach DENSELBEN Zustand hinterlassen
    /// wie eine Eingabe von Hand (<c>BeiRuecklauf</c>): Zeilenwert, Eingabefeld,
    /// Herleitungszeile weg und die Zeile ins Modell übernommen (die Arbeitskopie, aus
    /// der der Dialog speichert).
    /// </summary>
    [Fact]
    public async Task Verwaltung_Heizkessel_Ruecklauf_setzen_wirkt_wie_die_Eingabe_von_Hand()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var zeile = Kesselzeile(1, "Kessel A");
        zeile.TemperaturHerleitung = "Vorgabe 70/50 °C";
        var cut = Heizkesseldialog(new List<ErzeugerZeile> { zeile }, uebernommen.Add);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL_PROJEKT,
                                           "Rücklauftemperatur", "65");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(65, cut.Instance.Projektzeile!.Ruecklauf);
        Assert.Contains("Rücklauf", ergebnis.Text);
        Assert.Contains("„65“", ergebnis.Text);
        Assert.Contains("„50“", ergebnis.Text);
        Assert.Equal("", zeile.TemperaturHerleitung);
        Assert.Single(uebernommen);
        Assert.Same(zeile, uebernommen[0]);
        cut.WaitForAssertion(() =>
            Assert.Equal("65", cut.FindAll("input[inputmode=numeric]")[1].GetAttribute("value")));
    }

    /// <summary>
    /// Ohne gewählte Projektzeile — der Anwender hat eine Katalogzeile angeklickt — gibt
    /// es keine Anlage, in die der Wert gehört. Erwartet ist eine BENANNTE Absage, kein
    /// stilles „gesetzt".
    /// </summary>
    [Fact]
    public async Task Verwaltung_Heizkessel_ohne_gewaehlte_Zeile_lehnt_benannt_ab()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var zeilen = new List<ErzeugerZeile> { Kesselzeile(1, "Kessel A"), Kesselzeile(2, "Kessel B") };
        var cut = Heizkesseldialog(zeilen, uebernommen.Add);
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        Assert.Null(cut.Instance.Projektzeile);

        // Der Sperrgrund der Maske lehnt VOR der Bestätigung ab — der Anwender bestätigt
        // nichts, das danach scheitert.
        KiVorbereitung vorbereitung = await Vorbereiten("feld_setzen",
            Werte(KiMaskennamen.HEIZKESSEL_PROJEKT, "Rücklauftemperatur", "65"));
        Assert.Null(vorbereitung.Freigabe);
        KiErgebnis ergebnis = vorbereitung.Ablehnung;

        Assert.NotEqual(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(Resource.KI_ERZ_KEINE_PROJEKTZEILE, ergebnis.Text);
        Assert.All(zeilen, z => Assert.Equal(50, z.Ruecklauf));
        Assert.Empty(uebernommen);
    }

    /// <summary>
    /// Trägt das Projekt GENAU EINE Anlage und ist keine gewählt, wählt der Assistent sie
    /// wie das Öffnen des Dialogs — es gibt keine zweite, die gemeint sein könnte.
    /// </summary>
    [Fact]
    public async Task Verwaltung_Heizkessel_mit_einer_Zeile_waehlt_sie_wie_beim_Oeffnen()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var zeile = Kesselzeile(1, "Kessel A");
        var cut = Heizkesseldialog(new List<ErzeugerZeile> { zeile }, uebernommen.Add);
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle--name")[0].Click();
        Assert.Null(cut.Instance.Projektzeile);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.HEIZKESSEL_PROJEKT, "ruecklauf", "65");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Same(zeile, cut.Instance.Projektzeile);
        Assert.Equal(65, zeile.Ruecklauf);
        Assert.Single(uebernommen);
    }

    // =====================================================================
    //  Dasselbe Muster an den übrigen Erzeugermasken
    // =====================================================================

    /// <summary>
    /// Die Sichtklasse der Projektzeile (Heizkessel, BHKW, Pufferspeicher,
    /// Stromspeicher): Jedes Feld der Zeile geht nach dem Setzen den Übernahmeweg der
    /// Hand — und ohne Zeile lehnt es benannt ab.
    /// </summary>
    [Theory]
    [InlineData(KiMaskennamen.HEIZKESSEL_PROJEKT, "vorlauf", "75")]
    [InlineData(KiMaskennamen.HEIZKESSEL_PROJEKT, "ruecklauf", "55")]
    [InlineData(KiMaskennamen.BHKW_PROJEKT, "vorlauf", "75")]
    [InlineData(KiMaskennamen.BHKW_PROJEKT, "ruecklauf", "55")]
    [InlineData(KiMaskennamen.BHKW_PROJEKT, "grenzleistung", "40")]
    public async Task Erzeugerzeile_Setzen_uebernimmt_wie_die_Hand(string maske, string feld, string wert)
    {
        var zeile = Kesselzeile(1, "Anlage");
        var uebernommen = new List<ErzeugerZeile>();
        var sicht = new ErzeugerProjektKiSicht { Zeilenquelle = () => zeile, Uebernommen = uebernommen.Add };
        using var anmeldung = KiMaskenanmeldung.Fuer(maske, () => sicht, Haken());

        KiErgebnis ergebnis = await Setzen(maske, feld, wert);

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Single(uebernommen);
    }

    [Theory]
    [InlineData(KiMaskennamen.HEIZKESSEL_PROJEKT, "ruecklauf", "65")]
    [InlineData(KiMaskennamen.BHKW_PROJEKT, "grenzleistung", "40")]
    public async Task Erzeugerzeile_ohne_Zeile_lehnt_benannt_ab(string maske, string feld, string wert)
    {
        var sicht = new ErzeugerProjektKiSicht { Zeilenquelle = () => null };
        using var anmeldung = KiMaskenanmeldung.Fuer(maske, () => sicht, Haken());

        KiErgebnis ergebnis = await Setzen(maske, feld, wert);

        Assert.NotEqual(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(Resource.KI_ERZ_KEINE_PROJEKTZEILE, ergebnis.Text);
    }

    /// <summary>Photovoltaik: dieselben zwei Zusagen an ihrer eigenen Sicht.</summary>
    [Fact]
    public async Task Photovoltaik_Setzen_uebernimmt_und_ohne_Zeile_lehnt_es_ab()
    {
        var zeile = new ErzeugerZeile { Neigung = 30 };
        var uebernommen = new List<ErzeugerZeile>();
        ErzeugerZeile? gewaehlt = zeile;
        var sicht = new PhotovoltaikKiSicht { Zeilenquelle = () => gewaehlt, Uebernommen = uebernommen.Add };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => sicht, Haken());

        Assert.Equal(KiStatus.Ausgefuehrt, (await Setzen(KiMaskennamen.PHOTOVOLTAIK, "neigung", "35")).Status);
        Assert.Equal(35, zeile.Neigung);
        Assert.Single(uebernommen);

        gewaehlt = null;
        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "neigung", "40");
        Assert.NotEqual(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(Resource.KI_ERZ_KEINE_PROJEKTZEILE, ergebnis.Text);
        Assert.Equal(35, zeile.Neigung);
    }

    /// <summary>Solarkollektoren im Projekt: ohne gewählte Zeile kein Arbeitsstand — benannte Absage.</summary>
    [Fact]
    public async Task Solarkollektoren_ohne_Zeile_lehnt_benannt_ab()
    {
        var sicht = new SolarkollektorenKiSicht { Eingabenquelle = () => null };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, () => sicht, Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, "neigung", "35");

        Assert.NotEqual(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(Resource.KI_ERZ_KEINE_PROJEKTZEILE, ergebnis.Text);
    }

    /// <summary>
    /// Eine Maske, deren Daten-Objekt gerade fehlt (die Photovoltaik ohne jede gewählte
    /// Zeile meldet gar keine Sicht), verwirft einen Wert nicht still.
    /// </summary>
    [Fact]
    public async Task Ohne_Daten_Objekt_lehnt_das_Setzen_benannt_ab()
    {
        using var anmeldung = KiMaskenanmeldung.Fuer<PhotovoltaikKiSicht>(KiMaskennamen.PHOTOVOLTAIK,
                                                                         () => null, Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "neigung", "35");

        Assert.NotEqual(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(Resource.KI_FELD_KEIN_SATZ, ergebnis.Text);
    }

    // =====================================================================
    //  Sperrgrund je Feld (KiMaskenhaken.Sperrgrund) — Absage vor der Bestätigung
    // =====================================================================

    private const string GRUND_PROBE = "Probegrund der Maske";

    /// <summary>
    /// Eine von Hand gebaute Maske sperrt den Vorlauf: <c>feld_setzen</c> lehnt VOR der
    /// Bestätigung ab und nennt Feldname und Grund; der Rücklauf bleibt frei.
    /// </summary>
    [Fact]
    public async Task Sperrgrund_lehnt_das_gesperrte_Feld_vor_der_Bestaetigung_ab()
    {
        var zeile = Kesselzeile(1, "Anlage");
        var uebernommen = new List<ErzeugerZeile>();
        var sicht = new ErzeugerProjektKiSicht { Zeilenquelle = () => zeile, Uebernommen = uebernommen.Add };
        var haken = new KiMaskenhaken { Sperrgrund = f => f == "vorlauf" ? GRUND_PROBE : null };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL_PROJEKT, () => sicht, haken);

        KiVorbereitung vorbereitung = await Vorbereiten("feld_setzen",
            Werte(KiMaskennamen.HEIZKESSEL_PROJEKT, "vorlauf", "75"));

        Assert.Null(vorbereitung.Freigabe);
        Assert.Equal(KiStatus.Abgelehnt, vorbereitung.Ablehnung.Status);
        string name = KiMaskenbruecke.Feldzugang(KiMaskennamen.HEIZKESSEL_PROJEKT, "vorlauf")!.Feld.Anzeigename;
        Assert.Contains(name, vorbereitung.Ablehnung.Text);
        Assert.Contains(GRUND_PROBE, vorbereitung.Ablehnung.Text);
        Assert.Equal(70, zeile.Vorlauf);

        KiErgebnis frei = await Setzen(KiMaskennamen.HEIZKESSEL_PROJEKT, "ruecklauf", "55");
        Assert.Equal(KiStatus.Ausgefuehrt, frei.Status);
        Assert.Equal(55, zeile.Ruecklauf);
        Assert.Single(uebernommen);
    }

    /// <summary>
    /// <c>formular_ausfuellen</c> mit einem gesperrten Feld unter mehreren: Der ganze Block
    /// wird vor der Bestätigung abgelehnt, auch das freie Feld bleibt stehen.
    /// </summary>
    [Fact]
    public async Task Sperrgrund_lehnt_formular_ausfuellen_vor_der_Bestaetigung_ab()
    {
        var zeile = Kesselzeile(1, "Anlage");
        var sicht = new ErzeugerProjektKiSicht { Zeilenquelle = () => zeile };
        var haken = new KiMaskenhaken { Sperrgrund = f => f == "vorlauf" ? GRUND_PROBE : null };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL_PROJEKT, () => sicht, haken);

        KiVorbereitung vorbereitung = await Vorbereiten("formular_ausfuellen",
            new Dictionary<string, object?>
            {
                ["maske"] = KiMaskennamen.HEIZKESSEL_PROJEKT,
                ["werte"] = "ruecklauf=55; vorlauf=75"
            });

        Assert.Null(vorbereitung.Freigabe);
        Assert.Contains(GRUND_PROBE, vorbereitung.Ablehnung.Text);
        Assert.Equal(70, zeile.Vorlauf);
        Assert.Equal(50, zeile.Ruecklauf);
    }

    /// <summary>
    /// Der Energieträger am GEZEICHNETEN Heizkesseldialog: Absage vor der Bestätigung mit
    /// dem Hinweis auf den Handweg; die Zeile behält ihren Träger, und <c>dialog_lesen</c>
    /// zeigt ihn weiterhin.
    /// </summary>
    [Fact]
    public async Task Verwaltung_Heizkessel_Energietraeger_wird_vor_der_Bestaetigung_abgelehnt()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var zeile = Kesselzeile(1, "Kessel A");
        var cut = Heizkesseldialog(new List<ErzeugerZeile> { zeile }, uebernommen.Add);
        Assert.Same(zeile, cut.Instance.Projektzeile);

        KiVorbereitung vorbereitung = await Vorbereiten("feld_setzen",
            Werte(KiMaskennamen.HEIZKESSEL_PROJEKT, "energietraeger", "Erdgas E Variante"));

        Assert.Null(vorbereitung.Freigabe);
        Assert.Contains(Resource.KI_ERZ_TRAEGER_VON_HAND, vorbereitung.Ablehnung.Text);
        Assert.Equal(5, zeile.CarrierId);
        Assert.Empty(uebernommen);

        KiErgebnis gelesen = await new KiAusfuehrung { Schreibrecht = () => true }.AusfuehrenAsync(
            "dialog_lesen", new Dictionary<string, object> { ["maske"] = KiMaskennamen.HEIZKESSEL_PROJEKT });
        Assert.Equal(KiStatus.Ausgefuehrt, gelesen.Status);
        var traeger = Assert.Single(gelesen.Zeilen, z => Equals(z["name"], "energietraeger"));
        Assert.NotEqual("", Convert.ToString(traeger["wert"]));
    }

    /// <summary>
    /// Derselbe Riegel an BHKW, Stromspeicher und Photovoltaik — die Sicht von Hand, ihr
    /// Sperrgrund als Haken wie im Dialog.
    /// </summary>
    [Theory]
    [InlineData(KiMaskennamen.BHKW_PROJEKT)]
    [InlineData(KiMaskennamen.STROMSPEICHER_PROJEKT)]
    [InlineData(KiMaskennamen.PHOTOVOLTAIK)]
    public async Task Erzeuger_Energietraeger_wird_vor_der_Bestaetigung_abgelehnt(string maske)
    {
        var zeile = Kesselzeile(1, "Anlage");
        KiMaskenanmeldung anmeldung = maske == KiMaskennamen.PHOTOVOLTAIK
            ? Angemeldet(maske, new PhotovoltaikKiSicht { Zeilenquelle = () => zeile, Zeilenzahl = () => 1 },
                         s => s.Sperrgrund)
            : Angemeldet(maske, new ErzeugerProjektKiSicht { Zeilenquelle = () => zeile, Zeilenzahl = () => 1 },
                         s => s.Sperrgrund);
        using (anmeldung)
        {
            KiVorbereitung vorbereitung = await Vorbereiten("feld_setzen", Werte(maske, "energietraeger", "5"));

            Assert.Null(vorbereitung.Freigabe);
            Assert.Contains(Resource.KI_ERZ_TRAEGER_VON_HAND, vorbereitung.Ablehnung.Text);
            Assert.Equal(5, zeile.CarrierId);
            Assert.Equal(5, Convert.ToInt32(KiMaskenbruecke.Feldzugang(maske, "energietraeger")!.Lesen()));
        }
    }

    /// <summary>
    /// Die zweite Sicherung: Ohne Sperrgrund im Haken lehnt der Setzer des Energieträgers
    /// selbst benannt ab — nach der Bestätigung, aber nie still.
    /// </summary>
    [Fact]
    public async Task Erzeuger_Energietraeger_Setzer_lehnt_auch_ohne_Sperrgrund_ab()
    {
        var zeile = Kesselzeile(1, "Anlage");
        var sicht = new ErzeugerProjektKiSicht { Zeilenquelle = () => zeile };
        IReadOnlyList<KiWahleintrag> Eintraege()
            => KiMaskenanmeldung.Eintraege(new[] { (5, "Erdgas"), (6, "Biogas") }, v => v.Item1, v => v.Item2);
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.BHKW_PROJEKT, () => sicht, Haken(),
                                                     ("energietraeger", Eintraege));

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.BHKW_PROJEKT, "energietraeger", "Biogas");

        Assert.NotEqual(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(Resource.KI_ERZ_TRAEGER_VON_HAND, ergebnis.Text);
        Assert.Throws<InvalidOperationException>(() => new PhotovoltaikKiSicht().CarrierId = 7);
    }

    /// <summary>
    /// „Keine Anlage gewählt" an der Sicht: gesperrt bei keiner oder zwei Zeilen, frei bei
    /// genau einer (die Einzelwahl greift) und für die Felder des Aufklappers „Alle Daten".
    /// </summary>
    [Fact]
    public void Erzeuger_Sperrgrund_ohne_Wahl_haengt_an_der_Zeilenzahl()
    {
        int zahl = 2;
        var sicht = new ErzeugerProjektKiSicht { Zeilenquelle = () => null, Zeilenzahl = () => zahl };
        var solar = new SolarkollektorenKiSicht { Eingabenquelle = () => null, Zeilenzahl = () => zahl };
        var pv = new PhotovoltaikKiSicht { Zeilenquelle = () => null, Zeilenzahl = () => zahl };

        Assert.Equal(Resource.KI_ERZ_KEINE_PROJEKTZEILE, sicht.Sperrgrund("ruecklauf"));
        Assert.Equal(Resource.KI_ERZ_KEINE_PROJEKTZEILE, solar.Sperrgrund("neigung"));
        Assert.Equal(Resource.KI_ERZ_KEINE_PROJEKTZEILE, pv.Sperrgrund("auslegung_kalt"));
        Assert.Null(sicht.Sperrgrund(KiDialoge.KATALOGFELD_VORSILBE + "leistung"));
        Assert.Null(pv.Sperrgrund("strang_neigung"));

        zahl = 1;
        Assert.Null(sicht.Sperrgrund("ruecklauf"));
        Assert.Null(solar.Sperrgrund("neigung"));
        Assert.Equal(Resource.KI_ERZ_TRAEGER_VON_HAND, sicht.Sperrgrund("energietraeger"));

        zahl = 0;
        Assert.Equal(Resource.KI_ERZ_KEINE_PROJEKTZEILE, pv.Sperrgrund("neigung"));
    }

    private static KiMaskenanmeldung Angemeldet<T>(string maske, T sicht, Func<T, Func<string, string?>> sperre)
        where T : class
        => KiMaskenanmeldung.Fuer(maske, () => sicht, new KiMaskenhaken { Sperrgrund = f => sperre(sicht)(f) });

    private static ErzeugerZeile Kesselzeile(int schluessel, string name)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = 100 + schluessel,
                   CarrierId = 5, Vorlauf = 70, Ruecklauf = 50 };

    /// <summary>Der Projektdialog Heizkessel mit dem Nötigsten (Muster <c>HeizkesselDialogTests</c>).</summary>
    private IRenderedComponent<HeizkesselDialog> Heizkesseldialog(List<ErzeugerZeile> zeilen,
                                                                  Action<ErzeugerZeile> uebernehmen)
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        var profil = Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel,
                                                       s => Resource.ResourceManager.GetString(s) ?? s);
        var detail = new ErzeugerDetail("Kessel A", "Beschreibung",
                                        new[] { ("Leistung [kW]:", "120,00") });
        return Render<HeizkesselDialog>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogprofil, profil)
            .Add(x => x.Katalogzeilen, () => new[]
            {
                new Katalogfilterzeile(11, "Katalogkessel")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Katalogkessel")
            })
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.KatalogDetail, _ => detail)
            .Add(x => x.ProjektDetail, _ => detail)
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante") })
            .Add(x => x.Uebernehmen, uebernehmen));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static KiMaskenhaken Haken() => new KiMaskenhaken();

    private static IReadOnlyDictionary<string, object?> Werte(string maske, string feld, string wert)
        => new Dictionary<string, object?> { ["maske"] = maske, ["feld"] = feld, ["wert"] = wert };

    /// <summary>
    /// Nur die Vorbereitung: <c>Freigabe is null</c> heißt Absage VOR der Bestätigung
    /// (<c>Ablehnung</c>).
    /// </summary>
    private static async Task<KiVorbereitung> Vorbereiten(string aktion, IReadOnlyDictionary<string, object?> werte)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, aktion, werte);
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());
        return await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);
    }

    /// <summary>Vorbereiten, freigeben, ausführen — der ganze Weg einer Feldsetzung.</summary>
    private static async Task<KiErgebnis> Setzen(string maske, string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };

        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen",
                                                     Werte(maske, feld, wert));
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());

        KiVorbereitung vorbereitung =
            await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);

        if (vorbereitung.Freigabe is null) return vorbereitung.Ablehnung;

        vorbereitung.Freigabe.Erteilen();
        return await schicht.AusfuehrenAsync(geprueft.Aufruf, vorbereitung.Freigabe,
                                             CancellationToken.None);
    }

    /// <summary>
    /// Ein Arbeitsstand mit genau einer Flotteneinheit.
    /// </summary>
    /// <remarks>
    /// <b>Auslegung und Flotte werden ausdrücklich angelegt</b> — beide Eigenschaften
    /// tragen keinen Anfangswert, und genau das ist der Fall, den
    /// <see cref="StromspeicherKiSicht"/> mit ihren <c>null</c>-Stufen abfängt: Ohne
    /// Arbeitsstand hat die Ansicht keine Flotte, und die Felder sind leer.
    /// </remarks>
    private static SpeicherOptimierungEingaben Eingaben()
    {
        var flotte = new FlottenStudieKonfiguration();
        flotte.Einheiten.Add(new FlottenEinheit
        {
            Name = "E1",
            KapazitaetKWh = 24,
            LadeleistungKw = 10,
            EntladeleistungKw = 12
        });
        flotte.Optionen.NetzladungErlaubt = false;

        return new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration { Flotte = flotte }
        };
    }
}
