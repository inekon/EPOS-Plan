using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using KiKern;
using SpeicherEngine;
using WindowsFormsApplication1;
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
    //  Hilfen
    // =====================================================================

    private static KiMaskenhaken Haken() => new KiMaskenhaken();

    private static IReadOnlyDictionary<string, object?> Werte(string maske, string feld, string wert)
        => new Dictionary<string, object?> { ["maske"] = maske, ["feld"] = feld, ["wert"] = wert };

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
