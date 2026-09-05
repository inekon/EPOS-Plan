using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Zeile der Projektliste (iU9-W15a.0a).
    ///
    /// <para><b>Warum es diesen Typ gibt.</b> Der Bestand fuehrte VIER Projektlisten
    /// nebeneinander (Befund W15a-B52): das <c>ListView</c> in <c>ProjektAuswahl</c>
    /// (drei Spalten, Suche, Sortierung), das zweispaltige <c>ListView</c> in
    /// „Speichern unter", die <c>ComboBox</c> in „Loeschen" (ueber die
    /// Erweiterungsmethode <c>ControllerListen.FillComboBox</c>) und die eigene
    /// Schleife in „Export/Import". Jede las <c>Tab_Projekt</c> auf ihre Weise. Seit
    /// iU9-W15a liest sie EINE Stelle — <see cref="ProjektCtrl.NamenListe"/> —, und
    /// der Baustein <c>EPOS.UI/Bausteine/ProjektListe</c> zeigt sie.</para>
    ///
    /// <para><b>Die zwei letzten Felder gehoeren dem iOS-EINSTIEG.</b>
    /// <c>EPOS.UI/Seiten/Projektliste</c> zeigt statt Kunde und Aenderungsdatum die
    /// Klimaregion und die Ausstattung; beide Werte stehen nicht in
    /// <c>Tab_Projekt</c>, sondern werden von <c>IosProjektQuelle</c> zusammengesetzt.
    /// Sie sind deshalb vorbelegt und bleiben in den Windows-Wegen leer.</para>
    /// </summary>
    /// <param name="Id"><c>Tab_Projekt.ID</c>.</param>
    /// <param name="Name">Projektname — der fuehrende Schluessel des Bestands.</param>
    /// <param name="Kunde">Kunde; leer, wenn nicht gepflegt.</param>
    /// <param name="Beschreibung">Beschreibung. Sie ist in der Liste NICHT sichtbar, wird aber
    /// durchsucht (Befund W15a-B22) — wer sie beim Port vergisst, dem „findet die Suche nichts mehr".</param>
    /// <param name="Geaendert">Aenderungsdatum; <c>null</c>, wenn keines gespeichert ist.</param>
    /// <param name="Klimazone">Nur iOS-Einstieg: Klimaregion des Projekts.</param>
    /// <param name="Ausstattung">Nur iOS-Einstieg: Kurzform der belegten Gewerke.</param>
    /// <param name="StammId">Ist die Zeile eine VARIANTE, steht hier die Id ihres
    /// Stammprojekts (<c>Tab_Variante.ID_ProjektRef</c>); sonst 0.</param>
    /// <param name="Bezeichner">Der Variantenbezeichner (<c>Tab_Variante.Variantenname</c>) —
    /// beim Stamm und bei einem gewoehnlichen Projekt leer.</param>
    /// <param name="StammName">Der Projektname des Stammprojekts; leer, wenn es keines gibt.</param>
    public sealed record ProjektKopfZeile(
        int Id,
        string Name,
        string Kunde = "",
        string Beschreibung = "",
        DateTime? Geaendert = null,
        string Klimazone = "",
        string Ausstattung = "",
        int StammId = 0,
        string Bezeichner = "",
        string StammName = "")
    {
        /// <summary>
        /// Ist die Zeile eine Variante eines anderen Projekts? (Anwenderwunsch vom
        /// 05.09.2026, W15a-E-1.)
        ///
        /// <para><b>Warum die Frage ueberhaupt gestellt wird.</b> Eine Variante ist im
        /// Bestand ein vollwertiges Kopie-Projekt; erkennbar war sie bisher AM NAMEN,
        /// den <c>VariantenCtrl.AnlegenAusStamm</c> als „&lt;Stamm&gt; - &lt;Bezeichner&gt;"
        /// bildet. Sobald eine schmale Liste den Namen abschneidet, ist dieses
        /// Kennzeichen weg — drei Zeilen „Booster-Kette mit Kombi-Spe…" sind dann nicht
        /// mehr zu unterscheiden. Deshalb reist die Herkunft seither als eigenes Feld
        /// mit, statt im Namen zu stecken.</para>
        /// </summary>
        public bool IstVariante => StammId > 0;
    }

    /// <summary>
    /// Die neun Felder der ersten Assistentenseite (iU9-W15a.0g) — der Ersatz fuer die
    /// zehn <c>Get*</c>-Methoden von <c>Wizard_Projekt</c> (Befund W15a-B42).
    ///
    /// <para><b>Warum ein Objekt und keine neun Methoden.</b> <c>Wizard_Projekt</c> war die
    /// EINZIGE Assistentenseite, die der Rahmen an sechs Stellen mit hartem Typumbruch
    /// auslas. Die Razor-Fassung traegt stattdessen eine einelementige Liste dieses Typs
    /// (<c>BlazorAssistentSeite&lt;ProjektKopfSeite, ProjektKopfDaten&gt;</c>, Weg (a) der
    /// Vermessung § 13.5) — dieselbe Mechanik wie die vier Bedarfsseiten aus W9.0a, ohne
    /// einen neuen Vertrag und ohne Umbau an der Assistentenhuelle (R-W15a-8).</para>
    ///
    /// <para><b>Zwei Altlasten sind dabei geradegezogen.</b> <c>GetDatum()</c> lieferte
    /// <c>DateTime.Now</c> statt des Feldes (der Name log, W15a-B39) — hier heisst dasselbe
    /// Feld <see cref="Aenderungsdatum"/> und wird beim Speichern gesetzt.
    /// <c>GetErstellDatum()</c> parste den ANGEZEIGTEN Text mit
    /// <c>DateTime.Parse</c> ohne Kultur und ohne <c>TryParse</c> (W15a-B40); hier reist
    /// das Datum als <see cref="DateTime"/>, die Anzeige ist Sache der Oberflaeche.</para>
    /// </summary>
    public sealed class ProjektKopfDaten
    {
        /// <summary>Projektname.</summary>
        public string Name { get; set; } = "";

        /// <summary>Beschreibung.</summary>
        public string Beschreibung { get; set; } = "";

        /// <summary>Kunde.</summary>
        public string Kunde { get; set; } = "";

        /// <summary>Bearbeiter.</summary>
        public string Bearbeiter { get; set; } = "";

        /// <summary>Erstelldatum (gesperrtes Feld).</summary>
        public DateTime Erstelldatum { get; set; } = DateTime.Now;

        /// <summary>Aenderungsdatum (gesperrtes Feld).</summary>
        public DateTime Aenderungsdatum { get; set; } = DateTime.Now;

        /// <summary>Id der Klimaregion (Stammsatz, <c>Tab_Klimaregion_STAMM.ID_Klimaregion</c>).</summary>
        public int IdKlimaregion { get; set; }

        /// <summary>Anzeigename der Klimaregion.</summary>
        public string Klimaname { get; set; } = "";

        /// <summary>
        /// Darf der Projektname geaendert werden? Im Neu-Modus ja, im Bearbeiten-Modus
        /// nein — der Ersatz fuer <c>SetEditProjektName(bool)</c>. Gesetzt VOR
        /// <c>Bestuecken</c>.
        /// </summary>
        public bool NameAenderbar { get; set; } = true;
    }

    /// <summary>
    /// Ergebnis der drei Vorpruefungen des Duplizierwegs (iU9-W15a.0b).
    ///
    /// <para>Dieselben drei Pruefungen stehen seit jeher in
    /// <c>ProjektDuplizierenCtrl.Duplizieren</c> — dort aber nur als Meldung mit
    /// Rueckgabe <c>-1</c>. Die Oberflaeche prueft deshalb ein zweites Mal, und zwar
    /// FALSCH: <c>listView_Projekt.FindItemWithText("Muster")</c> trifft mit
    /// Praefix-Semantik ein vorhandenes „Musterprojekt" und lehnt einen freien Namen ab
    /// (Befund W15a-B10). Eine Pruefregel, ein Ort.</para>
    /// </summary>
    /// <summary>
    /// Befund der Kopfpruefung (Nutzerauftrag 02.09.2026, mit Merge 5 aus
    /// <c>Wizard_Projekt.Pruefe</c> in den Kern gezogen): Pflichtfelder und Namensdoppel.
    /// </summary>
    public enum ProjektKopfBefund
    {
        Ok = 0,
        NameLeer,
        NameVorhanden,
        KlimaLeer
    }

    /// <summary>
    /// Die Regeln des Projektkopfs - EINE Wahrheit fuer die Razor-Seite (Hinweis unter dem
    /// Feld) und den Assistenten (Veto beim Verlassen der Seite): Der Name ist Pflicht und
    /// darf bei einem NEUEN Projekt nicht vergeben sein (Vergleich ohne Gross/Klein), die
    /// Klimaregion ist Pflicht (Id oder - bei Altprojekten - der Name).
    /// </summary>
    public static class ProjektKopfRegeln
    {
        public static ProjektKopfBefund Pruefe(ProjektKopfDaten daten, IEnumerable<string> vergebeneNamen)
        {
            if (daten == null || string.IsNullOrWhiteSpace(daten.Name)) return ProjektKopfBefund.NameLeer;
            if (daten.NameAenderbar && vergebeneNamen != null)
                foreach (string n in vergebeneNamen)
                    if (string.Equals((n ?? "").Trim(), daten.Name.Trim(), StringComparison.CurrentCultureIgnoreCase))
                        return ProjektKopfBefund.NameVorhanden;
            if (daten.IdKlimaregion <= 0 && string.IsNullOrWhiteSpace(daten.Klimaname)) return ProjektKopfBefund.KlimaLeer;
            return ProjektKopfBefund.Ok;
        }
    }

    public enum DuplizierBefund
    {
        /// <summary>Beide Namen tragfaehig, das Ziel ist frei.</summary>
        Ok = 0,

        /// <summary>Quell- oder Zielname leer.</summary>
        NamenLeer,

        /// <summary>Das Quellprojekt gibt es nicht.</summary>
        QuelleFehlt,

        /// <summary>Ein Projekt dieses Namens existiert bereits.</summary>
        ZielExistiert
    }

    /// <summary>
    /// Ergebnis von <c>ProjektDuplizierenCtrl.VerwaltungsfelderSetzen</c> (iU9-W15a.0c).
    ///
    /// <para><b>Warum kein <c>bool</c>.</b> Der Vorlaeufer
    /// (<c>Form_ProjektSpeichernUnter.VerwaltungsfelderAufKopieSchreiben</c>) zeigte DREI
    /// verschiedene Meldungen — Kopie nicht gefunden, Schreiben fehlgeschlagen, Ausnahme
    /// mit Text. Ein Wahrheitswert traegt sie nicht; die Oberflaeche muesste raten, was
    /// sie meldet.</para>
    ///
    /// <para><b>Die Fehlerpolitik bleibt wortgleich</b> (R-W15a-11): Ein Fehler wird
    /// GEMELDET, aber NICHT zurueckgerollt — die Kopie ist an dieser Stelle vollstaendig
    /// angelegt.</para>
    /// </summary>
    public enum VerwaltungsfelderBefund
    {
        /// <summary>Die drei Felder stehen auf der Kopie.</summary>
        Ok = 0,

        /// <summary>Die Kopie wurde nicht gefunden.</summary>
        KopieFehlt,

        /// <summary>Das UPDATE hat nichts geschrieben.</summary>
        NichtGespeichert,

        /// <summary>Eine Ausnahme; ihr Text steht in <c>Fehlertext</c>.</summary>
        Fehler
    }

    /// <summary>
    /// Wie weit ein Loeschweg gekommen ist (iU9-W15a.0d).
    ///
    /// <para>Er traegt BEIDE Loeschwege: <c>ProjektCtrl.LoeschenMitVorarbeiten</c>
    /// (Projekt samt Vorarbeiten) und <c>VariantenCtrl.LoescheVariante</c> (Variante samt
    /// Verknuepfung und Energieanlagen). Seit dem Entscheid O-4 vom 04.09.2026 laufen
    /// beide durch dieselbe Vorpruefung auf einen mehrdeutigen Namen und melden denselben
    /// Befund; zwei getrennte Ergebnistypen fuer denselben Zweck gaebe es sonst ohne
    /// Not. <see cref="KeineVariante"/> und <see cref="Loeschfehler"/> kann nur der
    /// Variantenweg melden, <see cref="NameLeer"/> und
    /// <see cref="ApplikationsdatenFehler"/> nur der Projektweg.</para>
    /// </summary>
    public enum LoeschStand
    {
        /// <summary>Alle sechs Schritte gelaufen, das Projekt ist weg.</summary>
        Geloescht = 0,

        /// <summary>Kein Projektname — es wurde nichts angefasst.</summary>
        NameLeer,

        /// <summary>
        /// <c>Tab_Applikation</c> liess sich nicht zuruecksetzen. Der Vorlaeufer bricht
        /// hier ab, OHNE zu loeschen — sonst zeigte der gemerkte Projektstand auf ein
        /// Projekt, das es nicht mehr gibt.
        /// </summary>
        ApplikationsdatenFehler,

        /// <summary>
        /// Der Name trifft MEHRERE Projekte, und der Aufrufer hat das Loeschen aller
        /// nicht ausdruecklich verlangt (Entscheid W15a-O-3 vom 04.09.2026). Es wurde
        /// NICHTS angefasst; <c>Anzahl</c> sagt, wie viele Projekte den Namen tragen.
        ///
        /// <para>Regulaer kann das nicht vorkommen: <c>Tab_Projekt</c> traegt seit der
        /// SQLite-Migration den eindeutigen Index <c>Projektname</c>, und „Speichern
        /// unter" prueft ueber <c>PruefeNamen</c>. Ein Altbestand OHNE diesen Index kann
        /// den Fall aber fuehren — und dann darf der Loeschweg, der ueber den NAMEN
        /// laeuft, nicht still zwei Projekte mitnehmen.</para>
        /// </summary>
        Mehrdeutig,

        /// <summary>
        /// Nur <c>VariantenCtrl.LoescheVariante</c>: Das Projekt ist gar keine Variante —
        /// es haengt keine Zeile in <c>Tab_Variante</c> daran. Stammprojekte werden ueber
        /// diesen Weg nicht geloescht; es wurde nichts angefasst, der Grund steht im
        /// <c>Fehlertext</c>.
        /// </summary>
        KeineVariante,

        /// <summary>
        /// Nur <c>VariantenCtrl.LoescheVariante</c>: Einer der drei Schritte ist mit einer
        /// Ausnahme abgebrochen; ihr Text steht im <c>Fehlertext</c>. Wie im Vorlaeufer
        /// wird NICHT zurueckgerollt — was bis dahin lief, bleibt gelaufen.
        /// </summary>
        Loeschfehler
    }

    /// <summary>
    /// Ergebnis von <c>ProjektCtrl.LoeschenMitVorarbeiten</c> (iU9-W15a.0d) und — seit dem
    /// Entscheid O-4 vom 04.09.2026 — von <c>VariantenCtrl.LoescheVariante</c>.
    /// </summary>
    /// <param name="Stand">Wie weit der Weg gekommen ist.</param>
    /// <param name="Projektname">Der Name des geloeschten Projekts (fuer die Erfolgsmeldung).</param>
    /// <param name="Fehlertext">Der Ausnahmetext bei <see cref="LoeschStand.ApplikationsdatenFehler"/>; sonst leer.</param>
    /// <param name="Anzahl">
    /// Wie viele Projekte den Namen tragen (iU9-W15a, Entscheid O-3). Regulaer 1; bei
    /// <see cref="LoeschStand.Mehrdeutig"/> die Zahl, die in der Rueckfrage steht; bei
    /// <see cref="LoeschStand.NameLeer"/> 0.
    /// </param>
    public sealed record LoeschBefund(LoeschStand Stand, string Projektname, string Fehlertext = "", int Anzahl = 1);
}
