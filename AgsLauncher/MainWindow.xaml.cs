using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;

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
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        private string gameFolder;
        private long gamesize;
        private long gamefoldersize;
        private long gameexesize;
        int repairtime = 0;


        private LauncherStatus _status;
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

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "1i9qQNqWOlQcdrZ0qD3NU7WzHKW4h54U_.zip");
            gameExe = Path.Combine(rootPath, "WindowsNoEditor", "AveryGame.exe");
            gameFolder = Path.Combine(rootPath, "WindowsNoEditor");
            if (File.Exists(gameZip))
            {
                gamesize = new FileInfo("1i9qQNqWOlQcdrZ0qD3NU7WzHKW4h54U_.zip").Length;
            }
            if (File.Exists(gameFolder))
            {
                gamefoldersize = new FileInfo(gameFolder).Length;
                gameexesize = new FileInfo(gameExe).Length;
            }
        }

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
                WebClient webClient = new WebClient();
                Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1wfD44CyNOWy3wYE2u4OI0FDIRnYgMzl4"));
                MessageBoxButton buttons = MessageBoxButton.OK;
                MessageBoxResult result;
                result = MessageBox.Show("An Unexpected Error Occurred And Was Fixed. The Launcher Will Now Restart.", "Error Occurred", buttons);
                Process.Start("AgsLauncher.exe");
                this.Close();
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
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
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

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "WindowsNoEditor");
                Process.Start(startInfo);

                Close();
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

                File.Delete(gameZip);
                MessageBoxButton buttons = MessageBoxButton.OK;
                MessageBoxResult result;
                result = MessageBox.Show("An Unexpected Error Occurred And Was Fixed. The Launcher Will Now Restart.", "Error Occurred", buttons);
                Process.Start("AgsLauncher.exe");
                this.Close();
            }
            else if (Directory.Exists(gameFolder) && gamefoldersize < 2000000000)
            {
                MessageBoxButton buttons = MessageBoxButton.OK;
                MessageBoxResult result;
                result = MessageBox.Show("An Unexpected Error Occurred With The Game Install. Please Delete The Game Folder \"WindowsNoEditor\" And The \"Version.txt\" File, And Restart The Launcher.", "Error Occurred", buttons);
                this.Close();
            }
            else if (File.Exists(gameExe) && gameexesize < 150000)
            {
                MessageBoxButton buttons = MessageBoxButton.OK;
                MessageBoxResult result;
                result = MessageBox.Show("An Unexpected Error Occurred With The Game Install. Please Delete The Game Folder \"WindowsNoEditor\" And The \"Version.txt\" File, And Restart The Launcher.", "Error Occurred", buttons);
                this.Close();
            }
        }
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
