using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using System.Text;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Android.Locations;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
//using System.Diagnostics;
using Android.Content.PM;
/* Add Ringbuf library */
using CircularBuffer;
using ControllerDemo;
using Plugin.FilePicker;
using System.Text.RegularExpressions;
using Xamarin.Forms;
/* Adding oxyplot */
using OxyPlot.Xamarin.Android;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace SmartPillowTest
{
    public enum bStateMachineReceivingData : byte { eSM_Step0 = 0, eSM_Step1, eSM_Step2, eSM_Step3, eSM_Step4 };
    public enum MessageIds : byte
    {
        EEGDATA = 1,
        LOG = 2,
        SERIAL_PORT = 3,
        ACCELEROMETER_DATA = 0x4,
    }

    //[Activity(Label = "BluetoothApp", MainLauncher = true, Icon = "@drawable/icon")]
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        //BluetoothConnection myConnection = new BluetoothConnection();
        BluetoothManager _manager;
        protected override void OnCreate(Bundle bundle)
        {
            _manager = (BluetoothManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.BluetoothService);
            _manager.Adapter.Enable();
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // Get our button from the layout resource,
            // and attach an event to it
            buttonConnect = FindViewById<Button>(Resource.Id.btnConnect);
            buttonDisconnect = FindViewById<Button>(Resource.Id.btnDisconnect);
            buttonPickFile = FindViewById<Button>(Resource.Id.btnPickFile);
            buttonUpgrade = FindViewById<Button>(Resource.Id.btnUpgrade);
            connected = FindViewById<TextView>(Resource.Id.textView1);
            buttonBootloader = FindViewById<Button>(Resource.Id.btnJumpToBootloader);

            /* Default UI */
            StrInputDeviceName = FindViewById<EditText>(Resource.Id.textInputDeviceName);
            UIResponse = FindViewById<TextView>(Resource.Id.textUIResponse);
            /* Data result */
            /* UI Init */
            buttonDisconnect.Enabled = false;
            buttonUpgrade.Enabled = false;


            /* Current time */
            bDateTimeCurrent = DateTime.Now.ToLocalTime();
            string dt_string = bDateTimeCurrent.ToString("yyyy-MM-dd");
            RingBuFTestBuffer = new CircularBuffer<byte>(4096);
            RingBufferOTA = new CircularBuffer<byte>(4096);

            /* Button jump to bootloader */
            buttonBootloader.Click += delegate
            {
                try
                {
                    
                }
                catch { }
            };

            /* Process button click */
            buttonConnect.Click += delegate
            {
                try
                { 

                }
                catch (Exception CloseEX)
                {

                }
            };
            buttonPickFile.Click += delegate
            {
                try
                {
                    /* Update UIResponse */
                    UIResponse.Text = "[UI]:PickFile event!";
                    /* Pick file */
                    FilePickerButton();
                }
                catch { }
            };
            buttonUpgrade.Click += delegate
            {
                try
                {
                    /* Update UIResponse */
                    UIResponse.Text = "[UI]:Upgrade event!";
                    /* Set flag */
                    bFlagUpgradeOTA = true;
                    /* Display message */
                    UIResponse.Text = "Prepare to Upgrade Firmware";

                    /* Change some UI */
                    buttonDisconnect.Enabled = false;
                    buttonUpgrade.Enabled = false;
                    buttonPickFile.Enabled = false;
                    buttonBootloader.Enabled = false;

                    /* Start OTA application */
                    Upgrade_OTA_Progress();
                }
                catch { }
            };
            
            buttonDisconnect.Click += delegate
            {
                try
                {
                    Disconnect();
                }
                catch { }
            };
        }

        public void OpenLocationSettings()
        {

            LocationManager LM = (LocationManager)Forms.Context.GetSystemService(Android.Content.Context.LocationService);
            if (LM.IsProviderEnabled(LocationManager.GpsProvider) == false)
            {
                AlertDialog ad = new AlertDialog.Builder(this).Create();

                ad.SetMessage("Please open location");
                ad.SetCancelable(false);
                ad.SetCanceledOnTouchOutside(false);
                ad.SetButton("ok", delegate
                {
                    Android.Content.Context ctx = Forms.Context;
                    ctx.StartActivity(new Intent(Android.Provider.Settings.ActionLocationSourceSettings));
                });

                ad.SetButton2("cancle", delegate
                {

                });
                ad.Show();

            }
        }
        private void Disconnect()
        {
            
        }
        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            /* In timer */
            if (bFlagSendData == true)
            {
                /* Increase counter */
                uCounterTimer++;
                /* Check interval timeout */
                if (uCounterTimer >= 100)/* 30ms */
                {
                    bFlagIntervalTimeout = true;
                    uCounterTimer = 0;
                }
            }
            /* In timer */
        }

        public async Task Connect(CancellationToken? cancellationToken)
        {
            listener();
        }
        public async Task Upgrade_OTA_Progress()
        {
            await Task.Run(() =>
            {
                try
                {
                    
                    
                }
                catch { }
            });
        }

        public async Task listener()
        {
            await Task.Run(() =>
            {
                byte[] read = new byte[1];
                try
                {
                    /* Running task */
                    _disconnectTokenSource = new CancellationTokenSource();
                    _running = true;
                    while (_running && !_disconnectTokenSource.IsCancellationRequested)
                    {
                        /* Sleep to another task can run */
                        Thread.Sleep(10);
                        /* Check flag OTA */
                        if (bFlagUpgradeOTA == false)
                        {
                            /* Check flag running process */
                            if (bRunningProcess == true)
                            {
                                /* Check bluetooth socket connection */
                                if (_bluetoothSocket.IsConnected)
                                {
                                    Stream inStream = _bluetoothSocket.InputStream;
                                    /* Check data on buffer or not? */
                                    /* Just get maximum 1000bytes per 10ms */
                                    if (inStream.IsDataAvailable())
                                    {
                                        _bluetoothSPPDataReceived = true;
                                        // get 1 newest byte every 10ms 
                                        //inStream.ReadAsync(ringBufReceivedData, 0, 1);
                                        /* Continue reading when buffer under 1kB or end of package happen */
                                        while ((inStream.IsDataAvailable()) && (bNumberBytesPerPackageReceivedCounter < 1000))
                                        //while (inStream.IsDataAvailable())
                                        {
                                            /* Read single byte every 10ms */
                                            //int bDataReceived;
                                            //bDataReceived = inStream.ReadByte();
                                            //ringBufReceivedData[bNumberBytesPerPackageReceivedCounter] = (byte)bDataReceived;
                                            if (inStream.CanRead)
                                            {
                                                /* Read byte and put to Ringbuf */
                                                byte bDataReceived;
                                                bDataReceived = (byte)inStream.ReadByte();
                                                ringBufReceivedData[bNumberBytesPerPackageReceivedCounter] = bDataReceived;
                                                /* Increase numberbyte received */
                                                bNumberBytesPerPackageReceivedCounter++;

                                                /* Push to ringbuf */
                                                RingBuFTestBuffer.PushBack(bDataReceived);
                                                bFlagPackageReady = true;
                                            };
                                        }
                                    }

                                    /* Check timeout */
                                    if ((bFlagIntervalTimeout == true) && (bFlagPackageReady == true))
                                    {
                                        bFlagPackageReady = false;
                                        /* Clear flag timeout */
                                        bFlagIntervalTimeout = false;
                                        bFlagSendData = false;
                                        /* Test read data from ringbuf */
                                        /* Get all data until reach bNumberBytesLastPackage */
                                        byte[] bReceiveByteRingbuf = new byte[4096];
                                        for (int i = 0; i < bNumberBytesPerPackageReceivedCounter; i++)
                                        {
                                            bReceiveByteRingbuf[i] = RingBuFTestBuffer.PopFront();
                                        }
                                        /* Display data */
                                        /* Copy to last package sent */
                                        Array.Copy(bReceiveByteRingbuf, 0, PackageReceived, 0, bNumberBytesPerPackageReceivedCounter);
                                        var hexS = BitConverter.ToString(PackageReceived, 0, bNumberBytesPerPackageReceivedCounter);

                                        /* Update UI */
                                        RunOnUiThread(() =>
                                        {
                                            /* Compare last package to change color */
                                            bool equalAB = tvPackageReceived.Text.SequenceEqual(hexS);
                                            if (equalAB == true)
                                            {
                                                if (tvPackageReceived.CurrentTextColor == Android.Graphics.Color.Black)
                                                {
                                                    tvPackageReceived.SetTextColor(Android.Graphics.Color.Red);
                                                }
                                                else
                                                {
                                                    tvPackageReceived.SetTextColor(Android.Graphics.Color.Black);
                                                }
                                            }
                                            else
                                            {
                                                /* Set color default */
                                                tvPackageReceived.SetTextColor(Android.Graphics.Color.Black);
                                            }
                                            /* Display */
                                            tvPackageReceived.Text = hexS.ToString();
                                        });

                                        /* Reset counter */
                                        bNumberBytesPerPackageReceivedCounter = 0;

                                        /* Clear all cache data */
                                        int BufferSize = RingBuFTestBuffer.Size;
                                        for (int i = 0; i < BufferSize; i++)
                                        {
                                            bReceiveByteRingbuf[i] = RingBuFTestBuffer.PopFront();
                                        }
                                    }
                                }
                            }
                        }
                        /*else
                        {
                            *//* Check flag running process *//*
                            if (bRunningProcess == true)
                            {
                                *//* Check bluetooth socket connection *//*
                                if (_bluetoothSocket.IsConnected)
                                {
                                    Stream inStream = _bluetoothSocket.InputStream;
                                    *//* Check data on buffer or not? */
                                    /* Just get maximum 1000bytes per 10ms *//*
                                    if (inStream.IsDataAvailable())
                                    {
                                        while (inStream.IsDataAvailable())
                                        {
                                            *//* Read single byte every 10ms *//*
                                            if (inStream.CanRead)
                                            {
                                                *//* Read byte and put to Ringbuf *//*
                                                byte bDataReceived;
                                                bDataReceived = (byte)inStream.ReadByte();
                                                ringBufOTAReceivedData[bCounterOTAReceive++] = bDataReceived;
                                                *//* Push to ringbuf *//*
                                                RingBufferOTA.PushBack(bDataReceived);
                                            };
                                        }
                                    }
                                }
                            }
                        }*/
                    };
                }
                catch (Exception ex)
                {
                    _bluetoothSocket?.Close();
                }
                finally
                {
                    _bluetoothSocket?.Close();
                }
            });
        }

        string sprintf(string input, params object[] inpVars)
        {
            int i = 0;
            input = Regex.Replace(input, "%.", m => ("{" + i++/*increase have to be on right side*/ + "}"));
            return string.Format(input, inpVars);
        }

        private async void FilePickerButton()
        {
            var file = await CrossFilePicker.Current.PickFile();
            if (file != null)
            {
                /* Display File name */
                UIResponse.Text = sprintf("File selected: %s", file.FileName);
                /* Get Content of file */
                FileFWcontents = file.DataArray;
            }
        }

        #region Private Fields
        private TextView NumberPackageReceived;
        private TextView NumberBytesReceived;
        private TextView NewestPackageReceived;
        private TextView NumberBytesLastPackageReceived;
        private TextView UIResponse;
        private TextView uTimeStart;
        private TextView uTimeEnd;
        private EditText uIntervalValue;
        private EditText uBytestPerPackageValue;
        private EditText StrInputDeviceName;
        private TextView tvTimeProcess;
        private TextView tvPackageReceived;
        private TextView tvPackageSent;
        private TextView tvNumberPackageReceivedExpect;
        private TextView tvNumberDataReceived;
        private TextView tvNumberDataReceivedExpect;
        private TextView tvNumberDataLastPackage;
        private TextView tvNumberPackageMissing;
        private TextView tvNumberBytesMissing;
        private TextView tvRateDataMissing;
        private TextView tvNumberPackageMissingFilter;
        private TextView tvNumberBytesMissingFilter;
        private TextView tvRateDataMissingFilter;
        private TextView tvSamplePackageMatchResult;
        private TextView tvFailMatchCounter;
        private TextView tvNumberPackageReceivedFilter;
        private TextView tvNumberDataReceivedFilter;
        private TextView tvDiffResult;
        private TextView tvIndexMissResult;
        private TextView tvDetectErrorResult;
        private TextView txtUserProfileCommandActive;
        private TextView txtUserProfileCommandDeactive;

        private Button buttonConnect, buttonDisconnect, buttonPickFile, buttonUpgrade, buttonBootloader;
        private TextView connected;

        private int bInterval;
        private int bBytesPerPackage;
        private int bRefreshUI;
        private int bRefreshGraph;
        private DateTime bDateTimeStart;
        private DateTime bDateTimeEnd;
        private DateTime bDateTimeCurrent;
        /* Variable for connection process */
        private System.Timers.Timer bTimerQuerryReceiveData;
        private bool bRunningProcess = false;
        private bool bFlagGetTimeStart = false;
        private bool bRunningGet1stPackage = false;
        private TimeSpan bRunTime;
        private int bProcessedTime;
        /* Variable for calculate data process */
        private int bNumberPackageExpect;
        private int bNumberPackageMissing;
        private int bNumberDataExpect;
        private int bNumberDataMissing;
        private float fNumberRateDataMissing;
        private BluetoothDevice _bluetoothDevice;
        private BluetoothSocket _bluetoothSocket;
        private bool _running;
        private bool _bluetoothSPPDataReceived = false;
        private int _bluetoothSPPGetFullPackageData = 0;
        private int bNumberPackageReceivedCounter = 0;
        private int bNumberPackageReceivedCounterFilter = 0;
        private int bNumberBytesReceivedCounter = 0;
        private int bNumberUnMatchPackage = 0;
        private int bNumberBytesPerPackageReceivedCounter = 0;
        private int bCounterOTAReceive = 0;
        private int bNumberBytesLastPackage = 0;
        private int bNumberBytesLastDiffCounter = 0;
        private int bNumberBytesReceivedCounterFilter = 0;
        private int bDetectErrorFail = 0;
        byte[] SamplePackage;
        private string pathLogger;
        private string filenameLogger;
        private CancellationTokenSource _disconnectTokenSource;
        private CancellationTokenSource _cancellationSource;
        private int _lastDataLen = 0;
        private byte[] ringBufReceivedData = new byte[4096];
        private byte[] ringBufOTAReceivedData = new byte[4096];
        private byte[] cacheBufReceiveData = new byte[4096];
        private byte[] PackageSent = new byte[2048];
        private byte[] PackageReceived = new byte[4096];
        private string strPackageSample = "---0123456789 We start test thoughput mode perfomance missing package happen or not? 9876543210---00";
        byte[] JumptoBootloader = { 0x0F, 0x00, 0x01, 0x24, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        /* EEG data and ACCE data */
        private TextView tvTP9, tvFP1, tvFP2, tvTP10;
        private TextView tvACCE_X, tvACCE_Y, tvACCE_Z;
        private TextView tvGYRO_X, tvGYRO_Y, tvGYRO_Z;

        /* Command protocol */
        /* Connect/Disconnect command */
        private byte[] ConnectProtocol = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xC0, 0, 0 };
        private byte[] DisconnectProtocol = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xC1, 0, 0 };

        /* Device Information command */
        private byte[] InfoGetProductIDCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA0, 0, 0 };
        private byte[] InfoGetHWVersionCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA1, 0, 0 };
        private byte[] InfoGetFWVersionCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA2, 0, 0 };
        private byte[] InfoGetBLVersionCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA3, 0, 0 };
        private byte[] InfoGetActiveCodeCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA9, 0, 0 };
        private byte[] InfoSetProductIDCommand = new byte[50];
        private byte[] InfoSetProductActiveCode = new byte[50];

        /* User Information command */
        private byte[] InfoGetUserNameCommand       = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA4, 0, 0 };
        private byte[] InfoGetUserIDCommand         = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA5, 0, 0 };
        private byte[] InfoGetUserTeleNumbCommand   = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA6, 0, 0 };
        private byte[] InfoGetUserMailCommand       = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA7, 0, 0 };
        private byte[] InfoGetUserAccPassCommand    = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xA8, 0, 0 };

        private byte[] InfoSetUserNameCommand = new byte[50];
        private byte[] InfoSetUserIDCommand = new byte[50];
        private byte[] InfoSetUserTeleNumbCommand = new byte[50];
        private byte[] InfoSetUserMailCommand = new byte[50];
        private byte[] InfoSetUserAccPassCommand = new byte[50];

        /* User Profile command */
        private byte[] InfoGetPhaseXSPKCommand      = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xAA, 0, 1, 1};
        private byte[] InfoSetPhaseXSPKCommand      = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xBA, 0, 3, 1, 0, 0};
        private byte[] InfoGetPhaseXVIBCommand      = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xAB, 0, 1, 1 };
        private byte[] InfoSetPhaseXVIBCommand      = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xBB, 0, 3, 1, 0, 0 };
        private byte[] InfoGetPhaseTimeUsingCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xAC};
        private byte[] InfoSetPhaseTimeUsingCommand = new byte[20];
        private byte[] InfoUserProfileActive        = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xAD };
        private byte[] InfoUserProfileDeactive      = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xBD };

        /* Media Control command */
        private byte[] MediaPlayStopCommand         = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE0, 0, 0 };
        private byte[] MediaPlayPauseCommand        = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE6, 0, 0 };
        private byte[] MediaNextCommand             = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE1, 0, 0 };
        private byte[] MediaPreviousCommand         = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE2, 0, 0 };
        private byte[] MediaIncreaseVolumeCommand   = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE3, 0, 0 };
        private byte[] MediaDecreaseVolumeCommand   = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE4, 0, 0 };
        private byte[] SongSelectionCommand         = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xAE, 1, 0, 0, 0 };
        private byte[] A2DPCommand                  = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xAF, 0, 0};
        private byte[] GetSpeakerLevel              = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xE7, 0, 0 };
        private byte[] SetSpeakerLevel = new byte[50];
        private byte[] GetVibratorLevel             = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xF7, 0, 0 };
        private byte[] SetVibratorLevel = new byte[50];

        /* Vibrator Control command */
        private byte[] VibratorPlayStopCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xF0, 0, 0 };
        private byte[] VibratorWeakCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xF2, 0, 0 };
        private byte[] VibratorStrongCommand = { 0x41, 0x54, 0x44, 0x57, 0x00, 0x01, 0xF1, 0, 0 };

        /* Data variable */
        private string StrDataInput;
        private byte[] StrDataAddToFrame;

        /* For graph Speaker */
        private PlotView Speakerview;
        /* For graph Vibrator */
        private PlotView Vibratorview;

        /* For protocol*/
        private bool bFlagSendData = false;
        private bool bFlagIntervalTimeout = false, bFlagPackageReady = false;
        private int uCounterTimer = 0;

        /* For check 1st byte of header frame */
        int  bIndex = 0;
        byte bCheck1stHeaderByte =0;
        int  bCounterGetPackageHeader = 0;
        int  bFlagStartGetHeader = 0;
        bStateMachineReceivingData bStateFindPackageInRingBuffer = bStateMachineReceivingData.eSM_Step0;
        int bCurrentSequence = 0, bLastSequence = 0, bMissingRealPackageCounter=0;
        byte[] strSequence = new byte[2];
        MessageIds MSG_ID = 0;
        string strMissingSequence;
        /* Data value */
        float EEGTP9, EEGFP1, EEGFP2, EEGTP10;
        float GYRO_X, GYRO_Y, GYRO_Z, ACCE_X, ACCE_Y, ACCE_Z;

        private bool bFlagGetUserDataInput = false;
        /// <summary>
        /// Gets the plot controller.
        /// </summary>
        public IPlotController Controller { get; private set; }

        /* Component Index */
        private Component_Index pComIndex;
        private enum Component_Index
        {
            /* Information index */
            CoIdx_ProductID = 1,
            CoIdx_UserName  = 2,
            CoIdx_UserID    = 3,
            CoIdx_UserPass  = 4,
            CoIdx_UserTel   = 5,
            CoIdx_UserMail  = 6,
            CoIdx_ProductActiveCode = 7,

            CoIdx_SpeakerLevelPhase1= 8,
            CoIdx_SpeakerLevelPhase2 = 9,
            CoIdx_SpeakerLevelPhase3 = 10,
            CoIdx_SpeakerLevelPhase4 = 11,
            CoIdx_SpeakerLevelPhase5 = 12,

            CoIdx_SpeakerTimingPhase1 = 13,
            CoIdx_SpeakerTimingPhase2 = 14,
            CoIdx_SpeakerTimingPhase3 = 15,
            CoIdx_SpeakerTimingPhase4 = 16,
            CoIdx_SpeakerTimingPhase5 = 17,

            CoIdx_VibratorLevelPhase1 = 18,
            CoIdx_VibratorLevelPhase2 = 19,
            CoIdx_VibratorLevelPhase3 = 20,
            CoIdx_VibratorLevelPhase4 = 21,
            CoIdx_VibratorLevelPhase5 = 22,

            CoIdx_VibratorTimingPhase1 = 23,
            CoIdx_VibratorTimingPhase2 = 24,
            CoIdx_VibratorTimingPhase3 = 25,
            CoIdx_VibratorTimingPhase4 = 26,
            CoIdx_VibratorTimingPhase5 = 27,

            CoIdx_TimeUsing = 28,
            CoIdx_SpeakerLevel = 29,
            CoIdx_VibratorLevel = 30,
        }
        private enum Cmd_Type
        {
            /*Use for PC To TAG*/
            P2TCMD_CONNECT = 0xC0,
            P2TCMD_DISCONNECT = 0xC1,
            P2TCMD_START_DOWNLOAD_FIRMWARE = 0xC2,
            P2TCMD_DOWNLOAD_FIRMWARE = 0xC3,
            P2TCMD_END_DOWNLOAD_FIRMWARE = 0xC4,
            P2TCMD_RUN_FIRMWARE = 0xC5,
            P2TCMD_GET_CHECKSUM = 0xC6,

            /* Config Command  */
            P2TCMD_SET_CONFIG = 0xD1,/*Only Use for PC*/
            P2TCMD_GET_CONFIG = 0xD2,/*Only Use for PC*/

            /* Support Smart Pillow */
            /* Get Information about device */
            P2TCMD_GET_PRODUCT_ID = 0XA0,
            P2TCMD_GET_HW_VER = 0XA1,
            P2TCMD_GET_FW_VER = 0XA2,
            P2TCMD_GET_BL_VER = 0XA3,
            P2TCMD_GET_PRODUCT_ACTIVE = 0XA9,
            /* Set Information about device */
            P2TCMD_SET_PRODUCT_ID = 0XB0,
            P2TCMD_SET_HW_VER = 0XB1,
            P2TCMD_SET_FW_VER = 0XB2,
            P2TCMD_SET_BL_VER = 0XB3,
            P2TCMD_SET_PRODUCT_ACTIVE = 0XB9,

            /* Get Information about user */
            P2TCMD_GET_USER_NAME = 0XA4,
            P2TCMD_GET_USER_ID = 0XA5,
            P2TCMD_GET_USER_TELE_NUM = 0XA6,
            P2TCMD_GET_USER_MAIL = 0XA7,
            P2TCMD_GET_USER_ACCPASS = 0XA8,

            /* Set Information about user */
            P2TCMD_SET_USER_NAME = 0XB4,
            P2TCMD_SET_USER_ID = 0XB5,
            P2TCMD_SET_USER_TELE_NUM = 0XB6,
            P2TCMD_SET_USER_MAIL = 0XB7,
            P2TCMD_SET_USER_ACCPASS = 0XB8,

            /* For Media control in SD card */
            P2TCMD_MEDIA_PLAYSTOP = 0XE0,
            P2TCMD_MEDIA_NEXT = 0XE1,
            P2TCMD_MEDIA_PREVIOUS = 0XE2,
            P2TCMD_MEDIA_INCREASE_VOL = 0XE3,
            P2TCMD_MEDIA_DECREASE_VOL = 0XE4,
            P2TCMD_MEDIA_TIMEPLAY = 0xE5,
            P2TCMD_MEDIA_GET_SPEAKER_LEVEL = 0xE7,
            P2TCMD_MEDIA_SET_SPEAKER_LEVEL = 0xE8,

            /* For Vibrator control */
            P2TCMD_VIB_PLAYSTOP = 0XF0,
            P2TCMD_VIB_INCREASE_INT = 0XF1,
            P2TCMD_VIB_DECREASE_INT = 0XF2,
            P2TCMD_VIB_GET_VIBRATOR_LEVEL = 0xF7,
            P2TCMD_VIB_SET_VIBRATOR_LEVEL = 0xF8,

            /* User Profile */
            P2TCMD_GET_SPEAKER_PHASE_X = 0XAA,
            P2TCMD_SET_SPEAKER_PHASE_X = 0XBA,
            P2TCMD_GET_VIBRATOR_PHASE_X = 0XAB,
            P2TCMD_SET_VIBRATOR_PHASE_X = 0XBB,

            /* TIme Using */
            P2TCMD_GET_TIME_USING = 0XAC,
            P2TCMD_SET_TIME_USING = 0XBC,

            /* Song duration and selection */
            P2TCMD_SONG_SELECTION = 0XAE,
            P2TCMD_GET_A2DP_STATE = 0XAF,
        };

        /* OTA feature variables */
        private bool bFlagPickfile = false;
        private bool bFlagUpgradeOTA = false;
        public CircularBuffer<byte>  RingBuFTestBuffer, RingBufferOTA;
        byte[] FileFWcontents;

        /* Frame index */
        const int iUART_PREAM_OFFSET_1 = 0;
        const int iUART_PREAM_OFFSET_2 = 1;
        const int iUART_PREAM_OFFSET_3 = 2;
        const int iUART_PREAM_OFFSET_4 = 3;
        const int iUART_LENGTH_OFFSET  = 4;
        const int LENGTH_SIZE = 2;
        const int iUART_SIZE_LOW = 4;
        const int iUART_SIZE_HIGH = 5;
        const int iUART_CMD = 6;
        const int iUART_IDX_LOW = 7;
        const int iUART_IDX_HIGH = 8;
        const int iUART_DATA = 9;
        const int iUART_DATA_PACKAGE = 11;

        /* Size of field */
        const int PRODUCT_NAME_SIZE = 20;
        const int PRODUCT_ID_SIZE = 4;
        const int FW_VERSION_SIZE = 12;
        const int BL_VERSION_SIZE = 12;
        const int HW_VERSION_SIZE = 12;
        const int EMERGENCY_CODE_SIZE = 5;
        const int CONFIG_PARA_SIZE = 2;
        const int SIZE_ACTIVE_CODE = 4;
        const int SIZE_PHONE_NUMBER = 15;
        /* For user info */
        const int SIZE_USER_NAME = 20;
        const int SIZE_ID_ACCOUNT = 10;
        const int SIZE_ACCOUNT_PASSWORD = 18;
        const int SIZE_USER_TELEPHONE = 16;
        const int SIZE_USER_MAIL = 30;
        const int SIZE_PHASE = 2;//1 byte for timing, 1 byte for level
        const int ALL_SIZE = 256;
        BluetoothSocket _socket = null;
        #endregion
    }

    public class BluetoothConnection
    {
        public void getAdapter() { this.thisAdapter = BluetoothAdapter.DefaultAdapter; }
        //public void getDevice() { this.thisDevice = (from bd in this.thisAdapter.BondedDevices where bd.Name == "DeltawaveBand-102" select bd).FirstOrDefault(); }
        public void getDevice(string bDeviceName) { this.thisDevice = (from bd in this.thisAdapter.BondedDevices where bd.Name == bDeviceName select bd).FirstOrDefault(); }
        public BluetoothAdapter thisAdapter { get; set; }
        public BluetoothDevice thisDevice { get; set; }
        public BluetoothSocket thisSocket { get; set; }
    }

}

