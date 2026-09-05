using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    public class SimulationStrombedarf
    {
        public int m_ID_Projekt = 0;

        public int[] mo_anfang = new int[12];
        public int[] mo_ende = new int[12];
        public float[] prozesswerte = new float[8760 *4 ];

        /// <summary>
        /// Wochentag des 1. Januar für die Profilkachelung (Montag = 0 … Sonntag = 6,
        /// Entscheidung F3). <b>−1 = automatisch</b>: Dann wird er über die Klimaregion
        /// des Projekts aus <c>Tab_Klimadaten.WE</c> abgeleitet.
        ///
        /// Warum überhaupt eine Selbstermittlung: Diese Klasse bekommt nur die
        /// Projekt-ID, keine Klimaregion — anders als
        /// <see cref="SimulationWaermebedarf"/>, dem der Aufrufer beides übergibt. F3 gilt
        /// aber für ALLE drei Bedarfsarten; ohne eigenen Kalenderzugriff bliebe der
        /// Stromzweig als einziger bei der Altkonvention. Wer den Wert kennt, kann ihn
        /// setzen und spart die zusätzliche Lesung.
        /// </summary>
        public int WochentagJan1 = -1;

        /// <summary>Projekt, für das <see cref="_kalenderWert"/> gilt (−1 = noch keines).</summary>
        private int _kalenderProjekt = -1;

        /// <summary>Zwischengespeicherter Kalender des Projekts <see cref="_kalenderProjekt"/>.</summary>
        private int _kalenderWert = ProfilBedarf.WOCHENTAG_ALTKONVENTION;

        public float[] Strombedarf_viertelStundenwerte = new float[8760 * 4];
        private float[] Strombedarf_sortiert = new float[8760 * 4];
        public float[] Stromganglinie = new float[8760 *4];
        public float[] Strombedarf_monat = new float[12];

        public float Strombedarf_Gebaeude_gesamt;
        public float Stromganglinie_gesamt;
        public float Strombedarf_gesamt;
        public float Strombedarf_Max;

        public float[] Dauerlinie = new float[8760 * 4];
        public float[] Dauerlinie_nicht_sortiert = new float[8760 * 4];

        /// <summary>
        /// Wie viele Plätze von <see cref="Strombedarf_viertelStundenwerte"/> BELEGT sind
        /// (Windows-Abnahme 05.09.2026, Anwenderwunsch W8‑E‑2).
        ///
        /// <para><b>Warum das Feld nötig ist.</b> Der Vektor hat immer 35 040 Plätze, trägt
        /// aber je nach Weg zwei verschiedene Raster: <see cref="Berechnung"/> spreizt auf
        /// Viertelstunden und belegt alle 35 040, die VORSCHAU
        /// (<see cref="ProfilbedarfUebernehmen"/>) legt die 8 760 STUNDENwerte in die ersten
        /// Plätze und lässt den Rest auf null. Wer daraus ein Ganglinienbild zeichnet, muss
        /// wissen, welches von beidem vorliegt — sonst hinge ein Vierteljahr Nullen hinten
        /// dran, und „Woche 12" träfe die falschen Stunden.</para>
        /// </summary>
        public int Stuetzstellen = 8760 * 4;

        public SimulationStrombedarf()
        {
            Classes.Simulation.Init init = new Classes.Simulation.Init();
            init.Monatswerte_berechnen(mo_anfang, mo_ende);
        }

        /// <summary>
        /// Fehlertext eines dialogfrei abgebrochenen Strombedarfslaufs (Paket 8,
        /// Konzept 13.4). Leer, wenn nichts anlag.
        ///
        /// Die Klasse läuft VOR der Kaskade und kennt <see cref="SimulationControl"/>
        /// nicht; ihren Abbruch holt deshalb der jeweilige Einstiegspunkt ab
        /// (<c>SimulationRunner.Simuliere</c>, <c>Form_Simulation_Detail.Energiebedarf</c>).
        /// Bis Paket 8 zeigte sie stattdessen eine MessageBox und kehrte still zurück —
        /// der Lauf rechnete danach mit einem leeren Stromprofil weiter.
        /// </summary>
        public string Fehlertext = "";

        public void Berechnung(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();
            int index = 0;
            double wert = 0;
            int Interval = 0;

            m_ID_Projekt = ID_Projekt;
            Fehlertext = "";

            Strombedarf_Gebaeude_gesamt = 0;
            Stromganglinie_gesamt = 0;
            Strombedarf_gesamt = 0;
            Strombedarf_Max = 0;

            Array.Clear(Strombedarf_viertelStundenwerte, 0, Strombedarf_viertelStundenwerte.Length);
            Array.Clear(Strombedarf_sortiert, 0, Strombedarf_sortiert.Length);
            Array.Clear(prozesswerte, 0, prozesswerte.Length);
            Array.Clear(Stromganglinie, 0, Stromganglinie.Length);
            Array.Clear(Dauerlinie, 0, Dauerlinie.Length);
            Array.Clear(Dauerlinie_nicht_sortiert, 0, Dauerlinie_nicht_sortiert.Length);

            // ***********************************************************************
            // Stromprofile (Stundenwerte)
            // ***********************************************************************
            prozesswerte = Stromprofil_Strombedarf_berechnen();
            if(prozesswerte == null)
            {
                // PAKET 8 (Konzept 13.4): Fehlerkanal statt MessageBox. Der Abbruch
                // dieser Methode ist unverändert; neu ist, dass der Aufrufer davon
                // erfährt und keinen Lauf mit leerem Stromprofil speichert.
                Fehlertext = MyResource.Resource.SIMENG_STROMPROFILE_NICHT_BERECHENBAR;
                SimulationProtokoll.Aktuell.Fehlermeldung(
                    MyResource.Resource.SIMENG_PRAEFIX_STROMBEDARF + Fehlertext);
                return;
            }

            // auf 1/4 Stundenwerte umrechnen
            prozesswerte = Stundenwerte_zu_viertelstunden(prozesswerte);

            Strombedarf_viertelStundenwerte = (float[])prozesswerte.Clone();

            Strombedarf_Gebaeude_gesamt += prozesswerte.Sum() / 4000;

            // ***********************************************************************
            // Stromganglinien Stundenwerte bzw. Viertelstundenwerte gemäß Interval
            // 1=Stundenwerte, 4=Viertelstundenwerte
            // ***********************************************************************
            Z_ProjektStromganglinieCtrl waectrl = new Z_ProjektStromganglinieCtrl();
            waectrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + m_ID_Projekt);
            Stromganglinie_gesamt = 0;
            
            for (int n = 0; n < waectrl.rows; n++)
            {
                // BEFUND B1 (S7): Sichtspalten ueber den Tabellennamen angesprochen
                // (Tab_Stromganglinie.ID, Tab_StromganglinieDaten.ID). SQLite kennt an einer
                // Sicht nur deren eigene Ausgabespalten; der Fehler kam LAUT heraus
                // ("Stromganglinie ... hat 0 Werte"). Zweite ID heisst jetzt ID_Daten.
                rs.Open("select * from Abfrage_ProjektStromGanglinie where ID=" + waectrl.items[n].m_ID_Stromganglinie + " order by ID_Daten");

                index = 0;
                wert = 0;
                Interval = 0;

                while (rs.Next())
                {
                    Interval = (int)rs.Read("Zeitinterval");
                    wert = (double)rs.Read("Wert");
                    if (index < Stromganglinie.Length) Stromganglinie[index] = (float)wert;
                    index++;
                }
                rs.Close();

                // AP5 (Konzept 3.2): Sicherheitsnetz für Altbestände. Bis zur Import-
                // erweiterung konnte eine Ganglinie mit unpassender Wertzahl in der Datenbank
                // stehen. Die Additionsschleife unten lief dann stillschweigend über die
                // Mindestlänge, der Rest blieb 0 - ein zu kleiner Jahresstrombedarf, der
                // vollständig aussah. Minutenreihen (Zeitinterval 60, 525.600 Werte) passten
                // außerdem nie in den 35.040er Vektor und liefen in eine IndexOutOfRange-
                // Ausnahme. Neu importierte Ganglinien sind durch GanglinienPruefung auf
                // 8.760 bzw. 35.040 normalisiert; hier darf nichts mehr anlaufen.
                int erwartet = Interval == 1 ? 8760 : (Interval == 4 ? 8760 * 4 : 0);
                if (erwartet == 0 || index != erwartet)
                {
                    Fehlertext = string.Format(MyResource.Resource.IMPORT_GANGLINIE_RASTER_PASST_NICHT,
                                               waectrl.items[n].m_ID_Stromganglinie, index, Interval, erwartet);
                    SimulationProtokoll.Aktuell.Fehlermeldung(
                        MyResource.Resource.SIMENG_PRAEFIX_STROMBEDARF + Fehlertext);
                    return;
                }

                // Ganglinie mit Stundenwerte aufspreitzen auf 1/4 Stunden
                if (Interval == 1)
                    Stromganglinie = Stundenwerte_zu_viertelstunden(Stromganglinie);

                for (int i = 0; i < Strombedarf_viertelStundenwerte.Length && i < Stromganglinie.Length; i++)
                    Strombedarf_viertelStundenwerte[i] += Stromganglinie[i];

                Stromganglinie_gesamt += Stromganglinie.Sum();
            }

            Stromganglinie_gesamt = Stromganglinie_gesamt / 4000f; // MWh
            Strombedarf_monat = MonatsSumme_MW(Strombedarf_viertelStundenwerte, mo_anfang, mo_ende); // in MWh
            Strombedarf_Max = Maximaler_Strombedarf(Strombedarf_viertelStundenwerte); // in kWh
            Strombedarf_gesamt = Strombedarf_viertelStundenwerte.Sum() / 4000f; // in MWh 
            Strombedarf_sortiert = (float[])Strombedarf_viertelStundenwerte.Clone();
            Dauerlinie_nicht_sortiert = Strombedarf_viertelStundenwerte;
            Strombedarf_sortiert = NormVector(Strombedarf_sortiert, Strombedarf_Max);
            Dauerlinie_nicht_sortiert = NormVector(Dauerlinie_nicht_sortiert, Strombedarf_Max);
            Dauerlinie = SortVector(Strombedarf_sortiert);

            Array.Reverse(Dauerlinie);
        }

        /// <summary>
        /// Der Wochentag, mit dem die Profilkachelung dieses Laufs startet (F3).
        /// Reihenfolge: ausdrücklich gesetzter Wert, sonst der Kalender der Klimaregion
        /// des Projekts, sonst die Altkonvention (Sonntag).
        ///
        /// Das Ergebnis wird JE PROJEKT zwischengespeichert: Die Vorschaudialoge rufen die
        /// Profilrechnung mehrfach hintereinander, und der Kalender ändert sich zwischen
        /// zwei Aufrufen nicht.
        /// </summary>
        private int WochentagJan1Aufloesen()
        {
            if (WochentagJan1 >= 0) return WochentagJan1;
            if (_kalenderProjekt == m_ID_Projekt) return _kalenderWert;

            int wochentag = ProfilBedarf.WOCHENTAG_ALTKONVENTION;
            if (m_ID_Projekt > 0)
            {
                ProjektCtrl projektCtrl = new ProjektCtrl();
                projektCtrl.ReadSingle(m_ID_Projekt);
                wochentag = ProfilBedarf.WochentagJan1AusKlimaregion(projektCtrl.m_ID_Klimaregion);
            }

            _kalenderProjekt = m_ID_Projekt;
            _kalenderWert = wochentag;
            return wochentag;
        }

        /// <summary>
        /// Stromverbraucherprofile als Stundenganglinie [8760].
        ///
        /// PAKET K1: Der Algorithmus („12 Monatswerte × 168-h-Wochenprofil → 8760")
        /// steht jetzt einmal in <see cref="ProfilBedarf"/> — gemeinsam mit dem
        /// Brauchwasser- und dem Prozesswärmezweig (Konzept 4.2). Damit gelten hier
        /// dieselben Fehlerpfade wie dort; das ist gegenüber dem Bestand eine
        /// ÄNDERUNG: Ein fehlender Kopfsatz und ein fehlendes Wochenprofil liefen im
        /// Stromzweig bisher still durch — letzteres sogar mit dem Profil des vorigen
        /// Durchlaufs weiter (derselbe Befund, den V0-3 für die beiden Wärmezweige
        /// behoben hat). Beides wird jetzt gemeldet und mit Anteil 0 übersprungen.
        ///
        /// V0-2 UNVERÄNDERT: Der Ergebnisvektor summiert ALLE Profile — die
        /// Profilroutine addiert auf, statt zu überschreiben. Bei genau einem Profil ist
        /// das Ergebnis dasselbe wie vor V0.
        ///
        /// Rückgabe <c>null</c> = Abbruch (wie bisher): Der Aufrufer macht daraus den
        /// Fehlertext des Laufs, damit kein Ergebnis mit leerem Stromprofil entsteht.
        /// </summary>
        public float[] Stromprofil_Strombedarf_berechnen(List<string> list = null)
        {
            float[] summe = new float[8760];

            // NACHARBEIT PAKET 8, BEFUND N6: Das gerade bearbeitete Stromprofil, damit der
            // Sammel-catch unten sagen kann, WORAN es lag. Die häufigste Ursache ist eine
            // InvalidCastException aus dem Lesen eines leeren Monats- oder Wochenfelds
            // (DBNull). Ohne den Profilnamen muss der Anwender alle Stromprofile des
            // Projekts durchsehen. Die Profilroutine schreibt ihn laufend mit.
            ProfilLaufInfo info = new ProfilLaufInfo();

            try
            {
                ProfilQuellmodus modus = ProfilBedarf.Vorschaumodus(list, m_ID_Projekt);

                bool vollstaendig = ProfilBedarf.Rechnen(
                    ProfilQuelle.Strom(modus), m_ID_Projekt, list,
                    WochentagJan1Aufloesen(), mo_anfang, mo_ende, summe, null, info);

                // Typbezug leer: Bis hierher lief der Lauf in eine InvalidCastException und
                // brach über den catch ab. Jetzt meldet die Profilroutine den Grund und der
                // Abbruch bleibt - dieselbe Wirkung, mit Diagnose.
                if (!vollstaendig) return null;

                return summe;
            }
            catch (SystemException ex)
            {
                // PAKET 8 (Konzept 13.4): Der Sammel-catch meldet dialogfrei. Die
                // Rückgabe null ist unverändert - der Aufrufer oben macht daraus den
                // Fehlertext des Laufs.
                SimulationProtokoll.Aktuell.Fehlermeldung(string.Format(
                    MyResource.Resource.SIMENG_STROMPROFILE_DIAGNOSE,
                    string.IsNullOrEmpty(info.AktuellerName)
                        ? ""
                        : string.Format(MyResource.Resource.SIMENG_STROMPROFIL_ZULETZT_BEARBEITET,
                                        info.AktuellerName),
                    ex.Message));
                return null;
            }
        }

        /// <summary>
        /// Übernimmt eine gerechnete STUNDENREIHE [kWh] als Vorschaustand — der Knopf
        /// „Simulation" der Bedarfsprofil- und der Bedarfsverwaltungsdialoge.
        ///
        /// <para><b>Warum es die Methode gibt (Befund W8‑B‑3, Windows-Abnahme
        /// 05.09.2026).</b> Die Vorschauwege setzten die vier Kennzahlen jeder für sich
        /// zusammen, und in der Fassung des Bedarfsprofildialogs fehlte genau EINE Zeile:
        /// <see cref="Strombedarf_Gebaeude_gesamt"/> wurde nie belegt und blieb 0 — worauf
        /// die Zeile darunter <see cref="Strombedarf_gesamt"/> mit derselben 0
        /// überschrieb. Die Ergebnisanzeige zeigte „Gesamter Strombedarf 0" und
        /// „Strombedarf Gebäude 0", während „max. Strombedarf" mit 3,72 kW dastand —
        /// dieselbe Klasse Fehler wie W9‑B‑4/B‑5: eine von Hand nachgezogene Abschrift
        /// des Rechenwegs, aus der eine Zeile herausgefallen ist. Sie steht jetzt einmal,
        /// an der Klasse, deren Felder sie belegt.</para>
        ///
        /// <para><b>Die Einheiten sind die von <see cref="Berechnung"/>.</b> Die drei
        /// Energiemengen liegen in MWh, <see cref="Strombedarf_Max"/> ist eine LEISTUNG in
        /// kW, und <see cref="Strombedarf_monat"/> kommt aus
        /// <c>BhkwPlan.MonatsSumme</c> (Stundenindizes × 0,001) und liegt damit ebenfalls
        /// in MWh. Genau so liest die Ergebnishülle die Felder.</para>
        ///
        /// <para><b>Die Stromganglinie zählt hier nicht mit.</b> Eine Vorschau rechnet die
        /// AUSGEWÄHLTEN PROFILE, nicht das ganze Projekt; <see cref="Stromganglinie_gesamt"/>
        /// bleibt deshalb 0 und die Gesamtsumme ist die Summe beider Posten — dieselbe
        /// Rechnung wie im Lauf, nur mit einem Summanden weniger.</para>
        ///
        /// <para><b>Der Rechenweg bleibt unberührt</b>: <see cref="Berechnung"/> hat seine
        /// eigenen Zeilen, und der Referenzlauf bleibt byte-gleich.</para>
        /// </summary>
        /// <param name="stundenreihe">
        /// Die Stundenwerte aus <see cref="Stromprofil_Strombedarf_berechnen"/> [kWh];
        /// <c>null</c> lässt alles auf null.
        /// </param>
        public void ProfilbedarfUebernehmen(float[] stundenreihe)
        {
            Array.Clear(Strombedarf_viertelStundenwerte, 0, Strombedarf_viertelStundenwerte.Length);
            Strombedarf_Gebaeude_gesamt = 0;
            Stromganglinie_gesamt = 0;
            Strombedarf_gesamt = 0;
            Strombedarf_Max = 0;
            Array.Clear(Strombedarf_monat, 0, Strombedarf_monat.Length);
            Stuetzstellen = 0;

            if (stundenreihe == null || stundenreihe.Length == 0) return;

            // Das Zielfeld hat 35 040 Plaetze (Viertelstunden); belegt werden die ersten
            // 8 760. Woertlich wie in den Vorlaeufern - gespreizt wird hier NICHT, und
            // BhkwPlan.MonatsSumme zaehlt genau diese Stundenindizes ab.
            int n = Math.Min(stundenreihe.Length, Strombedarf_viertelStundenwerte.Length);
            Array.Copy(stundenreihe, Strombedarf_viertelStundenwerte, n);
            Stuetzstellen = n;

            // DIE ZEILE, DIE IM BEDARFSPROFILDIALOG FEHLTE (W8-B-3).
            Strombedarf_Gebaeude_gesamt = stundenreihe.Sum() / 1000;
            Strombedarf_gesamt = Strombedarf_Gebaeude_gesamt + Stromganglinie_gesamt;

            WPPlan.Core.BhkwPlan.MonatsSumme(Strombedarf_viertelStundenwerte, Strombedarf_monat,
                                             mo_anfang, mo_ende);
            Strombedarf_Max = Maximaler_Strombedarf(Strombedarf_viertelStundenwerte);
        }

        public float Maximaler_Strombedarf(float[] Strombedarf)
        {
            float Strombedarf_Max;

            Strombedarf_Max = 0;
            for (int i = 0; i < Strombedarf.Length; i++)
            {
                if (Strombedarf_Max < Strombedarf[i]) Strombedarf_Max = Strombedarf[i];
            }

            return Strombedarf_Max;
        }

        public float[] MonatsSumme_MW(float[] werte_array, int[] mo_anfang, int[] mo_ende)
        {
            float[] z = new float[12];
            for (int indexMonat = 0; indexMonat < 12; indexMonat++)
            {
                //var result = werte_array..GetRange(mo_anfang[indexMonat], mo_ende[indexMonat] - mo_anfang[indexMonat] + 1);

                for (int n = mo_anfang[indexMonat]*4; n <= mo_ende[indexMonat]*4; n++)
                {
                    z[indexMonat] += werte_array[n]; // Addiert numbers[1], numbers[2], numbers[3]
                }

                z[indexMonat] = z[indexMonat] / 4000.0f;
            }
            return z;
        }

        public float[] NormVector(float[] array1, float value)
        {
            // sort numbers in vector
            float[] z = array1.Select(x => (x / value) * 100).ToArray();
            return z;
        }

        public float[] SortVector(float[] array1)
        {
            // sort numbers in vector
            float[] z = array1.OrderBy(x => x).ToArray();
            return z;
        }

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {  
            float[] viertelstundenwerte = new float[8760 * 4];
            for (int i = 0; i < 8760; i++)
            {
                viertelstundenwerte[i * 4] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 1] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 2] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 3] = stundenwerte[i];
            }
            return viertelstundenwerte;
        }
        
        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] + array2[i];
            }
            return result;
        }
    }
}
