package com.janwillemboer.ema;

import java.io.IOException;
import java.util.Stack;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.View;
import android.view.Window;
import android.webkit.MimeTypeMap;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.ProgressBar;
import android.widget.TextView;

import com.janwillemboer.ema.dropbox.DropboxAuthentication;
import com.janwillemboer.ema.dropbox.Sync;
import com.janwillemboer.ema.dropbox.SyncPrefs;

public class ViewPage extends Activity {

	public static final String TAG = "ViewPage";

	public static final String TITLE_KEY = "ViewPage.Title";
	public static final String SCROLLPOS_KEY = "ViewPage.ScrollPos";

	private static final int EDIT_PAGE = 0;
	private static final int LIST_PAGES = 1;
	private static final int LOGIN = 2;

	private EmaActivityHelper mHelper;
	private int mActiveWebview = 0;
	private boolean mHasCustomTitle;
	private boolean mIgnoreBrowserUpdate;
	private String mCustomWindowTitle;
	private Stack<ViewPageCommand> mHistory;
	private boolean mNotABadTimeForASync = false;
	private SyncPrefs mSyncPrefs;
	private boolean mSyncRequestedByUser = false;
	private Handler mRefreshHandler;
	private Lock mRefreshLock = new ReentrantLock();
	private DropboxAuthentication mDropboxAuthentication;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		mHelper = new EmaActivityHelper(this);
		if (!mHelper.isInitialized()) {
			finish();
			return;
		}
		mSyncPrefs = new SyncPrefs(this);

		mHasCustomTitle = requestWindowFeature(Window.FEATURE_CUSTOM_TITLE);

		setContentView(R.layout.view_page);
		getWindow().setFeatureInt(Window.FEATURE_CUSTOM_TITLE,
				R.layout.custom_titlebar);

		setTitleCustom(getText(R.string.app_name).toString());

		initializeLocals();
		initializeButtons();
		initializeBrowser();

		tryToPopulateFromSavedState(savedInstanceState);
		startSyncThread();
	}

	private void initializeButtons() {

		setClickListenerOn(R.id.button_back, new View.OnClickListener() {
			public void onClick(View v) {
				onBackPressed();
			}
		});

		setClickListenerOn(R.id.button_edit, new View.OnClickListener() {
			public void onClick(View v) {
				editPage();
			}
		});

		setClickListenerOn(R.id.button_home, new View.OnClickListener() {
			public void onClick(View v) {
				loadUrlWithHistory(WikiPage.DEFAULT_PAGE);
			}
		});

		setClickListenerOn(R.id.button_sync, new View.OnClickListener() {
			public void onClick(View v) {
				mSyncRequestedByUser = true;
				mNotABadTimeForASync = true;
			}
		});
	}

	private void setClickListenerOn(int buttonId, View.OnClickListener listener) {
		Button b = mHelper.find(buttonId);
		if (b == null)
			return;

		b.setOnClickListener(listener);
	}

	private void setTitleCustom(String titleText) {
		if (mHasCustomTitle) {
			TextView title = mHelper.find(R.id.title);
			title.setText(titleText);
		} else {
			mCustomWindowTitle = titleText;
			setTitle(titleText);
		}
	}

	private void showStatus(String message, int progressPct) {
		if (mHasCustomTitle) {
			TextView msg = mHelper.find(R.id.title_message);
			if (message.length() > 20) {
				message = message.substring(0, 19);
			}
			msg.setText(message);
			msg.setVisibility(TextView.VISIBLE);

			ProgressBar titleProgressBar = (ProgressBar) findViewById(R.id.title_progressbar);
			titleProgressBar.setVisibility(ProgressBar.VISIBLE);
			titleProgressBar.setMax(100);
			titleProgressBar.setProgress(progressPct);
		} else {
			setTitle(mCustomWindowTitle + " " + message + " " + progressPct
					+ "%");
		}
	}

	private void hideStatus() {
		if (mHasCustomTitle) {
			TextView msg = mHelper.find(R.id.title_message);
			msg.setText("");
			ProgressBar titleProgressBar = mHelper.find(R.id.title_progressbar);
			titleProgressBar.setVisibility(ProgressBar.GONE);
			msg.setVisibility(TextView.GONE);
		} else {
			setTitle(mCustomWindowTitle);
		}
	}

	private void initializeLocals() {
		mHistory = new Stack<ViewPageCommand>();

		mRefreshHandler = new Handler() {
			@Override
			public void handleMessage(Message msg) {
				rememberScrollPos();
				showOrRefreshCurrentPage();
			}
		};
	}

	private WebView getWebView(int which) {
		if (which == 0) {
			return mHelper.find(R.id.webview0);
		} else {
			return mHelper.find(R.id.webview1);
		}
	}

	private WebView getWebView() {
		return getWebView(mActiveWebview);
	}

	private WebView getInvisibleWebView() {
		int otherView = mActiveWebview == 0 ? 1 : 0;
		return getWebView(otherView);
	}

	private void initializeBrowser() {
		initializeBrowser(0);
		initializeBrowser(1);
	}

	private void initializeBrowser(int which) {
		final WebView wv = getWebView(which);
		wv.getSettings().setJavaScriptEnabled(true);
		wv.getSettings().setBuiltInZoomControls(true);

		wv.setWebViewClient(new WebViewClient() {
			@Override
			public boolean shouldOverrideUrlLoading(WebView webview, String url) {
				return navigateTo(url);
			}

			@Override
			public void onPageFinished(WebView view, String url) {
				super.onPageFinished(view, url);
				if (mIgnoreBrowserUpdate) {
					mIgnoreBrowserUpdate = false;
				} else {
					restoreScrollPosAndShowBrowser();
				}
			}
		});

		wv.addJavascriptInterface(new JavascriptInterop(
				new JavascriptInterop.Listener() {
					public void sendMessage(String command, String parameters) {
						if (command.equalsIgnoreCase("checkbox")) {
							int whichOne = Integer.parseInt(parameters);
							toggleCheck(whichOne);
						}
					}
				}), "EmaInterop");
	}

	private void tryToPopulateFromSavedState(Bundle savedInstanceState) {
		boolean populated = populate(savedInstanceState);
		if (!populated) {
			// populate from other activity's intent
			Intent i = getIntent();
			if (i != null) {
				Bundle b = i.getExtras();
				populated = populate(b);
			}
		}

		if (!populated) {
			// do default (=Home)
			showOrRefreshCurrentPage();
		}
	}

	private void startSyncThread() {
		// show sync progress in title bar
		final Handler syncProgressHandler = new Handler() {
			@Override
			public void handleMessage(Message msg) {
				String message = (String) msg.obj;
				int pct = msg.arg1;
				showStatus(message, pct);
			}
		};
		final Handler hideStatusHandler = new Handler() {
			@Override
			public void handleMessage(Message msg) {
				hideStatus();
			}
		};

		// do the initial sync?
		if (mSyncPrefs.getOnStartup()) {
			mNotABadTimeForASync = true;
		}

		// start a synchronization thread
		Thread t = new Thread(new Runnable() {
			public void run() {
				while (true) {
					try {
						// sleep in batches of 1000 milliseconds, so it is
						// easier to interrupt by the "Not A Bad Time" trigger
						int seconds = 0;
						while (seconds < mSyncPrefs.getIntervalMinutes() * 60) {
							Thread.sleep(1000);

							// can loop be broken by the timer?
							if (mSyncPrefs.getPeriodically()) {
								seconds++;
							}

							if (mNotABadTimeForASync) {
								break;
							}
						}

						if (mDropboxAuthentication == null) {
							mDropboxAuthentication = new DropboxAuthentication(ViewPage.this);
						}

						if (!mDropboxAuthentication.getIsAuthenticated()) {
							if (mSyncRequestedByUser) {
								mSyncRequestedByUser = false;
								mHelper.showToast(getText(R.string.provide_credentials));
							}
							continue;
						}
						mSyncRequestedByUser = false;

						Sync sync = new Sync(mHelper, mDropboxAuthentication
								.getAPI());
						sync.setStatusHandler(syncProgressHandler);
						if (sync.perform()) {
							// -there was a change-
							mRefreshHandler.sendEmptyMessage(0);
						}
						hideStatusHandler.sendEmptyMessage(0);

					} catch (Exception e) {
						// show sync error
						mHelper.showToast(getText(R.string.syncError), e);
					} finally {
						mNotABadTimeForASync = false;
					}
				}
			}
		});
		t.start();
	}

	private boolean populate(Bundle b) {
		if (b == null) {
			return false;
		}

		String pageTitle = b.getString(TITLE_KEY);
		if (pageTitle == null || pageTitle.length() == 0) {
			return false;
		}

		double scrollPos = b.getDouble(SCROLLPOS_KEY);

		mHistory.push(new ViewPageCommand(pageTitle, scrollPos));

		showOrRefreshCurrentPage();
		return true;
	}

	private ViewPageCommand currentPage() {
		if (mHistory.empty()) {
			mHistory.add(new ViewPageCommand(WikiPage.DEFAULT_PAGE));
		}

		return mHistory.peek();
	}

	private void loadUrlWithHistory(String pageName) {
		if (currentPage().PageName != pageName) {
			rememberScrollPos(); // for current page that very soon is not the
									// current page any longer
			mHistory.push(new ViewPageCommand(pageName));
		}
		showOrRefreshCurrentPage();
	}

	@SuppressWarnings("unused")
	private void log(String message) {
		System.out.println(message);
	}

	private boolean mInRefresh = false;

	private void showOrRefreshCurrentPage() {
		// log("Requesting lock...");
		mRefreshLock.lock();
		// log("in lock");
		if (mInRefresh) {
			// log("a refresh action is already taking place, returning.");
			return;
		}
		mInRefresh = true;
		mRefreshLock.unlock();

		final ViewPageCommand currentPage = currentPage();
		setTitleCustom(currentPage.PageName);
		getWebView().setEnabled(false);

		// do time-consuming things asynchronously
		new Thread(new Runnable() {
			public void run() {
				WikiPage page = mHelper.getDal().fetchByName(
						currentPage.PageName);

				String htmlBody = "";
				try {
					htmlBody = page.getHtmlBody();
				} catch (IOException e) {
					mHelper.showToast(getText(R.string.page_error), e);
					return;
				}

				htmlBody = htmlBody.replaceAll("emafile\\:",
						"content://com.janwillemboer.ema.localfile/");

				// log("Loading html in view " + mActiveWebview);
				getInvisibleWebView().loadDataWithBaseURL("fake://i/will/smack/the/engineer/behind/this/scheme", 
						htmlBody, "text/html",
						FileStuff.ENCODING, "");
				// will be made visible in the onpageload eventhandler
			}
		}).start();
	}

	private void rememberScrollPos() {
		final WebView wv = getWebView();
		currentPage().setScrollPos(wv.getContentHeight(), wv.getScrollY());
	}

	private void restoreScrollPosAndShowBrowser() {
		final WebView invisibleWv = getInvisibleWebView();
		final WebView visibleWv = getWebView();

		final int pos = currentPage().getScrollPos(
				invisibleWv.getContentHeight());

		// the code to set scroll pos and show browser, to be executed later in
		// this method
		final Handler restoreScrollPosHandler = new Handler() {
			@Override
			public void handleMessage(Message msg) {
				try {
					// get position again, the previous pos was not reliable
					int posReloaded = currentPage().getScrollPos(
							invisibleWv.getContentHeight());

					invisibleWv.scrollTo(0, posReloaded);

					// log("set invisible: " + mActiveWebview);
					invisibleWv.setVisibility(View.VISIBLE);
					invisibleWv.setEnabled(true);
					visibleWv.setVisibility(View.GONE);
					mIgnoreBrowserUpdate = true;
					visibleWv.loadData("", "text/plain", FileStuff.ENCODING);
					mActiveWebview = mActiveWebview == 0 ? 1 : 0;
					// log("set visible: " + mActiveWebview);
				} finally {
					mRefreshLock.lock();
					mInRefresh = false;
					mRefreshLock.unlock();
					// log("released lock");
				}
			}
		};

		if (pos > 0) {
			// the onpageloaded event seems to be triggered to early to set the
			// scrollposition here. So do this after 100 ms from now.
			new Thread(new Runnable() {
				public void run() {
					try {
						Thread.sleep(100);
					} catch (InterruptedException ex) {
					}
					restoreScrollPosHandler.sendEmptyMessage(0);
				}
			}).start();
		} else {
			// no need to delay the code, execute right now.
			restoreScrollPosHandler.sendEmptyMessage(0);
		}
	}

	private void toggleCheck(final int whichOne) {

		// do asynchronously, because it is time-consuming
		final String page = currentPage().PageName;
		new Thread(new Runnable() {
			public void run() {
				try {
					mHelper.getDal().toggleCheckbox(page, whichOne);

					// do not refresh the browser here, because this looks very
					// slow
					// instead, the browser will assume a way to show the change
					// itself.

					if (mSyncPrefs.getAfterEdit()) {
						mNotABadTimeForASync = true;
					}
				} catch (IOException e) {
					mHelper.showToast(getText(R.string.save_error), e);
				}
			}
		}).start();
	}

	@Override
	public void onBackPressed() {
		if (currentPage().PageName == WikiPage.DEFAULT_PAGE) {
			// (home page is shown)
			super.onBackPressed();
			return;
		}

		mHistory.pop();
		showOrRefreshCurrentPage();
	}

	private void editPage() {
		Intent i = new Intent(this, EditPage.class);
		Bundle b = new Bundle();

		b.putString(EditPage.TITLE_KEY, currentPage().PageName);
		i.putExtras(b);

		rememberScrollPos();

		try {
			startActivityForResult(i, EDIT_PAGE);
		} catch (RuntimeException e) {
			System.out.println(e.toString());
			throw e;
		}
	}

	@Override
	protected void onActivityResult(int requestCode, int resultCode,
			Intent intent) {

		switch (requestCode) {
		case EDIT_PAGE:
			showOrRefreshCurrentPage();
			if (mSyncPrefs.getAfterEdit()) {
				mNotABadTimeForASync = true;
			}
			return;

		case LIST_PAGES:
			if (intent != null) {
				loadUrlWithHistory(intent.getStringExtra(TITLE_KEY));
				return;
			}
			break;

		case LOGIN:
			if (mDropboxAuthentication != null) {
				mDropboxAuthentication.reInitialize();
			}
			mNotABadTimeForASync = true;
			break;

		}

		super.onActivityResult(requestCode, resultCode, intent);

	}

	private static final Pattern wikiLinkPattern = Pattern.compile("ema:(.*)");

	private boolean navigateTo(String url) {

		Matcher m = wikiLinkPattern.matcher(url);
		if (m.matches()) {
			// UrlQuerySanitizer().unescape() works incorrect 
			// see http://code.google.com/p/android/issues/detail?id=14437 for details
//			String page = new UrlQuerySanitizer().unescape(m.group(1));
			String page = android.net.Uri.decode(m.group(1));

			loadUrlWithHistory(page);
			return true;
		}

		// normal url, leave it to the system in an external activity
		// replace replaced links
		url = url.replaceAll("content://com.janwillemboer.ema.localfile/",
				"file://" + mHelper.getDal().Dir().getAbsolutePath() + "/");

		boolean isFile = url.startsWith("file://");

		Uri uri = Uri.parse(url);
		Intent intent = new Intent(Intent.ACTION_VIEW, uri);

		if (isFile) {
			// find out the mimetype in file url's (because this is case
			// sensitive,
			// the default mechanism will not always behave correctly)
			String ext = MimeTypeMap.getFileExtensionFromUrl(uri.toString());
			String type = MimeTypeMap.getSingleton().getMimeTypeFromExtension(
					ext.toLowerCase());
			intent.setType(type);
		}

		try {
			startActivity(intent);
		} catch (ActivityNotFoundException e) {
			mHelper.showToast(R.string.unknownMimetype);
		}
		return true;
	}

	@Override
	protected void onSaveInstanceState(Bundle outState) {
		super.onSaveInstanceState(outState);
		ViewPageCommand currentPage = currentPage();
		outState.putString(TITLE_KEY, currentPage.PageName);
		outState.putDouble(SCROLLPOS_KEY, currentPage.RelativeScrollPosition);
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		MenuInflater inflater = getMenuInflater();
		inflater.inflate(R.menu.viewpage_menu, menu);
		return true;
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		// Handle item selection
		switch (item.getItemId()) {
		case R.id.about:
			loadUrlWithHistory(WikiPage.ABOUT_PAGE);
			return true;

		case R.id.help:
			navigateTo("http://www.janwillemboer.nl/blog/ema-personal-wiki");
			return true;

		case R.id.list_pages:
			listPages();
			return true;

		case R.id.quit:
			finish();
			return true;

		case R.id.dropbox_login:
			showDropboxLoginCredentials();
			return true;

		default:
			return super.onOptionsItemSelected(item);
		}
	}

	private void listPages() {
		Intent i = new Intent(this, PagesList.class);
		startActivityForResult(i, LIST_PAGES);
	}

	private void showDropboxLoginCredentials() {
		Intent i = new Intent(this,
				com.janwillemboer.ema.dropbox.EditSyncPreferences.class);
		startActivityForResult(i, LOGIN);
	}
}
