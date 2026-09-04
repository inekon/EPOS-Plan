using System;

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
    public sealed record ProjektKopfZeile(
        int Id,
        string Name,
        string Kunde = "",
        string Beschreibung = "",
        DateTime? Geaendert = null,
        string Klimazone = "",
        string Ausstattung = "");

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

    /// <summary>Wie weit der Loeschweg gekommen ist (iU9-W15a.0d).</summary>
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
        Mehrdeutig
    }

    /// <summary>
    /// Ergebnis von <c>ProjektCtrl.LoeschenMitVorarbeiten</c> (iU9-W15a.0d).
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
