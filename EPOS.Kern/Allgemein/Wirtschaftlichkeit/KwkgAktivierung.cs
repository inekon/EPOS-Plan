using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ist der KWK-Zuschlag dieser Gruppe aktiv?</b> — die EINE Antwort für den
    /// Rechenkern, den Word- und den Excel-Bericht, die Nachweiszeile und die beiden
    /// Windows-Hüllen (Etappe BK1, Entscheid BK-E-1 a).
    ///
    /// <para><b>Der Befund.</b> Bis BK1 stand derselbe Ausdruck
    /// <c>p.KwkgBonus &gt; 0 || p.KwkgBonusEinspeisung &gt; 0</c> an sechs Stellen —
    /// im Rechenkern, in <c>BausteineWirtschaftlichkeit</c>, im
    /// <c>ExcelBerichtGenerator</c>, in <c>WirtschaftlichkeitDaten.Nachweis</c>, in der
    /// <c>KapitalwertVerlaufHuelle</c> und in <c>WirtschaftlichkeitSeiteGaben</c>. Sechs
    /// Kopien einer Regel sind sechs Orte, an denen sie auseinanderlaufen kann — und
    /// genau das geschähe mit dem Umzug der Sätze an die Anlage: Der Ausdruck fragte
    /// weiter die PROJEKTvorgabe, während gerechnet wird, was an der Anlage steht.</para>
    ///
    /// <para><b>Die Regel selbst.</b> Aktiv ist der Zuschlag, sobald <b>irgendeine</b>
    /// BHKW-Anlage der Gruppe einen Satz größer 0 führt — auf selbst genutzten Strom
    /// oder auf eingespeisten. Eine einzige Anlage genügt; die übrigen dürfen leer sein.
    /// Der Projektsatz entscheidet nicht mehr mit: Seit Schemaschritt 89 trägt jede
    /// BHKW-Anlage den Wert, der früher über den Rückfall aus dem Projekt kam
    /// (<see cref="SchemaKatalog.Schritt89_KwkAnlagenwahrheit"/>).</para>
    ///
    /// <para><b>Reine Leseseite, tolerant.</b> Scheitert die Abfrage — fehlende Spalten
    /// in einer nicht migrierten Datenbank, fehlende Tabelle —, ist die Antwort
    /// <c>false</c>: dasselbe Verhalten wie ein Projekt ohne gepflegten Satz, und
    /// dieselbe Regel, die <c>KostenEmissionRechner.StromLeistungspreisGepflegt</c>
    /// fährt.</para>
    /// </summary>
    public static class KwkgAktivierung
    {
        /// <summary>
        /// <b>DIE Regel</b>, auf zwei Zahlen angewandt: Führt diese Anlage einen
        /// KWK-Zuschlagssatz? <c>null</c> und 0 heißen beide „nein" — die Nullsemantik
        /// des ganzen Dialogs.
        /// </summary>
        /// <param name="satzEigenCt">Satz auf selbst genutzten Strom [ct/kWh].</param>
        /// <param name="satzEinspCt">Satz auf eingespeisten Strom [ct/kWh].</param>
        public static bool SatzGefuehrt(double? satzEigenCt, double? satzEinspCt)
        {
            return (satzEigenCt.HasValue && satzEigenCt.Value > 0)
                || (satzEinspCt.HasValue && satzEinspCt.Value > 0);
        }

        /// <summary>
        /// Führt <b>eine</b> BHKW-Anlage dieses Projekts einen Satz? Liest
        /// <c>Tab_Energieanlagen</c> unmittelbar — die Frage steht vor jedem Rechenlauf
        /// und braucht kein Ergebnis.
        /// </summary>
        public static bool IstAktiv(int idProjekt)
        {
            if (idProjekt <= 0) return false;
            try
            {
                using (DataRepository.EngineModus())
                {
                    object o = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                        " WHERE ID_Projekt = ? AND ID_Type = " + WizardItemClass.BHKW_TYP +
                        " AND ([" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + "] > 0" +
                        " OR [" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + "] > 0)",
                        new DbParam("@p", idProjekt));
                    return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// Dieselbe Frage für eine ganze VERGLEICHSGRUPPE — Muster und Begründung
        /// wortgleich zu <c>KostenEmissionRechner.StromLeistungspreisGepflegt(int,
        /// IEnumerable&lt;int&gt;)</c>: Eine Variante kann den Zuschlag führen, ohne dass
        /// der Stamm es tut; entschiede allein der Stamm, sammelte die Wirtschaftlichkeit
        /// die Stundenreihen nicht ein, und der Variante fiele ihr Split still weg.
        /// </summary>
        /// <param name="idStamm">Das Stammprojekt der Gruppe.</param>
        /// <param name="versionen">Die übrigen Versionen (der Stamm darf darin stehen);
        /// <c>null</c> = nur der Stamm.</param>
        public static bool IstAktiv(int idStamm, IEnumerable<int> versionen)
        {
            if (IstAktiv(idStamm)) return true;
            if (versionen == null) return false;
            foreach (int id in versionen)
                if (id > 0 && id != idStamm && IstAktiv(id)) return true;
            return false;
        }
    }
}
