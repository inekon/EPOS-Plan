using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorprüfung einer Zone ohne Ausnahme</b> (Umsetzungskonzept Zapfprofilgenerator 2.2,
    /// N8 (b): Hinweis / Zone trägt 0 / benannter Abbruch). Beim Bearbeiten des Dialogs entstehen
    /// ständig Zwischenstände — eine neue Zone ohne Nutzungsart, eine leere Bezugsmenge, eine
    /// Wohnungszeile ohne Anzahl, ein Messwert ohne Einheit. Sie tragen 0 mit derselben benannten
    /// Ablehnung wie bisher, aber Vorschau, Ladevorschlag und Auslegung erkennen sie hier VOR der
    /// Rechnung und nehmen die Zone heraus, statt eine Ausnahme zu werfen und zu fangen.
    ///
    /// <para><b>Eine Regel je Fall.</b> Jede Prüfung ruft dieselbe Regel wie die Wurfstelle des
    /// Rechenwegs (<c>…Befund</c> in <see cref="Mengengeruest"/>, <see cref="Formvektor"/>,
    /// <see cref="Kaltwassergang"/>, <see cref="Parametersatz.FehltSatz"/>), in derselben Reihenfolge —
    /// Code und Satz der Ablehnung sind dieselben. Die Wurfstellen bleiben als Netz für Unerwartetes.</para>
    ///
    /// <para><b>Nicht hier:</b> die Kalibrierung gegen einen Messwert (<see cref="Mengengeruest.KalibrierBefund"/>)
    /// prüft der Rechenweg mit den Mengen vor der Kalibrierung, weil die Zone bis dahin zur Zirkulation
    /// beiträgt; die Zirkulation des Projekts prüft <see cref="Zirkulationskanal.Befund"/>.</para>
    /// </summary>
    internal static class Zonenvorpruefung
    {
        /// <summary>Die Vorprüfung mit Tagesgangsätzen, Parametersatz und Belegung des Eingangs.</summary>
        internal static ZapfAblehnung Pruefen(ZonenStand z, Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog)
            => Pruefen(z, katalog, e?.Tagesgangsaetze, e?.Parameter, e?.BelegungJeRaumzahl);

        /// <summary>
        /// <c>null</c>, wenn die Zone rechenbar ist (soweit es die Eingaben vor der Rechnung entscheiden),
        /// sonst ihre erste Ablehnung in der Reihenfolge des Rechenwegs: Nutzungsart, Tagesgangsatz,
        /// Temperaturen, Mengengerüst (Bezugsmenge, Wohnungstabelle, Belegung, Tagesbedarf, Spreizung),
        /// Zeitstruktur, Kaltwassergang, Jahresmesswert. Ohne Parametersatz entscheidet sie nicht (<c>null</c>).
        /// </summary>
        internal static ZapfAblehnung Pruefen(ZonenStand z, IReadOnlyList<Nutzungsart> katalog,
                                              IReadOnlyList<Tagesgangsatz> saetze, Parametersatz ps,
                                              IReadOnlyDictionary<string, double> belegungJeRaumzahl)
        {
            if (z == null)
                return new ZapfAblehnung("", ZapfEingabefehler.BezugsmengeFehlt, ZapfSatz.Neu("EINGABE_ZONE_OHNE_ANGABEN"));
            if (ps == null) return null;
            string zone = z.Name ?? "";
            ZapfAblehnung befund = NutzungsartBefund(z, katalog, out Nutzungsart n);
            if (befund != null) return befund;
            befund = TagesgangsatzBefund(z, n, saetze, out Tagesgangsatz satz);
            if (befund != null) return befund;
            befund = Mengengeruest.TemperaturenBefund(z, n, ps, out Zonentemperaturen t);
            if (befund != null) return befund;
            return Mengengeruest.JahresenergieBefund(z, n, t, ps, belegungJeRaumzahl)
                   ?? Formvektor.Befund(z, n, satz)
                   ?? Kaltwassergang.Befund(t, zone)
                   ?? Mengengeruest.MesswertBefund(z, t);
        }

        /// <summary>
        /// Die Nutzungsart der Zone aus dem Katalog — oder ihre Ablehnung mit zwei Sätzen: „noch keine
        /// gewählt" (Id ≤ 0, die neue Zone) und „nicht (mehr) im Katalog" samt Nummer. Beide unter
        /// <see cref="ZapfEingabefehler.NutzungsartFehlt"/>.
        /// </summary>
        internal static ZapfAblehnung NutzungsartBefund(ZonenStand z, IReadOnlyList<Nutzungsart> katalog, out Nutzungsart n)
        {
            n = null;
            string zone = z.Name ?? "";
            if (z.IdNutzungsart <= 0)
                return new ZapfAblehnung(zone, ZapfEingabefehler.NutzungsartFehlt,
                    ZapfSatz.Neu("EINGABE_NUTZUNGSART_NICHT_GEWAEHLT", zone));
            if (katalog != null)
                foreach (Nutzungsart k in katalog)
                    if (k != null && k.Id == z.IdNutzungsart) { n = k; return null; }
            return new ZapfAblehnung(zone, ZapfEingabefehler.NutzungsartFehlt,
                ZapfSatz.Neu("EINGABE_NUTZUNGSART_FEHLT", zone, z.IdNutzungsart));
        }

        /// <summary>
        /// Der Tagesgangsatz der Zone: der eigene, wenn sie einen wählt, sonst der der Nutzungsart — oder
        /// die Ablehnung, wenn der gewählte nicht unter <paramref name="saetze"/> steht.
        /// </summary>
        internal static ZapfAblehnung TagesgangsatzBefund(ZonenStand z, Nutzungsart n, IReadOnlyList<Tagesgangsatz> saetze,
                                                          out Tagesgangsatz satz)
        {
            satz = n.Tagesgaenge;
            if (!z.IdTagesgangsatz.HasValue) return null;
            satz = null;
            if (saetze != null)
                foreach (Tagesgangsatz s in saetze)
                    if (s != null && s.Id == z.IdTagesgangsatz.Value) { satz = s; return null; }
            string zone = z.Name ?? "";
            return new ZapfAblehnung(zone, ZapfEingabefehler.TagesgangsatzFehlt,
                ZapfSatz.Neu("EINGABE_TAGESGANGSATZ_FEHLT", z.IdTagesgangsatz.Value, zone));
        }
    }
}
