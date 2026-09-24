using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>„Schloss setzen…" / „Schloss aufheben…"</b> — die gemeinsame Handlung der zehn
/// Verwaltungen (Konzept Administrationsdialoge, Entscheid AD-Q15): Beschriftung nach der
/// Auswahl, harte Sperre im Lesemodus, Rückfrage in beiden Richtungen, Statuszeile, das
/// Merken der in dieser Sitzung entsperrten Sätze. Dazu die zwei Bausteinteile, die sie
/// braucht: Kurztext und Breitenvorlage der <see cref="Auswahlleiste"/> und das Band
/// <c>ADM_SB_ENTSPERRT</c> im <see cref="Stammblatt"/>. Kultur gepinnt.
/// </summary>
public class SchlossumschaltungTests : EposBunitContext
{
    private readonly List<(IReadOnlyList<int> Ids, bool Gesperrt)> _geschaltet = new();

    private static Katalogfilterzeile Zeile(int id, string name, bool gesperrt)
        => new Katalogfilterzeile(id, name) { Geschuetzt = gesperrt };

    private static readonly Katalogfilterzeile A = Zeile(1, "Kessel A", true);
    private static readonly Katalogfilterzeile B = Zeile(2, "Kessel B", false);
    private static readonly Katalogfilterzeile C = Zeile(3, "Kessel C", true);

    /// <summary>Ein Weg, der mitschreibt und jede ID als geändert meldet.</summary>
    private Schlossweg Weg(bool lesemodus = false, SchlossErgebnis? ergebnis = null)
        => new Schlossweg((ids, gesperrt) =>
        {
            _geschaltet.Add((ids, gesperrt));
            return ergebnis ?? new SchlossErgebnis(true, "") { Geaendert = ids };
        })
        { Lesemodus = () => lesemodus };

    private EventCallback Leer => EventCallback.Factory.Create(this, () => { });

    // =====================================================================
    //  Die Handlung: Beschriftung folgt der Auswahl, harte Sperre
    // =====================================================================

    [Fact]
    public void Ohne_Weg_keine_Handlung()
    {
        Assert.Null(new Schlossumschaltung().Handlung(null, Leer, new[] { A }, false));
    }

    /// <summary>
    /// <b>Die Beschriftung folgt der Auswahl</b>: Ist eine Zielzeile gesperrt, heißt die
    /// Handlung „Schloss aufheben…" (auch bei gemischter Auswahl), sonst „Schloss setzen…";
    /// die Breitenvorlage ist immer die andere Beschriftung.
    /// </summary>
    [Fact]
    public void Die_Beschriftung_folgt_der_Auswahl()
    {
        var s = new Schlossumschaltung();

        Auswahlhandlung auf = s.Handlung(Weg(), Leer, new[] { A }, false)!;
        Assert.Equal(Resource.ADM_AW_SCHLOSS_AUFHEBEN, auf.Text);
        Assert.Equal(Resource.ADM_AW_SCHLOSS_SETZEN, auf.Breitenvorlage);
        Assert.Equal(Resource.ADM_AW_SCHLOSS_AUFHEBEN_KURZ, auf.Kurztext);
        Assert.False(auf.Aus);

        Auswahlhandlung gemischt = s.Handlung(Weg(), Leer, new[] { A, B }, false)!;
        Assert.Equal(Resource.ADM_AW_SCHLOSS_AUFHEBEN, gemischt.Text);

        Auswahlhandlung zu = s.Handlung(Weg(), Leer, new[] { B }, false)!;
        Assert.Equal(Resource.ADM_AW_SCHLOSS_SETZEN, zu.Text);
        Assert.Equal(Resource.ADM_AW_SCHLOSS_AUFHEBEN, zu.Breitenvorlage);
        Assert.Equal(Resource.ADM_AW_SCHLOSS_SETZEN_KURZ, zu.Kurztext);
        Assert.Equal("Schloss aufheben...", auf.Text);
        Assert.Equal("Schloss setzen...", zu.Text);
    }

    [Fact]
    public void Im_Lesemodus_der_Lizenz_und_bei_NurLesen_hart_gesperrt()
    {
        var s = new Schlossumschaltung();
        Assert.True(s.Handlung(Weg(lesemodus: true), Leer, new[] { A }, false)!.Aus);
        Assert.True(s.Handlung(Weg(), Leer, new[] { A }, true)!.Aus);

        // Selbst ein Klick, der doch ankäme, fragt nicht und schreibt nicht.
        Assert.Equal(Resource.LIZ_LESEMODUS_SPERRE, s.Fragen(Weg(lesemodus: true), new[] { A }, false));
        Assert.False(s.FrageOffen);
        Assert.Empty(_geschaltet);
    }

    [Fact]
    public void Ungespeicherte_Felder_halten_die_Frage_an()
    {
        var s = new Schlossumschaltung();
        Assert.Equal(Resource.ADM_MSG_UNGESPEICHERT, s.Fragen(Weg(), new[] { A }, true));
        Assert.False(s.FrageOffen);
    }

    [Fact]
    public void Die_Zielzeilen_kommen_ueber_den_Schluessel_in_Listenreihenfolge()
    {
        IReadOnlyList<Katalogfilterzeile> z = Schlossumschaltung.Zeilen(new[] { "Kessel C", "Kessel A" }, new[] { A, B, C });
        Assert.Equal(new[] { A, C }, z);
    }

    // =====================================================================
    //  Die Rückfrage und die Antwort
    // =====================================================================

    /// <summary>
    /// <b>Aufheben wirkt nur auf die gesperrten Sätze</b> einer gemischten Auswahl; die
    /// Frage nennt sie, der Titel sagt die Richtung, ein „Ja" schaltet genau sie auf eigen.
    /// </summary>
    [Fact]
    public void Aufheben_fragt_nach_und_schaltet_nur_die_gesperrten()
    {
        var s = new Schlossumschaltung();
        Assert.Null(s.Fragen(Weg(), new[] { A, B, C }, false));

        Assert.True(s.FrageOffen);
        Assert.True(s.Aufheben);
        Assert.Equal(Resource.ADM_SCHLOSS_AUFHEBEN_TITEL, s.Titel);
        Assert.Equal(string.Format(Resource.ADM_SCHLOSS_AUFHEBEN_FRAGE_MEHR, 2, "Kessel A, Kessel C"), s.Frage);

        Schlossausgang a = s.Beantwortet(true, Weg());

        Assert.True(a.Geschrieben);
        Assert.False(s.FrageOffen);
        Assert.Single(_geschaltet);
        Assert.Equal(new[] { 1, 3 }, _geschaltet[0].Ids);
        Assert.False(_geschaltet[0].Gesperrt);
        Assert.Equal(string.Format(Resource.ADM_MSG_SCHLOSS_AUFGEHOBEN_MEHR, 2), a.Status);
        Assert.Equal(new[] { 1, 3 }, s.Entsperrte.OrderBy(i => i));
    }

    [Fact]
    public void Ein_Satz_Frage_mit_Namen_und_Zusatz_beim_Aufheben()
    {
        var s = new Schlossumschaltung();
        s.Fragen(Weg(), new[] { A }, false, ziele => "Zusatz " + ziele.Count);

        Assert.Equal(string.Format(Resource.ADM_SCHLOSS_AUFHEBEN_FRAGE, "Kessel A") + " Zusatz 1", s.Frage);
        Assert.StartsWith("Schloss von „Kessel A“ aufheben?", s.Frage);

        Schlossausgang a = s.Beantwortet(true, Weg());
        Assert.Equal("Schloss von „Kessel A“ aufgehoben.", a.Status);
    }

    /// <summary>
    /// <b>Setzen</b>, wenn keine Zielzeile gesperrt ist: für alle, der Zusatz des Aufhebens
    /// fehlt, und ein wieder gesetztes Schloss nimmt den Satz aus der Merkliste.
    /// </summary>
    [Fact]
    public void Setzen_fragt_fuer_alle_und_vergisst_das_Entsperren()
    {
        var s = new Schlossumschaltung();
        s.Fragen(Weg(), new[] { A }, false);
        s.Beantwortet(true, Weg());
        Assert.Contains(1, s.Entsperrte);

        var eigen = Zeile(1, "Kessel A", false);
        Assert.True(s.Entsperrt(eigen));

        s.Fragen(Weg(), new[] { eigen, B }, false, _ => "darf nicht erscheinen");
        Assert.False(s.Aufheben);
        Assert.Equal(Resource.ADM_SCHLOSS_SETZEN_TITEL, s.Titel);
        Assert.Equal(string.Format(Resource.ADM_SCHLOSS_SETZEN_FRAGE_MEHR, 2, "Kessel A, Kessel B"), s.Frage);

        Schlossausgang a = s.Beantwortet(true, Weg());
        Assert.True(_geschaltet[^1].Gesperrt);
        Assert.Equal(new[] { 1, 2 }, _geschaltet[^1].Ids);
        Assert.Equal(string.Format(Resource.ADM_MSG_SCHLOSS_GESETZT_MEHR, 2), a.Status);
        Assert.Empty(s.Entsperrte);
        Assert.False(s.Entsperrt(eigen));
    }

    [Fact]
    public void Nein_und_Esc_schreiben_nichts()
    {
        var s = new Schlossumschaltung();
        s.Fragen(Weg(), new[] { A }, false);
        Assert.Same(Schlossausgang.Nichts, s.Beantwortet(false, Weg()));

        s.Fragen(Weg(), new[] { A }, false);
        Assert.Same(Schlossausgang.Nichts, s.Beantwortet(null, Weg()));

        Assert.Empty(_geschaltet);
        Assert.False(s.FrageOffen);
    }

    [Fact]
    public void Ein_Fehlschlag_kommt_ins_Warnband_und_nichts_wird_gemerkt()
    {
        var s = new Schlossumschaltung();
        s.Fragen(Weg(), new[] { A }, false);

        Schlossausgang a = s.Beantwortet(true, Weg(ergebnis: new SchlossErgebnis(false, "kaputt")));

        Assert.False(a.Geschrieben);
        Assert.Equal("kaputt", a.Fehler);
        Assert.Empty(s.Entsperrte);
    }

    [Fact]
    public void Unveraenderte_Saetze_nennt_die_Statuszeile()
    {
        var s = new Schlossumschaltung();
        s.Fragen(Weg(), new[] { A, C }, false);

        Schlossausgang a = s.Beantwortet(true, Weg(ergebnis: new SchlossErgebnis(true, "")
        {
            Geaendert = new[] { 1 },
            Unveraendert = new[] { 3 }
        }));

        Assert.Equal("Schloss von „Kessel A“ aufgehoben. " + string.Format(Resource.ADM_SCHLOSS_UNVERAENDERT, 1), a.Status);
        Assert.Equal(new[] { 1 }, s.Entsperrte);
    }

    // =====================================================================
    //  Die Auswahlleiste: Kurztext und Breitenvorlage
    // =====================================================================

    private IRenderedComponent<Auswahlleiste> Leiste(params Auswahlhandlung[] handlungen)
        => Render<Auswahlleiste>(p => p.Add(x => x.Fokus, "Kessel A").Add(x => x.Handlungen, handlungen));

    [Fact]
    public void Der_freie_Knopf_traegt_den_Kurztext()
    {
        var cut = Leiste(new Auswahlhandlung("Tun", Leer) { Kurztext = "Was es tut" },
                         new Auswahlhandlung("Ohne", Leer));

        var knoepfe = cut.FindAll(".epos-auswahlleiste-knopf");
        Assert.Equal("Was es tut", knoepfe[0].GetAttribute("title"));
        Assert.False(knoepfe[1].HasAttribute("title"));
    }

    /// <summary>
    /// <b>Die Breitenvorlage</b>: zwei Texte in einer Zelle, der zweite für die Sprachausgabe
    /// verborgen — die Beschriftung ist die erste; ohne Vorlage bleibt der Knopf, wie er war.
    /// </summary>
    [Fact]
    public void Die_Breitenvorlage_steht_verborgen_neben_der_Beschriftung()
    {
        var cut = Leiste(new Auswahlhandlung("Schloss aufheben...", Leer) { Breitenvorlage = "Schloss setzen..." },
                         new Auswahlhandlung("Löschen", Leer));

        var knopf = cut.FindAll(".epos-auswahlleiste-knopf")[0];
        Assert.Contains("epos-auswahlleiste-knopf--zweitext", knopf.ClassList);
        Assert.Equal("Schloss aufheben...", knopf.QuerySelector(".epos-auswahlleiste-text")!.TextContent);
        var vorlage = knopf.QuerySelector(".epos-auswahlleiste-vorlage")!;
        Assert.Equal("Schloss setzen...", vorlage.TextContent);
        Assert.Equal("true", vorlage.GetAttribute("aria-hidden"));

        var loeschen = cut.FindAll(".epos-auswahlleiste-knopf")[1];
        Assert.DoesNotContain("epos-auswahlleiste-knopf--zweitext", loeschen.ClassList);
        Assert.Equal("Löschen", loeschen.TextContent.Trim());
    }

    [Fact]
    public void Hart_gesperrt_bleibt_disabled_mit_Vorlage()
    {
        var cut = Leiste(new Auswahlhandlung("Schloss aufheben...", Leer) { Breitenvorlage = "Schloss setzen...", Aus = true });
        var knopf = cut.Find(".epos-auswahlleiste-knopf");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.NotNull(knopf.QuerySelector(".epos-auswahlleiste-vorlage"));
    }

    // =====================================================================
    //  Das Stammblatt: Band „Schloss aufgehoben"
    // =====================================================================

    private IRenderedComponent<Stammblatt> Blatt(bool auslieferung, bool entsperrt)
        => Render<Stammblatt>(p => p.Add(x => x.Name, "Kessel A")
                                    .Add(x => x.Auslieferung, auslieferung)
                                    .Add(x => x.Entsperrt, entsperrt));

    [Fact]
    public void Das_Band_steht_bei_einem_entsperrten_Satz_am_Platz_des_Auslieferungshinweises()
    {
        var entsperrt = Blatt(false, true);
        var band = entsperrt.Find(".epos-stammblatt-schutz");
        Assert.Contains("epos-stammblatt-schutz--entsperrt", band.ClassList);
        Assert.Equal(Resource.ADM_SB_ENTSPERRT, band.TextContent);
        Assert.StartsWith("Ausgelieferter Satz, Schloss aufgehoben", band.TextContent);

        // Ein Auslieferungssatz trägt seinen Hinweis, nie beide.
        var gesperrt = Blatt(true, true);
        Assert.Single(gesperrt.FindAll(".epos-stammblatt-schutz"));
        Assert.Equal(Resource.ADM_SB_NUR_LESEN, gesperrt.Find(".epos-stammblatt-schutz").TextContent);

        Assert.Empty(Blatt(false, false).FindAll(".epos-stammblatt-schutz"));
    }
}
