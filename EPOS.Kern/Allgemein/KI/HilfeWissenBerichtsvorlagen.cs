using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public static partial class HilfeWissen
    {
        /// <summary>Die Wiki-Stelle der Berichtsvorlagen (Seite „Bericht“, Abschnitt „Vorlage“).</summary>
        private const string WIKI_BERICHT = WIKI + "Bericht#vorlage";

        /// <summary>
        /// <b>Das Aktionswissen der Berichtsvorlagen</b> (Etappe BV-E1): je erklärbarer Meldung EIN
        /// Abschnitt mit Bedeutung, Ursache, Abhilfe und Wiki-Stelle — die Regeln des Vorlagenprüfers
        /// (<c>VF_PRUEF_*</c>), die Prüfmeldungen des Vorlagen-Controllers (<c>BV_VORLAGEN_*</c>), die
        /// Abschnitte der Laufmeldung (<c>BV_LAUF_*</c>) und die Befunde der Rückfrage vor dem Start
        /// (<c>BV_START_*</c>). Die Kennungen stehen in <see cref="KiMeldungskennung.Berichtsvorlagen"/>.
        ///
        /// <para><b>Eine eigene Datei, dieselbe Klasse.</b> Die Abschnitte folgen dem Muster von
        /// <see cref="Aktionswissen"/> (Titel mit Kennung, Bereich, BEDEUTUNG/URSACHE/ABHILFE/WIKI),
        /// stehen aber hier, damit die Liste der Berichtsvorlagen mit ihren Regeln wachsen kann, ohne
        /// das übrige Aktionswissen zu berühren. Deutsch wie das übrige Einbauwissen; zweisprachig sind
        /// die vorbelegten Fragen <c>KI_FRAGE_*</c>.</para>
        /// </summary>
        private static List<WissensAbschnitt> Berichtsvorlagenwissen()
        {
            return new List<WissensAbschnitt>
            {
                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_UNLESBAR,
                    "Meldung VF_PRUEF_UNLESBAR: Die Vorlage kann nicht gelesen werden",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: EPOS-Plan konnte die Word-Vorlage nicht öffnen oder nicht durchlaufen; so wird sie " +
                    "weder geprüft noch gefüllt. URSACHE: Die Datei ist leer, beschädigt, kein Word-Dokument (etwa " +
                    "eine umbenannte PDF- oder Textdatei) oder nur unvollständig übertragen. ABHILFE: Die Datei in " +
                    "Word öffnen und als Word-Dokument (.docx) speichern, dann erneut prüfen. Bis dahin den Bericht " +
                    "mit der Standardvorlage erstellen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_FORMAT,
                    "Meldung VF_PRUEF_FORMAT: Das Dateiformat der Vorlage wird nicht unterstützt",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage ist kein Word-Dokument (.docx) und keine Word-Vorlage (.dotx), sondern " +
                    "etwa Word 97–2003 (.doc), RTF, OpenDocument oder ein verschlüsseltes Dokument. URSACHE: " +
                    "EPOS-Plan füllt nur die Formate .docx und .dotx; ältere, fremde und kennwortgeschützte Dateien " +
                    "kann es nicht lesen. ABHILFE: Die Datei in Word öffnen und unter „Speichern unter“ als " +
                    "Word-Dokument (.docx) oder Word-Vorlage (.dotx) ohne Kennwortschutz speichern. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_GROESSE,
                    "Meldung VF_PRUEF_GROESSE: Die Vorlage ist zu groß",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Datei ist größer als die Grenze von 20 MB; sie wird weder geprüft noch gefüllt. " +
                    "URSACHE: Meist große, unkomprimierte Bilder (Fotos, Logos in Druckauflösung) oder eingebettete " +
                    "Objekte. Die Grenze schützt davor, dass eine Datei beim Öffnen den Speicher sprengt. ABHILFE: In " +
                    "Word die Bilder komprimieren (Bildformat › Bilder komprimieren) oder kleinere Bilder einfügen " +
                    "und die Vorlage neu speichern. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_GROESSE_ENTPACKT,
                    "Meldung VF_PRUEF_GROESSE_ENTPACKT: Die Vorlage ist entpackt zu groß",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Eine .docx ist ein gepacktes Archiv; entpackt überschreiten ihre Teile zusammen die " +
                    "Grenze von 100 MB. Die Vorlage wird weder geprüft noch gefüllt. URSACHE: Sehr große Bilder oder " +
                    "eingebettete Objekte — oder eine Datei, die klein gepackt, aber entpackt riesig ist. ABHILFE: " +
                    "Bilder komprimieren, eingebettete Objekte entfernen und die Vorlage neu speichern. WIKI: " +
                    "Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_MAKROS,
                    "Meldung VF_PRUEF_MAKROS: Die Vorlage enthält Makros",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage trägt ein Makroprojekt (.docm, .dotm oder VBA im Paket). EPOS-Plan " +
                    "verwendet solche Vorlagen nicht. URSACHE: Makros könnten beim Öffnen des Berichts Programmcode " +
                    "ausführen; ein Bericht aus EPOS-Plan enthält deshalb keine. ABHILFE: In Word „Speichern unter“ " +
                    "als Word-Dokument (.docx) wählen — dabei entfallen die Makros — und die neue Datei als Vorlage " +
                    "hinzufügen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_AENDERUNGEN,
                    "Meldung VF_PRUEF_AENDERUNGEN: Nachverfolgte Änderungen in der Vorlage",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: In der Vorlage stehen Änderungen, die noch nicht angenommen oder abgelehnt sind. " +
                    "URSACHE: Die Vorlage wurde mit eingeschalteter Änderungsnachverfolgung bearbeitet. EPOS-Plan " +
                    "nimmt Änderungen nicht selbst an — sonst stünde im Bericht womöglich gelöschter Text. ABHILFE: " +
                    "In Word unter „Überprüfen“ alle Änderungen annehmen oder ablehnen, die Nachverfolgung " +
                    "ausschalten und speichern. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_VORLAGENVERWEIS,
                    "Meldung VF_PRUEF_VORLAGENVERWEIS: Der Verweis auf die Dokumentvorlage wird entfernt",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage verweist auf eine Word-Dokumentvorlage (etwa Normal.dotm), aus der sie " +
                    "entstand. Beim Erstellen des Berichts entfernt EPOS-Plan diesen Verweis. URSACHE: Der Bericht " +
                    "soll auf jedem Rechner gleich aussehen und keine fremde Dokumentvorlage nachladen. ABHILFE: " +
                    "Nichts zu tun — Formatvorlagen, Kopf- und Fußzeilen der Vorlage bleiben erhalten. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_EXTERN,
                    "Meldung VF_PRUEF_EXTERN: Verknüpfte Inhalte werden entfernt",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage enthält Inhalte, die nur als Verknüpfung auf eine andere Datei " +
                    "eingebunden sind, etwa ein verknüpftes Bild oder eine verknüpfte Mappe. Beim Erstellen des " +
                    "Berichts werden sie entfernt. URSACHE: Eine Verknüpfung zeigt auf einen Pfad des eigenen " +
                    "Rechners; beim Empfänger des Berichts fehlte der Inhalt. ABHILFE: Bilder und Objekte, die im " +
                    "Bericht erscheinen sollen, in die Vorlage einbetten statt verknüpfen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_UEBERSCHRIFTEN,
                    "Meldung VF_PRUEF_UEBERSCHRIFTEN: Überschriftenstile fehlen oder tragen keine Gliederungsebene",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: EPOS-Plan setzt die Kapitelüberschriften des Berichts mit den Formatvorlagen " +
                    "„Überschrift 1“ bis „Überschrift 3“. In der Vorlage fehlt mindestens eine davon, oder sie trägt " +
                    "keine Gliederungsebene. URSACHE: Die Vorlage wurde ohne diese Formatvorlagen angelegt, oder eine " +
                    "Formatvorlage wurde umbenannt. Ohne Gliederungsebene bleibt das Inhaltsverzeichnis leer. Eine " +
                    "fehlende Formatvorlage legt EPOS-Plan beim Erstellen an und nennt das. ABHILFE: In Word die " +
                    "eingebauten Formatvorlagen „Überschrift 1“ bis „Überschrift 3“ verwenden und nach Wunsch " +
                    "gestalten, ohne ihre Gliederungsebene zu ändern. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_KLAMMER_OFFEN,
                    "Meldung VF_PRUEF_KLAMMER_OFFEN: Platzhalter nicht erkannt",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Im Text steht eine öffnende doppelte Klammer ohne passende schließende im selben " +
                    "Absatz. EPOS-Plan erkennt dort keinen Platzhalter; der Text bleibt im Bericht stehen. URSACHE: " +
                    "Der Platzhalter ist durch einen Tabulator, einen Zeilenumbruch, ein Feld oder ein Bild getrennt, " +
                    "oder eine Klammer fehlt. ABHILFE: Den Platzhalter in einem Zug neu tippen — ohne Tabulator oder " +
                    "Umbruch dazwischen — oder aus dem Platzhalterkatalog einfügen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_MARKE_UNBEKANNT,
                    "Meldung VF_PRUEF_MARKE_UNBEKANNT: Unbekannte Marke in doppelten Klammern",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Zwischen doppelten geschweiften Klammern steht etwas, das weder ein Platzhalter noch " +
                    "eine Blockmarke ist, etwa ein Satz oder ein Zeichen, das in Schlüsseln nicht vorkommt. Die " +
                    "Stelle bleibt im Bericht gelb markiert stehen. URSACHE: Doppelte geschweifte Klammern sind in " +
                    "einer Berichtsvorlage den Platzhaltern vorbehalten. ABHILFE: Einen Platzhalter aus dem " +
                    "Platzhalterkatalog einsetzen oder die doppelten Klammern entfernen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_UNBEKANNT,
                    "Meldung VF_PRUEF_UNBEKANNT: Unbekannter Platzhalter",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Den Schlüssel des Platzhalters kennt der Platzhalterkatalog dieser Programmfassung " +
                    "nicht. Er bleibt im Bericht gelb markiert stehen und wird in der Laufmeldung genannt. URSACHE: " +
                    "Ein Tippfehler, ein Schlüssel einer späteren Programmfassung oder ein frei erfundener Name. " +
                    "ABHILFE: Den Vorschlag der Prüfung übernehmen — er nennt den ähnlichsten bekannten Schlüssel — " +
                    "oder den Platzhalter aus dem Platzhalterkatalog kopieren. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_NORMALFORM,
                    "Meldung VF_PRUEF_NORMALFORM: Platzhalter in abweichender Schreibweise",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Der Platzhalter ist erkannt und wird gefüllt, steht aber nicht in seiner Normalform, " +
                    "etwa mit Leerzeichen oder Großbuchstaben. URSACHE: Die Schreibweise ist erlaubt, weicht aber von " +
                    "der Form ab, die der Platzhalterkatalog nennt. ABHILFE: Nichts zwingend; bei Gelegenheit die " +
                    "genannte Normalform übernehmen, damit die Vorlage einheitlich bleibt. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_KONTEXT_STAND,
                    "Meldung VF_PRUEF_KONTEXT_STAND: Wert je Variante außerhalb eines Blocks je Variante",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Der Platzhalter ist ein Wert je Variante (stand.*), steht aber außerhalb eines " +
                    "Wiederholblocks {{#je stand}} oder {{#je variante}}; er bliebe gelb stehen. URSACHE: Ein Wert " +
                    "je Variante braucht die Variante, für die er gilt — die liefert erst der Block; außerhalb gelten " +
                    "nur {{stand.a…}} und {{stand.b…}} des Paarvergleichs. ABHILFE: Den Platzhalter zwischen " +
                    "{{#je stand}} und {{/je}} setzen oder einen Wert des Stammprojekts verwenden (stamm.*, " +
                    "projekt.*). WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_KONTEXT_GEBAEUDE,
                    "Meldung VF_PRUEF_KONTEXT_GEBAEUDE: Wert je Gebäude außerhalb eines Blocks je Gebäude",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Der Platzhalter ist ein Wert je Gebäude (gebaeude.*), steht aber außerhalb eines " +
                    "Wiederholblocks {{#je gebaeude}}; er bliebe gelb stehen. URSACHE: Ein Wert je Gebäude braucht " +
                    "das Gebäude, für das er gilt — das liefert erst der Block. ABHILFE: Den Platzhalter zwischen " +
                    "{{#je gebaeude}} und {{/je}} setzen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_ORT,
                    "Meldung VF_PRUEF_ORT: Platzhalter an einer Stelle, an der er nicht stehen kann",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Art des Platzhalters passt nicht an seinen Ort, etwa ein Kapitel oder eine Liste " +
                    "im Satz, in einer Tabellenzelle, in Kopf- oder Fußzeile, in einem Textfeld oder einer Fußnote. " +
                    "Er bliebe gelb stehen. URSACHE: Kapitel und Listen ersetzen ganze Absätze; sie stehen deshalb " +
                    "allein in einem Absatz des Haupttexts. Text, Zahl und Datum dürfen fast überall stehen. ABHILFE: " +
                    "Den Platzhalter wie in der Meldung beschrieben verschieben, meist allein in einen eigenen Absatz " +
                    "des Haupttexts. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_ANGABE_UNBEKANNT,
                    "Meldung VF_PRUEF_ANGABE_UNBEKANNT: Unbekannte Formatangabe",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Hinter dem senkrechten Strich eines Platzhalters steht eine Angabe, die EPOS-Plan " +
                    "nicht kennt, etwa ein Tippfehler in einer Stellenzahl. URSACHE: Formatangaben sind ein fester " +
                    "Wortschatz; andere Wörter versteht die Engine nicht. ABHILFE: Den Vorschlag der Prüfung " +
                    "übernehmen oder eine der genannten Angaben wählen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_ANGABE_UNPASSEND,
                    "Meldung VF_PRUEF_ANGABE_UNPASSEND: Formatangabe passt nicht zur Art des Platzhalters",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Formatangabe ist bekannt, gilt aber nicht für diese Art von Platzhalter, etwa " +
                    "Nachkommastellen bei einem Text. URSACHE: Jede Art — Text, Zahl, Datum, Liste — hat eigene " +
                    "Formatangaben. ABHILFE: Die Angabe entfernen oder eine der genannten, für diese Art gültigen " +
                    "Angaben verwenden. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_BEREICH,
                    "Meldung VF_PRUEF_BLOCK_BEREICH: Unbekannter Wiederholbereich",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Ein Wiederholblock nennt einen Bereich, über den EPOS-Plan nicht wiederholen kann. " +
                    "URSACHE: Wiederholt wird nur über feste Bereiche; die Meldung nennt die erlaubten. ABHILFE: " +
                    "Einen der genannten Bereiche einsetzen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT,
                    "Meldung VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT: Blockmarke als Inhaltssteuerelement wird nicht ausgewertet",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Ein Inhaltssteuerelement trägt eine Blockmarke als Tag, das EPOS-Plan nicht als " +
                    "Wiederhol- oder Bedingungsabschnitt auswerten kann; es bliebe stehen. URSACHE: Ein " +
                    "Block-Steuerelement begrenzt seinen Block selbst und braucht kein Ende; es liegt um ganze " +
                    "Absätze, Tabellen oder Tabellenzeilen, nicht im Satz und nicht um eine einzelne Zelle. ABHILFE: " +
                    "Als Tag den Blockanfang eintragen (etwa #je stand) und das Steuerelement um ganze Absätze oder " +
                    "Zeilen legen, oder getippte Marken {{#je stand}} … {{/je}} verwenden. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_PAARSICHT,
                    "Meldung VF_PRUEF_PAARSICHT: Vorlage nutzt den Paarvergleich, gewählt ist Sicht 1",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage zeigt Werte des Paarvergleichs (stand.a.*, stand.b.*), gewählt sind aber " +
                    "die Sicht 1 und mehr als eine Variante. URSACHE: Der Paarvergleich stellt genau zwei Stände " +
                    "gegenüber; in Sicht 1 ist das nur eindeutig, wenn genau eine Variante gewählt ist. ABHILFE: In " +
                    "der Ergebnisansicht die Paarsicht (Sicht 2) wählen oder genau eine Variante auswählen; für alle " +
                    "Varianten einen Block {{#je variante}} verwenden. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_MUSTER_OHNE_ROLLEN,
                    "Meldung VF_PRUEF_MUSTER_OHNE_ROLLEN: Mustertabelle ohne erkennbare Rolle",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Eine Tabelle trägt den Alternativtext {{muster.tabelle}}, nennt aber in keiner Zelle eine " +
                    "Rolle. URSACHE: Aus der Mustertabelle liest EPOS-Plan das Format der Rollen Stamm, Gruppe, Summe und " +
                    "Warnung — je Rolle eine Zelle, deren Text die Rolle nennt; ohne sie gilt für die Strukturtabellen die " +
                    "Direktformatierung. ABHILFE: In je eine Zelle „Stamm“, „Gruppe“, „Summe“ bzw. „Warnung“ schreiben und " +
                    "Schattierung und Zeichenformat der Zelle setzen; die Tabelle wird beim Füllen entfernt. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_WENN_OHNE_SCHALTER,
                    "Meldung VF_PRUEF_WENN_OHNE_SCHALTER: Bedingung ohne Schalter",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Eine Bedingung nennt keinen Schalter, nach dem sie entscheidet. URSACHE: Hinter " +
                    "„#wenn“ fehlt der Schlüssel. ABHILFE: Einen Schalter (ja/nein) aus dem Platzhalterkatalog als " +
                    "Bedingung einsetzen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_WENN_KEIN_SCHALTER,
                    "Meldung VF_PRUEF_WENN_KEIN_SCHALTER: Bedingung mit einem Wert statt eines Schalters",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Bedingung nennt einen Platzhalter, der kein Schalter (ja/nein) ist. URSACHE: Nur " +
                    "Schalter können eine Bedingung entscheiden, Texte, Zahlen oder Daten nicht. ABHILFE: Einen " +
                    "Schalter aus dem Platzhalterkatalog als Bedingung einsetzen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_ALLEIN,
                    "Meldung VF_PRUEF_BLOCK_ALLEIN: Blockmarke im Satz",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Eine Blockmarke — Anfang oder Ende eines Wiederhol- oder Bedingungsblocks — steht " +
                    "mitten in einem Satz. URSACHE: Blöcke wiederholen oder entfernen ganze Absätze oder " +
                    "Tabellenzeilen; ihre Marken stehen deshalb allein im Absatz oder in der ersten bzw. letzten " +
                    "Zelle einer Tabellenzeile. ABHILFE: Die Marke in einen eigenen Absatz setzen oder in die erste " +
                    "bzw. letzte Zelle der Tabellenzeile. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_TIEFE,
                    "Meldung VF_PRUEF_BLOCK_TIEFE: Block in der dritten Ebene",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Blöcke sind tiefer als zwei Ebenen ineinander verschachtelt. URSACHE: Erlaubt sind " +
                    "zwei Ebenen, etwa eine Bedingung innerhalb einer Wiederholung. ABHILFE: Die Verschachtelung auf " +
                    "zwei Ebenen verringern. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_ENDE,
                    "Meldung VF_PRUEF_BLOCK_ENDE: Blockende ohne Anfang",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Eine Endmarke hat keinen passenden Blockanfang davor. URSACHE: Der Anfang fehlt, " +
                    "steht in einem anderen Teil der Vorlage (etwa der Kopfzeile) oder ist falsch geschrieben. " +
                    "ABHILFE: Den Blockanfang ergänzen oder die Endmarke entfernen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_OFFEN,
                    "Meldung VF_PRUEF_BLOCK_OFFEN: Block wird nicht geschlossen",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Ein Blockanfang hat kein passendes Ende. URSACHE: Die Endmarke fehlt oder steht in " +
                    "einem anderen Teil der Vorlage. ABHILFE: Die in der Meldung genannte Endmarke an das Ende des " +
                    "Blocks setzen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_VERBUNDEN,
                    "Meldung VF_PRUEF_BLOCK_VERBUNDEN: Verbundene Zellen in der Wiederholzeile",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Tabellenzeile, die ein Block wiederholen soll, enthält verbundene Zellen. " +
                    "URSACHE: Eine Zeile mit Zellverbund lässt sich nicht sauber vervielfachen. ABHILFE: Den " +
                    "Zellverbund in dieser Zeile aufheben (in Word: Zellen teilen). WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_BLOCK_TABELLE,
                    "Meldung VF_PRUEF_BLOCK_TABELLE: Block reicht über eine Tabellengrenze",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Anfang und Ende eines Blocks liegen in verschiedenen Tabellen oder Zeilen, oder einer " +
                    "liegt innerhalb und einer außerhalb einer Tabelle. URSACHE: Ein Block wiederholt entweder " +
                    "Absätze oder genau eine Tabellenzeile. ABHILFE: Anfang und Ende in die erste und letzte Zelle " +
                    "derselben Tabellenzeile setzen oder beide außerhalb der Tabelle. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_DATUMSFELD,
                    "Meldung VF_PRUEF_DATUMSFELD: Datumsfeld zeigt das Datum des Öffnens",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage enthält ein Word-Feld DATE oder TIME. Es zeigt das Datum, an dem der " +
                    "Bericht geöffnet oder gedruckt wird, nicht das Datum seiner Erstellung. URSACHE: Word " +
                    "aktualisiert solche Felder bei jedem Öffnen oder Drucken. ABHILFE: Statt des Felds den " +
                    "Platzhalter {{bericht.datum}} verwenden. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_KOMMENTARE,
                    "Meldung VF_PRUEF_KOMMENTARE: Kommentare der Vorlage",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage enthält Kommentare. Sie werden beim Erstellen des Berichts vollständig " +
                    "entfernt; Platzhalter in Kommentaren werden weder geprüft noch gefüllt. URSACHE: Kommentare sind " +
                    "Notizen für den Autor der Vorlage und gehören nicht in den Bericht. ABHILFE: Nichts zu tun. " +
                    "WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_OHNE_PLATZHALTER,
                    "Meldung VF_PRUEF_OHNE_PLATZHALTER: Vorlage ohne Platzhalter",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage enthält keinen einzigen Platzhalter. EPOS-Plan setzt den Bericht dann mit " +
                    "{{bericht.inhalt}} an ihr Ende; nichts in der Vorlage wird gelöscht. URSACHE: Eine gewöhnliche " +
                    "Word-Datei, etwa ein Briefbogen, wurde als Vorlage gewählt. ABHILFE: {{bericht.inhalt}} an die " +
                    "Stelle setzen, an der der Bericht stehen soll, und nach Wunsch weitere Platzhalter für " +
                    "Deckblatt, Kopf- und Fußzeile. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_SPRACHE,
                    "Meldung VF_PRUEF_SPRACHE: Die Sprache der Vorlage weicht ab",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: In den Dokumenteigenschaften der Vorlage ist eine andere Sprache eingetragen als die, " +
                    "in der der Bericht entsteht. Feste Texte der Vorlage stünden dann in der anderen Sprache. " +
                    "URSACHE: Die Vorlage wurde für die andere Sprache angelegt, oder die Oberflächensprache wurde " +
                    "gewechselt. ABHILFE: Die Oberflächensprache wechseln, eine Vorlage der passenden Sprache wählen " +
                    "oder die Sprache der Vorlage anpassen. Vor dem Start fragt EPOS-Plan nach. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_FASSUNG_ALT,
                    "Meldung VF_PRUEF_FASSUNG_ALT: Vorlage aus einer älteren Katalogfassung",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage wurde für eine ältere Fassung des Platzhalterkatalogs angelegt; ein " +
                    "genutzter Schlüssel heißt inzwischen anders. Der alte Name wird weiter gefüllt. URSACHE: Der " +
                    "Katalog wächst mit den Programmfassungen; umbenannte Schlüssel bleiben als Alias gültig. " +
                    "ABHILFE: Bei Gelegenheit den neuen Namen übernehmen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_KAPITEL_NEU,
                    "Meldung VF_PRUEF_KAPITEL_NEU: Neues Kapitel, in der Vorlage nicht enthalten",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Seit der Katalogfassung der Vorlage gibt es ein neues Kapitel, das die Vorlage nicht " +
                    "einsetzt. URSACHE: Die Vorlage bestimmt Umfang und Reihenfolge des Berichts; neue Kapitel kommen " +
                    "nicht von selbst hinzu. ABHILFE: Bei Bedarf den genannten Kapitelplatzhalter an die gewünschte " +
                    "Stelle setzen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_FASSUNG_NEU,
                    "Meldung VF_PRUEF_FASSUNG_NEU: Vorlage aus einer neueren Katalogfassung",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage wurde mit einer neueren Programmfassung angelegt und nutzt Schlüssel, die " +
                    "diese Fassung nicht kennt; sie blieben gelb stehen. URSACHE: Die Vorlage stammt von einem " +
                    "Rechner mit einer neueren EPOS-Plan-Fassung. ABHILFE: EPOS-Plan aktualisieren oder die " +
                    "unbekannten Platzhalter entfernen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_GUELTIGKEIT,
                    "Meldung VF_PRUEF_GUELTIGKEIT: Gültigkeitshinweise fehlen",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage zeigt Werte der Wirtschaftlichkeit, aber keine Warnliste. Hinweise wie " +
                    "ein veraltetes Ergebnis oder fehlende Preise erschienen dann nicht neben den Zahlen. URSACHE: " +
                    "Der Standardbericht nennt die Gültigkeitshinweise im Kapitel; eine eigene Vorlage mit " +
                    "Einzelwerten muss sie selbst aufnehmen. ABHILFE: Eine Warnliste ergänzen, etwa " +
                    "{{bericht.warnungen}}. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_KAPITEL_DOPPELT,
                    "Meldung VF_PRUEF_KAPITEL_DOPPELT: Ein Kapitel steht mehrfach in der Vorlage",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Derselbe Kapitelplatzhalter (etwa {{kapitel.projekt}}) steht an mehr als einer " +
                    "Stelle. Gefüllt wird nur die erste; jede weitere bleibt im Bericht gelb markiert stehen. " +
                    "URSACHE: Ein Kapitel wurde kopiert oder zusätzlich zum Sammelanker noch einmal eingefügt. " +
                    "ABHILFE: Die weitere Stelle entfernen; die Reihenfolge der Kapitel bestimmt die Vorlage über " +
                    "die Lage der ersten Stellen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_SPAETER,
                    "Meldung VF_PRUEF_SPAETER: Der Platzhalter wirkt erst in einer späteren Programmfassung",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Der Katalog kennt den Platzhalter, diese Fassung füllt ihn an dieser Stelle aber " +
                    "noch nicht; er bliebe im Bericht gelb markiert stehen. URSACHE: Ein Bild füllt EPOS-Plan nur, " +
                    "wenn sein Schlüssel im Alternativtext eines Bildes steht, nicht als getippter Text. ABHILFE: " +
                    "Für das Logo ein Bild einfügen und {{bild.ersteller.logo}} als Alternativtext eintragen. WIKI: " +
                    "Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.VF_PRUEF_ANHANG_E_STELLE,
                    "Meldung VF_PRUEF_ANHANG_E_STELLE: Anhang E ohne Stelle für ein Kapitel",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage führt die Checkliste nach DIN EN 17463, Anhang E, aber nicht jedes " +
                    "Kapitel, auf das ihre Spalte „Stelle im Bericht“ verweist; dort steht dann „nicht im Bericht“. " +
                    "URSACHE: Die Vorlage lässt ein Kapitel weg, etwa die Projektbeschreibung oder den Anhang. " +
                    "ABHILFE: Das genannte Kapitel mit seinem Platzhalter aufnehmen, wenn der Bewertungsbericht es " +
                    "zeigen soll, oder die Checkliste so lassen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_VORLAGEN_NICHT_LESBAR,
                    "Meldung BV_VORLAGEN_NICHT_LESBAR: Die Vorlagendatei kann nicht gelesen werden",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die gewählte Vorlagendatei ließ sich nicht öffnen; die Meldung nennt den Grund. " +
                    "URSACHE: Die Datei ist gesperrt, liegt auf einem nicht erreichbaren Laufwerk oder in einem nur " +
                    "online verfügbaren Cloud-Ordner, ist größer als 20 MB, oder es fehlen Leserechte. ABHILFE: " +
                    "Laufwerk oder Cloud-Ordner verfügbar machen, die Datei lokal vorhalten, Rechte prüfen und erneut " +
                    "prüfen. Bis dahin den Bericht mit der Standardvorlage erstellen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_VORLAGEN_FEHLT,
                    "Meldung BV_VORLAGEN_FEHLT: Die Vorlage ist nicht vorhanden",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die gewählte Vorlagendatei gibt es am erwarteten Ort nicht mehr. Der Bericht nimmt " +
                    "dann die Vorgabe bzw. die Standardvorlage und nennt das. URSACHE: Die Datei wurde umbenannt, " +
                    "verschoben oder gelöscht, oder der Vorlagenordner wurde gewechselt. ABHILFE: Die Vorlage erneut " +
                    "hinzufügen („Hinzufügen…“) oder eine andere Vorlage wählen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_VORLAGEN_IN_WORD,
                    "Meldung BV_VORLAGEN_IN_WORD: Die Vorlage ist in Word geöffnet",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Neben der Vorlage liegt die Sperrdatei von Word; die Vorlage ist gerade in Word " +
                    "geöffnet. Geprüft und gefüllt wird der zuletzt gespeicherte Stand — ungespeicherte Änderungen " +
                    "fehlen. URSACHE: Die Vorlage wird gerade bearbeitet. ABHILFE: In Word speichern und erneut " +
                    "prüfen. Beim Erstellen füllt EPOS-Plan genau den Stand, den es vor dem Start gelesen hat. WIKI: " +
                    "Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_LAUF_VORLAGE,
                    "Meldung BV_LAUF_VORLAGE: Welche Word-Vorlage der Bericht nimmt",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Laufmeldung nennt die Vorlage, aus der der Word-Bericht entstand, und warum sie " +
                    "gewählt wurde. URSACHE: Die Wahl folgt einer festen Reihenfolge: die für dieses Stammprojekt " +
                    "gewählte Vorlage, sonst die Vorgabe der Installation, sonst die Standardvorlage (EPOS-Plan). " +
                    "Fehlt die Standardvorlage selbst, entsteht der Bericht mit den eingebauten Formaten. ABHILFE: " +
                    "Eine andere Vorlage in der Gruppe „Vorlage“ der Berichtsseite wählen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_LAUF_RUECKFALL,
                    "Meldung BV_LAUF_RUECKFALL: Rückfall bei der Vorlagenwahl",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die gewünschte Vorlage konnte nicht verwendet werden; EPOS-Plan hat eine andere " +
                    "genommen, und der Bericht ist trotzdem entstanden. URSACHE: Die gespeicherte Vorlage fehlt, ließ " +
                    "sich nicht lesen oder füllen, wurde in der Rückfrage für diesen Bericht durch die " +
                    "Standardvorlage ersetzt — oder die Standardvorlage selbst fehlt in der Installation. ABHILFE: " +
                    "Den genannten Grund beheben, etwa die Vorlage erneut hinzufügen oder in Word als .docx " +
                    "speichern. Fehlt die Standardvorlage, EPOS-Plan neu installieren. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_LAUF_UNBEKANNT,
                    "Meldung BV_LAUF_UNBEKANNT: Nicht ersetzte Platzhalter, gelb markiert",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Einige Platzhalter der Vorlage konnte EPOS-Plan nicht füllen; sie stehen im Bericht " +
                    "gelb markiert. Die Laufmeldung nennt je Stelle Platzhalter, Grund und Fundort. URSACHE: Ein " +
                    "unbekannter Schlüssel, ein Platzhalter an einer Stelle, an der er nicht stehen kann, eine " +
                    "Blockmarke ohne Gegenstück oder ein Wert je Variante außerhalb seines Blocks. ABHILFE: Die Vorlage prüfen, die " +
                    "genannten Stellen korrigieren und den Bericht neu erstellen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_LAUF_LEER,
                    "Meldung BV_LAUF_LEER: Platzhalter ohne Wert",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Für einige Platzhalter gab es im Lauf keinen Wert; an ihrer Stelle steht der Leerwert " +
                    "— bei Zahlen und Daten ein Strich, nie 0. Die Laufmeldung fasst je Platzhalter zusammen, an wie " +
                    "vielen Stellen er leer blieb. URSACHE: Eine Angabe ist im Projekt nicht gepflegt, etwa der " +
                    "Kunde, eine Kennzahl entsteht in diesem Projekt nicht, etwa eine Jahresarbeitszahl ohne " +
                    "Wärmepumpe, oder eine Rechnung lieferte kein Ergebnis. ABHILFE: Die Angabe im Projekt pflegen " +
                    "oder den Platzhalter aus der Vorlage nehmen. Mit der Formatangabe „|mit grund“ nennt der Bericht " +
                    "den Grund neben dem Strich. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_LAUF_KOMMENTARE,
                    "Meldung BV_LAUF_KOMMENTARE: Kommentare der Vorlage entfernt",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage enthielt Kommentare; der Bericht enthält keinen davon. URSACHE: " +
                    "Kommentare sind Notizen für den Autor der Vorlage und gehören nicht in den Bericht. ABHILFE: " +
                    "Nichts zu tun — die Vorlage selbst bleibt unverändert. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_LAUF_WARNUNGEN,
                    "Meldung BV_LAUF_WARNUNGEN: Warnungen beim Füllen der Vorlage",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Beim Füllen fiel etwas auf, das den Bericht anders aussehen lässt, als die Vorlage " +
                    "erwarten lässt: ein Kapitel oder eine Liste an einer unpassenden Stelle, eine Vorlage ohne " +
                    "Platzhalter, eine entfernte Verknüpfung oder ein Wert, der beim Auflösen scheiterte und durch " +
                    "einen Strich ersetzt wurde. URSACHE: Die Vorlage enthält Stellen, die die Vorprüfung als Fehler " +
                    "oder Warnung nennt, oder eine Quelle lieferte beim Auflösen keinen Wert. ABHILFE: Die Vorlage an " +
                    "den genannten Stellen korrigieren und erneut prüfen. WIKI: Programm " +
                    "Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_START_SICHT,
                    "Meldung BV_START_SICHT: Die Vorlage nutzt den Paarvergleich, gewählt ist Sicht 1",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Die Vorlage verwendet Werte des Paarvergleichs (stand.a, stand.b), die " +
                    "Ergebnisansicht steht aber auf Sicht 1, Stamm gegen jede Variante. URSACHE: Der Paarvergleich " +
                    "gilt nur in Sicht 2; in Sicht 1 ist stand.b allein zulässig, wenn genau eine Variante gewählt " +
                    "ist. ABHILFE: In der Ergebnisansicht die Vergleichssicht 2 wählen oder eine Vorlage ohne " +
                    "Paarvergleich nehmen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT),

                new WissensAbschnitt(KiMeldungskennung.BV_START_OHNE_WIRTSCHAFT,
                    "Meldung BV_START_OHNE_WIRTSCHAFT: Vorlage ohne Wirtschaftlichkeit",
                    KiChatKontext.B_BERICHT,
                    "BEDEUTUNG: Der Bericht wurde von der Seite der Wirtschaftlichkeit aus angestoßen, die gewählte " +
                    "Vorlage enthält aber keinen Platzhalter der Wirtschaftlichkeit — der Bericht zeigte sie nicht. " +
                    "URSACHE: Die Vorlage setzt weder {{bericht.inhalt}} noch einen Wert oder ein Kapitel der " +
                    "Wirtschaftlichkeit ein. ABHILFE: Für diesen Bericht die angebotene Standardvorlage nehmen oder " +
                    "die Vorlage um die Wirtschaftlichkeit ergänzen. WIKI: Programm Dokumentation/Bericht#vorlage.",
                    WIKI_BERICHT)
            };
        }
    }
}
