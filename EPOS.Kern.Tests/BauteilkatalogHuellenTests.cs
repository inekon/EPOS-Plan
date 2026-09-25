using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Die zwei plattformfreien Hüllen der Gebäudesimulation G3 (<c>BaustoffKatalogHuelle</c>,
/// <c>BauteilaufbauHuelle</c> in EPOS.UI.Daten): Ihr Parametersatz trifft nur Parameter seiner
/// Komponente (ein fremder Schlüssel bräche beim ersten Zeichnen), und die Abbildung zwischen
/// DTO und Kernmodell hält die Schichtdicke (mm ↔ m), die Reihenfolge und die NULL-Semantik.
/// Ohne Datenbank — gerufen werden nur die Gaben selbst und die Abbildungen.
/// </summary>
public class BauteilkatalogHuellenTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

    public void Dispose() => _kultur.Dispose();

    private static void TrifftNurParameter<TKomponente>(IReadOnlyDictionary<string, object> gaben)
        where TKomponente : IComponent
    {
        var parameter = typeof(TKomponente)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .ToDictionary(p => p.Name, p => p.PropertyType);

        foreach ((string schluessel, object wert) in gaben)
        {
            Assert.True(parameter.ContainsKey(schluessel), typeof(TKomponente).Name + " kennt " + schluessel + " nicht.");
            Assert.True(wert is null || parameter[schluessel].IsInstanceOfType(wert),
                        schluessel + ": " + wert?.GetType().Name + " passt nicht auf " + parameter[schluessel].Name);
        }
    }

    [Fact]
    public void Der_Parametersatz_der_Baustoffe_trifft_die_Komponente()
    {
        IReadOnlyDictionary<string, object> gaben = BaustoffKatalogHuelle.Gaben();

        TrifftNurParameter<BaustoffKatalogDialog>(gaben);
        foreach (string pflicht in new[] { "Katalogzeilen", "Katalogprofil", "Lies", "Pruefen", "Speichern",
                                           "Loeschen", "Duplizieren", "Schloss", "Verwendung" })
            Assert.Contains(pflicht, gaben.Keys);
        Assert.DoesNotContain("Geschlossen", gaben.Keys);          // das setzt der Wirt
    }

    [Fact]
    public void Der_Parametersatz_der_Aufbauten_trifft_die_Komponente()
    {
        IReadOnlyDictionary<string, object> gaben = BauteilaufbauHuelle.Gaben();

        TrifftNurParameter<BauteilaufbauDialog>(gaben);
        foreach (string pflicht in new[] { "Katalogzeilen", "Katalogprofil", "Lies", "Baustoffe", "Bauteilarten",
                                           "Kennwerte", "Pruefen", "Speichern", "Loeschen", "Duplizieren", "Schloss" })
            Assert.Contains(pflicht, gaben.Keys);
        Assert.DoesNotContain("Geschlossen", gaben.Keys);
    }

    [Fact]
    public void Die_Bauteilarten_sind_die_neun_Persistenzwerte_und_fuer_jede()
    {
        IReadOnlyList<(string Wert, string Text)> arten = BauteilaufbauHuelle.Bauteilarten();

        Assert.Equal(10, arten.Count);
        Assert.Equal(("", "für jede Bauteilart"), arten[0]);
        Assert.Equal(DbWerte.BAUTEILARTEN, arten.Skip(1).Select(a => a.Wert));
        Assert.Contains(("AUSSENWAND", "Außenwand"), arten);
        Assert.DoesNotContain(arten, a => a.Text == a.Wert && a.Wert.Length > 0);   // Steuerwert ≠ Anzeigetext
    }

    /// <summary>
    /// <b>Die Schichtdicke reist in mm zur Oberfläche und in m zurück</b> — umgerechnet genau in
    /// der Hülle, über den Kern; Reihenfolge lückenlos ab 1, leere Texte werden NULL, „für jede
    /// Bauteilart" wird NULL.
    /// </summary>
    [Fact]
    public void Die_Abbildung_haelt_Dicke_Reihenfolge_und_NULL()
    {
        var daten = new BauteilaufbauDaten
        {
            Id = 12, Bezeichner = "Wand", Beschreibung = " ", Bauteilart = "",
            Schichten =
            {
                new BauteilschichtDaten { IdBaustoff = 1, DickeMm = 175, Lambda = 0.99, Rho = 1800, Cp = 1000 },
                new BauteilschichtDaten { DickeMm = 12.5, IstLuftschicht = true }
            }
        };

        BauteilaufbauModel m = BauteilaufbauHuelle.AlsModell(daten);

        Assert.Equal(12, m.ID);
        Assert.Null(m.Beschreibung);
        Assert.Null(m.Bauteilart);
        Assert.Equal(new[] { 1, 2 }, m.Schichten.Select(s => s.Reihenfolge));
        Assert.Equal(0.175, m.Schichten[0].Dicke);
        Assert.Equal(0.0125, m.Schichten[1].Dicke);
        Assert.Null(m.Schichten[1].Lambda);
        Assert.True(m.Schichten[1].IstLuftschicht);

        BauteilaufbauDaten zurueck = BauteilaufbauHuelle.AlsDaten(m);
        Assert.Equal(new double?[] { 175, 12.5 }, zurueck.Schichten.Select(s => s.DickeMm));
        Assert.Equal(1, zurueck.Schichten[0].IdBaustoff);
        Assert.Equal("", zurueck.Bauteilart);
    }

    [Fact]
    public void Der_Summenfuss_der_Huelle_ist_der_des_Kerns()
    {
        var daten = new BauteilaufbauDaten
        {
            Bezeichner = "Wand", Bauteilart = "AUSSENWAND",
            Schichten = { new BauteilschichtDaten { DickeMm = 200, Lambda = 0.04, Rho = 30, Cp = 1000 } }
        };

        AufbauKennwerteDaten k = BauteilaufbauHuelle.Kennwerte(daten);

        Assert.Equal(5.0, k.R_M2KW!.Value, 12);
        Assert.Equal(1.0 / (0.13 + 5.0 + 0.04), k.U_WM2K!.Value, 12);
        Assert.Equal(30 * 1000 * 0.2 / 1000.0, k.Kapazitaet_KJM2K!.Value, 9);
        Assert.Equal("", k.Grund);
    }

    [Fact]
    public void Die_Pruefung_der_Huelle_ist_die_des_Kerns()
    {
        Func<BauteilaufbauDaten, string> pruefen =
            (Func<BauteilaufbauDaten, string>)BauteilaufbauHuelle.Gaben()["Pruefen"];

        Assert.Null(pruefen(new BauteilaufbauDaten
        {
            Bezeichner = "Wand",
            Schichten = { new BauteilschichtDaten { DickeMm = 100, Lambda = 0.5, Rho = 1000, Cp = 1000 } }
        }));
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BAUTEIL_MSG_KEINE_SCHICHT,
                     pruefen(new BauteilaufbauDaten { Bezeichner = "Leer" }));

        Func<BaustoffDaten, string> stoff = (Func<BaustoffDaten, string>)BaustoffKatalogHuelle.Gaben()["Pruefen"];
        Assert.Null(stoff(new BaustoffDaten { Bezeichner = "Lehm", Lambda = 0.8 }));
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BAUSTOFF_MSG_NAME_LEER, stoff(new BaustoffDaten()));
    }
}
