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

    /// <summary>
    /// Ein Ergebnisstand MIT aktiver Speichervariante (Welle KI‑F2) — er trägt die
    /// Einstellwerte des Reiters „Stromspeicher" und schreibt jede Feldsetzung mit.
    /// </summary>
    private static SimulationErgebnisDaten Speicherstand(List<string> geschrieben)
    {
        SimulationErgebnisDaten d = Ergebnisstand();
        d.Parameter = new ParameterDaten
        {
            Speicher = new SpeicherParameterDaten
            {
                VarianteVorhanden = true,
                SoCMinProzent = 20,
                SoCMaxProzent = 90,
                KapazitaetKwh = 40,
                Kapitalzins = 4.5,
                Betriebsart = "PeakShaving",
                Betriebsarten = new[]
                {
                    new Steuerwahl("PeakShaving", "Lastspitzenkappung"),
                    new Steuerwahl("PvGreedy", "PV-Eigenverbrauch")
                },
                PreisreiheId = 4,
                Preisreihen = new[] { (4, "Börsenpreis 2025"), (9, "Festpreis Nacht") }
            }
        };
        return d;
    }

    /// <summary>Die vier Laufparameter samt Schreibweg — und was der Weg mitbekommt.</summary>
    private sealed class Schreibprobe
    {
        internal ParameterDaten Stand = new ParameterDaten
        {
            Netzverluste = 3.0,
            Betriebsart = 0,
            UntersteLeistungsgrenze = 50,
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
            // 16.09.2026 (Auftrag #299): HeizstabSchreiben ist entfallen - der Heizstab
            // gehoert der WAERMEPUMPE (Tab_Energieanlagen.Heizstab je Anlage) und ist
            // kein Laufparameter des Projekts mehr.
            BereitschaftSchreiben = wert => Geschrieben.Add("bereitschaft=" +
                wert.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
        };
    }

    private static SimulationKiSicht Sicht(Schreibprobe probe,
                                           SimulationKonfigDaten? konfig = null,
                                           SimulationErgebnisDaten? ergebnis = null,
                                           string schritt = "1 Konfiguration",
                                           string reiter = "",
                                           SimulationErgebnisDienste? speicherwege = null,
                                           SimulationKonfigDienste? konfigwege = null,
                                           Action<double>? autarkiespeicher = null)
    {
        SimulationParameterDienste wege = probe.Wege();
        return new SimulationKiSicht(() => konfig, () => probe.Stand, () => wege,
                                     () => ergebnis, () => schritt, () => reiter,
                                     () => speicherwege, () => konfigwege,
                                     autarkiespeicher);
    }

    // =====================================================================
    //  1 — Was die Ansicht anmeldet
    // =====================================================================

    /// <summary>
    /// Vierzig: Zu den siebzehn Feldern der Ablaufleiste kommen die BLÄTTER der
    /// Ansicht — der Lesepunkt aus der Fußzeile von Schritt ① und die zweiundzwanzig
    /// Einstellwerte des Reiters „Stromspeicher" von Schritt ③, darunter die Preisreihe.
    /// Sie gehen nicht auf, sie stehen auf der Ansicht; eine Maske ist, was offen ist.
    ///
    /// <para>Mit der Welle KI‑F6 kommt die SPEICHERKAPAZITÄT der Autarkierechnung auf
    /// dem Blatt „Ergebnis" dazu — das einzige echte Eingabefeld der neun
    /// Reiterblätter. Alles andere darauf schaltet ein BILD.</para>
    ///
    /// <para>Welle #458: sechsundvierzig — der Kühlschalter von Schritt ① und die
    /// fünf Felder JE ANLAGE (Anlage, Wärmequelle, konstante Quelltemperatur,
    /// WP-Priorität, Betriebsmodus).</para>
    /// </summary>
    [Fact]
    public void Die_Ansicht_meldet_sechsundvierzig_Felder_an()
    {
        var probe = new Schreibprobe();
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => Sicht(probe), new KiMaskenhaken());

        Assert.True(anmeldung.Angemeldet);

        IReadOnlyList<KiFeldwert> felder = KiMaskenbruecke.Lesen(KiMaskennamen.SIMULATION);
        Assert.Equal(46, felder.Count);
    }

    [Fact]
    public void Schritt_1_liefert_Kaskade_Reihenfolge_und_die_vier_Laufparameter()
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

        // Die vier Laufparameter (seit Auftrag #299 ohne wp_heizstab).
        Assert.Equal("3", werte["netzverluste"]);
        Assert.Equal("0", werte["bhkw_betriebsart"]);
        Assert.Equal("50", werte["bhkw_leistungsgrenze"]);
        Assert.False(werte.ContainsKey("wp_heizstab"));
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
        // Die Gegenprobe zur Setzbarkeit: Die Kaskade, der Schritt, der Reiter und die
        // Kennzahlen des Laufs sind ABGELEITET und haben keinen Setzer. Setzbar sind
        // die vier Laufparameter und - seit der Welle KI-F2 - der Lesepunkt sowie die
        // zwanzig EINGEBBAREN Felder des Reiters "Stromspeicher"; der stromgefuehrte
        // BHKW-Betrieb steht dort sichtbar, aber dauerhaft gesperrt. Seit der Welle
        // KI-F6 kommt die Speicherkapazitaet des Blatts "Ergebnis" dazu - das einzige
        // echte Eingabefeld der neun Reiterblaetter.
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
            "kessel_bereitschaft", "kuehlbetrieb", "quellanlage", "waermequelle",
            "quelltemperatur_konstant", "wp_prioritaet", "wp_betriebsmodus",
            "autarkie_speicher", "lesepunkt_davor",
            "speicher_soc_min", "speicher_soc_max", "speicher_ladeleistung",
            "speicher_kapazitaet", "speicher_ladeschwelle", "speicher_betriebsart",
            "speicher_berechnungsart", "speicher_peakziel", "speicher_peakziel_adaptiv",
            "speicher_kompatibilitaet", "speicher_laden_pv", "speicher_laden_bhkw",
            "speicher_netzentladung", "speicher_kapitalzins", "speicher_nutzungsdauer",
            "speicher_leistungspreis", "speicher_netzladeaufschlag",
            "speicher_preisquelle", "speicher_preisreihe", "speicher_aufschlag"
        }, setzbar);
    }

    // =====================================================================
    //  2b — Der Reiter „Stromspeicher" (Welle KI-F2)
    // =====================================================================

    /// <summary>
    /// <b>Die Einstellwerte des Reiters „Stromspeicher" stehen an der Brücke</b> — und
    /// ein Setzen geht denselben Weg wie das Feld auf dem Bildschirm: erst merken,
    /// dann schreiben (<c>SpeicherfeldSchreiben</c> mit dem Feldschlüssel des Blatts).
    /// </summary>
    [Fact]
    public void Die_Speicherfelder_werden_gelesen_und_ueber_ihren_Feldweg_geschrieben()
    {
        var probe = new Schreibprobe();
        var geschrieben = new List<string>();
        SimulationErgebnisDaten stand = Speicherstand(geschrieben);

        var speicherwege = new SimulationErgebnisDienste
        {
            SpeicherfeldSchreiben = (feld, wert) =>
            {
                geschrieben.Add(feld + "=" + wert);
                return new Rueckmeldung(true, "");
            }
        };

        var sicht = Sicht(probe, ergebnis: stand, speicherwege: speicherwege);
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiFeldzugang zins =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "speicher_kapitalzins");
        Assert.Equal(4.5, zins.Lesen());

        zins.Setzen(3.0);

        Assert.Equal(3.0, stand.Parameter.Speicher.Kapitalzins);
        Assert.Contains(SpeicherFeld.Kapitalzins + "=3", geschrieben);
    }

    /// <summary>
    /// <b>Ein Steuerwert, den die Klappliste der Maske nicht führt, wird abgewiesen.</b>
    /// Er stünde sonst in der Datenbank, ohne dass ihn jemand wieder auswählen könnte.
    /// </summary>
    [Fact]
    public void Ein_unbekannter_Steuerwert_der_Betriebsart_wird_abgewiesen()
    {
        var probe = new Schreibprobe();
        var geschrieben = new List<string>();
        SimulationErgebnisDaten stand = Speicherstand(geschrieben);

        var speicherwege = new SimulationErgebnisDienste
        {
            SpeicherfeldSchreiben = (feld, wert) =>
            {
                geschrieben.Add(feld + "=" + wert);
                return new Rueckmeldung(true, "");
            }
        };

        var sicht = Sicht(probe, ergebnis: stand, speicherwege: speicherwege);
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiFeldzugang art =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "speicher_betriebsart");

        art.Setzen("Arbitrage");
        Assert.Equal("PeakShaving", stand.Parameter.Speicher.Betriebsart);
        Assert.Empty(geschrieben);

        art.Setzen("PvGreedy");
        Assert.Equal("PvGreedy", stand.Parameter.Speicher.Betriebsart);
        Assert.Contains(SpeicherFeld.Betriebsart + "=PvGreedy", geschrieben);
    }

    /// <summary>
    /// <b>Ein Wahlfeld wird über den ANGEZEIGTEN TEXT gesetzt</b> (KI-F1b, KI-D-Q6):
    /// Der Anwender liest „Lastspitzenkappung", die Eigenschaft trägt den Steuerwert
    /// „PeakShaving" — und die Preisreihe eine Id.
    /// </summary>
    [Fact]
    public void Die_Wahlfelder_des_Speicherreiters_nehmen_den_Anzeigetext()
    {
        var probe = new Schreibprobe();
        var geschrieben = new List<string>();
        SimulationErgebnisDaten stand = Speicherstand(geschrieben);

        var speicherwege = new SimulationErgebnisDienste
        {
            SpeicherfeldSchreiben = (feld, wert) =>
            {
                geschrieben.Add(feld + "=" + wert);
                return new Rueckmeldung(true, "");
            }
        };

        var sicht = Sicht(probe, ergebnis: stand, speicherwege: speicherwege);
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        // Die Betriebsart über ihren Anzeigetext - gesetzt wird der Steuerwert.
        KiFeldzugang art =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "speicher_betriebsart");
        Assert.True(art.IstWahl);

        KiFeldumsetzung u = KiFeldwandler.Wandle(art, "PV-Eigenverbrauch");
        Assert.True(u.Ok, u.Grund);
        art.Setzen(u.Wert);
        Assert.Equal("PvGreedy", stand.Parameter.Speicher.Betriebsart);

        // Die Preisreihe über ihren Namen - gesetzt wird die Id.
        KiFeldzugang reihe =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "speicher_preisreihe");
        Assert.True(reihe.IstWahl);

        KiFeldumsetzung r = KiFeldwandler.Wandle(reihe, "Festpreis Nacht");
        Assert.True(r.Ok, r.Grund);
        reihe.Setzen(r.Wert);
        Assert.Equal(9, stand.Parameter.Speicher.PreisreiheId);
        Assert.Contains(SpeicherFeld.Preisreihe + "=9", geschrieben);

        // Gelesen wird der TEXT, daneben steht der Schlüssel.
        KiFeldwert gelesen = null;
        foreach (KiFeldwert w in KiMaskenbruecke.Lesen(KiMaskennamen.SIMULATION))
            if (w.Name == "speicher_preisreihe") gelesen = w;

        Assert.NotNull(gelesen);
        Assert.Equal("Festpreis Nacht", gelesen.Text);
        Assert.Equal("9", gelesen.Schluessel);
    }

    /// <summary>
    /// Ein Name, den die Reihenliste nicht führt, wird BENANNT abgelehnt — und die
    /// Absage nennt, was zur Wahl steht.
    /// </summary>
    [Fact]
    public void Eine_unbekannte_Preisreihe_wird_benannt_abgelehnt()
    {
        var probe = new Schreibprobe();
        var geschrieben = new List<string>();
        SimulationErgebnisDaten stand = Speicherstand(geschrieben);

        var sicht = Sicht(probe, ergebnis: stand);
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiFeldzugang reihe =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "speicher_preisreihe");

        KiFeldumsetzung u = KiFeldwandler.Wandle(reihe, "Mondpreis");

        Assert.False(u.Ok);
        Assert.Contains("Börsenpreis 2025 (4)", u.Grund, StringComparison.Ordinal);
        Assert.Equal(4, stand.Parameter.Speicher.PreisreiheId);
    }

    /// <summary>
    /// Der LESEPUNKT von Schritt ① zieht erst nach, wenn sein Schreibweg die Einstellung
    /// bestätigt — genau wie am Schalter der Fußzeile.
    /// </summary>
    [Fact]
    public void Der_Lesepunkt_zieht_nur_nach_einer_bestaetigten_Einstellung_nach()
    {
        var probe = new Schreibprobe();
        SimulationKonfigDaten konfig = Konfigstand();
        konfig.BoosterDavor = true;

        bool angenommen = false;
        var konfigwege = new SimulationKonfigDienste
        {
            LesepunktSchreiben = _ => angenommen
        };

        var sicht = Sicht(probe, konfig: konfig, konfigwege: konfigwege);
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiFeldzugang punkt =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "lesepunkt_davor");

        Assert.Equal(true, punkt.Lesen());

        // Die Einstellung kommt nicht an: Der Stand bleibt, wie er war.
        punkt.Setzen(false);
        Assert.True(konfig.BoosterDavor);

        angenommen = true;
        punkt.Setzen(false);
        Assert.False(konfig.BoosterDavor);
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

    // =====================================================================
    //  Das Blatt „Ergebnis" (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// <b>Die Speicherkapazität der Autarkierechnung ist lesbar und setzbar</b> — das
    /// einzige echte Eingabefeld der neun Reiterblätter. Gesetzt wird über denselben
    /// Weg wie das Feld selbst; steht das Blatt nicht, bleibt sie lesbar.
    /// </summary>
    [Fact]
    public void Die_Speicherkapazitaet_des_Ergebnisblatts_ist_lesbar_und_setzbar()
    {
        var probe = new Schreibprobe();
        SimulationErgebnisDaten stand = Ergebnisstand();
        stand.Autarkie = new AutarkieDaten { HatPv = true, SpeicherKwh = 25.0 };

        var gesetzt = new List<double>();

        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION,
            () => Sicht(probe, ergebnis: stand, autarkiespeicher: gesetzt.Add),
            new KiMaskenhaken());

        KiFeldzugang feld = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SIMULATION, "autarkie_speicher");
        Assert.NotNull(feld);
        Assert.True(feld.Setzbar);
        Assert.Equal(25.0, Convert.ToDouble(feld.Lesen(),
                                            System.Globalization.CultureInfo.InvariantCulture));

        KiFeldumsetzung wert = KiFeldwandler.Wandle(feld, "40,5");
        Assert.True(wert.Ok, wert.Grund);
        feld.Setzen(wert.Wert);

        Assert.Equal(new[] { 40.5 }, gesetzt);
    }

    /// <summary>
    /// <b>Ohne den Schreibweg bleibt sie lesbar und läuft nicht ins Leere.</b> Steht
    /// das Blatt „Ergebnis" nicht, gibt es niemanden, der die Autarkie neu rechnet;
    /// die Setzung darf dann nichts behaupten.
    /// </summary>
    [Fact]
    public void Ohne_offenes_Ergebnisblatt_bleibt_die_Kapazitaet_stehen()
    {
        var probe = new Schreibprobe();
        SimulationErgebnisDaten stand = Ergebnisstand();
        stand.Autarkie = new AutarkieDaten { HatPv = true, SpeicherKwh = 25.0 };

        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION,
            () => Sicht(probe, ergebnis: stand),
            new KiMaskenhaken());

        KiFeldzugang feld = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SIMULATION, "autarkie_speicher");
        feld.Setzen(99.0);

        Assert.Equal(25.0, stand.Autarkie.SpeicherKwh);
    }

    // =====================================================================
    //  Der Kühlschalter von Schritt ① (Welle #458)
    // =====================================================================

    /// <summary>
    /// <b>„Kühlung rechnen" geht über den Delegaten des Schalters</b> und zieht den
    /// Stand erst nach, wenn das Schreiben angekommen ist.
    /// </summary>
    [Fact]
    public void Der_Kuehlschalter_schreibt_ueber_seinen_Delegaten()
    {
        var probe = new Schreibprobe();
        var geschrieben = new List<bool>();
        SimulationParameterDienste wege = probe.Wege();
        wege.KuehlbetriebSchreiben = w => { geschrieben.Add(w); return true; };

        var sicht = new SimulationKiSicht(() => null, () => probe.Stand, () => wege,
                                          () => null, () => "", () => "");
        using var anmeldung = KiMaskenanmeldung.Fuer(
            KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        KiFeldzugang feld = KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "kuehlbetrieb");
        Assert.Equal(false, feld.Lesen());

        feld.Setzen(true);

        Assert.Equal(new[] { true }, geschrieben);
        Assert.True(probe.Stand.Kuehlbetrieb);
    }

    /// <summary>
    /// Die GEGENPROBEN: Scheitert das Schreiben oder fehlt der Weg, bleibt der Stand
    /// stehen, und die Setzung sagt benannt, warum.
    /// </summary>
    [Fact]
    public void Ein_gescheiterter_oder_fehlender_Kuehlweg_wird_benannt()
    {
        var probe = new Schreibprobe();
        SimulationParameterDienste wege = probe.Wege();
        wege.KuehlbetriebSchreiben = _ => false;

        var sicht = new SimulationKiSicht(() => null, () => probe.Stand, () => wege,
                                          () => null, () => "", () => "");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => sicht.Kuehlbetrieb = true);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_MSG_KUEHLBETRIEB_FEHLER, ex.Message);
        Assert.False(probe.Stand.Kuehlbetrieb);

        wege.KuehlbetriebSchreiben = null;
        ex = Assert.Throws<InvalidOperationException>(() => sicht.Kuehlbetrieb = true);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KI_SIM_KEIN_SCHREIBWEG, ex.Message);
    }
}
