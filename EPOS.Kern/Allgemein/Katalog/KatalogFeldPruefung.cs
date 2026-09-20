using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wertpruefung des Katalog-Aufklappers</b> (Anwenderentscheid 15.09.2026) —
    /// eine Handvoll Regeln, die jeder der vier Schreibwege
    /// (<c>…StammCtrl.AnzeigefelderSchreiben</c>) vor dem <c>UPDATE</c> zieht.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum an EINEM Ort.</b> Die vier Erzeugerfamilien pruefen dasselbe:
    /// „keine negative Leistung", „kein Prozentwert ueber 100", „kein Energietraeger,
    /// den der Katalog nicht kennt". Stuenden die Regeln vier Mal, liefen sie
    /// auseinander — und mit ihnen der Wortlaut der Ablehnung.</para>
    /// <para><b>Die Meldung nennt das Feld so, wie es auf dem Schirm steht.</b> Der
    /// Feldname kommt aus <see cref="KatalogBrowserProfil"/>, also aus derselben Liste,
    /// aus der die Oberflaeche ihre Beschriftung nimmt (ohne den Doppelpunkt, Regel
    /// <see cref="BrowserDetailfeld.Feldname"/>). Ein Verstoss wird BENANNT abgelehnt —
    /// nie still verworfen.</para>
    /// </remarks>
    internal static class KatalogFeldPruefung
    {
        /// <summary>
        /// Der Feldname, den eine Ablehnung nennt: die Profilbeschriftung ohne „:".
        /// Faellt auf den Feldschluessel zurueck, wenn das Profil das Feld nicht fuehrt.
        /// </summary>
        public static string Feldname(KatalogBrowserArt art, string schluessel)
        {
            try
            {
                KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(art, Uebersetzt);
                foreach (BrowserDetailfeld feld in profil.Detailfelder)
                    if (string.Equals(feld.Schluessel, schluessel, StringComparison.Ordinal))
                        return feld.Feldname;
            }
            catch { }

            return schluessel ?? "";
        }

        /// <summary>
        /// <c>null</c>, wenn der Wert nicht negativ ist; sonst der Ablehnungsgrund im
        /// Klartext.
        /// </summary>
        public static string NichtNegativ(KatalogBrowserArt art, string schluessel, double wert)
        {
            if (wert >= 0) return null;

            return string.Format(
                Text("KBROW_MSG_WERT_NEGATIV", "„{0}“ darf nicht negativ sein."),
                Feldname(art, schluessel));
        }

        /// <summary>
        /// <c>null</c>, wenn der Wert im geschlossenen Bereich liegt; sonst der Grund.
        /// </summary>
        public static string ImBereich(KatalogBrowserArt art, string schluessel,
                                       double wert, double von, double bis)
        {
            if (wert >= von && wert <= bis) return null;

            return string.Format(
                Text("KBROW_MSG_WERT_BEREICH", "„{0}“ muss zwischen {1} und {2} liegen."),
                Feldname(art, schluessel),
                von.ToString(CultureInfo.CurrentCulture),
                bis.ToString(CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// <c>null</c>, wenn der Wirkungsgrad ein FAKTOR ist — größer 0 und höchstens
        /// <see cref="BhkwWirkungsgradFaktor.BAND_BIS"/>; sonst der Ablehnungsgrund.
        /// </summary>
        /// <remarks>
        /// <para><b>Wozu eine eigene Regel.</b> „Nicht negativ" ließ 29,5 durch — den
        /// ELEKTRISCHEN Wirkungsgrad in Prozent, wo der Gesamtwirkungsgrad als Faktor
        /// hingehört. Der Rechenweg teilt durch diese Zahl
        /// (<c>Verbrauch = (Wärme + Strom) / Wirkungsgrad</c>); ein Prozentwert macht
        /// den Brennstoff um rund Faktor 32 zu klein. Schemaschritt 98 hat den Bestand
        /// umgerechnet — diese Prüfung hält die Pflege davon frei.</para>
        /// <para><b>Die Obergrenze ist die des Bands</b>, aus derselben Quelle wie die
        /// Umrechnung: 1,05 lässt das Brennwertgerät zu, dessen Gesamtwirkungsgrad auf
        /// den Heizwert bezogen über 1 liegt.</para>
        /// <para><b><c>null</c> heißt „unverändert lassen"</b> — dieselbe Lesart wie im
        /// ganzen Aufklapper; ein nicht mitgeschickter Wert wird nicht geprüft.</para>
        /// </remarks>
        public static string WirkungsgradFaktor(KatalogBrowserArt art, string schluessel,
                                                double? wert)
        {
            if (!wert.HasValue) return null;
            if (wert.Value > 0 && wert.Value <= BhkwWirkungsgradFaktor.BAND_BIS) return null;

            return string.Format(
                Text("KBROW_MSG_WIRKUNGSGRAD_FAKTOR",
                     "„{0}“ ist ein Faktor zwischen 0 und {1} (z. B. 0,90), kein Prozentwert."),
                Feldname(art, schluessel),
                BhkwWirkungsgradFaktor.BAND_BIS.ToString(CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Prueft einen Wert gegen eine feste Liste erlaubter Texte — der Ersatz fuer die
        /// Klappliste, die es im Aufklapper nicht gibt (dort kennt der Baustein nur
        /// Text, Zahl und Schalter).
        /// </summary>
        /// <param name="wert">Was im Textfeld steht.</param>
        /// <param name="erlaubt">Die zulaessigen Werte in Anzeigereihenfolge.</param>
        /// <param name="treffer">
        /// Der Wert in der SCHREIBWEISE DER LISTE — nicht der des Anwenders. Bei leerer
        /// Eingabe <c>null</c>: „leer" heisst „unveraendert lassen" und ist der Grund,
        /// warum ein Satz mit einem Wert ausserhalb der Liste (Altbestand) trotzdem in
        /// seinen uebrigen Feldern pflegbar bleibt.
        /// </param>
        /// <returns><c>null</c>, wenn der Wert zulaessig oder leer ist; sonst der Grund.</returns>
        public static string AusListe(KatalogBrowserArt art, string schluessel, string wert,
                                      IReadOnlyList<string> erlaubt, out string treffer)
        {
            treffer = null;
            if (string.IsNullOrWhiteSpace(wert)) return null;

            string gesucht = wert.Trim();
            if (erlaubt != null)
            {
                foreach (string e in erlaubt)
                {
                    if (string.Equals(e, gesucht, StringComparison.CurrentCultureIgnoreCase))
                    {
                        treffer = e;
                        return null;
                    }
                }
            }

            return string.Format(
                Text("KBROW_MSG_WERT_UNBEKANNT",
                     "„{0}“: „{1}“ ist kein zulässiger Wert. Zulässig sind: {2}"),
                Feldname(art, schluessel), gesucht,
                erlaubt == null ? "" : string.Join(", ", erlaubt.Where(e => !string.IsNullOrEmpty(e))));
        }

        /// <summary>
        /// Der erste Grund aus einer Reihe von Pruefungen — <c>null</c>, wenn alle
        /// zufrieden sind. So bleibt der Aufrufer bei EINER Zeile je Feld.
        /// </summary>
        public static string ErsterGrund(params string[] gruende)
        {
            if (gruende == null) return null;
            foreach (string g in gruende)
                if (!string.IsNullOrEmpty(g)) return g;
            return null;
        }

        private static string Uebersetzt(string schluessel) => Text(schluessel, schluessel);

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
