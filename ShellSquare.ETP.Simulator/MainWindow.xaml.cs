using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using System.Runtime.InteropServices;
using Energistics.Etp.v11.Datatypes.ChannelData;
using SuperSocket.SocketBase;
using ETPSimulatorApp;
using Energistics.Etp.v11.Datatypes;
using Energistics.Etp.v11;

namespace ShellSquare.ETP.Simulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int m_MessageCount;
        private NotifyObservableCollection<ChannelItem> m_ChannelItems;
        private CancellationTokenSource m_Source;
        private CancellationToken m_Token;

        ETPHandler m_ConsumerHandler;
        ETPHandler m_ProducerHandler;

        //ConsumerHandler m_ConsumerHandler;
        //ProducerHandler m_ProducerHandler;

        private SolidColorBrush m_Brush;
        private List<UserDetails> userDataList = new List<UserDetails>();

        public ChannelMetadata Metadata;

        public MainWindow()
        {
            InitializeComponent();

            m_ChannelItems = new NotifyObservableCollection<ChannelItem>();
            Channels.ItemsSource = m_ChannelItems;

            m_Brush = new SolidColorBrush();
            m_Brush.Color = Color.FromRgb(133, 173, 173);
            LoadPreference();

            m_MessageCount = 5000;

            m_Source = new CancellationTokenSource();
            m_Token = m_Source.Token;

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;


            UserName.Text = "anoop";
            UserPassword.Password = "develop@123";
        }



        private async Task ConsumerConnect()
        {

            try
            {
                string url = EtpUrl.Text;
                string userName = UserName.Text;
                string password = UserPassword.Password;
                string startIndex = "200";
                int maxDataItems = 10000;
                int maxMessageRate = 1000;

                string message = $"Connecting to {url}.\nuser name {userName}.";


                var protocols = new List<SupportedProtocol>();

                SupportedProtocol p;
                p = ETPHandler.ToSupportedProtocol(Protocols.ChannelStreaming, "producer");
                protocols.Add(p);

                p = ETPHandler.ToSupportedProtocol(Protocols.Discovery, "store");
                protocols.Add(p);


                m_ConsumerHandler = new ETPHandler(startIndex);
                //m_ConsumerHandler = new ConsumerHandler();


                await Display(message);

                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        m_ConsumerHandler.Message = Message;
                        m_ConsumerHandler.ChannelInfoReceived += ChannelMeta;
                        await m_ConsumerHandler.Connect(url, userName, password, protocols, m_Token);

                    }
                    catch (Exception ex)
                    {

                    }
                });
            }
            catch (Exception ex)
            {
                // await DisplayError(ex.Message);
                // SetButtonStatus("Connect");
            }
        }


        private void EtpUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBox menuItem = (ComboBox)sender; ;

                foreach (var d in userDataList)
                {
                    if (d?.Url == menuItem.SelectedItem?.ToString())
                    {
                        UserName.Text = d.UserName;
                        UserPassword.Password = d.Password;
                        EtpUrl.Text = d.Url;
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message).Wait();
            }
        }


        private void SaveToPreference(string url, string userName, string password)
        {
            if (userDataList.Where(x => x.Url.Equals(url)).Count() == 0)
            {
                UserDetails user = new UserDetails();
                user.Url = url;
                user.UserName = userName;
                user.Password = password;
                userDataList.Add(user);
            }
            else if (userDataList.Where(x => x.Url.Equals(url) && (x.UserName != userName || x.Password != password)).Count() > 0)
            {
                UserDetails user = userDataList.Where(x => x.Url.Equals(url)).FirstOrDefault();
                user.Url = url;
                user.UserName = userName;
                user.Password = password;
            }

            string json = JsonConvert.SerializeObject(userDataList);
            Properties.Settings.Default.UserCredentials = json;
            Properties.Settings.Default.Save();
        }

        private void LoadPreference()
        {

            string jsonString = Properties.Settings.Default.UserCredentials;
            userDataList = JsonConvert.DeserializeObject<List<UserDetails>>(jsonString);
            var userDetail = userDataList?.FirstOrDefault();
            EtpUrl.Text = userDetail?.Url;
            UserName.Text = userDetail?.UserName;
            UserPassword.Password = userDetail?.Password;
            List<string> detail = new List<string>();
            if (userDataList == null)
            {
                userDataList = new List<UserDetails>();
            }
            foreach (var item in userDataList)
            {
                detail?.Add(item.Url);
            }
            EtpUrl.ItemsSource = detail;

            //if (string.IsNullOrWhiteSpace(EtpUrl.Text))
            //{
            //    EtpUrl.Text = "";
            //}
        }



        private async Task DisplayError(string message)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                string timeMessage = $"{DateTime.UtcNow.ToString("o")}";

                Paragraph p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0, 10, 0, 0);

                p = new Paragraph(new Run(message));
                p.Foreground = Brushes.Red;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                MessageDisplay.ScrollToEnd();
            });
        }

        private async Task Display(string message)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                string timeMessage = $"{DateTime.UtcNow.ToString("o")}";

                Paragraph p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0, 10, 0, 0);

                p = new Paragraph(new Run(message));
                p.Foreground = Brushes.Black;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                MessageDisplay.ScrollToEnd();
            });
        }




        private async Task Display(string message, double timeTaken, TraceLevel level)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                string timeMessage = $"{DateTime.UtcNow.ToString("o")}";

                Paragraph p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0, 10, 0, 0);

                timeMessage = $"Time taken : {timeTaken} (ms)";

                p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                if (level == TraceLevel.Error)
                {
                    p = new Paragraph(new Run(message));
                    p.Foreground = Brushes.Red;
                    MessageDisplay.Document.Blocks.Add(p);
                }
                else
                {
                    p = new Paragraph(new Run(message));
                    p.Foreground = Brushes.Black;
                    MessageDisplay.Document.Blocks.Add(p);
                }
                p.Margin = new Thickness(0);
                int totalCount = MessageDisplay.Document.Blocks.Count;

                if (totalCount > m_MessageCount)
                {
                    for (int i = totalCount - m_MessageCount; i > 0; i--)
                    {
                        MessageDisplay.Document.Blocks.Remove(MessageDisplay.Document.Blocks.ElementAt(i));
                    }
                }
                MessageDisplay.ScrollToEnd();
            });
        }


        private void ChannelMeta(ChannelMetadata metadata)
        {

            m_ConsumerHandler.Metadata = metadata;

            Channels.Dispatcher.InvokeAsync(() =>
            {
                try
                {

                    m_ChannelItems.Clear();
                    foreach (var c in metadata.Channels)
                    {

                        bool hasDepth = false;
                        bool hasTime = false;

                        foreach (var index in c.Indexes)
                        {
                            if (index.IndexType == Energistics.Etp.v11.Datatypes.ChannelData.ChannelIndexTypes.Depth)
                            {
                                hasDepth = true;
                            }

                            if (index.IndexType == Energistics.Etp.v11.Datatypes.ChannelData.ChannelIndexTypes.Time)
                            {
                                hasTime = true;
                            }
                        }

                        ChannelItem item = new ChannelItem()
                        {
                            Eml = c.ChannelUri,
                            Name = c.ChannelName,
                            Description = c.Description,
                            Uid = c.ChannelId.ToString(),
                            HasDepthIndex = hasDepth,
                            HasTimeIndex = hasTime,
                            Selected = true,
                            ChannelMetadataRecord = c
                        };

                        m_ChannelItems.Add(item);
                    }

                }
                catch (Exception ex)
                {
                    DisplayError(ex.Message).ConfigureAwait(true);
                }
            });


        }


        private async Task ProducerConnect()
        {
            try
            {
                string url = EtpUrl.Text;
                string userName = UserName.Text;
                string password = UserPassword.Password;
                string startIndex = "200";
                string message = $"Connecting to {url}.\nuser name {userName}.";


                var protocols = new List<SupportedProtocol>();

                SupportedProtocol p;
                p = ETPHandler.ToSupportedProtocol(Protocols.ChannelStreaming, "consumer");
                protocols.Add(p);

                p = ETPHandler.ToSupportedProtocol(Protocols.Discovery, "store");
                protocols.Add(p);


                m_ProducerHandler = new ETPHandler(startIndex);


                Display(message);

                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        m_ProducerHandler.Message = Message;
                        await m_ProducerHandler.Connect(url, userName, password, protocols, m_Token);

                    }
                    catch (Exception ex)
                    {

                    }

                });

            }
            catch (Exception ex)
            {
                // await DisplayError(ex.Message);
                // SetButtonStatus("Connect");
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

            await ConsumerConnect();
            await ProducerConnect();

            //try
            //{
            //    string url = EtpUrl.Text;
            //    string userName = UserName.Text;
            //    string password = UserPassword.Password;
            //    int maxDataItems = int.Parse(MaxItems.Text);
            //    int maxMessageRate = int.Parse(PollingRate.Text);

            //    string message = $"Connecting to {url}.\nuser name {userName}.";

            //    m_ConsumerHandler = new ConsumerHandler();
            //    m_ConsumerHandler.Message = Message;
            //    m_ConsumerHandler.ChannelInfoReceived += ChannelMeta;
            //    await m_ConsumerHandler.Connect(url, userName, password, m_Token);


            //    m_ProducerHandler = new ProducerHandler();
            //    m_ProducerHandler.Message = Message;
            //    await m_ProducerHandler.Connect(url, userName, password, m_Token);
            //    SaveToPreference(url, userName, password);
            //}
            //catch (Exception ex)
            //{
            //    await DisplayError(ex.Message);
            //}
        }


        private void ChannelStreamingInfo(ChannelMetadata Channel)
        {

            string a = "";
        }
        private async void Message(string message, double timeTaken, TraceLevel level = TraceLevel.Info)
        {
            await Display(message, timeTaken, level);
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_ConsumerHandler == null)
                {
                    await DisplayError("Use connect button to connect first");
                    return;
                }

                //new Code
                int maxDataItems = 10000;
                int maxMessageRate = 1000;
                await m_ConsumerHandler.Start(maxDataItems, maxMessageRate);
                ////Till Here
                List<string> channels = new List<string>();
                channels.Add(DescribeEml.Text);
                await m_ConsumerHandler.Describe(channels);
                //await m_ProducerHandler.SendChannelMetadata(m_ConsumerHandler.Metadata);

            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }
        private async void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_ProducerHandler == null)
                {
                    await DisplayError("Use connect button to connect first");
                    return;
                }
                List<ChannelMetadataRecord> timeRecords = new List<ChannelMetadataRecord>();
                List<ChannelMetadataRecord> depthRecords = new List<ChannelMetadataRecord>();
                List<ChannelMetadataRecord> timeAndDepthRecords = new List<ChannelMetadataRecord>();
                ChannelStreamingInfo channelStreamingInfo = new ChannelStreamingInfo();
                List<ChannelStreamingInfo> lstChannelStreamingInfo = new List<ChannelStreamingInfo>();
                m_ProducerHandler.ChannelInfoReceived += ChannelStreamingInfo;
                foreach (var item in m_ChannelItems)
                {
                    if (item.Selected)
                    {
                        if (item.HasDepthIndex && item.HasTimeIndex)
                        {
                            timeAndDepthRecords.Add(item.ChannelMetadataRecord);
                        }
                        else if (item.HasTimeIndex)
                        {
                            ///??Only for time??
                            channelStreamingInfo = new ChannelStreamingInfo()
                            {
                                ChannelId = item.ChannelMetadataRecord.ChannelId,
                                StartIndex = new StreamingStartIndex()
                                {
                                    Item = null
                                },
                                ReceiveChangeNotification = true
                            };
                            timeRecords.Add(item.ChannelMetadataRecord);
                        }
                        else if (item.HasDepthIndex)
                        {
                            depthRecords.Add(item.ChannelMetadataRecord);
                        }
                        lstChannelStreamingInfo.Add(channelStreamingInfo);
                    }
                }

                if (timeRecords.Count > 0)
                {
                    //string message = $"\nRequest: [Protocol {} MessageType {}]";
                    //Message?.Invoke("", 0, TraceLevel.Info);
                    //await m_ProducerHandler.Connect(EtpUrl .Text , UserName .Text , UserPassword .Password , protocols, m_Token);

                    await m_ProducerHandler.SendChannelData(lstChannelStreamingInfo);

                }
            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }

        private void ChannelsGridSelect_Click(object sender, RoutedEventArgs e)
        {
            m_ChannelItems.BeginChange();
            foreach (var item in Channels.SelectedItems)
            {
                ((ChannelItem)item).Selected = true;
            }
            m_ChannelItems.EndChange();
        }

        private void ChannelsGridDeselect_Click(object sender, RoutedEventArgs e)
        {
            m_ChannelItems.BeginChange();
            foreach (var item in Channels.SelectedItems)
            {
                ((ChannelItem)item).Selected = false;
            }
            m_ChannelItems.EndChange();
        }

        private void ChannelsGridSelectAll_Click(object sender, RoutedEventArgs e)
        {
            m_ChannelItems.BeginChange();
            foreach (var item in m_ChannelItems)
            {
                item.Selected = true;
            }
            m_ChannelItems.EndChange();
        }

        private void ChannelsGridDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            m_ChannelItems.BeginChange();
            foreach (var item in m_ChannelItems)
            {
                item.Selected = false;
            }
            m_ChannelItems.EndChange();
        }
        private async void SetUpButton_Click(object sender, RoutedEventArgs e)
        {
            await m_ProducerHandler.SendChannelMetadata(m_ConsumerHandler.Metadata);
        }
        private async void Describe_Click(object sender, RoutedEventArgs e)
        {
            // List<string> url = new List<string>();
            // url.Add(DescribeEml.Text);

            //await  m_ConsumerHandler.Describe(url);
        }
    }
}
