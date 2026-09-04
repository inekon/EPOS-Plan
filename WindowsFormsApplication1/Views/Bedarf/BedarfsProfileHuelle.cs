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

        /// <summary>Die PROZESSWÄRME-Seite des Assistenten (Seite 4).</summary>
        internal static Form AssistentSeiteProzess()
        {
            return new BlazorAssistentSeite<BedarfsProfileDialog, Z_ProjektProzesswaermeModel>(
                (projektId, projektName, modelle) =>
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

                    return new Dictionary<string, object>(
                        Gaben(null, BedarfsArt.Prozesswaerme, projektId, zeilen, geaendert,
                              wizard: true));
                },
                MASS);
        }

        /// <summary>Die STROMVERBRAUCHER-Seite des Assistenten (Seite 5).</summary>
        internal static Form AssistentSeiteStrom()
        {
            return new BlazorAssistentSeite<BedarfsProfileDialog, Z_ProjektStromverbraucherModel>(
                (projektId, projektName, modelle) =>
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

                    return new Dictionary<string, object>(
                        Gaben(null, BedarfsArt.Stromverbraucher, projektId, zeilen, geaendert,
                              wizard: true));
                },
                MASS);
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

                ["BtnHinzuText"] = "◀",
                ["BtnEntfernenText"] = "▶",
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

                ["HilfeSchluessel"] = HilfeSchluessel(art)
            };
        }

        // =================================================================================
        // Der Rechenstand - er gehoert der Huelle
        // =================================================================================

        /// <summary>
        /// Hält das Rechenobjekt zwischen „Simulation" und „monatlicher Verlauf" — im
        /// Vorläufer war es ein Feld der Maske.
        /// </summary>
        private sealed class Rechenstand
        {
            private readonly BedarfsArt _art;
            private readonly int _projektId;
            private readonly SimulationWaermebedarf _waerme = new SimulationWaermebedarf();
            private readonly SimulationStrombedarf _strom = new SimulationStrombedarf();
            private string _titelZusatz = "";
            private bool _gerechnet;

            internal Rechenstand(BedarfsArt art, int projektId)
            {
                _art = art;
                _projektId = projektId;
            }

            internal IReadOnlyDictionary<string, object> Rechnen(IReadOnlyList<string> namen)
            {
                var liste = new List<string>(namen);

                if (_art == BedarfsArt.Stromverbraucher)
                {
                    _strom.m_ID_Projekt = _projektId;
                    float[] ergebnis = _strom.Stromprofil_Strombedarf_berechnen(liste);
                    if (ergebnis == null) return null;

                    _strom.Strombedarf_gesamt = ergebnis.Sum();
                    Array.Copy(ergebnis, _strom.Strombedarf_viertelStundenwerte, ergebnis.Length);
                    WPPlan.Core.BhkwPlan.MonatsSumme(_strom.Strombedarf_viertelStundenwerte,
                        _strom.Strombedarf_monat, _strom.mo_anfang, _strom.mo_ende);
                    _strom.Strombedarf_Max =
                        _strom.Maximaler_Strombedarf(_strom.Strombedarf_viertelStundenwerte);
                    _strom.Strombedarf_gesamt = _strom.Strombedarf_Gebaeude_gesamt;

                    _gerechnet = true;
                    return LetzterStand();
                }

                _waerme.m_ID_Projekt = _projektId;

                if (_art == BedarfsArt.Prozesswaerme)
                {
                    _waerme.Prozesswaerme_berechnen(liste);

                    // W9-O-3, Entscheid des Anwenders vom 04.09.2026: Die Prozesssumme
                    // geht ueber die EINHEITENKLASSE in die Einheit, die der Kern fuer
                    // Waermebedarf_Prozess fuehrt - MWh. Hier stand bis hierher die
                    // blanke Summe der Stundenwerte, also kWh, woertlich aus
                    // Form_Prozesswaerme uebernommen; die Ergebnishuelle liest das Feld
                    // aber als MWh (BedarfErgebnisHuelle: Energieeinheit.MWh), und
                    // "Waermebedarf Prozess" stand in diesem Weg um Faktor 1000 zu gross.
                    // Der Kern (SimulationWaermebedarf.Waermebedarf_berechnen) und die
                    // Prozesswaerme-Verwaltung setzten das Feld schon immer in MWh.
                    _waerme.ProzesssummeUebernehmen();

                    WPPlan.Core.BhkwPlan.MonatsSumme(_waerme.prozesswerte,
                        _waerme.Waermebedarf_Prozess_Monat, _waerme.mo_anfang, _waerme.mo_ende);
                }
                else
                {
                    _waerme.Brauchwasserwaerme_berechnen(liste);

                    // BEWUSST unveraendert - und damit NICHT symmetrisch zum Zweig
                    // darueber: Waermebedarf_Brauchwasser liegt hier in kWh, und genau so
                    // nimmt die Ergebnishuelle es an (Energieeinheit.KWh, Entscheid
                    // W8-O-5). Der Weg zeigt heute die richtige Zahl; ihn auf MWh zu
                    // heben hiesse, die Annahme der Huelle mitzudrehen - und die gilt
                    // auch fuer den Simulationsweg, der das Feld aus dem Kern schon in
                    // MWh bekommt und es deshalb ein zweites Mal teilt. Diese
                    // Unstimmigkeit ist als W8-O-5b notiert; sie braucht einen eigenen
                    // Anwenderentscheid und wird hier nicht nebenbei mitentschieden.
                    _waerme.Waermebedarf_Brauchwasser = _waerme.brauchwasserwerte.Sum();
                    WPPlan.Core.BhkwPlan.MonatsSumme(_waerme.brauchwasserwerte,
                        _waerme.Waermebedarf_Brauchwasser_Monat, _waerme.mo_anfang, _waerme.mo_ende);
                    _titelZusatz = liste.Count > 0 ? liste[0] : "";
                }

                _gerechnet = true;
                return LetzterStand();
            }

            /// <summary>
            /// Derselbe Stand noch einmal — „monatlicher Verlauf". Die Startreiter sind
            /// wörtlich die der Vorläufer: 1 bei Prozess und Strom, 2 (Grafik samt
            /// Brauchwassersicht) beim Brauchwasser.
            /// </summary>
            internal IReadOnlyDictionary<string, object> LetzterStand()
            {
                if (!_gerechnet) return null;

                switch (_art)
                {
                    case BedarfsArt.Stromverbraucher:
                        return BedarfErgebnisHuelle.Gaben(_strom, 1);
                    case BedarfsArt.Prozesswaerme:
                        return BedarfErgebnisHuelle.Gaben(_waerme, false, 1, "");
                    default:
                        return BedarfErgebnisHuelle.Gaben(_waerme, true, 2, _titelZusatz);
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
