using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE des Dialogs „Nutzungsdauern (AfA)" (Konzept „Nutzungsdauer je
    /// Technik und Positionsart aus einer AfA-Tabelle", Stufe S1, Abschnitt 2.5).
    ///
    /// <para><b>Hier liegt die Datenseite</b>, und sie ist plattformfrei: Die
    /// Hülle ruft <see cref="NutzungsdauerCtrl"/> im Kern und wandelt zwischen
    /// dessen Fachklassen und den Anzeigezeilen der Razor-Komponente. Windows
    /// steuert nur ein Fenster bei (<c>Views/Kosten/NutzungsdauerFenster.cs</c>),
    /// auf iOS zeigt dieselbe Hülle dieselbe Komponente ohne diesen Adapter —
    /// Muster <see cref="EnergietraegerHuelle"/>.</para>
    ///
    /// <para><b>Die INSTANZ hält den Bearbeitungsstand.</b> Sie lebt über die
    /// Rückrufe ihres Parametersatzes so lange wie das Fenster; nur so kann
    /// <c>Neuladen</c> den Stand nach einer Zeilenaktion frisch aus der Datenbank
    /// holen.</para>
    /// </summary>
    public sealed class NutzungsdauerHuelle
    {
        /// <summary>Breite des Windows-Fensters — acht Spalten, zwei davon Text (Etappe E10:
        /// die zwei Satzspalten Instandsetzung und Wartung kamen dazu).</summary>
        public const int FENSTER_BREITE = 1240;

        /// <summary>Höhe des Windows-Fensters — 28 Auslieferungszeilen plus Neuzeile.</summary>
        public const int FENSTER_HOEHE = 760;

        /// <summary>Der Fenstertitel — derselbe Text wie die Dialogüberschrift.</summary>
        public static string Titel()
        {
            return Text("ND_TITEL", "Nutzungsdauern (AfA)");
        }

        /// <summary>Hat der Dialog etwas geschrieben? Der Aufrufer frischt dann auf.</summary>
        public bool Geaendert { get; private set; }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>, das setzt der
        /// Wirt (Windows-Fenster bzw. Überlagerung).
        /// </summary>
        public IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Texte"] = Texte(),
                ["Zeilen"] = Laden(),
                ["Techniken"] = Techniken(),

                ["Neuladen"] = new Func<IReadOnlyList<NutzungsdauerZeileAnzeige>>(Laden),

                ["Speichern"] = new Func<IReadOnlyList<NutzungsdauerZeileAnzeige>, string>(zeilen =>
                {
                    foreach (NutzungsdauerZeileAnzeige z in zeilen)
                    {
                        NutzungsdauerZeile fach = NutzungsdauerCtrl.Zeile(z.Id);
                        if (fach == null) continue;

                        fach.Positionsart = z.Positionsart;
                        fach.Nutzungsdauer = z.Nutzungsdauer;
                        fach.AfaSteuerlich = z.AfaSteuerlich;
                        // ETAPPE E10 (Stufe S3): die zwei Saetze sind jetzt sichtbar und
                        // werden wie jedes andere Feld der Zeile geschrieben.
                        fach.InstandsetzungProzent = z.InstandsetzungProzent;
                        fach.WartungProzent = z.WartungProzent;
                        fach.Quelle = z.Quelle;

                        string grund;
                        if (!NutzungsdauerCtrl.Speichern(fach, out grund))
                            return string.IsNullOrEmpty(grund)
                                ? Text("ND_MELD_NICHT_GESPEICHERT",
                                       "Die Zeile ließ sich nicht speichern.")
                                : grund;
                    }
                    Geaendert = true;
                    return null;
                }),

                ["AnlegenDelegat"] = new Func<NutzungsdauerNeuEingabe, string>(e =>
                {
                    string grund;
                    int id = NutzungsdauerCtrl.Neu(e.TechnikId, e.Positionsart,
                                                   e.Nutzungsdauer, e.AfaSteuerlich,
                                                   e.InstandsetzungProzent, e.WartungProzent,
                                                   Text("ND_QUELLE_EIGEN", "eigener Wert"),
                                                   out grund);
                    if (id <= 0) return string.IsNullOrEmpty(grund) ? "" : grund;
                    Geaendert = true;
                    return null;
                }),

                ["LoeschenDelegat"] = new Func<int, string>(id =>
                {
                    string grund;
                    if (!NutzungsdauerCtrl.Loeschen(id, out grund))
                        return string.IsNullOrEmpty(grund) ? "" : grund;
                    Geaendert = true;
                    return null;
                }),

                ["WiederherstellenDelegat"] = new Func<int>(() =>
                {
                    int n = NutzungsdauerCtrl.AuslieferungWiederherstellen();
                    if (n > 0) Geaendert = true;
                    return n;
                }),
            };
        }

        // =================================================================== Laden

        /// <summary>Die Zeilen des Kerns als Anzeigezeilen — Reihenfolge unverändert
        /// (nach Technik gruppiert, Standard zuerst).</summary>
        private static IReadOnlyList<NutzungsdauerZeileAnzeige> Laden()
        {
            var zeilen = new List<NutzungsdauerZeileAnzeige>();
            foreach (NutzungsdauerZeile z in NutzungsdauerCtrl.Alle())
                zeilen.Add(new NutzungsdauerZeileAnzeige
                {
                    Id = z.Id,
                    TechnikId = z.KomponentenId,
                    Technik = z.Technik ?? "",
                    Positionsart = z.Positionsart ?? "",
                    IstStandard = z.IstStandard,
                    Nutzungsdauer = z.Nutzungsdauer,
                    AfaSteuerlich = z.AfaSteuerlich,
                    InstandsetzungProzent = z.InstandsetzungProzent,
                    WartungProzent = z.WartungProzent,
                    Quelle = z.Quelle ?? "",
                    Auslieferung = z.NurLesen,
                });
            return zeilen;
        }

        /// <summary>Die wählbaren Techniken der Neuzeile — der Komponentenkatalog.</summary>
        private static IReadOnlyList<ValueTuple<int, string>> Techniken()
        {
            var liste = new List<ValueTuple<int, string>>();
            foreach (KeyValuePair<int, string> k in KostenVorlagenCtrl.Komponenten())
                liste.Add(new ValueTuple<int, string>(k.Key, k.Value ?? ""));
            return liste;
        }

        // =================================================================== Texte

        private static NutzungsdauerTexte Texte()
        {
            return new NutzungsdauerTexte
            {
                Titel = Text("ND_TITEL", "Nutzungsdauern (AfA)"),
                Kontext = Text("ND_KONTEXT",
                    "Rechnerische Nutzungsdauer je Technik und Positionsart — sie steuert " +
                    "Ersatzbeschaffung und Restwert im Kapitalwert."),
                SpalteTechnik = Text("ND_SP_TECHNIK", "Technik"),
                SpaltePositionsart = Text("ND_SP_POSITIONSART", "Positionsart"),
                SpalteNutzungsdauer = Text("ND_SP_NUTZUNGSDAUER", "Nutzungsdauer [a]"),
                SpalteAfa = Text("ND_SP_AFA", "AfA steuerlich [a]"),
                SpalteInstandsetzung = Text("ND_SP_INSTANDSETZUNG", "Instandsetzung [%/a]"),
                SpalteWartung = Text("ND_SP_WARTUNG", "Wartung [%/a]"),
                SaetzeHinweis = Text("ND_SAETZE_HINWEIS",
                    "Instandsetzung und Wartung in % der Investition je Jahr (VDI 2067 Blatt 1, " +
                    "Tabelle A2). Sie gelten für Betriebskostenpositionen „% der Investition“ " +
                    "ohne eigenen Satz; eine leere Zelle heißt „kein Satz“."),
                SpalteQuelle = Text("ND_SP_QUELLE", "Quelle"),
                SpalteAktionen = Text("ND_SP_AKTIONEN", "Aktionen"),
                Suche = Text("ND_SUCHE", "Suchen"),
                SuchePlatzhalter = Text("ND_SUCHE_PLATZHALTER", "Technik, Positionsart oder Quelle"),
                Neu = Text("ND_NEU", "Neu"),
                Loeschen = Text("ND_LOESCHEN", "Löschen"),
                Wiederherstellen = Text("ND_WIEDERHERSTELLEN", "Auslieferungswerte wiederherstellen"),
                FrageWiederherstellen = Text("ND_FRAGE_WIEDERHERSTELLEN",
                    "Die Werte aller Auslieferungszeilen auf die Auslieferung zurücksetzen? " +
                    "Eigene Zeilen bleiben unberührt."),
                FrageLoeschen = Text("ND_FRAGE_LOESCHEN", "Zeile „{0}“ löschen?"),
                TipAuslieferung = Text("ND_TIP_AUSLIEFERUNG",
                    "Auslieferungszeile: Der Wert ist änderbar, die Zeile wird nicht gelöscht."),
                TipStandard = Text("ND_TIP_STANDARD",
                    "Standardzeile der Technik — sie gilt, wenn eine Position keine Positionsart trägt."),
                KennzeichenStandard = Text("ND_KENNZEICHEN_STANDARD", "Standard"),
                Technikuebergreifend = Text("ND_TECHNIKUEBERGREIFEND", "technikübergreifend"),
                LeerHinweis = Text("ND_LEER_HINWEIS",
                    "Eine leere Nutzungsdauer heißt „wie die Standardzeile der Technik“."),
                LeereListe = Text("ND_LEER_LISTE", "Keine Zeile passt zur Suche."),
                MeldungArtLeer = Text("ND_MELD_ART_LEER", "Die Positionsart darf nicht leer sein."),
                Ok = MyResource.Resource.ALLG_BTN_OK,
                // W-E2 (Mockup-Prüfung 04): Abbrechen ist ebenfalls ein Hausknopf —
                // PVW_ABBRECHEN war ein eigener, gleichlautender Schlüssel für denselben Text.
                Abbrechen = Text("ALLG_BTN_ABBRECHEN", "Abbrechen"),
                Speichern = MyResource.Resource.ADM_BTN_SPEICHERN,
            };
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
