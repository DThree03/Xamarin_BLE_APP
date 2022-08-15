package crc648203361843190f5f;


public class Ble
	extends android.bluetooth.le.ScanCallback
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onScanResult:(ILandroid/bluetooth/le/ScanResult;)V:GetOnScanResult_ILandroid_bluetooth_le_ScanResult_Handler\n" +
			"n_onScanFailed:(I)V:GetOnScanFailed_IHandler\n" +
			"";
		mono.android.Runtime.register ("Quick.Xamarin.BLE.Ble, Quick.Xamarin.BLE", Ble.class, __md_methods);
	}


	public Ble ()
	{
		super ();
		if (getClass () == Ble.class)
			mono.android.TypeManager.Activate ("Quick.Xamarin.BLE.Ble, Quick.Xamarin.BLE", "", this, new java.lang.Object[] {  });
	}


	public void onScanResult (int p0, android.bluetooth.le.ScanResult p1)
	{
		n_onScanResult (p0, p1);
	}

	private native void n_onScanResult (int p0, android.bluetooth.le.ScanResult p1);


	public void onScanFailed (int p0)
	{
		n_onScanFailed (p0);
	}

	private native void n_onScanFailed (int p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
