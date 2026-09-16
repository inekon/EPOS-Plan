namespace WindowsFormsApplication1
{
    /// <summary>
    /// DIE VERGLEICHSGRUPPE des Bereichs „Berichte &amp; Kosten": welches
    /// Stammprojekt die Gruppe bildet und welche Version darin MARKIERT ist.
    ///
    /// <para><b>Warum das eine eigene Klasse ist.</b> Denselben Stand führten bis
    /// zum Anwenderbefund vom 16.09.2026 zwei Hüllen der Windows-Schale
    /// nebeneinander — die Übersicht in vier Feldern, der Rahmen in zwei weiteren.
    /// Jede Seite des Bereichs fragte danach auf eigene Weise, und die Kostenseite
    /// bekam am Ende eine ANDERE Antwort als die, die eine Zeile vorher ermittelt
    /// worden war. Hier steht die Antwort genau einmal — und plattformfrei, also
    /// prüfbar (<c>EPOS.Kern.Tests/BerichtsgruppeTests</c>); die Schale hält kein
    /// zweites Testprojekt bereit, in dem sich das messen ließe. Nachbarin und
    /// Vorbild ist <see cref="Vergleichsauswahl"/> — dieselbe Gruppe, andere
    /// Frage: WELCHE Versionen stehen nebeneinander.</para>
    ///
    /// <para><b>Die zwei Regeln, die sie hütet.</b> Erstens: Nach einem Wechsel des
    /// Projektkontexts ist die Version markiert, auf die der Kontext ZEIGT — die
    /// Variante, wenn es eine ist, sonst das Stammprojekt selbst. Zweitens: Die
    /// Kostenseite zeigt die markierte Version, und ohne Markierung das
    /// Stammprojekt der Gruppe — nie „kein Projekt".</para>
    ///
    /// <para><b>Id und Name gehören zusammen.</b> Sie werden gemeinsam gesetzt, damit
    /// die Überschrift nicht ein anderes Projekt nennt, als die Zahlen darunter
    /// meinen. Die einzige Ausnahme trägt ihren Grund im Namen:
    /// <see cref="MarkierungSetzen"/>.</para>
    /// </summary>
    public sealed class Berichtsgruppe
    {
        /// <summary>„Keins" — der Anfangswert, den die Hüllen des Bereichs führen.</summary>
        public const int KEINS = -1;

        private int _idStamm = KEINS;
        private string _stammName = "";
        private int _idMarkiert = KEINS;
        private string _markiertName = "";

        /// <summary>Das Stammprojekt der Gruppe; <see cref="KEINS"/> = noch keins.</summary>
        public int IdStamm { get { return _idStamm; } }

        /// <summary>Der Name des Stammprojekts.</summary>
        public string StammName { get { return _stammName; } }

        /// <summary>Die markierte Version (Stamm oder Variante); <see cref="KEINS"/> = keine.</summary>
        public int IdMarkiert { get { return _idMarkiert; } }

        /// <summary>Der Name der markierten Version.</summary>
        public string NameMarkiert { get { return _markiertName; } }

        /// <summary>
        /// Das Stammprojekt der Gruppe steht fest — die Vorauswahl aus dem
        /// Projektkontext und der Rückfall auf den ersten Eintrag der Liste. Die
        /// Markierung bleibt, wie sie ist: Sie gehört derselben Gruppe.
        /// </summary>
        public void StammSetzen(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
        }

        /// <summary>
        /// Der Anwender hat ein ANDERES Stammprojekt gewählt. Die Markierung zeigte
        /// auf eine Version der alten Gruppe und gilt nicht mehr; sie entsteht beim
        /// nächsten Laden der Liste neu.
        /// </summary>
        public void StammGewaehlt(int idStamm, string stammName)
        {
            StammSetzen(idStamm, stammName);
            MarkierungVerwerfen();
        }

        /// <summary>Diese Version ist markiert — Id und Name in einem Zug.</summary>
        public void Markieren(int idProjekt, string name)
        {
            _idMarkiert = idProjekt;
            _markiertName = name ?? "";
        }

        /// <summary>
        /// Nur die Id — der Name zieht beim nächsten Laden der Liste nach. Für die
        /// Wege, an denen der Aufrufer den Namen nicht hat (der Klick auf eine
        /// Listenzeile, die Übernahme eines Merkmals).
        /// </summary>
        public void MarkierungSetzen(int idProjekt)
        {
            _idMarkiert = idProjekt;
        }

        /// <summary>Keine Version markiert.</summary>
        public void MarkierungVerwerfen()
        {
            _idMarkiert = KEINS;
            _markiertName = "";
        }

        /// <summary>
        /// DER PROJEKTKONTEXT HAT GEWECHSELT — die Kopfzeile des Hauptfensters oder
        /// die Variantenwahl auf der Seite „Übersicht".
        ///
        /// <para>Markiert wird die Version, auf die der Kontext zeigt, gleich ob
        /// Variante oder Stammprojekt (Anwenderbefund 16.09.2026: Der Rückwechsel
        /// von einer Variante auf ihr Stammprojekt ließ die Markierung leer, und die
        /// Seite „Kosten" stand danach mit „Kein Projekt gewählt." da).</para>
        /// </summary>
        /// <param name="idProjekt">Das nun geöffnete Projekt.</param>
        /// <param name="projektname">Sein Name — er wird zum Namen der Markierung.</param>
        /// <param name="idStammRef">
        /// Das Stammprojekt, wenn <paramref name="idProjekt"/> eine Variante ist;
        /// sonst 0 oder <see cref="KEINS"/> (<c>VariantenCtrl.StammRefDerVariante</c>).
        /// </param>
        /// <returns>
        /// Das Stammprojekt, das die Gruppe bilden soll — die Referenz der Variante,
        /// sonst das Projekt selbst; 0, wenn kein Projekt offen ist.
        /// </returns>
        public int KontextGewechselt(int idProjekt, string projektname, int idStammRef)
        {
            if (idProjekt <= 0)
            {
                MarkierungVerwerfen();
                return 0;
            }

            Markieren(idProjekt, projektname);
            return idStammRef > 0 ? idStammRef : idProjekt;
        }

        /// <summary>
        /// DIE VERSION, DIE DIE KOSTENSEITE ZEIGT: die markierte, sonst das
        /// Stammprojekt der Gruppe. 0 heißt „wirklich keins" — dann, und nur dann,
        /// steht dort <c>BK_KOSTEN_KEIN_PROJEKT</c>.
        /// </summary>
        public int KostenId
        {
            get
            {
                if (_idMarkiert > 0) return _idMarkiert;
                return _idStamm > 0 ? _idStamm : 0;
            }
        }

        /// <summary>Der Name zu <see cref="KostenId"/> — aus derselben Wahl.</summary>
        public string KostenName
        {
            get
            {
                if (_idMarkiert > 0) return _markiertName;
                return _idStamm > 0 ? _stammName : "";
            }
        }
    }
}
