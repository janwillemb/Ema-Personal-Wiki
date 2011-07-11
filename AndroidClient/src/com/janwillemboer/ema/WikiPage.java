package com.janwillemboer.ema;

import java.io.File;
import java.io.IOException;
import java.util.LinkedList;
import java.util.Queue;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import com.petebevin.markdown.MarkdownProcessor;

public class WikiPage {

	private static final String EMA_PLACEHOLDER = "<_ema.ph_>";
	private static final String DOLLAR_PLACEHOLDER = "<_dollar.ph_>"; //due to a stupid JDK bug
	public static final String DEFAULT_PAGE = "Home";
	public static final String EMA_PAGES_PREFIX = "Ema.";
	public static final String ABOUT_PAGE = EMA_PAGES_PREFIX + "About";

	public static final String EXT = ".txt";
	public static String css = "";
	public static String defaultPageDefaultContent = "";
	public static String aboutPageContent = "";

	private File mFile;

	public WikiPage(File f) {
		mFile = f;
	}

	public String getName() {
		String pageName = mFile.getName();
		pageName = pageName.substring(0, pageName.length() - EXT.length());
		return pageName;
	}

	public String getBody() throws IOException {
		if (!mFile.exists()
				|| getName().toLowerCase().startsWith(
						EMA_PAGES_PREFIX.toLowerCase())) {
			// create default file content, ie empty in most cases.
			// but content for some special pages.
			return defaultContent();
		}

		// read content from file
		return FileStuff.readContents(mFile);
	}

	private String defaultContent() {
		if (getName().equalsIgnoreCase(DEFAULT_PAGE)) {
			return defaultPageDefaultContent;
		} else if (getName().equalsIgnoreCase(ABOUT_PAGE)) {
			return aboutPageContent;
		}

		return "";
	}

	private Queue<String> htmlTagsQueue;
	private static final Pattern tagsPattern = Pattern
			.compile("\\<a\\s.+?\\<\\/a\\>|\\<[^\\>]+\\>");

	private String cloakTags(String html) {
		StringBuffer sb = new StringBuffer();
		Matcher m = tagsPattern.matcher(html);
		while (m.find()) {
			htmlTagsQueue.offer(m.group(0));
			m.appendReplacement(sb, EMA_PLACEHOLDER);
		}
		m.appendTail(sb);
		return sb.toString();
	}

	private String uncloakTags(String html) {
		StringBuffer sb = new StringBuffer();
		Matcher m = tagsPattern.matcher(html);
		while (m.find()) {
			String tag = m.group(0);
			if (tag.equals(EMA_PLACEHOLDER)) {
				tag = htmlTagsQueue.poll();
			}
			
			//due to a JDK bug it is very hard to have dollar signs in the
			//replacement part. So just try very hard to evade it. 
			if (tag.contains("$")) {
				tag = tag.replaceAll("\\$", DOLLAR_PLACEHOLDER);
			}
			m.appendReplacement(sb, tag);
		}
		m.appendTail(sb);
		return sb.toString().replaceAll(DOLLAR_PLACEHOLDER, "\\$");
	}

	public static final Pattern checkBoxes = Pattern.compile(
			"^\\s*(\\<[^\\>]+\\>)?\\s*\\[([\\sxX])\\]([^\\<\\>\\n]*)",
			Pattern.MULTILINE);
	private static final Pattern wikiWords = Pattern
			.compile(
					"(~)?(           #remember the previous character if it is the ignore marker and start a group for the actual match\n"
							+ "\\p{Lu}            #start with uppercase letter \n"
							+ "\\p{Ll}+           #one or more lowercase letters \n"
							+ "\\p{Lu}            #one uppercase letter \n"
							+ "\\w*               #and zero or more arbitrary characters in the same word \n"
							+ "|                 #or \n"
							+ "\\{                #start with a curly bracket \n"
							+ "[^\\{\\}]+         #anything inbetween that is not curly bracket \n"
							+ "\\}                #end with cl br \n"
							+ ")               #close the group for the actual match",
					Pattern.COMMENTS);

	public String getHtmlBody() throws IOException {

		String html = getBody();

		// apply markdown formatting
		MarkdownProcessor processor = new MarkdownProcessor();
		html = processor.markdown(html);

		// remove tags from the body, so they can't be affected by the
		// custom transformations
		htmlTagsQueue = new LinkedList<String>();
		html = cloakTags(html);

		StringBuffer sb;
		Matcher m;

		// replace wikilinks
		sb = new StringBuffer();
		m = wikiWords.matcher(html);
		while (m.find()) {
			String nowikiword = m.group(1);
			String replacement = m.group(2);

			if (nowikiword == null) {
				if (replacement.startsWith("{")) {
					replacement = replacement.substring(1,
							replacement.length() - 1);
				}
				replacement = "<a href='ema:" + replacement + "'>"
						+ replacement + "</a>";
			}
			m.appendReplacement(sb, replacement);
		}
		m.appendTail(sb);
		html = sb.toString();

		// temporary uncloak and cloak again, to include the new tags
		html = uncloakTags(html);
		html = cloakTags(html);

		// replace checkboxes
		sb = new StringBuffer();
		m = checkBoxes.matcher(html);
		int checkboxIndex = 0;
		while (m.find()) {
			String prefix = m.group(1) == null ? "" : m.group(1);
			String checked = m.group(2);
			String label = m.group(3);
			if (label == null)
				label = "";

			String replacement = "<div class='ema-task'>";
			String js = "onclick=\"toggleCheck('" + checkboxIndex + "');\"";
			String labelTagStart = "<label id='label_" + checkboxIndex + "'";
			if (checked.equalsIgnoreCase("x")) {
				replacement += labelTagStart
						+ " class='ema-task-finished'><input type='checkbox' checked='checked' "
						+ js + "/>";
			} else {
				replacement += labelTagStart + "><input type='checkbox' " + js
						+ "/>";
			}
			replacement += label + "</label></div>";
			m.appendReplacement(sb, prefix + replacement);
			checkboxIndex++;
		}
		m.appendTail(sb);
		html = sb.toString();
		html = uncloakTags(html);

		return "<html>"
				+ "  <head>"
				+ "    <meta http-equiv='Content-Type' content='text/html;charset=UTF-8'/>"
				+ "    <title>Ema Personal Wiki</title>"
				+ "    <style type='text/css'>"
				+ css
				+ "</style>"
				+ "    <script type='text/javascript'>"
				+ "      function toggleCheck(which) {"
				+ "        window.EmaInterop.sendMessage('checkbox', which);"
				+ "        var lbl = document.getElementById('label_' + which); "
				+ "        if (lbl.className == 'ema-task-finished') "
				+ "          lbl.className =  '';"
				+ "        else "
				+ "          lbl.className = 'ema-task-finished';"
				+ "      }"
				+ "    </script>"
				+ "  </head>"
				+ "  <body>"
				+ html
				+ "  <div id='debug'></div>"
				+ "  <script type='text/javascript'>var dbg = document.getElementById('debug'); function log(text) { dbg.innerHTML = text }</script>"
				+ "  </body>" + "</html>";
	}

	public File getFile() {
		return mFile;
	}

}
