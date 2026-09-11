// WAS EIN DIALOG AUSSER SEINEN FELDERN BEISTEUERT (Auftrag #201, Stufe S3).
//
// Mit S2 meldete ein Razor-Dialog eine Feldliste an - Getter und (unbenutzte) Setter.
// Zum SETZEN gehoert mehr, und zwar genau das, was ein Anwender an der Maske ohnehin
// bekommt:
//
//   * Der neue Wert muss SICHTBAR werden (StateHasChanged) - sonst steht in der Maske
//     die alte Zahl, waehrend der Assistent "gesetzt" meldet.
//   * Der Dialog prueft seine Eingaben SELBST (Plausibilitaet, Pflicht, Einheit). Der
//     Assistent ersetzt diese Pruefung nicht, er loest sie aus (Umsetzungskonzept 3b,
//     Punkt 4).
//   * Ein Katalogsatz kann SCHREIBGESCHUETZT sein (ReadOnly der _STAMM-Tabellen). Das
//     weiss der Dialog, nicht der Kern.
//   * Speichern und Rechnen sind Wege der Maske, keine Aktionen des Kerns - der Kern
//     kennt weder den Speicherknopf des Heizkesseleditors noch den Rechenweg der
//     Stromspeicher-Ansicht.
//
// WARUM DELEGATEN UND KEINE SCHNITTSTELLE. Ein Razor-Dialog ist eine ComponentBase; eine
// Schnittstelle zwaenge jeden Dialog, sie zu erben oder einen Adapter danebenzustellen.
// Delegaten sind das Hausmuster der Oberflaeche (StromspeicherAuslegungDaten fuehrt
// siebzehn davon), und sie erlauben, genau das anzumelden, was dieser Dialog kann - und
// nichts anzumelden, was er nicht kann.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Rechenweg einer offenen Maske: er laeuft LANG, meldet Fortschritt und laesst
    /// sich abbrechen (Fachkonzept 5.3).
    /// </summary>
    /// <param name="umgebung">Fortschritt und Abbruchmarke.</param>
    /// <returns>Das Ergebnis in der Sprache des Assistenten.</returns>
    public delegate Task<KiErgebnis> KiRechenweg(KiLaufumgebung umgebung);

    /// <summary>
    /// Was ein angemeldeter Dialog ueber seine Felder hinaus beisteuert (Auftrag #201).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Alles ist freiwillig.</b> Ein Dialog, der nur gelesen werden soll, meldet gar
    /// keinen Haken an — dann lehnt die jeweilige Aktion benannt ab („Diese Maske bietet
    /// keinen Speicherweg an"), statt still nichts zu tun. Das ist der Unterschied, der
    /// den Anwender weiterbringt.
    /// </para>
    /// <para>
    /// <b>Kein Zustand.</b> Diese Klasse haelt Delegaten und sonst nichts; der Zustand
    /// liegt im Dialog, wohin er gehoert.
    /// </para>
    /// </remarks>
    public sealed class KiMaskenhaken
    {
        /// <summary>Der leere Satz — kein Auffrischen, keine Pruefung, kein Speichern.</summary>
        public static readonly KiMaskenhaken Leer = new KiMaskenhaken();

        private readonly Dictionary<string, KiRechenweg> _rechenwege =
            new Dictionary<string, KiRechenweg>(StringComparer.Ordinal);

        /// <summary>
        /// Zeichnet die Maske neu, nachdem der Assistent ein Feld gesetzt hat
        /// (<c>StateHasChanged</c>). <c>null</c> = der Dialog zeichnet von selbst.
        /// </summary>
        /// <remarks>
        /// Ohne diesen Haken bliebe die alte Zahl stehen: Der Setzer schreibt in das
        /// Daten-Objekt, aber Blazor zeichnet nur nach einem Ereignis DES BROWSERS — und
        /// eine Assistentenaktion ist keines.
        /// </remarks>
        public Action Auffrischen { get; set; }

        /// <summary>
        /// Die Plausibilitaetspruefung des Dialogs — dieselbe, die sein Speicherknopf
        /// ruft. Rueckgabe: Klartextbefund, oder <c>null</c>/leer, wenn alles steht.
        /// </summary>
        /// <remarks>
        /// <b>Sie laeuft NACH dem Setzen</b> (Konzept „Der Hilfe-Assistent im Dialog",
        /// 3.4): Der Assistent traegt den Wert ein wie eine Hand und laesst dann den
        /// Dialog urteilen. Ein Befund macht das Setzen nicht rueckgaengig — die Maske
        /// ist kein Transaktionsraum, und ein sichtbar falscher Wert mit sichtbarer
        /// Meldung ist fuer den Anwender besser als ein stilles Nichts.
        /// </remarks>
        public Func<string> Pruefen { get; set; }

        /// <summary>
        /// Ist der gerade bearbeitete Satz schreibgeschuetzt (<c>ReadOnly</c> der
        /// <c>_STAMM</c>-Tabellen, Fachkonzept 4.5)? <c>null</c> = nein.
        /// </summary>
        public Func<bool> Schreibgeschuetzt { get; set; }

        /// <summary>
        /// Der Speicherweg des Dialogs — sein OK-/Speichern-Knopf (Anwenderentscheid
        /// KI‑D‑Q4). <c>null</c> = diese Maske speichert nicht ueber den Assistenten.
        /// </summary>
        /// <remarks>
        /// <b>Der Sicherungspunkt steht davor, nicht hier</b>: Er gehoert zur AKTION
        /// (<c>dialog_speichern</c> ist datenbankwirksam), und der Ausfuehrer legt ihn an,
        /// bevor er ueberhaupt fragt (Fachkonzept 4.4, Punkt 1).
        /// </remarks>
        public Func<Task<KiErgebnis>> Speichern { get; set; }

        /// <summary>Die Namen der angemeldeten Rechenwege.</summary>
        public IReadOnlyCollection<string> Rechenwegnamen => _rechenwege.Keys;

        /// <summary>
        /// Meldet einen Rechenweg unter dem Namen der Aktion an, die ihn ruft
        /// (<c>peak_ziel_bestimmen</c>, <c>flotte_bewerten</c>).
        /// </summary>
        /// <remarks>
        /// <b>Der Schluessel ist der AKTIONSNAME und kein eigener Katalog.</b> Zwei
        /// Namensraeume fuer dieselbe Sache liefen auseinander; so faellt ein Tippfehler
        /// sofort auf — die Aktion findet ihren Weg nicht und lehnt benannt ab.
        /// </remarks>
        public KiMaskenhaken Rechenweg(string aktionsname, KiRechenweg weg)
        {
            if (string.IsNullOrWhiteSpace(aktionsname) || weg == null) return this;
            _rechenwege[aktionsname.Trim()] = weg;
            return this;
        }

        /// <summary>Der Rechenweg zu dieser Aktion; <c>null</c> = die Maske bietet ihn nicht an.</summary>
        public KiRechenweg FindeRechenweg(string aktionsname)
        {
            if (string.IsNullOrWhiteSpace(aktionsname)) return null;
            KiRechenweg weg;
            return _rechenwege.TryGetValue(aktionsname.Trim(), out weg) ? weg : null;
        }

        /// <summary>
        /// Frischt die Maske auf; ein werfender Haken bleibt folgenlos.
        /// </summary>
        public void Auffrischung()
        {
            Action haken = Auffrischen;
            if (haken == null) return;
            try { haken(); }
            catch (Exception) { /* Ein Zeichenfehler darf das Setzen nicht kippen. */ }
        }

        /// <summary>
        /// Der Befund der Dialogpruefung; leer = kein Befund. Ein werfender Haken zaehlt
        /// als „kein Befund" — er ist die Zugabe, nicht die Zusage.
        /// </summary>
        public string Befund()
        {
            Func<string> haken = Pruefen;
            if (haken == null) return "";
            try { return haken() ?? ""; }
            catch (Exception) { return ""; }
        }

        /// <summary>Ist der bearbeitete Satz schreibgeschuetzt? Im Zweifel NEIN.</summary>
        public bool IstSchreibgeschuetzt()
        {
            Func<bool> haken = Schreibgeschuetzt;
            if (haken == null) return false;
            try { return haken(); }
            catch (Exception) { return false; }
        }
    }
}
