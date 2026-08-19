using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das gefuellte Aktionsregister (Fachkonzept 3.2, Katalog 5.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum hier und nicht im Kern.</b> Nur an dieser Stelle darf UI- und DB-Code
    /// stehen (Fachkonzept 3.7). Weil die Registerbefuellung in DERSELBEN Assembly liegt
    /// wie die Controller, ist weder <c>InternalsVisibleTo</c> noch eine oeffentliche
    /// Fassade noetig - <c>ProjektCtrl</c>, <c>TechnikPlanwertCtrl</c> und
    /// <c>KostenPositionCtrl</c> sind <c>internal</c> (Fachkonzept 5.5).
    /// </para>
    /// <para>
    /// <b>Umfang dieses Pakets.</b> Nur Stufe 1 (lesend) aus Fachkonzept 5.1 - OHNE
    /// <c>maske_oeffnen</c> und <c>projekt_oeffnen</c>: beide oeffnen blockierend-modal
    /// (<c>MenueCtrl.cs:251-389</c>, <c>:130</c>, <c>:178</c>) und gehoeren in eine spaetere
    /// Etappe.
    /// </para>
    /// <para>
    /// <b>Regeln, die hier eingehalten werden.</b> Nur benannte Aktionen (kein generisches
    /// SQL, keine Reflexion); Parameter primitiv oder IDs aus einer Leseaktion;
    /// Aufzaehlungswerte, die auf Datenbankwerte abbilden, stammen aus
    /// <see cref="DbWerte"/> bzw. aus der Landkarte des jeweiligen Controllers - nie aus
    /// Modelltext; Zahlen invariant, Anzeige in <see cref="CultureInfo.CurrentCulture"/>.
    /// </para>
    /// </remarks>
    internal static class KiAktionen
    {
        /// <summary>Baut das vollstaendige Register.</summary>
        internal static KiRegister Erzeuge()
        {
            var register = new KiRegister();

            // ---- Projekte, Varianten, Speichervarianten
            register.Aufnehmen(KiAktionenProjekt.ProjekteAuflisten());
            register.Aufnehmen(KiAktionenProjekt.ProjektLesen());
            register.Aufnehmen(KiAktionenProjekt.VariantenAuflisten());
            register.Aufnehmen(KiAktionenProjekt.SpeichervariantenAuflisten());

            // ---- Wirtschaftlichkeit und Kosten
            register.Aufnehmen(KiAktionenWirtschaft.ErgebnisseLesen());
            register.Aufnehmen(KiAktionenWirtschaft.ParameterLesen());
            register.Aufnehmen(KiAktionenWirtschaft.KostenlagePruefen());

            // ---- Trockenlaeufe der Uebernahme
            register.Aufnehmen(KiAktionenUebernahme.UebernahmeVorschau());
            register.Aufnehmen(KiAktionenUebernahme.MerkmalVorschau());

            // ---- Lastgang und Peak-Shaving
            register.Aufnehmen(KiAktionenLastgang.LastgangPruefen());
            register.Aufnehmen(KiAktionenLastgang.GanglinienAuflisten());
            register.Aufnehmen(KiAktionenLastgang.MinimaleSpitzeErmitteln());

            // ---- Sitzungsgedaechtnis
            register.Aufnehmen(KiAktionenSitzung.LetzteAktionen());

            return register;
        }
    }

    /// <summary>
    /// Gemeinsame Bausteine der Registerbefuellung: Zeilenbau, Namensaufloesung,
    /// wiederkehrende Vorbedingungen.
    /// </summary>
    internal static class KiHilfe
    {
        /// <summary>Standardparameter „Projekt (ID)".</summary>
        internal static KiParameter ProjektId(string erlaeuterung = null, bool pflicht = true)
        {
            return new KiParameter(
                "projekt_id", KiParameterTyp.Ganzzahl,
                erlaeuterung ?? KiAktionsTexte.ProjektIdErlaeuterung,
                pflicht: pflicht,
                anzeigename: KiAktionsTexte.ProjektIdName,
                min: 1);
        }

        /// <summary>Baut eine Ergebniszeile aus Name/Wert-Paaren.</summary>
        internal static IReadOnlyDictionary<string, object> Zeile(params object[] paare)
        {
            var zeile = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < paare.Length; i += 2)
                zeile[(string)paare[i]] = paare[i + 1];
            return zeile;
        }

        /// <summary>Leere, typrichtige Zeilenliste.</summary>
        internal static List<IReadOnlyDictionary<string, object>> Liste()
        {
            return new List<IReadOnlyDictionary<string, object>>();
        }

        /// <summary>Gibt es ein Projekt mit dieser ID?</summary>
        internal static bool ProjektExistiert(int idProjekt)
        {
            if (idProjekt <= 0) return false;
            try
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?",
                    new OleDbParameter("@id", (Int32)idProjekt));
                return n != null && n != DBNull.Value && Convert.ToInt32(n) > 0;
            }
            catch { return false; }
        }

        /// <summary>Projektname zu einer ID; leer, wenn es die ID nicht gibt.</summary>
        internal static string ProjektName(int idProjekt)
        {
            if (idProjekt <= 0) return "";
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                    new OleDbParameter("@id", (Int32)idProjekt));
                return o == null || o == DBNull.Value ? "" : o.ToString();
            }
            catch { return ""; }
        }

        /// <summary>
        /// Vorbedingung „das Projekt gibt es" - der haeufigste Fall. Liefert den
        /// Klartextgrund oder <c>null</c>.
        /// </summary>
        internal static string ProjektMussExistieren(int idProjekt)
        {
            return ProjektExistiert(idProjekt)
                ? null
                : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.ProjektUnbekannt, idProjekt);
        }

        /// <summary>Zahl fuer die Ergebniszeile; <c>null</c> bleibt <c>null</c>.</summary>
        internal static object Wert(double? zahl)
        {
            return zahl.HasValue ? (object)Math.Round(zahl.Value, 4) : null;
        }

        /// <summary>Zahl fuer die Ergebniszeile.</summary>
        internal static object Wert(double zahl)
        {
            return Math.Round(zahl, 4);
        }

        /// <summary>Text fuer die Ergebniszeile; <c>null</c> wird zu leer.</summary>
        internal static object Text(string text)
        {
            return text ?? "";
        }

        /// <summary>„n von m" - die Kurzfassung, die in jeder Ergebnismeldung auftaucht.</summary>
        internal static string Anzahltext(int anzahl, string einzahl, string mehrzahl)
        {
            return anzahl + " " + (anzahl == 1 ? einzahl : mehrzahl);
        }

        /// <summary>Datum fuer die Ergebniszeile - invariant, damit es maschinenlesbar bleibt.</summary>
        internal static object Datum(DateTime wert)
        {
            return wert == default(DateTime) ? "" : wert.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
