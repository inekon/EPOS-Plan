using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Komponentenschritts (iU9-W16a.3) — sie löst
    /// <c>Wizard_Komponenten</c> ab.
    ///
    /// <para><b>Sie ist die Datenseite der Kacheln.</b> Der Bestand kommt aus
    /// <see cref="KomponentenBestandCtrl"/> (K1, seit iU9-W16a.0 im Kern) — derselben
    /// Quelle und denselben Kriterien wie die Bitmaske der Startmaske (Befund
    /// W16-B9). Die Komponente kennt weder Datenbank noch Rahmen: Sie bekommt
    /// dreizehn <see cref="KomponentenZeile"/> herein, schaltet darin um und meldet
    /// jede Umschaltung als Seitenindex zurück.</para>
    ///
    /// <para><b>Der Rahmen wird über <c>WizardParent.Aktiver</c> erreicht</b> — genau
    /// wie im Vorläufer (<c>Wizard_Komponenten.BestandAnzeigen</c> :121,
    /// <c>karte_Geklickt</c> :196). Diese Kante fällt mit iU9-W16a.5, wenn der Rahmen
    /// selbst eine Razor-Seite wird; bis dahin bleibt sie, damit die Teilwelle für
    /// sich lauffähig ist.</para>
    ///
    /// <para><b>Vier tote Glieder entfallen</b> (Befund W16-B13):
    /// <c>TextNeuesProjektFrage</c>, <c>TextNeuesProjektTitel</c>, <c>IstAn(int)</c>
    /// und <c>Bestand</c> hatten im ganzen Bestand keinen Aufrufer.</para>
    /// </summary>
    internal static class KomponentenauswahlHuelle
    {
        /// <summary>Wunschmaß der Seite im Assistentenfenster (Vorläufer: 831 × 744).</summary>
        private static readonly Size MASS = new Size(880, 760);

        /// <summary>
        /// Die KOMPONENTENSEITE des Assistenten (Index 0) — Muster
        /// <c>GebaeudeHuelle.AssistentSeite()</c>.
        /// </summary>
        internal static Form AssistentSeite()
        {
            return new BlazorAssistentSeite<KomponentenauswahlDialog, KomponentenZeile>(
                (projektId, projektName, modelle) =>
                    new Dictionary<string, object>(Gaben(projektId, modelle,
                                                         Betriebsart(), SeiteSchalten)),
                MASS);
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente. Er LIEST dabei den Bestand des Projekts
        /// und schaltet die Assistentenseiten danach — genau das, was
        /// <c>Wizard_Komponenten.BestandAnzeigen</c> tat.
        /// </summary>
        /// <param name="projektId">Das Projekt; 0 oder kleiner = neues Projekt, leerer Bestand.</param>
        /// <param name="zeilen">
        /// Die geteilte Liste der dreizehn Kacheln. Sie wird AN ORT UND STELLE neu
        /// aufgebaut — der Assistent reicht dasselbe Objekt über mehrere
        /// Seitenbesuche hinweg.
        /// </param>
        /// <param name="betriebsart">
        /// <c>WizardParent.WIZARD_MODE_NEU</c> oder <c>…_BEARBEITEN</c>; nur im
        /// Bearbeiten-Modus fragt das Abwählen einer belegten Komponente nach.
        /// </param>
        /// <param name="seiteSchalten">Schaltet eine Assistentenseite frei oder ab.</param>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int projektId, List<KomponentenZeile> zeilen, int betriebsart,
            Action<int, bool> seiteSchalten)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(projektId);

            zeilen.Clear();
            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
            {
                KomponentenBestandCtrl.Eintrag e = bestand[k];

                zeilen.Add(new KomponentenZeile
                {
                    Kennung = k,
                    Titel = Titel(k),
                    SeitenIndex = e.SeitenIndex,
                    Anzahl = e.Anzahl,
                    Namen = e.Namen.ToArray(),
                    An = e.Vorhanden
                });

                // Wie BestandAnzeigen: Der gelesene Bestand stellt die Seiten.
                if (seiteSchalten != null && e.SeitenIndex != KomponentenBestandCtrl.OHNE_SEITE)
                    seiteSchalten(e.SeitenIndex, e.Vorhanden);
            }

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["BearbeitenModus"] = betriebsart == WizardParent.WIZARD_MODE_BEARBEITEN,
                ["Geschaltet"] = seiteSchalten ?? ((_, __) => { }),

                ["KopfText"] = Text_("KOMPAUSW_KOPF", "Projekt-Erstellungskonfiguration"),
                ["HinweisText"] = Text_("KOMPAUSW_HINWEIS",
                    "Der Projektassistent führt Sie durch alle notwendigen Schritte. Alle zu " +
                    "einem Projekt notwendigen Eingaben werden dadurch vollständig durchgeführt. " +
                    "Navigieren Sie mit Weiter und Zurück, um die nächsten Eingaben zu machen " +
                    "bzw. um Eingaben nachträglich anzupassen."),
                ["AuswahlText"] = Text_("KOMPAUSW_AUSWAHL",
                    "Wärmeerzeuger bzw. Energieerzeuger Komponenten auswählen:"),

                ["EnthaltenText"] = Text_("KOMPAUSW_ENTHALTEN", "{0} im Projekt"),
                ["OhneText"] = Text_("KOMPAUSW_OHNE", "nicht im Projekt"),
                ["NurAnzeigeText"] = Text_("KOMPAUSW_NURANZEIGE", "nur Anzeige"),

                ["FrageText"] = Zeilenumbruch.Normalisieren(Text_("KOMPAUSW_FRAGE",
                    "„{0}“ wird aus dem Projekt genommen.\n\nBeim Speichern werden {1} " +
                    "Einträge gelöscht:\n{2}\n\nWirklich entfernen?")),
                ["FrageTitelText"] = Text_("KOMPAUSW_FRAGE_TITEL", "Komponente entfernen"),
                ["JaText"] = Text_("KOMPAUSW_BTN_JA", "Ja"),
                ["NeinText"] = Text_("KOMPAUSW_BTN_NEIN", "Nein")
            };
        }

        /// <summary>Der Fenstertitel der Seite — <c>$this.Text</c> des Vorläufers.</summary>
        internal static string Titelzeile()
        {
            return Text_("KOMPAUSW_TITEL", "Komponenten auswählen");
        }

        /// <summary>
        /// Die dreizehn Kachelbeschriftungen in der Lesereihenfolge des Bestands —
        /// wörtlich die <c>Titel</c> der dreizehn <c>AktionsKarte</c> aus
        /// <c>Wizard_Komponenten.de-DE.resx</c>.
        /// </summary>
        private static string Titel(int kennung)
        {
            switch (kennung)
            {
                case KomponentenBestandCtrl.GEBAEUDE: return Text_("KOMPAUSW_K_GEBAEUDE", "Gebäude");
                case KomponentenBestandCtrl.WAERMEBEDARF: return Text_("KOMPAUSW_K_WAERMEBEDARF", "Wärmebedarfsdaten");
                case KomponentenBestandCtrl.PROZESS: return Text_("KOMPAUSW_K_PROZESS", "Prozesswärme");
                case KomponentenBestandCtrl.BRAUCHWASSER: return Text_("KOMPAUSW_K_BRAUCHWASSER", "Brauchwasser");
                case KomponentenBestandCtrl.STROMSTD: return Text_("KOMPAUSW_K_STROMSTD", "Standard-Stromlastprofil");
                case KomponentenBestandCtrl.STROMLASTGANG: return Text_("KOMPAUSW_K_STROMLASTGANG", "Stromlastgang");
                case KomponentenBestandCtrl.WP: return Text_("KOMPAUSW_K_WP", "Wärmepumpe");
                case KomponentenBestandCtrl.BHKW: return Text_("KOMPAUSW_K_BHKW", "BHKW");
                case KomponentenBestandCtrl.KESSEL: return Text_("KOMPAUSW_K_KESSEL", "Spitzenkessel");
                case KomponentenBestandCtrl.SOLAR: return Text_("KOMPAUSW_K_SOLAR", "Solarthermie");
                case KomponentenBestandCtrl.PV: return Text_("KOMPAUSW_K_PV", "Photovoltaik");
                case KomponentenBestandCtrl.SP: return Text_("KOMPAUSW_K_SP", "Stromspeicher");
                case KomponentenBestandCtrl.PUFFER: return Text_("KOMPAUSW_K_PUFFER", "Pufferspeicher");
                default: return "";
            }
        }

        /// <summary>Die Betriebsart des laufenden Assistenten; Neu, wenn keiner läuft.</summary>
        private static int Betriebsart()
        {
            IAssistentRahmen rahmen = WizardParent.Aktiver;
            return rahmen == null ? WizardParent.WIZARD_MODE_NEU : rahmen.Betriebsart;
        }

        /// <summary>
        /// Schaltet eine Assistentenseite frei oder ab — wörtlich
        /// <c>Wizard_Komponenten.SeiteSchalten</c> (:143-151), ohne die dort
        /// überflüssige Rückzuweisung in die Liste (<c>WizardSeite</c> ist eine
        /// Klasse).
        /// </summary>
        private static void SeiteSchalten(int seitenIndex, bool aktiv)
        {
            IAssistentRahmen rahmen = WizardParent.Aktiver;
            if (rahmen == null) return;
            if (seitenIndex == KomponentenBestandCtrl.OHNE_SEITE) return;
            if (rahmen.Seiten == null || seitenIndex >= rahmen.Seiten.Count) return;

            rahmen.Seiten[seitenIndex].aktiv = aktiv;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
