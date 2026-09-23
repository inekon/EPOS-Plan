using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache zur Hausregel <b>„Eine Fußleiste, ein primärer Knopf, und er
/// steht zuletzt"</b> (Anwenderentscheid 19.09.2026: „Prüfe alle Dialoge [des
/// Administrationsmenüs] auf Überlappung und Übersichtlichkeit/Anordnung Buttons.";
/// Konzept <c>Dokumentation/aktuell/Konzept_Knopfleisten_Administration_EPOS-Plan.md</c>,
/// Schritt 0).
///
/// <para><b>Die Regel</b> — Quelle ist <c>EPOS.UI/CLAUDE.md</c>, Abschnitt
/// „Bedienung", und Abschnitt 1 des Konzepts:</para>
/// <list type="number">
///   <item><description>Jeder Dialog trägt <b>genau eine</b> Fußleiste. Eine zweite
///     Knopfleiste unmittelbar über ihr ist keine Blattleiste mehr, sondern ein
///     zweiter Fuß — ihre Knöpfe gehören in den Aktionsschlitz der einen Leiste
///     (<c>SpeichernLeiste.Aktionen</c>).</description></item>
///   <item><description>Die Fußleiste trägt <b>genau einen</b>
///     <c>epos-knopf--primaer</c>.</description></item>
///   <item><description>Dieser primäre Knopf ist der <b>letzte</b> Knopf der Leiste
///     — der Schlussknopf (OK bzw. Beenden) und sonst keiner.</description></item>
///   <item><description>Steht ein <b>Abbrechen</b>-Knopf in der Leiste, steht er
///     <b>unmittelbar vor</b> dem primären Knopf.</description></item>
/// </list>
///
/// <para><b>Was als Fußleiste gilt:</b> die LETZTE Knopfleiste der Komponente —
/// entweder ein <c>&lt;div class="epos-leiste"&gt;</c> oder ein
/// <c>&lt;SpeichernLeiste&gt;</c>. Nicht mitgezählt wird, was in einer
/// <c>&lt;Ueberlagerung&gt;</c> oder einem <c>&lt;Reiter&gt;</c> steckt: Das sind
/// Unterdialoge und Blattleisten, und in beinahe jedem Dialog des Hauses stehen die
/// Überlagerungen im Quelltext NACH der Fußleiste (Muster „Das Kreuz steht beim
/// Titel", vgl. <see cref="SchliesskreuzWacheTests"/>). Eine Knopfzeile in einer
/// Spalte oder einem Reiterblatt gilt nach Abschnitt 1 des Konzepts ausdrücklich
/// nicht als zweite Fußleiste.</para>
///
/// <para><b>Ein <c>&lt;SpeichernLeiste&gt;</c>-Fuß erfüllt die Regeln 2 bis 4 von
/// selbst</b> — der Baustein zeichnet Aktionen · Status · [Speichern] · Abbrechen ·
/// OK(primär) und sonst nichts (<c>Bausteine/SpeichernLeisteTests</c>). Für ihn
/// prüft die Wache nur Regel 1.</para>
///
/// <para><b>Geltungsbereich</b> sind die <c>*Dialog.razor</c> unter
/// <c>EPOS.UI/Dialoge/</c> — dieselbe Auswahl wie in
/// <see cref="SchliesskreuzWacheTests"/>, Fall 1. Eingebettete Feldblöcke
/// (<c>PvStraengeFelder</c>, <c>BrennstoffBestandteile</c>,
/// <c>EnergietraegerEinstellungen</c>, <c>SpeicherAuslegungEditor</c>,
/// <c>WertAbfrage</c>) haben keinen Fuß; ihre letzte Leiste ist eine Blattleiste
/// ihres Wirts und trägt deshalb zu Recht keinen primären Knopf.</para>
///
/// <para><b>Die Grenze der Wache:</b> Sie liest Markup, keinen kompilierten Baum.
/// Razor-Verzweigungen im Fuß löst sie auf, indem sie jede <c>@@if … else …</c>
/// getrennt als eigene <b>Variante</b> prüft (ohne das zählte der
/// <c>LizenzDialog</c> zwei primäre Knöpfe, obwohl seine beiden in
/// SICH AUSSCHLIESSENDEN Zweigen stehen). Ob ein Knopf „Abbrechen" ist, erkennt sie
/// an seiner Beschriftung; der primäre Schlussknopf ist davon ausgenommen, weil er
/// seinen Text wechseln darf (<c>KapitalwertVerlaufDialog</c>: „Abbrechen", solange
/// gerechnet wird, sonst „Schließen").</para>
/// </summary>
public sealed class KnopfleistenWacheTests
{
    // =====================================================================
    //  Die Regeln als Kennungen - sie stehen so in der Ausnahmeliste
    // =====================================================================

    private const string REGEL_EINE_LEISTE = "genau eine Fussleiste";
    private const string REGEL_EIN_PRIMAER = "genau ein primaerer Knopf";
    private const string REGEL_PRIMAER_ZULETZT = "der primaere Knopf steht zuletzt";
    private const string REGEL_ABBRECHEN_DAVOR = "Abbrechen steht unmittelbar vor dem primaeren Knopf";

    // =====================================================================
    //  Die Ausnahmeliste
    // =====================================================================

    /// <summary>
    /// Eine benannte Ausnahme: <paramref name="Datei"/> ist der repo-relative Pfad
    /// mit Schrägstrichen, <paramref name="Regel"/> die eine Regel, die diese Datei
    /// heute verletzt (eine der vier <c>REGEL_*</c>-Kennungen).
    /// </summary>
    private readonly record struct Ausnahme(string Datei, string Regel, string Grund);

    /// <summary>
    /// <b>Die Ausnahmeliste — sie ist leer.</b> Jede Fußleiste des Bestands folgt
    /// der Hausregel; die Wache steht allein.
    ///
    /// <para>Wer eine Leiste baut, die eine der vier Regeln verletzt, trägt sie
    /// hier mit <b>Grund und Termin</b> ein — und streicht den Eintrag, sobald die
    /// Leiste umgebaut ist. Eine Ausnahme, die ins Leere zeigt, lässt
    /// <see cref="Jede_Ausnahme_verletzt_heute_wirklich"/> rot werden; sie wird
    /// GESTRICHEN, nicht umgeschrieben.</para>
    /// </summary>
    private static readonly Ausnahme[] AUSNAHMEN =
    {
    };

    /// <summary>Die Ausnahmen, so wie die Wache sie prüft.</summary>
    private static Ausnahme[] AlleAusnahmen() => AUSNAHMEN;

    // =====================================================================
    //  Die Wache
    // =====================================================================

    /// <summary>Ein Fund: eine Fußleiste, die eine der vier Regeln verletzt.</summary>
    private readonly record struct Fund(string Datei, int Zeile, string Regel, string Bild);

    [Fact]
    public void Jede_Fussleiste_folgt_der_Hausregel()
    {
        List<Fund> funde = Funde(Dialogdateien(), AlleAusnahmen());

        Assert.True(funde.Count == 0,
            "Diese Fussleisten folgen nicht der Hausregel (eine Fussleiste; genau ein "
            + "epos-knopf--primaer; er steht zuletzt; ein Abbrechen-Knopf steht "
            + "unmittelbar davor). Entweder die Leiste umbauen oder die Stelle mit "
            + "Grund und Termin in AUSNAHMEN aufnehmen:\n"
            + Bericht(funde));
    }

    /// <summary>
    /// Die Gegenprobe (Lehre W6‑B‑1: eine Wache, die nie rot werden kann, prüft
    /// nichts): Jede der vier Regeln muss an einem gebauten Gegenbeispiel greifen —
    /// und die regelkonforme Leiste, die Blattleiste im Reiter, die Leiste in der
    /// nachgestellten Überlagerung und die Razor-Verzweigung mit je einem primären
    /// Knopf je Zweig dürfen nichts melden.
    /// </summary>
    [Fact]
    public void Die_Wache_findet_jede_der_vier_Verletzungen()
    {
        string Knopf(string text, bool primaer = false)
            => "        <button type=\"button\" class=\"epos-knopf"
               + (primaer ? " epos-knopf--primaer" : "") + "\"\n"
               + "                @onclick=\"() => Tu()\">" + text + "</button>\n";

        string Leiste(params string[] knoepfe)
            => "    <div class=\"epos-leiste\">\n" + string.Concat(knoepfe) + "    </div>\n";

        var dateien = new Dictionary<string, string>
        {
            // (1) zwei Leisten unmittelbar uebereinander
            ["EPOS.UI/Dialoge/X/StapelDialog.razor"] =
                Leiste(Knopf("@BtnNeuText")) + "\n    @* dazwischen nur ein Kommentar *@\n"
                + Leiste(Knopf("@AbbrechenText"), Knopf("@OkText", true)),

            // (2) kein primaerer Knopf
            ["EPOS.UI/Dialoge/X/BlassDialog.razor"] =
                Leiste(Knopf("@BtnExportText"), Knopf("@BtnSchliessenText")),

            // (2b) zwei primaere Knoepfe im SELBEN Zweig
            ["EPOS.UI/Dialoge/X/DoppeltDialog.razor"] =
                Leiste(Knopf("@BtnUebernehmenText", true), Knopf("@OkText", true)),

            // (3) der primaere Knopf steht nicht zuletzt
            ["EPOS.UI/Dialoge/X/VorneDialog.razor"] =
                Leiste(Knopf("@BtnSpeichernText", true), Knopf("@BtnBeendenText")),

            // (4) Abbrechen steht nicht unmittelbar vor dem primaeren Knopf
            ["EPOS.UI/Dialoge/X/FernDialog.razor"] =
                Leiste(Knopf("@AbbrechenText"), Knopf("@SpeichernText"), Knopf("@OkText", true))
        };

        List<Fund> funde = Funde(dateien, Array.Empty<Ausnahme>());

        Assert.Equal(
            new[]
            {
                ("EPOS.UI/Dialoge/X/BlassDialog.razor", REGEL_EIN_PRIMAER),
                ("EPOS.UI/Dialoge/X/DoppeltDialog.razor", REGEL_EIN_PRIMAER),
                ("EPOS.UI/Dialoge/X/FernDialog.razor", REGEL_ABBRECHEN_DAVOR),
                ("EPOS.UI/Dialoge/X/StapelDialog.razor", REGEL_EINE_LEISTE),
                ("EPOS.UI/Dialoge/X/VorneDialog.razor", REGEL_PRIMAER_ZULETZT)
            },
            funde.Select(f => (f.Datei, f.Regel)).ToArray());

        // Und die stillen Faelle - keiner davon ist ein Fund.
        var still = new Dictionary<string, string>
        {
            // regelkonform: Aktionen, Abbrechen, OK(primaer)
            ["EPOS.UI/Dialoge/X/GutDialog.razor"] =
                Leiste(Knopf("@BtnGrafikText"), Knopf("@AbbrechenText"), Knopf("@OkText", true)),

            // Blattleiste im Reiter + Fuss: die Blattleiste zaehlt nicht mit
            ["EPOS.UI/Dialoge/X/ReiterDialog.razor"] =
                "    <Reiter>\n        <KindInhalt>\n"
                + Leiste(Knopf("@BtnUebernehmenText", true))
                + "        </KindInhalt>\n    </Reiter>\n"
                + Leiste(Knopf("@AbbrechenText"), Knopf("@OkText", true)),

            // Ueberlagerung NACH dem Fuss (Hausmuster) - ihre Leiste zaehlt nicht
            ["EPOS.UI/Dialoge/X/UeberDialog.razor"] =
                Leiste(Knopf("@AbbrechenText"), Knopf("@OkText", true))
                + "    <Ueberlagerung Offen=\"@_offen\" Titel=\"@Kopf\">\n        <KindInhalt>\n"
                + Leiste(Knopf("@BtnJaText", true))
                + "        </KindInhalt>\n    </Ueberlagerung>\n",

            // zwei primaere Knoepfe, aber in SICH AUSSCHLIESSENDEN Zweigen (LizenzDialog)
            ["EPOS.UI/Dialoge/X/ZweigDialog.razor"] =
                "    <div class=\"epos-leiste\">\n"
                + Knopf("@BtnDruckenText")
                + "        @if (Zustimmungsmodus)\n        {\n"
                + Knopf("@BtnAblehnenText") + Knopf("@BtnZustimmenText", true)
                + "        }\n        else\n        {\n"
                + Knopf("@BtnSchliessenText", true)
                + "        }\n    </div>\n",

            // der primaere Schlussknopf DARF \"Abbrechen\" heissen (KapitalwertVerlaufDialog)
            ["EPOS.UI/Dialoge/X/WechselDialog.razor"] =
                Leiste("        <button type=\"button\" class=\"epos-knopf epos-knopf--primaer\"\n"
                       + "                @onclick=\"BeiSchliessen\">"
                       + "@(_laeuft ? AbbrechenText : SchliessenText)</button>\n"),

            // eine SpeichernLeiste als Fuss erfuellt die Regeln 2 bis 4 von selbst
            ["EPOS.UI/Dialoge/X/BausteinDialog.razor"] =
                "    <Gruppenkopf><KindInhalt>\n" + Leiste(Knopf("@BtnNeuText"))
                + "    </KindInhalt></Gruppenkopf>\n\n"
                + "    <SpeichernLeiste OkText=\"@OkText\" Ergebnis=\"BeiErgebnis\" />\n"
        };

        Assert.Empty(Funde(still, Array.Empty<Ausnahme>()));

        // Dieselbe Stelle, begruendet in der Ausnahmeliste, meldet nichts mehr.
        Assert.Empty(Funde(
            new Dictionary<string, string>
            {
                ["EPOS.UI/Dialoge/X/VorneDialog.razor"] = dateien["EPOS.UI/Dialoge/X/VorneDialog.razor"]
            },
            new[] { new Ausnahme("EPOS.UI/Dialoge/X/VorneDialog.razor", REGEL_PRIMAER_ZULETZT, "Probe") }));
    }

    /// <summary>
    /// Eine Ausnahme wirkt nur in IHRER Datei und nur für IHRE Regel: dieselbe Regel
    /// in einer anderen Datei deckt sie nicht zu.
    /// </summary>
    [Fact]
    public void Eine_Ausnahme_deckt_nur_ihre_eigene_Stelle()
    {
        const string fund =
            "    <div class=\"epos-leiste\">\n"
            + "        <button type=\"button\" class=\"epos-knopf epos-knopf--primaer\">@SpeichernText</button>\n"
            + "        <button type=\"button\" class=\"epos-knopf\">@BeendenText</button>\n"
            + "    </div>\n";

        var dateien = new Dictionary<string, string>
        {
            ["EPOS.UI/Dialoge/X/ADialog.razor"] = fund,
            ["EPOS.UI/Dialoge/X/BDialog.razor"] = fund
        };

        Fund treffer = Assert.Single(Funde(dateien,
            new[] { new Ausnahme("EPOS.UI/Dialoge/X/ADialog.razor", REGEL_PRIMAER_ZULETZT, "Probe") }));
        Assert.Equal("EPOS.UI/Dialoge/X/BDialog.razor", treffer.Datei);

        // Die falsche Regel deckt nichts zu.
        Assert.Equal(2, Funde(dateien,
            new[] { new Ausnahme("EPOS.UI/Dialoge/X/ADialog.razor", REGEL_EINE_LEISTE, "Probe") }).Count);
    }

    /// <summary>
    /// Jede Ausnahme muss GREIFEN: Die Datei gibt es, und ohne die Ausnahme wäre sie
    /// heute genau EIN Fund mit genau dieser Regel. Eine Ausnahme, die ins Leere
    /// zeigt, ist entweder erledigt (dann gehört sie gestrichen — das ist der
    /// Abschluss jedes Folgeauftrags DL-2b…k) oder falsch geschrieben (dann deckt sie
    /// nichts).
    /// </summary>
    [Fact]
    public void Jede_Ausnahme_verletzt_heute_wirklich()
    {
        Dictionary<string, string> dateien = Dialogdateien();
        var leerlauf = new List<string>();

        foreach (Ausnahme a in AlleAusnahmen())
        {
            if (!dateien.TryGetValue(a.Datei, out string? inhalt))
            {
                leerlauf.Add("  " + a.Datei + "  " + a.Regel + "  (Datei fehlt)");
                continue;
            }

            List<Fund> funde = Funde(
                new Dictionary<string, string> { [a.Datei] = inhalt }, Array.Empty<Ausnahme>());
            int treffer = funde.Count(f => f.Regel == a.Regel);

            if (treffer != 1)
                leerlauf.Add("  " + a.Datei + "  " + a.Regel + "  (Treffer: " + treffer
                             + (funde.Count > 0
                                ? "; gefunden: " + string.Join(", ", funde.Select(f => f.Regel))
                                : "")
                             + ")");
        }

        Assert.True(leerlauf.Count == 0,
            "Diese Ausnahmen greifen nicht mehr genau einmal - streichen oder berichtigen. "
            + "Eine erledigte Ausnahme wird GESTRICHEN, nicht umgeschrieben:\n"
            + string.Join("\n", leerlauf));
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Wache_liest_tatsaechlich_die_Dialoge()
    {
        Dictionary<string, string> dateien = Dialogdateien();
        Assert.True(dateien.Count > 60, "Es sind " + dateien.Count + " Dialoge.");

        // In jedem Dialog, der ueberhaupt eine Knopfleiste zeichnet, MUSS der Parser
        // auch eine finden - sonst prueft die Wache still nichts.
        var blind = dateien
            .Where(d => Regex.IsMatch(OhneUnterbereiche(d.Value),
                                      "<div class=\"epos-leiste[ \"]|<SpeichernLeiste\\b"))
            .Where(d => Leisten(OhneUnterbereiche(d.Value)).Count == 0)
            .Select(d => d.Key)
            .ToList();
        Assert.True(blind.Count == 0, "Leiste nicht erkannt in:\n" + string.Join("\n", blind));

        // Und das konforme Vorbild des Konzepts steht wirklich drin.
        Assert.Contains("EPOS.UI/Dialoge/Klimadaten/KlimadatenDialog.razor", dateien.Keys);
        List<Leistenblock> ls = Leisten(OhneUnterbereiche(
            dateien["EPOS.UI/Dialoge/Klimadaten/KlimadatenDialog.razor"]));
        // Seit Stufe 4 der Neuordnung: "Import…" und "Beenden" (Loeschen steht in der
        // Auswahlleiste, das Einlesen in der Ueberlagerung).
        Assert.Equal(2, Knoepfe(ls[^1].Text).Count);
        Assert.True(Knoepfe(ls[^1].Text)[^1].Primaer);
    }

    // =====================================================================
    //  Der Kern: Funde je Datei
    // =====================================================================

    private static List<Fund> Funde(IReadOnlyDictionary<string, string> dateien,
                                    IReadOnlyList<Ausnahme> ausnahmen)
    {
        var funde = new List<Fund>();

        foreach (KeyValuePair<string, string> datei in dateien.OrderBy(d => d.Key, StringComparer.Ordinal))
        {
            string s = OhneUnterbereiche(datei.Value);
            List<Leistenblock> leisten = Leisten(s);
            if (leisten.Count == 0) continue;

            Leistenblock fuss = leisten[^1];
            var regeln = new List<string>();

            // Regel 1 - genau eine Fussleiste: Steht die vorletzte Leiste unmittelbar
            // ueber dem Fuss (nur Leerraum und Razor-Kommentare dazwischen), ist sie
            // keine Blattleiste mehr, sondern ein zweiter Fuss.
            if (leisten.Count > 1
                && NurLeerraum(s[leisten[^2].Ende..fuss.Start]))
                regeln.Add(REGEL_EINE_LEISTE);

            // Regeln 2 bis 4 - nur fuer eine selbst gezeichnete Leiste. Der Baustein
            // SpeichernLeiste erfuellt sie von selbst.
            if (!fuss.IstBaustein)
                foreach (string variante in Varianten(fuss.Text))
                {
                    List<Knopf> knoepfe = Knoepfe(variante);
                    if (knoepfe.Count == 0) continue;

                    if (knoepfe.Count(k => k.Primaer) != 1)
                    {
                        regeln.Add(REGEL_EIN_PRIMAER);
                        continue;
                    }

                    if (!knoepfe[^1].Primaer)
                    {
                        regeln.Add(REGEL_PRIMAER_ZULETZT);
                        continue;
                    }

                    // Der primaere Schlussknopf darf selbst "Abbrechen" heissen
                    // (KapitalwertVerlaufDialog) - geprueft werden nur die anderen.
                    for (int i = 0; i < knoepfe.Count; i++)
                        if (!knoepfe[i].Primaer && knoepfe[i].Abbrechen && i != knoepfe.Count - 2)
                            regeln.Add(REGEL_ABBRECHEN_DAVOR);
                }

            foreach (string regel in regeln.Distinct())
            {
                if (ausnahmen.Any(a => a.Datei == datei.Key && a.Regel == regel)) continue;
                funde.Add(new Fund(datei.Key, 1 + Zeilenzahl(s, fuss.Start), regel,
                                   Bild(fuss)));
            }
        }

        return funde;
    }

    // =====================================================================
    //  Kleinwerkzeug
    // =====================================================================

    /// <summary>Eine gefundene Knopfleiste.</summary>
    private readonly record struct Leistenblock(int Start, int Ende, string Text, bool IstBaustein);

    /// <summary>Ein Knopf der Leiste.</summary>
    private readonly record struct Knopf(bool Primaer, bool Abbrechen, string Text);

    /// <summary>
    /// Blendet aus, was NICHT zur Komponente selbst gehört: den Inhalt jeder
    /// <c>&lt;Ueberlagerung&gt;</c> (Unterdialoge, im Quelltext fast immer NACH dem
    /// Fuß) und jedes <c>&lt;Reiter&gt;</c> (Blattleisten). Die Zeichen werden durch
    /// Leerzeichen ersetzt, Zeilenumbrüche bleiben stehen — so stimmen Indizes und
    /// Zeilennummern weiter mit der Quelldatei überein.
    /// </summary>
    private static string OhneUnterbereiche(string s)
    {
        char[] a = s.ToCharArray();

        foreach (string tag in new[] { "Ueberlagerung", "Reiter" })
        {
            foreach (Match m in Regex.Matches(s, "<" + tag + @"\b"))
            {
                if (a[m.Index] == ' ') continue;          // schon ausgeblendet

                int ende = PassendesEndtag(s, m.Index, tag);
                if (ende < 0) continue;

                for (int i = m.Index; i < ende; i++)
                    if (a[i] != '\n') a[i] = ' ';
            }
        }

        return new string(a);
    }

    /// <summary>Das Ende des <c>&lt;/Tag&gt;</c>, das zu diesem Starttag gehört (Schachtelung mitgezählt).</summary>
    private static int PassendesEndtag(string s, int start, string tag)
    {
        int tiefe = 0, i = start;
        while (i < s.Length)
        {
            int auf = s.IndexOf("<" + tag, i, StringComparison.Ordinal);
            int zu = s.IndexOf("</" + tag + ">", i, StringComparison.Ordinal);
            if (zu < 0) return -1;

            // "<Reiterblatt" ist kein "<Reiter": nur ein echtes Wortende zaehlt.
            while (auf >= 0 && auf + 1 + tag.Length < s.Length
                   && char.IsLetterOrDigit(s[auf + 1 + tag.Length]))
                auf = s.IndexOf("<" + tag, auf + 1, StringComparison.Ordinal);

            if (auf >= 0 && auf < zu) { tiefe++; i = auf + 1 + tag.Length; continue; }

            tiefe--;
            i = zu + tag.Length + 3;
            if (tiefe == 0) return i;
        }
        return -1;
    }

    /// <summary>
    /// Alle Knopfleisten der Reihe nach: <c>&lt;div class="epos-leiste…"&gt;</c>
    /// (mit passendem <c>&lt;/div&gt;</c>) und jedes <c>&lt;SpeichernLeiste&gt;</c>.
    /// </summary>
    private static List<Leistenblock> Leisten(string s)
    {
        var res = new List<Leistenblock>();

        foreach (Match m in Regex.Matches(s, "<div class=\"epos-leiste(?:\\s[^\"]*)?\""))
        {
            int auf = s.IndexOf('>', m.Index);
            if (auf < 0) continue;

            int tiefe = 1, j = auf + 1;
            while (tiefe > 0)
            {
                int a = s.IndexOf("<div", j, StringComparison.Ordinal);
                int b = s.IndexOf("</div>", j, StringComparison.Ordinal);
                if (b < 0) break;
                if (a >= 0 && a < b) { tiefe++; j = a + 4; }
                else { tiefe--; j = b + 6; }
            }

            res.Add(new Leistenblock(m.Index, j, s[m.Index..j], false));
        }

        foreach (Match m in Regex.Matches(s, @"<SpeichernLeiste\b"))
        {
            int auf = s.IndexOf('>', m.Index);
            if (auf < 0) continue;
            res.Add(new Leistenblock(m.Index, auf + 1, "", true));
        }

        res.Sort((x, y) => x.Start.CompareTo(y.Start));
        return res;
    }

    /// <summary>Zwischen zwei Leisten steht nichts als Leerraum und Razor-Kommentare?</summary>
    private static bool NurLeerraum(string s)
        => Regex.Replace(s, @"@\*.*?\*@", "", RegexOptions.Singleline).Trim().Length == 0;

    /// <summary>
    /// Jede <c>@@if (…) { … } [else { … }]</c> im Leistentext wird zu ZWEI Varianten
    /// aufgelöst — dem Zweig und seiner Alternative (ohne <c>else</c>: dem leeren
    /// Zweig). Geschachtelte Verzweigungen lösen sich dabei rekursiv mit auf.
    /// </summary>
    private static List<string> Varianten(string blk)
    {
        Match m = Regex.Match(blk, @"@if\s*\(");
        if (!m.Success) return new List<string> { blk };

        int rund = KlammerEnde(blk, m.Index + m.Length - 1, '(', ')');
        if (rund < 0) return new List<string> { blk };

        int auf = blk.IndexOf('{', rund);
        if (auf < 0) return new List<string> { blk };

        int zu = KlammerEnde(blk, auf, '{', '}');
        if (zu < 0) return new List<string> { blk };

        string kopf = blk[..m.Index];
        string ja = blk[(auf + 1)..zu];
        string rest = blk[(zu + 1)..];
        string nein = "";

        Match e = Regex.Match(rest, @"^\s*else\s*\{");
        if (e.Success)
        {
            int auf2 = zu + 1 + e.Length - 1;
            int zu2 = KlammerEnde(blk, auf2, '{', '}');
            if (zu2 > 0)
            {
                nein = blk[(auf2 + 1)..zu2];
                rest = blk[(zu2 + 1)..];
            }
        }

        var res = new List<string>();
        res.AddRange(Varianten(kopf + ja + rest));
        res.AddRange(Varianten(kopf + nein + rest));
        return res;
    }

    private static int KlammerEnde(string s, int start, char auf, char zu)
    {
        int tiefe = 0;
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == auf) tiefe++;
            else if (s[i] == zu && --tiefe == 0) return i;
        }
        return -1;
    }

    /// <summary>Die Knöpfe einer Leiste, in Markup-Reihenfolge.</summary>
    private static List<Knopf> Knoepfe(string blk)
    {
        var res = new List<Knopf>();

        foreach (Match m in Regex.Matches(blk, @"<button\b"))
        {
            int tagEnde = FindeTagEnde(blk, m.Index);
            int ende = blk.IndexOf("</button>", m.Index, StringComparison.Ordinal);
            if (tagEnde < 0 || ende < 0) continue;

            string starttag = blk[m.Index..(tagEnde + 1)];
            string text = blk[(tagEnde + 1)..ende];

            res.Add(new Knopf(
                starttag.Contains("epos-knopf--primaer", StringComparison.Ordinal),
                text.Contains("Abbrech", StringComparison.OrdinalIgnoreCase),
                Einzeilig(text)));
        }

        return res;
    }

    /// <summary>Das Ende des Starttags — die erste spitze Klammer, die kein Lambda-Pfeil <c>=&gt;</c> ist.</summary>
    private static int FindeTagEnde(string s, int start)
    {
        int i = start;
        while (true)
        {
            i = s.IndexOf('>', i);
            if (i < 0) return -1;
            if (i > 0 && s[i - 1] == '=') { i++; continue; }
            return i;
        }
    }

    private static int Zeilenzahl(string s, int bisIndex)
    {
        int n = 0;
        for (int i = 0; i < bisIndex && i < s.Length; i++)
            if (s[i] == '\n') n++;
        return n;
    }

    private static string Einzeilig(string s)
        => Regex.Replace(s.Replace("\r", " ").Replace("\n", " "), @"\s+", " ").Trim();

    /// <summary>Die Knopffolge als Kurzbild für die Fehlermeldung.</summary>
    private static string Bild(Leistenblock fuss)
        => fuss.IstBaustein
            ? "<SpeichernLeiste>"
            : string.Join(" . ", Knoepfe(fuss.Text).Select(
                k => k.Text + (k.Primaer ? " [primaer]" : "")));

    private static string Bericht(IEnumerable<Fund> funde)
        => string.Join("\n", funde.Select(
            f => "  " + f.Datei + ":" + f.Zeile + "\n      Regel : " + f.Regel
                 + "\n      Fuss  : " + f.Bild));

    // =====================================================================
    //  Der Weg zu den Quelldateien (dasselbe Verfahren wie SchliesskreuzWacheTests)
    // =====================================================================

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    /// <summary>
    /// Alle <c>*Dialog.razor</c> unter <c>EPOS.UI/Dialoge/</c>; Schlüssel ist der
    /// repo-relative Pfad mit Schrägstrichen — so steht er auch in den Ausnahmelisten
    /// und in der Fehlermeldung.
    /// </summary>
    private static Dictionary<string, string> Dialogdateien()
    {
        string wurzel = Wurzel();
        string start = Path.Combine(wurzel, "EPOS.UI", "Dialoge");
        var ergebnis = new Dictionary<string, string>();

        foreach (string p in Directory.GetFiles(start, "*Dialog.razor", SearchOption.AllDirectories))
            ergebnis[Path.GetRelativePath(wurzel, p).Replace('\\', '/')]
                = File.ReadAllText(p, Encoding.UTF8);

        return ergebnis;
    }
}
