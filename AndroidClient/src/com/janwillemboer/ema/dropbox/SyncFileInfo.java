package com.janwillemboer.ema.dropbox;

import java.io.File;
import java.util.Date;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import com.dropbox.client.DropboxAPI.Entry;

public class SyncFileInfo {

	private String mName;
	private JSONObject mSyncInfo;
	private JSONObject mSyncMetadata;

	private final static String JSON_NAME = "name";
	
	public SyncFileInfo(File f, JSONObject syncMetadata, File localDir) {
		mSyncMetadata = syncMetadata;
		mName = f.getName();

		// find the sync info of this file
		try {
			JSONArray files = mSyncMetadata.getJSONArray("files");
			for (int i = 0; i < files.length(); i++) {
				JSONObject file = files.getJSONObject(i);
				if (file.getString(JSON_NAME).equalsIgnoreCase(mName)) {
					mSyncInfo = file;
					break;
				}
			}
		} catch (JSONException e) {
		}

		setLocalFile(new File(localDir, mName));
	}

	private File mLocalFile;
	private Date mLocalChangeDate;
	private Date mSyncedWithLocalDate;
	private final static String SYNCED_WITH_LOCAL_DATE = "syncedWithLocalDate";

	private void setLocalFile(File value) {
		mLocalFile = value;
		if (value.exists()) {
			mLocalChangeDate = new Date(mLocalFile.lastModified());

			if (mSyncInfo != null) {
				mSyncedWithLocalDate = new Date(
						mSyncInfo.optLong(SYNCED_WITH_LOCAL_DATE));
			}
		}
	}

	private String mRemotePath;
	private Date mRemoteChangeDate;
	private Date mSyncedWithRemoteDate;
	private final static String SYNCED_WITH_REMOTE_DATE = "syncedWithRemoteDate";

	public void setRemoteFile(Entry value) {
		mRemotePath = value.path;

		mRemoteChangeDate = new Date(Date.parse(value.modified));

		if (mSyncInfo != null) {
			mSyncedWithRemoteDate = new Date(
					mSyncInfo.optLong(SYNCED_WITH_REMOTE_DATE));
		}
	}

	public void setSyncedWithRemoteDate(Date modified) {
		mSyncedWithRemoteDate = modified;
	}
	public void setSyncedWithRemoteDate(String modified) {
		mSyncedWithRemoteDate = new Date(Date.parse(modified));
	}
	public Date getRemoteChangeDate() {
		return mRemoteChangeDate;
	}

	public void setSyncedWithLocalDate(long modified) {
		mSyncedWithLocalDate = new Date(modified);
	}

	public JSONObject getJSON() {
		JSONObject json = new JSONObject();
		try {
			json.put(JSON_NAME, mName);
			json.put(SYNCED_WITH_LOCAL_DATE, mSyncedWithLocalDate.getTime());
			json.put(SYNCED_WITH_REMOTE_DATE,
					mSyncedWithRemoteDate.getTime());
		} catch (JSONException e) {
			throw new RuntimeException(e);
		}
		return json;
	}

	public String getName() {
		return mName;
	}

	public File getFile() {
		return mLocalFile;
	}

	public String getRemotePath() {
		return mRemotePath;
	}

	public boolean needsDownload() {
		if (mLocalFile == null || !mLocalFile.exists()) {
			return true;
		}
		if (mRemotePath == null || mRemotePath.length() == 0) {
			return false;
		}
		if (mSyncedWithRemoteDate == null) {
			return true;
		}
		return (mRemoteChangeDate.after(mSyncedWithRemoteDate));
	}

	public boolean needsUpload() {
		if (mRemotePath == null || mRemotePath.length() == 0) {
			return true;
		}
		if (mLocalFile == null || !mLocalFile.exists()) {
			return false;
		}

		if (mSyncedWithLocalDate == null) {
			return true;
		}

		return (mLocalChangeDate.after(mSyncedWithLocalDate));
	}
}

