using System;

namespace KiKern
{
    /// <summary>
    /// Die sichtbaren Texte des Kerns - und die EINE Stelle, an der sie ausgetauscht werden.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein Lieferant und keine Ressourcendatei.</b> Der Kern darf
    /// <c>MyResource.Resource</c> nicht referenzieren: er ist UI- und ressourcenfrei und
    /// hat ueberhaupt keine Paketreferenzen (Fachkonzept 3.7). Er kennt deshalb nur
    /// SCHLUESSEL. Das Anwendungsprojekt setzt beim Start <see cref="Lieferant"/> und
    /// beantwortet damit jeden Schluessel aus <c>MyResource</c> - in der Sprache, die der
    /// Anwender eingestellt hat.
    /// </para>
    /// <para>
    /// <b>Warum Eigenschaften und keine Konstanten.</b> Eine <c>const</c> wird beim
    /// Uebersetzen in den Aufrufer kopiert; ein spaeter gesetzter Lieferant koennte sie
    /// nicht mehr erreichen. Die deutschen Saetze bleiben als Vorgabe stehen - sie greifen,
    /// solange kein Lieferant gesetzt ist (Aktionsharnisch, Tests, Konsolenlauf) und
    /// jedesmal, wenn ein Schluessel im Katalog fehlt. Der Assistent bleibt damit auch dann
    /// sprechfaehig, wenn an den Ressourcen etwas schiefgegangen ist.
    /// </para>
    /// <para>
    /// Formatplatzhalter sind durchnummeriert ({0}, {1}, …), damit die Uebersetzung die
    /// Reihenfolge aendern kann.
    /// </para>
    /// </remarks>
    public static class KiTexte
    {
        /// <summary>
        /// Liefert zu einem Schluessel den Anzeigetext, oder <c>null</c>/leer, wenn der
        /// Katalog ihn nicht kennt. Wird vom Anwendungsprojekt beim Start gesetzt.
        /// </summary>
        /// <remarks>
        /// Der Lieferant darf nicht werfen; tut er es doch, greift die deutsche Vorgabe.
        /// Ein fehlender Text ist ein Schoenheitsfehler, kein Grund, eine Aktion scheitern
        /// zu lassen.
        /// </remarks>
        public static Func<string, string?>? Lieferant { get; set; }

        /// <summary>Holt einen Text ueber den Lieferanten; sonst die deutsche Vorgabe.</summary>
        public static string Hole(string schluessel, string vorgabe)
        {
            Func<string, string?>? l = Lieferant;
            if (l == null) return vorgabe;
            try
            {
                string? text = l(schluessel);
                return string.IsNullOrEmpty(text) ? vorgabe : text!;
            }
            catch
            {
                return vorgabe;
            }
        }

        // ================================================================== Schluessel

        /// <summary>Namensvorsatz aller Schluessel dieses Katalogs.</summary>
        public const string Vorsatz = "KI_KERN_";

        // ----------------------------------------------------------- Wirkungssaetze

        /// <summary>Wirkung einer Leseaktion - Vorbelegung fuer Stufe 1.</summary>
        public static string WirkungLesen => Hole(Vorsatz + "WIRKUNG_LESEN",
            "Diese Aktion liest nur; sie ändert nichts.");

        // ----------------------------------------------------------- Registerfehler

        /// <summary>{0} = angefragter Name, {1} = Liste der bekannten Namen.</summary>
        public static string AktionUnbekannt => Hole(Vorsatz + "AKTION_UNBEKANNT",
            "Die Aktion „{0}“ gibt es nicht. Bekannt sind: {1}.");

        /// <summary>{0} = Aktionsname.</summary>
        public static string AktionOhneAusfuehrung => Hole(Vorsatz + "AKTION_OHNE_AUSFUEHRUNG",
            "Die Aktion „{0}“ ist deklariert, aber nicht verdrahtet.");

        // ----------------------------------------------------------- Parameterfehler

        /// <summary>{0} = Anzeigename, {1} = Parametername.</summary>
        public static string PflichtfeldFehlt => Hole(Vorsatz + "PFLICHTFELD_FEHLT",
            "Pflichtangabe „{0}“ ({1}) fehlt.");

        /// <summary>{0} = Parametername, {1} = Liste der erlaubten Namen.</summary>
        public static string ParameterUnbekannt => Hole(Vorsatz + "PARAMETER_UNBEKANNT",
            "Den Parameter „{0}“ kennt diese Aktion nicht. Erlaubt sind: {1}.");

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public static string KeineGanzzahl => Hole(Vorsatz + "KEINE_GANZZAHL",
            "„{0}“ erwartet eine ganze Zahl; geliefert wurde „{1}“.");

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public static string KeineZahl => Hole(Vorsatz + "KEINE_ZAHL",
            "„{0}“ erwartet eine Zahl; geliefert wurde „{1}“.");

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public static string KeinText => Hole(Vorsatz + "KEIN_TEXT",
            "„{0}“ erwartet einen Text; geliefert wurde „{1}“.");

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public static string KeinWahrheitswert => Hole(Vorsatz + "KEIN_WAHRHEITSWERT",
            "„{0}“ erwartet ja oder nein; geliefert wurde „{1}“.");

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public static string KeineListe => Hole(Vorsatz + "KEINE_LISTE",
            "„{0}“ erwartet eine Liste ganzer Zahlen; geliefert wurde „{1}“.");

        /// <summary>{0} = Anzeigename.</summary>
        public static string ListeLeer => Hole(Vorsatz + "LISTE_LEER",
            "Die Liste „{0}“ ist leer.");

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = Untergrenze, {3} = Obergrenze.</summary>
        public static string AusserhalbBereich => Hole(Vorsatz + "AUSSERHALB_BEREICH",
            "„{0}“ liegt mit {1} außerhalb des zulässigen Bereichs {2} bis {3}.");

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = Untergrenze.</summary>
        public static string UnterGrenze => Hole(Vorsatz + "UNTER_GRENZE",
            "„{0}“ ist mit {1} kleiner als der zulässige Mindestwert {2}.");

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = Obergrenze.</summary>
        public static string UeberGrenze => Hole(Vorsatz + "UEBER_GRENZE",
            "„{0}“ ist mit {1} größer als der zulässige Höchstwert {2}.");

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = erlaubte Werte.</summary>
        public static string WertNichtErlaubt => Hole(Vorsatz + "WERT_NICHT_ERLAUBT",
            "„{0}“ kennt den Wert „{1}“ nicht. Erlaubt sind: {2}.");

        /// <summary>{0} = Anzeigename, {1} = Hoechstlaenge.</summary>
        public static string TextZuLang => Hole(Vorsatz + "TEXT_ZU_LANG",
            "„{0}“ ist zu lang (höchstens {1} Zeichen).");

        /// <summary>{0} = Anzeigename.</summary>
        public static string TextLeer => Hole(Vorsatz + "TEXT_LEER",
            "„{0}“ darf nicht leer sein.");

        /// <summary>Der Aufrufrumpf war kein JSON-Objekt.</summary>
        public static string KeinObjekt => Hole(Vorsatz + "KEIN_OBJEKT",
            "Die Parameter müssen als JSON-Objekt kommen.");

        // ----------------------------------------------------------- Absichtserkennung

        /// <summary>
        /// Der Schutzstufen-Riegel hat die Aktion angehalten.
        /// {0} = Aktionsname, {1} = Klartext der Stufe.
        /// </summary>
        public static string RiegelZu => Hole(Vorsatz + "RIEGEL_ZU",
            "Die Aktion „{0}“ gehört zu {1} und wird noch nicht ausgeführt — " +
            "das kommt mit der Bestätigungsschicht.");

        /// <summary>Der Rundendeckel ist erreicht. {0} = Zahl der Runden.</summary>
        public static string RundendeckelErreicht => Hole(Vorsatz + "RUNDENDECKEL_ERREICHT",
            "Der Assistent ist nach {0} Runden zu keiner abschließenden Antwort gekommen und " +
            "bricht ab. Bitte die Frage kleiner fassen oder die Aktion von Hand wählen.");

        /// <summary>
        /// Das Modell wollte mehrere Aktionen zugleich.
        /// {0} = ausgefuehrte Aktion, {1} = die uebrigen.
        /// </summary>
        public static string MehrereWerkzeuge => Hole(Vorsatz + "MEHRERE_WERKZEUGE",
            "Das Modell hat mehrere Aktionen zugleich vorgeschlagen; ausgeführt wird nur „{0}“. " +
            "Ebenfalls vorgeschlagen: {1}.");

        /// <summary>Die Antwort enthielt weder Text noch einen brauchbaren Aufruf.</summary>
        public static string AntwortLeer => Hole(Vorsatz + "ANTWORT_LEER",
            "Die Antwort des Modells war leer.");

        // ----------------------------------------------------------- Freigabe (Etappe 3)

        /// <summary>
        /// Die Aktion veraendert Daten und laeuft nur nach Klick. {0} = Aktionsname.
        /// </summary>
        public static string FreigabeFehlt => Hole(Vorsatz + "FREIGABE_FEHLT",
            "Die Aktion „{0}“ verändert Daten und läuft nur nach ausdrücklicher " +
            "Bestätigung im Chat.");

        /// <summary>Es liegt noch keine Entscheidung des Anwenders vor.</summary>
        public static string FreigabeOffen => Hole(Vorsatz + "FREIGABE_OFFEN",
            "Für diese Aktion liegt noch keine Entscheidung des Anwenders vor.");

        /// <summary>Der Anwender hat abgelehnt.</summary>
        public static string FreigabeAbgelehnt => Hole(Vorsatz + "FREIGABE_ABGELEHNT",
            "Der Anwender hat die Aktion abgebrochen; es wurde nichts geändert.");

        /// <summary>Die Vorschau ist verfallen (Fachkonzept 3.5, Punkt 5).</summary>
        public static string FreigabeVerfallen => Hole(Vorsatz + "FREIGABE_VERFALLEN",
            "Die Vorschau ist älter als eine Minute und wurde verworfen. " +
            "Bitte die Aktion neu anfragen.");

        /// <summary>Der Vorgang wurde abgebrochen.</summary>
        public static string FreigabeAbgebrochen => Hole(Vorsatz + "FREIGABE_ABGEBROCHEN",
            "Der Vorgang wurde abgebrochen; es wurde nichts geändert.");

        /// <summary>Ein Klick gilt fuer genau einen Aufruf (Fachkonzept 3.5, Punkt 4).</summary>
        public static string FreigabeVerbraucht => Hole(Vorsatz + "FREIGABE_VERBRAUCHT",
            "Diese Bestätigung wurde bereits eingelöst. Ein Klick gilt für genau einen Aufruf.");

        /// <summary>Zwischen Vorschau und Klick lief eine andere Aktion.</summary>
        public static string FreigabeUeberholt => Hole(Vorsatz + "FREIGABE_UEBERHOLT",
            "Seit der Vorschau ist eine andere Aktion gelaufen; die Bestätigung gilt nicht mehr. " +
            "Bitte neu anfragen.");

        /// <summary>Die Freigabe gehoert zu einem anderen Aufruf.</summary>
        public static string FreigabeFremd => Hole(Vorsatz + "FREIGABE_FREMD",
            "Die Bestätigung gehört zu einem anderen Aufruf und gilt hier nicht.");

        /// <summary>
        /// Die Stufe ist in dieser Ausbaustufe ueberhaupt nicht freigegeben.
        /// {0} = Aktionsname, {1} = Klartext der Stufe.
        /// </summary>
        public static string RiegelStufeGesperrt => Hole(Vorsatz + "RIEGEL_STUFE_GESPERRT",
            "Die Aktion „{0}“ gehört zu {1} und ist in dieser Ausbaustufe noch nicht freigegeben.");

        // ----------------------------------------------------------- Bestaetigungstext

        /// <summary>Ueberschriftfeld „Aktion".</summary>
        public static string FeldAktion => Hole(Vorsatz + "FELD_AKTION", "Aktion");

        /// <summary>Ueberschriftfeld „Zweck".</summary>
        public static string FeldZweck => Hole(Vorsatz + "FELD_ZWECK", "Zweck");

        /// <summary>Ueberschriftfeld „Angaben".</summary>
        public static string FeldAngaben => Hole(Vorsatz + "FELD_ANGABEN", "Angaben");

        /// <summary>Ueberschriftfeld „Wirkung".</summary>
        public static string FeldWirkung => Hole(Vorsatz + "FELD_WIRKUNG", "Wirkung");

        /// <summary>Ueberschriftfeld „Vorschau".</summary>
        public static string FeldVorschau => Hole(Vorsatz + "FELD_VORSCHAU", "Vorschau");

        /// <summary>Ueberschriftfeld „Andockpunkt".</summary>
        public static string FeldAndockpunkt => Hole(Vorsatz + "FELD_ANDOCKPUNKT", "Andockpunkt");

        /// <summary>Ueberschriftfeld „Rückholbar" (Fachkonzept 4.4, Punkt 3).</summary>
        public static string FeldRueckholbar => Hole(Vorsatz + "FELD_RUECKHOLBAR", "Rückholbar");

        /// <summary>Die Aktion ist umkehrbar - der Vorzustand ist bekannt.</summary>
        public static string RueckholbarJa => Hole(Vorsatz + "RUECKHOLBAR_JA",
            "ja — der Vorzustand ist bekannt und lässt sich als neue, ebenfalls " +
            "bestätigungspflichtige Aktion zurückschreiben");

        /// <summary>Die Aktion ist nicht umkehrbar.</summary>
        public static string RueckholbarNein => Hole(Vorsatz + "RUECKHOLBAR_NEIN",
            "nein — die Änderung lässt sich nicht automatisch zurücknehmen");

        /// <summary>Ueberschriftfeld „Sicherungspunkt" (Fachkonzept 4.4, Punkt 1).</summary>
        public static string FeldSicherung => Hole(Vorsatz + "FELD_SICHERUNG", "Sicherungspunkt");

        /// <summary>Ueberschriftfeld „Gültig bis" (Verfall der Vorschau).</summary>
        public static string FeldGueltigBis => Hole(Vorsatz + "FELD_GUELTIG_BIS", "Gültig bis");

        /// <summary>Steht bei „Angaben", wenn die Aktion keine Parameter hat.</summary>
        public static string KeineAngaben => Hole(Vorsatz + "KEINE_ANGABEN", "keine");

        /// <summary>Klartext der Stufe 1.</summary>
        public static string StufeLesen => Hole(Vorsatz + "STUFE_LESEN", "Stufe 1 – nur lesend");

        /// <summary>Klartext der Stufe 2.</summary>
        public static string StufeSchreiben => Hole(Vorsatz + "STUFE_SCHREIBEN", "Stufe 2 – verändert Daten");

        /// <summary>Klartext der Stufe 3.</summary>
        public static string StufeRechnen => Hole(Vorsatz + "STUFE_RECHNEN", "Stufe 3 – rechnet, läuft länger");

        /// <summary>Klartextname einer Stufe fuer die Bestaetigung.</summary>
        public static string Stufe(Schutzstufe stufe)
        {
            switch (stufe)
            {
                case Schutzstufe.Lesen: return StufeLesen;
                case Schutzstufe.Schreiben: return StufeSchreiben;
                case Schutzstufe.Rechnen: return StufeRechnen;
                default: throw new ArgumentOutOfRangeException(nameof(stufe));
            }
        }

        // ------------------------------------------ Formularsteuerung (Etappe 3b)

        /// <summary>Ueberschriftfeld „Maske" im Feldblock.</summary>
        public static string FeldMaske => Hole(Vorsatz + "FELD_MASKE", "Maske");

        /// <summary>
        /// Steht im Feldblock, wo ein Wert leer ist - eine Zeile ohne alten Wert saehe
        /// sonst aus wie ein Anzeigefehler.
        /// </summary>
        public static string WertLeer => Hole(Vorsatz + "WERT_LEER", "(leer)");

        /// <summary>Ein Knopf der Maske wird ausgeloest. {0} = Beschriftung des Knopfes.</summary>
        public static string KnopfWirdAusgeloest => Hole(Vorsatz + "KNOPF_WIRD_AUSGELOEST",
            "Knopf ‚{0}' wird ausgelöst");

        /// <summary>
        /// Der dauerhafte Hinweis im Chatfenster, wenn die Feldsicherung abgeschaltet ist
        /// (Fachkonzept 11.5). Der Satz nennt ausdruecklich auch, was WEITER gilt - sonst
        /// liesse sich „Feldsicherung AUS" als „gar keine Bestaetigung mehr" lesen.
        /// </summary>
        public static string FeldsicherungAus => Hole(Vorsatz + "FELDSICHERUNG_AUS",
            "Feldsicherung AUS — Felder werden ohne gesonderte Bestätigung gesetzt. " +
            "Die Bestätigung datenverändernder Aktionen bleibt bestehen.");

        /// <summary>
        /// Vermerk in jeder Protokollzeile, solange die Feldsicherung abgeschaltet ist -
        /// kurz gehalten, weil er in das Ergebnisfeld der Zeile passen muss.
        /// </summary>
        public static string FeldsicherungVermerk => Hole(Vorsatz + "FELDSICHERUNG_VERMERK",
            "Feldsicherung aus");

        // -------------------------------------------------- Stoerungen des Modelldienstes

        /// <summary>
        /// Kein Zugangsschluessel hinterlegt - der EINE Satz, mit dem eine Anfrage gar
        /// nicht erst hinausgeht (Anwenderbefund <b>W15b-B-2</b>).
        /// </summary>
        public static string DienstKeinSchluessel => Hole(Vorsatz + "DIENST_KEIN_SCHLUESSEL",
            "Kein API-Schlüssel hinterlegt — bitte unter „Einstellungen…\" eintragen.");

        /// <summary>Der Dienst hat die Zugangsdaten abgewiesen (401, 403).</summary>
        public static string DienstAbgelehnt => Hole(Vorsatz + "DIENST_ABGELEHNT",
            "Der KI-Dienst hat die Anfrage abgelehnt ({0}). Prüfen Sie den Schlüssel unter „Einstellungen…\".");

        /// <summary>Der Dienst hat den Schluessel nicht angenommen (400).</summary>
        public static string DienstSchluesselUngueltig => Hole(Vorsatz + "DIENST_SCHLUESSEL_UNGUELTIG",
            "Der KI-Dienst hat den Schlüssel nicht angenommen ({0}). Prüfen Sie ihn unter „Einstellungen…\".");

        /// <summary>Das Kontingent des Anbieters ist erschoepft (429).</summary>
        public static string DienstKontingent => Hole(Vorsatz + "DIENST_KONTINGENT",
            "Der KI-Dienst nimmt zurzeit keine weitere Anfrage an ({0}). Bitte später noch einmal fragen.");

        /// <summary>Der Dienst selbst ist gestoert (5xx).</summary>
        public static string DienstGestoert => Hole(Vorsatz + "DIENST_GESTOERT",
            "Der KI-Dienst ist zurzeit nicht erreichbar ({0}). Bitte später noch einmal fragen.");

        /// <summary>Jede andere Absage des Dienstes.</summary>
        public static string DienstUnbekannt => Hole(Vorsatz + "DIENST_UNBEKANNT",
            "Der KI-Dienst hat die Anfrage nicht beantwortet ({0}). Einzelheiten stehen unter „Protokoll anzeigen\".");

        /// <summary>Der ROHTEXT des Anbieters, wie er ins Protokoll geschrieben wird.</summary>
        public static string DienstProtokollzeile => Hole(Vorsatz + "DIENST_PROTOKOLLZEILE",
            "KI-Dienst HTTP {0}: {1}");

        /// <summary>Ueberschrift des Stoerungsteils unter „Protokoll anzeigen\".</summary>
        public static string DienstProtokollkopf => Hole(Vorsatz + "DIENST_PROTOKOLLKOPF",
            "Störungen des KI-Dienstes (diese Sitzung):");
    }
}
