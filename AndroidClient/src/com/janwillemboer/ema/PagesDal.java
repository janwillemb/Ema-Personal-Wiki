package com.janwillemboer.ema;

import java.io.File;
import java.io.FilenameFilter;
import java.io.IOException;
import java.util.Collection;
import java.util.SortedMap;
import java.util.TreeMap;
import java.util.regex.Matcher;

import org.apache.commons.io.FileUtils;

import android.app.AlertDialog;
import android.content.Context;
import android.os.Environment;

public class PagesDal {

	private File mFilesDir;
	private Context mCtx;

	public PagesDal(Context ctx) throws IOException {
		mCtx = ctx;

		if (!Environment.getExternalStorageState().equals(
				Environment.MEDIA_MOUNTED)) {
			throw new IOException(mCtx.getText(R.string.storage_not_accessible)
					.toString());
		}

		mFilesDir = new File(Environment.getExternalStorageDirectory(),
				"PersonalWiki"); // mCtx.getExternalFilesDir(null);
		
		boolean isNewInstallation = !mFilesDir.exists();
		mFilesDir.mkdir();
		if (!mFilesDir.exists()) {
			throw new IOException(mCtx.getText(
					R.string.storage_dir_creation_failed).toString());
		}

		//createBackup("2", isNewInstallation);

		createAndReadCss();

		// set default content for default page
		WikiPage.defaultPageDefaultContent = ctx.getText(
				R.string.default_page_text).toString();
		WikiPage.aboutPageContent = ctx.getText(R.string.about_text).toString();
	}

	private void createBackup(String version, boolean isNewInstallation) throws IOException {
		File signalFile = new File(mFilesDir, "v" + version + ".flg");
		if (signalFile.exists()) {
			return;
		}

		if (isNewInstallation) {
			return;
		}
		
		File newDir = new File(mFilesDir.getAbsolutePath() + "-backup-v"
				+ version);
		if (newDir.exists()) {
			return;
		}

		newDir.mkdir();

		FileUtils.copyDirectory(mFilesDir, newDir);
		signalFile.createNewFile();
		
		AlertDialog ad = new AlertDialog.Builder(mCtx).create();
		ad.setTitle(R.string.v2_update_title);
		ad.setMessage(mCtx.getText(R.string.v2_update_text));
		ad.show();
	}

	public File Dir() {
		return mFilesDir;
	}

	private void createAndReadCss() {
		// create default stylesheet if not present
		File css = new File(mFilesDir, "style.css");

		if (!css.exists()) {
			try {
				FileStuff
						.writeContents(
								css,
								"body {\npadding-bottom: 50px; \n}\n.ema-task-finished {\n  text-decoration: line-through;\n}");
			} catch (IOException e) {
				// too bad, but not fatal
			}
		}

		if (css.exists()) {
			try {
				WikiPage.css = FileStuff.readContents(css);
			} catch (IOException e) {
				// too bad
			}
		}
	}

	public Collection<WikiPage> fetchAll() {
		File[] files = mFilesDir.listFiles(new FilenameFilter() {
			public boolean accept(File dir, String filename) {

				if (filename.toLowerCase().endsWith(WikiPage.EXT)) {
					File f = new File(dir, filename);
					if (f.length() > 0) {
						return true;
					}
				}

				return false;
			}
		});

		SortedMap<String, WikiPage> pages = new TreeMap<String, WikiPage>();

		for (File f : files) {
			WikiPage page = new WikiPage(f);
			pages.put(page.getName(), page);
		}
		return pages.values();
	}

	public WikiPage fetchByName(String pageName) {
		return new WikiPage(getFileForPage(pageName));
	}

	private File getFileForPage(String pageName) {
		pageName = pageName.replaceAll("[^\\w\\.\\-]", "_");
		return new File(mFilesDir, pageName + WikiPage.EXT);
	}

	/*
	 * create page. If it already exists, function will return false.
	 */
	public boolean createNewPage(String pageName) throws IOException {
		File newPageFile = getFileForPage(pageName);
		if (newPageFile.exists()) {
			return false;
		}

		newPageFile.createNewFile();
		return true;
	}

	public void savePage(String name, String body) throws IOException {
		File page = getFileForPage(name);

		FileStuff.writeContents(page, body);
	}

	public void toggleCheckbox(String pageTitle, int whichOne)
			throws IOException {
		WikiPage page = fetchByName(pageTitle);

		String body = page.getBody();

		StringBuffer sb = new StringBuffer();
		Matcher m = WikiPage.checkBoxes.matcher(body);
		int checkboxIx = 0;
		while (m.find()) {
			String replacement = m.group();
			if (checkboxIx == whichOne) {
				// found the checkbox that should be toggled
				replacement = replacement.replace("[ ]", "[!]")
						.replace("[x]", "[ ]").replace("[!]", "[x]");
			}

			m.appendReplacement(sb, replacement);
			checkboxIx++;
		}
		m.appendTail(sb);

		body = sb.toString();

		savePage(pageTitle, body);
	}

}
