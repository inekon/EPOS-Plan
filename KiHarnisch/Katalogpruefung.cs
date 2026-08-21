using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using KiKern;
using WindowsFormsApplication1;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Der Laufzeit-Katalogtest der Formularsteuerung (Fachkonzept 11.3,
    /// Umsetzungskonzept Etappe 3b, Paket F4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wogegen er schuetzt.</b> Der Dialogkatalog (<c>Allgemein\KI\Dialoge\KiDialoge.cs</c>)
    /// nennt je Maske Controlpfade und Knopfnamen als Zeichenketten. Baut jemand eine Maske
    /// um oder benennt ein Feld anders, faellt das ohne diesen Test erst dem Anwender auf -
    /// naemlich dann, wenn eine Dialogaktion mit "Das Feld gibt es in dieser Maske nicht"
    /// abbricht. Fachkonzept 11.3 verlangt deshalb ausdruecklich: Ein Katalogtest
    /// instanziiert jede deklarierte Maske und weist nach, dass jeder Controlpfad aufloest
    /// und jeder deklarierte Knopf existiert - damit der Katalog nicht stumm altert.
    /// </para>
    /// <para>
    /// <b>Zwei Pruefwege, und warum es zwei sein muessen.</b>
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>Statisch</b> (jede Maske): Jedes Wegstueck eines Controlpfades muss als Feld des
    /// Maskentyps deklariert sein. Das trifft jede Maske, kostet nichts und braucht weder
    /// Datenbank noch Fenster - denn <c>InitializeComponent</c> legt genau diese Felder an.
    /// </description></item>
    /// <item><description>
    /// <b>Zur Laufzeit</b> (wo der Konstruktor ohne Datenbank auskommt): Die Maske wird
    /// gebaut - NICHT gezeigt - und jeder Pfad ueber denselben Weg aufgeloest, den auch die
    /// Dialogaktionen gehen (<c>KiDialogZugriff.Aufloesen</c>). Nur so faellt auf, wenn ein
    /// Feld zwar deklariert, aber keinem Behaelter zugeordnet ist, wenn die Art nicht passt
    /// (ein <c>Label</c>, wo der Katalog ein Eingabefeld erwartet) - oder wenn ein Control
    /// erst zur Laufzeit entsteht und dabei einen anderen Namen bekommt.
    /// </description></item>
    /// </list>
    /// <para>
    /// <b>Warum nicht alle vier Masken gebaut werden.</b> Zwei Konstruktoren gehen an die
    /// Datenbank: <c>Form_Heizkessel_Bearbeiten</c> ruft
    /// <c>HeizkesselStammCtrl.StelleSpaltenSicher()</c> und liest die Brennstoffarten
    /// (<c>Form_Heizkessel_Bearbeiten.cs:44,77</c>), <c>Form_WP</c> ruft
    /// <c>WPStammCtrl.ReadAll()</c> (<c>Form_WP.cs:36</c>). Der erste dieser Aufrufe wuerde
    /// die Tabelle sogar ERWEITERN. Dieser Test laeuft ohne Datenbank und ruehrt die
    /// produktive <c>Kenndaten.accdb</c> in keinem Fall an - fuer diese zwei Masken bleibt
    /// es deshalb bei der statischen Pruefung. Steht ihr Konstruktor eines Tages ohne
    /// Datenbank auf eigenen Fuessen, genuegt hier ein <c>Bauen</c>-Verweis.
    /// </para>
    /// <para>
    /// <b>Warum ueber Reflexion an den Katalog.</b> <c>KiDialoge</c> und
    /// <c>KiDialogZugriff</c> sind <c>internal</c>; das soll so bleiben (Maskenwissen ist
    /// Sache des Anwendungsprojekts). Ein <c>InternalsVisibleTo</c> nur fuer ein
    /// Pruefwerkzeug waere eine Aenderung am Bestand fuer die Pruefung - genau das vermeidet
    /// schon <c>DbUmgebung</c> auf demselben Weg (<c>Referenzlauf\DbUmgebung.cs:91</c>).
    /// Nachgebaut wird nichts: Die Aufloesung laeuft durch die ECHTE Methode, sonst pruefte
    /// der Test seine eigene Kopie der Regel.
    /// </para>
    /// </remarks>
    internal static class Katalogpruefung
    {
        /// <summary>Name des Aufrufknopfs (<c>KiAufrufKnopf.KNOPF_NAME</c>).</summary>
        private const string AUFRUFKNOPF = "btn_KiAufruf";

        /// <summary>
        /// Eine Maske des Katalogs samt der Frage, ob sie sich ohne Datenbank bauen laesst.
        /// </summary>
        private sealed class Maskenfall
        {
            /// <summary>Der Typ der Maske.</summary>
            public Type Typ;

            /// <summary>Baut die Maske; <c>null</c>, wenn ihr Konstruktor Daten braucht.</summary>
            public Func<Form> Bauen;

            /// <summary>Warum nicht gebaut wird - steht so im Protokoll.</summary>
            public string DbGrund = "";
        }

        /// <summary>
        /// Die vier Startmasken der Etappe 3b. Die Liste steht bewusst hier und nicht im
        /// Katalog: Ob ein Konstruktor Daten braucht, ist eine Eigenschaft des BESTANDS,
        /// keine Deklaration des Assistenten - und sie gehoert dorthin, wo sie geprueft wird.
        /// </summary>
        private static IEnumerable<Maskenfall> Faelle()
        {
            yield return new Maskenfall
            {
                Typ = typeof(Form_Heizkessel_Bearbeiten),
                Bauen = null,
                DbGrund = "Konstruktor ruft HeizkesselStammCtrl.StelleSpaltenSicher() und liest " +
                          "die Brennstoffarten (Form_Heizkessel_Bearbeiten.cs:44,77)"
            };
            yield return new Maskenfall
            {
                Typ = typeof(Form_PV),
                Bauen = () => new Form_PV()
            };
            yield return new Maskenfall
            {
                Typ = typeof(Form_PufferSp_Bearbeiten),
                Bauen = () => new Form_PufferSp_Bearbeiten(Form_PufferSp_Bearbeiten.MODE_NEU)
            };
            yield return new Maskenfall
            {
                Typ = typeof(Form_WP),
                Bauen = null,
                DbGrund = "Konstruktor ruft WPStammCtrl.ReadAll() (Form_WP.cs:36)"
            };
        }

        // =====================================================================
        // Der Lauf
        // =====================================================================

        /// <summary>
        /// Prueft den vollstaendigen Dialogkatalog. Fehler landen im Protokoll und damit im
        /// Rueckgabewert des Harnischs.
        /// </summary>
        internal static void Pruefen(Protokoll log)
        {
            log.Zeile("--- Laufzeit-Katalogtest der Formularsteuerung (Fachkonzept 11.3) ---");

            KiDialogKatalog katalog = KatalogHolen(log);
            if (katalog == null) return;

            log.Zeile("Katalogmasken: " + katalog.Anzahl);
            log.Leerzeile();

            var gesehen = new HashSet<string>(StringComparer.Ordinal);

            foreach (Maskenfall fall in Faelle())
            {
                KiDialog eintrag = katalog.Finde(fall.Typ.Name);
                if (eintrag == null)
                {
                    log.FehlerZeile("Maske " + fall.Typ.Name + " steht nicht im Katalog.");
                    continue;
                }

                gesehen.Add(eintrag.Maskenname);
                MaskePruefen(log, fall, eintrag);
                log.Leerzeile();
            }

            // Umgekehrte Richtung: Ein Katalogeintrag, den dieser Test nicht kennt, bliebe
            // ungeprueft - und genau das soll nicht unbemerkt bleiben.
            foreach (string name in katalog.Maskennamen())
                if (!gesehen.Contains(name))
                    log.FehlerZeile("Katalogeintrag " + name + " wird von diesem Test NICHT geprueft.");

            SchalterPruefen(log);
        }

        // =====================================================================
        // Befehlszeilenschalter der Feldsicherung (Fachkonzept 11.5, Paket F4)
        // =====================================================================

        /// <summary>
        /// Prueft die Erkennung von <c>/ki-feldsicherung-aus</c> - OHNE die Feldsicherung
        /// abzuschalten.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum nur die Erkennung.</b> <c>KiFeldsicherung.Abschalten</c> wirkt einmalig
        /// und prozessweit; ein Prueflauf koennte den Zustand nicht wiederherstellen und
        /// alles danach liefe unter abgeschalteter Sicherung. Geprueft wird deshalb die
        /// reine Funktion <c>Program.FeldsicherungAusVerlangt</c>, die genau diese Frage
        /// beantwortet - und die im Programm die EINZIGE Bedingung vor dem Abschalten ist.
        /// </para>
        /// <para>
        /// <b>Was die Faelle zusagen.</b> Der Schalter wird erkannt, egal wo er steht und
        /// wie er geschrieben ist; er wird NICHT aus einer Abwandlung heraus erkannt (ein
        /// weiterer Bindestrich, ein angehaengtes Wort, ein Wert dahinter), und Stelle 0 -
        /// der Programmpfad - zaehlt nicht mit. Damit ist belegt, dass es genau einen
        /// Abschaltkanal gibt und er sich nicht versehentlich treffen laesst.
        /// </para>
        /// </remarks>
        private static void SchalterPruefen(Protokoll log)
        {
            log.Leerzeile();
            log.Zeile("--- Befehlszeilenschalter /ki-feldsicherung-aus (nur Erkennung) ---");

            Pruefe(log, true, "erkannt", "app.exe", "/ki-feldsicherung-aus");
            Pruefe(log, true, "Gross-/Kleinschreibung egal", "app.exe", "/KI-Feldsicherung-AUS");
            Pruefe(log, true, "mit Leerraum", "app.exe", "  /ki-feldsicherung-aus  ");
            Pruefe(log, true, "hinter anderen Argumenten", "app.exe", "/x", "/ki-feldsicherung-aus");

            Pruefe(log, false, "leere Befehlszeile", "app.exe");
            Pruefe(log, false, "fremdes Argument", "app.exe", "/ki");
            Pruefe(log, false, "zweiter Bindestrich", "app.exe", "--ki-feldsicherung-aus");
            Pruefe(log, false, "angehaengter Text", "app.exe", "/ki-feldsicherung-aus=1");
            Pruefe(log, false, "nur der Programmpfad", "/ki-feldsicherung-aus");

            if (!KiFeldsicherung.Aktiv)
                log.FehlerZeile("Die Feldsicherung ist waehrend der Schalterpruefung abgeschaltet worden.");
            else
                log.Roh("      Die Feldsicherung ist unveraendert AKTIV.");

            TextePruefen(log);
        }

        /// <summary>
        /// Weist nach, dass Chathinweis und Protokollvermerk in BEIDEN Oberflaechensprachen
        /// aus <c>MyResource</c> kommen und nicht aus der deutschen Vorgabe des Kerns.
        /// </summary>
        /// <remarks>
        /// Der Kern kennt nur Schluessel und faellt auf seine deutsche Vorgabe zurueck, wenn
        /// der Textlieferant nichts liefert (<c>KiTextlieferant.cs:19-22</c>). Ein fehlender
        /// Ressourceneintrag faellt deshalb NICHT als Fehler auf, sondern nur daran, dass
        /// auf englischer Oberflaeche deutscher Text steht - genau das prueft dieser Fall.
        /// Die Kerntests koennen ihn nicht fuehren: Sie kennen <c>MyResource</c> nicht.
        /// </remarks>
        private static void TextePruefen(Protokoll log)
        {
            log.Leerzeile();
            log.Zeile("--- Texte der Feldsicherung aus MyResource (de und en) ---");

            KiTextlieferant.Einrichten();
            CultureInfo vorher = Thread.CurrentThread.CurrentUICulture;
            try
            {
                foreach (string sprache in new[] { "de-DE", "en-US" })
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(sprache);

                    Vergleiche(log, sprache, "Chathinweis",
                               MyResource.Resource.KI_KERN_FELDSICHERUNG_AUS, KiTexte.FeldsicherungAus);
                    Vergleiche(log, sprache, "Protokollvermerk",
                               MyResource.Resource.KI_KERN_FELDSICHERUNG_VERMERK, KiTexte.FeldsicherungVermerk);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = vorher;
            }
        }

        private static void Vergleiche(Protokoll log, string sprache, string was,
                                       string ausRessource, string ausKern)
        {
            if (string.IsNullOrEmpty(ausRessource))
            {
                log.FehlerZeile(sprache + ": Der Ressourceneintrag fuer den " + was + " fehlt.");
                return;
            }

            if (!string.Equals(ausRessource, ausKern, StringComparison.Ordinal))
            {
                log.FehlerZeile(sprache + ": Der " + was + " des Kerns stammt NICHT aus MyResource.");
                return;
            }

            log.Roh("      " + sprache + "  " + was.PadRight(18) + Kurz(ausKern));
        }

        private static string Kurz(string text)
        {
            string t = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            return t.Length <= 70 ? t : t.Substring(0, 67) + "...";
        }

        private static void Pruefe(Protokoll log, bool erwartet, string was, params string[] argumente)
        {
            bool ist;
            try
            {
                ist = FeldsicherungAusVerlangt(argumente);
            }
            catch (Exception ex)
            {
                log.FehlerZeile("Schalterpruefung '" + was + "': " + ex.Message);
                return;
            }

            if (ist != erwartet)
                log.FehlerZeile("Schalterpruefung '" + was + "': erwartet " + erwartet +
                                ", geliefert " + ist + ".");
            else
                log.Roh("      " + (erwartet ? "JA   " : "NEIN ") + was);
        }

        private static MethodInfo _schalter;

        /// <summary>Ruft <c>Program.FeldsicherungAusVerlangt(string[])</c> der Anwendung.</summary>
        private static bool FeldsicherungAusVerlangt(string[] argumente)
        {
            if (_schalter == null)
            {
                Type typ = typeof(KiAusfuehrer).Assembly
                               .GetType("WindowsFormsApplication1.Program", true);
                _schalter = typ.GetMethod("FeldsicherungAusVerlangt",
                                          BindingFlags.Static | BindingFlags.NonPublic |
                                          BindingFlags.Public,
                                          null, new[] { typeof(string[]) }, null);
                if (_schalter == null)
                    throw new InvalidOperationException(
                        "Program.FeldsicherungAusVerlangt(string[]) wurde nicht gefunden.");
            }

            return (bool)_schalter.Invoke(null, new object[] { argumente });
        }

        private static void MaskePruefen(Protokoll log, Maskenfall fall, KiDialog eintrag)
        {
            log.Zeile(eintrag.Maskenname + " (" + eintrag.Anzeigename + "): " +
                      eintrag.Felder.Count + " Felder, " + eintrag.Knoepfe.Count + " Knoepfe");

            // ---------------------------------------------------------- statisch
            foreach (KiDialogFeld f in eintrag.Felder)
                PfadStatisch(log, fall.Typ, f.Name, f.Controlpfad);

            foreach (KiDialogKnopf k in eintrag.Knoepfe)
                PfadStatisch(log, fall.Typ, k.Name, k.Controlpfad);

            log.Roh("      statisch: jeder Controlpfad ist ein Feld des Maskentyps.");

            // ---------------------------------------------------------- Laufzeit
            if (fall.Bauen == null)
            {
                log.Warnung("Laufzeitpruefung entfaellt fuer " + eintrag.Maskenname +
                            " - " + fall.DbGrund + ".");
                return;
            }

            Form maske;
            try
            {
                maske = fall.Bauen();
            }
            catch (Exception ex)
            {
                log.FehlerZeile(eintrag.Maskenname + ": Maske liess sich nicht bauen - " + ex.Message);
                return;
            }

            try
            {
                // Das Fenster wird NIE gezeigt: Der Katalog fragt nach Controls, nicht nach
                // Sichtbarkeit. Ein Show() brauchte einen Bediener - und der DialogWaechter
                // schloesse es sofort wieder.
                if (maske.Visible)
                    log.FehlerZeile(eintrag.Maskenname + ": Die Maske ist sichtbar geworden.");

                foreach (KiDialogFeld f in eintrag.Felder)
                    FeldZurLaufzeit(log, eintrag, maske, f);

                foreach (KiDialogKnopf k in eintrag.Knoepfe)
                    KnopfZurLaufzeit(log, eintrag, maske, k);

                AufrufknopfPruefen(log, eintrag, maske);
            }
            finally
            {
                try { maske.Dispose(); } catch (Exception) { }
            }
        }

        // =====================================================================
        // Statische Pruefung
        // =====================================================================

        /// <summary>
        /// Weist nach, dass jedes Wegstueck des Controlpfades als Feld des Maskentyps
        /// deklariert ist.
        /// </summary>
        /// <remarks>
        /// Verglichen wird ohne Ruecksicht auf Gross-/Kleinschreibung - genau wie in
        /// <c>KiDialogZugriff.Tiefensuche</c>. Gesucht wird auch in den Basisklassen
        /// (<c>BaseForm</c>, <c>Form</c>), weil <c>GetFields</c> geerbte private Felder
        /// nicht mitliefert.
        /// </remarks>
        private static void PfadStatisch(Protokoll log, Type maskentyp, string name, string pfad)
        {
            foreach (string stufe in (pfad ?? "").Split('.'))
            {
                string gesucht = stufe.Trim();
                if (gesucht.Length == 0) continue;

                if (!FeldVorhanden(maskentyp, gesucht))
                    log.FehlerZeile(maskentyp.Name + "." + name + ": Es gibt kein Feld '" +
                                    gesucht + "' (Controlpfad '" + pfad + "').");
            }
        }

        private static bool FeldVorhanden(Type typ, string name)
        {
            const BindingFlags WO = BindingFlags.Instance | BindingFlags.Public |
                                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (Type t = typ; t != null; t = t.BaseType)
                foreach (FieldInfo f in t.GetFields(WO))
                    if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                        return true;

            return false;
        }

        // =====================================================================
        // Laufzeitpruefung
        // =====================================================================

        private static void FeldZurLaufzeit(Protokoll log, KiDialog eintrag, Form maske,
                                            KiDialogFeld feld)
        {
            Control c = Aufloesen(maske, feld.Controlpfad);
            if (c == null)
            {
                log.FehlerZeile(eintrag.Maskenname + "." + feld.Name + ": Controlpfad '" +
                                feld.Controlpfad + "' loest zur Laufzeit NICHT auf.");
                return;
            }

            bool brauchbar = c is TextBox || c is CheckBox || c is ComboBox;
            if (!brauchbar)
                log.FehlerZeile(eintrag.Maskenname + "." + feld.Name + ": '" + feld.Controlpfad +
                                "' ist ein " + c.GetType().Name + " - kein setzbares Eingabefeld.");

            log.Roh("      Feld  " + feld.Name.PadRight(24) + feld.Controlpfad.PadRight(28) +
                    "-> " + c.GetType().Name);
        }

        private static void KnopfZurLaufzeit(Protokoll log, KiDialog eintrag, Form maske,
                                             KiDialogKnopf knopf)
        {
            Control c = Aufloesen(maske, knopf.Controlpfad);
            if (c == null)
            {
                log.FehlerZeile(eintrag.Maskenname + "." + knopf.Name + ": Knopf '" +
                                knopf.Controlpfad + "' loest zur Laufzeit NICHT auf.");
                return;
            }

            if (!(c is Button))
                log.FehlerZeile(eintrag.Maskenname + "." + knopf.Name + ": '" + knopf.Controlpfad +
                                "' ist ein " + c.GetType().Name + " - kein Knopf.");

            // Enabled wird ABSICHTLICH nicht gefordert: Die Masken sperren je nach Modus
            // einzelne Knoepfe (Form_PufferSp_Bearbeiten.cs:67-79). Ein gesperrter Knopf ist
            // eine Bedienlage, kein Katalogfehler - abgelehnt wird er dann zur Laufzeit im
            // Klartext (KiDialogZugriff.PruefeKnopf).
            log.Roh("      Knopf " + knopf.Name.PadRight(24) + knopf.Controlpfad.PadRight(28) +
                    "-> " + c.GetType().Name + (c.Enabled ? "" : " (gesperrt)"));
        }

        /// <summary>
        /// Prueft den Aufrufknopf aus Paket F2: genau einer, ohne Tabstopp, oben rechts
        /// verankert (Fachkonzept 11.8).
        /// </summary>
        private static void AufrufknopfPruefen(Protokoll log, KiDialog eintrag, Form maske)
        {
            Control[] treffer = maske.Controls.Find(AUFRUFKNOPF, false);
            if (treffer.Length != 1)
            {
                log.FehlerZeile(eintrag.Maskenname + ": " + treffer.Length + " Aufrufknoepfe '" +
                                AUFRUFKNOPF + "' statt genau einem.");
                return;
            }

            Control knopf = treffer[0];
            if (knopf.TabStop)
                log.FehlerZeile(eintrag.Maskenname + ": Der Aufrufknopf nimmt am Tabulator teil.");

            const AnchorStyles ERWARTET = AnchorStyles.Top | AnchorStyles.Right;
            if (knopf.Anchor != ERWARTET)
                log.FehlerZeile(eintrag.Maskenname + ": Der Aufrufknopf ist mit " + knopf.Anchor +
                                " verankert statt mit " + ERWARTET + ".");

            log.Roh("      Aufrufknopf '" + knopf.Text + "' bei " + knopf.Location +
                    ", " + knopf.Size + ", TabStop=" + knopf.TabStop);
        }

        // =====================================================================
        // Zugriff auf die internen Stellen des Anwendungsprojekts
        // =====================================================================

        private static MethodInfo _aufloesen;

        /// <summary>Ruft <c>KiDialogZugriff.Aufloesen(Control, string)</c>.</summary>
        private static Control Aufloesen(Control behaelter, string pfad)
        {
            if (_aufloesen == null)
            {
                Type typ = typeof(KiAusfuehrer).Assembly
                               .GetType("WindowsFormsApplication1.KiDialogZugriff", true);
                _aufloesen = typ.GetMethod("Aufloesen",
                                           BindingFlags.Static | BindingFlags.NonPublic |
                                           BindingFlags.Public,
                                           null,
                                           new[] { typeof(Control), typeof(string) },
                                           null);
                if (_aufloesen == null)
                    throw new InvalidOperationException(
                        "KiDialogZugriff.Aufloesen(Control, string) wurde nicht gefunden.");
            }

            return (Control)_aufloesen.Invoke(null, new object[] { behaelter, pfad });
        }

        /// <summary>
        /// Holt <c>KiDialoge.Katalog</c> - denselben Katalog, den auch die Aktionen sehen.
        /// </summary>
        private static KiDialogKatalog KatalogHolen(Protokoll log)
        {
            try
            {
                Type typ = typeof(KiAusfuehrer).Assembly
                               .GetType("WindowsFormsApplication1.KiDialoge", true);
                PropertyInfo p = typ.GetProperty("Katalog",
                                                 BindingFlags.Static | BindingFlags.NonPublic |
                                                 BindingFlags.Public);
                if (p == null)
                    throw new InvalidOperationException("KiDialoge.Katalog wurde nicht gefunden.");

                return (KiDialogKatalog)p.GetValue(null);
            }
            catch (Exception ex)
            {
                log.FehlerZeile("Der Dialogkatalog liess sich nicht laden: " +
                                (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                return null;
            }
        }
    }
}
