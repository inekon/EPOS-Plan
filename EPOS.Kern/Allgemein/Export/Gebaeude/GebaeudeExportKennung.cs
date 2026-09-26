using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennungen des Gebäudeexports</b> (Stufe G7a; Entscheid D5, Datenaustauschkonzept 5.4 in
    /// der Fassung des Umsetzungsauftrags G7a, Abschnitt 2.2) — deterministisch aus den
    /// EPOS-Schlüsseln und festen Kürzeln, <b>nie aus Namen</b>. Ein zweiter Export desselben Stands
    /// trägt dieselben Kennungen; ein dupliziertes Projekt trägt andere, weil seine Zeilen andere IDs
    /// haben.
    ///
    /// <para><b>Form:</b> Jede Kennung beginnt mit <c>epos-</c>, enthält nur Kleinbuchstaben, Ziffern
    /// und Bindestriche und ist damit ein gültiger <c>xsd:ID</c> (NCName, beginnt nicht mit einer
    /// Ziffer). <b>Ein Schlüssel ≤ 0 ist ein Programmfehler</b> und wirft — eine noch nicht
    /// gespeicherte Zeile (negative vorläufige Id) oder die übernommene Zone des Klassenwegs (Id −1/0)
    /// bekommt nie eine Kennung aus ihrer Id.</para>
    ///
    /// <para><b>Ein Aufbau je Übergangsfall:</b> Der U-Wert eines Aufbaus hängt an R_si und R_se, also
    /// an der wirksamen Neigung und der Randbedingung des Bauteils (Datenaustauschkonzept 5.3). Deshalb
    /// gibt es je Aufbau und Fall eine <c>Construction</c> (<see cref="Aufbau"/>); Schichten und
    /// Baustoffe teilen die Fälle, nur eine ruhende Luftschicht nicht — ihr Widerstand hängt an der
    /// Wärmestromrichtung (<see cref="SchichtRuhendeLuft"/>). Die Schicht-ID der Datenbank ist nicht
    /// stabil (beim Speichern werden die Schichten neu angelegt), deshalb zählt die Reihenfolge.</para>
    /// </summary>
    internal static class GebaeudeExportKennung
    {
        /// <summary>Kennung der <c>ProgramInfo</c> in <c>DocumentHistory</c>.</summary>
        internal const string PROGRAMM = "epos-programm";

        /// <summary>Kennung der neutralen <c>PersonInfo</c> in <c>DocumentHistory</c> (keine Anwenderdaten).</summary>
        internal const string PERSON = "epos-person";

        // ------------------------------------------------------------------
        //  Gebäude, Räume, Zonen
        // ------------------------------------------------------------------

        /// <summary><c>Campus</c> eines Gebäudes: <c>epos-campus-&lt;Geb.ID&gt;</c> (ID der Projektkopie).</summary>
        internal static string Campus(int idGebaeude) => "epos-campus-" + Schluessel(idGebaeude, nameof(idGebaeude));

        /// <summary><c>Building</c>: <c>epos-gebaeude-&lt;Geb.ID&gt;</c> (ID der Projektkopie).</summary>
        internal static string Gebaeude(int idGebaeude) => "epos-gebaeude-" + Schluessel(idGebaeude, nameof(idGebaeude));

        /// <summary><c>Space</c> einer Zone: <c>epos-raum-&lt;Zone.ID&gt;</c>.</summary>
        internal static string Raum(int idZone) => "epos-raum-" + Schluessel(idZone, nameof(idZone));

        /// <summary><c>Zone</c>: <c>epos-zone-&lt;Zone.ID&gt;</c>.</summary>
        internal static string Zone(int idZone) => "epos-zone-" + Schluessel(idZone, nameof(idZone));

        /// <summary>Der Platzhalter-<c>Space</c> „unbeheizt" eines Gebäudes (ohne Zone): <c>epos-unbeheizt-&lt;Geb.ID&gt;</c>.</summary>
        internal static string Unbeheizt(int idGebaeude) => "epos-unbeheizt-" + Schluessel(idGebaeude, nameof(idGebaeude));

        // ------------------------------------------------------------------
        //  Bauteile und Öffnungen
        // ------------------------------------------------------------------

        /// <summary><c>Surface</c> eines Bauteils: <c>epos-bauteil-&lt;Bauteil.ID&gt;</c> (ein Innenpaar nach der kleineren ID).</summary>
        internal static string Bauteil(int idBauteil) => "epos-bauteil-" + Schluessel(idBauteil, nameof(idBauteil));

        /// <summary><c>Opening</c> eines Fensters, einer Tür oder einer Vorhangfassade: <c>epos-oeffnung-&lt;Bauteil.ID&gt;</c>.</summary>
        internal static string Oeffnung(int idBauteil) => "epos-oeffnung-" + Schluessel(idBauteil, nameof(idBauteil));

        /// <summary><c>WindowType</c> je Fensterbauteil (g je Bauteil): <c>epos-fenstertyp-&lt;Bauteil.ID&gt;</c>.</summary>
        internal static string Fenstertyp(int idBauteil) => "epos-fenstertyp-" + Schluessel(idBauteil, nameof(idBauteil));

        /// <summary>Die Fläche innerer Masse des Gebäudes, wo keine Innenbauteile stehen: <c>epos-innenmasse-&lt;Geb.ID&gt;</c>.</summary>
        internal static string Innenmasse(int idGebaeude) => "epos-innenmasse-" + Schluessel(idGebaeude, nameof(idGebaeude));

        // ------------------------------------------------------------------
        //  Aufbauten, Schichten, Baustoffe
        // ------------------------------------------------------------------

        /// <summary>
        /// <c>Construction</c> eines Aufbaus in einem Übergangsfall:
        /// <c>epos-aufbau-&lt;Aufbau.ID&gt;-&lt;Richtung&gt;-&lt;Rand&gt;</c>, Richtung aus der wirksamen
        /// Neigung (<see cref="Richtungskuerzel"/>), Rand aus der Randbedingung (<see cref="Randkuerzel"/>).
        /// </summary>
        internal static string Aufbau(int idAufbau, Waermestromrichtung richtung, Bauteilrand rand)
            => "epos-aufbau-" + Schluessel(idAufbau, nameof(idAufbau)) + "-" + Richtungskuerzel(richtung) + "-" + Randkuerzel(rand);

        /// <summary><c>Layer</c> einer Schicht: <c>epos-schicht-&lt;Aufbau.ID&gt;-&lt;Reihenfolge&gt;</c>.</summary>
        internal static string Schicht(int idAufbau, int reihenfolge)
            => "epos-schicht-" + Schluessel(idAufbau, nameof(idAufbau)) + "-" + Schluessel(reihenfolge, nameof(reihenfolge));

        /// <summary><c>Material</c> einer Schicht: <c>epos-stoff-&lt;Aufbau.ID&gt;-&lt;Reihenfolge&gt;</c>.</summary>
        internal static string Stoff(int idAufbau, int reihenfolge)
            => "epos-stoff-" + Schluessel(idAufbau, nameof(idAufbau)) + "-" + Schluessel(reihenfolge, nameof(reihenfolge));

        /// <summary><c>Layer</c> einer ruhenden Luftschicht, je Wärmestromrichtung: <c>epos-schicht-&lt;Aufbau.ID&gt;-&lt;Reihenfolge&gt;-&lt;Richtung&gt;</c>.</summary>
        internal static string SchichtRuhendeLuft(int idAufbau, int reihenfolge, Waermestromrichtung richtung)
            => Schicht(idAufbau, reihenfolge) + "-" + Richtungskuerzel(richtung);

        /// <summary><c>Material</c> einer ruhenden Luftschicht, je Wärmestromrichtung: <c>epos-stoff-&lt;Aufbau.ID&gt;-&lt;Reihenfolge&gt;-&lt;Richtung&gt;</c>.</summary>
        internal static string StoffRuhendeLuft(int idAufbau, int reihenfolge, Waermestromrichtung richtung)
            => Stoff(idAufbau, reihenfolge) + "-" + Richtungskuerzel(richtung);

        /// <summary>Die Ersatzschichtung eines Bauteils ohne Schichten — <c>Construction</c>: <c>epos-aufbau-bauteil-&lt;Bauteil.ID&gt;</c>.</summary>
        internal static string ErsatzAufbau(int idBauteil) => "epos-aufbau-bauteil-" + Schluessel(idBauteil, nameof(idBauteil));

        /// <summary>Die Ersatzschicht eines Bauteils — <c>Layer</c>: <c>epos-schicht-bauteil-&lt;Bauteil.ID&gt;</c>.</summary>
        internal static string ErsatzSchicht(int idBauteil) => "epos-schicht-bauteil-" + Schluessel(idBauteil, nameof(idBauteil));

        /// <summary>Der Ersatzbaustoff eines Bauteils — <c>Material</c>: <c>epos-stoff-bauteil-&lt;Bauteil.ID&gt;</c>.</summary>
        internal static string ErsatzStoff(int idBauteil) => "epos-stoff-bauteil-" + Schluessel(idBauteil, nameof(idBauteil));

        /// <summary>Die Ersatzschichtung der Fläche innerer Masse — <c>Construction</c>: <c>epos-aufbau-innenmasse-&lt;Geb.ID&gt;</c>.</summary>
        internal static string InnenmasseAufbau(int idGebaeude) => "epos-aufbau-innenmasse-" + Schluessel(idGebaeude, nameof(idGebaeude));

        /// <summary>Die Ersatzschicht der Fläche innerer Masse — <c>Layer</c>: <c>epos-schicht-innenmasse-&lt;Geb.ID&gt;</c>.</summary>
        internal static string InnenmasseSchicht(int idGebaeude) => "epos-schicht-innenmasse-" + Schluessel(idGebaeude, nameof(idGebaeude));

        /// <summary>Der Ersatzbaustoff der Fläche innerer Masse — <c>Material</c>: <c>epos-stoff-innenmasse-&lt;Geb.ID&gt;</c>.</summary>
        internal static string InnenmasseStoff(int idGebaeude) => "epos-stoff-innenmasse-" + Schluessel(idGebaeude, nameof(idGebaeude));

        // ------------------------------------------------------------------
        //  Klassenweg (Gebäude ohne Zone; die übernommene Zone trägt Id −1/0)
        // ------------------------------------------------------------------

        /// <summary>Der <c>Space</c> des Klassenwegs: <c>epos-raum-klasse-&lt;Geb.ID&gt;</c>.</summary>
        internal static string KlassenRaum(int idGebaeude) => "epos-raum-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude));

        /// <summary>Die <c>Zone</c> des Klassenwegs: <c>epos-zone-klasse-&lt;Geb.ID&gt;</c>.</summary>
        internal static string KlassenZone(int idGebaeude) => "epos-zone-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude));

        /// <summary>Ein Bauteil des Übernahmevorschlags (<c>Surface</c> bzw. <c>Opening</c>): <c>epos-klasse-&lt;Geb.ID&gt;-&lt;Rang&gt;</c>.</summary>
        internal static string KlassenBauteil(int idGebaeude, int rang)
            => "epos-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude)) + "-" + Schluessel(rang, nameof(rang));

        /// <summary>Die Ersatzschichtung eines Bauteils des Klassenwegs — <c>Construction</c>: <c>epos-aufbau-klasse-&lt;Geb.ID&gt;-&lt;Rang&gt;</c>.</summary>
        internal static string KlassenAufbau(int idGebaeude, int rang)
            => "epos-aufbau-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude)) + "-" + Schluessel(rang, nameof(rang));

        /// <summary>Die Ersatzschicht eines Bauteils des Klassenwegs — <c>Layer</c>: <c>epos-schicht-klasse-&lt;Geb.ID&gt;-&lt;Rang&gt;</c>.</summary>
        internal static string KlassenSchicht(int idGebaeude, int rang)
            => "epos-schicht-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude)) + "-" + Schluessel(rang, nameof(rang));

        /// <summary>Der Ersatzbaustoff eines Bauteils des Klassenwegs — <c>Material</c>: <c>epos-stoff-klasse-&lt;Geb.ID&gt;-&lt;Rang&gt;</c>.</summary>
        internal static string KlassenStoff(int idGebaeude, int rang)
            => "epos-stoff-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude)) + "-" + Schluessel(rang, nameof(rang));

        /// <summary>Der Fenstertyp eines Fensters des Klassenwegs — <c>WindowType</c>: <c>epos-fenstertyp-klasse-&lt;Geb.ID&gt;-&lt;Rang&gt;</c>.</summary>
        internal static string KlassenFenstertyp(int idGebaeude, int rang)
            => "epos-fenstertyp-klasse-" + Schluessel(idGebaeude, nameof(idGebaeude)) + "-" + Schluessel(rang, nameof(rang));

        // ------------------------------------------------------------------
        //  Kürzel
        // ------------------------------------------------------------------

        /// <summary>Das Kürzel der Wärmestromrichtung: <c>auf</c>, <c>hor</c>, <c>ab</c>.</summary>
        internal static string Richtungskuerzel(Waermestromrichtung richtung)
        {
            switch (richtung)
            {
                case Waermestromrichtung.Aufwaerts: return "auf";
                case Waermestromrichtung.Horizontal: return "hor";
                case Waermestromrichtung.Abwaerts: return "ab";
                default: throw new ArgumentOutOfRangeException(nameof(richtung), richtung, "Unbekannte Wärmestromrichtung.");
            }
        }

        /// <summary>
        /// Das Kürzel der Randbedingung: <c>al</c> (Außenluft), <c>er</c> (Erdreich), <c>ub</c>
        /// (unbeheizt), <c>in</c> (innerhalb der Zone), <c>zo</c> (Trennfläche zur Nachbarzone).
        /// </summary>
        internal static string Randkuerzel(Bauteilrand rand)
        {
            switch (rand)
            {
                case Bauteilrand.Aussenluft: return "al";
                case Bauteilrand.Erdreich: return "er";
                case Bauteilrand.Unbeheizt: return "ub";
                case Bauteilrand.Innen: return "in";
                case Bauteilrand.Zone: return "zo";
                default: throw new ArgumentOutOfRangeException(nameof(rand), rand, "Die Randbedingung hat im Export kein Kürzel.");
            }
        }

        /// <summary>Ein Schlüssel als Zahl; ≤ 0 wirft (Programmfehler, Klassenkopf).</summary>
        private static string Schluessel(int wert, string name)
        {
            if (wert <= 0)
                throw new ArgumentOutOfRangeException(name, wert, "Eine Exportkennung braucht einen Schlüssel > 0.");
            return wert.ToString(CultureInfo.InvariantCulture);
        }
    }
}
