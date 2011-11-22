package com.janwillemboer.ema.dropbox;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.List;

import com.dropbox.client2.DropboxAPI;
import com.dropbox.client2.DropboxAPI.Entry;
import com.dropbox.client2.android.AndroidAuthSession;
import com.dropbox.client2.exception.DropboxException;

public class DropboxWrapper {

	private DropboxAPI<AndroidAuthSession> mAPI;

	public DropboxWrapper(DropboxAPI<AndroidAuthSession> api) {
		mAPI = api;
	}

	public Entry getOrCreateFolder(String name) throws DropboxException {
		Entry retval = mAPI.metadata(name, 0, null, true, null);
		if (retval.isDir) {
			return retval;
		}
		return mAPI.createFolder(name);
	}

	public Entry putFile(String path, File file) throws DropboxException {

		InputStream is = null;
		try {
			is = new BufferedInputStream(new FileInputStream(file));
			return mAPI.putFileOverwrite(path + "/" + file.getName(), is,
					file.length(), null);
		} catch (FileNotFoundException e) {
		} finally {
			if (is != null)
				try {
					is.close();
				} catch (IOException e) {
					// scheiss doch in die hosen mein freund
				}
		}
		return null;
	}

	public void getFile(String path, File file) throws DropboxException {

		OutputStream os = null;
		try {
			os = new BufferedOutputStream(new FileOutputStream(file));
			mAPI.getFile(path + "/" + file.getName(), null, os, null);
		} catch (FileNotFoundException e) {
			if (os != null) {
				try {
					os.close();
				} catch (IOException e1) {
					// poop
				}
			}

		}

	}

}
