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
    /// Gesetzeskatalog und die beiden Schreibwege gehen als Delegat hinein - genau
    /// die Uebergabe, die der Dialog erwartet (Leitentscheidung L9). Geschrieben
    /// wird erst in seinem OK-Weg; Abbrechen laesst Anlagen und Parametersatz
    /// unveraendert.
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
                SpeichereAnlage: a => SpeichereAnlage(anlagenCtrl, a),
                SpeichereVorgaben: p => SpeichereVorgaben(wirt, p));
        }
        catch (Exception ex)
        {
            Console.WriteLine("BHKW-Wirtschaftlichkeit: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Schreibt EINE Anlagenzeile - wortgleich zu
    /// <c>BhkwWirtschaftlichkeitHuelle.SpeichereAnlage</c>. Der Dialog ruft diesen
    /// Weg NUR in seinem OK-Weg.
    /// </summary>
    private static bool SpeichereAnlage(KwkgAnlagenCtrl anlagenCtrl, KwkgAnlagenAngabe anlage)
    {
        try { return anlagenCtrl.Speichere(anlage, true); }
        catch { return false; }
    }

    /// <summary>
    /// Schreibt die Projektvorgaben - wortgleich zu
    /// <c>BhkwWirtschaftlichkeitHuelle.SpeichereVorgaben</c>.
    /// </summary>
    private static bool SpeichereVorgaben(WirtschaftlichkeitCtrl wirt,
                                          WirtschaftlichkeitParameter parameter)
    {
        try { return wirt.SpeichereParameter(parameter); }
        catch { return false; }
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
    // Ansicht „Projektassistent" (Befund W16a-O-4)
    // =====================================================================

    /// <inheritdoc />
    /// <remarks>
    /// <para><b>Es wird nichts nachgebaut</b> - dieselbe Quelle, die auch die
    /// Windows-Schale benutzt (<c>AssistentAnsichtQuelle</c> in
    /// <c>EPOS.UI.Daten</c>). Sie legt je Lauf einen <c>AssistentCtrl</c> an, haengt
    /// die Ablaufdelegaten daran und liefert den Schluesselsatz, den
    /// <c>AssistentSeite</c> erwartet - samt <c>HatAenderungen</c>, also samt der
    /// Rueckfrage „Speichern / Verwerfen / Bleiben" aus dem Anwenderentscheid
    /// <b>62b-E-1</b>, die ausdruecklich auf BEIDEN Plattformen gilt.</para>
    ///
    /// <para><b>Der EINE Unterschied ist die Naht <c>AssistentPlattformwege</c>:</b>
    /// Elf der dreizehn Assistentenseiten bekommen ihren Parametersatz bis heute aus
    /// Huellen in <c>WindowsFormsApplication1/Views</c>, die einen Fensterbesitzer an
    /// die Katalogdialoge weiterreichen (<c>GebaeudeHuelle.Gaben(IWin32Window, …)</c>
    /// und zehn Geschwister). Sie stehen deshalb hier nicht zur Verfuegung und werden
    /// BENANNT abgelehnt: Wer den Schritt betritt, liest den Grund, statt vor einer
    /// leeren Flaeche zu stehen. Die zwei plattformfreien Schritte -
    /// Komponentenauswahl und Projektkopf - laufen vollstaendig, und mit ihnen der
    /// Speicherlauf; sie sind der Weg, auf dem ein Projekt entsteht. Die uebrigen elf
    /// kommen mit dem Umzug der Fachmasken (iU11).</para>
    ///
    /// <para><b>Die Vorauswahl.</b> Der iOS-Einstieg kommt aus der ZEILE eines
    /// Projekts (Knopf „Bearbeiten…" der Projektliste); das Projekt geht als
    /// Vorauswahl des linken Bandes hinein. Unter Windows gibt es diesen Einstieg
    /// nicht - dort markiert der Anwender im Band selbst, und die Vorauswahl bleibt
    /// 0.</para>
    ///
    /// <para><b>„Zuletzt geoeffnet" merkt JEDER Lauf</b> - Anwenderentscheid
    /// <b>W16a-O-4-Q1</b> vom 13.09.2026, Weg (a): „auf iOS wird IMMER gemerkt". Der
    /// Nachzug geht ueber <c>Dienste.Projekt.Uebernehmen</c>, also die merkende
    /// Fassung, die <c>Tab_Applikation</c> fortschreibt; eine zweite, nicht merkende
    /// Variante gibt es hier NICHT. Die Windows-Unterscheidung <c>Setzen</c> gegen
    /// <c>Uebernehmen</c> trennt ZWEI Einstiege - die Startkacheln merken, die zwei
    /// Menuewege nicht -, und beide gibt es auf iOS nicht: Die Projektliste ist die
    /// EINE Startansicht, ein Menue gibt es gar nicht. Der Windows-Weg bleibt davon
    /// unberuehrt.</para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? AssistentGaben(int betriebsart, int idProjekt)
    {
        try
        {
            int vorauswahl =
                betriebsart == AssistentCtrl.BETRIEBSART_BEARBEITEN && idProjekt > 0
                    ? idProjekt : 0;

            return AssistentAnsichtQuelle.AnsichtGaben(
                betriebsart,
                AssistentPlattformwege.Ohne(
                    WindowsFormsApplication1.MyResource.Resource.WIZ_SEITE_NICHT_HIER),
                vorauswahl);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Projektassistent: " + ex.Message);
            return null;
        }
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
    // Die FUENF Masken, die die Wurzel seit KI-D-Q8 selbst zeigt
    // =====================================================================
    //
    // ES WIRD NICHTS NACHGEBAUT: Jede der fuenf ruft dieselbe plattformfreie
    // Huelle in EPOS.UI.Daten, die auch das Windows-Fenster fuellt. Was die
    // Plattform beisteuert, kommt ueber die Kern-Dienste - die Dateiwahl ueber
    // Dienste.Datei, die Ablagewurzeln ueber Dienste.Pfade.
    //
    // Eine Ausnahme faengt jede: Ohne Datenbank oder bei einem Fehler im
    // Ladeweg antwortet die Quelle null, und die Wurzel nennt den Grund im
    // Banner, statt vor einer leeren Flaeche zu stehen.

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? KlimadatenGaben()
    {
        try { return KlimadatenHuelle.Gaben(); }
        catch (Exception ex) { Console.WriteLine("Klimadaten: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Der STAMM entscheidet, nicht das geoeffnete Projekt:</b> Eine Variante
    /// haengt immer am Stamm, nie an einer anderen Variante — das rechnet
    /// <c>ProjektVarianteHuelle.Vorbereiten</c> aus, dieselbe Stelle, die auch der
    /// Menueweg unter Windows geht. Ist kein Projekt offen oder kein Stamm zu
    /// finden, meldet sie es mit einem Schluessel, und hier wird daraus
    /// <c>null</c>: Die Wurzel nennt den Grund.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? ProjektVarianteGaben(int idProjekt)
    {
        try
        {
            var vor = ProjektVarianteHuelle.Vorbereiten(idProjekt, Projektname(idProjekt));
            if (!vor.Bereit) return null;

            return ProjektVarianteHuelle.Gaben(vor.IdStamm, vor.StammName);
        }
        catch (Exception ex) { Console.WriteLine("Projektvariante: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Der Schreibweg selbst liegt im Kern (<c>VariantenCtrl.AnlegenAusStamm</c>);
    /// hier wird nur der Stamm noch einmal bestimmt. Ein Nachziehen der Anzeige
    /// gibt es auf iOS nicht — die Wurzel laedt nach dem Rueckweg ohnehin neu.
    /// </remarks>
    public string ProjektVarianteUebernehmen(
        int idProjekt, EPOS.UI.Dialoge.Projekt.ProjektVarianteWahl wahl)
    {
        try
        {
            var vor = ProjektVarianteHuelle.Vorbereiten(idProjekt, Projektname(idProjekt));
            if (!vor.Bereit) return "";

            var ergebnis = ProjektVarianteHuelle.Anlegen(vor.IdStamm, vor.StammName, wahl);
            return ergebnis.Gelungen ? (wahl.Bezeichner ?? "") : "";
        }
        catch (Exception ex) { Console.WriteLine("Projektvariante: " + ex.Message); return ""; }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? ProjektKopieGaben()
    {
        try { return ProjektKopieHuelle.Gaben(); }
        catch (Exception ex) { Console.WriteLine("Projektkopie: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Ohne Projekt lauffaehig: Dann bleiben Stammganglinien und Direktimport,
    /// und der Ausgang in die Speichervariante faellt weg (kein Delegat, kein
    /// Knopf).
    /// </remarks>
    public IReadOnlyDictionary<string, object>? PeakShavingGaben(int idProjekt)
    {
        try { return new PeakShavingHuelle(idProjekt).Gaben(); }
        catch (Exception ex) { Console.WriteLine("Lastspitzenkappung: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? StromganglinieAdminGaben()
    {
        try { return StromganglinieAdminHuelle.Gaben(); }
        catch (Exception ex) { Console.WriteLine("Stromganglinien: " + ex.Message); return null; }
    }

    // =====================================================================
    //  Berichte, Kosten und Wirtschaftlichkeit (Etappe E3, Schritt 8; A19)
    // =====================================================================

    /// <summary>
    /// Die Huelle des Reiters „Berichte &amp; Kosten" wird je Sitzung GEHALTEN —
    /// wie die <c>SimulationAnsichtQuelle</c> seit #208. Sie fuehrt den geteilten
    /// Gruppenstand (Stammprojekt, Markierung, Vergleichswahl) der vier Seiten;
    /// eine neue Instanz je Ansichtswechsel verwuerfe ihn.
    /// </summary>
    private BerichteKostenHuelle? _berichte;

    /// <inheritdoc />
    /// <remarks>
    /// <b>Benannt abgelehnt bleibt genau eines:</b> der Knopf „Variante anlegen"
    /// der Uebersichtsseite. Er fuehrt unter Windows in ein ZWEITES Fenster;
    /// auf iOS ist der Variantendialog eine eigene Ansicht der Wurzel
    /// (<c>PROJEKT_ALS_VARIANTE</c>), und ohne Delegat zeichnet die Seite den
    /// Knopf gar nicht erst. Umbenennen, Loeschen, Simulieren, Uebernehmen und
    /// alle drei Nachbarseiten arbeiten vollstaendig.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? BerichteKostenGaben(int idProjekt)
    {
        try
        {
            _berichte ??= new BerichteKostenHuelle();
            _berichte.SetzeProjekt(idProjekt, Projektname(idProjekt));
            return _berichte.Gaben();
        }
        catch (Exception ex) { Console.WriteLine("Berichte und Kosten: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? KostenverwaltungGaben()
    {
        try
        {
            string titel;
            return KostenKomponenteHuelle.FuerStamm().Gaben(null, false, 0, out titel);
        }
        catch (Exception ex) { Console.WriteLine("Kostenverwaltung: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? NutzungsdauerGaben()
    {
        try { return new NutzungsdauerHuelle().Gaben(); }
        catch (Exception ex) { Console.WriteLine("Nutzungsdauern: " + ex.Message); return null; }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? GesetzeskatalogGaben()
    {
        try { return GesetzeskatalogHuelle.Gaben(""); }
        catch (Exception ex) { Console.WriteLine("Gesetzeskatalog: " + ex.Message); return null; }
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
