using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Abgeleitetes Vollbenutzungsstunden-Kontingent einer KWK-Anlage nach § 8 KWKG
    /// 2025 samt Herleitung (Etappe K6, Konzept § 8.1). Ein Kontingent von 0 mit
    /// gefüllter Herleitung heißt „begründet kein Zuschlag", nicht „nicht gerechnet" —
    /// dieselbe Regel wie bei <see cref="KwkgSatzVorschlag"/>.
    /// </summary>
    public sealed class KwkgKontingentVorschlag
    {
        /// <summary>Abgeleitetes Kontingent [h]; 0 = keine Stufe erreicht bzw. Angaben fehlen.</summary>
        public double KontingentH;

        /// <summary>Herleitung im Klartext (Norm, Stufe, Kostenanteil).</summary>
        public string Herleitung = "";

        /// <summary>true, wenn die Mindestschwelle der Anlagenart unterschritten ist
        /// oder eine gebrauchte Angabe fehlt — dann ist <see cref="KontingentH"/> 0 und
        /// die Herleitung nennt den Grund.</summary>
        public bool Unvollstaendig;
    }

    /// <summary>
    /// Leitet das Vbh-Kontingent des § 8 KWKG 2025 aus Anlagenart und Kostenanteil ab
    /// (Etappe K6, HF6). Faktenbasis:
    /// <c>Grundlagen_KWKG_Energiesteuer_Stromsteuer.md</c>, Abschnitt 1.4.
    ///
    /// <para><b>Reine Funktion ohne Datenbankzugriff</b> (Leitentscheidung L9): Der
    /// Katalog kommt als Delegat herein — dasselbe Muster wie
    /// <see cref="KwkgSatzRechner"/>, damit Dialog, Rechenkern und Probe dieselbe
    /// Ableitung verwenden.</para>
    ///
    /// <list type="table">
    ///   <item><term>neu</term><description>30.000 Vbh (§ 8 Abs. 1) — ohne
    ///     Kostenschwelle</description></item>
    ///   <item><term>modernisiert</term><description>15.000 Vbh ab 25 %, 30.000 Vbh ab
    ///     50 % der Neuherstellungskosten (§ 8 Abs. 2)</description></item>
    ///   <item><term>nachgerüstet</term><description>10.000 / 15.000 / 30.000 Vbh ab
    ///     10 / 25 / 50 % (§ 8 Abs. 3)</description></item>
    /// </list>
    ///
    /// <para><b>Der 6.000-Vbh-Sonderfall ist bewusst NICHT implementiert.</b> § 8 Abs. 2
    /// kennt eine vierte Stufe: 6.000 Vbh ab 10 % Kostenanteil, aber ausschließlich für
    /// <b>Dampfsammelschienen-KWK über 50 MW</b> und mit einem Mindestabstand von zwei
    /// Jahren zur Inbetriebnahme. EPOS-Plan führt weder eine Anlagenbauart noch
    /// Leistungen dieser Größenordnung; die Ausschreibungsgrenze des § 8a liegt bei
    /// 500 kW und ist im Programm ohnehin die harte Obergrenze der Förderfähigkeit. Eine
    /// Stufe, die erst hundertfach darüber greift, wäre toter Code mit einer
    /// Fehlbedienungsgefahr: Ein modernisiertes 200-kW-BHKW mit 12 % Kostenanteil
    /// bekäme sonst 6.000 Vbh, obwohl ihm nach dem Gesetz gar nichts zusteht. Der
    /// Katalogschlüssel <c>DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_10</c> bleibt gepflegt
    /// (Vollständigkeit des Gesetzesabbilds), wird hier aber nicht gelesen.</para>
    ///
    /// <para><b>Der Mindestabstand zur Inbetriebnahme (§ 8 Abs. 2: 5 bzw. 10 Jahre) wird
    /// nicht geprüft.</b> Er bezieht sich auf die Inbetriebnahme der ALTanlage; die
    /// führt das Datenmodell nicht. <c>KWKG_Inbetriebnahme</c> ist das Datum der neuen
    /// bzw. modernisierten Anlage. Die Herleitung sagt das als Vorbehalt.</para>
    /// </summary>
    public static class KwkgKontingentRechner
    {
        /// <summary>
        /// Das Kontingent zu einer Anlagenart.
        /// </summary>
        /// <param name="anlagenart">Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c>;
        /// leer = keine Angabe ⇒ kein abgeleitetes Kontingent.</param>
        /// <param name="kostenanteilProzent">Anteil an den Neuherstellungskosten [%];
        /// 0 = nicht gepflegt.</param>
        /// <param name="jahr">Stichtagsjahr für die Katalogauflösung.</param>
        /// <param name="katalog">Lesefassade auf <c>Tab_Gesetzesparameter</c>.</param>
        /// <param name="kultur">Zahlenformat der Herleitung.</param>
        public static KwkgKontingentVorschlag Ableiten(string anlagenart, double kostenanteilProzent,
                                                       int jahr,
                                                       Func<string, int, GesetzParameter> katalog,
                                                       CultureInfo kultur)
        {
            var v = new KwkgKontingentVorschlag();
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            if (katalog == null || string.IsNullOrEmpty(anlagenart))
            {
                v.Unvollstaendig = true;
                v.Herleitung = MyResource.Resource.WIRT_KWKG_KONTINGENT_OHNE_ART;
                return v;
            }

            // --- § 8 Abs. 1: neue Anlagen, ohne Kostenschwelle ---------------------
            if (string.Equals(anlagenart, DbWerte.KWKG_ANLAGENART_NEU, StringComparison.Ordinal))
            {
                double neu = Wert(katalog, jahr, DbWerte.GESETZ_KWKG_VBH_NEUANLAGE);
                if (neu <= 0)
                {
                    v.Unvollstaendig = true;
                    v.Herleitung = string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_SATZ_FEHLT,
                                                 DbWerte.GESETZ_KWKG_VBH_NEUANLAGE);
                    return v;
                }
                v.KontingentH = neu;
                v.Herleitung = string.Format(kultur, MyResource.Resource.WIRT_KWKG_KONTINGENT_NEU,
                                             neu.ToString("N0", kultur), NORM_ABS1,
                                             jahr.ToString(CultureInfo.InvariantCulture));
                return v;
            }

            bool nachgeruestet = string.Equals(anlagenart, DbWerte.KWKG_ANLAGENART_NACHGERUESTET,
                                               StringComparison.Ordinal);
            string norm = nachgeruestet ? NORM_ABS3 : NORM_ABS2;

            // Die Schwellen stehen im Katalog, nicht im Code — sie sind Gesetzeswerte.
            double s50 = Wert(katalog, jahr, DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_50, 50.0);
            double s25 = Wert(katalog, jahr, DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_25, 25.0);
            double s10 = Wert(katalog, jahr, DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_10, 10.0);

            // Mindestschwelle: modernisiert 25 % (die 10-%-Stufe ist der bewusst nicht
            // implementierte Dampfsammelschienen-Sonderfall), nachgerüstet 10 %.
            double mindest = nachgeruestet ? s10 : s25;

            if (kostenanteilProzent <= 0)
            {
                v.Unvollstaendig = true;
                v.Herleitung = string.Format(kultur, MyResource.Resource.WIRT_KWKG_KONTINGENT_ANTEIL_FEHLT,
                                             norm, mindest.ToString("N0", kultur));
                return v;
            }

            if (kostenanteilProzent < mindest)
            {
                v.Unvollstaendig = true;
                v.Herleitung = string.Format(kultur, MyResource.Resource.WIRT_KWKG_KONTINGENT_ZU_KLEIN,
                                             kostenanteilProzent.ToString("N1", kultur),
                                             mindest.ToString("N0", kultur), norm);
                return v;
            }

            string schluessel;
            double schwelle;
            if (kostenanteilProzent >= s50)
            {
                schluessel = nachgeruestet ? DbWerte.GESETZ_KWKG_VBH_NACHGERUESTET_50
                                           : DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_50;
                schwelle = s50;
            }
            else if (kostenanteilProzent >= s25)
            {
                schluessel = nachgeruestet ? DbWerte.GESETZ_KWKG_VBH_NACHGERUESTET_25
                                           : DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_25;
                schwelle = s25;
            }
            else
            {
                // Nur nachgerüstet erreicht diesen Zweig — bei modernisiert ist die
                // Mindestschwelle oben schon 25 %.
                schluessel = DbWerte.GESETZ_KWKG_VBH_NACHGERUESTET_10;
                schwelle = s10;
            }

            double kontingent = Wert(katalog, jahr, schluessel);
            if (kontingent <= 0)
            {
                v.Unvollstaendig = true;
                v.Herleitung = string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_SATZ_FEHLT, schluessel);
                return v;
            }

            v.KontingentH = kontingent;
            v.Herleitung = string.Format(kultur, MyResource.Resource.WIRT_KWKG_KONTINGENT_STUFE,
                                         kostenanteilProzent.ToString("N1", kultur),
                                         schwelle.ToString("N0", kultur),
                                         kontingent.ToString("N0", kultur), norm,
                                         jahr.ToString(CultureInfo.InvariantCulture));
            return v;
        }

        // Normbezeichnungen bestehen nur aus Paragrafenzeichen und Zahlen und bleiben
        // deshalb im Code (Drei-Schichten-Regel, wie KwkgSatzRechner.NormEigen).
        private const string NORM_ABS1 = "§ 8 Abs. 1 KWKG 2025";
        private const string NORM_ABS2 = "§ 8 Abs. 2 KWKG 2025";
        private const string NORM_ABS3 = "§ 8 Abs. 3 KWKG 2025";

        /// <summary>Ein Katalogwert; fehlt er, gilt der Ersatzwert (0 = kein Ersatz,
        /// der Aufrufer meldet dann den fehlenden Schlüssel).</summary>
        private static double Wert(Func<string, int, GesetzParameter> katalog, int jahr,
                                   string schluessel, double ersatz = 0)
        {
            try
            {
                GesetzParameter p = katalog(schluessel, jahr);
                if (p != null && p.Wert.HasValue && p.Wert.Value > 0) return p.Wert.Value;
            }
            catch { }
            return ersatz;
        }
    }
}
