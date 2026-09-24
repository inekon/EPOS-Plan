using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Das Stammblatt, seine Gruppen und die Vergleichstabelle</b> — Konzept
/// Administrationsdialoge, Stufe 3: V9 (Stammblatt mit steckbaren Gruppen), V12
/// (Vergleich im Blatt bei zwei, drei Sätzen, breit ab vier), V13 (Auslieferungssatz:
/// Schloss und Hinweis in Worten im Kopf).
///
/// <para>Dazu der Kopf aus der Zeile der Liste (<see cref="Stammblattkopf"/>) und der
/// Vergleich aus Feldern (<see cref="Vergleichsbau"/>). Kultur gepinnt.</para>
/// </summary>
public class StammblattTests : EposBunitContext
{
    public StammblattTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment Text(string id, string text)
        => b => b.AddMarkupContent(0, $"<p id=\"{id}\">{text}</p>");

    private IRenderedComponent<Stammblatt> Aufbauen(Action<ComponentParameterCollectionBuilder<Stammblatt>>? mehr = null)
        => Render<Stammblatt>(p =>
        {
            p.Add(x => x.Name, "Kessel 7")
             .Add(x => x.Unterzeile, "Hersteller D · Holzpellets · eigener Satz")
             .Add(x => x.Kennzahlen, new[]
             {
                 new Stammblattkennzahl("50 kW", "Nennleistung"),
                 new Stammblattkennzahl("0,93", "Wirkungsgrad"),
                 new Stammblattkennzahl("Nein", "Brennwert")
             })
             .Add(x => x.Kenndaten, Text("kenndaten", "Felder"))
             .Add(x => x.Kosten, Text("kosten", "Investition"))
             .Add(x => x.AlleDaten, Text("alle", "Alle Daten"))
             .Add(x => x.Gruppen, Text("kennlinie", "Kennlinie"));
            mehr?.Invoke(p);
        });

    // =====================================================================
    //  Kopf und Gruppen (V9)
    // =====================================================================

    /// <summary>Kopf: Name, Herkunft, drei Kennzahlen (Wert und Name).</summary>
    [Fact]
    public void Der_Kopf_nennt_Name_Herkunft_und_Kennzahlen()
    {
        var cut = Aufbauen();

        Assert.Equal("Kessel 7", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal("Hersteller D · Holzpellets · eigener Satz", cut.Find(".epos-stammblatt-unter").TextContent);
        var kz = cut.FindAll(".epos-stammblatt-kennzahl");
        Assert.Equal(3, kz.Count);
        Assert.Equal("50 kW", kz[0].QuerySelector("dd")!.TextContent);
        Assert.Equal("Nennleistung", kz[0].QuerySelector("dt")!.TextContent);
        Assert.Equal("Stammblatt", cut.Find(".epos-stammblatt").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll(".epos-schloss"));
        Assert.Empty(cut.FindAll(".epos-stammblatt-schutz"));
    }

    /// <summary>
    /// <b>Die Reihenfolge der Gruppen</b>: zuerst die dialogspezifischen, dann Kenndaten,
    /// Kosten, Alle Daten, Nachsatz — die festen zwei mit ihrem Titel.
    /// </summary>
    [Fact]
    public void Die_Gruppen_stehen_in_der_Reihenfolge_des_Schemas()
    {
        var cut = Aufbauen(p => p.Add(x => x.Nachsatz, Text("nachsatz", "Berechnungsweg")));

        var ids = cut.Find(".epos-stammblatt-inhalt").QuerySelectorAll("p[id]").Select(e => e.Id).ToList();
        Assert.Equal(new[] { "kennlinie", "kenndaten", "kosten", "alle", "nachsatz" }, ids);
        Assert.Equal(new[] { "Kenndaten", "Kosten" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent));
    }

    /// <summary>Keine Gabe, keine Gruppe — ohne Kosten steht keine Überschrift „Kosten".</summary>
    [Fact]
    public void Ohne_Gabe_keine_Gruppe()
    {
        var cut = Render<Stammblatt>(p => p
            .Add(x => x.Name, "Speicher 1")
            .Add(x => x.Kenndaten, Text("kenndaten", "Felder")));

        Assert.Equal(new[] { "Kenndaten" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent));
        Assert.Empty(cut.FindAll(".epos-stammblatt-kennzahlen"));
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz</b> (V13): Schloss hinter dem Namen, der Kurztext des
    /// Wirts daran, und der Hinweis in Worten im Kopf — ein Kurztext erreicht eine
    /// Berührung nicht.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_traegt_Schloss_und_Hinweis_im_Kopf()
    {
        var cut = Aufbauen(p => p
            .Add(x => x.Auslieferung, true)
            .Add(x => x.SchlossText, "Auslieferungssatz – nur lesen, Duplizieren erlaubt"));

        var schloss = cut.Find(".epos-stammblatt-name .epos-schloss");
        Assert.Equal("Auslieferungssatz – nur lesen, Duplizieren erlaubt", schloss.GetAttribute("title"));
        Assert.Equal("Auslieferungssatz – nur lesen. Zum Ändern in der Auswahlleiste duplizieren oder das Schloss aufheben.",
                     cut.Find(".epos-stammblatt-schutz").TextContent);
    }

    /// <summary>Der Hinweis des Wirts steht im Fuß; ohne Hinweis gibt es keinen Fuß.</summary>
    [Fact]
    public void Der_Hinweis_steht_im_Fuss()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-stammblatt-fuss"));

        var cut = Aufbauen(p => p.Add(x => x.Hinweis, "2 Felder geändert"));
        Assert.Equal("2 Felder geändert", cut.Find(".epos-stammblatt-fuss .epos-stammblatt-hinweis").TextContent);
    }

    /// <summary>Ohne Fokuszeile: die leise Zeile, keine Gruppe.</summary>
    [Fact]
    public void Ohne_Name_steht_die_leise_Zeile()
    {
        var cut = Render<Stammblatt>(p => p
            .Add(x => x.Name, "")
            .Add(x => x.Kenndaten, Text("kenndaten", "Felder")));

        Assert.Equal("Keine Zeile gewählt.", cut.Find(".epos-stammblatt-leer").TextContent);
        Assert.Empty(cut.FindAll(".epos-stammblattgruppe"));
    }

    /// <summary>„‹ Liste" nur mit Delegat und nur im schmalen Fenster (<c>epos-nur-schmal</c>).</summary>
    [Fact]
    public void Zur_Liste_steht_nur_mit_Delegat()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-stammblatt-zurliste"));

        bool gerufen = false;
        var cut = Aufbauen(p => p.Add(x => x.ZurListe, EventCallback.Factory.Create(this, () => gerufen = true)));
        var knopf = cut.Find(".epos-stammblatt-zurliste");
        Assert.Contains("epos-nur-schmal", knopf.ClassName ?? "");
        knopf.Click();
        Assert.True(gerufen);
    }

    /// <summary>
    /// <b>Kopfhandlungen</b> stehen mit „‹ Liste" in EINER Zeile, und die ganze Zeile nur im
    /// schmalen Fenster (<c>epos-nur-schmal</c>); ohne Fragment keine Zeile, „‹ Liste" bleibt
    /// dann allein.
    /// </summary>
    [Fact]
    public void Kopfhandlungen_stehen_schmal_in_der_Zeile_von_Zur_Liste()
    {
        Assert.Empty(Aufbauen(p => p.Add(x => x.ZurListe, EventCallback.Factory.Create(this, () => { })))
                         .FindAll(".epos-stammblatt-kopfzeile"));

        bool gerufen = false;
        var cut = Aufbauen(p => p
            .Add(x => x.ZurListe, EventCallback.Factory.Create(this, () => gerufen = true))
            .Add(x => x.Kopfhandlungen, Text("handlung", "CSV")));

        var zeile = cut.Find(".epos-stammblatt-kopf > .epos-stammblatt-kopfzeile");
        Assert.Contains("epos-nur-schmal", zeile.ClassName ?? "");
        Assert.Single(zeile.QuerySelectorAll(".epos-stammblatt-zurliste"));
        Assert.NotNull(zeile.QuerySelector(".epos-stammblatt-kopfhandlungen #handlung"));
        Assert.Single(cut.FindAll(".epos-stammblatt-zurliste"));

        cut.Find(".epos-stammblatt-zurliste").Click();
        Assert.True(gerufen);
    }

    /// <summary>Ohne Gaben zeichnet das Blatt (Hausregel).</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_das_Blatt()
    {
        var cut = Render<Stammblatt>();
        Assert.Single(cut.FindAll(".epos-stammblatt-leer"));
    }

    // =====================================================================
    //  Der Vergleich (V12)
    // =====================================================================

    private static IReadOnlyList<Vergleichszeile> Vergleich(int saetze)
    {
        var liste = new List<IReadOnlyList<Vergleichsfeld>>();
        for (int i = 0; i < saetze; i++)
            liste.Add(new[]
            {
                new Vergleichsfeld("P", "Nennleistung [kW]", (20 + i * 10).ToString()),
                new Vergleichsfeld("B", "Brennstoff", "Erdgas")
            });
        return Vergleichsbau.AusFeldern(liste);
    }

    /// <summary>
    /// <b>Zwei oder drei Sätze: der Vergleich IM Blatt</b> — er tritt an die Stelle der
    /// Gruppen; „‹ Stammblatt von Kessel 7" führt zurück; der Fuß sagt, dass er nur
    /// lesbar ist.
    /// </summary>
    [Fact]
    public void Zwei_Saetze_vergleicht_das_Blatt_selbst()
    {
        bool zurueck = false;
        var cut = Aufbauen(p => p
            .Add(x => x.Vergleichskoepfe, new[] { "Kessel 4", "Kessel 5" })
            .Add(x => x.Vergleichszeilen, Vergleich(2))
            .Add(x => x.VergleichZurueck, EventCallback.Factory.Create(this, () => zurueck = true)));

        Assert.True(cut.Instance.VergleichImBlatt);
        Assert.False(cut.Instance.VergleichBreit);
        Assert.Equal("Vergleich", cut.Find(".epos-stammblatt-name").TextContent);
        Assert.Equal("2 Sätze", cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Empty(cut.FindAll(".epos-stammblattgruppe"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Equal(new[] { "Parameter", "Kessel 4", "Kessel 5" },
                     cut.FindAll(".epos-vergleich thead th").Select(th => th.TextContent.Trim()));
        Assert.Contains("nur lesbar", cut.Find(".epos-stammblatt-hinweis").TextContent);

        var knopf = cut.Find(".epos-stammblatt-zurueck");
        Assert.Equal("‹ Stammblatt von Kessel 7", knopf.TextContent);
        knopf.Click();
        Assert.True(zurueck);
    }

    /// <summary>
    /// <b>Ab vier Sätzen die breite Überlagerung</b> (V6): Vier Wertespalten passen nicht
    /// in 440 px. Das Blatt dahinter bleibt das der Fokuszeile; Kreuz und Esc der
    /// Überlagerung führen zurück wie „‹ Stammblatt".
    /// </summary>
    [Fact]
    public void Ab_vier_Saetzen_die_breite_Ueberlagerung()
    {
        int zurueck = 0;
        var cut = Aufbauen(p => p
            .Add(x => x.Vergleichskoepfe, new[] { "A", "B", "C", "D" })
            .Add(x => x.Vergleichszeilen, Vergleich(4))
            .Add(x => x.VergleichZurueck, EventCallback.Factory.Create(this, () => zurueck++)));

        Assert.True(cut.Instance.VergleichBreit);
        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Contains("epos-ueberlagerung--breit", ueberlagerung.ClassName ?? "");
        Assert.Equal(5, ueberlagerung.QuerySelectorAll(".epos-vergleich thead th").Length);
        Assert.Equal("Kessel 7", cut.Find(".epos-stammblatt-nametext").TextContent);

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.Equal(1, zurueck);

        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, zurueck);
    }

    /// <summary>
    /// <b>Die Tabelle kennzeichnet mit Worten</b>: „≠ abweichend" gegen „✓ stimmig", und
    /// der Schalter „nur abweichende Werte" blendet die gleichen aus.
    /// </summary>
    [Fact]
    public void Die_Vergleichstabelle_kennzeichnet_mit_Worten_und_blendet_aus()
    {
        var cut = Render<Vergleichstabelle>(p => p
            .Add(x => x.Koepfe, new[] { "A", "B" })
            .Add(x => x.Zeilen, Vergleich(2)));

        var zeilen = cut.FindAll("tbody tr");
        Assert.Equal(2, zeilen.Count);
        Assert.Contains("epos-vergleich--abweichend", zeilen[0].ClassName ?? "");
        Assert.Contains("abweichend", zeilen[0].TextContent);
        Assert.Contains("stimmig", zeilen[1].TextContent);

        cut.Find(".epos-vergleichstabelle-schalter input").Change(true);
        Assert.True(cut.Instance.NurAbweichende);
        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Empty(cut.FindAll(".epos-vergleichstabelle-leer"));
    }

    /// <summary>Stimmen alle Werte überein, sagt eine leise Zeile es, statt einer leeren Tabelle.</summary>
    [Fact]
    public void Ohne_Abweichung_sagt_eine_leise_Zeile_es()
    {
        var gleich = Vergleichsbau.AusFeldern(new IReadOnlyList<Vergleichsfeld>[]
        {
            new[] { new Vergleichsfeld("B", "Brennstoff", "Erdgas") },
            new[] { new Vergleichsfeld("B", "Brennstoff", "Erdgas") }
        });
        var cut = Render<Vergleichstabelle>(p => p
            .Add(x => x.Koepfe, new[] { "A", "B" })
            .Add(x => x.Zeilen, gleich));

        cut.Find(".epos-vergleichstabelle-schalter input").Change(true);
        Assert.Empty(cut.FindAll("tbody tr"));
        Assert.Equal("Die gewählten Sätze stimmen in jedem Wert überein.",
                     cut.Find(".epos-vergleichstabelle-leer").TextContent);
    }

    // =====================================================================
    //  Vergleichsbau und Stammblattkopf
    // =====================================================================

    /// <summary>
    /// <b>Die Reihenfolge ist die des ersten Satzes</b>; ein Feld, das ein anderer Satz
    /// nicht führt, steht als Leerwert und weicht damit ab.
    /// </summary>
    [Fact]
    public void Der_Vergleichsbau_folgt_dem_ersten_Satz()
    {
        var zeilen = Vergleichsbau.AusFeldern(new IReadOnlyList<Vergleichsfeld>[]
        {
            new[] { new Vergleichsfeld("X", "Erstes", "1"), new Vergleichsfeld("Y", "Zweites", "2") },
            new[] { new Vergleichsfeld("Y", "Zweites", "2") }
        });

        Assert.Equal(new[] { "Erstes", "Zweites" }, zeilen.Select(z => z.Name));
        Assert.Equal(new[] { "1", ParameterVerwendung.LEER }, zeilen[0].Werte);
        Assert.True(zeilen[0].Abweichend);
        Assert.False(zeilen[1].Abweichend);
        Assert.Empty(Vergleichsbau.AusFeldern(Array.Empty<IReadOnlyList<Vergleichsfeld>>()));
    }

    /// <summary>
    /// <b>Der Kopf aus der Zeile der Liste</b>: die ersten zwei belegten Textspalten außer
    /// dem Bezeichner und die Herkunft; die ersten drei Zahlen- und Ja/Nein-Spalten mit
    /// Einheit als Kennzahlen.
    /// </summary>
    [Fact]
    public void Der_Stammblattkopf_liest_die_Zeile_der_Liste()
    {
        var profil = Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);
        var zeile = new Katalogfilterzeile(7, "Kessel 7")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel 7")
            .MitText(Katalogfilterprofil.SpHersteller, "Hersteller D")
            .MitText(Katalogfilterprofil.SpBrennstoff, "Holzpellets")
            .MitZahl(Katalogfilterprofil.SpPtherm, 50.0);

        Assert.Equal("Hersteller D · Holzpellets · eigener Satz",
                     Stammblattkopf.Unterzeile(profil, zeile, "eigener Satz"));

        var kennzahlen = Stammblattkopf.Kennzahlen(profil, zeile);
        Assert.Equal(3, kennzahlen.Count);
        Assert.StartsWith("50", kennzahlen[0].Wert);
        Assert.EndsWith(" kW", kennzahlen[0].Wert);

        Assert.Equal("eigener Satz", Stammblattkopf.Unterzeile(profil, null, "eigener Satz"));
        Assert.Empty(Stammblattkopf.Kennzahlen(profil, null));
    }

    // =====================================================================
    //  Stufe 4: der LESEMODUS (V13) - Werte als Text statt gesperrter Felder
    // =====================================================================

    /// <summary>
    /// <b>Die Gruppe im Lesemodus</b> zeichnet ihre Werte als Liste aus Name und Wert —
    /// kein Eingabefeld, der Inhalt des Wirts entfällt; der Nachsatz steht darunter.
    /// Ohne Lesemodus bleibt der Inhalt.
    /// </summary>
    [Fact]
    public void Die_Gruppe_zeigt_im_Lesemodus_ihre_Werte_als_Text()
    {
        var werte = new[]
        {
            Stammblattwert.Abschnitt("Gerät"),
            new Stammblattwert("Nennleistung:", "50", "kW"),
            new Stammblattwert("Brennwert", ""),
        };

        var lesen = Render<Stammblattgruppe>(p => p
            .Add(x => x.Titel, "Kenndaten")
            .Add(x => x.KindInhalt, Text("felder", "<input>"))
            .Add(x => x.Lesemodus, true)
            .Add(x => x.Werte, werte)
            .Add(x => x.Nachsatz, Text("herleitung", "aus dem Datenbestand")));

        Assert.True(lesen.Instance.ZeigtWerte);
        Assert.Contains("epos-stammblattgruppe--lesen", lesen.Find("section").ClassName);
        Assert.Empty(lesen.FindAll("#felder"));
        Assert.Empty(lesen.FindAll("input, select, textarea"));
        Assert.Equal("Gerät", lesen.Find(".epos-stammblattwerte-abschnitt").TextContent);
        Assert.Equal(new[] { "Nennleistung", "Brennwert" },
                     lesen.FindAll(".epos-stammblattwert dt").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "50 kW", "–" },
                     lesen.FindAll(".epos-stammblattwert dd").Select(e => e.TextContent).ToArray());
        Assert.Single(lesen.FindAll("#herleitung"));

        var bearbeiten = Render<Stammblattgruppe>(p => p
            .Add(x => x.Titel, "Kenndaten")
            .Add(x => x.KindInhalt, Text("felder", "Felder"))
            .Add(x => x.Lesemodus, false)
            .Add(x => x.Werte, werte));

        Assert.False(bearbeiten.Instance.ZeigtWerte);
        Assert.Single(bearbeiten.FindAll("#felder"));
        Assert.Empty(bearbeiten.FindAll(".epos-stammblattwerte"));
    }

    /// <summary>
    /// <b>Das Stammblatt im Lesemodus</b> (Auslieferungssatz): Kenndaten und Kosten
    /// zeigen die Werte des Wirts statt seiner Felder; ohne Werte bleiben die Felder,
    /// ohne Lesemodus erst recht.
    /// </summary>
    [Fact]
    public void Das_Stammblatt_zeigt_im_Lesemodus_Kenndaten_und_Kosten_als_Text()
    {
        var cut = Aufbauen(p => p
            .Add(x => x.Lesemodus, true)
            .Add(x => x.KenndatenWerte, new[] { new Stammblattwert("Nennleistung", "50", "kW") })
            .Add(x => x.KostenWerte, new[] { new Stammblattwert("Investition", "12000", "€") }));

        Assert.Empty(cut.FindAll("#kenndaten"));
        Assert.Empty(cut.FindAll("#kosten"));
        Assert.Equal(new[] { "50 kW", "12000 €" },
                     cut.FindAll(".epos-stammblattwert dd").Select(e => e.TextContent).ToArray());

        var ohneWerte = Aufbauen(p => p.Add(x => x.Lesemodus, true));
        Assert.Single(ohneWerte.FindAll("#kenndaten"));
        Assert.Single(ohneWerte.FindAll("#kosten"));
    }

    /// <summary>
    /// <b>Der Wert eines Lesemodus</b>: ohne Wert der Halbgeviertstrich OHNE Einheit, mit
    /// Wert die Einheit dahinter; eine Feldbeschriftung verliert ihren Doppelpunkt.
    /// </summary>
    [Fact]
    public void Der_Stammblattwert_zeigt_Wert_und_Einheit()
    {
        Assert.Equal("50 kW", new Stammblattwert("P", "50", "kW").Anzeige);
        Assert.Equal("–", new Stammblattwert("P", "", "kW").Anzeige);
        Assert.Equal("–", new Stammblattwert("P", "–", "kW").Anzeige);
        Assert.Equal("Text", new Stammblattwert("P", " Text ").Anzeige);
        Assert.Equal("Name", Stammblattwert.OhneDoppelpunkt("Name:"));
        Assert.Equal("Name", Stammblattwert.OhneDoppelpunkt(" Name : "));
        Assert.True(Stammblattwert.Abschnitt("Eingang").IstAbschnitt);
    }

    // =====================================================================
    //  Stufe 4: die Loeschsperre der Zeitreihenverwaltungen (Ganglinienblatt)
    // =====================================================================

    /// <summary>
    /// <b>Löschen ist gesperrt, wenn KEINE Zielzeile gelöscht werden darf</b>: Der Grund
    /// nennt die Projekte, die die Zeilen führen; sind es nur Auslieferungssätze, sagt er
    /// das. Darf eine Zeile weg, ist nichts gesperrt.
    /// </summary>
    [Fact]
    public void Die_Loeschsperre_der_Ganglinien_nennt_die_Projekte()
    {
        var geschuetzt = new HashSet<string> { "Auslieferung" };
        var verwendung = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Messung 1"] = new[] { "Projekt 1" },
            ["Messung 2"] = new[] { "Projekt 1", "Projekt 2" }
        };

        Assert.Equal("", Ganglinienblatt.LoeschSperrgrund(Array.Empty<string>(), geschuetzt, verwendung));
        Assert.Equal("", Ganglinienblatt.LoeschSperrgrund(new[] { "Frei", "Messung 1" }, geschuetzt, verwendung));
        Assert.Equal("Auslieferungssatz – Löschen gesperrt. Zuerst „Schloss aufheben...“.",
                     Ganglinienblatt.LoeschSperrgrund(new[] { "Auslieferung" }, geschuetzt, verwendung));
        Assert.Equal("In Projekten verwendet („Projekt 1“, „Projekt 2“) – Löschen gesperrt; dort zuerst entfernen.",
                     Ganglinienblatt.LoeschSperrgrund(new[] { "Messung 1", "Messung 2", "Auslieferung" },
                                                      geschuetzt, verwendung));

        // Ohne Karte sperrt nur die Auslieferung.
        Assert.Equal("", Ganglinienblatt.LoeschSperrgrund(new[] { "Messung 1" }, geschuetzt, null));
    }

    /// <summary>
    /// <b>Die Rückfrage</b> nennt, was gelöscht wird, und getrennt, was stehen bleibt —
    /// Auslieferung und Verwendung; die Statuszeile danach den Namen oder die Zahlen.
    /// </summary>
    [Fact]
    public void Rueckfrage_und_Statuszeile_der_Ganglinien()
    {
        string frage = Ganglinienblatt.Rueckfrage(new[] { "A", "B" }, new[] { "C" }, new[] { "D" },
                                                  "Soll {0} gelöscht werden?");
        Assert.Contains("Sollen diese 2 Sätze gelöscht werden? A, B", frage);
        Assert.Contains("Stehen bleiben (Auslieferungssatz): C", frage);
        Assert.Contains("Stehen bleiben (in Projekten verwendet): D", frage);

        Assert.Equal("Soll A gelöscht werden?",
                     Ganglinienblatt.Rueckfrage(new[] { "A" }, Array.Empty<string>(), Array.Empty<string>(),
                                                "Soll {0} gelöscht werden?"));
        Assert.Equal("A weg", Ganglinienblatt.Geloescht(1, 1, "A", "{0} weg"));
        Assert.Equal("2 gelöscht, 1 stehen geblieben", Ganglinienblatt.Geloescht(3, 2, "", "{0} weg"));
    }
}
