using System.Reflection;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Vertrag zwischen Oberfläche und Kern für die Konditionierung</b> (Stufe KP2, Welle U0b;
/// Entwurf KP2 Abschnitt 2): das DTO <see cref="KonditionierungDaten"/> und das Delegatenbündel
/// <see cref="KonditionierungWeg"/>.
///
/// <para><b>Was geprüft wird:</b> (a) die Kopie ist TIEF — Zellen, Kalender, Wochen und Perioden,
/// auch durch den Feldsatz, die Zone und den Stand hindurch; (b) die <c>Fassung</c> geht in den
/// Abdruck des Arbeitsstands und in den Vergleich der Zonen ein (Befund B10); (c) ein Bündel ohne
/// Delegaten bietet keinen Knopf an, jeder Delegat genau seinen, eine Sperre keinen — und jede
/// Handlung hat ihren Delegaten; (d) die Aufzählungen folgen dem Kern und die neun Bestandszellen
/// sind die des Kerns (<c>Matrixzellenort</c>), jede ein Feld des Feldsatzes — eine Wahrheit.</para>
///
/// <para>Ohne Texte, ohne Kultur: Der Abdruck ist invariant.</para>
/// </summary>
public sealed class KonditionierungDatenTests
{
    /// <summary>Eine Konditionierung, in der jede Ebene belegt ist.</summary>
    private static KonditionierungDaten Belegt()
    {
        var d = new KonditionierungDaten { Fassung = 3 };
        KonditionierungSpalte heizen = d.Spalte(KonditionierungGroesse.Heizen);
        heizen.Saison.Von = 274;
        heizen.Saison.Bis = 120;
        heizen.Kalender.Zustand = KonditionierungZustand.Angelegt;
        heizen.Kalender.Angabe = KonditionierungAngabe.Woche;
        heizen.Kalender.Woche = Enumerable.Repeat(20.0, 168).ToArray();
        heizen.Kalender.Woche[0] = double.NaN;
        heizen.Kalender.Vorlage = "Büro";
        heizen.Kalender.Perioden.Add(new KonditionierungPeriode
        {
            Rang = 300, Art = KonditionierungPeriodenart.Zeitraum, Name = "Betriebsurlaub", Von = 200, Bis = 214,
            Angabe = KonditionierungAngabe.Woche, Woche = Enumerable.Repeat(16.0, 168).ToArray()
        });
        KonditionierungSpalte lueftung = d.Spalte(KonditionierungGroesse.Lueftung);
        lueftung.Nacht.Wert = 2.0;
        lueftung.Nacht.Von = 22;
        lueftung.Nacht.Bis = 6;
        lueftung.Nacht.DeltaT = 3.0;
        d.Spalte(KonditionierungGroesse.Personen).Kalender.Nennwert = 280.0;
        return d;
    }

    // =================================================================================
    // (a) Die Kopie ist tief
    // =================================================================================

    [Fact]
    public void Die_Kopie_ist_tief_und_traegt_die_Fassung()
    {
        KonditionierungDaten original = Belegt();
        KonditionierungDaten kopie = original.Kopie();

        Assert.Equal(3, kopie.Fassung);
        Assert.NotSame(original.Spalte(KonditionierungGroesse.Heizen), kopie.Spalte(KonditionierungGroesse.Heizen));

        // Jede Ebene der Kopie ändern — das Original bleibt, wie es war.
        KonditionierungSpalte h = kopie.Spalte(KonditionierungGroesse.Heizen);
        h.Saison.Von = 1;
        h.Kalender.Woche![1] = 5.0;
        h.Kalender.Vorlage = "Schule";
        h.Kalender.Perioden[0].Woche![0] = 99.0;
        h.Kalender.Perioden[0].Name = "anders";
        h.Kalender.Perioden.Add(new KonditionierungPeriode { Rang = 1 });
        kopie.Spalte(KonditionierungGroesse.Lueftung).Nacht.DeltaT = 5.0;
        kopie.Spalte(KonditionierungGroesse.Personen).Kalender = new KonditionierungKalender();
        kopie.Weiterzaehlen();

        KonditionierungSpalte o = original.Spalte(KonditionierungGroesse.Heizen);
        Assert.Equal(274, o.Saison.Von);
        Assert.Equal(20.0, o.Kalender.Woche![1]);
        Assert.True(double.IsNaN(o.Kalender.Woche[0]));
        Assert.Equal("Büro", o.Kalender.Vorlage);
        Assert.Single(o.Kalender.Perioden);
        Assert.Equal(16.0, o.Kalender.Perioden[0].Woche![0]);
        Assert.Equal("Betriebsurlaub", o.Kalender.Perioden[0].Name);
        Assert.Equal(3.0, original.Spalte(KonditionierungGroesse.Lueftung).Nacht.DeltaT);
        Assert.Equal(280.0, original.Spalte(KonditionierungGroesse.Personen).Kalender.Nennwert);
        Assert.Equal(3, original.Fassung);
        Assert.Equal(4, kopie.Fassung);
    }

    [Fact]
    public void Feldsatz_Zone_und_Stand_kopieren_die_Konditionierung_tief()
    {
        var feldsatz = new GebaeudeKatalogDaten { Name = "Haus A", Konditionierung = Belegt() };
        var zone = new ZoneDaten { Id = -1, Bezeichner = "Büro", Konditionierung = Belegt() };
        var stand = new KonditionierungStand(feldsatz, new[] { zone });

        GebaeudeKatalogDaten fk = feldsatz.Kopie();
        ZoneDaten zk = zone.Kopie();
        KonditionierungStand sk = stand.Kopie();

        Assert.NotSame(feldsatz.Konditionierung, fk.Konditionierung);
        Assert.NotSame(zone.Konditionierung, zk.Konditionierung);
        Assert.NotSame(feldsatz, sk.Gebaeude);
        Assert.NotSame(feldsatz.Konditionierung, sk.Gebaeude.Konditionierung);
        Assert.NotSame(zone, sk.Zonen[0]);
        Assert.NotSame(zone.Konditionierung, sk.Zonen[0].Konditionierung);

        fk.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Woche![5] = 1.0;
        zk.Konditionierung!.Spalte(KonditionierungGroesse.Lueftung).Nacht.Wert = 9.0;
        sk.Gebaeude.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Perioden.Clear();
        sk.Zonen[0].Konditionierung!.Fassung = 17;

        Assert.Equal(20.0, feldsatz.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Woche![5]);
        Assert.Single(feldsatz.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Kalender.Perioden);
        Assert.Equal(2.0, zone.Konditionierung!.Spalte(KonditionierungGroesse.Lueftung).Nacht.Wert);
        Assert.Equal(3, zone.Konditionierung.Fassung);

        // Ohne Konditionierung bleibt es ohne.
        Assert.Null(new GebaeudeKatalogDaten().Kopie().Konditionierung);
        Assert.Null(new ZoneDaten().Kopie().Konditionierung);
    }

    [Fact]
    public void Eine_neue_Konditionierung_ist_leer_und_ueberall_abgeleitet()
    {
        var d = new KonditionierungDaten();

        Assert.Equal(0, d.Fassung);
        Assert.Equal(5, d.Spalten.Count);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            KonditionierungSpalte s = d.Spalte(g);
            Assert.Equal(g, s.Groesse);
            Assert.Same(s, d.Spalten[(int)g]);
            foreach (KonditionierungZeile z in Enum.GetValues<KonditionierungZeile>())
                Assert.True(s.Zelle(z).Leer);
            Assert.Equal(KonditionierungZustand.Abgeleitet, s.Kalender.Zustand);
            Assert.Null(s.Kalender.Woche);
            Assert.Empty(s.Kalender.Perioden);
        }
    }

    [Fact]
    public void Die_Spalte_liefert_je_Zeile_ihre_Zelle()
    {
        var s = new KonditionierungSpalte(KonditionierungGroesse.Kuehlen);

        Assert.Same(s.Nennwert, s.Zelle(KonditionierungZeile.Nennwert));
        Assert.Same(s.Tag, s.Zelle(KonditionierungZeile.Tag));
        Assert.Same(s.Nacht, s.Zelle(KonditionierungZeile.Nacht));
        Assert.Same(s.Wochenende, s.Zelle(KonditionierungZeile.Wochenende));
        Assert.Same(s.Ferien, s.Zelle(KonditionierungZeile.Ferien));
        Assert.Same(s.Saison, s.Zelle(KonditionierungZeile.Saison));

        s.Ferien.Aus = true;
        Assert.False(s.Ferien.Leer);
    }

    [Fact]
    public void Eigene_Perioden_sind_alle_ausserhalb_des_Matrixbereichs()
    {
        var k = new KonditionierungKalender();
        k.Perioden.Add(new KonditionierungPeriode { Rang = 201, Art = KonditionierungPeriodenart.Ferien, Matrixbereich = true });
        k.Perioden.Add(new KonditionierungPeriode { Rang = 900, Art = KonditionierungPeriodenart.Betriebspause, Matrixbereich = true });
        k.Perioden.Add(new KonditionierungPeriode { Rang = 300, Art = KonditionierungPeriodenart.Zeitraum });
        k.Perioden.Add(new KonditionierungPeriode { Rang = 100, Art = KonditionierungPeriodenart.Feiertag, Feiertagsregel = DbWerte.KOND_FEIERTAG_NEUJAHR });

        Assert.Equal(2, k.EigenePerioden);
    }

    // =================================================================================
    // (b) Die Fassung im Abdruck und im Vergleich der Zonen (Befund B10)
    // =================================================================================

    [Fact]
    public void Die_Fassung_geht_in_den_Abdruck_des_Arbeitsstands_ein()
    {
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(new GebaeudeKatalogDaten { Name = "Haus A", SollTag = 20, Konditionierung = Belegt() }, neu: false);

        string vorher = arbeit.Abdruck();
        Assert.Contains("|K3|", vorher);

        // Eine Änderung in einer Liste allein sähe der Abdruck nicht — die Fassung trägt sie.
        arbeit.Stand.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Perioden.Clear();
        Assert.Equal(vorher, arbeit.Abdruck());

        arbeit.Stand.Konditionierung.Weiterzaehlen();
        string nachher = arbeit.Abdruck();
        Assert.NotEqual(vorher, nachher);
        Assert.Contains("|K4|", nachher);

        // Ohne Konditionierung steht an ihrer Stelle nichts — ein anderer Abdruck als mit.
        arbeit.Stand.Konditionierung = null;
        Assert.DoesNotContain("|K", arbeit.Abdruck());
        Assert.NotEqual(nachher, arbeit.Abdruck());
    }

    [Fact]
    public void Eine_Zone_mit_anderer_Fassung_hat_andere_Werte()
    {
        var zone = new ZoneDaten { Id = 7, Bezeichner = "Büro", Konditionierung = Belegt() };
        ZoneDaten kopie = zone.Kopie();
        Assert.True(zone.GleicheWerte(kopie));

        kopie.Konditionierung!.Weiterzaehlen();
        Assert.False(zone.GleicheWerte(kopie));

        kopie.Konditionierung = null;
        Assert.False(zone.GleicheWerte(kopie));

        zone.Konditionierung = null;
        Assert.True(zone.GleicheWerte(kopie));
    }

    // =================================================================================
    // (c) Kein Delegat, kein Knopf
    // =================================================================================

    private static KonditionierungStand Stand() => new(new GebaeudeKatalogDaten(), Array.Empty<ZoneDaten>());

    private static KonditionierungErgebnis Gut(KonditionierungStand s) => KonditionierungErgebnis.Gut(s);

    private static readonly KonditionierungVorlageErgebnis VORLAGE_GUT = new(true, "", null);

    /// <summary>Ein Bündel, in dem jeder Delegat steht.</summary>
    private static KonditionierungWeg Voll(string? sperre = null) => new()
    {
        ZelleSetzen = (s, _, _, _) => Gut(s),
        Anlegen = (s, _) => Gut(s),
        Verwerfen = (s, _) => Gut(s),
        MatrixErneut = (s, _) => Gut(s),
        KatalogErneut = s => Gut(s),
        LuftwechselAufteilen = s => Gut(s),
        VomGebaeude = (s, _) => Gut(s),
        Geerbt = (_, _) => null,
        Vorlagen = _ => Array.Empty<KonditionierungVorlageDaten>(),
        VorlageUebernehmen = (s, _, _) => Gut(s),
        AlsVorlageSpeichern = (_, _, _) => VORLAGE_GUT,
        VorlageUmbenennen = (_, _) => VORLAGE_GUT,
        VorlageLoeschen = _ => VORLAGE_GUT,
        VorlageDuplizieren = (_, _) => VORLAGE_GUT,
        VorlageKopieren = (_, _) => VORLAGE_GUT,
        Kopierziele = _ => Array.Empty<KonditionierungKopierziel>(),
        Zeitfenster = (s, _, _) => Gut(s),
        Feiertage = (s, _, _) => Gut(s),
        Zeitstruktur = (s, _, _) => Gut(s),
        Grundangabe = (s, _, _) => Gut(s),
        Standardwoche = (s, _, _) => Gut(s),
        PeriodeSetzen = (s, _, _, _) => Gut(s),
        RangVerschieben = (s, _, _, _) => Gut(s),
        PeriodeLoeschen = (s, _, _) => Gut(s),
        SollwertprofilUebernehmen = s => Gut(s),
        Rueckfrage = (_, _, _) => null,
        SpeichernUnterRueckfrage = _ => null,
        WochenVorschau = (_, _) => null,
        Teppichbild = (_, _) => null,
        Lasten = (_, _) => null,
        Pruefen = _ => "",
        Sperre = sperre
    };

    /// <summary>Ein Bündel mit genau dem Delegaten einer Handlung (bei „Übernehmen" samt Liste, bei „Kopieren nach …" samt Zielen).</summary>
    private static KonditionierungWeg Nur(KonditionierungHandlung h) => h switch
    {
        KonditionierungHandlung.ZelleSetzen => new() { ZelleSetzen = (s, _, _, _) => Gut(s) },
        KonditionierungHandlung.Anlegen => new() { Anlegen = (s, _) => Gut(s) },
        KonditionierungHandlung.Verwerfen => new() { Verwerfen = (s, _) => Gut(s) },
        KonditionierungHandlung.MatrixErneut => new() { MatrixErneut = (s, _) => Gut(s) },
        KonditionierungHandlung.VorlageUebernehmen => new()
        {
            VorlageUebernehmen = (s, _, _) => Gut(s),
            Vorlagen = _ => Array.Empty<KonditionierungVorlageDaten>()
        },
        KonditionierungHandlung.AlsVorlageSpeichern => new() { AlsVorlageSpeichern = (_, _, _) => VORLAGE_GUT },
        KonditionierungHandlung.VorlageUmbenennen => new() { VorlageUmbenennen = (_, _) => VORLAGE_GUT },
        KonditionierungHandlung.VorlageLoeschen => new() { VorlageLoeschen = _ => VORLAGE_GUT },
        KonditionierungHandlung.VorlageDuplizieren => new() { VorlageDuplizieren = (_, _) => VORLAGE_GUT },
        KonditionierungHandlung.Zeitfenster => new() { Zeitfenster = (s, _, _) => Gut(s) },
        KonditionierungHandlung.Feiertage => new() { Feiertage = (s, _, _) => Gut(s) },
        KonditionierungHandlung.Zeitstruktur => new() { Zeitstruktur = (s, _, _) => Gut(s) },
        KonditionierungHandlung.KatalogErneut => new() { KatalogErneut = s => Gut(s) },
        KonditionierungHandlung.LuftwechselAufteilen => new() { LuftwechselAufteilen = s => Gut(s) },
        KonditionierungHandlung.VomGebaeude => new() { VomGebaeude = (s, _) => Gut(s) },
        KonditionierungHandlung.Grundangabe => new() { Grundangabe = (s, _, _) => Gut(s) },
        KonditionierungHandlung.Standardwoche => new() { Standardwoche = (s, _, _) => Gut(s) },
        KonditionierungHandlung.PeriodeSetzen => new() { PeriodeSetzen = (s, _, _, _) => Gut(s) },
        KonditionierungHandlung.RangVerschieben => new() { RangVerschieben = (s, _, _, _) => Gut(s) },
        KonditionierungHandlung.PeriodeLoeschen => new() { PeriodeLoeschen = (s, _, _) => Gut(s) },
        KonditionierungHandlung.SollwertprofilUebernehmen => new() { SollwertprofilUebernehmen = s => Gut(s) },
        KonditionierungHandlung.VorlageKopieren => new()
        {
            VorlageKopieren = (_, _) => VORLAGE_GUT,
            Kopierziele = _ => Array.Empty<KonditionierungKopierziel>()
        },
        _ => throw new ArgumentOutOfRangeException(nameof(h))
    };

    [Fact]
    public void Das_Nullbuendel_bietet_keinen_Knopf_an()
    {
        foreach (KonditionierungHandlung h in Enum.GetValues<KonditionierungHandlung>())
        {
            Assert.False(new KonditionierungWeg().Bietet(h), h.ToString());
            Assert.False(KonditionierungWeg.Keiner.Bietet(h), h.ToString());
        }

        // Und jeder Delegat des leeren Bündels ist wirklich leer.
        foreach (PropertyInfo p in typeof(KonditionierungWeg).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Assert.Null(p.GetValue(KonditionierungWeg.Keiner));
    }

    [Fact]
    public void Jeder_Delegat_bietet_genau_seinen_Knopf()
    {
        KonditionierungHandlung[] alle = Enum.GetValues<KonditionierungHandlung>();
        foreach (KonditionierungHandlung h in alle)
        {
            KonditionierungWeg weg = Nur(h);
            foreach (KonditionierungHandlung andere in alle)
                Assert.True(weg.Bietet(andere) == (andere == h), $"Nur {h}: Bietet({andere}) = {weg.Bietet(andere)}");
        }

        // „Übernehmen" ohne die Liste, aus der gewählt wird, ist kein Knopf - „Kopieren nach …" ohne Ziele ebenso.
        Assert.False(new KonditionierungWeg { VorlageUebernehmen = (s, _, _) => Gut(s) }
                         .Bietet(KonditionierungHandlung.VorlageUebernehmen));
        Assert.False(new KonditionierungWeg { VorlageKopieren = (_, _) => VORLAGE_GUT }
                         .Bietet(KonditionierungHandlung.VorlageKopieren));
    }

    [Fact]
    public void Das_volle_Buendel_bietet_jeden_Knopf_eine_Sperre_keinen()
    {
        foreach (KonditionierungHandlung h in Enum.GetValues<KonditionierungHandlung>())
        {
            Assert.True(Voll().Bietet(h), h.ToString());
            Assert.False(Voll("Die Datenbank trägt die Tabellen der Konditionierung nicht.").Bietet(h), h.ToString());
        }
    }

    [Fact]
    public void Jede_Handlung_hat_ihren_Delegaten_im_Buendel()
    {
        foreach (KonditionierungHandlung h in Enum.GetValues<KonditionierungHandlung>())
        {
            PropertyInfo? p = typeof(KonditionierungWeg).GetProperty(h.ToString());
            Assert.True(p is not null, "Kein Delegat für die Handlung " + h);
            Assert.True(typeof(Delegate).IsAssignableFrom(p!.PropertyType), h + " ist kein Delegat");
        }
    }

    [Fact]
    public void Das_Ergebnis_eines_Fehlschlags_traegt_keinen_Stand()
    {
        KonditionierungErgebnis f = KonditionierungErgebnis.Fehler("Rang 300 doppelt");
        Assert.False(f.Ok);
        Assert.Null(f.Stand);
        Assert.Equal("Rang 300 doppelt", f.Meldung);

        KonditionierungStand s = Stand();
        KonditionierungErgebnis g = KonditionierungErgebnis.Gut(s);
        Assert.True(g.Ok);
        Assert.Same(s, g.Stand);
        Assert.Equal("", g.Meldung);
    }

    // =================================================================================
    // (d) Die Aufzählungen folgen dem Kern; die neun Bestandszellen sind die des Kerns
    // =================================================================================

    /// <summary>Die Größe des Kerns zu einer Größe der Oberfläche — über die gemeinsame Reihenfolge.</summary>
    private static Konditionierungsgroesse Kern(KonditionierungGroesse g) => Konditionierungsgroessen.Alle[(int)g];

    [Fact]
    public void Die_Groessen_stehen_in_der_Reihenfolge_des_Kerns()
    {
        Assert.Equal(Konditionierungsgroessen.Alle.Length, KonditionierungDaten.Alle.Count);
        Assert.Equal(Enum.GetValues<KonditionierungGroesse>(), KonditionierungDaten.Alle);
        Assert.Equal(Konditionierungsgroesse.Heizsoll, Kern(KonditionierungGroesse.Heizen));
        Assert.Equal(Konditionierungsgroesse.Kuehlsoll, Kern(KonditionierungGroesse.Kuehlen));
        Assert.Equal(Konditionierungsgroesse.Lueftung, Kern(KonditionierungGroesse.Lueftung));
        Assert.Equal(Konditionierungsgroesse.Geraete, Kern(KonditionierungGroesse.Geraete));
        Assert.Equal(Konditionierungsgroesse.Personen, Kern(KonditionierungGroesse.Personen));
    }

    [Fact]
    public void Zeilen_Arten_Angaben_und_Nutzungen_folgen_dem_Schema()
    {
        Assert.Equal(DbWerte.KOND_ZEILEN, Enum.GetNames<KonditionierungZeile>().Select(n => n.ToUpperInvariant()));
        Assert.Equal(DbWerte.KOND_ARTEN, Enum.GetNames<KonditionierungPeriodenart>().Select(n => n.ToUpperInvariant()));
        Assert.Equal(Enum.GetNames<Angabeart>(), Enum.GetNames<KonditionierungAngabe>());
        Assert.Equal(DbWerte.KOND_NUTZUNGEN,
                     Enum.GetNames<KonditionierungNutzung>().Where(n => n != nameof(KonditionierungNutzung.Keine))
                         .Select(n => n.ToUpperInvariant()));
    }

    [Fact]
    public void Die_neun_Bestandszellen_sind_die_des_Kerns_und_Felder_des_Feldsatzes()
    {
        int zahl = 0;
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            foreach (KonditionierungZeile z in Enum.GetValues<KonditionierungZeile>())
            {
                string kennwort = DbWerte.KOND_ZEILEN[(int)z];
                bool kern = Matrixzellenort.HatBestandsspalte(Kalendereigentuemer.Gebaeude, Kern(g), kennwort);
                Assert.True(kern == KonditionierungDaten.IstBestandszelle(g, z), $"{g}/{z}: Kern {kern}");
                // Katalogbau und Zone führen dieselben Spalten (Matrixzellenort), eine Vorlage keine.
                Assert.Equal(kern, Matrixzellenort.HatBestandsspalte(Kalendereigentuemer.Katalogbau, Kern(g), kennwort));
                Assert.Equal(kern, Matrixzellenort.HatBestandsspalte(Kalendereigentuemer.Zone, Kern(g), kennwort));

                string? feld = KonditionierungDaten.Bestandsfeld(g, z);
                if (feld is null) continue;
                zahl++;
                PropertyInfo? p = typeof(GebaeudeKatalogDaten).GetProperty(feld);
                Assert.True(p is not null && p.PropertyType == typeof(double?), $"{g}/{z}: {feld} ist kein double?-Feld");
            }
        Assert.Equal(9, zahl);
    }

    [Fact]
    public void Die_Nachtzeiten_der_Heizspalte_stehen_im_Feldsatz()
    {
        (string Von, string Bis)? zeiten = KonditionierungDaten.Bestandszeiten(KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht);
        Assert.Equal((nameof(GebaeudeKatalogDaten.NachtBeginn), nameof(GebaeudeKatalogDaten.NachtEnde)), zeiten);
        Assert.Equal(typeof(int?), typeof(GebaeudeKatalogDaten).GetProperty(zeiten!.Value.Von)!.PropertyType);
        Assert.Equal(typeof(int?), typeof(GebaeudeKatalogDaten).GetProperty(zeiten.Value.Bis)!.PropertyType);

        // Sonst trägt jede Zelle ihre Zeiten selbst.
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            foreach (KonditionierungZeile z in Enum.GetValues<KonditionierungZeile>())
                if (g != KonditionierungGroesse.Heizen || z != KonditionierungZeile.Nacht)
                    Assert.Null(KonditionierungDaten.Bestandszeiten(g, z));
    }

    [Fact]
    public void Das_DTO_traegt_keine_Typen_des_Kerns()
    {
        // Die Datenseite der Konditionierung spricht nur Typen dieser Bibliothek und der Laufzeit —
        // die Übersetzung in den Kern gehört der Hülle (Hausregel „Kerntypen nicht im Razor-DTO").
        Type[] datentypen =
        {
            typeof(KonditionierungDaten), typeof(KonditionierungSpalte), typeof(KonditionierungZelle),
            typeof(KonditionierungKalender), typeof(KonditionierungPeriode), typeof(KonditionierungVorlageDaten),
            typeof(KonditionierungVorlageEingabe), typeof(KonditionierungZeitfenster), typeof(KonditionierungRueckfrage),
            typeof(KonditionierungPosten), typeof(KonditionierungLasten), typeof(KonditionierungOrt),
            typeof(KonditionierungStand), typeof(KonditionierungErgebnis), typeof(KonditionierungVorlageErgebnis),
            typeof(KonditionierungKopierziel), typeof(KonditionierungVorlageKopie)
        };
        Assembly kern = typeof(Konditionierungsgroesse).Assembly;
        foreach (Type t in datentypen)
            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Type typ = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                Type[] teile = typ.IsGenericType ? typ.GetGenericArguments() : typ.IsArray ? new[] { typ.GetElementType()! } : new[] { typ };
                foreach (Type teil in teile)
                    Assert.True(teil.Assembly != kern, $"{t.Name}.{p.Name} trägt den Kerntyp {teil.Name}");
            }

        // Die Bilder sind die Ausnahme des Hauses: ein Zeichenmodell für DiagrammSvg (EPOS.UI/CLAUDE.md).
        Assert.Equal(typeof(Zeichenmodell), typeof(KonditionierungWeg).GetProperty(nameof(KonditionierungWeg.Teppichbild))!
                                                                       .PropertyType.GetGenericArguments().Last());
    }
}
