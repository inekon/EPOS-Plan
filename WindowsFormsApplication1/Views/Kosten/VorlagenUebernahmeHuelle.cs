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
        ///
        /// <para><b>ANWENDERENTSCHEID 10.09.2026 (Ä25):</b> Der Primärknopf heißt jetzt
        /// OK, übernimmt und schließt; der zweite bricht ab. Für die Datenseite ändert
        /// das nichts — dieselben Controller, dieselben Filter —, wohl aber für den
        /// Rückruf: <c>Geschlossen(true)</c> heißt seither „übernommen", nicht mehr
        /// „irgendwann während der Sitzung einmal übernommen".</para>
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

            // ANWENDERENTSCHEID 19.09.2026: Der Katalogblock zeigt BEIDE Kategorien —
            // der Anwender wählt darin wie in der Administration. Deshalb werden auch
            // beide Variantenlisten geladen; sie tragen zugleich die Vorwahlregel
            // („nur die andere Kategorie führt eine") und den Kopf für den Schreibweg.
            IList<KostenVorlageKopf> vorlagenInvest =
                KostenVorlagenCtrl.Vorlagen(komponentenId, KostenSummenCtrl.KATEGORIE_INVESTITION);
            IList<KostenVorlageKopf> vorlagenBetrieb =
                KostenVorlagenCtrl.Vorlagen(komponentenId, KostenSummenCtrl.KATEGORIE_BETRIEB);
            bool investStart = kategorieId != KostenSummenCtrl.KATEGORIE_BETRIEB;

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

                ["InvestVorwahl"] = investStart,
                ["QuellVorlageVorwahl"] = quellVorlageVorwahl,

                ["VorlagenZu"] = new Func<bool, IReadOnlyList<ValueTuple<int, string>>>(
                    invest => Variantenliste(invest ? vorlagenInvest : vorlagenBetrieb)),

                ["PositionenZu"] = new Func<VorlagenUebernahmeWahl, IReadOnlyList<VorlagenPositionZeile>>(
                    wahl => Positionsvorschau(wahl, vorlagenInvest, vorlagenBetrieb,
                                              komponentenId, zielAnlageId)),

                ["Quellprojekte"] = (IReadOnlyList<ValueTuple<int, string>>)projektEintraege,
                ["QuellProjektVorwahl"] = quellProjektVorwahl,

                ["AnlagenZu"] = new Func<int, IReadOnlyList<ValueTuple<int, string>>>(
                    projekt => Quellanlagen(projekt, komponentenId, name, kategorieId)),

                ["Vorschau"] = new Func<VorlagenUebernahmeWahl, VorlagenUebernahmeVorschau>(
                    wahl => Vorschau(wahl, komponentenId, kategorieId, zielAnlageId)),

                ["Uebernehmen"] = new Func<VorlagenUebernahmeWahl, VorlagenUebernahmeAntwort>(
                    wahl => Uebernehmen(wahl, vorlagenInvest, vorlagenBetrieb,
                                        komponentenId, kategorieId, zielAnlageId)),

                // ANWENDERENTSCHEID 10.09.2026 (Ä25): KEIN eigener Titel. Die Maske
                // erscheint ausschließlich als Bereich einer Überlagerung, und deren
                // Kopf trägt „Übernahme ins Projekt" (KUEB_TITEL) bereits — bis hierher
                // stand die Überschrift zweimal übereinander.
                ["TitelText"] = "",
                ["LabelZielProjekt"] = Text_("KUEB_LBL_ZIEL", "Zielprojekt:"),
                ["LabelQuelleVorlage"] = Text_("KDLG_UEB_QUELLE_VORLAGE", "Aus Vorlage/Variante:"),
                ["LabelQuelleProjekt"] = Text_("KDLG_UEB_QUELLE_PROJEKT", "Aus Projekt/Anlage:"),
                ["LabelQuellProjekt"] = Text_("KUEB_LBL_QUELLPROJEKT", "Quellprojekt:"),
                ["LabelQuellAnlage"] = Text_("KUEB_LBL_QUELLANLAGE", "Quellanlage:"),

                // ANWENDERENTSCHEID 19.09.2026: derselbe Block wie im Katalogmodus —
                // deshalb dieselben Schlüssel, nicht neue mit gleichem Wortlaut.
                ["LabelKomponente"] = Text_("KDLG_LBL_KOMPONENTE", "Komponente:"),
                ["KomponenteText"] = name,
                ["KategorieInvestText"] = Text_("KDLG_KAT_INVEST", "Investitionskosten"),
                ["KategorieBetriebText"] = Text_("KDLG_KAT_BETRIEB", "Betriebskosten"),
                ["LabelVariante"] = Text_("KDLG_LBL_VARIANTE", "Variante:"),
                ["LabelPositionen"] = Text_("KKOMP_RASTER", "Positionen"),
                ["SpaltePosition"] = Text_("KDLG_SP_POSITION", "Position"),
                ["SpalteBemessung"] = Text_("KDLG_SP_BEMESSUNG", "Bemessung"),
                ["SpalteSatz"] = Text_("KDLG_SP_SATZ", "Satz"),
                ["SpalteNutzung"] = Text_("KDLG_SP_NUTZUNG", "Nutzungsdauer [a]"),
                ["SpalteZiel"] = Text_("KUEB_SP_ZIEL", "Ziel"),
                ["VorlageLeerText"] = Text_("KUEB_VORLAGE_LEER",
                    "Diese Vorlage führt keine Positionen."),
                ["KatalogLeerText"] = Text_("KUEB_KATALOG_LEER",
                    "Der Katalog führt für diese Komponente keine Vorlage — "
                    + "Administration › Kosten › Kostenverwaltung."),

                // Ä25: OK/Abbrechen statt „Übernehmen"/„Abbrechen" — die beiden
                // allgemeinen Beschriftungen, keine eigenen Schlüssel.
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        /// <summary>Die Varianten einer Kategorie als Eintragsliste (Standard zuerst).</summary>
        private static IReadOnlyList<ValueTuple<int, string>> Variantenliste(
            IList<KostenVorlageKopf> vorlagen)
        {
            var liste = new List<ValueTuple<int, string>>();
            foreach (KostenVorlageKopf v in vorlagen)
                liste.Add(new ValueTuple<int, string>(v.Id, v.Name));
            return liste;
        }

        /// <summary>Der Kopf zur gewählten Variantennummer — aus beiden Kategorien.</summary>
        private static KostenVorlageKopf Kopf(int vorlageId,
                                              IList<KostenVorlageKopf> invest,
                                              IList<KostenVorlageKopf> betrieb)
        {
            foreach (KostenVorlageKopf v in invest)
                if (v.Id == vorlageId) return v;
            foreach (KostenVorlageKopf v in betrieb)
                if (v.Id == vorlageId) return v;
            return null;
        }

        /// <summary>
        /// ANWENDERENTSCHEID 19.09.2026 — DIE POSITIONSVORSCHAU. Sie zeigt, was in der
        /// gewählten Variante steht, mit denselben Anzeigetexten wie das Positionsraster
        /// der Kostenverwaltung: Bemessungsart und Einheitenzeichen kommen aus
        /// <see cref="BemessungKatalog"/> und hängen am GEWERK und am Raster.
        ///
        /// <para><b>ANWENDERBEFUND 19.09.2026 — was fehlen würde.</b> Je Zeile steht
        /// „vorhanden" oder „wird angelegt". Gefragt wird mit derselben Regel, nach der
        /// <c>KostenVorlagenUebernahmeCtrl.AusVorlage</c> überspringt oder anlegt: Ein
        /// Zielbestand derselben Position zählt, mit Anlagenbezug nur AN DERSELBEN
        /// Anlage (Ä20). Gelesen wird über <c>KostenProjektPositionenCtrl.Lies</c> —
        /// derselbe Weg, den die Kostenverwaltung selbst geht; kein neues SQL, kein
        /// Schreibweg (die Übernahme legt Lexikoneinträge an, die Vorschau nicht).</para>
        /// </summary>
        private static IReadOnlyList<VorlagenPositionZeile> Positionsvorschau(
            VorlagenUebernahmeWahl wahl,
            IList<KostenVorlageKopf> vorlagenInvest, IList<KostenVorlageKopf> vorlagenBetrieb,
            int komponentenId, int zielAnlageId)
        {
            var liste = new List<VorlagenPositionZeile>();
            if (wahl.QuellVorlageId <= 0) return liste;

            int kategorieId = wahl.Invest
                ? KostenSummenCtrl.KATEGORIE_INVESTITION
                : KostenSummenCtrl.KATEGORIE_BETRIEB;

            // Der Zielbestand als Namensmenge — dieselbe Zählgrundlage wie die
            // Vorschauzeile (zielAnlageId 0 heißt „alle Anlagen", also -1).
            var imZiel = new HashSet<string>(StringComparer.Ordinal);
            bool zielBekannt = wahl.ZielProjektId > 0;
            if (zielBekannt)
            {
                foreach (KostenProjektPositionenCtrl.Zeile z in
                         KostenProjektPositionenCtrl.Lies(wahl.ZielProjektId, komponentenId,
                             kategorieId, zielAnlageId > 0 ? zielAnlageId : -1))
                {
                    string b = z.Raster != null ? z.Raster.Bezeichnung : null;
                    if (!string.IsNullOrEmpty(b)) imZiel.Add(b);
                }
            }

            string textVorhanden = Text_("KUEB_POS_VORHANDEN", "vorhanden");
            string textNeu = Text_("KUEB_POS_NEU", "wird angelegt");

            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(wahl.QuellVorlageId))
            {
                string bezeichnung = p.Bezeichnung ?? "";
                bool vorhanden = zielBekannt && imZiel.Contains(bezeichnung);
                string einheit = BemessungKatalog.Einheit(p.Bemessung, komponentenId, !wahl.Invest);
                string satz = ZahlText(p.Satz);
                if (satz.Length > 0 && einheit.Length > 0) satz = satz + " " + einheit;

                liste.Add(new VorlagenPositionZeile(
                    bezeichnung,
                    BemessungKatalog.Anzeige(p.Bemessung, komponentenId),
                    satz,
                    wahl.Invest ? ZahlText(p.Nutzungsdauer) : "",
                    !zielBekannt ? "" : (vorhanden ? textVorhanden : textNeu),
                    vorhanden));
            }
            return liste;
        }

        private static string ZahlText(double? wert)
        {
            return wert.HasValue
                ? wert.Value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)
                : "";
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
        ///
        /// <para><b>19.09.2026:</b> Die Zählung am Ziel folgt der im Katalogblock
        /// GEWÄHLTEN Kategorie; „Aus Projekt/Anlage" bleibt bei der Kategorie, aus der
        /// der Dialog geöffnet wurde.</para>
        /// </summary>
        private static VorlagenUebernahmeVorschau Vorschau(VorlagenUebernahmeWahl wahl,
                                                           int komponentenId, int kategorieId,
                                                           int zielAnlageId)
        {
            if (wahl.ZielProjektId <= 0) return new VorlagenUebernahmeVorschau("", false);

            int kategorie = Kategorie(wahl, kategorieId);

            int vorhanden = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                wahl.ZielProjektId, komponentenId, kategorie, zielAnlageId > 0 ? zielAnlageId : -1);

            int quelle = wahl.AusVorlage
                ? (wahl.QuellVorlageId > 0 ? KostenVorlagenCtrl.Positionen(wahl.QuellVorlageId).Count : 0)
                : KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                      wahl.QuellProjektId, komponentenId, kategorie, wahl.QuellAnlageId);

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

        /// <summary>
        /// Die Kategorie, in der gerechnet und geschrieben wird: im Katalogblock die
        /// GEWÄHLTE, sonst die, aus der der Dialog geöffnet wurde.
        /// </summary>
        private static int Kategorie(VorlagenUebernahmeWahl wahl, int kategorieId)
        {
            if (!wahl.AusVorlage) return kategorieId;
            return wahl.Invest
                ? KostenSummenCtrl.KATEGORIE_INVESTITION
                : KostenSummenCtrl.KATEGORIE_BETRIEB;
        }

        /// <summary>Der Schreibweg — wortgleich aus <c>btnUebernehmen_Click</c>.
        /// Der Vorlagenkopf trägt seine eigene <c>KategorieId</c>; <c>AusVorlage</c>
        /// schreibt damit in die im Block gewählte Kategorie.</summary>
        private static VorlagenUebernahmeAntwort Uebernehmen(VorlagenUebernahmeWahl wahl,
                                                             IList<KostenVorlageKopf> vorlagenInvest,
                                                             IList<KostenVorlageKopf> vorlagenBetrieb,
                                                             int komponentenId, int kategorieId,
                                                             int zielAnlageId)
        {
            UebernahmeErgebnis ergebnis;
            if (wahl.AusVorlage)
            {
                KostenVorlageKopf quelle = Kopf(wahl.QuellVorlageId, vorlagenInvest, vorlagenBetrieb);
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
