package com.janwillemboer.ema.dropbox;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import android.os.Handler;
import android.os.Message;

import com.dropbox.client2.DropboxAPI.Entry;
import com.dropbox.client2.exception.DropboxException;
import com.janwillemboer.ema.EmaActivityHelper;
import com.janwillemboer.ema.R;
import com.janwillemboer.ema.WikiPage;

public class Sync {

	private final static String METADATA_FILE = "sync-metadata-v2.json";

	private EmaActivityHelper mHelper;
	private File mLocalDir;
	private Handler mProgressUpdateHandler;
	private DropboxWrapper mApi;
	private JSONObject mSyncMetadata;

	public Sync(EmaActivityHelper helper, DropboxWrapper api) {
		mApi = api;
		mHelper = helper;

		mLocalDir = mHelper.getDal().Dir();

		File syncMetadataFile = new File(mLocalDir, METADATA_FILE);

		if (!syncMetadataFile.exists()) {
			mSyncMetadata = new JSONObject();
			return;
		}

		BufferedReader br = null;
		try {
			br = new BufferedReader(new FileReader(syncMetadataFile), 4096);
			char[] buffer = new char[(int) syncMetadataFile.length()];
			br.read(buffer);

			mSyncMetadata = new JSONObject(new String(buffer));
		} catch (Exception e) {
			throw new RuntimeException(e);
		} finally {
			if (br != null) {
				try {
					br.close();
				} catch (Exception e) {
				}
			}
		}

	}

	public void setStatusHandler(Handler progressUpdate) {
		mProgressUpdateHandler = progressUpdate;
	}

	public boolean perform() throws IOException, DropboxException {
		Entry syncDir = mApi.getOrCreateFolder("/PersonalWiki");

		showProgress(1, 0);

		// create a list of all local files
		Map<String, SyncFileInfo> files = new HashMap<String, SyncFileInfo>();
		for (WikiPage p : mHelper.getDal().fetchAll()) {
			SyncFileInfo fi = new SyncFileInfo(p.getFile(), mSyncMetadata,
					mLocalDir);
			files.put(fi.getName(), fi);
		}

		if (syncDir.contents != null) {
			// add info for remote files
			for (Entry f : syncDir.contents) {
				if (f.isDir) {
					// skip dirs
					continue;
				}

				SyncFileInfo fi;
				if (files.containsKey(f.fileName())) {
					fi = files.get(f.fileName());
				} else {
					File newFile = new File(mLocalDir, f.fileName());
					fi = new SyncFileInfo(newFile, mSyncMetadata, mLocalDir);
					files.put(fi.getName(), fi);
				}
				fi.setRemoteFile(f);
			}
		}

		int totalFiles = files.size();
		int count = 0;

		boolean syncedSomething = false;
		// do sync for all files
		for (SyncFileInfo fi : files.values()) {
			count++;
			showProgress(totalFiles, count);

			if (fi.needsUpload()) {

				Entry syncedFile = mApi.putFile(syncDir.path, fi.getFile());

				// update local sync info
				fi.updatedRemote(syncedFile);
				syncedSomething = true;

			} else if (fi.needsDownload()) {

				mApi.getFile(syncDir.path, fi.getFile());

				fi.updatedLocal();

				syncedSomething = true;

			}
		}

		updateSyncInfo(files);
		return syncedSomething;
	}

	private void showProgress(int totalFiles, int count) {
		Message updateMessage = new Message();
		updateMessage.obj = mHelper.getContext()
				.getText(R.string.synchronizing).toString();
		updateMessage.arg1 = (int) ((((double) count) / ((double) totalFiles)) * 100);
		mProgressUpdateHandler.sendMessage(updateMessage);
	}

	private void updateSyncInfo(Map<String, SyncFileInfo> files)
			throws IOException {

		try {
			mSyncMetadata = new JSONObject();
			JSONArray filesArray = new JSONArray();

			for (SyncFileInfo file : files.values()) {
				filesArray.put(file.getJSON());
			}

			mSyncMetadata.put("files", filesArray);

			File syncMetadataFile = new File(mLocalDir, METADATA_FILE);
			BufferedWriter bw = null;
			try {
				bw = new BufferedWriter(new FileWriter(syncMetadataFile), 4096);
				bw.write(mSyncMetadata.toString(2));
			} finally {
				if (bw != null) {
					try {
						bw.close();
					} catch (Exception e) {
					}
				}
			}

		} catch (JSONException e) {
			throw new RuntimeException(e);
		}
	}
}