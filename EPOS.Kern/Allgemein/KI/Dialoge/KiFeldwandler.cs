// VOM TEXT DES MODELLS ZUM WERT DER EIGENSCHAFT (Auftrag #201, Stufe S3).
//
// Der Setzweg der Maskenbruecke schreibt in eine Eigenschaft des Daten-Objekts - also in
// double?, int, bool, string. Was aus der Modellantwort kommt, ist TEXT (KiParameterTyp.Text
// mit hoechstens 400 Zeichen). Dazwischen fehlt genau eine Umsetzung, und die steht hier.
//
// WARUM NICHT Convert.ChangeType. Drei Gruende, alle im Bestand belegt:
//
//   1. KULTUR. Der Anwender tippt "12,5", das Modell schreibt "12.5". Beide muessen
//      ankommen. Convert.ChangeType nimmt genau eine Kultur.
//   2. LEER. Ein leerer Text ist fuer ein Feld mit leerErlaubt ein gueltiger Wert (null),
//      fuer ein Pflichtfeld ein Befund - und Convert.ChangeType wuerde in beiden Faellen
//      werfen.
//   3. WAHRHEITSWERTE auf Deutsch. "Ja"/"Nein" stehen so im Feldblock, den der Assistent
//      selbst gesendet hat (KiMaskenbruecke.AlsText); er muss sie zurueckannehmen koennen.
//
// WAS ER NICHT TUT: Er prueft keine Fachgrenzen. Ob 400 % Wirkungsgrad zulaessig sind,
// entscheidet der DIALOG (KiMaskenhaken.Pruefen) - genauso wie bei einer Eingabe von Hand.

using System;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Ergebnis einer Umsetzung: Wert oder Klartextbefund.
    /// </summary>
    public sealed class KiFeldumsetzung
    {
        private KiFeldumsetzung(object wert, string grund)
        {
            Wert = wert;
            Grund = grund;
        }

        /// <summary>Der umgesetzte Wert (kann <c>null</c> sein: ein geleertes Feld).</summary>
        public object Wert { get; }

        /// <summary>Klartextgrund; <c>null</c> = die Umsetzung ist gelungen.</summary>
        public string Grund { get; }

        /// <summary>Ist die Umsetzung gelungen?</summary>
        public bool Ok => Grund == null;

        /// <summary>Gelungen.</summary>
        public static KiFeldumsetzung Gut(object wert) => new KiFeldumsetzung(wert, null);

        /// <summary>Gescheitert.</summary>
        public static KiFeldumsetzung Schlecht(string grund) => new KiFeldumsetzung(null, grund ?? "");
    }

    /// <summary>
    /// Setzt den Text einer Modellantwort in den Wert um, den die Eigenschaft des
    /// Dialog-Daten-Objekts annimmt (Auftrag #201).
    /// </summary>
    public static class KiFeldwandler
    {
        /// <summary>
        /// Wandelt <paramref name="text"/> in den Wert fuer <paramref name="zugang"/>.
        /// </summary>
        /// <param name="zugang">Der Feldzugang - er nennt Deklaration und Zieltyp.</param>
        /// <param name="text">Der Text aus dem Aufruf.</param>
        public static KiFeldumsetzung Wandle(KiFeldzugang zugang, string text)
        {
            if (zugang == null) return KiFeldumsetzung.Schlecht("");

            KiDialogFeld feld = zugang.Feld;
            string roh = (text ?? "").Trim();
            Type ziel = Grundtyp(zugang.Werttyp);
            bool nullbar = Nullbar(zugang.Werttyp);

            // ---- Leer: erlaubt oder nicht - und was "leer" fuer den Zieltyp heisst.
            if (roh.Length == 0)
            {
                if (!feld.LeerErlaubt)
                    return KiFeldumsetzung.Schlecht(
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_FELD_LEER_VERBOTEN, feld.Anzeigename));

                if (ziel == null || ziel == typeof(string)) return KiFeldumsetzung.Gut("");
                if (nullbar) return KiFeldumsetzung.Gut(null);

                // Ein nicht nullbares Zahlenfeld kann nicht leer sein - der Dialog fuehrt
                // dort 0 und keinen Leerwert. Das ist kein Setzen, sondern eine Aussage
                // ueber den Dialog, und die gehoert benannt.
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_FELD_LEER_UNMOEGLICH, feld.Anzeigename));
            }

            // ---- Text bleibt Text.
            if (ziel == null || ziel == typeof(string)) return KiFeldumsetzung.Gut(roh);

            // ---- Wahrheitswert (auch "Ja"/"Nein" aus dem eigenen Feldblock).
            if (ziel == typeof(bool))
            {
                bool wert;
                if (!Wahrheitswert(roh, out wert))
                    return KiFeldumsetzung.Schlecht(
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_FELD_KEIN_WAHRHEITSWERT,
                                      feld.Anzeigename, roh));
                return KiFeldumsetzung.Gut(wert);
            }

            // ---- Zahl nach der EINEN Zahlregel des Hauses: Komma ODER Punkt, kein
            //      Tausendertrennzeichen. Der Anwender tippt "12,5", das Modell schreibt
            //      "12.5" - beides kommt an, und "12.500" ist keine Zahl, sondern ein
            //      Tippfehler.
            double zahl;
            if (!Zahl(roh, out zahl))
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_FELD_KEINE_ZAHL, feld.Anzeigename, roh));

            if (ziel == typeof(int) || ziel == typeof(long) || ziel == typeof(short))
            {
                double gerundet = Math.Round(zahl, MidpointRounding.AwayFromZero);
                if (Math.Abs(gerundet) > int.MaxValue)
                    return KiFeldumsetzung.Schlecht(
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_FELD_KEINE_ZAHL, feld.Anzeigename, roh));

                if (ziel == typeof(int)) return KiFeldumsetzung.Gut((int)gerundet);
                if (ziel == typeof(long)) return KiFeldumsetzung.Gut((long)gerundet);
                return KiFeldumsetzung.Gut((short)gerundet);
            }

            if (ziel == typeof(decimal)) return KiFeldumsetzung.Gut((decimal)zahl);
            if (ziel == typeof(float)) return KiFeldumsetzung.Gut((float)zahl);
            if (ziel == typeof(double)) return KiFeldumsetzung.Gut(zahl);

            // ---- Aufzaehlung: der Bezeichner, wie ihn der Feldblock zeigt.
            if (ziel.IsEnum)
            {
                try { return KiFeldumsetzung.Gut(Enum.Parse(ziel, roh, ignoreCase: true)); }
                catch (Exception)
                {
                    return KiFeldumsetzung.Schlecht(
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_DLG_AUSWAHL_UNBEKANNT,
                                      feld.Anzeigename, roh, string.Join(", ", Enum.GetNames(ziel))));
                }
            }

            // ---- Alles Uebrige: unveraendert durchreichen und dem Setzer ueberlassen.
            return KiFeldumsetzung.Gut(roh);
        }

        /// <summary>
        /// Die Zahl aus dem Text — <b>dieselbe Regel wie jedes Eingabefeld des Hauses</b>.
        /// </summary>
        /// <remarks>
        /// <see cref="ZahlText.Parsen"/> ist die EINE Zahlregel des Kerns (iU4-1): Komma
        /// ODER Punkt als Dezimaltrenner, KEIN Tausendertrennzeichen, invariant geparst.
        /// Eine eigene Regel hier waere die zweite Wahrheit — und sie waere die laschere:
        /// Mit <c>AllowThousands</c> laese eine invariante Kultur „2500,4" als
        /// zweiundzwanzigtausendfuenfhundertvier.
        /// </remarks>
        public static bool Zahl(string text, out double wert) => ZahlText.Parsen(text, out wert);

        /// <summary>
        /// Der Wahrheitswert aus dem Text: <c>true</c>/<c>false</c>, „Ja"/„Nein" in beiden
        /// Programmsprachen, dazu 1/0.
        /// </summary>
        /// <remarks>
        /// <b>Die zwei Anzeigetexte stehen in den Ressourcen</b>
        /// (<c>KI_DIALOGDATEN_JA</c>/<c>_NEIN</c>) — genau die, die
        /// <c>KiMaskenbruecke</c> in den Feldblock schreibt. Sie werden hier
        /// zurueckgelesen, damit der Assistent annehmen kann, was er selbst gesendet hat.
        /// Die englischen und die festen Formen stehen daneben, weil die Programmsprache
        /// zwischen Senden und Antworten wechseln kann.
        /// </remarks>
        public static bool Wahrheitswert(string text, out bool wert)
        {
            wert = false;
            string t = (text ?? "").Trim();
            if (t.Length == 0) return false;

            if (bool.TryParse(t, out wert)) return true;

            if (Gleich(t, "1") || Gleich(t, "ja") || Gleich(t, "yes") ||
                Gleich(t, MyResource.Resource.KI_DIALOGDATEN_JA))
            {
                wert = true;
                return true;
            }

            if (Gleich(t, "0") || Gleich(t, "nein") || Gleich(t, "no") ||
                Gleich(t, MyResource.Resource.KI_DIALOGDATEN_NEIN))
            {
                wert = false;
                return true;
            }

            return false;
        }

        private static bool Gleich(string a, string b)
            => !string.IsNullOrEmpty(b) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        /// <summary>Der Typ ohne <c>Nullable&lt;&gt;</c>-Huelle; <c>null</c> bleibt <c>null</c>.</summary>
        private static Type Grundtyp(Type typ)
        {
            if (typ == null) return null;
            return Nullable.GetUnderlyingType(typ) ?? typ;
        }

        /// <summary>Nimmt die Eigenschaft <c>null</c> an?</summary>
        private static bool Nullbar(Type typ)
        {
            if (typ == null) return true;
            if (Nullable.GetUnderlyingType(typ) != null) return true;
            return !typ.IsValueType;
        }
    }
}
