using Quick.Xamarin.BLE;
using Quick.Xamarin.BLE.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

using CircularBuffer;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;

namespace BLE_TEST
{
    public partial class Service : ContentPage
    {
        public static AdapterConnectStatus BleStatus;
        List<IGattCharacteristic> AllCharacteristics = new List<IGattCharacteristic>();
        IGattCharacteristic SelectCharacteristic = null;
        ObservableCollection<CharacteristicsList> CharacteristicsList = new ObservableCollection<CharacteristicsList>();

        public CircularBuffer<byte> RingBuFTestBuffer, RingBufferOTA;
        byte[] FileFWcontents;

        public Service()
        {
            InitializeComponent();
            Search.ble.AdapterStatusChange += Ble_AdapterStatusChange;
            //Search.ble.ServerCallBackEvent += Ble_ServerCallBackEvent;
            listView.ItemsSource = CharacteristicsList; 
            
        }

        private void Ble_AdapterStatusChange(object sender, AdapterConnectStatus e)
        {
            Device.BeginInvokeOnMainThread(async () => {
               Search.BleStatus = e;
                if(Search.BleStatus== AdapterConnectStatus.Connected)
                {
                    msg_txt.Text = "Success";
                    await Task.Delay(2000);
                    msg_layout.IsVisible = false;
                    listView.IsVisible = true;
                    ReadCharacteristics();
              
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
                    CharacteristicsList.Add(new CharacteristicsList( cha.Uuid,cha.CanRead(), cha.CanWrite(), cha.CanNotify()));
                });
            } );
        }
        private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            
            var select = (CharacteristicsList)e.Item;
            foreach (var c in AllCharacteristics)
            { 
                if (c.Uuid == select.Uuid)
                {
                    if(c.CanWrite())
                    {
                        SelectCharacteristic = c;
                        info_uuid.Text = "OTA Write UUID:" + SelectCharacteristic.Uuid;
                        info_filename.Text = "File Name:";
                        write_btn.IsVisible = true;
                        pickfile_btn.IsVisible = true;
                        background.IsVisible = true;
                        info.IsVisible = true;

                        RingBufferOTA = new CircularBuffer<byte>(4096);
                    }
                    else
                    {
                        info_uuid.Text = "UUID Cant't Write OTA Code!";
                        info_filename.Text = "Click in the Background and choose another UUID!";
                        write_btn.IsVisible = false;
                        pickfile_btn.IsVisible = false;
                        background.IsVisible = true;
                        info.IsVisible = true;
                    }
                    break;
                }
            }
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Search.ble.AdapterStatusChange -= Ble_AdapterStatusChange;
            //Search.ble.ServerCallBackEvent -= Ble_ServerCallBackEvent;
            if (Search.ConnectDevice!=null) Search.ConnectDevice.DisconnectDevice();
        }

        private void background_click(object sender, EventArgs e)
        {
            background.IsVisible = false;
            info.IsVisible = false;
            SelectCharacteristic = null;
        }

        private async void write_Clicked(object sender, EventArgs e)
        {
            //var bytearray = StringToByteArray(info_write.Text);
            //if (bytearray == null)
            // {
            //DisplayAlert("", "Input format error", "ok");
            // return;
            // }
            //SelectCharacteristic.Write(bytearray);
            await Upgrade_OTA_Progress();
        }
        private async void pickfile_Clicked(object sender, EventArgs e)
        {
            if (SelectCharacteristic != null)
            {
                await PickAndShow();
            }
        

            //if (pickfile_btn.Text.ToLower() == "Pick File")
            //{
            //isnotify = true;
            //pickfile_btn.Text = "Loading";
            //SelectCharacteristic.NotifyEvent += SelectCharacteristic_NotifyEvent;
            //SelectCharacteristic.Notify();
            // }
            //else
            //{
            // isnotify = false;
            // pickfile_btn.Text = "Pick File";
            //SelectCharacteristic.StopNotify();
            //SelectCharacteristic.NotifyEvent -= SelectCharacteristic_NotifyEvent;
            //}
        
        }
        private async Task PickAndShow()
        {
            FileData file = null;
            try
            {
                file = await CrossFilePicker.Current.PickFile().ConfigureAwait(true);

                if (file != null)
                {
                    
                    info_filename.Text = "The filename:" + file.FileName;
                    FileFWcontents = file.DataArray;
                    //image.Source = ImageSource.FromStream(() => data);
                }
            }
            catch {
                return;
            }
        }

        private async Task Upgrade_OTA_Progress()
        {
            await Task.Run(() =>
            {
                try
                {
                    ///* Sleep 1s and clear buffer */
                    Thread.Sleep(1000);
                    ///* Init Xmodem */
                    var xmodem = new XModem.XModem(SelectCharacteristic, RingBufferOTA);
                    byte[] data;
                    data = FileFWcontents;
                    int bytesSent = 0;
                    ///* Clear all data first */
                    //_bluetoothSocket.InputStream.Flush();
                    //_bluetoothSocket.OutputStream.Flush();
                    ///* Just display process */
                    xmodem.PacketSent += (sender, args) =>
                    {
                        bytesSent += 128;
                        int Percentage = Math.Min(bytesSent, data.Length) * 100 / data.Length;
                        //UIResponse.Text = sprintf("Firmware Update: {0}% sent!", Math.Min(bytesSent, data.Length) * 100 / data.Length);
                    //    UIResponse.Text = sprintf("Firmware Update: %d precentage sent!", Percentage);
                    };

                    ///* Send all firmare */
                    int result = xmodem.XmodemTransmit(data, data.Length, false);
                    if (result < data.Length)
                    {
                    //    UIResponse.Text = sprintf("Update Firmware Fail! Result: %d Length: %d", result, data.Length);
                    }
                    else
                    {
                    //    UIResponse.Text = "Update Firmware Success!";
                    }

                    ///* Disconnect bluetooth connection */
                    //myConnection.thisDevice.Dispose();
                    //myConnection.thisSocket.OutputStream.WriteByte(187);
                    //myConnection.thisSocket.OutputStream.Close();
                    //myConnection.thisSocket.Close();
                    //myConnection = new BluetoothConnection();
                    //_bluetoothSocket = null;
                    //connected.Text = "Disconnected!";
                    ///* Enable connect button again */
                    //buttonConnect.Enabled = true;
                    ///* Update UIResponse */
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
