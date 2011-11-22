package com.janwillemboer.ema.dropbox;

import android.content.Context;
import android.content.SharedPreferences;
import android.content.SharedPreferences.Editor;

import com.dropbox.client2.DropboxAPI;
import com.dropbox.client2.android.AndroidAuthSession;
import com.dropbox.client2.session.AccessTokenPair;
import com.dropbox.client2.session.AppKeyPair;
import com.dropbox.client2.session.Session.AccessType;

public class DropboxAuthentication {

	final static private String APP_KEY = "l8tliwhtfvkrxl7"; 
	final static private String APP_SECRET = "lfh5rpahsdhrqbp"; 
	final static private AccessType ACCESS_TYPE = AccessType.DROPBOX;

	final static public String ACCOUNT_PREFS_NAME = "Ema.DropboxAccount";
	final static public String ACCESS_KEY_NAME = "Ema.Dropbox.Key";
	final static public String ACCESS_SECRET_NAME = "Ema.Dropbox.Secret";

	private DropboxAPI<AndroidAuthSession> mDBApi;
	private Context mContext;
	private boolean mIsAuthenticated;

	public DropboxAuthentication(Context ctx) {
		mContext = ctx;
		
		initialize();
	}
	
	private void initialize() { 
		AppKeyPair appKeys = new AppKeyPair(APP_KEY, APP_SECRET);
		AccessTokenPair access = getStoredKeys();
		
		AndroidAuthSession session;
		if (access != null) {
			session = new AndroidAuthSession(appKeys, ACCESS_TYPE, access);
		} else {
			session = new AndroidAuthSession(appKeys, ACCESS_TYPE);
		}
		
		mDBApi = new DropboxAPI<AndroidAuthSession>(session);
		mIsAuthenticated = mDBApi.getSession().isLinked();
	}

	public void reInitialize() {
		initialize();
	}
	
	// start authentication; will return to onResume on the context
	public void startAuthentication() {
		mDBApi.getSession().startAuthentication(mContext);
	}

	public DropboxWrapper getAPI() {
		return new DropboxWrapper(mDBApi);
	}

	private AccessTokenPair getStoredKeys() {
		SharedPreferences prefs = mContext.getSharedPreferences(
				ACCOUNT_PREFS_NAME, 0);
		String key = prefs.getString(ACCESS_KEY_NAME, null);
		String secret = prefs.getString(ACCESS_SECRET_NAME, null);
		if (key != null && secret != null) {
			return new AccessTokenPair(key, secret);
		} else {
			return null;
		}
	}

	private void storeKeys(String key, String secret) {
		// Save the access key for later
		SharedPreferences prefs = mContext.getSharedPreferences(
				ACCOUNT_PREFS_NAME, 0);
		Editor edit = prefs.edit();
		edit.putString(ACCESS_KEY_NAME, key);
		edit.putString(ACCESS_SECRET_NAME, secret);
		edit.commit();
	}

	public void logout() {
		clearKeys();
		mIsAuthenticated = false;
		mDBApi.getSession().unlink();
	}

	private void clearKeys() {
		SharedPreferences prefs = mContext.getSharedPreferences(
				ACCOUNT_PREFS_NAME, 0);
		Editor edit = prefs.edit();
		edit.clear();
		edit.commit();
	}

	public boolean getIsAuthenticated() {
		return mIsAuthenticated;
	}

	public void authenticationFinished() {
		mIsAuthenticated = false;

		if (mDBApi.getSession().authenticationSuccessful()) {
			try {
				// MANDATORY call to complete auth.
				// Sets the access token on the session
				mDBApi.getSession().finishAuthentication();

				AccessTokenPair tokens = mDBApi.getSession().getAccessTokenPair();

				mIsAuthenticated = true;
				storeKeys(tokens.key, tokens.secret);

			} catch (IllegalStateException e) {
				mIsAuthenticated = false;
			}
		}

	}

}
