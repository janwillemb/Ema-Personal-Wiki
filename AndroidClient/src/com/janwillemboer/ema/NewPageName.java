package com.janwillemboer.ema;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;

public class NewPageName extends Activity {

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		
		setContentView(R.layout.newpage_name);
		
        Button confirmButton = (Button) findViewById(R.id.confirm);
        Button cancelButton = (Button) findViewById(R.id.cancel);
        final EditText editText = (EditText) findViewById(R.id.text1);
        
        confirmButton.setOnClickListener(new View.OnClickListener() {
			public void onClick(View v) {
				Bundle result = new Bundle();
				result.putString("pageTitle", editText.getText().toString());

				Intent i = new Intent();
				i.putExtras(result);

				setResult(RESULT_OK, i);
				finish();
			}
		});
        
        cancelButton.setOnClickListener(new View.OnClickListener() {
			public void onClick(View v) {
				setResult(RESULT_CANCELED);
				finish();
			}
		});


	}

}
