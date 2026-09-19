using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// KONZEPT § 2.15 — <b>welche Stände die Vergleichstafeln gegeneinander stellen</b>:
    /// alle angehakten Varianten gegen die Referenz der Gruppe (Sicht 1, der Bestand)
    /// oder zwei Stände A und B (Sicht 2).
    ///
    /// <para><b>Sicht 2 setzt A als Referenz DIESES Rechenlaufs</b> (VG‑Q1), ohne die
    /// gespeicherte Gruppenreferenz anzufassen. Die Alternative — die Paarwahl als reine
    /// Anzeige über der Referenzrechnung — trägt nur für die linearen Größen:
    /// Kapitalwertdifferenz, Annuität, Bandbreite, Gliederung und Brücke lassen sich aus
    /// Sicht 1 bilden, dynamische Amortisation und interner Zinsfuß nicht. Die hängen an
    /// der Jahresreihe der Differenz B − A und brauchen den Rechenlauf mit A als
    /// Gegenstück. Ein zweiter Rechenweg wäre eine zweite Wahrheit.</para>
    ///
    /// <para><b>Sitzung, nicht Datenbank</b> (VG‑Q3): Die Wahl lebt neben den Häkchen in
    /// <see cref="Vergleichsauswahl"/>. Eine gespeicherte Paarwahl wäre eine zweite
    /// Referenzangabe neben <c>ID_Referenzprojekt</c> — zwei Spalten, die dasselbe meinen
    /// können und sich widersprechen dürfen. Wer einen Paarvergleich dauerhaft will,
    /// wählt A als Gruppenreferenz (§ 2.9).</para>
    /// </summary>
    public sealed class Vergleichssicht
    {
        /// <summary>Sicht 1 — alle Varianten gegen die Referenz der Gruppe (Vorgabe).</summary>
        public const int ALLE = 0;

        /// <summary>Sicht 2 — zwei Stände A und B.</summary>
        public const int PAAR = 1;

        /// <summary><see cref="ALLE"/> oder <see cref="PAAR"/>.</summary>
        public int Sicht;

        /// <summary>Der Stand A — in Sicht 2 die Referenz dieser Sicht.</summary>
        public int IdA;

        /// <summary>Der Stand B — in Sicht 2 der Stand, dessen Differenz gezeigt wird.</summary>
        public int IdB;

        /// <summary>
        /// Ist die Paarsicht wirksam? A ≠ B ist Bedingung: Eine Paarwahl mit demselben
        /// Stand zweimal wäre eine Differenz gegen sich selbst.
        /// </summary>
        public bool IstPaar
        {
            get { return Sicht == PAAR && IdA > 0 && IdB > 0 && IdA != IdB; }
        }

        /// <summary>
        /// Die Referenz des Rechenlaufs dieser Sicht: in Sicht 2 <see cref="IdA"/>,
        /// sonst 0 — und 0 heißt „die Gruppenreferenz" (§ 2.9).
        /// </summary>
        public int Referenz { get { return IstPaar ? IdA : 0; } }

        /// <summary>
        /// Die Spalten dieser Sicht: in Sicht 2 genau A und B in dieser Reihenfolge,
        /// sonst die angehakten Stände, wie sie sind.
        /// </summary>
        public List<int> Spalten(IEnumerable<int> gewaehlte)
        {
            var l = new List<int>();
            if (IstPaar) { l.Add(IdA); l.Add(IdB); return l; }
            if (gewaehlte != null) foreach (int id in gewaehlte) l.Add(id);
            return l;
        }

        /// <summary>Eine wertgleiche Kopie — die Sitzungswahl wandert als Momentaufnahme
        /// in den Berichtslauf, damit sie sich während des Drucks nicht ändert.</summary>
        public Vergleichssicht Kopie()
        {
            return new Vergleichssicht { Sicht = Sicht, IdA = IdA, IdB = IdB };
        }
    }
}
