using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Abbildung Zeile ↔ Modell des Dialogs „Wärmebedarf extern“ / „Kältebedarf extern“ — plattformfrei, damit
    /// der Rückweg der Windows-Hülle (<c>WaermebedarfExternHuelle</c>) und des Assistenten ohne Windows prüfbar ist.
    /// Der Dialog führt Kennung, Ganglinie, Bezeichner, Kanal, Kälte-Art und die acht Deckungsfelder (K5a,
    /// <see cref="DeckungsartDaten"/>).
    ///
    /// <para><b>Modus „nur Kühlung“</b> (Konzept Kältebedarf 3.2): Die Hülle reicht dem Dialog nur die Zeilen des Kanals
    /// Kühlung (<see cref="Modelle"/>), und <see cref="Speichern"/> löscht und schreibt nur diese Kanalzeilen
    /// (<see cref="WizardCtrl.Del_WaermebedarfExtern(int, string, DbVorgang)"/>) — jede Wärmezeile bleibt mit Kennung,
    /// Ganglinie, Bezeichner und Kanal unverändert.</para>
    /// </summary>
    internal static class WaermebedarfExternAbbildung
    {
        /// <summary>
        /// Die Modelle, die der Dialog im <paramref name="modus"/> bearbeitet — im Modus „nur Kühlung“ allein die Zeilen
        /// des Kanals Kühlung (leer und unbekannt zählen wie beim Lesen als Heizung), sonst alle.
        /// </summary>
        internal static List<Z_ProjWaermebedarfModel> Modelle(IEnumerable<Z_ProjWaermebedarfModel> modelle,
                                                              WaermebedarfExternModus modus)
        {
            var liste = new List<Z_ProjWaermebedarfModel>();
            if (modelle == null) return liste;
            foreach (Z_ProjWaermebedarfModel m in modelle)
                if (m != null && (modus == WaermebedarfExternModus.Alle || Z_ProjektKaeltebedarfCtrl.IstKuehlkanal(m)))
                    liste.Add(m);
            return liste;
        }

        /// <summary>
        /// Der Speicherweg der Hülle (Startseite): prüft zuerst jede Kühlzeile mit der Regel des Kerns
        /// (<see cref="Z_ProjektKaeltebedarfCtrl.Deckungspruefung(Z_ProjWaermebedarfModel)"/>) und lehnt eine
        /// dezentrale Zeile ohne rechnenden EER BENANNT ab, ohne etwas zu schreiben; sonst Löschen + Neuanlegen in EINEM
        /// Vorgang — im Modus „nur Kühlung“ nur die Kanalzeilen Kühlung, sonst alle Zeilen des Projekts.
        /// </summary>
        /// <returns><c>null</c> = gespeichert; sonst der Grund (Ablehnung oder Fehlschlag, nichts geändert).</returns>
        internal static string Speichern(int projektId, List<Z_ProjWaermebedarfModel> modelle, WaermebedarfExternModus modus)
        {
            List<Z_ProjWaermebedarfModel> liste = Modelle(modelle, modus);
            string grund = Deckungsgrund(liste);
            if (grund != null) return grund;

            var wiz = new WizardCtrl();
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                bool geloescht = modus == WaermebedarfExternModus.NurKuehlung
                    ? wiz.Del_WaermebedarfExtern(projektId, DbWerte.KANAL_KUEHLUNG, vorgang)
                    : wiz.Del_WaermebedarfExtern(projektId, vorgang);
                if (geloescht && wiz.Add_WaermebedarfExtern(projektId, liste, vorgang))
                {
                    vorgang.Commit();
                    return null;
                }
                vorgang.Rollback();
            }
            return MyResource.Resource.DECK_MSG_SPEICHERN_FEHLER;
        }

        /// <summary>
        /// Die Prüfung „Split ohne EER“ über eine Modellliste — derselbe Wortlaut wie der Banner des Dialogs, mit dem
        /// Namen der ersten verworfenen Zeile; <c>null</c> = alle Kühlzeilen zulässig. Der Speicherweg der Hülle und der
        /// Assistent (Seite Wärmebedarf, <c>AssistentAnsichtQuelle</c>) prüfen damit, bevor der Kern schreibt.
        /// </summary>
        internal static string Deckungsgrund(IEnumerable<Z_ProjWaermebedarfModel> modelle)
        {
            if (modelle == null) return null;
            foreach (Z_ProjWaermebedarfModel m in modelle)
                if (m != null && Z_ProjektKaeltebedarfCtrl.Deckungspruefung(m) != null)
                    return MyResource.Resource.DECK_MSG_OHNE_EER + " (" + (m.m_szBezeichner ?? "") + ")";
            return null;
        }

        /// <summary>Die Dialogzeilen aus der Modellliste des Aufrufers.</summary>
        internal static List<WaermebedarfExternZeile> Zeilen(IEnumerable<Z_ProjWaermebedarfModel> modelle)
        {
            var zeilen = new List<WaermebedarfExternZeile>();
            if (modelle == null) return zeilen;
            foreach (Z_ProjWaermebedarfModel m in modelle) zeilen.Add(AusModell(m));
            return zeilen;
        }

        /// <summary>
        /// Der Änderungsrückruf des Dialogs: schreibt die Dialogzeilen in die Modellliste des Aufrufers zurück, die
        /// danach gespeichert wird (Löschen + Neuanlegen).
        ///
        /// <para><b>Die geladenen Modelle werden fortgeschrieben, nicht ersetzt.</b> Was der Dialog nicht kennt, kommt
        /// aus dem beim Öffnen geladenen Modell derselben Zuordnung (über <c>ID_Z</c>); die Deckungsfelder führt seit K5a
        /// die Zeile selbst (<see cref="WaermebedarfExternZeile.Deckung"/>). Neue Zeilen (<c>ID_Z = 0</c>) stehen auf der
        /// Vorgabe; einen Kanal außer Kühlung normalisiert der Speicherweg auf „zentral“.</para>
        /// </summary>
        internal static Action Rueckweg(List<WaermebedarfExternZeile> zeilen, List<Z_ProjWaermebedarfModel> modelle,
                                        int projektId)
        {
            // Stand beim Öffnen festhalten: Der Rückruf leert die Liste des Aufrufers und füllt sie neu.
            var geladen = new Dictionary<int, Z_ProjWaermebedarfModel>();
            foreach (Z_ProjWaermebedarfModel m in modelle)
                if (m != null && m.m_ID_Z > 0) geladen[m.m_ID_Z] = m;

            return () =>
            {
                modelle.Clear();
                foreach (WaermebedarfExternZeile z in zeilen)
                {
                    geladen.TryGetValue(z.IdZ, out Z_ProjWaermebedarfModel vorher);
                    modelle.Add(NachModell(z, projektId, z.IdZ > 0 ? vorher : null));
                }
            };
        }

        internal static WaermebedarfExternZeile AusModell(Z_ProjWaermebedarfModel m)
        {
            return new WaermebedarfExternZeile
            {
                IdZ = m.m_ID_Z,
                IdGanglinie = m.m_ID_Ganglinie,
                Bezeichner = m.m_szBezeichner ?? "",
                Kanal = Z_ProjektGebGanglinieCtrl.KanalOderHeizung(m.Kanal),
                KaelteArt = KaelteArtSchema.Normal(m.KaelteArt),
                Deckung = new DeckungsartDaten
                {
                    Deckungsart = string.IsNullOrEmpty(m.Deckung) ? KaeltebedarfSchema.DECKUNG_ZENTRAL : m.Deckung,
                    EerWeg = m.EerWeg, Eer1 = m.Eer1, Taussen1 = m.Taussen1, Eer2 = m.Eer2, Taussen2 = m.Taussen2,
                    KuehlIdCarrier = m.KuehlIdCarrier, KuehlEigenerZaehler = m.KuehlEigenerZaehler
                }
            };
        }

        /// <summary>
        /// Das Modell einer Dialogzeile. <paramref name="vorher"/> ist das beim Öffnen geladene Modell derselben
        /// Zuordnung: Was die Zeile nicht führt, wird übernommen; Kennung, Ganglinie, Bezeichner, Kanal, Kälte-Art und die
        /// Deckungsfelder kommen aus der Zeile.
        /// </summary>
        internal static Z_ProjWaermebedarfModel NachModell(WaermebedarfExternZeile z, int projektId,
                                                           Z_ProjWaermebedarfModel vorher = null)
        {
            var neu = new Z_ProjWaermebedarfModel();
            if (vorher != null) Z_ProjektKaeltebedarfCtrl.DeckungUebernehmen(vorher, neu);
            neu.m_ID_Z = z.IdZ;
            neu.m_ID_Projekt = projektId;
            neu.m_ID_Ganglinie = z.IdGanglinie;
            neu.m_szBezeichner = z.Bezeichner;
            neu.Kanal = Z_ProjektGebGanglinieCtrl.KanalOderHeizung(z.Kanal);
            neu.KaelteArt = KaelteArtSchema.Normal(z.KaelteArt);
            DeckungsartDaten d = z.Deckung ?? new DeckungsartDaten();
            neu.Deckung = string.IsNullOrEmpty(d.Deckungsart) ? KaeltebedarfSchema.DECKUNG_ZENTRAL : d.Deckungsart;
            neu.EerWeg = d.EerWeg; neu.Eer1 = d.Eer1; neu.Taussen1 = d.Taussen1; neu.Eer2 = d.Eer2; neu.Taussen2 = d.Taussen2;
            neu.KuehlIdCarrier = d.KuehlIdCarrier; neu.KuehlEigenerZaehler = d.KuehlEigenerZaehler;
            return neu;
        }
    }
}
