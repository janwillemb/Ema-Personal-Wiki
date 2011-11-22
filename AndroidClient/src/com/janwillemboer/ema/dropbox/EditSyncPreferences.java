package com.janwillemboer.ema.dropbox;

import android.app.Activity;
import android.os.Bundle;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.CompoundButton;
import android.widget.Spinner;
import android.widget.Toast;

import com.janwillemboer.ema.EmaActivityHelper;
import com.janwillemboer.ema.R;

public class EditSyncPreferences extends Activity {

	private DropboxAuthentication mDropboxAuth;
	private EmaActivityHelper mHelper;
	private boolean mLoggedIn;
	private SyncPrefs mPreferences;
	private Button mSubmit;

	/** Called when the activity is first created. */
	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.dropbox_sync);

		mHelper = new EmaActivityHelper(this);
		mPreferences = new SyncPrefs(this);

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
					//logout
					setLoggedIn(false);
					mDropboxAuth.logout();
				} else {
					// Try to log in
					doLogin();
				}
			}
		});

		setLoggedIn(mDropboxAuth.getIsAuthenticated());
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
		mDropboxAuth.startAuthentication();
	}

	protected void onResume() {
		super.onResume();

		mDropboxAuth.authenticationFinished();
		setLoggedIn(mDropboxAuth.getIsAuthenticated());
	}

	public void setLoggedIn(boolean loggedIn) {
		mLoggedIn = loggedIn;
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