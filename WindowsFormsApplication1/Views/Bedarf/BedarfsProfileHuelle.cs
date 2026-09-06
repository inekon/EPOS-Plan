using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der drei BEDARFSPROFIL-Masken (iU9-W9.5) — sie löst
    /// <c>Form_Prozesswaerme</c>, <c>Form_Stromverbraucher</c> und
    /// <c>Form_Brauchwasser</c> ab.
    ///
    /// <para><b>Drillinge, eine Hülle.</b> Die drei Masken haben denselben Aufbau und
    /// unterscheiden sich in Titel, Beschriftungen, Zieltabelle, Rechenweg und einer
    /// Handvoll Meldungen. Alles davon hängt an <see cref="BedarfsArt"/>.</para>
    ///
    /// <para><b>Die Hülle RECHNET, die Komponente zeigt.</b> Der Vorläufer hielt ein
    /// lebendes <c>SimulationWaermebedarf</c> bzw. <c>SimulationStrombedarf</c> und gab es
    /// dem Ergebnisdialog in die Hand. Hier bleibt es hier: Die Hülle rechnet, baut den
    /// Parametersatz des Ergebnisdialogs und reicht nur den herein (Risiko R‑W9‑4, wie
    /// W8.2).</para>
    ///
    /// <para><b>Vier Überlagerungen statt vier Fenstern</b> (Risiko R2): Ergebnis (W8.2),
    /// Stammkopf (W8.1) in beiden Modi und Wochen-Stundenprofil (W8.3).</para>
    /// </summary>
    internal static class BedarfsProfileHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1004 × 636 bzw. 964 × 574).</summary>
        private static readonly Size MASS = new Size(1000, 720);

        /// <summary>Die vorläufige Id einer noch nicht gespeicherten Zuordnung.</summary>
        private const int STARTINDEX = 100000;

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>Die PROZESSWÄRME eines Projekts.</summary>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjektProzesswaermeModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektProzesswaermeModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Prozesswaerme,
                    Name = m.szProzessname ?? "", Summe = m.Summe
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektProzesswaermeModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId, ID_Prozesswaerme = z.IdStamm,
                        szProzessname = z.Name, Summe = z.Summe
                    });
            };

            return Zeigen(besitzer, BedarfsArt.Prozesswaerme, projektId, zeilen, geaendert,
                          wizard: false);
        }

        /// <summary>Die STROMVERBRAUCHER eines Projekts.</summary>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjektStromverbraucherModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektStromverbraucherModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.m_ID_Z, IdStamm = m.m_ID_Stromverbraucher,
                    Name = m.m_szVerbraucher ?? "", Summe = m.m_Summe
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektStromverbraucherModel
                    {
                        m_ID_Z = z.IdZ, m_ID_Projekt = projektId, m_ID_Stromverbraucher = z.IdStamm,
                        m_szVerbraucher = z.Name, m_Summe = z.Summe
                    });
            };

            return Zeigen(besitzer, BedarfsArt.Stromverbraucher, projektId, zeilen, geaendert,
                          wizard: false);
        }

        /// <summary>Die BRAUCHWASSERPROFILE eines Projekts.</summary>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjektBrauchwasserModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektBrauchwasserModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Brauchwasser,
                    Name = m.szBezeichner ?? "", Summe = m.Summe
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektBrauchwasserModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId, ID_Brauchwasser = z.IdStamm,
                        szBezeichner = z.Name, Summe = z.Summe
                    });
            };

            return Zeigen(besitzer, BedarfsArt.Brauchwasser, projektId, zeilen, geaendert,
                          wizard: false);
        }

        /// <summary>
        /// Der PARAMETERSATZ der PROZESSWÄRME-Seite des Assistenten (Seite 4).
        ///
        /// <para>iU9-W16a.5: Die Fabrikmethode <c>AssistentSeiteProzess()</c> ist
        /// entfallen — der Assistent ist selbst eine Razor-Seite und braucht kein
        /// randloses WinForms-Formular mehr.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> AssistentGabenProzess(
            int projektId, List<Z_ProjektProzesswaermeModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektProzesswaermeModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Prozesswaerme,
                    Name = m.szProzessname ?? "", Summe = m.Summe
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektProzesswaermeModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId,
                        ID_Prozesswaerme = z.IdStamm,
                        szProzessname = z.Name, Summe = z.Summe
                    });
            };

            return Gaben(null, BedarfsArt.Prozesswaerme, projektId, zeilen, geaendert,
                         wizard: true);
        }

        /// <summary>Der PARAMETERSATZ der STROMVERBRAUCHER-Seite des Assistenten (Seite 5).</summary>
        internal static IReadOnlyDictionary<string, object> AssistentGabenStrom(
            int projektId, List<Z_ProjektStromverbraucherModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektStromverbraucherModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.m_ID_Z, IdStamm = m.m_ID_Stromverbraucher,
                    Name = m.m_szVerbraucher ?? "", Summe = m.m_Summe
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektStromverbraucherModel
                    {
                        m_ID_Z = z.IdZ, m_ID_Projekt = projektId,
                        m_ID_Stromverbraucher = z.IdStamm,
                        m_szVerbraucher = z.Name, m_Summe = z.Summe
                    });
            };

            return Gaben(null, BedarfsArt.Stromverbraucher, projektId, zeilen, geaendert,
                         wizard: true);
        }

        // =================================================================================

        private static bool Zeigen(IWin32Window besitzer, BedarfsArt art, int projektId,
                                   List<BedarfsProfilZeile> zeilen, Action geaendert, bool wizard)
        {
            bool ok = false;
            BlazorDialogForm<BedarfsProfileDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, art, projektId, zeilen, geaendert, wizard))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<BedarfsProfileDialog>(Titel(art), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, BedarfsArt art, int projektId,
            List<BedarfsProfilZeile> zeilen, Action geaendert, bool wizard)
        {
            int[] naechsteId = { STARTINDEX };

            // Das Rechenobjekt gehoert der HUELLE - genau wie im Vorlaeufer, wo es ein Feld
            // der Maske war. "monatlicher Verlauf" zeigt danach denselben Stand noch einmal.
            var rechenstand = new Rechenstand(art, projektId);

            return new Dictionary<string, object>
            {
                ["Art"] = art,
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Geaendert"] = geaendert,

                ["Katalog"] = new Func<IReadOnlyList<BedarfsKatalogZeile>>(() => Katalog(art)),
                ["Info"] = new Func<string, BedarfsProfilInfo>(name => Info(art, name)),
                ["Jahressumme"] = new Func<string, double>(
                    name => BedarfStammCtrl.Jahressumme(art, name)),
                ["Aufnehmen"] = new Func<string, BedarfsProfilZeile>(
                    name => Aufnehmen(art, name, naechsteId)),
                ["KatalogLoeschen"] = new Func<string, bool>(name => KatalogLoeschen(art, name)),
                ["ProjektGespeichert"] = new Func<bool>(() => ProjektCtrl.Existiert(projektId)),
                ["SummeSichern"] = new Action<string, double>(
                    (name, wert) => SummeSichern(art, projektId, name, wert)),

                ["Simulieren"] = new Func<IReadOnlyList<string>, IReadOnlyDictionary<string, object>>(
                    namen => rechenstand.Rechnen(namen)),
                ["ErgebnisGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => rechenstand.LetzterStand()),

                ["TypStammGaben"] =
                    new Func<string, string, string, bool, IReadOnlyDictionary<string, object>>(
                        (name, beschr, typ, istNeu) => TypStammHuelle.Gaben(
                            art, name, istNeu ? "" : beschr, istNeu ? "" : typ,
                            istNeu ? KatalogModus.Neu : KatalogModus.Bearbeiten)),
                ["TypProfilGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => TypStammHuelle.ProfilGaben(art)),

                ["TitelText"] = Titel(art),
                ["KopfbandText"] = Titel(art),
                ["LabelProjektliste"] = Text_(art, "BPF_LBL_PROJEKTLISTE",
                    "Ausgewählte Prozesse im Projekt",
                    "Ausgewählte Strombedarfe im Projekt",
                    "Ausgewählte Profile im Projekt"),
                ["LabelKatalog"] = Text_(art, "BPF_LBL_KATALOG",
                    "Datenbank Prozesswärme", "Datenbank Strombedarf", "Datenbank Profile"),
                ["GruppeInfo"] = TextEinfach("BPF_GRP_INFO", "Profil"),
                ["GruppeVerbrauch"] =
                    TextEinfach("BPF_GRP_VERBRAUCH", "Ändern des Jahresverbrauchs"),
                ["LabelName"] = TextEinfach("BTYP_LBL_NAME", "Name:"),
                ["LabelTyp"] = TextEinfach("BPF_LBL_TYP", "Typ:"),
                ["LabelBeschreibung"] = TextEinfach("BTYP_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelJahresverbrauch"] = Text_(art, "BPF_LBL_JAHRESVERBRAUCH",
                    "jährlicher Prozesswärmebedarf:", "jährlicher Strombedarf:",
                    "jährlicher Wärmebedarf:"),
                ["LabelSumme"] = Text_(art, "BPF_LBL_SUMME",
                    "Summe aller ausgew. Prozesse:",
                    "Summe aller ausgewählten Strombedarfe:",
                    "Summe Brauchwasserprofile:"),
                ["LabelNeuerWert"] = TextEinfach("BPF_LBL_NEUER_WERT", "neuer Wert"),
                ["LabelEinheit"] = TextEinfach("ALLG_LBL_EINHEIT", "Einheit:"),
                // Die Anzeigeeinheit (Entscheid W9-O-3 vom 04.09.2026): MWh als Vorgabe,
                // kWh waehlbar. Sie kommt aus derselben gemerkten Wahl wie die des
                // Ergebnisdialogs - sonst stuende hier MWh und in der Ueberlagerung kWh.
                // Die Summen der Projektzeilen und die Jahressummen des Katalogs liegen
                // in MWh; die Komponente rechnet nur fuer Anzeige und Eingabe um.
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib),
                ["SpalteWahl"] = TextEinfach("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = TextEinfach("BHKWV_SP_NAME", "Name"),
                ["SpalteTyp"] = TextEinfach("BPF_SP_TYP", "Typ"),

                // Entscheid #76 (05.09.2026): Das Zeichen setzt der Baustein
                // Zweispaltenauswahl je nach Anordnung - der Knopf traegt Klartext.
                ["BtnHinzuText"] = TextEinfach("AUSWAHL_BTN_UEBERNEHMEN", "In das Projekt übernehmen"),
                ["BtnEntfernenText"] = TextEinfach("AUSWAHL_BTN_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnDbAendernText"] = Text_(art, "BPF_BTN_DB_AENDERN",
                    "Prozess in DB ändern", "Stromverbraucher ändern...", "Profil in DB ändern"),
                ["BtnDbNeuText"] = Text_(art, "BPF_BTN_DB_NEU",
                    "Prozess in DB neu", "Stromverbraucher neu...", "Profil in DB neu"),
                ["BtnDbLoeschenText"] = Text_(art, "BPF_BTN_DB_LOESCHEN",
                    "Prozess in DB löschen", "Stromverbraucher löschen", "Profil in DB löschen"),
                ["BtnTypAendernText"] = Text_(art, "BPF_BTN_TYP_AENDERN",
                    "Typ in DB ändern", "Typ in DB ändern...", "Typ in DB ändern"),
                ["BtnSimulationText"] = Text_(art, "BPF_BTN_SIMULATION",
                    "Simulation", "Simulation...", "Simulation"),
                ["BtnVerlaufText"] = Text_(art, "BPF_BTN_VERLAUF",
                    "monatlicher Verlauf", "monatlicher Verlauf...", "monatlicher Verlauf"),
                ["BtnUebernehmenText"] = TextEinfach("BPF_BTN_UEBERNEHMEN", "Übernehmen"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["MeldungKeineAuswahl"] = TextEinfach("BPF_MSG_KEINE_AUSWAHL",
                    "Bitte einen Eintrag aus der Liste auswählen!"),
                ["MeldungKeineZeile"] = TextEinfach("BPF_MSG_KEINE_ZEILE",
                    "Bitte einen Eintrag aus der Liste auswählen und einen Wert eingeben!"),
                // W9-B7 ERLEDIGT (Entscheid des Anwenders vom 04.09.2026): Der Bestand
                // nannte beim Stromverbraucher kWh, bei Prozess und Brauchwasser MWh -
                // fuer DIESELBE Groesse. Es gibt jetzt EINEN Text mit einem Platzhalter
                // fuer die Einheit; welche darin steht, entscheidet die Wahl im Dialog.
                ["MeldungWertUngueltig"] = TextEinfach("BPF_MSG_WERT",
                    "Bitte den Jahresverbrauch als Zahl in {0} eingeben, z. B. 12,5."),
                ["MeldungUebernommen"] =
                    TextEinfach("BPF_MSG_UEBERNOMMEN", "Jahresverbrauch übernommen."),
                ["MeldungVorschau"] = TextEinfach("BPF_MSG_VORSCHAU",
                    "Das Projekt ist noch nicht gespeichert. Die Vorschau rechnet deshalb mit " +
                    "den Katalogwerten; der eingegebene Jahresverbrauch wirkt sich erst nach " +
                    "dem Speichern des Projekts auf die Simulation aus."),
                ["MeldungLoeschfrage"] =
                    TextEinfach("BPRO_FRAGE_LOESCHEN", "Soll {0} wirklich gelöscht werden ?"),
                // Nur die Prozessmaske meldete den Erfolg (btn_Prozess_loeschen_Click:491).
                ["MeldungGeloescht"] = art == BedarfsArt.Prozesswaerme
                    ? TextEinfach("BPF_MSG_GELOESCHT", "Prozess erfolgreich gelöscht.") : "",
                ["MeldungNameFehlt"] =
                    TextEinfach("BTYP_MSG_NAME_LEER", "Bitte einen Namen eingeben!"),

                ["HilfeSchluessel"] = HilfeSchluessel(art),

                // H13 (06.09.2026): der zweite Knopf im Kopf - der RECHENWEG der
                // Auspraegung. Er zeigt auf die Rubrik "Programm Dokumentation/
                // Berechnung"; der Fensterknopf daneben bleibt die Bedienhilfe.
                ["HilfeSchluesselBerechnung"] = BerechnungsSchluessel(art),
                ["HilfeKurztextBerechnung"] = BerechnungsKurztext(art)
            };
        }

        // =================================================================================
        // Der Rechenstand - er gehoert der Huelle
        // =================================================================================

        /// <summary>
        /// Hält das Rechenobjekt zwischen „Simulation" und „monatlicher Verlauf" — im
        /// Vorläufer war es ein Feld der Maske.
        ///
        /// <para><b>Die RECHNUNG steht seit dem Befund W8‑B‑3 im Kern</b>
        /// (<c>BedarfsVorschauCtrl.ProjektVorschau</c>, Windows-Abnahme 05.09.2026). Hier
        /// stand sie bis dahin ein zweites Mal, von Hand nachgezogen — und beim Strom
        /// fehlte darin die Zeile, die <c>Strombedarf_Gebaeude_gesamt</c> belegt: Die
        /// Ergebnisanzeige zeigte „Gesamter Strombedarf 0" und „Strombedarf Gebäude 0"
        /// neben einem gerechneten Spitzenwert von 3,72 kW. Die Hülle HÄLT den Stand
        /// jetzt nur noch; gerechnet wird einmal, im Kern.</para>
        /// </summary>
        private sealed class Rechenstand
        {
            private readonly BedarfsArt _art;
            private readonly int _projektId;
            private BedarfsVorschau _stand;
            private string _titelZusatz = "";

            internal Rechenstand(BedarfsArt art, int projektId)
            {
                _art = art;
                _projektId = projektId;
            }

            internal IReadOnlyDictionary<string, object> Rechnen(IReadOnlyList<string> namen)
            {
                BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(_art, _projektId, namen);
                if (!v.Erfolgreich) return null;

                _stand = v;

                // Nur der Brauchwasserweg haengt den Profilnamen an den Fenstertitel
                // (Form_Brauchwasser.btn_Berechnen_Click:308).
                _titelZusatz = (_art == BedarfsArt.Brauchwasser && namen != null && namen.Count > 0)
                    ? namen[0] : "";

                return LetzterStand();
            }

            /// <summary>
            /// Derselbe Stand noch einmal — „monatlicher Verlauf". Die Startreiter sind
            /// wörtlich die der Vorläufer: 1 bei Prozess und Strom, 2 (Grafik samt
            /// Brauchwassersicht) beim Brauchwasser.
            /// </summary>
            internal IReadOnlyDictionary<string, object> LetzterStand()
            {
                if (_stand == null) return null;

                switch (_art)
                {
                    case BedarfsArt.Stromverbraucher:
                        return BedarfErgebnisHuelle.Gaben(_stand.Strom, 1);
                    case BedarfsArt.Prozesswaerme:
                        return BedarfErgebnisHuelle.Gaben(_stand.Waerme, false, 1, "");
                    default:
                        return BedarfErgebnisHuelle.Gaben(_stand.Waerme, true, 2, _titelZusatz);
                }
            }
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        private static IReadOnlyList<BedarfsKatalogZeile> Katalog(BedarfsArt art)
        {
            var zeilen = new List<BedarfsKatalogZeile>();

            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    var s = new StromverbraucherStammCtrl();
                    s.ReadAll();
                    for (int i = 0; i < s.rows; i++)
                        zeilen.Add(new BedarfsKatalogZeile(s.items[i].m_szBezeichner ?? "",
                                                           s.items[i].m_szTyp ?? ""));
                    break;

                case BedarfsArt.Prozesswaerme:
                    var p = new ProzesswaermeStammCtrl();
                    p.ReadAll();
                    for (int i = 0; i < p.rows; i++)
                        zeilen.Add(new BedarfsKatalogZeile(p.items[i].m_szProzessname ?? "",
                                                           p.items[i].m_szTyp ?? ""));
                    break;

                default:
                    var b = new BrauchwasserStammCtrl();
                    b.ReadAll();
                    for (int i = 0; i < b.rows; i++)
                        zeilen.Add(new BedarfsKatalogZeile(b.items[i].m_szBezeichner ?? "",
                                                           b.items[i].m_szTyp ?? ""));
                    break;
            }
            return zeilen;
        }

        private static BedarfsProfilInfo Info(BedarfsArt art, string name)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    var s = new StromverbraucherStammCtrl();
                    s.ReadSingle(name);
                    return s.rows > 0
                        ? new BedarfsProfilInfo(name, s.m_szBeschreibung ?? "", s.m_szTyp ?? "")
                        : null;

                case BedarfsArt.Prozesswaerme:
                    var p = new ProzesswaermeStammCtrl();
                    p.ReadSingle(name);
                    return p.rows > 0
                        ? new BedarfsProfilInfo(name, p.m_szBeschreibung ?? "", p.m_szTyp ?? "")
                        : null;

                default:
                    var b = new BrauchwasserStammCtrl();
                    b.ReadSingle(name);
                    return b.rows > 0
                        ? new BedarfsProfilInfo(name, b.m_szBeschreibung ?? "", b.m_szTyp ?? "")
                        : null;
            }
        }

        /// <summary>
        /// „◀" — die Stamm-Id per Name, die Summe als Σ der zwölf Monatswerte
        /// (<c>btn_Hinzu_Click</c> der drei Vorläufer).
        /// </summary>
        private static BedarfsProfilZeile Aufnehmen(BedarfsArt art, string name, int[] naechsteId)
        {
            int idStamm = DataRepository.GetIdByName(BedarfStammCtrl.KopfTabelle(art),
                                                     "Bezeichner", name);
            if (idStamm <= 0) return null;

            return new BedarfsProfilZeile
            {
                IdZ = naechsteId[0]++,      // noch nicht gespeichert, also noch unbekannt
                IdStamm = idStamm,
                Name = name ?? "",
                Summe = BedarfStammCtrl.Jahressumme(art, name)
            };
        }

        private static bool KatalogLoeschen(BedarfsArt art, string name)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return new StromverbraucherStammCtrl().Delete(name);
                case BedarfsArt.Prozesswaerme:    return new ProzesswaermeStammCtrl().Delete(name);
                default:                          return new BrauchwasserStammCtrl().Delete(name);
            }
        }

        private static void SummeSichern(BedarfsArt art, int projektId, string name, double wert)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    new Z_ProjektStromverbraucherCtrl().UpdateSumme(wert, name, projektId);
                    break;
                case BedarfsArt.Prozesswaerme:
                    new Z_ProjektProzesswaermeCtrl().UpdateSumme(wert, name, projektId);
                    break;
                default:
                    new Z_ProjektBrauchwasserCtrl().UpdateSumme(wert, name, projektId);
                    break;
            }
        }

        // =================================================================================
        // Texte
        // =================================================================================

        internal static string Titel(BedarfsArt art)
        {
            return Text_(art, "BPF_TITEL", "Prozesswärme", "Standard Stromprofil",
                         "Brauchwasserwärme");
        }

        private static string HilfeSchluessel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return "Form_Stromverbraucher.btn_Help";
                case BedarfsArt.Prozesswaerme:    return "Form_Prozesswaerme.btn_Help";
                default:                          return "Form_Brauchwasser.btn_Help";
            }
        }

        /// <summary>
        /// H13 — der Schlüssel des RECHENWEG-Knopfes je Ausprägung. Dieselben drei
        /// Präfixe wie oben, nur mit dem Nachnamen <c>.Berechnung</c>; die Ziele stehen
        /// in <c>help_mapping.txt</c>, Abschnitt „H13 - Rubrik Berechnung".
        /// </summary>
        private static string BerechnungsSchluessel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return "Form_Stromverbraucher.Berechnung";
                case BedarfsArt.Prozesswaerme:    return "Form_Prozesswaerme.Berechnung";
                default:                          return "Form_Brauchwasser.Berechnung";
            }
        }

        /// <summary>
        /// Der Kurztext am Rechenweg-Knopf, solange der Hilfekatalog den Schlüssel nicht
        /// kennt (die Wikiseiten sind erst anzulegen). Kennt er ihn, gewinnt der Tooltip
        /// des Katalogs — „Berechnung: Brauchwasser".
        /// </summary>
        private static string BerechnungsKurztext(BedarfsArt art)
        {
            return Text_(art, "BPF_HILFE_BERECHNUNG",
                         "Berechnungsweg: Prozesswärme",
                         "Berechnungsweg: Strombedarf",
                         "Berechnungsweg: Brauchwasser");
        }

        /// <summary>
        /// Ein Text je Ausprägung. Der Ressourcenschlüssel bekommt das Kürzel der
        /// Ausprägung angehängt (<c>_PROZ</c>, <c>_STROM</c>, <c>_BW</c>).
        /// </summary>
        private static string Text_(BedarfsArt art, string schluessel,
                                    string prozess, string strom, string brauchwasser)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return TextEinfach(schluessel + "_STROM", strom);
                case BedarfsArt.Prozesswaerme:    return TextEinfach(schluessel + "_PROZ", prozess);
                default:                          return TextEinfach(schluessel + "_BW", brauchwasser);
            }
        }

        private static string TextEinfach(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
