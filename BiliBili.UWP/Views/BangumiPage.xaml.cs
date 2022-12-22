using BiliBili.UWP.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using BiliBili.UWP.Pages;
using System.Text.RegularExpressions;
using BiliBili.UWP.Pages.Season;
using BiliBili.UWP.Api.User;
using BiliBili.UWP.Api;
using Newtonsoft.Json.Linq;
using Windows.Web.Http.Filters;
using Windows.Web.Http;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace BiliBili.UWP.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class BangumiPage : Page
    {
        public BangumiPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (SettingHelper.Get_RefreshButton() && SettingHelper.IsPc())
            {
                b_btn_Refresh.Visibility = Visibility.Visible;
            }
            else
            {
                b_btn_Refresh.Visibility = Visibility.Collapsed;

            }
            if (e.NavigationMode == NavigationMode.New )
            {
                await Task.Delay(200);
                
                if (ApiHelper.IsLogin())
                {
                    
                    myban.Visibility = Visibility.Visible;
                    LoadMy();
                }
                else
                {
                    myban.Visibility = Visibility.Collapsed;
                    b_btn_Refresh.Visibility = Visibility.Collapsed;
                }
                if (list_ban_jp.ItemsSource == null)
                {
                    LoadHome();
                }
                
            }
            // await Task.Delay(200);
          

        }
        private async void LoadMy()
        {
            try
            {
                pr_Load.Visibility = Visibility.Visible;
                HttpBaseProtocolFilter hb = new HttpBaseProtocolFilter();
                HttpCookieCollection cookieCollection = hb.CookieManager.GetCookies(new Uri("https://bilibili.com")); // Get all cookies
                string cookie = "";
                foreach (HttpCookie item in cookieCollection)
                {
                    if (item.Name == "SESSDATA")
                    {
                        cookie = "SESSDATA=" + item.Value; // Get SESSDATA cookie
                    }
                }

                string url = string.Format("http://space.bilibili.com/ajax/Bangumi/getList?mid=" + ApiHelper.GetUserId());
                Dictionary<string, string> header = new Dictionary<string, string>();
                header.Add("Cookie", cookie); // Set header
                string result = await WebClientClass.GetResults(new Uri(url), header);
                FollowedBanModel model = JsonConvert.DeserializeObject<FollowedBanModel>(result);
                
                //var data = await result.GetJson<ApiResultModel<JObject>>();
                if (model.success)
                {
                    list_ban_mine.ItemsSource = (await model.data.result.ToString().DeserializeJson<List<FollowSeasonModel>>()).Take(9).ToList();
                }
                else
                {
                    Utils.ShowMessageToast(model.message);
                }

            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147012867 || ex.HResult == -2147012889)
                {
                    Utils.ShowMessageToast("无法连接服务器，请检查你的网络连接", 3000);
                }
                else
                {

                    Utils.ShowMessageToast("读取追番失败了", 3000);
                }
            }
            finally
            {
                pr_Load.Visibility = Visibility.Collapsed;
            }
        }
        private async void LoadHome()
        {
            try
            {
                pr_Load.Visibility = Visibility.Visible;
                string url = string.Format("https://bangumi.bilibili.com/appindex/follow_index_page?appkey={0}&build=5250000&mobi_app=android&platform=wp&ts={1}000",ApiHelper.AndroidKey.Appkey,ApiHelper.GetTimeSpan);
                url += "&sign=" + ApiHelper.GetSign(url);
                string results = await WebClientClass.GetResultsUTF8Encode(new Uri(url));
                BangumiHomeModel m = JsonConvert.DeserializeObject<BangumiHomeModel>(results);
                if (m.code==0)
                {
                    this.DataContext = m;
                }
                else
                {
                    Utils.ShowMessageToast(m.message, 3000);
                }
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147012867 || ex.HResult == -2147012889)
                {
                    Utils.ShowMessageToast("无法连接服务器，请检查你的网络连接", 3000);
                }
                else
                {

                    Utils.ShowMessageToast("读取推荐信息", 3000);
                }
            }
            finally
            {
                pr_Load.Visibility = Visibility.Collapsed;
            }
        }
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewBox2_num.Width = ActualWidth / 3 - 21;
        }

        private void btn_MyBan_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.SendNavigateTo(NavigateMode.Info, typeof(FollowSeasonPage), Modules.SeasonType.bangumi);
        }

        private void list_ban_mine_ItemClick(object sender, ItemClickEventArgs e)
        {
            MessageCenter.SendNavigateTo(NavigateMode.Info, typeof(BanInfoPage), (e.ClickedItem as FollowSeasonModel).season_id.ToString());
        }

        private void list_ban_jp_ItemClick(object sender, ItemClickEventArgs e)
        {
            MessageCenter.SendNavigateTo(NavigateMode.Info, typeof(BanInfoPage), (e.ClickedItem as BangumiHomeModel).season_id.ToString());
        }

        private async void list_ban_cn_foot_ItemClick(object sender, ItemClickEventArgs e)
        {
            //妈蛋，B站就一定要返回个链接么,就不能返回个类型加参数吗
            var link = (e.ClickedItem as BangumiHomeModel).link;
            if(!await MessageCenter.HandelUrl(link))
            {
                MessageCenter.SendNavigateTo(NavigateMode.Info, typeof(WebPage), link);
            }
        }

        private void btn_Timeline_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.SendNavigateTo(NavigateMode.Info, typeof(TimelinePage));
        }

        private void btn_tag_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.SendNavigateTo(NavigateMode.Info, typeof(SeasonIndexPage),new Modules.Season.SeasonIndexParameter() { 
                type= Modules.Season.IndexSeasonType.Anime
            });
        }

        private void btn_jp_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(JpBangumiPage));
        }

        private void btn_cn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CnBangumiPage));
        }

        private void b_btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (ApiHelper.IsLogin())
            {
                myban.Visibility = Visibility.Visible;
                LoadMy();
            }
            else
            {
                myban.Visibility = Visibility.Collapsed;
            }
            if (list_ban_jp.ItemsSource == null)
            {
                LoadHome();
            }
        }

        private void PullToRefreshBox_RefreshInvoked(DependencyObject sender, object args)
        {
            if (ApiHelper.IsLogin())
            {
                myban.Visibility = Visibility.Visible;
                LoadMy();
            }
            else
            {
                myban.Visibility = Visibility.Collapsed;
            }
            if (list_ban_jp.ItemsSource == null)
            {
                LoadHome();
            }
        }

        public class FollowedBanModel
        {
            public int code { get; set; }
            private string _message;
            public string message
            {
                get
                {
                    if (string.IsNullOrEmpty(_message))
                    {
                        return msg;
                    }
                    else
                    {
                        return _message;
                    }
                }
                set { _message = value; }
            }
            public string msg { get; set; } = "";

            public bool success
            {
                get
                {
                    return code == 0;
                }
            }

            public object result { get; set; }

            public FollowedBanModel data { get; set; }
        }
    }
}
