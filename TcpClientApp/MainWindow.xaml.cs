using System.Text;
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

namespace TcpServerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Engine Engine;

        public MainWindow()
        {
            InitializeComponent();

            Engine = new Engine();

            Engine.RecvMessage = WriteRecvMessage;
        }
        private void WriteRecvMessage(string value)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                this.LogTextBox.AppendText($"{value}\r");
            }));
        }
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Engine.Run();
        }
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Engine.Send(this.InputTextBox.Text);
        }
        private void SendClear_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                this.InputTextBox.Text = string.Empty;
            }));
        }

        private void RevClear_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                this.LogTextBox.Document.Blocks.Clear();
            }));
        }
    }
}