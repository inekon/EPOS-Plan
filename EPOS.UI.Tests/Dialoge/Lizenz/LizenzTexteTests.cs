using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using EPOS.UI.Dialoge.Lizenz;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Lizenz;

/// <summary>
/// <see cref="LizenzTexte"/> — Zeuge des Bündels aus W15c-O-2 (04.09.2026).
///
/// <para><b>Die eine Zusage.</b> Das Bündel füllt sich OHNE Angabe selbst aus
/// <c>MyResource</c>, und zwar in der jeweils eingestellten Oberflächensprache.
/// Damit ist der Umbau von 18 + 29 Einzelparametern auf einen Parameter
/// wirklich eine Aufräumarbeit: Die Hüllen setzen keinen Text mehr einzeln, und
/// trotzdem steht in jeder Maske derselbe Katalogeintrag wie vorher.</para>
///
/// <para>Geprüft wird das über ALLE Eigenschaften beider Sätze — nicht an
/// Stichproben —, damit ein künftig vergessener Schlüssel auffällt statt eine
/// leere Beschriftung zu hinterlassen. Die Bauart folgt
/// <c>MenuebandTests.Jede_Beschriftung_steht_in_MyResource_und_zwar_zweisprachig</c>
/// (W16c).</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8) und stellt sie am
/// Ende wieder her — sie schaltet als einzige des Ordners auf Englisch um.</para>
/// </summary>
public class LizenzTexteTests : IDisposable
{
    /// <summary>
    /// Die EINZIGE Eigenschaft, die auf Deutsch mit Absicht leer ist: Der
    /// Sprachhinweis über den Rechtstexten steht nur im englischen Zweig
    /// (Entscheid W15c-E-7 — verbindlich ist allein die deutsche Fassung).
    /// </summary>
    private const string NUR_ENGLISCH = nameof(LizenzTexte.SprachHinweis);

    public LizenzTexteTests() => Kultur("de-DE");

    public void Dispose()
    {
        Kultur("de-DE");
        GC.SuppressFinalize(this);
    }

    private static void Kultur(string name)
    {
        var kultur = new CultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = kultur;
        CultureInfo.DefaultThreadCurrentUICulture = kultur;
        Thread.CurrentThread.CurrentCulture = kultur;
        Thread.CurrentThread.CurrentUICulture = kultur;
        CultureInfo.CurrentCulture = kultur;
        CultureInfo.CurrentUICulture = kultur;
    }

    /// <summary>Alle Zeichenketten-Eigenschaften eines Satzes, nach Namen.</summary>
    private static IReadOnlyDictionary<string, string> Werte(object satz)
        => satz.GetType()
               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(e => e.PropertyType == typeof(string))
               .ToDictionary(e => e.Name, e => (string)(e.GetValue(satz) ?? ""), StringComparer.Ordinal);

    /// <summary>
    /// <b>Ohne Angabe steht der Katalogtext da</b> — auf Deutsch und auf Englisch,
    /// in beiden Sätzen des Bündels.
    /// </summary>
    [Fact]
    public void Das_Buendel_fuellt_sich_ohne_Angabe_aus_MyResource_in_de_und_en()
    {
        Kultur("de-DE");
        var deutsch = Werte(new LizenzTexte());
        var deutschVerwaltung = Werte(new LizenzTexte().Verwaltung);

        Kultur("en-US");
        var englisch = Werte(new LizenzTexte());
        var englischVerwaltung = Werte(new LizenzTexte().Verwaltung);

        Kultur("de-DE");

        // ---- Vollzaehligkeit: kein Schluessel bleibt leer --------------------
        foreach (string name in deutsch.Keys)
        {
            if (name != NUR_ENGLISCH)
                Assert.False(string.IsNullOrWhiteSpace(deutsch[name]), name + " ohne deutschen Text.");
            Assert.False(string.IsNullOrWhiteSpace(englisch[name]), name + " ohne englischen Text.");
        }

        foreach (string name in deutschVerwaltung.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(deutschVerwaltung[name]),
                         "Verwaltung." + name + " ohne deutschen Text.");
            Assert.False(string.IsNullOrWhiteSpace(englischVerwaltung[name]),
                         "Verwaltung." + name + " ohne englischen Text.");
        }

        // ---- Stichproben, woertlich aus Resource.resx bzw. .en-US.resx ------
        Assert.Equal("Lizenz und rechtliche Hinweise", deutsch[nameof(LizenzTexte.KopfTitel)]);
        Assert.Equal("License and legal information", englisch[nameof(LizenzTexte.KopfTitel)]);
        Assert.Equal("Lizenzvereinbarung", deutsch[nameof(LizenzTexte.ReiterVertrag)]);
        Assert.Equal("License agreement", englisch[nameof(LizenzTexte.ReiterVertrag)]);
        Assert.Equal("Lizenz aktivieren...", deutsch[nameof(LizenzTexte.KnopfAktivieren)]);
        Assert.Equal("Activate license...", englisch[nameof(LizenzTexte.KnopfAktivieren)]);

        Assert.Equal("Lizenzstatus auf diesem Arbeitsplatz",
                     deutschVerwaltung[nameof(LizenzVerwaltungTexte.GruppeStatus)]);
        Assert.Equal("License status on this workstation",
                     englischVerwaltung[nameof(LizenzVerwaltungTexte.GruppeStatus)]);
        Assert.Equal("Jetzt aktivieren", deutschVerwaltung[nameof(LizenzVerwaltungTexte.KnopfAktivieren)]);
        Assert.Equal("Activate now", englischVerwaltung[nameof(LizenzVerwaltungTexte.KnopfAktivieren)]);
        Assert.Equal("Ja", deutschVerwaltung[nameof(LizenzVerwaltungTexte.Ja)]);
        Assert.Equal("Yes", englischVerwaltung[nameof(LizenzVerwaltungTexte.Ja)]);

        // ---- E-7: der Sprachhinweis steht NUR im englischen Zweig ------------
        Assert.Equal("", deutsch[NUR_ENGLISCH]);
        Assert.Equal("Binding version in German.", englisch[NUR_ENGLISCH]);

        // ---- Und eine gesetzte Eigenschaft schlaegt den Katalog --------------
        Assert.Equal("Lizenz — EPOS-Plan",
                     new LizenzTexte { Verwaltung = { Titel = "Lizenz — EPOS-Plan" } }.Verwaltung.Titel);
    }
}
