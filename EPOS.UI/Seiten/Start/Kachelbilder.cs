namespace EPOS.UI.Seiten.Start;

/// <summary>
/// Das Sinnbild JE KACHEL der Startseite (Anwenderwunsch 05.09.2026,
/// W16b‑E‑3) — die Nachfolge der 21 <c>BackgroundImage</c>-Zuweisungen des
/// Vorläufers.
///
/// <para><b>Warum eine Tabelle und keine Gabe.</b> Das Bild hängt am
/// SCHLÜSSEL, nicht an den Daten: „Wärmepumpe" trägt dasselbe Sinnbild, ganz
/// gleich ob ein Projekt offen ist und welche Hülle die Kachel liefert. Stünde
/// es in <see cref="StartKachel"/>, müssten es BEIDE Hüllen setzen — die
/// Windows-Hülle <c>StartseiteHuelle</c> und der iOS-Weg
/// <c>IProjektQuelle.Startkacheln</c> —, und die zwei Listen liefen beim ersten
/// neuen Bild auseinander. Dieselbe Bauart wie <c>Menuetabelle</c>: Das Bild ist
/// Teil der Oberflächentabelle, nicht der Daten.</para>
///
/// <para><b>Die Zuordnung ist die des Bestands</b>, abgelesen aus dem
/// eingefrorenen Designer
/// <c>Werkzeuge/Formularkarte.Tests/Pruefmuster/Hauptformular/Form_Start.Designer.cs</c>
/// (jede <c>pBox_*.BackgroundImage</c> und jede
/// <c>karte_*.KartenBild</c>). Zwei Zeilen weichen ab und sind unten
/// begründet.</para>
///
/// <para><b>Die Dateien sind DIESELBEN.</b> Sie sind mit diesem Schritt
/// unverändert (<c>git mv</c>) aus
/// <c>WindowsFormsApplication1/Resources/</c> nach
/// <c>EPOS.UI/wwwroot/bilder/start/</c> gewandert und aus
/// <c>Properties/Resources.resx</c> ausgetragen — kein Umkodieren, kein
/// Zuschneiden: JPG bleibt JPG. Der Weg zum Anwender ist derselbe wie bei den
/// elf Menübildern (<c>Menueband.Bild</c>):
/// <c>_content/EPOS.UI/bilder/start/…</c>.</para>
///
/// <para><b>Der Ausschnitt steht im Stilblatt.</b> Die fünfzehn Kachel-JPG sind
/// die GANZE Kachel des Vorläufers — eine weiße Karte von rund 554 × 260 mit
/// dem Sinnbild oben links, über der WinForms seine Beschriftungen legte. Hier
/// trägt die Kachel ihren Text selbst; gezeigt wird deshalb nur das Sinnbild,
/// und den Ausschnitt macht CSS (<c>object-fit: none</c> +
/// <c>object-position</c>, Klassen <c>epos-kachel-bild--ausschnitt</c> und
/// <c>…--ausschnitt-flach</c>). Die fünf Aktionskarten tragen ihr fertig
/// zugeschnittenes <c>*_Symbol.png</c> und brauchen keinen
/// (<c>epos-kachel-bild--symbol</c>).</para>
/// </summary>
public static class Kachelbilder
{
    /// <summary>Der Ordner der Bilder als Web-Adresse — wie bei den Menübildern.</summary>
    public const string ORDNER = "_content/EPOS.UI/bilder/start/";

    /// <summary>Ein fertig zugeschnittenes Symbol (die fünf Aktionskarten).</summary>
    public const string KLASSE_SYMBOL = "epos-kachel-bild--symbol";

    /// <summary>Ausschnitt aus einer hohen Kachel des Vorläufers (rund 554 × 260).</summary>
    public const string KLASSE_AUSSCHNITT = "epos-kachel-bild--ausschnitt";

    /// <summary>
    /// Ausschnitt aus einer FLACHEN Kachel (554 × 117) — Stromspeicher und
    /// Pufferspeicher standen im Bestand als halbhohe Kacheln nebeneinander
    /// (<c>pBox_Stromspeicher</c> 405 × 112, <c>pBox_Pufferspeicher</c>
    /// 405 × 112). Ihr Sinnbild sitzt deshalb rund 22 px höher.
    /// </summary>
    public const string KLASSE_AUSSCHNITT_FLACH = "epos-kachel-bild--ausschnitt-flach";

    /// <summary>Dateiname und Stilklasse je Kachelschlüssel.</summary>
    private static readonly IReadOnlyDictionary<string, (string Datei, string Klasse)> _tabelle =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            // ---- Reiter 1 „Projekt": die fuenf AktionsKarte ------------------
            // Herkunft: karte_*.KartenBild (Form_Start.Designer.cs:238-273).
            // Die sechste Karte "Projekt Details" ist mit E-7 entfallen; ihr
            // Symbol PProjektDetails_Symbol.png bleibt deshalb liegen.
            [Kachelschluessel.ProjektNeu] = ("PProjektNeu_Symbol.png", KLASSE_SYMBOL),
            [Kachelschluessel.ProjektOeffnen] = ("PProjektOeffnen_Symbol.png", KLASSE_SYMBOL),
            [Kachelschluessel.ProjektZuletzt] = ("PProjektZuletzt_Symbol.png", KLASSE_SYMBOL),
            [Kachelschluessel.ProjektSpeichernUnter] = ("PProjektBearbeiten_Symbol.png", KLASSE_SYMBOL),
            [Kachelschluessel.ProjektLoeschen] = ("PDelete_Symbol.png", KLASSE_SYMBOL),

            // ---- Reiter 2 „Waermebedarf" ------------------------------------
            // Prozesswaerme und Brauchwasser trugen im Bestand DASSELBE Bild
            // (Unbenannt3, Designer :405 und :422) - das ist kein Versehen der
            // Uebernahme, sondern der Bestand.
            [Kachelschluessel.Gebaeude] = ("PGebaeude.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.WaermebedarfDaten] = ("Unbenannt2.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Prozesswaerme] = ("Unbenannt3.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Brauchwasser] = ("Unbenannt3.jpg", KLASSE_AUSSCHNITT),

            // ---- Reiter 3 „Strombedarf" -------------------------------------
            [Kachelschluessel.StromStandardprofil] = ("PStdLastProfil.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.StromEigenesProfil] = ("PStromProfilEigenes.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.StromMessdaten] = ("PStromMessdaten.jpg", KLASSE_AUSSCHNITT),

            // ---- Reiter 4 „Energieerzeuger" ---------------------------------
            // ABWEICHUNG 1: pBox_Heizkessel trug sein Bild nicht als
            // Properties.Resources.*, sondern EINGEBETTET in Form_Start.resx
            // (der einzige solche Fall der Maske). Die eingebetteten Bytes sind
            // Byte fuer Byte PHeizkessel.jpg (8 066 B, 551 x 215) - nachgemessen,
            // nicht geraten.
            [Kachelschluessel.Waermepumpe] = ("PWP.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Heizkessel] = ("PHeizkessel.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Solarthermie] = ("PProjektSolarthermie.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Bhkw] = ("PBHKW.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Photovoltaik] = ("PProjektPV.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.Stromspeicher] = ("PSSpeicher.jpg", KLASSE_AUSSCHNITT_FLACH),
            [Kachelschluessel.Pufferspeicher] = ("PPufferSpeicher.jpg", KLASSE_AUSSCHNITT_FLACH),

            // ---- Reiter 5 „Simulation" --------------------------------------
            // ABWEICHUNG 2: btn_SimKonfig war ein Knopf OHNE Bild. Er ist die
            // 21. Kachel dieser Seite, und der Anwenderwunsch vom 05.09.2026
            // lautet "Icons fehlen" - er bekommt deshalb PSchnellSim.jpg (der
            // Blitz), das einzige Kachelbild des Bestands ohne eigene Kachel.
            [Kachelschluessel.SimulationKonfiguration] = ("PSchnellSim.jpg", KLASSE_AUSSCHNITT),
            [Kachelschluessel.SimulationErgebnis] = ("PDetailSim.jpg", KLASSE_AUSSCHNITT)
        };

    /// <summary>Alle Zuordnungen — der Prüfstand liest sie und legt die Dateien nach.</summary>
    public static IReadOnlyDictionary<string, (string Datei, string Klasse)> Alle => _tabelle;

    /// <summary>
    /// Die Web-Adresse des Sinnbilds; leerer Text, wenn der Schlüssel keines
    /// führt (dann zeigt <c>Kachel</c> wie bisher gar kein Bild).
    /// </summary>
    public static string Quelle(string schluessel)
        => _tabelle.TryGetValue(schluessel, out (string Datei, string Klasse) eintrag)
            ? ORDNER + eintrag.Datei
            : "";

    /// <summary>Die Stilklasse des Ausschnitts; leerer Text ohne Bild.</summary>
    public static string Klasse(string schluessel)
        => _tabelle.TryGetValue(schluessel, out (string Datei, string Klasse) eintrag)
            ? eintrag.Klasse
            : "";
}
