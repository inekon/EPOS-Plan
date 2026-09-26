using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// PufferSpProjektDialog (iU9-W10a.4) - der Ersatz fuer Form_PufferSp_Projekt, die
/// groesste Maske der Welle.
///
/// <para>Die Datenseite ist ein Pruefstand: siebzehn Delegaten, die mitschreiben,
/// womit sie gerufen wurden. Damit laesst sich pruefen, WAS der Dialog speichern
/// will, ohne dass eine Datenbank in der Naehe ist.</para>
///
/// <para><b>Der Kern dieser Faelle ist der DATENBANKSTAND, nicht der Dialogzustand.</b>
/// Geschrieben wird allein im OK-Weg; <c>Schreibzugriffe</c> zaehlt jeden Aufruf von
/// Anlegen, Aendern und Entfernen. Nach Abbrechen, Esc und dem Kreuz muss er auf 0
/// stehen - egal, was im Dialog vorher geschah.</para>
/// </summary>
public class PufferSpProjektDialogTests : EposBunitContext
{
    public PufferSpProjektDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ============================================================ Pruefstand

    /// <summary>
    /// Ein Delegatensatz, der mitschreibt statt zu schreiben. Er ist <c>internal</c>,
    /// weil ihn auch die drei EINBETTUNGSSTELLEN brauchen: Nur mit ihm laesst sich dort
    /// pruefen, dass der Wirt nicht mehr auf einem fruehen Schreiben sitzt.
    /// </summary>
    internal sealed class Pruefstand
    {
        internal List<PspPufferstand> Bestand { get; } = new();
        internal List<PspKatalogzeile> Katalog { get; } = new();
        internal List<string> Referenzen { get; } = new();
        internal (int? V, int? R) Systemvorgaben { get; set; } = (70, 50);
        internal bool Leitspeicher { get; set; }
        internal string? Temperaturfehler { get; set; }
        internal string? KlemmText { get; set; }

        /// <summary>Die Zeile „leer = Automatik …"; <c>null</c> = die Hülle liefert keine.</summary>
        internal Func<int, double, string>? Automatik { get; set; }
        internal int AnlegenErgebnis { get; set; } = 42;
        internal bool AendernErgebnis { get; set; } = true;
        internal bool EntfernenErgebnis { get; set; } = true;

        internal PspEingaben? Angelegt;
        internal PspEingaben? Geaendert;
        internal int GeaendertId;
        internal int Entfernt;

        /// <summary>Jeder Aufruf von Anlegen, Aendern oder Entfernen - der Datenbankstand.</summary>
        internal int Schreibzugriffe;

        /// <summary>Die Schreibzugriffe in ihrer Reihenfolge, als Text.</summary>
        internal List<string> Schreibfolge { get; } = new();

        internal PufferSpProjektDienste Dienste() => new(
            Katalogzeilen: () => Katalog,
            Projektliste: () => Bestand.Select(p => new PspProjektzeile(p.Id, p.Bezeichner)).ToList(),
            Listentext: e => e.Bezeichner,
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
            Anlegen: e =>
            {
                Angelegt = e; Schreibzugriffe++; Schreibfolge.Add("anlegen " + e.Bezeichner);
                return AnlegenErgebnis;
            },
            Aendern: (id, e) =>
            {
                GeaendertId = id; Geaendert = e; Schreibzugriffe++;
                Schreibfolge.Add("aendern " + id);
                return AendernErgebnis;
            },
            Entfernen: id =>
            {
                Entfernt = id; Schreibzugriffe++; Schreibfolge.Add("entfernen " + id);
                return EntfernenErgebnis;
            },
            Klemmhinweis: (_, _) => KlemmText,
            Kapazitaet: (v, dt) => v * 1.16 * dt / 1000.0,
            NachrangAutomatik: Automatik);
    }

    internal static PspPufferstand Speicher(int id, string name, bool h = true, bool b = false,
                                           bool p = false, int vorlauf = 70, int ruecklauf = 50,
                                           int schichten = 1, double? nachrang = 95)
        => new(id, name, 800, 1.5, vorlauf, ruecklauf, 10, 95, nachrang, 10, 0, h, b, p,
               new PspSchichtdaten(schichten));

    private IRenderedComponent<PufferSpProjektDialog> Zeige(
        Pruefstand stand, string? verwendung = null, int idPuffer = 0,
        Action<int>? geschlossen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        IReadOnlyList<int>? passt = null,
        bool titelAnzeigen = true)
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
            p.Add(x => x.TitelAnzeigen, titelAnzeigen);
        });
    }

    internal static Pruefstand MitZwei()
    {
        var s = new Pruefstand();
        s.Bestand.Add(Speicher(11, "Heizungsspeicher"));
        s.Bestand.Add(Speicher(12, "Brauchwasserspeicher", h: false, b: true));
        s.Katalog.Add(new PspKatalogzeile(5, "Vitocell 600", 600, 2.5));
        return s;
    }

    /// <summary>
    /// Der Parametersatz, den die drei Einbettungsstellen splatten — so klein wie
    /// moeglich, aber mit der Datenseite: ohne sie schriebe der eingebettete Dialog
    /// nirgendwohin, und genau das ist dort die Frage.
    /// </summary>
    internal static IReadOnlyDictionary<string, object> Gaben(Pruefstand stand)
        => new Dictionary<string, object>
        {
            ["IdProjekt"] = 1030,
            ["Dienste"] = stand.Dienste()
        };

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
        Assert.Null(d.Schwellen.Nachrang);                // leer = Automatik, keine 95 %
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

    // ============================================================ Nachrang: leer = Automatik

    /// <summary>
    /// Eine Neuanlage mit LEERER nachrangiger Schwelle geht als <c>null</c> an Anlegen —
    /// die Hülle schreibt daraus NULL, und im Lauf gilt die Automatik (30 % bei
    /// Solarthermie am Puffer).
    /// </summary>
    [Fact]
    public void Ein_leeres_Nachrangfeld_wird_als_null_angelegt()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);

        Bezeichner(cut, "Solarpuffer");
        Volumen(cut, 3000);
        Uebernehmen(cut);
        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
        Ok(cut);

        Assert.NotNull(stand.Angelegt);
        Assert.Null(stand.Angelegt!.SchwelleNachrang);
        Assert.Equal(95.0, stand.Angelegt.SchwelleAus);
    }

    /// <summary>
    /// Ein Speicher ohne gepflegte Nachrangschwelle öffnet mit LEEREM Feld, und eine
    /// Änderung an einem anderen Feld schreibt sie nicht als Wert zurück.
    /// </summary>
    [Fact]
    public void Ein_ungepflegter_Speicher_bleibt_beim_Aendern_leer()
    {
        var stand = new Pruefstand();
        stand.Bestand.Add(Speicher(11, "Heizungsspeicher", nachrang: null));
        var cut = Zeige(stand);

        Assert.Null(cut.Instance.Schwellen.Nachrang);

        Volumen(cut, 1200);
        Ok(cut);

        Assert.Equal(11, stand.GeaendertId);
        Assert.Equal(1200, stand.Geaendert!.Volumen);
        Assert.Null(stand.Geaendert.SchwelleNachrang);
    }

    /// <summary>Ein gepflegter Wert bleibt maßgeblich — auch der alte Vorbelegungswert 95.</summary>
    [Fact]
    public void Ein_gepflegter_Nachrangwert_bleibt()
    {
        var stand = MitZwei();                            // beide mit gepflegten 95 %
        var cut = Zeige(stand);

        Assert.Equal(95.0, cut.Instance.Schwellen.Nachrang);
        Volumen(cut, 1200);
        Ok(cut);
        Assert.Equal(95.0, stand.Geaendert!.SchwelleNachrang);

        stand = MitZwei();
        cut = Zeige(stand);
        Schwelle(cut, 2, 40);
        Ok(cut);
        Assert.Equal(40.0, stand.Geaendert!.SchwelleNachrang);
    }

    /// <summary>
    /// Wer das Feld LEERT, stellt auf Automatik: geschrieben wird <c>null</c>, und die
    /// Prüfkette (über Abschalt-, unter Einschaltschwelle) gilt für ein leeres Feld nicht.
    /// </summary>
    [Fact]
    public void Ein_geleertes_Nachrangfeld_wird_als_null_geschrieben()
    {
        var stand = MitZwei();
        var cut = Zeige(stand);

        cut.FindAll("input.epos-eingabe")[5 + 2].Input("");
        Assert.Null(cut.Instance.Schwellen.Nachrang);
        Ok(cut);

        Assert.Equal(11, stand.GeaendertId);
        Assert.Null(stand.Geaendert!.SchwelleNachrang);
    }

    /// <summary>
    /// Neben dem Feld steht, was LEER bedeutet — Wert und Grund aus der Hülle, gerechnet
    /// mit der Abschaltschwelle im Feld; sie folgt deren Eingabe. Ohne Delegat keine Zeile.
    /// </summary>
    [Fact]
    public void Neben_dem_Feld_steht_die_Automatik()
    {
        var stand = MitZwei();
        stand.Automatik = (id, aus) => id == 11
            ? "leer = Automatik: 30 % (Solarthermie am Puffer)"
            : $"leer = Automatik: {aus:0.#} % (= Abschaltschwelle)";
        var cut = Zeige(stand, idPuffer: 11);

        Assert.Equal("leer = Automatik: 30 % (Solarthermie am Puffer)", cut.Instance.NachrangAutomatik);
        Assert.Contains("leer = Automatik: 30 % (Solarthermie am Puffer)", cut.Markup);

        cut = Zeige(stand, idPuffer: 12);
        Schwelle(cut, 1, 90);
        Assert.Equal("leer = Automatik: 90 % (= Abschaltschwelle)", cut.Instance.NachrangAutomatik);

        cut = Zeige(MitZwei());
        Assert.Equal("", cut.Instance.NachrangAutomatik);
        Assert.DoesNotContain("leer = Automatik", cut.Markup);
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

    // ============================================================ Arbeitsstand

    /// <summary>
    /// „Anlegen" schreibt NICHT - es legt die geprueften Felder als vorlaeufige Zeile in
    /// die Liste. Ihre Nummer ist negativ; die echte Id entsteht erst im OK-Weg, und
    /// genau sie bekommt der Wirt danach.
    /// </summary>
    [Fact]
    public void Anlegen_legt_eine_vorlaeufige_Zeile_an_die_Id_kommt_erst_beim_OK()
    {
        int? ergebnis = null;
        var stand = new Pruefstand { AnlegenErgebnis = 77 };
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Bezeichner(cut, "Neuer Speicher");
        Volumen(cut, 900);
        Uebernehmen(cut);

        Assert.Equal(0, stand.Schreibzugriffe);                 // noch nichts in der Datenbank
        Assert.Single(cut.Instance.Bestandszeilen);
        Assert.True(cut.Instance.Bestandszeilen[0] < 0);        // vorlaeufig
        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
        Assert.Null(ergebnis);

        Ok(cut);

        Assert.NotNull(stand.Angelegt);
        Assert.Equal("Neuer Speicher", stand.Angelegt!.Bezeichner);
        Assert.Equal(900, stand.Angelegt.Volumen);
        Assert.True(stand.Angelegt.Heizung);
        Assert.Equal(77, ergebnis);
        Assert.Equal(77, cut.Instance.Ergebnis);
    }

    /// <summary>
    /// Hausmuster der <c>SpeichernLeiste</c>: Der Vermerk von „Anlegen"/„Übernehmen" steht
    /// auch in der Statusspanne der Leiste, dort, wo geklickt wurde; die nächste Eingabe
    /// nimmt ihn zurück.
    /// </summary>
    [Fact]
    public void Uebernehmen_meldet_sich_in_der_Leiste_bis_zur_naechsten_Eingabe()
    {
        var cut = Zeige(new Pruefstand());
        AngleSharp.Dom.IElement Status() => cut.FindAll(".epos-leiste .epos-status")[^1];

        Bezeichner(cut, "Neuer Speicher");
        Volumen(cut, 900);
        Uebernehmen(cut);

        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
        Assert.Equal(cut.Instance.Meldung, Status().TextContent);
        Assert.NotEqual("", Status().TextContent);
        Assert.DoesNotContain("epos-status--fehler", Status().ClassName);

        Volumen(cut, 950);

        Assert.Equal("", Status().TextContent);
    }

    /// <summary>
    /// Ein gescheitertes Anlegen im OK-Weg meldet und HAELT den Dialog offen - der
    /// Arbeitsstand bleibt stehen, damit der Anwender ihn nicht verliert.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Anlegen_meldet_und_schliesst_nicht()
    {
        int? ergebnis = null;
        var stand = new Pruefstand { AnlegenErgebnis = 0 };
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Bezeichner(cut, "Neu");
        Volumen(cut, 500);
        Uebernehmen(cut);
        Ok(cut);

        Assert.Contains("nicht angelegt", cut.Instance.Meldung);
        Assert.NotEqual(WarnStufe.Erfolg, cut.Instance.MeldungStufe);
        Assert.Null(ergebnis);
        Assert.True(cut.Instance.Offen);
    }

    /// <summary>
    /// Kriterium W4 ist WEICH und kommt NACH der Uebernahme: Die Angabe steht im
    /// Arbeitsstand, der Anwender erfaehrt nur, was der Lauf mit ihr macht. Der Hinweis
    /// haengt allein an den Eingaben - er braucht keine geschriebene Zeile.
    /// </summary>
    [Fact]
    public void Der_Klemmhinweis_kommt_nach_dem_Uebernehmen()
    {
        var stand = MitZwei();
        stand.KlemmText = "Die Nutztemperatur wird geklemmt.";
        var cut = Zeige(stand);

        Uebernehmen(cut);

        Assert.True(cut.Instance.Offen);                       // uebernommen wurde
        Assert.Equal(0, stand.Schreibzugriffe);                // geschrieben nicht
        Assert.Contains("geklemmt", cut.Instance.Meldung);     // und gemeldet
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
        Assert.True(cut.Instance.Offen);

        // mit Referenzen: Rueckfrage
        stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Uebernehmen(cut);
        Assert.True(cut.Instance.FrageSteht);
        Assert.Contains("WP 1", cut.Instance.Fragetext);
        Assert.False(cut.Instance.Offen);
    }

    [Fact]
    public void Ja_auf_die_Nutzungsfrage_uebernimmt_Nein_nicht()
    {
        var stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        var cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Uebernehmen(cut);
        Ja(cut);
        Assert.True(cut.Instance.Offen);
        Assert.Equal(0, stand.Schreibzugriffe);

        stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        cut = Zeige(stand);
        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Uebernehmen(cut);
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent == "Nein").Click();
        Assert.False(cut.Instance.Offen);
    }

    /// <summary>
    /// Kommt die Nutzungsfrage im OK-Weg, fuehrt das Ja den Weg zu Ende: uebernehmen,
    /// schreiben, schliessen. Das Nein haelt den Dialog offen und schreibt nichts -
    /// sonst waere die Rueckfrage eine Formalie.
    /// </summary>
    [Fact]
    public void Die_Nutzungsfrage_fuehrt_den_OK_Weg_zu_Ende()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Ok(cut);
        Assert.True(cut.Instance.FrageSteht);
        Assert.Equal(0, stand.Schreibzugriffe);

        Ja(cut);
        Assert.Equal(11, stand.GeaendertId);
        Assert.Equal(11, ergebnis);

        // ... und dasselbe mit Nein
        stand = MitZwei();
        stand.Referenzen.Add("WP 1");
        ergebnis = null;
        cut = Zeige(stand, geschlossen: id => ergebnis = id);

        cut.FindAll("input[type=checkbox]")[2].Change(true);
        Ok(cut);
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent == "Nein").Click();

        Assert.Equal(0, stand.Schreibzugriffe);
        Assert.Null(ergebnis);
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

    /// <summary>
    /// Das Ja nimmt die Zeile aus der LISTE; die Datenbankzeile faellt erst im OK-Weg.
    /// Bis dahin ist das Entfernen ruecknehmbar - genau darum geht es beim Abbrechen.
    /// </summary>
    [Fact]
    public void Ohne_Referenzen_fragt_das_Entfernen_zurueck()
    {
        var stand = MitZwei();
        var cut = Zeige(stand);

        Entfernen(cut);
        Assert.True(cut.Instance.FrageSteht);
        Assert.Contains("Heizungsspeicher", cut.Instance.Fragetext);

        Ja(cut);
        Assert.Equal(new[] { 12 }, cut.Instance.Bestandszeilen);
        Assert.Equal(0, stand.Entfernt);
        Assert.Equal(0, stand.Schreibzugriffe);
        Assert.Equal(WarnStufe.Erfolg, cut.Instance.MeldungStufe);

        Ok(cut);
        Assert.Equal(11, stand.Entfernt);
    }

    /// <summary>
    /// Eine vorlaeufige Zeile hat keine Datenbankzeile: Ihr Entfernen erreicht die
    /// Datenbank nie - auch nicht im OK-Weg.
    /// </summary>
    [Fact]
    public void Eine_vorlaeufige_Zeile_faellt_ohne_Datenbankweg()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);

        Bezeichner(cut, "Nur kurz");
        Volumen(cut, 400);
        Uebernehmen(cut);
        Assert.Single(cut.Instance.Bestandszeilen);

        Entfernen(cut);
        Ja(cut);
        Assert.Empty(cut.Instance.Bestandszeilen);

        Ok(cut);
        Assert.Equal(0, stand.Schreibzugriffe);
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
    /// Der Dialog traegt das Hausmuster: OK und Abbrechen neben dem nicht schliessenden
    /// „Anlegen"/„Uebernehmen". Ohne Aenderung an der offenen Zeile schreibt OK nichts
    /// und liefert den gewaehlten Speicher.
    /// </summary>
    [Fact]
    public void Die_Leiste_traegt_OK_Abbrechen_und_Uebernehmen()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        var knoepfe = cut.FindAll(".epos-leiste button").Select(b => b.TextContent).ToList();
        Assert.Contains("OK", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);
        Assert.Contains("Übernehmen", knoepfe);

        Ok(cut);
        Assert.Equal(11, ergebnis);
        Assert.Equal(0, stand.Schreibzugriffe);
    }

    /// <summary>
    /// DER KERN DES MUSTERS: Abbrechen verlaesst den Dialog, ohne dass irgendetwas
    /// geschrieben wurde - auch dann nicht, wenn vorher eine Zeile angelegt, eine
    /// zweite geaendert und eine dritte entfernt wurde. Geprueft wird der
    /// DATENBANKSTAND, nicht der Dialogzustand.
    /// </summary>
    [Fact]
    public void Abbrechen_laesst_die_Datenbank_unveraendert()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        // 11 aendern
        Volumen(cut, 1200);
        Uebernehmen(cut);

        // 12 entfernen
        cut.FindAll(".epos-zr-zeile button")[1].Click();
        Entfernen(cut);
        Ja(cut);

        // eine dritte Zeile anlegen
        Bezeichner(cut, "Ganz neu");
        Volumen(cut, 700);
        Uebernehmen(cut);

        Assert.Equal(0, stand.Schreibzugriffe);

        Abbrechen(cut);

        Assert.Equal(0, stand.Schreibzugriffe);
        Assert.Empty(stand.Schreibfolge);
        Assert.Null(stand.Angelegt);
        Assert.Null(stand.Geaendert);
        Assert.Equal(0, stand.Entfernt);
        Assert.Equal(0, ergebnis);
    }

    /// <summary>
    /// OK schreibt denselben Arbeitsstand - und zwar in der Reihenfolge Entfernen,
    /// Aendern, Anlegen: Erst dann ist ein Bezeichner wieder frei, den eine neue Zeile
    /// tragen soll.
    /// </summary>
    [Fact]
    public void OK_schreibt_den_ganzen_Arbeitsstand_und_schliesst()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        stand.AnlegenErgebnis = 99;
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Volumen(cut, 1200);
        Uebernehmen(cut);

        cut.FindAll(".epos-zr-zeile button")[1].Click();
        Entfernen(cut);
        Ja(cut);

        Bezeichner(cut, "Ganz neu");
        Volumen(cut, 700);
        Uebernehmen(cut);

        Ok(cut);

        Assert.Equal(new[] { "entfernen 12", "aendern 11", "anlegen Ganz neu" },
                     stand.Schreibfolge);
        Assert.Equal(1200, stand.Geaendert!.Volumen);
        Assert.Equal(99, ergebnis);
    }

    /// <summary>
    /// OK prueft die offene Zeile, wenn an ihr etwas geaendert wurde: Eine verletzte
    /// Regel meldet, schreibt nicht und haelt den Dialog offen.
    /// </summary>
    [Fact]
    public void OK_prueft_die_offene_Zeile_und_haelt_bei_Fehler()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Schwelle(cut, 0, 96);          // Einschaltschwelle ueber der Abschaltschwelle
        Ok(cut);

        Assert.Contains("Einschaltschwelle muss kleiner", cut.Instance.Meldung);
        Assert.Equal(0, stand.Schreibzugriffe);
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// … eine UNBERUEHRTE Zeile prueft OK dagegen NICHT. Sonst koennte ein Altbestand,
    /// der die heutigen Regeln verletzt, den Dialog verriegeln: Der Anwender kaeme nur
    /// noch ueber Abbrechen hinaus.
    /// </summary>
    [Fact]
    public void OK_prueft_eine_unberuehrte_Zeile_nicht()
    {
        int? ergebnis = null;
        var stand = new Pruefstand();
        stand.Bestand.Add(new PspPufferstand(11, "Altbestand", 800, 1.5, 70, 50,
                                             10, 95, 95, 10, 0,
                                             false, false, false,     // leeres Klassen-Set
                                             new PspSchichtdaten()));
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Ok(cut);

        Assert.Equal(11, ergebnis);
        Assert.Equal(0, stand.Schreibzugriffe);
    }

    /// <summary>Esc wirkt wie Abbrechen: hinaus, ohne zu schreiben.</summary>
    [Fact]
    public void Esc_verwirft_wie_Abbrechen()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Volumen(cut, 1200);
        Uebernehmen(cut);
        cut.Find("div.epos-dialog").KeyDown("Escape");

        Assert.Equal(0, ergebnis);
        Assert.Equal(0, stand.Schreibzugriffe);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Esc/Abbrechen: hinaus, ohne zu schreiben.</summary>
    [Fact]
    public void Kreuz_verwirft_wie_Abbrechen()
    {
        int? ergebnis = null;
        var stand = MitZwei();
        var cut = Zeige(stand, geschlossen: id => ergebnis = id);

        Volumen(cut, 1200);
        Uebernehmen(cut);
        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(0, ergebnis);
        Assert.Equal(0, stand.Schreibzugriffe);
    }

    /// <summary>
    /// Traegt die umschliessende Ueberlagerung den Titel (TitelAnzeigen="false"),
    /// zeigt der Dialog auch kein eigenes Kreuz - Hausregel "Ein Titel, eine Stelle".
    /// </summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Dialog_kein_Kreuz()
    {
        var stand = MitZwei();
        var cut = Zeige(stand, titelAnzeigen: false);

        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    // ============================================================ Formularraster

    /// <summary>
    /// <b>iU8‑E‑2, Nachzug iU8‑O‑1</b> (Anwender, 05.09.2026: „Darstellung der
    /// Dialoge kompakter und übersichtlicher — Parameterblöcke rechts"): Die drei
    /// Parameterblöcke stehen im <c>Formularraster</c> — Beschriftung NEBEN dem
    /// Feld, Zahlenfelder kurz mit der Einheit dahinter, auf breitem Schirm zwei
    /// Feldpaare je Zeile.
    ///
    /// <para>Geprüft wird das MARKUP: Jeder Block trägt <c>epos-formularraster</c>,
    /// und darin stehen Felder. Was der Raster daraus MACHT (Beschriftungsspalte,
    /// kurzes Feld, zwei Spalten), steht als Stilblattprobe in
    /// <c>FormularrasterTests</c> — eine bunit-Probe rechnet kein CSS aus
    /// (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_drei_Bloecke_stehen_im_Formularraster()
    {
        var stand = new Pruefstand();
        stand.Bestand.Add(Speicher(11, "Geschichtet", schichten: 3));
        var cut = Zeige(stand);

        // Eigenschaften, Schichtung, Entladeprioritaet.
        Assert.Equal(3, cut.FindAll(".epos-formularraster").Count);
        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld"));

        // KEIN Raster ist einspaltig gesetzt: Die Felder tragen hier keine
        // Reihenfolge, sie gehoeren paarweise zusammen (Volumen/Verluste,
        // Vorlauf/Ruecklauf, Ein-/Abschaltschwelle).
        Assert.Empty(cut.FindAll(".epos-formularraster--einspaltig"));
    }

    /// <summary>
    /// Die Gruppen sind <c>Formulargruppe</c>n — leise Zwischenüberschriften, deren
    /// Felder DIREKTE Rasterkinder bleiben. Vier stehen fest (Volumen und Verluste,
    /// Temperaturen, Schwellen, Leistungsgrenzen), zwei erscheinen erst im
    /// erweiterten Teil (Schichtmodell, Entnahmehöhen).
    /// </summary>
    [Fact]
    public void Die_Gruppen_sind_leise_Zwischenueberschriften()
    {
        var stand = new Pruefstand();
        stand.Bestand.Add(Speicher(11, "Einzonig", schichten: 1));
        var cut = Zeige(stand);

        // Kompakt: Volumen und Verluste, Temperaturen, Schwellen, Leistungsgrenzen.
        Assert.Equal(4, cut.FindAll(".epos-formulargruppe-titel").Count);
        Assert.Contains("Volumen und Verluste", cut.Markup);
        Assert.Contains("Schwellen", cut.Markup);

        // Ab zwei Schichten kommen Schichtmodell und Entnahmehoehen dazu.
        cut.FindAll("input.epos-eingabe").First(f => f.GetAttribute("value") == "1").Input("3");

        Assert.Equal(6, cut.FindAll(".epos-formulargruppe-titel").Count);
        Assert.Contains("Schichtmodell", cut.Markup);
        Assert.Contains("Entnahmehöhe", cut.Markup);
    }

    /// <summary>
    /// Die Zahlenfelder melden sich als KURZE Felder mit der Einheit unmittelbar
    /// dahinter — Gesamtvolumen [l], Vorlauf [°C], die vier Schwellen [%]. Genau das
    /// war der sichtbare Teil des Befunds: Die Einheit stand am rechten Rand des
    /// Blocks statt hinter dem Wert.
    ///
    /// <para>Die <c>Mehrfachauswahl</c> „Nutzung" und die <c>Herleitungszeile</c>n
    /// stehen als Rasterkinder mit im Block; das Stilblatt spannt sie über alle
    /// Spalten (<c>FormularrasterTests</c>).</para>
    /// </summary>
    [Fact]
    public void Die_Zahlenfelder_sind_kurz_und_tragen_ihre_Einheit()
    {
        var cut = Zeige(MitZwei());

        Assert.NotEmpty(cut.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));

        Assert.NotEmpty(cut.FindAll(".epos-formularraster > .epos-mehrfachauswahl"));
        Assert.NotEmpty(cut.FindAll(".epos-formularraster > .epos-herleitung"));
    }

    /// <summary>
    /// Das <c>Zeilenraster</c> der Ladereihenfolge bleibt DRAUSSEN — eine Liste ist
    /// kein Formularblock (dieselbe Grenze, die Paket P3 bei der Senkenliste des
    /// <c>WaermesenkeDialog</c> gezogen hat). Der Raster dieses Blocks trägt nur die
    /// Entladepriorität und ihre beiden Herleitungszeilen.
    /// </summary>
    [Fact]
    public void Die_Listen_bleiben_ausserhalb_des_Rasters()
    {
        var cut = Zeige(MitZwei());

        Assert.NotEmpty(cut.FindAll(".epos-zeilenraster"));
        Assert.Empty(cut.FindAll(".epos-formularraster .epos-zeilenraster"));
    }

    // ============================================================ Hilfsgriffe

    private static void Uebernehmen(IRenderedComponent<PufferSpProjektDialog> cut)
        => cut.FindAll("button")
              .First(b => b.TextContent.Contains("Übernehmen") || b.TextContent.Contains("Anlegen"))
              .Click();

    /// <summary>OK — der einzige Weg, auf dem geschrieben wird.</summary>
    private static void Ok(IRenderedComponent<PufferSpProjektDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    private static void Abbrechen(IRenderedComponent<PufferSpProjektDialog> cut)
        => cut.FindAll(".epos-leiste button").First(b => b.TextContent == "Abbrechen").Click();

    private static void Entfernen(IRenderedComponent<PufferSpProjektDialog> cut)
        => cut.FindAll("button").First(b => b.TextContent.Contains("Entfernen")).Click();

    private static void Ja(IRenderedComponent<PufferSpProjektDialog> cut)
        => cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent == "Ja").Click();

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

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F2)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>PufferSpProjektKiSicht</c> und steht deshalb nicht in der
    /// Markup-Probe des Dialogkatalogs — dieser Fall ist ihr Ersatz und die schärfere
    /// Probe: Die Maske steht gezeichnet da, die Brücke liest den Wert, den der
    /// Anwender sieht, und ein Setzen landet im Eingabefeld.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist — die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_das_Volumen()
    {
        var cut = Zeige(MitZwei(), idPuffer: 11);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PUFFERSPEICHER_VERWALTUNG));

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, "volumen");
        Assert.NotNull(zugang);
        Assert.Equal(800, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(1200);

        // Der Wert steht danach im EINGABEFELD der Maske - nicht nur im Sichtmodell.
        cut.Render();
        Assert.Equal("1200", cut.FindAll("input.epos-eingabe")[1].GetAttribute("value"));
    }

    /// <summary>
    /// Der Bezeichner ist Pflichtfeld und Text — er wird gelesen und gesetzt wie jedes
    /// andere Feld der Maske.
    /// </summary>
    [Fact]
    public void Der_Assistent_liest_und_setzt_den_Bezeichner()
    {
        var cut = Zeige(MitZwei(), idPuffer: 11);

        KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, "bezeichner");

        Assert.Equal("Heizungsspeicher", zugang.Lesen());

        zugang.Setzen("Pufferspeicher Nord");
        Assert.Equal("Pufferspeicher Nord", zugang.Lesen());
    }

    /// <summary>
    /// <b>Die Entladepriorität ist ein WAHLFELD</b> (KI-F1b): Ihr Eintrag 0 trägt
    /// einen TEXT und keine Zahl — er wird über ihn getroffen, und die Brücke zeigt
    /// ihn danach auch als Text.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_Entladeprioritaet_ueber_ihren_Anzeigetext()
    {
        var cut = Zeige(MitZwei(), idPuffer: 11);

        KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, "entladeprioritaet");
        Assert.NotNull(zugang);

        KiFeldumsetzung rang = KiFeldwandler.Wandle(zugang, "7");
        Assert.True(rang.Ok, rang.Grund);
        zugang.Setzen(rang.Wert);
        cut.Render();
        Assert.Equal(7, zugang.Lesen());

        KiFeldumsetzung zurueck = KiFeldwandler.Wandle(zugang, "automatisch");
        Assert.True(zurueck.Ok, zurueck.Grund);
        zugang.Setzen(zurueck.Wert);
        cut.Render();
        Assert.Equal(0, zugang.Lesen());

        KiFeldwert wert = KiMaskenbruecke.Lesen(KiMaskennamen.PUFFERSPEICHER_VERWALTUNG)
                                         .Single(f => f.Name == "entladeprioritaet");
        Assert.Equal("automatisch", wert.Text);
        Assert.Equal("0", wert.Schluessel);
    }

    /// <summary>
    /// Die Nutzung ist auf der Maske EINE Mehrfachwahl; im Katalog sind es drei
    /// Wahrheitswerte. Ein Setzen nimmt die Klasse über denselben Rückruf in das Set
    /// hinein, den ein Klick nimmt — die übrigen Klassen bleiben stehen.
    /// </summary>
    [Fact]
    public void Der_Assistent_nimmt_das_Brauchwasser_in_die_Nutzung_auf()
    {
        var cut = Zeige(MitZwei(), idPuffer: 11);

        KiFeldzugang heizung = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, "nutzung_heizung");
        KiFeldzugang brauchwasser = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, "nutzung_brauchwasser");

        Assert.Equal(true, heizung.Lesen());
        Assert.Equal(false, brauchwasser.Lesen());

        brauchwasser.Setzen(true);
        cut.Render();

        Assert.Equal(true, brauchwasser.Lesen());
        Assert.Contains(0, cut.Instance.KlassenSet);
        Assert.Contains(1, cut.Instance.KlassenSet);
    }
}
