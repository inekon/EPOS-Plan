using System;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

[Collection("Testdatenbank")]
public class SpeicherAuslegungProfilTests
{
    private const int Projekt = 192001;
    private const int Anlage = 192101;

    private static void Sql(string sql, params DbParam[] parameter)
    {
        using var db = DataRepository.Vorgang();
        db.Ausfuehren(sql, parameter);
        db.Commit();
    }

    private static void Anlegen()
    {
        Sql("INSERT INTO Tab_Projekt (ID,Projektname) VALUES (?,?)", new("@p", Projekt), new("@n", "Speicherauslegung Profiltest"));
        Sql("INSERT INTO Tab_Energieanlagen (ID,ID_Projekt,Bezeichner,ID_Type,ID_WP,ID_Kessel,ID_BHKW,ID_PV,ID_Solar,ID_SP,ID_PUFFER) " +
            "VALUES (?,?,?,?,NULL,NULL,NULL,NULL,NULL,NULL,NULL)", new("@a", Anlage), new("@p", Projekt), new("@n", "Profilspeicher"), new("@typ", WizardItemClass.SP_TYP));
    }

    private static SpeicherOptimierungEingaben Eingaben() => new()
    {
        Groessenachse = OptimiererGroessenachse.LeistungKw,
        PMinKw = 50, PMaxKw = 125, PSchrittKw = 25,
        Auslegung = new()
        {
            Profilname = "Mein Profil", EposModelljahrZuordnen = true,
            Strompreisprofil = new KostenprofilModel { Bezeichner = "Eigener Tarif", Monatswerte = "20;21", Wochenwerte = "1;2" },
            DirekteKosten = new() { InvestEurProKw = 200, BetriebEurProKwhJahr = 2.5 },
            LastDatei = new SpeicherZeitreihe
            {
                QuelleName = "Last.csv", Rolle = SpeicherZeitreihenRolle.Last,
                ZeitstempelUtc = new[] { new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                Werte = new[] { 12.5 }, SHA256 = "Pruefwert",
                Optionen = new() { WertSpalte = 3, ZeitstempelSpalte = 1 }
            }
        }
    };

    [Fact]
    public void Profil_speichert_Zeitreihe_Einheiten_Feldmodell_und_editierbare_tiefe_Kopie()
    {
        var original = Eingaben();
        var geladen = SpeicherAuslegungCtrl.Deserialisieren(SpeicherAuslegungCtrl.Serialisieren(original));
        Assert.Equal(50, geladen.PMinKw);
        Assert.Equal("20;21", geladen.Auslegung.Strompreisprofil.Monatswerte);
        Assert.Equal(3, geladen.Auslegung.LastDatei.Optionen.WertSpalte);
        Assert.Equal(original.Auslegung.LastDatei.ZeitstempelUtc, geladen.Auslegung.LastDatei.ZeitstempelUtc);
        Assert.True(geladen.Auslegung.EposModelljahrZuordnen);
        var kopie = geladen.Kopie();
        kopie.Auslegung.LastDatei.Werte[0] = 999;
        kopie.Auslegung.Strompreisprofil.Monatswerte = "30";
        Assert.Equal(12.5, geladen.Auslegung.LastDatei.Werte[0]);
        Assert.Equal("20;21", geladen.Auslegung.Strompreisprofil.Monatswerte);
    }

    [Fact]
    public void Datenbankprofil_wird_aktualisiert_und_uebersteht_erneutes_Lesen()
    {
        using var kopie = new TestDatenbank();
        Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        Anlegen();
        var e = Eingaben();
        SpeicherAuslegungCtrl.Speichern(Projekt, Anlage, "Sommer", e);
        e.PMaxKw = 200;
        SpeicherAuslegungCtrl.Speichern(Projekt, Anlage, "Sommer", e);
        SpeicherAuslegungCtrl.Speichern(Projekt, Anlage, "Winter", Eingaben());
        var profile = SpeicherAuslegungCtrl.Profile(Projekt, Anlage);
        Assert.Equal(2, profile.Count);
        Assert.Equal(200, profile.Single(p => p.Name == "Sommer").Eingaben.PMaxKw);
        Assert.Equal(125, profile.Single(p => p.Name == "Winter").Eingaben.PMaxKw);
        Assert.Equal("Pruefwert", profile[0].Eingaben.Auslegung.LastDatei.SHA256);
        Sql("DELETE FROM Tab_Projekt WHERE ID=?", new DbParam("@p", Projekt));
        Assert.Empty(SpeicherAuslegungCtrl.Profile(Projekt, Anlage));
    }

    [Fact]
    public void Kostenadapter_nimmt_nur_gueltige_aktive_spezifische_Kosten()
    {
        using var kopie = new TestDatenbank();
        Assert.True(kopie.Vorhanden);
        Anlegen();
        void Position(int id, int kategorie, string basis, double satz, string art = "BETRIEBSGEBUNDEN", int start = 0) =>
            Sql("INSERT INTO Tab_ProjektWerte (ID,ProjektID,ID_Anlage,KategorieID,Bemessung,Einheitpreis,Kostenart,IstErloes,StartJahr,StammID,KomponentenID,Gruppe) VALUES (?,?,?,?,?,?,?,0,?,NULL,NULL,NULL)",
                new("@id", id), new("@p", Projekt), new("@a", Anlage), new("@k", kategorie), new("@b", basis), new("@s", satz), new("@art", art), new("@start", start));
        Position(192000001, 1, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 100);
        Position(192000002, 1, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, 200);
        Position(192000003, 2, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, 3);
        Position(192000004, 2, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, 4);
        Position(192000005, 2, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.01);
        Position(192000006, 1, "EUR_PAUSCHAL", 10000);
        Position(192000007, 1, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 999, DbWerte.KOSTENART_ZUSCHUSS);
        Position(192000008, 2, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, 999, start: 5);
        var k = SpeicherAuslegungCtrl.Modulkosten(Projekt, Anlage);
        Assert.True(k.InvestVorhanden);
        Assert.True(k.BetriebVorhanden);
        Assert.Equal(100, k.InvestEurProKw);
        Assert.Equal(200, k.InvestEurProKwh);
        Assert.Equal(3, k.BetriebEurProKwJahr);
        Assert.Equal(4, k.BetriebEurProKwhJahr);
        Assert.Equal(0.01, k.BetriebEurProKwhEntladen);
        Assert.Equal(3, k.AusgelassenePositionen.Count);
    }

    [Fact]
    public void Anlagenassistent_erhaelt_Profile_beim_Erneuern_der_Anlagenzeile()
    {
        using var kopie = new TestDatenbank();
        Assert.True(kopie.Vorhanden);
        Sql("INSERT INTO Tab_Projekt (ID,Projektname) VALUES (?,?)", new("@p", Projekt), new("@n", "Speicherauslegung Assistenttest"));
        var wizard = new WizardCtrl();
        var liste = new System.Collections.Generic.List<WErzeugerModel>
        {
            new() { ID_Projekt = Projekt, Bezeichner = "BYD B-Box HVM 11.0", ID_Type = WizardItemClass.SP_TYP }
        };
        Assert.True(wizard.Add_WP_Waermeerzeuger(Projekt, liste));
        int vorher = SpeicherAuslegungCtrl.Anlage(Projekt);
        Assert.True(vorher > 0);
        SpeicherAuslegungCtrl.Speichern(Projekt, vorher, "Bleibt gespeichert", Eingaben());
        Assert.True(wizard.Del_Projekt_Waermeerzeuger(Projekt, WizardItemClass.SP_TYP));
        Assert.True(wizard.Add_WP_Waermeerzeuger(Projekt, liste));
        int nachher = SpeicherAuslegungCtrl.Anlage(Projekt);
        var profile = SpeicherAuslegungCtrl.Profile(Projekt, nachher);
        Assert.Single(profile);
        Assert.Equal("Bleibt gespeichert", profile[0].Name);
        Assert.Equal(125, profile[0].Eingaben.PMaxKw);
    }

    [Fact]
    public void Projektkopie_uebernimmt_Profil_mit_eigenen_Fremdschluesseln()
    {
        using var kopie = new TestDatenbank();
        Assert.True(kopie.Vorhanden);
        Anlegen();
        SpeicherAuslegungCtrl.Speichern(Projekt, Anlage, "Kopie", Eingaben());
        int neu = new ProjektDuplizierenCtrl().Duplizieren("Speicherauslegung Profiltest", "Speicherauslegung Kopie");
        Assert.True(neu > 0 && neu != Projekt);
        int neueAnlage;
        using (var db = DataRepository.Vorgang())
            neueAnlage = Convert.ToInt32(db.Skalar("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt=?", new DbParam("@p", neu)));
        Assert.NotEqual(Anlage, neueAnlage);
        var profile = SpeicherAuslegungCtrl.Profile(neu, neueAnlage);
        Assert.Single(profile);
        Assert.Equal(50, profile[0].Eingaben.PMinKw);
    }
}
