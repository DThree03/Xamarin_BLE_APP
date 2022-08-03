using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Bluetooth;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPillowTest;

namespace OTABootloader
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class AnalyzeEEGDataActivity : Activity
	{
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
            // Set our view from the "EEGData analyze" layout resource
            SetContentView(Resource.Layout.activity_eeg);

            // Get our button from the layout resource,
            Button buttonBenchmark = FindViewById<Button>(Resource.Id.buttonBenchmark);
            Button buttonEEGData = FindViewById<Button>(Resource.Id.buttonEEGData);
            Button buttonAcceData = FindViewById<Button>(Resource.Id.buttonAcceData);
            UIResponse = FindViewById<TextView>(Resource.Id.textUIResponse);
            /* Default */
            buttonBenchmark.Enabled = true;
            buttonEEGData.Enabled = false;
            buttonAcceData.Enabled = true;

            /* Page EEGData action */
            buttonBenchmark.Click += delegate
            {
                try
                {
                    var intent = new Intent(this, typeof(MainActivity));
                    StartActivity(intent);
                }
                catch { }
                /* Update UI */
                RunOnUiThread(() =>
                {
                    /* Update UIResponse */
                    UIResponse.Text = "[UI]:Page EEGData action";
                });
            };
            // Create your application here
            //var phoneNumbers = Intent.Extras.GetStringArrayList("phone_numbers") ?? new string[0];
            //this.ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, phoneNumbers);
        }
        #region Private Fields
        private TextView UIResponse;
        #endregion
    }
}
