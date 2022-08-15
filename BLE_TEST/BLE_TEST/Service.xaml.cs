using Quick.Xamarin.BLE;
using Quick.Xamarin.BLE.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BLE_TEST
{
    public partial class Service : ContentPage
    {
        public static AdapterConnectStatus BleStatus;
        List<IGattCharacteristic> AllCharacteristics = new List<IGattCharacteristic>();
        IGattCharacteristic TxCharacteristic = null;
        IGattCharacteristic RxCharacteristic = null;
        public Service()
        {
            InitializeComponent();
            Search.ble.AdapterStatusChange += Ble_AdapterStatusChange;
            Search.ble.ServerCallBackEvent += Ble_ServerCallBackEvent;
        }

        private void Ble_ServerCallBackEvent(string uuid, byte[] value)
        {
            Device.BeginInvokeOnMainThread(() => {
                if (RxCharacteristic != null)
                {
                    if (RxCharacteristic.Uuid == uuid)
                    {
                        string str = BitConverter.ToString(value);
                        info_read.Text = "Read Data:" + str;
                    }
                }
            });
        }

        private void Ble_AdapterStatusChange(object sender, AdapterConnectStatus e)
        {
            Device.BeginInvokeOnMainThread(async () => {
               Search.BleStatus = e;
                if(Search.BleStatus== AdapterConnectStatus.Connected)
                {
                    msg_txt.Text = "Success";
                    await Task.Delay(1000);
                    msg_layout.IsVisible = false;
                    ReadCharacteristics();
                    await Task.Delay(4000);
                    foreach (var c in AllCharacteristics)
                    { 
                        if (c.CanNotify() || c.CanRead())
                        {
                            RxCharacteristic = c;

                            read_btn.IsVisible = true;
                            upgrade_btn.IsVisible = true;
  
                            info_read.Text = "Read Data:";
                        }
                        else if (c.CanWrite())
                        {
                            TxCharacteristic = c;
                            write_btn.IsVisible = true;
                        }

                        if((TxCharacteristic != null) && (RxCharacteristic != null))
                        {
                            
                            info_uuid.Text = "TX:" + TxCharacteristic.Uuid + " RX:" + RxCharacteristic.Uuid;
                            info.IsVisible = true;
                            break;
                        }
                    }
                }
                if (Search.BleStatus == AdapterConnectStatus.None)
                {
                  await  Navigation.PopToRootAsync(true);
                }
            });
        }
        void ReadCharacteristics()
        {
            Search.ConnectDevice.CharacteristicsDiscovered(cha =>
            {  
                Device.BeginInvokeOnMainThread(() => {
                    AllCharacteristics.Add(cha);
                });
            } );
        }
       
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Search.ble.AdapterStatusChange -= Ble_AdapterStatusChange;
            Search.ble.ServerCallBackEvent -= Ble_ServerCallBackEvent;
            if (Search.ConnectDevice!=null) Search.ConnectDevice.DisconnectDevice();
        }

        private void read_Clicked(object sender, EventArgs e)
        {
            if (RxCharacteristic != null)
            {
                RxCharacteristic.ReadCallBack();
            }
        }
        private void write_Clicked(object sender, EventArgs e)
        {
           var bytearray= StringToByteArray(info_write.Text);
            if (bytearray == null)
            {
                DisplayAlert("", "Input format error", "ok");
                return;
            }
            TxCharacteristic.Write(bytearray);

        }
        private void upgrade_Clicked(object sender, EventArgs e)
        {
            if ((RxCharacteristic != null) && (TxCharacteristic != null))
            {
               
            }
        }
       
        public static byte[] StringToByteArray(string hex)
        {
            try
            {
                return Enumerable.Range(0, hex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                 .ToArray();
            }
            catch{ return null; }
        }
    }
}
