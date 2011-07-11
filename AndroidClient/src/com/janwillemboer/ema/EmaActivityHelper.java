package com.janwillemboer.ema;

import java.io.IOException;

import android.app.Activity;
import android.content.Context;
import android.os.Handler;
import android.os.Message;
import android.view.View;
import android.widget.Toast;

public class EmaActivityHelper {

	private PagesDal mDal;
	private Context mCtx;
	private Handler mToastHandler;

	public EmaActivityHelper(Context ctx) {
		mCtx = ctx;
		
		mToastHandler = new Handler() {
			@Override
			public void handleMessage(Message msg) {
				CharSequence cs = (CharSequence) msg.obj;
				Toast.makeText(mCtx, cs, Toast.LENGTH_LONG).show();
			}
		};

		try {
			mDal = new PagesDal(ctx);
		} catch (IOException e) {
			showToast("Error initializing Ema Personal Wiki", e);
		}

	}

	public boolean isInitialized() {
		return mDal != null;
	}

	public PagesDal getDal() {
		return mDal;
	}

	public void showToast(int r) {
		showToast(mCtx.getText(r));
	}
	
	@SuppressWarnings("unchecked")
	public <T extends View> T find(int id) {
		return (T) ((Activity)mCtx).findViewById(id);
	}

	public void showToast(CharSequence message) {
		Message msg = new Message();
		msg.obj = message;
		mToastHandler.sendMessage(msg);
	}

	public void showToast(CharSequence message, Exception ex) {
		if (ex.getLocalizedMessage() == null) {
			showToast(message);
		}
		else {
			showToast(message + " \n" + ex.getLocalizedMessage());
		}
	}

	public Context getContext() {
		return mCtx;
	}
}
