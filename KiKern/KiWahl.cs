using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KiKern
{
    /// <summary>
    /// EIN Eintrag eines Wahlfeldes: der Schluessel, der in die Eigenschaft geschrieben
    /// wird, und der Text, den der Anwender auf der Maske liest (KI-F1b, KI-D-Q6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Die Eintraege kommen zur Laufzeit von der MASKE, nie aus dem Katalog.</b> Eine
    /// Klappliste fuehrt Energietraeger, Module, Geraete oder Preisreihen aus der
    /// Datenbank; eine feste Liste im Katalog waere am Tag ihrer Deklaration richtig und
    /// danach nie wieder. Deshalb deklariert der Katalog nur die ART
    /// (<see cref="KiParameterTyp.Wahl"/>), und der Feldzugang der Bruecke liefert die
    /// Eintraege bei jedem Zugriff frisch.
    /// </para>
    /// <para>
    /// <b>Schluessel und Text sind beide Text.</b> Was die Zieleigenschaft daraus macht
    /// (<c>int</c>, <c>string</c>, eine Aufzaehlung), entscheidet der Wandler an der
    /// Maske; der Kern haelt nur das Paar zusammen.
    /// </para>
    /// </remarks>
    public sealed class KiWahleintrag
    {
        /// <summary>Legt einen Eintrag an.</summary>
        /// <param name="schluessel">Der Wert, der gesetzt wird (Id, Bezeichner, Aufzaehlungsname).</param>
        /// <param name="text">Der Text, der auf der Maske steht; leer = der Schluessel.</param>
        public KiWahleintrag(string? schluessel, string? text = null)
        {
            Schluessel = (schluessel ?? "").Trim();
            Text = string.IsNullOrWhiteSpace(text) ? Schluessel : text!.Trim();
        }

        /// <summary>Der Wert, der in die Eigenschaft geschrieben wird.</summary>
        public string Schluessel { get; }

        /// <summary>Der Text, den der Anwender auf der Maske liest.</summary>
        public string Text { get; }

        /// <summary>Der Eintrag fuer die Anzeige: „Text (Schluessel)" - oder nur der Text.</summary>
        public string Beschriftung
            => string.Equals(Text, Schluessel, StringComparison.Ordinal)
                   ? Text
                   : Text + " (" + Schluessel + ")";

        /// <inheritdoc/>
        public override string ToString() => Beschriftung;
    }

    /// <summary>
    /// Das Ergebnis einer toleranten Namenssuche: der eine gemeinte Eintrag, oder die
    /// Kandidaten, zwischen denen NICHT geraten wird.
    /// </summary>
    public sealed class KiWahltreffer
    {
        internal KiWahltreffer(int stelle, IReadOnlyList<string> kandidaten)
        {
            Stelle = stelle;
            Kandidaten = kandidaten;
        }

        /// <summary>Die Stelle des Treffers in der uebergebenen Liste; <c>-1</c> = keiner.</summary>
        public int Stelle { get; }

        /// <summary>
        /// Die Beschriftungen der in Frage kommenden Eintraege - belegt, sobald mehr als
        /// einer passt. Leer, wenn gar keiner passt oder genau einer.
        /// </summary>
        public IReadOnlyList<string> Kandidaten { get; }

        /// <summary>Genau ein Eintrag passt.</summary>
        public bool Eindeutig => Stelle >= 0;

        /// <summary>Mehrere Eintraege passen - es wird nicht geraten.</summary>
        public bool Mehrdeutig => Stelle < 0 && Kandidaten.Count > 0;
    }

    /// <summary>
    /// Die EINE Namensregel des Assistenten: Wie ein genannter Text auf einen Schluessel
    /// trifft - beim Wert eines Wahlfeldes wie beim Namen eines Feldes (KI-F1b, KI-D-Q6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum EINE Regel fuer beides.</b> „vorlauftemperatur" soll das Feld
    /// <c>vorlauf</c> treffen und „erdgas" den Energietraeger „Erdgas H"; das ist
    /// zweimal dieselbe Frage - welcher von mehreren beschrifteten Schluesseln ist
    /// gemeint? Zwei Regeln waeren die naechste Stelle, an der sich Ablehnung und
    /// Annahme auseinanderentwickeln.
    /// </para>
    /// <para>
    /// <b>Vier Stufen, und die erste, die ETWAS findet, entscheidet.</b> Exakter
    /// Schluessel, exakter Anzeigetext, eindeutiger Anfang (in beide Richtungen -
    /// „vorlauftemperatur" beginnt mit „vorlauf"), eindeutig enthaltener Teil. Findet
    /// eine Stufe mehrere, wird NICHT zur naechsten weitergegangen: Mehrdeutig bleibt
    /// mehrdeutig, und die Absage nennt die Kandidaten
    /// (Bestandsregel, <c>EPOS.Kern.Tests/KiMaskenwegTests</c>).
    /// </para>
    /// <para>
    /// <b>Gefaltet wird gross/klein, Umlaut, Unterstrich und Leerzeichen</b>
    /// (ae/ä, oe/ö, ue/ü, ss/ß): Der Text kommt aus einer Modellantwort und traegt mal
    /// „Rücklauf", mal „ruecklauf", mal „Rueck Lauf". Was der Anwender meint, ist in
    /// allen drei Faellen dasselbe.
    /// </para>
    /// </remarks>
    public static class KiWahl
    {
        /// <summary>Wie viele Eintraege eine Absage hoechstens aufzaehlt.</summary>
        public const int HOECHSTENS_GENANNT = 30;

        /// <summary>
        /// Ab welcher Laenge ein Kandidat auch dann trifft, wenn der GENANNTE Text ihn
        /// enthaelt („Temperatur Vorlauf" trifft <c>vorlauf</c>).
        /// </summary>
        /// <remarks>
        /// Ohne Untergrenze traefe ein zweibuchstabiger Schluessel in beinahe jedem Satz -
        /// und zwar mehrfach, was nur zu einer Mehrdeutigkeit fuehrte, die keine ist.
        /// </remarks>
        private const int MINDESTLAENGE_ENTHALTEN = 3;

        /// <summary>
        /// Der Text in seiner Vergleichsform: klein, ohne Umlaut, ohne Unterstrich und
        /// ohne Leerraum.
        /// </summary>
        public static string Falte(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var sb = new StringBuilder(text!.Length);

            foreach (char roh in text)
            {
                char c = char.ToLowerInvariant(roh);

                switch (c)
                {
                    case 'ä': sb.Append("ae"); break;
                    case 'ö': sb.Append("oe"); break;
                    case 'ü': sb.Append("ue"); break;
                    case 'ß': sb.Append("ss"); break;
                    case '_': break;
                    default:
                        if (!char.IsWhiteSpace(c)) sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sucht den gemeinten Eintrag unter <paramref name="eintraege"/>.
        /// </summary>
        /// <param name="eintraege">Die zur Wahl stehenden Paare aus Schluessel und Text.</param>
        /// <param name="genannt">Der Text aus dem Aufruf.</param>
        public static KiWahltreffer Treffer(IReadOnlyList<KiWahleintrag>? eintraege, string? genannt)
        {
            if (eintraege == null || eintraege.Count == 0) return Keiner;

            string roh = (genannt ?? "").Trim();
            if (roh.Length == 0) return Keiner;

            // ---- Stufe 1: der Schluessel, buchstabengetreu.
            for (int i = 0; i < eintraege.Count; i++)
                if (eintraege[i] != null &&
                    string.Equals(eintraege[i].Schluessel, roh, StringComparison.Ordinal))
                    return new KiWahltreffer(i, Array.Empty<string>());

            string gesucht = Falte(roh);
            if (gesucht.Length == 0) return Keiner;

            // ---- Stufe 2: der Schluessel, gefaltet.
            KiWahltreffer stufe = Stufe(eintraege, e => Falte(e.Schluessel) == gesucht);
            if (stufe != null) return stufe;

            // ---- Stufe 3: der Anzeigetext, gefaltet.
            stufe = Stufe(eintraege, e => Falte(e.Text) == gesucht);
            if (stufe != null) return stufe;

            // ---- Stufe 4: der eindeutige Anfang, in beide Richtungen.
            stufe = Stufe(eintraege, e => Anfang(Falte(e.Schluessel), gesucht) ||
                                          Anfang(Falte(e.Text), gesucht));
            if (stufe != null) return stufe;

            // ---- Stufe 5: der eindeutig enthaltene Teil.
            return Stufe(eintraege, e => Enthalten(Falte(e.Schluessel), gesucht) ||
                                         Enthalten(Falte(e.Text), gesucht))
                   ?? Keiner;
        }

        /// <summary>
        /// Zaehlt die Eintraege lesbar auf; lange Listen nennen die Zahl und die ersten
        /// <see cref="HOECHSTENS_GENANNT"/>.
        /// </summary>
        public static string Aufzaehlen(IReadOnlyList<KiWahleintrag>? eintraege)
        {
            if (eintraege == null || eintraege.Count == 0) return "";

            var teile = new List<string>();
            for (int i = 0; i < eintraege.Count && i < HOECHSTENS_GENANNT; i++)
                if (eintraege[i] != null) teile.Add(eintraege[i].Beschriftung);

            string text = string.Join(", ", teile);

            if (eintraege.Count > HOECHSTENS_GENANNT)
                text += ", ... (" +
                        eintraege.Count.ToString(CultureInfo.InvariantCulture) + ")";

            return text;
        }

        /// <summary>Die Paare aus Schluessel und Anzeigename einer Feldliste.</summary>
        /// <remarks>
        /// Damit laeuft die Feldnamensuche durch DIESELBE Regel wie die Wahl eines
        /// Listeneintrags - ein Feld ist fuer sie nichts anderes als ein beschrifteter
        /// Schluessel.
        /// </remarks>
        public static IReadOnlyList<KiWahleintrag> Paare(IReadOnlyList<KiDialogFeld>? felder)
        {
            if (felder == null) return Array.Empty<KiWahleintrag>();

            var paare = new List<KiWahleintrag>(felder.Count);
            foreach (KiDialogFeld f in felder)
                if (f != null) paare.Add(new KiWahleintrag(f.Name, f.Anzeigename));

            return paare;
        }

        // ================================================================== Hilfen

        private static readonly KiWahltreffer Keiner =
            new KiWahltreffer(-1, Array.Empty<string>());

        /// <summary>
        /// Wendet EINE Stufe an: genau ein Treffer wird geliefert, mehrere werden als
        /// Kandidaten benannt, keiner fuehrt auf die naechste Stufe (<c>null</c>).
        /// </summary>
        private static KiWahltreffer? Stufe(IReadOnlyList<KiWahleintrag> eintraege,
                                            Func<KiWahleintrag, bool> passt)
        {
            int stelle = -1;
            List<string>? mehrere = null;

            for (int i = 0; i < eintraege.Count; i++)
            {
                KiWahleintrag e = eintraege[i];
                if (e == null || !passt(e)) continue;

                if (stelle < 0) { stelle = i; continue; }

                if (mehrere == null)
                {
                    mehrere = new List<string> { eintraege[stelle].Beschriftung };
                }
                mehrere.Add(e.Beschriftung);
            }

            if (mehrere != null) return new KiWahltreffer(-1, mehrere);
            return stelle < 0 ? null : new KiWahltreffer(stelle, Array.Empty<string>());
        }

        /// <summary>Beginnt einer der beiden Texte mit dem anderen?</summary>
        private static bool Anfang(string kandidat, string gesucht)
        {
            if (kandidat.Length == 0) return false;
            return kandidat.StartsWith(gesucht, StringComparison.Ordinal) ||
                   gesucht.StartsWith(kandidat, StringComparison.Ordinal);
        }

        /// <summary>Enthaelt einer der beiden Texte den anderen?</summary>
        private static bool Enthalten(string kandidat, string gesucht)
        {
            if (kandidat.Length == 0) return false;
            if (kandidat.Contains(gesucht, StringComparison.Ordinal)) return true;

            return kandidat.Length >= MINDESTLAENGE_ENTHALTEN &&
                   gesucht.Contains(kandidat, StringComparison.Ordinal);
        }
    }
}
