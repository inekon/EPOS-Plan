// Die MASKENBRUECKE: der Weg vom offenen Razor-Dialog zum Hilfe-Assistenten
// (Auftrag #200, Stufe S2 des Konzepts "Der Hilfe-Assistent im Dialog", 3.3 / Weg 4).
//
// WOZU SIE DA IST. Der Dialogkatalog (KiKern.KiDialogKatalog) sagt, WELCHE Felder eine
// freigegebene Maske hat; er sagt nicht, was gerade darin steht. Der Bestand loeste das
// ueber Application.OpenForms und FindControlRecursive - seit iU9 sind die vier Masken
// Razor-Komponenten, und dieser Weg ist tot. An seine Stelle tritt eine ANMELDUNG: Der
// offene Dialog traegt sich mit seinen Lese- und Schreibzugaengen ein und meldet sich beim
// Schliessen wieder ab.
//
// WARUM IM KERN UND NICHT IN DER OBERFLAECHE. Windows und iOS sollen denselben Weg fahren
// (Konzept 4, "iOS"): Unter Windows liest die Aktion dialog_lesen aus der Bruecke, auf iOS
// der Chat unmittelbar - beide ueber DIESE Klasse. Eine Fassung in EPOS.UI koennte der Kern
// nicht sehen (EPOS.UI kennt EPOS.Kern, nicht umgekehrt), und eine zweite in der
// Windows-Huelle waere wieder eine Wahrheit zuviel.
//
// WAS SIE NICHT TUT. Sie liest keine Datenbank und kein Modell - der Getter eines Feldes
// zeigt auf den ZUSTAND DES DIALOGS (Konzept 3.3: "Getter lesen den aktuellen
// Dialogzustand"). Was der Anwender gerade getippt und noch nicht gespeichert hat, steht
// darin; was in der Datenbank steht, ausdruecklich nicht.
//
// DER SETZER IST ANGELEGT UND UNBENUTZT. Feld_setzen ist Stufe S3 (Auftrag #201). Er steht
// hier trotzdem, weil er zum Zugang gehoert: Ein Dialog, der seine Felder anmeldet, meldet
// beides an oder gar nichts - sonst entstuende die Setzseite spaeter an einer zweiten
// Stelle mit einer zweiten Namensregel.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Zugang zu EINEM Feld einer offenen Maske: die Deklaration aus dem Katalog
    /// und die zwei Wege in den Dialogzustand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Die Deklaration kommt aus dem Katalog, nicht aus dem Dialog.</b> Anzeigename,
    /// Art, Einheit und Leer-Regel stehen in <see cref="KiDialogFeld"/> — dieselbe
    /// Deklaration, aus der die Feldliste fuer das Modell, die Pruefung des Setzweges
    /// und der Klartext der Bestaetigung entstehen (Fachkonzept 11.3, „eine Deklaration,
    /// drei Verwendungen"). Der Dialog steuert nur die zwei Delegaten bei.
    /// </para>
    /// <para>
    /// <b><see cref="Setzen"/> darf <c>null</c> sein</b> — dann ist das Feld NUR lesbar.
    /// Das ist der Regelfall fuer abgeleitete Groessen (die Diagnose einer Flotte, das
    /// Ergebnis der letzten Bewertung): Sie sind Auskunft, kein Eingabefeld.
    /// </para>
    /// </remarks>
    public sealed class KiFeldzugang
    {
        /// <summary>Legt einen Feldzugang an.</summary>
        /// <param name="feld">Die Deklaration aus dem Dialogkatalog.</param>
        /// <param name="lesen">Liefert den aktuellen Wert aus dem Dialogzustand.</param>
        /// <param name="setzen">Schreibt einen Wert in den Dialog; <c>null</c> = nur lesbar.</param>
        public KiFeldzugang(KiDialogFeld feld, Func<object> lesen, Action<object> setzen = null)
        {
            if (feld == null) throw new ArgumentNullException(nameof(feld));
            if (lesen == null) throw new ArgumentNullException(nameof(lesen));

            Feld = feld;
            Lesen = lesen;
            Setzen = setzen;
        }

        /// <summary>Die Deklaration aus dem Dialogkatalog.</summary>
        public KiDialogFeld Feld { get; }

        /// <summary>Liest den aktuellen Wert aus dem Dialogzustand.</summary>
        public Func<object> Lesen { get; }

        /// <summary>Schreibt einen Wert in den Dialog; <c>null</c> = das Feld ist nur lesbar.</summary>
        public Action<object> Setzen { get; }

        /// <summary>Laesst sich dieses Feld setzen? (Stufe S3)</summary>
        public bool Setzbar => Setzen != null;

        /// <summary>Der logische Feldname — der Schluessel innerhalb der Maske.</summary>
        public string Name => Feld.Name;
    }

    /// <summary>
    /// EIN gelesenes Feld: Deklaration, Rohwert und der Wert als Text.
    /// </summary>
    /// <remarks>
    /// <b>Zwei Fassungen desselben Wertes, und beide werden gebraucht.</b> Der
    /// <see cref="Rohwert"/> ist das, was im Dialog steht (<c>double?</c>, <c>bool</c>,
    /// <c>string</c>) — damit rechnet der Setzweg der Stufe S3 und ein Werkzeugergebnis,
    /// das Zahlen vergleicht. Der <see cref="Text"/> ist die Anzeige in der
    /// Anwenderkultur — das, was der Anwender auf der Maske liest und in der Vorschau
    /// wiedererkennen soll. Eine Fassung allein zwaenge jede Verwendung, die andere
    /// nachzubauen.
    /// </remarks>
    public sealed class KiFeldwert
    {
        /// <summary>Legt einen gelesenen Feldwert an.</summary>
        public KiFeldwert(KiDialogFeld feld, object rohwert, string text, bool setzbar)
        {
            if (feld == null) throw new ArgumentNullException(nameof(feld));

            Feld = feld;
            Rohwert = rohwert;
            Text = text ?? "";
            Setzbar = setzbar;
        }

        /// <summary>Die Deklaration aus dem Dialogkatalog.</summary>
        public KiDialogFeld Feld { get; }

        /// <summary>Der logische Feldname.</summary>
        public string Name => Feld.Name;

        /// <summary>Der Klartextname, wie er auf der Maske steht.</summary>
        public string Anzeigename => Feld.Anzeigename;

        /// <summary>Die Feldart aus der Deklaration.</summary>
        public KiParameterTyp Typ => Feld.Typ;

        /// <summary>Die Einheit aus der Deklaration; leer, wenn keine.</summary>
        public string Einheit => Feld.Einheit;

        /// <summary>Darf das Feld leer bleiben?</summary>
        public bool LeerErlaubt => Feld.LeerErlaubt;

        /// <summary>Der Wert, wie er im Dialog steht; <c>null</c> = leer.</summary>
        public object Rohwert { get; }

        /// <summary>Derselbe Wert als Text in der Anwenderkultur; leer = leer.</summary>
        public string Text { get; }

        /// <summary>Liesse sich dieses Feld setzen? (Stufe S3)</summary>
        public bool Setzbar { get; }

        /// <summary>Ist das Feld leer?</summary>
        public bool IstLeer => Text.Length == 0;

        /// <inheritdoc/>
        public override string ToString() => Name + " = " + Text;
    }

    /// <summary>
    /// Was eine offene Maske dem Assistenten mitgibt — der Block, der in die Anfrage
    /// geht, samt seiner Herkunft.
    /// </summary>
    /// <remarks>
    /// <b>Ein Objekt statt einer nackten Zeichenkette</b>, weil die Protokollzeile Maske
    /// und Feldzahl nennen muss (Konzept 4, „Protokoll") — aus dem Text allein liessen
    /// sie sich nur wieder herauslesen.
    /// </remarks>
    public sealed class KiDialogdaten
    {
        /// <summary>Der Katalogschluessel der Maske (<c>Form_PV</c>).</summary>
        public string Maskenname { get; set; } = "";

        /// <summary>Der Klartextname der Maske.</summary>
        public string Anzeigename { get; set; } = "";

        /// <summary>Zahl der uebertragenen Felder.</summary>
        public int Feldzahl { get; set; }

        /// <summary>Der woertliche Block, der in die Anfrage geht.</summary>
        public string Text { get; set; } = "";

        /// <summary>Gibt es ueberhaupt etwas zu senden?</summary>
        public bool Belegt => Feldzahl > 0 && Text.Length > 0;

        /// <inheritdoc/>
        public override string ToString()
            => Maskenname + " (" + Feldzahl.ToString(CultureInfo.InvariantCulture) + ")";
    }

    /// <summary>
    /// Die Bruecke zwischen offenen Masken und dem Hilfe-Assistenten (Auftrag #200,
    /// Stufe S2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ein Eintrag je Maske.</b> Der Schluessel ist der Maskenname des Katalogs; eine
    /// zweite Anmeldung derselben Maske ERSETZT die erste. Das ist Absicht: Zwei
    /// Instanzen derselben Maske waeren fuer den Assistenten nicht auseinanderzuhalten
    /// (dieselbe Ueberlegung wie im Bestand, <c>KiDialogZugriff.Aufloesen</c>) — und
    /// unter Blazor entsteht bei einem Wiederaufbau der Ueberlagerung tatsaechlich eine
    /// zweite Instanz, waehrend die erste noch nicht entsorgt ist. Die JUENGSTE gilt.
    /// </para>
    /// <para>
    /// <b>Abmelden ist zahnlos, wenn eine andere Instanz schon uebernommen hat.</b>
    /// <see cref="Abmelden(string,object)"/> nimmt deshalb die Marke der Anmeldung
    /// entgegen und raeumt nur, wenn sie noch die stehende ist. Ohne diese Probe loeschte
    /// die scheidende Instanz den Eintrag der nachfolgenden.
    /// </para>
    /// <para>
    /// <b>Thread-sicher ueber eine Sperre und unveraenderliche Schnappschuesse.</b>
    /// Angemeldet wird auf dem Renderer-Faden, gelesen im Zweifel aus einem
    /// Hintergrundlauf des Chats. <see cref="Offene"/> und <see cref="Lesen"/> geben
    /// jeweils eine frische Liste heraus — eine herausgereichte Sammlung, die sich unter
    /// dem Leser veraendert, waere die naechste seltene Ausnahme.
    /// </para>
    /// <para>
    /// <b>Der Getter darf werfen.</b> Ein Dialog kann waehrend des Lesens abgeraeumt
    /// werden; dann liefert <see cref="Lesen"/> fuer dieses Feld einen leeren Wert statt
    /// die ganze Auskunft scheitern zu lassen. Eine Assistentenauskunft ist kein
    /// Rechenweg — sie soll unvollstaendig sein duerfen, aber nie sprengen.
    /// </para>
    /// </remarks>
    public static class KiMaskenbruecke
    {
        private sealed class Eintrag
        {
            internal object Marke;
            internal KiDialog Dialog;
            internal List<KiFeldzugang> Felder;
        }

        private static readonly object _sperre = new object();

        private static readonly Dictionary<string, Eintrag> _offen =
            new Dictionary<string, Eintrag>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<string> _reihenfolge = new List<string>();

        /// <summary>
        /// Die Senke der Protokollzeile. Die Plattformhuelle haengt sie beim Start ein
        /// (Windows: <c>KiAusfuehrer.ProtokollzeileAnhaengen</c>); ohne Senke wird nur
        /// <see cref="LetzteProtokollzeile"/> gefuehrt.
        /// </summary>
        /// <remarks>
        /// Die ZEILE baut der Kern (<see cref="KiProtokoll.Zeile(DateTime,string,Schutzstufe,string,int,KiStatus,string,TimeSpan)"/>),
        /// die Huelle haengt sie nur an ihre Datei. Sonst entstuende das Format zweimal —
        /// einmal unter Windows, einmal auf iOS.
        /// </remarks>
        public static Action<string> Protokollsenke { get; set; }

        /// <summary>Die zuletzt gebaute Protokollzeile; leer, wenn noch keine entstand.</summary>
        public static string LetzteProtokollzeile { get; private set; } = "";

        // ==================================================================
        //  An- und Abmelden
        // ==================================================================

        /// <summary>
        /// Meldet eine offene Maske samt ihren Feldzugaengen an und liefert die Marke,
        /// mit der sie sich wieder abmeldet.
        /// </summary>
        /// <param name="maskenname">Der Katalogschluessel der Maske.</param>
        /// <param name="dialog">Der Katalogeintrag (fuer Anzeigename und Feldliste).</param>
        /// <param name="felder">Die Zugaenge in Anzeigereihenfolge.</param>
        /// <returns>Die Marke dieser Anmeldung; <c>null</c>, wenn nichts angemeldet wurde.</returns>
        public static object Anmelden(string maskenname, KiDialog dialog,
                                      IReadOnlyList<KiFeldzugang> felder)
        {
            if (string.IsNullOrWhiteSpace(maskenname)) return null;
            if (felder == null || felder.Count == 0) return null;

            var eintrag = new Eintrag
            {
                Marke = new object(),
                Dialog = dialog,
                Felder = new List<KiFeldzugang>(felder.Count)
            };

            foreach (KiFeldzugang z in felder)
                if (z != null) eintrag.Felder.Add(z);

            if (eintrag.Felder.Count == 0) return null;

            string schluessel = maskenname.Trim();

            lock (_sperre)
            {
                if (!_offen.ContainsKey(schluessel)) _reihenfolge.Add(schluessel);
                _offen[schluessel] = eintrag;
            }

            return eintrag.Marke;
        }

        /// <summary>
        /// Meldet die Maske ab. <b>Idempotent</b> — zweimal abmelden ist kein Fehler,
        /// und eine unbekannte Maske ebenso wenig.
        /// </summary>
        /// <param name="maskenname">Der Katalogschluessel.</param>
        /// <param name="marke">
        /// Die Marke aus <see cref="Anmelden"/>; <c>null</c> raeumt den Eintrag
        /// bedingungslos. Mit Marke wird nur geraeumt, wenn sie noch die stehende ist —
        /// so loescht eine scheidende Instanz nicht den Eintrag ihrer Nachfolgerin.
        /// </param>
        public static void Abmelden(string maskenname, object marke = null)
        {
            if (string.IsNullOrWhiteSpace(maskenname)) return;

            string schluessel = maskenname.Trim();

            lock (_sperre)
            {
                Eintrag steht;
                if (!_offen.TryGetValue(schluessel, out steht)) return;
                if (marke != null && !ReferenceEquals(steht.Marke, marke)) return;

                _offen.Remove(schluessel);
                _reihenfolge.Remove(schluessel);
            }
        }

        /// <summary>Raeumt alle Anmeldungen (Programmstart, Pruefstand).</summary>
        public static void Leeren()
        {
            lock (_sperre)
            {
                _offen.Clear();
                _reihenfolge.Clear();
            }
        }

        // ==================================================================
        //  Fragen
        // ==================================================================

        /// <summary>Die Namen der angemeldeten Masken in Anmeldereihenfolge.</summary>
        public static IReadOnlyList<string> Offene()
        {
            lock (_sperre) { return _reihenfolge.ToArray(); }
        }

        /// <summary>Ist diese Maske angemeldet?</summary>
        public static bool IstAngemeldet(string maskenname)
        {
            if (string.IsNullOrWhiteSpace(maskenname)) return false;
            lock (_sperre) { return _offen.ContainsKey(maskenname.Trim()); }
        }

        /// <summary>
        /// Die Maske, die gemeint ist, wenn der Aufrufer keine nennt: die ZULETZT
        /// angemeldete. Leer, wenn keine offen ist.
        /// </summary>
        /// <remarks>
        /// Der Bestand verlangte an dieser Stelle GENAU EINE offene Katalogmaske und
        /// lehnte sonst ab (<c>KiDialogZugriff.MehrereOffen</c>). Unter Blazor ist
        /// „mehrere offen" aber der Regelfall: Eine Ueberlagerung liegt UEBER ihrem
        /// Wirt, und beide sind angemeldet. Gemeint ist dann immer die obere — also die
        /// zuletzt angemeldete.
        /// </remarks>
        public static string AktiveMaske()
        {
            lock (_sperre)
            {
                return _reihenfolge.Count == 0 ? "" : _reihenfolge[_reihenfolge.Count - 1];
            }
        }

        /// <summary>Der Katalogeintrag einer angemeldeten Maske; <c>null</c> = keine.</summary>
        public static KiDialog Katalogeintrag(string maskenname)
        {
            Eintrag e = Finde(maskenname);
            return e?.Dialog;
        }

        /// <summary>
        /// Liest die Felder einer angemeldeten Maske. Leere Liste, wenn sie nicht
        /// angemeldet ist.
        /// </summary>
        /// <param name="maskenname">Der Katalogschluessel; leer = <see cref="AktiveMaske"/>.</param>
        public static IReadOnlyList<KiFeldwert> Lesen(string maskenname = null)
        {
            Eintrag eintrag = Finde(maskenname);
            if (eintrag == null) return Array.Empty<KiFeldwert>();

            var werte = new List<KiFeldwert>(eintrag.Felder.Count);

            foreach (KiFeldzugang zugang in eintrag.Felder)
            {
                object roh;

                // Ein Dialog kann waehrend des Lesens abgeraeumt werden. Dann ist DIESES
                // Feld leer - und nicht die ganze Auskunft hinfaellig.
                try { roh = zugang.Lesen(); }
                catch (Exception) { roh = null; }

                werte.Add(new KiFeldwert(zugang.Feld, roh, AlsText(roh), zugang.Setzbar));
            }

            return werte;
        }

        /// <summary>
        /// Der Zugang zu EINEM Feld — der Einstieg des Setzweges der Stufe S3
        /// (Auftrag #201). <c>null</c>, wenn Maske oder Feld nicht angemeldet sind.
        /// </summary>
        public static KiFeldzugang Feldzugang(string maskenname, string feldname)
        {
            if (string.IsNullOrWhiteSpace(feldname)) return null;

            Eintrag eintrag = Finde(maskenname);
            if (eintrag == null) return null;

            foreach (KiFeldzugang z in eintrag.Felder)
                if (string.Equals(z.Name, feldname, StringComparison.Ordinal)) return z;

            return null;
        }

        // ==================================================================
        //  Der Block, der in die Anfrage geht
        // ==================================================================

        /// <summary>
        /// Baut den woertlichen Feldblock der angemeldeten Maske — genau den Text, den
        /// die Vorschau zeigt und den die Anfrage mitnimmt.
        /// </summary>
        /// <param name="maskenname">Der Katalogschluessel; leer = <see cref="AktiveMaske"/>.</param>
        /// <returns><c>null</c>, wenn keine Maske angemeldet ist.</returns>
        /// <remarks>
        /// <b>Vorschau und Wirklichkeit sind DERSELBE Text.</b> Eine zweite Fassung nur
        /// fuer die Anzeige waere genau die Stelle, an der die Zusage „Sie sehen vorher,
        /// was gesendet wird" unbemerkt auseinanderliefe (dieselbe Regel wie bei
        /// <c>KiChatService.SendeVorschau</c>).
        /// </remarks>
        public static KiDialogdaten Dialogdaten(string maskenname = null)
        {
            Eintrag eintrag = Finde(maskenname);
            if (eintrag == null) return null;

            string schluessel = Schluessel(maskenname);
            IReadOnlyList<KiFeldwert> werte = Lesen(schluessel);
            string anzeige = eintrag.Dialog != null && !string.IsNullOrEmpty(eintrag.Dialog.Anzeigename)
                ? eintrag.Dialog.Anzeigename
                : schluessel;

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture,
                                        MyResource.Resource.KI_DIALOGDATEN_BLOCK_KOPF,
                                        anzeige, werte.Count));

            foreach (KiFeldwert w in werte)
            {
                sb.Append("- ").Append(w.Anzeigename);
                if (w.Einheit.Length > 0) sb.Append(" [").Append(w.Einheit).Append(']');
                sb.Append(": ")
                  .AppendLine(w.IstLeer ? MyResource.Resource.KI_DIALOGDATEN_LEER : w.Text);
            }

            return new KiDialogdaten
            {
                Maskenname = schluessel,
                Anzeigename = anzeige,
                Feldzahl = werte.Count,
                Text = sb.ToString()
            };
        }

        /// <summary>
        /// Vermerkt eine tatsaechliche Uebertragung im Aktionsprotokoll — Maske und
        /// Feldzahl (Konzept 4, „Protokoll").
        /// </summary>
        /// <remarks>
        /// Die Zeile hat das Format jeder anderen Protokollzeile
        /// (<see cref="KiProtokoll"/>, Fachkonzept 3.6); ein Freitext daneben zerstoerte
        /// das Format, das der Leser erwartet. Die Aktion heisst <c>dialog_daten</c>, die
        /// Stufe ist <see cref="Schutzstufe.Lesen"/> — uebertragen wird, nichts
        /// veraendert.
        /// </remarks>
        public static void Vermerken(KiDialogdaten daten)
        {
            if (daten == null || !daten.Belegt) return;

            string parameter = "{\"maske\":\"" + daten.Maskenname + "\",\"felder\":" +
                               daten.Feldzahl.ToString(CultureInfo.InvariantCulture) + "}";

            string zeile = KiProtokoll.Zeile(
                DateTime.Now, "dialog_daten", Schutzstufe.Lesen, parameter, 0,
                KiStatus.Ausgefuehrt,
                string.Format(CultureInfo.CurrentCulture,
                              MyResource.Resource.KI_DIALOGDATEN_PROTOKOLL,
                              daten.Anzeigename, daten.Feldzahl),
                TimeSpan.Zero);

            LetzteProtokollzeile = zeile;

            Action<string> senke = Protokollsenke;
            if (senke == null) return;

            try { senke(zeile); }
            catch (Exception) { /* Ein Protokollfehler darf keine Anfrage kippen. */ }
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static Eintrag Finde(string maskenname)
        {
            string schluessel = Schluessel(maskenname);
            if (schluessel.Length == 0) return null;

            lock (_sperre)
            {
                Eintrag e;
                return _offen.TryGetValue(schluessel, out e) ? e : null;
            }
        }

        private static string Schluessel(string maskenname)
            => string.IsNullOrWhiteSpace(maskenname) ? AktiveMaske() : maskenname.Trim();

        /// <summary>
        /// Der Wert als Anzeigetext in der Anwenderkultur — dieselbe Schreibweise, die
        /// der Anwender auf der Maske liest.
        /// </summary>
        /// <remarks>
        /// <b>Ein Wahrheitswert wird ausgeschrieben</b> („Ja"/„Nein"), nicht als
        /// <c>True</c> uebergeben: Der Block geht in eine Anfrage, die auf Deutsch
        /// beantwortet wird, und ein englisches Schlagwort mitten im Feldblock waere
        /// dort ein Fremdkoerper.
        /// </remarks>
        private static string AlsText(object wert)
        {
            if (wert == null) return "";

            if (wert is bool b)
                return b ? MyResource.Resource.KI_DIALOGDATEN_JA
                         : MyResource.Resource.KI_DIALOGDATEN_NEIN;

            if (wert is IFormattable f)
                return f.ToString(null, CultureInfo.CurrentCulture);

            return wert.ToString() ?? "";
        }
    }
}
