using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine vorgeschlagene Bauteilzeile</b> (<c>Tab_Bauteil</c>) samt Summenfeld, Quellentität und
    /// Herkunft je Wert. Die Zeile selbst trägt eine negative vorläufige Id (Muster A6) und — wenn
    /// sie einen Aufbau hat — die vorläufige Id seines <see cref="GebaeudeAufbauzeile"/>.
    /// </summary>
    internal sealed class GebaeudeBauteilzeile
    {
        internal GebaeudeBauteilzeile(BauteilModel bauteil, string summenfeld, string quelltyp, string kennung, string seite)
        {
            Bauteil = bauteil;
            Summenfeld = summenfeld;
            Quelltyp = quelltyp;
            Kennung = kennung;
            Seite = seite;
        }

        /// <summary>Die Zeile mit vorläufiger (negativer) Id.</summary>
        internal BauteilModel Bauteil { get; }

        /// <summary>
        /// Das Flächenfeld des Gebäudeeditors, in das die Fläche zählt (<see cref="GebaeudeZielfelder.FLAECHE_AUSSENWAND"/>,
        /// …, <see cref="GebaeudeZielfelder.FENSTER_GESAMT"/>); <c>null</c> = innere Masse (Gruppe IW).
        /// </summary>
        internal string Summenfeld { get; }

        /// <summary>Typ der Quellentität (<c>Surface</c>, <c>Opening</c>, <c>IfcWall</c> …); <c>null</c> = Vorgabezeile ohne Quelle.</summary>
        internal string Quelltyp { get; }

        /// <summary>Kennung der Quellentität, ungekürzt; <c>null</c> = Vorgabezeile ohne Quelle.</summary>
        internal string Kennung { get; }

        /// <summary>Bei innerer Masse die Seite der Trennfläche (<c>A</c> = der erste beheizte Raum, <c>B</c> = der andere); sonst <c>null</c>.</summary>
        internal string Seite { get; }

        /// <summary>Herkunft der Fläche.</summary>
        internal Importherkunft HerkunftFlaeche { get; set; }

        /// <summary>Herkunft des U-Werts — auch, wenn er aus den Schichten folgt (die Schichten stammen aus der Datei).</summary>
        internal Importherkunft HerkunftU { get; set; }

        /// <summary>Herkunft des g-Werts; <see cref="Importherkunft.Leer"/> = Wert des Gebäudes.</summary>
        internal Importherkunft HerkunftG { get; set; }

        /// <summary>Herkunft der Neigung; <see cref="Importherkunft.Leer"/> = Vorgabe nach Bauteilart.</summary>
        internal Importherkunft HerkunftNeigung { get; set; }

        /// <summary>Herkunft des Azimuts; <see cref="Importherkunft.Leer"/> = keiner.</summary>
        internal Importherkunft HerkunftAzimut { get; set; }

        /// <summary>Herkunft des Aufbaus; <see cref="Importherkunft.Leer"/> = keiner (nur U-Wert).</summary>
        internal Importherkunft HerkunftAufbau { get; set; }

        /// <summary>Der U-Wert, den die Datei einträgt [W/(m²K)]; <c>null</c> = keiner.</summary>
        internal double? UDatei { get; set; }

        /// <summary>Der U-Wert aus den Schichten des Aufbaus [W/(m²K)]; <c>null</c> = kein Aufbau.</summary>
        internal double? USchichten { get; set; }

        /// <summary>Kurzfassung für Tests.</summary>
        public override string ToString()
            => Bauteil.Bauteilart + " " + Bauteil.Bezeichner + " " + Bauteil.Flaeche.ToString("0.###", CultureInfo.InvariantCulture)
               + " m² " + (Bauteil.Randbedingung ?? "(innen)") + " → " + (Summenfeld ?? "IW");
    }

    /// <summary>
    /// <b>Ein vorgeschlagener Aufbau</b> (<c>Tab_Bauteilaufbau</c> samt <c>Tab_Bauteilschicht</c>,
    /// Projektkopie) — die Schichten innen → außen, Stoffwerte als Kopie; vorläufige negative Id.
    /// </summary>
    internal sealed class GebaeudeAufbauzeile
    {
        internal GebaeudeAufbauzeile(BauteilaufbauModel aufbau, string quelltyp, string kennung, bool gegenseite, bool richtungAngenommen,
                                     Importherkunft herkunft = Importherkunft.Leer, IReadOnlyList<int?> stammbaustoffe = null,
                                     IReadOnlyList<GebaeudeBaustoffquelle> baustoffquellen = null)
        {
            Aufbau = aufbau;
            Quelltyp = quelltyp;
            Kennung = kennung;
            Gegenseite = gegenseite;
            RichtungAngenommen = richtungAngenommen;
            Herkunft = herkunft;
            Stammbaustoffe = stammbaustoffe ?? aufbau.Schichten.Select(_ => (int?)null).ToArray();
            Baustoffquellen = baustoffquellen ?? Array.Empty<GebaeudeBaustoffquelle>();
        }

        /// <summary>
        /// Herkunft der Stoffwerte des Aufbaus: das Format (<see cref="Importherkunft.Ifc"/>,
        /// <see cref="Importherkunft.GbXml"/>) oder <see cref="Importherkunft.Katalog"/>, sobald eine Schicht
        /// ihre Werte aus dem Namensabgleich hat (Mehrzonenkonzept 3.5, Herkunftskennzeichen).
        /// </summary>
        internal Importherkunft Herkunft { get; }

        /// <summary>
        /// Je Schicht (in der Reihenfolge von <c>Aufbau.Schichten</c>) der Katalogbaustoff
        /// (<c>Tab_Baustoff_STAMM.ID</c>), aus dem sie ihre Werte hat; <c>null</c> = Werte der Datei oder
        /// ruhende Luftschicht. Der Schreibweg kopiert den Baustoff in das Projekt und setzt
        /// <c>ID_Baustoff</c> der Schicht auf die Projektkopie.
        /// </summary>
        internal IReadOnlyList<int?> Stammbaustoffe { get; }

        /// <summary>Die Baustoffe der Datei, die über den Abgleich einen Katalogbaustoff tragen — die Paarungen für <c>Tab_Importzuordnung</c> (Ziel <c>ID_Baustoff</c>).</summary>
        internal IReadOnlyList<GebaeudeBaustoffquelle> Baustoffquellen { get; }

        /// <summary>Der Aufbau mit vorläufiger (negativer) Id und seinen Schichten, innen → außen.</summary>
        internal BauteilaufbauModel Aufbau { get; }

        /// <summary>Typ der Quellentität (<c>Construction</c>, <c>IfcMaterialLayerSet</c>).</summary>
        internal string Quelltyp { get; }

        /// <summary>Kennung der Quellentität, ungekürzt.</summary>
        internal string Kennung { get; }

        /// <summary>Ist es die gespiegelte Schichtfolge derselben Konstruktion (die andere Seite einer Trennfläche)?</summary>
        internal bool Gegenseite { get; }

        /// <summary>Ist die Schichtrichtung nur angenommen (gbXML: „erste Schicht außen", Datenaustauschkonzept 3.7)?</summary>
        internal bool RichtungAngenommen { get; }
    }

    /// <summary>
    /// Ein Baustoff der Datei, der über den Namensabgleich einen Katalogbaustoff trägt — Quellentität
    /// (<c>IfcMaterial</c> mit dem Namen als Kennung, gbXML <c>Material</c> mit seiner <c>id</c>) und
    /// <c>Tab_Baustoff_STAMM.ID</c>. Der Schreibweg paart die Quellentität mit der Projektkopie.
    /// </summary>
    internal sealed record GebaeudeBaustoffquelle(string Quelltyp, string Kennung, int IdStamm);

    /// <summary>
    /// <b>Ein Materialname der Datei</b> mit dem Ergebnis des Namensabgleichs — die Liste, an der die
    /// Anwenderzuordnung (Abschnitt „Baustoffe" des Importdialogs) ansetzt: Name, Stufe, Baustoff oder
    /// „ohne Treffer", Zahl der Schichten.
    /// </summary>
    internal sealed class GebaeudeMaterialzeile
    {
        internal GebaeudeMaterialzeile(string name, string quelltyp, string kennung)
        {
            Name = name ?? "";
            Quelltyp = quelltyp;
            Kennung = kennung ?? "";
            Normiert = Baustoffabgleich.Schluessel(name);
        }

        /// <summary>Der Name, wie die Datei ihn schreibt.</summary>
        internal string Name { get; }

        /// <summary>Der normalisierte Name (Kern nach N1/N2) — der Schlüssel einer gemerkten Zuordnung.</summary>
        internal string Normiert { get; }

        /// <summary>Typ der Quellentität (<c>IfcMaterial</c>, <c>Material</c>).</summary>
        internal string Quelltyp { get; }

        /// <summary>Kennung der ersten Quellentität dieses Namens (IFC: der Name, gbXML: die <c>id</c>).</summary>
        internal string Kennung { get; }

        /// <summary>Das Ergebnis des Abgleichs; <c>null</c> ohne Abgleich.</summary>
        internal Abgleichtreffer Treffer { get; set; }

        /// <summary>
        /// Zahl der Schichten mit diesem Namen, je Aufbau des Abbilds gezählt — beim IFC-Weg je Bauteil (jedes
        /// Bauteil trägt seinen Schichtsatz), beim gbXML-Weg je Konstruktion.
        /// </summary>
        internal int Schichten { get; set; }

        /// <summary>Zahl der Schichten, deren Stoffwerte ganz aus der Datei stammen (im Band).</summary>
        internal int SchichtenAusDatei { get; set; }

        /// <summary>Zahl der Schichten, die mindestens einen Stoffwert aus dem Katalog tragen.</summary>
        internal int SchichtenAusKatalog { get; set; }

        /// <summary>Braucht der Name einen Baustoff (mindestens eine Schicht ohne vollständige Werte der Datei)?</summary>
        internal bool BrauchtAbgleich => Schichten > SchichtenAusDatei;

        /// <summary>Kurzfassung für Tests.</summary>
        public override string ToString()
            => Name + " (" + Schichten.ToString(CultureInfo.InvariantCulture) + ") → " + (Treffer?.ToString() ?? "ohne Abgleich");
    }

    /// <summary>Wie der Vorschlag die innere Masse trägt — nach Datenlage (Anwenderentscheid vom 25.09.2026).</summary>
    internal enum Innenweg
    {
        /// <summary>
        /// Innenbauteile übernommen: Jede innere Trennfläche trägt Fläche und vollständige Stoffwerte,
        /// die Innenfläche ist plausibel — je Seite eine Zeile innerhalb der Zone mit Aufbau.
        /// </summary>
        Bauteile = 0,

        /// <summary>
        /// Innenfläche aus der Datei: keine Innenzeilen, der Innenflächenfaktor aus der gemessenen
        /// Fläche beider Seiten, die Masse aus der Bauweise (Innengruppe im Klassenweg).
        /// </summary>
        Innenflaechenfaktor = 1,

        /// <summary>Keine Innenfläche in der Datei oder keine Nutzfläche: der Faktor bleibt leer (Vorgabe 2,5).</summary>
        Vorgabe = 2,
    }

    /// <summary>
    /// <b>Der Bauteilvorschlag eines importierten Gebäudes</b> (Stufe G4b; Konzept 7.6 Punkt 3,
    /// Datenaustauschkonzept 3.3–3.8, Mehrzonenkonzept 4.2 und 6) — EINE Zone mit echten Bauteilen,
    /// Aufbauten und Schichten, gebildet aus dem formatfreien <see cref="GebaeudeAbbild"/> mit der
    /// Raumauswahl des Zuordnungsdialogs. Ohne Datenbank; geschrieben wird er über
    /// <c>GebaeudeZonenCtrl.VorschlagSchreiben</c>, dann rechnet das Gebäude den Bauteilweg.
    ///
    /// <para><b>Im Einklang mit dem Einzonenweg.</b> Die Zone ist die der Zuordnung
    /// (<see cref="GebaeudeAggregation"/>, Zonenregel X4): Nutzfläche, Volumen und Raumhöhe der
    /// übernommenen beheizten Räume. Die Bauteile ordnet <see cref="GebaeudeHuelleneinordnung"/> nach
    /// denselben Regeln in dieselben Summenfelder — jede Gruppe summiert dieselbe Nettofläche wie das
    /// Summenfeld des Gebäudeeditors; weicht eine ab, lehnt der Vorschlag benannt ab
    /// (<see cref="SUMME_ABWEICHUNG"/>).</para>
    ///
    /// <list type="bullet">
    /// <item><b>Bauteilart und Randbedingung</b> (Datenaustauschkonzept 3.5): die Art des Abbilds; an
    /// Außenluft und Erdreich die Seite. <b>Gegen einen unbeheizten oder unbekannten Raum</b> steht
    /// die Randbedingung <c>UNBEHEIZT</c> (G3: Kellertemperatur des Gebäudes) — für Boden, Decke und
    /// Wand; die Fläche zählt wie im Einzonenweg (Boden → Grundfläche, Decke → Dach, Wand → Sonstige).
    /// Eine <b>Vorhangfassade ist transparent</b> (Anwenderentscheid): eine Zeile der Art
    /// <c>VORHANGFASSADE</c>, die im Fensterzweig mit Sonneneintrag rechnet — U und g aus der Datei,
    /// sonst U aus der Fenstervorgabe der Klasse und g, Rahmenanteil und Verschattung leer (dann die
    /// Werte des Gebäudes bzw. die Vorgaben); in den Summenfeldern bleibt sie, wie im Einzonenweg,
    /// unter „Sonstige Flächen", und die Summenprobe hält die Gruppe Sonstige samt Vorhangfassaden
    /// gegen dieses Feld. Ein Fenster oder eine Vorhangfassade an Erdreich rechnet an Außenluft.</item>
    /// <item><b>Fläche:</b> Nettofläche nach dem Fensterabzug (U14); Fenster und Türen je eine Zeile
    /// mit ihrer Fläche. Fehlt der Gruppe jede Fläche und setzt die Zuordnung eine Vorgabe (IFC,
    /// Umsetzungskonzept 3.4), trägt sie ein einziges flächenloses Bauteil der Gruppe, sonst eine
    /// Vorgabezeile ohne Quelle.</item>
    /// <item><b>Neigung und Azimut</b> wie im Abbild, 0° = Nord — beim IFC-Weg samt Nordwinkel, beim
    /// gbXML-Weg ohne <c>CADModelAzimuth</c> (Datenaustauschkonzept 3.2); aus der Sicht des beheizten
    /// Raums (ist er der zweite Nachbar, gespiegelt). Fenster und Türen nehmen den Azimut ihrer Wand.
    /// Ein nicht waagerechtes Bauteil an Außenluft ohne Azimut wird benannt abgelehnt (N1.46, 9).</item>
    /// <item><b>Aufbau und U-Wert</b> (3.6): Ein vollständiger Aufbau (Dicke, λ, ρ, c je Schicht, im
    /// Band) wird geschrieben, die Schichten innen → außen, der U-Wert bleibt leer und folgt aus den
    /// Schichten (G3-Regel: der Bauteilweg rechnet aus dem Aufbau); trägt die Datei zusätzlich einen
    /// U-Wert, werden beide verglichen und eine Abweichung über 5 % gemeldet. Ohne vollständige
    /// Stoffwerte kein Aufbau, nur der U-Wert — der der Datei, sonst der aus einer masselosen
    /// Schichtung, sonst die Vorgabe der Baualtersklasse (Herkunft <c>VORGABE</c>).</item>
    /// <item><b>Namensabgleich der Baustoffe</b> (Mehrzonenkonzept 3.5/6.3, nur mit einem
    /// <see cref="Baustoffabgleich"/>): Ein Stoffwert gilt nur im Plausibilitätsband (λ [0,005; 500],
    /// ρ [5; 8 000], c [100; 5 000]); fehlt er oder liegt er außerhalb, sucht der Abgleich nach dem
    /// Materialnamen (N1…N7). Trifft er, trägt die Schicht die Werte des Baustoffs — ein Wert der Datei
    /// im Band behält Vorrang, fehlt λ, gilt d/R der Datei, wenn sie einen R-Wert trägt —, die Dicke
    /// aus der Datei; eine Luftschicht wird ruhende Luftschicht, eine Schraffur verworfen. Sind danach
    /// alle Schichten vollständig, entsteht der Aufbau mit Herkunft <c>KATALOG</c>, sobald ein Wert aus
    /// dem Katalog stammt, und die Schicht merkt sich ihren Katalogbaustoff (<see cref="GebaeudeAufbauzeile.Stammbaustoffe"/>).
    /// Ohne Treffer bleibt es beim Rückfall (nur U-Wert), und der Name steht in der Meldung
    /// <see cref="BAUSTOFF_UNBEKANNT"/>. Trägt eine Schicht vollständige Werte der Datei, ist der
    /// Abgleich die Gegenprobe (<see cref="LAMBDA_GEGENPROBE_GRENZE"/>). Die Liste
    /// <see cref="Materialien"/> nennt jeden Materialnamen mit Stufe, Baustoff und Zahl der Schichten.</item>
    /// <item><b>Innere Masse — nach Datenlage</b> (Anwenderentscheid vom 25.09.2026, <see cref="Innenweg"/>):
    /// Die Innenfläche beider Seiten zählt immer — eine Trennfläche zwischen zwei beheizten Räumen
    /// derselben Zone mit beiden Seiten, eine zu einem beheizten Raum eines anderen Gebäudes mit der
    /// eigenen. Die symmetrische Reduktion nach VDI 6007-1 Gl. (12)–(16) bildet je Seite die Masse bis
    /// zur Mittelebene ab (Mehrzonenkonzept 2.2, Nr. 3); liegen beide Seiten in der Zone, trägt die
    /// Innengruppe die ganze Wand. <b>Vollständig</b> ist die Datenlage, wenn jede innere Trennfläche
    /// eine Fläche und einen vollständigen Aufbau trägt und die Innenfläche beider Seiten im Band
    /// <see cref="INNENFLAECHE_FAKTOR_MIN"/> … <see cref="INNENFLAECHE_FAKTOR_MAX"/> × Nutzfläche
    /// liegt: dann je Seite eine Zeile innerhalb der Zone (leere Randbedingung) mit der Nettofläche
    /// und dem Aufbau aus ihrer Sicht (<see cref="Innenweg.Bauteile"/>). <b>Sonst</b> keine
    /// Innenzeilen, sondern der Innenflächenfaktor aus der Datei — gemessene Innenfläche ÷ Nutzfläche
    /// (<see cref="Innenflaechenfaktor"/>, für <c>Tab_Gebaeude.Innenflaechenfaktor</c>); die Masse
    /// bleibt die der Bauweise, die Innengruppe rechnet den Klassenweg mit gemessenem A_IW
    /// (<see cref="Innenweg.Innenflaechenfaktor"/>). Ohne jede Innenfläche bleibt der Faktor leer
    /// (<see cref="Innenweg.Vorgabe"/>, 2,5). Eine Meldung nennt den Weg samt Grund.</item>
    /// <item><b>Herkunft</b> je Zeile: das Format; <c>VORGABE</c>, sobald Fläche oder U-Wert eine
    /// Vorgabe ist. Die Herkunft je Wert steht an der <see cref="GebaeudeBauteilzeile"/>.</item>
    /// </list>
    ///
    /// <para><b>Benannt abgelehnt</b> (Stufe Fehler, <see cref="Abgelehnt"/>) wird, was sich nicht
    /// abbilden lässt: kein Gebäude unter dem Index, keine beheizten Räume, kein Außenbauteil, ein
    /// Bauteil an Außenluft ohne Azimut, ein opakes Bauteil ohne U-Wert und ohne Vorgabe, eine negative
    /// Nettofläche, eine Summe ungleich der Zuordnung — und zuletzt alles, was die Abbildung
    /// (<see cref="GebaeudeZonenabbildung.AlsZonensatz"/>) oder der Bauteilweg
    /// (<see cref="ErsatzparameterRC.AusBauteilweg(BauteilwegGebaeude, IReadOnlyList{BauteilEingang})"/>)
    /// ablehnt: Der Vorschlag rechnet sich zur Probe einmal durch. Nie eine Ausnahme.</para>
    ///
    /// <para><b>Meldungen</b> tragen die Schlüssel <c>IMP_BAUTEIL_PROT_*</c> (Konstanten unten);
    /// Texte liefert <see cref="GanglinienProtokollText"/> über die Ressourcen — bis sie dort stehen,
    /// erscheint der Schlüssel mit seinen Werten.</para>
    /// </summary>
    internal sealed class GebaeudeBauteilvorschlag
    {
        // ==================================================================
        //  Meldungskennungen
        // ==================================================================

        /// <summary>F — {0} Index, {1} Zahl der Gebäude der Datei: Unter dem Index steht kein Gebäude.</summary>
        internal const string KEIN_GEBAEUDE = "IMP_BAUTEIL_PROT_KEIN_GEBAEUDE";
        /// <summary>F — {0} Gebäude: Kein beheizter Raum, also keine Zone.</summary>
        internal const string KEINE_BEHEIZTEN_RAEUME = "IMP_BAUTEIL_PROT_KEINE_BEHEIZTEN_RAEUME";
        /// <summary>F — {0} Gebäude: Kein opakes Außenbauteil; der Bauteilweg braucht mindestens eines.</summary>
        internal const string KEINE_AUSSENBAUTEILE = "IMP_BAUTEIL_PROT_KEINE_AUSSENBAUTEILE";
        /// <summary>F — {0} Zahl, {1} Beispiele: Bauteil an Außenluft, nicht waagerecht, ohne Azimut.</summary>
        internal const string AZIMUT_FEHLT = "IMP_BAUTEIL_PROT_AZIMUT_FEHLT";
        /// <summary>F — {0} Zahl, {1} Beispiele, {2} Klasse: Kein U-Wert, keine Schichten, keine Vorgabe.</summary>
        internal const string UWERT_FEHLT = "IMP_BAUTEIL_PROT_UWERT_FEHLT";
        /// <summary>F — {0} Summenfeld, {1} Summe des Vorschlags, {2} Summe der Zuordnung [m²].</summary>
        internal const string SUMME_ABWEICHUNG = "IMP_BAUTEIL_PROT_SUMME_ABWEICHUNG";
        /// <summary>F — {0} Meldung der Abbildung bzw. des Bauteilwegs: Die Probe über den Bauteilweg scheitert.</summary>
        internal const string BAUTEILWEG = "IMP_BAUTEIL_PROT_BAUTEILWEG";
        /// <summary>W — {0} Zahl, {1} Beispiele: Bauteile, Fenster oder Türen ohne Fläche — nicht übernommen.</summary>
        internal const string OHNE_FLAECHE = "IMP_BAUTEIL_PROT_OHNE_FLAECHE";
        /// <summary>W — {0} Zahl, {1} Beispiele: Flächen ohne Nachbarraum — keinem Gebäude zugeordnet, nicht übernommen.</summary>
        internal const string OHNE_NACHBAR = "IMP_BAUTEIL_PROT_OHNE_NACHBAR";
        /// <summary>W — {0} Aufbau, {1} U der Datei, {2} U aus den Schichten, {3} Abweichung [%]: Es rechnet der Aufbau.</summary>
        internal const string U_ABWEICHUNG = "IMP_BAUTEIL_PROT_U_ABWEICHUNG";
        /// <summary>W — {0} Aufbau, {1} Grund: Stoffwerte unvollständig oder außerhalb des Bandes — kein Aufbau, nur U-Wert.</summary>
        internal const string STOFFWERTE_UNVOLLSTAENDIG = "IMP_BAUTEIL_PROT_STOFFWERTE_UNVOLLSTAENDIG";
        /// <summary>I — {0} Zahl, {1} Klasse: U-Werte aus der Baualtersklasse.</summary>
        internal const string U_VORGABE = "IMP_BAUTEIL_PROT_U_VORGABE";
        /// <summary>I — {0} Zahl, {1} Klasse, {2} Klasse der Quelle: U-Werte als freier Wert nach Stein/Loga (2025), weil die Klasse keinen Katalogsatz hat (E51).</summary>
        internal const string U_VORGABE_FREI = "IMP_BAUTEIL_PROT_U_VORGABE_FREI";
        /// <summary>I — {0} Summenfeld, {1} Fläche [m²]: Fläche einer Gruppe ohne gelesene Fläche aus der Vorgabe.</summary>
        internal const string FLAECHE_VORGABE = "IMP_BAUTEIL_PROT_FLAECHE_VORGABE";
        /// <summary>I — {0} Trennflächen, {1} Zeilen, {2} Innenfläche beider Seiten [m²], {3} je Nutzfläche [–]: Innenbauteile übernommen.</summary>
        internal const string INNEN_BAUTEILE = "IMP_BAUTEIL_PROT_INNEN_BAUTEILE";
        /// <summary>I — {0} Innenflächenfaktor [–], {1} Innenfläche [m²], {2} Zahl ohne, {3} Zahl der Trennflächen: Innenfläche aus der Datei, Masse aus der Bauweise, weil Stoffwerte oder Flächen fehlen.</summary>
        internal const string INNEN_FAKTOR_STOFFWERTE = "IMP_BAUTEIL_PROT_INNEN_FAKTOR_STOFFWERTE";
        /// <summary>W — {0} Innenflächenfaktor [–], {1} Innenfläche [m²], {2} untere, {3} obere Grenze [–]: Innenfläche aus der Datei, Masse aus der Bauweise, weil die Innenfläche nicht plausibel ist.</summary>
        internal const string INNEN_FAKTOR_UNPLAUSIBEL = "IMP_BAUTEIL_PROT_INNEN_FAKTOR_UNPLAUSIBEL";
        /// <summary>I — {0} Vorgabe [–]: Die Datei führt keine Innenflächen — Innenflächenfaktor Vorgabe, Masse aus der Bauweise.</summary>
        internal const string INNEN_VORGABE = "IMP_BAUTEIL_PROT_INNEN_VORGABE";
        /// <summary>I — {0} Innenfläche [m²], {1} Vorgabe [–]: Ohne Nutzfläche lässt sich die Innenfläche nicht beziehen — Vorgabe, Masse aus der Bauweise.</summary>
        internal const string INNEN_OHNE_NUTZFLAECHE = "IMP_BAUTEIL_PROT_INNEN_OHNE_NUTZFLAECHE";
        /// <summary>I — {0} Zahl: Trennflächen zu beheizten Räumen eines anderen Gebäudes — nur die eigene Seite.</summary>
        internal const string GEBAEUDETRENNFLAECHE = "IMP_BAUTEIL_PROT_GEBAEUDETRENNFLAECHE";
        /// <summary>I — {0} Zahl, {1} Fläche [m²]: gegen unbeheizte oder unbekannte Räume mit der Kellertemperatur.</summary>
        internal const string UNBEHEIZT = "IMP_BAUTEIL_PROT_UNBEHEIZT";
        /// <summary>I — {0} Zahl: Vorhangfassaden rechnen transparent mit Sonneneintrag; in den Summenfeldern stehen sie unter „Sonstige Flächen".</summary>
        internal const string VORHANGFASSADE = "IMP_BAUTEIL_PROT_VORHANGFASSADE";
        /// <summary>I — {0} Zahl: Fenster oder Vorhangfassaden an Erdreich rechnen an Außenluft.</summary>
        internal const string FENSTER_ERDREICH = "IMP_BAUTEIL_PROT_FENSTER_ERDREICH";
        /// <summary>I — {0} Kennung: Nettofläche der Datei 0 — keine Zeile, die Öffnungen bleiben.</summary>
        internal const string NETTOFLAECHE_NULL = "IMP_BAUTEIL_PROT_NETTOFLAECHE_NULL";
        /// <summary>I — {0} Winkel [°]: Die Azimute tragen die Nordrichtung der Datei (IFC).</summary>
        internal const string NORDWINKEL_ANGEWANDT = "IMP_BAUTEIL_PROT_NORDWINKEL_ANGEWANDT";
        /// <summary>I — {0} Winkel [°]: Die Nordangabe der Datei ist nicht aufaddiert (gbXML, 3.2).</summary>
        internal const string NORDWINKEL_NICHT_ANGEWANDT = "IMP_BAUTEIL_PROT_NORDWINKEL_NICHT_ANGEWANDT";

        /// <summary>F — {0} Gebäude-Id: Das Gebäude gibt es nicht (Schreibweg).</summary>
        internal const string GEBAEUDE_FEHLT = "IMP_BAUTEIL_PROT_GEBAEUDE_FEHLT";
        /// <summary>F — {0} Gebäude-Id: Das Gebäude ist keine Projektkopie — Aufbauten brauchen ein Projekt (Schreibweg).</summary>
        internal const string KEINE_PROJEKTKOPIE = "IMP_BAUTEIL_PROT_KEINE_PROJEKTKOPIE";
        /// <summary>F — {0} Gebäude-Id, {1} Zahl der Zonen: Das Gebäude trägt schon eine Zone; G3 kennt nur eine (Schreibweg).</summary>
        internal const string ZONE_VORHANDEN = "IMP_BAUTEIL_PROT_ZONE_VORHANDEN";
        /// <summary>F — {0} Grund: Nichts geschrieben (Prüfung vor dem Schreiben oder Fehler im Vorgang).</summary>
        internal const string NICHT_GESCHRIEBEN = "IMP_BAUTEIL_PROT_NICHT_GESCHRIEBEN";

        // ---- Namensabgleich der Baustoffe (Mehrzonenkonzept 3.5/6.3) --------------------------

        /// <summary>I — {0} Schichten mit Katalogwerten, {1} vervollständigte Aufbauten, {2} getroffene Namen, {3} Namen ohne vollständige Werte der Datei.</summary>
        internal const string ABGLEICH = "IMP_BAUTEIL_PROT_ABGLEICH";
        /// <summary>W — {0} Zahl, {1} Namen: Materialnamen ohne Treffer im Baustoffkatalog — ihre Aufbauten tragen nur den U-Wert (Mehrzonenkonzept 6.6, <c>…_BAUSTOFF_UNBEKANNT</c>).</summary>
        internal const string BAUSTOFF_UNBEKANNT = "IMP_BAUTEIL_PROT_BAUSTOFF_UNBEKANNT";
        /// <summary>W — {0} Zahl, {1} Namen: Stoffwerte außerhalb des Plausibilitätsbands gelten als nicht geliefert (Mehrzonenkonzept 6.6, <c>…_STOFFWERT_UNGUELTIG</c>).</summary>
        internal const string STOFFWERT_UNGUELTIG = "IMP_BAUTEIL_PROT_STOFFWERT_UNGUELTIG";
        /// <summary>W — {0} Zahl, {1} Namen: Schichten ohne Stoff (Schraffur, leer) verworfen (N6).</summary>
        internal const string SCHICHT_VERWORFEN = "IMP_BAUTEIL_PROT_SCHICHT_VERWORFEN";
        /// <summary>I — {0} Zahl: Luftschichten als ruhende Luftschicht nach DIN EN ISO 6946 (N6).</summary>
        internal const string LUFTSCHICHT = "IMP_BAUTEIL_PROT_LUFTSCHICHT";
        /// <summary>W — {0} Name, {1} λ der Datei, {2} Baustoff, {3} λ des Katalogs, {4} Abweichung [%]: die Gegenprobe; es rechnen die Werte der Datei.</summary>
        internal const string LAMBDA_GEGENPROBE = "IMP_BAUTEIL_PROT_LAMBDA_GEGENPROBE";

        /// <summary>
        /// <b>Die Schwelle der Gegenprobe</b> (Datenaustauschkonzept 3.6: bei gbXML ist der Abgleich
        /// meist nur die Gegenprobe): Weicht λ der Datei um mehr als 50 % vom λ des getroffenen
        /// Katalogbaustoffs ab, wird es gemeldet. Begründung: Ein Synonym trifft den Vertreter einer
        /// Stoffreihe, und innerhalb einer Reihe streut λ mit der Rohdichte um bis zu etwa diesen Betrag
        /// (Kalksandstein 1400 … 2000: 0,70 … 1,10; Porenbeton 350 … 600: 0,11 … 0,19; Mineralwolle
        /// λD 0,032 … 0,040) — das ist Streuung, kein Fehler. Darüber liegt eher ein falscher Stoff oder
        /// eine falsche Einheit (BTU-Werte, Faktor 1 000). Es rechnen immer die Werte der Datei.
        /// </summary>
        internal const double LAMBDA_GEGENPROBE_GRENZE = 0.5;

        /// <summary>Typ der Quellentität eines IFC-Baustoffs.</summary>
        internal const string QUELLTYP_IFC_BAUSTOFF = "IfcMaterial";

        /// <summary>Typ der Quellentität eines gbXML-Baustoffs.</summary>
        internal const string QUELLTYP_GBXML_BAUSTOFF = "Material";

        /// <summary>Relative Abweichung zwischen U-Wert der Datei und U-Wert aus den Schichten, ab der gemeldet wird.</summary>
        internal const double U_ABWEICHUNG_GRENZE = 0.05;

        /// <summary>
        /// <b>Untere Grenze der plausiblen Innenfläche</b> beider Seiten je Nutzfläche [–]. Das Band
        /// <see cref="INNENFLAECHE_FAKTOR_MIN"/> … <see cref="INNENFLAECHE_FAKTOR_MAX"/> liegt mit dem
        /// Faktor 2,5 zu beiden Seiten um die Vorgabe f_IW = 2,5 (A_m/A_f nach DIN EN ISO 13790,
        /// Tabelle 12: 2,5 bis 3,5 je nach Bauart; Konzept 4.3): Ein Wohngebäude hat Innenwände
        /// (beidseitig etwa eine bis zwei Nutzflächen) und je Geschossdecke zwei Seiten. Unter dem
        /// 0,4-Fachen der Vorgabe fehlt der Datei ein wesentlicher Teil der Innenbauteile (etwa die
        /// Geschossdecken oder die Innenwände eines Geschosses) — die Zeilen trügen zu wenig Masse.
        /// </summary>
        internal const double INNENFLAECHE_FAKTOR_MIN = 1.0;

        /// <summary>
        /// <b>Obere Grenze der plausiblen Innenfläche</b> je Nutzfläche [–] — das Doppelte der Vorgabe:
        /// Darüber zählt die Datei Flächen mehrfach (Schalen, Raumgrenzen je Teilfläche) oder trägt
        /// eine falsche Einheit. Begründung des Bands: <see cref="INNENFLAECHE_FAKTOR_MIN"/>.
        /// </summary>
        internal const double INNENFLAECHE_FAKTOR_MAX = 5.0;

        /// <summary>Relative Toleranz der Summenprobe gegen die Zuordnung.</summary>
        internal const double SUMMEN_TOLERANZ = 1e-9;

        /// <summary>Die Bezeichnung der Vorgabezeile eines Daches ohne gelesene Fläche.</summary>
        internal const string VORGABEZEILE_DACH = "Dach (Vorgabe)";

        /// <summary>Die Bezeichnung der Vorgabezeile einer Grundfläche ohne gelesene Fläche.</summary>
        internal const string VORGABEZEILE_GRUND = "Grundfläche (Vorgabe)";

        /// <summary>Die Summenfelder, gegen die der Vorschlag die Zuordnung hält.</summary>
        internal static readonly IReadOnlyList<string> Summenfelder = new[]
        {
            GebaeudeZielfelder.FLAECHE_AUSSENWAND, GebaeudeZielfelder.FLAECHE_DACH, GebaeudeZielfelder.FLAECHE_GRUND,
            GebaeudeZielfelder.FLAECHE_SONSTIGE, GebaeudeZielfelder.FENSTER_GESAMT,
        };

        private readonly List<GebaeudeBauteilzeile> _zeilen = new List<GebaeudeBauteilzeile>();
        private readonly List<GebaeudeAufbauzeile> _aufbauten = new List<GebaeudeAufbauzeile>();
        private readonly List<GebaeudeQuellzuordnung> _raeume = new List<GebaeudeQuellzuordnung>();
        private readonly List<PruefMeldung> _meldungen = new List<PruefMeldung>();
        private readonly List<GebaeudeMaterialzeile> _materialien = new List<GebaeudeMaterialzeile>();

        private GebaeudeBauteilvorschlag() { }

        // ==================================================================
        //  Inhalt
        // ==================================================================

        /// <summary>Format der Datei (<see cref="GebaeudeQuelle.FORMAT_GBXML"/>, <see cref="GebaeudeQuelle.FORMAT_IFC"/>).</summary>
        internal string Format { get; private set; } = "";

        /// <summary>Der Dateiname — die <c>Quelle</c> der Aufbauten.</summary>
        internal string Dateiname { get; private set; } = "";

        /// <summary>Die Kennung des Gebäudes in der Datei.</summary>
        internal string Gebaeudekennung { get; private set; }

        /// <summary>Die Baualtersklasse, mit der die Vorgaben gezogen sind (gewählt oder aus dem Baujahr); <c>null</c> = keine.</summary>
        internal char? Baualtersklasse { get; private set; }

        /// <summary>Die Nordrichtung der Datei [°]; <c>null</c> = keine Angabe.</summary>
        internal double? NordwinkelGrad { get; private set; }

        /// <summary>Tragen die Azimute die Nordrichtung (IFC), oder steht sie nur als Angabe daneben (gbXML)?</summary>
        internal bool NordwinkelAngewandt { get; private set; }

        /// <summary>Die Zone mit vorläufiger Id −1 und ihren Bauteilen (in der Reihenfolge von <see cref="Zeilen"/>); <c>null</c>, wenn keine zu bilden war.</summary>
        internal ZoneModel Zone { get; private set; }

        /// <summary>Herkunft der Nutzfläche der Zone.</summary>
        internal Importherkunft HerkunftNutzflaeche { get; private set; }

        /// <summary>Herkunft des Volumens der Zone.</summary>
        internal Importherkunft HerkunftVolumen { get; private set; }

        /// <summary>Herkunft der Raumhöhe der Zone.</summary>
        internal Importherkunft HerkunftRaumhoehe { get; private set; }

        /// <summary>Die Bauteilzeilen — dieselben Modelle wie <c>Zone.Bauteile</c>.</summary>
        internal IReadOnlyList<GebaeudeBauteilzeile> Zeilen => _zeilen;

        /// <summary>Die Aufbauten, je Konstruktion und Schichtfolge einer.</summary>
        internal IReadOnlyList<GebaeudeAufbauzeile> Aufbauten => _aufbauten;

        /// <summary>Die übernommenen beheizten Räume als Paarung auf die Zone (Ziel-Id erst nach dem Schreiben).</summary>
        internal IReadOnlyList<GebaeudeQuellzuordnung> Raeume => _raeume;

        /// <summary>Die Meldungen des Vorschlags (ohne die des Lesers und der Zuordnung).</summary>
        internal IReadOnlyList<PruefMeldung> Meldungen => _meldungen;

        /// <summary>Die Zuordnung des Einzonenwegs, gegen die der Vorschlag gehalten ist; <c>null</c> ohne Gebäude.</summary>
        internal GebaeudeImportSatz Satz { get; private set; }

        /// <summary>Lässt sich der Vorschlag nicht abbilden (eine Meldung der Stufe Fehler)?</summary>
        internal bool Abgelehnt => Zone == null || _meldungen.Any(m => m.Stufe == PruefStufe.Fehler);

        /// <summary>Die Aufbauten je vorläufiger Id — der Eingang von <see cref="GebaeudeZonenabbildung.AlsZonensatz"/>.</summary>
        internal IReadOnlyDictionary<int, BauteilaufbauModel> AufbautenJeId
            => _aufbauten.ToDictionary(a => a.Aufbau.ID, a => a.Aufbau);

        /// <summary>Die Flächensumme der Zeilen eines Summenfelds [m²].</summary>
        internal double Summe(string summenfeld)
            => _zeilen.Where(z => string.Equals(z.Summenfeld, summenfeld, StringComparison.Ordinal)).Sum(z => z.Bauteil.Flaeche);

        /// <summary>Die Fläche der Innenzeilen, beide Seiten [m²] — A_IW der Zone auf dem Weg <see cref="Innenweg.Bauteile"/>, sonst 0.</summary>
        internal double FlaecheInnen => _zeilen.Where(z => z.Summenfeld == null).Sum(z => z.Bauteil.Flaeche);

        /// <summary>Der Weg der inneren Masse (nach Datenlage).</summary>
        internal Innenweg Innenweg { get; private set; } = Innenweg.Vorgabe;

        /// <summary>Die gemessene Innenfläche der Datei, beide Seiten [m²] — auf jedem Weg; 0 ohne Innenflächen.</summary>
        internal double InnenflaecheDateiM2 { get; private set; }

        /// <summary>Zahl der inneren Trennflächen (Flächen mit zwei beheizten Nachbarn) der Datei.</summary>
        internal int Innenflaechen { get; private set; }

        /// <summary>Zahl der inneren Trennflächen ohne Fläche oder ohne vollständige Stoffwerte.</summary>
        internal int InnenflaechenUnvollstaendig { get; private set; }

        /// <summary>
        /// Der Innenflächenfaktor aus der Datei für <c>Tab_Gebaeude.Innenflaechenfaktor</c> —
        /// Innenfläche beider Seiten ÷ Nutzfläche; nur auf dem Weg <see cref="Innenweg.Innenflaechenfaktor"/>,
        /// sonst <c>null</c> (Vorgabe bzw. die Zeilen tragen A_IW). Geschrieben wird er über das Zielfeld
        /// <see cref="GebaeudeZielfelder.INNENFLAECHENFAKTOR"/> des Gebäudes, das dieselbe Messung trägt
        /// (<see cref="Huelleneinordnung.InnenflaecheM2"/>), nicht vom Schreibweg des Vorschlags.
        /// </summary>
        internal double? Innenflaechenfaktor { get; private set; }

        /// <summary>Herkunft des Innenflächenfaktors; <see cref="Importherkunft.Leer"/> ohne Faktor.</summary>
        internal Importherkunft HerkunftInnenflaechenfaktor { get; private set; }

        /// <summary>Lief der Namensabgleich der Baustoffe (ein <see cref="Baustoffabgleich"/> war übergeben)?</summary>
        internal bool AbgleichAktiv { get; private set; }

        /// <summary>
        /// <b>Die Materialnamen der Datei</b> in der Reihenfolge ihres ersten Auftretens — je Name Stufe,
        /// Baustoff oder „ohne Treffer" und die Zahl der Schichten; der Eingang der Anwenderzuordnung.
        /// Gesammelt aus allen Aufbauten, die der Vorschlag betrachtet (Hülle und Trennflächen).
        /// </summary>
        internal IReadOnlyList<GebaeudeMaterialzeile> Materialien => _materialien;

        /// <summary>Die Materialnamen, die einen Baustoff bräuchten und keinen treffen (ohne N6).</summary>
        internal IReadOnlyList<GebaeudeMaterialzeile> OhneTreffer
            => _materialien.Where(m => m.BrauchtAbgleich && m.Treffer != null && m.Treffer.Stufe == Abgleichstufe.Keine).ToList();

        // ==================================================================
        //  Bilden
        // ==================================================================

        /// <summary>Der Vorschlag aus dem gelesenen Ablauf (Abbild, Quelle und Profil des letzten Laufs).</summary>
        internal static GebaeudeBauteilvorschlag Bilden(GebaeudeImportAblauf ablauf, int gebaeudeIndex, char? baualtersklasse,
                                                        IReadOnlyDictionary<string, bool> beheiztUebersteuert = null,
                                                        Baustoffabgleich abgleich = null)
            => Bilden(ablauf?.Abbild, gebaeudeIndex, baualtersklasse, ablauf?.Quelle, ablauf?.Profil, beheiztUebersteuert, abgleich);

        /// <summary>
        /// <b>Bildet den Vorschlag</b> eines Gebäudes der Datei (Regeln: Klassenkopf). Schreibt nichts,
        /// wirft nicht; was sich nicht abbilden lässt, steht als Meldung der Stufe Fehler darin.
        /// </summary>
        /// <param name="abbild">Das gelesene Abbild.</param>
        /// <param name="gebaeudeIndex">Das Gebäude der Datei (U13: eines je Lauf).</param>
        /// <param name="baualtersklasse">Die gewählte Klasse A…M; <c>null</c> = aus dem Baujahr der Datei bzw. keine.</param>
        /// <param name="quelle">Die Quelle des Laufs (Dateiname); <c>null</c> = keine.</param>
        /// <param name="profil">Das Profil des Formats (Meldungspräfix, Vorgabe-Rückfälle).</param>
        /// <param name="beheiztUebersteuert">Die Haken der Raumliste, Raumkennung → beheizt; <c>null</c> = wie gelesen.</param>
        /// <param name="abgleich">Der Namensabgleich der Baustoffe (Katalog, Synonyme, gemerkte Zuordnungen des
        /// Projekts); <c>null</c> = ohne Abgleich — dann gelten allein die Stoffwerte der Datei.</param>
        internal static GebaeudeBauteilvorschlag Bilden(GebaeudeAbbild abbild, int gebaeudeIndex, char? baualtersklasse,
                                                        GebaeudeQuelle quelle, GebaeudeImportProfil profil,
                                                        IReadOnlyDictionary<string, bool> beheiztUebersteuert = null,
                                                        Baustoffabgleich abgleich = null)
        {
            var v = new GebaeudeBauteilvorschlag { AbgleichAktiv = abgleich != null };
            if (abbild == null || profil == null || gebaeudeIndex < 0 || gebaeudeIndex >= abbild.Gebaeude.Count)
            {
                v._meldungen.Add(new PruefMeldung(PruefStufe.Fehler, KEIN_GEBAEUDE,
                    Ganz(gebaeudeIndex), Ganz(abbild?.Gebaeude.Count ?? 0)));
                return v;
            }
            new Bauer(v, abbild, gebaeudeIndex, baualtersklasse, quelle, profil, beheiztUebersteuert, abgleich).Bauen();
            return v;
        }

        // ==================================================================
        //  Der Bauer — der Zustand eines Laufs
        // ==================================================================

        private sealed class Bauer
        {
            private readonly GebaeudeBauteilvorschlag _v;
            private readonly GebaeudeAbbild _abbild;
            private readonly int _index;
            private readonly char? _klasseGewaehlt;
            private readonly GebaeudeQuelle _quelle;
            private readonly GebaeudeImportProfil _profil;
            private readonly IReadOnlyDictionary<string, bool> _uebersteuert;

            private Importherkunft _datei;
            private string _herkunftWert;
            private char? _klasse;
            private AbbildGebaeude _g;
            private Dictionary<string, AbbildRaum> _raeume;
            private Dictionary<string, double?> _geschosslage;

            private int _naechsteZeile = -1;
            private int _naechsterAufbau = -1;
            private readonly Dictionary<string, GebaeudeAufbauzeile> _aufbauJeSignatur = new Dictionary<string, GebaeudeAufbauzeile>(StringComparer.Ordinal);
            private readonly HashSet<string> _aufbaunamen = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _gemeldeteAufbauten = new HashSet<string>(StringComparer.Ordinal);

            private readonly List<string> _ohneFlaeche = new List<string>();
            private readonly List<string> _ohneAzimut = new List<string>();
            private readonly List<string> _ohneU = new List<string>();
            private int _uVorgabe, _uVorgabeFrei, _vorhangfassaden, _fensterErdreich, _unbeheizt;
            private string _quellklasseFrei;
            private double _unbeheiztM2;

            // Namensabgleich der Baustoffe: je Aufbau der Datei EINE Ergänzung; je Name eine Materialzeile.
            private readonly Baustoffabgleich _abgleich;
            private readonly Dictionary<AbbildAufbau, Ergaenzung> _ergaenzt = new Dictionary<AbbildAufbau, Ergaenzung>(ReferenceEqualityComparer.Instance);
            private readonly Dictionary<string, GebaeudeMaterialzeile> _materialJeName = new Dictionary<string, GebaeudeMaterialzeile>(StringComparer.Ordinal);
            private readonly List<string> _ungueltig = new List<string>();
            private readonly List<string> _verworfen = new List<string>();
            private readonly HashSet<string> _gegenprobe = new HashSet<string>(StringComparer.Ordinal);
            private int _luftschichten, _katalogschichten;

            internal Bauer(GebaeudeBauteilvorschlag v, GebaeudeAbbild abbild, int index, char? klasse, GebaeudeQuelle quelle,
                           GebaeudeImportProfil profil, IReadOnlyDictionary<string, bool> uebersteuert, Baustoffabgleich abgleich = null)
            {
                _v = v;
                _abbild = abbild;
                _index = index;
                _klasseGewaehlt = klasse;
                _quelle = quelle;
                _profil = profil;
                _uebersteuert = uebersteuert;
                _abgleich = abgleich;
            }

            private bool IstBeheizt(AbbildRaum r) => GebaeudeRaumzeile.BeheiztWirksam(r, _uebersteuert);

            internal void Bauen()
            {
                _g = _abbild.Gebaeude[_index];
                _datei = string.Equals(_abbild.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)
                    ? Importherkunft.Ifc : Importherkunft.GbXml;
                _herkunftWert = ImportherkunftWerte.Wert(_datei);
                _v.Format = _abbild.Format ?? "";
                _v.Dateiname = _quelle?.Dateiname ?? "";
                _v.Gebaeudekennung = _g.Kennung;
                _v.NordwinkelGrad = _abbild.NordwinkelGrad;
                _v.NordwinkelAngewandt = _datei == Importherkunft.Ifc;

                // Die Zuordnung des Einzonenwegs — Kenngrößen der Zone, Vorgaben, Summenprobe.
                GebaeudeImportSatz satz = GebaeudeAggregation.Bilden(_abbild, _index, _klasseGewaehlt, _quelle, _profil, _uebersteuert);
                _v.Satz = satz;
                _klasse = satz.Baualtersklasse;
                _v.Baualtersklasse = _klasse;

                _raeume = new Dictionary<string, AbbildRaum>(StringComparer.Ordinal);
                foreach (AbbildGebaeude geb in _abbild.Gebaeude)
                    foreach (AbbildRaum r in geb.Raeume)
                        if (!_raeume.ContainsKey(r.Kennung)) _raeume[r.Kennung] = r;
                _geschosslage = new Dictionary<string, double?>(StringComparer.Ordinal);
                foreach (AbbildGebaeude geb in _abbild.Gebaeude)
                    foreach (AbbildGeschoss s in geb.Geschosse)
                        if (!_geschosslage.ContainsKey(s.Kennung)) _geschosslage[s.Kennung] = s.LageM;

                List<AbbildRaum> beheizt = _g.Raeume.Where(IstBeheizt).ToList();
                if (beheizt.Count == 0)
                {
                    Fehler(KEINE_BEHEIZTEN_RAEUME, _g.Anzeigename);
                    return;
                }
                foreach (AbbildRaum r in beheizt)
                    _v._raeume.Add(new GebaeudeQuellzuordnung(r.Quelltyp, r.Kennung, ImportZiel.Zone));

                ZoneBilden(satz);

                if (_v.NordwinkelGrad is double nord && nord != 0.0)
                    Info(_v.NordwinkelAngewandt ? NORDWINKEL_ANGEWANDT : NORDWINKEL_NICHT_ANGEWANDT, Zahl(nord));

                Huelleneinordnung e = GebaeudeHuelleneinordnung.Einordnen(_abbild, _index, IstBeheizt);
                var ohneFlaecheJeFeld = new Dictionary<string, List<Huellposten>>(StringComparer.Ordinal);
                foreach (Huellposten p in e.Huelle.Where(x => !x.Verworfen))
                {
                    if (p.NettoNegativ)
                        _v._meldungen.Add(new PruefMeldung(PruefStufe.Fehler, _profil.Meldung("NETTOFLAECHE_NEGATIV"),
                            p.Bauteil.Kennung, Zahl(p.BruttoM2 ?? 0.0), Zahl(p.AbzugM2)));
                    if (p.NettoM2.HasValue)
                    {
                        if (p.NettoM2.Value > 0.0) Huellzeile(p, p.NettoM2.Value, _datei);
                        else if (!p.NettoNegativ) Info(NETTOFLAECHE_NULL, p.Bauteil.Kennung);
                    }
                    else
                    {
                        if (!ohneFlaecheJeFeld.TryGetValue(p.Summenfeld, out List<Huellposten> liste))
                            ohneFlaecheJeFeld[p.Summenfeld] = liste = new List<Huellposten>();
                        liste.Add(p);
                    }
                    foreach (AbbildBauteil f in p.Fenster) Fensterzeile(p, f);
                    foreach (AbbildBauteil t in p.Tueren) Tuerzeile(p, t);
                }
                InnereMasse(e);

                Rueckfaelle(satz, ohneFlaecheJeFeld);
                foreach (List<Huellposten> rest in ohneFlaecheJeFeld.Values)
                    foreach (Huellposten p in rest) _ohneFlaeche.Add(p.Bauteil.Kennung);

                Sammelmeldungen();
                Summenprobe(satz);
                // Ein opakes Außenbauteil: weder Innenzeile noch Fenster noch Vorhangfassade.
                if (!_v._zeilen.Any(z => z.Summenfeld != null && z.Summenfeld != GebaeudeZielfelder.FENSTER_GESAMT
                                         && z.Bauteil.Bauteilart != DbWerte.BAUTEILART_VORHANGFASSADE))
                    Fehler(KEINE_AUSSENBAUTEILE, _g.Anzeigename);
                if (!_v.Abgelehnt) Probe(satz);
            }

            // ------------------------------------------------------------------
            //  Zone
            // ------------------------------------------------------------------

            private void ZoneBilden(GebaeudeImportSatz satz)
            {
                GebaeudeFeldzeile nf = satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE);
                GebaeudeFeldzeile vol = satz.Zeile(GebaeudeZielfelder.VOLUMEN);
                GebaeudeFeldzeile rh = satz.Zeile(GebaeudeZielfelder.RAUMHOEHE);
                string name = string.IsNullOrWhiteSpace(_g.Anzeigename) ? GebaeudeZonenuebernahme.ZONE_BEZEICHNUNG : _g.Anzeigename.Trim();
                _v.Zone = new ZoneModel
                {
                    ID = -1,
                    Bezeichner = Kuerzen(name, BaustoffSchema.LAENGE_BEZEICHNER),
                    Nutzflaeche = nf.Wert > 0.0 ? nf.Wert : null,
                    Volumen = vol.Wert > 0.0 ? vol.Wert : null,
                    Raumhoehe = rh.Wert > 0.0 ? rh.Wert : null,
                    IstBeheizt = true,
                    Herkunft = _herkunftWert,
                    Quellkennung = string.IsNullOrEmpty(_g.Kennung) ? null : WindowsFormsApplication1.Quellkennung.Kuerzen(_g.Kennung),
                };
                _v.HerkunftNutzflaeche = nf.Wert > 0.0 ? nf.Herkunft : Importherkunft.Leer;
                _v.HerkunftVolumen = vol.Wert > 0.0 ? vol.Herkunft : Importherkunft.Leer;
                _v.HerkunftRaumhoehe = rh.Wert > 0.0 ? rh.Herkunft : Importherkunft.Leer;
            }

            // ------------------------------------------------------------------
            //  Hülle
            // ------------------------------------------------------------------

            /// <summary>Die Zeile eines Hüllbauteils mit der Fläche <paramref name="flaeche"/>.</summary>
            private GebaeudeBauteilzeile Huellzeile(Huellposten p, double flaeche, Importherkunft herkunftFlaeche)
            {
                AbbildBauteil s = p.Bauteil;
                Bauteilart art = s.Art;
                if (art == Bauteilart.Vorhangfassade) return Fassadenzeile(p, flaeche, herkunftFlaeche);
                Bauteilrand rand = RandAus(p.Rand);
                bool gespiegelt = p.HeizPos > 0;
                (double? neigung, Importherkunft hn) = Neigung(s.NeigungGrad, gespiegelt);
                if (!neigung.HasValue && p.Boden.HasValue)
                {
                    neigung = p.Boden.Value ? GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_UNTEN : GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_OBEN;
                    hn = _datei;
                }
                (double? azimut, Importherkunft ha) = Azimut(s.AzimutGrad, gespiegelt);

                GebaeudeBauteilzeile z = NeueZeile(Name(s), art, flaeche, rand, p.Summenfeld, s.Quelltyp, s.Kennung, null);
                z.HerkunftFlaeche = herkunftFlaeche;
                Setzen(z, neigung, hn, azimut, ha);
                Opak(z, s, art, rand, gespiegelt, p.Summenfeld);
                if (rand == Bauteilrand.Unbeheizt) { _unbeheizt++; _unbeheiztM2 += flaeche; }
                Abschliessen(z);
                return z;
            }

            /// <summary>
            /// <b>Eine Vorhangfassade — transparent</b> (Anwenderentscheid): eine Zeile der Art
            /// <c>VORHANGFASSADE</c>, die der Bauteilweg im Fensterzweig mit Sonneneintrag rechnet.
            /// U und g aus der Datei, wo sie welche trägt; sonst U aus der Fenstervorgabe der
            /// Baualtersklasse (Herkunft <c>VORGABE</c>) und g leer — dann gilt der Wert des Gebäudes.
            /// Rahmenanteil und Verschattungsfaktor bleiben leer: Es gelten die des Gebäudes, ohne sie
            /// die Vorgaben (<see cref="BauteilEingang.MitGebaeudewerten"/>). Die Fläche zählt weiter in
            /// das Summenfeld der Einordnung („Sonstige Flächen"), wie im Einzonenweg. An Erdreich
            /// rechnet sie wie ein Fenster an Außenluft; an Außenluft braucht sie einen Azimut.
            /// </summary>
            private GebaeudeBauteilzeile Fassadenzeile(Huellposten p, double flaeche, Importherkunft herkunftFlaeche)
            {
                AbbildBauteil s = p.Bauteil;
                _vorhangfassaden++;
                Bauteilrand rand = RandAus(p.Rand);
                if (rand == Bauteilrand.Erdreich)
                {
                    rand = Bauteilrand.Aussenluft;
                    _fensterErdreich++;
                }
                bool gespiegelt = p.HeizPos > 0;
                (double? neigung, Importherkunft hn) = Neigung(s.NeigungGrad, gespiegelt);
                (double? azimut, Importherkunft ha) = Azimut(s.AzimutGrad, gespiegelt);

                GebaeudeBauteilzeile z = NeueZeile(Name(s), Bauteilart.Vorhangfassade, flaeche, rand, p.Summenfeld,
                                                   s.Quelltyp, s.Kennung, null);
                z.HerkunftFlaeche = herkunftFlaeche;
                Setzen(z, neigung, hn, azimut, ha);

                z.UDatei = s.UWertWm2K;
                if (s.UWertWm2K.HasValue) { z.Bauteil.U_Wert = s.UWertWm2K; z.HerkunftU = _datei; }
                else UVorgabe(z, GebaeudeZielfelder.U_FENSTER);
                // g: ≤ 0 oder > 1 ist eine Fehlstelle (Datenaustauschkonzept 3.6) — dann gilt der Wert des Gebäudes.
                if (s.GWert > 0.0 && s.GWert <= 1.0) { z.Bauteil.g_Wert = s.GWert; z.HerkunftG = _datei; }
                if (rand == Bauteilrand.Unbeheizt) { _unbeheizt++; _unbeheiztM2 += flaeche; }
                Abschliessen(z);
                return z;
            }

            private void Fensterzeile(Huellposten p, AbbildBauteil f)
            {
                if (!(f.BruttoflaecheM2 > 0.0)) { _ohneFlaeche.Add(f.Kennung); return; }
                Bauteilrand rand = RandAus(p.Rand);
                if (rand == Bauteilrand.Erdreich)
                {
                    rand = Bauteilrand.Aussenluft;
                    _fensterErdreich++;
                }
                bool gespiegelt = p.HeizPos > 0;
                (double? neigung, Importherkunft hn) = Neigung(f.NeigungGrad ?? p.Bauteil.NeigungGrad, gespiegelt);
                (double? azimut, Importherkunft ha) = Azimut(p.Bauteil.AzimutGrad ?? f.AzimutGrad, gespiegelt);

                GebaeudeBauteilzeile z = NeueZeile(Name(f), Bauteilart.Fenster, f.BruttoflaecheM2.Value, rand,
                                                   GebaeudeZielfelder.FENSTER_GESAMT, f.Quelltyp, f.Kennung, null);
                z.HerkunftFlaeche = _datei;
                Setzen(z, neigung, hn, azimut, ha);

                z.UDatei = f.UWertWm2K;
                if (f.UWertWm2K.HasValue) { z.Bauteil.U_Wert = f.UWertWm2K; z.HerkunftU = _datei; }
                else UVorgabe(z, GebaeudeZielfelder.U_FENSTER);
                // g: der Wert der Öffnung; ≤ 0 oder > 1 ist eine Fehlstelle (Datenaustauschkonzept 3.6) —
                // dann bleibt er leer und es gilt der Wert des Gebäudes.
                if (f.GWert > 0.0 && f.GWert <= 1.0) { z.Bauteil.g_Wert = f.GWert; z.HerkunftG = _datei; }
                Abschliessen(z);
            }

            private void Tuerzeile(Huellposten p, AbbildBauteil t)
            {
                if (!(t.BruttoflaecheM2 > 0.0)) { _ohneFlaeche.Add(t.Kennung); return; }
                Bauteilrand rand = RandAus(p.Rand);
                bool gespiegelt = p.HeizPos > 0;
                (double? neigung, Importherkunft hn) = Neigung(t.NeigungGrad ?? p.Bauteil.NeigungGrad, gespiegelt);
                (double? azimut, Importherkunft ha) = Azimut(p.Bauteil.AzimutGrad ?? t.AzimutGrad, gespiegelt);

                GebaeudeBauteilzeile z = NeueZeile(Name(t), Bauteilart.Tuer, t.BruttoflaecheM2.Value, rand,
                                                   GebaeudeZielfelder.FLAECHE_SONSTIGE, t.Quelltyp, t.Kennung, null);
                z.HerkunftFlaeche = _datei;
                Setzen(z, neigung, hn, azimut, ha);
                Opak(z, t, Bauteilart.Tuer, rand, gespiegelt, GebaeudeZielfelder.FLAECHE_SONSTIGE);
                Abschliessen(z);
            }

            /// <summary>
            /// U-Wert und Aufbau eines opaken Bauteils (Klassenkopf, Punkt „Aufbau und U-Wert").
            /// </summary>
            private void Opak(GebaeudeBauteilzeile z, AbbildBauteil s, Bauteilart art, Bauteilrand rand, bool gespiegelt, string summenfeld)
            {
                z.UDatei = s.UWertWm2K;
                GebaeudeAufbauzeile aufbau = AufbauFuer(s.Aufbau, art, gespiegelt);
                if (aufbau != null)
                {
                    z.Bauteil.ID_Aufbau = aufbau.Aufbau.ID;
                    z.HerkunftAufbau = aufbau.Herkunft;
                    z.HerkunftU = aufbau.Herkunft;
                    z.USchichten = UAusAufbau(aufbau.Aufbau, z.Bauteil.Neigung ?? BauteilEingang.VorgabeNeigung(art), rand);
                    if (z.UDatei.HasValue && z.USchichten > 0.0
                        && Math.Abs(z.UDatei.Value / z.USchichten.Value - 1.0) > U_ABWEICHUNG_GRENZE
                        && _gemeldeteAufbauten.Add("U|" + aufbau.Kennung))
                        Warnung(U_ABWEICHUNG, aufbau.Kennung, Zahl(z.UDatei.Value), Zahl(Math.Round(z.USchichten.Value, 4)),
                                Zahl(Math.Round(100.0 * (z.UDatei.Value / z.USchichten.Value - 1.0), 1)));
                    return;
                }

                if (s.UWertWm2K.HasValue)
                {
                    z.Bauteil.U_Wert = s.UWertWm2K;
                    z.HerkunftU = _datei;
                    return;
                }
                if (s.Aufbau != null && s.Aufbau.Status == Aufbaustatus.Masselos
                    && SchichtwerteNaht.UWertAusSchichten(s.Aufbau, z.Bauteil.Neigung ?? BauteilEingang.VorgabeNeigung(art),
                                                          RandAbbild(rand), out _) is double um)
                {
                    z.Bauteil.U_Wert = um;
                    z.HerkunftU = _datei;
                    return;
                }
                UVorgabe(z, UFeld(summenfeld));
            }

            /// <summary>
            /// Der U-Wert der Baualtersklasse: Median des Katalogs, ohne Katalogsatz der freie Wert nach
            /// Stein/Loga (E51, Herkunft <see cref="Importherkunft.VorgabeFrei"/>, gespeichert als VORGABE).
            /// </summary>
            private void UVorgabe(GebaeudeBauteilzeile z, string feldU)
            {
                Baualtersvorgabe v = GebaeudeVorgaben.Fuer(_klasse, null);
                double? u = GebaeudeVorgaben.Wert(_klasse, feldU);
                if (u.HasValue)
                {
                    z.Bauteil.U_Wert = u;
                    z.HerkunftU = GebaeudeVorgaben.Herkunft(v);
                    if (v.Frei)
                    {
                        _uVorgabeFrei++;
                        _quellklasseFrei = v.Quellklasse;
                    }
                    else _uVorgabe++;
                }
                else _ohneU.Add(z.Kennung ?? z.Bauteil.Bezeichner);
            }

            // ------------------------------------------------------------------
            //  Innere Masse
            // ------------------------------------------------------------------

            /// <summary>
            /// <b>Die innere Masse nach Datenlage</b> (Klassenkopf, Punkt „Innere Masse"): die Innenfläche
            /// beider Seiten messen, die Vollständigkeit prüfen (Fläche und vollständiger Aufbau je
            /// Trennfläche, Innenfläche im Band) und danach entweder die Zeilen beider Seiten bilden oder
            /// den Innenflächenfaktor aus der Datei setzen — mit einer Meldung, die Weg und Grund nennt.
            /// </summary>
            private void InnereMasse(Huelleneinordnung einordnung)
            {
                // Eine Trennfläche mit Nettofläche 0 (ganz Öffnung) ist keine Fläche innerer Masse; die
                // Messung ist die der Einordnung — dieselbe, aus der die Zuordnung ihr Zielfeld bildet.
                IReadOnlyList<Innenposten> flaechen = einordnung.Innenflaechen;
                double innenflaeche = einordnung.InnenflaecheM2;
                int unvollstaendig = flaechen.Count(p => !p.NettoM2.HasValue || Schichtfolge(p.Bauteil.Aufbau, false, out _) == null);
                _v.Innenflaechen = flaechen.Count;
                _v.InnenflaecheDateiM2 = innenflaeche;
                _v.InnenflaechenUnvollstaendig = unvollstaendig;

                double vorgabe = GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR;
                if (flaechen.Count == 0 || !(innenflaeche > 0.0))
                {
                    _v.Innenweg = Innenweg.Vorgabe;
                    Info(INNEN_VORGABE, Zahl(vorgabe));
                    return;
                }
                if (!(_v.Zone.Nutzflaeche > 0.0))
                {
                    _v.Innenweg = Innenweg.Vorgabe;
                    Info(INNEN_OHNE_NUTZFLAECHE, Zahl(innenflaeche), Zahl(vorgabe));
                    return;
                }

                double faktor = innenflaeche / _v.Zone.Nutzflaeche.Value;
                bool plausibel = faktor >= INNENFLAECHE_FAKTOR_MIN && faktor <= INNENFLAECHE_FAKTOR_MAX;
                if (unvollstaendig == 0 && plausibel)
                {
                    _v.Innenweg = Innenweg.Bauteile;
                    int trenn = 0;
                    foreach (Innenposten p in flaechen)
                    {
                        Bauteilart art = GebaeudeHuelleneinordnung.IstWaagerechteArt(p.Bauteil.Art) ? Bauteilart.Decke : Bauteilart.Innenwand;
                        if (p.PosB < 0) trenn++;
                        Innenzeile(p, art, p.PosA, p.PosB, p.PosB >= 0 ? "A" : null);
                        if (p.PosB >= 0) Innenzeile(p, art, p.PosB, p.PosA, "B");
                    }
                    Info(INNEN_BAUTEILE, Ganz(flaechen.Count), Ganz(_v._zeilen.Count(z => z.Summenfeld == null)),
                         Zahl(innenflaeche), Zahl(Math.Round(faktor, 4)));
                    if (trenn > 0) Info(GEBAEUDETRENNFLAECHE, Ganz(trenn));
                    return;
                }

                // Sonst: keine Innenzeilen — der Innenflächenfaktor aus der Datei, die Masse aus der Bauweise.
                _v.Innenweg = Innenweg.Innenflaechenfaktor;
                _v.Innenflaechenfaktor = faktor;
                _v.HerkunftInnenflaechenfaktor = _datei;
                if (unvollstaendig > 0)
                    Info(INNEN_FAKTOR_STOFFWERTE, Zahl(Math.Round(faktor, 4)), Zahl(innenflaeche), Ganz(unvollstaendig), Ganz(flaechen.Count));
                if (!plausibel)
                    Warnung(INNEN_FAKTOR_UNPLAUSIBEL, Zahl(Math.Round(faktor, 4)), Zahl(innenflaeche),
                            Zahl(INNENFLAECHE_FAKTOR_MIN), Zahl(INNENFLAECHE_FAKTOR_MAX));
            }

            private void Innenzeile(Innenposten p, Bauteilart art, int pos, int gegenPos, string seite)
            {
                AbbildBauteil s = p.Bauteil;
                bool gespiegelt = pos > 0;
                (double? neigung, Importherkunft hn) = Neigung(s.NeigungGrad, gespiegelt);
                if (!neigung.HasValue && art == Bauteilart.Decke)
                {
                    neigung = DeckenneigungAusSicht(s, pos, gegenPos);
                    if (neigung.HasValue) hn = _datei;
                }
                string name = Name(s) + (seite == null ? "" : " (Seite " + seite + ")");
                GebaeudeBauteilzeile z = NeueZeile(name, art, p.NettoM2.Value, Bauteilrand.Innen, null, s.Quelltyp, s.Kennung, seite);
                z.HerkunftFlaeche = _datei;
                Setzen(z, neigung, hn, null, Importherkunft.Leer);
                z.UDatei = s.UWertWm2K;
                // Ein Innenbauteil braucht keinen U-Wert: mit Aufbau die Masse aus den Schichten, ohne
                // Aufbau nur die Fläche (N1.46, 6).
                GebaeudeAufbauzeile aufbau = AufbauFuer(s.Aufbau, art, gespiegelt);
                if (aufbau != null)
                {
                    z.Bauteil.ID_Aufbau = aufbau.Aufbau.ID;
                    z.HerkunftAufbau = aufbau.Herkunft;
                    z.USchichten = UAusAufbau(aufbau.Aufbau, neigung ?? BauteilEingang.VorgabeNeigung(art), Bauteilrand.Innen);
                    z.HerkunftU = aufbau.Herkunft;
                }
                Abschliessen(z);
            }

            /// <summary>
            /// Die Neigung einer inneren Decke aus Sicht eines ihrer Räume, wenn die Datei keine trägt:
            /// die Sicht des Raums (Boden → 180°, Decke → 0°), sonst die Geschosslage der beiden Räume.
            /// </summary>
            private double? DeckenneigungAusSicht(AbbildBauteil s, int pos, int gegenPos)
            {
                bool? boden = GebaeudeAggregation.SichtIstBoden(s.Nachbarn[pos].Sicht);
                if (!boden.HasValue && gegenPos >= 0)
                {
                    bool? gegen = GebaeudeAggregation.SichtIstBoden(s.Nachbarn[gegenPos].Sicht);
                    if (gegen.HasValue) boden = !gegen.Value;
                }
                if (!boden.HasValue && gegenPos >= 0
                    && Lage(s.Nachbarn[pos].Kennung) is double eigen && Lage(s.Nachbarn[gegenPos].Kennung) is double andere && eigen != andere)
                    boden = eigen > andere;
                if (!boden.HasValue) return null;
                return boden.Value ? GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_UNTEN : GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_OBEN;
            }

            private double? Lage(string raum)
                => _raeume.TryGetValue(raum, out AbbildRaum r) && r.GeschossKennung != null
                   && _geschosslage.TryGetValue(r.GeschossKennung, out double? l) ? l : null;

            // ------------------------------------------------------------------
            //  Vorgabe-Rückfälle der Flächen (IFC, Umsetzungskonzept 3.4)
            // ------------------------------------------------------------------

            private void Rueckfaelle(GebaeudeImportSatz satz, Dictionary<string, List<Huellposten>> ohneFlaeche)
            {
                foreach (string feld in new[] { GebaeudeZielfelder.FLAECHE_DACH, GebaeudeZielfelder.FLAECHE_GRUND })
                {
                    GebaeudeFeldzeile zf = satz.Zeile(feld);
                    if (zf == null || zf.Herkunft != Importherkunft.Vorgabe || !(zf.Wert > 0.0)) continue;
                    if (_v._zeilen.Any(z => z.Summenfeld == feld)) continue;   // die Gruppe hat gelesene Flächen

                    double flaeche = zf.Wert.Value;
                    Info(FLAECHE_VORGABE, feld, Zahl(flaeche));
                    if (ohneFlaeche.TryGetValue(feld, out List<Huellposten> liste) && liste.Count == 1)
                    {
                        Huellzeile(liste[0], flaeche, Importherkunft.Vorgabe);
                        ohneFlaeche.Remove(feld);
                        continue;
                    }

                    bool dach = feld == GebaeudeZielfelder.FLAECHE_DACH;
                    Bauteilrand rand = dach ? Bauteilrand.Aussenluft : GrundrandAusSatz(satz);
                    GebaeudeBauteilzeile z = NeueZeile(dach ? VORGABEZEILE_DACH : VORGABEZEILE_GRUND,
                                                       dach ? Bauteilart.Dach : Bauteilart.Bodenplatte, flaeche, rand, feld, null, null, null);
                    z.HerkunftFlaeche = Importherkunft.Vorgabe;
                    Setzen(z, dach ? GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_OBEN : GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_UNTEN,
                           Importherkunft.Vorgabe, null, Importherkunft.Leer);
                    UVorgabe(z, UFeld(feld));
                    if (rand == Bauteilrand.Unbeheizt) { _unbeheizt++; _unbeheiztM2 += flaeche; }
                    Abschliessen(z);
                }
            }

            private static Bauteilrand GrundrandAusSatz(GebaeudeImportSatz satz)
            {
                switch (satz.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG)?.Textwert)
                {
                    case DbWerte.GRUND_KELLER: return Bauteilrand.Unbeheizt;
                    case DbWerte.GRUND_AUSSENLUFT: return Bauteilrand.Aussenluft;
                    default: return Bauteilrand.Erdreich;
                }
            }

            // ------------------------------------------------------------------
            //  Aufbauten
            // ------------------------------------------------------------------

            /// <summary>Eine Schicht mit ihrem Katalogbaustoff (<c>null</c> = Werte der Datei) und ihrer Quelle.</summary>
            private readonly struct Schichteintrag
            {
                internal Schichteintrag(Schicht schicht, int? stamm, AbbildSchicht quelle)
                {
                    Schicht = schicht;
                    Stamm = stamm;
                    Quelle = quelle;
                }

                internal Schicht Schicht { get; }

                internal int? Stamm { get; }

                internal AbbildSchicht Quelle { get; }
            }

            /// <summary>Die Ergänzung eines Aufbaus der Datei über den Namensabgleich — einmal je Aufbau, in der Folge der Datei.</summary>
            private sealed class Ergaenzung
            {
                internal readonly List<Schichteintrag> Schichten = new List<Schichteintrag>();
                internal bool Vollstaendig;
                internal bool Katalog;
                internal string Grund;
            }

            /// <summary>
            /// Der Aufbau eines Bauteils aus Sicht seines Raums — je Konstruktion und Schichtfolge einer;
            /// <c>null</c>, wenn die Stoffwerte nicht vollständig oder außerhalb des Bandes sind (einmal je
            /// Konstruktion gemeldet) oder die Art keinen Aufbau trägt. Mit dem Namensabgleich trägt er die
            /// ergänzten Schichten (<see cref="Ergaenzen"/>) und die Herkunft <c>KATALOG</c>, sobald ein Wert
            /// aus dem Katalog stammt.
            /// </summary>
            private GebaeudeAufbauzeile AufbauFuer(AbbildAufbau a, Bauteilart art, bool gespiegelt)
            {
                if (a == null || a.Schichten.Count == 0) return null;
                List<Schichteintrag> schichten = Schichtfolge(a, gespiegelt, out string grund);
                if (schichten == null)
                {
                    if (_gemeldeteAufbauten.Add("S|" + a.Kennung))
                        Warnung(STOFFWERTE_UNVOLLSTAENDIG, a.Kennung, grund);
                    return null;
                }
                bool katalog = _abgleich != null && Ergaenzen(a).Katalog;

                string signatur = a.Kennung + "\u0001" + string.Join("\u0002", schichten.Select(x =>
                    x.Schicht.Dicke_M.ToString("R", CultureInfo.InvariantCulture) + ";" + x.Schicht.Lambda_WmK.ToString("R", CultureInfo.InvariantCulture) + ";" +
                    x.Schicht.Rohdichte_KgM3.ToString("R", CultureInfo.InvariantCulture) + ";" + x.Schicht.Cp_JkgK.ToString("R", CultureInfo.InvariantCulture) + ";" +
                    (x.Schicht.IstLuftschicht ? "L" : "") + ";" + (x.Stamm?.ToString(CultureInfo.InvariantCulture) ?? "")));
                string artWert = GebaeudeZonenabbildung.ArtFuerZeile(art);
                if (_aufbauJeSignatur.TryGetValue(signatur, out GebaeudeAufbauzeile vorhanden))
                {
                    if (vorhanden.Aufbau.Bauteilart != null && vorhanden.Aufbau.Bauteilart != artWert)
                        vorhanden.Aufbau.Bauteilart = null;   // für mehrere Arten: „für jede"
                    return vorhanden;
                }

                bool gegenseite = _v._aufbauten.Any(x => string.Equals(x.Kennung, a.Kennung, StringComparison.Ordinal));
                string stamm = string.IsNullOrWhiteSpace(a.Name) ? a.Kennung : a.Name.Trim();
                string name = FreierName(gegenseite ? stamm + " (Gegenseite)" : stamm);
                var modell = new BauteilaufbauModel
                {
                    ID = _naechsterAufbau--,
                    Bezeichner = name,
                    Bauteilart = artWert,
                    Quelle = string.IsNullOrEmpty(_v.Dateiname) ? null : Kuerzen(_v.Dateiname, BaustoffSchema.LAENGE_QUELLE),
                    Herkunft = katalog ? DbWerte.HERKUNFT_KATALOG : _herkunftWert,
                    Quellkennung = WindowsFormsApplication1.Quellkennung.Kuerzen(a.Kennung),
                };
                foreach (Schichteintrag x in schichten)
                    modell.Schichten.Add(new BauteilschichtModel
                    {
                        Reihenfolge = modell.Schichten.Count + 1,
                        Dicke = x.Schicht.Dicke_M,
                        Lambda = Wert(x.Schicht.Lambda_WmK),
                        Rho = Wert(x.Schicht.Rohdichte_KgM3),
                        Cp = Wert(x.Schicht.Cp_JkgK),
                        IstLuftschicht = x.Schicht.IstLuftschicht,
                    });
                string quelltypStoff = _datei == Importherkunft.Ifc ? QUELLTYP_IFC_BAUSTOFF : QUELLTYP_GBXML_BAUSTOFF;
                List<GebaeudeBaustoffquelle> quellen = schichten
                    .Where(x => x.Stamm.HasValue && !string.IsNullOrWhiteSpace(x.Quelle?.BaustoffKennung))
                    .Select(x => new GebaeudeBaustoffquelle(quelltypStoff, x.Quelle.BaustoffKennung, x.Stamm.Value))
                    .Distinct().ToList();
                var zeile = new GebaeudeAufbauzeile(modell, _datei == Importherkunft.Ifc ? "IfcMaterialLayerSet" : "Construction",
                                                    a.Kennung, gegenseite, a.RichtungAngenommen,
                                                    katalog ? Importherkunft.Katalog : _datei,
                                                    schichten.Select(x => x.Stamm).ToArray(), quellen);
                _aufbauJeSignatur[signatur] = zeile;
                _v._aufbauten.Add(zeile);
                return zeile;
            }

            /// <summary>
            /// Die Schichten eines Aufbaus innen → außen aus Sicht eines Raums — <c>null</c>, wenn der
            /// Aufbau fehlt, keine Schicht hat, nicht vollständig ist (Grund: der Aufbaustatus bzw. mit dem
            /// Abgleich die Namen der Schichten, die ohne Werte bleiben) oder eine Schicht außerhalb des
            /// Stoffwertbands liegt (Grund: die Meldung von <see cref="Bauteilreduktion.Pruefen"/>). Die Folge
            /// des Abbilds gilt aus Sicht des ERSTEN Nachbarn; die andere Seite (<paramref name="gespiegelt"/>)
            /// liest sie rückwärts. Meldet nichts.
            /// </summary>
            private List<Schichteintrag> Schichtfolge(AbbildAufbau a, bool gespiegelt, out string grund)
            {
                grund = null;
                if (a == null || a.Schichten.Count == 0)
                {
                    grund = Aufbaustatus.OhneAufbau.ToString();
                    return null;
                }
                List<Schichteintrag> folge;
                if (_abgleich == null)
                {
                    if (a.Status != Aufbaustatus.Vollstaendig)
                    {
                        grund = a.Status.ToString();
                        return null;
                    }
                    folge = a.Schichten.Select(x => new Schichteintrag(
                        new Schicht(x.DickeM.Value, x.LambdaWmK.Value, x.RhoKgM3.Value, x.CpJkgK.Value), null, x)).ToList();
                }
                else
                {
                    Ergaenzung e = Ergaenzen(a);
                    if (!e.Vollstaendig)
                    {
                        grund = e.Grund;
                        return null;
                    }
                    folge = e.Schichten.ToList();
                }
                if (a.Richtung != Schichtrichtung.InnenNachAussen) folge.Reverse();
                if (gespiegelt) folge.Reverse();
                try
                {
                    Bauteilreduktion.Pruefen(folge.Select(x => x.Schicht).ToList(), a.Kennung);
                }
                catch (GebaeudeModellException ex)
                {
                    grund = ex.Message;
                    return null;
                }
                return folge;
            }

            /// <summary>
            /// <b>Die Ergänzung eines Aufbaus über den Namensabgleich</b> (Klassenkopf, Punkt
            /// „Namensabgleich") — einmal je Aufbau der Datei, in ihrer Schichtfolge. Je Schicht: Stoffwerte
            /// außerhalb des Bandes gelten als nicht geliefert; fehlt λ und trägt die Datei einen R-Wert, gilt
            /// d/R. Sind Dicke, λ, ρ und c dann da, rechnet die Schicht mit den Werten der Datei, und der
            /// Abgleich ist nur die Gegenprobe. Sonst entscheidet der Materialname: eine Schraffur fällt
            /// weg, eine Luftschicht wird ruhende Luftschicht, ein Treffer füllt die fehlenden Werte aus dem
            /// Baustoff (ein Wert der Datei im Band behält Vorrang). Vollständig ist der Aufbau, wenn jede
            /// verbliebene Schicht es ist.
            /// </summary>
            private Ergaenzung Ergaenzen(AbbildAufbau a)
            {
                if (_ergaenzt.TryGetValue(a, out Ergaenzung bekannt)) return bekannt;
                var e = new Ergaenzung { Vollstaendig = true };
                var fehlend = new List<string>();
                var ausKatalog = new List<GebaeudeMaterialzeile>();
                int luft = 0;
                foreach (AbbildSchicht s in a.Schichten)
                {
                    GebaeudeMaterialzeile m = Material(s);
                    if (m != null) m.Schichten++;
                    double? d = s.DickeM > 0.0 ? s.DickeM : null;
                    double? l = s.LambdaWmK, r = s.RhoKgM3, c = s.CpJkgK;
                    bool ungueltig = false;
                    if (Baustoffabgleich.AusserhalbDesBands(l, Baustoffabgleich.LambdaImBand)) { l = null; ungueltig = true; }
                    if (Baustoffabgleich.AusserhalbDesBands(r, Baustoffabgleich.RhoImBand)) { r = null; ungueltig = true; }
                    if (Baustoffabgleich.AusserhalbDesBands(c, Baustoffabgleich.CpImBand)) { c = null; ungueltig = true; }
                    if (ungueltig) Merken(_ungueltig, Stoffname(s));
                    if (!l.HasValue && s.RWertM2KW > 0.0 && d.HasValue && Baustoffabgleich.LambdaImBand(d.Value / s.RWertM2KW.Value))
                        l = d.Value / s.RWertM2KW.Value;

                    Abgleichtreffer t = _abgleich.Abgleichen(s.Name);
                    if (m != null) m.Treffer = t;

                    if (d.HasValue && l.HasValue && r.HasValue && c.HasValue)
                    {
                        // Vollständige Werte der Datei: sie rechnen, der Abgleich ist die Gegenprobe.
                        if (m != null) m.SchichtenAusDatei++;
                        Gegenprobe(s, l.Value, t);
                        e.Schichten.Add(new Schichteintrag(new Schicht(d.Value, l.Value, r.Value, c.Value), null, s));
                        continue;
                    }

                    if (t.Sonderfall == Abgleichsonderfall.Verwerfen)
                    {
                        Merken(_verworfen, Stoffname(s));
                        continue;
                    }
                    if (t.Sonderfall == Abgleichsonderfall.Luftschicht)
                    {
                        if (!d.HasValue)
                        {
                            e.Vollstaendig = false;
                            fehlend.Add(Stoffname(s));
                            continue;
                        }
                        luft++;
                        e.Schichten.Add(new Schichteintrag(l.HasValue
                            ? new Schicht(d.Value, l.Value, r ?? double.NaN, c ?? double.NaN, true)
                            : Schicht.RuhendeLuft(d.Value), null, s));
                        continue;
                    }

                    BaustoffModel b = t.Baustoff;
                    double? lw = l ?? (Baustoffabgleich.LambdaImBand(b?.Lambda) ? b.Lambda : null);
                    double? rw = r ?? (Baustoffabgleich.RhoImBand(b?.Rho) ? b.Rho : null);
                    double? cw = c ?? (Baustoffabgleich.CpImBand(b?.Cp) ? b.Cp : null);
                    if (b == null || !d.HasValue || !lw.HasValue || !rw.HasValue || !cw.HasValue)
                    {
                        e.Vollstaendig = false;
                        fehlend.Add(Stoffname(s));
                        continue;
                    }
                    e.Katalog = true;
                    if (m != null) ausKatalog.Add(m);
                    e.Schichten.Add(new Schichteintrag(new Schicht(d.Value, lw.Value, rw.Value, cw.Value), b.ID, s));
                }
                if (e.Schichten.Count == 0) e.Vollstaendig = false;
                if (e.Vollstaendig)
                {
                    _katalogschichten += e.Schichten.Count(x => x.Stamm.HasValue);
                    _luftschichten += luft;
                    foreach (GebaeudeMaterialzeile m in ausKatalog) m.SchichtenAusKatalog++;
                }
                else
                {
                    e.Katalog = false;
                    e.Grund = fehlend.Count > 0 ? string.Join(", ", fehlend.Distinct(StringComparer.Ordinal)) : a.Status.ToString();
                }
                _ergaenzt[a] = e;
                return e;
            }

            /// <summary>
            /// <b>Die Gegenprobe</b> (Datenaustauschkonzept 3.6): λ der Datei gegen λ des getroffenen
            /// Katalogbaustoffs; über <see cref="LAMBDA_GEGENPROBE_GRENZE"/> einmal je Name gemeldet. Es rechnen
            /// die Werte der Datei.
            /// </summary>
            private void Gegenprobe(AbbildSchicht s, double lambda, Abgleichtreffer t)
            {
                if (t == null || !t.Getroffen || !(t.Baustoff.Lambda > 0.0)) return;
                double abweichung = lambda / t.Baustoff.Lambda.Value - 1.0;
                if (Math.Abs(abweichung) > LAMBDA_GEGENPROBE_GRENZE && _gegenprobe.Add(Stoffname(s)))
                    Warnung(LAMBDA_GEGENPROBE, Stoffname(s), Zahl(lambda), t.Baustoff.Bezeichner, Zahl(t.Baustoff.Lambda.Value),
                            Zahl(Math.Round(100.0 * abweichung, 1)));
            }

            /// <summary>Die Materialzeile eines Namens — beim ersten Auftreten angelegt; <c>null</c> für eine Schicht ohne Namen.</summary>
            private GebaeudeMaterialzeile Material(AbbildSchicht s)
            {
                string name = s?.Name?.Trim();
                if (string.IsNullOrEmpty(name)) return null;
                if (_materialJeName.TryGetValue(name, out GebaeudeMaterialzeile m)) return m;
                m = new GebaeudeMaterialzeile(name, _datei == Importherkunft.Ifc ? QUELLTYP_IFC_BAUSTOFF : QUELLTYP_GBXML_BAUSTOFF,
                                              s.BaustoffKennung);
                _materialJeName[name] = m;
                _v._materialien.Add(m);
                return m;
            }

            private static string Stoffname(AbbildSchicht s)
                => string.IsNullOrWhiteSpace(s?.Name) ? s?.BaustoffKennung ?? "" : s.Name.Trim();

            private static void Merken(List<string> liste, string name)
            {
                if (!liste.Contains(name, StringComparer.Ordinal)) liste.Add(name);
            }

            private static double? Wert(double w) => double.IsNaN(w) ? (double?)null : w;

            /// <summary>Ein Aufbauname, der im Vorschlag noch frei ist — höchstens 80 Zeichen.</summary>
            private string FreierName(string name)
            {
                string basis = Kuerzen(name, BaustoffSchema.LAENGE_BEZEICHNER);
                string kandidat = basis;
                for (int n = 2; !_aufbaunamen.Add(kandidat); n++)
                {
                    string zusatz = " (" + n.ToString(CultureInfo.InvariantCulture) + ")";
                    kandidat = Kuerzen(name, BaustoffSchema.LAENGE_BEZEICHNER - zusatz.Length) + zusatz;
                }
                return kandidat;
            }

            /// <summary>
            /// Der U-Wert eines Aufbaus aus seinen Schichten über <see cref="Bauteilreduktion"/> — die Schicht
            /// über <see cref="GebaeudeZonenabbildung.AlsSchicht"/> wie im Lauf (auch die ruhende Luftschicht);
            /// <c>null</c> bei einem Fehler.
            /// </summary>
            private static double? UAusAufbau(BauteilaufbauModel a, double neigung, Bauteilrand rand)
            {
                try
                {
                    var schichten = a.Schichten.Select((x, i) => GebaeudeZonenabbildung.AlsSchicht(x, i + 1, a.Bezeichner)).ToList();
                    return Bauteilreduktion.UWertAusSchichten(schichten, neigung, rand, a.Bezeichner).U_WM2K;
                }
                catch (GebaeudeModellException)
                {
                    return null;
                }
            }

            // ------------------------------------------------------------------
            //  Zeilen
            // ------------------------------------------------------------------

            private GebaeudeBauteilzeile NeueZeile(string name, Bauteilart art, double flaeche, Bauteilrand rand, string summenfeld,
                                                  string quelltyp, string kennung, string seite)
            {
                var b = new BauteilModel
                {
                    ID = _naechsteZeile--,
                    Bezeichner = Kuerzen(name, BaustoffSchema.LAENGE_BEZEICHNER),
                    Bauteilart = GebaeudeZonenabbildung.ArtFuerZeile(art),
                    Flaeche = flaeche,
                    Randbedingung = RandWert(art, rand),
                    Quellkennung = string.IsNullOrEmpty(kennung) ? null : WindowsFormsApplication1.Quellkennung.Kuerzen(kennung),
                };
                return new GebaeudeBauteilzeile(b, summenfeld, string.IsNullOrEmpty(kennung) ? null : quelltyp,
                                                string.IsNullOrEmpty(kennung) ? null : kennung, seite);
            }

            private static void Setzen(GebaeudeBauteilzeile z, double? neigung, Importherkunft hn, double? azimut, Importherkunft ha)
            {
                z.Bauteil.Neigung = neigung;
                z.HerkunftNeigung = neigung.HasValue ? hn : Importherkunft.Leer;
                z.Bauteil.Azimut = azimut;
                z.HerkunftAzimut = azimut.HasValue ? ha : Importherkunft.Leer;
            }

            /// <summary>Herkunft der Zeile setzen, Azimutpflicht prüfen, die Zeile an Zone und Vorschlag hängen.</summary>
            private void Abschliessen(GebaeudeBauteilzeile z)
            {
                BauteilModel b = z.Bauteil;
                b.Herkunft = ImportherkunftWerte.IstVorgabe(z.HerkunftFlaeche) || ImportherkunftWerte.IstVorgabe(z.HerkunftU)
                    ? DbWerte.HERKUNFT_VORGABE : _herkunftWert;
                if (!b.Azimut.HasValue && GebaeudeZonenCtrl.BrauchtAzimut(b))
                    _ohneAzimut.Add(z.Kennung ?? b.Bezeichner);
                _v._zeilen.Add(z);
                _v.Zone.Bauteile.Add(b);
            }

            /// <summary>Neigung aus Sicht des beheizten Raums: gespiegelt ist sie 180° − Neigung.</summary>
            private (double?, Importherkunft) Neigung(double? neigung, bool gespiegelt)
            {
                if (!(neigung is double t) || double.IsNaN(t) || double.IsInfinity(t)) return (null, Importherkunft.Leer);
                return (gespiegelt ? 180.0 - t : t, _datei);
            }

            /// <summary>Azimut aus Sicht des beheizten Raums: gespiegelt um 180° gedreht; 0 … 360°.</summary>
            private (double?, Importherkunft) Azimut(double? azimut, bool gespiegelt)
            {
                if (!(azimut is double a) || double.IsNaN(a) || double.IsInfinity(a)) return (null, Importherkunft.Leer);
                double w = (gespiegelt ? a + 180.0 : a) % 360.0;
                if (w < 0.0) w += 360.0;
                return (w + 0.0, _datei);   // + 0.0 macht aus −0° die 0°
            }

            // ------------------------------------------------------------------
            //  Meldungen, Summenprobe, Probe über den Bauteilweg
            // ------------------------------------------------------------------

            private void Sammelmeldungen()
            {
                if (_ohneAzimut.Count > 0) Fehler(AZIMUT_FEHLT, Ganz(_ohneAzimut.Count), Liste(_ohneAzimut));
                if (_ohneU.Count > 0) Fehler(UWERT_FEHLT, Ganz(_ohneU.Count), Liste(_ohneU), _klasse?.ToString() ?? "");
                if (_ohneFlaeche.Count > 0) Warnung(OHNE_FLAECHE, Ganz(_ohneFlaeche.Count), Liste(_ohneFlaeche));
                List<string> ohneNachbar = _abbild.BauteileOhneGebaeude.Select(b => b.Kennung).ToList();
                if (ohneNachbar.Count > 0) Warnung(OHNE_NACHBAR, Ganz(ohneNachbar.Count), Liste(ohneNachbar));
                if (_uVorgabe > 0) Info(U_VORGABE, Ganz(_uVorgabe), _klasse?.ToString() ?? "");
                if (_uVorgabeFrei > 0) Info(U_VORGABE_FREI, Ganz(_uVorgabeFrei), _klasse?.ToString() ?? "", _quellklasseFrei ?? "");
                if (_unbeheizt > 0) Info(UNBEHEIZT, Ganz(_unbeheizt), Zahl(_unbeheiztM2));
                if (_vorhangfassaden > 0) Info(VORHANGFASSADE, Ganz(_vorhangfassaden));
                if (_fensterErdreich > 0) Info(FENSTER_ERDREICH, Ganz(_fensterErdreich));
                if (_abgleich == null) return;

                // Der Namensabgleich: was er ergänzt hat, was ohne Treffer bleibt, was verworfen oder
                // als ungültig übergangen wurde.
                List<GebaeudeMaterialzeile> brauchen = _v._materialien.Where(m => m.BrauchtAbgleich).ToList();
                if (_katalogschichten > 0)
                    Info(ABGLEICH, Ganz(_katalogschichten), Ganz(_v._aufbauten.Count(a => a.Herkunft == Importherkunft.Katalog)),
                         Ganz(brauchen.Count(m => m.Treffer != null && m.Treffer.Getroffen)), Ganz(brauchen.Count));
                List<string> ohne = _v.OhneTreffer.Select(m => m.Name).ToList();
                if (ohne.Count > 0) Warnung(BAUSTOFF_UNBEKANNT, Ganz(ohne.Count), Liste(ohne));
                if (_ungueltig.Count > 0) Warnung(STOFFWERT_UNGUELTIG, Ganz(_ungueltig.Count), Liste(_ungueltig));
                if (_verworfen.Count > 0) Warnung(SCHICHT_VERWORFEN, Ganz(_verworfen.Count), Liste(_verworfen));
                if (_luftschichten > 0) Info(LUFTSCHICHT, Ganz(_luftschichten));
            }

            /// <summary>Jede Gruppe summiert dieselbe Fläche wie das Summenfeld der Zuordnung.</summary>
            private void Summenprobe(GebaeudeImportSatz satz)
            {
                foreach (string feld in Summenfelder)
                {
                    double soll = satz.Zeile(feld)?.Wert ?? 0.0;
                    double ist = _v.Summe(feld);
                    if (Math.Abs(ist - soll) > SUMMEN_TOLERANZ * Math.Max(1.0, Math.Abs(soll)))
                        Fehler(SUMME_ABWEICHUNG, feld, Zahl(ist), Zahl(soll));
                }
            }

            /// <summary>
            /// <b>Die Probe</b>: die Zeilenprüfung des Schreibwegs, die Abbildung Zeile → Kern und der
            /// Bauteilweg mit den Gebäudegrößen der Zuordnung (H_ve = 0 — die Lüftung prüft der Lauf).
            /// </summary>
            private void Probe(GebaeudeImportSatz satz)
            {
                string pruef = GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { _v.Zone });
                if (pruef != null)
                {
                    Fehler(BAUTEILWEG, pruef);
                    return;
                }
                try
                {
                    GebaeudeZonensatz zs = GebaeudeZonenabbildung.AlsZonensatz(_v.Zone, _v.AufbautenJeId);
                    double af = _v.Zone.Nutzflaeche ?? 100.0;
                    double g = satz.Zeile(GebaeudeZielfelder.G_WERT)?.Wert is double gw && gw > 0.0 && gw <= 1.0 ? gw : 0.6;
                    int bauart = GebaeudeZielfelder.BauartIndex(satz.Zeile(GebaeudeZielfelder.BAUART)?.Textwert);
                    if (bauart < 0) bauart = Gebaeudebauweise.SCHWER;
                    List<BauteilEingang> bauteile = zs.Bauteile
                        .Select(b => b.MitGebaeudewerten(g, GebaeudeFestwerte.VORGABE_RAHMENANTEIL, GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR))
                        .ToList();
                    ErsatzparameterRC.AusBauteilweg(new BauteilwegGebaeude(_v.Zone.Bezeichner, af,
                        Gebaeudebauweise.BauweiseAusBauart(bauart, af), GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN,
                        _v.Innenflaechenfaktor ?? GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR, 0.0), bauteile);
                }
                catch (GebaeudeModellException ex)
                {
                    Fehler(BAUTEILWEG, ex.Message);
                }
            }

            private void Fehler(string schluessel, params string[] werte) => _v._meldungen.Add(new PruefMeldung(PruefStufe.Fehler, schluessel, werte));

            private void Warnung(string schluessel, params string[] werte) => _v._meldungen.Add(new PruefMeldung(PruefStufe.Warnung, schluessel, werte));

            private void Info(string schluessel, params string[] werte) => _v._meldungen.Add(new PruefMeldung(PruefStufe.Info, schluessel, werte));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>Die Kern-Randbedingung einer Seite.</summary>
        private static Bauteilrand RandAus(Randbedingung rand)
        {
            switch (rand)
            {
                case Randbedingung.Aussenluft: return Bauteilrand.Aussenluft;
                case Randbedingung.Erdreich: return Bauteilrand.Erdreich;
                default: return Bauteilrand.Unbeheizt;
            }
        }

        /// <summary>Die Randbedingung des Abbilds zu einer Kern-Randbedingung (für <see cref="SchichtwerteNaht"/>).</summary>
        private static Randbedingung RandAbbild(Bauteilrand rand)
        {
            switch (rand)
            {
                case Bauteilrand.Aussenluft: return Randbedingung.Aussenluft;
                case Bauteilrand.Erdreich: return Randbedingung.Erdreich;
                case Bauteilrand.Innen: return Randbedingung.Innen;
                default: return Randbedingung.Unbeheizt;
            }
        }

        /// <summary>Der Persistenzwert der Randbedingung: „innerhalb der Zone" steht als NULL an Innenwand und Decke.</summary>
        private static string RandWert(Bauteilart art, Bauteilrand rand)
            => rand == Bauteilrand.Innen ? null : GebaeudeZonenabbildung.RandFuerZeile(art, rand, "");

        /// <summary>Das U-Feld der Vorgaben zu einem Summenfeld.</summary>
        private static string UFeld(string summenfeld)
        {
            switch (summenfeld)
            {
                case GebaeudeZielfelder.FLAECHE_AUSSENWAND: return GebaeudeZielfelder.U_AUSSENWAND;
                case GebaeudeZielfelder.FLAECHE_DACH: return GebaeudeZielfelder.U_DACH;
                case GebaeudeZielfelder.FLAECHE_GRUND: return GebaeudeZielfelder.U_GRUND;
                case GebaeudeZielfelder.FENSTER_GESAMT: return GebaeudeZielfelder.U_FENSTER;
                default: return GebaeudeZielfelder.U_SONSTIGE;
            }
        }

        private static string Name(AbbildBauteil b) => string.IsNullOrWhiteSpace(b.Name) ? b.Kennung ?? "" : b.Name.Trim();

        private static string Kuerzen(string text, int laenge)
        {
            if (text == null) return "";
            if (text.Length <= laenge) return text;
            int n = char.IsHighSurrogate(text[laenge - 1]) ? laenge - 1 : laenge;
            return text.Substring(0, n);
        }

        private static string Liste(List<string> kennungen)
            => kennungen.Count <= 5 ? string.Join(", ", kennungen) : string.Join(", ", kennungen.Take(5)) + ", …";

        private static string Zahl(double w) => GebaeudeImportAblauf.Zahl(w);

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
