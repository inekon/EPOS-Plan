using System.Linq;
using Bunit;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>Raster&lt;TZeile&gt; - die Hausklasse ueber QuickGrid.</summary>
public class RasterTests : BunitContext
{
    private sealed record Zeile(int Id, string Bezeichner);

    [Fact]
    public void Raster_zeigt_Kopfzeile_und_Zeilen()
    {
        // QuickGrid laedt beim ersten Zeichnen ein JS-Modul; im lockeren Modus
        // beantwortet bunit das, ohne dass der Test ein Modul stellen muss.
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = new[]
        {
            new Zeile(3, "Erdgas"),
            new Zeile(7, "Fernwaerme")
        }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.KindInhalt, (RenderFragment)(bau =>
            {
                bau.OpenComponent<PropertyColumn<Zeile, string>>(0);
                bau.AddComponentParameter(1, nameof(PropertyColumn<Zeile, string>.Property),
                                          (System.Linq.Expressions.Expression<Func<Zeile, string>>)(z => z.Bezeichner));
                bau.AddComponentParameter(2, nameof(PropertyColumn<Zeile, string>.Title), "Bezeichnung");
                bau.CloseComponent();
            })));

        Assert.Contains("epos-raster", cut.Find("table").ClassName);
        Assert.Contains("Bezeichnung", cut.Find("thead").TextContent);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Contains("Fernwaerme", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("epos-raster--bearbeitbar", cut.Find("table").ClassName);
    }

    // =====================================================================
    // Bearbeitbare Zellen (iU9-W3.0)
    // =====================================================================

    private sealed class Satz
    {
        internal bool Aktiv;
        internal double? Wert;
    }

    /// <summary>
    /// Ein Schalter und ein Zahlenfeld in TemplateColumns: Das Raster zeigt sie,
    /// und ihre Aenderung erreicht die Zeile - der Ersatz fuer die editierbaren
    /// Spalten des DataGridView.
    /// </summary>
    [Fact]
    public void Bearbeitbare_Zellen_melden_ihre_Aenderung_an_die_Zeile()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var satz = new Satz { Aktiv = false, Wert = 201.0 };
        var zeilen = new[] { satz }.AsQueryable();

        var cut = Render<Raster<Satz>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Bearbeitbar, true)
            .Add(x => x.KindInhalt, (RenderFragment)(bau =>
            {
                bau.OpenComponent<TemplateColumn<Satz>>(0);
                bau.AddComponentParameter(1, nameof(TemplateColumn<Satz>.Title), "aktiv");
                bau.AddComponentParameter(2, nameof(TemplateColumn<Satz>.ChildContent),
                    (RenderFragment<Satz>)(z => kind =>
                    {
                        kind.OpenComponent<Schalter>(0);
                        kind.AddComponentParameter(1, nameof(Schalter.Wert), z.Aktiv);
                        kind.AddComponentParameter(2, nameof(Schalter.WertChanged),
                            EventCallback.Factory.Create<bool>(this, b => z.Aktiv = b));
                        kind.CloseComponent();
                    }));
                bau.CloseComponent();

                bau.OpenComponent<TemplateColumn<Satz>>(3);
                bau.AddComponentParameter(4, nameof(TemplateColumn<Satz>.Title), "Wert");
                bau.AddComponentParameter(5, nameof(TemplateColumn<Satz>.ChildContent),
                    (RenderFragment<Satz>)(z => kind =>
                    {
                        kind.OpenComponent<Zahlenfeld>(0);
                        kind.AddComponentParameter(1, nameof(Zahlenfeld.Wert), z.Wert);
                        kind.AddComponentParameter(2, nameof(Zahlenfeld.WertChanged),
                            EventCallback.Factory.Create<double?>(this, w => z.Wert = w));
                        kind.CloseComponent();
                    }));
                bau.CloseComponent();
            })));

        Assert.Contains("epos-raster--bearbeitbar", cut.Find("table").ClassName);
        Assert.Equal(2, cut.FindAll("tbody td").Count);

        cut.Find("tbody td:first-child input").Change(true);
        Assert.True(satz.Aktiv);

        cut.Find("tbody td:last-child input").Input("55,5");
        Assert.Equal(55.5, satz.Wert);
    }

    // =====================================================================
    // Lange Listen (iU9-W13.0l)
    // =====================================================================

    /// <summary>
    /// Ohne <c>Virtualisiert</c> zeichnet QuickGrid JEDE Zeile - fuer die
    /// 20 746 Zeilen der CEC-Modulliste zu viel. Mit dem Schalter haelt es nur
    /// den sichtbaren Ausschnitt im Baum, und die Huelle bekommt die feste
    /// Hoehe, ohne die es nichts zu rollen gaebe.
    /// </summary>
    [Fact]
    public void Virtualisiert_zeichnet_nur_den_sichtbaren_Ausschnitt()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = Enumerable.Range(0, 2000)
                               .Select(i => new Zeile(i, "Modul " + i))
                               .AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Contains("epos-raster-huelle--hoch", cut.Find("div").ClassName);
        Assert.True(cut.FindAll("tbody tr").Count < 2000,
                    "Eine virtualisierte Liste darf nicht alle 2 000 Zeilen zeichnen.");
    }

    [Fact]
    public void Ohne_den_Schalter_bleibt_die_Huelle_die_gewohnte()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = new[] { new Zeile(1, "Erdgas") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal("epos-raster-huelle", cut.Find("div").ClassName);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    // =====================================================================
    // Eine neue Zeilenmenge bekommt ein neues Raster (Befund W6-B-2)
    // =====================================================================

    /// <summary>
    /// <b>Der Waechter zum Befund W6‑B‑2</b> (Windows 07.09.2026, „der Filter bei der
    /// Herstellerauswahl funktioniert nicht"): QuickGrid traegt den virtualisierten und
    /// den flachen Weg in EINER Instanz, und der flache Zwischenspeicher
    /// (<c>_currentNonVirtualizedViewItems</c>) wird nur EINMAL gefuellt — beim ersten
    /// Datenabruf, als das <c>@ref</c> auf das Virtualize-Kind noch <c>null</c> war,
    /// und ohne Pagination mit der GANZEN Liste. Faellt der Schalter danach auf
    /// <c>false</c> (2 343 Zeilen → 109 nach einem Herstellerfilter), zeichnet
    /// QuickGrid genau diesen uralten Stand.
    ///
    /// <para>Der Fix ist ein <c>@key</c> an (Schalter, Zeilenzahl). Geprueft wird
    /// deshalb die IDENTITAET der Rasterkomponente: Nach dem Wechsel muss es eine
    /// ANDERE Instanz sein — eine frische faengt mit leerem Zustand an.</para>
    /// </summary>
    [Fact]
    public void Der_Wechsel_des_Virtualisierungsschalters_baut_das_Raster_neu_auf()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var viele = Enumerable.Range(0, 2343)
                              .Select(i => new Zeile(i, "ABB " + i))
                              .AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, viele)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> vorher = cut.FindComponent<QuickGrid<Zeile>>().Instance;
        Assert.Equal((true, 2343), cut.Instance.Rasterstand);

        // Der Herstellerfilter: 2 343 -> 109, und damit faellt der Schalter.
        var wenige = Enumerable.Range(0, 109)
                               .Select(i => new Zeile(i, "SMA America " + i))
                               .AsQueryable();

        cut.Render(p => p
            .Add(x => x.Zeilen, wenige)
            .Add(x => x.Virtualisiert, false)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> nachher = cut.FindComponent<QuickGrid<Zeile>>().Instance;

        Assert.Equal((false, 109), cut.Instance.Rasterstand);
        Assert.NotSame(vorher, nachher);

        // ... und in der Tabelle stehen die NEUEN Zeilen, keine einzige alte.
        string tabelle = cut.Find("tbody").TextContent;
        Assert.Contains("SMA America", tabelle);
        Assert.DoesNotContain("ABB", tabelle);
    }

    /// <summary>
    /// Auch ohne Wechsel des Schalters bekommt eine andere Zeilenzahl ein neues
    /// Raster — so steht die Liste nach einem Filterwechsel wieder am ANFANG statt
    /// im Nirgendwo der alten Rollposition.
    /// </summary>
    [Fact]
    public void Eine_andere_Zeilenzahl_baut_das_Raster_neu_auf()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var drei = new[] { new Zeile(1, "a"), new Zeile(2, "b"), new Zeile(3, "c") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, drei)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> vorher = cut.FindComponent<QuickGrid<Zeile>>().Instance;

        cut.Render(p => p
            .Add(x => x.Zeilen, new[] { new Zeile(2, "b") }.AsQueryable())
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.NotSame(vorher, cut.FindComponent<QuickGrid<Zeile>>().Instance);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    /// <summary>
    /// <b>Und die Gegenprobe:</b> Bleiben Schalter und Zeilenzahl gleich, BLEIBT das
    /// Raster dieselbe Instanz. Das ist keine Feinheit — ein Raster mit
    /// Bedienelementen in den Zellen (<c>Bearbeitbar</c>) wuerde dem Anwender sonst
    /// mitten in der Eingabe unter den Fingern neu aufgebaut, und der Eingabepunkt
    /// waere weg. Deshalb haengt der Schluessel an der ZAHL und nicht an der
    /// Zeilenmenge: Beim Tippen in einer Zelle aendert sie sich nicht.
    /// </summary>
    [Fact]
    public void Dieselbe_Zeilenzahl_behaelt_die_Rasterinstanz()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var erst = new[] { new Zeile(1, "Erdgas"), new Zeile(2, "Heizoel") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, erst)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> vorher = cut.FindComponent<QuickGrid<Zeile>>().Instance;

        // Ein neuer AsQueryable-Aufruf ueber DIESELBEN Zeilen - genau das, was ein
        // Wirt bei jedem Render hereinreicht.
        cut.Render(p => p
            .Add(x => x.Zeilen, new[] { new Zeile(1, "Erdgas"), new Zeile(2, "Heizoel") }.AsQueryable())
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Same(vorher, cut.FindComponent<QuickGrid<Zeile>>().Instance);
    }

    // =====================================================================
    // Eine virtualisierte Liste braucht eine STABILE Menge (Befund W13-B-6)
    // =====================================================================

    /// <summary>
    /// Eine Zeilenmenge, die MITZÄHLT, wer sie wie anfasst — die Prüfhilfe der beiden
    /// Fälle zum Befund W13‑B‑6.
    ///
    /// <para><b>Sie trennt zwei Zugriffe, die sonst nicht zu unterscheiden sind.</b>
    /// <see cref="Raster{TZeile}"/> zählt seine Zeilen über <see cref="IEnumerable{T}"/>
    /// (<c>Zaehlen</c> → <c>Enumerable.Count</c>) und läuft damit über
    /// <see cref="GetEnumerator"/> DIESER Klasse — jeder solche Durchlauf ist eine
    /// Zählung des Rasters. QuickGrid dagegen fasst die Menge ausschliesslich als
    /// <see cref="IQueryable{T}"/> an (<c>Items.Count()</c>, <c>Skip</c>/<c>Take</c>);
    /// das läuft über <see cref="Provider"/> und <see cref="Expression"/> und damit
    /// über die INNERE Menge, nicht hier vorbei.</para>
    ///
    /// <para><b>Und genau deshalb misst sie ohne Uhr.</b> Die Vorfassung dieser beiden
    /// Fälle zählte Zeichenläufe innerhalb von QuickGrids Entprellung (100 ms) und
    /// verglich einen vor der Messstrecke genommenen Ausgangsstand. Beides hängt daran,
    /// dass QuickGrids ASYNCHRONE Ladung genau dann nichts tut — unter Parallellast tat
    /// sie doch etwas, und die Fälle fielen (zuletzt: erwartet 3 Durchläufe, gemessen
    /// 5). Was diese Prüfhilfe zählt, kann eine Ladung QuickGrids nicht verändern.</para>
    /// </summary>
    private sealed class Zaehlmenge : IQueryable<Zeile>
    {
        private readonly IQueryable<Zeile> _quelle;
        private int _zaehlungen;

        internal Zaehlmenge(int anzahl)
            => _quelle = Enumerable.Range(0, anzahl)
                                   .Select(i => new Zeile(i, "Speicher " + i))
                                   .ToList()
                                   .AsQueryable();

        /// <summary>Wie oft das Raster die Menge gezählt hat.</summary>
        internal int Zaehlungen => System.Threading.Volatile.Read(ref _zaehlungen);

        public System.Collections.Generic.IEnumerator<Zeile> GetEnumerator()
        {
            System.Threading.Interlocked.Increment(ref _zaehlungen);
            return _quelle.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public Type ElementType => _quelle.ElementType;
        public System.Linq.Expressions.Expression Expression => _quelle.Expression;
        public IQueryProvider Provider => _quelle.Provider;
    }

    /// <summary>Die Datenquelle, die das QuickGrid dieses Rasters gerade trägt.</summary>
    private static IQueryable<Zeile>? Datenquelle(IRenderedComponent<Raster<Zeile>> cut)
        => cut.FindComponent<QuickGrid<Zeile>>().Instance.Items;

    /// <summary>
    /// <b>Der Wächter zum Befund W13‑B‑6</b> (Windows-Abnahme 09.09.2026: „die Liste
    /// blinkt und ist nicht sichtbar", 6 654 Stromspeicher).
    ///
    /// <para><b>Was QuickGrid tut.</b> Es vergleicht seine Datenquelle nach REFERENZ
    /// (<c>OnParametersSetAsync</c>: <c>dataSourceHasChanged =
    /// _newItemsOrItemsProvider != _lastAssignedItemsOrProvider</c>). Ein frisches
    /// <c>AsQueryable()</c> über DERSELBEN Liste ist damit eine neue Menge; QuickGrid
    /// bricht die laufende Ladung ab (<c>_pendingDataLoadCancellationTokenSource
    /// ?.Cancel()</c>) und stellt hinter <c>await Task.Delay(100)</c> eine neue an.
    /// Solange eine Ladung offen ist, trägt die Tabelle die Klasse <c>loading</c>, und
    /// QuickGrids Stilblatt blendet damit den Körper auf <c>opacity: .25</c> ab — das
    /// gemeldete BLINKEN.</para>
    ///
    /// <para><b>Gemessen wird deshalb die URSACHE, nicht ihre Folge</b> (10.09.2026,
    /// W11b‑B‑26): ob die Datenquelle des QuickGrids je Zeichenlauf eine ANDERE
    /// Instanz ist. Der Vorfassung sah man an der Klasse <c>loading</c> nach, und zwar
    /// innerhalb von QuickGrids 100-ms-Entprellung — eine Messung über die Uhr, die
    /// unter Parallellast kippte. Die Referenzfrage stellt sich ohne Uhr, und sie ist
    /// genau die Frage, die QuickGrid selbst stellt.</para>
    ///
    /// <para>Der Fix sitzt im WIRT (<c>Katalogliste.razor</c>), nicht hier: Ein
    /// Raster, das eine neue Menge stillschweigend für die alte hielte, wäre die
    /// nächste Fehlerquelle. Dieser Fall hält beide Seiten fest — er ist zugleich der
    /// Wächter gegen ein QuickGrid, das seinen Vergleich eines Tages ändert.</para>
    /// </summary>
    [Fact]
    public void Eine_stabile_Zeilenmenge_laesst_die_virtualisierte_Liste_zur_Ruhe_kommen()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var quelle = Enumerable.Range(0, 6654).Select(i => new Zeile(i, "Speicher " + i)).ToList();

        // (A) Ein frisches AsQueryable je Zeichenlauf - der gemeldete Stand.
        var frisch = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, quelle.AsQueryable())
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        IQueryable<Zeile>? zuletzt = Datenquelle(frisch);
        int neue = 0;
        for (int i = 0; i < 10; i++)
        {
            frisch.Render(p => p
                .Add(x => x.Zeilen, quelle.AsQueryable())
                .Add(x => x.Virtualisiert, true)
                .Add(x => x.KindInhalt, Bezeichnerspalte()));

            IQueryable<Zeile>? jetzt = Datenquelle(frisch);
            if (!ReferenceEquals(zuletzt, jetzt)) neue++;
            zuletzt = jetzt;
        }
        Assert.Equal(10, neue);

        // (B) EINE Instanz ueber alle Zeichenlaeufe - der Stand nach dem Fix. Sie kommt
        // unveraendert am QuickGrid an, und das Raster selbst bleibt dieselbe Instanz:
        // Damit gibt es nichts abzubrechen und nichts neu anzustellen.
        var stabil = quelle.AsQueryable();
        var ruhig = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, stabil)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> raster = ruhig.FindComponent<QuickGrid<Zeile>>().Instance;

        for (int i = 0; i < 10; i++)
        {
            ruhig.Render(p => p
                .Add(x => x.Zeilen, stabil)
                .Add(x => x.Virtualisiert, true)
                .Add(x => x.KindInhalt, Bezeichnerspalte()));

            Assert.Same(raster, ruhig.FindComponent<QuickGrid<Zeile>>().Instance);
            Assert.Same(stabil, Datenquelle(ruhig));
        }
    }

    /// <summary>
    /// <b>Dieselbe Menge wird nur EINMAL gezählt</b> (Befund W13‑B‑6). Die Zeilenzahl
    /// steht im <c>@key</c> und wurde deshalb bei jedem Zeichenlauf geholt — und ein
    /// <c>AsQueryable()</c> über einer Liste ist keine <c>ICollection</c>, sein
    /// <c>Count()</c> läuft WIRKLICH über alle 6 654 Zeilen. Gemessen: vorher zehn
    /// volle Durchläufe für zehn Zeichenläufe, danach keiner.
    ///
    /// <para><b>Gezählt wird über die <see cref="Zaehlmenge"/></b> (10.09.2026,
    /// W11b‑B‑26) — sie sieht NUR die Zählungen des Rasters, nicht QuickGrids eigene
    /// Zugriffe. Die Vorfassung nahm ihren Ausgangsstand über alles und musste deshalb
    /// darauf hoffen, dass QuickGrid während der Messstrecke nichts mehr nachlädt; sie
    /// fiel unter Parallellast. Elf Zeichenläufe, eine Zählung — das ist die Aussage,
    /// und die hängt an keiner Uhr.</para>
    /// </summary>
    [Fact]
    public void Dieselbe_Zeilenmenge_wird_nur_einmal_gezaehlt()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var folge = new Zaehlmenge(6654);

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, folge)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal(1, folge.Zaehlungen);

        for (int i = 0; i < 10; i++)
            cut.Render(p => p
                .Add(x => x.Zeilen, folge)
                .Add(x => x.Virtualisiert, true)
                .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal(1, folge.Zaehlungen);
        Assert.Equal((true, 6654), cut.Instance.Rasterstand);

        // Und die Gegenprobe: Eine ANDERE Menge wird wieder gezaehlt - der
        // Zwischenspeicher haengt an der Referenz, nicht an der Zahl.
        var zweite = new Zaehlmenge(109);
        cut.Render(p => p
            .Add(x => x.Zeilen, zweite)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal(1, zweite.Zaehlungen);
        Assert.Equal(1, folge.Zaehlungen);
        Assert.Equal((true, 109), cut.Instance.Rasterstand);
    }

    // =====================================================================
    //  Das Zeilenmass wird GESETZT, nicht geraten (Befund #235)
    // =====================================================================

    /// <summary>
    /// <b>Dieselbe Zahl geht an <c>Virtualize</c> UND ins Stilblatt</b> (Befund
    /// <b>#235</b>, 12.09.2026 im Browser gemessen).
    ///
    /// <para>Virtualize misst nicht, es rechnet: Es teilt die Höhe des Rollbehälters
    /// durch <c>ItemSize</c> und setzt danach die Höhen seiner zwei Abstandshalter.
    /// Weicht das Maß von dem ab, was wirklich im Baum steht, kommen seine zwei
    /// Sichtbarkeitsmelder auf verschiedene Anfangszeilen und schieben das Fenster
    /// gegeneinander — gemessen alle 33 ms, endlos, mit dauerhaften Platzhalterzeilen.
    /// Deshalb reicht <c>Raster</c> den Wert nicht nur als <c>ItemSize</c> weiter,
    /// sondern legt ihn als <c>--epos-rasterzeile</c> an die Hülle; das Stilblatt gibt
    /// ihn jeder Zeile.</para>
    ///
    /// <para>Eine bunit-Probe misst keine Pixel (Lehre W6‑B‑1) — geprüft wird die
    /// VERBINDUNG der zwei Wege: dieselbe Zahl im Stil der Hülle und in
    /// <c>ItemSize</c>. Dass sie im Browser auch ankommt, prüft
    /// <c>Proben/Rasterprobe</c>.</para>
    /// </summary>
    [Fact]
    public void Die_virtualisierte_Huelle_gibt_das_Zeilenmass_ans_Stilblatt_weiter()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = Enumerable.Range(0, 2000)
                               .Select(i => new Zeile(i, "Modul " + i))
                               .AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal("--epos-rasterzeile: 53px", cut.Find("div").GetAttribute("style"));
        Assert.Equal(Raster<Zeile>.ZEILENHOEHE,
                     cut.FindComponent<QuickGrid<Zeile>>().Instance.ItemSize);

        // Ein eigenes Mass des Wirtes geht BEIDE Wege - sonst liefen sie auseinander.
        cut.Render(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.Zeilenhoehe, 61f)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal("--epos-rasterzeile: 61px", cut.Find("div").GetAttribute("style"));
        Assert.Equal(61f, cut.FindComponent<QuickGrid<Zeile>>().Instance.ItemSize);
    }

    /// <summary>
    /// <b>Nur die VIRTUALISIERTE Liste bekommt das feste Zeilenmass</b> (Befund
    /// <b>#235</b>). Eine kurze Liste zeichnet QuickGrid vollständig; dort gibt es
    /// keine Abstandshalter, nichts zu rechnen und damit auch keinen Grund, die Zeile
    /// höher zu machen, als ihr Inhalt sie braucht.
    /// </summary>
    [Fact]
    public void Ohne_Virtualisierung_bleibt_die_Huelle_ohne_Zeilenmass()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = new[] { new Zeile(1, "Erdgas") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Null(cut.Find("div").GetAttribute("style"));
    }

    /// <summary>
    /// <b>Das Stilblatt setzt das Maß wirklich</b> (Befund <b>#235</b>) — und zwar für
    /// BEIDE Zeilenarten des virtualisierten Rasters. Die Platzhalterzeile war der
    /// entscheidende Teil: In ihr steht kein Bedienelement, gemessen war sie 21,9 px
    /// hoch gegen 48,2 px der echten Zeile. Solange geladen wird, schrumpfte der
    /// gezeichnete Block damit auf 45 % dessen, was <c>Virtualize</c> annimmt.
    ///
    /// <para><b>Und die ZELLENPOLSTERUNG des Hauses gilt seit Auftrag #240 auch im
    /// QuickGrid</b> — ohne die Anhebung auf (0,2,4) gewänne dessen eigene (0,2,3)-Regel
    /// mit 0,1rem, und die echte Zeile fiele wieder auf 48,2 px unter das gesetzte Maß.
    /// Die Höhe eines <c>tr</c> ist ein Mindestmaß, die Polsterung nicht.</para>
    /// </summary>
    [Fact]
    public void Das_Stilblatt_setzt_das_Zeilenmass_der_virtualisierten_Liste()
    {
        string blatt = Stilblatt().Replace("\r\n", "\n");

        Assert.Contains(
            ".epos-raster-huelle--hoch .epos-raster > tbody > tr,\n" +
            ".epos-raster-huelle--hoch .epos-raster > tbody > tr > td {\n" +
            "    height: var(--epos-rasterzeile, 53px);\n}",
            blatt);

        // Der Rueckfall im Blatt und die Vorgabe im Programm sind DIESELBE Zahl.
        Assert.Equal(53f, Raster<Zeile>.ZEILENHOEHE);

        // Auftrag #240: die Hausregel gegen QuickGrids eigene Zellenpolsterung -
        // und daneben der Rueckweg fuer die BEARBEITBARE Zeile, die eng bleiben muss.
        Assert.Contains("table.epos-raster.quickgrid > tbody > tr > td {\n    padding: 4px 8px;\n}",
                        blatt);
        Assert.Contains("table.epos-raster--bearbeitbar.quickgrid > tbody > tr > td {\n    padding: 0 8px;\n}",
                        blatt);
    }

    /// <summary>Das Stilblatt der Bibliothek, aus dem Quellbaum gelesen.</summary>
    private static string Stilblatt()
    {
        var d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
        {
            string kandidat = System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css");
            if (System.IO.File.Exists(kandidat)) return System.IO.File.ReadAllText(kandidat);
        }

        Assert.Fail("epos-ui.css wurde nicht gefunden.");
        return "";
    }

    private static RenderFragment Bezeichnerspalte() => bau =>
    {
        bau.OpenComponent<PropertyColumn<Zeile, string>>(0);
        bau.AddComponentParameter(1, nameof(PropertyColumn<Zeile, string>.Property),
                                  (System.Linq.Expressions.Expression<Func<Zeile, string>>)(z => z.Bezeichner));
        bau.AddComponentParameter(2, nameof(PropertyColumn<Zeile, string>.Title), "Bezeichnung");
        bau.CloseComponent();
    };
}
