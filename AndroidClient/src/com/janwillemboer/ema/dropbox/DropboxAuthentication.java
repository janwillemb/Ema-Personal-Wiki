package com.janwillemboer.ema.dropbox;

import android.content.Context;
import android.content.SharedPreferences;
import android.content.SharedPreferences.Editor;

import com.dropbox.client.Authenticator;
import com.dropbox.client.DropboxAPI;
import com.dropbox.client.DropboxAPI.Config;
import com.dropbox.client.DropboxClient;

public class DropboxAuthentication {

	final static private String CONSUMER_KEY = "l8tliwhtfvkrxl7";
	final static private String CONSUMER_SECRET = "lfh5rpahsdhrqbp";
	
	final static public String ACCOUNT_PREFS_NAME = "Ema.DropboxAccount";
	final static public String ACCESS_KEY_NAME = "Ema.Dropbox.Key";
	final static public String ACCESS_SECRET_NAME = "Ema.Dropbox.Secret";

	private DropboxAPI mApi = new DropboxAPI();
	private Config mConfig;
	private Context mContext;

	public DropboxAuthentication(Context ctx) {
		mContext = ctx;
	}

	/*
	 * log in with username and password if there are no saved credentials.
	 */
	public LoginResult login(String user, String pass) {
		try {
			mConfig = mApi.authenticate(getConfig(), user, pass);
			if (mConfig.authStatus == DropboxAPI.STATUS_SUCCESS) {
				storeKeys(mConfig.accessTokenKey, mConfig.accessTokenSecret);
				return new LoginResult(true);
			}
			return new LoginResult(false, "Invalid username or password.");
		} catch (Exception e) {
			return new LoginResult(false, e.getLocalizedMessage());
		}
	}
	
	public void logout() {
		mApi.deauthenticate();
		clearKeys();
	}

	public class LoginResult {
		private boolean mSucceeded;
		private String mErrorMessage;

		public LoginResult(boolean succeeded)  {
			this(succeeded, "");
		}

		public LoginResult(boolean succeeded, String errorMessage) {
			mSucceeded = succeeded;
			mErrorMessage = errorMessage;
		}

		public boolean getSucceeded() {
			return mSucceeded;
		}

		public String getErrorMessage() {
			return mErrorMessage;
		}
	}

	/*
	 * try to log in with saved credentials. If it fails, user needs to
	 * log in with his/her dropbox account first. 
	 */
	public LoginResult checkLoginToken() {
		try {
			String keys[] = getKeys();
			if (keys != null) {
				mConfig = mApi.authenticateToken(keys[0], keys[1], getConfig());
				if (mConfig != null) {
					return new LoginResult(true);
				}
				clearKeys();
				return new LoginResult(false,
						"Saved authentication is outdated.");
			}
			return new LoginResult(false);
		} catch (Exception e) {
			return new LoginResult(false, e.getLocalizedMessage());
		}
	}

	/*
	 * API to use after authentication.
	 */
	public DropboxAPI getDropboxApi() {
		return mApi;
	}
	
	public DropboxClient getDropboxClient() {
		try {
			return new DropboxClient(mConfig.toMap(), new Authenticator(mConfig.toMap()));
		} catch (Exception e) {
			throw new RuntimeException(e.getLocalizedMessage());
		}
	}
	
	private Config getConfig() {
		if (mConfig == null) {
			mConfig = mApi.getConfig(null, false);
			mConfig.consumerKey = CONSUMER_KEY;
			mConfig.consumerSecret = CONSUMER_SECRET;
			mConfig.server = "api.dropbox.com";
			mConfig.contentServer = "api-content.dropbox.com";
			mConfig.port = 80;
		}
		return mConfig;
	}

	private String[] getKeys() {
		SharedPreferences prefs = mContext.getSharedPreferences(
				ACCOUNT_PREFS_NAME, 0);
		String key = prefs.getString(ACCESS_KEY_NAME, null);
		String secret = prefs.getString(ACCESS_SECRET_NAME, null);
		if (key != null && secret != null) {
			String[] ret = new String[2];
			ret[0] = key;
			ret[1] = secret;
			return ret;
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

	private void clearKeys() {
		SharedPreferences prefs = mContext.getSharedPreferences(
				ACCOUNT_PREFS_NAME, 0);
		Editor edit = prefs.edit();
		edit.clear();
		edit.commit();
	}

}
