using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPOS.UI.Seiten;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// K7 (iU9-W16c.0, Entscheide E-1/E-2) — <see cref="Seitenschluessel"/> ist die
/// EINE Schluesseltabelle beider Plattformen.
///
/// <para>Geprueft wird genau das, was die Zusammenlegung zusichert: Die Schluessel
/// sind sprachneutrales ASCII, sie sind untereinander verschieden, und die
/// uebernommenen Werte sind ZEICHENGLEICH mit <c>Masken</c> bzw.
/// <c>Ansichten</c> im Kern. Laeuft eines der beiden auseinander, oeffnet das
/// Menueband des Hauptfensters eine andere Maske als
/// <c>WinFormsNavigation.OeffneMaske</c>.</para>
///
/// <para>Der Zwilling im Kern ist
/// <c>DiensteTests.Navigationsschluessel_sind_sprachneutrales_ASCII</c>; er
/// prueft dieselbe Regel auf der Kernseite, wo EPOS.UI nicht sichtbar ist.</para>
///
/// <para>Keine Sprachbindung: Der Fall prueft ausschliesslich Zeichenketten,
/// keine Anzeigetexte.</para>
/// </summary>
public class SeitenschluesselTests
{
    /// <summary>Alle <c>public const string</c> einer Klasse, Name -&gt; Wert.</summary>
    private static IReadOnlyDictionary<string, string> Konstanten(Type typ)
    {
        return typ.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                  .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                  .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);
    }

    [Fact]
    public void Jeder_Seitenschluessel_ist_sprachneutrales_ASCII()
    {
        Assert.NotEmpty(Seitenschluessel.Alle);

        foreach (string s in Seitenschluessel.Alle)
        {
            Assert.False(string.IsNullOrWhiteSpace(s));
            foreach (char c in s)
                Assert.True(c < 128, "Nicht-ASCII in Schluessel '" + s + "'");
        }
    }

    [Fact]
    public void Die_Schluessel_sind_untereinander_verschieden()
    {
        // Zwei gleiche Werte hiessen: ein Menuepunkt oeffnet die Ansicht eines
        // anderen. Masken.Assistent und Seitenschluessel.Assistent tragen
        // BEWUSST denselben Wert - deshalb steht er nur einmal in Alle.
        var eindeutig = new HashSet<string>(Seitenschluessel.Alle, StringComparer.Ordinal);
        Assert.Equal(Seitenschluessel.Alle.Count, eindeutig.Count);
    }

    [Fact]
    public void Alle_zaehlt_jede_Konstante_der_Klasse_genau_einmal()
    {
        // Die Liste Alle ist die Grundlage von N4 und der beiden Faelle oben.
        // Waere sie unvollstaendig, pruefte sie an einem neuen Schluessel vorbei.
        IReadOnlyDictionary<string, string> konstanten = Konstanten(typeof(Seitenschluessel));

        var werte = new HashSet<string>(konstanten.Values, StringComparer.Ordinal);
        var inAlle = new HashSet<string>(Seitenschluessel.Alle, StringComparer.Ordinal);

        Assert.Equal(werte.OrderBy(w => w, StringComparer.Ordinal),
                     inAlle.OrderBy(w => w, StringComparer.Ordinal));
    }

    [Fact]
    public void Die_uebernommenen_Werte_sind_zeichengleich_mit_dem_Kern()
    {
        // E-2: Seitenschluessel ERBT die Werte, es schreibt sie nicht ab. Der
        // Fall haelt beide Klassen Feld fuer Feld gegeneinander.
        IReadOnlyDictionary<string, string> ui = Konstanten(typeof(Seitenschluessel));
        IReadOnlyDictionary<string, string> kern = Konstanten(typeof(Masken));

        Assert.NotEmpty(kern);

        foreach (KeyValuePair<string, string> eintrag in kern)
        {
            Assert.True(ui.ContainsKey(eintrag.Key),
                        "Masken." + eintrag.Key + " fehlt in Seitenschluessel (K7).");
            Assert.Equal(eintrag.Value, ui[eintrag.Key]);
        }

        Assert.Equal(Ansichten.BerichteKosten, Seitenschluessel.BerichteKosten);
        Assert.Equal(Ansichten.Varianten, Seitenschluessel.Varianten);
    }

    [Fact]
    public void Die_drei_neuen_Schluessel_des_Rahmens_stehen()
    {
        // W16c.0 legt STARTSEITE und BERICHTE_KOSTEN neu an; ASSISTENT steht
        // seit W16a.5. Die drei sind die Ansichten, die AppWurzel mit W16c.2
        // bekommt (E-1).
        Assert.Equal("STARTSEITE", Seitenschluessel.Startseite);
        Assert.Equal("BERICHTE_KOSTEN", Seitenschluessel.BerichteKosten);
        Assert.Equal("ASSISTENT", Seitenschluessel.Assistent);
    }
}
