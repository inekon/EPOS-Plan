using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// DIE SIMULATIONSANSICHT AN DER MASKENBRÜCKE (Auftrag #221, Anwenderentscheid
/// <b>KI‑D‑E‑1</b> vom 11.09.2026).
///
/// <para><b>Der Befund.</b> „Es muss einen Kontext in der KI-Funktion der zweiten Sicht
/// geben, der sich von dem anderen KI-Button unterscheidet." Bis hierher meldete die
/// Ansicht „Simulation" NICHTS an — <c>dialog_lesen</c> und der Schalter „Feldwerte
/// mitsenden" hatten dort nichts zu zeigen, und der ganze Unterschied zum Assistenten
/// des Hauptfensters war eine Bereichszeichenkette.</para>
///
/// <para><b>Das Soll.</b> Schritt ① meldet Kaskade und Reihenfolge (lesend) und die FÜNF
/// Laufparameter (lesbar und setzbar, Schreibweg <see cref="SimulationParameterDienste"/>),
/// Schritt ③ die Kennzahlen des Laufs (nur lesend) samt dem offenen Reiterblatt und den
/// Laufhinweisen.</para>
///
/// <para><b>Warum ohne Renderer</b> (Muster <c>KiFeldSetzenTests</c>): Die Maskenbrücke
/// ist prozessweiter Zustand, und xunit fährt Testklassen nebeneinander. Geprüft wird
/// der WEG — Anmeldung über <see cref="KiMaskenanmeldung"/>, Lesen über die Brücke,
/// Setzen über den Ausführer des Kerns — gegen das ECHTE Sichtmodell der Ansicht.</para>
/// </summary>
public class KiSimulationMaskeTests : IDisposable
{
    private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

    // Die Maskenbrücke formatiert Feldwerte mit der PROZESSKULTUR (KiMaskenbruecke,
    // CultureInfo.CurrentCulture); die Erwartungen unten ("420,5") sind de-DE. Ohne
    // Pinnen hängt der Fall an der Laufreihenfolge — im Gate sept31 (Merge #221) fiel
    // er mit "420.5". Hausvorrichtung seit #168, Rückstellung in Dispose.
    private readonly Kulturvorrichtung _kultur = new();

    public KiSimulationMaskeTests() => Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;

    public void Dispose()
    {
        Schreibnaht.Schreibrecht = _schreibrechtVorher;
        _kultur.Dispose();
    }

    // =====================================================================
    //  Probendaten — die vier Stände der Ansicht
    // =====================================================================

    /// <summary>Eine Kaskade aus zwei aufgenommenen und einem verfügbaren Erzeuger.</summary>
    private static SimulationKonfigDaten Konfigstand() => new SimulationKonfigDaten
    {
        IdProjekt = 1030,
        Gruppen = new[]
        {
            new KachelGruppe
            {
                Titel = "Wärmeerzeuger",
                Zeilen = new[]
                {
                    new ErzeugerZeile
                    {
                        DbWert = "BHKW", Bezeichner = "BHKW 1", HatAnlage = true,
                        Kachel = new EPOS.UI.Bausteine.ErzeugerKachelDaten
                        {
                            Schluessel = "BHKW", Rang = "1.", Titel = "BHKW 1"
                        }
                    },
                    new ErzeugerZeile
                    {
                        DbWert = "HEIZKESSEL", Bezeichner = "Kessel", HatAnlage = true,
                        Kachel = new EPOS.UI.Bausteine.ErzeugerKachelDaten
                        {
                            Schluessel = "HEIZKESSEL", Rang = "2.", Titel = "Kessel"
                        }
                    },
                    new ErzeugerZeile
                    {
                        // ANGELEGT, aber auf keinem Platz der Simulation (#190).
                        DbWert = "SOLARTHERMIE", Bezeichner = "Kollektorfeld",
                        HatAnlage = true, Verfuegbar = true,
                        Kachel = new EPOS.UI.Bausteine.ErzeugerKachelDaten
                        {
                            Schluessel = "SOLARTHERMIE", Titel = "Kollektorfeld"
                        }
                    }
                }
            }
        }
    };

    private static SimulationErgebnisDaten Ergebnisstand() => new SimulationErgebnisDaten
    {
        IdProjekt = 1030,
        ErgebnisGueltig = true,
        Kennzahlen = new SimulationErgebnisCtrl.UebersichtKennzahlen
        {
            WaermebedarfGesamtMwh = 420.5,
            RestwaermeMwh = 12.25,
            StrombedarfGesamtMwh = 88.0,
            ReststromMwh = 33.5,
            StromspeicherEntladungMwh = 4.75
        },
        Uebersicht = new UebersichtDaten
        {
            WaermedeckungProzent = 97.1,
            StromdeckungProzent = 61.9
        },
        Laufmeldungen = "LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ: Kollektorfeld"
    };

    /// <summary>Die fünf Laufparameter samt Schreibweg — und was der Weg mitbekommt.</summary>
    private sealed class Schreibprobe
    {
        internal ParameterDaten Stand = new ParameterDaten
        {
            Netzverluste = 3.0,
            Betriebsart = 0,
            UntersteLeistungsgrenze = 50,
            Heizstab = false,
            Bereitschaft = 4000.0
        };

        internal readonly List<string> Geschrieben = new();

        internal SimulationParameterDienste Wege() => new SimulationParameterDienste
        {
            Laden = () => Stand,
            NetzverlusteSchreiben = (wert, einheit) =>
                Geschrieben.Add("netzverluste=" + wert.ToString("0.##",
                    System.Globalization.CultureInfo.InvariantCulture) + einheit),
            BetriebsartSchreiben = wert => Geschrieben.Add("betriebsart=" + wert),
            LeistungsgrenzeSchreiben = wert => Geschrieben.Add("grenze=" + wert),
            HeizstabSchreiben = wert => Geschrieben.Add("heizstab=" + wert),
            BereitschaftSchreiben = wert => Geschrieben.Add("bereitschaft=" +
                wert.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
        };
    }

    private static SimulationKiSicht Sicht(Schreibprobe probe,
                                           SimulationKonfigDaten? konfig = null,
                                           SimulationErgebnisDaten? ergebnis = null,
                                           string schritt = "1 Konfiguration",
                                           string reiter = "")
    {
        SimulationParameterDienste wege = probe.Wege();
        return new SimulationKiSicht(() => konfig, () => probe.Stand, () => wege,
                                     () => ergebnis, () => schritt, () => reiter);
    }

    // =====================================================================
    //  1 — Was die Ansicht anmeldet
    // =====================================================================

    [Fact]
    public void Die_Ansicht_meldet_achtzehn_Felder_an()
    {
        var probe = new Schreibprobe();
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => Sicht(probe), new KiMaskenhaken());

        Assert.True(anmeldung.Angemeldet);

        IReadOnlyList<KiFeldwert> felder = KiMaskenbruecke.Lesen(KiMaskennamen.SIMULATION);
        Assert.Equal(18, felder.Count);
    }

    [Fact]
    public void Schritt_1_liefert_Kaskade_Reihenfolge_und_die_fuenf_Laufparameter()
    {
        var probe = new Schreibprobe();
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION,
            () => Sicht(probe, konfig: Konfigstand()),
            new KiMaskenhaken());

        Dictionary<string, string> werte = Werte();

        // Die KASKADE steht als eine Zeile - in der Reihenfolge, in der die
        // Simulation die Erzeuger einsetzt.
        Assert.Equal("Wärmeerzeuger: 1. BHKW 1, 2. Kessel", werte["kaskade"]);

        // Der angelegte, aber nicht aufgenommene Erzeuger steht GETRENNT davon: Er ist
        // der Grund fuer eine 0,00 in der Uebersicht (#190).
        Assert.Equal("Kollektorfeld", werte["nicht_aufgenommen"]);

        // Die fuenf Laufparameter.
        Assert.Equal("3", werte["netzverluste"]);
        Assert.Equal("0", werte["bhkw_betriebsart"]);
        Assert.Equal("50", werte["bhkw_leistungsgrenze"]);
        Assert.Equal("Nein", werte["wp_heizstab"]);
        Assert.Equal("4000", werte["kessel_bereitschaft"]);

        Assert.Equal("1 Konfiguration", werte["schritt"]);
    }

    [Fact]
    public void Schritt_3_liefert_die_Kennzahlen_des_Laufs_den_Reiter_und_die_Hinweise()
    {
        var probe = new Schreibprobe();
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION,
            () => Sicht(probe, ergebnis: Ergebnisstand(),
                        schritt: "3 Ergebnis", reiter: "Stromspeicher"),
            new KiMaskenhaken());

        Dictionary<string, string> werte = Werte();

        Assert.Equal("420,5", werte["waermebedarf"]);
        Assert.Equal("97,1", werte["waermedeckung"]);
        Assert.Equal("12,25", werte["restwaerme"]);
        Assert.Equal("88", werte["strombedarf"]);
        Assert.Equal("61,9", werte["stromdeckung"]);
        Assert.Equal("33,5", werte["reststrom"]);
        Assert.Equal("4,75", werte["speicher_entladung"]);

        Assert.Equal("3 Ergebnis", werte["schritt"]);
        Assert.Equal("Stromspeicher", werte["reiter"]);
        Assert.Contains("LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ", werte["laufhinweise"]);
    }

    [Fact]
    public void Ohne_gerechneten_Lauf_stehen_die_Kennzahlen_auf_null()
    {
        // Die Gegenprobe: Der Assistent erfindet keine Zahlen, wenn nichts gerechnet ist.
        var probe = new Schreibprobe();
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => Sicht(probe), new KiMaskenhaken());

        Dictionary<string, string> werte = Werte();

        Assert.Equal("0", werte["waermebedarf"]);
        Assert.Equal("0", werte["reststrom"]);
        Assert.Equal("", werte["laufhinweise"]);
        Assert.Equal("", werte["kaskade"]);
    }

    // =====================================================================
    //  2 — feld_setzen geht ueber den Schreibweg der Ansicht
    // =====================================================================

    [Theory]
    [InlineData("netzverluste", "7,5", "netzverluste=7.5%")]
    [InlineData("bhkw_betriebsart", "1", "betriebsart=1")]
    [InlineData("bhkw_leistungsgrenze", "30", "grenze=30")]
    [InlineData("wp_heizstab", "Ja", "heizstab=True")]
    [InlineData("kessel_bereitschaft", "6000", "bereitschaft=6000")]
    public async Task Ein_Laufparameter_wird_ueber_den_Delegaten_geschrieben(
        string feld, string wert, string erwartet)
    {
        var probe = new Schreibprobe();
        SimulationKiSicht sicht = Sicht(probe, konfig: Konfigstand());

        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiErgebnis ergebnis = await Setzen(feld, wert);

        Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
        Assert.Contains(erwartet, probe.Geschrieben);
    }

    [Fact]
    public async Task Der_gesetzte_Wert_steht_danach_auch_in_der_Anzeige()
    {
        // Der Stand, den die Sicht schreibt, IST das Objekt der Konfigurationsseite -
        // sonst meldete der Assistent „gesetzt", und das Feld zeigte die alte Zahl.
        var probe = new Schreibprobe();
        SimulationKiSicht sicht = Sicht(probe);

        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        await Setzen("netzverluste", "7,5");

        Assert.Equal(7.5, probe.Stand.Netzverluste);
    }

    [Theory]
    [InlineData("kaskade")]
    [InlineData("waermebedarf")]
    [InlineData("stromdeckung")]
    [InlineData("speicher_entladung")]
    [InlineData("laufhinweise")]
    public async Task Eine_Kennzahl_des_Laufs_ist_nicht_setzbar(string feld)
    {
        // Sie sind GERECHNET und haben keinen Setzer; die Ablehnung nennt sie beim
        // Namen, statt still nichts zu tun (Muster der Stromspeicher-Diagnose, #201).
        var probe = new Schreibprobe();
        SimulationKiSicht sicht = Sicht(probe, ergebnis: Ergebnisstand());

        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiErgebnis ergebnis = await Setzen(feld, "1");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.NotEqual("", ergebnis.Text);
        Assert.Empty(probe.Geschrieben);
    }

    [Fact]
    public void Kein_Feld_des_Laufs_traegt_einen_Setzer()
    {
        // Die Gegenprobe zur Setzbarkeit: genau FUENF der achtzehn Felder sind setzbar.
        var probe = new Schreibprobe();
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => Sicht(probe), new KiMaskenhaken());

        var setzbar = new List<string>();
        foreach (KiDialogFeld feld in KiDialoge.Katalog.Finde(KiMaskennamen.SIMULATION)!.Felder)
        {
            KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, feld.Name);
            if (zugang is not null && zugang.Setzbar) setzbar.Add(feld.Name);
        }

        Assert.Equal(new[]
        {
            "netzverluste", "bhkw_betriebsart", "bhkw_leistungsgrenze",
            "wp_heizstab", "kessel_bereitschaft"
        }, setzbar);
    }

    // =====================================================================
    //  3 — Der Maskenhaken: die Ansicht ohne Schreibwege ist schreibgeschuetzt
    // =====================================================================

    [Fact]
    public async Task Ohne_Schreibwege_lehnt_feld_setzen_benannt_ab()
    {
        var probe = new Schreibprobe();
        var sicht = new SimulationKiSicht(() => null, () => probe.Stand,
                                          () => null, () => null,
                                          () => "1 Konfiguration", () => "");

        var haken = new KiMaskenhaken { Schreibgeschuetzt = () => true };
        using var anmeldung = KiMaskenanmeldung.Fuer(KiMaskennamen.SIMULATION, () => sicht, haken);

        KiErgebnis ergebnis = await Setzen("netzverluste", "7,5");

        Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        Assert.Empty(probe.Geschrieben);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static Dictionary<string, string> Werte()
        => KiMaskenbruecke.Lesen(KiMaskennamen.SIMULATION)
                          .ToDictionary(w => w.Feld.Name, w => w.Text, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, object?> Auftrag(string feld, string wert)
        => new Dictionary<string, object?>
        {
            ["maske"] = KiMaskennamen.SIMULATION,
            ["feld"] = feld,
            ["wert"] = wert
        };

    /// <summary>Vorbereiten, freigeben, ausführen — der ganze Weg einer Feldsetzung.</summary>
    private static async Task<KiErgebnis> Setzen(string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };

        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen",
                                                     Auftrag(feld, wert));
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());

        KiVorbereitung vorbereitung =
            await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);

        if (vorbereitung.Freigabe is null) return vorbereitung.Ablehnung;

        vorbereitung.Freigabe.Erteilen();
        return await schicht.AusfuehrenAsync(geprueft.Aufruf, vorbereitung.Freigabe,
                                             CancellationToken.None);
    }
}
