using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// PufferSpProjektDialog (iU9-W10a.4) - der Ersatz fuer Form_PufferSp_Projekt, die
/// groesste Maske der Welle.
///
/// <para>Die Datenseite ist ein Pruefstand: sechzehn Delegaten, die mitschreiben,
/// womit sie gerufen wurden. Damit laesst sich pruefen, WAS der Dialog speichern
/// will, ohne dass eine Datenbank in der Naehe ist.</para>
/// </summary>
public class PufferSpProjektDialogTests : BunitContext
{
    public PufferSpProjektDialogTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ============================================================ Pruefstand

    /// <summary>Ein Delegatensatz, der mitschreibt statt zu schreiben.</summary>
    private sealed class Pruefstand
    {
        internal List<PspPufferstand> Bestand { get; } = new();
        internal List<PspKatalogzeile> Katalog { get; } = new();
        internal List<string> Referenzen { get; } = new();
        internal (int? V, int? R) Systemvorgaben { get; set; } = (70, 50);
        internal bool Leitspeicher { get; set; }
        internal string? Temperaturfehler { get; set; }
        internal string? KlemmText { get; set; }
        internal int AnlegenErgebnis { get; set; } = 42;
        internal bool AendernErgebnis { get; set; } = true;
        internal bool EntfernenErgebnis { get; set; } = true;

        internal PspEingaben? Angelegt;
        internal PspEingaben? Geaendert;
        internal int GeaendertId;
        internal int Entfernt;

        internal PufferSpProjektDienste Dienste() => new(
            Katalogzeilen: () => Katalog,
            Projektliste: () => Bestand.Select(p => new PspProjektzeile(p.Id, p.Bezeichner)).ToList(),
            PufferLesen: id => Bestand.FirstOrDefault(p => p.Id == id),
            Systemvorgaben: () => Systemvorgaben,
            Ladereihenfolge: _ => new[] { new PspLadezeile("1.", "WP 1", "Wärmepumpe", "Hauptsenke", "3", "80 %") },
            Automatiktext: _ => "automatisch: 5",
            Entladeposition: (_, h, b, _) => h && b ? "Heizung 1. von 2\nBrauchwasser 1. von 1" : "1. von 2",
            KlassenSetAnzeige: (h, b, p) =>
                string.Join("+", new[] { h ? "H" : null, b ? "B" : null, p ? "P" : null }.Where(x => x is not null)),
            IstLeitspeicher: _ => Leitspeicher,
            Referenzen: _ => Referenzen,
            TemperaturenPruefen: (_, _) => Temperaturfehler,
            Anlegen: e => { Angelegt = e; return AnlegenErgebnis; },
            Aendern: (id, e) => { GeaendertId = id; Geaendert = e; return AendernErgebnis; },
            Entfernen: id => { Entfernt = id; return EntfernenErgebnis; },
            Klemmhinweis: (_, _) => KlemmText,
            Kapazitaet: (v, dt) => v * 1.16 * dt / 1000.0);
    }

    private static PspPufferstand Speicher(int id, string name, bool h = true, bool b = false,
                                           bool p = false, int vorlauf = 70, int ruecklauf = 50,
                                           int schichten = 1)
        => new(id, name, 800, 1.5, vorlauf, ruecklauf, 10, 95, 95, 10, 0, h, b, p,
               new PspSchichtdaten(schichten));

    private IRenderedComponent<PufferSpProjektDialog> Zeige(
        Pruefstand stand, string? verwendung = null, int idPuffer = 0,
        Action<int>? geschlossen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        IReadOnlyList<int>? passt = null)
    {
        return Render<PufferSpProjektDialog>(p =>
        {
            p.Add(x => x.IdProjekt, 1030);
            p.Add(x => x.Verwendung, verwendung);
            p.Add(x => x.IdPuffer, idPuffer);
            p.Add(x => x.Dienste, stand.Dienste());
            if (passt is not null) p.Add(x => x.PasstZurVerwendung, passt);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
            if (verwaltung is not null) p.Add(x => x.VerwaltungGaben, verwaltung);
        });
    }

    private static Pruefstand MitZwei()
    {
        var s = new Pruefstand();
        s.Bestand.Add(Speicher(11, "Heizungsspeicher"));
        s.Bestand.Add(Speicher(12, "Brauchwasserspeicher", h: false, b: true));
        s.Katalog.Add(new PspKatalogzeile(5, "Vitocell 600", 600, 2.5));
        return s;
    }

    // ============================================================ Vorwahlregel

    /// <summary>
    /// SetControls:1089-1103 - die Reihenfolge Id, Verwendung, erster, Neuanlage. Die
    /// ID hat VORRANG: Das Bleistiftsymbol einer Speicherkarte meint GENAU diesen
    /// Speicher, und bei zwei Heizungsspeichern landete der Anwender sonst regelmaessig
    /// im falschen.
    /// </summary>
    [Fact]
    public void Die_Id_hat_Vorrang_vor_allem_anderen()
    {
        Assert.Equal(12, Zeige(MitZwei(), verwendung: "Heizung", idPuffer: 12,
                               passt: new[] { 11 }).Instance.BearbeiteteId);
    }

    [Fact]
    public void Ohne_Id_entscheidet_die_Verwendung()
    {
        Assert.Equal(12, Zeige(MitZwei(), verwendung: "Brauchwasser",
                               passt: new[] { 12 }).Instance.BearbeiteteId);
    }

    [Fact]
    public void Ohne_Wunsch_steht_der_erste_Speicher()
    {
        Assert.Equal(11, Zeige(MitZwei()).Instance.BearbeiteteId);
    }

    /// <summary>
    /// Kein passender Speicher heisst NEUANLAGE - genau der Schritt, den der Absprung
    /// aus dem Senkendialog ersparen soll.
    /// </summary>
    [Fact]
    public void Ohne_passenden_Speicher_geht_es_in_die_Neuanlage()
    {
        Assert.Equal(0, Zeige(MitZwei(), verwendung: "Prozess",
                              passt: Array.Empty<int>()).Instance.BearbeiteteId);
        Assert.Equal(0, Zeige(new Pruefstand()).Instance.BearbeiteteId);
    }

    // ============================================================ Neu-Vorbelegung

    /// <summary>
    /// NeuVorbereiten:1178-1239 woertlich: Verluste 0, Vorlauf/Ruecklauf aus den
    /// Systemvorgaben, die vier Schwellen auf ihren Vorgaben, Prioritaet automatisch.
    /// </summary>
    [Fact]
    public void Die_Neuanlage_ist_woertlich_vorbelegt()
    {
        var cut = Zeige(new Pruefstand());
        var d = cut.Instance;

        Assert.Equal(0, d.BearbeiteteId);
        Assert.Equal("", d.Bezeichner);
        Assert.Null(d.Volumen);
        Assert.Equal(0.0, d.Verluste);
        Assert.Equal(10.0, d.Schwellen.Ein);
        Assert.Equal(95.0, d.Schwellen.Aus);
        Assert.Equal(95.0, d.Schwellen.Nachrang);
        Assert.Equal(10.0, d.Schwellen.Reserve);
        Assert.Equal(new[] { 0 }, d.KlassenSet);          // Vorbelegung Heizung
    }

    /// <summary>
    /// Fehlen die Systemvorgaben, bleiben Vorlauf und Ruecklauf LEER - eine erfundene
    /// Vorbelegung waere bei einem Niedertemperatursystem falsch. Ohne Temperaturpaar
    /// steht auch keine Kapazitaetszeile da (QmaxAnzeigen:1374).
    /// </summary>
    [Fact]
    public void Ohne_Systemvorgaben_bleiben_die_Temperaturen_leer()
    {
        var stand = new Pruefstand { Systemvorgaben = (null, null) };
        Assert.Equal("", Zeige(stand).Instance.Qmax);
    }

    // ============================================================ Katalog

    /// <summary>
    /// cbKatalog_SelectedIndexChanged:1566-1591 - die Katalogzeile fuellt DREI Felder.
    /// </summary>
    [Fact]
    public void Die_Katalogwahl_fuellt_drei_Felder()
    {
        var cut = Zeige(new Pruefstand
        {
            Katalog = { new PspKatalogzeile(5, "Vitocell 600", 600, 2.5) }
        });

        cut.FindAll("select")[0].Change("0");        // erste Katalogzeile

        Assert.Equal("Vitocell 600", cut.Instance.Bezeichner);
        Assert.Equal(600, cut.Instance.Volumen);
        Assert.Equal(2.5, cut.Instance.Verluste);
    }

    /// <summary>Ein BESTEHENDER Speicher wird nicht neu uebernommen — die Liste ist gesperrt.</summary>
    [Fact]
    public void Beim_bestehenden_Speicher_ist_die_Katalogliste_gesperrt()
    {
        var cut = Zeige(MitZwei());
        Assert.True(cut.FindAll("select")[0].HasAttribute("disabled"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Neuer")).Click();
        Assert.False(cut.FindAll("select")[0].HasAttribute("disabled"));
    }

    // ============================================================ Kapazität

    [Fact]
    public void Die_Kapazitaet_folgt_Volumen_und_Temperaturpaar()
    {
        var cut = Zeige(MitZwei());          // 800 l, 70/50 -> 800*1,16*20/1000 = 18,56
        Assert.Contains("18,6", cut.Instance.Qmax);
    }

    /// <summary>
    /// LEER, sobald das Volumen nicht positiv ist oder der Vorlauf den Ruecklauf nicht
    /// uebersteigt (QmaxAnzeigen:1374) - woertlich.
    /// </summary>
    [Fact]
    public void Die_Kapazitaet_bleibt_bei_unsinnigem_Paar_leer()
    {
        var stand = new Pruefstand();
        stand.Bestand.Add(Speicher(11, "Verdreht", vorlauf: 40, ruecklauf: 60));
        Assert.Equal("", Zeige(stand).Instance.Qmax);
    }

    // ============================================================ Klassen-Set

    /// <summary>
    /// Das Klassen-Set ist die FUEHRENDE Wahrheit; die Herleitungszeile nennt den
    /// abgeleiteten Altwert.
    /// </summary>
    [Fact]
    public void Das_Klassenset_steht_in_der_Mehrfachauswahl()
    {
        var cut = Zeige(MitZwei());
        Assert.Equal(new[] { 0 }, cut.Instance.KlassenSet);
        Assert.Contains("H", cut.Markup);
    }

    /// <summary>
    /// Das LEERE Set wird beim Klicken NICHT abgefangen - wer von {H} auf {B}
    /// umstellt, muss zwischendurch durch das leere Set gehen. Gemeldet wird erst beim
    /// Uebernehmen (Kommentar KlassenSet_Geaendert).
    /// </summary>
    [Fact]
    public void Das_leere_Set_meldet_erst_beim_Uebernehmen()
    {
        var stand = MitZwei();
        var cut = Zeige(stand);

        cut.FindAll("input[type=checkbox]")[0].Change(false);
        Assert.Empty(cut.Instance.KlassenSet);
        Assert.Equal("", cut.Instance.Meldung);

        cut.FindAll("button").First(b => b.TextContent.Contains("Übernehmen")).Click();
        Assert.Contains("Mindestens eine Nutzung", cut.Instance.Meldung);
        Assert.Null(stand.Geaendert);
    }

    // ============================================================ Schichtung

    /// <summary>
    /// SchichtSichtbarkeitSetzen:867-931 als reine SICHTBARKEITSREGEL: Erst ab zwei
    /// Schichten haben Schichthoehe, Waermeleitwert und Nutztemperatur eine Bedeutung.
    /// </summary>
    [Fact]
    public void Die_Schichtfelder_erscheinen_erst_ab_zwei_Schichten()
    {
        var stand = new Pruefstand();
        stand.Bestand.Add(Speicher(11, "Einzonig", schichten: 1));
        var cut = Zeige(stand);

        Assert.False(cut.Instance.Erweitert);
        int vorher = cut.FindAll("input.epos-eingabe").Count;

        // Das Schichtenfeld ist das erste Ganzzahlfeld der Schichtgruppe.
        cut.FindAll("input.epos-eingabe").First(f => f.GetAttribute("value") == "1").Input("3");

        Assert.True(cut.Instance.Erweitert);
        Assert.True(cut.FindAll("input.epos-eingabe").Count > vorher);
    }

    // ============================================================ Prüfkette

    [Fact]
    public void Die_Pruefkette_meldet_woertlich_und_in_ihrer_Reihenfolge()
    {
        // 1 - Bezeichner fehlt
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        Uebernehmen(cut);
        Assert.Contains("Bezeichner eintragen", cut.Instance.Meldung);

        // 3 - Volumen
        cut = Zeige(new Pruefstand());
        Bezeichner(cut, "Neu");
        Uebernehmen(cut);
        Assert.Contains("Gesamtvolumen in Litern", cut.Instance.Meldung);

        // 7 - Einschaltschwelle nicht unter der Abschaltschwelle
        stand = MitZwei();
        cut = Zeige(stand);
        Schwelle(cut, 0, 96);
        Uebernehmen(cut);
        Assert.Contains("Einschaltschwelle muss kleiner", cut.Instance.Meldung);

        // 8 - nachrangige Schwelle ueber der Abschaltschwelle
        cut = Zeige(MitZwei());
        Schwelle(cut, 2, 99);
        Uebernehmen(cut);
        Assert.Contains("darf die Abschaltschwelle nicht", cut.Instance.Meldung);

        // 9 - nachrangige Schwelle unter der Einschaltschwelle
        cut = Zeige(MitZwei());
        Schwelle(cut, 2, 5);
        Uebernehmen(cut);
        Assert.Contains("muss über der Einschaltschwelle", cut.Instance.Meldung);

        // 11 - Mindestfuellstand auf oder ueber der Abschaltschwelle
        cut = Zeige(MitZwei());
        Schwelle(cut, 3, 96);
        Uebernehmen(cut);
        Assert.Contains("Mindestfüllstand muss unter", cut.Instance.Meldung);
    }

    /// <summary>
    /// Der Mindestfuellstand darf ausdruecklich 0 sein („dieser Speicher darf
    /// leergefahren werden"), die drei Schaltschwellen nicht.
    /// </summary>
    [Fact]
    public void Nur_der_Mindestfuellstand_darf_null_sein()
    {
        var stand = MitZwei();
        var cut = Zeige(stand);
        Schwelle(cut, 3, 0);
        Uebernehmen(cut);
        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);

        cut = Zeige(MitZwei());
        Schwelle(cut, 0, 0);
        Uebernehmen(cut);
        Assert.Contains("Einschaltschwelle", cut.Instance.Meldung);
    }

    /// <summary>Der Fehlertext des Temperaturpaars kommt aus dem Kern, nicht aus dem Dialog.</summary>
    [Fact]
    public void Das_Temperaturpaar_prueft_der_Kern()
    {
        var stand = MitZwei();
        stand.Temperaturfehler = "Vorlauf muss über Rücklauf liegen.";
        var cut = Zeige(stand);

        Uebernehmen(cut);

        Assert.Equal("Vorlauf muss über Rücklauf liegen.", cut.Instance.Meldung);
        Assert.Null(stand.Geaendert);
    }

    // ============================================================ Kriterium W6

    /// <summary>
    /// Kriterium W6 ist HART: Ein Leitspeicher eines Parallelverbunds kann keine
    /// Schichtung fuehren - abgewiesen, nicht gewarnt (Entscheidung F8).
    /// </summary>
    [Fact]
    public void Schichtung_am_Verbund_wird_abgewiesen()
    {
        var stand = MitZwei();
        stand.Leitspeicher = true;
        stand.Bestand[0] = Speicher(11, "Leitspeicher", schichten: 4);

        var cut = Zeige(stand);
        Uebernehmen(cut);

        Assert.Contains("Leitspeicher eines Parallelverbunds", cut.Instance.Meldung);
        Assert.Equal(WarnStufe.Fehler, cut.Instance.MeldungStufe);
        Assert.Null(stand.Geaendert);
    }

    // ============================================================ Speichern

    [Fact]
    public void Anlegen_liefert_die_neue_Id_und_meldet_Erfolg()
    {
        var stand = new Pruefstand { AnlegenErgebnis = 77 };
        var cut = Zeige(stand);

        Bezeichner(cut, "Neuer Speicher");
        Volumen(cut, 900);
        Uebernehmen(cut);

        Assert.NotNull(stand.Angelegt);
        Assert.Equal("Neuer Speicher", stand.Angelegt!.Bezeichner);
        Assert.Equal(900, stand.Angelegt.Volumen);
        Assert.True(stand.Angelegt.Heizung);
        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
        Assert.Equal(77, cut.Instance.Ergebnis);
    }

    [Fact]
    public void Ein_gescheitertes_Anlegen_meldet_und_liefert_nichts()
    {
        var stand = new Pruefstand { AnlegenErgebnis = 0 };
        var cut = Zeige(stand);

        Bezeichner(cut, "Neu");
        Volumen(cut, 500);
        Uebernehmen(cut);

        Assert.Contains("nicht angelegt", cut.Instance.Meldung);
        Assert.NotEqual(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
        Assert.Equal(0, cut.Instance.Ergebnis);
    }

    /// <summary>
    /// Kriterium W4 ist WEICH und kommt NACH dem Speichern: Gespeichert wird trotzdem,
    /// der Anwender erfaehrt nur, was der Lauf mit der Angabe macht.
    /// </summary>
    [Fact]
    public void Der_Klemmhinweis_kommt_nach_dem_Speichern()
    {
        var stand = MitZwei();
        stand.KlemmText = "Die Nutztemperatur wird geklemmt.";
        var cut = Zeige(stand);

        Uebernehmen(cut);

        Assert.NotNull(stand.Geaendert);                       // gespeichert wurde
        Assert.Contains("geklemmt", cut.Instance.Meldung);     // und dann gemeldet
    }

    // ============================================================ Rückfragen

    /// <summary>
    /// Der Nutzungswechsel wird nur zurueckgefragt, wenn der Speicher REFERENZIERT ist
    /// (Befund W10-B32) - und verglichen werden die drei Flags, nicht der abgeleitete
    /// Altwert.
    /// </summary>
    [Fact]
    public void Der_Nutzungswechsel_fragt_nur_mit_Referenzen_zurueck()
    {
        // ohne Referenzen: geht still durch
        var stand = MitZwei();
        var cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);    // Prozess dazu
        Uebernehmen(cut);
        Assert.False(cut.Instance.FrageSteht);
        Assert.NotNull(stand.Geaendert);

        // mit Referenzen: Rueckfrage
        stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Uebernehmen(cut);
        Assert.True(cut.Instance.FrageSteht);
        Assert.Contains("WP 1", cut.Instance.Fragetext);
        Assert.Null(stand.Geaendert);
    }

    [Fact]
    public void Ja_auf_die_Nutzungsfrage_speichert_Nein_nicht()
    {
        var stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        var cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Uebernehmen(cut);
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent == "Ja").Click();
        Assert.NotNull(stand.Geaendert);

        stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Uebernehmen(cut);
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent == "Nein").Click();
        Assert.Null(stand.Geaendert);
    }

    /// <summary>
    /// Entfernen wird BLOCKIERT, solange eine Anlage den Speicher referenziert - dann
    /// gibt es gar keine Rueckfrage, sondern eine Meldung mit der Aufzaehlung.
    /// </summary>
    [Fact]
    public void Referenzen_blockieren_das_Entfernen()
    {
        var stand = MitZwei();
        stand.Referenzen.Add("BHKW 1");
        var cut = Zeige(stand);

        cut.FindAll("button").First(b => b.TextContent.Contains("Entfernen")).Click();

        Assert.False(cut.Instance.FrageSteht);
        Assert.Contains("BHKW 1", cut.Instance.Meldung);
        Assert.Equal(0, stand.Entfernt);
    }

    [Fact]
    public void Ohne_Referenzen_fragt_das_Entfernen_zurueck()
    {
        var stand = MitZwei();
        var cut = Zeige(stand);

        cut.FindAll("button").First(b => b.TextContent.Contains("Entfernen")).Click();
        Assert.True(cut.Instance.FrageSteht);
        Assert.Contains("Heizungsspeicher", cut.Instance.Fragetext);

        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent == "Ja").Click();
        Assert.Equal(11, stand.Entfernt);
        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
    }

    // ============================================================ Sprung / Schluss

    /// <summary>
    /// Ohne Parametersatz der Verwaltung fehlt der Katalogknopf — Hausregel. Seit
    /// iU9-W14a.4 ist der Auslieferungskatalog eine ÜBERLAGERUNG im selben Fenster
    /// (mit <c>NurLesen</c>), nicht mehr ein Sprung über
    /// <c>Sprungziel.PufferSpAdminNurLesen</c>.
    /// </summary>
    [Fact]
    public void Ohne_Verwaltungsgaben_gibt_es_keinen_Katalogknopf()
    {
        var ohne = Zeige(MitZwei());
        Assert.DoesNotContain(ohne.FindAll("button"), b => b.TextContent.Contains("Katalog"));

        var mit = Zeige(MitZwei(), verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button"), b => b.TextContent.Contains("Katalog"));
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — NUR ZUM ANSEHEN.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.KatalogBrowserArt.Pufferspeicher,
            ["NurLesen"] = true,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.KatalogBrowserWege()
        };

    /// <summary>
    /// Der Dialog hat KEIN Abbrechen (Befund W10-B29) - nur "Schliessen", und das
    /// liefert den zuletzt angelegten oder gewaehlten Speicher.
    /// </summary>
    [Fact]
    public void Es_gibt_nur_Schliessen_und_es_liefert_die_Id()
    {
        int? ergebnis = null;
        var cut = Zeige(MitZwei(), geschlossen: id => ergebnis = id);

        Assert.DoesNotContain(cut.FindAll(".epos-leiste button"),
                              b => b.TextContent == "Abbrechen");

        cut.Find(".epos-leiste button.epos-knopf--primaer").Click();
        Assert.Equal(11, ergebnis);
    }

    /// <summary>Esc schliesst und nimmt NICHTS zurueck — der Dialog hat laengst geschrieben.</summary>
    [Fact]
    public void Esc_schliesst_ohne_Ruecknahme()
    {
        int? ergebnis = null;
        var cut = Zeige(MitZwei(), geschlossen: id => ergebnis = id);

        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Equal(11, ergebnis);
    }

    // ============================================================ Hilfsgriffe

    private static void Uebernehmen(IRenderedComponent<PufferSpProjektDialog> cut)
        => cut.FindAll("button")
              .First(b => b.TextContent.Contains("Übernehmen") || b.TextContent.Contains("Anlegen"))
              .Click();

    private static void Bezeichner(IRenderedComponent<PufferSpProjektDialog> cut, string wert)
        => cut.Find("input.epos-eingabe[type=text]").Input(wert);

    private static void Volumen(IRenderedComponent<PufferSpProjektDialog> cut, int wert)
        => cut.FindAll("input.epos-eingabe")[1].Input(wert.ToString());

    /// <summary>Setzt eine der vier Schwellen (0 = ein, 1 = aus, 2 = nachrangig, 3 = Reserve).</summary>
    private static void Schwelle(IRenderedComponent<PufferSpProjektDialog> cut, int nummer, double wert)
    {
        // Reihenfolge der Zahlenfelder: Verluste, Vorlauf, Ruecklauf, dann die vier
        // Schwellen (das Volumen ist ein Ganzzahlfeld und steht davor).
        var felder = cut.FindAll("input.epos-eingabe");
        felder[5 + nummer].Input(wert.ToString(CultureInfo.GetCultureInfo("de-DE")));
    }
}
