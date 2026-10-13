using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ABGLEICH EINER ANLAGE MIT DEM ZENTRALEN KOSTENKATALOG - Welle KB-3 (Anwenderentscheid
    // 13.10.2026: "die Kosten einer Anlage sollen abgeglichen werden koennen mit dem zentralen
    // Kostenkatalog, der unter Berichte & Kosten > Kosten gefuehrt wird").
    //
    // WAS. Er haelt die Kostenpositionen EINER Anlage (Tab_ProjektWerte, Kategorie 1 und 2) gegen
    // die Satzvorlagen des Katalogsatzes, aus dem ihre Projektkopie stammt (Verweise
    // ID_KostenVorlage und ID_KostenVorlageInvestition der Schritte 208/209, gesaet mit Schritt
    // KatalogkostenSaatSchema.SCHRITT). Je Vorlagenposition eines von drei Ergebnissen:
    //   FEHLT      - die Anlage hat die Position nicht,
    //   WEICHT_AB  - sie hat sie mit einem anderen Satz (beide Zahlen stehen in der Zeile),
    //   GLEICH     - Projekt- und Katalogwert sind gleich.
    //
    // NIE VON SELBST. Vorschau() liest nur. Geschrieben wird allein in Ausfuehren(), und nur auf
    // ausdrueckliche Ausloesung: fehlende Positionen entstehen ueber den bestehenden Weg
    // (KostenVorlagenUebernahmeCtrl.AusVorlage - vorhandene Zeilen bleiben unberuehrt), abweichende
    // bekommen den Katalogwert in ihrem Satzfeld. Deshalb bleibt der Kapitalwert jedes Projekts
    // unveraendert, solange niemand abgleicht (Wirtschaftlichkeitsanker 1024, 1030, 1042).
    //
    // DAS SATZFELD JE BEMESSUNG folgt KostenVorlagenUebernahmeCtrl: eine absolute Bemessung
    // (Betrag, Jahresbetrag) traegt den Satz in EingegebenerWert, eine satzbasierte in Einheitpreis.
    // Verglichen wird genau das Feld, das die Bemessung meint - Menge und Bezugsgroesse bleiben, wie
    // der Anwender sie gepflegt hat.
    //
    // DIE PROJEKTKOPIE BLEIBT UNBERUEHRT. Ihre Kostenspalten liest allein der Planwert
    // (TechnikPlanwertCtrl); sie entfallen mit Welle KB-4. Dieser Weg schreibt sie nicht.
    //
    // BENANNTE ABSAGEN statt stiller Leere (Grund in Abgleichabsage): kein Gewerk mit Ursprung,
    // keine Zuordnung zum Katalog, kein Katalogsatz mit Satzvorlage, kein Positionslexikon.
    //
    // DER STROMSPEICHER gleicht wie jedes andere Gewerk ab (Anwenderentscheid 13.10.2026); seine
    // eigene Speicherrechnung liest weiter die Geraetespalten - ihre Umstellung ist Welle KB-5.
    // ====================================================================================

    /// <summary>Warum ein Abgleich nicht moeglich ist.</summary>
    public enum Abgleichabsage
    {
        /// <summary>Kein Grund — der Abgleich ist möglich.</summary>
        Keine = 0,

        /// <summary>Das Gewerk der Anlage führt keinen Ursprung und damit keine Satzvorlage.</summary>
        KeinGewerk = 1,

        /// <summary>Die Projektkopie kennt ihren Katalogsatz nicht (<c>ID_Stamm</c> leer).</summary>
        KeineZuordnung = 2,

        /// <summary>Der Katalogsatz führt keine Satzvorlage (keine Kosten gepflegt).</summary>
        KeineSatzvorlage = 3,
    }

    /// <summary>Das Ergebnis einer Position im Abgleich.</summary>
    public enum Abgleichart
    {
        /// <summary>Die Anlage hat die Position nicht.</summary>
        Fehlt = 0,

        /// <summary>Die Anlage hat sie mit einem anderen Satz.</summary>
        WeichtAb = 1,

        /// <summary>Projekt- und Katalogwert sind gleich.</summary>
        Gleich = 2,
    }

    /// <summary>Eine Zeile des Abgleichs — eine Position der Satzvorlage.</summary>
    public sealed class Abgleichzeile
    {
        /// <summary>1 = Investition, 2 = Betrieb.</summary>
        public int Kategorie;

        /// <summary>Die Bezeichnung der Position (Katalogtext).</summary>
        public string Bezeichnung = "";

        /// <summary>Die Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>).</summary>
        public string Bemessung = "";

        /// <summary>Der Satz der Vorlagenposition; <c>null</c> = nicht gepflegt.</summary>
        public double? Katalogwert;

        /// <summary>Der Satz der Projektposition; <c>null</c> = die Position fehlt oder ist nicht gepflegt.</summary>
        public double? Projektwert;

        /// <summary>Das Ergebnis dieser Zeile.</summary>
        public Abgleichart Art;

        /// <summary><c>Tab_ProjektWerte.ID</c> der Projektposition; 0 = sie fehlt.</summary>
        public int PositionsId;

        /// <summary><c>Tab_KostenVorlage.ID</c> der Satzvorlage, aus der die Zeile stammt.</summary>
        public int VorlageId;

        /// <summary>Der Name der Satzvorlage.</summary>
        public string Vorlagenname = "";
    }

    /// <summary>Der Befund eines Abgleichs — Absage oder Zeilen.</summary>
    public sealed class Abgleichbefund
    {
        /// <summary>Warum nichts abzugleichen ist; <see cref="Abgleichabsage.Keine"/> = es geht.</summary>
        public Abgleichabsage Absage;

        /// <summary>Die Zeilen, Investition vor Betrieb, je Kategorie nach Sortierung.</summary>
        public IReadOnlyList<Abgleichzeile> Zeilen = new List<Abgleichzeile>();

        /// <summary>Zahl der fehlenden Positionen.</summary>
        public int Fehlt;

        /// <summary>Zahl der abweichenden Positionen.</summary>
        public int WeichtAb;

        /// <summary>Zahl der gleichen Positionen.</summary>
        public int Gleich;

        /// <summary>Gibt es etwas zu tun?</summary>
        public bool Handlungsbedarf => Fehlt + WeichtAb > 0;

        /// <summary>Die Namen der beteiligten Satzvorlagen (höchstens zwei: Betrieb, Investition).</summary>
        public IReadOnlyList<string> Vorlagennamen = new List<string>();
    }

    /// <summary>Was ein ausgeführter Abgleich getan hat.</summary>
    public sealed class Abgleichergebnis
    {
        /// <summary>Zahl der neu angelegten Positionen.</summary>
        public int Angelegt;

        /// <summary>Zahl der auf den Katalogwert gesetzten Positionen.</summary>
        public int Gesetzt;

        /// <summary>Zahl der unveränderten (gleichen) Positionen.</summary>
        public int Unveraendert;

        /// <summary>Warum nichts getan wurde; <see cref="Abgleichabsage.Keine"/> = es lief.</summary>
        public Abgleichabsage Absage;

        /// <summary>true, sobald eine Zeile nicht geschrieben werden konnte.</summary>
        public bool Fehler;
    }

    /// <summary>
    /// <b>Abgleich einer Anlage mit dem zentralen Kostenkatalog</b> — Vorschau und Ausführen.
    /// Anlass, Regeln und Grenzen im Kopf der Datei.
    /// </summary>
    public static class Katalogkostenabgleich
    {
        /// <summary>Unterhalb dieser Spanne gelten zwei Sätze als gleich (Rundung der Anzeige).</summary>
        public const double TOLERANZ = 0.005;

        /// <summary>
        /// Was ein Abgleich der Anlage <paramref name="idAnlage"/> ändern würde — <b>reiner Lesevorgang</b>.
        /// </summary>
        public static Abgleichbefund Vorschau(int idProjekt, int idAnlage, int idType, int komponentenId)
        {
            var befund = new Abgleichbefund();
            if (idProjekt <= 0 || idAnlage <= 0 || komponentenId <= 0 || !Katalogrueckweg.Gewerkbekannt(idType))
            {
                befund.Absage = Abgleichabsage.KeinGewerk;
                return befund;
            }
            if (Katalogrueckweg.UrsprungsSatz(idAnlage, idType) <= 0)
            {
                befund.Absage = Abgleichabsage.KeineZuordnung;
                return befund;
            }
            IReadOnlyList<KostenVorlageKopf> vorlagen =
                Katalogrueckweg.SatzvorlagenDerAnlage(idAnlage, idType, komponentenId);
            if (vorlagen.Count == 0)
            {
                befund.Absage = Abgleichabsage.KeineSatzvorlage;
                return befund;
            }

            var zeilen = new List<Abgleichzeile>();
            foreach (KostenVorlageKopf v in vorlagen.OrderBy(x => x.KategorieId == DbWerte.KOSTEN_KATEGORIE_BETRIEB ? 1 : 0))
                foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(v.Id))
                    zeilen.Add(Pruefe(idProjekt, idAnlage, komponentenId, v, p));

            befund.Zeilen = zeilen;
            befund.Fehlt = zeilen.Count(z => z.Art == Abgleichart.Fehlt);
            befund.WeichtAb = zeilen.Count(z => z.Art == Abgleichart.WeichtAb);
            befund.Gleich = zeilen.Count(z => z.Art == Abgleichart.Gleich);
            befund.Vorlagennamen = vorlagen.Select(v => v.Name ?? "").ToList();
            return befund;
        }

        /// <summary>
        /// Eine Zeile des Abgleichs: die Projektposition zur Vorlagenposition suchen und beide Sätze
        /// vergleichen. Das verglichene Feld ist das, welches die Bemessung meint.
        /// </summary>
        private static Abgleichzeile Pruefe(int idProjekt, int idAnlage, int komponentenId,
                                             KostenVorlageKopf v, KostenVorlagenPosition p)
        {
            var z = new Abgleichzeile
            {
                Kategorie = v.KategorieId,
                Bezeichnung = p.Bezeichnung ?? "",
                Bemessung = string.IsNullOrEmpty(p.Bemessung) ? DbWerte.BEMESSUNG_BETRAG : p.Bemessung,
                Katalogwert = p.Satz,
                VorlageId = v.Id,
                Vorlagenname = v.Name ?? "",
            };
            int stammId = StammId(p.Bezeichnung);
            if (stammId <= 0)
            {
                z.Art = Abgleichart.Fehlt;
                return z;
            }
            int id = KostenPositionCtrl.FindePosition(idProjekt, v.KategorieId, komponentenId, stammId, idAnlage);
            if (id <= 0)
            {
                z.Art = Abgleichart.Fehlt;
                return z;
            }
            z.PositionsId = id;
            z.Projektwert = Projektsatz(id, z.Bemessung);
            z.Art = Gleich(z.Projektwert, z.Katalogwert) ? Abgleichart.Gleich : Abgleichart.WeichtAb;
            return z;
        }

        /// <summary>
        /// Legt die fehlenden Positionen an und setzt die abweichenden auf den Katalogwert —
        /// <b>nur auf ausdrückliche Auslösung</b>. Gleiche Zeilen bleiben unberührt, ebenso jede
        /// Position, die die Satzvorlage nicht führt (sie ist keine Dublette, sondern eine eigene
        /// Erfassung des Anwenders).
        /// </summary>
        public static Abgleichergebnis Ausfuehren(int idProjekt, int idAnlage, int idType, int komponentenId)
        {
            var e = new Abgleichergebnis();
            Abgleichbefund befund = Vorschau(idProjekt, idAnlage, idType, komponentenId);
            if (befund.Absage != Abgleichabsage.Keine)
            {
                e.Absage = befund.Absage;
                return e;
            }
            e.Unveraendert = befund.Gleich;

            // 1. Fehlende Positionen: derselbe Weg wie die Übernahme einer Vorlage (NurAnlegen).
            if (befund.Fehlt > 0)
                foreach (int idVorlage in befund.Zeilen.Where(z => z.Art == Abgleichart.Fehlt)
                                                       .Select(z => z.VorlageId).Distinct())
                {
                    KostenVorlageKopf kopf = Kopf(befund, idVorlage, komponentenId);
                    if (kopf == null) { e.Fehler = true; continue; }
                    UebernahmeErgebnis u = KostenVorlagenUebernahmeCtrl.AusVorlage(idProjekt, kopf, idAnlage);
                    e.Angelegt += u.Angelegt;
                    if (u.Fehler) e.Fehler = true;
                }

            // 2. Abweichende Sätze auf den Katalogwert setzen — Feld nach Bemessung, Menge bleibt.
            foreach (Abgleichzeile z in befund.Zeilen.Where(x => x.Art == Abgleichart.WeichtAb))
            {
                if (SatzSetzen(z)) e.Gesetzt++;
                else e.Fehler = true;
            }
            return e;
        }

        private static KostenVorlageKopf Kopf(Abgleichbefund befund, int idVorlage, int komponentenId)
        {
            Abgleichzeile z = befund.Zeilen.FirstOrDefault(x => x.VorlageId == idVorlage);
            if (z == null) return null;
            return new KostenVorlageKopf
            {
                Id = idVorlage,
                Name = z.Vorlagenname,
                KomponentenId = komponentenId,
                KategorieId = z.Kategorie,
                IstStandard = false,
            };
        }

        /// <summary>
        /// Setzt den Satz einer vorhandenen Position auf den Katalogwert. Eine absolute Bemessung
        /// schreibt ihn als Betrag, eine satzbasierte in den Einheitpreis; die übrigen Zusatzangaben
        /// bleiben, wie sie sind.
        /// </summary>
        private static bool SatzSetzen(Abgleichzeile z)
        {
            if (z.PositionsId <= 0) return false;
            KostenPositionCtrl.Zusatz zusatz = KostenPositionCtrl.LiesZusatzNachId(z.PositionsId);
            double wert = z.Katalogwert ?? 0;
            if (Absolut(z.Bemessung))
                return KostenPositionCtrl.SetzeBetragMitZusatz(z.PositionsId, wert, zusatz);
            zusatz.Einheitpreis = z.Katalogwert;
            return KostenPositionCtrl.SetzeBetragMitZusatz(z.PositionsId,
                       KostenPositionCtrl.LiesBetrag(z.PositionsId), zusatz);
        }

        /// <summary>Trägt diese Bemessung ihren Wert als Betrag (nicht als Satz)?</summary>
        internal static bool Absolut(string bemessung)
        {
            BemessungKatalog.Info info = BemessungKatalog.Finde(
                string.IsNullOrEmpty(bemessung) ? DbWerte.BEMESSUNG_BETRAG : bemessung);
            return info == null || info.Absolut;
        }

        /// <summary>Der Satz, den die Projektposition in dem Feld trägt, das ihre Bemessung meint.</summary>
        private static double? Projektsatz(int positionsId, string bemessung)
        {
            if (Absolut(bemessung)) return KostenPositionCtrl.LiesBetrag(positionsId);
            return KostenPositionCtrl.LiesZusatzNachId(positionsId).Einheitpreis;
        }

        /// <summary>Zwei Sätze gelten als gleich, wenn beide fehlen oder sich um weniger als
        /// <see cref="TOLERANZ"/> unterscheiden. Ein gepflegter Wert ist nie gleich einem fehlenden.</summary>
        internal static bool Gleich(double? projekt, double? katalog)
        {
            if (!projekt.HasValue && !katalog.HasValue) return true;
            if (!projekt.HasValue || !katalog.HasValue) return false;
            return Math.Abs(projekt.Value - katalog.Value) < TOLERANZ;
        }

        /// <summary>
        /// Die <c>StammID</c> des Positionslexikons zu einer Bezeichnung — <b>nur gelesen</b>, damit
        /// die Vorschau nichts schreibt. 0 = die Bezeichnung steht nicht im Lexikon; die Position
        /// zählt dann als fehlend, und das Ausführen legt sie über
        /// <c>KostenVorlagenUebernahmeCtrl.StammIdSicher</c> samt Lexikoneintrag an.
        /// </summary>
        internal static int StammId(string bezeichnung)
        {
            if (string.IsNullOrWhiteSpace(bezeichnung)) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT MAX(StammID) FROM Tab_Kostenfaktor WHERE Bezeichnung = ?",
                new DbParam("@b", bezeichnung));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }
    }
}
