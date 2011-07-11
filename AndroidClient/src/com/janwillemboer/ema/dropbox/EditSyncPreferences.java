/*
 * Copyright (c) 2010 Evenflow, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 * 
 * I modified the POOPY CRAPPY code to be better.
 */

package com.janwillemboer.ema.dropbox;

import android.app.Activity;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.Spinner;
import android.widget.Toast;

import com.janwillemboer.ema.EmaActivityHelper;
import com.janwillemboer.ema.R;

public class EditSyncPreferences extends Activity {

	private DropboxAuthentication mDropboxAuth;
	private EmaActivityHelper mHelper;
	private boolean mLoggedIn;
	private EditText mLoginEmail;
	private EditText mLoginPassword;
	private SyncPrefs mPreferences;
	private Button mSubmit;

	/** Called when the activity is first created. */
	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.dropbox_sync);

		mHelper = new EmaActivityHelper(this);
		mPreferences = new SyncPrefs(this);

		mLoginEmail = mHelper.find(R.id.login_email);
		mLoginPassword = mHelper.find(R.id.login_password);
		mSubmit = mHelper.find(R.id.login_submit);

		initializeBooleans();
		initializeSpinner();
		initializeLoginControls();
	}

	private void initializeBooleans() {
		CheckBox cb;

		cb = mHelper.find(R.id.after_edit);
		cb.setChecked(mPreferences.getAfterEdit());
		cb.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
			public void onCheckedChanged(CompoundButton buttonView,
					boolean isChecked) {
				mPreferences.setAfterEdit(isChecked);
			}
		});

		cb = mHelper.find(R.id.when_starting_app);
		cb.setChecked(mPreferences.getOnStartup());
		cb.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
			public void onCheckedChanged(CompoundButton buttonView,
					boolean isChecked) {
				mPreferences.setOnStartup(isChecked);
			}
		});

		cb = mHelper.find(R.id.periodically);
		cb.setChecked(mPreferences.getPeriodically());
		cb.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
			public void onCheckedChanged(CompoundButton buttonView,
					boolean isChecked) {
				mPreferences.setPeriodically(isChecked);
			}
		});
	}

	private void initializeLoginControls() {
		mDropboxAuth = new DropboxAuthentication(this);

		mSubmit.setOnClickListener(new OnClickListener() {
			public void onClick(View v) {
				if (mLoggedIn) {
					// We're going to log out
					mDropboxAuth.logout();
					setLoggedIn(false);
				} else {
					// Try to log in
					doLogin();
				}
			}
		});

		DropboxAuthentication.LoginResult loginResult = mDropboxAuth
				.checkLoginToken();
		if (loginResult.getSucceeded()) {
			setLoggedIn(true);
		} else {
			setLoggedIn(false);
		}
	}

	private void initializeSpinner() {
		final Integer[] items = new Integer[] { 1, 2, 5, 10, 15, 20, 30, 45, 60 };
		Spinner spinner = mHelper.find(R.id.minutes_spinner);
		ArrayAdapter<Integer> adapter = new ArrayAdapter<Integer>(this,
				android.R.layout.simple_spinner_item, items);
		adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
		spinner.setAdapter(adapter);

		// find position of setting in items
		final int minutes = mPreferences.getIntervalMinutes();
		int position = 3; // 10
		for (int ix = 0; ix < items.length; ix++) {
			if (items[ix] == minutes) {
				position = ix;
				break;
			}
		}
		spinner.setSelection(position);

		spinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {

			public void onItemSelected(AdapterView<?> arg0, View arg1,
					int arg2, long arg3) {
				mPreferences.setIntervalMinutes(items[arg2]);
			}

			public void onNothingSelected(AdapterView<?> arg0) {
			}
		});
	}

	private void doLogin() {
		final Handler resultHandler = new Handler() {
			@Override
			public void handleMessage(Message msg) {
				DropboxAuthentication.LoginResult result = (DropboxAuthentication.LoginResult) msg.obj;
				setLoggedIn(result.getSucceeded());
				if (result.getSucceeded()) {
					mHelper.showToast(R.string.login_success);
					finish();
				} else {
					mHelper.showToast(result.getErrorMessage());
				}

			}
		};

		Thread t = new Thread(new Runnable() {
			public void run() {
				String email = mLoginEmail.getText().toString();
				if (email.length() < 5 || email.indexOf("@") < 0
						|| email.indexOf(".") < 0) {
					mHelper.showToast(R.string.enter_valid_email);
					return;
				}

				String password = mLoginPassword.getText().toString();
				if (password.length() == 0) {
					mHelper.showToast(R.string.enter_password);
					return;
				}

				DropboxAuthentication.LoginResult result = mDropboxAuth.login(
						email, password);

				Message msg = new Message();
				msg.obj = result;
				resultHandler.sendMessage(msg);
			}
		});
		t.start();
	}

	public void setLoggedIn(boolean loggedIn) {
		mLoggedIn = loggedIn;
		mLoginEmail.setEnabled(!loggedIn);
		mLoginPassword.setEnabled(!loggedIn);
		if (loggedIn) {
			mSubmit.setText(getText(R.string.do_dropbox_logout));
		} else {
			mSubmit.setText(getText(R.string.do_dropbox_login));
		}
	}

	public void showToast(String msg) {
		Toast message = Toast.makeText(this, msg, Toast.LENGTH_LONG);
		message.show();
	}

}