using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
/// </summary>
public class KiDialogkatalogTests
{
    /// <summary>Die sechs Masken und ihre Daten-Objekte — die EINE Zuordnungstabelle.</summary>
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
          typeof(EPOS.UI.Seiten.Simulation.SimulationKiSicht) }
    };

    // =====================================================================
    //  Der Wächter
    // =====================================================================

    [Theory]
    [MemberData(nameof(Masken))]
    public void Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt(string maske, Type daten)
    {
        IReadOnlyList<string> fehlt = KiMaskenanmeldung.Pruefe(maske, daten);

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
            KiMaskenanmeldung.Pruefe(KiMaskennamen.HEIZKESSEL, typeof(PufferSpKatalogDaten));

        Assert.Equal(15, fehlt.Count);
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
    public void Der_Katalog_fuehrt_sechs_Masken()
    {
        KiDialogKatalog katalog = KiDialoge.Katalog;

        Assert.Equal(6, katalog.Anzahl);
        foreach (object[] zeile in Masken())
            Assert.True(katalog.Kennt((string)zeile[0]), (string)zeile[0]);
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
    public void Die_vier_Startmasken_fuehren_unveraendert_15_3_1_und_1_Feld()
    {
        // Der Feldumfang ist mit #200 NICHT gewachsen — sonst liesse sich hinterher
        // nicht sagen, was den Feldblock verändert hat: der Umfang oder der
        // Auflösungsweg (Fachkonzept 11.6).
        Assert.Equal(15, KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL)!.Felder.Count);
        Assert.Equal(3, KiDialoge.Katalog.Finde(KiMaskennamen.PHOTOVOLTAIK)!.Felder.Count);
        Assert.Single(KiDialoge.Katalog.Finde(KiMaskennamen.PUFFERSPEICHER)!.Felder);
        Assert.Single(KiDialoge.Katalog.Finde(KiMaskennamen.WAERMEPUMPE)!.Felder);
    }

    // =====================================================================
    //  Die fünfte Deklaration
    // =====================================================================

    [Fact]
    public void Die_Stromspeicher_Ansicht_fuehrt_sechsundzwanzig_Felder_und_keinen_Knopf()
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;

        // Seit #215 siebzehn („Peak-Ziel adaptiv", kausale Ratsche, Spezifikation
        // 5.1.1); seit AUFTRAG #224 sechsundzwanzig: der SCHRITT der Ansicht, die vier
        // Felder der Station 4 („beste Größe suchen", Feinraster, maximale Kandidaten,
        // Kandidatenzahl des Suchraums) und die vier des Kastens „Bestes Ergebnis".
        Assert.Equal(26, d.Felder.Count);

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
