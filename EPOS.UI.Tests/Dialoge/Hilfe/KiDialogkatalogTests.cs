using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using KiKern;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Der WÄCHTER über den Dialogkatalog (Auftrag #200, Stufe S2).
///
/// <para><b>Warum er hier steht und nicht in <c>EPOS.Kern.Tests</c>.</b> Der Katalog
/// liegt im Kern, die Daten-Objekte liegen in <c>EPOS.UI</c> — und geprüft wird gerade
/// ihr Zusammenpassen. Ein Wächter im Kern könnte die Typen gar nicht sehen.</para>
///
/// <para><b>Was er verhindert.</b> Der zweite Parameter jeder <c>KiDialogFeld</c> ist
/// seit diesem Auftrag der Name einer EIGENSCHAFT
/// (<c>HeizkesselKatalogDaten.Ptherm</c>), aufgelöst per Reflection. Ein Tippfehler
/// darin bricht nichts — das Feld wird still nicht angemeldet und fehlt im Feldblock.
/// Genau deshalb muss ihn ein Zeuge nennen.</para>
///
/// <para><b>ZWEI Wächter, zwei Fragen (15.09.2026).</b>
/// <see cref="Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt"/> fragt, ob
/// sich der Pfad AUFLÖSEN lässt;
/// <see cref="Jeder_Feldpfad_steht_im_Markup_seiner_Maske"/> fragt, ob der Anwender das
/// Feld auch SIEHT. Die zweite Frage kam dazu, weil der Heizkesseleditor seine Kosten-
/// und Emissionsgruppen verlor und der Katalog die neun Felder trotzdem weiterführte:
/// Die Eigenschaften gibt es am DTO noch — die Hülle liest und schreibt die Spalten —,
/// nur zeigt die Maske sie nicht mehr. Der erste Wächter blieb dabei grün.</para>
/// </summary>
public class KiDialogkatalogTests
{
    /// <summary>Die Masken und ihre Daten-Objekte — die EINE Zuordnungstabelle.</summary>
    /// <remarks>
    /// <b>Ein Daten-Objekt darf MEHRERE Masken tragen.</b> Die Erzeugermasken des
    /// Projekts (Welle KI‑F1) melden alle dieselbe <c>ErzeugerZeile</c> an — die
    /// gewählte Zeile ihrer Projektliste; welche Felder daran hängen, sagt der
    /// Katalogeintrag und nicht der Typ.
    /// </remarks>
    public static TheoryData<string, Type> Masken() => new()
    {
        { KiMaskennamen.HEIZKESSEL,              typeof(HeizkesselKatalogDaten) },
        { KiMaskennamen.PHOTOVOLTAIK,            typeof(ErzeugerZeile) },
        { KiMaskennamen.PUFFERSPEICHER,          typeof(PufferSpKatalogDaten) },
        { KiMaskennamen.WAERMEPUMPE,             typeof(WaermepumpeStammDaten) },
        { KiMaskennamen.STROMSPEICHER_AUSLEGUNG, typeof(StromspeicherKiSicht) },

        // Auftrag #221 (KI-D-E-1): die sechste Maske — die Ansicht „Simulation".
        // Voll ausgeschrieben: EPOS.UI.Seiten.Simulation fuehrt eine ZWEITE
        // ErzeugerZeile, und ein using darauf machte die Zeile darueber mehrdeutig.
        { KiMaskennamen.SIMULATION,
          typeof(EPOS.UI.Seiten.Simulation.SimulationKiSicht) },

        // 14.09.2026: die siebte Maske — und die erste mit einem RASTER. Ihre Felder
        // sind zum Teil SPALTEN (KostenKomponenteStand.Zeilen[].Nutzungsdauer); der
        // Waechter unten loest sie ueber KiMaskenanmeldung.Pruefe mit auf.
        { KiMaskennamen.KOSTENVERWALTUNG,
          typeof(EPOS.UI.Dialoge.Kosten.KostenKomponenteStand) },

        // Welle KI-F1: die Erzeugermasken des PROJEKTS. Sie melden die GEWAEHLTE
        // Zeile ihrer Projektliste an - denselben Typ wie Form_PV.
        { KiMaskennamen.HEIZKESSEL_PROJEKT,     typeof(ErzeugerZeile) },
        { KiMaskennamen.BHKW_PROJEKT,           typeof(ErzeugerZeile) },
        { KiMaskennamen.PUFFERSPEICHER_PROJEKT, typeof(ErzeugerZeile) },
        { KiMaskennamen.STROMSPEICHER_PROJEKT,  typeof(ErzeugerZeile) },

        // Die Solarkollektoren melden den ARBEITSSTAND der Kollektorgruppe an und
        // nicht die Zeile: Ihre fuenf Zahlen gehen erst mit „Uebernehmen" dorthin.
        { KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT,
          typeof(EPOS.UI.Dialoge.Solarthermie.SolarkollektorenEingaben) },

        // Die Waermepumpen-ANLAGE - ein Feldsatz fuer alle drei Bloecke der Maske.
        { KiMaskennamen.WAERMEPUMPE_ANLAGE, typeof(WaermepumpeAnlageDaten) },

        // Welle KI-F2: die Masken der SIMULATIONSKONFIGURATION. Sie melden je eine
        // SICHTKLASSE an - siehe OhneMarkupprobe.
        { KiMaskennamen.PUFFERSPEICHER_VERWALTUNG,
          typeof(EPOS.UI.Dialoge.Simulation.PufferSpProjektKiSicht) },
        { KiMaskennamen.QUELLE_ERDREICH,
          typeof(EPOS.UI.Dialoge.Simulation.QuelleErdreichKiSicht) },
        { KiMaskennamen.QUELLE_PUFFERSPEICHER,
          typeof(EPOS.UI.Dialoge.Simulation.QuellePufferspeicherKiSicht) },
        { KiMaskennamen.QUELLPROFIL,
          typeof(EPOS.UI.Dialoge.Simulation.QuellprofilKiSicht) },
        { KiMaskennamen.WAERMESENKE,
          typeof(EPOS.UI.Dialoge.Simulation.WaermesenkeKiSicht) },
        { KiMaskennamen.KOMPONENTENKONFIGURATION,
          typeof(EPOS.UI.Dialoge.Simulation.KomponentenKonfigurationKiSicht) },

        // Welle KI-F3: die Masken des BEDARFS. Sie melden je eine SICHTKLASSE an -
        // siehe OhneMarkupprobe.
        { KiMaskennamen.GEBAEUDE,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeKiSicht) },
        { KiMaskennamen.GEBAEUDE_WOHNFLAECHE,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeWohnflaecheKiSicht) },
        { KiMaskennamen.GEBAEUDE_KATALOG,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogKiSicht) }
    };

    /// <summary>
    /// Je Maske die WAHLFELDER, deren Einträge der Dialog beim Anmelden ausdrücklich
    /// hereinreicht (KI-F1b, KI-D-Q6).
    /// </summary>
    /// <remarks>
    /// <b>Diese Liste ist die Gegenprobe zum Dialog.</b> Ein Wahlfeld löst seine Quelle
    /// entweder über die Begleiteigenschaft <c>&lt;Eigenschaft&gt;Wahl</c> am Daten-Objekt
    /// auf — das findet der Wächter selbst — oder über einen Lieferanten im Dialog, und
    /// den kann er nicht sehen. Steht ein Feld hier, das der Dialog NICHT liefert, bleibt
    /// es im Betrieb ohne Auswahl; steht eines nicht hier, das er liefert, meldet der
    /// Wächter es als fehlend. Beides fällt auf.
    /// </remarks>
    private static string[] Wahlquellen(string maske) => maske switch
    {
        KiMaskennamen.HEIZKESSEL           => new[] { "energietraeger" },
        KiMaskennamen.PUFFERSPEICHER       => new[] { "speichertyp" },
        KiMaskennamen.WAERMEPUMPE          => new[] { "typ", "leistungsstufen",
                                                      "aufstellung", "baujahr" },
        KiMaskennamen.HEIZKESSEL_PROJEKT   => new[] { "energietraeger" },
        KiMaskennamen.BHKW_PROJEKT         => new[] { "energietraeger" },
        KiMaskennamen.STROMSPEICHER_PROJEKT => new[] { "energietraeger" },
        KiMaskennamen.PHOTOVOLTAIK         => new[] { "energietraeger" },
        KiMaskennamen.WAERMEPUMPE_ANLAGE   => new[] { "energietraeger", "betriebsart", "typ",
                                                      "leistungsstufen", "aufstellung", "baujahr" },

        // Die sechs Masken der SIMULATIONSKONFIGURATION stehen hier bewusst NICHT:
        // Sie melden je eine Sichtklasse an, und die traegt zu jedem Wahlfeld ihre
        // Begleiteigenschaft <Eigenschaft>Wahl - den Weg findet der Waechter selbst.
        // Wer eines ihrer Felder hier eintruege, naehme ihm genau diese Probe.
        _ => Array.Empty<string>()
    };

    // =====================================================================
    //  Der Wächter
    // =====================================================================

    [Theory]
    [MemberData(nameof(Masken))]
    public void Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt(string maske, Type daten)
    {
        IReadOnlyList<string> fehlt = KiMaskenanmeldung.Pruefe(maske, daten, Wahlquellen(maske));

        Assert.True(fehlt.Count == 0,
                    "Diese Eigenschaftspfade der Maske '" + maske + "' lösen an " +
                    daten.Name + " nicht auf: " + string.Join(", ", fehlt));
    }

    [Fact]
    public void Die_Gegenprobe_ein_falsches_Daten_Objekt_faellt_auf()
    {
        // Ohne die Typprobe VOR dem Punkt fiele ein an die falsche Maske gehängtes
        // Daten-Objekt erst auf, wenn zufällig eine Eigenschaft gleich heisst.
        IReadOnlyList<string> fehlt =
            KiMaskenanmeldung.Pruefe(KiMaskennamen.HEIZKESSEL, typeof(PufferSpKatalogDaten),
                                     Wahlquellen(KiMaskennamen.HEIZKESSEL));

        // ELF seit der Welle KI-F1b: die sechs Zahlen des Katalogeditors und die fuenf
        // uebrigen Eingabefelder (Name, Hersteller, Beschreibung, Energietraeger,
        // Brennwert) - keines davon gibt es an PufferSpKatalogDaten.
        Assert.Equal(11, fehlt.Count);
        Assert.Contains("HeizkesselKatalogDaten.Ptherm", fehlt);
    }

    [Fact]
    public void Eine_unbekannte_Maske_wird_benannt_und_nicht_verschwiegen()
    {
        IReadOnlyList<string> fehlt =
            KiMaskenanmeldung.Pruefe("Form_GibtEsNicht", typeof(HeizkesselKatalogDaten));

        Assert.Single(fehlt);
        Assert.Contains("Form_GibtEsNicht", fehlt[0], StringComparison.Ordinal);
    }

    // =====================================================================
    //  Der Katalog selbst
    // =====================================================================

    [Fact]
    public void Der_Katalog_fuehrt_zweiundzwanzig_Masken()
    {
        KiDialogKatalog katalog = KiDialoge.Katalog;

        Assert.Equal(22, katalog.Anzahl);
        foreach (object[] zeile in Masken())
            Assert.True(katalog.Kennt((string)zeile[0]), (string)zeile[0]);
    }

    /// <summary>
    /// <b>Jede Katalogmaske hat ein Öffnungsziel, und die Projektmasken teilen sich
    /// eines</b> — die STARTSEITE, von deren Erzeugerkarte aus sie aufgehen (Welle
    /// KI‑F1).
    /// </summary>
    /// <remarks>
    /// <b>Der Wächter über die zwei Fundstellen.</b> <c>KiMaskenziele.STARTSEITE</c>
    /// steht im Kern als Zeichenkette, weil der Kern die Oberfläche nicht kennt;
    /// <c>Seitenschluessel.Startseite</c> steht in <c>EPOS.UI</c>. Laufen beide
    /// auseinander, führt <c>dialog_oeffnen</c> ins Leere — dasselbe Muster, mit dem
    /// die Stromspeicher-Ansicht zusammengehalten wird.
    /// </remarks>
    [Fact]
    public void Das_Ziel_der_Projektmasken_ist_der_Seitenschluessel_der_Startseite()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Startseite, KiMaskenziele.STARTSEITE);

        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.HEIZKESSEL_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.BHKW_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.STROMSPEICHER_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.WAERMEPUMPE_ANLAGE));
    }

    /// <summary>
    /// <b>Die Masken der Simulationskonfiguration führen auf die ANSICHT, aus der sie
    /// aufgehen</b> (Welle KI‑F2).
    /// </summary>
    /// <remarks>
    /// Sie brauchen alle eine gewählte Komponente; kontextfrei öffnen lässt sich keine.
    /// <c>Masken.Simulation</c> kennt die Windows-Navigationstabelle, und dieselbe
    /// Zeichenkette ist der Seitenschlüssel der <c>AppWurzel</c>
    /// (<c>Seitenschluessel.Simulation</c> ist auf die Konstante des Kerns gesetzt) —
    /// hier führt ein Ziel also wirklich irgendwohin, anders als bei der
    /// Kostenverwaltung.
    /// </remarks>
    [Fact]
    public void Das_Ziel_der_Simulationsmasken_ist_die_Ansicht_Simulation()
    {
        // Voll ausgeschrieben: Die Testklasse fuehrt selbst eine Masken()-Methode,
        // und die verdeckt den Typnamen.
        string ziel = WindowsFormsApplication1.Masken.Simulation;

        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER_VERWALTUNG));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.QUELLE_ERDREICH));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.QUELLE_PUFFERSPEICHER));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.QUELLPROFIL));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.WAERMESENKE));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.KOMPONENTENKONFIGURATION));
    }

    /// <summary>
    /// <b>Die Gebäudemaske IST die Gebäudeverwaltung</b> (Welle KI‑F3):
    /// <c>Masken.GebaeudeAdmin</c> öffnet dieselbe Razor-Komponente in der Betriebsart
    /// Admin, und der Katalogeditor geht aus ihr auf.
    /// </summary>
    /// <remarks>
    /// Die Wohn-/Nutzflächenangabe dagegen hängt an einer gewählten Projektzeile und
    /// geht über den Knopf „Ändern…" auf; ihr Ziel ist deshalb die Startseite — dieselbe
    /// Begründung wie bei den Erzeugermasken des Projekts.
    /// </remarks>
    [Fact]
    public void Das_Ziel_der_Gebaeudemasken_ist_die_Gebaeudeverwaltung()
    {
        string ziel = WindowsFormsApplication1.Masken.GebaeudeAdmin;

        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE_KATALOG));

        Assert.Equal(KiMaskenziele.STARTSEITE,
                     KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE_WOHNFLAECHE));
    }

    [Fact]
    public void Kein_Feld_traegt_mehr_einen_WinForms_Controlnamen()
    {
        // Der Beleg für die Umstellung: Ein Controlname ist EINE Stufe
        // ("tb_th_Leistung") und läuft damit gegen die Zwei-Stufen-Regel des
        // Eigenschaftspfades. Geprüft wird trotzdem am Präfix — er ist das, was ein
        // Rückfall in die alte Schreibweise zuerst wieder mitbrächte.
        var funde = new List<string>();

        foreach (KiDialog d in KiDialoge.Katalog)
            foreach (KiDialogFeld f in d.Felder)
            {
                Assert.True(KiEigenschaftspfad.IstGueltig(f.Eigenschaftspfad), f.Eigenschaftspfad);

                if (f.Eigenschaftspfad.StartsWith("tb_", StringComparison.Ordinal) ||
                    f.Eigenschaftspfad.StartsWith("textBox", StringComparison.Ordinal) ||
                    f.Eigenschaftspfad.StartsWith("gb_", StringComparison.Ordinal))
                    funde.Add(d.Maskenname + "." + f.Name + " → " + f.Eigenschaftspfad);
            }

        Assert.True(funde.Count == 0, string.Join("; ", funde));
    }

    [Fact]
    public void Keine_Maske_traegt_mehr_eine_Knopfposition()
    {
        // Sie waren gemessene Koordinaten im Client-Bereich gefallener WinForms-Masken;
        // den Aufrufknopf zeichnet seit iU9-W15b.5 der Baustein KiKnopf im Dialogkopf.
        foreach (KiDialog d in KiDialoge.Katalog)
            Assert.False(d.HatKnopfposition, d.Maskenname);
    }

    [Fact]
    public void Die_vier_Startmasken_fuehren_11_15_5_und_11_Felder()
    {
        // Der Feldumfang ist mit #200 NICHT gewachsen — sonst liesse sich hinterher
        // nicht sagen, was den Feldblock verändert hat: der Umfang oder der
        // Auflösungsweg (Fachkonzept 11.6).
        //
        // GESCHRUMPFT ist er am 15.09.2026, und zwar allein beim Heizkessel: von 15 auf
        // 6. Mit dem Anwenderentscheid „Der Dialog über Button Bearbeiten soll keine
        // Kosten und Emissionen enthalten" hat die Maske neun Felder verloren; der
        // Katalog führt sie deshalb auch nicht mehr (siehe
        // Die_Heizkesselmaske_fuehrt_nur_noch_die_sechs_sichtbaren_Felder).
        Assert.Equal(11, KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL)!.Felder.Count);
        Assert.Equal(15, KiDialoge.Katalog.Finde(KiMaskennamen.PHOTOVOLTAIK)!.Felder.Count);
        Assert.Equal(5, KiDialoge.Katalog.Finde(KiMaskennamen.PUFFERSPEICHER)!.Felder.Count);
        Assert.Equal(11, KiDialoge.Katalog.Finde(KiMaskennamen.WAERMEPUMPE)!.Felder.Count);
    }

    /// <summary>
    /// <b>Die Photovoltaik führt ELF Felder mehr als die drei der Startmaske</b> (Welle
    /// KI‑F1): die drei Modellfelder samt der Wechselrichterwahl und die SIEBEN Spalten
    /// der Strangliste.
    /// </summary>
    /// <remarks>
    /// Die Strangfelder sind SPALTEN (<c>ErzeugerZeile.Straenge[].Mppt</c>): Je
    /// vorhandener Strangzeile wird daraus ein gewöhnliches Feld, und das
    /// Zeilenkennzeichen ist der Bezeichner des Strangs. Ohne diesen Fall bliebe
    /// unbemerkt, wenn eines der sieben wieder zu einem flachen Feld würde.
    /// </remarks>
    [Fact]
    public void Die_Photovoltaikmaske_fuehrt_die_Modellfelder_und_sieben_Strangspalten()
    {
        KiDialog pv = KiDialoge.Katalog.Finde(KiMaskennamen.PHOTOVOLTAIK)!;

        foreach (string name in new[] { "modell_erweitert", "wr_wirkungsgrad",
                                        "systemverluste", "mit_wechselrichter" })
        {
            KiDialogFeld feld = pv.FindeFeld(name)!;
            Assert.NotNull(feld);
            Assert.False(feld.IstSpalte, name);
        }

        string[] spalten =
        {
            "strang", "strang_geraet", "strang_mppt", "strang_module_reihe",
            "strang_parallel", "strang_neigung", "strang_azimut"
        };

        foreach (string name in spalten)
        {
            KiDialogFeld feld = pv.FindeFeld(name)!;
            Assert.NotNull(feld);
            Assert.True(feld.IstSpalte, name);
            Assert.Equal("Straenge", feld.Sammlung);
            Assert.Equal("Bezeichner", feld.Zeilenkennzeichen);
        }

        // Die Anlagenwerte des Wechselrichters stehen in einer eigenen Ueberlagerung
        // und bleiben deshalb draussen (Fachkonzept 11.6).
        foreach (string weg in new[] { "wr_nennleistung", "wr_eta10", "wr_eta50", "wr_eta100" })
            Assert.False(pv.KenntFeld(weg), weg);
    }

    /// <summary>
    /// <b>Was der Heizkesseleditor am 15.09.2026 verloren hat — namentlich.</b>
    ///
    /// <para>Neun Felder: <c>investitionskosten</c>, <c>wartungskosten</c>,
    /// <c>raumbedarf</c>, <c>nutzungsdauer</c>, <c>co2</c>, <c>so2</c>, <c>nox</c>,
    /// <c>co</c>, <c>staub</c>. Der Zählfall oben sagt nur, dass es sechs SIND; dieser
    /// sagt, WELCHE — und dass keines der neun auf einem Umweg zurückkommt.</para>
    /// </summary>
    [Fact]
    public void Die_Heizkesselmaske_fuehrt_genau_ihre_sichtbaren_Felder()
    {
        KiDialog hk = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL)!;

        string[] erwartet =
        {
            "th_leistung", "wirkungsgrad_gas", "wirkungsgrad_oel",
            "bereitschaftsverlust", "vorlauf", "ruecklauf",
            // Welle KI-F1b: die uebrigen Eingabefelder derselben Maske.
            "name", "hersteller", "beschreibung", "energietraeger", "brennwert"
        };
        Assert.Equal(erwartet.OrderBy(x => x, StringComparer.Ordinal),
                     hk.Felder.Select(f => f.Name).OrderBy(x => x, StringComparer.Ordinal));

        foreach (string weg in new[]
                 {
                     "investitionskosten", "wartungskosten", "raumbedarf", "nutzungsdauer",
                     "co2", "so2", "nox", "co", "staub"
                 })
            Assert.False(hk.KenntFeld(weg), weg);
    }

    // =====================================================================
    //  Der zweite Wächter: steht das Feld auch WIRKLICH auf der Maske?
    // =====================================================================

    /// <summary>
    /// Maskenname → Razor-Datei(en), repo-relativ. Die EINE Zuordnungstabelle für die
    /// Markup-Probe (Auftrag vom 15.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>Sie ist bewusst getrennt von <see cref="Masken"/>: Dort steht das
    /// DATEN-OBJEKT (was der Dialog anmeldet), hier die DATEI (was der Anwender
    /// sieht). Bei der Simulations- und der Stromspeicher-Ansicht fallen beide
    /// auseinander — siehe <see cref="OhneMarkupprobe"/>.</para>
    ///
    /// <para><b>Eine Maske darf aus MEHREREN Dateien bestehen</b> (mit <c>;</c>
    /// getrennt): Wandert das Feldraster eines Dialogs in einen eigenen Baustein, weil
    /// ein zweiter Wirt dieselben Felder zeigt, steht das Feld weiterhin vor dem
    /// Anwender — nur eben in der Kinddatei. Für <c>Form_WP</c> ist das seit #297 so:
    /// <c>WaermepumpeStammFelder</c> trägt das Raster, und der Anlagendialog der
    /// Wärmepumpe bettet es genauso ein wie die Stammdatenpflege.</para>
    /// </remarks>
    public static TheoryData<string, string> Markupdateien() => new()
    {
        { KiMaskennamen.HEIZKESSEL,       "EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor" },
        // DREI Dateien (Welle KI-F1): Der Projektdialog zeichnet Neigung, Azimut und
        // Modulzahl selbst, die Modellfelder und die Straenge stehen in Bausteinen.
        { KiMaskennamen.PHOTOVOLTAIK,     "EPOS.UI/Dialoge/Erzeuger/PhotovoltaikDialog.razor;" +
                                          "EPOS.UI/Dialoge/Erzeuger/PvModellFelder.razor;" +
                                          "EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor" },
        { KiMaskennamen.PUFFERSPEICHER,   "EPOS.UI/Dialoge/Erzeuger/PufferSpKatalogDialog.razor" },
        { KiMaskennamen.WAERMEPUMPE,      "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammDialog.razor;" +
                                          "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammFelder.razor" },
        { KiMaskennamen.KOSTENVERWALTUNG, "EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor" },

        // Welle KI-F1: die Erzeugermasken des PROJEKTS.
        { KiMaskennamen.HEIZKESSEL_PROJEKT,     "EPOS.UI/Dialoge/Erzeuger/HeizkesselDialog.razor" },
        { KiMaskennamen.BHKW_PROJEKT,           "EPOS.UI/Dialoge/Erzeuger/BhkwDialog.razor" },
        { KiMaskennamen.PUFFERSPEICHER_PROJEKT, "EPOS.UI/Dialoge/Erzeuger/PufferspeicherDialog.razor" },
        { KiMaskennamen.STROMSPEICHER_PROJEKT,  "EPOS.UI/Dialoge/Erzeuger/StromspeicherDialog.razor" },
        { KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT,
          "EPOS.UI/Dialoge/Solarthermie/SolarkollektorenDialog.razor" },

        // DREI Dateien: Der Anlagendialog zeichnet die Auslegung selbst und bettet
        // die Konfiguration und den Stammfeldblock ein - jedes Feld steht damit vor
        // dem Anwender, nur eben teils in einer Kinddatei.
        { KiMaskennamen.WAERMEPUMPE_ANLAGE,
          "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeAnlageDialog.razor;" +
          "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeKonfiguration.razor;" +
          "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammFelder.razor" }
    };

    /// <summary>
    /// Die Masken OHNE Markup-Probe — mit Grund, je eine Zeile.
    /// </summary>
    /// <remarks>
    /// <b>Beide binden über eine SICHTKLASSE.</b> <c>StromspeicherKiSicht</c> und
    /// <c>SimulationKiSicht</c> sind flache Sichtmodelle, die die Kette zum lebenden
    /// Stand EINMAL an einer benannten Stelle auflösen
    /// (<c>KiEigenschaftspfad</c>-Kommentar: „Wo eine Maske echte Tiefe braucht,
    /// bekommt sie ein flaches SICHTMODELL"). Ihre Eigenschaftsnamen
    /// (<c>KapazitaetGesamtKWh</c>, <c>WaermedeckungProzent</c>) stehen deshalb NICHT
    /// im Markup — die Ansicht zeigt dieselben Zahlen aus ihrem eigenen Stand. Für
    /// beide hält die Sichtklasse selbst den Zeugen (<c>KiDialogkatalogTests</c>
    /// weiter unten, <c>KiSimulationMaskeTests</c>): Dort wird gerechnet, ob die
    /// Sicht die Werte der Ansicht trägt, und das ist die schärfere Probe.
    /// </remarks>
    /// <remarks>
    /// <b>Die Masken der Simulationskonfiguration kommen mit der Welle KI‑F2 dazu —
    /// aus demselben Grund und mit demselben Ersatz.</b> Ihre Dialoge führen ihren
    /// Arbeitsstand nicht als veränderliches DTO, sondern in den Eingabefeldern der
    /// Maske; was sie hereinbekommen und herausgeben, sind unveränderliche Records
    /// (<c>init</c>-Eigenschaften, neu erzeugt per <c>with</c>). Ein daran angemeldeter
    /// Katalog zeigte den Stand von vorhin und setzte ins Leere. Jede dieser Masken
    /// bindet deshalb über eine Sichtklasse auf die LEBENDEN Felder — und jede hält
    /// ihren Zeugen in der Testklasse ihres Dialogs: Dort steht die Maske gezeichnet
    /// an der Brücke, und ein Feld wird gelesen UND gesetzt. Das ist die schärfere
    /// Probe, denn sie misst den ganzen Weg statt einer Zeichenkette im Markup.
    /// </remarks>
    private static readonly Dictionary<string, string> OhneMarkupprobe = new()
    {
        [KiMaskennamen.STROMSPEICHER_AUSLEGUNG] =
            "bindet über die Sichtklasse StromspeicherKiSicht, nicht über das Markup",
        [KiMaskennamen.SIMULATION] =
            "bindet über die Sichtklasse SimulationKiSicht, nicht über das Markup",
        [KiMaskennamen.PUFFERSPEICHER_VERWALTUNG] =
            "bindet über die Sichtklasse PufferSpProjektKiSicht auf die Eingabefelder " +
            "der Maske; Zeuge ist PufferSpProjektDialogTests",
        [KiMaskennamen.QUELLE_ERDREICH] =
            "bindet über die Sichtklasse QuelleErdreichKiSicht auf die Eingabefelder " +
            "der Maske; Zeuge ist QuelleErdreichDialogTests",
        [KiMaskennamen.QUELLE_PUFFERSPEICHER] =
            "bindet über die Sichtklasse QuellePufferspeicherKiSicht auf die " +
            "Eingabefelder der Maske; Zeuge ist QuellePufferspeicherDialogTests",
        [KiMaskennamen.QUELLPROFIL] =
            "bindet über die Sichtklasse QuellprofilKiSicht auf die Kopffelder der " +
            "Maske; Zeuge ist QuellprofilDialogTests",
        [KiMaskennamen.WAERMESENKE] =
            "bindet über die Sichtklasse WaermesenkeKiSicht auf die Bedienelemente " +
            "der gewählten Zeile; Zeuge ist WaermesenkeDialogTests",
        [KiMaskennamen.KOMPONENTENKONFIGURATION] =
            "bindet über die Sichtklasse KomponentenKonfigurationKiSicht auf ZWEI " +
            "Arbeitskopien; Zeuge ist KomponentenKonfigurationDialogTests",
        [KiMaskennamen.GEBAEUDE] =
            "bindet über die Sichtklasse GebaeudeKiSicht auf die Filterfelder und den " +
            "Detailblock der Maske; Zeuge ist GebaeudeDialogTests",
        [KiMaskennamen.GEBAEUDE_WOHNFLAECHE] =
            "bindet über die Sichtklasse GebaeudeWohnflaecheKiSicht auf die lebenden " +
            "Eingabefelder; Zeuge ist GebaeudeWohnflaecheDialogTests",
        [KiMaskennamen.GEBAEUDE_KATALOG] =
            "bindet über die Sichtklasse GebaeudeKatalogKiSicht auf BEIDE Reiterblätter; " +
            "Zeuge ist GebaeudeKatalogDialogTests"
    };

    /// <summary>
    /// <b>Ein Katalogfeld, das auf der Maske nicht steht, ist eine stille Setzung.</b>
    ///
    /// <para><b>Der Befund (15.09.2026).</b> Der Heizkesseleditor verlor die Gruppen
    /// „Kosten", „Emissionen nach BEHG-V" und „Emissionsfaktoren"; der Dialogkatalog
    /// führte die neun Felder weiter. Der bisherige Wächter
    /// (<see cref="Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt"/>) blieb
    /// dabei GRÜN: Die Eigenschaften gibt es am DTO ja noch — die Hülle liest und
    /// schreibt die Spalten weiterhin. Grün heißt hier also nur „auflösbar", nicht
    /// „sichtbar". Der Assistent hätte angeboten, eine Zahl zu setzen, die der Anwender
    /// in der offenen Maske nirgends nachlesen kann.</para>
    ///
    /// <para><b>Die Regel.</b> Der Eigenschaftsname — bei einer Spalte
    /// (<c>Stand.Zeilen[].Nutzungsdauer</c>) der Teil NACH dem <c>[]</c> — muss im
    /// Markup MIT FÜHRENDEM PUNKT vorkommen: <c>Daten.Ptherm</c>,
    /// <c>_projektZeile.Neigung</c>, <c>zeile.Nutzungsdauer</c>. Der Punkt ist das
    /// Entscheidende: Er trennt eine BINDUNG von einer gleichlautenden Zeichenkette —
    /// <c>"Investitionskosten…"</c> als Knopftext oder <c>LabelInvestKurz</c> als
    /// Parametername treffen die Regel nicht. Kommentarzeilen zählen nicht mit
    /// (dieselbe Ausnahme wie bei den zwei <c>git grep</c>-Wächtern des Kerns);
    /// sonst hielte ein „HIER STAND Daten.CO2" die entfernte Deklaration am Leben.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Markupdateien))]
    public void Jeder_Feldpfad_steht_im_Markup_seiner_Maske(string maske, string datei)
    {
        string markup = MarkupOhneKommentare(datei);

        KiDialog dialog = KiDialoge.Katalog.Finde(maske)!;
        Assert.NotNull(dialog);
        Assert.NotEmpty(dialog.Felder);

        var fehlt = new List<string>();
        foreach (KiDialogFeld f in dialog.Felder)
        {
            string eigenschaft = KiEigenschaftspfad.Eigenschaft(f.Eigenschaftspfad);
            Assert.NotEqual("", eigenschaft);

            if (!StehtImMarkup(markup, eigenschaft))
                fehlt.Add(f.Name + " → " + f.Eigenschaftspfad);
        }

        Assert.True(fehlt.Count == 0,
                    "Diese Felder der Maske '" + maske + "' stehen in " + datei +
                    " an keiner Bindung — sie sind für den Anwender unsichtbar und " +
                    "gehören damit nicht in den Dialogkatalog: " + string.Join(", ", fehlt));
    }

    /// <summary>
    /// Jede Maske des Katalogs steht in GENAU EINER der beiden Listen — entweder mit
    /// Razor-Datei oder mit begründeter Ausnahme. Ohne diesen Fall verschwände eine
    /// neue Maske stillschweigend aus der Probe, indem niemand sie einträgt.
    /// </summary>
    [Fact]
    public void Jede_Katalogmaske_hat_entweder_ein_Markup_oder_einen_Ausnahmegrund()
    {
        var mitDatei = new HashSet<string>();
        foreach (object[] zeile in Markupdateien()) mitDatei.Add((string)zeile[0]);

        foreach (KiDialog d in KiDialoge.Katalog.Alle)
        {
            bool markup = mitDatei.Contains(d.Maskenname);
            bool ausnahme = OhneMarkupprobe.ContainsKey(d.Maskenname);

            Assert.True(markup ^ ausnahme,
                        "Die Maske '" + d.Maskenname + "' steht in keiner oder in beiden " +
                        "Listen der Markup-Probe.");
        }

        // Und die Ausnahme bleibt an ihre Begründung gebunden: Eine Maske ohne
        // Markup-Probe MUSS über eine Sichtklasse binden — daran hängt der Grund.
        foreach (object[] zeile in Masken())
        {
            if (!OhneMarkupprobe.ContainsKey((string)zeile[0])) continue;

            Type daten = (Type)zeile[1];
            Assert.EndsWith("KiSicht", daten.Name, StringComparison.Ordinal);
            Assert.NotEmpty(OhneMarkupprobe[(string)zeile[0]]);
        }
    }

    /// <summary>
    /// Die GEGENPROBE: Die Regel darf nicht alles durchlassen. Ein erfundener
    /// Eigenschaftsname und eine Zeichenkette ohne führenden Punkt fallen durch.
    /// </summary>
    [Fact]
    public void Die_Markupprobe_laesst_nicht_alles_durch()
    {
        string markup = MarkupOhneKommentare(
            "EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor");

        // Es gibt sie: die sechs Felder, die die Maske zeigt.
        Assert.True(StehtImMarkup(markup, "Ptherm"));
        Assert.True(StehtImMarkup(markup, "Ruecklauf"));

        // Es gibt sie nicht: die neun, die am 15.09.2026 gefallen sind. Genau darauf
        // hätte der bisherige Wächter nicht angeschlagen.
        foreach (string weg in new[]
                 {
                     "Investitionskosten", "Wartungskosten", "Raumbedarf", "Nutzungsdauer",
                     "CO2", "SO2", "NOx", "CO", "Staub"
                 })
            Assert.False(StehtImMarkup(markup, weg), weg);

        // Und ein Name, den es nie gab.
        Assert.False(StehtImMarkup(markup, "GibtEsNichtImMarkup"));
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Markupprobe_findet_ihre_Dateien_und_prueft_genug_Felder()
    {
        int felder = 0;

        foreach (object[] zeile in Markupdateien())
        {
            foreach (string datei in Dateien((string)zeile[1]))
                Assert.True(File.Exists(Path.Combine(Wurzel(),
                                                     datei.Replace('/', Path.DirectorySeparatorChar))),
                            datei);
            felder += KiDialoge.Katalog.Finde((string)zeile[0])!.Felder.Count;
        }

        // 6 (Heizkesseleditor) + 14 (PV) + 1 (Puffereditor) + 1 (WP-Verwaltung) +
        // 7 (Kostenverwaltung) + 3 (Heizkessel im Projekt) + 4 (BHKW im Projekt) +
        // 1 (Pufferspeicher im Projekt) + 1 (Stromspeicher im Projekt) +
        // 5 (Solarkollektoren) + 21 (Waermepumpen-Anlage) = 64.
        Assert.True(felder >= 64, "Nur " + felder + " Feldpfade geprüft.");
    }

    // ---------------------------------------------------------------------
    //  Hilfen der Markup-Probe
    // ---------------------------------------------------------------------

    /// <summary>
    /// Kommt <paramref name="eigenschaft"/> im Markup als BINDUNG vor — also mit
    /// führendem Punkt und an einer Wortgrenze endend?
    /// </summary>
    /// <remarks>
    /// Die Wortgrenze am Ende ist nötig, damit <c>CO</c> nicht auf <c>Daten.CO2</c>
    /// trifft; der führende Punkt trennt die Bindung vom gleichlautenden Literaltext.
    /// </remarks>
    private static bool StehtImMarkup(string markup, string eigenschaft)
        => Regex.IsMatch(markup, @"\." + Regex.Escape(eigenschaft) + @"\b");

    /// <summary>
    /// Die einzelnen Dateien einer Maske — eine Zeile der Zuordnungstabelle trägt sie
    /// mit <c>;</c> getrennt (siehe <see cref="Markupdateien"/>).
    /// </summary>
    private static string[] Dateien(string eintrag)
        => eintrag.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Der Inhalt der Razor-Datei(en) einer Maske OHNE Kommentare: <c>@* … *@</c> und
    /// jede Zeile, die (nach Einrückung) mit <c>//</c> oder <c>///</c> beginnt.
    /// </summary>
    private static string MarkupOhneKommentare(string eintrag)
        => string.Join("\n", Dateien(eintrag).Select(EineDateiOhneKommentare));

    private static string EineDateiOhneKommentare(string repopfad)
    {
        string voll = Path.Combine(Wurzel(), repopfad.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(voll), repopfad);

        string text = Regex.Replace(File.ReadAllText(voll), @"@\*.*?\*@", " ",
                                    RegexOptions.Singleline);

        return string.Join("\n", text.Split('\n')
                                     .Where(z => !z.TrimStart().StartsWith("//", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Der Weg zur Repowurzel — dasselbe Verfahren wie
    /// <c>UeberlagerungstitelTests.Wurzel</c> und <c>StilblattTests.Wwwroot</c>.
    /// </summary>
    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    // =====================================================================
    //  Die fünfte Deklaration
    // =====================================================================

    [Fact]
    public void Die_Stromspeicher_Ansicht_fuehrt_siebenundzwanzig_Felder_und_keinen_Knopf()
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;

        // Seit #215 siebzehn („Peak-Ziel adaptiv", kausale Ratsche, Spezifikation
        // 5.1.1); seit AUFTRAG #224 sechsundzwanzig: der SCHRITT der Ansicht, die vier
        // Felder der Station 4 („beste Größe suchen", Feinraster, maximale Kandidaten,
        // Kandidatenzahl des Suchraums) und die vier des Kastens „Bestes Ergebnis".
        // Seit AUFTRAG #247 siebenundzwanzig: die SUCHMETHODE (nur lesend) sagt, WAS
        // der nächste Lauf variiert — Größe oder Stückzahl (SD‑E‑10).
        Assert.Equal(27, d.Felder.Count);

        // KEINE Knöpfe: „Berechnen", „Peak-Ziel bestimmen…" und „Speichern" sind
        // rechnende bzw. datenbankwirksame Aktionen und gehören in das Aktionsregister
        // mit Bestätigung und Sicherungspunkt (Stufe S3), nicht in eine Knopfliste.
        Assert.Empty(d.Knoepfe);
    }

    [Theory]
    [InlineData("einheiten")]
    [InlineData("einheiten_liste")]
    [InlineData("kapazitaet_gesamt")]
    [InlineData("ladeleistung_gesamt")]
    [InlineData("entladeleistung_gesamt")]
    [InlineData("betriebsziel")]
    [InlineData("peak_ziel")]
    [InlineData("peak_ziel_adaptiv")]
    [InlineData("netzladung")]
    [InlineData("start_soc")]
    [InlineData("peak_reserve")]
    [InlineData("diagnose_arbeitslos")]
    [InlineData("diagnose_gruende")]
    [InlineData("pruefhinweise")]
    [InlineData("ergebnis_bezugsspitze")]
    [InlineData("ergebnis_netzbezug")]
    [InlineData("ergebnis_kapitalwert")]
    [InlineData("schritt")]
    [InlineData("groessen_optimieren")]
    [InlineData("feinraster")]
    [InlineData("maximale_kandidaten")]
    [InlineData("kandidatenzahl")]
    [InlineData("bestes_kapitalwert")]
    [InlineData("bestes_kapazitaet")]
    [InlineData("bestes_ersparnis")]
    [InlineData("bestes_phase")]
    public void Die_Stromspeicher_Ansicht_kennt_dieses_Feld(string name)
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;

        KiDialogFeld feld = d.FindeFeld(name)!;
        Assert.NotNull(feld);
        Assert.NotEmpty(feld.Anzeigename);
        Assert.NotEmpty(feld.Erlaeuterung);
    }

    [Fact]
    public void Nur_die_neun_eingebbaren_Felder_sind_SETZBAR()
    {
        // Die übrigen siebzehn sind ABGELEITET: die Summen der Flotte (eine Zahl je
        // Feld, aber viele Einheiten dahinter), der Schritt der Ansicht, die
        // Kandidatenzahl des Suchraums, die Diagnose und das Ergebnis des letzten Laufs.
        // Die Stufe S3 muss ein feld_setzen darauf ablehnen können, und das hängt an
        // der Schreibbarkeit der Eigenschaft (KiFeldzugang.Setzbar).
        //
        // MIT AUFTRAG #224 kommen DREI setzbare dazu, alle aus Station 4: die Wahl
        // „beste Größe suchen", der Feinraster-Schalter und die Kandidatengrenze.
        string[] setzbar =
        {
            "betriebsziel", "peak_ziel", "peak_ziel_adaptiv", "netzladung", "start_soc",
            "peak_reserve", "groessen_optimieren", "feinraster", "maximale_kandidaten"
        };

        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;
        var gefundenSetzbar = new List<string>();

        foreach (KiDialogFeld f in d.Felder)
        {
            var eigenschaft = typeof(StromspeicherKiSicht)
                .GetProperty(f.Eigenschaft,
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.Instance);

            Assert.NotNull(eigenschaft);
            if (eigenschaft!.CanWrite) gefundenSetzbar.Add(f.Name);
        }

        Assert.Equal(setzbar.OrderBy(x => x, StringComparer.Ordinal),
                     gefundenSetzbar.OrderBy(x => x, StringComparer.Ordinal));
    }

    // =====================================================================
    //  Das Sichtmodell rechnet aus den lebenden Ständen
    // =====================================================================

    [Fact]
    public void Die_Sicht_summiert_die_Flotte_und_mittelt_den_Start_SoC()
    {
        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.Equal(2, sicht.Einheitenzahl);
        Assert.Equal(40.0, sicht.KapazitaetGesamtKWh);
        Assert.Equal(16.0, sicht.LadeleistungGesamtKw);
        Assert.Equal(19.0, sicht.EntladeleistungGesamtKw);
        Assert.Equal(45.0, sicht.StartSocProzent);            // (0,40 + 0,50) / 2 · 100
        Assert.Equal(7.0, sicht.PeakReserveKWh);

        Assert.Equal("PeakShaving", sicht.Betriebsziel);
        Assert.Equal(16.0, sicht.PeakZielKw);
        Assert.False(sicht.NetzladungErlaubt);

        Assert.Contains("Eins: 24 kWh, 10 / 12 kW", sicht.EinheitenListe, StringComparison.Ordinal);
        Assert.Contains("Zwei: 16 kWh, 6 / 7 kW", sicht.EinheitenListe, StringComparison.Ordinal);
    }

    [Fact]
    public void Die_Sicht_liest_bei_jedem_Zugriff_neu()
    {
        // Die Ansicht ersetzt ihren Eingabestand bei jeder Änderung durch eine KOPIE;
        // ein festgehaltenes Objekt zeigte dem Assistenten den Stand von vorhin.
        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.Equal(2, sicht.Einheitenzahl);

        eingaben = Eingaben();
        eingaben.Auslegung!.Flotte!.Einheiten.RemoveAt(1);

        Assert.Equal(1, sicht.Einheitenzahl);
        Assert.Equal(24.0, sicht.KapazitaetGesamtKWh);
    }

    [Fact]
    public void Ohne_Flotte_und_ohne_Lauf_bleibt_die_Sicht_leer_statt_zu_werfen()
    {
        var sicht = new StromspeicherKiSicht(() => null, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.Equal(0, sicht.Einheitenzahl);
        Assert.Equal("", sicht.EinheitenListe);
        Assert.Equal(0.0, sicht.KapazitaetGesamtKWh);
        Assert.Equal(0.0, sicht.StartSocProzent);
        Assert.Equal("", sicht.Betriebsziel);
        Assert.Null(sicht.PeakZielKw);
        Assert.False(sicht.Arbeitslos);
        Assert.Equal("", sicht.DiagnoseGruende);
        Assert.Equal("", sicht.Pruefhinweise);
        Assert.Null(sicht.BezugsspitzeKw);
        Assert.Null(sicht.NetzbezugKWh);
        Assert.Null(sicht.KapitalwertEuro);
    }

    [Fact]
    public void Die_Sicht_gibt_Diagnose_und_Ergebnis_des_letzten_Laufs_heraus()
    {
        // „Warum ist die Flotte arbeitslos?" wird damit mit den echten Zählern
        // beantwortet und nicht mit einer Vermutung.
        var ergebnis = new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Studie = new FlottenStudienErgebnis
            {
                Variante = new FlottenSimulationErgebnis
                {
                    NetzbezugKWh = 51_611.0,
                    MaximalerNetzbezugKw = 16.7428,
                    Diagnose = new FlottenDiagnose
                    {
                        IntervalleGesamt = 35_040,
                        Arbeitslos = true,
                        IntervalleLadedeckelNullNetzladeverbot = 12_000
                    }
                },
                Wirtschaftlichkeit = new FlottenWirtschaftlichkeitErgebnis
                {
                    KapitalwertEuro = -1234.5
                }
            },
            Pruefhinweise =
            {
                new FlottenHinweis { Stufe = FlottenHinweisStufe.Warnung, Text = "Kein Peak-Ziel gesetzt." },
                new FlottenHinweis { Stufe = FlottenHinweisStufe.Hinweis, Text = "Netzladung verboten." }
            }
        };

        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => ergebnis,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.True(sicht.Arbeitslos);
        Assert.NotEqual("", sicht.DiagnoseGruende);
        Assert.Equal("Kein Peak-Ziel gesetzt.\nNetzladung verboten.", sicht.Pruefhinweise);
        Assert.Equal(16.7428, sicht.BezugsspitzeKw);
        Assert.Equal(51_611.0, sicht.NetzbezugKWh);
        Assert.Equal(-1234.5, sicht.KapitalwertEuro);
    }

    [Fact]
    public void Ohne_Lauf_traegt_die_Sicht_die_Hinweise_der_VORPRUEFUNG()
    {
        var eingaben = Eingaben();
        var vorpruefung = new[]
        {
            new FlottenHinweis { Stufe = FlottenHinweisStufe.Hinweis, Text = "Kostensätze fehlen." }
        };

        var sicht = new StromspeicherKiSicht(() => eingaben, () => null, () => vorpruefung);

        Assert.Equal("Kostensätze fehlen.", sicht.Pruefhinweise);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Ein Arbeitsstand nach dem Muster des Referenzprojekts 1046 („Prüfprojekt
    /// Speicherflotte"): zwei Einheiten, Ziel <c>PeakShaving</c> gegen 16 kW.
    /// </summary>
    private static SpeicherOptimierungEingaben Eingaben()
    {
        return new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Flotte = new FlottenStudieKonfiguration
                {
                    Einheiten =
                    {
                        new FlottenEinheit
                        {
                            Id = "1", Name = "Eins",
                            KapazitaetKWh = 24.0, LadeleistungKw = 10.0, EntladeleistungKw = 12.0,
                            SocStart = 0.40, PeakReserveKWh = 3.0
                        },
                        new FlottenEinheit
                        {
                            Id = "2", Name = "Zwei",
                            KapazitaetKWh = 16.0, LadeleistungKw = 6.0, EntladeleistungKw = 7.0,
                            SocStart = 0.50, PeakReserveKWh = 4.0
                        }
                    },
                    Optionen = new FlottenSimulationOptionen
                    {
                        Betriebsziel = FlottenBetriebsziel.PeakShaving,
                        Verteilung = FlottenVerteilung.Kaskade,
                        WirtschaftlicherPeakZielwertKw = 16.0,
                        NetzladungErlaubt = false
                    }
                }
            }
        };
    }
}
