using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
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
using System.Windows.Threading;

namespace BilibiliCommentsLottery
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        int TotalCommentsCount;
        string oid;
        List<string> AllCommentsNickName = new List<string>();

        public JObject ReadJson(string json)
        {
            
            JObject jsondata = (JObject)JsonConvert.DeserializeObject(json);
            return jsondata;
        }

        public string GetHttp(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.ReadWriteTimeout = 5000;
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent = "WebKit/537.36";
            request.Headers.Add("Cache-Control:no-cache;max-age=0");
            //request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))
            {
                myResponseStream = new GZipStream(myResponseStream, CompressionMode.Decompress);
            }
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            return retString;
        }

        public string GetOid(string vid)
        {
            string url = @"https://www.bilibili.com/video/" + @vid;
            string oid = GetHttp(url);
            oid = oid.Substring(oid.IndexOf("aid=") + 4);
            oid = oid.Substring(0, oid.IndexOf("&"));
            return oid;
        }

        public string GetTitle(string vid)
        {
            string url = @"https://www.bilibili.com/video/" + @vid;
            string title = GetHttp(url);
            title = title.Substring(title.IndexOf("\"title\":\"") + 9);
            title = title.Substring(0, title.IndexOf("\""));
            return title;
        }

        public List<string> GetAllCommentsNickName(string oid)
        {
            string url, CommentsNickName;
            JObject temp;
            int Pnsize, CommentsCount = 0, maxPn, p, i;
            url = @"https://api.bilibili.com/x/v2/reply?pn=1&ps=49&type=1&oid=" + @oid;
            temp = ReadJson(GetHttp(url));
            Pnsize = Convert.ToInt32(temp["data"]["page"]["size"]);
            TotalCommentsCount = Convert.ToInt32(temp["data"]["page"]["count"]);
            maxPn = (int)Math.Ceiling((decimal)TotalCommentsCount / Pnsize);
            for (p = 1; p <= maxPn; p++)
            {
                url = @"https://api.bilibili.com/x/v2/reply?pn=" + Convert.ToString(p) + "&ps=49&type=1&oid=" + @oid;
                temp = ReadJson(GetHttp(url));
                for (i = 0; i < Pnsize; i++)
                {
                    if (CommentsCount < TotalCommentsCount)
                    {
                        CommentsCount++;
                        CommentsNickName = (string)temp["data"]["replies"][i]["member"]["uname"];
                        AllCommentsNickName.Add(CommentsNickName);
                    }
                    else
                    {
                        break;
                    }
                }
                oput.Text += "已加载......" + p + "/" + maxPn + "\r\n";
                oput.ScrollToEnd();
                DoEvents();
                Thread.Sleep(1000);
            }
            return AllCommentsNickName;
        }

        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static Object ExitFrame(Object state)
        {
            ((DispatcherFrame)state).Continue = false;
            return null;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            vid.Focus();
        }

        private void LoadComments_Click(object sender, RoutedEventArgs e)
        {
            oput.Text = "正在加载......\r\n加载速度过快可能会被服务器拦截哦！\r\n请耐心等待......\r\n";
            DoEvents();
            try
            {
                oid = GetOid(vid.Text);
                //oput.Text += oid + "\r\n";
                AllCommentsNickName = GetAllCommentsNickName(oid);
                //for (int i = 0; i < AllCommentsNickName.Length; i++)
                //{
                //    oput.Text += AllCommentsNickName[i] + "\r\n";
                //}
                oput.Text += "\r\n已经加载完成啦！\r\n可能很久很久之前投稿的视频的数据是有问题的哦\r\n请确认一下下面的标题吧：\r\n";
                oput.Text += GetTitle(vid.Text) + "\r\n";
                oput.ScrollToEnd();
            }
            catch (Exception)
            {
                oput.Text = null;
                MessageBox.Show("你的输入可能有误哦~", "错误：", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartLottery_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            int rad;
            List<string> list = AllCommentsNickName;
            list = list.Union(list).ToList();
            if (list.Count != 0)
            {
                if (Convert.ToInt32(lotteryCount.Text) > list.Count)
                {
                    MessageBox.Show("你的评论人数可没有那么多哦！", "信息：", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (Convert.ToInt32(lotteryCount.Text) == list.Count)
                {
                    MessageBox.Show("你的评论人数也就" + lotteryCount.Text + "个……你还抽啥呢！？", "信息：", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    oput.Text = "以下是抽奖名单：\r\n";
                    for (int i = 0; i < Convert.ToInt32(lotteryCount.Text); i++)
                    {
                        rad = rand.Next(0, TotalCommentsCount - 1 - i);
                        oput.Text += list[rad] + "\r\n";
                        oput.ScrollToEnd();
                        list.Remove(list[rad]);
                    }
                }
            }
            else
            {
                MessageBox.Show("你可能还没有加载评论呢！", "信息：", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void vid_GotFocus(object sender, RoutedEventArgs e)
        {
            StartLottery.IsDefault = false;
            LoadComments.IsDefault = true;
        }

        private void vid_LostFocus(object sender, RoutedEventArgs e)
        {
            StartLottery.IsDefault = false;
            LoadComments.IsDefault = false;
        }

        private void lotteryCount_GotFocus(object sender, RoutedEventArgs e)
        {
            StartLottery.IsDefault = true;
            LoadComments.IsDefault = false;
        }

        private void lotteryCount_LostFocus(object sender, RoutedEventArgs e)
        {
            StartLottery.IsDefault = false;
            LoadComments.IsDefault = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
