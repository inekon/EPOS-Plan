<?php
/**
 * Frontend-Lizenzportal (Shortcode [epos_lizenzportal]).
 *
 * Benutzer:      Lizenzstatus, eigene Geräte einsehen und freigeben.
 * Firmen-Admin:  zusätzlich Benutzer verwalten und Lizenzschlüssel neu
 *                erzeugen (per E-Mail oder als .lic-Download).
 * Bei Demo-/Personenlizenzen ist der Inhaber sein eigener Verwalter.
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Portal {

	public static function init() {
		add_shortcode( 'epos_lizenzportal', array( __CLASS__, 'shortcode' ) );
		add_action( 'template_redirect', array( __CLASS__, 'aktionen' ) );
	}

	/** Lizenz des angemeldeten Benutzers. */
	private static function eigene_lizenz() {
		if ( ! is_user_logged_in() ) {
			return null;
		}
		$lizenz_id = (int) get_user_meta( get_current_user_id(), '_epos_lizenz_id', true );
		return $lizenz_id ? Epos_Lizenz_Cpt::lade( $lizenz_id ) : null;
	}

	/** Ist der aktuelle Benutzer Verwalter seiner Lizenz? */
	private static function ist_verwalter( $lizenz ) {
		$user = wp_get_current_user();
		if ( in_array( 'administrator', (array) $user->roles, true ) ) {
			return true;
		}
		$typ = get_post_meta( $lizenz->ID, '_epos_typ', true );
		if ( 'firma' !== $typ ) {
			return true; // Demo/Person: Inhaber verwaltet sich selbst
		}
		return in_array( 'epos_firmenadmin', (array) $user->roles, true );
	}

	/**
	 * POST-Aktionen (vor der Ausgabe, wegen .lic-Download).
	 */
	public static function aktionen() {
		if ( empty( $_POST['epos_aktion'] ) || ! is_user_logged_in() ) {
			return;
		}
		if ( ! isset( $_POST['epos_nonce'] ) || ! wp_verify_nonce( $_POST['epos_nonce'], 'epos_portal' ) ) {
			return;
		}

		$lizenz = self::eigene_lizenz();
		if ( ! $lizenz ) {
			return;
		}

		$aktion = sanitize_key( $_POST['epos_aktion'] );

		// Eigenes Gerät freigeben
		if ( 'geraet_freigeben' === $aktion ) {
			$geraet_id = (int) $_POST['geraet'];
			$geraet    = null;
			foreach ( Epos_Geraete::je_benutzer( $lizenz->ID, get_current_user_id(), false ) as $g ) {
				if ( (int) $g->id === $geraet_id ) {
					$geraet = $g;
				}
			}
			// Verwalter dürfen alle Geräte der Lizenz freigeben
			if ( ! $geraet && self::ist_verwalter( $lizenz ) ) {
				foreach ( Epos_Geraete::je_lizenz( $lizenz->ID ) as $g ) {
					if ( (int) $g->id === $geraet_id ) {
						$geraet = $g;
					}
				}
			}
			if ( $geraet ) {
				Epos_Geraete::deaktivieren( $geraet->id );
				self::meldung( 'Das Gerät wurde freigegeben.' );
			}
			return;
		}

		if ( ! self::ist_verwalter( $lizenz ) ) {
			return;
		}

		// Benutzer hinzufügen
		if ( 'benutzer_hinzu' === $aktion ) {
			$frist = Epos_Lizenz_Cpt::sperrfrist_aktiv( $lizenz->ID );
			if ( $frist ) {
				self::meldung( 'Ein kürzlich freigegebener Platz ist erst ab ' . esc_html( $frist ) . ' Uhr neu belegbar.', true );
				return;
			}
			$ergebnis = Epos_Lizenz_Cpt::benutzer_zuordnen( $lizenz->ID, (string) $_POST['email'] );
			if ( is_wp_error( $ergebnis ) ) {
				self::meldung( $ergebnis->get_error_message(), true );
			} else {
				self::meldung( 'Der Benutzer wurde hinzugefügt und erhält eine E-Mail zum Setzen seines Portal-Passworts.' );
			}
			return;
		}

		// Benutzer deaktivieren
		if ( 'benutzer_deaktivieren' === $aktion ) {
			$ziel = (int) $_POST['benutzer'];
			if ( $ziel === get_current_user_id() ) {
				self::meldung( 'Sie können sich nicht selbst deaktivieren.', true );
				return;
			}
			$ergebnis = Epos_Lizenz_Cpt::benutzer_deaktivieren( $lizenz->ID, $ziel );
			if ( is_wp_error( $ergebnis ) ) {
				self::meldung( $ergebnis->get_error_message(), true );
			} else {
				self::meldung( 'Der Benutzer wurde deaktiviert. Der Platz ist nach der Sperrfrist von 7 Tagen wieder belegbar.' );
			}
			return;
		}

		// Lizenzschlüssel neu erzeugen
		if ( 'schluessel_neu' === $aktion ) {
			$zustellung = sanitize_key( isset( $_POST['zustellung'] ) ? $_POST['zustellung'] : 'email' );
			$neu        = Epos_Lizenz_Cpt::schluessel_neu( $lizenz->ID );
			if ( is_wp_error( $neu ) ) {
				self::meldung( $neu->get_error_message(), true );
				return;
			}
			$firma = get_post_meta( $lizenz->ID, '_epos_firma', true );
			$email = wp_get_current_user()->user_email;

			if ( 'datei' === $zustellung ) {
				$lic = Epos_Token::lic_datei( $neu['schluessel'], $firma, $email );
				if ( ! is_wp_error( $lic ) ) {
					nocache_headers();
					header( 'Content-Type: application/octet-stream' );
					header( 'Content-Disposition: attachment; filename="EPOS-Plan.lic"' );
					header( 'Content-Length: ' . strlen( $lic ) );
					echo $lic; // phpcs:ignore
					exit;
				}
				self::meldung( 'Die Lizenzdatei konnte nicht erzeugt werden.', true );
				return;
			}

			Epos_Mail::schluessel_senden( $email, $neu['schluessel'], $firma, false );
			self::meldung( 'Der neue Lizenzschlüssel wurde an ' . esc_html( $email ) . ' gesendet. Der bisherige Schlüssel ist damit ungültig; bereits aktivierte Geräte laufen unverändert weiter.' );
		}
	}

	private static function meldung( $text, $fehler = false ) {
		$GLOBALS['epos_portal_meldung'] = array( 'text' => $text, 'fehler' => $fehler );
	}

	/**
	 * Ausgabe des Portals.
	 */
	public static function shortcode() {
		if ( ! is_user_logged_in() ) {
			return '<div class="epos-portal"><p>Bitte melden Sie sich an, um Ihre EPOS-Plan-Lizenz zu verwalten.</p>'
				. '<p><a class="button" href="' . esc_url( wp_login_url( get_permalink() ) ) . '">Zur Anmeldung</a></p></div>';
		}

		$lizenz = self::eigene_lizenz();
		if ( ! $lizenz ) {
			return '<div class="epos-portal"><p>Ihrem Konto ist keine EPOS-Plan-Lizenz zugeordnet.</p></div>';
		}

		$typ       = get_post_meta( $lizenz->ID, '_epos_typ', true );
		$typ_text  = array( 'demo' => 'Demoversion', 'person' => 'Personenbezogene Lizenz', 'firma' => 'Firmenlizenz' );
		$bis       = get_post_meta( $lizenz->ID, '_epos_gueltig_bis', true );
		$max       = max( 1, (int) get_post_meta( $lizenz->ID, '_epos_max_benutzer', true ) );
		$aktive    = Epos_Lizenz_Cpt::benutzer( $lizenz->ID, true );
		$verwalter = self::ist_verwalter( $lizenz );
		$nonce     = wp_nonce_field( 'epos_portal', 'epos_nonce', true, false );
		$status_ok = ! is_wp_error( Epos_Lizenz_Cpt::status_pruefen( $lizenz ) );

		ob_start();
		echo '<div class="epos-portal">';

		if ( ! empty( $GLOBALS['epos_portal_meldung'] ) ) {
			$m = $GLOBALS['epos_portal_meldung'];
			printf(
				'<div style="padding:10px 14px;margin-bottom:16px;border-left:4px solid %s;background:%s">%s</div>',
				$m['fehler'] ? '#d63638' : '#00a32a',
				$m['fehler'] ? '#fcf0f1' : '#f0f6f0',
				esc_html( $m['text'] )
			);
		}

		// Status
		echo '<h3>Lizenz</h3><table class="epos-tabelle">';
		printf( '<tr><td>Lizenznummer</td><td><strong>%05d</strong></td></tr>', $lizenz->ID );
		printf( '<tr><td>Typ</td><td>%s</td></tr>', esc_html( isset( $typ_text[ $typ ] ) ? $typ_text[ $typ ] : $typ ) );
		printf( '<tr><td>Inhaber</td><td>%s</td></tr>', esc_html( get_post_meta( $lizenz->ID, '_epos_firma', true ) ) );
		printf( '<tr><td>Gültig bis</td><td>%s %s</td></tr>',
			esc_html( $bis ? date_i18n( 'd.m.Y', strtotime( $bis ) ) : '—' ),
			$status_ok ? '' : ' <span style="color:#d63638">(abgelaufen oder gesperrt)</span>'
		);
		if ( 'firma' === $typ ) {
			printf( '<tr><td>Benutzer</td><td>%d von %d Plätzen belegt</td></tr>', count( $aktive ), $max );
		}
		echo '</table>';

		// Geräte
		$geraete = $verwalter
			? Epos_Geraete::je_lizenz( $lizenz->ID )
			: Epos_Geraete::je_benutzer( $lizenz->ID, get_current_user_id(), false );

		echo '<h3>Registrierte Geräte</h3>';
		if ( empty( $geraete ) ) {
			echo '<p>Noch keine Geräte registriert.</p>';
		} else {
			echo '<table class="epos-tabelle"><tr><th>Gerät</th><th>Benutzer</th><th>Aktiviert</th><th>Letzte Prüfung</th><th>Status</th><th></th></tr>';
			foreach ( $geraete as $g ) {
				$g_user = get_user_by( 'id', $g->user_id );
				echo '<tr>';
				printf( '<td>%s</td>', esc_html( '' !== $g->name ? $g->name : substr( $g->geraete_hash, 0, 12 ) . '…' ) );
				printf( '<td>%s</td>', esc_html( $g_user ? $g_user->user_email : '—' ) );
				printf( '<td>%s</td>', esc_html( date_i18n( 'd.m.Y', strtotime( $g->aktiviert_am ) ) ) );
				printf( '<td>%s</td>', esc_html( $g->letzte_pruefung ? date_i18n( 'd.m.Y', strtotime( $g->letzte_pruefung ) ) : '—' ) );
				printf( '<td>%s</td>', esc_html( $g->status ) );
				echo '<td>';
				if ( 'aktiv' === $g->status ) {
					echo '<form method="post" style="margin:0">' . $nonce; // phpcs:ignore
					printf( '<input type="hidden" name="epos_aktion" value="geraet_freigeben"><input type="hidden" name="geraet" value="%d">', (int) $g->id );
					echo '<button type="submit" class="button">Freigeben</button></form>';
				}
				echo '</td></tr>';
			}
			echo '</table>';
			echo '<p style="font-size:0.9em;color:#666">Ein freigegebenes Gerät zählt nicht mehr zum Gerätelimit; die Anwendung dort verlangt beim nächsten Prüflauf eine Neuaktivierung.</p>';
		}

		// Verwaltung
		if ( $verwalter ) {
			if ( 'firma' === $typ ) {
				echo '<h3>Benutzer verwalten</h3>';
				$alle = Epos_Lizenz_Cpt::benutzer( $lizenz->ID );
				echo '<table class="epos-tabelle"><tr><th>E-Mail</th><th>Status</th><th></th></tr>';
				foreach ( $alle as $u ) {
					$u_status = get_user_meta( $u->ID, '_epos_status', true );
					echo '<tr>';
					printf( '<td>%s%s</td>', esc_html( $u->user_email ), in_array( 'epos_firmenadmin', (array) $u->roles, true ) ? ' <em>(Admin)</em>' : '' );
					printf( '<td>%s</td>', esc_html( 'deaktiviert' === $u_status ? 'deaktiviert' : 'aktiv' ) );
					echo '<td>';
					if ( 'deaktiviert' !== $u_status && $u->ID !== get_current_user_id() ) {
						echo '<form method="post" style="margin:0">' . $nonce; // phpcs:ignore
						printf( '<input type="hidden" name="epos_aktion" value="benutzer_deaktivieren"><input type="hidden" name="benutzer" value="%d">', (int) $u->ID );
						echo '<button type="submit" class="button">Deaktivieren</button></form>';
					}
					echo '</td></tr>';
				}
				echo '</table>';

				if ( count( $aktive ) < $max ) {
					echo '<form method="post" style="margin:12px 0">' . $nonce; // phpcs:ignore
					echo '<input type="hidden" name="epos_aktion" value="benutzer_hinzu">';
					echo '<input type="email" name="email" required placeholder="benutzer@firma.de" style="min-width:260px"> ';
					echo '<button type="submit" class="button">Benutzer hinzufügen</button></form>';
				} else {
					printf( '<p>Alle %d Benutzerplätze sind belegt.</p>', $max );
				}
			}

			echo '<h3>Lizenzschlüssel</h3>';
			echo '<p>Der Lizenzschlüssel wird aus Sicherheitsgründen nicht gespeichert und kann hier nicht angezeigt werden. '
				. 'Bei Verlust erzeugen Sie einen neuen — der bisherige wird damit ungültig, bereits aktivierte Geräte laufen unverändert weiter.</p>';
			echo '<form method="post" onsubmit="return confirm(\'Neuen Lizenzschlüssel erzeugen? Der bisherige wird ungültig.\')">' . $nonce; // phpcs:ignore
			echo '<input type="hidden" name="epos_aktion" value="schluessel_neu">';
			echo '<label><input type="radio" name="zustellung" value="email" checked> per E-Mail zusenden</label> &nbsp; ';
			echo '<label><input type="radio" name="zustellung" value="datei"> als Lizenzdatei (.lic) herunterladen</label><br><br>';
			echo '<button type="submit" class="button button-primary">Neuen Schlüssel erzeugen</button></form>';
		}

		echo '</div>';
		echo '<style>.epos-portal .epos-tabelle{border-collapse:collapse;width:100%;margin-bottom:1.2em}'
			. '.epos-portal .epos-tabelle td,.epos-portal .epos-tabelle th{border:1px solid #ddd;padding:6px 10px;text-align:left}'
			. '.epos-portal h3{margin-top:1.4em}</style>';

		return ob_get_clean();
	}
}
