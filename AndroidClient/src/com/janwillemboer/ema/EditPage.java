package com.janwillemboer.ema;

import java.io.IOException;
import java.util.Timer;
import java.util.TimerTask;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

public class EditPage extends Activity {

	public final static String TITLE_KEY = "EditPage.Title";
	private final static int SAVE_INTERVAL_SECONDS = 10;

	private EmaActivityHelper  mHelper;
	private String mPageTitle;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.edit_page);
		
		mHelper = new EmaActivityHelper(this);

		// try to populate from saved state
		if (!populate(savedInstanceState)) {
			// no saved state, populate from intent extras
			Intent i = getIntent();
			if (i != null) {
				populate(i.getExtras());
			}
		}

		Button saveButton = (Button) findViewById(R.id.save);
		saveButton.setOnClickListener(new View.OnClickListener() {
			public void onClick(View v) {
				okResult();
			}
		});

	}

	@Override
	protected void onStart() {
		super.onStart();
		startTimer();
	}
	
	@Override
	protected void onStop() {
		super.onStop();
		mAutoSaveTimer.cancel();
	}
	
	Timer mAutoSaveTimer;
	/*
	 * start timer that saves the note periodically
	 */
	private void startTimer() {
		mAutoSaveTimer = new Timer();
		mAutoSaveTimer.scheduleAtFixedRate(new TimerTask() {
			
			@Override
			public void run() {
				save();
			}
		}, 1000 * SAVE_INTERVAL_SECONDS, 1000 * SAVE_INTERVAL_SECONDS);
	}

	private boolean populate(Bundle b) {
		if (b == null) {
			return false;
		}

		mPageTitle = b.getString(TITLE_KEY);
		if (mPageTitle == null || mPageTitle.length() == 0) {
			return false;
		}
		return populate();
	}

	private boolean populate() {
		String pageBody;

		WikiPage page = mHelper.getDal().fetchByName(mPageTitle);
		try {
			pageBody = page.getBody();
		} catch (IOException e) {
			Toast.makeText(
					this,
					getText(R.string.page_error) + "\n"
							+ e.getLocalizedMessage(), Toast.LENGTH_LONG);
			return false;
		}

		TextView title = (TextView) findViewById(R.id.page_title);
		EditText body = (EditText) findViewById(R.id.page_body);

		title.setText(mPageTitle);
		if (pageBody != null) {
			body.setText(pageBody);
		}

		return true;
	}

	private void okResult() {
		save();
		setResult(RESULT_OK);
		finish();
	}
	
	@Override
	public void onBackPressed() {
		okResult();
	}

	private synchronized void save() {
		EditText body = (EditText) findViewById(R.id.page_body);

		try {
			mHelper.getDal().savePage(mPageTitle, body.getText().toString());
		} catch (IOException e) {
			Toast.makeText(
					this,
					getText(R.string.save_error) + "\n"
							+ e.getLocalizedMessage(), Toast.LENGTH_LONG);
		}
	}
	

	@Override
	protected void onPause() {
		save();
		super.onPause();
	}

	@Override
	protected void onResume() {
		super.onResume();
		populate();
	}

	@Override
	protected void onSaveInstanceState(Bundle outState) {
		save();
		outState.putString(TITLE_KEY, mPageTitle);
		super.onSaveInstanceState(outState);
	}

	@Override
	protected void onRestoreInstanceState(Bundle savedInstanceState) {
		super.onRestoreInstanceState(savedInstanceState);
		if (!populate(savedInstanceState)) {
			finish();
		}
	}
	
	

}
