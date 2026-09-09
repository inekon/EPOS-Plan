<?php
/**
 * wp-admin: Lizenzverwaltung für INEKON.
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Admin {

	public static function init() {
		add_action( 'add_meta_boxes_epos_lizenz', array( __CLASS__, 'metaboxen' ) );
		add_action( 'save_post_epos_lizenz', array( __CLASS__, 'speichern' ), 10, 2 );
		add_filter( 'manage_epos_lizenz_posts_columns', array( __CLASS__, 'spalten' ) );
		add_action( 'manage_epos_lizenz_posts_custom_column', array( __CLASS__, 'spalten_inhalt' ), 10, 2 );
		add_action( 'admin_post_epos_schluessel_neu', array( __CLASS__, 'aktion_schluessel_neu' ) );
		add_action( 'admin_post_epos_lic_download', array( __CLASS__, 'aktion_lic_download' ) );
		add_action( 'admin_post_epos_benutzer_loesen', array( __CLASS__, 'aktion_benutzer_loesen' ) );
		add_action( 'admin_notices', array( __CLASS__, 'schluessel_hinweis' ) );
		add_action( 'admin_menu', array( __CLASS__, 'einstellungen_menue' ) );
	}

	/* ------------------------------------------------- Metaboxen */

	public static function metaboxen() {
		add_meta_box( 'epos_daten', 'Lizenzdaten', array( __CLASS__, 'box_daten' ), 'epos_lizenz', 'normal', 'high' );
		add_meta_box( 'epos_schluessel', 'Lizenzschlüssel', array( __CLASS__, 'box_schluessel' ), 'epos_lizenz', 'normal' );
		add_meta_box( 'epos_benutzer', 'Benutzer', array( __CLASS__, 'box_benutzer' ), 'epos_lizenz', 'normal' );
		add_meta_box( 'epos_geraete', 'Geräte', array( __CLASS__, 'box_geraete' ), 'epos_lizenz', 'normal' );
		add_meta_box( 'epos_supportvertrag', 'Supportvertrag (optional)', array( __CLASS__, 'box_supportvertrag' ), 'epos_lizenz', 'normal' );
	}

	public static function box_daten( $post ) {
		wp_nonce_field( 'epos_lizenz_speichern', 'epos_lizenz_nonce' );
		$typ    = get_post_meta( $post->ID, '_epos_typ', true ) ?: 'firma';
		$firma  = get_post_meta( $post->ID, '_epos_firma', true );
		$max    = get_post_meta( $post->ID, '_epos_max_benutzer', true ) ?: 1;
		$ab     = get_post_meta( $post->ID, '_epos_gueltig_ab', true ) ?: gmdate( 'Y-m-d' );
		$bis    = get_post_meta( $post->ID, '_epos_gueltig_bis', true ) ?: gmdate( 'Y-m-d', time() + YEAR_IN_SECONDS );
		$status = get_post_meta( $post->ID, '_epos_status', true ) ?: 'aktiv';
		$edition = get_post_meta( $post->ID, '_epos_edition', true ) ?: 'standard';
		?>
		<table class="form-table">
			<tr><th>Lizenznummer</th><td><code><?php printf( '%05d', $post->ID ); ?></code> (wird automatisch vergeben)</td></tr>
			<tr><th><label for="epos_typ">Typ</label></th><td>
				<select name="epos_typ" id="epos_typ">
					<option value="demo" <?php selected( $typ, 'demo' ); ?>>Demoversion (D)</option>
					<option value="person" <?php selected( $typ, 'person' ); ?>>Personenbezogen (P)</option>
					<option value="firma" <?php selected( $typ, 'firma' ); ?>>Firmenlizenz (F)</option>
				</select>
			</td></tr>
			<tr><th><label for="epos_firma">Firma / Inhaber</label></th>
				<td><input type="text" class="regular-text" name="epos_firma" id="epos_firma" value="<?php echo esc_attr( $firma ); ?>"></td></tr>
			<tr><th><label for="epos_max">Max. Benutzer</label></th>
				<td><input type="number" min="1" name="epos_max" id="epos_max" value="<?php echo esc_attr( $max ); ?>"> (nur bei Firmenlizenz; sonst 1)</td></tr>
			<tr><th><label for="epos_ab">Gültig ab</label></th>
				<td><input type="date" name="epos_ab" id="epos_ab" value="<?php echo esc_attr( $ab ); ?>"></td></tr>
			<tr><th><label for="epos_bis">Gültig bis</label></th>
				<td><input type="date" name="epos_bis" id="epos_bis" value="<?php echo esc_attr( $bis ); ?>"> — Verlängerung: einfach dieses Datum neu setzen</td></tr>
			<tr><th><label for="epos_status">Status</label></th><td>
				<select name="epos_status" id="epos_status">
					<option value="aktiv" <?php selected( $status, 'aktiv' ); ?>>aktiv</option>
					<option value="gesperrt" <?php selected( $status, 'gesperrt' ); ?>>gesperrt</option>
				</select>
			</td></tr>
			<tr><th><label for="epos_edition">Edition</label></th>
				<td><input type="text" name="epos_edition" id="epos_edition" value="<?php echo esc_attr( $edition ); ?>"></td></tr>
		</table>
		<?php
	}

	public static function box_schluessel( $post ) {
		$erzeugt = (int) get_post_meta( $post->ID, '_epos_schluessel_erzeugt', true );
		$hash    = get_post_meta( $post->ID, '_epos_schluessel_hash', true );
		echo '<p>';
		if ( $hash ) {
			printf( 'Zuletzt erzeugt am <strong>%s</strong>. Der Schlüssel ist nur als Hash gespeichert und kann nicht angezeigt werden.', esc_html( $erzeugt ? date_i18n( 'd.m.Y H:i', $erzeugt ) : '—' ) );
		} else {
			echo '<strong>Noch kein Schlüssel erzeugt.</strong> Ohne Schlüssel ist keine Aktivierung möglich.';
		}
		echo '</p>';
		// Seit 1.4.1: Der Knopf erscheint erst auf einer veröffentlichten Lizenz mit
		// gespeichertem Typ. Vorher konnte man auf dem Entwurf nach „Neue Lizenz
		// anlegen“ sofort einen Schlüssel erzeugen: ohne Typkennung, und die Lizenz
		// blieb als Entwurf in der Liste unsichtbar.
		$typ_gespeichert = (string) get_post_meta( $post->ID, '_epos_typ', true );
		if ( 'publish' !== $post->post_status || ! isset( Epos_Schluessel::TYPEN[ $typ_gespeichert ] ) ) {
			echo '<p><strong>Noch kein Schlüssel möglich.</strong> Bitte zuerst die Lizenzdaten ausfüllen (Typ, Firma, Laufzeit) und die Lizenz <em>veröffentlichen</em>. Der Knopf „Neuen Schlüssel erzeugen“ erscheint danach.</p>';
			return;
		}
		$url = wp_nonce_url(
			admin_url( 'admin-post.php?action=epos_schluessel_neu&lizenz=' . $post->ID ),
			'epos_schluessel_neu_' . $post->ID
		);
		printf(
			'<a href="%s" class="button button-primary" onclick="return confirm(\'Neuen Lizenzschlüssel erzeugen? Ein vorhandener wird ungültig.\')">Neuen Schlüssel erzeugen</a>',
			esc_url( $url )
		);
		echo '<p class="description">Der neue Schlüssel wird einmalig angezeigt — mit der Möglichkeit, ihn als .lic-Datei herunterzuladen oder per E-Mail zu versenden.</p>';
	}

	public static function box_benutzer( $post ) {
		$benutzer = Epos_Lizenz_Cpt::benutzer( $post->ID );
		if ( empty( $benutzer ) ) {
			echo '<p>Noch keine Benutzer zugeordnet. Benutzer ordnen sich bei der Aktivierung selbst zu (solange Plätze frei sind) oder werden im Frontend-Portal vom Firmen-Admin angelegt.</p>';
		} else {
			echo '<table class="widefat striped"><thead><tr><th>E-Mail</th><th>Rolle</th><th>Status</th><th>Aktion</th></tr></thead><tbody>';
			foreach ( $benutzer as $u ) {
				printf(
					'<tr><td><a href="%s">%s</a></td><td>%s</td><td>%s</td><td><a href="%s" onclick="return confirm( \'Benutzer wirklich von der Lizenz lösen? Die E-Mail wird damit für andere Lizenzen frei.\' );">Von Lizenz lösen</a></td></tr>',
					esc_url( get_edit_user_link( $u->ID ) ),
					esc_html( $u->user_email ),
					in_array( 'epos_firmenadmin', (array) $u->roles, true ) ? 'Firmen-Admin' : 'Benutzer',
					esc_html( get_user_meta( $u->ID, '_epos_status', true ) ?: 'aktiv' ),
					esc_url( wp_nonce_url( admin_url( 'admin-post.php?action=epos_benutzer_loesen&lizenz=' . $post->ID . '&benutzer=' . $u->ID ), 'epos_benutzer_loesen_' . $post->ID . '_' . $u->ID ) )
				);
			}
			echo '</tbody></table>';
			echo '<p class="description">Rolle „Firmen-Admin" im jeweiligen Benutzerprofil vergeben (Rolle <code>epos_firmenadmin</code>).</p>';
		}
	}

	public static function box_geraete( $post ) {
		$geraete = Epos_Geraete::je_lizenz( $post->ID );
		if ( empty( $geraete ) ) {
			echo '<p>Noch keine Geräte registriert.</p>';
			return;
		}
		echo '<table class="widefat striped"><thead><tr><th>Gerät</th><th>Benutzer</th><th>Aktiviert</th><th>Letzte Prüfung</th><th>Status</th></tr></thead><tbody>';
		foreach ( $geraete as $g ) {
			$u = get_user_by( 'id', $g->user_id );
			printf(
				'<tr><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td></tr>',
				esc_html( '' !== $g->name ? $g->name : substr( $g->geraete_hash, 0, 16 ) . '…' ),
				esc_html( $u ? $u->user_email : '—' ),
				esc_html( date_i18n( 'd.m.Y H:i', strtotime( $g->aktiviert_am ) ) ),
				esc_html( $g->letzte_pruefung ? date_i18n( 'd.m.Y H:i', strtotime( $g->letzte_pruefung ) ) : '—' ),
				esc_html( $g->status )
			);
		}
		echo '</tbody></table>';
	}

	public static function speichern( $post_id, $post ) {
		if ( ! isset( $_POST['epos_lizenz_nonce'] ) || ! wp_verify_nonce( $_POST['epos_lizenz_nonce'], 'epos_lizenz_speichern' ) ) {
			return;
		}
		if ( defined( 'DOING_AUTOSAVE' ) && DOING_AUTOSAVE ) {
			return;
		}
		if ( ! current_user_can( 'manage_options' ) ) {
			return;
		}
		$typ = isset( $_POST['epos_typ'] ) && isset( Epos_Schluessel::TYPEN[ $_POST['epos_typ'] ] ) ? $_POST['epos_typ'] : 'firma';
		update_post_meta( $post_id, '_epos_typ', $typ );
		update_post_meta( $post_id, '_epos_firma', sanitize_text_field( isset( $_POST['epos_firma'] ) ? $_POST['epos_firma'] : '' ) );
		update_post_meta( $post_id, '_epos_max_benutzer', ( 'firma' === $typ ) ? max( 1, (int) ( isset( $_POST['epos_max'] ) ? $_POST['epos_max'] : 1 ) ) : 1 );
		update_post_meta( $post_id, '_epos_gueltig_ab', sanitize_text_field( isset( $_POST['epos_ab'] ) ? $_POST['epos_ab'] : '' ) );
		update_post_meta( $post_id, '_epos_gueltig_bis', sanitize_text_field( isset( $_POST['epos_bis'] ) ? $_POST['epos_bis'] : '' ) );
		update_post_meta( $post_id, '_epos_status', ( isset( $_POST['epos_status'] ) && 'gesperrt' === $_POST['epos_status'] ) ? 'gesperrt' : 'aktiv' );
		update_post_meta( $post_id, '_epos_edition', sanitize_text_field( isset( $_POST['epos_edition'] ) ? $_POST['epos_edition'] : 'standard' ) );

		// Supportvertrag-Vermerk (optional, auch nachträglich)
		if ( class_exists( 'Epos_Vertrag' ) ) {
			if ( ! empty( $_POST['epos_support_entfernen'] ) ) {
				delete_post_meta( $post_id, '_epos_supportvertrag' );
			} elseif ( ! empty( $_POST['epos_support_vermerken'] ) && ! get_post_meta( $post_id, '_epos_supportvertrag', true ) ) {
				$sup = Epos_Vertrag::dokument( 'support' );
				update_post_meta( $post_id, '_epos_supportvertrag', array(
					'abgeschlossen_utc'   => gmdate( 'Y-m-d H:i:s' ) . ' UTC',
					'abgeschlossen_lokal' => date_i18n( 'd.m.Y H:i:s' ),
					'quelle'              => 'admin',
					'bestellung'          => 0,
					'ip'                  => '',
					'fassung'             => $sup ? array( 'stand' => $sup['stand'], 'sha256' => $sup['hash'], 'datei' => basename( $sup['pfad'] ) ) : null,
				) );
			}
		}
	}

	
	/**
	 * Metabox „Supportvertrag (optional)": Status anzeigen; kann jederzeit
	 * nachträglich vermerkt werden (Abschluss im Kundenkonto oder in Textform).
	 */
	public static function box_supportvertrag( $post ) {
		$rec = class_exists( 'Epos_Vertrag' ) ? Epos_Vertrag::support_der_lizenz( $post->ID ) : null;
		if ( $rec ) {
			$quellen = array( 'bestellung' => 'mit der Bestellung', 'konto' => 'nachträglich im Kundenkonto', 'admin' => 'manuell durch INEKON vermerkt' );
			$q = ( isset( $rec['quelle'] ) && isset( $quellen[ $rec['quelle'] ] ) ) ? $quellen[ $rec['quelle'] ] : '';
			echo '<p><strong>Supportvertrag abgeschlossen</strong> am ' . esc_html( isset( $rec['abgeschlossen_lokal'] ) ? $rec['abgeschlossen_lokal'] : '—' ) . ( $q ? ' — ' . esc_html( $q ) : '' ) . '.</p>';
			if ( ! empty( $rec['fassung']['stand'] ) ) {
				echo '<p class="description">Akzeptierte Fassung: Stand ' . esc_html( $rec['fassung']['stand'] ) . ' · SHA-256: <code>' . esc_html( substr( (string) $rec['fassung']['sha256'], 0, 16 ) ) . '…</code></p>';
			}
			if ( get_post_meta( $post->ID, '_epos_supportvertrag', true ) ) {
				echo '<p><label><input type="checkbox" name="epos_support_entfernen" value="1"> Vermerk beim Speichern entfernen (nur zur Korrektur von Fehleinträgen)</label></p>';
			}
		} else {
			echo '<p>Kein Supportvertrag abgeschlossen. Der Kunde kann ihn jederzeit selbst im Kundenkonto abschließen (200 € netto je Stunde, Mindestabnahme eine Stunde pro Jahr, darüber hinaus Abrechnung in 15-Minuten-Schritten; Laufzeit bis Jahresende mit automatischer Verlängerung, kündbar zum Jahresende) — oder Sie vermerken einen bereits in Textform geschlossenen Vertrag hier:</p>';
			echo '<p><label><input type="checkbox" name="epos_support_vermerken" value="1"> Supportvertrag beim Speichern als abgeschlossen vermerken (Abschluss liegt in Textform vor)</label></p>';
		}
	}

	/* ------------------------------------------------- Listenspalten */

	public static function spalten( $spalten ) {
		return array(
			'cb'          => $spalten['cb'],
			'title'       => 'Firma / Inhaber',
			'epos_nummer' => 'Nr.',
			'epos_typ'    => 'Typ',
			'epos_bis'    => 'Gültig bis',
			'epos_plaetze' => 'Benutzer',
			'epos_support' => 'Supportvertrag',
			'epos_status' => 'Status',
		);
	}

	public static function spalten_inhalt( $spalte, $post_id ) {
		switch ( $spalte ) {
			case 'epos_nummer':
				printf( '<code>%05d</code>', $post_id );
				break;
			case 'epos_typ':
				$t = get_post_meta( $post_id, '_epos_typ', true );
				$namen = array( 'demo' => 'Demo (D)', 'person' => 'Person (P)', 'firma' => 'Firma (F)' );
				echo esc_html( isset( $namen[ $t ] ) ? $namen[ $t ] : $t );
				break;
			case 'epos_bis':
				$bis = get_post_meta( $post_id, '_epos_gueltig_bis', true );
				if ( $bis ) {
					$abgelaufen = strtotime( $bis . ' 23:59:59 UTC' ) < time();
					printf( $abgelaufen ? '<span style="color:#d63638">%s</span>' : '%s', esc_html( date_i18n( 'd.m.Y', strtotime( $bis ) ) ) );
				} else {
					echo '—';
				}
				break;
			case 'epos_plaetze':
				printf( '%d / %d', count( Epos_Lizenz_Cpt::benutzer( $post_id, true ) ), max( 1, (int) get_post_meta( $post_id, '_epos_max_benutzer', true ) ) );
				break;
			case 'epos_support':
				$rec = class_exists( 'Epos_Vertrag' ) ? Epos_Vertrag::support_der_lizenz( $post_id ) : null;
				if ( $rec ) {
					$wann = isset( $rec['abgeschlossen_lokal'] ) ? trim( strtok( (string) $rec['abgeschlossen_lokal'], ' ' ) ) : '';
					echo esc_html( 'ja' . ( $wann ? ' (seit ' . $wann . ')' : '' ) );
				} else {
					echo '—';
				}
				break;
			case 'epos_status':
				$s = get_post_meta( $post_id, '_epos_status', true );
				echo ( 'gesperrt' === $s ) ? '<span style="color:#d63638">gesperrt</span>' : 'aktiv';
				break;
		}
	}

	/* ------------------------------------------------- Schlüssel-Aktionen */

	public static function aktion_schluessel_neu() {
		$lizenz_id = (int) ( isset( $_GET['lizenz'] ) ? $_GET['lizenz'] : 0 );
		if ( ! current_user_can( 'manage_options' ) || ! wp_verify_nonce( isset( $_GET['_wpnonce'] ) ? $_GET['_wpnonce'] : '', 'epos_schluessel_neu_' . $lizenz_id ) ) {
			wp_die( 'Keine Berechtigung.' );
		}
		$neu = Epos_Lizenz_Cpt::schluessel_neu( $lizenz_id );
		if ( is_wp_error( $neu ) ) {
			wp_die( esc_html( $neu->get_error_message() ) );
		}
		// Einmalige Anzeige: 10 Minuten für den angemeldeten Admin vorhalten
		set_transient( 'epos_schluessel_anzeige_' . get_current_user_id(), array(
			'lizenz'     => $lizenz_id,
			'schluessel' => $neu['schluessel'],
		), 10 * MINUTE_IN_SECONDS );

		wp_safe_redirect( get_edit_post_link( $lizenz_id, 'url' ) );
		exit;
	}

	/** Admin-Aktion: Benutzer vollständig von der Lizenz lösen (Bindung entfernen). */
	public static function aktion_benutzer_loesen() {
		$lizenz_id = (int) ( isset( $_GET['lizenz'] ) ? $_GET['lizenz'] : 0 );
		$user_id   = (int) ( isset( $_GET['benutzer'] ) ? $_GET['benutzer'] : 0 );
		if ( ! current_user_can( 'manage_options' ) || ! wp_verify_nonce( isset( $_GET['_wpnonce'] ) ? $_GET['_wpnonce'] : '', 'epos_benutzer_loesen_' . $lizenz_id . '_' . $user_id ) ) {
			wp_die( 'Keine Berechtigung.' );
		}
		$erg = Epos_Lizenz_Cpt::benutzer_loesen( $lizenz_id, $user_id );
		if ( is_wp_error( $erg ) ) {
			wp_die( esc_html( $erg->get_error_message() ) );
		}
		wp_safe_redirect( get_edit_post_link( $lizenz_id, 'url' ) );
		exit;
	}

	public static function schluessel_hinweis() {
		$daten = get_transient( 'epos_schluessel_anzeige_' . get_current_user_id() );
		if ( ! $daten || ! is_array( $daten ) ) {
			return;
		}
		$screen = function_exists( 'get_current_screen' ) ? get_current_screen() : null;
		if ( ! $screen || 'epos_lizenz' !== $screen->post_type ) {
			return;
		}
		$lic_url = wp_nonce_url(
			admin_url( 'admin-post.php?action=epos_lic_download&lizenz=' . $daten['lizenz'] ),
			'epos_lic_download_' . $daten['lizenz']
		);
		$mail_url = wp_nonce_url(
			admin_url( 'admin-post.php?action=epos_lic_download&lizenz=' . $daten['lizenz'] . '&senden=1' ),
			'epos_lic_download_' . $daten['lizenz']
		);
		echo '<div class="notice notice-success"><p><strong>Neuer Lizenzschlüssel (einmalige Anzeige — jetzt sichern!):</strong></p>';
		printf( '<p><code style="font-size:1.3em;padding:6px 10px">%s</code></p>', esc_html( $daten['schluessel'] ) );
		printf(
			'<p><a href="%s" class="button">Als .lic-Datei herunterladen</a> &nbsp; <a href="%s" class="button">Per E-Mail an den ersten Firmen-Admin/Benutzer senden</a></p>',
			esc_url( $lic_url ),
			esc_url( $mail_url )
		);
		echo '<p>Nach 10 Minuten (oder nach Download/Versand) ist der Schlüssel hier nicht mehr abrufbar.</p></div>';
	}

	public static function aktion_lic_download() {
		$lizenz_id = (int) ( isset( $_GET['lizenz'] ) ? $_GET['lizenz'] : 0 );
		if ( ! current_user_can( 'manage_options' ) || ! wp_verify_nonce( isset( $_GET['_wpnonce'] ) ? $_GET['_wpnonce'] : '', 'epos_lic_download_' . $lizenz_id ) ) {
			wp_die( 'Keine Berechtigung.' );
		}
		$daten = get_transient( 'epos_schluessel_anzeige_' . get_current_user_id() );
		if ( ! $daten || (int) $daten['lizenz'] !== $lizenz_id ) {
			wp_die( 'Der Schlüssel ist nicht mehr verfügbar. Bitte einen neuen erzeugen.' );
		}

		$firma = get_post_meta( $lizenz_id, '_epos_firma', true );

		if ( ! empty( $_GET['senden'] ) ) {
			$empfaenger = null;
			foreach ( Epos_Lizenz_Cpt::benutzer( $lizenz_id, true ) as $u ) {
				if ( in_array( 'epos_firmenadmin', (array) $u->roles, true ) ) {
					$empfaenger = $u->user_email;
					break;
				}
				if ( ! $empfaenger ) {
					$empfaenger = $u->user_email;
				}
			}
			if ( ! $empfaenger ) {
				wp_die( 'Der Lizenz ist noch kein Benutzer zugeordnet — kein E-Mail-Empfänger vorhanden.' );
			}
			Epos_Mail::schluessel_senden( $empfaenger, $daten['schluessel'], $firma, false );
			delete_transient( 'epos_schluessel_anzeige_' . get_current_user_id() );
			wp_safe_redirect( get_edit_post_link( $lizenz_id, 'url' ) );
			exit;
		}

		$lic = Epos_Token::lic_datei( $daten['schluessel'], $firma, '' );
		if ( is_wp_error( $lic ) ) {
			wp_die( esc_html( $lic->get_error_message() ) );
		}
		delete_transient( 'epos_schluessel_anzeige_' . get_current_user_id() );

		nocache_headers();
		header( 'Content-Type: application/octet-stream' );
		header( 'Content-Disposition: attachment; filename="EPOS-Plan.lic"' );
		header( 'Content-Length: ' . strlen( $lic ) );
		echo $lic; // phpcs:ignore
		exit;
	}

	/* ------------------------------------------------- Einstellungen */

	public static function einstellungen_menue() {
		add_submenu_page(
			'edit.php?post_type=epos_lizenz',
			'EPOS-Lizenz Einstellungen',
			'Einstellungen',
			'manage_options',
			'epos-lizenz-einstellungen',
			array( __CLASS__, 'einstellungen_seite' )
		);
	}

	public static function einstellungen_seite() {
		if ( ! current_user_can( 'manage_options' ) ) {
			return;
		}

		if ( isset( $_POST['epos_opt_nonce'] ) && wp_verify_nonce( $_POST['epos_opt_nonce'], 'epos_optionen' ) ) {
			update_option( 'epos_lizenz_optionen', array(
				'token_tage'        => max( 7, (int) $_POST['token_tage'] ),
				'geraete_je_nutzer' => max( 1, (int) $_POST['geraete_je_nutzer'] ),
				'trial_tage'        => max( 1, (int) $_POST['trial_tage'] ),
				'kulanz_tage'       => max( 0, (int) $_POST['kulanz_tage'] ),
				'portal_seite'      => (int) $_POST['portal_seite'],
			) );
						$vertrag = Epos_Vertrag::optionen();
			foreach ( array( 'einzel', 'team', 'teamplus', 'campus', 'einzel3m', 'support' ) as $vk ) {
				$vertrag[ 'dok_' . $vk ]   = isset( $_POST[ 'epos_dok_' . $vk ] ) ? (int) $_POST[ 'epos_dok_' . $vk ] : 0;
				$vertrag[ 'stand_' . $vk ] = sanitize_text_field( isset( $_POST[ 'epos_stand_' . $vk ] ) ? $_POST[ 'epos_stand_' . $vk ] : '' );
			}
			update_option( Epos_Vertrag::OPTION, $vertrag );
			echo '<div class="notice notice-success"><p>Einstellungen gespeichert.</p></div>';
		}

		Epos_Token::schluesselpaar_sicherstellen();
		$oeffentlich = Epos_Token::oeffentlicher_schluessel_base64();
		$verzeichnis = Epos_Token::schluessel_verzeichnis();
		?>
		<div class="wrap">
			<h1>EPOS-Lizenz — Einstellungen</h1>
			<form method="post">
				<?php wp_nonce_field( 'epos_optionen', 'epos_opt_nonce' ); ?>
				<table class="form-table">
					<tr><th>Token-Gültigkeit (Offline-Leine)</th>
						<td><input type="number" name="token_tage" min="7" value="<?php echo esc_attr( epos_lizenz_option( 'token_tage' ) ); ?>"> Tage</td></tr>
					<tr><th>Geräte je Benutzer</th>
						<td><input type="number" name="geraete_je_nutzer" min="1" value="<?php echo esc_attr( epos_lizenz_option( 'geraete_je_nutzer' ) ); ?>"></td></tr>
					<tr><th>Laufzeit Demoversion</th>
						<td><input type="number" name="trial_tage" min="1" value="<?php echo esc_attr( epos_lizenz_option( 'trial_tage' ) ); ?>"> Tage</td></tr>
					<tr><th>Kulanzfenster nach Ablauf</th>
						<td><input type="number" name="kulanz_tage" min="0" value="<?php echo esc_attr( epos_lizenz_option( 'kulanz_tage' ) ); ?>"> Tage (wird im Token an die Anwendung übermittelt)</td></tr>
					<tr><th>Portal-Seite</th>
						<td><?php wp_dropdown_pages( array( 'name' => 'portal_seite', 'selected' => (int) epos_lizenz_option( 'portal_seite' ), 'show_option_none' => '— wählen —' ) ); ?>
						<p class="description">Seite mit dem Shortcode <code>[epos_lizenzportal]</code>.</p></td></tr>
				</table>
				
<h2>Online-Vertragsschluss — Vertragsdokumente</h2>
<p class="description">PDF-Fassungen aus der Mediathek (Anhangs-ID eintragen). Diese Fassungen werden im Checkout verlinkt, mit Stand-Datum und SHA-256-Hash protokolliert und der Bestellbestätigung als Anhang beigefügt (§ 312i Abs. 1 Nr. 4 BGB).</p>
<table class="form-table">
<?php
$epos_vo = Epos_Vertrag::optionen();
foreach ( array( 'einzel' => 'Lizenzvereinbarung Tarif „Einzel"', 'team' => 'Lizenzvereinbarung Tarif „Team"', 'teamplus' => 'Lizenzvereinbarung Tarif „Team Plus"', 'campus' => 'Lizenzvereinbarung Tarif Campus/Student', 'einzel3m' => 'Lizenzvereinbarung Tarif Einzel (3 Monate)', 'support' => 'Supportvertrag' ) as $epos_k => $epos_label ) :
	$epos_dok = Epos_Vertrag::dokument( $epos_k );
?>
<tr><th><?php echo esc_html( $epos_label ); ?></th>
<td>
Mediathek-ID: <input type="number" min="0" name="epos_dok_<?php echo esc_attr( $epos_k ); ?>" value="<?php echo esc_attr( $epos_vo[ 'dok_' . $epos_k ] ); ?>" style="width:90px">
&nbsp; Stand: <input type="text" name="epos_stand_<?php echo esc_attr( $epos_k ); ?>" value="<?php echo esc_attr( $epos_vo[ 'stand_' . $epos_k ] ); ?>" style="width:110px" placeholder="TT.MM.JJJJ">
<?php if ( $epos_dok ) : ?>
<p class="description"><a href="<?php echo esc_url( $epos_dok['url'] ); ?>" target="_blank"><?php echo esc_html( basename( $epos_dok['pfad'] ) ); ?></a> · SHA-256: <code><?php echo esc_html( substr( $epos_dok['hash'], 0, 20 ) ); ?>…</code></p>
<?php else : ?>
<p class="description" style="color:#d63638">Kein PDF hinterlegt — die zugehörige Checkout-Checkbox kann nicht angezeigt werden, Bestellungen dieses Tarifs sind blockiert.</p>
<?php endif; ?>
</td></tr>
<?php endforeach; ?>
</table>

<?php submit_button( 'Speichern' ); ?>
			</form>

			<h2>Signaturschlüssel</h2>
			<?php if ( $oeffentlich ) : ?>
				<p>Öffentlicher Schlüssel (Ed25519, Base64) — diesen Wert in die EPOS-Plan-Anwendung einkompilieren:</p>
				<p><textarea readonly rows="2" cols="70" onclick="this.select()"><?php echo esc_textarea( $oeffentlich ); ?></textarea></p>
				<p class="description">Ablage des Schlüsselpaars: <code><?php echo esc_html( $verzeichnis ); ?></code><br>
				Der private Schlüssel verlässt den Server nicht. Bitte extern sichern (z. B. verschlüsselter Datenträger) — bei Verlust können keine Lizenzen mehr ausgestellt werden.</p>
			<?php else : ?>
				<p style="color:#d63638">Es konnte kein Schlüsselpaar erzeugt werden (Verzeichnis nicht beschreibbar oder Sodium fehlt).</p>
			<?php endif; ?>

			<h2>REST-Endpunkte</h2>
			<p><code><?php echo esc_html( home_url( '/wp-json/epos/v1/' ) ); ?></code> — activate, validate, deactivate, trial (jeweils POST)</p>
		</div>
		<?php
	}
}
