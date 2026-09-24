using System;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Beleg einer Zahl</b> — woraus sie stammt (Datenaustauschkonzept 2.2), sprachneutral
    /// wie <see cref="PruefMeldung"/>: ein Ressourcenschlüssel und die eingesetzten Werte, bereits
    /// invariant formatiert. Den Text holt erst die Oberfläche
    /// (<see cref="GebaeudeZuordnungsModell.BelegText"/>).
    /// </summary>
    internal sealed class GebaeudeBeleg
    {
        /// <summary>Legt einen Beleg an.</summary>
        /// <param name="schluessel">Ressourcenschlüssel (<c>GIMP_BELEG_*</c>).</param>
        /// <param name="werte">Platzhalterwerte, invariant formatiert.</param>
        public GebaeudeBeleg(string schluessel, params string[] werte)
        {
            Schluessel = schluessel ?? "";
            Werte = werte ?? Array.Empty<string>();
        }

        /// <summary>Sprachneutraler Schlüssel; Name des Ressourcenschlüssels.</summary>
        public string Schluessel { get; }

        /// <summary>Platzhalterwerte <c>{0}</c>, <c>{1}</c> …, invariant formatiert.</summary>
        public string[] Werte { get; }

        /// <summary>Sprachunabhängige Kurzfassung für Protokolle und Tests: <c>SCHLUESSEL: w1; w2</c>.</summary>
        public override string ToString()
            => Werte.Length == 0 ? Schluessel : Schluessel + ": " + string.Join("; ", Werte);
    }

    /// <summary>
    /// <b>Eine Zeile des Zuordnungsdialogs — je ZIELFELD eine</b> (Datenaustauschkonzept 2.1,
    /// Softwarearchitektur 1.5): Zielfeld, Gruppe, Wert, Einheit, Herkunft, Beleg, Vorgabewert und
    /// Haken.
    ///
    /// <para><b>Wert und Vorgabe stehen nebeneinander.</b> <see cref="Wert"/> ist die Zahl, die mit
    /// dem Haken übernommen würde — gelesen (<see cref="Importherkunft.GbXml"/>), vorbelegt
    /// (<see cref="Importherkunft.Vorgabe"/>) oder leer; <see cref="VorgabeWert"/> ist der Wert der
    /// Baualtersklasse, auch dann, wenn die Datei eine eigene Zahl liefert. So sieht der Anwender je
    /// Zelle, ob eine Zahl gelesen, vorbelegt oder geraten ist (E2).</para>
    ///
    /// <para><b>Die Herkunft je Feld wird nicht persistiert</b> (Softwarearchitektur 2.7): Sie lebt
    /// nur bis zum Schließen des Dialogs.</para>
    /// </summary>
    internal sealed class GebaeudeFeldzeile
    {
        /// <summary>Legt eine Zeile zu einem Zielfeld an; Gruppe und Einheit kommen aus <see cref="GebaeudeZielfelder"/>.</summary>
        public GebaeudeFeldzeile(string zielfeld)
        {
            GebaeudeZielfeld f = GebaeudeZielfelder.Finde(zielfeld);
            if (f == null) throw new ArgumentException("Unbekanntes Zielfeld: " + zielfeld, nameof(zielfeld));
            Zielfeld = f.Schluessel;
            Gruppe = f.Gruppe;
            Einheit = f.Einheit;
            Reihenfolge = f.Reihenfolge;
        }

        /// <summary>Sprachneutraler Schlüssel des Zielfelds.</summary>
        public string Zielfeld { get; }

        /// <summary>Sprachneutraler Schlüssel der Gruppe.</summary>
        public string Gruppe { get; }

        /// <summary>Einheitenzeichen.</summary>
        public string Einheit { get; }

        /// <summary>Position in der Zeilenliste.</summary>
        public int Reihenfolge { get; }

        /// <summary>Der zu übernehmende Zahlenwert in der <see cref="Einheit"/>; <c>null</c> = leer.</summary>
        public double? Wert { get; set; }

        /// <summary>Der zu übernehmende Aufzählungswert (Baualtersklasse, Bauart, Randbedingung); <c>null</c> = leer.</summary>
        public string Textwert { get; set; }

        /// <summary>Woher <see cref="Wert"/> bzw. <see cref="Textwert"/> kommt.</summary>
        public Importherkunft Herkunft { get; set; }

        /// <summary>Woraus der Wert stammt; <c>null</c> = kein Beleg (dann ist er eine Vorgabe oder leer).</summary>
        public GebaeudeBeleg Beleg { get; set; }

        /// <summary>Der Vorgabewert der Baualtersklasse; <c>null</c> = die Klasse hat keinen.</summary>
        public double? VorgabeWert { get; set; }

        /// <summary>Beleg des Vorgabewerts (Klasse und Katalogherkunft).</summary>
        public GebaeudeBeleg VorgabeBeleg { get; set; }

        /// <summary>
        /// Bruttowert einer Flächenzeile — Bauteilflächen einschließlich ihrer Öffnungen (U14); der
        /// Export braucht ihn (Datenaustauschkonzept 5.5, Punkt 2). <c>null</c> bei Zeilen ohne
        /// Öffnungsabzug.
        /// </summary>
        public double? Bruttowert { get; set; }

        /// <summary>Wird die Zeile übernommen (der Haken)?</summary>
        public bool Uebernehmen { get; set; }

        /// <summary>
        /// Markierung der Zeile: <see cref="PruefStufe.Warnung"/> gelb, <see cref="PruefStufe.Fehler"/>
        /// rot; <c>null</c> = unauffällig. Eine übernommene Fehlerzeile blockiert die Übernahme
        /// (<see cref="GebaeudeImportAblauf.Pruefen"/>).
        /// </summary>
        public PruefStufe? Markierung { get; set; }

        /// <summary>Hat die Zeile einen Wert (Zahl oder Aufzählung)?</summary>
        public bool HatWert => Wert.HasValue || Textwert != null;

        /// <summary>Setzt die Markierung, eine schärfere Stufe schlägt eine mildere.</summary>
        public void Markieren(PruefStufe stufe)
        {
            if (!Markierung.HasValue || stufe > Markierung.Value) Markierung = stufe;
        }

        /// <summary>Sprachunabhängige Kurzfassung für Tests: <c>ZIELFELD = wert (Herkunft)</c>.</summary>
        public override string ToString()
        {
            string wert = Textwert ?? (Wert.HasValue
                ? Wert.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                : "leer");
            return Zielfeld + " = " + wert + " (" + Herkunft + ")";
        }
    }
}
