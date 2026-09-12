using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Vom Anwender gewaehlte Aufloesung eines Import-Konflikts (Konzept 1.1).</summary>
    public enum KonfliktAktion
    {
        Importieren,
        Auslassen,
        Ueberschreiben,
        Umbenennen
    }

    /// <summary>Ergebniszeile des Konfliktdialogs: Pruefergebnis plus gewaehlte Aktion.</summary>
    public class KonfliktEntscheidung
    {
        public ImportPruefung Pruefung;
        public KonfliktAktion Aktion;
        /// <summary>Bei <see cref="KonfliktAktion.Umbenennen"/>: der neue Name (getrimmt).</summary>
        public string NeuerName;
    }

    /// <summary>
    /// Die Entscheidungsregeln des gemeinsamen Konfliktdialogs (Konzept 3.3 und 4.3) —
    /// oberflaechenfrei, damit sie Windows, Blazor und iOS gleich beantworten
    /// (iU9-W12.0b, Befund W12-B18).
    ///
    /// <para><b>Woher sie kommen.</b> Bis zu dieser Welle standen sie in
    /// <c>Views/Import/Form_ImportKonflikte.cs</c> — einer WinForms-Datei, die
    /// FUENF Importmasken benutzten. Solange <see cref="KonfliktAktion"/> und
    /// <see cref="KonfliktEntscheidung"/> dort liegen, zieht jede Razor-Komponente,
    /// die den Konfliktdialog braucht, eine WinForms-Abhaengigkeit nach
    /// <c>EPOS.UI</c> — und die Bibliothek uebersetzt dann gar nicht mehr
    /// (<c>EnableWindowsTargeting=false</c>).</para>
    ///
    /// <para><b>Befund W12-B19 mit erledigt.</b> Der Vorlaeufer bildete die gewaehlte
    /// Aktion aus dem ANZEIGETEXT der Zelle zurueck
    /// (<c>ZeilenAktion</c>: Vergleich mit <c>IMP_KONFLIKT_AKTION_*</c>). Ein
    /// Sprachwechsel zur Laufzeit — die Anwendung kann das — haette die Zuordnung
    /// zerrissen, und die Drei-Schichten-Regel verbietet es ohnehin: Kein Anzeigetext
    /// darf Steuerwert sein. Hier ist die Aktion ein WERT; <see cref="AktionText"/>
    /// liefert nur noch die Beschriftung dazu.</para>
    /// </summary>
    public static class ImportKonfliktModell
    {
        /// <summary>Beschriftung einer Aktion — Anzeige, nie Steuerwert (W12-B19).</summary>
        public static string AktionText(KonfliktAktion a)
        {
            switch (a)
            {
                case KonfliktAktion.Importieren: return MyResource.Resource.IMP_KONFLIKT_AKTION_IMPORTIEREN;
                case KonfliktAktion.Auslassen: return MyResource.Resource.IMP_KONFLIKT_AKTION_AUSLASSEN;
                case KonfliktAktion.Ueberschreiben: return MyResource.Resource.IMP_KONFLIKT_AKTION_UEBERSCHREIBEN;
                default: return MyResource.Resource.IMP_KONFLIKT_AKTION_UMBENENNEN;
            }
        }

        /// <summary>
        /// Erlaubte Aktionen und Vorbelegung je Befund (Konzept 3.3) — woertlich aus
        /// <c>Form_ImportKonflikte.ErlaubteAktionen</c>.
        /// </summary>
        public static List<KonfliktAktion> ErlaubteAktionen(ImportPruefung p, out KonfliktAktion vorbelegung)
        {
            List<KonfliktAktion> aktionen = new List<KonfliktAktion>();

            if (p.NameDoppeltInAuswahl && p.Befund == ImportBefund.Neu)
            {
                // Zwei markierte Eintraege mit demselben Namen: nur einer darf ihn tragen.
                aktionen.Add(KonfliktAktion.Auslassen);
                aktionen.Add(KonfliktAktion.Umbenennen);
                vorbelegung = KonfliktAktion.Auslassen;
                return aktionen;
            }

            switch (p.Befund)
            {
                case ImportBefund.Neu:
                    aktionen.Add(KonfliktAktion.Importieren);
                    aktionen.Add(KonfliktAktion.Auslassen);
                    vorbelegung = KonfliktAktion.Importieren;
                    break;

                case ImportBefund.InhaltsGleich:
                    aktionen.Add(KonfliktAktion.Importieren);
                    aktionen.Add(KonfliktAktion.Auslassen);
                    vorbelegung = KonfliktAktion.Importieren;   // gewollte Varianten sind der Regelfall (Konzept 3.3)
                    break;

                default:   // Identisch, NameVorhanden
                    aktionen.Add(KonfliktAktion.Auslassen);
                    if (!p.NameMehrfachInDb) aktionen.Add(KonfliktAktion.Ueberschreiben);
                    aktionen.Add(KonfliktAktion.Umbenennen);
                    vorbelegung = KonfliktAktion.Auslassen;
                    break;
            }
            return aktionen;
        }

        /// <summary>
        /// Befundtext einer Zeile samt Zusatzzeilen. Die Zusatzzeilen sind mit
        /// <see cref="Environment.NewLine"/> angehaengt — im Raster der Blazor-Fassung
        /// steht die Zelle deshalb im Umbruchmodus, wie die <c>WrapMode</c>-Spalte
        /// des Vorlaeufers.
        /// </summary>
        public static string BefundText(ImportPruefung p)
        {
            string text;
            switch (p.Befund)
            {
                case ImportBefund.Identisch:
                    text = MyResource.Resource.IMP_KONFLIKT_BEFUND_IDENTISCH;
                    break;
                case ImportBefund.NameVorhanden:
                    text = string.Format(MyResource.Resource.IMP_KONFLIKT_BEFUND_NAME_VORHANDEN,
                        string.Join(", ", p.AbweichendeSpalten));
                    break;
                case ImportBefund.InhaltsGleich:
                    text = string.Format(MyResource.Resource.IMP_KONFLIKT_BEFUND_INHALT_GLEICH,
                        p.Vorhanden != null ? p.Vorhanden.Name : "");
                    break;
                default:
                    text = MyResource.Resource.IMP_KONFLIKT_BEFUND_NEU;
                    break;
            }

            if (p.NameDoppeltInAuswahl)
                text += Environment.NewLine + MyResource.Resource.IMP_KONFLIKT_BEFUND_AUSWAHL_DOPPELT;
            if (p.NameMehrfachInDb)
                text += Environment.NewLine + MyResource.Resource.IMP_KONFLIKT_BEFUND_NAME_MEHRFACH;
            if (p.Vorhanden != null && p.Vorhanden.ReadOnly &&
                (p.Befund == ImportBefund.Identisch || p.Befund == ImportBefund.NameVorhanden))
                text += Environment.NewLine + MyResource.Resource.IMP_KONFLIKT_HINWEIS_READONLY;

            return text;
        }

        /// <summary>Zaehlt die Zeilen mit Konflikt — Grundlage des Kopftextes.</summary>
        public static int Konflikte(IEnumerable<ImportPruefung> pruefungen)
        {
            int konflikte = 0;
            if (pruefungen == null) return 0;
            foreach (ImportPruefung p in pruefungen)
                if (p.Befund != ImportBefund.Neu || p.NameDoppeltInAuswahl) konflikte++;
            return konflikte;
        }

        /// <summary>
        /// Ist diese Zeile ein Konflikt? Nur solche setzt „Alle Konflikte auslassen".
        /// </summary>
        public static bool IstKonflikt(ImportPruefung p)
            => p != null && (p.Befund != ImportBefund.Neu || p.NameDoppeltInAuswahl);

        /// <summary>Kopftext „{0} Eintraege, davon {1} mit Konflikt …".</summary>
        public static string KopfText(int gesamt, int konflikte)
            => string.Format(MyResource.Resource.IMP_KONFLIKT_KOPF, gesamt, konflikte);

        /// <summary>
        /// Erster freier Name der Form <c>"Name (2)"</c> … <c>"Name (99)"</c>; danach
        /// <c>"Name (neu)"</c>. Geprueft wird gegen die normalisierten Bestandsnamen.
        /// </summary>
        public static string NamensVorschlag(string name, ICollection<string> vergebeneNamen)
        {
            for (int i = 2; i < 100; i++)
            {
                string kandidat = name + " (" + i + ")";
                if (vergebeneNamen == null ||
                    !vergebeneNamen.Contains(DublettenPruefung.NormalisiereName(kandidat)))
                    return kandidat;
            }
            return name + " (neu)";
        }

        /// <summary>
        /// Die beanstandete Zeile der OK-Pruefung: ihr Platz in der Liste und der Name,
        /// der die Meldung fuellt. <c>null</c> = alles in Ordnung.
        /// </summary>
        /// <param name="Zeile">Nullbasierter Platz in der uebergebenen Liste.</param>
        /// <param name="Name">Der getrimmte Name, wie ihn die Meldung nennt.</param>
        public sealed record Beanstandung(int Zeile, string Name);

        /// <summary>
        /// Die Pruefregel des OK-Knopfes (Konzept 4.3), woertlich aus
        /// <c>Form_ImportKonflikte.BtnOk_Click</c>: Ein umbenannter Name darf weder
        /// leer noch schon vergeben sein, und zwei nicht ausgelassene Zeilen duerfen
        /// nicht auf denselben Zielnamen laufen — es sei denn, sie ueberschreiben.
        /// </summary>
        /// <param name="entscheidungen">Die Zeilen in Anzeigereihenfolge.</param>
        /// <param name="vergebeneNamen">Normalisierte Bestandsnamen des Katalogs.</param>
        /// <returns>Die erste beanstandete Zeile oder <c>null</c>.</returns>
        public static Beanstandung Pruefe(IReadOnlyList<KonfliktEntscheidung> entscheidungen,
                                          ICollection<string> vergebeneNamen)
        {
            if (entscheidungen == null) return null;

            HashSet<string> finaleNamen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entscheidungen.Count; i++)
            {
                KonfliktEntscheidung ent = entscheidungen[i];
                if (ent == null || ent.Aktion == KonfliktAktion.Auslassen) continue;

                string name = (ent.NeuerName ?? NameDerZeile(ent) ?? "").Trim();
                string norm = DublettenPruefung.NormalisiereName(name);

                bool ungueltig = false;
                if (ent.Aktion == KonfliktAktion.Umbenennen)
                    ungueltig = norm.Length == 0 ||
                                (vergebeneNamen != null && vergebeneNamen.Contains(norm));
                if (!ungueltig && ent.Aktion != KonfliktAktion.Ueberschreiben && !finaleNamen.Add(norm))
                    ungueltig = true;   // zweiter Eintrag mit demselben Zielnamen

                if (ungueltig) return new Beanstandung(i, name);
            }
            return null;
        }

        /// <summary>Die Meldung zu einer Beanstandung (<c>IMP_KONFLIKT_NAME_UNGUELTIG</c>).</summary>
        public static string BeanstandungsText(Beanstandung b)
            => b == null ? "" : string.Format(MyResource.Resource.IMP_KONFLIKT_NAME_UNGUELTIG, b.Name);

        private static string NameDerZeile(KonfliktEntscheidung ent)
            => ent.Pruefung != null && ent.Pruefung.Kandidat != null ? ent.Pruefung.Kandidat.Name : "";
    }
}
