using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// KONZEPT § 2.9 — die aufgelöste <b>Referenz</b> einer Vergleichsgruppe: gegen
    /// welchen Stand die Differenzkennzahlen rechnen, wie er heißt und ob dabei ein
    /// Rückfall auf den Stamm nötig war.
    ///
    /// <para><b>Warum das eine eigene Klasse ist.</b> Drei Stellen brauchen dieselbe
    /// Antwort: die Rechnung (<c>WirtschaftlichkeitCtrl.Berechne</c> und
    /// <c>BerechneVerlauf</c>), die Zeilendefinition
    /// (<see cref="WirtschaftlichkeitZeilen"/>, die die Referenzspalte kennzeichnet)
    /// und die Anzeige samt Bericht (Nachweiszeile, Deklarationszeile). Stünde die
    /// Auflösung dreimal da, dürften die drei auseinanderlaufen — und die Software
    /// zeigte eine andere Referenz an, als sie rechnet.</para>
    ///
    /// <para><b>Plattformfrei und ohne Datenbank:</b> Die Klasse bekommt die Gruppe
    /// hereingereicht und gibt eine Auskunft zurück. Ihre Texte kommen aus
    /// <c>MyResource.Resource.WIRT_SICHT_*</c> (Drei-Schichten-Regel).</para>
    /// </summary>
    public sealed class Referenzwahl
    {
        /// <summary>
        /// Die WIRKSAME Referenz — <c>Tab_Projekt.ID</c>. 0 nur dann, wenn die Gruppe
        /// leer ist; sonst immer ein Stand der Gruppe.
        /// </summary>
        public int IdReferenz;

        /// <summary>Der Anzeigename der wirksamen Referenz (leer = unbekannt).</summary>
        public string Anzeige = "";

        /// <summary>
        /// true, wenn die GEWÄHLTE Referenz nicht (mehr) in der Gruppe stand und
        /// deshalb der Stamm einspringt. Die Spalte wird dabei NICHT still bereinigt —
        /// der Rückfall steht in <see cref="Warnung"/>.
        /// </summary>
        public bool Rueckfall;

        /// <summary>Benennt den Rückfall; <c>null</c> = kein Rückfall.</summary>
        public string Warnung;

        /// <summary>Ist dieser Stand die Referenz?</summary>
        public bool IstReferenz(int idProjekt) { return idProjekt == IdReferenz; }

        /// <summary>
        /// Löst die Referenz gegen die Vergleichsgruppe auf.
        /// </summary>
        /// <param name="daten">Die gesammelte Gruppe (Stamm zuerst); <c>null</c> = leere Auskunft.</param>
        /// <param name="idGewaehlt">
        /// Die gewünschte Referenz. <b>0 = Stamm</b> — die Vorgabe, mit der sich jede
        /// Bestandsrechnung unverändert verhält.
        /// </param>
        public static Referenzwahl Bestimme(BerichtsDaten daten, int idGewaehlt)
        {
            if (daten == null) return new Referenzwahl();
            return Bestimme(daten.Varianten, daten.IdStamm, idGewaehlt);
        }

        /// <summary>
        /// Dieselbe Auflösung ohne <see cref="BerichtsDaten"/> — für die Anzeige, die
        /// nur Ids und Namen der Gruppe kennt.
        /// </summary>
        /// <param name="gruppe">Die Ids der Gruppe in Listenreihenfolge (Stamm zuerst).</param>
        /// <param name="idStamm">Die Id des Stammprojekts.</param>
        /// <param name="idGewaehlt">Die gewünschte Referenz; 0 = Stamm.</param>
        /// <param name="name">Namensgeber je Id (<c>null</c> = ohne Namen).</param>
        public static Referenzwahl Bestimme(IEnumerable<int> gruppe, int idStamm,
                                            int idGewaehlt, Func<int, string> name)
        {
            var w = new Referenzwahl();
            bool stammDa = false, gewaehltDa = false;
            if (gruppe != null)
                foreach (int id in gruppe)
                {
                    if (id == idStamm) stammDa = true;
                    if (idGewaehlt > 0 && id == idGewaehlt) gewaehltDa = true;
                }

            if (gewaehltDa) w.IdReferenz = idGewaehlt;
            else if (stammDa || idStamm > 0)
            {
                w.IdReferenz = idStamm;
                // Der Rückfall wird BENANNT und nicht still vollzogen: Eine Gruppe, die
                // gestern gegen eine Variante rechnete und heute gegen den Stamm, hat
                // andere Zahlen - das muss dastehen (§ 2.9, Randfall 2).
                if (idGewaehlt > 0 && idGewaehlt != idStamm) w.Rueckfall = true;
            }

            w.Anzeige = name != null && w.IdReferenz > 0 ? (name(w.IdReferenz) ?? "") : "";
            if (w.Rueckfall)
                w.Warnung = string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.WIRT_SICHT_RUECKFALL, w.Anzeige);
            return w;
        }

        /// <summary>Auflösung über die gesammelten Varianten (mit Namen aus der Gruppe).</summary>
        public static Referenzwahl Bestimme(IList<VariantenDaten> varianten, int idStamm,
                                            int idGewaehlt)
        {
            var ids = new List<int>();
            var namen = new Dictionary<int, string>();
            if (varianten != null)
                foreach (VariantenDaten v in varianten)
                {
                    if (v == null) continue;
                    ids.Add(v.IdProjekt);
                    if (!namen.ContainsKey(v.IdProjekt)) namen[v.IdProjekt] = Name(v);
                }
            return Bestimme(ids, idStamm, idGewaehlt,
                            id => namen.ContainsKey(id) ? namen[id] : "");
        }

        /// <summary>Der Anzeigename eines Standes — beim Stamm der Projektname.</summary>
        public static string Name(VariantenDaten v)
        {
            if (v == null) return "";
            if (!v.IstStamm && !string.IsNullOrEmpty(v.Variantenname)) return v.Variantenname;
            return v.Projektname ?? "";
        }

        // ------------------------------------------------------------- Nachweistexte

        /// <summary>
        /// Die Nachweiszeile der Sicht 1 („Referenz: ‹Name›" · „Differenz = Variante −
        /// Referenz") — sie ersetzt den festen Satz „Referenz: Stammprojekt".
        /// </summary>
        public static string Nachweiszeile(string referenz)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.WIRT_SICHT_GRUPPE, referenz ?? "") +
                   " · " + MyResource.Resource.WIRT_SICHT_DIFF_ALLE;
        }

        /// <summary>
        /// Die Nachweiszeile der Sicht 2 („Referenz dieser Sicht: ‹A› · Referenz der
        /// Gruppe: ‹Referenz›" · „Differenz = B − A", § 2.15).
        /// </summary>
        public static string Nachweiszeile(string a, string gruppenreferenz)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.WIRT_SICHT_REFERENZ,
                                 a ?? "", gruppenreferenz ?? "") +
                   " · " + MyResource.Resource.WIRT_SICHT_DIFF_PAAR;
        }

        /// <summary>
        /// Die Deklarationszeile des BERICHTS in Sicht 2 (§ 2.15, VG‑Q4): „Referenz
        /// dieser Bewertung: ‹A› · Unterlassensalternative der Gruppe: ‹Referenz›".
        /// </summary>
        public static string Deklarationszeile(string a, string gruppenreferenz)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.WIRT_SICHT_DEKLARATION,
                                 a ?? "", gruppenreferenz ?? "");
        }

        /// <summary>
        /// Die Deklaration der ValERI-Bewertung in Sicht 2 (§ 2.15, VG‑Q6): Die Norm
        /// lässt die Wahl unter Alternativen über den höheren Kapitalwert zu (8.1.2),
        /// verlangt aber die Benennung.
        /// </summary>
        public static string ValeriDeklaration(string gruppenreferenz)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.WIRT_SICHT_VALERI, gruppenreferenz ?? "");
        }

        /// <summary>
        /// Der Satz für den Randfall „Referenz ohne Rechenergebnis" (§ 2.9): nie ein
        /// stiller Rückfall auf den Stamm, sondern der benannte Grund.
        /// </summary>
        public static string ReferenzFehlt(string referenz, string grund)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.WIRT_SICHT_REF_FEHLT,
                                 referenz ?? "", string.IsNullOrEmpty(grund) ? "—" : grund);
        }
    }
}
