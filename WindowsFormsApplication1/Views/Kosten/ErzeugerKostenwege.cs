using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die drei Wege hinter der <c>KostenKnoepfeLeiste</c> der Anlagendialoge —
    /// „Investitionskosten…", „Betriebskosten…" und „Energiekosten…".
    ///
    /// <para><b>Warum hier und nicht siebenmal.</b> Alle Hüllen gehen denselben Weg
    /// und unterscheiden sich nur in der Kostenkomponente (<c>DbWerte.ERZEUGER_*</c>):
    /// Heizkessel, BHKW, Photovoltaik, Stromspeicher, Pufferspeicher, Solarthermie und
    /// seit dem 17.09.2026 die Wärmepumpe. Was sie gemeinsam brauchen, steht deshalb
    /// hier einmal — dasselbe Muster wie <see cref="ErzeugerTraegerHuelle"/>.</para>
    ///
    /// <para><b>Zwei Zuschnitte, ein Weg.</b> Die Dialoge mit einer Anlagenliste
    /// reichen ihre GEWÄHLTE <c>ErzeugerZeile</c> durch; die Wärmepumpe bearbeitet
    /// genau EINE Anlage und kennt deren Anlagen-, Träger- und Geräte-Id selbst. Für
    /// sie gibt es je eine Überladung, die diese Ids unmittelbar nimmt — die Ziele
    /// und der Nachlauf bleiben dieselben.</para>
    ///
    /// <para><b>Der Bezug ist die GEWÄHLTE Anlagenzeile.</b> Der Dialog reicht sie
    /// durch (sie ist <c>null</c>, solange keine gewählt ist) — genau wie die
    /// Kostenseite ihre gewählte Zeile an <c>VerwaltungGaben</c>/<c>TraegerGaben</c>
    /// durchreicht (Auftrag 268). Mit Zeile zeigt die Verwaltung den passenden
    /// Ausschnitt, ohne Zeile die Komponente ohne weitere Einengung.</para>
    ///
    /// <para><b>Die Ziele sind ZWEITE Fenster</b> (Abweichung A-1 aus Welle 6,
    /// unverändert): Kostenverwaltung und Energieträgerverwaltung sind selbst
    /// Blazor-Hüllen; ihre Verschmelzung zur <c>Ueberlagerung</c> bräuchte deren
    /// Datenseite als Delegatensatz. Hochgefahren werden sie über
    /// <see cref="Blazornachlauf"/> — ein modales Fenster darf nie SYNCHRON aus einem
    /// Blazor-Ereignis aufgehen (Befunde W13‑B‑1 und W15b‑B‑1).</para>
    /// </summary>
    internal static class ErzeugerKostenwege
    {
        /// <summary>
        /// „Investitionskosten…" / „Betriebskosten…" — die Kostenverwaltung im
        /// Projektmodus, vorgewählt auf die Anlagenzeile der gewählten Zeile.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem die Verwaltung erscheint.</param>
        /// <param name="projektId">Das geöffnete Projekt; 0 = kein Projektkontext.</param>
        /// <param name="erzeugerart">Kostenkomponente (<c>DbWerte.ERZEUGER_*</c>).</param>
        /// <param name="zeile">Die gewählte Projektzeile; <c>null</c> = keine Wahl.</param>
        /// <param name="betrieb"><c>false</c> = Investitions-, <c>true</c> = Betriebskosten.</param>
        internal static Task Kosten(IWin32Window besitzer, int projektId, string erzeugerart,
                                    ErzeugerZeile zeile, bool betrieb)
        {
            if (projektId <= 0) return Task.CompletedTask;

            return Kosten(besitzer, projektId, erzeugerart,
                          AnlageZu(projektId, erzeugerart, zeile), betrieb);
        }

        /// <summary>
        /// Derselbe Weg für einen Dialog, der SEINE Anlagenzeile schon kennt — die
        /// Wärmepumpe. Sie bearbeitet genau eine Anlage; es gibt dort keine Liste, aus
        /// der eine Zeile gewählt würde, und damit auch nichts nachzuschlagen. 0 =
        /// noch nicht gespeichert; dann wählt die Verwaltung nur die Komponente vor.
        /// </summary>
        internal static Task Kosten(IWin32Window besitzer, int projektId, string erzeugerart,
                                    int idAnlage, bool betrieb)
        {
            if (projektId <= 0) return Task.CompletedTask;

            string projektname = Projektname(projektId);

            return Blazornachlauf.Nachgelagert(() =>
                KostenKomponenteHuelle.OeffnenProjekt(besitzer, projektId, projektname,
                                                      erzeugerart, betrieb, idAnlage));
        }

        /// <summary>
        /// „Energiekosten…" — die Energieträgerverwaltung des Projekts, eingeengt auf
        /// die Träger, die zu dieser Komponente passen, und aufgeschlagen beim Träger
        /// der gewählten Zeile.
        /// </summary>
        /// <remarks>
        /// <paramref name="erzeugerart"/> geht IMMER mit: Der Dialog ist ja der eines
        /// Brenners, und ohne Gerät lässt der Kern den Kessel offen und gibt dem BHKW
        /// gasförmig und flüssig (<c>EnergietraegerZulaessigkeit</c>). Erst die
        /// gewählte Zeile nennt das Gerät und damit die Brennstoffkategorie.
        /// </remarks>
        internal static Task Energiekosten(IWin32Window besitzer, int projektId,
                                           string erzeugerart, ErzeugerZeile zeile)
        {
            if (projektId <= 0) return Task.CompletedTask;

            int traegerId = zeile != null ? zeile.CarrierId : 0;
            int geraeteId = zeile != null ? zeile.GeraetId : 0;

            return Energiekosten(besitzer, projektId, erzeugerart, traegerId, geraeteId);
        }

        /// <summary>
        /// Derselbe Weg für die Wärmepumpe, die Träger und Gerät ihrer EINEN Anlage
        /// schon kennt.
        /// </summary>
        /// <remarks>
        /// <paramref name="erzeugerart"/> geht auch hier immer mit — sie allein engt
        /// die Wärmepumpe auf STROM ein
        /// (<c>EnergietraegerZulaessigkeit.Kategoriecodes</c>: <c>NUR_STROM</c>,
        /// Aufträge 268/311). Das Gerät ist die Zeile in <c>Tab_WP</c>.
        /// </remarks>
        internal static Task Energiekosten(IWin32Window besitzer, int projektId,
                                           string erzeugerart, int traegerId, int geraeteId)
        {
            if (projektId <= 0) return Task.CompletedTask;

            return Blazornachlauf.Nachgelagert(() =>
                EnergietraegerFenster.Oeffnen(besitzer, projektId, traegerId,
                                              erzeugerart, geraeteId));
        }

        // =================================================================================
        // Datenseite
        // =================================================================================

        /// <summary>
        /// Die <c>Tab_Energieanlagen.ID</c> zur gewählten Zeile — 0, wenn es sie (noch)
        /// nicht gibt.
        ///
        /// <para><b>Warum das nachgeschlagen wird.</b> Eine Zeile, die der Anwender in
        /// diesem Dialog gerade erst aufgenommen hat, trägt als Schlüssel den Zähler ab
        /// 100000; ihre Anlagenzeile schreibt der Aufrufer erst beim OK. Die Id
        /// ungeprüft weiterzureichen hiesse, der Kostenverwaltung eine Anlage zu nennen,
        /// die es nicht gibt — sie schlüge dann gar nichts auf. Mit 0 wählt sie die
        /// Komponente vor, und das stimmt.</para>
        ///
        /// <para>Zwei Stufen wie in <c>WErzeugerCtrl.AnlagenzeileNachziehen</c>: erst
        /// über die Anlagen-Id selbst, dann über den GERÄTEANKER (die Projektkopie in
        /// <c>Tab_Heizkessel</c> bzw. <c>Tab_BHKW</c>). Gelesen wird die Liste, die
        /// schon die Kostenseite führt (Auftrag 268).</para>
        /// </summary>
        private static int AnlageZu(int projektId, string erzeugerart, ErzeugerZeile zeile)
        {
            if (zeile == null) return 0;

            try
            {
                List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen =
                    ProjektEnergietraegerCtrl.AnlagenMitTraeger(projektId);

                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
                {
                    if (!Gleich(a.Komponente, erzeugerart)) continue;
                    if (a.AnlageId == zeile.Schluessel) return a.AnlageId;
                }

                if (zeile.GeraetId > 0)
                    foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
                    {
                        if (!Gleich(a.Komponente, erzeugerart)) continue;
                        if (a.GeraeteId == zeile.GeraetId) return a.AnlageId;
                    }
            }
            catch { }

            return 0;
        }

        /// <summary>Der Projektname für die Titelzeile der Kostenverwaltung.</summary>
        private static string Projektname(int projektId)
        {
            try
            {
                var pc = new ProjektCtrl();
                pc.ReadSingle(projektId);
                return pc.rows > 0 ? (pc.m_szProjektname ?? "") : "";
            }
            catch { return ""; }
        }

        private static bool Gleich(string a, string b)
            => string.Equals(a ?? "", b ?? "", System.StringComparison.OrdinalIgnoreCase);
    }
}
