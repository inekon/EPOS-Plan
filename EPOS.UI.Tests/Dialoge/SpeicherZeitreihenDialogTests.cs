using System.Globalization;
using System.Text;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Zeugen der Maske „Zeitreihe einlesen" (Entscheid KI‑D‑Q9, 21.09.2026).
///
/// <para><b>Befund 1 — sechzehn deutsche Beschriftungen als Literale.</b> Die Maske
/// schrieb Beschriftungen, Gruppentitel, Knopftexte und die Eintraege ihrer
/// Klapplisten als deutsche Zeichenketten ins Markup; <c>Resource.</c> kam in der
/// Datei nicht vor. Hier steht dagegen, dass jeder sichtbare Text aus dem
/// Textbuendel und damit aus <c>MyResource</c> kommt — die Gegenprobe in der
/// zweiten Sprache fuehrt <see cref="SpeicherZeitreihenDialogEnglischTests"/>.</para>
///
/// <para><b>Befund 2 — die Intervallkonvention hing an einem Listenplatz.</b> Die
/// Klappliste fuehrte zwei Eintraege mit den Ids 0 („Anfang") und 1 („Ende"),
/// waehrend <see cref="IntervallKonvention.Anfang"/> den Wert 1 traegt und
/// <c>Auswahlfeld</c> die ID meldet: Beim ersten Zeichnen stand „Ende" da, obwohl
/// „Anfang" galt, und „Automatisch" — der erste Wert der Aufzaehlung — war
/// ueberhaupt nicht waehlbar.</para>
/// </summary>
public sealed class SpeicherZeitreihenDialogTests : EposBunitContext
{
    // =====================================================================
    //  Befund 1: die Beschriftungen kommen aus den Ressourcen
    // =====================================================================

    /// <summary>
    /// Die zwoelf Beschriftungen der EINEN Zeitstempelspalte — in der Reihenfolge der
    /// Maske und woertlich die Ressourcenwerte.
    /// </summary>
    [Fact]
    public void Alle_Beschriftungen_der_Maske_kommen_aus_den_Ressourcen()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, LastCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Assert.Equal(new[]
        {
            Resource.SZR_LBL_TRENNZEICHEN,
            Resource.SZR_LBL_DEZIMALTRENNER,
            Resource.SZR_LBL_KODIERUNG,
            Resource.SZR_LBL_KOPFZEILE,
            Resource.SZR_LBL_UEBERSPRINGEN,
            Resource.SZR_LBL_ZEITANGABE,
            Resource.SZR_LBL_ZEITSTEMPELSPALTE,
            Resource.SZR_LBL_ZEITSTEMPELFORMAT,
            Resource.SZR_LBL_WERTSPALTE,
            Resource.SZR_LBL_ZEITZONE,
            Resource.SZR_LBL_INTERVALLLAGE,
            Resource.SZR_LBL_EINHEIT
        }, Beschriftungen(cut));

        // Gruppentitel, Vorschauueberschrift und die zwei Knoepfe ebenso.
        Assert.Contains(Resource.SZR_GRP_FORMAT, cut.Markup);
        Assert.Contains(Resource.SZR_GRP_SPALTEN, cut.Markup);
        Assert.Contains(Resource.SZR_GRP_VORSCHAU, cut.Markup);
        Assert.Equal(Resource.SZR_BTN_ABBRECHEN, Knopf(cut, 0));
        Assert.Equal(Resource.SZR_BTN_UEBERNEHMEN, Knopf(cut, 1));
    }

    /// <summary>
    /// Die vier Beschriftungen der GETRENNTEN Datums- und Uhrzeitspalten — mit den
    /// zwoelf oben sind es die sechzehn des Befundes.
    /// </summary>
    [Fact]
    public void Auch_die_getrennte_Zeitangabe_beschriftet_aus_den_Ressourcen()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, GetrennteCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Auswahl(cut, Resource.SZR_LBL_ZEITANGABE).Change("1");
        cut.Render();

        string[] sichtbar = Beschriftungen(cut);
        Assert.Contains(Resource.SZR_LBL_DATUMSSPALTE, sichtbar);
        Assert.Contains(Resource.SZR_LBL_UHRZEITSPALTE, sichtbar);
        Assert.Contains(Resource.SZR_LBL_DATUMSFORMAT, sichtbar);
        Assert.Contains(Resource.SZR_LBL_UHRZEITFORMAT, sichtbar);
        Assert.DoesNotContain(Resource.SZR_LBL_ZEITSTEMPELSPALTE, sichtbar);
    }

    /// <summary>
    /// Auch die Eintraege der festen Klapplisten stehen in den Ressourcen — samt der
    /// Vorlage „Spalte {0}" und der Einheit „kWh je Intervall"; die reinen
    /// Einheitenzeichen (kW, MW) bleiben Literale, sie werden nicht uebersetzt.
    /// </summary>
    [Fact]
    public void Die_Eintraege_der_Klapplisten_stehen_in_den_Ressourcen()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, LastCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Assert.Equal(new[] { Resource.SZR_OPT_SEMIKOLON, Resource.SZR_OPT_KOMMA, Resource.SZR_OPT_TABULATOR },
                     Texte(cut, Resource.SZR_LBL_TRENNZEICHEN));
        Assert.Equal(new[] { Resource.SZR_OPT_DEZ_KOMMA, Resource.SZR_OPT_DEZ_PUNKT },
                     Texte(cut, Resource.SZR_LBL_DEZIMALTRENNER));
        Assert.Equal(new[] { Resource.SZR_OPT_UTF8, Resource.SZR_OPT_WINDOWS1252 },
                     Texte(cut, Resource.SZR_LBL_KODIERUNG));
        Assert.Equal(new[] { Resource.SZR_OPT_ZEIT_EINE_SPALTE, Resource.SZR_OPT_ZEIT_GETRENNT },
                     Texte(cut, Resource.SZR_LBL_ZEITANGABE));
        Assert.Equal(new[] { "kW", "MW", Resource.SZR_OPT_KWH_INTERVALL },
                     Texte(cut, Resource.SZR_LBL_EINHEIT));
        Assert.Equal(Spaltentext(1), Texte(cut, Resource.SZR_LBL_WERTSPALTE)[0]);
    }

    // =====================================================================
    //  Befund 2: die Klappliste haengt an der AUFZAEHLUNG
    // =====================================================================

    /// <summary>
    /// Drei Eintraege in der Reihenfolge der Aufzaehlung, „automatisch erkennen"
    /// zuerst, und jede Id IST ihr Aufzaehlungswert.
    /// </summary>
    [Fact]
    public void Die_Konventionsliste_fuehrt_drei_Eintraege_in_Aufzaehlungsreihenfolge()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, LastCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        AngleSharp.Dom.IElement liste = Auswahl(cut, Resource.SZR_LBL_INTERVALLLAGE);

        Assert.Equal(new[] { Id(IntervallKonvention.Automatisch), Id(IntervallKonvention.Anfang),
                             Id(IntervallKonvention.Ende) },
                     liste.QuerySelectorAll("option").Select(x => x.GetAttribute("value")).ToArray());
        Assert.Equal(new[] { Resource.IMPORT_KONV_AUTO, Resource.IMPORT_KONV_ANFANG,
                             Resource.IMPORT_KONV_ENDE },
                     liste.QuerySelectorAll("option").Select(x => x.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// <b>Die Vorauswahl ist der wirkliche Wert.</b> Die Vorgabe der
    /// <c>SpeicherZeitreihenOptionen</c> ist <see cref="IntervallKonvention.Anfang"/> —
    /// also steht beim ersten Zeichnen „Intervallanfang" da, nicht mehr
    /// „Intervallende".
    /// </summary>
    [Fact]
    public void Beim_ersten_Zeichnen_steht_der_wirkliche_Wert_Intervallanfang()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, LastCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        AngleSharp.Dom.IElement gewaehlt =
            Auswahl(cut, Resource.SZR_LBL_INTERVALLLAGE).QuerySelector("option[selected]")!;

        Assert.Equal(Id(IntervallKonvention.Anfang), gewaehlt.GetAttribute("value"));
        Assert.Equal(Resource.IMPORT_KONV_ANFANG, gewaehlt.TextContent.Trim());
        Assert.Equal(IntervallKonvention.Anfang, Konvention());
    }

    /// <summary>
    /// Die Wahl schreibt genau den Aufzaehlungswert — „Intervallende" ergibt
    /// <see cref="IntervallKonvention.Ende"/>, der erste Eintrag
    /// <see cref="IntervallKonvention.Automatisch"/>. Gelesen wird ueber den
    /// Feldzugang des Assistenten; er zeigt auf denselben Optionssatz.
    /// </summary>
    [Fact]
    public void Die_Wahl_setzt_den_Aufzaehlungswert_einschliesslich_Automatisch()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, LastCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Auswahl(cut, Resource.SZR_LBL_INTERVALLLAGE).Change(Id(IntervallKonvention.Ende));
        cut.Render();
        Assert.Equal(IntervallKonvention.Ende, Konvention());
        Assert.Equal(Id(IntervallKonvention.Ende), AusgewaehlterWert(cut, Resource.SZR_LBL_INTERVALLLAGE));

        Auswahl(cut, Resource.SZR_LBL_INTERVALLLAGE).Change(Id(IntervallKonvention.Automatisch));
        cut.Render();
        Assert.Equal(IntervallKonvention.Automatisch, Konvention());
        Assert.Equal(Id(IntervallKonvention.Automatisch),
                     AusgewaehlterWert(cut, Resource.SZR_LBL_INTERVALLLAGE));
    }

    /// <summary>
    /// <b>„Automatisch" ist nicht nur waehlbar, es traegt auch durch:</b> Die Maske
    /// liest die Datei damit ein, der Kern loest die Konvention am ersten Zeitstempel
    /// auf, und die uebernommene Reihe fuehrt den AUFGELOESTEN Wert. Die Reihe hier
    /// beginnt um 00:15 im Viertelstundenraster — also Intervallende.
    /// </summary>
    [Fact]
    public void Mit_Automatisch_laesst_sich_die_Datei_uebernehmen()
    {
        SpeicherZeitreihe? reihe = null;
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, EndeCsv())
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last)
            .Add(x => x.Uebernommen, x => reihe = x));

        Auswahl(cut, Resource.SZR_LBL_INTERVALLLAGE).Change(Id(IntervallKonvention.Automatisch));
        cut.Render();
        cut.Find("button.epos-knopf--primaer").Click();

        Assert.NotNull(reihe);
        Assert.Equal(IntervallKonvention.Ende, reihe!.Optionen.Konvention);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    // =====================================================================
    //  Werkzeug
    // =====================================================================

    private static SpeicherImportDatei LastCsv() => new()
    {
        Dateiname = "last.csv",
        Inhalt = Encoding.UTF8.GetBytes("Zeit;Last\n2026-01-01 00:00;1,5\n2026-01-01 00:15;2,0\n")
    };

    private static SpeicherImportDatei GetrennteCsv() => new()
    {
        Dateiname = "getrennt.csv",
        Inhalt = Encoding.UTF8.GetBytes("Datum;Zeit;Last\n2026-01-01;00:00;1,5\n2026-01-01;00:15;2,0\n")
    };

    /// <summary>Eine Reihe, die um 00:15 des 01.01. beginnt — die Automatik erkennt Ende.</summary>
    private static SpeicherImportDatei EndeCsv() => new()
    {
        Dateiname = "ende.csv",
        Inhalt = Encoding.UTF8.GetBytes("Zeit;Last\n2026-01-01 00:15;1,5\n2026-01-01 00:30;2,0\n")
    };

    /// <summary>Die Id eines Aufzaehlungswerts, wie sie im Markup steht.</summary>
    private static string Id(IntervallKonvention wert)
        => ((int)wert).ToString(CultureInfo.InvariantCulture);

    private static string Spaltentext(int nummer)
        => string.Format(CultureInfo.CurrentCulture, Resource.SZR_OPT_SPALTE, nummer);

    private static string[] Beschriftungen(IRenderedComponent<SpeicherZeitreihenDialog> cut)
        => cut.FindAll(".epos-feld-text").Select(x => x.TextContent.Trim()).ToArray();

    private static string Knopf(IRenderedComponent<SpeicherZeitreihenDialog> cut, int stelle)
        => cut.FindAll(".epos-leiste button")[stelle].TextContent.Trim();

    private static AngleSharp.Dom.IElement Auswahl(
        IRenderedComponent<SpeicherZeitreihenDialog> cut, string label)
        => cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;

    private static string[] Texte(IRenderedComponent<SpeicherZeitreihenDialog> cut, string label)
        => Auswahl(cut, label).QuerySelectorAll("option").Select(x => x.TextContent.Trim()).ToArray();

    private static string? AusgewaehlterWert(
        IRenderedComponent<SpeicherZeitreihenDialog> cut, string label)
        => Auswahl(cut, label).QuerySelector("option[selected]")?.GetAttribute("value");

    /// <summary>Der lebende Wert der Maske — ueber den Feldzugang des Assistenten.</summary>
    private static IntervallKonvention Konvention()
        => (IntervallKonvention)KiMaskenbruecke
            .Feldzugang(KiMaskennamen.SPEICHER_ZEITREIHEN, "intervallbezug").Lesen()!;
}

/// <summary>
/// <b>Die Gegenprobe in der zweiten Sprache.</b> Ein Text aus den Ressourcen wechselt
/// mit der Oberflaechenkultur — ein Literal im Markup nicht. Dieser Fall zeichnet
/// dieselbe Maske unter <c>en-US</c> und haelt fest, dass die englischen Texte
/// dastehen und KEINER der frueheren deutschen Literaltexte.
/// </summary>
public sealed class SpeicherZeitreihenDialogEnglischTests : EposBunitContext
{
    /// <summary>
    /// Die Maske spricht hier Englisch — der KI-Dialogkatalog aber nicht.
    /// </summary>
    /// <remarks>
    /// <b>Der Katalog ist ein Bau-einmal-Zwischenspeicher der SITZUNG</b>
    /// (<c>KiDialoge.Katalog</c>): Seine Anzeigenamen frieren in der Sprache ein, in
    /// der ihn der ERSTE Zugriff baut. Diese Maske meldet sich beim Zeichnen am
    /// Assistenten an und fasst ihn damit an — waere sie der erste Zugriff des
    /// Testlaufs, stuende der Katalog danach fuer ALLE Faelle auf Englisch, und
    /// <c>KiDialogkatalogTests</c> verglichen englische Katalognamen mit deutschen
    /// Maskenbeschriftungen. Der Katalog wird deshalb hier ausdruecklich auf Deutsch
    /// vorgewaermt; danach gilt wieder <c>en-US</c>.
    /// </remarks>
    public SpeicherZeitreihenDialogEnglischTests() : base("en-US")
    {
        using (new Kulturvorrichtung("de-DE")) _ = KiDialoge.Katalog;
    }

    /// <summary>Die frueheren Literale der Maske — sie duerfen nirgends mehr stehen.</summary>
    private static readonly string[] AlteLiterale =
    {
        "Trennzeichen:", "Dezimaltrenner:", "Kodierung:",
        "Erste Datenzeile ist eine Kopfzeile", "Zeilen davor überspringen:",
        "Zeitangabe:", "Zeitstempelspalte:", "Zeitstempelformat (leer = Standard):",
        "Datumsspalte:", "Uhrzeitspalte:", "Datumsformat (leer = Standard):",
        "Uhrzeitformat (leer = Standard):", "Wertspalte:", "Zeitzone:",
        "Zeitstempel bezeichnet Intervall:", "Einheit:",
        "CSV-Format", "Spalten und Zeit", "Vorschau", "Abbrechen", "Übernehmen",
        "Semikolon (;)", "Komma (,)", "Tabulator", "Punkt (.)",
        "Eine Zeitstempelspalte", "Getrennte Datums- und Uhrzeitspalten",
        "kWh je Intervall", "Spalte 1", "Anfang", "Ende"
    };

    [Fact]
    public void Unter_en_US_steht_kein_deutscher_Literaltext_mehr_in_der_Maske()
    {
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, new SpeicherImportDatei
            {
                Dateiname = "last.csv",
                Inhalt = Encoding.UTF8.GetBytes("Zeit;Last\n2026-01-01 00:00;1,5\n2026-01-01 00:15;2,0\n")
            })
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        string markup = cut.Markup;
        foreach (string alt in AlteLiterale)
            Assert.DoesNotContain(alt, markup);

        // Und die englischen Texte stehen da — Beschriftung, Gruppentitel, Knopf,
        // Spaltenvorlage und die drei Konventionen.
        Assert.Contains("Separator:", markup);
        Assert.Contains("Timestamp denotes interval:", markup);
        Assert.Contains("CSV format", markup);
        Assert.Contains("Apply", markup);
        Assert.Contains("Column 1", markup);
        Assert.Contains("detect automatically", markup);
        Assert.Contains("start of interval", markup);
        Assert.Contains("end of interval", markup);
    }
}
