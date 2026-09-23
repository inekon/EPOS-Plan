using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Warum eine Razor-Maske mit Eingabefeldern NICHT im Dialogkatalog steht —
    /// die Gruppen der Ausnahmeliste des Entscheids KI‑D‑Q11 (23.09.2026).
    /// </summary>
    public enum KiAusnahmegrund
    {
        /// <summary>Reine Anzeige ohne Einstellwerte (Ergebnis-, Berichts-, Übersichtsseite).</summary>
        Anzeige,

        /// <summary>Verwaltung ohne Einstellwerte — sie führt nur Zeitreihen und ihre Herkunft.</summary>
        OhneEinstellwerte,

        /// <summary>Legt Sätze an oder nimmt sie weg: Neu, Duplizieren, Löschen, Import, Export.</summary>
        AnlegenOderEntfernen,

        /// <summary>Dateidialog — die Pfadwahl bleibt Anwendersache.</summary>
        Datei,

        /// <summary>Rückfrage.</summary>
        Rueckfrage,

        /// <summary>Lizenz- oder Schlüsseleingabe.</summary>
        LizenzOderSchluessel,

        /// <summary>Der Hilfe-Assistent selbst.</summary>
        Assistent,

        /// <summary>Offen, bis eine Bedingung erfüllt ist — die Erläuterung nennt sie.</summary>
        Offen
    }

    /// <summary>
    /// Eine Razor-Maske, die der Assistent mit Absicht NICHT steuert (KI‑D‑Q11).
    /// </summary>
    public sealed class KiAusnahme
    {
        /// <summary>Legt einen Eintrag der Ausnahmeliste an.</summary>
        /// <param name="komponente">Typname der Razor-Komponente, ohne Namensraum.</param>
        /// <param name="grund">Die Gruppe der Ausnahmeliste.</param>
        /// <param name="erlaeuterung">Ein Satz für Entwickler — kein Anzeigetext.</param>
        public KiAusnahme(string komponente, KiAusnahmegrund grund, string erlaeuterung)
        {
            if (string.IsNullOrWhiteSpace(komponente))
                throw new ArgumentException("Die Ausnahme braucht den Typnamen der Komponente.", nameof(komponente));
            if (string.IsNullOrWhiteSpace(erlaeuterung))
                throw new ArgumentException("Die Ausnahme '" + komponente + "' braucht ihren Grund in einem Satz.",
                                            nameof(erlaeuterung));

            Komponente = komponente.Trim();
            Grund = grund;
            Erlaeuterung = erlaeuterung.Trim();
        }

        /// <summary>Typname der Razor-Komponente, ohne Namensraum.</summary>
        public string Komponente { get; }

        /// <summary>Die Gruppe der Ausnahmeliste.</summary>
        public KiAusnahmegrund Grund { get; }

        /// <summary>Der Grund in einem Satz — für Entwickler, kein Anzeigetext.</summary>
        public string Erlaeuterung { get; }

        /// <inheritdoc/>
        public override string ToString() => Komponente + " (" + Grund + ")";
    }

    /// <summary>
    /// <b>Die Ausnahmeliste des Dialogkatalogs als DATEN</b> (Entscheid KI‑D‑Q11,
    /// Welle #456): Steuerbar ist jede Maske mit Einstellwerten, Projekt- wie
    /// Administrationsdialoge; was davon abweicht, steht hier mit Grund.
    /// </summary>
    /// <remarks>
    /// <para><b>Wozu als Daten.</b> Die Regel lautet „jede Razor-Maske mit
    /// Eingabefeldern ist angemeldet ODER steht auf der Ausnahmeliste". Als Satz in einem
    /// Konzept lässt sie sich nicht prüfen; als Liste neben dem Katalog
    /// (<see cref="KiDialoge"/>) kann ein Wächter beide gegen den Bestand der
    /// Komponenten halten. Den Wächter und das Inventar der übrigen Masken bringt die
    /// Folgewelle; hier steht die Struktur mit den Einträgen, die schon feststehen.</para>
    /// <para><b>Was NICHT hier steht:</b> Bausteine ohne eigenes Fenster (sie gehören zur
    /// Maske ihres Wirts) und Masken, die im Katalog stehen — auch dann nicht, wenn nur
    /// ein Teil ihrer Felder setzbar ist; das sagt der Katalog selbst
    /// (<c>nurLesen</c>).</para>
    /// </remarks>
    public static class KiDialogAusnahmen
    {
        /// <summary>Die Einträge der Ausnahmeliste.</summary>
        public static IReadOnlyList<KiAusnahme> Alle { get; } = new[]
        {
            new KiAusnahme("KiChatDialog", KiAusnahmegrund.Assistent,
                           "Der Chat des Hilfe-Assistenten steuert sich nicht selbst."),
            new KiAusnahme("KiEinstellungenDialog", KiAusnahmegrund.LizenzOderSchluessel,
                           "Die Einstellungen des Assistenten tragen Zugangsschlüssel und Einwilligungen."),
            new KiAusnahme("LizenzDialog", KiAusnahmegrund.LizenzOderSchluessel,
                           "Lizenzeingaben bleiben Sache des Anwenders."),
            new KiAusnahme("LizenzVerwaltungDialog", KiAusnahmegrund.LizenzOderSchluessel,
                           "Lizenzeingaben bleiben Sache des Anwenders."),
            new KiAusnahme("Rueckfrage", KiAusnahmegrund.Rueckfrage,
                           "Eine Rückfrage beantwortet der Anwender, nicht der Assistent."),
            new KiAusnahme("NamensDialog", KiAusnahmegrund.AnlegenOderEntfernen,
                           "Die Namensabfrage gehört zu Neu… und Duplizieren…, die Sätze anlegen."),
            new KiAusnahme("KatalogImportDialog", KiAusnahmegrund.AnlegenOderEntfernen,
                           "Der Herstellerimport legt Katalogsätze an."),
            new KiAusnahme("WaermebedarfAdminDialog", KiAusnahmegrund.OhneEinstellwerte,
                           "Die Verwaltung führt nur Lastgänge und ihre Herkunft."),
            new KiAusnahme("SolarganglinieAdminDialog", KiAusnahmegrund.OhneEinstellwerte,
                           "Die Verwaltung führt nur Ganglinien und ihre Herkunft.")
        };

        /// <summary>Der Eintrag zu einer Komponente; <c>null</c> = sie steht nicht auf der Liste.</summary>
        public static KiAusnahme Finde(string komponente)
        {
            if (string.IsNullOrWhiteSpace(komponente)) return null;
            foreach (KiAusnahme a in Alle)
                if (string.Equals(a.Komponente, komponente.Trim(), StringComparison.Ordinal)) return a;
            return null;
        }
    }
}
