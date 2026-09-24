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
// Geprueft wird allein die Grenze, die das EINGABEFELD selbst fuehrt (Min/Max am
// Zahlenfeld der Maske, im Katalog als KiDialogFeld.Min/Max deklariert, Welle #458
// Stufe 3b): Was der Anwender dort nicht eintippen kann, setzt auch der Assistent nicht.
//
// DIE ZAHLENREIHE (Welle #458 Stufe 3b) hat ihren eigenen Wandler (WandleReihe): Sie
// nimmt keinen Text, sondern die gepruefte Zahlenliste aus reihe_setzen, legt sie auf
// die Stellen ab der genannten und liefert die GANZE neue Reihe im Typ der Eigenschaft.

using System;
using System.Collections.Generic;
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

            // ---- Eine ZAHLENREIHE nimmt keinen Einzeltext (Welle #458 Stufe 3b): Sie hat
            //      ihren eigenen Weg mit Laengenpruefung und Reihenblock (reihe_setzen).
            //      Die Absage nennt ihn, statt einen Text in eine Liste zu zwingen.
            if (feld.IstReihe)
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_IST_REIHE,
                                  feld.Anzeigename, feld.Reihe.Laenge));

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

            // ---- WAHL: der Text des Modells trifft einen Eintrag der Maske (KI-F1b).
            //      Gesetzt wird der SCHLUESSEL dahinter, im Typ der Zieleigenschaft.
            if (zugang.IstWahl) return AusWahl(zugang, roh, ziel, nullbar);

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

                string grenze = Bereichsgrund(feld, feld.Anzeigename, gerundet);
                if (grenze != null) return KiFeldumsetzung.Schlecht(grenze);

                if (ziel == typeof(int)) return KiFeldumsetzung.Gut((int)gerundet);
                if (ziel == typeof(long)) return KiFeldumsetzung.Gut((long)gerundet);
                return KiFeldumsetzung.Gut((short)gerundet);
            }

            if (ziel == typeof(decimal) || ziel == typeof(float) || ziel == typeof(double))
            {
                string grenze = Bereichsgrund(feld, feld.Anzeigename, zahl);
                if (grenze != null) return KiFeldumsetzung.Schlecht(grenze);
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
        /// Setzt den genannten Text in den SCHLUESSEL eines Wahleintrags um
        /// (KI-F1b, KI-D-Q6).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Getroffen wird ueber den angezeigten Text ODER den Schluessel</b>, nach der
        /// einen Namensregel des Hauses (<see cref="KiWahl.Treffer"/>): gross/klein,
        /// Umlaut, Leerzeichen gefaltet, eindeutiger Anfang genuegt. Der Anwender sagt
        /// „erdgas", und die Maske traegt „Erdgas H" unter der Id 3.
        /// </para>
        /// <para>
        /// <b>Kein Treffer und Mehrdeutigkeit sind ZWEI verschiedene Absagen.</b> Die
        /// erste nennt, was zur Wahl steht; die zweite nennt die Kandidaten und bittet
        /// um einen genaueren Namen. Ein gemeinsamer Text waere in beiden Faellen der
        /// falsche.
        /// </para>
        /// </remarks>
        private static KiFeldumsetzung AusWahl(KiFeldzugang zugang, string roh,
                                               Type ziel, bool nullbar)
        {
            KiDialogFeld feld = zugang.Feld;
            IReadOnlyList<KiWahleintrag> eintraege = zugang.Wahleintraege();

            if (eintraege.Count == 0)
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_FELD_WAHL_LEER, feld.Anzeigename));

            KiWahltreffer treffer = KiWahl.Treffer(eintraege, roh);

            if (treffer.Mehrdeutig)
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_FELD_WAHL_MEHRDEUTIG,
                                  feld.Anzeigename, roh, string.Join(", ", treffer.Kandidaten)));

            if (!treffer.Eindeutig)
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_FELD_WAHL_UNBEKANNT,
                                  feld.Anzeigename, roh, KiWahl.Aufzaehlen(eintraege)));

            string schluessel = eintraege[treffer.Stelle].Schluessel;

            // ---- Der Schluessel im Typ der Zieleigenschaft.
            if (ziel == null || ziel == typeof(string)) return KiFeldumsetzung.Gut(schluessel);

            if (schluessel.Length == 0)
                return nullbar
                           ? KiFeldumsetzung.Gut(null)
                           : KiFeldumsetzung.Schlecht(
                                 string.Format(CultureInfo.CurrentCulture,
                                               MyResource.Resource.KI_FELD_LEER_UNMOEGLICH,
                                               feld.Anzeigename));

            if (ziel == typeof(bool))
            {
                bool schalter;
                if (Wahrheitswert(schluessel, out schalter)) return KiFeldumsetzung.Gut(schalter);
                return Unbrauchbar(feld, eintraege[treffer.Stelle].Text, schluessel);
            }

            if (ziel.IsEnum)
            {
                try { return KiFeldumsetzung.Gut(Enum.Parse(ziel, schluessel, ignoreCase: true)); }
                catch (Exception)
                {
                    double roheZahl;
                    if (Zahl(schluessel, out roheZahl))
                        return KiFeldumsetzung.Gut(
                            Enum.ToObject(ziel, (int)Math.Round(roheZahl, MidpointRounding.AwayFromZero)));

                    return Unbrauchbar(feld, eintraege[treffer.Stelle].Text, schluessel);
                }
            }

            double zahl;
            if (!Zahl(schluessel, out zahl))
                return Unbrauchbar(feld, eintraege[treffer.Stelle].Text, schluessel);

            if (ziel == typeof(int) || ziel == typeof(long) || ziel == typeof(short))
            {
                double gerundet = Math.Round(zahl, MidpointRounding.AwayFromZero);
                if (Math.Abs(gerundet) > int.MaxValue)
                    return Unbrauchbar(feld, eintraege[treffer.Stelle].Text, schluessel);

                if (ziel == typeof(int)) return KiFeldumsetzung.Gut((int)gerundet);
                if (ziel == typeof(long)) return KiFeldumsetzung.Gut((long)gerundet);
                return KiFeldumsetzung.Gut((short)gerundet);
            }

            if (ziel == typeof(decimal)) return KiFeldumsetzung.Gut((decimal)zahl);
            if (ziel == typeof(float)) return KiFeldumsetzung.Gut((float)zahl);
            if (ziel == typeof(double)) return KiFeldumsetzung.Gut(zahl);

            return KiFeldumsetzung.Gut(schluessel);
        }

        /// <summary>
        /// Ein Eintrag, dessen Schluessel die Eigenschaft nicht annimmt - das ist ein
        /// Befund ueber die MASKE und wird deshalb benannt, nicht stillschweigend gesetzt.
        /// </summary>
        private static KiFeldumsetzung Unbrauchbar(KiDialogFeld feld, string text, string schluessel)
            => KiFeldumsetzung.Schlecht(
                   string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.KI_FELD_WAHL_SCHLUESSEL,
                                 feld.Anzeigename, text, schluessel));

        // =====================================================================
        // Die ZAHLENREIHE (Welle #458 Stufe 3b)
        // =====================================================================

        /// <summary>
        /// Legt <paramref name="werte"/> ab Stelle <paramref name="ab"/> auf die Reihe
        /// hinter <paramref name="zugang"/> und liefert die GANZE neue Reihe im Typ der
        /// Eigenschaft - oder den Klartextgrund, warum nicht.
        /// </summary>
        /// <param name="zugang">Der Feldzugang einer Zahlenreihe.</param>
        /// <param name="werte">Die gepruefte Zahlenliste aus dem Aufruf.</param>
        /// <param name="ab">
        /// Die Stelle des ersten Wertes, bei 1 beginnend; <c>0</c> = ohne Angabe - dann muss
        /// die Liste die ganze Reihe tragen.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>Ganz oder ab einer Stelle, nie still gestutzt.</b> Ohne Stelle muss die Zahl
        /// der Werte die Laenge der Reihe treffen - elf Monatswerte fuer zwoelf Monate sind
        /// ein Befund, kein Auftrag, den Dezember stehen zu lassen. Mit Stelle muss der
        /// Ausschnitt in die Reihe passen; was nicht genannt ist, bleibt, wie es steht.
        /// </para>
        /// <para>
        /// <b>Die Grenzen sind die des Eingabefeldes</b> (<see cref="KiDialogFeld.Min"/>,
        /// <see cref="KiDialogFeld.Max"/>), geprueft je Wert und mit dem Namen der Stelle
        /// in der Absage. Alles Weitere prueft der Dialog nach dem Setzen (Haken
        /// <c>Pruefen</c>), wie bei jeder Eingabe von Hand.
        /// </para>
        /// <para>
        /// <b>Der Zieltyp ist der der Eigenschaft</b> (<c>double?[]</c> oder <c>double[]</c>);
        /// ein unbekannter wird <c>double?[]</c>. Ein leeres Glied der bisherigen Reihe
        /// bleibt in einem <c>double[]</c> eine 0 - so zeigt es die Maske.
        /// </para>
        /// </remarks>
        public static KiFeldumsetzung WandleReihe(KiFeldzugang zugang, IReadOnlyList<double> werte, int ab)
        {
            if (zugang == null) return KiFeldumsetzung.Schlecht("");

            KiDialogFeld feld = zugang.Feld;
            if (!feld.IstReihe)
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_KEINE_REIHE,
                                  feld.Anzeigename));

            KiZahlenreihe reihe = feld.Reihe;
            int anzahl = werte == null ? 0 : werte.Count;
            if (anzahl == 0)
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_REIHE_LAENGE,
                                  feld.Anzeigename, reihe.Laenge, 0));

            // ---- Ohne Stelle: die ganze Reihe; mit Stelle: ein Ausschnitt, der passt.
            if (ab <= 0)
            {
                if (anzahl != reihe.Laenge)
                    return KiFeldumsetzung.Schlecht(
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_REIHE_LAENGE,
                                      feld.Anzeigename, reihe.Laenge, anzahl));
                ab = 1;
            }
            else if (ab > reihe.Laenge || ab - 1 + anzahl > reihe.Laenge)
            {
                return KiFeldumsetzung.Schlecht(
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_REIHE_UEBERLAUF,
                                  feld.Anzeigename, reihe.Laenge, ab,
                                  Math.Max(0, reihe.Laenge - ab + 1), anzahl));
            }

            // ---- Jeder Wert: eine endliche Zahl in den Grenzen des Eingabefeldes.
            for (int i = 0; i < anzahl; i++)
            {
                double w = werte[i];
                string stelle = feld.Anzeigename + " (" + reihe.Stellenname(ab + i) + ")";

                if (double.IsNaN(w) || double.IsInfinity(w))
                    return KiFeldumsetzung.Schlecht(
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_KEINE_ZAHL,
                                      stelle, w.ToString(CultureInfo.InvariantCulture)));

                string grenze = Bereichsgrund(feld, stelle, w);
                if (grenze != null) return KiFeldumsetzung.Schlecht(grenze);
            }

            // ---- Die neue Reihe: die bisherige, und ab der Stelle die genannten Werte.
            IReadOnlyList<double?> bisher = Reihenwerte(zugang);
            var neu = new double?[reihe.Laenge];
            for (int i = 0; i < reihe.Laenge; i++) neu[i] = i < bisher.Count ? bisher[i] : null;
            for (int i = 0; i < anzahl; i++) neu[ab - 1 + i] = werte[i];

            Type ziel = zugang.Werttyp;
            if (ziel == typeof(double[]))
            {
                var dicht = new double[neu.Length];
                for (int i = 0; i < neu.Length; i++) dicht[i] = neu[i] ?? 0.0;
                return KiFeldumsetzung.Gut(dicht);
            }

            return KiFeldumsetzung.Gut(neu);
        }

        /// <summary>
        /// Die Werte einer Reihe, wie sie JETZT in der Maske stehen; eine leere Liste, wenn
        /// der Getter nichts oder etwas anderes liefert.
        /// </summary>
        public static IReadOnlyList<double?> Reihenwerte(KiFeldzugang zugang)
        {
            if (zugang == null) return Array.Empty<double?>();

            object roh;
            try { roh = zugang.Lesen(); }
            catch (Exception) { return Array.Empty<double?>(); }

            return KiZahlenreihe.Werte(roh) ?? Array.Empty<double?>();
        }

        /// <summary>
        /// Warum <paramref name="wert"/> ausserhalb der Grenzen des Eingabefeldes liegt;
        /// <c>null</c>, wenn er passt oder das Feld keine Grenze fuehrt.
        /// </summary>
        /// <param name="feld">Die Deklaration mit <c>Min</c>/<c>Max</c>.</param>
        /// <param name="benennung">Der Name in der Absage - bei einer Reihe samt Stelle.</param>
        /// <param name="wert">Der zu setzende Wert.</param>
        internal static string Bereichsgrund(KiDialogFeld feld, string benennung, double wert)
        {
            if (feld == null || !feld.HatBereich) return null;

            bool unten = feld.Min.HasValue && wert < feld.Min.Value;
            bool oben = feld.Max.HasValue && wert > feld.Max.Value;
            if (!unten && !oben) return null;

            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_BEREICH,
                                 benennung, wert.ToString(CultureInfo.CurrentCulture), Bereichstext(feld));
        }

        /// <summary>
        /// Der zulaessige Bereich im Klartext - „0 bis 100000", „mindestens 0",
        /// „höchstens 12"; leer, wenn das Feld keinen fuehrt.
        /// </summary>
        public static string Bereichstext(KiDialogFeld feld)
        {
            if (feld == null || !feld.HatBereich) return "";

            CultureInfo k = CultureInfo.CurrentCulture;
            if (feld.Min.HasValue && feld.Max.HasValue)
                return string.Format(k, MyResource.Resource.KI_FELD_BEREICH_VON_BIS,
                                     feld.Min.Value.ToString(k), feld.Max.Value.ToString(k));
            if (feld.Min.HasValue)
                return string.Format(k, MyResource.Resource.KI_FELD_BEREICH_AB, feld.Min.Value.ToString(k));
            return string.Format(k, MyResource.Resource.KI_FELD_BEREICH_BIS, feld.Max.Value.ToString(k));
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
        /// Programmsprachen, „an"/„aus", „wahr"/„falsch", dazu 1/0.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die zwei Anzeigetexte stehen in den Ressourcen</b>
        /// (<c>KI_DIALOGDATEN_JA</c>/<c>_NEIN</c>) — genau die, die
        /// <c>KiMaskenbruecke</c> in den Feldblock schreibt. Sie werden hier
        /// zurueckgelesen, damit der Assistent annehmen kann, was er selbst gesendet hat.
        /// Die englischen und die festen Formen stehen daneben, weil die Programmsprache
        /// zwischen Senden und Antworten wechseln kann.
        /// </para>
        /// <para>
        /// <b>„an"/„aus" und „wahr"/„falsch" kommen aus der Sprache des Anwenders</b>
        /// (KI-F1b): Ein Schalter heisst auf der Maske „Heizstab", und die Bitte lautet
        /// „schalte den Heizstab an" - nicht „setze ihn auf Ja".
        /// </para>
        /// </remarks>
        public static bool Wahrheitswert(string text, out bool wert)
        {
            wert = false;
            string t = (text ?? "").Trim();
            if (t.Length == 0) return false;

            if (bool.TryParse(t, out wert)) return true;

            if (Gleich(t, "1") || Gleich(t, "ja") || Gleich(t, "yes") ||
                Gleich(t, "an") || Gleich(t, "ein") || Gleich(t, "on") ||
                Gleich(t, "wahr") || Gleich(t, "true") ||
                Gleich(t, MyResource.Resource.KI_DIALOGDATEN_JA))
            {
                wert = true;
                return true;
            }

            if (Gleich(t, "0") || Gleich(t, "nein") || Gleich(t, "no") ||
                Gleich(t, "aus") || Gleich(t, "off") ||
                Gleich(t, "falsch") || Gleich(t, "false") ||
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
