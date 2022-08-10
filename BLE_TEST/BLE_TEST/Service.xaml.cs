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
using Xamarin.Essentials;

namespace BLE_TEST
{
    public partial class Service : ContentPage
    {
        public static AdapterConnectStatus BleStatus;
        List<IGattCharacteristic> AllCharacteristics = new List<IGattCharacteristic>();
        IGattCharacteristic SelectCharacteristic = null;
        ObservableCollection<CharacteristicsList> CharacteristicsList = new ObservableCollection<CharacteristicsList>();
        bool isnotify = false;
        byte[] FileFWcontents;

        public Service()
        {
            InitializeComponent();
            Search.ble.AdapterStatusChange += Ble_AdapterStatusChange;
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
            //if (isnotify)
            //{
            //    SelectCharacteristic.StopNotify();
            //    SelectCharacteristic.NotifyEvent -= SelectCharacteristic_NotifyEvent;
            //}
        }

        private void write_Clicked(object sender, EventArgs e)
        {
           //var bytearray = StringToByteArray(info_write.Text);
            //if (bytearray == null)
           // {
                //DisplayAlert("", "Input format error", "ok");
               // return;
           // }
            //SelectCharacteristic.Write(bytearray);

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
                    //image.Source = ImageSource.FromStream(() => data);
                }
            }
            catch {
                return;
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
