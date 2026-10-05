using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Das Blatt „Nutzungsprofile"</b> (Stufe NP3a; Konzept Nutzungsprofile 6.1, NP-F3, NP-F6, NP-F12,
/// NP-F19): Katalogbaum, Profileditor, Vorschau und der Reiter „Zuordnung" über einem Weg OHNE Datenbank —
/// die Delegaten merken sich, was sie bekommen haben. Geprüft werden der Feldbestand, der Lesemodus eines
/// ausgelieferten Profils, Anlegen, Ändern und Löschen eines eigenen, die Vorschau je Größe, das Umstellen
/// einer Zuordnung und der Fall OHNE Gaben.
/// </summary>
/// <remarks>Die Kultur ist über <see cref="EposBunitContext"/> auf de-DE gepinnt.</remarks>
public class RaumnutzungBlattTests : EposBunitContext
{
    private const long KAT_MUSTER = 1;
    private const long KAT_EIGEN = 2;
    private const long PROFIL_MUSTER = 11;
    private const long PROFIL_EIGEN = 12;

    /// <summary>Ein Weg, der mitschreibt — die Rolle der Hülle im Prüfstand.</summary>
    private sealed class Probeweg
    {
        internal readonly List<string> Spur = new();
        internal readonly List<RaumnutzungKategorieDaten> Kategorien = new();
        internal readonly List<RaumnutzungProfilDaten> Profile = new();
        internal readonly List<RaumnutzungZuordnungDaten> Zuordnungen = new();
        internal RaumnutzungProfilDaten Geschrieben;
        internal long NaechsteId = 90;

        internal RaumnutzungWeg Weg() => new RaumnutzungWeg
        {
            Kategorien = () => Kategorien.ToList(),
            Profile = id => Profile.Where(p => p.IdKategorie == id).ToList(),
            Zuordnungen = () => Zuordnungen.ToList(),
            Vorschau = (p, flaeche, hoehe) => Vorschau(p),
            KategorieAnlegen = (bez, besch, quelle) =>
            {
                Spur.Add("KategorieAnlegen:" + bez);
                var k = new RaumnutzungKategorieDaten(++NaechsteId, bez, RaumnutzungArt.Eigen, false, besch, quelle);
                Kategorien.Add(k);
                return RaumnutzungErgebnis.MitId(k.Id);
            },
            KategorieAendern = (id, bez, besch, quelle) => RaumnutzungErgebnis.Gut,
            KategorieDuplizieren = (id, bez) => RaumnutzungErgebnis.MitId(++NaechsteId),
            KategorieLoeschen = id =>
            {
                Spur.Add("KategorieLoeschen:" + id);
                Kategorien.RemoveAll(k => k.Id == id);
                return RaumnutzungErgebnis.Gut;
            },
            ProfilAnlegen = p =>
            {
                Spur.Add("ProfilAnlegen:" + p.Bezeichner);
                RaumnutzungProfilDaten neu = p.Kopie();
                neu.Id = ++NaechsteId;
                Profile.Add(neu);
                return RaumnutzungErgebnis.MitId(neu.Id);
            },
            ProfilAendern = p =>
            {
                Spur.Add("ProfilAendern:" + p.Id);
                Geschrieben = p.Kopie();
                Profile.RemoveAll(x => x.Id == p.Id);
                Profile.Add(p.Kopie());
                return RaumnutzungErgebnis.MitId(p.Id);
            },
            ProfilDuplizieren = (id, ziel, bez) => RaumnutzungErgebnis.MitId(++NaechsteId),
            ProfilLoeschen = id =>
            {
                Spur.Add("ProfilLoeschen:" + id);
                Profile.RemoveAll(p => p.Id == id);
                return RaumnutzungErgebnis.Gut;
            },
            ZuordnungenAuf = id => Zuordnungen.Count(z => z.IdProfil == id),
            ZuordnungSetzen = (art, schluessel, idProfil) =>
            {
                Spur.Add("ZuordnungSetzen:" + art + ":" + schluessel + ":" + (idProfil?.ToString() ?? "keine"));
                int i = Zuordnungen.FindIndex(z => z.Art == art && z.Schluessel == schluessel);
                if (i >= 0) Zuordnungen[i] = Zuordnungen[i] with { IdProfil = idProfil };
                else Zuordnungen.Add(new RaumnutzungZuordnungDaten(++NaechsteId, art, schluessel, idProfil, false));
                return RaumnutzungErgebnis.Gut;
            },
            ZuordnungLoeschen = id =>
            {
                Spur.Add("ZuordnungLoeschen:" + id);
                Zuordnungen.RemoveAll(z => z.Id == id);
                return RaumnutzungErgebnis.Gut;
            },
        };

        /// <summary>Eine Vorschau, die je Größe eine Zeile führt — Heizen und Personen belegt, der Rest nicht.</summary>
        private static RaumnutzungVorschau Vorschau(RaumnutzungProfilDaten p)
        {
            var groessen = new List<RaumnutzungVorschauGroesse>();
            foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            {
                bool belegt = g is KonditionierungGroesse.Heizen or KonditionierungGroesse.Personen;
                groessen.Add(new RaumnutzungVorschauGroesse(g, belegt, "aus Kennwerten",
                    belegt
                        ? new[] { new RaumnutzungVorschauzeile("Tag", "20 °C", "7 – 18", "Mo–Fr") }
                        : Array.Empty<RaumnutzungVorschauzeile>(),
                    belegt ? "8 W/m² × 120 m² = 960 W" : "", ""));
            }
            return new RaumnutzungVorschau(p.ToString(), groessen, "erzeugt: 250 Nutzungstage, Quelle: 250 Tage",
                                           Array.Empty<string>());
        }
    }

    /// <summary>Ein Katalog mit zwei Kategorien, einem ausgelieferten und einem eigenen Profil.</summary>
    private static Probeweg Probekatalog()
    {
        var w = new Probeweg();
        w.Kategorien.Add(new RaumnutzungKategorieDaten(KAT_MUSTER, "EPOS-Muster", RaumnutzungArt.EposMuster, true,
                                                       "Die ausgelieferten Muster", ""));
        w.Kategorien.Add(new RaumnutzungKategorieDaten(KAT_EIGEN, "Eigene Profile", RaumnutzungArt.Eigen, false, "", ""));
        w.Profile.Add(new RaumnutzungProfilDaten
        {
            Id = PROFIL_MUSTER, IdKategorie = KAT_MUSTER, Nummer = "1", Bezeichner = "Wohnen",
            Ausgeliefert = true, HeizSoll = 20, NutzungstageWoche = "1111111",
        });
        w.Profile.Add(new RaumnutzungProfilDaten
        {
            Id = PROFIL_EIGEN, IdKategorie = KAT_EIGEN, Nummer = "", Bezeichner = "Mein Büro",
            HeizSoll = 21, KuehlSoll = 26, NutzungstageWoche = "1111100", NutzungstageJahr = 250,
            NutzungVon = 7, NutzungBis = 18, GeraeteLeistung = 8, PersonenFlaeche = 12,
        });
        w.Zuordnungen.Add(new RaumnutzungZuordnungDaten(21, RaumnutzungZuordnungsart.DinNummer, "1", PROFIL_MUSTER, true));
        w.Zuordnungen.Add(new RaumnutzungZuordnungDaten(22, RaumnutzungZuordnungsart.IfcKlasse, "Buero", null, false));
        return w;
    }

    private IRenderedComponent<RaumnutzungBlatt> Blatt(Probeweg w)
        => Render<RaumnutzungBlatt>(p => p
               .Add(x => x.Katalogweg, w?.Weg())
               .Add(x => x.Texte, new RaumnutzungTexte())
               .Add(x => x.GroessenTexte, new KonditionierungTexte()));

    // =====================================================================
    //  Der Katalogbaum
    // =====================================================================

    /// <summary>
    /// Der Baum führt die Kategorien mit Art und Schloss; ein Klick klappt die Profile einer Kategorie auf
    /// (Konzept Nutzungsprofile 6.1 Punkt 1).
    /// </summary>
    [Fact]
    public void Der_Baum_fuehrt_die_Kategorien_und_klappt_ihre_Profile_auf()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);

        Assert.Equal(2, cut.FindAll(".epos-raumnutzung-kategorie").Count);
        Assert.Contains("EPOS-Muster", cut.Markup);
        Assert.Contains("Eigene Profile", cut.Markup);
        // Noch ist keine Kategorie offen, also steht kein Profil da.
        Assert.Empty(cut.FindAll(".epos-raumnutzung-profil"));

        cut.Find("[data-kategorie=\"1\"] .epos-raumnutzung-kategorie-waehlen").Click();
        Assert.Single(cut.FindAll(".epos-raumnutzung-profil"));
        Assert.Contains("1 Wohnen", cut.Markup);
        Assert.Equal(KAT_MUSTER, cut.Instance.GewaehlteKategorie);
    }

    /// <summary>Ein ausgeliefertes Profil trägt das Schloss und steht im Lesemodus (NP-F19).</summary>
    [Fact]
    public void Ein_ausgeliefertes_Profil_ist_lesend_und_sein_Speichern_ist_weich_gesperrt()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find("[data-kategorie=\"1\"] .epos-raumnutzung-kategorie-waehlen").Click();
        cut.Find("[data-profil=\"11\"] .epos-raumnutzung-profil-waehlen").Click();

        Assert.Equal(PROFIL_MUSTER, cut.Instance.GewaehltesProfil);
        Assert.NotEmpty(cut.FindAll(".epos-raumnutzung-lesend"));
        IElement speichern = cut.Find(".epos-raumnutzung-speichern");
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        // Ein Klick meldet den Grund und schreibt nichts.
        speichern.Click();
        Assert.Empty(w.Spur);
        Assert.NotEmpty(cut.FindAll(".epos-raumnutzung-meldung"));
    }

    // =====================================================================
    //  Der Profileditor
    // =====================================================================

    /// <summary>Ein eigenes Profil lässt sich ändern; der Editor gibt den ganzen Feldsatz weiter.</summary>
    [Fact]
    public void Ein_eigenes_Profil_laesst_sich_aendern()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find("[data-kategorie=\"2\"] .epos-raumnutzung-kategorie-waehlen").Click();
        cut.Find("[data-profil=\"12\"] .epos-raumnutzung-profil-waehlen").Click();

        Assert.NotNull(cut.Instance.Arbeitsstand);
        Assert.Equal("Mein Büro", cut.Instance.Arbeitsstand.Bezeichner);
        Assert.Equal(21, cut.Instance.Arbeitsstand.HeizSoll);

        cut.Find(".epos-raumnutzung-speichern").Click();
        Assert.Contains("ProfilAendern:12", w.Spur);
        Assert.NotNull(w.Geschrieben);
        Assert.Equal(250, w.Geschrieben.NutzungstageJahr);
        Assert.Equal(8, w.Geschrieben.GeraeteLeistung);
    }

    /// <summary>„Neues Profil…" legt in der gewählten eigenen Kategorie an (NP-F19).</summary>
    [Fact]
    public void Ein_eigenes_Profil_laesst_sich_anlegen()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find("[data-kategorie=\"2\"] .epos-raumnutzung-kategorie-waehlen").Click();
        cut.Find(".epos-raumnutzung-profil-neu").Click();

        Assert.Single(w.Spur);
        Assert.StartsWith("ProfilAnlegen:", w.Spur[0]);
        Assert.Equal(2, w.Profile.Count(p => p.IdKategorie == KAT_EIGEN));
    }

    /// <summary>
    /// Löschen fragt zuerst und nennt die Zuordnungszeilen, die dabei auf „keine" fallen (NP-F19); „Ja"
    /// schreibt, „Nein" nicht.
    /// </summary>
    [Fact]
    public void Loeschen_fragt_erst_und_nennt_die_Zuordnungen()
    {
        Probeweg w = Probekatalog();
        w.Zuordnungen.Add(new RaumnutzungZuordnungDaten(23, RaumnutzungZuordnungsart.IfcKlasse, "Raum", PROFIL_EIGEN, false));
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find("[data-kategorie=\"2\"] .epos-raumnutzung-kategorie-waehlen").Click();
        cut.Find("[data-profil=\"12\"] .epos-raumnutzung-profil-waehlen").Click();
        cut.Find(".epos-raumnutzung-profil-loeschen").Click();

        Assert.Contains("Mein Büro", cut.Markup);
        Assert.Contains("1 Zuordnungszeile(n)", cut.Markup);
        Assert.Empty(w.Spur);

        cut.FindAll(".epos-rueckfrage button")[1].Click();
        Assert.Empty(w.Spur);

        cut.Find("[data-profil=\"12\"] .epos-raumnutzung-profil-waehlen").Click();
        cut.Find(".epos-raumnutzung-profil-loeschen").Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Contains("ProfilLoeschen:12", w.Spur);
        Assert.DoesNotContain(w.Profile, p => p.Id == PROFIL_EIGEN);
    }

    // =====================================================================
    //  Die Vorschau
    // =====================================================================

    /// <summary>
    /// „Vorschau" zeigt je Größe das Ergebnis des Generators; eine nicht belegte Größe sagt, dass das Ziel
    /// seinen Kalender behält (NP-F6), eine belegte führt Zeilen und die Nennwertherleitung (NP-F18).
    /// </summary>
    [Fact]
    public void Die_Vorschau_zeigt_je_Groesse_Zeilen_oder_den_Grund()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find("[data-kategorie=\"2\"] .epos-raumnutzung-kategorie-waehlen").Click();
        cut.Find("[data-profil=\"12\"] .epos-raumnutzung-profil-waehlen").Click();
        cut.Find(".epos-raumnutzung-vorschau-knopf").Click();

        Assert.Equal(5, cut.FindAll(".epos-raumnutzung-vorschau-groesse").Count);
        Assert.Equal(2, cut.FindAll(".epos-raumnutzung-vorschauzeilen").Count);
        Assert.Equal(3, cut.FindAll(".epos-raumnutzung-nichtbelegt").Count);
        Assert.Contains("erzeugt: 250 Nutzungstage", cut.Markup);
        Assert.Contains("8 W/m² × 120 m² = 960 W", cut.Markup);
    }

    // =====================================================================
    //  Der Reiter „Zuordnung"
    // =====================================================================

    /// <summary>
    /// Die Zuordnungstabelle führt Art, Schlüssel und die Profilwahl mit „keine"; eine ausgelieferte Zeile
    /// trägt das Schloss, bleibt aber umstellbar (NP-F12, NP-F19).
    /// </summary>
    [Fact]
    public void Die_Zuordnung_laesst_sich_umstellen_auch_in_einer_ausgelieferten_Zeile()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find(".epos-raumnutzung-reiter-zuordnung").Click();

        Assert.Equal(2, cut.FindAll(".epos-raumnutzung-zuordnungen tbody tr").Count);
        Assert.Contains("DIN-Nummer", cut.Markup);
        Assert.Contains("IFC-Klasse", cut.Markup);
        Assert.Contains("keine", cut.Markup);

        // Die ausgelieferte Zeile (NP-F19): Loeschen ist weich gesperrt, die Profilwahl nicht.
        Assert.Equal("true",
            cut.Find("[data-zuordnung=\"21\"] .epos-raumnutzung-zuordnung-loeschen").GetAttribute("aria-disabled"));
        cut.Find("[data-zuordnung=\"21\"] .epos-raumnutzung-zuordnung-profil").Change("12");
        Assert.Contains("ZuordnungSetzen:DinNummer:1:12", w.Spur);
        Assert.Equal(PROFIL_EIGEN, w.Zuordnungen.Single(z => z.Id == 21).IdProfil);
    }

    /// <summary>Eine neue Zeile entsteht mit Art und Schlüssel und zunächst ohne Profil (NP-F12).</summary>
    [Fact]
    public void Eine_neue_Zuordnungszeile_entsteht_ohne_Profil()
    {
        Probeweg w = Probekatalog();
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(w);
        cut.Find(".epos-raumnutzung-reiter-zuordnung").Click();
        cut.Find(".epos-raumnutzung-zuordnung-neu input[type=\"text\"]").Input("Lager");
        cut.Find(".epos-raumnutzung-zuordnung-neu-knopf").Click();

        Assert.Contains("ZuordnungSetzen:DinNummer:Lager:keine", w.Spur);
        Assert.Equal(3, w.Zuordnungen.Count);
    }

    // =====================================================================
    //  Ohne Gaben
    // =====================================================================

    /// <summary>
    /// Ohne Weg nennt das Blatt den Grund und bietet keinen Knopf an — „Kein Delegat, kein Knopf"
    /// (Hausregel, Konzept Nutzungsprofile 6.1).
    /// </summary>
    [Fact]
    public void Ohne_Gaben_nennt_das_Blatt_den_Grund_und_keinen_Knopf()
    {
        IRenderedComponent<RaumnutzungBlatt> cut = Blatt(null);

        Assert.NotEmpty(cut.FindAll(".epos-raumnutzung-sperrzeile"));
        Assert.Empty(cut.FindAll(".epos-raumnutzung-kategorie"));
        Assert.Empty(cut.FindAll(".epos-raumnutzung-profil-neu"));
        Assert.Empty(cut.FindAll(".epos-raumnutzung-reiter-zuordnung"));
    }

    /// <summary>
    /// Ein Bündel mit Listen, aber ohne Schreibdelegaten, zeigt den Baum und keinen Schreibknopf — die
    /// Knöpfe hängen je Handlung an ihrem Delegaten.
    /// </summary>
    [Fact]
    public void Ohne_Schreibdelegaten_steht_der_Baum_ohne_Schreibknopf()
    {
        Probeweg p = Probekatalog();
        RaumnutzungWeg voll = p.Weg();
        var nurLesen = new RaumnutzungWeg
        {
            Kategorien = voll.Kategorien,
            Profile = voll.Profile,
            Zuordnungen = voll.Zuordnungen,
        };
        IRenderedComponent<RaumnutzungBlatt> cut = Render<RaumnutzungBlatt>(x => x
            .Add(c => c.Katalogweg, nurLesen)
            .Add(c => c.Texte, new RaumnutzungTexte())
            .Add(c => c.GroessenTexte, new KonditionierungTexte()));

        Assert.Equal(2, cut.FindAll(".epos-raumnutzung-kategorie").Count);
        Assert.Empty(cut.FindAll(".epos-raumnutzung-profil-neu"));
        Assert.Empty(cut.FindAll(".epos-raumnutzung-kategorie-neu"));
        cut.Find(".epos-raumnutzung-reiter-zuordnung").Click();
        Assert.Empty(cut.FindAll(".epos-raumnutzung-zuordnung-neu-knopf"));
        Assert.Empty(cut.FindAll(".epos-raumnutzung-zuordnung-loeschen"));
    }
}
