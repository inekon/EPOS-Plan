namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Der KONTEXT der Kostenverwaltung (iU9-W4.2): Komponente bzw. Anlage,
/// Kategorie und — im Stammkontext — die Variante.
///
/// <para>Im Bestand war das keine Klasse, sondern der Zustand dreier
/// Steuerelemente (<c>cmbKomponente</c>, <c>rbInvest</c>/<c>rbBetrieb</c>,
/// <c>cmbVariante</c>), den <c>Kontext_Geaendert</c> abfragte. Hier ist es ein
/// Wert, den die Komponente an die Hülle reicht — sie fragt damit dieselbe
/// Frage, ohne die Hülle ihre Felder lesen zu lassen.</para>
/// </summary>
/// <param name="EintragId">Id des gewählten Klapplisteneintrags — im
/// Stammkontext die Komponente, im Projektmodus die Anlagenzeile (Ä20).</param>
/// <param name="Invest">Investitionskosten (sonst Betriebskosten).</param>
/// <param name="VarianteId">Die gewählte Vorlage/Variante; <c>null</c> = die
/// erste bzw. keine (im Projektmodus immer <c>null</c>).</param>
public sealed record KostenKomponenteKontext(int? EintragId, bool Invest, int? VarianteId);

/// <summary>
/// EINE Zeile des Positionsrasters, wie die Komponente sie sieht (iU9-W4.2).
///
/// <para><b>Warum veränderlich.</b> Die WinForms-Zeile schrieb ihre Felder in
/// die übergebene <c>KostenVorlagenPosition</c> und ließ sie dort liegen, bis
/// „Speichern" oder „OK" sie in die Datenbank trug (Ä12/Ä19). Genau diese
/// Arbeitsteilung bleibt: Die Komponente ändert die Zeile, die Hülle schreibt
/// sie. Ein unveränderlicher Record wäre hier die falsche Form — der Aufrufer
/// ERWARTET die Änderung am Objekt (Risiko R3 des Wellenplans nennt den
/// umgekehrten Fall).</para>
/// </summary>
public sealed class KostenPositionZeile
{
    /// <summary>Id der Position — Schlüssel für Löschen, Editor und Worst/Best.</summary>
    public int Id { get; set; }

    /// <summary>Bezeichnung (<c>txtBezeichnung</c>).</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Id der Bemessung in <see cref="KostenKomponenteStand.Bemessungen"/>.</summary>
    public int? BemessungId { get; set; }

    /// <summary>Der Satz (<c>txtSatz</c>).</summary>
    public double? Satz { get; set; }

    /// <summary>Nutzungsdauer in Jahren (<c>txtNutzung</c>).</summary>
    public double? Nutzungsdauer { get; set; }

    /// <summary>Einheit hinter dem Satzfeld (<c>lblEinheit</c>) — folgt der Bemessung.</summary>
    public string Einheit { get; set; } = "";

    /// <summary>Der Betrag als fertiger Text (<c>txtBetrag</c>), nie eingebbar.</summary>
    public string BetragText { get; set; } = "";

    /// <summary>Satz und Betrag sind EIN Wert (Kopplungsregel KL4 / § 5.4).</summary>
    public bool Kette { get; set; }

    /// <summary>Kurztext des Betragsfeldes — je Kontext ein anderer.</summary>
    public string BetragKurztext { get; set; } = "";

    /// <summary>
    /// ANWENDERBEFUND 14.09.2026: Diese Zeile hat KEINE Bezugsgröße — ihr Betrag ist
    /// der erfasste (Anwenderentscheid I-2), bei einer satzbasierten Zeile also 0.
    ///
    /// <para><b>Warum ein eigenes Kennzeichen.</b> Den Grund nennt seit H4c
    /// <see cref="BetragKurztext"/> — aber nur als Werkzeugtipp des Betragsfeldes.
    /// Der Anwender sah im Raster eine 0 und keinen Hinweis; er hätte mit der Maus
    /// auf dem Feld stehen bleiben müssen, um zu erfahren, warum. Mit dem Kennzeichen
    /// trägt die Zeile ein sichtbares Zeichen, und der Dialog sammelt die Gründe unter
    /// dem Raster.</para>
    /// </summary>
    public bool OhneBasis { get; set; }

    /// <summary>
    /// 14.09.2026: WIE die Bezugsgröße entstanden ist, wenn sie GERECHNET und nicht
    /// am Gerät abgelesen wurde — „0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW".
    /// Leer, wo die Größe unmittelbar in einer Gerätemaske steht. Der Satz kommt
    /// fertig aus dem Kern (<c>TechnikPlanwertCtrl.BaugroesseHerleitung</c>).
    /// </summary>
    public string BasisHerleitung { get; set; } = "";

    /// <summary>
    /// U28: Die HERLEITUNGSZEILE unter dem Betrag — Bezugsgröße, Herkunft und
    /// Kaskadenrunde („× 300,00 kW · P_el der Anlage · Runde 1"), bei einer
    /// absoluten Zeile „Satz = Betrag". Sie kommt fertig aus dem Kern
    /// (<c>KostenHerleitung</c>); leer heißt keine Zeile — im Stammkontext und dort,
    /// wo die Bezugsgröße fehlt (da steht das ⚠ und der Grund unter dem Raster).
    /// </summary>
    public string Herleitung { get; set; } = "";

    /// <summary>Empfehlungsbereich als Kurztext des Satzfeldes; leer = keiner.</summary>
    public string EmpfehlungKurztext { get; set; } = "";

    /// <summary>
    /// U31: Derselbe Bereich als SICHTBARE Zeile unter dem Satzfeld
    /// („Empfehlung 0,02 bis 0,04 €/kWh"). Er kommt fertig aus dem Kern
    /// (<c>KostenHerleitung</c>); leer heißt keine Zeile.
    /// </summary>
    public string EmpfehlungZeile { get; set; } = "";

    /// <summary>
    /// ANWENDERENTSCHEID 19.09.2026: Die Hinweiszeile unter der Herleitung —
    /// „Vorlage ‚Standard': % des Endenergiebedarfs". Sie steht nur an einer
    /// Projektposition, deren Bemessung von der der Standardvorlage abweicht; der
    /// Satz kommt fertig aus dem Kern (<c>KostenHerleitung</c>), leer heißt keine
    /// Zeile.
    /// </summary>
    public string VorlagenHinweis { get; set; } = "";

    /// <summary>
    /// Der Eintrag der Bemessungsliste, den „übernehmen" setzt (Id in
    /// <see cref="KostenKomponenteStand.Bemessungen"/>); <c>null</c> = die Bemessung
    /// der Vorlage steht in diesem Kontext nicht zur Auswahl, dann bleibt es beim
    /// Hinweistext ohne Knopf.
    /// </summary>
    public int? VorlagenBemessungId { get; set; }

    /// <summary>
    /// U8 (Stufe S2): Die HERLEITUNGSZEILE unter der Nutzungsdauer — „15 a · Vorgabe
    /// der Technik", „… · AfA-Tabelle: ‹Positionsart›" oder „… · eigener Wert". Sie
    /// kommt fertig aus dem Kern (<c>NutzungsdauerCtrl.Herleitungszeile</c>); leer
    /// heißt keine Zeile — auf der Betriebsseite und überall dort, wo keine Dauer
    /// gepflegt ist (die nennt die Tafel „Ersatz und Restwert").
    /// </summary>
    public string NutzungsdauerHerleitung { get; set; } = "";

    /// <summary>
    /// ETAPPE E10 (Stufe S3): Die HERKUNFTSZEILE unter dem Satzfeld einer
    /// Betriebsposition „Instandhaltung …"/„Wartung …" mit „% der Investition" —
    /// „2 % · Satz aus Nutzungsdauertabelle: Heizkessel · Wärmeerzeuger (Instandsetzung)".
    /// Sie steht, wenn das Feld den Satz der Tabelle trägt (vorbelegt oder mit der Vorlage
    /// übernommen); ein leeres Feld rechnet mit nichts und bekommt keine Zeile. Fertig aus
    /// dem Kern (<c>NutzungsdauerSatzCtrl.Herleitungszeile</c>), leer heißt keine Zeile.
    /// </summary>
    public string SatzHerleitung { get; set; } = "";

    /// <summary>Darf die Zeile bearbeitet werden? (Auslieferungsvorlagen nicht.)</summary>
    public bool Schreibbar { get; set; } = true;
}

/// <summary>
/// U30 — EINE Zeile der Tafel „Ersatz und Restwert" unter dem Raster der
/// Investitionsseite.
///
/// <para>Alles fertig gesetzt: Ersatzjahre, Ersatzbeträge und der lineare Restwert
/// kommen aus derselben Funktion, die die Kapitalwertrechnung durchläuft
/// (<c>KapitalwertRechner.Ersatz</c>); gerechnet und formatiert wird im Kern
/// (<c>ErsatzRestwertTafel</c>). Die Oberfläche zeigt nur an — auch den
/// Gedankenstrich, der dort steht, wo keine Nutzungsdauer gepflegt ist.</para>
/// </summary>
/// <param name="Position">Bezeichnung; in der Summenzeile der Komponentenname.</param>
/// <param name="IstSumme">Die Summenzeile der Komponente — sie wird hervorgehoben.</param>
/// <param name="Betrag">Betrag netto, gesetzt („196.080,00").</param>
/// <param name="Dauer">„15 a", „—" oder — in der Summenzeile — „3 von 4".</param>
/// <param name="Ersatz">„Jahr 15", „— deckungsgleich mit T" oder „— läuft wie T".</param>
/// <param name="ErsatzBarwert">„Barwert 132.149"; leer = keine Ersatzbeschaffung.</param>
/// <param name="Restwert">Restwert im Jahr T, nominal.</param>
/// <param name="RestwertBarwert">„Barwert 75.995"; leer = kein Restwert.</param>
/// <param name="Herkunft">Woher die Nutzungsdauer stammt.</param>
public sealed record ErsatzRestwertZeile(string Position, bool IstSumme, string Betrag,
                                         string Dauer, string Ersatz, string ErsatzBarwert,
                                         string Restwert, string RestwertBarwert,
                                         string Herkunft);

/// <summary>
/// U8 (Stufe S2): Was der Knopf „Nutzungsdauern vorbelegen…" bewirkt hat.
/// </summary>
/// <param name="Gefuellt">Zahl der Positionen, die eine Dauer bekommen haben.</param>
/// <param name="Belegt">Zahl der Positionen, die BEREITS eine Dauer tragen und deren
/// Vorgabe davon abweicht — sie bleiben, bis der Anwender das Überschreiben
/// bestätigt (Anwenderentscheid ND‑Q4 (b): nichts geschieht still).</param>
public sealed record NutzungsdauerVorbelegung(int Gefuellt, int Belegt);

/// <summary>
/// U31 — EINE Zeile der Gruppe „Endenergie je Komponente" unter dem Raster der
/// Betriebsseite.
///
/// <para>Alles fertig gesetzt: Die Mengen kommen aus dem jüngsten Simulationslauf
/// (<c>EndenergieAufloeser</c>), gerechnet und formatiert wird im Kern
/// (<c>KostenBetriebsstand.Endenergie</c>). Die Oberfläche zeigt nur an — auch den
/// Gedankenstrich, der dort steht, wo ein Arbeitspreis fehlt.</para>
/// </summary>
/// <param name="Komponente">Komponente und Anlagenbezeichner („BHKW — Modul 1").</param>
/// <param name="Bedarf">Jahresbedarf, gesetzt („4.342.100 kWh").</param>
/// <param name="Kosten">Arbeitskosten („312.631,20 €/a") oder Gedankenstrich.</param>
/// <param name="Basis">Woher die Menge stammt.</param>
public sealed record EndenergieZeile(string Komponente, string Bedarf, string Kosten, string Basis);

/// <summary>
/// Was die Hülle zu einem <see cref="KostenKomponenteKontext"/> antwortet
/// (iU9-W4.2) — alles, was der Dialog danach zeigt.
///
/// <para>Im Bestand verteilte sich dasselbe auf <c>KopfAnzeigen</c>,
/// <c>VariantenLaden</c>, <c>RasterAufbauen</c>, <c>SummenAnzeigen</c> und
/// <c>ErtragReiterSteuern</c>. Ein Wert statt fünf Methoden heißt: Der Dialog
/// fragt einmal und zeigt, was zurückkommt — und die Hülle behält die Regel,
/// welche Liste zu welchem Kontext gehört (Regel F4).</para>
/// </summary>
public sealed class KostenKomponenteStand
{
    /// <summary>Überschrift (<c>lblTitel</c>).</summary>
    public string Titel { get; set; } = "";

    /// <summary>Unterzeile (<c>lblUntertitel</c>).</summary>
    public string Untertitel { get; set; } = "";

    /// <summary>Die wählbaren Vorlagen/Varianten; leer im Projektmodus.</summary>
    public IReadOnlyList<(int Id, string Text)> Varianten { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Die gewählte Variante.</summary>
    public int? VarianteId { get; set; }

    /// <summary>Zeigt die Variantenzeile (nur im Stammkontext, Ä-KD6a).</summary>
    public bool VariantePflegbar { get; set; }

    /// <summary>Die gewählte Vorlage ist eine Auslieferungsvorlage (<c>lblReadOnly</c>).</summary>
    public bool NurLesen { get; set; }

    /// <summary>Die Positionen in Anzeigereihenfolge.</summary>
    public IReadOnlyList<KostenPositionZeile> Zeilen { get; set; }
        = Array.Empty<KostenPositionZeile>();

    /// <summary>Die wählbaren Bemessungen dieses Kontexts (§ 5.3, gefiltert).</summary>
    public IReadOnlyList<(int Id, string Text)> Bemessungen { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>
    /// Dieselben Bemessungen als Wahleinträge für den Hilfe-Assistenten (KI-F1b,
    /// KI-D-Q6) — die Anmeldung findet sie über die Namenskonvention
    /// <c>&lt;Eigenschaft&gt;Wahl</c> zur Spalte <c>BemessungId</c>.
    /// </summary>
    public IReadOnlyList<KiKern.KiWahleintrag> BemessungIdWahl
    {
        get
        {
            var liste = new List<KiKern.KiWahleintrag>(Bemessungen.Count);
            foreach ((int id, string text) in Bemessungen)
                liste.Add(new KiKern.KiWahleintrag(
                    id.ToString(System.Globalization.CultureInfo.InvariantCulture), text));
            return liste;
        }
    }

    /// <summary>Überschrift der Betragsspalte („Betrag netto [€]" bzw. „[€/a]").</summary>
    public string SpalteBetrag { get; set; } = "";

    /// <summary>Zeigt die Spalte Nutzungsdauer (nur bei Investitionskosten).</summary>
    public bool MitNutzungsdauer { get; set; }

    /// <summary>Zeigt das ± je Zeile (nur im Projektmodus).</summary>
    public bool MitWorstBest { get; set; }

    /// <summary>Der Summenfuß: Nettosumme (stark) und Bruttozeile.</summary>
    public IReadOnlyList<(string Text, bool Stark)> Summen { get; set; }
        = Array.Empty<(string, bool)>();

    /// <summary>Ist eine neue Position möglich? (<c>btnPositionNeu.Enabled</c>)</summary>
    public bool PositionNeuMoeglich { get; set; }

    /// <summary>Löschen der Variante möglich? (<c>btnVarianteLoeschen.Enabled</c>)</summary>
    public bool VarianteLoeschbar { get; set; }

    /// <summary>
    /// U31: Die Zeile ÜBER dem Raster der Betriebsseite — aus welchem Lauf die
    /// Mengen stammen („Mengen stammen aus dem Simulationslauf vom …") bzw. der
    /// Grund „kein Simulationslauf". Leer = keine Zeile (Stammkontext und
    /// Investitionsseite, wo keine Menge aus dem Lauf kommt).
    /// </summary>
    public string Laufstand { get; set; } = "";

    /// <summary>
    /// U31: Die Doppelpflege-Warnung der Kohärenzprüfung, wortgleich
    /// (<c>KOH_HILFSENERGIE_DOPPELT</c>) — Hilfsenergie ist an der Anlage UND als
    /// Kostenposition gepflegt. Leer = keine Doppelpflege, dann kein Banner.
    /// </summary>
    public string DoppelpflegeWarnung { get; set; } = "";

    /// <summary>
    /// U31: Die Gruppe „Endenergie je Komponente" unter dem Raster der
    /// Betriebsseite — je Anlage mit Endenergie eine Zeile. Leere Liste = keine
    /// Gruppe.
    /// </summary>
    public IReadOnlyList<EndenergieZeile> Endenergie { get; set; }
        = Array.Empty<EndenergieZeile>();

    /// <summary>
    /// U30: Die Tafel „Ersatz und Restwert" unter dem Raster der Investitionsseite —
    /// je Position eine Zeile, zuletzt die Summenzeile der Komponente. Leere Liste =
    /// keine Tafel (Betriebsseite, Katalogkontext, kein Betrag).
    /// </summary>
    public IReadOnlyList<ErsatzRestwertZeile> ErsatzRestwert { get; set; }
        = Array.Empty<ErsatzRestwertZeile>();

    /// <summary>
    /// U30: Der Satz ÜBER der Tafel (Konzept Wirtschaftlichkeit § 2.13 (3)) —
    /// Betrachtungszeitraum über der Vorgabe der Technik, k von n Positionen ohne
    /// Dauer. Leer = nichts zu prüfen, dann kein Hinweis.
    /// </summary>
    public string ErsatzRestwertHinweis { get; set; } = "";

    /// <summary>U30: Überschrift der Restwertspalte samt T („Restwert Jahr 20").</summary>
    public string SpalteRestwert { get; set; } = "";

    /// <summary>
    /// U8 (Stufe S2): Ist der Knopf „Nutzungsdauern vorbelegen…" bedienbar? Nur auf
    /// der Investitionsseite einer schreibbaren Vorlage bzw. eines Projekts.
    /// </summary>
    public bool NutzungsdauerVorbelegbar { get; set; }

    /// <summary>
    /// ETAPPE E10 (Stufe S3): Derselbe Knopf auf der BETRIEBSSEITE — als „Sätze
    /// vorbelegen…": Er füllt die leeren Sätze der Positionen „Instandhaltung …"/
    /// „Wartung …" mit „% der Investition" aus der Nutzungsdauertabelle. Nur auf der
    /// Betriebsseite einer schreibbaren Vorlage bzw. eines Projekts.
    /// </summary>
    public bool SaetzeVorbelegbar { get; set; }

    /// <summary>Zeigt den Abschnitt „Ertrag/Bonus" (FK5: nur BHKW und Photovoltaik).</summary>
    public bool ErtragSichtbar { get; set; }

    /// <summary>Die Werte dieses Abschnitts, wenn er sichtbar ist.</summary>
    public IReadOnlyDictionary<string, object>? ErtragGaben { get; set; }
}
