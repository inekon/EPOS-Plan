using System.Data;
using System.Globalization;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IProjektQuelle"/> - die Datenseite der
/// Seiten in EPOS.UI.
///
/// <para><b>Warum sie in der HUELLE liegt und nicht in EPOS.UI.</b> Hausregel:
/// „Keine Datenbank in EPOS.UI." Eine Komponente bekommt ihre Daten fertig
/// herein; wer sie holt, ist die Huelle. Unter Windows tun das die Aufrufer
/// <c>Views/Heizkessel/Form_Heizkessel</c> (fruehere Fassung in <c>Form_Kosten</c>, seit W0 geloescht) und
/// <c>Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle</c> - hier steht
/// dasselbe, nur ohne Fenster.</para>
///
/// <para><b>Es wird nichts nachgebaut.</b> Jede Zeile hier ruft denselben
/// Kern-Controller, den die Windows-Huelle ruft:
/// <c>EnergietraegerVarianteCtrl.Energietraeger</c>,
/// <c>WirtschaftlichkeitCtrl.LadeParameter</c>,
/// <c>KwkgAnlagenCtrl.LadeGruppe</c>, <c>GesetzKatalog.WertMitHerkunft</c>,
/// <c>KohaerenzPruefung.Pruefe</c>. Zwei Fassungen desselben Ladewegs waeren
/// die Stelle, an der iPad und Windows verschiedene Zahlen zeigen.</para>
///
/// <para>Die Datei kennt keine iOS-API und laesst sich ohne Mac uebersetzen.</para>
/// </summary>
public sealed class IosProjektQuelle : IProjektQuelle
{
    // =====================================================================
    // Projektliste
    // =====================================================================

    /// <inheritdoc />
    public IReadOnlyList<ProjektZeile> Projekte()
    {
        var zeilen = new List<ProjektZeile>();

        try
        {
            IReadOnlyDictionary<int, string> ausstattung = Ausstattung();

            DataTable projekte = DataRepository.GetDataTable(
                "SELECT p.ID, p.Projektname, IFNULL(k.Name, '') AS Klimaregion " +
                "FROM Tab_Projekt p " +
                "LEFT JOIN Tab_Klimaregion_STAMM k ON k.ID_Klimaregion = p.ID_Klimaregion " +
                "ORDER BY p.ID");

            foreach (DataRow r in projekte.Rows)
            {
                int id = Zahl(r["ID"]);
                if (id <= 0) continue;

                zeilen.Add(new ProjektZeile(
                    id,
                    Text(r["Projektname"]),
                    Text(r["Klimaregion"]),
                    ausstattung.TryGetValue(id, out string? a) ? a : ""));
            }
        }
        catch
        {
            // Keine Datenbank, kein Schema: Die Seite zeigt ihren Leertext.
        }

        return zeilen;
    }

    /// <summary>
    /// Die Kurzform der belegten Gewerke je Projekt, z. B. „WP+BHKW+Puffer".
    ///
    /// <para>Die Typnummern stehen in <c>Tab_Energieanlagen.ID_Type</c> und sind
    /// dieselben, mit denen <c>Referenzlauf/Projektauswahl</c> die Ausstattung
    /// eines Projektes bestimmt: 1 = Waermepumpe, 2 = Solarthermie,
    /// 3 = Photovoltaik, 4 = Stromspeicher, 10 = Heizkessel, 11 = BHKW,
    /// 12 = Pufferspeicher.</para>
    /// </summary>
    private static IReadOnlyDictionary<int, string> Ausstattung()
    {
        var typen = new Dictionary<int, SortedSet<int>>();

        try
        {
            DataTable anlagen = DataRepository.GetDataTable(
                "SELECT ID_Projekt, ID_Type FROM Tab_Energieanlagen");

            foreach (DataRow r in anlagen.Rows)
            {
                int projekt = Zahl(r["ID_Projekt"]);
                int typ = Zahl(r["ID_Type"]);
                if (projekt <= 0 || typ <= 0) continue;

                if (!typen.TryGetValue(projekt, out SortedSet<int>? satz))
                {
                    satz = new SortedSet<int>();
                    typen[projekt] = satz;
                }
                satz.Add(typ);
            }
        }
        catch
        {
        }

        var kurz = new Dictionary<int, string>();
        foreach (KeyValuePair<int, SortedSet<int>> eintrag in typen)
        {
            var namen = new List<string>();
            foreach (int typ in eintrag.Value)
            {
                string name = Gewerkskurzform(typ);
                if (name.Length > 0) namen.Add(name);
            }
            kurz[eintrag.Key] = string.Join("+", namen);
        }
        return kurz;
    }

    private static string Gewerkskurzform(int idType) => idType switch
    {
        1 => "WP",
        2 => "Solar",
        3 => "PV",
        4 => "Speicher",
        10 => "Kessel",
        11 => "BHKW",
        12 => "Puffer",
        _ => ""
    };

    // =====================================================================
    // Dialog „Energietraeger anlegen"
    // =====================================================================

    /// <inheritdoc />
    public IReadOnlyList<(int Id, string Name)> Energietraeger()
    {
        try { return EnergietraegerVarianteCtrl.Energietraeger(); }
        catch { return Array.Empty<(int, string)>(); }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para><b>iU10 legt noch nichts an - und das ist Absicht.</b> Der
    /// Schreibweg des Dialogs (Katalogsuche, INSERT in <c>energy_carrier</c>,
    /// Preishistorie, Projektzuordnung) steht bis heute in der WinForms-Maske
    /// <c>Views/Heizkessel/Form_Heizkessel.CreateNewEnergyCarrier (Form_Kosten ist seit W0 geloescht)</c> und haengt dort
    /// am Typ <c>EnergyCarrier</c> und an <c>EnergietraegerKatalogCtrl</c> -
    /// beides ist mit Absicht in der Anwendung geblieben (siehe
    /// <c>EPOS.Kern/CLAUDE.md</c>, „Was mit Absicht NICHT hier liegt").</para>
    ///
    /// <para>Ihn hier NACHZUBAUEN waere genau die Doppelpflege, die Modell C
    /// abschafft: Zwei Fassungen desselben Anlegewegs wuerden auf iPad und
    /// Windows verschiedene Katalogzeilen erzeugen. Der Dialog laeuft deshalb
    /// vollstaendig - er laedt die echten Energietraeger, prueft die Eingabe
    /// und meldet sein Ergebnis -, das Anlegen selbst wartet auf den Umzug des
    /// Schreibwegs in den Kern (iU9-Welle „Kosten" bzw. iU11).</para>
    ///
    /// <para>Die Rueckgabe <c>""</c> ist dabei kein Fehlerzustand, sondern
    /// derselbe, den die Windows-Maske liefert, wenn der Anwender abbricht.</para>
    /// </remarks>
    public string EnergietraegerUebernehmen(int idProjekt, EnergietraegerVarianteErgebnis ergebnis)
    {
        try
        {
            // Die sechs abgeleiteten Werte holt derselbe Kern-Controller wie
            // unter Windows - damit ist wenigstens der LESEteil bereits geprueft.
            EnergietraegerDaten daten = EnergietraegerVarianteCtrl.Ergaenzen(ergebnis.BrennstoffId);

            Console.WriteLine("Energietraeger-Variante \"" + ergebnis.VariantenName +
                              "\" zu Brennstoff " + ergebnis.BrennstoffId.ToString(CultureInfo.InvariantCulture) +
                              " (" + daten.Code + ", " + daten.BillingUnit + "): Der Schreibweg liegt noch in " +
                              "Views/Heizkessel/Form_Heizkessel.CreateNewEnergyCarrier (Form_Kosten ist seit W0 geloescht) und wandert mit dem " +
                              "Umzug der Kostenmasken in den Kern.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Energietraeger-Variante: " + ex.Message);
        }

        return "";
    }

    // =====================================================================
    // Dialog „BHKW-Wirtschaftlichkeit"
    // =====================================================================

    /// <inheritdoc />
    /// <remarks>
    /// Dieselbe Ladefolge wie
    /// <c>Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.Oeffnen</c>:
    /// Parametersatz, Erzeugerkennzeichen, Stammname, Anlagen der
    /// Vergleichsgruppe, laufunabhaengige Doppelpflegepruefung. Der
    /// Gesetzeskatalog und der Schreibweg gehen als Delegat hinein - genau die
    /// Uebergabe, die der Dialog erwartet (Leitentscheidung L9).
    /// </remarks>
    public BhkwDialogDaten? BhkwDaten(int idProjekt)
    {
        if (idProjekt <= 0) return null;

        try
        {
            var wirt = new WirtschaftlichkeitCtrl();
            var anlagenCtrl = new KwkgAnlagenCtrl();
            var katalog = new GesetzKatalog();

            WirtschaftlichkeitParameter parameter = wirt.LadeParameter(idProjekt);
            WirtschaftlichkeitCtrl.ErzeugerFlags erzeuger = wirt.ErzeugerDerGruppe(idProjekt);

            var projekt = new ProjektCtrl();
            try { projekt.ReadSingle(idProjekt); } catch { }
            string stammName = projekt.rows > 0 ? (projekt.m_szProjektname ?? "") : "";

            List<KwkgAnlagenAngabe> anlagen = anlagenCtrl.LadeGruppe(idProjekt, stammName);
            if (anlagen == null || anlagen.Count == 0) return null;

            var doppelpflege = new List<KohaerenzHinweis>();
            try { doppelpflege.AddRange(KohaerenzPruefung.Pruefe(idProjekt, null)); }
            catch { }

            return new BhkwDialogDaten(
                IdStamm: idProjekt,
                StammName: stammName,
                Anlagen: anlagen,
                Parameter: parameter,
                HatHeizkessel: erzeuger != null && erzeuger.Heizkessel,
                Doppelpflege: doppelpflege,
                Katalog: katalog.WertMitHerkunft,
                ErgebnisseLaden: ids => wirt.LadeErgebnisse(new List<int>(ids)),
                Speichern: () => Speichern(anlagenCtrl, wirt, anlagen, parameter));
        }
        catch (Exception ex)
        {
            Console.WriteLine("BHKW-Wirtschaftlichkeit: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Schreibt den Bildschirmzustand fort und liefert die Zahl der
    /// GESCHEITERTEN Saetze - wortgleich zu
    /// <c>BhkwWirtschaftlichkeitHuelle.Speichern</c>.
    /// </summary>
    private static int Speichern(KwkgAnlagenCtrl anlagenCtrl, WirtschaftlichkeitCtrl wirt,
                                 List<KwkgAnlagenAngabe> anlagen,
                                 WirtschaftlichkeitParameter parameter)
    {
        int fehler = 0;

        foreach (KwkgAnlagenAngabe a in anlagen)
        {
            if (!anlagenCtrl.Speichere(a, true)) fehler++;
        }

        try { if (!wirt.SpeichereParameter(parameter)) fehler++; }
        catch { fehler++; }

        return fehler;
    }

    // =====================================================================
    // Ansicht „Simulation" (Auftrag #208, Stufe S2)
    // =====================================================================

    /// <summary>
    /// Die Simulationsansicht EINES Projekts - sie ueberlebt den Ansichtswechsel.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum ein gehaltenes Feld.</b> Der gerechnete Lauf, die zwoelf Bilder
    /// und die Gueltigkeitsmarke leben in der Ergebnishuelle, nicht in der Razor-Seite.
    /// Wer die Ansicht wechselt - auf die Stromspeicher-Auslegung und zurueck -,
    /// verliert die Seite; nur eine GEHALTENE Quelle bringt den Lauf wieder mit
    /// (Konzept „Simulationsablauf" 1.3). Unter Windows haelt sie
    /// <c>Views/Simulation/SimulationHuelle</c>, hier diese Projektquelle.</para>
    ///
    /// <para>Die Quelle selbst legt je Projekt neue Huellen an
    /// (<c>SimulationAnsichtQuelle.Nachziehen</c>); ein Projektwechsel braucht hier
    /// also nichts.</para>
    /// </remarks>
    private SimulationAnsichtQuelle? _simulation;

    /// <inheritdoc />
    /// <remarks>
    /// <para><b>Es wird nichts nachgebaut</b> - dieselbe Quelle, die auch die
    /// Windows-Schale benutzt. Der EINE Unterschied ist die Naht
    /// <c>SimulationPlattformwege</c>: Der Waermepumpen-Assistent oeffnet aus sich
    /// heraus WinForms-Fenster (<c>WaermepumpenHuelle.Gaben(IWin32Window, …)</c>) und
    /// steht deshalb hier nicht zur Verfuegung. Er wird BENANNT abgelehnt und faellt
    /// nicht still aus: Wer im Reiter „Waermepumpe" eine Modulzeile doppelt antippt,
    /// bekommt den Grund als Meldung ueber <c>Dienste.Dialog</c>.</para>
    ///
    /// <para>Der zweite Weg, der hier fehlt, ist der Knopf „Katalog ansehen" in der
    /// Pufferverwaltung (<c>Katalogwege.PufferKatalogGaben</c>) - ohne eingehaengten
    /// Haken zeigt der Dialog ihn gar nicht erst an. Alles Uebrige - Konfiguration,
    /// Lauf, elf Reiter, Bilder, CSV-Ausgaben ueber <c>Dienste.Datei</c>, die
    /// Stromspeicher-Auslegung - ist derselbe Code wie unter Windows.</para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? SimulationGaben(int idProjekt)
    {
        if (idProjekt <= 0) return null;

        try
        {
            _simulation ??= new SimulationAnsichtQuelle(
                new BedarfsZustand(),
                SimulationPlattformwege.Ohne(
                    WindowsFormsApplication1.MyResource.Resource.SIM_MSG_WEG_NICHT_HIER));

            return _simulation.AnsichtGaben(idProjekt, Projektname(idProjekt));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Simulation: " + ex.Message);
            return null;
        }
    }

    /// <summary>Der Projektname fuer die Kopfzeile der Ansicht; leer, wenn unbekannt.</summary>
    private static string Projektname(int idProjekt)
    {
        try
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            return projekt.rows > 0 ? (projekt.m_szProjektname ?? "") : "";
        }
        catch { return ""; }
    }

    // =====================================================================

    /// <summary>
    /// Das Lagebild der Lizenz fuer das Banner der <c>AppWurzel</c> (Welle iF30).
    /// </summary>
    /// <remarks>
    /// Unter Windows reicht die Huelle es als Parameter herein; auf iOS gibt es keine
    /// Seitenhuelle, und deshalb geht es ueber die Projektquelle. Gerechnet wird es im
    /// Kern (<c>LizenzLage.Ermitteln</c>), der dabei ueber <c>Dienste.Lizenzablage</c>
    /// den SCHLUESSELBUND liest - genau der synchrone Zugriff, den eine Razor-Komponente
    /// nicht selbst tun darf (Regel S-2 aus W15c).
    /// </remarks>
    public WindowsFormsApplication1.LizenzLage? Lizenzlage()
    {
        return WindowsFormsApplication1.LizenzLage.Ermitteln();
    }

    // =====================================================================

    private static int Zahl(object wert)
    {
        if (wert == null || wert == DBNull.Value) return 0;
        try { return Convert.ToInt32(wert, CultureInfo.InvariantCulture); }
        catch { return 0; }
    }

    private static string Text(object wert)
        => wert == null || wert == DBNull.Value ? "" : (Convert.ToString(wert) ?? "");
}
