using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Übernahme ins Projekt" (iU9-W1.4).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente
    /// <see cref="VorlagenUebernahmeDialog"/> kennt keine Datenbank (Hausregel
    /// <c>EPOS.UI/CLAUDE.md</c>). Alles, was sie zeigt, wird hier geladen — mit
    /// denselben Controllern und denselben Filterparametern wie zuvor in
    /// <c>Form_VorlagenUebernahme.SetControls</c> (Regel F4) —, und alles, was
    /// sie auslöst, wird hier gerechnet und geschrieben:
    /// <c>KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt</c> für die Vorschau,
    /// <c>AusVorlage</c> bzw. <c>AusProjekt</c> für den Lauf.</para>
    ///
    /// <para><b>Drei Delegaten statt eines Zustands.</b> Die Anlagenliste hängt am
    /// gewählten Quellprojekt, die Vorschau an der ganzen Wahl, und der Lauf
    /// ebenfalls. Statt der Komponente die Zählwege zu geben, gibt die Hülle ihr
    /// drei Funktionen — dieselbe Bauweise wie
    /// <c>BhkwWirtschaftlichkeitHuelle</c> (K7, Speichern als <c>Func&lt;int&gt;</c>).</para>
    /// </summary>
    internal static class VorlagenUebernahmeHuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W4.2). Bis Welle 3 zeigte diese
        /// Hülle ein eigenes Fenster; seit die Kostenverwaltung selbst eine
        /// Razor-Komponente ist, erscheint der Übernahmedialog in einer
        /// <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe WebView
        /// (Risiko R2). Geblieben ist die Datenseite: dieselben Controller mit
        /// denselben Filtern wie zuvor <c>Form_VorlagenUebernahme.SetControls</c>.
        ///
        /// <para><c>Geschlossen</c> steht bewusst NICHT im Satz — den Rückruf
        /// setzt der Wirt, der auch das Fenster hält.</para>
        /// </summary>
        /// <param name="komponentenId">Kostenkomponente (Ä7-Auswahl).</param>
        /// <param name="komponentenName">Anzeigename der Komponente — zugleich der
        /// Schlüssel, mit dem die Quell-Anlagenliste gefiltert wird (Ä21).</param>
        /// <param name="kategorieId">Investition oder Betrieb.</param>
        /// <param name="vorlage">VORAUSWAHL der Quellvorlage; <c>null</c> = die erste
        /// (Ä11: zur Auswahl stehen immer alle Vorlagen des Katalogs).</param>
        /// <param name="zielProjektId">&gt; 0 = das Ziel steht fest (Projektmodus).</param>
        /// <param name="zielAnlageId">Ziel-Anlage der Übernahme (Ä20); 0 = ohne Bezug.</param>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int komponentenId, string komponentenName, int kategorieId,
            KostenVorlageKopf vorlage, int zielProjektId = 0, int zielAnlageId = 0)
        {
            string name = komponentenName ?? "";

            // Ä11: Vorlagenliste des Admin-Katalogs als wählbare Quelle.
            IList<KostenVorlageKopf> vorlagen = KostenVorlagenCtrl.Vorlagen(komponentenId, kategorieId);
            var vorlagenEintraege = new List<ValueTuple<int, string>>();
            foreach (KostenVorlageKopf v in vorlagen)
                vorlagenEintraege.Add(new ValueTuple<int, string>(v.Id, v.Name));

            IList<KeyValuePair<int, string>> projekte = KostenVorlagenUebernahmeCtrl.Projekte();
            var projektEintraege = new List<ValueTuple<int, string>>();
            foreach (KeyValuePair<int, string> p in projekte)
                projektEintraege.Add(new ValueTuple<int, string>(p.Key, p.Value + "  [" + p.Key + "]"));

            // Projektmodus: Das Ziel IST das geöffnete Projekt — keine Umwahl; die
            // Quelle startet dann sinnvollerweise beim eigenen Projekt.
            int? zielVorwahl = zielProjektId > 0 ? (int?)zielProjektId : null;
            int? quellProjektVorwahl = zielProjektId > 0 ? (int?)zielProjektId : null;
            int? quellVorlageVorwahl = vorlage != null ? (int?)vorlage.Id : null;

            return new Dictionary<string, object>
            {
                ["KontextText"] = name + " · " +
                    (kategorieId == KostenSummenCtrl.KATEGORIE_BETRIEB
                        ? Text_("KDLG_KAT_BETRIEB", "Betriebskosten")
                        : Text_("KDLG_KAT_INVEST", "Investitionskosten")),

                ["Zielprojekte"] = (IReadOnlyList<ValueTuple<int, string>>)projektEintraege,
                ["ZielProjektVorwahl"] = zielVorwahl,
                ["ZielWaehlbar"] = zielProjektId <= 0,

                ["Quellvorlagen"] = (IReadOnlyList<ValueTuple<int, string>>)vorlagenEintraege,
                ["QuellVorlageVorwahl"] = quellVorlageVorwahl,

                ["Quellprojekte"] = (IReadOnlyList<ValueTuple<int, string>>)projektEintraege,
                ["QuellProjektVorwahl"] = quellProjektVorwahl,

                ["AnlagenZu"] = new Func<int, IReadOnlyList<ValueTuple<int, string>>>(
                    projekt => Quellanlagen(projekt, komponentenId, name, kategorieId)),

                ["Vorschau"] = new Func<VorlagenUebernahmeWahl, VorlagenUebernahmeVorschau>(
                    wahl => Vorschau(wahl, komponentenId, kategorieId, zielAnlageId)),

                ["Uebernehmen"] = new Func<VorlagenUebernahmeWahl, VorlagenUebernahmeAntwort>(
                    wahl => Uebernehmen(wahl, vorlagen, komponentenId, kategorieId, zielAnlageId)),

                ["TitelText"] = Text_("KUEB_TITEL", "Übernahme ins Projekt"),
                ["LabelZielProjekt"] = Text_("KUEB_LBL_ZIEL", "Zielprojekt:"),
                ["LabelQuelleVorlage"] = Text_("KDLG_UEB_QUELLE_VORLAGE", "Aus Vorlage/Variante:"),
                ["LabelQuelleProjekt"] = Text_("KDLG_UEB_QUELLE_PROJEKT", "Aus Projekt/Anlage:"),
                ["LabelQuellVorlage"] = Text_("KUEB_LBL_QUELLVORLAGE", "Vorlage/Variante:"),
                ["LabelQuellProjekt"] = Text_("KUEB_LBL_QUELLPROJEKT", "Quellprojekt:"),
                ["LabelQuellAnlage"] = Text_("KUEB_LBL_QUELLANLAGE", "Quellanlage:"),
                ["UebernehmenText"] = Text_("KDLG_ET_BTN_UEBERNEHMEN", "Übernehmen"),
                ["SchliessenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        /// <summary>
        /// Die Anlagen der Komponente im gewählten Quellprojekt — plus
        /// „(ohne Anlagenzuordnung)", wenn dort lose Positionen liegen.
        /// Wortgleich aus <c>Form_VorlagenUebernahme.QuellAnlagenFuellen</c> (Ä21).
        /// </summary>
        private static IReadOnlyList<ValueTuple<int, string>> Quellanlagen(
            int projekt, int komponentenId, string komponentenName, int kategorieId)
        {
            var liste = new List<ValueTuple<int, string>>();
            if (projekt <= 0) return liste;

            foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in
                     ProjektEnergietraegerCtrl.AnlagenMitTraeger(projekt))
            {
                if (!string.Equals(a.Komponente, komponentenName, StringComparison.Ordinal))
                    continue;
                string text = string.IsNullOrEmpty(a.Bezeichner)
                    ? a.Komponente : a.Komponente + " — " + a.Bezeichner;
                liste.Add(new ValueTuple<int, string>(a.AnlageId, text));
            }

            int lose = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                           projekt, komponentenId, kategorieId, 0) +
                       KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                           projekt, komponentenId,
                           kategorieId == KostenSummenCtrl.KATEGORIE_BETRIEB
                               ? KostenSummenCtrl.KATEGORIE_INVESTITION
                               : KostenSummenCtrl.KATEGORIE_BETRIEB, 0);
            if (lose > 0 || liste.Count == 0)
                liste.Add(new ValueTuple<int, string>(0,
                    Text_("KDLG_UEB_QUELLE_LOSE", "(ohne Anlagenzuordnung)")));

            return liste;
        }

        /// <summary>
        /// Klartext-Vorschau (§ 8 Nr. 3) — nur Zählen, kein Schreiben. Wortgleich aus
        /// <c>VorschauAktualisieren</c>, samt der Ä21-Regel, dass Ziel und Quelle
        /// ANLAGENBEZOGEN gezählt werden, und samt der Bedingung des
        /// Übernehmen-Knopfes.
        /// </summary>
        private static VorlagenUebernahmeVorschau Vorschau(VorlagenUebernahmeWahl wahl,
                                                           int komponentenId, int kategorieId,
                                                           int zielAnlageId)
        {
            if (wahl.ZielProjektId <= 0) return new VorlagenUebernahmeVorschau("", false);

            int vorhanden = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                wahl.ZielProjektId, komponentenId, kategorieId, zielAnlageId > 0 ? zielAnlageId : -1);

            int quelle = wahl.AusVorlage
                ? (wahl.QuellVorlageId > 0 ? KostenVorlagenCtrl.Positionen(wahl.QuellVorlageId).Count : 0)
                : KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                      wahl.QuellProjektId, komponentenId, kategorieId, wahl.QuellAnlageId);

            string text = string.Format(
                Text_("KDLG_UEB_VORSCHAU",
                    "Die Quelle enthält {0} Positionen. Das Zielprojekt führt für diese " +
                    "Komponente bereits {1} Positionen — vorhandene bleiben unberührt, nur " +
                    "fehlende werden angelegt. Die Herkunft wird je Position vermerkt."),
                quelle, vorhanden);

            bool moeglich = quelle > 0 &&
                (wahl.AusVorlage || wahl.QuellProjektId != wahl.ZielProjektId ||
                 (wahl.QuellAnlageId != zielAnlageId &&
                  (wahl.QuellAnlageId > 0 || zielAnlageId > 0)));

            return new VorlagenUebernahmeVorschau(text, moeglich);
        }

        /// <summary>Der Schreibweg — wortgleich aus <c>btnUebernehmen_Click</c>.</summary>
        private static VorlagenUebernahmeAntwort Uebernehmen(VorlagenUebernahmeWahl wahl,
                                                             IList<KostenVorlageKopf> vorlagen,
                                                             int komponentenId, int kategorieId,
                                                             int zielAnlageId)
        {
            UebernahmeErgebnis ergebnis;
            if (wahl.AusVorlage)
            {
                KostenVorlageKopf quelle = null;
                foreach (KostenVorlageKopf v in vorlagen)
                    if (v.Id == wahl.QuellVorlageId) { quelle = v; break; }
                ergebnis = KostenVorlagenUebernahmeCtrl.AusVorlage(
                    wahl.ZielProjektId, quelle, zielAnlageId);
            }
            else
            {
                ergebnis = KostenVorlagenUebernahmeCtrl.AusProjekt(
                    wahl.ZielProjektId, wahl.QuellProjektId, komponentenId, kategorieId,
                    wahl.QuellAnlageId, zielAnlageId);
            }

            return new VorlagenUebernahmeAntwort(
                ergebnis.Fehler,
                string.Join(Environment.NewLine, ergebnis.Meldungen));
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
