using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Vormerkung der Bedarfsprofile wird erst NACH dem Anlegen der Zuordnungen abgeschlossen</b>
/// (Nachzug der Katalogauswahl, Konzept Katalogauswahl 5.13).
///
/// <para><b>Der Befund.</b> Stromverbraucher, Prozesswärme und Kältebedarf schreiben ihre Zuordnungen
/// erst der Aufrufer der Hülle (die Kachelwege der Startseite), nicht der Dialog. Der Löschweg des Kerns
/// (<c>BedarfStammCtrl.ProjektkopieEntfernen</c>) räumt eine Projektkopie nur ab, wenn in der DATENBANK
/// keine Zuordnung mehr auf sie zeigt. Wurde die Vormerkung vor dem Schreiben abgeschlossen, hielt die
/// noch stehende ALTE Zuordnung die Kopie einer entfernten Zeile fest: Die Kopie blieb als Waise im
/// Projekt, ohne Zeile und ohne Zuordnung.</para>
///
/// <para><b>Hier gemessen</b> an einer Ablage, die den Löschweg des Kerns nachbildet (Kopie nur ohne
/// Zuordnung), dazu eine Wache über die drei Kachelwege und den Abschluss der Hülle. Gegen die
/// Testdatenbank läuft derselbe Ablauf in <c>EPOS.Kern.Tests/BedarfsprofilZuordnungVormerkungTests</c>.</para>
/// </summary>
public class BedarfsprofilVormerkungWacheTests
{
    private const int KOPIE_A = 301;
    private const int KOPIE_B = 302;
    private const int KOPIE_C = 303;

    /// <summary>
    /// Die Datenbank im Kleinen: Projektkopien je Bezeichner, die Zuordnungen des Projekts und eine Spur
    /// der Schritte. <see cref="Entfernen"/> ist der Löschweg des Kerns — er räumt eine Kopie nur ab,
    /// wenn keine Zuordnung mehr auf sie zeigt.
    /// </summary>
    private sealed class Ablage
    {
        public readonly Dictionary<string, int> Kopien = new();
        public readonly List<(string Name, int Kopie)> Zuordnungen = new();
        public readonly List<string> Spur = new();

        public Ablage()
        {
            Kopien["Profil A"] = KOPIE_A;
            Kopien["Profil B"] = KOPIE_B;
            Zuordnungen.Add(("Profil A", KOPIE_A));
            Zuordnungen.Add(("Profil B", KOPIE_B));
        }

        /// <summary>Der Schreibweg des Aufrufers: Löschen und Neuanlegen der Zuordnungen.</summary>
        public void Schreiben(params (string Name, int Kopie)[] zeilen)
        {
            Spur.Add("schreiben");
            Zuordnungen.Clear();
            Zuordnungen.AddRange(zeilen);
        }

        /// <summary>Der Löschweg des Kerns (<c>BedarfStammCtrl.ProjektkopieEntfernen</c>).</summary>
        public void Entfernen(string bezeichner)
        {
            Spur.Add("entfernen:" + bezeichner);
            if (!Kopien.TryGetValue(bezeichner, out int kopie)) return;
            if (Zuordnungen.Exists(z => z.Kopie == kopie)) return;
            Kopien.Remove(bezeichner);
        }
    }

    /// <summary>Der Bestand beim Öffnen: zwei Zeilen, jede mit ihrer Projektkopie.</summary>
    private static IEnumerable<(string Bezeichner, int KopieId)> Bestand()
        => new[] { ("Profil A", KOPIE_A), ("Profil B", KOPIE_B) };

    /// <summary>Nach dem Schließen zeigt nur noch die Zeile „Profil B" auf ihre Kopie.</summary>
    private static Func<int, bool> NochReferenziert() => id => id == KOPIE_B;

    [Fact]
    public void Der_Abschluss_vor_dem_Schreiben_laesst_die_Kopie_als_Waise_stehen()
    {
        var a = new Ablage();
        var v = new Projektkopievormerkung(a.Entfernen);

        // Die alte Reihenfolge: erst abschließen, dann die Zuordnungen schreiben.
        v.EntferntGegenueber(Bestand(), NochReferenziert());
        v.Abschliessen(true, NochReferenziert());
        a.Schreiben(("Profil B", KOPIE_B));

        Assert.True(a.Kopien.ContainsKey("Profil A"));                       // die Kopie steht noch ...
        Assert.DoesNotContain(a.Zuordnungen, z => z.Kopie == KOPIE_A);       // ... ohne Zuordnung: Waise
    }

    [Fact]
    public void Schliessen_raeumt_die_Kopie_der_entfernten_Zeile_ab()
    {
        var a = new Ablage();
        var v = new Projektkopievormerkung(a.Entfernen);

        v.Schliessen(true, () => a.Schreiben(("Profil B", KOPIE_B)), Bestand(), NochReferenziert());

        Assert.Equal(new[] { "schreiben", "entfernen:Profil A" }, a.Spur);   // schreiben zuerst
        Assert.False(a.Kopien.ContainsKey("Profil A"));
        Assert.True(a.Kopien.ContainsKey("Profil B"));                       // die gebliebene Zeile behält ihre
    }

    [Fact]
    public void Abbrechen_schreibt_keine_Zuordnung_und_raeumt_nur_die_neue_Kopie_ab()
    {
        var a = new Ablage();
        var v = new Projektkopievormerkung(a.Entfernen);
        a.Kopien["Profil C"] = KOPIE_C;                                      // in der Sitzung übernommen
        v.Angelegt("Profil C", KOPIE_C);

        v.Schliessen(false, () => a.Schreiben(("Profil B", KOPIE_B)), Bestand(), NochReferenziert());

        Assert.DoesNotContain("schreiben", a.Spur);
        Assert.False(a.Kopien.ContainsKey("Profil C"));
        Assert.True(a.Kopien.ContainsKey("Profil A"));                       // entfernt wird nur beim OK
        Assert.Equal(2, a.Zuordnungen.Count);
    }

    /// <summary>Kachelweg der Startseite und der Aufruf, mit dem er seine Zuordnungen schreibt.</summary>
    public static TheoryData<string, string> Kachelwege() => new()
    {
        { "Prozesswaerme", "wizctrl.Add_Projekt_Prozess(_kontext.Id, liste);" },
        { "Standardlastprofil", "wizctrl.Add_Projekt_Stromverbraucher(_kontext.Id, liste);" },
        { "Kaeltebedarfsprofile", "KaeltebedarfsprofilAbbildung.Speichern(_kontext.Id, liste)" },
    };

    /// <summary>
    /// Die drei Kachelwege reichen ihren Schreibweg in die Hülle, statt ihn nach dem <c>Oeffnen</c> zu
    /// fahren — sonst stünde er wieder hinter dem Abschluss der Vormerkung.
    /// </summary>
    [Theory]
    [MemberData(nameof(Kachelwege))]
    public void Die_drei_Kachelwege_reichen_ihren_Schreibweg_in_die_Huelle(string weg, string schreibaufruf)
    {
        string rumpf = Methodenrumpf(Pfad("WindowsFormsApplication1/Views/Hauptformular/StartseiteHuelle.cs"),
                                     "private void " + weg + "(IWin32Window wirt)");

        Assert.DoesNotContain("if (BedarfsProfileHuelle.Oeffnen", rumpf);
        Assert.DoesNotContain("if (!BedarfsProfileHuelle.Oeffnen", rumpf);
        int oeffnen = rumpf.IndexOf("BedarfsProfileHuelle.Oeffnen(wirt, _kontext.Id, _kontext.Name, liste, () =>",
                                    StringComparison.Ordinal);
        Assert.True(oeffnen >= 0, weg + ": reicht seinen Schreibweg nicht in die Hülle.");
        int schreiben = rumpf.IndexOf(schreibaufruf, StringComparison.Ordinal);
        Assert.True(schreiben > oeffnen, weg + ": schreibt die Zuordnungen außerhalb des hereingereichten Wegs.");
    }

    /// <summary>Die Hülle schließt die Vormerkung über den einen Weg ab, der vorher schreiben lässt.</summary>
    [Fact]
    public void Die_Huelle_schliesst_die_Vormerkung_ueber_den_Schreibweg_ab()
    {
        string quelle = File.ReadAllText(Pfad("WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs"));

        Assert.Contains("vormerkung.Schliessen(ok, zuordnungenSchreiben, bestand,", quelle);
        Assert.DoesNotContain("vormerkung.Abschliessen(", quelle);
        Assert.DoesNotContain("vormerkung.EntferntGegenueber(", quelle);
    }

    /// <summary>Der Rumpf einer Methode — von ihrer Signatur bis zur nächsten Methode der Datei.</summary>
    private static string Methodenrumpf(string datei, string signatur)
    {
        string quelle = File.ReadAllText(datei);
        int start = quelle.IndexOf(signatur, StringComparison.Ordinal);
        Assert.True(start > 0, signatur + " steht nicht in " + Path.GetFileName(datei) + ".");
        int ende = quelle.IndexOf("\n        private ", start + signatur.Length, StringComparison.Ordinal);
        return ende < 0 ? quelle.Substring(start) : quelle.Substring(start, ende - start);
    }

    private static string Pfad(string relativ)
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !Directory.Exists(Path.Combine(ordner.FullName, "WindowsFormsApplication1", "Views")))
            ordner = ordner.Parent;
        Assert.True(ordner is not null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return Path.Combine(ordner!.FullName, relativ.Replace('/', Path.DirectorySeparatorChar));
    }
}
