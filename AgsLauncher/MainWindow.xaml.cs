using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Input;
using DiscordRPC;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows.Data;
using System.Threading;

namespace GameLauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        installable
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
#pragma warning disable 0649 //you can comment this out if you want its just to fix 1 and 2
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        private string gameFolder;
        private long gamesize;
        private long gamefoldersize; //1
        private long gameexesize; //2
        int repairtime = 0;
        bool maximised = false;



        private LauncherStatus _status;
        private DiscordRpcClient client;

        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;
                    case LauncherStatus.installable:
                        PlayButton.Content = "Install";
                        break;
                    default:
                        break;
                }
            }
        }

        void Initialize()
        { 
        }

        public MainWindow()
        {
            InitializeComponent();
            /*
          Create a Discord client
          NOTE: 	If you are using Unity3D, you must use the full constructor and define
                   the pipe connection.
          */
            client = new DiscordRpcClient("917581646302183554");

            //Set the logger
            client.Logger = new DiscordRPC.Logging.ConsoleLogger() { Level = DiscordRPC.Logging.LogLevel.Warning };

            //Subscribe to events
            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };

            //Connect to the RPC
            client.Initialize();

            //Set the rich presence
            //Call this as many times as you want and anywhere in your code.
            client.SetPresence(new DiscordRPC.RichPresence()
            {
                Details = "",
                State = "In The Launcher",
                Assets = new Assets()
                {
                    LargeImageKey = "image_large",
                    LargeImageText = "Avery Game 4.2",
                }
            });
            this.DataContext = this;
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "1i9qQNqWOlQcdrZ0qD3NU7WzHKW4h54U_.zip");
            gameExe = Path.Combine(rootPath, "AveryGame\\WindowsNoEditor\\AveryGame.exe");
            gameFolder = Path.Combine(rootPath, "WindowsNoEditor");
            if (File.Exists(gameZip))
            {
                gamesize = new FileInfo("1i9qQNqWOlQcdrZ0qD3NU7WzHKW4h54U_.zip").Length;
            }
            }

        /*public string test = "a string";
        public string Test
        {
            get { return test; }
            set { test = value; }
        }
        */


        /*
        {
            string message = "Are You Sure You Want To Exit?";
            string caption = "Exit";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxResult result;
            result = MessageBox.Show(message, caption, buttons);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
            else if (result == MessageBoxResult.No)
            {

            }
        } */

        private void CheckForUpdates()
        {


            if (Directory.Exists(gameFolder) && !File.Exists(versionFile))
            {
                try
                {

                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1wfD44CyNOWy3wYE2u4OI0FDIRnYgMzl4"));
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("An Unexpected Error Occurred And Was Fixed. The Launcher Will Now Restart.", "Error Occurred", buttons);
                    Process.Start("AgsLauncher.exe");
                    this.Close();
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"An Unexpected Error Occured: {ex}");
                }
            }

            else if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1wfD44CyNOWy3wYE2u4OI0FDIRnYgMzl4"));

                    if (onlineVersion.IsDifferentThan(localVersion) && !File.Exists(gameFolder))
                    {
                        Status = LauncherStatus.installable;
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                Status = LauncherStatus.installable;
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1wfD44CyNOWy3wYE2u4OI0FDIRnYgMzl4"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://www.googleapis.com/drive/v3/files/1i9qQNqWOlQcdrZ0qD3NU7WzHKW4h54U_?alt=media&key=AIzaSyD3hsuSxEFnxZkgadbUSPt_iyx8qJ4lwWQ"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
            string ClientUsername = client.CurrentUser.Username.ToString();
        }

        public void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void ClientUsername(object sender, EventArgs e)
        {
            client.CurrentUser.Username.ToString();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)

        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "\\AveryGame\\WindowsNoEditor");
                Process.Start(Path.Combine(rootPath, gameExe));
                this.WindowState = WindowState.Minimized;
                new ToastContentBuilder()
                       .AddArgument("action", "viewConversation")
                       .AddArgument("conversationId", 9813)
                       .AddText("Avery Game has been started successfully!")
                       .Show();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }

            else if (Status == LauncherStatus.installable)
            {
                if (File.Exists(versionFile))
                {
                    Version localVersion = new Version(File.ReadAllText(versionFile));

                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1wfD44CyNOWy3wYE2u4OI0FDIRnYgMzl4"));
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        InstallGameFiles(false, onlineVersion);
                    }
                }
                else
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1wfD44CyNOWy3wYE2u4OI0FDIRnYgMzl4"));
                    InstallGameFiles(false, onlineVersion);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (Status == LauncherStatus.ready || Status == LauncherStatus.installable)
            {
                this.Close();
            }
            else
            {
                string message = "Are You Sure You Want To Exit? This Will Cancel Any Current Installs.";
                string caption = "Exit Confirmation";
                MessageBoxButton buttons = MessageBoxButton.YesNo;
                MessageBoxResult result;
                result = MessageBox.Show(message, caption, buttons);
                if (result == MessageBoxResult.Yes)
                {
                    this.Close();
                }
                else
                {

                }
            }
        }

        private void close(object sender, RoutedEventArgs e)
        {
            if (Status == LauncherStatus.downloadingUpdate || Status == LauncherStatus.downloadingGame)
            {
                if (Status == LauncherStatus.downloadingUpdate)
                {
                    string msg1 = "AveryGame is currently updating. Are you sure you would like to exit?";
                    string text1 = "Warning";
                    MessageBoxButton buttons1 = MessageBoxButton.YesNo;
                    MessageBoxImage icon1 = MessageBoxImage.Warning;
                    MessageBoxResult result1;
                    result1 = MessageBox.Show(msg1, text1, buttons1, icon1);
                    if (result1 == MessageBoxResult.Yes)
                    {
                        this.Close();
                    }
                    else
                    {

                    }
                }
                if (Status == LauncherStatus.downloadingGame)
                {
                    string msg1 = "AveryGame is currently downloading. Are you sure you would like to exit?";
                    string text1 = "Warning";
                    MessageBoxButton buttons1 = MessageBoxButton.YesNo;
                    MessageBoxImage icon1 = MessageBoxImage.Warning;
                    MessageBoxResult result1;
                    result1 = MessageBox.Show(msg1, text1, buttons1, icon1);
                    if (result1 == MessageBoxResult.Yes)
                    {
                        this.Close();
                    }
                    else
                    {

                    }
                }
            }
            else
            {
                this.Close();  
            }
        }

        private void minimize(object sender, RoutedEventArgs e)
        {
            if (maximised == false)
            {
                WindowState = WindowState.Maximized;
                maximised = true;
            }
            else
            {
                WindowState = WindowState.Normal;
                maximised = false;
            }
        }
        
        private void minToTB(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void drag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        /*  private void Uninstall_Click(object sender, RoutedEventArgs e)
          {
              if (Status == LauncherStatus.ready)
              {
                  MessageBoxButton confirmbutton = MessageBoxButton.YesNo;
                  MessageBoxResult confirmresult;
                  confirmresult = MessageBox.Show("Are You Sure You Would Like To Uninstall The Game?", "Uninstall Confirmation", confirmbutton);
                  if (confirmresult == MessageBoxResult.Yes)
                  {
                      try
                      {
                          MessageBoxButton uninstallbutton = MessageBoxButton.OK;
                          MessageBoxResult uninstallresult;
                          uninstallresult = MessageBox.Show("Uninstall Success!", "Uninstall Finished", uninstallbutton);
                          Directory.Delete("WindowsNoEditor");
                      } catch(Exception ex)
                      {
                          MessageBox.Show($"Error Uninstalling: {ex}");
                      }

                  } 
                  else
                  {

                  }
              }
          } */


        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(versionFile))
            {
                if (repairtime < 10)
                {
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("Nothing Found To Repair!", "Repair Finish", buttons);
                    repairtime += 1;
                }
                else if (repairtime == 10)
                {
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("What", "", buttons);
                    repairtime += 1;
                }
                else if (repairtime > 10 && repairtime < 20)
                {
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("Nothing Found To Repair!", "Repair Finish", buttons);
                    repairtime += 1;
                }
                else if (repairtime == 20)
                {
                    // why
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("ok we get it you like finding secrets", "", buttons);
                    result = MessageBox.Show("but like cmon now", "", buttons);
                    result = MessageBox.Show("do you really think theres more secrets after this?", "", buttons);
                }
                else if (repairtime > 20 && repairtime < 45)
                {
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("Nothing Found To Repair!", "Repair Finish", buttons);
                }
                else if (repairtime == 45)
                {
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("ok fine ill give you a secret", "", buttons);
                    result = MessageBox.Show("press ctrl + c on the launcher screen.", "", buttons);
                    result = MessageBox.Show("ok bye now.", "", buttons);
                }
            }
            else if (File.Exists(gameZip) && gamesize < 2000000000)
            {
                try
                {
                    File.Delete(gameZip);
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("An Unexpected Error Occurred And Was Fixed. The Launcher Will Now Restart.", "Error Occurred", buttons);
                    Process.Start("AgsLauncher.exe");
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Fatal Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (Directory.Exists(gameFolder) && gamefoldersize < 2000000000)
            {
                try
                {

                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("An Unexpected Error Occurred With The Game Install. Please Delete The Game Folder \"WindowsNoEditor\" And The \"Version.txt\" File, And Restart The Launcher.", "Error Occurred", buttons);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Fatal Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (File.Exists(gameExe) && gameexesize < 150000)
            {
                try
                {
                    MessageBoxButton buttons = MessageBoxButton.OK;
                    MessageBoxResult result;
                    result = MessageBox.Show("An Unexpected Error Occurred With The Game Install. Please Delete The Game Folder \"WindowsNoEditor\" And The \"Version.txt\" File, And Restart The Launcher.", "Error Occurred", buttons);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Fatal Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        //PLEASE DONT CHANGE A SINGLE THING ABOUT THIS CODE BELOW
        public string Test1()
        {
            Thread.Sleep(1000);
            if (this.client.CurrentUser == null)
            {
                return "😊";
            }
            return this.client.CurrentUser.Username;
        }
        public string Test
        {
            get { return Test1(); }
            set { value = Test1(); }
        }
        //THE CODE ABOVE IS WHAT I GOT WORKING AFTER 3 HOURS OF TRYING
    }




    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
