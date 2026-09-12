<?php
/**
 * Custom Post Type "epos_lizenz" + Zugriffsfunktionen auf Lizenzdaten.
 *
 * Meta-Felder:
 *   _epos_typ            demo | person | firma
 *   _epos_firma          Firmenname (bei Demo/Person: Name des Inhabers)
 *   _epos_max_benutzer   int (Demo/Person: 1)
 *   _epos_gueltig_ab     Y-m-d
 *   _epos_gueltig_bis    Y-m-d
 *   _epos_status         aktiv | gesperrt
 *   _epos_edition        frei (z. B. "standard")
 *   _epos_schluessel_hash  password_hash des Geheimteils
 *   _epos_schluessel_erzeugt  Zeitstempel der letzten Schlüsselerzeugung
 *
 * Benutzerzuordnung über User-Meta:
 *   _epos_lizenz_id      Post-ID der Lizenz
 *   _epos_status         aktiv | deaktiviert
 *   _epos_deaktiviert_am Zeitstempel (für die Wechsel-Sperrfrist)
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Lizenz_Cpt {

	const CPT = 'epos_lizenz';

	public static function registrieren() {
		register_post_type( self::CPT, array(
			'labels' => array(
				'name'          => 'EPOS-Lizenzen',
				'singular_name' => 'EPOS-Lizenz',
				'add_new'       => 'Neue Lizenz',
				'add_new_item'  => 'Neue Lizenz anlegen',
				'edit_item'     => 'Lizenz bearbeiten',
				'search_items'  => 'Lizenzen durchsuchen',
			),
			'public'              => false,
			'show_ui'             => true,
			'show_in_menu'        => true,
			'menu_icon'           => 'dashicons-lock',
			'menu_position'       => 58,
			'supports'            => array( 'title' ),
			'capability_type'     => 'post',
			'capabilities'        => array( 'create_posts' => 'manage_options' ),
			'map_meta_cap'        => true,
			'exclude_from_search' => true,
			'show_in_rest'        => false,
		) );
	}

	/** Lizenz anhand der Nummer (= Post-ID) laden. */
	public static function lade( $nummer ) {
		$post = get_post( (int) $nummer );
		if ( ! $post || self::CPT !== $post->post_type || 'trash' === $post->post_status ) {
			return null;
		}
		return $post;
	}

	/** Gültigkeitsprüfung des Lizenzdatensatzes (nicht des Schlüssels). */
	public static function status_pruefen( $lizenz ) {
		if ( 'gesperrt' === get_post_meta( $lizenz->ID, '_epos_status', true ) ) {
			return new WP_Error( 'lizenz_gesperrt', 'Diese Lizenz ist gesperrt. Bitte wenden Sie sich an den Support.' );
		}
		$bis = (string) get_post_meta( $lizenz->ID, '_epos_gueltig_bis', true );
		if ( $bis && strtotime( $bis . ' 23:59:59 UTC' ) < time() ) {
			return new WP_Error( 'lizenz_abgelaufen', 'Diese Lizenz ist am ' . $bis . ' abgelaufen.' );
		}
		$ab = (string) get_post_meta( $lizenz->ID, '_epos_gueltig_ab', true );
		if ( $ab && strtotime( $ab . ' 00:00:00 UTC' ) > time() ) {
			return new WP_Error( 'lizenz_zukunft', 'Diese Lizenz ist erst ab dem ' . $ab . ' gültig.' );
		}
		return true;
	}

	/** Alle der Lizenz zugeordneten Benutzer. */
	public static function benutzer( $lizenz_id, $nur_aktive = false ) {
		$args = array(
			'meta_key'   => '_epos_lizenz_id',
			'meta_value' => (int) $lizenz_id,
			'number'     => 500,
		);
		$alle = get_users( $args );
		if ( ! $nur_aktive ) {
			return $alle;
		}
		return array_values( array_filter( $alle, function ( $u ) {
			return 'deaktiviert' !== get_user_meta( $u->ID, '_epos_status', true );
		} ) );
	}

	/** Aktiven Benutzer der Lizenz anhand der E-Mail finden. */
	public static function benutzer_nach_email( $lizenz_id, $email ) {
		$user = get_user_by( 'email', $email );
		if ( ! $user ) {
			return null;
		}
		if ( (int) get_user_meta( $user->ID, '_epos_lizenz_id', true ) !== (int) $lizenz_id ) {
			return null;
		}
		return $user;
	}

	/**
	 * Benutzer der Lizenz zuordnen; legt das WP-Konto bei Bedarf an.
	 *
	 * @return WP_User|WP_Error
	 */
	public static function benutzer_zuordnen( $lizenz_id, $email, $rolle = 'epos_benutzer' ) {
		$email = sanitize_email( $email );
		if ( ! is_email( $email ) ) {
			return new WP_Error( 'email', 'Ungültige E-Mail-Adresse.' );
		}

		$max    = max( 1, (int) get_post_meta( $lizenz_id, '_epos_max_benutzer', true ) );
		$aktive = self::benutzer( $lizenz_id, true );

		$user = get_user_by( 'email', $email );

		if ( $user && (int) get_user_meta( $user->ID, '_epos_lizenz_id', true ) === (int) $lizenz_id ) {
			if ( 'deaktiviert' === get_user_meta( $user->ID, '_epos_status', true ) ) {
				if ( count( $aktive ) >= $max ) {
					return new WP_Error( 'kontingent', sprintf( 'Alle %d Benutzerplätze dieser Lizenz sind belegt.', $max ) );
				}
				update_user_meta( $user->ID, '_epos_status', 'aktiv' );
			}
			return $user;
		}

		if ( $user && get_user_meta( $user->ID, '_epos_lizenz_id', true ) ) {
			$andere_id        = (int) get_user_meta( $user->ID, '_epos_lizenz_id', true );
			$andere           = get_post( $andere_id );
			$verwaist         = ( ! $andere || 'epos_lizenz' !== $andere->post_type || 'trash' === $andere->post_status );
			$dort_deaktiviert = ( 'deaktiviert' === get_user_meta( $user->ID, '_epos_status', true ) );
			if ( ! $verwaist && ! $dort_deaktiviert ) {
				return new WP_Error( 'fremde_lizenz', 'Diese E-Mail-Adresse ist bereits einer anderen Lizenz aktiv zugeordnet. Bitte zuerst dort deaktivieren oder von der Lizenz lösen lassen.' );
			}
			// Alte Bindung lösen: Lizenz existiert nicht mehr oder Benutzer ist dort deaktiviert.
			Epos_Geraete::deaktiviere_benutzer( $andere_id, $user->ID );
			delete_user_meta( $user->ID, '_epos_lizenz_id' );
			delete_user_meta( $user->ID, '_epos_deaktiviert_am' );
		}

		if ( count( $aktive ) >= $max ) {
			return new WP_Error( 'kontingent', sprintf( 'Alle %d Benutzerplätze dieser Lizenz sind belegt.', $max ) );
		}

		if ( ! $user ) {
			$user_id = wp_insert_user( array(
				'user_login' => self::freier_loginname( $email ),
				'user_email' => $email,
				'user_pass'  => wp_generate_password( 24 ),
				'role'       => $rolle,
			) );
			if ( is_wp_error( $user_id ) ) {
				return $user_id;
			}
			$user = get_user_by( 'id', $user_id );
			// E-Mail zum Setzen des Passworts (für das Portal-Login)
			wp_new_user_notification( $user_id, null, 'user' );
		} else {
			$user->add_role( $rolle );
		}

		update_user_meta( $user->ID, '_epos_lizenz_id', (int) $lizenz_id );
		update_user_meta( $user->ID, '_epos_status', 'aktiv' );

		return $user;
	}

	/** Benutzer deaktivieren (Platz wird nach Sperrfrist wieder frei nutzbar). */
	public static function benutzer_deaktivieren( $lizenz_id, $user_id ) {
		if ( (int) get_user_meta( $user_id, '_epos_lizenz_id', true ) !== (int) $lizenz_id ) {
			return new WP_Error( 'zuordnung', 'Der Benutzer gehört nicht zu dieser Lizenz.' );
		}
		update_user_meta( $user_id, '_epos_status', 'deaktiviert' );
		update_user_meta( $user_id, '_epos_deaktiviert_am', time() );
		Epos_Geraete::deaktiviere_benutzer( $lizenz_id, $user_id );
		return true;
	}

	/** Benutzer vollständig von der Lizenz lösen — die E-Mail wird für andere Lizenzen frei. */
	public static function benutzer_loesen( $lizenz_id, $user_id ) {
		if ( (int) get_user_meta( $user_id, '_epos_lizenz_id', true ) !== (int) $lizenz_id ) {
			return new WP_Error( 'zuordnung', 'Der Benutzer gehört nicht zu dieser Lizenz.' );
		}
		Epos_Geraete::deaktiviere_benutzer( $lizenz_id, $user_id );
		delete_user_meta( $user_id, '_epos_lizenz_id' );
		delete_user_meta( $user_id, '_epos_status' );
		delete_user_meta( $user_id, '_epos_deaktiviert_am' );
		return true;
	}

	/** Sperrfrist: frühester Zeitpunkt, zu dem der Platz neu vergeben werden darf. */
	public static function sperrfrist_aktiv( $lizenz_id ) {
		$benutzer = self::benutzer( $lizenz_id );
		$frist    = 7 * DAY_IN_SECONDS;
		foreach ( $benutzer as $u ) {
			if ( 'deaktiviert' === get_user_meta( $u->ID, '_epos_status', true ) ) {
				$wann = (int) get_user_meta( $u->ID, '_epos_deaktiviert_am', true );
				if ( $wann && ( time() - $wann ) < $frist ) {
					return gmdate( 'd.m.Y H:i', $wann + $frist );
				}
			}
		}
		return false;
	}

	/**
	 * Neue Lizenz anlegen.
	 *
	 * @return int|WP_Error Post-ID
	 */
	public static function anlegen( $typ, $firma, $max_benutzer, $gueltig_ab, $gueltig_bis, $edition = 'standard' ) {
		if ( ! isset( Epos_Schluessel::TYPEN[ $typ ] ) ) {
			return new WP_Error( 'typ', 'Unbekannter Lizenztyp.' );
		}
		$post_id = wp_insert_post( array(
			'post_type'   => self::CPT,
			'post_status' => 'publish',
			'post_title'  => $firma,
		), true );
		if ( is_wp_error( $post_id ) ) {
			return $post_id;
		}
		update_post_meta( $post_id, '_epos_typ', $typ );
		update_post_meta( $post_id, '_epos_firma', sanitize_text_field( $firma ) );
		update_post_meta( $post_id, '_epos_max_benutzer', ( 'firma' === $typ ) ? max( 1, (int) $max_benutzer ) : 1 );
		update_post_meta( $post_id, '_epos_gueltig_ab', $gueltig_ab );
		update_post_meta( $post_id, '_epos_gueltig_bis', $gueltig_bis );
		update_post_meta( $post_id, '_epos_status', 'aktiv' );
		update_post_meta( $post_id, '_epos_edition', sanitize_text_field( $edition ) );
		return $post_id;
	}

	/**
	 * Neuen Lizenzschlüssel erzeugen; alter wird ungültig.
	 *
	 * @return array{schluessel:string}|WP_Error
	 */
	public static function schluessel_neu( $lizenz_id ) {
		$lizenz = self::lade( $lizenz_id );
		if ( ! $lizenz ) {
			return new WP_Error( 'lizenz', 'Lizenz nicht gefunden.' );
		}

		// Seit 1.4.1: Nur eine veröffentlichte Lizenz mit gespeichertem Typ bekommt
		// einen Schlüssel. Auf einem noch nicht gespeicherten Entwurf (wp-admin
		// „Neue Lizenz anlegen“, dann sofort „Neuen Schlüssel erzeugen“) fehlen Typ,
		// Firma und Laufzeit: Der Schlüssel bekäme eine leere Typkennung und wäre bei
		// der Aktivierung unbrauchbar, und die Lizenz bliebe als Entwurf unsichtbar.
		if ( 'publish' !== $lizenz->post_status ) {
			return new WP_Error( 'lizenz_entwurf', 'Die Lizenz ist noch nicht veröffentlicht. Bitte zuerst die Lizenzdaten ausfüllen und „Veröffentlichen“ klicken, danach den Schlüssel erzeugen.' );
		}
		$typ = (string) get_post_meta( $lizenz_id, '_epos_typ', true );
		if ( ! isset( Epos_Schluessel::TYPEN[ $typ ] ) ) {
			return new WP_Error( 'typ', 'Der Lizenztyp fehlt. Bitte die Lizenz mit Typ (Demo, Person oder Firma) speichern, danach den Schlüssel erzeugen.' );
		}

		// Rate-Limit: max. 3 Neuerzeugungen je Lizenz und Tag
		$zaehler_key = 'epos_schluessel_limit_' . $lizenz_id;
		$zaehler     = (int) get_transient( $zaehler_key );
		if ( $zaehler >= 3 ) {
			return new WP_Error( 'limit', 'Es wurden heute bereits 3 neue Schlüssel erzeugt. Bitte versuchen Sie es morgen erneut.' );
		}
		$neu = Epos_Schluessel::erzeugen( $typ, $lizenz_id );
		if ( is_wp_error( $neu ) ) {
			return $neu;
		}
		set_transient( $zaehler_key, $zaehler + 1, DAY_IN_SECONDS );

		update_post_meta( $lizenz_id, '_epos_schluessel_hash', $neu['hash'] );
		update_post_meta( $lizenz_id, '_epos_schluessel_erzeugt', time() );

		return array( 'schluessel' => $neu['schluessel'] );
	}

	private static function freier_loginname( $email ) {
		$basis = sanitize_user( strstr( $email, '@', true ), true );
		if ( '' === $basis ) {
			$basis = 'epos';
		}
		$login = $basis;
		$i     = 1;
		while ( username_exists( $login ) ) {
			$login = $basis . $i;
			$i++;
		}
		return $login;
	}
}
