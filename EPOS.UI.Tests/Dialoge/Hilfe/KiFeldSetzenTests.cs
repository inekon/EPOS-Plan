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

    [Fact]
    public async Task Photovoltaik_Die_Bestaetigung_setzt_die_Neigung_der_gewaehlten_Zeile()
    {
        var zeile = new ErzeugerZeile { Neigung = 30 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => zeile,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.PHOTOVOLTAIK, "neigung", "35");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(35, zeile.Neigung);
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

    [Fact]
    public async Task Waermepumpe_Die_Bestaetigung_setzt_die_Modulkosten()
    {
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten { Modulkosten = 1000 };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.WAERMEPUMPE, () => daten,
                                                     Haken());

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE, "modulkosten", "2400");

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Equal(2400, daten.Modulkosten);
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
        var daten = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten
        {
            Modulkosten = 1000,
            NurLesen = true
        };

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => daten.NurLesen };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.WAERMEPUMPE, () => daten, haken);

        KiErgebnis ergebnis = await Setzen(KiMaskennamen.WAERMEPUMPE, "modulkosten", "2400");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Equal(1000, daten.Modulkosten);
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

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => zeile.NurLesen };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.PHOTOVOLTAIK, () => zeile, haken);

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
