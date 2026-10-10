using System.Globalization;
using System.Text;
using EPOS.UI.Dienste;
using KiKern;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Das FLACHE Abbild der Ansicht „Simulation" für den Hilfe-Assistenten (Auftrag #221,
/// sechste Deklaration des Dialogkatalogs).
///
/// <para><b>Warum die Ansicht überhaupt Felder anmeldet.</b> Bis hierher unterschied
/// sich der Assistent der Simulationsansicht von dem des Hauptfensters in genau einer
/// Zeichenkette — dem Bereich aus dem Hilfeschlüssel. Der Anwender hat das am
/// 11.09.2026 benannt: „Die KI-Buttons haben keine unterschiedliche Funktion im
/// Kontext." Ein Kontext, der nur aus einem Namen besteht, ist keiner; er wird einer,
/// sobald der Assistent die Kaskade, die fünf Laufparameter und die Kennzahlen des
/// Laufs LESEN kann.</para>
///
/// <para><b>Warum ein eigenes Sichtmodell und nicht die DTO selbst</b> (Muster
/// <c>StromspeicherKiSicht</c>, #200): Ein Maskenfeld des Katalogs ist genau ZWEI
/// Stufen tief (<c>Typ.Eigenschaft</c>, <c>KiEigenschaftspfad</c>); was die Ansicht
/// zeigt, liegt drei bis vier Stufen tief
/// (<c>Ergebnis.Kennzahlen.WaermebedarfGesamtMwh</c>) und verteilt über VIER Stände —
/// die Konfiguration, die fünf Laufparameter, das Ergebnis und die Ansicht selbst
/// (Schritt und Reiter). Diese Klasse löst die Ketten an EINER benannten Stelle auf.</para>
///
/// <para><b>Sie hält keinen Zustand.</b> Jede Eigenschaft rechnet bei jedem Zugriff aus
/// den Delegaten; die Ansicht tauscht ihre Stände bei jedem Auffrischen aus, und ein
/// festgehaltenes Objekt zeigte dem Assistenten den Stand von vorhin.</para>
///
/// <para><b>Elf Eigenschaften sind ABGELEITET und ohne Setzer</b> — die Kaskade, der
/// Schritt, der Reiter, die sieben Kennzahlen des Laufs und die Laufhinweise. Das ist
/// die Aussage, nicht eine Nachlässigkeit: Sie lassen sich nicht eingeben, und
/// <c>feld_setzen</c> lehnt sie deshalb ab (<c>KiFeldzugang.Setzbar</c> folgt der
/// Schreibbarkeit der Eigenschaft). Setzbar sind die Laufparameter — und sie gehen
/// denselben Weg wie das Feld auf dem Bildschirm (<see cref="SimulationParameterDienste"/>,
/// jedes Feld schreibt sofort).</para>
///
/// <para><b>Welle #458: der Kühlschalter und die Werte JE ANLAGE.</b> Die
/// Projekteinstellung „Kühlung rechnen" geht über denselben Delegaten wie ihr Schalter;
/// Wärmequelle, konstante Quelltemperatur, WP-Priorität und Betriebsmodus der gewählten
/// Karte gehen über Schritt ① selbst (<c>SimulationKonfigSeite.Ki*Setzen</c>) — dieselben
/// Vorprüfungen und Schreibwege wie die Überlagerungen, aus denen der Anwender sie
/// setzt. Einen Grund der Seite wirft die Sicht als benannte Absage weiter.</para>
/// </summary>
public sealed class SimulationKiSicht
{
    private readonly Func<SimulationKonfigDaten?> _konfiguration;
    private readonly Func<ParameterDaten?> _laufparameter;
    private readonly Func<SimulationParameterDienste?> _schreibwege;
    private readonly Func<SimulationErgebnisDaten?> _ergebnis;
    private readonly Func<string> _schritt;
    private readonly Func<string> _reiter;
    private readonly Func<SimulationErgebnisDienste?>? _speicherwege;
    private readonly Func<SimulationKonfigDienste?>? _konfigwege;
    private readonly Action<double>? _autarkiespeicher;
    private readonly Func<SimulationKonfigSeite?>? _konfigseite;

    /// <summary>Legt die Sicht über die lebenden Stände der Ansicht.</summary>
    /// <param name="konfiguration">Der Stand von Schritt ①; <c>null</c> = ① steht nicht.</param>
    /// <param name="laufparameter">Der Stand der vier Laufparameter; <c>null</c> = keiner.</param>
    /// <param name="schreibwege">Die vier Schreibwege; <c>null</c> = nichts ist setzbar.</param>
    /// <param name="ergebnis">Der Stand von Schritt ③; <c>null</c> = es wurde nicht gerechnet.</param>
    /// <param name="schritt">Der Name des stehenden Schritts, in der Oberflächensprache.</param>
    /// <param name="reiter">Der Name des offenen Reiterblatts; leer in ①.</param>
    /// <param name="speicherwege">
    /// Der Schreibdienst des Reiters „Stromspeicher" (Welle KI‑F2). <c>null</c> = das Blatt
    /// steht nicht oder die Plattform stellt ihn nicht; dann bleiben die einundzwanzig
    /// Speicherfelder lesbar, und eine Setzung läuft ins Leere statt in die Datenbank.
    /// </param>
    /// <param name="konfigwege">
    /// Der Schreibdienst von Schritt ① (Welle KI‑F2) — er trägt den Lesepunkt des
    /// Wärmepumpen-Kennfelds. <c>null</c> = ① steht nicht.
    /// </param>
    /// <param name="autarkiespeicher">
    /// Der Schreibweg der Speicherkapazität auf dem Blatt „Ergebnis" (Welle KI‑F6) —
    /// das EINZIGE echte Eingabefeld der neun Reiterblätter. <c>null</c> = das Blatt
    /// steht nicht; dann bleibt der Wert lesbar.
    /// </param>
    /// <param name="konfigseite">
    /// Schritt ① selbst (Welle #458) — er trägt die Werte JE ANLAGE (Wärmequelle,
    /// konstante Quelltemperatur, WP-Priorität, Betriebsmodus) samt den Vorprüfungen
    /// seiner Überlagerungen. <c>null</c> = ① steht nicht; dann lesen die Felder leer,
    /// und eine Setzung wird benannt abgelehnt.
    /// </param>
    public SimulationKiSicht(Func<SimulationKonfigDaten?> konfiguration,
                             Func<ParameterDaten?> laufparameter,
                             Func<SimulationParameterDienste?> schreibwege,
                             Func<SimulationErgebnisDaten?> ergebnis,
                             Func<string> schritt,
                             Func<string> reiter,
                             Func<SimulationErgebnisDienste?>? speicherwege = null,
                             Func<SimulationKonfigDienste?>? konfigwege = null,
                             Action<double>? autarkiespeicher = null,
                             Func<SimulationKonfigSeite?>? konfigseite = null)
    {
        _autarkiespeicher = autarkiespeicher;
        _konfigseite = konfigseite;
        _konfiguration = konfiguration ?? throw new ArgumentNullException(nameof(konfiguration));
        _laufparameter = laufparameter ?? throw new ArgumentNullException(nameof(laufparameter));
        _schreibwege = schreibwege ?? throw new ArgumentNullException(nameof(schreibwege));
        _ergebnis = ergebnis ?? throw new ArgumentNullException(nameof(ergebnis));
        _schritt = schritt ?? throw new ArgumentNullException(nameof(schritt));
        _reiter = reiter ?? throw new ArgumentNullException(nameof(reiter));
        _speicherwege = speicherwege;
        _konfigwege = konfigwege;
    }

    // =====================================================================
    //  Wo der Anwender steht (nur lesend)
    // =====================================================================

    /// <summary>Der stehende Schritt der Ablaufleiste — „Konfiguration" oder „Ergebnis".</summary>
    public string Ansichtsschritt => _schritt() ?? "";

    /// <summary>
    /// Das offene Reiterblatt von Schritt ③ („Übersicht", „Stromspeicher", …); leer,
    /// solange ① vorn steht. <b>Seit Welle #458 (Stufe 2) ein Wahlfeld:</b> Es zu setzen
    /// schlägt das Blatt auf wie ein Klick auf den Reiter — nur, solange ③ vorn steht;
    /// sonst lehnt die Ansicht benannt ab.
    /// </summary>
    public string Reiter
    {
        get => _reiter() ?? "";
        set
        {
            if (ReiterWaehlen is null)
                throw new InvalidOperationException(Resource.KI_DLG_SIM_REITER_NICHT_VORN);
            ReiterWaehlen(value ?? "");
        }
    }

    /// <summary>Die Blätter, zwischen denen der Anwender in ③ wechselt — Schlüssel ist der Titel (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> ReiterWahl
        => KiMaskenanmeldung.Eintraege(ReiterEintraege?.Invoke() ?? Array.Empty<string>(), t => t);

    /// <summary>Die Titel der Blätter, die der Reiter in ③ gerade führt; leer in ①.</summary>
    public Func<IReadOnlyList<string>>? ReiterEintraege { get; init; }

    /// <summary>Schlägt ein Blatt über seinen Titel auf; wirft mit Grund, wenn ③ nicht vorn steht.</summary>
    public Action<string>? ReiterWaehlen { get; init; }

    // =====================================================================
    //  Die Anzeigeschalter des offenen Ergebnisblattes (Welle #458, Stufe 2)
    // =====================================================================

    /// <summary>Liefert die Schalter der gezeichneten Blätter; leer in ①.</summary>
    public Func<IReadOnlyList<Anzeigeschalter>>? ErgebnisschalterLesen { get; init; }

    /// <summary>
    /// Die Schalter der Anzeige auf dem offenen Ergebnisblatt — eine SPALTE, je Schalter
    /// eine Zeile mit seiner Beschriftung als Kennzeichen. Sie stellen nur das Bild ein.
    /// </summary>
    public IReadOnlyList<Anzeigeschalter> Ergebnisschalter
        => ErgebnisschalterLesen?.Invoke() ?? Array.Empty<Anzeigeschalter>();

    // =====================================================================
    //  Schritt ① — Kaskade und Reihenfolge (nur lesend)
    // =====================================================================

    /// <summary>
    /// Die AUFGENOMMENEN Erzeuger in Kaskadenreihenfolge, je Gruppe eine Aufzählung
    /// („Wärmeerzeuger: 1. BHKW 1, 2. Heizkessel"); leer, solange nichts aufgenommen ist.
    /// </summary>
    /// <remarks>
    /// <para><b>Ein Maskenfeld trägt EINEN Wert</b> (<c>KiDialogFeld</c>), eine Kaskade
    /// aber beliebig viele Plätze, deren Zahl erst zur Laufzeit feststeht. Die
    /// Aufstellung geht deshalb als TEXT hinaus — genau so, wie die Ansicht sie zeigt;
    /// dieselbe Regel wie bei <c>StromspeicherKiSicht.EinheitenListe</c>.</para>
    /// <para><b>Nicht aufgenommene Karten stehen NICHT darin.</b> Sie sind der Grund
    /// vieler Fragen („warum liefert mein Kessel nichts?"), aber sie sind keine
    /// Kaskade; sie stehen in <see cref="NichtAufgenommen"/>.</para>
    /// </remarks>
    public string Kaskade
    {
        get
        {
            SimulationKonfigDaten? d = _konfiguration();
            if (d is null) return "";

            var sb = new StringBuilder();

            foreach (KachelGruppe gruppe in d.Gruppen)
            {
                var teile = new List<string>();

                foreach (ErzeugerZeile zeile in gruppe.Zeilen)
                {
                    if (zeile.Verfuegbar) continue;

                    string rang = (zeile.Kachel.Rang ?? "").Trim();
                    string name = string.IsNullOrWhiteSpace(zeile.Kachel.Titel)
                        ? zeile.DbWert
                        : zeile.Kachel.Titel;

                    teile.Add(rang.Length == 0 ? name : rang + " " + name);
                }

                if (teile.Count == 0) continue;

                if (sb.Length > 0) sb.Append("; ");
                sb.Append(gruppe.Titel).Append(": ").Append(string.Join(", ", teile));
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Die Erzeuger, die das Projekt FÜHRT, die aber auf keinem Platz der Simulation
    /// stehen; leer = keine.
    /// </summary>
    /// <remarks>
    /// Sie beantworten die häufigste Rückfrage zu einem Lauf: Eine angelegte Anlage
    /// ohne Kaskadenplatz rechnet nicht, und die Übersicht zeigt dafür 0,00 (#190).
    /// </remarks>
    public string NichtAufgenommen
    {
        get
        {
            SimulationKonfigDaten? d = _konfiguration();
            if (d is null) return "";

            var teile = new List<string>();

            foreach (KachelGruppe gruppe in d.Gruppen)
                foreach (ErzeugerZeile zeile in gruppe.Zeilen)
                {
                    if (!zeile.Verfuegbar || !zeile.HatAnlage) continue;

                    teile.Add(string.IsNullOrWhiteSpace(zeile.Bezeichner)
                                  ? zeile.DbWert
                                  : zeile.Bezeichner);
                }

            return string.Join(", ", teile);
        }
    }

    // =====================================================================
    //  Die FÜNF Laufparameter (lesbar und setzbar)
    // =====================================================================

    /// <summary>Die Netzwärmeverluste [%] — sie wirken nur bei vorhandenem Wärmebedarf.</summary>
    public double Netzverluste
    {
        get => Parameter?.Netzverluste ?? 0.0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Netzverluste = value;
            Wege?.NetzverlusteSchreiben?.Invoke(value, p.NetzverlusteEinheit);
        }
    }

    /// <summary>Die Betriebsart der BHKW: 0 = wärmegeführt, 1 = stromgeführt, 2 = ohne Einspeisung.</summary>
    public int BhkwBetriebsart
    {
        get => Parameter?.Betriebsart ?? 0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Betriebsart = value;
            Wege?.BetriebsartSchreiben?.Invoke(value);
        }
    }

    /// <summary>Die projektweite untere Modulationsgrenze der BHKW-Module [%].</summary>
    public int BhkwLeistungsgrenze
    {
        get => Parameter?.UntersteLeistungsgrenze ?? 0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.UntersteLeistungsgrenze = value;
            Wege?.LeistungsgrenzeSchreiben?.Invoke(value);
        }
    }

    // 16.09.2026 (Auftrag #299): Hier stand "WpHeizstab" - das KI-Feld wp_heizstab der
    // Simulationsmaske. Der Heizstab ist kein Laufparameter des Projekts mehr, sondern
    // ein Feld JE WÄRMEPUMPE (Tab_Energieanlagen.Heizstab); die Maske führt ihn deshalb
    // nicht mehr, und KiDialoge.Katalog nennt ihn nicht mehr.

    /// <summary>Die Betriebsbereitschaft des Heizkessels [h/a].</summary>
    public double KesselBereitschaft
    {
        get => Parameter?.Bereitschaft ?? 0.0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Bereitschaft = value;
            Wege?.BereitschaftSchreiben?.Invoke(value);
        }
    }

    /// <summary>
    /// Die Heizgrenze der Kesselbereitschaft [°C]; <c>null</c> = leer (Vorgabe 15 °C). Ein Wert
    /// außerhalb der Grenzen steht danach im Arbeitsstand und meldet sich in der Prüfung der
    /// Maske — geschrieben wird er nicht.
    /// </summary>
    public double? KesselHeizgrenze
    {
        get => Parameter?.Heizgrenze;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Heizgrenze = value;
            if (WindowsFormsApplication1.SimulationSPK.HeizgrenzePlausibel(value))
                Wege?.HeizgrenzeSchreiben?.Invoke(value);
        }
    }

    /// <summary>
    /// Der Lesepunkt des Wärmepumpen-Kennfelds: <c>true</c> = vor dem Speicher ablesen
    /// (Welle KI‑F2).
    /// </summary>
    /// <remarks>
    /// Der Schalter steht in der Fußzeile von Schritt ① und nur dort, wo er überhaupt
    /// etwas bedeutet (<c>BoosterSichtbar</c>). Sein Schreibweg ist derselbe wie am
    /// Schalter: <c>SimulationKonfigDienste.LesepunktSchreiben</c>; er meldet, ob die
    /// Einstellung angekommen ist, und nur dann zieht der Stand nach.
    /// </remarks>
    public bool LesepunktDavor
    {
        get => _konfiguration()?.BoosterDavor ?? false;
        set
        {
            SimulationKonfigDaten? d = _konfiguration();
            if (d is null) return;
            if (_konfigwege?.Invoke()?.LesepunktSchreiben?.Invoke(value) == true)
                d.BoosterDavor = value;
        }
    }

    // =====================================================================
    //  Schritt ① — der Kühlschalter und die Werte JE ANLAGE (Welle #458)
    // =====================================================================

    /// <summary>
    /// Die Projekteinstellung „Kühlung rechnen" — geschrieben über denselben Delegaten
    /// wie der Schalter (<c>KuehlbetriebSchreiben</c>), sofort.
    /// </summary>
    /// <remarks>
    /// <b>Benannt statt still:</b> Bietet die Plattform keinen Schreibweg an, steht der
    /// Schalter gar nicht auf der Seite — die Setzung sagt das; scheitert das Schreiben,
    /// sagt sie denselben Satz wie die Seite (<c>SIMKONF_MSG_KUEHLBETRIEB_FEHLER</c>).
    /// </remarks>
    public bool Kuehlbetrieb
    {
        get => Parameter?.Kuehlbetrieb ?? false;
        set
        {
            ParameterDaten? p = Parameter;
            Func<bool, bool>? schreiben = Wege?.KuehlbetriebSchreiben;
            if (p is null || schreiben is null)
                throw new InvalidOperationException(Resource.KI_SIM_KEIN_SCHREIBWEG);
            if (!schreiben(value))
                throw new InvalidOperationException(Resource.SIMKONF_MSG_KUEHLBETRIEB_FEHLER);
            p.Kuehlbetrieb = value;
        }
    }

    // ---- Der Bereich „Kälte“ (Welle KB-B; Entwurf Kältebereich 3, 5.4) ----

    /// <summary>
    /// Die Kälteerzeuger in Rechenfolge (nur lesend) — „Nummer. Name (Kennwerte)“, wie die Kacheln des Bereichs.
    /// </summary>
    public string Kaelteerzeuger
        => string.Join("; ", (_konfiguration()?.Kaeltebereich.Erzeuger ?? Array.Empty<KaelteerzeugerZeile>())
               .Select(z => z.Nummer.ToString(System.Globalization.CultureInfo.CurrentCulture) + ". " + z.Bezeichner +
                            " (" + string.Join(", ", z.Kachel.Chips.Select(c => c.Text)
                                .Concat(z.Art == KaelteStufe.Waermepumpe && !z.Kuehlbetrieb
                                    ? new[] { Resource.KI_DLG_SIM_KUEHL_AUS } : Array.Empty<string>())) + ")"));

    /// <summary>Die Kältespeicher des Bereichs „Kälte“ (nur lesend): Name, Volumen, Temperaturpaar.</summary>
    public string Kaeltespeicher
        => string.Join("; ", (_konfiguration()?.Kaeltebereich.Kaeltespeicher ?? Array.Empty<Bausteine.SpeicherKachelDaten>())
               .Select(s => string.Join(" · ", new[] { s.Bezeichner, s.Volumen, s.Temperaturpaar }.Where(t => !string.IsNullOrEmpty(t)))));

    /// <summary>
    /// Die Wärmepumpen im Kühlbetrieb, durch Komma getrennt — setzbar: Jede genannte Wärmepumpe des Bereichs schaltet
    /// den Kühlbetrieb an, jede ungenannte aus, über denselben Weg wie die Kachel (<c>KuehlbetriebWpSchreiben</c>),
    /// sofort. Ein unbekannter Name oder ein Sperrgrund lehnt benannt ab.
    /// </summary>
    public string KuehlbetriebWaermepumpen
    {
        get => string.Join(", ", (_konfiguration()?.Kaeltebereich.Erzeuger ?? Array.Empty<KaelteerzeugerZeile>())
                   .Where(z => z.Art == KaelteStufe.Waermepumpe && z.Kuehlbetrieb).Select(z => z.Bezeichner));
        set
        {
            SimulationKonfigSeite seite = SeiteOderAbsage();
            List<KaelteerzeugerZeile> wps = (_konfiguration()?.Kaeltebereich.Erzeuger ?? Array.Empty<KaelteerzeugerZeile>())
                .Where(z => z.Art == KaelteStufe.Waermepumpe).ToList();
            List<string> namen = (value ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            foreach (string n in namen)
                if (!wps.Any(w => string.Equals(w.Bezeichner, n, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException(string.Format(Resource.KI_DLG_SIM_KUEHL_WP_UNBEKANNT, n));
            foreach (KaelteerzeugerZeile w in wps)
            {
                bool an = namen.Any(n => string.Equals(w.Bezeichner, n, StringComparison.OrdinalIgnoreCase));
                Absage(seite.KiKuehlbetriebWpSetzen(w.IdAnlage, an));
            }
        }
    }

    /// <summary>
    /// Die Projekteinstellung „Anlagenkopplung" (Konzept Anlagenkopplung 9.4) als Steuerwert
    /// (<c>DbWerte.ANLAGENKOPPLUNG_*</c>) — geschrieben über denselben Delegaten wie die Wahl
    /// (<c>AnlagenkopplungSchreiben</c>), sofort. Eine Stufe, deren Rechenweg nicht gebaut ist,
    /// lehnt die Setzung benannt ab (<c>SIMKONF_ANLAGENKOPPLUNG_NICHT_VERFUEGBAR</c>), wie die
    /// gesperrten Einträge der Liste.
    /// </summary>
    public string Anlagenkopplung
    {
        get => Parameter?.Anlagenkopplung ?? WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AUS;
        set
        {
            ParameterDaten? p = Parameter;
            Func<string?, bool>? schreiben = Wege?.AnlagenkopplungSchreiben;
            if (p is null || schreiben is null)
                throw new InvalidOperationException(Resource.KI_SIM_KEIN_SCHREIBWEG);
            string stufe = string.IsNullOrWhiteSpace(value) ? WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AUS : value.Trim();
            if (!WindowsFormsApplication1.Waermeuebergabevorgaben.Stufen.Contains(stufe))
                throw new InvalidOperationException(string.Format(Resource.KI_DLG_SIM_ANLAGENKOPPLUNG_UNBEKANNT, stufe));
            if (!WindowsFormsApplication1.Waermeuebergabevorgaben.StufeGebaut(stufe))
                throw new InvalidOperationException(SimulationKonfigSeite.Kopplungsname(stufe) + ": " +
                                                    Resource.SIMKONF_ANLAGENKOPPLUNG_NICHT_VERFUEGBAR);
            string? wert = stufe == WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AUS ? null : stufe;
            if (!schreiben(wert))
                throw new InvalidOperationException(Resource.SIMKONF_MSG_ANLAGENKOPPLUNG_FEHLER);
            p.Anlagenkopplung = wert;
        }
    }

    /// <summary>Die wählbaren Stufen (Schlüssel = Steuerwert): nur die gebauten.</summary>
    public IReadOnlyList<KiWahleintrag> AnlagenkopplungWahl
        => WindowsFormsApplication1.Waermeuebergabevorgaben.Stufen
               .Where(WindowsFormsApplication1.Waermeuebergabevorgaben.StufeGebaut)
               .Select(s => new KiWahleintrag(s, SimulationKonfigSeite.Kopplungsname(s)))
               .ToList();

    // =====================================================================
    //  Netzverluste je Kanal und Zirkulation im Bestandsweg (Entscheidungsvorlage BW4)
    // =====================================================================
    //
    // Acht Felder über EINEN Delegaten (NetzkanaeleSchreiben): Jede Setzung schreibt die ganze
    // Vorgabe sofort, wie ein Feld des Abschnitts. Leer = kein Kanalwert; sind alle drei leer, gilt
    // der Projektwert „netzverluste". Eine Einheit ohne Wert bleibt im Arbeitsstand, bis ein Wert
    // dazukommt.

    /// <summary>Netzverlust des Heizkanals in seiner Einheit; leer = kein Kanalwert.</summary>
    public double? NetzverlustHeizung
    {
        get => Netzkanaele.HeizungWert;
        set => NetzkanalSetzen(v => v with { HeizungWert = value });
    }

    /// <summary>Einheit des Heizkanalwerts: <c>%</c> oder <c>kWh/a</c>.</summary>
    public string NetzverlustHeizungEinheit
    {
        get => Netzkanaele.HeizungEinheit ?? WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_PROZENT;
        set => NetzkanalSetzen(v => v with { HeizungEinheit = KanalEinheit(value) });
    }

    /// <summary>Netzverlust des Brauchwasserkanals in seiner Einheit; leer = kein Kanalwert.</summary>
    public double? NetzverlustBrauchwasser
    {
        get => Netzkanaele.BrauchwasserWert;
        set => NetzkanalSetzen(v => v with { BrauchwasserWert = value });
    }

    /// <summary>Einheit des Brauchwasserkanalwerts.</summary>
    public string NetzverlustBrauchwasserEinheit
    {
        get => Netzkanaele.BrauchwasserEinheit ?? WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_PROZENT;
        set => NetzkanalSetzen(v => v with { BrauchwasserEinheit = KanalEinheit(value) });
    }

    /// <summary>Netzverlust des Prozesskanals in seiner Einheit; leer = kein Kanalwert.</summary>
    public double? NetzverlustProzess
    {
        get => Netzkanaele.ProzessWert;
        set => NetzkanalSetzen(v => v with { ProzessWert = value });
    }

    /// <summary>Einheit des Prozesskanalwerts.</summary>
    public string NetzverlustProzessEinheit
    {
        get => Netzkanaele.ProzessEinheit ?? WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_PROZENT;
        set => NetzkanalSetzen(v => v with { ProzessEinheit = KanalEinheit(value) });
    }

    /// <summary>Die Zirkulationsleistung des Bestandswegs [kW]; leer = keine Zirkulation.</summary>
    public double? ZirkulationLeistung
    {
        get => Netzkanaele.ZirkulationLeistungKw;
        set => NetzkanalSetzen(v => v with { ZirkulationLeistungKw = value });
    }

    /// <summary>Die Laufzeit der Zirkulation [h/d]; leer = keine Zirkulation.</summary>
    public double? ZirkulationLaufzeit
    {
        get => Netzkanaele.ZirkulationLaufzeitHd;
        set => NetzkanalSetzen(v => v with { ZirkulationLaufzeitHd = value });
    }

    /// <summary>Die zwei Einheiten eines Kanalwerts.</summary>
    public IReadOnlyList<KiWahleintrag> NetzverlustHeizungEinheitWahl => Kanaleinheiten;

    /// <summary>Die zwei Einheiten eines Kanalwerts.</summary>
    public IReadOnlyList<KiWahleintrag> NetzverlustBrauchwasserEinheitWahl => Kanaleinheiten;

    /// <summary>Die zwei Einheiten eines Kanalwerts.</summary>
    public IReadOnlyList<KiWahleintrag> NetzverlustProzessEinheitWahl => Kanaleinheiten;

    private static readonly IReadOnlyList<KiWahleintrag> Kanaleinheiten = new[]
    {
        new KiWahleintrag(WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_PROZENT,
                          WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_PROZENT),
        new KiWahleintrag(WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_KWH,
                          WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_KWH)
    };

    private WindowsFormsApplication1.Netzverlustvorgabe Netzkanaele
        => Parameter?.Netzkanaele ?? WindowsFormsApplication1.Netzverlustvorgabe.Leer;

    /// <summary>Eine Einheit aus der Wahl; eine fremde lehnt die Sicht benannt ab.</summary>
    private static string KanalEinheit(string? wert)
    {
        string w = (wert ?? "").Trim();
        if (w == WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_PROZENT ||
            w == WindowsFormsApplication1.BedarfNetzKalenderSchema.EINHEIT_KWH) return w;
        throw new InvalidOperationException(string.Format(Resource.KI_DLG_SIM_NV_EINHEIT_UNBEKANNT, w));
    }

    /// <summary>Schreibt die ganze Vorgabe sofort; scheitert es, steht der Grund in der Ausnahme.</summary>
    private void NetzkanalSetzen(Func<WindowsFormsApplication1.Netzverlustvorgabe, WindowsFormsApplication1.Netzverlustvorgabe> aenderung)
    {
        ParameterDaten? p = Parameter;
        Func<WindowsFormsApplication1.Netzverlustvorgabe, bool>? schreiben = Wege?.NetzkanaeleSchreiben;
        if (p is null || schreiben is null)
            throw new InvalidOperationException(Resource.KI_SIM_KEIN_SCHREIBWEG);
        WindowsFormsApplication1.Netzverlustvorgabe neu = aenderung(p.Netzkanaele);
        WindowsFormsApplication1.Netzverlustvorgabe gespeichert = neu.Normalisiert();
        string? grund = gespeichert.Pruefen();
        if (grund is not null) throw new InvalidOperationException(grund);
        if (!schreiben(gespeichert))
            throw new InvalidOperationException(Resource.SIMKONF_MSG_NV_KANAL_FEHLER);
        p.Netzkanaele = gespeichert with
        {
            HeizungEinheit = gespeichert.HeizungEinheit ?? neu.HeizungEinheit,
            BrauchwasserEinheit = gespeichert.BrauchwasserEinheit ?? neu.BrauchwasserEinheit,
            ProzessEinheit = gespeichert.ProzessEinheit ?? neu.ProzessEinheit
        };
    }

    // =====================================================================
    //  Die Projekteinstellung „Aufheizoptimierung" (Entwurf KP3, Grundsatz 5; Welle O1)
    // =====================================================================
    //
    // Fünf Felder über EINEN Delegaten (AufheizvorgabeSchreiben): Jede Setzung schreibt die ganze
    // Einstellung sofort, wie ein Feld des Abschnitts. Die Normalisierung macht der Record
    // (Festlegung 24): „kälteste Stunde" und „täglich" werden NULL, ein leeres Zahlenfeld NULL, ein
    // getippter Wert bleibt. Was die Maske nicht zeigt, lehnt die Sicht benannt ab: die vier
    // Werte bei Schalter aus, ΔT_K bei der Bemessung „kälteste Stunde".

    /// <summary>Der Projektschalter „Aufheizoptimierung rechnen"; aus behält die übrigen Werte.</summary>
    public bool Aufheizoptimierung
    {
        get => Aufheizstand.An;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = Aufheizstand;
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(value, a.Bemessung, a.AbzugK, a.Reserve, a.Art, a.AufschlagH, a.AufschlagProzent));
        }
    }

    /// <summary>
    /// Die Bemessung als Steuerwert (<c>DbWerte.AUFHEIZ_BEMESSUNG_*</c>): <c>STUNDE</c> (Vorgabe) oder
    /// <c>STUNDE_ABZUG</c>; leer heißt die Vorgabe.
    /// </summary>
    public string AufheizBemessung
    {
        get => Aufheizstand.BemessungWirksam;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = AufheizEingeschaltet();
            string wert = Steuerwert(value, WindowsFormsApplication1.DbWerte.AUFHEIZ_BEMESSUNGEN,
                                     WindowsFormsApplication1.DbWerte.AUFHEIZ_BEMESSUNG_STUNDE);
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(a.An, wert, a.AbzugK, a.Reserve, a.Art, a.AufschlagH, a.AufschlagProzent));
        }
    }

    /// <summary>Die zwei Bemessungen mit ihren Namen auf der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> AufheizBemessungWahl => new[]
    {
        new KiWahleintrag(WindowsFormsApplication1.DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, Resource.SIMKONF_AUFH_BEMESSUNG_STUNDE),
        new KiWahleintrag(WindowsFormsApplication1.DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, Resource.SIMKONF_AUFH_BEMESSUNG_ABZUG)
    };

    /// <summary>ΔT_K [K] der Bemessung „kälteste Stunde − ΔT_K"; <c>null</c> = leer (Vorgabe 2 K).</summary>
    public double? AufheizAbzugK
    {
        get => Aufheizstand.AbzugK;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = AufheizEingeschaltet();
            if (!a.MitAbzug)
                throw new InvalidOperationException(Resource.KI_DLG_SIM_AUFH_NUR_ABZUG);
            Bereich(value, WindowsFormsApplication1.AufheizvorgabeSchema.ABZUG_MIN_K,
                    WindowsFormsApplication1.AufheizvorgabeSchema.ABZUG_MAX_K, Resource.SIMKONF_AUFH_LBL_ABZUG);
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(a.An, a.Bemessung, value, a.Reserve, a.Art, a.AufschlagH, a.AufschlagProzent));
        }
    }

    /// <summary>
    /// Die Aufheizreserve ρ in Prozent, wie das Feld sie zeigt; gespeichert als Anteil (Festlegung 15).
    /// <c>null</c> = leer (Vorgabe 20 %).
    /// </summary>
    public double? AufheizReserveProzent
    {
        get => Aufheizstand.Reserve is double r ? r * 100.0 : null;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = AufheizEingeschaltet();
            Bereich(value, 1.0, 100.0, Resource.SIMKONF_AUFH_LBL_RESERVE);
            double? anteil = value is double p ? p / 100.0 : null;
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(a.An, a.Bemessung, a.AbzugK, anteil, a.Art, a.AufschlagH, a.AufschlagProzent));
        }
    }

    /// <summary>
    /// Die Art der Aufheizzeit als Steuerwert (<c>DbWerte.AUFHEIZ_ART_*</c>): <c>TAEGLICH</c>
    /// (Vorgabe) oder <c>FEST</c>; leer heißt die Vorgabe.
    /// </summary>
    public string AufheizArt
    {
        get => Aufheizstand.ArtWirksam;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = AufheizEingeschaltet();
            string wert = Steuerwert(value, WindowsFormsApplication1.DbWerte.AUFHEIZ_ARTEN,
                                     WindowsFormsApplication1.DbWerte.AUFHEIZ_ART_TAEGLICH);
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(a.An, a.Bemessung, a.AbzugK, a.Reserve, wert, a.AufschlagH, a.AufschlagProzent));
        }
    }

    /// <summary>Die zwei Arten mit ihren Namen auf der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> AufheizArtWahl => new[]
    {
        new KiWahleintrag(WindowsFormsApplication1.DbWerte.AUFHEIZ_ART_TAEGLICH, Resource.SIMKONF_AUFH_ART_TAEGLICH),
        new KiWahleintrag(WindowsFormsApplication1.DbWerte.AUFHEIZ_ART_FEST, Resource.SIMKONF_AUFH_ART_FEST)
    };

    // Der Aufschlag (Welle O1b; E59 (2), Festlegungen 35, 36): zwei Felder über denselben Delegaten,
    // nur bei Schalter an (sonst benannt abgelehnt wie die Maske, die sie dann nicht zeigt); 0 und
    // leer werden NULL (der Record normalisiert), die übrigen Werte gehen unverändert mit.

    /// <summary>Der Aufschlag in Stunden 0 … 24; <c>null</c> = leer (kein Aufschlag).</summary>
    public int? AufheizAufschlagH
    {
        get => Aufheizstand.AufschlagH;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = AufheizEingeschaltet();
            Bereich(value, 0, WindowsFormsApplication1.Aufheizvorgabe.AUFSCHLAG_H_MAX, Resource.SIMKONF_AUFH_AUFSCHLAG_LBL_H);
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(a.An, a.Bemessung, a.AbzugK, a.Reserve, a.Art,
                                                                        value, a.AufschlagProzent));
        }
    }

    /// <summary>Der Aufschlag in Prozent der Stufenzahl n, 0 … 100; <c>null</c> = leer (kein Aufschlag).</summary>
    public double? AufheizAufschlagProzent
    {
        get => Aufheizstand.AufschlagProzent;
        set
        {
            WindowsFormsApplication1.Aufheizvorgabe a = AufheizEingeschaltet();
            Bereich(value, 0, WindowsFormsApplication1.Aufheizvorgabe.AUFSCHLAG_PROZENT_MAX,
                    Resource.SIMKONF_AUFH_AUFSCHLAG_LBL_PROZENT);
            AufheizSchreiben(new WindowsFormsApplication1.Aufheizvorgabe(a.An, a.Bemessung, a.AbzugK, a.Reserve, a.Art,
                                                                        a.AufschlagH, value));
        }
    }

    // =====================================================================
    //  Die Projekteinstellung „Einspeisegrenze" (Welle M5, PV3)
    // =====================================================================

    /// <summary>Der Zahlenwert der Einspeisegrenze in ihrer Einheit; <c>null</c> = keine Grenze.</summary>
    public double? Einspeisegrenze
    {
        get => Einspeisestand.Wert;
        set
        {
            if (value is double w && (double.IsNaN(w) || w < 0))
                Bereich(value, 0.0, double.MaxValue, Resource.SIMKONF_LBL_EINSPEISEGRENZE);
            EinspeisegrenzeSetzen(new WindowsFormsApplication1.Einspeisegrenze(value, Einspeisestand.Einheit));
        }
    }

    /// <summary>
    /// Die Einheit als Steuerwert (<c>DbWerte.EINSPEISEGRENZE_*</c>): <c>kW</c> (Vorgabe) oder <c>%</c>
    /// der installierten PV-Leistung; leer heißt die Vorgabe.
    /// </summary>
    public string EinspeisegrenzeEinheit
    {
        get => Einspeisestand.EinheitWirksam;
        set
        {
            string wert = Steuerwert(value, new[] { WindowsFormsApplication1.DbWerte.EINSPEISEGRENZE_KW,
                                                    WindowsFormsApplication1.DbWerte.EINSPEISEGRENZE_PROZENT },
                                     WindowsFormsApplication1.DbWerte.EINSPEISEGRENZE_KW);
            EinspeisegrenzeSetzen(new WindowsFormsApplication1.Einspeisegrenze(Einspeisestand.Wert, wert));
        }
    }

    /// <summary>Die zwei Einheiten mit ihren Namen auf der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> EinspeisegrenzeEinheitWahl => new[]
    {
        new KiWahleintrag(WindowsFormsApplication1.DbWerte.EINSPEISEGRENZE_KW, Resource.SIMKONF_EINSPEISEGRENZE_EINHEIT_KW),
        new KiWahleintrag(WindowsFormsApplication1.DbWerte.EINSPEISEGRENZE_PROZENT, Resource.SIMKONF_EINSPEISEGRENZE_EINHEIT_PROZENT)
    };

    private WindowsFormsApplication1.Einspeisegrenze Einspeisestand
        => Parameter?.Einspeisegrenze ?? WindowsFormsApplication1.Einspeisegrenze.Keine;

    /// <summary>Schreibt über den Delegaten des Abschnitts; ohne Weg und bei Fehlschlag benannt.</summary>
    private void EinspeisegrenzeSetzen(WindowsFormsApplication1.Einspeisegrenze neu)
    {
        ParameterDaten? p = Parameter;
        Func<WindowsFormsApplication1.Einspeisegrenze, bool>? schreiben = Wege?.EinspeisegrenzeSchreiben;
        if (p is null || schreiben is null)
            throw new InvalidOperationException(Resource.KI_SIM_KEIN_SCHREIBWEG);
        if (!schreiben(neu))
            throw new InvalidOperationException(Resource.SIMKONF_EINSPEISEGRENZE_FEHLER);
        p.Einspeisegrenze = neu;
    }

    // =====================================================================
    //  Die Projekteinstellung „Thermische Desinfektion" (Welle M7, BW5)
    // =====================================================================

    /// <summary>Läuft die thermische Desinfektion?</summary>
    public bool Desinfektion
    {
        get => Desinfektionsstand.Aktiv;
        set => DesinfektionSetzen(Desinfektionsstand with { Aktiv = value });
    }

    /// <summary>Intervall [Tage], 1 … 31; leer heißt 7.</summary>
    public int? DesinfektionIntervall
    {
        get => Desinfektionsstand.IntervallTage;
        set => DesinfektionSetzen(Desinfektionsstand with { IntervallTage = value });
    }

    /// <summary>Stunde des Tages, 0 … 23; leer heißt 2.</summary>
    public int? DesinfektionStunde
    {
        get => Desinfektionsstand.Stunde;
        set => DesinfektionSetzen(Desinfektionsstand with { Stunde = value });
    }

    /// <summary>Zieltemperatur [°C], 55 … 90; leer heißt 70.</summary>
    public double? DesinfektionZieltemperatur
    {
        get => Desinfektionsstand.ZielC;
        set => DesinfektionSetzen(Desinfektionsstand with { ZielC = value });
    }

    /// <summary>Volumen [l], 0 … 100 000; leer heißt das Volumen der Brauchwasserspeicher.</summary>
    public double? DesinfektionVolumen
    {
        get => Desinfektionsstand.VolumenL;
        set => DesinfektionSetzen(Desinfektionsstand with { VolumenL = value });
    }

    private WindowsFormsApplication1.Desinfektionsvorgabe Desinfektionsstand
        => Parameter?.Desinfektion ?? WindowsFormsApplication1.Desinfektionsvorgabe.Aus;

    /// <summary>Prüft und schreibt über den Delegaten des Abschnitts; ohne Weg und bei Fehlschlag benannt.</summary>
    private void DesinfektionSetzen(WindowsFormsApplication1.Desinfektionsvorgabe neu)
    {
        ParameterDaten? p = Parameter;
        Func<WindowsFormsApplication1.Desinfektionsvorgabe, bool>? schreiben = Wege?.DesinfektionSchreiben;
        if (p is null || schreiben is null)
            throw new InvalidOperationException(Resource.KI_SIM_KEIN_SCHREIBWEG);
        string? fehler = WindowsFormsApplication1.Desinfektionsvorgabe.Pruefen(neu.IntervallTage, neu.Stunde, neu.ZielC, neu.VolumenL);
        if (fehler != null) throw new ArgumentOutOfRangeException(nameof(neu), fehler);
        var normal = new WindowsFormsApplication1.Desinfektionsvorgabe(neu.Aktiv, neu.IntervallTage, neu.Stunde, neu.ZielC, neu.VolumenL);
        if (!schreiben(normal))
            throw new InvalidOperationException(Resource.SIMKONF_DESINFEKTION_FEHLER);
        p.Desinfektion = normal;
    }

    /// <summary>Die gespeicherte Einstellung; ohne Stand „aus".</summary>
    private WindowsFormsApplication1.Aufheizvorgabe Aufheizstand
        => Parameter?.Aufheizung ?? WindowsFormsApplication1.Aufheizvorgabe.Aus;

    /// <summary>Der Stand für einen Wert, den die Maske nur bei Schalter an zeigt — sonst benannt abgelehnt.</summary>
    private WindowsFormsApplication1.Aufheizvorgabe AufheizEingeschaltet()
    {
        WindowsFormsApplication1.Aufheizvorgabe a = Aufheizstand;
        if (!a.An) throw new InvalidOperationException(Resource.KI_DLG_SIM_AUFH_NICHT_AN);
        return a;
    }

    /// <summary>
    /// Ein Steuerwert der Wertliste (ohne Rücksicht auf Groß- und Kleinschreibung); leer heißt die
    /// Vorgabe, ein fremder Wert wird benannt abgelehnt.
    /// </summary>
    private static string Steuerwert(string? roh, IReadOnlyList<string> liste, string vorgabe)
    {
        if (string.IsNullOrWhiteSpace(roh)) return vorgabe;
        string t = roh.Trim();
        foreach (string w in liste)
            if (string.Equals(w, t, StringComparison.OrdinalIgnoreCase)) return w;
        throw new InvalidOperationException(string.Format(Resource.KI_DLG_SIM_AUFH_UNBEKANNT, t, string.Join(", ", liste)));
    }

    /// <summary>Prüft die Grenzen des Eingabefeldes; leer ist zulässig.</summary>
    private static void Bereich(double? wert, double min, double max, string feld)
    {
        if (wert is not double w) return;
        if (!double.IsNaN(w) && w >= min && w <= max) return;
        CultureInfo k = CultureInfo.CurrentCulture;
        throw new InvalidOperationException(string.Format(k, Resource.KI_FELD_BEREICH, feld, w.ToString(k),
            string.Format(k, Resource.KI_FELD_BEREICH_VON_BIS, min.ToString(k), max.ToString(k))));
    }

    /// <summary>
    /// Schreibt die ganze Einstellung über den Delegaten des Abschnitts und zieht den Stand erst nach,
    /// wenn das Schreiben angekommen ist; ohne Weg und bei Fehlschlag benannt.
    /// </summary>
    private void AufheizSchreiben(WindowsFormsApplication1.Aufheizvorgabe neu)
    {
        ParameterDaten? p = Parameter;
        Func<WindowsFormsApplication1.Aufheizvorgabe, bool>? schreiben = Wege?.AufheizvorgabeSchreiben;
        if (p is null || schreiben is null)
            throw new InvalidOperationException(Resource.KI_SIM_KEIN_SCHREIBWEG);
        if (!schreiben(neu))
            throw new InvalidOperationException(Resource.SIMKONF_AUFH_MSG_FEHLER);
        p.Aufheizung = neu;
    }

    /// <summary>
    /// Die gewählte Karte mit Quellenwahl (Wärmepumpe oder Heizkessel) als
    /// <c>ID_Anlage</c>; 0 = keine. Setzen wählt die Karte — derselbe Weg wie der Knopf an
    /// einer Meldung (<c>KarteHervorheben</c>).
    /// </summary>
    public int Quellanlage
    {
        get => Seite?.KiAnlage?.IdAnlage ?? 0;
        set => Absage(SeiteOderAbsage().KiAnlageWaehlen(value));
    }

    /// <summary>Die Karten mit Quellenwahl als Wahleinträge (Schlüssel = <c>ID_Anlage</c>).</summary>
    public IReadOnlyList<KiWahleintrag> QuellanlageWahl
        => KiMaskenanmeldung.Eintraege(Seite?.KiAnlagen, z => z.IdAnlage,
                                       z => string.IsNullOrWhiteSpace(z.Bezeichner) ? z.Kachel.Titel : z.Bezeichner);

    /// <summary>Der Quelltyp der gewählten Anlage (Steuerwert, nicht Anzeigetext).</summary>
    public string Waermequelle
    {
        get => Seite?.KiQuelle() ?? "";
        set => Absage(SeiteOderAbsage().KiQuelleSetzen(value ?? ""));
    }

    /// <summary>Die Quelltypen der gewählten Anlage.</summary>
    public IReadOnlyList<KiWahleintrag> WaermequelleWahl
    {
        get
        {
            IReadOnlyList<Quellentyp> typen = Seite?.KiQuellentypen() ?? Array.Empty<Quellentyp>();
            var liste = new List<KiWahleintrag>(typen.Count);
            foreach (Quellentyp t in typen) liste.Add(new KiWahleintrag(t.Wert, t.Text));
            return liste;
        }
    }

    /// <summary>Die konstante Quelltemperatur der gewählten Anlage [°C].</summary>
    public double QuelltemperaturKonstant
    {
        get => Seite?.KiQuelltemperatur() ?? 0.0;
        set => Absage(SeiteOderAbsage().KiQuelltemperaturSetzen(value));
    }

    /// <summary>Die Einsatzreihenfolge der gewählten Wärmepumpe; 0 = keine gewählt.</summary>
    public int WpPrioritaet
    {
        get => Seite?.KiAnlage?.Prioritaet ?? 0;
        set => Absage(SeiteOderAbsage().KiPrioritaetSetzen(value));
    }

    /// <summary>Der Betriebsmodus (<c>BM_Typ</c>) der gewählten Wärmepumpe als Steuerwert.</summary>
    public string WpBetriebsmodus
    {
        get => Seite?.KiBetriebsmodi().Aktuell ?? "";
        set => Absage(SeiteOderAbsage().KiBetriebsmodusSetzen(value ?? ""));
    }

    /// <summary>Die drei Betriebsmodi, die der Dialog anbietet.</summary>
    public IReadOnlyList<KiWahleintrag> WpBetriebsmodusWahl
    {
        get
        {
            var liste = new List<KiWahleintrag>();
            if (Seite is { } s)
                foreach ((string wert, string text) in s.KiBetriebsmodi().Modi)
                    liste.Add(new KiWahleintrag(wert, text));
            return liste;
        }
    }

    /// <summary>Schritt ①; <c>null</c> = er steht nicht.</summary>
    private SimulationKonfigSeite? Seite => _konfigseite?.Invoke();

    /// <summary>Schritt ① — oder die benannte Absage, dass er nicht steht.</summary>
    private SimulationKonfigSeite SeiteOderAbsage()
        => Seite ?? throw new InvalidOperationException(Resource.KI_SIM_SCHRITT1_FEHLT);

    /// <summary>
    /// Ein Grund der Seite wird zur benannten Absage — der Assistent meldet dann nicht
    /// „gesetzt", wo nichts geschah.
    /// </summary>
    private static void Absage(string? grund)
    {
        if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
    }

    // =====================================================================
    //  Der Reiter „Stromspeicher" — die Parameter der aktiven Variante
    //  (Welle KI-F2)
    // =====================================================================
    //
    // WARUM SIE HIER STEHEN UND NICHT IN EINER EIGENEN MASKE. Der Block ist ein
    // BLATT der Ansicht "Simulation" und kein Dialog: Er geht nicht auf, er steht
    // auf dem Reiter "Stromspeicher" von Schritt ③. Eine Maske ist, was offen ist -
    // und offen ist die Simulationsansicht.
    //
    // JEDES FELD SCHREIBT SOFORT, ueber denselben Weg wie das Feld auf dem
    // Bildschirm (SpeicherParameterBlock.Schreiben -> SpeicherfeldSchreiben je
    // Feldschluessel). Erst merken, dann schreiben - dieselbe Reihenfolge; sonst
    // stuende in der Maske eine andere Zahl als in der Datenbank.

    /// <summary>Der untere Ladezustand des Bandes [%].</summary>
    public double SpeicherSocMin
    {
        get => Speicher?.SoCMinProzent ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.SoCMin, value, s => s.SoCMinProzent = value);
    }

    /// <summary>Der obere Ladezustand des Bandes [%].</summary>
    public double SpeicherSocMax
    {
        get => Speicher?.SoCMaxProzent ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.SoCMax, value, s => s.SoCMaxProzent = value);
    }

    /// <summary>Lade- und Entladeleistung der Speicheranlage [kW].</summary>
    public double SpeicherLadeleistung
    {
        get => Speicher?.LadeleistungKw ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Leistung, value, s => s.LadeleistungKw = value);
    }

    /// <summary>Die Nennkapazität der Speicheranlage [kWh].</summary>
    public double SpeicherKapazitaet
    {
        get => Speicher?.KapazitaetKwh ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Kapazitaet, value, s => s.KapazitaetKwh = value);
    }

    /// <summary>Die Preisschwelle, unter der geladen wird [ct/kWh].</summary>
    public double SpeicherLadeschwelle
    {
        get => Speicher?.Ladeschwellwert ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Ladeschwelle, value, s => s.Ladeschwellwert = value);
    }

    /// <summary>Der Steuerwert der Betriebsart (nicht ihr Anzeigetext).</summary>
    public string SpeicherBetriebsart
    {
        get => Speicher?.Betriebsart ?? "";
        set => SpeicherWahl(SpeicherFeld.Betriebsart, value, s => s.Betriebsart = value ?? "");
    }

    /// <summary>Der Steuerwert der Berechnungsart (nicht ihr Anzeigetext).</summary>
    public string SpeicherBerechnungsart
    {
        get => Speicher?.Berechnungsart ?? "";
        set => SpeicherWahl(SpeicherFeld.Berechnungsart, value, s => s.Berechnungsart = value ?? "");
    }

    /// <summary>Die Zielschwelle der Lastspitzenkappung [kW]; 0 heißt „nicht gepflegt".</summary>
    public double SpeicherPeakZiel
    {
        get => Speicher?.PeakZiel ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.PeakZiel, value, s => s.PeakZiel = value);
    }

    /// <summary>Zieht die Zielschwelle sich selbst nach?</summary>
    public bool SpeicherPeakZielAdaptiv
    {
        get => Speicher?.PeakZielAdaptiv ?? false;
        set => SpeicherSchalter(SpeicherFeld.PeakZielAdaptiv, value, s => s.PeakZielAdaptiv = value);
    }

    /// <summary>Der Kompatibilitätsmodus der Dauernutzung.</summary>
    public bool SpeicherKompatibilitaet
    {
        get => Speicher?.Kompatibilitaet ?? false;
        set => SpeicherSchalter(SpeicherFeld.Kompatibilitaet, value, s => s.Kompatibilitaet = value);
    }

    /// <summary>Laden aus dem Überschuss der Photovoltaik.</summary>
    public bool SpeicherLadenAusPv
    {
        get => Speicher?.LadenAusPv ?? false;
        set => SpeicherSchalter(SpeicherFeld.LadenPv, value, s => s.LadenAusPv = value);
    }

    /// <summary>Laden aus dem Überschuss der BHKW.</summary>
    public bool SpeicherLadenAusBhkw
    {
        get => Speicher?.LadenAusBhkw ?? false;
        set => SpeicherSchalter(SpeicherFeld.LadenBhkw, value, s => s.LadenAusBhkw = value);
    }

    /// <summary>Entladen in das Netz erlaubt.</summary>
    public bool SpeicherNetzentladung
    {
        get => Speicher?.Netzentladung ?? false;
        set => SpeicherSchalter(SpeicherFeld.Netzentladung, value, s => s.Netzentladung = value);
    }

    /// <summary>
    /// Stromgeführter BHKW-Betrieb — sichtbar, aber dauerhaft gesperrt (Ausbaustufe 11).
    /// </summary>
    public bool SpeicherBhkwStromgefuehrt => Speicher?.BhkwStromgefuehrt ?? false;

    /// <summary>Der Kalkulationszins der Speicherwirtschaftlichkeit [%].</summary>
    public double SpeicherKapitalzins
    {
        get => Speicher?.Kapitalzins ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Kapitalzins, value, s => s.Kapitalzins = value);
    }

    /// <summary>Die Nutzungsdauer der Speicheranlage [a].</summary>
    public double SpeicherNutzungsdauer
    {
        get => Speicher?.Nutzungsdauer ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Nutzungsdauer, value, s => s.Nutzungsdauer = value);
    }

    /// <summary>Der Leistungspreis des Netzbetreibers [€/(kW·a)].</summary>
    public double SpeicherLeistungspreis
    {
        get => Speicher?.Leistungspreis ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Leistungspreis, value, s => s.Leistungspreis = value);
    }

    /// <summary>Der Aufschlag auf netzgeladene Energie [ct/kWh].</summary>
    public double SpeicherNetzladeaufschlag
    {
        get => Speicher?.Netzladeaufschlag ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Netzladeaufschlag, value, s => s.Netzladeaufschlag = value);
    }

    /// <summary>Der Steuerwert der Preisquelle (nicht ihr Anzeigetext).</summary>
    public string SpeicherPreisquelle
    {
        get => Speicher?.Preisquelle ?? "";
        set => SpeicherWahl(SpeicherFeld.Preisquelle, value, s => s.Preisquelle = value ?? "");
    }

    /// <summary>Die gewählte Preisreihe des Stromtarifs (KI-F1b).</summary>
    /// <remarks>
    /// <b>Sie hängt an der Preisquelle:</b> Mit ihr wechseln Beschriftung und Inhalt
    /// der Reihenauswahl. Eine Id, die die Liste gerade nicht führt, wird abgewiesen —
    /// dieselbe Regel wie bei den drei Steuerwerten darüber.
    /// </remarks>
    public int SpeicherPreisreihe
    {
        get => Speicher?.PreisreiheId ?? 0;
        set
        {
            SpeicherParameterDaten? s = Speicher;
            if (s is null) return;

            bool bekannt = false;
            foreach ((int id, string _) in s.Preisreihen)
                if (id == value) bekannt = true;

            if (!bekannt) return;

            SpeicherSetzen(SpeicherFeld.Preisreihe,
                           value.ToString(CultureInfo.InvariantCulture),
                           z => z.PreisreiheId = value);
        }
    }

    // ---- Die Auswahl der vier Wahlfelder (KI-F1b, KI-D-Q6) -----------------
    //
    // Die Anmeldung findet sie ueber die Namenskonvention <Eigenschaft>Wahl; der
    // Dialog braucht dafuer keine Zeile. Gerufen wird bei JEDEM Zugriff - die
    // Reihenauswahl wechselt mit der Preisquelle.

    /// <summary>Die Betriebsarten, die die Maske anbietet.</summary>
    public IReadOnlyList<KiWahleintrag> SpeicherBetriebsartWahl => Auswahl(Speicher?.Betriebsarten);

    /// <summary>Die Berechnungsarten, die die Maske anbietet.</summary>
    public IReadOnlyList<KiWahleintrag> SpeicherBerechnungsartWahl => Auswahl(Speicher?.Berechnungsarten);

    /// <summary>Die Preisquellen, die die Maske anbietet.</summary>
    public IReadOnlyList<KiWahleintrag> SpeicherPreisquelleWahl => Auswahl(Speicher?.Preisquellen);

    /// <summary>Die Preisreihen der gewählten Quelle.</summary>
    public IReadOnlyList<KiWahleintrag> SpeicherPreisreiheWahl
    {
        get
        {
            SpeicherParameterDaten? s = Speicher;
            if (s is null) return Array.Empty<KiWahleintrag>();

            var liste = new List<KiWahleintrag>(s.Preisreihen.Count);
            foreach ((int id, string text) in s.Preisreihen)
                liste.Add(new KiWahleintrag(id.ToString(CultureInfo.InvariantCulture), text));
            return liste;
        }
    }

    /// <summary>Steuerwerte der Maske als Wahleinträge; Schlüssel ist der Steuerwert.</summary>
    private static IReadOnlyList<KiWahleintrag> Auswahl(IReadOnlyList<Steuerwahl>? liste)
    {
        if (liste is null) return Array.Empty<KiWahleintrag>();

        var eintraege = new List<KiWahleintrag>(liste.Count);
        foreach (Steuerwahl wahl in liste) eintraege.Add(new KiWahleintrag(wahl.Wert, wahl.Text));
        return eintraege;
    }

    /// <summary>Den Aufschlag auf die Preisreihe anwenden.</summary>
    public bool SpeicherAufschlag
    {
        get => Speicher?.Aufschlag ?? false;
        set => SpeicherSchalter(SpeicherFeld.Aufschlag, value, s => s.Aufschlag = value);
    }

    // =====================================================================
    //  Schritt ③ — die Kennzahlen des Laufs (nur lesend)
    // =====================================================================

    /// <summary>Der Wärmebedarf des Projekts [MWh/a].</summary>
    public double WaermebedarfMwh => Kennzahlen?.WaermebedarfGesamtMwh ?? 0.0;

    /// <summary>Die Wärmedeckung des Laufs [%] — der Ring der Übersicht.</summary>
    public double WaermedeckungProzent => Uebersicht?.WaermedeckungProzent ?? 0.0;

    /// <summary>Der REST, der nach allen Erzeugern ungedeckt bleibt [MWh/a].</summary>
    public double RestwaermeMwh => Kennzahlen?.RestwaermeMwh ?? 0.0;

    /// <summary>Der Strombedarf des Projekts [MWh/a].</summary>
    public double StrombedarfMwh => Kennzahlen?.StrombedarfGesamtMwh ?? 0.0;

    /// <summary>Die Stromdeckung des Laufs [%] — der zweite Ring der Übersicht.</summary>
    public double StromdeckungProzent => Uebersicht?.StromdeckungProzent ?? 0.0;

    /// <summary>Der ungedeckte Reststrombedarf [MWh/a].</summary>
    public double ReststromMwh => Kennzahlen?.ReststromMwh ?? 0.0;

    /// <summary>Die Entladung des Stromspeichers im Lauf [MWh/a]; 0 ohne Speicher.</summary>
    public double SpeicherentladungMwh => Kennzahlen?.StromspeicherEntladungMwh ?? 0.0;

    /// <summary>
    /// Das SoC-Band des Stromspeichers („20 – 90 %"); leer, wenn das Projekt keine
    /// aktive Speichervariante führt.
    /// </summary>
    public string SpeicherSocBand
    {
        get
        {
            SpeicherParameterDaten? s = _ergebnis()?.Parameter?.Speicher;
            if (s is null || !s.VarianteVorhanden) return "";

            var k = CultureInfo.CurrentCulture;
            return s.SoCMinProzent.ToString("0.#", k) + " – " +
                   s.SoCMaxProzent.ToString("0.#", k) + " %";
        }
    }

    /// <summary>
    /// Die Warnungen und Hinweise des letzten Laufs; leer = keine.
    /// </summary>
    /// <remarks>
    /// Sie tragen die Kennungen, zu denen <c>HilfeWissen</c> seinen Aktionswissen-
    /// Abschnitt führt (<c>LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ</c>, …) — der Assistent
    /// findet die Erklärung damit auch ohne Modell.
    /// </remarks>
    public string Laufhinweise => _ergebnis()?.Laufmeldungen ?? "";

    /// <summary>
    /// UB‑E4 (Fachkonzept Übergabegrenze 7.4): die Betriebsbereiche der Wärmepumpe des Laufs — je Wert
    /// „Spaltenname=Wert“ mit den Spaltennamen des Ergebnisses als Schlüssel; leer ohne Bivalenzobjekt.
    /// </summary>
    public string WpBetriebsbereiche
    {
        get
        {
            WindowsFormsApplication1.Bereichskennzahlen? b = _ergebnis()?.Waermepumpe?.Bereiche;
            if (b is null) return "";
            return string.Join("; ", b.Schluesselwerte().Select(kv =>
                kv.Key + "=" + kv.Value.ToString("0.###", CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>
    /// KM3‑E3‑b (Fachkonzept Teillast und Takten 5.4): Teillast und Takten je Kältemaschine mit Teillastweg —
    /// „Anlage: Spaltenname=Wert; …“, Maschinen durch „ | “ getrennt; leer ohne solche Maschine.
    /// </summary>
    public string KmTeillast
    {
        get
        {
            IReadOnlyList<KaeltemaschineTeillastKachel>? l = _ergebnis()?.Bedarf?.Kaelte?.Teillast;
            if (l is null || l.Count == 0) return "";
            static string W(double? x) => x is double d ? d.ToString("0.###", CultureInfo.InvariantCulture) : "";
            return string.Join(" | ", l.Select(k => k.Anlage + ": " + string.Join("; ", new[]
            {
                "Taktstrom_kWh=" + W(k.TaktstromKwh), "Starts=" + W(k.Starts), "Teillastanteil_Prozent=" + W(k.TeillastanteilProzent),
                "Lastgrad_Mittel=" + W(k.Lastgrad), "EER_ohne_Hilfsstrom=" + W(k.EerOhneHilfsstrom)
            })));
        }
    }

    /// <summary>
    /// Die Speicherkapazität [kWh] der Autarkierechnung auf dem Blatt „Ergebnis"
    /// (Welle KI‑F6).
    /// </summary>
    /// <remarks>
    /// <para><b>Sie ist das EINZIGE echte Eingabefeld der neun Reiterblätter.</b>
    /// Alles andere darauf sind Schalter EINES BILDES — „sortiert", die Reihenhaken,
    /// die Streuwolken, die Nullzeilen —; sie schreiben nichts und leben in privaten
    /// Feldern, die der Reiter bei jedem Zeichenlauf neu aufbaut. Ein Setzer darauf
    /// schriebe in ein Feld, das der nächste Aufbau verwirft (Fachkonzept 11.6: eine
    /// Setzung, die der Anwender in der offenen Maske nicht nachlesen kann).</para>
    /// <para><b>Gesetzt wird über denselben Weg wie das Feld selbst:</b>
    /// <c>SimulationErgebnisSeite.KapazitaetGeaendert</c> rechnet die Autarkie neu
    /// (<c>AutarkieRechnen</c>) und tauscht den Stand aus. Steht das Blatt nicht,
    /// bleibt der Wert lesbar und die Setzung läuft benannt ins Leere.</para>
    /// </remarks>
    public double AutarkieSpeicherKWh
    {
        get => _ergebnis()?.Autarkie?.SpeicherKwh ?? 0.0;
        set => _autarkiespeicher?.Invoke(value);
    }

    // =====================================================================
    //  Die Auflösung der Ketten — jede null-Stufe hat einen Grund
    // =====================================================================

    /// <summary>
    /// Der Stand der fünf Laufparameter. <c>null</c> = die Plattform bietet sie nicht
    /// an; dann bleiben die fünf Felder bei ihren Vorgaben und sind nicht setzbar.
    /// </summary>
    private ParameterDaten? Parameter => _laufparameter();

    /// <summary>Die fünf Schreibwege; <c>null</c> = die Plattform schreibt sie nicht.</summary>
    private SimulationParameterDienste? Wege => _schreibwege();

    /// <summary>
    /// Die 13 Zahlen und sechs Summen des Laufs; <c>null</c> = es wurde nicht
    /// gerechnet, und die sieben Kennzahlen stehen auf 0.
    /// </summary>
    private WindowsFormsApplication1.SimulationErgebnisCtrl.UebersichtKennzahlen? Kennzahlen
        => _ergebnis()?.Kennzahlen;

    /// <summary>Präsenz und Ringmittelwerte; <c>null</c> = es wurde nicht gerechnet.</summary>
    private UebersichtDaten? Uebersicht => _ergebnis()?.Uebersicht;

    /// <summary>
    /// Der Stand des Reiters „Stromspeicher" — dasselbe Objekt, das
    /// <c>SpeicherParameterBlock</c> bearbeitet; <c>null</c> = es wurde nicht geladen.
    /// </summary>
    private SpeicherParameterDaten? Speicher => _ergebnis()?.Parameter?.Speicher;

    /// <summary>
    /// Merkt eine Speicherzahl im Stand und schreibt sie — in dieser Reihenfolge und
    /// INVARIANT, wie <c>SpeicherParameterBlock.Zahl</c>.
    /// </summary>
    private void SpeicherZahl(string feld, double wert, Action<SpeicherParameterDaten> merken)
        => SpeicherSetzen(feld, wert.ToString(CultureInfo.InvariantCulture), merken);

    /// <summary>Merkt einen Speicherschalter und schreibt ihn als 0/1.</summary>
    private void SpeicherSchalter(string feld, bool wert, Action<SpeicherParameterDaten> merken)
        => SpeicherSetzen(feld, wert ? "1" : "0", merken);

    /// <summary>
    /// Merkt einen Steuerwert und schreibt ihn wörtlich. <b>Ein Wert, den die Liste der
    /// Maske nicht führt, wird abgewiesen</b> — die Klappliste kennt genau diese
    /// Steuerwerte, und ein erfundener stünde danach in der Datenbank, ohne dass ihn
    /// jemand wieder auswählen könnte.
    /// </summary>
    private void SpeicherWahl(string feld, string? wert, Action<SpeicherParameterDaten> merken)
    {
        SpeicherParameterDaten? s = Speicher;
        if (s is null || wert is null) return;

        IReadOnlyList<Steuerwahl> liste = feld switch
        {
            SpeicherFeld.Betriebsart => s.Betriebsarten,
            SpeicherFeld.Berechnungsart => s.Berechnungsarten,
            _ => s.Preisquellen
        };

        bool bekannt = false;
        foreach (Steuerwahl wahl in liste)
            if (string.Equals(wahl.Wert, wert, StringComparison.Ordinal)) bekannt = true;

        if (!bekannt) return;

        SpeicherSetzen(feld, wert, merken);
    }

    private void SpeicherSetzen(string feld, string wert, Action<SpeicherParameterDaten> merken)
    {
        SpeicherParameterDaten? s = Speicher;
        if (s is null) return;

        merken(s);
        _speicherwege?.Invoke()?.SpeicherfeldSchreiben?.Invoke(feld, wert);
    }
}
