using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Kostenverwaltung (iU9-W4.2) — Nachfolge der
    /// gelöschten Maske <c>Views/Kosten/Form_KostenKomponente</c> (918 Z.).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente
    /// <see cref="KostenKomponenteDialog"/> kennt keine Datenbank (Hausregel
    /// <c>EPOS.UI/CLAUDE.md</c>). Sie fragt mit einem
    /// <see cref="KostenKomponenteKontext"/> und bekommt einen
    /// <see cref="KostenKomponenteStand"/>; alles darin wird hier mit denselben
    /// Controllern und denselben Filtern geladen wie zuvor in
    /// <c>Kontext_Geaendert</c>, <c>VariantenLaden</c>, <c>RasterAufbauen</c>,
    /// <c>SummenAnzeigen</c> und <c>ErtragReiterSteuern</c> (Regel F4).</para>
    ///
    /// <para><b>Die Ä12/Ä19-Semantik bleibt.</b> Eine Feldänderung lebt bis
    /// „Speichern"/„OK" nur im Objekt (<see cref="KostenVorlagenPosition"/>);
    /// „Abbrechen" und jeder Kontextwechsel verwerfen sie. Anlegen, Löschen,
    /// Zeileneditor und Worst/Best schreiben weiter sofort — sie haben eigene
    /// Bestätigungen.</para>
    ///
    /// <para><b>Ein Fenster, eine WebView.</b> Die fünf Unterdialoge (Worst/Best,
    /// Zeileneditor, Namensabfrage, Übernahme, Kostenfaktor-Katalog) sind seit
    /// den Wellen 1 bis 3 Razor-Komponenten. Diese Hülle baut nur noch ihre
    /// Parametersätze; gezeigt werden sie in einer <c>Ueberlagerung</c>
    /// innerhalb desselben Fensters (W4.0, Risiko R2).</para>
    /// </summary>
    internal sealed class KostenKomponenteHuelle
    {
        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 1004 × 721;
        /// das Positionsraster braucht seine Breite (Befund 03.09.2026:
        /// Tabellen ohne Umbruch).</summary>
        private static readonly System.Drawing.Size FENSTER = new System.Drawing.Size(1100, 800);

        // ---- Kontext -------------------------------------------------------

        private readonly int _idProjekt;
        private readonly string _projektname;
        private bool ProjektModus { get { return _idProjekt > 0; } }

        private IList<KeyValuePair<int, string>> _komponenten;
        private List<ProjektEnergietraegerCtrl.AnlagenEintrag> _anlagenListe;

        /// <summary>Ä20: ein Eintrag der Klappliste — im Stammkontext eine
        /// Komponente, im Projektmodus eine Anlagenzeile.</summary>
        private sealed class Eintrag
        {
            public string Komponente;
            public int AnlageId;
            public string Text;
        }

        private readonly List<Eintrag> _eintraege = new List<Eintrag>();
        private int _eintragIndex;

        // ---- Aktueller Stand -----------------------------------------------

        private IList<KostenVorlageKopf> _varianten = new List<KostenVorlageKopf>();
        private KostenVorlageKopf _variante;
        private bool _invest = true;

        private readonly List<BemessungKatalog.Info> _bemessungen = new List<BemessungKatalog.Info>();

        /// <summary>Die Bindung einer Anzeigezeile an ihre Fachobjekte.</summary>
        private sealed class Bindung
        {
            public KostenVorlagenPosition Position;
            public KostenProjektPositionenCtrl.Zeile Projektzeile;   // null im Stammkontext
        }

        private readonly Dictionary<KostenPositionZeile, Bindung> _bindungen =
            new Dictionary<KostenPositionZeile, Bindung>();

        private List<KostenPositionZeile> _zeilen = new List<KostenPositionZeile>();

        private KostenKomponenteHuelle(int idProjekt, string projektname)
        {
            _idProjekt = idProjekt > 0 ? idProjekt : 0;
            _projektname = projektname ?? "";
        }

        // =====================================================================
        // Einstiege
        // =====================================================================

        /// <summary>
        /// Stammkontext (Katalogpflege) — Nachfolge von
        /// <c>new Form_KostenKomponente()</c> samt <c>SetControls</c>/<c>WaehleBetrieb</c>.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="komponente">Vorwahl der Komponente; <c>null</c> = die erste.</param>
        /// <param name="betrieb"><c>true</c> = auf die Betriebskostensicht schalten.</param>
        internal static void Oeffnen(IWin32Window besitzer, string komponente = null,
                                     bool betrieb = false)
        {
            new KostenKomponenteHuelle(0, "").Zeigen(besitzer, komponente, betrieb, 0);
        }

        /// <summary>
        /// PROJEKTMODUS (KD6a) — Nachfolge von <c>SetProjekt</c>: derselbe Dialog
        /// pflegt die <c>Tab_ProjektWerte</c>-Positionen des Projekts.
        /// </summary>
        internal static void OeffnenProjekt(IWin32Window besitzer, int idProjekt, string projektname,
                                            string komponente = null, bool betrieb = false,
                                            int idAnlage = 0)
        {
            new KostenKomponenteHuelle(idProjekt, projektname)
                .Zeigen(besitzer, komponente, betrieb, idAnlage);
        }

        /// <summary>
        /// Der PARAMETERSATZ der Kostenverwaltung im PROJEKTMODUS (iU9-W5.4).
        /// Seit die Kostenseite selbst eine Razor-Komponente ist, erscheint sie
        /// in einer <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe
        /// WebView (Risiko R2). <c>Geschlossen</c> setzt der Wirt.
        ///
        /// <para>Die Hüllen-INSTANZ hält den Bearbeitungsstand; sie lebt über
        /// die Rückrufe des Satzes so lange wie der Bereich.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> GabenProjekt(
            int idProjekt, string projektname, string komponente = null,
            bool betrieb = false, int idAnlage = 0)
        {
            string titel;
            return new KostenKomponenteHuelle(idProjekt, projektname)
                .GabenIntern(komponente, betrieb, idAnlage, out titel);
        }

        private void Zeigen(IWin32Window besitzer, string komponente, bool betrieb, int idAnlage)
        {
            BlazorDialogForm<KostenKomponenteDialog> dlg = null;

            string titel;
            var werte = new Dictionary<string, object>(
                GabenIntern(komponente, betrieb, idAnlage, out titel))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<KostenKomponenteDialog>(titel, FENSTER, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        private IReadOnlyDictionary<string, object> GabenIntern(
            string komponente, bool betrieb, int idAnlage, out string titel)
        {
            _komponenten = KostenVorlagenCtrl.Komponenten();
            int vorwahl = EintraegeBauen(idAnlage, komponente);
            _invest = !betrieb;

            var eintraege = new List<ValueTuple<int, string>>();
            for (int i = 0; i < _eintraege.Count; i++)
                eintraege.Add(new ValueTuple<int, string>(i, _eintraege[i].Text));

            titel = ProjektModus
                ? string.Format(T("KDLG_TITEL_PROJEKT", "Kostenverwaltung {0} — {1}"),
                                _eintraege.Count > 0 ? _eintraege[Math.Max(0, vorwahl)].Text : "", _projektname)
                : T("KDLG_TITEL", "Kostenverwaltung {0}").Replace(" {0}", "");

            return new Dictionary<string, object>
            {
                ["Eintraege"] = (IReadOnlyList<ValueTuple<int, string>>)eintraege,
                ["EintragVorwahl"] = vorwahl >= 0 ? (int?)vorwahl : null,
                ["InvestVorwahl"] = !betrieb,

                ["Laden"] = new Func<KostenKomponenteKontext, KostenKomponenteStand>(Stand),
                ["Nachziehen"] = new Action<KostenPositionZeile>(Nachziehen),
                ["Summen"] = new Func<IReadOnlyList<ValueTuple<string, bool>>>(Summen),
                ["Speichern"] = new Func<bool>(Speichern),
                ["PositionNeu"] = new Func<string, int>(PositionNeu),
                ["PositionLoeschen"] = new Func<KostenPositionZeile, bool>(PositionLoeschen),
                ["IstPflicht"] = new Func<KostenPositionZeile, bool>(IstPflicht),

                ["CaseGaben"] = new Func<KostenPositionZeile, IReadOnlyDictionary<string, object>>(CaseGaben),
                ["CaseFertig"] = new Action<KostenPositionZeile, CaseEingabeErgebnis>(CaseFertig),
                ["EditorGaben"] = new Func<KostenPositionZeile, IReadOnlyDictionary<string, object>>(EditorGaben),
                ["EditorFertig"] = new Action<KostenPositionZeile, VorlagenPositionErgebnis>(EditorFertig),
                ["VariantenGaben"] = new Func<bool, IReadOnlyDictionary<string, object>>(VariantenGaben),
                ["VarianteNeu"] = new Func<bool, string, int>(VarianteNeu),
                ["VarianteLoeschen"] = new Func<bool>(VarianteLoeschen),
                ["VarianteIstStandard"] = new Func<bool>(VarianteIstStandard),
                ["UebernahmeGaben"] = new Func<IReadOnlyDictionary<string, object>>(UebernahmeGaben),
                ["KatalogGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => KostenfaktorKatalogHuelle.Gaben()),

                // iU9-W14c.3: die SECHSTE Ueberlagerung. Bis dahin sprang das
                // Reiterblatt "Ertrag/Bonus" ueber die Sprungbruecke in das
                // WinForms-Fenster Form_Gesetzesparameter; das Ziel ist jetzt selbst
                // Razor (Risiko R2). Ohne diese Gaben bleibt der Knopf im Blatt weg.
                ["GesetzeGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => GesetzeskatalogHuelle.Gaben()),

                ["BannerText"] = T("KDLG_BANNER", "Alle Beträge und alle Bezugsgrößen sind NETTO."),
                ["BannerZuKurztext"] = T("KKOMP_BANNER_ZU", "Hinweis ausblenden"),
                ["LabelKomponente"] = T("KDLG_LBL_KOMPONENTE", "Komponente:"),
                ["KategorieInvestText"] = T("KDLG_KAT_INVEST", "Investitionskosten"),
                ["KategorieBetriebText"] = T("KDLG_KAT_BETRIEB", "Betriebskosten"),
                ["LabelVariante"] = T("KDLG_LBL_VARIANTE", "Variante:"),
                ["VarianteNeuText"] = T("KDLG_BTN_NEU", "Neu…"),
                ["SpeichernUnterText"] = T("KDLG_BTN_SPEICHERN_UNTER", "Speichern unter…"),
                ["VarianteLoeschenKurztext"] = T("KKOMP_TT_VARIANTE_LOESCHEN", "Variante löschen"),
                ["ReadOnlyHinweis"] = T("KDLG_READONLY_HINWEIS",
                    "Auslieferungsvorlage (schreibgeschützt) — zum Ändern „Speichern unter…\" verwenden."),
                ["LabelRaster"] = T("KKOMP_RASTER", "Positionen"),
                ["SpalteAktionen"] = T("KDLG_SP_AKTIONEN", "Aktionen"),
                ["SpaltePosition"] = T("KDLG_SP_POSITION", "Position"),
                ["SpalteBemessung"] = T("KDLG_SP_BEMESSUNG", "Bemessung"),
                ["SpalteSatz"] = T("KDLG_SP_SATZ", "Satz"),
                ["SpalteNutzung"] = T("KDLG_SP_NUTZUNG", "Nutzung [a]"),
                ["SpalteWorstBest"] = T("KDLG_SP_WORSTBEST", "Worst/Best"),
                ["NeuPlatzhalter"] = "+ " + T("KDLG_POS_NEU_VORGABE", "Neue Position") + "…",
                ["PositionNeuText"] = T("KDLG_BTN_POSITION", "+ Position hinzufügen"),
                ["PositionNeuVorgabe"] = T("KDLG_POS_NEU_VORGABE", "Neue Position"),
                ["UebernahmeText"] = ProjektModus
                    ? T("KDLG_BTN_UEBERNAHME_PROJEKT", "Aus Vorlage übernehmen…")
                    : T("KDLG_BTN_UEBERNAHME", "In Projekt übernehmen…"),
                ["KatalogText"] = T("KDLG_BTN_KATALOG", "Positionskatalog…"),
                ["EditorKurztext"] = T("KKOMP_TT_EDITOR", "Position bearbeiten"),
                ["ZeileLoeschenKurztext"] = T("KKOMP_TT_ZEILE_LOESCHEN", "Position löschen"),
                ["WorstBestKurztext"] = T("KDLG_TT_WORSTBEST",
                    "Worst/Best wird je Projektposition gepflegt, nicht in der Stammvorlage."),
                ["KetteKurztext"] = T("KDLG_TT_KETTE",
                    "Satz und Betrag netto sind verknüpft und werden bei Eingabe umgerechnet."),
                ["SpeichernText"] = T("KDLG_BTN_SPEICHERN", "Speichern"),
                ["AbbrechenText"] = T("KDLG_BTN_ABBRECHEN", "Abbrechen"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["JaText"] = T("KKOMP_BTN_JA", "Ja"),
                ["NeinText"] = T("KKOMP_BTN_NEIN", "Nein"),
                ["FrageTitel"] = T("KDLG_TITEL", "Kostenverwaltung {0}").Replace(" {0}", ""),
                ["CaseTitel"] = T("KCASE_TITEL", "Eingabe Worst/Best Case"),
                ["EditorTitel"] = T("VPOS_TITEL", "Position bearbeiten"),
                ["VarianteTitel"] = T("KDLG_MSG_NEU_TITEL", "Neue Variante"),
                ["UebernahmeTitel"] = T("KUEB_TITEL", "Übernahme ins Projekt"),
                ["KatalogTitel"] = T("KFAK_TITEL", "Administration Kostenfaktoren"),
                ["VorlagePositionLoeschen"] = T("KDLG_MSG_POS_LOESCHEN", "Position „{0}\" löschen?"),
                ["VorlagePflichtLoeschen"] = T("KDLG_MSG_PFLICHT_LOESCHEN",
                    "„{0}\" ist eine Pflichtposition dieser Komponente und kann nicht gelöscht werden.\r\n"
                    + "Zum Deaktivieren den Satz bzw. Betrag auf 0 setzen."),
                ["VorlageVarianteLoeschen"] = T("KDLG_MSG_LOESCHEN", "Variante „{0}\" wirklich löschen?"),
                ["MeldungStandardLoeschen"] = T("KDLG_MSG_STANDARD_LOESCHEN",
                    "Die Standardvorlage kann nicht gelöscht werden — Varianten schon."),
                ["MeldungNameBelegt"] = T("KDLG_MSG_NAME_BELEGT",
                    "Der Name ist bereits vergeben oder leer."),
                ["VorlageGespeichert"] = T("KDLG_GESPEICHERT", "gespeichert {0:HH:mm} Uhr")
                    .Replace("{0:HH:mm}", "{0}")
            };
        }

        // =====================================================================
        // Klappliste (Ä20)
        // =====================================================================

        /// <summary>
        /// Baut die Klapplisteneinträge und liefert den Index der Vorwahl (−1 =
        /// keine). Wortgleich aus <c>AnlagenlisteFuellen</c> bzw. — im
        /// Stammkontext — aus dem Konstruktor der gelöschten Maske.
        /// </summary>
        private int EintraegeBauen(int vorwahlAnlage, string vorwahlKomponente)
        {
            _eintraege.Clear();

            if (!ProjektModus)
            {
                int treffer = -1;
                foreach (KeyValuePair<int, string> k in _komponenten)
                {
                    if (treffer < 0 && vorwahlKomponente != null &&
                        string.Equals(k.Value, vorwahlKomponente, StringComparison.Ordinal))
                        treffer = _eintraege.Count;
                    _eintraege.Add(new Eintrag { Komponente = k.Value, AnlageId = 0, Text = k.Value });
                }
                return treffer;
            }

            // Ä21: verwaiste Zuordnungen zuerst heilen (Wizard-Neuaufbau).
            try { KostenProjektPositionenCtrl.ZuordnungReparieren(_idProjekt); } catch { }
            _anlagenListe = ProjektEnergietraegerCtrl.AnlagenMitTraeger(_idProjekt);
            HashSet<string> lose = LoseKomponenten();

            var mitAnlage = new HashSet<string>(StringComparer.Ordinal);
            int vorwahl = -1;

            foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in _anlagenListe)
            {
                if (!KostenVorlagenCtrl.IstWaehlbar(a.Komponente)) continue;   // Ä7
                mitAnlage.Add(a.Komponente);
                string text = string.IsNullOrEmpty(a.Bezeichner)
                    ? a.Komponente : a.Komponente + " — " + a.Bezeichner;
                if (vorwahl < 0 &&
                    ((vorwahlAnlage > 0 && a.AnlageId == vorwahlAnlage) ||
                     (vorwahlAnlage <= 0 && vorwahlKomponente != null &&
                      string.Equals(a.Komponente, vorwahlKomponente, StringComparison.Ordinal))))
                    vorwahl = _eintraege.Count;
                _eintraege.Add(new Eintrag { Komponente = a.Komponente, AnlageId = a.AnlageId, Text = text });
            }

            foreach (KeyValuePair<int, string> k in _komponenten)
            {
                bool hatAnlagen = mitAnlage.Contains(k.Value);
                bool hatLose = lose.Contains(k.Value);
                if (hatAnlagen && !hatLose) continue;
                string text = hatLose
                    ? string.Format(T("KDLG_ANLAGE_LOSE", "{0} (ohne Anlagenzuordnung)"), k.Value)
                    : string.Format(T("KDLG_ANLAGE_KEINE", "{0} (keine Anlage im Projekt)"), k.Value);
                if (vorwahl < 0 && vorwahlAnlage <= 0 && vorwahlKomponente != null &&
                    string.Equals(k.Value, vorwahlKomponente, StringComparison.Ordinal))
                    vorwahl = _eintraege.Count;
                _eintraege.Add(new Eintrag { Komponente = k.Value, AnlageId = 0, Text = text });
            }

            return vorwahl;
        }

        /// <summary>Komponenten, die Positionen ohne (gültige) Anlagenzuordnung
        /// führen — wortgleich aus <c>LoseKomponenten</c>.</summary>
        private HashSet<string> LoseKomponenten()
        {
            var s = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                var ids = new HashSet<int>();
                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in _anlagenListe)
                    ids.Add(a.AnlageId);
                foreach (int kat in new[] { KostenSummenCtrl.KATEGORIE_INVESTITION,
                                            KostenSummenCtrl.KATEGORIE_BETRIEB })
                {
                    System.Data.DataTable t = KostenSummenCtrl.LiesAnlagenSummen(_idProjekt, kat);
                    if (t == null) continue;
                    foreach (System.Data.DataRow r in t.Rows)
                    {
                        bool loseZeile = r["ID_Anlage"] == DBNull.Value ||
                                         !ids.Contains(Convert.ToInt32(r["ID_Anlage"]));
                        if (loseZeile) s.Add(Convert.ToString(r["Komponente"]));
                    }
                }
            }
            catch { }
            return s;
        }

        private string AktuelleKomponente
        {
            get
            {
                return _eintragIndex >= 0 && _eintragIndex < _eintraege.Count
                    ? _eintraege[_eintragIndex].Komponente : "";
            }
        }

        private int AnlagenId
        {
            get
            {
                return _eintragIndex >= 0 && _eintragIndex < _eintraege.Count
                    ? _eintraege[_eintragIndex].AnlageId : 0;
            }
        }

        private int KomponentenId
        {
            get
            {
                string name = AktuelleKomponente;
                foreach (KeyValuePair<int, string> k in _komponenten)
                    if (string.Equals(k.Value, name, StringComparison.Ordinal)) return k.Key;
                return 0;
            }
        }

        private int KategorieId
        {
            get
            {
                return _invest ? KostenSummenCtrl.KATEGORIE_INVESTITION
                               : KostenSummenCtrl.KATEGORIE_BETRIEB;
            }
        }

        // =====================================================================
        // Der Stand zu einem Kontext
        // =====================================================================

        /// <summary>
        /// Die eine Antwort auf einen Kontextwechsel — im Bestand
        /// <c>Kontext_Geaendert</c> mit seinen fünf Folgemethoden.
        /// </summary>
        private KostenKomponenteStand Stand(KostenKomponenteKontext kontext)
        {
            _eintragIndex = kontext.EintragId ?? 0;
            _invest = kontext.Invest;

            var stand = new KostenKomponenteStand
            {
                Titel = ProjektModus
                    ? string.Format(T("KDLG_TITEL_PROJEKT", "Kostenverwaltung {0} — {1}"),
                                    _eintragIndex >= 0 && _eintragIndex < _eintraege.Count
                                        ? _eintraege[_eintragIndex].Text : AktuelleKomponente,
                                    _projektname)
                    : string.Format(T("KDLG_TITEL", "Kostenverwaltung {0}"), AktuelleKomponente),
                Untertitel = _invest
                    ? T("KDLG_UNTERTITEL_INVEST", "Investitionskosten nach VDI 2067")
                    : T("KDLG_UNTERTITEL_BETRIEB", "Betriebskosten nach VDI 2067"),
                SpalteBetrag = _invest
                    ? T("KDLG_SP_BETRAG", "Betrag netto [€]")
                    : T("KDLG_SP_BETRAG_JAHR", "Betrag netto [€/a]"),
                MitNutzungsdauer = _invest,
                MitWorstBest = ProjektModus,
                VariantePflegbar = !ProjektModus
            };

            if (ProjektModus) ProjektRasterAufbauen(stand);
            else VorlagenRasterAufbauen(stand, kontext.VarianteId);

            stand.Bemessungen = BemessungenBauen();
            stand.Summen = Summen();
            ErtragSetzen(stand);
            return stand;
        }

        /// <summary>Stammkontext: Varianten laden und das Raster daraus bauen.</summary>
        private void VorlagenRasterAufbauen(KostenKomponenteStand stand, int? varianteId)
        {
            _varianten = KostenVorlagenCtrl.Vorlagen(KomponentenId, KategorieId);

            var eintraege = new List<ValueTuple<int, string>>();
            foreach (KostenVorlageKopf v in _varianten)
                eintraege.Add(new ValueTuple<int, string>(v.Id, v.Name));
            stand.Varianten = eintraege;

            _variante = null;
            foreach (KostenVorlageKopf v in _varianten)
                if (varianteId.HasValue && v.Id == varianteId.Value) { _variante = v; break; }
            if (_variante == null && _varianten.Count > 0) _variante = _varianten[0];
            stand.VarianteId = _variante != null ? (int?)_variante.Id : null;

            bool nurLesen = _variante == null || _variante.NurLesen;
            stand.NurLesen = _variante != null && _variante.NurLesen;
            stand.PositionNeuMoeglich = _variante != null && !_variante.NurLesen;
            stand.VarianteLoeschbar = _variante != null && !_variante.NurLesen;

            _bindungen.Clear();
            _zeilen = new List<KostenPositionZeile>();
            if (_variante != null)
            {
                foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(_variante.Id))
                {
                    KostenPositionZeile z = ZeileBauen(p, null, !nurLesen);
                    _zeilen.Add(z);
                }
            }
            stand.Zeilen = _zeilen;
        }

        /// <summary>Projektzweig (KD6a) — wortgleich aus <c>ProjektRasterAufbauen</c>.</summary>
        private void ProjektRasterAufbauen(KostenKomponenteStand stand)
        {
            stand.Varianten = Array.Empty<ValueTuple<int, string>>();
            stand.VarianteId = null;
            stand.PositionNeuMoeglich = KomponentenId > 0;
            stand.VarianteLoeschbar = false;

            List<KostenProjektPositionenCtrl.Zeile> zeilen = KomponentenId > 0
                ? KostenProjektPositionenCtrl.Lies(_idProjekt, KomponentenId, KategorieId, AnlagenId)
                : new List<KostenProjektPositionenCtrl.Zeile>();

            _bindungen.Clear();
            _zeilen = new List<KostenPositionZeile>();
            foreach (KostenProjektPositionenCtrl.Zeile pz in zeilen)
                _zeilen.Add(ZeileBauen(pz.Raster, pz, true));
            stand.Zeilen = _zeilen;
        }

        /// <summary>Eine Anzeigezeile aus einer Position — wortgleich aus
        /// <c>ucVorlagenZeile.Zeige</c> samt Kopplung und Empfehlung.</summary>
        private KostenPositionZeile ZeileBauen(KostenVorlagenPosition p,
                                               KostenProjektPositionenCtrl.Zeile pz,
                                               bool schreibbar)
        {
            var z = new KostenPositionZeile
            {
                Id = p.Id,
                Bezeichnung = p.Bezeichnung ?? "",
                Satz = p.Satz,
                Nutzungsdauer = p.Nutzungsdauer,
                Schreibbar = schreibbar
            };
            _bindungen[z] = new Bindung { Position = p, Projektzeile = pz };

            BemessungKatalog.Info info = BemessungInfo(p.Bemessung);
            z.BemessungId = info != null ? (int?)BemessungIndex(info) : null;
            z.Einheit = info != null ? info.Einheit : "";
            KopplungAnwenden(z, p, info);
            z.EmpfehlungKurztext = EmpfehlungText(p, z.Einheit);
            return z;
        }

        /// <summary>Betragsfeld nach Kopplungsregel (KL4/§ 5.4) — wortgleich aus
        /// <c>ucVorlagenZeile.KopplungAnwenden</c>.</summary>
        private void KopplungAnwenden(KostenPositionZeile z, KostenVorlagenPosition p,
                                      BemessungKatalog.Info info)
        {
            bool absolut = info != null && info.Absolut;
            if (absolut)
            {
                z.BetragText = ZahlText(z.Satz);
                z.Kette = true;
                z.BetragKurztext = T("KDLG_TT_KETTE",
                    "Satz und Betrag netto sind verknüpft und werden bei Eingabe umgerechnet.");
            }
            else if (ProjektModus)
            {
                z.BetragText = p != null ? ZahlText(p.BetragNetto) : "";
                z.Kette = false;
                z.BetragKurztext = T("KDLG_TT_BETRAG_PROJEKT",
                    "Aus Satz und Bezugsgröße des Projekts berechnet.");
            }
            else
            {
                z.BetragText = "—";
                z.Kette = false;
                z.BetragKurztext = T("KDLG_TT_BETRAG_ADMIN",
                    "Bezugsgröße erst im Projekt bekannt — der Betrag entsteht bei der Übernahme.");
            }
        }

        private static string EmpfehlungText(KostenVorlagenPosition p, string einheit)
        {
            if (p == null || (!p.EmpfehlungVon.HasValue && !p.EmpfehlungBis.HasValue)) return "";
            return "Empfehlung: " + ZahlText(p.EmpfehlungVon) + " – " +
                   ZahlText(p.EmpfehlungBis) + " " + einheit;
        }

        // =====================================================================
        // Bemessungen
        // =====================================================================

        /// <summary>
        /// Die wählbaren Bemessungen dieses Kontexts — wortgleich aus
        /// <c>BemessungenFuellen</c>: gefiltert nach Invest/Betrieb, dazu jede
        /// Bemessung, die eine vorhandene Zeile bereits trägt (sonst verlöre eine
        /// Bestandsposition beim Anzeigen ihren Wert).
        /// </summary>
        private IReadOnlyList<ValueTuple<int, string>> BemessungenBauen()
        {
            _bemessungen.Clear();
            var benutzt = new HashSet<string>(StringComparer.Ordinal);
            foreach (KostenPositionZeile z in _zeilen)
            {
                Bindung b;
                if (_bindungen.TryGetValue(z, out b) && b.Position.Bemessung != null)
                    benutzt.Add(b.Position.Bemessung);
            }

            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                if ((_invest && i.FuerInvest) || (!_invest && i.FuerBetrieb) || benutzt.Contains(i.Persistenz))
                    _bemessungen.Add(i);

            var liste = new List<ValueTuple<int, string>>();
            for (int n = 0; n < _bemessungen.Count; n++)
                liste.Add(new ValueTuple<int, string>(n, BemessungKatalog.Anzeige(_bemessungen[n].Persistenz)));

            // Die Ids der Zeilen zeigen auf diese Liste — sie werden erst hier gültig.
            foreach (KostenPositionZeile z in _zeilen)
            {
                Bindung b;
                if (!_bindungen.TryGetValue(z, out b)) continue;
                BemessungKatalog.Info info = BemessungInfo(b.Position.Bemessung);
                z.BemessungId = info != null ? (int?)BemessungIndex(info) : null;
            }
            return liste;
        }

        private static BemessungKatalog.Info BemessungInfo(string persistenz)
        {
            if (string.IsNullOrEmpty(persistenz)) return null;
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                if (string.Equals(i.Persistenz, persistenz, StringComparison.Ordinal)) return i;
            return null;
        }

        private int BemessungIndex(BemessungKatalog.Info info)
        {
            for (int n = 0; n < _bemessungen.Count; n++)
                if (ReferenceEquals(_bemessungen[n], info)) return n;
            return -1;
        }

        private BemessungKatalog.Info BemessungAus(int? id)
        {
            return id.HasValue && id.Value >= 0 && id.Value < _bemessungen.Count
                ? _bemessungen[id.Value] : null;
        }

        // =====================================================================
        // Feldänderung, Summen, Speichern
        // =====================================================================

        /// <summary>
        /// Nach jeder Feldänderung: Felder in die Position übernehmen und die
        /// Kopplung neu anwenden — wortgleich aus <c>FelderUebernehmen</c> und
        /// <c>KopplungAnwenden</c>. Geschrieben wird NICHT (Ä12/Ä19).
        /// </summary>
        private void Nachziehen(KostenPositionZeile z)
        {
            Bindung b;
            if (z == null || !_bindungen.TryGetValue(z, out b)) return;
            KostenVorlagenPosition p = b.Position;

            string name = (z.Bezeichnung ?? "").Trim();
            if (name.Length > 0) p.Bezeichnung = name;
            else z.Bezeichnung = p.Bezeichnung;   // leerer Name: zurücksetzen

            BemessungKatalog.Info info = BemessungAus(z.BemessungId);
            if (info != null)
            {
                p.Bemessung = info.Persistenz;
                z.Einheit = info.Einheit;
            }

            p.Satz = z.Satz;
            p.Nutzungsdauer = _invest ? z.Nutzungsdauer : p.Nutzungsdauer;

            // KL4/§ 5.4: absolut ⇒ Satz und Betrag sind EIN Wert; sonst bleibt der
            // Betrag im Stammkontext leer (Bezugsgröße erst im Projekt).
            if (info != null && info.Absolut) p.BetragNetto = p.Satz;
            else p.BetragNetto = null;

            KopplungAnwenden(z, p, info);
            z.EmpfehlungKurztext = EmpfehlungText(p, z.Einheit);
        }

        /// <summary>
        /// Der Summenfuß (§ 5.2) — wortgleich aus <c>SummenAnzeigen</c>: nur
        /// absolute Positionen tragen einen Betrag, Erlöse mit negativem Ausweis
        /// (L7); Brutto ist reine Anzeige (KL5).
        /// </summary>
        private IReadOnlyList<ValueTuple<string, bool>> Summen()
        {
            double netto = 0;
            foreach (KostenPositionZeile z in _zeilen)
            {
                Bindung b;
                if (!_bindungen.TryGetValue(z, out b)) continue;
                KostenVorlagenPosition p = b.Position;
                if (!p.BetragNetto.HasValue) continue;
                netto += p.IstErloes ? -p.BetragNetto.Value : p.BetragNetto.Value;
            }

            var liste = new List<ValueTuple<string, bool>>();
            string nettoText = netto.ToString("#,##0.00", CultureInfo.CurrentCulture);
            liste.Add(new ValueTuple<string, bool>(string.Format(_invest
                    ? T("KDLG_SUMME_NETTO_INVEST", "Summe Investitionskosten netto: {0} €")
                    : T("KDLG_SUMME_NETTO_BETRIEB", "Summe Betriebskosten netto: {0} €/a"),
                nettoText), true));

            double? ust = KostenVorlagenCtrl.UstSatzProzent();
            if (ust.HasValue)
            {
                double brutto = netto * (1.0 + ust.Value / 100.0);
                string bruttoText = brutto.ToString("#,##0.00", CultureInfo.CurrentCulture) +
                                    (_invest ? " €" : " €/a");
                liste.Add(new ValueTuple<string, bool>(string.Format(
                    T("KDLG_SUMME_BRUTTO", "Summe brutto: {0} (Umsatzsteuer {1} % aus dem Katalog)"),
                    bruttoText, ust.Value.ToString("0.#", CultureInfo.CurrentCulture)), false));
            }
            return liste;
        }

        /// <summary>Ä12/Ä19: Der ausdrückliche Speicherbefehl schreibt alle Zeilen.</summary>
        private bool Speichern()
        {
            bool alles = true;
            foreach (KostenPositionZeile z in _zeilen)
            {
                Bindung b;
                if (!_bindungen.TryGetValue(z, out b) || !z.Schreibbar) continue;
                if (!Sichern(b)) alles = false;
            }
            return alles;
        }

        private bool Sichern(Bindung b)
        {
            return b.Projektzeile != null
                ? KostenProjektPositionenCtrl.Speichern(b.Projektzeile)
                : KostenVorlagenCtrl.PositionSpeichern(b.Position);
        }

        // =====================================================================
        // Positionen
        // =====================================================================

        /// <summary>Wortgleich aus <c>btnPositionNeu_Click</c> bzw.
        /// <c>ucVorlagenZeile.NeuAnlegenVersuchen</c>.</summary>
        private int PositionNeu(string name)
        {
            string kostenart = _invest ? DbWerte.KOSTENART_KAPITALGEBUNDEN
                                       : DbWerte.KOSTENART_BETRIEBSGEBUNDEN;
            string bemessung = _invest ? DbWerte.BEMESSUNG_BETRAG
                                       : DbWerte.BEMESSUNG_JAHRESBETRAG;

            if (ProjektModus)
                return KostenProjektPositionenCtrl.Neu(_idProjekt, KomponentenId, KategorieId,
                                                       name, kostenart, bemessung, AnlagenId);

            return _variante == null ? 0
                : KostenVorlagenCtrl.PositionNeu(_variante.Id, name, kostenart, bemessung);
        }

        private bool PositionLoeschen(KostenPositionZeile z)
        {
            Bindung b;
            if (z == null || !_bindungen.TryGetValue(z, out b)) return false;
            return ProjektModus
                ? KostenProjektPositionenCtrl.Loeschen(b.Position.Id)
                : KostenVorlagenCtrl.PositionLoeschen(b.Position.Id);
        }

        /// <summary>H3 (H1-2): Pflichtpositionen (Schritt 59) sind nicht löschbar.</summary>
        private bool IstPflicht(KostenPositionZeile z)
        {
            Bindung b;
            if (z == null || !_bindungen.TryGetValue(z, out b)) return false;
            return ProjektModus && KostenProjektPositionenCtrl.IstPflicht(b.Position.Id);
        }

        // =====================================================================
        // Worst/Best (W1.3) und Zeileneditor (W1.1)
        // =====================================================================

        /// <summary>
        /// Die Parameter des KD6-Dialogs — wortgleich aus
        /// <c>Zeile_WorstBestAngefordert</c>, OHNE Zuschuss-Schalter (Befund A-6
        /// aus Welle 1: dieser Aufrufer las <c>IstZuschuss</c> nie zurück).
        /// </summary>
        private IReadOnlyDictionary<string, object> CaseGaben(KostenPositionZeile z)
        {
            Bindung b;
            if (z == null || !_bindungen.TryGetValue(z, out b) || b.Projektzeile == null)
                return new Dictionary<string, object>();

            KostenProjektPositionenCtrl.Zeile pz = b.Projektzeile;
            return new Dictionary<string, object>
            {
                ["Betrag"] = b.Position.BetragNetto ?? 0,
                ["BestCase"] = pz.Best,
                ["WorstCase"] = pz.Worst,
                ["BestNutzungsdauer"] = pz.BestNutzung,
                ["WorstNutzungsdauer"] = pz.WorstNutzung,
                ["StartJahr"] = pz.StartJahr,
                ["IstZuschuss"] = false,
                ["ZuschussMoeglich"] = false,
                ["IstErloes"] = b.Position.IstErloes,

                ["TitelText"] = T("KCASE_TITEL", "Eingabe Worst/Best Case"),
                ["LabelAbsolut"] = T("KOSTEN_CASE_ABSOLUT", "Eingabe absolut [€]"),
                ["LabelProzent"] = T("KOSTEN_CASE_PROZENT", "Eingabe in % vom Erwartungswert"),
                ["VorlageUmrechnung"] = T("KOSTEN_CASE_UMRECHNUNG",
                    "ergibt: Best {0:N2} € · Worst {1:N2} €"),
                ["LabelKosten"] = T("KCASE_G_KOSTEN", "Kosten:"),
                ["LabelNutzungsdauer"] = T("KCASE_G_NUTZUNGSDAUER", "Nutzungsdauer:"),
                ["LabelBestKosten"] = T("KCASE_BEST_EUR", "Best Case [€]:"),
                ["LabelWorstKosten"] = T("KCASE_WORST_EUR", "Worst Case [€]:"),
                ["LabelBestNutzung"] = T("KCASE_BEST_A", "Best Case [a]:"),
                ["LabelWorstNutzung"] = T("KCASE_WORST_A", "Worst Case [a]:"),
                ["LabelStartJahr"] = T("KOSTEN_CASE_STARTJAHR",
                    "Startjahr (0 = sofort; Jahr X: Zahlung/Betrieb ab X):"),
                ["LabelZuschuss"] = MyResource.Resource.KOSTEN_CHK_ZUSCHUSS,
                ["HinweisZuschuss"] = MyResource.Resource.KOSTEN_CHK_ZUSCHUSS_HINT,
                ["HinweisErloes"] = T("KCASE_ERLOES_HINWEIS",
                    "Erlösposition: Die Werte werden als Betrag eingegeben; das negative "
                    + "Vorzeichen setzt die Rechnung."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        private void CaseFertig(KostenPositionZeile z, CaseEingabeErgebnis e)
        {
            Bindung b;
            if (z == null || e == null || !_bindungen.TryGetValue(z, out b) ||
                b.Projektzeile == null) return;

            KostenProjektPositionenCtrl.Zeile pz = b.Projektzeile;
            pz.Best = e.BestCase;
            pz.Worst = e.WorstCase;
            pz.BestNutzung = e.BestNutzungsdauer;
            pz.WorstNutzung = e.WorstNutzungsdauer;
            pz.StartJahr = e.StartJahr;
            KostenProjektPositionenCtrl.CaseSichern(pz);
        }

        /// <summary>
        /// Die Kostenarten nach VDI 2067 in der Reihenfolge der Klappliste —
        /// wortgleich aus der gelöschten Maske <c>Form_VorlagenPosition</c>
        /// (iU9-W1.1). Der Index in diesem Feld IST die <c>KostenartId</c>.
        /// </summary>
        private static readonly string[] KOSTENARTEN =
        {
            DbWerte.KOSTENART_KAPITALGEBUNDEN,
            DbWerte.KOSTENART_BEDARFSGEBUNDEN,
            DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
            DbWerte.KOSTENART_SONSTIGE,
            DbWerte.KOSTENART_ZUSCHUSS,
        };

        private static string KostenartAnzeige(string persistenz)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString("KOSTENART_" + persistenz); }
            catch { }
            if (!string.IsNullOrEmpty(t)) return t;
            switch (persistenz)
            {
                case "KAPITALGEBUNDEN": return "kapitalgebunden";
                case "BEDARFSGEBUNDEN": return "bedarfsgebunden";
                case "BETRIEBSGEBUNDEN": return "betriebsgebunden";
                case "ZUSCHUSS": return "Zuschuss";
                default: return "sonstige";
            }
        }

        /// <summary>Wortgleich aus <c>Zeile_EditorAngefordert</c>.</summary>
        private IReadOnlyDictionary<string, object> EditorGaben(KostenPositionZeile z)
        {
            Bindung b;
            if (z == null || !_bindungen.TryGetValue(z, out b))
                return new Dictionary<string, object>();

            var eintraege = new List<ValueTuple<int, string>>();
            for (int i = 0; i < KOSTENARTEN.Length; i++)
                eintraege.Add(new ValueTuple<int, string>(i, KostenartAnzeige(KOSTENARTEN[i])));

            int vorwahl = Array.IndexOf(KOSTENARTEN, b.Position.Kostenart ?? "");

            return new Dictionary<string, object>
            {
                ["Kostenarten"] = (IReadOnlyList<ValueTuple<int, string>>)eintraege,
                ["Bezeichnung"] = b.Position.Bezeichnung ?? "",
                ["KostenartId"] = vorwahl >= 0 ? (int?)vorwahl : null,
                ["IstErloes"] = b.Position.IstErloes,
                ["EmpfehlungVon"] = b.Position.EmpfehlungVon,
                ["EmpfehlungBis"] = b.Position.EmpfehlungBis,

                ["TitelText"] = T("VPOS_TITEL", "Position bearbeiten"),
                ["LabelBezeichnung"] = T("VPOS_LBL_BEZEICHNUNG", "Bezeichnung:"),
                ["LabelKostenart"] = T("VPOS_LBL_KOSTENART", "Kostenart:"),
                ["LabelErloes"] = T("VPOS_CHK_ERLOES", "Erlös/Zuschuss (negativer Ausweis)"),
                ["LabelEmpfehlungVon"] = T("VPOS_LBL_EMPFEHLUNG", "Empfehlung von/bis:"),
                ["LabelEmpfehlungBis"] = T("VPOS_LBL_BIS", "bis"),
                ["MeldungNameFehlt"] = T("VPOS_MSG_NAME_FEHLT", "Bitte eine Bezeichnung eingeben."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        /// <summary>Wortgleich aus <c>btnOk_Click</c> der gelöschten Maske
        /// <c>Form_VorlagenPosition</c>: eintragen und SOFORT schreiben (Ä12).</summary>
        private void EditorFertig(KostenPositionZeile z, VorlagenPositionErgebnis e)
        {
            Bindung b;
            if (z == null || e == null || !_bindungen.TryGetValue(z, out b)) return;

            KostenVorlagenPosition p = b.Position;
            p.Bezeichnung = e.Bezeichnung;
            p.Kostenart = KOSTENARTEN[Math.Max(0, Math.Min(KOSTENARTEN.Length - 1, e.KostenartId))];
            p.IstErloes = e.IstErloes;
            p.EmpfehlungVon = e.EmpfehlungVon;
            p.EmpfehlungBis = e.EmpfehlungBis;
            Sichern(b);
        }

        // =====================================================================
        // Varianten (W1.2)
        // =====================================================================

        /// <summary>FK9: Namensschema „‹Name› — Variante ‹n›" als Vorbelegung.</summary>
        private IReadOnlyDictionary<string, object> VariantenGaben(bool kopie)
        {
            string basis = _eintragIndex >= 0 && _eintragIndex < _eintraege.Count
                ? _eintraege[_eintragIndex].Text : "";
            string vorschlag = basis + " — Variante " + _varianten.Count;

            return NamensDialogHuelle.Gaben(
                kopie ? T("KDLG_MSG_KOPIE_TITEL", "Speichern unter")
                      : T("KDLG_MSG_NEU_TITEL", "Neue Variante"),
                T("KDLG_MSG_NEU_NAME", "Name der neuen Variante:"),
                vorschlag,
                T("NAMD_MSG_LEER", "Bitte einen Namen eingeben."));
        }

        private int VarianteNeu(bool kopie, string name)
        {
            if (kopie && _variante == null) return 0;
            return kopie
                ? KostenVorlagenCtrl.SpeichernUnter(_variante.Id, name)
                : KostenVorlagenCtrl.VorlageNeu(KomponentenId, KategorieId, name);
        }

        private bool VarianteLoeschen()
        {
            return _variante != null && KostenVorlagenCtrl.VorlageLoeschen(_variante.Id);
        }

        private bool VarianteIstStandard()
        {
            return _variante != null && _variante.IstStandard;
        }

        // =====================================================================
        // Übernahme (W1.4)
        // =====================================================================

        /// <summary>
        /// KD3 (§ 8). Ä11: Im Projektmodus steht das Ziel fest; die Quellvorlage
        /// wählt der Dialog. Ä20: übernommen wird in die GEWÄHLTE Anlage.
        /// </summary>
        private IReadOnlyDictionary<string, object> UebernahmeGaben()
        {
            return VorlagenUebernahmeHuelle.Gaben(KomponentenId, AktuelleKomponente, KategorieId,
                                                  ProjektModus ? null : _variante,
                                                  ProjektModus ? _idProjekt : 0,
                                                  ProjektModus ? AnlagenId : 0);
        }

        // =====================================================================
        // Ertrag/Bonus (KD5)
        // =====================================================================

        /// <summary>
        /// FK5: Der Abschnitt „Ertrag/Bonus" existiert nur für BHKW und
        /// Photovoltaik — bei allen übrigen Komponenten erscheint er nicht.
        /// </summary>
        private void ErtragSetzen(KostenKomponenteStand stand)
        {
            string name = AktuelleKomponente;
            stand.ErtragSichtbar = ErtragBonusGaben.HatInhalt(name);
            stand.ErtragGaben = stand.ErtragSichtbar ? ErtragBonusGaben.Bauen(name) : null;
        }

        // =====================================================================
        // Kleinwerkzeug
        // =====================================================================

        private static string ZahlText(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString("0.##", CultureInfo.CurrentCulture) : "";
        }

        private static string T(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
