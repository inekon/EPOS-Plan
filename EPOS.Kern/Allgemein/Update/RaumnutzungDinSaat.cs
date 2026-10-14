using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DA-6 - DIE SAAT DER DIN-NUTZUNGSPROFILE (Entscheid E135, Anwender: "DIN/TS 18599-10: Profile sind nicht
    // lizenzpflichtig. Aufnehmen und bei Import pruefen und zuordnen."; Konzept Datenaustausch Simulationsdaten Abschnitt 5).
    //
    // QUELLE. DIN/TS 18599-10:2025-10, Tabellen 5 (Wohnen), 6 und 7 (Nichtwohnen), vom Anwender in das CSV-Format 6.4 der
    // Nutzungsprofile (NP7, mit Heiz_Absenkung_K) uebertragen und am 14.10.2026 geliefert (Datei
    // "DIN_TS_18599-10_Nutzungsprofile_NP7.csv", 45 Datenzeilen x 29 Felder). Die Tabelle steht unten WOERTLICH; gelesen wird
    // sie zur Laufzeit von DEMSELBEN Leser wie jede Datei des Anwenders (RaumnutzungCsv.Lesen). Eine Aenderung der Werte - etwa
    // nach dem offenen Entscheid ueber das Wochenmuster - ist reine Datenpflege an der Konstanten TABELLE (dazu PRUEFSUMME
    // neu); Schritt, Abgleich und Ruecknahme lesen allein von hier.
    //
    // WAS DIE TABELLE TRAEGT. Nutzungs- und (Wohnen) Betriebszeit, Nutzungstage der Woche nur bei 365 Nutzungstagen (fuer
    // weniger Tage nennt die Norm kein Wochenmuster - offener Anwenderentscheid; die Zellen bleiben leer), Heiz- und
    // Kuehlsollwert, Absenkung (der Leser bildet daraus Heiz_Soll_Ausserhalb), Aussenluft mit Einheit, beim Einfamilienhaus
    // "Heizung ausserhalb aus". Leer, weil die Norm sie nicht hergibt: Betriebszeiten Nichtwohnen, Feiertage, die uebrigen
    // Werte ausserhalb, Aussenluft je Person (die Fassung 2025-10 fuehrt keine), Personen, Geraete, Beleuchtung (die Norm
    // nennt Wh/(m2*d) bzw. lx - das steht als Vermerk in der Beschreibung). Die Beschreibung je Profil ist der Quellenvermerk.
    //
    // EINFAMILIENHAUS. "Heizung ausserhalb aus" und die Absenkung (-> Heiz_Soll_Ausserhalb) stehen zusammen: Die Profilpruefung
    // laesst beides zu, und der Generator gibt "aus" den Vorrang (Raumnutzungsgenerator: aus vor Sollwert ausserhalb). Der
    // abgesenkte Sollwert ruht, bis der Anwender "aus" abwaehlt; die Tabelle bleibt wie geliefert.
    //
    // DAS PDF DER NORM liegt nie im Repositorium. Die Tabelle wird nicht aus dem Gedaechtnis ergaenzt oder geaendert.
    // ====================================================================================

    /// <summary>
    /// <b>Die Saat der 45 Nutzungsprofile der Kategorie DIN/TS 18599-10</b> (DA-6, E135): die gelieferte Tabelle im
    /// CSV-Format 6.4, gelesen von <see cref="RaumnutzungCsv"/>. Geschrieben wird sie vom Schemaschritt
    /// <see cref="RaumnutzungDinWerteSchema"/>; „Normwerte einlesen“ (<see cref="RaumnutzungNormwerte"/>) gleicht gegen sie ab
    /// und stellt sie bei der Rücknahme wieder her. Anlass und Inhalt stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungDinSaat
    {
        /// <summary>Die Fassung der Norm, aus der die Tabelle stammt.</summary>
        public const string FASSUNG = "2025-10";

        /// <summary>Die Zahl der Profile der Tabelle (43 nummerierte, zwei Wohnprofile ohne Nummer).</summary>
        public const int ANZAHL = 45;

        /// <summary>Der Tag der Lieferung.</summary>
        public const string LIEFERUNG = "2026-10-14";

        /// <summary>
        /// SHA-256 (klein, hex) der <see cref="TABELLE"/> in UTF-8 — der Zeilenumbruch der Quelltextdatei wirkt nicht
        /// (<see cref="Pruefsumme"/> vereinheitlicht ihn auf LF).
        /// </summary>
        public const string PRUEFSUMME = "391984120b9d1e4e4c4b30ea056866ab367c762a16733bdbe285705de2bd978b";

        /// <summary>
        /// Der Quellenhinweis, den die Kategorie DIN bis zur Saat trug (Schritte 189/190, eingefroren) — der Schritt
        /// <see cref="RaumnutzungDinWerteSchema"/> ersetzt allein ihn durch <see cref="RaumnutzungSaat.QUELLE_DIN"/>.
        /// </summary>
        public const string QUELLENHINWEIS_VOR_SAAT =
            "DIN/TS 18599-10:2025-10, Energetische Bewertung von Gebäuden – Teil 10: Nutzungsrandbedingungen, " +
            "Klimadaten; ersetzt DIN V 18599-10:2018-09. Werte trägt der Anwender aus seiner lizenzierten Ausgabe ein " +
            "oder importiert sie.";

        /// <summary>Die Beschreibung, die die Kategorie DIN bis zur Saat trug (eingefroren; folgt: <see cref="RaumnutzungSaat.BESCHREIBUNG_DIN"/>).</summary>
        public const string BESCHREIBUNG_VOR_SAAT = "Nutzungsprofile nach DIN/TS 18599-10 mit Nummer und Name, ohne Werte";

        /// <summary>Die gelieferte Tabelle, wörtlich (CSV-Format 6.4, Trenner Semikolon, Dezimalpunkt).</summary>
        public const string TABELLE = """
Nummer;Bezeichner;Beschreibung;Nutzung_Von;Nutzung_Bis;Betrieb_Von;Betrieb_Bis;Nutzungstage_Woche;Feiertage_Wie_Sonntag;Heiz_Soll;Heiz_Soll_Ausserhalb;Heiz_Aus_Ausserhalb;Kuehl_Soll;Kuehl_Soll_Ausserhalb;Kuehl_Aus_Ausserhalb;Aussenluft;Aussenluft_Einheit;Aussenluft_Ausserhalb;Aussenluft_Person;Personen_Flaeche;Personen_Waerme;Personen_Anteil;Personen_Anteil_Ausserhalb;Geraete_Leistung;Geraete_Anteil;Geraete_Anteil_Ausserhalb;Beleuchtung_Leistung;Beleuchtung_Anteil;Heiz_Absenkung_K
1;Einzelbüro;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=30 Wh/(m²·d); q_I,app=39 Wh/(m²·d)";7;18;;;;;21;;;24;;;4;m3/hm2;;;;;;;;;;;;4
2;Gruppenbüro (zwei bis sechs Arbeitsplätze);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=30 Wh/(m²·d); q_I,app=70 Wh/(m²·d)";7;18;;;;;21;;;24;;;4;m3/hm2;;;;;;;;;;;;4
3;Großraumbüro (ab sieben Arbeitsplätze);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=42 Wh/(m²·d); q_I,app=60 Wh/(m²·d)";7;18;;;;;21;;;24;;;6;m3/hm2;;;;;;;;;;;;4
4;Besprechung, Sitzung, Seminar;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=93 Wh/(m²·d); q_I,app=5 Wh/(m²·d)";7;18;;;;;21;;;24;;;15;m3/hm2;;;;;;;;;;;;4
5;Schalterhalle;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=35 Wh/(m²·d); q_I,app=24 Wh/(m²·d)";7;18;;;;;21;;;24;;;2;m3/hm2;;;;;;;;;;;;4
6;Einzelhandel/Kaufhaus;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=12 h/d; d_nutz,a=300 d/a; t_v,op,d=14 h/d; d_op,a=300 d/a; t_h,op,d=14 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=84 Wh/(m²·d); q_I,app=24 Wh/(m²·d)";8;20;;;;;21;;;24;;;4;m3/hm2;;;;;;;;;;;;4
7;Einzelhandel/Kaufhaus (Lebensmittelabteilung mit Kühlprodukten);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=12 h/d; d_nutz,a=300 d/a; t_v,op,d=14 h/d; d_op,a=300 d/a; t_h,op,d=14 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=84 Wh/(m²·d); q_I,app=-170 Wh/(m²·d)";8;20;;;;;21;;;24;;;4;m3/hm2;;;;;;;;;;;;4
8;Klassenzimmer (Schule), Gruppenraum (Kindergarten);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=7 h/d; d_nutz,a=200 d/a; t_v,op,d=9 h/d; d_op,a=200 d/a; t_h,op,d=9 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=100 Wh/(m²·d); q_I,app=27 Wh/(m²·d)";8;15;;;;;21;;;24;;;10;m3/hm2;;;;;;;;;;;;4
9;Hörsaal, Auditorium;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=10 h/d; d_nutz,a=150 d/a; t_v,op,d=12 h/d; d_op,a=150 d/a; t_h,op,d=12 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=420 Wh/(m²·d); q_I,app=1 Wh/(m²·d)";8;18;;;;;21;;;24;;;30;m3/hm2;;;;;;;;;;;;4
10;Bettenzimmer;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=24 h/d; d_nutz,a=365 d/a; t_v,op,d=24 h/d; d_op,a=365 d/a; t_h,op,d=24 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=120 Wh/(m²·d); q_I,app=24 Wh/(m²·d)";0;24;;;1111111;;22;;;24;;;5;m3/hm2;;;;;;;;;;;;
11;Hotelzimmer;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=365 d/a; t_v,op,d=24 h/d; d_op,a=365 d/a; t_h,op,d=24 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=70 Wh/(m²·d); q_I,app=44 Wh/(m²·d)";21;8;;;1111111;;21;;;24;;;3;m3/hm2;;;;;;;;;;;;
12;Kantine;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=7 h/d; d_nutz,a=250 d/a; t_v,op,d=9 h/d; d_op,a=250 d/a; t_h,op,d=9 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=175 Wh/(m²·d); q_I,app=3 Wh/(m²·d)";8;15;;;;;21;;;24;;;18;m3/hm2;;;;;;;;;;;;4
13;Restaurant;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=14 h/d; d_nutz,a=300 d/a; t_v,op,d=16 h/d; d_op,a=300 d/a; t_h,op,d=16 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=233 Wh/(m²·d); q_I,app=14 Wh/(m²·d)";10;24;;;;;21;;;24;;;18;m3/hm2;;;;;;;;;;;;4
14;Küchen in Nichtwohngebäuden;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=13 h/d; d_nutz,a=300 d/a; t_v,op,d=15 h/d; d_op,a=300 d/a; t_h,op,d=15 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=56 Wh/(m²·d); q_I,app=1800 Wh/(m²·d)";10;23;;;;;21;;;24;;;90;m3/hm2;;;;;;;;;;;;4
15;Küche – Vorbereitung, Lager;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=13 h/d; d_nutz,a=300 d/a; t_v,op,d=15 h/d; d_op,a=300 d/a; t_h,op,d=15 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=56 Wh/(m²·d); q_I,app=304 Wh/(m²·d)";10;23;;;;;21;;;24;;;15;m3/hm2;;;;;;;;;;;;4
16;WC und Sanitärräume in Nichtwohngebäuden;"DIN/TS 18599-10:2025-10 Tab. 6/7; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";;;;;;;21;;;24;;;15;m3/hm2;;;;;;;;;;;;4
17;Sonstige Aufenthaltsräume;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=93 Wh/(m²·d); q_I,app=3 Wh/(m²·d)";7;18;;;;;21;;;24;;;7;m3/hm2;;;;;;;;;;;;4
18;Nebenflächen (ohne Aufenthaltsräume);"DIN/TS 18599-10:2025-10 Tab. 6/7; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=5 Wh/(m²·d)";;;;;;;21;;;24;;;0.15;m3/hm2;;;;;;;;;;;;4
19;Verkehrsflächen;"DIN/TS 18599-10:2025-10 Tab. 6/7; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";;;;;;;21;;;24;;;0;m3/hm2;;;;;;;;;;;;4
20;Lager, Technik, Archiv;"DIN/TS 18599-10:2025-10 Tab. 6/7; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";;;;;;;21;;;24;;;0.15;m3/hm2;;;;;;;;;;;;4
21;Rechenzentrum;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=24 h/d; d_nutz,a=365 d/a; t_v,op,d=24 h/d; d_op,a=365 d/a; t_h,op,d=24 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=14 Wh/(m²·d); q_I,app=1800 Wh/(m²·d)";0;24;;;1111111;;21;;;24;;;1.3;m3/hm2;;;;;;;;;;;;4
22;Gewerbliche und industrielle Hallen – schwere Arbeit, stehende Tätigkeit;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=9 h/d; d_nutz,a=230 d/a; t_v,op,d=10 h/d; d_op,a=230 d/a; t_h,op,d=10 h/d; θ_i,h,min=15 °C; θ_i,c,max=30 °C; q_I,p=48 Wh/(m²·d); q_I,app=280 Wh/(m²·d)";7;16;;;;;15;;;28;;;3.5;m3/hm2;;;;;;;;;;;;4
23;Gewerbliche und industrielle Hallen – mittelschwere Arbeit;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=9 h/d; d_nutz,a=230 d/a; t_v,op,d=10 h/d; d_op,a=230 d/a; t_h,op,d=10 h/d; θ_i,h,min=15 °C; θ_i,c,max=28 °C; q_I,p=40 Wh/(m²·d); q_I,app=280 Wh/(m²·d)";7;16;;;;;17;;;26;;;2.5;m3/hm2;;;;;;;;;;;;4
24;Gewerbliche und industrielle Hallen – leichte Arbeit;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=9 h/d; d_nutz,a=230 d/a; t_v,op,d=10 h/d; d_op,a=230 d/a; t_h,op,d=10 h/d; θ_i,h,min=18 °C; θ_i,c,max=26 °C; q_I,p=32 Wh/(m²·d); q_I,app=280 Wh/(m²·d)";7;16;;;;;20;;;24;;;1.5;m3/hm2;;;;;;;;;;;;4
25;Zuschauerbereich (Theater und Veranstaltungsbauten);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=4 h/d; d_nutz,a=250 d/a; t_v,op,d=6 h/d; d_op,a=250 d/a; t_h,op,d=6 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=187 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";19;23;;;;;21;;;24;;;40;m3/hm2;;;;;;;;;;;;4
26;Foyer (Theater und Veranstaltungsbauten);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=4 h/d; d_nutz,a=250 d/a; t_v,op,d=6 h/d; d_op,a=250 d/a; t_h,op,d=6 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=88 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";19;23;;;;;21;;;24;;;25;m3/hm2;;;;;;;;;;;;4
27;Bühne (Theater und Veranstaltungsbauten);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=10 h/d; d_nutz,a=250 d/a; t_v,op,d=12 h/d; d_op,a=250 d/a; t_h,op,d=12 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";13;23;;;;;21;;;24;;;0.3;m3/hm2;;;;;;;;;;;;4
28;Messe/Kongress;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=9 h/d; d_nutz,a=150 d/a; t_v,op,d=11 h/d; d_op,a=150 d/a; t_h,op,d=11 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=140 Wh/(m²·d); q_I,app=12 Wh/(m²·d)";9;18;;;;;21;;;24;;;7;m3/hm2;;;;;;;;;;;;4
29;Ausstellungsräume und Museum mit konservatorischen Anforderungen;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=8 h/d; d_nutz,a=250 d/a; t_v,op,d=24 h/d; d_op,a=365 d/a; t_h,op,d=24 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=28 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";10;18;;;;;21;;;24;;;2;m3/hm2;;;;;;;;;;;;4
30;Bibliothek – Lesesaal;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=12 h/d; d_nutz,a=300 d/a; t_v,op,d=14 h/d; d_op,a=300 d/a; t_h,op,d=14 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=168 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";8;20;;;;;21;;;24;;;8;m3/hm2;;;;;;;;;;;;4
31;Bibliothek – Freihandbereich;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=12 h/d; d_nutz,a=300 d/a; t_v,op,d=14 h/d; d_op,a=300 d/a; t_h,op,d=14 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=42 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";8;20;;;;;21;;;24;;;2;m3/hm2;;;;;;;;;;;;4
32;Bibliothek – Magazin und Depot;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=12 h/d; d_nutz,a=300 d/a; t_v,op,d=14 h/d; d_op,a=300 d/a; t_h,op,d=14 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";8;20;;;;;21;;;24;;;3;m3/hm2;;;;;;;;;;;;4
33;Turnhalle (ohne Zuschauerbereich);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=15 h/d; d_nutz,a=250 d/a; t_v,op,d=17 h/d; d_op,a=250 d/a; t_h,op,d=17 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=63 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";8;23;;;;;19;;;24;;;3;m3/hm2;;;;;;;;;;;;4
34;Parkhäuser (Büro- und Privatnutzung);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";7;18;;;;;21;;;24;;;8;m3/hm2;;;;;;;;;;;;4
35;Parkhäuser (öffentliche Nutzung);"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=15 h/d; d_nutz,a=365 d/a; t_v,op,d=17 h/d; d_op,a=365 d/a; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";9;24;;;1111111;;21;;;24;;;8;m3/hm2;;;;;;;;;;;;4
36;Saunabereich;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=12 h/d; d_nutz,a=365 d/a; t_v,op,d=14 h/d; d_op,a=365 d/a; t_h,op,d=14 h/d; θ_i,h,min=23 °C; q_I,p=58 Wh/(m²·d); q_I,app=500 Wh/(m²·d)";10;22;;;1111111;;24;;;;;;15;m3/hm2;;;;;;;;;;;;4
37;Fitnessraum;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=15 h/d; d_nutz,a=365 d/a; t_v,op,d=17 h/d; d_op,a=365 d/a; t_h,op,d=17 h/d; θ_i,h,min=18 °C; θ_i,c,max=26 °C; q_I,p=264 Wh/(m²·d); q_I,app=24 Wh/(m²·d)";8;23;;;1111111;;20;;;24;;;12;m3/hm2;;;;;;;;;;;;4
38;Labor;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=24 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=39 Wh/(m²·d); q_I,app=108 Wh/(m²·d)";7;18;;;;;22;;;24;;;25;m3/hm2;;;;;;;;;;;;4
39;Untersuchungs- und Behandlungsräume;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=11 h/d; d_nutz,a=250 d/a; t_v,op,d=13 h/d; d_op,a=250 d/a; t_h,op,d=13 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=82 Wh/(m²·d); q_I,app=35 Wh/(m²·d)";7;18;;;;;22;;;24;;;10;m3/hm2;;;;;;;;;;;;
40;Spezialpflegebereiche;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=24 h/d; d_nutz,a=365 d/a; t_v,op,d=24 h/d; d_op,a=365 d/a; t_h,op,d=24 h/d; θ_i,h,min=22 °C; θ_i,c,max=26 °C; q_I,p=112 Wh/(m²·d); q_I,app=228 Wh/(m²·d)";0;24;;;1111111;;24;;;24;;;30;m3/hm2;;;;;;;;;;;;
41;Flure des allgemeinen Pflegebereichs;"DIN/TS 18599-10:2025-10 Tab. 6/7; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";;;;;;;22;;;24;;;10;m3/hm2;;;;;;;;;;;;
42;Arztpraxen und Therapeutische Praxen;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=10 h/d; d_nutz,a=250 d/a; t_v,op,d=12 h/d; d_op,a=250 d/a; t_h,op,d=12 h/d; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I,p=53 Wh/(m²·d); q_I,app=25 Wh/(m²·d)";8;18;;;;;22;;;24;;;10;m3/hm2;;;;;;;;;;;;4
43;Lagerhallen, Logistikhallen;"DIN/TS 18599-10:2025-10 Tab. 6/7; t_nutz,d=24 h/d; d_nutz,a=365 d/a; t_v,op,d=24 h/d; d_op,a=365 d/a; t_h,op,d=24 h/d; θ_i,h,min=12 °C; θ_i,c,max=28 °C; q_I,p=0 Wh/(m²·d); q_I,app=0 Wh/(m²·d)";0;24;;;1111111;;12;;;26;;;1;m3/hm2;;;;;;;;;;;;
;Wohngebäude – Einfamilienhaus;"DIN/TS 18599-10:2025-10 Tab. 5; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I=45 Wh/(m²·d); Betrieb=Heizung 06–23 (WLA 00–24); n_nutz=0.5 1/h nicht bedarfsgeführt";0;24;6;23;1111111;;20;;1;25;;;0.5;1/h;;;;;;;;;;;;4
;Wohngebäude – Mehrfamilienhaus;"DIN/TS 18599-10:2025-10 Tab. 5; θ_i,h,min=20 °C; θ_i,c,max=26 °C; q_I=90 Wh/(m²·d); Betrieb=Heizung 06–23 (WLA 00–24); n_nutz=0.5 1/h nicht bedarfsgeführt";0;24;6;23;1111111;;20;;0;25;;;0.5;1/h;;;;;;;;;;;;4
""";

        private static readonly Lazy<IReadOnlyList<Raumnutzungsprofil>> PROFILE = new Lazy<IReadOnlyList<Raumnutzungsprofil>>(Lesen);

        /// <summary>
        /// <b>Die 45 Profile der Saat</b> — Kopf (Nummer, Bezeichner, Beschreibung) und Kennwerte, ohne Zeilenbild und Stunden.
        /// Wirft benannt, wenn die Tabelle nicht genau <see cref="ANZAHL"/> übernehmbare Zeilen trägt. Die Liste ist geteilt:
        /// Wer ein Profil ändern will, nimmt <see cref="Raumnutzungsprofil.Kopie"/>.
        /// </summary>
        public static IReadOnlyList<Raumnutzungsprofil> Profile => PROFILE.Value;

        /// <summary>Die Prüfsumme eines Tabellentexts (UTF-8, Zeilenende vereinheitlicht auf LF, ohne Zeilenende am Schluss).</summary>
        public static string Pruefsumme(string text)
        {
            string t = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t))).ToLowerInvariant();
        }

        /// <summary>
        /// Das Saatprofil zu einem ausgelieferten DIN-Profil — über die Nummer (führende Nullen gleich), ohne Nummer über den
        /// Namen eines Wohnprofils; sonst <c>null</c>. Dieselbe Zuordnung wie „Normwerte einlesen“.
        /// </summary>
        public static Raumnutzungsprofil Zu(string nummer, string bezeichner)
        {
            string nr = RaumnutzungNormwerte.Nummernschluessel(nummer);
            if (nr != null)
                return Profile.FirstOrDefault(p => string.Equals(RaumnutzungNormwerte.Nummernschluessel(p.Nummer), nr, StringComparison.Ordinal));
            string name = bezeichner?.Trim();
            if (string.IsNullOrEmpty(name) || !RaumnutzungNormkategorienSchema.WOHNPROFILE.Contains(name, StringComparer.OrdinalIgnoreCase))
                return null;
            return Profile.FirstOrDefault(p => p.Nummer == null && string.Equals(p.Bezeichner, name, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<Raumnutzungsprofil> Lesen()
        {
            RaumnutzungCsvLesung lesung = RaumnutzungCsv.Lesen(TABELLE);
            if (lesung.Abbruch != null)
                throw new InvalidOperationException("Die Saat der DIN-Nutzungsprofile ist nicht lesbar: " + lesung.Abbruch);
            int abgelehnt = lesung.Zeilen.Count(z => !z.Uebernehmbar);
            List<Raumnutzungsprofil> profile = lesung.Uebernehmbare.Select(z => z.Profil).ToList();
            if (abgelehnt > 0 || profile.Count != ANZAHL)
                throw new InvalidOperationException("Die Saat der DIN-Nutzungsprofile traegt " + profile.Count + " uebernehmbare Zeile(n), " +
                                                    abgelehnt + " abgelehnt; erwartet " + ANZAHL + ".");
            foreach (Raumnutzungsprofil p in profile)
            {
                p.Zeilen = new List<Vorgabezeile>();
                p.Stunden = new List<Raumnutzungsstunden>();
                p.Nutzungstage_Jahr = null;
                p.Ausgeliefert = true;
            }
            return profile;
        }
    }
}
