using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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

namespace Upwork_Withdrawal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); 
        }
    }


    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : BaseViewModel
    {
        private CurrencyViewModel currencyVM;
        private ConverterViewModel converterVM;

        public CurrencyViewModel CurrencyVM { get { return currencyVM; } set { currencyVM = value; OnPropertyChanged(); } }
        public ConverterViewModel ConverterVM { get { return converterVM; } set { converterVM = value; OnPropertyChanged(); } }

        public MainViewModel()
        {
            CurrencyVM = new CurrencyViewModel();
            Update();
        }

        private void Update()
        {
            Task.Run(async () =>
            {
                var response = await UpdateCurrency();
                Stream responseStream = response.GetResponseStream();
                StreamReader sr = new StreamReader(responseStream, Encoding.UTF8);
                string JSON = sr.ReadToEnd();
                var dic = JsonConvert.DeserializeObject<Dictionary<string, float>>(JSON);
                CurrencyVM.USD = dic["USD_UAH"];
                CurrencyVM.UAH = dic["UAH_USD"];
                ConverterVM = new ConverterViewModel(CurrencyVM.USD, CurrencyVM.UAH, 2.0f, 2.5f, 2.5f, 0.035f);
            }).Wait();
        }

        private Task<WebResponse> UpdateCurrency()
        {
            WebRequest req = WebRequest.CreateDefault(new Uri("http://free.currencyconverterapi.com/api/v3/convert?q=USD_UAH,UAH_USD&compact=ultra"));
            return req.GetResponseAsync();
           
        }
    }

    public class CurrencyViewModel : BaseViewModel
    {
        private float usd;
        private float uah;

        public float USD { get { return usd; } set { usd = value; OnPropertyChanged(); } }
        public float UAH { get { return uah; } set { uah = value; OnPropertyChanged(); } }
    }

    public class ConverterViewModel : BaseViewModel
    {
        public float USD { get; set; }
        public float UAH { get; set; }
        public float UpworkFee { get; set; }
        public float ImmediateFee { get; set; }
        public float CashFee { get; set; }
        public float MasterCardFee { get; set; }

        private float withdraw;
        private float withdrawFeesFree;

        private float receive;
        private float receiveFeesFree;

        private bool upworkWithdrawal;
        private bool immediateWithdrawal;
        private bool cashWithdrawal;

        private bool toUAH = true;
        private bool isCalculating;

        public float Receive { get { return receive; } set { receive = value; OnPropertyChanged(); } }
        public float Withdraw { get { return withdraw; } set { withdraw = value; OnPropertyChanged(); } }
        

        public float WithdrawFeesFree { get { return withdrawFeesFree; } set { withdrawFeesFree = value; OnPropertyChanged(); } }
        public float ReceiveFeesFree { get { return receiveFeesFree; } set { receiveFeesFree = value; OnPropertyChanged(); } }



        public bool UpworkWithdrawal { get { return upworkWithdrawal; } set { upworkWithdrawal = value; OnPropertyChanged(); OnPropertyChanged("ImmediateWithdrawal"); } }
        public bool ImmediateWithdrawal { get { return UpworkWithdrawal && immediateWithdrawal; } set { immediateWithdrawal = value; OnPropertyChanged(); } }
        public bool CashWithdrawal { get { return cashWithdrawal; } set { cashWithdrawal = value; OnPropertyChanged(); } }

        public ConverterViewModel(float usdToUah, float uahToUsd, float upworkFee, float immediateFee, float cashFee, float masterCardFee)
        {
            USD = usdToUah;
            UAH = uahToUsd;
            UpworkFee = upworkFee;
            ImmediateFee = immediateFee;
            CashFee = cashFee;
            MasterCardFee = masterCardFee;
            PropertyChanged += ConverterViewModel_PropertyChanged;
        }

        private void ConverterViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isCalculating) return;
            if (e.PropertyName == "Withdraw")
                toUAH = true;
            if (e.PropertyName == "Receive")
                toUAH = false;
            Calculate(toUAH);
        }

        private void Calculate(bool toUAH = true)
        {
            isCalculating = true;
            float upworkFee = (UpworkWithdrawal ? UpworkFee : 0.0f);
            float immediateFee = (ImmediateWithdrawal ? ImmediateFee : 0.0f);
            float cashFee = (CashWithdrawal ? CashFee : 0.0f);

            if (toUAH) {
                float receive = Withdraw - upworkFee - immediateFee - cashFee;
                if (CashWithdrawal)
                {
                    float masterCardFee = receive * MasterCardFee;
                    receive -= masterCardFee;
                }
                WithdrawFeesFree = receive;
                ReceiveFeesFree = Withdraw * USD;
                Receive = receive * USD;
            } else
            {
                float withdraw = Receive * UAH;
                float masterCardFee = CashWithdrawal ? withdraw * MasterCardFee : 0.0f;
                ReceiveFeesFree = withdraw;
                WithdrawFeesFree = Receive * UAH;
                Withdraw = withdraw + upworkFee + immediateFee + cashFee + masterCardFee;
            }
            

            isCalculating = false;
        }
    }
}
