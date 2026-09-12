using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Projekt;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE Datenseite des Dialogs „Als Variante speichern"
    /// (Auftrag <b>#237</b>, Anwenderwunsch 12.09.2026).
    ///
    /// <para><b>Warum sie hier liegt und nicht in <c>Views/Varianten/</c>.</b> Regel
    /// des Auftrags #208: Eine Hülle, die keine einzige Windows-Zeile führt, gehört
    /// nach <c>EPOS.UI.Daten</c> — sonst ist ihre Seite auf iOS unerreichbar. Diese
    /// Hülle bestimmt den Stamm, holt die Projektzeilen, reicht die Namensregel des
    /// Kerns durch und legt an; nichts davon braucht ein Fenster.</para>
    ///
    /// <para><b>Was WINDOWS beisteuert, kommt als Rückruf herein</b> — das Nachziehen
    /// der Startseite (<c>StartseiteHuelle.Aktuelle?.VariantenAnzeigeAktualisieren()</c>)
    /// ist die Anzeige eines WinForms-Wirts und hat hier nichts verloren. Der Adapter
    /// <c>Views/Varianten/AlsVarianteHuelle</c> gibt ihn mit; auf iOS bleibt er leer.</para>
    ///
    /// <para><b>Gerechnet wird im Kern</b> (<see cref="VariantenCtrl"/>); hier steht
    /// keine eigene Anlegelogik und kein SQL.</para>
    /// </summary>
    internal static class ProjektVarianteHuelle
    {
        /// <summary>
        /// Was vor dem Öffnen feststehen muss: Zu welchem STAMM gehört die neue
        /// Variante? Ist das geöffnete Projekt selbst eine Variante, ist es ihr Stamm —
        /// eine Variante hängt immer am Stamm, nie an einer anderen Variante, sonst
        /// wäre die Vergleichsgruppe der Wirtschaftlichkeit eine Kette.
        /// </summary>
        /// <param name="IdStamm">Id des Stammprojekts; 0, wenn es keines gibt.</param>
        /// <param name="StammName">Name des Stammprojekts; leer, wenn es keines gibt.</param>
        /// <param name="Fehlerschluessel">
        /// Leer = bereit. Sonst der Ressourcenschlüssel der Meldung, die der Aufrufer
        /// zeigt: <c>VAR_MSG_KEIN_PROJEKT</c> (kein Projekt offen) oder
        /// <c>BK_MSG_KEIN_STAMM</c> (Stamm nicht auffindbar).
        /// </param>
        internal readonly record struct Vorbereitung(int IdStamm, string StammName, string Fehlerschluessel)
        {
            /// <summary>Kann der Dialog geöffnet werden?</summary>
            public bool Bereit => Fehlerschluessel.Length == 0 && IdStamm > 0 && StammName.Length > 0;
        }

        /// <summary>Ergebnis des Anlegens: neue Projekt-Id oder Fehlertext.</summary>
        /// <param name="NeueId">Die Id der angelegten Variante; <c>-1</c> bei Fehler.</param>
        /// <param name="Fehlertext">Der Grund bei <c>-1</c>; sonst leer.</param>
        internal readonly record struct Anlegeergebnis(int NeueId, string Fehlertext)
        {
            public bool Gelungen => NeueId > 0;
        }

        /// <summary>Bestimmt den Stamm zum geöffneten Projekt (siehe <see cref="Vorbereitung"/>).</summary>
        /// <param name="idProjekt">Das geöffnete Projekt (Stamm ODER Variante).</param>
        /// <param name="projektname">Sein Name, soweit der Aufrufer ihn kennt.</param>
        internal static Vorbereitung Vorbereiten(int idProjekt, string projektname)
        {
            if (idProjekt <= 0)
                return new Vorbereitung(0, "", "VAR_MSG_KEIN_PROJEKT");

            int idStamm = new VariantenCtrl().StammRefDerVariante(idProjekt);
            bool istVariante = idStamm > 0;
            if (!istVariante) idStamm = idProjekt;

            string stammName = istVariante ? StartseiteCtrl.Projektname(idStamm) : (projektname ?? "");
            if (string.IsNullOrWhiteSpace(stammName)) stammName = StartseiteCtrl.Projektname(idStamm);
            if (string.IsNullOrWhiteSpace(stammName))
                return new Vorbereitung(0, "", "BK_MSG_KEIN_STAMM");

            return new Vorbereitung(idStamm, stammName, "");
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, das setzt der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idStamm, string stammName)
        {
            var ctrl = new VariantenCtrl();

            return new Dictionary<string, object>
            {
                ["StammName"] = stammName ?? "",
                ["Zeilen"] = ProjektCtrl.NamenListe(),

                // DIE Namensregel des Kerns - der Dialog baut sie nicht nach.
                ["Zielname"] = new Func<string, string>(b => ctrl.Zielname(stammName ?? "", b)),

                ["TitelText"] = MyResource.Resource.VAR_DLG_TITEL,
                ["HinweisText"] = string.Format(MyResource.Resource.VAR_DLG_HINWEIS, stammName ?? ""),
                ["HinweisQuelleFormat"] = MyResource.Resource.VAR_DLG_HINWEIS_QUELLE,
                ["QuelleHakenText"] = MyResource.Resource.VAR_DLG_QUELLE_HAKEN,
                ["ZielnameFormat"] = MyResource.Resource.VAR_DLG_ZIELNAME,
                ["MeldungQuelleWaehlen"] = MyResource.Resource.VAR_MSG_QUELLE_WAEHLEN,

                ["FrageText"] = MyResource.Resource.BK_LBL_BEZEICHNER,
                ["OkText"] = MyResource.Resource.BK_BTN_ANLEGEN,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,

                ["AnzahlFormat"] = MyResource.Resource.PRJ_LIST_ANZAHL,
                ["SpalteName"] = MyResource.Resource.PRJ_LIST_SP_NAME,
                ["SpalteKunde"] = MyResource.Resource.PRJ_LIST_SP_KUNDE,
                ["SpalteGeaendert"] = MyResource.Resource.PRJ_LIST_SP_GEAENDERT,
                ["SpalteArt"] = MyResource.Resource.PRJ_LIST_SP_ART,
                ["ArtStammText"] = MyResource.Resource.PRJ_LIST_ART_STAMM,
                ["ArtVarianteText"] = MyResource.Resource.PRJ_LIST_ART_VARIANTE,
                ["VarianteVonFormat"] = MyResource.Resource.PRJ_LIST_VARIANTE_VON,
                ["SucheText"] = MyResource.Resource.PRJ_LIST_LBL_SUCHE,
                ["LeerText"] = MyResource.Resource.PRJ_LIST_LEER
            };
        }

        /// <summary>
        /// Legt die Variante an und zieht danach die Startseite nach.
        /// </summary>
        /// <param name="wahl">Die Antwort des Dialogs (Bezeichner, Quellprojekt).</param>
        /// <param name="nachziehen">
        /// Die Anzeige des Wirts auffrischen; <c>null</c> erlaubt (iOS kennt keine
        /// Windows-Startseite).
        /// </param>
        internal static Anlegeergebnis Anlegen(int idStamm, string stammName,
                                               ProjektVarianteWahl wahl, Action nachziehen = null)
        {
            try
            {
                string fehler;
                int neueId = new VariantenCtrl().AnlegenAusStamm(
                    idStamm, stammName, wahl.Bezeichner,
                    wahl.IdQuelle > 0 ? wahl.IdQuelle : idStamm, out fehler);

                if (neueId <= 0)
                    return new Anlegeergebnis(-1,
                        string.IsNullOrEmpty(fehler)
                            ? MyResource.Resource.BK_MSG_ANLEGEN_FEHLGESCHLAGEN : fehler);

                // Startseite nachziehen: Variantenauswahl und - falls schon aufgebaut -
                // der Reiter "Berichte & Kosten" kennen die neue Variante sonst nicht.
                nachziehen?.Invoke();
                return new Anlegeergebnis(neueId, "");
            }
            catch (Exception ex)
            {
                return new Anlegeergebnis(-1,
                    string.Format(MyResource.Resource.BK_MSG_ANLEGEFEHLER, ex.Message));
            }
        }
    }
}
