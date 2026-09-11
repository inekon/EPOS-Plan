using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

[Collection("Testdatenbank")]
public sealed class SpeicherFlottenProjektProfilTests
{
    private const int Projekt = 193001;
    private const int AnlageA = 193101;
    private const int AnlageB = 193102;
    private const int GeraetA = 193201;
    private const int GeraetB = 193202;

    [Fact]
    public void Projektflotte_bleibt_vom_Aktuellen_Studienarbeitsstand_getrennt()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        using (var db = DataRepository.Vorgang())
        {
            db.Ausfuehren("INSERT INTO Tab_Projekt (ID,Projektname) VALUES (?,?)",
                new DbParam("@p", Projekt), new DbParam("@n", "Projektflotten-Profiltest"));
            db.Commit();
        }

        SpeicherOptimierungEingaben projektflotte = Eingaben(true);
        SpeicherAuslegungCtrl.Speichern(Projekt, 0,
            SpeicherFlottenProjektCtrl.ProjektflottenStand, projektflotte);
        SpeicherAuslegungCtrl.Speichern(Projekt, 0,
            SpeicherAuslegungCtrl.AktuellerStand, Eingaben(false));
        Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(Projekt));

        // Der Dialog darf @Aktuell beliebig weiterschreiben, ohne den übernommenen
        // Projektstand und dessen Aktivierung zu verändern.
        SpeicherAuslegungCtrl.Speichern(Projekt, 0,
            SpeicherAuslegungCtrl.AktuellerStand, Eingaben(true));
        Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(Projekt));

        SpeicherFlottenProjektCtrl.Deaktivieren(Projekt);
        Assert.False(SpeicherFlottenProjektCtrl.IstAktiv(Projekt));
        Assert.True(SpeicherAuslegungCtrl.Profile(Projekt, 0)
            .Single(x => x.Name == SpeicherAuslegungCtrl.AktuellerStand)
            .Eingaben.Auslegung.FlotteImProjektAktiv);
    }

    [Fact]
    public void Projektflotte_bleibt_bei_Wechsel_und_Loeschung_der_aktiven_Einzelanlage_erhalten()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");
        Sql("INSERT INTO Tab_Projekt (ID,Projektname) VALUES (?,?)",
            new DbParam("@p", Projekt), new DbParam("@n", "Projektweite Speicherflotte"));
        Speicheranlage(AnlageA, GeraetA, "Einzelspeicher A");
        Speicheranlage(AnlageB, GeraetB, "Einzelspeicher B");

        var variantenCtrl = new StromspeicherVarianteCtrl();
        StromspeicherVarianteModel zuerst = variantenCtrl.AktiveVarianteSicherstellen(Projekt);
        Assert.NotNull(zuerst);
        Assert.Equal(AnlageA, zuerst.ID_Energieanlage);

        SpeicherFlottenProjektCtrl.Aktivieren(Projekt, Aktivierungsergebnis());

        StromspeicherVarianteModel zweite = variantenCtrl.ReadAllByProjekt(Projekt)
            .Single(x => x.ID_Energieanlage == AnlageB);
        Assert.True(variantenCtrl.SetzeAktiv(Projekt, zweite.ID));
        Assert.Equal(AnlageB, SpeicherAuslegungCtrl.Anlage(Projekt));
        Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(Projekt));
        Assert.NotNull(SpeicherFlottenProjektCtrl.AktiveKonfiguration(Projekt));

        Sql("DELETE FROM Tab_Energieanlagen WHERE ID=?", new DbParam("@a", AnlageB));

        Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(Projekt));
        Assert.NotNull(SpeicherFlottenProjektCtrl.AktiveKonfiguration(Projekt));
        Assert.Single(SpeicherAuslegungCtrl.Profile(Projekt, 0),
            x => x.Name == SpeicherFlottenProjektCtrl.ProjektflottenStand);
    }

    private static void Speicheranlage(int anlage, int geraet, string name)
    {
        Sql("INSERT INTO Tab_Stromspeicher (ID,ID_Projekt,Bezeichner,Leistung,Energie) VALUES (?,?,?,?,?)",
            new DbParam("@g", geraet), new DbParam("@p", Projekt), new DbParam("@n", name),
            new DbParam("@l", 10d), new DbParam("@e", 20d));
        Sql("INSERT INTO Tab_Energieanlagen (ID,ID_Projekt,Bezeichner,ID_Type,ID_WP,ID_Kessel,ID_BHKW,ID_PV,ID_Solar,ID_SP,ID_PUFFER) " +
            "VALUES (?,?,?,?,NULL,NULL,NULL,NULL,NULL,?,NULL)", new DbParam("@a", anlage),
            new DbParam("@p", Projekt), new DbParam("@n", name), new DbParam("@t", WizardItemClass.SP_TYP),
            new DbParam("@g", geraet));
    }

    private static void Sql(string sql, params DbParam[] parameter)
    {
        using var db = DataRepository.Vorgang();
        db.Ausfuehren(sql, parameter);
        db.Commit();
    }

    private static SpeicherOptimierungEingaben Eingaben(bool aktiv) => new()
    {
        Auslegung = new SpeicherAuslegungKonfiguration
        {
            FlotteImProjektAktiv = aktiv,
            Flotte = new SpeicherEngine.FlottenStudieKonfiguration()
        }
    };

    private static SpeicherFlottenErgebnis Aktivierungsergebnis()
    {
        var flotte = new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit>
            {
                new()
                {
                    Id = "projekt-speicher", Name = "Projektweiter Speicher",
                    KapazitaetKWh = 20, LadeleistungKw = 10, EntladeleistungKw = 10,
                    Ladewirkungsgrad = 1, Entladewirkungsgrad = 1,
                    SocMin = 0, SocStart = .5, SocMax = 1
                }
            }
        };
        var eingaben = new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Lastquelle = SpeicherAuslegungQuelle.Epos,
                PvQuelle = SpeicherAuslegungQuelle.Keine,
                Preisquelle = SpeicherAuslegungQuelle.Epos,
                Flotte = SpeicherAuslegungKopie.Von(flotte)
            }
        };
        string konfigurationId = Convert.ToHexString(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(eingaben.Auslegung)));
        FlottenSimulationErgebnis Simulation() => new()
        {
            Zulaessig = true,
            KonfigurationId = konfigurationId,
            DatenId = "projektweite-testdaten",
            Intervalle = new List<FlottenIntervallErgebnis> { new() }
        };
        return new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Eingaben = eingaben,
            Konfiguration = flotte,
            Studie = new FlottenStudienErgebnis
            {
                ReferenzOhneSpeicher = Simulation(),
                Variante = Simulation()
            }
        };
    }
}
