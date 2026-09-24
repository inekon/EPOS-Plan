using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Warum eine Razor-Maske mit Eingabefeldern NICHT im Dialogkatalog steht —
    /// die Gruppen der Ausnahmeliste des Entscheids KI‑D‑Q11 (23.09.2026).
    /// </summary>
    /// <remarks>
    /// Jede Gruppe hat einen Anzeigetext (<see cref="KiDialogAusnahmen.Grundtext"/>):
    /// Mit ihm begründet der Assistent seine Absage, wenn er aus einer ausgenommenen
    /// Maske heraus gerufen wird.
    /// </remarks>
    public enum KiAusnahmegrund
    {
        /// <summary>Reine Anzeige ohne Einstellwerte (Ergebnis-, Berichts-, Übersichtsseite, Diagrammschalter).</summary>
        Anzeige,

        /// <summary>Verwaltung ohne Einstellwerte — sie führt nur Zeitreihen und ihre Herkunft.</summary>
        OhneEinstellwerte,

        /// <summary>Legt Sätze an oder nimmt sie weg: Neu, Duplizieren, Löschen.</summary>
        AnlegenOderEntfernen,

        /// <summary>Gehört zu einem Import — was eingelesen wird, entscheidet der Anwender.</summary>
        Import,

        /// <summary>Gehört zu einem Export — was ausgegeben wird, entscheidet der Anwender.</summary>
        Export,

        /// <summary>Dateidialog — die Pfadwahl bleibt Anwendersache.</summary>
        Datei,

        /// <summary>Rückfrage.</summary>
        Rueckfrage,

        /// <summary>Lizenz- oder Schlüsseleingabe.</summary>
        LizenzOderSchluessel,

        /// <summary>Der Hilfe-Assistent selbst.</summary>
        Assistent,

        /// <summary>Eine Aktion, die der Anwender auslöst und bestätigt (Übernahme, Auswahl zum Öffnen).</summary>
        Aktion,

        /// <summary>Ein Pflegewerkzeug, dessen Eingriffe der Anwender selbst vornimmt.</summary>
        Werkzeug,

        /// <summary>
        /// Eine Überlagerung, deren Wert als Feld der angemeldeten Wirtsmaske setzbar ist —
        /// die Überlagerung selbst bleibt dem Anwender.
        /// </summary>
        FeldDesWirts,

        /// <summary>Offen, bis ein Auftrag sie anbindet — <see cref="KiAusnahme.Auftrag"/> nennt ihn.</summary>
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
        /// <param name="auftrag">
        /// Der Auftrag, der die Maske anbindet oder die Ausnahme überprüft („#458 Stufe 2");
        /// Pflicht bei <see cref="KiAusnahmegrund.Offen"/>.
        /// </param>
        /// <param name="hilfeschluessel">
        /// Der Hilfeschlüssel am Info-Knopf der Maske (<c>Form_Zapfprofil.btn_Help</c>);
        /// leer, wenn sie keinen eigenen trägt. Über ihn erkennt der Assistent, dass er
        /// aus dieser Maske gerufen wurde, und nennt den Grund seiner Absage.
        /// </param>
        public KiAusnahme(string komponente, KiAusnahmegrund grund, string erlaeuterung,
                          string auftrag = null, string hilfeschluessel = null)
        {
            if (string.IsNullOrWhiteSpace(komponente))
                throw new ArgumentException("Die Ausnahme braucht den Typnamen der Komponente.", nameof(komponente));
            if (string.IsNullOrWhiteSpace(erlaeuterung))
                throw new ArgumentException("Die Ausnahme '" + komponente + "' braucht ihren Grund in einem Satz.",
                                            nameof(erlaeuterung));
            if (grund == KiAusnahmegrund.Offen && string.IsNullOrWhiteSpace(auftrag))
                throw new ArgumentException("Die offene Ausnahme '" + komponente + "' braucht den Auftrag, der sie anbindet.",
                                            nameof(auftrag));

            Komponente = komponente.Trim();
            Grund = grund;
            Erlaeuterung = erlaeuterung.Trim();
            Auftrag = (auftrag ?? "").Trim();
            Hilfeschluessel = (hilfeschluessel ?? "").Trim();
        }

        /// <summary>Typname der Razor-Komponente, ohne Namensraum.</summary>
        public string Komponente { get; }

        /// <summary>Die Gruppe der Ausnahmeliste.</summary>
        public KiAusnahmegrund Grund { get; }

        /// <summary>Der Grund in einem Satz — für Entwickler, kein Anzeigetext.</summary>
        public string Erlaeuterung { get; }

        /// <summary>Der Auftrag, der die Maske anbindet oder die Ausnahme prüft; leer = keiner.</summary>
        public string Auftrag { get; }

        /// <summary>Der Hilfeschlüssel am Info-Knopf der Maske; leer = sie trägt keinen eigenen.</summary>
        public string Hilfeschluessel { get; }

        /// <inheritdoc/>
        public override string ToString() => Komponente + " (" + Grund + ")";
    }

    /// <summary>
    /// <b>Die Ausnahmeliste des Dialogkatalogs als DATEN</b> (Entscheid KI‑D‑Q11,
    /// Wellen #456 und #458): Steuerbar ist jede Maske mit Einstellwerten, Projekt- wie
    /// Administrationsdialoge; was davon abweicht, steht hier mit Grund.
    /// </summary>
    /// <remarks>
    /// <para><b>Die Regel:</b> Eine Razor-Maske mit Eingabefeldern ist beim Assistenten
    /// angemeldet (<c>KiMaskenanmeldung.Fuer</c>), gehört als Baustein zu einem Wirt, der
    /// anmeldet, oder steht hier mit Grund. Der Wächter
    /// <c>EPOS.UI.Tests/Dialoge/Hilfe/KiMaskenabdeckungWacheTests</c> hält alle drei Wege
    /// gegen den Bestand der Komponenten unter <c>EPOS.UI/Dialoge</c> und
    /// <c>EPOS.UI/Seiten</c> — und jeden Eintrag hier gegen die Datei: Sie existiert, hat
    /// Eingabefelder und meldet nicht an. Ein Eintrag, der nicht mehr zutrifft, wird
    /// gestrichen.</para>
    /// <para><b>Was NICHT hier steht:</b> Bausteine ohne eigenes Fenster (sie gehören zur
    /// Maske ihres Wirts), Masken ohne Eingabefelder (reine Anzeigen, Knopfleisten) und
    /// Masken, die im Katalog stehen — auch dann nicht, wenn nur ein Teil ihrer Felder
    /// setzbar ist; das sagt der Katalog selbst (<c>nurLesen</c>) und die Eingabebilanz
    /// des Wächters (<c>BewusstDraussen</c>).</para>
    /// </remarks>
    public static class KiDialogAusnahmen
    {
        /// <summary>Die Einträge der Ausnahmeliste.</summary>
        public static IReadOnlyList<KiAusnahme> Alle { get; } = new[]
        {
            // ---- Der Assistent selbst -------------------------------------------------
            new KiAusnahme("KiChatDialog", KiAusnahmegrund.Assistent,
                           "Der Chat des Hilfe-Assistenten steuert sich nicht selbst.",
                           hilfeschluessel: "Form_KiChat.btn_Help"),
            new KiAusnahme("KiEingabezeile", KiAusnahmegrund.Assistent,
                           "Die Eingabezeile des Chats nimmt die Frage des Anwenders auf."),
            new KiAusnahme("KiWerkzeugliste", KiAusnahmegrund.Assistent,
                           "Die Werkzeugliste des Assistenten ist seine eigene Übersicht.",
                           hilfeschluessel: "KiWerkzeugliste.btn_Help"),

            // ---- Lizenz und Schlüssel ------------------------------------------------
            new KiAusnahme("KiEinstellungenDialog", KiAusnahmegrund.LizenzOderSchluessel,
                           "Die Einstellungen des Assistenten tragen Zugangsschlüssel und Einwilligungen."),
            new KiAusnahme("LizenzVerwaltungDialog", KiAusnahmegrund.LizenzOderSchluessel,
                           "Lizenzeingaben bleiben Sache des Anwenders.",
                           hilfeschluessel: "Form_LizenzVerwaltung.btn_Help"),

            // ---- Anlegen, Import, Export ---------------------------------------------
            new KiAusnahme("NamensDialog", KiAusnahmegrund.AnlegenOderEntfernen,
                           "Die Namensabfrage gehört zu Neu… und Duplizieren…, die Sätze anlegen."),
            new KiAusnahme("KatalogImportDialog", KiAusnahmegrund.Import,
                           "Der Herstellerimport legt Katalogsätze an."),
            new KiAusnahme("ImportKonflikteDialog", KiAusnahmegrund.Import,
                           "Die Konfliktliste des Herstellerimports entscheidet über anzulegende Sätze.",
                           hilfeschluessel: "Form_ImportKonflikte.btn_Help"),
            new KiAusnahme("SpotpreisImportDialog", KiAusnahmegrund.Import,
                           "Der Spotpreisimport liest eine Preisreihe aus einer Datei ein.",
                           hilfeschluessel: "Form_SpotpreisImport.btn_Help"),
            new KiAusnahme("GanglinieImportOptionenDialog", KiAusnahmegrund.Import,
                           "Die Optionen gelten nur für das Einlesen einer Gangliniendatei.",
                           hilfeschluessel: "Form_GanglinieImportOptionen.btn_Help"),
            new KiAusnahme("SpeicherFlottenCsvDialog", KiAusnahmegrund.Import,
                           "Das Format einer CSV-Datei der Flotte gilt nur für ihr Einlesen."),
            new KiAusnahme("ProjektTransferDialog", KiAusnahmegrund.Export,
                           "Der Projekttransfer schreibt und liest Projektpakete als Datei."),

            // ---- Aktionen, Rückfragen, Werkzeuge -------------------------------------
            new KiAusnahme("ProjektWahlDialog", KiAusnahmegrund.Aktion,
                           "Die Projektwahl öffnet oder löscht ein Projekt; die Sicherung davor wählt der Anwender."),
            new KiAusnahme("BkUebernahmeDialog", KiAusnahmegrund.Aktion,
                           "Die Übernahme in die Kostenaufstellung ist eine Aktion, keine Einstellung.",
                           hilfeschluessel: "Form_BkUebernahme.btn_Help"),
            new KiAusnahme("VorlagenUebernahmeDialog", KiAusnahmegrund.Aktion,
                           "Die Vorlagenübernahme legt Kostenpositionen an; ihre Wahl ist eine Aktion.",
                           hilfeschluessel: "Form_VorlagenUebernahme.btn_Help"),
            new KiAusnahme("WaermepumpenKatalogDialog", KiAusnahmegrund.Aktion,
                           "Die Katalogauswahl übernimmt ein Gerät; ihr Schalter filtert nur die Liste.",
                           hilfeschluessel: "Form_WPFilterAuswahl.btn_Help"),
            new KiAusnahme("WertAbfrage", KiAusnahmegrund.Rueckfrage,
                           "Die Zahlabfrage ist eine Rückfrage ihres Wirts; die Simulationsansicht führt " +
                           "Priorität und Quelltemperatur als eigene Felder."),
            new KiAusnahme("KatalogDublettenDialog", KiAusnahmegrund.Werkzeug,
                           "Das Dublettenwerkzeug führt Katalogsätze zusammen; der Eingriff bleibt beim Anwender.",
                           hilfeschluessel: "Form_KatalogDubletten.btn_Help"),

            // ---- Überlagerung, deren Wert der Wirt führt -----------------------------
            new KiAusnahme("BetriebsmodusDialog", KiAusnahmegrund.FeldDesWirts,
                           "Der Betriebsmodus ist das Feld „betriebsmodus“ der Maske Simulation — " +
                           "derselbe Schreibweg (BetriebsmodusSchreiben); die Überlagerung bleibt dem Anwender.",
                           hilfeschluessel: "Form_Betriebsmodus.btn_Help"),

            // ---- Anzeigen mit Schaltern eines Bildes ---------------------------------
            new KiAusnahme("BedarfGangGrafik", KiAusnahmegrund.Anzeige,
                           "Die Optionsgruppe wählt nur die gezeigte Kurve."),
            new KiAusnahme("SpeicherFlottenErgebnisAnsicht", KiAusnahmegrund.Anzeige,
                           "Die Schalter stellen nur das Ergebnisbild der Flotte ein."),
            new KiAusnahme("SpeicherFlottenGroessenAnsicht", KiAusnahmegrund.Anzeige,
                           "Die Wahl stellt nur das Bild der Größenrechnung ein.")

            // ---- Offen ----------------------------------------------------------------
            // Eine Maske mit Einstellwerten, die noch nicht angebunden ist, steht hier als
            // KiAusnahmegrund.Offen mit dem Auftrag, der sie anbindet.
        };

        /// <summary>Der Eintrag zu einer Komponente; <c>null</c> = sie steht nicht auf der Liste.</summary>
        public static KiAusnahme Finde(string komponente)
        {
            if (string.IsNullOrWhiteSpace(komponente)) return null;
            foreach (KiAusnahme a in Alle)
                if (string.Equals(a.Komponente, komponente.Trim(), StringComparison.Ordinal)) return a;
            return null;
        }

        /// <summary>
        /// Der Eintrag, dessen Maske diesen Hilfeschlüssel trägt; <c>null</c> = keiner.
        /// </summary>
        /// <remarks>
        /// Der Weg der benannten Absage: Der Assistent weiß aus seinem Aufruf
        /// (<see cref="KiChatKontext.Aufruf"/>), aus welcher Maske er gerufen wurde — nur
        /// über den Hilfeschlüssel ihres Info-Knopfes. Teilen zwei Einträge einen Schlüssel,
        /// gilt der erste; der Wächter verlangt dann denselben Grund.
        /// </remarks>
        public static KiAusnahme FuerHilfeschluessel(string hilfeschluessel)
        {
            if (string.IsNullOrWhiteSpace(hilfeschluessel)) return null;
            string gesucht = hilfeschluessel.Trim();
            foreach (KiAusnahme a in Alle)
                if (a.Hilfeschluessel.Length > 0 &&
                    string.Equals(a.Hilfeschluessel, gesucht, StringComparison.Ordinal)) return a;
            return null;
        }

        /// <summary>Der Anzeigetext einer Gruppe — der Grund, den die Absage nennt.</summary>
        public static string Grundtext(KiAusnahmegrund grund)
        {
            switch (grund)
            {
                case KiAusnahmegrund.Anzeige: return MyResource.Resource.KI_AUSNAHME_ANZEIGE;
                case KiAusnahmegrund.OhneEinstellwerte: return MyResource.Resource.KI_AUSNAHME_OHNE_EINSTELLWERTE;
                case KiAusnahmegrund.AnlegenOderEntfernen: return MyResource.Resource.KI_AUSNAHME_ANLEGEN;
                case KiAusnahmegrund.Import: return MyResource.Resource.KI_AUSNAHME_IMPORT;
                case KiAusnahmegrund.Export: return MyResource.Resource.KI_AUSNAHME_EXPORT;
                case KiAusnahmegrund.Datei: return MyResource.Resource.KI_AUSNAHME_DATEI;
                case KiAusnahmegrund.Rueckfrage: return MyResource.Resource.KI_AUSNAHME_RUECKFRAGE;
                case KiAusnahmegrund.LizenzOderSchluessel: return MyResource.Resource.KI_AUSNAHME_LIZENZ;
                case KiAusnahmegrund.Assistent: return MyResource.Resource.KI_AUSNAHME_ASSISTENT;
                case KiAusnahmegrund.Aktion: return MyResource.Resource.KI_AUSNAHME_AKTION;
                case KiAusnahmegrund.Werkzeug: return MyResource.Resource.KI_AUSNAHME_WERKZEUG;
                case KiAusnahmegrund.FeldDesWirts: return MyResource.Resource.KI_AUSNAHME_FELD_DES_WIRTS;
                default: return MyResource.Resource.KI_AUSNAHME_OFFEN;
            }
        }

        /// <summary>
        /// Die Absage für einen Aufruf aus einer ausgenommenen Maske — „Diese Maske ist
        /// bewusst nicht steuerbar: ⟨Grund⟩"; <c>null</c>, wenn der Hilfeschlüssel keine
        /// Maske der Liste trifft.
        /// </summary>
        /// <remarks>
        /// Eine <see cref="KiAusnahmegrund.Offen"/>-Maske ist nicht BEWUSST ausgenommen,
        /// sondern noch nicht angebunden; ihre Absage sagt das.
        /// </remarks>
        public static string Absage(string hilfeschluessel) => AbsageFuer(FuerHilfeschluessel(hilfeschluessel));

        /// <summary>
        /// Die Absage zu einem Eintrag — derselbe Satz wie über seinen Hilfeschlüssel;
        /// <c>null</c> ohne Eintrag.
        /// </summary>
        public static string AbsageFuer(KiAusnahme a)
        {
            if (a == null) return null;

            string vorlage = a.Grund == KiAusnahmegrund.Offen
                ? MyResource.Resource.KI_DLG_NOCH_NICHT_STEUERBAR
                : MyResource.Resource.KI_DLG_BEWUSST_NICHT_STEUERBAR;

            return string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, Grundtext(a.Grund));
        }
    }
}
