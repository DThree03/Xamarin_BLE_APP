using Quick.Xamarin.BLE;
using Quick.Xamarin.BLE.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;

using System.Threading;
using System.Threading.Tasks;

using CircularBuffer;

namespace BLE_TEST
{
    public partial class Service : ContentPage
    {
        public static AdapterConnectStatus BleStatus;
        List<IGattCharacteristic> AllCharacteristics = new List<IGattCharacteristic>();
        IGattCharacteristic TxCharacteristic = null;
        IGattCharacteristic RxCharacteristic = null;

        byte[] FileFWcontents;
        public static byte[] RxData;
        public CircularBuffer<byte> RingBuFTestBuffer, RingBufferOTA;
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
                        RxData = value;
                        string str = BitConverter.ToString(RxData);
                        //string str = RxData[0].ToString();
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
                    upgrade_btn.IsVisible = false;
                    ReadCharacteristics();
                    await Task.Delay(4000);
                    foreach (var c in AllCharacteristics)
                    { 
                        if (c.CanNotify() || c.CanRead())
                        {
                            RxCharacteristic = c;

                            read_btn.IsVisible = true;
                            pickfile_btn.IsVisible = true;
  
                            info_read.Text = "Read Data:";
                        }
                        else if (c.CanWrite())
                        {
                            TxCharacteristic = c;
                            write_btn.IsVisible = true;
                        }

                        if((TxCharacteristic != null) && (RxCharacteristic != null))
                        {
                            
                            info_uuid.Text = "TX:" + TxCharacteristic.Uuid + "\nRX:" + RxCharacteristic.Uuid;
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
       
        //protected override void OnDisappearing()
        //{
        //    base.OnDisappearing();
        //    Search.ble.AdapterStatusChange -= Ble_AdapterStatusChange;
        //    Search.ble.ServerCallBackEvent -= Ble_ServerCallBackEvent;
        //    if (Search.ConnectDevice!=null) Search.ConnectDevice.DisconnectDevice();
        //}

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

        private async void pickfile_Clicked(object sender, EventArgs e)
        {
            try
            {
                FileData file = await CrossFilePicker.Current.PickFile();
                if (file != null)
                {
                    info_read.Text = "OTA Firmware:" + file.FileName;
                    /* Get Content of file */
                    FileFWcontents = file.DataArray;
                    upgrade_btn.IsVisible = true;
                }
            }
            catch { return; }
        }
        private async void upgrade_Clicked(object sender, EventArgs e)
        {
            if ((RxCharacteristic != null) && (TxCharacteristic != null))
            {
                await Upgrade_OTA_Progress();
            }
        }

        private async Task Upgrade_OTA_Progress()
        {
            await Task.Run(() =>
            {
                try
                {
                    /* Sleep 1s and clear buffer */
                    Thread.Sleep(1000);
                    /* Init Xmodem */
                    var xmodem = new XModem.XModem(RxCharacteristic, TxCharacteristic, RingBufferOTA);
                    byte[] data;
                    data = FileFWcontents;
                    int bytesSent = 0;
                    /* Clear all data first */
                    RxData = null;
                    //_bluetoothSocket.InputStream.Flush();
                    //_bluetoothSocket.OutputStream.Flush();
                    /* Just display process */
                    xmodem.PacketSent += (sender, args) =>
                    {
                        bytesSent += 128;
                        int Percentage = Math.Min(bytesSent, data.Length) * 100 / data.Length;
                        //UIResponse.Text = sprintf("Firmware Update: {0}% sent!", Math.Min(bytesSent, data.Length) * 100 / data.Length);
                        //UIResponse.Text = sprintf("Firmware Update: %d precentage sent!", Percentage);
                    };

                    /* Send all firmare */
                    int result = xmodem.XmodemTransmit(data, data.Length, false);
                    //int result = 5;

                    if (result < data.Length)
                    {
                        //UIResponse.Text = sprintf("Update Firmware Fail! Result: %d Length: %d", result, data.Length);
                        info_read.Text = "Upgrade FW Fail";
                    }
                    else
                    {
                        //UIResponse.Text = "Update Firmware Success!";
                        info_read.Text = "Upgrade FW Success!";
                    }
                    /* Disconnect bluetooth connection */
                    //myConnection.thisDevice.Dispose();
                    //myConnection.thisSocket.OutputStream.WriteByte(187);
                    //myConnection.thisSocket.OutputStream.Close();
                    //myConnection.thisSocket.Close();
                    //myConnection = new BluetoothConnection();
                    //_bluetoothSocket = null;
                    //connected.Text = "Disconnected!";
                    /* Enable connect button again */
                    //buttonConnect.Enabled = true;
                    /* Update UIResponse */
                    //UIResponse.Text = "[UI]:Disconnected event!";
                }
                catch { }
            });
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
