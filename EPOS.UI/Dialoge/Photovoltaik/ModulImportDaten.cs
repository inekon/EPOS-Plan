#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Photovoltaik
{
    /// <summary>
    /// Was das Laden EINER Quelle ergeben hat — die gelesenen Sätze und, im Fehlerfall,
    /// der Meldungsschlüssel des Dienstes.
    /// </summary>
    /// <remarks>
    /// <para>Erbe von <c>PvLeseErgebnis</c> und <c>WrLeseErgebnis</c>, die bis W6‑O‑1
    /// nebeneinanderstanden und sich nur im Satztyp unterschieden. <b>Der Satztyp ist
    /// hier <see cref="object"/></b> — die Zeilenform baut das Profil im Kern
    /// (<see cref="ModulImportProfil.Zeile"/>), und der Dialog kennt weder
    /// <c>UnifiedModule</c> noch <c>CecWechselrichter</c> noch
    /// <c>OndWechselrichter</c>. Genau das macht den EINEN Wirt möglich.</para>
    /// </remarks>
    public sealed class ImportLeseErgebnis
    {
        public ImportLeseErgebnis(bool erfolgreich, IReadOnlyList<object>? saetze,
                                  CecFortschritt meldung)
        {
            Erfolgreich = erfolgreich;
            Saetze = saetze ?? Array.Empty<object>();
            Meldung = meldung;
        }

        /// <summary>Steht die Liste?</summary>
        public bool Erfolgreich { get; }

        /// <summary>Die Sätze der gewählten Quelle, in der Reihenfolge der Quelle.</summary>
        public IReadOnlyList<object> Saetze { get; }

        /// <summary>Der Schlüssel der Rückmeldung samt Platzhalterwerten.</summary>
        public CecFortschritt Meldung { get; }
    }

    /// <summary>
    /// Das Ergebnis der Vorprüfung EINES Satzes.
    /// </summary>
    /// <remarks>
    /// <para>Hieß bis W6‑O‑1 <c>PvVorpruefung</c> und lag in <c>PvModulImportDaten.cs</c>;
    /// der Name war schon damals zu eng — der Wechselrichterimport hat sie unverändert
    /// mitbenutzt, weil sie KEINEN Satztyp kennt. Mit dem einen Wirt heißt sie, was sie
    /// ist.</para>
    /// <para><b>Ein Befund und keine Liste:</b> Beide Importe wählen genau EINEN Satz
    /// (der Vorläufer setzte <c>MultiSelect = false</c>) und schreiben ihn einzeln.</para>
    /// </remarks>
    public sealed class ImportVorpruefung
    {
        public ImportVorpruefung(ImportBefund befund, IReadOnlyList<ImportPruefung>? pruefungen,
                                 IReadOnlyCollection<string>? vergebeneNamen,
                                 string plausibilitaet = "", bool gesperrt = false)
        {
            Befund = befund;
            Pruefungen = pruefungen ?? Array.Empty<ImportPruefung>();
            VergebeneNamen = vergebeneNamen ?? Array.Empty<string>();
            Plausibilitaet = plausibilitaet ?? "";
            Gesperrt = gesperrt;
        }

        /// <summary>Der Befund des einen Kandidaten.</summary>
        public ImportBefund Befund { get; }

        /// <summary>Die Prüfliste für den Konfliktdialog (genau ein Eintrag).</summary>
        public IReadOnlyList<ImportPruefung> Pruefungen { get; }

        /// <summary>Die normalisierten Bestandsnamen — für die Namensvalidierung.</summary>
        public IReadOnlyCollection<string> VergebeneNamen { get; }

        /// <summary>
        /// Befund der Plausibilitätsprüfung (<c>PvModulPlausibilitaet</c> bzw.
        /// <c>WechselrichterPlausibilitaet</c>): leer = nichts zu bemerken; sonst der
        /// fertige Meldungstext. Mit <see cref="Gesperrt"/> ist es ein Fehler, der die
        /// Übernahme verhindert, sonst eine Warnung, die der Dialog zurückfragt.
        /// </summary>
        public string Plausibilitaet { get; }

        /// <summary>Sperrt der Befund das Schreiben?</summary>
        public bool Gesperrt { get; }
    }

    /// <summary>
    /// Der Satz Delegaten, mit dem die Hülle den Geräteimport an Netz, Dateisystem und
    /// Datenbank hängt.
    /// </summary>
    /// <remarks>
    /// Bauart wörtlich <c>ModulKatalogWege</c> (iU9‑W14a): EIN Parameter statt sieben,
    /// und <b>kein Delegat, kein Bedienelement</b> — eine Quelle des Profils erscheint
    /// nur, wenn der Weg dazu da ist.
    /// </remarks>
    public sealed class ModulImportWege
    {
        /// <summary>
        /// Holt eine Netzquelle (Schlüssel der <see cref="ImportQuelle"/>), mit
        /// Fortschrittsmelder und Abbruch.
        /// </summary>
        public Func<string, IProgress<CecFortschritt>, CancellationToken, Task<ImportLeseErgebnis>>? Netz { get; init; }

        /// <summary>
        /// Der Dateiwähler zu einer Dateiquelle; er bekommt Filter und Startordner aus
        /// dem Profil. Leerer Rückgabewert = abgebrochen.
        /// </summary>
        public Func<ImportQuelle, Task<string?>>? DateiWaehlen { get; init; }

        /// <summary>Liest die gewählte Datei einer Dateiquelle.</summary>
        public Func<ImportQuelle, string, Task<ImportLeseErgebnis>>? DateiLaden { get; init; }

        /// <summary>Vorprüfung des gewählten Satzes gegen den Katalog.</summary>
        public Func<object, Task<ImportVorpruefung>>? Vorpruefen { get; init; }

        /// <summary>Legt den Katalogsatz neu an; <c>false</c> = es blieb aus.</summary>
        public Func<object, string, Task<bool>>? Anlegen { get; init; }

        /// <summary>Aktualisiert den Bestandssatz mit dieser Id.</summary>
        public Func<object, int, Task<bool>>? Ueberschreiben { get; init; }

        /// <summary>Übersetzt einen Meldungsschlüssel eines Dienstes.</summary>
        public Func<CecFortschritt, string>? Meldungstext { get; init; }
    }

    /// <summary>
    /// Der Übersetzer, den <see cref="ModulImportProfil.Finde"/> braucht.
    /// </summary>
    /// <remarks>
    /// <para>Der Kern kennt keine Anzeigetexte — er führt Schlüssel. Anders als
    /// <c>Dialoge.Import.Texte</c> steht hier keine Fallunterscheidung mit siebzig
    /// Zweigen: Der Ressourcenkatalog wird direkt gefragt (wörtlich das Muster
    /// <c>PvModulImportHuelle.Text_</c>). Ein unbekannter Schlüssel bleibt stehen und
    /// ist damit in der Maske sofort sichtbar.</para>
    /// </remarks>
    public static class ImportTexte
    {
        /// <summary>Schlüssel → Text; ein unbekannter Schlüssel bleibt stehen.</summary>
        public static string Zu(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return "";

            string? text = null;
            try { text = Resource.ResourceManager.GetString(schluessel); }
            catch { /* ein fehlender Katalog darf keine Maske sprengen */ }

            return string.IsNullOrEmpty(text) ? schluessel : text!;
        }
    }
}
