using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie dringlich ein Lizenzhinweis ist — die Kernfassung der drei Stufen aus
    /// § 6 des Lizenzierungskonzepts. Welche Farbe daraus wird, entscheidet die
    /// Oberfläche (in <c>EPOS.UI</c> die <c>WarnStufe</c> des <c>Warnbanner</c>).
    /// </summary>
    public enum LizenzDringlichkeit
    {
        /// <summary>Nichts zu melden — kein Banner.</summary>
        Keine,

        /// <summary>Ein leiser Hinweis (30 und 14 Tage vor Ablauf, Kulanz, Nachprüfung).</summary>
        Hinweis,

        /// <summary>Eine Warnung (7 Tage vor Ablauf und der Lesemodus selbst).</summary>
        Warnung
    }

    /// <summary>
    /// <b>Das Lagebild der Lizenz für das Banner</b> (Welle iF30) — sprachfertig, ohne
    /// Token, ohne Ablage, ohne Oberfläche.
    ///
    /// <para><b>Warum ein eigenes Lagebild.</b> <c>AppWurzel</c> ist die gemeinsame Wurzel
    /// beider Plattformen und darf den Lizenzkern nicht selbst rufen (Regel S-2 aus W15c:
    /// auf iOS liest <c>LizenzManager.Pruefe()</c> den Schlüsselbund SYNCHRON, und eine
    /// Komponente ruft immer vom Zeichenfaden). Sie bekommt deshalb dieses fertige Bündel
    /// als Parameter — <b>kein Token, kein Zeitanker, kein Schlüssel</b>.</para>
    ///
    /// <para><b>Die Rechnung ist rein.</b> <see cref="Bilden"/> nimmt Zustand, Token und
    /// Tagesdatum entgegen und fasst nichts an — dieselbe Trennung wie
    /// <c>LizenzManager.Bewerten</c> gegenüber <c>Pruefe</c> (Entscheid W15c-E-10). Nur
    /// <see cref="Ermitteln"/> geht an die Ablage — und mit ihm
    /// <see cref="MitTagesmerker"/>.</para>
    ///
    /// <para><b>Seit dem Anwenderentscheid iF30‑O‑2</b> (06.09.2026, „einmal täglich
    /// reicht") erscheinen die drei Warnstufen 30/14/7 einmal je Kalendertag statt bei
    /// jedem Programmstart. Ob heute schon gewarnt wurde, entscheidet
    /// <see cref="MitTagesmerker"/> über <see cref="LizenzWarnungMerker"/>; die
    /// Oberfläche fragt nur <see cref="WarnungZeigen"/>. Der LESEMODUS ist davon
    /// unberührt und bleibt bei jedem Start sichtbar.</para>
    /// </summary>
    public sealed class LizenzLage
    {
        /// <summary>
        /// Ein fertiges Lagebild. <b>Öffentlich, weil es der Transporttyp zwischen Kern
        /// und Oberfläche ist</b>: Eine Hülle darf eines bauen — und ein Prüfstand von
        /// <c>EPOS.UI.Tests</c> muss es können, ohne an den Lizenzkern zu kommen.
        /// Gerechnet wird es im Regelfall von <see cref="Bilden"/>.
        /// </summary>
        /// <param name="warnungZeigen">
        /// Ob das Banner erscheinen soll — siehe <see cref="WarnungZeigen"/>. Der
        /// Vorgabewert <c>true</c> hält jeden vorhandenen Aufrufer und jeden Prüfstand
        /// gültig, der von iF30‑O‑2 nichts weiß.
        /// </param>
        public LizenzLage(LizenzStatus status, bool lesemodus, int warnstufe,
                          int? restTage, LizenzDringlichkeit dringlichkeit,
                          string text, string detail, bool warnungZeigen = true)
        {
            Status = status;
            Lesemodus = lesemodus;
            Warnstufe = warnstufe;
            RestTage = restTage;
            Dringlichkeit = dringlichkeit;
            Text = text ?? "";
            Detail = detail ?? "";
            WarnungZeigen = warnungZeigen;
        }

        /// <summary>Der Lizenzzustand, aus dem alles Übrige folgt.</summary>
        public LizenzStatus Status { get; }

        /// <summary>
        /// Ist der Lesemodus in Kraft? Genau das Gegenteil von
        /// <c>LizenzManager.DarfSchreiben(Status)</c>.
        /// </summary>
        public bool Lesemodus { get; }

        /// <summary>Die erreichte Warnstufe vor dem Ablauf: 0, 30, 14 oder 7.</summary>
        public int Warnstufe { get; }

        /// <summary>
        /// Tage bis zum Ablauf der Lizenz; <c>null</c> bei unbefristetem oder fehlendem
        /// Token. Negativ heißt „schon abgelaufen".
        /// </summary>
        public int? RestTage { get; }

        /// <summary>Wie dringlich der Hinweis ist — die Bannerstufe.</summary>
        public LizenzDringlichkeit Dringlichkeit { get; }

        /// <summary>Der Bannertext in der Oberflächensprache; <c>""</c> = kein Banner.</summary>
        public string Text { get; }

        /// <summary>Die Statuszeile (<c>LizenzManager.StatusText</c>) als Beiwerk.</summary>
        public string Detail { get; }

        /// <summary>
        /// Soll das Banner ERSCHEINEN? <c>false</c> heißt: Die Lage besteht, der Hinweis
        /// dazu ist heute aber schon einmal gezeigt worden (Anwenderentscheid
        /// <b>iF30‑O‑2</b> vom 06.09.2026, „einmal täglich reicht").
        ///
        /// <para><b>Nur die drei Warnstufen können hier <c>false</c> stehen.</b> Der
        /// Lesemodus, das Kulanzfenster und die fällige Nachprüfung tragen immer
        /// <c>true</c> — siehe <see cref="MitTagesmerker"/>. Die Oberfläche muss also
        /// keine Fallunterscheidung führen: Sie fragt dieses eine Feld.</para>
        /// </summary>
        public bool WarnungZeigen { get; }

        /// <summary>Nichts zu melden — die Lage einer gültigen Lizenz fern vom Ablauf.</summary>
        public static readonly LizenzLage Ruhig =
            new LizenzLage(LizenzStatus.Gueltig, false, 0, null,
                           LizenzDringlichkeit.Keine, "", "");

        /// <summary>
        /// Das Lagebild dieses Arbeitsplatzes — <b>die Fassade</b>: Zustand über
        /// <c>LizenzManager.Pruefe()</c> (Ablage und Zeitanker), dann
        /// <see cref="Bilden"/>.
        /// </summary>
        /// <remarks>
        /// Sie gehört in die HÜLLE, nicht in eine Razor-Komponente: Der Aufruf liest unter
        /// iOS den Schlüsselbund und unter Windows die DPAPI-Ablage — beides synchron.
        /// </remarks>
        public static LizenzLage Ermitteln()
        {
            try
            {
                LizenzStatus status = LizenzManager.Pruefe();
                DateTime heute = DateTime.UtcNow.Date;

                // Erst die reine Rechnung, dann der Tagesmerker (iF30-O-2). Beides in
                // dieser Reihenfolge, damit Bilden rein bleibt: Der Merker SCHREIBT.
                return Bilden(status, LizenzManager.Token, heute).MitTagesmerker(heute);
            }
            catch (Exception)
            {
                // Eine unlesbare Ablage darf kein Banner erzwingen - dieselbe Linie wie
                // Schreibnaht.Lizenzantwort: im Zweifel nicht sperren, nicht warnen.
                return Ruhig;
            }
        }

        /// <summary>
        /// Dieselbe Lage, durch den <see cref="LizenzWarnungMerker"/> gefiltert
        /// (Anwenderentscheid <b>iF30‑O‑2</b> vom 06.09.2026: „einmal täglich reicht").
        ///
        /// <para><b>Die Entscheidung liegt im Kern, nicht in der Oberfläche.</b> Die
        /// <c>AppWurzel</c> fragt nur <see cref="WarnungZeigen"/>; ob heute schon gewarnt
        /// wurde, steht hier und an keiner zweiten Stelle.</para>
        ///
        /// <para><b>Der Lesemodus geht unberührt durch</b> — und mit ihm Kulanzfenster
        /// und fällige Nachprüfung, die beide Warnstufe <c>0</c> tragen. Nur die drei
        /// Stufen 30/14/7 fragen den Merker, und nur sie können danach stumm sein.</para>
        /// </summary>
        /// <param name="heute">
        /// Derselbe Tag, mit dem <see cref="Bilden"/> gerechnet hat — zwei Vorstellungen
        /// von „heute" wären eine zu viel.
        /// </param>
        /// <returns>
        /// Diese Lage selbst, wenn der Hinweis erscheinen soll; sonst eine gleiche Lage
        /// mit <see cref="WarnungZeigen"/> = <c>false</c>. <b>Der Aufruf ist nicht
        /// wirkungsfrei</b>: Ein gezeigter Hinweis wird dabei vermerkt.
        /// </returns>
        public LizenzLage MitTagesmerker(DateTime heute)
        {
            // Der Lesemodus bleibt bei JEDEM Start sichtbar (Hausregel W16b-E-6), und
            // ohne Warnstufe gibt es nichts zu unterdruecken. Beide Faelle fassen den
            // Merker gar nicht erst an - er soll keinen Vermerk fuer sie anlegen.
            if (Lesemodus || Warnstufe <= 0) return this;

            if (LizenzWarnungMerker.SollZeigen(Warnstufe, heute)) return this;

            return new LizenzLage(Status, Lesemodus, Warnstufe, RestTage,
                                  Dringlichkeit, Text, Detail, warnungZeigen: false);
        }

        /// <summary>
        /// Die reine Rechnung: aus Zustand, Token und Tagesdatum wird das Lagebild.
        /// Ohne Ablage, ohne Netz, ohne Nebenwirkung.
        /// </summary>
        /// <param name="status">Der bereits ermittelte Zustand.</param>
        /// <param name="token">Das geladene Token oder <c>null</c>.</param>
        /// <param name="heute">Der heutige Tag (UTC, ohne Uhrzeit).</param>
        public static LizenzLage Bilden(LizenzStatus status, LizenzToken token, DateTime heute)
        {
            bool lesemodus = !LizenzManager.DarfSchreiben(status);
            int? rest = LizenzManager.RestTage(token, heute);
            int stufe = LizenzManager.Warnstufe(token, heute);
            string detail = token == null && status != LizenzStatus.NichtAktiviert
                                ? ""
                                : Statuszeile(status, token);

            // 1. Der Lesemodus schlaegt alles: Er ist der Zustand, den der Anwender
            //    beheben MUSS und sonst nicht sieht - der einzige Grund fuer ein
            //    DAUERHAFTES Banner (Hausregel W16b-E-6).
            if (lesemodus)
            {
                return new LizenzLage(status, true, stufe, rest,
                                      LizenzDringlichkeit.Warnung,
                                      MyResource.Resource.LIZ_BANNER_LESEMODUS, detail);
            }

            // 2. Die drei Warnstufen vor dem Ablauf (Konzept § 6).
            if (stufe > 0 && rest.HasValue)
            {
                string datum = token != null && token.GueltigBis.HasValue
                                   ? token.GueltigBis.Value.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)
                                   : "-";
                string text;
                if (rest.Value <= 0)
                    text = string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.LIZ_BANNER_ABLAUF_HEUTE, datum);
                else if (rest.Value == 1)
                    text = string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.LIZ_BANNER_ABLAUF_EIN, datum);
                else
                    text = string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.LIZ_BANNER_ABLAUF, rest.Value, datum);

                return new LizenzLage(status, false, stufe, rest,
                                      stufe <= LizenzManager.WARNSTUFE_3
                                          ? LizenzDringlichkeit.Warnung
                                          : LizenzDringlichkeit.Hinweis,
                                      text, detail);
            }

            // 3. Kulanzfenster und faellige Nachpruefung: volle Funktion, aber ein
            //    deutlicher Hinweis (Konzept § 6, Zeile "Ablauf bis +14 Tage").
            if (status == LizenzStatus.Kulanz || status == LizenzStatus.NachpruefungFaellig)
                return new LizenzLage(status, false, stufe, rest,
                                      LizenzDringlichkeit.Hinweis, detail, detail);

            return new LizenzLage(status, false, 0, rest, LizenzDringlichkeit.Keine, "", detail);
        }

        /// <summary>
        /// <c>LizenzManager.StatusText</c> mit Netz: Ohne Token wirft der Zweig
        /// <c>Gueltig</c> dort auf <c>t.TypText()</c> — hier kann das nicht passieren,
        /// weil ohne Token nie <c>Gueltig</c> herauskommt; die Klammer steht trotzdem.
        /// </summary>
        private static string Statuszeile(LizenzStatus status, LizenzToken token)
        {
            try { return LizenzManager.StatusText(status, token); }
            catch (Exception) { return ""; }
        }
    }
}
