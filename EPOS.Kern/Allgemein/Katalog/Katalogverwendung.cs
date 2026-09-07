using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Spalte „im Projekt verwendet"</b> — Frage <b>Q12</b> des
    /// <c>Konzept_Katalogfilter</c>, Stufe S2.3. Sie gibt es NUR in den sieben
    /// Projektdialogen; in der Verwaltung waere sie eine Zaehlung ueber alle
    /// Projekte ohne Nutzen fuer die Pflege.
    ///
    /// <para><b>Sie kommt aus der LEBENDEN Projektliste, nicht aus einer Abfrage</b>
    /// — und das ist keine Abkuerzung, sondern die einzige richtige Quelle. Die
    /// Projektdialoge schreiben ihre Liste erst beim OK zurueck („DER DIALOG
    /// SCHREIBT BEIM OK NICHTS. Der Aufrufer loescht danach die Tab_Energieanlagen
    /// dieses Typs und schreibt die Liste neu"); eine Zaehlabfrage waere in dem
    /// Augenblick veraltet, in dem der Anwender eine Zeile uebernimmt oder
    /// entfernt. Die Liste, die oben im Dialog steht, ist die Wahrheit — und sie
    /// steht ohnehin da.</para>
    ///
    /// <para><b>EINMAL fuer die ganze Liste, nicht je Zeile.</b> Die Namen des
    /// Projekts wandern in eine Menge, und danach ist jede Zeile ein Nachschlagen
    /// in konstanter Zeit. Fuer die 20 749 PV-Module heisst das ein Durchlauf statt
    /// 20 749 Vergleichsschleifen — genau die Forderung aus S2.3.</para>
    ///
    /// <para><b>Gross/klein egal, wie ueberall im Katalogfilter</b>
    /// (<c>CurrentCultureIgnoreCase</c>, dieselbe Wahl wie
    /// <c>VdiAuswahlFilter.Passt</c>): Der Bezeichner einer Projektkopie und der des
    /// Katalogsatzes stammen aus derselben Quelle, aber die Kopie kann von Hand
    /// umbenannt worden sein.</para>
    /// </summary>
    public static class Katalogverwendung
    {
        /// <summary>
        /// Setzt in jeder Katalogzeile das Kennzeichen
        /// <see cref="Katalogfilterprofil.SpVerwendet"/> — „Ja", wenn ihr Bezeichner
        /// in <paramref name="imProjekt"/> steht.
        /// </summary>
        /// <param name="katalog">Die Zeilen der Katalogliste; <c>null</c> ist erlaubt.</param>
        /// <param name="imProjekt">
        /// Die Bezeichner der PROJEKTliste, wie sie gerade dasteht. <c>null</c> oder
        /// leer heisst: keine Zeile ist verwendet — dann traegt jede Zeile „Nein".
        /// </param>
        /// <returns>Wie viele Zeilen „Ja" bekommen haben (Pruefhilfe und Auskunft).</returns>
        public static int Stempeln(IEnumerable<Katalogfilterzeile> katalog,
                                   IEnumerable<string> imProjekt)
        {
            if (katalog == null) return 0;

            var namen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            if (imProjekt != null)
                foreach (string n in imProjekt)
                    if (!string.IsNullOrWhiteSpace(n)) namen.Add(n.Trim());

            int treffer = 0;
            foreach (Katalogfilterzeile z in katalog)
            {
                if (z == null) continue;
                bool ja = namen.Contains((z.Bezeichner ?? "").Trim());
                if (ja) treffer++;
                z.MitKennzeichen(Katalogfilterprofil.SpVerwendet, ja);
            }
            return treffer;
        }
    }
}
